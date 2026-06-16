using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcRuntimeSceneRenderer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Renderer runtime minimo per materializzare gli NPC ArcGraph in scena.
    /// </para>
    ///
    /// <para><b>Principio architetturale: pool scene-side sopra queue passiva</b></para>
    /// <para>
    /// Questo componente consuma una <c>ArcGraphRenderQueue</c> gia' prodotta dal
    /// coordinator ArcGraph e disegna solo gli item actor. Ogni NPC viene mantenuto
    /// in un pool indicizzato per <c>actorId</c>, cosi' i frame successivi aggiornano
    /// posizione, sprite e sorting senza distruggere e ricreare continuamente
    /// oggetti Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>runtimeWrapper</b>: sorgente opzionale manuale della queue gia' costruita.</item>
    ///   <item><b>spriteResolverBehaviour</b>: resolver sprite assegnato da Inspector.</item>
    ///   <item><b>_actorPool</b>: handle runtime riusabili per actorId.</item>
    ///   <item><b>RenderFromQueue</b>: entry point produttivo minimo.</item>
    ///   <item><b>RenderFromRuntimeWrapper</b>: entry point manuale per test da Inspector.</item>
    ///   <item><b>ClearRuntimeRenderer</b>: cleanup confinato del solo root NPC ArcGraph.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphNpcRuntimeSceneRenderer : MonoBehaviour
    {
        [SerializeField] private ArcGraphMinimalRuntimeSceneWrapper runtimeWrapper;
        [SerializeField] private MonoBehaviour spriteResolverBehaviour;
        [SerializeField] private TextAsset npcVisualCatalogJson;
        [SerializeField] private bool useLayeredActorCatalog;
        [SerializeField] private bool rendererEnabled;
        [SerializeField] private bool renderOnStart;
        [SerializeField] private bool logDiagnostics;
        [SerializeField] private bool allowGeneratedFallbackSprites = true;
        [SerializeField] private bool disableMissingActorsAfterRender = true;
        [SerializeField] private int idleFrameStep = 12;
        [SerializeField] private bool renderActorShadow = true;
        [SerializeField] private string actorShadowSpriteKey = "ArcGraph/NPC/common/shadow/soft_ellipse_32x16";
        [SerializeField] private Vector3 actorShadowLocalOffset = new Vector3(0f, -0.1f, 0f);
        [SerializeField] private Vector2 actorShadowLocalScale = Vector2.one;
        [SerializeField] private Color actorShadowTint = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private int actorShadowSortingOffset = -2;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private float actorZOffset = -0.02f;
        [SerializeField] private float actorScale = 1f;
        [SerializeField] private Vector3 layeredActorSpriteLocalOffset = new Vector3(0f, -0.25f, 0f);
        [SerializeField] private string runtimeRootName = "ArcGraphNpcRuntimeRoot";

        private readonly Dictionary<int, ActorHandle> _actorPool = new();
        private readonly ArcGraphActorObjectSceneRenderPlan _plan = new();
        private readonly ArcGraphActorObjectSceneRenderPlanBuilder _planBuilder = new();
        private Transform _root;
        private Sprite _generatedFallbackSprite;
        private Sprite _generatedShadowSprite;
        private ArcGraphNpcVisualCatalog _npcVisualCatalog;
        private string _npcVisualCatalogSourceText;
        private ArcGraphNpcRuntimeSceneRendererDiagnostics _lastDiagnostics;

        public ArcGraphNpcRuntimeSceneRendererDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool RendererEnabled => rendererEnabled;
        public int PooledActorCount => _actorPool.Count;

        // =============================================================================
        // ActorHandle
        // =============================================================================
        /// <summary>
        /// <para>
        /// Handle locale di un NPC materializzato in scena.
        /// </para>
        ///
        /// <para><b>Pooling per identita' actor</b></para>
        /// <para>
        /// Il renderer non tratta gli NPC come sprite anonimi da ricreare. Ogni
        /// handle resta associato a un <c>actorId</c> e viene aggiornato frame dopo
        /// frame. Questo prepara il terreno per animazioni, interpolazioni e overlay
        /// NPC senza cambiare il contratto dati.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>ActorId</b>: identita' runtime dell'actor.</item>
        ///   <item><b>GameObject</b>: oggetto scena posseduto dal renderer.</item>
        ///   <item><b>Renderer</b>: SpriteRenderer aggiornato dal frame corrente.</item>
        ///   <item><b>WasTouchedThisFrame</b>: marker per disattivare NPC spariti dalla queue.</item>
        /// </list>
        /// </summary>
        private sealed class ActorHandle
        {
            public int ActorId;
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public SpriteRenderer ShadowRenderer;
            public readonly Dictionary<string, SpriteRenderer> PartRenderers = new();
            public string LastDirection = "south";
            public bool WasTouchedThisFrame;
        }

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia opzionalmente il rendering NPC runtime dalla queue del wrapper.
        /// </para>
        ///
        /// <para><b>Default spento</b></para>
        /// <para>
        /// Il renderer non parte in automatico durante la transizione MapGrid ->
        /// ArcGraph. Serve una scelta esplicita da Inspector o dal futuro wiring.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (!renderOnStart)
                return;

            RenderFromRuntimeWrapper();
        }

        // =============================================================================
        // SetRendererEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il gate principale del renderer NPC.
        /// </para>
        /// </summary>
        public void SetRendererEnabled(bool enabled)
        {
            rendererEnabled = enabled;
        }

        // =============================================================================
        // SetRuntimeWrapper
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il wrapper minimo da cui leggere manualmente la render queue.
        /// </para>
        /// </summary>
        public void SetRuntimeWrapper(ArcGraphMinimalRuntimeSceneWrapper wrapper)
        {
            runtimeWrapper = wrapper;
        }

        // =============================================================================
        // SetSpriteResolverBehaviour
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna un componente che implementa <see cref="IArcGraphSpriteResolver"/>.
        /// </para>
        /// </summary>
        public void SetSpriteResolverBehaviour(MonoBehaviour resolverBehaviour)
        {
            spriteResolverBehaviour = resolverBehaviour;
        }

        // =============================================================================
        // SetNpcVisualCatalogJson
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il catalogo visuale NPC usato per il rendering modulare a parti.
        /// </para>
        /// </summary>
        public void SetNpcVisualCatalogJson(TextAsset catalogJson)
        {
            npcVisualCatalogJson = catalogJson;
            _npcVisualCatalog = null;
            _npcVisualCatalogSourceText = null;
        }

        // =============================================================================
        // SetUseLayeredActorCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita la composizione NPC a layer letta dal catalogo
        /// visuale.
        /// </para>
        ///
        /// <para><b>Principio architetturale: configurazione esplicita del renderer</b></para>
        /// <para>
        /// L'installer di scena puo' attivare il path modulare degli NPC senza
        /// accedere ai campi privati del componente. Il renderer resta comunque
        /// consumer della queue ArcGraph: questa opzione decide solo come tradurre
        /// una entry actor in sprite Unity.
        /// </para>
        /// </summary>
        public void SetUseLayeredActorCatalog(bool enabled)
        {
            useLayeredActorCatalog = enabled;
        }

        // =============================================================================
        // SetRenderActorShadow
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita la resa dell'ombra locale degli NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: fallback visuale controllabile</b></para>
        /// <para>
        /// L'ombra generata e' utile come placeholder, ma durante il gate sugli
        /// sprite reali puo' confondere la lettura dell'asset. Esporre questo setter
        /// consente all'installer o a futuri pannelli debug di spegnerla senza
        /// modificare la logica di rendering degli attori.
        /// </para>
        /// </summary>
        public void SetRenderActorShadow(bool enabled)
        {
            renderActorShadow = enabled;
        }

        // =============================================================================
        // RenderFromRuntimeWrapperContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point manuale da Inspector per renderizzare la queue del wrapper.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Render NPC Runtime From Wrapper Queue")]
        public void RenderFromRuntimeWrapperContextMenu()
        {
            RenderFromRuntimeWrapper();
        }

        // =============================================================================
        // RenderFromRuntimeWrapper
        // =============================================================================
        /// <summary>
        /// <para>
        /// Renderizza gli NPC usando la queue gia' esposta dal wrapper runtime.
        /// </para>
        ///
        /// <para><b>Compatibilita' test manuale</b></para>
        /// <para>
        /// Questo metodo non chiede al wrapper di processare un frame. Legge solo la
        /// queue attualmente disponibile, cosi' il test resta separato: prima si
        /// processa il wrapper, poi si renderizzano gli NPC.
        /// </para>
        /// </summary>
        public ArcGraphNpcRuntimeSceneRendererDiagnostics RenderFromRuntimeWrapper()
        {
            return RenderFromQueue(runtimeWrapper != null ? runtimeWrapper.RenderQueue : null);
        }

        // =============================================================================
        // RenderFromQueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica in scena gli NPC presenti nella render queue ArcGraph.
        /// </para>
        ///
        /// <para><b>Entry point produttivo minimo</b></para>
        /// <para>
        /// Il futuro wrapper/coordinator potra' chiamare direttamente questo metodo
        /// dopo aver costruito la queue actor/object. Il renderer filtra le entry
        /// actor, risolve gli sprite e aggiorna il pool locale senza toccare oggetti,
        /// terreno, input, HUD o simulazione.
        /// </para>
        /// </summary>
        public ArcGraphNpcRuntimeSceneRendererDiagnostics RenderFromQueue(
            ArcGraphRenderQueue queue)
        {
            ArcGraphNpcRuntimeSceneRendererContract contract = CreateContract();
            IArcGraphSpriteResolver spriteResolver = ResolveSpriteResolver();
            bool hasSpriteResolver = spriteResolver != null;

            if (!rendererEnabled)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "RendererDisabled");

            if (!contract.IsRuntimeSafe)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "UnsafeContract");

            if (queue == null)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "QueueMissing");

            ArcGraphActorObjectSceneRendererContract planContract = contract.CreateActorObjectPlanContract();
            ArcGraphActorObjectSceneRendererDiagnostics planDiagnostics = _planBuilder.Build(
                queue,
                planContract,
                _plan,
                hasSpriteResolver,
                clearPlan: true);

            if (!planDiagnostics.ContractSafe)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "UnsafePlanContract");

            ApplyActorEntries(
                _plan,
                contract,
                spriteResolver,
                out int rendered,
                out int created,
                out int reused,
                out int disabled,
                out int missingSprites,
                out int generatedFallbacks,
                out int layeredActors,
                out int createdPartRenderers,
                out int reusedPartRenderers,
                out int missingCatalogFrames);

            string reason = rendered > 0
                ? "NpcRuntimeRendered"
                : "NpcRuntimeNoVisibleActors";

            return StoreAndLogDiagnostics(
                contract,
                queue,
                hasSpriteResolver,
                builtPlan: true,
                CountActorEntries(_plan),
                rendered,
                created,
                reused,
                disabled,
                missingSprites,
                generatedFallbacks,
                layeredActors,
                createdPartRenderers,
                reusedPartRenderers,
                missingCatalogFrames,
                reason);
        }

        // =============================================================================
        // ClearRuntimeRenderer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove tutti gli NPC runtime creati da questo renderer.
        /// </para>
        ///
        /// <para><b>Cleanup confinato</b></para>
        /// <para>
        /// Il cleanup distrugge solo il root NPC locale e la sprite fallback generata
        /// dal renderer. Non tocca resolver, asset assegnati, MapGrid, terrain
        /// renderer, wrapper o simulazione.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear NPC Runtime Renderer")]
        public void ClearRuntimeRenderer()
        {
            _actorPool.Clear();

            if (_root != null)
            {
                DestroyUnityObject(_root.gameObject);
                _root = null;
            }

            if (_generatedFallbackSprite != null)
            {
                // La texture della sprite fallback viene distrutta insieme alla
                // sprite: entrambe sono create e possedute da questo renderer.
                Texture2D texture = _generatedFallbackSprite.texture;
                DestroyUnityObject(_generatedFallbackSprite);
                DestroyUnityObject(texture);
                _generatedFallbackSprite = null;
            }

            if (_generatedShadowSprite != null)
            {
                Texture2D texture = _generatedShadowSprite.texture;
                DestroyUnityObject(_generatedShadowSprite);
                DestroyUnityObject(texture);
                _generatedShadowSprite = null;
            }
        }

        // =============================================================================
        // LogLastDiagnosticsFromContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ristampa in Console l'ultima diagnostica prodotta dal renderer NPC.
        /// </para>
        ///
        /// <para><b>Supporto gate visuale</b></para>
        /// <para>
        /// Durante i test Unity puo' essere utile leggere di nuovo l'ultimo stato
        /// senza rieseguire il render: questo menu non modifica scena, pool o
        /// simulazione, ma richiama soltanto il log diagnostico gia' salvato.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Log Last NPC Runtime Diagnostics")]
        public void LogLastDiagnosticsFromContextMenu()
        {
            LogLastDiagnostics();
        }

        private ArcGraphNpcRuntimeSceneRendererContract CreateContract()
        {
            return new ArcGraphNpcRuntimeSceneRendererContract(
                runtimeRootName,
                tileWorldSize,
                originOffset,
                actorZOffset,
                actorScale,
                disableMissingActorsAfterRender);
        }

        private IArcGraphSpriteResolver ResolveSpriteResolver()
        {
            return spriteResolverBehaviour as IArcGraphSpriteResolver;
        }

        private void ApplyActorEntries(
            ArcGraphActorObjectSceneRenderPlan plan,
            ArcGraphNpcRuntimeSceneRendererContract contract,
            IArcGraphSpriteResolver spriteResolver,
            out int rendered,
            out int created,
            out int reused,
            out int disabled,
            out int missingSprites,
            out int generatedFallbacks,
            out int layeredActors,
            out int createdPartRenderers,
            out int reusedPartRenderers,
            out int missingCatalogFrames)
        {
            rendered = 0;
            created = 0;
            reused = 0;
            disabled = 0;
            missingSprites = 0;
            generatedFallbacks = 0;
            layeredActors = 0;
            createdPartRenderers = 0;
            reusedPartRenderers = 0;
            missingCatalogFrames = 0;

            if (plan == null)
                return;

            EnsureRoot(contract);
            MarkAllHandlesUntouched();

            IReadOnlyList<ArcGraphActorObjectSceneRenderEntry> entries = plan.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                ArcGraphActorObjectSceneRenderEntry entry = entries[i];
                if (entry.Kind != ArcGraphRenderItemKind.Actor)
                    continue;

                ActorHandle handle = GetOrCreateActorHandle(entry.EntityId, contract, out bool wasCreated);
                bool renderedLayered = TryApplyLayeredActorEntry(
                    handle,
                    entry,
                    contract,
                    spriteResolver,
                    out int entryMissingSprites,
                    out int entryGeneratedFallbacks,
                    out int entryCreatedParts,
                    out int entryReusedParts,
                    out int entryMissingCatalogFrames);

                if (renderedLayered)
                {
                    layeredActors++;
                    missingSprites += entryMissingSprites;
                    generatedFallbacks += entryGeneratedFallbacks;
                    createdPartRenderers += entryCreatedParts;
                    reusedPartRenderers += entryReusedParts;
                    missingCatalogFrames += entryMissingCatalogFrames;
                }
                else
                {
                    Sprite sprite = ResolveSprite(entry, spriteResolver, out bool usedGeneratedFallback);
                    if (sprite == null)
                    {
                        missingSprites++;
                        continue;
                    }

                    ApplyActorEntry(handle, entry, sprite, contract, spriteResolver);

                    if (usedGeneratedFallback)
                        generatedFallbacks++;
                }

                rendered++;

                if (wasCreated)
                    created++;
                else
                    reused++;
            }

            if (contract.DisableMissingActorsAfterRender)
                disabled = DisableUntouchedHandles();
        }

        private Sprite ResolveSprite(
            ArcGraphActorObjectSceneRenderEntry entry,
            IArcGraphSpriteResolver spriteResolver,
            out bool usedGeneratedFallback)
        {
            usedGeneratedFallback = false;

            if (spriteResolver != null
                && spriteResolver.TryResolveSprite(entry.SpriteRequest, out Sprite resolved)
                && resolved != null)
            {
                return resolved;
            }

            if (!allowGeneratedFallbackSprites)
                return null;

            usedGeneratedFallback = true;
            return GetOrCreateFallbackSprite();
        }

        private ActorHandle GetOrCreateActorHandle(
            int actorId,
            ArcGraphNpcRuntimeSceneRendererContract contract,
            out bool wasCreated)
        {
            if (_actorPool.TryGetValue(actorId, out var handle))
            {
                wasCreated = false;
                return handle;
            }

            EnsureRoot(contract);

            var go = new GameObject("ArcGraphNpcRuntimeActor_" + actorId);
            go.transform.SetParent(_root, false);

            handle = new ActorHandle
            {
                ActorId = actorId,
                GameObject = go,
                Renderer = go.AddComponent<SpriteRenderer>(),
                WasTouchedThisFrame = true
            };

            _actorPool[actorId] = handle;
            wasCreated = true;
            return handle;
        }

        private void ApplyActorEntry(
            ActorHandle handle,
            ArcGraphActorObjectSceneRenderEntry entry,
            Sprite sprite,
            ArcGraphNpcRuntimeSceneRendererContract contract,
            IArcGraphSpriteResolver spriteResolver)
        {
            // Le coordinate mondo sono gia' state calcolate dal plan builder. Qui
            // aggiungiamo solo l'offset locale del renderer per poter sovrapporre
            // ArcGraph a MapGrid durante i test senza cambiare i dati sorgente.
            handle.GameObject.transform.localPosition = contract.OriginOffset + new Vector3(
                entry.WorldX,
                entry.WorldY,
                entry.WorldZ + contract.ZOffset);
            handle.GameObject.transform.localScale = Vector3.one * contract.ActorScale;
            handle.Renderer.sprite = sprite;
            handle.Renderer.sortingOrder = entry.SortingOrder;
            handle.Renderer.enabled = true;
            SetPartRenderersEnabled(handle, false);
            ApplyActorShadow(handle, entry, contract, spriteResolver);
            handle.GameObject.SetActive(true);
            handle.WasTouchedThisFrame = true;
        }

        private bool TryApplyLayeredActorEntry(
            ActorHandle handle,
            ArcGraphActorObjectSceneRenderEntry entry,
            ArcGraphNpcRuntimeSceneRendererContract contract,
            IArcGraphSpriteResolver spriteResolver,
            out int missingSprites,
            out int generatedFallbacks,
            out int createdPartRenderers,
            out int reusedPartRenderers,
            out int missingCatalogFrames)
        {
            missingSprites = 0;
            generatedFallbacks = 0;
            createdPartRenderers = 0;
            reusedPartRenderers = 0;
            missingCatalogFrames = 0;

            if (!useLayeredActorCatalog || entry.SpriteRequest.UsesSimplifiedRepresentation)
                return false;

            ArcGraphNpcVisualCatalog catalog = GetOrParseNpcVisualCatalog();
            if (catalog == null || catalog.PartCount <= 0)
                return false;

            string direction = ResolveDirection(handle, entry, contract);
            string animation = entry.HasMotion ? "walk" : catalog.DefaultAnimationKey;
            bool appliedAnyPart = false;

            // Prima di applicare il frame modulare corrente spegniamo tutte le
            // parti gia' presenti nel pool. Questo evita residui visivi: se, per
            // esempio, nel frame precedente esisteva la testa e nel frame corrente
            // la sprite head manca o il catalogo non la risolve, la vecchia testa
            // non deve restare disegnata per errore.
            SetPartRenderersEnabled(handle, false);

            for (int i = 0; i < catalog.Parts.Count; i++)
            {
                string partKey = catalog.Parts[i];
                int frameIndex = ResolveAnimationFrameIndex(
                    catalog,
                    partKey,
                    direction,
                    animation,
                    entry);

                if (!catalog.TryResolveFrame(
                        catalog.DefaultVisualKey,
                        partKey,
                        direction,
                        animation,
                        frameIndex,
                        out ArcGraphNpcVisualFrame visualFrame)
                    && !catalog.TryResolveFrame(
                        catalog.DefaultVisualKey,
                        partKey,
                        direction,
                        catalog.DefaultAnimationKey,
                        0,
                        out visualFrame))
                {
                    missingCatalogFrames++;
                    continue;
                }

                var request = new ArcGraphSpriteResolveRequest(
                    ArcGraphRenderItemKind.Actor,
                    entry.EntityId,
                    visualFrame.SpriteKey,
                    visualFrame.PartKey,
                    usesSimplifiedRepresentation: false);

                Sprite sprite = ResolveSprite(
                    request,
                    spriteResolver,
                    allowFallbackForThisRequest: false,
                    out bool usedGeneratedFallback);
                if (sprite == null)
                {
                    missingSprites++;
                    continue;
                }

                SpriteRenderer partRenderer = GetOrCreatePartRenderer(
                    handle,
                    visualFrame.PartKey,
                    out bool wasCreated);
                ApplyPartRenderer(handle, partRenderer, visualFrame, entry, sprite, contract);

                appliedAnyPart = true;
                if (usedGeneratedFallback)
                    generatedFallbacks++;

                if (wasCreated)
                    createdPartRenderers++;
                else
                    reusedPartRenderers++;
            }

            if (!appliedAnyPart)
                return false;

            ApplyActorShadow(handle, entry, contract, spriteResolver);
            handle.Renderer.enabled = false;
            handle.GameObject.SetActive(true);
            handle.WasTouchedThisFrame = true;
            return true;
        }

        private int ResolveAnimationFrameIndex(
            ArcGraphNpcVisualCatalog catalog,
            string partKey,
            string direction,
            string animation,
            ArcGraphActorObjectSceneRenderEntry entry)
        {
            int frameCount = catalog.ResolveFrameCount(
                catalog.DefaultVisualKey,
                partKey,
                direction,
                animation);

            if (frameCount <= 1)
                return 0;

            if (entry.HasMotion)
            {
                // Il movimento multi-tick possiede gia' un progresso 0..1. Lo
                // usiamo come timeline naturale della camminata, cosi' velocita'
                // diverse attraversano comunque tutti i frame disponibili.
                int walkFrame = Mathf.FloorToInt(entry.MotionProgress01 * frameCount);
                return Mathf.Clamp(walkFrame, 0, frameCount - 1);
            }

            // L'idle non ha progresso di movimento. Usiamo un contatore visuale
            // locale Unity molto economico: nessuna allocazione, nessun accesso al
            // World, solo cambio frame ogni N frame render.
            int safeStep = idleFrameStep > 0 ? idleFrameStep : 12;
            int idleFrame = Time.frameCount / safeStep;
            return idleFrame % frameCount;
        }

        private SpriteRenderer GetOrCreatePartRenderer(
            ActorHandle handle,
            string partKey,
            out bool wasCreated)
        {
            string safePartKey = string.IsNullOrWhiteSpace(partKey) ? "part" : partKey;
            if (handle.PartRenderers.TryGetValue(safePartKey, out SpriteRenderer renderer)
                && renderer != null)
            {
                wasCreated = false;
                return renderer;
            }

            var go = new GameObject("ArcGraphNpcPart_" + handle.ActorId + "_" + safePartKey);
            go.transform.SetParent(handle.GameObject.transform, false);
            renderer = go.AddComponent<SpriteRenderer>();
            handle.PartRenderers[safePartKey] = renderer;
            wasCreated = true;
            return renderer;
        }

        private void ApplyPartRenderer(
            ActorHandle handle,
            SpriteRenderer partRenderer,
            ArcGraphNpcVisualFrame visualFrame,
            ArcGraphActorObjectSceneRenderEntry entry,
            Sprite sprite,
            ArcGraphNpcRuntimeSceneRendererContract contract)
        {
            handle.GameObject.transform.localPosition = contract.OriginOffset + new Vector3(
                entry.WorldX,
                entry.WorldY,
                entry.WorldZ + contract.ZOffset);
            handle.GameObject.transform.localScale = Vector3.one * contract.ActorScale;

            partRenderer.transform.localPosition = layeredActorSpriteLocalOffset;
            partRenderer.transform.localScale = Vector3.one;
            partRenderer.sprite = sprite;
            partRenderer.sortingOrder = entry.SortingOrder + visualFrame.SortingOffset;
            partRenderer.enabled = true;
        }

        private void ApplyActorShadow(
            ActorHandle handle,
            ArcGraphActorObjectSceneRenderEntry entry,
            ArcGraphNpcRuntimeSceneRendererContract contract,
            IArcGraphSpriteResolver spriteResolver)
        {
            if (!renderActorShadow)
            {
                if (handle.ShadowRenderer != null)
                    handle.ShadowRenderer.enabled = false;

                return;
            }

            Sprite shadowSprite = ResolveActorShadowSprite(
                entry,
                spriteResolver);
            if (shadowSprite == null)
            {
                if (handle.ShadowRenderer != null)
                    handle.ShadowRenderer.enabled = false;

                return;
            }

            SpriteRenderer shadowRenderer = GetOrCreateShadowRenderer(handle);

            // L'ombra appartiene alla resa visuale dell'attore, non al catalogo
            // delle parti anatomiche. Per questo usa un renderer separato che
            // segue il root dell'NPC e resta sotto body/head/legs/feet.
            shadowRenderer.transform.localPosition = actorShadowLocalOffset;
            shadowRenderer.transform.localScale = new Vector3(
                actorShadowLocalScale.x,
                actorShadowLocalScale.y,
                1f);
            shadowRenderer.sprite = shadowSprite;
            shadowRenderer.color = actorShadowTint;
            shadowRenderer.sortingOrder = entry.SortingOrder + actorShadowSortingOffset;
            shadowRenderer.enabled = true;
        }

        private Sprite ResolveActorShadowSprite(
            ArcGraphActorObjectSceneRenderEntry entry,
            IArcGraphSpriteResolver spriteResolver)
        {
            if (spriteResolver != null && !string.IsNullOrWhiteSpace(actorShadowSpriteKey))
            {
                var request = new ArcGraphSpriteResolveRequest(
                    ArcGraphRenderItemKind.Actor,
                    entry.EntityId,
                    actorShadowSpriteKey,
                    "shadow",
                    usesSimplifiedRepresentation: false);

                if (spriteResolver.TryResolveSprite(request, out Sprite resolved)
                    && resolved != null)
                {
                    return resolved;
                }
            }

            return allowGeneratedFallbackSprites
                ? GetOrCreateGeneratedShadowSprite()
                : null;
        }

        private SpriteRenderer GetOrCreateShadowRenderer(ActorHandle handle)
        {
            if (handle.ShadowRenderer != null)
                return handle.ShadowRenderer;

            var go = new GameObject("ArcGraphNpcShadow_" + handle.ActorId);
            go.transform.SetParent(handle.GameObject.transform, false);
            handle.ShadowRenderer = go.AddComponent<SpriteRenderer>();
            return handle.ShadowRenderer;
        }

        private void SetPartRenderersEnabled(
            ActorHandle handle,
            bool enabled)
        {
            foreach (var pair in handle.PartRenderers)
            {
                if (pair.Value != null)
                    pair.Value.enabled = enabled;
            }
        }

        private void EnsureRoot(ArcGraphNpcRuntimeSceneRendererContract contract)
        {
            if (_root != null)
                return;

            _root = transform.Find(contract.RootName);
            if (_root != null)
                return;

            var go = new GameObject(contract.RootName);
            go.transform.SetParent(transform, false);
            _root = go.transform;
        }

        private void MarkAllHandlesUntouched()
        {
            foreach (var pair in _actorPool)
                pair.Value.WasTouchedThisFrame = false;
        }

        private int DisableUntouchedHandles()
        {
            int disabled = 0;

            foreach (var pair in _actorPool)
            {
                ActorHandle handle = pair.Value;
                if (handle.WasTouchedThisFrame)
                    continue;

                if (handle.GameObject != null && handle.GameObject.activeSelf)
                {
                    handle.GameObject.SetActive(false);
                    disabled++;
                }
            }

            return disabled;
        }

        private Sprite GetOrCreateFallbackSprite()
        {
            if (_generatedFallbackSprite != null)
                return _generatedFallbackSprite;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                name = "ArcGraphNpcGeneratedFallbackTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, new Color(1f, 0f, 1f, 1f));
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            _generatedFallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
            _generatedFallbackSprite.name = "ArcGraphNpcGeneratedFallbackSprite";
            return _generatedFallbackSprite;
        }

        private Sprite GetOrCreateGeneratedShadowSprite()
        {
            if (_generatedShadowSprite != null)
                return _generatedShadowSprite;

            const int width = 32;
            const int height = 16;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
            {
                name = "ArcGraphNpcGeneratedShadowTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            // Ellisse morbida a bassissimo costo: viene generata una volta sola e
            // poi riusata da tutti gli NPC del renderer. Non e' simulazione di luce,
            // e' solo un placeholder visivo coerente con la pipeline ArcGraph.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = ((x + 0.5f) / width * 2f) - 1f;
                    float ny = ((y + 0.5f) / height * 2f) - 1f;
                    float distance = (nx * nx) + (ny * ny);
                    float alpha = Mathf.Clamp01(1f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            _generatedShadowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 32f);
            _generatedShadowSprite.name = "ArcGraphNpcGeneratedShadowSprite";
            return _generatedShadowSprite;
        }

        private ArcGraphNpcRuntimeSceneRendererDiagnostics StoreAndLogDiagnostics(
            ArcGraphNpcRuntimeSceneRendererContract contract,
            ArcGraphRenderQueue queue,
            bool hasSpriteResolver,
            bool builtPlan,
            int actorEntryCount,
            int renderedActorCount,
            int createdActorObjectCount,
            int reusedActorObjectCount,
            int disabledActorObjectCount,
            int missingSpriteCount,
            int generatedFallbackSpriteCount,
            int layeredActorCount,
            int createdPartRendererCount,
            int reusedPartRendererCount,
            int missingCatalogFrameCount,
            string reason)
        {
            _lastDiagnostics = new ArcGraphNpcRuntimeSceneRendererDiagnostics(
                rendererEnabled,
                hasContract: true,
                contract.IsRuntimeSafe,
                queue != null,
                hasSpriteResolver,
                builtPlan,
                queue != null ? queue.Entries.Count : 0,
                queue != null ? queue.ActorItems.Count : 0,
                actorEntryCount,
                renderedActorCount,
                createdActorObjectCount,
                reusedActorObjectCount,
                disabledActorObjectCount,
                CountActiveActorObjects(),
                missingSpriteCount,
                generatedFallbackSpriteCount,
                layeredActorCount,
                createdPartRendererCount,
                reusedPartRendererCount,
                missingCatalogFrameCount,
                reason);

            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphNpcRuntimeSceneRenderer] " + _lastDiagnostics.Reason +
                " enabled=" + _lastDiagnostics.RendererEnabled +
                ", contractSafe=" + _lastDiagnostics.ContractSafe +
                ", queue=" + _lastDiagnostics.HasQueue +
                ", resolver=" + _lastDiagnostics.HasSpriteResolver +
                ", builtPlan=" + _lastDiagnostics.BuiltPlan +
                ", queueEntries=" + _lastDiagnostics.QueueEntryCount +
                ", actorItems=" + _lastDiagnostics.ActorItemCount +
                ", actorEntries=" + _lastDiagnostics.ActorEntryCount +
                ", rendered=" + _lastDiagnostics.RenderedActorCount +
                ", created=" + _lastDiagnostics.CreatedActorObjectCount +
                ", reused=" + _lastDiagnostics.ReusedActorObjectCount +
                ", disabled=" + _lastDiagnostics.DisabledActorObjectCount +
                ", active=" + _lastDiagnostics.ActiveActorObjectCount +
                ", missingSprites=" + _lastDiagnostics.MissingSpriteCount +
                ", generatedFallbacks=" + _lastDiagnostics.GeneratedFallbackSpriteCount +
                ", layeredActors=" + _lastDiagnostics.LayeredActorCount +
                ", createdParts=" + _lastDiagnostics.CreatedPartRendererCount +
                ", reusedParts=" + _lastDiagnostics.ReusedPartRendererCount +
                ", missingCatalogFrames=" + _lastDiagnostics.MissingCatalogFrameCount);
        }

        private int CountActiveActorObjects()
        {
            int count = 0;

            foreach (var pair in _actorPool)
            {
                ActorHandle handle = pair.Value;
                if (handle.GameObject != null && handle.GameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private static int CountActorEntries(ArcGraphActorObjectSceneRenderPlan plan)
        {
            if (plan == null)
                return 0;

            int count = 0;
            IReadOnlyList<ArcGraphActorObjectSceneRenderEntry> entries = plan.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Kind == ArcGraphRenderItemKind.Actor)
                    count++;
            }

            return count;
        }

        private ArcGraphNpcVisualCatalog GetOrParseNpcVisualCatalog()
        {
            string json = npcVisualCatalogJson != null
                ? npcVisualCatalogJson.text
                : null;

            if (_npcVisualCatalog != null && _npcVisualCatalogSourceText == json)
                return _npcVisualCatalog;

            if (!ArcGraphNpcVisualCatalogJson.TryParse(json, out _npcVisualCatalog))
            {
                _npcVisualCatalog = null;
                _npcVisualCatalogSourceText = null;
                return null;
            }

            _npcVisualCatalogSourceText = json;
            return _npcVisualCatalog;
        }

        private static string ResolveDirection(
            ActorHandle handle,
            ArcGraphActorObjectSceneRenderEntry entry,
            ArcGraphNpcRuntimeSceneRendererContract contract)
        {
            if (!entry.HasMotion)
            {
                // In assenza di movimento non abbiamo un vettore corrente da cui
                // ricavare la direzione. Manteniamo quindi l'ultima direzione
                // visuale nota dell'actor: cosi' un NPC che ha camminato verso
                // nord resta in idle nord invece di scattare sempre a sud.
                return handle != null && !string.IsNullOrWhiteSpace(handle.LastDirection)
                    ? handle.LastDirection
                    : "south";
            }

            float tileSize = contract.TileWorldSize > 0f ? contract.TileWorldSize : 1f;
            float visualX = (entry.WorldX / tileSize) - 0.5f;
            float visualY = (entry.WorldY / tileSize) - 0.5f;
            float dx = visualX - entry.DiscreteCell.X;
            float dy = visualY - entry.DiscreteCell.Y;

            string direction;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                direction = dx >= 0f ? "east" : "west";
            else
                direction = dy >= 0f ? "north" : "south";

            if (handle != null)
                handle.LastDirection = direction;

            return direction;
        }

        private Sprite ResolveSprite(
            ArcGraphSpriteResolveRequest request,
            IArcGraphSpriteResolver spriteResolver,
            out bool usedGeneratedFallback)
        {
            return ResolveSprite(
                request,
                spriteResolver,
                allowFallbackForThisRequest: true,
                out usedGeneratedFallback);
        }

        private Sprite ResolveSprite(
            ArcGraphSpriteResolveRequest request,
            IArcGraphSpriteResolver spriteResolver,
            bool allowFallbackForThisRequest,
            out bool usedGeneratedFallback)
        {
            usedGeneratedFallback = false;

            if (spriteResolver != null
                && spriteResolver.TryResolveSprite(request, out Sprite resolved)
                && resolved != null)
            {
                return resolved;
            }

            // Le parti modulari dell'NPC non devono produrre quadrati magenta
            // giganti se manca un singolo PNG: meglio saltare la parte, loggare
            // missingSprites e lasciare visibile il resto del corpo. Il fallback
            // resta invece disponibile per il path non modulare, dove senza sprite
            // l'intero actor sparirebbe e il gate visuale sarebbe meno leggibile.
            if (!allowFallbackForThisRequest || !allowGeneratedFallbackSprites)
                return null;

            usedGeneratedFallback = true;
            return GetOrCreateFallbackSprite();
        }

        private static void DestroyUnityObject(Object unityObject)
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
            ClearRuntimeRenderer();
        }
    }
}
