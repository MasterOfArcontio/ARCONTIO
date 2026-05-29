using Arcontio.Core.Diagnostics;
using Arcontio.Core.Config;
using Arcontio.Core.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // NeedsDecisionRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Compatibility shim runtime per il vecchio ciclo decisionale basato sui bisogni.
    /// </para>
    ///
    /// <para><b>v0.11c.01e - Legacy Transitional Decision Bridge</b></para>
    /// <para>
    /// Questa classe non rappresenta il modello architetturale target del Decision
    /// Layer. Resta viva per compatibilita' con il runtime osservabile v0.11B/v0.11C:
    /// riceve il tick, attraversa il primo MBQD reale quando possibile, e mantiene
    /// ancora i fallback storici verso <c>ICommand</c> finche' il futuro
    /// <c>DecisionOrchestratorSystem</c> non diventera' il punto primario.
    /// </para>
    ///
    /// <para><b>Boundary temporaneo dichiarato</b></para>
    /// <para>
    /// Le responsabilita' gia' estratte non devono rientrare qui: costruzione del
    /// contesto, routing intent-to-JobRequest ed emissione explainability vivono nei
    /// componenti dedicati. Le scansioni su <c>World</c>, i fallback food/rest e i
    /// command legacy rimasti in questa classe sono debito transitorio esplicito, non
    /// pattern da copiare nel nuovo orchestrator.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cadence legacy</b>: usa ancora <c>TickPulseEvent</c> e throttle locale.</item>
    ///   <item><b>MBQD callsite</b>: invoca generator/scoring/selection e route estratte.</item>
    ///   <item><b>Job bridge</b>: tenta Food/SearchFood job vertical slice senza preemption diretta.</item>
    ///   <item><b>Fallback legacy</b>: conserva command storici per compatibilita' runtime.</item>
    ///   <item><b>Feedback food</b>: mantiene il feedback cognitivo minimo dei failure food.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// NeedsDecisionRule (Day9):
    /// Rule alto livello, coerente col tuo stile:
    /// - Reagisce a eventi (TickPulseEvent) e produce Commands.
    ///
    /// Decisione v0:
    /// - se hungry: privato -> stock community (VISIBILE) -> furto (se moralità/emergenza)
    /// - se tired: letto community libero (VISIBILE) -> letto altrui (se moralità/emergenza)
    ///
    /// IMPORTANTISSIMO (patch):
    /// In ARCONTIO la visibilità non è "telepatia".
    /// Se un oggetto è dietro un muro, la decisione *non deve* poterlo usare come se fosse noto.
    ///
    /// Per questo motivo qui applichiamo un filtro "Visible" minimale:
    /// - posizione NPC: world.GridPos[npcId]
    /// - posizione oggetto: world.Objects[objId].CellX/CellY
    /// - visibilità: world.HasLineOfSight(nx,ny,ox,oy) + range discreto
    ///
    /// Nota pragmatica:
    /// - NON applichiamo il cono FOV (orientamento) in questa rule,
    ///   perché per una decisione "mangio" spesso ti interessa la *conoscenza pratica* dell'oggetto
    ///   (es. lo hai visto un secondo fa, ti giri, ecc.).
    /// - Se in futuro vuoi coerenza totale col pipeline Range?Cone?LOS, il posto giusto
    ///   è far sì che NeedsDecisionRule consulti Memory/ObjectPerception, non il World "nudo".
    /// </remarks>
    public sealed class NeedsDecisionRule : IRule
    {
        private readonly BeliefUpdater _beliefUpdater = new();
        private readonly BeliefQueryService _beliefQueryService = new();

        // Componenti gia' estratti dalla rule durante v0.11c.01. Restano iniettati
        // come callsite interni per preservare il comportamento runtime, ma indicano
        // il confine target: il futuro orchestrator dovra' comporli senza assorbire
        // fallback legacy, scansioni World o command adapter storici.
        private readonly DecisionCandidateGenerator _decisionCandidateGenerator = new();
        private readonly DecisionScoringService _decisionScoringService = new();
        private readonly DecisionSelectionService _decisionSelectionService = new();
        private readonly DecisionContextBuilder _decisionContextBuilder = new();
        private readonly IntentExecutionRouter _intentExecutionRouter = new();
        private readonly DecisionExplainabilityBridge _decisionExplainabilityBridge = new();
        private readonly FoodDecisionJobOrchestrator _foodDecisionJobOrchestrator = new();
        private readonly List<DecisionCandidate> _decisionCandidates = new(16);
        private readonly System.Random _decisionRandom = new(1505);

        // Throttle legacy: decide ogni N tick-pulse per contenere il rumore del
        // vecchio bridge. Non e' ancora la cognitive cadence target descritta da
        // ARC-DEC-018/019 e non deve diventare NpcDecisionScheduler.
        private readonly int _decisionEveryTicks;

        // Range di ricerca "decisionale" per cibo/letto.
        // È volutamente conservativo: evita che un NPC "usi" risorse che stanno a metà mappa
        // solo perché la LOS non è bloccata (corridoio lungo, ecc.).
        private readonly int _maxSeekRangeCells;
        private readonly bool _enableFoodJobVerticalSlice;
        private readonly JobTemplateRegistry _jobTemplateRegistry;

        public NeedsDecisionRule(
            int decisionEveryTicks = 10,
            int maxSeekRangeCells = 8,
            bool enableFoodJobVerticalSlice = false,
            JobTemplateRegistry jobTemplateRegistry = null)
        {
            _decisionEveryTicks = Mathf.Max(1, decisionEveryTicks);
            _maxSeekRangeCells = Mathf.Max(1, maxSeekRangeCells);
            _enableFoodJobVerticalSlice = enableFoodJobVerticalSlice;
            _jobTemplateRegistry = jobTemplateRegistry;
        }

        public void Handle(World world, ISimEvent e, List<ICommand> outCommands, Telemetry telemetry)
        {
            // Entry point legacy del bridge: il futuro DecisionOrchestratorSystem non
            // deve essere cablato qui in modo implicito. Fino alla migrazione
            // dedicata, questa rule resta l'adapter compatibile che puo' ancora
            // produrre ICommand tramite fallback storici.
            // Usiamo TickPulseEvent come clock decisionale.
            if (e is not TickPulseEvent pulse)
                return;

            if ((pulse.TickIndex % _decisionEveryTicks) != 0)
                return;

            var cfg = world.Global.Needs;

            int ate = 0, slept = 0, antisocial = 0, moved = 0;

            foreach (var npcId in world.NpcDna.Keys)
            {
                if (!world.Needs.TryGetValue(npcId, out var needs)) continue;

                if (TryPlanFromDecisionLayer(
                    world,
                    npcId,
                    in needs,
                    (int)pulse.TickIndex,
                    telemetry,
                    out var decisionCmd,
                    out bool decisionDidSteal,
                    out bool decisionDidMove,
                    out bool decisionHandled))
                {
                    if (decisionCmd != null)
                    {
                        outCommands.Add(decisionCmd);
                        if (decisionDidMove) moved++; else if (decisionCmd is SleepInBedCommand) slept++; else ate++;
                        if (decisionDidSteal) antisocial++;

                        ArcontioLogger.Info(
                            new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "DecisionBridge"),
                            new LogBlock(LogLevel.Info, "log.decision.bridge.command")
                                .AddField("tick", pulse.TickIndex)
                                .AddField("npcId", npcId)
                                .AddField("Command", decisionCmd.Name));
                    }

                    if (decisionHandled)
                        continue;
                }

                // --- MANGIA ---
                if (needs.GetValue(NeedKind.Hunger) >= cfg.hungryThreshold)
                {
                    // v0.11.01: il Decision Layer puo' non selezionare EatKnownFood
                    // in tutti gli scenari legacy minimali, ma il gate opt-in deve
                    // comunque proteggere il caso supportato "community known stock"
                    // dal vecchio bypass NeedsDecisionRule -> ICommand. Se la route
                    // job accetta, non emettiamo command legacy nello stesso tick.
                    if (_foodDecisionJobOrchestrator.TryStartKnownCommunityFoodJobFromLegacyFallback(
                        world,
                        npcId,
                        (int)pulse.TickIndex,
                        in needs,
                        _enableFoodJobVerticalSlice,
                        _maxSeekRangeCells,
                        _jobTemplateRegistry,
                        telemetry,
                        out _))
                    {
                        continue;
                    }

                    if (TryPlanEatOrMove(world, npcId, in needs, (int)pulse.TickIndex, telemetry, out var cmd, out bool didSteal, out bool didMove))
                    {
                        outCommands.Add(cmd);
                        if (didMove) moved++; else ate++;
                        if (didSteal) antisocial++;

                        ArcontioLogger.Info(
                            new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsDecisionRule"),
                            new LogBlock(LogLevel.Info, "log.needsconfig.Handle")
                                         .AddField("tick", pulse.TickIndex)
                                         .AddField("npcId", npcId)
                                         .AddField("Command", cmd.Name));


                        continue; // una sola azione per tick
                    }
                }

                // --- BEVI ---
                // v0.04.08: decay Thirst attivo. Il DrinkCommand e i WorldObject sorgente d'acqua
                // non sono ancora implementati — il blocco rileva la soglia e logga, ma non emette
                // comandi. Quando i water source object saranno introdotti, sostituire il log
                // con TryPlanDrink (analoga a TryPlanEatOrMove).
                if (needs.GetValue(NeedKind.Thirst) >= cfg.thirstyThreshold)
                {
                    ArcontioLogger.Info(
                        new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsDecisionRule"),
                        new LogBlock(LogLevel.Info, "log.needs.thirst_alert")
                            .AddField("tick",    pulse.TickIndex)
                            .AddField("npcId",   npcId)
                            .AddField("thirst",  needs.GetValue(NeedKind.Thirst).ToString("0.00"))
                            .AddField("status",  "DrinkCommand_pending_water_objects"));
                    // TODO v0.04.xx: aggiungere TryPlanDrink quando water source WorldObject esiste
                    continue;
                }

                // --- DORMI ---
                if (needs.GetValue(NeedKind.Rest) >= cfg.tiredThreshold)
                {
                    if (TryPlanSleep(world, npcId, needs, out var cmd, out bool didTrespass))
                    {
                        outCommands.Add(cmd);
                        slept++;
                        if (didTrespass) antisocial++;

                        ArcontioLogger.Info(
                            new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsDecisionRule"),
                            new LogBlock(LogLevel.Info, "log.needsconfig.Handle")
                                         .AddField("tick", pulse.TickIndex)
                                         .AddField("npcId", npcId)
                                         .AddField("Command", cmd.Name));

                        continue; // una sola azione per tick
                    }
                }
            }

            if (ate + slept + antisocial > 0)
            {
                ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsDecisionRule"),
                new LogBlock(LogLevel.Info, "log.needsconfig.Handle")
                    .AddField("tick=", pulse.TickIndex)
                    .AddField("ate==", ate)
                    .AddField("moved==", moved)
                    .AddField("antisocial==", antisocial));
            }
        }

        // =============================================================================
        // TryPlanFromDecisionLayer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ponte provvisorio tra il nuovo Decision Layer v0.05 e la rule legacy dei
        /// bisogni.
        /// </para>
        ///
        /// <para><b>Compatibility shim, non modello target</b></para>
        /// <para>
        /// Il metodo usa i componenti MBQD gia' estratti per scegliere un'intenzione,
        /// poi resta nel vecchio bridge per preservare route Job vertical slice e
        /// fallback command legacy. La presenza di <c>ICommand</c> in questa firma e'
        /// debito compatibile: non deve essere copiata nel futuro orchestrator e non
        /// assegna autorita' di preemption al Decision Layer.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Gate</b>: attiva il ponte solo per fame/riposo, non per sete ancora priva di command.</item>
        ///   <item><b>Context</b>: costruisce un <c>DecisionEvaluationContext</c> con input per-NPC e belief soggettivi.</item>
        ///   <item><b>Pipeline</b>: genera candidati, calcola score e seleziona weighted random.</item>
        ///   <item><b>Adapter</b>: delega Food/SearchFood ai job route estratti dove possibile.</item>
        ///   <item><b>Fallback</b>: conserva command legacy e no-op transitori senza cambiare policy.</item>
        /// </list>
        /// </summary>
        private bool TryPlanFromDecisionLayer(
            World world,
            int npcId,
            in NpcNeeds needs,
            int nowTick,
            Telemetry telemetry,
            out ICommand cmd,
            out bool didSteal,
            out bool didMove,
            out bool handled)
        {
            cmd = null;
            didSteal = false;
            didMove = false;
            handled = false;

            if (world == null)
                return false;

            bool hungerAlert = needs.IsAlert(NeedKind.Hunger) || needs.GetValue(NeedKind.Hunger) >= world.Global.Needs.hungryThreshold;
            bool restAlert = needs.IsAlert(NeedKind.Rest) || needs.GetValue(NeedKind.Rest) >= world.Global.Needs.tiredThreshold;
            if (!hungerAlert && !restAlert)
                return false;

            var explainabilityConfig = world.Config?.Sim?.memory_belief_decision_explainability;

            if (!_decisionContextBuilder.TryBuild(world, npcId, in needs, nowTick, out var context))
                return false;

            var audit = DecisionInputAudit.Audit(context);
            if (!audit.IsValid)
            {
                telemetry?.Counter("DecisionBridge.AuditInvalid", 1);
                return false;
            }

            // Il cibo portato addosso e' uno stato locale dell'NPC, non una query sul
            // mondo: lo manteniamo come shortcut operativo finche' il catalogo non
            // avra' una intenzione dedicata EatCarriedFood.
            if (hungerAlert && world.NpcPrivateFood.TryGetValue(npcId, out int privateFood) && privateFood > 0)
            {
                cmd = new EatPrivateFoodCommand(npcId);
                handled = true;
                _decisionExplainabilityBridge.TryLogDecisionBridgeFallback(
                    nowTick,
                    npcId,
                    DecisionIntentKind.EatKnownFood,
                    cmd,
                    LegacyFallbackKind.CompatibilityFallback,
                    "PrivateCarriedFoodLegacyCommand");
                return true;
            }

            _decisionCandidateGenerator.GeneratePhase1Candidates(context, _decisionCandidates);
            var scoringConfig = DecisionScoringConfig.Default();
            var selectionConfig = ResolveDecisionSelectionConfig(world.Config?.Sim?.decision);
            _decisionScoringService.ScoreCandidates(context, _decisionCandidates, scoringConfig);

            var selection = _decisionSelectionService.Select(
                context,
                _decisionCandidates,
                selectionConfig,
                _decisionRandom);

            if (selection.IsEmpty)
                return false;

            handled = true;
            telemetry?.Counter("DecisionBridge.IntentSelected", 1);
            _decisionExplainabilityBridge.TryEmitDecisionTrace(explainabilityConfig, world.MemoryBeliefDecisionExplainability, context, true, _decisionCandidates, selection, selectionConfig);

            ArcontioLogger.Info(
                new LogContext(tick: nowTick, channel: "DecisionBridge", npcId: npcId),
                new LogBlock(LogLevel.Info, "log.decision.bridge.intent")
                    .AddField("intent", selection.Candidate.Kind.ToString())
                    .AddField("score", selection.Candidate.FinalScore.ToString("0.000"))
                    .AddField("candidateCount", _decisionCandidates.Count));

            switch (selection.Candidate.Kind)
            {
                case DecisionIntentKind.EatKnownFood:
                {
                    if (_foodDecisionJobOrchestrator.TryStartKnownCommunityFoodJob(
                        world,
                        npcId,
                        nowTick,
                        selection.Candidate,
                        _enableFoodJobVerticalSlice,
                        _maxSeekRangeCells,
                        _jobTemplateRegistry,
                        _intentExecutionRouter,
                        _decisionExplainabilityBridge,
                        telemetry,
                        out string jobRouteReason))
                    {
                        cmd = null;
                        didSteal = false;
                        didMove = false;
                        _decisionExplainabilityBridge.TryEmitBridgeTrace(explainabilityConfig, world.MemoryBeliefDecisionExplainability, nowTick, npcId, selection.Candidate, cmd, didSteal, didMove, true, false, LegacyFallbackKind.None, "FoodJobRouteAccepted:" + jobRouteReason);
                        return true;
                    }

                    bool planned = TryPlanEatOrMove(world, npcId, in needs, nowTick, telemetry, out cmd, out didSteal, out didMove);
                    var fallbackKind = ResolveFoodJobRouteFallbackKind(jobRouteReason, planned);
                    string fallbackReason = BuildFoodJobRouteFallbackReason(jobRouteReason, planned);
                    ApplyFoodRouteFailureCognitiveFeedback(world, npcId, nowTick, selection.Candidate, jobRouteReason, explainabilityConfig, telemetry);
                    _decisionExplainabilityBridge.TryEmitBridgeTrace(explainabilityConfig, world.MemoryBeliefDecisionExplainability, nowTick, npcId, selection.Candidate, cmd, didSteal, didMove, planned, true, fallbackKind, fallbackReason);
                    return planned;
                }

                case DecisionIntentKind.TakeRestrictedFood:
                {
                    bool planned = TryPlanEatOrMove(world, npcId, in needs, nowTick, telemetry, out cmd, out didSteal, out didMove);
                    _decisionExplainabilityBridge.TryEmitBridgeTrace(explainabilityConfig, world.MemoryBeliefDecisionExplainability, nowTick, npcId, selection.Candidate, cmd, didSteal, didMove, planned, true, LegacyFallbackKind.TransitionalDebtFallback, planned ? "RestrictedFoodLegacyCommandAdapter" : "RestrictedFoodLegacyAdapterNoCommand");
                    return planned;
                }

                case DecisionIntentKind.RestKnownPlace:
                case DecisionIntentKind.UseRestrictedRestPlace:
                {
                    bool planned = TryPlanSleep(world, npcId, needs, out cmd, out didSteal);
                    _decisionExplainabilityBridge.TryEmitBridgeTrace(explainabilityConfig, world.MemoryBeliefDecisionExplainability, nowTick, npcId, selection.Candidate, cmd, didSteal, didMove, planned, true, LegacyFallbackKind.TransitionalDebtFallback, planned ? "RestLegacyCommandAdapter" : "RestLegacyAdapterNoCommand");
                    return planned;
                }

                case DecisionIntentKind.SearchFood:
                {
                    if (_foodDecisionJobOrchestrator.TryStartSearchFoodJob(
                        world,
                        npcId,
                        nowTick,
                        selection.Candidate,
                        _enableFoodJobVerticalSlice,
                        _maxSeekRangeCells,
                        _jobTemplateRegistry,
                        _intentExecutionRouter,
                        _decisionExplainabilityBridge,
                        telemetry,
                        out string searchJobRouteReason))
                    {
                        cmd = null;
                        didSteal = false;
                        didMove = false;
                        _decisionExplainabilityBridge.TryEmitBridgeTrace(explainabilityConfig, world.MemoryBeliefDecisionExplainability, nowTick, npcId, selection.Candidate, cmd, didSteal, didMove, true, false, LegacyFallbackKind.None, "SearchFoodJobRouteAccepted:" + searchJobRouteReason);
                        return true;
                    }

                    cmd = null;
                    didSteal = false;
                    didMove = false;
                    handled = false;
                    _decisionExplainabilityBridge.TryEmitBridgeTrace(
                        explainabilityConfig,
                        world.MemoryBeliefDecisionExplainability,
                        nowTick,
                        npcId,
                        selection.Candidate,
                        cmd,
                        didSteal,
                        didMove,
                        false,
                        true,
                        ResolveSearchFoodJobRouteFallbackKind(searchJobRouteReason),
                        "NonExecutableIntentFallback:SearchFoodJobRouteRejected:" + searchJobRouteReason);
                    return false;
                }

                case DecisionIntentKind.SearchRestPlace:
                case DecisionIntentKind.WaitAndObserve:
                    // Ricerca/attesa non hanno ancora un Job/Step dedicato. Il ponte
                    // registra comunque l'intenzione scelta dal Decision Layer, ma
                    // lascia proseguire la logica legacy: cosi' evitiamo che un
                    // intento non eseguibile trasformi fame/riposo in un no-op.
                    cmd = null;
                    didSteal = false;
                    didMove = false;
                    handled = false;
                    _decisionExplainabilityBridge.TryEmitBridgeTrace(explainabilityConfig, world.MemoryBeliefDecisionExplainability, nowTick, npcId, selection.Candidate, cmd, didSteal, didMove, false, true, LegacyFallbackKind.NonExecutableIntentFallback, "NonExecutableIntentLegacyFallback");
                    return false;

                default:
                    handled = false;
                    _decisionExplainabilityBridge.TryEmitBridgeTrace(explainabilityConfig, world.MemoryBeliefDecisionExplainability, nowTick, npcId, selection.Candidate, cmd, didSteal, didMove, false, true, LegacyFallbackKind.NoOpFallback, "UnsupportedIntentNoOp");
                    return false;
            }
        }

        // =============================================================================
        // ResolveFoodJobRouteFallbackKind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Classifica il motivo per cui la route <c>EatKnownFood -> Job</c> e' tornata
        /// al percorso legacy.
        /// </para>
        ///
        /// <para><b>Classificazione diagnostica, non routing</b></para>
        /// <para>
        /// Questo helper non decide se emettere command e non assegna job. Traduce una
        /// reason gia' prodotta dal boundary food-job in una categoria stabile per EL e
        /// log strutturato, lasciando invariato il comportamento runtime v0.11B.
        /// </para>
        /// </summary>
        private static LegacyFallbackKind ResolveFoodJobRouteFallbackKind(string jobRouteReason, bool planned)
        {
            if (!planned)
                return LegacyFallbackKind.SafetyFallback;

            if (string.Equals(jobRouteReason, "GateDisabled", System.StringComparison.OrdinalIgnoreCase))
                return LegacyFallbackKind.CompatibilityFallback;

            if (string.Equals(jobRouteReason, "ReservationDenied", System.StringComparison.OrdinalIgnoreCase))
                return LegacyFallbackKind.CompatibilityFallback;

            if (string.Equals(jobRouteReason, "SameOrLowerPriority", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "CurrentJobPreferred", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "CurrentStillPreferred", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "CurrentPhaseProtected", System.StringComparison.OrdinalIgnoreCase))
                return LegacyFallbackKind.CompatibilityFallback;

            if (string.IsNullOrWhiteSpace(jobRouteReason)
                || jobRouteReason.StartsWith("FoodJobRouteFailed", System.StringComparison.OrdinalIgnoreCase)
                || jobRouteReason.StartsWith("ResolvedTargetMismatch", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "KnownCommunityFoodMissing", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "JobRuntimeMissing", System.StringComparison.OrdinalIgnoreCase))
                return LegacyFallbackKind.SafetyFallback;

            return LegacyFallbackKind.TransitionalDebtFallback;
        }

        private static LegacyFallbackKind ResolveSearchFoodJobRouteFallbackKind(string jobRouteReason)
        {
            if (string.Equals(jobRouteReason, "SearchFoodProbeUnavailable", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "NpcPositionMissing", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "JobRuntimeMissing", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "RegistryMissing", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "MissingSearchFoodProbeCell", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "InvalidSearchFoodJobIntent", System.StringComparison.OrdinalIgnoreCase))
            {
                return LegacyFallbackKind.SafetyFallback;
            }

            return LegacyFallbackKind.NonExecutableIntentFallback;
        }

        // =============================================================================
        // BuildFoodJobRouteFallbackReason
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce reason string stabili per il log di fallback della route food-job.
        /// </para>
        /// </summary>
        private static string BuildFoodJobRouteFallbackReason(string jobRouteReason, bool planned)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(jobRouteReason) ? "Unknown" : jobRouteReason;

            if (string.Equals(normalizedReason, "GateDisabled", System.StringComparison.OrdinalIgnoreCase))
                normalizedReason = "FoodJobGateDisabled";
            else if (string.Equals(normalizedReason, "ReservationDenied", System.StringComparison.OrdinalIgnoreCase))
                normalizedReason = "ReservationDeniedLegacyFoodFallback";
            else if (string.Equals(normalizedReason, "SameOrLowerPriority", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedReason, "CurrentJobPreferred", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedReason, "CurrentStillPreferred", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedReason, "CurrentPhaseProtected", System.StringComparison.OrdinalIgnoreCase))
                normalizedReason = "JobArbiterRejectedLegacyFoodFallback";
            else if (string.Equals(normalizedReason, "KnownCommunityFoodMissing", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedReason, "JobRuntimeMissing", System.StringComparison.OrdinalIgnoreCase)
                || normalizedReason.StartsWith("ResolvedTargetMismatch", System.StringComparison.OrdinalIgnoreCase))
                normalizedReason = "FoodJobRouteFailed:" + normalizedReason;

            return planned
                ? "FoodJobRouteRejectedThenLegacyFood:" + normalizedReason
                : "FoodJobRouteFailed:" + normalizedReason;
        }

        // =============================================================================
        // LogSearchFoodJobRoute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Emette diagnostica strutturata per la sola route SearchFood -> Job.
        /// </para>
        ///
        /// <para><b>Diagnostica senza cambio di comportamento</b></para>
        /// <para>
        /// Questo helper non decide, non costruisce job, non assegna job e non modifica
        /// fallback. Serve solo a rendere osservabile il punto esatto in cui la catena
        /// <c>SearchFood -> JobRequest -> Job -> TryAssignJob</c> si interrompe oppure
        /// conferma il successo. Il successo non passa da <c>DecisionBridgeFallback</c>
        /// perche' usa <c>FallbackKind.None</c>, quindi senza questo log positivo lo
        /// smoke runtime non puo' distinguere tra route accettata e pannello UI vuoto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>stage</b>: punto della route raggiunto.</item>
        ///   <item><b>reason</b>: reason stabile o reason runtime specifica prefissata.</item>
        ///   <item><b>probe/request/factory/assignment</b>: booleane diagnostiche additive.</item>
        /// </list>
        /// </summary>
        private static void LogSearchFoodJobRoute(
            int tick,
            int npcId,
            string stage,
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
                new LogContext(tick: tick, channel: "DecisionBridgeJobRoute", npcId: npcId),
                new LogBlock(LogLevel.Debug, "log.decision.bridge.job_route")
                    .AddField("tick", tick)
                    .AddField("npcId", npcId)
                    .AddField("intent", DecisionIntentKind.SearchFood.ToString())
                    .AddField("stage", stage ?? string.Empty)
                    .AddField("reason", reason ?? string.Empty)
                    .AddField("gateEnabled", gateEnabled)
                    .AddField("probeFound", probeFound)
                    .AddField("probeX", probeFound ? probeCell.x : 0)
                    .AddField("probeY", probeFound ? probeCell.y : 0)
                    .AddField("requestBuilt", requestBuilt)
                    .AddField("factoryCreated", factoryCreated)
                    .AddField("jobId", jobId ?? string.Empty)
                    .AddField("assigned", assigned)
                    .AddField("assignReason", assignReason ?? string.Empty));
        }

        // =============================================================================
        // ToBeliefRef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia una credenza selezionata in un riferimento EL minimale.
        /// </para>
        ///
        /// <para><b>Belief soggettivo senza store live</b></para>
        /// <para>
        /// Il record contiene solo valori primitivi del belief vincitore. Non espone
        /// la lista del BeliefStore e non permette al log di mutare conoscenza NPC.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Identita'</b>: categoria e id locale per-NPC.</item>
        ///   <item><b>Qualita'</b>: confidence, freshness, status e source.</item>
        ///   <item><b>Target</b>: posizione stimata usata dalla decisione.</item>
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
        // ApplyFoodRouteFailureCognitiveFeedback
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce i fallimenti significativi del path food runtime in un feedback
        /// cognitivo minimo, mantenendo invariati fallback legacy e job semantics.
        /// </para>
        ///
        /// <para><b>Failure operativo != sempre belief falsa</b></para>
        /// <para>
        /// ARC-CON-014 richiede che i fallimenti significativi non restino muti per la
        /// cognizione dell'NPC. Questo pero' non significa invalidare sempre il target:
        /// una reservation negata o un job rifiutato dall'arbitro indicano contesa o
        /// scheduling, non necessariamente una credenza falsa. Solo i casi in cui il
        /// target ricordato manca o non coincide piu' generano una contradiction.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Contradiction</b>: cibo ricordato mancante o target mismatch, con update BeliefStore.</item>
        ///   <item><b>Operational trace</b>: reservation denied o assignment rejected, senza invalidare belief.</item>
        ///   <item><b>Anti-spam</b>: non riemette contradiction se la belief e' gia' Discarded.</item>
        /// </list>
        /// </summary>
        private void ApplyFoodRouteFailureCognitiveFeedback(
            World world,
            int npcId,
            int nowTick,
            DecisionCandidate candidate,
            string jobRouteReason,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            Telemetry telemetry)
        {
            if (world == null || candidate.Kind != DecisionIntentKind.EatKnownFood || candidate.BeliefResult.IsEmpty)
                return;

            if (string.Equals(jobRouteReason, "KnownCommunityFoodMissing", System.StringComparison.OrdinalIgnoreCase))
            {
                ApplyFoodBeliefContradictionFromCandidate(
                    world,
                    npcId,
                    nowTick,
                    candidate,
                    "BeliefContradiction:RememberedFoodMissing",
                    telemetry);
                return;
            }

            if (!string.IsNullOrWhiteSpace(jobRouteReason)
                && jobRouteReason.StartsWith("ResolvedTargetMismatch", System.StringComparison.OrdinalIgnoreCase))
            {
                ApplyFoodBeliefContradictionFromCandidate(
                    world,
                    npcId,
                    nowTick,
                    candidate,
                    "BeliefContradiction:FoodTargetMismatch",
                    telemetry);
                return;
            }

            if (string.Equals(jobRouteReason, "ReservationDenied", System.StringComparison.OrdinalIgnoreCase))
            {
                EmitFoodOperationalFailureTrace(
                    world,
                    npcId,
                    nowTick,
                    candidate,
                    JobFailureReason.ReservationDenied,
                    "OperationalFailure:ReservationDenied",
                    explainabilityConfig);
                return;
            }

            if (IsFoodJobAssignmentRejected(jobRouteReason))
            {
                EmitFoodOperationalFailureTrace(
                    world,
                    npcId,
                    nowTick,
                    candidate,
                    JobFailureReason.Unknown,
                    "OperationalFailure:JobAssignmentRejected",
                    explainabilityConfig);
            }
        }

        // =============================================================================
        // ApplyFoodBeliefContradictionFromCandidate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica una contradiction locale alla belief Food che ha guidato il
        /// candidato EatKnownFood selezionato.
        /// </para>
        ///
        /// <para><b>Feedback cognitivo minimo e localizzato</b></para>
        /// <para>
        /// Il metodo non cerca nuovi target e non decide una nuova azione. Usa solo la
        /// belief gia' trasportata dal candidato MBQD, delega la mutazione al
        /// <c>BeliefUpdater</c> e produce una trace Belief esplicita quando lo store
        /// e' stato davvero aggiornato.
        /// </para>
        /// </summary>
        private void ApplyFoodBeliefContradictionFromCandidate(
            World world,
            int npcId,
            int nowTick,
            DecisionCandidate candidate,
            string reason,
            Telemetry telemetry)
        {
            if (!world.Beliefs.TryGetValue(npcId, out var store) || store == null)
                return;

            var selectedBelief = candidate.BeliefResult.Belief;
            if (selectedBelief.Status == BeliefStatus.Discarded)
            {
                telemetry?.Counter("BeliefUpdater.FoodRouteContradictionAlreadyDiscarded", 1);
                return;
            }

            var signal = new BeliefFailureSignal(
                npcId: npcId,
                beliefId: selectedBelief.BeliefId,
                category: BeliefCategory.Food,
                estimatedPosition: selectedBelief.EstimatedPosition,
                failureKind: BeliefFailureKind.DirectLocalContradiction,
                penalty01: 1f,
                tick: nowTick);

            bool updated = _beliefUpdater.UpdateFromOperationalFailure(signal, store);
            telemetry?.Counter(updated ? "BeliefUpdater.FoodRouteContradictionApplied" : "BeliefUpdater.FoodRouteContradictionNoMatch", 1);

            if (updated && TryFindBeliefById(store, selectedBelief.BeliefId, out var updatedBelief))
            {
                var explainabilityConfig = world.Config?.Sim?.memory_belief_decision_explainability;
                if (!MemoryBeliefDecisionExplainabilityEmitter.ShouldWriteTrace(
                        explainabilityConfig,
                        MemoryBeliefDecisionTraceKind.Belief))
                    return;

                MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(
                    explainabilityConfig,
                    world.MemoryBeliefDecisionExplainability,
                    new MemoryBeliefDecisionTrace
                    {
                        Kind = MemoryBeliefDecisionTraceKind.Belief,
                        Tick = nowTick,
                        NpcId = npcId,
                        Belief = new MemoryBeliefDecisionBeliefRecord
                        {
                            Operation = MemoryBeliefDecisionBeliefOperation.Discarded,
                            HasSourceTrace = false,
                            SourceTraceType = default,
                            Belief = ToBeliefRef(updatedBelief),
                            Reason = reason ?? string.Empty,
                        },
                    });
            }
        }

        // =============================================================================
        // EmitFoodOperationalFailureTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra un fallimento operativo del food path senza invalidare la belief
        /// che ha motivato la decisione.
        /// </para>
        ///
        /// <para><b>Trace operativa senza mutazione cognitiva forte</b></para>
        /// <para>
        /// Reservation denied e assignment rejected sono segnali utili per audit e
        /// futuro scoring, ma non provano che il cibo non esista. Per questo la patch
        /// li rende osservabili come failure learning EL e lascia intatto il
        /// BeliefStore.
        /// </para>
        /// </summary>
        private static void EmitFoodOperationalFailureTrace(
            World world,
            int npcId,
            int nowTick,
            DecisionCandidate candidate,
            JobFailureReason failureReason,
            string reason,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig)
        {
            if (world == null || explainabilityConfig == null)
                return;

            var targetCell = candidate.BeliefResult.IsEmpty
                ? Vector2Int.zero
                : candidate.BeliefResult.Belief.EstimatedPosition;

            if (HasLatestEquivalentFailureTrace(world, npcId, targetCell, failureReason, reason))
                return;

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteFailureLearningTrace(
                explainabilityConfig,
                world.MemoryBeliefDecisionExplainability,
                npcId,
                nowTick,
                string.Empty,
                targetCell,
                failureReason,
                nowTick,
                0f,
                reason);
        }

        private static bool IsFoodJobAssignmentRejected(string jobRouteReason)
        {
            return string.Equals(jobRouteReason, "CurrentStillPreferred", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "CurrentJobPreferred", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "SameOrLowerPriority", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "CurrentPhaseProtected", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "CurrentPhaseNotInterruptible", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "NewJobMissing", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "InvalidNpcId", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobRouteReason, "JobMissing", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryFindBeliefById(BeliefStore store, int beliefId, out BeliefEntry belief)
        {
            belief = default;

            if (store == null || beliefId <= 0)
                return false;

            var entries = store.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].BeliefId != beliefId)
                    continue;

                belief = entries[i];
                return true;
            }

            return false;
        }

        private static bool HasLatestEquivalentFailureTrace(
            World world,
            int npcId,
            Vector2Int targetCell,
            JobFailureReason failureReason,
            string reason)
        {
            if (world?.MemoryBeliefDecisionExplainability == null)
                return false;

            if (!world.MemoryBeliefDecisionExplainability.TryGetNpcStore(npcId, out var store) || store == null)
                return false;

            if (!store.TryGetLatestFailureLearningTrace(out var trace) || trace?.FailureLearning == null)
                return false;

            return trace.FailureLearning.TargetCell == targetCell
                && trace.FailureLearning.FailureReason == failureReason
                && string.Equals(trace.FailureLearning.Reason, reason, System.StringComparison.Ordinal);
        }

        // =============================================================================
        // Clamp01
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza un valore nel range 0-1 usato dai parametri EL del bridge.
        /// </para>
        ///
        /// <para><b>Validazione numerica locale</b></para>
        /// <para>
        /// Il calcolo dell'effective noise replica una formula diagnostica: il clamp
        /// evita valori fuori range senza dipendere da helper esterni.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Lower bound</b>: valori negativi diventano 0.</item>
        ///   <item><b>Upper bound</b>: valori sopra 1 diventano 1.</item>
        ///   <item><b>Return</b>: valore sicuro per JSONL e UI futura.</item>
        /// </list>
        /// </summary>
        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        // =============================================================================
        // ResolveDecisionSelectionConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la configurazione JSON del Decision Layer nella struct usata dal
        /// servizio di selezione.
        /// </para>
        ///
        /// <para><b>QA deterministico senza cambiare il SelectionService</b></para>
        /// <para>
        /// La modalita' <c>DeterministicTop1</c> viene tradotta in <c>topN = 1</c>,
        /// <c>noise01 = 0</c> e <c>impulsivityNoiseBonus = 0</c>. Il servizio weighted
        /// random considera quindi un solo candidato: il migliore ordinato per score.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Defaults</b>: usa i valori storici quando la sezione manca.</item>
        ///   <item><b>WeightedRandomTopN</b>: legge topN, noise e pesi minimi da JSON.</item>
        ///   <item><b>DeterministicTop1</b>: forza una selezione ripetibile per test runtime.</item>
        /// </list>
        /// </summary>
        private static DecisionSelectionConfig ResolveDecisionSelectionConfig(DecisionRuntimeParams runtimeConfig)
        {
            var config = DecisionSelectionConfig.Default();
            if (runtimeConfig == null)
                return config;

            if (string.Equals(runtimeConfig.selectionMode, "DeterministicTop1", System.StringComparison.OrdinalIgnoreCase))
            {
                config.topN = 1;
                config.noise01 = 0f;
                config.impulsivityNoiseBonus = 0f;
                config.minimumWeight = runtimeConfig.minimumWeight > 0f ? runtimeConfig.minimumWeight : config.minimumWeight;
                return config;
            }

            // In modalita' standard manteniamo fallback conservativi: valori non
            // positivi nel JSON non devono azzerare accidentalmente il selettore.
            config.topN = runtimeConfig.topN > 0 ? runtimeConfig.topN : config.topN;
            config.noise01 = Clamp01(runtimeConfig.noise01);
            config.impulsivityNoiseBonus = Clamp01(runtimeConfig.impulsivityNoiseBonus);
            config.minimumWeight = runtimeConfig.minimumWeight > 0f ? runtimeConfig.minimumWeight : config.minimumWeight;
            return config;
        }

        // =============================================================================
        // TryStartSearchFoodJobVerticalSlice
        // =============================================================================
        /// <summary>
        /// <para>
        /// Devia il solo intent <c>SearchFood</c> verso un job eseguibile minimale:
        /// movimento verso una probe cell locale.
        /// </para>
        ///
        /// <para><b>Probe locale, non conoscenza del cibo</b></para>
        /// <para>
        /// La probe cell e' una destinazione operativa esplorativa scelta vicino
        /// all'NPC. Non rappresenta una belief Food, non deriva da <c>World.Objects</c>
        /// o <c>FoodStocks</c> e non aggiorna direttamente memoria o belief. Il suo
        /// unico scopo e' far muovere fisicamente l'NPC in modo che, nei tick
        /// successivi, <c>ObjectPerceptionSystem</c>, <c>MemoryEncodingSystem</c> e
        /// <c>BeliefUpdater</c> possano eventualmente scoprire cibo tramite la
        /// pipeline percettiva reale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Probe</b>: risolta in modo deterministico da posizione NPC e vincoli fisici locali.</item>
        ///   <item><b>Request</b>: <c>SearchFood</c>, target cell presente, target object assente.</item>
        ///   <item><b>Factory</b>: materializza il template <c>food.search_local_probe.v1</c>.</item>
        ///   <item><b>Assign</b>: consegna il job a <c>JobRuntimeState</c> senza emettere command legacy.</item>
        /// </list>
        /// </summary>
        private bool TryStartSearchFoodJobVerticalSlice(
            World world,
            int npcId,
            int nowTick,
            DecisionCandidate candidate,
            Telemetry telemetry,
            out string reason)
        {
            reason = string.Empty;

            LogSearchFoodJobRoute(
                nowTick,
                npcId,
                "EnterSearchFoodRoute",
                "EnterSearchFoodRoute",
                _enableFoodJobVerticalSlice);

            if (!_enableFoodJobVerticalSlice)
            {
                reason = "GateDisabled";
                LogSearchFoodJobRoute(
                    nowTick,
                    npcId,
                    "GateDisabled",
                    reason,
                    _enableFoodJobVerticalSlice);
                return false;
            }

            if (world?.JobRuntimeState == null)
            {
                reason = "JobRuntimeMissing";
                LogSearchFoodJobRoute(
                    nowTick,
                    npcId,
                    "JobRuntimeMissing",
                    reason,
                    _enableFoodJobVerticalSlice);
                return false;
            }

            if (!TryResolveSearchFoodProbeCell(world, npcId, out var probeCell, out reason))
            {
                LogSearchFoodJobRoute(
                    nowTick,
                    npcId,
                    "ProbeUnavailable",
                    reason,
                    _enableFoodJobVerticalSlice);
                return false;
            }

            LogSearchFoodJobRoute(
                nowTick,
                npcId,
                "ProbeResolved",
                reason,
                _enableFoodJobVerticalSlice,
                probeFound: true,
                probeCell: probeCell);

            if (!_intentExecutionRouter.TryRouteSearchFood(
                nowTick,
                npcId,
                candidate,
                probeCell,
                out var route))
            {
                reason = route.Reason;
                LogSearchFoodJobRoute(
                    nowTick,
                    npcId,
                    "RequestBuildFailed",
                    "SearchFoodRequestBuildFailed:" + reason,
                    _enableFoodJobVerticalSlice,
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
                _enableFoodJobVerticalSlice,
                probeFound: true,
                probeCell: probeCell,
                requestBuilt: true);

            bool created = SearchFoodJobFactory.TryCreateSearchFoodLocalProbeJob(
                _jobTemplateRegistry,
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
                    _enableFoodJobVerticalSlice,
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
                _enableFoodJobVerticalSlice,
                probeFound: true,
                probeCell: probeCell,
                requestBuilt: true,
                factoryCreated: true,
                jobId: job.JobId);

            _decisionExplainabilityBridge.TryEmitJobRequestTrace(
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
                _enableFoodJobVerticalSlice,
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
        // TryResolveSearchFoodProbeCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sceglie una cella locale esplorativa per SearchFood senza trasformarla in
        /// conoscenza cognitiva di una fonte di cibo.
        /// </para>
        ///
        /// <para><b>Anti-telepatia nella probe policy</b></para>
        /// <para>
        /// Questo helper usa solo posizione NPC e vincoli fisici locali: bounds,
        /// blocco movimento e occupazione. Non legge oggetti per categoria, non
        /// consulta stock alimentari, non mantiene memoria di celle visitate e non
        /// introduce random policy. La scelta e' deterministica per rendere la slice
        /// testabile e compatibile col tick runtime.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Origine</b>: posizione corrente dell'NPC da <c>GridPos</c>.</item>
        ///   <item><b>Ordine stabile</b>: cardinali a raggio 1, poi diagonali/cardinali a raggio 2.</item>
        ///   <item><b>Gate fisici</b>: in bounds, non movement-blocked, non occupata da NPC o oggetto.</item>
        /// </list>
        /// </summary>
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
            var offsets = SearchFoodProbeOffsets;
            for (int i = 0; i < offsets.Length; i++)
            {
                var candidate = origin + offsets[i];
                if (!IsValidSearchFoodProbeCell(world, npcId, candidate))
                    continue;

                probeCell = candidate;
                reason = "SearchFoodProbeResolved";
                return true;
            }

            reason = "SearchFoodProbeUnavailable";
            return false;
        }

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

            // TODO ARC-CON-014: preferire celle non attualmente visibili solo quando
            // esistera' un helper canonico "cell currently visible by NPC" riusabile
            // senza duplicare la semantica di ObjectPerceptionSystem/FOV debug.
            return true;
        }

        // =============================================================================
        // TryStartFoodJobVerticalSlice
        // =============================================================================
        /// <summary>
        /// <para>
        /// Devia in modo opt-in il solo caso <c>EatKnownFood</c> community/known stock
        /// verso il Job System runtime.
        /// </para>
        ///
        /// <para><b>Ponte temporaneo legacy -> job senza doppia pipeline</b></para>
        /// <para>
        /// Questo metodo non sostituisce ancora <c>NeedsDecisionRule</c>. Usa la rule
        /// legacy come punto controllato di bootstrap della vertical slice: se riesce
        /// ad assegnare un job, il chiamante non deve emettere command legacy nello
        /// stesso tick per la stessa intenzione. Se il gate e' spento o la route non
        /// e' applicabile, il comportamento precedente resta invariato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Gate</b>: default false, comandato da <c>SimulationHost</c>.</item>
        ///   <item><b>Target</b>: solo food stock community visibile o ricordato.</item>
        ///   <item><b>Factory</b>: trasforma target gia' scelto in job da template JSON.</item>
        ///   <item><b>Assign</b>: scrive in <c>World.JobRuntimeState</c>, non nel profilo NPC.</item>
        /// </list>
        /// </summary>
        private bool TryStartFoodJobVerticalSlice(
            World world,
            int npcId,
            int nowTick,
            DecisionCandidate candidate,
            Telemetry telemetry,
            out string reason)
        {
            reason = string.Empty;

            if (!_enableFoodJobVerticalSlice)
            {
                reason = "GateDisabled";
                return false;
            }

            if (world?.JobRuntimeState == null)
            {
                reason = "JobRuntimeMissing";
                return false;
            }

            if (!TryResolveKnownCommunityFoodTarget(world, npcId, out int foodObjectId, out int targetX, out int targetY, out string targetSource))
            {
                reason = "KnownCommunityFoodMissing";
                return false;
            }

            if (!_intentExecutionRouter.TryRouteEatKnownFood(
                nowTick,
                npcId,
                candidate,
                foodObjectId,
                out var route))
            {
                reason = route.Reason;
                return false;
            }

            var request = route.Request;
            // Il boundary reale di questa patch resta intenzionalmente stretto:
            // il Decision Layer seleziona EatKnownFood e produce un JobRequest dati,
            // mentre la verifica legacy del target operativo resta qui nel bridge.
            // Se la cella risolta dal path storico diverge dalla belief selezionata,
            // non inventiamo una nuova semantica: lasciamo proseguire i fallback
            // legacy gia' presenti e compatibili con v0.11B.
            if (request.TargetCell.x != targetX || request.TargetCell.y != targetY)
            {
                reason = "ResolvedTargetMismatch:" + targetSource;
                return false;
            }

            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                _jobTemplateRegistry,
                request,
                out var job,
                out reason);

            if (!created)
                return false;

            _decisionExplainabilityBridge.TryEmitJobRequestTrace(
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

        private bool TryResolveKnownCommunityFoodTarget(
            World world,
            int npcId,
            out int foodObjectId,
            out int targetX,
            out int targetY,
            out string targetSource)
        {
            foodObjectId = 0;
            targetX = 0;
            targetY = 0;
            targetSource = string.Empty;

            foodObjectId = FindVisibleCommunityFoodStock(world, npcId, _maxSeekRangeCells);
            if (foodObjectId != 0 && TryGetObjectCell(world, foodObjectId, out targetX, out targetY))
            {
                targetSource = "VisibleCommunityFood";
                return true;
            }

            foodObjectId = FindRememberedCommunityFoodStock(world, npcId, _maxSeekRangeCells, out targetX, out targetY);
            if (foodObjectId != 0)
            {
                targetSource = "RememberedCommunityFood";
                return true;
            }

            return false;
        }

        // =============================================================================
        // TryStartFoodJobVerticalSliceFromLegacyFallback
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica lo stesso gate food-job al fallback storico fame -> command.
        /// </para>
        ///
        /// <para><b>Anti-bypass transitorio</b></para>
        /// <para>
        /// In v0.11.01 <c>NeedsDecisionRule</c> resta legacy, quindi esistono ancora
        /// percorsi che possono arrivare a <c>TryPlanEatOrMove</c> quando il Decision
        /// Layer non ha gestito l'intenzione. Questo helper intercetta solo il caso
        /// gia' autorizzato e sicuro: food stock community conosciuto/visibile. Se la
        /// route job non accetta, il fallback legacy prosegue invariato.
        /// </para>
        /// </summary>
        private bool TryStartFoodJobVerticalSliceFromLegacyFallback(
            World world,
            int npcId,
            int nowTick,
            in NpcNeeds needs,
            Telemetry telemetry,
            out string reason)
        {
            reason = string.Empty;

            if (!_enableFoodJobVerticalSlice)
            {
                reason = "GateDisabled";
                return false;
            }

            if (world?.JobRuntimeState == null)
            {
                reason = "JobRuntimeMissing";
                return false;
            }

            if (!TryResolveKnownCommunityFoodTarget(world, npcId, out int foodObjectId, out int targetX, out int targetY, out string targetSource))
            {
                reason = "KnownCommunityFoodMissing";
                return false;
            }

            float urgency01 = needs.GetValue(NeedKind.Hunger);
            bool created = FoodJobFactory.TryCreateKnownCommunityFoodJob(
                _jobTemplateRegistry,
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

        // ============================================================
        // EAT DECISION
        // ============================================================

        /// <summary>
        /// Day10: Eat OR Move OR Steal.
        ///
        /// Ordine decisionale (coerente con la policy di progetto):
        /// 1) cibo privato addosso
        /// 2) cibo community visibile: se sei sulla cella -> mangia, altrimenti -> muoviti
        /// 3) se NON esiste cibo legale e "okToSteal":
        ///    3a) stock privato a terra (OwnerKind=Npc, OwnerId!=me) visibile: se sei sulla cella -> ruba unità, altrimenti -> muoviti
        ///    3b) altrimenti prova furto "addosso" (NpcPrivateFood) come fallback
        ///
        /// Nota:
        /// - Questo metodo evita sia "mangiare a distanza" sia "rubare a distanza".
        /// - Il movimento viene espresso tramite SetMoveIntentCommand.
        ///   (Se nel tuo branch non esiste ancora MoveIntent, questo è il punto dove dovrai allineare i tipi.)
        /// </summary>
        private bool TryPlanEatOrMove(
            World world,
            int npcId,
            in NpcNeeds needs,
            int nowTick,
            Telemetry telemetry,
            out ICommand cmd,
            out bool didSteal,
            out bool didMove)
        {
            cmd = null;
            didSteal = false;
            didMove = false;

            // 1) privato
            if (world.NpcPrivateFood.TryGetValue(npcId, out int priv) && priv > 0)
            {
                cmd = new EatPrivateFoodCommand(npcId);
                return true;
            }

            
            // 1b) Patch 5.1 (revised): stock privato *mio* a terra (Pinned BELIEF, non memoria percettiva e non telepatia)
            //
            // Bug che correggiamo:
            // - Se un NPC ha cibo "privato" NON addosso (NpcPrivateFood=0) ma depositato a terra
            //   in uno stock OwnerKind=Npc, OwnerId=thisNpc, la logica precedente lo ignorava
            //   e l'NPC poteva morire di fame o passare direttamente a rubare.
            //
            // Policy ARCONTIO (manifesto):
            // - La decisione deve usare percezione/memoria soggettiva, NON scansioni globali.
            //
            // Regola fisica (richiesta utente):
            // - Per mangiare o rubare cibo a terra dallo stock, devi essere SULLA cella dello stock (co-locazione).
            // - Se non sei sulla cella, pianifica un MoveIntent verso l'ultima cella nota dello stock.
            int ownFoodObj = FindPinnedBelievedOwnNpcFoodStock(world, npcId, _maxSeekRangeCells, out int ox, out int oy);
            if (ownFoodObj != 0)
            {
                if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                    return false;

                if (nx == ox && ny == oy)
                {
                    // Sei arrivato nella cella dove *credi* di avere il tuo stock privato.
                    //
                    // Ora facciamo la verifica "locale" (non telepatica):
                    // - se lo stock esiste ancora IN QUELLA CELLA ed è effettivamente ancora tuo e non vuoto -> mangia.
                    // - altrimenti, significa che l'NPC ha appena scoperto una discrepanza (furto/distruzione/spostamento).
                    //   In questo caso invalidiamo la belief e proseguiamo con le alternative (community / furto).
                    int objAtCell = world.GetObjectAt(ox, oy);

                    bool foundMyStockHere = false;

                    if (objAtCell == ownFoodObj && world.FoodStocks.TryGetValue(ownFoodObj, out var st))
                    {
                        if (st.Units > 0 && st.OwnerKind == OwnerKind.Npc && st.OwnerId == npcId)
                        {
                            foundMyStockHere = true;
                        }
                    }

                    if (foundMyStockHere)
                    {
                        // Stock confermato sul posto: l'NPC può mangiare.
                        cmd = new EatFromStockCommand(npcId, ownFoodObj);
                        return true;
                    }

                    // Scoperta locale: lo stock non è dove doveva essere, o non è più mio.
                    // Aggiorniamo la belief pinned: da questo momento non lo considero più disponibile.
                    world.RemovePinnedFoodStockBelief(npcId, ownFoodObj);
                    ApplyDirectFoodBeliefContradiction(world, npcId, ox, oy, nowTick, telemetry);

                    // Nota:
                    // - Qui NON creiamo ancora un evento di "furto sospettato": sarebbe un sistema separato.
                    // - Per ora ci limitiamo a far sì che l'NPC non resti bloccato a cercare all'infinito.
                }

                // Non sei ancora arrivato: vai sulla cella ricordata dello stock.
                didMove = true;

                cmd = new SetMoveIntentCommand(npcId, new MoveIntent
                {
                    Active = true,
                    TargetX = ox,
                    TargetY = oy,
                    Reason = MoveIntentReason.SeekFood,
                    TargetObjectId = ownFoodObj
                });

                return true;
            }

// 2) stock community (visibile OR remembered)
            // -----------------------------------------------------------------
            // REGRESSION FIX (0.02.05.2b):
            // Nelle patch recenti abbiamo irrigidito molto la rule per evitare
            // telepatia sui food stock community. Il risultato collaterale, però,
            // è stato questo:
            // - se l'NPC VEDEVA uno stock community, lo registrava correttamente
            //   nella memoria oggetti;
            // - ma appena usciva dalla LOS corrente, la decisione smetteva di
            //   considerarlo un target valido, perché qui interrogavamo SOLO
            //   FindVisibleCommunityFoodStock(...).
            //
            // Questo inibiva un comportamento che in versioni precedenti era di
            // fatto possibile: "ho fame, so dove c'è cibo, quindi vado a prenderlo
            // anche se in questo tick non lo sto vedendo".
            //
            // La correzione qui sotto mantiene il vincolo anti-telepatia:
            // - PRIMA preferiamo uno stock realmente visibile ORA;
            // - SE non c'è nulla di visibile, usiamo uno stock community ricordato
            //   nella memoria soggettiva dell'NPC.
            //
            // Quindi la conoscenza torna ad essere operativa, ma senza tornare a
            // scandire il mondo globale come facevano le versioni "telepatiche".
            int foodObj = FindVisibleCommunityFoodStock(world, npcId, _maxSeekRangeCells);
            bool foodTargetFromMemory = false;
            int fx = 0, fy = 0;

            if (foodObj != 0)
            {
                if (!TryGetObjectCell(world, foodObj, out fx, out fy))
                    return false;
            }
            else
            {
                // Fallback memory-driven: uso SOLO ciò che l'NPC ricorda di avere visto.
                foodObj = FindRememberedCommunityFoodStock(world, npcId, _maxSeekRangeCells, out fx, out fy);
                foodTargetFromMemory = foodObj != 0;
            }

            if (foodObj != 0)
            {
                if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                    return false;

                if (nx == fx && ny == fy)
                {
                    // Arrivo sulla cella target.
                    // Se il target veniva dalla memoria, verifica locale: il cibo è ancora qui?
                    if (foodTargetFromMemory)
                    {
                        bool foodStillHere = world.FoodStocks.TryGetValue(foodObj, out var st)
                                            && st.Units > 0;

                        if (!foodStillHere)
                        {
                            // Scoperta locale: il cibo non c'è più.
                            // Invalida il memory slot — l'NPC sa ora che è obsoleto.
                            if (world.NpcObjectMemory.TryGetValue(npcId, out var mem) && mem != null)
                            {
                                for (int si = 0; si < mem.Slots.Length; si++)
                                {
                                    ref var slot = ref mem.Slots[si];
                                    if (!slot.IsValid) continue;
                                    int slotObjId = slot.SubjectId != 0 ? slot.SubjectId : slot.ObjectId;
                                    if (slotObjId == foodObj)
                                    {
                                        slot.IsValid = false;
                                        break;
                                    }
                                }
                            }

                            // Balloon debug: cibo non trovato dove ricordato.
                            world.EmitNpcBalloon(npcId, NpcBalloonKind.FoodNotFound);
                            ApplyDirectFoodBeliefContradiction(world, npcId, fx, fy, nowTick, telemetry);
                            return false;
                        }
                    }

                    // Cibo confermato (visibile o verificato localmente): mangia.
                    cmd = new EatFromStockCommand(npcId, foodObj);
                    return true;
                }

                didMove = true;

                // Fix stuck detection: se c'è già un MoveIntent attivo verso
                // le stesse coordinate, non sovrascriverlo. La Rule viene chiamata
                // ogni _decisionEveryTicks tick e senza questo check resetta
                // BlockedTicks ogni volta, impedendo allo stuck detection di
                // scattare quando il target è fisicamente irraggiungibile.
                if (world.NpcMoveIntents.TryGetValue(npcId, out var existingIntent)
                    && existingIntent.Active
                    && existingIntent.TargetX == fx
                    && existingIntent.TargetY == fy)
                {
                    // Intent già attivo verso lo stesso target: non sovrascrivere.
                    // MovementSystem gestirà lo stuck o troverà un percorso.
                    return true;
                }

                BeliefEntryRef beliefBasis = default;
                bool hasBeliefBasis = foodTargetFromMemory
                    && TryBuildFoodBeliefBasisForTarget(
                        world,
                        npcId,
                        nx,
                        ny,
                        fx,
                        fy,
                        needs.GetValue(NeedKind.Hunger),
                        nowTick,
                        out beliefBasis);

                cmd = new SetMoveIntentCommand(npcId, new MoveIntent
                {
                    Active = true,
                    TargetX = fx,
                    TargetY = fy,
                    Reason = MoveIntentReason.SeekFood,
                    TargetObjectId = foodTargetFromMemory ? 0 : foodObj,
                    HasBeliefBasis = hasBeliefBasis,
                    BeliefBasis = beliefBasis,
                    Urgency01 = needs.GetValue(NeedKind.Hunger)
                });

                // Nota molto utile per la lettura futura di questo ramo:
                // non cambia il tipo di comando, cambia solo la sorgente decisionale
                // del target. La card debug distinguerà poi Visible vs KnownObject.
                return true;
            }

            // 3) furto se moralità/emergenza
            float law = world.Social.TryGetValue(npcId, out var soc) ? soc.JusticePerception01 : 0.5f;
            bool emergency = needs.GetValue(NeedKind.Hunger) >= 0.95f;
            bool okToSteal = emergency || law < 0.45f;

            if (!okToSteal)
                return false;

            // 3a) Day10/Step5: furto da stock privato a terra (non addosso)
//
// CAMBIO ARCHITETTURALE (Step5):
// - Prima: FindVisibleOtherNpcFoodStock() scandiva world.FoodStocks => era "conoscenza globale".
// - Ora: scegliamo il target SOLO dalla memoria soggettiva dell'NPC (World.NpcObjectMemory[npcId]).
//
// Nota importante:
// - In execution (Step2) abbiamo già blindato il comando: il furto da stock è valido SOLO se sei sulla cella dello stock.
// - Qui, lato planning, facciamo la stessa cosa: se non sei sulla cella -> SetMoveIntentCommand.
int stolenStockObj = FindRememberedOtherNpcFoodStock(world, npcId, _maxSeekRangeCells, out int sx, out int sy, out int victimOwnerId);
if (stolenStockObj != 0)
{
    if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
        return false;

    if (nx == sx && ny == sy)
    {
        // Ruba davvero solo se sei sullo stock (regola: stesso tile).
        didSteal = true;
        cmd = new StealFromStockCommand(npcId, stolenStockObj);
        return true;
    }

    // Altrimenti ti avvicini prima: niente furto "a distanza".
    didMove = true;
    didSteal = true; // stai pianificando un'azione antisociale

    cmd = new SetMoveIntentCommand(npcId, new MoveIntent
    {
        Active = true,
        TargetX = sx,
        TargetY = sy,
        Reason = MoveIntentReason.SeekFood,
        TargetObjectId = stolenStockObj
    });

    return true;
}
    // 3b) Step5: furto di cibo "addosso" (NPC -> NPC)
    //
    // CAMBIO ARCHITETTURALE (Step5):
    // - Prima: FindNpcWithPrivateFood() scandiva world.NpcPrivateFood => telepatia (conoscenza globale).
    // - Ora: scegliamo la vittima SOLO dalla memoria soggettiva:
    //   World.NpcObjectMemory[npcId] con entry Kind=Npc e flag "HasCarriedFood".
    //
    // Regola di interazione (design):
    // - Il furto "addosso" è valido solo se sei ADIACENTE (Manhattan=1) e senza occlusioni (LOS).
    // - Se non sei in range, devi prima muoverti verso la last-known cell della vittima.
    int victim = FindRememberedNpcWithCarriedFood(world, npcId, _maxSeekRangeCells, out int vx, out int vy, out int carriedApprox);
    if (victim != 0)
    {
        if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
            return false;

        int manhattan = Mathf.Abs(vx - nx) + Mathf.Abs(vy - ny);

        // Se sei già vicino, prova il furto (execution farà comunque i check runtime Step2).
        if (manhattan == 1 && world.HasLineOfSight(nx, ny, vx, vy))
        {
            didSteal = true;
            cmd = new StealPrivateFoodCommand(npcId, victim);
            return true;
        }

        // Altrimenti: prima insegui la last-known cell della vittima.
        didMove = true;
        didSteal = true;

        cmd = new SetMoveIntentCommand(npcId, new MoveIntent
        {
            Active = true,
            TargetX = vx,
            TargetY = vy,
            Reason = MoveIntentReason.SeekFood,

            // Nota:
            // - MoveIntent oggi non ha TargetNpcId.
            // - NON usiamo TargetObjectId per non confondere i sistemi che assumono "oggetto nel mondo".
            TargetObjectId = 0
        });

        return true;
    }

    return false;
}

        // =============================================================================
        // TryBuildFoodBeliefBasisForTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una snapshot belief per l'EL pathfinding quando il Decision Layer
        /// sta gia' pianificando un movimento verso una cella di cibo ricordata.
        /// </para>
        ///
        /// <para><b>QuerySystem come causa esplicita</b></para>
        /// <para>
        /// Il metodo interroga il BeliefStore solo qui, nel livello decisionale, e solo
        /// per ottenere una causa diagnostica coerente con il target gia' scelto. Se la
        /// miglior belief Food non punta alla stessa cella, non forza alcuna causa:
        /// restituisce false e il MoveIntent resta privo di BeliefBasis.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>BeliefStore</b>: letto dal per-NPC store, mai dal pathfinding.</item>
        ///   <item><b>BeliefQueryService</b>: seleziona il miglior candidato Food.</item>
        ///   <item><b>Target match</b>: richiede che la cella stimata coincida col target.</item>
        ///   <item><b>BeliefEntryRef</b>: snapshot minimale passato al MoveIntent.</item>
        /// </list>
        /// </summary>
        private bool TryBuildFoodBeliefBasisForTarget(
            World world,
            int npcId,
            int npcX,
            int npcY,
            int targetX,
            int targetY,
            float urgency01,
            int nowTick,
            out BeliefEntryRef beliefBasis)
        {
            beliefBasis = default;

            if (!world.Beliefs.TryGetValue(npcId, out var store) || store == null)
                return false;

            var queryResult = _beliefQueryService.GetBestKnownFoodSource(
                store,
                new Vector2Int(npcX, npcY),
                urgency01,
                world.Global.BeliefQuery,
                world.Config?.Sim?.memory_belief_decision_explainability,
                npcId,
                nowTick,
                world.MemoryBeliefDecisionExplainability);

            if (queryResult.IsEmpty)
                return false;

            if (queryResult.Belief.EstimatedPosition.x != targetX
                || queryResult.Belief.EstimatedPosition.y != targetY)
                return false;

            return MovementExplainabilityBeliefSnapshot.TryFromQueryResult(
                queryResult,
                nowTick,
                out beliefBasis);
        }
        /// <summary>
        /// FindRememberedCommunityFoodStock (0.02.05.2b):
        ///
        /// Scopo:
        /// - ripristinare il comportamento corretto "ho visto del cibo, quindi posso
        ///   tornarci anche se ora non lo sto guardando", SENZA reintrodurre telepatia.
        ///
        /// Principio architetturale:
        /// - NON scandiamo il mondo per cercare cibo community;
        /// - scandiamo invece la memoria soggettiva world.NpcObjectMemory[npcId].
        ///
        /// Validazione minima:
        /// - l'entry deve essere un WorldObject compatibile con un food stock;
        /// - l'oggetto reale, se ancora esiste, deve risultare uno stock community con units > 0;
        /// - usiamo la posizione reale se l'oggetto è ancora nel World, altrimenti la last-known cell.
        ///
        /// Questa validazione non è telepatia "forte":
        /// stiamo controllando lo stato dell'oggetto che l'NPC ricorda già, non stiamo
        /// cercando nuovi target globali fuori dalla sua conoscenza.
        /// </summary>
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

                // Filtro ownership da metadati memoria (scritti al momento della percezione).
                if (e.OwnerKind != OwnerKind.Community || e.OwnerId != 0)
                    continue;

                // Coordinate: usa quelle reali se l'oggetto esiste ancora,
                // altrimenti usa quelle ricordate (l'NPC non sa che è sparito).
                // NON saltiamo lo slot se l'oggetto non esiste più in FoodStocks:
                // l'NPC deve poter andare alle coordinate ricordate per scoprire
                // che il cibo non c'è più (scoperta locale all'arrivo).
                int ox = e.CellX;
                int oy = e.CellY;

                if (world.FoodStocks.TryGetValue(objId, out var st))
                {
                    // Oggetto esiste: filtra stock esauriti e aggiorna posizione reale.
                    if (st.Units <= 0)
                        continue;
                    if (world.Objects.TryGetValue(objId, out var inst) && inst != null)
                    {
                        ox = inst.CellX;
                        oy = inst.CellY;
                    }
                }
                // else: oggetto non in FoodStocks → usa coordinate di memoria.

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

        /// <summary>
        /// Trova uno stock di cibo della Community che l'NPC può *realisticamente* usare:
        /// - lo stock deve avere Units > 0
        /// - deve essere OwnerKind=Community, OwnerId=0 (convenzione attuale)
        /// - deve essere "visibile" secondo un test minimo (range + LOS)
        ///
        /// Perché qui e non in World?
        /// - World è "verità oggettiva" e dovrebbe restare tendenzialmente neutro.
        /// - La nozione di "posso usarlo perché lo vedo" è una policy decisionale.
        /// </summary>
        private static int FindVisibleCommunityFoodStock(World world, int npcId, int maxRangeCells)
        {
            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            foreach (var kv in world.FoodStocks)
            {
                int objId = kv.Key;
                var st = kv.Value;
                //if (st == null) continue;

                if (st.Units <= 0) continue;
                if (st.OwnerKind != OwnerKind.Community || st.OwnerId != 0) continue;

                // IMPORTANTISSIMO:
                // Le coordinate NON sono in FoodStockComponent.
                // Le coordinate stanno in world.Objects[objId].
                if (!TryGetObjectCell(world, objId, out int ox, out int oy))
                    continue;

                // Range discreto: Manhattan (cheap e coerente con grid).
                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                // LOS: se un muro è in mezzo, HasLineOfSight deve tornare false.
                if (!world.HasLineOfSight(nx, ny, ox, oy))
                    continue;

                return objId;
            }

            return 0;
        }

        
        /// <summary>
        /// Day10: trova uno stock di cibo privato (OwnerKind=Npc) appartenente ad un altro NPC,
        /// che sia visibile (range + LOS) e con Units > 0.
        ///
        /// Nota strategica:
        /// - In futuro questo non dovrebbe essere "scan globale world.FoodStocks",
        ///   ma una query su NpcObjectMemoryStore (conoscenza soggettiva).
        /// - Per il test Day10 (seed) va benissimo: vogliamo validare meccanica furto + witness.
        /// </summary>
        
        
/// <summary>
/// Step5: cerca nella MEMORIA soggettiva dell'NPC uno stock di cibo privato (OwnerKind=Npc)
/// appartenente ad un altro NPC.
///
/// Principio:
/// - la scelta del target deve essere memory-driven, non world-driven.
/// - quindi NON scandiamo world.FoodStocks; scandiamo invece world.NpcObjectMemory[npcId].
///
/// Nota sulla robustezza:
/// - per evitare target "fantasma", facciamo una validazione puntuale sull'ObjectId:
///   world.FoodStocks.TryGetValue(objId, out st) e Units>0.
/// - questa NON è telepatia: non stiamo "cercando" cibo nel mondo, stiamo solo verificando
///   se l'ID che ricordo esiste ancora ed è effettivamente uno stock di cibo.
///
/// Ritorna:
/// - objectId dello stock se trovato
/// - out sx/sy = cella target (preferiamo la cella reale dal World se disponibile, altrimenti memoria)
/// - out ownerId = npc "vittima" (proprietario dello stock)
/// </summary>

        /// <summary>
        /// Step5+Fix runtime:
        /// Trova, nella MEMORIA soggettiva dell'NPC, uno stock di cibo privato appartenente a SE STESSO
        /// (OwnerKind=Npc, OwnerId=npcId).
        ///
        /// Perché esiste:
        /// - Caso comune: l'NPC deposita il proprio cibo a terra (stock privato) e poi deve tornarci per mangiare.
        /// - Senza questo ramo, l'NPC ignora il proprio stock a terra e passa a community/steal, risultando illogico.
        ///
        /// Policy:
        /// - Memory-driven: nessuna scansione globale del mondo per scegliere "che cosa c'è in giro".
        /// - Validazione runtime: anche se la memoria è stale, in execution i comandi verificano lo stato reale.
        /// </summary>
        private static 
        /// <summary>
        /// FindPinnedBelievedOwnNpcFoodStock (Patch 5.1 - revised):
        ///
        /// Scopo:
        /// - trovare "il mio stock privato a terra" usando una lista PINNED di belief,
        ///   non dipendente dalla memoria percettiva e non dipendente da scansioni globali del World.
        ///
        /// Perché:
        /// - Bug: l'NPC poteva ignorare il suo stock privato a terra se non era dentro NpcObjectMemory.
        /// - Vincolo manifesto: l'NPC NON deve sapere automaticamente se qualcuno gli ruba lo stock fuori vista.
        ///
        /// Comportamento:
        /// - L'NPC usa solo le "last known coordinates" conservate nella belief.
        /// - Se è già arrivato in quella cella e lo stock non c'è (o non è più suo / è vuoto),
        ///   allora la belief viene invalidata (RemovePinnedFoodStockBelief) e la rule prosegue
        ///   con le alternative (community / furto).
        /// - Se NON è ancora arrivato, pianifichiamo MoveIntent verso quella cella per ispezione.
        /// </summary>
        int FindPinnedBelievedOwnNpcFoodStock(
            World world,
            int npcId,
            int maxRangeCells,
            out int bestX,
            out int bestY)
        {
            bestX = 0;
            bestY = 0;

            // Se non abbiamo pinned belief, non abbiamo nulla da pianificare.
            if (!world.NpcPinnedFoodStockBeliefs.TryGetValue(npcId, out var list) || list == null || list.Count == 0)
                return 0;

            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            int bestObj = 0;
            int bestDist = int.MaxValue;

            // IMPORTANTISSIMO:
            // Qui NON consultiamo World.Objects[objId] per scoprire "dove sta davvero lo stock ora".
            // Quello sarebbe telepatia. Usiamo solo la posizione che l'NPC crede essere valida (LastKnownX/Y).
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (!b.IsValid)
                    continue;

                int ox = b.LastKnownX;
                int oy = b.LastKnownY;

                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                if (manhattan < bestDist)
                {
                    bestDist = manhattan;
                    bestObj = b.ObjectId;
                    bestX = ox;
                    bestY = oy;
                }
            }

            return bestObj;
        }

        // =============================================================================
        // ApplyDirectFoodBeliefContradiction
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce il feedback operativo esplicito per il caso in cui un NPC verifica
        /// localmente una cella dove credeva di trovare cibo e scopre che la credenza
        /// non e' piu' valida.
        /// </para>
        ///
        /// <para><b>Ponte provvisorio Rule -> BeliefUpdater</b></para>
        /// <para>
        /// La rule e' il punto che osserva la smentita diretta in questa fase del
        /// progetto, perche' il Job System non esiste ancora. La rule non modifica
        /// direttamente le entry: costruisce un <c>BeliefFailureSignal</c> e delega al
        /// <c>BeliefUpdater</c> la semantica cognitiva dell'invalidazione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Store lookup</b>: usa il BeliefStore per-NPC se gia' presente.</item>
        ///   <item><b>Fallback key</b>: usa categoria <c>Food</c> e posizione stimata, perche' la rule non possiede ancora un <c>BeliefId</c>.</item>
        ///   <item><b>Telemetry</b>: conta feedback applicati o senza match per debug progressivo.</item>
        /// </list>
        /// </summary>
        private void ApplyDirectFoodBeliefContradiction(
            World world,
            int npcId,
            int cellX,
            int cellY,
            int nowTick,
            Telemetry telemetry)
        {
            if (world == null)
                return;

            if (!world.Beliefs.TryGetValue(npcId, out var store) || store == null)
                return;

            var signal = new BeliefFailureSignal(
                npcId: npcId,
                beliefId: 0,
                category: BeliefCategory.Food,
                estimatedPosition: new Vector2Int(cellX, cellY),
                failureKind: BeliefFailureKind.DirectLocalContradiction,
                penalty01: 1f,
                tick: nowTick);

            bool updated = _beliefUpdater.UpdateFromOperationalFailure(signal, store);
            telemetry?.Counter(updated ? "BeliefUpdater.JobFailureDiscarded" : "BeliefUpdater.JobFailureNoMatch", 1);
        }

private static int FindRememberedOtherNpcFoodStock(
    World world,
    int npcId,
    int maxRangeCells,
    out int sx,
    out int sy,
    out int ownerId)
{
    sx = 0;
    sy = 0;
    ownerId = 0;

    if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
        return 0;

    if (!world.NpcObjectMemory.TryGetValue(npcId, out var mem) || mem == null)
        return 0;

    int bestObjId = 0;
    int bestDist = int.MaxValue;
    int bestX = 0;
    int bestY = 0;
    int bestOwner = 0;

    for (int i = 0; i < mem.Slots.Length; i++)
    {
        var e = mem.Slots[i];
        if (!e.IsValid) continue;

        if (e.Kind != NpcObjectMemoryStore.SubjectKind.WorldObject)
            continue;

        // Recuperiamo l'ObjectId in modo robusto (compat: SubjectId e ObjectId possono coincidere).
        int objId = e.SubjectId != 0 ? e.SubjectId : e.ObjectId;
        if (objId == 0) continue;

        if (!world.FoodStocks.TryGetValue(objId, out var st))
            continue;

        if (st.Units <= 0)
            continue;

        // Deve essere privato di un altro NPC.
        if (st.OwnerKind != OwnerKind.Npc) continue;
        if (st.OwnerId <= 0) continue;
        if (st.OwnerId == npcId) continue;

        // Prendiamo la cella reale se l'oggetto esiste in world.Objects, altrimenti la last-known cell in memoria.
        int ox = e.CellX;
        int oy = e.CellY;
        if (world.Objects.TryGetValue(objId, out var inst) && inst != null)
        {
            ox = inst.CellX;
            oy = inst.CellY;
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
            bestOwner = st.OwnerId;
        }
    }

    if (bestObjId != 0)
    {
        sx = bestX;
        sy = bestY;
        ownerId = bestOwner;
    }

    return bestObjId;
}

/// <summary>
/// Step5: cerca nella MEMORIA soggettiva un NPC osservato che (secondo il ricordo)
/// aveva cibo addosso.
///
/// Importante:
/// - questa funzione NON guarda world.NpcPrivateFood per scegliere la vittima.
/// - seleziona la vittima da mem.Slots (Kind=Npc) e flag HasCarriedFood.
///
/// Ritorna:
/// - victimNpcId
/// - out vx/vy = last-known cell della vittima (o last seen)
/// - out carriedApprox = stima (debug/UI), non vincolante.
/// </summary>
private static int FindRememberedNpcWithCarriedFood(
    World world,
    int npcId,
    int maxRangeCells,
    out int vx,
    out int vy,
    out int carriedApprox)
{
    vx = 0;
    vy = 0;
    carriedApprox = 0;

    if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
        return 0;

    if (!world.NpcObjectMemory.TryGetValue(npcId, out var mem) || mem == null)
        return 0;

    int bestVictim = 0;
    int bestDist = int.MaxValue;
    int bestX = 0;
    int bestY = 0;
    int bestApprox = 0;

    for (int i = 0; i < mem.Slots.Length; i++)
    {
        var e = mem.Slots[i];
        if (!e.IsValid) continue;
        if (e.Kind != NpcObjectMemoryStore.SubjectKind.Npc)
            continue;

        int victimId = e.SubjectId;
        if (victimId <= 0) continue;
        if (victimId == npcId) continue;

        // Flag "has carried food" (derivato solo quando l'NPC era visibile: Step4).
        if ((e.Flags & NpcObjectMemoryStore.ObservedFlags.HasCarriedFood) == 0)
            continue;

        // Se la stima è 0, consideriamolo "non interessante" per il furto.
        // (In futuro potresti scegliere di rubare comunque, ma qui usiamo una regola semplice.)
        if (e.CarriedFoodUnitsApprox <= 0)
            continue;

        int ox = e.CellX;
        int oy = e.CellY;

        int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
        if (manhattan > maxRangeCells)
            continue;

        if (manhattan < bestDist)
        {
            bestDist = manhattan;
            bestVictim = victimId;
            bestX = ox;
            bestY = oy;
            bestApprox = e.CarriedFoodUnitsApprox;
        }
    }

    if (bestVictim != 0)
    {
        vx = bestX;
        vy = bestY;
        carriedApprox = bestApprox;
    }

    return bestVictim;
}
// ============================================================
        // SLEEP DECISION
        // ============================================================

        private bool TryPlanSleep(World world, int npcId, NpcNeeds needs, out ICommand cmd, out bool didTrespass)
        {
            cmd = null;
            didTrespass = false;

            // 1) letto community libero (VISIBILE)
            int bedCommunity = FindVisibleBed(world, npcId, OwnerKind.Community, 0, _maxSeekRangeCells);
            if (bedCommunity != 0 && !world.GetUseStateOrDefault(bedCommunity).IsInUse)
            {
                cmd = new SleepInBedCommand(npcId, bedCommunity, "Community");
                return true;
            }

            // 2) letto altrui se moralità/emergenza
            float law = world.Social.TryGetValue(npcId, out var soc) ? soc.JusticePerception01 : 0.5f;
            bool emergency = needs.GetValue(NeedKind.Rest) >= 0.95f;
            bool okToTrespass = emergency || law < 0.45f;

            if (!okToTrespass)
                return false;

            int bedOther = FindAnyOwnedBedNotNpc(world, npcId, _maxSeekRangeCells);
            if (bedOther != 0 && !world.GetUseStateOrDefault(bedOther).IsInUse)
            {
                didTrespass = true;
                cmd = new SleepInBedCommand(npcId, bedOther, "Trespass");
                return true;
            }

            return false;
        }

        private static int FindVisibleBed(World world, int npcId, OwnerKind ownerKind, int ownerId, int maxRangeCells)
        {
            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            foreach (var kv in world.Objects)
            {
                int objId = kv.Key;
                var obj = kv.Value;
                if (obj == null) continue;

                // v0: un letto se defId contiene "bed" (manteniamo la regola originale del file).
                if (string.IsNullOrWhiteSpace(obj.DefId)) continue;
                if (!obj.DefId.Contains("bed")) continue;

                if (obj.OwnerKind != ownerKind || obj.OwnerId != ownerId) continue;

                int ox = obj.CellX;
                int oy = obj.CellY;

                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                if (!world.HasLineOfSight(nx, ny, ox, oy))
                    continue;

                return objId;
            }

            return 0;
        }

        private static int FindAnyOwnedBedNotNpc(World world, int npcId, int maxRangeCells)
        {
            if (!TryGetNpcCell(world, npcId, out int nx, out int ny))
                return 0;

            foreach (var kv in world.Objects)
            {
                int objId = kv.Key;
                var obj = kv.Value;
                if (obj == null) continue;

                // v0: un letto se defId contiene "bed" (manteniamo la regola originale del file).
                if (string.IsNullOrWhiteSpace(obj.DefId)) continue;
                if (!obj.DefId.Contains("bed")) continue;

                // Escludi letti di proprietà dell'NPC.
                if (obj.OwnerKind == OwnerKind.Npc && obj.OwnerId == npcId) continue;

                int ox = obj.CellX;
                int oy = obj.CellY;

                int manhattan = Mathf.Abs(ox - nx) + Mathf.Abs(oy - ny);
                if (manhattan > maxRangeCells)
                    continue;

                if (!world.HasLineOfSight(nx, ny, ox, oy))
                    continue;

                return objId;
            }

            return 0;
        }

        // ============================================================
        // VERY SMALL UTILITIES (deliberatamente verbose)
        // ============================================================

        /// <summary>
        /// Estrae la cella corrente dell'NPC.
        /// In ARCONTIO la posizione runtime degli NPC è in world.GridPos (component store),
        /// NON dentro NpcCore (che è più "identità"/stato logico).
        /// </summary>
        private static bool TryGetNpcCell(World world, int npcId, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (!world.GridPos.TryGetValue(npcId, out var pos))
                return false;

            x = pos.X;
            y = pos.Y;
            return true;
        }

        /// <summary>
        /// Estrae la cella di un oggetto dato il suo objectId.
        /// Nota: questa è la *singola fonte di verità* per posizione oggetti in World:
        /// world.Objects[objId].CellX / CellY.
        ///
        /// (Il FoodStockComponent NON contiene necessariamente coordinate; è un componente logico.)
        /// </summary>
        private static bool TryGetObjectCell(World world, int objectId, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (!world.Objects.TryGetValue(objectId, out var obj) || obj == null)
                return false;

            x = obj.CellX;
            y = obj.CellY;
            return true;
        }
    }
}
