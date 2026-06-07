using System;

namespace Arcontio.Core
{
    // =============================================================================
    // RunningActionLifecycleStatus
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato di ciclo vita minimale per una futura action runtime multi-tick.
    /// </para>
    ///
    /// <para><b>Principio architetturale: progresso interno separato dal World</b></para>
    /// <para>
    /// ARC-DEC-020 distingue il progresso volatile di una running action dalla
    /// mutazione oggettiva del mondo. Questa enum descrive solo il lifecycle interno
    /// dell'action: non assegna job, non emette command e non modifica posizione,
    /// inventario, reservation o altri store del <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: stato non inizializzato o snapshot vuoto.</item>
    ///   <item><b>Started</b>: action appena creata, elapsed ancora a zero.</item>
    ///   <item><b>Running</b>: action avanzata almeno una volta ma non terminale.</item>
    ///   <item><b>Completed</b>: progresso interno sufficiente per completare.</item>
    ///   <item><b>Failed</b>: action chiusa con failure reason diagnostica.</item>
    ///   <item><b>Interrupted</b>: action fermata prima del completamento finale.</item>
    /// </list>
    /// </summary>
    public enum RunningActionLifecycleStatus
    {
        None = 0,
        Started = 10,
        Running = 20,
        Completed = 30,
        Failed = 40,
        Interrupted = 50
    }

    // =============================================================================
    // RunningActionKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classificazione leggera e non esecutiva delle future running action.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tag semantico, non executor</b></para>
    /// <para>
    /// Il kind aiuta test, explainability e audit a distinguere movimento, uso di
    /// oggetti o lavoro lungo senza introdurre dipendenze dai sistemi concreti. In
    /// particolare <c>Movement</c> non chiama <c>MovementSystem</c> e non implica
    /// traversal reale: resta un'etichetta passiva.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: default difensivo.</item>
    ///   <item><b>Movement</b>: futuro attraversamento multi-tick tra celle.</item>
    ///   <item><b>Work</b>: futura produzione/lavoro su durata.</item>
    ///   <item><b>UseObject</b>: futura interazione lunga con oggetto.</item>
    ///   <item><b>Wait</b>: attesa esplicita come running action osservabile.</item>
    ///   <item><b>Sleep</b>: riposo/sonno multi-tick futuro.</item>
    ///   <item><b>Social</b>: azione sociale lunga futura.</item>
    ///   <item><b>Custom</b>: tag transitorio per domini non ancora modellati.</item>
    /// </list>
    /// </summary>
    public enum RunningActionKind
    {
        None = 0,
        Movement = 10,
        Work = 20,
        UseObject = 30,
        Wait = 40,
        Sleep = 50,
        Social = 60,
        Custom = 999
    }

    // =============================================================================
    // RunningActionCompletionPolicy
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO passivo che dichiara soglie minime di completamento, timeout e failure
    /// reason per una running action.
    /// </para>
    ///
    /// <para><b>Principio architetturale: condizioni dichiarate, non nascoste</b></para>
    /// <para>
    /// ARC-DEC-020 richiede che ogni running action dichiari timeout, failure
    /// conditions e interruption conditions. Questa struttura non valuta il mondo e
    /// non decide preemption: rappresenta solo parametri leggibili dal futuro
    /// runtime esecutivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RequiredTicks</b>: progresso necessario per completare.</item>
    ///   <item><b>TimeoutTicks</b>: limite massimo opzionale, zero significa assente.</item>
    ///   <item><b>FailureReason</b>: reason da usare se il runtime fallira'.</item>
    ///   <item><b>InterruptionReason</b>: reason da usare se il runtime interrompera'.</item>
    /// </list>
    /// </summary>
    public readonly struct RunningActionCompletionPolicy
    {
        public readonly int RequiredTicks;
        public readonly int TimeoutTicks;
        public readonly JobFailureReason FailureReason;
        public readonly JobFailureReason InterruptionReason;

        public RunningActionCompletionPolicy(
            int requiredTicks,
            int timeoutTicks,
            JobFailureReason failureReason,
            JobFailureReason interruptionReason)
        {
            RequiredTicks = Math.Max(0, requiredTicks);
            TimeoutTicks = Math.Max(0, timeoutTicks);
            FailureReason = NormalizeReason(failureReason);
            InterruptionReason = NormalizeReason(interruptionReason);
        }

        public bool CanComplete(int elapsedTicks)
        {
            // RequiredTicks pari a zero indica action completabile subito dal punto
            // di vista del progresso interno; non emette comunque alcun command.
            return Math.Max(0, elapsedTicks) >= RequiredTicks;
        }

        public bool IsTimedOut(int elapsedTicks)
        {
            // TimeoutTicks zero significa "nessun timeout dichiarato" per mantenere
            // piccolo il contratto senza introdurre nullable o config esterne.
            return TimeoutTicks > 0 && Math.Max(0, elapsedTicks) >= TimeoutTicks;
        }

        private static JobFailureReason NormalizeReason(JobFailureReason reason)
        {
            return reason == JobFailureReason.None ? JobFailureReason.Unknown : reason;
        }
    }

    // =============================================================================
    // RunningActionMovementSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Metadato tipizzato minimale per descrivere un segmento di movimento in corso.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dato visuale derivato, non authority</b></para>
    /// <para>
    /// Il Job Layer resta l'unico punto che decide e completa il movimento reale.
    /// Questa struttura conserva solo origine e destinazione del segmento corrente,
    /// usando interi primitivi per evitare dipendenze Unity e per mantenere basso il
    /// costo runtime. ArcGraph puo' leggerla per interpolare lo sprite, ma non puo'
    /// usarla per mutare la posizione simulativa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasMovementSegment</b>: indica se il metadato e' realmente presente.</item>
    ///   <item><b>FromCellX/FromCellY</b>: cella discreta di partenza.</item>
    ///   <item><b>ToCellX/ToCellY</b>: cella discreta di arrivo.</item>
    /// </list>
    /// </summary>
    public readonly struct RunningActionMovementSnapshot
    {
        public readonly bool HasMovementSegment;
        public readonly int FromCellX;
        public readonly int FromCellY;
        public readonly int ToCellX;
        public readonly int ToCellY;

        public bool IsValidStep =>
            HasMovementSegment
            && (FromCellX != ToCellX || FromCellY != ToCellY);

        // =============================================================================
        // RunningActionMovementSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il metadato read-only del segmento movimento.
        /// </para>
        ///
        /// <para><b>Principio architetturale: default struct distinguibile da movimento reale</b></para>
        /// <para>
        /// Il costruttore riceve un flag esplicito per evitare che una struttura
        /// default venga interpretata come movimento valido. Questo mantiene sicuro
        /// l'uso di value type e permette snapshot economici senza nullable.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>hasMovementSegment</b>: presenza effettiva del segmento.</item>
        ///   <item><b>fromCellX/fromCellY</b>: origine discreta.</item>
        ///   <item><b>toCellX/toCellY</b>: destinazione discreta.</item>
        /// </list>
        /// </summary>
        public RunningActionMovementSnapshot(
            bool hasMovementSegment,
            int fromCellX,
            int fromCellY,
            int toCellX,
            int toCellY)
        {
            // Il flag esplicito evita di interpretare il default struct come un
            // movimento reale da (0,0) a (0,0). Le coordinate restano primitive:
            // niente Vector2Int, niente allocazioni, niente dipendenza Unity.
            HasMovementSegment = hasMovementSegment;
            FromCellX = fromCellX;
            FromCellY = fromCellY;
            ToCellX = toCellX;
            ToCellY = toCellY;
        }

        // =============================================================================
        // Create
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un metadato di movimento presente usando coordinate primitive.
        /// </para>
        ///
        /// <para><b>Factory minimale</b></para>
        /// <para>
        /// La factory evita al chiamante di passare manualmente il flag di presenza.
        /// Non valida pathfinding, adiacenza o camminabilita': quelle authority
        /// restano nel Job Layer e nel World.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>fromCellX/fromCellY</b>: origine del segmento.</item>
        ///   <item><b>toCellX/toCellY</b>: destinazione del segmento.</item>
        /// </list>
        /// </summary>
        public static RunningActionMovementSnapshot Create(
            int fromCellX,
            int fromCellY,
            int toCellX,
            int toCellY)
        {
            return new RunningActionMovementSnapshot(
                hasMovementSegment: true,
                fromCellX,
                fromCellY,
                toCellX,
                toCellY);
        }
    }

    // =============================================================================
    // RunningActionProgressSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only del progresso volatile di una running action.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' senza authority</b></para>
    /// <para>
    /// Lo snapshot serve a test, QA e futura explainability. Copia valori primitivi
    /// e non espone riferimenti mutabili. Non e' uno stato persistente e non deve
    /// essere inserito nel savegame: dopo load il runtime dovra' ricostruire nuove
    /// intenzioni/job dalla cognizione persistita.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Identita'</b>: action, NPC, job, fase e step opzionali.</item>
    ///   <item><b>Tempo</b>: tick di start/update ed elapsed interno.</item>
    ///   <item><b>Status</b>: lifecycle corrente e terminalita'.</item>
    ///   <item><b>Policy</b>: required/timeout e failure reason correnti.</item>
    ///   <item><b>Movement</b>: eventuale segmento origine/destinazione read-only.</item>
    /// </list>
    /// </summary>
    public readonly struct RunningActionProgressSnapshot
    {
        public readonly string ActionInstanceId;
        public readonly RunningActionKind Kind;
        public readonly int NpcId;
        public readonly string JobId;
        public readonly string PhaseId;
        public readonly string JobActionId;
        public readonly int StartedTick;
        public readonly int UpdatedTick;
        public readonly int ElapsedTicks;
        public readonly int RequiredTicks;
        public readonly int TimeoutTicks;
        public readonly RunningActionLifecycleStatus Status;
        public readonly JobFailureReason FailureReason;
        public readonly bool IsTerminal;
        public readonly bool CanComplete;
        public readonly bool IsTimedOut;
        public readonly RunningActionMovementSnapshot Movement;

        public RunningActionProgressSnapshot(
            string actionInstanceId,
            RunningActionKind kind,
            int npcId,
            string jobId,
            string phaseId,
            string jobActionId,
            int startedTick,
            int updatedTick,
            int elapsedTicks,
            int requiredTicks,
            int timeoutTicks,
            RunningActionLifecycleStatus status,
            JobFailureReason failureReason,
            bool isTerminal,
            bool canComplete,
            bool isTimedOut,
            RunningActionMovementSnapshot movement)
        {
            ActionInstanceId = actionInstanceId ?? string.Empty;
            Kind = kind;
            NpcId = npcId;
            JobId = jobId ?? string.Empty;
            PhaseId = phaseId ?? string.Empty;
            JobActionId = jobActionId ?? string.Empty;
            StartedTick = Math.Max(0, startedTick);
            UpdatedTick = Math.Max(0, updatedTick);
            ElapsedTicks = Math.Max(0, elapsedTicks);
            RequiredTicks = Math.Max(0, requiredTicks);
            TimeoutTicks = Math.Max(0, timeoutTicks);
            Status = status;
            FailureReason = failureReason;
            IsTerminal = isTerminal;
            CanComplete = canComplete;
            IsTimedOut = isTimedOut;
            Movement = movement;
        }
    }

    // =============================================================================
    // RunningActionRuntimeState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Skeleton passivo del futuro stato runtime volatile per action multi-tick.
    /// </para>
    ///
    /// <para><b>Principio architetturale: no-op temporal foundation</b></para>
    /// <para>
    /// Questo tipo prepara il lessico tecnico richiesto da ARC-DEC-020 senza
    /// cablarlo nel runtime produttivo. Conserva solo progresso interno, lifecycle
    /// e reason diagnostiche. Non conosce <c>World</c>, non dipende da
    /// <c>MovementSystem</c>, non accoda <c>ICommand</c>, non assegna job, non
    /// arbitra preemption e non entra nel save/load.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Identity</b>: action instance, NPC, job, phase e action id.</item>
    ///   <item><b>Policy</b>: soglie dichiarative di completamento e timeout.</item>
    ///   <item><b>Progress</b>: elapsed volatile aggiornabile solo internamente.</item>
    ///   <item><b>Lifecycle</b>: started/running/completed/failed/interrupted.</item>
    ///   <item><b>Snapshot</b>: proiezione read-only per QA e futura EL.</item>
    /// </list>
    /// </summary>
    public sealed class RunningActionRuntimeState
    {
        public string ActionInstanceId { get; }
        public RunningActionKind Kind { get; }
        public int NpcId { get; }
        public string JobId { get; }
        public string PhaseId { get; }
        public string JobActionId { get; }
        public int StartedTick { get; }
        public int UpdatedTick { get; private set; }
        public int ElapsedTicks { get; private set; }
        public RunningActionCompletionPolicy CompletionPolicy { get; }
        public RunningActionMovementSnapshot Movement { get; }
        public RunningActionLifecycleStatus Status { get; private set; }
        public JobFailureReason FailureReason { get; private set; }

        public bool IsTerminal =>
            Status == RunningActionLifecycleStatus.Completed
            || Status == RunningActionLifecycleStatus.Failed
            || Status == RunningActionLifecycleStatus.Interrupted;

        public bool CanComplete => CompletionPolicy.CanComplete(ElapsedTicks);
        public bool IsTimedOut => CompletionPolicy.IsTimedOut(ElapsedTicks);

        private RunningActionRuntimeState(
            string actionInstanceId,
            RunningActionKind kind,
            int npcId,
            string jobId,
            string phaseId,
            string jobActionId,
            int startedTick,
            RunningActionCompletionPolicy completionPolicy,
            RunningActionMovementSnapshot movement)
        {
            ActionInstanceId = string.IsNullOrWhiteSpace(actionInstanceId)
                ? Guid.NewGuid().ToString("N")
                : actionInstanceId;
            Kind = kind;
            NpcId = Math.Max(0, npcId);
            JobId = jobId ?? string.Empty;
            PhaseId = phaseId ?? string.Empty;
            JobActionId = jobActionId ?? string.Empty;
            StartedTick = Math.Max(0, startedTick);
            UpdatedTick = StartedTick;
            ElapsedTicks = 0;
            CompletionPolicy = completionPolicy;
            Movement = movement;
            Status = RunningActionLifecycleStatus.Started;
            FailureReason = JobFailureReason.None;
        }

        public static RunningActionRuntimeState Start(
            string actionInstanceId,
            RunningActionKind kind,
            int npcId,
            string jobId,
            string phaseId,
            string jobActionId,
            int startedTick,
            RunningActionCompletionPolicy completionPolicy)
        {
            // Factory esplicita: la nascita di una running action e' un evento di
            // stato interno, non una mutazione World e non un'assegnazione job.
            return new RunningActionRuntimeState(
                actionInstanceId,
                kind,
                npcId,
                jobId,
                phaseId,
                jobActionId,
                startedTick,
                completionPolicy,
                default);
        }

        // =============================================================================
        // StartMovement
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia una running action di movimento con segmento origine/destinazione
        /// tipizzato.
        /// </para>
        ///
        /// <para><b>Principio architetturale: movement metadata sotto authority Job</b></para>
        /// <para>
        /// Il metodo nasce per il driver di movimento del Job Layer. Imposta
        /// <c>RunningActionKind.Movement</c> e conserva il segmento in un value type,
        /// evitando parsing dell'id azione e impedendo alla view di inventare il
        /// moto. La factory non muta il <c>World</c> e non completa il job.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Identita'</b>: action, NPC, job, phase e job action.</item>
        ///   <item><b>Policy</b>: durata e reason dichiarative.</item>
        ///   <item><b>Segmento</b>: coordinate from/to del passo corrente.</item>
        /// </list>
        /// </summary>
        public static RunningActionRuntimeState StartMovement(
            string actionInstanceId,
            int npcId,
            string jobId,
            string phaseId,
            string jobActionId,
            int startedTick,
            RunningActionCompletionPolicy completionPolicy,
            int fromCellX,
            int fromCellY,
            int toCellX,
            int toCellY)
        {
            // Factory dedicata al movimento: il chiamante non deve ricordarsi di
            // impostare a mano Kind=Movement e non deve serializzare from/to dentro
            // stringhe diagnostiche. Il dato resta tipizzato e value-only.
            return new RunningActionRuntimeState(
                actionInstanceId,
                RunningActionKind.Movement,
                npcId,
                jobId,
                phaseId,
                jobActionId,
                startedTick,
                completionPolicy,
                RunningActionMovementSnapshot.Create(
                    fromCellX,
                    fromCellY,
                    toCellX,
                    toCellY));
        }

        public bool AdvanceProgress(int deltaTicks, int tick)
        {
            // L'avanzamento non fa altro che aumentare elapsed e aggiornare lo
            // status a Running. Se l'action e' terminale, il metodo rifiuta la
            // mutazione interna per evitare progress post-completion.
            if (IsTerminal || deltaTicks <= 0)
                return false;

            ElapsedTicks += deltaTicks;
            UpdatedTick = Math.Max(UpdatedTick, tick);

            if (Status == RunningActionLifecycleStatus.Started)
                Status = RunningActionLifecycleStatus.Running;

            return true;
        }

        public bool TryMarkCompleted(int tick)
        {
            // Completare richiede solo la policy interna. Il metodo non emette il
            // command finale: quel passaggio apparterra' al futuro execution layer.
            if (IsTerminal || !CanComplete)
                return false;

            Status = RunningActionLifecycleStatus.Completed;
            UpdatedTick = Math.Max(UpdatedTick, tick);
            FailureReason = JobFailureReason.None;
            return true;
        }

        public bool MarkFailed(JobFailureReason reason, int tick)
        {
            // Failure resta diagnostica e locale allo skeleton. Non rilascia
            // reservation, non chiude job e non produce fallback cognitivo.
            if (IsTerminal)
                return false;

            Status = RunningActionLifecycleStatus.Failed;
            UpdatedTick = Math.Max(UpdatedTick, tick);
            FailureReason = reason == JobFailureReason.None ? CompletionPolicy.FailureReason : reason;
            return true;
        }

        public bool Interrupt(JobFailureReason reason, int tick)
        {
            // Interruption e' separata dalla preemption. Questo metodo registra solo
            // che l'action e' stata fermata; JobArbiter/JobRuntimeState restano le
            // future authority per decidere se e quando interrompere davvero.
            if (IsTerminal)
                return false;

            Status = RunningActionLifecycleStatus.Interrupted;
            UpdatedTick = Math.Max(UpdatedTick, tick);
            FailureReason = reason == JobFailureReason.None ? CompletionPolicy.InterruptionReason : reason;
            return true;
        }

        public RunningActionProgressSnapshot ToSnapshot()
        {
            // Snapshot difensivo per test e futura explainability: nessun riferimento
            // mutabile, nessun accesso al World, nessuna serializzazione implicita.
            return new RunningActionProgressSnapshot(
                ActionInstanceId,
                Kind,
                NpcId,
                JobId,
                PhaseId,
                JobActionId,
                StartedTick,
                UpdatedTick,
                ElapsedTicks,
                CompletionPolicy.RequiredTicks,
                CompletionPolicy.TimeoutTicks,
                Status,
                FailureReason,
                IsTerminal,
                CanComplete,
                IsTimedOut,
                Movement);
        }
    }
}
