using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MemoryBeliefDecisionExplainabilityEmitter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Emitter statico minimale per trasformare eventi Memory/Belief gia' avvenuti
    /// in snapshot EL-MBD append-only.
    /// </para>
    ///
    /// <para><b>Emitter one-way senza accesso globale</b></para>
    /// <para>
    /// L'emitter non cerca dati nel <c>World</c>, non interroga <c>MemoryStore</c> e
    /// non modifica <c>BeliefStore</c>. Riceve trace, esito dello store o belief gia'
    /// aggiornato dai sistemi proprietari e li inoltra al sink JSONL.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryWriteMemoryTrace</b>: esporta la trace accettata, rinforzata o droppata.</item>
    ///   <item><b>TryWriteBeliefTrace</b>: esporta il belief risultante da aggregazione o feedback.</item>
    ///   <item><b>ToBeliefRef</b>: copia campi primitivi evitando riferimenti live allo store.</item>
    /// </list>
    /// </summary>
    public static class MemoryBeliefDecisionExplainabilityEmitter
    {
        // =============================================================================
        // TryWriteTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inoltra una trace EL-MBQD verso le due destinazioni diagnostiche previste:
        /// registry runtime UI-friendly e JSONL persistente.
        /// </para>
        ///
        /// <para><b>Doppia uscita one-way</b></para>
        /// <para>
        /// La trace viene costruita una sola volta dal producer legittimo e poi copiata
        /// verso destinazioni passive. Il registry serve alla UI live, il JSONL serve
        /// all'analisi offline; nessuna delle due destinazioni ricalcola o arricchisce
        /// i dati interrogando il mondo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Config guard</b>: config null disattiva entrambe le uscite.</item>
        ///   <item><b>Registry</b>: conserva la trace bounded per NPC, se disponibile.</item>
        ///   <item><b>JSONL</b>: mantiene il comportamento append-only esistente.</item>
        /// </list>
        /// </summary>
        public static void TryWriteTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            MemoryBeliefDecisionTrace trace)
        {
            if (config == null || trace == null)
                return;

            // Il registry applica gli stessi gate di abilitazione/kind della config,
            // ma non apre file e non produce side-effect simulativi.
            registry?.AddTrace(config, trace);

            // Il sink conserva la semantica precedente: se writeJsonLog e' false o il
            // kind e' disabilitato, la chiamata termina come no-op.
            MemoryBeliefDecisionJsonLogSink.TryWriteTrace(config, trace);
        }

        // =============================================================================
        // TryWriteMemoryTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta un record EL per una <c>MemoryTrace</c> appena processata dal
        /// <c>MemoryStore</c>.
        /// </para>
        ///
        /// <para><b>Memoria come ingresso soggettivo</b></para>
        /// <para>
        /// Il record conserva tipo, soggetto, cella e qualita' della traccia, piu'
        /// l'esito di <c>AddOrMerge</c>. In questo modo un log runtime puo' distinguere
        /// memoria inserita, rinforzata, rimpiazzata o scartata.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Config</b>: no-op quando EL o JSONL sono disabilitati.</item>
        ///   <item><b>Trace</b>: copia solo dati gia' presenti nella memoria.</item>
        ///   <item><b>StoreResult</b>: collega la trace all'esito del MemoryStore.</item>
        /// </list>
        /// </summary>
        // =============================================================================
        // TryWriteMemoryTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload storico che esporta una memory trace solo verso le destinazioni
        /// disponibili prima del registry runtime.
        /// </para>
        /// </summary>
        public static void TryWriteMemoryTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            int npcId,
            long tick,
            in MemoryTrace trace,
            AddOrMergeResult storeResult,
            string eventType)
        {
            TryWriteMemoryTrace(config, null, npcId, tick, trace, storeResult, eventType);
        }

        public static void TryWriteMemoryTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            in MemoryTrace trace,
            AddOrMergeResult storeResult,
            string eventType)
        {
            if (config == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Memory,
                Tick = tick,
                NpcId = npcId,
                Memory = new MemoryBeliefDecisionMemoryTraceRecord
                {
                    EventType = eventType ?? string.Empty,
                    TraceType = trace.Type,
                    SubjectId = trace.SubjectId,
                    SecondarySubjectId = trace.SecondarySubjectId,
                    SubjectDefId = trace.SubjectDefId ?? string.Empty,
                    Cell = new Vector2Int(trace.CellX, trace.CellY),
                    Intensity01 = trace.Intensity01,
                    Reliability01 = trace.Reliability01,
                    IsHeard = trace.IsHeard,
                    HeardKind = trace.HeardKind.ToString(),
                    SourceSpeakerId = trace.SourceSpeakerId,
                    StoreResult = storeResult,
                },
            });
        }

        // =============================================================================
        // TryWriteBeliefTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta un record EL per una mutazione del BeliefStore appena completata.
        /// </para>
        ///
        /// <para><b>Belief come risultato, non come sorgente logica</b></para>
        /// <para>
        /// Il metodo riceve il belief gia' trovato dal chiamante e lo copia in un
        /// riferimento serializzabile. Non decide se la mutazione sia corretta e non
        /// rilegge la lista delle credenze.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Operation</b>: creazione, merge, rinforzo o indebolimento.</item>
        ///   <item><b>SourceTrace</b>: conserva il tipo della memory che ha alimentato il belief.</item>
        ///   <item><b>Reason</b>: stringa breve per correlare regola o feedback operativo.</item>
        /// </list>
        /// </summary>
        // =============================================================================
        // TryWriteBeliefTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload storico che esporta una mutazione belief senza richiedere al
        /// chiamante di possedere un registry runtime.
        /// </para>
        /// </summary>
        public static void TryWriteBeliefTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            int npcId,
            long tick,
            MemoryBeliefDecisionBeliefOperation operation,
            in MemoryTrace sourceTrace,
            BeliefEntry belief,
            string reason)
        {
            TryWriteBeliefTrace(config, null, npcId, tick, operation, sourceTrace, belief, reason);
        }

        public static void TryWriteBeliefTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            MemoryBeliefDecisionBeliefOperation operation,
            in MemoryTrace sourceTrace,
            BeliefEntry belief,
            string reason)
        {
            if (config == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Belief,
                Tick = tick,
                NpcId = npcId,
                Belief = new MemoryBeliefDecisionBeliefRecord
                {
                    Operation = operation,
                    HasSourceTrace = true,
                    SourceTraceType = sourceTrace.Type,
                    Belief = ToBeliefRef(belief),
                    Reason = reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // TryWriteJobRequestTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta una trace EL del passaggio Decision -> JobRequest.
        /// </para>
        ///
        /// <para><b>Separazione tra nuovo path job e bridge legacy</b></para>
        /// <para>
        /// Il chiamante puo' emettere questa trace anche quando il bridge legacy resta
        /// attivo. In questo modo l'EL v0.07 mostra chiaramente che il Job System sta
        /// ricevendo input senza confondere tale passaggio con la produzione diretta di
        /// command.
        /// </para>
        /// </summary>
        public static void TryWriteJobRequestTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            JobRequest request,
            string jobId,
            string reason,
            bool legacyBridgeStillUsed)
        {
            if (config == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobRequest,
                Tick = tick,
                NpcId = npcId,
                JobRequest = new MemoryBeliefDecisionJobRequestRecord
                {
                    RequestId = request.RequestId,
                    JobId = jobId ?? string.Empty,
                    Intent = request.IntentKind,
                    PriorityClass = request.PriorityClass,
                    Urgency01 = request.Urgency01,
                    HasTargetCell = request.HasTargetCell,
                    TargetCell = request.TargetCell,
                    TargetObjectId = request.TargetObjectId,
                    BeliefKey = request.BeliefKey ?? string.Empty,
                    DebugLabel = request.DebugLabel ?? string.Empty,
                    Reason = reason ?? string.Empty,
                    LegacyBridgeStillUsed = legacyBridgeStillUsed,
                },
            });
        }

        // =============================================================================
        // TryWriteJobLifecycleTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta una trace EL del ciclo di vita del job.
        /// </para>
        /// </summary>
        public static void TryWriteJobLifecycleTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            MemoryBeliefDecisionJobLifecycleOperation operation,
            Job job,
            string reason)
        {
            if (config == null || job == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobLifecycle,
                Tick = tick,
                NpcId = npcId,
                JobLifecycle = new MemoryBeliefDecisionJobLifecycleRecord
                {
                    Operation = operation,
                    Job = ToJobRef(job),
                    Reason = reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // TryWriteJobPhaseTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta una trace EL della fase corrente del job.
        /// </para>
        /// </summary>
        public static void TryWriteJobPhaseTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            MemoryBeliefDecisionJobPhaseOperation operation,
            Job job,
            JobPhase phase,
            int phaseIndex,
            string reason)
        {
            if (config == null || job == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobPhase,
                Tick = tick,
                NpcId = npcId,
                JobPhase = new MemoryBeliefDecisionJobPhaseRecord
                {
                    Operation = operation,
                    Job = ToJobRef(job),
                    Phase = ToJobPhaseRef(phase, phaseIndex),
                    Reason = reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // TryWriteStepTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta una trace EL di step e StepResult.
        /// </para>
        /// </summary>
        public static void TryWriteStepTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            Job job,
            JobPhase phase,
            int phaseIndex,
            JobAction step,
            int actionIndex,
            StepResult result,
            string reason)
        {
            if (config == null || job == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Step,
                Tick = tick,
                NpcId = npcId,
                Step = new MemoryBeliefDecisionStepRecord
                {
                    Job = ToJobRef(job),
                    Phase = ToJobPhaseRef(phase, phaseIndex),
                    Step = ToStepRef(step, actionIndex),
                    Result = ToStepResultRef(result),
                    Reason = reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // TryWriteJobStateTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta una snapshot diagnostica di <c>NpcJobState</c>.
        /// </para>
        /// </summary>
        public static void TryWriteJobStateTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            in NpcJobState jobState,
            string reason)
        {
            if (config == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobState,
                Tick = tick,
                NpcId = npcId,
                JobState = new MemoryBeliefDecisionJobStateRecord
                {
                    HasActiveJob = jobState.HasActiveJob,
                    ActiveJobId = jobState.ActiveJobId ?? string.Empty,
                    ActivePhaseIndex = jobState.ActivePhaseIndex,
                    ActiveActionIndex = jobState.ActiveActionIndex,
                    WaitUntilTick = jobState.WaitUntilTick,
                    SuspendedJobId = jobState.SuspendedJobId ?? string.Empty,
                    LastFailureReason = jobState.LastFailureReason,
                    Reason = reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // TryWriteJobArbitrationTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta la decisione dell'arbitro tra job corrente e job proposto.
        /// </para>
        /// </summary>
        public static void TryWriteJobArbitrationTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            Job currentJob,
            Job proposedJob,
            JobArbitrationResult arbitration)
        {
            if (config == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.JobArbitration,
                Tick = tick,
                NpcId = npcId,
                JobArbitration = new MemoryBeliefDecisionJobArbitrationRecord
                {
                    CurrentJob = currentJob != null ? ToJobRef(currentJob) : new MemoryBeliefDecisionJobRef(),
                    ProposedJob = proposedJob != null ? ToJobRef(proposedJob) : new MemoryBeliefDecisionJobRef(),
                    Decision = arbitration.Decision,
                    AcceptedJobId = arbitration.AcceptedJobId ?? string.Empty,
                    Reason = arbitration.Reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // TryWriteReservationTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta una trace diagnostica del Reservation layer.
        /// </para>
        /// </summary>
        public static void TryWriteReservationTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            MemoryBeliefDecisionReservationOperation operation,
            in ReservationRecord reservation,
            string reason)
        {
            if (config == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Reservation,
                Tick = tick,
                NpcId = npcId,
                Reservation = new MemoryBeliefDecisionReservationRecord
                {
                    Operation = operation,
                    ReservationId = reservation.ReservationId ?? string.Empty,
                    JobId = reservation.JobId ?? string.Empty,
                    OwnerNpcId = reservation.NpcId,
                    TargetKind = reservation.TargetKind,
                    TargetCell = reservation.TargetCell,
                    TargetObjectId = reservation.TargetObjectId,
                    CreatedTick = reservation.CreatedTick,
                    ExpiresTick = reservation.ExpiresTick,
                    Reason = reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // TryWriteCommandTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta una trace diagnostica del confine JobCommandBuffer -> ICommand.
        /// </para>
        /// </summary>
        public static void TryWriteCommandTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            MemoryBeliefDecisionCommandOperation operation,
            string jobId,
            string commandName,
            int queueCount,
            string reason)
        {
            if (config == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Command,
                Tick = tick,
                NpcId = npcId,
                Command = new MemoryBeliefDecisionCommandRecord
                {
                    Operation = operation,
                    JobId = jobId ?? string.Empty,
                    CommandName = commandName ?? string.Empty,
                    QueueCount = queueCount,
                    Reason = reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // TryWriteFailureLearningTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta una trace diagnostica del failure learning per target cella.
        /// </para>
        /// </summary>
        public static void TryWriteFailureLearningTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            MemoryBeliefDecisionExplainabilityRegistry registry,
            int npcId,
            long tick,
            string jobId,
            Vector2Int targetCell,
            JobFailureReason failureReason,
            int failureTick,
            float penalty01,
            string reason)
        {
            if (config == null)
                return;

            TryWriteTrace(config, registry, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.FailureLearning,
                Tick = tick,
                NpcId = npcId,
                FailureLearning = new MemoryBeliefDecisionFailureLearningRecord
                {
                    JobId = jobId ?? string.Empty,
                    TargetCell = targetCell,
                    FailureReason = failureReason,
                    FailureTick = failureTick,
                    Penalty01 = Mathf.Clamp01(penalty01),
                    Reason = reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // ToBeliefRef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una <c>BeliefEntry</c> in snapshot EL serializzabile.
        /// </para>
        ///
        /// <para><b>Nessun riferimento live allo store</b></para>
        /// <para>
        /// La copia contiene solo valori primitivi e la posizione stimata. Il file
        /// JSONL non puo' quindi diventare un canale alternativo per leggere o mutare
        /// la struttura interna del BeliefStore.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Categoria/status/source</b>: identita' semantica e stato operativo.</item>
        ///   <item><b>BeliefId</b>: id locale per-NPC.</item>
        ///   <item><b>Qualita'</b>: confidence, freshness e source count.</item>
        /// </list>
        /// </summary>
        private static MemoryBeliefDecisionBeliefRef ToBeliefRef(BeliefEntry belief)
        {
            return new MemoryBeliefDecisionBeliefRef
            {
                Category = belief.Category,
                Status = belief.Status,
                Source = belief.Source,
                BeliefId = belief.BeliefId,
                EstimatedPosition = belief.EstimatedPosition,
                Confidence = belief.Confidence,
                Freshness = belief.Freshness,
                SourceCount = belief.SourceCount,
            };
        }

        // =============================================================================
        // ToJobRef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un <c>Job</c> runtime in snapshot serializzabile EL.
        /// </para>
        /// </summary>
        private static MemoryBeliefDecisionJobRef ToJobRef(Job job)
        {
            if (job == null)
                return new MemoryBeliefDecisionJobRef();

            return new MemoryBeliefDecisionJobRef
            {
                JobId = job.JobId ?? string.Empty,
                RequestId = job.Request.RequestId ?? string.Empty,
                Intent = job.Request.IntentKind,
                PriorityClass = job.Request.PriorityClass,
                Urgency01 = job.Request.Urgency01,
                Status = job.Status,
                FailureReason = job.FailureReason,
                CreatedTick = job.CreatedTick,
                UpdatedTick = job.UpdatedTick,
                ActivePhaseIndex = job.ActivePhaseIndex,
                HasTargetCell = job.Request.HasTargetCell,
                TargetCell = job.Request.TargetCell,
                TargetObjectId = job.Request.TargetObjectId,
                DebugLabel = job.Request.DebugLabel ?? string.Empty,
            };
        }

        // =============================================================================
        // ToJobPhaseRef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una <c>JobPhase</c> runtime in snapshot serializzabile EL.
        /// </para>
        /// </summary>
        private static MemoryBeliefDecisionJobPhaseRef ToJobPhaseRef(JobPhase phase, int phaseIndex)
        {
            return new MemoryBeliefDecisionJobPhaseRef
            {
                PhaseId = phase.PhaseId ?? string.Empty,
                Kind = phase.Kind,
                DisplayName = phase.DisplayName ?? string.Empty,
                PhaseIndex = phaseIndex,
                ExpectedStepCount = phase.ExpectedStepCount,
                IsInterruptible = phase.IsInterruptible,
            };
        }

        // =============================================================================
        // ToStepRef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una <c>JobAction</c> runtime in snapshot serializzabile EL.
        /// </para>
        /// </summary>
        private static MemoryBeliefDecisionStepRef ToStepRef(JobAction step, int actionIndex)
        {
            return new MemoryBeliefDecisionStepRef
            {
                ActionId = step.ActionId ?? string.Empty,
                Kind = step.Kind,
                Label = step.Label ?? string.Empty,
                ActionIndex = actionIndex,
                HasTargetCell = step.HasTargetCell,
                TargetCell = step.TargetCell,
                TargetObjectId = step.TargetObjectId,
                DurationTicks = step.DurationTicks,
                PayloadKey = step.PayloadKey ?? string.Empty,
            };
        }

        // =============================================================================
        // ToStepResultRef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte uno <c>StepResult</c> runtime in snapshot serializzabile EL.
        /// </para>
        /// </summary>
        private static MemoryBeliefDecisionStepResultRef ToStepResultRef(StepResult result)
        {
            return new MemoryBeliefDecisionStepResultRef
            {
                Status = result.Status,
                FailureReason = result.FailureReason,
                SuggestedWaitTicks = result.SuggestedWaitTicks,
                DiagnosticMessage = result.DiagnosticMessage ?? string.Empty,
            };
        }
    }
}
