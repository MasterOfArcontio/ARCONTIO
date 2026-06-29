using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Environment;
using NUnit.Framework;

namespace Arcontio.EditorTests
{
    // =============================================================================
    // EnvironmentBiologicalProductCatalogQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test EditMode del catalogo prodotti biologici separato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: cataloghi separati, contratto verificato</b></para>
    /// <para>
    /// La Biosfera dichiara productKey tramite il catalogo piante. Il catalogo
    /// prodotti biologici risolve quel productKey in un contratto semantico, mentre
    /// il catalogo oggetti resta responsabile di item e nutrizione. Questi test
    /// proteggono proprio quel triangolo, senza creare job o inventario.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Catalogo</b>: verifica wood_log, acorn e berry.</item>
    ///   <item><b>Validatore</b>: verifica link a ObjectDef e regole food/non-food.</item>
    ///   <item><b>Query legacy</b>: conferma che le query Biosfera continuano a usare productKey.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentBiologicalProductCatalogQaTests
    {
        // =============================================================================
        // DefaultCatalogLoadsInitialBiologicalProducts
        // =============================================================================
        /// <summary>
        /// <para>
        /// Il catalogo default espone i tre prodotti richiesti dalla v0.71.05.A.
        /// </para>
        /// </summary>
        [Test]
        public void DefaultCatalogLoadsInitialBiologicalProducts()
        {
            EnvironmentBiologicalProductCatalog catalog =
                MakeProductCatalogConfig().ToCatalog();

            Assert.That(catalog.ContainsProduct("wood_log"), Is.True);
            Assert.That(catalog.ContainsProduct("acorn"), Is.True);
            Assert.That(catalog.ContainsProduct("berry"), Is.True);

            Assert.That(catalog.TryGetProduct("acorn", out var acorn), Is.True);
            Assert.That(acorn.ObjectDefId, Is.EqualTo("acorn"));
            Assert.That(acorn.IsFood, Is.True);
            Assert.That(acorn.HasCategory(EnvironmentBiologicalProductCategory.Seed), Is.True);

            Assert.That(catalog.TryGetProduct("wood_log", out var wood), Is.True);
            Assert.That(wood.ObjectDefId, Is.EqualTo("wood_log"));
            Assert.That(wood.IsFood, Is.False);
            Assert.That(wood.RecommendedToolKey, Is.EqualTo("axe"));
        }

        // =============================================================================
        // PlantProductsResolveToBiologicalProductCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ogni productKey dichiarato dalle specie del test esiste nel catalogo
        /// prodotti biologici separato.
        /// </para>
        /// </summary>
        [Test]
        public void PlantProductsResolveToBiologicalProductCatalog()
        {
            EnvironmentConfigValidationResult validation =
                EnvironmentBiologicalProductCatalogValidator.Validate(
                    MakePlantCatalogConfig(),
                    MakeProductCatalogConfig(),
                    MakeObjectDefs());

            Assert.That(validation.ErrorCount, Is.EqualTo(0));
        }

        // =============================================================================
        // MissingObjectDefProducesDiagnostic
        // =============================================================================
        /// <summary>
        /// <para>
        /// Un productKey valido ma privo di ObjectDef produce errore diagnostico.
        /// </para>
        /// </summary>
        [Test]
        public void MissingObjectDefProducesDiagnostic()
        {
            Dictionary<string, ObjectDef> objectDefs = MakeObjectDefs();
            objectDefs.Remove("berry");

            EnvironmentConfigValidationResult validation =
                EnvironmentBiologicalProductCatalogValidator.Validate(
                    MakePlantCatalogConfig(),
                    MakeProductCatalogConfig(),
                    objectDefs);

            Assert.That(ContainsIssue(validation, "ENV_BIO_PRODUCT_OBJECT_DEF_MISSING"), Is.True);
        }

        // =============================================================================
        // FoodProductWithoutNutritionProducesDiagnostic
        // =============================================================================
        /// <summary>
        /// <para>
        /// Un prodotto food richiede ObjectDef alimentare e NutritionValue positivo.
        /// </para>
        /// </summary>
        [Test]
        public void FoodProductWithoutNutritionProducesDiagnostic()
        {
            Dictionary<string, ObjectDef> objectDefs = MakeObjectDefs();
            objectDefs["berry"] = MakeObjectDef(
                "berry",
                foodItem: true,
                nutritionValue: 0f);

            EnvironmentConfigValidationResult validation =
                EnvironmentBiologicalProductCatalogValidator.Validate(
                    MakePlantCatalogConfig(),
                    MakeProductCatalogConfig(),
                    objectDefs);

            Assert.That(ContainsIssue(validation, "ENV_BIO_PRODUCT_FOOD_OBJECT_INVALID"), Is.True);
        }

        // =============================================================================
        // NonFoodWithZeroNutritionIsValid
        // =============================================================================
        /// <summary>
        /// <para>
        /// Un prodotto non-food con NutritionValue zero non viene trattato come cibo.
        /// </para>
        /// </summary>
        [Test]
        public void NonFoodWithZeroNutritionIsValid()
        {
            EnvironmentConfigValidationResult validation =
                EnvironmentBiologicalProductCatalogValidator.Validate(
                    MakePlantCatalogConfig(),
                    MakeProductCatalogConfig(),
                    MakeObjectDefs());

            Assert.That(ContainsIssue(validation, "ENV_BIO_PRODUCT_NON_FOOD_OBJECT_MARKED_FOOD"), Is.False);
            Assert.That(validation.ErrorCount, Is.EqualTo(0));
        }

        // =============================================================================
        // ExistingBiosphereQueriesKeepReturningProductKeys
        // =============================================================================
        /// <summary>
        /// <para>
        /// Le query Biosfera gia' presenti continuano a restituire productKey, senza
        /// dipendere da inventario o job.
        /// </para>
        /// </summary>
        [Test]
        public void ExistingBiosphereQueriesKeepReturningProductKeys()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            Assert.That(
                plantCatalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak),
                Is.True);

            var state = new EnvironmentState();
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(1000),
                oak,
                new EnvironmentCellCoord(5, 5),
                300,
                0.9f,
                new EnvironmentAreaId(10)));

            EnvironmentFullSnapshot snapshot =
                EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(state.CreateSnapshot());
            IReadOnlyList<EnvironmentConsumerResourceCandidate> wood =
                EnvironmentConsumerQueryResolver.QueryHarvestableResourcesForProduct(
                    snapshot,
                    plantCatalog,
                    new EnvironmentCellCoord(5, 5),
                    1,
                    "wood_log");

            Assert.That(wood.Count, Is.EqualTo(1));
            Assert.That(wood[0].ResourceOutputKey, Is.EqualTo("wood_log"));
            Assert.That(wood[0].DestroysPlantOnHarvest, Is.True);
            Assert.That(wood[0].RequiresToolKey, Is.EqualTo("axe"));
        }

        private static EnvironmentPlantCatalogConfig MakePlantCatalogConfig()
        {
            return new EnvironmentPlantCatalogConfig
            {
                species = new[]
                {
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
                                stageKey = "adult",
                                requiredAgeDays = 300,
                                maturity01 = 1f,
                                isHarvestable = true
                            }
                        },
                        products = new[]
                        {
                            new EnvironmentPlantProductConfig
                            {
                                productKey = "wood_log",
                                isFood = false,
                                destroysPlantOnHarvest = true,
                                requiresToolKey = "axe",
                                minGrowthStageKey = "adult",
                                baseMaxAmountUnits = 8,
                                regrowDays = 0
                            },
                            new EnvironmentPlantProductConfig
                            {
                                productKey = "acorn",
                                isFood = true,
                                destroysPlantOnHarvest = false,
                                minGrowthStageKey = "adult",
                                baseMaxAmountUnits = 4,
                                regrowDays = 365
                            }
                        }
                    },
                    new EnvironmentPlantSpeciesConfig
                    {
                        speciesKey = "berry_bush",
                        category = "Shrub",
                        growthStages = new[]
                        {
                            new EnvironmentPlantGrowthStageConfig
                            {
                                stageKey = "fruiting",
                                requiredAgeDays = 45,
                                maturity01 = 1f,
                                isHarvestable = true
                            }
                        },
                        products = new[]
                        {
                            new EnvironmentPlantProductConfig
                            {
                                productKey = "berry",
                                isFood = true,
                                minGrowthStageKey = "fruiting",
                                baseMaxAmountUnits = 6,
                                regrowDays = 20
                            }
                        }
                    }
                }
            };
        }

        private static EnvironmentBiologicalProductCatalogConfig MakeProductCatalogConfig()
        {
            return new EnvironmentBiologicalProductCatalogConfig
            {
                products = new[]
                {
                    new EnvironmentBiologicalProductConfig
                    {
                        productKey = "wood_log",
                        objectDefId = "wood_log",
                        categories = new[] { "Material" },
                        isFood = false,
                        isTransportable = true,
                        recommendedToolKey = "axe",
                        defaultHarvestEffort = 0.65f,
                        defaultCarryUnits = 1
                    },
                    new EnvironmentBiologicalProductConfig
                    {
                        productKey = "acorn",
                        objectDefId = "acorn",
                        categories = new[] { "Food", "Seed" },
                        isFood = true,
                        isTransportable = true,
                        defaultHarvestEffort = 0.20f,
                        defaultCarryUnits = 1
                    },
                    new EnvironmentBiologicalProductConfig
                    {
                        productKey = "berry",
                        objectDefId = "berry",
                        categories = new[] { "Food" },
                        isFood = true,
                        isTransportable = true,
                        defaultHarvestEffort = 0.15f,
                        defaultCarryUnits = 1
                    }
                }
            };
        }

        private static Dictionary<string, ObjectDef> MakeObjectDefs()
        {
            return new Dictionary<string, ObjectDef>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["wood_log"] = MakeObjectDef("wood_log", foodItem: false, nutritionValue: 0f),
                ["acorn"] = MakeObjectDef("acorn", foodItem: true, nutritionValue: 0.18f),
                ["berry"] = MakeObjectDef("berry", foodItem: true, nutritionValue: 0.32f)
            };
        }

        private static ObjectDef MakeObjectDef(
            string id,
            bool foodItem,
            float nutritionValue)
        {
            var properties = new List<ObjectPropertyKV>
            {
                new ObjectPropertyKV
                {
                    Key = "BiologicalProduct",
                    Value = 1f
                },
                new ObjectPropertyKV
                {
                    Key = "NutritionValue",
                    Value = nutritionValue
                }
            };

            if (foodItem)
            {
                properties.Add(new ObjectPropertyKV
                {
                    Key = "FoodItem",
                    Value = 1f
                });
            }

            return new ObjectDef
            {
                Id = id,
                DisplayName = id,
                Properties = properties
            };
        }

        private static bool ContainsIssue(
            EnvironmentConfigValidationResult validation,
            string code)
        {
            if (validation == null || validation.Issues == null)
                return false;

            for (int i = 0; i < validation.Issues.Count; i++)
            {
                if (validation.Issues[i].Code == code)
                    return true;
            }

            return false;
        }
    }
}
