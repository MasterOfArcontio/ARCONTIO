using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Environment;
using Arcontio.View.ArcGraph;
using NUnit.Framework;
using UnityEngine;

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

        // =============================================================================
        // PlantResourceStateTracksRealAmountsAndSeason
        // =============================================================================
        /// <summary>
        /// <para>
        /// Una pianta concreta possiede stato risorse per-prodotto: il legno della
        /// quercia adulta e' disponibile in primavera, mentre la ghianda resta
        /// prodotto possibile ma non disponibile fuori stagione.
        /// </para>
        /// </summary>
        [Test]
        public void PlantResourceStateTracksRealAmountsAndSeason()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            Assert.That(plantCatalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak), Is.True);

            EnvironmentPlantInstance plant = EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(2000),
                oak,
                new EnvironmentCellCoord(4, 4),
                300,
                0.9f,
                new EnvironmentAreaId(10),
                EnvironmentSeasonKind.Spring,
                enforceSeason: true);

            Assert.That(EnvironmentPlantResourceStateResolver.TryFindResource(plant.Resources, "wood_log", out var wood), Is.True);
            Assert.That(wood.AvailableAmountUnits, Is.EqualTo(8));
            Assert.That(wood.MaxAmountUnits, Is.EqualTo(8));
            Assert.That(wood.IsAvailable, Is.True);

            Assert.That(EnvironmentPlantResourceStateResolver.TryFindResource(plant.Resources, "acorn", out var acorn), Is.True);
            Assert.That(acorn.AvailableAmountUnits, Is.EqualTo(0));
            Assert.That(acorn.MaxAmountUnits, Is.EqualTo(4));
            Assert.That(acorn.IsSeasonallyAvailable, Is.False);
            Assert.That(acorn.IsAvailable, Is.False);
        }

        // =============================================================================
        // HarvestQueriesUseRealPlantResourceAmounts
        // =============================================================================
        /// <summary>
        /// <para>
        /// Le query harvestable usano la quantita' reale nello stato pianta: una
        /// risorsa possibile ma a zero non diventa candidata raccoglibile.
        /// </para>
        /// </summary>
        [Test]
        public void HarvestQueriesUseRealPlantResourceAmounts()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            Assert.That(plantCatalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak), Is.True);

            var state = new EnvironmentState();
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(2001),
                oak,
                new EnvironmentCellCoord(5, 5),
                300,
                0.9f,
                new EnvironmentAreaId(10),
                EnvironmentSeasonKind.Spring,
                enforceSeason: true));

            EnvironmentFullSnapshot snapshot =
                EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(state.CreateSnapshot());
            IReadOnlyList<EnvironmentConsumerResourceCandidate> wood =
                EnvironmentConsumerQueryResolver.QueryHarvestableResourcesForProduct(
                    snapshot,
                    plantCatalog,
                    new EnvironmentCellCoord(5, 5),
                    1,
                    "wood_log");
            IReadOnlyList<EnvironmentConsumerResourceCandidate> acorns =
                EnvironmentConsumerQueryResolver.QueryHarvestableResourcesForProduct(
                    snapshot,
                    plantCatalog,
                    new EnvironmentCellCoord(5, 5),
                    1,
                    "acorn");

            Assert.That(wood.Count, Is.EqualTo(1));
            Assert.That(wood[0].EstimatedAmountUnits, Is.EqualTo(8));
            Assert.That(acorns.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // PlantInspectorShowsProductsFromArcGraphContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// L'inspector pianta riceve i prodotti tramite proiezione World e ViewModel
        /// ArcGraph, senza accedere direttamente allo stato interno della Biosfera.
        /// </para>
        /// </summary>
        [Test]
        public void PlantInspectorShowsProductsFromArcGraphContract()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            Assert.That(plantCatalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak), Is.True);

            World world = MakeWorldWithNaturalSurface();
            var areaId = new EnvironmentAreaId(10);
            var plantId = new EnvironmentPlantId(2002);
            var cell = new EnvironmentCellCoord(3, 3);
            var state = new EnvironmentState();
            state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Vegetation,
                new EnvironmentAreaBounds(0, 0, 8, 8),
                1,
                true,
                "qa_area"));
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                plantId,
                oak,
                cell,
                300,
                0.9f,
                areaId,
                EnvironmentSeasonKind.Spring,
                enforceSeason: true));
            state.ReplaceBiologicalPlacementsForSaveLoad(
                new EnvironmentVegetationCellPlacement[0],
                new[]
                {
                    new EnvironmentPhysicalPlantPlacement(
                        plantId,
                        areaId,
                        cell,
                        "oak_tree")
                });
            world.SetEnvironmentState(state);
            Assert.That(world.ApplyEnvironmentPhysicalPlantProjections(), Is.EqualTo(1));

            var providerObject = new GameObject("ArcGraphPlantInspectorQaProvider");
            try
            {
                var contextProvider = providerObject.AddComponent<TestRuntimeContextProvider>();
                contextProvider.World = world;
                var provider = new ArcUiInspectorRuntimeSnapshotProvider();
                provider.SetRuntimeContextProvider(contextProvider);
                var target = new ArcUiSelectionTarget(
                    ArcUiSelectionTargetKind.Plant,
                    plantId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    new ArcGraphCellCoord(cell.X, cell.Y, cell.Z),
                    "Pianta QA",
                    "qa");

                Assert.That(provider.TryBuildPlantViewModel(target, out ArcUiInspectorViewModel viewModel), Is.True);
                Assert.That(HasTab(viewModel, "products"), Is.True);
                Assert.That(ContainsRowValue(viewModel, "wood_log"), Is.True);
                Assert.That(ContainsRowValue(viewModel, "8/8"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(providerObject);
            }
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

        private static World MakeWorldWithNaturalSurface()
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.InitMap(8, 8);
            world.SurfaceDefs["grass"] = new CellSurfaceDef
            {
                Id = "grass",
                DisplayName = "grass",
                MacroSurface = "Natural",
                CanHostNaturalVegetation = true,
                CanHostPhysicalPlant = true
            };

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Assert.That(world.CellSurfaces.SetSurface(x, y, CellSurfaceMacro.Natural, "grass"), Is.True);
                }
            }

            return world;
        }

        private static bool HasTab(ArcUiInspectorViewModel viewModel, string tabKey)
        {
            for (int i = 0; i < viewModel.Tabs.Length; i++)
            {
                if (viewModel.Tabs[i].TabKey == tabKey)
                    return true;
            }

            return false;
        }

        private static bool ContainsRowValue(ArcUiInspectorViewModel viewModel, string value)
        {
            for (int tabIndex = 0; tabIndex < viewModel.Tabs.Length; tabIndex++)
            {
                if (ContainsRowValue(viewModel.Tabs[tabIndex].Rows, value))
                    return true;
            }

            return false;
        }

        private static bool ContainsRowValue(ArcUiInspectorRow[] rows, string value)
        {
            if (rows == null)
                return false;

            for (int i = 0; i < rows.Length; i++)
            {
                ArcUiInspectorRow row = rows[i];
                if (row.Label == value || row.Value == value || row.SecondaryValue == value)
                    return true;

                if (ContainsRowValue(row.Details, value))
                    return true;
            }

            return false;
        }

        private sealed class TestRuntimeContextProvider : ArcGraphRuntimeContextProvider
        {
            public World World;

            public override ArcGraphRuntimeContext BuildTerrainRuntimeContext()
            {
                return new ArcGraphRuntimeContext(World);
            }
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
