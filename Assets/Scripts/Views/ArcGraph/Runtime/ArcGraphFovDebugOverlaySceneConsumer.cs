using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphFovDebugOverlaySceneConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Consumer Unity pooled per visualizzare celle FOV debug dentro ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: renderer debug consumer-only</b></para>
    /// <para>
    /// Questo componente non calcola FOV, non sceglie l'NPC attivo e non legge il
    /// <c>World</c>. Consuma una <c>ArcGraphDebugOverlayQueue</c> gia' costruita e
    /// materializza soltanto le celle FOV tramite <c>SpriteRenderer</c> riusabili.
    /// In questo modo il metodo visuale di MapGrid viene assorbito senza copiare il
    /// monolite <c>MapGridWorldView</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RenderQueue</b>: applica la queue FOV al pool di celle.</item>
    ///   <item><b>ClearOverlay</b>: spegne tutte le celle attive.</item>
    ///   <item><b>GetOrCreateCellRenderer</b>: crea o riusa un renderer per cella.</item>
    ///   <item><b>ResolveColor</b>: traduce kind/colorKey in colore debug.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphFovDebugOverlaySceneConsumer : MonoBehaviour, IArcGraphDebugOverlayQueueConsumer
    {
        private const string RootName = "ArcGraphFovDebugOverlayRoot";
        private const int DefaultSortingOrder = 178;

        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private float cellScale = 0.96f;
        [SerializeField] private int sortingOrder = DefaultSortingOrder;
        [SerializeField] private bool logDiagnostics;

        private readonly Dictionary<int, SpriteRenderer> _cellRenderers = new Dictionary<int, SpriteRenderer>(256);
        private readonly List<int> _lastActiveKeys = new List<int>(256);

        private Transform _root;
        private Sprite _debugSprite;
        private Texture2D _debugTexture;

        // =============================================================================
        // SetTileWorldSize
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna la dimensione cella usata per posizionare l'overlay.
        /// </para>
        /// </summary>
        public void SetTileWorldSize(float value)
        {
            tileWorldSize = value > 0.0001f ? value : 1f;
        }

        // =============================================================================
        // RenderQueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna le sole celle FOV contenute nella queue debug ArcGraph.
        /// </para>
        /// </summary>
        public void RenderQueue(ArcGraphDebugOverlayQueue queue)
        {
            DeactivateLastFrameCells();

            if (queue == null)
            {
                Log("QueueMissing");
                return;
            }

            EnsureRoot();
            EnsureDebugSprite();

            int rendered = 0;
            IReadOnlyList<ArcGraphDebugCellOverlayItem> cells = queue.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                ArcGraphDebugCellOverlayItem item = cells[i];
                if (!item.IsVisible || !IsFovCell(item.Kind))
                    continue;

                int key = ResolveCellKey(item);
                SpriteRenderer renderer = GetOrCreateCellRenderer(key);
                renderer.color = ResolveColor(item);
                renderer.transform.position = CellCenterWorld(item.Cell);
                renderer.transform.localScale = Vector3.one * Mathf.Max(0.01f, tileWorldSize * cellScale);
                renderer.sortingOrder = sortingOrder;
                renderer.gameObject.SetActive(true);

                _lastActiveKeys.Add(key);
                rendered++;
            }

            Log("Rendered=" + rendered);
        }

        // =============================================================================
        // ClearOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Spegne tutte le celle FOV attive senza distruggere il pool.
        /// </para>
        /// </summary>
        public void ClearOverlay()
        {
            DeactivateLastFrameCells();
        }

        private void DeactivateLastFrameCells()
        {
            for (int i = 0; i < _lastActiveKeys.Count; i++)
            {
                int key = _lastActiveKeys[i];
                if (_cellRenderers.TryGetValue(key, out SpriteRenderer renderer) && renderer != null)
                    renderer.gameObject.SetActive(false);
            }

            _lastActiveKeys.Clear();
        }

        private SpriteRenderer GetOrCreateCellRenderer(int key)
        {
            if (_cellRenderers.TryGetValue(key, out SpriteRenderer existing) && existing != null)
                return existing;

            EnsureRoot();
            EnsureDebugSprite();

            var go = new GameObject("ArcGraphFovCell_" + key);
            go.transform.SetParent(_root, false);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _debugSprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.clear;
            renderer.gameObject.SetActive(false);

            _cellRenderers[key] = renderer;
            return renderer;
        }

        private void EnsureRoot()
        {
            if (_root != null)
                return;

            Transform existing = transform.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                return;
            }

            var go = new GameObject(RootName);
            go.transform.SetParent(transform, false);
            _root = go.transform;
        }

        private void EnsureDebugSprite()
        {
            if (_debugSprite != null)
                return;

            _debugTexture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            _debugTexture.name = "ArcGraphFovDebugOverlayPixel";
            _debugTexture.SetPixel(0, 0, Color.white);
            _debugTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            _debugSprite = Sprite.Create(
                _debugTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
            _debugSprite.name = "ArcGraphFovDebugOverlaySprite";
        }

        private Vector3 CellCenterWorld(ArcGraphCellCoord cell)
        {
            float worldX = (cell.X + 0.5f) * tileWorldSize;
            float worldY = (cell.Y + 0.5f) * tileWorldSize;
            return new Vector3(worldX, worldY, 0f);
        }

        private static bool IsFovCell(ArcGraphDebugOverlayKind kind)
        {
            return kind == ArcGraphDebugOverlayKind.FovObservedCell
                || kind == ArcGraphDebugOverlayKind.FovWatchedMarginCell
                || kind == ArcGraphDebugOverlayKind.FovHistoricalHeatCell;
        }

        private static int ResolveCellKey(ArcGraphDebugCellOverlayItem item)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (int)item.Kind;
                hash = (hash * 31) + item.Cell.X;
                hash = (hash * 31) + item.Cell.Y;
                hash = (hash * 31) + item.Cell.Z;
                return hash & int.MaxValue;
            }
        }

        private static Color ResolveColor(ArcGraphDebugCellOverlayItem item)
        {
            if (item.Kind == ArcGraphDebugOverlayKind.FovWatchedMarginCell)
                return new Color(0.45f, 0.92f, 1f, 0.16f);

            if (item.Kind == ArcGraphDebugOverlayKind.FovHistoricalHeatCell)
            {
                float alpha = Mathf.Clamp01(item.Intensity01) * 0.50f;
                return new Color(1f, 1f, 1f, alpha);
            }

            return new Color(1f, 1f, 1f, 0.22f);
        }

        private void Log(string reason)
        {
            if (!logDiagnostics)
                return;

            Debug.Log("[ArcGraphFovDebugOverlaySceneConsumer] " + reason);
        }

        private static void DestroyRuntimeObject(Object unityObject)
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
            if (_debugSprite != null)
            {
                DestroyRuntimeObject(_debugSprite);
                _debugSprite = null;
            }

            if (_debugTexture != null)
            {
                DestroyRuntimeObject(_debugTexture);
                _debugTexture = null;
            }
        }
    }
}
