namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiSelectionController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller shell per la selezione singola della UI ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato UI, non query World</b></para>
    /// <para>
    /// Il controller conserva solo il target gia' risolto da un boundary
    /// autorizzato. Non fa hit test, non legge il mondo e non apre inspector da
    /// solo. Lo step selezione colleghera' questo stato ai consumer runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_current</b>: target selezionato corrente.</item>
    ///   <item><b>Select</b>: accetta un target gia' preparato.</item>
    ///   <item><b>Clear</b>: torna allo stato senza selezione.</item>
    /// </list>
    /// </summary>
    public sealed class ArcUiSelectionController
    {
        private ArcUiSelectionTarget _current = ArcUiSelectionTarget.None("selection_controller");

        public ArcUiSelectionTarget Current => _current;

        // =============================================================================
        // Select
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra il target selezionato se e' valido.
        /// </para>
        /// </summary>
        public void Select(ArcUiSelectionTarget target)
        {
            _current = target.IsValid ? target : ArcUiSelectionTarget.None("selection_controller");
        }

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove la selezione corrente senza toccare il mondo.
        /// </para>
        /// </summary>
        public void Clear()
        {
            _current = ArcUiSelectionTarget.None("selection_controller");
        }
    }

    // =============================================================================
    // ArcUiPlacementController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller shell per la richiesta placement corrente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: intenzione sospesa</b></para>
    /// <para>
    /// Il controller puo' ricordare quale operation e' stata scelta, ma non invia
    /// comandi e non decide se la cella sia valida. Questo impedisce ai pulsanti
    /// UI di diventare scorciatoie dirette verso la simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_pending</b>: richiesta placement in preparazione.</item>
    ///   <item><b>Begin</b>: inizia una richiesta senza cella.</item>
    ///   <item><b>SetTargetCell</b>: aggiunge la cella scelta.</item>
    ///   <item><b>Cancel</b>: annulla lo stato locale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcUiPlacementController
    {
        private ArcUiPlacementRequest _pending;

        public ArcUiPlacementRequest Pending => _pending;

        // =============================================================================
        // Begin
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inizia una intenzione di placement partendo da una operation definition.
        /// </para>
        /// </summary>
        public void Begin(ArcUiOperationDefinition operation, string targetDefId)
        {
            _pending = operation.IsValid
                ? ArcUiPlacementRequest.WithoutCell(operation.OperationKey, targetDefId)
                : default;
        }

        // =============================================================================
        // SetTargetCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Completa la richiesta locale con la cella selezionata.
        /// </para>
        /// </summary>
        public void SetTargetCell(ArcGraphCellCoord cell)
        {
            if (!_pending.IsValid)
            {
                return;
            }

            _pending = new ArcUiPlacementRequest(
                _pending.OperationKey,
                cell,
                _pending.TargetDefId,
                true);
        }

        // =============================================================================
        // Cancel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Annulla la richiesta placement locale senza effetti runtime.
        /// </para>
        /// </summary>
        public void Cancel()
        {
            _pending = default;
        }
    }

    // =============================================================================
    // ArcUiInspectionController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller shell per il ViewModel inspector corrente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: inspector come consumer di ViewModel</b></para>
    /// <para>
    /// Il controller non costruisce dati da NPC, oggetti, celle o job runtime. Tiene
    /// solo l'ultimo <see cref="ArcUiInspectorViewModel"/> ricevuto da una factory
    /// futura. In questo modo il pannello inspector puo' evolvere senza ottenere
    /// accesso diretto al mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_current</b>: ViewModel inspector corrente.</item>
    ///   <item><b>Set</b>: accetta un ViewModel gia' preparato.</item>
    ///   <item><b>Clear</b>: torna allo stato inspector vuoto.</item>
    /// </list>
    /// </summary>
    public sealed class ArcUiInspectionController
    {
        private ArcUiInspectorViewModel _current = ArcUiInspectorViewModel.Empty();

        public ArcUiInspectorViewModel Current => _current;

        // =============================================================================
        // Set
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra un ViewModel inspector gia' preparato.
        /// </para>
        /// </summary>
        public void Set(ArcUiInspectorViewModel viewModel)
        {
            _current = viewModel.HasTarget || viewModel.HasTabs
                ? viewModel
                : ArcUiInspectorViewModel.Empty();
        }

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota l'inspector senza chiudere o modificare elementi runtime.
        /// </para>
        /// </summary>
        public void Clear()
        {
            _current = ArcUiInspectorViewModel.Empty();
        }
    }

    // =============================================================================
    // ArcUiViewModeController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller shell per la view mode UI corrente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservazione senza comando</b></para>
    /// <para>
    /// Il controller conserva quale modalita' visuale e' stata scelta. Non accende
    /// overlay Unity, non legge NPC e non modifica stato simulativo. Gli step futuri
    /// collegheranno questa scelta a OverlayRoot.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_current</b>: definizione view mode corrente.</item>
    ///   <item><b>Set</b>: aggiorna la modalita' se valida.</item>
    ///   <item><b>Clear</b>: torna a nessuna modalita' dedicata.</item>
    /// </list>
    /// </summary>
    public sealed class ArcUiViewModeController
    {
        private ArcUiViewModeDefinition _current;

        public ArcUiViewModeDefinition Current => _current;

        // =============================================================================
        // Set
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una view mode valida come scelta corrente.
        /// </para>
        /// </summary>
        public void Set(ArcUiViewModeDefinition definition)
        {
            _current = definition.IsValid ? definition : default;
        }

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cancella la view mode corrente senza spegnere overlay reali.
        /// </para>
        /// </summary>
        public void Clear()
        {
            _current = default;
        }
    }
}
