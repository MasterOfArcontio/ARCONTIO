using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // JobRuntimeState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Store runtime transitorio posseduto da <see cref="World"/> per contenere lo
    /// stato operativo minimo del Job System.
    /// </para>
    ///
    /// <para><b>World come ownership dello stato runtime, non esecuzione logica</b></para>
    /// <para>
    /// Lo store vive nel <c>World</c> per evitare una seconda sorgente di verita'
    /// nascosta dentro <c>JobExecutionSystem</c>, ma rimane volutamente passivo:
    /// non decide, non ticka e non muta il mondo. Conserva soltanto job attivi,
    /// cursori per-NPC, reservation store e command buffer del layer job.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcStates</b>: cursore job per ogni NPC registrato nel mondo.</item>
    ///   <item><b>Jobs</b>: istanze di job runtime indicizzate per job id.</item>
    ///   <item><b>Reservations</b>: store condiviso delle reservation del job layer.</item>
    ///   <item><b>CommandBuffer</b>: buffer dei command prodotti dagli step e pompati da <c>SimulationHost</c>.</item>
    /// </list>
    /// </summary>
    public sealed class JobRuntimeState
    {
        private readonly Dictionary<int, NpcJobState> _npcStates = new();
        private readonly Dictionary<string, Job> _jobs = new();
        private readonly JobArbiter _arbiter = new();

        public ReservationStore Reservations { get; } = new();
        public JobCommandBuffer CommandBuffer { get; } = new();

        public int ActiveJobCount => _jobs.Count;
        public int NpcStateCount => _npcStates.Count;

        // =============================================================================
        // EnsureNpcState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Garantisce che un NPC abbia uno slot di stato job vuoto.
        /// </para>
        ///
        /// <para><b>Inizializzazione esplicita per-NPC</b></para>
        /// <para>
        /// La registrazione viene chiamata sia da <c>CreateNpc</c> sia dal load
        /// ID-preserving. In v0.11.01 il contenuto e' sempre transitorio: dopo load
        /// gli NPC ripartono senza job attivo e possono ricostruire l'intenzione dal
        /// tick successivo tramite needs/beliefs.
        /// </para>
        /// </summary>
        public void EnsureNpcState(int npcId)
        {
            if (npcId <= 0)
                return;

            if (!_npcStates.ContainsKey(npcId))
                _npcStates[npcId] = NpcJobState.Empty();
        }

        public bool TryGetNpcState(int npcId, out NpcJobState state)
        {
            return _npcStates.TryGetValue(npcId, out state);
        }

        public void SetNpcState(int npcId, in NpcJobState state)
        {
            if (npcId <= 0)
                return;

            _npcStates[npcId] = state;
        }

        // =============================================================================
        // TryAssignJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra un job e lo assegna all'NPC passando sempre da una policy di
        /// arbitraggio minima.
        /// </para>
        ///
        /// <para><b>Anti doppia pipeline</b></para>
        /// <para>
        /// La route job non scrive direttamente nel cursore quando l'NPC e' occupato:
        /// chiede prima al <c>JobArbiter</c> se il nuovo lavoro puo' sostituire quello
        /// corrente. Se l'arbitro rifiuta, il chiamante puo' lasciare invariato il
        /// fallback legacy; se accetta una preemption, il job precedente viene chiuso
        /// con ragione <c>Preempted</c> e il nuovo job diventa l'unico attivo.
        /// </para>
        /// </summary>
        public bool TryAssignJob(int npcId, Job job, int tick, out string reason)
        {
            reason = string.Empty;

            if (npcId <= 0)
            {
                reason = "InvalidNpcId";
                return false;
            }

            if (job == null)
            {
                reason = "JobMissing";
                return false;
            }

            EnsureNpcState(npcId);
            var state = _npcStates[npcId];
            TryGetJob(state.ActiveJobId, out var currentJob);
            var arbitration = _arbiter.Evaluate(state, currentJob, job);
            reason = arbitration.Reason;

            if (arbitration.Decision == JobArbitrationDecision.RejectInvalid
                || arbitration.Decision == JobArbitrationDecision.KeepCurrent)
            {
                return false;
            }

            if (arbitration.Decision == JobArbitrationDecision.SuspendCurrentForNew
                || arbitration.Decision == JobArbitrationDecision.CancelCurrentForNew)
            {
                FailCurrentJob(npcId, JobFailureReason.Preempted, tick, out _);
                state = _npcStates[npcId];
            }

            _jobs[job.JobId] = job;
            state.AssignJob(job.JobId, tick);
            _npcStates[npcId] = state;
            reason = arbitration.Reason == "NpcIdle" ? "JobAssigned" : arbitration.Reason;
            return true;
        }

        // =============================================================================
        // CompleteCurrentJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Chiude positivamente il job corrente dell'NPC e libera il cursore runtime.
        /// </para>
        ///
        /// <para><b>Helper espliciti per lifecycle runtime</b></para>
        /// <para>
        /// La state machine puo' ancora manipolare direttamente il cursore nello step
        /// corrente, ma questi helper definiscono il contratto pubblico minimo per
        /// future integrazioni: completare, fallire o pulire un job senza duplicare
        /// accessi ai dizionari interni.
        /// </para>
        /// </summary>
        public bool CompleteCurrentJob(int npcId, int tick, out string reason)
        {
            reason = string.Empty;

            if (!TryGetActiveJob(npcId, out var state, out var job) || job == null)
            {
                reason = "NoActiveJob";
                return false;
            }

            job.MarkCompleted(tick);
            _jobs.Remove(job.JobId);
            state.Clear(JobFailureReason.None);
            _npcStates[npcId] = state;
            reason = "JobCompleted";
            return true;
        }

        public bool FailCurrentJob(int npcId, JobFailureReason failureReason, int tick, out string reason)
        {
            reason = string.Empty;

            if (!TryGetActiveJob(npcId, out var state, out var job) || job == null)
            {
                reason = "NoActiveJob";
                return false;
            }

            job.MarkFailed(failureReason, tick);
            _jobs.Remove(job.JobId);
            state.Clear(failureReason);
            _npcStates[npcId] = state;
            reason = "JobFailed";
            return true;
        }

        public bool ClearNpcJob(int npcId, JobFailureReason clearReason, out string reason)
        {
            reason = string.Empty;

            if (!_npcStates.TryGetValue(npcId, out var state))
            {
                reason = "NpcStateMissing";
                return false;
            }

            if (state.HasActiveJob)
                _jobs.Remove(state.ActiveJobId);

            state.Clear(clearReason);
            _npcStates[npcId] = state;
            reason = "NpcJobCleared";
            return true;
        }

        public bool HasActiveJob(int npcId)
        {
            return _npcStates.TryGetValue(npcId, out var state) && state.HasActiveJob;
        }

        public bool TryGetJob(string jobId, out Job job)
        {
            return _jobs.TryGetValue(jobId ?? string.Empty, out job);
        }

        public bool TryGetActiveJob(int npcId, out NpcJobState state, out Job job)
        {
            job = null;
            if (!_npcStates.TryGetValue(npcId, out state))
                return false;

            if (!state.HasActiveJob)
                return false;

            return TryGetJob(state.ActiveJobId, out job);
        }

        public void CopyNpcIdsWithActiveJobsTo(List<int> output)
        {
            if (output == null)
                return;

            output.Clear();
            foreach (var pair in _npcStates)
            {
                if (pair.Value.HasActiveJob)
                    output.Add(pair.Key);
            }
        }

        // =============================================================================
        // ClearTransientJobs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota jobs, reservation e command buffer preservando gli slot NPC come
        /// idle.
        /// </para>
        ///
        /// <para><b>Save/load v0.11.01: job non persistiti</b></para>
        /// <para>
        /// La prima slice non serializza job attivi. Questo metodo rende esplicito il
        /// reset transitorio usato dopo load o durante bootstrap controllati: il mondo
        /// oggettivo e le memorie persistono, ma il layer job riparte pulito.
        /// </para>
        /// </summary>
        public void ClearTransientJobs()
        {
            _jobs.Clear();
            CommandBuffer.Clear();

            var keys = new List<int>(_npcStates.Keys);
            for (int i = 0; i < keys.Count; i++)
                _npcStates[keys[i]] = NpcJobState.Empty();
        }
    }
}
