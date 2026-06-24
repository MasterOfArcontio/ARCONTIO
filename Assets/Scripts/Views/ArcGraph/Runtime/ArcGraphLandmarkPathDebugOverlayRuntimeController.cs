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
                probe;
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
            if (!forceRender
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
