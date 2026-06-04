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

        // =============================================================================
        // RenderCurrentCone
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna il cono percettivo geometrico corrente di un singolo NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: debug visivo non autoritativo</b></para>
        /// <para>
        /// Questo metodo non produce percezione, non aggiorna memoria, non marca dirty
        /// e non modifica il mondo. Serve solo a visualizzare il cono che l'NPC avrebbe
        /// in questo momento in base a posizione, orientamento, stato percettivo,
        /// raggio, cono e linea di vista.
        /// </para>
        ///
        /// <para><b>Perché non usa più solo la heatmap storica:</b></para>
        /// <para>
        /// La heatmap è un buffer diagnostico a finestre di tick. Con cadenze
        /// percettive alte può risultare vuota, parziale o visivamente ambigua.
        /// Il pulsante FOV della barra runtime deve invece rispondere alla domanda
        /// immediata: "dove sta guardando ora questo NPC?".
        /// </para>
        /// </summary>
        public void RenderCurrentCone(World world, int npcId, bool useLos)
        {
            if (_root == null || _sprite == null || world == null || npcId <= 0)
            {
                Clear();
                return;
            }

            if (!world.GridPos.TryGetValue(npcId, out var origin))
            {
                Clear();
                return;
            }

            if (!world.NpcFacing.TryGetValue(npcId, out var facing))
                facing = CardinalDirection.North;

            int visionRange = world.GetNpcPerceptionRangeCells(npcId);
            if (visionRange <= 0)
            {
                Clear();
                return;
            }

            bool useCone = world.GetNpcPerceptionUseCone(npcId);
            float coneSlope = world.GetNpcPerceptionConeSlope(npcId);

            for (int i = 0; i < _lastActiveKeys.Count; i++)
            {
                int key = _lastActiveKeys[i];
                if (_cellRenderers.TryGetValue(key, out var sr) && sr != null)
                    sr.gameObject.SetActive(false);
            }
            _lastActiveKeys.Clear();

            int watchedMargin = world.Global.PerceptionDirtyRadiusMarginCells;
            if (watchedMargin < 0)
                watchedMargin = 0;

            int watchedRange = visionRange + watchedMargin;
            int minX = origin.X - watchedRange;
            int maxX = origin.X + watchedRange;
            int minY = origin.Y - watchedRange;
            int maxY = origin.Y + watchedRange;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!world.InBounds(x, y))
                        continue;

                    int distance = FovUtils.Manhattan(origin.X, origin.Y, x, y);
                    if (distance <= 0 || distance > watchedRange)
                        continue;

                    bool observed = IsObservedFovCell(
                        world,
                        origin.X,
                        origin.Y,
                        facing,
                        x,
                        y,
                        visionRange,
                        useCone,
                        coneSlope,
                        useLos);

                    if (!observed && !IsWatchedMarginCell(
                            world,
                            origin.X,
                            origin.Y,
                            facing,
                            x,
                            y,
                            visionRange,
                            watchedMargin,
                            useCone,
                            coneSlope,
                            useLos))
                    {
                        continue;
                    }

                    bool watchedOnly = !observed;

                    int key = (y * world.MapWidth) + x;
                    var sr = GetOrCreateCellRenderer(key);

                    sr.color = watchedOnly
                        ? new Color(0.45f, 0.92f, 1f, 0.16f)
                        : new Color(1f, 1f, 1f, 0.22f);

                    sr.transform.position = CellCenterWorld(x, y);
                    sr.sortingOrder = _sortingOrder;
                    sr.gameObject.SetActive(true);
                    _lastActiveKeys.Add(key);
                }
            }
        }

        // =============================================================================
        // IsObservedFovCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se una cella appartiene al cono visivo realmente osservato.
        /// Il controllo usa lo stesso percorso logico del debug FOV immediato:
        /// raggio percettivo, cono o fronte legacy e linea di vista opzionale.
        /// </para>
        ///
        /// <para><b>Principio architetturale: observed separato da watched</b></para>
        /// <para>
        /// Una cella osservata e' una cella che l'NPC vede davvero. Una cella
        /// watched e' invece solo una cella di margine diagnostico attorno al cono.
        /// Tenere separati i due concetti evita che il bordo conservativo venga
        /// scambiato per percezione reale.
        /// </para>
        /// </summary>
        private static bool IsObservedFovCell(
            World world,
            int originX,
            int originY,
            CardinalDirection facing,
            int x,
            int y,
            int visionRange,
            bool useCone,
            float coneSlope,
            bool useLos)
        {
            int distance = FovUtils.Manhattan(originX, originY, x, y);
            if (distance <= 0 || distance > visionRange)
                return false;

            if (useCone)
            {
                if (!FovUtils.IsInCone(originX, originY, facing, x, y, coneSlope))
                    return false;
            }
            else if (!FovUtils.IsInFront(originX, originY, facing, x, y))
            {
                return false;
            }

            return !useLos || world.HasLineOfSight(originX, originY, x, y);
        }

        // =============================================================================
        // IsWatchedMarginCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se una cella appartiene al bordo watched attorno al cono osservato.
        /// Il bordo viene calcolato come dilatazione locale delle celle osservate,
        /// non come semplice allungamento del raggio del cono.
        /// </para>
        ///
        /// <para><b>Principio architetturale: bordo conservativo simmetrico</b></para>
        /// <para>
        /// Questo metodo rende visibile il margine su tutti i lati del cono. La
        /// versione precedente estendeva solo il raggio massimo e quindi poteva
        /// mostrare il bordo su due lati, lasciando scoperti gli altri due.
        /// </para>
        /// </summary>
        private static bool IsWatchedMarginCell(
            World world,
            int originX,
            int originY,
            CardinalDirection facing,
            int x,
            int y,
            int visionRange,
            int watchedMargin,
            bool useCone,
            float coneSlope,
            bool useLos)
        {
            if (watchedMargin <= 0)
                return false;

            for (int dy = -watchedMargin; dy <= watchedMargin; dy++)
            {
                for (int dx = -watchedMargin; dx <= watchedMargin; dx++)
                {
                    if ((dx == 0 && dy == 0) || FovUtils.Manhattan(0, 0, dx, dy) > watchedMargin)
                        continue;

                    int observedX = x + dx;
                    int observedY = y + dy;
                    if (!world.InBounds(observedX, observedY))
                        continue;

                    if (IsObservedFovCell(
                            world,
                            originX,
                            originY,
                            facing,
                            observedX,
                            observedY,
                            visionRange,
                            useCone,
                            coneSlope,
                            useLos))
                    {
                        return true;
                    }
                }
            }

            return false;
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
