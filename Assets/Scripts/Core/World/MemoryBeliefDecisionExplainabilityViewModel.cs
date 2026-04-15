// =============================================================================
// MemoryBeliefDecisionExplainabilityViewModel.cs
// Namespace: Arcontio.Core
// Sessione: v0.05.43-EL_MBQD_ViewModel
// =============================================================================
//
// ViewModel runtime read-only per l'Explainability Layer Memory/Belief/Query/
// Decision. Il builder legge soltanto il registry EL-MBQD gia' popolato dagli
// emitter: non interroga MemoryStore, BeliefStore, World.Objects o FoodStocks.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MemoryBeliefDecisionExplainabilityViewModel
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot UI-friendly dell'EL-MBQD per un singolo NPC selezionato.
    /// </para>
    ///
    /// <para><b>Boundary Registry -> UI</b></para>
    /// <para>
    /// Il modello contiene solo valori copiati e formattati dal registry runtime.
    /// La UI puo' quindi visualizzare Memory, Belief, Query, Decision e Bridge senza
    /// conoscere ring buffer, DTO JSONL, store cognitivi o stato oggettivo del mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Counts</b>: conteggi per famiglia diagnostica.</item>
    ///   <item><b>Latest*</b>: ultimo snapshot di ogni famiglia.</item>
    ///   <item><b>MemoryBars</b>: aggregazione per traceType delle memory recenti.</item>
    ///   <item><b>BeliefRows</b>: belief recenti deduplicate per id/categoria.</item>
    ///   <item><b>Timeline</b>: righe recenti combinate e ordinate per tick.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MemoryBeliefDecisionExplainabilityViewModel
    {
        public bool HasNpc;
        public int NpcId;
        public long LatestTick;
        public int MemoryCount;
        public int BeliefCount;
        public int QueryCount;
        public int DecisionCount;
        public int BridgeCount;
        public string HeaderTitle = string.Empty;
        public string HeaderSubtitle = string.Empty;
        public readonly List<MemoryBeliefDecisionMetricView> MemoryBars = new(12);
        public readonly List<MemoryBeliefDecisionBeliefView> BeliefRows = new(16);
        public readonly MemoryBeliefDecisionMemoryView LatestMemory = new();
        public readonly MemoryBeliefDecisionBeliefMutationView LatestBeliefMutation = new();
        public readonly MemoryBeliefDecisionQueryView LatestQuery = new();
        public readonly MemoryBeliefDecisionDecisionView LatestDecision = new();
        public readonly MemoryBeliefDecisionBridgeView LatestBridge = new();
        public readonly List<MemoryBeliefDecisionTimelineView> Timeline = new(48);
    }

    // =============================================================================
    // MemoryBeliefDecisionColorRole
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ruolo cromatico semantico usato dalla UI EL-MBQD.
    /// </para>
    ///
    /// <para><b>Colori come linguaggio diagnostico</b></para>
    /// <para>
    /// Il ViewModel non contiene colori Unity: espone ruoli stabili che la view puo'
    /// mappare su verde, rosso, bianco, grigio, ambra e blu come nel mockup.
    /// </para>
    /// </summary>
    public enum MemoryBeliefDecisionColorRole
    {
        Primary = 0,
        Muted = 1,
        Ok = 2,
        Warning = 3,
        Error = 4,
        Info = 5
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionMetricView
    {
        public string Label = string.Empty;
        public int Count;
        public float Fill01;
        public MemoryBeliefDecisionColorRole ColorRole;
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionMemoryView
    {
        public bool HasValue;
        public long Tick;
        public string EventType = string.Empty;
        public string TraceType = string.Empty;
        public int SubjectId;
        public int SecondarySubjectId;
        public string SubjectDefId = string.Empty;
        public string Cell = string.Empty;
        public float Intensity01;
        public float Reliability01;
        public bool IsHeard;
        public string HeardKind = string.Empty;
        public int SourceSpeakerId;
        public string StoreResult = string.Empty;
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionBeliefView
    {
        public int BeliefId;
        public string Category = string.Empty;
        public string Status = string.Empty;
        public string Source = string.Empty;
        public string EstimatedCell = string.Empty;
        public float Confidence;
        public float Freshness;
        public int SourceCount;
        public MemoryBeliefDecisionColorRole ColorRole;
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionBeliefMutationView
    {
        public bool HasValue;
        public long Tick;
        public string Operation = string.Empty;
        public bool HasSourceTrace;
        public string SourceTraceType = string.Empty;
        public string Reason = string.Empty;
        public readonly MemoryBeliefDecisionBeliefView Belief = new();
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionContributionView
    {
        public string Label = string.Empty;
        public float Value;
        public MemoryBeliefDecisionColorRole ColorRole;
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionQueryView
    {
        public bool HasValue;
        public long Tick;
        public string GoalType = string.Empty;
        public float Urgency01;
        public string NpcCell = string.Empty;
        public float MinConfidence;
        public int CandidateCount;
        public int UsableCandidateCount;
        public bool IsEmpty;
        public string EmptyReason = string.Empty;
        public float FinalScore;
        public readonly MemoryBeliefDecisionBeliefView Winner = new();
        public readonly List<MemoryBeliefDecisionContributionView> Contributions = new(8);
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionCandidateView
    {
        public string Intent = string.Empty;
        public bool Available;
        public string Need = string.Empty;
        public float NeedUrgency01;
        public bool IsCritical;
        public bool RequiresBeliefTarget;
        public bool BeliefResultEmpty;
        public string FilteredReason = string.Empty;
        public float Score;
        public readonly MemoryBeliefDecisionBeliefView Belief = new();
        public readonly List<MemoryBeliefDecisionContributionView> Contributions = new(8);
        public MemoryBeliefDecisionColorRole ColorRole;
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionDecisionView
    {
        public bool HasValue;
        public long Tick;
        public bool AuditValid;
        public int CandidateCount;
        public string SelectedIntent = string.Empty;
        public float SelectedScore;
        public int SelectedIndex;
        public int SelectionTopN;
        public float SelectionNoise01;
        public float Impulsivity01;
        public float EffectiveNoise01;
        public readonly List<MemoryBeliefDecisionCandidateView> Candidates = new(12);
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionBridgeView
    {
        public bool HasValue;
        public long Tick;
        public string SelectedIntent = string.Empty;
        public string CommandName = string.Empty;
        public bool Handled;
        public bool DidMove;
        public bool DidSteal;
        public string TargetCell = string.Empty;
        public string TargetSource = string.Empty;
        public bool LegacyFallbackUsed;
        public string Reason = string.Empty;
        public MemoryBeliefDecisionColorRole ColorRole;
    }

    [Serializable]
    public sealed class MemoryBeliefDecisionTimelineView
    {
        public long Tick;
        public string Kind = string.Empty;
        public string Summary = string.Empty;
        public MemoryBeliefDecisionColorRole ColorRole;
    }

    // =============================================================================
    // MemoryBeliefDecisionExplainabilityViewModelBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder read-only che costruisce un ViewModel EL-MBQD a partire dal registry
    /// runtime contenuto nel <see cref="World"/>.
    /// </para>
    ///
    /// <para><b>Nessuna rilettura cognitiva</b></para>
    /// <para>
    /// Il builder non legge MemoryStore, BeliefStore o oggetti fisici. Se una riga
    /// deve comparire nel pannello, deve essere stata emessa come trace EL-MBQD.
    /// Questo mantiene il pannello allineato al JSONL e impedisce onniscienza debug.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildForNpc</b>: entry point usato dalla UI.</item>
    ///   <item><b>Fill*</b>: conversioni specifiche per famiglia diagnostica.</item>
    ///   <item><b>BuildTimeline</b>: timeline combinata memory/belief/query/decision/bridge.</item>
    ///   <item><b>Resolve*</b>: ruoli cromatici e riassunti testuali.</item>
    /// </list>
    /// </summary>
    public static class MemoryBeliefDecisionExplainabilityViewModelBuilder
    {
        private const int DefaultMaxTimelineRows = 48;
        private static readonly List<MemoryBeliefDecisionTrace> TraceBuffer = new(128);
        private static readonly List<MemoryBeliefDecisionTrace> TimelineBuffer = new(128);

        public static bool BuildForNpc(
            World world,
            int npcId,
            MemoryBeliefDecisionExplainabilityViewModel output,
            int maxTimelineRows = DefaultMaxTimelineRows)
        {
            if (output == null)
                return false;

            Reset(output, npcId);

            if (world == null
                || world.MemoryBeliefDecisionExplainability == null
                || !world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store))
            {
                output.HeaderTitle = $"NPC #{npcId}";
                output.HeaderSubtitle = "EL-MBQD non disponibile per questo NPC";
                return false;
            }

            output.HasNpc = true;
            output.NpcId = npcId;
            output.LatestTick = store.LatestTick;
            output.MemoryCount = store.MemoryTraceCount;
            output.BeliefCount = store.BeliefTraceCount;
            output.QueryCount = store.QueryTraceCount;
            output.DecisionCount = store.DecisionTraceCount;
            output.BridgeCount = store.BridgeTraceCount;
            output.HeaderTitle = $"NPC #{npcId}";
            output.HeaderSubtitle = $"tick {store.LatestTick} | M {store.MemoryTraceCount} | B {store.BeliefTraceCount} | Q {store.QueryTraceCount} | D {store.DecisionTraceCount} | bridge {store.BridgeTraceCount}";

            if (store.TryGetLatestMemoryTrace(out var memoryTrace))
                FillMemory(output.LatestMemory, memoryTrace);

            if (store.TryGetLatestBeliefTrace(out var beliefTrace))
                FillBeliefMutation(output.LatestBeliefMutation, beliefTrace);

            if (store.TryGetLatestQueryTrace(out var queryTrace))
                FillQuery(output.LatestQuery, queryTrace);

            if (store.TryGetLatestDecisionTrace(out var decisionTrace))
                FillDecision(output.LatestDecision, decisionTrace);

            if (store.TryGetLatestBridgeTrace(out var bridgeTrace))
                FillBridge(output.LatestBridge, bridgeTrace);

            BuildMemoryBars(store, output.MemoryBars);
            BuildBeliefRows(store, output.BeliefRows);
            BuildTimeline(store, output.Timeline, maxTimelineRows);
            return true;
        }

        private static void Reset(MemoryBeliefDecisionExplainabilityViewModel output, int npcId)
        {
            output.HasNpc = false;
            output.NpcId = npcId;
            output.LatestTick = 0;
            output.MemoryCount = 0;
            output.BeliefCount = 0;
            output.QueryCount = 0;
            output.DecisionCount = 0;
            output.BridgeCount = 0;
            output.HeaderTitle = string.Empty;
            output.HeaderSubtitle = string.Empty;
            output.MemoryBars.Clear();
            output.BeliefRows.Clear();
            output.Timeline.Clear();
            ResetMemory(output.LatestMemory);
            ResetBeliefMutation(output.LatestBeliefMutation);
            ResetQuery(output.LatestQuery);
            ResetDecision(output.LatestDecision);
            ResetBridge(output.LatestBridge);
        }

        private static void FillMemory(MemoryBeliefDecisionMemoryView view, MemoryBeliefDecisionTrace trace)
        {
            ResetMemory(view);
            if (trace?.Memory == null)
                return;

            var memory = trace.Memory;
            view.HasValue = true;
            view.Tick = trace.Tick;
            view.EventType = memory.EventType ?? string.Empty;
            view.TraceType = memory.TraceType.ToString();
            view.SubjectId = memory.SubjectId;
            view.SecondarySubjectId = memory.SecondarySubjectId;
            view.SubjectDefId = memory.SubjectDefId ?? string.Empty;
            view.Cell = FormatCell(memory.Cell);
            view.Intensity01 = Mathf.Clamp01(memory.Intensity01);
            view.Reliability01 = Mathf.Clamp01(memory.Reliability01);
            view.IsHeard = memory.IsHeard;
            view.HeardKind = memory.HeardKind ?? string.Empty;
            view.SourceSpeakerId = memory.SourceSpeakerId;
            view.StoreResult = memory.StoreResult.ToString();
        }

        private static void FillBeliefMutation(MemoryBeliefDecisionBeliefMutationView view, MemoryBeliefDecisionTrace trace)
        {
            ResetBeliefMutation(view);
            if (trace?.Belief == null)
                return;

            var belief = trace.Belief;
            view.HasValue = true;
            view.Tick = trace.Tick;
            view.Operation = belief.Operation.ToString();
            view.HasSourceTrace = belief.HasSourceTrace;
            view.SourceTraceType = belief.HasSourceTrace ? belief.SourceTraceType.ToString() : string.Empty;
            view.Reason = belief.Reason ?? string.Empty;
            FillBelief(view.Belief, belief.Belief);
        }

        private static void FillQuery(MemoryBeliefDecisionQueryView view, MemoryBeliefDecisionTrace trace)
        {
            ResetQuery(view);
            if (trace?.Query == null)
                return;

            var query = trace.Query;
            view.HasValue = true;
            view.Tick = trace.Tick;
            view.GoalType = query.GoalType.ToString();
            view.Urgency01 = Mathf.Clamp01(query.Urgency01);
            view.NpcCell = FormatCell(query.NpcPosition);
            view.MinConfidence = query.MinConfidence;
            view.CandidateCount = query.CandidateCount;
            view.UsableCandidateCount = query.UsableCandidateCount;
            view.IsEmpty = query.IsEmpty;
            view.EmptyReason = query.EmptyReason ?? string.Empty;
            view.FinalScore = query.FinalScore;
            FillBelief(view.Winner, query.Winner);
            FillContributions(view.Contributions, query.Contributions);
        }

        private static void FillDecision(MemoryBeliefDecisionDecisionView view, MemoryBeliefDecisionTrace trace)
        {
            ResetDecision(view);
            if (trace?.Decision == null)
                return;

            var decision = trace.Decision;
            view.HasValue = true;
            view.Tick = trace.Tick;
            view.AuditValid = decision.AuditValid;
            view.CandidateCount = decision.CandidateCount;
            view.SelectedIntent = decision.SelectedIntent.ToString();
            view.SelectedScore = decision.SelectedScore;
            view.SelectedIndex = decision.SelectedIndex;
            view.SelectionTopN = decision.SelectionTopN;
            view.SelectionNoise01 = decision.SelectionNoise01;
            view.Impulsivity01 = decision.Impulsivity01;
            view.EffectiveNoise01 = decision.EffectiveNoise01;

            if (decision.Candidates == null)
                return;

            for (int i = 0; i < decision.Candidates.Length; i++)
            {
                var source = decision.Candidates[i];
                var candidate = new MemoryBeliefDecisionCandidateView
                {
                    Intent = source.Intent.ToString(),
                    Available = source.Available,
                    Need = source.Need.ToString(),
                    NeedUrgency01 = source.NeedUrgency01,
                    IsCritical = source.IsCritical,
                    RequiresBeliefTarget = source.RequiresBeliefTarget,
                    BeliefResultEmpty = source.BeliefResultEmpty,
                    FilteredReason = source.FilteredReason ?? string.Empty,
                    Score = source.Score,
                    ColorRole = source.Available ? MemoryBeliefDecisionColorRole.Ok : MemoryBeliefDecisionColorRole.Muted,
                };

                FillBelief(candidate.Belief, source.Belief);
                FillContributions(candidate.Contributions, source.ScoreContributions);
                view.Candidates.Add(candidate);
            }
        }

        private static void FillBridge(MemoryBeliefDecisionBridgeView view, MemoryBeliefDecisionTrace trace)
        {
            ResetBridge(view);
            if (trace?.Bridge == null)
                return;

            var bridge = trace.Bridge;
            view.HasValue = true;
            view.Tick = trace.Tick;
            view.SelectedIntent = bridge.SelectedIntent.ToString();
            view.CommandName = bridge.CommandName ?? string.Empty;
            view.Handled = bridge.Handled;
            view.DidMove = bridge.DidMove;
            view.DidSteal = bridge.DidSteal;
            view.TargetCell = FormatCell(bridge.TargetCell);
            view.TargetSource = bridge.TargetSource.ToString();
            view.LegacyFallbackUsed = bridge.LegacyFallbackUsed;
            view.Reason = bridge.Reason ?? string.Empty;
            view.ColorRole = bridge.LegacyFallbackUsed
                ? MemoryBeliefDecisionColorRole.Warning
                : (bridge.Handled ? MemoryBeliefDecisionColorRole.Ok : MemoryBeliefDecisionColorRole.Error);
        }

        private static void BuildMemoryBars(MemoryBeliefDecisionExplainabilityNpcStore store, List<MemoryBeliefDecisionMetricView> output)
        {
            output.Clear();
            TraceBuffer.Clear();
            store.CopyMemoryTracesTo(TraceBuffer);

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < TraceBuffer.Count; i++)
            {
                string key = TraceBuffer[i]?.Memory != null ? TraceBuffer[i].Memory.TraceType.ToString() : "Unknown";
                counts.TryGetValue(key, out int count);
                counts[key] = count + 1;
            }

            int max = 1;
            foreach (var kv in counts)
                max = Math.Max(max, kv.Value);

            foreach (var kv in counts)
            {
                output.Add(new MemoryBeliefDecisionMetricView
                {
                    Label = kv.Key,
                    Count = kv.Value,
                    Fill01 = kv.Value / (float)max,
                    ColorRole = ResolveTraceTypeColor(kv.Key),
                });
            }
        }

        private static void BuildBeliefRows(MemoryBeliefDecisionExplainabilityNpcStore store, List<MemoryBeliefDecisionBeliefView> output)
        {
            output.Clear();
            TraceBuffer.Clear();
            store.CopyBeliefTracesTo(TraceBuffer);

            for (int i = TraceBuffer.Count - 1; i >= 0; i--)
            {
                var record = TraceBuffer[i]?.Belief;
                if (record == null)
                    continue;

                if (ContainsBelief(output, record.Belief.BeliefId, record.Belief.Category))
                    continue;

                var view = new MemoryBeliefDecisionBeliefView();
                FillBelief(view, record.Belief);
                output.Add(view);
            }
        }

        private static void BuildTimeline(MemoryBeliefDecisionExplainabilityNpcStore store, List<MemoryBeliefDecisionTimelineView> output, int maxTimelineRows)
        {
            output.Clear();
            TimelineBuffer.Clear();
            store.CopyMemoryTracesTo(TimelineBuffer);
            store.CopyBeliefTracesTo(TimelineBuffer, clearOutput: false);
            store.CopyQueryTracesTo(TimelineBuffer, clearOutput: false);
            store.CopyDecisionTracesTo(TimelineBuffer, clearOutput: false);
            store.CopyBridgeTracesTo(TimelineBuffer, clearOutput: false);
            TimelineBuffer.Sort((a, b) => a.Tick.CompareTo(b.Tick));

            int safeMax = Math.Max(0, maxTimelineRows);
            int start = safeMax <= 0 ? TimelineBuffer.Count : Math.Max(0, TimelineBuffer.Count - safeMax);
            for (int i = TimelineBuffer.Count - 1; i >= start; i--)
            {
                var trace = TimelineBuffer[i];
                output.Add(new MemoryBeliefDecisionTimelineView
                {
                    Tick = trace.Tick,
                    Kind = trace.Kind.ToString(),
                    Summary = BuildTimelineSummary(trace),
                    ColorRole = ResolveTraceColor(trace),
                });
            }
        }

        private static void FillBelief(MemoryBeliefDecisionBeliefView view, MemoryBeliefDecisionBeliefRef belief)
        {
            view.BeliefId = belief.BeliefId;
            view.Category = belief.Category.ToString();
            view.Status = belief.Status.ToString();
            view.Source = belief.Source.ToString();
            view.EstimatedCell = FormatCell(belief.EstimatedPosition);
            view.Confidence = Mathf.Clamp01(belief.Confidence);
            view.Freshness = Mathf.Clamp01(belief.Freshness);
            view.SourceCount = belief.SourceCount;
            view.ColorRole = ResolveBeliefStatusColor(view.Status);
        }

        private static void FillContributions(List<MemoryBeliefDecisionContributionView> output, MemoryBeliefDecisionScoreContributionRef[] contributions)
        {
            output.Clear();
            if (contributions == null)
                return;

            for (int i = 0; i < contributions.Length; i++)
            {
                float value = contributions[i].Value;
                output.Add(new MemoryBeliefDecisionContributionView
                {
                    Label = contributions[i].Label ?? string.Empty,
                    Value = value,
                    ColorRole = value < 0f ? MemoryBeliefDecisionColorRole.Error : MemoryBeliefDecisionColorRole.Primary,
                });
            }
        }

        private static bool ContainsBelief(List<MemoryBeliefDecisionBeliefView> rows, int beliefId, BeliefCategory category)
        {
            string categoryText = category.ToString();
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].BeliefId == beliefId && string.Equals(rows[i].Category, categoryText, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string BuildTimelineSummary(MemoryBeliefDecisionTrace trace)
        {
            return trace.Kind switch
            {
                MemoryBeliefDecisionTraceKind.Memory => trace.Memory == null ? "memory vuota" : $"{trace.Memory.TraceType} {FormatCell(trace.Memory.Cell)} result={trace.Memory.StoreResult}",
                MemoryBeliefDecisionTraceKind.Belief => trace.Belief == null ? "belief vuota" : $"{trace.Belief.Operation} {trace.Belief.Belief.Category}#{trace.Belief.Belief.BeliefId} {FormatCell(trace.Belief.Belief.EstimatedPosition)}",
                MemoryBeliefDecisionTraceKind.Query => trace.Query == null ? "query vuota" : $"{trace.Query.GoalType} usable={trace.Query.UsableCandidateCount}/{trace.Query.CandidateCount} score={trace.Query.FinalScore:0.00}",
                MemoryBeliefDecisionTraceKind.Decision => trace.Decision == null ? "decision vuota" : $"{trace.Decision.SelectedIntent} score={trace.Decision.SelectedScore:0.00}",
                MemoryBeliefDecisionTraceKind.Bridge => trace.Bridge == null ? "bridge vuoto" : $"{trace.Bridge.SelectedIntent} -> {trace.Bridge.CommandName} source={trace.Bridge.TargetSource}",
                _ => "unknown"
            };
        }

        private static MemoryBeliefDecisionColorRole ResolveTraceColor(MemoryBeliefDecisionTrace trace)
        {
            if (trace == null)
                return MemoryBeliefDecisionColorRole.Muted;

            return trace.Kind switch
            {
                MemoryBeliefDecisionTraceKind.Memory => MemoryBeliefDecisionColorRole.Info,
                MemoryBeliefDecisionTraceKind.Belief => trace.Belief != null ? ResolveBeliefOperationColor(trace.Belief.Operation.ToString()) : MemoryBeliefDecisionColorRole.Muted,
                MemoryBeliefDecisionTraceKind.Query => trace.Query != null && trace.Query.IsEmpty ? MemoryBeliefDecisionColorRole.Warning : MemoryBeliefDecisionColorRole.Info,
                MemoryBeliefDecisionTraceKind.Decision => MemoryBeliefDecisionColorRole.Ok,
                MemoryBeliefDecisionTraceKind.Bridge => trace.Bridge != null && trace.Bridge.LegacyFallbackUsed ? MemoryBeliefDecisionColorRole.Warning : MemoryBeliefDecisionColorRole.Ok,
                _ => MemoryBeliefDecisionColorRole.Muted
            };
        }

        private static MemoryBeliefDecisionColorRole ResolveTraceTypeColor(string traceType)
        {
            if (string.IsNullOrWhiteSpace(traceType))
                return MemoryBeliefDecisionColorRole.Muted;

            if (traceType.IndexOf("Danger", StringComparison.OrdinalIgnoreCase) >= 0
                || traceType.IndexOf("Predator", StringComparison.OrdinalIgnoreCase) >= 0
                || traceType.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0)
                return MemoryBeliefDecisionColorRole.Error;

            if (traceType.IndexOf("Object", StringComparison.OrdinalIgnoreCase) >= 0)
                return MemoryBeliefDecisionColorRole.Warning;

            return MemoryBeliefDecisionColorRole.Info;
        }

        private static MemoryBeliefDecisionColorRole ResolveBeliefStatusColor(string status)
        {
            return status switch
            {
                "Active" => MemoryBeliefDecisionColorRole.Ok,
                "Weak" => MemoryBeliefDecisionColorRole.Warning,
                "Conflicted" => MemoryBeliefDecisionColorRole.Error,
                "Discarded" => MemoryBeliefDecisionColorRole.Error,
                "Stale" => MemoryBeliefDecisionColorRole.Muted,
                _ => MemoryBeliefDecisionColorRole.Primary
            };
        }

        private static MemoryBeliefDecisionColorRole ResolveBeliefOperationColor(string operation)
        {
            return operation switch
            {
                "Created" => MemoryBeliefDecisionColorRole.Ok,
                "Merged" => MemoryBeliefDecisionColorRole.Ok,
                "Reinforced" => MemoryBeliefDecisionColorRole.Ok,
                "Weakened" => MemoryBeliefDecisionColorRole.Warning,
                "Stale" => MemoryBeliefDecisionColorRole.Warning,
                "Conflicted" => MemoryBeliefDecisionColorRole.Error,
                "Discarded" => MemoryBeliefDecisionColorRole.Error,
                "RemovedByDecay" => MemoryBeliefDecisionColorRole.Error,
                _ => MemoryBeliefDecisionColorRole.Muted
            };
        }

        private static string FormatCell(Vector2Int cell)
        {
            return $"({cell.x}, {cell.y})";
        }

        private static void ResetMemory(MemoryBeliefDecisionMemoryView view)
        {
            view.HasValue = false;
            view.Tick = 0;
            view.EventType = string.Empty;
            view.TraceType = string.Empty;
            view.SubjectId = 0;
            view.SecondarySubjectId = 0;
            view.SubjectDefId = string.Empty;
            view.Cell = string.Empty;
            view.Intensity01 = 0f;
            view.Reliability01 = 0f;
            view.IsHeard = false;
            view.HeardKind = string.Empty;
            view.SourceSpeakerId = 0;
            view.StoreResult = string.Empty;
        }

        private static void ResetBeliefMutation(MemoryBeliefDecisionBeliefMutationView view)
        {
            view.HasValue = false;
            view.Tick = 0;
            view.Operation = string.Empty;
            view.HasSourceTrace = false;
            view.SourceTraceType = string.Empty;
            view.Reason = string.Empty;
            FillBelief(view.Belief, default);
        }

        private static void ResetQuery(MemoryBeliefDecisionQueryView view)
        {
            view.HasValue = false;
            view.Tick = 0;
            view.GoalType = string.Empty;
            view.Urgency01 = 0f;
            view.NpcCell = string.Empty;
            view.MinConfidence = 0f;
            view.CandidateCount = 0;
            view.UsableCandidateCount = 0;
            view.IsEmpty = false;
            view.EmptyReason = string.Empty;
            view.FinalScore = 0f;
            FillBelief(view.Winner, default);
            view.Contributions.Clear();
        }

        private static void ResetDecision(MemoryBeliefDecisionDecisionView view)
        {
            view.HasValue = false;
            view.Tick = 0;
            view.AuditValid = false;
            view.CandidateCount = 0;
            view.SelectedIntent = string.Empty;
            view.SelectedScore = 0f;
            view.SelectedIndex = 0;
            view.SelectionTopN = 0;
            view.SelectionNoise01 = 0f;
            view.Impulsivity01 = 0f;
            view.EffectiveNoise01 = 0f;
            view.Candidates.Clear();
        }

        private static void ResetBridge(MemoryBeliefDecisionBridgeView view)
        {
            view.HasValue = false;
            view.Tick = 0;
            view.SelectedIntent = string.Empty;
            view.CommandName = string.Empty;
            view.Handled = false;
            view.DidMove = false;
            view.DidSteal = false;
            view.TargetCell = string.Empty;
            view.TargetSource = string.Empty;
            view.LegacyFallbackUsed = false;
            view.Reason = string.Empty;
            view.ColorRole = MemoryBeliefDecisionColorRole.Muted;
        }
    }
}
