using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // RunningActionKey
    // =============================================================================
    /// <summary>
    /// <para>
    /// Chiave composta che identifica una singola running action nello scope del
    /// runtime job produttivo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: evitare doppio stato e desync</b></para>
    /// <para>
    /// ARC-DEC-020 richiede progress volatile separato dalla mutazione del mondo,
    /// ma quello stato non deve diventare una seconda verita' scollegata dal job
    /// attivo. La chiave include quindi NPC, job, fase e action: lo store puo'
    /// associare il progresso interno esattamente al cursore operativo che lo ha
    /// generato, senza assumere traversal, movement o preemption.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: NPC proprietario del job runtime.</item>
    ///   <item><b>JobId</b>: job attivo a cui appartiene l'action.</item>
    ///   <item><b>PhaseIndex</b>: fase corrente del piano job.</item>
    ///   <item><b>ActionIndex</b>: action corrente dentro la fase.</item>
    /// </list>
    /// </summary>
    public readonly struct RunningActionKey : IEquatable<RunningActionKey>
    {
        public readonly int NpcId;
        public readonly string JobId;
        public readonly int PhaseIndex;
        public readonly int ActionIndex;

        public RunningActionKey(int npcId, string jobId, int phaseIndex, int actionIndex)
        {
            NpcId = Math.Max(0, npcId);
            JobId = jobId ?? string.Empty;
            PhaseIndex = Math.Max(0, phaseIndex);
            ActionIndex = Math.Max(0, actionIndex);
        }

        public bool IsValid => NpcId > 0 && !string.IsNullOrWhiteSpace(JobId);

        public bool Equals(RunningActionKey other)
        {
            // La comparazione e' volutamente ordinaria e deterministica: due action
            // sono la stessa solo se appartengono allo stesso NPC, job e cursore.
            return NpcId == other.NpcId
                && string.Equals(JobId, other.JobId, StringComparison.Ordinal)
                && PhaseIndex == other.PhaseIndex
                && ActionIndex == other.ActionIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is RunningActionKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + NpcId;
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(JobId ?? string.Empty);
                hash = (hash * 31) + PhaseIndex;
                hash = (hash * 31) + ActionIndex;
                return hash;
            }
        }
    }

    // =============================================================================
    // RunningActionStore
    // =============================================================================
    /// <summary>
    /// <para>
    /// Store runtime volatile delle <see cref="RunningActionRuntimeState"/> posseduto
    /// da <see cref="JobRuntimeState"/>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: progress interno sotto authority Job</b></para>
    /// <para>
    /// Lo store vive sotto <c>JobRuntimeState</c> perche' il Job Layer e' la sorgente
    /// corretta dello stato esecutivo. Rimane pero' un contenitore passivo: non
    /// ticka le action, non emette <c>ICommand</c>, non assegna job, non decide
    /// preemption e non muta <c>World</c>. Conserva solo progress volatile, cioe'
    /// dati che non devono entrare nel save/load e che vengono puliti quando il job
    /// termina, fallisce, viene preemptato o quando il runtime transitorio viene
    /// resettato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Dictionary</b>: mappa chiave composta -> stato running action.</item>
    ///   <item><b>Register/Update</b>: API esplicite per start e replace controllato.</item>
    ///   <item><b>Clear</b>: cleanup per key, NPC, job o reset globale.</item>
    ///   <item><b>Snapshots</b>: enumerazione read-only difensiva per QA/futura EL.</item>
    /// </list>
    /// </summary>
    public sealed class RunningActionStore
    {
        private readonly Dictionary<RunningActionKey, RunningActionRuntimeState> _states = new();

        public int Count => _states.Count;

        public bool Register(RunningActionKey key, RunningActionRuntimeState state, out string reason)
        {
            reason = string.Empty;

            // Lo store accetta solo chiavi agganciate a un NPC/job reale. Questa e'
            // una guardia contro progress orfani che sarebbero impossibili da pulire
            // correttamente durante complete/fail/preempt.
            if (!key.IsValid)
            {
                reason = "InvalidRunningActionKey";
                return false;
            }

            if (state == null)
            {
                reason = "RunningActionStateMissing";
                return false;
            }

            if (_states.ContainsKey(key))
            {
                reason = "RunningActionAlreadyRegistered";
                return false;
            }

            _states[key] = state;
            reason = "RunningActionRegistered";
            return true;
        }

        public bool TryGet(RunningActionKey key, out RunningActionRuntimeState state)
        {
            // Il chiamante riceve il riferimento runtime volutamente mutabile:
            // l'executor futuro dovra' avanzare lo stato interno. La mutation resta
            // comunque confinata al progress della running action, non al World.
            return _states.TryGetValue(key, out state);
        }

        public bool Update(RunningActionKey key, RunningActionRuntimeState state, out string reason)
        {
            reason = string.Empty;

            if (!key.IsValid)
            {
                reason = "InvalidRunningActionKey";
                return false;
            }

            if (state == null)
            {
                reason = "RunningActionStateMissing";
                return false;
            }

            if (!_states.ContainsKey(key))
            {
                reason = "RunningActionMissing";
                return false;
            }

            _states[key] = state;
            reason = "RunningActionUpdated";
            return true;
        }

        public bool Clear(RunningActionKey key)
        {
            return _states.Remove(key);
        }

        public int ClearByNpc(int npcId)
        {
            if (npcId <= 0 || _states.Count == 0)
                return 0;

            var keysToRemove = new List<RunningActionKey>();
            foreach (var pair in _states)
            {
                if (pair.Key.NpcId == npcId)
                    keysToRemove.Add(pair.Key);
            }

            return RemoveKeys(keysToRemove);
        }

        public int ClearByJob(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId) || _states.Count == 0)
                return 0;

            var keysToRemove = new List<RunningActionKey>();
            foreach (var pair in _states)
            {
                if (string.Equals(pair.Key.JobId, jobId, StringComparison.Ordinal))
                    keysToRemove.Add(pair.Key);
            }

            return RemoveKeys(keysToRemove);
        }

        public void ClearAll()
        {
            // ClearAll e' il path esplicito per reset transitori e load: il progress
            // running action e' volatile per contratto ARC-DEC-020.
            _states.Clear();
        }

        public IReadOnlyList<RunningActionProgressSnapshot> GetSnapshots()
        {
            // Esponiamo snapshot value-type, non il dizionario. Questo mantiene il
            // canale di osservabilita' leggibile senza consegnare authority mutabile
            // a UI, test o futura explainability.
            var snapshots = new List<RunningActionProgressSnapshot>(_states.Count);
            foreach (var pair in _states)
                snapshots.Add(pair.Value.ToSnapshot());

            return snapshots;
        }

        private int RemoveKeys(List<RunningActionKey> keysToRemove)
        {
            int removed = 0;
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                if (_states.Remove(keysToRemove[i]))
                    removed++;
            }

            return removed;
        }
    }
}
