using System.Collections.Generic;
using UnityEngine;
using Arcontio.Core;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridFovHeatmapOverlay:
    ///
    /// Responsabilità:
    /// - Renderizzare (sulla grid view) un overlay di celle "viste" dall'NPC.
    /// - La sorgente dati è World.DebugFovTelemetry (READ buffer).
    /// - Ogni cella ha un contatore: più alta è la frequenza di "vista" nella finestra,
    ///   più alta è la luminosità (alpha).
    ///
    /// Nota:
    /// - Questo è tooling di debug.
    /// - Evitiamo di pre-creare N*N sprite renderer (costoso in GameObject).
    /// - Usiamo una pool: creiamo sprite renderer solo per celle con heat > 0.
    /// </summary>
    public sealed class MapGridFovHeatmapOverlay
    {
        private readonly Dictionary<int, SpriteRenderer> _cellRenderers = new(4096);
        private readonly Stack<SpriteRenderer> _pool = new(4096);
        private readonly List<int> _activeCellKeys = new(4096);

        private Transform _root;
        private Sprite _sprite;

        private float _tileSizeWorld;
        private int _sortingOrder;

        // Cache per evitare alloc: lista delle celle attive nel frame precedente.
        // Serve per disattivare velocemente tutto ciò che non è più attivo.
        private readonly List<int> _lastActiveKeys = new(4096);

        public void Init(Transform parent, float tileSizeWorld, string spriteResourcePath, int sortingOrder)
        {
            _tileSizeWorld = tileSizeWorld <= 0f ? 1f : tileSizeWorld;
            _sortingOrder = sortingOrder;

            _root = new GameObject("FovHeatmapOverlay").transform;
            _root.SetParent(parent, false);

            _sprite = Resources.Load<Sprite>(spriteResourcePath);
            if (_sprite == null)
            {
                Debug.LogWarning($"[MapGridFovHeatmapOverlay] Missing sprite at Resources/{spriteResourcePath}.png. Overlay will be invisible.");
            }
        }

        public void Clear()
        {
            // Disattiva tutto ciò che era attivo.
            for (int i = 0; i < _lastActiveKeys.Count; i++)
            {
                int key = _lastActiveKeys[i];
                if (_cellRenderers.TryGetValue(key, out var sr) && sr != null)
                    sr.gameObject.SetActive(false);
            }
            _lastActiveKeys.Clear();
        }

        /// <summary>
        /// Renderizza la heatmap di un NPC.
        ///
        /// Parametri:
        /// - heat: array 1D len = width*height, indicizzato con idx=y*width+x
        /// - width/height: dimensioni griglia
        /// - windowTicks: usato per normalizzare alpha (count/windowTicks)
        /// </summary>
        public void Render(int[] heat, int width, int height, int windowTicks)
        {
            if (_root == null) return;

            // Se non ho sprite, non renderizzo (ma mantengo il sistema attivo per debug).
            if (_sprite == null)
            {
                Clear();
                return;
            }

            if (heat == null || heat.Length == 0 || width <= 0 || height <= 0)
            {
                Clear();
                return;
            }

            if (windowTicks <= 0) windowTicks = 1;

            // 1) Disattiva tutto l'overlay del frame precedente.
            //    (poi riattiviamo solo ciò che serve)
            for (int i = 0; i < _lastActiveKeys.Count; i++)
            {
                int key = _lastActiveKeys[i];
                if (_cellRenderers.TryGetValue(key, out var sr) && sr != null)
                    sr.gameObject.SetActive(false);
            }
            _lastActiveKeys.Clear();

            // 2) Scansiona heatmap e attiva solo celle con heat > 0.
            //    Nota: è O(width*height). Con 64x64 è ok per debug.
            int size = width * height;
            if (heat.Length < size) size = heat.Length;

            for (int idx = 0; idx < size; idx++)
            {
                int count = heat[idx];
                if (count <= 0) continue;

                int x = idx % width;
                int y = idx / width;

                var sr = GetOrCreateCellRenderer(idx);

                // Alpha incrementale: N volte vista => N volte più chiara.
                // Normalizziamo rispetto a windowTicks (max teorico se una cella è sempre nel cono).
                float a = count / (float)windowTicks;
                if (a < 0f) a = 0f;
                if (a > 1f) a = 1f;

                var c = sr.color;
                
                // Metto un alpha preso dal valore di a, ma sottratto di una percentuale per evitare di avere delle celle completamente bianche opache
                c.a = a-(a/2);
                sr.color = c;

                sr.transform.position = CellCenterWorld(x, y);
                sr.sortingOrder = _sortingOrder;

                sr.gameObject.SetActive(true);
                _lastActiveKeys.Add(idx);
            }
        }

        private SpriteRenderer GetOrCreateCellRenderer(int cellKey)
        {
            if (_cellRenderers.TryGetValue(cellKey, out var existing) && existing != null)
            {
                return existing;
            }

            SpriteRenderer sr;
            if (_pool.Count > 0)
            {
                sr = _pool.Pop();
                // Nota: sr potrebbe essere stato distrutto se scene reload; fail-safe.
                if (sr == null)
                    return CreateNew(cellKey);
            }
            else
            {
                return CreateNew(cellKey);
            }

            _cellRenderers[cellKey] = sr;
            return sr;
        }

        private SpriteRenderer CreateNew(int cellKey)
        {
            var go = new GameObject($"FovCell_{cellKey}");
            go.transform.SetParent(_root, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.sortingOrder = _sortingOrder;

            // Default color: bianco con alpha 0 (verrà impostato in Render)
            sr.color = new Color(1f, 1f, 1f, 0f);

            _cellRenderers[cellKey] = sr;
            return sr;
        }

        private Vector3 CellCenterWorld(int cellX, int cellY)
        {
            float wx = (cellX + 0.5f) * _tileSizeWorld;
            float wy = (cellY + 0.5f) * _tileSizeWorld;
            return new Vector3(wx, wy, 0f);
        }
    }
}
