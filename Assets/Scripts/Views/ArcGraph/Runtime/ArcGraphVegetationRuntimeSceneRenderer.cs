using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVegetationRuntimeSceneRenderer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Renderer runtime minimo per materializzare piante e vegetazione diffusa
    /// ArcGraph in scena.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Biosfera visualizzata tramite ArcGraph</b></para>
    /// <para>
    /// Il renderer consuma esclusivamente <see cref="ArcGraphVegetationLayer"/> gia'
    /// popolato dal percorso World -> ArcGraph. Non legge direttamente Biosfera,
    /// non modifica World, non calcola crescita e non decide quali celle siano
    /// naturali. Il suo lavoro e' creare o aggiornare SpriteRenderer scene-side
    /// per snapshot vegetazione gia' autorizzati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>runtimeWrapper</b>: sorgente opzionale del runtime ArcGraph gia' processato.</item>
    ///   <item><b>spriteResolverBehaviour</b>: resolver sprite scene-side condiviso con oggetti/NPC.</item>
    ///   <item><b>_vegetationPool</b>: pool GameObject indicizzato da chiave stabile dell'item.</item>
    ///   <item><b>RenderFromRuntime</b>: entry point chiamabile dal wrapper.</item>
    ///   <item><b>Fallback</b>: sprite generato visibile quando manca un asset reale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphVegetationRuntimeSceneRenderer : MonoBehaviour
    {
        [SerializeField] private ArcGraphMinimalRuntimeSceneWrapper runtimeWrapper;
        [SerializeField] private MonoBehaviour spriteResolverBehaviour;
        [SerializeField] private bool rendererEnabled;
        [SerializeField] private bool renderOnStart;
        [SerializeField] private bool logDiagnostics;
        [SerializeField] private bool allowGeneratedFallbackSprites = true;
        [SerializeField] private bool disableMissingVegetationAfterRender = true;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private float vegetationZOffset;
        [SerializeField] private float vegetationScale = 1f;
        [SerializeField] private int baseSortingOrder = 10;
        [SerializeField] private string runtimeRootName = "ArcGraphVegetationRuntimeRoot";

        private readonly Dictionary<int, VegetationHandle> _vegetationPool = new();
        private readonly ArcGraphVegetationRenderQueueBuilder _queueBuilder = new();
        private readonly List<ArcGraphVegetationRenderItem> _items = new();
        private Transform _root;
        private Sprite _generatedFallbackSprite;
        private ArcGraphVegetationRuntimeSceneRendererDiagnostics _lastDiagnostics;

        public ArcGraphVegetationRuntimeSceneRendererDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool RendererEnabled => rendererEnabled;
        public int PooledVegetationCount => _vegetationPool.Count;

        // =============================================================================
        // VegetationHandle
        // =============================================================================
        /// <summary>
        /// <para>
        /// Handle locale di una cella vegetazione materializzata in scena.
        /// </para>
        ///
        /// <para><b>Pooling per item visuale</b></para>
        /// <para>
        /// L'id stabile deriva dalla sort key dell'item ArcGraph. In questo modo
        /// refresh successivi aggiornano posizione, sprite e sorting dello stesso
        /// GameObject invece di ricrearlo a ogni frame ambientale.
        /// </para>
        /// </summary>
        private sealed class VegetationHandle
        {
            public int EntityId;
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public bool WasTouchedThisFrame;
        }

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia opzionalmente il rendering vegetazione dal wrapper runtime.
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
        /// Abilita o disabilita il gate principale del renderer vegetazione.
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
        /// Assegna il wrapper minimo da cui leggere il runtime ArcGraph.
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
        /// Entry point manuale da Inspector per renderizzare dal wrapper runtime.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Render Vegetation Runtime From Wrapper")]
        public void RenderFromRuntimeWrapperContextMenu()
        {
            RenderFromRuntimeWrapper();
        }

        // =============================================================================
        // RenderFromRuntimeWrapper
        // =============================================================================
        /// <summary>
        /// <para>
        /// Renderizza vegetazione e piante usando il runtime esposto dal wrapper.
        /// </para>
        /// </summary>
        public ArcGraphVegetationRuntimeSceneRendererDiagnostics RenderFromRuntimeWrapper()
        {
            return RenderFromRuntime(runtimeWrapper != null ? runtimeWrapper.Runtime : null);
        }

        // =============================================================================
        // RenderFromRuntime
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica in scena gli item vegetazione derivati dal runtime ArcGraph.
        /// </para>
        ///
        /// <para><b>Sequenza visuale</b></para>
        /// <para>
        /// Il metodo legge il layer vegetazione, costruisce una queue ordinata,
        /// risolve gli sprite e aggiorna il pool. Se manca uno sprite reale e il
        /// fallback e' abilitato, viene mostrato un marker generato: cosi' il test
        /// runtime resta leggibile anche prima di avere tutti i PNG definitivi.
        /// </para>
        /// </summary>
        public ArcGraphVegetationRuntimeSceneRendererDiagnostics RenderFromRuntime(
            ArcGraphBootstrapRuntime runtime)
        {
            ArcGraphVegetationRuntimeSceneRendererContract contract = CreateContract();
            IArcGraphSpriteResolver spriteResolver = ResolveSpriteResolver();
            bool hasSpriteResolver = spriteResolver != null;

            if (!rendererEnabled)
                return StoreAndLogDiagnostics(contract, runtime, false, hasSpriteResolver, 0, 0, 0, 0, 0, 0, 0, 0, "RendererDisabled");

            if (!contract.IsRuntimeSafe)
                return StoreAndLogDiagnostics(contract, runtime, false, hasSpriteResolver, 0, 0, 0, 0, 0, 0, 0, 0, "UnsafeContract");

            if (runtime == null || !runtime.IsInitialized || runtime.LayerStack == null)
                return StoreAndLogDiagnostics(contract, runtime, false, hasSpriteResolver, 0, 0, 0, 0, 0, 0, 0, 0, "RuntimeMissingOrNotInitialized");

            if (!runtime.LayerStack.TryGetLayer<ArcGraphVegetationLayer>(out var vegetationLayer))
                return StoreAndLogDiagnostics(contract, runtime, false, hasSpriteResolver, 0, 0, 0, 0, 0, 0, 0, 0, "VegetationLayerMissing");

            _items.Clear();
            ArcGraphVegetationRenderQueueDiagnostics queueDiagnostics = _queueBuilder.Build(
                vegetationLayer,
                ArcGraphZoomLodPolicy.ResolveFullDetail(),
                _items);

            ApplyItems(
                _items,
                contract,
                spriteResolver,
                out int rendered,
                out int created,
                out int reused,
                out int disabled,
                out int missingSprites,
                out int generatedFallbacks);

            string reason = rendered > 0
                ? "VegetationRuntimeRendered"
                : "VegetationRuntimeNoVisibleItems";

            return StoreAndLogDiagnostics(
                contract,
                runtime,
                true,
                hasSpriteResolver,
                queueDiagnostics.SnapshotCount,
                _items.Count,
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
        /// Rimuove tutti gli sprite vegetazione creati da questo renderer.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear Vegetation Runtime Renderer")]
        public void ClearRuntimeRenderer()
        {
            _vegetationPool.Clear();

            if (_root != null)
            {
                DestroyUnityObject(_root.gameObject);
                _root = null;
            }

            if (_generatedFallbackSprite != null)
            {
                Texture2D texture = _generatedFallbackSprite.texture;
                DestroyUnityObject(_generatedFallbackSprite);
                DestroyUnityObject(texture);
                _generatedFallbackSprite = null;
            }
        }

        private ArcGraphVegetationRuntimeSceneRendererContract CreateContract()
        {
            return new ArcGraphVegetationRuntimeSceneRendererContract(
                runtimeRootName,
                tileWorldSize,
                originOffset,
                vegetationZOffset,
                vegetationScale,
                baseSortingOrder,
                disableMissingVegetationAfterRender);
        }

        private IArcGraphSpriteResolver ResolveSpriteResolver()
        {
            return spriteResolverBehaviour as IArcGraphSpriteResolver;
        }

        private void ApplyItems(
            IReadOnlyList<ArcGraphVegetationRenderItem> items,
            ArcGraphVegetationRuntimeSceneRendererContract contract,
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

            if (items == null)
                return;

            EnsureRoot(contract);
            MarkAllHandlesUntouched();

            for (int i = 0; i < items.Count; i++)
            {
                ArcGraphVegetationRenderItem item = items[i];
                if (!item.IsVisible)
                    continue;

                Sprite sprite = ResolveSprite(item, spriteResolver, out bool usedGeneratedFallback);
                if (sprite == null)
                {
                    missingSprites++;
                    continue;
                }

                if (usedGeneratedFallback)
                {
                    missingSprites++;
                    generatedFallbacks++;
                }

                int entityId = ResolveEntityId(item);
                VegetationHandle handle = GetOrCreateHandle(entityId, contract, out bool wasCreated);
                ApplyItem(handle, item, sprite, contract, i);

                rendered++;
                if (wasCreated)
                    created++;
                else
                    reused++;
            }

            if (contract.DisableMissingVegetationAfterRender)
                disabled = DisableUntouchedHandles();
        }

        private Sprite ResolveSprite(
            ArcGraphVegetationRenderItem item,
            IArcGraphSpriteResolver spriteResolver,
            out bool usedGeneratedFallback)
        {
            usedGeneratedFallback = false;

            var request = new ArcGraphSpriteResolveRequest(
                ArcGraphRenderItemKind.Vegetation,
                ResolveEntityId(item),
                item.SpriteKey,
                item.SpeciesKey);

            if (spriteResolver != null
                && spriteResolver.TryResolveSprite(request, out Sprite resolved)
                && resolved != null)
            {
                return resolved;
            }

            if (!allowGeneratedFallbackSprites)
                return null;

            usedGeneratedFallback = true;
            return GetOrCreateFallbackSprite();
        }

        private VegetationHandle GetOrCreateHandle(
            int entityId,
            ArcGraphVegetationRuntimeSceneRendererContract contract,
            out bool wasCreated)
        {
            if (_vegetationPool.TryGetValue(entityId, out var handle))
            {
                wasCreated = false;
                return handle;
            }

            EnsureRoot(contract);

            var go = new GameObject("ArcGraphVegetationRuntimeItem_" + entityId);
            go.transform.SetParent(_root, false);

            handle = new VegetationHandle
            {
                EntityId = entityId,
                GameObject = go,
                Renderer = go.AddComponent<SpriteRenderer>(),
                WasTouchedThisFrame = true
            };

            _vegetationPool[entityId] = handle;
            wasCreated = true;
            return handle;
        }

        private void ApplyItem(
            VegetationHandle handle,
            ArcGraphVegetationRenderItem item,
            Sprite sprite,
            ArcGraphVegetationRuntimeSceneRendererContract contract,
            int queueIndex)
        {
            Vector3 localPosition = contract.OriginOffset + new Vector3(
                (item.Cell.X + 0.5f) * contract.TileWorldSize,
                (item.Cell.Y + 0.5f) * contract.TileWorldSize,
                item.Cell.Z + contract.ZOffset);

            handle.GameObject.transform.localPosition = localPosition;
            handle.GameObject.transform.localScale = Vector3.one * contract.VegetationScale;
            handle.Renderer.sprite = sprite;
            handle.Renderer.sortingOrder = contract.BaseSortingOrder + queueIndex;
            handle.Renderer.color = ResolveTint(item);
            handle.Renderer.enabled = true;
            handle.GameObject.SetActive(true);
            handle.WasTouchedThisFrame = true;
        }

        private static Color ResolveTint(ArcGraphVegetationRenderItem item)
        {
            if (item.Density01 >= 0.99f)
                return Color.white;

            float alpha = Mathf.Lerp(0.55f, 1f, item.Density01);
            return new Color(1f, 1f, 1f, alpha);
        }

        private int DisableUntouchedHandles()
        {
            int disabled = 0;

            foreach (var pair in _vegetationPool)
            {
                VegetationHandle handle = pair.Value;
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

        private void MarkAllHandlesUntouched()
        {
            foreach (var pair in _vegetationPool)
                pair.Value.WasTouchedThisFrame = false;
        }

        private void EnsureRoot(ArcGraphVegetationRuntimeSceneRendererContract contract)
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

        private Sprite GetOrCreateFallbackSprite()
        {
            if (_generatedFallbackSprite != null)
                return _generatedFallbackSprite;

            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                name = "ArcGraphVegetationGeneratedFallbackTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    texture.SetPixel(
                        x,
                        y,
                        border
                            ? new Color(0.02f, 0.22f, 0.04f, 1f)
                            : new Color(0.2f, 0.85f, 0.18f, 0.85f));
                }
            }

            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            _generatedFallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 16f);
            _generatedFallbackSprite.name = "ArcGraphVegetationGeneratedFallbackSprite";
            return _generatedFallbackSprite;
        }

        private ArcGraphVegetationRuntimeSceneRendererDiagnostics StoreAndLogDiagnostics(
            ArcGraphVegetationRuntimeSceneRendererContract contract,
            ArcGraphBootstrapRuntime runtime,
            bool hasVegetationLayer,
            bool hasSpriteResolver,
            int snapshotCount,
            int renderItemCount,
            int renderedVegetationCount,
            int createdObjectCount,
            int reusedObjectCount,
            int disabledObjectCount,
            int missingSpriteCount,
            int generatedFallbackSpriteCount,
            string reason)
        {
            _lastDiagnostics = new ArcGraphVegetationRuntimeSceneRendererDiagnostics(
                rendererEnabled,
                hasContract: true,
                contract.IsRuntimeSafe,
                runtime != null,
                hasVegetationLayer,
                hasSpriteResolver,
                snapshotCount,
                renderItemCount,
                renderedVegetationCount,
                createdObjectCount,
                reusedObjectCount,
                disabledObjectCount,
                CountActiveObjects(),
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
                "[ArcGraphVegetationRuntimeSceneRenderer] " + _lastDiagnostics.Reason +
                " enabled=" + _lastDiagnostics.RendererEnabled +
                ", contractSafe=" + _lastDiagnostics.ContractSafe +
                ", runtime=" + _lastDiagnostics.HasRuntime +
                ", vegetationLayer=" + _lastDiagnostics.HasVegetationLayer +
                ", resolver=" + _lastDiagnostics.HasSpriteResolver +
                ", snapshots=" + _lastDiagnostics.SnapshotCount +
                ", items=" + _lastDiagnostics.RenderItemCount +
                ", rendered=" + _lastDiagnostics.RenderedVegetationCount +
                ", created=" + _lastDiagnostics.CreatedObjectCount +
                ", reused=" + _lastDiagnostics.ReusedObjectCount +
                ", disabled=" + _lastDiagnostics.DisabledObjectCount +
                ", active=" + _lastDiagnostics.ActiveObjectCount +
                ", missingSprites=" + _lastDiagnostics.MissingSpriteCount +
                ", generatedFallbacks=" + _lastDiagnostics.GeneratedFallbackSpriteCount);
        }

        private int CountActiveObjects()
        {
            int count = 0;

            foreach (var pair in _vegetationPool)
            {
                VegetationHandle handle = pair.Value;
                if (handle.GameObject != null && handle.GameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private static int ResolveEntityId(ArcGraphVegetationRenderItem item)
        {
            return item.SortKey.EntityId;
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
