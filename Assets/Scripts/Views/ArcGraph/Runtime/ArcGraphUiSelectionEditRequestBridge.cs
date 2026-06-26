using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiSelectionEditRequestBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ponte non distruttivo tra la richiesta Modifica del menu selezione e la
    /// draft edit usata dai futuri controller.
    /// </para>
    ///
    /// <para><b>Principio architetturale: edit come stato preparatorio</b></para>
    /// <para>
    /// Il menu hover produce una <see cref="ArcUiSelectionActionRequest"/> di tipo
    /// Edit. Questo bridge la converte in <see cref="ArcUiEditSelectionRequest"/> e
    /// la conserva in <see cref="ArcUiEditSelectionController"/>. Non invia comandi,
    /// non legge il <c>World</c> e non consuma la request originaria, perche' il
    /// RightInspector la usa per restare aperto in modalita' modifica.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_selectionActionController</b>: sorgente della richiesta Edit pending.</item>
    ///   <item><b>_editSelectionController</b>: destinazione della draft edit.</item>
    ///   <item><b>_lastSignature</b>: evita di riscrivere la stessa draft ogni frame.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiSelectionEditRequestBridge : MonoBehaviour
    {
        [SerializeField] private bool bridgeEnabled = true;

        private ArcUiSelectionActionController _selectionActionController;
        private ArcUiEditSelectionController _editSelectionController;
        private string _lastSignature = string.Empty;

        // =============================================================================
        // SetSelectionActionController
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il bridge al controller che riceve il click Modifica.
        /// </para>
        /// </summary>
        public void SetSelectionActionController(ArcUiSelectionActionController controller)
        {
            _selectionActionController = controller;
        }

        // =============================================================================
        // SetEditSelectionController
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il bridge al controller draft edit.
        /// </para>
        /// </summary>
        public void SetEditSelectionController(ArcUiEditSelectionController controller)
        {
            _editSelectionController = controller;
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Mantiene sincronizzata la draft edit con la richiesta Edit corrente.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!bridgeEnabled || _selectionActionController == null || _editSelectionController == null)
                return;

            ArcUiSelectionActionRequest actionRequest = _selectionActionController.Pending;
            if (!actionRequest.IsEdit)
            {
                ClearDraft();
                return;
            }

            ArcUiEditSelectionRequest editRequest = ArcUiEditSelectionRequest.FromSelectionAction(actionRequest);
            if (!editRequest.IsValid)
            {
                ClearDraft();
                return;
            }

            string signature = BuildSignature(editRequest);
            if (signature == _lastSignature)
                return;

            _lastSignature = signature;
            _editSelectionController.Begin(editRequest);
        }

        // =============================================================================
        // ClearDraft
        // =============================================================================
        /// <summary>
        /// <para>
        /// Pulisce la draft quando il flusso edit non e' piu' attivo.
        /// </para>
        /// </summary>
        private void ClearDraft()
        {
            if (string.IsNullOrEmpty(_lastSignature))
                return;

            _lastSignature = string.Empty;
            _editSelectionController?.Clear();
        }

        // =============================================================================
        // BuildSignature
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una firma stabile per evitare aggiornamenti identici ogni frame.
        /// </para>
        /// </summary>
        private static string BuildSignature(ArcUiEditSelectionRequest request)
        {
            return ((int)request.Kind).ToString(System.Globalization.CultureInfo.InvariantCulture)
                   + "|"
                   + request.Target.Kind.ToString()
                   + "|"
                   + request.Target.Id
                   + "|"
                   + request.Target.Cell.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
                   + ","
                   + request.Target.Cell.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)
                   + ","
                   + request.Target.Cell.Z.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
