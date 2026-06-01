using Arcontio.Core.Config;
using System;

namespace Arcontio.Core
{
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
    }
}
