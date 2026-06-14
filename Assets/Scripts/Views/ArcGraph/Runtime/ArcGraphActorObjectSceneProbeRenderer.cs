using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorObjectSceneProbeRendererDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del probe scena actor/object ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: probe temporaneo spiegabile</b></para>
    /// <para>
    /// Il probe actor/object puo' creare <c>SpriteRenderer</c> temporanei, quindi
    /// deve rendere visibili i prerequisiti e i contatori principali. Questa
    /// struttura spiega se esisteva il context runtime, se il bootstrap ArcGraph ha
    /// prodotto layer actor/object, quante entry sono state pianificate e quanti
    /// oggetti scena sono stati creati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasRuntimeAdapter</b>: adapter MapGrid -> ArcGraph assegnato.</item>
    ///   <item><b>HasWorld</b>: context con World disponibile.</item>
    ///   <item><b>DidInitializeBootstrap</b>: bootstrap ArcGraph temporaneo riuscito.</item>
    ///   <item><b>HasActorLayer/HasObjectLayer</b>: layer necessari disponibili.</item>
    ///   <item><b>PlannedEntryCount</b>: entry scene-side prodotte dal plan.</item>
    ///   <item><b>CreatedSceneObjectCount</b>: GameObject temporanei creati.</item>
    ///   <item><b>Reason</b>: esito sintetico leggibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorObjectSceneProbeRendererDiagnostics
    {
        public readonly bool HasRuntimeAdapter;
        public readonly bool HasWorld;
        public readonly bool DidInitializeBootstrap;
        public readonly bool HasActorLayer;
        public readonly bool HasObjectLayer;
        public readonly bool HasSpriteResolver;
        public readonly bool UsedGeneratedFallbackSprite;
        public readonly bool ContractSafe;
        public readonly int QueueEntryCount;
        public readonly int PlannedEntryCount;
        public readonly int CreatedSceneObjectCount;
        public readonly int MissingSpriteCount;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphActorObjectSceneProbeRendererDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile del probe scena actor/object.
        /// </para>
        /// </summary>
        public ArcGraphActorObjectSceneProbeRendererDiagnostics(
            bool hasRuntimeAdapter,
            bool hasWorld,
            bool didInitializeBootstrap,
            bool hasActorLayer,
            bool hasObjectLayer,
            bool hasSpriteResolver,
            bool usedGeneratedFallbackSprite,
            bool contractSafe,
            int queueEntryCount,
            int plannedEntryCount,
            int createdSceneObjectCount,
            int missingSpriteCount,
            string reason)
        {
            HasRuntimeAdapter = hasRuntimeAdapter;
            HasWorld = hasWorld;
            DidInitializeBootstrap = didInitializeBootstrap;
            HasActorLayer = hasActorLayer;
            HasObjectLayer = hasObjectLayer;
            HasSpriteResolver = hasSpriteResolver;
            UsedGeneratedFallbackSprite = usedGeneratedFallbackSprite;
            ContractSafe = contractSafe;
            QueueEntryCount = queueEntryCount < 0 ? 0 : queueEntryCount;
            PlannedEntryCount = plannedEntryCount < 0 ? 0 : plannedEntryCount;
            CreatedSceneObjectCount = createdSceneObjectCount < 0 ? 0 : createdSceneObjectCount;
            MissingSpriteCount = missingSpriteCount < 0 ? 0 : missingSpriteCount;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphActorObjectSceneProbeRenderer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Renderer probe temporaneo per visualizzare actor e oggetti runtime tramite
    /// la pipeline ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: materializzazione temporanea della queue</b></para>
    /// <para>
    /// Il componente consuma il context read-only prodotto da
    /// <c>ArcGraphTerrainRuntimeMapGridAdapter</c>, inizializza un
    /// <c>ArcGraphBootstrapRuntime</c> temporaneo, costruisce
    /// <c>ArcGraphRenderQueue</c>, la converte in <c>ArcGraphActorObjectSceneRenderPlan</c>
    /// e crea <c>SpriteRenderer</c> solo sotto un root dedicato. Non legge globali,
    /// non salva scena, non modifica MapGrid, non migra input/UI e non sostituisce
    /// ancora <c>MapGridWorldView</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RenderActorObjectSceneProbeFromMapGrid</b>: comando manuale del probe.</item>
    ///   <item><b>ClearProbe</b>: rimuove solo root temporaneo e sprite generati.</item>
    ///   <item><b>CreateSceneObject</b>: materializza una entry del plan.</item>
    ///   <item><b>ResolveSprite</b>: usa resolver serializzato o fallback generato.</item>
    ///   <item><b>BuildDiagnostics</b>: produce esito leggibile del probe.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphActorObjectSceneProbeRenderer : MonoBehaviour
    {
        [SerializeField] private ArcGraphTerrainRuntimeMapGridAdapter runtimeMapAdapter;
        [SerializeField] private MonoBehaviour spriteResolverBehaviour;
        [SerializeField] private bool renderProbeOnStart;
        [SerializeField] private bool clearBeforeRender = true;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private bool allowGeneratedFallbackSprites = true;
        [SerializeField] private int zoomLevel = 4;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float actorScale = 0.85f;
        [SerializeField] private float objectScale = 0.75f;
        [SerializeField] private float probeZOffset = 0.02f;

        private const string ProbeRootName = "ArcGraphActorObjectSceneProbeRoot";

        private Transform _root;
        private Sprite _generatedActorSprite;
        private Sprite _generatedObjectSprite;
        private Texture2D _generatedActorTexture;
        private Texture2D _generatedObjectTexture;
        private ArcGraphActorObjectSceneProbeRendererDiagnostics _lastDiagnostics;

        public ArcGraphActorObjectSceneProbeRendererDiagnostics LastDiagnostics => _lastDiagnostics;

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia opzionalmente il probe actor/object.
        /// </para>
        ///
        /// <para><b>Default spento</b></para>
        /// <para>
        /// Il flag e' falso di default. Il probe non deve accendersi in modo
        /// implicito sopra MapGrid: serve scelta manuale da Inspector o context menu.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (!renderProbeOnStart)
                return;

            RenderActorObjectSceneProbeFromMapGrid();
        }

        // =============================================================================
        // RenderActorObjectSceneProbeFromMapGridContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point void per il context menu Unity.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Render Actor Object Scene Probe From MapGrid")]
        public void RenderActorObjectSceneProbeFromMapGridContextMenu()
        {
            RenderActorObjectSceneProbeFromMapGrid();
        }

        // =============================================================================
        // RenderActorObjectSceneProbeFromMapGrid
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce e disegna il probe actor/object dalla runtime MapGrid corrente.
        /// </para>
        ///
        /// <para><b>Pipeline temporanea</b></para>
        /// <para>
        /// Il metodo crea un bootstrap ArcGraph solo in memoria. Gli unici oggetti
        /// scena prodotti sono figli del root temporaneo del probe, cancellabile con
        /// il context menu di cleanup.
        /// </para>
        /// </summary>
        public ArcGraphActorObjectSceneProbeRendererDiagnostics RenderActorObjectSceneProbeFromMapGrid()
        {
            if (clearBeforeRender)
                ClearProbe();

            ArcGraphRuntimeContext context = runtimeMapAdapter != null
                ? runtimeMapAdapter.BuildTerrainRuntimeContext()
                : ArcGraphRuntimeContext.Empty();

            var contract = ArcGraphActorObjectSceneRendererContract.CreateTemporaryProbeContract(
                ResolveTileWorldSize(context));

            IArcGraphSpriteResolver spriteResolver = ResolveSpriteResolver();
            bool hasSpriteResolver = spriteResolver != null;

            if (runtimeMapAdapter == null || context == null || !context.HasWorld || !contract.IsTemporaryProbeSafe())
            {
                _lastDiagnostics = BuildDiagnostics(
                    context,
                    didInitializeBootstrap: false,
                    hasActorLayer: false,
                    hasObjectLayer: false,
                    hasSpriteResolver,
                    usedGeneratedFallbackSprite: false,
                    contract,
                    queueEntryCount: 0,
                    plannedEntryCount: 0,
                    createdSceneObjectCount: 0,
                    missingSpriteCount: 0,
                    ResolveEarlyFailureReason(context, contract));
                LogLastDiagnostics();
                return _lastDiagnostics;
            }

            var runtime = new ArcGraphBootstrapRuntime();
            bool initialized = runtime.Initialize(
                context,
                ArcGraphBootstrapOptions.CreateDefault());

            ArcGraphActorLayer actorLayer = null;
            ArcGraphObjectLayer objectLayer = null;
            bool hasLayerStack = initialized && runtime.LayerStack != null;
            bool hasActorLayer = hasLayerStack
                                 && runtime.LayerStack.TryGetLayer<ArcGraphActorLayer>(out actorLayer);
            bool hasObjectLayer = hasLayerStack
                                  && runtime.LayerStack.TryGetLayer<ArcGraphObjectLayer>(out objectLayer);

            if (!initialized || !hasActorLayer || !hasObjectLayer)
            {
                _lastDiagnostics = BuildDiagnostics(
                    context,
                    initialized,
                    hasActorLayer,
                    hasObjectLayer,
                    hasSpriteResolver,
                    usedGeneratedFallbackSprite: false,
                    contract,
                    queueEntryCount: 0,
                    plannedEntryCount: 0,
                    createdSceneObjectCount: 0,
                    missingSpriteCount: 0,
                    !initialized ? "BootstrapInitializeFailed" : "ActorObjectLayersMissing");
                runtime.Dispose();
                LogLastDiagnostics();
                return _lastDiagnostics;
            }

            var queue = new ArcGraphRenderQueue();
            var queueBuilder = new ArcGraphRenderQueueBuilder();
            ArcGraphZoomLodProfile lodProfile = ResolveLodProfile();
            queueBuilder.Build(actorLayer, objectLayer, lodProfile, queue);

            var plan = new ArcGraphActorObjectSceneRenderPlan();
            var planBuilder = new ArcGraphActorObjectSceneRenderPlanBuilder();
            planBuilder.Build(queue, contract, plan, hasSpriteResolver);

            EnsureRoot(contract.RootName);

            int created = 0;
            int missingSprites = 0;
            bool usedGeneratedFallback = false;

            for (int i = 0; i < plan.Entries.Count; i++)
            {
                ArcGraphActorObjectSceneRenderEntry entry = plan.Entries[i];
                if (CreateSceneObject(entry, spriteResolver, out bool usedGeneratedSprite))
                {
                    created++;
                    usedGeneratedFallback |= usedGeneratedSprite;
                }
                else
                {
                    missingSprites++;
                }
            }

            _lastDiagnostics = BuildDiagnostics(
                context,
                initialized,
                hasActorLayer,
                hasObjectLayer,
                hasSpriteResolver,
                usedGeneratedFallback,
                contract,
                queue.Entries.Count,
                plan.Entries.Count,
                created,
                missingSprites,
                "ActorObjectSceneProbeRendered");

            runtime.Dispose();
            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        // =============================================================================
        // ClearProbe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cancella gli oggetti temporanei creati dal probe actor/object.
        /// </para>
        ///
        /// <para><b>Cleanup confinato</b></para>
        /// <para>
        /// Il metodo cerca e distrugge solo il root dedicato del probe. Non tocca
        /// MapGrid, altri renderer ArcGraph, scene asset, prefab o UI.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear Actor Object Scene Probe")]
        public void ClearProbe()
        {
            if (_root == null)
                _root = transform.Find(ProbeRootName);

            if (_root == null)
                return;

            DestroyProbeObject(_root.gameObject);
            _root = null;
        }

        private bool CreateSceneObject(
            ArcGraphActorObjectSceneRenderEntry entry,
            IArcGraphSpriteResolver spriteResolver,
            out bool usedGeneratedSprite)
        {
            usedGeneratedSprite = false;

            if (!ResolveSprite(entry, spriteResolver, out Sprite sprite, out Color color, out usedGeneratedSprite))
                return false;

            EnsureRoot(ProbeRootName);

            var go = new GameObject(CreateSceneObjectName(entry));
            go.transform.SetParent(_root, false);
            go.transform.position = ResolveWorldPosition(entry);
            go.transform.localScale = Vector3.one * ResolveScale(entry);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = entry.SortingOrder;

            return true;
        }

        private bool ResolveSprite(
            ArcGraphActorObjectSceneRenderEntry entry,
            IArcGraphSpriteResolver spriteResolver,
            out Sprite sprite,
            out Color color,
            out bool usedGeneratedSprite)
        {
            color = Color.white;
            usedGeneratedSprite = false;

            if (spriteResolver != null
                && spriteResolver.TryResolveSprite(entry.SpriteRequest, out sprite)
                && sprite != null)
            {
                return true;
            }

            if (!allowGeneratedFallbackSprites)
            {
                sprite = null;
                return false;
            }

            sprite = ResolveGeneratedFallbackSprite(entry.Kind);
            color = entry.Kind == ArcGraphRenderItemKind.Actor
                ? new Color(0.95f, 0.15f, 0.95f, 1f)
                : new Color(1f, 0.55f, 0.12f, 1f);
            usedGeneratedSprite = sprite != null;
            return sprite != null;
        }

        private Sprite ResolveGeneratedFallbackSprite(ArcGraphRenderItemKind kind)
        {
            if (kind == ArcGraphRenderItemKind.Actor)
            {
                EnsureGeneratedActorSprite();
                return _generatedActorSprite;
            }

            if (kind == ArcGraphRenderItemKind.Object)
            {
                EnsureGeneratedObjectSprite();
                return _generatedObjectSprite;
            }

            return null;
        }

        private void EnsureGeneratedActorSprite()
        {
            if (_generatedActorSprite != null)
                return;

            CreateGeneratedSprite(
                "ArcGraphGeneratedActorProbeSprite",
                new Color(0.95f, 0.15f, 0.95f, 1f),
                out _generatedActorTexture,
                out _generatedActorSprite);
        }

        private void EnsureGeneratedObjectSprite()
        {
            if (_generatedObjectSprite != null)
                return;

            CreateGeneratedSprite(
                "ArcGraphGeneratedObjectProbeSprite",
                new Color(1f, 0.55f, 0.12f, 1f),
                out _generatedObjectTexture,
                out _generatedObjectSprite);
        }

        private static void CreateGeneratedSprite(
            string name,
            Color color,
            out Texture2D texture,
            out Sprite sprite)
        {
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            texture.name = name + "Texture";
            texture.SetPixel(0, 0, color);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
            sprite.name = name;
        }

        private void EnsureRoot(string rootName)
        {
            if (_root != null)
                return;

            _root = transform.Find(rootName);
            if (_root != null)
                return;

            var go = new GameObject(rootName);
            go.transform.SetParent(transform, false);
            _root = go.transform;
        }

        private Vector3 ResolveWorldPosition(
            ArcGraphActorObjectSceneRenderEntry entry)
        {
            return originOffset + new Vector3(
                entry.WorldX,
                entry.WorldY,
                entry.WorldZ + probeZOffset);
        }

        private float ResolveScale(
            ArcGraphActorObjectSceneRenderEntry entry)
        {
            if (entry.Kind == ArcGraphRenderItemKind.Actor)
                return actorScale > 0f ? actorScale : 1f;

            if (entry.Kind == ArcGraphRenderItemKind.Object)
            {
                // Se l'oggetto dichiara dimensioni visuali reali, il probe non deve
                // ridurlo con la scala placeholder: un muro 32x83 deve restare
                // leggibile con la sua altezza effettiva.
                if (entry.HasObjectVisualMetadata)
                    return 1f;

                return objectScale > 0f ? objectScale : 1f;
            }

            return 1f;
        }

        private IArcGraphSpriteResolver ResolveSpriteResolver()
        {
            return spriteResolverBehaviour as IArcGraphSpriteResolver;
        }

        private ArcGraphZoomLodProfile ResolveLodProfile()
        {
            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            int safeZoom = zoomLevel < 1 ? 1 : zoomLevel;
            return ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(safeZoom));
        }

        private static float ResolveTileWorldSize(
            ArcGraphRuntimeContext context)
        {
            if (context != null && context.HasConfig && context.Config.tileSizeWorld > 0.0001f)
                return context.Config.tileSizeWorld;

            return 1f;
        }

        private static string ResolveEarlyFailureReason(
            ArcGraphRuntimeContext context,
            ArcGraphActorObjectSceneRendererContract contract)
        {
            if (context == null)
                return "RuntimeContextMissing";

            if (!context.HasWorld)
                return "WorldMissing";

            if (!contract.IsTemporaryProbeSafe())
                return "UnsafeContract";

            return "RuntimeAdapterMissing";
        }

        private static string CreateSceneObjectName(
            ArcGraphActorObjectSceneRenderEntry entry)
        {
            if (entry.Kind == ArcGraphRenderItemKind.Actor)
                return "ArcGraphActor_" + entry.EntityId;

            if (entry.Kind == ArcGraphRenderItemKind.Object)
                return "ArcGraphObject_" + entry.EntityId;

            return "ArcGraphItem_" + entry.EntityId;
        }

        private ArcGraphActorObjectSceneProbeRendererDiagnostics BuildDiagnostics(
            ArcGraphRuntimeContext context,
            bool didInitializeBootstrap,
            bool hasActorLayer,
            bool hasObjectLayer,
            bool hasSpriteResolver,
            bool usedGeneratedFallbackSprite,
            ArcGraphActorObjectSceneRendererContract contract,
            int queueEntryCount,
            int plannedEntryCount,
            int createdSceneObjectCount,
            int missingSpriteCount,
            string reason)
        {
            return new ArcGraphActorObjectSceneProbeRendererDiagnostics(
                runtimeMapAdapter != null,
                context != null && context.HasWorld,
                didInitializeBootstrap,
                hasActorLayer,
                hasObjectLayer,
                hasSpriteResolver,
                usedGeneratedFallbackSprite,
                contract.IsTemporaryProbeSafe(),
                queueEntryCount,
                plannedEntryCount,
                createdSceneObjectCount,
                missingSpriteCount,
                reason);
        }

        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphActorObjectSceneProbeRenderer] " + _lastDiagnostics.Reason +
                " runtimeAdapter=" + _lastDiagnostics.HasRuntimeAdapter +
                ", world=" + _lastDiagnostics.HasWorld +
                ", initialized=" + _lastDiagnostics.DidInitializeBootstrap +
                ", actorLayer=" + _lastDiagnostics.HasActorLayer +
                ", objectLayer=" + _lastDiagnostics.HasObjectLayer +
                ", spriteResolver=" + _lastDiagnostics.HasSpriteResolver +
                ", generatedFallback=" + _lastDiagnostics.UsedGeneratedFallbackSprite +
                ", queueEntries=" + _lastDiagnostics.QueueEntryCount +
                ", plannedEntries=" + _lastDiagnostics.PlannedEntryCount +
                ", createdObjects=" + _lastDiagnostics.CreatedSceneObjectCount +
                ", missingSprites=" + _lastDiagnostics.MissingSpriteCount);
        }

        private static void DestroyProbeObject(Object unityObject)
        {
            if (unityObject == null)
                return;

            if (Application.isPlaying)
                Destroy(unityObject);
            else
                DestroyImmediate(unityObject);
        }

        private void OnDestroy()
        {
            ClearProbe();

            DestroyProbeObject(_generatedActorSprite);
            DestroyProbeObject(_generatedObjectSprite);
            DestroyProbeObject(_generatedActorTexture);
            DestroyProbeObject(_generatedObjectTexture);

            _generatedActorSprite = null;
            _generatedObjectSprite = null;
            _generatedActorTexture = null;
            _generatedObjectTexture = null;
        }
    }
}
