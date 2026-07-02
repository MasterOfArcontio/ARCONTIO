namespace Arcontio.Core
{
    // =============================================================================
    // LandmarkSpottedEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento percettivo prodotto quando un NPC vede direttamente un landmark del
    /// <see cref="LandmarkRegistry"/>. A differenza degli eventi world-level che
    /// devono ancora essere distribuiti ai testimoni, questo evento e' gia'
    /// observer-bound: l'osservatore e' stato deciso dal sistema di percezione
    /// dopo range, cono e linea di vista.
    /// </para>
    ///
    /// <para><b>Principio architetturale: percezione soggettiva, non registry globale</b></para>
    /// <para>
    /// Il landmark registry resta oggettivo, ma questa notifica nasce solo quando
    /// un NPC specifico vede un nodo specifico. Il layer memoria puo' quindi creare
    /// una trace soggettiva senza rieseguire una scansione globale e senza concedere
    /// conoscenza onnisciente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ObserverNpcId</b>: NPC che ha visto il landmark.</item>
    ///   <item><b>LandmarkNodeId</b>: nodo oggettivo visto, usato come soggetto della memoria.</item>
    ///   <item><b>LandmarkKind</b>: tipo compatto del landmark visto.</item>
    ///   <item><b>CellX/CellY</b>: cella osservata del nodo.</item>
    ///   <item><b>WitnessQuality01</b>: qualita' percettiva calcolata dal sistema di visione.</item>
    /// </list>
    /// </summary>
    public sealed class LandmarkSpottedEvent : IWorldEvent
    {
        public readonly int ObserverNpcId;
        public readonly int LandmarkNodeId;
        public readonly LandmarkRegistry.LandmarkKind LandmarkKind;
        public readonly int CellX;
        public readonly int CellY;
        public readonly float WitnessQuality01;

        public LandmarkSpottedEvent(
            int observerNpcId,
            int landmarkNodeId,
            LandmarkRegistry.LandmarkKind landmarkKind,
            int cellX,
            int cellY,
            float witnessQuality01)
        {
            ObserverNpcId = observerNpcId;
            LandmarkNodeId = landmarkNodeId;
            LandmarkKind = landmarkKind;
            CellX = cellX;
            CellY = cellY;
            WitnessQuality01 = witnessQuality01 < 0.05f ? 0.05f : witnessQuality01;
        }
    }
}
