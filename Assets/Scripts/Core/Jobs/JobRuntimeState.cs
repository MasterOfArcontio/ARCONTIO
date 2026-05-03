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
        /// Registra un job e lo assegna all'NPC solo se il cursore per-NPC non ha gia'
        /// un lavoro attivo.
        /// </para>
        ///
        /// <para><b>Anti doppia pipeline</b></para>
        /// <para>
        /// La prima slice non introduce code multiple o preemption completa. Se un
        /// job e' gia' attivo, la route job deve considerarsi occupata e impedire al
        /// bridge legacy di emettere un command parallelo per la stessa intenzione.
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
            if (state.HasActiveJob)
            {
                reason = "NpcAlreadyHasActiveJob";
                return false;
            }

            _jobs[job.JobId] = job;
            state.AssignJob(job.JobId, tick);
            _npcStates[npcId] = state;
            reason = "JobAssigned";
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
