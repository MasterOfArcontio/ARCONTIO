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
        /// prodotto possibile e maturo ma non raccoglibile fuori stagione.
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
            Assert.That(acorn.AvailableAmountUnits, Is.EqualTo(4));
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
        /// risorsa possibile ma fuori stagione non diventa candidata raccoglibile.
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

        // =============================================================================
        // PlantResourcesRegrowProgressivelyByElapsedDays
        // =============================================================================
        /// <summary>
        /// <para>
        /// La ricrescita usa giorni ambientali discreti e aumenta le quantita' a
        /// unita' intere, senza tornare al massimo in un singolo scatto.
        /// </para>
        /// </summary>
        [Test]
        public void PlantResourcesRegrowProgressivelyByElapsedDays()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            Assert.That(plantCatalog.TryGetSpecies("berry_bush", out EnvironmentPlantSpeciesDefinition berryBush), Is.True);

            var plantId = new EnvironmentPlantId(3000);
            EnvironmentState state = MakeStateWithSinglePlant(
                EnvironmentPlantInstance.CreateFromSpecies(
                        plantId,
                        berryBush,
                        new EnvironmentCellCoord(2, 2),
                        45,
                        1f,
                        new EnvironmentAreaId(20),
                        EnvironmentSeasonKind.Summer,
                        enforceSeason: true)
                    .WithResourceStates(new[]
                    {
                        new EnvironmentPlantResourceState(
                            "berry",
                            0,
                            6,
                            isFood: true,
                            destroysPlantOnHarvest: false,
                            requiresToolKey: "",
                            minGrowthStageKey: "fruiting",
                            regrowDays: 20,
                            isStageAvailable: true,
                            isSeasonallyAvailable: true,
                            regrowProgressDays: 0)
                    }));

            EnvironmentNaturalGrowthResult partial = EvolveByDays(
                state,
                plantCatalog,
                EnvironmentSeasonKind.Summer,
                4);
            Assert.That(partial.State.TryGetPlantInstance(plantId, out EnvironmentPlantInstance partialPlant), Is.True);
            Assert.That(EnvironmentPlantResourceStateResolver.TryFindResource(partialPlant.Resources, "berry", out var partialBerry), Is.True);
            Assert.That(partialBerry.AvailableAmountUnits, Is.EqualTo(1));
            Assert.That(partialBerry.RegrowProgressDays, Is.EqualTo(4));

            EnvironmentNaturalGrowthResult full = EvolveByDays(
                state,
                plantCatalog,
                EnvironmentSeasonKind.Summer,
                20);
            Assert.That(full.State.TryGetPlantInstance(plantId, out EnvironmentPlantInstance fullPlant), Is.True);
            Assert.That(EnvironmentPlantResourceStateResolver.TryFindResource(fullPlant.Resources, "berry", out var fullBerry), Is.True);
            Assert.That(fullBerry.AvailableAmountUnits, Is.EqualTo(6));
            Assert.That(fullBerry.RegrowProgressDays, Is.EqualTo(20));
        }

        // =============================================================================
        // OffSeasonResourcesMatureButRemainNonHarvestable
        // =============================================================================
        /// <summary>
        /// <para>
        /// La quantita' reale puo' esistere fuori stagione, ma le query harvestable
        /// la escludono finche' la stagione del prodotto non torna valida.
        /// </para>
        /// </summary>
        [Test]
        public void OffSeasonResourcesMatureButRemainNonHarvestable()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            Assert.That(plantCatalog.TryGetSpecies("berry_bush", out EnvironmentPlantSpeciesDefinition berryBush), Is.True);

            var plantId = new EnvironmentPlantId(3001);
            EnvironmentState springState = MakeStateWithSinglePlant(
                EnvironmentPlantInstance.CreateFromSpecies(
                    plantId,
                    berryBush,
                    new EnvironmentCellCoord(3, 3),
                    45,
                    1f,
                    new EnvironmentAreaId(21),
                    EnvironmentSeasonKind.Spring,
                    enforceSeason: true));

            IReadOnlyList<EnvironmentConsumerResourceCandidate> springCandidates =
                QueryProduct(springState, plantCatalog, new EnvironmentCellCoord(3, 3), "berry");
            Assert.That(springCandidates.Count, Is.EqualTo(0));
            Assert.That(springState.TryGetPlantInstance(plantId, out EnvironmentPlantInstance springPlant), Is.True);
            Assert.That(EnvironmentPlantResourceStateResolver.TryFindResource(springPlant.Resources, "berry", out var springBerry), Is.True);
            Assert.That(springBerry.AvailableAmountUnits, Is.EqualTo(6));
            Assert.That(springBerry.IsSeasonallyAvailable, Is.False);

            EnvironmentNaturalGrowthResult summer = EvolveByDays(
                springState,
                plantCatalog,
                EnvironmentSeasonKind.Summer,
                75);
            IReadOnlyList<EnvironmentConsumerResourceCandidate> summerCandidates =
                QueryProduct(summer.State, plantCatalog, new EnvironmentCellCoord(3, 3), "berry");
            Assert.That(summerCandidates.Count, Is.EqualTo(1));
            Assert.That(summerCandidates[0].EstimatedAmountUnits, Is.EqualTo(6));
        }

        // =============================================================================
        // NonRegrowingResourceDoesNotIncreasePassively
        // =============================================================================
        /// <summary>
        /// <para>
        /// Un prodotto con <c>regrowDays = 0</c> non viene rigenerato dal ciclo
        /// naturale. Questo protegge risorse distruttive come il legno.
        /// </para>
        /// </summary>
        [Test]
        public void NonRegrowingResourceDoesNotIncreasePassively()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            Assert.That(plantCatalog.TryGetSpecies("oak_tree", out EnvironmentPlantSpeciesDefinition oak), Is.True);

            var plantId = new EnvironmentPlantId(3002);
            EnvironmentState state = MakeStateWithSinglePlant(
                EnvironmentPlantInstance.CreateFromSpecies(
                        plantId,
                        oak,
                        new EnvironmentCellCoord(4, 4),
                        300,
                        1f,
                        new EnvironmentAreaId(22),
                        EnvironmentSeasonKind.Summer,
                        enforceSeason: true)
                    .WithResourceStates(new[]
                    {
                        new EnvironmentPlantResourceState(
                            "wood_log",
                            0,
                            8,
                            isFood: false,
                            destroysPlantOnHarvest: true,
                            requiresToolKey: "axe",
                            minGrowthStageKey: "adult",
                            regrowDays: 0,
                            isStageAvailable: true,
                            isSeasonallyAvailable: true)
                    }));

            EnvironmentNaturalGrowthResult result = EvolveByDays(
                state,
                plantCatalog,
                EnvironmentSeasonKind.Summer,
                30);
            Assert.That(result.State.TryGetPlantInstance(plantId, out EnvironmentPlantInstance plant), Is.True);
            Assert.That(EnvironmentPlantResourceStateResolver.TryFindResource(plant.Resources, "wood_log", out var wood), Is.True);
            Assert.That(wood.AvailableAmountUnits, Is.EqualTo(0));
        }

        // =============================================================================
        // ResourceChangeProducesPhysicalPlantDelta
        // =============================================================================
        /// <summary>
        /// <para>
        /// Quando cambia solo la risorsa della pianta, la Biosfera produce comunque
        /// un delta per riallineare la proiezione World consumata da ArcGraph.
        /// </para>
        /// </summary>
        [Test]
        public void ResourceChangeProducesPhysicalPlantDelta()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            Assert.That(plantCatalog.TryGetSpecies("berry_bush", out EnvironmentPlantSpeciesDefinition berryBush), Is.True);

            var plantId = new EnvironmentPlantId(3003);
            EnvironmentState state = MakeStateWithSinglePlant(
                EnvironmentPlantInstance.CreateFromSpecies(
                        plantId,
                        berryBush,
                        new EnvironmentCellCoord(5, 5),
                        45,
                        1f,
                        new EnvironmentAreaId(23),
                        EnvironmentSeasonKind.Summer,
                        enforceSeason: true)
                    .WithResourceStates(new[]
                    {
                        new EnvironmentPlantResourceState(
                            "berry",
                            0,
                            6,
                            isFood: true,
                            destroysPlantOnHarvest: false,
                            requiresToolKey: "",
                            minGrowthStageKey: "fruiting",
                            regrowDays: 20,
                            isStageAvailable: true,
                            isSeasonallyAvailable: true,
                            regrowProgressDays: 0)
                    }));

            EnvironmentNaturalGrowthResult result = EvolveByDays(
                state,
                plantCatalog,
                EnvironmentSeasonKind.Summer,
                4);

            Assert.That(result.PhysicalPlantDeltas.Count, Is.EqualTo(1));
            Assert.That(result.PhysicalPlantDeltas[0].Kind, Is.EqualTo(EnvironmentPhysicalPlantDeltaKind.ResourceChanged));
            Assert.That(result.PhysicalPlantDeltas[0].PlantId, Is.EqualTo(plantId));
        }

        // =============================================================================
        // KnownAreaResourceQueryFindsPotentialProductWithoutNavigationAnchor
        // =============================================================================
        /// <summary>
        /// <para>
        /// La query F su area nota risponde che l'area puo' fornire berry, ma non
        /// trasforma il centro area in un'ancora navigabile per l'NPC.
        /// </para>
        /// </summary>
        [Test]
        public void KnownAreaResourceQueryFindsPotentialProductWithoutNavigationAnchor()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            World world = MakeWorldWithBiologicalResourceArea(
                plantCatalog,
                "berry_bush",
                out EnvironmentAreaId areaId);

            EnvironmentKnownAreaResourceQueryResult result =
                world.QueryEnvironmentKnownAreaResourcePotential(
                    areaId,
                    "berry",
                    plantCatalog);

            Assert.That(result.IsValidAreaReference, Is.True);
            Assert.That(result.CanPotentiallyProvide, Is.True);
            Assert.That(result.ProductKey, Is.EqualTo("berry"));
            Assert.That(result.IsFood, Is.True);
            Assert.That(result.BaseMaxAmountUnits, Is.EqualTo(6));
            Assert.That(result.RegrowDays, Is.EqualTo(20));
            Assert.That(result.LivePlantCount, Is.EqualTo(1));
            Assert.That(result.CanUseAsNavigationAnchor, Is.False);
            Assert.That(result.LandmarkNodeId, Is.EqualTo(0));
        }

        // =============================================================================
        // KnownAreaResourceQueryDistinguishesMissingProductFromInvalidArea
        // =============================================================================
        /// <summary>
        /// <para>
        /// Un'area biologica valida che non produce il prodotto cercato resta una
        /// risposta negativa valida, distinta da un riferimento area inesistente.
        /// </para>
        /// </summary>
        [Test]
        public void KnownAreaResourceQueryDistinguishesMissingProductFromInvalidArea()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            World world = MakeWorldWithBiologicalResourceArea(
                plantCatalog,
                "berry_bush",
                out EnvironmentAreaId areaId);

            EnvironmentKnownAreaResourceQueryResult absent =
                world.QueryEnvironmentKnownAreaResourcePotential(
                    areaId,
                    "acorn",
                    plantCatalog);
            EnvironmentKnownAreaResourceQueryResult invalid =
                world.QueryEnvironmentKnownAreaResourcePotential(
                    new EnvironmentAreaId(9999),
                    "berry",
                    plantCatalog);

            Assert.That(absent.IsValidAreaReference, Is.True);
            Assert.That(absent.CanPotentiallyProvide, Is.False);
            Assert.That(absent.ProductKey, Is.EqualTo("acorn"));

            Assert.That(invalid.IsValidAreaReference, Is.False);
            Assert.That(invalid.CanPotentiallyProvide, Is.False);
        }

        // =============================================================================
        // KnownLandmarkResourceQueryUsesLandmarkAsNavigationAnchor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Quando la query parte da un landmark biologico, il risultato resta
        /// ancorato al nodeId del landmark e non al centro tecnico dell'area.
        /// </para>
        /// </summary>
        [Test]
        public void KnownLandmarkResourceQueryUsesLandmarkAsNavigationAnchor()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            World world = MakeWorldWithBiologicalResourceArea(
                plantCatalog,
                "berry_bush",
                out EnvironmentAreaId areaId);
            world.RebuildLandmarksBootstrap();

            Assert.That(
                world.EnvironmentState.TryGetBiologicalLandmarkNodeIds(areaId, out IReadOnlyList<int> landmarkIds),
                Is.True);
            Assert.That(landmarkIds.Count, Is.GreaterThan(0));

            int landmarkNodeId = landmarkIds[0];
            EnvironmentKnownAreaResourceQueryResult result =
                world.QueryEnvironmentKnownBiologicalLandmarkResourcePotential(
                    landmarkNodeId,
                    "berry",
                    plantCatalog);

            Assert.That(result.IsValidAreaReference, Is.True);
            Assert.That(result.CanPotentiallyProvide, Is.True);
            Assert.That(result.CanUseAsNavigationAnchor, Is.True);
            Assert.That(result.LandmarkNodeId, Is.EqualTo(landmarkNodeId));
            Assert.That(result.AreaId, Is.EqualTo(areaId));
            Assert.That(result.AreaCenterCell.X, Is.EqualTo(4));
            Assert.That(result.AreaCenterCell.Y, Is.EqualTo(4));
        }

        // =============================================================================
        // KnownLandmarkResourceQueryBuildsSinglePotentialBeliefHint
        // =============================================================================
        /// <summary>
        /// <para>
        /// Il helper cognitivo produce un solo hint potenziale per prodotto e non
        /// contiene quantita' osservate reali.
        /// </para>
        /// </summary>
        [Test]
        public void KnownLandmarkResourceQueryBuildsSinglePotentialBeliefHint()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            World world = MakeWorldWithBiologicalResourceArea(
                plantCatalog,
                "berry_bush",
                out EnvironmentAreaId areaId);
            world.RebuildLandmarksBootstrap();

            Assert.That(
                world.EnvironmentState.TryGetBiologicalLandmarkNodeIds(areaId, out IReadOnlyList<int> landmarkIds),
                Is.True);

            int landmarkNodeId = landmarkIds[0];
            bool built = world.TryBuildEnvironmentPotentialProductBeliefHintForBiologicalLandmarkProduct(
                landmarkNodeId,
                "berry",
                plantCatalog,
                observedDay: 12,
                out EnvironmentBiologicalResourceBeliefHint hint);
            bool absentBuilt = world.TryBuildEnvironmentPotentialProductBeliefHintForBiologicalLandmarkProduct(
                landmarkNodeId,
                "acorn",
                plantCatalog,
                observedDay: 12,
                out _);

            Assert.That(built, Is.True);
            Assert.That(hint.Kind, Is.EqualTo(EnvironmentBiologicalResourceBeliefKind.Potential));
            Assert.That(hint.LandmarkNodeId, Is.EqualTo(landmarkNodeId));
            Assert.That(hint.AreaId, Is.EqualTo(areaId));
            Assert.That(hint.ProductKey, Is.EqualTo("berry"));
            Assert.That(hint.EstimatedAmount, Is.EqualTo(0));
            Assert.That(hint.ObservedDay, Is.EqualTo(12));
            Assert.That(absentBuilt, Is.False);
        }

        // =============================================================================
        // UnknownBiologicalLandmarkResourceQueryIsInvalid
        // =============================================================================
        /// <summary>
        /// <para>
        /// Un landmark non registrato o non biologico non produce ancora navigabile
        /// ne' hint di belief potenziale.
        /// </para>
        /// </summary>
        [Test]
        public void UnknownBiologicalLandmarkResourceQueryIsInvalid()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            World world = MakeWorldWithBiologicalResourceArea(
                plantCatalog,
                "berry_bush",
                out _);

            EnvironmentKnownAreaResourceQueryResult result =
                world.QueryEnvironmentKnownBiologicalLandmarkResourcePotential(
                    9999,
                    "berry",
                    plantCatalog);
            bool built = world.TryBuildEnvironmentPotentialProductBeliefHintForBiologicalLandmarkProduct(
                9999,
                "berry",
                plantCatalog,
                observedDay: 12,
                out _);

            Assert.That(result.IsValidAreaReference, Is.False);
            Assert.That(result.CanPotentiallyProvide, Is.False);
            Assert.That(result.CanUseAsNavigationAnchor, Is.False);
            Assert.That(built, Is.False);
        }

        // =============================================================================
        // ReachablePlantResourceQueryReturnsAdjacentInteractionCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// La query operativa G non manda l'NPC sulla cella della pianta fisica:
        /// restituisce una cella cardinale adiacente raggiungibile tramite
        /// pathfinding.
        /// </para>
        /// </summary>
        [Test]
        public void ReachablePlantResourceQueryReturnsAdjacentInteractionCell()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            World world = MakeWorldWithBerryResourcePlants(
                plantCatalog,
                new[] { new EnvironmentPlantId(9200) },
                new[] { new EnvironmentCellCoord(5, 5) },
                out EnvironmentAreaId areaId,
                out int landmarkNodeId);
            int npcId = CreateNpcOnLandmark(world, landmarkNodeId);

            EnvironmentReachablePlantResourceQueryResult result =
                world.QueryEnvironmentReachablePlantResourceFromBiologicalLandmark(
                    npcId,
                    landmarkNodeId,
                    10,
                    "berry",
                    plantCatalog);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.AreaId, Is.EqualTo(areaId));
            Assert.That(result.PlantId, Is.EqualTo(new EnvironmentPlantId(9200)));
            Assert.That(result.PlantCell.X, Is.EqualTo(5));
            Assert.That(result.PlantCell.Y, Is.EqualTo(5));
            Assert.That(
                Mathf.Abs(result.InteractionCell.X - result.PlantCell.X)
                + Mathf.Abs(result.InteractionCell.Y - result.PlantCell.Y),
                Is.EqualTo(1));
            Assert.That(result.EstimatedAmountUnits, Is.EqualTo(6));
            Assert.That(result.IsFood, Is.True);
        }

        // =============================================================================
        // ReachablePlantResourceQuerySkipsCloserBlockedPlant
        // =============================================================================
        /// <summary>
        /// <para>
        /// Se la pianta piu' vicina non ha nessuna cella di interazione
        /// raggiungibile, la query sceglie una pianta piu' lontana ma realmente
        /// raggiungibile.
        /// </para>
        /// </summary>
        [Test]
        public void ReachablePlantResourceQuerySkipsCloserBlockedPlant()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            World world = MakeWorldWithBerryResourcePlants(
                plantCatalog,
                new[]
                {
                    new EnvironmentPlantId(9201),
                    new EnvironmentPlantId(9202)
                },
                new[]
                {
                    new EnvironmentCellCoord(4, 4),
                    new EnvironmentCellCoord(8, 4)
                },
                out _,
                out int landmarkNodeId);
            AddBlockingWallRingAroundCell(world, 4, 4);
            int npcId = CreateNpcOnLandmark(world, landmarkNodeId);

            EnvironmentReachablePlantResourceQueryResult result =
                world.QueryEnvironmentReachablePlantResourceFromBiologicalLandmark(
                    npcId,
                    landmarkNodeId,
                    12,
                    "berry",
                    plantCatalog);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.PlantId, Is.EqualTo(new EnvironmentPlantId(9202)));
            Assert.That(result.PlantCell.X, Is.EqualTo(8));
            Assert.That(result.PlantCell.Y, Is.EqualTo(4));
        }

        // =============================================================================
        // ReachablePlantResourceQueryFailsWhenNoInteractionCellIsReachable
        // =============================================================================
        /// <summary>
        /// <para>
        /// Una risorsa harvestable ma completamente isolata non diventa target
        /// operativo: il job futuro dovra' ricevere un fallimento esplicito.
        /// </para>
        /// </summary>
        [Test]
        public void ReachablePlantResourceQueryFailsWhenNoInteractionCellIsReachable()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            World world = MakeWorldWithBerryResourcePlants(
                plantCatalog,
                new[] { new EnvironmentPlantId(9203) },
                new[] { new EnvironmentCellCoord(4, 4) },
                out _,
                out int landmarkNodeId);
            AddBlockingWallRingAroundCell(world, 4, 4);
            int npcId = CreateNpcOnLandmark(world, landmarkNodeId);

            EnvironmentReachablePlantResourceQueryResult result =
                world.QueryEnvironmentReachablePlantResourceFromBiologicalLandmark(
                    npcId,
                    landmarkNodeId,
                    10,
                    "berry",
                    plantCatalog);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Reason, Is.EqualTo("NoReachablePlant"));
            Assert.That(result.PlantId.IsValid, Is.False);
        }

        // =============================================================================
        // ReachablePlantResourceQueryRejectsInvalidLandmarkAndMissingProduct
        // =============================================================================
        /// <summary>
        /// <para>
        /// La query G distingue un landmark non biologico/inesistente da un
        /// prodotto che non esiste tra le risorse harvestable locali.
        /// </para>
        /// </summary>
        [Test]
        public void ReachablePlantResourceQueryRejectsInvalidLandmarkAndMissingProduct()
        {
            EnvironmentPlantCatalog plantCatalog = MakePlantCatalogConfig().ToCatalog();
            World world = MakeWorldWithBerryResourcePlants(
                plantCatalog,
                new[] { new EnvironmentPlantId(9204) },
                new[] { new EnvironmentCellCoord(5, 5) },
                out _,
                out int landmarkNodeId);
            int npcId = CreateNpcOnLandmark(world, landmarkNodeId);

            EnvironmentReachablePlantResourceQueryResult invalidLandmark =
                world.QueryEnvironmentReachablePlantResourceFromBiologicalLandmark(
                    npcId,
                    9999,
                    10,
                    "berry",
                    plantCatalog);
            EnvironmentReachablePlantResourceQueryResult missingProduct =
                world.QueryEnvironmentReachablePlantResourceFromBiologicalLandmark(
                    npcId,
                    landmarkNodeId,
                    10,
                    "acorn",
                    plantCatalog);

            Assert.That(invalidLandmark.IsValid, Is.False);
            Assert.That(invalidLandmark.Reason, Is.EqualTo("BiologicalLandmarkMissing"));
            Assert.That(missingProduct.IsValid, Is.False);
            Assert.That(missingProduct.Reason, Is.EqualTo("NoHarvestableCandidates"));
        }

        private static EnvironmentState MakeStateWithSinglePlant(
            EnvironmentPlantInstance plant)
        {
            var state = new EnvironmentState();
            state.SetCalendar(EnvironmentCalendarResolver.Resolve(0, new EnvironmentCalendarConfig()));
            state.SetClimate(new EnvironmentGlobalClimateState(
                0.55f,
                0.55f,
                0f,
                EnvironmentWeatherState.Clear,
                EnvironmentSeasonKind.Spring));
            state.SetPlantInstance(plant);
            return state;
        }

        private static EnvironmentNaturalGrowthResult EvolveByDays(
            EnvironmentState state,
            EnvironmentPlantCatalog plantCatalog,
            EnvironmentSeasonKind targetSeason,
            int days)
        {
            var calendarConfig = new EnvironmentCalendarConfig();
            long previousTicks = 0;
            long currentTicks = ResolveTicksForDays(calendarConfig, days);
            EnvironmentTemporalTransition transition = EnvironmentTemporalTransitionResolver.Resolve(
                previousTicks,
                currentTicks,
                calendarConfig);
            var climate = new EnvironmentGlobalClimateState(
                0.55f,
                0.55f,
                0f,
                EnvironmentWeatherState.Clear,
                targetSeason);
            EnvironmentSeasonProfile profile = EnvironmentCalendarResolver.ResolveSeasonProfile(
                calendarConfig,
                targetSeason);

            return EnvironmentNaturalGrowthResolver.Evolve(
                state.CreateSnapshot(),
                plantCatalog,
                transition,
                climate,
                profile,
                new EnvironmentNaturalGrowthConfig
                {
                    allowNewPlantInstances = false,
                    maxNewPlantsPerDay = 0,
                    maxNewPlantsPerAreaPerDay = 0,
                    healthRecoveryStep01 = 0f,
                    healthStressStep01 = 0f,
                    plantAridityHealthStressScale01 = 0f
                });
        }

        private static IReadOnlyList<EnvironmentConsumerResourceCandidate> QueryProduct(
            EnvironmentState state,
            EnvironmentPlantCatalog plantCatalog,
            EnvironmentCellCoord cell,
            string productKey)
        {
            EnvironmentFullSnapshot snapshot =
                EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(state.CreateSnapshot());
            return EnvironmentConsumerQueryResolver.QueryHarvestableResourcesForProduct(
                snapshot,
                plantCatalog,
                cell,
                1,
                productKey);
        }

        private static long ResolveTicksForDays(
            EnvironmentCalendarConfig calendarConfig,
            int days)
        {
            int safeDays = days < 0 ? 0 : days;
            int ticksPerDay =
                calendarConfig.ResolveHoursPerDay()
                * calendarConfig.ResolveCalendarTicksPerSimulatedHour();
            return (long)safeDays * ticksPerDay;
        }

        private static World MakeWorldWithBiologicalResourceArea(
            EnvironmentPlantCatalog plantCatalog,
            string speciesKey,
            out EnvironmentAreaId areaId)
        {
            World world = MakeWorldWithNaturalSurface();
            areaId = new EnvironmentAreaId(91);

            Assert.That(plantCatalog.TryGetSpecies(speciesKey, out EnvironmentPlantSpeciesDefinition species), Is.True);
            var state = new EnvironmentState();
            state.SetCalendar(EnvironmentCalendarResolver.Resolve(0, new EnvironmentCalendarConfig()));
            state.SetClimate(new EnvironmentGlobalClimateState(
                0.55f,
                0.55f,
                0f,
                EnvironmentWeatherState.Clear,
                EnvironmentSeasonKind.Summer));
            state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Vegetation,
                new EnvironmentAreaBounds(1, 1, 6, 6),
                1,
                true,
                "qa_resource_area"));
            state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                new EnvironmentPlantId(9100),
                species,
                new EnvironmentCellCoord(4, 4),
                300,
                0.95f,
                areaId,
                EnvironmentSeasonKind.Summer,
                enforceSeason: true));
            state.ReplaceBiologicalPlacementsForSaveLoad(
                new EnvironmentVegetationCellPlacement[0],
                new[]
                {
                    new EnvironmentPhysicalPlantPlacement(
                        new EnvironmentPlantId(9100),
                        areaId,
                        new EnvironmentCellCoord(4, 4),
                        speciesKey)
                });
            world.SetEnvironmentState(state);
            return world;
        }

        // =============================================================================
        // MakeWorldWithBerryResourcePlants
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un mondo QA con una singola area biologica, una lista di
        /// piante berry mature, proiezioni fisiche attive e landmark biologici gia'
        /// ricostruiti.
        /// </para>
        /// </summary>
        private static World MakeWorldWithBerryResourcePlants(
            EnvironmentPlantCatalog plantCatalog,
            EnvironmentPlantId[] plantIds,
            EnvironmentCellCoord[] plantCells,
            out EnvironmentAreaId areaId,
            out int landmarkNodeId)
        {
            Assert.That(plantIds, Is.Not.Null);
            Assert.That(plantCells, Is.Not.Null);
            Assert.That(plantIds.Length, Is.EqualTo(plantCells.Length));
            Assert.That(plantCatalog.TryGetSpecies("berry_bush", out EnvironmentPlantSpeciesDefinition berryBush), Is.True);

            World world = MakeWorldWithNaturalSurface(12, 12);
            world.Config.Sim.landmarks.localSearch.maxSearchRadius = 16;
            world.Config.Sim.landmarks.localSearch.maxExpandedNodes = 256;
            world.Config.Sim.landmarks.localSearch.maxIterations = 512;

            areaId = new EnvironmentAreaId(92);
            var state = new EnvironmentState();
            state.SetCalendar(EnvironmentCalendarResolver.Resolve(0, new EnvironmentCalendarConfig()));
            state.SetClimate(new EnvironmentGlobalClimateState(
                0.55f,
                0.55f,
                0f,
                EnvironmentWeatherState.Clear,
                EnvironmentSeasonKind.Summer));
            state.SetAreaDefinition(new EnvironmentAreaDefinition(
                areaId,
                EnvironmentAreaKind.Vegetation,
                new EnvironmentAreaBounds(1, 1, 10, 10),
                1,
                true,
                "qa_reachable_resource_area"));

            var placements = new List<EnvironmentPhysicalPlantPlacement>(plantIds.Length);
            for (int i = 0; i < plantIds.Length; i++)
            {
                EnvironmentCellCoord cell = plantCells[i];
                state.SetPlantInstance(EnvironmentPlantInstance.CreateFromSpecies(
                    plantIds[i],
                    berryBush,
                    cell,
                    45,
                    0.95f,
                    areaId,
                    EnvironmentSeasonKind.Summer,
                    enforceSeason: true));
                placements.Add(new EnvironmentPhysicalPlantPlacement(
                    plantIds[i],
                    areaId,
                    cell,
                    "berry_bush"));
            }

            state.ReplaceBiologicalPlacementsForSaveLoad(
                new EnvironmentVegetationCellPlacement[0],
                placements.ToArray());
            world.SetEnvironmentState(state);
            Assert.That(world.ApplyEnvironmentPhysicalPlantProjections(), Is.EqualTo(plantIds.Length));
            world.RebuildLandmarksBootstrap();

            Assert.That(
                world.EnvironmentState.TryGetBiologicalLandmarkNodeIds(areaId, out IReadOnlyList<int> landmarkIds),
                Is.True);
            Assert.That(landmarkIds.Count, Is.GreaterThan(0));
            landmarkNodeId = landmarkIds[0];
            return world;
        }

        // =============================================================================
        // CreateNpcOnLandmark
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un NPC esattamente sulla cella del landmark biologico usato dalla
        /// query, simulando lo stato operativo in cui il job ha gia' raggiunto
        /// l'ancora locale e deve cercare una pianta concreta.
        /// </para>
        /// </summary>
        private static int CreateNpcOnLandmark(
            World world,
            int landmarkNodeId)
        {
            Assert.That(world.LandmarkRegistry.TryGetActiveNodeById(landmarkNodeId, out LandmarkRegistry.LandmarkNode node), Is.True);
            Assert.That(node, Is.Not.Null);
            return world.CreateNpc(
                NpcDnaProfile.CreateDefault("reachable_resource_qa"),
                NpcNeeds.Make(0.4f, 0.2f),
                new Arcontio.Core.Social(),
                node.CellX,
                node.CellY);
        }

        // =============================================================================
        // AddBlockingWallRingAroundCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Circonda una cella con quattro oggetti bloccanti cardinali per verificare
        /// che la query non confonda una risorsa biologica esistente con una risorsa
        /// realmente raggiungibile.
        /// </para>
        /// </summary>
        private static void AddBlockingWallRingAroundCell(
            World world,
            int centerX,
            int centerY)
        {
            EnsureBlockingWallDef(world);
            Assert.That(world.CreateObject("qa_blocking_wall", centerX, centerY + 1), Is.GreaterThan(0));
            Assert.That(world.CreateObject("qa_blocking_wall", centerX + 1, centerY), Is.GreaterThan(0));
            Assert.That(world.CreateObject("qa_blocking_wall", centerX, centerY - 1), Is.GreaterThan(0));
            Assert.That(world.CreateObject("qa_blocking_wall", centerX - 1, centerY), Is.GreaterThan(0));
        }

        // =============================================================================
        // EnsureBlockingWallDef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra nel catalogo oggetti QA un ostacolo minimale che aggiorna la
        /// cache movimento del World tramite il percorso ordinario
        /// <see cref="World.CreateObject"/>.
        /// </para>
        /// </summary>
        private static void EnsureBlockingWallDef(
            World world)
        {
            if (world.ObjectDefs.ContainsKey("qa_blocking_wall"))
                return;

            world.ObjectDefs["qa_blocking_wall"] = new ObjectDef
            {
                Id = "qa_blocking_wall",
                DisplayName = "QA Blocking Wall",
                IsOccluder = true,
                BlocksMovement = true,
                BlocksVision = true,
                VisionCost = 1f
            };
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
                                availableSeasons = new[] { "Autumn" },
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
                                availableSeasons = new[] { "Summer", "Autumn" },
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

        private static World MakeWorldWithNaturalSurface(
            int width = 8,
            int height = 8)
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.InitMap(width, height);
            world.SurfaceDefs["grass"] = new CellSurfaceDef
            {
                Id = "grass",
                DisplayName = "grass",
                MacroSurface = "Natural",
                CanHostNaturalVegetation = true,
                CanHostPhysicalPlant = true
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
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
