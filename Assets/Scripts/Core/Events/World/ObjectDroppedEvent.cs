namespace Arcontio.Core
{
    // =============================================================================
    // ObjectDroppedEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento world-level minimale emesso quando un NPC deposita fisicamente un
    /// oggetto trasportato su una cella del mondo.
    /// </para>
    ///
    /// <para><b>World fact osservabile</b></para>
    /// <para>
    /// Il drop cambia lo stato oggettivo dell'oggetto da held a grounded e aggiorna
    /// la cella canonica dell'istanza. L'evento conserva il fatto senza introdurre
    /// memoria sociale, giudizio o regole automatiche: quelle pipeline potranno
    /// essere collegate in futuro se il dominio lo richiedera'.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Tick</b>: tick simulativo in cui il command ha applicato la mutazione.</item>
    ///   <item><b>NpcId/ObjectId</b>: attore fisico e oggetto rilasciato.</item>
    ///   <item><b>ToCell</b>: cella in cui l'oggetto e' tornato grounded.</item>
    /// </list>
    /// </summary>
    public sealed class ObjectDroppedEvent : IWorldEvent
    {
        public readonly long Tick;
        public readonly int NpcId;
        public readonly int ObjectId;
        public readonly int ToCellX;
        public readonly int ToCellY;

        public ObjectDroppedEvent(long tick, int npcId, int objectId, int toCellX, int toCellY)
        {
            Tick = tick;
            NpcId = npcId;
            ObjectId = objectId;
            ToCellX = toCellX;
            ToCellY = toCellY;
        }

        public string Describe()
            => $"ObjectDropped tick={Tick} npc={NpcId} obj={ObjectId} to=({ToCellX},{ToCellY})";
    }
}
