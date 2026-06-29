using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDoorVisualState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato visuale normalizzato di una porta ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato porta copiato, non deciso dalla UI</b></para>
    /// <para>
    /// Questo enum non apre, chiude o blocca porte. Rappresenta soltanto la lettura
    /// value-only dello stato gia' autorizzato dal <c>World</c>, cosi' il renderer
    /// puo' scegliere una slice grafica senza introdurre una seconda authority.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: oggetto non porta o stato assente.</item>
    ///   <item><b>Closed</b>: porta chiusa non locked.</item>
    ///   <item><b>Open</b>: porta aperta.</item>
    ///   <item><b>Locked</b>: porta chiusa con lock visuale.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphDoorVisualState
    {
        None = 0,
        Closed = 1,
        Open = 2,
        Locked = 3
    }

    // =============================================================================
    // ArcGraphDoorVisualOrientation
    // =============================================================================
    /// <summary>
    /// <para>
    /// Orientamento visuale derivato per una porta ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: orientamento visual-only</b></para>
    /// <para>
    /// L'orientamento non viene salvato nel <c>WorldObjectInstance</c>. Viene dedotto
    /// dalla topologia visuale attorno alla porta e serve solo a scegliere fra slice
    /// orizzontali e verticali dello spritesheet.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Horizontal</b>: usa le slice <c>horizontal_*</c>.</item>
    ///   <item><b>Vertical</b>: usa le slice <c>vertical_*</c>.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphDoorVisualOrientation
    {
        Horizontal = 0,
        Vertical = 1
    }

    // =============================================================================
    // ArcGraphDoorVisualResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver passivo delle varianti visuali porta ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: porta visuale senza nuova simulazione</b></para>
    /// <para>
    /// Il resolver riceve solo snapshot oggetto gia' copiati dal boundary ArcGraph.
    /// Non legge <c>World</c>, non apre porte, non modifica lock, non carica sprite
    /// Unity e non decide blocchi fisici. Produce soltanto la sprite key sliced
    /// corretta e i due valori visuali necessari al renderer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildSolidDoorContextCellIndex</b>: indicizza muri e porte vicine.</item>
    ///   <item><b>ResolveSpriteKey</b>: sceglie una slice porta 32x83.</item>
    ///   <item><b>ResolveOrientation</b>: deduce orizzontale/verticale dalla topologia.</item>
    ///   <item><b>ResolveState</b>: traduce <c>IsOpen/IsLocked</c> in stato visuale.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphDoorVisualResolver
    {
        private const string DoorVisualKind = "door";
        private const char SpriteSheetSeparator = '#';

        public static HashSet<ArcGraphCellCoord> BuildSolidDoorContextCellIndex(
            IReadOnlyList<ArcGraphObjectVisualSnapshot> snapshots)
        {
            var cells = new HashSet<ArcGraphCellCoord>();
            if (snapshots == null)
                return cells;

            for (int i = 0; i < snapshots.Count; i++)
            {
                ArcGraphObjectVisualSnapshot snapshot = snapshots[i];
                if (!IsDoorSnapshot(snapshot) && !ArcGraphWallCardinalResolver.IsWallSnapshot(snapshot))
                    continue;

                cells.Add(snapshot.Cell);
            }

            return cells;
        }

        public static string ResolveSpriteKey(
            ArcGraphObjectVisualSnapshot snapshot,
            HashSet<ArcGraphCellCoord> solidDoorContextCells,
            out ArcGraphDoorVisualState state,
            out ArcGraphDoorVisualOrientation orientation)
        {
            state = ResolveState(snapshot);
            orientation = ResolveOrientation(snapshot, solidDoorContextCells);

            string baseSpriteKey = snapshot.SpriteKey ?? string.Empty;
            if (!IsDoorSnapshot(snapshot) || string.IsNullOrWhiteSpace(baseSpriteKey))
                return baseSpriteKey;

            string sliceName = ResolveSliceName(state, orientation);
            if (string.IsNullOrWhiteSpace(sliceName))
                return baseSpriteKey;

            return baseSpriteKey + SpriteSheetSeparator + sliceName;
        }

        public static bool IsDoorSnapshot(ArcGraphObjectVisualSnapshot snapshot)
        {
            if (snapshot.IsDoor)
                return true;

            return string.Equals(
                snapshot.VisualKind ?? string.Empty,
                DoorVisualKind,
                System.StringComparison.OrdinalIgnoreCase);
        }

        public static ArcGraphDoorVisualState ResolveState(
            ArcGraphObjectVisualSnapshot snapshot)
        {
            if (!IsDoorSnapshot(snapshot))
                return ArcGraphDoorVisualState.None;

            if (snapshot.IsDoorLocked)
                return ArcGraphDoorVisualState.Locked;

            return snapshot.IsDoorOpen
                ? ArcGraphDoorVisualState.Open
                : ArcGraphDoorVisualState.Closed;
        }

        public static ArcGraphDoorVisualOrientation ResolveOrientation(
            ArcGraphObjectVisualSnapshot snapshot,
            HashSet<ArcGraphCellCoord> solidDoorContextCells)
        {
            if (solidDoorContextCells == null || solidDoorContextCells.Count == 0)
                return ArcGraphDoorVisualOrientation.Horizontal;

            ArcGraphCellCoord cell = snapshot.Cell;
            int horizontal = CountNeighbor(solidDoorContextCells, cell.X - 1, cell.Y, cell.Z)
                             + CountNeighbor(solidDoorContextCells, cell.X + 1, cell.Y, cell.Z);
            int vertical = CountNeighbor(solidDoorContextCells, cell.X, cell.Y - 1, cell.Z)
                           + CountNeighbor(solidDoorContextCells, cell.X, cell.Y + 1, cell.Z);

            if (vertical > horizontal)
                return ArcGraphDoorVisualOrientation.Vertical;

            return ArcGraphDoorVisualOrientation.Horizontal;
        }

        private static int CountNeighbor(
            HashSet<ArcGraphCellCoord> cells,
            int x,
            int y,
            int z)
        {
            return cells.Contains(new ArcGraphCellCoord(x, y, z)) ? 1 : 0;
        }

        private static string ResolveSliceName(
            ArcGraphDoorVisualState state,
            ArcGraphDoorVisualOrientation orientation)
        {
            bool vertical = orientation == ArcGraphDoorVisualOrientation.Vertical;
            if (state == ArcGraphDoorVisualState.Open)
                return vertical ? "vertical_open" : "horizontal_open";

            if (state == ArcGraphDoorVisualState.Closed || state == ArcGraphDoorVisualState.Locked)
                return vertical ? "vertical_close" : "horizontal_close";

            return string.Empty;
        }
    }

    // =============================================================================
    // ArcGraphObjectRenderQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot oggetto ArcGraph in render item
    /// ordinabili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: queue oggetti senza asset e senza scena</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphObjectLayer</c> e un profilo LOD gia'
    /// risolto. Produce <c>ArcGraphObjectRenderItem</c>, ma non crea sprite, non
    /// carica asset, non legge il <c>World</c>, non modifica stock e non decide
    /// ownership. E' un traduttore deterministico tra cache visuale e futuro
    /// wrapper renderer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola una lista di item oggetto ordinati.</item>
    ///   <item><b>CreateItem</b>: converte uno snapshot in item value-only.</item>
    ///   <item><b>CompareObjects</b>: sorting deterministico via sort key.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphObjectRenderQueueBuilder
    {
        private const int ObjectVisualLayerOrder = 10;

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue oggetti a partire dal layer oggetti ArcGraph.
        /// </para>
        ///
        /// <para><b>Output controllato</b></para>
        /// <para>
        /// Il metodo usa una lista temporanea di snapshot copiati dal layer, poi
        /// produce item renderizzabili nel target. Gli item nascosti possono essere
        /// esclusi dal target ma restano contati nella diagnostica, cosi' il QA puo'
        /// capire perche' un oggetto non comparirebbe.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>objectLayer</b>: cache snapshot da leggere.</item>
        ///   <item><b>lodProfile</b>: policy visuale gia' risolta.</item>
        ///   <item><b>target</b>: lista render item da popolare.</item>
        ///   <item><b>includeHiddenItems</b>: se true, conserva anche item nascosti.</item>
        /// </list>
        /// </summary>
        public ArcGraphRenderQueueDiagnostics Build(
            ArcGraphObjectLayer objectLayer,
            ArcGraphZoomLodProfile lodProfile,
            IList<ArcGraphObjectRenderItem> target,
            bool clearTarget = true,
            bool includeHiddenItems = false)
        {
            if (target == null)
            {
                return new ArcGraphRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    "TargetMissing");
            }

            if (clearTarget)
                target.Clear();

            if (objectLayer == null)
            {
                return new ArcGraphRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    "ObjectLayerMissing");
            }

            var snapshots = new List<ArcGraphObjectVisualSnapshot>();
            objectLayer.CopySnapshotsTo(snapshots);
            Dictionary<string, HashSet<ArcGraphCellCoord>> wallCellsByFamily =
                ArcGraphWallCardinalResolver.BuildWallCellIndex(snapshots);
            AddDoorConnectorCellsToWallFamilies(snapshots, wallCellsByFamily);
            HashSet<ArcGraphCellCoord> doorContextCells =
                ArcGraphDoorVisualResolver.BuildSolidDoorContextCellIndex(snapshots);

            int visibleCount = 0;
            int hiddenCount = 0;

            for (int i = 0; i < snapshots.Count; i++)
            {
                ArcGraphObjectRenderItem item = CreateItem(
                    snapshots[i],
                    lodProfile,
                    wallCellsByFamily,
                    doorContextCells);

                if (item.IsVisible)
                    visibleCount++;
                else
                    hiddenCount++;

                if (item.IsVisible || includeHiddenItems)
                    target.Add(item);
            }

            Sort(target);

            return new ArcGraphRenderQueueDiagnostics(
                0,
                snapshots.Count,
                visibleCount,
                hiddenCount,
                "ObjectQueueBuilt");
        }

        // =============================================================================
        // AddDoorConnectorCellsToWallFamilies
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge le celle porta agli indici dei muri come connettori visuali.
        /// </para>
        ///
        /// <para><b>Principio architetturale: continuita' grafica senza mutazione fisica</b></para>
        /// <para>
        /// Le porte restano oggetti porta e mantengono sprite, stato open/close/lock
        /// e cache fisiche gestite dal <c>World</c>. Qui vengono copiate nelle sole
        /// mappe usate dal resolver muri, cosi' un muro accanto a una porta non
        /// sceglie la variante terminale ma quella di prosecuzione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Filtro porte</b>: usa il resolver porta visual-only.</item>
        ///   <item><b>Famiglie muro</b>: la porta collega tutte le famiglie presenti.</item>
        ///   <item><b>Nessun World</b>: lavora solo su snapshot ArcGraph.</item>
        /// </list>
        /// </summary>
        private static void AddDoorConnectorCellsToWallFamilies(
            IReadOnlyList<ArcGraphObjectVisualSnapshot> snapshots,
            Dictionary<string, HashSet<ArcGraphCellCoord>> wallCellsByFamily)
        {
            if (snapshots == null || wallCellsByFamily == null || wallCellsByFamily.Count == 0)
                return;

            for (int i = 0; i < snapshots.Count; i++)
            {
                ArcGraphObjectVisualSnapshot snapshot = snapshots[i];
                if (!ArcGraphDoorVisualResolver.IsDoorSnapshot(snapshot))
                    continue;

                foreach (var pair in wallCellsByFamily)
                    pair.Value.Add(snapshot.Cell);
            }
        }

        private static ArcGraphObjectRenderItem CreateItem(
            ArcGraphObjectVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile,
            IReadOnlyDictionary<string, HashSet<ArcGraphCellCoord>> wallCellsByFamily,
            HashSet<ArcGraphCellCoord> doorContextCells)
        {
            bool isVisible = true;
            string hiddenReason = "None";
            string spriteKey = ArcGraphWallCardinalResolver.ResolveSpriteKey(
                snapshot,
                wallCellsByFamily);
            ArcGraphDoorVisualState doorVisualState = ArcGraphDoorVisualState.None;
            ArcGraphDoorVisualOrientation doorVisualOrientation = ArcGraphDoorVisualOrientation.Horizontal;

            if (ArcGraphDoorVisualResolver.IsDoorSnapshot(snapshot))
            {
                spriteKey = ArcGraphDoorVisualResolver.ResolveSpriteKey(
                    snapshot,
                    doorContextCells,
                    out doorVisualState,
                    out doorVisualOrientation);
            }

            if (snapshot.ObjectId <= 0)
            {
                isVisible = false;
                hiddenReason = "InvalidObjectId";
            }
            else if (snapshot.IsHeld)
            {
                isVisible = false;
                hiddenReason = "HeldObject";
            }
            else if (string.IsNullOrWhiteSpace(spriteKey))
            {
                isVisible = false;
                hiddenReason = "MissingSpriteKey";
            }

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.Cell,
                ObjectVisualLayerOrder,
                ArcGraphRenderItemKind.Object,
                snapshot.ObjectId);

            return new ArcGraphObjectRenderItem(
                snapshot.ObjectId,
                snapshot.DefId,
                snapshot.Cell,
                spriteKey,
                lodProfile.ObjectMode,
                lodProfile.ShowMinorItems,
                snapshot.IsHeld,
                snapshot.HolderActorId,
                snapshot.FoodStockUnits,
                snapshot.FootprintWidth,
                snapshot.FootprintHeight,
                snapshot.VisualKind,
                snapshot.VisualResolverKey,
                snapshot.VisualWidthPixels,
                snapshot.VisualHeightPixels,
                snapshot.VisualBaseWidthPixels,
                snapshot.VisualBaseHeightPixels,
                snapshot.VisualBaseMiniTileMask,
                snapshot.VisualPivot,
                snapshot.VisualOffsetX,
                snapshot.VisualOffsetY,
                snapshot.FadeWhenActorBehind,
                snapshot.UseShadow,
                isVisible,
                hiddenReason,
                sortKey,
                ArcGraphDoorVisualResolver.IsDoorSnapshot(snapshot),
                snapshot.IsDoorOpen,
                snapshot.IsDoorLocked,
                snapshot.IsDoorLockable,
                doorVisualState,
                doorVisualOrientation);
        }

        private static void Sort(IList<ArcGraphObjectRenderItem> target)
        {
            if (target == null || target.Count <= 1)
                return;

            if (target is List<ArcGraphObjectRenderItem> list)
            {
                list.Sort(CompareObjects);
                return;
            }

            var copy = new List<ArcGraphObjectRenderItem>(target);
            copy.Sort(CompareObjects);

            target.Clear();
            for (int i = 0; i < copy.Count; i++)
            {
                target.Add(copy[i]);
            }
        }

        private static int CompareObjects(
            ArcGraphObjectRenderItem left,
            ArcGraphObjectRenderItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }
    }
}
