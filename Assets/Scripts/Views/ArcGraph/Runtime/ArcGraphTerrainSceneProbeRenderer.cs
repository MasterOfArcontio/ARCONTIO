using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainSceneProbeRendererDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del probe scena terrain ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: gate visuale spiegabile</b></para>
    /// <para>
    /// Il probe terrain e' autorizzato a creare oggetti Unity temporanei, ma deve
    /// spiegare sempre quali prerequisiti erano presenti, quanti chunk sono stati
    /// costruiti e quanti oggetti scena sono stati creati. Questa struttura evita
    /// che il test visuale diventi un secondo renderer permanente non tracciato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasRuntimeAdapter</b>: provider runtime ArcGraph assegnato.</item>
    ///   <item><b>HasConfig/HasMap</b>: context terrain minimo disponibile.</item>
    ///   <item><b>HasCamera/HasMaterial</b>: prerequisiti dichiarati dal gate scena.</item>
    ///   <item><b>DidInitializeBootstrap</b>: bootstrap ArcGraph temporaneo riuscito.</item>
    ///   <item><b>BuiltChunkCount</b>: chunk mesh data prodotti dal builder.</item>
    ///   <item><b>CreatedSceneObjectCount</b>: GameObject temporanei creati sotto root.</item>
    ///   <item><b>Reason</b>: esito sintetico leggibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainSceneProbeRendererDiagnostics
    {
        public readonly bool HasRuntimeAdapter;
        public readonly bool HasConfig;
        public readonly bool HasMap;
        public readonly bool HasCamera;
        public readonly bool HasMaterial;
        public readonly bool DidInitializeBootstrap;
        public readonly bool HasTerrainLayer;
        public readonly bool GateAllowed;
        public readonly int TerrainSnapshotCount;
        public readonly int BuiltChunkCount;
        public readonly int NonEmptyChunkCount;
        public readonly int CreatedSceneObjectCount;
        public readonly bool UsedFallbackUv;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainSceneProbeRendererDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile del probe terrain scena.
        /// </para>
        ///
        /// <para><b>Snapshot dell'esito</b></para>
        /// <para>
        /// I valori vengono copiati in campi readonly, cosi' il risultato resta
        /// stabile dopo cleanup del root temporaneo o dopo dispose del bootstrap
        /// ArcGraph usato per costruire gli snapshot.
        /// </para>
        /// </summary>
        public ArcGraphTerrainSceneProbeRendererDiagnostics(
            bool hasRuntimeAdapter,
            bool hasConfig,
            bool hasMap,
            bool hasCamera,
            bool hasMaterial,
            bool didInitializeBootstrap,
            bool hasTerrainLayer,
            bool gateAllowed,
            int terrainSnapshotCount,
            int builtChunkCount,
            int nonEmptyChunkCount,
            int createdSceneObjectCount,
            bool usedFallbackUv,
            string reason)
        {
            HasRuntimeAdapter = hasRuntimeAdapter;
            HasConfig = hasConfig;
            HasMap = hasMap;
            HasCamera = hasCamera;
            HasMaterial = hasMaterial;
            DidInitializeBootstrap = didInitializeBootstrap;
            HasTerrainLayer = hasTerrainLayer;
            GateAllowed = gateAllowed;
            TerrainSnapshotCount = terrainSnapshotCount;
            BuiltChunkCount = builtChunkCount;
            NonEmptyChunkCount = nonEmptyChunkCount;
            CreatedSceneObjectCount = createdSceneObjectCount;
            UsedFallbackUv = usedFallbackUv;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainSceneProbeRenderer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Renderer probe temporaneo che visualizza il terrain ArcGraph costruito dalla
    /// MapGrid runtime corrente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: scena temporanea, non renderer produttivo</b></para>
    /// <para>
    /// Questo componente consuma il context read-only prodotto da
    /// <c>ArcGraphRuntimeContextProvider</c>, inizializza un
    /// <c>ArcGraphBootstrapRuntime</c> temporaneo, costruisce mesh data terrain e
    /// le applica a <c>GameObject</c> figli di un root dedicato. Non legge globali,
    /// non carica asset, non salva scene, non muta MapGrid e non sostituisce
    /// <c>MapGridChunkRenderer</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RenderTerrainSceneProbeFromMapGrid</b>: comando manuale del probe.</item>
    ///   <item><b>ClearProbe</b>: distrugge solo il root temporaneo e le mesh create.</item>
    ///   <item><b>CreateUvMap</b>: traduce le tileDefs MapGrid in UV map ArcGraph.</item>
    ///   <item><b>CreateChunkObject</b>: applica un chunk mesh data a GameObject temporaneo.</item>
    ///   <item><b>EvaluateGate</b>: blocca il probe se manca un prerequisito dichiarato.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainSceneProbeRenderer : MonoBehaviour
    {
        [SerializeField] private ArcGraphRuntimeContextProvider runtimeContextProvider;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private bool renderTerrainProbeOnStart;
        [SerializeField] private bool clearBeforeRender = true;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float probeZOffset = -0.05f;
        [SerializeField] private int terrainSortingOrder = -5;

        private const string ProbeRootName = "ArcGraphTerrainSceneProbeRoot";

        private Transform _root;
        private ArcGraphTerrainSceneProbeRendererDiagnostics _lastDiagnostics;

        public ArcGraphTerrainSceneProbeRendererDiagnostics LastDiagnostics => _lastDiagnostics;

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia opzionalmente il probe terrain.
        /// </para>
        ///
        /// <para><b>Attivazione esplicita</b></para>
        /// <para>
        /// Il flag serializzato e' falso di default. In una scena produttiva il
        /// terrain ArcGraph non deve accendersi da solo sopra MapGrid: serve un
        /// comando manuale o un consenso di debug esplicito.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (!renderTerrainProbeOnStart)
                return;

            RenderTerrainSceneProbeFromMapGrid();
        }

        // =============================================================================
        // RenderTerrainSceneProbeFromMapGridContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point void usato dal context menu Unity.
        /// </para>
        ///
        /// <para><b>Compatibilita' Inspector</b></para>
        /// <para>
        /// Unity richiama in modo piu' affidabile metodi senza parametri e senza
        /// valore di ritorno dai context menu. Il metodo operativo resta separato e
        /// restituisce diagnostica per eventuali harness futuri.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Render Terrain Scene Probe From MapGrid")]
        public void RenderTerrainSceneProbeFromMapGridContextMenu()
        {
            RenderTerrainSceneProbeFromMapGrid();
        }

        // =============================================================================
        // RenderTerrainSceneProbeFromMapGrid
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce e disegna il terrain ArcGraph partendo dal context MapGrid.
        /// </para>
        ///
        /// <para><b>Probe gated</b></para>
        /// <para>
        /// Il metodo valuta prima il gate comparativo: MapGrid resta primario, il
        /// doppio renderer permanente resta vietato e il probe puo' agganciarsi
        /// alla scena solo se camera, materiale e dati terrain sono disponibili.
        /// </para>
        /// </summary>
        public ArcGraphTerrainSceneProbeRendererDiagnostics RenderTerrainSceneProbeFromMapGrid()
        {
            ArcGraphRuntimeContext context = runtimeContextProvider != null
                ? runtimeContextProvider.BuildTerrainRuntimeContext()
                : ArcGraphRuntimeContext.Empty();

            ArcGraphComparisonDiagnostics gate = EvaluateGate(context);
            if (!gate.CanAttachSceneProbe)
                return StoreAndLogDiagnostics(context, false, false, gate.IsAllowed, 0, 0, 0, 0, false, gate.Reason);

            var runtime = new ArcGraphBootstrapRuntime();
            bool initialized = runtime.Initialize(
                context,
                ArcGraphBootstrapOptions.CreateDefault());

            if (!initialized)
            {
                var diagnostics = StoreAndLogDiagnostics(
                    context,
                    false,
                    false,
                    gate.IsAllowed,
                    0,
                    0,
                    0,
                    0,
                    false,
                    runtime.Diagnostics.Reason);

                runtime.Dispose();
                return diagnostics;
            }

            bool hasTerrainLayer = runtime.LayerStack.TryGetLayer<ArcGraphTerrainLayer>(out var terrainLayer);
            if (!hasTerrainLayer)
            {
                var diagnostics = StoreAndLogDiagnostics(
                    context,
                    true,
                    false,
                    gate.IsAllowed,
                    runtime.TerrainSnapshots.Count,
                    0,
                    0,
                    0,
                    false,
                    "TerrainLayerMissing");

                runtime.Dispose();
                return diagnostics;
            }

            List<ArcGraphTerrainChunkMeshData> chunks = BuildTerrainChunks(
                context,
                runtime.RenderState,
                terrainLayer);

            int createdObjects = RenderChunks(chunks);
            int nonEmptyChunks = CountNonEmptyChunks(chunks);
            bool usedFallbackUv = AnyChunkUsedFallbackUv(chunks);

            var result = StoreAndLogDiagnostics(
                context,
                true,
                true,
                gate.IsAllowed,
                runtime.TerrainSnapshots.Count,
                chunks.Count,
                nonEmptyChunks,
                createdObjects,
                usedFallbackUv,
                createdObjects > 0 ? "TerrainSceneProbeRendered" : "TerrainSceneProbeEmpty");

            runtime.Dispose();
            return result;
        }

        // =============================================================================
        // ClearProbe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Distrugge gli oggetti temporanei creati dal probe terrain.
        /// </para>
        ///
        /// <para><b>Cleanup confinato</b></para>
        /// <para>
        /// Il cleanup cerca solo il root dedicato del probe e distrugge le mesh
        /// runtime create sotto quel root. Non tocca i chunk MapGrid, la camera, il
        /// materiale assegnato da Inspector o oggetti esterni.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear Terrain Scene Probe")]
        public void ClearProbe()
        {
            if (_root == null)
                _root = FindExistingRoot();

            if (_root == null)
                return;

            DestroyMeshesInRoot(_root);
            DestroyProbeObject(_root.gameObject);
            _root = null;
        }

        // =============================================================================
        // BuildTerrainChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce mesh data terrain usando layer, render state e policy legacy.
        /// </para>
        ///
        /// <para><b>Conversione snapshot -> mesh data</b></para>
        /// <para>
        /// Il metodo resta nella fase dati: invoca il builder ArcGraph gia'
        /// esistente e prepara una UV map derivata dalla config MapGrid. I
        /// GameObject vengono creati solo dopo, in <c>RenderChunks</c>.
        /// </para>
        /// </summary>
        private List<ArcGraphTerrainChunkMeshData> BuildTerrainChunks(
            ArcGraphRuntimeContext context,
            ArcGraphRenderState renderState,
            ArcGraphTerrainLayer terrainLayer)
        {
            var builder = new ArcGraphTerrainChunkMeshBuilder();
            return builder.BuildDirtyChunks(
                terrainLayer,
                CreateUvMap(),
                renderState,
                ArcGraphTerrainVisualPolicy.CreateLegacyDefault());
        }

        // =============================================================================
        // RenderChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica i chunk mesh data a oggetti Unity temporanei.
        /// </para>
        ///
        /// <para><b>Applicazione scena controllata</b></para>
        /// <para>
        /// Ogni chunk non vuoto diventa un figlio del root probe. Il metodo non
        /// conserva cache produttive: al prossimo render, se richiesto, il root viene
        /// distrutto e ricreato.
        /// </para>
        /// </summary>
        private int RenderChunks(List<ArcGraphTerrainChunkMeshData> chunks)
        {
            if (chunks == null || chunks.Count == 0)
                return 0;

            if (clearBeforeRender)
                ClearProbe();

            EnsureRoot();

            int created = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                ArcGraphTerrainChunkMeshData chunk = chunks[i];
                if (chunk == null || chunk.IsEmpty)
                    continue;

                CreateChunkObject(chunk, i);
                created++;
            }

            return created;
        }

        // =============================================================================
        // CreateChunkObject
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il GameObject temporaneo di un singolo chunk terrain.
        /// </para>
        ///
        /// <para><b>Mesh runtime locale</b></para>
        /// <para>
        /// La mesh viene creata in memoria e assegnata al <c>MeshFilter</c> del
        /// GameObject probe. Non viene salvata come asset, non viene condivisa con
        /// MapGrid e viene distrutta nel cleanup del probe.
        /// </para>
        /// </summary>
        private void CreateChunkObject(
            ArcGraphTerrainChunkMeshData chunk,
            int index)
        {
            EnsureRoot();

            var go = new GameObject("TerrainChunk_" + chunk.Diagnostics.Chunk.X + "_" + chunk.Diagnostics.Chunk.Y + "_" + index);
            go.transform.SetParent(_root, false);

            var mesh = new Mesh
            {
                name = "ArcGraphTerrainProbeMesh_" + index
            };
            mesh.vertices = chunk.Vertices;
            mesh.uv = chunk.Uvs;
            mesh.triangles = chunk.Triangles;
            mesh.RecalculateBounds();

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = terrainMaterial;
            renderer.sortingOrder = terrainSortingOrder;
        }

        // =============================================================================
        // EvaluateGate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valuta se il probe terrain puo' agganciarsi temporaneamente alla scena.
        /// </para>
        ///
        /// <para><b>Uso del gate comparativo esistente</b></para>
        /// <para>
        /// Il metodo non inventa una nuova policy: riusa
        /// <c>ArcGraphComparisonGate</c> e dichiara i prerequisiti primitivi
        /// disponibili. Il materiale deve essere assegnato da Inspector.
        /// </para>
        /// </summary>
        private ArcGraphComparisonDiagnostics EvaluateGate(ArcGraphRuntimeContext context)
        {
            bool hasArcGraphTerrainData = context != null && context.HasMap;
            return ArcGraphComparisonGate.Evaluate(
                ArcGraphComparisonOptions.CreateTemporaryDebugSceneProbe(),
                hasLegacyRenderer: true,
                hasArcGraphTerrainData,
                hasCamera: sceneCamera != null,
                hasMaterial: terrainMaterial != null);
        }

        // =============================================================================
        // CreateUvMap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea la UV map ArcGraph partendo dal materiale assegnato.
        /// </para>
        ///
        /// <para><b>Fallback neutrale</b></para>
        /// <para>
        /// Il probe non legge piu' <c>MapGridConfig</c>. La copertura completa deve
        /// arrivare dai cataloghi ArcGraph; questo fallback crea solo una UV map
        /// minima quando il probe viene usato senza catalogo.
        /// </para>
        /// </summary>
        private ArcGraphTerrainTileUvMap CreateUvMap()
        {
            int tilePixels = 32;

            Texture texture = terrainMaterial != null
                ? terrainMaterial.mainTexture
                : null;

            int atlasWidth = texture != null
                ? texture.width
                : tilePixels;
            int atlasHeight = texture != null
                ? texture.height
                : tilePixels;

            return new ArcGraphTerrainTileUvMap(
                atlasWidth,
                atlasHeight,
                tilePixels);
        }

        // =============================================================================
        // StoreAndLogDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Salva e, se richiesto, stampa la diagnostica del probe.
        /// </para>
        ///
        /// <para><b>Esito unico</b></para>
        /// <para>
        /// Tutti i percorsi di uscita passano da qui, cosi' l'Inspector e la console
        /// mostrano sempre un risultato coerente anche in caso di blocco del gate.
        /// </para>
        /// </summary>
        private ArcGraphTerrainSceneProbeRendererDiagnostics StoreAndLogDiagnostics(
            ArcGraphRuntimeContext context,
            bool didInitializeBootstrap,
            bool hasTerrainLayer,
            bool gateAllowed,
            int terrainSnapshotCount,
            int builtChunkCount,
            int nonEmptyChunkCount,
            int createdSceneObjectCount,
            bool usedFallbackUv,
            string reason)
        {
            _lastDiagnostics = new ArcGraphTerrainSceneProbeRendererDiagnostics(
                runtimeContextProvider != null,
                context != null && context.HasConfig,
                context != null && context.HasMap,
                sceneCamera != null,
                terrainMaterial != null,
                didInitializeBootstrap,
                hasTerrainLayer,
                gateAllowed,
                terrainSnapshotCount,
                builtChunkCount,
                nonEmptyChunkCount,
                createdSceneObjectCount,
                usedFallbackUv,
                reason);

            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        // =============================================================================
        // EnsureRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Garantisce l'esistenza del root temporaneo del probe.
        /// </para>
        ///
        /// <para><b>Root confinato</b></para>
        /// <para>
        /// Il root viene creato come figlio del GameObject che ospita il componente.
        /// Tutti gli oggetti terrain temporanei devono vivere sotto questo nodo per
        /// rendere il cleanup semplice e non distruttivo.
        /// </para>
        /// </summary>
        private void EnsureRoot()
        {
            if (_root != null)
                return;

            _root = FindExistingRoot();
            if (_root != null)
                return;

            var go = new GameObject(ProbeRootName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = originOffset + new Vector3(0f, 0f, probeZOffset);
            _root = go.transform;
        }

        // =============================================================================
        // FindExistingRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un root probe gia' presente sotto il componente corrente.
        /// </para>
        ///
        /// <para><b>Nessuna ricerca globale</b></para>
        /// <para>
        /// Il metodo usa solo <c>transform.Find</c> sul parent locale. Non cerca
        /// nella scena intera e non rischia di toccare probe appartenenti ad altri
        /// GameObject.
        /// </para>
        /// </summary>
        private Transform FindExistingRoot()
        {
            return transform.Find(ProbeRootName);
        }

        // =============================================================================
        // LogLastDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive in console l'ultima diagnostica del probe terrain.
        /// </para>
        ///
        /// <para><b>Log opzionale</b></para>
        /// <para>
        /// Il log resta disattivabile da Inspector. Non alimenta decisioni runtime e
        /// non modifica lo stato del probe: serve solo al gate manuale.
        /// </para>
        /// </summary>
        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphTerrainSceneProbe] " + _lastDiagnostics.Reason +
                " adapter=" + _lastDiagnostics.HasRuntimeAdapter +
                ", config=" + _lastDiagnostics.HasConfig +
                ", map=" + _lastDiagnostics.HasMap +
                ", camera=" + _lastDiagnostics.HasCamera +
                ", material=" + _lastDiagnostics.HasMaterial +
                ", initialized=" + _lastDiagnostics.DidInitializeBootstrap +
                ", terrainLayer=" + _lastDiagnostics.HasTerrainLayer +
                ", gateAllowed=" + _lastDiagnostics.GateAllowed +
                ", snapshots=" + _lastDiagnostics.TerrainSnapshotCount +
                ", chunks=" + _lastDiagnostics.BuiltChunkCount +
                ", nonEmptyChunks=" + _lastDiagnostics.NonEmptyChunkCount +
                ", sceneObjects=" + _lastDiagnostics.CreatedSceneObjectCount +
                ", fallbackUv=" + _lastDiagnostics.UsedFallbackUv);
        }

        // =============================================================================
        // CountNonEmptyChunks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Conta i chunk mesh data che contengono almeno una cella disegnata.
        /// </para>
        ///
        /// <para><b>Diagnostica chunk</b></para>
        /// <para>
        /// Il conteggio separa i chunk richiesti dal builder dai chunk realmente
        /// utili alla scena. Questa distinzione aiuta a capire se il dirty state ha
        /// prodotto richieste vuote.
        /// </para>
        /// </summary>
        private static int CountNonEmptyChunks(List<ArcGraphTerrainChunkMeshData> chunks)
        {
            if (chunks == null)
                return 0;

            int count = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i] != null && !chunks[i].IsEmpty)
                    count++;
            }

            return count;
        }

        // =============================================================================
        // AnyChunkUsedFallbackUv
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se almeno un chunk ha usato UV fallback.
        /// </para>
        ///
        /// <para><b>Controllo atlante</b></para>
        /// <para>
        /// Un fallback UV non blocca il probe, ma indica che alcune tile non sono
        /// registrate nella config o che la policy visuale sta chiedendo id non
        /// presenti. Il gate visuale dovra' guardare questo valore.
        /// </para>
        /// </summary>
        private static bool AnyChunkUsedFallbackUv(List<ArcGraphTerrainChunkMeshData> chunks)
        {
            if (chunks == null)
                return false;

            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i]?.Diagnostics.UsedFallbackUv == true)
                    return true;
            }

            return false;
        }

        // =============================================================================
        // DestroyMeshesInRoot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Distrugge le mesh runtime assegnate ai figli del root probe.
        /// </para>
        ///
        /// <para><b>Cleanup asset runtime</b></para>
        /// <para>
        /// Le mesh create dal probe non sono asset di progetto. Prima di distruggere
        /// il root, il metodo sgancia e distrugge ogni mesh runtime per evitare
        /// residui in memoria durante test ripetuti.
        /// </para>
        /// </summary>
        private static void DestroyMeshesInRoot(Transform root)
        {
            if (root == null)
                return;

            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(includeInactive: true);
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i] != null
                    ? filters[i].sharedMesh
                    : null;

                if (mesh == null)
                    continue;

                filters[i].sharedMesh = null;
                DestroyProbeObject(mesh);
            }
        }

        // =============================================================================
        // DestroyProbeObject
        // =============================================================================
        /// <summary>
        /// <para>
        /// Distrugge un oggetto Unity creato dal probe nel modo adatto al contesto.
        /// </para>
        ///
        /// <para><b>Runtime o editor</b></para>
        /// <para>
        /// In Play Mode usa <c>Destroy</c>; fuori Play Mode usa
        /// <c>DestroyImmediate</c>. Il metodo e' confinato agli oggetti creati dal
        /// probe e non deve essere usato per elementi esterni.
        /// </para>
        /// </summary>
        private static void DestroyProbeObject(Object unityObject)
        {
            if (unityObject == null)
                return;

            if (Application.isPlaying)
                Destroy(unityObject);
            else
                DestroyImmediate(unityObject);
        }

        // =============================================================================
        // OnDestroy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Pulisce il probe quando il componente viene distrutto.
        /// </para>
        ///
        /// <para><b>Chiusura del ciclo temporaneo</b></para>
        /// <para>
        /// Se l'oggetto che ospita il probe viene rimosso, anche il root temporaneo
        /// e le mesh runtime vengono eliminati. Non vengono toccati MapGrid,
        /// materiali assegnati da Inspector o dati simulativi.
        /// </para>
        /// </summary>
        private void OnDestroy()
        {
            ClearProbe();
        }
    }
}
