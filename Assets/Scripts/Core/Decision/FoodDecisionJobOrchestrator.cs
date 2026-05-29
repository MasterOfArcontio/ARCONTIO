using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // FoodDecisionJobOrchestrator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Servizio transitorio dedicato al tratto Fame/SearchFood/EatKnownFood verso
    /// <c>JobRequest</c> e <c>Job</c>.
    /// </para>
    ///
    /// <para><b>v0.13b - Estrazione servizi utili da NeedsDecisionRule</b></para>
    /// <para>
    /// Questo componente sposta fuori da <c>NeedsDecisionRule</c> il primo ramo gia'
    /// maturo del ponte MBQD -> Job. Non diventa un nuovo sistema decisionale, non
    /// emette <c>ICommand</c>, non modifica il <c>World</c> e non sostituisce ancora
    /// l'orchestratore generale: prende un candidato decisionale gia' selezionato,
    /// costruisce la richiesta di incarico tramite il router esistente, materializza
    /// il job con le factory gia' presenti e chiede a <c>JobRuntimeState</c> di
    /// assegnarlo.
    /// </para>
    ///
    /// <para><b>Estrazione behavior-preserving</b></para>
    /// <para>
    /// Le policy operative restano identiche al ramo precedente: stesso gate, stessa
    /// probe locale per <c>SearchFood</c>, stessa risoluzione community food
    /// visibile/ricordato, stesse ragioni di fallimento e stessi contatori. Se la
    /// route Job non viene accettata, il chiamante legacy puo' continuare con i
    /// fallback gia' esistenti fino allo scollegamento previsto in v0.13c.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SearchFood</b>: seleziona una probe cell locale e crea un job di esplorazione.</item>
    ///   <item><b>EatKnownFood</b>: risolve uno stock community visibile o ricordato e crea un job food.</item>
    ///   <item><b>Legacy fallback hook</b>: conserva il gate job anche quando la rule arriva dal ramo fame storico.</item>
    ///   <item><b>Explainability</b>: emette la stessa trace <c>JobRequest</c> del ponte precedente.</item>
    /// </list>
    /// </summary>
    public sealed class FoodDecisionJobOrchestrator
    {
        private static readonly Vector2Int[] SearchFoodProbeOffsets =
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(2, 0),
            new Vector2Int(0, 2),
            new Vector2Int(-2, 0),
            new Vector2Int(0, -2),
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
        };

        // =============================================================================
        // TryStartSearchFoodJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova ad avviare il job <c>SearchFood</c> senza produrre command legacy.
        /// </para>
        /// </summary>
        public bool TryStartSearchFoodJob(
            World world,
            int npcId,
            int nowTick,
            DecisionCandidate candidate,
            bool gateEnabled,
            JobTemplateRegistry jobTemplateRegistry,
            IntentExecutionRouter intentExecutionRouter,
            DecisionExplainabilityBridge explainabilityBridge,
            Telemetry telemetry,
            out string reason)
        {
            reason = string.Empty;

            LogSearchFoodJobRoute(nowTick, npcId, "EnterSearchFoodRoute", "EnterSearchFoodRoute", gateEnabled);

            if (!gateEnabled)
            {
                reason = "GateDisabled";
                LogSearchFoodJobRoute(nowTick, npcId, "GateDisabled", reason, gateEnabled);
                return false;
            }

            if (world?.JobRuntimeState == null)
            {
                reason = "JobRuntimeMissing";
                LogSearchFoodJobRoute(nowTick, npcId, "JobRuntimeMissing", reason, gateEnabled);
                return false;
            }

            if (!TryResolveSearchFoodProbeCell(world, npcId, out var probeCell, out reason))
            {
                LogSearchFoodJobRoute(nowTick, npcId, "ProbeUnavailable", reason, gateEnabled);
                return false;
            }

            LogSearchFoodJobRoute(
                nowTick,
                npcId,
                "ProbeResolved",
                reason,
                gateEnabled,
                probeFound: true,
                probeCell: probeCell);

            if (intentExecutionRouter == null)
            {
                reason = "IntentExecutionRouterMissing";
                LogSearchFoodJobRoute(
                    nowTick,
                    npcId,
                    "RequestBuildFailed",
                    "SearchFoodRequestBuildFailed:" + reason,
                    gateEnabled,
                    probeFound: true,
                    probeCell: probeCell);
                return false;
            }

            if (!intentExecutionRouter.TryRouteSearchFood(nowTick, npcId, candidate, probeCell, out var route))
            {
                reason = route.Reason;
                LogSearchFoodJobRoute(
                    nowTick,
                    npcId,
                    "RequestBuildFailed",
                    "SearchFoodRequestBuildFailed:" + reason,
                    gateEnabled,
                    probeFound: true,
                    probeCell: probeCell);
                return false;
            }

            var request = route.Request;
            LogSearchFoodJobRoute(
                nowTick,
                npcId,
                "RequestBuilt",
                "SearchFoodRequestBuilt",
                gateEnabled,
                probeFound: true,
                probeCell: probeCell,
                requestBuilt: true);

            bool created = SearchFoodJobFactory.TryCreateSearchFoodLocalProbeJob(
                jobTemplateRegistry,
                request,
                out var job,
                out reason);

            if (!created)
            {
                LogSearchFoodJobRoute(
                    nowTick,
                    npcId,
                    "FactoryFailed",
                    "SearchFoodFactoryFailed:" + reason,
                    gateEnabled,
                    probeFound: true,
                    probeCell: probeCell,
                    requestBuilt: true);
                return false;
            }

            LogSearchFoodJobRoute(
                nowTick,
                npcId,
                "FactoryCreated",
                "SearchFoodFactoryCreated",
                gateEnabled,
                probeFound: true,
                probeCell: probeCell,
                requestBuilt: true,
                factoryCreated: true,
                jobId: job.JobId);

            explainabilityBridge?.TryEmitJobRequestTrace(
                world.Config?.Sim?.memory_belief_decision_explainability,
                world.MemoryBeliefDecisionExplainability,
                nowTick,
                npcId,
                request,
                job.JobId,
                legacyBridgeStillUsed: false);

            bool assigned = world.JobRuntimeState.TryAssignJob(npcId, job, nowTick, out reason);
            LogSearchFoodJobRoute(
                nowTick,
                npcId,
                assigned ? "AssignmentAccepted" : "AssignmentRejected",
                assigned ? "SearchFoodJobRouteAccepted:" + reason : "SearchFoodAssignmentRejected:" + reason,
                gateEnabled,
                probeFound: true,
                probeCell: probeCell,
                requestBuilt: true,
                factoryCreated: true,
                jobId: job.JobId,
                assigned: assigned,
                assignReason: reason);

            telemetry?.Counter(assigned ? "SearchFoodJobVerticalSlice.Assigned" : "SearchFoodJobVerticalSlice.AssignFailed", 1);
            return assigned;
        }

        // =============================================================================
        // TryStartKnownCommunityFoodJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova ad avviare il job <c>EatKnownFood</c> per uno stock community gia'
        /// visibile o ricordato.
        /// </para>
        /// </summary>
        public bool TryStartKnownCommunityFoodJob(
            World world,
            int npcId,
            int nowTick,
            DecisionCandidate candidate,
            bool gateEnabled,
            int maxSeekRangeCells,
            JobTemplateRegistry jobTemplateRegistry,
            IntentExecutionRouter intentExecutionRouter,
            DecisionExplainabilityBridge explainabilityBridge,
            Telemetry telemetry,
            out string reason)
        {
            reason = string.Empty;

            if (!gateEnabled)
            {
                reason = "GateDisabled";
                return false;
            }

            if (world?.JobRuntimeState == null)
            {
                reason = "JobRuntimeMissing";
                return false;
            }

            if (!TryResolveKnownCommunityFoodTarget(world, npcId, maxSeekRangeCells, out int foodObjectId, out int targetX, out int targetY, out string targetSource))
            {
                reason = "KnownCommunityFoodMissing";
                return false;
            }

            if (intentExecutionRouter == null)
            {
                reason = "IntentExecutionRouterMissing";
                return false;
            }

            if (!intentExecutionRouter.TryRouteEatKnownFood(nowTick, npcId, candidate, foodObjectId, out var route))
            {
                reason = route.Reason;
                return false;
            }

            var request = route.Request;
            if (request.TargetCell.x != targetX || request.TargetCell.y != targetY)
            {
                reason = "ResolvedTargetMismatch:" + targetSource;
                return false;
            }

            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                jobTemplateRegistry,
                request,
                out var job,
                out reason);

            if (!created)
                return false;

            explainabilityBridge?.TryEmitJobRequestTrace(
                world.Config?.Sim?.memory_belief_decision_explainability,
                world.MemoryBeliefDecisionExplainability,
                nowTick,
                npcId,
                request,
                job.JobId,
                legacyBridgeStillUsed: false);

            bool assigned = world.JobRuntimeState.TryAssignJob(npcId, job, nowTick, out reason);
            telemetry?.Counter(assigned ? "FoodJobVerticalSlice.Assigned" : "FoodJobVerticalSlice.AssignFailed", 1);
            return assigned;
        }

        // =============================================================================
        // TryStartKnownCommunityFoodJobFromLegacyFallback
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica lo stesso gate food-job al fallback storico fame -> command.
        /// </para>
        /// </summary>
        public bool TryStartKnownCommunityFoodJobFromLegacyFallback(
            World world,
            int npcId,
            int nowTick,
            in NpcNeeds needs,
            bool gateEnabled,
            int maxSeekRangeCells,
            JobTemplateRegistry jobTemplateRegistry,
            Telemetry telemetry,
            out string reason)
        {
            reason = string.Empty;

            if (!gateEnabled)
            {
                reason = "GateDisabled";
                return false;
            }

            if (world?.JobRuntimeState == null)
            {
                reason = "JobRuntimeMissing";
                return false;
            }

            if (!TryResolveKnownCommunityFoodTarget(world, npcId, maxSeekRangeCells, out int foodObjectId, out int targetX, out int targetY, out string targetSource))
            {
                reason = "KnownCommunityFoodMissing";
                return false;
            }

            float urgency01 = needs.GetValue(NeedKind.Hunger);
            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                jobTemplateRegistry,
                npcId,
                foodObjectId,
                new Vector2Int(targetX, targetY),
                nowTick,
                urgency01,
                targetSource,
                out var job,
                out reason);

            if (!created)
                return false;

            bool assigned = world.JobRuntimeState.TryAssignJob(npcId, job, nowTick, out reason);
            telemetry?.Counter(assigned ? "FoodJobVerticalSlice.LegacyFallbackAssigned" : "FoodJobVerticalSlice.LegacyFallbackAssignFailed", 1);
            return assigned;
        }

        private static bool TryResolveKnownCommunityFoodTarget(
            World world,
            int npcId,
            int maxSeekRangeCells,
            out int foodObjectId,
            out int targetX,
            out int targetY,
            out string targetSource)
        {
            foodObjectId = 0;
            targetX = 0;
            targetY = 0;
            targetSource = string.Empty;

            foodObjectId = FindVisibleCommunityFoodStock(world, npcId, maxSeekRangeCells);
            if (foodObjectId != 0 && TryGetObjectCell(world, foodObjectId, out targetX, out targetY))
            {
                targetSource = "VisibleCommunityFood";
                return true;
            }

            foodObjectId = FindRememberedCommunityFoodStock(world, npcId, maxSeekRangeCells, out targetX, out targetY);
            if (foodObjectId != 0)
            {
                targetSource = "RememberedCommunityFood";
                return true;
            }

            return false;
        }

        private static bool TryResolveSearchFoodProbeCell(World world, int npcId, out Vector2Int probeCell, out string reason)
        {
            probeCell = default;
            reason = string.Empty;

            if (world == null || !world.GridPos.TryGetValue(npcId, out var position))
            {
                reason = "NpcPositionMissing";
                return false;
            }

            var origin = new Vector2Int(position.X, position.Y);
            for (int i = 0; i < SearchFoodProbeOffsets.Length; i++)
            {
                var candidate = origin + SearchFoodProbeOffsets[i];
                if (!IsValidSearchFoodProbeCell(world, npcId, candidate))
                    continue;

                probeCell = candidate;
                reason = "SearchFoodProbeResolved";
                return true;
            }

            reason = "SearchFoodProbeUnavailable";
            return false;
        }

        private static bool IsValidSearchFoodProbeCell(World world, int npcId, Vector2Int cell)
        {
            if (!world.InBounds(cell.x, cell.y))
                return false;

            if (world.IsMovementBlocked(cell.x, cell.y))
                return false;

            if (world.GetObjectAt(cell.x, cell.y) >= 0)
                return false;

            foreach (var kv in world.GridPos)
            {
                if (kv.Key == npcId)
                    continue;

                if (kv.Value.X == cell.x && kv.Value.Y == cell.y)
                    return false;
            }

            return true;
        }

        private static int FindRememberedCommunityFoodStock(
            World world,
            int npcId,
            int maxRangeCells,
            out int sx,
            out int sy)
        {
            sx = 0;
            sy = 0;

            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            if (!world.NpcObjectMemory.TryGetValue(npcId, out var mem) || mem == null)
                return 0;

            int bestObjId = 0;
            int bestDist = int.MaxValue;
            int bestX = 0;
            int bestY = 0;

            for (int i = 0; i < mem.Slots.Length; i++)
            {
                var e = mem.Slots[i];
                if (!e.IsValid)
                    continue;

                if (e.Kind != NpcObjectMemoryStore.SubjectKind.WorldObject)
                    continue;

                int objId = e.SubjectId != 0 ? e.SubjectId : e.ObjectId;
                if (objId == 0)
                    continue;

                if (e.OwnerKind != OwnerKind.Community || e.OwnerId != 0)
                    continue;

                int ox = e.CellX;
                int oy = e.CellY;

                if (world.FoodStocks.TryGetValue(objId, out var st))
                {
                    if (st.Units <= 0)
                        continue;

                    if (world.Objects.TryGetValue(objId, out var inst) && inst != null)
                    {
                        ox = inst.CellX;
                        oy = inst.CellY;
                    }
                }

                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                if (manhattan < bestDist)
                {
                    bestDist = manhattan;
                    bestObjId = objId;
                    bestX = ox;
                    bestY = oy;
                }
            }

            if (bestObjId != 0)
            {
                sx = bestX;
                sy = bestY;
            }

            return bestObjId;
        }

        private static int FindVisibleCommunityFoodStock(World world, int npcId, int maxRangeCells)
        {
            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            foreach (var kv in world.FoodStocks)
            {
                int objId = kv.Key;
                var st = kv.Value;

                if (st.Units <= 0)
                    continue;

                if (st.OwnerKind != OwnerKind.Community || st.OwnerId != 0)
                    continue;

                if (!TryGetObjectCell(world, objId, out int ox, out int oy))
                    continue;

                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                if (!world.HasLineOfSight(nx, ny, ox, oy))
                    continue;

                return objId;
            }

            return 0;
        }

        private static bool TryGetNpcCell(World world, int npcId, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (world == null || !world.GridPos.TryGetValue(npcId, out var pos))
                return false;

            x = pos.X;
            y = pos.Y;
            return true;
        }

        private static bool TryGetObjectCell(World world, int objectId, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (world == null || !world.Objects.TryGetValue(objectId, out var inst) || inst == null)
                return false;

            x = inst.CellX;
            y = inst.CellY;
            return true;
        }

        private static void LogSearchFoodJobRoute(
            int tick,
            int npcId,
            string phase,
            string reason,
            bool gateEnabled,
            bool probeFound = false,
            Vector2Int probeCell = default,
            bool requestBuilt = false,
            bool factoryCreated = false,
            string jobId = "",
            bool assigned = false,
            string assignReason = "")
        {
            ArcontioLogger.Debug(
                new LogContext(tick: tick, channel: "DecisionBridge"),
                new LogBlock(LogLevel.Debug, "log.decision.search_food_job_route")
                    .AddField("tick", tick)
                    .AddField("npcId", npcId)
                    .AddField("intent", DecisionIntentKind.SearchFood.ToString())
                    .AddField("phase", phase)
                    .AddField("reason", reason ?? string.Empty)
                    .AddField("gateEnabled", gateEnabled)
                    .AddField("probeFound", probeFound)
                    .AddField("probeX", probeCell.x)
                    .AddField("probeY", probeCell.y)
                    .AddField("requestBuilt", requestBuilt)
                    .AddField("factoryCreated", factoryCreated)
                    .AddField("jobId", jobId ?? string.Empty)
                    .AddField("assigned", assigned)
                    .AddField("assignReason", assignReason ?? string.Empty));
        }
    }
}
