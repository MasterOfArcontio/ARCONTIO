namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentCultivationStage
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stadio data-only di una coltivazione intenzionale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: agricoltura separata dalla vegetazione naturale</b></para>
    /// <para>
    /// La vegetazione spontanea nasce da aree e seed bank naturali; la coltivazione
    /// nasce invece da una scelta futura di NPC/job. Questa enum separa i due mondi
    /// senza introdurre ancora sistemi di lavoro agricolo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna coltivazione attiva.</item>
    ///   <item><b>Prepared</b>: suolo preparato ma non seminato.</item>
    ///   <item><b>Sown</b>: seme agricolo collocato o dichiarato.</item>
    ///   <item><b>Growing</b>: coltura in crescita.</item>
    ///   <item><b>ReadyToHarvest</b>: raccolto disponibile come dato.</item>
    ///   <item><b>Exhausted</b>: coltura terminata o campo da recuperare.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentCultivationStage
    {
        None = 0,
        Prepared = 10,
        Sown = 20,
        Growing = 30,
        ReadyToHarvest = 40,
        Exhausted = 50
    }

    // =============================================================================
    // EnvironmentAgricultureIntentKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo di intenzione agricola futura.
    /// </para>
    ///
    /// <para><b>Principio architetturale: hook futuri senza job concreti</b></para>
    /// <para>
    /// Il Core Environment puo' dichiarare quali operazioni agricole avranno senso,
    /// ma non deve ancora creare job, prenotazioni, animazioni o decisioni NPC.
    /// Questa enum prepara il vocabolario per quei bridge futuri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PrepareSoil</b>: preparazione area coltivabile.</item>
    ///   <item><b>Sow</b>: semina intenzionale.</item>
    ///   <item><b>Irrigate</b>: apporto acqua controllato.</item>
    ///   <item><b>Weed</b>: riduzione pressione infestanti.</item>
    ///   <item><b>Harvest</b>: raccolta output maturo.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentAgricultureIntentKind
    {
        PrepareSoil = 0,
        Sow = 10,
        Irrigate = 20,
        Weed = 30,
        Harvest = 40
    }

    // =============================================================================
    // EnvironmentCultivatedAreaState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato data-only di un'area coltivata o coltivabile.
    /// </para>
    ///
    /// <para><b>Principio architetturale: campo come area, non come oggetto</b></para>
    /// <para>
    /// Un campo agricolo e' un layer ambientale intenzionale. Non diventa un oggetto
    /// piazzato e non sostituisce fertilita', acqua o vegetazione: li legge e li
    /// usera' in futuro come condizioni di lavoro e crescita.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area ambientale associata al campo.</item>
    ///   <item><b>CropSpeciesKey</b>: specie coltivata prevista.</item>
    ///   <item><b>Stage</b>: stato coltivazione intenzionale.</item>
    ///   <item><b>SoilPreparation01</b>: preparazione agricola del suolo.</item>
    ///   <item><b>Irrigation01</b>: apporto acqua controllato.</item>
    ///   <item><b>WeedPressure01</b>: pressione erbe/competizione.</item>
    ///   <item><b>CultivationHealth01</b>: salute media della coltura.</item>
    ///   <item><b>IsActive</b>: gate dichiarativo dell'area agricola.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentCultivatedAreaState
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly string CropSpeciesKey;
        public readonly EnvironmentCultivationStage Stage;
        public readonly float SoilPreparation01;
        public readonly float Irrigation01;
        public readonly float WeedPressure01;
        public readonly float CultivationHealth01;
        public readonly bool IsActive;

        // =============================================================================
        // EnvironmentCultivatedAreaState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo stato area agricola normalizzando i valori continui.
        /// </para>
        /// </summary>
        public EnvironmentCultivatedAreaState(
            EnvironmentAreaId areaId,
            string cropSpeciesKey,
            EnvironmentCultivationStage stage,
            float soilPreparation01,
            float irrigation01,
            float weedPressure01,
            float cultivationHealth01,
            bool isActive)
        {
            AreaId = areaId;
            CropSpeciesKey = string.IsNullOrWhiteSpace(cropSpeciesKey)
                ? string.Empty
                : cropSpeciesKey;
            Stage = stage;
            SoilPreparation01 = EnvironmentMath.Clamp01(soilPreparation01);
            Irrigation01 = EnvironmentMath.Clamp01(irrigation01);
            WeedPressure01 = EnvironmentMath.Clamp01(weedPressure01);
            CultivationHealth01 = EnvironmentMath.Clamp01(cultivationHealth01);
            IsActive = isActive;
        }
    }

    // =============================================================================
    // EnvironmentAgriculturalSeedResourceBoundary
    // =============================================================================
    /// <summary>
    /// <para>
    /// Confine dati tra seed bank naturale e semi agricoli concreti futuri.
    /// </para>
    ///
    /// <para><b>Principio architetturale: semi naturali e semi posseduti non coincidono</b></para>
    /// <para>
    /// La seed bank naturale e' pressione ecologica dell'area. I semi agricoli usati
    /// dagli NPC saranno risorse concrete, possedute e consumabili. Questa struttura
    /// dichiara il confine senza introdurre inventario o magazzino.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SeedResourceKey</b>: chiave risorsa concreta futura.</item>
    ///   <item><b>SpeciesKey</b>: specie vegetale che il seme puo' produrre.</item>
    ///   <item><b>Quantity</b>: quantita' intera posseduta o richiesta.</item>
    ///   <item><b>IsConcreteResource</b>: true se appartiene al dominio risorse, non seed bank.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAgriculturalSeedResourceBoundary
    {
        public readonly string SeedResourceKey;
        public readonly string SpeciesKey;
        public readonly int Quantity;
        public readonly bool IsConcreteResource;

        public bool IsUsable =>
            IsConcreteResource
            && Quantity > 0
            && !string.IsNullOrWhiteSpace(SeedResourceKey)
            && !string.IsNullOrWhiteSpace(SpeciesKey);

        // =============================================================================
        // EnvironmentAgriculturalSeedResourceBoundary
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il confine seme agricolo normalizzando la quantita'.
        /// </para>
        /// </summary>
        public EnvironmentAgriculturalSeedResourceBoundary(
            string seedResourceKey,
            string speciesKey,
            int quantity,
            bool isConcreteResource)
        {
            SeedResourceKey = seedResourceKey ?? string.Empty;
            SpeciesKey = speciesKey ?? string.Empty;
            Quantity = quantity < 0 ? 0 : quantity;
            IsConcreteResource = isConcreteResource;
        }
    }

    // =============================================================================
    // EnvironmentHarvestOutput
    // =============================================================================
    /// <summary>
    /// <para>
    /// Output di raccolta potenziale derivato da una pianta matura.
    /// </para>
    ///
    /// <para><b>Principio architetturale: raccolto come dato, non come job</b></para>
    /// <para>
    /// Il raccolto disponibile viene rappresentato come informazione leggibile. Non
    /// crea item, non prenota un NPC e non modifica magazzini. Un job futuro potra'
    /// consumare questo contratto per produrre effetti concreti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PlantId</b>: pianta sorgente.</item>
    ///   <item><b>SpeciesKey</b>: specie della pianta.</item>
    ///   <item><b>ResourceOutputKey</b>: risorsa prodotta dal catalogo.</item>
    ///   <item><b>Amount01</b>: quantita' normalizzata potenziale.</item>
    ///   <item><b>Quality01</b>: qualita' normalizzata potenziale.</item>
    ///   <item><b>IsAvailable</b>: true se il raccolto e' leggibile da hook futuri.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentHarvestOutput
    {
        public readonly EnvironmentPlantId PlantId;
        public readonly string SpeciesKey;
        public readonly string ResourceOutputKey;
        public readonly float Amount01;
        public readonly float Quality01;
        public readonly bool IsAvailable;

        // =============================================================================
        // EnvironmentHarvestOutput
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un output raccolto normalizzando quantita' e qualita'.
        /// </para>
        /// </summary>
        public EnvironmentHarvestOutput(
            EnvironmentPlantId plantId,
            string speciesKey,
            string resourceOutputKey,
            float amount01,
            float quality01,
            bool isAvailable)
        {
            PlantId = plantId;
            SpeciesKey = speciesKey ?? string.Empty;
            ResourceOutputKey = resourceOutputKey ?? string.Empty;
            Amount01 = EnvironmentMath.Clamp01(amount01);
            Quality01 = EnvironmentMath.Clamp01(quality01);
            IsAvailable = isAvailable
                          && plantId.IsValid
                          && !string.IsNullOrWhiteSpace(ResourceOutputKey);
        }
    }

    // =============================================================================
    // EnvironmentAgricultureJobHook
    // =============================================================================
    /// <summary>
    /// <para>
    /// Hook data-only per una futura operazione agricola.
    /// </para>
    ///
    /// <para><b>Principio architetturale: intenzione prima dell'esecuzione job</b></para>
    /// <para>
    /// Questo hook non e' un job. Non riserva actor, non controlla pathfinding e non
    /// consuma risorse. E' solo il punto dati con cui Environment potra' dichiarare
    /// che una certa azione agricola e' sensata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IntentKind</b>: tipo operazione futura.</item>
    ///   <item><b>AreaId</b>: area agricola coinvolta.</item>
    ///   <item><b>PlantId</b>: pianta coinvolta, se presente.</item>
    ///   <item><b>SpeciesKey</b>: specie target.</item>
    ///   <item><b>Cell</b>: cella target.</item>
    ///   <item><b>Priority</b>: priorita' futura puramente dichiarativa.</item>
    ///   <item><b>IsEnabled</b>: gate del suggerimento.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAgricultureJobHook
    {
        public readonly EnvironmentAgricultureIntentKind IntentKind;
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentPlantId PlantId;
        public readonly string SpeciesKey;
        public readonly EnvironmentCellCoord Cell;
        public readonly int Priority;
        public readonly bool IsEnabled;

        // =============================================================================
        // EnvironmentAgricultureJobHook
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un hook agricolo futuro senza avviare alcun job.
        /// </para>
        /// </summary>
        public EnvironmentAgricultureJobHook(
            EnvironmentAgricultureIntentKind intentKind,
            EnvironmentAreaId areaId,
            EnvironmentPlantId plantId,
            string speciesKey,
            EnvironmentCellCoord cell,
            int priority,
            bool isEnabled)
        {
            IntentKind = intentKind;
            AreaId = areaId;
            PlantId = plantId;
            SpeciesKey = speciesKey ?? string.Empty;
            Cell = cell;
            Priority = priority < 0 ? 0 : priority;
            IsEnabled = isEnabled;
        }
    }

    // =============================================================================
    // EnvironmentAgricultureFoundationResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only dei contratti agricoli preparatori.
    /// </para>
    ///
    /// <para><b>Principio architetturale: agricoltura leggibile prima dei sistemi</b></para>
    /// <para>
    /// Il resolver verifica se un seme agricolo puo' essere usato, se una pianta puo'
    /// esporre raccolto e quale hook sarebbe adatto. Non modifica stato, non crea
    /// risorse e non comunica con il Job Layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CanSowSeed</b>: valida confine seme agricolo/crop.</item>
    ///   <item><b>TryBuildHarvestOutput</b>: deriva raccolto potenziale da snapshot pianta.</item>
    ///   <item><b>BuildHook</b>: crea un hook futuro puramente dichiarativo.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentAgricultureFoundationResolver
    {
        // =============================================================================
        // CanSowSeed
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se un confine seme agricolo e' compatibile con il catalogo piante.
        /// </para>
        /// </summary>
        public static bool CanSowSeed(
            EnvironmentAgriculturalSeedResourceBoundary seed,
            EnvironmentPlantCatalog catalog)
        {
            if (!seed.IsUsable || catalog == null)
                return false;

            return catalog.TryGetSpecies(
                       seed.SpeciesKey,
                       out EnvironmentPlantSpeciesDefinition species)
                   && species.Category == EnvironmentPlantCategory.Crop;
        }

        // =============================================================================
        // TryBuildHarvestOutput
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un output raccolto se la pianta e' viva, matura e produttiva.
        /// </para>
        /// </summary>
        public static bool TryBuildHarvestOutput(
            EnvironmentPlantSnapshot plant,
            EnvironmentPlantCatalog catalog,
            out EnvironmentHarvestOutput output)
        {
            output = default;

            if (!plant.IsAlive
                || !plant.IsHarvestable
                || catalog == null
                || !catalog.TryGetSpecies(
                    plant.SpeciesKey,
                    out EnvironmentPlantSpeciesDefinition species)
                || string.IsNullOrWhiteSpace(species.ResourceOutputKey))
            {
                return false;
            }

            float amount = plant.Maturity01 * plant.Health01;
            float quality = (plant.Maturity01 + plant.Health01) * 0.5f;
            output = new EnvironmentHarvestOutput(
                plant.PlantId,
                plant.SpeciesKey,
                species.ResourceOutputKey,
                amount,
                quality,
                true);
            return output.IsAvailable;
        }

        // =============================================================================
        // BuildHook
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un hook agricolo futuro senza produrre effetti runtime.
        /// </para>
        /// </summary>
        public static EnvironmentAgricultureJobHook BuildHook(
            EnvironmentAgricultureIntentKind intentKind,
            EnvironmentAreaId areaId,
            EnvironmentPlantId plantId,
            string speciesKey,
            EnvironmentCellCoord cell,
            int priority,
            bool isEnabled)
        {
            return new EnvironmentAgricultureJobHook(
                intentKind,
                areaId,
                plantId,
                speciesKey,
                cell,
                priority,
                isEnabled);
        }
    }
}
