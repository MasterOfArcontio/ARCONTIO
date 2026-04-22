using System;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // JobPriorityClass
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classe discreta di priorita' usata dal futuro JobArbiter per confrontare
    /// richieste eterogenee senza mescolare subito urgenza numerica, norme sociali
    /// e stati critici dell'NPC.
    /// </para>
    ///
    /// <para><b>Separazione Decision Layer / Job Execution</b></para>
    /// <para>
    /// Il Decision Layer puo' proporre una intenzione e una pressione motivazionale,
    /// ma l'esecuzione ha bisogno di una categoria stabile per decidere se un lavoro
    /// puo' interromperne un altro. Questa enum e' quindi un ponte dati, non una
    /// policy completa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Idle</b>: lavoro di riempimento o attesa senza urgenza.</item>
    ///   <item><b>Normal</b>: lavoro ordinario prodotto dalla pianificazione locale.</item>
    ///   <item><b>Important</b>: lavoro utile ma non ancora critico.</item>
    ///   <item><b>Critical</b>: lavoro legato a bisogni o sicurezza immediata.</item>
    ///   <item><b>Emergency</b>: lavoro che puo' preemptare quasi tutto nella ladder futura.</item>
    /// </list>
    /// </summary>
    public enum JobPriorityClass
    {
        Idle = 0,
        Normal = 10,
        Important = 20,
        Critical = 30,
        Emergency = 40
    }

    // =============================================================================
    // JobStatus
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato persistente di un job assegnato o candidato all'assegnazione.
    /// </para>
    ///
    /// <para><b>Stato esplicito, niente impliciti nel sistema</b></para>
    /// <para>
    /// La v0.06 deve rendere osservabile la vita del job. Uno stato esplicito evita
    /// che sistemi separati deducano il progresso da flag locali non coordinati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Created</b>: il job esiste ma non e' ancora in esecuzione.</item>
    ///   <item><b>Running</b>: il job ha una fase attiva.</item>
    ///   <item><b>Suspended</b>: il job e' parcheggiato da una preemption recuperabile.</item>
    ///   <item><b>Completed</b>: tutte le fasi richieste sono finite.</item>
    ///   <item><b>Failed</b>: il job si e' fermato con una ragione diagnostica.</item>
    ///   <item><b>Cancelled</b>: il job e' stato chiuso da policy esterna o riassegnazione.</item>
    /// </list>
    /// </summary>
    public enum JobStatus
    {
        Created = 0,
        Running = 10,
        Suspended = 20,
        Completed = 30,
        Failed = 40,
        Cancelled = 50
    }

    // =============================================================================
    // JobFailureReason
    // =============================================================================
    /// <summary>
    /// <para>
    /// Motivo normalizzato con cui un job puo' terminare in fallimento o essere
    /// chiuso prima del completamento naturale.
    /// </para>
    ///
    /// <para><b>Failure learning progressivo</b></para>
    /// <para>
    /// Il sistema di apprendimento dagli insuccessi arrivera' negli step finali
    /// della v0.06. Introdurre subito un vocabolario piccolo permette ai test e alla
    /// telemetria di non dipendere da stringhe libere.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessun errore registrato.</item>
    ///   <item><b>InvalidRequest</b>: richiesta incompleta o incoerente.</item>
    ///   <item><b>MissingPlan</b>: nessun piano eseguibile associato al job.</item>
    ///   <item><b>MissingTarget</b>: target richiesto non disponibile.</item>
    ///   <item><b>ReservationDenied</b>: risorsa o cella gia' impegnata.</item>
    ///   <item><b>MovementFailed</b>: la fase di movimento non ha prodotto arrivo.</item>
    ///   <item><b>StepFailed</b>: una azione atomica ha fallito senza motivo piu' specifico.</item>
    ///   <item><b>Preempted</b>: il job e' stato sostituito da priorita' superiore.</item>
    ///   <item><b>Cancelled</b>: chiusura intenzionale da policy esterna.</item>
    ///   <item><b>Unknown</b>: fallback diagnostico da usare solo in transizione.</item>
    /// </list>
    /// </summary>
    public enum JobFailureReason
    {
        None = 0,
        InvalidRequest = 10,
        MissingPlan = 20,
        MissingTarget = 30,
        ReservationDenied = 40,
        MovementFailed = 50,
        StepFailed = 60,
        Preempted = 70,
        Cancelled = 80,
        Unknown = 999
    }

    // =============================================================================
    // JobPhaseKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Categoria funzionale di una fase interna al job.
    /// </para>
    ///
    /// <para><b>Job complessi composti da mini job</b></para>
    /// <para>
    /// La fase rappresenta il livello "mini job" concordato in progettazione: non
    /// e' ancora il singolo comando, ma un blocco coerente come raggiungere,
    /// preparare, eseguire, pulire o recuperare.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: fase non classificata, utile per test e default.</item>
    ///   <item><b>ReachTarget</b>: avvicinamento al target operativo.</item>
    ///   <item><b>Prepare</b>: prenotazioni, controlli o setup prima dell'azione.</item>
    ///   <item><b>Execute</b>: nucleo produttivo del job.</item>
    ///   <item><b>Cleanup</b>: rilascio risorse e chiusura ordinata.</item>
    ///   <item><b>Recover</b>: fase di recupero dopo fallimento parziale.</item>
    ///   <item><b>Custom</b>: estensione controllata per domini futuri.</item>
    /// </list>
    /// </summary>
    public enum JobPhaseKind
    {
        None = 0,
        ReachTarget = 10,
        Prepare = 20,
        Execute = 30,
        Cleanup = 40,
        Recover = 50,
        Custom = 100
    }

    // =============================================================================
    // JobRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta pura con cui il Decision Layer, o un sistema autorizzato, propone
    /// un lavoro al futuro JobArbiter.
    /// </para>
    ///
    /// <para><b>Contratto tra decisione ed esecuzione</b></para>
    /// <para>
    /// La richiesta contiene solo dati gia' risolti dal chiamante: identita' NPC,
    /// intenzione, target opzionale, priorita' e riferimenti diagnostici. Non possiede
    /// un puntatore al World e non puo' leggere stato globale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RequestId</b>: identificatore stabile della richiesta.</item>
    ///   <item><b>NpcId</b>: NPC destinatario del job.</item>
    ///   <item><b>IntentKind</b>: intenzione decisionale da trasformare in piano.</item>
    ///   <item><b>PriorityClass/Urgency01</b>: pressione usata dall'arbitraggio futuro.</item>
    ///   <item><b>CreatedTick</b>: tick di nascita per audit e tie-break.</item>
    ///   <item><b>TargetCell/TargetObjectId</b>: target opzionali gia' noti al chiamante.</item>
    ///   <item><b>BeliefKey</b>: riferimento testuale alla credenza che ha motivato la richiesta.</item>
    ///   <item><b>DebugLabel</b>: etichetta umana non necessaria alla logica.</item>
    /// </list>
    /// </summary>
    public readonly struct JobRequest
    {
        public readonly string RequestId;
        public readonly int NpcId;
        public readonly DecisionIntentKind IntentKind;
        public readonly JobPriorityClass PriorityClass;
        public readonly float Urgency01;
        public readonly int CreatedTick;
        public readonly bool HasTargetCell;
        public readonly Vector2Int TargetCell;
        public readonly int TargetObjectId;
        public readonly string BeliefKey;
        public readonly string DebugLabel;

        public JobRequest(
            string requestId,
            int npcId,
            DecisionIntentKind intentKind,
            JobPriorityClass priorityClass,
            float urgency01,
            int createdTick,
            bool hasTargetCell,
            Vector2Int targetCell,
            int targetObjectId,
            string beliefKey,
            string debugLabel)
        {
            // Gli identificatori nulli vengono normalizzati per rendere i log e i
            // test deterministici senza imporre subito un generatore globale di ID.
            RequestId = string.IsNullOrWhiteSpace(requestId) ? Guid.NewGuid().ToString("N") : requestId;
            NpcId = npcId;
            IntentKind = intentKind;
            PriorityClass = priorityClass;
            Urgency01 = Clamp01(urgency01);
            CreatedTick = createdTick;
            HasTargetCell = hasTargetCell;
            TargetCell = targetCell;
            TargetObjectId = targetObjectId;
            BeliefKey = beliefKey ?? string.Empty;
            DebugLabel = debugLabel ?? string.Empty;
        }

        public static JobRequest FromDecision(
            string requestId,
            int npcId,
            DecisionIntentKind intentKind,
            JobPriorityClass priorityClass,
            float urgency01,
            int createdTick,
            Vector2Int targetCell,
            string beliefKey,
            string debugLabel)
        {
            // Factory comoda per il caso piu' frequente della v0.06: una decisione
            // produce un target cella soggettivo e lo consegna al job system.
            return new JobRequest(
                requestId,
                npcId,
                intentKind,
                priorityClass,
                urgency01,
                createdTick,
                true,
                targetCell,
                -1,
                beliefKey,
                debugLabel);
        }

        public static JobRequest WithoutTarget(
            string requestId,
            int npcId,
            DecisionIntentKind intentKind,
            JobPriorityClass priorityClass,
            float urgency01,
            int createdTick,
            string debugLabel)
        {
            // Alcuni job, come WaitAndObserve o future ricerche esplorative, nascono
            // senza una cella obiettivo gia' selezionata.
            return new JobRequest(
                requestId,
                npcId,
                intentKind,
                priorityClass,
                urgency01,
                createdTick,
                false,
                Vector2Int.zero,
                -1,
                string.Empty,
                debugLabel);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    // =============================================================================
    // JobPhase
    // =============================================================================
    /// <summary>
    /// <para>
    /// Mini job ordinato dentro un piano piu' grande.
    /// </para>
    ///
    /// <para><b>Gerarchia Job -> JobPlan -> JobPhase -> Step</b></para>
    /// <para>
    /// Questa struttura introduce il livello intermedio richiesto per lavori
    /// complessi. Una fase non esegue ancora comandi: descrive confini, ordine e
    /// intenzione operativa che gli step successivi riempiranno con azioni atomiche.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PhaseId</b>: identificatore stabile dentro il piano.</item>
    ///   <item><b>Kind</b>: categoria funzionale della fase.</item>
    ///   <item><b>DisplayName</b>: nome leggibile per log e debug.</item>
    ///   <item><b>ExpectedStepCount</b>: promessa diagnostica in attesa degli step concreti.</item>
    ///   <item><b>IsInterruptible</b>: suggerimento per la futura preemption ladder.</item>
    /// </list>
    /// </summary>
    public readonly struct JobPhase
    {
        public readonly string PhaseId;
        public readonly JobPhaseKind Kind;
        public readonly string DisplayName;
        public readonly int ExpectedStepCount;
        public readonly bool IsInterruptible;

        public JobPhase(string phaseId, JobPhaseKind kind, string displayName, int expectedStepCount, bool isInterruptible)
        {
            PhaseId = string.IsNullOrWhiteSpace(phaseId) ? kind.ToString() : phaseId;
            Kind = kind;
            DisplayName = displayName ?? string.Empty;
            ExpectedStepCount = Math.Max(0, expectedStepCount);
            IsInterruptible = isInterruptible;
        }
    }

    // =============================================================================
    // JobPlan
    // =============================================================================
    /// <summary>
    /// <para>
    /// Piano ordinato di fasi che descrive come un job dovra' essere eseguito.
    /// </para>
    ///
    /// <para><b>Piano deterministico prima del GOAP completo</b></para>
    /// <para>
    /// La v0.06 non introduce ancora un planner GOAP completo. Il piano e' quindi
    /// una sequenza esplicita e testabile di mini job, generata da factory o builder
    /// specializzati e poi consumata dalla macchina a stati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PlanId</b>: identificatore stabile del piano.</item>
    ///   <item><b>Phases</b>: copia difensiva delle fasi ordinate.</item>
    ///   <item><b>PhaseCount</b>: numero di fasi disponibili.</item>
    ///   <item><b>IsEmpty</b>: true quando il piano non contiene lavoro eseguibile.</item>
    /// </list>
    /// </summary>
    public sealed class JobPlan
    {
        public readonly string PlanId;
        public readonly JobPhase[] Phases;

        public int PhaseCount => Phases.Length;
        public bool IsEmpty => Phases.Length == 0;

        public JobPlan(string planId, JobPhase[] phases)
        {
            PlanId = string.IsNullOrWhiteSpace(planId) ? "JobPlan" : planId;

            // La copia difensiva impedisce al chiamante di cambiare il piano dopo
            // averlo consegnato al job system, mantenendo deterministici test e log.
            Phases = phases == null || phases.Length == 0
                ? Array.Empty<JobPhase>()
                : (JobPhase[])phases.Clone();
        }

        public bool TryGetPhase(int phaseIndex, out JobPhase phase)
        {
            // Il piano non lancia eccezioni per indici di runtime: la macchina a
            // stati usera' questo metodo per distinguere fine piano ed errore.
            if (phaseIndex < 0 || phaseIndex >= Phases.Length)
            {
                phase = default;
                return false;
            }

            phase = Phases[phaseIndex];
            return true;
        }
    }

    // =============================================================================
    // Job
    // =============================================================================
    /// <summary>
    /// <para>
    /// Istanza persistente di lavoro assegnabile a un NPC.
    /// </para>
    ///
    /// <para><b>Job come stato di esecuzione, non come decisione</b></para>
    /// <para>
    /// Il job conserva la richiesta originaria e il piano scelto, ma non decide da
    /// solo cosa fare nel mondo. Gli step successivi useranno questa struttura per
    /// avanzare fasi, produrre comandi e registrare fallimenti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>JobId</b>: identificatore stabile dell'istanza.</item>
    ///   <item><b>Request</b>: richiesta che ha generato il job.</item>
    ///   <item><b>Plan</b>: sequenza di fasi da eseguire.</item>
    ///   <item><b>Status</b>: stato corrente del ciclo di vita.</item>
    ///   <item><b>ActivePhaseIndex</b>: fase corrente del piano.</item>
    ///   <item><b>CreatedTick/UpdatedTick</b>: tracciamento temporale minimale.</item>
    ///   <item><b>FailureReason</b>: motivo diagnostico se il job non completa.</item>
    /// </list>
    /// </summary>
    public sealed class Job
    {
        public readonly string JobId;
        public readonly JobRequest Request;
        public readonly JobPlan Plan;
        public JobStatus Status { get; private set; }
        public int ActivePhaseIndex { get; private set; }
        public int CreatedTick { get; }
        public int UpdatedTick { get; private set; }
        public JobFailureReason FailureReason { get; private set; }

        public Job(string jobId, JobRequest request, JobPlan plan)
        {
            JobId = string.IsNullOrWhiteSpace(jobId) ? Guid.NewGuid().ToString("N") : jobId;
            Request = request;
            Plan = plan ?? new JobPlan("EmptyPlan", Array.Empty<JobPhase>());
            Status = JobStatus.Created;
            ActivePhaseIndex = 0;
            CreatedTick = request.CreatedTick;
            UpdatedTick = request.CreatedTick;
            FailureReason = JobFailureReason.None;
        }

        public bool TryGetActivePhase(out JobPhase phase)
        {
            // Il job delega al piano la validazione dell'indice: questo mantiene la
            // struttura piccola e lascia al futuro state machine la policy di errore.
            return Plan.TryGetPhase(ActivePhaseIndex, out phase);
        }

        public void MarkRunning(int tick)
        {
            // La transizione resta intenzionalmente permissiva nello step 01: le
            // regole rigorose arriveranno con la macchina a stati dello step 05.
            Status = JobStatus.Running;
            UpdatedTick = tick;
            FailureReason = JobFailureReason.None;
        }

        public void MoveToPhase(int phaseIndex, int tick)
        {
            // Il metodo registra solo il cursore. L'esistenza della fase viene
            // controllata dal chiamante per permettere test separati sulla policy.
            ActivePhaseIndex = Math.Max(0, phaseIndex);
            UpdatedTick = tick;
        }

        public void MarkCompleted(int tick)
        {
            // La chiusura positiva azzera la ragione di fallimento per evitare che
            // vecchi errori transitori restino appesi a un job recuperato.
            Status = JobStatus.Completed;
            UpdatedTick = tick;
            FailureReason = JobFailureReason.None;
        }

        public void MarkFailed(JobFailureReason reason, int tick)
        {
            // Un fallimento senza ragione esplicita viene normalizzato a Unknown:
            // meglio un segnale diagnostico imperfetto che un errore silenzioso.
            Status = JobStatus.Failed;
            UpdatedTick = tick;
            FailureReason = reason == JobFailureReason.None ? JobFailureReason.Unknown : reason;
        }
    }
}
