using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayQueue
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contenitore passivo dei contratti debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: queue debug separata dalla queue produttiva</b></para>
    /// <para>
    /// Gli overlay diagnostici non devono confondersi con actor, object, terrain o
    /// layer ambientali produttivi. Questa queue tiene separati cell, node, edge e
    /// label, cosi' i renderer futuri possono consumare solo il sottoinsieme che
    /// sanno disegnare. Non crea oggetti Unity e non possiede riferimenti al mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cells</b>: overlay cell-based.</item>
    ///   <item><b>Nodes</b>: marker puntuali.</item>
    ///   <item><b>Edges</b>: segmenti tra celle.</item>
    ///   <item><b>Labels</b>: label/HUD screen-space.</item>
    ///   <item><b>Add/Clear</b>: API minimale per builder futuri.</item>
    ///   <item><b>CreateDiagnostics</b>: conteggio verificabile senza rendering.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugOverlayQueue
    {
        private readonly List<ArcGraphDebugCellOverlayItem> _cells = new List<ArcGraphDebugCellOverlayItem>(256);
        private readonly List<ArcGraphDebugNodeOverlayItem> _nodes = new List<ArcGraphDebugNodeOverlayItem>(128);
        private readonly List<ArcGraphDebugEdgeOverlayItem> _edges = new List<ArcGraphDebugEdgeOverlayItem>(256);
        private readonly List<ArcGraphDebugLabelOverlayItem> _labels = new List<ArcGraphDebugLabelOverlayItem>(128);

        public IReadOnlyList<ArcGraphDebugCellOverlayItem> Cells => _cells;
        public IReadOnlyList<ArcGraphDebugNodeOverlayItem> Nodes => _nodes;
        public IReadOnlyList<ArcGraphDebugEdgeOverlayItem> Edges => _edges;
        public IReadOnlyList<ArcGraphDebugLabelOverlayItem> Labels => _labels;

        public int TotalItemCount => _cells.Count + _nodes.Count + _edges.Count + _labels.Count;

        public void Clear()
        {
            _cells.Clear();
            _nodes.Clear();
            _edges.Clear();
            _labels.Clear();
        }

        public void AddCell(ArcGraphDebugCellOverlayItem item)
        {
            _cells.Add(item);
        }

        public void AddNode(ArcGraphDebugNodeOverlayItem item)
        {
            _nodes.Add(item);
        }

        public void AddEdge(ArcGraphDebugEdgeOverlayItem item)
        {
            _edges.Add(item);
        }

        public void AddLabel(ArcGraphDebugLabelOverlayItem item)
        {
            _labels.Add(item);
        }

        // =============================================================================
        // CreateDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce una diagnostica sintetica della queue corrente.
        /// </para>
        ///
        /// <para><b>CPU leggera</b></para>
        /// <para>
        /// Il metodo scorre quattro liste lineari e conta solo flag gia' presenti.
        /// Non ordina, non alloca array temporanei e non interroga sistemi esterni.
        /// </para>
        /// </summary>
        public ArcGraphDebugOverlayQueueDiagnostics CreateDiagnostics(string reason)
        {
            int visible = 0;
            int hidden = 0;
            int screen = 0;

            CountCells(ref visible, ref hidden);
            CountNodes(ref visible, ref hidden);
            CountEdges(ref visible, ref hidden);
            CountLabels(ref visible, ref hidden, ref screen);

            return new ArcGraphDebugOverlayQueueDiagnostics(
                _cells.Count,
                _nodes.Count,
                _edges.Count,
                _labels.Count,
                visible,
                hidden,
                screen,
                reason);
        }

        private void CountCells(ref int visible, ref int hidden)
        {
            for (int i = 0; i < _cells.Count; i++)
                CountVisibility(_cells[i].IsVisible, ref visible, ref hidden);
        }

        private void CountNodes(ref int visible, ref int hidden)
        {
            for (int i = 0; i < _nodes.Count; i++)
                CountVisibility(_nodes[i].IsVisible, ref visible, ref hidden);
        }

        private void CountEdges(ref int visible, ref int hidden)
        {
            for (int i = 0; i < _edges.Count; i++)
                CountVisibility(_edges[i].IsVisible, ref visible, ref hidden);
        }

        private void CountLabels(ref int visible, ref int hidden, ref int screen)
        {
            for (int i = 0; i < _labels.Count; i++)
            {
                CountVisibility(_labels[i].IsVisible, ref visible, ref hidden);
                if (_labels[i].Space == ArcGraphDebugOverlaySpace.ScreenHud
                    || _labels[i].Space == ArcGraphDebugOverlaySpace.ScreenLabel)
                {
                    screen++;
                }
            }
        }

        private static void CountVisibility(bool isVisible, ref int visible, ref int hidden)
        {
            if (isVisible)
                visible++;
            else
                hidden++;
        }
    }
}
