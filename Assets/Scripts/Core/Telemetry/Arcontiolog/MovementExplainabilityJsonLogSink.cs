using System;
using System.IO;
using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MovementExplainabilityJsonLogSink
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sink diagnostico append-only per esportare le trace EL pathfinding in formato
    /// JSONL. Ogni chiamata produce al massimo una riga JSON autonoma, pensata per
    /// debug esterno, analisi offline e confronto con il pannello runtime.
    /// </para>
    ///
    /// <para><b>Separazione simulazione / log</b></para>
    /// <para>
    /// Il sink non decide path, non modifica registry, non rilegge BeliefStore e non
    /// restituisce valori al movement. Riceve trace gia' costruite dall'emitter e le
    /// copia su disco solo se la configurazione lo richiede.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryWriteIntent</b>: export della trace intent.</item>
    ///   <item><b>TryWritePlan</b>: export della trace plan.</item>
    ///   <item><b>TryWriteExecutionEvent</b>: export degli eventi runtime.</item>
    ///   <item><b>ResolveLogPath</b>: risolve il file JSONL della sessione corrente.</item>
    /// </list>
    /// </summary>
    public static class MovementExplainabilityJsonLogSink
    {
        private const string SchemaVersion = "el_pathfinding.v1";
        private const string DefaultDirectoryName = "Arcontio_EL_Pathfinding";

        private static string _resolvedPath = string.Empty;
        private static string _resolvedPattern = string.Empty;
        private static bool _hasReportedWriteError;

        // =============================================================================
        // TryWriteIntent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive una riga JSONL per una <see cref="MovementIntentTrace"/>. La funzione
        /// accetta null e configurazioni spente come no-op, cosi' l'emitter puo'
        /// chiamarla senza duplicare controlli di sicurezza.
        /// </para>
        /// </summary>
        public static void TryWriteIntent(MovementExplainabilityParams config, MovementIntentTrace trace)
        {
            if (trace == null)
                return;

            TryWriteRecord(config, new MovementExplainabilityJsonLogRecord
            {
                schema = SchemaVersion,
                kind = "intent",
                npcId = trace.NpcId,
                tick = trace.Tick,
                intentId = trace.IntentId,
                planId = 0,
                intent = BuildIntentPayload(trace),
            });
        }

        // =============================================================================
        // TryWritePlan
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive una riga JSONL per una <see cref="PathPlanTrace"/>. La trace resta
        /// un export diagnostico: non viene trasformata in input per il planner.
        /// </para>
        /// </summary>
        public static void TryWritePlan(MovementExplainabilityParams config, PathPlanTrace trace)
        {
            if (trace == null)
                return;

            TryWriteRecord(config, new MovementExplainabilityJsonLogRecord
            {
                schema = SchemaVersion,
                kind = "plan",
                npcId = trace.NpcId,
                tick = trace.Tick,
                intentId = trace.IntentId,
                planId = trace.PlanId,
                plan = BuildPlanPayload(trace),
            });
        }

        // =============================================================================
        // TryWriteExecutionEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive una riga JSONL per un <see cref="PathExecutionEvent"/>. Gli eventi
        /// sono gia' filtrati dalla verbosita' nell'emitter; il sink non applica una
        /// seconda policy comportamentale.
        /// </para>
        /// </summary>
        public static void TryWriteExecutionEvent(MovementExplainabilityParams config, PathExecutionEvent evt)
        {
            if (evt == null)
                return;

            TryWriteRecord(config, new MovementExplainabilityJsonLogRecord
            {
                schema = SchemaVersion,
                kind = "event",
                npcId = evt.NpcId,
                tick = evt.Tick,
                intentId = evt.IntentId,
                planId = evt.PlanId,
                executionEvent = BuildEventPayload(evt),
            });
        }

        // =============================================================================
        // TryWriteRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Appende il record al file JSONL corrente. Gli errori di IO vengono assorbiti
        /// e riportati una sola volta tramite Debug.LogWarning, perche' il logging non
        /// deve mai interrompere la simulazione.
        /// </para>
        /// </summary>
        private static void TryWriteRecord(MovementExplainabilityParams config, MovementExplainabilityJsonLogRecord record)
        {
            if (config == null || !config.writeJsonLog || record == null)
                return;

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
                Debug.LogWarning($"[EL Pathfinding] JSONL log write failed: {ex.Message}");
            }
        }

        // =============================================================================
        // ResolveLogPath
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve una sola volta il percorso del file JSONL per la sessione runtime.
        /// Il pattern puo' contenere il token <c>{yyyyMMdd_HHmmss}</c>, sostituito con
        /// il timestamp locale di creazione del sink.
        /// </para>
        /// </summary>
        private static string ResolveLogPath(MovementExplainabilityParams config)
        {
            string pattern = string.IsNullOrWhiteSpace(config.jsonLogFileNamePattern)
                ? "arcontio_el_pathfinding_{yyyyMMdd_HHmmss}.jsonl"
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

        // =============================================================================
        // BuildIntentPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una trace intent interna in una payload JSONL leggibile. Gli enum
        /// vengono trasformati in stringhe per evitare i codici numerici prodotti da
        /// <see cref="JsonUtility"/> sui tipi runtime originali.
        /// </para>
        /// </summary>
        private static MovementIntentLogPayload BuildIntentPayload(MovementIntentTrace trace)
        {
            return new MovementIntentLogPayload
            {
                sourceJobId = trace.SourceJobId ?? string.Empty,
                sourceStepId = trace.SourceStepId ?? string.Empty,
                movementPurpose = trace.MovementPurpose.ToString(),
                targetType = trace.TargetType.ToString(),
                targetCell = BuildCell(trace.TargetCell),
                targetCellText = FormatCell(trace.TargetCell),
                targetObjectId = trace.TargetObjectId,
                hasBeliefBasis = trace.HasBeliefBasis,
                beliefBasis = BuildBelief(trace.BeliefBasis),
                urgency = trace.Urgency,
                verbosityLevel = trace.VerbosityLevel,
            };
        }

        // =============================================================================
        // BuildPlanPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una plan trace interna in payload JSONL orientata alla lettura
        /// umana. In particolare, modalita', ragione di scelta e candidati espongono
        /// sia valori strutturati sia testo descrittivo.
        /// </para>
        /// </summary>
        private static PathPlanLogPayload BuildPlanPayload(PathPlanTrace trace)
        {
            var payload = new PathPlanLogPayload
            {
                startCell = BuildCell(trace.StartCell),
                startCellText = FormatCell(trace.StartCell),
                goalCell = BuildCell(trace.GoalCell),
                goalCellText = FormatCell(trace.GoalCell),
                selectedMode = trace.SelectedMode.ToString(),
                selectionReason = trace.SelectionReason.ToString(),
                macroRouteNodes = trace.MacroRouteNodes ?? Array.Empty<int>(),
                macroRouteCost = trace.MacroRouteCost,
                macroRouteCostText = FormatCost(trace.MacroRouteCost),
                hasLocalRouteFirstStep = trace.HasLocalRouteFirstStep,
                localRouteFirstStep = BuildCell(trace.LocalRouteFirstStep),
                localRouteFirstStepText = trace.HasLocalRouteFirstStep ? FormatCell(trace.LocalRouteFirstStep) : "Primo passo non disponibile",
                verbosityLevel = trace.VerbosityLevel,
                candidates = BuildCandidates(trace.Candidates),
            };

            return payload;
        }

        // =============================================================================
        // BuildEventPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un evento runtime in payload JSONL leggibile. Il tipo evento,
        /// failure detail e door detail vengono esportati come stringhe invece che
        /// come codici enum numerici.
        /// </para>
        /// </summary>
        private static PathExecutionEventLogPayload BuildEventPayload(PathExecutionEvent evt)
        {
            return new PathExecutionEventLogPayload
            {
                eventType = evt.EventType.ToString(),
                activeMode = evt.ActiveMode ?? string.Empty,
                currentCell = BuildCell(evt.CurrentCell),
                currentCellText = FormatCell(evt.CurrentCell),
                targetCell = BuildCell(evt.TargetCell),
                targetCellText = FormatCell(evt.TargetCell),
                hasFailureDetail = evt.HasFailureDetail,
                failureDetail = BuildFailure(evt.FailureDetail),
                hasDoorDetail = evt.HasDoorDetail,
                doorDetail = BuildDoor(evt.DoorDetail),
                verbosityLevel = evt.VerbosityLevel,
                summary = evt.Summary ?? string.Empty,
            };
        }

        // =============================================================================
        // BuildCandidates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la lista dei candidati plan in un array serializzabile con enum
        /// testuali. L'array vuoto mantiene il record JSON stabile anche quando la
        /// trace non ha candidati.
        /// </para>
        /// </summary>
        private static PlannerCandidateLogPayload[] BuildCandidates(System.Collections.Generic.List<PlannerCandidate> candidates)
        {
            if (candidates == null || candidates.Count <= 0)
                return Array.Empty<PlannerCandidateLogPayload>();

            var output = new PlannerCandidateLogPayload[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                output[i] = new PlannerCandidateLogPayload
                {
                    mode = candidate.Mode.ToString(),
                    valid = candidate.Valid,
                    estimatedCost = candidate.EstimatedCost,
                    costText = FormatCost(candidate.EstimatedCost),
                    invalidReason = candidate.InvalidReason.ToString(),
                    note = candidate.Note ?? string.Empty,
                    summary = $"{candidate.Mode} | {(candidate.Valid ? "valido" : "scartato: " + candidate.InvalidReason)} | {FormatCost(candidate.EstimatedCost)}",
                };
            }

            return output;
        }

        // =============================================================================
        // BuildBelief
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte lo snapshot belief in payload leggibile. Anche qui la categoria
        /// enum viene duplicata come stringa per favorire analisi manuale e tool esterni.
        /// </para>
        /// </summary>
        private static BeliefEntryLogPayload BuildBelief(BeliefEntryRef belief)
        {
            return new BeliefEntryLogPayload
            {
                category = belief.Category.ToString(),
                beliefId = belief.BeliefId,
                entityId = belief.EntityId,
                confidence = belief.Confidence,
                freshness = belief.Freshness,
                ageTicks = belief.AgeTicks,
            };
        }

        // =============================================================================
        // BuildFailure
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il dettaglio fallimento in payload testuale. Viene sempre costruito
        /// un oggetto stabile, ma il chiamante usa `hasFailureDetail` per sapere se
        /// il contenuto e' semanticamente valido.
        /// </para>
        /// </summary>
        private static FailureDetailLogPayload BuildFailure(FailureDetail detail)
        {
            return new FailureDetailLogPayload
            {
                failureType = detail.FailureType.ToString(),
                hasBlockingCell = detail.HasBlockingCell,
                blockingCell = BuildCell(detail.BlockingCell),
                blockingCellText = detail.HasBlockingCell ? FormatCell(detail.BlockingCell) : "cella n/d",
                hasBlockingNpcId = detail.HasBlockingNpcId,
                blockingNpcId = detail.BlockingNpcId,
                blockedTicks = detail.BlockedTicks,
                backOffStage = detail.BackOffStage,
                lastActiveMode = detail.LastActiveMode ?? string.Empty,
                oscillationFlag = detail.OscillationFlag,
            };
        }

        // =============================================================================
        // BuildDoor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il dettaglio porta in payload leggibile, preservando id/cella e
        /// trasformando gli stati porta in stringhe.
        /// </para>
        /// </summary>
        private static DoorInteractionLogPayload BuildDoor(DoorInteractionDetail detail)
        {
            return new DoorInteractionLogPayload
            {
                doorObjectId = detail.DoorObjectId,
                doorCell = BuildCell(detail.DoorCell),
                doorCellText = FormatCell(detail.DoorCell),
                stateBefore = detail.StateBefore.ToString(),
                stateAfter = detail.StateAfter.ToString(),
                commandEmitted = detail.CommandEmitted,
                accessGranted = detail.AccessGranted,
            };
        }

        // =============================================================================
        // BuildCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una cella Unity in DTO serializzabile con campi scalari semplici.
        /// </para>
        /// </summary>
        private static CellLogPayload BuildCell(Vector2Int cell)
        {
            return new CellLogPayload
            {
                x = cell.x,
                y = cell.y,
            };
        }

        // =============================================================================
        // FormatCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea la rappresentazione testuale della cella per lettura immediata nel
        /// JSONL senza dover ricostruire manualmente `x` e `y`.
        /// </para>
        /// </summary>
        private static string FormatCell(Vector2Int cell)
        {
            return $"({cell.x}, {cell.y})";
        }

        // =============================================================================
        // FormatCost
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un costo diagnostico in testo leggibile. Nel contratto corrente
        /// valori negativi indicano costo non disponibile o non applicabile.
        /// </para>
        /// </summary>
        private static string FormatCost(float cost)
        {
            return cost >= 0f ? $"costo {cost:0.00}" : "costo n/d";
        }

        // =============================================================================
        // MovementExplainabilityJsonLogRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// DTO serializzabile della riga JSONL. Contiene un envelope comune e una sola
        /// payload principale valorizzata tra intent, plan ed executionEvent.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class MovementExplainabilityJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public int npcId;
            public long tick;
            public int intentId;
            public int planId;
            public MovementIntentLogPayload intent;
            public PathPlanLogPayload plan;
            public PathExecutionEventLogPayload executionEvent;
        }

        // =============================================================================
        // MovementIntentLogPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Payload JSONL leggibile della trace intent. Duplica gli enum come stringhe
        /// per rendere il file consultabile senza dover conoscere i valori numerici C#.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class MovementIntentLogPayload
        {
            public string sourceJobId = string.Empty;
            public string sourceStepId = string.Empty;
            public string movementPurpose = string.Empty;
            public string targetType = string.Empty;
            public CellLogPayload targetCell;
            public string targetCellText = string.Empty;
            public int targetObjectId;
            public bool hasBeliefBasis;
            public BeliefEntryLogPayload beliefBasis;
            public float urgency;
            public int verbosityLevel;
        }

        // =============================================================================
        // PathPlanLogPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Payload JSONL leggibile del piano pathfinding. Espone modalita', ragione,
        /// celle e candidati in forma adatta a lettori esterni e analisi manuale.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class PathPlanLogPayload
        {
            public CellLogPayload startCell;
            public string startCellText = string.Empty;
            public CellLogPayload goalCell;
            public string goalCellText = string.Empty;
            public string selectedMode = string.Empty;
            public string selectionReason = string.Empty;
            public int[] macroRouteNodes = Array.Empty<int>();
            public float macroRouteCost;
            public string macroRouteCostText = string.Empty;
            public bool hasLocalRouteFirstStep;
            public CellLogPayload localRouteFirstStep;
            public string localRouteFirstStepText = string.Empty;
            public int verbosityLevel;
            public PlannerCandidateLogPayload[] candidates = Array.Empty<PlannerCandidateLogPayload>();
        }

        // =============================================================================
        // PathExecutionEventLogPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Payload JSONL leggibile dell'evento runtime. Mantiene separati evento,
        /// failure detail e door detail senza esporre enum serializzati come numeri.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class PathExecutionEventLogPayload
        {
            public string eventType = string.Empty;
            public string activeMode = string.Empty;
            public CellLogPayload currentCell;
            public string currentCellText = string.Empty;
            public CellLogPayload targetCell;
            public string targetCellText = string.Empty;
            public bool hasFailureDetail;
            public FailureDetailLogPayload failureDetail;
            public bool hasDoorDetail;
            public DoorInteractionLogPayload doorDetail;
            public int verbosityLevel;
            public string summary = string.Empty;
        }

        // =============================================================================
        // PlannerCandidateLogPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riga candidato del piano in formato JSONL. Include costo numerico e testo
        /// gia' pronto, cosi' il lettore non deve ricostruire la semantica di -1.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class PlannerCandidateLogPayload
        {
            public string mode = string.Empty;
            public bool valid;
            public float estimatedCost;
            public string costText = string.Empty;
            public string invalidReason = string.Empty;
            public string note = string.Empty;
            public string summary = string.Empty;
        }

        // =============================================================================
        // BeliefEntryLogPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Snapshot belief leggibile per JSONL. La categoria viene esportata come
        /// stringa, mantenendo id e valori scalari originali.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class BeliefEntryLogPayload
        {
            public string category = string.Empty;
            public int beliefId;
            public int entityId;
            public float confidence;
            public float freshness;
            public long ageTicks;
        }

        // =============================================================================
        // FailureDetailLogPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Dettaglio fallimento in formato JSONL leggibile. Conserva sia coordinate
        /// strutturate sia testo di supporto per debugging rapido.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class FailureDetailLogPayload
        {
            public string failureType = string.Empty;
            public bool hasBlockingCell;
            public CellLogPayload blockingCell;
            public string blockingCellText = string.Empty;
            public bool hasBlockingNpcId;
            public int blockingNpcId;
            public int blockedTicks;
            public int backOffStage;
            public string lastActiveMode = string.Empty;
            public bool oscillationFlag;
        }

        // =============================================================================
        // DoorInteractionLogPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Dettaglio porta in formato JSONL leggibile. Gli stati porta vengono scritti
        /// come stringhe per evitare mapping manuale dei codici enum.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class DoorInteractionLogPayload
        {
            public int doorObjectId;
            public CellLogPayload doorCell;
            public string doorCellText = string.Empty;
            public string stateBefore = string.Empty;
            public string stateAfter = string.Empty;
            public bool commandEmitted;
            public bool accessGranted;
        }

        // =============================================================================
        // CellLogPayload
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cella serializzabile minima usata dalle payload JSONL. Affianca i campi
        /// testuali `*CellText` quando il lettore vuole coordinate strutturate.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class CellLogPayload
        {
            public int x;
            public int y;
        }
    }
}
