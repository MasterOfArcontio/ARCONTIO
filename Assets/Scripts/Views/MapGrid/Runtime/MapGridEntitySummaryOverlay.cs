using NUnit.Framework;
using Arcontio.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridEntitySummaryOverlay:
    /// Debug overlay "schede per entità" (NPC + oggetti interagibili).
    ///
    /// IMPORTANTISSIMO (ARCONTIO):
    /// - Questo componente è SOLO view/debug. Non deve modificare logica di simulazione.
    /// - Legge dati dal World (component stores) e li rende osservabili.
    ///
    /// Feature incluse:
    /// - Card per NPC: dati base + tabella MemoryTrace + tabella memoria oggetti interagibili.
    /// - Card per oggetti interagibili (ObjectDef.IsInteractable).
    /// - Le card non stanno "sopra" l'entità: stanno vicino con un offset memorizzato.
    /// - Lineetta UI collega anchor (entità) -> bordo della card.
    /// - Drag&drop con mouse: l'utente sposta le card, offset memorizzato per sessione.
    /// - "One-shot" initial layout: quando una card nasce senza offset, cerchiamo un offset non sovrapposto.
    /// - Reset offsets: chiamato dal WorldView (es. Shift+Toggle).
    ///
    /// Nota tecnica:
    /// - Coordinate: WorldToScreenPoint -> ScreenPointToLocalPointInRectangle (Canvas overlay).
    /// - Non usiamo Camera.main come source of truth: Tick(...) riceve la camera corretta.
    /// </summary>
    public sealed class MapGridEntitySummaryOverlay
    {
        private RectTransform _linesRoot;
        private RectTransform _cardsRoot;
        private MapGridMovementExplainabilityPanelView _movementExplainabilityPanel;

        // ============================================================
        // PUBLIC API (lifecycle)
        // ============================================================

        public bool IsEnabled => _enabled;

        public void AttachTo(Transform parent)
        {
            if (_root != null) return;

            EnsureEventSystemExists();

            _root = new GameObject("MapGridEntitySummaryOverlay");
            _root.transform.SetParent(parent, false);

            _rootRt = _root.GetComponent<RectTransform>();
            if (_rootRt == null)
                _rootRt = _root.AddComponent<RectTransform>();

            _rootRt.anchorMin = new Vector2(0f, 0f);
            _rootRt.anchorMax = new Vector2(1f, 1f);
            _rootRt.pivot = new Vector2(0.5f, 0.5f);
            _rootRt.anchoredPosition = Vector2.zero;
            _rootRt.sizeDelta = Vector2.zero;
            _rootRt.offsetMin = Vector2.zero;
            _rootRt.offsetMax = Vector2.zero;

            // Canvas root (ScreenSpaceOverlay) per debug overlay.
            _canvas = _root.AddComponent<Canvas>();

            // ============================================================
            // Overlay roots
            // - Lines: sempre dietro
            // - Cards: sempre sopra (e riceve input drag)
            // ============================================================
            _linesRoot = new GameObject("Lines").AddComponent<RectTransform>();
            _linesRoot.SetParent(_root.transform, false);
            _linesRoot.anchorMin = Vector2.zero;
            _linesRoot.anchorMax = Vector2.one;
            _linesRoot.pivot = new Vector2(0.5f, 0.5f);
            _linesRoot.anchoredPosition = Vector2.zero;
            _linesRoot.sizeDelta = Vector2.zero;
            _linesRoot.offsetMin = Vector2.zero;
            _linesRoot.offsetMax = Vector2.zero;

            _cardsRoot = new GameObject("Cards").AddComponent<RectTransform>();
            _cardsRoot.SetParent(_root.transform, false);
            _cardsRoot.anchorMin = Vector2.zero;
            _cardsRoot.anchorMax = Vector2.one;
            _cardsRoot.pivot = new Vector2(0.5f, 0.5f);
            _cardsRoot.anchoredPosition = Vector2.zero;
            _cardsRoot.sizeDelta = Vector2.zero;
            _cardsRoot.offsetMin = Vector2.zero;
            _cardsRoot.offsetMax = Vector2.zero;

            // Pannello EL fisso a destra:
            // resta nello stesso Canvas del SummaryOverlay, ma non segue le card e
            // non viene collegato alle linee entity->card. E' una diagnostica laterale
            // dedicata all'NPC selezionato tramite NPCSelection.
            _movementExplainabilityPanel = new MapGridMovementExplainabilityPanelView();
            _movementExplainabilityPanel.AttachTo(_root.transform);

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999; // sopra quasi tutto

            _root.AddComponent<CanvasScaler>();
            _root.AddComponent<GraphicRaycaster>();

            _root.SetActive(false);
            _enabled = false;
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (_root != null)
                _root.SetActive(enabled);

            if (enabled)
            {
                // Quando attivi: chiediamo un layout iniziale (one-shot) per le nuove card.
                _needsInitialLayout = true;
            }
        }

        public void ClearOffsetsAndRequestRelayout()
        {
            _npcOffsets.Clear();
            _objOffsets.Clear();
            _needsInitialLayout = true;
        }

        /// <summary>
        /// Tick:
        /// chiamato dal WorldView ogni frame (o ogni tick) quando overlay è abilitato.
        /// </summary>
        public void Tick(Arcontio.Core.World world, Camera cam, float tileSizeWorld)
        {
            if (!_enabled) return;
            if (_root == null) return;
            if (world == null) return;
            if (cam == null) return;

            _lastCam = cam;
            _lastTileSizeWorld = tileSizeWorld;

            SyncNpcCards(world);
            SyncObjectCards(world);

            // Layout iniziale: solo una volta quando richiesto.
            if (_needsInitialLayout)
            {
                ApplyInitialLayoutAvoidOverlap(world, cam, tileSizeWorld);
                _needsInitialLayout = false;
            }

            UpdateNpcPositions(world, cam, tileSizeWorld);
            UpdateObjectPositions(world, cam, tileSizeWorld);

            RefreshNpcTexts(world);
            RefreshObjectTexts(world);
            RefreshMovementExplainabilityPanel(world);
        }

        // ============================================================
        // INTERNAL STATE
        // ============================================================

        private bool _enabled;

        private GameObject _root;
        private RectTransform _rootRt;
        private Canvas _canvas;

        private Camera _lastCam;
        private float _lastTileSizeWorld = 1f;

        // Card registries
        private readonly Dictionary<int, MapGridNpcSummaryCardView> _npcCards = new();
        private readonly Dictionary<int, MapGridObjectSummaryCardView> _objCards = new();

        // Line registries
        private readonly Dictionary<int, MapGridOverlayLine> _npcLines = new();
        private readonly Dictionary<int, MapGridOverlayLine> _objLines = new();

        // Offset persistence (in RAM)
        private readonly Dictionary<int, Vector2> _npcOffsets = new();
        private readonly Dictionary<int, Vector2> _objOffsets = new();

        // Remove buffers
        private readonly List<int> _npcToRemove = new(64);
        private readonly List<int> _objToRemove = new(128);

        // Text buffers (no alloc)
        private readonly StringBuilder _sbHeader = new(512);
        private readonly StringBuilder _sbMem = new(1024);
        private readonly StringBuilder _sbObjMem = new(1024);
        private readonly StringBuilder _sbComms = new(1024);
        private readonly StringBuilder _sbLandmarks = new(256);
        private readonly StringBuilder _sbExplainabilityIntentPlan = new(2048);
        private readonly StringBuilder _sbExplainabilityEvents = new(4096);
        private readonly StringBuilder _sbMbqdMemoryStore = new(1024);
        private readonly StringBuilder _sbMbqdMemoryLatest = new(1536);
        private readonly StringBuilder _sbMbqdMemoryTimeline = new(2048);
        private readonly StringBuilder _sbMbqdBeliefEntries = new(2048);
        private readonly StringBuilder _sbMbqdBeliefQuery = new(2048);
        private readonly StringBuilder _sbMbqdBeliefMutation = new(1536);
        private readonly StringBuilder _sbMbqdDecisionSelected = new(1536);
        private readonly StringBuilder _sbMbqdDecisionCandidates = new(3072);
        private readonly StringBuilder _sbMbqdDecisionBridge = new(1536);

        private readonly List<Arcontio.Core.MemoryTrace> _topMem = new(32);
        private readonly MovementExplainabilityViewModel _movementExplainabilityViewModel = new();
        private readonly MemoryBeliefDecisionExplainabilityViewModel _mbqdExplainabilityViewModel = new();

        private bool _needsInitialLayout;
        private float _lastMovementPanelRefreshRealtime = -999f;
        private int _lastMovementPanelSelectedNpcId = int.MinValue;
        private long _lastMovementPanelWorldTick = long.MinValue;

        private const float MovementPanelRefreshIntervalSeconds = 0.25f;

        // ============================================================
        // SYNC (create/destroy cards)
        // ============================================================

        private void SyncNpcCards(Arcontio.Core.World world)
        {
            _npcToRemove.Clear();

            // Remove NPC che non esistono più (NpcDna è il registro canonico).
            foreach (var kv in _npcCards)
            {
                int npcId = kv.Key;
                if (!world.NpcDna.ContainsKey(npcId))
                    _npcToRemove.Add(npcId);
            }

            for (int i = 0; i < _npcToRemove.Count; i++)
            {
                int npcId = _npcToRemove[i];

                if (_npcCards.TryGetValue(npcId, out var card))
                    card.SetVisible(false);
                _npcCards.Remove(npcId);

                if (_npcLines.TryGetValue(npcId, out var line) && line != null)
                    line.gameObject.SetActive(false);
                _npcLines.Remove(npcId);
            }

            // Add missing NPC cards
            foreach (var kv in world.NpcDna)
            {
                int npcId = kv.Key;

                if (_npcCards.ContainsKey(npcId))
                    continue;

                var card = new MapGridNpcSummaryCardView();
                //card.AttachTo(_root.transform);
                card.AttachTo(_cardsRoot);
                card.SetVisible(true);
                _npcCards[npcId] = card;

                var line = CreateLine($"NpcLine_{npcId}");
                _npcLines[npcId] = line;
                //line.SetVisible(true);
                HookDrag(card.RootRectTransform, npcId, isNpc: true, getAnchor: () => GetNpcAnchorLocal(world, npcId, _lastCam, _lastTileSizeWorld));
            }
        }

        private void SyncObjectCards(Arcontio.Core.World world)
        {
            _objToRemove.Clear();

            // Remove missing objects or objects that are no longer interactable.
            foreach (var kv in _objCards)
            {
                int objId = kv.Key;

                if (!world.Objects.TryGetValue(objId, out var inst) || inst == null)
                {
                    _objToRemove.Add(objId);
                    continue;
                }

                if (!TryGetDef(world, inst.DefId, out var def) || def == null || !def.IsInteractable)
                    _objToRemove.Add(objId);
            }

            for (int i = 0; i < _objToRemove.Count; i++)
            {
                int objId = _objToRemove[i];

                if (_objCards.TryGetValue(objId, out var card))
                    card.SetVisible(false);
                _objCards.Remove(objId);

                if (_objLines.TryGetValue(objId, out var line) && line != null)
                    line.gameObject.SetActive(false);
                _objLines.Remove(objId);
            }

            // Add missing object cards (only interactables)
            foreach (var kv in world.Objects)
            {
                int objId = kv.Key;
                var inst = kv.Value;
                if (inst == null) continue;

                if (!TryGetDef(world, inst.DefId, out var def) || def == null) continue;
                if (!def.IsInteractable) continue;

                if (_objCards.ContainsKey(objId))
                    continue;

                var card = new MapGridObjectSummaryCardView();
                //card.AttachTo(_root.transform);
                card.AttachTo(_cardsRoot);
                card.SetVisible(true);
                _objCards[objId] = card;

                var line = CreateLine($"ObjLine_{objId}");
                _objLines[objId] = line;

                HookDrag(card.RootRectTransform, objId, isNpc: false, getAnchor: () => GetObjectAnchorLocal(world, objId, _lastCam, _lastTileSizeWorld));
            }
        }

        private static bool TryGetDef(Arcontio.Core.World world, string defId, out Arcontio.Core.ObjectDef def)
        {
            if (world != null && defId != null && world.ObjectDefs.TryGetValue(defId, out def))
                return true;

            def = null;
            return false;
        }

        // ============================================================
        // LINE / DRAG HELPERS
        // ============================================================

        private MapGridOverlayLine CreateLine(string name)
        {
            var go = new GameObject(name);
            //go.transform.SetParent(_root.transform, false);
            go.transform.SetParent(_linesRoot != null ? _linesRoot : _root.transform, false);

            var rt = go.AddComponent<RectTransform>();

            // IMPORTANT: la linea è un Graphic. Se il rect è 0×0 Unity può cullare.
            // La facciamo “stretch” sull’intero canvas overlay.
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;     // con stretch, sizeDelta=0 => full size
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            //rt.SetParent(_root.transform, false); // o _linesRoot sotto _root
            //rt.anchorMin = new Vector2(0.5f, 0.5f);
            //rt.anchorMax = new Vector2(0.5f, 0.5f);
            //rt.pivot = new Vector2(0.5f, 0.5f);
            //rt.sizeDelta = Vector2.zero;

            var line = go.AddComponent<MapGridOverlayLine>();
            line.raycastTarget = false;
            line.color = new Color(1f, 1f, 1f, 0.65f);
            line.SetThickness(1f);

            // Linee dietro alle card
            //go.transform.SetAsFirstSibling();
            return line;
        }

        private void HookDrag(RectTransform cardRt, int entityId, bool isNpc, Func<Vector2> getAnchor)
        {
            if (cardRt == null) return;

            var drag = cardRt.gameObject.GetComponent<MapGridDraggableCard>();
            if (drag == null)
                drag = cardRt.gameObject.AddComponent<MapGridDraggableCard>();

            drag.Init(_rootRt,
                getAnchorLocal: getAnchor,
                onDragged: (newPos, anchor) =>
                {
                    Vector2 offset = newPos - anchor;
                    if (isNpc) _npcOffsets[entityId] = offset;
                    else _objOffsets[entityId] = offset;
                });
        }

        // ============================================================
        // POSITIONS
        // ============================================================

        private void UpdateNpcPositions(Arcontio.Core.World world, Camera cam, float tileSizeWorld)
        {
            foreach (var kv in world.GridPos)
            {
                int npcId = kv.Key;

                if (!_npcCards.TryGetValue(npcId, out var card))
                    continue;

                if (!_npcLines.TryGetValue(npcId, out var line))
                    continue;

                Vector2 anchor = GetNpcAnchorLocal(world, npcId, cam, tileSizeWorld);
                if (float.IsNaN(anchor.x))
                {
                    card.SetVisible(false);
                    line.SetVisible(false);
                    continue;
                }

                card.SetVisible(true);
                line.gameObject.SetActive(true);
                var endOnCard = GetClosestPointOnCardBorder(card.RootRectTransform, anchor);
                line.SetEndpoints(anchor, endOnCard);
                line.SetVisible(true);

                Vector2 offset = GetOrCreateOffset(_npcOffsets, npcId, DefaultNpcOffset(npcId));
                Vector2 cardPos = anchor + offset;

                // Se in drag: non sovrascrivere, ma mantieni offset coerente con anchor.
                var rt = card.RootRectTransform;
                var drag = rt != null ? rt.GetComponent<MapGridDraggableCard>() : null;

                if (drag != null && drag.IsDragging)
                {
                    Vector2 current = card.GetCanvasLocalPosition();
                    _npcOffsets[npcId] = current - anchor;
                    cardPos = current;
                }
                else
                {
                    card.SetCanvasLocalPosition(cardPos);
                }
            }
        }

        private void UpdateObjectPositions(Arcontio.Core.World world, Camera cam, float tileSizeWorld)
        {
            foreach (var kv in world.Objects)
            {
                int objId = kv.Key;
                var inst = kv.Value;
                if (inst == null) continue;

                if (!TryGetDef(world, inst.DefId, out var def) || def == null || !def.IsInteractable)
                    continue;

                if (!_objCards.TryGetValue(objId, out var card))
                    continue;

                if (!_objLines.TryGetValue(objId, out var line))
                    continue;

                Vector2 anchor = GetObjectAnchorLocal(world, objId, cam, tileSizeWorld);
                if (float.IsNaN(anchor.x))
                {
                    card.SetVisible(false);
                    line.SetVisible(false);
                    continue;
                }

                card.SetVisible(true);
                line.gameObject.SetActive(true);

                var endOnCard = GetClosestPointOnCardBorder(card.RootRectTransform, anchor);
                line.SetEndpoints(anchor, endOnCard);

                line.SetVisible(true);

                Vector2 offset = GetOrCreateOffset(_objOffsets, objId, DefaultObjectOffset(objId));
                Vector2 cardPos = anchor + offset;

                var rt = card.RootRectTransform;
                var drag = rt != null ? rt.GetComponent<MapGridDraggableCard>() : null;

                if (drag != null && drag.IsDragging)
                {
                    Vector2 current = card.GetCanvasLocalPosition();
                    _objOffsets[objId] = current - anchor;
                    cardPos = current;
                }
                else
                {
                    card.SetCanvasLocalPosition(cardPos);
                }
            }
        }

        private static Vector2 GetOrCreateOffset(Dictionary<int, Vector2> dict, int id, Vector2 defaultOffset)
        {
            if (dict.TryGetValue(id, out var off))
                return off;

            dict[id] = defaultOffset;
            return defaultOffset;
        }

        private static Vector2 DefaultNpcOffset(int npcId)
        {
            float x = (npcId % 2 == 0) ? 160f : -160f;
            float y = 60f + ((npcId * 17) % 60);
            return new Vector2(x, y);
        }

        private static Vector2 DefaultObjectOffset(int objId)
        {
            float x = (objId % 2 == 0) ? 140f : -140f;
            float y = -40f - ((objId * 13) % 40);
            return new Vector2(x, y);
        }

        // ============================================================
        // ANCHOR COMPUTATION
        // ============================================================

        private Vector2 GetNpcAnchorLocal(Arcontio.Core.World world, int npcId, Camera cam, float tileSizeWorld)
        {
            if (!world.GridPos.TryGetValue(npcId, out var pos))
                return new Vector2(float.NaN, float.NaN);

            var wp = new Vector3((pos.X + 0.5f) * tileSizeWorld, (pos.Y + 0.5f) * tileSizeWorld, 0f);
            var sp = cam.WorldToScreenPoint(wp);

            if (sp.z < 0f)
                return new Vector2(float.NaN, float.NaN);

            var screen = new Vector2(sp.x, sp.y - 10f);
            return ScreenToCanvasLocal(screen);
        }

        private Vector2 GetObjectAnchorLocal(Arcontio.Core.World world, int objId, Camera cam, float tileSizeWorld)
        {
            if (!world.Objects.TryGetValue(objId, out var inst) || inst == null)
                return new Vector2(float.NaN, float.NaN);

            var wp = new Vector3((inst.CellX + 0.5f) * tileSizeWorld, (inst.CellY + 0.5f) * tileSizeWorld, 0f);
            var sp = cam.WorldToScreenPoint(wp);

            if (sp.z < 0f)
                return new Vector2(float.NaN, float.NaN);

            var screen = new Vector2(sp.x, sp.y - 10f);
            return ScreenToCanvasLocal(screen);
        }

        private Vector2 ScreenToCanvasLocal(Vector2 screen)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootRt, screen, null, out var local))
                return local;

            return Vector2.zero;
        }

        // ============================================================
        // LINE ENDPOINT: snap to closest point on card border
        // ============================================================

        /// <summary>
        /// Calcola un endpoint sulla cornice della card.
        ///
        /// Input:
        /// - cardRt: RectTransform della card
        /// - fromLocal: punto di partenza della linea in coordinate canvas-local (anchor entità)
        ///
        /// Output:
        /// - punto sulla cornice (canvas-local)
        ///
        /// Nota:
        /// - È una versione volutamente semplice e deterministica:
        ///   prendiamo il centro della card e clamping sul suo rect.
        /// </summary>
        private static Vector2 GetClosestPointOnCardBorder(RectTransform cardRt, Vector2 fromLocal)
        {
            if (cardRt == null) return fromLocal;

            Vector2 center = cardRt.anchoredPosition;

            // Rettangolo in coordinate locali della card (centrato sul pivot della cardRt).
            Rect r = cardRt.rect;

            // Trasformiamo "from" nel local space della card.
            // In pratica: vettore dal centro card al punto from.
            Vector2 dir = fromLocal - center;

            // Se dir è zero, ritorna centro.
            if (dir.sqrMagnitude < 0.0001f)
                return center;

            // Calcolo fattori di scala per intersecare con lati.
            // Lavoriamo sul rect in spazio card (pivot).
            float halfW = r.width * 0.5f;
            float halfH = r.height * 0.5f;

            // Evita divisione per zero.
            float dx = Mathf.Abs(dir.x) < 0.0001f ? 0.0001f : dir.x;
            float dy = Mathf.Abs(dir.y) < 0.0001f ? 0.0001f : dir.y;

            float tx = halfW / Mathf.Abs(dx);
            float ty = halfH / Mathf.Abs(dy);

            // Prendi il minimo: primo impatto col bordo.
            float t = Mathf.Min(tx, ty);

            Vector2 localOnBorder = new Vector2(dir.x * t, dir.y * t);

            // Riporta in canvas-local.
            return center + localOnBorder;
        }

        // ============================================================
        // TEXT (format) - usa i component stores reali del tuo World
        // ============================================================

        private void RefreshNpcTexts(Arcontio.Core.World world)
        {
            long nowTickL = Arcontio.Core.TickContext.CurrentTickIndex;
            int nowTick = (nowTickL > int.MaxValue) ? int.MaxValue : (int)nowTickL;

            foreach (var kv in _npcCards)
            {
                int npcId = kv.Key;
                var card = kv.Value;

                // Presence
                if (!world.NpcDna.TryGetValue(npcId, out var dna))
                    continue;

                world.GridPos.TryGetValue(npcId, out var pos);
                world.NpcFacing.TryGetValue(npcId, out var facing);
                world.Needs.TryGetValue(npcId, out var needs);

                // ============================================================
                // HEADER / IDENTITY / STATE
                // ============================================================
                string navModeHeader = "IDLE";
                string execPhaseHeader = "NONE";
                if (world.TryGetNpcMacroRouteDebugReport(npcId, out var routeReportHeader))
                {
                    navModeHeader = string.IsNullOrWhiteSpace(routeReportHeader.NavigationMode) ? "IDLE" : routeReportHeader.NavigationMode;
                    execPhaseHeader = routeReportHeader.ExecutionActive
                        ? "EXECUTING"
                        : (!string.IsNullOrEmpty(routeReportHeader.ExecutionFailureReason) ? "FAILED" : "NONE");
                }

                string actionKindHeader = "Unknown";
                if (world.TryGetNpcAction(npcId, out var actionHeader))
                    actionKindHeader = actionHeader.Kind.ToString();

                _sbHeader.Clear();
                _sbHeader.Append("NPC #").Append(npcId);
                if (!string.IsNullOrEmpty(dna.Identity.Name))
                    _sbHeader.Append("  ").Append(dna.Identity.Name);

                // Feedback UI della selezione debug:
                // NPCSelection vive nello strato view condiviso e non nel core
                // simulativo. Qui lo mostriamo soltanto per rendere verificabile
                // il click sinistro introdotto nella sessione k.
                bool isSelectedNpc = SocialViewer.UI.NPCSelection.SelectedNpcId == npcId;

                _sbHeader.Append('\n')
                    .Append("Selected = ").Append(isSelectedNpc ? "YES" : "NO")
                    .Append('\n')
                    .Append("Pos = (").Append(pos.X).Append(',').Append(pos.Y).Append(")")
                    .Append("   Facing = ").Append(facing)
                    .Append('\n')
                    .Append("State = ").Append(actionKindHeader)
                    .Append("   NavMode = ").Append(navModeHeader)
                    .Append("   Exec = ").Append(execPhaseHeader)
                    .Append('\n')
                    .Append("Needs: hunger=").Append(needs.GetValue(NeedKind.Hunger).ToString("0.00"))
                    .Append("   rest=").Append(needs.GetValue(NeedKind.Rest).ToString("0.00"));

                // ============================================================
                // LANDMARK DEBUG REPORT (v0.02.03 / Day3)
                // ============================================================
                // Prima (Day2) questi contatori erano "appesi" all'header.
                // Ora li spostiamo in una sezione dedicata nella card, per:
                // - maggiore leggibilità
                // - supportare il collapse/expand per gruppo
                _sbLandmarks.Clear();
                _sbLandmarks.Append("Navigation / MacroRoute\n");

                if (world.TryGetNpcLandmarkDebugReport(npcId, out var lmReport))
                {
                    _sbLandmarks.Append("[NAV]\n");

                    if (world.TryGetNpcMacroRouteDebugReport(npcId, out var routeReport))
                    {
                        string execPhase;
                        if (routeReport.ExecutionActive)
                            execPhase = "EXECUTING";
                        else if (!string.IsNullOrEmpty(routeReport.ExecutionFailureReason))
                            execPhase = "FAILED";
                        else if (string.Equals(routeReport.LastModeSwitchReason, "MoveIntentCompleted", System.StringComparison.Ordinal) || string.Equals(routeReport.LastModeSwitchReason, "LocalSearchCompletedTargetReached", System.StringComparison.Ordinal))
                            execPhase = "COMPLETED";
                        else
                            execPhase = "NONE";

                        _sbLandmarks.Append("NavMode = ").Append(routeReport.NavigationMode).Append('\n')
                            .Append("ExecActive = ").Append(routeReport.ExecutionActive ? "YES" : "NO").Append('\n')
                            .Append("ExecPhase = ").Append(execPhase).Append('\n')
                            .Append("TargetCell = (").Append(routeReport.TargetCellX).Append(',').Append(routeReport.TargetCellY).Append(')').Append('\n')
                            .Append("ImmediateTarget = (").Append(routeReport.ImmediateTargetX).Append(',').Append(routeReport.ImmediateTargetY).Append(')').Append('\n');

                        if (routeReport.LastModeSwitchTick >= 0)
                            _sbLandmarks.Append("ModeSwitchTick = ").Append(routeReport.LastModeSwitchTick).Append('\n');
                        if (!string.IsNullOrEmpty(routeReport.LastModeSwitchReason))
                            _sbLandmarks.Append("ModeSwitchWhy = ").Append(routeReport.LastModeSwitchReason).Append('\n');

                        _sbLandmarks.Append('\n').Append("[LM PLAN]\n")
                            .Append("MacroRoute = ").Append(routeReport.HasRoute ? "OK" : "FAIL").Append('\n')
                            .Append("RouteNodes = ").Append(routeReport.RouteNodeCount).Append('\n')
                            .Append("StartLM = ").Append(routeReport.StartNodeId).Append('\n')
                            .Append("TargetLM = ").Append(routeReport.TargetNodeId).Append('\n')
                            .Append("NextLMIndex = ").Append(routeReport.NextRouteNodeIndex).Append('\n')
                            .Append("NextLM = ").Append(routeReport.NextRouteNodeId).Append('\n')
                            .Append("LastMileFlag = ").Append(routeReport.IsDoingLastMile ? "YES" : "NO").Append('\n')
                            .Append("GoalLocalSearch = ").Append(routeReport.GoalLocalSearchActive ? "ON" : "OFF")
                            .Append("   Budget = ").Append(routeReport.GoalLocalSearchBudgetRemaining);

                        if (!string.IsNullOrEmpty(routeReport.FailureReason))
                            _sbLandmarks.Append('\n').Append("RouteFail = ").Append(routeReport.FailureReason);
                        if (!string.IsNullOrEmpty(routeReport.ExecutionFailureReason))
                            _sbLandmarks.Append('\n').Append("ExecFail = ").Append(routeReport.ExecutionFailureReason);
                    }

                    _sbLandmarks.Append('\n').Append('\n').Append("[LM KNOWLEDGE]\n")
                        .Append("KnownLandmarks = ").Append(lmReport.KnownLandmarksCount).Append('\n')
                        .Append("KnownEdges = ").Append(lmReport.KnownEdgesCount).Append('\n')
                        .Append("PoiAnchors = ").Append(lmReport.PoiAnchorCount).Append('\n')
                        .Append("Replans/min = ").Append(lmReport.ReplansPerMin.ToString("0.0")).Append('\n')
                        .Append("Failures/min = ").Append(lmReport.FailuresPerMin.ToString("0.0")).Append('\n')
                        .Append("Blacklist = ").Append(lmReport.BlacklistSize);
                }
                else
                {
                    _sbLandmarks.Append("<color=#aaaaaa>(no landmark report)</color>");
                }

                // ============================================================
                // AZIONE + COLORE (rich text)
                // ============================================================
                // Requisito:
                // - mostrare l'azione in colore diverso nella card.
                //
                // Nota architetturale:
                // - l'azione NON viene inferita dalla view.
                // - viene letta da world.NpcAction (NpcActionState).
                string actionRich = string.Empty;
                if (world.TryGetNpcAction(npcId, out var action))
                {
                    int age = nowTick - action.StartedTick;
                    if (age < 0) age = 0;

                    string hex = action.Kind switch
                    {
                        Arcontio.Core.NpcActionKind.Idle => "AAAAAA",
                        Arcontio.Core.NpcActionKind.MoveTo => "66CCFF",
                        Arcontio.Core.NpcActionKind.Scan => "FFD966",
                        Arcontio.Core.NpcActionKind.Eat => "66FF66",
                        Arcontio.Core.NpcActionKind.Sleep => "B4A7D6",
                        Arcontio.Core.NpcActionKind.Steal => "FF6666",
                        Arcontio.Core.NpcActionKind.Work => "9FC5E8",
                        Arcontio.Core.NpcActionKind.Social => "F6B26B",
                        Arcontio.Core.NpcActionKind.Combat => "FF0000",
                        _ => "FFFFFF"
                    };

                    string moveIntentState = "NO";
                    string moveReason = "None";
                    int moveTargetX = 0;
                    int moveTargetY = 0;
                    int moveTargetObjectId = 0;
                    int blockedTicks = 0;
                    if (world.NpcMoveIntents.TryGetValue(npcId, out var mi) && mi.Active)
                    {
                        moveIntentState = "YES";
                        moveReason = mi.Reason.ToString();
                        moveTargetX = mi.TargetX;
                        moveTargetY = mi.TargetY;
                        moveTargetObjectId = mi.TargetObjectId;
                        blockedTicks = mi.BlockedTicks;
                    }

                    int visibleCommunityFood = 0;
                    int rememberedCommunityFood = 0;
                    string needTargetSource = "None";
                    string needTargetReason = "NoKnownFoodCandidate";
                    string goalSource = "None";
                    ComputeFoodTargetDebug(world, npcId, out visibleCommunityFood, out rememberedCommunityFood);
                    if (visibleCommunityFood != 0)
                    {
                        needTargetSource = "Visible";
                        needTargetReason = "VisibleCommunityFood";
                    }
                    else if (rememberedCommunityFood != 0)
                    {
                        needTargetSource = "KnownObject";
                        needTargetReason = "RememberedCommunityFood";
                    }

                    if (moveIntentState == "YES")
                    {
                        switch (moveReason)
                        {
                            case "SeekFood": goalSource = "Hunger"; break;
                            case "SeekBed": goalSource = "Fatigue"; break;
                            case "DebugClick": goalSource = "PlayerOrder"; break;
                            default: goalSource = "AI"; break;
                        }
                    }

                    if (moveReason == "DebugClick")
                    {
                        needTargetSource = "Suppressed";
                        needTargetReason = "SuppressedByManualControl";
                    }

                    var sbGoal = new StringBuilder(256);
                    sbGoal.Append("Action = <color=#").Append(hex).Append('>').Append(action.Kind).Append("</color>\n")
                        .Append("ActionLabel = ").Append(action).Append("   age=").Append(age).Append("t\n")
                        .Append("MoveIntent = ").Append(moveIntentState).Append("   Reason = ").Append(moveReason).Append('\n');

                    if (moveIntentState == "YES")
                        sbGoal.Append("TargetCell = (").Append(moveTargetX).Append(',').Append(moveTargetY).Append(")   TargetObject = ").Append(moveTargetObjectId).Append("   BlockedTicks = ").Append(blockedTicks).Append('\n');
                    else if (action.HasTargetCell)
                        sbGoal.Append("TargetCell = (").Append(action.TargetX).Append(',').Append(action.TargetY).Append(")\n");

                    if (action.TargetObjectId != 0)
                        sbGoal.Append("ActionTargetObject = ").Append(action.TargetObjectId).Append('\n');

                    sbGoal.Append("NeedTargetSource = ").Append(needTargetSource).Append('\n')
                        .Append("NeedTargetReason = ").Append(needTargetReason);

                    actionRich = sbGoal.ToString();
                }
                else
                {
                    actionRich = "Action = <color=#AAAAAA>Unknown</color>";
                }

                // ============================================================
                // INVENTORY / CIBO TRASPORTATO
                // ============================================================
                // Requisito:
                // - aggiungere un blocco info nella card con il cibo trasportato.
                //
                // Fonte di verità:
                // - world.NpcPrivateFood (inventario v0)
                //
                // Nota:
                // - Se in futuro aggiungerai un Inventory component reale, sposteremo qui la lettura.
                int carriedFood = 0;
                if (world.NpcPrivateFood != null && world.NpcPrivateFood.TryGetValue(npcId, out var pf))
                    carriedFood = pf;

                // IMPORTANT (Day10/Day11 UX):
                // In ARCONTIO oggi esistono DUE "forme" di cibo privato:
                // 1) "Addosso" (inventario v0): World.NpcPrivateFood[npcId]
                // 2) "A terra ma di proprietà": FoodStockComponent con OwnerKind=Npc e OwnerId=npcId
                //
                // L'utente vede la proprietà dagli overlay numerici sulle pile, quindi
                // qui dobbiamo mostrare entrambe le quantità, altrimenti la card sembra "buggata".
                int ownedStockUnits = 0;
                int ownedStockPiles = 0;
                if (world.FoodStocks != null)
                {
                    foreach (var sKv in world.FoodStocks)
                    {
                        var s = sKv.Value;
                        if (s.OwnerKind == Arcontio.Core.OwnerKind.Npc && s.OwnerId == npcId && s.Units > 0)
                        {
                            ownedStockUnits += s.Units;
                            ownedStockPiles++;
                        }
                    }
                }

                long lastEatTick = -1;
                if (world.NpcLastPrivateFoodConsumeTick != null && world.NpcLastPrivateFoodConsumeTick.TryGetValue(npcId, out var lt))
                    lastEatTick = lt;

                // UX:
                // - "Carried" = cibo in inventario (spendibile subito).
                // - "Owned in world" = scorte a terra di proprietà (possono essere rubate da altri).
                // - "Total" = somma, utile per ragionare sullo stato generale.
                int totalOwned = carriedFood + ownedStockUnits;

                int visibleCommunityFoodDbg = 0;
                int rememberedCommunityFoodDbg = 0;
                ComputeFoodTargetDebug(world, npcId, out visibleCommunityFoodDbg, out rememberedCommunityFoodDbg);

                // [NEEDS] rimosso: ora visualizzato come barre nella sezione "Needs" della card
                var sb = new StringBuilder(512);
                sb.AppendLine("[INVENTORY]")
                  .AppendLine($"PrivateFood (carried) = {carriedFood}")
                  .AppendLine($"PrivateFood (owned in world) = {ownedStockUnits}  (piles={ownedStockPiles})")
                  .AppendLine($"Total private food = {totalOwned}");

                if (lastEatTick >= 0)
                    sb.AppendLine($"Last private consume tick = {lastEatTick}");

                sb.AppendLine()
                  .AppendLine("[FOOD KNOWLEDGE]")
                  .AppendLine($"Visible community food = {(visibleCommunityFoodDbg != 0 ? visibleCommunityFoodDbg.ToString() : "None")}")
                  .AppendLine($"Remembered community food = {(rememberedCommunityFoodDbg != 0 ? rememberedCommunityFoodDbg.ToString() : "None")}");

                string invText = sb.ToString();

                // ============================================================
                // COMUNICAZIONI SIMBOLICHE (Patch 0.01P2)
                // ============================================================
                // Requisito:
                // - Nelle card NPC vogliamo vedere le "tracce simboliche" comunicate.
                // - Distinzione chiara tra:
                //   OUT: token pronunciati dall'NPC
                //   IN:  token sentiti/ricevuti dall'NPC
                //
                // Fonte di verità:
                // - world.DebugNpcTokens (cache view-only)
                // - popolata dal core in TokenEmissionPipeline (OUT) e TokenDeliveryPipeline (IN).
                //
                // Nota UX:
                // - Mostriamo poche righe per non rendere la card illeggibile.
                // - Usiamo RichText per colorare le direzioni.
                _sbComms.Clear();
                _sbComms.Append("Comms (tokens)\n");

                if (world.DebugNpcTokens != null && world.DebugNpcTokens.TryGetValue(npcId, out var tlog) && tlog != null)
                {
                    // Mostriamo gli ultimi N per direzione (N piccolo).
                    const int MaxPerDir = 4;

                    // OUT (gli ultimi)
                    var outList = tlog.Outgoing;
                    int outCount = outList != null ? outList.Count : 0;
                    _sbComms.Append("<color=#FFAA66>OUT</color>: ");
                    if (outCount == 0)
                    {
                        _sbComms.Append("<color=#aaaaaa>(none)</color>\n");
                    }
                    else
                    {
                        _sbComms.Append('\n');
                        int start = Mathf.Max(0, outCount - MaxPerDir);
                        for (int oi = start; oi < outCount; oi++)
                        {
                            var env = outList[oi];
                            var tok = env.Token;

                            // Formato verbose ma compatto:
                            // - canale
                            // - destinatario
                            // - tipo token + subject
                            // - eventuale cell
                            _sbComms.Append("  → ")
                                .Append(env.ListenerId)
                                .Append("  ch=").Append(env.Channel)
                                .Append("  ")
                                .Append(tok.Type)
                                .Append(" subj=").Append(tok.SubjectId);

                            // Patch 0.01P3 extension:
                            // Mostriamo anche il soggetto secondario se presente.
                            // Esempio utile: TheftReportWitness subj=thief sec=victim
                            if (tok.SecondarySubjectId >= 0)
                                _sbComms.Append(" sec=").Append(tok.SecondarySubjectId);

                            if (tok.HasCell)
                                _sbComms.Append(" cell=(").Append(tok.CellX).Append(',').Append(tok.CellY).Append(')');

                            _sbComms.Append("  I=").Append(tok.Intensity01.ToString("0.00"))
                                .Append(" R=").Append(tok.Reliability01.ToString("0.00"))
                                .Append(" d=").Append(tok.ChainDepth)
                                .Append('\n');
                        }
                    }

                    // IN (gli ultimi)
                    var inList = tlog.Incoming;
                    int inCount = inList != null ? inList.Count : 0;
                    _sbComms.Append("<color=#66CCFF>IN</color>: ");
                    if (inCount == 0)
                    {
                        _sbComms.Append("<color=#aaaaaa>(none)</color>\n");
                    }
                    else
                    {
                        _sbComms.Append('\n');
                        int start = Mathf.Max(0, inCount - MaxPerDir);
                        for (int ii = start; ii < inCount; ii++)
                        {
                            var env = inList[ii];
                            var tok = env.Token;

                            _sbComms.Append("  ← ")
                                .Append(env.SpeakerId)
                                .Append("  ch=").Append(env.Channel)
                                .Append("  ")
                                .Append(tok.Type)
                                .Append(" subj=").Append(tok.SubjectId);

                            // Patch 0.01P3 extension: soggetto secondario (se presente)
                            if (tok.SecondarySubjectId >= 0)
                                _sbComms.Append(" sec=").Append(tok.SecondarySubjectId);

                            if (tok.HasCell)
                                _sbComms.Append(" cell=(").Append(tok.CellX).Append(',').Append(tok.CellY).Append(')');

                            _sbComms.Append("  I=").Append(tok.Intensity01.ToString("0.00"))
                                .Append(" R=").Append(tok.Reliability01.ToString("0.00"))
                                .Append(" d=").Append(tok.ChainDepth)
                                .Append('\n');
                        }
                    }
                }
                else
                {
                    // Caso più comune nei branch vecchi o quando la feature non è stata inizializzata:
                    // non deve fare crash, semplicemente mostra "n/a".
                    _sbComms.Append("<color=#aaaaaa>(no token log)</color>\n");
                }

                // Memory traces
                _sbMem.Clear();
                _sbMem.Append("Memory traces:\n");

                if (world.Memory.TryGetValue(npcId, out var mem) && mem != null)
                {
                    // Per debug: prendiamo le top 16 per non spam UI.
                    mem.GetTopTraces(16, _topMem);

                    for (int i = 0; i < _topMem.Count; i++)
                    {
                        var t = _topMem[i];
                        _sbMem.Append("- ").Append(t.Type)
                            .Append("  I=").Append(t.Intensity01.ToString("0.00"))
                            .Append("  R=").Append(t.Reliability01.ToString("0.00"))
                            .Append("  cell=(").Append(t.CellX).Append(",").Append(t.CellY).Append(")")
                            .Append('\n');
                    }
                }

                // Known entities (NpcObjectMemoryStore.Slots)
                _sbObjMem.Clear();
                _sbObjMem.Append("Perception / Knowledge\n");

                if (world.NpcMoveIntents.TryGetValue(npcId, out var dbgMi) && dbgMi.Active)
                {
                    _sbObjMem.Append("MoveIntentReason = ").Append(dbgMi.Reason).Append("   TargetObject = ").Append(dbgMi.TargetObjectId).Append("\n");
                }

                if (world.NpcPinnedFoodStockBeliefs.TryGetValue(npcId, out var pinnedBeliefs) && pinnedBeliefs != null && pinnedBeliefs.Count > 0)
                {
                    _sbObjMem.Append("PinnedFoodBeliefs = ").Append(pinnedBeliefs.Count).Append("\n");
                }

                if (world.NpcObjectMemory.TryGetValue(npcId, out var store) && store != null)
                {
                    var slots = store.Slots;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        var e = slots[i];
                        if (!e.IsValid) continue;

                        // ============================================================
                        // Nota UI (molto verbosa ma utile in debug):
                        // Questo overlay serve per "vedere" cosa l'NPC crede di sapere.
                        // NON è la verità globale del mondo: è la cache soggettiva.
                        //
                        // Da Step3 in poi, lo store è generalizzato:
                        // - Kind=WorldObject  -> oggetti interagibili (stock/letto/porta...)
                        // - Kind=Npc          -> NPC osservati (posizione + facts osservati)
                        // ============================================================

                        if (e.Kind == NpcObjectMemoryStore.SubjectKind.Npc)
                        {
                            _sbObjMem.Append("- NPC #").Append(e.SubjectId)
                                .Append(" cell=(").Append(e.CellX).Append(",").Append(e.CellY).Append(")");

                            if ((e.Flags & NpcObjectMemoryStore.ObservedFlags.HasCarriedFood) != 0)
                                _sbObjMem.Append(" carriedFood~").Append(e.CarriedFoodUnitsApprox);

                            _sbObjMem.Append(" rel=").Append(e.Reliability01.ToString("0.00"))
                                .Append(" util=").Append(e.UtilityScore01.ToString("0.00"))
                                .Append(" seen=").Append(e.LastSeenTick)
                                .Append('\n');
                        }
                        else
                        {
                            // Default: WorldObject (compat con Day10)
                            _sbObjMem.Append("- ").Append(e.DefId);

                            if (e.ObjectId != 0) _sbObjMem.Append(" #").Append(e.ObjectId);

                            _sbObjMem.Append(" cell=(").Append(e.CellX).Append(",").Append(e.CellY).Append(")")
                                .Append(" rel=").Append(e.Reliability01.ToString("0.00"))
                                .Append(" util=").Append(e.UtilityScore01.ToString("0.00"))
                                .Append(" seen=").Append(e.LastSeenTick)
                                .Append('\n');
                        }
                    }
                }

                card.SetTexts(_sbHeader.ToString(), _sbMem.ToString(), _sbObjMem.ToString());

                // Sezioni extra (action + inventory) introdotte in MapGridNpcSummaryCardView.
                // Nota difensiva:
                // - la card potrebbe essere vecchia (se in qualche branch non aggiorni i file),
                //   quindi usiamo chiamate dirette ma lasciamo che i null-check dentro la view gestiscano.
                card.SetActionText(actionRich);
                card.SetInventoryText(invText);
                card.SetCommsText(_sbComms.ToString());

                // Patch 0.02.03: landmark/edge conosciuti.
                card.SetLandmarksText(_sbLandmarks.ToString());

                // Bisogni (v0.04.13) — barre invertite (piena=ok, vuota=critico).
                // Sessione 13:
                //   l'overlay riusa la procedura già esistente della card. La card legge
                //   tutti i NeedKind via NeedKind.COUNT e visualizza anche i flag critici
                //   già presenti in NpcNeeds; questo controller deve solo passare lo stato.
                card.UpdateNeedsBars(needs);

                // DNA DRIFT (v0.04.07.b) — barre proporzionali
                if (world.NpcProfiles.TryGetValue(npcId, out var profile))
                {
                    var driftResult = NpcDnaDistance.Compute(dna, profile);
                    card.UpdateDnaDrift(dna, profile, driftResult);
                }
            }
        }

        // =============================================================================
        // RefreshMovementExplainabilityPanel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna il pannello EL fisso a destra usando l'NPC selezionato nella view.
        /// Il pannello non crea selezione propria: segue <see cref="SocialViewer.UI.NPCSelection"/>.
        /// </para>
        ///
        /// <para><b>Debug panel separato dalla simulazione</b></para>
        /// <para>
        /// Questo metodo vive nello strato view/debug e legge lo stesso ViewModel EL
        /// usato dalla card NPC. Non modifica intent, pathfinding o registry: produce
        /// soltanto testo per il pannello laterale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>SelectedNpcId</b>: sorgente view-only dell'NPC attivo.</item>
        ///   <item><b>_sbExplainabilityIntentPlan</b>: buffer dedicato al pannellino Intent/Plan.</item>
        ///   <item><b>_sbExplainabilityEvents</b>: buffer dedicato all'area eventi espandibile.</item>
        ///   <item><b>BuildMovementExplainabilityText</b>: formatter dedicato ai blocchi del pannello destro.</item>
        /// </list>
        /// </summary>
        private void RefreshMovementExplainabilityPanel(Arcontio.Core.World world)
        {
            if (_movementExplainabilityPanel == null)
                return;

            int selectedNpcId = SocialViewer.UI.NPCSelection.SelectedNpcId;
            long worldTick = world != null ? Arcontio.Core.TickContext.CurrentTickIndex : -1;
            bool selectionChanged = selectedNpcId != _lastMovementPanelSelectedNpcId;
            bool tickChanged = worldTick != _lastMovementPanelWorldTick;
            bool refreshWindowExpired = Time.unscaledTime - _lastMovementPanelRefreshRealtime >= MovementPanelRefreshIntervalSeconds;

            if (!selectionChanged && (!tickChanged || !refreshWindowExpired))
                return;

            _lastMovementPanelSelectedNpcId = selectedNpcId;
            _lastMovementPanelWorldTick = worldTick;
            _lastMovementPanelRefreshRealtime = Time.unscaledTime;

            if (selectedNpcId <= 0)
            {
                _movementExplainabilityPanel.SetHeader("Explainability Layer", string.Empty);
                _movementExplainabilityPanel.SetDiagnostics("selectedNpc=-1 | nessuna selezione");
                _movementExplainabilityPanel.SetMemoryText("<color=#6E7681>Nessun NPC selezionato.</color>", string.Empty, string.Empty);
                _movementExplainabilityPanel.SetBeliefText("<color=#6E7681>Nessun NPC selezionato.</color>", string.Empty, string.Empty);
                _movementExplainabilityPanel.SetDecisionText("<color=#6E7681>Nessun NPC selezionato.</color>", string.Empty, string.Empty);
                _movementExplainabilityPanel.SetPathfindingText(
                    "<color=#6E7681>Nessun NPC selezionato.</color>",
                    "<color=#6E7681>Click sinistro su un NPC nella MapGrid per aprire la diagnostica EL.</color>");
                _movementExplainabilityPanel.SetVisible(true);
                return;
            }

            if (world == null || !world.NpcDna.ContainsKey(selectedNpcId))
            {
                _movementExplainabilityPanel.SetHeader("Explainability Layer", $"NPC #{selectedNpcId}");
                _movementExplainabilityPanel.SetDiagnostics($"selectedNpc={selectedNpcId} | world/npc non valido");
                _movementExplainabilityPanel.SetMemoryText("<color=#F85149>L'NPC selezionato non esiste piu' nel World.</color>", string.Empty, string.Empty);
                _movementExplainabilityPanel.SetBeliefText("<color=#F85149>L'NPC selezionato non esiste piu' nel World.</color>", string.Empty, string.Empty);
                _movementExplainabilityPanel.SetDecisionText("<color=#F85149>L'NPC selezionato non esiste piu' nel World.</color>", string.Empty, string.Empty);
                _movementExplainabilityPanel.SetPathfindingText(string.Empty, "<color=#F85149>L'NPC selezionato non esiste piu' nel World.</color>");
                _movementExplainabilityPanel.SetVisible(true);
                return;
            }

            string headerMeta = BuildMovementExplainabilityText(world, selectedNpcId, _sbExplainabilityIntentPlan, _sbExplainabilityEvents, maxEvents: 32);
            string mbqdHeaderMeta = BuildMemoryBeliefDecisionExplainabilityText(world, selectedNpcId);
            _movementExplainabilityPanel.SetHeader($"Explainability Layer - NPC #{selectedNpcId}", string.IsNullOrWhiteSpace(mbqdHeaderMeta) ? headerMeta : mbqdHeaderMeta);
            _movementExplainabilityPanel.SetDiagnostics(
                $"selectedNpc={selectedNpcId} | worldTick={worldTick} | registryNpc={_mbqdExplainabilityViewModel.HasNpc} | MBQD M{_mbqdExplainabilityViewModel.MemoryCount} B{_mbqdExplainabilityViewModel.BeliefCount} Q{_mbqdExplainabilityViewModel.QueryCount} D{_mbqdExplainabilityViewModel.DecisionCount}");
            _movementExplainabilityPanel.SetPathfindingText(_sbExplainabilityIntentPlan.ToString(), _sbExplainabilityEvents.ToString());
            _movementExplainabilityPanel.SetMemoryText(_sbMbqdMemoryStore.ToString(), _sbMbqdMemoryLatest.ToString(), _sbMbqdMemoryTimeline.ToString());
            _movementExplainabilityPanel.SetBeliefText(_sbMbqdBeliefEntries.ToString(), _sbMbqdBeliefQuery.ToString(), _sbMbqdBeliefMutation.ToString());
            _movementExplainabilityPanel.SetDecisionText(_sbMbqdDecisionSelected.ToString(), _sbMbqdDecisionCandidates.ToString(), _sbMbqdDecisionBridge.ToString());
            _movementExplainabilityPanel.SetVisible(true);
        }

        // =============================================================================
        // BuildMemoryBeliefDecisionExplainabilityText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce i blocchi testuali delle tab Memory, Belief e Decision usando
        /// il ViewModel EL-MBQD runtime.
        /// </para>
        ///
        /// <para><b>UI da registry, non da store live</b></para>
        /// <para>
        /// Il metodo non legge direttamente MemoryStore, BeliefStore o oggetti del
        /// World. Tutte le righe derivano da trace gia' passate dal registry
        /// UI-friendly, allineando pannello e JSONL.
        /// </para>
        /// </summary>
        private string BuildMemoryBeliefDecisionExplainabilityText(Arcontio.Core.World world, int npcId)
        {
            ClearMbqdBuffers();

            bool hasModel = MemoryBeliefDecisionExplainabilityViewModelBuilder.BuildForNpc(
                world,
                npcId,
                _mbqdExplainabilityViewModel,
                maxTimelineRows: 48);

            if (!hasModel)
            {
                string message = string.IsNullOrWhiteSpace(_mbqdExplainabilityViewModel.HeaderSubtitle)
                    ? "EL-MBQD non disponibile per questo NPC"
                    : _mbqdExplainabilityViewModel.HeaderSubtitle;

                _sbMbqdMemoryStore.Append("<color=#6E7681>").Append(message).Append("</color>");
                _sbMbqdBeliefEntries.Append("<color=#6E7681>").Append(message).Append("</color>");
                _sbMbqdDecisionSelected.Append("<color=#6E7681>").Append(message).Append("</color>");
                return $"NPC #{npcId}";
            }

            var model = _mbqdExplainabilityViewModel;
            AppendMbqdMemory(model);
            AppendMbqdBelief(model);
            AppendMbqdDecision(model);
            return model.HeaderSubtitle;
        }

        private void ClearMbqdBuffers()
        {
            _sbMbqdMemoryStore.Clear();
            _sbMbqdMemoryLatest.Clear();
            _sbMbqdMemoryTimeline.Clear();
            _sbMbqdBeliefEntries.Clear();
            _sbMbqdBeliefQuery.Clear();
            _sbMbqdBeliefMutation.Clear();
            _sbMbqdDecisionSelected.Clear();
            _sbMbqdDecisionCandidates.Clear();
            _sbMbqdDecisionBridge.Clear();
        }

        private void AppendMbqdMemory(MemoryBeliefDecisionExplainabilityViewModel model)
        {
            _sbMbqdMemoryStore.Append(Kv("trace totali", model.MemoryCount.ToString(), "#E6EDF3")).Append('\n');
            if (model.MemoryBars.Count == 0)
            {
                _sbMbqdMemoryStore.Append("<color=#6E7681>(nessuna memory trace)</color>");
            }
            else
            {
                for (int i = 0; i < model.MemoryBars.Count; i++)
                {
                    var bar = model.MemoryBars[i];
                    _sbMbqdMemoryStore
                        .Append("<color=").Append(ColorFor(bar.ColorRole)).Append(">")
                        .Append(bar.Label).Append("</color>")
                        .Append("  count=").Append(bar.Count)
                        .Append("  fill=").Append(bar.Fill01.ToString("0.00"))
                        .Append('\n');
                }
            }

            var memory = model.LatestMemory;
            if (!memory.HasValue)
            {
                _sbMbqdMemoryLatest.Append("<color=#6E7681>(nessuna ultima trace)</color>");
            }
            else
            {
                _sbMbqdMemoryLatest
                    .Append(Kv("tick", memory.Tick.ToString(), "#8B949E")).Append('\n')
                    .Append(Kv("eventType", memory.EventType, "#58A6FF")).Append('\n')
                    .Append(Kv("traceType", memory.TraceType, "#58A6FF")).Append('\n')
                    .Append(Kv("subjectId", memory.SubjectId.ToString(), "#E6EDF3")).Append('\n')
                    .Append(Kv("secondarySubjectId", memory.SecondarySubjectId.ToString(), "#8B949E")).Append('\n')
                    .Append(Kv("subjectDefId", EmptyMuted(memory.SubjectDefId), "#E6EDF3")).Append('\n')
                    .Append(Kv("cell", memory.Cell, "#E6EDF3")).Append('\n')
                    .Append(Kv("intensity", memory.Intensity01.ToString("0.00"), "#3FB950")).Append('\n')
                    .Append(Kv("reliability", memory.Reliability01.ToString("0.00"), "#3FB950")).Append('\n')
                    .Append(Kv("isHeard", memory.IsHeard.ToString(), memory.IsHeard ? "#D29922" : "#6E7681")).Append('\n')
                    .Append(Kv("heardKind", EmptyMuted(memory.HeardKind), "#8B949E")).Append('\n')
                    .Append(Kv("sourceSpeakerId", memory.SourceSpeakerId.ToString(), "#8B949E")).Append('\n')
                    .Append(Kv("storeResult", memory.StoreResult, ColorForResult(memory.StoreResult)));
            }

            AppendTimeline(_sbMbqdMemoryTimeline, model, onlyMemory: true);
        }

        private void AppendMbqdBelief(MemoryBeliefDecisionExplainabilityViewModel model)
        {
            if (model.BeliefRows.Count == 0)
            {
                _sbMbqdBeliefEntries.Append("<color=#6E7681>(nessuna belief mutation recente)</color>");
            }
            else
            {
                for (int i = 0; i < model.BeliefRows.Count; i++)
                {
                    var belief = model.BeliefRows[i];
                    _sbMbqdBeliefEntries
                        .Append("<color=").Append(ColorFor(belief.ColorRole)).Append(">")
                        .Append('#').Append(belief.BeliefId).Append(' ')
                        .Append(belief.Category).Append("  ").Append(belief.EstimatedCell)
                        .Append("</color>")
                        .Append("  conf=").Append(belief.Confidence.ToString("0.00"))
                        .Append("  fresh=").Append(belief.Freshness.ToString("0.00"))
                        .Append("  status=").Append(belief.Status)
                        .Append("  source=").Append(belief.Source)
                        .Append("  sources=").Append(belief.SourceCount)
                        .Append('\n');
                }
            }

            var query = model.LatestQuery;
            if (!query.HasValue)
            {
                _sbMbqdBeliefQuery.Append("<color=#6E7681>(nessuna query)</color>");
            }
            else
            {
                _sbMbqdBeliefQuery
                    .Append(Kv("tick", query.Tick.ToString(), "#8B949E")).Append('\n')
                    .Append(Kv("goalType", query.GoalType, "#58A6FF")).Append('\n')
                    .Append(Kv("urgency", query.Urgency01.ToString("0.00"), "#D29922")).Append('\n')
                    .Append(Kv("npcCell", query.NpcCell, "#E6EDF3")).Append('\n')
                    .Append(Kv("minConfidence", query.MinConfidence.ToString("0.00"), "#E6EDF3")).Append('\n')
                    .Append(Kv("candidateCount", query.CandidateCount.ToString(), "#E6EDF3")).Append('\n')
                    .Append(Kv("usableCandidateCount", query.UsableCandidateCount.ToString(), "#E6EDF3")).Append('\n')
                    .Append(Kv("isEmpty", query.IsEmpty.ToString(), query.IsEmpty ? "#D29922" : "#3FB950")).Append('\n')
                    .Append(Kv("emptyReason", EmptyMuted(query.EmptyReason), query.IsEmpty ? "#D29922" : "#6E7681")).Append('\n')
                    .Append(Kv("winner", FormatBeliefInline(query.Winner), query.IsEmpty ? "#6E7681" : "#3FB950")).Append('\n')
                    .Append(Kv("finalScore", query.FinalScore.ToString("0.00"), "#E6EDF3")).Append('\n');

                AppendContributions(_sbMbqdBeliefQuery, query.Contributions);
            }

            var mutation = model.LatestBeliefMutation;
            if (!mutation.HasValue)
            {
                _sbMbqdBeliefMutation.Append("<color=#6E7681>(nessuna mutazione belief)</color>");
            }
            else
            {
                _sbMbqdBeliefMutation
                    .Append(Kv("tick", mutation.Tick.ToString(), "#8B949E")).Append('\n')
                    .Append(Kv("operation", mutation.Operation, ColorForOperation(mutation.Operation))).Append('\n')
                    .Append(Kv("hasSourceTrace", mutation.HasSourceTrace.ToString(), mutation.HasSourceTrace ? "#3FB950" : "#6E7681")).Append('\n')
                    .Append(Kv("sourceTraceType", EmptyMuted(mutation.SourceTraceType), "#58A6FF")).Append('\n')
                    .Append(Kv("belief", FormatBeliefInline(mutation.Belief), ColorFor(mutation.Belief.ColorRole))).Append('\n')
                    .Append(Kv("reason", EmptyMuted(mutation.Reason), "#E6EDF3"));
            }
        }

        private void AppendMbqdDecision(MemoryBeliefDecisionExplainabilityViewModel model)
        {
            var decision = model.LatestDecision;
            if (!decision.HasValue)
            {
                _sbMbqdDecisionSelected.Append("<color=#6E7681>(nessuna decisione)</color>");
            }
            else
            {
                _sbMbqdDecisionSelected
                    .Append("<color=#3FB950>").Append(decision.SelectedIntent).Append("</color>\n")
                    .Append(Kv("tick", decision.Tick.ToString(), "#8B949E")).Append('\n')
                    .Append(Kv("auditValid", decision.AuditValid.ToString(), decision.AuditValid ? "#3FB950" : "#F85149")).Append('\n')
                    .Append(Kv("candidateCount", decision.CandidateCount.ToString(), "#E6EDF3")).Append('\n')
                    .Append(Kv("selectedScore", decision.SelectedScore.ToString("0.00"), "#3FB950")).Append('\n')
                    .Append(Kv("selectedIndex", decision.SelectedIndex.ToString(), "#E6EDF3")).Append('\n')
                    .Append(Kv("selectionTopN", decision.SelectionTopN.ToString(), "#E6EDF3")).Append('\n')
                    .Append(Kv("selectionNoise01", decision.SelectionNoise01.ToString("0.00"), "#D29922")).Append('\n')
                    .Append(Kv("impulsivity01", decision.Impulsivity01.ToString("0.00"), "#D29922")).Append('\n')
                    .Append(Kv("effectiveNoise01", decision.EffectiveNoise01.ToString("0.00"), "#D29922"));

                for (int i = 0; i < decision.Candidates.Count; i++)
                {
                    var candidate = decision.Candidates[i];
                    _sbMbqdDecisionCandidates
                        .Append("<color=").Append(ColorFor(candidate.ColorRole)).Append(">")
                        .Append(candidate.Intent).Append("</color>")
                        .Append("  score=").Append(candidate.Score.ToString("0.00"))
                        .Append("  available=").Append(candidate.Available)
                        .Append("  need=").Append(candidate.Need)
                        .Append("  urgency=").Append(candidate.NeedUrgency01.ToString("0.00"))
                        .Append("  critical=").Append(candidate.IsCritical)
                        .Append("  requiresBelief=").Append(candidate.RequiresBeliefTarget)
                        .Append("  beliefEmpty=").Append(candidate.BeliefResultEmpty)
                        .Append('\n');

                    if (!string.IsNullOrWhiteSpace(candidate.FilteredReason))
                        _sbMbqdDecisionCandidates.Append("  filteredReason=").Append(candidate.FilteredReason).Append('\n');

                    if (candidate.RequiresBeliefTarget && !candidate.BeliefResultEmpty)
                        _sbMbqdDecisionCandidates.Append("  belief=").Append(FormatBeliefInline(candidate.Belief)).Append('\n');

                    AppendContributions(_sbMbqdDecisionCandidates, candidate.Contributions);
                    _sbMbqdDecisionCandidates.Append('\n');
                }
            }

            var bridge = model.LatestBridge;
            if (!bridge.HasValue)
            {
                _sbMbqdDecisionBridge.Append("<color=#6E7681>(nessun bridge)</color>");
            }
            else
            {
                _sbMbqdDecisionBridge
                    .Append(Kv("tick", bridge.Tick.ToString(), "#8B949E")).Append('\n')
                    .Append(Kv("selectedIntent", bridge.SelectedIntent, "#58A6FF")).Append('\n')
                    .Append(Kv("commandName", EmptyMuted(bridge.CommandName), bridge.Handled ? "#3FB950" : "#6E7681")).Append('\n')
                    .Append(Kv("handled", bridge.Handled.ToString(), bridge.Handled ? "#3FB950" : "#F85149")).Append('\n')
                    .Append(Kv("didMove", bridge.DidMove.ToString(), bridge.DidMove ? "#58A6FF" : "#6E7681")).Append('\n')
                    .Append(Kv("didSteal", bridge.DidSteal.ToString(), bridge.DidSteal ? "#D29922" : "#6E7681")).Append('\n')
                    .Append(Kv("targetCell", bridge.TargetCell, "#E6EDF3")).Append('\n')
                    .Append(Kv("targetSource", bridge.TargetSource, "#58A6FF")).Append('\n')
                    .Append(Kv("legacyFallbackUsed", bridge.LegacyFallbackUsed.ToString(), bridge.LegacyFallbackUsed ? "#D29922" : "#3FB950")).Append('\n')
                    .Append(Kv("reason", EmptyMuted(bridge.Reason), "#E6EDF3"));
            }
        }

        private static void AppendTimeline(StringBuilder output, MemoryBeliefDecisionExplainabilityViewModel model, bool onlyMemory)
        {
            if (model.Timeline.Count == 0)
            {
                output.Append("<color=#6E7681>(nessuna timeline)</color>");
                return;
            }

            for (int i = 0; i < model.Timeline.Count; i++)
            {
                var row = model.Timeline[i];
                if (onlyMemory && !string.Equals(row.Kind, "Memory", StringComparison.Ordinal))
                    continue;

                output.Append("<color=").Append(ColorFor(row.ColorRole)).Append(">")
                    .Append("t").Append(row.Tick)
                    .Append("  ").Append(row.Kind)
                    .Append("  ").Append(row.Summary)
                    .Append("</color>\n");
            }
        }

        private static void AppendContributions(StringBuilder output, List<MemoryBeliefDecisionContributionView> contributions)
        {
            if (contributions == null || contributions.Count == 0)
                return;

            output.Append("<color=#6E7681>-- score breakdown --</color>\n");
            for (int i = 0; i < contributions.Count; i++)
            {
                var contribution = contributions[i];
                string sign = contribution.Value >= 0f ? "+" : string.Empty;
                output.Append("  ")
                    .Append(contribution.Label)
                    .Append(" = <color=").Append(ColorFor(contribution.ColorRole)).Append(">")
                    .Append(sign).Append(contribution.Value.ToString("0.00"))
                    .Append("</color>\n");
            }
        }

        private static string Kv(string key, string value, string color)
        {
            return $"<color=#8B949E>{key}</color>  <color={color}>{value}</color>";
        }

        private static string FormatBeliefInline(MemoryBeliefDecisionBeliefView belief)
        {
            if (belief == null || belief.BeliefId == 0)
                return "nessuna";

            return $"#{belief.BeliefId} {belief.Category} {belief.EstimatedCell} conf={belief.Confidence:0.00} fresh={belief.Freshness:0.00} status={belief.Status} source={belief.Source} sources={belief.SourceCount}";
        }

        private static string EmptyMuted(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "n/d" : value;
        }

        private static string ColorFor(MemoryBeliefDecisionColorRole role)
        {
            return role switch
            {
                MemoryBeliefDecisionColorRole.Ok => "#3FB950",
                MemoryBeliefDecisionColorRole.Warning => "#D29922",
                MemoryBeliefDecisionColorRole.Error => "#F85149",
                MemoryBeliefDecisionColorRole.Info => "#58A6FF",
                MemoryBeliefDecisionColorRole.Muted => "#6E7681",
                _ => "#E6EDF3"
            };
        }

        private static string ColorForResult(string result)
        {
            return result switch
            {
                "Inserted" => "#3FB950",
                "Reinforced" => "#3FB950",
                "Merged" => "#3FB950",
                "Replaced" => "#D29922",
                "Dropped" => "#F85149",
                "Ignored" => "#6E7681",
                _ => "#E6EDF3"
            };
        }

        private static string ColorForOperation(string operation)
        {
            return operation switch
            {
                "Created" => "#3FB950",
                "Merged" => "#3FB950",
                "Reinforced" => "#3FB950",
                "Weakened" => "#D29922",
                "Stale" => "#D29922",
                "Conflicted" => "#F85149",
                "Discarded" => "#F85149",
                "RemovedByDecay" => "#F85149",
                _ => "#E6EDF3"
            };
        }

        // =============================================================================
        // BuildMovementExplainabilityText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il blocco testuale della sezione debug "EL Pathfinding" usando
        /// il <see cref="MovementExplainabilityViewModel"/> introdotto nella sessione H.
        /// Il testo viene poi passato al pannello laterale EL della MapGrid.
        /// </para>
        ///
        /// <para><b>Uso della pipeline grafica esistente</b></para>
        /// <para>
        /// La card NPC attuale e' una UI debug prefabless basata su sezioni testuali;
        /// questo pannello segue lo stesso metodo operativo, ma separa header,
        /// intent/plan ed eventi in tre buffer distinti. Non crea dati simulativi,
        /// prefab o sistemi grafici paralleli.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>world/npcId</b>: contesto debug gia' usato dal SummaryOverlay.</item>
        ///   <item><b>_movementExplainabilityViewModel</b>: snapshot riutilizzato per ridurre allocazioni.</item>
        ///   <item><b>intentPlanOutput</b>: buffer testuale del pannellino Intent/Plan.</item>
        ///   <item><b>eventsOutput</b>: buffer testuale dell'area eventi espandibile.</item>
        /// </list>
        /// </summary>
        private string BuildMovementExplainabilityText(Arcontio.Core.World world, int npcId, StringBuilder intentPlanOutput, StringBuilder eventsOutput, int maxEvents)
        {
            intentPlanOutput.Clear();
            eventsOutput.Clear();

            // Il ViewModel e' la barriera di lettura: la debug UI non entra nei ring
            // buffer e non ricalcola il pathfinding. Se l'EL non ha ancora dati, la
            // sezione resta informativa e non produce eccezioni.
            bool hasModel = MovementExplainabilityViewModelBuilder.BuildForNpc(
                world,
                npcId,
                _movementExplainabilityViewModel,
                maxEvents: maxEvents);

            if (!hasModel)
            {
                string message = string.IsNullOrWhiteSpace(_movementExplainabilityViewModel.HeaderSubtitle)
                    ? "EL pathfinding non disponibile per questo NPC"
                    : _movementExplainabilityViewModel.HeaderSubtitle;

                eventsOutput.Append("<color=#FF6666>").Append(message).Append("</color>");
                return $"NPC #{npcId}";
            }

            var model = _movementExplainabilityViewModel;
            string headerMeta = $"Tick = {model.Tick}   Intent = {model.CurrentIntentId}   Plan = {model.CurrentPlanId}";

            if (model.Intent != null && model.Intent.HasIntent)
            {
                // Intent: e' il "perche' sto andando li'". Rimane compatto per non
                // far crescere troppo la card quando tutte le sezioni sono aperte.
                intentPlanOutput.Append("<color=#A8E6A1>[INTENT]</color>\n")
                    .Append("Purpose = ").Append(model.Intent.Purpose).Append('\n')
                    .Append("Target = ").Append(model.Intent.Target).Append('\n')
                    .Append("Belief = ").Append(model.Intent.Belief).Append('\n')
                    .Append("Urgency = ").Append(model.Intent.Urgency01.ToString("0.00"))
                    .Append("   Verbosity = ").Append(model.Intent.VerbosityLevel)
                    .Append('\n');
            }
            else
            {
                intentPlanOutput.Append("<color=#888888>[INTENT] nessuna trace</color>\n");
            }

            if (model.Plan != null && model.Plan.HasPlan)
            {
                intentPlanOutput.Append('\n').Append("<color=#9FC5E8>[PLAN]</color>\n")
                    .Append("Mode = ").Append(model.Plan.SelectedMode)
                    .Append("   Why = ").Append(model.Plan.SelectionReason).Append('\n')
                    .Append("Route = ").Append(model.Plan.RouteSummary).Append('\n')
                    .Append("FirstStep = ").Append(model.Plan.FirstStep)
                    .Append("   Verbosity = ").Append(model.Plan.VerbosityLevel)
                    .Append('\n');

                if (model.Plan.Candidates != null && model.Plan.Candidates.Count > 0)
                {
                    intentPlanOutput.Append("Candidates:\n");
                    int candidateCount = Mathf.Min(3, model.Plan.Candidates.Count);

                    // Limite intenzionale: la card resta leggibile. Il ViewModel conserva
                    // comunque tutti i candidati disponibili per una UI piu' ampia futura.
                    for (int i = 0; i < candidateCount; i++)
                        intentPlanOutput.Append("- ").Append(model.Plan.Candidates[i]).Append('\n');

                    if (model.Plan.Candidates.Count > candidateCount)
                        intentPlanOutput.Append("- ... altri ").Append(model.Plan.Candidates.Count - candidateCount).Append('\n');
                }
            }
            else
            {
                intentPlanOutput.Append('\n').Append("<color=#888888>[PLAN] nessuna trace</color>\n");
            }

            eventsOutput.Append("<color=#FFD966>[EVENTS]</color>\n");
            if (model.Events == null || model.Events.Count <= 0)
            {
                eventsOutput.Append("<color=#888888>(nessun evento runtime)</color>");
                return headerMeta;
            }

            // Timeline gia' limitata dal builder. Qui invertiamo l'ordine per mostrare
            // subito l'evento piu' recente, e alterniamo bianco/grigio chiaro per
            // aumentare la separazione visiva tra righe consecutive.
            int visibleIndex = 0;
            for (int i = model.Events.Count - 1; i >= 0; i--)
            {
                var evt = model.Events[i];
                string color = ResolveExplainabilityEventColor(evt, visibleIndex);

                eventsOutput.Append("<color=").Append(color).Append(">");
                eventsOutput.Append("- t").Append(evt.Tick)
                    .Append(" ").Append(evt.EventType)
                    .Append("  mode=").Append(evt.ActiveMode)
                    .Append("  ").Append(evt.CurrentCell)
                    .Append(" -> ").Append(evt.TargetCell);

                if (!string.IsNullOrWhiteSpace(evt.Summary))
                    eventsOutput.Append("  | ").Append(evt.Summary);

                if (!string.IsNullOrWhiteSpace(evt.Detail))
                    eventsOutput.Append('\n').Append("  detail: ").Append(evt.Detail);

                eventsOutput.Append("</color>");

                if (i > 0)
                    eventsOutput.Append('\n').Append('\n');
                else
                    eventsOutput.Append('\n');

                visibleIndex++;
            }

            return headerMeta;
        }

        // =============================================================================
        // ResolveExplainabilityEventColor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Decide il colore rich text di una riga evento EL. Gli errori e i fallimenti
        /// hanno priorita' e vengono mostrati in rosso; gli altri eventi alternano
        /// bianco e grigio chiaro partendo dal piu' recente.
        /// </para>
        ///
        /// <para><b>Leggibilita' timeline</b></para>
        /// <para>
        /// La funzione resta nel layer view/debug: non interpreta il pathfinding per
        /// decidere comportamento, ma soltanto per rendere piu' leggibile la timeline.
        /// </para>
        /// </summary>
        private static string ResolveExplainabilityEventColor(MovementExplainabilityEventView evt, int visibleIndex)
        {
            if (IsExplainabilityErrorEvent(evt))
                return "#FF6666";

            return visibleIndex % 2 == 0 ? "#FFFFFF" : "#ADADAD";
        }

        // =============================================================================
        // IsExplainabilityErrorEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riconosce gli eventi EL che devono essere evidenziati come errore o
        /// fallimento nella UI debug. La classificazione usa solo dati gia' formattati
        /// dal ViewModel, senza rileggere store o pathfinding.
        /// </para>
        /// </summary>
        private static bool IsExplainabilityErrorEvent(MovementExplainabilityEventView evt)
        {
            if (evt == null)
                return false;

            if (string.Equals(evt.EventType, "Failed", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(evt.Detail)
                && evt.Detail.IndexOf("Failure", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return !string.IsNullOrWhiteSpace(evt.Summary)
                   && evt.Summary.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ============================================================
        // DEBUG HELPERS (food target reasoning)
        // ============================================================
        // Questi helper esistono per una ragione molto precisa:
        // vogliamo rendere leggibile nella card SE il sistema dei bisogni
        // dispone di un target cibo attuale perché lo vede ORA oppure perché
        // lo ricorda dalla memoria oggettuale.
        //
        // Non sono usati dalla simulazione per decidere; sono solo strumenti
        // di osservabilità, e quindi possono permettersi un po' di verbosità.
        private static void ComputeFoodTargetDebug(Arcontio.Core.World world, int npcId, out int visibleCommunityFoodObjId, out int rememberedCommunityFoodObjId)
        {
            visibleCommunityFoodObjId = 0;
            rememberedCommunityFoodObjId = 0;

            if (!world.GridPos.TryGetValue(npcId, out var npcPos))
                return;

            if (world.NpcObjectMemory.TryGetValue(npcId, out var store) && store != null)
            {
                var slots = store.Slots;
                for (int i = 0; i < slots.Length; i++)
                {
                    var e = slots[i];
                    if (!e.IsValid)
                        continue;
                    if (e.Kind != NpcObjectMemoryStore.SubjectKind.WorldObject)
                        continue;

                    int objId = e.SubjectId != 0 ? e.SubjectId : e.ObjectId;
                    if (objId == 0)
                        continue;
                    if (!world.FoodStocks.TryGetValue(objId, out var st))
                        continue;
                    if (st.Units <= 0)
                        continue;
                    if (st.OwnerKind != Arcontio.Core.OwnerKind.Community || st.OwnerId != 0)
                        continue;

                    rememberedCommunityFoodObjId = objId;

                    int ox = e.CellX;
                    int oy = e.CellY;
                    if (world.Objects.TryGetValue(objId, out var inst) && inst != null)
                    {
                        ox = inst.CellX;
                        oy = inst.CellY;
                    }

                    if (world.HasLineOfSight(npcPos.X, npcPos.Y, ox, oy))
                    {
                        visibleCommunityFoodObjId = objId;
                        return;
                    }
                }
            }

            // Fallback puramente osservativo: se non c'è nulla in memoria,
            // proviamo a capire se almeno in questo tick esiste uno stock community
            // direttamente visibile. Serve per leggere bug di encoding percettivo.
            foreach (var kv in world.FoodStocks)
            {
                int objId = kv.Key;
                var st = kv.Value;
                if (st.Units <= 0)
                    continue;
                if (st.OwnerKind != Arcontio.Core.OwnerKind.Community || st.OwnerId != 0)
                    continue;
                if (!world.Objects.TryGetValue(objId, out var inst) || inst == null)
                    continue;
                if (!world.HasLineOfSight(npcPos.X, npcPos.Y, inst.CellX, inst.CellY))
                    continue;
                visibleCommunityFoodObjId = objId;
                return;
            }
        }

        private void RefreshObjectTexts(Arcontio.Core.World world)
        {
            foreach (var kv in _objCards)
            {
                int objId = kv.Key;
                var card = kv.Value;

                if (!world.Objects.TryGetValue(objId, out var inst) || inst == null)
                    continue;

                if (!TryGetDef(world, inst.DefId, out var def) || def == null)
                    continue;

                _sbHeader.Clear();
                _sbHeader.Append("OBJ #").Append(objId)
                    .Append("  ").Append(def.Id)
                    .Append("  cell=(").Append(inst.CellX).Append(",").Append(inst.CellY).Append(")");

                // ============================================================
                // FOOD STOCK (runtime) — fonte di verità per quantità e ownership
                // ============================================================

                // Se è uno stock runtime, stampiamo SOLO questo.
                if (world.FoodStocks != null &&
                    world.FoodStocks.TryGetValue(objId, out var fs))
                {
                    _sbHeader.Append('\n');
                    _sbHeader.Append("FoodStock: units=").Append(fs.Units)
                             .Append(" owner=").Append(fs.OwnerKind).Append('/').Append(fs.OwnerId);
                }
                else
                {
                    // Dump properties (statiche) solo se NON esiste runtime food stock
                    if (def.Properties != null && def.Properties.Count > 0)
                    {
                        _sbHeader.Append('\n');
                        _sbHeader.Append("Props:");
                        for (int i = 0; i < def.Properties.Count; i++)
                        {
                            var p = def.Properties[i];
                            _sbHeader.Append(' ')
                                     .Append(p.Key).Append('=').Append(p.Value);
                            if (i < def.Properties.Count - 1) _sbHeader.Append(',');
                        }
                    }
                }

                card.SetText(_sbHeader.ToString());
            }
        }

        // ============================================================
        // INITIAL LAYOUT (one-shot collision avoidance)
        // ============================================================

        private void ApplyInitialLayoutAvoidOverlap(Arcontio.Core.World world, Camera cam, float tileSizeWorld)
        {
            // Obiettivo:
            // - Non risolvere continuamente (niente jitter).
            // - Solo dare un offset iniziale "ragionevole" per le card senza offset.
            //
            // Metodo:
            // - Costruiamo lista dei rect già occupati (in canvas local).
            // - Per ogni card senza offset, cerchiamo una posizione libera su una spirale discreta.

            _occupiedRects.Clear();

            // 1) registra rect già posizionati (quelli che hanno offset memorizzato)
            foreach (var kv in _npcCards)
            {
                int id = kv.Key;
                if (!_npcOffsets.TryGetValue(id, out var off)) continue;

                Vector2 anchor = GetNpcAnchorLocal(world, id, cam, tileSizeWorld);
                if (float.IsNaN(anchor.x)) continue;

                var rt = kv.Value.RootRectTransform;
                if (rt == null) continue;

                Vector2 pos = anchor + off;
                _occupiedRects.Add(GetRectAt(rt, pos));
            }

            foreach (var kv in _objCards)
            {
                int id = kv.Key;
                if (!_objOffsets.TryGetValue(id, out var off)) continue;

                Vector2 anchor = GetObjectAnchorLocal(world, id, cam, tileSizeWorld);
                if (float.IsNaN(anchor.x)) continue;

                var rt = kv.Value.RootRectTransform;
                if (rt == null) continue;

                Vector2 pos = anchor + off;
                _occupiedRects.Add(GetRectAt(rt, pos));
            }

            // 2) assegna offset iniziale ai mancanti (NPC)
            foreach (var kv in _npcCards)
            {
                int npcId = kv.Key;
                if (_npcOffsets.ContainsKey(npcId)) continue;

                Vector2 anchor = GetNpcAnchorLocal(world, npcId, cam, tileSizeWorld);
                if (float.IsNaN(anchor.x)) continue;

                var rt = kv.Value.RootRectTransform;
                if (rt == null) continue;

                Vector2 chosen = FindFreeOffset(anchor, rt, DefaultNpcOffset(npcId));
                _npcOffsets[npcId] = chosen;

                Vector2 pos = anchor + chosen;
                _occupiedRects.Add(GetRectAt(rt, pos));
            }

            // 3) assegna offset iniziale ai mancanti (Objects)
            foreach (var kv in _objCards)
            {
                int objId = kv.Key;
                if (_objOffsets.ContainsKey(objId)) continue;

                Vector2 anchor = GetObjectAnchorLocal(world, objId, cam, tileSizeWorld);
                if (float.IsNaN(anchor.x)) continue;

                var rt = kv.Value.RootRectTransform;
                if (rt == null) continue;

                Vector2 chosen = FindFreeOffset(anchor, rt, DefaultObjectOffset(objId));
                _objOffsets[objId] = chosen;

                Vector2 pos = anchor + chosen;
                _occupiedRects.Add(GetRectAt(rt, pos));
            }
        }

        private readonly List<Rect> _occupiedRects = new(256);

        private Vector2 FindFreeOffset(Vector2 anchor, RectTransform rt, Vector2 seedOffset)
        {
            // Prima prova: seedOffset
            var seedRect = GetRectAt(rt, anchor + seedOffset);
            if (!OverlapsAny(seedRect))
                return seedOffset;

            // Spirale discreta: r cresce, theta gira.
            // Parametri conservativi: debug overlay, non serve ottimo globale.
            float rStep = 28f;
            int rings = 10;
            int stepsPerRing = 12;

            for (int ring = 1; ring <= rings; ring++)
            {
                float r = ring * rStep;

                for (int s = 0; s < stepsPerRing; s++)
                {
                    float t = (s / (float)stepsPerRing) * (Mathf.PI * 2f);
                    var off = seedOffset + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r;

                    var rect = GetRectAt(rt, anchor + off);
                    if (!OverlapsAny(rect))
                        return off;
                }
            }

            // Se non troviamo: fallback al seed
            return seedOffset;
        }

        private bool OverlapsAny(Rect candidate)
        {
            for (int i = 0; i < _occupiedRects.Count; i++)
            {
                if (candidate.Overlaps(_occupiedRects[i]))
                    return true;
            }
            return false;
        }

        private static Rect GetRectAt(RectTransform rt, Vector2 anchoredPos)
        {
            Rect r = rt.rect;
            // Convertiamo rect locale (pivot) in canvas-local assumendo pivot centrale.
            // Nota: in questa UI le card hanno anchor/pivot centrati per semplicità.
            var min = anchoredPos + new Vector2(r.xMin, r.yMin);
            return new Rect(min, r.size);
        }

        // ============================================================
        // EVENTSYSTEM ENSURE
        // ============================================================

        private static void EnsureEventSystemExists()
        {
            // 1) Assicura che l’EventSystem esista.
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                var esGo = new GameObject("EventSystem");
                existing = esGo.AddComponent<EventSystem>();
            }

            // 2) Assicura che ci sia anche un input module UI, altrimenti non arrivano drag/pointer events.
            //    Se non c’è nessun BaseInputModule attaccato, lo aggiungiamo noi.
            var modules = existing.GetComponents<BaseInputModule>();
            if (modules != null && modules.Length > 0)
                return;

#if ENABLE_INPUT_SYSTEM
            // New Input System
            existing.gameObject.AddComponent<InputSystemUIInputModule>();
#else
    // Legacy Input System
    existing.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
