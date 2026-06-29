using System;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisualCoverageHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sulla coverage dei cataloghi terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica cataloghi senza scena</b></para>
    /// <para>
    /// Il risultato permette di controllare se l'analizzatore riconosce correttamente
    /// un catalogo completo e un catalogo con tile mancante senza creare mesh,
    /// GameObject, materiali o texture.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito globale.</item>
    ///   <item><b>CompleteCoveragePassed</b>: scenario completo riconosciuto.</item>
    ///   <item><b>MissingCoveragePassed</b>: scenario mancante riconosciuto.</item>
    ///   <item><b>Reason</b>: motivo sintetico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualCoverageHarnessResult
    {
        public readonly bool Passed;
        public readonly bool CompleteCoveragePassed;
        public readonly bool MissingCoveragePassed;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainVisualCoverageHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato immutabile dello smoke test coverage.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualCoverageHarnessResult(
            bool passed,
            bool completeCoveragePassed,
            bool missingCoveragePassed,
            string reason)
        {
            Passed = passed;
            CompleteCoveragePassed = completeCoveragePassed;
            MissingCoveragePassed = missingCoveragePassed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualCoverageHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare l'analisi coverage visual catalog -> UV catalog.
    /// </para>
    ///
    /// <para><b>Principio architetturale: controllo authoring prima degli asset reali</b></para>
    /// <para>
    /// Questo harness dimostra che la diagnostica puo' intercettare tile dichiarati
    /// dal visual catalog ma non presenti nel catalogo UV, prima di arrivare a un
    /// gate visuale Unity con sprite mancanti o fallback invisibili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: esegue scenario completo e scenario mancante.</item>
    ///   <item><b>CreateVisualCatalog</b>: catalogo visuale minimo con varianti e animazione.</item>
    ///   <item><b>CreateUvCatalog</b>: catalogo UV completo o incompleto.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainVisualCoverageHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno smoke test data-only della diagnostica coverage.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainVisualCoverageHarnessResult RunDefaultSmoke()
        {
            var analyzer = new ArcGraphTerrainVisualCoverageAnalyzer();
            ArcGraphTerrainVisualCatalog visualCatalog = CreateVisualCatalog();

            ArcGraphTerrainVisualCoverageDiagnostics complete = analyzer.Analyze(
                visualCatalog,
                CreateUvCatalog(includeAllTiles: true));

            ArcGraphTerrainVisualCoverageDiagnostics missing = analyzer.Analyze(
                visualCatalog,
                CreateUvCatalog(includeAllTiles: false));

            bool completePassed = complete.IsFullyCovered
                                  && complete.RequiredTileCount == 4
                                  && complete.MissingTileCount == 0;

            bool missingPassed = !missing.IsFullyCovered
                                 && missing.RequiredTileCount == 4
                                 && missing.MissingTileCount == 1
                                 && missing.FirstMissingTileId == 31;

            bool passed = completePassed && missingPassed;
            return new ArcGraphTerrainVisualCoverageHarnessResult(
                passed,
                completePassed,
                missingPassed,
                passed ? "CoverageSmokePassed" : "CoverageSmokeFailed");
        }

        private static ArcGraphTerrainVisualCatalog CreateVisualCatalog()
        {
            return new ArcGraphTerrainVisualCatalog(
                new[]
                {
                    new ArcGraphTerrainVisualDefinition(
                        "grass",
                        defaultTileId: 0,
                        new[]
                        {
                            new ArcGraphTerrainVisualVariant(0, 1),
                            new ArcGraphTerrainVisualVariant(1, 1)
                        },
                        new ArcGraphTerrainVisualAnimation(null, 0f)),
                    new ArcGraphTerrainVisualDefinition(
                        "water",
                        defaultTileId: 30,
                        null,
                        new ArcGraphTerrainVisualAnimation(new[] { 30, 31 }, 0.25f))
                },
                Array.Empty<ArcGraphTerrainVisualTransitionSet>());
        }

        private static ArcGraphTerrainCatalog CreateUvCatalog(bool includeAllTiles)
        {
            return new ArcGraphTerrainCatalog(
                "ArcGraph/Atlas/TerrainBase",
                tilePixels: 32,
                atlasWidthPixels: 128,
                atlasHeightPixels: 32,
                uvInsetPixels: 0f,
                includeAllTiles
                    ? new[]
                    {
                        new ArcGraphTerrainCatalogEntry(0, "grass_base", 0, 0),
                        new ArcGraphTerrainCatalogEntry(1, "grass_alt", 1, 0),
                        new ArcGraphTerrainCatalogEntry(30, "water_00", 2, 0),
                        new ArcGraphTerrainCatalogEntry(31, "water_01", 3, 0)
                    }
                    : new[]
                    {
                        new ArcGraphTerrainCatalogEntry(0, "grass_base", 0, 0),
                        new ArcGraphTerrainCatalogEntry(1, "grass_alt", 1, 0),
                        new ArcGraphTerrainCatalogEntry(30, "water_00", 2, 0)
                    });
        }
    }
}
