using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionConsumerRouterDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del router consumer interattivi ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: fan-out osservabile</b></para>
    /// <para>
    /// Il router deve poter inoltrare lo stesso frame a piu' consumer senza
    /// nascondere quanti moduli sono stati chiamati, quanti erano nulli e quanti non
    /// implementavano l'interfaccia richiesta. Questa diagnostica evita che il
    /// collegamento UI diventi una scatola nera.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DidReceiveFrame</b>: il router ha ricevuto un frame.</item>
    ///   <item><b>RouterEnabled</b>: gate principale attivo.</item>
    ///   <item><b>CandidateConsumerCount</b>: consumer ispezionati.</item>
    ///   <item><b>DispatchedConsumerCount</b>: consumer chiamati.</item>
    ///   <item><b>SkippedConsumerCount</b>: consumer ignorati.</item>
    ///   <item><b>Reason</b>: motivo sintetico dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphInteractionConsumerRouterDiagnostics
    {
        public readonly bool DidReceiveFrame;
        public readonly bool RouterEnabled;
        public readonly int CandidateConsumerCount;
        public readonly int DispatchedConsumerCount;
        public readonly int SkippedConsumerCount;
        public readonly ArcGraphInteractionTargetKind TargetKind;
        public readonly int ActorId;
        public readonly string Reason;

        public ArcGraphInteractionConsumerRouterDiagnostics(
            bool didReceiveFrame,
            bool routerEnabled,
            int candidateConsumerCount,
            int dispatchedConsumerCount,
            int skippedConsumerCount,
            ArcGraphInteractionTargetKind targetKind,
            int actorId,
            string reason)
        {
            DidReceiveFrame = didReceiveFrame;
            RouterEnabled = routerEnabled;
            CandidateConsumerCount = candidateConsumerCount < 0 ? 0 : candidateConsumerCount;
            DispatchedConsumerCount = dispatchedConsumerCount < 0 ? 0 : dispatchedConsumerCount;
            SkippedConsumerCount = skippedConsumerCount < 0 ? 0 : skippedConsumerCount;
            TargetKind = targetKind;
            ActorId = actorId > 0 ? actorId : -1;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphInteractionConsumerRouter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Consumer composito che inoltra un frame interattivo ArcGraph a piu' consumer
    /// modulari.
    /// </para>
    ///
    /// <para><b>Principio architetturale: wrapper singolo, moduli multipli</b></para>
    /// <para>
    /// Il wrapper scena puo' dispatchare a un solo
    /// <c>IArcGraphInteractionFrameConsumer</c>. Questo router consente di collegare
    /// piu' moduli, per esempio Pointer HUD e Selection, senza far dipendere quei
    /// moduli tra loro e senza mettere logica di tool dentro il wrapper.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>routerEnabled</b>: gate principale, falso di default.</item>
    ///   <item><b>consumerBehaviours</b>: consumer assegnabili da Inspector.</item>
    ///   <item><b>SetRuntimeConsumers</b>: consumer assegnabili da codice.</item>
    ///   <item><b>ConsumeInteractionFrame</b>: fan-out del frame.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphInteractionConsumerRouter : MonoBehaviour, IArcGraphInteractionFrameConsumer
    {
        [SerializeField] private bool routerEnabled;
        [SerializeField] private MonoBehaviour[] consumerBehaviours;

        private IArcGraphInteractionFrameConsumer[] _runtimeConsumers;
        private ArcGraphInteractionConsumerRouterDiagnostics _lastDiagnostics =
            new ArcGraphInteractionConsumerRouterDiagnostics(
                false,
                false,
                0,
                0,
                0,
                ArcGraphInteractionTargetKind.None,
                -1,
                "NotInitialized");

        public ArcGraphInteractionConsumerRouterDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool RouterEnabled => routerEnabled;

        // =============================================================================
        // ConsumeInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inoltra il frame interattivo a tutti i consumer validi.
        /// </para>
        ///
        /// <para><b>Fan-out senza decisione operativa</b></para>
        /// <para>
        /// Il router non interpreta click, non seleziona NPC, non mostra HUD e non
        /// invia comandi. Decide solo quali consumer espliciti ricevono il frame.
        /// Ogni consumer resta responsabile della propria semantica.
        /// </para>
        /// </summary>
        public void ConsumeInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
        {
            if (!routerEnabled)
            {
                StoreDiagnostics(interactionFrame, 0, 0, 0, "RouterDisabled");
                return;
            }

            int candidateCount = 0;
            int dispatchedCount = 0;
            int skippedCount = 0;

            DispatchRuntimeConsumers(interactionFrame, diagnostics, ref candidateCount, ref dispatchedCount, ref skippedCount);
            DispatchBehaviourConsumers(interactionFrame, diagnostics, ref candidateCount, ref dispatchedCount, ref skippedCount);

            string reason = dispatchedCount > 0 ? "FrameDispatched" : "NoConsumerDispatched";
            StoreDiagnostics(interactionFrame, candidateCount, dispatchedCount, skippedCount, reason);
        }

        // =============================================================================
        // SetRouterEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il router.
        /// </para>
        /// </summary>
        public void SetRouterEnabled(bool enabled)
        {
            routerEnabled = enabled;
        }

        // =============================================================================
        // SetRuntimeConsumers
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta consumer runtime da codice, senza passare da Inspector.
        /// </para>
        /// </summary>
        public void SetRuntimeConsumers(params IArcGraphInteractionFrameConsumer[] consumers)
        {
            _runtimeConsumers = consumers;
        }

        // =============================================================================
        // EnableRouterFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per abilitare il router durante i test.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Enable Interaction Consumer Router")]
        public void EnableRouterFromInspector()
        {
            SetRouterEnabled(true);
        }

        // =============================================================================
        // DisableRouterFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per disabilitare il router durante i test.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Disable Interaction Consumer Router")]
        public void DisableRouterFromInspector()
        {
            SetRouterEnabled(false);
        }

        private void DispatchRuntimeConsumers(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics,
            ref int candidateCount,
            ref int dispatchedCount,
            ref int skippedCount)
        {
            if (_runtimeConsumers == null)
                return;

            for (int i = 0; i < _runtimeConsumers.Length; i++)
            {
                candidateCount++;
                IArcGraphInteractionFrameConsumer consumer = _runtimeConsumers[i];
                DispatchConsumer(consumer, interactionFrame, diagnostics, ref dispatchedCount, ref skippedCount);
            }
        }

        private void DispatchBehaviourConsumers(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics,
            ref int candidateCount,
            ref int dispatchedCount,
            ref int skippedCount)
        {
            if (consumerBehaviours == null)
                return;

            for (int i = 0; i < consumerBehaviours.Length; i++)
            {
                candidateCount++;
                IArcGraphInteractionFrameConsumer consumer = consumerBehaviours[i] as IArcGraphInteractionFrameConsumer;
                DispatchConsumer(consumer, interactionFrame, diagnostics, ref dispatchedCount, ref skippedCount);
            }
        }

        private static void DispatchConsumer(
            IArcGraphInteractionFrameConsumer consumer,
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics,
            ref int dispatchedCount,
            ref int skippedCount)
        {
            if (consumer == null)
            {
                skippedCount++;
                return;
            }

            consumer.ConsumeInteractionFrame(interactionFrame, diagnostics);
            dispatchedCount++;
        }

        private void StoreDiagnostics(
            ArcGraphInteractionFrame interactionFrame,
            int candidateCount,
            int dispatchedCount,
            int skippedCount,
            string reason)
        {
            _lastDiagnostics = new ArcGraphInteractionConsumerRouterDiagnostics(
                true,
                routerEnabled,
                candidateCount,
                dispatchedCount,
                skippedCount,
                interactionFrame.TargetKind,
                interactionFrame.ActorId,
                reason);
        }
    }
}
