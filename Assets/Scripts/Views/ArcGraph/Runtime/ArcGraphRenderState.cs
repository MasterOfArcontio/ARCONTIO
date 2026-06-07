using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRenderState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato minimo condiviso del sistema grafico <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato grafico, non stato mondo</b></para>
    /// <para>
    /// Questo oggetto descrive come la view sta mostrando il mondo, non cosa sia
    /// vero nel mondo. Contiene il livello <c>Z</c> attivo, la dimensione chunk,
    /// il rapporto cella/world e il dirty state visuale. Non possiede NPC, oggetti,
    /// bisogni, decisioni, job o memoria.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>VisibleZLevel</b>: livello discreto attualmente mostrato.</item>
    ///   <item><b>TileSizeWorld</b>: scala cella -> world units.</item>
    ///   <item><b>ChunkSizeCells</b>: grandezza chunk grafico in celle.</item>
    ///   <item><b>Dirty</b>: registro delle porzioni da aggiornare.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRenderState
    {
        public int VisibleZLevel { get; private set; }
        public float TileSizeWorld { get; private set; }
        public int ChunkSizeCells { get; private set; }
        public ArcGraphDirtyState Dirty { get; } = new();

        // =============================================================================
        // ArcGraphRenderState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea uno stato render con parametri normalizzati.
        /// </para>
        ///
        /// <para><b>Compatibilita' iniziale</b></para>
        /// <para>
        /// I default rispecchiano la MapGrid attuale: cella world pari a uno e
        /// chunk da sedici celle. Il costruttore corregge valori non validi per
        /// evitare che un renderer futuro divida per zero o crei chunk nulli.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>visibleZLevel</b>: livello mostrato.</item>
        ///   <item><b>tileSizeWorld</b>: scala cella/world normalizzata.</item>
        ///   <item><b>chunkSizeCells</b>: chunk minimo pari a una cella.</item>
        /// </list>
        /// </summary>
        public ArcGraphRenderState(
            int visibleZLevel = ArcGraphZLevelPolicy.DefaultVisibleZLevel,
            float tileSizeWorld = 1f,
            int chunkSizeCells = 16)
        {
            VisibleZLevel = visibleZLevel;
            TileSizeWorld = tileSizeWorld > 0.0001f ? tileSizeWorld : 1f;
            ChunkSizeCells = chunkSizeCells > 0 ? chunkSizeCells : 1;
        }

        // =============================================================================
        // SetVisibleZLevel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cambia il livello <c>Z</c> visualizzato.
        /// </para>
        ///
        /// <para><b>Separazione simulazione/rendering</b></para>
        /// <para>
        /// Cambiare livello visuale non sposta entita', non cambia altitudine reale
        /// e non modifica la mappa. Segnala solo quale slice il renderer dovrebbe
        /// mostrare nel prossimo refresh.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>zLevel</b>: nuovo livello discreto visibile.</item>
        /// </list>
        /// </summary>
        public void SetVisibleZLevel(int zLevel)
        {
            VisibleZLevel = zLevel;
        }

        // =============================================================================
        // ResolveChunkCoord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una cella in coordinata chunk usando il chunk size corrente.
        /// </para>
        ///
        /// <para><b>Dirty chunk preparatorio</b></para>
        /// <para>
        /// Questo helper permette a un layer di marcare il chunk corrispondente a
        /// una cella senza duplicare formule tra moduli futuri. Per ora resta
        /// semplice e discreto: nessuna camera, nessun culling, nessun accesso al
        /// <c>World</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>cell</b>: cella da convertire.</item>
        ///   <item><b>ChunkSizeCells</b>: divisore normalizzato.</item>
        /// </list>
        /// </summary>
        public ArcGraphChunkCoord ResolveChunkCoord(ArcGraphCellCoord cell)
        {
            return new ArcGraphChunkCoord(
                FloorDiv(cell.X, ChunkSizeCells),
                FloorDiv(cell.Y, ChunkSizeCells),
                cell.Z);
        }

        // =============================================================================
        // IsCellOnVisibleZLevel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se una cella appartiene al livello <c>Z</c> attualmente visibile.
        /// </para>
        ///
        /// <para><b>Filtro di presentazione, non esistenza simulativa</b></para>
        /// <para>
        /// Il metodo aiuta i futuri renderer a distinguere la slice grafica mostrata.
        /// Non afferma che celle su altri livelli non esistano e non modifica dirty,
        /// layer o <c>World</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>cell</b>: coordinata da confrontare.</item>
        ///   <item><b>VisibleZLevel</b>: livello grafico corrente.</item>
        /// </list>
        /// </summary>
        public bool IsCellOnVisibleZLevel(ArcGraphCellCoord cell)
        {
            return cell.Z == VisibleZLevel;
        }

        // =============================================================================
        // IsChunkOnVisibleZLevel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se un chunk appartiene al livello <c>Z</c> attualmente visibile.
        /// </para>
        ///
        /// <para><b>Compatibilita' chunk multilivello</b></para>
        /// <para>
        /// I chunk dirty conservano il proprio <c>Z</c>. Questo helper permette al
        /// renderer futuro di ignorare temporaneamente chunk non visibili senza
        /// cancellare il loro stato e senza perdere la distinzione tra livelli.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>chunk</b>: coordinata chunk da confrontare.</item>
        ///   <item><b>VisibleZLevel</b>: livello grafico corrente.</item>
        /// </list>
        /// </summary>
        public bool IsChunkOnVisibleZLevel(ArcGraphChunkCoord chunk)
        {
            return chunk.Z == VisibleZLevel;
        }

        // =============================================================================
        // MarkCellAndChunkDirty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Marca una cella e il chunk grafico che la contiene come sporchi.
        /// </para>
        ///
        /// <para><b>Principio architetturale: dirty grafico centralizzato</b></para>
        /// <para>
        /// I layer non devono duplicare la formula di conversione cella -> chunk.
        /// Questo metodo mantiene la logica nel render state, che e' l'unico punto
        /// che conosce <c>ChunkSizeCells</c>. La marcatura resta puramente grafica:
        /// non emette eventi, non modifica il <c>World</c> e non comunica nulla agli
        /// NPC.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>cell</b>: cella grafica da aggiornare.</item>
        ///   <item><b>DirtyCells</b>: riceve la cella.</item>
        ///   <item><b>DirtyChunks</b>: riceve il chunk risolto dalla cella.</item>
        /// </list>
        /// </summary>
        public void MarkCellAndChunkDirty(ArcGraphCellCoord cell)
        {
            Dirty.MarkCellDirty(cell);
            Dirty.MarkChunkDirty(ResolveChunkCoord(cell));
        }

        // =============================================================================
        // MarkCellsAndChunksDirty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Marca una sequenza di celle e i rispettivi chunk come sporchi.
        /// </para>
        ///
        /// <para><b>Batch semplice, non diff engine</b></para>
        /// <para>
        /// Questo helper serve a ridurre duplicazione nei layer quando arrivano
        /// snapshot multipli. Non confronta vecchio e nuovo stato, non calcola delta
        /// minimi e non ordina priorita'. L'ottimizzazione dirty aggressiva resta
        /// fuori scope in questo checkpoint.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>cells</b>: sequenza di coordinate da marcare.</item>
        ///   <item><b>HashSet dirty</b>: deduplica gestita da <c>ArcGraphDirtyState</c>.</item>
        /// </list>
        /// </summary>
        public void MarkCellsAndChunksDirty(IEnumerable<ArcGraphCellCoord> cells)
        {
            if (cells == null)
                return;

            foreach (var cell in cells)
                MarkCellAndChunkDirty(cell);
        }

        // =============================================================================
        // ClearDirty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Pulisce il dirty state grafico condiviso.
        /// </para>
        ///
        /// <para><b>Cleanup esplicito della presentazione</b></para>
        /// <para>
        /// La pulizia resta una scelta del chiamante, non un effetto automatico del
        /// refresh dei layer. Questo evita che un layer debug o un layer parziale
        /// consumi accidentalmente il dirty prima degli altri renderer futuri.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Dirty</b>: registro grafico svuotato.</item>
        /// </list>
        /// </summary>
        public void ClearDirty()
        {
            Dirty.Clear();
        }

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
                quotient--;
            return quotient;
        }
    }
}
