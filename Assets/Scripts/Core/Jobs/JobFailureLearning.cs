using System.Collections.Generic;
using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // JobFailureObservation
    // =============================================================================
    /// <summary>
    /// <para>
    /// Osservazione data-pura di un fallimento di job.
    /// </para>
    ///
    /// <para><b>Failure learning senza memoria episodica grezza</b></para>
    /// <para>
    /// Lo store non riceve il World e non ricostruisce la scena. Registra soltanto
    /// intenzione, motivo e contesto minimo necessario ad aggregare pattern.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId/JobId</b>: origine operativa del fallimento.</item>
    ///   <item><b>IntentKind</b>: tipo di intenzione che ha generato il job.</item>
    ///   <item><b>Reason</b>: motivo normalizzato del fallimento.</item>
    ///   <item><b>Tick</b>: tempo dell'osservazione.</item>
    ///   <item><b>DiagnosticKey</b>: chiave opzionale per target o fase.</item>
    /// </list>
    /// </summary>
    public readonly struct JobFailureObservation
    {
        public readonly int NpcId;
        public readonly string JobId;
        public readonly DecisionIntentKind IntentKind;
        public readonly JobFailureReason Reason;
        public readonly int Tick;
        public readonly string DiagnosticKey;
        public readonly bool HasTargetCell;
        public readonly Vector2Int TargetCell;

        public JobFailureObservation(int npcId, string jobId, DecisionIntentKind intentKind, JobFailureReason reason, int tick, string diagnosticKey)
            : this(npcId, jobId, intentKind, reason, tick, diagnosticKey, false, Vector2Int.zero)
        {
        }

        public JobFailureObservation(
            int npcId,
            string jobId,
            DecisionIntentKind intentKind,
            JobFailureReason reason,
            int tick,
            string diagnosticKey,
            bool hasTargetCell,
            Vector2Int targetCell)
        {
            NpcId = npcId;
            JobId = jobId ?? string.Empty;
            IntentKind = intentKind;
            Reason = reason == JobFailureReason.None ? JobFailureReason.Unknown : reason;
            Tick = tick;
            DiagnosticKey = diagnosticKey ?? string.Empty;
            HasTargetCell = hasTargetCell;
            TargetCell = targetCell;
        }
    }

    // =============================================================================
    // JobFailureLearningStore
    // =============================================================================
    /// <summary>
    /// <para>
    /// Store aggregato dei fallimenti job osservati.
    /// </para>
    ///
    /// <para><b>Apprendimento incrementale e modulare</b></para>
    /// <para>
    /// La v0.06 si limita a contare pattern per NPC/intenzione/motivo. Decision Layer
    /// e planner potranno usare questi dati piu' avanti per penalizzare scelte che
    /// falliscono spesso.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_counts</b>: mappa chiave aggregata -> conteggio.</item>
    ///   <item><b>Record</b>: registra un fallimento normalizzato.</item>
    ///   <item><b>GetCount</b>: legge il conteggio per pattern.</item>
    ///   <item><b>GetPenalty01</b>: converte count in penalita' normalizzata semplice.</item>
    /// </list>
    /// </summary>
    public sealed class JobFailureLearningStore
    {
        private readonly Dictionary<string, int> _counts = new();

        // =============================================================================
        // Record
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload EL-aware che registra il fallimento e ne esporta la penalita'
        /// aggiornata come trace diagnostica.
        /// </para>
        /// </summary>
        public void Record(
            JobFailureObservation observation,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            Record(observation);

            if (explainabilityConfig == null)
                return;

            Vector2Int targetCell = observation.HasTargetCell ? observation.TargetCell : Vector2Int.zero;
            MemoryBeliefDecisionExplainabilityEmitter.TryWriteFailureLearningTrace(
                explainabilityConfig,
                explainabilityRegistry,
                observation.NpcId,
                observation.Tick,
                observation.JobId,
                targetCell,
                observation.Reason,
                observation.Tick,
                GetPenalty01(observation.NpcId, observation.IntentKind, observation.Reason),
                observation.DiagnosticKey);
        }

        public void Record(JobFailureObservation observation)
        {
            // La chiave include NpcId per mantenere soggettivo l'apprendimento: due
            // NPC possono avere esperienze diverse con la stessa intenzione.
            var key = BuildKey(observation.NpcId, observation.IntentKind, observation.Reason);
            _counts.TryGetValue(key, out var count);
            _counts[key] = count + 1;
        }

        public int GetCount(int npcId, DecisionIntentKind intentKind, JobFailureReason reason)
        {
            // Lettura stabile per test, scoring futuro e debug UI.
            _counts.TryGetValue(BuildKey(npcId, intentKind, reason), out var count);
            return count;
        }

        public float GetPenalty01(int npcId, DecisionIntentKind intentKind, JobFailureReason reason)
        {
            // Penalita' MVP: tre fallimenti saturano a 1.0. La curva potra' diventare
            // configurabile quando verra' collegata allo scoring.
            var count = GetCount(npcId, intentKind, reason);
            if (count <= 0) return 0f;
            if (count >= 3) return 1f;
            return count / 3f;
        }

        private static string BuildKey(int npcId, DecisionIntentKind intentKind, JobFailureReason reason)
        {
            return npcId + "|" + intentKind + "|" + reason;
        }
    }
}
