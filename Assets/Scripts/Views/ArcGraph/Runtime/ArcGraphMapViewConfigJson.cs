using System;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphMapViewConfigJson
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper di frontiera per trasformare JSON view ArcGraph in contratto runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: config camera, non LOD</b></para>
    /// <para>
    /// Il JSON ArcGraph non definisce piu' livelli zoom o policy sprite. Contiene
    /// solo parametri continui della camera
    /// ortografica. Le animazioni e gli sprite restano indipendenti dallo zoom.
    /// </para>
    /// </summary>
    public static class ArcGraphMapViewConfigJson
    {
        public const string DefaultResourcePath = "ArcGraph/Config/ArcGraphViewConfig";

        public static ArcGraphMapViewConfig ParseOrDefault(string json)
        {
            return TryParse(json, out var config)
                ? config
                : ArcGraphMapViewConfig.CreateDefaultV033();
        }

        public static bool TryParse(string json, out ArcGraphMapViewConfig config)
        {
            config = null;

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var dto = JsonUtility.FromJson<ArcGraphMapViewConfigDto>(json);
                if (dto == null)
                    return false;

                config = dto.ToRuntimeConfig();
                return config != null;
            }
            catch
            {
                config = null;
                return false;
            }
        }

        public static ArcGraphViewState CreateInitialViewStateOrDefault(string json)
        {
            var config = ParseOrDefault(json);
            return ArcGraphViewState.CreateDefault(config);
        }
    }

    // =============================================================================
    // ArcGraphMapViewConfigDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile della configurazione camera ArcGraph.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphMapViewConfigDto
    {
        public int mapWidthCells;
        public int mapHeightCells;
        public float defaultOrthographicSize = 75f;
        public float minOrthographicSize = 8f;
        public float maxOrthographicSize = 150f;
        public float zoomStep = 8f;
        public float zoomSmoothTime = 0.20f;
        public bool panUsesMiddleMouseButton = true;
        public bool panInertiaEnabled = true;
        public float panInertiaDamping = 7.5f;
        public float panInertiaStopThreshold = 0.05f;
        public float panVelocityMultiplier = 0.75f;

        public ArcGraphMapViewConfig ToRuntimeConfig()
        {
            var defaults = ArcGraphMapViewConfig.CreateDefaultV033();

            return new ArcGraphMapViewConfig(
                mapWidthCells > 0 ? mapWidthCells : defaults.MapWidthCells,
                mapHeightCells > 0 ? mapHeightCells : defaults.MapHeightCells,
                defaultOrthographicSize > 0f
                    ? defaultOrthographicSize
                    : defaults.DefaultOrthographicSize,
                minOrthographicSize > 0f
                    ? minOrthographicSize
                    : defaults.MinOrthographicSize,
                maxOrthographicSize > 0f
                    ? maxOrthographicSize
                    : defaults.MaxOrthographicSize,
                zoomStep > 0f ? zoomStep : defaults.ZoomStep,
                zoomSmoothTime >= 0f ? zoomSmoothTime : defaults.ZoomSmoothTime,
                panUsesMiddleMouseButton,
                panInertiaEnabled,
                panInertiaDamping > 0f ? panInertiaDamping : defaults.PanInertiaDamping,
                panInertiaStopThreshold > 0f
                    ? panInertiaStopThreshold
                    : defaults.PanInertiaStopThreshold,
                panVelocityMultiplier > 0f ? panVelocityMultiplier : defaults.PanVelocityMultiplier);
        }
    }
}
