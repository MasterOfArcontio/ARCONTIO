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
        private ArcGraphRuntimeTerrainMap _runtimeTerrainMap;

        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Terrain;
        public int CellCount => _cells.Count;
        public ArcGraphRuntimeTerrainMap RuntimeTerrainMap => _runtimeTerrainMap;

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
            if (snapshots == null)
            {
                if (_cells.Count > 0)
                {
                    foreach (ArcGraphCellCoord previousCell in _cells.Keys)
                        MarkTerrainCellAndVisualNeighborsDirty(previousCell, renderState);

                    _cells.Clear();
                    _runtimeTerrainMap = null;
                }

                return;
            }

            var incoming = new Dictionary<ArcGraphCellCoord, ArcGraphTerrainCellSnapshot>();
            bool changed = false;

            foreach (var snapshot in snapshots)
            {
                incoming[snapshot.Cell] = snapshot;

                if (!_cells.TryGetValue(snapshot.Cell, out ArcGraphTerrainCellSnapshot previous)
                    || !AreSameTerrainSnapshot(previous, snapshot))
                {
                    changed = true;
                    MarkTerrainCellAndVisualNeighborsDirty(snapshot.Cell, renderState);
                }
            }

            foreach (ArcGraphCellCoord previousCell in _cells.Keys)
            {
                if (incoming.ContainsKey(previousCell))
                    continue;

                changed = true;
                MarkTerrainCellAndVisualNeighborsDirty(previousCell, renderState);
            }

            if (!changed && incoming.Count == _cells.Count)
                return;

            _cells.Clear();
            foreach (var pair in incoming)
                _cells[pair.Key] = pair.Value;

            _runtimeTerrainMap = null;
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
        // RebuildRuntimeTerrainMap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricostruisce la mappa runtime semantica del terreno a partire dagli snapshot.
        /// </para>
        ///
        /// <para><b>Cache visuale esplicita</b></para>
        /// <para>
        /// Gli snapshot restano il ponte compatibile col runtime attuale, ma la
        /// runtime map separa il significato della cella dalla cache grafica.
        /// Questo metodo e' il punto in cui le varianti statiche vengono congelate
        /// in modo deterministico per coordinate, mentre le celle animate vengono
        /// marcate per risoluzione visuale successiva.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>visualPolicy</b>: fallback legacy per tile non coperti dal catalogo.</item>
        ///   <item><b>visualBuildOptions</b>: catalogo visuale opzionale e tempo visuale.</item>
        ///   <item><b>_runtimeTerrainMap</b>: cache semantica aggiornata.</item>
        /// </list>
        /// </summary>
        public ArcGraphRuntimeTerrainMap RebuildRuntimeTerrainMap(
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions)
        {
            var builder = new ArcGraphRuntimeTerrainMapBuilder();
            _runtimeTerrainMap = builder.Build(
                _cells.Values,
                visualPolicy,
                visualBuildOptions);

            return _runtimeTerrainMap;
        }

        // =============================================================================
        // GetOrRebuildRuntimeTerrainMap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la mappa runtime terrain gia' risolta, ricostruendola solo
        /// quando la cache e' stata invalidata da un cambio reale degli snapshot.
        /// </para>
        ///
        /// <para><b>Principio architetturale: terreno statico finche' non cambia</b></para>
        /// <para>
        /// Le animazioni visuali, gli NPC e gli oggetti non devono costringere il
        /// terreno a ricalcolare tutta la propria semantica. La runtime map viene
        /// quindi conservata tra frame e rigenerata solo dopo una modifica di
        /// terreno, caricamento mappa o cleanup esplicito.
        /// </para>
        /// </summary>
        public ArcGraphRuntimeTerrainMap GetOrRebuildRuntimeTerrainMap(
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphTerrainVisualBuildOptions visualBuildOptions)
        {
            return _runtimeTerrainMap ?? RebuildRuntimeTerrainMap(
                visualPolicy,
                visualBuildOptions);
        }

        private static bool AreSameTerrainSnapshot(
            ArcGraphTerrainCellSnapshot left,
            ArcGraphTerrainCellSnapshot right)
        {
            return left.Cell.Equals(right.Cell)
                   && left.TileId == right.TileId
                   && left.IsBlocked == right.IsBlocked
                   && left.SurfaceMacro == right.SurfaceMacro
                   && left.SurfaceKey == right.SurfaceKey
                   && left.VisualRuleKey == right.VisualRuleKey
                   && left.HasAuthoritativeSurface == right.HasAuthoritativeSurface;
        }

        private static void MarkTerrainCellAndVisualNeighborsDirty(
            ArcGraphCellCoord cell,
            ArcGraphRenderState renderState)
        {
            if (renderState == null)
                return;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    renderState.MarkCellAndChunkDirty(new ArcGraphCellCoord(
                        cell.X + dx,
                        cell.Y + dy,
                        cell.Z));
                }
            }
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
            _runtimeTerrainMap = null;
        }

        public override void Dispose()
        {
            ClearSnapshots();
            base.Dispose();
        }
    }
}
