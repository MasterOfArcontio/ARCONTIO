using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectOcclusionFadeController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller runtime visual-only per muri/oggetti alti che coprono actor o
    /// oggetti ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: correzione visuale fuori dalla simulazione</b></para>
    /// <para>
    /// Il controller legge solo render queue, frame interattivo e selezione UI
    /// locale. Non legge <c>World</c>, non modifica oggetti, non cambia FOV, non
    /// cambia pathfinding e non decide comandi. La sua responsabilita' e' soltanto
    /// applicare alpha temporanei ai renderer muri e disegnare sagome leggere sopra
    /// l'occluder quando un target visuale resta dietro un muro alto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>renderQueue</b>: sorgente actor/object gia' derivata da ArcGraph.</item>
    ///   <item><b>interactionWrapper</b>: sorgente del frame puntatore corrente.</item>
    ///   <item><b>selectionConsumer</b>: sorgente della selezione UI locale.</item>
    ///   <item><b>objectRenderer</b>: unico componente autorizzato a ricevere alpha objectId.</item>
    ///   <item><b>spriteResolverBehaviour</b>: resolver sprite scene-side per le sagome.</item>
    ///   <item><b>_currentObjectAlphas</b>: fade morbido dei soli occluder.</item>
    ///   <item><b>_silhouettePool</b>: overlay riusabile per actor/object coperti.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphObjectOcclusionFadeController : MonoBehaviour
    {
        [SerializeField] private bool controllerEnabled = true;
        [SerializeField] private bool processInUpdate = true;
        [SerializeField] private bool enablePointerFade = true;
        [SerializeField] private bool enableSelectionFade = true;
        [SerializeField] private bool enableSilhouettes = true;
        [SerializeField] private float fadedAlpha = 0.35f;
        [SerializeField] private float fadeDurationSeconds = 0.12f;
        [SerializeField] private int maximumOcclusionDepthCells = ArcGraphOcclusionPolicy.DefaultMaximumDepthCells;
        [SerializeField] private Color silhouetteTint = new Color(0.7f, 0.9f, 1f, 0.34f);
        [SerializeField] private int silhouetteSortingOffset = 240;
        [SerializeField] private Vector3 silhouetteWorldOffset = new Vector3(0f, 0f, -0.04f);
        [SerializeField] private string silhouetteRootName = "ArcGraphOcclusionSilhouetteRoot";

        [SerializeField] private ArcGraphMinimalRuntimeSceneWrapper runtimeWrapper;
        [SerializeField] private ArcGraphInteractionSceneAdapterWrapper interactionWrapper;
        [SerializeField] private ArcGraphUiSelectionSceneConsumer selectionConsumer;
        [SerializeField] private ArcGraphObjectRuntimeSceneRenderer objectRenderer;
        [SerializeField] private MonoBehaviour spriteResolverBehaviour;

        private readonly HashSet<int> _desiredFadedObjectIds = new();
        private readonly Dictionary<int, float> _currentObjectAlphas = new();
        private readonly List<int> _alphaKeysScratch = new();
        private readonly Dictionary<string, SilhouetteHandle> _silhouettePool = new();
        private readonly HashSet<string> _desiredSilhouetteKeys = new();
        private readonly ArcGraphActorObjectSceneRenderPlan _scenePlan = new();
        private readonly ArcGraphActorObjectSceneRenderPlanBuilder _scenePlanBuilder = new();

        private Transform _silhouetteRoot;

        // =============================================================================
        // SilhouetteHandle
        // =============================================================================
        /// <summary>
        /// <para>
        /// Handle locale di una sagoma visuale materializzata sopra un muro.
        /// </para>
        ///
        /// <para><b>Overlay non autoritativo</b></para>
        /// <para>
        /// L'handle conserva solo GameObject e SpriteRenderer creati dal controller.
        /// Non e' una copia dell'NPC o dell'oggetto nel mondo: e' un segnale grafico
        /// leggero, cancellabile e ricostruibile a ogni frame.
        /// </para>
        /// </summary>
        private sealed class SilhouetteHandle
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public bool WasTouchedThisFrame;
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa il controller in Update quando il wiring runtime lo abilita.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!processInUpdate)
                return;

            ProcessFrame();
        }

        private void OnDisable()
        {
            ClearVisualState();
        }

        // =============================================================================
        // SetRuntimeWrapper
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il wrapper runtime da cui leggere la render queue corrente.
        /// </para>
        /// </summary>
        public void SetRuntimeWrapper(ArcGraphMinimalRuntimeSceneWrapper wrapper)
        {
            runtimeWrapper = wrapper;
        }

        public void SetInteractionWrapper(ArcGraphInteractionSceneAdapterWrapper wrapper)
        {
            interactionWrapper = wrapper;
        }

        public void SetSelectionConsumer(ArcGraphUiSelectionSceneConsumer consumer)
        {
            selectionConsumer = consumer;
        }

        public void SetObjectRenderer(ArcGraphObjectRuntimeSceneRenderer renderer)
        {
            objectRenderer = renderer;
        }

        public void SetSpriteResolverBehaviour(MonoBehaviour resolverBehaviour)
        {
            spriteResolverBehaviour = resolverBehaviour;
        }

        public void SetControllerEnabled(bool enabled)
        {
            controllerEnabled = enabled;

            if (!enabled)
                ClearVisualState();
        }

        public void SetProcessInUpdate(bool enabled)
        {
            processInUpdate = enabled;
        }

        // =============================================================================
        // ProcessFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola fade e sagome per il frame visuale corrente.
        /// </para>
        ///
        /// <para><b>Sequenza visuale dichiarata</b></para>
        /// <para>
        /// Prima raccogliamo gli occluder da puntatore/selezione, poi aggiorniamo
        /// alpha in modo morbido, infine ricostruiamo le sagome dei target coperti.
        /// Ogni passaggio lavora su dati gia' derivati e resta confinato alla scena.
        /// </para>
        /// </summary>
        public void ProcessFrame()
        {
            ArcGraphRenderQueue queue = ResolveRenderQueue();
            if (!controllerEnabled || queue == null || objectRenderer == null)
            {
                ClearVisualState();
                return;
            }

            _desiredFadedObjectIds.Clear();
            _desiredSilhouetteKeys.Clear();

            CollectPointerFade(queue);
            CollectSelectionFade(queue);

            UpdateObjectFadeAlphas();

            if (enableSilhouettes)
                RenderSilhouettes(queue);
            else
                DisableUntouchedSilhouettes(markAllUntouchedFirst: true);
        }

        private ArcGraphRenderQueue ResolveRenderQueue()
        {
            return runtimeWrapper != null ? runtimeWrapper.RenderQueue : null;
        }

        private IArcGraphSpriteResolver ResolveSpriteResolver()
        {
            return spriteResolverBehaviour as IArcGraphSpriteResolver;
        }

        private void CollectPointerFade(ArcGraphRenderQueue queue)
        {
            if (!enablePointerFade || interactionWrapper == null)
                return;

            ArcGraphInteractionFrame frame = interactionWrapper.LastInteractionFrame;
            if (frame.IsPointerOverUi || !frame.HasValidCell)
                return;

            if (TryFindPointedOccluderWithCoveredTarget(
                queue,
                frame.Cell,
                out ArcGraphObjectRenderItem occluder))
            {
                _desiredFadedObjectIds.Add(occluder.ObjectId);
            }
        }

        private void CollectSelectionFade(ArcGraphRenderQueue queue)
        {
            if (!enableSelectionFade || selectionConsumer == null)
                return;

            ArcUiSelectionTarget selection = selectionConsumer.CurrentSelection;
            if (!selection.IsValid)
                return;

            if (!TryResolveSelectionTargetCell(selection, out ArcGraphCellCoord targetCell, out int ignoredObjectId))
                return;

            if (ArcGraphOcclusionPolicy.TryFindOccluderCoveringTargetCell(
                queue.ObjectItems,
                targetCell,
                maximumOcclusionDepthCells,
                ignoredObjectId,
                out ArcGraphObjectRenderItem occluder))
            {
                _desiredFadedObjectIds.Add(occluder.ObjectId);
            }
        }

        private bool TryFindPointedOccluderWithCoveredTarget(
            ArcGraphRenderQueue queue,
            ArcGraphCellCoord pointerCell,
            out ArcGraphObjectRenderItem occluder)
        {
            occluder = default;

            if (queue.ObjectItems == null || queue.ObjectItems.Count == 0)
                return false;

            bool hasSelected = false;
            ArcGraphRenderSortKey selectedSortKey = default;

            for (int i = 0; i < queue.ObjectItems.Count; i++)
            {
                ArcGraphObjectRenderItem item = queue.ObjectItems[i];
                if (!ArcGraphOcclusionPolicy.IsFadeableOccluder(item))
                    continue;

                if (!ArcGraphOcclusionPolicy.IsObjectBaseHit(item, pointerCell))
                    continue;

                if (!ArcGraphOcclusionPolicy.TryPickCoveredTarget(
                    item,
                    queue.ActorItems,
                    queue.ObjectItems,
                    maximumOcclusionDepthCells,
                    out _))
                {
                    continue;
                }

                if (!hasSelected || item.SortKey.CompareTo(selectedSortKey) >= 0)
                {
                    occluder = item;
                    selectedSortKey = item.SortKey;
                    hasSelected = true;
                }
            }

            return hasSelected;
        }

        private static bool TryResolveSelectionTargetCell(
            ArcUiSelectionTarget selection,
            out ArcGraphCellCoord targetCell,
            out int ignoredObjectId)
        {
            targetCell = selection.Cell;
            ignoredObjectId = -1;

            if (selection.Kind == ArcUiSelectionTargetKind.Npc)
                return true;

            if (selection.Kind == ArcUiSelectionTargetKind.Object
                || selection.Kind == ArcUiSelectionTargetKind.Wall)
            {
                ignoredObjectId = TryParsePositiveInt(selection.Id, out int objectId)
                    ? objectId
                    : -1;
                return true;
            }

            return false;
        }

        private void UpdateObjectFadeAlphas()
        {
            _alphaKeysScratch.Clear();

            foreach (int objectId in _currentObjectAlphas.Keys)
                _alphaKeysScratch.Add(objectId);

            foreach (int objectId in _desiredFadedObjectIds)
            {
                if (!_currentObjectAlphas.ContainsKey(objectId))
                    _alphaKeysScratch.Add(objectId);
            }

            float targetFadeAlpha = Mathf.Clamp01(fadedAlpha);
            float duration = fadeDurationSeconds <= 0f ? 0.001f : fadeDurationSeconds;
            float step = Time.unscaledDeltaTime / duration;

            for (int i = 0; i < _alphaKeysScratch.Count; i++)
            {
                int objectId = _alphaKeysScratch[i];
                bool shouldFade = _desiredFadedObjectIds.Contains(objectId);
                float currentAlpha = _currentObjectAlphas.TryGetValue(objectId, out float storedAlpha)
                    ? storedAlpha
                    : 1f;
                float targetAlpha = shouldFade ? targetFadeAlpha : 1f;
                float nextAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, step);

                if (nextAlpha >= 0.999f && !shouldFade)
                {
                    _currentObjectAlphas.Remove(objectId);
                    objectRenderer.ClearObjectAlphaOverride(objectId);
                    continue;
                }

                _currentObjectAlphas[objectId] = nextAlpha;
                objectRenderer.SetObjectAlphaOverride(objectId, nextAlpha);
            }
        }

        private void RenderSilhouettes(ArcGraphRenderQueue queue)
        {
            IArcGraphSpriteResolver spriteResolver = ResolveSpriteResolver();
            if (spriteResolver == null)
            {
                DisableUntouchedSilhouettes(markAllUntouchedFirst: true);
                return;
            }

            EnsureSilhouetteRoot();
            MarkAllSilhouettesUntouched();

            ArcGraphActorObjectSceneRendererContract contract =
                ArcGraphActorObjectSceneRendererContract.CreateTemporaryProbeContract();
            _scenePlanBuilder.Build(
                queue,
                contract,
                _scenePlan,
                hasSpriteResolver: true,
                clearPlan: true);

            IReadOnlyList<ArcGraphObjectRenderItem> objects = queue.ObjectItems;
            if (objects != null)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    ArcGraphObjectRenderItem occluder = objects[i];
                    if (!ArcGraphOcclusionPolicy.TryPickCoveredTarget(
                        occluder,
                        queue.ActorItems,
                        queue.ObjectItems,
                        maximumOcclusionDepthCells,
                        out ArcGraphOcclusionTarget target))
                    {
                        continue;
                    }

                    TryRenderSilhouetteForTarget(target, spriteResolver);
                }
            }

            DisableUntouchedSilhouettes(markAllUntouchedFirst: false);
        }

        private bool TryRenderSilhouetteForTarget(
            ArcGraphOcclusionTarget target,
            IArcGraphSpriteResolver spriteResolver)
        {
            if (!target.IsValid)
                return false;

            if (!TryFindSceneEntry(target, out ArcGraphActorObjectSceneRenderEntry entry))
                return false;

            if (!spriteResolver.TryResolveSprite(entry.SpriteRequest, out Sprite sprite) || sprite == null)
                return false;

            string key = CreateSilhouetteKey(target);
            _desiredSilhouetteKeys.Add(key);

            SilhouetteHandle handle = GetOrCreateSilhouetteHandle(key);
            Vector3 localPosition = new Vector3(entry.WorldX, entry.WorldY, entry.WorldZ) + silhouetteWorldOffset;
            localPosition += ResolveSpritePivotCompensation(entry, sprite);

            handle.GameObject.transform.localPosition = localPosition;
            handle.GameObject.transform.localScale = Vector3.one;
            handle.Renderer.sprite = sprite;
            handle.Renderer.color = silhouetteTint;
            handle.Renderer.sortingOrder = entry.SortingOrder + silhouetteSortingOffset;
            handle.Renderer.enabled = true;
            handle.GameObject.SetActive(true);
            handle.WasTouchedThisFrame = true;
            return true;
        }

        private bool TryFindSceneEntry(
            ArcGraphOcclusionTarget target,
            out ArcGraphActorObjectSceneRenderEntry entry)
        {
            entry = default;
            IReadOnlyList<ArcGraphActorObjectSceneRenderEntry> entries = _scenePlan.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                ArcGraphActorObjectSceneRenderEntry candidate = entries[i];
                if (target.Kind == ArcGraphOcclusionTargetKind.Actor
                    && candidate.Kind == ArcGraphRenderItemKind.Actor
                    && candidate.EntityId == target.EntityId)
                {
                    entry = candidate;
                    return true;
                }

                if (target.Kind == ArcGraphOcclusionTargetKind.Object
                    && candidate.Kind == ArcGraphRenderItemKind.Object
                    && candidate.EntityId == target.EntityId)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        private SilhouetteHandle GetOrCreateSilhouetteHandle(string key)
        {
            if (_silhouettePool.TryGetValue(key, out SilhouetteHandle handle))
                return handle;

            EnsureSilhouetteRoot();
            var go = new GameObject("ArcGraphOcclusionSilhouette_" + key);
            go.transform.SetParent(_silhouetteRoot, false);

            handle = new SilhouetteHandle
            {
                GameObject = go,
                Renderer = go.AddComponent<SpriteRenderer>(),
                WasTouchedThisFrame = true
            };

            _silhouettePool[key] = handle;
            return handle;
        }

        private void EnsureSilhouetteRoot()
        {
            if (_silhouetteRoot != null)
                return;

            _silhouetteRoot = transform.Find(silhouetteRootName);
            if (_silhouetteRoot != null)
                return;

            var go = new GameObject(silhouetteRootName);
            go.transform.SetParent(transform, false);
            _silhouetteRoot = go.transform;
        }

        private void MarkAllSilhouettesUntouched()
        {
            foreach (var pair in _silhouettePool)
                pair.Value.WasTouchedThisFrame = false;
        }

        private void DisableUntouchedSilhouettes(bool markAllUntouchedFirst)
        {
            if (markAllUntouchedFirst)
                MarkAllSilhouettesUntouched();

            foreach (var pair in _silhouettePool)
            {
                SilhouetteHandle handle = pair.Value;
                if (handle.WasTouchedThisFrame)
                    continue;

                if (handle.GameObject != null && handle.GameObject.activeSelf)
                    handle.GameObject.SetActive(false);
            }
        }

        private void ClearVisualState()
        {
            _desiredFadedObjectIds.Clear();
            _currentObjectAlphas.Clear();
            objectRenderer?.ClearObjectAlphaOverrides();
            DisableUntouchedSilhouettes(markAllUntouchedFirst: true);
        }

        private static string CreateSilhouetteKey(ArcGraphOcclusionTarget target)
        {
            return target.Kind + "_" + target.EntityId;
        }

        private static Vector3 ResolveSpritePivotCompensation(
            ArcGraphActorObjectSceneRenderEntry entry,
            Sprite sprite)
        {
            if (sprite == null || entry.Kind != ArcGraphRenderItemKind.Object)
                return Vector3.zero;

            Bounds bounds = sprite.bounds;
            float x = 0f;
            float y = 0f;

            if (IsPivot(entry.VisualPivot, "bottom_left"))
                x = -bounds.min.x;
            else if (IsPivot(entry.VisualPivot, "bottom_right"))
                x = -bounds.max.x;

            if (IsBottomPivot(entry.VisualPivot))
                y = -bounds.min.y;

            return new Vector3(x, y, 0f);
        }

        private static bool IsBottomPivot(string pivot)
        {
            return IsPivot(pivot, "bottom_center")
                   || IsPivot(pivot, "bottom_left")
                   || IsPivot(pivot, "bottom_right");
        }

        private static bool IsPivot(string pivot, string expected)
        {
            return string.Equals(
                pivot ?? string.Empty,
                expected ?? string.Empty,
                System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParsePositiveInt(string value, out int result)
        {
            result = -1;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (!int.TryParse(value.Trim(), out int parsed) || parsed <= 0)
                return false;

            result = parsed;
            return true;
        }
    }
}
