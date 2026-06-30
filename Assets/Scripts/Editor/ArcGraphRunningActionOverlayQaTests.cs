using Arcontio.Core;
using Arcontio.View.ArcGraph;
using NUnit.Framework;

namespace Arcontio.Tests
{
    // =============================================================================
    // ArcGraphRunningActionOverlayQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test data-only per il piccolo overlay ArcGraph che mostra sopra l'NPC la
    /// running action corrente e il tempo residuo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI da snapshot, non da World diretto</b></para>
    /// <para>
    /// Questi test verificano il contratto di trasporto del dato visuale: il
    /// progresso nasce da uno snapshot read-only della running action e arriva al
    /// render item actor come DTO immutabile. Non vengono creati GameObject, non si
    /// leggono dizionari del World e non si modificano job o command.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Normalization</b>: label, progress e remaining sono gia' pronti per il renderer.</item>
    ///   <item><b>Propagation</b>: actor snapshot e queue item conservano il DTO.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRunningActionOverlayQaTests
    {
        [Test]
        public void RunningActionOverlayNormalizesProgressAndLabel()
        {
            var state = CreateUseObjectState("consume_known_food", requiredTicks: 6);
            Assert.That(state.AdvanceProgress(3, tick: 3), Is.True);

            ArcGraphActorRunningActionOverlaySnapshot overlay =
                ArcGraphActorRunningActionOverlaySnapshot.FromRunningAction(state.ToSnapshot());

            Assert.That(overlay.IsActive, Is.True);
            Assert.That(overlay.ActionKind, Is.EqualTo(RunningActionKind.UseObject));
            Assert.That(overlay.JobActionId, Is.EqualTo("consume_known_food"));
            Assert.That(overlay.Label, Is.EqualTo("mangia"));
            Assert.That(overlay.Progress01, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(overlay.Remaining01, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void RunningActionOverlayHandlesZeroRequiredTicks()
        {
            var overlay = new ArcGraphActorRunningActionOverlaySnapshot(
                true,
                RunningActionKind.UseObject,
                "pickup_food_to_hand",
                elapsedTicks: 0,
                requiredTicks: 0,
                label: string.Empty);

            Assert.That(overlay.IsActive, Is.True);
            Assert.That(overlay.Label, Is.EqualTo("raccoglie"));
            Assert.That(overlay.Progress01, Is.EqualTo(1f));
            Assert.That(overlay.Remaining01, Is.EqualTo(0f));
        }

        [Test]
        public void ActorRenderItemPreservesRunningActionOverlay()
        {
            var overlay = new ArcGraphActorRunningActionOverlaySnapshot(
                true,
                RunningActionKind.UseObject,
                "pickup_food_to_hand",
                elapsedTicks: 1,
                requiredTicks: 4,
                label: "raccoglie");
            var actorLayer = new ArcGraphActorLayer();
            var snapshot = new ArcGraphActorVisualSnapshot(
                7,
                ArcGraphZLevelPolicy.CreateRuntimeCell(2, 3),
                "ArcGraph/NPC/default",
                ArcGraphActorMotionSnapshot.None(ArcGraphZLevelPolicy.CreateRuntimeCell(2, 3)),
                hasHungerValue: false,
                hunger01: 0f,
                facingDirectionKey: "south",
                runningActionOverlay: overlay);
            actorLayer.ReplaceSnapshots(new[] { snapshot });

            var builder = new ArcGraphActorRenderQueueBuilder();
            var items = new System.Collections.Generic.List<ArcGraphActorRenderItem>();
            builder.Build(
                actorLayer,
                ArcGraphZoomLodPolicy.ResolveFullDetail(),
                items);

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].RunningActionOverlay.IsActive, Is.True);
            Assert.That(items[0].RunningActionOverlay.Label, Is.EqualTo("raccoglie"));
            Assert.That(items[0].RunningActionOverlay.Remaining01, Is.EqualTo(0.75f).Within(0.0001f));
        }

        private static RunningActionRuntimeState CreateUseObjectState(string actionId, int requiredTicks)
        {
            var policy = new RunningActionCompletionPolicy(
                requiredTicks,
                timeoutTicks: 0,
                failureReason: JobFailureReason.StepFailed,
                interruptionReason: JobFailureReason.Preempted);

            return RunningActionRuntimeState.Start(
                "qa-use-object",
                RunningActionKind.UseObject,
                npcId: 7,
                jobId: "job-qa",
                phaseId: "phase-qa",
                jobActionId: actionId,
                startedTick: 0,
                completionPolicy: policy);
        }
    }
}
