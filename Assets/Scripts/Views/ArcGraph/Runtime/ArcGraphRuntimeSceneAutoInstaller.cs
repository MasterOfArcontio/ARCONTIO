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
        private const string TerrainAtlasPath = "MapGrid/Atlas/TerrainAtlas";
        private const string ViewConfigPath = ArcGraphMapViewConfigJson.DefaultResourcePath;
        private const string TerrainCatalogPath = "ArcGraph/Config/ArcGraphTerrainCatalog";
        private const string TerrainVisualCatalogPath = ArcGraphTerrainVisualCatalogJson.DefaultResourcePath;
        private const string NpcVisualCatalogPath = "ArcGraph/Config/ArcGraphNpcVisualCatalog";
        private const int LateBindFrameBudget = 240;

        [SerializeField] private bool logDiagnostics;

        private ArcGraphRuntimeWorldAdapter _contextProvider;
        private ArcGraphMinimalRuntimeSceneWrapper _wrapper;
        private ArcGraphTerrainRuntimeSceneRenderer _terrainRenderer;
        private ArcGraphNpcRuntimeSceneRenderer _npcRenderer;
        private ArcGraphObjectRuntimeSceneRenderer _objectRenderer;
        private ArcGraphCameraViewportController _cameraViewportController;
        private ArcGraphInteractionSceneAdapterWrapper _interactionWrapper;
        private ArcGraphInteractionConsumerRouter _interactionRouter;
        private ArcGraphPlacementCellHighlightSceneConsumer _placementHighlightConsumer;
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
        private int _lastMapGridRootCount = -1;
        private bool _lastHadDevToolsOverlay;
        private bool _lastHadWorld;
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
            _placementHighlightConsumer = _visualRoot.AddComponent<ArcGraphPlacementCellHighlightSceneConsumer>();
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

            if (logDiagnostics)
            {
                Debug.Log(
                    "[ArcGraphRuntimeSceneAutoInstaller] Installed runtime ArcGraph wiring. " +
                    "ArcGraph e' la vista runtime iniziale; toggle F12 disattivato.");
            }
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
            _interactionRouter.SetRouterEnabled(true);
            _interactionRouter.SetRuntimeConsumers(_placementHighlightConsumer);
            _placementHighlightConsumer.SetSpriteResolverBehaviour(_spriteResolver);
            _placementHighlightConsumer.SetSceneCamera(Camera.main);

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
            _uiRoot.SetFovViewModeClicked(ToggleFovDebugOverlay);
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
            MapGridRuntimeDevToolsOverlay devToolsOverlay = FindSceneComponent<MapGridRuntimeDevToolsOverlay>();
            MapGridCameraController cameraController = FindSceneComponent<MapGridCameraController>();
            Arcontio.Core.SimulationHost simulationHost = Arcontio.Core.SimulationHost.Instance;

            if (_contextProvider != null)
                _contextProvider.SetSimulationHost(simulationHost);

            if (_placementHighlightConsumer != null)
                _placementHighlightConsumer.SetDevToolsOverlay(devToolsOverlay);

            if (_cameraViewportController != null)
                _cameraViewportController.SetSceneCamera(Camera.main);

            ConfigureLegacyMapGridCameraControllerForArcGraph(cameraController);

            // Anche i root visuali MapGrid possono essere creati dal bootstrap
            // legacy dopo l'installazione ArcGraph. Ricablare il controller per
            // pochi frame mantiene il soft retirement stabile rispetto all'ordine
            // di Start dei componenti legacy.
            if (_switcher != null && _visualRoot != null)
                ConfigureSwitcher();

            LogBindingStateIfChanged(simulationHost, devToolsOverlay);
        }

        // =============================================================================
        // ConfigureLegacyMapGridCameraControllerForArcGraph
        // =============================================================================
        /// <summary>
        /// <para>
        /// Configura il controller camera legacy MapGrid come bridge camera
        /// provvisorio per la vista runtime ArcGraph.
        /// </para>
        ///
        /// <para><b>Principio architetturale: assorbimento parziale del legacy</b></para>
        /// <para>
        /// ArcGraph usa <c>ArcGraphViewConfig</c> e <c>ArcGraphViewState</c> per
        /// lavorare con livelli zoom discreti. Il vecchio
        /// <c>MapGridCameraController</c> contiene pero' anche il pan fisico della
        /// camera. Ora pero' il pan operativo e' stato assorbito nel controller
        /// logico ArcGraph: il componente legacy resta acceso solo per ricevere
        /// offset esterni e tenere allineato il proprio target interno. Spegniamo
        /// quindi sia la rotellina sia la lettura diretta del pan legacy, evitando
        /// due sorgenti concorrenti sullo stesso transform camera.
        /// </para>
        /// </summary>
        private static void ConfigureLegacyMapGridCameraControllerForArcGraph(
            MapGridCameraController cameraController)
        {
            if (cameraController == null)
                return;

            if (!cameraController.enabled)
                cameraController.enabled = true;

            cameraController.SetZoomInputEnabled(false);
            cameraController.SetPanInputEnabled(false);
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
        // LogBindingStateIfChanged
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive diagnostica di binding solo quando cambia qualcosa di rilevante.
        /// </para>
        /// </summary>
        private void LogBindingStateIfChanged(
            Arcontio.Core.SimulationHost simulationHost,
            MapGridRuntimeDevToolsOverlay devToolsOverlay)
        {
            if (!logDiagnostics)
                return;

            bool hasDevToolsOverlay = devToolsOverlay != null;
            bool hasWorld = simulationHost != null && simulationHost.World != null;
            int rootCount = _configuredMapGridRootCount;

            if (_lastMapGridRootCount == rootCount
                && _lastHadDevToolsOverlay == hasDevToolsOverlay
                && _lastHadWorld == hasWorld)
            {
                return;
            }

            _lastMapGridRootCount = rootCount;
            _lastHadDevToolsOverlay = hasDevToolsOverlay;
            _lastHadWorld = hasWorld;

            Debug.Log(
                "[ArcGraphRuntimeSceneAutoInstaller] BindingState " +
                "mapGridRoots=" + rootCount +
                ", world=" + hasWorld +
                ", devToolsOverlay=" + hasDevToolsOverlay +
                ", visualRoot=" + (_visualRoot != null));
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

            Texture2D atlas = Resources.Load<Texture2D>(TerrainAtlasPath);
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
                    TerrainAtlasPath + ".png");
            }

            return material;
        }
    }
}
