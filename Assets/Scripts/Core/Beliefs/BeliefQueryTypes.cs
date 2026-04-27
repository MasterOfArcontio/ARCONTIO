using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // BeliefQueryContext
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contesto minimo con cui il futuro Decision Layer chiede al QuerySystem di
    /// valutare credenze appartenenti a una categoria obiettivo.
    /// </para>
    ///
    /// <para><b>QueryContext minimale</b></para>
    /// <para>
    /// Il documento BeliefStore/QuerySystem chiede disciplina: un campo entra nel
    /// contesto solo quando serve a un caso d'uso concreto e quando e' usato da piu'
    /// evaluator. In questa sessione MVP manteniamo solo categoria, urgenza,
    /// posizione NPC e soglia minima di confidence.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>GoalType</b>: categoria di belief richiesta dal chiamante.</item>
    ///   <item><b>Urgency01</b>: urgenza normalizzata del bisogno o obiettivo.</item>
    ///   <item><b>NpcPosition</b>: cella corrente dell'NPC, usata dal DistanceEvaluator.</item>
    ///   <item><b>MinConfidence</b>: soglia sotto cui il candidato viene scartato.</item>
    /// </list>
    /// </summary>
    public readonly struct BeliefQueryContext
    {
        public readonly BeliefCategory GoalType;
        public readonly float Urgency01;
        public readonly Vector2Int NpcPosition;
        public readonly float MinConfidence;

        public BeliefQueryContext(BeliefCategory goalType, float urgency01, Vector2Int npcPosition, float minConfidence)
        {
            GoalType = goalType;
            Urgency01 = Clamp01(urgency01);
            NpcPosition = npcPosition;
            MinConfidence = Clamp01(minConfidence);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    // =============================================================================
    // BeliefScoreContribution
    // =============================================================================
    /// <summary>
    /// <para>
    /// Singolo contributo nominato allo score finale di una query belief.
    /// </para>
    ///
    /// <para><b>Explainability nativa</b></para>
    /// <para>
    /// Ogni evaluator produce un'etichetta e un valore. Il risultato della query puo'
    /// quindi spiegare perche' un belief e' stato selezionato senza affidarsi a log
    /// testuali sparsi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Label</b>: nome leggibile dell'evaluator.</item>
    ///   <item><b>Value</b>: contributo numerico allo score finale.</item>
    /// </list>
    /// </summary>
    public readonly struct BeliefScoreContribution
    {
        public readonly string Label;
        public readonly float Value;

        public BeliefScoreContribution(string label, float value)
        {
            Label = label ?? string.Empty;
            Value = value;
        }
    }

    // =============================================================================
    // BeliefScoreContext
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contesto di scoring temporaneo usato dagli evaluator mentre valutano un
    /// singolo <c>BeliefEntry</c>.
    /// </para>
    ///
    /// <para><b>Accumulo strutturato</b></para>
    /// <para>
    /// Il contesto conserva il belief, il contesto di query e la lista dei contributi.
    /// Gli evaluator non modificano lo store e non scelgono il vincitore: aggiungono
    /// solo un contributo leggibile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Belief</b>: entry sotto valutazione.</item>
    ///   <item><b>Query</b>: richiesta del chiamante.</item>
    ///   <item><b>Contributions</b>: breakdown cumulativo dello score.</item>
    /// </list>
    /// </summary>
    public sealed class BeliefScoreContext
    {
        public BeliefEntry Belief;
        public BeliefQueryContext Query;
        public readonly List<BeliefScoreContribution> Contributions = new(8);

        public void Reset(BeliefEntry belief, BeliefQueryContext query)
        {
            Belief = belief;
            Query = query;
            Contributions.Clear();
        }
    }

    // =============================================================================
    // BeliefQueryResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato strutturato di una query sul BeliefStore.
    /// </para>
    ///
    /// <para><b>Contratto per il Decision Layer</b></para>
    /// <para>
    /// Il chiamante riceve un flag <c>IsEmpty</c> invece di dover interpretare valori
    /// sentinel. Se un belief e' disponibile, il risultato include score finale e
    /// breakdown dei contributi per debug ed explainability futura.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsEmpty</b>: true quando non esiste alcun candidato usabile.</item>
    ///   <item><b>Belief</b>: entry selezionata dalla query.</item>
    ///   <item><b>FinalScore</b>: somma dei contributi degli evaluator.</item>
    ///   <item><b>Contributions</b>: breakdown stabile per UI/debug.</item>
    /// </list>
    /// </summary>
    public readonly struct BeliefQueryResult
    {
        public readonly bool IsEmpty;
        public readonly BeliefEntry Belief;
        public readonly float FinalScore;
        public readonly BeliefScoreContribution[] Contributions;

        public BeliefQueryResult(bool isEmpty, BeliefEntry belief, float finalScore, BeliefScoreContribution[] contributions)
        {
            IsEmpty = isEmpty;
            Belief = belief;
            FinalScore = finalScore;
            Contributions = contributions ?? System.Array.Empty<BeliefScoreContribution>();
        }

        public static BeliefQueryResult Empty()
        {
            return new BeliefQueryResult(true, default, 0f, System.Array.Empty<BeliefScoreContribution>());
        }
    }

    // =============================================================================
    // IBeliefEvaluator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modulo riusabile che assegna un contributo numerico a un belief candidato.
    /// </para>
    ///
    /// <para><b>Evaluator condivisi</b></para>
    /// <para>
    /// Le query devono usare gli stessi evaluator per mantenere coerenza cognitiva:
    /// confidence, freshness e distanza non devono avere significati diversi tra cibo,
    /// riposo e sicurezza.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Label</b>: nome leggibile per il breakdown.</item>
    ///   <item><b>Evaluate</b>: restituisce il contributo allo score finale.</item>
    /// </list>
    /// </summary>
    public interface IBeliefEvaluator
    {
        string Label { get; }
        float Evaluate(BeliefScoreContext context, BeliefQueryConfig config);
    }
}
