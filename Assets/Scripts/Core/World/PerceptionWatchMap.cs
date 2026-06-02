using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // PerceptionWatchMap
    // =============================================================================
    /// <summary>
    /// <para>
    /// Mappa runtime leggera delle dipendenze percettive tra entita' osservabili e
    /// NPC osservatori.
    /// </para>
    ///
    /// <para><b>Principio architetturale: invalidazione percettiva event-driven</b></para>
    /// <para>
    /// La struttura non conserva celle vuote e non sostituisce la percezione. Registra
    /// solo quali NPC hanno visto un oggetto o un altro NPC nell'ultimo ciclo
    /// percettivo utile. Quando una di quelle entita' cambia, la mappa marca sporchi
    /// gli osservatori interessati. La v0.20 usera' questo stato insieme alla cadenza
    /// percettiva per decidere quando saltare o rieseguire uno scan.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Oggetti</b>: <c>objectId -> osservatori + modificato</c>.</item>
    ///   <item><b>NPC osservati</b>: <c>observedNpcId -> osservatori + modificato</c>.</item>
    ///   <item><b>NPC sporchi</b>: stato separato consumabile in futuro dalla percezione cadenzata.</item>
    /// </list>
    /// </summary>
    public sealed class PerceptionWatchMap
    {
        private readonly Dictionary<int, ObservedEntityState> _objects = new(512);
        private readonly Dictionary<int, ObservedEntityState> _npcs = new(512);
        private readonly Dictionary<int, DirtyPerceptionState> _dirtyNpcs = new(256);
        private readonly List<int> _entityGarbage = new(128);

        // =============================================================================
        // RecordObjectObserved
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra che un NPC ha visto un oggetto nel ciclo percettivo corrente.
        /// </para>
        /// </summary>
        public void RecordObjectObserved(int objectId, int observerNpcId)
        {
            if (objectId <= 0 || observerNpcId <= 0)
                return;

            GetOrCreate(_objects, objectId).AddObserver(observerNpcId);
        }

        // =============================================================================
        // RecordNpcObserved
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra che un NPC osservatore ha visto un altro NPC.
        /// </para>
        /// </summary>
        public void RecordNpcObserved(int observedNpcId, int observerNpcId)
        {
            if (observedNpcId <= 0 || observerNpcId <= 0 || observedNpcId == observerNpcId)
                return;

            GetOrCreate(_npcs, observedNpcId).AddObserver(observerNpcId);
        }

        public void ClearObjectObservers()
        {
            ClearObservers(_objects);
        }

        public void ClearNpcObservers()
        {
            ClearObservers(_npcs);
        }

        public void MarkObjectModified(int objectId)
        {
            if (objectId <= 0)
                return;

            GetOrCreate(_objects, objectId).Modified = true;
        }

        public void MarkNpcModified(int npcId)
        {
            if (npcId <= 0)
                return;

            GetOrCreate(_npcs, npcId).Modified = true;
        }

        public void RemoveObject(int objectId)
        {
            _objects.Remove(objectId);
        }

        public void RemoveNpc(int npcId)
        {
            _npcs.Remove(npcId);
            _dirtyNpcs.Remove(npcId);

            foreach (var pair in _objects)
                pair.Value.RemoveObserver(npcId);

            foreach (var pair in _npcs)
                pair.Value.RemoveObserver(npcId);
        }

        // =============================================================================
        // PropagateModifiedEntitiesToDirtyNpcs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Trasforma le entita' osservate e modificate in NPC osservatori sporchi.
        /// </para>
        /// </summary>
        public int PropagateModifiedEntitiesToDirtyNpcs(int tick, string reason)
        {
            int dirtyCount = 0;
            dirtyCount += PropagateModifiedMap(_objects, tick, reason);
            dirtyCount += PropagateModifiedMap(_npcs, tick, reason);
            return dirtyCount;
        }

        public void MarkNpcPerceptionDirty(int npcId, int tick, string reason)
        {
            if (npcId <= 0)
                return;

            _dirtyNpcs[npcId] = new DirtyPerceptionState
            {
                Tick = tick,
                Reason = string.IsNullOrWhiteSpace(reason) ? "PerceptionDependencyChanged" : reason
            };
        }

        public bool IsNpcPerceptionDirty(int npcId)
        {
            return _dirtyNpcs.ContainsKey(npcId);
        }

        public bool TryConsumeNpcPerceptionDirty(int npcId, out int tick, out string reason)
        {
            tick = 0;
            reason = string.Empty;
            if (!_dirtyNpcs.TryGetValue(npcId, out var state))
                return false;

            tick = state.Tick;
            reason = state.Reason;
            _dirtyNpcs.Remove(npcId);
            return true;
        }

        public int GetObjectObserverCount(int objectId)
        {
            return _objects.TryGetValue(objectId, out var state)
                ? state.ObserverNpcIds.Count
                : 0;
        }

        public int GetNpcObserverCount(int observedNpcId)
        {
            return _npcs.TryGetValue(observedNpcId, out var state)
                ? state.ObserverNpcIds.Count
                : 0;
        }

        private int PropagateModifiedMap(Dictionary<int, ObservedEntityState> map, int tick, string reason)
        {
            int dirtyCount = 0;
            foreach (var pair in map)
            {
                var state = pair.Value;
                if (state == null || !state.Modified)
                    continue;

                for (int i = 0; i < state.ObserverNpcIds.Count; i++)
                {
                    MarkNpcPerceptionDirty(state.ObserverNpcIds[i], tick, reason);
                    dirtyCount++;
                }

                state.Modified = false;
            }

            return dirtyCount;
        }

        private static ObservedEntityState GetOrCreate(Dictionary<int, ObservedEntityState> map, int id)
        {
            if (!map.TryGetValue(id, out var state))
            {
                state = new ObservedEntityState();
                map[id] = state;
            }

            return state;
        }

        private void ClearObservers(Dictionary<int, ObservedEntityState> map)
        {
            _entityGarbage.Clear();
            foreach (var pair in map)
            {
                pair.Value.ObserverNpcIds.Clear();
                if (!pair.Value.Modified)
                    _entityGarbage.Add(pair.Key);
            }

            for (int i = 0; i < _entityGarbage.Count; i++)
                map.Remove(_entityGarbage[i]);
        }

        private sealed class ObservedEntityState
        {
            public readonly List<int> ObserverNpcIds = new(4);
            public bool Modified;

            public void AddObserver(int npcId)
            {
                for (int i = 0; i < ObserverNpcIds.Count; i++)
                {
                    if (ObserverNpcIds[i] == npcId)
                        return;
                }

                ObserverNpcIds.Add(npcId);
            }

            public void RemoveObserver(int npcId)
            {
                for (int i = ObserverNpcIds.Count - 1; i >= 0; i--)
                {
                    if (ObserverNpcIds[i] == npcId)
                        ObserverNpcIds.RemoveAt(i);
                }
            }
        }

        private struct DirtyPerceptionState
        {
            public int Tick;
            public string Reason;
        }
    }
}
