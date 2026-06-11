namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainAnimationClockHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test del clock animazioni terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: animazione verificata fuori scena</b></para>
    /// <para>
    /// Il test non crea GameObject, Mesh o Material. Verifica solo che il clock
    /// visuale segnali il refresh al momento giusto e che il resolver possa usare
    /// quel tempo per scegliere un frame animato diverso dal primo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito globale.</item>
    ///   <item><b>FirstRefreshDue</b>: true se il primo delta breve non forza refresh.</item>
    ///   <item><b>SecondRefreshDue</b>: true se il secondo delta completa l'intervallo.</item>
    ///   <item><b>FrameAdvanced</b>: true se il resolver passa dal primo al secondo frame.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainAnimationClockHarnessResult
    {
        public readonly bool Passed;
        public readonly bool FirstRefreshDue;
        public readonly bool SecondRefreshDue;
        public readonly bool FrameAdvanced;
        public readonly int FirstFrameTileId;
        public readonly int SecondFrameTileId;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainAnimationClockHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato immutabile dello smoke test animazioni terrain.
        /// </para>
        /// </summary>
        public ArcGraphTerrainAnimationClockHarnessResult(
            bool passed,
            bool firstRefreshDue,
            bool secondRefreshDue,
            bool frameAdvanced,
            int firstFrameTileId,
            int secondFrameTileId,
            string reason)
        {
            Passed = passed;
            FirstRefreshDue = firstRefreshDue;
            SecondRefreshDue = secondRefreshDue;
            FrameAdvanced = frameAdvanced;
            FirstFrameTileId = firstFrameTileId;
            SecondFrameTileId = secondFrameTileId;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainAnimationClockHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il clock delle animazioni terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: refresh controllato, non redraw continuo</b></para>
    /// <para>
    /// Il test dimostra che un delta breve non sporca subito i chunk, mentre il
    /// raggiungimento dell'intervallo di refresh produce un segnale esplicito. Poi
    /// usa lo stesso tempo visuale nel resolver per verificare che un terreno
    /// animato, come l'acqua, possa avanzare di frame.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo clock + resolver acqua.</item>
    ///   <item><b>CreateWaterCatalog</b>: catalogo visuale con acqua a quattro frame.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainAnimationClockHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test data-only sul clock animazione terrain.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainAnimationClockHarnessResult RunDefaultSmoke()
        {
            var clock = new ArcGraphTerrainAnimationClock();
            ArcGraphTerrainAnimationClockStep firstStep = clock.Advance(
                deltaSeconds: 0.10f,
                refreshSeconds: 0.25f);
            ArcGraphTerrainAnimationClockStep secondStep = clock.Advance(
                deltaSeconds: 0.15f,
                refreshSeconds: 0.25f);

            var resolver = new ArcGraphTerrainVisualResolver();
            ArcGraphTerrainVisualCatalog catalog = CreateWaterCatalog();
            ArcGraphTerrainVisualResolveResult firstFrame = resolver.Resolve(
                catalog,
                new ArcGraphTerrainVisualResolveInput(
                    new ArcGraphCellCoord(0, 0, 0),
                    "water",
                    neighborTerrainId: null,
                    neighborMask: null,
                    visualTimeSeconds: 0f));
            ArcGraphTerrainVisualResolveResult secondFrame = resolver.Resolve(
                catalog,
                new ArcGraphTerrainVisualResolveInput(
                    new ArcGraphCellCoord(0, 0, 0),
                    "water",
                    neighborTerrainId: null,
                    neighborMask: null,
                    visualTimeSeconds: secondStep.VisualTimeSeconds));

            bool frameAdvanced = firstFrame.TileId == 30
                                 && secondFrame.TileId == 31
                                 && secondFrame.UsedAnimation;
            bool passed = !firstStep.RefreshDue
                          && secondStep.RefreshDue
                          && frameAdvanced;

            return new ArcGraphTerrainAnimationClockHarnessResult(
                passed,
                firstStep.RefreshDue,
                secondStep.RefreshDue,
                frameAdvanced,
                firstFrame.TileId,
                secondFrame.TileId,
                passed ? "TerrainAnimationClockSmokePassed" : "TerrainAnimationClockSmokeFailed");
        }

        private static ArcGraphTerrainVisualCatalog CreateWaterCatalog()
        {
            return new ArcGraphTerrainVisualCatalog(
                new[]
                {
                    new ArcGraphTerrainVisualDefinition(
                        "water",
                        defaultTileId: 30,
                        variants: null,
                        animation: new ArcGraphTerrainVisualAnimation(new[] { 30, 31, 32, 33 }, 0.25f))
                },
                new ArcGraphTerrainVisualTransitionSet[0]);
        }
    }
}
