using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDirtyState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Registro passivo delle celle e dei chunk grafici che devono essere
    /// aggiornati dai layer <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: rendering locale, non scansione globale</b></para>
    /// <para>
    /// Il sistema grafico futuro deve evitare ridisegni completi della mappa. Questo
    /// stato conserva soltanto coordinate sporche e non legge mai il <c>World</c>.
    /// In questo modo il dirty grafico resta un fatto di presentazione: segnala che
    /// qualcosa va ridisegnato, ma non diventa conoscenza simulativa e non decide
    /// quali fatti siano veri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DirtyCells</b>: celle specifiche da aggiornare.</item>
    ///   <item><b>DirtyChunks</b>: blocchi grafici da ricostruire o rinfrescare.</item>
    ///   <item><b>Clear</b>: reset esplicito dopo consumo da parte dei renderer.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDirtyState
    {
        private readonly HashSet<ArcGraphCellCoord> _dirtyCells = new();
        private readonly HashSet<ArcGraphChunkCoord> _dirtyChunks = new();

        public int DirtyCellCount => _dirtyCells.Count;
        public int DirtyChunkCount => _dirtyChunks.Count;
        public bool HasDirtyWork => _dirtyCells.Count > 0 || _dirtyChunks.Count > 0;

        // =============================================================================
        // MarkCellDirty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Marca una singola cella come graficamente sporca.
        /// </para>
        ///
        /// <para><b>Uso previsto</b></para>
        /// <para>
        /// Questo metodo sara' chiamato da adapter o sistemi visuali quando una
        /// mutazione simulativa gia' avvenuta richiede un aggiornamento grafico.
        /// Non deve essere usato per mutare la simulazione o per segnalare eventi
        /// cognitivi agli NPC.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>coord</b>: cella da aggiornare.</item>
        ///   <item><b>HashSet</b>: evita duplicati nello stesso frame grafico.</item>
        /// </list>
        /// </summary>
        public void MarkCellDirty(ArcGraphCellCoord coord)
        {
            _dirtyCells.Add(coord);
        }

        // =============================================================================
        // MarkChunkDirty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Marca un chunk grafico come sporco.
        /// </para>
        ///
        /// <para><b>Uso previsto</b></para>
        /// <para>
        /// Il chunk dirty serve per layer a mesh o tile batching, come il futuro
        /// terreno chunked. Una singola cella puo' sporcare un chunk intero quando
        /// il renderer e' costruito per blocchi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>coord</b>: chunk da aggiornare.</item>
        ///   <item><b>HashSet</b>: evita ricostruzioni duplicate nello stesso ciclo.</item>
        /// </list>
        /// </summary>
        public void MarkChunkDirty(ArcGraphChunkCoord coord)
        {
            _dirtyChunks.Add(coord);
        }

        public IReadOnlyCollection<ArcGraphCellCoord> DirtyCells => _dirtyCells;
        public IReadOnlyCollection<ArcGraphChunkCoord> DirtyChunks => _dirtyChunks;

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Pulisce tutte le coordinate sporche dopo che il renderer le ha consumate.
        /// </para>
        ///
        /// <para><b>Authority limitata alla presentazione</b></para>
        /// <para>
        /// La pulizia del dirty grafico non conferma ne' annulla fatti simulativi.
        /// Significa solo che la presentazione considera aggiornate le proprie
        /// cache visuali per il ciclo corrente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>DirtyCells</b>: svuotato.</item>
        ///   <item><b>DirtyChunks</b>: svuotato.</item>
        /// </list>
        /// </summary>
        public void Clear()
        {
            _dirtyCells.Clear();
            _dirtyChunks.Clear();
        }
    }
}
