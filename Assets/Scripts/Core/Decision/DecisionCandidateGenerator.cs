using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // DecisionCandidateGenerator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Generatore della Fase 1 del Decision Layer: costruisce il set iniziale di
    /// intenzioni candidate per un singolo NPC.
    /// </para>
    ///
    /// <para><b>Fase 1 - filtri non cognitivi</b></para>
    /// <para>
    /// Questa sessione applica solo precondizioni fisiologiche/psicologiche di base
    /// e lo stub di ScheduleFrame. Non legge il world state oggettivo e non valuta
    /// target, ownership, norme o score: questi passaggi arrivano nelle sessioni
    /// successive del Decision Layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Catalog scan</b>: visita tutte le intenzioni note al catalogo.</item>
    ///   <item><b>Need gate</b>: abilita bisogni sopra soglia di allerta o intenzioni MVP sempre disponibili.</item>
    ///   <item><b>Schedule gate</b>: filtra per dominio solo quando esiste una finestra attiva.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionCandidateGenerator
    {
        private const float MinimumObligationForWorkIntent01 = 0.01f;

        private readonly BeliefQueryService _beliefQueryService = new();

        // =============================================================================
        // GeneratePhase1Candidates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riempie la lista di output con i candidati disponibili per l'NPC nel tick
        /// corrente.
        /// </para>
        ///
        /// <para><b>Contratto senza allocazioni nascoste</b></para>
        /// <para>
        /// Il chiamante possiede la lista e puo' riusarla tra NPC o tick. La funzione
        /// la svuota e aggiunge solo candidati disponibili, lasciando ai test la
        /// responsabilita' di interrogare eventuali motivi di filtro con helper futuri.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Clear</b>: resetta l'output del chiamante.</item>
        ///   <item><b>Catalogo</b>: legge <c>DecisionIntentCatalog.All</c>.</item>
        ///   <item><b>Filtro</b>: delega a <c>TryBuildCandidate</c> la singola intenzione.</item>
        /// </list>
        /// </summary>
        public void GeneratePhase1Candidates(in DecisionEvaluationContext context, List<DecisionCandidate> output)
        {
            if (output == null)
                return;

            output.Clear();

            var all = DecisionIntentCatalog.All;
            for (int i = 0; i < all.Length; i++)
            {
                // Ogni intenzione attraversa gli stessi gate. Questo rende la Fase 1
                // prevedibile e facilita l'audit quando aggiungeremo filtri nuovi.
                if (TryBuildCandidate(context, all[i], out var candidate))
                    output.Add(candidate);
            }
        }

        private static bool TryBuildCandidate(
            in DecisionEvaluationContext context,
            DecisionIntentKind kind,
            out DecisionCandidate candidate)
        {
            candidate = default;

            if (!DecisionIntentCatalog.TryGetMetadata(kind, out var metadata))
                return false;

            float urgency = GetNeedUrgency(context.Needs, metadata.PrimaryNeed);
            bool isCritical = IsNeedCritical(context.Needs, context.Dna, metadata.PrimaryNeed, urgency);

            // Le intenzioni non MVP restano nel catalogo, ma non entrano ancora nel
            // set operativo: cosi' il catalogo e' completo senza promettere esecuzione.
            if (!metadata.IsMvpAvailable)
                return false;

            // WaitAndObserve e' il fallback minimo: deve poter esistere anche quando
            // nessun bisogno supera la soglia di allerta.
            if (metadata.Kind != DecisionIntentKind.WaitAndObserve
                && metadata.PrimaryNeed != NeedKind.COUNT
                && !IsNeedAlert(context.Needs, context.Dna, metadata.PrimaryNeed, urgency))
                return false;

            if (!context.ScheduleFrame.Allows(metadata, isCritical))
                return false;

            if (!context.NormContext.Allows(metadata, isCritical))
                return false;

            if (!PassesObligationGate(context.Profile, metadata, isCritical))
                return false;

            candidate = DecisionCandidate.Available(metadata, urgency, isCritical);

            if (metadata.RequiresBeliefTarget
                && !TryAttachBeliefTarget(context, metadata, urgency, ref candidate))
            {
                return false;
            }

            return true;
        }

        // =============================================================================
        // TryAttachBeliefTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega a un candidato un target belief selezionato tramite QuerySystem.
        /// </para>
        ///
        /// <para><b>Decision Layer -> QuerySystem, mai BeliefStore diretto</b></para>
        /// <para>
        /// Il metodo costruisce un <c>BeliefQueryContext</c> minimale e delega la
        /// selezione a <c>BeliefQueryService</c>. Non interpreta direttamente le entry
        /// del <c>BeliefStore</c> e non consulta lo stato oggettivo del mondo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Null gate</b>: senza store soggettivo non esiste target valido.</item>
        ///   <item><b>Query</b>: categoria, urgenza, posizione e min confidence.</item>
        ///   <item><b>Attach</b>: conserva il risultato per scoring e debug successivi.</item>
        /// </list>
        /// </summary>
        private bool TryAttachBeliefTarget(
            in DecisionEvaluationContext context,
            DecisionIntentMetadata metadata,
            float urgency01,
            ref DecisionCandidate candidate)
        {
            if (context.Beliefs == null)
                return false;

            var query = new BeliefQueryContext(
                metadata.TargetBeliefCategory,
                urgency01,
                context.NpcPosition,
                context.BeliefQueryConfig.defaultMinConfidence);

            // Il Decision Layer non legge direttamente il BeliefStore per scegliere:
            // delega al QuerySystem, che applica filtro, ranking e breakdown.
            var result = _beliefQueryService.QueryBest(
                context.Beliefs,
                query,
                context.BeliefQueryConfig);

            if (result.IsEmpty)
                return false;

            candidate.AttachBeliefResult(result);
            return true;
        }

        // =============================================================================
        // PassesObligationGate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il gate minimo basato su <c>ObligationProfile</c> per impedire che
        /// intenzioni di dominio non motivate entrino nel set candidati.
        /// </para>
        ///
        /// <para><b>ObligationProfile come filtro leggero</b></para>
        /// <para>
        /// In questa sessione l'obbligo non e' ancora uno score: diventa solo una
        /// precondizione conservativa per intenzioni non emergenziali. La pressione
        /// numerica dell'obbligo verra' valorizzata nella Fase 2.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Emergenza</b>: bisogni critici e intenzioni emergenziali passano sempre.</item>
        ///   <item><b>Dominio nullo</b>: fallback e osservazione non richiedono obbligo.</item>
        ///   <item><b>Profilo mancante</b>: fallback permissivo per test e migrazioni.</item>
        /// </list>
        /// </summary>
        private static bool PassesObligationGate(NpcProfile profile, DecisionIntentMetadata metadata, bool isCritical)
        {
            // Le intenzioni emergenziali non devono essere bloccate da un profilo
            // obblighi ancora neutro: fame, riposo e sicurezza restano floor futuri.
            if (isCritical || metadata.IsEmergencyIntent)
                return true;

            // Le intenzioni senza dominio non appartengono a un ruolo o obbligo.
            if (metadata.Domain == DomainKind.None)
                return true;

            if (profile == null || profile.Obligation == null)
                return true;

            // Per ora il gate e' volutamente minimo: impedisce solo che una futura
            // intenzione lavorativa con obbligo zero entri nel set senza motivazione.
            return profile.Obligation.Get(metadata.Domain) >= MinimumObligationForWorkIntent01;
        }

        private static float GetNeedUrgency(NpcNeeds needs, NeedKind need)
        {
            int index = (int)need;
            if (index < 0 || index >= (int)NeedKind.COUNT)
                return 0f;

            return needs.GetValue(need);
        }

        private static bool IsNeedAlert(NpcNeeds needs, NpcDnaProfile dna, NeedKind need, float urgency01)
        {
            int index = (int)need;
            if (index < 0 || index >= (int)NeedKind.COUNT)
                return false;

            // Preferiamo il flag calcolato dal NeedsDecaySystem, ma teniamo un fallback
            // diretto sulle soglie DNA per test e contesti costruiti a mano.
            if (needs.IsAlert(need))
                return true;

            float threshold = dna != null ? dna.Thresholds.NeedAlert01 : 0.5f;
            return urgency01 >= threshold;
        }

        private static bool IsNeedCritical(NpcNeeds needs, NpcDnaProfile dna, NeedKind need, float urgency01)
        {
            int index = (int)need;
            if (index < 0 || index >= (int)NeedKind.COUNT)
                return false;

            // Come sopra: il flag runtime e' autorevole quando presente, il fallback
            // mantiene utilizzabile il generatore nei test isolati.
            if (needs.IsCritical(need))
                return true;

            float threshold = dna != null ? dna.Thresholds.NeedCritical01 : 0.8f;
            return urgency01 >= threshold;
        }
    }
}
