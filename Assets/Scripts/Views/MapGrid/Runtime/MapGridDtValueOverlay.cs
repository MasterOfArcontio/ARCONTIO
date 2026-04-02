using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcontio.Core;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridDtValueOverlay (v0.03.02.a.4)
    ///
    /// Overlay Canvas ScreenSpaceOverlay che mostra il valore numerico della
    /// Distance Transform su ogni cella walkable.
    ///
    /// Scopo: debug del Passo 1 del HybridLandmarkExtractor.
    /// Permette di verificare che la DT sia calcolata correttamente nelle zone
    /// degli edifici, dove la heatmap colorata non ha abbastanza contrasto.
    ///
    /// Attivazione: tasto D in game (toggle indipendente da L e G).
    ///
    /// Nota prestazionale: su una mappa 64×64 = 4096 celle, con tileSizeWorld=1
    /// e zoom normale, la maggior parte delle celle è fuori schermo.
    /// Il rendering è limitato alle celle visibili tramite frustum check della camera.
    ///
    /// Pool: le label Text UI sono pooled per evitare GC.
    /// </summary>
    public sealed class MapGridDtValueOverlay
    {
        // ============================================================
        // COSTANTI
        // ============================================================

        // Dimensione della label per cella — piccola per non coprire i sprite
        private const int   LabelFontSize = 9;
        private const float CardWidth     = 20f;
        private const float CardHeight    = 14f;

        // Colore testo: bianco con outline nero per leggibilità su qualsiasi sfondo
        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.92f);

        // Mostra solo celle con DT > 0 (muri e celle non raggiunte sono 0)
        private const int MinDtToShow = 1;

        // ============================================================
        // STATO
        // ============================================================

        private Canvas        _canvas;
        private RectTransform _rootRt;
        private bool          _enabled;

        // Pool di label: riusate tra frame
        private readonly List<DtLabel> _pool    = new List<DtLabel>(256);
        private readonly List<DtLabel> _active  = new List<DtLabel>(256);

        // ============================================================
        // INIT / ENABLE
        // ============================================================

        public void Init(Transform parent)
        {
            var go = new GameObject("DtValueOverlay");
            go.transform.SetParent(parent, false);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 997; // sotto LandmarkLabelOverlay (998)

            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>().enabled = false;

            _rootRt = go.GetComponent<RectTransform>();
            if (_rootRt == null) _rootRt = go.AddComponent<RectTransform>();
            _rootRt.anchorMin  = Vector2.zero;
            _rootRt.anchorMax  = Vector2.one;
            _rootRt.pivot      = new Vector2(0.5f, 0.5f);
            _rootRt.sizeDelta  = Vector2.zero;

            SetEnabled(false);
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (_canvas != null)
                _canvas.gameObject.SetActive(enabled);
            if (!enabled) HideAll();
        }

        public bool IsEnabled => _enabled;

        // ============================================================
        // RENDER
        // ============================================================

        /// <summary>
        /// Aggiorna le label per il frame corrente.
        /// Chiamato da MapGridWorldView.Update() quando il DT overlay è attivo.
        ///
        /// snapshot: il GvdDinOverlaySnapshot già popolato da LandmarkOverlay.
        ///   Contiene DtCells con i valori DT per ogni cella walkable.
        ///   Riusato senza riallocazioni.
        ///
        /// cam: camera principale per WorldToScreenPoint.
        /// tileSizeWorld: dimensione di una cella in unità world (tipicamente 1.0).
        /// </summary>
        public void Render(
            GvdDinOverlaySnapshot snapshot,
            Camera cam,
            float tileSizeWorld)
        {
            if (!_enabled || cam == null || _rootRt == null) return;
            if (snapshot == null || !snapshot.IsValid)
            {
                HideAll();
                return;
            }

            // Disattiva tutte le label attive del frame precedente
            foreach (var lbl in _active) lbl.SetVisible(false);
            _active.Clear();

            int poolIdx = 0;

            foreach (var cell in snapshot.DtCells)
            {
                if (cell.DtValue < MinDtToShow) continue;

                // Converti posizione cella → screen → canvas local
                var wp = new Vector3(
                    (cell.CellX + 0.5f) * tileSizeWorld,
                    (cell.CellY + 0.5f) * tileSizeWorld,
                    0f);

                var sp = cam.WorldToScreenPoint(wp);
                if (sp.z < 0f) continue; // dietro la camera

                // Frustum check: scarta celle fuori schermo
                if (sp.x < -CardWidth || sp.x > Screen.width  + CardWidth ||
                    sp.y < -CardHeight || sp.y > Screen.height + CardHeight)
                    continue;

                // Ottieni o crea label dal pool
                DtLabel lbl;
                if (poolIdx < _pool.Count)
                {
                    lbl = _pool[poolIdx];
                }
                else
                {
                    lbl = CreateLabel();
                    _pool.Add(lbl);
                }
                poolIdx++;

                // Posiziona e aggiorna testo
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootRt, new Vector2(sp.x, sp.y), null, out var local))
                {
                    lbl.Rt.anchoredPosition = local;
                }

                lbl.Label.text = cell.DtValue.ToString();

                // Colora in base al valore DT per una lettura rapida:
                // verde = DT alta (centro stanza), rosso = DT bassa (vicino muro)
                float t = cell.DtNormalized01;
                lbl.Label.color = Color.Lerp(
                    new Color(1f, 0.4f, 0.4f, 0.9f),  // rosso: DT=1 (bordo muro)
                    new Color(0.4f, 1f, 0.5f, 0.9f),  // verde: DT alta (centro)
                    t);

                lbl.SetVisible(true);
                _active.Add(lbl);
            }
        }

        public void Clear() => HideAll();

        // ============================================================
        // INTERNALS
        // ============================================================

        private void HideAll()
        {
            foreach (var lbl in _active) lbl.SetVisible(false);
            _active.Clear();
        }

        private DtLabel CreateLabel()
        {
            var go = new GameObject("DT_Val");
            go.transform.SetParent(_rootRt, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            // Sfondo semitrasparente per leggibilità
            var bg = go.AddComponent<Image>();
            bg.color         = new Color(0f, 0f, 0f, 0.45f);
            bg.raycastTarget = false;

            var textGo = new GameObject("T");
            textGo.transform.SetParent(go.transform, false);

            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var label = textGo.AddComponent<Text>();
            label.raycastTarget      = false;
            label.font               = GetUiFont();
            label.fontSize           = LabelFontSize;
            label.fontStyle          = FontStyle.Bold;
            label.alignment          = TextAnchor.MiddleCenter;
            label.color              = TextColor;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow   = VerticalWrapMode.Overflow;

            go.SetActive(false);
            return new DtLabel(rt, label, go);
        }

        private static Font GetUiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // ============================================================
        // LABEL STRUCT
        // ============================================================

        private sealed class DtLabel
        {
            public readonly RectTransform Rt;
            public readonly Text          Label;
            private readonly GameObject   _go;

            public DtLabel(RectTransform rt, Text label, GameObject go)
            { Rt = rt; Label = label; _go = go; }

            public void SetVisible(bool v) { if (_go != null) _go.SetActive(v); }
        }
    }
}
