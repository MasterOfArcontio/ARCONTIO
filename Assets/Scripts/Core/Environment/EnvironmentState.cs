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

        private readonly Dictionary<EnvironmentAreaId, EnvironmentAreaDefinition> _areaDefinitions = new();
        private readonly Dictionary<EnvironmentAreaId, EnvironmentFertilityAreaState> _fertilityAreas = new();
        private readonly Dictionary<EnvironmentAreaId, EnvironmentWaterAreaState> _waterAreas = new();
        private readonly Dictionary<EnvironmentAreaId, EnvironmentVegetationAreaState> _vegetationAreas = new();
        private readonly Dictionary<EnvironmentAreaId, EnvironmentSeedBankAreaState> _seedBankAreas = new();
        private readonly Dictionary<EnvironmentPlantId, EnvironmentPlantInstance> _plantInstances = new();

        public EnvironmentCalendarState Calendar { get; private set; }
        public EnvironmentGlobalClimateState Climate { get; private set; }
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
