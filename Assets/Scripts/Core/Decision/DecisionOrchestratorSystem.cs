using Arcontio.Core.Config;
using Arcontio.Core.Diagnostics;
using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // DecisionOrchestratorSystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Orchestratore decisionale NPC per il primo percorso runtime ordinario
    /// Decisione -> Richiesta di incarico -> Incarico.
    /// </para>
    ///
    /// <para><b>v0.11c.01a - Preparazione senza behavior change</b></para>
    /// <para>
    /// Questo componente sostituisce il primo tratto maturo di <c>NeedsDecisionRule</c>
    /// senza ereditarne i fallback command. In v0.13d copre solo le intenzioni fame
    /// gia' disponibili come incarichi: <c>EatKnownFood</c> e <c>SearchFood</c>.
    /// Le intenzioni prive di route Job vengono osservate e lasciate senza effetti,
    /// invece di ricadere su comandi legacy.
    /// </para>
    ///
    /// <para><b>Boundary architetturali preservati</b></para>
    /// <para>
    /// Il sistema legge <c>World</c> solo come adapter runtime per costruire il
    /// contesto gia' previsto da <c>DecisionContextBuilder</c> e per consegnare il
    /// job al <c>JobRuntimeState</c>. Non emette mai <c>ICommand</c>, non esegue
    /// comandi, non decide preemption e non muta direttamente bisogni, inventario o
    /// posizione. L'arbitraggio resta nel Job Layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Scheduler</b>: gate cognitivo locale, con cadenza per NPC.</item>
    ///   <item><b>Pipeline MBQD</b>: context, candidati, score, selezione e trace decisionale.</item>
    ///   <item><b>Route Job</b>: solo food/search food tramite servizi gia' estratti.</item>
    ///   <item><b>No fallback legacy</b>: nessun command diretto se la route non esiste o fallisce.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionOrchestratorSystem : ISystem
    {
        private readonly NpcDecisionScheduler _scheduler;
        private readonly DecisionContextBuilder _contextBuilder = new();
        private readonly DecisionCandidateGenerator _candidateGenerator = new();
        private readonly DecisionScoringService _scoringService = new();
        private readonly DecisionSelectionService _selectionService = new();
        private readonly IntentExecutionRouter _intentExecutionRouter = new();
        private readonly DecisionExplainabilityBridge _explainabilityBridge = new();
        private readonly FoodDecisionJobOrchestrator _foodDecisionJobOrchestrator = new();
        private readonly List<DecisionCandidate> _decisionCandidates = new(16);
        private readonly Dictionary<int, int> _lastDecisionTicks = new();
        private readonly Random _decisionRandom = new(1505);
        private readonly int _decisionEveryTicks;
        private readonly int _maxSeekRangeCells;
        private readonly bool _enableFoodJobVerticalSlice;
        private readonly JobTemplateRegistry _jobTemplateRegistry;

        public int Period => 1;

        public DecisionOrchestratorSystem()
            : this(
                DecisionRuntimeParams.DefaultDecisionEveryTicks,
                8,
                true,
                null,
                new NpcDecisionScheduler())
        {
        }

        public DecisionOrchestratorSystem(
            int decisionEveryTicks,
            int maxSeekRangeCells,
            bool enableFoodJobVerticalSlice,
            JobTemplateRegistry jobTemplateRegistry)
            : this(
                decisionEveryTicks,
                maxSeekRangeCells,
                enableFoodJobVerticalSlice,
                jobTemplateRegistry,
                new NpcDecisionScheduler())
        {
        }

        public DecisionOrchestratorSystem(NpcDecisionScheduler scheduler)
            : this(
                DecisionRuntimeParams.DefaultDecisionEveryTicks,
                8,
                true,
                null,
                scheduler)
        {
        }

        public DecisionOrchestratorSystem(
            int decisionEveryTicks,
            int maxSeekRangeCells,
            bool enableFoodJobVerticalSlice,
            JobTemplateRegistry jobTemplateRegistry,
            NpcDecisionScheduler scheduler)
        {
            _scheduler = scheduler ?? new NpcDecisionScheduler();
            _decisionEveryTicks = Math.Max(1, decisionEveryTicks);
            _maxSeekRangeCells = Math.Max(1, maxSeekRangeCells);
            _enableFoodJobVerticalSlice = enableFoodJobVerticalSlice;
            _jobTemplateRegistry = jobTemplateRegistry;
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valuta gli NPC eleggibili e prova ad assegnare job solo per le intenzioni
        /// gia' migrate.
        /// </para>
        ///
        /// <para><b>Nessun fallback command</b></para>
        /// <para>
        /// Se la selezione produce un'intenzione senza route job, o se la route
        /// fallisce, il sistema non torna ai comandi storici. Questo e' il punto che
        /// separa il nuovo percorso ordinario dal vecchio <c>NeedsDecisionRule</c>.
        /// </para>
        /// </summary>
        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world == null)
                return;

            int nowTick = (int)tick.Index;
            foreach (var pair in world.NpcDna)
            {
                int npcId = pair.Key;
                if (!world.Needs.TryGetValue(npcId, out var needs))
                    continue;

                if (!CanEvaluateNpc(world, npcId, in needs, nowTick))
                    continue;

                _lastDecisionTicks[npcId] = nowTick;
                TryEvaluateAndStartJob(world, npcId, in needs, nowTick, telemetry);
            }
        }

        private bool CanEvaluateNpc(World world, int npcId, in NpcNeeds needs, int nowTick)
        {
            _lastDecisionTicks.TryGetValue(npcId, out int lastDecisionTick);
            if (!_lastDecisionTicks.ContainsKey(npcId))
                lastDecisionTick = -1;

            bool hasActiveJob = world.JobRuntimeState != null
                && world.JobRuntimeState.GetSnapshot(npcId, nowTick).HasActiveJob;

            bool hasEmergencyIntentSignal = HasCriticalNeedSignal(needs);
            var input = new NpcDecisionSchedulerInput(
                npcId,
                nowTick,
                lastDecisionTick,
                _decisionEveryTicks,
                hasActiveJob,
                hasHigherPriorityIntentSignal: false,
                hasEmergencyIntentSignal: hasEmergencyIntentSignal);

            return _scheduler.EvaluateEligibility(input).AllowsEvaluation;
        }

        private void TryEvaluateAndStartJob(World world, int npcId, in NpcNeeds needs, int nowTick, Telemetry telemetry)
        {
            if (!_contextBuilder.TryBuild(world, npcId, in needs, nowTick, out var context))
                return;

            _candidateGenerator.GeneratePhase1Candidates(in context, _decisionCandidates);
            _scoringService.ScoreCandidates(in context, _decisionCandidates, DecisionScoringConfig.Default());

            var selectionConfig = ResolveDecisionSelectionConfig(world.Config?.Sim?.decision);
            var selection = _selectionService.Select(in context, _decisionCandidates, selectionConfig, _decisionRandom);
            _explainabilityBridge.TryEmitDecisionTrace(
                world.Config?.Sim?.memory_belief_decision_explainability,
                world.MemoryBeliefDecisionExplainability,
                in context,
                auditValid: true,
                _decisionCandidates,
                selection,
                selectionConfig);

            if (selection.IsEmpty)
                return;

            if (selection.Candidate.Kind == DecisionIntentKind.EatKnownFood)
            {
                _foodDecisionJobOrchestrator.TryStartKnownCommunityFoodJob(
                    world,
                    npcId,
                    nowTick,
                    selection.Candidate,
                    _enableFoodJobVerticalSlice,
                    _maxSeekRangeCells,
                    _jobTemplateRegistry,
                    _intentExecutionRouter,
                    _explainabilityBridge,
                    telemetry,
                    out _);
                return;
            }

            if (selection.Candidate.Kind == DecisionIntentKind.SearchFood)
            {
                _foodDecisionJobOrchestrator.TryStartSearchFoodJob(
                    world,
                    npcId,
                    nowTick,
                    selection.Candidate,
                    _enableFoodJobVerticalSlice,
                    _jobTemplateRegistry,
                    _intentExecutionRouter,
                    _explainabilityBridge,
                    telemetry,
                    out _);
            }
        }

        // =============================================================================
        // EvaluateNoOp
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valuta solo l'eleggibilita' cognitiva di un NPC e restituisce un risultato
        /// no-op.
        /// </para>
        ///
        /// <para><b>Nessuna pipeline decisionale ancora cablata</b></para>
        /// <para>
        /// Anche quando lo scheduler consente la rivalutazione, questo metodo non
        /// chiama <c>DecisionCandidateGenerator</c>, <c>DecisionScoringService</c> o
        /// <c>DecisionSelectionService</c>. Non seleziona intenzioni e non propone
        /// job. Il nome esplicito <c>EvaluateNoOp</c> rende difficile scambiarlo per
        /// il runtime finale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Eligibility</b>: delegata allo scheduler cognitivo.</item>
        ///   <item><b>No-op result</b>: risultato passivo, senza side effect.</item>
        ///   <item><b>Punto futuro</b>: area in cui le patch successive potranno inserire context builder e router.</item>
        /// </list>
        /// </summary>
        public DecisionOrchestrationResult EvaluateNoOp(in NpcDecisionSchedulerInput input)
        {
            var eligibility = _scheduler.EvaluateEligibility(input);
            return DecisionOrchestrationResult.NoOp(input.NpcId, input.Tick, in eligibility);
        }

        private static bool HasCriticalNeedSignal(NpcNeeds needs)
        {
            for (int i = 0; i < (int)NeedKind.COUNT; i++)
            {
                var kind = (NeedKind)i;
                if (needs.IsCritical(kind))
                    return true;
            }

            return false;
        }

        private static DecisionSelectionConfig ResolveDecisionSelectionConfig(DecisionRuntimeParams runtimeConfig)
        {
            var config = DecisionSelectionConfig.Default();
            if (runtimeConfig == null)
                return config;

            if (string.Equals(runtimeConfig.selectionMode, "DeterministicTop1", StringComparison.OrdinalIgnoreCase))
            {
                config.topN = 1;
                config.noise01 = 0f;
                config.impulsivityNoiseBonus = 0f;
                config.minimumWeight = runtimeConfig.minimumWeight > 0f ? runtimeConfig.minimumWeight : config.minimumWeight;
                return config;
            }

            config.topN = runtimeConfig.topN > 0 ? runtimeConfig.topN : config.topN;
            config.noise01 = Clamp01(runtimeConfig.noise01);
            config.impulsivityNoiseBonus = Clamp01(runtimeConfig.impulsivityNoiseBonus);
            config.minimumWeight = runtimeConfig.minimumWeight > 0f ? runtimeConfig.minimumWeight : config.minimumWeight;
            return config;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
