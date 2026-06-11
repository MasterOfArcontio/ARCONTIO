namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVariantHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sulle varianti terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test data-only prima del gate visuale</b></para>
    /// <para>
    /// Il risultato espone solo dati semplici: parse del catalogo, stabilita' delle
    /// celle, numero di tile distinti e motivo dell'esito. Non contiene riferimenti
    /// a Unity, materiali, mesh o texture.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito globale dello smoke test.</item>
    ///   <item><b>CatalogParsed</b>: true se il catalogo inline e' valido.</item>
    ///   <item><b>Diagnostics</b>: diagnostica dettagliata prodotta dall'analizzatore.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVariantHarnessResult
    {
        public readonly bool Passed;
        public readonly bool CatalogParsed;
        public readonly ArcGraphTerrainVariantDiagnostics Diagnostics;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainVariantHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato immutabile dello smoke test varianti terrain.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVariantHarnessResult(
            bool passed,
            bool catalogParsed,
            ArcGraphTerrainVariantDiagnostics diagnostics,
            string reason)
        {
            Passed = passed;
            CatalogParsed = catalogParsed;
            Diagnostics = diagnostics;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVariantHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per verificare le varianti tile terrain senza scena.
    /// </para>
    ///
    /// <para><b>Principio architetturale: varianti deterministiche e leggere</b></para>
    /// <para>
    /// Le variazioni grafiche del terreno devono essere una conseguenza stabile del
    /// catalogo e delle coordinate. Questo harness dimostra che un terreno come
    /// <c>grass</c> puo' usare piu' tile su celle diverse, ma che ogni singola cella
    /// resta inchiodata allo stesso tile finche' non cambiano dati o catalogo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo su prato con tre varianti pesate.</item>
    ///   <item><b>CreateVariantJson</b>: JSON inline autosufficiente per il test.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainVariantHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno smoke test data-only delle varianti del terreno.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainVariantHarnessResult RunDefaultSmoke()
        {
            bool parsed = ArcGraphTerrainVisualCatalogJson.TryParse(
                CreateVariantJson(),
                out ArcGraphTerrainVisualCatalog catalog);

            if (!parsed || catalog == null)
            {
                return new ArcGraphTerrainVariantHarnessResult(
                    false,
                    false,
                    default,
                    "ParseFailed");
            }

            var analyzer = new ArcGraphTerrainVariantAnalyzer();
            ArcGraphTerrainVariantDiagnostics diagnostics = analyzer.Analyze(
                catalog,
                "grass",
                sampleWidth: 16,
                sampleHeight: 16,
                zLevel: 0,
                visualTimeSeconds: 0f);

            bool passed = diagnostics.HasCatalog
                          && diagnostics.HasTerrainDefinition
                          && diagnostics.IsStable
                          && diagnostics.HasVariation
                          && diagnostics.SampleCount == 256
                          && diagnostics.ChangedSampleCount == 0
                          && diagnostics.DistinctTileCount > 1;

            return new ArcGraphTerrainVariantHarnessResult(
                passed,
                true,
                diagnostics,
                passed ? "VariantSmokePassed" : diagnostics.Reason);
        }

        private static string CreateVariantJson()
        {
            return "{"
                   + "\"terrains\":["
                   + "{\"terrainId\":\"grass\",\"defaultTileId\":0,\"variants\":[{\"tileId\":0,\"weight\":70},{\"tileId\":1,\"weight\":20},{\"tileId\":2,\"weight\":10}]}"
                   + "],"
                   + "\"transitions\":[]"
                   + "}";
        }
    }
}
