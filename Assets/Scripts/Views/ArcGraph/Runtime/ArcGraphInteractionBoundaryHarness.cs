using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionBoundaryHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato smoke del boundary interattivo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: QA senza scena Unity</b></para>
    /// <para>
    /// Il risultato permette di validare picking actor/object/cella e blocco UI senza
    /// creare GameObject, leggere mouse fisico o richiedere test visuale.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphInteractionBoundaryHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly ArcGraphInteractionTargetKind ActorTargetKind;
        public readonly ArcGraphInteractionTargetKind CellTargetKind;
        public readonly ArcGraphInteractionTargetKind UiTargetKind;

        public ArcGraphInteractionBoundaryHarnessResult(
            bool passed,
            string reason,
            ArcGraphInteractionTargetKind actorTargetKind,
            ArcGraphInteractionTargetKind cellTargetKind,
            ArcGraphInteractionTargetKind uiTargetKind)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            ActorTargetKind = actorTargetKind;
            CellTargetKind = cellTargetKind;
            UiTargetKind = uiTargetKind;
        }
    }

    // =============================================================================
    // ArcGraphInteractionBoundaryHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il contratto di interazione ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contract-first</b></para>
    /// <para>
    /// Lo smoke test costruisce una view 250x250, una queue actor/object minima e
    /// tre frame input: uno su actor, uno su cella vuota e uno bloccato dalla UI.
    /// Questo verifica il comportamento del boundary senza introdurre renderer,
    /// input fisico, scene, DevTools o accessi globali.
    /// </para>
    /// </summary>
    public static class ArcGraphInteractionBoundaryHarness
    {
        public static ArcGraphInteractionBoundaryHarnessResult RunDefaultSmoke()
        {
            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var state = ArcGraphViewState.CreateDefault(config);

            var actorItems = new List<ArcGraphActorRenderItem>
            {
                CreateActor(10, 120, 120)
            };

            var objectItems = new List<ArcGraphObjectRenderItem>
            {
                CreateObject(20, 121, 120)
            };

            var builder = new ArcGraphInteractionBoundaryBuilder();

            ArcGraphInteractionFrame actorFrame = builder.Build(
                config,
                state,
                CreatePointerFrame(5f, 5f, false),
                20,
                20,
                actorItems,
                objectItems);

            ArcGraphInteractionFrame cellFrame = builder.Build(
                config,
                state,
                CreatePointerFrame(2f, 2f, false),
                20,
                20,
                actorItems,
                objectItems);

            ArcGraphInteractionFrame uiFrame = builder.Build(
                config,
                state,
                CreatePointerFrame(10f, 10f, true),
                20,
                20,
                actorItems,
                objectItems);

            bool passed = actorFrame.TargetKind == ArcGraphInteractionTargetKind.Actor
                          && actorFrame.ActorId == 10
                          && actorFrame.HasObject == false
                          && cellFrame.TargetKind == ArcGraphInteractionTargetKind.Cell
                          && uiFrame.TargetKind == ArcGraphInteractionTargetKind.UiBlocked;

            return new ArcGraphInteractionBoundaryHarnessResult(
                passed,
                passed ? "InteractionBoundarySmokePassed" : "InteractionBoundarySmokeFailed",
                actorFrame.TargetKind,
                cellFrame.TargetKind,
                uiFrame.TargetKind);
        }

        private static ArcGraphViewInputFrame CreatePointerFrame(
            float screenX,
            float screenY,
            bool isPointerOverUi)
        {
            return new ArcGraphViewInputFrame(
                wheelStepDelta: 0,
                isMiddleMouseHeld: false,
                mouseDeltaPixelsX: 0f,
                mouseDeltaPixelsY: 0f,
                pointerScreenX: screenX,
                pointerScreenY: screenY,
                hasPointerScreenPosition: true,
                isPointerOverUi: isPointerOverUi);
        }

        private static ArcGraphActorRenderItem CreateActor(int actorId, int x, int y)
        {
            var cell = ArcGraphZLevelPolicy.CreateRuntimeCell(x, y);
            return new ArcGraphActorRenderItem(
                actorId,
                cell,
                x + 0.5f,
                y + 0.5f,
                0f,
                "harness/actor",
                ArcGraphActorLodMode.FullFlatSprite,
                true,
                true,
                false,
                0f,
                true,
                "None",
                ArcGraphRenderSortKey.FromCell(cell, 100, ArcGraphRenderItemKind.Actor, actorId));
        }

        private static ArcGraphObjectRenderItem CreateObject(int objectId, int x, int y)
        {
            var cell = ArcGraphZLevelPolicy.CreateRuntimeCell(x, y);
            return new ArcGraphObjectRenderItem(
                objectId,
                "harness_object",
                cell,
                "harness/object",
                ArcGraphObjectLodMode.StaticSprites,
                true,
                false,
                -1,
                -1,
                true,
                "None",
                ArcGraphRenderSortKey.FromCell(cell, 50, ArcGraphRenderItemKind.Object, objectId));
        }
    }
}
