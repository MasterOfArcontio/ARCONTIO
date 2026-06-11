using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVariantDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica data-only della scelta delle varianti tile per un terrain type.
    /// </para>
    ///
    /// <para><b>Principio architetturale: varieta' grafica stabile, non random runtime</b></para>
    /// <para>
    /// Le varianti del terreno devono dare varietà visiva alla mappa senza cambiare
    /// a ogni frame. Questa diagnostica controlla una finestra rettangolare di celle
    /// e verifica che la stessa cella risolva sempre lo stesso tile, mentre celle
    /// diverse possano distribuire piu' varianti dichiarate dal catalogo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SampleCount</b>: celle controllate.</item>
    ///   <item><b>StableSampleCount</b>: celle che hanno prodotto lo stesso tile in due risoluzioni consecutive.</item>
    ///   <item><b>ChangedSampleCount</b>: celle instabili, da considerare errore per le varianti statiche.</item>
    ///   <item><b>DistinctTileCount</b>: tile diversi effettivamente incontrati nel campione.</item>
    ///   <item><b>Reason</b>: esito sintetico leggibile per log e harness.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVariantDiagnostics
    {
        public readonly bool HasCatalog;
        public readonly bool HasTerrainDefinition;
        public readonly bool IsStable;
        public readonly bool HasVariation;
        public readonly int SampleCount;
        public readonly int StableSampleCount;
        public readonly int ChangedSampleCount;
        public readonly int DistinctTileCount;
        public readonly int FirstTileId;
        public readonly int FirstChangedCellX;
        public readonly int FirstChangedCellY;
        public readonly int FirstChangedCellZ;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainVariantDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile della distribuzione varianti.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVariantDiagnostics(
            bool hasCatalog,
            bool hasTerrainDefinition,
            bool isStable,
            bool hasVariation,
            int sampleCount,
            int stableSampleCount,
            int changedSampleCount,
            int distinctTileCount,
            int firstTileId,
            int firstChangedCellX,
            int firstChangedCellY,
            int firstChangedCellZ,
            string reason)
        {
            HasCatalog = hasCatalog;
            HasTerrainDefinition = hasTerrainDefinition;
            IsStable = isStable;
            HasVariation = hasVariation;
            SampleCount = NormalizeCount(sampleCount);
            StableSampleCount = NormalizeCount(stableSampleCount);
            ChangedSampleCount = NormalizeCount(changedSampleCount);
            DistinctTileCount = NormalizeCount(distinctTileCount);
            FirstTileId = firstTileId;
            FirstChangedCellX = firstChangedCellX;
            FirstChangedCellY = firstChangedCellY;
            FirstChangedCellZ = firstChangedCellZ;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVariantAnalyzer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Analizzatore passivo della stabilita' delle varianti terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica del catalogo prima della scena</b></para>
    /// <para>
    /// L'analizzatore usa solo il catalogo visuale e il resolver gia' esistenti.
    /// Non crea mesh, non carica texture, non legge asset Unity e non interroga la
    /// mappa reale. Serve a confermare che il catalogo puo' produrre una variazione
    /// visiva ripetibile prima di portarla nel gate visuale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Analyze</b>: controlla un rettangolo di celle per un terrain id.</item>
    ///   <item><b>ResolveTwice</b>: risolve due volte la stessa cella per intercettare instabilita'.</item>
    ///   <item><b>HashSet tile</b>: conta quanti tile distinti appaiono nel campione.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainVariantAnalyzer
    {
        // =============================================================================
        // Analyze
        // =============================================================================
        /// <summary>
        /// <para>
        /// Analizza stabilita' e distribuzione delle varianti per un terrain type.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVariantDiagnostics Analyze(
            ArcGraphTerrainVisualCatalog catalog,
            string terrainId,
            int sampleWidth,
            int sampleHeight,
            int zLevel,
            float visualTimeSeconds)
        {
            if (catalog == null)
            {
                return new ArcGraphTerrainVariantDiagnostics(
                    false,
                    false,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    -1,
                    -1,
                    -1,
                    -1,
                    "CatalogMissing");
            }

            if (!catalog.TryGetDefinition(terrainId, out var definition) || definition == null)
            {
                return new ArcGraphTerrainVariantDiagnostics(
                    true,
                    false,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    -1,
                    -1,
                    -1,
                    -1,
                    "TerrainDefinitionMissing");
            }

            int width = sampleWidth > 0 ? sampleWidth : 1;
            int height = sampleHeight > 0 ? sampleHeight : 1;
            int sampleCount = 0;
            int stableCount = 0;
            int changedCount = 0;
            int firstTileId = -1;
            int firstChangedX = -1;
            int firstChangedY = -1;
            int firstChangedZ = -1;
            var distinctTiles = new HashSet<int>();
            var resolver = new ArcGraphTerrainVisualResolver();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new ArcGraphCellCoord(x, y, zLevel);
                    var input = new ArcGraphTerrainVisualResolveInput(
                        cell,
                        definition.TerrainId,
                        null,
                        null,
                        visualTimeSeconds);

                    // Ogni cella viene risolta due volte di fila: se il risultato cambia,
                    // la variante sta usando una sorgente non stabile e il gate deve fallire.
                    ArcGraphTerrainVisualResolveResult first = resolver.Resolve(catalog, input);
                    ArcGraphTerrainVisualResolveResult second = resolver.Resolve(catalog, input);

                    sampleCount++;
                    distinctTiles.Add(first.TileId);

                    if (firstTileId < 0)
                        firstTileId = first.TileId;

                    if (first.TileId == second.TileId)
                    {
                        stableCount++;
                        continue;
                    }

                    changedCount++;
                    if (firstChangedX < 0)
                    {
                        firstChangedX = x;
                        firstChangedY = y;
                        firstChangedZ = zLevel;
                    }
                }
            }

            bool isStable = sampleCount > 0 && changedCount == 0;
            bool hasVariation = distinctTiles.Count > 1;
            string reason = BuildReason(isStable, hasVariation, definition);

            return new ArcGraphTerrainVariantDiagnostics(
                true,
                true,
                isStable,
                hasVariation,
                sampleCount,
                stableCount,
                changedCount,
                distinctTiles.Count,
                firstTileId,
                firstChangedX,
                firstChangedY,
                firstChangedZ,
                reason);
        }

        private static string BuildReason(
            bool isStable,
            bool hasVariation,
            ArcGraphTerrainVisualDefinition definition)
        {
            if (!isStable)
                return "VariantChangedBetweenResolves";

            if (definition.HasAnimation)
                return "AnimatedTerrainNotVariantDriven";

            if (!hasVariation && definition.VariantCount > 1)
                return "StableButSampleDidNotHitMultipleVariants";

            if (!hasVariation)
                return "StableSingleVariant";

            return "StableVariantDistribution";
        }
    }
}
