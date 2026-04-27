using Arcontio.Core;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    // =============================================================================
    // MapGridRuntimeControlTopBar
    // =============================================================================
    /// <summary>
    /// <para>
    /// Barra comandi runtime sottile, ancorata al bordo superiore dello schermo nella
    /// scena MapGrid. La barra raccoglie i controlli operativi usati durante il debug:
    /// attivazione spawn/DevTools, pausa, step singolo e step multiplo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: controllo UI come adapter view-side</b></para>
    /// <para>
    /// La barra non possiede lo stato simulativo e non esegue sistemi. Inoltra comandi
    /// espliciti al <c>SimulationHost</c> per pausa/step e al
    /// <c>MapGridRuntimeDevToolsOverlay</c> per la modalita' spawn. In questo modo la
    /// UI resta un adapter sottile sopra layer gia' esistenti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Canvas overlay</b>: pannello screen-space indipendente dalla camera MapGrid.</item>
    ///   <item><b>Pulsanti</b>: quattro azioni piccole e sempre visibili.</item>
    ///   <item><b>RefreshLabels</b>: sincronizza testo e colore con pausa e DevTools.</item>
    /// </list>
    /// </summary>
    public sealed class MapGridRuntimeControlTopBar
    {
        private GameObject _root;
        private Text _spawnText;
        private Text _pauseText;
        private Text _stepOneText;
        private Text _stepTenText;
        private Image _spawnImage;
        private Image _pauseImage;
        private MapGridRuntimeDevToolsOverlay _devToolsOverlay;
        private float _nextRefreshTime;

        // =============================================================================
        // AttachTo
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea la barra sotto un parent di scena e collega il riferimento opzionale
        /// al pannello DevTools. Se il riferimento non e' disponibile subito, la barra
        /// ritenta tramite <c>FindObjectOfType</c> durante il refresh.
        /// </para>
        /// </summary>
        public void AttachTo(Transform parent, MapGridRuntimeDevToolsOverlay devToolsOverlay)
        {
            if (_root != null)
                return;

            EnsureEventSystemExists();
            _devToolsOverlay = devToolsOverlay;

            _root = new GameObject("MapGridRuntimeControlTopBar");
            _root.transform.SetParent(parent, false);

            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10003;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _root.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(_root.transform, false);

            var panelImage = panelGo.AddComponent<Image>();
            panelImage.raycastTarget = true;
            panelImage.color = ColorFromHex("#010409", 0.74f);

            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(0f, 32f);
            panelRt.offsetMin = new Vector2(244f, -32f);
            panelRt.offsetMax = Vector2.zero;

            var layout = panelGo.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 6;
            layout.padding = new RectOffset(10, 10, 4, 4);

            CreateButton(panelGo.transform, "Spawn F3", 112f, OnSpawnClicked, out _spawnText, out _spawnImage);
            CreateButton(panelGo.transform, "Pausa P", 96f, OnPauseClicked, out _pauseText, out _pauseImage);
            CreateButton(panelGo.transform, "Step 1 O", 96f, OnStepOneClicked, out _stepOneText, out _);
            CreateButton(panelGo.transform, "Step 10 I", 104f, OnStepTenClicked, out _stepTenText, out _);

            RefreshLabels(force: true);
        }

        // =============================================================================
        // Tick
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna periodicamente il testo dei pulsanti senza far dipendere il layout
        /// da polling costoso ogni frame. La barra riflette stato pausa e DevTools.
        /// </para>
        /// </summary>
        public void Tick()
        {
            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + 0.2f;
            RefreshLabels(force: false);
        }

        private void OnSpawnClicked()
        {
            ResolveDevToolsOverlay()?.ToggleSpawnToolOverlay();
            RefreshLabels(force: true);
        }

        private void OnPauseClicked()
        {
            SimulationHost.Instance?.TogglePause();
            RefreshLabels(force: true);
        }

        private void OnStepOneClicked()
        {
            SimulationHost.Instance?.StepOneTickPaused();
            RefreshLabels(force: true);
        }

        private void OnStepTenClicked()
        {
            SimulationHost.Instance?.StepManyTicksPaused(10);
            RefreshLabels(force: true);
        }

        private void RefreshLabels(bool force)
        {
            var host = SimulationHost.Instance;
            bool paused = host != null && host.IsPaused;
            bool spawnEnabled = ResolveDevToolsOverlay()?.IsDevModeEnabled == true;

            if (_spawnText != null)
                _spawnText.text = spawnEnabled ? "Spawn F3: ON" : "Spawn F3: OFF";

            if (_pauseText != null)
                _pauseText.text = paused ? "Riprendi P" : "Pausa P";

            if (_stepOneText != null)
                _stepOneText.text = "Step 1 O";

            if (_stepTenText != null)
                _stepTenText.text = "Step 10 I";

            if (_spawnImage != null)
                _spawnImage.color = spawnEnabled ? ColorFromHex("#238636", 0.82f) : ColorFromHex("#161B22", 0.88f);

            if (_pauseImage != null)
                _pauseImage.color = paused ? ColorFromHex("#D29922", 0.82f) : ColorFromHex("#161B22", 0.88f);
        }

        private MapGridRuntimeDevToolsOverlay ResolveDevToolsOverlay()
        {
            if (_devToolsOverlay != null)
                return _devToolsOverlay;

            _devToolsOverlay = Object.FindObjectOfType<MapGridRuntimeDevToolsOverlay>();
            return _devToolsOverlay;
        }

        private static void CreateButton(Transform parent, string label, float width, UnityEngine.Events.UnityAction onClick, out Text text, out Image image)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);

            image = go.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = ColorFromHex("#161B22", 0.88f);

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
            le.minHeight = 24f;
            le.preferredHeight = 24f;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            text = textGo.AddComponent<Text>();
            text.raycastTarget = false;
            text.font = GetUiFont();
            text.fontSize = 11;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = ColorFromHex("#E6EDF3", 1f);
            text.text = label;

            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(4f, 1f);
            textRt.offsetMax = new Vector2(-4f, -1f);
        }

        private static void EnsureEventSystemExists()
        {
            var existing = Object.FindObjectOfType<EventSystem>();
            if (existing == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                existing = eventSystemGo.AddComponent<EventSystem>();
            }

            var modules = existing.GetComponents<BaseInputModule>();
            if (modules != null && modules.Length > 0)
                return;

#if ENABLE_INPUT_SYSTEM
            existing.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            existing.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Color ColorFromHex(string hex, float alpha)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var color))
            {
                color.a = alpha;
                return color;
            }

            return new Color(1f, 1f, 1f, alpha);
        }

        private static Font GetUiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                return font;

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
