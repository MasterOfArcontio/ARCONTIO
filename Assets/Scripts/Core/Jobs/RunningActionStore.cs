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
    ///   <item><b>Indice movimento</b>: mappa NPC -> running action movement attiva.</item>
    ///   <item><b>Register/Update</b>: API esplicite per start e replace controllato.</item>
    ///   <item><b>Clear</b>: cleanup per key, NPC, job o reset globale.</item>
    ///   <item><b>Snapshots</b>: enumerazione read-only difensiva per QA/futura EL.</item>
    /// </list>
    /// </summary>
    public sealed class RunningActionStore
    {
        private readonly Dictionary<RunningActionKey, RunningActionRuntimeState> _states = new();
        private readonly Dictionary<int, RunningActionKey> _activeMovementByNpc = new();

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
            TrackMovementIndex(key, state);
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
            TrackMovementIndex(key, state);
            reason = "RunningActionUpdated";
            return true;
        }

        public bool Clear(RunningActionKey key)
        {
            bool removed = _states.Remove(key);
            if (removed)
                UntrackMovementIndex(key);

            return removed;
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
            _activeMovementByNpc.Clear();
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

        // =============================================================================
        // TryGetActiveMovementSnapshotForNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Recupera lo snapshot read-only del movimento attivo di un NPC, se esiste.
        /// </para>
        ///
        /// <para><b>Principio architetturale: lettura visuale veloce senza authority</b></para>
        /// <para>
        /// ArcGraph deve poter chiedere il movimento di un actor senza ricevere lo
        /// store mutabile e senza costruire una lista completa di tutte le running
        /// action. Il metodo usa un indice minimale per NPC e restituisce uno
        /// snapshot copiato. Non avanza progress, non pulisce job, non modifica il
        /// <c>World</c> e non completa il movimento.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>npcId</b>: actor/NPC da interrogare.</item>
        ///   <item><b>snapshot</b>: copia value-only del movimento trovato.</item>
        ///   <item><b>return</b>: true solo per movement non terminale con segmento valido.</item>
        /// </list>
        /// </summary>
        public bool TryGetActiveMovementSnapshotForNpc(
            int npcId,
            out RunningActionProgressSnapshot snapshot)
        {
            snapshot = default;

            // Lettura O(1) pensata per ArcGraph: il rendering puo' interrogare molti
            // attori per frame/tick, quindi evitiamo sia la lista allocata da
            // GetSnapshots sia una scansione completa dello store per ogni NPC.
            if (npcId <= 0
                || !_activeMovementByNpc.TryGetValue(npcId, out var key)
                || !_states.TryGetValue(key, out var state)
                || state == null
                || state.Kind != RunningActionKind.Movement
                || state.IsTerminal
                || !state.Movement.IsValidStep)
            {
                return false;
            }

            snapshot = state.ToSnapshot();
            return true;
        }

        private int RemoveKeys(List<RunningActionKey> keysToRemove)
        {
            int removed = 0;
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                if (_states.Remove(keysToRemove[i]))
                {
                    UntrackMovementIndex(keysToRemove[i]);
                    removed++;
                }
            }

            return removed;
        }

        // =============================================================================
        // TrackMovementIndex
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna l'indice NPC -> running action movement quando uno stato viene
        /// registrato o sostituito.
        /// </para>
        ///
        /// <para><b>Principio architetturale: costo runtime spostato sui cambi di stato</b></para>
        /// <para>
        /// L'indice viene aggiornato solo quando lo store cambia, non durante ogni
        /// lettura grafica. Questo riduce il costo per ArcGraph mantenendo lo store
        /// ancora piccolo: un solo dizionario aggiuntivo con chiave NPC e valore
        /// <c>RunningActionKey</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>key</b>: chiave composta dello stato nello store principale.</item>
        ///   <item><b>state</b>: stato da indicizzare se e' movement valido.</item>
        /// </list>
        /// </summary>
        private void TrackMovementIndex(RunningActionKey key, RunningActionRuntimeState state)
        {
            // Solo i movement con segmento reale entrano nell'indice. Le wait action,
            // i lavori lunghi futuri e gli snapshot default non devono diventare
            // candidati visuali per errore.
            if (state != null
                && state.Kind == RunningActionKind.Movement
                && !state.IsTerminal
                && state.Movement.IsValidStep)
            {
                _activeMovementByNpc[key.NpcId] = key;
                return;
            }

            UntrackMovementIndex(key);
        }

        // =============================================================================
        // UntrackMovementIndex
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove dall'indice movimento la chiave indicata, se e' ancora quella
        /// associata all'NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: cleanup mirato dello stato derivato</b></para>
        /// <para>
        /// L'indice movimento e' derivato dallo store principale. Quando una action
        /// viene cancellata, l'indice deve essere pulito senza rimuovere per errore
        /// un eventuale movimento piu' recente dello stesso NPC. Per questo la
        /// rimozione confronta sempre la chiave completa prima di cancellare.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>key</b>: running action da rimuovere dall'indice derivato.</item>
        /// </list>
        /// </summary>
        private void UntrackMovementIndex(RunningActionKey key)
        {
            if (!_activeMovementByNpc.TryGetValue(key.NpcId, out var indexedKey))
                return;

            if (indexedKey.Equals(key))
                _activeMovementByNpc.Remove(key.NpcId);
        }
    }
}
