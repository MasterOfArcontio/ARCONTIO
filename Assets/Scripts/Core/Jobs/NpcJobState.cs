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
        }

        public void AdvanceAction()
        {
            // Avanzare action non cambia fase: la state machine decidera' quando una
            // fase e' completata e chiamera' AdvancePhase.
            ActiveActionIndex = Math.Max(0, ActiveActionIndex + 1);
            WaitUntilTick = 0;
        }

        public void AdvancePhase()
        {
            // Il passaggio di fase azzera lo step index per mantenere leggibile il
            // cursore gerarchico fase -> step.
            ActivePhaseIndex = Math.Max(0, ActivePhaseIndex + 1);
            ActiveActionIndex = 0;
            WaitUntilTick = 0;
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
        }
    }
}
