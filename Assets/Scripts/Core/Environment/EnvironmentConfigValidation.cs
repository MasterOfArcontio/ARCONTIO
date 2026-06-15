using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentConfigValidationSeverity
    // =============================================================================
    /// <summary>
    /// <para>
    /// Severita' di una diagnostica di configurazione ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica leggibile prima del loader</b></para>
    /// <para>
    /// Il Core Environment non deve decidere subito come mostrare o bloccare un
    /// errore. La severita' permette a loader, test o strumenti editor futuri di
    /// scegliere policy diverse senza cambiare il validatore.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Info</b>: nota informativa non bloccante.</item>
    ///   <item><b>Warning</b>: dato sospetto ma correggibile via fallback.</item>
    ///   <item><b>Error</b>: dato incoerente che puo' perdere stato o produrre aree invisibili.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentConfigValidationSeverity
    {
        Info = 0,
        Warning = 10,
        Error = 20
    }

    // =============================================================================
    // EnvironmentConfigValidationIssue
    // =============================================================================
    /// <summary>
    /// <para>
    /// Singola diagnostica prodotta dalla validazione ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: codici stabili, messaggi sostituibili</b></para>
    /// <para>
    /// Il codice tecnico resta stabile per test e tooling, mentre il messaggio puo'
    /// essere tradotto o riscritto in futuro. L'eventuale area id aiuta a collegare
    /// il problema alla riga o entry del file di configurazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Code</b>: codice diagnostico stabile.</item>
    ///   <item><b>Severity</b>: severita' del problema.</item>
    ///   <item><b>AreaId</b>: area coinvolta, oppure zero se globale.</item>
    ///   <item><b>Message</b>: descrizione leggibile.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentConfigValidationIssue
    {
        public readonly string Code;
        public readonly EnvironmentConfigValidationSeverity Severity;
        public readonly EnvironmentAreaId AreaId;
        public readonly string Message;

        // =============================================================================
        // EnvironmentConfigValidationIssue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica normalizzando stringhe nulle.
        /// </para>
        /// </summary>
        public EnvironmentConfigValidationIssue(
            string code,
            EnvironmentConfigValidationSeverity severity,
            EnvironmentAreaId areaId,
            string message)
        {
            Code = code ?? string.Empty;
            Severity = severity;
            AreaId = areaId;
            Message = message ?? string.Empty;
        }
    }

    // =============================================================================
    // EnvironmentConfigValidationResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato aggregato della validazione di configurazioni ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: validazione read-only e materializzata</b></para>
    /// <para>
    /// Il risultato copia le issue in una lista read-only per evitare che il
    /// chiamante modifichi accidentalmente la diagnostica gia' prodotta. Il Core non
    /// lancia eccezioni per dati recuperabili: espone conteggi e flag.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Issues</b>: diagnostiche materializzate.</item>
    ///   <item><b>ErrorCount</b>: numero di errori bloccanti.</item>
    ///   <item><b>WarningCount</b>: numero di warning.</item>
    ///   <item><b>InfoCount</b>: numero di note informative.</item>
    ///   <item><b>IsValid</b>: assenza di errori.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentConfigValidationResult
    {
        private static readonly EnvironmentConfigValidationIssue[] EmptyIssues =
            new EnvironmentConfigValidationIssue[0];

        public IReadOnlyList<EnvironmentConfigValidationIssue> Issues { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }
        public int InfoCount { get; }
        public bool IsValid => ErrorCount == 0;

        // =============================================================================
        // EnvironmentConfigValidationResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato calcolando i conteggi per severita'.
        /// </para>
        /// </summary>
        public EnvironmentConfigValidationResult(
            IReadOnlyList<EnvironmentConfigValidationIssue> issues)
        {
            Issues = issues ?? EmptyIssues;

            // I conteggi vengono calcolati una volta sola: i consumer futuri possono
            // leggere IsValid o WarningCount senza riscorrere tutta la lista.
            for (int i = 0; i < Issues.Count; i++)
            {
                if (Issues[i].Severity == EnvironmentConfigValidationSeverity.Error)
                    ErrorCount++;
                else if (Issues[i].Severity == EnvironmentConfigValidationSeverity.Warning)
                    WarningCount++;
                else
                    InfoCount++;
            }
        }
    }

    // =============================================================================
    // EnvironmentConfigValidator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Validatore data-only delle configurazioni della Environment Foundation.
    /// </para>
    ///
    /// <para><b>Principio architetturale: controlli prima della costruzione stato</b></para>
    /// <para>
    /// Il builder puo' applicare fallback e ignorare entry non valide, ma il loader
    /// futuro avra' bisogno di sapere perche' una configurazione e' sospetta. Questo
    /// validatore ispeziona DTO calendario, clima e aree senza mutarli.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Validate</b>: valida calendario, clima, set aree e catalogo piante.</item>
    ///   <item><b>ValidateCalendar</b>: segnala valori temporali corretti via fallback.</item>
    ///   <item><b>ValidateClimate</b>: segnala profili clima mancanti o sospetti.</item>
    ///   <item><b>ValidateAreaSet</b>: segnala id duplicati, payload orfani e entry nulle.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentConfigValidator
    {
        // =============================================================================
        // Validate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida una configurazione radice della foundation ambientale.
        /// </para>
        /// </summary>
        public static EnvironmentConfigValidationResult Validate(
            EnvironmentFoundationConfig config)
        {
            var issues = new List<EnvironmentConfigValidationIssue>();

            if (config == null)
            {
                AddIssue(
                    issues,
                    "ENV_FOUNDATION_CONFIG_MISSING",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "Config radice assente: verra' usata una configurazione default.");
                ValidateCalendar(null, issues);
                ValidateClimate(null, issues);
                ValidateAreaSet(null, issues);
                ValidatePlantCatalog(null, issues);
                return new EnvironmentConfigValidationResult(issues);
            }

            ValidateFoundationRoot(config, issues);
            ValidateCalendar(config.calendar, issues);
            ValidateClimate(config.climate, issues);
            ValidateAreaSet(config.areas, issues);
            ValidatePlantCatalog(config.plantCatalog, issues);

            return new EnvironmentConfigValidationResult(issues);
        }

        // =============================================================================
        // Validate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida l'insieme delle configurazioni ambientali principali.
        /// </para>
        /// </summary>
        public static EnvironmentConfigValidationResult Validate(
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig,
            EnvironmentAreaSetConfig areaSetConfig)
        {
            var issues = new List<EnvironmentConfigValidationIssue>();

            ValidateCalendar(calendarConfig, issues);
            ValidateClimate(climateConfig, issues);
            ValidateAreaSet(areaSetConfig, issues);

            return new EnvironmentConfigValidationResult(issues);
        }

        private static void ValidateFoundationRoot(
            EnvironmentFoundationConfig config,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (config.schemaVersion <= 0)
            {
                AddIssue(
                    issues,
                    "ENV_FOUNDATION_SCHEMA_VERSION_INVALID",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "schemaVersion non positivo: verra' normalizzato alla versione corrente.");
            }
            else if (config.schemaVersion != EnvironmentFoundationConfig.CurrentSchemaVersion)
            {
                AddIssue(
                    issues,
                    "ENV_FOUNDATION_SCHEMA_VERSION_MISMATCH",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "schemaVersion diverso dalla versione corrente della foundation.");
            }

            if (string.IsNullOrWhiteSpace(config.configKey))
            {
                AddIssue(
                    issues,
                    "ENV_FOUNDATION_CONFIG_KEY_EMPTY",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "configKey vuota: verra' usato un fallback leggibile.");
            }

            if (config.initialEnvironmentTicks < 0)
            {
                AddIssue(
                    issues,
                    "ENV_FOUNDATION_INITIAL_TICKS_NEGATIVE",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "initialEnvironmentTicks negativo: verra' normalizzato a zero.");
            }
        }

        private static void ValidateCalendar(
            EnvironmentCalendarConfig calendarConfig,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (calendarConfig == null)
            {
                AddIssue(
                    issues,
                    "ENV_CALENDAR_MISSING",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "Calendario assente: verranno usati i default della foundation.");
                return;
            }

            // I resolver hanno fallback, ma segnaliamo comunque valori non positivi
            // per evitare che un file config sembri applicato quando in realta' no.
            AddNonPositiveWarning(issues, calendarConfig.hoursPerDay, "ENV_CALENDAR_HOURS_PER_DAY", "hoursPerDay");
            AddNonPositiveWarning(issues, calendarConfig.calendarTicksPerSimulatedHour, "ENV_CALENDAR_TICKS_PER_HOUR", "calendarTicksPerSimulatedHour");
            AddNonPositiveWarning(issues, calendarConfig.daysPerMonth, "ENV_CALENDAR_DAYS_PER_MONTH", "daysPerMonth");
            AddNonPositiveWarning(issues, calendarConfig.monthsPerYear, "ENV_CALENDAR_MONTHS_PER_YEAR", "monthsPerYear");
            AddNonPositiveWarning(issues, calendarConfig.monthsPerSeason, "ENV_CALENDAR_MONTHS_PER_SEASON", "monthsPerSeason");

            if (calendarConfig.seasonProfiles == null || calendarConfig.seasonProfiles.Length == 0)
            {
                AddIssue(
                    issues,
                    "ENV_CALENDAR_SEASONS_MISSING",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "Profili stagionali assenti: il resolver usera' il set default.");
            }
        }

        private static void ValidateClimate(
            EnvironmentClimateConfig climateConfig,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (climateConfig == null)
            {
                AddIssue(
                    issues,
                    "ENV_CLIMATE_MISSING",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "Clima assente: verranno usati i default della foundation.");
                return;
            }

            if (climateConfig.seasonClimateProfiles == null || climateConfig.seasonClimateProfiles.Length == 0)
            {
                AddIssue(
                    issues,
                    "ENV_CLIMATE_SEASONS_MISSING",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "Profili climatici stagionali assenti: il resolver usera' il set default.");
            }

            if (climateConfig.weatherPersistence01 < 0f || climateConfig.weatherPersistence01 > 1f)
            {
                AddIssue(
                    issues,
                    "ENV_CLIMATE_WEATHER_PERSISTENCE_RANGE",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "weatherPersistence01 e' fuori range 0..1 e verra' normalizzato.");
            }

            if (climateConfig.hourlyTemperatureVariation01 < 0f || climateConfig.hourlyTemperatureVariation01 > 1f)
            {
                AddIssue(
                    issues,
                    "ENV_CLIMATE_HOURLY_VARIATION_RANGE",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "hourlyTemperatureVariation01 e' fuori range 0..1 e verra' normalizzato.");
            }
        }

        private static void ValidateAreaSet(
            EnvironmentAreaSetConfig areaSetConfig,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (areaSetConfig == null)
            {
                AddIssue(
                    issues,
                    "ENV_AREA_SET_MISSING",
                    EnvironmentConfigValidationSeverity.Info,
                    EnvironmentAreaId.None,
                    "Set aree assente: lo stato ambientale partira' senza aree configurate.");
                return;
            }

            var declaredAreaIds = new HashSet<int>();
            var duplicatedAreaIds = new HashSet<int>();
            ValidateAreaDefinitions(areaSetConfig.areas, declaredAreaIds, duplicatedAreaIds, issues);
            ValidateFertilityPayloads(areaSetConfig.fertilityAreas, declaredAreaIds, issues);
            ValidateWaterPayloads(areaSetConfig.waterAreas, declaredAreaIds, issues);
            ValidateVegetationPayloads(areaSetConfig.vegetationAreas, declaredAreaIds, issues);
            ValidateSeedBankPayloads(areaSetConfig.seedBankAreas, declaredAreaIds, issues);
        }

        private static void ValidatePlantCatalog(
            EnvironmentPlantCatalogConfig plantCatalogConfig,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (plantCatalogConfig == null)
            {
                AddIssue(
                    issues,
                    "ENV_PLANT_CATALOG_MISSING",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "Catalogo piante assente: verra' usato il catalogo default.");
                return;
            }

            var species = plantCatalogConfig.species;
            if (species == null || species.Length == 0)
            {
                AddIssue(
                    issues,
                    "ENV_PLANT_CATALOG_EMPTY",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "Catalogo piante senza specie: nessuna PlantInstance futura potra' risolvere specie configurate.");
                return;
            }

            var keys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var duplicates = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < species.Length; i++)
            {
                var entry = species[i];
                if (entry == null)
                {
                    AddIssue(
                        issues,
                        "ENV_PLANT_SPECIES_NULL",
                        EnvironmentConfigValidationSeverity.Warning,
                        EnvironmentAreaId.None,
                        "Specie vegetale nulla: verra' ignorata dal catalogo.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.speciesKey))
                {
                    AddIssue(
                        issues,
                        "ENV_PLANT_SPECIES_KEY_EMPTY",
                        EnvironmentConfigValidationSeverity.Warning,
                        EnvironmentAreaId.None,
                        "Specie vegetale senza speciesKey: verra' ignorata dal catalogo.");
                }
                else if (!keys.Add(entry.speciesKey) && duplicates.Add(entry.speciesKey))
                {
                    AddIssue(
                        issues,
                        "ENV_PLANT_SPECIES_KEY_DUPLICATE",
                        EnvironmentConfigValidationSeverity.Warning,
                        EnvironmentAreaId.None,
                        "Specie vegetale duplicata: il catalogo usera' la prima definizione.");
                }

                ValidatePlantGrowthStages(entry, issues);
            }
        }

        private static void ValidatePlantGrowthStages(
            EnvironmentPlantSpeciesConfig species,
            List<EnvironmentConfigValidationIssue> issues)
        {
            var stages = species.growthStages;
            if (stages == null || stages.Length == 0)
            {
                AddIssue(
                    issues,
                    "ENV_PLANT_GROWTH_STAGES_EMPTY",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "Specie vegetale senza stadi crescita: il lifecycle futuro non avra' soglie.");
                return;
            }

            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] != null)
                    continue;

                AddIssue(
                    issues,
                    "ENV_PLANT_GROWTH_STAGE_NULL",
                    EnvironmentConfigValidationSeverity.Warning,
                    EnvironmentAreaId.None,
                    "Stadio crescita nullo: verra' normalizzato dal catalogo.");
            }
        }

        private static void ValidateAreaDefinitions(
            EnvironmentAreaConfig[] areas,
            HashSet<int> declaredAreaIds,
            HashSet<int> duplicatedAreaIds,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (areas == null)
                return;

            for (int i = 0; i < areas.Length; i++)
            {
                var area = areas[i];
                if (area == null)
                {
                    AddIssue(
                        issues,
                        "ENV_AREA_NULL",
                        EnvironmentConfigValidationSeverity.Error,
                        EnvironmentAreaId.None,
                        "Definizione area nulla.");
                    continue;
                }

                if (area.areaId <= 0)
                {
                    AddIssue(
                        issues,
                        "ENV_AREA_ID_INVALID",
                        EnvironmentConfigValidationSeverity.Error,
                        new EnvironmentAreaId(area.areaId),
                        "Definizione area con id non valido.");
                    continue;
                }

                if (!declaredAreaIds.Add(area.areaId) && duplicatedAreaIds.Add(area.areaId))
                {
                    AddIssue(
                        issues,
                        "ENV_AREA_ID_DUPLICATE",
                        EnvironmentConfigValidationSeverity.Error,
                        new EnvironmentAreaId(area.areaId),
                        "Definizione area duplicata.");
                }
            }
        }

        private static void ValidateFertilityPayloads(
            EnvironmentFertilityAreaConfig[] payloads,
            HashSet<int> declaredAreaIds,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (payloads == null)
                return;

            for (int i = 0; i < payloads.Length; i++)
            {
                if (payloads[i] == null)
                {
                    AddNullPayloadIssue(issues, "ENV_FERTILITY_NULL", "Payload fertilita' nullo.");
                    continue;
                }

                ValidatePayloadAreaId(
                    payloads[i].areaId,
                    declaredAreaIds,
                    "ENV_FERTILITY_AREA_ID_INVALID",
                    "ENV_FERTILITY_AREA_ORPHAN",
                    "Payload fertilita' con id area non valido.",
                    "Payload fertilita' collegato a un'area non dichiarata.",
                    issues);
            }
        }

        private static void ValidateWaterPayloads(
            EnvironmentWaterAreaConfig[] payloads,
            HashSet<int> declaredAreaIds,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (payloads == null)
                return;

            for (int i = 0; i < payloads.Length; i++)
            {
                if (payloads[i] == null)
                {
                    AddNullPayloadIssue(issues, "ENV_WATER_NULL", "Payload acqua nullo.");
                    continue;
                }

                ValidatePayloadAreaId(
                    payloads[i].areaId,
                    declaredAreaIds,
                    "ENV_WATER_AREA_ID_INVALID",
                    "ENV_WATER_AREA_ORPHAN",
                    "Payload acqua con id area non valido.",
                    "Payload acqua collegato a un'area non dichiarata.",
                    issues);
            }
        }

        private static void ValidateVegetationPayloads(
            EnvironmentVegetationAreaConfig[] payloads,
            HashSet<int> declaredAreaIds,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (payloads == null)
                return;

            for (int i = 0; i < payloads.Length; i++)
            {
                if (payloads[i] == null)
                {
                    AddNullPayloadIssue(issues, "ENV_VEGETATION_NULL", "Payload vegetazione nullo.");
                    continue;
                }

                ValidatePayloadAreaId(
                    payloads[i].areaId,
                    declaredAreaIds,
                    "ENV_VEGETATION_AREA_ID_INVALID",
                    "ENV_VEGETATION_AREA_ORPHAN",
                    "Payload vegetazione con id area non valido.",
                    "Payload vegetazione collegato a un'area non dichiarata.",
                    issues);
            }
        }

        private static void ValidateSeedBankPayloads(
            EnvironmentSeedBankAreaConfig[] payloads,
            HashSet<int> declaredAreaIds,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (payloads == null)
                return;

            for (int i = 0; i < payloads.Length; i++)
            {
                if (payloads[i] == null)
                {
                    AddNullPayloadIssue(issues, "ENV_SEED_BANK_NULL", "Payload seed bank nullo.");
                    continue;
                }

                ValidatePayloadAreaId(
                    payloads[i].areaId,
                    declaredAreaIds,
                    "ENV_SEED_BANK_AREA_ID_INVALID",
                    "ENV_SEED_BANK_AREA_ORPHAN",
                    "Payload seed bank con id area non valido.",
                    "Payload seed bank collegato a un'area non dichiarata.",
                    issues);

                var entries = payloads[i].entries;
                if (entries == null || entries.Length == 0)
                {
                    AddIssue(
                        issues,
                        "ENV_SEED_BANK_ENTRIES_EMPTY",
                        EnvironmentConfigValidationSeverity.Warning,
                        new EnvironmentAreaId(payloads[i].areaId),
                        "Payload seed bank senza entry: la pressione ecologica sara' nulla.");
                    continue;
                }

                for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                {
                    if (entries[entryIndex] != null)
                        continue;

                    AddIssue(
                        issues,
                        "ENV_SEED_BANK_ENTRY_NULL",
                        EnvironmentConfigValidationSeverity.Warning,
                        new EnvironmentAreaId(payloads[i].areaId),
                        "Entry seed bank nulla: verra' normalizzata a pressione zero.");
                }
            }
        }

        private static void ValidatePayloadAreaId(
            int areaId,
            HashSet<int> declaredAreaIds,
            string invalidCode,
            string orphanCode,
            string invalidMessage,
            string orphanMessage,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (areaId <= 0)
            {
                AddIssue(
                    issues,
                    invalidCode,
                    EnvironmentConfigValidationSeverity.Error,
                    new EnvironmentAreaId(areaId),
                    invalidMessage);
                return;
            }

            if (!declaredAreaIds.Contains(areaId))
            {
                AddIssue(
                    issues,
                    orphanCode,
                    EnvironmentConfigValidationSeverity.Error,
                    new EnvironmentAreaId(areaId),
                    orphanMessage);
            }
        }

        private static void AddNonPositiveWarning(
            List<EnvironmentConfigValidationIssue> issues,
            int value,
            string code,
            string fieldName)
        {
            if (value > 0)
                return;

            AddIssue(
                issues,
                code,
                EnvironmentConfigValidationSeverity.Warning,
                EnvironmentAreaId.None,
                fieldName + " e' non positivo e verra' sostituito dal default.");
        }

        private static void AddNullPayloadIssue(
            List<EnvironmentConfigValidationIssue> issues,
            string code,
            string message)
        {
            AddIssue(
                issues,
                code,
                EnvironmentConfigValidationSeverity.Error,
                EnvironmentAreaId.None,
                message);
        }

        private static void AddIssue(
            List<EnvironmentConfigValidationIssue> issues,
            string code,
            EnvironmentConfigValidationSeverity severity,
            EnvironmentAreaId areaId,
            string message)
        {
            issues.Add(new EnvironmentConfigValidationIssue(
                code,
                severity,
                areaId,
                message));
        }
    }
}
