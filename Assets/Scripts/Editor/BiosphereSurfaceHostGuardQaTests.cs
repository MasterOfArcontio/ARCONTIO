using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Environment;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // BiosphereSurfaceHostGuardQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il guard World che impedisce alla Biosfera di proiettare
    /// piante fisiche o vegetazione diffusa su superfici non naturali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Biosfera owner del dato, World owner dello spazio</b></para>
    /// <para>
    /// La Biosfera puo' produrre delta biologici data-only, ma l'applicazione fisica
    /// o visuale leggera passa dal World. Questi test fissano il confine: acqua,
    /// stone floor e tile floor restano superfici non ospitanti anche se arriva un
    /// delta apparentemente valido.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>World minimale</b>: mappa piccola con catalogo superfici locale.</item>
    ///   <item><b>Piante fisiche</b>: delta Born e full refresh accettano solo erba lontana da superfici non host.</item>
    ///   <item><b>Vegetazione diffusa</b>: delta Appeared accettato solo su erba lontana da superfici non host.</item>
    /// </list>
    /// </summary>
    public sealed class BiosphereSurfaceHostGuardQaTests
    {
        // =============================================================================
        // PhysicalPlantDeltaIsRejectedOnWaterStoneAndTile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una pianta fisica nata da delta Biosfera non venga proiettata
        /// su acqua o pavimenti artificiali.
        /// </para>
        /// </summary>
        [Test]
        public void PhysicalPlantDeltaIsRejectedOnWaterStoneAndTile()
        {
            World world = MakeWorldWithSurfaceCatalog();

            SetSurface(world, 1, 1, CellSurfaceMacro.Natural, "grass");
            SetSurface(world, 3, 1, CellSurfaceMacro.Water, "water");
            SetSurface(world, 5, 1, CellSurfaceMacro.Artificial, "stone_floor");
            SetSurface(world, 7, 1, CellSurfaceMacro.Artificial, "tile_floor");

            Assert.That(world.ApplyEnvironmentPhysicalPlantDelta(MakePlantDelta(1, 1, 1)), Is.True);
            Assert.That(world.ApplyEnvironmentPhysicalPlantDelta(MakePlantDelta(2, 3, 1)), Is.False);
            Assert.That(world.ApplyEnvironmentPhysicalPlantDelta(MakePlantDelta(3, 5, 1)), Is.False);
            Assert.That(world.ApplyEnvironmentPhysicalPlantDelta(MakePlantDelta(4, 7, 1)), Is.False);

            Assert.That(world.HasPhysicalPlantAt(1, 1), Is.True);
            Assert.That(world.HasPhysicalPlantAt(3, 1), Is.False);
            Assert.That(world.HasPhysicalPlantAt(5, 1), Is.False);
            Assert.That(world.HasPhysicalPlantAt(7, 1), Is.False);
        }

        // =============================================================================
        // PhysicalPlantFullRefreshIsRejectedOnWaterStoneAndTile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una full refresh dei placement fisici non proietti piante
        /// biologiche vive su superfici non ospitanti.
        /// </para>
        /// </summary>
        [Test]
        public void PhysicalPlantFullRefreshIsRejectedOnWaterStoneAndTile()
        {
            World world = MakeWorldWithSurfaceCatalog();

            SetSurface(world, 1, 3, CellSurfaceMacro.Natural, "grass");
            SetSurface(world, 3, 3, CellSurfaceMacro.Water, "water");
            SetSurface(world, 5, 3, CellSurfaceMacro.Artificial, "stone_floor");
            SetSurface(world, 7, 3, CellSurfaceMacro.Artificial, "tile_floor");

            var state = new EnvironmentState();
            var areaId = new EnvironmentAreaId(1);
            Assert.That(
                state.SetAreaDefinition(new EnvironmentAreaDefinition(
                    areaId,
                    EnvironmentAreaKind.Vegetation,
                    new EnvironmentAreaBounds(0, 0, 7, 7),
                    priority: 0,
                    isEnabled: true,
                    key: "qa_biological_area")),
                Is.True);

            var placements = new List<EnvironmentPhysicalPlantPlacement>
            {
                MakePlantPlacement(state, areaId, 11, 1, 3),
                MakePlantPlacement(state, areaId, 12, 3, 3),
                MakePlantPlacement(state, areaId, 13, 5, 3),
                MakePlantPlacement(state, areaId, 14, 7, 3)
            };

            Assert.That(
                state.ReplaceBiologicalPlacementsForSaveLoad(null, placements),
                Is.EqualTo(0));

            world.SetEnvironmentState(state);

            Assert.That(world.ApplyEnvironmentPhysicalPlantProjections(), Is.EqualTo(1));
            Assert.That(world.HasPhysicalPlantAt(1, 3), Is.True);
            Assert.That(world.HasPhysicalPlantAt(3, 3), Is.False);
            Assert.That(world.HasPhysicalPlantAt(5, 3), Is.False);
            Assert.That(world.HasPhysicalPlantAt(7, 3), Is.False);
        }

        // =============================================================================
        // PhysicalPlantDeltaIsRejectedOnNaturalBorderAroundBlockedSurface
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una cella prato adiacente ad acqua venga trattata come bordo
        /// visuale non ospitante per piante fisiche.
        /// </para>
        /// </summary>
        [Test]
        public void PhysicalPlantDeltaIsRejectedOnNaturalBorderAroundBlockedSurface()
        {
            World world = MakeWorldWithSurfaceCatalog();

            SetSurface(world, 1, 1, CellSurfaceMacro.Natural, "grass");
            SetSurface(world, 2, 2, CellSurfaceMacro.Water, "water");
            SetSurface(world, 6, 6, CellSurfaceMacro.Natural, "grass");

            Assert.That(world.ApplyEnvironmentPhysicalPlantDelta(MakePlantDelta(21, 1, 1)), Is.False);
            Assert.That(world.ApplyEnvironmentPhysicalPlantDelta(MakePlantDelta(22, 6, 6)), Is.True);

            Assert.That(world.HasPhysicalPlantAt(1, 1), Is.False);
            Assert.That(world.HasPhysicalPlantAt(6, 6), Is.True);
        }

        // =============================================================================
        // DiffuseVegetationDeltaIsRejectedOnWaterStoneAndTile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la vegetazione diffusa non venga conservata nel World quando
        /// il delta punta a superfici non naturali o non abilitate dal catalogo.
        /// </para>
        /// </summary>
        [Test]
        public void DiffuseVegetationDeltaIsRejectedOnWaterStoneAndTile()
        {
            World world = MakeWorldWithSurfaceCatalog();

            SetSurface(world, 1, 5, CellSurfaceMacro.Natural, "grass");
            SetSurface(world, 3, 5, CellSurfaceMacro.Water, "water");
            SetSurface(world, 5, 5, CellSurfaceMacro.Artificial, "stone_floor");
            SetSurface(world, 7, 5, CellSurfaceMacro.Artificial, "tile_floor");

            Assert.That(world.ApplyEnvironmentDiffuseVegetationDelta(MakeVegetationDelta(1, 5)), Is.True);
            Assert.That(world.ApplyEnvironmentDiffuseVegetationDelta(MakeVegetationDelta(3, 5)), Is.False);
            Assert.That(world.ApplyEnvironmentDiffuseVegetationDelta(MakeVegetationDelta(5, 5)), Is.False);
            Assert.That(world.ApplyEnvironmentDiffuseVegetationDelta(MakeVegetationDelta(7, 5)), Is.False);
        }

        // =============================================================================
        // DiffuseVegetationDeltaIsRejectedOnNaturalBorderAroundBlockedSurface
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una cella prato adiacente a pavimento artificiale venga
        /// trattata come bordo visuale non ospitante per vegetazione diffusa.
        /// </para>
        /// </summary>
        [Test]
        public void DiffuseVegetationDeltaIsRejectedOnNaturalBorderAroundBlockedSurface()
        {
            World world = MakeWorldWithSurfaceCatalog();

            SetSurface(world, 1, 1, CellSurfaceMacro.Natural, "grass");
            SetSurface(world, 2, 1, CellSurfaceMacro.Artificial, "stone_floor");
            SetSurface(world, 6, 6, CellSurfaceMacro.Natural, "grass");

            Assert.That(world.ApplyEnvironmentDiffuseVegetationDelta(MakeVegetationDelta(1, 1)), Is.False);
            Assert.That(world.ApplyEnvironmentDiffuseVegetationDelta(MakeVegetationDelta(6, 6)), Is.True);
        }

        // =============================================================================
        // MakeWorldWithSurfaceCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un World minimale con le definizioni superficie necessarie al test.
        /// </para>
        /// </summary>
        private static World MakeWorldWithSurfaceCatalog()
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            world.InitMap(10, 10);

            // Il test costruisce il catalogo in memoria per restare indipendente da
            // Resources/Unity import e verificare solo il contratto Core.
            AddSurfaceDef(world, "grass", "Natural", canHostVegetation: true, canHostPlant: true);
            AddSurfaceDef(world, "water", "Water", canHostVegetation: false, canHostPlant: false);
            AddSurfaceDef(world, "stone_floor", "Artificial", canHostVegetation: false, canHostPlant: false);
            AddSurfaceDef(world, "tile_floor", "Artificial", canHostVegetation: false, canHostPlant: false);

            return world;
        }

        // =============================================================================
        // AddSurfaceDef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una definizione superficie data-driven nel catalogo del World.
        /// </para>
        /// </summary>
        private static void AddSurfaceDef(
            World world,
            string id,
            string macroSurface,
            bool canHostVegetation,
            bool canHostPlant)
        {
            world.SurfaceDefs[id] = new CellSurfaceDef
            {
                Id = id,
                DisplayName = id,
                MacroSurface = macroSurface,
                CanHostNaturalVegetation = canHostVegetation,
                CanHostPhysicalPlant = canHostPlant
            };
        }

        // =============================================================================
        // SetSurface
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta una superficie cella e fallisce subito se il layer rifiuta il dato.
        /// </para>
        /// </summary>
        private static void SetSurface(
            World world,
            int x,
            int y,
            CellSurfaceMacro macroSurface,
            string surfaceKey)
        {
            Assert.That(
                world.CellSurfaces.SetSurface(x, y, macroSurface, surfaceKey),
                Is.True);
        }

        // =============================================================================
        // MakePlantDelta
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un delta Born vivo per una pianta fisica di test.
        /// </para>
        /// </summary>
        private static EnvironmentPhysicalPlantDelta MakePlantDelta(int plantId, int x, int y)
        {
            return new EnvironmentPhysicalPlantDelta(
                EnvironmentPhysicalPlantDeltaKind.Born,
                new EnvironmentPlantId(plantId),
                new EnvironmentAreaId(1),
                new EnvironmentCellCoord(x, y),
                default,
                "oak",
                "seedling",
                EnvironmentPlantHealthState.Healthy,
                isAlive: true);
        }

        // =============================================================================
        // MakePlantPlacement
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una PlantInstance nello stato e restituisce il relativo placement.
        /// </para>
        /// </summary>
        private static EnvironmentPhysicalPlantPlacement MakePlantPlacement(
            EnvironmentState state,
            EnvironmentAreaId areaId,
            int plantId,
            int x,
            int y)
        {
            var id = new EnvironmentPlantId(plantId);
            var cell = new EnvironmentCellCoord(x, y);

            Assert.That(
                state.SetPlantInstance(new EnvironmentPlantInstance(
                    id,
                    "oak",
                    cell,
                    3,
                    EnvironmentPlantGrowthStage.Seedling,
                    "seedling",
                    EnvironmentPlantHealthState.Healthy,
                    1f,
                    0.2f,
                    false,
                    areaId)),
                Is.True);

            return new EnvironmentPhysicalPlantPlacement(
                id,
                areaId,
                cell,
                "oak");
        }

        // =============================================================================
        // MakeVegetationDelta
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un delta Appeared valido per vegetazione diffusa di test.
        /// </para>
        /// </summary>
        private static EnvironmentDiffuseVegetationDelta MakeVegetationDelta(int x, int y)
        {
            return new EnvironmentDiffuseVegetationDelta(
                EnvironmentDiffuseVegetationDeltaKind.Appeared,
                new EnvironmentAreaId(1),
                new EnvironmentCellCoord(x, y),
                EnvironmentVegetationKind.Grass,
                EnvironmentVegetationCoverageBand.Medium,
                EnvironmentVegetationConditionBand.Healthy);
        }
    }
}
