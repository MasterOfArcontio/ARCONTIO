using System.Collections.Generic;
using Arcontio.Core.Config;

namespace Arcontio.Core
{
    // =============================================================================
    // JobCommandBuffer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Buffer ordinato di comandi prodotti dal Job Execution layer ma non ancora
    /// applicati al mondo.
    /// </para>
    ///
    /// <para><b>Separazione produzione comando / mutazione World</b></para>
    /// <para>
    /// Gli step possono dichiarare effetti tramite <c>ICommand</c>, ma l'esecuzione
    /// effettiva resta in un punto orchestrato del tick. Questo evita mutazioni
    /// immediate e rende ispezionabile cosa il job vuole fare.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_commands</b>: lista ordinata dei comandi accodati.</item>
    ///   <item><b>Enqueue</b>: aggiunge un comando non nullo.</item>
    ///   <item><b>Snapshot</b>: restituisce copia difensiva per test e orchestration.</item>
    ///   <item><b>Clear</b>: svuota il buffer dopo flush esterno.</item>
    /// </list>
    /// </summary>
    public sealed class JobCommandBuffer
    {
        private readonly List<ICommand> _commands = new();

        public int Count => _commands.Count;

        // =============================================================================
        // Enqueue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload EL-aware che rende osservabile l'accodamento dei command senza
        /// imporre dipendenze diagnostiche al path base.
        /// </para>
        /// </summary>
        public bool Enqueue(
            ICommand command,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId,
            int tick,
            string jobId,
            string reason)
        {
            bool accepted = Enqueue(command);

            if (explainabilityConfig != null && accepted)
            {
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteCommandTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionCommandOperation.Enqueued,
                    jobId,
                    command != null ? command.Name : string.Empty,
                    _commands.Count,
                    reason ?? "CommandEnqueued");
            }

            return accepted;
        }

        public bool Enqueue(ICommand command)
        {
            // Accodare null sarebbe un bug silenzioso difficile da diagnosticare:
            // restituiamo false e lasciamo al chiamante decidere il fallimento.
            if (command == null)
                return false;

            _commands.Add(command);
            return true;
        }

        // =============================================================================
        // Snapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload EL-aware che esporta una trace diagnostica dello snapshot del
        /// buffer comandi.
        /// </para>
        /// </summary>
        public ICommand[] Snapshot(
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId,
            int tick,
            string jobId,
            string reason)
        {
            var snapshot = Snapshot();

            if (explainabilityConfig != null)
            {
                string commandName = snapshot.Length > 0 ? snapshot[snapshot.Length - 1].Name : string.Empty;
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteCommandTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionCommandOperation.Snapshot,
                    jobId,
                    commandName,
                    snapshot.Length,
                    reason ?? "CommandBufferSnapshot");
            }

            return snapshot;
        }

        public ICommand[] Snapshot()
        {
            // La copia difensiva impedisce a test o sistemi esterni di mutare la
            // lista mentre il job system sta ancora accumulando comandi.
            return _commands.ToArray();
        }

        public void Clear()
        {
            // Il flush reale verra' orchestrato fuori dal buffer; qui ci limitiamo a
            // preparare il contenitore per il tick successivo.
            _commands.Clear();
        }
    }
}
