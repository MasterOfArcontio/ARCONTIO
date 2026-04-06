// Assets/Scripts/Views/MapGrid/Runtime/MapGridRuntimeDevToolsOverlay.cs
using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridRuntimeDevToolsOverlay (DevMode v1):
    /// runtime devtools "map editor" per debug rapido dentro la simulazione.
    ///
    /// Questo file nasce come DevMode v0 (MVP) e viene esteso in v1 con:
    /// - brush continuo (paint mentre tieni premuto il mouse)
    /// - rectangle tool (drag di un rettangolo e fill)
    /// - spawn NPC
    /// - orientamento NPC (set facing N/E/S/W)
    /// - overlay info cella sotto il mouse (dentro finestra DevTools)
    ///
    /// Nota architetturale (ARCONTIO):
    /// - Questo componente è View-side.
    /// - NON modifica direttamente il World.
    /// - Enqueue di ICommand su SimulationHost (CommandBuffer view->core).
    /// </summary>
    public sealed class MapGridRuntimeDevToolsOverlay : MonoBehaviour
    {
        // ============================================================
        // TOOL MODEL (v1)
        // ============================================================

        /// <summary>
        /// Tool "macro" selezionato nella UI.
        /// - Place / Erase lavorano sugli oggetti.
        /// - SpawnNpc / OrientNpc lavorano sugli NPC.
        /// </summary>
        private enum Tool
        {
            Place = 0,
            Erase = 1,
            SpawnNpc = 2,
            OrientNpc = 3,
            EraseNpc = 4,
        }

        /// <summary>
        /// Shape applicabile a Place/Erase.
        /// - Click: singola cella (behaviour v0)
        /// - Brush: scrive continuamente mentre il pulsante è premuto
        /// - Rectangle: drag rettangolo e fill al rilascio
        /// </summary>
        private enum PaintShape
        {
            Click = 0,
            Brush = 1,
            Rectangle = 2,
        }

        [Header("References")]
        // NOTA IMPORTANTE (Patch 0.02.RDM-V0.1):
        // In ARCONTIO attuale, MapGridConfig è una classe serializzabile (non ScriptableObject).
        // Unity quindi la serializza "inline" nel componente e mostra un albero di campi.
        // Questo costringe l'utente a duplicare manualmente la config.
        //
        // Per eliminare questo problema, auto-bindiamo tutto da MapGridWorldView/Bootstrap.
        // Tenere comunque i SerializeField è utile come escape-hatch in scene non standard.
        [SerializeField] private MapGridConfig cfg;

        [Tooltip("Camera usata per ScreenToWorld. Se null: auto-bind o fallback Camera.main.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Provider puntatore (New Input System). Se null: auto-bind da MapGridWorldView/Bootstrap.")]
        [SerializeField] private MapGridPointerInputActionsProvider pointerProvider;

        [Header("DevMode")]
        [Tooltip("Tasto per toggle DevMode. In origine era F2 (documento), ma qui è rimappabile perché F2 può essere già occupato.")]
        [SerializeField] private Key toggleKey = Key.F2;

        [Tooltip("Nome default del file per Save/Load (senza estensione).")]
        [SerializeField] private string defaultMapName = "devmap";

        // ============================================================
        // RUNTIME STATE
        // ============================================================

        private bool _enabled;

        private Tool _tool = Tool.Place;
        private PaintShape _shape = PaintShape.Click;

        // Oggetti: selezione defId per placement.
        private string _selectedDefId;

        // Save/Load: name editabile.
        private string _mapName;

        // NPC: facing selezionato (usato sia per spawn che per orient).
        private CardinalDirection _npcFacing = CardinalDirection.North;

        // Brush: cache dell'ultima cella "dipinta" per evitare spam sullo stesso tile.
        private int _lastPaintX = int.MinValue;
        private int _lastPaintY = int.MinValue;

        // Rectangle: drag state (start + current).
        private bool _rectDragging;
        private int _rectStartX;
        private int _rectStartY;
        private int _rectEndX;
        private int _rectEndY;

        // Cache UI
        private Vector2 _scroll;
        private Rect _windowRect = new Rect(16, 16, 420, 560);

        // Per evitare "click-through" (il problema riportato: clicco un bottone e contemporaneamente piazzo sulla mappa),
        // teniamo una flag aggiornata ogni frame che indica se il mouse sta "sopra" la finestra IMGUI.
        private bool _isPointerOverUiWindow;

        private void Awake()
        {
            _mapName = defaultMapName;

            // ============================================================
            // Auto-bind (plug-and-play):
            // - Niente configurazione manuale in Inspector.
            // - Usiamo la config reale caricata dal JSON ufficiale.
            // ============================================================
            AutoBindIfNeeded();
        }

        private void AutoBindIfNeeded()
        {
            // 1) Prova prima a prendere tutto dalla View già esistente.
            //    Questa è la fonte più corretta perché è quella effettivamente in uso a runtime.
            var view = FindObjectOfType<MapGridWorldView>();
            if (view != null)
            {
                if (cfg == null) cfg = view.RuntimeConfig;
                if (pointerProvider == null) pointerProvider = view.RuntimePointerProvider;
                if (worldCamera == null) worldCamera = view.RuntimeWorldCamera;
            }

            // 2) Se manca la config, fallback robusto: carichiamo dalla Resources path standard.
            //    Questo è esattamente ciò che fa MapGridBootstrap.
            if (cfg == null)
            {
                // Nota: MapGridJsonLoader sta nello stesso namespace Arcontio.View.MapGrid.
                cfg = MapGridJsonLoader.LoadFromResources<MapGridConfig>("MapGrid/Config/MapGridConfig");
            }

            // 3) Provider: fallback su un provider qualunque in scena.
            if (pointerProvider == null)
                pointerProvider = FindObjectOfType<MapGridPointerInputActionsProvider>();

            // 4) Camera: fallback.
            if (worldCamera == null)
                worldCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        }

        private void Update()
        {
            // ============================================================
            // 0) Toggle DevMode
            // ============================================================
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                _enabled = !_enabled;

                // UX: quando entri in DevMode e non hai ancora selezione,
                // scegliamo la prima definizione disponibile.
                if (_enabled && string.IsNullOrWhiteSpace(_selectedDefId))
                    _selectedDefId = ResolveFirstPlaceableDefId();

                // Reset "paint cache" quando entri/esci: evita edge-case dove il primo brush non scrive.
                _lastPaintX = int.MinValue;
                _lastPaintY = int.MinValue;

                // Reset rect
                _rectDragging = false;
            }

            if (!_enabled)
                return;

            // ============================================================
            // 0.5) Click-through guard:
            // se il puntatore è sopra la finestra UI, NON processiamo l'editing world.
            // ============================================================
            UpdatePointerOverUiWindowFlag();
            if (_isPointerOverUiWindow)
                return;

            // ============================================================
            // 1) Input world editing (place/erase/spawn/orient)
            // ============================================================
            if (!TryGetPointerCell(out int cx, out int cy, out bool inBounds) || !inBounds)
                return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            // Right click: in DevMode resta un erase "always", come in v0.
            // Questo è utilissimo per debug rapido ("togli quella cosa") anche se stai usando tool diversi.
            if (mouse.rightButton.wasPressedThisFrame)
            {
                Enqueue(new DevEraseObjectCommand(cx, cy));
                return;
            }

            // Left click / hold:
            // il significato dipende dal tool selezionato.
            switch (_tool)
            {
                case Tool.Place:
                case Tool.Erase:
                    HandleObjectPainting(mouse, cx, cy);
                    break;

                case Tool.SpawnNpc:
                    if (mouse.leftButton.wasPressedThisFrame)
                        Enqueue(new DevSpawnNpcCommand(cx, cy, facing: _npcFacing));
                    break;

                case Tool.OrientNpc:
                    if (mouse.leftButton.wasPressedThisFrame)
                        Enqueue(new DevSetNpcFacingAtCellCommand(cx, cy, _npcFacing));
                    break;

                case Tool.EraseNpc:
                    if (mouse.leftButton.wasPressedThisFrame)
                        Enqueue(new DevEraseNpcAtCellCommand(cx, cy));
                    break;
            }
        }

        private void HandleObjectPainting(Mouse mouse, int cx, int cy)
        {
            // Tool base determina l'azione.
            bool isPlace = (_tool == Tool.Place);

            // ------------------------------------------------------------
            // Shape: CLICK (v0 behaviour)
            // ------------------------------------------------------------
            if (_shape == PaintShape.Click)
            {
                if (!mouse.leftButton.wasPressedThisFrame)
                    return;

                if (isPlace)
                {
                    if (!string.IsNullOrWhiteSpace(_selectedDefId))
                        Enqueue(new DevPlaceObjectCommand(_selectedDefId, cx, cy));
                }
                else
                {
                    Enqueue(new DevEraseObjectCommand(cx, cy));
                }

                return;
            }

            // ------------------------------------------------------------
            // Shape: BRUSH (continuous paint)
            // ------------------------------------------------------------
            if (_shape == PaintShape.Brush)
            {
                // Brush = mentre il bottone è premuto, applica quando cambi cella.
                if (!mouse.leftButton.isPressed)
                {
                    // Reset quando rilasci: così il prossimo click parte "pulito".
                    _lastPaintX = int.MinValue;
                    _lastPaintY = int.MinValue;
                    return;
                }

                if (cx == _lastPaintX && cy == _lastPaintY)
                    return;

                _lastPaintX = cx;
                _lastPaintY = cy;

                if (isPlace)
                {
                    if (!string.IsNullOrWhiteSpace(_selectedDefId))
                        Enqueue(new DevPlaceObjectCommand(_selectedDefId, cx, cy));
                }
                else
                {
                    Enqueue(new DevEraseObjectCommand(cx, cy));
                }

                return;
            }

            // ------------------------------------------------------------
            // Shape: RECTANGLE
            // ------------------------------------------------------------
            if (_shape == PaintShape.Rectangle)
            {
                // Start drag
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    _rectDragging = true;
                    _rectStartX = cx;
                    _rectStartY = cy;
                    _rectEndX = cx;
                    _rectEndY = cy;
                    return;
                }

                // Update drag
                if (_rectDragging && mouse.leftButton.isPressed)
                {
                    _rectEndX = cx;
                    _rectEndY = cy;
                    return;
                }

                // Release -> fill rect
                if (_rectDragging && mouse.leftButton.wasReleasedThisFrame)
                {
                    _rectDragging = false;
                    FillRectangle(isPlace, _rectStartX, _rectStartY, _rectEndX, _rectEndY);
                }

                return;
            }
        }

        private void FillRectangle(bool isPlace, int ax, int ay, int bx, int by)
        {
            // Rettangolo inclusivo.
            int minX = Mathf.Min(ax, bx);
            int maxX = Mathf.Max(ax, bx);
            int minY = Mathf.Min(ay, by);
            int maxY = Mathf.Max(ay, by);

            if (isPlace && string.IsNullOrWhiteSpace(_selectedDefId))
                return;

            // Nota:
            // - per semplicità (MVP), enqueuiamo 1 comando per cella.
            // - Se in futuro si volesse ridurre overhead, si può introdurre un DevPlaceRectCommand core-side.
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (isPlace)
                        Enqueue(new DevPlaceObjectCommand(_selectedDefId, x, y));
                    else
                        Enqueue(new DevEraseObjectCommand(x, y));
                }
            }
        }

        private void OnGUI()
        {
            if (!_enabled) return;

            // Finestra IMGUI minimalista.
            _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "ARCONTIO DevMode v1");
        }

        private void DrawWindow(int id)
        {
            var world = MapGridWorldProvider.TryGetWorld();
            if (world == null)
            {
                GUILayout.Label("World: <null>");
                GUI.DragWindow(new Rect(0, 0, 10000, 20));
                return;
            }

            GUILayout.Label($"World: {world.MapWidth}x{world.MapHeight}");

            GUILayout.Space(8);

            // ------------------------------------------------------------
            // TOOL selection
            // ------------------------------------------------------------
            GUILayout.Label("Tool");
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_tool == Tool.Place, "Place", "Button")) _tool = Tool.Place;
            if (GUILayout.Toggle(_tool == Tool.Erase, "Erase", "Button")) _tool = Tool.Erase;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_tool == Tool.SpawnNpc, "Spawn NPC", "Button")) _tool = Tool.SpawnNpc;
            if (GUILayout.Toggle(_tool == Tool.OrientNpc, "Orient NPC", "Button")) _tool = Tool.OrientNpc;
            if (GUILayout.Toggle(_tool == Tool.EraseNpc, "Erase NPC", "Button")) _tool = Tool.EraseNpc;
            GUILayout.EndHorizontal();

            // ------------------------------------------------------------
            // Shape selection (only relevant for Place/Erase)
            // ------------------------------------------------------------
            if (_tool == Tool.Place || _tool == Tool.Erase)
            {
                GUILayout.Space(6);
                GUILayout.Label("Paint Shape");
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_shape == PaintShape.Click, "Click", "Button")) _shape = PaintShape.Click;
                if (GUILayout.Toggle(_shape == PaintShape.Brush, "Brush", "Button")) _shape = PaintShape.Brush;
                if (GUILayout.Toggle(_shape == PaintShape.Rectangle, "Rect", "Button")) _shape = PaintShape.Rectangle;
                GUILayout.EndHorizontal();

                if (_shape == PaintShape.Rectangle && _rectDragging)
                {
                    GUILayout.Label($"Rect: start({_rectStartX},{_rectStartY}) → now({_rectEndX},{_rectEndY})");
                }
            }

            // ------------------------------------------------------------
            // NPC facing selection (relevant for SpawnNpc + OrientNpc)
            // ------------------------------------------------------------
            if (_tool == Tool.SpawnNpc || _tool == Tool.OrientNpc)
            {
                GUILayout.Space(6);
                GUILayout.Label("NPC Facing");
                GUILayout.BeginHorizontal();
                GUILayout.Label(_npcFacing.ToString(), GUILayout.Width(90));
                if (GUILayout.Button("Rotate ↻", GUILayout.Width(90)))
                    _npcFacing = NextFacing(_npcFacing);
                GUILayout.EndHorizontal();
            }

            if (_tool == Tool.EraseNpc)
            {
                GUILayout.Space(6);
                GUILayout.Label("Erase NPC: click on a cell containing an NPC");
            }

            GUILayout.Space(8);

            // ------------------------------------------------------------
            // Save / Load (JSON)
            // ------------------------------------------------------------
            GUILayout.Label("Save / Load (JSON)");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.Width(42));
            _mapName = GUILayout.TextField(_mapName ?? "", GUILayout.Width(170));
            if (GUILayout.Button("Save", GUILayout.Width(70)))
                Enqueue(new DevSaveMapCommand(_mapName));
            if (GUILayout.Button("Load", GUILayout.Width(70)))
                Enqueue(new DevLoadMapCommand(_mapName, clearObjects: true));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // ------------------------------------------------------------
            // Palette (ObjectDef) - solo se stai piazzando oggetti
            // ------------------------------------------------------------
            if (_tool == Tool.Place)
            {
                GUILayout.Label("Palette (ObjectDef)");
                GUILayout.Label($"Selected: {_selectedDefId}");

                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(260));

                foreach (var kv in world.ObjectDefs)
                {
                    var def = kv.Value;
                    if (def == null) continue;

                    // Evitiamo definizioni interne/non piazzabili.
                    if (string.IsNullOrWhiteSpace(def.Id)) continue;
                    if (def.Id == "_runtime_occluder") continue;

                    bool isSel = (_selectedDefId == def.Id);

                    // Label breve: Id + DisplayName (se presente)
                    string label = def.Id;
                    if (!string.IsNullOrWhiteSpace(def.DisplayName) && def.DisplayName != def.Id)
                        label += $"  —  {def.DisplayName}";

                    if (GUILayout.Toggle(isSel, label, "Button"))
                        _selectedDefId = def.Id;
                }

                GUILayout.EndScrollView();
            }

            GUILayout.Space(8);

            // ------------------------------------------------------------
            // Overlay info cella sotto il mouse (DevMode v1 requirement)
            // ------------------------------------------------------------
            DrawCellInfo(world);

            GUILayout.Space(10);
            GUILayout.Label($"Controls: {toggleKey} toggle | RMB erase always");

            // Drag handle (top bar only)
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawCellInfo(World world)
        {
            // Nota UX:
            // - Lo mostriamo dentro la finestra, così non dobbiamo gestire label flottanti in world-space.
            // - Il requisito è "mostrare info cella sotto il mouse", non necessariamente fuori dalla finestra.
            if (!TryGetPointerCell(out int cx, out int cy, out bool inBounds) || !inBounds)
            {
                GUILayout.Label("Cell: (out of bounds)");
                return;
            }

            int objId = world.GetObjectAt(cx, cy);
            bool blocksMove = world.BlocksMovementAt(cx, cy);
            bool blocksVision = world.BlocksVisionAt(cx, cy);

            int npcId = 0;
            bool hasNpc = world.TryGetNpcAt(cx, cy, out npcId);

            string objLabel = (objId >= 0 && world.Objects.TryGetValue(objId, out var inst) && inst != null)
                ? $"{objId} ({inst.DefId})"
                : (objId >= 0 ? $"{objId}" : "<none>");

            GUILayout.Label($"Cell: ({cx},{cy})");
            GUILayout.Label($"Object: {objLabel}");
            GUILayout.Label($"Walkable: {!blocksMove}   BlocksVision: {blocksVision}");

            if (hasNpc)
            {
                var facing = world.GetFacing(npcId);
                string npcName = world.NpcDna.TryGetValue(npcId, out var dna) ? dna.Identity.Name : "<unnamed>";
                GUILayout.Label($"NPC: {npcId}  '{npcName}'  Facing: {facing}");
            }
            else
            {
                GUILayout.Label("NPC: <none>");
            }

            // Landmark presence (best-effort, O(N) on registry; ok for debug)
            int lmCount = 0;
            if (world.LandmarkRegistry != null)
            {
                var nodes = world.LandmarkRegistry.Nodes;
                for (int i = 0; i < nodes.Count; i++)
                {
                    var n = nodes[i];
                    if (n == null) continue;
                    if (!n.IsActive) continue;
                    if (n.CellX == cx && n.CellY == cy) lmCount++;
                }
            }
            GUILayout.Label($"Landmarks here: {lmCount}");
        }

        private static CardinalDirection NextFacing(CardinalDirection dir)
        {
            // Ordine deterministico: N -> E -> S -> W -> N
            switch (dir)
            {
                case CardinalDirection.North: return CardinalDirection.East;
                case CardinalDirection.East: return CardinalDirection.South;
                case CardinalDirection.South: return CardinalDirection.West;
                default: return CardinalDirection.North;
            }
        }

        private void Enqueue(ICommand cmd)
        {
            // Nota:
            // - Se non esiste SimulationHost, siamo in una scena non runtime.
            // - In quel caso non facciamo nulla.
            if (SimulationHost.Instance == null) return;
            SimulationHost.Instance.EnqueueExternalCommand(cmd);
        }

        private Camera ResolveCamera()
        {
            if (worldCamera != null) return worldCamera;
            if (Camera.main != null) return Camera.main;
            return FindObjectOfType<Camera>();
        }

        private void UpdatePointerOverUiWindowFlag()
        {
            _isPointerOverUiWindow = false;

            var mouse = Mouse.current;
            if (mouse == null) return;

            // Screen-space mouse (origin bottom-left)
            var sp = mouse.position.ReadValue();

            // Convert to IMGUI coords (origin top-left)
            var guiPos = new Vector2(sp.x, Screen.height - sp.y);

            _isPointerOverUiWindow = _windowRect.Contains(guiPos);
        }

        private bool TryGetPointerCell(out int cellX, out int cellY, out bool inBounds)
        {
            cellX = 0;
            cellY = 0;
            inBounds = false;

            var world = MapGridWorldProvider.TryGetWorld();
            if (world == null) return false;

            if (cfg == null || cfg.tileSizeWorld <= 0f) return false;

            var cam = ResolveCamera();
            if (cam == null) return false;

            Vector2 screenPos;

            // Preferiamo il provider (coerenza con overlay coords/tooltip), ma fallback su Mouse.
            if (pointerProvider != null && pointerProvider.TryGetPointerScreenPosition(out var p))
                screenPos = p;
            else if (Mouse.current != null)
                screenPos = Mouse.current.position.ReadValue();
            else
                return false;

            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
            wp.z = 0f;

            cellX = Mathf.FloorToInt(wp.x / cfg.tileSizeWorld);
            cellY = Mathf.FloorToInt(wp.y / cfg.tileSizeWorld);

            inBounds = world.InBounds(cellX, cellY);
            return true;
        }

        private string ResolveFirstPlaceableDefId()
        {
            var world = MapGridWorldProvider.TryGetWorld();
            if (world == null) return null;

            foreach (var kv in world.ObjectDefs)
            {
                var def = kv.Value;
                if (def == null) continue;
                if (string.IsNullOrWhiteSpace(def.Id)) continue;
                if (def.Id == "_runtime_occluder") continue;
                return def.Id;
            }

            return null;
        }
    }
}
