namespace Arcontio.Core
{
    // =============================================================================
    // ObjectPickedUpEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento world-level minimale emesso quando un NPC prende fisicamente un oggetto
    /// da una cella del mondo.
    /// </para>
    ///
    /// <para><b>World fact osservabile</b></para>
    /// <para>
    /// Il pickup cambia lo stato oggettivo dell'oggetto da grounded a held. Anche
    /// nel percorso DevTools questo non deve restare una mutazione silenziosa: il
    /// MessageBus riceve un fatto di mondo marcato <see cref="IWorldEvent"/> che
    /// future pipeline di memoria, explainability o norme potranno decidere di
    /// consumare senza cambiare il contratto di base.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Tick</b>: tick simulativo in cui il command ha applicato la mutazione.</item>
    ///   <item><b>NpcId/ObjectId</b>: attore fisico e oggetto trasportato.</item>
    ///   <item><b>FromCell</b>: cella da cui l'oggetto e' stato rimosso.</item>
    /// </list>
    /// </summary>
    public sealed class ObjectPickedUpEvent : IWorldEvent
    {
        public readonly long Tick;
        public readonly int NpcId;
        public readonly int ObjectId;
        public readonly int FromCellX;
        public readonly int FromCellY;

        public ObjectPickedUpEvent(long tick, int npcId, int objectId, int fromCellX, int fromCellY)
        {
            Tick = tick;
            NpcId = npcId;
            ObjectId = objectId;
            FromCellX = fromCellX;
            FromCellY = fromCellY;
        }

        public string Describe()
            => $"ObjectPickedUp tick={Tick} npc={NpcId} obj={ObjectId} from=({FromCellX},{FromCellY})";
    }
}
