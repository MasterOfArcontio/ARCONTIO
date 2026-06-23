namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiSimulationControlRequestKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo minimale di richiesta UI per il controllo temporale della simulazione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: intenzione temporale separata dal bottone</b></para>
    /// <para>
    /// La TopBar non deve decidere direttamente come mutare il simulatore. Questa
    /// enum descrive solo l'intenzione dell'utente: mettere in pausa, riprendere o
    /// richiedere una velocita'. Il controller autorizzato decide cosa puo' essere
    /// applicato al runtime corrente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna richiesta valida.</item>
    ///   <item><b>Pause</b>: richiesta di pausa.</item>
    ///   <item><b>Resume</b>: richiesta di ripresa.</item>
    ///   <item><b>SetSpeed</b>: richiesta di fattore velocita' UI.</item>
    /// </list>
    /// </summary>
    public enum ArcUiSimulationControlRequestKind
    {
        None = 0,
        Pause = 1,
        Resume = 2,
        SetSpeed = 3
    }

    // =============================================================================
    // ArcUiSimulationControlRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta asciutta prodotta dalla TopBar ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: richiesta UI, non comando Core</b></para>
    /// <para>
    /// La richiesta non contiene riferimenti a <c>SimulationHost</c>, <c>World</c>,
    /// scheduler o sistemi. Trasporta soltanto tipo, fattore velocita' e sorgente.
    /// Questo mantiene esplicito il passaggio TopBar -> controller autorizzato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: intenzione temporale richiesta.</item>
    ///   <item><b>SpeedMultiplier</b>: fattore normalizzato tra 1 e 4.</item>
    ///   <item><b>Source</b>: nome del componente UI che ha prodotto la richiesta.</item>
    ///   <item><b>IsValid</b>: true solo per richieste semanticamente utilizzabili.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiSimulationControlRequest
    {
        public readonly ArcUiSimulationControlRequestKind Kind;
        public readonly int SpeedMultiplier;
        public readonly string Source;

        public bool IsValid => Kind != ArcUiSimulationControlRequestKind.None;
        public bool IsPause => Kind == ArcUiSimulationControlRequestKind.Pause;
        public bool IsResume => Kind == ArcUiSimulationControlRequestKind.Resume;
        public bool IsSetSpeed => Kind == ArcUiSimulationControlRequestKind.SetSpeed;

        // =============================================================================
        // ArcUiSimulationControlRequest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una richiesta normalizzando sorgente e fattore velocita'.
        /// </para>
        /// </summary>
        public ArcUiSimulationControlRequest(
            ArcUiSimulationControlRequestKind kind,
            int speedMultiplier,
            string source)
        {
            Kind = kind;
            SpeedMultiplier = NormalizeSpeedMultiplier(speedMultiplier);
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        // =============================================================================
        // Pause
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di pausa della simulazione.
        /// </para>
        /// </summary>
        public static ArcUiSimulationControlRequest Pause(string source)
        {
            return new ArcUiSimulationControlRequest(
                ArcUiSimulationControlRequestKind.Pause,
                1,
                source);
        }

        // =============================================================================
        // Resume
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di ripresa della simulazione.
        /// </para>
        /// </summary>
        public static ArcUiSimulationControlRequest Resume(string source)
        {
            return new ArcUiSimulationControlRequest(
                ArcUiSimulationControlRequestKind.Resume,
                1,
                source);
        }

        // =============================================================================
        // SetSpeed
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta di cambio velocita' UI.
        /// </para>
        /// </summary>
        public static ArcUiSimulationControlRequest SetSpeed(
            int speedMultiplier,
            string source)
        {
            return new ArcUiSimulationControlRequest(
                ArcUiSimulationControlRequestKind.SetSpeed,
                speedMultiplier,
                source);
        }

        // =============================================================================
        // NormalizeSpeedMultiplier
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza il fattore velocita' nel range operativo iniziale x1-x4.
        /// </para>
        /// </summary>
        public static int NormalizeSpeedMultiplier(int speedMultiplier)
        {
            if (speedMultiplier < 1)
                return 1;

            return speedMultiplier > 4 ? 4 : speedMultiplier;
        }
    }

    // =============================================================================
    // ArcUiSimulationControlState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot UI del controllo temporale mostrabile dalla TopBar.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato leggibile dalla UI</b></para>
    /// <para>
    /// La TopBar deve poter aggiornare etichette e pulsanti senza interrogare il
    /// <c>SimulationHost</c>. Il controller prepara quindi uno snapshot compatto
    /// con pausa, velocita' richiesta e tick corrente se disponibile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasRuntimeHost</b>: true quando il controller ha un SimulationHost.</item>
    ///   <item><b>IsPaused</b>: stato pausa letto dal controller.</item>
    ///   <item><b>SpeedMultiplier</b>: fattore velocita' richiesto dalla UI.</item>
    ///   <item><b>TickIndex</b>: tick corrente noto, o 0 se non disponibile.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiSimulationControlState
    {
        public readonly bool HasRuntimeHost;
        public readonly bool IsPaused;
        public readonly int SpeedMultiplier;
        public readonly long TickIndex;

        public ArcUiSimulationControlState(
            bool hasRuntimeHost,
            bool isPaused,
            int speedMultiplier,
            long tickIndex)
        {
            HasRuntimeHost = hasRuntimeHost;
            IsPaused = isPaused;
            SpeedMultiplier = ArcUiSimulationControlRequest.NormalizeSpeedMultiplier(speedMultiplier);
            TickIndex = tickIndex < 0L ? 0L : tickIndex;
        }
    }
}
