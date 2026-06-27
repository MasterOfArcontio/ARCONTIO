using Arcontio.Core;
using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contenitore passivo della foundation ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato ambientale senza lifecycle runtime</b></para>
    /// <para>
    /// Questa classe non e' un sistema, non ticka, non legge il World e non produce
    /// rendering. Conserva soltanto dati gia' risolti e permette di costruire uno
    /// snapshot read-only. L'eventuale ownership definitiva dentro <c>World</c> sara'
    /// decisa in un checkpoint successivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaDefinitions</b>: dizionario delle aree dichiarate.</item>
    ///   <item><b>FertilityAreas</b>: payload fertilita' per area.</item>
    ///   <item><b>WaterAreas</b>: payload acqua per area.</item>
    ///   <item><b>VegetationAreas</b>: payload vegetazione per area.</item>
    ///   <item><b>SeedBankAreas</b>: payload seed bank diffusa per area.</item>
    ///   <item><b>PlantInstances</b>: piante importanti conservate come stato Core.</item>
    ///   <item><b>Set*</b>: sostituzione esplicita di stato passivo.</item>
    ///   <item><b>CreateSnapshot</b>: materializzazione read-only.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentState
    {
        private static readonly EnvironmentSeedBankAreaState EmptySeedBankArea =
            new EnvironmentSeedBankAreaState(
                EnvironmentAreaId.None,
                new EnvironmentSeedBankEntry[0]);

        // Scala iniziale seed -> piante fisiche: il numero non rappresenta un target
        // fisso, ma la quota di celle naturali libere che una seed pressure massima
        // puo' occupare all'avvio. Con area raggio 14 e oak 0.95/0.95 produce circa
        // il range di test richiesto senza scollegarsi da Amount/Viability.
        private const float PhysicalPlantSeedPressurePlacementScale01 = 0.045f;
        private const float InitialPhysicalPlantVitalityMin01 = 0.70f;
        private const float InitialPhysicalPlantVitalityMax01 = 1.35f;
        private const float InitialPhysicalPlantHealthVitalityScale01 = 0.22f;
        private const int BiologicalOrganicMaskCoarseCellSize = 3;
        private const float BiologicalOrganicMaskBaseCoreRadius01 = 0.26f;
        private const float BiologicalOrganicMaskIntensityCoreRadius01 = 0.18f;
        private const float BiologicalOrganicMaskMinNoiseStrength01 = 0.18f;
        private const float BiologicalOrganicMaskMaxNoiseStrength01 = 0.88f;
        private const float BiologicalOrganicMaskMinEdgeThreshold01 = 0.02f;
        private const float BiologicalOrganicMaskMaxEdgeThreshold01 = 0.16f;

        private readonly Dictionary<EnvironmentAreaId, EnvironmentAreaDefinition> _areaDefinitions = new();
        private readonly Dictionary<EnvironmentAreaId, EnvironmentFertilityAreaState> _fertilityAreas = new();
        private readonly Dictionary<EnvironmentAreaId, EnvironmentWaterAreaState> _waterAreas = new();
        private readonly Dictionary<EnvironmentAreaId, EnvironmentVegetationAreaState> _vegetationAreas = new();
        private readonly Dictionary<EnvironmentAreaId, EnvironmentSeedBankAreaState> _seedBankAreas = new();
        private readonly Dictionary<EnvironmentPlantId, EnvironmentPlantInstance> _plantInstances = new();
        private readonly Dictionary<EnvironmentAreaId, int[]> _biologicalLandmarkNodeIdsByArea = new();
        private readonly Dictionary<EnvironmentAreaId, EnvironmentCellCoord[]> _biologicalLandmarkCellsByArea = new();
        private readonly List<EnvironmentVegetationCellPlacement> _vegetationCellPlacements = new();
        private readonly List<EnvironmentPhysicalPlantPlacement> _physicalPlantPlacements = new();

        public EnvironmentCalendarState Calendar { get; private set; }
        public EnvironmentGlobalClimateState Climate { get; private set; }
        public IReadOnlyList<EnvironmentVegetationCellPlacement> VegetationCellPlacements => _vegetationCellPlacements;
        public IReadOnlyList<EnvironmentPhysicalPlantPlacement> PhysicalPlantPlacements => _physicalPlantPlacements;
        public int AreaCount => _areaDefinitions.Count;
        public int PlantCount => _plantInstances.Count;

        // =============================================================================
        // SetCalendar
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sostituisce lo stato calendario gia' risolto.
        /// </para>
        /// </summary>
        public void SetCalendar(EnvironmentCalendarState calendar)
        {
            // Nessun avanzamento temporale avviene qui: il chiamante consegna un dato
            // gia' calcolato da sistemi futuri.
            Calendar = calendar;
        }

        // =============================================================================
        // SetClimate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sostituisce lo stato climatico globale.
        /// </para>
        /// </summary>
        public void SetClimate(EnvironmentGlobalClimateState climate)
        {
            // Il contenitore non genera meteo, non applica stagioni e non muta aree.
            Climate = climate;
        }

        // =============================================================================
        // SetAreaDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra o sostituisce la definizione base di un'area.
        /// </para>
        /// </summary>
        public bool SetAreaDefinition(EnvironmentAreaDefinition definition)
        {
            // Aree senza id valido vengono ignorate per evitare chiavi ambigue.
            if (!definition.AreaId.IsValid)
                return false;

            _areaDefinitions[definition.AreaId] = definition;
            return true;
        }

        // =============================================================================
        // SetFertilityArea
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra o sostituisce il payload di fertilita' di un'area.
        /// </para>
        ///
        /// <para><b>Payload separato dalla definizione</b></para>
        /// <para>
        /// La fertilita' resta un layer specializzato: non viene fusa dentro
        /// <c>EnvironmentAreaDefinition</c> e non crea oggetti di mondo.
        /// </para>
        /// </summary>
        public bool SetFertilityArea(EnvironmentFertilityAreaState state)
        {
            if (!state.AreaId.IsValid)
                return false;

            _fertilityAreas[state.AreaId] = state;
            return true;
        }

        // =============================================================================
        // SetWaterArea
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra o sostituisce il payload acqua di un'area.
        /// </para>
        ///
        /// <para><b>Acqua come layer ambientale</b></para>
        /// <para>
        /// Il metodo conserva solo il dato consegnato dal chiamante. Non propaga
        /// flusso, non modifica movimento e non notifica renderer.
        /// </para>
        /// </summary>
        public bool SetWaterArea(EnvironmentWaterAreaState state)
        {
            if (!state.AreaId.IsValid)
                return false;

            _waterAreas[state.AreaId] = state;
            return true;
        }

        // =============================================================================
        // SetVegetationArea
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra o sostituisce il payload di vegetazione diffusa di un'area.
        /// </para>
        ///
        /// <para><b>Seed bank e densita' come dati passivi</b></para>
        /// <para>
        /// Il contenitore non fa nascere piante e non aggiorna crescita. Conserva
        /// soltanto la fotografia area-based che sistemi futuri useranno con cadenza
        /// giornaliera.
        /// </para>
        /// </summary>
        public bool SetVegetationArea(EnvironmentVegetationAreaState state)
        {
            if (!state.AreaId.IsValid)
                return false;

            _vegetationAreas[state.AreaId] = state;
            return true;
        }

        // =============================================================================
        // SetSeedBankArea
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra o sostituisce il payload seed bank diffusa di un'area.
        /// </para>
        ///
        /// <para><b>Semi naturali come layer ecologico</b></para>
        /// <para>
        /// Il contenitore conserva disponibilita' e vitalita' astratte. Non crea semi
        /// fisici, non genera piante e non apre flussi agricoli concreti.
        /// </para>
        /// </summary>
        public bool SetSeedBankArea(EnvironmentSeedBankAreaState state)
        {
            if (state == null || !state.AreaId.IsValid)
                return false;

            _seedBankAreas[state.AreaId] = state;
            return true;
        }

        // =============================================================================
        // SetPlantInstance
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra o sostituisce una pianta importante nello stato ambientale.
        /// </para>
        ///
        /// <para><b>Piante come stato ambientale, non come oggetti Unity</b></para>
        /// <para>
        /// Il metodo conserva una PlantInstance gia' decisa dal chiamante. Non crea
        /// sprite, non alloca oggetti del World e non applica crescita automatica.
        /// </para>
        /// </summary>
        public bool SetPlantInstance(EnvironmentPlantInstance plant)
        {
            if (!plant.PlantId.IsValid || string.IsNullOrWhiteSpace(plant.SpeciesKey))
                return false;

            _plantInstances[plant.PlantId] = plant;
            return true;
        }

        // =============================================================================
        // RemovePlantInstance
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove una pianta importante dallo stato ambientale passivo.
        /// </para>
        /// </summary>
        public bool RemovePlantInstance(EnvironmentPlantId plantId)
        {
            if (!plantId.IsValid)
                return false;

            // La rimozione e' locale allo stato ambientale. Eventuali eventi, job o
            // decomposizione saranno responsabilita' di sistemi futuri.
            return _plantInstances.Remove(plantId);
        }

        // =============================================================================
        // TryGetPlantInstance
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca una PlantInstance interna per id senza esporre il dizionario.
        /// </para>
        /// </summary>
        public bool TryGetPlantInstance(
            EnvironmentPlantId plantId,
            out EnvironmentPlantInstance plant)
        {
            plant = default;

            if (!plantId.IsValid)
                return false;

            return _plantInstances.TryGetValue(plantId, out plant);
        }

        // =============================================================================
        // BuildBiologicalLandmarkCandidates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce i candidati landmark biologici da consegnare al rebuild del
        /// <see cref="LandmarkRegistry"/>. La biosfera decide le coordinate usando
        /// le proprie aree e le celle libere fornite dal <see cref="World"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: biosfera decide, registry integra</b></para>
        /// <para>
        /// Il registry landmark non conosce foreste, seed bank o fertilita'. Riceve
        /// solo coordinate gia' selezionate e un enum numerico. La mappa
        /// <c>areaId -> nodeId</c> resta qui, fuori dal nodo landmark caldo.
        /// </para>
        /// </summary>
        public int BuildBiologicalLandmarkCandidates(
            World world,
            List<LandmarkRegistry.ManualLandmarkCandidate> outCandidates)
        {
            if (world == null || outCandidates == null)
                return 0;

            int before = outCandidates.Count;
            var freeCells = new List<EnvironmentCellCoord>(64);
            var selectedCells = new List<EnvironmentCellCoord>(8);
            var globalSelectedCells = new List<EnvironmentCellCoord>(32);
            var biologicalAreas = CollectSortedBiologicalAreas();

            for (int areaIndex = 0; areaIndex < biologicalAreas.Count; areaIndex++)
            {
                EnvironmentAreaDefinition area = biologicalAreas[areaIndex];

                freeCells.Clear();
                selectedCells.Clear();

                CollectBiologicalFreeCells(world, area, freeCells);
                SelectBiologicalAnchorCells(area, freeCells, selectedCells, globalSelectedCells);

                for (int i = 0; i < selectedCells.Count; i++)
                {
                    EnvironmentCellCoord cell = selectedCells[i];
                    outCandidates.Add(new LandmarkRegistry.ManualLandmarkCandidate(
                        cell.X,
                        cell.Y,
                        LandmarkRegistry.LandmarkKind.BiologicalAnchor,
                        1.0f,
                        area.AreaId.Value));
                    AddCellIfMissing(globalSelectedCells, cell);
                }
            }

            return outCandidates.Count - before;
        }

        // =============================================================================
        // ApplyBiologicalLandmarkResolutions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riceve dal registry gli id effettivi degli anchor biologici creati o
        /// mergiati durante il rebuild landmark.
        /// </para>
        /// </summary>
        public void ApplyBiologicalLandmarkResolutions(
            IReadOnlyList<LandmarkRegistry.ManualLandmarkResolution> resolutions)
        {
            _biologicalLandmarkNodeIdsByArea.Clear();
            _biologicalLandmarkCellsByArea.Clear();
            if (resolutions == null || resolutions.Count == 0)
                return;

            var grouped = new Dictionary<EnvironmentAreaId, List<int>>();
            var groupedCells = new Dictionary<EnvironmentAreaId, List<EnvironmentCellCoord>>();
            for (int i = 0; i < resolutions.Count; i++)
            {
                LandmarkRegistry.ManualLandmarkResolution resolution = resolutions[i];
                if (resolution.OwnerId <= 0 || resolution.NodeId <= 0)
                    continue;

                if (resolution.Kind != LandmarkRegistry.LandmarkKind.BiologicalAnchor)
                    continue;

                var areaId = new EnvironmentAreaId(resolution.OwnerId);
                if (!grouped.TryGetValue(areaId, out var nodeIds))
                {
                    nodeIds = new List<int>(8);
                    grouped[areaId] = nodeIds;
                }

                if (!nodeIds.Contains(resolution.NodeId))
                    nodeIds.Add(resolution.NodeId);

                if (!groupedCells.TryGetValue(areaId, out var cells))
                {
                    cells = new List<EnvironmentCellCoord>(8);
                    groupedCells[areaId] = cells;
                }

                var cell = new EnvironmentCellCoord(resolution.CellX, resolution.CellY, 0);
                if (!ContainsCell(cells, cell))
                    cells.Add(cell);
            }

            foreach (var pair in grouped)
                _biologicalLandmarkNodeIdsByArea[pair.Key] = pair.Value.ToArray();

            foreach (var pair in groupedCells)
                _biologicalLandmarkCellsByArea[pair.Key] = pair.Value.ToArray();
        }

        public bool TryGetBiologicalLandmarkNodeIds(
            EnvironmentAreaId areaId,
            out IReadOnlyList<int> nodeIds)
        {
            if (_biologicalLandmarkNodeIdsByArea.TryGetValue(areaId, out int[] stored) && stored != null)
            {
                nodeIds = stored;
                return stored.Length > 0;
            }

            nodeIds = null;
            return false;
        }

        // =============================================================================
        // TryResolveAreaIdForBiologicalLandmark
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve l'area biologica collegata a un landmark biologico gia' registrato.
        /// </para>
        ///
        /// <para><b>Principio architetturale: mapping laterale, landmark leggero</b></para>
        /// <para>
        /// Il <see cref="LandmarkRegistry"/> conserva solo nodi compatti. Il legame
        /// semantico <c>landmark -> area biologica</c> resta nella biosfera, cosi'
        /// NPC e World possono interrogare l'ambiente senza appesantire ogni nodo
        /// landmark con payload ecologici.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>landmarkNodeId</b>: id del nodo visto o ricordato dall'NPC.</item>
        ///   <item><b>areaId</b>: area biologica risolta, se il nodo appartiene alla biosfera.</item>
        ///   <item><b>return</b>: false per id non validi, mapping assente o rebuild non ancora eseguita.</item>
        /// </list>
        /// </summary>
        public bool TryResolveAreaIdForBiologicalLandmark(
            int landmarkNodeId,
            out EnvironmentAreaId areaId)
        {
            areaId = EnvironmentAreaId.None;
            if (landmarkNodeId <= 0)
                return false;

            foreach (var pair in _biologicalLandmarkNodeIdsByArea)
            {
                int[] nodeIds = pair.Value;
                if (nodeIds == null)
                    continue;

                for (int i = 0; i < nodeIds.Length; i++)
                {
                    if (nodeIds[i] != landmarkNodeId)
                        continue;

                    areaId = pair.Key;
                    return areaId.IsValid;
                }
            }

            return false;
        }

        // =============================================================================
        // BuildInitialBiologicalOccupancy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola le celle iniziali occupate da vegetazione decorativa e piante
        /// fisiche importanti usando aree, superfici e seed bank gia' caricate.
        /// </para>
        ///
        /// <para><b>Principio architetturale: biosfera conserva il dato biologico, World fornisce la griglia</b></para>
        /// <para>
        /// Il metodo legge dal <see cref="World"/> solo disponibilita' spaziale e
        /// superfici naturali. Non crea oggetti, non sceglie sprite e non modifica
        /// ArcGraph. Le piante fisiche generate restano <see cref="EnvironmentPlantInstance"/>
        /// nello stato biosfera; la futura proiezione World potra' leggerle e
        /// trasformarle in occupazione fisica minima.
        /// </para>
        /// </summary>
        public int BuildInitialBiologicalOccupancy(World world)
        {
            _vegetationCellPlacements.Clear();
            _physicalPlantPlacements.Clear();
            _plantInstances.Clear();

            if (world == null)
                return 0;

            var freeCells = new List<EnvironmentCellCoord>(128);
            var physicalPlantCells = new List<EnvironmentCellCoord>(64);
            var anchorCells = new List<EnvironmentCellCoord>(8);
            var globalAnchorCells = new List<EnvironmentCellCoord>(32);
            var globalPlantUsedCells = new List<EnvironmentCellCoord>(32);
            var globalVegetationUsedCells = new List<EnvironmentCellCoord>(128);
            var biologicalAreas = CollectSortedBiologicalAreas();

            for (int areaIndex = 0; areaIndex < biologicalAreas.Count; areaIndex++)
            {
                EnvironmentAreaDefinition area = biologicalAreas[areaIndex];

                freeCells.Clear();
                physicalPlantCells.Clear();
                anchorCells.Clear();

                CollectBiologicalFreeCells(world, area, freeCells);
                CollectPhysicalPlantFreeCells(world, area, physicalPlantCells);
                RemoveCells(freeCells, globalPlantUsedCells);
                SelectBiologicalAnchorCells(area, freeCells, anchorCells, globalPlantUsedCells);
                AddCellsIfMissing(globalAnchorCells, anchorCells);
                BuildPhysicalPlantCells(area, physicalPlantCells, globalAnchorCells, globalPlantUsedCells);
                BuildVegetationCells(area, freeCells, globalVegetationUsedCells, globalPlantUsedCells);
            }

            return _vegetationCellPlacements.Count + _physicalPlantPlacements.Count;
        }

        // =============================================================================
        // RebuildRuntimeBiologicalPlacements
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricostruisce solo i placement runtime derivati da uno stato biologico gia'
        /// esistente.
        /// </para>
        ///
        /// <para><b>Principio architetturale: derivato fisico/visuale, biologia immutata</b></para>
        /// <para>
        /// Dopo un avanzamento giornaliero la biosfera puo' avere nuova densita'
        /// vegetale, nuove condizioni e PlantInstance aggiornate. Questo metodo
        /// riallinea le liste cell-based usate dai boundary World/ArcGraph senza
        /// svuotare <c>_plantInstances</c>, senza rigenerare piante da seed bank e
        /// senza modificare il catalogo biologico.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Vegetazione diffusa</b>: ricalcolata dalle aree e dalle celle naturali fornite dal World.</item>
        ///   <item><b>Piante fisiche</b>: ricavate dalle PlantInstance vive gia' presenti nello stato.</item>
        ///   <item><b>Nessuna mutazione biologica</b>: eta', salute, specie e seed bank restano quelli prodotti dal resolver.</item>
        /// </list>
        /// </summary>
        public int RebuildRuntimeBiologicalPlacements(World world)
        {
            _vegetationCellPlacements.Clear();
            _physicalPlantPlacements.Clear();

            if (world == null)
                return 0;

            var freeCells = new List<EnvironmentCellCoord>(128);
            var globalPlantUsedCells = new List<EnvironmentCellCoord>(32);
            var globalVegetationUsedCells = new List<EnvironmentCellCoord>(128);
            var livePlants = new List<EnvironmentPlantInstance>(_plantInstances.Count);

            foreach (var pair in _plantInstances)
            {
                EnvironmentPlantInstance plant = pair.Value;
                if (!plant.IsAlive || !plant.PlantId.IsValid)
                    continue;

                livePlants.Add(plant);
            }

            livePlants.Sort(ComparePlantInstancesForProjection);

            for (int i = 0; i < livePlants.Count; i++)
            {
                EnvironmentPlantInstance plant = livePlants[i];
                if (ContainsCell(globalPlantUsedCells, plant.Cell))
                    continue;

                _physicalPlantPlacements.Add(new EnvironmentPhysicalPlantPlacement(
                    plant.PlantId,
                    plant.SourceAreaId,
                    plant.Cell,
                    plant.SpeciesKey));
                AddCellIfMissing(globalPlantUsedCells, plant.Cell);
            }

            var biologicalAreas = CollectSortedBiologicalAreas();
            for (int areaIndex = 0; areaIndex < biologicalAreas.Count; areaIndex++)
            {
                EnvironmentAreaDefinition area = biologicalAreas[areaIndex];

                freeCells.Clear();
                CollectBiologicalFreeCells(world, area, freeCells);
                BuildVegetationCells(area, freeCells, globalVegetationUsedCells, globalPlantUsedCells);
            }

            return _vegetationCellPlacements.Count + _physicalPlantPlacements.Count;
        }

        // =============================================================================
        // ReplaceBiologicalPlacementsForSaveLoad
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sostituisce i placement cell-based della biosfera durante un load canonico.
        /// </para>
        ///
        /// <para><b>Principio architetturale: restore del layout senza rigenerazione</b></para>
        /// <para>
        /// Le <c>PlantInstance</c> conservano biologia, eta', salute e specie; questi
        /// placement conservano invece dove la biosfera aveva materializzato piante e
        /// vegetazione diffusa. Il load non deve rilanciare il bootstrap naturale se
        /// uno snapshot contiene gia' il layout vissuto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Vegetazione diffusa</b>: accettata se area e coordinate sono valide.</item>
        ///   <item><b>Piante fisiche</b>: accettate solo se puntano a una PlantInstance viva esistente.</item>
        ///   <item><b>Rejected</b>: conteggio record scartati senza mutare altri domini.</item>
        /// </list>
        /// </summary>
        public int ReplaceBiologicalPlacementsForSaveLoad(
            IReadOnlyList<EnvironmentVegetationCellPlacement> vegetationPlacements,
            IReadOnlyList<EnvironmentPhysicalPlantPlacement> physicalPlantPlacements)
        {
            _vegetationCellPlacements.Clear();
            _physicalPlantPlacements.Clear();

            int rejected = 0;
            var safeVegetation = vegetationPlacements ?? new EnvironmentVegetationCellPlacement[0];
            for (int i = 0; i < safeVegetation.Count; i++)
            {
                EnvironmentVegetationCellPlacement placement = safeVegetation[i];
                if (!placement.AreaId.IsValid || !_areaDefinitions.ContainsKey(placement.AreaId))
                {
                    rejected++;
                    continue;
                }

                _vegetationCellPlacements.Add(placement);
            }

            var safePhysicalPlants = physicalPlantPlacements ?? new EnvironmentPhysicalPlantPlacement[0];
            for (int i = 0; i < safePhysicalPlants.Count; i++)
            {
                EnvironmentPhysicalPlantPlacement placement = safePhysicalPlants[i];
                if (!placement.PlantId.IsValid
                    || !_plantInstances.TryGetValue(placement.PlantId, out EnvironmentPlantInstance plant)
                    || !plant.IsAlive
                    || !placement.AreaId.IsValid
                    || !_areaDefinitions.ContainsKey(placement.AreaId))
                {
                    rejected++;
                    continue;
                }

                _physicalPlantPlacements.Add(placement);
            }

            return rejected;
        }

        private bool IsBiologicalArea(EnvironmentAreaDefinition area)
        {
            return area.AreaId.IsValid
                   && area.IsEnabled
                   && (area.Kind == EnvironmentAreaKind.Vegetation
                       || _vegetationAreas.ContainsKey(area.AreaId)
                       || _seedBankAreas.ContainsKey(area.AreaId));
        }

        private void CollectBiologicalFreeCells(
            World world,
            EnvironmentAreaDefinition area,
            List<EnvironmentCellCoord> outCells)
        {
            if (world == null || outCells == null)
                return;

            world.CollectEnvironmentNaturalCandidateCells(area, outCells);
            ApplyBiologicalOrganicMask(area, outCells);
        }

        private void CollectPhysicalPlantFreeCells(
            World world,
            EnvironmentAreaDefinition area,
            List<EnvironmentCellCoord> outCells)
        {
            if (world == null || outCells == null)
                return;

            world.CollectEnvironmentNaturalCandidateCells(
                area,
                outCells,
                requirePhysicalPlantHost: true);
            ApplyBiologicalOrganicMask(area, outCells);
        }

        // =============================================================================
        // ApplyBiologicalOrganicMask
        // =============================================================================
        /// <summary>
        /// <para>
        /// Filtra le celle naturali candidate di un'area biologica usando una
        /// maschera organica deterministica.
        /// </para>
        ///
        /// <para><b>Principio architetturale: raggio massimo, forma biologica locale</b></para>
        /// <para>
        /// Il <see cref="World"/> continua a rispondere con tutte le celle naturali
        /// dentro il raggio massimo dell'area. La biosfera, che e' proprietaria della
        /// semantica biologica, decide poi quali celle appartengono davvero alla
        /// macchia ecologica. In questo modo non introduciamo un sistema di forme
        /// parallelo e non appesantiamo il file mappa: centro e raggio restano il
        /// contratto spaziale minimo, mentre il bordo reale viene irregolarizzato con
        /// hash stabile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Compatibilita'</b>: aree non circolari e raggi minuscoli restano invariate.</item>
        ///   <item><b>Intensita'</b>: ricavata dai payload esistenti di vegetazione, seed bank e fertilita'.</item>
        ///   <item><b>Noise</b>: combinazione coarse/fine deterministica, senza Random runtime.</item>
        ///   <item><b>Fallback</b>: se il filtro svuotasse l'area, conserva le celle originali.</item>
        /// </list>
        /// </summary>
        private void ApplyBiologicalOrganicMask(
            EnvironmentAreaDefinition area,
            List<EnvironmentCellCoord> cells)
        {
            if (cells == null || cells.Count <= 1 || !area.UsesCircularArea || area.RadiusCells <= 2)
                return;

            float intensity01 = ResolveBiologicalAreaMaskIntensity01(area);
            int writeIndex = 0;

            for (int readIndex = 0; readIndex < cells.Count; readIndex++)
            {
                EnvironmentCellCoord cell = cells[readIndex];
                if (!IsInsideBiologicalOrganicMask(area, cell, intensity01))
                    continue;

                cells[writeIndex] = cell;
                writeIndex++;
            }

            if (writeIndex <= 0)
                return;

            if (writeIndex < cells.Count)
                cells.RemoveRange(writeIndex, cells.Count - writeIndex);
        }

        private float ResolveBiologicalAreaMaskIntensity01(EnvironmentAreaDefinition area)
        {
            float intensity01 = 0.5f;

            if (_vegetationAreas.TryGetValue(area.AreaId, out EnvironmentVegetationAreaState vegetation))
                intensity01 = System.Math.Max(intensity01, vegetation.Density01);

            if (_seedBankAreas.TryGetValue(area.AreaId, out EnvironmentSeedBankAreaState seedBank))
                intensity01 = System.Math.Max(
                    intensity01,
                    EnvironmentMath.Clamp01(seedBank.TotalAmount01 * seedBank.AverageViability01));

            if (_fertilityAreas.TryGetValue(area.AreaId, out EnvironmentFertilityAreaState fertility))
                intensity01 = System.Math.Max(intensity01, fertility.GrowthModifier01);

            return EnvironmentMath.Clamp01(intensity01);
        }

        private static bool IsInsideBiologicalOrganicMask(
            EnvironmentAreaDefinition area,
            EnvironmentCellCoord cell,
            float intensity01)
        {
            int dx = cell.X - area.CenterX;
            int dy = cell.Y - area.CenterY;
            int radius = area.RadiusCells;
            int distanceSquared = (dx * dx) + (dy * dy);
            if (distanceSquared > radius * radius)
                return false;

            float distance01 = (float)(System.Math.Sqrt(distanceSquared) / radius);
            float coreRadius01 = BiologicalOrganicMaskBaseCoreRadius01
                                  + (EnvironmentMath.Clamp01(intensity01) * BiologicalOrganicMaskIntensityCoreRadius01);
            if (distance01 <= coreRadius01)
                return true;

            float edgePresence01 = 1f - distance01;
            float noise01 = ResolveBiologicalOrganicMaskNoise01(area, cell);
            float intensityBias01 = (EnvironmentMath.Clamp01(intensity01) - 0.5f) * 0.20f;
            float irregularity01 = EnvironmentMath.Clamp01(area.Irregularity01);
            float noiseStrength01 = Lerp(
                BiologicalOrganicMaskMinNoiseStrength01,
                BiologicalOrganicMaskMaxNoiseStrength01,
                irregularity01);
            float edgeThreshold01 = Lerp(
                BiologicalOrganicMaskMinEdgeThreshold01,
                BiologicalOrganicMaskMaxEdgeThreshold01,
                irregularity01);
            float presence01 = edgePresence01
                                + ((noise01 - 0.5f) * noiseStrength01)
                                + intensityBias01;

            return presence01 >= edgeThreshold01;
        }

        private static float ResolveBiologicalOrganicMaskNoise01(
            EnvironmentAreaDefinition area,
            EnvironmentCellCoord cell)
        {
            int coarseX = cell.X / BiologicalOrganicMaskCoarseCellSize;
            int coarseY = cell.Y / BiologicalOrganicMaskCoarseCellSize;
            float coarse01 = ResolveBiologicalOrganicMaskHash01(area, coarseX, coarseY, 719);
            float fine01 = ResolveBiologicalOrganicMaskHash01(area, cell.X, cell.Y, 421);
            return (coarse01 * 0.75f) + (fine01 * 0.25f);
        }

        private static float ResolveBiologicalOrganicMaskHash01(
            EnvironmentAreaDefinition area,
            int x,
            int y,
            int salt)
        {
            unchecked
            {
                int hash = salt;
                hash = (hash * 397) ^ ResolveStableAreaSeed(area);
                hash = (hash * 397) ^ (x * 73856093);
                hash = (hash * 397) ^ (y * 19349663);
                hash = (hash * 397) ^ (area.Bounds.Z * 83492791);
                return (hash & int.MaxValue) / (float)int.MaxValue;
            }
        }

        private static float Lerp(float from, float to, float t)
        {
            float safeT = EnvironmentMath.Clamp01(t);
            return from + ((to - from) * safeT);
        }

        // =============================================================================
        // BuildVegetationCells
        // =============================================================================
        /// <summary>
        /// <para>
        /// Materializza la vegetazione diffusa di un'area rispettando le riserve gia'
        /// assegnate da aree biologiche processate prima.
        /// </para>
        ///
        /// <para><b>Principio architetturale: overlap risolto nel placement, non nel catalogo</b></para>
        /// <para>
        /// Le aree biologiche possono sovrapporsi come informazione ecologica, ma la
        /// proiezione cell-based verso World/ArcGraph deve restare univoca. Per
        /// questo il metodo riceve celle di vegetazione gia' usate e celle occupate
        /// da piante fisiche: una pianta vince sempre su vegetazione decorativa.
        /// </para>
        /// </summary>
        private void BuildVegetationCells(
            EnvironmentAreaDefinition area,
            List<EnvironmentCellCoord> freeCells,
            List<EnvironmentCellCoord> usedVegetationCells,
            List<EnvironmentCellCoord> usedPhysicalPlantCells)
        {
            if (!_vegetationAreas.TryGetValue(area.AreaId, out EnvironmentVegetationAreaState vegetation))
                return;

            int availableCount = CountAvailableCells(freeCells, usedVegetationCells, usedPhysicalPlantCells);
            int targetCount = ResolvePlacementCount(availableCount, vegetation.Density01, 1.0f);
            var distributedCells = new List<EnvironmentCellCoord>(freeCells);
            SortCellsByDistributionScore(distributedCells, ResolveStableAreaSeed(area));

            int created = 0;
            for (int i = 0; i < distributedCells.Count && created < targetCount; i++)
            {
                EnvironmentCellCoord cell = distributedCells[i];
                if (ContainsCell(usedVegetationCells, cell) || ContainsCell(usedPhysicalPlantCells, cell))
                    continue;

                _vegetationCellPlacements.Add(new EnvironmentVegetationCellPlacement(
                    area.AreaId,
                    cell,
                    vegetation.VegetationKind,
                    vegetation.Density01,
                    vegetation.Health01));
                AddCellIfMissing(usedVegetationCells, cell);
                created++;
            }
        }

        private void BuildPhysicalPlantCells(
            EnvironmentAreaDefinition area,
            List<EnvironmentCellCoord> freeCells,
            List<EnvironmentCellCoord> anchorCells,
            List<EnvironmentCellCoord> usedPlantCells)
        {
            if (!_seedBankAreas.TryGetValue(area.AreaId, out EnvironmentSeedBankAreaState seedBank)
                || seedBank.Entries == null
                || seedBank.Entries.Count == 0)
            {
                return;
            }

            int createdInArea = 0;

            for (int entryIndex = 0; entryIndex < seedBank.Entries.Count; entryIndex++)
            {
                EnvironmentSeedBankEntry entry = seedBank.Entries[entryIndex];
                if (string.IsNullOrWhiteSpace(entry.SpeciesKey))
                    continue;

                float pressure = entry.Amount01 * entry.Viability01;
                int targetForSpecies = ResolvePlacementCount(
                    freeCells.Count,
                    pressure,
                    PhysicalPlantSeedPressurePlacementScale01);
                for (int i = 0; i < targetForSpecies; i++)
                {
                    if (!TryPickPlantCell(freeCells, anchorCells, usedPlantCells, area, entryIndex, i, out EnvironmentCellCoord cell))
                        return;

                    int plantOrdinal = createdInArea + 1;
                    var plantId = new EnvironmentPlantId(200000 + (area.AreaId.Value * 1000) + plantOrdinal);
                    float initialHealth01 = ResolveInitialPhysicalPlantHealth01(
                        entry,
                        plantId,
                        cell);
                    var plant = new EnvironmentPlantInstance(
                        plantId,
                        entry.SpeciesKey,
                        cell,
                        0,
                        EnvironmentPlantGrowthStage.Seedling,
                        "seedling",
                        EnvironmentPlantHealthState.Healthy,
                        initialHealth01,
                        0f,
                        false,
                        area.AreaId);

                    _plantInstances[plantId] = plant;
                    _physicalPlantPlacements.Add(new EnvironmentPhysicalPlantPlacement(
                        plantId,
                        area.AreaId,
                        cell,
                        entry.SpeciesKey));
                    AddCellIfMissing(usedPlantCells, cell);
                    createdInArea++;
                }
            }
        }

        // =============================================================================
        // ResolveInitialPhysicalPlantHealth01
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola una salute iniziale differenziata per le piante fisiche di bootstrap.
        /// </para>
        ///
        /// <para><b>Coerenza con il modello naturale giornaliero</b></para>
        /// <para>
        /// Le piante create all'avvio usano lo stesso concetto di vigore individuale
        /// poi applicato dal ciclo naturale: individui piu' robusti partono con piu'
        /// margine, quelli fragili con meno. Il valore resta deterministico su
        /// specie, id e cella, quindi non cambia tra due avvii identici.
        /// </para>
        /// </summary>
        private static float ResolveInitialPhysicalPlantHealth01(
            EnvironmentSeedBankEntry entry,
            EnvironmentPlantId plantId,
            EnvironmentCellCoord cell)
        {
            float vitality = ResolveInitialPhysicalPlantVitality01(
                plantId,
                entry.SpeciesKey,
                cell);
            float offset = (vitality - 1f) * InitialPhysicalPlantHealthVitalityScale01;
            return EnvironmentMath.Clamp01(entry.Viability01 + offset);
        }

        private static float ResolveInitialPhysicalPlantVitality01(
            EnvironmentPlantId plantId,
            string speciesKey,
            EnvironmentCellCoord cell)
        {
            float roll = ResolveInitialPhysicalPlantHash01(
                plantId,
                speciesKey,
                cell,
                431);
            return InitialPhysicalPlantVitalityMin01
                   + ((InitialPhysicalPlantVitalityMax01 - InitialPhysicalPlantVitalityMin01) * roll);
        }

        private static int ResolvePlacementCount(int availableCount, float intensity01, float scale)
        {
            if (availableCount <= 0)
                return 0;

            float intensity = EnvironmentMath.Clamp01(intensity01);
            float scaled = availableCount * intensity * scale;
            int count = (int)System.Math.Round(scaled);
            if (count < 0) return 0;
            if (count > availableCount) return availableCount;
            return count;
        }

        private static bool TryPickPlantCell(
            List<EnvironmentCellCoord> freeCells,
            List<EnvironmentCellCoord> anchorCells,
            List<EnvironmentCellCoord> usedPlantCells,
            EnvironmentAreaDefinition area,
            int entryIndex,
            int localIndex,
            out EnvironmentCellCoord cell)
        {
            cell = default;
            if (freeCells == null || freeCells.Count == 0)
                return false;

            int bestScore = int.MinValue;
            bool hasBest = false;
            int areaSeed = ResolveStableAreaSeed(area);
            int salt = (entryIndex * 1009) ^ (localIndex * 9176) ^ areaSeed;

            for (int i = 0; i < freeCells.Count; i++)
            {
                EnvironmentCellCoord candidate = freeCells[i];
                if (ContainsCell(anchorCells, candidate) || ContainsCell(usedPlantCells, candidate))
                    continue;

                int score = ResolveCellDistributionScore(candidate, salt);
                if (hasBest && score <= bestScore)
                    continue;

                bestScore = score;
                cell = candidate;
                hasBest = true;
            }

            return hasBest;
        }

        // =============================================================================
        // SortCellsByDistributionScore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ordina le celle candidate con uno score pseudo-casuale deterministico.
        /// </para>
        ///
        /// <para><b>Distribuzione spaziale stabile</b></para>
        /// <para>
        /// Il <c>World</c> consegna celle in ordine di scansione. Ordinare una copia
        /// locale con hash evita pattern a righe senza introdurre random runtime:
        /// la stessa mappa, la stessa area e la stessa configurazione producono
        /// sempre la stessa distribuzione iniziale e gli stessi cambiamenti sparsi.
        /// </para>
        /// </summary>
        private static void SortCellsByDistributionScore(
            List<EnvironmentCellCoord> cells,
            int salt)
        {
            if (cells == null || cells.Count <= 1)
                return;

            cells.Sort((left, right) =>
            {
                int rightScore = ResolveCellDistributionScore(right, salt);
                int leftScore = ResolveCellDistributionScore(left, salt);
                int scoreCompare = rightScore.CompareTo(leftScore);
                if (scoreCompare != 0)
                    return scoreCompare;

                int yCompare = left.Y.CompareTo(right.Y);
                if (yCompare != 0)
                    return yCompare;

                int xCompare = left.X.CompareTo(right.X);
                if (xCompare != 0)
                    return xCompare;

                return left.Z.CompareTo(right.Z);
            });
        }

        private static int ResolveStableAreaSeed(EnvironmentAreaDefinition area)
        {
            return (area.AreaId.Value * 73856093)
                   ^ (area.CenterX * 19349663)
                   ^ (area.CenterY * 83492791)
                   ^ (area.RadiusCells * 265443576);
        }

        private static float ResolveInitialPhysicalPlantHash01(
            EnvironmentPlantId plantId,
            string speciesKey,
            EnvironmentCellCoord cell,
            int salt)
        {
            unchecked
            {
                int hash = 97 + salt;
                hash = (hash * 397) ^ plantId.Value;
                hash = (hash * 397) ^ ResolveStableSpeciesHash(speciesKey);
                hash = (hash * 397) ^ (cell.X * 73856093);
                hash = (hash * 397) ^ (cell.Y * 19349663);
                hash = (hash * 397) ^ (cell.Z * 83492791);
                return (hash & int.MaxValue) / (float)int.MaxValue;
            }
        }

        private static int ResolveStableSpeciesHash(string speciesKey)
        {
            if (string.IsNullOrWhiteSpace(speciesKey))
                return 17;

            int hash = 23;
            for (int i = 0; i < speciesKey.Length; i++)
            {
                // Hash stabile e minimale: serve a differenziare individui iniziali
                // senza usare Random runtime o stato globale.
                hash = (hash * 31) + speciesKey[i];
            }

            return hash;
        }

        private static int ResolveCellDistributionScore(EnvironmentCellCoord cell, int salt)
        {
            unchecked
            {
                int hash = salt;
                hash = (hash * 397) ^ (cell.X * 73856093);
                hash = (hash * 397) ^ (cell.Y * 19349663);
                hash = (hash * 397) ^ (cell.Z * 83492791);
                return hash & int.MaxValue;
            }
        }

        // =============================================================================
        // SelectBiologicalAnchorCells
        // =============================================================================
        /// <summary>
        /// <para>
        /// Seleziona le celle landmark biologiche evitando celle gia' riservate da
        /// piante fisiche o da landmark biologici scelti per aree precedenti.
        /// </para>
        /// </summary>
        private static void SelectBiologicalAnchorCells(
            EnvironmentAreaDefinition area,
            List<EnvironmentCellCoord> freeCells,
            List<EnvironmentCellCoord> outSelected,
            List<EnvironmentCellCoord> reservedCells = null)
        {
            if (freeCells == null || outSelected == null || freeCells.Count == 0)
                return;

            int radius = area.UsesCircularArea
                ? area.RadiusCells
                : System.Math.Max(area.Bounds.Width, area.Bounds.Height) / 2;
            int targetCount = radius >= 9 ? 8 : radius >= 5 ? 6 : 4;

            int[,] dirs = targetCount >= 8
                ? new[,] { { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 } }
                : targetCount == 6
                    ? new[,] { { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 0 }, { -1, -1 }, { 0, -1 } }
                    : new[,] { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

            for (int i = 0; i < targetCount; i++)
            {
                int tx = area.CenterX + (dirs[i, 0] * System.Math.Max(1, radius));
                int ty = area.CenterY + (dirs[i, 1] * System.Math.Max(1, radius));

                if (TryFindNearestUnusedCell(freeCells, outSelected, reservedCells, tx, ty, out EnvironmentCellCoord selected))
                    outSelected.Add(selected);
            }
        }

        private static bool TryFindNearestUnusedCell(
            List<EnvironmentCellCoord> cells,
            List<EnvironmentCellCoord> used,
            List<EnvironmentCellCoord> reserved,
            int targetX,
            int targetY,
            out EnvironmentCellCoord selected)
        {
            selected = default;
            int bestIndex = -1;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < cells.Count; i++)
            {
                EnvironmentCellCoord cell = cells[i];
                if (ContainsCell(used, cell) || ContainsCell(reserved, cell))
                    continue;

                int dx = cell.X - targetX;
                int dy = cell.Y - targetY;
                int distance = (dx * dx) + (dy * dy);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestIndex = i;
            }

            if (bestIndex < 0)
                return false;

            selected = cells[bestIndex];
            return true;
        }

        private List<EnvironmentAreaDefinition> CollectSortedBiologicalAreas()
        {
            var areas = new List<EnvironmentAreaDefinition>(_areaDefinitions.Count);
            foreach (var pair in _areaDefinitions)
            {
                EnvironmentAreaDefinition area = pair.Value;
                if (IsBiologicalArea(area))
                    areas.Add(area);
            }

            areas.Sort(CompareBiologicalAreasForOccupancy);
            return areas;
        }

        private static int CompareBiologicalAreasForOccupancy(
            EnvironmentAreaDefinition left,
            EnvironmentAreaDefinition right)
        {
            // Priorita' maggiore prima: in celle sovrapposte l'area piu' importante
            // riserva per prima vegetazione, piante e anchor biologici.
            int priorityCompare = right.Priority.CompareTo(left.Priority);
            if (priorityCompare != 0)
                return priorityCompare;

            return left.AreaId.Value.CompareTo(right.AreaId.Value);
        }

        private int ComparePlantInstancesForProjection(
            EnvironmentPlantInstance left,
            EnvironmentPlantInstance right)
        {
            int leftPriority = ResolveAreaPriority(left.SourceAreaId);
            int rightPriority = ResolveAreaPriority(right.SourceAreaId);
            int priorityCompare = rightPriority.CompareTo(leftPriority);
            if (priorityCompare != 0)
                return priorityCompare;

            int areaCompare = left.SourceAreaId.Value.CompareTo(right.SourceAreaId.Value);
            if (areaCompare != 0)
                return areaCompare;

            return left.PlantId.Value.CompareTo(right.PlantId.Value);
        }

        private int ResolveAreaPriority(EnvironmentAreaId areaId)
        {
            return areaId.IsValid && _areaDefinitions.TryGetValue(areaId, out EnvironmentAreaDefinition area)
                ? area.Priority
                : int.MinValue;
        }

        private static int CountAvailableCells(
            List<EnvironmentCellCoord> cells,
            List<EnvironmentCellCoord> usedVegetationCells,
            List<EnvironmentCellCoord> usedPhysicalPlantCells)
        {
            if (cells == null || cells.Count == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                EnvironmentCellCoord cell = cells[i];
                if (ContainsCell(usedVegetationCells, cell) || ContainsCell(usedPhysicalPlantCells, cell))
                    continue;

                count++;
            }

            return count;
        }

        private static void RemoveCells(
            List<EnvironmentCellCoord> cells,
            List<EnvironmentCellCoord> reservedCells)
        {
            if (cells == null || cells.Count == 0 || reservedCells == null || reservedCells.Count == 0)
                return;

            for (int i = cells.Count - 1; i >= 0; i--)
                if (ContainsCell(reservedCells, cells[i]))
                    cells.RemoveAt(i);
        }

        private static void AddCellsIfMissing(
            List<EnvironmentCellCoord> target,
            List<EnvironmentCellCoord> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
                AddCellIfMissing(target, source[i]);
        }

        private static void AddCellIfMissing(
            List<EnvironmentCellCoord> cells,
            EnvironmentCellCoord cell)
        {
            if (cells == null || ContainsCell(cells, cell))
                return;

            cells.Add(cell);
        }

        private static bool ContainsCell(List<EnvironmentCellCoord> cells, EnvironmentCellCoord target)
        {
            if (cells == null)
                return false;

            for (int i = 0; i < cells.Count; i++)
                if (cells[i].X == target.X && cells[i].Y == target.Y && cells[i].Z == target.Z)
                    return true;

            return false;
        }

        // =============================================================================
        // CreateSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Materializza uno snapshot read-only dello stato ambientale.
        /// </para>
        ///
        /// <para><b>Snapshot senza esposizione dei dizionari</b></para>
        /// <para>
        /// Il metodo copia i payload in una lista nuova. I consumer futuri possono
        /// leggere lo snapshot senza mutare lo stato interno della foundation.
        /// </para>
        /// </summary>
        public EnvironmentSnapshot CreateSnapshot()
        {
            var areas = new List<EnvironmentAreaSnapshot>(_areaDefinitions.Count);
            var plants = new List<EnvironmentPlantSnapshot>(_plantInstances.Count);

            foreach (var pair in _areaDefinitions)
            {
                var areaId = pair.Key;
                _fertilityAreas.TryGetValue(areaId, out var fertility);
                _waterAreas.TryGetValue(areaId, out var water);
                _vegetationAreas.TryGetValue(areaId, out var vegetation);
                _seedBankAreas.TryGetValue(areaId, out var seedBank);

                areas.Add(new EnvironmentAreaSnapshot(
                    pair.Value,
                    _fertilityAreas.ContainsKey(areaId),
                    fertility,
                    _waterAreas.ContainsKey(areaId),
                    water,
                    _vegetationAreas.ContainsKey(areaId),
                    vegetation,
                    _seedBankAreas.ContainsKey(areaId),
                    seedBank ?? EmptySeedBankArea));
            }

            foreach (var pair in _plantInstances)
            {
                // Le piante vengono copiate in snapshot autonomi: nessun consumer
                // riceve accesso al registry interno della biosfera.
                plants.Add(pair.Value.ToSnapshot());
            }

            return new EnvironmentSnapshot(Calendar, Climate, areas, plants);
        }

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota le aree ambientali conservando calendario e clima correnti.
        /// </para>
        /// </summary>
        public void Clear()
        {
            // Il reset e' esplicito e locale: nessun oggetto del World o consumer
            // visuale viene notificato da questa foundation passiva.
            _areaDefinitions.Clear();
            _fertilityAreas.Clear();
            _waterAreas.Clear();
            _vegetationAreas.Clear();
            _seedBankAreas.Clear();
            _plantInstances.Clear();
        }
    }
}
