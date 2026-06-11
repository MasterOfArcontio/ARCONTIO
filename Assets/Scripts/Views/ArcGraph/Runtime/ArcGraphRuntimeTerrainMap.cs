using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisualCache
    // =============================================================================
    /// <summary>
    /// <para>
    /// Cache visuale stabile associata a una cella terrain runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: presentazione derivata, non semantica</b></para>
    /// <para>
    /// La cache dice quale tile disegnare, non cosa e' la cella. La semantica resta
    /// nel terrain id e nei flag della cella runtime. In questo modo una cella puo'
    /// essere <c>grass</c> a livello logico e avere come cache visuale
    /// <c>grass_variant_02</c>, senza trasformare la mappa in una semplice tabella
    /// di sprite.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>StaticTileId</b>: tile statico pre-risolto per celle non animate.</item>
    ///   <item><b>HasStaticTile</b>: true se il tile statico e' valido.</item>
    ///   <item><b>HasAnimatedVisual</b>: true se la cella deve ancora risolvere frame visuali nel tempo.</item>
    ///   <item><b>UsedVisualResolver</b>: true se la cache deriva dal catalogo visuale ArcGraph.</item>
    ///   <item><b>UsedVariant</b>: true se la cache statica e' una variante deterministica.</item>
    ///   <item><b>UsedFallback</b>: true se la cache e' tornata al percorso legacy.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualCache
    {
        public readonly int StaticTileId;
        public readonly bool HasStaticTile;
        public readonly bool HasAnimatedVisual;
        public readonly bool UsedVisualResolver;
        public readonly bool UsedVariant;
        public readonly bool UsedFallback;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainVisualCache
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una cache visuale terrain normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualCache(
            int staticTileId,
            bool hasStaticTile,
            bool hasAnimatedVisual,
            bool usedVisualResolver,
            bool usedVariant,
            bool usedFallback,
            string reason)
        {
            StaticTileId = staticTileId;
            HasStaticTile = hasStaticTile;
            HasAnimatedVisual = hasAnimatedVisual;
            UsedVisualResolver = usedVisualResolver;
            UsedVariant = usedVariant;
            UsedFallback = usedFallback;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphRuntimeTerrainCell
    // =============================================================================
    /// <summary>
    /// <para>
    /// Cella semantica runtime del terreno ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: mappa semantica con cache grafica</b></para>
    /// <para>
    /// La cella conserva il significato del terreno e una cache visuale derivata.
    /// Questo e' il passaggio intermedio tra il layout legacy a tile id e la futura
    /// mappa definitiva a layer: il renderer puo' leggere un tile gia' risolto,
    /// mentre i sistemi futuri potranno leggere terrain id, blocco, costo movimento
    /// e altri metadati senza dipendere dallo sprite.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: coordinata discreta della cella.</item>
    ///   <item><b>TerrainId</b>: identita' semantica provvisoria del terreno.</item>
    ///   <item><b>SourceTileId</b>: tile id letto dalla sorgente legacy.</item>
    ///   <item><b>IsBlocked</b>: flag blocco ereditato dalla sorgente corrente.</item>
    ///   <item><b>MovementCost</b>: costo movimento preparatorio, default leggero.</item>
    ///   <item><b>VisualCache</b>: tile statico o stato animato derivato dal catalogo visuale.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphRuntimeTerrainCell
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly string TerrainId;
        public readonly int SourceTileId;
        public readonly bool IsBlocked;
        public readonly int MovementCost;
        public readonly ArcGraphTerrainVisualCache VisualCache;

        // =============================================================================
        // ArcGraphRuntimeTerrainCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una cella runtime terrain immutabile.
        /// </para>
        /// </summary>
        public ArcGraphRuntimeTerrainCell(
            ArcGraphCellCoord cell,
            string terrainId,
            int sourceTileId,
            bool isBlocked,
            int movementCost,
            ArcGraphTerrainVisualCache visualCache)
        {
            Cell = cell;
            TerrainId = string.IsNullOrWhiteSpace(terrainId) ? "unknown" : terrainId;
            SourceTileId = sourceTileId;
            IsBlocked = isBlocked;
            MovementCost = movementCost > 0 ? movementCost : 1;
            VisualCache = visualCache;
        }
    }

    // =============================================================================
    // ArcGraphRuntimeTerrainMap
    // =============================================================================
    /// <summary>
    /// <para>
    /// Mappa runtime terrain ArcGraph con celle semantiche e cache visuale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: runtime map derivata e consultabile</b></para>
    /// <para>
    /// Questa mappa non e' ancora la sorgente simulativa canonica del terreno, ma e'
    /// il contenitore ArcGraph corretto per uscire dal modello "tile grafico per
    /// cella". Viene costruita da snapshot o, in futuro, direttamente da un loader
    /// mappa ArcGraph. Una volta costruita, il renderer legge celle e cache senza
    /// ricalcolare varianti statiche a ogni quad.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_cells</b>: celle runtime in ordine deterministico.</item>
    ///   <item><b>_cellsByCoord</b>: indice puntuale per coordinate.</item>
    ///   <item><b>TryGetCell</b>: lettura read-only della cella runtime.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRuntimeTerrainMap
    {
        private readonly ArcGraphRuntimeTerrainCell[] _cells;
        private readonly Dictionary<ArcGraphCellCoord, ArcGraphRuntimeTerrainCell> _cellsByCoord;

        public IReadOnlyList<ArcGraphRuntimeTerrainCell> Cells => _cells;
        public int CellCount => _cells.Length;

        // =============================================================================
        // ArcGraphRuntimeTerrainMap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una mappa terrain runtime indicizzata per coordinata.
        /// </para>
        /// </summary>
        public ArcGraphRuntimeTerrainMap(ArcGraphRuntimeTerrainCell[] cells)
        {
            _cells = CopyCells(cells);
            _cellsByCoord = BuildIndex(_cells);
        }

        // =============================================================================
        // TryGetCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a leggere una cella terrain runtime per coordinata.
        /// </para>
        /// </summary>
        public bool TryGetCell(
            ArcGraphCellCoord cell,
            out ArcGraphRuntimeTerrainCell runtimeCell)
        {
            return _cellsByCoord.TryGetValue(cell, out runtimeCell);
        }

        private static ArcGraphRuntimeTerrainCell[] CopyCells(ArcGraphRuntimeTerrainCell[] cells)
        {
            if (cells == null || cells.Length == 0)
                return new ArcGraphRuntimeTerrainCell[0];

            var copy = new ArcGraphRuntimeTerrainCell[cells.Length];
            cells.CopyTo(copy, 0);
            return copy;
        }

        private static Dictionary<ArcGraphCellCoord, ArcGraphRuntimeTerrainCell> BuildIndex(
            ArcGraphRuntimeTerrainCell[] cells)
        {
            var index = new Dictionary<ArcGraphCellCoord, ArcGraphRuntimeTerrainCell>();
            for (int i = 0; i < cells.Length; i++)
                index[cells[i].Cell] = cells[i];

            return index;
        }
    }
}
