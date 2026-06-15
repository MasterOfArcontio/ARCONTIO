using UnityEngine;
using UnityEngine.InputSystem;

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
    /// La struttura contiene solo valori copiati: modalita', contatori di root
    /// assegnati/attivi, presenza dei componenti e ragione dell'ultimo esito. Non
    /// espone riferimenti Unity e non consente di modificare scena o simulazione
    /// dall'esterno.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphViewModeSwitcherDiagnostics
    {
        public readonly bool SwitcherEnabled;
        public readonly ArcGraphViewMode CurrentMode;
        public readonly KeyCode ToggleKey;
        public readonly int MapGridVisualRootCount;
        public readonly int ArcGraphVisualRootCount;
        public readonly int MapGridActiveRootCount;
        public readonly int ArcGraphActiveRootCount;
        public readonly bool HasRuntimeWrapper;
        public readonly bool HasTerrainRenderer;
        public readonly bool HasNpcRenderer;
        public readonly bool HasObjectRenderer;
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
            int mapGridActiveRootCount,
            int arcGraphActiveRootCount,
            bool hasRuntimeWrapper,
            bool hasTerrainRenderer,
            bool hasNpcRenderer,
            bool hasObjectRenderer,
            bool didToggle,
            bool didProcessArcGraphFrame,
            string reason)
        {
            SwitcherEnabled = switcherEnabled;
            CurrentMode = currentMode;
            ToggleKey = toggleKey;
            MapGridVisualRootCount = mapGridVisualRootCount < 0 ? 0 : mapGridVisualRootCount;
            ArcGraphVisualRootCount = arcGraphVisualRootCount < 0 ? 0 : arcGraphVisualRootCount;
            MapGridActiveRootCount = mapGridActiveRootCount < 0 ? 0 : mapGridActiveRootCount;
            ArcGraphActiveRootCount = arcGraphActiveRootCount < 0 ? 0 : arcGraphActiveRootCount;
            HasRuntimeWrapper = hasRuntimeWrapper;
            HasTerrainRenderer = hasTerrainRenderer;
            HasNpcRenderer = hasNpcRenderer;
            HasObjectRenderer = hasObjectRenderer;
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
    ///   <item><b>terrainRenderer/npcRenderer/objectRenderer</b>: renderer minimi che possono essere accesi dal wrapper.</item>
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
        [SerializeField] private ArcGraphObjectRuntimeSceneRenderer objectRenderer;
        [SerializeField] private bool configureWrapperRenderingOnSwitch = true;
        [SerializeField] private bool enableWrapperUpdateInArcGraphMode = true;
        [SerializeField] private bool processArcGraphFrameOnSwitch = true;
        [SerializeField] private bool clearArcGraphWhenReturningToMapGrid;
        [SerializeField] private bool applyStartModeOnStart = true;
        [SerializeField] private bool logDiagnostics = true;

        private ArcGraphViewMode _currentMode;
        private ArcGraphViewModeSwitcherDiagnostics _lastDiagnostics;
        private bool _hasAppliedMode;

        public ArcGraphViewMode CurrentMode => _currentMode;
        public ArcGraphViewModeSwitcherDiagnostics LastDiagnostics => _lastDiagnostics;

        // =============================================================================
        // ConfigureRuntimeWiring
        // =============================================================================
        /// <summary>
        /// <para>
        /// Configura da codice i riferimenti scena necessari allo switch MapGrid /
        /// ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: cablaggio visuale esplicito</b></para>
        /// <para>
        /// Il metodo non cerca oggetti, non crea renderer e non deduce quali root
        /// spegnere. Riceve tutto dall'installer o dall'Inspector e aggiorna solo
        /// i riferimenti interni usati da <c>SetMode</c>. In questo modo lo switch
        /// F12 resta un puro cambio di vista, non un secondo bootstrap simulativo.
        /// </para>
        /// </summary>
        public void ConfigureRuntimeWiring(
            GameObject[] mapGridRoots,
            GameObject[] arcGraphRoots,
            ArcGraphMinimalRuntimeSceneWrapper wrapper,
            ArcGraphTerrainRuntimeSceneRenderer terrain,
            ArcGraphNpcRuntimeSceneRenderer npc,
            ArcGraphObjectRuntimeSceneRenderer objects = null)
        {
            mapGridVisualRoots = mapGridRoots;
            arcGraphVisualRoots = arcGraphRoots;
            runtimeWrapper = wrapper;
            terrainRenderer = terrain;
            npcRenderer = npc;
            objectRenderer = objects;

            if (_hasAppliedMode)
                ReapplyCurrentModeWithoutFrame();
        }

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

            if (!WasToggleKeyPressedThisFrame())
                return;

            ToggleMode();
        }

        // =============================================================================
        // WasToggleKeyPressedThisFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se il tasto di toggle e' stato premuto usando il New Input
        /// System.
        /// </para>
        ///
        /// <para><b>Principio architetturale: input view-side compatibile col progetto</b></para>
        /// <para>
        /// ARCONTIO usa il package Input System come backend attivo. Per questo lo
        /// switcher non puo' interrogare <c>UnityEngine.Input.GetKeyDown</c>, che
        /// genera eccezione quando il vecchio input manager e' disabilitato. Il
        /// metodo mantiene il contratto semplice del componente: ascolta solo il
        /// tasto configurato, senza introdurre action asset, comandi simulativi o
        /// dipendenze dal decision layer.
        /// </para>
        /// </summary>
        private bool WasToggleKeyPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            switch (toggleKey)
            {
                case KeyCode.F1:
                    return keyboard.f1Key.wasPressedThisFrame;
                case KeyCode.F2:
                    return keyboard.f2Key.wasPressedThisFrame;
                case KeyCode.F3:
                    return keyboard.f3Key.wasPressedThisFrame;
                case KeyCode.F4:
                    return keyboard.f4Key.wasPressedThisFrame;
                case KeyCode.F5:
                    return keyboard.f5Key.wasPressedThisFrame;
                case KeyCode.F6:
                    return keyboard.f6Key.wasPressedThisFrame;
                case KeyCode.F7:
                    return keyboard.f7Key.wasPressedThisFrame;
                case KeyCode.F8:
                    return keyboard.f8Key.wasPressedThisFrame;
                case KeyCode.F9:
                    return keyboard.f9Key.wasPressedThisFrame;
                case KeyCode.F10:
                    return keyboard.f10Key.wasPressedThisFrame;
                case KeyCode.F11:
                    return keyboard.f11Key.wasPressedThisFrame;
                case KeyCode.F12:
                    return keyboard.f12Key.wasPressedThisFrame;
                default:
                    return false;
            }
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
        // LogViewSwitchGateProbeFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive in Console una diagnostica sintetica per il gate manuale F12.
        /// </para>
        ///
        /// <para><b>Principio architetturale: gate visuale assistito, non automazione del test</b></para>
        /// <para>
        /// Il metodo non alterna modalita', non crea componenti, non processa frame
        /// e non muta il mondo simulativo. Serve solo a leggere lo stato gia'
        /// applicato dallo switcher dopo uno Start o dopo una pressione di F12.
        /// L'operatore umano resta responsabile della validazione visiva:
        /// terrain, NPC, muri e preview devono essere osservati in Game view.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Log F12 Manual Gate Probe")]
        public void LogViewSwitchGateProbeFromInspector()
        {
            LogViewSwitchGateProbe();
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
        // LogViewSwitchGateProbe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce una lettura diagnostica dello stato corrente dello switch F12.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Cablaggio</b>: verifica presenza di root MapGrid, root ArcGraph, wrapper e renderer.</item>
        ///   <item><b>Coerenza modalita'</b>: verifica che i root attivi siano coerenti con la modalita' corrente.</item>
        ///   <item><b>Esito</b>: segnala se il gate e' pronto per controllo visivo o se manca un prerequisito tecnico.</item>
        /// </list>
        /// </summary>
        public void LogViewSwitchGateProbe()
        {
            int mapGridRootCount = CountAssignedRoots(mapGridVisualRoots);
            int arcGraphRootCount = CountAssignedRoots(arcGraphVisualRoots);
            int mapGridActiveCount = CountActiveRoots(mapGridVisualRoots);
            int arcGraphActiveCount = CountActiveRoots(arcGraphVisualRoots);
            bool hasVisualRoots = mapGridRootCount > 0 && arcGraphRootCount > 0;
            bool hasRuntimeWiring = runtimeWrapper != null
                && terrainRenderer != null
                && npcRenderer != null
                && objectRenderer != null;
            bool rootsMatchCurrentMode = AreRootsCoherentWithCurrentMode(
                mapGridActiveCount,
                arcGraphActiveCount);
            bool readyForVisualCheck = switcherEnabled
                && _hasAppliedMode
                && hasVisualRoots
                && hasRuntimeWiring
                && rootsMatchCurrentMode;

            Debug.Log(
                "[ArcGraphViewModeSwitcher] F12ManualGateProbe " +
                "readyForVisualCheck=" + readyForVisualCheck +
                ", enabled=" + switcherEnabled +
                ", modeApplied=" + _hasAppliedMode +
                ", mode=" + _currentMode +
                ", key=" + toggleKey +
                ", mapGridRoots=" + mapGridRootCount +
                ", arcGraphRoots=" + arcGraphRootCount +
                ", mapGridActiveRoots=" + mapGridActiveCount +
                ", arcGraphActiveRoots=" + arcGraphActiveCount +
                ", wrapper=" + (runtimeWrapper != null) +
                ", terrainRenderer=" + (terrainRenderer != null) +
                ", npcRenderer=" + (npcRenderer != null) +
                ", objectRenderer=" + (objectRenderer != null) +
                ", rootsCoherent=" + rootsMatchCurrentMode +
                ", expectedHumanCheck=terrain+npc+muri+previewF3");
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
            _hasAppliedMode = true;
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

        // =============================================================================
        // ReapplyCurrentModeWithoutFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riapplica la visibilita' della modalita' corrente senza processare un
        /// nuovo frame ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: late binding senza lavoro grafico extra</b></para>
        /// <para>
        /// L'auto-installer puo' scoprire root MapGrid o ArcGraph alcuni frame dopo
        /// lo Start. Quando succede, i nuovi riferimenti devono assumere subito lo
        /// stato visuale corretto, ma non serve ricostruire mesh, queue o renderer.
        /// Questo metodo sincronizza solo root e gate runtime, evitando frame
        /// ridondanti e log rumorosi durante il binding ritardato.
        /// </para>
        /// </summary>
        private void ReapplyCurrentModeWithoutFrame()
        {
            bool arcGraphMode = _currentMode == ArcGraphViewMode.ArcGraph;

            SetRootsActive(mapGridVisualRoots, !arcGraphMode);
            SetRootsActive(arcGraphVisualRoots, arcGraphMode);
            ConfigureArcGraphRuntime(arcGraphMode);
        }

        private void ConfigureArcGraphRuntime(bool arcGraphMode)
        {
            if (terrainRenderer != null)
                terrainRenderer.SetRendererEnabled(arcGraphMode);

            if (npcRenderer != null)
                npcRenderer.SetRendererEnabled(arcGraphMode);

            if (objectRenderer != null)
                objectRenderer.SetRendererEnabled(arcGraphMode);

            if (runtimeWrapper == null)
                return;

            runtimeWrapper.SetWrapperEnabled(arcGraphMode);
            runtimeWrapper.SetProcessInUpdate(arcGraphMode && enableWrapperUpdateInArcGraphMode);
            runtimeWrapper.SetInteractionRouting(
                pushQueue: arcGraphMode,
                enableInteractionAfterPush: arcGraphMode);

            if (configureWrapperRenderingOnSwitch)
            {
                runtimeWrapper.SetRuntimeRendering(
                    arcGraphMode,
                    arcGraphMode,
                    arcGraphMode,
                    enableTerrainBeforeRender: arcGraphMode,
                    enableNpcBeforeRender: arcGraphMode,
                    enableObjectBeforeRender: arcGraphMode);
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

            if (objectRenderer != null)
                objectRenderer.ClearRuntimeRenderer();
        }

        // =============================================================================
        // AreRootsCoherentWithCurrentMode
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se lo stato active dei root visuali e' coerente con la modalita'
        /// corrente.
        /// </para>
        ///
        /// <para>
        /// In modalita' ArcGraph ci aspettiamo almeno un root ArcGraph attivo e
        /// nessun root MapGrid visuale attivo. In modalita' MapGrid ci aspettiamo
        /// almeno un root MapGrid attivo e nessun root ArcGraph attivo. La funzione
        /// controlla solo i root assegnati allo switcher: altri overlay legacy non
        /// ancora migrati restano fuori da questo gate tecnico.
        /// </para>
        /// </summary>
        private bool AreRootsCoherentWithCurrentMode(
            int mapGridActiveCount,
            int arcGraphActiveCount)
        {
            if (_currentMode == ArcGraphViewMode.ArcGraph)
                return mapGridActiveCount == 0 && arcGraphActiveCount > 0;

            return mapGridActiveCount > 0 && arcGraphActiveCount == 0;
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
                CountActiveRoots(mapGridVisualRoots),
                CountActiveRoots(arcGraphVisualRoots),
                runtimeWrapper != null,
                terrainRenderer != null,
                npcRenderer != null,
                objectRenderer != null,
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
                ", mapGridActiveRoots=" + _lastDiagnostics.MapGridActiveRootCount +
                ", arcGraphActiveRoots=" + _lastDiagnostics.ArcGraphActiveRootCount +
                ", wrapper=" + _lastDiagnostics.HasRuntimeWrapper +
                ", terrainRenderer=" + _lastDiagnostics.HasTerrainRenderer +
                ", npcRenderer=" + _lastDiagnostics.HasNpcRenderer +
                ", objectRenderer=" + _lastDiagnostics.HasObjectRenderer +
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

        private static int CountActiveRoots(GameObject[] roots)
        {
            if (roots == null)
                return 0;

            int count = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                // Usiamo activeSelf e non activeInHierarchy: qui vogliamo sapere
                // se lo switcher ha applicato lo stato richiesto al root assegnato,
                // non se un parent esterno lo sta mascherando per altri motivi.
                if (roots[i] != null && roots[i].activeSelf)
                    count++;
            }

            return count;
        }
    }
}
