using System;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphMapViewConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione passiva della finestra mappa gestita da ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: configurazione view-side dichiarata</b></para>
    /// <para>
    /// Questa classe descrive dimensioni mappa e profilo zoom senza leggere file,
    /// senza cercare oggetti in scena e senza modificare la simulazione. In
    /// <c>v0.33c</c> potra' essere alimentata da JSON; in <c>v0.33b</c> serve come
    /// contratto runtime pulito per il futuro controller.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MapWidthCells/MapHeightCells</b>: dimensione logica della mappa.</item>
    ///   <item><b>ZoomLevels</b>: livelli zoom discreti disponibili.</item>
    ///   <item><b>DefaultZoomLevel</b>: livello iniziale richiesto.</item>
    ///   <item><b>MouseWheelStepsPerZoomLevel</b>: scatti rotellina per cambiare livello.</item>
    ///   <item><b>PanUsesMiddleMouseButton</b>: policy input per pan futuro.</item>
    ///   <item><b>ZoomTransitionSeconds</b>: durata della transizione morbida fra livelli zoom.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphMapViewConfig
    {
        private readonly ArcGraphViewZoomLevelDefinition[] _zoomLevels;

        public int MapWidthCells { get; }
        public int MapHeightCells { get; }
        public int DefaultZoomLevel { get; }
        public int MouseWheelStepsPerZoomLevel { get; }
        public bool PanUsesMiddleMouseButton { get; }
        public float ZoomTransitionSeconds { get; }
        public int ZoomLevelCount => _zoomLevels.Length;
        public ArcGraphViewZoomLevelDefinition[] ZoomLevels => CopyZoomLevels();

        // =============================================================================
        // ArcGraphMapViewConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una configurazione view con livelli zoom espliciti.
        /// </para>
        ///
        /// <para><b>Contratto senza JSON diretto</b></para>
        /// <para>
        /// Il costruttore riceve valori gia' materializzati. Non legge Resources,
        /// non carica file e non dipende da <c>MapGridConfig</c>. Questo mantiene
        /// separata la forma del contratto dalla futura serializzazione.
        /// </para>
        /// </summary>
        public ArcGraphMapViewConfig(
            int mapWidthCells,
            int mapHeightCells,
            ArcGraphViewZoomLevelDefinition[] zoomLevels,
            int defaultZoomLevel = 1,
            int mouseWheelStepsPerZoomLevel = 1,
            bool panUsesMiddleMouseButton = true,
            float zoomTransitionSeconds = 0.30f)
        {
            MapWidthCells = mapWidthCells > 0 ? mapWidthCells : 1;
            MapHeightCells = mapHeightCells > 0 ? mapHeightCells : 1;
            _zoomLevels = NormalizeZoomLevels(zoomLevels);
            DefaultZoomLevel = ResolveZoomLevel(defaultZoomLevel).Level;
            MouseWheelStepsPerZoomLevel = mouseWheelStepsPerZoomLevel > 0
                ? mouseWheelStepsPerZoomLevel
                : 1;
            PanUsesMiddleMouseButton = panUsesMiddleMouseButton;
            ZoomTransitionSeconds = zoomTransitionSeconds >= 0f
                ? zoomTransitionSeconds
                : 0.30f;
        }

        // =============================================================================
        // CreateDefaultV033
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea la configurazione canonica decisa per <c>v0.33</c>.
        /// </para>
        ///
        /// <para><b>Decisione progettuale registrata</b></para>
        /// <para>
        /// La mappa prevista e' <c>250x250</c>. Lo zoom ha quattro livelli:
        /// <c>300x300</c>, <c>150x150</c>, <c>75x75</c> e <c>20x20</c>. I primi due
        /// livelli non usano animazioni sprite e non usano vestizione actor a layer.
        /// Il primo livello non permette pan.
        /// </para>
        /// </summary>
        public static ArcGraphMapViewConfig CreateDefaultV033()
        {
            return new ArcGraphMapViewConfig(
                250,
                250,
                new[]
                {
                    new ArcGraphViewZoomLevelDefinition(1, 300, 300, false, false, false, true),
                    new ArcGraphViewZoomLevelDefinition(2, 150, 150, true, false, false, true),
                    new ArcGraphViewZoomLevelDefinition(3, 75, 75, true, true, false, false),
                    new ArcGraphViewZoomLevelDefinition(4, 20, 20, true, true, true, false)
                },
                defaultZoomLevel: 1,
                mouseWheelStepsPerZoomLevel: 1,
                panUsesMiddleMouseButton: true,
                zoomTransitionSeconds: 0.30f);
        }

        // =============================================================================
        // WithMapDimensions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una copia della configurazione usando nuove dimensioni mappa.
        /// </para>
        ///
        /// <para><b>Principio architetturale: dimensione mappa dal runtime World</b></para>
        /// <para>
        /// Il profilo zoom resta configurazione visuale, mentre larghezza e altezza
        /// della mappa arrivano dal <c>World</c> caricato. Questo metodo permette
        /// all'auto-installer di sostituire solo la shape runtime senza duplicare
        /// le dimensioni dentro il JSON ArcGraph.
        /// </para>
        /// </summary>
        public ArcGraphMapViewConfig WithMapDimensions(
            int mapWidthCells,
            int mapHeightCells)
        {
            return new ArcGraphMapViewConfig(
                mapWidthCells,
                mapHeightCells,
                _zoomLevels,
                DefaultZoomLevel,
                MouseWheelStepsPerZoomLevel,
                PanUsesMiddleMouseButton,
                ZoomTransitionSeconds);
        }

        // =============================================================================
        // ResolveZoomLevel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve una definizione zoom partendo dal numero livello richiesto.
        /// </para>
        ///
        /// <para><b>Fallback deterministico</b></para>
        /// <para>
        /// Se il livello richiesto non esiste, viene restituito il livello piu'
        /// vicino. In questo modo la futura UI puo' chiedere zoom avanti/indietro
        /// senza rischiare stati nulli.
        /// </para>
        /// </summary>
        public ArcGraphViewZoomLevelDefinition ResolveZoomLevel(int requestedLevel)
        {
            int index = ResolveZoomIndex(requestedLevel);
            return _zoomLevels[index];
        }

        // =============================================================================
        // ResolveZoomLevelFromWheel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola il livello zoom dopo uno o piu' scatti rotellina.
        /// </para>
        ///
        /// <para><b>Input astratto, non lettura mouse</b></para>
        /// <para>
        /// Il metodo riceve gia' il delta logico della rotellina. Non legge
        /// <c>Mouse.current</c> e non interpreta dispositivi fisici. Il futuro
        /// controller si occupera' di convertire input Unity in questo valore.
        /// </para>
        /// </summary>
        public ArcGraphViewZoomLevelDefinition ResolveZoomLevelFromWheel(
            int currentLevel,
            int wheelStepDelta)
        {
            int currentIndex = ResolveZoomIndex(currentLevel);
            int step = wheelStepDelta / MouseWheelStepsPerZoomLevel;

            if (wheelStepDelta != 0 && step == 0)
                step = wheelStepDelta > 0 ? 1 : -1;

            int targetIndex = ClampIndex(currentIndex + step);
            return _zoomLevels[targetIndex];
        }

        private ArcGraphViewZoomLevelDefinition[] CopyZoomLevels()
        {
            var copy = new ArcGraphViewZoomLevelDefinition[_zoomLevels.Length];
            Array.Copy(_zoomLevels, copy, _zoomLevels.Length);
            return copy;
        }

        private int ResolveZoomIndex(int requestedLevel)
        {
            for (int i = 0; i < _zoomLevels.Length; i++)
            {
                if (_zoomLevels[i].Level == requestedLevel)
                    return i;
            }

            int nearestIndex = 0;
            int nearestDistance = Math.Abs(_zoomLevels[0].Level - requestedLevel);

            for (int i = 1; i < _zoomLevels.Length; i++)
            {
                int distance = Math.Abs(_zoomLevels[i].Level - requestedLevel);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private int ClampIndex(int index)
        {
            if (index < 0) return 0;
            if (index >= _zoomLevels.Length) return _zoomLevels.Length - 1;
            return index;
        }

        private static ArcGraphViewZoomLevelDefinition[] NormalizeZoomLevels(
            ArcGraphViewZoomLevelDefinition[] zoomLevels)
        {
            if (zoomLevels == null || zoomLevels.Length == 0)
            {
                return new[]
                {
                    new ArcGraphViewZoomLevelDefinition(1, 1, 1, false, false, false, true)
                };
            }

            var copy = new ArcGraphViewZoomLevelDefinition[zoomLevels.Length];
            Array.Copy(zoomLevels, copy, zoomLevels.Length);
            Array.Sort(copy, (a, b) => a.Level.CompareTo(b.Level));
            return copy;
        }
    }
}
