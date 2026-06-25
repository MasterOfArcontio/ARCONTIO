using System.Collections.Generic;
using Arcontio.Core;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlaySceneProbeRenderer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Renderer debug temporaneo per visualizzare una <c>ArcGraphDebugOverlayQueue</c>
    /// dentro una scena Unity.
    /// </para>
    ///
    /// <para><b>Principio architetturale: probe debug separato dal renderer produttivo</b></para>
    /// <para>
    /// Questo componente consuma solo queue debug ArcGraph gia' prodotte. Non legge
    /// il <c>World</c>, non chiama il feed runtime reale, non consulta
    /// <c>MapGridWorldView</c>, non interpreta input e non carica asset. Crea solo
    /// oggetti runtime temporanei sotto un root dedicato, cosi' possiamo provare a
    /// schermo Landmark/GVD senza trasformare il probe in renderer definitivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RenderDefaultDebugOverlayProbe</b>: crea dati finti Landmark/GVD e li disegna.</item>
    ///   <item><b>RenderQueue</b>: disegna celle, nodi ed edge da una queue passiva.</item>
    ///   <item><b>ClearProbe</b>: distrugge solo il root temporaneo del probe.</item>
    ///   <item><b>CreateSprite</b>: primitive sprite runtime per celle e nodi.</item>
    ///   <item><b>CreateLine</b>: primitive LineRenderer runtime per edge.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugOverlaySceneProbeRenderer : MonoBehaviour, IArcGraphDebugOverlayQueueConsumer
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private bool renderDefaultProbeOnStart;
        [SerializeField] private bool clearBeforeRender = true;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private bool placeProbeAtSceneCameraCenter = true;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float cellScale = 0.95f;
        [SerializeField] private float nodeScale = 0.55f;
        [SerializeField] private float normalEdgeWidth = 0.055f;
        [SerializeField] private float strongEdgeWidth = 0.10f;

        private const string ProbeRootName = "ArcGraphDebugOverlaySceneProbeRoot";
        private const int CellSortingOrder = 180;
        private const int EdgeSortingOrder = 190;
        private const int NodeSortingOrder = 200;
        private const int LabelSortingOrder = 210;

        private Transform _root;
        private Sprite _debugSprite;
        private Texture2D _debugTexture;
        private Material _lineMaterial;
        private Font _labelFont;

        // =============================================================================
        // SetTileWorldSize
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna la dimensione mondo di una cella per il rendering runtime.
        /// </para>
        /// </summary>
        public void SetTileWorldSize(float value)
        {
            tileWorldSize = value > 0.0001f ? value : 1f;
        }

        // =============================================================================
        // SetPlaceProbeAtSceneCameraCenter
        // =============================================================================
        /// <summary>
        /// <para>
        /// Decide se il probe deve essere centrato sulla camera o sulle coordinate
        /// reali della griglia. In runtime ArcGraph questo valore deve essere falso.
        /// </para>
        /// </summary>
        public void SetPlaceProbeAtSceneCameraCenter(bool enabled)
        {
            placeProbeAtSceneCameraCenter = enabled;
        }

        // =============================================================================
        // SetLogDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita i log del renderer debug.
        /// </para>
        /// </summary>
        public void SetLogDiagnostics(bool enabled)
        {
            logDiagnostics = enabled;
        }

        // =============================================================================
        // BuildProbeDiagnosticsText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce una diagnostica testuale minima del probe visuale.
        /// </para>
        ///
        /// <para><b>Principio architetturale: debug osservabile senza console log</b></para>
        /// <para>
        /// Il metodo non crea oggetti e non cambia stato: guarda soltanto se il
        /// root runtime esiste e quanti figli contiene. Serve a distinguere un
        /// problema di dati vuoti da un problema di rendering invisibile.
        /// </para>
        /// </summary>
        public string BuildProbeDiagnosticsText()
        {
            Transform root = _root != null ? _root : FindExistingRoot();
            if (root == null)
                return "probeRoot=False children=0";

            return "probeRoot=True children=" + root.childCount;
        }

        public bool HasProbeRoot()
        {
            return (_root != null ? _root : FindExistingRoot()) != null;
        }

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia opzionalmente il probe debug default.
        /// </para>
        ///
        /// <para><b>Attivazione esplicita</b></para>
        /// <para>
        /// Il flag e' falso di default. In una scena produttiva il probe non deve
        /// accendersi da solo: lo si attiva manualmente dall'Inspector o tramite
        /// context menu durante i test.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (!renderDefaultProbeOnStart)
                return;

            RenderDefaultDebugOverlayProbe();
        }

        // =============================================================================
        // RenderDefaultDebugOverlayProbe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue debug finta Landmark/GVD e la renderizza.
        /// </para>
        ///
        /// <para><b>Probe senza World</b></para>
        /// <para>
        /// I dati vengono creati localmente come DTO preparati. Il metodo usa il feed
        /// prepared-data solo per riusare la pipeline snapshot -> queue, ma non legge
        /// il mondo reale e non aggancia il runtime produttivo.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Render Default Debug Overlay Probe")]
        public void RenderDefaultDebugOverlayProbe()
        {
            ArcGraphDebugOverlayQueue queue = CreateDefaultDebugOverlayQueue();
            RenderQueue(queue);
        }

        // =============================================================================
        // RenderQueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna una queue debug ArcGraph gia' pronta.
        /// </para>
        ///
        /// <para><b>Renderer consumer-only</b></para>
        /// <para>
        /// Il metodo non costruisce DTO e non interroga sistemi esterni. Traduce solo
        /// item visibili in primitive Unity temporanee. Le label dei nodi landmark
        /// vengono disegnate come testo world-space leggero; le label HUD restano
        /// fuori da questo probe per non introdurre Canvas o pannelli debug.
        /// </para>
        /// </summary>
        public void RenderQueue(ArcGraphDebugOverlayQueue queue)
        {
            if (queue == null)
            {
                LogWarning("DebugQueueMissing");
                return;
            }

            EnsureRoot();

            if (clearBeforeRender)
                ClearProbeChildren();

            EnsureDebugSprite();
            EnsureLineMaterial();

            RenderCells(queue);
            RenderEdges(queue);
            RenderNodes(queue);

            if (logDiagnostics)
            {
                ArcGraphDebugOverlayQueueDiagnostics diagnostics =
                    queue.CreateDiagnostics("DebugOverlaySceneProbeRendered");
                Debug.Log(
                    "[ArcGraphDebugOverlaySceneProbe] Rendered debug queue. " +
                    "cells=" + diagnostics.CellItemCount +
                    ", nodes=" + diagnostics.NodeItemCount +
                    ", edges=" + diagnostics.EdgeItemCount +
                    ", hudLabelsIgnored=" + diagnostics.LabelItemCount +
                    ", visible=" + diagnostics.VisibleItemCount);
            }
        }

        // =============================================================================
        // ClearProbe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Distrugge gli oggetti temporanei creati dal probe debug.
        /// </para>
        ///
        /// <para><b>Cleanup confinato</b></para>
        /// <para>
        /// Il cleanup cerca e distrugge solo il root dedicato del probe. Non tocca
        /// MapGrid, altri renderer ArcGraph, scene asset, prefab o oggetti esterni.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear Debug Overlay Probe")]
        public void ClearProbe()
        {
            if (_root == null)
                _root = FindExistingRoot();

            if (_root == null)
                return;

            DestroyProbeObject(_root.gameObject);
            _root = null;
        }

        private void ClearProbeChildren()
        {
            if (_root == null)
                _root = FindExistingRoot();

            if (_root == null)
                return;

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                Transform child = _root.GetChild(i);
                if (child == null)
                    continue;

                child.gameObject.SetActive(false);
                DestroyProbeObject(child.gameObject);
            }
        }

        private void RenderCells(ArcGraphDebugOverlayQueue queue)
        {
            IReadOnlyList<ArcGraphDebugCellOverlayItem> cells = queue.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                ArcGraphDebugCellOverlayItem item = cells[i];
                if (!item.IsVisible)
                    continue;

                CreateSprite(
                    "DebugCell_" + i,
                    CellCenter(item.Cell),
                    cellScale,
                    ResolveColor(item.ColorKey, item.Intensity01),
                    CellSortingOrder);
            }
        }

        private void RenderNodes(ArcGraphDebugOverlayQueue queue)
        {
            IReadOnlyList<ArcGraphDebugNodeOverlayItem> nodes = queue.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                ArcGraphDebugNodeOverlayItem item = nodes[i];
                if (!item.IsVisible)
                    continue;

                CreateSprite(
                    "DebugNode_" + item.NodeId + "_" + i,
                    CellCenter(item.Cell),
                    Mathf.Max(0.08f, nodeScale * Mathf.Max(0.25f, item.Scale01)),
                    ResolveColor(item.ColorKey, 1f),
                    NodeSortingOrder);

                if (!string.IsNullOrWhiteSpace(item.Label))
                    CreateWorldLabel(
                        "DebugNodeLabel_" + item.NodeId + "_" + i,
                        CellCenter(item.Cell) + new Vector3(0.28f, 0.28f, 0f),
                        item.Label);
            }
        }

        private void RenderEdges(ArcGraphDebugOverlayQueue queue)
        {
            IReadOnlyList<ArcGraphDebugEdgeOverlayItem> edges = queue.Edges;
            for (int i = 0; i < edges.Count; i++)
            {
                ArcGraphDebugEdgeOverlayItem item = edges[i];
                if (!item.IsVisible)
                    continue;

                CreateLine(
                    "DebugEdge_" + i,
                    CellCenter(item.From),
                    CellCenter(item.To),
                    ResolveEdgeWidth(item.WidthKey),
                    ResolveColor(item.ColorKey, item.Reliability01),
                    EdgeSortingOrder);
            }
        }

        private SpriteRenderer CreateSprite(
            string name,
            Vector3 localCellPosition,
            float scale,
            Color color,
            int sortingOrder)
        {
            EnsureRoot();
            EnsureDebugSprite();

            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = ResolveWorldPosition(localCellPosition);
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale * tileWorldSize);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _debugSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return renderer;
        }

        private LineRenderer CreateLine(
            string name,
            Vector3 localFrom,
            Vector3 localTo,
            float width,
            Color color,
            int sortingOrder)
        {
            EnsureRoot();
            EnsureLineMaterial();

            var go = new GameObject(name);
            go.transform.SetParent(_root, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = 2;
            line.material = _lineMaterial;
            line.sortingOrder = sortingOrder;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = Mathf.Max(0.005f, width * tileWorldSize);
            line.endWidth = Mathf.Max(0.005f, width * tileWorldSize);
            line.SetPosition(0, ResolveWorldPosition(localFrom));
            line.SetPosition(1, ResolveWorldPosition(localTo));

            return line;
        }

        private TextMesh CreateWorldLabel(
            string name,
            Vector3 localCellPosition,
            string label)
        {
            EnsureRoot();

            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = ResolveWorldPosition(localCellPosition);
            go.transform.localScale = Vector3.one;

            var text = go.AddComponent<TextMesh>();
            text.text = label ?? string.Empty;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 28;
            text.characterSize = Mathf.Max(0.05f, 0.10f * tileWorldSize);
            text.anchor = TextAnchor.MiddleLeft;
            text.alignment = TextAlignment.Left;
            text.color = new Color(0.86f, 0.96f, 1f, 0.96f);

            Font labelFont = ResolveLabelFont();
            if (labelFont != null)
                text.font = labelFont;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (labelFont != null)
                    renderer.sharedMaterial = labelFont.material;

                renderer.sortingOrder = LabelSortingOrder;
            }

            return text;
        }

        // =============================================================================
        // ResolveLabelFont
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica il font IBM Plex usato dalle etichette world-space del probe.
        /// </para>
        ///
        /// <para><b>Principio architetturale: stile UI centralizzato con fallback locale</b></para>
        /// <para>
        /// Il renderer prova a usare lo stesso font ufficiale gia' importato sotto
        /// Resources, ma non rende la sonda debug dipendente da quel caricamento:
        /// se Unity non trova il font, il <c>TextMesh</c> resta comunque visibile
        /// con il font di default del motore.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Cache</b>: evita lookup ripetuti a ogni redraw degli overlay.</item>
        ///   <item><b>Medium</b>: peso preferito per leggibilita' sulle label piccole.</item>
        ///   <item><b>Regular</b>: fallback se il Medium non e' disponibile.</item>
        /// </list>
        /// </summary>
        private Font ResolveLabelFont()
        {
            if (_labelFont != null)
                return _labelFont;

            _labelFont = Resources.Load<Font>("ArcGraph/UI/fonts/IBMPlex/IBMPlexSans-Medium");
            if (_labelFont == null)
                _labelFont = Resources.Load<Font>("ArcGraph/UI/fonts/IBMPlex/IBMPlexSans-Regular");

            return _labelFont;
        }

        private ArcGraphDebugOverlayQueue CreateDefaultDebugOverlayQueue()
        {
            var feed = new ArcGraphDebugOverlayRuntimeFeed();

            var worldNodes = new List<LandmarkOverlayNode>
            {
                new LandmarkOverlayNode(1, 1, 0, 10, "W#10")
            };
            var worldEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(1, 1, 2, 1, 1f)
            };
            var knownNodes = new List<LandmarkOverlayNode>
            {
                new LandmarkOverlayNode(3, 1, 0, 20, "K#20")
            };
            var knownEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(3, 1, 4, 1, 0.8f)
            };
            var routeNodes = new List<LandmarkOverlayNode>
            {
                new LandmarkOverlayNode(5, 1, 0, 30, "R#30")
            };
            var routeEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(5, 1, 6, 1, 1f)
            };
            var lmPathEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(6, 1, 6, 2, 1f)
            };
            var directPathEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(6, 2, 7, 2, 1f)
            };
            var jumpPathEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(7, 2, 7, 3, 1f)
            };
            var complexEdges = new List<LandmarkOverlayEdge>
            {
                new LandmarkOverlayEdge(7, 3, 8, 3, 0.6f)
            };

            var gvd = new GvdDinOverlaySnapshot();
            gvd.IsValid = true;
            gvd.DtCells.Add(new GvdDinOverlayCellDt(10, 10, 4, 0.50f));
            gvd.DtCells.Add(new GvdDinOverlayCellDt(11, 10, 6, 0.75f));
            gvd.GvdRawCells.Add(new GvdDinOverlayCellGvd(12, 10, 100, 101));
            gvd.GvdNodes.Add(new LandmarkOverlayNode(13, 10, 3, 40, "G#40"));
            gvd.GvdEdges.Add(new LandmarkOverlayEdge(13, 10, 14, 10, 1f));

            feed.BuildFromPreparedDebugData(
                worldNodes,
                worldEdges,
                knownNodes,
                knownEdges,
                routeNodes,
                routeEdges,
                lmPathEdges,
                directPathEdges,
                jumpPathEdges,
                complexEdges,
                gvd);

            return feed.Queue;
        }

        private void EnsureRoot()
        {
            if (_root != null)
                return;

            _root = FindExistingRoot();
            if (_root != null)
                return;

            var go = new GameObject(ProbeRootName);
            go.transform.SetParent(transform, false);
            _root = go.transform;
        }

        private Transform FindExistingRoot()
        {
            return transform.Find(ProbeRootName);
        }

        private void EnsureDebugSprite()
        {
            if (_debugSprite != null)
                return;

            _debugTexture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            _debugTexture.name = "ArcGraphDebugOverlayProbePixel";
            _debugTexture.SetPixel(0, 0, Color.white);
            _debugTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            _debugSprite = Sprite.Create(
                _debugTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
            _debugSprite.name = "ArcGraphDebugOverlayProbeSprite";
        }

        private void EnsureLineMaterial()
        {
            if (_lineMaterial != null)
                return;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _lineMaterial = new Material(shader);
        }

        private Vector3 CellCenter(ArcGraphCellCoord cell)
        {
            return new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z);
        }

        private Vector3 ResolveWorldPosition(Vector3 localCellPosition)
        {
            Vector3 baseOffset = originOffset;

            if (placeProbeAtSceneCameraCenter && sceneCamera != null)
            {
                Vector3 cameraPosition = sceneCamera.transform.position;
                baseOffset += new Vector3(cameraPosition.x, cameraPosition.y, 0f);
                baseOffset -= new Vector3(7f * tileWorldSize, 5f * tileWorldSize, 0f);
            }

            return baseOffset + (localCellPosition * tileWorldSize);
        }

        private float ResolveEdgeWidth(string widthKey)
        {
            return widthKey == "debug/edge/strong"
                ? strongEdgeWidth
                : normalEdgeWidth;
        }

        private Color ResolveColor(string colorKey, float intensity01)
        {
            Color color;
            switch (colorKey)
            {
                case "debug/fov/observed":
                    color = new Color(1f, 1f, 1f, 0.22f);
                    break;
                case "debug/fov/watched":
                    color = new Color(0.45f, 0.92f, 1f, 0.16f);
                    break;
                case "debug/fov/heat":
                    color = new Color(1f, 1f, 1f, Mathf.Clamp01(intensity01) * 0.50f);
                    break;
                case "debug/dt/heat":
                    color = Color.Lerp(new Color(0.10f, 0.18f, 1f, 0.35f), new Color(1f, 0.15f, 0.05f, 0.55f), Clamp01(intensity01));
                    break;
                case "debug/gvd/raw":
                    color = new Color(0f, 1f, 1f, 0.55f);
                    break;
                case "debug/landmark/world-node":
                    color = new Color(1f, 1f, 1f, 0.85f);
                    break;
                case "debug/landmark/world-edge":
                    color = new Color(0.86f, 0.94f, 1f, 0.24f);
                    break;
                case "debug/landmark/doorway":
                    color = new Color(0.35f, 0.68f, 1f, 1f);
                    break;
                case "debug/landmark/junction":
                    color = new Color(1f, 0.82f, 0.25f, 1f);
                    break;
                case "debug/landmark/area-center":
                    color = new Color(0.55f, 0.95f, 1f, 1f);
                    break;
                case "debug/landmark/biological-anchor":
                    color = new Color(0.10f, 0.95f, 0.25f, 1f);
                    break;
                case "debug/landmark/known-node":
                    color = new Color(0.20f, 1f, 0.55f, 1f);
                    break;
                case "debug/landmark/known-edge":
                    color = new Color(0.20f, 1f, 0.55f, 0.62f);
                    break;
                case "debug/landmark/route-node":
                    color = new Color(1f, 0.70f, 0.16f, 0.95f);
                    break;
                case "debug/landmark/route-edge":
                    color = new Color(1f, 0.70f, 0.16f, 0.92f);
                    break;
                case "debug/path/lm":
                    color = new Color(1f, 0.70f, 0.16f, 0.95f);
                    break;
                case "debug/path/direct":
                    color = new Color(0.20f, 0.75f, 1f, 0.95f);
                    break;
                case "debug/path/jump":
                    color = new Color(1f, 0.20f, 0.75f, 0.95f);
                    break;
                case "debug/path/complex":
                    color = new Color(1f, 0.90f, 0.18f, 0.10f);
                    break;
                case "debug/landmark/gvd-node":
                case "debug/gvd/edge":
                    color = new Color(0.70f, 0.10f, 1f, 1f);
                    break;
                case "debug/landmark/biological-node":
                    color = new Color(0.10f, 0.95f, 0.25f, 1f);
                    break;
                default:
                    color = new Color(1f, 1f, 1f, 0.65f);
                    break;
            }

            return color;
        }

        private void LogWarning(string reason)
        {
            if (!logDiagnostics)
                return;

            Debug.LogWarning("[ArcGraphDebugOverlaySceneProbe] " + reason);
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }

        private static void DestroyProbeObject(Object unityObject)
        {
            if (unityObject == null)
                return;

            if (Application.isPlaying)
                Destroy(unityObject);
            else
                DestroyImmediate(unityObject);
        }

        private void OnDestroy()
        {
            ClearProbe();

            if (_debugSprite != null)
            {
                DestroyProbeObject(_debugSprite);
                _debugSprite = null;
            }

            if (_debugTexture != null)
            {
                DestroyProbeObject(_debugTexture);
                _debugTexture = null;
            }

            if (_lineMaterial != null)
            {
                DestroyProbeObject(_lineMaterial);
                _lineMaterial = null;
            }
        }
    }
}
