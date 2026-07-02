using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // SupportOpenSpaceLandmarkProvider
    // =============================================================================
    /// <summary>
    /// <para>
    /// Provider dei landmark oggettivi di supporto per le aree aperte.
    /// </para>
    ///
    /// <para><b>Principio architetturale: scaffolding navigazionale oggettivo</b></para>
    /// <para>
    /// Questi landmark non sono memoria soggettiva, non sono belief e non
    /// rappresentano risorse biologiche. Servono solo a garantire che lo spazio
    /// aperto abbia una rete minima di punti navigazionali quando i landmark
    /// strutturali esistenti non coprono abbastanza la mappa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Overlay input</b>: legge dal registry solo nodi attivi data-only.</item>
    ///   <item><b>Coverage-first</b>: seleziona iterativamente la cella valida piu' lontana dai landmark navigazionali.</item>
    ///   <item><b>Distanza minima</b>: non crea mai support troppo vicini a landmark navigazionali o support gia' accettati.</item>
    ///   <item><b>Debug</b>: pubblica nel World un riepilogo value-only dell'ultimo passaggio.</item>
    /// </list>
    /// </summary>
    public sealed class SupportOpenSpaceLandmarkProvider : IWorldLandmarkCoverageProvider
    {
        private readonly List<LandmarkOverlayNode> _existingNodes = new(256);
        private readonly List<LandmarkOverlayEdge> _scratchEdges = new(256);
        private readonly List<CoverageNode> _coverageNodes = new(256);
        private readonly List<CoverageNode> _acceptedSupportNodes = new(128);
        private readonly List<CandidateCell> _candidateCells = new(1024);

        public LandmarkProviderKind ProviderKind => LandmarkProviderKind.SupportOpenSpace;

        public int BuildLandmarkCandidates(
            World world,
            List<LandmarkRegistry.ManualLandmarkCandidate> outCandidates)
        {
            return 0;
        }

        public void ApplyLandmarkResolutions(
            IReadOnlyList<LandmarkRegistry.ManualLandmarkResolution> resolutions)
        {
        }

        public int BuildCoverageLandmarkCandidates(
            World world,
            LandmarkRegistry registry,
            List<LandmarkRegistry.ManualLandmarkCandidate> outCandidates)
        {
            if (world == null || registry == null || outCandidates == null || world.SpatialAreas == null)
                return 0;

            _existingNodes.Clear();
            _scratchEdges.Clear();
            _acceptedSupportNodes.Clear();
            registry.FillOverlayData(_existingNodes, _scratchEdges);

            int before = outCandidates.Count;
            int spacing = world.Config?.Sim?.spatial_areas != null
                ? world.Config.Sim.spatial_areas.ResolveSupportLandmarkSpacingCells(world.Global.NpcVisionRangeCells)
                : 1;
            if (spacing <= 0)
                spacing = 1;

            int openAreasProcessed = 0;
            int sourceLandmarks = 0;
            int candidateCells = 0;
            int iterations = 0;
            int maxResidual = 0;
            int rejectedBorder = 0;
            int rejectedOccupied = 0;

            IReadOnlyList<WorldSpatialArea> areas = world.SpatialAreas.Areas;
            for (int i = 0; i < areas.Count; i++)
            {
                WorldSpatialArea area = areas[i];
                if (area == null || area.Kind != WorldSpatialAreaKind.OpenArea || area.Cells == null)
                    continue;

                openAreasProcessed++;
                AddSupportCandidatesForArea(
                    world,
                    area,
                    spacing,
                    outCandidates,
                    ref sourceLandmarks,
                    ref candidateCells,
                    ref iterations,
                    ref maxResidual,
                    ref rejectedBorder,
                    ref rejectedOccupied);
            }

            int produced = outCandidates.Count - before;
            string reason = produced > 0
                ? "Generated"
                : openAreasProcessed <= 0
                    ? "NoOpenArea"
                    : candidateCells <= 0
                        ? "NoValidOpenAreaCandidateCells"
                        : "OpenAreaCoveredByNavigationalLandmarks";

            world.SetSupportOpenSpaceGenerationDebug(new WorldSupportLandmarkGenerationDebugSnapshot(
                openAreasProcessed,
                sourceLandmarks,
                candidateCells,
                iterations,
                produced,
                maxResidual,
                rejectedBorder,
                rejectedOccupied,
                reason));

            return produced;
        }

        private void AddSupportCandidatesForArea(
            World world,
            WorldSpatialArea area,
            int spacing,
            List<LandmarkRegistry.ManualLandmarkCandidate> outCandidates,
            ref int sourceLandmarks,
            ref int candidateCells,
            ref int iterations,
            ref int maxResidual,
            ref int rejectedBorder,
            ref int rejectedOccupied)
        {
            _coverageNodes.Clear();
            _acceptedSupportNodes.Clear();
            _candidateCells.Clear();

            CollectCoverageNodesForArea(world, area);
            sourceLandmarks += _coverageNodes.Count;
            CollectCandidateCells(world, area, ref rejectedBorder, ref rejectedOccupied);
            candidateCells += _candidateCells.Count;

            int guard = _candidateCells.Count + 1;
            while (guard-- > 0)
            {
                iterations++;
                int bestIndex = ResolveFarthestCandidateIndex(area, spacing, out int bestDistance);
                if (bestIndex < 0)
                    break;

                if (bestDistance <= spacing)
                {
                    if (bestDistance > maxResidual)
                        maxResidual = bestDistance;
                    break;
                }

                CandidateCell best = _candidateCells[bestIndex];
                if (!IsFarEnoughFromCoverage(best.X, best.Y, spacing))
                {
                    _candidateCells[bestIndex] = best.WithRejected();
                    continue;
                }

                var key = new LandmarkProviderKey(LandmarkProviderKind.SupportOpenSpace, area.AreaId);
                outCandidates.Add(new LandmarkRegistry.ManualLandmarkCandidate(
                    best.X,
                    best.Y,
                    LandmarkRegistry.LandmarkKind.SupportOpenSpaceAnchor,
                    1f,
                    key));

                _acceptedSupportNodes.Add(new CoverageNode(best.X, best.Y));
                _candidateCells[bestIndex] = best.WithRejected();
            }

            int residual = ResolveMaxResidualDistance(area);
            if (residual > maxResidual)
                maxResidual = residual;
        }

        private void CollectCoverageNodesForArea(World world, WorldSpatialArea area)
        {
            for (int i = 0; i < _existingNodes.Count; i++)
            {
                LandmarkOverlayNode node = _existingNodes[i];
                if (!IsNavigationalCoverageNode(node.Kind))
                    continue;

                if (!world.TryGetSpatialAreaAt(node.CellX, node.CellY, out WorldSpatialArea nodeArea)
                    || nodeArea == null
                    || nodeArea.AreaId != area.AreaId)
                {
                    continue;
                }

                _coverageNodes.Add(new CoverageNode(node.CellX, node.CellY));
            }
        }

        private void CollectCandidateCells(
            World world,
            WorldSpatialArea area,
            ref int rejectedBorder,
            ref int rejectedOccupied)
        {
            for (int i = 0; i < area.Cells.Length; i++)
            {
                WorldSpatialAreaCell cell = area.Cells[i];
                if (IsMapBorder(world, cell.X, cell.Y))
                {
                    rejectedBorder++;
                    continue;
                }

                if (world.BlocksMovementAt(cell.X, cell.Y) || world.IsSpatialAreaBoundaryAt(cell.X, cell.Y))
                    continue;

                if (HasActiveNodeAtCell(cell.X, cell.Y))
                {
                    rejectedOccupied++;
                    continue;
                }

                _candidateCells.Add(new CandidateCell(cell.X, cell.Y, false));
            }
        }

        private int ResolveFarthestCandidateIndex(WorldSpatialArea area, int spacing, out int bestDistance)
        {
            int bestIndex = -1;
            bestDistance = -1;

            for (int i = 0; i < _candidateCells.Count; i++)
            {
                CandidateCell candidate = _candidateCells[i];
                if (candidate.Rejected)
                    continue;

                int distance = ResolveDistanceToNearestCoverage(candidate.X, candidate.Y, area);
                if (distance <= spacing)
                    continue;

                if (distance > bestDistance
                    || distance == bestDistance && IsDeterministicallyBefore(candidate, _candidateCells[bestIndex]))
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private int ResolveDistanceToNearestCoverage(int x, int y, WorldSpatialArea area)
        {
            int best = int.MaxValue;
            for (int i = 0; i < _coverageNodes.Count; i++)
            {
                CoverageNode node = _coverageNodes[i];
                int distance = GridDistance(x, y, node.X, node.Y);
                if (distance < best)
                    best = distance;
            }

            for (int i = 0; i < _acceptedSupportNodes.Count; i++)
            {
                CoverageNode node = _acceptedSupportNodes[i];
                int distance = GridDistance(x, y, node.X, node.Y);
                if (distance < best)
                    best = distance;
            }

            if (best != int.MaxValue)
                return best;

            return DistanceFromAreaBounds(x, y, area);
        }

        private int ResolveMaxResidualDistance(WorldSpatialArea area)
        {
            int max = 0;
            for (int i = 0; i < _candidateCells.Count; i++)
            {
                CandidateCell candidate = _candidateCells[i];
                int distance = ResolveDistanceToNearestCoverage(candidate.X, candidate.Y, area);
                if (distance > max)
                    max = distance;
            }

            return max;
        }

        private bool IsFarEnoughFromCoverage(int x, int y, int spacing)
        {
            for (int i = 0; i < _coverageNodes.Count; i++)
            {
                CoverageNode node = _coverageNodes[i];
                if (GridDistance(x, y, node.X, node.Y) <= spacing)
                    return false;
            }

            for (int i = 0; i < _acceptedSupportNodes.Count; i++)
            {
                CoverageNode node = _acceptedSupportNodes[i];
                if (GridDistance(x, y, node.X, node.Y) <= spacing)
                    return false;
            }

            return true;
        }

        private bool HasActiveNodeAtCell(int x, int y)
        {
            for (int i = 0; i < _existingNodes.Count; i++)
            {
                LandmarkOverlayNode node = _existingNodes[i];
                if (node.CellX == x && node.CellY == y)
                    return true;
            }

            for (int i = 0; i < _acceptedSupportNodes.Count; i++)
            {
                CoverageNode node = _acceptedSupportNodes[i];
                if (node.X == x && node.Y == y)
                    return true;
            }

            return false;
        }

        private static bool IsNavigationalCoverageNode(int kind)
        {
            return kind == (int)LandmarkRegistry.LandmarkKind.Doorway
                || kind == (int)LandmarkRegistry.LandmarkKind.Junction
                || kind == (int)LandmarkRegistry.LandmarkKind.AreaCenter
                || kind == (int)LandmarkRegistry.LandmarkKind.SupportOpenSpaceAnchor;
        }

        private static bool IsMapBorder(World world, int x, int y)
        {
            return world == null
                || x <= 0
                || y <= 0
                || x >= world.MapWidth - 1
                || y >= world.MapHeight - 1;
        }

        private static int DistanceFromAreaBounds(int x, int y, WorldSpatialArea area)
        {
            int left = x - area.MinX;
            int right = area.MaxX - x;
            int bottom = y - area.MinY;
            int top = area.MaxY - y;
            int horizontal = left < right ? left : right;
            int vertical = bottom < top ? bottom : top;
            return horizontal < vertical ? horizontal : vertical;
        }

        private static int GridDistance(int ax, int ay, int bx, int by)
        {
            int dx = ax >= bx ? ax - bx : bx - ax;
            int dy = ay >= by ? ay - by : by - ay;
            return dx > dy ? dx : dy;
        }

        private static bool IsDeterministicallyBefore(CandidateCell candidate, CandidateCell current)
        {
            if (candidate.Y != current.Y)
                return candidate.Y < current.Y;

            return candidate.X < current.X;
        }

        private readonly struct CoverageNode
        {
            public readonly int X;
            public readonly int Y;

            public CoverageNode(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private readonly struct CandidateCell
        {
            public readonly int X;
            public readonly int Y;
            public readonly bool Rejected;

            public CandidateCell(int x, int y, bool rejected)
            {
                X = x;
                Y = y;
                Rejected = rejected;
            }

            public CandidateCell WithRejected()
            {
                return new CandidateCell(X, Y, true);
            }
        }
    }
}
