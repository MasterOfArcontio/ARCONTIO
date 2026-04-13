using System.Collections.Generic;
using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MovementExplainabilityEmitter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Adapter one-way che traduce i dati gia' calcolati dal <see cref="MovementSystem"/>
    /// in trace dell'Explainability Layer pathfinding.
    /// </para>
    ///
    /// <para><b>Separazione simulazione / spiegazione</b></para>
    /// <para>
    /// Questo emitter non sceglie path, non interroga pathfinder per ottenere una
    /// scelta alternativa e non restituisce valori al movimento. Riceve soltanto
    /// snapshot gia' disponibili nel punto di pianificazione e li copia nel registry
    /// EL del World. In questo modo la spiegazione resta osservatore passivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryEmitIntentAndPlan</b>: entry point della sessione E.</item>
    ///   <item><b>ShouldEmitForNpc</b>: gate config e filtro NPC tracciati.</item>
    ///   <item><b>BuildIntentTrace</b>: snapshot dell'intent di movimento.</item>
    ///   <item><b>BuildPlanTrace</b>: snapshot della scelta planner iniziale.</item>
    ///   <item><b>Mapper privati</b>: traduzione da tipi runtime a enum EL.</item>
    /// </list>
    /// </summary>
    public static class MovementExplainabilityEmitter
    {
        // =============================================================================
        // TryEmitIntentAndPlan
        // =============================================================================
        /// <summary>
        /// <para>
        /// Emette, se la config lo consente, la coppia di trace iniziali di un nuovo
        /// movimento: <see cref="MovementIntentTrace"/> e <see cref="PathPlanTrace"/>.
        /// </para>
        ///
        /// <para><b>Emissione atomica diagnostica</b></para>
        /// <para>
        /// Intent e piano nascono nello stesso punto perche' oggi il codice elabora il
        /// nuovo <see cref="MoveIntent"/> e sceglie la modalita' iniziale nello stesso
        /// metodo. Se in futuro il decision layer passera' belief/job piu' ricchi,
        /// questa funzione potra' ricevere snapshot aggiuntivi senza cambiare il planner.
        /// </para>
        /// </summary>
        public static void TryEmitIntentAndPlan(
            World world,
            int npcId,
            in MoveIntent intent,
            GridPosition startCell,
            bool targetVisible,
            bool directPathClear,
            PlannerMode selectedMode,
            SelectionReason selectionReason,
            NpcMacroRoutePlan macroPlan,
            IReadOnlyList<Vector2Int> localPath)
        {
            if (!ShouldEmitForNpc(world, npcId, out _, out int verbosity))
                return;

            var registry = world.MovementExplainability;
            if (registry == null)
                return;

            int intentId = registry.AllocateIntentId();
            int planId = registry.AllocatePlanId();

            registry.EmitIntent(BuildIntentTrace(npcId, intent, intentId, verbosity));
            registry.EmitPlan(BuildPlanTrace(
                npcId,
                intent,
                intentId,
                planId,
                startCell,
                targetVisible,
                directPathClear,
                selectedMode,
                selectionReason,
                macroPlan,
                localPath,
                verbosity));
        }

        // =============================================================================
        // ShouldEmitForNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il gate configurabile dell'EL: master switch, verbosity, lista NPC
        /// esplicita e limite massimo di NPC osservati automaticamente.
        /// </para>
        /// </summary>
        private static bool ShouldEmitForNpc(
            World world,
            int npcId,
            out MovementExplainabilityParams config,
            out int verbosity)
        {
            config = world?.Config?.Sim?.explainability;
            verbosity = config != null ? Mathf.Max(0, config.defaultVerbosity) : 0;

            if (world == null || config == null || !config.enabled || verbosity <= 0)
                return false;

            if (config.trackedNpcIds != null && config.trackedNpcIds.Length > 0)
                return ContainsNpcId(config.trackedNpcIds, npcId);

            int maxTracked = Mathf.Max(0, config.maxTrackedNpcs);
            if (maxTracked <= 0)
                return false;

            if (world.MovementExplainability != null
                && world.MovementExplainability.TryGetNpcStore(npcId, out _))
                return true;

            return world.MovementExplainability == null
                   || world.MovementExplainability.StoreCount < maxTracked;
        }

        // =============================================================================
        // BuildIntentTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot EL dell'intent. La funzione copia solo valori gia'
        /// presenti nel <see cref="MoveIntent"/> e non cerca cause a posteriori nel
        /// World o nel BeliefStore.
        /// </para>
        /// </summary>
        private static MovementIntentTrace BuildIntentTrace(
            int npcId,
            in MoveIntent intent,
            int intentId,
            int verbosity)
        {
            return new MovementIntentTrace
            {
                NpcId = npcId,
                Tick = TickContext.CurrentTickIndex,
                IntentId = intentId,
                MovementPurpose = MapPurpose(intent.Reason),
                TargetType = intent.TargetObjectId != 0 ? MovementTargetType.WorldObject : MovementTargetType.Cell,
                TargetCell = new Vector2Int(intent.TargetX, intent.TargetY),
                TargetObjectId = intent.TargetObjectId,
                HasBeliefBasis = false,
                Urgency = 0f,
                VerbosityLevel = verbosity,
            };
        }

        // =============================================================================
        // BuildPlanTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot EL della pianificazione iniziale. I candidati
        /// descrivono cio' che il MovementSystem ha gia' valutato: direct sempre,
        /// landmark solo quando il direct non era selezionabile.
        /// </para>
        /// </summary>
        private static PathPlanTrace BuildPlanTrace(
            int npcId,
            in MoveIntent intent,
            int intentId,
            int planId,
            GridPosition startCell,
            bool targetVisible,
            bool directPathClear,
            PlannerMode selectedMode,
            SelectionReason selectionReason,
            NpcMacroRoutePlan macroPlan,
            IReadOnlyList<Vector2Int> localPath,
            int verbosity)
        {
            var trace = new PathPlanTrace
            {
                NpcId = npcId,
                Tick = TickContext.CurrentTickIndex,
                IntentId = intentId,
                PlanId = planId,
                StartCell = new Vector2Int(startCell.X, startCell.Y),
                GoalCell = new Vector2Int(intent.TargetX, intent.TargetY),
                SelectedMode = selectedMode,
                SelectionReason = selectionReason,
                MacroRouteNodes = macroPlan != null && macroPlan.NodeIds != null && macroPlan.NodeIds.Count > 0
                    ? macroPlan.NodeIds.ToArray()
                    : null,
                MacroRouteCost = macroPlan != null && macroPlan.Succeeded ? Mathf.Max(0, macroPlan.NodeIds.Count - 1) : -1f,
                HasLocalRouteFirstStep = localPath != null && localPath.Count > 1,
                LocalRouteFirstStep = localPath != null && localPath.Count > 1 ? localPath[1] : default,
                VerbosityLevel = verbosity,
            };

            AddDirectCandidate(trace.Candidates, targetVisible, directPathClear, localPath);
            if (!directPathClear || macroPlan != null)
                AddLandmarkCandidate(trace.Candidates, macroPlan);

            if (selectedMode == PlannerMode.DirectFallback)
                AddDirectFallbackCandidate(trace.Candidates, localPath);

            return trace;
        }

        // =============================================================================
        // AddDirectCandidate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge il candidato direct. Questo candidato e' sempre disponibile come
        /// valutazione iniziale nel codice attuale, perche' il planner verifica prima
        /// visibilita'/traversabilita' del target diretto.
        /// </para>
        /// </summary>
        private static void AddDirectCandidate(
            List<PlannerCandidate> candidates,
            bool targetVisible,
            bool directPathClear,
            IReadOnlyList<Vector2Int> localPath)
        {
            candidates.Add(new PlannerCandidate
            {
                Mode = PlannerMode.Direct,
                Valid = directPathClear,
                EstimatedCost = localPath != null && localPath.Count > 0 ? Mathf.Max(0, localPath.Count - 1) : -1f,
                InvalidReason = directPathClear ? InvalidReason.None : MapDirectInvalidReason(targetVisible),
                Note = directPathClear ? "direct_path_clear" : "direct_not_selected",
            });
        }

        // =============================================================================
        // AddLandmarkCandidate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge il candidato landmark quando il codice ha dovuto tentare o
        /// osservare una macro-route. Se non esiste un piano macro, la ragione resta
        /// una diagnostica conservativa.
        /// </para>
        /// </summary>
        private static void AddLandmarkCandidate(List<PlannerCandidate> candidates, NpcMacroRoutePlan macroPlan)
        {
            candidates.Add(new PlannerCandidate
            {
                Mode = PlannerMode.LandmarkAstar,
                Valid = macroPlan != null && macroPlan.Succeeded,
                EstimatedCost = macroPlan != null && macroPlan.Succeeded ? Mathf.Max(0, macroPlan.NodeIds.Count - 1) : -1f,
                InvalidReason = macroPlan != null && macroPlan.Succeeded ? InvalidReason.None : MapMacroInvalidReason(macroPlan),
                Note = macroPlan != null ? macroPlan.FailureReason ?? string.Empty : "macro_not_available",
            });
        }

        // =============================================================================
        // AddDirectFallbackCandidate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge il candidato fallback direct/parziale quando la macro-route non
        /// viene attivata e il sistema prepara soltanto un prefix visuale/diagnostico.
        /// </para>
        /// </summary>
        private static void AddDirectFallbackCandidate(
            List<PlannerCandidate> candidates,
            IReadOnlyList<Vector2Int> localPath)
        {
            candidates.Add(new PlannerCandidate
            {
                Mode = PlannerMode.DirectFallback,
                Valid = localPath != null && localPath.Count > 1,
                EstimatedCost = localPath != null && localPath.Count > 0 ? Mathf.Max(0, localPath.Count - 1) : -1f,
                InvalidReason = localPath != null && localPath.Count > 1 ? InvalidReason.None : InvalidReason.PathBlocked,
                Note = "macro_unavailable_direct_prefix",
            });
        }

        // =============================================================================
        // ContainsNpcId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un NPC nella lista esplicita configurata. La lista e' volutamente un
        /// array JsonUtility-friendly, quindi usiamo una scansione lineare semplice.
        /// </para>
        /// </summary>
        private static bool ContainsNpcId(int[] ids, int npcId)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == npcId)
                    return true;
            }

            return false;
        }

        // =============================================================================
        // MapPurpose
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce la reason runtime in uno scopo EL piu' leggibile. La mappatura non
        /// modifica la reason originale e non viene riletta dal movimento.
        /// </para>
        /// </summary>
        private static MovementPurpose MapPurpose(MoveIntentReason reason)
        {
            return reason switch
            {
                MoveIntentReason.SeekFood => MovementPurpose.ReachFood,
                MoveIntentReason.SeekBed => MovementPurpose.ReachBed,
                MoveIntentReason.Wander => MovementPurpose.Wander,
                MoveIntentReason.SeekTalkTarget => MovementPurpose.Follow,
                MoveIntentReason.DebugClick => MovementPurpose.DebugClick,
                _ => MovementPurpose.Unknown,
            };
        }

        // =============================================================================
        // MapDirectInvalidReason
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce la doppia valutazione direct in una ragione di scarto leggibile.
        /// Se il target non era acquisibile percettivamente, non attribuiamo il rifiuto
        /// alla traversabilita' del path.
        /// </para>
        /// </summary>
        private static InvalidReason MapDirectInvalidReason(bool targetVisible)
        {
            return targetVisible ? InvalidReason.PathBlocked : InvalidReason.TargetNotVisible;
        }

        // =============================================================================
        // MapMacroInvalidReason
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce le stringhe diagnostiche esistenti della macro-route in enum EL.
        /// Mantiene una mappatura conservativa per non inventare cause piu' precise di
        /// quelle gia' prodotte dal planner landmark.
        /// </para>
        /// </summary>
        private static InvalidReason MapMacroInvalidReason(NpcMacroRoutePlan macroPlan)
        {
            string reason = macroPlan?.FailureReason ?? string.Empty;

            return reason switch
            {
                "LandmarkSystemDisabled" => InvalidReason.LandmarkSystemDisabled,
                "NoLandmarkRegistry" => InvalidReason.LandmarkSystemDisabled,
                "NoLandmarkMemory" => InvalidReason.NoKnownLandmarks,
                "InvalidEndpoint" => InvalidReason.NoKnownLandmarks,
                "EndpointNotKnown" => InvalidReason.NoKnownLandmarks,
                "NoMacroRoute" => InvalidReason.LmPlanFailed,
                "NpcHasNoGridPos" => InvalidReason.LmPlanFailed,
                _ => InvalidReason.LmPlanFailed,
            };
        }
    }
}
