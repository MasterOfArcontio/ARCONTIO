// =============================================================================
// MemoryBeliefDecisionExplainabilityStore.cs
// Namespace: Arcontio.Core
// Sessione: v0.05.42-EL_MBQD_Runtime_Registry
// =============================================================================
//
// Registry runtime passivo per l'Explainability Layer Memory, Belief, Query e
// Decision. Il file replica il modello architetturale gia' usato dall'EL
// Pathfinding: gli emitter scrivono snapshot bounded, la UI legge copie tramite
// ViewModel, la simulazione non dipende mai dal contenuto diagnostico.
// =============================================================================

using System;
using System.Collections.Generic;
using Arcontio.Core.Config;

namespace Arcontio.Core
{
    // =============================================================================
    // MemoryBeliefDecisionExplainabilityRingBuffer<T>
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ring buffer bounded usato dal registry EL-MBQD per conservare soltanto le
    /// trace recenti di una famiglia diagnostica.
    /// </para>
    ///
    /// <para><b>Contenitore passivo e limitato</b></para>
    /// <para>
    /// Il buffer non conosce NPC, World, BeliefStore o UI. Gestisce solo una finestra
    /// cronologica a capacita' fissa: quando la capacita' e' piena, la trace piu'
    /// vecchia viene sovrascritta dalla piu' recente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_items</b>: array circolare allocato una volta.</item>
    ///   <item><b>_start</b>: indice dell'elemento cronologicamente piu' vecchio.</item>
    ///   <item><b>_count</b>: numero di elementi validi presenti nel buffer.</item>
    ///   <item><b>Capacity</b>: capacita' normalizzata ad almeno 1.</item>
    /// </list>
    /// </summary>
    public sealed class MemoryBeliefDecisionExplainabilityRingBuffer<T>
    {
        private readonly T[] _items;
        private int _start;
        private int _count;

        public int Capacity { get; }
        public int Count => _count;

        // =============================================================================
        // MemoryBeliefDecisionExplainabilityRingBuffer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il ring buffer con capacita' bounded. Una capacita' non valida viene
        /// convertita a 1 per impedire stati nulli nella pipeline diagnostica.
        /// </para>
        /// </summary>
        public MemoryBeliefDecisionExplainabilityRingBuffer(int capacity)
        {
            Capacity = Math.Max(1, capacity);
            _items = new T[Capacity];
            _start = 0;
            _count = 0;
        }

        // =============================================================================
        // Add
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge un elemento recente al buffer, sovrascrivendo il piu' vecchio
        /// quando la finestra e' piena.
        /// </para>
        /// </summary>
        public void Add(T value)
        {
            if (_count < Capacity)
            {
                // Finche' esiste spazio libero, scriviamo in coda logica senza
                // muovere _start: l'ordine oldest-first resta naturale.
                int index = (_start + _count) % Capacity;
                _items[index] = value;
                _count++;
                return;
            }

            // A buffer pieno, _start punta proprio al record piu' vecchio: lo
            // rimpiazziamo e facciamo avanzare la finestra cronologica.
            _items[_start] = value;
            _start = (_start + 1) % Capacity;
        }

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota il buffer e libera le reference conservate nell'array interno.
        /// </para>
        /// </summary>
        public void Clear()
        {
            Array.Clear(_items, 0, _items.Length);
            _start = 0;
            _count = 0;
        }

        // =============================================================================
        // CopyTo
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia gli elementi validi in ordine cronologico nella lista di output.
        /// L'array interno non viene mai esposto.
        /// </para>
        /// </summary>
        public void CopyTo(List<T> output, bool clearOutput = true)
        {
            if (output == null)
                return;

            if (clearOutput)
                output.Clear();

            for (int i = 0; i < _count; i++)
            {
                int index = (_start + i) % Capacity;
                output.Add(_items[index]);
            }
        }

        // =============================================================================
        // TryGetNewest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce l'elemento piu' recente senza rimuoverlo dal buffer.
        /// </para>
        /// </summary>
        public bool TryGetNewest(out T value)
        {
            if (_count <= 0)
            {
                value = default;
                return false;
            }

            int index = (_start + _count - 1) % Capacity;
            value = _items[index];
            return true;
        }
    }

    // =============================================================================
    // MemoryBeliefDecisionExplainabilityNpcStore
    // =============================================================================
    /// <summary>
    /// <para>
    /// Store runtime passivo delle trace EL-MBQD di un singolo NPC.
    /// </para>
    ///
    /// <para><b>Famiglie diagnostiche separate</b></para>
    /// <para>
    /// Memory, Belief, Query, Decision e Bridge hanno ring buffer separati perche'
    /// hanno frequenze diverse e vengono visualizzati in tab diverse. Una raffica di
    /// memory trace non deve quindi espellere subito l'ultima decisione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_memoryTraces</b>: eventi di encoding/merge MemoryStore.</item>
    ///   <item><b>_beliefTraces</b>: mutazioni BeliefStore osservate.</item>
    ///   <item><b>_queryTraces</b>: chiamate BeliefQueryService.</item>
    ///   <item><b>_decisionTraces</b>: selezioni del Decision Layer.</item>
    ///   <item><b>_bridgeTraces</b>: adattamenti Decision -> Command legacy.</item>
    /// </list>
    /// </summary>
    public sealed class MemoryBeliefDecisionExplainabilityNpcStore
    {
        private readonly MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace> _memoryTraces;
        private readonly MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace> _beliefTraces;
        private readonly MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace> _queryTraces;
        private readonly MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace> _decisionTraces;
        private readonly MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace> _bridgeTraces;

        public int NpcId { get; }
        public long LatestTick { get; private set; }
        public int MemoryTraceCount => _memoryTraces.Count;
        public int BeliefTraceCount => _beliefTraces.Count;
        public int QueryTraceCount => _queryTraces.Count;
        public int DecisionTraceCount => _decisionTraces.Count;
        public int BridgeTraceCount => _bridgeTraces.Count;

        // =============================================================================
        // MemoryBeliefDecisionExplainabilityNpcStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea lo store per-NPC con capacita' indipendenti per ogni famiglia di trace.
        /// </para>
        /// </summary>
        public MemoryBeliefDecisionExplainabilityNpcStore(
            int npcId,
            int memoryCapacity,
            int beliefCapacity,
            int queryCapacity,
            int decisionCapacity,
            int bridgeCapacity)
        {
            NpcId = npcId;
            LatestTick = 0;

            _memoryTraces = new MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace>(memoryCapacity);
            _beliefTraces = new MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace>(beliefCapacity);
            _queryTraces = new MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace>(queryCapacity);
            _decisionTraces = new MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace>(decisionCapacity);
            _bridgeTraces = new MemoryBeliefDecisionExplainabilityRingBuffer<MemoryBeliefDecisionTrace>(bridgeCapacity);
        }

        // =============================================================================
        // AddTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inserisce una trace nella famiglia coerente con il suo <c>Kind</c>.
        /// </para>
        /// </summary>
        public void AddTrace(MemoryBeliefDecisionTrace trace)
        {
            if (trace == null)
                return;

            // LatestTick e' uno snapshot UI: non guida simulazione e serve solo a
            // mostrare quando l'NPC ha prodotto l'ultimo dato EL-MBQD.
            LatestTick = Math.Max(LatestTick, trace.Tick);

            switch (trace.Kind)
            {
                case MemoryBeliefDecisionTraceKind.Memory:
                    _memoryTraces.Add(trace);
                    break;
                case MemoryBeliefDecisionTraceKind.Belief:
                    _beliefTraces.Add(trace);
                    break;
                case MemoryBeliefDecisionTraceKind.Query:
                    _queryTraces.Add(trace);
                    break;
                case MemoryBeliefDecisionTraceKind.Decision:
                    _decisionTraces.Add(trace);
                    break;
                case MemoryBeliefDecisionTraceKind.Bridge:
                    _bridgeTraces.Add(trace);
                    break;
            }
        }

        public void CopyMemoryTracesTo(List<MemoryBeliefDecisionTrace> output, bool clearOutput = true)
            => _memoryTraces.CopyTo(output, clearOutput);

        public void CopyBeliefTracesTo(List<MemoryBeliefDecisionTrace> output, bool clearOutput = true)
            => _beliefTraces.CopyTo(output, clearOutput);

        public void CopyQueryTracesTo(List<MemoryBeliefDecisionTrace> output, bool clearOutput = true)
            => _queryTraces.CopyTo(output, clearOutput);

        public void CopyDecisionTracesTo(List<MemoryBeliefDecisionTrace> output, bool clearOutput = true)
            => _decisionTraces.CopyTo(output, clearOutput);

        public void CopyBridgeTracesTo(List<MemoryBeliefDecisionTrace> output, bool clearOutput = true)
            => _bridgeTraces.CopyTo(output, clearOutput);

        public bool TryGetLatestMemoryTrace(out MemoryBeliefDecisionTrace trace)
            => _memoryTraces.TryGetNewest(out trace);

        public bool TryGetLatestBeliefTrace(out MemoryBeliefDecisionTrace trace)
            => _beliefTraces.TryGetNewest(out trace);

        public bool TryGetLatestQueryTrace(out MemoryBeliefDecisionTrace trace)
            => _queryTraces.TryGetNewest(out trace);

        public bool TryGetLatestDecisionTrace(out MemoryBeliefDecisionTrace trace)
            => _decisionTraces.TryGetNewest(out trace);

        public bool TryGetLatestBridgeTrace(out MemoryBeliefDecisionTrace trace)
            => _bridgeTraces.TryGetNewest(out trace);
    }

    // =============================================================================
    // MemoryBeliefDecisionExplainabilityRegistry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Registry runtime passivo per-NPC delle trace EL-MBQD.
    /// </para>
    ///
    /// <para><b>Stesso pattern dell'EL Pathfinding</b></para>
    /// <para>
    /// Il registry e' scritto dagli emitter one-way e letto da adapter UI. Non apre
    /// file, non calcola decisioni, non interroga World e non crea dati simulativi:
    /// conserva soltanto snapshot gia' prodotti dai punti legittimi della pipeline.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_stores</b>: dizionario npcId -> store EL-MBQD.</item>
    ///   <item><b>_capacities</b>: capacita' bounded per famiglia diagnostica.</item>
    ///   <item><b>AddTrace</b>: entry point one-way usato dagli emitter.</item>
    ///   <item><b>TryGetNpcStore</b>: lettura passiva per ViewModel e UI.</item>
    /// </list>
    /// </summary>
    public sealed class MemoryBeliefDecisionExplainabilityRegistry
    {
        public const int DefaultMemoryCapacity = 80;
        public const int DefaultBeliefCapacity = 80;
        public const int DefaultQueryCapacity = 40;
        public const int DefaultDecisionCapacity = 24;
        public const int DefaultBridgeCapacity = 24;

        private readonly Dictionary<int, MemoryBeliefDecisionExplainabilityNpcStore> _stores;
        private readonly int _memoryCapacity;
        private readonly int _beliefCapacity;
        private readonly int _queryCapacity;
        private readonly int _decisionCapacity;
        private readonly int _bridgeCapacity;

        public int StoreCount => _stores.Count;

        // =============================================================================
        // MemoryBeliefDecisionExplainabilityRegistry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il registry usando capacita' esplicite o default conservativi.
        /// </para>
        /// </summary>
        public MemoryBeliefDecisionExplainabilityRegistry(
            int memoryCapacity = DefaultMemoryCapacity,
            int beliefCapacity = DefaultBeliefCapacity,
            int queryCapacity = DefaultQueryCapacity,
            int decisionCapacity = DefaultDecisionCapacity,
            int bridgeCapacity = DefaultBridgeCapacity)
        {
            _memoryCapacity = Math.Max(1, memoryCapacity);
            _beliefCapacity = Math.Max(1, beliefCapacity);
            _queryCapacity = Math.Max(1, queryCapacity);
            _decisionCapacity = Math.Max(1, decisionCapacity);
            _bridgeCapacity = Math.Max(1, bridgeCapacity);
            _stores = new Dictionary<int, MemoryBeliefDecisionExplainabilityNpcStore>(64);
        }

        // =============================================================================
        // AddTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inserisce una trace nel registry se configurazione, kind e npcId lo
        /// permettono.
        /// </para>
        /// </summary>
        public void AddTrace(MemoryBeliefDecisionExplainabilityParams config, MemoryBeliefDecisionTrace trace)
        {
            if (config == null || !config.enabled || trace == null)
                return;

            if (trace.NpcId <= 0 || !IsKindEnabled(config, trace.Kind) || !ShouldTrackNpc(config, trace.NpcId))
                return;

            if (!HasExplicitTrackedNpcList(config)
                && !_stores.ContainsKey(trace.NpcId)
                && _stores.Count >= Math.Max(1, config.maxTrackedNpcs))
                return;

            EnsureStore(trace.NpcId).AddTrace(trace);
        }

        // =============================================================================
        // TryGetNpcStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce lo store di un NPC soltanto se e' gia' stato popolato da trace.
        /// La lettura non crea store vuoti.
        /// </para>
        /// </summary>
        public bool TryGetNpcStore(int npcId, out MemoryBeliefDecisionExplainabilityNpcStore store)
        {
            return _stores.TryGetValue(npcId, out store) && store != null;
        }

        // =============================================================================
        // ClearNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove le trace EL-MBQD conservate per un singolo NPC.
        /// </para>
        /// </summary>
        public void ClearNpc(int npcId)
        {
            _stores.Remove(npcId);
        }

        // =============================================================================
        // ClearAll
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota tutto il registry runtime EL-MBQD.
        /// </para>
        /// </summary>
        public void ClearAll()
        {
            _stores.Clear();
        }

        private MemoryBeliefDecisionExplainabilityNpcStore EnsureStore(int npcId)
        {
            if (_stores.TryGetValue(npcId, out var store) && store != null)
                return store;

            store = new MemoryBeliefDecisionExplainabilityNpcStore(
                npcId,
                _memoryCapacity,
                _beliefCapacity,
                _queryCapacity,
                _decisionCapacity,
                _bridgeCapacity);

            _stores[npcId] = store;
            return store;
        }

        private static bool IsKindEnabled(MemoryBeliefDecisionExplainabilityParams config, MemoryBeliefDecisionTraceKind kind)
        {
            return kind switch
            {
                MemoryBeliefDecisionTraceKind.Memory => config.logMemory,
                MemoryBeliefDecisionTraceKind.Belief => config.logBelief,
                MemoryBeliefDecisionTraceKind.Query => config.logQuery,
                MemoryBeliefDecisionTraceKind.Decision => config.logDecision,
                MemoryBeliefDecisionTraceKind.Bridge => config.logBridge,
                _ => false
            };
        }

        private static bool ShouldTrackNpc(MemoryBeliefDecisionExplainabilityParams config, int npcId)
        {
            if (!HasExplicitTrackedNpcList(config))
                return true;

            for (int i = 0; i < config.trackedNpcIds.Length; i++)
            {
                if (config.trackedNpcIds[i] == npcId)
                    return true;
            }

            return false;
        }

        private static bool HasExplicitTrackedNpcList(MemoryBeliefDecisionExplainabilityParams config)
        {
            return config.trackedNpcIds != null && config.trackedNpcIds.Length > 0;
        }
    }
}
