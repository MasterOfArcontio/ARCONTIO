using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // WorldSpatialAreaKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classificazione compatta delle aree fisico-spaziali del World.
    /// </para>
    ///
    /// <para><b>Principio architetturale: aree fisiche separate dalla Biosfera</b></para>
    /// <para>
    /// Questo enum non descrive aree ecologiche o biologiche. Descrive solo la
    /// topologia percorribile della mappa: spazio aperto, stanza chiusa o corridoio
    /// chiuso. La Biosfera continua a possedere <c>EnvironmentArea</c>; il World
    /// possiede invece queste aree spaziali.
    /// </para>
    /// </summary>
    public enum WorldSpatialAreaKind : byte
    {
        None = 0,
        OpenArea = 1,
        ClosedRoom = 2,
        Corridor = 3
    }

    // =============================================================================
    // WorldSpatialBoundaryKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classificazione diagnostica minima delle celle che chiudono un'area spaziale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: una sola regola boundary</b></para>
    /// <para>
    /// Il builder delle aree e il pannello debug devono leggere la stessa decisione
    /// prodotta dal World. Questo enum evita che ArcGraph o i test ricostruiscano
    /// in modo parallelo il significato di muro, porta o altro confine strutturale.
    /// </para>
    /// </summary>
    public enum WorldSpatialBoundaryKind : byte
    {
        None = 0,
        Wall = 1,
        Door = 2,
        Other = 3
    }

    // =============================================================================
    // WorldSpatialAreaCell
    // =============================================================================
    /// <summary>
    /// <para>
    /// Cella concreta appartenente a una <see cref="WorldSpatialArea"/>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: membership esplicita e serializzabile</b></para>
    /// <para>
    /// La membership viene salvata come coordinate leggere, non come riferimento a
    /// renderer, tile o strutture mutabili del World.
    /// </para>
    /// </summary>
    [Serializable]
    public struct WorldSpatialAreaCell
    {
        public int X;
        public int Y;

        public WorldSpatialAreaCell(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    // =============================================================================
    // WorldSpatialArea
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record data-only di una singola area fisico-spaziale del World.
    /// </para>
    ///
    /// <para><b>Principio architetturale: World owner della topologia fisica</b></para>
    /// <para>
    /// L'area non decide pathfinding, non crea landmark e non produce belief. E'
    /// una fotografia oggettiva della mappa su cui altri moduli possono basarsi in
    /// modo autorizzato.
    /// </para>
    /// </summary>
    public sealed class WorldSpatialArea
    {
        private static readonly WorldSpatialAreaCell[] EmptyCells = Array.Empty<WorldSpatialAreaCell>();

        public readonly int AreaId;
        public readonly WorldSpatialAreaKind Kind;
        public readonly OwnerKind OwnerKind;
        public readonly int OwnerId;
        public readonly int MinX;
        public readonly int MinY;
        public readonly int MaxX;
        public readonly int MaxY;
        public readonly int CellCount;
        public readonly WorldSpatialAreaCell[] Cells;

        public WorldSpatialArea(
            int areaId,
            WorldSpatialAreaKind kind,
            OwnerKind ownerKind,
            int ownerId,
            int minX,
            int minY,
            int maxX,
            int maxY,
            IReadOnlyList<WorldSpatialAreaCell> cells)
        {
            AreaId = areaId < 0 ? 0 : areaId;
            Kind = kind;
            OwnerKind = ownerKind == OwnerKind.Npc && ownerId > 0
                ? OwnerKind.Npc
                : ownerKind == OwnerKind.Group
                    ? OwnerKind.Group
                    : OwnerKind.Community;
            OwnerId = OwnerKind == OwnerKind.Npc || OwnerKind == OwnerKind.Group ? Math.Max(0, ownerId) : 0;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            Cells = CopyCells(cells);
            CellCount = Cells.Length;
        }

        private static WorldSpatialAreaCell[] CopyCells(IReadOnlyList<WorldSpatialAreaCell> cells)
        {
            if (cells == null || cells.Count == 0)
                return EmptyCells;

            var copy = new WorldSpatialAreaCell[cells.Count];
            for (int i = 0; i < cells.Count; i++)
                copy[i] = cells[i];

            return copy;
        }
    }

    // =============================================================================
    // WorldSpatialAreaOverlayCell
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO read-only per disegnare una cella dell'overlay AREA ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI consumer, World producer</b></para>
    /// <para>
    /// ArcGraph riceve gia' area id, kind e intensita' colore. La view non deve
    /// ricostruire flood-fill, non deve leggere oggetti e non deve decidere se una
    /// porta separa due aree.
    /// </para>
    /// </summary>
    public readonly struct WorldSpatialAreaOverlayCell
    {
        public readonly int AreaId;
        public readonly WorldSpatialAreaKind Kind;
        public readonly int X;
        public readonly int Y;
        public readonly float Intensity01;

        public WorldSpatialAreaOverlayCell(int areaId, WorldSpatialAreaKind kind, int x, int y, float intensity01)
        {
            AreaId = areaId;
            Kind = kind;
            X = x;
            Y = y;
            Intensity01 = intensity01 <= 0f ? 0f : intensity01 >= 1f ? 1f : intensity01;
        }
    }

    // =============================================================================
    // WorldSupportLandmarkDebugEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Riga read-only del pannello diagnostico Spatial/Support Landmark.
    /// </para>
    ///
    /// <para><b>Principio architetturale: debug UI da snapshot, non da registry mutabile</b></para>
    /// <para>
    /// Il pannello ArcGraph non deve scorrere direttamente il <c>LandmarkRegistry</c>.
    /// Il World produce invece righe value-only gia' risolte con area spaziale,
    /// lasciando alla view solo il compito di stamparle.
    /// </para>
    /// </summary>
    public readonly struct WorldSupportLandmarkDebugEntry
    {
        public readonly int NodeId;
        public readonly int CellX;
        public readonly int CellY;
        public readonly int AreaId;
        public readonly WorldSpatialAreaKind AreaKind;

        public WorldSupportLandmarkDebugEntry(
            int nodeId,
            int cellX,
            int cellY,
            int areaId,
            WorldSpatialAreaKind areaKind)
        {
            NodeId = nodeId;
            CellX = cellX;
            CellY = cellY;
            AreaId = areaId;
            AreaKind = areaKind;
        }
    }

    // =============================================================================
    // WorldSpatialAreaClassificationDebugEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Riga diagnostica compatta di una componente flood-fill classificata dal
    /// builder aree spaziali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: debug del processo, non solo del risultato</b></para>
    /// <para>
    /// Quando una stanza non viene riconosciuta, il solo conteggio finale non basta.
    /// Questa riga conserva i dati essenziali del passaggio di classificazione:
    /// bounds, superficie, contatto con bordo mappa, narrow span e risultato.
    /// </para>
    /// </summary>
    public readonly struct WorldSpatialAreaClassificationDebugEntry
    {
        public readonly int AreaId;
        public readonly WorldSpatialAreaKind Kind;
        public readonly int MinX;
        public readonly int MinY;
        public readonly int MaxX;
        public readonly int MaxY;
        public readonly int CellCount;
        public readonly bool TouchesMapBorder;
        public readonly int MaxNarrowSpan;
        public readonly bool IsInvalid;

        public WorldSpatialAreaClassificationDebugEntry(
            int areaId,
            WorldSpatialAreaKind kind,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int cellCount,
            bool touchesMapBorder,
            int maxNarrowSpan,
            bool isInvalid)
        {
            AreaId = areaId < 0 ? 0 : areaId;
            Kind = kind;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            CellCount = cellCount < 0 ? 0 : cellCount;
            TouchesMapBorder = touchesMapBorder;
            MaxNarrowSpan = maxNarrowSpan < 0 ? 0 : maxNarrowSpan;
            IsInvalid = isInvalid;
        }
    }

    // =============================================================================
    // WorldSpatialAreaBuildDebugSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica read-only prodotta dal builder aree spaziali durante l'ultimo
    /// rebuild.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica Core, visualizzazione ArcGraph</b></para>
    /// <para>
    /// I conteggi di boundary e flood-fill nascono dove avviene il calcolo reale.
    /// La UI riceve soltanto numeri e righe gia' normalizzate, senza ripetere il
    /// flood-fill e senza leggere oggetti o cataloghi.
    /// </para>
    /// </summary>
    public sealed class WorldSpatialAreaBuildDebugSnapshot
    {
        private static readonly WorldSpatialAreaClassificationDebugEntry[] EmptyClassifications =
            Array.Empty<WorldSpatialAreaClassificationDebugEntry>();

        public static readonly WorldSpatialAreaBuildDebugSnapshot Empty =
            new WorldSpatialAreaBuildDebugSnapshot(0, 0, 0, 0, 0, 0, 0, 0, 0, EmptyClassifications);

        public readonly int BoundaryWallCount;
        public readonly int BoundaryDoorCount;
        public readonly int BoundaryOtherCount;
        public readonly int BoundaryTotalCount;
        public readonly int FloodComponentCount;
        public readonly int FloodOpenComponentCount;
        public readonly int FloodClosedCandidateCount;
        public readonly int FloodClosedRoomCount;
        public readonly int FloodCorridorCount;
        public readonly int FloodInvalidClosedAreaCount;
        public readonly WorldSpatialAreaClassificationDebugEntry[] Classifications;

        public WorldSpatialAreaBuildDebugSnapshot(
            int boundaryWallCount,
            int boundaryDoorCount,
            int boundaryOtherCount,
            int floodComponentCount,
            int floodOpenComponentCount,
            int floodClosedCandidateCount,
            int floodClosedRoomCount,
            int floodCorridorCount,
            int floodInvalidClosedAreaCount,
            IReadOnlyList<WorldSpatialAreaClassificationDebugEntry> classifications)
        {
            BoundaryWallCount = boundaryWallCount < 0 ? 0 : boundaryWallCount;
            BoundaryDoorCount = boundaryDoorCount < 0 ? 0 : boundaryDoorCount;
            BoundaryOtherCount = boundaryOtherCount < 0 ? 0 : boundaryOtherCount;
            BoundaryTotalCount = BoundaryWallCount + BoundaryDoorCount + BoundaryOtherCount;
            FloodComponentCount = floodComponentCount < 0 ? 0 : floodComponentCount;
            FloodOpenComponentCount = floodOpenComponentCount < 0 ? 0 : floodOpenComponentCount;
            FloodClosedCandidateCount = floodClosedCandidateCount < 0 ? 0 : floodClosedCandidateCount;
            FloodClosedRoomCount = floodClosedRoomCount < 0 ? 0 : floodClosedRoomCount;
            FloodCorridorCount = floodCorridorCount < 0 ? 0 : floodCorridorCount;
            FloodInvalidClosedAreaCount = floodInvalidClosedAreaCount < 0 ? 0 : floodInvalidClosedAreaCount;
            Classifications = CopyClassifications(classifications);
        }

        private static WorldSpatialAreaClassificationDebugEntry[] CopyClassifications(
            IReadOnlyList<WorldSpatialAreaClassificationDebugEntry> classifications)
        {
            if (classifications == null || classifications.Count == 0)
                return EmptyClassifications;

            var copy = new WorldSpatialAreaClassificationDebugEntry[classifications.Count];
            for (int i = 0; i < classifications.Count; i++)
                copy[i] = classifications[i];

            return copy;
        }
    }

    // =============================================================================
    // WorldSupportLandmarkGenerationDebugSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica dell'ultimo passaggio coverage-first dei landmark SupportOpenSpace.
    /// </para>
    ///
    /// <para><b>Principio architetturale: provider isolato, debug value-only</b></para>
    /// <para>
    /// Il provider produce questi numeri mentre lavora, poi il World li espone come
    /// snapshot. ArcGraph non vede liste mutabili del provider e non ricalcola la
    /// copertura dei landmark.
    /// </para>
    /// </summary>
    public readonly struct WorldSupportLandmarkGenerationDebugSnapshot
    {
        public static readonly WorldSupportLandmarkGenerationDebugSnapshot Empty =
            new WorldSupportLandmarkGenerationDebugSnapshot(0, 0, 0, 0, 0, 0, 0, 0, "NotGenerated");

        public readonly int OpenAreasProcessed;
        public readonly int NavigationalSourceLandmarks;
        public readonly int CandidateCellsValidated;
        public readonly int FarthestIterations;
        public readonly int SupportAccepted;
        public readonly int MaxResidualDistanceCells;
        public readonly int RejectedBorderCells;
        public readonly int RejectedOccupiedCells;
        public readonly string LastReason;

        public WorldSupportLandmarkGenerationDebugSnapshot(
            int openAreasProcessed,
            int navigationalSourceLandmarks,
            int candidateCellsValidated,
            int farthestIterations,
            int supportAccepted,
            int maxResidualDistanceCells,
            int rejectedBorderCells,
            int rejectedOccupiedCells,
            string lastReason)
        {
            OpenAreasProcessed = openAreasProcessed < 0 ? 0 : openAreasProcessed;
            NavigationalSourceLandmarks = navigationalSourceLandmarks < 0 ? 0 : navigationalSourceLandmarks;
            CandidateCellsValidated = candidateCellsValidated < 0 ? 0 : candidateCellsValidated;
            FarthestIterations = farthestIterations < 0 ? 0 : farthestIterations;
            SupportAccepted = supportAccepted < 0 ? 0 : supportAccepted;
            MaxResidualDistanceCells = maxResidualDistanceCells < 0 ? 0 : maxResidualDistanceCells;
            RejectedBorderCells = rejectedBorderCells < 0 ? 0 : rejectedBorderCells;
            RejectedOccupiedCells = rejectedOccupiedCells < 0 ? 0 : rejectedOccupiedCells;
            LastReason = string.IsNullOrWhiteSpace(lastReason) ? string.Empty : lastReason;
        }
    }

    // =============================================================================
    // WorldSpatialAreaDebugSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only dello stato aree spaziali e landmark di supporto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica data-only autorizzata</b></para>
    /// <para>
    /// Questo DTO e' prodotto dal World e consumato da ArcGraph. Contiene solo
    /// conteggi, configurazione effettiva e righe gia' normalizzate; non contiene
    /// riferimenti a liste interne, provider o oggetti mutabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Conteggi aree</b>: totali per open, room, corridor e diagnostiche invalide.</item>
    ///   <item><b>Config support</b>: spacing, raggio e moltiplicatore effettivi.</item>
    ///   <item><b>SupportLandmarks</b>: elenco dei nodi S# finali presenti nel registry.</item>
    ///   <item><b>SupportLandmarkZeroReason</b>: motivo sintetico quando non esistono S#.</item>
    /// </list>
    /// </summary>
    public sealed class WorldSpatialAreaDebugSnapshot
    {
        private static readonly string[] EmptyDiagnostics = Array.Empty<string>();
        private static readonly WorldSupportLandmarkDebugEntry[] EmptySupportLandmarks =
            Array.Empty<WorldSupportLandmarkDebugEntry>();

        public readonly int TotalAreaCount;
        public readonly int OpenAreaCount;
        public readonly int ClosedRoomCount;
        public readonly int CorridorCount;
        public readonly int InvalidDiagnosticCount;
        public readonly int SupportLandmarkSpacingCells;
        public readonly int SupportLandmarkCoverageRadiusCells;
        public readonly int SupportLandmarkCoverageMultiplier;
        public readonly int SupportLandmarkCount;
        public readonly string SupportLandmarkZeroReason;
        public readonly WorldSpatialAreaBuildDebugSnapshot BuildDebug;
        public readonly WorldSupportLandmarkGenerationDebugSnapshot SupportGenerationDebug;
        public readonly string[] Diagnostics;
        public readonly WorldSupportLandmarkDebugEntry[] SupportLandmarks;
        public readonly WorldSpatialAreaClassificationDebugEntry[] Classifications;

        public bool HasErrors => InvalidDiagnosticCount > 0;

        public WorldSpatialAreaDebugSnapshot(
            int totalAreaCount,
            int openAreaCount,
            int closedRoomCount,
            int corridorCount,
            int invalidDiagnosticCount,
            int supportLandmarkSpacingCells,
            int supportLandmarkCoverageRadiusCells,
            int supportLandmarkCoverageMultiplier,
            int supportLandmarkCount,
            string supportLandmarkZeroReason,
            WorldSpatialAreaBuildDebugSnapshot buildDebug,
            WorldSupportLandmarkGenerationDebugSnapshot supportGenerationDebug,
            IReadOnlyList<string> diagnostics,
            IReadOnlyList<WorldSupportLandmarkDebugEntry> supportLandmarks,
            IReadOnlyList<WorldSpatialAreaClassificationDebugEntry> classifications)
        {
            TotalAreaCount = totalAreaCount < 0 ? 0 : totalAreaCount;
            OpenAreaCount = openAreaCount < 0 ? 0 : openAreaCount;
            ClosedRoomCount = closedRoomCount < 0 ? 0 : closedRoomCount;
            CorridorCount = corridorCount < 0 ? 0 : corridorCount;
            InvalidDiagnosticCount = invalidDiagnosticCount < 0 ? 0 : invalidDiagnosticCount;
            SupportLandmarkSpacingCells = supportLandmarkSpacingCells <= 0 ? 1 : supportLandmarkSpacingCells;
            SupportLandmarkCoverageRadiusCells = supportLandmarkCoverageRadiusCells < 0 ? 0 : supportLandmarkCoverageRadiusCells;
            SupportLandmarkCoverageMultiplier = supportLandmarkCoverageMultiplier <= 0 ? 1 : supportLandmarkCoverageMultiplier;
            SupportLandmarkCount = supportLandmarkCount < 0 ? 0 : supportLandmarkCount;
            SupportLandmarkZeroReason = string.IsNullOrWhiteSpace(supportLandmarkZeroReason)
                ? string.Empty
                : supportLandmarkZeroReason;
            BuildDebug = buildDebug ?? WorldSpatialAreaBuildDebugSnapshot.Empty;
            SupportGenerationDebug = supportGenerationDebug;
            Diagnostics = CopyDiagnostics(diagnostics);
            SupportLandmarks = CopySupportLandmarks(supportLandmarks);
            Classifications = CopyClassifications(classifications);
        }

        private static string[] CopyDiagnostics(IReadOnlyList<string> diagnostics)
        {
            if (diagnostics == null || diagnostics.Count == 0)
                return EmptyDiagnostics;

            var copy = new string[diagnostics.Count];
            for (int i = 0; i < diagnostics.Count; i++)
                copy[i] = diagnostics[i] ?? string.Empty;

            return copy;
        }

        private static WorldSupportLandmarkDebugEntry[] CopySupportLandmarks(
            IReadOnlyList<WorldSupportLandmarkDebugEntry> supportLandmarks)
        {
            if (supportLandmarks == null || supportLandmarks.Count == 0)
                return EmptySupportLandmarks;

            var copy = new WorldSupportLandmarkDebugEntry[supportLandmarks.Count];
            for (int i = 0; i < supportLandmarks.Count; i++)
                copy[i] = supportLandmarks[i];

            return copy;
        }

        private static WorldSpatialAreaClassificationDebugEntry[] CopyClassifications(
            IReadOnlyList<WorldSpatialAreaClassificationDebugEntry> classifications)
        {
            if (classifications == null || classifications.Count == 0)
                return Array.Empty<WorldSpatialAreaClassificationDebugEntry>();

            var copy = new WorldSpatialAreaClassificationDebugEntry[classifications.Count];
            for (int i = 0; i < classifications.Count; i++)
                copy[i] = classifications[i];

            return copy;
        }
    }

    // =============================================================================
    // WorldSpatialAreaState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Store autoritativo delle aree fisico-spaziali derivate dalla mappa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: store oggettivo con API read-only</b></para>
    /// <para>
    /// Il World possiede questo store, ma i consumer esterni ricevono solo query o
    /// snapshot value-only. Overlay, save/load e provider landmark usano quindi lo
    /// stesso punto di ingresso senza duplicare la topologia.
    /// </para>
    /// </summary>
    public sealed class WorldSpatialAreaState
    {
        private readonly List<WorldSpatialArea> _areas = new(32);
        private readonly Dictionary<int, WorldSpatialArea> _areaById = new(32);
        private readonly List<string> _diagnostics = new(8);
        private int[] _areaIdByCell = Array.Empty<int>();
        private int _mapWidth;
        private int _mapHeight;
        private WorldSpatialAreaBuildDebugSnapshot _buildDebug = WorldSpatialAreaBuildDebugSnapshot.Empty;

        public IReadOnlyList<WorldSpatialArea> Areas => _areas;
        public IReadOnlyList<string> Diagnostics => _diagnostics;
        public int AreaCount => _areas.Count;
        public int DiagnosticCount => _diagnostics.Count;
        public WorldSpatialAreaBuildDebugSnapshot BuildDebug => _buildDebug ?? WorldSpatialAreaBuildDebugSnapshot.Empty;

        public void Clear(int mapWidth, int mapHeight)
        {
            _areas.Clear();
            _areaById.Clear();
            _diagnostics.Clear();
            _mapWidth = Math.Max(0, mapWidth);
            _mapHeight = Math.Max(0, mapHeight);
            int size = _mapWidth * _mapHeight;
            _areaIdByCell = size > 0 ? new int[size] : Array.Empty<int>();
            _buildDebug = WorldSpatialAreaBuildDebugSnapshot.Empty;
        }

        public void ReplaceAll(
            int mapWidth,
            int mapHeight,
            IReadOnlyList<WorldSpatialArea> areas,
            IReadOnlyList<string> diagnostics,
            WorldSpatialAreaBuildDebugSnapshot buildDebug = null)
        {
            Clear(mapWidth, mapHeight);
            _buildDebug = buildDebug ?? WorldSpatialAreaBuildDebugSnapshot.Empty;

            if (diagnostics != null)
            {
                for (int i = 0; i < diagnostics.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(diagnostics[i]))
                        _diagnostics.Add(diagnostics[i]);
                }
            }

            if (areas == null)
                return;

            for (int i = 0; i < areas.Count; i++)
                AddArea(areas[i]);
        }

        public bool TryGetArea(int areaId, out WorldSpatialArea area)
        {
            return _areaById.TryGetValue(areaId, out area) && area != null;
        }

        public bool TryGetAreaAt(int x, int y, out WorldSpatialArea area)
        {
            area = null;
            if (!InBounds(x, y))
                return false;

            int areaId = _areaIdByCell[(y * _mapWidth) + x];
            return areaId > 0 && TryGetArea(areaId, out area);
        }

        public void FillOverlayCells(List<WorldSpatialAreaOverlayCell> outCells)
        {
            if (outCells == null)
                return;

            for (int areaIndex = 0; areaIndex < _areas.Count; areaIndex++)
            {
                WorldSpatialArea area = _areas[areaIndex];
                if (area == null || area.Cells == null)
                    continue;

                float intensity = ResolveOverlayIntensity(area.AreaId);
                for (int i = 0; i < area.Cells.Length; i++)
                {
                    WorldSpatialAreaCell cell = area.Cells[i];
                    outCells.Add(new WorldSpatialAreaOverlayCell(
                        area.AreaId,
                        area.Kind,
                        cell.X,
                        cell.Y,
                        intensity));
                }
            }
        }

        private void AddArea(WorldSpatialArea area)
        {
            if (area == null || area.AreaId <= 0 || area.Kind == WorldSpatialAreaKind.None)
                return;

            if (_areaById.ContainsKey(area.AreaId))
            {
                _diagnostics.Add("DuplicateSpatialAreaId:" + area.AreaId);
                return;
            }

            _areas.Add(area);
            _areaById[area.AreaId] = area;

            for (int i = 0; i < area.Cells.Length; i++)
            {
                WorldSpatialAreaCell cell = area.Cells[i];
                if (!InBounds(cell.X, cell.Y))
                    continue;

                _areaIdByCell[(cell.Y * _mapWidth) + cell.X] = area.AreaId;
            }
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _mapWidth && y < _mapHeight;
        }

        private static float ResolveOverlayIntensity(int areaId)
        {
            int slot = Math.Abs(areaId * 37) % 7;
            return 0.30f + (slot * 0.07f);
        }
    }

    // =============================================================================
    // WorldSpatialAreaBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder deterministico delle aree fisico-spaziali del World.
    /// </para>
    ///
    /// <para><b>Principio architetturale: topologia fisica senza Biosfera</b></para>
    /// <para>
    /// Il builder guarda solo il confine spaziale esposto dal World. Le piante e la
    /// vegetazione non chiudono aree; muri e porte si'. Il risultato e' uno store
    /// oggettivo che altri moduli possono interrogare senza rifare il flood-fill.
    /// </para>
    /// </summary>
    public sealed class WorldSpatialAreaBuilder
    {
        private static readonly int[] Dx = { 1, -1, 0, 0 };
        private static readonly int[] Dy = { 0, 0, 1, -1 };

        private readonly List<WorldSpatialArea> _areas = new(32);
        private readonly List<string> _diagnostics = new(8);
        private readonly List<WorldSpatialAreaClassificationDebugEntry> _classificationDebug = new(32);
        private readonly List<WorldSpatialAreaCell> _component = new(256);
        private readonly Queue<WorldSpatialAreaCell> _queue = new(256);
        private bool[] _visited;
        private bool[] _boundary;
        private int _width;
        private int _height;
        private int _boundaryWallCount;
        private int _boundaryDoorCount;
        private int _boundaryOtherCount;
        private int _floodComponentCount;
        private int _floodOpenComponentCount;
        private int _floodClosedCandidateCount;
        private int _floodClosedRoomCount;
        private int _floodCorridorCount;
        private int _floodInvalidClosedAreaCount;

        public IReadOnlyList<WorldSpatialArea> Areas => _areas;
        public IReadOnlyList<string> Diagnostics => _diagnostics;
        public WorldSpatialAreaBuildDebugSnapshot BuildDebug => new WorldSpatialAreaBuildDebugSnapshot(
            _boundaryWallCount,
            _boundaryDoorCount,
            _boundaryOtherCount,
            _floodComponentCount,
            _floodOpenComponentCount,
            _floodClosedCandidateCount,
            _floodClosedRoomCount,
            _floodCorridorCount,
            _floodInvalidClosedAreaCount,
            _classificationDebug);

        public void Build(World world, Config.SpatialAreaParams config)
        {
            _areas.Clear();
            _diagnostics.Clear();
            _classificationDebug.Clear();
            _boundaryWallCount = 0;
            _boundaryDoorCount = 0;
            _boundaryOtherCount = 0;
            _floodComponentCount = 0;
            _floodOpenComponentCount = 0;
            _floodClosedCandidateCount = 0;
            _floodClosedRoomCount = 0;
            _floodCorridorCount = 0;
            _floodInvalidClosedAreaCount = 0;

            if (world == null)
                return;

            _width = world.MapWidth;
            _height = world.MapHeight;
            int size = _width * _height;
            _visited = size > 0 ? new bool[size] : Array.Empty<bool>();
            _boundary = size > 0 ? new bool[size] : Array.Empty<bool>();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    WorldSpatialBoundaryKind boundaryKind = world.ResolveSpatialAreaBoundaryKindAt(x, y);
                    _boundary[Index(x, y)] = boundaryKind != WorldSpatialBoundaryKind.None;
                    if (boundaryKind == WorldSpatialBoundaryKind.Wall)
                        _boundaryWallCount++;
                    else if (boundaryKind == WorldSpatialBoundaryKind.Door)
                        _boundaryDoorCount++;
                    else if (boundaryKind == WorldSpatialBoundaryKind.Other)
                        _boundaryOtherCount++;
                }
            }

            int nextAreaId = 1;
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int idx = Index(x, y);
                    if (_visited[idx] || _boundary[idx])
                        continue;

                    if (!BuildComponent(x, y, out bool touchesBorder, out int minX, out int minY, out int maxX, out int maxY))
                        continue;

                    _floodComponentCount++;
                    WorldSpatialAreaKind kind = ClassifyComponent(config, touchesBorder, out bool invalidClosedArea, out int maxNarrowSpan);
                    if (touchesBorder)
                        _floodOpenComponentCount++;
                    else
                        _floodClosedCandidateCount++;

                    if (invalidClosedArea)
                    {
                        _floodInvalidClosedAreaCount++;
                        _diagnostics.Add(
                            "InvalidClosedSpatialArea:cells=" + _component.Count +
                            " bounds=(" + minX + "," + minY + ")-(" + maxX + "," + maxY + ")");
                        _classificationDebug.Add(new WorldSpatialAreaClassificationDebugEntry(
                            0,
                            WorldSpatialAreaKind.None,
                            minX,
                            minY,
                            maxX,
                            maxY,
                            _component.Count,
                            touchesBorder,
                            maxNarrowSpan,
                            true));
                        continue;
                    }

                    if (kind == WorldSpatialAreaKind.ClosedRoom)
                        _floodClosedRoomCount++;
                    else if (kind == WorldSpatialAreaKind.Corridor)
                        _floodCorridorCount++;

                    _classificationDebug.Add(new WorldSpatialAreaClassificationDebugEntry(
                        nextAreaId,
                        kind,
                        minX,
                        minY,
                        maxX,
                        maxY,
                        _component.Count,
                        touchesBorder,
                        maxNarrowSpan,
                        false));

                    _areas.Add(new WorldSpatialArea(
                        nextAreaId++,
                        kind,
                        OwnerKind.Community,
                        0,
                        minX,
                        minY,
                        maxX,
                        maxY,
                        _component));
                }
            }
        }

        private bool BuildComponent(
            int startX,
            int startY,
            out bool touchesMapBorder,
            out int minX,
            out int minY,
            out int maxX,
            out int maxY)
        {
            _component.Clear();
            _queue.Clear();
            touchesMapBorder = false;
            minX = maxX = startX;
            minY = maxY = startY;

            _visited[Index(startX, startY)] = true;
            _queue.Enqueue(new WorldSpatialAreaCell(startX, startY));

            while (_queue.Count > 0)
            {
                WorldSpatialAreaCell cell = _queue.Dequeue();
                _component.Add(cell);

                if (cell.X == 0 || cell.Y == 0 || cell.X == _width - 1 || cell.Y == _height - 1)
                    touchesMapBorder = true;

                if (cell.X < minX) minX = cell.X;
                if (cell.Y < minY) minY = cell.Y;
                if (cell.X > maxX) maxX = cell.X;
                if (cell.Y > maxY) maxY = cell.Y;

                for (int d = 0; d < 4; d++)
                {
                    int nx = cell.X + Dx[d];
                    int ny = cell.Y + Dy[d];
                    if (!InBounds(nx, ny))
                        continue;

                    int nIdx = Index(nx, ny);
                    if (_visited[nIdx] || _boundary[nIdx])
                        continue;

                    _visited[nIdx] = true;
                    _queue.Enqueue(new WorldSpatialAreaCell(nx, ny));
                }
            }

            return _component.Count > 0;
        }

        private WorldSpatialAreaKind ClassifyComponent(
            Config.SpatialAreaParams config,
            bool touchesMapBorder,
            out bool invalidClosedArea,
            out int maxNarrowSpan)
        {
            invalidClosedArea = false;
            maxNarrowSpan = 0;
            if (touchesMapBorder)
                return WorldSpatialAreaKind.OpenArea;

            int maxRoomSurface = config != null
                ? config.ResolveMaxClosedRoomSurfaceCells()
                : Config.SpatialAreaParams.DefaultMaxClosedRoomSurfaceCells;
            int corridorMaxWidth = config != null
                ? config.ResolveCorridorMaxWidthCells()
                : Config.SpatialAreaParams.DefaultCorridorMaxWidthCells;

            maxNarrowSpan = ResolveMaxLocalNarrowSpan();
            if (maxNarrowSpan <= corridorMaxWidth)
                return WorldSpatialAreaKind.Corridor;

            if (_component.Count <= maxRoomSurface)
                return WorldSpatialAreaKind.ClosedRoom;

            invalidClosedArea = true;
            return WorldSpatialAreaKind.None;
        }

        private int ResolveMaxLocalNarrowSpan()
        {
            int maxNarrowSpan = 0;
            for (int i = 0; i < _component.Count; i++)
            {
                WorldSpatialAreaCell cell = _component[i];
                int horizontal = CountOpenSpan(cell.X, cell.Y, 1, 0) + CountOpenSpan(cell.X, cell.Y, -1, 0) + 1;
                int vertical = CountOpenSpan(cell.X, cell.Y, 0, 1) + CountOpenSpan(cell.X, cell.Y, 0, -1) + 1;
                int narrow = horizontal < vertical ? horizontal : vertical;
                if (narrow > maxNarrowSpan)
                    maxNarrowSpan = narrow;
            }

            return maxNarrowSpan;
        }

        private int CountOpenSpan(int startX, int startY, int dx, int dy)
        {
            int count = 0;
            int x = startX + dx;
            int y = startY + dy;

            while (InBounds(x, y) && !_boundary[Index(x, y)])
            {
                count++;
                x += dx;
                y += dy;
            }

            return count;
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        private int Index(int x, int y)
        {
            return (y * _width) + x;
        }
    }
}
