using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisualCoverageDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica passiva della copertura tra catalogo visuale terrain e catalogo UV.
    /// </para>
    ///
    /// <para><b>Principio architetturale: authoring verificabile prima del gate visuale</b></para>
    /// <para>
    /// Il visual catalog puo' dichiarare tile base, varianti, frame animati e
    /// transizioni. Il catalogo UV deve poi sapere dove quei tile stanno nell'atlas.
    /// Questa diagnostica controlla la coerenza tra i due cataloghi senza creare
    /// mesh, senza leggere texture, senza caricare asset e senza toccare la scena.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RequiredTileCount</b>: tile richiesti dal visual catalog.</item>
    ///   <item><b>CoveredTileCount</b>: tile richiesti presenti nel catalogo UV.</item>
    ///   <item><b>MissingTileCount</b>: tile richiesti ma non coperti.</item>
    ///   <item><b>FirstMissingTileId</b>: primo id mancante per correzione rapida.</item>
    ///   <item><b>Reason</b>: esito sintetico del controllo.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualCoverageDiagnostics
    {
        public readonly bool HasVisualCatalog;
        public readonly bool HasUvCatalog;
        public readonly bool IsFullyCovered;
        public readonly int RequiredTileCount;
        public readonly int CoveredTileCount;
        public readonly int MissingTileCount;
        public readonly int FirstMissingTileId;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainVisualCoverageDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile di coverage terrain.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualCoverageDiagnostics(
            bool hasVisualCatalog,
            bool hasUvCatalog,
            bool isFullyCovered,
            int requiredTileCount,
            int coveredTileCount,
            int missingTileCount,
            int firstMissingTileId,
            string reason)
        {
            HasVisualCatalog = hasVisualCatalog;
            HasUvCatalog = hasUvCatalog;
            IsFullyCovered = isFullyCovered;
            RequiredTileCount = requiredTileCount < 0 ? 0 : requiredTileCount;
            CoveredTileCount = coveredTileCount < 0 ? 0 : coveredTileCount;
            MissingTileCount = missingTileCount < 0 ? 0 : missingTileCount;
            FirstMissingTileId = firstMissingTileId;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualCoverageAnalyzer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Analizzatore data-only della copertura tile richiesta dal visual catalog.
    /// </para>
    ///
    /// <para><b>Principio architetturale: controllo cataloghi, non rendering</b></para>
    /// <para>
    /// L'analizzatore lavora solo su cataloghi runtime gia' parsati. Non conosce
    /// materiali Unity, non legge texture, non costruisce UV e non decide quale tile
    /// usare in una cella. Dice soltanto se tutti i tile dichiarati dal visual
    /// catalog esistono anche nel catalogo UV.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Analyze</b>: entry point del controllo.</item>
    ///   <item><b>CollectRequiredTiles</b>: raccoglie default, varianti, animazioni e transizioni.</item>
    ///   <item><b>CollectAvailableTiles</b>: raccoglie gli id presenti nel catalogo UV.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainVisualCoverageAnalyzer
    {
        // =============================================================================
        // Analyze
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta visual catalog e catalogo UV producendo diagnostica leggibile.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualCoverageDiagnostics Analyze(
            ArcGraphTerrainVisualCatalog visualCatalog,
            ArcGraphTerrainCatalog uvCatalog)
        {
            if (visualCatalog == null)
            {
                return new ArcGraphTerrainVisualCoverageDiagnostics(
                    false,
                    uvCatalog != null,
                    false,
                    0,
                    0,
                    0,
                    -1,
                    "VisualCatalogMissing");
            }

            if (uvCatalog == null)
            {
                int requiredWithoutUv = CollectRequiredTiles(visualCatalog).Count;
                return new ArcGraphTerrainVisualCoverageDiagnostics(
                    true,
                    false,
                    false,
                    requiredWithoutUv,
                    0,
                    requiredWithoutUv,
                    requiredWithoutUv > 0 ? 0 : -1,
                    "UvCatalogMissing");
            }

            HashSet<int> required = CollectRequiredTiles(visualCatalog);
            HashSet<int> available = CollectAvailableTiles(uvCatalog);

            int covered = 0;
            int missing = 0;
            int firstMissing = -1;

            foreach (int tileId in required)
            {
                if (available.Contains(tileId))
                {
                    covered++;
                    continue;
                }

                missing++;
                if (firstMissing < 0 || tileId < firstMissing)
                    firstMissing = tileId;
            }

            bool fullyCovered = required.Count > 0 && missing == 0;
            return new ArcGraphTerrainVisualCoverageDiagnostics(
                true,
                true,
                fullyCovered,
                required.Count,
                covered,
                missing,
                firstMissing,
                fullyCovered ? "CoverageComplete" : "CoverageMissingTiles");
        }

        private static HashSet<int> CollectRequiredTiles(ArcGraphTerrainVisualCatalog visualCatalog)
        {
            var required = new HashSet<int>();

            for (int i = 0; i < visualCatalog.Definitions.Count; i++)
            {
                ArcGraphTerrainVisualDefinition definition = visualCatalog.Definitions[i];
                if (definition == null)
                    continue;

                required.Add(definition.DefaultTileId);

                for (int v = 0; v < definition.Variants.Count; v++)
                    required.Add(definition.Variants[v].TileId);

                if (definition.HasAnimation)
                {
                    for (int f = 0; f < definition.Animation.FrameTileIds.Count; f++)
                        required.Add(definition.Animation.FrameTileIds[f]);
                }
            }

            for (int i = 0; i < visualCatalog.TransitionSets.Count; i++)
            {
                ArcGraphTerrainVisualTransitionSet transitionSet = visualCatalog.TransitionSets[i];
                if (transitionSet == null)
                    continue;

                for (int r = 0; r < transitionSet.Rules.Count; r++)
                    required.Add(transitionSet.Rules[r].TileId);
            }

            return required;
        }

        private static HashSet<int> CollectAvailableTiles(ArcGraphTerrainCatalog uvCatalog)
        {
            var available = new HashSet<int>();
            for (int i = 0; i < uvCatalog.Entries.Count; i++)
                available.Add(uvCatalog.Entries[i].Id);

            return available;
        }
    }
}
