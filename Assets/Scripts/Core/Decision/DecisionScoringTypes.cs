namespace Arcontio.Core
{
    // =============================================================================
    // DecisionScoreContribution
    // =============================================================================
    /// <summary>
    /// <para>
    /// Singolo contributo nominato allo score di un candidato decisionale.
    /// </para>
    ///
    /// <para><b>Explainability del Decision Layer</b></para>
    /// <para>
    /// Ogni termine dello scoring deve restare leggibile in debug. La struttura
    /// conserva label e valore senza esporre logica, cosi' la UI o i test possono
    /// spiegare perche' una intenzione abbia vinto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Label</b>: nome stabile del termine di scoring.</item>
    ///   <item><b>Value</b>: contributo numerico gia' pesato.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionScoreContribution
    {
        public readonly string Label;
        public readonly float Value;

        public DecisionScoreContribution(string label, float value)
        {
            Label = label ?? string.Empty;
            Value = value;
        }
    }

    // =============================================================================
    // DecisionScoringConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione dei pesi della Fase 2 del Decision Layer.
    /// </para>
    ///
    /// <para><b>Pesi nominati e centralizzati</b></para>
    /// <para>
    /// I pesi restano in una struct separata per evitare costanti sparse negli
    /// evaluator. In questa sessione e' attivo solo <c>needUrgencyWeight</c>; i
    /// campi successivi entrano nelle sessioni 6-8.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>needUrgencyWeight</b>: peso della pressione del bisogno.</item>
    ///   <item><b>competenceWeight/preferenceWeight</b>: riservati alla sessione 6.</item>
    ///   <item><b>obligationWeight</b>: riservato alla sessione 7.</item>
    ///   <item><b>memoryConfidenceWeight</b>: riservato alla sessione 8.</item>
    /// </list>
    /// </summary>
    public struct DecisionScoringConfig
    {
        public float needUrgencyWeight;
        public float competenceWeight;
        public float preferenceWeight;
        public float obligationWeight;
        public float memoryConfidenceWeight;
        public float cognitiveModulatorWeight;
        public float criticalNeedFloor;
        public float highObligationFloor;

        public static DecisionScoringConfig Default()
        {
            return new DecisionScoringConfig
            {
                needUrgencyWeight = 1.00f,
                competenceWeight = 0.20f,
                preferenceWeight = 0.25f,
                obligationWeight = 0f,
                memoryConfidenceWeight = 0f,
                cognitiveModulatorWeight = 0f,
                criticalNeedFloor = 0f,
                highObligationFloor = 0f
            };
        }
    }
}
