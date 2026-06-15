using System;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentFoundationConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile radice della Environment Foundation.
    /// </para>
    ///
    /// <para><b>Principio architetturale: un solo documento dati per il bootstrap</b></para>
    /// <para>
    /// I futuri file di configurazione devono poter descrivere calendario, clima,
    /// aree e tick iniziale tramite un solo oggetto radice. Questo DTO non carica
    /// file e non conosce Unity Resources: definisce soltanto la forma dei dati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>schemaVersion</b>: versione dello schema dati.</item>
    ///   <item><b>configKey</b>: chiave leggibile del documento.</item>
    ///   <item><b>initialEnvironmentTicks</b>: tick ambientale iniziale.</item>
    ///   <item><b>calendar</b>: configurazione calendario.</item>
    ///   <item><b>climate</b>: configurazione clima globale.</item>
    ///   <item><b>areas</b>: set aree ambientali.</item>
    ///   <item><b>plantCatalog</b>: catalogo specie vegetali importanti.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentFoundationConfig
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string configKey = "default_environment";
        public long initialEnvironmentTicks;
        public EnvironmentCalendarConfig calendar = new EnvironmentCalendarConfig();
        public EnvironmentClimateConfig climate = new EnvironmentClimateConfig();
        public EnvironmentAreaSetConfig areas = new EnvironmentAreaSetConfig();
        public EnvironmentPlantCatalogConfig plantCatalog = new EnvironmentPlantCatalogConfig();

        // =============================================================================
        // ResolveInitialEnvironmentTicks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il tick iniziale normalizzando valori negativi a zero.
        /// </para>
        /// </summary>
        public long ResolveInitialEnvironmentTicks()
        {
            return initialEnvironmentTicks < 0 ? 0 : initialEnvironmentTicks;
        }

        // =============================================================================
        // ResolveSchemaVersion
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la versione schema normalizzando valori non positivi.
        /// </para>
        /// </summary>
        public int ResolveSchemaVersion()
        {
            return schemaVersion > 0 ? schemaVersion : CurrentSchemaVersion;
        }

        // =============================================================================
        // ResolveConfigKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce una chiave config leggibile con fallback stabile.
        /// </para>
        /// </summary>
        public string ResolveConfigKey()
        {
            return string.IsNullOrWhiteSpace(configKey)
                ? "default_environment"
                : configKey;
        }
    }

    // =============================================================================
    // EnvironmentFoundationBootstrapResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato completo del bootstrap data-only della foundation ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: validazione e build restano osservabili</b></para>
    /// <para>
    /// Il bootstrap aggrega validazione, build e snapshot senza nascondere i passaggi.
    /// Il chiamante puo' decidere se accettare uno stato costruito con warning o
    /// bloccare la pipeline in presenza di errori.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Validation</b>: diagnostica della configurazione.</item>
    ///   <item><b>Build</b>: stato e report builder.</item>
    ///   <item><b>Snapshot</b>: snapshot read-only iniziale.</item>
    ///   <item><b>IsValid</b>: assenza di errori di validazione.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentFoundationBootstrapResult
    {
        public EnvironmentConfigValidationResult Validation { get; }
        public EnvironmentFoundationBuildResult Build { get; }
        public EnvironmentSnapshot Snapshot { get; }
        public EnvironmentPlantCatalog PlantCatalog { get; }
        public bool IsValid => Validation == null || Validation.IsValid;

        // =============================================================================
        // EnvironmentFoundationBootstrapResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato aggregato del bootstrap.
        /// </para>
        /// </summary>
        public EnvironmentFoundationBootstrapResult(
            EnvironmentConfigValidationResult validation,
            EnvironmentFoundationBuildResult build,
            EnvironmentSnapshot snapshot,
            EnvironmentPlantCatalog plantCatalog)
        {
            Validation = validation;
            Build = build ?? new EnvironmentFoundationBuildResult(
                new EnvironmentState(),
                new EnvironmentFoundationBuildReport(0, 0, 0, 0, 0, 0));
            Snapshot = snapshot ?? Build.State.CreateSnapshot();
            PlantCatalog = plantCatalog ?? new EnvironmentPlantCatalog(null);
        }
    }

    // =============================================================================
    // EnvironmentFoundationBootstrap
    // =============================================================================
    /// <summary>
    /// <para>
    /// Pipeline data-only di bootstrap della Environment Foundation.
    /// </para>
    ///
    /// <para><b>Principio architetturale: loader futuro sottile</b></para>
    /// <para>
    /// Quando esistera' un loader JSON o asset, dovra' solo popolare
    /// <see cref="EnvironmentFoundationConfig"/> e passarlo qui. La logica di
    /// validazione e build resta confinata nel Core Environment.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Bootstrap</b>: valida e costruisce da root config.</item>
    ///   <item><b>CreateDefaultConfig</b>: crea una config radice vuota ma completa.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentFoundationBootstrap
    {
        // =============================================================================
        // Bootstrap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida e costruisce lo stato iniziale della foundation da config radice.
        /// </para>
        /// </summary>
        public static EnvironmentFoundationBootstrapResult Bootstrap(
            EnvironmentFoundationConfig config)
        {
            var safeConfig = config ?? CreateDefaultConfig();
            var validation = EnvironmentConfigValidator.Validate(safeConfig);
            var build = EnvironmentFoundationBuilder.BuildState(
                safeConfig.ResolveInitialEnvironmentTicks(),
                safeConfig.calendar,
                safeConfig.climate,
                safeConfig.areas);
            var snapshot = build.State.CreateSnapshot();
            var plantCatalog = safeConfig.plantCatalog != null
                ? safeConfig.plantCatalog.ToCatalog()
                : new EnvironmentPlantCatalogConfig().ToCatalog();

            return new EnvironmentFoundationBootstrapResult(
                validation,
                build,
                snapshot,
                plantCatalog);
        }

        // =============================================================================
        // CreateDefaultConfig
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una configurazione radice completa con default vuoti.
        /// </para>
        /// </summary>
        public static EnvironmentFoundationConfig CreateDefaultConfig()
        {
            return new EnvironmentFoundationConfig
            {
                schemaVersion = EnvironmentFoundationConfig.CurrentSchemaVersion,
                configKey = "default_environment",
                initialEnvironmentTicks = 0,
                calendar = new EnvironmentCalendarConfig(),
                climate = new EnvironmentClimateConfig(),
                areas = new EnvironmentAreaSetConfig(),
                plantCatalog = new EnvironmentPlantCatalogConfig()
            };
        }
    }
}
