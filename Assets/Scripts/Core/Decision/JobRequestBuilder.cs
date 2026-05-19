using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // JobRequestBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Costruisce <c>JobRequest</c> a partire da una intenzione decisionale gia'
    /// selezionata e da target operativi gia' risolti dal chiamante legacy.
    /// </para>
    ///
    /// <para><b>Boundary dati SelectedDecision -> JobRequest</b></para>
    /// <para>
    /// Questo builder non seleziona intenzioni, non genera candidati, non calcola
    /// score, non legge <c>World</c>, non emette <c>ICommand</c>, non crea job e non
    /// assegna job. La sua responsabilita' e' limitata a rendere esplicito il record
    /// dati che attraversa il confine Decision Layer -> Job Layer.
    /// </para>
    ///
    /// <para><b>Debito transitorio dichiarato</b></para>
    /// <para>
    /// Alcuni target, come l'object id del food stock o la probe cell di SearchFood,
    /// sono ancora risolti da <c>NeedsDecisionRule</c> tramite helper legacy. Questo
    /// builder li riceve come input gia' pronti e non prova a validarli leggendo il
    /// mondo oggettivo. La migrazione di quella risoluzione appartiene a checkpoint
    /// futuri, non a v0.11c.01c.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EatKnownFood</b>: richiede belief target e object id operativo.</item>
    ///   <item><b>SearchFood</b>: richiede una probe cell locale gia' scelta dal bridge.</item>
    ///   <item><b>Priority mapping</b>: conserva la mappatura conservativa preesistente.</item>
    /// </list>
    /// </summary>
    public sealed class JobRequestBuilder
    {
        // =============================================================================
        // TryBuildEatKnownFoodRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la richiesta job per l'intent <c>EatKnownFood</c>.
        /// </para>
        ///
        /// <para><b>Nessuna validazione oggettiva</b></para>
        /// <para>
        /// Il metodo non verifica se il target esista davvero nel <c>World</c>. Usa
        /// il belief scelto dal Decision Layer per la cella soggettiva e conserva
        /// l'object id operativo ricevuto dal bridge transitorio.
        /// </para>
        /// </summary>
        public bool TryBuildEatKnownFoodRequest(
            int tick,
            int npcId,
            DecisionCandidate candidate,
            int targetObjectId,
            out JobRequest request,
            out string reason)
        {
            request = default;
            reason = string.Empty;

            if (candidate.Kind != DecisionIntentKind.EatKnownFood)
            {
                reason = "UnsupportedJobRequestIntent";
                return false;
            }

            if (candidate.BeliefResult.IsEmpty)
            {
                reason = "MissingBeliefTarget";
                return false;
            }

            if (targetObjectId <= 0)
            {
                reason = "MissingTargetObject";
                return false;
            }

            string beliefKey = BuildBeliefKey(candidate.BeliefResult.Belief);
            request = new JobRequest(
                $"jobreq_food_{npcId}_{targetObjectId}_{tick}",
                npcId,
                candidate.Kind,
                ResolveJobPriorityClass(candidate),
                candidate.NeedUrgency01,
                tick,
                true,
                candidate.BeliefResult.Belief.EstimatedPosition,
                targetObjectId,
                beliefKey,
                "FoodJobVerticalSlice");

            reason = "JobRequestBuilt";
            return true;
        }

        // =============================================================================
        // TryBuildSearchFoodRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la richiesta job per l'intent <c>SearchFood</c>.
        /// </para>
        ///
        /// <para><b>Probe esecutiva, non conoscenza cognitiva</b></para>
        /// <para>
        /// La target cell rappresenta una destinazione locale di esplorazione gia'
        /// calcolata dal bridge. Il builder non cerca cibo, non legge oggetti e non
        /// aggiorna memoria o belief.
        /// </para>
        /// </summary>
        public bool TryBuildSearchFoodRequest(
            int tick,
            int npcId,
            DecisionCandidate candidate,
            Vector2Int targetCell,
            out JobRequest request,
            out string reason)
        {
            request = default;
            reason = string.Empty;

            if (candidate.Kind != DecisionIntentKind.SearchFood)
            {
                reason = "UnsupportedJobRequestIntent";
                return false;
            }

            request = new JobRequest(
                $"jobreq_search_food_probe_{npcId}_{targetCell.x}_{targetCell.y}_{tick}",
                npcId,
                DecisionIntentKind.SearchFood,
                ResolveJobPriorityClass(candidate),
                candidate.NeedUrgency01,
                tick,
                true,
                targetCell,
                0,
                string.Empty,
                "SearchFoodLocalProbe");

            reason = "JobRequestBuilt";
            return true;
        }

        // =============================================================================
        // ResolveJobPriorityClass
        // =============================================================================
        /// <summary>
        /// <para>
        /// Mappa il candidato decisionale in una classe di priorita' minima per la
        /// richiesta job, preservando la logica precedente di <c>NeedsDecisionRule</c>.
        /// </para>
        ///
        /// <para><b>Non e' arbitration</b></para>
        /// <para>
        /// Questa mappatura non decide preemption e non confronta job attivi. Fornisce
        /// solo un campo dati che il Job Layer potra' usare secondo le proprie policy.
        /// </para>
        /// </summary>
        public static JobPriorityClass ResolveJobPriorityClass(DecisionCandidate candidate)
        {
            if (candidate.IsCritical)
                return JobPriorityClass.Critical;

            if (candidate.NeedUrgency01 >= 0.8f)
                return JobPriorityClass.Important;

            if (candidate.Kind == DecisionIntentKind.WaitAndObserve)
                return JobPriorityClass.Idle;

            return JobPriorityClass.Normal;
        }

        // =============================================================================
        // BuildBeliefKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una chiave diagnostica compatta per correlare il JobRequest alla
        /// belief che ha motivato la scelta.
        /// </para>
        /// </summary>
        public static string BuildBeliefKey(BeliefEntry belief)
        {
            return $"{belief.Category}:{belief.BeliefId}@{belief.EstimatedPosition.x},{belief.EstimatedPosition.y}";
        }
    }
}
