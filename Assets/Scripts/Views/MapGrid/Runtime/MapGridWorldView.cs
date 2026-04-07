using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Arcontio.Core;
using SocialViewer.UI;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// View binder: sincronizza World -> SpriteRenderers (NPC + Objects).
    /// Non scrive nel core: solo lettura.
    /// </summary>
    public sealed class MapGridWorldView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapGridConfig cfg;
        [SerializeField] private MapGridPointerInputActionsProvider pointerProvider;
        [Tooltip("Camera usata per WorldToScreen/ScreenToWorld. Se null, fallback: Camera.main o prima camera attiva trovata.")]
        [SerializeField] private Camera worldCamera;
        
        // Cache interna per evitare Find/scan ogni frame.
        private Camera _resolvedWorldCamera;

        [Header("Sprite fallbacks")]
        [SerializeField] private string defaultNpcSpritePath = "MapGrid/Sprites/NPC_Astro";

        [Header("NPC balloons (view-only)")]
        [Tooltip("Durata in secondi per cui un balloon resta visibile sopra l'NPC.")]
        [SerializeField] private float npcBalloonVisibleSeconds = 1.25f;

        [Tooltip("Offset verticale (world units) del balloon rispetto allo sprite NPC.")]
        [SerializeField] private float npcBalloonYOffsetWorld = 0.55f;

        [Tooltip("Resources path per balloon 'Eat'.")]
        [SerializeField] private string balloonEatSpritePath = "MapGrid/Sprites/Balloons/Balloon_Eat";

        [Tooltip("Resources path per balloon 'Steal'.")]
        [SerializeField] private string balloonStealSpritePath = "MapGrid/Sprites/Balloons/Balloon_Steal";

        [Tooltip("Resources path per balloon 'TheftWitnessed'.")]
        [SerializeField] private string balloonTheftWitnessedSpritePath = "MapGrid/Sprites/Balloons/Balloon_TheftWitnessed";

        [Tooltip("Resources path per balloon 'TheftSuffered'.")]
        [SerializeField] private string balloonTheftSufferedSpritePath = "MapGrid/Sprites/Balloons/Balloon_TheftSuffered";

        // Patch 0.01P3:
        // Balloon comunicazione.
        // IMPORTANTE: l'utente vuole i file in:
        //   Assets/Resources/MapGrid/Sprites/Balloons
        // e con naming:
        //   Balloon_nomeballoon
        [SerializeField] private string balloonTokenOutSpritePath = "MapGrid/Sprites/Balloons/Balloon_TokenOut";
        [SerializeField] private string balloonTokenInSpritePath = "MapGrid/Sprites/Balloons/Balloon_TokenIn";

        // Patch 0.01P3 extension: sprite per comunicazione furto (IN/OUT differenziati per ruolo)
        [SerializeField] private string balloonTheftVictimOutSpritePath = "MapGrid/Sprites/Balloons/Balloon_TheftReportVictimOut";
        [SerializeField] private string balloonTheftVictimInSpritePath = "MapGrid/Sprites/Balloons/Balloon_TheftReportVictimIn";
        [SerializeField] private string balloonTheftWitnessOutSpritePath = "MapGrid/Sprites/Balloons/Balloon_TheftReportWitnessOut";
        [SerializeField] private string balloonTheftWitnessInSpritePath = "MapGrid/Sprites/Balloons/Balloon_TheftReportWitnessIn";

        [Tooltip("[DEBUG] Balloon mostrato quando l'NPC arriva dove ricordava il cibo ma non lo trova più.")]
        [SerializeField] private string balloonFoodNotFoundSpritePath = "MapGrid/Sprites/Balloons/Balloon_FoodNotFound";

        [Header("Debug overlays")]
        [Tooltip("Sprite usato per evidenziare celle viste (DebugFovTelemetry). Deve essere un Sprite in Resources.")]
        [SerializeField] private string fovOverlaySpritePath = "MapGrid/Sprites/CellHighlight";

        [Tooltip("Sorting order dell'overlay FOV. Deve stare sotto NPC/Objects ma sopra il terreno.")]
        [SerializeField] private int fovOverlayOrder = 25;

        // ============================================================
        // Debug overlay: Landmark nodes/edges (v0.02 Day1)
        // ============================================================
        [Tooltip("Sprite usato come marker per i nodi landmark (overlay debug).")]
        [SerializeField] private string landmarkOverlayNodeSpritePath = "MapGrid/Sprites/CellHighlight";

        [Tooltip("Sorting order dell'overlay Landmark. Per debug è consigliato che stia SOPRA NPC/Objects, così i marker non vengono occlusi quando un NPC ci passa sopra.")]
        [SerializeField] private int landmarkOverlayOrder = 24;

        [Header("Sorting")]
        [SerializeField] private int terrainOrder = 0;
        [SerializeField] private int objectBaseOrder = 50;
        [SerializeField] private int npcBaseOrder = 100;
        [SerializeField] private bool sortByY = true; // DF-like

        // ---------------- Core binding ----------------
        private World _world;

        private Transform _objectsRoot;
        private Transform _npcsRoot;

        private readonly Dictionary<int, SpriteRenderer> _npcViews = new();
        private readonly Dictionary<int, SpriteRenderer> _objectViews = new();
        private readonly Dictionary<string, Sprite> _spriteCache = new();

        private Sprite _defaultNpcSprite;
        private MapGridPointerInputActionsProvider _pointerProvider;

        // ---------------- Always-on overlay: pointer cell coords (Patch 0.01P2) ----------------
        private MapGridPointerCoordsOverlay _pointerCoords;

        // ---------------- Debug overlay: FOV heatmap ----------------
        private MapGridFovHeatmapOverlay _fovOverlay;

        // ---------------- Debug overlay: Landmarks/edges (v0.02 Day1) ----------------
        private MapGridLandmarkOverlay _landmarkOverlay;
        private bool _landmarkOverlayEnabled;

        // Patch 0.03.02.a.2: label landmark via Canvas UI.
        private MapGridLandmarkLabelOverlay _landmarkLabelOverlay;

        // Patch 0.03.02.a.4: overlay valori DT numerici per debug (tasto D).
        private MapGridDtValueOverlay _dtValueOverlay;
        private bool                  _dtValueOverlayEnabled;
        private GvdDinOverlaySnapshot _dtSnapshot = new GvdDinOverlaySnapshot();

        // ---------------- Debug overlay: GVD-DIN (v0.03) ----------------
        // Il toggle GVD-DIN riusa _landmarkOverlay (stessa istanza), attivando
        // i layer GVD-DIN interni tramite SetGvdDinLayerFlags().
        // Viene mostrato solo quando il LandmarkOverlay è attivo.
        private bool _gvdDinOverlayEnabled;

        // ---------------- Debug overlay: Summary cards (F1) ----------------
        //
        // Requisito:
        // - quando attivo: tooltip sparisce e compaiono schede sopra ogni NPC e oggetto interagibile.
        // - quando disattivo: schede spariscono e torna tooltip.
        private MapGridEntitySummaryOverlay _summaryOverlay;
        private bool _summaryOverlayEnabled;

        // ============================================================
        // Debug click-to-move (runtime test tool)
        // ============================================================
        // UX voluta:
        // - premi K quando hai selezionato / puntato un NPC => quel NPC diventa il target sticky
        // - poi clicchi una cella della mappa => emettiamo un MoveIntent verso quella cella
        //
        // Perché sticky:
        // - se usassimo l'NPC sotto il mouse al momento del click, quando clicchi il terreno
        //   non staresti più sopra l'NPC e quindi il comando non partirebbe.
        private bool _debugClickMoveModeEnabled;
        private int _debugClickMoveNpcId = -1;

        /// <summary>
        /// Flag read-only: utile ad altri sistemi view-only per “spegnersi” quando il SummaryOverlay è attivo.
        /// </summary>
        public bool IsSummaryOverlayEnabled => _summaryOverlayEnabled;

        // ============================================================
        // DevTools bridge (v0.02.RDM)
        // ============================================================
        //
        // Motivazione:
        // - Il DevMode runtime (MapGridRuntimeDevToolsOverlay) ha bisogno di:
        //   1) MapGridConfig effettivamente in uso (caricata dal JSON ufficiale)
        //   2) Provider input puntatore (New Input System) già configurato dal Bootstrap
        //   3) Camera di riferimento per ScreenToWorld
        //
        // Problema risolto:
        // - Se MapGridConfig è una classe [Serializable] e non un asset, Unity la serializza "inline".
        //   L'Inspector mostra quindi un albero di campi (mapWidth, mapHeight, ...).
        //   Questo porta l'utente a dover duplicare la config manualmente nel DevToolsOverlay.
        //
        // Soluzione:
        // - Espongo accessori read-only "safe" su MapGridWorldView.
        // - Il DevToolsOverlay può auto-bindarsi senza richiedere configurazione manuale.
        //
        // Nota:
        // - Queste property NON introducono dipendenze core->view: restiamo view-only.
        // - Sono volutamente read-only per evitare che il DevTools modifichi la config a runtime.
        public MapGridConfig RuntimeConfig => cfg;
        public MapGridPointerInputActionsProvider RuntimePointerProvider => _pointerProvider != null ? _pointerProvider : pointerProvider;
        public Camera RuntimeWorldCamera => worldCamera != null ? worldCamera : (Camera.main != null ? Camera.main : FindObjectOfType<Camera>());

        public void Init(MapGridConfig config)
        {
            cfg = config;

            _objectsRoot = new GameObject("ObjectViews").transform;
            _objectsRoot.SetParent(transform, false);

            _npcsRoot = new GameObject("NPCViews").transform;
            _npcsRoot.SetParent(transform, false);

            // default npc sprite: prima da config, altrimenti fallback
            var npcPath = cfg?.npc?.spriteResourcePath;
            if (string.IsNullOrWhiteSpace(npcPath)) npcPath = defaultNpcSpritePath;

            _defaultNpcSprite = LoadSpriteCached(npcPath);
            if (_defaultNpcSprite == null)
                Debug.LogWarning($"[MapGrid] Default NPC sprite not found at Resources/{npcPath}.png");

            // Patch 0.01P2:
            // indicatore costante in alto a sinistra con le coordinate della cella sotto il mouse.
            // Nota: deve restare attivo anche quando SummaryOverlay è ON (tooltip off).
            _pointerCoords = new MapGridPointerCoordsOverlay();

            // ============================================================
            // Debug overlay: FOV heatmap
            // ============================================================
            // Nota:
            // - L'overlay legge dal World.DebugFovTelemetry.
            // - Se la feature è disabilitata da config, _world.DebugFovTelemetry sarà null
            //   e quindi non renderizziamo nulla.
            _fovOverlay = new MapGridFovHeatmapOverlay();
            _fovOverlay.Init(transform, cfg.tileSizeWorld, fovOverlaySpritePath, fovOverlayOrder);

            // ============================================================
            // Debug overlay: Landmarks/edges (v0.02 Day1)
            // ============================================================
            // Nota:
            // - In Day1 non ci sono ancora dati landmark reali: l'overlay renderizza liste vuote.
            // - Serve però per verificare toggle UI + pipeline view->world->overlay.
            _landmarkOverlay = new MapGridLandmarkOverlay();

            // ------------------------------------------------------------
            // HOTFIX (Day2/Day3 debug UX): Landmark overlay non deve sparire sotto gli NPC.
            // ------------------------------------------------------------
            // Problema osservato in runtime:
            // - quando un NPC passa sopra un nodo, alcuni marker/linee "spariscono".
            // Causa reale:
            // - non è (necessariamente) un problema di memoria/registry: è rendering order.
            // - di default landmarkOverlayOrder (24) è SOTTO npcBaseOrder (100) e objectBaseOrder (50).
            //   quindi SpriteRenderer/LineRenderer dell'overlay vengono occlusi da NPC/oggetti.
            // Fix:
            // - forziamo un sorting order minimo sopra NPC/objects.
            // Nota:
            // - questo è un overlay debug, quindi la scelta UX migliore è "sempre visibile".
            int safeLandmarkOverlayOrder = landmarkOverlayOrder;
            int minAboveNpc = npcBaseOrder + 5;
            int minAboveObj = objectBaseOrder + 5;
            if (safeLandmarkOverlayOrder < minAboveNpc) safeLandmarkOverlayOrder = minAboveNpc;
            if (safeLandmarkOverlayOrder < minAboveObj) safeLandmarkOverlayOrder = minAboveObj;

            _landmarkOverlay.Init(transform, cfg.tileSizeWorld, landmarkOverlayNodeSpritePath, safeLandmarkOverlayOrder);
            _landmarkOverlayEnabled = false;

            // Patch 0.03.01.h: inizializza il label overlay Canvas UI.
            _landmarkLabelOverlay = new MapGridLandmarkLabelOverlay();
            _landmarkLabelOverlay.Init(transform);
            // Parte disabilitato — si attiva insieme a _landmarkOverlay con il tasto L.

            // Patch 0.03.02.a.4: overlay valori DT numerici (tasto D).
            _dtValueOverlay = new MapGridDtValueOverlay();
            _dtValueOverlay.Init(transform);
            _dtValueOverlayEnabled = false;

            // ============================================================
            // Debug overlay: Summary cards (F1)
            // ============================================================
            // Nota:
            // - View-only.
            // - Non deve mai “bloccare” la sim: se world/camera null, semplicemente non renderizza.
            _summaryOverlay = new MapGridEntitySummaryOverlay();
            _summaryOverlay.AttachTo(transform);          // ✅ QUESTA RIGA MANCAVA
            // Card overlay attiva di default all'avvio.
            _summaryOverlay.SetEnabled(true);
            _summaryOverlayEnabled = true;
        }

        private void Update()
        {
            if (cfg == null) return;

            // Bind al world (ritenta finché non c'è)
            _world ??= MapGridWorldProvider.TryGetWorld();
            if (_world == null) return;

            // ============================================================
            // INPUT (debug): F1 toggle SummaryOverlay
            // ============================================================
            //
            // Scelta tecnica:
            // - Uso diretto Keyboard.current per un toggle debug.
            // - Così non devo toccare il file .inputactions.
            //
            // Se in futuro vuoi “configurabile”, lo facciamo via InputActionReference.
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                // Debug UX: Shift + toggle => reset posizioni card (offset) e forza relayout iniziale.
                bool reset = (Keyboard.current.leftShiftKey != null && Keyboard.current.leftShiftKey.isPressed)
                             || (Keyboard.current.rightShiftKey != null && Keyboard.current.rightShiftKey.isPressed);

                if (reset && _summaryOverlay != null)
                    _summaryOverlay.ClearOffsetsAndRequestRelayout();

                ToggleSummaryOverlay();
            }

// ============================================================
// INPUT (debug): L toggle LandmarkOverlay (v0.02 Day1)
// ============================================================
// Scelta:
// - hotkey semplice per debug (come SummaryOverlay), senza toccare .inputactions.
// - se in futuro vuoi, lo rendiamo InputActionReference.
if (Keyboard.current != null && Keyboard.current.lKey != null && Keyboard.current.lKey.wasPressedThisFrame)
{
    _landmarkOverlayEnabled = !_landmarkOverlayEnabled;
    _landmarkOverlay?.SetEnabled(_landmarkOverlayEnabled);
    // Patch 0.03.01.h: label overlay segue il toggle L.
    _landmarkLabelOverlay?.SetEnabled(_landmarkOverlayEnabled);

    // Se si spegne il landmark overlay, azzera anche il flag GVD-DIN.
    if (!_landmarkOverlayEnabled && _gvdDinOverlayEnabled)
    {
        _gvdDinOverlayEnabled = false;
        _landmarkOverlay?.SetGvdDinLayerFlags(false, false, false);
    }
}

// ============================================================
// INPUT (debug): G toggle GVD-DIN overlay layer (v0.03)
// ============================================================
// Attiva/disattiva i tre layer GVD-DIN sovrapposti al LandmarkOverlay.
// Funziona solo se il LandmarkOverlay è già attivo (L premuto).
// Legge i flag di visibilità da game_params.json (gvd_din.debug.*).
if (Keyboard.current != null && Keyboard.current.gKey != null && Keyboard.current.gKey.wasPressedThisFrame)
{
    _gvdDinOverlayEnabled = !_gvdDinOverlayEnabled;

    // Se il landmark overlay non è attivo, lo attiviamo automaticamente.
    if (_gvdDinOverlayEnabled && !_landmarkOverlayEnabled)
    {
        _landmarkOverlayEnabled = true;
        _landmarkOverlay?.SetEnabled(true);
    }

    // Leggiamo i flag da config (così l'utente può cambiarli nel JSON senza ricompilare).
    if (_landmarkOverlay != null && _world != null)
    {
        var gvdCfg = _world.Config?.Sim?.gvd_din?.debug;
        bool showDt    = _gvdDinOverlayEnabled && (gvdCfg?.show_dt_heatmap ?? false);
        bool showRaw   = _gvdDinOverlayEnabled && (gvdCfg?.show_gvd_raw    ?? false);
        bool showNodes = _gvdDinOverlayEnabled && (gvdCfg?.show_gvd_nodes  ?? true);
        _landmarkOverlay.SetGvdDinLayerFlags(showDt, showRaw, showNodes);
    }
}

// ============================================================
// INPUT (debug): D toggle DT value overlay (v0.03.02.a.4)
// ============================================================
// Mostra il valore numerico della Distance Transform su ogni cella.
// Utile per verificare che la DT sia calcolata correttamente.
// Indipendente da L e G — può essere attivato da solo.
if (Keyboard.current != null && Keyboard.current.dKey != null && Keyboard.current.dKey.wasPressedThisFrame)
{
    _dtValueOverlayEnabled = !_dtValueOverlayEnabled;
    _dtValueOverlay?.SetEnabled(_dtValueOverlayEnabled);
}

            // ============================================================
            // INPUT (debug): K toggle Click-To-Move mode
            // ============================================================
            // UX:
            // - alla pressione di K proviamo a catturare un NPC "sticky":
            //   1) NPCSelection.SelectedNpcId
            //   2) NPC sotto il mouse in quel momento
            // - se troviamo un NPC valido, attiviamo/disattiviamo la modalita'
            //   e memorizziamo il target sticky.
            // - se non troviamo nessun NPC, il toggle non si attiva (evitiamo false aspettative).
            if (Keyboard.current != null && Keyboard.current.kKey != null && Keyboard.current.kKey.wasPressedThisFrame)
            {
                int selectedNpcId = NPCSelection.SelectedNpcId;
                int hoveredNpcId = ResolveHoveredNpcId();
                int candidateNpcId = selectedNpcId > 0 ? selectedNpcId : hoveredNpcId;

                if (_debugClickMoveModeEnabled)
                {
                    // Seconda pressione: spegniamo sempre la modalita'.
                    _debugClickMoveModeEnabled = false;
                    _debugClickMoveNpcId = -1;
                }
                else if (candidateNpcId > 0)
                {
                    _debugClickMoveModeEnabled = true;
                    _debugClickMoveNpcId = candidateNpcId;
                }
            }

            // ============================================================
            // INPUT (debug): Click-To-Move command emission
            // ============================================================
            // Regola:
            // - funziona solo se la modalita' e' attiva e abbiamo un NPC sticky valido.
            // - il click sinistro su una cella della mappa emette un SetMoveIntentCommand
            //   verso quella cella.
            if (_debugClickMoveModeEnabled && _debugClickMoveNpcId > 0 && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryIssueDebugClickMoveOrder(_debugClickMoveNpcId);
            }

            SyncObjects();
            SyncNpcs();

            // (opzionale) cleanup: se entità spariscono, rimuovi view
            CleanupMissing();

            // ============================================================
            // DEBUG FOV OVERLAY (heatmap su finestre N tick)
            // ============================================================
            // Policy UX:
            // - Mostriamo la heatmap dell'NPC "attivo":
            //   - se il mouse è sopra un NPC, usiamo quello.
            //   - altrimenti fallback: primo NPC esistente.
            //
            // Nota:
            // - la view NON ricalcola la percezione.
            // - legge il buffer READ del core (DebugFovTelemetry), che contiene la somma
            //   dei coni nella finestra di N tick.
            // Calcoliamo UNA volta l'NPC “attivo” per gli overlay diagnostici.
            // Questo evita shadowing di variabili locali e garantisce coerenza: FOV e Landmark
            // devono riferirsi allo stesso soggetto quando l'utente muove il mouse.
            int activeNpcId = ResolveActiveNpcForFovOverlay();

            if (_world.DebugFovTelemetry != null && _fovOverlay != null)
            {
                if (activeNpcId > 0 && _world.DebugFovTelemetry.TryGetReadHeat(activeNpcId, out var heat))
                {
                    int windowTicks = _world.DebugFovTelemetry.WindowTicks;
                    _fovOverlay.Render(heat, _world.DebugFovTelemetry.Width, _world.DebugFovTelemetry.Height, windowTicks);
                }
                else
                {
                    _fovOverlay.Clear();
                }
            }

            // ============================================================
            // LANDMARK OVERLAY (v0.02 Day1)
            // ============================================================
            // Policy UX:
            // - Mostriamo (come per FOV) SOLO l'NPC “attivo”.
            // - In Day1, il core non fornisce ancora nodi/edges: overlay vuoto, ma toggle verificabile.
            if (_landmarkOverlayEnabled && _landmarkOverlay != null)
            {
                if (activeNpcId > 0)
                    _landmarkOverlay.Render(_world, activeNpcId);
                else
                    _landmarkOverlay.Clear();
            }

            // Patch 0.03.01.h: render label overlay Canvas UI.
            // Legge i nodi già popolati da _landmarkOverlay.Render() tramite le
            // proprietà read-only WorldNodes / KnownNodes / RouteNodes / GvdNodes.
            if (_landmarkOverlayEnabled && _landmarkLabelOverlay != null && _world != null)
            {
                var cam2 = ResolveWorldCamera();
                float tileSize = cfg != null ? cfg.tileSizeWorld : 1f;
                if (activeNpcId > 0 && cam2 != null)
                {
                    // Converti IReadOnlyList → List per la firma del metodo Render.
                    // Usiamo liste temporanee locali — il costo è trascurabile (solo riferimenti).
                    var wn = new System.Collections.Generic.List<LandmarkOverlayNode>(_landmarkOverlay.WorldNodes);
                    var kn = new System.Collections.Generic.List<LandmarkOverlayNode>(_landmarkOverlay.KnownNodes);
                    var rn = new System.Collections.Generic.List<LandmarkOverlayNode>(_landmarkOverlay.RouteNodes);
                    var gn = new System.Collections.Generic.List<LandmarkOverlayNode>(_landmarkOverlay.GvdNodes);
                    _landmarkLabelOverlay.Render(_world, cam2, tileSize, wn, kn, rn, gn);
                }
                else
                {
                    _landmarkLabelOverlay.Clear();
                }
            }

            // Patch 0.03.02.a.4: render DT value overlay (tasto D).
            // Riusa lo snapshot GVD-DIN già popolato nell'Update precedente
            // da LandmarkOverlay.Render → GetGvdDinOverlayData.
            if (_dtValueOverlayEnabled && _dtValueOverlay != null && _world != null)
            {
                var cam3 = ResolveWorldCamera();
                float tileSize3 = cfg != null ? cfg.tileSizeWorld : 1f;
                if (cam3 != null)
                {
                    // Popola lo snapshot DT fresco per questo frame
                    _world.GetGvdDinOverlayData(_dtSnapshot);
                    _dtValueOverlay.Render(_dtSnapshot, cam3, tileSize3);
                }
                else
                {
                    _dtValueOverlay.Clear();
                }
            }

            // ============================================================
            // Summary overlay (F1) vs Hover tooltip (default)
            // ============================================================
            var cam = ResolveWorldCamera();

            // ============================================================
            // Always-on pointer coords overlay (Patch 0.01P2)
            // ============================================================
            // Aggiorniamo SEMPRE (indipendentemente da SummaryOverlay), perché è un indicatore costante.
            if (_pointerCoords != null)
            {
                if (cam == null || _pointerProvider == null || !_pointerProvider.TryGetPointerScreenPosition(out var pp) || cfg.tileSizeWorld <= 0f)
                {
                    _pointerCoords.SetUnknown();
                }
                else
                {
                    Vector3 wp = cam.ScreenToWorldPoint(new Vector3(pp.x, pp.y, 0f));
                    int cx = Mathf.FloorToInt(wp.x / cfg.tileSizeWorld);
                    int cy = Mathf.FloorToInt(wp.y / cfg.tileSizeWorld);

                    bool inBounds = (_world != null) && _world.InBounds(cx, cy);
                    _pointerCoords.SetCell(cx, cy, inBounds);
                }
            }

            if (_summaryOverlayEnabled)
            {
                if (cam != null && _summaryOverlay != null)
                    _summaryOverlay.Tick(_world, cam, cfg.tileSizeWorld);
            }
            else
            {
                if (_summaryOverlay != null)
                    _summaryOverlay.SetEnabled(false);
            }
        }

        private void ToggleSummaryOverlay()
        {
            _summaryOverlayEnabled = !_summaryOverlayEnabled;

            if (_summaryOverlay != null)
                _summaryOverlay.SetEnabled(_summaryOverlayEnabled);
        }

        /// <summary>
        /// Decide quale NPC considerare "attivo" per l'overlay FOV.
        ///
        /// Regola (semplice, debug):
        /// 1) se puntatore sopra una cella che contiene NPC => quel NPC
        /// 2) altrimenti => primo NPC nel world (se esiste)
        /// </summary>
        private int ResolveActiveNpcForFovOverlay()
        {
            /*// 1) prova hover: pointer -> world -> cell -> npc
            var cam = ResolveWorldCamera();
            if (cam != null && _pointerProvider != null && _pointerProvider.TryGetPointerScreenPosition(out var p))
            {
                Vector3 wp = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, 0f));
                int cellX = Mathf.FloorToInt(wp.x / cfg.tileSizeWorld);
                int cellY = Mathf.FloorToInt(wp.y / cfg.tileSizeWorld);

                int hovered = FindNpcAtCell(_world, cellX, cellY);
                if (hovered > 0)
                    return hovered;
            }

            // 2) fallback: primo NPC esistente
            foreach (var kv in _world.NpcDna)
                return kv.Key;

            return -1;*/

            // Questo blocco di modifica fa sì che se il mouse non è su un NPC, la heatmap viene nascosta
            return ResolveHoveredNpcId();
        }

        private int ResolveHoveredNpcId()
        {
            if (_world == null || cfg == null || cfg.tileSizeWorld <= 0f)
                return -1;

            var cam = ResolveWorldCamera();
            if (cam == null || _pointerProvider == null || !_pointerProvider.TryGetPointerScreenPosition(out var pointer))
                return -1;

            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, Mathf.Abs(cam.transform.position.z)));
            wp.z = 0f;

            int cellX = Mathf.FloorToInt(wp.x / cfg.tileSizeWorld);
            int cellY = Mathf.FloorToInt(wp.y / cfg.tileSizeWorld);

            if (!_world.InBounds(cellX, cellY))
                return -1;

            return FindNpcAtCell(_world, cellX, cellY);
        }

        private void TryIssueDebugClickMoveOrder(int npcId)
        {
            if (_world == null || cfg == null || cfg.tileSizeWorld <= 0f) return;
            if (!_world.NpcDna.ContainsKey(npcId)) return;

            var cam = ResolveWorldCamera();
            if (cam == null || _pointerProvider == null || !_pointerProvider.TryGetPointerScreenPosition(out var p))
                return;

            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, Mathf.Abs(cam.transform.position.z)));
            wp.z = 0f;

            int cellX = Mathf.FloorToInt(wp.x / cfg.tileSizeWorld);
            int cellY = Mathf.FloorToInt(wp.y / cfg.tileSizeWorld);

            if (!_world.InBounds(cellX, cellY))
                return;

            var cmd = new SetMoveIntentCommand(npcId, new MoveIntent
            {
                Active = true,
                TargetX = cellX,
                TargetY = cellY,
                Reason = MoveIntentReason.Wander,
                TargetObjectId = 0,
                BlockedTicks = 0,
            });

            SimulationHost.Instance?.EnqueueExternalCommand(cmd);
        }

        private static int FindNpcAtCell(World world, int x, int y)
        {
            if (world == null) return -1;
            foreach (var kv in world.GridPos)
            {
                var p = kv.Value;
                if (p.X == x && p.Y == y)
                    return kv.Key;
            }
            return -1;
        }

        private void SyncNpcs()
        {
            foreach (var kv in _world.GridPos)
            {
                int npcId = kv.Key;
                var pos = kv.Value;

                if (!_npcViews.TryGetValue(npcId, out var sr) || sr == null)
                {
                    sr = CreateSpriteRenderer(_npcsRoot, $"NPC_{npcId}", _defaultNpcSprite);
                    _npcViews[npcId] = sr;

                    // ---- NEW: collider + handle per hover ----
                    // Motivazione:
                    // - la view non deve conoscere input system complessi
                    // - con un BoxCollider2D posso fare Physics2D.Raycast in modo semplice.
                    EnsureNpcHoverComponents(sr.gameObject, npcId);

                    // ---- NEW: balloon view ----
                    // Un piccolo fumetto sopra la testa dell'NPC, quando il core emette NpcBalloonSignal.
                    EnsureNpcBalloonComponents(sr.gameObject, npcId);
                }

                sr.transform.position = CellCenterWorld(pos.X, pos.Y);
                sr.sortingOrder = sortByY ? npcBaseOrder - pos.Y : npcBaseOrder;
            }
        }

        private void EnsureNpcBalloonComponents(GameObject npcGo, int npcId)
        {
            if (npcGo == null) return;

            var balloon = npcGo.GetComponent<MapGridNpcBalloonView>();
            if (balloon == null)
                balloon = npcGo.AddComponent<MapGridNpcBalloonView>();

            // Mapping sprite per kind.
            // Nota:
            // - Lo teniamo qui (WorldView) perché è l'unico posto che conosce i path Resources della mappa.
            var map = new Dictionary<Arcontio.Core.NpcBalloonKind, string>
            {
                { Arcontio.Core.NpcBalloonKind.Eat, balloonEatSpritePath },
                { Arcontio.Core.NpcBalloonKind.Steal, balloonStealSpritePath },
                { Arcontio.Core.NpcBalloonKind.TheftWitnessed, balloonTheftWitnessedSpritePath },
                { Arcontio.Core.NpcBalloonKind.TheftSuffered, balloonTheftSufferedSpritePath },

                // Patch 0.01P3
                { Arcontio.Core.NpcBalloonKind.TokenOut, balloonTokenOutSpritePath },
                { Arcontio.Core.NpcBalloonKind.TokenIn, balloonTokenInSpritePath },

                // Patch 0.01P3 extension: comunicazione furto
                { Arcontio.Core.NpcBalloonKind.TheftReportVictimOut, balloonTheftVictimOutSpritePath },
                { Arcontio.Core.NpcBalloonKind.TheftReportVictimIn, balloonTheftVictimInSpritePath },
                { Arcontio.Core.NpcBalloonKind.TheftReportWitnessOut, balloonTheftWitnessOutSpritePath },
                { Arcontio.Core.NpcBalloonKind.TheftReportWitnessIn, balloonTheftWitnessInSpritePath },

                // Debug: cibo non trovato alla posizione ricordata
                { Arcontio.Core.NpcBalloonKind.FoodNotFound, balloonFoodNotFoundSpritePath },
            };

            balloon.Init(npcId, npcBalloonYOffsetWorld, npcBalloonVisibleSeconds, map);
        }

        public void SetPointerProvider(MapGridPointerInputActionsProvider provider)
        {
            _pointerProvider = provider;
        }

        private void SyncObjects()
        {
            foreach (var kv in _world.Objects)
            {
                int objId = kv.Key;
                var inst = kv.Value;

                if (!_objectViews.TryGetValue(objId, out var sr) || sr == null)
                {
                    // risolvi spriteKey da ObjectDef (se presente), altrimenti fallback
                    string spriteKey = null;

                    if (_world.TryGetObjectDef(inst.DefId, out var def))
                    {
                        // Il tuo JSON ha "SpriteKey".
                        // Assumo che in C# ObjectDef abbia "SpriteKey".
                        spriteKey = def.SpriteKey;
                    }

                    if (string.IsNullOrWhiteSpace(spriteKey))
                        spriteKey = $"MapGrid/Sprites/Objects/{inst.DefId}";

                    var sprite = LoadSpriteCached(spriteKey);
                    if (sprite == null)
                        Debug.LogWarning($"[MapGrid] Missing object sprite for defId='{inst.DefId}' at Resources/{spriteKey}.png");

                    sr = CreateSpriteRenderer(_objectsRoot, $"OBJ_{objId}_{inst.DefId}", sprite);
                    _objectViews[objId] = sr;
                }

                sr.transform.position = CellCenterWorld(inst.CellX, inst.CellY);
                sr.sortingOrder = sortByY ? objectBaseOrder - inst.CellY : objectBaseOrder;

                // Stock label (solo se è FoodStock)
                var label = sr.GetComponent<MapGridStockLabel>();
                if (label == null) label = sr.gameObject.AddComponent<MapGridStockLabel>();

                if (_world.FoodStocks.TryGetValue(objId, out var stock))
                {
                    label.SetText(stock.Units.ToString());
                    label.SetSorting(sr.sortingOrder);
                }
                else
                {
                    label.SetText("");
                }
            }
        }

        private void CleanupMissing()
        {
            // NPC: se non esiste più in GridPos, distruggi view
            // (Oggi è ok O(n) perché poche entità; se cresce, ottimizziamo con stamp tick.)
            var npcToRemove = ListPool.Get();

            foreach (var id in _npcViews.Keys)
                if (!_world.GridPos.ContainsKey(id))
                    npcToRemove.Add(id);

            foreach (var id in npcToRemove)
            {
                if (_npcViews.TryGetValue(id, out var sr) && sr != null)
                    Destroy(sr.gameObject);

                _npcViews.Remove(id);
            }

            ListPool.Release(npcToRemove);

            var objToRemove = ListPool.Get();

            foreach (var id in _objectViews.Keys)
                if (!_world.Objects.ContainsKey(id))
                    objToRemove.Add(id);

            foreach (var id in objToRemove)
            {
                if (_objectViews.TryGetValue(id, out var sr) && sr != null)
                    Destroy(sr.gameObject);

                _objectViews.Remove(id);
            }

            ListPool.Release(objToRemove);
        }

        private Vector3 CellCenterWorld(int cellX, int cellY)
        {
            float wx = (cellX + 0.5f) * cfg.tileSizeWorld;
            float wy = (cellY + 0.5f) * cfg.tileSizeWorld;
            return new Vector3(wx, wy, 0f);
        }

        private SpriteRenderer CreateSpriteRenderer(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 0;

            // Se vuoi evitare che terreno copra sprites: assicurati terrainOrder << objectBaseOrder
            // Il terreno (mesh) non usa sortingOrder; quindi questo va bene.
            // Se sprite null, l'oggetto esiste comunque e lo vedi in hierarchy (debug).

            return sr;
        }

        private Sprite LoadSpriteCached(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;

            if (_spriteCache.TryGetValue(resourcePath, out var s) && s != null)
                return s;

            s = Resources.Load<Sprite>(resourcePath);
            _spriteCache[resourcePath] = s;
            return s;
        }

        /// <summary>
        /// NEW:
        /// Garantisce che l’NPC view sia hittabile dal mouse e che porti l’id dell’NPC.
        ///
        /// Nota architetturale:
        /// - NON aggiungiamo dipendenze al Core.
        /// - L’NPC id resta un dato “view-only” (handle) per risalire al World.
        /// </summary>
        private void EnsureNpcHoverComponents(GameObject npcGo, int npcId)
        {
            var handle = npcGo.GetComponent<MapGridNpcViewHandle>();
            if (handle == null) handle = npcGo.AddComponent<MapGridNpcViewHandle>();
            handle.NpcId = npcId;

            // Collider 2D per raycast.
            var col = npcGo.GetComponent<BoxCollider2D>();
            if (col == null) col = npcGo.AddComponent<BoxCollider2D>();

            // Dimensione collider: 1 cella.
            // (Se in futuro sprite NPC è più grande/piccolo, puoi esporre un moltiplicatore in config.)
            col.size = new Vector2(cfg.tileSizeWorld, cfg.tileSizeWorld);
            col.offset = Vector2.zero;
            col.isTrigger = true; // così non rompe eventuali collisioni fisiche future.
        }

        /// <summary>
        /// Piccola pool per evitare allocazioni in cleanup (debug-friendly).
        /// </summary>
        private static class ListPool
        {
            private static readonly Stack<List<int>> _pool = new();

            public static List<int> Get()
                => _pool.Count > 0 ? _pool.Pop() : new List<int>(64);

            public static void Release(List<int> list)
            {
                list.Clear();
                _pool.Push(list);
            }
        }

        /// <summary>
        /// Risolve in modo robusto la camera usata per la conversione World→Screen.
        /// Evita dipendenze dal tag MainCamera (Camera.main) che spesso in scene debug non è settato.
        /// Priorità:
        /// 1) Camera assegnata da Inspector (worldCamera)
        /// 2) Cache già risolta
        /// 3) Camera.main
        /// 4) Prima camera attiva in scena (preferendo orthographic)
        /// </summary>
        private Camera ResolveWorldCamera()
        {
            // 1) Inspector override
            if (worldCamera != null)
            {
                _resolvedWorldCamera = worldCamera;
                return worldCamera;
            }

            // 2) Cache
            if (_resolvedWorldCamera != null && _resolvedWorldCamera.isActiveAndEnabled)
                return _resolvedWorldCamera;

            // 3) MainCamera tag
            var main = Camera.main;
            if (main != null)
            {
                _resolvedWorldCamera = main;
                return main;
            }

            // 4) Fallback: prima camera attiva, preferendo ortografica
            var cams = Camera.allCameras;
            Camera best = null;

            for (int i = 0; i < cams.Length; i++)
            {
                var c = cams[i];
                if (c == null || !c.isActiveAndEnabled) continue;

                if (best == null) best = c;

                // Preferisci ortografica (MapGrid tipicamente è ortho)
                if (c.orthographic)
                {
                    best = c;
                    break;
                }
            }

            _resolvedWorldCamera = best;
            return best;
        }

    }
}
