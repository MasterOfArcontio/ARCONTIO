using System.Collections.Generic;
using System.Globalization;
using Arcontio.Core;
using Arcontio.Core.Environment;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectFoodStockOwnerOption
    // =============================================================================
    /// <summary>
    /// <para>
    /// Opzione value-only usata dal RightInspector per mostrare gli NPC candidati a
    /// proprietari di uno stock oggetto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: opzioni UI come snapshot, non dizionario World</b></para>
    /// <para>
    /// La view non deve conservare <c>NpcDnaProfile</c> o leggere direttamente
    /// <c>World.NpcDna</c>. Il provider autorizzato prepara solo id e label stabile,
    /// abbastanza per disegnare un pulsante che il bridge trasformera' in comando.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: identificatore runtime dell'NPC.</item>
    ///   <item><b>Label</b>: testo compatto per il bottone UI.</item>
    ///   <item><b>IsValid</b>: guardia minima contro opzioni vuote.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphObjectFoodStockOwnerOption
    {
        public readonly int NpcId;
        public readonly string Label;
        public bool IsValid => NpcId > 0;

        public ArcGraphObjectFoodStockOwnerOption(int npcId, string label)
        {
            NpcId = npcId;
            Label = string.IsNullOrWhiteSpace(label)
                ? "NPC " + npcId.ToString(CultureInfo.InvariantCulture)
                : label.Trim();
        }
    }

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
        // FillNpcOwnerOptions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riempie una lista chiamante con gli NPC attualmente selezionabili come
        /// owner di uno stock oggetto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: elenco NPC come snapshot autorizzato</b></para>
        /// <para>
        /// Il metodo legge il <c>World</c> soltanto dentro il provider runtime e
        /// restituisce alla UI oggetti valore minimali. Non espone dizionari, profili
        /// mutabili o riferimenti alla simulazione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Clear</b>: la lista chiamante viene sempre riallineata.</item>
        ///   <item><b>Sort</b>: l'ordine e' stabile per id crescente.</item>
        ///   <item><b>Label</b>: usa nome DNA se disponibile, altrimenti NPC #id.</item>
        /// </list>
        /// </summary>
        public void FillNpcOwnerOptions(List<ArcGraphObjectFoodStockOwnerOption> target)
        {
            if (target == null)
                return;

            target.Clear();

            if (!TryGetWorld(out World world) || world.NpcDna == null || world.NpcDna.Count == 0)
                return;

            var ids = new List<int>(world.NpcDna.Keys);
            ids.Sort();

            for (int i = 0; i < ids.Count; i++)
            {
                int npcId = ids[i];
                if (npcId <= 0)
                    continue;

                target.Add(new ArcGraphObjectFoodStockOwnerOption(
                    npcId,
                    ResolveNpcOwnerOptionLabel(world, npcId)));
            }
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
        // TryBuildNpcEditViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la prima shell di modifica NPC per il RightInspector.
        /// </para>
        ///
        /// <para><b>Principio architetturale: modifica preparata, non applicata</b></para>
        /// <para>
        /// Questo metodo risponde a una <see cref="ArcUiSelectionActionRequest"/> di
        /// tipo Modifica e produce soltanto un ViewModel. Legge lo snapshot runtime
        /// necessario a mostrare DNA e inventario, ma non cambia valori, non crea
        /// slider operativi, non scrive sul <c>World</c> e non invia comandi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>DNA</b>: visualizza le barre DNA che diventeranno tuning autorizzato.</item>
        ///   <item><b>Inventario</b>: visualizza le metriche cibo gia' usate nell'Info tab.</item>
        ///   <item><b>Conferma</b>: documenta il ponte futuro verso controller e gateway.</item>
        /// </list>
        /// </summary>
        public bool TryBuildNpcEditViewModel(
            ArcUiSelectionActionRequest request,
            out ArcUiInspectorViewModel viewModel)
        {
            viewModel = ArcUiInspectorViewModel.Empty();

            if (!request.IsEdit
                || request.Target.Kind != ArcUiSelectionTargetKind.Npc
                || !TryParseNpcId(request.Target, out int npcId))
            {
                return false;
            }

            if (!TryGetWorld(out World world) || !world.NpcDna.TryGetValue(npcId, out NpcDnaProfile dna) || dna == null)
                return false;

            world.NpcProfiles.TryGetValue(npcId, out NpcProfile profile);
            world.GridPos.TryGetValue(npcId, out GridPosition position);

            string title = "Modifica " + ResolveNpcTitle(request.Target, dna, npcId);
            var tabs = new[]
            {
                new ArcUiInspectorTab("edit_dna", "DNA", BuildNpcEditDnaRows(request, npcId, dna, profile, position)),
                new ArcUiInspectorTab("edit_inventory", "Inventario", BuildNpcEditInventoryRows(world, npcId)),
                new ArcUiInspectorTab("edit_confirm", "Conferma", BuildNpcEditConfirmRows(request, npcId))
            };

            viewModel = new ArcUiInspectorViewModel(
                title,
                request.Target,
                tabs,
                "edit_dna");

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

        // =============================================================================
        // TryBuildPlantViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il ViewModel read-only del RightInspector per una pianta
        /// fisica proiettata nel World.
        /// </para>
        ///
        /// <para><b>Principio architetturale: World projection -> Snapshot -> UI</b></para>
        /// <para>
        /// Il provider non legge lo stato interno della Biosfera. Usa soltanto
        /// <c>World.PhysicalPlants</c>, cioe' la proiezione autorizzata gia' copiata
        /// nel boundary fisico/spaziale, e produce righe UI value-only.
        /// </para>
        /// </summary>
        public bool TryBuildPlantViewModel(
            ArcUiSelectionTarget target,
            out ArcUiInspectorViewModel viewModel)
        {
            viewModel = ArcUiInspectorViewModel.Empty();

            if (target.Kind != ArcUiSelectionTargetKind.Plant || !TryParsePlantId(target, out int plantId))
                return false;

            if (!TryGetWorld(out World world)
                || !world.TryGetPhysicalPlant(new EnvironmentPlantId(plantId), out WorldPhysicalPlantProjection plant))
            {
                return false;
            }

            var tabs = new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", BuildPlantInfoRows(target, plant)),
                new ArcUiInspectorTab("physical", "Fisica", BuildPlantPhysicalRows(plant)),
                new ArcUiInspectorTab("boundary", "Boundary", BuildPlantBoundaryRows(target, plant))
            };

            viewModel = new ArcUiInspectorViewModel(
                ResolvePlantTitle(target, plant),
                target,
                tabs,
                InfoTabKey);
            return true;
        }

        // =============================================================================
        // TryBuildObjectEditViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la shell di modifica per oggetti, muri e porte selezionati.
        /// </para>
        ///
        /// <para><b>Principio architetturale: modifica oggetto come ViewModel</b></para>
        /// <para>
        /// Il metodo riceve una richiesta UI gia' prodotta dal menu selezione e
        /// prepara soltanto tab e righe per il RightInspector. Non apre prefab, non
        /// sostituisce ObjectDef, non modifica <c>World.Objects</c>, non cambia
        /// food stock, porte o muri e non invia comandi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Parametri</b>: identita' oggetto e parametri modificabili noti.</item>
        ///   <item><b>Dettaglio</b>: storage, porta, materiale o proprieta' catalogo.</item>
        ///   <item><b>Conferma</b>: punto futuro di passaggio a controller/gateway.</item>
        /// </list>
        /// </summary>
        public bool TryBuildObjectEditViewModel(
            ArcUiSelectionActionRequest request,
            out ArcUiInspectorViewModel viewModel)
        {
            viewModel = ArcUiInspectorViewModel.Empty();

            if (!request.IsEdit
                || (request.Target.Kind != ArcUiSelectionTargetKind.Object && request.Target.Kind != ArcUiSelectionTargetKind.Wall)
                || !TryParseObjectId(request.Target, out int objectId))
            {
                return false;
            }

            if (!TryGetWorld(out World world)
                || !world.Objects.TryGetValue(objectId, out WorldObjectInstance instance)
                || instance == null)
            {
                return false;
            }

            world.TryGetObjectDef(instance.DefId, out ObjectDef def);

            bool hasFoodStock = world.FoodStocks.TryGetValue(instance.ObjectId, out FoodStockComponent stock);
            bool isDoor = def != null && def.IsDoor;
            bool isWall = request.Target.Kind == ArcUiSelectionTargetKind.Wall
                || (def != null
                    && def.Visual != null
                    && string.Equals(def.Visual.VisualKind, "wall", System.StringComparison.OrdinalIgnoreCase));

            var tabs = new List<ArcUiInspectorTab>(4)
            {
                new ArcUiInspectorTab("edit_params", "Parametri", BuildObjectEditParameterRows(request, instance, def, hasFoodStock, stock, isDoor, isWall))
            };

            if (hasFoodStock)
                tabs.Add(new ArcUiInspectorTab("edit_storage", "Storage", BuildObjectEditStorageRows(stock)));
            else if (isDoor)
                tabs.Add(new ArcUiInspectorTab("edit_door", "Porta", BuildObjectEditDoorRows(instance, def)));
            else if (isWall)
                tabs.Add(new ArcUiInspectorTab("edit_material", "Materiale", BuildObjectEditMaterialRows(instance, def)));
            else
                tabs.Add(new ArcUiInspectorTab("edit_catalog", "Catalogo", BuildObjectUseRows(world, instance, def)));

            tabs.Add(new ArcUiInspectorTab("edit_confirm", "Conferma", BuildObjectEditConfirmRows(request, instance, def, hasFoodStock, isDoor, isWall)));

            viewModel = new ArcUiInspectorViewModel(
                "Modifica " + ResolveObjectTitle(request.Target, instance, def),
                request.Target,
                tabs.ToArray(),
                "edit_params");

            return true;
        }

        // =============================================================================
        // TryBuildDeleteConfirmationViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce la shell di conferma eliminazione per target selezionati.
        /// </para>
        ///
        /// <para><b>Principio architetturale: eliminazione come richiesta autorizzata</b></para>
        /// <para>
        /// Il metodo prepara solo un ViewModel di conferma. Non rimuove NPC, non
        /// rimuove oggetti, non aggiorna indici, non libera riserve e non modifica il
        /// <c>World</c>. L'eliminazione reale dovra' passare da controller
        /// autorizzato e Command Gateway.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>NPC</b>: conferma target NPC e impatto runtime minimale.</item>
        ///   <item><b>Object/Wall</b>: conferma target oggetto, porta o muro.</item>
        ///   <item><b>Fallback</b>: se il runtime non e' disponibile, la factory usa la conferma generica.</item>
        /// </list>
        /// </summary>
        public bool TryBuildDeleteConfirmationViewModel(
            ArcUiSelectionActionRequest request,
            out ArcUiInspectorViewModel viewModel)
        {
            viewModel = ArcUiInspectorViewModel.Empty();

            if (!request.IsDelete)
                return false;

            if (request.Target.Kind == ArcUiSelectionTargetKind.Npc
                && TryBuildNpcDeleteConfirmationViewModel(request, out viewModel))
            {
                return true;
            }

            if ((request.Target.Kind == ArcUiSelectionTargetKind.Object || request.Target.Kind == ArcUiSelectionTargetKind.Wall)
                && TryBuildObjectDeleteConfirmationViewModel(request, out viewModel))
            {
                return true;
            }

            return false;
        }

        // =============================================================================
        // TryBuildNpcDeleteConfirmationViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara la conferma eliminazione specifica per NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: NPC letto come snapshot</b></para>
        /// <para>
        /// La funzione recupera nome, cella e stato runtime minimo per rendere
        /// chiaro cosa verra' eliminato in futuro. Non tocca profilo, bisogni,
        /// inventario, job o memoria dell'NPC.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Conferma</b>: identita' del target.</item>
        ///   <item><b>Impatto</b>: stato e cibo collegato.</item>
        ///   <item><b>Gateway</b>: richiesta futura non ancora inviata.</item>
        /// </list>
        /// </summary>
        private bool TryBuildNpcDeleteConfirmationViewModel(
            ArcUiSelectionActionRequest request,
            out ArcUiInspectorViewModel viewModel)
        {
            viewModel = ArcUiInspectorViewModel.Empty();

            if (!TryParseNpcId(request.Target, out int npcId))
                return false;

            if (!TryGetWorld(out World world) || !world.NpcDna.TryGetValue(npcId, out NpcDnaProfile dna) || dna == null)
                return false;

            world.GridPos.TryGetValue(npcId, out GridPosition position);
            world.TryGetNpcAction(npcId, out NpcActionState action);

            string title = "Elimina " + ResolveNpcTitle(request.Target, dna, npcId);
            var tabs = new[]
            {
                new ArcUiInspectorTab("delete_confirm", "Conferma", BuildNpcDeleteConfirmRows(request, npcId, dna, position)),
                new ArcUiInspectorTab("delete_impact", "Impatto", BuildNpcDeleteImpactRows(world, npcId, action)),
                new ArcUiInspectorTab("delete_gateway", "Gateway", BuildDeleteGatewayRows(request, "DeleteNpcCommandRequest futuro"))
            };

            viewModel = new ArcUiInspectorViewModel(
                title,
                request.Target,
                tabs,
                "delete_confirm");

            return true;
        }

        // =============================================================================
        // TryBuildObjectDeleteConfirmationViewModel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara la conferma eliminazione specifica per oggetti, muri e porte.
        /// </para>
        ///
        /// <para><b>Principio architetturale: eliminazione separata dal renderer</b></para>
        /// <para>
        /// Il metodo legge istanza oggetto e ObjectDef solo per descrivere il target.
        /// Non distrugge renderer, GameObject, stock, porta o istanza World.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Conferma</b>: identita' e tipo oggetto.</item>
        ///   <item><b>Impatto</b>: owner, occupazione, storage e regole mappa.</item>
        ///   <item><b>Gateway</b>: richiesta futura non ancora inviata.</item>
        /// </list>
        /// </summary>
        private bool TryBuildObjectDeleteConfirmationViewModel(
            ArcUiSelectionActionRequest request,
            out ArcUiInspectorViewModel viewModel)
        {
            viewModel = ArcUiInspectorViewModel.Empty();

            if (!TryParseObjectId(request.Target, out int objectId))
                return false;

            if (!TryGetWorld(out World world)
                || !world.Objects.TryGetValue(objectId, out WorldObjectInstance instance)
                || instance == null)
            {
                return false;
            }

            world.TryGetObjectDef(instance.DefId, out ObjectDef def);

            bool hasFoodStock = world.FoodStocks.TryGetValue(instance.ObjectId, out FoodStockComponent stock);
            bool isDoor = def != null && def.IsDoor;
            bool isWall = request.Target.Kind == ArcUiSelectionTargetKind.Wall
                || (def != null
                    && def.Visual != null
                    && string.Equals(def.Visual.VisualKind, "wall", System.StringComparison.OrdinalIgnoreCase));

            string deleteRequestName = isDoor
                ? "DeleteDoorCommandRequest futuro"
                : isWall
                    ? "DeleteWallCommandRequest futuro"
                    : "DeleteObjectCommandRequest futuro";

            var tabs = new[]
            {
                new ArcUiInspectorTab("delete_confirm", "Conferma", BuildObjectDeleteConfirmRows(request, instance, def, isDoor, isWall)),
                new ArcUiInspectorTab("delete_impact", "Impatto", BuildObjectDeleteImpactRows(world, instance, def, hasFoodStock, stock, isDoor, isWall)),
                new ArcUiInspectorTab("delete_gateway", "Gateway", BuildDeleteGatewayRows(request, deleteRequestName))
            };

            viewModel = new ArcUiInspectorViewModel(
                "Elimina " + ResolveObjectTitle(request.Target, instance, def),
                request.Target,
                tabs,
                "delete_confirm");

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

        // =============================================================================
        // BuildNpcEditDnaRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara le righe della tab DNA usata dalla shell di modifica NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: valori modificabili solo come snapshot</b></para>
        /// <para>
        /// Le barre DNA qui prodotte rappresentano i valori che in futuro potranno
        /// essere collegati a controlli autorizzati. In questo step restano valori
        /// visuali read-only, cosi' la UI non introduce una mutazione diretta del
        /// profilo NPC.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Shell modifica</b>: stato del workflow provvisorio.</item>
        ///   <item><b>Target</b>: id, nome e cella logica dell'NPC.</item>
        ///   <item><b>Valori DNA</b>: barre gia' condivise con l'inspector read-only.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildNpcEditDnaRows(
            ArcUiSelectionActionRequest request,
            int npcId,
            NpcDnaProfile dna,
            NpcProfile profile,
            GridPosition position)
        {
            var rows = new List<ArcUiInspectorRow>(32)
            {
                ArcUiInspectorRow.Section("Shell modifica"),
                new ArcUiInspectorRow("Stato", "Draft DNA editabile"),
                new ArcUiInspectorRow("Effetto", "Applica accoda comando autorizzato"),
                new ArcUiInspectorRow("NPC", npcId.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Nome", ResolveNpcTitle(request.Target, dna, npcId)),
                new ArcUiInspectorRow("Cella logica", FormatCell(position.X, position.Y, 0)),
                ArcUiInspectorRow.Section("Valori DNA")
            };

            rows.AddRange(BuildDnaRows(dna, profile));
            return rows.ToArray();
        }

        // =============================================================================
        // BuildNpcEditInventoryRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara le righe inventario della shell di modifica NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: inventario letto, non scritto</b></para>
        /// <para>
        /// La funzione riusa le metriche cibo gia' autorizzate per l'Info tab e non
        /// espone alcun controllo operativo. La futura modifica inventario dovra'
        /// passare da richiesta dedicata, controller e command gateway.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Inventario shell</b>: stato esplicito del workflow non operativo.</item>
        ///   <item><b>Cibo</b>: metriche compatte del cibo privato e comunitario.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildNpcEditInventoryRows(
            World world,
            int npcId)
        {
            var rows = new List<ArcUiInspectorRow>(12)
            {
                ArcUiInspectorRow.Section("Inventario shell"),
                new ArcUiInspectorRow("Stato", "Modifica cibo addosso disponibile"),
                new ArcUiInspectorRow("Effetto", "I bottoni sotto accodano comando autorizzato"),
                ArcUiInspectorRow.Section("Cibo")
            };

            AddFoodMetrics(world, npcId, rows);
            return rows.ToArray();
        }

        // =============================================================================
        // BuildNpcEditConfirmRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara la tab di conferma futura per la shell di modifica NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: conferma prima del comando</b></para>
        /// <para>
        /// La tab non contiene ancora un bottone di conferma reale. Serve a rendere
        /// visibile il punto in cui, negli step successivi, la UI consegnera' una
        /// richiesta validata al controller autorizzato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Stato</b>: richiesta di modifica ricevuta.</item>
        ///   <item><b>Target</b>: NPC oggetto della modifica.</item>
        ///   <item><b>Prossimo ponte</b>: controller autorizzato e command gateway.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildNpcEditConfirmRows(
            ArcUiSelectionActionRequest request,
            int npcId)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Conferma futura"),
                new ArcUiInspectorRow("Stato", "Richiesta modifica pronta"),
                new ArcUiInspectorRow("Target", "NPC " + npcId.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Sorgente", ReadString(request.Source, EmptyValue)),
                new ArcUiInspectorRow("Draft request", "ArcUiEditSelectionRequest attiva"),
                new ArcUiInspectorRow("Effetto attuale", "Nessuna mutazione"),
                new ArcUiInspectorRow("Prossimo ponte", "Command Gateway autorizzato")
            };
        }

        // =============================================================================
        // BuildObjectEditParameterRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara la tab principale della shell di modifica oggetto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: parametri espliciti, niente payload generico</b></para>
        /// <para>
        /// La tab espone solo i dati che oggi hanno un significato runtime o di
        /// catalogo chiaro: identita', cella, tipo, variante e presenza di parametri
        /// specifici. Non usa un dizionario libero per anticipare funzioni future non
        /// ancora decise.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Shell modifica</b>: stato non operativo della richiesta.</item>
        ///   <item><b>Target</b>: id, def, nome e cella logica.</item>
        ///   <item><b>Parametri disponibili</b>: tipo di modifica realmente preparabile.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildObjectEditParameterRows(
            ArcUiSelectionActionRequest request,
            WorldObjectInstance instance,
            ObjectDef def,
            bool hasFoodStock,
            FoodStockComponent stock,
            bool isDoor,
            bool isWall)
        {
            var rows = new List<ArcUiInspectorRow>(28)
            {
                ArcUiInspectorRow.Section("Shell modifica"),
                new ArcUiInspectorRow("Stato", "Modifica stock preparata"),
                new ArcUiInspectorRow("Effetto", "Scrittura solo da controlli autorizzati"),
                new ArcUiInspectorRow("Oggetto", instance.ObjectId.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Def", ReadString(instance.DefId, EmptyValue)),
                new ArcUiInspectorRow("Nome", ResolveObjectTitle(request.Target, instance, def)),
                new ArcUiInspectorRow("Cella logica", FormatCell(instance.CellX, instance.CellY, 0)),
                ArcUiInspectorRow.Section("Parametri disponibili")
            };

            if (hasFoodStock)
            {
                rows.Add(ArcUiInspectorRow.IconMetrics(
                    "edit_food_stock",
                    "Food stock",
                    new[]
                    {
                        new ArcUiInspectorMetric("food_units", "Unita'", stock.Units.ToString(CultureInfo.InvariantCulture), stock.Units > 0 ? ArcUiInspectorSeverity.Good : ArcUiInspectorSeverity.Muted),
                        new ArcUiInspectorMetric("food_owner", "Owner", FormatOwner(stock.OwnerKind, stock.OwnerId), ArcUiInspectorSeverity.Info)
                    }));
                rows.Add(new ArcUiInspectorRow("Parametro operativo", "Quantita' e owner food stock"));
            }
            else if (isDoor)
            {
                rows.Add(new ArcUiInspectorRow("Parametro futuro", "Apertura / blocco porta"));
                rows.Add(new ArcUiInspectorRow("Aperta", instance.IsOpen ? "Si" : "No"));
                rows.Add(new ArcUiInspectorRow("Bloccata", instance.IsLocked ? "Si" : "No"));
            }
            else if (isWall)
            {
                rows.Add(new ArcUiInspectorRow("Parametro futuro", "Variante materiale muro"));
                rows.Add(new ArcUiInspectorRow("Materiale attuale", def == null ? EmptyValue : ReadString(def.DisplayName, EmptyValue)));
            }
            else
            {
                rows.Add(new ArcUiInspectorRow("Parametro futuro", "Nessun parametro runtime noto"));
                rows.Add(new ArcUiInspectorRow("Modifica possibile", "Solo sostituzione variante futura"));
            }

            rows.Add(ArcUiInspectorRow.Section("Ownership oggetto food"));
            rows.Add(new ArcUiInspectorRow("Owner oggetto", FormatOwner(instance.OwnerKind, instance.OwnerId)));
            rows.Add(new ArcUiInspectorRow("Trasportato", instance.IsHeld ? "Si" : "No"));
            return rows.ToArray();
        }

        // =============================================================================
        // BuildObjectEditStorageRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara la tab storage per oggetti che possiedono un FoodStockComponent.
        /// </para>
        ///
        /// <para><b>Principio architetturale: snapshot visuale, comando separato</b></para>
        /// <para>
        /// La quantita' e l'owner vengono mostrati come snapshot corrente. I bottoni
        /// operativi vengono disegnati dalla view, ma la validazione e la mutazione
        /// restano nel comando Core autorizzato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Food stock</b>: unita', owner e stato vuoto.</item>
        ///   <item><b>Controllo operativo</b>: descrive stepper e owner routing autorizzati.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildObjectEditStorageRows(FoodStockComponent stock)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Food stock"),
                new ArcUiInspectorRow("Unita' attuali", stock.Units.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Owner stock/oggetto", FormatOwner(stock.OwnerKind, stock.OwnerId)),
                new ArcUiInspectorRow("Vuoto", stock.IsEmpty ? "Si" : "No"),
                ArcUiInspectorRow.Section("Controllo operativo"),
                new ArcUiInspectorRow("Quantita'", "Stepper autorizzato nel pannello"),
                new ArcUiInspectorRow("Proprieta'", "Community oppure NPC esistente"),
                new ArcUiInspectorRow("Effetto", "Accoda comando runtime autorizzato")
            };
        }

        // =============================================================================
        // BuildObjectEditDoorRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara la tab porta per la shell di modifica oggetto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: stato porta senza comando diretto</b></para>
        /// <para>
        /// Apertura e blocco sono stati runtime sensibili per movimento e visione.
        /// Qui vengono solo letti: un futuro toggle dovra' produrre una richiesta
        /// autorizzata, non modificare direttamente l'istanza.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Stato porta</b>: aperta, bloccata, lockable e key.</item>
        ///   <item><b>Controlli futuri</b>: toggle autorizzati non ancora operativi.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildObjectEditDoorRows(
            WorldObjectInstance instance,
            ObjectDef def)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Stato porta"),
                new ArcUiInspectorRow("Aperta", instance.IsOpen ? "Si" : "No"),
                new ArcUiInspectorRow("Bloccata", instance.IsLocked ? "Si" : "No"),
                new ArcUiInspectorRow("Lockable", BoolText(def != null && def.IsLockable)),
                new ArcUiInspectorRow("Key", def == null ? EmptyValue : ReadString(def.KeyId, EmptyValue)),
                ArcUiInspectorRow.Section("Controlli futuri"),
                new ArcUiInspectorRow("Apertura", "Toggle autorizzato"),
                new ArcUiInspectorRow("Blocco", "Toggle autorizzato se lockable"),
                new ArcUiInspectorRow("Effetto attuale", "Nessun comando porta inviato")
            };
        }

        // =============================================================================
        // BuildObjectEditMaterialRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara la tab materiale/variante per muri e oggetti senza storage.
        /// </para>
        ///
        /// <para><b>Principio architetturale: variante come scelta futura di catalogo</b></para>
        /// <para>
        /// Un cambio materiale non viene trattato come mutazione diretta del def
        /// esistente. La shell espone il def attuale e segnala che il cambio dovra'
        /// diventare una scelta catalogo validata da controller.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Definizione attuale</b>: def, nome catalogo, footprint e sprite.</item>
        ///   <item><b>Variante futura</b>: dropdown/catalogo non ancora operativo.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildObjectEditMaterialRows(
            WorldObjectInstance instance,
            ObjectDef def)
        {
            var rows = new List<ArcUiInspectorRow>(16)
            {
                ArcUiInspectorRow.Section("Definizione attuale"),
                new ArcUiInspectorRow("Def", ReadString(instance.DefId, EmptyValue)),
                new ArcUiInspectorRow("Nome catalogo", def == null ? EmptyValue : ReadString(def.DisplayName, EmptyValue)),
                new ArcUiInspectorRow("Footprint", FormatFootprint(def)),
                new ArcUiInspectorRow("Sprite", def == null ? EmptyValue : ReadString(def.ResolveArcGraphSpritePath(), EmptyValue)),
                ArcUiInspectorRow.Section("Variante futura"),
                new ArcUiInspectorRow("Tipo controllo", "Dropdown/catalogo variante"),
                new ArcUiInspectorRow("Effetto attuale", "Nessuna sostituzione applicata")
            };

            AddObjectProperties(rows, def);
            return rows.ToArray();
        }

        // =============================================================================
        // BuildObjectEditConfirmRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara la tab di conferma futura per modifica oggetto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: richiesta prima del comando</b></para>
        /// <para>
        /// Questa tab rende esplicito quale tipo di richiesta dovra' essere emessa
        /// in futuro. Non contiene ancora il pulsante operativo e non produce alcun
        /// command request.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Target</b>: oggetto runtime selezionato.</item>
        ///   <item><b>Tipo modifica</b>: storage, porta, muro/materiale o generica.</item>
        ///   <item><b>Prossimo ponte</b>: controller autorizzato e command gateway.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildObjectEditConfirmRows(
            ArcUiSelectionActionRequest request,
            WorldObjectInstance instance,
            ObjectDef def,
            bool hasFoodStock,
            bool isDoor,
            bool isWall)
        {
            string editKind = hasFoodStock
                ? "Food stock"
                : isDoor
                    ? "Porta"
                    : isWall
                        ? "Muro/materiale"
                        : "Oggetto generico";

            return new[]
            {
                ArcUiInspectorRow.Section("Conferma futura"),
                new ArcUiInspectorRow("Stato", "Richiesta modifica pronta"),
                new ArcUiInspectorRow("Target", "Oggetto " + instance.ObjectId.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Nome", ResolveObjectTitle(request.Target, instance, def)),
                new ArcUiInspectorRow("Tipo modifica", editKind),
                new ArcUiInspectorRow("Sorgente", ReadString(request.Source, EmptyValue)),
                new ArcUiInspectorRow("Draft request", "ArcUiEditSelectionRequest attiva"),
                new ArcUiInspectorRow("Effetto attuale", "Nessuna mutazione"),
                new ArcUiInspectorRow("Prossimo ponte", "Command Gateway autorizzato")
            };
        }

        // =============================================================================
        // BuildNpcDeleteConfirmRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara le righe della tab Conferma per eliminazione NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: conferma esplicita prima della mutazione</b></para>
        /// <para>
        /// La tab mostra il target e dichiara che l'effetto attuale e' nullo. La
        /// conferma reale verra' introdotta solo quando esistera' un controller di
        /// eliminazione autorizzato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Target</b>: id, nome e cella logica.</item>
        ///   <item><b>Stato</b>: richiesta preparata ma non applicata.</item>
        ///   <item><b>Avviso</b>: eliminazione non reversibile nel futuro gateway.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildNpcDeleteConfirmRows(
            ArcUiSelectionActionRequest request,
            int npcId,
            NpcDnaProfile dna,
            GridPosition position)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Conferma eliminazione"),
                new ArcUiInspectorRow("Stato", "Richiesta eliminazione preparata"),
                new ArcUiInspectorRow("Effetto attuale", "Nessuna eliminazione applicata"),
                new ArcUiInspectorRow("Tipo", "NPC"),
                new ArcUiInspectorRow("NPC", npcId.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Nome", ResolveNpcTitle(request.Target, dna, npcId)),
                new ArcUiInspectorRow("Cella logica", FormatCell(position.X, position.Y, 0)),
                ArcUiInspectorRow.Section("Avviso"),
                new ArcUiInspectorRow("Richiede conferma", "Si"),
                new ArcUiInspectorRow("Mutazione futura", "Solo tramite controller autorizzato")
            };
        }

        // =============================================================================
        // BuildNpcDeleteImpactRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara le righe di impatto potenziale per eliminazione NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: impatto descritto, non risolto</b></para>
        /// <para>
        /// La funzione elenca dati che il futuro controller dovra' considerare, come
        /// azione corrente e cibo collegato. Non cancella job, food stock, memoria o
        /// riferimenti all'NPC.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Runtime</b>: azione corrente.</item>
        ///   <item><b>Cibo</b>: metriche cibo privato/comunitario gia' disponibili.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildNpcDeleteImpactRows(
            World world,
            int npcId,
            NpcActionState action)
        {
            var rows = new List<ArcUiInspectorRow>(16)
            {
                ArcUiInspectorRow.Section("Runtime collegato"),
                new ArcUiInspectorRow("Azione corrente", ReadString(action.ToString(), EmptyValue)),
                new ArcUiInspectorRow("Cleanup futuro", "Job, inventario, memoria e riferimenti"),
                ArcUiInspectorRow.Section("Cibo collegato")
            };

            AddFoodMetrics(world, npcId, rows);
            return rows.ToArray();
        }

        // =============================================================================
        // BuildObjectDeleteConfirmRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara le righe della tab Conferma per eliminazione oggetto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: target fisico separato dal comando</b></para>
        /// <para>
        /// La tab identifica oggetto, porta o muro senza distruggere l'istanza
        /// runtime. Il futuro comando dovra' validare target, occupazione e permessi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Target</b>: object id, def, nome e cella.</item>
        ///   <item><b>Classificazione</b>: oggetto, muro o porta.</item>
        ///   <item><b>Avviso</b>: nessuna mutazione in questa shell.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildObjectDeleteConfirmRows(
            ArcUiSelectionActionRequest request,
            WorldObjectInstance instance,
            ObjectDef def,
            bool isDoor,
            bool isWall)
        {
            string kind = isDoor
                ? "Porta"
                : isWall
                    ? "Muro"
                    : "Oggetto";

            return new[]
            {
                ArcUiInspectorRow.Section("Conferma eliminazione"),
                new ArcUiInspectorRow("Stato", "Richiesta eliminazione preparata"),
                new ArcUiInspectorRow("Effetto attuale", "Nessuna eliminazione applicata"),
                new ArcUiInspectorRow("Tipo", kind),
                new ArcUiInspectorRow("Oggetto", instance.ObjectId.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Def", ReadString(instance.DefId, EmptyValue)),
                new ArcUiInspectorRow("Nome", ResolveObjectTitle(request.Target, instance, def)),
                new ArcUiInspectorRow("Cella logica", FormatCell(instance.CellX, instance.CellY, 0)),
                ArcUiInspectorRow.Section("Avviso"),
                new ArcUiInspectorRow("Richiede conferma", "Si"),
                new ArcUiInspectorRow("Mutazione futura", "Solo tramite controller autorizzato")
            };
        }

        // =============================================================================
        // BuildObjectDeleteImpactRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara le righe di impatto potenziale per eliminazione oggetto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: dipendenze visibili prima del comando</b></para>
        /// <para>
        /// La funzione mostra owner, occupazione, storage e regole mappa che il
        /// controller futuro dovra' validare. Non libera celle, non rimuove stock e
        /// non aggiorna pathfinding o occlusione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Runtime</b>: owner, trasporto e occupazione.</item>
        ///   <item><b>Storage</b>: food stock eventuale.</item>
        ///   <item><b>Mappa</b>: blocco movimento/visione e stato porta.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildObjectDeleteImpactRows(
            World world,
            WorldObjectInstance instance,
            ObjectDef def,
            bool hasFoodStock,
            FoodStockComponent stock,
            bool isDoor,
            bool isWall)
        {
            ObjectUseState useState = world.GetUseStateOrDefault(instance.ObjectId);
            var rows = new List<ArcUiInspectorRow>(24)
            {
                ArcUiInspectorRow.Section("Runtime collegato"),
                new ArcUiInspectorRow("Owner", FormatOwner(instance.OwnerKind, instance.OwnerId)),
                new ArcUiInspectorRow("Trasportato", instance.IsHeld ? "Si" : "No"),
                new ArcUiInspectorRow("Holder NPC", instance.HolderNpcId > 0 ? instance.HolderNpcId.ToString(CultureInfo.InvariantCulture) : EmptyValue),
                new ArcUiInspectorRow("In uso", useState.IsInUse ? "Si" : "No"),
                new ArcUiInspectorRow("Using NPC", useState.UsingNpcId > 0 ? useState.UsingNpcId.ToString(CultureInfo.InvariantCulture) : EmptyValue),
                new ArcUiInspectorRow("Occupante", instance.OccupantNpcId >= 0 ? instance.OccupantNpcId.ToString(CultureInfo.InvariantCulture) : EmptyValue)
            };

            if (hasFoodStock)
            {
                rows.Add(ArcUiInspectorRow.Section("Storage"));
                rows.Add(ArcUiInspectorRow.IconMetrics(
                    "delete_food_stock",
                    "Food stock",
                    new[]
                    {
                        new ArcUiInspectorMetric("food_units", "Unita'", stock.Units.ToString(CultureInfo.InvariantCulture), stock.Units > 0 ? ArcUiInspectorSeverity.Warning : ArcUiInspectorSeverity.Muted),
                        new ArcUiInspectorMetric("food_owner", "Owner", FormatOwner(stock.OwnerKind, stock.OwnerId), ArcUiInspectorSeverity.Info)
                    }));
            }

            rows.Add(ArcUiInspectorRow.Section("Mappa"));
            rows.Add(new ArcUiInspectorRow("Footprint", FormatFootprint(def)));
            rows.Add(new ArcUiInspectorRow("Blocca movimento", BoolText(def != null && def.BlocksMovement)));
            rows.Add(new ArcUiInspectorRow("Blocca visione", BoolText(def != null && def.BlocksVision)));

            if (isDoor)
            {
                rows.Add(new ArcUiInspectorRow("Porta aperta", instance.IsOpen ? "Si" : "No"));
                rows.Add(new ArcUiInspectorRow("Porta bloccata", instance.IsLocked ? "Si" : "No"));
            }

            if (isWall)
                rows.Add(new ArcUiInspectorRow("Connessioni visuali", "Da ricostruire dopo eliminazione futura"));

            return rows.ToArray();
        }

        // =============================================================================
        // BuildDeleteGatewayRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara le righe comuni della tab Gateway per eliminazione.
        /// </para>
        ///
        /// <para><b>Principio architetturale: UI produce intenzione, non comando diretto</b></para>
        /// <para>
        /// La tab dichiara il command request futuro, ma non lo crea e non lo invia.
        /// Questo mantiene separati hover menu, inspector, controller e simulazione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Request UI</b>: sorgente e target della richiesta.</item>
        ///   <item><b>Gateway futuro</b>: controller e command request attesi.</item>
        /// </list>
        /// </summary>
        private static ArcUiInspectorRow[] BuildDeleteGatewayRows(
            ArcUiSelectionActionRequest request,
            string futureCommandRequestName)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Request UI"),
                new ArcUiInspectorRow("Sorgente", ReadString(request.Source, EmptyValue)),
                new ArcUiInspectorRow("Azione", "Delete"),
                new ArcUiInspectorRow("Target id", ReadString(request.Target.Id, EmptyValue)),
                ArcUiInspectorRow.Section("Gateway futuro"),
                new ArcUiInspectorRow("Controller", "ArcDeleteSelectionController futuro"),
                new ArcUiInspectorRow("Command request", futureCommandRequestName),
                new ArcUiInspectorRow("Effetto attuale", "Nessun comando inviato")
            };
        }

        // =============================================================================
        // BuildPlantInfoRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara le righe informative principali per una pianta fisica.
        /// </para>
        /// </summary>
        private static ArcUiInspectorRow[] BuildPlantInfoRows(
            ArcUiSelectionTarget target,
            WorldPhysicalPlantProjection plant)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Pianta fisica"),
                new ArcUiInspectorRow("Plant id", plant.PlantId.Value.ToString(CultureInfo.InvariantCulture)),
                new ArcUiInspectorRow("Specie", ReadString(plant.SpeciesKey, EmptyValue)),
                new ArcUiInspectorRow("Stadio", ReadString(plant.GrowthStageKey, EmptyValue)),
                new ArcUiInspectorRow("Salute", plant.HealthState.ToString()),
                new ArcUiInspectorRow("Viva", BoolText(plant.IsAlive)),
                new ArcUiInspectorRow("Area", plant.AreaId.ToString()),
                new ArcUiInspectorRow("Cella", FormatCell(plant.Cell.X, plant.Cell.Y, plant.Cell.Z)),
                new ArcUiInspectorRow("Sorgente", ReadString(target.SourceView, EmptyValue))
            };
        }

        // =============================================================================
        // BuildPlantPhysicalRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara le righe dell'impatto fisico della pianta nel World.
        /// </para>
        /// </summary>
        private static ArcUiInspectorRow[] BuildPlantPhysicalRows(
            WorldPhysicalPlantProjection plant)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Impatto World"),
                new ArcUiInspectorRow("Blocca movimento", BoolText(plant.BlocksMovement)),
                new ArcUiInspectorRow("Blocca visione", BoolText(plant.BlocksVision)),
                new ArcUiInspectorRow("Vision cost", FormatDecimal(plant.VisionCost)),
                ArcUiInspectorRow.Section("Contratto"),
                new ArcUiInspectorRow("Owner dati biologici", "Biosfera"),
                new ArcUiInspectorRow("Owner spazio/fisica", "World projection"),
                new ArcUiInspectorRow("Mutazione UI", "Non consentita")
            };
        }

        // =============================================================================
        // BuildPlantBoundaryRows
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prepara righe di debug leggero sul boundary visuale della selezione.
        /// </para>
        /// </summary>
        private static ArcUiInspectorRow[] BuildPlantBoundaryRows(
            ArcUiSelectionTarget target,
            WorldPhysicalPlantProjection plant)
        {
            return new[]
            {
                ArcUiInspectorRow.Section("Selection target"),
                new ArcUiInspectorRow("Tipo UI", "Pianta"),
                new ArcUiInspectorRow("Target id", ReadString(target.Id, EmptyValue)),
                new ArcUiInspectorRow("Target cell", FormatCell(target.Cell.X, target.Cell.Y, target.Cell.Z)),
                ArcUiInspectorRow.Section("Proiezione"),
                new ArcUiInspectorRow("Projection cell", FormatCell(plant.Cell.X, plant.Cell.Y, plant.Cell.Z)),
                new ArcUiInspectorRow("Harvest job", "Fuori scope v0.71.08")
            };
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

        private static bool TryParsePlantId(ArcUiSelectionTarget target, out int plantId)
        {
            return int.TryParse(target.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out plantId) && plantId > 0;
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

        private static string ResolveNpcOwnerOptionLabel(World world, int npcId)
        {
            if (world != null
                && world.NpcDna != null
                && world.NpcDna.TryGetValue(npcId, out NpcDnaProfile dna)
                && dna != null
                && !string.IsNullOrWhiteSpace(dna.Identity.Name))
            {
                return dna.Identity.Name.Trim();
            }

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

        private static string ResolvePlantTitle(
            ArcUiSelectionTarget target,
            WorldPhysicalPlantProjection plant)
        {
            if (!string.IsNullOrWhiteSpace(target.DisplayName))
                return target.DisplayName.Trim();

            if (!string.IsNullOrWhiteSpace(plant.SpeciesKey))
                return "Pianta " + plant.SpeciesKey.Trim() + " #" + plant.PlantId.Value.ToString(CultureInfo.InvariantCulture);

            return "Pianta #" + plant.PlantId.Value.ToString(CultureInfo.InvariantCulture);
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
