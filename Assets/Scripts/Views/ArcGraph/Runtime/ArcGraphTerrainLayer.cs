using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer minimo del terreno per il futuro sistema <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: terreno come cache visuale derivata</b></para>
    /// <para>
    /// Il layer non legge direttamente il <c>World</c> e non legge direttamente
    /// <c>MapGridData</c>. Riceve snapshot terreno gia' preparati dall'adapter e li
    /// conserva in una cache locale, utile al renderer futuro per sapere quali tile
    /// disegnare. Questa cache e' grafica: non decide pathfinding, occlusione o
    /// fertilita' e non diventa una mappa simulativa parallela.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_cells</b>: snapshot terreno indicizzati per coordinata cella.</item>
    ///   <item><b>ReplaceSnapshots</b>: aggiorna la cache da una sequenza esterna.</item>
    ///   <item><b>TryGetCell</b>: lettura puntuale della cache visuale.</item>
    ///   <item><b>ClearSnapshots</b>: svuota solo lo stato grafico locale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainLayer : ArcGraphLayerBase
    {
        private readonly Dictionary<ArcGraphCellCoord, ArcGraphTerrainCellSnapshot> _cells = new();

        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Terrain;
        public int CellCount => _cells.Count;

        // =============================================================================
        // ReplaceSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sostituisce la cache terreno locale con gli snapshot ricevuti.
        /// </para>
        ///
        /// <para><b>Dirty preparatorio</b></para>
        /// <para>
        /// Ogni cella importata viene marcata come dirty nello stato render fornito,
        /// insieme al relativo chunk. In questa fase la marcatura e' volutamente
        /// semplice e conservativa: non prova ancora a calcolare differenze minime,
        /// perche' l'ottimizzazione dirty aggressiva appartiene a checkpoint futuri.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>snapshots</b>: sequenza di celle terreno da copiare.</item>
        ///   <item><b>renderState</b>: stato grafico da marcare dirty, opzionale.</item>
        /// </list>
        /// </summary>
        public void ReplaceSnapshots(
            IEnumerable<ArcGraphTerrainCellSnapshot> snapshots,
            ArcGraphRenderState renderState = null)
        {
            _cells.Clear();

            if (snapshots == null)
                return;

            foreach (var snapshot in snapshots)
            {
                _cells[snapshot.Cell] = snapshot;
                renderState?.MarkCellAndChunkDirty(snapshot.Cell);
            }
        }

        // =============================================================================
        // TryGetCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a leggere lo snapshot terreno di una cella.
        /// </para>
        ///
        /// <para><b>Lettura view-side</b></para>
        /// <para>
        /// Il metodo legge solo la cache locale del layer. Non interroga il mondo e
        /// non corregge la simulazione se la cella non esiste.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>cell</b>: coordinata richiesta.</item>
        ///   <item><b>snapshot</b>: risultato copiato se presente.</item>
        /// </list>
        /// </summary>
        public bool TryGetCell(ArcGraphCellCoord cell, out ArcGraphTerrainCellSnapshot snapshot)
        {
            return _cells.TryGetValue(cell, out snapshot);
        }

        // =============================================================================
        // ClearSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota la cache terreno locale.
        /// </para>
        ///
        /// <para><b>Cleanup grafico</b></para>
        /// <para>
        /// La pulizia non rimuove tile dalla mappa reale e non cambia il buffer
        /// MapGrid. Serve solo a resettare il layer, ad esempio durante load o
        /// rebind futuro del renderer.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>_cells</b>: dizionario svuotato.</item>
        /// </list>
        /// </summary>
        public void ClearSnapshots()
        {
            _cells.Clear();
        }

        public override void Dispose()
        {
            ClearSnapshots();
            base.Dispose();
        }
    }
}
