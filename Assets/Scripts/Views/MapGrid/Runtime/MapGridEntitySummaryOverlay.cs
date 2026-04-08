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


            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999; // sopra quasi tutto

            _root.AddComponent<CanvasScaler>();
            _root.AddComponent<GraphicRaycaster>();

            _rootRt = _root.GetComponent<RectTransform>();
            if (_rootRt == null) _rootRt = _root.AddComponent<RectTransform>();

            _rootRt.anchorMin = new Vector2(0f, 0f);
            _rootRt.anchorMax = new Vector2(1f, 1f);
            _rootRt.pivot = new Vector2(0.5f, 0.5f);
            _rootRt.sizeDelta = Vector2.zero;

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

        private readonly List<Arcontio.Core.MemoryTrace> _topMem = new(32);

        private bool _needsInitialLayout;

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

                _sbHeader.Append('\n')
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

                // Bisogni (v0.04.08) — barre invertite (piena=ok, vuota=critico)
                card.UpdateNeedsBars(needs);

                // DNA DRIFT (v0.04.07.b) — barre proporzionali
                if (world.NpcProfiles.TryGetValue(npcId, out var profile))
                {
                    var driftResult = NpcDnaDistance.Compute(dna, profile);
                    card.UpdateDnaDrift(dna, profile, driftResult);
                }
            }
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
