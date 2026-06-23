using System.Collections.Generic;
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
    /// Il pannello UGUI non legge il <c>World</c>. Questo provider usa il
    /// <see cref="ArcGraphRuntimeContextProvider"/> gia' presente nella pipeline e
    /// produce un <see cref="ArcUiInspectorViewModel"/> fatto solo di valori,
    /// righe, barre e liste espandibili gia' preparate.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildInfoRows</b>: unisce Info, Stato e Inventario NPC.</item>
    ///   <item><b>BuildDnaRows</b>: produce barre DNA/profilo.</item>
    ///   <item><b>BuildMemory/Belief/Decision/JobRows</b>: adatta il ViewModel EL-MBQD.</item>
    ///   <item><b>BuildPathRows</b>: mantiene la struttura informativa del vecchio MapGrid.</item>
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
        /// Costruisce il ViewModel read-only del RightInspector per un NPC.
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
            world.TryGetNpcAction(npcId, out NpcActionState action);

            bool hasEl = MemoryBeliefDecisionExplainabilityViewModelBuilder.BuildForNpc(
                world,
                npcId,
                _elModel,
                48,
                MemoryBeliefDecisionViewModelBuildScope.All);

            string title = ResolveNpcTitle(target, dna, npcId);
            var tabs = new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", BuildInfoRows(world, target, npcId, dna, needs, position, action)),
                new ArcUiInspectorTab("dna", "DNA", BuildDnaRows(dna, profile)),
                new ArcUiInspectorTab("memory", "Memory", BuildMemoryRows(hasEl)),
                new ArcUiInspectorTab("belief", "Belief", BuildBeliefRows(hasEl)),
                new ArcUiInspectorTab("decision", "Decision", BuildDecisionRows(hasEl)),
                new ArcUiInspectorTab("job", "Job", BuildJobRows(world, npcId, hasEl)),
                new ArcUiInspectorTab("path", "Path", BuildPathRows(world, npcId))
            };

            viewModel = new ArcUiInspectorViewModel(title, target, tabs, InfoTabKey);
            return true;
        }

        // =============================================================================
        // TryBuildObjectViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il ViewModel read-only del RightInspector per oggetti, muri e
        /// porte usando solo snapshot autorizzati dal World runtime.
        /// </para>
        /// </summary>
        public bool TryBuildObjectViewModel(
            ArcUiSelectionTarget target,
            out ArcUiInspectorViewModel viewModel)
        {
            viewModel = ArcUiInspectorViewModel.Empty();

            if ((target.Kind != ArcUiSelectionTargetKind.Object && target.Kind != ArcUiSelectionTargetKind.Wall)
                || !TryParseObjectId(target, out int objectId))
                return false;

            if (!TryGetWorld(out World world)
                || !world.Objects.TryGetValue(objectId, out WorldObjectInstance instance)
                || instance == null)
                return false;

            world.TryGetObjectDef(instance.DefId, out ObjectDef def);

            bool isDoor = def != null && def.IsDoor;
            bool isWall = target.Kind == ArcUiSelectionTargetKind.Wall
                || (def != null
                    && def.Visual != null
                    && string.Equals(def.Visual.VisualKind, "wall", System.StringComparison.OrdinalIgnoreCase));

            ArcUiInspectorTab[] tabs = isDoor
                ? BuildDoorTabs(world, target, instance, def)
                : isWall
                    ? BuildWallTabs(world, target, instance, def)
                    : BuildObjectTabs(world, target, instance, def);

            viewModel = new ArcUiInspectorViewModel(
                ResolveObjectTitle(target, instance, def),
                target,
                tabs,
                InfoTabKey);
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
            World world,
            ArcUiSelectionTarget target,
            int npcId,
            NpcDnaProfile dna,
            NpcNeeds needs,
            GridPosition position,
            NpcActionState action)
        {
            var rows = new List<ArcUiInspectorRow>(20)
            {
                ArcUiInspectorRow.Section("Stato"),
                ArcUiInspectorRow.Bar(
                    "npc_action_state",
                    "Azione",
                    ReadString(action.ToString(), EmptyValue),
                    1f,
                    ResolveActionSeverity(action.Kind)),
                ArcUiInspectorRow.Section("Bisogni")
            };

            AddNeedBars(rows, dna, needs);
            rows.Add(ArcUiInspectorRow.Section("Cibo"));
            AddFoodMetrics(world, npcId, rows);
            rows.Add(new ArcUiInspectorRow("Sorgente", ReadString(target.SourceView, EmptyValue)));
            return rows.ToArray();
        }

        private static ArcUiInspectorTab[] BuildObjectTabs(
            World world,
            ArcUiSelectionTarget target,
            WorldObjectInstance instance,
            ObjectDef def)
        {
            return new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", BuildObjectInfoRows(target, instance, def)),
                new ArcUiInspectorTab("state", "Stato", BuildObjectStateRows(world, instance, def)),
                new ArcUiInspectorTab("use", "Uso", BuildObjectUseRows(world, instance, def)),
                new ArcUiInspectorTab("storage", "Storage", BuildObjectStorageRows(world, instance, def))
            };
        }

        private static ArcUiInspectorTab[] BuildDoorTabs(
            World world,
            ArcUiSelectionTarget target,
            WorldObjectInstance instance,
            ObjectDef def)
        {
            return new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", BuildObjectInfoRows(target, instance, def)),
                new ArcUiInspectorTab("state", "Stato", BuildObjectStateRows(world, instance, def)),
                new ArcUiInspectorTab("access", "Accesso", BuildDoorAccessRows(instance, def)),
                new ArcUiInspectorTab("material", "Materiale", BuildObjectMaterialRows(instance, def))
            };
        }

        private static ArcUiInspectorTab[] BuildWallTabs(
            World world,
            ArcUiSelectionTarget target,
            WorldObjectInstance instance,
            ObjectDef def)
        {
            return new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", BuildObjectInfoRows(target, instance, def)),
                new ArcUiInspectorTab("material", "Materiale", BuildObjectMaterialRows(instance, def)),
                new ArcUiInspectorTab("connections", "Connessioni", BuildWallConnectionRows(instance, def)),
                new ArcUiInspectorTab("state", "Stato", BuildObjectStateRows(world, instance, def))
            };
        }

        private static ArcUiInspectorRow[] BuildObjectInfoRows(
            ArcUiSelectionTarget target,
            WorldObjectInstance instance,
            ObjectDef def)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Identita'"),
                new ArcUiInspectorRow("Id", instance.ObjectId.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Def", ReadString(instance.DefId, EmptyValue)),
                new ArcUiInspectorRow("Nome", ResolveObjectTitle(target, instance, def)),
                new ArcUiInspectorRow("Cella logica", FormatCell(instance.CellX, instance.CellY, 0)),
                new ArcUiInspectorRow("Footprint", FormatFootprint(def)),
                new ArcUiInspectorRow("Sorgente", ReadString(target.SourceView, EmptyValue)),
                ArcUiInspectorRow.Section("Ownership"),
                new ArcUiInspectorRow("Owner", FormatOwner(instance.OwnerKind, instance.OwnerId)),
                new ArcUiInspectorRow("Trasportato", instance.IsHeld ? "Si" : "No"),
                new ArcUiInspectorRow("Holder NPC", instance.HolderNpcId > 0 ? instance.HolderNpcId.ToString(CultureInfo.InvariantCulture) : EmptyValue)
            };
        }

        private static ArcUiInspectorRow[] BuildObjectStateRows(
            World world,
            WorldObjectInstance instance,
            ObjectDef def)
        {
            ObjectUseState useState = world.GetUseStateOrDefault(instance.ObjectId);
            var rows = new List<ArcUiInspectorRow>(20)
            {
                ArcUiInspectorRow.Section("Runtime"),
                new ArcUiInspectorRow("In uso", useState.IsInUse ? "Si" : "No"),
                new ArcUiInspectorRow("Using NPC", useState.UsingNpcId > 0 ? useState.UsingNpcId.ToString(CultureInfo.InvariantCulture) : EmptyValue),
                new ArcUiInspectorRow("Occupante", instance.OccupantNpcId >= 0 ? instance.OccupantNpcId.ToString(CultureInfo.InvariantCulture) : EmptyValue),
                ArcUiInspectorRow.Section("Regole"),
                new ArcUiInspectorRow("Interagibile", BoolText(def != null && def.IsInteractable)),
                new ArcUiInspectorRow("Occluder", BoolText(def != null && def.IsOccluder)),
                new ArcUiInspectorRow("Blocca movimento", BoolText(def != null && def.BlocksMovement)),
                new ArcUiInspectorRow("Blocca visione", BoolText(def != null && def.BlocksVision)),
                new ArcUiInspectorRow("Vision cost", def == null ? EmptyValue : FormatDecimal(def.VisionCost))
            };

            if (def != null && def.MaxHp > 0)
                rows.Add(new ArcUiInspectorRow("Max HP", def.MaxHp.ToString(CultureInfo.InvariantCulture)));

            if (def != null && def.Hardness > 0f)
                rows.Add(new ArcUiInspectorRow("Hardness", FormatDecimal(def.Hardness)));

            return rows.ToArray();
        }

        private static ArcUiInspectorRow[] BuildObjectUseRows(
            World world,
            WorldObjectInstance instance,
            ObjectDef def)
        {
            ObjectUseState useState = world.GetUseStateOrDefault(instance.ObjectId);
            var rows = new List<ArcUiInspectorRow>(20)
            {
                ArcUiInspectorRow.Section("Uso runtime"),
                new ArcUiInspectorRow("In uso", useState.IsInUse ? "Si" : "No"),
                new ArcUiInspectorRow("NPC utilizzatore", useState.UsingNpcId > 0 ? useState.UsingNpcId.ToString(CultureInfo.InvariantCulture) : EmptyValue),
                new ArcUiInspectorRow("Occupante", instance.OccupantNpcId >= 0 ? instance.OccupantNpcId.ToString(CultureInfo.InvariantCulture) : EmptyValue),
                ArcUiInspectorRow.Section("Proprieta' catalogo")
            };

            AddObjectProperties(rows, def);
            return rows.ToArray();
        }

        private static ArcUiInspectorRow[] BuildObjectStorageRows(
            World world,
            WorldObjectInstance instance,
            ObjectDef def)
        {
            var rows = new List<ArcUiInspectorRow>(16)
            {
                ArcUiInspectorRow.Section("Storage")
            };

            if (world.FoodStocks.TryGetValue(instance.ObjectId, out FoodStockComponent stock))
            {
                rows.Add(ArcUiInspectorRow.IconMetrics(
                    "object_food_stock",
                    "Food stock",
                    new[]
                    {
                        new ArcUiInspectorMetric("food_units", "Unita'", stock.Units.ToString(CultureInfo.InvariantCulture), stock.Units > 0 ? ArcUiInspectorSeverity.Good : ArcUiInspectorSeverity.Muted),
                        new ArcUiInspectorMetric("food_owner", "Owner", FormatOwner(stock.OwnerKind, stock.OwnerId), ArcUiInspectorSeverity.Info)
                    }));
                rows.Add(new ArcUiInspectorRow("Vuoto", stock.IsEmpty ? "Si" : "No"));
            }
            else
            {
                rows.Add(new ArcUiInspectorRow("Food stock", "Assente"));
            }

            rows.Add(ArcUiInspectorRow.Section("Proprieta' catalogo"));
            AddObjectProperties(rows, def);
            return rows.ToArray();
        }

        private static ArcUiInspectorRow[] BuildDoorAccessRows(
            WorldObjectInstance instance,
            ObjectDef def)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Porta"),
                new ArcUiInspectorRow("Aperta", instance.IsOpen ? "Si" : "No"),
                new ArcUiInspectorRow("Bloccata", instance.IsLocked ? "Si" : "No"),
                new ArcUiInspectorRow("Lockable", BoolText(def != null && def.IsLockable)),
                new ArcUiInspectorRow("Key", def == null ? EmptyValue : ReadString(def.KeyId, EmptyValue)),
                ArcUiInspectorRow.Section("Effetto su mappa"),
                new ArcUiInspectorRow("Blocca movimento", BoolText(def != null && def.BlocksMovement && !instance.IsOpen)),
                new ArcUiInspectorRow("Blocca visione", BoolText(def != null && def.BlocksVision && !instance.IsOpen)),
                new ArcUiInspectorRow("Nota", "Read-only: nessun comando porta inviato")
            };
        }

        private static ArcUiInspectorRow[] BuildObjectMaterialRows(
            WorldObjectInstance instance,
            ObjectDef def)
        {
            ObjectVisualDef visual = def == null ? null : def.Visual;
            return new[]
            {
                ArcUiInspectorRow.Section("Definizione"),
                new ArcUiInspectorRow("Def", ReadString(instance.DefId, EmptyValue)),
                new ArcUiInspectorRow("Nome catalogo", def == null ? EmptyValue : ReadString(def.DisplayName, EmptyValue)),
                new ArcUiInspectorRow("Sprite", def == null ? EmptyValue : ReadString(def.ResolveArcGraphSpritePath(), EmptyValue)),
                ArcUiInspectorRow.Section("Visual"),
                new ArcUiInspectorRow("Kind", visual == null ? EmptyValue : ReadString(visual.VisualKind, EmptyValue)),
                new ArcUiInspectorRow("Resolver", visual == null ? EmptyValue : ReadString(visual.ResolverKey, EmptyValue)),
                new ArcUiInspectorRow("Dimensione", visual == null ? EmptyValue : FormatSize(visual.WidthPixels, visual.HeightPixels)),
                new ArcUiInspectorRow("Base", visual == null ? EmptyValue : FormatSize(visual.BaseWidthPixels, visual.BaseHeightPixels)),
                new ArcUiInspectorRow("Pivot", visual == null ? EmptyValue : ReadString(visual.Pivot, EmptyValue)),
                new ArcUiInspectorRow("Offset", visual == null ? EmptyValue : FormatOffset(visual.OffsetX, visual.OffsetY))
            };
        }

        private static ArcUiInspectorRow[] BuildWallConnectionRows(
            WorldObjectInstance instance,
            ObjectDef def)
        {
            ObjectVisualDef visual = def == null ? null : def.Visual;
            return new[]
            {
                ArcUiInspectorRow.Section("Connessioni visuali"),
                new ArcUiInspectorRow("Resolver", visual == null ? EmptyValue : ReadString(visual.ResolverKey, EmptyValue)),
                new ArcUiInspectorRow("Mini-tile base", visual == null ? EmptyValue : ReadString(visual.BaseMiniTileMask, EmptyValue)),
                new ArcUiInspectorRow("Footprint", FormatFootprint(def)),
                new ArcUiInspectorRow("Cella base", FormatCell(instance.CellX, instance.CellY, 0)),
                new ArcUiInspectorRow("Nota", "Connessioni cardinali calcolate dal renderer")
            };
        }

        private static void AddNeedBars(
            List<ArcUiInspectorRow> rows,
            NpcDnaProfile dna,
            NpcNeeds needs)
        {
            AddNeedBar(rows, "Fame", NeedKind.Hunger, dna, needs);
            AddNeedBar(rows, "Sete", NeedKind.Thirst, dna, needs);
            AddNeedBar(rows, "Riposo", NeedKind.Rest, dna, needs);
            AddNeedBar(rows, "Salute", NeedKind.Health, dna, needs);
            AddNeedBar(rows, "Comfort", NeedKind.Comfort, dna, needs);
            AddNeedBar(rows, "Sicurezza", NeedKind.Security, dna, needs);
            AddNeedBar(rows, "Stabilita'", NeedKind.Stability, dna, needs);
            AddNeedBar(rows, "Socialita'", NeedKind.Sociality, dna, needs);
        }

        private static void AddNeedBar(
            List<ArcUiInspectorRow> rows,
            string label,
            NeedKind kind,
            NpcDnaProfile dna,
            NpcNeeds needs)
        {
            float needValue = Mathf.Clamp01(needs.GetValue(kind));
            float wellness = 1f - needValue;
            float alertMarker = 1f - Mathf.Clamp01(dna.Thresholds.NeedAlert01);
            float criticalMarker = 1f - Mathf.Clamp01(dna.Thresholds.NeedCritical01);
            ArcUiInspectorSeverity severity = needValue >= dna.Thresholds.NeedCritical01
                ? ArcUiInspectorSeverity.Danger
                : needValue >= dna.Thresholds.NeedAlert01
                    ? ArcUiInspectorSeverity.Warning
                    : ArcUiInspectorSeverity.Good;

            rows.Add(ArcUiInspectorRow.Bar(
                "need_" + kind,
                label,
                FormatPercent(wellness),
                wellness,
                severity,
                alertMarker,
                criticalMarker));
        }

        private static void AddFoodMetrics(
            World world,
            int npcId,
            List<ArcUiInspectorRow> rows)
        {
            int carriedFood = world.NpcPrivateFood.TryGetValue(npcId, out int privateFood)
                ? privateFood
                : 0;

            int ownedStockUnits = 0;
            if (world.FoodStocks != null)
            {
                foreach (var pair in world.FoodStocks)
                {
                    FoodStockComponent stock = pair.Value;
                    if (stock.OwnerKind == OwnerKind.Npc && stock.OwnerId == npcId && stock.Units > 0)
                        ownedStockUnits += stock.Units;
                }
            }

            ComputeFoodTargetDebug(world, npcId, out int visibleCommunityFoodObjId, out int rememberedCommunityFoodObjId);

            rows.Add(ArcUiInspectorRow.IconMetrics(
                "npc_private_food",
                "Cibo privato",
                new[]
                {
                    new ArcUiInspectorMetric("food_carried", "Portato", carriedFood.ToString(CultureInfo.InvariantCulture)),
                    new ArcUiInspectorMetric("food_owned_world", "A terra", ownedStockUnits.ToString(CultureInfo.InvariantCulture)),
                    new ArcUiInspectorMetric("food_total", "Totale", (carriedFood + ownedStockUnits).ToString(CultureInfo.InvariantCulture), ArcUiInspectorSeverity.Good)
                }));

            rows.Add(ArcUiInspectorRow.IconMetrics(
                "npc_community_food",
                "Cibo comunitario",
                new[]
                {
                    new ArcUiInspectorMetric("food_visible", "Visibile", visibleCommunityFoodObjId.ToString(CultureInfo.InvariantCulture), visibleCommunityFoodObjId > 0 ? ArcUiInspectorSeverity.Good : ArcUiInspectorSeverity.Muted),
                    new ArcUiInspectorMetric("food_remembered", "Ricordato", rememberedCommunityFoodObjId.ToString(CultureInfo.InvariantCulture), rememberedCommunityFoodObjId > 0 ? ArcUiInspectorSeverity.Info : ArcUiInspectorSeverity.Muted)
                }));
        }

        private static ArcUiInspectorRow[] BuildDnaRows(
            NpcDnaProfile dna,
            NpcProfile profile)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Capacita'"),
                DnaBar("dna_strength", "Forza", dna.Capacities.Strength01),
                DnaBar("dna_endurance", "Resistenza", dna.Capacities.Endurance01),
                DnaBar("dna_agility", "Agilita'", dna.Capacities.Agility01),
                DnaBar("dna_intelligence", "Intelligenza", dna.Capacities.BaseIntelligence01),
                ArcUiInspectorRow.Section("Disposizioni"),
                DnaBar("dna_introversion", "Introversione", dna.Dispositions.Introversion01),
                DnaBar("dna_aggressiveness", "Aggressivita'", dna.Dispositions.Aggressiveness01),
                DnaBar("dna_curiosity", "Curiosita'", dna.Dispositions.Curiosity01),
                DnaBar("dna_cooperation", "Cooperazione", dna.Dispositions.Cooperativeness01),
                ArcUiInspectorRow.Section("Modulatori"),
                DnaBar("dna_impulsivity", "Impulsivita'", dna.CognitiveModulators.Impulsivity01),
                DnaBar("dna_risk", "Avversione rischio", dna.CognitiveModulators.RiskAversion01),
                new ArcUiInspectorRow("Ruolo", profile == null ? EmptyValue : ReadString(profile.AssignedRole, "Nessuno")),
                new ArcUiInspectorRow("Origine", ReadString(dna.Identity.OriginTag, EmptyValue))
            };
        }

        private ArcUiInspectorRow[] BuildMemoryRows(bool hasEl)
        {
            var rows = new List<ArcUiInspectorRow>(24)
            {
                ArcUiInspectorRow.Section("Trace recenti"),
                new ArcUiInspectorRow("EL disponibile", hasEl ? "Si" : "No"),
                new ArcUiInspectorRow("Trace totali", _elModel.MemoryCount.ToString(CultureInfo.InvariantCulture))
            };

            if (_elModel.LatestMemory.HasValue)
            {
                MemoryBeliefDecisionMemoryView memory = _elModel.LatestMemory;
                rows.Add(ArcUiInspectorRow.Expandable(
                    "memory_latest_" + memory.Tick,
                    "t" + memory.Tick.ToString(CultureInfo.InvariantCulture),
                    ReadString(memory.TraceType, EmptyValue),
                    "+",
                    ArcUiInspectorSeverity.Info,
                    false,
                    new[]
                    {
                        new ArcUiInspectorRow("Soggetto principale", memory.SubjectId.ToString(CultureInfo.InvariantCulture)),
                        new ArcUiInspectorRow("Soggetto secondario", memory.SecondarySubjectId.ToString(CultureInfo.InvariantCulture)),
                        new ArcUiInspectorRow("Posizione", ReadString(memory.Cell, EmptyValue)),
                        ArcUiInspectorRow.Bar("memory_intensity_" + memory.Tick, "Intensita'", FormatPercent(memory.Intensity01), memory.Intensity01, ArcUiInspectorSeverity.Info),
                        ArcUiInspectorRow.Bar("memory_reliability_" + memory.Tick, "Affidabilita'", FormatPercent(memory.Reliability01), memory.Reliability01, ArcUiInspectorSeverity.Good),
                        new ArcUiInspectorRow("Heard", memory.IsHeard ? "Si" : "No"),
                        new ArcUiInspectorRow("Store result", ReadString(memory.StoreResult, EmptyValue))
                    }));
            }
            else
            {
                rows.Add(new ArcUiInspectorRow("Trace", "Nessuna trace dettagliata"));
            }

            rows.Add(ArcUiInspectorRow.Section("Timeline memory"));
            AddTimelineRows(rows, "memory_timeline", onlyMemory: true);
            return rows.ToArray();
        }

        private ArcUiInspectorRow[] BuildBeliefRows(bool hasEl)
        {
            var rows = new List<ArcUiInspectorRow>(32)
            {
                ArcUiInspectorRow.Section("Belief correnti"),
                new ArcUiInspectorRow("EL disponibile", hasEl ? "Si" : "No")
            };

            if (_elModel.BeliefRows.Count == 0)
            {
                rows.Add(new ArcUiInspectorRow("Belief", "Nessun belief corrente"));
            }
            else
            {
                for (int i = 0; i < _elModel.BeliefRows.Count; i++)
                {
                    MemoryBeliefDecisionBeliefView belief = _elModel.BeliefRows[i];
                    rows.Add(BuildBeliefExpandable("belief_current_" + belief.BeliefId, belief));
                }
            }

            rows.Add(ArcUiInspectorRow.Section("Ultima query"));
            AddQueryRows(rows);
            return rows.ToArray();
        }

        private ArcUiInspectorRow[] BuildDecisionRows(bool hasEl)
        {
            var rows = new List<ArcUiInspectorRow>(48)
            {
                ArcUiInspectorRow.Section("Candidati")
            };

            MemoryBeliefDecisionDecisionView decision = _elModel.LatestDecision;
            if (!hasEl || !decision.HasValue)
            {
                rows.Add(new ArcUiInspectorRow("Decisione", "Nessun candidato decisionale"));
            }
            else
            {
                rows.Add(ArcUiInspectorRow.IconMetrics(
                    "decision_header",
                    "Decisione",
                    new[]
                    {
                        new ArcUiInspectorMetric("tick", "Tick", decision.Tick.ToString(CultureInfo.InvariantCulture)),
                        new ArcUiInspectorMetric("noise", "Noise", FormatDecimal(decision.SelectionNoise01)),
                        new ArcUiInspectorMetric("impulse", "Impulsivita'", FormatDecimal(decision.Impulsivity01))
                    }));

                for (int i = 0; i < decision.Candidates.Count; i++)
                {
                    MemoryBeliefDecisionCandidateView candidate = decision.Candidates[i];
                    rows.Add(BuildDecisionCandidateRow(i, candidate));
                }
            }

            rows.Add(ArcUiInspectorRow.Section("Decisioni vincitrici recenti"));
            if (_elModel.IntentOutcomeRows.Count == 0)
            {
                rows.Add(new ArcUiInspectorRow("Recenti", "Nessuna decisione vincitrice recente"));
            }
            else
            {
                for (int i = 0; i < _elModel.IntentOutcomeRows.Count; i++)
                {
                    MemoryBeliefDecisionIntentOutcomeView outcome = _elModel.IntentOutcomeRows[i];
                    rows.Add(ArcUiInspectorRow.Timeline(
                        "decision_outcome_" + i + "_" + outcome.Tick,
                        "t" + outcome.Tick.ToString(CultureInfo.InvariantCulture),
                        ReadString(outcome.Intent, EmptyValue),
                        ConvertSeverity(outcome.ColorRole)));
                }
            }

            return rows.ToArray();
        }

        private ArcUiInspectorRow[] BuildJobRows(
            World world,
            int npcId,
            bool hasEl)
        {
            JobRuntimeSnapshot snapshot = world.JobRuntimeState.GetSnapshot(npcId, (int)world.Global.CurrentTickIndex);
            var rows = new List<ArcUiInspectorRow>(40)
            {
                ArcUiInspectorRow.Section("Job attivo")
            };

            if (!snapshot.HasActiveJob)
            {
                rows.Add(new ArcUiInspectorRow("Job", "Nessun job attivo"));
                return rows.ToArray();
            }

            rows.Add(ArcUiInspectorRow.Expandable(
                "job_active_" + snapshot.CurrentJobId,
                ReadString(snapshot.CurrentJobId, EmptyValue),
                ReadString(snapshot.TemplateId, "Job"),
                "+",
                ArcUiInspectorSeverity.Good,
                true,
                BuildActiveJobDetails(snapshot)));

            rows.Add(ArcUiInspectorRow.Section("Trace job"));
            AddJobTraceRows(rows);
            return rows.ToArray();
        }

        private ArcUiInspectorRow[] BuildPathRows(World world, int npcId)
        {
            var rows = new List<ArcUiInspectorRow>(32)
            {
                ArcUiInspectorRow.Section("Runtime pathfinding")
            };

            if (world.TryGetNpcMacroRouteDebugReport(npcId, out var routeReport))
            {
                rows.Add(new ArcUiInspectorRow("Navigation mode", routeReport.NavigationMode.ToString()));
                rows.Add(new ArcUiInspectorRow("Execution active", routeReport.ExecutionActive ? "Si" : "No"));
                rows.Add(new ArcUiInspectorRow("Target cell", FormatCell(routeReport.TargetCellX, routeReport.TargetCellY, 0)));
                rows.Add(new ArcUiInspectorRow("Immediate target", FormatCell(routeReport.ImmediateTargetX, routeReport.ImmediateTargetY, 0)));
                rows.Add(new ArcUiInspectorRow("Mode switch tick", routeReport.LastModeSwitchTick.ToString(CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Mode switch reason", ReadString(routeReport.LastModeSwitchReason, EmptyValue)));
                rows.Add(ArcUiInspectorRow.Section("Macro route"));
                rows.Add(new ArcUiInspectorRow("Macro route", routeReport.HasRoute ? "OK" : "FAIL"));
                rows.Add(new ArcUiInspectorRow("Route nodes", routeReport.RouteNodeCount.ToString(CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Start landmark", routeReport.StartNodeId.ToString(CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Target landmark", routeReport.TargetNodeId.ToString(CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Next landmark", routeReport.NextRouteNodeId.ToString(CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Last mile", routeReport.IsDoingLastMile ? "Si" : "No"));
                rows.Add(new ArcUiInspectorRow("Local search budget", routeReport.GoalLocalSearchBudgetRemaining.ToString(CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Route failure", ReadString(routeReport.FailureReason, EmptyValue)));
                rows.Add(new ArcUiInspectorRow("Execution failure", ReadString(routeReport.ExecutionFailureReason, EmptyValue)));
            }
            else
            {
                rows.Add(new ArcUiInspectorRow("Pathfinding", "Nessun macro route report"));
            }

            if (world.TryGetNpcLandmarkDebugReport(npcId, out var lmReport))
            {
                rows.Add(ArcUiInspectorRow.Section("Landmark knowledge"));
                rows.Add(new ArcUiInspectorRow("Known landmarks", lmReport.KnownLandmarksCount.ToString(CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Known edges", lmReport.KnownEdgesCount.ToString(CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Poi anchors", lmReport.PoiAnchorCount.ToString(CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Replans/min", lmReport.ReplansPerMin.ToString("0.0", CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Failures/min", lmReport.FailuresPerMin.ToString("0.0", CultureInfo.InvariantCulture)));
                rows.Add(new ArcUiInspectorRow("Blacklist", lmReport.BlacklistSize.ToString(CultureInfo.InvariantCulture)));
            }

            rows.Add(ArcUiInspectorRow.Section("Storico recente"));
            AddTimelineRows(rows, "path_timeline", onlyMemory: false);
            return rows.ToArray();
        }

        private static ArcUiInspectorRow DnaBar(string rowKey, string label, float value01)
        {
            return ArcUiInspectorRow.Bar(
                rowKey,
                label,
                FormatPercent(value01),
                value01,
                ArcUiInspectorSeverity.Info);
        }

        private static ArcUiInspectorRow BuildBeliefExpandable(
            string rowKey,
            MemoryBeliefDecisionBeliefView belief)
        {
            return ArcUiInspectorRow.Expandable(
                rowKey,
                "#" + belief.BeliefId.ToString(CultureInfo.InvariantCulture) + " | " + ReadString(belief.Category, EmptyValue),
                "src " + belief.SourceCount.ToString(CultureInfo.InvariantCulture),
                "+",
                ConvertSeverity(belief.ColorRole),
                false,
                new[]
                {
                    new ArcUiInspectorRow("Status", ReadString(belief.Status, EmptyValue)),
                    new ArcUiInspectorRow("Source", ReadString(belief.Source, EmptyValue)),
                    new ArcUiInspectorRow("Estimated cell", ReadString(belief.EstimatedCell, EmptyValue)),
                    new ArcUiInspectorRow("Subject id", belief.SubjectId.ToString(CultureInfo.InvariantCulture)),
                    ArcUiInspectorRow.Bar(rowKey + "_confidence", "Confidence", FormatPercent(belief.Confidence), belief.Confidence, ArcUiInspectorSeverity.Good),
                    ArcUiInspectorRow.Bar(rowKey + "_freshness", "Freshness", FormatPercent(belief.Freshness), belief.Freshness, ArcUiInspectorSeverity.Info)
                });
        }

        private void AddQueryRows(List<ArcUiInspectorRow> rows)
        {
            MemoryBeliefDecisionQueryView query = _elModel.LatestQuery;
            if (!query.HasValue)
            {
                rows.Add(new ArcUiInspectorRow("Query", "Nessuna query"));
                return;
            }

            rows.Add(new ArcUiInspectorRow("Tick", query.Tick.ToString(CultureInfo.InvariantCulture)));
            rows.Add(new ArcUiInspectorRow("Goal type", ReadString(query.GoalType, EmptyValue)));
            rows.Add(ArcUiInspectorRow.Bar("query_urgency", "Urgency", FormatPercent(query.Urgency01), query.Urgency01, ArcUiInspectorSeverity.Warning));
            rows.Add(new ArcUiInspectorRow("NPC cell", ReadString(query.NpcCell, EmptyValue)));
            rows.Add(ArcUiInspectorRow.Bar("query_min_confidence", "Min confidence", FormatPercent(query.MinConfidence), query.MinConfidence, ArcUiInspectorSeverity.Info));
            rows.Add(new ArcUiInspectorRow("Candidate count", query.CandidateCount.ToString(CultureInfo.InvariantCulture)));
            rows.Add(new ArcUiInspectorRow("Usable candidate", query.UsableCandidateCount.ToString(CultureInfo.InvariantCulture)));
            rows.Add(new ArcUiInspectorRow("Empty", query.IsEmpty ? "Si" : "No"));
            rows.Add(new ArcUiInspectorRow("Empty reason", ReadString(query.EmptyReason, EmptyValue)));
            rows.Add(new ArcUiInspectorRow("Winner", FormatBeliefInline(query.Winner)));
            rows.Add(new ArcUiInspectorRow("Final score", FormatDecimal(query.FinalScore)));

            if (query.Contributions.Count > 0)
            {
                rows.Add(ArcUiInspectorRow.Section("Contributions"));
                AddContributionRows(rows, "query_contribution", query.Contributions);
            }
        }

        private static ArcUiInspectorRow BuildDecisionCandidateRow(
            int index,
            MemoryBeliefDecisionCandidateView candidate)
        {
            var details = new List<ArcUiInspectorRow>(8)
            {
                new ArcUiInspectorRow("Score", FormatDecimal(candidate.Score)),
                new ArcUiInspectorRow("Bisogno", ReadString(candidate.Need, EmptyValue)),
                new ArcUiInspectorRow("Filtered reason", ReadString(candidate.FilteredReason, EmptyValue)),
                new ArcUiInspectorRow("Belief target", FormatBeliefInline(candidate.Belief))
            };

            AddContributionRows(details, "candidate_" + index + "_score", candidate.Contributions);

            return ArcUiInspectorRow.Expandable(
                "decision_candidate_" + index + "_" + candidate.Intent,
                ReadString(candidate.Intent, EmptyValue),
                FormatDecimal(candidate.Score),
                "+",
                candidate.IsSelected ? ArcUiInspectorSeverity.Good : ArcUiInspectorSeverity.Muted,
                candidate.IsSelected,
                details.ToArray());
        }

        private static ArcUiInspectorRow[] BuildActiveJobDetails(JobRuntimeSnapshot snapshot)
        {
            var details = new List<ArcUiInspectorRow>(12)
            {
                new ArcUiInspectorRow("Template", ReadString(snapshot.TemplateId, EmptyValue)),
                new ArcUiInspectorRow("Target cell", snapshot.HasTargetCell ? FormatCell(snapshot.TargetCell.x, snapshot.TargetCell.y, 0) : EmptyValue),
                new ArcUiInspectorRow("Target object", snapshot.TargetObjectId == 0 ? EmptyValue : snapshot.TargetObjectId.ToString(CultureInfo.InvariantCulture)),
                ArcUiInspectorRow.Section("Fase corrente"),
                ArcUiInspectorRow.Expandable(
                    "job_phase_current_" + snapshot.CurrentPhaseId,
                    ReadString(snapshot.CurrentPhaseId, "Fase corrente"),
                    ReadString(snapshot.CurrentActionId, EmptyValue),
                    "+",
                    ArcUiInspectorSeverity.Good,
                    true,
                    new[]
                    {
                        new ArcUiInspectorRow("Step corrente", ReadString(snapshot.CurrentActionId, EmptyValue)),
                        new ArcUiInspectorRow("Stato", snapshot.Status.ToString()),
                        new ArcUiInspectorRow("Ultimo fallimento", snapshot.LastFailureReason.ToString()),
                        new ArcUiInspectorRow("Elapsed ticks", snapshot.ElapsedTicks.ToString(CultureInfo.InvariantCulture))
                    })
            };

            return details.ToArray();
        }

        private void AddJobTraceRows(List<ArcUiInspectorRow> rows)
        {
            if (_elModel.LatestJobRequest.HasValue)
            {
                MemoryBeliefDecisionJobRequestView request = _elModel.LatestJobRequest;
                rows.Add(ArcUiInspectorRow.Expandable(
                    "job_request_" + request.RequestId,
                    "Request " + ReadString(request.RequestId, EmptyValue),
                    ReadString(request.Intent, EmptyValue),
                    "+",
                    ArcUiInspectorSeverity.Info,
                    false,
                    new[]
                    {
                        new ArcUiInspectorRow("Intent", ReadString(request.Intent, EmptyValue)),
                        new ArcUiInspectorRow("Priority", ReadString(request.PriorityClass, EmptyValue)),
                        ArcUiInspectorRow.Bar("job_request_urgency", "Urgency", FormatPercent(request.Urgency01), request.Urgency01, ArcUiInspectorSeverity.Warning),
                        new ArcUiInspectorRow("Target cell", ReadString(request.TargetCell, EmptyValue)),
                        new ArcUiInspectorRow("Target object", request.TargetObjectId == 0 ? EmptyValue : request.TargetObjectId.ToString(CultureInfo.InvariantCulture)),
                        new ArcUiInspectorRow("Reason", ReadString(request.Reason, EmptyValue))
                    }));
            }

            if (_elModel.LatestJobPhase.HasValue)
                rows.Add(new ArcUiInspectorRow("Ultima fase EL", ReadString(_elModel.LatestJobPhase.Phase.DisplayName, EmptyValue)));

            if (_elModel.LatestStep.HasValue)
                rows.Add(new ArcUiInspectorRow("Ultimo step EL", ReadString(_elModel.LatestStep.Step.Label, EmptyValue)));

            if (_elModel.LatestReservation.HasValue)
                rows.Add(new ArcUiInspectorRow("Reservation", ReadString(_elModel.LatestReservation.Operation, EmptyValue) + " " + ReadString(_elModel.LatestReservation.TargetCell, EmptyValue)));

            if (_elModel.LatestCommand.HasValue)
                rows.Add(new ArcUiInspectorRow("Command", ReadString(_elModel.LatestCommand.CommandName, EmptyValue)));

            if (_elModel.LatestFailureLearning.HasValue)
                rows.Add(new ArcUiInspectorRow("Failure learning", ReadString(_elModel.LatestFailureLearning.FailureReason, EmptyValue)));
        }

        private void AddTimelineRows(
            List<ArcUiInspectorRow> rows,
            string keyPrefix,
            bool onlyMemory)
        {
            int added = 0;
            for (int i = 0; i < _elModel.Timeline.Count; i++)
            {
                MemoryBeliefDecisionTimelineView row = _elModel.Timeline[i];
                if (onlyMemory && !string.Equals(row.Kind, "Memory", System.StringComparison.Ordinal))
                    continue;

                rows.Add(ArcUiInspectorRow.Timeline(
                    keyPrefix + "_" + i + "_" + row.Tick,
                    "t" + row.Tick.ToString(CultureInfo.InvariantCulture),
                    ReadString(row.Kind, EmptyValue) + " | " + ReadString(row.Summary, EmptyValue),
                    ConvertSeverity(row.ColorRole)));
                added++;
            }

            if (added == 0)
                rows.Add(new ArcUiInspectorRow("Timeline", "Nessun evento recente"));
        }

        private static void AddContributionRows(
            List<ArcUiInspectorRow> rows,
            string keyPrefix,
            IList<MemoryBeliefDecisionContributionView> contributions)
        {
            if (contributions == null || contributions.Count == 0)
                return;

            for (int i = 0; i < contributions.Count; i++)
            {
                MemoryBeliefDecisionContributionView contribution = contributions[i];
                rows.Add(ArcUiInspectorRow.Bar(
                    keyPrefix + "_" + i,
                    ReadString(contribution.Label, "Score"),
                    FormatSignedDecimal(contribution.Value),
                    Mathf.Clamp01(Mathf.Abs(contribution.Value)),
                    ConvertSeverity(contribution.ColorRole)));
            }
        }

        private static void ComputeFoodTargetDebug(
            World world,
            int npcId,
            out int visibleCommunityFoodObjId,
            out int rememberedCommunityFoodObjId)
        {
            visibleCommunityFoodObjId = 0;
            rememberedCommunityFoodObjId = 0;

            if (!world.GridPos.TryGetValue(npcId, out GridPosition npcPos))
                return;

            if (world.NpcObjectMemory.TryGetValue(npcId, out NpcObjectMemoryStore store) && store != null)
            {
                var slots = store.Slots;
                for (int i = 0; i < slots.Length; i++)
                {
                    var entry = slots[i];
                    if (!entry.IsValid || entry.Kind != NpcObjectMemoryStore.SubjectKind.WorldObject)
                        continue;

                    int objectId = entry.SubjectId != 0 ? entry.SubjectId : entry.ObjectId;
                    if (objectId == 0 || !world.FoodStocks.TryGetValue(objectId, out FoodStockComponent stock))
                        continue;

                    if (stock.Units <= 0 || stock.OwnerKind != OwnerKind.Community || stock.OwnerId != 0)
                        continue;

                    rememberedCommunityFoodObjId = objectId;
                    int objectX = entry.CellX;
                    int objectY = entry.CellY;
                    if (world.Objects.TryGetValue(objectId, out WorldObjectInstance instance) && instance != null)
                    {
                        objectX = instance.CellX;
                        objectY = instance.CellY;
                    }

                    if (world.HasLineOfSight(npcPos.X, npcPos.Y, objectX, objectY))
                    {
                        visibleCommunityFoodObjId = objectId;
                        return;
                    }
                }
            }

            foreach (var pair in world.FoodStocks)
            {
                int objectId = pair.Key;
                FoodStockComponent stock = pair.Value;
                if (stock.Units <= 0 || stock.OwnerKind != OwnerKind.Community || stock.OwnerId != 0)
                    continue;

                if (!world.Objects.TryGetValue(objectId, out WorldObjectInstance instance) || instance == null)
                    continue;

                if (!world.HasLineOfSight(npcPos.X, npcPos.Y, instance.CellX, instance.CellY))
                    continue;

                visibleCommunityFoodObjId = objectId;
                return;
            }
        }

        private static bool TryParseNpcId(ArcUiSelectionTarget target, out int npcId)
        {
            return int.TryParse(target.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out npcId) && npcId > 0;
        }

        private static bool TryParseObjectId(ArcUiSelectionTarget target, out int objectId)
        {
            return int.TryParse(target.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out objectId) && objectId > 0;
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

        private static string ResolveObjectTitle(
            ArcUiSelectionTarget target,
            WorldObjectInstance instance,
            ObjectDef def)
        {
            if (def != null && !string.IsNullOrWhiteSpace(def.DisplayName))
                return def.DisplayName.Trim();

            if (!string.IsNullOrWhiteSpace(target.DisplayName))
                return target.DisplayName.Trim();

            if (!string.IsNullOrWhiteSpace(instance.DefId))
                return instance.DefId.Trim() + " #" + instance.ObjectId.ToString(CultureInfo.InvariantCulture);

            return "Oggetto " + instance.ObjectId.ToString(CultureInfo.InvariantCulture);
        }

        private static void AddObjectProperties(
            List<ArcUiInspectorRow> rows,
            ObjectDef def)
        {
            if (def == null || def.Properties == null || def.Properties.Count == 0)
            {
                rows.Add(new ArcUiInspectorRow("Proprieta'", "Nessuna"));
                return;
            }

            for (int i = 0; i < def.Properties.Count; i++)
            {
                ObjectPropertyKV property = def.Properties[i];
                rows.Add(new ArcUiInspectorRow(
                    ReadString(property.Key, "Property"),
                    FormatDecimal(property.Value)));
            }
        }

        private static string FormatOwner(OwnerKind ownerKind, int ownerId)
        {
            if (ownerKind == OwnerKind.None)
                return "None";

            return ownerKind + ":" + ownerId.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatFootprint(ObjectDef def)
        {
            if (def == null)
                return EmptyValue;

            int width = def.FootprintWidth <= 0 ? 1 : def.FootprintWidth;
            int height = def.FootprintHeight <= 0 ? 1 : def.FootprintHeight;
            return width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatSize(int width, int height)
        {
            if (width <= 0 && height <= 0)
                return EmptyValue;

            return width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatOffset(int x, int y)
        {
            return "x " + x.ToString(CultureInfo.InvariantCulture)
                + " | y " + y.ToString(CultureInfo.InvariantCulture);
        }

        private static string BoolText(bool value)
        {
            return value ? "Si" : "No";
        }

        private static ArcUiInspectorSeverity ResolveActionSeverity(NpcActionKind kind)
        {
            return kind switch
            {
                NpcActionKind.Eat => ArcUiInspectorSeverity.Good,
                NpcActionKind.MoveTo => ArcUiInspectorSeverity.Info,
                NpcActionKind.Sleep => ArcUiInspectorSeverity.Info,
                NpcActionKind.Steal => ArcUiInspectorSeverity.Danger,
                NpcActionKind.Combat => ArcUiInspectorSeverity.Danger,
                NpcActionKind.Work => ArcUiInspectorSeverity.Info,
                NpcActionKind.Social => ArcUiInspectorSeverity.Warning,
                _ => ArcUiInspectorSeverity.Muted
            };
        }

        private static ArcUiInspectorSeverity ConvertSeverity(MemoryBeliefDecisionColorRole role)
        {
            return role switch
            {
                MemoryBeliefDecisionColorRole.Ok => ArcUiInspectorSeverity.Good,
                MemoryBeliefDecisionColorRole.Warning => ArcUiInspectorSeverity.Warning,
                MemoryBeliefDecisionColorRole.Error => ArcUiInspectorSeverity.Danger,
                MemoryBeliefDecisionColorRole.Info => ArcUiInspectorSeverity.Info,
                MemoryBeliefDecisionColorRole.Muted => ArcUiInspectorSeverity.Muted,
                _ => ArcUiInspectorSeverity.Normal
            };
        }

        private static string FormatBeliefInline(MemoryBeliefDecisionBeliefView belief)
        {
            if (belief == null || belief.BeliefId == 0)
                return EmptyValue;

            return "#" + belief.BeliefId.ToString(CultureInfo.InvariantCulture)
                + " " + ReadString(belief.Category, EmptyValue)
                + " " + ReadString(belief.EstimatedCell, EmptyValue);
        }

        private static string FormatPercent(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatDecimal(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatSignedDecimal(float value)
        {
            string sign = value >= 0f ? "+" : string.Empty;
            return sign + value.ToString("0.###", CultureInfo.InvariantCulture);
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
