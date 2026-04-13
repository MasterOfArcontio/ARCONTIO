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

            candidate = DecisionCandidate.Available(metadata, urgency, isCritical);
            return true;
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
