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

        public IReadOnlyList<WorldSpatialArea> Areas => _areas;
        public IReadOnlyList<string> Diagnostics => _diagnostics;
        public int AreaCount => _areas.Count;
        public int DiagnosticCount => _diagnostics.Count;

        public void Clear(int mapWidth, int mapHeight)
        {
            _areas.Clear();
            _areaById.Clear();
            _diagnostics.Clear();
            _mapWidth = Math.Max(0, mapWidth);
            _mapHeight = Math.Max(0, mapHeight);
            int size = _mapWidth * _mapHeight;
            _areaIdByCell = size > 0 ? new int[size] : Array.Empty<int>();
        }

        public void ReplaceAll(
            int mapWidth,
            int mapHeight,
            IReadOnlyList<WorldSpatialArea> areas,
            IReadOnlyList<string> diagnostics)
        {
            Clear(mapWidth, mapHeight);

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
        private readonly List<WorldSpatialAreaCell> _component = new(256);
        private readonly Queue<WorldSpatialAreaCell> _queue = new(256);
        private bool[] _visited;
        private bool[] _boundary;
        private int _width;
        private int _height;

        public IReadOnlyList<WorldSpatialArea> Areas => _areas;
        public IReadOnlyList<string> Diagnostics => _diagnostics;

        public void Build(World world, Config.SpatialAreaParams config)
        {
            _areas.Clear();
            _diagnostics.Clear();

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
                    _boundary[Index(x, y)] = world.IsSpatialAreaBoundaryAt(x, y);
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

                    WorldSpatialAreaKind kind = ClassifyComponent(config, touchesBorder, out bool invalidClosedArea);
                    if (invalidClosedArea)
                    {
                        _diagnostics.Add(
                            "InvalidClosedSpatialArea:cells=" + _component.Count +
                            " bounds=(" + minX + "," + minY + ")-(" + maxX + "," + maxY + ")");
                        continue;
                    }

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
            out bool invalidClosedArea)
        {
            invalidClosedArea = false;
            if (touchesMapBorder)
                return WorldSpatialAreaKind.OpenArea;

            int maxRoomSurface = config != null
                ? config.ResolveMaxClosedRoomSurfaceCells()
                : Config.SpatialAreaParams.DefaultMaxClosedRoomSurfaceCells;
            int corridorMaxWidth = config != null
                ? config.ResolveCorridorMaxWidthCells()
                : Config.SpatialAreaParams.DefaultCorridorMaxWidthCells;

            if (ResolveMaxLocalNarrowSpan() <= corridorMaxWidth)
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
