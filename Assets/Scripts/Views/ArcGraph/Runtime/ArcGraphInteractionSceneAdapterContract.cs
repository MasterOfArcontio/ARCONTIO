using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionSceneAdapterContract
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto C# passivo del futuro adapter scena interattivo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: coordinatore di boundary, non tool host</b></para>
    /// <para>
    /// Questo contratto riceve un frame scena gia' normalizzato, applica il
    /// controller view, costruisce il frame interattivo tramite boundary e,
    /// opzionalmente, lo consegna a un consumer esterno. Non legge dispositivi Unity,
    /// non conosce <c>World</c>, non seleziona NPC, non invia comandi e non crea UI.
    /// Il futuro <c>MonoBehaviour</c> scena dovra' limitarsi a leggere input fisico
    /// e chiamare questo contratto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_viewController</b>: applica zoom e pan a <c>ArcGraphViewState</c>.</item>
    ///   <item><b>_boundaryBuilder</b>: risolve cella, actor e object sotto puntatore.</item>
    ///   <item><b>ProcessFrame</b>: sequenza view controller -> boundary -> consumer.</item>
    ///   <item><b>CreateDiagnostics</b>: esito compatto per pannelli QA futuri.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphInteractionSceneAdapterContract
    {
        private readonly ArcGraphViewController _viewController = new();
        private readonly ArcGraphInteractionBoundaryBuilder _boundaryBuilder = new();

        public ArcGraphViewControllerResult LastViewControllerResult { get; private set; }
        public ArcGraphInteractionFrame LastInteractionFrame { get; private set; }
        public ArcGraphInteractionSceneAdapterDiagnostics LastDiagnostics { get; private set; }

        // =============================================================================
        // ProcessFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa un frame scena usando una queue render actor/object gia' prodotta.
        /// </para>
        /// </summary>
        public ArcGraphInteractionSceneAdapterDiagnostics ProcessFrame(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            ArcGraphInteractionSceneFrame sceneFrame,
            ArcGraphRenderQueue renderQueue,
            IArcGraphInteractionFrameConsumer consumer = null)
        {
            return ProcessFrame(
                config,
                viewState,
                sceneFrame,
                renderQueue != null ? renderQueue.ActorItems : null,
                renderQueue != null ? renderQueue.ObjectItems : null,
                null,
                hasRenderQueue: renderQueue != null,
                consumer);
        }

        // =============================================================================
        // ProcessFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa un frame scena usando liste actor/object gia' preparate.
        /// </para>
        ///
        /// <para><b>Overload per test e producer intermedi</b></para>
        /// <para>
        /// Lo stesso contratto puo' essere validato senza costruire una
        /// <c>ArcGraphRenderQueue</c> completa. Questo e' utile per harness e per
        /// adapter intermedi che hanno gia' liste ordinate e non devono mutare la queue.
        /// </para>
        /// </summary>
        public ArcGraphInteractionSceneAdapterDiagnostics ProcessFrame(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            ArcGraphInteractionSceneFrame sceneFrame,
            IReadOnlyList<ArcGraphActorRenderItem> actorItems,
            IReadOnlyList<ArcGraphObjectRenderItem> objectItems,
            IArcGraphInteractionFrameConsumer consumer = null)
        {
            return ProcessFrame(
                config,
                viewState,
                sceneFrame,
                actorItems,
                objectItems,
                null,
                consumer);
        }

        // =============================================================================
        // ProcessFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa un frame scena includendo gli item vegetazione fisica nel
        /// boundary di picking ArcGraph.
        /// </para>
        /// </summary>
        public ArcGraphInteractionSceneAdapterDiagnostics ProcessFrame(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            ArcGraphInteractionSceneFrame sceneFrame,
            IReadOnlyList<ArcGraphActorRenderItem> actorItems,
            IReadOnlyList<ArcGraphObjectRenderItem> objectItems,
            IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems,
            IArcGraphInteractionFrameConsumer consumer = null)
        {
            return ProcessFrame(
                config,
                viewState,
                sceneFrame,
                actorItems,
                objectItems,
                vegetationItems,
                hasRenderQueue: actorItems != null || objectItems != null || vegetationItems != null,
                consumer);
        }

        private ArcGraphInteractionSceneAdapterDiagnostics ProcessFrame(
            ArcGraphMapViewConfig config,
            ArcGraphViewState viewState,
            ArcGraphInteractionSceneFrame sceneFrame,
            IReadOnlyList<ArcGraphActorRenderItem> actorItems,
            IReadOnlyList<ArcGraphObjectRenderItem> objectItems,
            IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems,
            bool hasRenderQueue,
            IArcGraphInteractionFrameConsumer consumer)
        {
            bool hasConfig = config != null;
            bool wasViewStateProvided = viewState != null;
            config = config ?? ArcGraphMapViewConfig.CreateDefaultV033();
            viewState = viewState ?? ArcGraphViewState.CreateDefault(config);

            // Se il viewport non e' valido, non esiste una base affidabile per
            // pan, zoom o picking. Produciamo diagnostica e frame vuoto.
            if (!sceneFrame.HasValidViewport)
            {
                LastViewControllerResult = default;
                LastInteractionFrame = ArcGraphInteractionFrame.Empty("ViewportInvalid");
                LastDiagnostics = CreateDiagnostics(
                    hasConfig || config != null,
                    wasViewStateProvided,
                    hasRenderQueue,
                    sceneFrame,
                    didApplyViewController: false,
                    viewResult: default,
                    didBuildInteractionFrame: false,
                    wasConsumerProvided: consumer != null,
                    didDispatchToConsumer: false,
                    LastInteractionFrame,
                    "ViewportInvalid");

                return LastDiagnostics;
            }

            // Il controller view lavora solo su stato vista: niente camera, niente
            // input fisico, niente renderer. Questo mantiene pan/zoom testabili.
            LastViewControllerResult = _viewController.ApplyInputFrame(
                config,
                viewState,
                sceneFrame.Input,
                sceneFrame.ViewportPixelWidth,
                sceneFrame.ViewportPixelHeight);

            // Dopo aver aggiornato la vista, il boundary usa la cella scene-side se
            // il wrapper l'ha gia' risolta dalla camera reale. Questo mantiene
            // allineati hover cella, selection e picking dopo pan/zoom fisico.
            LastInteractionFrame = sceneFrame.HasSceneResolvedCell
                ? _boundaryBuilder.BuildFromResolvedCell(
                    config,
                    viewState,
                    sceneFrame.Input,
                    sceneFrame.ViewportPixelWidth,
                    sceneFrame.ViewportPixelHeight,
                    actorItems,
                    objectItems,
                    vegetationItems,
                    sceneFrame.SceneResolvedCell)
                : _boundaryBuilder.Build(
                    config,
                    viewState,
                    sceneFrame.Input,
                    sceneFrame.ViewportPixelWidth,
                    sceneFrame.ViewportPixelHeight,
                    actorItems,
                    objectItems,
                    vegetationItems);

            bool shouldDispatch = sceneFrame.ShouldDispatchToConsumer && consumer != null;
            LastDiagnostics = CreateDiagnostics(
                hasConfig || config != null,
                wasViewStateProvided,
                hasRenderQueue,
                sceneFrame,
                didApplyViewController: true,
                LastViewControllerResult,
                didBuildInteractionFrame: true,
                wasConsumerProvided: consumer != null,
                didDispatchToConsumer: shouldDispatch,
                LastInteractionFrame,
                shouldDispatch ? "InteractionFrameDispatched" : LastInteractionFrame.Reason);

            if (shouldDispatch)
                consumer.ConsumeInteractionFrame(LastInteractionFrame, LastDiagnostics);

            return LastDiagnostics;
        }

        private static ArcGraphInteractionSceneAdapterDiagnostics CreateDiagnostics(
            bool hasConfig,
            bool wasViewStateProvided,
            bool hasRenderQueue,
            ArcGraphInteractionSceneFrame sceneFrame,
            bool didApplyViewController,
            ArcGraphViewControllerResult viewResult,
            bool didBuildInteractionFrame,
            bool wasConsumerProvided,
            bool didDispatchToConsumer,
            ArcGraphInteractionFrame interactionFrame,
            string reason)
        {
            return new ArcGraphInteractionSceneAdapterDiagnostics(
                hasConfig,
                wasViewStateProvided,
                hasRenderQueue,
                sceneFrame.HasValidViewport,
                sceneFrame.Input.HasPointerScreenPosition,
                sceneFrame.Input.IsPointerOverUi,
                didApplyViewController,
                didApplyViewController && viewResult.DidChangeZoom,
                didApplyViewController && viewResult.DidApplyPan,
                didBuildInteractionFrame,
                wasConsumerProvided,
                didDispatchToConsumer,
                interactionFrame.TargetKind,
                interactionFrame.ActorId,
                interactionFrame.ObjectId,
                interactionFrame.PlantId,
                interactionFrame.HasValidCell,
                sceneFrame.SourceFrameIndex,
                reason);
        }
    }
}
