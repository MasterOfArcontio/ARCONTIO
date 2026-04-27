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
                case MemoryBeliefDecisionTraceKind.JobRequest:
                    TryWriteRecord(config, new JobRequestJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        jobRequest = BuildJobRequest(trace.JobRequest),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.JobLifecycle:
                    TryWriteRecord(config, new JobLifecycleJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        jobLifecycle = BuildJobLifecycle(trace.JobLifecycle),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.JobPhase:
                    TryWriteRecord(config, new JobPhaseJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        jobPhase = BuildJobPhase(trace.JobPhase),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.Step:
                    TryWriteRecord(config, new StepJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        step = BuildStep(trace.Step),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.JobState:
                    TryWriteRecord(config, new JobStateJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        jobState = BuildJobState(trace.JobState),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.JobArbitration:
                    TryWriteRecord(config, new JobArbitrationJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        jobArbitration = BuildJobArbitration(trace.JobArbitration),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.Reservation:
                    TryWriteRecord(config, new ReservationJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        reservation = BuildReservation(trace.Reservation),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.Command:
                    TryWriteRecord(config, new CommandJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        command = BuildCommand(trace.Command),
                    });
                    break;
                case MemoryBeliefDecisionTraceKind.FailureLearning:
                    TryWriteRecord(config, new FailureLearningJsonLogRecord
                    {
                        schema = SchemaVersion,
                        kind = ToKindString(trace.Kind),
                        tick = trace.Tick,
                        npcId = trace.NpcId,
                        failureLearning = BuildFailureLearning(trace.FailureLearning),
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
                MemoryBeliefDecisionTraceKind.JobRequest => config.logJobRequest,
                MemoryBeliefDecisionTraceKind.JobLifecycle => config.logJobLifecycle,
                MemoryBeliefDecisionTraceKind.JobPhase => config.logJobPhase,
                MemoryBeliefDecisionTraceKind.Step => config.logStep,
                MemoryBeliefDecisionTraceKind.JobState => config.logJobState,
                MemoryBeliefDecisionTraceKind.JobArbitration => config.logJobArbitration,
                MemoryBeliefDecisionTraceKind.Reservation => config.logReservation,
                MemoryBeliefDecisionTraceKind.Command => config.logCommand,
                MemoryBeliefDecisionTraceKind.FailureLearning => config.logFailureLearning,
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

        private static JobRequestLogPayload BuildJobRequest(MemoryBeliefDecisionJobRequestRecord jobRequest)
        {
            if (jobRequest == null)
                return null;

            return new JobRequestLogPayload
            {
                requestId = jobRequest.RequestId ?? string.Empty,
                jobId = jobRequest.JobId ?? string.Empty,
                intent = jobRequest.Intent.ToString(),
                priorityClass = jobRequest.PriorityClass.ToString(),
                urgency01 = jobRequest.Urgency01,
                hasTargetCell = jobRequest.HasTargetCell,
                targetCell = BuildCell(jobRequest.TargetCell),
                targetCellText = FormatCell(jobRequest.TargetCell),
                targetObjectId = jobRequest.TargetObjectId,
                beliefKey = jobRequest.BeliefKey ?? string.Empty,
                debugLabel = jobRequest.DebugLabel ?? string.Empty,
                reason = jobRequest.Reason ?? string.Empty,
                legacyBridgeStillUsed = jobRequest.LegacyBridgeStillUsed,
            };
        }

        private static JobLifecycleLogPayload BuildJobLifecycle(MemoryBeliefDecisionJobLifecycleRecord lifecycle)
        {
            if (lifecycle == null)
                return null;

            return new JobLifecycleLogPayload
            {
                operation = lifecycle.Operation.ToString(),
                job = BuildJobRef(lifecycle.Job),
                reason = lifecycle.Reason ?? string.Empty,
            };
        }

        private static JobPhaseLogPayload BuildJobPhase(MemoryBeliefDecisionJobPhaseRecord phase)
        {
            if (phase == null)
                return null;

            return new JobPhaseLogPayload
            {
                operation = phase.Operation.ToString(),
                job = BuildJobRef(phase.Job),
                phase = BuildJobPhaseRef(phase.Phase),
                reason = phase.Reason ?? string.Empty,
            };
        }

        private static StepLogPayload BuildStep(MemoryBeliefDecisionStepRecord step)
        {
            if (step == null)
                return null;

            return new StepLogPayload
            {
                job = BuildJobRef(step.Job),
                phase = BuildJobPhaseRef(step.Phase),
                step = BuildStepRef(step.Step),
                result = BuildStepResult(step.Result),
                reason = step.Reason ?? string.Empty,
            };
        }

        private static JobStateLogPayload BuildJobState(MemoryBeliefDecisionJobStateRecord jobState)
        {
            if (jobState == null)
                return null;

            return new JobStateLogPayload
            {
                hasActiveJob = jobState.HasActiveJob,
                activeJobId = jobState.ActiveJobId ?? string.Empty,
                activePhaseIndex = jobState.ActivePhaseIndex,
                activeActionIndex = jobState.ActiveActionIndex,
                waitUntilTick = jobState.WaitUntilTick,
                suspendedJobId = jobState.SuspendedJobId ?? string.Empty,
                lastFailureReason = jobState.LastFailureReason.ToString(),
                reason = jobState.Reason ?? string.Empty,
            };
        }

        private static JobArbitrationLogPayload BuildJobArbitration(MemoryBeliefDecisionJobArbitrationRecord arbitration)
        {
            if (arbitration == null)
                return null;

            return new JobArbitrationLogPayload
            {
                currentJob = BuildJobRef(arbitration.CurrentJob),
                proposedJob = BuildJobRef(arbitration.ProposedJob),
                decision = arbitration.Decision.ToString(),
                acceptedJobId = arbitration.AcceptedJobId ?? string.Empty,
                reason = arbitration.Reason ?? string.Empty,
            };
        }

        private static ReservationLogPayload BuildReservation(MemoryBeliefDecisionReservationRecord reservation)
        {
            if (reservation == null)
                return null;

            return new ReservationLogPayload
            {
                operation = reservation.Operation.ToString(),
                reservationId = reservation.ReservationId ?? string.Empty,
                jobId = reservation.JobId ?? string.Empty,
                ownerNpcId = reservation.OwnerNpcId,
                targetKind = reservation.TargetKind.ToString(),
                targetCell = BuildCell(reservation.TargetCell),
                targetCellText = FormatCell(reservation.TargetCell),
                targetObjectId = reservation.TargetObjectId,
                createdTick = reservation.CreatedTick,
                expiresTick = reservation.ExpiresTick,
                reason = reservation.Reason ?? string.Empty,
            };
        }

        private static CommandLogPayload BuildCommand(MemoryBeliefDecisionCommandRecord command)
        {
            if (command == null)
                return null;

            return new CommandLogPayload
            {
                operation = command.Operation.ToString(),
                jobId = command.JobId ?? string.Empty,
                commandName = command.CommandName ?? string.Empty,
                queueCount = command.QueueCount,
                reason = command.Reason ?? string.Empty,
            };
        }

        private static FailureLearningLogPayload BuildFailureLearning(MemoryBeliefDecisionFailureLearningRecord failure)
        {
            if (failure == null)
                return null;

            return new FailureLearningLogPayload
            {
                jobId = failure.JobId ?? string.Empty,
                targetCell = BuildCell(failure.TargetCell),
                targetCellText = FormatCell(failure.TargetCell),
                failureReason = failure.FailureReason.ToString(),
                failureTick = failure.FailureTick,
                penalty01 = failure.Penalty01,
                reason = failure.Reason ?? string.Empty,
            };
        }

        private static JobRefLogPayload BuildJobRef(MemoryBeliefDecisionJobRef job)
        {
            if (job == null)
                return null;

            return new JobRefLogPayload
            {
                jobId = job.JobId ?? string.Empty,
                requestId = job.RequestId ?? string.Empty,
                intent = job.Intent.ToString(),
                priorityClass = job.PriorityClass.ToString(),
                urgency01 = job.Urgency01,
                status = job.Status.ToString(),
                failureReason = job.FailureReason.ToString(),
                createdTick = job.CreatedTick,
                updatedTick = job.UpdatedTick,
                activePhaseIndex = job.ActivePhaseIndex,
                hasTargetCell = job.HasTargetCell,
                targetCell = BuildCell(job.TargetCell),
                targetCellText = FormatCell(job.TargetCell),
                targetObjectId = job.TargetObjectId,
                debugLabel = job.DebugLabel ?? string.Empty,
            };
        }

        private static JobPhaseRefLogPayload BuildJobPhaseRef(MemoryBeliefDecisionJobPhaseRef phase)
        {
            if (phase == null)
                return null;

            return new JobPhaseRefLogPayload
            {
                phaseId = phase.PhaseId ?? string.Empty,
                kind = phase.Kind.ToString(),
                displayName = phase.DisplayName ?? string.Empty,
                phaseIndex = phase.PhaseIndex,
                expectedStepCount = phase.ExpectedStepCount,
                isInterruptible = phase.IsInterruptible,
            };
        }

        private static StepRefLogPayload BuildStepRef(MemoryBeliefDecisionStepRef step)
        {
            if (step == null)
                return null;

            return new StepRefLogPayload
            {
                actionId = step.ActionId ?? string.Empty,
                kind = step.Kind.ToString(),
                label = step.Label ?? string.Empty,
                actionIndex = step.ActionIndex,
                hasTargetCell = step.HasTargetCell,
                targetCell = BuildCell(step.TargetCell),
                targetCellText = FormatCell(step.TargetCell),
                targetObjectId = step.TargetObjectId,
                durationTicks = step.DurationTicks,
                payloadKey = step.PayloadKey ?? string.Empty,
            };
        }

        private static StepResultLogPayload BuildStepResult(MemoryBeliefDecisionStepResultRef result)
        {
            if (result == null)
                return null;

            return new StepResultLogPayload
            {
                status = result.Status.ToString(),
                failureReason = result.FailureReason.ToString(),
                suggestedWaitTicks = result.SuggestedWaitTicks,
                diagnosticMessage = result.DiagnosticMessage ?? string.Empty,
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
                MemoryBeliefDecisionTraceKind.JobRequest => "job_request",
                MemoryBeliefDecisionTraceKind.JobLifecycle => "job_lifecycle",
                MemoryBeliefDecisionTraceKind.JobPhase => "job_phase",
                MemoryBeliefDecisionTraceKind.Step => "step",
                MemoryBeliefDecisionTraceKind.JobState => "job_state",
                MemoryBeliefDecisionTraceKind.JobArbitration => "job_arbitration",
                MemoryBeliefDecisionTraceKind.Reservation => "reservation",
                MemoryBeliefDecisionTraceKind.Command => "command",
                MemoryBeliefDecisionTraceKind.FailureLearning => "failure_learning",
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
        private sealed class JobRequestJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public JobRequestLogPayload jobRequest;
        }

        [Serializable]
        private sealed class JobLifecycleJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public JobLifecycleLogPayload jobLifecycle;
        }

        [Serializable]
        private sealed class JobPhaseJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public JobPhaseLogPayload jobPhase;
        }

        [Serializable]
        private sealed class StepJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public StepLogPayload step;
        }

        [Serializable]
        private sealed class JobStateJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public JobStateLogPayload jobState;
        }

        [Serializable]
        private sealed class JobArbitrationJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public JobArbitrationLogPayload jobArbitration;
        }

        [Serializable]
        private sealed class ReservationJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public ReservationLogPayload reservation;
        }

        [Serializable]
        private sealed class CommandJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public CommandLogPayload command;
        }

        [Serializable]
        private sealed class FailureLearningJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public long tick;
            public int npcId;
            public FailureLearningLogPayload failureLearning;
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
        private sealed class JobRequestLogPayload
        {
            public string requestId = string.Empty;
            public string jobId = string.Empty;
            public string intent = string.Empty;
            public string priorityClass = string.Empty;
            public float urgency01;
            public bool hasTargetCell;
            public CellLogPayload targetCell;
            public string targetCellText = string.Empty;
            public int targetObjectId;
            public string beliefKey = string.Empty;
            public string debugLabel = string.Empty;
            public string reason = string.Empty;
            public bool legacyBridgeStillUsed;
        }

        [Serializable]
        private sealed class JobLifecycleLogPayload
        {
            public string operation = string.Empty;
            public JobRefLogPayload job;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class JobPhaseLogPayload
        {
            public string operation = string.Empty;
            public JobRefLogPayload job;
            public JobPhaseRefLogPayload phase;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class StepLogPayload
        {
            public JobRefLogPayload job;
            public JobPhaseRefLogPayload phase;
            public StepRefLogPayload step;
            public StepResultLogPayload result;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class JobStateLogPayload
        {
            public bool hasActiveJob;
            public string activeJobId = string.Empty;
            public int activePhaseIndex;
            public int activeActionIndex;
            public int waitUntilTick;
            public string suspendedJobId = string.Empty;
            public string lastFailureReason = string.Empty;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class JobArbitrationLogPayload
        {
            public JobRefLogPayload currentJob;
            public JobRefLogPayload proposedJob;
            public string decision = string.Empty;
            public string acceptedJobId = string.Empty;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class ReservationLogPayload
        {
            public string operation = string.Empty;
            public string reservationId = string.Empty;
            public string jobId = string.Empty;
            public int ownerNpcId;
            public string targetKind = string.Empty;
            public CellLogPayload targetCell;
            public string targetCellText = string.Empty;
            public int targetObjectId;
            public int createdTick;
            public int expiresTick;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class CommandLogPayload
        {
            public string operation = string.Empty;
            public string jobId = string.Empty;
            public string commandName = string.Empty;
            public int queueCount;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class FailureLearningLogPayload
        {
            public string jobId = string.Empty;
            public CellLogPayload targetCell;
            public string targetCellText = string.Empty;
            public string failureReason = string.Empty;
            public int failureTick;
            public float penalty01;
            public string reason = string.Empty;
        }

        [Serializable]
        private sealed class JobRefLogPayload
        {
            public string jobId = string.Empty;
            public string requestId = string.Empty;
            public string intent = string.Empty;
            public string priorityClass = string.Empty;
            public float urgency01;
            public string status = string.Empty;
            public string failureReason = string.Empty;
            public int createdTick;
            public int updatedTick;
            public int activePhaseIndex;
            public bool hasTargetCell;
            public CellLogPayload targetCell;
            public string targetCellText = string.Empty;
            public int targetObjectId;
            public string debugLabel = string.Empty;
        }

        [Serializable]
        private sealed class JobPhaseRefLogPayload
        {
            public string phaseId = string.Empty;
            public string kind = string.Empty;
            public string displayName = string.Empty;
            public int phaseIndex;
            public int expectedStepCount;
            public bool isInterruptible;
        }

        [Serializable]
        private sealed class StepRefLogPayload
        {
            public string actionId = string.Empty;
            public string kind = string.Empty;
            public string label = string.Empty;
            public int actionIndex;
            public bool hasTargetCell;
            public CellLogPayload targetCell;
            public string targetCellText = string.Empty;
            public int targetObjectId;
            public int durationTicks;
            public string payloadKey = string.Empty;
        }

        [Serializable]
        private sealed class StepResultLogPayload
        {
            public string status = string.Empty;
            public string failureReason = string.Empty;
            public int suggestedWaitTicks;
            public string diagnosticMessage = string.Empty;
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
