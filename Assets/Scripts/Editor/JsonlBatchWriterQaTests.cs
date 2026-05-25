using System;
using System.IO;
using Arcontio.Core.Logging;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // JsonlBatchWriterQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per lo scrittore JSONL batchato introdotto in v0.11d.00b.
    /// Verifica il contratto minimo richiesto dalla stabilizzazione runtime:
    /// apertura file ritardata, scrittura a blocchi e saturazione leggibile.
    /// </para>
    ///
    /// <para><b>Diagnostica non comportamentale</b></para>
    /// <para>
    /// Il test non crea World, non esegue sistemi e non modifica lo stato
    /// simulativo. Controlla soltanto il percorso di scrittura file usato dai sink
    /// diagnostici.
    /// </para>
    /// </summary>
    public sealed class JsonlBatchWriterQaTests
    {
        // =============================================================================
        // WriterOpensLazilyFlushesBatchAndReportsSaturation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Protegge le tre proprieta' essenziali dello scrittore: nessun file creato
        /// all'accodamento, flush esplicito a blocchi e record sintetico quando la
        /// coda supera il limite.
        /// </para>
        /// </summary>
        [Test]
        public void WriterOpensLazilyFlushesBatchAndReportsSaturation()
        {
            string directory = Path.Combine(Application.persistentDataPath, "Arcontio_QA_JsonlBatchWriter");
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, "qa_jsonl_batch_writer.jsonl");
            if (File.Exists(path))
                File.Delete(path);

            using (var writer = new JsonlBatchWriter(
                       "qa",
                       path,
                       maxQueuedLines: 2,
                       maxLinesPerFlush: 10,
                       flushInterval: TimeSpan.FromMinutes(1)))
            {
                Assert.That(writer.Enqueue("{\"kind\":\"first\"}"), Is.True);
                Assert.That(writer.Enqueue("{\"kind\":\"second\"}"), Is.True);

                Assert.That(File.Exists(path), Is.False);

                Assert.That(writer.Enqueue("{\"kind\":\"third\"}"), Is.False);
                Assert.That(writer.Frozen, Is.True);
                Assert.That(writer.DroppedLines, Is.EqualTo(1));

                writer.Flush(force: true);
            }

            string jsonl = File.ReadAllText(path);
            Assert.That(jsonl, Does.Contain("\"kind\":\"first\""));
            Assert.That(jsonl, Does.Contain("\"kind\":\"second\""));
            Assert.That(jsonl, Does.Contain("\"kind\":\"jsonl_writer_saturation\""));
            Assert.That(jsonl, Does.Contain("\"channel\":\"qa\""));
            Assert.That(jsonl, Does.Contain("\"dropped\":1"));
            Assert.That(jsonl, Does.Contain("\"frozen\":true"));
            Assert.That(jsonl, Does.Not.Contain("\"kind\":\"third\""));
        }
    }
}
