namespace Arcontio.Core
{
    /// <summary>
    /// NpcSpottedEvent (IWorldEvent):
    /// Un NPC osserva un altro NPC (entità mobile).
    ///
    /// Perché serve:
    /// - Evita telepatia: la conoscenza "NPC X è (stato) in quella cella" nasce solo se lo vedi.
    /// - Alimenta la memoria generalizzata (Observed entities) tramite MemoryEncodingSystem.
    ///
    /// Nota:
    /// - Questo evento è *visual-based* (range + cono + LOS), calcolato da NpcPerceptionSystem.
    /// - WitnessQuality01 è una stima qualitativa (oggi: funzione della distanza).
    ///
    /// Importante (design):
    /// - L'evento contiene anche la posizione dell'NPC osservato perché:
    ///   1) serve per la witness selection (altri NPC possono essere testimoni)
    ///   2) serve per aggiornare LastKnownCell nella memoria soggettiva
    /// </summary>
    public sealed class NpcSpottedEvent : IWorldEvent
    {
        public readonly int ObserverNpcId;
        public readonly int ObservedNpcId;

        public readonly int CellX;
        public readonly int CellY;

        public readonly int DistanceCells;
        public readonly float WitnessQuality01;

        public NpcSpottedEvent(int observerNpcId, int observedNpcId, int cellX, int cellY, int distanceCells, float witnessQuality01)
        {
            ObserverNpcId = observerNpcId;
            ObservedNpcId = observedNpcId;
            CellX = cellX;
            CellY = cellY;
            DistanceCells = distanceCells;
            WitnessQuality01 = witnessQuality01;
        }
    }
}
