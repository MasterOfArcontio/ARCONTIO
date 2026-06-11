namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisualCatalogHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test del catalogo visuale terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contratti grafici verificabili senza scena</b></para>
    /// <para>
    /// Il risultato espone solo numeri e booleani. Questo consente di verificare il
    /// comportamento del catalogo e del resolver senza creare GameObject, mesh o
    /// materiali Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito globale dello smoke test.</item>
    ///   <item><b>DefinitionCount</b>: terrain type caricati.</item>
    ///   <item><b>TransitionSetCount</b>: set di transizione caricati.</item>
    ///   <item><b>VariantStable</b>: la stessa cella produce la stessa variante.</item>
    ///   <item><b>TransitionResolved</b>: una regola bordo viene applicata.</item>
    ///   <item><b>AnimationResolved</b>: un frame animato viene applicato.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualCatalogHarnessResult
    {
        public readonly bool Passed;
        public readonly int DefinitionCount;
        public readonly int TransitionSetCount;
        public readonly bool VariantStable;
        public readonly bool TransitionResolved;
        public readonly bool AnimationResolved;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainVisualCatalogHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato immutabile dello smoke test visual terrain.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualCatalogHarnessResult(
            bool passed,
            int definitionCount,
            int transitionSetCount,
            bool variantStable,
            bool transitionResolved,
            bool animationResolved,
            string reason)
        {
            Passed = passed;
            DefinitionCount = definitionCount < 0 ? 0 : definitionCount;
            TransitionSetCount = transitionSetCount < 0 ? 0 : transitionSetCount;
            VariantStable = variantStable;
            TransitionResolved = transitionResolved;
            AnimationResolved = animationResolved;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualCatalogHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il primo catalogo visuale terrain data-driven.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test del contratto prima del renderer</b></para>
    /// <para>
    /// Questo harness non sostituisce il gate visuale Unity. Serve a dimostrare che
    /// il modello dati puo' rappresentare tre casi chiave: variante stabile, bordo
    /// tra terreni e animazione tile. Solo dopo questa verifica ha senso collegare
    /// il resolver al mesh builder terrain.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario completo da JSON inline.</item>
    ///   <item><b>CreateDefaultJson</b>: catalogo minimo con grass, stone e water.</item>
    ///   <item><b>Resolve*</b>: controlli separati su variante, transizione e animazione.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainVisualCatalogHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno smoke test data-only del catalogo visuale terrain.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainVisualCatalogHarnessResult RunDefaultSmoke()
        {
            bool parsed = ArcGraphTerrainVisualCatalogJson.TryParse(
                CreateDefaultJson(),
                out ArcGraphTerrainVisualCatalog catalog);

            if (!parsed || catalog == null)
            {
                return new ArcGraphTerrainVisualCatalogHarnessResult(
                    false,
                    0,
                    0,
                    false,
                    false,
                    false,
                    "ParseFailed");
            }

            var resolver = new ArcGraphTerrainVisualResolver();
            bool variantStable = ResolveVariantStability(resolver, catalog);
            bool transitionResolved = ResolveTransition(resolver, catalog);
            bool animationResolved = ResolveAnimation(resolver, catalog);
            bool passed = catalog.DefinitionCount == 3
                          && catalog.TransitionSetCount == 1
                          && variantStable
                          && transitionResolved
                          && animationResolved;

            return new ArcGraphTerrainVisualCatalogHarnessResult(
                passed,
                catalog.DefinitionCount,
                catalog.TransitionSetCount,
                variantStable,
                transitionResolved,
                animationResolved,
                passed ? "SmokePassed" : "SmokeFailed");
        }

        private static bool ResolveVariantStability(
            ArcGraphTerrainVisualResolver resolver,
            ArcGraphTerrainVisualCatalog catalog)
        {
            var input = new ArcGraphTerrainVisualResolveInput(
                new ArcGraphCellCoord(12, 9, 0),
                "grass",
                null,
                null,
                visualTimeSeconds: 0f);

            ArcGraphTerrainVisualResolveResult first = resolver.Resolve(catalog, input);
            ArcGraphTerrainVisualResolveResult second = resolver.Resolve(catalog, input);
            return first.TileId == second.TileId
                   && first.UsedVariant
                   && second.UsedVariant;
        }

        private static bool ResolveTransition(
            ArcGraphTerrainVisualResolver resolver,
            ArcGraphTerrainVisualCatalog catalog)
        {
            var input = new ArcGraphTerrainVisualResolveInput(
                new ArcGraphCellCoord(3, 4, 0),
                "grass",
                "stone_floor",
                "E",
                visualTimeSeconds: 0f);

            ArcGraphTerrainVisualResolveResult result = resolver.Resolve(catalog, input);
            return result.TileId == 20 && result.UsedTransition;
        }

        private static bool ResolveAnimation(
            ArcGraphTerrainVisualResolver resolver,
            ArcGraphTerrainVisualCatalog catalog)
        {
            var input = new ArcGraphTerrainVisualResolveInput(
                new ArcGraphCellCoord(0, 0, 0),
                "water",
                null,
                null,
                visualTimeSeconds: 0.26f);

            ArcGraphTerrainVisualResolveResult result = resolver.Resolve(catalog, input);
            return result.TileId == 31 && result.UsedAnimation;
        }

        private static string CreateDefaultJson()
        {
            return "{"
                   + "\"terrains\":["
                   + "{\"terrainId\":\"grass\",\"defaultTileId\":0,\"variants\":[{\"tileId\":0,\"weight\":70},{\"tileId\":1,\"weight\":20},{\"tileId\":2,\"weight\":10}]},"
                   + "{\"terrainId\":\"stone_floor\",\"defaultTileId\":10,\"variants\":[{\"tileId\":10,\"weight\":1}]},"
                   + "{\"terrainId\":\"water\",\"defaultTileId\":30,\"animation\":{\"frameTileIds\":[30,31,32,33],\"frameSeconds\":0.25}}"
                   + "],"
                   + "\"transitions\":["
                   + "{\"fromTerrainId\":\"grass\",\"toTerrainId\":\"stone_floor\",\"rules\":[{\"mask\":\"E\",\"tileId\":20}]}"
                   + "]"
                   + "}";
        }
    }
}
