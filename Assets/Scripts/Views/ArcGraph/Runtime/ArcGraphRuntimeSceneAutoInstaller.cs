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
    /// necessari al test F12.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bootstrap visuale di bordo, non simulazione</b></para>
    /// <para>
    /// Questo componente risolve un problema pratico di scena: se
    /// <c>ArcGraphMinimalRuntimeSceneWrapper</c>,
    /// <c>ArcGraphTerrainRuntimeSceneRenderer</c>,
    /// <c>ArcGraphNpcRuntimeSceneRenderer</c> e
    /// <c>ArcGraphViewModeSwitcher</c> non sono presenti in Hierarchy, il tasto F12
    /// non puo' produrre alcun effetto. L'installer crea quindi solo GameObject e
    /// MonoBehaviour visuali, carica cataloghi da <c>Resources</c> e collega
    /// riferimenti gia' esistenti della MapGrid. Non modifica il <c>World</c>, non
    /// invia comandi, non crea job, non decide nulla per gli NPC e non rende
    /// ArcGraph una sorgente parallela della simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RuntimeInitializeOnLoadMethod</b>: registra l'installer sulle scene caricate.</item>
    ///   <item><b>Controller root</b>: GameObject sempre attivo che contiene lo switch F12.</item>
    ///   <item><b>Visual root</b>: GameObject attivato solo in modalita' ArcGraph.</item>
    ///   <item><b>Adapter</b>: ponte read-only verso MapGridBootstrap e MapGridWorldView.</item>
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
        private const string TerrainCatalogPath = "ArcGraph/Config/ArcGraphTerrainCatalog";
        private const string TerrainVisualCatalogPath = "ArcGraph/Config/ArcGraphTerrainVisualCatalog";
        private const string NpcVisualCatalogPath = "ArcGraph/Config/ArcGraphNpcVisualCatalog";
        private const int LateBindFrameBudget = 240;

        private ArcGraphTerrainRuntimeMapGridAdapter _adapter;
        private ArcGraphMinimalRuntimeSceneWrapper _wrapper;
        private ArcGraphTerrainRuntimeSceneRenderer _terrainRenderer;
        private ArcGraphNpcRuntimeSceneRenderer _npcRenderer;
        private ArcGraphObjectRuntimeSceneRenderer _objectRenderer;
        private ArcGraphInteractionSceneAdapterWrapper _interactionWrapper;
        private ArcGraphInteractionConsumerRouter _interactionRouter;
        private ArcGraphPlacementCellHighlightSceneConsumer _placementHighlightConsumer;
        private ArcGraphNpcSpriteResourceProbe _npcSpriteProbe;
        private ArcGraphSerializedSpriteResolver _spriteResolver;
        private ArcGraphViewModeSwitcher _switcher;
        private GameObject _visualRoot;
        private Material _terrainMaterial;
        private int _lateBindFramesLeft;
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
            RefreshMapGridSources();
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
            // acceso/spento dallo switcher F12, cosi' ArcGraph puo' essere nascosto
            // senza disattivare il componente che ascolta il tasto.
            _visualRoot = new GameObject(VisualRootName);
            _visualRoot.transform.SetParent(transform, false);
            _visualRoot.SetActive(false);

            _adapter = _visualRoot.AddComponent<ArcGraphTerrainRuntimeMapGridAdapter>();
            _wrapper = _visualRoot.AddComponent<ArcGraphMinimalRuntimeSceneWrapper>();
            _terrainRenderer = _visualRoot.AddComponent<ArcGraphTerrainRuntimeSceneRenderer>();
            _npcRenderer = _visualRoot.AddComponent<ArcGraphNpcRuntimeSceneRenderer>();
            _objectRenderer = _visualRoot.AddComponent<ArcGraphObjectRuntimeSceneRenderer>();
            _interactionWrapper = _visualRoot.AddComponent<ArcGraphInteractionSceneAdapterWrapper>();
            _interactionRouter = _visualRoot.AddComponent<ArcGraphInteractionConsumerRouter>();
            _placementHighlightConsumer = _visualRoot.AddComponent<ArcGraphPlacementCellHighlightSceneConsumer>();
            _npcSpriteProbe = _visualRoot.AddComponent<ArcGraphNpcSpriteResourceProbe>();
            _spriteResolver = _visualRoot.AddComponent<ArcGraphSerializedSpriteResolver>();
            _switcher = gameObject.AddComponent<ArcGraphViewModeSwitcher>();

            ConfigureAdapterAndRenderers();
            ConfigureSwitcher();
            RefreshMapGridSources();

            _lateBindFramesLeft = LateBindFrameBudget;
            _installed = true;

            Debug.Log(
                "[ArcGraphRuntimeSceneAutoInstaller] Installed runtime ArcGraph wiring. " +
                "Premi F12 per alternare MapGrid/ArcGraph.");
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
            TextAsset terrainCatalog = Resources.Load<TextAsset>(TerrainCatalogPath);
            TextAsset terrainVisualCatalog = Resources.Load<TextAsset>(TerrainVisualCatalogPath);
            TextAsset npcVisualCatalog = Resources.Load<TextAsset>(NpcVisualCatalogPath);

            _terrainMaterial = CreateTerrainMaterial();

            // Il terrain renderer riceve tutto in modo esplicito: adapter, JSON e
            // materiale. Il materiale e' runtime-only e usa la texture atlas
            // esistente in Resources.
            _terrainRenderer.SetRuntimeMapAdapter(_adapter);
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

            _wrapper.SetRuntimeMapAdapter(_adapter);
            _wrapper.SetTerrainRenderer(_terrainRenderer);
            _wrapper.SetNpcRenderer(_npcRenderer);
            _wrapper.SetObjectRenderer(_objectRenderer);
            _wrapper.SetInteractionWrapper(_interactionWrapper);
            _interactionWrapper.SetConsumer(_interactionRouter);
            _interactionWrapper.SetConfig(ArcGraphMapViewConfig.CreateDefaultV033());
            _interactionRouter.SetRouterEnabled(true);
            _interactionRouter.SetRuntimeConsumers(_placementHighlightConsumer);
        }

        // =============================================================================
        // ConfigureSwitcher
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara lo switcher F12 con root MapGrid euristiche e root ArcGraph auto.
        /// </para>
        /// </summary>
        private void ConfigureSwitcher()
        {
            GameObject[] mapGridVisualRoots = FindMapGridVisualRoots();
            GameObject[] arcGraphVisualRoots = { _visualRoot };

            _switcher.ConfigureRuntimeWiring(
                mapGridVisualRoots,
                arcGraphVisualRoots,
                _wrapper,
                _terrainRenderer,
                _npcRenderer,
                _objectRenderer);
        }

        // =============================================================================
        // RefreshMapGridSources
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna i riferimenti MapGrid usati dall'adapter terrain/runtime.
        /// </para>
        /// </summary>
        private void RefreshMapGridSources()
        {
            MapGridBootstrap bootstrap = FindObjectOfType<MapGridBootstrap>();
            MapGridWorldView worldView = FindObjectOfType<MapGridWorldView>();
            MapGridRuntimeDevToolsOverlay devToolsOverlay = FindObjectOfType<MapGridRuntimeDevToolsOverlay>();

            if (_adapter != null)
                _adapter.SetMapGridSources(bootstrap, worldView);

            if (_placementHighlightConsumer != null)
                _placementHighlightConsumer.SetDevToolsOverlay(devToolsOverlay);

            // Anche i root visuali MapGrid possono essere creati dal bootstrap
            // legacy dopo l'installazione ArcGraph. Ricablare lo switcher per pochi
            // frame rende il gate F12 meno dipendente dall'ordine di Start.
            if (_switcher != null && _visualRoot != null)
                ConfigureSwitcher();
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
                GameObject root = GameObject.Find(names[i]);
                if (root != null)
                    roots.Add(root);
            }

            return roots.ToArray();
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
