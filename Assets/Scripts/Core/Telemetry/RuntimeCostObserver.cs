using Arcontio.Core.Config;
using System;
using System.Diagnostics;

namespace Arcontio.Core
{
    // =============================================================================
    // RuntimeCostChannel
    // =============================================================================
    /// <summary>
    /// <para>
    /// Canali numerici dell'osservatorio costi runtime.
    /// </para>
    ///
    /// <para><b>Niente stringhe nei percorsi caldi</b></para>
    /// <para>
    /// I sistemi misurati usano enum invece di nomi testuali per evitare allocazioni
    /// e confronti stringa durante il tick. La traduzione in etichette leggibili
    /// verra' fatta solo da pannelli o JSONL futuri, fuori dal percorso spento.
    /// </para>
    /// </summary>
    public enum RuntimeCostChannel
    {
        ObjectPerception = 0,
        NpcPerception = 1,
        MemoryEncoding = 2,
        BeliefDecay = 3,
        BeliefQuery = 4,
        Decision = 5,
        JobExecution = 6,
        TokenEmission = 7,
        TokenDelivery = 8,
        TokenAssimilation = 9,
        Count = 10
    }

    // =============================================================================
    // RuntimeCostCounter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contatori operativi numerici raccolti dall'osservatorio costi runtime.
    /// </para>
    ///
    /// <para><b>Contatori di dominio, non log testuali</b></para>
    /// <para>
    /// Ogni valore rappresenta lavoro realmente compiuto o entita' attraversate:
    /// NPC, oggetti, coppie, eventi, trace, job o token. Non contiene messaggi,
    /// payload serializzati o informazioni di interfaccia.
    /// </para>
    /// </summary>
    public enum RuntimeCostCounter
    {
        ObjectPerceptionNpcScans = 0,
        ObjectPerceptionObjectChecks = 1,
        ObjectPerceptionSpottedEvents = 2,
        ObjectPerceptionFoodBeliefChecks = 3,
        NpcPerceptionPairChecks = 4,
        NpcPerceptionSpottedEvents = 5,
        MemoryEncodingEvents = 6,
        MemoryEncodingRuleChecks = 7,
        MemoryEncodingWitnessChecks = 8,
        MemoryEncodingTracesAdded = 9,
        BeliefDecayStores = 10,
        BeliefDecayEntriesUpdated = 11,
        BeliefQueryEntriesRead = 12,
        BeliefQueryCandidatesUsable = 13,
        BeliefQueryEvaluatorRuns = 14,
        DecisionNpcEvaluations = 15,
        DecisionJobsStarted = 16,
        DecisionRouteRejected = 17,
        JobExecutionActiveNpcs = 18,
        JobExecutionSteps = 19,
        TokenEmissionPairChecks = 20,
        TokenEmissionTokensCreated = 21,
        TokenDeliveryTokens = 22,
        TokenDeliveryDelivered = 23,
        TokenAssimilationTokens = 24,
        TokenAssimilationTracesAdded = 25,
        Count = 26
    }

    // =============================================================================
    // RuntimeCostObserver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Punto di aggancio opzionale per la futura misura dei costi runtime per tick,
    /// sistema e NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' congelabile</b></para>
    /// <para>
    /// Questo oggetto esiste solo quando la configurazione lo abilita. Quando la
    /// configurazione e' spenta, il <c>World</c> mantiene il riferimento nullo e i
    /// sistemi caldi potranno uscire con un solo controllo <c>null</c>. La classe non
    /// misura ancora nulla in `v0.17b`: prepara soltanto un punto stabile e sicuro
    /// per `v0.17c`.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CreateIfEnabled</b>: factory che restituisce null se il costo diagnostico deve essere zero.</item>
    ///   <item><b>Config normalizzata</b>: valori minimi protetti per evitare frequenze invalide.</item>
    ///   <item><b>ShouldSample</b>: helper cheap per decidere se un tick verra' misurato.</item>
    /// </list>
    /// </summary>
    public sealed class RuntimeCostObserver
    {
        private readonly int _sampleEveryTicks;
        private readonly long[] _durationTicksByChannel;
        private readonly long[] _sampleCountByChannel;
        private readonly long[] _counters;

        public bool TrackPerNpc { get; }
        public bool WriteJsonl { get; }
        public int MaxTicksInMemory { get; }
        public int JsonlFlushIntervalTicks { get; }
        public int JsonlMaxQueueSize { get; }
        public int JsonlMaxBatchSize { get; }
        public string JsonLogFileNamePattern { get; }

        private RuntimeCostObserver(RuntimeCostObserverParams config)
        {
            _sampleEveryTicks = Math.Max(1, config?.sampleEveryTicks ?? 1);
            _durationTicksByChannel = new long[(int)RuntimeCostChannel.Count];
            _sampleCountByChannel = new long[(int)RuntimeCostChannel.Count];
            _counters = new long[(int)RuntimeCostCounter.Count];
            TrackPerNpc = config != null && config.trackPerNpc;
            WriteJsonl = config != null && config.writeJsonl;
            MaxTicksInMemory = Math.Max(1, config?.maxTicksInMemory ?? 256);
            JsonlFlushIntervalTicks = Math.Max(1, config?.jsonlFlushIntervalTicks ?? 25);
            JsonlMaxQueueSize = Math.Max(1, config?.jsonlMaxQueueSize ?? 4096);
            JsonlMaxBatchSize = Math.Max(1, config?.jsonlMaxBatchSize ?? 512);
            JsonLogFileNamePattern = string.IsNullOrWhiteSpace(config?.jsonLogFileNamePattern)
                ? "arcontio_runtime_cost_{yyyyMMdd_HHmmss}.jsonl"
                : config.jsonLogFileNamePattern;
        }

        // =============================================================================
        // CreateIfEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea l'osservatorio solo se la configurazione e' esplicitamente attiva.
        /// </para>
        ///
        /// <para>
        /// Questa factory e' il guardrail principale della fase: il percorso spento
        /// non deve allocare registri, buffer o strutture di misura. Il chiamante deve
        /// conservare il <c>null</c> e usarlo come segnale di uscita immediata.
        /// </para>
        /// </summary>
        public static RuntimeCostObserver CreateIfEnabled(RuntimeCostObserverParams config)
        {
            if (config == null || !config.enabled)
                return null;

            return new RuntimeCostObserver(config);
        }

        // =============================================================================
        // ShouldSample
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se il tick corrente rientra nella cadenza di campionamento.
        /// </para>
        ///
        /// <para>
        /// In `v0.17b` questo metodo non e' ancora collegato ai sistemi caldi. In
        /// `v0.17c` permettera' di evitare misure continue quando si vuole un profilo
        /// piu' leggero.
        /// </para>
        /// </summary>
        public bool ShouldSample(long tick)
        {
            return _sampleEveryTicks <= 1 || tick % _sampleEveryTicks == 0;
        }

        // =============================================================================
        // BeginSample
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il timestamp grezzo di inizio misura.
        /// </para>
        ///
        /// <para>
        /// Usa `Stopwatch.GetTimestamp()` statico per evitare istanze temporanee. Il
        /// chiamante deve invocarlo solo dopo avere verificato che l'osservatorio non
        /// sia nullo e che il tick sia campionato.
        /// </para>
        /// </summary>
        public long BeginSample()
        {
            return Stopwatch.GetTimestamp();
        }

        // =============================================================================
        // EndSample
        // =============================================================================
        /// <summary>
        /// <para>
        /// Accumula la durata grezza di un canale runtime.
        /// </para>
        /// </summary>
        public void EndSample(RuntimeCostChannel channel, long startTimestamp)
        {
            int index = (int)channel;
            if (index < 0 || index >= _durationTicksByChannel.Length)
                return;

            long elapsed = Stopwatch.GetTimestamp() - startTimestamp;
            if (elapsed < 0)
                elapsed = 0;

            _durationTicksByChannel[index] += elapsed;
            _sampleCountByChannel[index]++;
        }

        // =============================================================================
        // AddCounter
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge un valore a un contatore operativo numerico.
        /// </para>
        /// </summary>
        public void AddCounter(RuntimeCostCounter counter, long value)
        {
            if (value == 0)
                return;

            int index = (int)counter;
            if (index < 0 || index >= _counters.Length)
                return;

            _counters[index] += value;
        }

        public long GetDurationTicks(RuntimeCostChannel channel)
        {
            int index = (int)channel;
            return index >= 0 && index < _durationTicksByChannel.Length ? _durationTicksByChannel[index] : 0L;
        }

        public long GetSampleCount(RuntimeCostChannel channel)
        {
            int index = (int)channel;
            return index >= 0 && index < _sampleCountByChannel.Length ? _sampleCountByChannel[index] : 0L;
        }

        public long GetCounter(RuntimeCostCounter counter)
        {
            int index = (int)counter;
            return index >= 0 && index < _counters.Length ? _counters[index] : 0L;
        }
    }
}
