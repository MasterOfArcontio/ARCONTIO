using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Arcontio.Core.Logging
{
    // =============================================================================
    // JsonlRuntimeLogHub
    // =============================================================================
    /// <summary>
    /// <para>
    /// Centro minimo di scrittura JSONL runtime. Riceve righe gia' serializzate dai
    /// sink diagnostici, le accoda in memoria con un limite esplicito e le scarica su
    /// disco in blocchi durante il normale ciclo di flush gia' usato dal runtime.
    /// </para>
    ///
    /// <para><b>Stabilizzazione diagnostica v0.11d.00b</b></para>
    /// <para>
    /// Questa classe non introduce thread, task paralleli o nuova architettura di
    /// explainability. Serve soltanto a rimuovere il pattern patologico
    /// "un evento = una apertura/scrittura/chiusura file", preservando i produttori
    /// e i consumatori esistenti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_writers</b>: scrittori distinti per canale e percorso file.</item>
    ///   <item><b>EnqueueLine</b>: ingresso leggero per una riga JSONL gia' pronta.</item>
    ///   <item><b>FlushAll</b>: scarico periodico chiamato dal percorso runtime esistente.</item>
    ///   <item><b>Shutdown</b>: chiusura difensiva dei file aperti.</item>
    /// </list>
    /// </summary>
    public static class JsonlRuntimeLogHub
    {
        private const int DefaultMaxQueuedLines = 4096;
        private const int DefaultMaxLinesPerFlush = 512;
        private const double DefaultFlushIntervalSeconds = 0.25;

        private static readonly Dictionary<string, JsonlBatchWriter> _writers = new();

        // =============================================================================
        // EnqueueLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Accoda una riga JSONL per un canale diagnostico. Il file non viene aperto
        /// qui: lo scrittore lo apre solo al primo flush utile. Se il canale e' saturo
        /// o congelato, la riga viene contata come persa e la simulazione prosegue.
        /// </para>
        /// </summary>
        public static bool EnqueueLine(string channelId, string filePath, string jsonLine)
        {
            if (string.IsNullOrWhiteSpace(channelId) ||
                string.IsNullOrWhiteSpace(filePath) ||
                string.IsNullOrWhiteSpace(jsonLine))
            {
                return false;
            }

            string key = channelId + "|" + filePath;
            if (!_writers.TryGetValue(key, out var writer) || writer == null)
            {
                writer = new JsonlBatchWriter(
                    channelId,
                    filePath,
                    DefaultMaxQueuedLines,
                    DefaultMaxLinesPerFlush,
                    TimeSpan.FromSeconds(DefaultFlushIntervalSeconds));

                _writers[key] = writer;
            }

            return writer.Enqueue(jsonLine);
        }

        // =============================================================================
        // FlushAll
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scarica su disco i canali che hanno superato l'intervallo minimo o che
        /// vengono forzati in chiusura. La chiamata e' intenzionalmente sincrona e
        /// deterministica: nessun thread nascosto interviene sul runtime.
        /// </para>
        /// </summary>
        public static void FlushAll(bool force = false)
        {
            foreach (var writer in _writers.Values)
            {
                writer?.Flush(force);
            }
        }

        // =============================================================================
        // Shutdown
        // =============================================================================
        /// <summary>
        /// <para>
        /// Chiude tutti gli scrittori ancora vivi, provando prima a scaricare le righe
        /// accodate. Dopo la chiusura il centro torna freddo e riaprira' i canali solo
        /// se un sink inviera' nuove righe.
        /// </para>
        /// </summary>
        public static void Shutdown()
        {
            foreach (var writer in _writers.Values)
            {
                writer?.Dispose();
            }

            _writers.Clear();
        }
    }

    // =============================================================================
    // JsonlBatchWriter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Scrittore JSONL a blocchi per un singolo canale/percorso. Mantiene una coda
    /// limitata, apre il file solo quando deve scaricare dati e si congela se la
    /// produzione supera la capacita' dichiarata.
    /// </para>
    ///
    /// <para><b>Protezione runtime</b></para>
    /// <para>
    /// La saturazione non blocca il tick e non espande la memoria: il canale viene
    /// congelato, le righe successive vengono contate come perse e al flush viene
    /// scritto un record sintetico di saturazione.
    /// </para>
    /// </summary>
    public sealed class JsonlBatchWriter : IDisposable
    {
        private readonly string _channelId;
        private readonly string _filePath;
        private readonly int _maxQueuedLines;
        private readonly int _maxLinesPerFlush;
        private readonly TimeSpan _flushInterval;
        private readonly Queue<string> _queue = new();

        private StreamWriter _writer;
        private DateTime _lastFlushUtc;
        private bool _frozen;
        private bool _disposed;
        private long _droppedLines;
        private bool _saturationRecordPending;

        public bool Frozen => _frozen;
        public long DroppedLines => _droppedLines;
        public int QueuedLines => _queue.Count;

        // =============================================================================
        // JsonlBatchWriter
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea uno scrittore freddo: il costruttore non apre file e non crea cartelle.
        /// L'accesso al disco avviene solo al primo flush con righe disponibili.
        /// </para>
        /// </summary>
        public JsonlBatchWriter(
            string channelId,
            string filePath,
            int maxQueuedLines,
            int maxLinesPerFlush,
            TimeSpan flushInterval)
        {
            _channelId = string.IsNullOrWhiteSpace(channelId) ? "unknown" : channelId;
            _filePath = filePath;
            _maxQueuedLines = Math.Max(1, maxQueuedLines);
            _maxLinesPerFlush = Math.Max(1, maxLinesPerFlush);
            _flushInterval = flushInterval <= TimeSpan.Zero ? TimeSpan.FromSeconds(0.25) : flushInterval;
            _lastFlushUtc = DateTime.UtcNow;
        }

        // =============================================================================
        // Enqueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inserisce una riga nella coda bounded. Se il canale e' gia' congelato o la
        /// coda e' piena, la riga non viene conservata e il contatore persi avanza.
        /// </para>
        /// </summary>
        public bool Enqueue(string jsonLine)
        {
            if (_disposed || string.IsNullOrWhiteSpace(jsonLine))
                return false;

            if (_frozen)
            {
                _droppedLines++;
                _saturationRecordPending = true;
                return false;
            }

            if (_queue.Count >= _maxQueuedLines)
            {
                _frozen = true;
                _droppedLines++;
                _saturationRecordPending = true;
                return false;
            }

            _queue.Enqueue(jsonLine);
            return true;
        }

        // =============================================================================
        // Flush
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive al massimo un blocco di righe per chiamata, rispettando l'intervallo
        /// minimo se il flush non e' forzato. La limitazione evita picchi troppo lunghi
        /// in un singolo frame.
        /// </para>
        /// </summary>
        public void Flush(bool force)
        {
            if (_disposed)
                return;

            if (_queue.Count <= 0 && !_saturationRecordPending)
                return;

            DateTime now = DateTime.UtcNow;
            if (!force && now - _lastFlushUtc < _flushInterval)
                return;

            try
            {
                EnsureWriter();

                int written = 0;
                while (_queue.Count > 0 && written < _maxLinesPerFlush)
                {
                    _writer.WriteLine(_queue.Dequeue());
                    written++;
                }

                if (_saturationRecordPending)
                {
                    _writer.WriteLine(BuildSaturationRecord());
                    _saturationRecordPending = false;
                }

                _writer.Flush();
                _lastFlushUtc = now;
            }
            catch
            {
                _frozen = true;
                _saturationRecordPending = false;
            }
        }

        // =============================================================================
        // Dispose
        // =============================================================================
        /// <summary>
        /// <para>
        /// Tenta uno scarico finale e poi chiude il file, se era stato aperto.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try { Flush(force: true); } catch { }
            try { _writer?.Dispose(); } catch { }

            _writer = null;
            _disposed = true;
        }

        private void EnsureWriter()
        {
            if (_writer != null)
                return;

            string directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var stream = new FileStream(
                _filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read);

            _writer = new StreamWriter(stream, new UTF8Encoding(false))
            {
                AutoFlush = false
            };
        }

        private string BuildSaturationRecord()
        {
            string timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            return "{\"kind\":\"jsonl_writer_saturation\",\"channel\":\"" +
                   Escape(_channelId) +
                   "\",\"dropped\":" +
                   _droppedLines.ToString(CultureInfo.InvariantCulture) +
                   ",\"frozen\":" +
                   (_frozen ? "true" : "false") +
                   ",\"utc\":\"" +
                   timestamp +
                   "\"}";
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }
    }
}
