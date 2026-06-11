using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisibleChunkFilterResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato immutabile del filtro chunk visibili terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: culling spiegabile prima del rendering</b></para>
    /// <para>
    /// Il filtro non costruisce mesh e non tocca la scena. Produce solo una lista
    /// di chunk ammessi e alcuni contatori diagnostici. Questo permette al renderer
    /// terrain di spiegare quanti chunk erano sporchi, quanti erano nel viewport e
    /// quanti sono stati scartati prima di arrivare alla fase mesh.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Chunks</b>: chunk ammessi dal filtro.</item>
    ///   <item><b>InputChunkCount</b>: chunk ricevuti in ingresso.</item>
    ///   <item><b>VisibleChunkCount</b>: chunk che intersecano il viewport.</item>
    ///   <item><b>CulledChunkCount</b>: chunk scartati per viewport o livello Z.</item>
    ///   <item><b>Reason</b>: esito sintetico del filtro.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainVisibleChunkFilterResult
    {
        private readonly List<ArcGraphChunkCoord> _chunks;

        public IReadOnlyList<ArcGraphChunkCoord> Chunks => _chunks;
        public int InputChunkCount { get; }
        public int VisibleChunkCount { get; }
        public int CulledChunkCount { get; }
        public string Reason { get; }

        // =============================================================================
        // ArcGraphTerrainVisibleChunkFilterResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato filtro copiando la lista dei chunk ammessi.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisibleChunkFilterResult(
            List<ArcGraphChunkCoord> chunks,
            int inputChunkCount,
            int visibleChunkCount,
            int culledChunkCount,
            string reason)
        {
            _chunks = chunks != null
                ? new List<ArcGraphChunkCoord>(chunks)
                : new List<ArcGraphChunkCoord>();

            InputChunkCount = inputChunkCount < 0 ? 0 : inputChunkCount;
            VisibleChunkCount = visibleChunkCount < 0 ? 0 : visibleChunkCount;
            CulledChunkCount = culledChunkCount < 0 ? 0 : culledChunkCount;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisibleChunkFilter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Filtro passivo che seleziona i chunk terrain intersecanti il viewport celle.
    /// </para>
    ///
    /// <para><b>Principio architetturale: viewport come vincolo grafico locale</b></para>
    /// <para>
    /// Il filtro riceve chunk gia' noti e un rettangolo visibile gia' calcolato da
    /// altri moduli view-side. Non legge camera Unity, input, MapGrid o World. Il
    /// suo unico compito e' dire se il rettangolo celle di un chunk interseca la
    /// finestra visuale corrente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Filter</b>: applica filtro viewport e filtro Z.</item>
    ///   <item><b>ChunkIntersectsRect</b>: controlla intersezione half-open.</item>
    ///   <item><b>CompareChunks</b>: mantiene output ordinato e deterministico.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainVisibleChunkFilter
    {
        // =============================================================================
        // Filter
        // =============================================================================
        /// <summary>
        /// <para>
        /// Filtra i chunk ricevuti usando viewport opzionale e livello Z visibile.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisibleChunkFilterResult Filter(
            IEnumerable<ArcGraphChunkCoord> chunks,
            int chunkSizeCells,
            int visibleZLevel,
            bool useVisibleCellRect,
            ArcGraphViewCellRect visibleCellRect)
        {
            var accepted = new List<ArcGraphChunkCoord>();
            int inputCount = 0;
            int culledCount = 0;
            int safeChunkSize = chunkSizeCells > 0 ? chunkSizeCells : 1;

            if (chunks == null)
            {
                return new ArcGraphTerrainVisibleChunkFilterResult(
                    accepted,
                    0,
                    0,
                    0,
                    "NoInputChunks");
            }

            foreach (ArcGraphChunkCoord chunk in chunks)
            {
                inputCount++;

                if (chunk.Z != visibleZLevel)
                {
                    culledCount++;
                    continue;
                }

                if (useVisibleCellRect
                    && (visibleCellRect.IsEmpty || !ChunkIntersectsRect(chunk, safeChunkSize, visibleCellRect)))
                {
                    culledCount++;
                    continue;
                }

                accepted.Add(chunk);
            }

            accepted.Sort(CompareChunks);

            return new ArcGraphTerrainVisibleChunkFilterResult(
                accepted,
                inputCount,
                accepted.Count,
                culledCount,
                useVisibleCellRect ? "ViewportFiltered" : "ViewportDisabled");
        }

        public static bool ChunkIntersectsRect(
            ArcGraphChunkCoord chunk,
            int chunkSizeCells,
            ArcGraphViewCellRect rect)
        {
            if (rect.IsEmpty)
                return false;

            int safeChunkSize = chunkSizeCells > 0 ? chunkSizeCells : 1;
            int chunkMinX = chunk.X * safeChunkSize;
            int chunkMinY = chunk.Y * safeChunkSize;
            int chunkMaxX = chunkMinX + safeChunkSize;
            int chunkMaxY = chunkMinY + safeChunkSize;

            return chunkMinX < rect.MaxXExclusive
                   && chunkMaxX > rect.MinX
                   && chunkMinY < rect.MaxYExclusive
                   && chunkMaxY > rect.MinY;
        }

        private static int CompareChunks(ArcGraphChunkCoord a, ArcGraphChunkCoord b)
        {
            int z = a.Z.CompareTo(b.Z);
            if (z != 0) return z;

            int y = a.Y.CompareTo(b.Y);
            if (y != 0) return y;

            return a.X.CompareTo(b.X);
        }
    }
}
