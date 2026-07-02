using System;
using System.Collections.Generic;

namespace Arcontio.Core.Save
{
    public static class WorldSpatialAreaSaveLoader
    {
        public static bool CanApplySpatialAreasSafely(World world, WorldSaveData data, out string error)
        {
            if (world == null)
            {
                error = "WorldSpatialAreaSaveLoader: world nullo.";
                return false;
            }

            if (data == null)
            {
                error = "WorldSpatialAreaSaveLoader: WorldSaveData nullo.";
                return false;
            }

            WorldSpatialAreaSaveData spatial = data.spatialAreas;
            if (spatial == null || spatial.areas == null || spatial.areas.Length == 0)
            {
                error = string.Empty;
                return true;
            }

            var areaIds = new HashSet<int>();
            var occupiedCells = new HashSet<int>();
            for (int i = 0; i < spatial.areas.Length; i++)
            {
                SpatialAreaSaveEntry entry = spatial.areas[i];
                if (entry == null)
                {
                    error = $"WorldSpatialAreaSaveLoader: area nulla all'indice {i}.";
                    return false;
                }

                if (!ValidateAreaEntry(world, data, entry, areaIds, occupiedCells, out error))
                    return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool TryApplySpatialAreas(World world, WorldSaveData data, out string error)
        {
            if (!CanApplySpatialAreasSafely(world, data, out error))
                return false;

            WorldSpatialAreaSaveData spatial = data.spatialAreas;
            if (spatial == null || spatial.areas == null || spatial.areas.Length == 0)
            {
                world.RebuildSpatialAreas();
                error = string.Empty;
                return true;
            }

            var areas = new List<WorldSpatialArea>(spatial.areas.Length);
            for (int i = 0; i < spatial.areas.Length; i++)
                areas.Add(FromDto(spatial.areas[i]));

            if (world.SpatialAreas == null)
                world.RebuildSpatialAreas();

            world.SpatialAreas.ReplaceAll(
                world.MapWidth,
                world.MapHeight,
                areas,
                spatial.diagnostics ?? Array.Empty<string>());

            error = string.Empty;
            return true;
        }

        private static bool ValidateAreaEntry(
            World world,
            WorldSaveData data,
            SpatialAreaSaveEntry entry,
            HashSet<int> areaIds,
            HashSet<int> occupiedCells,
            out string error)
        {
            if (entry.areaId <= 0 || !areaIds.Add(entry.areaId))
            {
                error = $"WorldSpatialAreaSaveLoader: areaId invalido o duplicato {entry.areaId}.";
                return false;
            }

            if (!Enum.IsDefined(typeof(WorldSpatialAreaKind), entry.kind) || entry.kind == (int)WorldSpatialAreaKind.None)
            {
                error = $"WorldSpatialAreaSaveLoader: kind invalido {entry.kind} per areaId {entry.areaId}.";
                return false;
            }

            if (!Enum.IsDefined(typeof(OwnerKind), entry.ownerKind))
            {
                error = $"WorldSpatialAreaSaveLoader: ownerKind invalido {entry.ownerKind} per areaId {entry.areaId}.";
                return false;
            }

            if ((OwnerKind)entry.ownerKind == OwnerKind.Npc && !WillNpcExistAfterLoad(world, data, entry.ownerId))
            {
                error = $"WorldSpatialAreaSaveLoader: owner NPC mancante {entry.ownerId} per areaId {entry.areaId}.";
                return false;
            }

            WorldSpatialAreaCellSaveData[] cells = entry.cells ?? Array.Empty<WorldSpatialAreaCellSaveData>();
            if (cells.Length <= 0)
            {
                error = $"WorldSpatialAreaSaveLoader: areaId {entry.areaId} senza celle.";
                return false;
            }

            for (int i = 0; i < cells.Length; i++)
            {
                int x = cells[i].x;
                int y = cells[i].y;
                if (!world.InBounds(x, y))
                {
                    error = $"WorldSpatialAreaSaveLoader: cella fuori mappa ({x},{y}) per areaId {entry.areaId}.";
                    return false;
                }

                int cellKey = world.CellIndex(x, y);
                if (!occupiedCells.Add(cellKey))
                {
                    error = $"WorldSpatialAreaSaveLoader: cella duplicata ({x},{y}) in spatial areas.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool WillNpcExistAfterLoad(World world, WorldSaveData data, int npcId)
        {
            if (npcId <= 0)
                return false;

            if (world != null && world.ExistsNpc(npcId))
                return true;

            NpcSaveEntry[] npcs = data != null ? data.npcs : null;
            if (npcs == null)
                return false;

            for (int i = 0; i < npcs.Length; i++)
            {
                if (npcs[i] != null && npcs[i].npcId == npcId)
                    return true;
            }

            return false;
        }

        private static WorldSpatialArea FromDto(SpatialAreaSaveEntry entry)
        {
            WorldSpatialAreaCellSaveData[] cells = entry.cells ?? Array.Empty<WorldSpatialAreaCellSaveData>();
            var areaCells = new WorldSpatialAreaCell[cells.Length];
            for (int i = 0; i < cells.Length; i++)
                areaCells[i] = new WorldSpatialAreaCell(cells[i].x, cells[i].y);

            return new WorldSpatialArea(
                entry.areaId,
                (WorldSpatialAreaKind)entry.kind,
                (OwnerKind)entry.ownerKind,
                entry.ownerId,
                entry.minX,
                entry.minY,
                entry.maxX,
                entry.maxY,
                areaCells);
        }
    }
}
