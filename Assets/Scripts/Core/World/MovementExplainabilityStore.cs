// =============================================================================
// MovementExplainabilityStore.cs
// Namespace: Arcontio.Core
// Sessione: v0.04.1.c-EL_Pathfinding_Ring_Buffer_Registry
// =============================================================================
//
// Contenitori passivi per le trace EL del pathfinding.
// Non contengono logica di pathfinding, non interrogano World, non emettono
// eventi di simulazione e non contengono codice UI.
// =============================================================================

using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // MovementExplainabilityRingBuffer<T>
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ring buffer generico e bounded usato dallo store EL per conservare solo le
    /// trace recenti. Quando la capacita' e' piena, il prossimo inserimento sovrascrive
    /// implicitamente l'elemento piu' vecchio.
    /// </para>
    ///
    /// <para><b>Contenitore passivo</b></para>
    /// <para>
    /// Questo tipo non conosce NPC, pathfinding, UI o configurazione globale. Gestisce
    /// soltanto una sequenza limitata di valori e fornisce copie snapshot al chiamante.
    /// E' quindi una funzione di contenimento dati, non una funzione simulativa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_items</b>: array circolare che conserva i valori recenti.</item>
    ///   <item><b>_start</b>: indice dell'elemento piu' vecchio attualmente valido.</item>
    ///   <item><b>_count</b>: numero di elementi validi presenti nel buffer.</item>
    ///   <item><b>Capacity</b>: limite massimo, normalizzato ad almeno 1.</item>
    /// </list>
    /// </summary>
    public sealed class MovementExplainabilityRingBuffer<T>
    {
        private readonly T[] _items;
        private int _start;
        private int _count;

        public int Capacity { get; }
        public int Count => _count;

        // =============================================================================
        // MovementExplainabilityRingBuffer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un ring buffer con capacita' bounded. La capacita' ricevuta viene
        /// normalizzata ad almeno 1 per mantenere il contenitore sempre operativo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>capacity</b>: limite richiesto dal chiamante.</item>
        ///   <item><b>_items</b>: array allocato una sola volta.</item>
        ///   <item><b>_start/_count</b>: indici inizializzati a zero.</item>
        /// </list>
        /// </summary>
        public MovementExplainabilityRingBuffer(int capacity)
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
        /// Aggiunge un valore recente al buffer. Se il buffer non e' pieno, il valore
        /// viene scritto dopo l'ultimo elemento valido; se e' pieno, sovrascrive
        /// l'elemento piu' vecchio e fa avanzare la finestra.
        /// </para>
        ///
        /// <para><b>Oldest-first overwrite</b></para>
        /// <para>
        /// L'EL deve restare bounded: quando la finestra e' piena, perdere la trace
        /// piu' vecchia e' il comportamento previsto dal contratto.
        /// </para>
        /// </summary>
        public void Add(T value)
        {
            if (_count < Capacity)
            {
                int index = (_start + _count) % Capacity;
                _items[index] = value;
                _count++;
                return;
            }

            _items[_start] = value;
            _start = (_start + 1) % Capacity;
        }

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota il buffer e rimuove le reference conservate dall'array interno.
        /// Il buffer resta riutilizzabile dopo il clear.
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
        /// Copia gli elementi validi nella lista di output, in ordine cronologico dal
        /// piu' vecchio al piu' recente. Non espone mai l'array interno.
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
        /// Restituisce l'elemento piu' recente se il buffer contiene almeno una trace.
        /// Serve al futuro ViewModel per leggere rapidamente lo stato corrente.
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
    // MovementExplainabilityNpcStore
    // =============================================================================
    /// <summary>
    /// <para>
    /// Store passivo delle trace EL di un singolo NPC. Contiene tre ring buffer
    /// separati: intent, pianificazioni e eventi di esecuzione.
    /// </para>
    ///
    /// <para><b>Separazione dati / simulazione</b></para>
    /// <para>
    /// Questo store non decide, non pianifica e non legge il mondo. Riceve trace gia'
    /// costruite e le conserva in modo bounded. I futuri emitter potranno scriverlo,
    /// mentre UI e log potranno copiarne snapshot tramite metodi dedicati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_intentTraces</b>: ring buffer delle ultime MovementIntentTrace.</item>
    ///   <item><b>_planTraces</b>: ring buffer delle ultime PathPlanTrace.</item>
    ///   <item><b>_executionEvents</b>: ring buffer degli ultimi PathExecutionEvent.</item>
    ///   <item><b>CurrentIntentId</b>: ultimo intent EL noto per questo NPC.</item>
    ///   <item><b>CurrentPlanId</b>: ultimo piano EL noto per questo NPC.</item>
    /// </list>
    /// </summary>
    public sealed class MovementExplainabilityNpcStore
    {
        private readonly MovementExplainabilityRingBuffer<MovementIntentTrace> _intentTraces;
        private readonly MovementExplainabilityRingBuffer<PathPlanTrace> _planTraces;
        private readonly MovementExplainabilityRingBuffer<PathExecutionEvent> _executionEvents;

        public int NpcId { get; }
        public int CurrentIntentId { get; private set; }
        public int CurrentPlanId { get; private set; }

        public int IntentTraceCount => _intentTraces.Count;
        public int PlanTraceCount => _planTraces.Count;
        public int ExecutionEventCount => _executionEvents.Count;

        // =============================================================================
        // MovementExplainabilityNpcStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea lo store per un singolo NPC usando capacita' separate per intent,
        /// piani ed eventi. Gli eventi possono essere piu' numerosi, soprattutto a
        /// verbosita' alta.
        /// </para>
        /// </summary>
        public MovementExplainabilityNpcStore(
            int npcId,
            int intentCapacity,
            int planCapacity,
            int eventCapacity)
        {
            NpcId = npcId;
            CurrentIntentId = 0;
            CurrentPlanId = 0;

            _intentTraces = new MovementExplainabilityRingBuffer<MovementIntentTrace>(intentCapacity);
            _planTraces = new MovementExplainabilityRingBuffer<PathPlanTrace>(planCapacity);
            _executionEvents = new MovementExplainabilityRingBuffer<PathExecutionEvent>(eventCapacity);
        }

        // =============================================================================
        // AddIntentTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge una trace di nascita intent allo store dell'NPC e aggiorna
        /// `CurrentIntentId`. La trace null viene ignorata.
        /// </para>
        /// </summary>
        public void AddIntentTrace(MovementIntentTrace trace)
        {
            if (trace == null)
                return;

            CurrentIntentId = trace.IntentId;
            _intentTraces.Add(trace);
        }

        // =============================================================================
        // AddPlanTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge una trace di pianificazione allo store dell'NPC e aggiorna
        /// `CurrentPlanId`. Lo store non verifica il piano contro il world state.
        /// </para>
        /// </summary>
        public void AddPlanTrace(PathPlanTrace trace)
        {
            if (trace == null)
                return;

            CurrentPlanId = trace.PlanId;
            _planTraces.Add(trace);
        }

        // =============================================================================
        // AddExecutionEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge un evento di esecuzione allo store dell'NPC. Se l'evento porta
        /// `IntentId` o `PlanId` positivi, aggiorna anche gli id correnti osservati.
        /// </para>
        /// </summary>
        public void AddExecutionEvent(PathExecutionEvent evt)
        {
            if (evt == null)
                return;

            if (evt.IntentId > 0)
                CurrentIntentId = evt.IntentId;

            if (evt.PlanId > 0)
                CurrentPlanId = evt.PlanId;

            _executionEvents.Add(evt);
        }

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cancella tutte le trace dello store e azzera gli id correnti EL. Il reset
        /// riguarda solo dati di debug/spiegazione, non il movimento reale.
        /// </para>
        /// </summary>
        public void Clear()
        {
            _intentTraces.Clear();
            _planTraces.Clear();
            _executionEvents.Clear();
            CurrentIntentId = 0;
            CurrentPlanId = 0;
        }

        // =============================================================================
        // CopyIntentTracesTo
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia le intent trace in ordine cronologico nella lista del chiamante,
        /// senza esporre il ring buffer interno.
        /// </para>
        /// </summary>
        public void CopyIntentTracesTo(List<MovementIntentTrace> output, bool clearOutput = true)
            => _intentTraces.CopyTo(output, clearOutput);

        // =============================================================================
        // CopyPlanTracesTo
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia le plan trace in ordine cronologico nella lista del chiamante,
        /// senza esporre il ring buffer interno.
        /// </para>
        /// </summary>
        public void CopyPlanTracesTo(List<PathPlanTrace> output, bool clearOutput = true)
            => _planTraces.CopyTo(output, clearOutput);

        // =============================================================================
        // CopyExecutionEventsTo
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia gli eventi di esecuzione in ordine cronologico nella lista del
        /// chiamante, preparando timeline UI e sink log futuri.
        /// </para>
        /// </summary>
        public void CopyExecutionEventsTo(List<PathExecutionEvent> output, bool clearOutput = true)
            => _executionEvents.CopyTo(output, clearOutput);

        // =============================================================================
        // TryGetLatestIntentTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce l'ultima intent trace disponibile per questo NPC.
        /// </para>
        /// </summary>
        public bool TryGetLatestIntentTrace(out MovementIntentTrace trace)
            => _intentTraces.TryGetNewest(out trace);

        // =============================================================================
        // TryGetLatestPlanTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce l'ultima plan trace disponibile per questo NPC.
        /// </para>
        /// </summary>
        public bool TryGetLatestPlanTrace(out PathPlanTrace trace)
            => _planTraces.TryGetNewest(out trace);

        // =============================================================================
        // TryGetLatestExecutionEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce l'ultimo evento di esecuzione disponibile per questo NPC.
        /// </para>
        /// </summary>
        public bool TryGetLatestExecutionEvent(out PathExecutionEvent evt)
            => _executionEvents.TryGetNewest(out evt);
    }

    // =============================================================================
    // MovementExplainabilityRegistry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Registro passivo per-NPC delle trace EL pathfinding. Possiede uno store per
    /// ogni NPC tracciato e offre API one-way per inserire intent, piani ed eventi.
    /// </para>
    ///
    /// <para><b>Registry osservatore</b></para>
    /// <para>
    /// Il registry non conosce `World`, non pubblica `ISimEvent`, non consulta
    /// BeliefStore e non contiene logica UI. E' un punto di raccolta dati pensato per
    /// essere letto in futuro da adapter, log sink e pannello runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_stores</b>: dizionario npcId -> MovementExplainabilityNpcStore.</item>
    ///   <item><b>_intentCapacity</b>: capacita' default intent trace.</item>
    ///   <item><b>_planCapacity</b>: capacita' default plan trace.</item>
    ///   <item><b>_eventCapacity</b>: capacita' default execution event.</item>
    /// </list>
    /// </summary>
    public sealed class MovementExplainabilityRegistry
    {
        public const int DefaultIntentCapacity = 10;
        public const int DefaultPlanCapacity = 10;
        public const int DefaultEventCapacity = 60;
        public const int DefaultHighVerbosityEventCapacity = 200;

        private readonly Dictionary<int, MovementExplainabilityNpcStore> _stores;
        private readonly int _intentCapacity;
        private readonly int _planCapacity;
        private readonly int _eventCapacity;

        public int StoreCount => _stores.Count;

        // =============================================================================
        // MovementExplainabilityRegistry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un registry EL con capacita' bounded per gli store NPC generati in modo
        /// lazy. La sessione corrente non introduce ancora config, quindi i default
        /// sono coerenti con il contratto della sessione A.
        /// </para>
        /// </summary>
        public MovementExplainabilityRegistry(
            int intentCapacity = DefaultIntentCapacity,
            int planCapacity = DefaultPlanCapacity,
            int eventCapacity = DefaultEventCapacity)
        {
            _intentCapacity = Math.Max(1, intentCapacity);
            _planCapacity = Math.Max(1, planCapacity);
            _eventCapacity = Math.Max(1, eventCapacity);
            _stores = new Dictionary<int, MovementExplainabilityNpcStore>(64);
        }

        // =============================================================================
        // EmitIntent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inserisce una `MovementIntentTrace` nello store dell'NPC corrispondente,
        /// creando lo store se necessario. Il metodo non restituisce dati utili alla
        /// simulazione e ignora trace null.
        /// </para>
        /// </summary>
        public void EmitIntent(MovementIntentTrace trace)
        {
            if (trace == null)
                return;

            EnsureStore(trace.NpcId).AddIntentTrace(trace);
        }

        // =============================================================================
        // EmitPlan
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inserisce una `PathPlanTrace` nello store dell'NPC corrispondente, creando
        /// lo store se necessario. Non valida il piano contro il world state.
        /// </para>
        /// </summary>
        public void EmitPlan(PathPlanTrace trace)
        {
            if (trace == null)
                return;

            EnsureStore(trace.NpcId).AddPlanTrace(trace);
        }

        // =============================================================================
        // EmitExecutionEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inserisce un `PathExecutionEvent` nello store dell'NPC corrispondente,
        /// creando lo store se necessario. Il registry conserva soltanto la timeline
        /// prodotta dall'emitter futuro.
        /// </para>
        /// </summary>
        public void EmitExecutionEvent(PathExecutionEvent evt)
        {
            if (evt == null)
                return;

            EnsureStore(evt.NpcId).AddExecutionEvent(evt);
        }

        // =============================================================================
        // TryGetNpcStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce lo store di un NPC se e' gia' stato creato da almeno una trace.
        /// Non crea store vuoti durante letture passive.
        /// </para>
        /// </summary>
        public bool TryGetNpcStore(int npcId, out MovementExplainabilityNpcStore store)
        {
            return _stores.TryGetValue(npcId, out store) && store != null;
        }

        // =============================================================================
        // ClearNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove completamente lo store EL di un NPC dal registry. Questa operazione
        /// cancella solo dati EL e non modifica NPC, intent o pathfinding reale.
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
        /// Rimuove tutti gli store EL dal registry. Pensato per reset di sessione,
        /// cambio scena o disattivazione futura dell'EL.
        /// </para>
        /// </summary>
        public void ClearAll()
        {
            _stores.Clear();
        }

        // =============================================================================
        // EnsureStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Recupera lo store di un NPC o lo crea in modo lazy con le capacita' del
        /// registry. Lo store viene creato solo quando arriva una trace.
        /// </para>
        /// </summary>
        private MovementExplainabilityNpcStore EnsureStore(int npcId)
        {
            if (_stores.TryGetValue(npcId, out var store) && store != null)
                return store;

            store = new MovementExplainabilityNpcStore(
                npcId,
                _intentCapacity,
                _planCapacity,
                _eventCapacity);

            _stores[npcId] = store;
            return store;
        }
    }
}
