using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot debug in <c>ArcGraphDebugOverlayQueue</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: debug overlay senza MapGrid e senza World</b></para>
    /// <para>
    /// Il builder riceve dati gia' preparati dal chiamante e li converte in item
    /// ArcGraph ordinati. Non legge <c>World</c>, non consulta <c>MapGridWorldView</c>,
    /// non crea <c>GameObject</c>, non carica asset e non interpreta input utente.
    /// Questo mantiene il debug come flusso dichiarativo: producer -> snapshot ->
    /// builder -> queue -> renderer futuro.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola la queue debug da uno snapshot.</item>
    ///   <item><b>Create*</b>: normalizza DTO in item value-only.</item>
    ///   <item><b>Resolve*</b>: produce sort key e id stabili senza allocazioni pesanti.</item>
    ///   <item><b>Sort</b>: ordina ogni famiglia di item prima di inserirla in queue.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugOverlayQueueBuilder
    {
        private const int CellVisualLayerOrder = 110;
        private const int EdgeVisualLayerOrder = 115;
        private const int NodeVisualLayerOrder = 120;
        private const int LabelVisualLayerOrder = 130;

        private readonly List<ArcGraphDebugCellOverlayItem> _cellBuffer = new List<ArcGraphDebugCellOverlayItem>(256);
        private readonly List<ArcGraphDebugNodeOverlayItem> _nodeBuffer = new List<ArcGraphDebugNodeOverlayItem>(128);
        private readonly List<ArcGraphDebugEdgeOverlayItem> _edgeBuffer = new List<ArcGraphDebugEdgeOverlayItem>(256);
        private readonly List<ArcGraphDebugLabelOverlayItem> _labelBuffer = new List<ArcGraphDebugLabelOverlayItem>(128);

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue debug normalizzata partendo da uno snapshot passivo.
        /// </para>
        ///
        /// <para><b>Output controllato</b></para>
        /// <para>
        /// La queue viene pulita, poi popolata con item ordinati per famiglia. Gli
        /// item disabilitati o invalidi restano fuori dalla queue per default, ma
        /// possono essere inclusi con <c>includeHiddenItems</c> per smoke test e QA.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>snapshot</b>: input DTO gia' prodotto dal chiamante.</item>
        ///   <item><b>queue</b>: contenitore ArcGraph da popolare.</item>
        ///   <item><b>clearQueue</b>: controlla se svuotare il target prima del build.</item>
        ///   <item><b>includeHiddenItems</b>: conserva item nascosti per diagnostica.</item>
        /// </list>
        /// </summary>
        public ArcGraphDebugOverlayQueueDiagnostics Build(
            ArcGraphDebugOverlaySnapshot snapshot,
            ArcGraphDebugOverlayQueue queue,
            bool clearQueue = true,
            bool includeHiddenItems = false)
        {
            if (queue == null)
            {
                return new ArcGraphDebugOverlayQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    "QueueMissing");
            }

            if (clearQueue)
                queue.Clear();

            if (snapshot == null)
            {
                return queue.CreateDiagnostics("SnapshotMissing");
            }

            ClearBuffers();
            BuildCells(snapshot, includeHiddenItems);
            BuildNodes(snapshot, includeHiddenItems);
            BuildEdges(snapshot, includeHiddenItems);
            BuildLabels(snapshot, includeHiddenItems);

            FlushBuffers(queue);
            return queue.CreateDiagnostics("DebugOverlayQueueBuilt");
        }

        private void ClearBuffers()
        {
            _cellBuffer.Clear();
            _nodeBuffer.Clear();
            _edgeBuffer.Clear();
            _labelBuffer.Clear();
        }

        private void BuildCells(
            ArcGraphDebugOverlaySnapshot snapshot,
            bool includeHiddenItems)
        {
            IReadOnlyList<ArcGraphDebugCellOverlaySnapshot> cells = snapshot.Cells;

            for (int i = 0; i < cells.Count; i++)
            {
                ArcGraphDebugCellOverlayItem item = CreateCellItem(cells[i]);
                if (item.IsVisible || includeHiddenItems)
                    _cellBuffer.Add(item);
            }

            _cellBuffer.Sort(CompareCells);
        }

        private void BuildNodes(
            ArcGraphDebugOverlaySnapshot snapshot,
            bool includeHiddenItems)
        {
            IReadOnlyList<ArcGraphDebugNodeOverlaySnapshot> nodes = snapshot.Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                ArcGraphDebugNodeOverlayItem item = CreateNodeItem(nodes[i]);
                if (item.IsVisible || includeHiddenItems)
                    _nodeBuffer.Add(item);
            }

            _nodeBuffer.Sort(CompareNodes);
        }

        private void BuildEdges(
            ArcGraphDebugOverlaySnapshot snapshot,
            bool includeHiddenItems)
        {
            IReadOnlyList<ArcGraphDebugEdgeOverlaySnapshot> edges = snapshot.Edges;

            for (int i = 0; i < edges.Count; i++)
            {
                ArcGraphDebugEdgeOverlayItem item = CreateEdgeItem(edges[i]);
                if (item.IsVisible || includeHiddenItems)
                    _edgeBuffer.Add(item);
            }

            _edgeBuffer.Sort(CompareEdges);
        }

        private void BuildLabels(
            ArcGraphDebugOverlaySnapshot snapshot,
            bool includeHiddenItems)
        {
            IReadOnlyList<ArcGraphDebugLabelOverlaySnapshot> labels = snapshot.Labels;

            for (int i = 0; i < labels.Count; i++)
            {
                ArcGraphDebugLabelOverlayItem item = CreateLabelItem(labels[i]);
                if (item.IsVisible || includeHiddenItems)
                    _labelBuffer.Add(item);
            }

            _labelBuffer.Sort(CompareLabels);
        }

        private void FlushBuffers(ArcGraphDebugOverlayQueue queue)
        {
            for (int i = 0; i < _cellBuffer.Count; i++)
                queue.AddCell(_cellBuffer[i]);

            for (int i = 0; i < _nodeBuffer.Count; i++)
                queue.AddNode(_nodeBuffer[i]);

            for (int i = 0; i < _edgeBuffer.Count; i++)
                queue.AddEdge(_edgeBuffer[i]);

            for (int i = 0; i < _labelBuffer.Count; i++)
                queue.AddLabel(_labelBuffer[i]);
        }

        private static ArcGraphDebugCellOverlayItem CreateCellItem(
            ArcGraphDebugCellOverlaySnapshot snapshot)
        {
            ResolveVisibility(
                snapshot.Kind,
                snapshot.IsEnabled,
                out bool isVisible,
                out string hiddenReason);

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.Cell,
                CellVisualLayerOrder,
                ArcGraphRenderItemKind.Debug,
                ResolveCellEntityId(snapshot));

            return new ArcGraphDebugCellOverlayItem(
                snapshot.Cell,
                snapshot.Kind,
                snapshot.Intensity01,
                snapshot.NumericValue,
                snapshot.ColorKey,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static ArcGraphDebugNodeOverlayItem CreateNodeItem(
            ArcGraphDebugNodeOverlaySnapshot snapshot)
        {
            ResolveVisibility(
                snapshot.Kind,
                snapshot.IsEnabled,
                out bool isVisible,
                out string hiddenReason);

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.Cell,
                NodeVisualLayerOrder,
                ArcGraphRenderItemKind.Debug,
                ResolveNodeEntityId(snapshot));

            return new ArcGraphDebugNodeOverlayItem(
                snapshot.Cell,
                snapshot.Kind,
                snapshot.NodeId,
                snapshot.Label,
                snapshot.Scale01,
                snapshot.ColorKey,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static ArcGraphDebugEdgeOverlayItem CreateEdgeItem(
            ArcGraphDebugEdgeOverlaySnapshot snapshot)
        {
            ResolveVisibility(
                snapshot.Kind,
                snapshot.IsEnabled,
                out bool isVisible,
                out string hiddenReason);

            if (isVisible && snapshot.From.Equals(snapshot.To))
            {
                isVisible = false;
                hiddenReason = "DegenerateEdge";
            }

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.From,
                EdgeVisualLayerOrder,
                ArcGraphRenderItemKind.Debug,
                ResolveEdgeEntityId(snapshot));

            return new ArcGraphDebugEdgeOverlayItem(
                snapshot.From,
                snapshot.To,
                snapshot.Kind,
                snapshot.Reliability01,
                snapshot.WidthKey,
                snapshot.ColorKey,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static ArcGraphDebugLabelOverlayItem CreateLabelItem(
            ArcGraphDebugLabelOverlaySnapshot snapshot)
        {
            ResolveVisibility(
                snapshot.Kind,
                snapshot.IsEnabled,
                out bool isVisible,
                out string hiddenReason);

            if (isVisible && string.IsNullOrWhiteSpace(snapshot.Text))
            {
                isVisible = false;
                hiddenReason = "MissingLabelText";
            }

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.AnchorCell,
                LabelVisualLayerOrder,
                ArcGraphRenderItemKind.Debug,
                ResolveLabelEntityId(snapshot));

            return new ArcGraphDebugLabelOverlayItem(
                snapshot.AnchorCell,
                snapshot.Kind,
                snapshot.OwnerId,
                snapshot.Text,
                snapshot.Space,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static void ResolveVisibility(
            ArcGraphDebugOverlayKind kind,
            bool isEnabled,
            out bool isVisible,
            out string hiddenReason)
        {
            isVisible = true;
            hiddenReason = "None";

            if (!isEnabled)
            {
                isVisible = false;
                hiddenReason = "DisabledBySnapshot";
            }
            else if (kind == ArcGraphDebugOverlayKind.None)
            {
                isVisible = false;
                hiddenReason = "InvalidDebugKind";
            }
        }

        private static int ResolveCellEntityId(ArcGraphDebugCellOverlaySnapshot snapshot)
        {
            return StableHash(
                (int)snapshot.Kind,
                snapshot.Cell.X,
                snapshot.Cell.Y,
                snapshot.Cell.Z,
                snapshot.NumericValue);
        }

        private static int ResolveNodeEntityId(ArcGraphDebugNodeOverlaySnapshot snapshot)
        {
            if (snapshot.NodeId > 0)
                return snapshot.NodeId;

            return StableHash(
                (int)snapshot.Kind,
                snapshot.Cell.X,
                snapshot.Cell.Y,
                snapshot.Cell.Z,
                0);
        }

        private static int ResolveEdgeEntityId(ArcGraphDebugEdgeOverlaySnapshot snapshot)
        {
            return StableHash(
                (int)snapshot.Kind,
                snapshot.From.X,
                snapshot.From.Y,
                snapshot.To.X,
                snapshot.To.Y);
        }

        private static int ResolveLabelEntityId(ArcGraphDebugLabelOverlaySnapshot snapshot)
        {
            if (snapshot.OwnerId > 0)
                return snapshot.OwnerId;

            return StableHash(
                (int)snapshot.Kind,
                snapshot.AnchorCell.X,
                snapshot.AnchorCell.Y,
                snapshot.AnchorCell.Z,
                (int)snapshot.Space);
        }

        private static int StableHash(
            int a,
            int b,
            int c,
            int d,
            int e)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + a;
                hash = (hash * 31) + b;
                hash = (hash * 31) + c;
                hash = (hash * 31) + d;
                hash = (hash * 31) + e;
                return hash & int.MaxValue;
            }
        }

        private static int CompareCells(
            ArcGraphDebugCellOverlayItem left,
            ArcGraphDebugCellOverlayItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }

        private static int CompareNodes(
            ArcGraphDebugNodeOverlayItem left,
            ArcGraphDebugNodeOverlayItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }

        private static int CompareEdges(
            ArcGraphDebugEdgeOverlayItem left,
            ArcGraphDebugEdgeOverlayItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }

        private static int CompareLabels(
            ArcGraphDebugLabelOverlayItem left,
            ArcGraphDebugLabelOverlayItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }
    }
}
