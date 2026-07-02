using System;

namespace Arcontio.Core.Save
{
    [Serializable]
    public sealed class WorldSpatialAreaSaveData
    {
        public SpatialAreaSaveEntry[] areas = Array.Empty<SpatialAreaSaveEntry>();
        public string[] diagnostics = Array.Empty<string>();
    }

    [Serializable]
    public sealed class SpatialAreaSaveEntry
    {
        public int areaId;
        public int kind;
        public int ownerKind;
        public int ownerId;
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
        public WorldSpatialAreaCellSaveData[] cells = Array.Empty<WorldSpatialAreaCellSaveData>();
    }

    [Serializable]
    public struct WorldSpatialAreaCellSaveData
    {
        public int x;
        public int y;
    }
}
