using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisibleChunkFilterHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato dello smoke test data-only del filtro chunk visibili terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: prestazioni verificabili senza scena</b></para>
    /// <para>
    /// Il risultato permette di controllare che il filtro scarti chunk fuori
    /// viewport e chunk su livelli Z non visibili senza usare camera Unity, mesh,
    /// GameObject o renderer scena.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito globale dello smoke test.</item>
    ///   <item><b>InputChunkCount</b>: chunk finti passati al filtro.</item>
    ///   <item><b>VisibleChunkCount</b>: chunk ammessi.</item>
    ///   <item><b>CulledChunkCount</b>: chunk scartati.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisibleChunkFilterHarnessResult
    {
        public readonly bool Passed;
        public readonly int InputChunkCount;
        public readonly int VisibleChunkCount;
        public readonly int CulledChunkCount;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainVisibleChunkFilterHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato immutabile dello smoke test filtro viewport.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisibleChunkFilterHarnessResult(
            bool passed,
            int inputChunkCount,
            int visibleChunkCount,
            int culledChunkCount,
            string reason)
        {
            Passed = passed;
            InputChunkCount = inputChunkCount < 0 ? 0 : inputChunkCount;
            VisibleChunkCount = visibleChunkCount < 0 ? 0 : visibleChunkCount;
            CulledChunkCount = culledChunkCount < 0 ? 0 : culledChunkCount;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisibleChunkFilterHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il filtro chunk visibili terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: culling prima della ricchezza visuale</b></para>
    /// <para>
    /// Questo harness protegge il contratto introdotto prima di collegare varianti,
    /// animazioni e transizioni al renderer. La pipeline deve poter ridurre il set
    /// di chunk da aggiornare prima di renderizzare tile piu' ricchi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario con chunk dentro, fuori e su Z diverso.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainVisibleChunkFilterHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno smoke test data-only del filtro viewport.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainVisibleChunkFilterHarnessResult RunDefaultSmoke()
        {
            var chunks = new List<ArcGraphChunkCoord>
            {
                new ArcGraphChunkCoord(0, 0, 0),
                new ArcGraphChunkCoord(1, 0, 0),
                new ArcGraphChunkCoord(4, 4, 0),
                new ArcGraphChunkCoord(0, 0, 1)
            };

            var filter = new ArcGraphTerrainVisibleChunkFilter();
            ArcGraphTerrainVisibleChunkFilterResult result = filter.Filter(
                chunks,
                chunkSizeCells: 16,
                visibleZLevel: 0,
                useVisibleCellRect: true,
                visibleCellRect: new ArcGraphViewCellRect(0, 0, 20, 20));

            bool passed = result.InputChunkCount == 4
                          && result.VisibleChunkCount == 2
                          && result.CulledChunkCount == 2;

            return new ArcGraphTerrainVisibleChunkFilterHarnessResult(
                passed,
                result.InputChunkCount,
                result.VisibleChunkCount,
                result.CulledChunkCount,
                passed ? "SmokePassed" : "SmokeFailed");
        }
    }
}
