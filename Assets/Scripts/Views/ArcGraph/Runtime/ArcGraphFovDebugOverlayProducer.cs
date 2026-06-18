using Arcontio.Core;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphFovDebugOverlayProducerDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del producer FOV corrente per ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: debug leggibile, non simulazione</b></para>
    /// <para>
    /// Questa struttura descrive solo quante celle diagnostiche sono state copiate
    /// nello snapshot ArcGraph. Non contiene riferimenti Unity, non espone array
    /// mutabili del mondo e non rappresenta memoria o percezione reale dell'NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasWorld</b>: il producer ha ricevuto un World valido.</item>
    ///   <item><b>NpcId</b>: NPC usato come osservatore debug.</item>
    ///   <item><b>ObservedCellCount</b>: celle nel cono osservato reale.</item>
    ///   <item><b>WatchedMarginCellCount</b>: celle del margine watched diagnostico.</item>
    ///   <item><b>Reason</b>: esito sintetico del build.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphFovDebugOverlayProducerDiagnostics
    {
        public readonly bool HasWorld;
        public readonly int NpcId;
        public readonly int ObservedCellCount;
        public readonly int WatchedMarginCellCount;
        public readonly string Reason;

        public int TotalCellCount => ObservedCellCount + WatchedMarginCellCount;

        // =============================================================================
        // ArcGraphFovDebugOverlayProducerDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile del build FOV.
        /// </para>
        /// </summary>
        public ArcGraphFovDebugOverlayProducerDiagnostics(
            bool hasWorld,
            int npcId,
            int observedCellCount,
            int watchedMarginCellCount,
            string reason)
        {
            HasWorld = hasWorld;
            NpcId = npcId > 0 ? npcId : -1;
            ObservedCellCount = observedCellCount < 0 ? 0 : observedCellCount;
            WatchedMarginCellCount = watchedMarginCellCount < 0 ? 0 : watchedMarginCellCount;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphFovDebugOverlayProducer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Producer read-only che copia nello snapshot ArcGraph il cono FOV corrente di
    /// un NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: metodo MapGrid assorbito come producer</b></para>
    /// <para>
    /// Il vecchio <c>MapGridFovHeatmapOverlay.RenderCurrentCone(...)</c> calcolava
    /// e disegnava nello stesso punto. Qui separiamo le due responsabilita': questo
    /// producer usa la stessa geometria Core, cioe' range, cono/front e linea di
    /// vista opzionale, ma produce solo DTO <c>ArcGraphDebugCellOverlaySnapshot</c>.
    /// Il renderer ArcGraph decide poi come mostrare quelle celle.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FillCurrentConeSnapshot</b>: entry point pubblico verso snapshot ArcGraph.</item>
    ///   <item><b>IsObservedFovCell</b>: replica il gate observed del metodo MapGrid.</item>
    ///   <item><b>IsWatchedMarginCell</b>: replica il bordo watched simmetrico.</item>
    ///   <item><b>AddCell</b>: converte una cella in DTO debug ArcGraph.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphFovDebugOverlayProducer
    {
        private const string ObservedColorKey = "debug/fov/observed";
        private const string WatchedColorKey = "debug/fov/watched";

        // =============================================================================
        // FillCurrentConeSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Popola uno snapshot debug ArcGraph con il cono FOV corrente.
        /// </para>
        ///
        /// <para><b>Accesso read-only al World</b></para>
        /// <para>
        /// Il metodo legge posizione, facing e parametri percettivi gia' presenti
        /// nel <c>World</c>. Non marca dirty, non aggiorna telemetry, non scrive in
        /// memoria NPC e non invia eventi. Serve solo al debug visuale.
        /// </para>
        /// </summary>
        public ArcGraphFovDebugOverlayProducerDiagnostics FillCurrentConeSnapshot(
            World world,
            int npcId,
            bool useLineOfSight,
            bool includeWatchedMargin,
            ArcGraphDebugOverlaySnapshot snapshot,
            bool clearSnapshot = true)
        {
            if (snapshot == null)
                return new ArcGraphFovDebugOverlayProducerDiagnostics(false, npcId, 0, 0, "SnapshotMissing");

            if (clearSnapshot)
                snapshot.Clear();

            if (world == null)
                return new ArcGraphFovDebugOverlayProducerDiagnostics(false, npcId, 0, 0, "WorldMissing");

            if (npcId <= 0 || !world.ExistsNpc(npcId))
                return new ArcGraphFovDebugOverlayProducerDiagnostics(true, npcId, 0, 0, "NpcMissing");

            if (!world.GridPos.TryGetValue(npcId, out GridPosition origin))
                return new ArcGraphFovDebugOverlayProducerDiagnostics(true, npcId, 0, 0, "NpcPositionMissing");

            CardinalDirection facing = world.NpcFacing.TryGetValue(npcId, out CardinalDirection resolvedFacing)
                ? resolvedFacing
                : CardinalDirection.North;

            int visionRange = world.GetNpcPerceptionRangeCells(npcId);
            if (visionRange <= 0)
                return new ArcGraphFovDebugOverlayProducerDiagnostics(true, npcId, 0, 0, "VisionRangeMissing");

            bool useCone = world.GetNpcPerceptionUseCone(npcId);
            float coneSlope = world.GetNpcPerceptionConeSlope(npcId);
            int watchedMargin = includeWatchedMargin ? world.Global.PerceptionDirtyRadiusMarginCells : 0;
            if (watchedMargin < 0)
                watchedMargin = 0;

            int watchedRange = visionRange + watchedMargin;
            int observedCount = 0;
            int watchedCount = 0;

            for (int y = origin.Y - watchedRange; y <= origin.Y + watchedRange; y++)
            {
                for (int x = origin.X - watchedRange; x <= origin.X + watchedRange; x++)
                {
                    if (!world.InBounds(x, y))
                        continue;

                    int distance = FovUtils.Manhattan(origin.X, origin.Y, x, y);
                    if (distance <= 0 || distance > watchedRange)
                        continue;

                    bool observed = IsObservedFovCell(
                        world,
                        origin.X,
                        origin.Y,
                        facing,
                        x,
                        y,
                        visionRange,
                        useCone,
                        coneSlope,
                        useLineOfSight);

                    if (observed)
                    {
                        AddCell(snapshot, x, y, ArcGraphDebugOverlayKind.FovObservedCell, 1f, distance, ObservedColorKey);
                        observedCount++;
                        continue;
                    }

                    if (includeWatchedMargin
                        && IsWatchedMarginCell(
                            world,
                            origin.X,
                            origin.Y,
                            facing,
                            x,
                            y,
                            visionRange,
                            watchedMargin,
                            useCone,
                            coneSlope,
                            useLineOfSight))
                    {
                        AddCell(snapshot, x, y, ArcGraphDebugOverlayKind.FovWatchedMarginCell, 0.55f, distance, WatchedColorKey);
                        watchedCount++;
                    }
                }
            }

            return new ArcGraphFovDebugOverlayProducerDiagnostics(
                true,
                npcId,
                observedCount,
                watchedCount,
                observedCount + watchedCount > 0 ? "CurrentFovConeBuilt" : "CurrentFovConeEmpty");
        }

        private static bool IsObservedFovCell(
            World world,
            int originX,
            int originY,
            CardinalDirection facing,
            int x,
            int y,
            int visionRange,
            bool useCone,
            float coneSlope,
            bool useLineOfSight)
        {
            int distance = FovUtils.Manhattan(originX, originY, x, y);
            if (distance <= 0 || distance > visionRange)
                return false;

            if (useCone)
            {
                if (!FovUtils.IsInCone(originX, originY, facing, x, y, coneSlope))
                    return false;
            }
            else if (!FovUtils.IsInFront(originX, originY, facing, x, y))
            {
                return false;
            }

            return !useLineOfSight || world.HasLineOfSight(originX, originY, x, y);
        }

        private static bool IsWatchedMarginCell(
            World world,
            int originX,
            int originY,
            CardinalDirection facing,
            int x,
            int y,
            int visionRange,
            int watchedMargin,
            bool useCone,
            float coneSlope,
            bool useLineOfSight)
        {
            if (watchedMargin <= 0)
                return false;

            for (int dy = -watchedMargin; dy <= watchedMargin; dy++)
            {
                for (int dx = -watchedMargin; dx <= watchedMargin; dx++)
                {
                    if ((dx == 0 && dy == 0) || FovUtils.Manhattan(0, 0, dx, dy) > watchedMargin)
                        continue;

                    int observedX = x + dx;
                    int observedY = y + dy;
                    if (!world.InBounds(observedX, observedY))
                        continue;

                    if (IsObservedFovCell(
                            world,
                            originX,
                            originY,
                            facing,
                            observedX,
                            observedY,
                            visionRange,
                            useCone,
                            coneSlope,
                            useLineOfSight))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void AddCell(
            ArcGraphDebugOverlaySnapshot snapshot,
            int x,
            int y,
            ArcGraphDebugOverlayKind kind,
            float intensity01,
            int distance,
            string colorKey)
        {
            snapshot.AddCell(
                new ArcGraphDebugCellOverlaySnapshot(
                    new ArcGraphCellCoord(x, y, 0),
                    kind,
                    intensity01,
                    distance,
                    colorKey));
        }
    }
}
