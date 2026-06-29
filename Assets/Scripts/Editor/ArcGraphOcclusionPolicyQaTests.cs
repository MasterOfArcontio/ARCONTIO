using System.Collections.Generic;
using Arcontio.View.ArcGraph;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // ArcGraphOcclusionPolicyQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la policy di occlusione visuale ArcGraph usata da
    /// picking, trasparenza muri e sagome leggere.
    /// </para>
    ///
    /// <para><b>Principio architetturale: visual quality senza simulazione</b></para>
    /// <para>
    /// Questi test lavorano solo su render item value-only. Non aprono scene, non
    /// leggono <c>World</c>, non caricano sprite e non modificano asset. Lo scopo e'
    /// verificare che la definizione di "dietro un muro alto" resti stabile e
    /// riutilizzabile sia dal boundary di picking sia dal controller renderer-only.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Policy</b>: riconoscimento muro alto e target dietro.</item>
    ///   <item><b>Boundary</b>: click su muro alto seleziona target coperto.</item>
    ///   <item><b>UI block</b>: puntatore sopra UI blocca anche il picking assistito.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphOcclusionPolicyQaTests
    {
        // =============================================================================
        // TallFadeableWallCanPickActorBehind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un muro alto fadeable trovi un NPC nella cella visualmente
        /// coperta.
        /// </para>
        /// </summary>
        [Test]
        public void TallFadeableWallCanPickActorBehind()
        {
            ArcGraphObjectRenderItem wall = CreateWall(100, 4, 4);
            ArcGraphActorRenderItem actor = CreateActor(7, 4, 5);

            bool picked = ArcGraphOcclusionPolicy.TryPickCoveredTarget(
                wall,
                new[] { actor },
                new ArcGraphObjectRenderItem[0],
                2,
                out ArcGraphOcclusionTarget target);

            Assert.That(picked, Is.True);
            Assert.That(target.Kind, Is.EqualTo(ArcGraphOcclusionTargetKind.Actor));
            Assert.That(target.EntityId, Is.EqualTo(7));
        }

        // =============================================================================
        // DiffuseOrLowObjectDoesNotBecomeOccluder
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un oggetto non fadeable o non alto non venga trattato come
        /// muro trasparente.
        /// </para>
        /// </summary>
        [Test]
        public void DiffuseOrLowObjectDoesNotBecomeOccluder()
        {
            ArcGraphObjectRenderItem lowObject = CreateObject(
                101,
                "crate",
                4,
                4,
                visualHeightPixels: 32,
                visualBaseHeightPixels: 32,
                fadeWhenActorBehind: true);

            Assert.That(ArcGraphOcclusionPolicy.IsFadeableOccluder(lowObject), Is.False);
        }

        // =============================================================================
        // BoundaryPicksCoveredActorWhenPointerHitsWallBase
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il caso operativo del mouse: puntatore sulla base del muro,
        /// target finale sull'NPC coperto dietro al muro.
        /// </para>
        /// </summary>
        [Test]
        public void BoundaryPicksCoveredActorWhenPointerHitsWallBase()
        {
            var builder = new ArcGraphInteractionBoundaryBuilder();
            ArcGraphViewInputFrame input = CreatePointerInput(isPointerOverUi: false);
            ArcGraphCellCoord pointerCell = new ArcGraphCellCoord(4, 4, 0);

            ArcGraphInteractionFrame frame = builder.BuildFromResolvedCell(
                ArcGraphMapViewConfig.CreateDefaultV033(),
                ArcGraphViewState.CreateDefault(ArcGraphMapViewConfig.CreateDefaultV033()),
                input,
                1920,
                1080,
                new[] { CreateActor(7, 4, 5) },
                new[] { CreateWall(100, 4, 4) },
                new List<ArcGraphVegetationRenderItem>(),
                pointerCell);

            Assert.That(frame.TargetKind, Is.EqualTo(ArcGraphInteractionTargetKind.Actor));
            Assert.That(frame.ActorId, Is.EqualTo(7));
        }

        // =============================================================================
        // BoundaryKeepsUiBlockedWhenPointerIsOverUi
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che la UI continui a bloccare il picking assistito dietro muri.
        /// </para>
        /// </summary>
        [Test]
        public void BoundaryKeepsUiBlockedWhenPointerIsOverUi()
        {
            var builder = new ArcGraphInteractionBoundaryBuilder();
            ArcGraphViewInputFrame input = CreatePointerInput(isPointerOverUi: true);

            ArcGraphInteractionFrame frame = builder.BuildFromResolvedCell(
                ArcGraphMapViewConfig.CreateDefaultV033(),
                ArcGraphViewState.CreateDefault(ArcGraphMapViewConfig.CreateDefaultV033()),
                input,
                1920,
                1080,
                new[] { CreateActor(7, 4, 5) },
                new[] { CreateWall(100, 4, 4) },
                new List<ArcGraphVegetationRenderItem>(),
                new ArcGraphCellCoord(4, 4, 0));

            Assert.That(frame.TargetKind, Is.EqualTo(ArcGraphInteractionTargetKind.UiBlocked));
        }

        // =============================================================================
        // TargetCellBehindWallFindsCoveringOccluder
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che anche la cella del target dietro il muro possa trovare il
        /// muro coprente, non solo la cella base del muro stesso.
        /// </para>
        /// </summary>
        [Test]
        public void TargetCellBehindWallFindsCoveringOccluder()
        {
            ArcGraphObjectRenderItem wall = CreateWall(100, 4, 4);
            ArcGraphObjectRenderItem crate = CreateObject(
                200,
                "crate",
                4,
                5,
                visualHeightPixels: 32,
                visualBaseHeightPixels: 32,
                fadeWhenActorBehind: false);

            bool found = ArcGraphOcclusionPolicy.TryFindOccluderCoveringTargetCell(
                new[] { wall, crate },
                crate.Cell,
                2,
                ignoredObjectId: crate.ObjectId,
                out ArcGraphObjectRenderItem occluder);

            Assert.That(found, Is.True);
            Assert.That(occluder.ObjectId, Is.EqualTo(wall.ObjectId));
        }

        private static ArcGraphViewInputFrame CreatePointerInput(bool isPointerOverUi)
        {
            return new ArcGraphViewInputFrame(
                0,
                false,
                0f,
                0f,
                100f,
                100f,
                true,
                isPointerOverUi);
        }

        private static ArcGraphActorRenderItem CreateActor(int actorId, int x, int y)
        {
            var cell = new ArcGraphCellCoord(x, y, 0);
            return new ArcGraphActorRenderItem(
                actorId,
                cell,
                x,
                y,
                0f,
                "qa_actor",
                default,
                allowsSpriteAnimation: false,
                allowsLayeredActorSprites: false,
                hasMotion: false,
                motionProgress01: 0f,
                isVisible: true,
                hiddenReason: "None",
                ArcGraphRenderSortKey.FromCell(cell, 20, ArcGraphRenderItemKind.Actor, actorId));
        }

        private static ArcGraphObjectRenderItem CreateWall(int objectId, int x, int y)
        {
            return CreateObject(
                objectId,
                "wall_stone",
                x,
                y,
                visualHeightPixels: 83,
                visualBaseHeightPixels: 32,
                fadeWhenActorBehind: true);
        }

        private static ArcGraphObjectRenderItem CreateObject(
            int objectId,
            string defId,
            int x,
            int y,
            int visualHeightPixels,
            int visualBaseHeightPixels,
            bool fadeWhenActorBehind)
        {
            var cell = new ArcGraphCellCoord(x, y, 0);
            return new ArcGraphObjectRenderItem(
                objectId,
                defId,
                cell,
                "qa_object",
                default,
                showMinorItems: false,
                isHeld: false,
                holderActorId: -1,
                foodStockUnits: -1,
                footprintWidth: 1,
                footprintHeight: 1,
                visualKind: "wall",
                visualResolverKey: "wall_stone_cardinal",
                visualWidthPixels: 32,
                visualHeightPixels: visualHeightPixels,
                visualBaseWidthPixels: 32,
                visualBaseHeightPixels: visualBaseHeightPixels,
                visualBaseMiniTileMask: string.Empty,
                visualPivot: "bottom_center",
                visualOffsetX: 0,
                visualOffsetY: 0,
                fadeWhenActorBehind: fadeWhenActorBehind,
                useShadow: false,
                isVisible: true,
                hiddenReason: "None",
                ArcGraphRenderSortKey.FromCell(cell, 10, ArcGraphRenderItemKind.Object, objectId));
        }
    }
}
