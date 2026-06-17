namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentRuntimeAdvanceReport
    // =============================================================================
    /// <summary>
    /// <para>
    /// Report dell'ultimo avanzamento della biosfera agganciata al runtime
    /// simulativo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica senza ownership esterna</b></para>
    /// <para>
    /// Il report permette a <c>SimulationHost</c>, debug panel e futuri harness di
    /// capire cosa e' cambiato senza leggere log globali o interrogare direttamente
    /// lo stato mutabile. Il dato resta una fotografia dell'ultimo step, non un
    /// sistema attivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PreviousEnvironmentTicks</b>: tick ambientale prima dello step.</item>
    ///   <item><b>CurrentEnvironmentTicks</b>: tick ambientale dopo lo step.</item>
    ///   <item><b>Advanced</b>: indica se lo step ha realmente mosso il tempo.</item>
    ///   <item><b>LastAdvance</b>: risultato dei resolver calendario/clima/area.</item>
    ///   <item><b>LastGrowthReport</b>: diagnostica del ciclo naturale giornaliero.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentRuntimeAdvanceReport
    {
        public long PreviousEnvironmentTicks { get; }
        public long CurrentEnvironmentTicks { get; }
        public bool Advanced { get; }
        public EnvironmentAdvanceResult LastAdvance { get; }
        public EnvironmentNaturalGrowthReport LastGrowthReport { get; }

        public bool DayChanged =>
            LastAdvance != null
            && LastAdvance.Transition.DayChanged;

        public bool GrowthChanged => LastGrowthReport.HasChanges;

        // =============================================================================
        // EnvironmentRuntimeAdvanceReport
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il report normalizzando tick negativi e conservando i risultati
        /// Core prodotti dai resolver.
        /// </para>
        /// </summary>
        public EnvironmentRuntimeAdvanceReport(
            long previousEnvironmentTicks,
            long currentEnvironmentTicks,
            bool advanced,
            EnvironmentAdvanceResult lastAdvance,
            EnvironmentNaturalGrowthReport lastGrowthReport)
        {
            PreviousEnvironmentTicks = previousEnvironmentTicks < 0
                ? 0
                : previousEnvironmentTicks;
            CurrentEnvironmentTicks = currentEnvironmentTicks < 0
                ? 0
                : currentEnvironmentTicks;
            Advanced = advanced;
            LastAdvance = lastAdvance;
            LastGrowthReport = lastGrowthReport;
        }
    }

    // =============================================================================
    // EnvironmentRuntimeSystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Runtime data-only della biosfera collegabile al tick ufficiale del
    /// simulatore.
    /// </para>
    ///
    /// <para><b>Principio architetturale: integrazione sottile con SimulationHost</b></para>
    /// <para>
    /// Questa classe non e' un <c>MonoBehaviour</c>, non entra nello scheduler NPC,
    /// non accede a <c>World</c> e non conosce ArcGraph. Riceve configurazioni gia'
    /// caricate, mantiene lo stato ambientale Core e produce snapshot read-only che
    /// altri sistemi potranno consultare in modo esplicito.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Bootstrap</b>: costruisce stato, cataloghi e snapshot iniziali.</item>
    ///   <item><b>AdvanceToEnvironmentTicks</b>: allinea la biosfera al tick simulativo richiesto.</item>
    ///   <item><b>RunNaturalGrowthIfNeeded</b>: applica la crescita solo sui confini giornalieri.</item>
    ///   <item><b>FullSnapshot</b>: read model aggregato per ArcGraph/NPC/debug futuri.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentRuntimeSystem
    {
        private EnvironmentFoundationConfig _config;
        private EnvironmentState _state;
        private EnvironmentSnapshot _snapshot;
        private EnvironmentFullSnapshot _fullSnapshot;
        private EnvironmentPlantCatalog _plantCatalog;
        private EnvironmentNaturalGrowthConfig _naturalGrowthConfig;
        private EnvironmentBiomeProfile _biomeProfile = EnvironmentBiomeProfile.Default;
        private EnvironmentFoundationBootstrapResult _bootstrap;
        private EnvironmentRuntimeAdvanceReport _lastReport;

        public bool IsBootstrapped => _state != null && _snapshot != null;
        public EnvironmentFoundationConfig Config => _config;
        public EnvironmentFoundationBootstrapResult BootstrapResult => _bootstrap;
        public EnvironmentSnapshot Snapshot => _snapshot ?? new EnvironmentState().CreateSnapshot();
        public EnvironmentFullSnapshot FullSnapshot =>
            _fullSnapshot ?? EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(Snapshot);
        public EnvironmentRuntimeAdvanceReport LastReport =>
            _lastReport ?? CreateIdleReport(CurrentEnvironmentTicks);
        public long CurrentEnvironmentTicks => Snapshot.Calendar.ElapsedEnvironmentTicks;

        // =============================================================================
        // Bootstrap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inizializza la biosfera runtime usando configurazioni e cataloghi gia'
        /// risolti dal bordo Unity.
        /// </para>
        /// </summary>
        public EnvironmentFoundationBootstrapResult Bootstrap(
            EnvironmentFoundationConfig config,
            EnvironmentPlantCatalog plantCatalog = null,
            EnvironmentNaturalGrowthConfig naturalGrowthConfig = null,
            EnvironmentBiomeProfile biomeProfile = default)
        {
            // Il runtime conserva la config radice per poter fare reset coerenti
            // dopo load snapshot o futuri cambi di mondo senza richiedere un nuovo
            // accesso ai file.
            _config = config ?? EnvironmentFoundationBootstrap.CreateDefaultConfig();
            _bootstrap = EnvironmentFoundationBootstrap.Bootstrap(_config);

            // Lo stato iniziale arriva dal builder Core; se la config e' parziale,
            // il bootstrap ha gia' applicato fallback robusti e diagnostica.
            _state = _bootstrap.Build.State ?? new EnvironmentState();
            _snapshot = _state.CreateSnapshot();

            // Il catalogo piante esterno ha precedenza sul catalogo embedded della
            // foundation: cosi' la produzione puo' usare environment_plants.json
            // senza duplicare tutte le specie dentro il file radice.
            _plantCatalog = plantCatalog
                            ?? _bootstrap.PlantCatalog
                            ?? new EnvironmentPlantCatalogConfig().ToCatalog();
            _naturalGrowthConfig = naturalGrowthConfig ?? new EnvironmentNaturalGrowthConfig();
            _biomeProfile = biomeProfile.IsValid
                ? biomeProfile
                : EnvironmentBiomeProfile.Default;

            _fullSnapshot = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(_snapshot);
            _lastReport = CreateIdleReport(_snapshot.Calendar.ElapsedEnvironmentTicks);
            return _bootstrap;
        }

        // =============================================================================
        // AdvanceToEnvironmentTicks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza la biosfera fino al tick ambientale assoluto richiesto.
        /// </para>
        /// </summary>
        public EnvironmentRuntimeAdvanceReport AdvanceToEnvironmentTicks(
            long targetEnvironmentTicks)
        {
            EnsureBootstrapped();

            long previousTicks = CurrentEnvironmentTicks;
            long safeTargetTicks = targetEnvironmentTicks < 0
                ? 0
                : targetEnvironmentTicks;
            if (safeTargetTicks <= previousTicks)
            {
                _lastReport = CreateIdleReport(previousTicks);
                return _lastReport;
            }

            // Il resolver Core aggiorna calendario, clima e layer area-based in un
            // passaggio puro. Il runtime si limita ad adottare il nuovo stato.
            var advance = EnvironmentAdvanceResolver.AdvanceStateSnapshot(
                _state,
                safeTargetTicks,
                _config.calendar,
                _config.climate,
                _biomeProfile);
            _state = advance.State;
            _snapshot = advance.Snapshot;

            EnvironmentNaturalGrowthReport growthReport = default;
            if (advance.Transition.DayChanged)
                growthReport = RunNaturalGrowth(advance);

            _fullSnapshot = EnvironmentReadOnlySnapshotResolver.BuildFullSnapshot(_snapshot);
            _lastReport = new EnvironmentRuntimeAdvanceReport(
                previousTicks,
                CurrentEnvironmentTicks,
                true,
                advance,
                growthReport);
            return _lastReport;
        }

        // =============================================================================
        // RunNaturalGrowth
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il ciclo naturale quando l'avanzamento ha attraversato almeno un
        /// confine giornaliero.
        /// </para>
        /// </summary>
        private EnvironmentNaturalGrowthReport RunNaturalGrowth(
            EnvironmentAdvanceResult advance)
        {
            // La crescita naturale lavora sullo snapshot appena avanzato: in questo
            // modo usa clima, stagione e aree gia' allineati al tick corrente.
            var growth = EnvironmentNaturalGrowthResolver.Evolve(
                _snapshot,
                _plantCatalog,
                advance.Transition,
                advance.Climate,
                advance.SeasonProfile,
                _naturalGrowthConfig,
                _biomeProfile);

            // Il resolver restituisce un nuovo stato completo; adottarlo qui mantiene
            // il runtime come unico proprietario dello stato ambientale vivo.
            _state = growth.State ?? _state;
            _snapshot = _state.CreateSnapshot();
            return growth.Report;
        }

        // =============================================================================
        // EnsureBootstrapped
        // =============================================================================
        /// <summary>
        /// <para>
        /// Garantisce che il runtime abbia uno stato iniziale prima di avanzare.
        /// </para>
        /// </summary>
        private void EnsureBootstrapped()
        {
            if (IsBootstrapped)
                return;

            // Fallback conservativo: se il chiamante usa Advance prima del bootstrap,
            // creiamo una foundation vuota ma valida. SimulationHost normalmente non
            // passa da qui perche' inizializza esplicitamente il runtime in Awake.
            Bootstrap(EnvironmentFoundationBootstrap.CreateDefaultConfig());
        }

        private static EnvironmentRuntimeAdvanceReport CreateIdleReport(long ticks)
        {
            long safeTicks = ticks < 0 ? 0 : ticks;
            return new EnvironmentRuntimeAdvanceReport(
                safeTicks,
                safeTicks,
                false,
                null,
                default);
        }
    }
}
