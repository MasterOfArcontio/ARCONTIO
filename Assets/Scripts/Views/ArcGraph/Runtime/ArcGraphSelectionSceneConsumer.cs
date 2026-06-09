using SocialViewer.UI;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphSelectionSceneConsumerDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del consumer selection ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: selection osservabile e non implicita</b></para>
    /// <para>
    /// La selection e' un effetto view-side condiviso. Questa diagnostica rende
    /// esplicito se il frame e' stato ricevuto, se c'era click primario, se la UI ha
    /// bloccato il puntatore e se un actor e' stato realmente selezionato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DidReceiveFrame</b>: il consumer ha ricevuto un frame.</item>
    ///   <item><b>SelectionEnabled</b>: gate locale abilitato.</item>
    ///   <item><b>WasPrimaryClick</b>: click primario presente nel frame.</item>
    ///   <item><b>DidSelectActor</b>: selection aggiornata su actor valido.</item>
    ///   <item><b>Reason</b>: motivo sintetico dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphSelectionSceneConsumerDiagnostics
    {
        public readonly bool DidReceiveFrame;
        public readonly bool SelectionEnabled;
        public readonly bool WasPrimaryClick;
        public readonly bool WasPointerOverUi;
        public readonly bool DidSelectActor;
        public readonly ArcGraphInteractionTargetKind TargetKind;
        public readonly int ActorId;
        public readonly int SelectedNpcId;
        public readonly string Reason;

        public ArcGraphSelectionSceneConsumerDiagnostics(
            bool didReceiveFrame,
            bool selectionEnabled,
            bool wasPrimaryClick,
            bool wasPointerOverUi,
            bool didSelectActor,
            ArcGraphInteractionTargetKind targetKind,
            int actorId,
            int selectedNpcId,
            string reason)
        {
            DidReceiveFrame = didReceiveFrame;
            SelectionEnabled = selectionEnabled;
            WasPrimaryClick = wasPrimaryClick;
            WasPointerOverUi = wasPointerOverUi;
            DidSelectActor = didSelectActor;
            TargetKind = targetKind;
            ActorId = actorId > 0 ? actorId : -1;
            SelectedNpcId = selectedNpcId > 0 ? selectedNpcId : -1;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphSelectionSceneConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Consumer scena che collega il picking actor ArcGraph al servizio
    /// <c>NPCSelection</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: selection come consumer, non come renderer</b></para>
    /// <para>
    /// Il componente riceve un <c>ArcGraphInteractionFrame</c> gia' costruito dal
    /// boundary ArcGraph. Se il frame contiene un click primario su actor valido,
    /// aggiorna il servizio view-side <c>NPCSelection</c>. Non legge mouse fisico,
    /// non usa camera, non interroga il mondo, non cerca <c>MapGridWorldView</c>, non
    /// invia comandi e non apre DevTools.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>selectionEnabled</b>: gate principale, falso di default.</item>
    ///   <item><b>ConsumeInteractionFrame</b>: applica selection solo su click actor.</item>
    ///   <item><b>SetSelectionEnabled</b>: attivazione esplicita da test/wiring.</item>
    ///   <item><b>LastDiagnostics</b>: esito leggibile dell'ultimo frame.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphSelectionSceneConsumer : MonoBehaviour, IArcGraphInteractionFrameConsumer
    {
        [SerializeField] private bool selectionEnabled;
        [SerializeField] private bool logSelectionEvents;

        private ArcGraphSelectionSceneConsumerDiagnostics _lastDiagnostics =
            new ArcGraphSelectionSceneConsumerDiagnostics(
                false,
                false,
                false,
                false,
                false,
                ArcGraphInteractionTargetKind.None,
                -1,
                NPCSelection.SelectedNpcId,
                "NotInitialized");

        public ArcGraphSelectionSceneConsumerDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool SelectionEnabled => selectionEnabled;

        // =============================================================================
        // ConsumeInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Consuma il frame interattivo e aggiorna la selection solo quando il frame
        /// rappresenta un click primario su actor.
        /// </para>
        ///
        /// <para><b>Semantica coerente con MapGrid legacy</b></para>
        /// <para>
        /// Il comportamento legacy seleziona un NPC quando il left click cade su un
        /// NPC visibile e non fa nulla su celle vuote. Questo consumer mantiene lo
        /// stesso default: niente selection da hover e niente clear automatico.
        /// </para>
        /// </summary>
        public void ConsumeInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
        {
            if (!selectionEnabled)
            {
                StoreDiagnostics(interactionFrame, false, "SelectionDisabled");
                return;
            }

            if (interactionFrame.IsPointerOverUi)
            {
                StoreDiagnostics(interactionFrame, false, "PointerOverUi");
                return;
            }

            if (!interactionFrame.Input.IsPrimaryPointerPressedThisFrame)
            {
                StoreDiagnostics(interactionFrame, false, "WaitingForPrimaryClick");
                return;
            }

            if (interactionFrame.TargetKind != ArcGraphInteractionTargetKind.Actor || !interactionFrame.HasActor)
            {
                StoreDiagnostics(interactionFrame, false, "PrimaryClickWithoutActor");
                return;
            }

            NPCSelection.Select(interactionFrame.ActorId);
            StoreDiagnostics(interactionFrame, true, "ActorSelected");

            if (logSelectionEvents)
            {
                Debug.Log(
                    "[ArcGraphSelectionSceneConsumer] selectedNpc=" +
                    interactionFrame.ActorId +
                    ", reason=" + _lastDiagnostics.Reason);
            }
        }

        // =============================================================================
        // SetSelectionEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita la selection da frame ArcGraph.
        /// </para>
        /// </summary>
        public void SetSelectionEnabled(bool enabled)
        {
            selectionEnabled = enabled;
        }

        // =============================================================================
        // EnableSelectionFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per abilitare la selection ArcGraph durante i test.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Enable Selection Consumer")]
        public void EnableSelectionFromInspector()
        {
            SetSelectionEnabled(true);
        }

        // =============================================================================
        // DisableSelectionFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per disabilitare la selection ArcGraph durante i test.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Disable Selection Consumer")]
        public void DisableSelectionFromInspector()
        {
            SetSelectionEnabled(false);
        }

        private void StoreDiagnostics(
            ArcGraphInteractionFrame interactionFrame,
            bool didSelectActor,
            string reason)
        {
            _lastDiagnostics = new ArcGraphSelectionSceneConsumerDiagnostics(
                true,
                selectionEnabled,
                interactionFrame.Input.IsPrimaryPointerPressedThisFrame,
                interactionFrame.IsPointerOverUi,
                didSelectActor,
                interactionFrame.TargetKind,
                interactionFrame.ActorId,
                NPCSelection.SelectedNpcId,
                reason);
        }
    }
}
