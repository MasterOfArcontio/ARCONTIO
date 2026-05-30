using System;

namespace Arcontio.Core
{
    // =============================================================================
    // NpcJobState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato operativo per-NPC usato dal Job System per ricordare quale job e quale
    /// step sono attualmente attivi.
    /// </para>
    ///
    /// <para><b>Stato per-NPC invece di accesso globale implicito</b></para>
    /// <para>
    /// La simulazione deve poter chiedere "cosa sta facendo questo NPC" senza
    /// cercare in liste globali non strutturate. Questo componente mantiene solo il
    /// cursore operativo: il job completo resta in uno store dedicato o nel sistema
    /// che lo ha assegnato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasActiveJob</b>: true quando l'NPC possiede un job corrente.</item>
    ///   <item><b>ActiveJobId</b>: riferimento testuale al job corrente.</item>
    ///   <item><b>ActivePhaseIndex</b>: indice della fase corrente nel piano.</item>
    ///   <item><b>ActiveActionIndex</b>: indice dello step corrente nella fase.</item>
    ///   <item><b>WaitUntilTick</b>: tick fino a cui lo step resta in attesa.</item>
    ///   <item><b>Recovery*</b>: contatori minimi per retry locale controllato dello step corrente.</item>
    ///   <item><b>SuspendedJobId</b>: job parcheggiato da preemption recuperabile.</item>
    ///   <item><b>LastFailureReason</b>: ultimo motivo diagnostico registrato.</item>
    /// </list>
    /// </summary>
    public struct NpcJobState
    {
        public bool HasActiveJob;
        public string ActiveJobId;
        public int ActivePhaseIndex;
        public int ActiveActionIndex;
        public int WaitUntilTick;
        public JobStepFailureKind RecoveryFailureKind;
        public int RecoveryPhaseIndex;
        public int RecoveryActionIndex;
        public int RecoveryRetryCount;
        public int RecoveryAlternativeTargetCount;
        public int RecoveryStartedTick;
        public string SuspendedJobId;
        public JobFailureReason LastFailureReason;

        public static NpcJobState Empty()
        {
            // Empty crea uno stato pulito e serializzabile, senza null esposti alla
            // UI o alla telemetria futura.
            return new NpcJobState
            {
                HasActiveJob = false,
                ActiveJobId = string.Empty,
                ActivePhaseIndex = 0,
                ActiveActionIndex = 0,
                WaitUntilTick = 0,
                RecoveryFailureKind = JobStepFailureKind.None,
                RecoveryPhaseIndex = -1,
                RecoveryActionIndex = -1,
                RecoveryRetryCount = 0,
                RecoveryAlternativeTargetCount = 0,
                RecoveryStartedTick = 0,
                SuspendedJobId = string.Empty,
                LastFailureReason = JobFailureReason.None
            };
        }

        public void AssignJob(string jobId, int tick)
        {
            // L'assegnazione riparte sempre dall'inizio del piano: eventuali resume
            // saranno introdotti dalla preemption ladder, non nascosti qui.
            HasActiveJob = !string.IsNullOrWhiteSpace(jobId);
            ActiveJobId = HasActiveJob ? jobId : string.Empty;
            ActivePhaseIndex = 0;
            ActiveActionIndex = 0;
            WaitUntilTick = Math.Max(0, tick);
            LastFailureReason = JobFailureReason.None;
            ClearRecovery();
        }

        public void Clear(JobFailureReason reason)
        {
            // Clear chiude il riferimento al job ma conserva l'ultimo motivo: la UI
            // QA puo' mostrare perche' l'NPC e' tornato idle.
            HasActiveJob = false;
            ActiveJobId = string.Empty;
            ActivePhaseIndex = 0;
            ActiveActionIndex = 0;
            WaitUntilTick = 0;
            LastFailureReason = reason;
            ClearRecovery();
        }

        public void AdvanceAction()
        {
            // Avanzare action non cambia fase: la state machine decidera' quando una
            // fase e' completata e chiamera' AdvancePhase.
            ActiveActionIndex = Math.Max(0, ActiveActionIndex + 1);
            WaitUntilTick = 0;
            ClearRecovery();
        }

        public void AdvancePhase()
        {
            // Il passaggio di fase azzera lo step index per mantenere leggibile il
            // cursore gerarchico fase -> step.
            ActivePhaseIndex = Math.Max(0, ActivePhaseIndex + 1);
            ActiveActionIndex = 0;
            WaitUntilTick = 0;
            ClearRecovery();
        }

        public void SetWaitingUntil(int tick)
        {
            // L'attesa viene clampata a zero per evitare tick negativi generati da
            // test o dati incompleti.
            WaitUntilTick = Math.Max(0, tick);
        }

        public bool IsWaitingAt(int tick)
        {
            // La comparazione e' inclusiva sul futuro: se il tick corrente e' minore
            // del limite, lo step resta fermo; al tick limite puo' riprovare.
            return WaitUntilTick > Math.Max(0, tick);
        }

        public void SuspendActiveJob()
        {
            // La sospensione conserva l'id per un futuro resume, ma libera il cursore
            // attivo cosi' un job di priorita' superiore puo' essere assegnato.
            SuspendedJobId = HasActiveJob ? ActiveJobId : string.Empty;
            HasActiveJob = false;
            ActiveJobId = string.Empty;
            ActivePhaseIndex = 0;
            ActiveActionIndex = 0;
            WaitUntilTick = 0;
            ClearRecovery();
        }

        // =============================================================================
        // GetRecoveryRetryCount
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il numero di retry gia' registrati per la stessa combinazione
        /// fallimento/fase/step.
        /// </para>
        ///
        /// <para><b>Contatore locale, non scheduler</b></para>
        /// <para>
        /// Il metodo non decide se il retry sia ammesso e non modifica lo stato.
        /// Serve al recovery evaluator per confrontare il contatore corrente con i
        /// limiti dichiarati dalla policy configurabile.
        /// </para>
        /// </summary>
        public int GetRecoveryRetryCount(JobStepFailureKind failureKind, int phaseIndex, int actionIndex)
        {
            return MatchesRecoveryCursor(failureKind, phaseIndex, actionIndex)
                ? RecoveryRetryCount
                : 0;
        }

        public int GetRecoveryAlternativeTargetCount(JobStepFailureKind failureKind, int phaseIndex, int actionIndex)
        {
            return MatchesRecoveryCursor(failureKind, phaseIndex, actionIndex)
                ? RecoveryAlternativeTargetCount
                : 0;
        }

        // =============================================================================
        // GetRecoveryElapsedTicks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola da quanti tick dura il tentativo di recupero locale dello stesso
        /// step, se il cursore recovery corrisponde ancora.
        /// </para>
        /// </summary>
        public int GetRecoveryElapsedTicks(JobStepFailureKind failureKind, int phaseIndex, int actionIndex, int tick)
        {
            if (!MatchesRecoveryCursor(failureKind, phaseIndex, actionIndex))
                return 0;

            return Math.Max(0, Math.Max(0, tick) - Math.Max(0, RecoveryStartedTick));
        }

        // =============================================================================
        // RegisterRecoveryRetry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra un retry locale per lo step corrente mantenendo un contatore
        /// bounded dal chiamante.
        /// </para>
        ///
        /// <para><b>Stato recovery agganciato al cursore Job</b></para>
        /// <para>
        /// Il contatore viene riusato solo se fallimento, fase e action coincidono.
        /// Quando cambia una di queste coordinate, la finestra recovery riparte da
        /// zero. Questo evita che un retry vecchio contamini uno step successivo.
        /// </para>
        /// </summary>
        public void RegisterRecoveryRetry(JobStepFailureKind failureKind, int phaseIndex, int actionIndex, int tick)
        {
            if (failureKind == JobStepFailureKind.None)
            {
                ClearRecovery();
                return;
            }

            if (!MatchesRecoveryCursor(failureKind, phaseIndex, actionIndex))
            {
                RecoveryFailureKind = failureKind;
                RecoveryPhaseIndex = Math.Max(0, phaseIndex);
                RecoveryActionIndex = Math.Max(0, actionIndex);
                RecoveryRetryCount = 0;
                RecoveryStartedTick = Math.Max(0, tick);
            }

            RecoveryRetryCount = Math.Max(0, RecoveryRetryCount + 1);
        }

        public void RegisterRecoveryAlternativeTarget(JobStepFailureKind failureKind, int phaseIndex, int actionIndex, int tick)
        {
            if (failureKind == JobStepFailureKind.None)
            {
                ClearRecovery();
                return;
            }

            if (!MatchesRecoveryCursor(failureKind, phaseIndex, actionIndex))
            {
                RecoveryFailureKind = failureKind;
                RecoveryPhaseIndex = Math.Max(0, phaseIndex);
                RecoveryActionIndex = Math.Max(0, actionIndex);
                RecoveryRetryCount = 0;
                RecoveryAlternativeTargetCount = 0;
                RecoveryStartedTick = Math.Max(0, tick);
            }

            RecoveryAlternativeTargetCount = Math.Max(0, RecoveryAlternativeTargetCount + 1);
        }

        private bool MatchesRecoveryCursor(JobStepFailureKind failureKind, int phaseIndex, int actionIndex)
        {
            return RecoveryFailureKind == failureKind
                && RecoveryPhaseIndex == Math.Max(0, phaseIndex)
                && RecoveryActionIndex == Math.Max(0, actionIndex);
        }

        private void ClearRecovery()
        {
            RecoveryFailureKind = JobStepFailureKind.None;
            RecoveryPhaseIndex = -1;
            RecoveryActionIndex = -1;
            RecoveryRetryCount = 0;
            RecoveryAlternativeTargetCount = 0;
            RecoveryStartedTick = 0;
        }
    }
}
