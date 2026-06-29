using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectRuntimeSceneRenderer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Renderer runtime minimo per materializzare gli oggetti ArcGraph in scena.
    /// </para>
    ///
    /// <para><b>Principio architetturale: renderer oggetti separato dagli NPC</b></para>
    /// <para>
    /// Gli oggetti, inclusi i muri <c>wall_stone</c>, usano la stessa
    /// <c>ArcGraphRenderQueue</c> degli actor ma vengono materializzati da un
    /// renderer dedicato. Questo evita di trasformare il renderer NPC in un
    /// contenitore generico e prepara l'estensione futura per alberi, mobili,
    /// oggetti alti, trasparenze e shadow senza mescolare responsabilita'.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>runtimeWrapper</b>: sorgente opzionale della queue gia' costruita.</item>
    ///   <item><b>spriteResolverBehaviour</b>: resolver sprite scene-side assegnato da Inspector o auto-installer.</item>
    ///   <item><b>_objectPool</b>: handle riusabili indicizzati per objectId.</item>
    ///   <item><b>RenderFromQueue</b>: entry point runtime produttivo minimo.</item>
    ///   <item><b>ClearRuntimeRenderer</b>: cleanup confinato al root oggetti ArcGraph.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphObjectRuntimeSceneRenderer : MonoBehaviour
    {
        [SerializeField] private ArcGraphMinimalRuntimeSceneWrapper runtimeWrapper;
        [SerializeField] private MonoBehaviour spriteResolverBehaviour;
        [SerializeField] private bool rendererEnabled;
        [SerializeField] private bool renderOnStart;
        [SerializeField] private bool logDiagnostics;
        [SerializeField] private bool disableMissingObjectsAfterRender = true;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private float objectZOffset = -0.01f;
        [SerializeField] private float objectScale = 1f;
        [SerializeField] private string runtimeRootName = "ArcGraphObjectRuntimeRoot";

        private readonly Dictionary<int, ObjectHandle> _objectPool = new();
        private readonly Dictionary<int, float> _objectAlphaOverrides = new();
        private readonly ArcGraphActorObjectSceneRenderPlan _plan = new();
        private readonly ArcGraphActorObjectSceneRenderPlanBuilder _planBuilder = new();
        private Transform _root;
        private ArcGraphObjectRuntimeSceneRendererDiagnostics _lastDiagnostics;

        public ArcGraphObjectRuntimeSceneRendererDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool RendererEnabled => rendererEnabled;
        public int PooledObjectCount => _objectPool.Count;

        // =============================================================================
        // ObjectHandle
        // =============================================================================
        /// <summary>
        /// <para>
        /// Handle locale di un oggetto materializzato in scena.
        /// </para>
        ///
        /// <para><b>Pooling per identita' oggetto</b></para>
        /// <para>
        /// Ogni handle resta associato a un <c>objectId</c>. Quando la queue viene
        /// ricostruita dopo un comando F3, il renderer aggiorna sprite e posizione
        /// degli stessi GameObject invece di distruggerli e ricrearli a ogni frame.
        /// </para>
        /// </summary>
        private sealed class ObjectHandle
        {
            public int ObjectId;
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public bool WasTouchedThisFrame;
        }

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia opzionalmente il rendering oggetti dalla queue del wrapper.
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
        /// Abilita o disabilita il gate principale del renderer oggetti.
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
        /// Assegna esplicitamente il wrapper minimo da cui leggere la queue.
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
        // SetObjectAlphaOverride
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica un alpha visuale temporaneo a un singolo oggetto materializzato.
        /// </para>
        ///
        /// <para><b>Principio architetturale: effetto renderer-only</b></para>
        /// <para>
        /// L'override non entra nella render queue, non modifica object snapshot e
        /// non cambia lo stato del <c>World</c>. E' un attributo di scena usato da
        /// controller visuali come la trasparenza muri. Quando il renderer aggiorna
        /// lo sprite dell'oggetto, riapplica lo stesso alpha senza perdere il fade.
        /// </para>
        /// </summary>
        public void SetObjectAlphaOverride(
            int objectId,
            float alpha)
        {
            if (objectId <= 0)
                return;

            float safeAlpha = Mathf.Clamp01(alpha);
            if (safeAlpha >= 0.999f)
            {
                ClearObjectAlphaOverride(objectId);
                return;
            }

            _objectAlphaOverrides[objectId] = safeAlpha;

            if (_objectPool.TryGetValue(objectId, out ObjectHandle handle))
                ApplyObjectAlpha(handle);
        }

        // =============================================================================
        // ClearObjectAlphaOverride
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove l'override alpha visuale di un oggetto e ripristina opacita'
        /// piena sul renderer esistente.
        /// </para>
        /// </summary>
        public void ClearObjectAlphaOverride(int objectId)
        {
            if (objectId <= 0)
                return;

            _objectAlphaOverrides.Remove(objectId);

            if (_objectPool.TryGetValue(objectId, out ObjectHandle handle))
                ApplyObjectAlpha(handle);
        }

        // =============================================================================
        // ClearObjectAlphaOverrides
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cancella tutti gli alpha visuali temporanei applicati dal controller
        /// esterno.
        /// </para>
        /// </summary>
        public void ClearObjectAlphaOverrides()
        {
            _objectAlphaOverrides.Clear();

            foreach (var pair in _objectPool)
                ApplyObjectAlpha(pair.Value);
        }

        [ContextMenu("ArcGraph/Render Object Runtime From Wrapper Queue")]
        public void RenderFromRuntimeWrapperContextMenu()
        {
            RenderFromRuntimeWrapper();
        }

        // =============================================================================
        // RenderFromRuntimeWrapper
        // =============================================================================
        /// <summary>
        /// <para>
        /// Renderizza gli oggetti usando la queue gia' esposta dal wrapper runtime.
        /// </para>
        /// </summary>
        public ArcGraphObjectRuntimeSceneRendererDiagnostics RenderFromRuntimeWrapper()
        {
            return RenderFromQueue(runtimeWrapper != null ? runtimeWrapper.RenderQueue : null);
        }

        // =============================================================================
        // RenderFromQueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica in scena gli oggetti presenti nella render queue ArcGraph.
        /// </para>
        /// </summary>
        public ArcGraphObjectRuntimeSceneRendererDiagnostics RenderFromQueue(
            ArcGraphRenderQueue queue)
        {
            ArcGraphObjectRuntimeSceneRendererContract contract = CreateContract();
            IArcGraphSpriteResolver spriteResolver = ResolveSpriteResolver();
            bool hasSpriteResolver = spriteResolver != null;

            if (!rendererEnabled)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, "RendererDisabled");

            if (!contract.IsRuntimeSafe)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, "UnsafeContract");

            if (queue == null)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, "QueueMissing");

            ArcGraphActorObjectSceneRendererContract planContract = contract.CreateActorObjectPlanContract();
            ArcGraphActorObjectSceneRendererDiagnostics planDiagnostics = _planBuilder.Build(
                queue,
                planContract,
                _plan,
                hasSpriteResolver,
                clearPlan: true);

            if (!planDiagnostics.ContractSafe)
                return StoreAndLogDiagnostics(contract, queue, hasSpriteResolver, false, 0, 0, 0, 0, 0, 0, "UnsafePlanContract");

            ApplyObjectEntries(
                _plan,
                contract,
                spriteResolver,
                out int objectEntries,
                out int rendered,
                out int created,
                out int reused,
                out int disabled,
                out int missingSprites);

            string reason = rendered > 0
                ? "ObjectRuntimeRendered"
                : "ObjectRuntimeNoVisibleObjects";

            return StoreAndLogDiagnostics(
                contract,
                queue,
                hasSpriteResolver,
                builtPlan: true,
                objectEntries,
                rendered,
                created,
                reused,
                disabled,
                missingSprites,
                reason);
        }

        // =============================================================================
        // ClearRuntimeRenderer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove tutti gli oggetti runtime creati da questo renderer.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear Object Runtime Renderer")]
        public void ClearRuntimeRenderer()
        {
            _objectPool.Clear();
            _objectAlphaOverrides.Clear();

            if (_root == null)
                return;

            DestroyUnityObject(_root.gameObject);
            _root = null;
        }

        [ContextMenu("ArcGraph/Log Last Object Runtime Diagnostics")]
        public void LogLastDiagnosticsFromContextMenu()
        {
            LogLastDiagnostics();
        }

        private ArcGraphObjectRuntimeSceneRendererContract CreateContract()
        {
            return new ArcGraphObjectRuntimeSceneRendererContract(
                runtimeRootName,
                tileWorldSize,
                originOffset,
                objectZOffset,
                objectScale,
                disableMissingObjectsAfterRender);
        }

        private IArcGraphSpriteResolver ResolveSpriteResolver()
        {
            return spriteResolverBehaviour as IArcGraphSpriteResolver;
        }

        private void ApplyObjectEntries(
            ArcGraphActorObjectSceneRenderPlan plan,
            ArcGraphObjectRuntimeSceneRendererContract contract,
            IArcGraphSpriteResolver spriteResolver,
            out int objectEntries,
            out int rendered,
            out int created,
            out int reused,
            out int disabled,
            out int missingSprites)
        {
            objectEntries = 0;
            rendered = 0;
            created = 0;
            reused = 0;
            disabled = 0;
            missingSprites = 0;

            if (plan == null)
                return;

            EnsureRoot(contract);
            MarkAllHandlesUntouched();

            IReadOnlyList<ArcGraphActorObjectSceneRenderEntry> entries = plan.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                ArcGraphActorObjectSceneRenderEntry entry = entries[i];
                if (entry.Kind != ArcGraphRenderItemKind.Object)
                    continue;

                objectEntries++;
                Sprite sprite = ResolveSprite(entry, spriteResolver);
                if (sprite == null)
                {
                    missingSprites++;
                    continue;
                }

                ObjectHandle handle = GetOrCreateObjectHandle(entry.EntityId, contract, out bool wasCreated);
                ApplyObjectEntry(handle, entry, sprite, contract);

                rendered++;
                if (wasCreated)
                    created++;
                else
                    reused++;
            }

            if (contract.DisableMissingObjectsAfterRender)
                disabled = DisableUntouchedHandles();
        }

        private Sprite ResolveSprite(
            ArcGraphActorObjectSceneRenderEntry entry,
            IArcGraphSpriteResolver spriteResolver)
        {
            if (spriteResolver != null
                && spriteResolver.TryResolveSprite(entry.SpriteRequest, out Sprite resolved)
                && resolved != null)
            {
                return resolved;
            }

            return null;
        }

        private ObjectHandle GetOrCreateObjectHandle(
            int objectId,
            ArcGraphObjectRuntimeSceneRendererContract contract,
            out bool wasCreated)
        {
            if (_objectPool.TryGetValue(objectId, out var handle))
            {
                wasCreated = false;
                return handle;
            }

            EnsureRoot(contract);

            var go = new GameObject("ArcGraphObjectRuntimeObject_" + objectId);
            go.transform.SetParent(_root, false);

            handle = new ObjectHandle
            {
                ObjectId = objectId,
                GameObject = go,
                Renderer = go.AddComponent<SpriteRenderer>(),
                WasTouchedThisFrame = true
            };

            _objectPool[objectId] = handle;
            wasCreated = true;
            return handle;
        }

        private void ApplyObjectEntry(
            ObjectHandle handle,
            ArcGraphActorObjectSceneRenderEntry entry,
            Sprite sprite,
            ArcGraphObjectRuntimeSceneRendererContract contract)
        {
            // Posizione e sorting arrivano dal plan builder: il renderer aggiunge
            // solo offset e scala locali, senza rileggere celle, World o MapGrid.
            // Per gli oggetti con pivot basso compensiamo anche il pivot reale
            // importato da Unity: se una sub-sprite 32x83 resta con pivot Center,
            // il bordo basso verrebbe disegnato molto sotto la cella logica.
            Vector3 localPosition = contract.OriginOffset + new Vector3(
                entry.WorldX,
                entry.WorldY,
                entry.WorldZ + contract.ZOffset);
            localPosition += ResolveSpritePivotCompensation(
                entry,
                sprite,
                contract.ObjectScale);

            handle.GameObject.transform.localPosition = localPosition;
            handle.GameObject.transform.localScale = Vector3.one * contract.ObjectScale;
            handle.Renderer.sprite = sprite;
            handle.Renderer.sortingOrder = entry.SortingOrder;
            handle.Renderer.enabled = true;
            ApplyObjectAlpha(handle);
            handle.GameObject.SetActive(true);
            handle.WasTouchedThisFrame = true;
        }

        // =============================================================================
        // ApplyObjectAlpha
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica al renderer Unity il colore derivato dall'override alpha
        /// corrente.
        /// </para>
        /// </summary>
        private void ApplyObjectAlpha(ObjectHandle handle)
        {
            if (handle == null || handle.Renderer == null)
                return;

            float alpha = ResolveObjectAlpha(handle.ObjectId);
            handle.Renderer.color = new Color(1f, 1f, 1f, alpha);
        }

        private float ResolveObjectAlpha(int objectId)
        {
            if (objectId > 0
                && _objectAlphaOverrides.TryGetValue(objectId, out float alpha))
            {
                return Mathf.Clamp01(alpha);
            }

            return 1f;
        }

        private static Vector3 ResolveSpritePivotCompensation(
            ArcGraphActorObjectSceneRenderEntry entry,
            Sprite sprite,
            float objectScale)
        {
            if (sprite == null || entry.Kind != ArcGraphRenderItemKind.Object)
                return Vector3.zero;

            float safeScale = objectScale > 0f ? objectScale : 1f;
            Bounds bounds = sprite.bounds;
            float x = 0f;
            float y = 0f;

            if (IsPivot(entry.VisualPivot, "bottom_left"))
                x = -bounds.min.x * safeScale;
            else if (IsPivot(entry.VisualPivot, "bottom_right"))
                x = -bounds.max.x * safeScale;

            if (IsBottomPivot(entry.VisualPivot))
                y = -bounds.min.y * safeScale;

            return new Vector3(x, y, 0f);
        }

        private static bool IsBottomPivot(
            string pivot)
        {
            return IsPivot(pivot, "bottom_center")
                   || IsPivot(pivot, "bottom_left")
                   || IsPivot(pivot, "bottom_right");
        }

        private static bool IsPivot(
            string pivot,
            string expected)
        {
            return string.Equals(
                pivot ?? string.Empty,
                expected ?? string.Empty,
                System.StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureRoot(ArcGraphObjectRuntimeSceneRendererContract contract)
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
            foreach (var pair in _objectPool)
                pair.Value.WasTouchedThisFrame = false;
        }

        private int DisableUntouchedHandles()
        {
            int disabled = 0;

            foreach (var pair in _objectPool)
            {
                ObjectHandle handle = pair.Value;
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

        private ArcGraphObjectRuntimeSceneRendererDiagnostics StoreAndLogDiagnostics(
            ArcGraphObjectRuntimeSceneRendererContract contract,
            ArcGraphRenderQueue queue,
            bool hasSpriteResolver,
            bool builtPlan,
            int objectEntryCount,
            int renderedObjectCount,
            int createdObjectCount,
            int reusedObjectCount,
            int disabledObjectCount,
            int missingSpriteCount,
            string reason)
        {
            _lastDiagnostics = new ArcGraphObjectRuntimeSceneRendererDiagnostics(
                rendererEnabled,
                hasContract: true,
                contract.IsRuntimeSafe,
                queue != null,
                hasSpriteResolver,
                builtPlan,
                queue != null ? queue.Entries.Count : 0,
                queue != null ? queue.ObjectItems.Count : 0,
                objectEntryCount,
                renderedObjectCount,
                createdObjectCount,
                reusedObjectCount,
                disabledObjectCount,
                CountActiveObjects(),
                missingSpriteCount,
                reason);

            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphObjectRuntimeSceneRenderer] " + _lastDiagnostics.Reason +
                " enabled=" + _lastDiagnostics.RendererEnabled +
                ", contractSafe=" + _lastDiagnostics.ContractSafe +
                ", queue=" + _lastDiagnostics.HasQueue +
                ", resolver=" + _lastDiagnostics.HasSpriteResolver +
                ", builtPlan=" + _lastDiagnostics.BuiltPlan +
                ", queueEntries=" + _lastDiagnostics.QueueEntryCount +
                ", objectItems=" + _lastDiagnostics.ObjectItemCount +
                ", objectEntries=" + _lastDiagnostics.ObjectEntryCount +
                ", rendered=" + _lastDiagnostics.RenderedObjectCount +
                ", created=" + _lastDiagnostics.CreatedObjectCount +
                ", reused=" + _lastDiagnostics.ReusedObjectCount +
                ", disabled=" + _lastDiagnostics.DisabledObjectCount +
                ", active=" + _lastDiagnostics.ActiveObjectCount +
                ", missingSprites=" + _lastDiagnostics.MissingSpriteCount);
        }

        private int CountActiveObjects()
        {
            int count = 0;

            foreach (var pair in _objectPool)
            {
                ObjectHandle handle = pair.Value;
                if (handle.GameObject != null && handle.GameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private static void DestroyUnityObject(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
                return;

            if (Application.isPlaying)
                Destroy(unityObject);
            else
                DestroyImmediate(unityObject);
        }
    }
}
