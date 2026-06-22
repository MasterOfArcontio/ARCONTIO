using System.Globalization;
using Arcontio.Core;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiInspectorRuntimeSnapshotProvider
    // =============================================================================
    /// <summary>
    /// <para>
    /// Provider read-only che trasforma dati runtime autorizzati in ViewModel
    /// dell'inspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: World -> Snapshot -> UI</b></para>
    /// <para>
    /// Il pannello UGUI non deve leggere il <c>World</c>. Questo provider resta
    /// fuori dalla view e usa solo il <see cref="ArcGraphRuntimeContextProvider"/>
    /// gia' presente nella pipeline ArcGraph. Il risultato esposto e' un
    /// <see cref="ArcUiInspectorViewModel"/> composto da stringhe e righe
    /// read-only, quindi non permette mutazioni dirette della simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SetRuntimeContextProvider</b>: riceve il boundary runtime autorizzato.</item>
    ///   <item><b>TryBuildNpcViewModel</b>: costruisce lo snapshot informativo NPC.</item>
    ///   <item><b>Build*Rows</b>: organizza Info, Stato, DNA, Inventario, EL e Job.</item>
    /// </list>
    /// </summary>
    public sealed class ArcUiInspectorRuntimeSnapshotProvider
    {
        private const string InfoTabKey = "info";
        private const string EmptyValue = "--";

        private ArcGraphRuntimeContextProvider _runtimeContextProvider;
        private readonly MemoryBeliefDecisionExplainabilityViewModel _elModel = new();

        // =============================================================================
        // SetRuntimeContextProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il provider di context runtime usato per leggere snapshot.
        /// </para>
        /// </summary>
        public void SetRuntimeContextProvider(ArcGraphRuntimeContextProvider provider)
        {
            _runtimeContextProvider = provider;
        }

        // =============================================================================
        // TryBuildNpcViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un ViewModel read-only per un NPC selezionato.
        /// </para>
        ///
        /// <para><b>Contratto asciutto</b></para>
        /// <para>
        /// Il metodo non introduce payload generici o parametri futuri. Prende il
        /// target selezionato, legge lo stato minimo oggi disponibile e produce le
        /// tab che il RightInspector puo' gia' mostrare.
        /// </para>
        /// </summary>
        public bool TryBuildNpcViewModel(
            ArcUiSelectionTarget target,
            out ArcUiInspectorViewModel viewModel)
        {
            viewModel = ArcUiInspectorViewModel.Empty();

            if (target.Kind != ArcUiSelectionTargetKind.Npc || !TryParseNpcId(target, out int npcId))
                return false;

            if (!TryGetWorld(out World world) || !world.NpcDna.TryGetValue(npcId, out NpcDnaProfile dna) || dna == null)
                return false;

            world.NpcProfiles.TryGetValue(npcId, out NpcProfile profile);
            world.Needs.TryGetValue(npcId, out NpcNeeds needs);
            world.GridPos.TryGetValue(npcId, out GridPosition position);
            world.NpcFacing.TryGetValue(npcId, out CardinalDirection facing);
            world.TryGetNpcAction(npcId, out NpcActionState action);

            bool hasEl = MemoryBeliefDecisionExplainabilityViewModelBuilder.BuildForNpc(
                world,
                npcId,
                _elModel,
                24,
                MemoryBeliefDecisionViewModelBuildScope.All);

            string title = ResolveNpcTitle(target, dna, npcId);
            var tabs = new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", BuildInfoRows(target, npcId, dna, position, facing, action)),
                new ArcUiInspectorTab("state", "Stato", BuildStateRows(world, npcId, dna, needs, action)),
                new ArcUiInspectorTab("dna", "DNA", BuildDnaRows(dna, profile)),
                new ArcUiInspectorTab("inventory", "Inventario", BuildInventoryRows(world, npcId)),
                new ArcUiInspectorTab("memory", "Memory", BuildMemoryRows(hasEl)),
                new ArcUiInspectorTab("belief", "Belief", BuildBeliefRows(hasEl)),
                new ArcUiInspectorTab("decision", "Decision", BuildDecisionRows(hasEl)),
                new ArcUiInspectorTab("job", "Job", BuildJobRows(world, npcId, hasEl))
            };

            viewModel = new ArcUiInspectorViewModel(title, target, tabs, InfoTabKey);
            return true;
        }

        private bool TryGetWorld(out World world)
        {
            world = null;

            if (_runtimeContextProvider == null)
                return false;

            ArcGraphRuntimeContext context = _runtimeContextProvider.BuildTerrainRuntimeContext();
            world = context?.World;
            return world != null;
        }

        private static ArcUiInspectorRow[] BuildInfoRows(
            ArcUiSelectionTarget target,
            int npcId,
            NpcDnaProfile dna,
            GridPosition position,
            CardinalDirection facing,
            NpcActionState action)
        {
            return new[]
            {
                new ArcUiInspectorRow("Modalita'", "Ispezione"),
                new ArcUiInspectorRow("Tipo", "NPC"),
                new ArcUiInspectorRow("Id", npcId.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Nome", ReadString(dna.Identity.Name, target.DisplayName)),
                new ArcUiInspectorRow("Origine", ReadString(dna.Identity.OriginTag, EmptyValue)),
                new ArcUiInspectorRow("Cella logica", FormatCell(position.X, position.Y, 0)),
                new ArcUiInspectorRow("Direzione", facing.ToString()),
                new ArcUiInspectorRow("Azione", ReadString(action.ToString(), EmptyValue)),
                new ArcUiInspectorRow("Sorgente", ReadString(target.SourceView, EmptyValue))
            };
        }

        private static ArcUiInspectorRow[] BuildStateRows(
            World world,
            int npcId,
            NpcDnaProfile dna,
            NpcNeeds needs,
            NpcActionState action)
        {
            return new[]
            {
                new ArcUiInspectorRow("Tick", world.Global.CurrentTickIndex.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Azione corrente", ReadString(action.ToString(), EmptyValue)),
                new ArcUiInspectorRow("Fame", FormatNeed(needs, NeedKind.Hunger)),
                new ArcUiInspectorRow("Sete", FormatNeed(needs, NeedKind.Thirst)),
                new ArcUiInspectorRow("Riposo", FormatNeed(needs, NeedKind.Rest)),
                new ArcUiInspectorRow("Sicurezza", FormatNeed(needs, NeedKind.Security)),
                new ArcUiInspectorRow("Stabilita'", FormatNeed(needs, NeedKind.Stability)),
                new ArcUiInspectorRow("Socialita'", FormatNeed(needs, NeedKind.Sociality)),
                new ArcUiInspectorRow("Soglia allerta", FormatPercent(dna.Thresholds.NeedAlert01)),
                new ArcUiInspectorRow("Soglia critica", FormatPercent(dna.Thresholds.NeedCritical01))
            };
        }

        private static ArcUiInspectorRow[] BuildDnaRows(
            NpcDnaProfile dna,
            NpcProfile profile)
        {
            return new[]
            {
                new ArcUiInspectorRow("Forza", FormatPercent(dna.Capacities.Strength01)),
                new ArcUiInspectorRow("Resistenza", FormatPercent(dna.Capacities.Endurance01)),
                new ArcUiInspectorRow("Agilita'", FormatPercent(dna.Capacities.Agility01)),
                new ArcUiInspectorRow("Intelligenza", FormatPercent(dna.Capacities.BaseIntelligence01)),
                new ArcUiInspectorRow("Introversione", FormatPercent(dna.Dispositions.Introversion01)),
                new ArcUiInspectorRow("Aggressivita'", FormatPercent(dna.Dispositions.Aggressiveness01)),
                new ArcUiInspectorRow("Curiosita'", FormatPercent(dna.Dispositions.Curiosity01)),
                new ArcUiInspectorRow("Cooperazione", FormatPercent(dna.Dispositions.Cooperativeness01)),
                new ArcUiInspectorRow("Impulsivita'", FormatPercent(dna.CognitiveModulators.Impulsivity01)),
                new ArcUiInspectorRow("Avversione rischio", FormatPercent(dna.CognitiveModulators.RiskAversion01)),
                new ArcUiInspectorRow("Ruolo", profile == null ? EmptyValue : ReadString(profile.AssignedRole, "Nessuno"))
            };
        }

        private static ArcUiInspectorRow[] BuildInventoryRows(World world, int npcId)
        {
            world.NpcPrivateFood.TryGetValue(npcId, out int privateFood);

            return new[]
            {
                new ArcUiInspectorRow("Cibo portato", privateFood.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Capienza max", world.GetInventoryMaxUnits(npcId).ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Spazio usato", world.GetInventoryUsedUnits(npcId).ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Spazio libero", world.GetInventoryFreeCapacity(npcId).ToString(CultureInfo.InvariantCulture))
            };
        }

        private ArcUiInspectorRow[] BuildMemoryRows(bool hasEl)
        {
            return new[]
            {
                new ArcUiInspectorRow("EL disponibile", hasEl ? "Si" : "No"),
                new ArcUiInspectorRow("Trace memory", _elModel.MemoryCount.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Ultimo tick", _elModel.LatestMemory.HasValue ? _elModel.LatestMemory.Tick.ToString(CultureInfo.InvariantCulture) : EmptyValue),
                new ArcUiInspectorRow("Tipo trace", _elModel.LatestMemory.HasValue ? ReadString(_elModel.LatestMemory.TraceType, EmptyValue) : EmptyValue),
                new ArcUiInspectorRow("Evento", _elModel.LatestMemory.HasValue ? ReadString(_elModel.LatestMemory.EventType, EmptyValue) : EmptyValue),
                new ArcUiInspectorRow("Cella", _elModel.LatestMemory.HasValue ? ReadString(_elModel.LatestMemory.Cell, EmptyValue) : EmptyValue),
                new ArcUiInspectorRow("Intensita'", _elModel.LatestMemory.HasValue ? FormatPercent(_elModel.LatestMemory.Intensity01) : EmptyValue),
                new ArcUiInspectorRow("Affidabilita'", _elModel.LatestMemory.HasValue ? FormatPercent(_elModel.LatestMemory.Reliability01) : EmptyValue)
            };
        }

        private ArcUiInspectorRow[] BuildBeliefRows(bool hasEl)
        {
            return new[]
            {
                new ArcUiInspectorRow("EL disponibile", hasEl ? "Si" : "No"),
                new ArcUiInspectorRow("Trace belief", _elModel.BeliefCount.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Belief correnti", _elModel.BeliefRows.Count.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Mutazioni recenti", _elModel.BeliefMutationRows.Count.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Ultima operazione", _elModel.LatestBeliefMutation.HasValue ? ReadString(_elModel.LatestBeliefMutation.Operation, EmptyValue) : EmptyValue),
                new ArcUiInspectorRow("Ultima ragione", _elModel.LatestBeliefMutation.HasValue ? ReadString(_elModel.LatestBeliefMutation.Reason, EmptyValue) : EmptyValue),
                new ArcUiInspectorRow("Query count", _elModel.QueryCount.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Ultima query", _elModel.LatestQuery.HasValue ? ReadString(_elModel.LatestQuery.GoalType, EmptyValue) : EmptyValue)
            };
        }

        private ArcUiInspectorRow[] BuildDecisionRows(bool hasEl)
        {
            return new[]
            {
                new ArcUiInspectorRow("EL disponibile", hasEl ? "Si" : "No"),
                new ArcUiInspectorRow("Trace decision", _elModel.DecisionCount.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Candidati", _elModel.LatestDecision.HasValue ? _elModel.LatestDecision.CandidateCount.ToString(CultureInfo.InvariantCulture) : EmptyValue),
                new ArcUiInspectorRow("Intent selezionato", _elModel.LatestDecision.HasValue ? ReadString(_elModel.LatestDecision.SelectedIntent, EmptyValue) : EmptyValue),
                new ArcUiInspectorRow("Score", _elModel.LatestDecision.HasValue ? FormatDecimal(_elModel.LatestDecision.SelectedScore) : EmptyValue),
                new ArcUiInspectorRow("Audit", _elModel.LatestDecision.HasValue ? (_elModel.LatestDecision.AuditValid ? "Valido" : "Non valido") : EmptyValue),
                new ArcUiInspectorRow("Bridge", _elModel.LatestBridge.HasValue ? ReadString(_elModel.LatestBridge.CommandName, EmptyValue) : EmptyValue),
                new ArcUiInspectorRow("Fallback legacy", _elModel.LatestBridge.HasValue ? (_elModel.LatestBridge.LegacyFallbackUsed ? "Si" : "No") : EmptyValue)
            };
        }

        private ArcUiInspectorRow[] BuildJobRows(
            World world,
            int npcId,
            bool hasEl)
        {
            JobRuntimeSnapshot snapshot = world.JobRuntimeState.GetSnapshot(npcId, (int)world.Global.CurrentTickIndex);

            return new[]
            {
                new ArcUiInspectorRow("EL disponibile", hasEl ? "Si" : "No"),
                new ArcUiInspectorRow("Job attivo", snapshot.HasActiveJob ? "Si" : "No"),
                new ArcUiInspectorRow("Job id", ReadString(snapshot.CurrentJobId, EmptyValue)),
                new ArcUiInspectorRow("Template", ReadString(snapshot.TemplateId, EmptyValue)),
                new ArcUiInspectorRow("Fase", ReadString(snapshot.CurrentPhaseId, EmptyValue)),
                new ArcUiInspectorRow("Azione", ReadString(snapshot.CurrentActionId, EmptyValue)),
                new ArcUiInspectorRow("Target cella", snapshot.HasTargetCell ? FormatCell(snapshot.TargetCell.x, snapshot.TargetCell.y, 0) : EmptyValue),
                new ArcUiInspectorRow("Target oggetto", snapshot.TargetObjectId == 0 ? EmptyValue : snapshot.TargetObjectId.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Stato", snapshot.Status.ToString()),
                new ArcUiInspectorRow("Ultimo fallimento", snapshot.LastFailureReason.ToString()),
                new ArcUiInspectorRow("Trace job", _elModel.JobLifecycleCount.ToString(CultureInfo.InvariantCulture))
            };
        }

        private static bool TryParseNpcId(ArcUiSelectionTarget target, out int npcId)
        {
            return int.TryParse(target.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out npcId) && npcId > 0;
        }

        private static string ResolveNpcTitle(
            ArcUiSelectionTarget target,
            NpcDnaProfile dna,
            int npcId)
        {
            if (!string.IsNullOrWhiteSpace(dna.Identity.Name))
                return dna.Identity.Name.Trim();

            if (!string.IsNullOrWhiteSpace(target.DisplayName))
                return target.DisplayName.Trim();

            return "NPC " + npcId.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatNeed(NpcNeeds needs, NeedKind kind)
        {
            float value = needs.GetValue(kind);
            string suffix = needs.IsCritical(kind) ? " critico" : needs.IsAlert(kind) ? " allerta" : string.Empty;
            return FormatPercent(value) + suffix;
        }

        private static string FormatPercent(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatDecimal(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatCell(int x, int y, int z)
        {
            return "col " + x.ToString(CultureInfo.InvariantCulture)
                + " | riga " + y.ToString(CultureInfo.InvariantCulture)
                + " | z " + z.ToString(CultureInfo.InvariantCulture);
        }

        private static string ReadString(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
