using System.Collections.Generic;

namespace Arcontio.Core
{
    public sealed class SupportOpenSpaceLandmarkProvider : IWorldLandmarkCoverageProvider
    {
        private readonly List<LandmarkOverlayNode> _existingNodes = new(256);
        private readonly List<LandmarkOverlayEdge> _scratchEdges = new(256);
        private readonly List<LandmarkOverlayNode> _acceptedSupportNodes = new(128);

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
            int radius = world.Config?.Sim?.spatial_areas != null
                ? world.Config.Sim.spatial_areas.ResolveSupportLandmarkCoverageRadiusCells(world.Global.NpcVisionRangeCells)
                : spacing * 2;
            if (spacing <= 0)
                spacing = 1;
            if (radius < spacing)
                radius = spacing;

            IReadOnlyList<WorldSpatialArea> areas = world.SpatialAreas.Areas;
            for (int i = 0; i < areas.Count; i++)
            {
                WorldSpatialArea area = areas[i];
                if (area == null || area.Kind != WorldSpatialAreaKind.OpenArea || area.Cells == null)
                    continue;

                AddSupportCandidatesForArea(area, spacing, radius, outCandidates);
            }

            return outCandidates.Count - before;
        }

        private void AddSupportCandidatesForArea(
            WorldSpatialArea area,
            int spacing,
            int radius,
            List<LandmarkRegistry.ManualLandmarkCandidate> outCandidates)
        {
            for (int i = 0; i < area.Cells.Length; i++)
            {
                WorldSpatialAreaCell cell = area.Cells[i];
                if (((cell.X - area.MinX) % spacing) != 0 || ((cell.Y - area.MinY) % spacing) != 0)
                    continue;

                if (IsCovered(cell.X, cell.Y, radius))
                    continue;

                var key = new LandmarkProviderKey(LandmarkProviderKind.SupportOpenSpace, area.AreaId);
                outCandidates.Add(new LandmarkRegistry.ManualLandmarkCandidate(
                    cell.X,
                    cell.Y,
                    LandmarkRegistry.LandmarkKind.SupportOpenSpaceAnchor,
                    1f,
                    key));

                _acceptedSupportNodes.Add(new LandmarkOverlayNode(
                    cell.X,
                    cell.Y,
                    (int)LandmarkRegistry.LandmarkKind.SupportOpenSpaceAnchor,
                    area.AreaId,
                    string.Empty));
            }
        }

        private bool IsCovered(int x, int y, int radius)
        {
            for (int i = 0; i < _existingNodes.Count; i++)
            {
                LandmarkOverlayNode node = _existingNodes[i];
                if (GridDistance(x, y, node.CellX, node.CellY) <= radius)
                    return true;
            }

            for (int i = 0; i < _acceptedSupportNodes.Count; i++)
            {
                LandmarkOverlayNode node = _acceptedSupportNodes[i];
                if (GridDistance(x, y, node.CellX, node.CellY) <= radius)
                    return true;
            }

            return false;
        }

        private static int GridDistance(int ax, int ay, int bx, int by)
        {
            int dx = ax >= bx ? ax - bx : bx - ax;
            int dy = ay >= by ? ay - by : by - ay;
            return dx > dy ? dx : dy;
        }
    }
}
