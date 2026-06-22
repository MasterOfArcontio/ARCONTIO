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
    // ArcUiSelectionActionController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller shell per le azioni rapide sul target selezionato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte intenzionale prima del comando</b></para>
    /// <para>
    /// Il controller riceve richieste come Modifica o Elimina dalla UI e le conserva
    /// come intenzioni pending. Non apre pannelli, non invia comandi, non cancella
    /// entita', non modifica NPC/oggetti/muri e non legge il <c>World</c>. Serve a
    /// chiudere il passaggio tra pulsanti del menu hover e futuri controller
    /// autorizzati, mantenendo il flusso verificabile ma non distruttivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_pending</b>: ultima richiesta valida ricevuta.</item>
    ///   <item><b>RequestEdit</b>: registra una intenzione di modifica.</item>
    ///   <item><b>RequestDelete</b>: registra una intenzione di eliminazione.</item>
    ///   <item><b>Clear</b>: rimuove la richiesta pending senza effetti runtime.</item>
    /// </list>
    /// </summary>
    public sealed class ArcUiSelectionActionController
    {
        private ArcUiSelectionActionRequest _pending;

        public ArcUiSelectionActionRequest Pending => _pending;
        public bool HasPending => _pending.IsValid;

        // =============================================================================
        // RequestEdit
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una richiesta di modifica sul target selezionato.
        /// </para>
        ///
        /// <para><b>Boundary UI</b></para>
        /// <para>
        /// Il metodo accetta solo un <see cref="ArcUiSelectionTarget"/> gia' risolto
        /// dal layer di selezione. Non tenta di ritrovare il target in mappa e non
        /// interroga la simulazione.
        /// </para>
        /// </summary>
        public void RequestEdit(
            ArcUiSelectionTarget target,
            string source)
        {
            SetPending(ArcUiSelectionActionRequest.Edit(target, source));
        }

        // =============================================================================
        // RequestDelete
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una richiesta di eliminazione sul target selezionato.
        /// </para>
        ///
        /// <para><b>Richiesta non distruttiva</b></para>
        /// <para>
        /// Il nome Delete descrive l'intenzione utente, non un effetto immediato.
        /// La cancellazione reale dovra' passare da conferme, controller
        /// autorizzati e gateway comando negli step successivi.
        /// </para>
        /// </summary>
        public void RequestDelete(
            ArcUiSelectionTarget target,
            string source)
        {
            SetPending(ArcUiSelectionActionRequest.Delete(target, source));
        }

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cancella la richiesta pending senza cambiare la selezione corrente.
        /// </para>
        /// </summary>
        public void Clear()
        {
            _pending = default;
        }

        private void SetPending(ArcUiSelectionActionRequest request)
        {
            _pending = request.IsValid ? request : default;
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
    ///   <item><b>SetMode</b>: cambia tra placement singolo e brush.</item>
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
            Begin(operation, targetDefId, ArcUiPlacementMode.Single);
        }

        // =============================================================================
        // Begin
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inizia una intenzione di placement indicando anche la modalita' strumento.
        /// </para>
        ///
        /// <para><b>Separazione tra catalogo e gesto utente</b></para>
        /// <para>
        /// La operation descrive cosa piazzare. La modalita' descrive come l'utente
        /// vuole raccogliere le celle, quindi puo' cambiare senza creare una seconda
        /// operation key.
        /// </para>
        /// </summary>
        public void Begin(
            ArcUiOperationDefinition operation,
            string targetDefId,
            ArcUiPlacementMode mode)
        {
            _pending = operation.IsValid
                ? ArcUiPlacementRequest.WithoutCell(
                    operation.OperationKey,
                    string.IsNullOrWhiteSpace(targetDefId) ? operation.TargetDefId : targetDefId,
                    mode)
                : default;
        }

        // =============================================================================
        // SetMode
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna la modalita' della richiesta placement corrente.
        /// </para>
        /// </summary>
        public void SetMode(ArcUiPlacementMode mode)
        {
            if (!_pending.IsValid)
            {
                return;
            }

            _pending = new ArcUiPlacementRequest(
                _pending.OperationKey,
                _pending.TargetCell,
                _pending.TargetDefId,
                mode == ArcUiPlacementMode.None ? ArcUiPlacementMode.Single : mode,
                _pending.HasTargetCell);
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
                _pending.Mode,
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
