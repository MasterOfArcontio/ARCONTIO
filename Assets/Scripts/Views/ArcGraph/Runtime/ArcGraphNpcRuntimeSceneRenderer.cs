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
        [SerializeField] private bool rendererEnabled;
        [SerializeField] private bool renderOnStart;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private bool allowGeneratedFallbackSprites = true;
        [SerializeField] private bool disableMissingActorsAfterRender = true;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private float actorZOffset = -0.02f;
        [SerializeField] private float actorScale = 1f;
        [SerializeField] private string runtimeRootName = "ArcGraphNpcRuntimeRoot";

        private readonly Dictionary<int, ActorHandle> _actorPool = new();
        private readonly ArcGraphActorObjectSceneRenderPlan _plan = new();
        private readonly ArcGraphActorObjectSceneRenderPlanBuilder _planBuilder = new();
        private Transform _root;
        private Sprite _generatedFallbackSprite;
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
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, 0, "RendererDisabled");

            if (!contract.IsRuntimeSafe)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, 0, "UnsafeContract");

            if (queue == null)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, 0, "QueueMissing");

            ArcGraphActorObjectSceneRendererContract planContract = contract.CreateActorObjectPlanContract();
            ArcGraphActorObjectSceneRendererDiagnostics planDiagnostics = _planBuilder.Build(
                queue,
                planContract,
                _plan,
                hasSpriteResolver,
                clearPlan: true);

            if (!planDiagnostics.ContractSafe)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, 0, "UnsafePlanContract");

            ApplyActorEntries(
                _plan,
                contract,
                spriteResolver,
                out int rendered,
                out int created,
                out int reused,
                out int disabled,
                out int missingSprites,
                out int generatedFallbacks);

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
            out int generatedFallbacks)
        {
            rendered = 0;
            created = 0;
            reused = 0;
            disabled = 0;
            missingSprites = 0;
            generatedFallbacks = 0;

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

                Sprite sprite = ResolveSprite(entry, spriteResolver, out bool usedGeneratedFallback);
                if (sprite == null)
                {
                    missingSprites++;
                    continue;
                }

                ActorHandle handle = GetOrCreateActorHandle(entry.EntityId, contract, out bool wasCreated);
                ApplyActorEntry(handle, entry, sprite, contract);

                rendered++;
                if (usedGeneratedFallback)
                    generatedFallbacks++;

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
            ArcGraphNpcRuntimeSceneRendererContract contract)
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
            handle.GameObject.SetActive(true);
            handle.WasTouchedThisFrame = true;
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
                ", generatedFallbacks=" + _lastDiagnostics.GeneratedFallbackSpriteCount);
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
