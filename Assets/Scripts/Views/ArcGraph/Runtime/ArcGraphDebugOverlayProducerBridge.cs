using System.Collections.Generic;
using Arcontio.Core;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayProducerBridgeDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del bridge producer debug verso ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verificare il ponte senza renderizzare</b></para>
    /// <para>
    /// La diagnostica conta quanti DTO sorgente sono stati letti e quanti snapshot
    /// ArcGraph sono stati aggiunti. Non misura frame time, non conosce Unity e non
    /// decide se un overlay debba essere mostrato a schermo: serve solo a rendere
    /// controllabile la conversione passiva.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Source*</b>: quantita' lette dai DTO Core.</item>
    ///   <item><b>Added*</b>: quantita' aggiunte allo snapshot ArcGraph.</item>
    ///   <item><b>WasGvdValid</b>: stato dichiarato dallo snapshot GVD-DIN sorgente.</item>
    ///   <item><b>Reason</b>: esito sintetico del bridge.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugOverlayProducerBridgeDiagnostics
    {
        public readonly int SourceNodeCount;
        public readonly int SourceEdgeCount;
        public readonly int SourceDtCellCount;
        public readonly int SourceGvdRawCellCount;
        public readonly int AddedNodeCount;
        public readonly int AddedEdgeCount;
        public readonly int AddedCellCount;
        public readonly bool WasGvdValid;
        public readonly string Reason;

        public int TotalAddedItemCount => AddedNodeCount + AddedEdgeCount + AddedCellCount;

        public ArcGraphDebugOverlayProducerBridgeDiagnostics(
            int sourceNodeCount,
            int sourceEdgeCount,
            int sourceDtCellCount,
            int sourceGvdRawCellCount,
            int addedNodeCount,
            int addedEdgeCount,
            int addedCellCount,
            bool wasGvdValid,
            string reason)
        {
            SourceNodeCount = NormalizeCount(sourceNodeCount);
            SourceEdgeCount = NormalizeCount(sourceEdgeCount);
            SourceDtCellCount = NormalizeCount(sourceDtCellCount);
            SourceGvdRawCellCount = NormalizeCount(sourceGvdRawCellCount);
            AddedNodeCount = NormalizeCount(addedNodeCount);
            AddedEdgeCount = NormalizeCount(addedEdgeCount);
            AddedCellCount = NormalizeCount(addedCellCount);
            WasGvdValid = wasGvdValid;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }

    // =============================================================================
    // ArcGraphDebugOverlayProducerBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Bridge passivo tra DTO debug esistenti del Core e snapshot debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: producer bridge senza nuova authority</b></para>
    /// <para>
    /// Il bridge non legge <c>World</c>, non consulta <c>MapGridWorldView</c>, non
    /// calcola pathfinding, non calcola FOV corrente e non crea oggetti Unity.
    /// Riceve liste gia' prodotte da altri sistemi e le converte in
    /// <c>ArcGraphDebugOverlaySnapshot</c>. In questo modo ArcGraph diventa un
    /// consumer visuale dei contratti debug, non un secondo manager diagnostico.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FillLandmarkDebugSnapshot</b>: converte nodi/edge landmark e path.</item>
    ///   <item><b>FillGvdDinDebugSnapshot</b>: converte DT, GVD raw, nodi GVD e edge GVD.</item>
    ///   <item><b>AppendNodes/AppendEdges</b>: helper lineari senza allocazioni complesse.</item>
    ///   <item><b>AppendDtCells/AppendGvdRawCells</b>: helper cell-based per GVD-DIN.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugOverlayProducerBridge
    {
        private const float WorldNodeScale = 0.35f;
        private const float KnownNodeScale = 0.48f;
        private const float RouteNodeScale = 0.62f;
        private const float GvdNodeScale = 0.50f;

        // =============================================================================
        // FillLandmarkDebugSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte i DTO landmark/path gia' prodotti dal Core in snapshot ArcGraph.
        /// </para>
        ///
        /// <para><b>Bridge read-only</b></para>
        /// <para>
        /// Il metodo accetta le stesse famiglie concettuali oggi usate da
        /// <c>World.GetNpcLandmarkOverlayData(...)</c>, ma non chiama quel metodo e
        /// non riceve il <c>World</c>. Il chiamante resta responsabile di produrre i
        /// DTO; questo bridge li classifica soltanto in kind ArcGraph.
        /// </para>
        /// </summary>
        public ArcGraphDebugOverlayProducerBridgeDiagnostics FillLandmarkDebugSnapshot(
            ArcGraphDebugOverlaySnapshot target,
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
            bool clearTarget = false,
            bool isEnabled = true,
            bool includeLandmarkGraph = true,
            bool includeLandmarkGraphEdges = true,
            bool includeKnownLandmarkGraph = true,
            bool includeLandmarkRoute = true,
            bool includeLandmarkPaths = true)
        {
            if (target == null)
                return CreateMissingTargetDiagnostics("TargetMissing");

            if (clearTarget)
                target.Clear();

            int addedNodes = 0;
            int addedEdges = 0;

            // Ogni gruppo conserva il proprio significato diagnostico. Il bridge non
            // prova a dedurre il tipo dal contenuto dei nodi, cosi' resta stabile.
            // I flag permettono alla UI ArcGraph di attivare Landmark e Pathfinding
            // come overlay sovrapponibili distinti, pur usando lo stesso DTO Core.
            if (includeLandmarkGraph)
            {
                addedNodes += AppendNodes(target, worldNodes, ArcGraphDebugOverlayKind.LandmarkWorldNode, WorldNodeScale, isEnabled);
                if (includeLandmarkGraphEdges)
                    addedEdges += AppendEdges(target, worldEdges, ArcGraphDebugOverlayKind.LandmarkWorldEdge, isEnabled);

                if (includeKnownLandmarkGraph)
                {
                    addedNodes += AppendNodes(target, knownNodes, ArcGraphDebugOverlayKind.LandmarkKnownNode, KnownNodeScale, isEnabled);
                    if (includeLandmarkGraphEdges)
                        addedEdges += AppendEdges(target, knownEdges, ArcGraphDebugOverlayKind.LandmarkKnownEdge, isEnabled);
                }
            }

            if (includeLandmarkRoute)
            {
                addedNodes += AppendNodes(target, routeNodes, ArcGraphDebugOverlayKind.LandmarkRouteNode, RouteNodeScale, isEnabled);
                addedEdges += AppendEdges(target, routeEdges, ArcGraphDebugOverlayKind.LandmarkRouteEdge, isEnabled);
            }

            if (includeLandmarkPaths)
            {
                addedEdges += AppendEdges(target, lmPathEdges, ArcGraphDebugOverlayKind.LandmarkLmPathEdge, isEnabled);
                addedEdges += AppendEdges(target, directPathEdges, ArcGraphDebugOverlayKind.LandmarkDirectPathEdge, isEnabled);
                addedEdges += AppendEdges(target, jumpPathEdges, ArcGraphDebugOverlayKind.LandmarkJumpPathEdge, isEnabled);
                addedEdges += AppendEdges(target, complexEdges, ArcGraphDebugOverlayKind.LandmarkComplexEdge, isEnabled);
            }

            int sourceNodes = Count(worldNodes) + Count(knownNodes) + Count(routeNodes);
            int sourceEdges = Count(worldEdges)
                              + Count(knownEdges)
                              + Count(routeEdges)
                              + Count(lmPathEdges)
                              + Count(directPathEdges)
                              + Count(jumpPathEdges)
                              + Count(complexEdges);

            return new ArcGraphDebugOverlayProducerBridgeDiagnostics(
                sourceNodes,
                sourceEdges,
                0,
                0,
                addedNodes,
                addedEdges,
                0,
                false,
                "LandmarkDebugSnapshotFilled");
        }

        // =============================================================================
        // FillGvdDinDebugSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte uno <c>GvdDinOverlaySnapshot</c> in snapshot debug ArcGraph.
        /// </para>
        ///
        /// <para><b>GVD-DIN come DTO gia' pronto</b></para>
        /// <para>
        /// Il metodo non chiede al registry di calcolare nulla. Se lo snapshot non
        /// e' valido, non aggiunge item e restituisce diagnostica esplicita. Se e'
        /// valido, copia DT, GVD raw, nodi GVD e edge GVD nei contratti ArcGraph.
        /// </para>
        /// </summary>
        public ArcGraphDebugOverlayProducerBridgeDiagnostics FillGvdDinDebugSnapshot(
            ArcGraphDebugOverlaySnapshot target,
            GvdDinOverlaySnapshot source,
            bool clearTarget = false,
            bool includeDtHeatmap = true,
            bool includeGvdRaw = true,
            bool includeGvdGraph = true,
            bool isEnabled = true)
        {
            if (target == null)
                return CreateMissingTargetDiagnostics("TargetMissing");

            if (clearTarget)
                target.Clear();

            if (source == null)
                return CreateMissingTargetDiagnostics("GvdSnapshotMissing");

            if (!source.IsValid)
            {
                return new ArcGraphDebugOverlayProducerBridgeDiagnostics(
                    Count(source.GvdNodes),
                    Count(source.GvdEdges),
                    Count(source.DtCells),
                    Count(source.GvdRawCells),
                    0,
                    0,
                    0,
                    false,
                    "GvdSnapshotInvalid");
            }

            int addedCells = 0;
            int addedNodes = 0;
            int addedEdges = 0;

            if (includeDtHeatmap)
                addedCells += AppendDtCells(target, source.DtCells, isEnabled);

            if (includeGvdRaw)
                addedCells += AppendGvdRawCells(target, source.GvdRawCells, isEnabled);

            if (includeGvdGraph)
            {
                addedNodes += AppendNodes(target, source.GvdNodes, ArcGraphDebugOverlayKind.LandmarkGvdNode, GvdNodeScale, isEnabled);
                addedEdges += AppendEdges(target, source.GvdEdges, ArcGraphDebugOverlayKind.LandmarkGvdEdge, isEnabled);
            }

            return new ArcGraphDebugOverlayProducerBridgeDiagnostics(
                Count(source.GvdNodes),
                Count(source.GvdEdges),
                Count(source.DtCells),
                Count(source.GvdRawCells),
                addedNodes,
                addedEdges,
                addedCells,
                true,
                "GvdDinDebugSnapshotFilled");
        }

        private static int AppendNodes(
            ArcGraphDebugOverlaySnapshot target,
            IReadOnlyList<LandmarkOverlayNode> nodes,
            ArcGraphDebugOverlayKind kind,
            float scale01,
            bool isEnabled)
        {
            if (nodes == null || nodes.Count == 0)
                return 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                LandmarkOverlayNode node = nodes[i];

                // Il nodo Core e' gia' un DTO view-only. Qui aggiungiamo solo lo
                // z-level corrente e il kind ArcGraph assegnato dal chiamante.
                target.AddNode(new ArcGraphDebugNodeOverlaySnapshot(
                    ArcGraphZLevelPolicy.CreateRuntimeCell(node.CellX, node.CellY),
                    ResolveNodeOverlayKind(kind, node),
                    node.NodeId,
                    ResolveNodeLabel(kind, node),
                    scale01,
                    ResolveNodeColorKey(kind, node),
                    isEnabled));
            }

            return nodes.Count;
        }

        // =============================================================================
        // ResolveNodeLabel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce l'etichetta breve da consegnare alla view per un nodo landmark.
        /// </para>
        ///
        /// <para><b>Principio architetturale: fallback visuale senza interrogare il World</b></para>
        /// <para>
        /// Il bridge usa prima la label gia' preparata dal Core. Se quella label non
        /// e' disponibile, costruisce un fallback minimo usando solo i campi del DTO
        /// view-only ricevuto. In questo modo la UI resta consumer passiva e non deve
        /// conoscere il registry o cercare dati aggiuntivi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Label sorgente</b>: mantiene il testo se il Core lo ha fornito.</item>
        ///   <item><b>Filtro layer</b>: produce fallback solo per il grafo landmark oggettivo.</item>
        ///   <item><b>Prefissi brevi</b>: D/A/B/J per doorway, area, biological anchor e junction.</item>
        /// </list>
        /// </summary>
        private static string ResolveNodeLabel(
            ArcGraphDebugOverlayKind fallback,
            LandmarkOverlayNode node)
        {
            if (!string.IsNullOrWhiteSpace(node.Label))
                return node.Label;

            if (fallback != ArcGraphDebugOverlayKind.LandmarkWorldNode)
                return string.Empty;

            if (node.Kind == (int)LandmarkRegistry.LandmarkKind.Doorway)
                return "D#" + node.NodeId;

            if (node.Kind == (int)LandmarkRegistry.LandmarkKind.AreaCenter)
                return "A#" + node.NodeId;

            if (node.Kind == (int)LandmarkRegistry.LandmarkKind.BiologicalAnchor)
                return "B#" + node.NodeId;

            return "J#" + node.NodeId;
        }

        private static ArcGraphDebugOverlayKind ResolveNodeOverlayKind(
            ArcGraphDebugOverlayKind fallback,
            LandmarkOverlayNode node)
        {
            if (node.Kind == (int)LandmarkRegistry.LandmarkKind.BiologicalAnchor)
                return ArcGraphDebugOverlayKind.LandmarkBiologicalNode;

            return fallback;
        }

        private static string ResolveNodeColorKey(
            ArcGraphDebugOverlayKind fallback,
            LandmarkOverlayNode node)
        {
            if (fallback != ArcGraphDebugOverlayKind.LandmarkWorldNode)
                return null;

            if (node.Kind == (int)LandmarkRegistry.LandmarkKind.Doorway)
                return "debug/landmark/doorway";

            if (node.Kind == (int)LandmarkRegistry.LandmarkKind.Junction)
                return "debug/landmark/junction";

            if (node.Kind == (int)LandmarkRegistry.LandmarkKind.AreaCenter)
                return "debug/landmark/area-center";

            if (node.Kind == (int)LandmarkRegistry.LandmarkKind.BiologicalAnchor)
                return "debug/landmark/biological-anchor";

            return null;
        }

        private static int AppendEdges(
            ArcGraphDebugOverlaySnapshot target,
            IReadOnlyList<LandmarkOverlayEdge> edges,
            ArcGraphDebugOverlayKind kind,
            bool isEnabled)
        {
            if (edges == null || edges.Count == 0)
                return 0;

            for (int i = 0; i < edges.Count; i++)
            {
                LandmarkOverlayEdge edge = edges[i];

                // Gli estremi restano celle discrete. Eventuali edge degeneri saranno
                // marcati hidden dal QueueBuilder, non corretti qui in modo silenzioso.
                target.AddEdge(new ArcGraphDebugEdgeOverlaySnapshot(
                    ArcGraphZLevelPolicy.CreateRuntimeCell(edge.Ax, edge.Ay),
                    ArcGraphZLevelPolicy.CreateRuntimeCell(edge.Bx, edge.By),
                    kind,
                    edge.Reliability01,
                    null,
                    null,
                    isEnabled));
            }

            return edges.Count;
        }

        private static int AppendDtCells(
            ArcGraphDebugOverlaySnapshot target,
            IReadOnlyList<GvdDinOverlayCellDt> cells,
            bool isEnabled)
        {
            if (cells == null || cells.Count == 0)
                return 0;

            for (int i = 0; i < cells.Count; i++)
            {
                GvdDinOverlayCellDt cell = cells[i];

                target.AddCell(new ArcGraphDebugCellOverlaySnapshot(
                    ArcGraphZLevelPolicy.CreateRuntimeCell(cell.CellX, cell.CellY),
                    ArcGraphDebugOverlayKind.DtHeatCell,
                    cell.DtNormalized01,
                    cell.DtValue,
                    null,
                    isEnabled));
            }

            return cells.Count;
        }

        private static int AppendGvdRawCells(
            ArcGraphDebugOverlaySnapshot target,
            IReadOnlyList<GvdDinOverlayCellGvd> cells,
            bool isEnabled)
        {
            if (cells == null || cells.Count == 0)
                return 0;

            for (int i = 0; i < cells.Count; i++)
            {
                GvdDinOverlayCellGvd cell = cells[i];

                // NumericValue conserva ObstacleA come minimo indizio diagnostico.
                // ObstacleB resta nel DTO Core; ArcGraph non amplia il contratto qui.
                target.AddCell(new ArcGraphDebugCellOverlaySnapshot(
                    ArcGraphZLevelPolicy.CreateRuntimeCell(cell.CellX, cell.CellY),
                    ArcGraphDebugOverlayKind.GvdRawCell,
                    1f,
                    cell.ObstacleA,
                    null,
                    isEnabled));
            }

            return cells.Count;
        }

        private static ArcGraphDebugOverlayProducerBridgeDiagnostics CreateMissingTargetDiagnostics(string reason)
        {
            return new ArcGraphDebugOverlayProducerBridgeDiagnostics(
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                reason);
        }

        private static int Count<T>(IReadOnlyList<T> items)
        {
            return items == null ? 0 : items.Count;
        }
    }
}
