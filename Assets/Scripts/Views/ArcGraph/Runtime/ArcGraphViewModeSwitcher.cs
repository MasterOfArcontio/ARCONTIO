using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' visuale attiva tra renderer MapGrid legacy e renderer ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: switch di vista, non switch di simulazione</b></para>
    /// <para>
    /// La modalita' indica solo quali radici visuali vengono mostrate. Non decide
    /// job, non ferma il World, non cambia MapGrid come sorgente dati provvisoria
    /// e non emette comandi verso gli NPC.
    /// </para>
    /// </summary>
    public enum ArcGraphViewMode
    {
        MapGrid = 0,
        ArcGraph = 1
    }

    // =============================================================================
    // ArcGraphViewModeSwitcherDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica dell'ultimo cambio modalita' visuale MapGrid/ArcGraph.
    /// </para>
    ///
    /// <para><b>Diagnostica value-only</b></para>
    /// <para>
    /// La struttura contiene solo valori copiati: modalita', contatori, presenza
    /// dei componenti e ragione dell'ultimo esito. Non espone riferimenti Unity e
    /// non consente di modificare scena o simulazione dall'esterno.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphViewModeSwitcherDiagnostics
    {
        public readonly bool SwitcherEnabled;
        public readonly ArcGraphViewMode CurrentMode;
        public readonly KeyCode ToggleKey;
        public readonly int MapGridVisualRootCount;
        public readonly int ArcGraphVisualRootCount;
        public readonly bool HasRuntimeWrapper;
        public readonly bool HasTerrainRenderer;
        public readonly bool HasNpcRenderer;
        public readonly bool DidToggle;
        public readonly bool DidProcessArcGraphFrame;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphViewModeSwitcherDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile dello switcher visuale.
        /// </para>
        /// </summary>
        public ArcGraphViewModeSwitcherDiagnostics(
            bool switcherEnabled,
            ArcGraphViewMode currentMode,
            KeyCode toggleKey,
            int mapGridVisualRootCount,
            int arcGraphVisualRootCount,
            bool hasRuntimeWrapper,
            bool hasTerrainRenderer,
            bool hasNpcRenderer,
            bool didToggle,
            bool didProcessArcGraphFrame,
            string reason)
        {
            SwitcherEnabled = switcherEnabled;
            CurrentMode = currentMode;
            ToggleKey = toggleKey;
            MapGridVisualRootCount = mapGridVisualRootCount < 0 ? 0 : mapGridVisualRootCount;
            ArcGraphVisualRootCount = arcGraphVisualRootCount < 0 ? 0 : arcGraphVisualRootCount;
            HasRuntimeWrapper = hasRuntimeWrapper;
            HasTerrainRenderer = hasTerrainRenderer;
            HasNpcRenderer = hasNpcRenderer;
            DidToggle = didToggle;
            DidProcessArcGraphFrame = didProcessArcGraphFrame;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphViewModeSwitcher
    // =============================================================================
    /// <summary>
    /// <para>
    /// Componente scena che permette di passare da vista MapGrid a vista ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: frontiera visuale esplicita</b></para>
    /// <para>
    /// Lo switcher non cerca componenti globali e non deduce da solo cosa spegnere.
    /// L'operatore assegna da Inspector le radici visuali MapGrid e ArcGraph. Il
    /// componente si limita ad abilitarle/disabilitarle e, quando entra in ArcGraph,
    /// chiede al wrapper minimo di processare un frame visuale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>mapGridVisualRoots</b>: GameObject del vecchio renderer da mostrare in modalita' MapGrid.</item>
    ///   <item><b>arcGraphVisualRoots</b>: GameObject del nuovo renderer da mostrare in modalita' ArcGraph.</item>
    ///   <item><b>runtimeWrapper</b>: ponte ArcGraph gia' cablato nella scena.</item>
    ///   <item><b>terrainRenderer/npcRenderer</b>: renderer minimi che possono essere accesi dal wrapper.</item>
    ///   <item><b>toggleKey</b>: tasto di cambio modalita', di default F12.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphViewModeSwitcher : MonoBehaviour
    {
        [SerializeField] private bool switcherEnabled = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;
        [SerializeField] private ArcGraphViewMode startMode = ArcGraphViewMode.MapGrid;
        [SerializeField] private GameObject[] mapGridVisualRoots;
        [SerializeField] private GameObject[] arcGraphVisualRoots;
        [SerializeField] private ArcGraphMinimalRuntimeSceneWrapper runtimeWrapper;
        [SerializeField] private ArcGraphTerrainRuntimeSceneRenderer terrainRenderer;
        [SerializeField] private ArcGraphNpcRuntimeSceneRenderer npcRenderer;
        [SerializeField] private bool configureWrapperRenderingOnSwitch = true;
        [SerializeField] private bool enableWrapperUpdateInArcGraphMode = true;
        [SerializeField] private bool processArcGraphFrameOnSwitch = true;
        [SerializeField] private bool clearArcGraphWhenReturningToMapGrid;
        [SerializeField] private bool applyStartModeOnStart = true;
        [SerializeField] private bool logDiagnostics = true;

        private ArcGraphViewMode _currentMode;
        private ArcGraphViewModeSwitcherDiagnostics _lastDiagnostics;

        public ArcGraphViewMode CurrentMode => _currentMode;
        public ArcGraphViewModeSwitcherDiagnostics LastDiagnostics => _lastDiagnostics;

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica opzionalmente la modalita' iniziale configurata da Inspector.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (!applyStartModeOnStart)
            {
                _currentMode = startMode;
                StoreAndLogDiagnostics(false, false, "StartModeNotApplied");
                return;
            }

            SetMode(startMode, didToggle: false);
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ascolta il tasto configurato e alterna la modalita' visuale.
        /// </para>
        /// </summary>
        private void Update()
        {
            // Se lo switcher e' spento non interroghiamo neppure Input: costo nullo
            // lato runtime e nessun cambio accidentale di visuale.
            if (!switcherEnabled)
                return;

            if (!Input.GetKeyDown(toggleKey))
                return;

            ToggleMode();
        }

        // =============================================================================
        // ToggleModeFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Alterna la modalita' visuale da menu contestuale Inspector.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Toggle View Mode")]
        public void ToggleModeFromInspector()
        {
            ToggleMode();
        }

        // =============================================================================
        // SetMapGridModeFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Forza la vista MapGrid da menu contestuale Inspector.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Switch To MapGrid View")]
        public void SetMapGridModeFromInspector()
        {
            SetMode(ArcGraphViewMode.MapGrid, didToggle: false);
        }

        // =============================================================================
        // SetArcGraphModeFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Forza la vista ArcGraph da menu contestuale Inspector.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Switch To ArcGraph View")]
        public void SetArcGraphModeFromInspector()
        {
            SetMode(ArcGraphViewMode.ArcGraph, didToggle: false);
        }

        // =============================================================================
        // ToggleMode
        // =============================================================================
        /// <summary>
        /// <para>
        /// Alterna tra modalita' MapGrid e ArcGraph.
        /// </para>
        /// </summary>
        public void ToggleMode()
        {
            ArcGraphViewMode nextMode = _currentMode == ArcGraphViewMode.ArcGraph
                ? ArcGraphViewMode.MapGrid
                : ArcGraphViewMode.ArcGraph;

            SetMode(nextMode, didToggle: true);
        }

        // =============================================================================
        // SetMode
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica una modalita' visuale specifica.
        /// </para>
        /// </summary>
        public void SetMode(
            ArcGraphViewMode mode,
            bool didToggle = false)
        {
            if (!switcherEnabled)
            {
                StoreAndLogDiagnostics(didToggle, false, "SwitcherDisabled");
                return;
            }

            _currentMode = mode;
            bool arcGraphMode = mode == ArcGraphViewMode.ArcGraph;

            SetRootsActive(mapGridVisualRoots, !arcGraphMode);
            SetRootsActive(arcGraphVisualRoots, arcGraphMode);
            ConfigureArcGraphRuntime(arcGraphMode);

            bool processedFrame = arcGraphMode && TryProcessArcGraphFrame();

            if (!arcGraphMode && clearArcGraphWhenReturningToMapGrid)
                ClearArcGraphRuntimeRenderers();

            StoreAndLogDiagnostics(
                didToggle,
                processedFrame,
                arcGraphMode ? "ArcGraphViewActive" : "MapGridViewActive");
        }

        private void ConfigureArcGraphRuntime(bool arcGraphMode)
        {
            if (terrainRenderer != null)
                terrainRenderer.SetRendererEnabled(arcGraphMode);

            if (npcRenderer != null)
                npcRenderer.SetRendererEnabled(arcGraphMode);

            if (runtimeWrapper == null)
                return;

            runtimeWrapper.SetWrapperEnabled(arcGraphMode);
            runtimeWrapper.SetProcessInUpdate(arcGraphMode && enableWrapperUpdateInArcGraphMode);

            if (configureWrapperRenderingOnSwitch)
            {
                runtimeWrapper.SetRuntimeRendering(
                    arcGraphMode,
                    arcGraphMode,
                    enableTerrainBeforeRender: arcGraphMode,
                    enableNpcBeforeRender: arcGraphMode);
            }
        }

        private bool TryProcessArcGraphFrame()
        {
            if (!processArcGraphFrameOnSwitch)
                return false;

            if (runtimeWrapper == null)
                return false;

            runtimeWrapper.ProcessFrame();
            return true;
        }

        private void ClearArcGraphRuntimeRenderers()
        {
            if (terrainRenderer != null)
                terrainRenderer.ClearRuntimeRenderer();

            if (npcRenderer != null)
                npcRenderer.ClearRuntimeRenderer();
        }

        private static void SetRootsActive(
            GameObject[] roots,
            bool active)
        {
            if (roots == null)
                return;

            for (int i = 0; i < roots.Length; i++)
            {
                // Le entry nulle vengono tollerate: durante il cablaggio Inspector
                // e' normale avere slot non ancora assegnati.
                if (roots[i] != null)
                    roots[i].SetActive(active);
            }
        }

        private void StoreAndLogDiagnostics(
            bool didToggle,
            bool didProcessArcGraphFrame,
            string reason)
        {
            _lastDiagnostics = new ArcGraphViewModeSwitcherDiagnostics(
                switcherEnabled,
                _currentMode,
                toggleKey,
                CountAssignedRoots(mapGridVisualRoots),
                CountAssignedRoots(arcGraphVisualRoots),
                runtimeWrapper != null,
                terrainRenderer != null,
                npcRenderer != null,
                didToggle,
                didProcessArcGraphFrame,
                reason);

            LogLastDiagnostics();
        }

        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphViewModeSwitcher] " + _lastDiagnostics.Reason +
                " enabled=" + _lastDiagnostics.SwitcherEnabled +
                ", mode=" + _lastDiagnostics.CurrentMode +
                ", key=" + _lastDiagnostics.ToggleKey +
                ", mapGridRoots=" + _lastDiagnostics.MapGridVisualRootCount +
                ", arcGraphRoots=" + _lastDiagnostics.ArcGraphVisualRootCount +
                ", wrapper=" + _lastDiagnostics.HasRuntimeWrapper +
                ", terrainRenderer=" + _lastDiagnostics.HasTerrainRenderer +
                ", npcRenderer=" + _lastDiagnostics.HasNpcRenderer +
                ", toggled=" + _lastDiagnostics.DidToggle +
                ", processedFrame=" + _lastDiagnostics.DidProcessArcGraphFrame);
        }

        private static int CountAssignedRoots(GameObject[] roots)
        {
            if (roots == null)
                return 0;

            int count = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                    count++;
            }

            return count;
        }
    }
}
