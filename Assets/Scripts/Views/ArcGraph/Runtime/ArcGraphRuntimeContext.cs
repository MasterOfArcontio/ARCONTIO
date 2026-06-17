using Arcontio.Core;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRuntimeContext
    // =============================================================================
    /// <summary>
    /// <para>
    /// Pacchetto esplicito di sorgenti runtime neutrali che il bootstrap ArcGraph
    /// puo' leggere come input.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dati ricevuti, non cercati globalmente</b></para>
    /// <para>
    /// ArcGraph non deve ricevere <c>MapGridData</c> come sorgente terrain e non
    /// deve trattare il renderer legacy come fonte della mappa. Un chiamante esterno
    /// costruisce questo context partendo da sorgenti autorizzate e lo passa al
    /// bootstrap. Il bootstrap usa i riferimenti per copiare snapshot, non per
    /// mutare il mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>World</b>: source of truth oggettiva letta solo dall'adapter.</item>
    ///   <item><b>MapWidthCells/MapHeightCells</b>: dimensioni runtime neutrali della vista.</item>
    ///   <item><b>TileSizeWorld/ChunkSizeCells</b>: parametri grafici minimi non MapGrid.</item>
    ///   <item><b>DefaultNpcSpriteKey</b>: fallback visuale finche' non esiste profilo NPC Core.</item>
    ///   <item><b>HasAnyRuntimeData</b>: segnala se il context contiene almeno una sorgente.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRuntimeContext
    {
        public World World { get; }
        public int MapWidthCells { get; }
        public int MapHeightCells { get; }
        public float TileSizeWorld { get; }
        public int ChunkSizeCells { get; }
        public string DefaultNpcSpriteKey { get; }
        private readonly bool _hasRuntimeShape;

        public bool HasWorld => World != null;
        public bool HasCellSurfaces => World?.CellSurfaces != null;
        public bool HasConfig => _hasRuntimeShape && TileSizeWorld > 0.0001f && ChunkSizeCells > 0;
        public bool HasMap => HasCellSurfaces;
        public bool HasAnyRuntimeData => HasWorld || HasConfig;
        public bool HasCompleteRuntimeData => HasWorld && HasCellSurfaces;

        // =============================================================================
        // ArcGraphRuntimeContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un context runtime con riferimenti e parametri neutrali.
        /// </para>
        ///
        /// <para><b>Context parziale ammesso</b></para>
        /// <para>
        /// Il <c>World</c> e' opzionale per permettere harness interni, ma il
        /// percorso terrain produttivo richiede <c>World.CellSurfaces</c>. Le
        /// dimensioni e i parametri grafici vengono normalizzati qui, non letti da
        /// MapGrid.
        /// </para>
        /// </summary>
        public ArcGraphRuntimeContext(
            World world = null,
            int mapWidthCells = 0,
            int mapHeightCells = 0,
            float tileSizeWorld = 1f,
            int chunkSizeCells = 16,
            string defaultNpcSpriteKey = "human_default")
        {
            World = world;
            MapWidthCells = mapWidthCells > 0
                ? mapWidthCells
                : world?.MapWidth > 0 ? world.MapWidth : 1;
            MapHeightCells = mapHeightCells > 0
                ? mapHeightCells
                : world?.MapHeight > 0 ? world.MapHeight : 1;
            TileSizeWorld = tileSizeWorld > 0.0001f ? tileSizeWorld : 1f;
            ChunkSizeCells = chunkSizeCells > 0 ? chunkSizeCells : 16;
            DefaultNpcSpriteKey = string.IsNullOrWhiteSpace(defaultNpcSpriteKey)
                ? "human_default"
                : defaultNpcSpriteKey;
            _hasRuntimeShape = world != null || mapWidthCells > 0 || mapHeightCells > 0;
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un context vuoto per bootstrap puramente interno.
        /// </para>
        ///
        /// <para><b>Uso previsto</b></para>
        /// <para>
        /// Il context vuoto permette di verificare che render state e layer stack
        /// possano accendersi anche senza sorgenti runtime. Non rappresenta una
        /// mappa valida e non deve essere usato come fonte di dati simulativi.
        /// </para>
        /// </summary>
        public static ArcGraphRuntimeContext Empty()
        {
            return new ArcGraphRuntimeContext();
        }
    }
}
