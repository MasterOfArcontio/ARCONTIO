using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobCommandBufferQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il command buffer del Job System.
    /// </para>
    ///
    /// <para><b>Comandi prodotti ma non eseguiti</b></para>
    /// <para>
    /// Il test dimostra che gli step possono ricevere un buffer nel contesto e che il
    /// buffer conserva comandi ordinati senza chiamare <c>Execute</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Enqueue</b>: accetta comandi validi e rifiuta null.</item>
    ///   <item><b>Snapshot</b>: produce copia ordinata.</item>
    ///   <item><b>Context</b>: trasporta il buffer verso gli executor.</item>
    /// </list>
    /// </summary>
    public sealed class JobCommandBufferQaTests
    {
        // =============================================================================
        // CommandBufferStoresCommandsWithoutExecutingThem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il buffer raccolga comandi e li esponga in modo difensivo.
        /// </para>
        ///
        /// <para><b>Flush esterno</b></para>
        /// <para>
        /// Il Job System non deve mutare il World durante la produzione dello step.
        /// L'orchestratore futuro leggerà il buffer e applichera' i comandi nel punto
        /// corretto del tick.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>FakeCommand</b>: comando di test non eseguito.</item>
        ///   <item><b>Snapshot</b>: copia array.</item>
        ///   <item><b>Clear</b>: svuota dopo flush simulato.</item>
        /// </list>
        /// </summary>
        [Test]
        public void CommandBufferStoresCommandsWithoutExecutingThem()
        {
            // Arrange: buffer e comando finto, senza World o MessageBus.
            var buffer = new JobCommandBuffer();
            var command = new FakeCommand("job-command");

            // Act: accodiamo, leggiamo snapshot e poi puliamo.
            var nullAccepted = buffer.Enqueue(null);
            var accepted = buffer.Enqueue(command);
            var snapshot = buffer.Snapshot();
            buffer.Clear();

            // Assert: il comando e' presente ma non e' mai stato eseguito.
            Assert.That(nullAccepted, Is.False);
            Assert.That(accepted, Is.True);
            Assert.That(snapshot.Length, Is.EqualTo(1));
            Assert.That(snapshot[0].Name, Is.EqualTo("job-command"));
            Assert.That(command.ExecuteCount, Is.EqualTo(0));
            Assert.That(buffer.Count, Is.EqualTo(0));
        }

        // =============================================================================
        // ExecutionContextCarriesCommandBuffer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il contesto di esecuzione possa trasportare il command buffer
        /// verso un executor senza dipendenze globali.
        /// </para>
        ///
        /// <para><b>Context esplicito</b></para>
        /// <para>
        /// Il buffer non e' singleton e non e' globale: viene passato come dipendenza
        /// esplicita, coerente con l'architettura del progetto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>JobActionExecutionContext</b>: include CommandBuffer.</item>
        ///   <item><b>Assert</b>: stesso riferimento, nessuna creazione implicita.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ExecutionContextCarriesCommandBuffer()
        {
            // Arrange/Act: costruiamo contesto con buffer esplicito.
            var buffer = new JobCommandBuffer();
            var context = new JobActionExecutionContext(1, "job", 1, Vector2Int.zero, null, buffer);

            // Assert: l'executor futuro potra' accodare sul buffer ricevuto.
            Assert.That(context.CommandBuffer, Is.SameAs(buffer));
        }

        private sealed class FakeCommand : ICommand
        {
            private readonly string _name;

            public FakeCommand(string name)
            {
                _name = name;
            }

            public string Name => _name;
            public int ExecuteCount { get; private set; }

            public void Execute(World world, MessageBus bus)
            {
                // Il test non deve chiamare Execute; il contatore renderebbe evidente
                // una mutazione anticipata del comando.
                ExecuteCount++;
            }
        }
    }
}
