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
    /// <para><b>Principio architetturale: serializzazione separata dal runtime contract</b></para>
    /// <para>
    /// Il JSON non deve diventare direttamente stato operativo. Questo helper legge
    /// una stringa JSON gia' ricevuta dal chiamante, crea un DTO compatibile con
    /// <c>JsonUtility</c>, normalizza valori mancanti o non validi e produce un
    /// <c>ArcGraphMapViewConfig</c>. Non carica Resources, non cerca file, non
    /// accede alla scena e non crea componenti Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DefaultResourcePath</b>: path suggerita per il futuro wrapper loader.</item>
    ///   <item><b>ParseOrDefault</b>: converte JSON testuale in config runtime con fallback.</item>
    ///   <item><b>TryParse</b>: variante esplicita con esito booleano.</item>
    ///   <item><b>CreateInitialViewStateOrDefault</b>: crea anche lo stato vista iniziale.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphMapViewConfigJson
    {
        public const string DefaultResourcePath = "ArcGraph/Config/ArcGraphViewConfig";

        // =============================================================================
        // ParseOrDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una stringa JSON in configurazione view ArcGraph.
        /// </para>
        ///
        /// <para><b>Fallback non distruttivo</b></para>
        /// <para>
        /// Se la stringa e' vuota, nulla o non parsabile, il metodo restituisce la
        /// configurazione canonica <c>v0.33</c>. Questo mantiene il sistema
        /// avviabile durante la fase di transizione, senza forzare un aggancio
        /// immediato a Resources o alla scena.
        /// </para>
        /// </summary>
        public static ArcGraphMapViewConfig ParseOrDefault(string json)
        {
            return TryParse(json, out var config)
                ? config
                : ArcGraphMapViewConfig.CreateDefaultV033();
        }

        // =============================================================================
        // TryParse
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a convertire una stringa JSON in configurazione runtime.
        /// </para>
        ///
        /// <para><b>Parsing puro</b></para>
        /// <para>
        /// Il metodo usa solo <c>JsonUtility.FromJson</c> sulla stringa ricevuta.
        /// Non legge <c>Resources.Load</c>, non stampa log, non intercetta input e
        /// non produce effetti collaterali. L'esito booleano permette al chiamante
        /// futuro di decidere se mostrare diagnostica.
        /// </para>
        /// </summary>
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

        // =============================================================================
        // CreateInitialViewStateOrDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il JSON in configurazione e crea lo stato vista iniziale.
        /// </para>
        ///
        /// <para><b>Catena completa v0.33c</b></para>
        /// <para>
        /// Questo metodo rappresenta la catena prevista dal checkpoint:
        /// JSON configurativo, DTO serializzabile, contratto runtime e stato vista
        /// iniziale. Non applica lo stato a una camera Unity: quello resta fuori
        /// scope fino al controller dei passaggi successivi.
        /// </para>
        /// </summary>
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
    /// DTO serializzabile della configurazione view ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: DTO mutabile solo al confine JSON</b></para>
    /// <para>
    /// <c>JsonUtility</c> richiede campi pubblici e tipi serializzabili. Per questo
    /// il DTO e' volutamente semplice e mutabile. La configurazione runtime resta
    /// invece <c>ArcGraphMapViewConfig</c>, che normalizza i dati e non espone il
    /// proprio array interno.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>mapWidthCells/mapHeightCells</b>: dimensione mappa in celle.</item>
    ///   <item><b>defaultZoomLevel</b>: livello iniziale richiesto.</item>
    ///   <item><b>mouseWheelStepsPerZoomLevel</b>: scatti rotellina per livello.</item>
    ///   <item><b>panUsesMiddleMouseButton</b>: policy del futuro pan.</item>
    ///   <item><b>zoomTransitionSeconds</b>: durata della transizione visuale tra livelli zoom.</item>
    ///   <item><b>panSmoothTime</b>: inerzia visuale del pan camera-side.</item>
    ///   <item><b>panMaxSpeedCellsPerSecond</b>: limite massimo del pan visuale.</item>
    ///   <item><b>zoomLevels</b>: profilo zoom serializzato.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphMapViewConfigDto
    {
        public int mapWidthCells = 250;
        public int mapHeightCells = 250;
        public int defaultZoomLevel = 1;
        public int mouseWheelStepsPerZoomLevel = 1;
        public bool panUsesMiddleMouseButton = true;
        public float zoomTransitionSeconds = 0.12f;
        public float panSmoothTime = 0.18f;
        public float panMaxSpeedCellsPerSecond = 90f;
        public ArcGraphZoomLevelConfigDto[] zoomLevels;

        // =============================================================================
        // ToRuntimeConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il DTO JSON in configurazione runtime ArcGraph.
        /// </para>
        ///
        /// <para><b>Normalizzazione del confine esterno</b></para>
        /// <para>
        /// Valori mancanti, array nulli o dimensioni non valide non vengono propagati
        /// come stato operativo fragile. Il metodo usa il profilo <c>v0.33</c> come
        /// fallback e produce sempre un contratto runtime coerente.
        /// </para>
        /// </summary>
        public ArcGraphMapViewConfig ToRuntimeConfig()
        {
            var defaults = ArcGraphMapViewConfig.CreateDefaultV033();
            var runtimeZoomLevels = BuildRuntimeZoomLevels(defaults);

            return new ArcGraphMapViewConfig(
                mapWidthCells > 0 ? mapWidthCells : defaults.MapWidthCells,
                mapHeightCells > 0 ? mapHeightCells : defaults.MapHeightCells,
                runtimeZoomLevels,
                defaultZoomLevel > 0 ? defaultZoomLevel : defaults.DefaultZoomLevel,
                mouseWheelStepsPerZoomLevel > 0
                    ? mouseWheelStepsPerZoomLevel
                    : defaults.MouseWheelStepsPerZoomLevel,
                panUsesMiddleMouseButton,
                zoomTransitionSeconds >= 0f
                    ? zoomTransitionSeconds
                    : defaults.ZoomTransitionSeconds,
                panSmoothTime >= 0f
                    ? panSmoothTime
                    : defaults.PanSmoothTime,
                panMaxSpeedCellsPerSecond > 0f
                    ? panMaxSpeedCellsPerSecond
                    : defaults.PanMaxSpeedCellsPerSecond);
        }

        private ArcGraphViewZoomLevelDefinition[] BuildRuntimeZoomLevels(
            ArcGraphMapViewConfig defaults)
        {
            if (zoomLevels == null || zoomLevels.Length == 0)
                return defaults.ZoomLevels;

            var runtime = new ArcGraphViewZoomLevelDefinition[zoomLevels.Length];

            for (int i = 0; i < zoomLevels.Length; i++)
            {
                var dto = zoomLevels[i];
                runtime[i] = dto != null
                    ? dto.ToRuntimeDefinition(defaults)
                    : defaults.ResolveZoomLevel(1);
            }

            return runtime;
        }
    }

    // =============================================================================
    // ArcGraphZoomLevelConfigDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile di un singolo livello zoom ArcGraph.
    /// </para>
    ///
    /// <para><b>Policy visuale configurabile</b></para>
    /// <para>
    /// Ogni livello zoom dichiara sia la finestra di celle richiesta sia il grado di
    /// dettaglio ammesso. In questo modo zoom 1 e zoom 2 possono restare statici e
    /// semplificati, mentre zoom 3 e 4 possono progressivamente mostrare piu'
    /// dettaglio senza cambiare la simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>level</b>: numero livello zoom.</item>
    ///   <item><b>visibleCellsX/visibleCellsY</b>: dimensione viewport in celle.</item>
    ///   <item><b>allowsPan</b>: pan permesso a questo livello.</item>
    ///   <item><b>allowsSpriteAnimation</b>: animazioni sprite permesse.</item>
    ///   <item><b>allowsLayeredActorSprites</b>: actor a layer permessi.</item>
    ///   <item><b>usesSimplifiedRepresentation</b>: icone/statici/aggregazioni.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphZoomLevelConfigDto
    {
        public int level;
        public int visibleCellsX;
        public int visibleCellsY;
        public bool allowsPan;
        public bool allowsSpriteAnimation;
        public bool allowsLayeredActorSprites;
        public bool usesSimplifiedRepresentation;

        // =============================================================================
        // ToRuntimeDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il livello zoom JSON in definizione runtime.
        /// </para>
        ///
        /// <para><b>Fallback per campi numerici</b></para>
        /// <para>
        /// I booleani sono trattati come decisioni esplicite del JSON. I campi
        /// numerici, invece, ricevono fallback dal profilo default se risultano
        /// mancanti o non validi, per evitare livelli zoom a dimensione zero.
        /// </para>
        /// </summary>
        public ArcGraphViewZoomLevelDefinition ToRuntimeDefinition(
            ArcGraphMapViewConfig defaults)
        {
            defaults = defaults ?? ArcGraphMapViewConfig.CreateDefaultV033();

            int resolvedLevel = level > 0 ? level : 1;
            var fallback = defaults.ResolveZoomLevel(resolvedLevel);

            return new ArcGraphViewZoomLevelDefinition(
                resolvedLevel,
                visibleCellsX > 0 ? visibleCellsX : fallback.VisibleCellsX,
                visibleCellsY > 0 ? visibleCellsY : fallback.VisibleCellsY,
                allowsPan,
                allowsSpriteAnimation,
                allowsLayeredActorSprites,
                usesSimplifiedRepresentation);
        }
    }
}
