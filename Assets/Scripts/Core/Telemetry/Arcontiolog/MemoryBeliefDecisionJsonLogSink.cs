using System;
using System.IO;
using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MemoryBeliefDecisionJsonLogSink
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sink diagnostico append-only per esportare trace EL-MBD in formato JSONL.
    /// Ogni chiamata produce al massimo una riga JSON autonoma e leggibile.
    /// </para>
    ///
    /// <para><b>Separazione simulazione / diagnostica</b></para>
    /// <para>
    /// Il sink non decide, non interroga il mondo, non modifica store e non rilegge
    /// BeliefStore o MemoryStore. Riceve snapshot gia' costruiti dagli emitter e li
    /// serializza solo se la configurazione lo permette.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryWriteTrace</b>: entry point comune per record memory, belief, query, decision e bridge.</item>
    ///   <item><b>Build*</b>: converte snapshot runtime in payload JSONL con enum testuali.</item>
    ///   <item><b>ResolveLogPath</b>: risolve il file JSONL della sessione corrente.</item>
    ///   <item><b>TryWriteRecord</b>: append difensivo che non interrompe la simulazione.</item>
    /// </list>
    /// </summary>
    public static class MemoryBeliefDecisionJsonLogSink
    {
        private const string SchemaVersion = "arcontio_el_mbd.v1";
        private const string DefaultDirectoryName = "Arcontio_EL_MBD";

        private static string _resolvedPath = string.Empty;
        private static string _resolvedPattern = string.Empty;
        private static bool _hasReportedWriteError;

        // =============================================================================
        // TryWriteTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive una trace EL-MBD nel file JSONL configurato.
        /// </para>
        ///
        /// <para><b>No-op configurabile</b></para>
        /// <para>
        /// Configurazioni null, disabilitate o senza file attivo sono trattate come
        /// no-op. Gli emitter futuri potranno quindi chiamare il sink senza duplicare
        /// guardie in ogni punto della pipeline.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Guard</b>: verifica config e trace.</item>
        ///   <item><b>Switch</b>: costruisce la payload coerente con il kind.</item>
        ///   <item><b>Append</b>: delega a <c>TryWriteRecord</c>.</item>
        /// </list>
        /// </summary>
        public static void TryWriteTrace(MemoryBeliefDecisionExplainabilityParams config, MemoryBeliefDecisionTrace trace)
        {
            if (config == null || !config.enabled || !config.writeJsonLog || trace == null)
                return;

            if (!IsKindEnabled(config, trace.Kind))
                return;

            switch (trace.Kind)
            {
                case MemoryBeliefDecisionTraceKind.Memory:
                    TryWriteRecord(config, new MemoryJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        memory = BuildMemory(trace.Memory),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.Belief:
                    TryWriteRecord(config, new BeliefJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        belief = BuildBeliefRecord(trace.Belief),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.Query:
                    TryWriteRecord(config, new QueryJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        query = BuildQuery(trace.Query),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.Decision:
                    TryWriteRecord(config, new DecisionJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        decision = BuildDecision(trace.Decision, config),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.Bridge:
                    TryWriteRecord(config, new BridgeJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        bridge = BuildBridge(trace.Bridge),
                    });
                    break;
                default:
                    return;
            }
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

        private static void TryWriteRecord(MemoryBeliefDecisionExplainabilityParams config, object record)
        {
            string path = ResolveLogPath(config);
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                string json = JsonUtility.ToJson(record, prettyPrint: false);
                File.AppendAllText(path, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                if (_hasReportedWriteError)
                    return;

                _hasReportedWriteError = true;
                Debug.LogWarning($"[EL MBD] JSONL log write failed: {ex.Message}");
            }
        }

        private static string ResolveLogPath(MemoryBeliefDecisionExplainabilityParams config)
        {
            string pattern = string.IsNullOrWhiteSpace(config.jsonLogFileNamePattern)
                ? "arcontio_el_mbd_{yyyyMMdd_HHmmss}.jsonl"
                : config.jsonLogFileNamePattern;

            if (!string.IsNullOrWhiteSpace(_resolvedPath) && string.Equals(_resolvedPattern, pattern, StringComparison.Ordinal))
                return _resolvedPath;

            string safeFileName = pattern.Replace("{yyyyMMdd_HHmmss}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            if (!safeFileName.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                safeFileName += ".jsonl";

            string directory = Path.Combine(Application.persistentDataPath, DefaultDirectoryName);
            Directory.CreateDirectory(directory);

            _resolvedPattern = pattern;
            _resolvedPath = Path.Combine(directory, safeFileName);
            _hasReportedWriteError = false;

            return _resolvedPath;
        }

        private static MemoryLogPayload BuildMemory(MemoryBeliefDecisionMemoryTraceRecord memory)
        {
            if (memory == null)
                return null;

            return new MemoryLogPayload
            {
                eventType = memory.EventType ?? string.Empty,
                traceType = memory.TraceType.ToString(),
                subjectId = memory.SubjectId,
                secondarySubjectId = memory.SecondarySubjectId,
                subjectDefId = memory.SubjectDefId ?? string.Empty,
                cell = BuildCell(memory.Cell),
                cellText = FormatCell(memory.Cell),
                intensity01 = memory.Intensity01,
                reliability01 = memory.Reliability01,
                isHeard = memory.IsHeard,
                heardKind = memory.HeardKind ?? string.Empty,
                sourceSpeakerId = memory.SourceSpeakerId,
                storeResult = memory.StoreResult.ToString(),
            };
        }

        private static BeliefLogPayload BuildBeliefRef(MemoryBeliefDecisionBeliefRef belief)
        {
            return new BeliefLogPayload
            {
                category = belief.Category.ToString(),
                status = belief.Status.ToString(),
                source = belief.Source.ToString(),
                beliefId = belief.BeliefId,
                estimatedCell = BuildCell(belief.EstimatedPosition),
                estimatedCellText = FormatCell(belief.EstimatedPosition),
                confidence = belief.Confidence,
                freshness = belief.Freshness,
                sourceCount = belief.SourceCount,
            };
        }

        private static BeliefRecordLogPayload BuildBeliefRecord(MemoryBeliefDecisionBeliefRecord belief)
        {
            if (belief == null)
                return null;

            return new BeliefRecordLogPayload
            {
                operation = belief.Operation.ToString(),
                hasSourceTrace = belief.HasSourceTrace,
                sourceTraceType = belief.HasSourceTrace ? belief.SourceTraceType.ToString() : string.Empty,
                belief = BuildBeliefRef(belief.Belief),
                reason = belief.Reason ?? string.Empty,
            };
        }

        private static QueryLogPayload BuildQuery(MemoryBeliefDecisionQueryRecord query)
        {
            if (query == null)
                return null;

            return new QueryLogPayload
            {
                goalType = query.GoalType.ToString(),
                urgency01 = query.Urgency01,
                npcCell = BuildCell(query.NpcPosition),
                npcCellText = FormatCell(query.NpcPosition),
                minConfidence = query.MinConfidence,
                candidateCount = query.CandidateCount,
                usableCandidateCount = query.UsableCandidateCount,
                isEmpty = query.IsEmpty,
                emptyReason = query.EmptyReason ?? string.Empty,
                winner = BuildBeliefRef(query.Winner),
                finalScore = query.FinalScore,
                contributions = BuildContributions(query.Contributions),
            };
        }

        private static DecisionLogPayload BuildDecision(
            MemoryBeliefDecisionDecisionRecord decision,
            MemoryBeliefDecisionExplainabilityParams config)
        {
            if (decision == null)
                return null;

            return new DecisionLogPayload
            {
                auditValid = decision.AuditValid,
                candidateCount = decision.CandidateCount,
                selectedIntent = decision.SelectedIntent.ToString(),
                selectedScore = decision.SelectedScore,
                selectedIndex = decision.SelectedIndex,
                selectionTopN = decision.SelectionTopN,
                selectionNoise01 = decision.SelectionNoise01,
                impulsivity01 = decision.Impulsivity01,
                effectiveNoise01 = decision.EffectiveNoise01,
                candidates = config.includeCandidates ? BuildCandidates(decision.Candidates, config) : Array.Empty<CandidateLogPayload>(),
            };
        }

        private static BridgeLogPayload BuildBridge(MemoryBeliefDecisionBridgeRecord bridge)
        {
            if (bridge == null)
                return null;

            return new BridgeLogPayload
            {
                selectedIntent = bridge.SelectedIntent.ToString(),
                commandName = bridge.CommandName ?? string.Empty,
                handled = bridge.Handled,
                didMove = bridge.DidMove,
                didSteal = bridge.DidSteal,
                targetCell = BuildCell(bridge.TargetCell),
                targetCellText = FormatCell(bridge.TargetCell),
                targetSource = bridge.TargetSource.ToString(),
                legacyFallbackUsed = bridge.LegacyFallbackUsed,
                reason = bridge.Reason ?? string.Empty,
            };
        }

        private static CandidateLogPayload[] BuildCandidates(
            MemoryBeliefDecisionCandidateRecord[] candidates,
            MemoryBeliefDecisionExplainabilityParams config)
        {
            if (candidates == null || candidates.Length == 0)
                return Array.Empty<CandidateLogPayload>();

            var output = new CandidateLogPayload[candidates.Length];
            for (int i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                output[i] = new CandidateLogPayload
                {
                    intent = candidate.Intent.ToString(),
                    available = candidate.Available,
                    need = candidate.Need.ToString(),
                    needUrgency01 = candidate.NeedUrgency01,
                    isCritical = candidate.IsCritical,
                    requiresBeliefTarget = candidate.RequiresBeliefTarget,
                    beliefResultEmpty = candidate.BeliefResultEmpty,
                    belief = BuildBeliefRef(candidate.Belief),
                    score = candidate.Score,
                    filteredReason = candidate.FilteredReason ?? string.Empty,
                    scoreContributions = config.includeScoreBreakdown
                        ? BuildContributions(candidate.ScoreContributions)
                        : Array.Empty<ContributionLogPayload>(),
                };
            }

            return output;
        }

        private static ContributionLogPayload[] BuildContributions(MemoryBeliefDecisionScoreContributionRef[] contributions)
        {
            if (contributions == null || contributions.Length == 0)
                return Array.Empty<ContributionLogPayload>();

            var output = new ContributionLogPayload[contributions.Length];
            for (int i = 0; i < contributions.Length; i++)
            {
                output[i] = new ContributionLogPayload
                {
                    label = contributions[i].Label ?? string.Empty,
                    value = contributions[i].Value,
                };
            }

            return output;
        }

        private static CellLogPayload BuildCell(Vector2Int cell)
        {
            return new CellLogPayload
            {
                x = cell.x,
                y = cell.y,
            };
        }

        private static string FormatCell(Vector2Int cell)
        {
            return $"({cell.x}, {cell.y})";
        }

        private static string ToKindString(MemoryBeliefDecisionTraceKind kind)
        {
            return kind switch
            {
                MemoryBeliefDecisionTraceKind.Memory => "memory",
                MemoryBeliefDecisionTraceKind.Belief => "belief",
                MemoryBeliefDecisionTraceKind.Query => "query",
                MemoryBeliefDecisionTraceKind.Decision => "decision",
                MemoryBeliefDecisionTraceKind.Bridge => "bridge",
                _ => "unknown"
            };
        }

        // =============================================================================
        // MemoryJsonLogRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Record JSONL compatto per payload <c>memory</c>.
        /// </para>
        ///
        /// <para><b>Payload singola</b></para>
        /// <para>
        /// La classe contiene solo il campo coerente con il kind, evitando che
        /// <c>JsonUtility</c> serializzi payload vuoti per belief, query, decision e
        /// bridge.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>schema/kind/tick/npcId</b>: envelope comune.</item>
        ///   <item><b>memory</b>: payload della trace di memoria.</item>
        /// </list>
        /// </summary>
        [Serializable]
        private sealed class MemoryJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public MemoryLogPayload memory;
        }

        // =============================================================================
        // BeliefJsonLogRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Record JSONL compatto per payload <c>belief</c>.
        /// </para>
        ///
        /// <para><b>Payload singola</b></para>
        /// <para>
        /// Tiene separata la mutazione belief dagli altri payload EL-MBD, riducendo
        /// rumore nel file e rendendo ogni riga piu' leggibile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>schema/kind/tick/npcId</b>: envelope comune.</item>
        ///   <item><b>belief</b>: payload della mutazione belief.</item>
        /// </list>
        /// </summary>
        [Serializable]
        private sealed class BeliefJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public BeliefRecordLogPayload belief;
        }

        // =============================================================================
        // QueryJsonLogRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Record JSONL compatto per payload <c>query</c>.
        /// </para>
        ///
        /// <para><b>Query isolata</b></para>
        /// <para>
        /// La riga contiene solo contesto, winner e breakdown della query, senza
        /// oggetti vuoti riferiti a memory o decision.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>schema/kind/tick/npcId</b>: envelope comune.</item>
        ///   <item><b>query</b>: payload del BeliefQueryService.</item>
        /// </list>
        /// </summary>
        [Serializable]
        private sealed class QueryJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public QueryLogPayload query;
        }

        // =============================================================================
        // DecisionJsonLogRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Record JSONL compatto per payload <c>decision</c>.
        /// </para>
        ///
        /// <para><b>Decisione isolata</b></para>
        /// <para>
        /// Conserva solo selezione, candidati e score breakdown della decisione,
        /// rendendo piu' semplice il confronto tra run deterministici.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>schema/kind/tick/npcId</b>: envelope comune.</item>
        ///   <item><b>decision</b>: payload della selezione decisionale.</item>
        /// </list>
        /// </summary>
        [Serializable]
        private sealed class DecisionJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public DecisionLogPayload decision;
        }

        // =============================================================================
        // BridgeJsonLogRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Record JSONL compatto per payload <c>bridge</c>.
        /// </para>
        ///
        /// <para><b>Bridge isolato</b></para>
        /// <para>
        /// La riga descrive solo la traduzione Decision -> Command legacy, senza
        /// payload vuoti di memory, belief, query o decision.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>schema/kind/tick/npcId</b>: envelope comune.</item>
        ///   <item><b>bridge</b>: payload del ponte provvisorio.</item>
        /// </list>
        /// </summary>
        [Serializable]
        private sealed class BridgeJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public BridgeLogPayload bridge;
        }

        [Serializable]
        private sealed class MemoryLogPayload
        {
            public string eventType = string.Empty;
            public string traceType = string.Empty;
            public int subjectId;
            public int secondarySubjectId;
            public string subjectDefId = string.Empty;
            public CellLogPayload cell;
            public string cellText = string.Empty;
            public float intensity01;
            public float reliability01;
            public bool isHeard;
            public string heardKind = string.Empty;
            public int sourceSpeakerId;
            public string storeResult = string.Empty;
        }

        [Serializable]
        private sealed class BeliefRecordLogPayload
        {
            public string operation = string.Empty;
            public bool hasSourceTrace;
            public string sourceTraceType = string.Empty;
            public BeliefLogPayload belief;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class QueryLogPayload
        {
            public string goalType = string.Empty;
            public float urgency01;
            public CellLogPayload npcCell;
            public string npcCellText = string.Empty;
            public float minConfidence;
            public int candidateCount;
            public int usableCandidateCount;
            public bool isEmpty;
            public string emptyReason = string.Empty;
            public BeliefLogPayload winner;
            public float finalScore;
            public ContributionLogPayload[] contributions = Array.Empty<ContributionLogPayload>();
        }

        [Serializable]
        private sealed class DecisionLogPayload
        {
            public bool auditValid;
            public int candidateCount;
            public string selectedIntent = string.Empty;
            public float selectedScore;
            public int selectedIndex;
            public int selectionTopN;
            public float selectionNoise01;
            public float impulsivity01;
            public float effectiveNoise01;
            public CandidateLogPayload[] candidates = Array.Empty<CandidateLogPayload>();
        }

        [Serializable]
        private sealed class BridgeLogPayload
        {
            public string selectedIntent = string.Empty;
            public string commandName = string.Empty;
            public bool handled;
            public bool didMove;
            public bool didSteal;
            public CellLogPayload targetCell;
            public string targetCellText = string.Empty;
            public string targetSource = string.Empty;
            public bool legacyFallbackUsed;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class CandidateLogPayload
        {
            public string intent = string.Empty;
            public bool available;
            public string need = string.Empty;
            public float needUrgency01;
            public bool isCritical;
            public bool requiresBeliefTarget;
            public bool beliefResultEmpty;
            public BeliefLogPayload belief;
            public float score;
            public string filteredReason = string.Empty;
            public ContributionLogPayload[] scoreContributions = Array.Empty<ContributionLogPayload>();
        }

        [Serializable]
        private sealed class BeliefLogPayload
        {
            public string category = string.Empty;
            public string status = string.Empty;
            public string source = string.Empty;
            public int beliefId;
            public CellLogPayload estimatedCell;
            public string estimatedCellText = string.Empty;
            public float confidence;
            public float freshness;
            public int sourceCount;
        }

        [Serializable]
        private sealed class ContributionLogPayload
        {
            public string label = string.Empty;
            public float value;
        }

        [Serializable]
        private sealed class CellLogPayload
        {
            public int x;
            public int y;
        }
    }
}
