using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // DecisionContextBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Costruisce il <c>DecisionEvaluationContext</c> minimo ammesso dal Decision
    /// Layer per un singolo NPC.
    /// </para>
    ///
    /// <para><b>v0.11c.01b - Estrazione behavior-preserving del context gathering</b></para>
    /// <para>
    /// Questo builder non decide intenzioni, non genera candidati, non calcola score,
    /// non seleziona, non produce <c>JobRequest</c>, non emette <c>ICommand</c> e non
    /// assegna job. La sua unica responsabilita' e' raccogliere gli stessi input che
    /// <c>NeedsDecisionRule</c> gia' passava al Decision Layer, rendendo esplicito il
    /// punto di compatibilita' transitorio tra il bridge legacy e il futuro
    /// Decision Orchestrator.
    /// </para>
    ///
    /// <para><b>World come compatibilita' transitoria, non modello cognitivo target</b></para>
    /// <para>
    /// Il metodo riceve ancora <c>World</c> perche' oggi gli store per-NPC vivono li':
    /// <c>NpcDna</c>, <c>NpcProfiles</c>, <c>GridPos</c>, <c>Beliefs</c>, config query
    /// e registry di explainability. Questo accesso e' un adapter runtime
    /// provvisorio: il contesto risultante non espone mai <c>World</c>, non legge
    /// <c>World.Objects</c>, non legge <c>FoodStocks</c> e non legge
    /// <c>MemoryStore</c> come fonte cognitiva.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Validazione componenti</b>: fallisce se mancano DNA, profilo, posizione o belief store.</item>
    ///   <item><b>Snapshot per-NPC</b>: copia posizione e riferimenti gia' risolti per il Decision Layer.</item>
    ///   <item><b>Stub permissivi</b>: conserva ScheduleFrame e NormContext esattamente come nel bridge precedente.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionContextBuilder
    {
        // =============================================================================
        // TryBuild
        // =============================================================================
        /// <summary>
        /// <para>
        /// Tenta di costruire un <c>DecisionEvaluationContext</c> per l'NPC indicato.
        /// </para>
        ///
        /// <para><b>Costruzione passiva del contesto</b></para>
        /// <para>
        /// Il metodo esegue solo lookup read-only sugli store per-NPC necessari alla
        /// pipeline decisionale. Se un input richiesto manca, restituisce
        /// <c>false</c> e lascia il chiamante libero di mantenere il comportamento
        /// legacy gia' esistente. Non produce fallback e non registra telemetry.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Dna/Profile</b>: identita' cognitiva e profilo runtime dell'NPC.</item>
        ///   <item><b>Position</b>: posizione runtime per-NPC usata dal QuerySystem per il calcolo distanza.</item>
        ///   <item><b>Beliefs</b>: store soggettivo interrogabile solo dai servizi di query.</item>
        ///   <item><b>Config/EL</b>: configurazione e registry diagnostici passivi.</item>
        /// </list>
        /// </summary>
        public bool TryBuild(
            World world,
            int npcId,
            in NpcNeeds needs,
            int nowTick,
            out DecisionEvaluationContext context)
        {
            context = default;

            if (world == null)
                return false;

            if (!world.NpcDna.TryGetValue(npcId, out var dna) || dna == null)
                return false;

            if (!world.NpcProfiles.TryGetValue(npcId, out var profile) || profile == null)
                return false;

            if (!world.GridPos.TryGetValue(npcId, out var position))
                return false;

            if (!world.Beliefs.TryGetValue(npcId, out var beliefs) || beliefs == null)
                return false;

            context = new DecisionEvaluationContext(
                npcId: npcId,
                tick: nowTick,
                needs: needs,
                dna: dna,
                profile: profile,
                npcPosition: new Vector2Int(position.X, position.Y),
                beliefs: beliefs,
                beliefQueryConfig: world.Global.BeliefQuery,
                explainabilityConfig: world.Config?.Sim?.memory_belief_decision_explainability,
                explainabilityRegistry: world.MemoryBeliefDecisionExplainability,
                scheduleFrame: new DecisionScheduleFrame(false, DomainKind.None, true),
                normContext: new DecisionNormContext(false, 1f, true));

            return true;
        }
    }
}
