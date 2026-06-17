using System;
using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentPlantCatalogConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del catalogo specie vegetali importanti.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo biologico data-driven</b></para>
    /// <para>
    /// Il catalogo deve poter vivere in un file di configurazione futuro. Questo DTO
    /// non carica file, non conosce Unity Resources e non collega sprite o prefab:
    /// descrive soltanto specie, requisiti ecologici e stadi dichiarativi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>species</b>: definizioni configurabili delle specie.</item>
    ///   <item><b>ToCatalog</b>: costruisce un catalogo read-only normalizzato.</item>
    ///   <item><b>CreateDefaultSet</b>: seed minimo per test e bootstrap data-only.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentPlantCatalogConfig
    {
        public EnvironmentPlantSpeciesConfig[] species =
            EnvironmentPlantSpeciesConfig.CreateDefaultSet();

        // =============================================================================
        // ToCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la configurazione in catalogo read-only.
        /// </para>
        /// </summary>
        public EnvironmentPlantCatalog ToCatalog()
        {
            var safeSpecies = species ?? new EnvironmentPlantSpeciesConfig[0];
            var definitions = new List<EnvironmentPlantSpeciesDefinition>(safeSpecies.Length);
            for (int i = 0; i < safeSpecies.Length; i++)
            {
                if (safeSpecies[i] == null)
                    continue;

                definitions.Add(safeSpecies[i].ToDefinition());
            }

            return new EnvironmentPlantCatalog(definitions);
        }
    }

    // =============================================================================
    // EnvironmentPlantGrowthStageConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile di uno stadio crescita vegetale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: soglie di crescita fuori dal codice caldo</b></para>
    /// <para>
    /// I giorni richiesti per ogni stadio sono dati di bilanciamento. Il Core li
    /// normalizza in value type, ma non avanza piante e non decide raccolti in
    /// questo checkpoint.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>stageKey</b>: chiave leggibile dello stadio.</item>
    ///   <item><b>requiredAgeDays</b>: eta' minima in giorni ambientali.</item>
    ///   <item><b>maturity01</b>: maturita' normalizzata.</item>
    ///   <item><b>isHarvestable</b>: flag di produzione futura.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentPlantGrowthStageConfig
    {
        public string stageKey = "sprout";
        public int requiredAgeDays;
        public float maturity01;
        public bool isHarvestable;

        // =============================================================================
        // ToDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il DTO in definizione value-only.
        /// </para>
        /// </summary>
        public EnvironmentPlantGrowthStageDefinition ToDefinition()
        {
            return new EnvironmentPlantGrowthStageDefinition(
                stageKey,
                requiredAgeDays,
                maturity01,
                isHarvestable);
        }
    }

    // =============================================================================
    // EnvironmentPlantSpeciesConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile di una specie vegetale importante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: specie separata ma collegabile agli oggetti</b></para>
    /// <para>
    /// Una specie non duplica <c>ObjectDef</c>: descrive biologia, stadi e requisiti
    /// ecologici. Quando una pianta viva o un raccolto dovranno diventare oggetti
    /// del mondo, questa configurazione conservera' soltanto le chiavi ponte verso
    /// il catalogo oggetti, lasciando a <c>object_defs.json</c> l'autorita' su
    /// sprite, ingombro, interazione e proprieta' materiali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>speciesKey</b>: chiave stabile della specie.</item>
    ///   <item><b>category</b>: categoria configurabile.</item>
    ///   <item><b>growthStages</b>: stadi crescita.</item>
    ///   <item><b>favorableSeasons</b>: nomi stagione favorevoli.</item>
    ///   <item><b>idealTemperature01/idealHumidity01/minimumFertility01</b>: requisiti ecologici.</item>
    ///   <item><b>resourceOutputKey</b>: output futuro dichiarativo.</item>
    ///   <item><b>liveObjectDefinitionKey</b>: ObjectDef futuro per la pianta viva, se materializzata.</item>
    ///   <item><b>harvestObjectDefinitionKey</b>: ObjectDef futuro per il prodotto raccolto, se materializzato.</item>
    ///   <item><b>seasonalBehavior</b>: comportamento stagionale configurabile.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentPlantSpeciesConfig
    {
        public string speciesKey = "wild_grass";
        public string category = "Grass";
        public EnvironmentPlantGrowthStageConfig[] growthStages =
            CreateDefaultGrowthStages();
        public string[] favorableSeasons = { "Spring", "Summer", "Autumn" };
        public float idealTemperature01 = 0.55f;
        public float idealHumidity01 = 0.55f;
        public float minimumFertility01 = 0.25f;
        public string resourceOutputKey = string.Empty;
        public string liveObjectDefinitionKey = string.Empty;
        public string harvestObjectDefinitionKey = string.Empty;
        public string seasonalBehavior = "Perennial";

        // =============================================================================
        // ToDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la specie configurabile in definizione read-only.
        /// </para>
        /// </summary>
        public EnvironmentPlantSpeciesDefinition ToDefinition()
        {
            var safeStages = growthStages ?? new EnvironmentPlantGrowthStageConfig[0];
            var stageDefinitions =
                new EnvironmentPlantGrowthStageDefinition[safeStages.Length];
            for (int i = 0; i < safeStages.Length; i++)
            {
                // Stage nulli diventano uno stadio innocuo; il validatore li segnala,
                // ma il catalogo resta costruibile e deterministico.
                stageDefinitions[i] = safeStages[i] != null
                    ? safeStages[i].ToDefinition()
                    : new EnvironmentPlantGrowthStageDefinition("stage", 0, 0f, false);
            }

            return new EnvironmentPlantSpeciesDefinition(
                speciesKey,
                EnvironmentPlantCatalogParsing.ParsePlantCategory(category),
                stageDefinitions,
                EnvironmentPlantCatalogParsing.ParseSeasonMask(favorableSeasons),
                idealTemperature01,
                idealHumidity01,
                minimumFertility01,
                resourceOutputKey,
                liveObjectDefinitionKey,
                harvestObjectDefinitionKey,
                EnvironmentPlantCatalogParsing.ParseSeasonalBehavior(seasonalBehavior));
        }

        public static EnvironmentPlantSpeciesConfig[] CreateDefaultSet()
        {
            return new[]
            {
                new EnvironmentPlantSpeciesConfig
                {
                    speciesKey = "wild_grass",
                    category = "Grass",
                    growthStages = CreateDefaultGrowthStages(),
                    favorableSeasons = new[] { "Spring", "Summer", "Autumn" },
                    idealTemperature01 = 0.55f,
                    idealHumidity01 = 0.55f,
                    minimumFertility01 = 0.20f,
                    resourceOutputKey = "grass_cuttings",
                    harvestObjectDefinitionKey = "grass_cuttings",
                    seasonalBehavior = "Perennial"
                },
                new EnvironmentPlantSpeciesConfig
                {
                    speciesKey = "oak_tree",
                    category = "Tree",
                    growthStages = new[]
                    {
                        new EnvironmentPlantGrowthStageConfig
                        {
                            stageKey = "sapling",
                            requiredAgeDays = 0,
                            maturity01 = 0.15f
                        },
                        new EnvironmentPlantGrowthStageConfig
                        {
                            stageKey = "young",
                            requiredAgeDays = 90,
                            maturity01 = 0.45f
                        },
                        new EnvironmentPlantGrowthStageConfig
                        {
                            stageKey = "adult",
                            requiredAgeDays = 300,
                            maturity01 = 1f,
                            isHarvestable = true
                        }
                    },
                    favorableSeasons = new[] { "Spring", "Summer" },
                    idealTemperature01 = 0.50f,
                    idealHumidity01 = 0.65f,
                    minimumFertility01 = 0.55f,
                    resourceOutputKey = "wood",
                    harvestObjectDefinitionKey = "wood",
                    seasonalBehavior = "Deciduous"
                }
            };
        }

        private static EnvironmentPlantGrowthStageConfig[] CreateDefaultGrowthStages()
        {
            return new[]
            {
                new EnvironmentPlantGrowthStageConfig
                {
                    stageKey = "sprout",
                    requiredAgeDays = 0,
                    maturity01 = 0.15f
                },
                new EnvironmentPlantGrowthStageConfig
                {
                    stageKey = "mature",
                    requiredAgeDays = 8,
                    maturity01 = 1f,
                    isHarvestable = true
                }
            };
        }
    }

    // =============================================================================
    // EnvironmentPlantCatalogParsing
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper interno di parsing per il catalogo piante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stringhe ai bordi, enum nel Core</b></para>
    /// <para>
    /// I file futuri possono usare stringhe leggibili. Il Core, invece, riceve enum
    /// e maschere normalizzate con fallback conservativi e senza eccezioni.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ParsePlantCategory</b>: converte categoria specie.</item>
    ///   <item><b>ParseSeasonalBehavior</b>: converte comportamento stagionale.</item>
    ///   <item><b>ParseSeasonMask</b>: converte lista stagioni in bitmask.</item>
    /// </list>
    /// </summary>
    internal static class EnvironmentPlantCatalogParsing
    {
        public static EnvironmentPlantCategory ParsePlantCategory(string value)
        {
            if (string.Equals(value, "Shrub", StringComparison.OrdinalIgnoreCase))
                return EnvironmentPlantCategory.Shrub;

            if (string.Equals(value, "Tree", StringComparison.OrdinalIgnoreCase))
                return EnvironmentPlantCategory.Tree;

            if (string.Equals(value, "Crop", StringComparison.OrdinalIgnoreCase))
                return EnvironmentPlantCategory.Crop;

            if (string.Equals(value, "Medicinal", StringComparison.OrdinalIgnoreCase))
                return EnvironmentPlantCategory.Medicinal;

            return EnvironmentPlantCategory.Grass;
        }

        public static EnvironmentPlantSeasonalBehavior ParseSeasonalBehavior(string value)
        {
            if (string.Equals(value, "Annual", StringComparison.OrdinalIgnoreCase))
                return EnvironmentPlantSeasonalBehavior.Annual;

            if (string.Equals(value, "Deciduous", StringComparison.OrdinalIgnoreCase))
                return EnvironmentPlantSeasonalBehavior.Deciduous;

            if (string.Equals(value, "Evergreen", StringComparison.OrdinalIgnoreCase))
                return EnvironmentPlantSeasonalBehavior.Evergreen;

            return EnvironmentPlantSeasonalBehavior.Perennial;
        }

        public static int ParseSeasonMask(string[] values)
        {
            if (values == null || values.Length == 0)
                return BuildAllSeasonMask();

            int mask = 0;
            for (int i = 0; i < values.Length; i++)
            {
                EnvironmentSeasonKind season =
                    EnvironmentConfigParsing.ParseSeason(values[i]);
                mask |= 1 << (int)season;
            }

            return mask == 0 ? BuildAllSeasonMask() : mask;
        }

        private static int BuildAllSeasonMask()
        {
            return (1 << (int)EnvironmentSeasonKind.Spring)
                   | (1 << (int)EnvironmentSeasonKind.Summer)
                   | (1 << (int)EnvironmentSeasonKind.Autumn)
                   | (1 << (int)EnvironmentSeasonKind.Winter);
        }
    }
}
