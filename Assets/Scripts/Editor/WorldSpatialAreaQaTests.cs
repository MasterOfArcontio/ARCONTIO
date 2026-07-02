using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Config;
using Arcontio.Core.Save;
using NUnit.Framework;

namespace Arcontio.EditorTests
{
    // =============================================================================
    // WorldSpatialAreaQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test EditMode del modulo WorldSpatialAreas e dei landmark di supporto open
    /// space. I test restano data-only: non aprono scene, non toccano asset e non
    /// chiedono alla UI di calcolare la topologia.
    /// </para>
    ///
    /// <para><b>Principio architetturale: topologia World distinta dalla Biosfera</b></para>
    /// <para>
    /// Le aree spaziali descrivono la forma fisica della mappa. Non sono
    /// EnvironmentArea, non dipendono dalle piante e diventano un input autorizzato
    /// per overlay e provider landmark modulari.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Flood</b>: verifica spazio aperto, stanza, corridoio e area invalida.</item>
    ///   <item><b>Confini</b>: verifica porte come separatori permanenti.</item>
    ///   <item><b>Save</b>: verifica persistenza modulare separata.</item>
    ///   <item><b>Provider</b>: verifica creazione di landmark SupportOpenSpace.</item>
    /// </list>
    /// </summary>
    public sealed class WorldSpatialAreaQaTests
    {
        // =============================================================================
        // OpenMapConnectedToBorderBuildsOpenArea
        // =============================================================================
        /// <summary>
        /// <para>
        /// Una mappa priva di barriere tocca il bordo e quindi produce una sola area
        /// aperta.
        /// </para>
        /// </summary>
        [Test]
        public void OpenMapConnectedToBorderBuildsOpenArea()
        {
            World world = CreateWorld(5, 5);

            world.RebuildSpatialAreas();

            Assert.That(world.SpatialAreas.AreaCount, Is.EqualTo(1));
            Assert.That(world.TryGetSpatialAreaAt(2, 2, out WorldSpatialArea area), Is.True);
            Assert.That(area.Kind, Is.EqualTo(WorldSpatialAreaKind.OpenArea));
            Assert.That(area.OwnerKind, Is.EqualTo(OwnerKind.Community));
            Assert.That(area.OwnerId, Is.EqualTo(0));
        }

        // =============================================================================
        // ClosedRoomUnderConfiguredSurfaceBuildsClosedRoom
        // =============================================================================
        /// <summary>
        /// <para>
        /// Una componente chiusa, non stretta e sotto la soglia massima viene
        /// classificata come stanza chiusa.
        /// </para>
        /// </summary>
        [Test]
        public void ClosedRoomUnderConfiguredSurfaceBuildsClosedRoom()
        {
            World world = CreateWorld(6, 6);
            AddWallBox(world, minX: 0, minY: 0, maxX: 5, maxY: 5);

            world.RebuildSpatialAreas();

            Assert.That(world.SpatialAreas.DiagnosticCount, Is.EqualTo(0));
            Assert.That(world.TryGetSpatialAreaAt(2, 2, out WorldSpatialArea area), Is.True);
            Assert.That(area.Kind, Is.EqualTo(WorldSpatialAreaKind.ClosedRoom));
            Assert.That(area.CellCount, Is.EqualTo(16));
        }

        // =============================================================================
        // LongNarrowClosedAreaBuildsCorridor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Un'area chiusa stretta entro la larghezza configurata resta corridoio
        /// anche se e' piu' lunga di una stanza piccola.
        /// </para>
        /// </summary>
        [Test]
        public void LongNarrowClosedAreaBuildsCorridor()
        {
            World world = CreateWorld(5, 8);
            AddWallBox(world, minX: 0, minY: 0, maxX: 4, maxY: 7);

            world.RebuildSpatialAreas();

            Assert.That(world.TryGetSpatialAreaAt(2, 3, out WorldSpatialArea area), Is.True);
            Assert.That(area.Kind, Is.EqualTo(WorldSpatialAreaKind.Corridor));
            Assert.That(area.CellCount, Is.EqualTo(18));
        }

        // =============================================================================
        // ClosedAreaTooLargeAndWideProducesDiagnostic
        // =============================================================================
        /// <summary>
        /// <para>
        /// Un salone chiuso troppo grande e troppo largo non viene promosso ad area
        /// valida: produce diagnostica stabile di mappa.
        /// </para>
        /// </summary>
        [Test]
        public void ClosedAreaTooLargeAndWideProducesDiagnostic()
        {
            var sim = new SimulationParams();
            sim.spatial_areas.max_closed_room_surface_cells = 4;
            sim.spatial_areas.corridor_max_width_cells = 1;
            World world = CreateWorld(6, 6, sim);
            AddWallBox(world, minX: 0, minY: 0, maxX: 5, maxY: 5);

            world.RebuildSpatialAreas();

            Assert.That(world.SpatialAreas.AreaCount, Is.EqualTo(0));
            Assert.That(world.SpatialAreas.DiagnosticCount, Is.EqualTo(1));
            Assert.That(world.SpatialAreas.Diagnostics[0], Does.StartWith("InvalidClosedSpatialArea"));
        }

        // =============================================================================
        // DoorSeparatesAreasEvenWhenOpen
        // =============================================================================
        /// <summary>
        /// <para>
        /// Una porta e' un confine semantico permanente per le aree spaziali: anche
        /// aperta non viene assorbita dentro la stanza o lo spazio aperto.
        /// </para>
        /// </summary>
        [Test]
        public void DoorSeparatesAreasEvenWhenOpen()
        {
            World world = CreateWorld(5, 5);
            int doorId = world.CreateObject("qa_door", 2, 2, OwnerKind.Community, 0);
            world.Objects[doorId].IsOpen = true;

            world.RebuildSpatialAreas();

            Assert.That(world.IsSpatialAreaBoundaryAt(2, 2), Is.True);
            Assert.That(world.TryGetSpatialAreaAt(2, 2, out _), Is.False);
        }

        // =============================================================================
        // NonStructuralObjectsDoNotCloseAreas
        // =============================================================================
        /// <summary>
        /// <para>
        /// Oggetti mobili e piante fisiche non sono confini spaziali: il flood-fill
        /// li ignora, cosi' una radura circondata da alberi non diventa una stanza.
        /// </para>
        /// </summary>
        [Test]
        public void NonStructuralObjectsDoNotCloseAreas()
        {
            World world = CreateWorld(5, 5);
            world.CreateObject("qa_tree", 1, 2, OwnerKind.Community, 0);
            world.CreateObject("qa_tree", 2, 1, OwnerKind.Community, 0);
            world.CreateObject("qa_tree", 3, 2, OwnerKind.Community, 0);
            world.CreateObject("qa_tree", 2, 3, OwnerKind.Community, 0);

            world.RebuildSpatialAreas();

            Assert.That(world.SpatialAreas.AreaCount, Is.EqualTo(1));
            Assert.That(world.TryGetSpatialAreaAt(2, 2, out WorldSpatialArea area), Is.True);
            Assert.That(area.Kind, Is.EqualTo(WorldSpatialAreaKind.OpenArea));
        }

        // =============================================================================
        // SpatialAreasAreSavedAndLoadedAsSeparateModule
        // =============================================================================
        /// <summary>
        /// <para>
        /// Il salvataggio globale conserva la sezione spatialAreas senza chiedere al
        /// loader inventario, landmark o UI di ricostruirla.
        /// </para>
        /// </summary>
        [Test]
        public void SpatialAreasAreSavedAndLoadedAsSeparateModule()
        {
            World source = CreateWorld(6, 6);
            AddWallBox(source, minX: 0, minY: 0, maxX: 5, maxY: 5);
            source.RebuildSpatialAreas();

            WorldSaveData data = WorldSaveBuilder.BuildFromWorld(source, savedAtTick: 7);
            World target = CreateWorld(6, 6);
            bool applied = WorldSaveLoader.TryApplyObjectiveWorld(target, data, out string error);

            Assert.That(applied, Is.True, error);
            Assert.That(target.TryGetSpatialAreaAt(2, 2, out WorldSpatialArea area), Is.True);
            Assert.That(area.Kind, Is.EqualTo(WorldSpatialAreaKind.ClosedRoom));
            Assert.That(area.OwnerKind, Is.EqualTo(OwnerKind.Community));
        }

        // =============================================================================
        // SupportOpenSpaceProviderAddsSupportAnchorOnlyInOpenArea
        // =============================================================================
        /// <summary>
        /// <para>
        /// Il provider SupportOpenSpace entra nel coordinator solo dopo il primo
        /// rebuild landmark e produce anchor di supporto nello spazio aperto.
        /// </para>
        /// </summary>
        [Test]
        public void SupportOpenSpaceProviderAddsSupportAnchorOnlyInOpenArea()
        {
            var sim = new SimulationParams();
            sim.npcVisionRangeCells = 3;
            sim.spatial_areas.support_lm_spacing_cells = 0;
            sim.spatial_areas.support_lm_spacing_vision_margin_cells = 1;
            sim.spatial_areas.support_lm_coverage_radius_multiplier = 1;
            World world = CreateWorld(8, 8, sim);

            world.RebuildLandmarksBootstrap();

            var nodes = new List<LandmarkOverlayNode>();
            var edges = new List<LandmarkOverlayEdge>();
            world.LandmarkRegistry.FillOverlayData(nodes, edges);

            bool foundSupport = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Kind != (int)LandmarkRegistry.LandmarkKind.SupportOpenSpaceAnchor)
                    continue;

                foundSupport = true;
                Assert.That(world.TryGetSpatialAreaAt(nodes[i].CellX, nodes[i].CellY, out WorldSpatialArea area), Is.True);
                Assert.That(area.Kind, Is.EqualTo(WorldSpatialAreaKind.OpenArea));
            }

            Assert.That(foundSupport, Is.True);
        }

        // =============================================================================
        // DefaultMapHouseBuildsClosedRoom
        // =============================================================================
        /// <summary>
        /// <para>
        /// La casa reale della mappa default deve essere una stanza chiusa 12x12
        /// interna, non parte dello spazio aperto globale.
        /// </para>
        /// </summary>
        [Test]
        public void DefaultMapHouseBuildsClosedRoom()
        {
            var world = new World(new WorldConfig(new SimulationParams()));
            ObjectDatabaseLoader.LoadIntoWorld(world);
            CellSurfaceDatabaseLoader.LoadIntoWorld(world);

            bool loaded = WorldMapConfigLoader.LoadIntoWorld(world);

            Assert.That(loaded, Is.True);
            Assert.That(world.TryGetSpatialAreaAt(52, 14, out WorldSpatialArea house), Is.True);
            Assert.That(house.Kind, Is.EqualTo(WorldSpatialAreaKind.ClosedRoom));
            Assert.That(house.MinX, Is.EqualTo(46));
            Assert.That(house.MaxX, Is.EqualTo(57));
            Assert.That(house.MinY, Is.EqualTo(9));
            Assert.That(house.MaxY, Is.EqualTo(20));
            Assert.That(house.CellCount, Is.EqualTo(144));
            Assert.That(world.TryGetSpatialAreaAt(51, 8, out _), Is.False);
            Assert.That(world.TryGetSpatialAreaAt(58, 14, out _), Is.False);
            Assert.That(world.TryGetSpatialAreaAt(32, 32, out WorldSpatialArea outside), Is.True);
            Assert.That(outside.Kind, Is.EqualTo(WorldSpatialAreaKind.OpenArea));
        }

        // =============================================================================
        // BiologicalAnchorsDoNotCoverSupportOpenSpace
        // =============================================================================
        /// <summary>
        /// <para>
        /// I landmark biologici restano ancore cognitive/ambientali e non saturano
        /// la copertura navigazionale richiesta dai support landmark.
        /// </para>
        /// </summary>
        [Test]
        public void BiologicalAnchorsDoNotCoverSupportOpenSpace()
        {
            var sim = new SimulationParams();
            sim.spatial_areas.support_lm_spacing_cells = 8;
            sim.spatial_areas.support_lm_coverage_radius_multiplier = 2;
            World world = CreateWorld(24, 24, sim);
            world.RebuildSpatialAreas();

            var registry = new LandmarkRegistry();
            var manual = new List<LandmarkRegistry.ManualLandmarkCandidate>
            {
                new LandmarkRegistry.ManualLandmarkCandidate(
                    8,
                    8,
                    LandmarkRegistry.LandmarkKind.BiologicalAnchor,
                    1f,
                    new LandmarkProviderKey(LandmarkProviderKind.EnvironmentBiosphere, 1))
            };
            var resolutions = new List<LandmarkRegistry.ManualLandmarkResolution>();
            registry.RebuildFromWorld(world, manual, resolutions);

            var provider = new SupportOpenSpaceLandmarkProvider();
            var supportCandidates = new List<LandmarkRegistry.ManualLandmarkCandidate>();
            int produced = provider.BuildCoverageLandmarkCandidates(world, registry, supportCandidates);

            Assert.That(produced, Is.GreaterThan(0));
            Assert.That(supportCandidates.Count, Is.GreaterThan(0));
        }

        private static World CreateWorld(int width, int height, SimulationParams sim = null)
        {
            var world = new World(new WorldConfig(sim ?? new SimulationParams()));
            world.InitMap(width, height);
            AddObjectDefs(world);
            return world;
        }

        private static void AddObjectDefs(World world)
        {
            world.ObjectDefs["qa_wall"] = new ObjectDef
            {
                Id = "qa_wall",
                DisplayName = "QA Wall",
                FootprintWidth = 1,
                FootprintHeight = 1,
                IsOccluder = true,
                BlocksMovement = true,
                BlocksVision = true,
                CanPlaceInHand = false,
                CanPlaceInContainer = false,
                Visual = new ObjectVisualDef { VisualKind = "wall" }
            };
            world.ObjectDefs["qa_door"] = new ObjectDef
            {
                Id = "qa_door",
                DisplayName = "QA Door",
                FootprintWidth = 1,
                FootprintHeight = 1,
                IsDoor = true,
                IsLockable = true,
                IsOccluder = true,
                BlocksMovement = true,
                BlocksVision = true,
                CanPlaceInHand = false,
                CanPlaceInContainer = false
            };
            world.ObjectDefs["qa_tree"] = new ObjectDef
            {
                Id = "qa_tree",
                DisplayName = "QA Tree",
                FootprintWidth = 1,
                FootprintHeight = 1,
                IsOccluder = true,
                BlocksMovement = false,
                BlocksVision = false,
                CanPlaceInHand = false,
                CanPlaceInContainer = false,
                Visual = new ObjectVisualDef { VisualKind = "plant" }
            };
        }

        private static void AddWallBox(World world, int minX, int minY, int maxX, int maxY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Assert.That(world.CreateObject("qa_wall", x, minY, OwnerKind.Community, 0), Is.GreaterThan(0));
                Assert.That(world.CreateObject("qa_wall", x, maxY, OwnerKind.Community, 0), Is.GreaterThan(0));
            }

            for (int y = minY + 1; y < maxY; y++)
            {
                Assert.That(world.CreateObject("qa_wall", minX, y, OwnerKind.Community, 0), Is.GreaterThan(0));
                Assert.That(world.CreateObject("qa_wall", maxX, y, OwnerKind.Community, 0), Is.GreaterThan(0));
            }
        }
    }
}
