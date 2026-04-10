namespace Arcontio.Core
{
    /// <summary>
    /// DoorOpenedEvent (IWorldEvent):
    /// Un NPC ha aperto una porta.
    ///
    /// Evento fattuale — descrive che la porta è stata aperta fisicamente.
    /// Implementa IWorldEvent perché può creare memoria negli NPC vicini
    /// (altri possono sentire il rumore o vedere la porta aprirsi).
    ///
    /// npcId:    NPC che ha aperto la porta (-1 se aperta da sistema)
    /// objectId: ID runtime della porta aperta
    /// cellX/Y:  posizione della porta nel mondo (per LOS/range percezione)
    /// </summary>
    public sealed class DoorOpenedEvent : IWorldEvent
    {
        public readonly int NpcId;
        public readonly int ObjectId;
        public readonly int CellX;
        public readonly int CellY;

        public DoorOpenedEvent(int npcId, int objectId, int cellX, int cellY)
        {
            NpcId    = npcId;
            ObjectId = objectId;
            CellX    = cellX;
            CellY    = cellY;
        }

        public override string ToString()
            => $"DoorOpened npc={NpcId} obj={ObjectId} at=({CellX},{CellY})";
    }
}
