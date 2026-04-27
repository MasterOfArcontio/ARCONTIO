using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // ConfidenceBeliefEvaluator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evaluator MVP che premia la confidence soggettiva del belief.
    /// </para>
    ///
    /// <para><b>Qualita' della credenza</b></para>
    /// <para>
    /// La confidence rappresenta quanto l'NPC ritiene affidabile l'ipotesi operativa.
    /// Il contributo e' pesato da configurazione, non hardcoded inline nella query.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Input</b>: <c>Belief.Confidence</c>.</item>
    ///   <item><b>Peso</b>: <c>BeliefQueryConfig.confidenceWeight</c>.</item>
    /// </list>
    /// </summary>
    public sealed class ConfidenceBeliefEvaluator : IBeliefEvaluator
    {
        public string Label => "ConfidenceScore";

        public float Evaluate(BeliefScoreContext context, BeliefQueryConfig config)
        {
            return context.Belief.Confidence * config.confidenceWeight;
        }
    }

    // =============================================================================
    // FreshnessBeliefEvaluator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evaluator MVP che premia la freschezza dell'informazione soggettiva.
    /// </para>
    ///
    /// <para><b>Informazione recente</b></para>
    /// <para>
    /// Freshness decade piu' rapidamente della confidence. Questo evaluator evita che
    /// una credenza molto sicura ma vecchia domini sempre alternative piu' recenti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Input</b>: <c>Belief.Freshness</c>.</item>
    ///   <item><b>Peso</b>: <c>BeliefQueryConfig.freshnessWeight</c>.</item>
    /// </list>
    /// </summary>
    public sealed class FreshnessBeliefEvaluator : IBeliefEvaluator
    {
        public string Label => "FreshnessScore";

        public float Evaluate(BeliefScoreContext context, BeliefQueryConfig config)
        {
            return context.Belief.Freshness * config.freshnessWeight;
        }
    }

    // =============================================================================
    // DistanceBeliefEvaluator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evaluator MVP che penalizza la distanza tra NPC e posizione stimata del belief.
    /// </para>
    ///
    /// <para><b>Distanza modulata dall'urgenza</b></para>
    /// <para>
    /// Con urgenza alta la distanza pesa meno, perche' l'NPC puo' accettare viaggi
    /// piu' lunghi pur di soddisfare il bisogno. Con urgenza bassa il sistema
    /// preferisce credenze vicine a parita' di confidence/freshness.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Distance01</b>: manhattan normalizzata da <c>maxDistanceCells</c>.</item>
    ///   <item><b>Urgency</b>: riduce la penalita' fino al moltiplicatore minimo configurato.</item>
    ///   <item><b>Output</b>: contributo negativo allo score finale.</item>
    /// </list>
    /// </summary>
    public sealed class DistanceBeliefEvaluator : IBeliefEvaluator
    {
        public string Label => "DistancePenalty";

        public float Evaluate(BeliefScoreContext context, BeliefQueryConfig config)
        {
            int distance = Mathf.Abs(context.Belief.EstimatedPosition.x - context.Query.NpcPosition.x)
                         + Mathf.Abs(context.Belief.EstimatedPosition.y - context.Query.NpcPosition.y);

            float maxDistance = config.maxDistanceCells > 0 ? config.maxDistanceCells : 1f;
            float distance01 = Mathf.Clamp01(distance / maxDistance);
            float urgency = Mathf.Clamp01(context.Query.Urgency01);
            float urgencyMultiplier = Mathf.Lerp(1f, config.highUrgencyDistancePenaltyMultiplier, urgency);

            return -distance01 * config.distanceWeight * urgencyMultiplier;
        }
    }
}
