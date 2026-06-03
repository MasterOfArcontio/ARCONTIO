using Arcontio.Core;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridPointerCoordsOverlay (Patch 0.01P2):
    /// Indicatore costante in alto a sinistra che mostra le coordinate griglia
    /// sotto il puntatore del mouse.
    ///
    /// Perché non riusiamo il tooltip hover:
    /// - Il tooltip è contestuale e può essere disabilitato (es. SummaryOverlay ON).
    /// - L'utente vuole un indicatore *sempre presente*.
    ///
    /// Policy:
    /// - View-only, prefabless (come gli altri overlay).
    /// - Aggiornato dal MapGridWorldView.
    /// - Se mancano camera/input, mostra "Cell: -,-".
    /// </summary>
    public sealed class MapGridPointerCoordsOverlay
    {
        private readonly GameObject _root;
        private readonly Canvas _canvas;
        private readonly RectTransform _panelRt;
        private readonly Text _text;
        private readonly StringBuilder _textBuilder = new StringBuilder(256);

        private bool _visible;
        private bool _costMode;

        public MapGridPointerCoordsOverlay()
        {
            _root = new GameObject("MapGridPointerCoordsOverlay");

            _canvas = _root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10001; // sopra al tooltip overlay (10000)

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Panel (piccolo)
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(_root.transform, false);

            var img = panelGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);

            _panelRt = panelGo.GetComponent<RectTransform>();

            // Top-left anchoring
            _panelRt.anchorMin = new Vector2(0f, 1f);
            _panelRt.anchorMax = new Vector2(0f, 1f);
            _panelRt.pivot = new Vector2(0f, 1f);

            // Offset dal bordo schermo.
            // Il riquadro non resta piu' appiccicato al bordo alto perche' la barra
            // runtime contiene pulsanti F3/Pausa/Step: tenerlo sotto la toolbar
            // evita sovrapposizioni durante i test con osservatorio costi attivo.
            _panelRt.anchoredPosition = new Vector2(150f, -62f);
            _panelRt.sizeDelta = new Vector2(220f, 34f);

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panelGo.transform, false);

            _text = textGo.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 14;
            _text.alignment = TextAnchor.MiddleLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.supportRichText = true;
            _text.color = Color.white;
            _text.text = "Cell: -,-";

            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(10f, 4f);
            trt.offsetMax = new Vector2(-10f, -4f);

            SetVisible(true);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
        }

        public void SetVisible(bool v)
        {
            if (_visible == v) return;
            _visible = v;
            if (_root != null) _root.SetActive(v);
        }

        public void SetCell(int cellX, int cellY, bool inBounds)
        {
            SetCell(cellX, cellY, inBounds, null);
        }

        // =============================================================================
        // SetCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna il riquadro coordinate e, solo quando l'osservatorio costi runtime
        /// e' attivo, aggiunge una riga diagnostica sui costi percettivi principali.
        /// </para>
        ///
        /// <para><b>Principio architetturale: diagnostica congelabile</b></para>
        /// <para>
        /// Il percorso ordinario resta identico al comportamento storico: se
        /// <c>world.RuntimeCostObserver</c> e' nullo, il metodo non legge contatori,
        /// non costruisce riepiloghi aggiuntivi e mantiene il pannello compatto.
        /// </para>
        /// </summary>
        public void SetCell(int cellX, int cellY, bool inBounds, World world)
        {
            var costObserver = world?.RuntimeCostObserver;
            bool showCost = costObserver != null;
            SetCostMode(showCost);

            // UX: se fuori bounds (o tileSize/camera invalidi) lo segnaliamo.
            if (!inBounds)
            {
                _text.text = showCost
                    ? BuildCostText($"Cell: <color=#FF6666>{cellX},{cellY}</color>", world, costObserver)
                    : $"Cell: <color=#FF6666>{cellX},{cellY}</color>";
                return;
            }

            _text.text = showCost
                ? BuildCostText($"Cell: <b>{cellX},{cellY}</b>", world, costObserver)
                : $"Cell: <b>{cellX},{cellY}</b>";
        }

        public void SetUnknown()
        {
            SetCostMode(false);
            _text.text = "Cell: -,-";
        }

        private string BuildCostText(string cellText, World world, RuntimeCostObserver costObserver)
        {
            var stats = world.GetLastNpcPerceptionTickBudgetStats();
            long objectCells = costObserver.GetCounter(RuntimeCostCounter.ObjectPerceptionCandidateCells);
            long objectChecks = costObserver.GetCounter(RuntimeCostCounter.ObjectPerceptionObjectChecks);
            long npcCells = costObserver.GetCounter(RuntimeCostCounter.NpcPerceptionCandidateCells);
            long npcPairs = costObserver.GetCounter(RuntimeCostCounter.NpcPerceptionPairChecks);
            long debugFovCells = costObserver.GetCounter(RuntimeCostCounter.ObjectPerceptionDebugFovCells);
            long jobSteps = costObserver.GetCounter(RuntimeCostCounter.JobExecutionSteps);
            long routePreparations = costObserver.GetCounter(RuntimeCostCounter.MoveToRoutePreparations);
            long moveFailures = costObserver.GetCounter(RuntimeCostCounter.MoveToFailures);
            long recoveryEvaluated = costObserver.GetCounter(RuntimeCostCounter.JobRecoveryEvaluated);

            _textBuilder.Clear();
            _textBuilder.Append(cellText)
                .Append("  <color=#8DD6FF>Runtime cost</color>");
            _textBuilder.Append('\n');
            _textBuilder.Append("<color=#A7F3D0>Percezione</color> tick ").Append(stats.TickIndex)
                .Append(" | NPC ").Append(stats.SelectedCount).Append('/').Append(stats.TotalNpcCount)
                .Append(" | limite ").Append(stats.MaxPerceptionUpdates)
                .Append(" | attesa ").Append(stats.PendingCount)
                .Append(" | dirty ").Append(stats.DirtyNpcCount)
                .Append(" | cadenza ").Append(stats.SkippedByCadenceCount);
            AppendSelectedPerceptionState(world);
            _textBuilder.Append('\n');
            _textBuilder.Append("<color=#FFD166>Oggetti</color> celle ").Append(objectCells)
                .Append(" | controlli ").Append(objectChecks)
                .Append(" | fov ").Append(debugFovCells)
                .Append("    <color=#FFD166>NPC</color> celle ").Append(npcCells)
                .Append(" | coppie ").Append(npcPairs);
            _textBuilder.Append('\n');
            _textBuilder.Append("<color=#FF9F8A>Job</color> step ").Append(jobSteps)
                .Append(" | route ").Append(routePreparations)
                .Append(" | fail ").Append(moveFailures)
                .Append(" | recovery ").Append(recoveryEvaluated);
            return _textBuilder.ToString();
        }

        private void AppendSelectedPerceptionState(World world)
        {
            int npcId = SocialViewer.UI.NPCSelection.SelectedNpcId;
            if (npcId <= 0 || world == null || !world.ExistsNpc(npcId))
                return;

            var state = world.GetNpcPerceptionActivityState(npcId);
            int cadence = world.GetNpcPerceptionCadenceTicks(npcId);
            int range = world.GetNpcPerceptionRangeCells(npcId);
            bool dirty = world.IsNpcPerceptionDirty(npcId);

            _textBuilder.Append(" | <color=#C084FC>sel #").Append(npcId)
                .Append("</color> perc ").Append(state)
                .Append(" cad ").Append(cadence)
                .Append(" range ").Append(range)
                .Append(" dirty ").Append(dirty ? "si" : "no");
        }

        private void SetCostMode(bool enabled)
        {
            if (_costMode == enabled)
                return;

            _costMode = enabled;
            _panelRt.sizeDelta = enabled ? new Vector2(560f, 92f) : new Vector2(220f, 34f);
            _text.fontSize = enabled ? 12 : 14;
        }
    }
}
