using System;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato grafico minimo della finestra mappa ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: centro view, non zoom LOD</b></para>
    /// <para>
    /// Lo stato non contiene piu' livelli zoom. Lo zoom fisico vive nella camera
    /// ortografica Unity, mentre questo oggetto conserva solo il centro mappa
    /// condiviso da renderer, culling e picking.
    /// </para>
    /// </summary>
    public sealed class ArcGraphViewState
    {
        public float CenterCellX { get; private set; }
        public float CenterCellY { get; private set; }

        public ArcGraphViewState(
            ArcGraphMapViewConfig config,
            float centerCellX,
            float centerCellY)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();

            CenterCellX = centerCellX;
            CenterCellY = centerCellY;
            ClampCenterToMap(config, config.DefaultOrthographicSize);
        }

        public static ArcGraphViewState CreateDefault(ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            return new ArcGraphViewState(
                config,
                config.MapWidthCells * 0.5f,
                config.MapHeightCells * 0.5f);
        }

        public void SetCenterCell(
            float centerCellX,
            float centerCellY,
            ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();

            CenterCellX = centerCellX;
            CenterCellY = centerCellY;
            ClampCenterToMap(config, config.DefaultOrthographicSize);
        }

        public bool ApplyPanCells(
            float deltaCellsX,
            float deltaCellsY,
            ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();

            CenterCellX += deltaCellsX;
            CenterCellY += deltaCellsY;
            ClampCenterToMap(config, config.DefaultOrthographicSize);
            return true;
        }

        public ArcGraphViewCellRect ResolveVisibleCellRect(ArcGraphMapViewConfig config)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            float halfHeight = Math.Max(0f, config.DefaultOrthographicSize);
            float halfWidth = halfHeight;

            int minX = FloorToInt(CenterCellX - halfWidth);
            int minY = FloorToInt(CenterCellY - halfHeight);
            int maxX = (int)Math.Ceiling(CenterCellX + halfWidth);
            int maxY = (int)Math.Ceiling(CenterCellY + halfHeight);

            minX = ClampInt(minX, 0, config.MapWidthCells);
            minY = ClampInt(minY, 0, config.MapHeightCells);
            maxX = ClampInt(maxX, minX, config.MapWidthCells);
            maxY = ClampInt(maxY, minY, config.MapHeightCells);

            return new ArcGraphViewCellRect(minX, minY, maxX, maxY);
        }

        private void ClampCenterToMap(
            ArcGraphMapViewConfig config,
            float orthographicSize)
        {
            float halfHeight = Math.Max(0f, orthographicSize);
            float halfWidth = halfHeight;

            CenterCellX = ClampCenterAxis(CenterCellX, config.MapWidthCells, halfWidth);
            CenterCellY = ClampCenterAxis(CenterCellY, config.MapHeightCells, halfHeight);
        }

        private static float ClampCenterAxis(
            float current,
            int mapCells,
            float halfVisibleCells)
        {
            if (mapCells <= 0)
                return 0f;

            if (mapCells <= halfVisibleCells * 2f)
                return mapCells * 0.5f;

            float min = halfVisibleCells;
            float max = mapCells - halfVisibleCells;

            if (current < min) return min;
            if (current > max) return max;
            return current;
        }

        private static int FloorToInt(float value)
        {
            return (int)Math.Floor(value);
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (max < min)
                return min;

            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
