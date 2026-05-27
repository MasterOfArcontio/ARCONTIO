namespace Arcontio.Core
{
    // =============================================================================
    // BedRestedEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento world-level minimale emesso quando un NPC usa con successo un letto e
    /// riceve il recupero di riposo previsto dal command.
    /// </para>
    ///
    /// <para><b>Osservabilita' senza nuova semantica di sonno</b></para>
    /// <para>
    /// Il command continua a possedere l'unica mutazione: occupazione del letto e
    /// riduzione del bisogno di riposo. Questo evento viene pubblicato dopo tale
    /// mutazione e non introduce durata, rilascio letto, memoria sociale o regole
    /// decisionali nuove.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Tick/NpcId</b>: quando e chi ha usato il letto.</item>
    ///   <item><b>BedObjectId/Cell</b>: oggetto letto e posizione osservabile.</item>
    ///   <item><b>RestAfter</b>: valore del bisogno dopo la mutazione riuscita.</item>
    ///   <item><b>ReasonTag</b>: tag diagnostico gia' esistente nel command.</item>
    /// </list>
    /// </summary>
    public sealed class BedRestedEvent : IWorldEvent
    {
        public readonly long Tick;
        public readonly int NpcId;
        public readonly int BedObjectId;
        public readonly int CellX;
        public readonly int CellY;
        public readonly float RestAfter;
        public readonly string ReasonTag;

        public BedRestedEvent(long tick, int npcId, int bedObjectId, int cellX, int cellY, float restAfter, string reasonTag)
        {
            Tick = tick;
            NpcId = npcId;
            BedObjectId = bedObjectId;
            CellX = cellX;
            CellY = cellY;
            RestAfter = restAfter;
            ReasonTag = reasonTag ?? string.Empty;
        }

        public string Describe()
            => $"BedRested tick={Tick} npc={NpcId} bed={BedObjectId} restAfter={RestAfter:0.00} at=({CellX},{CellY})";
    }
}
