using System.Collections.Generic;

namespace Arcontio.Core.Diagnostics
{
    // =============================================================================
    // Telemetry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Raccoglie contatori diagnostici legacy prodotti da systems e rules.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte diagnostico transitorio</b></para>
    /// <para>
    /// Questo tipo resta temporaneamente nelle firme di <c>ISystem</c> e
    /// <c>IRule</c> per evitare una migrazione larga durante la pulizia v0.12f.
    /// Quando e' disattivo non deve allocare dizionari runtime, non deve trattenere
    /// contatori e non deve produrre console output. In questo modo il vecchio
    /// canale Telemetry diventa un ponte inerte, assorbibile in futuro da EL/JSONL.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Enabled</b>: decide se i contatori vengono realmente registrati.</item>
    ///   <item><b>_counters</b>: viene creato solo quando Telemetry e' attiva.</item>
    ///   <item><b>EmptyCounters</b>: vista vuota condivisa quando Telemetry e' spenta.</item>
    /// </list>
    /// </summary>
    public sealed class Telemetry
    {
        private static readonly IReadOnlyDictionary<string, long> EmptyCounters =
            new Dictionary<string, long>(0);

        private readonly Dictionary<string, long> _counters;

        public Telemetry() : this(true)
        {
        }

        public Telemetry(bool enabled)
        {
            Enabled = enabled;
            _counters = enabled ? new Dictionary<string, long>() : null;
        }

        public bool Enabled { get; }

        public void Counter(string name, long delta)
        {
            if (!Enabled || _counters == null)
                return;

            if (_counters.TryGetValue(name, out var v)) _counters[name] = v + delta;
            else _counters[name] = delta;
        }

        public void Gauge(string name, long value)
        {
            if (!Enabled || _counters == null)
                return;

            _counters[name] = value;
        }

        public IReadOnlyDictionary<string, long> Counters => _counters ?? EmptyCounters;
    }
}
