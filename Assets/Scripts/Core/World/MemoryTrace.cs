namespace Arcontio.Core
{
    // =============================================================================
    // MemoryTrace
    // =============================================================================
    /// <summary>
    /// <para>
    /// Unità minima di memoria soggettiva conservata nel <c>MemoryStore</c> di un
    /// NPC. La traccia descrive cosa è stato percepito o sentito, dove è avvenuto
    /// e con quale qualità soggettiva viene ricordato.
    /// </para>
    ///
    /// <para><b>Memoria soggettiva arricchita al momento percettivo</b></para>
    /// <para>
    /// La traccia deve contenere le informazioni semantiche già disponibili nel
    /// momento in cui nasce l'evento percettivo. Per gli oggetti, ad esempio,
    /// <c>SubjectDefId</c> conserva il tipo osservato (<c>food_stock</c>,
    /// <c>bed_wood_poor</c>, ecc.) così i layer successivi non devono interrogare
    /// il <c>World</c> per ricostruire retroattivamente il significato della memoria.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Type</b>: tipo narrativo della memoria.</item>
    ///   <item><b>SubjectId</b>: id del soggetto principale, se applicabile.</item>
    ///   <item><b>SubjectDefId</b>: tipo semantico del soggetto quando noto alla percezione.</item>
    ///   <item><b>CellX/CellY</b>: posizione soggettiva associata alla traccia.</item>
    ///   <item><b>Intensity/Reliability/Decay</b>: qualità e decadimento della memoria.</item>
    ///   <item><b>Heard fields</b>: metadati per distinguere esperienza diretta e informazione comunicata.</item>
    /// </list>
    /// </summary>
    public struct MemoryTrace
    {
        public MemoryType Type;

        /// <summary>
        /// Soggetto dell'evento nella memoria (es: predatorId, attackerId, victimId).
        /// -1 se non applicabile.
        /// </summary>
        public int SubjectId;

        /// <summary>
        /// Soggetto secondario opzionale.
        /// Esempi:
        /// - TheftWitnessed ? vittima
        /// - altri eventi ? 0
        /// </summary>
        public int SecondarySubjectId;

        /// <summary>
        /// Tipo semantico del soggetto quando il sistema percettivo lo conosce.
        /// Per <c>ObjectSpotted</c> corrisponde al <c>DefId</c> dell'oggetto visto.
        /// Resta vuoto per eventi che non hanno un soggetto tipizzato o per memorie
        /// legacy salvate prima dell'introduzione di questo campo.
        /// </summary>
        public string SubjectDefId;

        /// <summary>
        /// Coordinate della cella a cui la memoria è associata.
        /// -1/-1 se non applicabile.
        /// </summary>
        public int CellX;
        public int CellY;

        /// <summary>
        /// Intensità corrente: 0..1
        /// </summary>
        public float Intensity01;

        /// <summary>
        /// Affidabilità: 0..1
        /// </summary>
        public float Reliability01;

        /// <summary>
        /// Decadimento per tick (base).
        /// Esempio: 0.01 significa che in ~100 tick la traccia si azzera (a parità di dt).
        /// </summary>
        public float DecayPerTick01;

        /// <summary>
        /// Tick dell'ultima osservazione diretta che ha prodotto o rinforzato
        /// questa traccia. Vale 0 per trace legacy o trace non percettive.
        /// </summary>
        public int LastObservedTick;

        /// <summary>
        /// Gestione del tipo di fonte: se diretta o raccontata
        /// </summary>       
        public bool IsHeard;
        public HeardKind HeardKind;
        public int SourceSpeakerId; // chi me l'ha detto (solo se IsHeard=true)


        public override string ToString()
        {
            string def = string.IsNullOrWhiteSpace(SubjectDefId) ? "" : $" def={SubjectDefId}";
            return $"{Type} subj={SubjectId}{def} cell=({CellX},{CellY}) I={Intensity01:0.00} R={Reliability01:0.00} d={DecayPerTick01:0.000} seenTick={LastObservedTick}";
        }
    }

    /// <summary>
    /// HeardKind: indica come questa memoria è stata acquisita via comunicazione.
    /// Non è stata quindi acquisita tramite una esperienza diretta
    /// </summary>
    public enum HeardKind
    {
        None = 0,       // memoria non "sentita": esperienza diretta
        DirectHeard = 1, // ricevuta direttamente da uno speaker
        RumorHeard = 2   // informazione di seconda mano (chainDepth > 0)
    }
}
