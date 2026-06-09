using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionSceneAdapterContractHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato smoke del contratto adapter scena interazione ArcGraph.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphInteractionSceneAdapterContractHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly ArcGraphInteractionTargetKind ActorTargetKind;
        public readonly ArcGraphInteractionTargetKind UiTargetKind;
        public readonly bool ConsumerReceivedFrame;
        public readonly bool ZoomChanged;

        public ArcGraphInteractionSceneAdapterContractHarnessResult(
            bool passed,
            string reason,
            ArcGraphInteractionTargetKind actorTargetKind,
            ArcGraphInteractionTargetKind uiTargetKind,
            bool consumerReceivedFrame,
            bool zoomChanged)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            ActorTargetKind = actorTargetKind;
            UiTargetKind = uiTargetKind;
            ConsumerReceivedFrame = consumerReceivedFrame;
            ZoomChanged = zoomChanged;
        }
    }

    // =============================================================================
    // ArcGraphInteractionSceneAdapterContractHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il contratto scena interattivo senza Unity.
    /// </para>
    ///
    /// <para><b>Principio architetturale: scena simulata tramite valori primitivi</b></para>
    /// <para>
    /// Lo smoke test non legge mouse, tastiera, camera, EventSystem, World o
    /// MapGrid. Costruisce input sintetico, liste actor/object e un consumer finto
    /// per verificare che il contratto applichi zoom, produca picking e consegni il
    /// frame solo quando il chiamante lo richiede.
    /// </para>
    /// </summary>
    public static class ArcGraphInteractionSceneAdapterContractHarness
    {
        public static ArcGraphInteractionSceneAdapterContractHarnessResult RunDefaultSmoke()
        {
            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var state = ArcGraphViewState.CreateDefault(config);
            state.SetZoomLevel(4, config);

            var actorItems = new List<ArcGraphActorRenderItem>
            {
                CreateActor(10, 120, 120)
            };

            var objectItems = new List<ArcGraphObjectRenderItem>
            {
                CreateObject(20, 121, 120)
            };

            var contract = new ArcGraphInteractionSceneAdapterContract();
            var consumer = new CapturingConsumer();

            ArcGraphInteractionSceneFrame actorSceneFrame = new ArcGraphInteractionSceneFrame(
                CreatePointerFrame(5f, 5f, false, wheelStepDelta: 0),
                20,
                20,
                shouldDispatchToConsumer: true,
                sourceFrameIndex: 1);

            ArcGraphInteractionSceneAdapterDiagnostics actorDiagnostics =
                contract.ProcessFrame(
                    config,
                    state,
                    actorSceneFrame,
                    actorItems,
                    objectItems,
                    consumer);

            ArcGraphInteractionSceneFrame uiSceneFrame = new ArcGraphInteractionSceneFrame(
                CreatePointerFrame(10f, 10f, true, wheelStepDelta: 1),
                20,
                20,
                shouldDispatchToConsumer: false,
                sourceFrameIndex: 2);

            ArcGraphInteractionSceneAdapterDiagnostics uiDiagnostics =
                contract.ProcessFrame(
                    config,
                    state,
                    uiSceneFrame,
                    actorItems,
                    objectItems,
                    consumer: null);

            bool passed = actorDiagnostics.TargetKind == ArcGraphInteractionTargetKind.Actor
                          && actorDiagnostics.DidDispatchToConsumer
                          && consumer.WasCalled
                          && uiDiagnostics.TargetKind == ArcGraphInteractionTargetKind.UiBlocked
                          && uiDiagnostics.DidChangeZoom == false
                          && uiDiagnostics.DidDispatchToConsumer == false;

            return new ArcGraphInteractionSceneAdapterContractHarnessResult(
                passed,
                passed ? "InteractionSceneAdapterContractSmokePassed" : "InteractionSceneAdapterContractSmokeFailed",
                actorDiagnostics.TargetKind,
                uiDiagnostics.TargetKind,
                consumer.WasCalled,
                uiDiagnostics.DidChangeZoom);
        }

        private static ArcGraphViewInputFrame CreatePointerFrame(
            float screenX,
            float screenY,
            bool isPointerOverUi,
            int wheelStepDelta)
        {
            return new ArcGraphViewInputFrame(
                wheelStepDelta,
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
                false,
                true,
                false,
                -1,
                -1,
                true,
                "None",
                ArcGraphRenderSortKey.FromCell(cell, 50, ArcGraphRenderItemKind.Object, objectId));
        }

        private sealed class CapturingConsumer : IArcGraphInteractionFrameConsumer
        {
            public bool WasCalled { get; private set; }

            public void ConsumeInteractionFrame(
                ArcGraphInteractionFrame interactionFrame,
                ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
            {
                WasCalled = interactionFrame.TargetKind == ArcGraphInteractionTargetKind.Actor
                            && diagnostics.DidDispatchToConsumer;
            }
        }
    }
}
