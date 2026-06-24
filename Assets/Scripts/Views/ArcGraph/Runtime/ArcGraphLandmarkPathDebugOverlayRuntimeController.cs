using System.Collections.Generic;
using System.Text;
using Arcontio.Core;
using SocialViewer.UI;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphLandmarkPathDebugOverlayRuntimeController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller runtime degli overlay debug Landmark e Pathfinding in ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: overlay visuali sovrapponibili, non view mode esclusive</b></para>
    /// <para>
    /// Landmark e Pathfinding sono layer di osservazione che possono essere accesi
    /// indipendentemente dalla TopBar. Il controller legge solo un context runtime
    /// read-only, sceglie l'NPC attivo in modo view-side e consegna una queue
    /// filtrata al renderer debug. Non muta il <c>World</c>, non calcola nuovi
    /// path, non interroga <c>MapGridWorldView</c> e non invia comandi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>runtimeContextProvider</b>: sorgente autorizzata del context ArcGraph.</item>
    ///   <item><b>overlayConsumer</b>: renderer world-space della queue debug.</item>
    ///   <item><b>landmarkOverlayEnabled</b>: mostra solo nodi landmark globali con label.</item>
    ///   <item><b>pathfindingOverlayEnabled</b>: mostra route/path dell'NPC selezionato.</item>
    ///   <item><b>_feed/_options</b>: costruzione dati filtrata, senza duplicare producer Core.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphLandmarkPathDebugOverlayRuntimeController : MonoBehaviour
    {
        [SerializeField] private ArcGraphRuntimeContextProvider runtimeContextProvider;
        [SerializeField] private ArcGraphDebugOverlaySceneProbeRenderer overlayConsumer;
        [SerializeField] private bool landmarkOverlayEnabled;
        [SerializeField] private bool pathfindingOverlayEnabled;
        [SerializeField] private bool processInUpdate = true;
        [SerializeField] private bool logDiagnostics;

        private readonly ArcGraphDebugOverlayRuntimeFeed _feed = new ArcGraphDebugOverlayRuntimeFeed();
        private readonly ArcGraphDebugOverlayRuntimeFeedOptions _options = new ArcGraphDebugOverlayRuntimeFeedOptions
        {
            IncludeLandmark = true,
            IncludeLandmarkGraph = true,
            IncludeLandmarkGraphEdges = false,
            IncludeKnownLandmarkGraph = false,
            IncludeLandmarkRoute = false,
            IncludeLandmarkPaths = false,
            IncludeLandmarkComplexEdges = false,
            IncludeGvdDin = false,
            IncludeDtHeatmap = false,
            IncludeGvdRaw = false,
            IncludeGvdGraph = false
        };

        private long _lastRenderedTick = long.MinValue;
        private int _lastRenderedNpcId = -1;
        private bool _lastRenderedLandmarkOverlayEnabled;
        private bool _lastRenderedPathfindingOverlayEnabled;
        private int _landmarkSetCallCount;
        private int _pathfindingSetCallCount;
        private int _processFrameCallCount;
        private string _lastProcessReason = "NotProcessed";

        public bool LandmarkOverlayEnabled => landmarkOverlayEnabled;
        public bool PathfindingOverlayEnabled => pathfindingOverlayEnabled;
        public ArcGraphDebugOverlayRuntimeFeedDiagnostics LastDiagnostics => _feed.LastDiagnostics;

        // =============================================================================
        // BuildRuntimeDiagnosticsText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce una stringa compatta per il pannello diagnostico runtime LM/PF.
        /// </para>
        ///
        /// <para><b>Principio architetturale: osservabilita' read-only</b></para>
        /// <para>
        /// Questo metodo non accende overlay, non invia comandi e non muta il
        /// mondo. Legge solo binding, stato toggle e ultima diagnostica del feed,
        /// cosi' possiamo capire se la rottura e' nel controller, nei dati World,
        /// nella queue o nel renderer Unity.
        /// </para>
        /// </summary>
        public string BuildRuntimeDiagnosticsText()
        {
            ArcGraphRuntimeContext context = runtimeContextProvider != null
                ? runtimeContextProvider.BuildTerrainRuntimeContext()
                : null;
            World world = context?.World;
            int activeNpcId = ResolveSelectedNpcId(world);
            ArcGraphDebugOverlayRuntimeFeedDiagnostics diagnostics = LastDiagnostics;
            ArcGraphDebugOverlayQueueDiagnostics queue = diagnostics.QueueDiagnostics;

            string probe = overlayConsumer != null
                ? overlayConsumer.BuildProbeDiagnosticsText()
                : "probeRoot=False children=0";

            string tick = world != null
                ? world.Global.CurrentTickIndex.ToString()
                : "--";

            return
                "LM/PF DEBUG\n" +
                "toggle LM=" + landmarkOverlayEnabled +
                " PF=" + pathfindingOverlayEnabled +
                " update=" + processInUpdate + "\n" +
                "calls setLM=" + _landmarkSetCallCount +
                " setPF=" + _pathfindingSetCallCount +
                " process=" + _processFrameCallCount + "\n" +
                "binding provider=" + (runtimeContextProvider != null) +
                " consumer=" + (overlayConsumer != null) +
                " world=" + (world != null) + "\n" +
                "tick=" + tick +
                " selectedNpc=" + activeNpcId +
                " reason=" + diagnostics.Reason +
                " lastProcess=" + _lastProcessReason + "\n" +
                "attempt LM=" + diagnostics.LandmarkAttempted +
                " srcNodes=" + diagnostics.LandmarkSourceNodeCount +
                " srcEdges=" + diagnostics.LandmarkSourceEdgeCount + "\n" +
                "queue cells=" + queue.CellItemCount +
                " nodes=" + queue.NodeItemCount +
                " edges=" + queue.EdgeItemCount +
                " visible=" + queue.VisibleItemCount + "\n" +
                "visual routeEdge=" + _options.IncludeLandmarkRoute +
                " complexEdges=" + _options.IncludeLandmarkComplexEdges + "\n" +
                probe + "\n" +
                BuildSelectedNpcPathfindingDiagnostics(world, activeNpcId);
        }

        // =============================================================================
        // BuildSelectedNpcPathfindingDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce la sezione diagnostica del pathfinding dell'NPC selezionato.
        /// </para>
        ///
        /// <para><b>Principio architetturale: pannello debug come lettura passiva</b></para>
        /// <para>
        /// Questa diagnostica non chiama planner, non forza replan e non corregge
        /// stati runtime. Legge soltanto i dizionari gia' esposti dal Core per
        /// capire quale fonte dati stia usando l'overlay PF: macro-route, direct
        /// commit, local search o buffer debug accumulati.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Report route</b>: stato aggregato visibile anche all'inspector.</item>
        ///   <item><b>Stati esecutivi</b>: MacroRoute, DirectCommit e GoalLocalSearch.</item>
        ///   <item><b>Buffer overlay</b>: conteggi delle liste LM/Direct/Jump usate dal renderer.</item>
        ///   <item><b>Preview celle</b>: piccola anteprima del path corrente letto dall'overlay.</item>
        /// </list>
        /// </summary>
        private static string BuildSelectedNpcPathfindingDiagnostics(World world, int activeNpcId)
        {
            if (world == null)
                return "PF NPC DATA\nworld=NULL";

            if (activeNpcId <= 0 || !world.ExistsNpc(activeNpcId))
                return "PF NPC DATA\nselectedNpc=NULL";

            var sb = new StringBuilder(1024);
            sb.AppendLine("PF NPC DATA");

            if (world.GridPos.TryGetValue(activeNpcId, out var pos))
                sb.AppendLine("npc=" + activeNpcId + " cell=(" + pos.X + "," + pos.Y + ")");
            else
                sb.AppendLine("npc=" + activeNpcId + " cell=NULL");

            if (world.TryGetNpcMacroRouteDebugReport(activeNpcId, out var report))
            {
                sb.AppendLine(
                    "report hasRoute=" + report.HasRoute +
                    " exec=" + report.ExecutionActive +
                    " mode=" + Safe(report.NavigationMode) +
                    " lastMile=" + report.IsDoingLastMile);
                sb.AppendLine(
                    "report nextIdx=" + report.NextRouteNodeIndex +
                    " nextNode=" + report.NextRouteNodeId +
                    " immediate=(" + report.ImmediateTargetX + "," + report.ImmediateTargetY + ")" +
                    " target=(" + report.TargetCellX + "," + report.TargetCellY + ")");
                sb.AppendLine(
                    "report switchTick=" + report.LastModeSwitchTick +
                    " switchReason=" + Safe(report.LastModeSwitchReason) +
                    " fail=" + Safe(report.ExecutionFailureReason));
            }
            else
            {
                sb.AppendLine("report=NULL");
            }

            sb.AppendLine("overlaySource=" + ResolveOverlayPathSource(world, activeNpcId));
            AppendMacroStateDiagnostics(sb, world, activeNpcId);
            AppendDirectStateDiagnostics(sb, world, activeNpcId);
            AppendLocalStateDiagnostics(sb, world, activeNpcId);
            AppendDebugBufferDiagnostics(sb, world, activeNpcId);

            return sb.ToString().TrimEnd();
        }

        // =============================================================================
        // ResolveOverlayPathSource
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica quale fonte path dovrebbe essere dominante per l'overlay PF.
        /// </para>
        ///
        /// <para><b>Principio architetturale: diagnostica sintetica sopra dati runtime</b></para>
        /// <para>
        /// Il metodo non decide nuovi comportamenti. Riassume soltanto la priorita'
        /// usata dalla lettura overlay: local search, direct commit, traccia LM
        /// realmente percorsa oppure nessuna fonte path disponibile.
        /// </para>
        /// </summary>
        private static string ResolveOverlayPathSource(World world, int npcId)
        {
            if (world.Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var local)
                && local != null
                && local.Active
                && local.CurrentPath != null
                && local.CurrentPath.Count >= 2)
            {
                return "local-current-path";
            }

            if (world.Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var direct)
                && direct != null
                && direct.Active
                && direct.CurrentPath != null
                && direct.CurrentPath.Count >= 2)
            {
                return "direct-current-path";
            }

            if (world.Pathfinding.DebugLmPathCells.TryGetValue(npcId, out var lmPath)
                && lmPath != null
                && lmPath.Count >= 2)
            {
                return "lm-debug-trace";
            }

            return "none";
        }

        // =============================================================================
        // AppendMacroStateDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge al pannello lo stato macro-route dell'NPC.
        /// </para>
        /// </summary>
        private static void AppendMacroStateDiagnostics(StringBuilder sb, World world, int npcId)
        {
            bool hasMacro = world.Pathfinding.MacroRouteExecution.TryGetValue(npcId, out var macro)
                && macro != null;
            bool hasPlan = world.NpcMacroRoutes.TryGetValue(npcId, out var plan)
                && plan != null;

            sb.AppendLine(
                "macro state=" + hasMacro +
                " active=" + (hasMacro && macro.Active) +
                " plan=" + hasPlan +
                " planOk=" + (hasPlan && plan.Succeeded) +
                " planNodes=" + (hasPlan && plan.NodeIds != null ? plan.NodeIds.Count : 0));

            if (!hasMacro)
                return;

            sb.AppendLine(
                "macro mode=" + Safe(macro.NavigationMode) +
                " nextIdx=" + macro.NextRouteNodeIndex +
                " approachingFirstLm=" + macro.IsApproachingFirstLm +
                " lastMile=" + macro.IsDoingLastMile);
        }

        // =============================================================================
        // AppendDirectStateDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge al pannello lo stato DirectCommit e il path corrente diretto.
        /// </para>
        /// </summary>
        private static void AppendDirectStateDiagnostics(StringBuilder sb, World world, int npcId)
        {
            bool hasDirect = world.Pathfinding.DirectCommitExecution.TryGetValue(npcId, out var direct)
                && direct != null;
            int count = hasDirect && direct.CurrentPath != null ? direct.CurrentPath.Count : 0;

            sb.AppendLine(
                "direct state=" + hasDirect +
                " active=" + (hasDirect && direct.Active) +
                " count=" + count +
                " nextIdx=" + (hasDirect ? direct.NextPathIndex : -1));

            if (hasDirect)
            {
                sb.AppendLine(
                    "direct immediate=(" + direct.ImmediateTargetX + "," + direct.ImmediateTargetY + ")" +
                    " final=(" + direct.FinalTargetCellX + "," + direct.FinalTargetCellY + ")" +
                    " fail=" + Safe(direct.FailureReason));
                sb.AppendLine("direct path=" + FormatPathPreview(direct.CurrentPath, direct.NextPathIndex));
            }
        }

        // =============================================================================
        // AppendLocalStateDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge al pannello lo stato GoalLocalSearch e il path locale corrente.
        /// </para>
        /// </summary>
        private static void AppendLocalStateDiagnostics(StringBuilder sb, World world, int npcId)
        {
            bool hasLocal = world.Pathfinding.GoalLocalSearchExecution.TryGetValue(npcId, out var local)
                && local != null;
            int count = hasLocal && local.CurrentPath != null ? local.CurrentPath.Count : 0;

            sb.AppendLine(
                "local state=" + hasLocal +
                " active=" + (hasLocal && local.Active) +
                " count=" + count +
                " nextIdx=" + (hasLocal ? local.NextPathIndex : -1) +
                " budget=" + (hasLocal ? local.BudgetRemaining : 0));

            if (hasLocal)
            {
                sb.AppendLine(
                    "local immediate=(" + local.ImmediateTargetX + "," + local.ImmediateTargetY + ")" +
                    " final=(" + local.FinalTargetCellX + "," + local.FinalTargetCellY + ")" +
                    " fail=" + Safe(local.FailureReason));
                sb.AppendLine("local path=" + FormatPathPreview(local.CurrentPath, local.NextPathIndex));
            }
        }

        // =============================================================================
        // AppendDebugBufferDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge al pannello i buffer storici/debug ancora presenti nello stato PF.
        /// </para>
        /// </summary>
        private static void AppendDebugBufferDiagnostics(StringBuilder sb, World world, int npcId)
        {
            int lmCount = CountDebugPath(world.Pathfinding.DebugLmPathCells, npcId);
            int directCount = CountDebugPath(world.Pathfinding.DebugDirectPathCells, npcId);
            int jumpCount = CountDebugPath(world.Pathfinding.DebugJumpPathCells, npcId);

            sb.AppendLine(
                "debug buffers lm=" + lmCount +
                " direct=" + directCount +
                " jump=" + jumpCount);

            if (world.Pathfinding.DebugLmPathCells.TryGetValue(npcId, out var lmPath) && lmPath != null)
                sb.AppendLine("debug lmPath=" + FormatPathPreview(lmPath, lmPath.Count));
            if (world.Pathfinding.DebugDirectPathCells.TryGetValue(npcId, out var directPath) && directPath != null)
                sb.AppendLine("debug directPath=" + FormatPathPreview(directPath, directPath.Count));
            if (world.Pathfinding.DebugJumpPathCells.TryGetValue(npcId, out var jumpPath) && jumpPath != null)
                sb.AppendLine("debug jumpPath=" + FormatPathPreview(jumpPath, jumpPath.Count));
        }

        // =============================================================================
        // CountDebugPath
        // =============================================================================
        /// <summary>
        /// <para>
        /// Conta in modo null-safe le celle di un buffer path debug per NPC.
        /// </para>
        /// </summary>
        private static int CountDebugPath(Dictionary<int, List<GridPosition>> store, int npcId)
        {
            return store != null && store.TryGetValue(npcId, out var path) && path != null
                ? path.Count
                : 0;
        }

        // =============================================================================
        // FormatPathPreview
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un path cella-per-cella in una stringa breve leggibile a pannello.
        /// </para>
        /// </summary>
        private static string FormatPathPreview(List<GridPosition> path, int nextPathIndex)
        {
            if (path == null || path.Count == 0)
                return "[]";

            int startIndex = 0;
            int max = Mathf.Min(path.Count, startIndex + 10);
            var sb = new StringBuilder(160);
            sb.Append("[");
            for (int i = startIndex; i < max; i++)
            {
                if (i > startIndex)
                    sb.Append(">");

                sb.Append("(");
                sb.Append(path[i].X);
                sb.Append(",");
                sb.Append(path[i].Y);
                sb.Append(")");
            }

            if (max < path.Count)
                sb.Append(">...");

            sb.Append("] total=");
            sb.Append(path.Count);
            sb.Append(" start=");
            sb.Append(startIndex);
            sb.Append(" next=");
            sb.Append(nextPathIndex);
            return sb.ToString();
        }

        // =============================================================================
        // Safe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza stringhe diagnostiche vuote in un trattino leggibile.
        /// </para>
        /// </summary>
        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna gli overlay attivi durante il runtime.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!processInUpdate || !HasAnyOverlayEnabled())
                return;

            ProcessFrame(forceRender: false);
        }

        // =============================================================================
        // SetRuntimeContextProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la sorgente context autorizzata.
        /// </para>
        /// </summary>
        public void SetRuntimeContextProvider(ArcGraphRuntimeContextProvider provider)
        {
            runtimeContextProvider = provider;
        }

        // =============================================================================
        // SetOverlayConsumer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il consumer visuale della queue debug Landmark/Path.
        /// </para>
        /// </summary>
        public void SetOverlayConsumer(ArcGraphDebugOverlaySceneProbeRenderer consumer)
        {
            overlayConsumer = consumer;
        }

        // =============================================================================
        // SetProcessInUpdate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Decide se il controller deve aggiornare automaticamente gli overlay.
        /// </para>
        /// </summary>
        public void SetProcessInUpdate(bool enabled)
        {
            processInUpdate = enabled;
        }

        // =============================================================================
        // SetLandmarkOverlayEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il layer landmark generale.
        /// </para>
        /// </summary>
        public void SetLandmarkOverlayEnabled(bool enabled)
        {
            _landmarkSetCallCount++;
            landmarkOverlayEnabled = enabled;
            RefreshOrClear();
        }

        // =============================================================================
        // SetPathfindingOverlayEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il layer route/pathfinding.
        /// </para>
        /// </summary>
        public void SetPathfindingOverlayEnabled(bool enabled)
        {
            _pathfindingSetCallCount++;
            pathfindingOverlayEnabled = enabled;
            RefreshOrClear();
        }

        // =============================================================================
        // ProcessFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue filtrata e la invia al renderer.
        /// </para>
        /// </summary>
        public void ProcessFrame()
        {
            ProcessFrame(forceRender: true);
        }

        private void ProcessFrame(bool forceRender)
        {
            _processFrameCallCount++;

            if (!HasAnyOverlayEnabled())
            {
                overlayConsumer?.ClearProbe();
                ResetRenderMarkers();
                _lastProcessReason = "NoOverlayEnabled";
                return;
            }

            if (runtimeContextProvider == null || overlayConsumer == null)
            {
                overlayConsumer?.ClearProbe();
                ResetRenderMarkers();
                _lastProcessReason = "RuntimeBindingMissing";
                Log("RuntimeBindingMissing");
                return;
            }

            ArcGraphRuntimeContext context = runtimeContextProvider.BuildTerrainRuntimeContext();
            World world = context?.World;
            if (world == null)
            {
                overlayConsumer.ClearProbe();
                ResetRenderMarkers();
                _lastProcessReason = "WorldMissing";
                Log("WorldMissing");
                return;
            }

            int activeNpcId = ResolveSelectedNpcId(world);
            if (pathfindingOverlayEnabled && activeNpcId <= 0 && !landmarkOverlayEnabled)
            {
                overlayConsumer.ClearProbe();
                ResetRenderMarkers();
                _lastProcessReason = "SelectedNpcMissing";
                Log("SelectedNpcMissing");
                return;
            }

            long tick = world.Global.CurrentTickIndex;
            // Il layer landmark puro puo' saltare frame identici: i nodi statici
            // non cambiano di continuo. Il layer pathfinding, invece, deve leggere
            // ogni Update quando resta acceso, perche' il Job MoveTo aggiorna route
            // e DirectCommit mentre il toggle UI e l'NPC selezionato restano uguali.
            if (!pathfindingOverlayEnabled
                && !forceRender
                && tick == _lastRenderedTick
                && activeNpcId == _lastRenderedNpcId
                && landmarkOverlayEnabled == _lastRenderedLandmarkOverlayEnabled
                && pathfindingOverlayEnabled == _lastRenderedPathfindingOverlayEnabled
                && overlayConsumer.HasProbeRoot())
            {
                _lastProcessReason = "SkippedSameFrame";
                return;
            }

            ConfigureOptions();
            overlayConsumer.SetTileWorldSize(context.TileSizeWorld);
            overlayConsumer.SetPlaceProbeAtSceneCameraCenter(false);
            overlayConsumer.SetLogDiagnostics(false);

            ArcGraphDebugOverlayRuntimeFeedDiagnostics diagnostics =
                _feed.BuildFromWorld(world, activeNpcId, _options);
            overlayConsumer.RenderQueue(_feed.Queue);
            _lastProcessReason = diagnostics.Reason;
            _lastRenderedTick = tick;
            _lastRenderedNpcId = activeNpcId;
            _lastRenderedLandmarkOverlayEnabled = landmarkOverlayEnabled;
            _lastRenderedPathfindingOverlayEnabled = pathfindingOverlayEnabled;

            Log(
                diagnostics.Reason +
                " npc=" + activeNpcId +
                ", landmark=" + landmarkOverlayEnabled +
                ", path=" + pathfindingOverlayEnabled +
                ", visible=" + diagnostics.QueueDiagnostics.VisibleItemCount);
        }

        private void RefreshOrClear()
        {
            if (!HasAnyOverlayEnabled())
            {
                overlayConsumer?.ClearProbe();
                ResetRenderMarkers();
                return;
            }

            ProcessFrame(forceRender: true);
        }

        private void ResetRenderMarkers()
        {
            _lastRenderedTick = long.MinValue;
            _lastRenderedNpcId = -1;
            _lastRenderedLandmarkOverlayEnabled = false;
            _lastRenderedPathfindingOverlayEnabled = false;
        }

        private void ConfigureOptions()
        {
            bool hasAny = HasAnyOverlayEnabled();

            // Il producer Core fornisce landmark e path in una sola chiamata.
            // La separazione avviene qui, tramite opzioni, prima del rendering.
            _options.IncludeLandmark = hasAny;
            _options.IncludeLandmarkGraph = landmarkOverlayEnabled;
            _options.IncludeLandmarkGraphEdges = false;
            _options.IncludeKnownLandmarkGraph = false;
            _options.IncludeLandmarkRoute = pathfindingOverlayEnabled;
            _options.IncludeLandmarkPaths = pathfindingOverlayEnabled;
            _options.IncludeLandmarkComplexEdges = false;
            _options.IncludeGvdDin = false;
            _options.IncludeDtHeatmap = false;
            _options.IncludeGvdRaw = false;
            _options.IncludeGvdGraph = false;
        }

        private bool HasAnyOverlayEnabled()
        {
            return landmarkOverlayEnabled || pathfindingOverlayEnabled;
        }

        private static int ResolveSelectedNpcId(World world)
        {
            if (world == null)
                return -1;

            int selectedNpcId = NPCSelection.SelectedNpcId;
            if (selectedNpcId > 0 && world.ExistsNpc(selectedNpcId))
                return selectedNpcId;

            return -1;
        }

        private void Log(string reason)
        {
            if (!logDiagnostics)
                return;

            Debug.Log("[ArcGraphLandmarkPathDebugOverlayRuntimeController] " + reason);
        }
    }
}
