using System;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewCoordinateMapper
    // =============================================================================
    /// <summary>
    /// <para>
    /// Mapper passivo per convertire coordinate schermo in celle ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: coordinate view senza camera produttiva</b></para>
    /// <para>
    /// In questa fase ArcGraph non deve ancora dipendere da una <c>Camera</c> Unity.
    /// Il mapper riceve dimensioni viewport in pixel, coordinate schermo gia'
    /// relative a quel viewport e lo stato view corrente. Da questi dati ricava la
    /// cella visibile corrispondente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ResolveCellFromViewportPoint</b>: converte pixel viewport in cella.</item>
    ///   <item><b>TryNormalizeViewportPoint</b>: trasforma pixel in coordinate 0..1.</item>
    ///   <item><b>ResolveCellInsideVisibleRect</b>: proietta il punto normalizzato nel rettangolo celle.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphViewCoordinateMapper
    {
        // =============================================================================
        // ResolveCellFromViewportPoint
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la cella ArcGraph sotto un punto viewport.
        /// </para>
        ///
        /// <para><b>Convenzione coordinate</b></para>
        /// <para>
        /// La funzione assume coordinate viewport con origine in basso a sinistra,
        /// coerenti con la convenzione screen-space Unity. Il punto <c>(0,0)</c>
        /// indica il bordo basso/sinistro del viewport; <c>(width,height)</c>
        /// indica il bordo alto/destro ed e' considerato fuori, come limite
        /// esclusivo.
        /// </para>
        /// </summary>
        public static ArcGraphViewCoordinateResult ResolveCellFromViewportPoint(
            ArcGraphMapViewConfig config,
            ArcGraphViewState state,
            float viewportPointX,
            float viewportPointY,
            int viewportPixelWidth,
            int viewportPixelHeight,
            int zLevel = ArcGraphZLevelPolicy.CurrentRuntimeZLevel)
        {
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            state = state ?? ArcGraphViewState.CreateDefault(config);

            if (!TryNormalizeViewportPoint(
                viewportPointX,
                viewportPointY,
                viewportPixelWidth,
                viewportPixelHeight,
                out float normalizedX,
                out float normalizedY,
                out string reason))
            {
                return ArcGraphViewCoordinateResult.Invalid(reason);
            }

            var visibleRect = state.ResolveVisibleCellRect(config);
            if (visibleRect.IsEmpty)
                return ArcGraphViewCoordinateResult.Invalid("VisibleRectEmpty");

            var cell = ResolveCellInsideVisibleRect(
                visibleRect,
                normalizedX,
                normalizedY,
                zLevel);

            return new ArcGraphViewCoordinateResult(
                true,
                cell,
                normalizedX,
                normalizedY,
                visibleRect,
                "Ok");
        }

        // =============================================================================
        // TryNormalizeViewportPoint
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un punto viewport in coordinate normalizzate.
        /// </para>
        ///
        /// <para><b>Bounds half-open</b></para>
        /// <para>
        /// Il punto e' valido se <c>0 &lt;= x &lt; width</c> e
        /// <c>0 &lt;= y &lt; height</c>. Questo evita che il bordo destro o alto
        /// producano un indice cella pari al limite esclusivo.
        /// </para>
        /// </summary>
        public static bool TryNormalizeViewportPoint(
            float viewportPointX,
            float viewportPointY,
            int viewportPixelWidth,
            int viewportPixelHeight,
            out float normalizedX,
            out float normalizedY,
            out string reason)
        {
            normalizedX = 0f;
            normalizedY = 0f;
            reason = string.Empty;

            if (viewportPixelWidth <= 0 || viewportPixelHeight <= 0)
            {
                reason = "ViewportInvalid";
                return false;
            }

            if (viewportPointX < 0f ||
                viewportPointY < 0f ||
                viewportPointX >= viewportPixelWidth ||
                viewportPointY >= viewportPixelHeight)
            {
                reason = "PointOutsideViewport";
                return false;
            }

            normalizedX = viewportPointX / viewportPixelWidth;
            normalizedY = viewportPointY / viewportPixelHeight;
            reason = "Ok";
            return true;
        }

        private static ArcGraphCellCoord ResolveCellInsideVisibleRect(
            ArcGraphViewCellRect visibleRect,
            float normalizedX,
            float normalizedY,
            int zLevel)
        {
            int offsetX = ClampInt(
                (int)Math.Floor(normalizedX * visibleRect.Width),
                0,
                visibleRect.Width - 1);

            int offsetY = ClampInt(
                (int)Math.Floor(normalizedY * visibleRect.Height),
                0,
                visibleRect.Height - 1);

            return new ArcGraphCellCoord(
                visibleRect.MinX + offsetX,
                visibleRect.MinY + offsetY,
                zLevel);
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
