using System.Collections.Generic;
using Arcontio.Core;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayRuntimeFeed
    // =============================================================================
    /// <summary>
    /// <para>
    /// Feed runtime passivo per produrre la queue debug ArcGraph da dati Core reali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: lettura debug read-only separata dal renderer</b></para>
    /// <para>
    /// Questo modulo e' il primo ponte runtime tra <c>World</c> e gli overlay debug
    /// ArcGraph. Legge il mondo solo tramite producer view/debug gia' esistenti,
    /// copia DTO in liste riusabili, delega la conversione al producer bridge e
    /// poi normalizza tutto in <c>ArcGraphDebugOverlayQueue</c>. Non crea oggetti
    /// Unity, non conosce input, non consulta <c>MapGridWorldView</c> e non muta la
    /// simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_snapshot</b>: frame debug ArcGraph riusabile.</item>
    ///   <item><b>_queue</b>: output normalizzato per renderer futuri.</item>
    ///   <item><b>_bridge</b>: conversione DTO Core -> snapshot ArcGraph.</item>
    ///   <item><b>_queueBuilder</b>: conversione snapshot -> queue ordinata.</item>
    ///   <item><b>liste landmark</b>: buffer riusabili per evitare allocazioni per frame.</item>
    ///   <item><b>_gvdSnapshot</b>: buffer GVD-DIN riusabile.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugOverlayRuntimeFeed
    {
        private readonly ArcGraphDebugOverlaySnapshot _snapshot = new ArcGraphDebugOverlaySnapshot();
        private readonly ArcGraphDebugOverlayQueue _queue = new ArcGraphDebugOverlayQueue();
        private readonly ArcGraphDebugOverlayProducerBridge _bridge = new ArcGraphDebugOverlayProducerBridge();
        private readonly ArcGraphDebugOverlayQueueBuilder _queueBuilder = new ArcGraphDebugOverlayQueueBuilder();

        private readonly List<LandmarkOverlayNode> _worldNodes = new List<LandmarkOverlayNode>(128);
        private readonly List<LandmarkOverlayEdge> _worldEdges = new List<LandmarkOverlayEdge>(256);
        private readonly List<LandmarkOverlayNode> _knownNodes = new List<LandmarkOverlayNode>(64);
        private readonly List<LandmarkOverlayEdge> _knownEdges = new List<LandmarkOverlayEdge>(128);
        private readonly List<LandmarkOverlayNode> _routeNodes = new List<LandmarkOverlayNode>(32);
        private readonly List<LandmarkOverlayEdge> _routeEdges = new List<LandmarkOverlayEdge>(64);
        private readonly List<LandmarkOverlayEdge> _lmPathEdges = new List<LandmarkOverlayEdge>(128);
        private readonly List<LandmarkOverlayEdge> _directPathEdges = new List<LandmarkOverlayEdge>(128);
        private readonly List<LandmarkOverlayEdge> _jumpPathEdges = new List<LandmarkOverlayEdge>(128);
        private readonly List<LandmarkOverlayEdge> _complexEdges = new List<LandmarkOverlayEdge>(128);
        private readonly GvdDinOverlaySnapshot _gvdSnapshot = new GvdDinOverlaySnapshot();

        public ArcGraphDebugOverlaySnapshot Snapshot => _snapshot;
        public ArcGraphDebugOverlayQueue Queue => _queue;
        public ArcGraphDebugOverlayRuntimeFeedDiagnostics LastDiagnostics { get; private set; }

        // =============================================================================
        // BuildFromWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce una queue debug ArcGraph leggendo producer read-only del mondo.
        /// </para>
        ///
        /// <para><b>Accesso al World limitato</b></para>
        /// <para>
        /// Il metodo chiama solo <c>World.GetNpcLandmarkOverlayData(...)</c> e
        /// <c>World.GetGvdDinOverlayData(...)</c>. Non calcola pathfinding, non
        /// calcola FOV, non risolve l'NPC attivo dal mouse e non scrive in alcuno
        /// store. Il chiamante deve fornire <c>activeNpcId</c> gia' deciso.
        /// </para>
        /// </summary>
        public ArcGraphDebugOverlayRuntimeFeedDiagnostics BuildFromWorld(
            World world,
            int activeNpcId,
            ArcGraphDebugOverlayRuntimeFeedOptions options = null)
        {
            options ??= ArcGraphDebugOverlayRuntimeFeedOptions.CreateDefault();

            ClearRuntimeBuffers();

            // Anche in assenza di World produciamo una queue vuota: il renderer
            // futuro potra' consumarla senza branch speciali o null check esterni.
            if (world == null)
            {
                LastDiagnostics = BuildQueueDiagnostics(
                    false,
                    activeNpcId,
                    options.IncludeLandmark,
                    false,
                    options.IncludeGvdDin,
                    false,
                    default,
                    default,
                    options,
                    "WorldMissing");
                return LastDiagnostics;
            }

            bool landmarkAttempted = false;
            bool gvdAttempted = false;
            ArcGraphDebugOverlayProducerBridgeDiagnostics landmarkDiagnostics = default;
            ArcGraphDebugOverlayProducerBridgeDiagnostics gvdDiagnostics = default;

            if (options.IncludeLandmark && (activeNpcId > 0 || options.IncludeLandmarkGraph))
            {
                // Il World riempie liste gia' possedute dal feed. Le liste vengono
                // riusate tra frame e vengono pulite dal producer stesso.
                world.GetNpcLandmarkOverlayData(
                    activeNpcId,
                    _worldNodes,
                    _worldEdges,
                    _knownNodes,
                    _knownEdges,
                    _routeNodes,
                    _routeEdges,
                    _lmPathEdges,
                    _directPathEdges,
                    _jumpPathEdges,
                    _complexEdges,
                    out _);

                landmarkAttempted = true;
                landmarkDiagnostics = _bridge.FillLandmarkDebugSnapshot(
                    _snapshot,
                    _worldNodes,
                    _worldEdges,
                    _knownNodes,
                    _knownEdges,
                    _routeNodes,
                    _routeEdges,
                    _lmPathEdges,
                    _directPathEdges,
                    _jumpPathEdges,
                    options.IncludeLandmarkComplexEdges ? _complexEdges : null,
                    false,
                    true,
                    options.IncludeLandmarkGraph,
                    options.IncludeLandmarkGraphEdges,
                    options.IncludeKnownLandmarkGraph,
                    options.IncludeLandmarkRoute,
                    options.IncludeLandmarkPaths);
            }

            if (options.IncludeGvdDin)
            {
                // GVD-DIN e' globale, quindi non dipende da activeNpcId. Se il Core
                // non ha dati validi, il bridge restituisce una diagnostica vuota.
                world.GetGvdDinOverlayData(_gvdSnapshot);
                gvdAttempted = true;
                gvdDiagnostics = _bridge.FillGvdDinDebugSnapshot(
                    _snapshot,
                    _gvdSnapshot,
                    false,
                    options.IncludeDtHeatmap,
                    options.IncludeGvdRaw,
                    options.IncludeGvdGraph);
            }

            LastDiagnostics = BuildQueueDiagnostics(
                true,
                activeNpcId,
                options.IncludeLandmark,
                landmarkAttempted,
                options.IncludeGvdDin,
                gvdAttempted,
                landmarkDiagnostics,
                gvdDiagnostics,
                options,
                ResolveWorldReason(world, activeNpcId, options, landmarkAttempted, gvdAttempted));

            return LastDiagnostics;
        }

        // =============================================================================
        // BuildFromPreparedDebugData
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce una queue debug usando DTO gia' preparati dal chiamante.
        /// </para>
        ///
        /// <para><b>Uso previsto: harness e futuri producer intermedi</b></para>
        /// <para>
        /// Questo overload non legge il <c>World</c>. Serve per verificare il feed
        /// in isolamento e per consentire, in futuro, a un producer esterno di
        /// consegnare DTO gia' raccolti senza passare di nuovo dal mondo.
        /// </para>
        /// </summary>
        public ArcGraphDebugOverlayRuntimeFeedDiagnostics BuildFromPreparedDebugData(
            IReadOnlyList<LandmarkOverlayNode> worldNodes,
            IReadOnlyList<LandmarkOverlayEdge> worldEdges,
            IReadOnlyList<LandmarkOverlayNode> knownNodes,
            IReadOnlyList<LandmarkOverlayEdge> knownEdges,
            IReadOnlyList<LandmarkOverlayNode> routeNodes,
            IReadOnlyList<LandmarkOverlayEdge> routeEdges,
            IReadOnlyList<LandmarkOverlayEdge> lmPathEdges,
            IReadOnlyList<LandmarkOverlayEdge> directPathEdges,
            IReadOnlyList<LandmarkOverlayEdge> jumpPathEdges,
            IReadOnlyList<LandmarkOverlayEdge> complexEdges,
            GvdDinOverlaySnapshot gvdSnapshot,
            ArcGraphDebugOverlayRuntimeFeedOptions options = null)
        {
            options ??= ArcGraphDebugOverlayRuntimeFeedOptions.CreateDefault();

            ClearRuntimeBuffers();

            ArcGraphDebugOverlayProducerBridgeDiagnostics landmarkDiagnostics = default;
            ArcGraphDebugOverlayProducerBridgeDiagnostics gvdDiagnostics = default;
            bool landmarkAttempted = options.IncludeLandmark;
            bool gvdAttempted = options.IncludeGvdDin && gvdSnapshot != null;

            if (options.IncludeLandmark)
            {
                landmarkDiagnostics = _bridge.FillLandmarkDebugSnapshot(
                    _snapshot,
                    worldNodes,
                    worldEdges,
                    knownNodes,
                    knownEdges,
                    routeNodes,
                    routeEdges,
                    lmPathEdges,
                    directPathEdges,
                    jumpPathEdges,
                    options.IncludeLandmarkComplexEdges ? complexEdges : null,
                    false,
                    true,
                    options.IncludeLandmarkGraph,
                    options.IncludeLandmarkGraphEdges,
                    options.IncludeKnownLandmarkGraph,
                    options.IncludeLandmarkRoute,
                    options.IncludeLandmarkPaths);
            }

            if (options.IncludeGvdDin)
            {
                gvdDiagnostics = _bridge.FillGvdDinDebugSnapshot(
                    _snapshot,
                    gvdSnapshot,
                    false,
                    options.IncludeDtHeatmap,
                    options.IncludeGvdRaw,
                    options.IncludeGvdGraph);
            }

            LastDiagnostics = BuildQueueDiagnostics(
                false,
                -1,
                options.IncludeLandmark,
                landmarkAttempted,
                options.IncludeGvdDin,
                gvdAttempted,
                landmarkDiagnostics,
                gvdDiagnostics,
                options,
                _snapshot.TotalItemCount > 0 ? "PreparedDebugRuntimeFeedBuilt" : "PreparedDebugRuntimeFeedEmpty");

            return LastDiagnostics;
        }

        private void ClearRuntimeBuffers()
        {
            _snapshot.Clear();
            _queue.Clear();

            _worldNodes.Clear();
            _worldEdges.Clear();
            _knownNodes.Clear();
            _knownEdges.Clear();
            _routeNodes.Clear();
            _routeEdges.Clear();
            _lmPathEdges.Clear();
            _directPathEdges.Clear();
            _jumpPathEdges.Clear();
            _complexEdges.Clear();
            _gvdSnapshot.Clear();
        }

        private ArcGraphDebugOverlayRuntimeFeedDiagnostics BuildQueueDiagnostics(
            bool wasWorldProvided,
            int activeNpcId,
            bool landmarkRequested,
            bool landmarkAttempted,
            bool gvdRequested,
            bool gvdAttempted,
            ArcGraphDebugOverlayProducerBridgeDiagnostics landmarkDiagnostics,
            ArcGraphDebugOverlayProducerBridgeDiagnostics gvdDiagnostics,
            ArcGraphDebugOverlayRuntimeFeedOptions options,
            string reason)
        {
            // Il builder resta l'unico punto che decide ordinamento, hidden state
            // e normalizzazione degli item. Il feed produce soltanto input.
            ArcGraphDebugOverlayQueueDiagnostics queueDiagnostics = _queueBuilder.Build(
                _snapshot,
                _queue,
                true,
                options != null && options.IncludeHiddenItems);

            return new ArcGraphDebugOverlayRuntimeFeedDiagnostics(
                wasWorldProvided,
                activeNpcId,
                landmarkRequested,
                landmarkAttempted,
                gvdRequested,
                gvdAttempted,
                landmarkDiagnostics.SourceNodeCount,
                landmarkDiagnostics.SourceEdgeCount,
                gvdDiagnostics.SourceDtCellCount,
                gvdDiagnostics.SourceGvdRawCellCount,
                _snapshot.TotalItemCount,
                queueDiagnostics,
                reason);
        }

        private static string ResolveWorldReason(
            World world,
            int activeNpcId,
            ArcGraphDebugOverlayRuntimeFeedOptions options,
            bool landmarkAttempted,
            bool gvdAttempted)
        {
            if (world == null)
                return "WorldMissing";

            if (options != null && options.IncludeLandmark && activeNpcId <= 0 && !gvdAttempted)
                return "ActiveNpcMissing";

            if (!landmarkAttempted && !gvdAttempted)
                return "NoDebugProducerRequested";

            return "RuntimeDebugFeedBuilt";
        }
    }
}
