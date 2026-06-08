using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugCellOverlaySnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO passivo in ingresso per un overlay debug ancorato a una cella.
    /// </para>
    ///
    /// <para><b>Principio architetturale: input debug senza accesso al World</b></para>
    /// <para>
    /// Lo snapshot descrive un fatto visuale gia' prodotto da un sistema esterno:
    /// per esempio una cella FOV osservata, una cella DT o una cella GVD. Non
    /// calcola visibilita', non consulta NPC, non legge la mappa e non decide se
    /// il debug sia attivo. Il builder ArcGraph lo tradurra' in item ordinabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: coordinata discreta gia' risolta.</item>
    ///   <item><b>Kind</b>: tipo diagnostico dichiarativo.</item>
    ///   <item><b>Intensity01</b>: intensita' normalizzata suggerita.</item>
    ///   <item><b>NumericValue</b>: valore opzionale, utile per DT/GVD.</item>
    ///   <item><b>ColorKey</b>: stile opzionale astratto.</item>
    ///   <item><b>IsEnabled</b>: toggle gia' deciso dal chiamante.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugCellOverlaySnapshot
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly ArcGraphDebugOverlayKind Kind;
        public readonly float Intensity01;
        public readonly int NumericValue;
        public readonly string ColorKey;
        public readonly bool IsEnabled;

        public ArcGraphDebugCellOverlaySnapshot(
            ArcGraphCellCoord cell,
            ArcGraphDebugOverlayKind kind,
            float intensity01,
            int numericValue,
            string colorKey,
            bool isEnabled = true)
        {
            Cell = cell;
            Kind = kind;
            Intensity01 = intensity01;
            NumericValue = numericValue;
            ColorKey = colorKey;
            IsEnabled = isEnabled;
        }
    }

    // =============================================================================
    // ArcGraphDebugNodeOverlaySnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO passivo in ingresso per un marker debug puntuale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: landmark come payload, non come servizio</b></para>
    /// <para>
    /// Lo snapshot porta un nodo landmark/pathfinding gia' noto al chiamante. Non
    /// conosce registry, memoria NPC o grafo di navigazione. Questo permette ad
    /// ArcGraph di ricevere nodi debug senza diventare un secondo sistema
    /// decisionale o un lettore diretto del Core.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: cella del marker.</item>
    ///   <item><b>Kind</b>: significato diagnostico del nodo.</item>
    ///   <item><b>NodeId</b>: identificativo esterno, se disponibile.</item>
    ///   <item><b>Label</b>: testo breve opzionale.</item>
    ///   <item><b>Scale01</b>: scala visuale normalizzata suggerita.</item>
    ///   <item><b>ColorKey</b>: stile astratto opzionale.</item>
    ///   <item><b>IsEnabled</b>: toggle gia' deciso dal producer.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugNodeOverlaySnapshot
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly ArcGraphDebugOverlayKind Kind;
        public readonly int NodeId;
        public readonly string Label;
        public readonly float Scale01;
        public readonly string ColorKey;
        public readonly bool IsEnabled;

        public ArcGraphDebugNodeOverlaySnapshot(
            ArcGraphCellCoord cell,
            ArcGraphDebugOverlayKind kind,
            int nodeId,
            string label,
            float scale01,
            string colorKey,
            bool isEnabled = true)
        {
            Cell = cell;
            Kind = kind;
            NodeId = nodeId;
            Label = label;
            Scale01 = scale01;
            ColorKey = colorKey;
            IsEnabled = isEnabled;
        }
    }

    // =============================================================================
    // ArcGraphDebugEdgeOverlaySnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO passivo in ingresso per un segmento debug tra due celle.
    /// </para>
    ///
    /// <para><b>Principio architetturale: edge dichiarativo senza renderer Unity</b></para>
    /// <para>
    /// Lo snapshot dice che esiste un segmento diagnostico tra due celle. Non crea
    /// linee, mesh, materiali o <c>LineRenderer</c>. Non decide pathfinding:
    /// conserva solo il risultato gia' calcolato da un producer esterno.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>From/To</b>: estremi discreti.</item>
    ///   <item><b>Kind</b>: tipo diagnostico del segmento.</item>
    ///   <item><b>Reliability01</b>: affidabilita' o intensita' normalizzata.</item>
    ///   <item><b>WidthKey</b>: stile spessore astratto opzionale.</item>
    ///   <item><b>ColorKey</b>: stile colore astratto opzionale.</item>
    ///   <item><b>IsEnabled</b>: toggle gia' deciso dal chiamante.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugEdgeOverlaySnapshot
    {
        public readonly ArcGraphCellCoord From;
        public readonly ArcGraphCellCoord To;
        public readonly ArcGraphDebugOverlayKind Kind;
        public readonly float Reliability01;
        public readonly string WidthKey;
        public readonly string ColorKey;
        public readonly bool IsEnabled;

        public ArcGraphDebugEdgeOverlaySnapshot(
            ArcGraphCellCoord from,
            ArcGraphCellCoord to,
            ArcGraphDebugOverlayKind kind,
            float reliability01,
            string widthKey,
            string colorKey,
            bool isEnabled = true)
        {
            From = from;
            To = to;
            Kind = kind;
            Reliability01 = reliability01;
            WidthKey = widthKey;
            ColorKey = colorKey;
            IsEnabled = isEnabled;
        }
    }

    // =============================================================================
    // ArcGraphDebugLabelOverlaySnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO passivo in ingresso per label debug e HUD.
    /// </para>
    ///
    /// <para><b>Principio architetturale: testo gia' formato dal producer</b></para>
    /// <para>
    /// Lo snapshot non sa nulla di font, Canvas, TextMeshPro o layout. Il testo e'
    /// gia' stato scelto dal producer debug; ArcGraph deve solo conservarlo come
    /// dato ordinabile e separare lo spazio HUD dallo spazio mappa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AnchorCell</b>: cella di ancoraggio, se rilevante.</item>
    ///   <item><b>Kind</b>: tipo di label.</item>
    ///   <item><b>OwnerId</b>: id opzionale del soggetto rappresentato.</item>
    ///   <item><b>Text</b>: contenuto testuale gia' pronto.</item>
    ///   <item><b>Space</b>: HUD o label screen-space.</item>
    ///   <item><b>IsEnabled</b>: toggle gia' deciso dal chiamante.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugLabelOverlaySnapshot
    {
        public readonly ArcGraphCellCoord AnchorCell;
        public readonly ArcGraphDebugOverlayKind Kind;
        public readonly int OwnerId;
        public readonly string Text;
        public readonly ArcGraphDebugOverlaySpace Space;
        public readonly bool IsEnabled;

        public ArcGraphDebugLabelOverlaySnapshot(
            ArcGraphCellCoord anchorCell,
            ArcGraphDebugOverlayKind kind,
            int ownerId,
            string text,
            ArcGraphDebugOverlaySpace space,
            bool isEnabled = true)
        {
            AnchorCell = anchorCell;
            Kind = kind;
            OwnerId = ownerId;
            Text = text;
            Space = space;
            IsEnabled = isEnabled;
        }
    }

    // =============================================================================
    // ArcGraphDebugOverlaySnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Frame passivo di input per il builder overlay debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: raccolta input separata dalla queue</b></para>
    /// <para>
    /// Questo contenitore rappresenta cio' che un producer debug vuole mostrare in
    /// un frame. E' distinto da <c>ArcGraphDebugOverlayQueue</c> perche' non
    /// contiene ancora sort key, normalizzazione e motivazioni di hidden state.
    /// La separazione tiene chiaro il flusso: producer prepara DTO, builder
    /// normalizza, renderer futuro consuma la queue.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cells</b>: DTO FOV/DT/GVD cell-based.</item>
    ///   <item><b>Nodes</b>: DTO marker landmark.</item>
    ///   <item><b>Edges</b>: DTO segmenti landmark/pathfinding.</item>
    ///   <item><b>Labels</b>: DTO label e HUD.</item>
    ///   <item><b>Add/Clear</b>: API minimale per popolare il frame.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugOverlaySnapshot
    {
        private readonly List<ArcGraphDebugCellOverlaySnapshot> _cells = new List<ArcGraphDebugCellOverlaySnapshot>(256);
        private readonly List<ArcGraphDebugNodeOverlaySnapshot> _nodes = new List<ArcGraphDebugNodeOverlaySnapshot>(128);
        private readonly List<ArcGraphDebugEdgeOverlaySnapshot> _edges = new List<ArcGraphDebugEdgeOverlaySnapshot>(256);
        private readonly List<ArcGraphDebugLabelOverlaySnapshot> _labels = new List<ArcGraphDebugLabelOverlaySnapshot>(128);

        public IReadOnlyList<ArcGraphDebugCellOverlaySnapshot> Cells => _cells;
        public IReadOnlyList<ArcGraphDebugNodeOverlaySnapshot> Nodes => _nodes;
        public IReadOnlyList<ArcGraphDebugEdgeOverlaySnapshot> Edges => _edges;
        public IReadOnlyList<ArcGraphDebugLabelOverlaySnapshot> Labels => _labels;

        public int TotalItemCount => _cells.Count + _nodes.Count + _edges.Count + _labels.Count;

        public void Clear()
        {
            _cells.Clear();
            _nodes.Clear();
            _edges.Clear();
            _labels.Clear();
        }

        public void AddCell(ArcGraphDebugCellOverlaySnapshot snapshot)
        {
            _cells.Add(snapshot);
        }

        public void AddNode(ArcGraphDebugNodeOverlaySnapshot snapshot)
        {
            _nodes.Add(snapshot);
        }

        public void AddEdge(ArcGraphDebugEdgeOverlaySnapshot snapshot)
        {
            _edges.Add(snapshot);
        }

        public void AddLabel(ArcGraphDebugLabelOverlaySnapshot snapshot)
        {
            _labels.Add(snapshot);
        }
    }
}
