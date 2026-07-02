using System;
using System.Collections.Generic;

namespace Arcontio.Core.Save
{
    public static class WorldSpatialAreaSaveBuilder
    {
        public static WorldSpatialAreaSaveData BuildFromWorld(World world)
        {
            if (world == null || world.SpatialAreas == null)
                return new WorldSpatialAreaSaveData();

            IReadOnlyList<WorldSpatialArea> areas = world.SpatialAreas.Areas;
            var areaDtos = new SpatialAreaSaveEntry[areas.Count];
            for (int i = 0; i < areas.Count; i++)
                areaDtos[i] = FromArea(areas[i]);

            IReadOnlyList<string> diagnostics = world.SpatialAreas.Diagnostics;
            var diagnosticDtos = new string[diagnostics.Count];
            for (int i = 0; i < diagnostics.Count; i++)
                diagnosticDtos[i] = diagnostics[i] ?? string.Empty;

            return new WorldSpatialAreaSaveData
            {
                areas = areaDtos,
                diagnostics = diagnosticDtos
            };
        }

        private static SpatialAreaSaveEntry FromArea(WorldSpatialArea area)
        {
            if (area == null)
                return new SpatialAreaSaveEntry();

            return new SpatialAreaSaveEntry
            {
                areaId = area.AreaId,
                kind = (int)area.Kind,
                ownerKind = (int)area.OwnerKind,
                ownerId = area.OwnerId,
                minX = area.MinX,
                minY = area.MinY,
                maxX = area.MaxX,
                maxY = area.MaxY,
                cells = FromCells(area.Cells)
            };
        }

        private static WorldSpatialAreaCellSaveData[] FromCells(WorldSpatialAreaCell[] cells)
        {
            if (cells == null || cells.Length == 0)
                return Array.Empty<WorldSpatialAreaCellSaveData>();

            var result = new WorldSpatialAreaCellSaveData[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                result[i] = new WorldSpatialAreaCellSaveData
                {
                    x = cells[i].X,
                    y = cells[i].Y
                };
            }

            return result;
        }
    }
}
