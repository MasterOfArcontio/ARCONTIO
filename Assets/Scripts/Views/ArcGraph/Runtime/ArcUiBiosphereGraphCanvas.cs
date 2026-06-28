using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiBiosphereGraphCanvas
    // =============================================================================
    /// <summary>
    /// <para>
    /// Renderer texture-based per i grafici runtime della Biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: debug visuale deterministico</b></para>
    /// <para>
    /// La versione iniziale disegnava una mesh UGUI custom. Il contenitore del
    /// pannello azione risultava visibile, ma la mesh non appariva in modo
    /// affidabile durante i rebuild del layout runtime. Per il pannello di debug
    /// Biosfera usiamo quindi una <c>RawImage</c> con <c>Texture2D</c> generata:
    /// se il rettangolo UI esiste, Unity deve mostrare anche i pixel del grafico.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Texture</b>: buffer pixel ricreato quando cambia la dimensione del rettangolo.</item>
    ///   <item><b>Griglia</b>: riferimento locale autoscalato.</item>
    ///   <item><b>Meteo</b>: barre colorate di sfondo.</item>
    ///   <item><b>Serie</b>: polilinee disegnate da ViewModel read-only.</item>
    /// </list>
    /// </summary>
    public sealed class ArcUiBiosphereGraphCanvas : RawImage
    {
        private const int DefaultTextureWidth = 760;
        private const int DefaultTextureHeight = 500;
        private const int LeftPadding = 38;
        private const int RightPadding = 14;
        private const int TopPadding = 12;
        private const int BottomPadding = 28;
        private const int LineWidth = 3;

        private ArcUiBiosphereGraphViewModel _viewModel = ArcUiBiosphereGraphViewModel.Empty();
        private Texture2D _texture;
        private Color32[] _pixels;
        private Vector2Int _lastPixelSize;
        private int _forcedRefreshFrames;

        // =============================================================================
        // SetViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riceve il ViewModel gia' normalizzato dal provider UI e rigenera la
        /// texture del grafico.
        /// </para>
        /// </summary>
        public void SetViewModel(ArcUiBiosphereGraphViewModel viewModel)
        {
            _viewModel = viewModel ?? ArcUiBiosphereGraphViewModel.Empty();
            _forcedRefreshFrames = 2;
            RenderGraphTexture();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            color = Color.white;
            _forcedRefreshFrames = 2;
            RenderGraphTexture();
        }

        protected override void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }

            base.OnDestroy();
        }

        private void LateUpdate()
        {
            Vector2Int size = ResolvePixelSize();
            if (size != _lastPixelSize)
            {
                RenderGraphTexture();
                return;
            }

            if (_forcedRefreshFrames <= 0)
                return;

            _forcedRefreshFrames--;
            RenderGraphTexture();
        }

        private void RenderGraphTexture()
        {
            Vector2Int size = ResolvePixelSize();
            EnsureTexture(size.x, size.y);
            Clear(ColorFromHex("#05090C", 0.96f));

            RectInt full = new RectInt(0, 0, size.x, size.y);
            DrawFrame(full, ColorFromHex("#DDE6EE", 0.58f));

            RectInt plot = BuildPlotRect(size.x, size.y);
            DrawGrid(plot);
            DrawWeather(plot);
            bool drewSeries = DrawSeries(plot);
            if (!drewSeries)
                DrawNoSeriesMarker(plot);

            _texture.SetPixels32(_pixels);
            _texture.Apply(false, false);
            texture = _texture;
            _lastPixelSize = size;
        }

        private Vector2Int ResolvePixelSize()
        {
            Rect rect = rectTransform.rect;
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);
            if (width <= 4)
                width = DefaultTextureWidth;
            if (height <= 4)
                height = DefaultTextureHeight;

            return new Vector2Int(
                Mathf.Clamp(width, 32, 2048),
                Mathf.Clamp(height, 32, 2048));
        }

        private void EnsureTexture(int width, int height)
        {
            if (_texture != null && _texture.width == width && _texture.height == height)
                return;

            if (_texture != null)
                Destroy(_texture);

            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "ArcUiBiosphereGraphTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _pixels = new Color32[width * height];
        }

        private void Clear(Color color)
        {
            Color32 c = color;
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = c;
        }

        private static RectInt BuildPlotRect(int width, int height)
        {
            return new RectInt(
                LeftPadding,
                BottomPadding,
                Mathf.Max(4, width - LeftPadding - RightPadding),
                Mathf.Max(4, height - BottomPadding - TopPadding));
        }

        private void DrawGrid(RectInt plot)
        {
            Color axis = ColorFromHex("#DDE6EE", 0.48f);
            Color grid = ColorFromHex("#DDE6EE", 0.20f);

            DrawLine(plot.xMin, plot.yMin, plot.xMax, plot.yMin, axis, 2);
            DrawLine(plot.xMin, plot.yMin, plot.xMin, plot.yMax, axis, 2);

            for (int i = 1; i < 4; i++)
            {
                int y = Mathf.RoundToInt(Mathf.Lerp(plot.yMin, plot.yMax, i / 4f));
                DrawLine(plot.xMin, y, plot.xMax, y, grid, 1);
            }
        }

        private void DrawFrame(RectInt rect, Color color)
        {
            DrawLine(rect.xMin, rect.yMin, rect.xMax - 1, rect.yMin, color, 2);
            DrawLine(rect.xMax - 1, rect.yMin, rect.xMax - 1, rect.yMax - 1, color, 2);
            DrawLine(rect.xMax - 1, rect.yMax - 1, rect.xMin, rect.yMax - 1, color, 2);
            DrawLine(rect.xMin, rect.yMax - 1, rect.xMin, rect.yMin, color, 2);
        }

        private void DrawWeather(RectInt plot)
        {
            ArcUiBiosphereWeatherBucket[] weather = _viewModel.WeatherBuckets ?? new ArcUiBiosphereWeatherBucket[0];
            if (weather.Length == 0)
                return;

            float maxCount = 1f;
            for (int i = 0; i < weather.Length; i++)
                maxCount = Mathf.Max(maxCount, weather[i].Count);

            float totalBuckets = Mathf.Max(1f, _viewModel.MaxX - _viewModel.MinX + 1f);
            float bucketWidth = plot.width / totalBuckets;

            for (int i = 0; i < weather.Length; i++)
            {
                int x = Mathf.RoundToInt(Mathf.Lerp(plot.xMin, plot.xMax, NormalizeX(weather[i].BucketIndex)));
                int halfWidth = Mathf.Max(1, Mathf.RoundToInt(bucketWidth * 0.42f));
                int height = Mathf.RoundToInt(Mathf.Lerp(plot.height * 0.08f, plot.height * 0.28f, Mathf.Clamp01(weather[i].Count / maxCount)));
                Color color = weather[i].Color;
                color.a = 0.34f;
                FillRect(x - halfWidth, plot.yMin, halfWidth * 2, height, color);
            }
        }

        private bool DrawSeries(RectInt plot)
        {
            bool drewAny = false;
            ArcUiBiosphereGraphSeries[] series = _viewModel.Series ?? new ArcUiBiosphereGraphSeries[0];
            for (int s = 0; s < series.Length; s++)
            {
                if (!series[s].Visible)
                    continue;

                ArcUiBiosphereGraphPoint[] points = series[s].Points ?? new ArcUiBiosphereGraphPoint[0];
                if (points.Length == 1)
                {
                    Vector2Int only = MapPoint(points[0], plot);
                    FillRect(only.x - 2, only.y - 2, 5, 5, series[s].Color);
                    drewAny = true;
                    continue;
                }

                for (int p = 1; p < points.Length; p++)
                {
                    Vector2Int a = MapPoint(points[p - 1], plot);
                    Vector2Int b = MapPoint(points[p], plot);
                    DrawLine(a.x, a.y, b.x, b.y, series[s].Color, LineWidth);
                    drewAny = true;
                }
            }

            return drewAny;
        }

        private void DrawNoSeriesMarker(RectInt plot)
        {
            int y = Mathf.RoundToInt(Mathf.Lerp(plot.yMin, plot.yMax, 0.5f));
            DrawLine(plot.xMin + 10, y, plot.xMax - 10, y, ColorFromHex("#FFCC33", 0.92f), 2);
        }

        private Vector2Int MapPoint(ArcUiBiosphereGraphPoint point, RectInt plot)
        {
            return new Vector2Int(
                Mathf.RoundToInt(Mathf.Lerp(plot.xMin, plot.xMax, NormalizeX(point.X))),
                Mathf.RoundToInt(Mathf.Lerp(plot.yMin, plot.yMax, NormalizeY(point.Y))));
        }

        private float NormalizeX(float value)
        {
            float span = Mathf.Max(0.0001f, _viewModel.MaxX - _viewModel.MinX);
            return Mathf.Clamp01((value - _viewModel.MinX) / span);
        }

        private float NormalizeY(float value)
        {
            float span = Mathf.Max(0.0001f, _viewModel.MaxY - _viewModel.MinY);
            return Mathf.Clamp01((value - _viewModel.MinY) / span);
        }

        private void DrawLine(int x0, int y0, int x1, int y1, Color color, int width)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int steps = Mathf.Max(dx, dy);
            if (steps <= 0)
            {
                SetPixel(x0, y0, color);
                return;
            }

            float radius = Mathf.Max(0, width - 1) * 0.5f;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                FillRect(
                    Mathf.RoundToInt(x - radius),
                    Mathf.RoundToInt(y - radius),
                    Mathf.Max(1, width),
                    Mathf.Max(1, width),
                    color);
            }
        }

        private void FillRect(int x, int y, int width, int height, Color color)
        {
            int xMin = Mathf.Clamp(x, 0, _texture.width);
            int yMin = Mathf.Clamp(y, 0, _texture.height);
            int xMax = Mathf.Clamp(x + width, 0, _texture.width);
            int yMax = Mathf.Clamp(y + height, 0, _texture.height);
            for (int yy = yMin; yy < yMax; yy++)
                for (int xx = xMin; xx < xMax; xx++)
                    SetPixel(xx, yy, color);
        }

        private void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= _texture.width || y >= _texture.height)
                return;

            Color32 src = color;
            if (src.a >= 250)
            {
                _pixels[y * _texture.width + x] = src;
                return;
            }

            Color32 dst = _pixels[y * _texture.width + x];
            float a = src.a / 255f;
            _pixels[y * _texture.width + x] = new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(dst.r, src.r, a)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(dst.g, src.g, a)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(dst.b, src.b, a)),
                255);
        }

        private static Color ColorFromHex(string hex, float alpha)
        {
            Color color = Color.white;
            ColorUtility.TryParseHtmlString(hex, out color);
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
