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
        [SerializeField] private ArcGraphDebugOverlaySceneProbeRenderer landmarkOverlayConsumer;
        [SerializeField] private ArcGraphDebugOverlaySceneProbeRenderer pathfindingOverlayConsumer;
        [SerializeField] private bool landmarkOverlayEnabled;
        [SerializeField] private bool pathfindingOverlayEnabled;
        [SerializeField] private bool processInUpdate = true;
        [SerializeField] private bool logDiagnostics;

        private readonly ArcGraphDebugOverlayRuntimeFeed _landmarkFeed = new ArcGraphDebugOverlayRuntimeFeed();
        private readonly ArcGraphDebugOverlayRuntimeFeed _pathfindingFeed = new ArcGraphDebugOverlayRuntimeFeed();
        private readonly ArcGraphDebugOverlayRuntimeFeedOptions _landmarkOptions = new ArcGraphDebugOverlayRuntimeFeedOptions
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
        private readonly ArcGraphDebugOverlayRuntimeFeedOptions _pathfindingOptions = new ArcGraphDebugOverlayRuntimeFeedOptions
        {
            IncludeLandmark = true,
            IncludeLandmarkGraph = false,
            IncludeLandmarkGraphEdges = false,
            IncludeKnownLandmarkGraph = false,
            IncludeLandmarkRoute = true,
            IncludeLandmarkPaths = true,
            IncludeGvdDin = false,
            IncludeDtHeatmap = false,
            IncludeGvdRaw = false,
            IncludeGvdGraph = false
        };

        private long _lastRenderedTick = long.MinValue;
        private int _lastRenderedNpcId = -1;
        private bool _lastRenderedLandmarkOverlayEnabled;
        private bool _lastRenderedPathfindingOverlayEnabled;

        public bool LandmarkOverlayEnabled => landmarkOverlayEnabled;
        public bool PathfindingOverlayEnabled => pathfindingOverlayEnabled;
        public ArcGraphDebugOverlayRuntimeFeedDiagnostics LastDiagnostics => pathfindingOverlayEnabled
            ? _pathfindingFeed.LastDiagnostics
            : _landmarkFeed.LastDiagnostics;

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
        /// Assegna lo stesso consumer visuale a Landmark e Pathfinding.
        /// </para>
        ///
        /// <para><b>Compatibilita' temporanea</b></para>
        /// <para>
        /// Il metodo resta disponibile per vecchi cablaggi manuali, ma il runtime
        /// ArcGraph usa <see cref="SetLandmarkOverlayConsumer"/> e
        /// <see cref="SetPathfindingOverlayConsumer"/> per tenere i due overlay
        /// davvero separati.
        /// </para>
        /// </summary>
        public void SetOverlayConsumer(ArcGraphDebugOverlaySceneProbeRenderer consumer)
        {
            landmarkOverlayConsumer = consumer;
            pathfindingOverlayConsumer = consumer;
        }

        // =============================================================================
        // SetLandmarkOverlayConsumer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il consumer visuale dedicato ai landmark globali.
        /// </para>
        /// </summary>
        public void SetLandmarkOverlayConsumer(ArcGraphDebugOverlaySceneProbeRenderer consumer)
        {
            landmarkOverlayConsumer = consumer;
        }

        // =============================================================================
        // SetPathfindingOverlayConsumer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il consumer visuale dedicato al pathfinding dell'NPC selezionato.
        /// </para>
        /// </summary>
        public void SetPathfindingOverlayConsumer(ArcGraphDebugOverlaySceneProbeRenderer consumer)
        {
            pathfindingOverlayConsumer = consumer;
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
            if (!HasAnyOverlayEnabled())
            {
                ClearAllConsumers();
                ResetRenderMarkers();
                return;
            }

            if (runtimeContextProvider == null)
            {
                ClearAllConsumers();
                ResetRenderMarkers();
                Log("RuntimeBindingMissing");
                return;
            }

            ArcGraphRuntimeContext context = runtimeContextProvider.BuildTerrainRuntimeContext();
            World world = context?.World;
            if (world == null)
            {
                ClearAllConsumers();
                ResetRenderMarkers();
                Log("WorldMissing");
                return;
            }

            int activeNpcId = ResolveSelectedNpcId(world);
            long tick = world.Global.CurrentTickIndex;
            if (!forceRender
                && tick == _lastRenderedTick
                && activeNpcId == _lastRenderedNpcId
                && landmarkOverlayEnabled == _lastRenderedLandmarkOverlayEnabled
                && pathfindingOverlayEnabled == _lastRenderedPathfindingOverlayEnabled)
            {
                return;
            }

            ArcGraphDebugOverlayRuntimeFeedDiagnostics landmarkDiagnostics =
                RenderLandmarkOverlay(world, context.TileSizeWorld);
            ArcGraphDebugOverlayRuntimeFeedDiagnostics pathDiagnostics =
                RenderPathfindingOverlay(world, activeNpcId, context.TileSizeWorld);

            _lastRenderedTick = tick;
            _lastRenderedNpcId = activeNpcId;
            _lastRenderedLandmarkOverlayEnabled = landmarkOverlayEnabled;
            _lastRenderedPathfindingOverlayEnabled = pathfindingOverlayEnabled;

            Log(
                landmarkDiagnostics.Reason +
                "/" +
                pathDiagnostics.Reason +
                " npc=" + activeNpcId +
                ", landmark=" + landmarkOverlayEnabled +
                ", path=" + pathfindingOverlayEnabled +
                ", landmarkVisible=" + landmarkDiagnostics.QueueDiagnostics.VisibleItemCount +
                ", pathVisible=" + pathDiagnostics.QueueDiagnostics.VisibleItemCount);
        }

        private void RefreshOrClear()
        {
            if (!HasAnyOverlayEnabled())
            {
                ClearAllConsumers();
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

        private ArcGraphDebugOverlayRuntimeFeedDiagnostics RenderLandmarkOverlay(
            World world,
            float tileWorldSize)
        {
            if (!landmarkOverlayEnabled)
            {
                landmarkOverlayConsumer?.ClearProbe();
                return _landmarkFeed.LastDiagnostics;
            }

            if (landmarkOverlayConsumer == null)
                return _landmarkFeed.LastDiagnostics;

            PrepareConsumer(landmarkOverlayConsumer, tileWorldSize);

            // Il layer LM e' globale: non dipende dall'NPC selezionato e quindi
            // usa -1 come id attivo. Le opzioni chiedono solo nodi landmark world,
            // senza grafo noto dell'NPC e senza path.
            ArcGraphDebugOverlayRuntimeFeedDiagnostics diagnostics =
                _landmarkFeed.BuildFromWorld(world, -1, _landmarkOptions);
            landmarkOverlayConsumer.RenderQueue(_landmarkFeed.Queue);
            return diagnostics;
        }

        private ArcGraphDebugOverlayRuntimeFeedDiagnostics RenderPathfindingOverlay(
            World world,
            int activeNpcId,
            float tileWorldSize)
        {
            if (!pathfindingOverlayEnabled || activeNpcId <= 0)
            {
                pathfindingOverlayConsumer?.ClearProbe();
                return _pathfindingFeed.LastDiagnostics;
            }

            if (pathfindingOverlayConsumer == null)
                return _pathfindingFeed.LastDiagnostics;

            PrepareConsumer(pathfindingOverlayConsumer, tileWorldSize);

            // Il layer PATH e' invece legato alla selezione: mostra solo route e
            // path dell'NPC scelto, senza landmark globali. Questo evita che PATH
            // diventi un secondo modo per disegnare anche LM.
            ArcGraphDebugOverlayRuntimeFeedDiagnostics diagnostics =
                _pathfindingFeed.BuildFromWorld(world, activeNpcId, _pathfindingOptions);
            pathfindingOverlayConsumer.RenderQueue(_pathfindingFeed.Queue);
            return diagnostics;
        }

        private static void PrepareConsumer(
            ArcGraphDebugOverlaySceneProbeRenderer consumer,
            float tileWorldSize)
        {
            consumer.SetTileWorldSize(tileWorldSize);
            consumer.SetPlaceProbeAtSceneCameraCenter(false);
            consumer.SetLogDiagnostics(false);
        }

        private void ClearAllConsumers()
        {
            landmarkOverlayConsumer?.ClearProbe();

            if (pathfindingOverlayConsumer != landmarkOverlayConsumer)
                pathfindingOverlayConsumer?.ClearProbe();
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
