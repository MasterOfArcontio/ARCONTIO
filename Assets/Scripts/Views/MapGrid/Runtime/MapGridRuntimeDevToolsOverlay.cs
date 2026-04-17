// Assets/Scripts/Views/MapGrid/Runtime/MapGridRuntimeDevToolsOverlay.cs
using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using SocialViewer.UI;
using UnityEngine;
using UnityEngine.EventSystems;
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
            PlaceDoor = 5,
            PlaceFoodStock = 6,
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

        // =============================================================================
        // DoorPlacementState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Stato iniziale richiesto dalla UI per una porta piazzata tramite DevTools.
        /// La scelta resta view-side fino alla creazione del comando; la validazione
        /// effettiva del lock avviene nel core, dove e' disponibile la <c>ObjectDef</c>.
        /// </para>
        ///
        /// <para><b>UI dichiarativa / Core validante</b></para>
        /// <para>
        /// L'overlay esprime l'intenzione dell'utente, ma non forza direttamente
        /// <c>WorldObjectInstance</c>. Il comando traduce questo stato in flag runtime
        /// e usa <c>World.SetDoorOpen</c> per tenere coerenti le cache.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Open</b>: porta inizialmente aperta.</item>
        ///   <item><b>Closed</b>: porta chiusa ma non bloccata.</item>
        ///   <item><b>Locked</b>: porta chiusa a chiave, valido solo se la porta e' lockable.</item>
        /// </list>
        /// </summary>
        private enum DoorPlacementState
        {
            Open = 0,
            Closed = 1,
            Locked = 2,
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
        [SerializeField] private Key toggleKey = Key.F3;

        [Tooltip("Nome default del file per Save/Load (senza estensione).")]
        [SerializeField] private string defaultMapName = "devmap";

        // ============================================================
        // RUNTIME STATE
        // ============================================================

        // Guard contro istanze duplicate: può accadere se il componente è già nell'Inspector
        // della scena E MapGridWorldView ne aggiunge un secondo via AddComponent.
        private static MapGridRuntimeDevToolsOverlay _instance;

        private bool _enabled;

        private Tool _tool = Tool.Place;
        private PaintShape _shape = PaintShape.Click;

        // Oggetti: selezione defId per placement.
        private string _selectedDefId;

        // Save/Load: name editabile.
        private string _mapName;

        // NPC: facing selezionato (usato sia per spawn che per orient).
        private CardinalDirection _npcFacing = CardinalDirection.North;

        // Porte: stato richiesto dal pannello dedicato "Inserisci porta".
        private bool _doorWithLock;
        private DoorPlacementState _doorPlacementState = DoorPlacementState.Closed;

        // Cibo a terra: stock piazzato sulla mappa con proprieta' comunitaria o NPC.
        private int _foodStockUnits = 1;
        private bool _foodStockOwnedByNpc;
        private int _foodStockOwnerNpcId = -1;

        // Cibo addosso: incremento del cibo privato trasportato dall'NPC selezionato.
        private int _carriedFoodUnits = 1;

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
        private Rect _windowRect = new Rect(16, 16, 460, 720);

        // Per evitare "click-through" (il problema riportato: clicco un bottone e contemporaneamente piazzo sulla mappa),
        // teniamo una flag aggiornata ogni frame che indica se il mouse sta "sopra" la finestra IMGUI.
        private bool _isPointerOverUiWindow;

        public bool IsDevModeEnabled => _enabled;

        // =============================================================================
        // ToggleSpawnToolOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Attiva o disattiva il pannello DevTools direttamente dalla barra comandi
        /// MapGrid. Quando apre il pannello seleziona lo strumento di spawn NPC; quando
        /// il pannello e' gia' visibile lo chiude senza cambiare tool.
        /// </para>
        ///
        /// <para><b>Principio architetturale: UI come richiesta, non mutazione diretta</b></para>
        /// <para>
        /// La top bar non crea NPC e non modifica il World. Si limita a cambiare lo
        /// stato dello strumento view-side; il click successivo sulla mappa produrra'
        /// ancora un comando esplicito, accodato al <c>SimulationHost</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>_enabled</b>: visibilita' e operativita' del pannello DevTools.</item>
        ///   <item><b>_tool</b>: forzato a <c>SpawnNpc</c> quando la barra apre il pannello.</item>
        ///   <item><b>ResetTransientInputState</b>: pulisce cache brush/rectangle tra un toggle e l'altro.</item>
        /// </list>
        /// </summary>
        public void ToggleSpawnToolOverlay()
        {
            if (_enabled)
            {
                _enabled = false;
                ResetTransientInputState();
                return;
            }

            OpenSpawnToolOverlay();
        }

        // =============================================================================
        // OpenSpawnToolOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Apre esplicitamente i DevTools in modalita' spawn NPC. Questo percorso viene
        /// usato dal tasto F3, che storicamente e' un accesso rapido allo spawn e non
        /// solo un toggle cieco della finestra.
        /// </para>
        ///
        /// <para><b>Principio architetturale: intenzioni UI distinte</b></para>
        /// <para>
        /// Il pulsante top bar e il tasto rapido possono esprimere intenzioni diverse:
        /// il pulsante chiude se vede gia' il menu, mentre F3 garantisce l'ingresso in
        /// modalita' spawn. Entrambe restano view-side e non mutano direttamente il World.
        /// </para>
        /// </summary>
        public void OpenSpawnToolOverlay()
        {
            _enabled = true;
            _tool = Tool.SpawnNpc;

            if (_enabled && string.IsNullOrWhiteSpace(_selectedDefId))
                _selectedDefId = ResolveFirstPlaceableDefId();

            ResetTransientInputState();
        }

        private void Awake()
        {
            // Se esiste già un'altra istanza attiva, questa è un duplicato: si autodistrugge.
            // Il duplicato nasce quando il componente è già nell'Inspector della scena
            // E MapGridWorldView ne aggiunge un secondo tramite AddComponent.
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            _mapName = defaultMapName;

            // ============================================================
            // Auto-bind (plug-and-play):
            // - Niente configurazione manuale in Inspector.
            // - Usiamo la config reale caricata dal JSON ufficiale.
            // ============================================================
            AutoBindIfNeeded();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
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
                // F3 ora è un vero toggle: apre se chiuso, chiude se aperto.
                // Prima chiamava OpenSpawnToolOverlay() che impostava sempre _enabled=true,
                // impedendo la chiusura tramite tastiera.
                ToggleSpawnToolOverlay();
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

                case Tool.PlaceDoor:
                    if (mouse.leftButton.wasPressedThisFrame)
                        Enqueue(BuildDoorPlacementCommand(cx, cy));
                    break;

                case Tool.PlaceFoodStock:
                    if (mouse.leftButton.wasPressedThisFrame)
                        Enqueue(BuildFoodStockPlacementCommand(MapGridWorldProvider.TryGetWorld(), cx, cy));
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

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_tool == Tool.PlaceDoor, "Inserisci porta", "Button")) _tool = Tool.PlaceDoor;
            if (GUILayout.Toggle(_tool == Tool.PlaceFoodStock, "Inserisci cibo", "Button")) _tool = Tool.PlaceFoodStock;
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

            DrawNpcCarriedFoodControls(world);
            DrawDoorPlacementControls();
            DrawFoodStockPlacementControls(world);

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
                    if (IsDedicatedDevToolObject(def)) continue;

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

        // =============================================================================
        // DrawNpcCarriedFoodControls
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna il blocco UI che permette di aggiungere cibo direttamente addosso
        /// all'NPC selezionato. Il blocco legge la selezione view-only, ma non modifica
        /// direttamente il mondo: al click produce un <c>DevAddNpcPrivateFoodCommand</c>.
        /// </para>
        ///
        /// <para><b>Separazione tra selezione View e mutazione Core</b></para>
        /// <para>
        /// <c>NPCSelection</c> e' uno stato condiviso della View, utile per sapere quale
        /// NPC l'utente sta osservando. Il fatto simulativo "questo NPC trasporta cibo"
        /// resta pero' nel <c>World</c> e viene scritto solo dal comando core-side.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>SelectedNpcId</b>: identifica il destinatario del cibo addosso.</item>
        ///   <item><b>Quantita'</b>: campo numerico clampato a valori positivi.</item>
        ///   <item><b>Bottone comando</b>: accoda il comando solo se l'NPC esiste ancora.</item>
        /// </list>
        /// </summary>
        private void DrawNpcCarriedFoodControls(World world)
        {
            GUILayout.Space(8);
            GUILayout.Label("Cibo addosso a NPC selezionato");

            int selectedNpcId = NPCSelection.SelectedNpcId;
            bool hasSelectedNpc = selectedNpcId > 0 && world.ExistsNpc(selectedNpcId);

            if (hasSelectedNpc)
            {
                string npcName = world.NpcDna.TryGetValue(selectedNpcId, out var dna) && dna != null ? dna.Identity.Name : "<unnamed>";
                world.NpcPrivateFood.TryGetValue(selectedNpcId, out int currentFood);
                int freeCapacity = world.GetInventoryFreeCapacity(selectedNpcId);

                GUILayout.Label($"NPC: {selectedNpcId} '{npcName}'");
                GUILayout.Label($"Cibo trasportato: {currentFood} | Spazio libero: {freeCapacity}");
            }
            else
            {
                GUILayout.Label("NPC: nessun NPC selezionato");
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Unita'", GUILayout.Width(54));
            _carriedFoodUnits = DrawPositiveIntField(_carriedFoodUnits, GUILayout.Width(70));

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && hasSelectedNpc;
            if (GUILayout.Button("Aggiungi addosso", GUILayout.Width(140)))
                Enqueue(new DevAddNpcPrivateFoodCommand(selectedNpcId, _carriedFoodUnits));
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
        }

        // =============================================================================
        // DrawDoorPlacementControls
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna il blocco UI dedicato all'inserimento porte. L'utente non sceglie
        /// piu' tra piu' voci grezze di palette: sceglie una porta e ne configura lo
        /// stato tramite opzioni esplicite.
        /// </para>
        ///
        /// <para><b>Astrazione UI sopra ObjectDef tecnici</b></para>
        /// <para>
        /// La distinzione tra <c>door_wood_good</c> e <c>door_wood_locked</c> resta un
        /// dettaglio dati. La UI espone invece la domanda sistemica: "la porta supporta
        /// serratura?" e "qual e' il suo stato iniziale?".
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Tool dedicato</b>: il click mappa piazza una porta solo quando e' selezionato <c>PlaceDoor</c>.</item>
        ///   <item><b>Serratura</b>: abilita la scelta "chiusa a chiave".</item>
        ///   <item><b>Stato</b>: aperta, chiusa o locked, con locked disabilitato su porte non lockable.</item>
        /// </list>
        /// </summary>
        private void DrawDoorPlacementControls()
        {
            GUILayout.Space(8);
            GUILayout.Label("Porta");

            _doorWithLock = GUILayout.Toggle(_doorWithLock, "Con serratura a chiave");
            if (!_doorWithLock && _doorPlacementState == DoorPlacementState.Locked)
                _doorPlacementState = DoorPlacementState.Closed;

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_doorPlacementState == DoorPlacementState.Open, "Aperta", "Button"))
                _doorPlacementState = DoorPlacementState.Open;
            if (GUILayout.Toggle(_doorPlacementState == DoorPlacementState.Closed, "Chiusa", "Button"))
                _doorPlacementState = DoorPlacementState.Closed;

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && _doorWithLock;
            if (GUILayout.Toggle(_doorPlacementState == DoorPlacementState.Locked, "Chiusa a chiave", "Button"))
                _doorPlacementState = DoorPlacementState.Locked;
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();

            if (_tool == Tool.PlaceDoor)
                GUILayout.Label("Click mappa: inserisce porta con queste opzioni");
        }

        // =============================================================================
        // DrawFoodStockPlacementControls
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna il blocco UI dedicato al cibo a terra. L'utente sceglie se lo stock
        /// e' comunitario oppure privato di un NPC selezionato dalla lista degli NPC
        /// presenti nel mondo runtime.
        /// </para>
        ///
        /// <para><b>Proprieta' come fatto del mondo</b></para>
        /// <para>
        /// La scelta comunitario/NPC diventa <c>OwnerKind</c>/<c>OwnerId</c> dentro il
        /// <c>FoodStockComponent</c>. Questo consente alle regole di bisogno e furto di
        /// ragionare sul cibo senza dipendere dalla UI.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Unita'</b>: quantita' iniziale dello stock piazzato.</item>
        ///   <item><b>Community/NPC</b>: selettore proprieta' dello stock.</item>
        ///   <item><b>Lista NPC</b>: scelta del proprietario quando il cibo e' privato.</item>
        /// </list>
        /// </summary>
        private void DrawFoodStockPlacementControls(World world)
        {
            GUILayout.Space(8);
            GUILayout.Label("Cibo a terra");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Unita'", GUILayout.Width(54));
            _foodStockUnits = DrawPositiveIntField(_foodStockUnits, GUILayout.Width(70));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(!_foodStockOwnedByNpc, "Comunitario", "Button"))
                _foodStockOwnedByNpc = false;
            if (GUILayout.Toggle(_foodStockOwnedByNpc, "Di un NPC", "Button"))
            {
                _foodStockOwnedByNpc = true;
                EnsureFoodOwnerNpcSelection(world);
            }
            GUILayout.EndHorizontal();

            if (_foodStockOwnedByNpc)
                DrawFoodOwnerNpcList(world);

            if (_tool == Tool.PlaceFoodStock)
                GUILayout.Label("Click mappa: inserisce cibo con queste proprieta'");
        }

        // =============================================================================
        // DrawFoodOwnerNpcList
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna la lista compatta degli NPC disponibili come proprietari dello stock
        /// di cibo a terra. La lista e' letta dal <c>World</c> ma produce solo uno stato
        /// temporaneo della UI, usato poi per costruire il comando di placement.
        /// </para>
        ///
        /// <para><b>Accesso read-only dalla View</b></para>
        /// <para>
        /// Leggere gli NPC per popolare una scelta debug e' accettabile lato View; la
        /// mutazione della proprieta' dello stock resta comunque demandata al comando.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Fallback</b>: se il proprietario selezionato sparisce, viene scelto il primo NPC valido.</item>
        ///   <item><b>Toggle</b>: ogni NPC e' una scelta mutuamente esclusiva.</item>
        ///   <item><b>Nome</b>: mostra id e nome per evitare ambiguita' durante il debug.</item>
        /// </list>
        /// </summary>
        private void DrawFoodOwnerNpcList(World world)
        {
            EnsureFoodOwnerNpcSelection(world);

            if (_foodStockOwnerNpcId <= 0)
            {
                GUILayout.Label("Nessun NPC disponibile come proprietario");
                return;
            }

            foreach (var kv in world.NpcDna)
            {
                int npcId = kv.Key;
                if (npcId <= 0)
                    continue;

                string npcName = kv.Value != null ? kv.Value.Identity.Name : "<unnamed>";
                bool isSelected = _foodStockOwnerNpcId == npcId;
                string label = $"{npcId} '{npcName}'";

                if (GUILayout.Toggle(isSelected, label, "Button"))
                    _foodStockOwnerNpcId = npcId;
            }
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

        // =============================================================================
        // BuildDoorPlacementCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce lo stato UI del pannello porta in un comando di piazzamento oggetto.
        /// La UI espone una sola azione "Inserisci porta", mentre qui scegliamo il defId
        /// tecnico coerente con la presenza o meno della serratura.
        /// </para>
        ///
        /// <para><b>Mappatura UI -> ObjectDef</b></para>
        /// <para>
        /// La porta senza serratura usa <c>door_wood_good</c>; la porta con serratura
        /// usa <c>door_wood_locked</c>. La possibilita' di lock reale viene comunque
        /// rivalidata da <c>DevPlaceObjectCommand</c> tramite la <c>ObjectDef</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>defId</b>: selezionato in base alla checkbox serratura.</item>
        ///   <item><b>doorOpen</b>: true solo per stato aperta.</item>
        ///   <item><b>doorLocked</b>: true solo per stato locked e porta con serratura.</item>
        /// </list>
        /// </summary>
        private ICommand BuildDoorPlacementCommand(int cellX, int cellY)
        {
            string defId = _doorWithLock ? "door_wood_locked" : "door_wood_good";
            bool isOpen = _doorPlacementState == DoorPlacementState.Open;
            bool isLocked = _doorWithLock && _doorPlacementState == DoorPlacementState.Locked;

            return new DevPlaceObjectCommand(
                defId,
                cellX,
                cellY,
                ownerKind: OwnerKind.Community,
                ownerId: 0,
                doorOpen: isOpen,
                doorLocked: isLocked);
        }

        // =============================================================================
        // BuildFoodStockPlacementCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce lo stato UI del pannello cibo in un comando di piazzamento
        /// <c>food_stock</c>, includendo quantita' e proprietario logico.
        /// </para>
        ///
        /// <para><b>Proprieta' sistemica del cibo</b></para>
        /// <para>
        /// Un cibo comunitario rimane accessibile come risorsa condivisa. Un cibo di
        /// un NPC diventa invece stock privato a terra e alimenta le regole di furto,
        /// memoria e pinned belief gia' presenti nel core.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>OwnerKind.Community</b>: default quando la checkbox NPC non e' attiva.</item>
        ///   <item><b>OwnerKind.Npc</b>: usato solo se esiste un proprietario valido.</item>
        ///   <item><b>foodUnits</b>: quantita' iniziale normalizzata a valori positivi.</item>
        /// </list>
        /// </summary>
        private ICommand BuildFoodStockPlacementCommand(World world, int cellX, int cellY)
        {
            OwnerKind ownerKind = OwnerKind.Community;
            int ownerId = 0;

            if (_foodStockOwnedByNpc)
            {
                EnsureFoodOwnerNpcSelection(world);
                if (_foodStockOwnerNpcId > 0 && world != null && world.ExistsNpc(_foodStockOwnerNpcId))
                {
                    ownerKind = OwnerKind.Npc;
                    ownerId = _foodStockOwnerNpcId;
                }
            }

            return new DevPlaceObjectCommand(
                "food_stock",
                cellX,
                cellY,
                ownerKind,
                ownerId,
                foodUnits: Mathf.Max(1, _foodStockUnits));
        }

        // =============================================================================
        // EnsureFoodOwnerNpcSelection
        // =============================================================================
        /// <summary>
        /// <para>
        /// Garantisce che la selezione del proprietario del cibo punti a un NPC ancora
        /// presente. Se il valore corrente e' vuoto o obsoleto, sceglie il primo NPC
        /// disponibile nel mondo.
        /// </para>
        ///
        /// <para><b>Robustezza della UI runtime</b></para>
        /// <para>
        /// Gli NPC possono essere creati o cancellati mentre la finestra resta aperta.
        /// Questo helper evita che la UI conservi un proprietario morto e produca cibo
        /// privato di un id non valido.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione corrente</b>: mantiene la scelta se l'NPC esiste.</item>
        ///   <item><b>Fallback deterministico</b>: usa il primo id positivo disponibile.</item>
        ///   <item><b>Sentinella</b>: usa -1 quando non ci sono NPC.</item>
        /// </list>
        /// </summary>
        private void EnsureFoodOwnerNpcSelection(World world)
        {
            if (world != null && _foodStockOwnerNpcId > 0 && world.ExistsNpc(_foodStockOwnerNpcId))
                return;

            _foodStockOwnerNpcId = -1;
            if (world == null)
                return;

            foreach (var kv in world.NpcDna)
            {
                if (kv.Key <= 0)
                    continue;

                _foodStockOwnerNpcId = kv.Key;
                return;
            }
        }

        // =============================================================================
        // DrawPositiveIntField
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna un campo numerico IMGUI per valori interi positivi. Se l'utente
        /// inserisce testo non valido, conserva il valore precedente normalizzato.
        /// </para>
        ///
        /// <para><b>Input debug tollerante</b></para>
        /// <para>
        /// La finestra DevTools deve restare usabile anche durante editing parziale del
        /// testo. Questo helper evita eccezioni e impedisce quantita' zero o negative.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>TextField</b>: input IMGUI semplice.</item>
        ///   <item><b>TryParse</b>: validazione senza eccezioni.</item>
        ///   <item><b>Mathf.Max</b>: clamp finale a minimo 1.</item>
        /// </list>
        /// </summary>
        private static int DrawPositiveIntField(int currentValue, params GUILayoutOption[] options)
        {
            int safeCurrent = Mathf.Max(1, currentValue);
            string text = GUILayout.TextField(safeCurrent.ToString(), options);

            if (int.TryParse(text, out int parsed))
                return Mathf.Max(1, parsed);

            return safeCurrent;
        }

        // =============================================================================
        // IsDedicatedDevToolObject
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se una definizione oggetto e' gestita da un pannello dedicato della
        /// finestra DevTools invece che dalla palette generica.
        /// </para>
        ///
        /// <para><b>UI semantica sopra palette tecnica</b></para>
        /// <para>
        /// Porte e cibo hanno opzioni sistemiche proprie. Nasconderli dalla palette
        /// generica riduce ambiguita': l'utente non sceglie piu' "porta/porta locked",
        /// ma usa un unico inserimento porta con stato esplicito.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>IsDoor</b>: tutte le porte passano dal pannello porta.</item>
        ///   <item><b>food_stock</b>: il cibo passa dal pannello proprieta' cibo.</item>
        /// </list>
        /// </summary>
        private static bool IsDedicatedDevToolObject(ObjectDef def)
        {
            if (def == null)
                return false;

            if (def.IsDoor)
                return true;

            return def.Id == "food_stock";
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
            if (cmd == null) return;
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

            // Controllo 1: finestra IMGUI DevTools (coordinate top-left)
            var guiPos = new Vector2(sp.x, Screen.height - sp.y);
            if (_windowRect.Contains(guiPos))
            {
                _isPointerOverUiWindow = true;
                return;
            }

            // Controllo 2: qualsiasi elemento Canvas UI (TopBar, pannello EL, ecc.).
            // EventSystem.IsPointerOverGameObject restituisce true se il cursore è sopra
            // un Graphic con raycastTarget=true — cattura TopBar e pannello EL senza
            // bisogno di conoscerne i RectTransform esplicitamente.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                _isPointerOverUiWindow = true;
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

        private void ResetTransientInputState()
        {
            // Reset "paint cache" quando entri/esci: evita edge-case dove il primo brush non scrive.
            _lastPaintX = int.MinValue;
            _lastPaintY = int.MinValue;

            // Reset rect: la preview rettangolare non deve sopravvivere al cambio modalita'.
            _rectDragging = false;
        }
    }
}
