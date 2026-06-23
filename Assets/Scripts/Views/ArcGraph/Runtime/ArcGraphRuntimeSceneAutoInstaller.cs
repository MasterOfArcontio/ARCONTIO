using System;
using System.Collections.Generic;
using Arcontio.View.MapGrid;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRuntimeSceneAutoInstaller
    // =============================================================================
    /// <summary>
    /// <para>
    /// Installer runtime controllato che crea il cablaggio minimo ArcGraph dentro
    /// <c>Scene_MapGrid</c> quando la scena non contiene ancora i componenti
    /// necessari alla vista ArcGraph runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bootstrap visuale di bordo, non simulazione</b></para>
    /// <para>
    /// Questo componente risolve un problema pratico di scena: se
    /// <c>ArcGraphMinimalRuntimeSceneWrapper</c>,
    /// <c>ArcGraphTerrainRuntimeSceneRenderer</c>,
    /// <c>ArcGraphNpcRuntimeSceneRenderer</c> e
    /// <c>ArcGraphViewModeSwitcher</c> non sono presenti in Hierarchy, ArcGraph non
    /// puo' diventare la vista runtime principale. L'installer crea quindi solo
    /// GameObject e MonoBehaviour visuali, carica cataloghi da <c>Resources</c> e
    /// collega solo i bridge legacy ancora necessari alla scena. Non modifica il
    /// <c>World</c>, non invia comandi, non crea job, non decide nulla per gli NPC e
    /// non rende ArcGraph una sorgente parallela della simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RuntimeInitializeOnLoadMethod</b>: registra l'installer sulle scene caricate.</item>
    ///   <item><b>Controller root</b>: GameObject sempre attivo che contiene il controller vista.</item>
    ///   <item><b>Visual root</b>: GameObject attivato in modalita' ArcGraph.</item>
    ///   <item><b>Provider</b>: ponte read-only verso SimulationHost/World.</item>
    ///   <item><b>Wrapper e renderer</b>: percorso runtime minimo terrain + NPC.</item>
    ///   <item><b>Late binding</b>: pochi frame di ricontrollo per agganciare view/runtime creati dopo il load scena.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRuntimeSceneAutoInstaller : MonoBehaviour
    {
        private const string MapGridSceneName = "Scene_MapGrid";
        private const string ControllerRootName = "ArcGraphRuntimeController_Auto";
        private const string VisualRootName = "ArcGraphRuntimeVisualRoot_Auto";
        private const string GameParamsPath = "Arcontio/Config/game_params";
        private const string DefaultTerrainAtlasPath = "ArcGraph/Atlas/TerrainAtlas";
        private const string ViewConfigPath = ArcGraphMapViewConfigJson.DefaultResourcePath;
        private const string TerrainCatalogPath = "ArcGraph/Config/ArcGraphTerrainCatalog";
        private const string TerrainVisualCatalogPath = ArcGraphTerrainVisualCatalogJson.DefaultResourcePath;
        private const string NpcVisualCatalogPath = "ArcGraph/Config/ArcGraphNpcVisualCatalog";
        private const int LateBindFrameBudget = 240;

        private ArcGraphRuntimeWorldAdapter _contextProvider;
        private ArcGraphMinimalRuntimeSceneWrapper _wrapper;
        private ArcGraphTerrainRuntimeSceneRenderer _terrainRenderer;
        private ArcGraphNpcRuntimeSceneRenderer _npcRenderer;
        private ArcGraphObjectRuntimeSceneRenderer _objectRenderer;
        private ArcGraphCameraViewportController _cameraViewportController;
        private ArcGraphInteractionSceneAdapterWrapper _interactionWrapper;
        private ArcGraphInteractionConsumerRouter _interactionRouter;
        private ArcGraphPlacementToolController _placementToolController;
        private ArcGraphPointerCellHoverSceneConsumer _pointerCellHoverConsumer;
        private ArcGraphPlacementCellHighlightSceneConsumer _placementHighlightConsumer;
        private ArcGraphUiSelectionSceneConsumer _uiSelectionConsumer;
        private ArcGraphSelectionActionMenuSceneView _selectionActionMenu;
        private ArcUiSelectionActionController _selectionActionController;
        private ArcUiSimulationControlController _simulationControlController;
        private ArcUiVisualOverlayController _visualOverlayController;
        private ArcGraphRightInspectorSceneView _rightInspectorView;
        private ArcUiInspectionController _inspectionController;
        private ArcGraphSelectionSceneConsumer _selectionConsumer;
        private ArcGraphFovDebugOverlaySceneConsumer _fovOverlayConsumer;
        private ArcGraphFovDebugOverlayRuntimeController _fovOverlayController;
        private ArcGraphNpcSpriteResourceProbe _npcSpriteProbe;
        private ArcGraphSerializedSpriteResolver _spriteResolver;
        private ArcGraphUiRuntimeRoot _uiRoot;
        private ArcGraphViewModeSwitcher _switcher;
        private GameObject _visualRoot;
        private Material _terrainMaterial;
        private ArcGraphMapViewConfig _viewConfig;
        private int _lateBindFramesLeft;
        private int _configuredMapGridRootCount;
        private bool _installed;

        // =============================================================================
        // RegisterSceneHook
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra l'hook di caricamento scena prima che Unity apra le scene
        /// runtime.
        /// </para>
        ///
        /// <para><b>Accensione confinata</b></para>
        /// <para>
        /// L'hook non installa nulla nelle scene non MapGrid. Serve solo a evitare
        /// cablaggio manuale durante il gate visuale ArcGraph.
        /// </para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // =============================================================================
        // InstallOnCurrentSceneIfNeeded
        // =============================================================================
        /// <summary>
        /// <para>
        /// Gestisce il caso in cui la scena attiva sia gia' <c>Scene_MapGrid</c>
        /// quando il dominio runtime viene inizializzato.
        /// </para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnCurrentSceneIfNeeded()
        {
            TryInstallForScene(SceneManager.GetActiveScene());
        }

        // =============================================================================
        // OnSceneLoaded
        // =============================================================================
        /// <summary>
        /// <para>
        /// Callback Unity invocata quando una scena viene caricata.
        /// </para>
        /// </summary>
        private static void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            TryInstallForScene(scene);
        }

        // =============================================================================
        // TryInstallForScene
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il root installer solo nella scena MapGrid operativa.
        /// </para>
        /// </summary>
        private static void TryInstallForScene(Scene scene)
        {
            if (!scene.IsValid() || scene.name != MapGridSceneName)
                return;

            // Se esiste gia' il controller auto non creiamo doppioni. Questo evita
            // duplicazioni quando Unity richiama sia AfterSceneLoad sia sceneLoaded
            // durante lo stesso avvio.
            if (GameObject.Find(ControllerRootName) != null)
                return;

            var controllerRoot = new GameObject(ControllerRootName);
            controllerRoot.AddComponent<ArcGraphRuntimeSceneAutoInstaller>();
        }

        // =============================================================================
        // Awake
        // =============================================================================
        /// <summary>
        /// <para>
        /// Installa immediatamente il cablaggio runtime minimo.
        /// </para>
        /// </summary>
        private void Awake()
        {
            Install();
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricontrolla per pochi frame i riferimenti MapGrid che potrebbero essere
        /// creati dopo il caricamento scena.
        /// </para>
        ///
        /// <para><b>Late binding limitato</b></para>
        /// <para>
        /// Non facciamo ricerche globali per sempre. Il budget e' finito e serve
        /// solo ad attraversare l'avvio Unity, dove alcuni componenti legacy
        /// possono essere aggiunti da bootstrap dopo l'installazione ArcGraph.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!_installed || _lateBindFramesLeft <= 0)
                return;

            _lateBindFramesLeft--;
            RefreshRuntimeSceneBindings();
        }

        // =============================================================================
        // OnDestroy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rilascia il materiale runtime creato per il terrain renderer.
        /// </para>
        /// </summary>
        private void OnDestroy()
        {
            if (_terrainMaterial == null)
                return;

            Destroy(_terrainMaterial);
            _terrainMaterial = null;
        }

        // =============================================================================
        // Install
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea e collega adapter, wrapper, renderer, resolver e switcher.
        /// </para>
        /// </summary>
        private void Install()
        {
            if (_installed)
                return;

            // Il controller resta sempre attivo. Il visual root invece viene
            // acceso/spento dal controller vista, cosi' ArcGraph puo' diventare la
            // modalita' principale senza disattivare i componenti che processano
            // wrapper, renderer e interazione.
            _visualRoot = new GameObject(VisualRootName);
            _visualRoot.transform.SetParent(transform, false);
            _visualRoot.SetActive(false);

            _contextProvider = _visualRoot.AddComponent<ArcGraphRuntimeWorldAdapter>();
            _wrapper = _visualRoot.AddComponent<ArcGraphMinimalRuntimeSceneWrapper>();
            _terrainRenderer = _visualRoot.AddComponent<ArcGraphTerrainRuntimeSceneRenderer>();
            _npcRenderer = _visualRoot.AddComponent<ArcGraphNpcRuntimeSceneRenderer>();
            _objectRenderer = _visualRoot.AddComponent<ArcGraphObjectRuntimeSceneRenderer>();
            _cameraViewportController = _visualRoot.AddComponent<ArcGraphCameraViewportController>();
            _interactionWrapper = _visualRoot.AddComponent<ArcGraphInteractionSceneAdapterWrapper>();
            _interactionRouter = _visualRoot.AddComponent<ArcGraphInteractionConsumerRouter>();
            _placementToolController = gameObject.AddComponent<ArcGraphPlacementToolController>();
            _placementToolController.enabled = false;
            _pointerCellHoverConsumer = _visualRoot.AddComponent<ArcGraphPointerCellHoverSceneConsumer>();
            _placementHighlightConsumer = _visualRoot.AddComponent<ArcGraphPlacementCellHighlightSceneConsumer>();
            _uiSelectionConsumer = _visualRoot.AddComponent<ArcGraphUiSelectionSceneConsumer>();
            _selectionActionMenu = _visualRoot.AddComponent<ArcGraphSelectionActionMenuSceneView>();
            _selectionActionController = new ArcUiSelectionActionController();
            _simulationControlController = new ArcUiSimulationControlController();
            _visualOverlayController = new ArcUiVisualOverlayController();
            _rightInspectorView = _visualRoot.AddComponent<ArcGraphRightInspectorSceneView>();
            _inspectionController = new ArcUiInspectionController();
            _selectionConsumer = _visualRoot.AddComponent<ArcGraphSelectionSceneConsumer>();
            _fovOverlayConsumer = _visualRoot.AddComponent<ArcGraphFovDebugOverlaySceneConsumer>();
            _fovOverlayController = _visualRoot.AddComponent<ArcGraphFovDebugOverlayRuntimeController>();
            _npcSpriteProbe = _visualRoot.AddComponent<ArcGraphNpcSpriteResourceProbe>();
            _spriteResolver = _visualRoot.AddComponent<ArcGraphSerializedSpriteResolver>();
            _uiRoot = gameObject.AddComponent<ArcGraphUiRuntimeRoot>();
            _switcher = gameObject.AddComponent<ArcGraphViewModeSwitcher>();
            _switcher.ConfigureRuntimePolicy(
                ArcGraphViewMode.ArcGraph,
                enableKeyboardToggle: false);

            ConfigureAdapterAndRenderers();
            ConfigureUiRoot();
            ConfigureSwitcher();
            RefreshRuntimeSceneBindings();

            _lateBindFramesLeft = LateBindFrameBudget;
            _installed = true;

        }

        // =============================================================================
        // ConfigureAdapterAndRenderers
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna cataloghi, resolver, wrapper e materiale ai componenti ArcGraph.
        /// </para>
        /// </summary>
        private void ConfigureAdapterAndRenderers()
        {
            TextAsset viewConfigJson = Resources.Load<TextAsset>(ViewConfigPath);
            TextAsset terrainCatalog = Resources.Load<TextAsset>(TerrainCatalogPath);
            TextAsset terrainVisualCatalog = Resources.Load<TextAsset>(TerrainVisualCatalogPath);
            TextAsset npcVisualCatalog = Resources.Load<TextAsset>(NpcVisualCatalogPath);
            _viewConfig = ArcGraphMapViewConfigJson.ParseOrDefault(
                viewConfigJson != null ? viewConfigJson.text : null);

            _terrainMaterial = CreateTerrainMaterial();

            // Il terrain renderer riceve tutto in modo esplicito: provider, JSON e
            // materiale. Il materiale e' runtime-only e usa la texture atlas
            // esistente in Resources.
            _terrainRenderer.SetRuntimeContextProvider(_contextProvider);
            _terrainRenderer.SetTerrainMaterial(_terrainMaterial);
            _terrainRenderer.SetTerrainCatalogJson(terrainCatalog);
            _terrainRenderer.SetTerrainVisualCatalogJson(terrainVisualCatalog);

            // Il renderer NPC usa il resolver Resources, quindi gli sprite collocati
            // in Assets/Resources/ArcGraph/NPC/... vengono caricati dalla loro key.
            _npcRenderer.SetRuntimeWrapper(_wrapper);
            _npcRenderer.SetSpriteResolverBehaviour(_spriteResolver);
            _npcRenderer.SetNpcVisualCatalogJson(npcVisualCatalog);
            _npcRenderer.SetUseLayeredActorCatalog(true);
            _npcRenderer.SetRenderActorShadow(true);

            // Gli oggetti ArcGraph, inclusi i muri wall_stone, usano lo stesso
            // resolver scene-side. Il resolver supporta anche la forma
            // sheet#subSprite necessaria alla striscia muro 32x83 sliced.
            _objectRenderer.SetRuntimeWrapper(_wrapper);
            _objectRenderer.SetSpriteResolverBehaviour(_spriteResolver);

            // Il probe NPC resta passivo: viene solo preparato con lo stesso
            // catalogo e lo stesso resolver del renderer. Non gira in Update, non
            // disegna e non modifica scena; serve al prossimo gate per capire
            // subito quali parti/direzioni/animazioni siano coperte dagli asset.
            _npcSpriteProbe.SetNpcVisualCatalogJson(npcVisualCatalog);
            _npcSpriteProbe.SetSpriteResolverBehaviour(_spriteResolver);

            _wrapper.SetRuntimeContextProvider(_contextProvider);
            _wrapper.SetTerrainRenderer(_terrainRenderer);
            _wrapper.SetNpcRenderer(_npcRenderer);
            _wrapper.SetObjectRenderer(_objectRenderer);
            _wrapper.SetInteractionWrapper(_interactionWrapper);
            _wrapper.SetCameraViewportController(_cameraViewportController);
            _wrapper.SetViewConfig(_viewConfig);
            _cameraViewportController.SetConfig(_viewConfig);
            _cameraViewportController.SetSceneCamera(Camera.main);
            _cameraViewportController.SetControllerEnabled(true);
            _cameraViewportController.SetProcessInUpdate(false);
            _interactionWrapper.SetConsumer(_interactionRouter);
            _interactionWrapper.SetConfig(_viewConfig);
            _interactionWrapper.SetViewState(_cameraViewportController.ViewState);
            _interactionWrapper.SetViewInputEnabled(false);
            _interactionWrapper.SetSceneCamera(Camera.main);
            _interactionWrapper.SetSceneCameraZoomSyncEnabled(false);
            _interactionWrapper.SetDispatchToConsumer(true);
            _interactionRouter.SetRouterEnabled(true);
            _interactionRouter.SetRuntimeConsumers(
                _pointerCellHoverConsumer,
                _placementHighlightConsumer,
                _uiSelectionConsumer);
            _placementToolController.SetRuntimeContextProvider(_contextProvider);
            _placementToolController.SetSceneCamera(Camera.main);
            _pointerCellHoverConsumer.SetSceneCamera(Camera.main);
            _placementHighlightConsumer.SetRuntimeContextProvider(_contextProvider);
            _placementHighlightConsumer.SetSpriteResolverBehaviour(_spriteResolver);
            _placementHighlightConsumer.SetSceneCamera(Camera.main);
            _uiSelectionConsumer.SetRenderQueue(_wrapper.RenderQueue);
            _uiSelectionConsumer.SetSelectionEnabled(true);
            _selectionActionMenu.SetUiRoot(_uiRoot);
            _selectionActionMenu.SetSelectionConsumer(_uiSelectionConsumer);
            _selectionActionMenu.SetSelectionActionController(_selectionActionController);
            _selectionActionMenu.SetRenderQueue(_wrapper.RenderQueue);
            _selectionActionMenu.SetSceneCamera(Camera.main);
            _selectionActionMenu.SetMenuEnabled(true);
            _rightInspectorView.SetUiRoot(_uiRoot);
            _rightInspectorView.SetSelectionConsumer(_uiSelectionConsumer);
            _rightInspectorView.SetSelectionActionController(_selectionActionController);
            _rightInspectorView.SetRuntimeContextProvider(_contextProvider);
            _rightInspectorView.SetInspectionController(_inspectionController);
            _rightInspectorView.SetInspectorEnabled(true);
            _selectionConsumer.SetSelectionEnabled(false);

            // Il FOV debug ArcGraph usa la pipeline corretta:
            // UI -> controller -> provider World -> producer snapshot -> consumer.
            // Il bottone UI viene collegato in ConfigureUiRoot, non qui, per
            // mantenere distinta la shell UGUI dal rendering world-space.
            _fovOverlayController.SetRuntimeContextProvider(_contextProvider);
            _fovOverlayController.SetOverlayConsumer(_fovOverlayConsumer);
            _fovOverlayController.SetProcessInUpdate(true);
            _fovOverlayController.SetOverlayEnabled(false);
        }

        // =============================================================================
        // ConfigureUiRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la shell UI runtime ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: UI shell senza dati simulativi</b></para>
        /// <para>
        /// La UI viene cablata accanto al runtime visuale ArcGraph, ma non riceve
        /// <c>World</c>, <c>SimulationHost</c>, adapter MapGrid o command gateway.
        /// Questo primo step crea solo root, pannelli e archetipi provvisori: i
        /// futuri binder ViewModel e controller autorizzati verranno aggiunti in
        /// step successivi.
        /// </para>
        /// </summary>
        private void ConfigureUiRoot()
        {
            if (_uiRoot == null)
                return;

            _uiRoot.BuildRuntimeUi();
            _uiRoot.SetUiEnabled(true);
            ApplyUiMapViewportToMainCamera();
            _uiRoot.SetFovViewModeClicked(ToggleFovDebugOverlay);
            _uiRoot.SetVisualOverlayController(_visualOverlayController);
            _uiRoot.SetSimulationControlController(_simulationControlController);
        }

        // =============================================================================
        // ApplyUiMapViewportToMainCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sincronizza la camera principale con il rettangolo <c>MapViewport</c>
        /// costruito dalla shell UI ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: un solo rettangolo viewport</b></para>
        /// <para>
        /// La camera, il culling terrain e il picking devono lavorare dentro lo
        /// stesso rettangolo che la UI lascia libero alla mappa. Questo metodo non
        /// legge dati simulativi: collega solo due componenti view-side gia'
        /// autorizzati, evitando che ArcGraph venga disegnato sotto i pannelli.
        /// </para>
        /// </summary>
        private void ApplyUiMapViewportToMainCamera()
        {
            if (_uiRoot == null)
                return;

            _uiRoot.TryApplyMapViewportToCamera(Camera.main);
        }

        // =============================================================================
        // ToggleFovDebugOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Callback UI autorizzata per attivare/disattivare il FOV debug ArcGraph.
        /// </para>
        /// </summary>
        private void ToggleFovDebugOverlay()
        {
            _fovOverlayController?.ToggleOverlay();
        }

        // =============================================================================
        // ConfigureSwitcher
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara il controller vista con root MapGrid euristiche e root ArcGraph auto.
        /// </para>
        /// </summary>
        private void ConfigureSwitcher()
        {
            GameObject[] mapGridVisualRoots = FindMapGridVisualRoots();
            GameObject[] arcGraphVisualRoots = FindArcGraphVisualRoots();
            _configuredMapGridRootCount = mapGridVisualRoots.Length;

            _switcher.ConfigureRuntimeWiring(
                mapGridVisualRoots,
                arcGraphVisualRoots,
                _wrapper,
                _terrainRenderer,
                _npcRenderer,
                _objectRenderer);
        }

        // =============================================================================
        // FindArcGraphVisualRoots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce i root visuali ArcGraph controllabili dallo switcher.
        /// </para>
        ///
        /// <para><b>Principio architetturale: UI e rendering restano root separati</b></para>
        /// <para>
        /// Il renderer mappa e la UI runtime non vengono fusi nello stesso
        /// GameObject. Lo switcher puo' pero' trattarli come due root visuali della
        /// stessa vista ArcGraph, mantenendo separata la responsabilita' interna di
        /// ciascun blocco.
        /// </para>
        /// </summary>
        private GameObject[] FindArcGraphVisualRoots()
        {
            if (_uiRoot != null && _uiRoot.RootGameObject != null)
                return new[] { _visualRoot, _uiRoot.RootGameObject };

            return new[] { _visualRoot };
        }

        // =============================================================================
        // RefreshRuntimeSceneBindings
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna i riferimenti runtime e legacy ancora necessari alla scena.
        /// </para>
        /// </summary>
        private void RefreshRuntimeSceneBindings()
        {
            MapGridCameraController cameraController = FindSceneComponent<MapGridCameraController>();
            MapGridRuntimeDevToolsOverlay legacyPlacementOverlay = FindSceneComponent<MapGridRuntimeDevToolsOverlay>();
            MonoBehaviour placementPreviewSource =
                legacyPlacementOverlay != null
                    ? legacyPlacementOverlay
                    : _placementToolController != null
                        ? _placementToolController
                        : FindSceneBehaviourImplementing<IArcGraphPlacementPreviewSource>();
            Arcontio.Core.SimulationHost simulationHost = Arcontio.Core.SimulationHost.Instance;

            if (_contextProvider != null)
                _contextProvider.SetSimulationHost(simulationHost);

            if (_simulationControlController != null)
                _simulationControlController.SetSimulationHost(simulationHost);

            if (_uiRoot != null)
                _uiRoot.RefreshSimulationControlTopBar();

            RefreshViewConfigFromRuntimeContext();

            if (_placementToolController != null)
            {
                _placementToolController.SetRuntimeContextProvider(_contextProvider);
                _placementToolController.SetSceneCamera(Camera.main);
            }

            if (_placementHighlightConsumer != null)
                _placementHighlightConsumer.SetPlacementPreviewSource(placementPreviewSource);

            if (_cameraViewportController != null)
                _cameraViewportController.SetSceneCamera(Camera.main);

            if (_selectionActionMenu != null)
                _selectionActionMenu.SetSceneCamera(Camera.main);

            ApplyUiMapViewportToMainCamera();

            ConfigureLegacyMapGridCameraControllerForArcGraph(cameraController);
            ConfigureLegacyMapGridPlacementOverlayForArcGraph(legacyPlacementOverlay);

            // Anche i root visuali MapGrid possono essere creati dal bootstrap
            // legacy dopo l'installazione ArcGraph. Ricablare il controller per
            // pochi frame mantiene il soft retirement stabile rispetto all'ordine
            // di Start dei componenti legacy.
            if (_switcher != null && _visualRoot != null)
                ConfigureSwitcher();

        }

        // =============================================================================
        // RefreshViewConfigFromRuntimeContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riallinea la configurazione vista alle dimensioni reali del World.
        /// </para>
        ///
        /// <para><b>Principio architetturale: una sola fonte per la dimensione mappa</b></para>
        /// <para>
        /// <c>ArcGraphViewConfig.json</c> decide solo i parametri continui della
        /// camera e del pan, ma non deve piu' dichiarare la dimensione della mappa.
        /// Appena il <c>SimulationHost</c> espone un <c>World</c>, l'installer copia
        /// larghezza e altezza dal context runtime e propaga la config aggiornata a
        /// wrapper, camera e interazione.
        /// </para>
        /// </summary>
        private void RefreshViewConfigFromRuntimeContext()
        {
            if (_contextProvider == null || _viewConfig == null)
                return;

            ArcGraphRuntimeContext context = _contextProvider.BuildTerrainRuntimeContext();
            if (context == null
                || !context.HasWorld
                || context.MapWidthCells <= 0
                || context.MapHeightCells <= 0)
            {
                return;
            }

            if (_viewConfig.MapWidthCells == context.MapWidthCells
                && _viewConfig.MapHeightCells == context.MapHeightCells)
            {
                return;
            }

            _viewConfig = _viewConfig.WithMapDimensions(
                context.MapWidthCells,
                context.MapHeightCells);

            if (_wrapper != null)
                _wrapper.SetViewConfig(_viewConfig);

            if (_cameraViewportController != null)
                _cameraViewportController.SetConfig(_viewConfig);

            if (_interactionWrapper != null)
            {
                _interactionWrapper.SetConfig(_viewConfig);
                if (_cameraViewportController != null)
                    _interactionWrapper.SetViewState(_cameraViewportController.ViewState);
            }
        }

        // =============================================================================
        // ConfigureLegacyMapGridCameraControllerForArcGraph
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disabilita il controller camera legacy MapGrid quando ArcGraph controlla
        /// la camera runtime.
        /// </para>
        ///
        /// <para><b>Principio architetturale: una sola sorgente pan/zoom</b></para>
        /// <para>
        /// ArcGraph usa il proprio controller camera ortografico continuo. Il
        /// vecchio <c>MapGridCameraController</c> contiene pan fisico, zoom, target
        /// interno e inerzia legacy. Quando ArcGraph diventa vista runtime, quel
        /// componente viene spento per evitare due sorgenti concorrenti sullo stesso
        /// transform camera.
        /// </para>
        /// </summary>
        private static void ConfigureLegacyMapGridCameraControllerForArcGraph(
            MapGridCameraController cameraController)
        {
            if (cameraController == null)
                return;

            cameraController.SetZoomInputEnabled(false);
            cameraController.SetPanInputEnabled(false);
            cameraController.enabled = false;
        }

        // =============================================================================
        // ConfigureLegacyMapGridPlacementOverlayForArcGraph
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riattiva temporaneamente il DevTools placement legacy MapGrid mentre
        /// ArcGraph possiede la vista runtime.
        /// </para>
        ///
        /// <para><b>Principio architetturale: un solo lettore operativo di F3</b></para>
        /// <para>
        /// Il vecchio overlay MapGrid resta un debito da eliminare, ma in questo
        /// gate viene riesumato come strumento debug per leggere/scrivere mappe,
        /// piazzare food stock, porte e muri. La vista resta ArcGraph: il pannello
        /// legacy invia comunque comandi al <c>SimulationHost</c> e viene usato
        /// anche come sorgente preview tramite <c>IArcGraphPlacementPreviewSource</c>.
        /// </para>
        /// </summary>
        private static void ConfigureLegacyMapGridPlacementOverlayForArcGraph(
            MapGridRuntimeDevToolsOverlay legacyPlacementOverlay)
        {
            if (legacyPlacementOverlay == null)
                return;

            legacyPlacementOverlay.enabled = true;
        }

        // =============================================================================
        // FindMapGridVisualRoots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Trova i root visuali legacy piu' comuni da spegnere quando ArcGraph e'
        /// attivo.
        /// </para>
        ///
        /// <para><b>Euristica confinata al gate visuale</b></para>
        /// <para>
        /// Non spegniamo <c>MapGridRoot</c> perche' puo' contenere componenti
        /// runtime ancora necessari come sorgenti dati. Spegniamo invece root
        /// grafici noti, se presenti. Le entry mancanti vengono ignorate.
        /// </para>
        /// </summary>
        private static GameObject[] FindMapGridVisualRoots()
        {
            string[] names =
            {
                // MapGridBootstrap crea il terreno legacy sotto questo root.
                // Se resta attivo mentre ArcGraph e' la vista principale, a zoom
                // larghi possono riemergere vecchi chunk e layout diagnostici.
                "TerrainChunks",
                "GridRoot",
                "NPCViews",
                "ObjectViews",
                "FovHeatmapOverlay",
                "LandmarkOverlay",
                "LandmarkLabelOverlay",
                "DtValueOverlay",
                "GvdDinOverlay"
            };

            var roots = new List<GameObject>();
            for (int i = 0; i < names.Length; i++)
            {
                AddSceneGameObjectsByName(names[i], roots);
            }

            return roots.ToArray();
        }

        // =============================================================================
        // AddSceneGameObjectsByName
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge alla lista tutti i GameObject della scena attiva che hanno il
        /// nome richiesto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: spegnimento completo del legacy visuale</b></para>
        /// <para>
        /// Il vecchio percorso MapGrid puo' creare piu' root con lo stesso nome in
        /// momenti diversi. Il caso piu' evidente e' <c>NPCViews</c>: puo' esistere
        /// un root placeholder creato dal bootstrap e un root runtime creato dal
        /// <c>MapGridWorldView</c>. Se il controller vista ne spegne solo uno,
        /// entrando in ArcGraph resta visibile il vecchio sprite statico dell'NPC. Per questo
        /// l'auto-installer non cerca piu' il primo match, ma registra tutti i match
        /// legacy visuali trovati nella scena.
        /// </para>
        /// </summary>
        private static void AddSceneGameObjectsByName(
            string objectName,
            List<GameObject> results)
        {
            if (string.IsNullOrWhiteSpace(objectName) || results == null)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
                AddGameObjectsByNameRecursive(rootObjects[i].transform, objectName, results);
        }

        // =============================================================================
        // FindSceneComponent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un componente nella scena attiva includendo anche GameObject
        /// temporaneamente disattivati.
        /// </para>
        ///
        /// <para><b>Principio architetturale: vista ArcGraph con sorgenti legacy leggibili</b></para>
        /// <para>
        /// Durante la modalita' ArcGraph alcuni root MapGrid vengono spenti. Le API
        /// globali piu' semplici di Unity possono ignorare oggetti inattivi; se il
        /// late binding usasse solo quelle, i bridge legacy temporanei potrebbero
        /// perdere riferimenti ancora necessari. La scansione resta confinata alla
        /// scena attiva e viene usata solo nel breve budget di bootstrap.
        /// </para>
        /// </summary>
        private static T FindSceneComponent<T>()
            where T : Component
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                T component = rootObjects[i].GetComponentInChildren<T>(includeInactive: true);
                if (component != null)
                    return component;
            }

            return null;
        }

        // =============================================================================
        // FindSceneBehaviourImplementing
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un componente di scena che implementa una specifica interfaccia.
        /// </para>
        ///
        /// <para><b>Contratto bridge neutro</b></para>
        /// <para>
        /// Unity non serializza direttamente le interfacce come campi Inspector.
        /// Per questo l'installer scansiona i <c>MonoBehaviour</c> attivi e
        /// inattivi della scena e restituisce il primo componente compatibile. Il
        /// chiamante conserva il riferimento come <c>MonoBehaviour</c>, mentre il
        /// consumer ArcGraph lo converte al contratto read-only richiesto.
        /// </para>
        /// </summary>
        private static MonoBehaviour FindSceneBehaviourImplementing<TInterface>()
            where TInterface : class
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                MonoBehaviour[] behaviours =
                    rootObjects[i].GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

                for (int j = 0; j < behaviours.Length; j++)
                {
                    MonoBehaviour behaviour = behaviours[j];
                    if (behaviour is TInterface)
                        return behaviour;
                }
            }

            return null;
        }

        // =============================================================================
        // FindSceneGameObjectByName
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un GameObject per nome nella scena attiva, includendo i figli
        /// inattivi.
        /// </para>
        /// </summary>
        private static GameObject FindSceneGameObjectByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject found = FindGameObjectByNameRecursive(rootObjects[i].transform, objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        // =============================================================================
        // FindGameObjectByNameRecursive
        // =============================================================================
        /// <summary>
        /// <para>
        /// Attraversa ricorsivamente un sottoalbero Unity per trovare un nome
        /// specifico, senza dipendere dallo stato active del GameObject.
        /// </para>
        /// </summary>
        private static GameObject FindGameObjectByNameRecursive(
            Transform root,
            string objectName)
        {
            if (root == null)
                return null;

            if (root.name == objectName)
                return root.gameObject;

            for (int i = 0; i < root.childCount; i++)
            {
                GameObject found = FindGameObjectByNameRecursive(root.GetChild(i), objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        // =============================================================================
        // AddGameObjectsByNameRecursive
        // =============================================================================
        /// <summary>
        /// <para>
        /// Attraversa un sottoalbero Unity e aggiunge tutti i nodi con il nome
        /// richiesto, includendo oggetti inattivi.
        /// </para>
        /// </summary>
        private static void AddGameObjectsByNameRecursive(
            Transform root,
            string objectName,
            List<GameObject> results)
        {
            if (root == null || results == null)
                return;

            if (root.name == objectName && !results.Contains(root.gameObject))
                results.Add(root.gameObject);

            for (int i = 0; i < root.childCount; i++)
                AddGameObjectsByNameRecursive(root.GetChild(i), objectName, results);
        }

        // =============================================================================
        // CreateTerrainMaterial
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un materiale runtime minimale per disegnare la mesh terrain.
        /// </para>
        /// </summary>
        private static Material CreateTerrainMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            var material = new Material(shader)
            {
                name = "ArcGraphTerrainRuntimeMaterial_Auto"
            };

            string terrainAtlasPath = ResolveTerrainAtlasPathFromGameParams();
            Texture2D atlas = Resources.Load<Texture2D>(terrainAtlasPath);
            if (atlas != null)
            {
                // L'atlas resta un asset Resources. Il materiale runtime conserva
                // solo il riferimento alla texture, senza modificarla.
                material.mainTexture = atlas;
            }
            else
            {
                Debug.LogWarning(
                    "[ArcGraphRuntimeSceneAutoInstaller] Terrain atlas non trovato in Resources/" +
                    terrainAtlasPath + ".png");
            }

            return material;
        }

        // =============================================================================
        // ResolveTerrainAtlasPathFromGameParams
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge da <c>game_params</c> il path Resources dell'atlas terrain.
        /// </para>
        ///
        /// <para><b>Principio architetturale: configurazione runtime centralizzata</b></para>
        /// <para>
        /// Il materiale terrain non deve conservare un riferimento hardcoded al
        /// vecchio path MapGrid. La scelta dell'atlas resta dichiarativa dentro
        /// <c>game_params.json</c>; se la sezione ArcGraph manca o contiene un path
        /// vuoto, viene usato il default ArcGraph nativo.
        /// </para>
        /// </summary>
        private static string ResolveTerrainAtlasPathFromGameParams()
        {
            TextAsset gameParams = Resources.Load<TextAsset>(GameParamsPath);
            if (gameParams == null || string.IsNullOrWhiteSpace(gameParams.text))
                return DefaultTerrainAtlasPath;

            try
            {
                var dto = JsonUtility.FromJson<ArcGraphGameParamsDto>(gameParams.text);
                string path = dto != null && dto.arcGraph != null
                    ? dto.arcGraph.terrainAtlasResourcePath
                    : null;

                return string.IsNullOrWhiteSpace(path)
                    ? DefaultTerrainAtlasPath
                    : path.Trim();
            }
            catch
            {
                return DefaultTerrainAtlasPath;
            }
        }

        // =============================================================================
        // ArcGraphGameParamsDto
        // =============================================================================
        /// <summary>
        /// <para>
        /// DTO minimale usato solo per leggere la sezione ArcGraph di game_params.
        /// </para>
        ///
        /// <para><b>Confine JSON locale alla vista</b></para>
        /// <para>
        /// L'installer non ha bisogno di interpretare tutta la configurazione
        /// simulativa. Questo DTO prende solo il sotto-blocco visuale autorizzato e
        /// lascia invariati tutti gli altri campi letti dal Core.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class ArcGraphGameParamsDto
        {
            public ArcGraphGameParamsSectionDto arcGraph;
        }

        // =============================================================================
        // ArcGraphGameParamsSectionDto
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sotto-blocco ArcGraph serializzabile dentro <c>game_params.json</c>.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class ArcGraphGameParamsSectionDto
        {
            public string terrainAtlasResourcePath;
        }
    }
}
