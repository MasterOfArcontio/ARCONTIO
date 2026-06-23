using Arcontio.Core;
using Arcontio.Core.Environment;
using System.Globalization;

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

    // =============================================================================
    // ArcUiSimulationControlController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller autorizzato minimo per la TopBar di controllo simulazione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: TopBar -> Controller -> SimulationHost</b></para>
    /// <para>
    /// La TopBar non riceve direttamente il <c>SimulationHost</c> e non chiama i
    /// suoi metodi. Questo controller riceve richieste UI tipizzate, applica solo
    /// le operazioni gia' esposte pubblicamente dal runtime e conserva lo stato
    /// richiesto. Le velocita' normali <c>x1-x4</c> e il fast-forward debug
    /// Biosfera <c>x50/x100/x200</c> restano due percorsi separati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_simulationHost</b>: host runtime esplicitamente assegnato dall'installer.</item>
    ///   <item><b>_lastRequest</b>: ultima intenzione UI ricevuta.</item>
    ///   <item><b>_speedMultiplier</b>: velocita' richiesta dalla UI e applicata al loop tick normale.</item>
    ///   <item><b>_biosphereDebugFastForwardMultiplier</b>: fattore debug ambientale separato dalla simulazione sociale.</item>
    ///   <item><b>Request*</b>: metodi invocabili dai pulsanti TopBar.</item>
    ///   <item><b>BuildStateSnapshot</b>: snapshot letto dalla view.</item>
    /// </list>
    /// </summary>
    public sealed class ArcUiSimulationControlController
    {
        private SimulationHost _simulationHost;
        private ArcUiSimulationControlRequest _lastRequest;
        private int _speedMultiplier = 1;
        private int _biosphereDebugFastForwardMultiplier = 50;

        public bool HasRuntimeHost => _simulationHost != null;
        public ArcUiSimulationControlRequest LastRequest => _lastRequest;
        public int RequestedSpeedMultiplier => _speedMultiplier;
        public int RequestedBiosphereDebugFastForwardMultiplier => _biosphereDebugFastForwardMultiplier;

        // =============================================================================
        // SetSimulationHost
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna l'host runtime autorizzato usato per pausa e ripresa.
        /// </para>
        ///
        /// <para><b>Binding esplicito</b></para>
        /// <para>
        /// Il riferimento arriva dall'auto-installer ArcGraph, che gia' possiede il
        /// compito di collegare i componenti runtime. Il controller non cerca
        /// GameObject in scena e non usa fallback nascosti.
        /// </para>
        /// </summary>
        public void SetSimulationHost(SimulationHost host)
        {
            _simulationHost = host;

            if (_simulationHost != null)
            {
                _simulationHost.SetRuntimeTickSpeedMultiplier(_speedMultiplier);
                _simulationHost.SetBiosphereDebugFastForwardMultiplier(_biosphereDebugFastForwardMultiplier);
            }
        }

        // =============================================================================
        // RequestPause
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra e applica una richiesta di pausa.
        /// </para>
        /// </summary>
        public void RequestPause(string source)
        {
            Apply(ArcUiSimulationControlRequest.Pause(source));
        }

        // =============================================================================
        // RequestResume
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra e applica una richiesta di ripresa.
        /// </para>
        /// </summary>
        public void RequestResume(string source)
        {
            Apply(ArcUiSimulationControlRequest.Resume(source));
        }

        // =============================================================================
        // RequestSpeed
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una richiesta di velocita' simulativa.
        /// </para>
        ///
        /// <para><b>Velocita' normale applicata al loop tick</b></para>
        /// <para>
        /// Il controller applica solo i fattori semplici x1-x4. La futura modalita'
        /// debug x50 resta fuori da questo percorso perche' richiede freeze visuale
        /// e policy esplicita sui sistemi da eseguire o saltare.
        /// </para>
        /// </summary>
        public void RequestSpeed(
            int speedMultiplier,
            string source)
        {
            Apply(ArcUiSimulationControlRequest.SetSpeed(speedMultiplier, source));
        }

        // =============================================================================
        // CycleSpeed
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza ciclicamente tra i fattori x1, x2, x3 e x4.
        /// </para>
        /// </summary>
        public void CycleSpeed(string source)
        {
            int next = _speedMultiplier >= 4 ? 1 : _speedMultiplier + 1;
            RequestSpeed(next, source);
        }

        // =============================================================================
        // RequestBiosphereDebugFastForwardMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra la scelta del moltiplicatore debug Biosfera.
        /// </para>
        /// </summary>
        public void RequestBiosphereDebugFastForwardMultiplier(
            int multiplier,
            string source)
        {
            Apply(ArcUiSimulationControlRequest.SetBiosphereDebugFastForwardMultiplier(multiplier, source));
        }

        // =============================================================================
        // CycleBiosphereDebugFastForwardMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza ciclicamente tra <c>x50</c>, <c>x100</c> e <c>x200</c>.
        /// </para>
        /// </summary>
        public void CycleBiosphereDebugFastForwardMultiplier(string source)
        {
            int next = _biosphereDebugFastForwardMultiplier >= 200
                ? 50
                : _biosphereDebugFastForwardMultiplier >= 100
                    ? 200
                    : 100;
            RequestBiosphereDebugFastForwardMultiplier(next, source);
        }

        // =============================================================================
        // ToggleBiosphereDebugFastForward
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia o ferma il fast-forward debug solo Biosfera.
        /// </para>
        /// </summary>
        public void ToggleBiosphereDebugFastForward(string source)
        {
            if (_simulationHost != null && _simulationHost.IsBiosphereDebugFastForwardActive)
                Apply(ArcUiSimulationControlRequest.StopBiosphereDebugFastForward(source));
            else
                Apply(ArcUiSimulationControlRequest.StartBiosphereDebugFastForward(
                    _biosphereDebugFastForwardMultiplier,
                    source));
        }

        // =============================================================================
        // BuildStateSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce lo snapshot corrente leggibile dalla TopBar.
        /// </para>
        /// </summary>
        public ArcUiSimulationControlState BuildStateSnapshot()
        {
            bool hasHost = _simulationHost != null;
            bool isPaused = hasHost && _simulationHost.IsPaused;
            long tickIndex = hasHost ? _simulationHost.TickIndex : 0L;
            int speedMultiplier = hasHost
                ? _simulationHost.RuntimeTickSpeedMultiplier
                : _speedMultiplier;
            bool biosphereDebugActive = hasHost && _simulationHost.IsBiosphereDebugFastForwardActive;
            int biosphereDebugMultiplier = hasHost
                ? _simulationHost.BiosphereDebugFastForwardMultiplier
                : _biosphereDebugFastForwardMultiplier;
            ArcUiEnvironmentStatusSnapshot environmentStatus = BuildEnvironmentStatusSnapshot();

            return new ArcUiSimulationControlState(
                hasHost,
                isPaused,
                speedMultiplier,
                tickIndex,
                biosphereDebugActive,
                biosphereDebugMultiplier,
                environmentStatus);
        }

        // =============================================================================
        // Apply
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la richiesta UI solo se il runtime espone gia' un metodo sicuro.
        /// </para>
        /// </summary>
        private void Apply(ArcUiSimulationControlRequest request)
        {
            if (!request.IsValid)
                return;

            _lastRequest = request;

            if (request.IsSetSpeed)
            {
                _speedMultiplier = request.SpeedMultiplier;
                if (_simulationHost != null)
                    _simulationHost.SetRuntimeTickSpeedMultiplier(_speedMultiplier);

                return;
            }

            if (request.IsSetBiosphereDebugFastForwardMultiplier)
            {
                _biosphereDebugFastForwardMultiplier = request.BiosphereDebugFastForwardMultiplier;
                if (_simulationHost != null)
                    _simulationHost.SetBiosphereDebugFastForwardMultiplier(_biosphereDebugFastForwardMultiplier);

                return;
            }

            if (_simulationHost == null)
                return;

            if (request.IsStartBiosphereDebugFastForward)
            {
                _biosphereDebugFastForwardMultiplier = request.BiosphereDebugFastForwardMultiplier;
                _simulationHost.StartBiosphereDebugFastForward(_biosphereDebugFastForwardMultiplier);
                return;
            }

            if (request.IsStopBiosphereDebugFastForward)
            {
                _simulationHost.StopBiosphereDebugFastForward();
                return;
            }

            if (request.IsPause)
            {
                _simulationHost.SetPaused(true);
                return;
            }

            if (request.IsResume)
                _simulationHost.SetPaused(false);
        }

        // =============================================================================
        // BuildEnvironmentStatusSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce lo snapshot ambiente letto dalla TopBar.
        /// </para>
        ///
        /// <para><b>Principio architetturale: boundary ambiente autorizzato</b></para>
        /// <para>
        /// Solo questo controller conosce il <c>SimulationHost</c>. La UI riceve un
        /// contratto data-only con valori semplici e label, senza accedere a
        /// <c>World</c>, <c>EnvironmentState</c> o resolver della Biosfera.
        /// </para>
        /// </summary>
        private ArcUiEnvironmentStatusSnapshot BuildEnvironmentStatusSnapshot()
        {
            if (_simulationHost == null)
                return ArcUiEnvironmentStatusSnapshot.Empty();

            bool hasCalendar = _simulationHost.TryGetEnvironmentCalendarState(out EnvironmentCalendarState calendar);
            bool hasClimate = _simulationHost.TryGetEnvironmentClimateState(out EnvironmentGlobalClimateState climate);
            if (!hasCalendar && !hasClimate)
                return ArcUiEnvironmentStatusSnapshot.Empty();

            int year = 0;
            int month = 0;
            int dayOfMonth = 0;
            int dayOfYear = 0;
            int hour = 0;
            int minute = 0;
            string seasonKey = string.Empty;
            string seasonLabel = "Stagione --";
            string dayLabel = "Giorno --";
            string monthLabel = "Mese --";
            string yearLabel = "Anno ----";
            string timeLabel = "--:--";

            if (hasCalendar)
            {
                year = calendar.Date.Year;
                month = calendar.Date.Month + 1;
                dayOfMonth = calendar.Date.DayOfMonth + 1;
                dayOfYear = calendar.Date.DayOfYear + 1;
                hour = calendar.TimeOfDay.Hour;
                minute = calendar.TimeOfDay.Minute;
                seasonKey = calendar.Date.Season.ToString();
                seasonLabel = ToSeasonLabel(calendar.Date.Season);
                dayLabel = "Giorno " + (calendar.Date.DayOfMonth + 1).ToString(CultureInfo.InvariantCulture);
                monthLabel = "Mese " + (calendar.Date.Month + 1).ToString(CultureInfo.InvariantCulture);
                yearLabel = "Anno " + calendar.Date.Year.ToString(CultureInfo.InvariantCulture);
                timeLabel =
                    calendar.TimeOfDay.Hour.ToString("00", CultureInfo.InvariantCulture) +
                    ":" +
                    calendar.TimeOfDay.Minute.ToString("00", CultureInfo.InvariantCulture);
            }

            float temperature01 = 0f;
            float humidity01 = 0f;
            string weatherKey = string.Empty;
            string weatherLabel = "Meteo --";
            string temperatureLabel = "-- C";
            string humidityLabel = "-- %";

            if (hasClimate)
            {
                temperature01 = Clamp01(climate.Temperature01);
                humidity01 = Clamp01(climate.Humidity01);
                weatherKey = climate.Weather.Kind.ToString();
                temperatureLabel = Mathf01ToTemperatureCelsius(climate.Temperature01);
                humidityLabel = Mathf01ToPercent(climate.Humidity01);
                weatherLabel = "Meteo " + ToWeatherLabel(climate.Weather.Kind);
            }

            return new ArcUiEnvironmentStatusSnapshot(
                hasCalendar,
                hasClimate,
                year,
                month,
                dayOfMonth,
                dayOfYear,
                hour,
                minute,
                seasonKey,
                seasonLabel,
                temperature01,
                humidity01,
                weatherKey,
                weatherLabel,
                dayLabel,
                monthLabel,
                yearLabel,
                timeLabel,
                temperatureLabel,
                humidityLabel);
        }

        private static string ToSeasonLabel(EnvironmentSeasonKind season)
        {
            return season switch
            {
                EnvironmentSeasonKind.Summer => "Estate",
                EnvironmentSeasonKind.Autumn => "Autunno",
                EnvironmentSeasonKind.Winter => "Inverno",
                _ => "Primavera"
            };
        }

        private static string ToWeatherLabel(EnvironmentWeatherKind weather)
        {
            return weather switch
            {
                EnvironmentWeatherKind.Rain => "Pioggia",
                EnvironmentWeatherKind.Snow => "Neve",
                EnvironmentWeatherKind.Wind => "Vento",
                EnvironmentWeatherKind.HeatWave => "Caldo",
                EnvironmentWeatherKind.Storm => "Tempesta",
                _ => "Sereno"
            };
        }

        private static string Mathf01ToPercent(float value01)
        {
            int value = (int)System.Math.Round(Clamp01(value01) * 100f);
            return value.ToString(CultureInfo.InvariantCulture) + " %";
        }

        private static string Mathf01ToTemperatureCelsius(float value01)
        {
            float clamped = Clamp01(value01);
            int celsius = (int)System.Math.Round(-10f + (clamped * 45f));
            return celsius.ToString(CultureInfo.InvariantCulture) + " C";
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            return value > 1f ? 1f : value;
        }
    }
}
