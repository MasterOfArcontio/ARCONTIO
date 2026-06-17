using System;
using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentBiomeProfile
    // =============================================================================
    /// <summary>
    /// <para>
    /// Profilo biome value-only usato per parametrizzare l'equilibrio ecologico.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bioma come sorgente di target, non come eccezione</b></para>
    /// <para>
    /// La biosfera non deve avere una formula hardcoded valida solo per un prato
    /// temperato. Il profilo biome descrive i target verso cui vegetazione, seed
    /// bank e crescita naturale tendono nel lungo periodo. Deserto, giungla, tundra
    /// e grassland possono cosi' condividere la stessa pipeline con parametri
    /// differenti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BiomeKey</b>: chiave stabile del bioma.</item>
    ///   <item><b>Target*</b>: densita', salute, seed bank e fertilita' attese in equilibrio.</item>
    ///   <item><b>BaseMoisture01</b>: umidita' locale strutturale del bioma.</item>
    ///   <item><b>*Resistance01</b>: resistenze a siccita', freddo e caldo.</item>
    ///   <item><b>Seasonality01</b>: quanto la stagione deve incidere.</item>
    ///   <item><b>NaturalRecoveryRate01</b>: velocita' di ritorno verso l'equilibrio.</item>
    ///   <item><b>DisturbanceSensitivity01</b>: sensibilita' a stress e disturbi.</item>
    ///   <item><b>MaxPlantInstancesPerArea</b>: limite diagnostico per piante importanti.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentBiomeProfile
    {
        private static readonly string[] EmptySpeciesKeys = new string[0];

        public readonly string BiomeKey;
        public readonly float TargetFertility01;
        public readonly float TargetVegetationDensity01;
        public readonly float TargetVegetationHealth01;
        public readonly float TargetSeedBankAmount01;
        public readonly float TargetSeedBankViability01;
        public readonly float BaseMoisture01;
        public readonly float DroughtResistance01;
        public readonly float ColdResistance01;
        public readonly float HeatResistance01;
        public readonly float Seasonality01;
        public readonly float NaturalRecoveryRate01;
        public readonly float DisturbanceSensitivity01;
        public readonly int MaxPlantInstancesPerArea;
        public readonly string[] AllowedPlantSpeciesKeys;
        public readonly string[] PreferredSeedBankSpeciesKeys;

        public bool IsValid => !string.IsNullOrWhiteSpace(BiomeKey);

        // =============================================================================
        // EnvironmentBiomeProfile
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un profilo biome normalizzando i valori nel range atteso.
        /// </para>
        /// </summary>
        public EnvironmentBiomeProfile(
            string biomeKey,
            float targetFertility01,
            float targetVegetationDensity01,
            float targetVegetationHealth01,
            float targetSeedBankAmount01,
            float targetSeedBankViability01,
            float baseMoisture01,
            float droughtResistance01,
            float coldResistance01,
            float heatResistance01,
            float seasonality01,
            float naturalRecoveryRate01,
            float disturbanceSensitivity01,
            int maxPlantInstancesPerArea,
            string[] allowedPlantSpeciesKeys = null,
            string[] preferredSeedBankSpeciesKeys = null)
        {
            BiomeKey = string.IsNullOrWhiteSpace(biomeKey)
                ? "temperate_grassland"
                : biomeKey;
            TargetFertility01 = EnvironmentMath.Clamp01(targetFertility01);
            TargetVegetationDensity01 = EnvironmentMath.Clamp01(targetVegetationDensity01);
            TargetVegetationHealth01 = EnvironmentMath.Clamp01(targetVegetationHealth01);
            TargetSeedBankAmount01 = EnvironmentMath.Clamp01(targetSeedBankAmount01);
            TargetSeedBankViability01 = EnvironmentMath.Clamp01(targetSeedBankViability01);
            BaseMoisture01 = EnvironmentMath.Clamp01(baseMoisture01);
            DroughtResistance01 = EnvironmentMath.Clamp01(droughtResistance01);
            ColdResistance01 = EnvironmentMath.Clamp01(coldResistance01);
            HeatResistance01 = EnvironmentMath.Clamp01(heatResistance01);
            Seasonality01 = EnvironmentMath.Clamp01(seasonality01);
            NaturalRecoveryRate01 = EnvironmentMath.Clamp01(naturalRecoveryRate01);
            DisturbanceSensitivity01 = EnvironmentMath.Clamp01(disturbanceSensitivity01);
            MaxPlantInstancesPerArea = maxPlantInstancesPerArea < 0 ? 0 : maxPlantInstancesPerArea;
            AllowedPlantSpeciesKeys = CopySpeciesKeys(allowedPlantSpeciesKeys);
            PreferredSeedBankSpeciesKeys = CopySpeciesKeys(preferredSeedBankSpeciesKeys);
        }

        // =============================================================================
        // AllowsPlantSpecies
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se una specie puo' essere reclutata come PlantInstance nel bioma.
        /// </para>
        /// </summary>
        public bool AllowsPlantSpecies(string speciesKey)
        {
            if (AllowedPlantSpeciesKeys == null || AllowedPlantSpeciesKeys.Length == 0)
                return true;

            if (string.IsNullOrWhiteSpace(speciesKey))
                return false;

            for (int i = 0; i < AllowedPlantSpeciesKeys.Length; i++)
            {
                if (string.Equals(
                    AllowedPlantSpeciesKeys[i],
                    speciesKey,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // =============================================================================
        // CreateTemperateGrassland
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il profilo di riferimento per un prato temperato fertile.
        /// </para>
        /// </summary>
        public static EnvironmentBiomeProfile CreateTemperateGrassland()
        {
            return new EnvironmentBiomeProfile(
                "temperate_grassland",
                0.72f,
                0.72f,
                0.78f,
                0.76f,
                0.72f,
                0.52f,
                0.40f,
                0.45f,
                0.55f,
                0.55f,
                0.62f,
                0.45f,
                96,
                new[] { "wild_grass", "oak_tree" },
                new[] { "wild_grass", "oak_tree" });
        }

        public static EnvironmentBiomeProfile CreateDesert()
        {
            return new EnvironmentBiomeProfile(
                "desert",
                0.22f,
                0.10f,
                0.58f,
                0.24f,
                0.62f,
                0.08f,
                0.92f,
                0.25f,
                0.86f,
                0.25f,
                0.28f,
                0.30f,
                20,
                new[] { "wild_grass" },
                new[] { "wild_grass" });
        }

        public static EnvironmentBiomeProfile CreateJungle()
        {
            return new EnvironmentBiomeProfile(
                "jungle",
                0.82f,
                0.94f,
                0.88f,
                0.90f,
                0.84f,
                0.88f,
                0.30f,
                0.18f,
                0.78f,
                0.22f,
                0.84f,
                0.72f,
                180,
                new[] { "wild_grass", "oak_tree" },
                new[] { "oak_tree", "wild_grass" });
        }

        public static EnvironmentBiomeProfile CreateTundra()
        {
            return new EnvironmentBiomeProfile(
                "tundra",
                0.34f,
                0.24f,
                0.62f,
                0.34f,
                0.55f,
                0.32f,
                0.58f,
                0.88f,
                0.18f,
                0.86f,
                0.22f,
                0.56f,
                36,
                new[] { "wild_grass" },
                new[] { "wild_grass" });
        }

        public static EnvironmentBiomeProfile Default => CreateTemperateGrassland();

        private static string[] CopySpeciesKeys(string[] source)
        {
            if (source == null || source.Length == 0)
                return EmptySpeciesKeys;

            var buffer = new List<string>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i]))
                    continue;

                buffer.Add(source[i]);
            }

            return buffer.Count == 0 ? EmptySpeciesKeys : buffer.ToArray();
        }
    }

    // =============================================================================
    // EnvironmentBiomeProfileConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile di un profilo biome.
    /// </para>
    ///
    /// <para><b>Principio architetturale: biomi pronti per file di configurazione</b></para>
    /// <para>
    /// I valori biologici non devono restare nel codice. Questo DTO permette a un
    /// loader futuro di popolare biome da JSON o asset senza cambiare i resolver.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>biomeKey</b>: chiave del profilo.</item>
    ///   <item><b>target*</b>: target ecologici di equilibrio.</item>
    ///   <item><b>resistance/rate</b>: parametri di risposta a stress e recupero.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentBiomeProfileConfig
    {
        public string biomeKey = "temperate_grassland";
        public float targetFertility01 = 0.72f;
        public float targetVegetationDensity01 = 0.72f;
        public float targetVegetationHealth01 = 0.78f;
        public float targetSeedBankAmount01 = 0.76f;
        public float targetSeedBankViability01 = 0.72f;
        public float baseMoisture01 = 0.52f;
        public float droughtResistance01 = 0.40f;
        public float coldResistance01 = 0.45f;
        public float heatResistance01 = 0.55f;
        public float seasonality01 = 0.55f;
        public float naturalRecoveryRate01 = 0.62f;
        public float disturbanceSensitivity01 = 0.45f;
        public int maxPlantInstancesPerArea = 96;
        public string[] allowedPlantSpeciesKeys = { "wild_grass", "oak_tree" };
        public string[] preferredSeedBankSpeciesKeys = { "wild_grass", "oak_tree" };

        public EnvironmentBiomeProfile ToProfile()
        {
            return new EnvironmentBiomeProfile(
                biomeKey,
                targetFertility01,
                targetVegetationDensity01,
                targetVegetationHealth01,
                targetSeedBankAmount01,
                targetSeedBankViability01,
                baseMoisture01,
                droughtResistance01,
                coldResistance01,
                heatResistance01,
                seasonality01,
                naturalRecoveryRate01,
                disturbanceSensitivity01,
                maxPlantInstancesPerArea,
                allowedPlantSpeciesKeys,
                preferredSeedBankSpeciesKeys);
        }

        public static EnvironmentBiomeProfileConfig[] CreateDefaultSet()
        {
            return new[]
            {
                FromProfile(EnvironmentBiomeProfile.CreateTemperateGrassland()),
                FromProfile(EnvironmentBiomeProfile.CreateDesert()),
                FromProfile(EnvironmentBiomeProfile.CreateJungle()),
                FromProfile(EnvironmentBiomeProfile.CreateTundra())
            };
        }

        private static EnvironmentBiomeProfileConfig FromProfile(EnvironmentBiomeProfile profile)
        {
            return new EnvironmentBiomeProfileConfig
            {
                biomeKey = profile.BiomeKey,
                targetFertility01 = profile.TargetFertility01,
                targetVegetationDensity01 = profile.TargetVegetationDensity01,
                targetVegetationHealth01 = profile.TargetVegetationHealth01,
                targetSeedBankAmount01 = profile.TargetSeedBankAmount01,
                targetSeedBankViability01 = profile.TargetSeedBankViability01,
                baseMoisture01 = profile.BaseMoisture01,
                droughtResistance01 = profile.DroughtResistance01,
                coldResistance01 = profile.ColdResistance01,
                heatResistance01 = profile.HeatResistance01,
                seasonality01 = profile.Seasonality01,
                naturalRecoveryRate01 = profile.NaturalRecoveryRate01,
                disturbanceSensitivity01 = profile.DisturbanceSensitivity01,
                maxPlantInstancesPerArea = profile.MaxPlantInstancesPerArea,
                allowedPlantSpeciesKeys = profile.AllowedPlantSpeciesKeys,
                preferredSeedBankSpeciesKeys = profile.PreferredSeedBankSpeciesKeys
            };
        }
    }

    // =============================================================================
    // EnvironmentBiomeCatalogConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Wrapper serializzabile dei profili biome caricabili da file di configurazione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: parametri ecologici fuori dal codice</b></para>
    /// <para>
    /// Il file JSON puo' dichiarare target, resistenze, limiti di PlantInstance e
    /// specie vegetali ammesse per ogni bioma. Il Core riceve poi un catalogo
    /// read-only, senza conoscere Resources, TextAsset o altri dettagli Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>profiles</b>: profili biome disponibili.</item>
    ///   <item><b>ToCatalog</b>: converte i DTO in un catalogo read-only.</item>
    ///   <item><b>CreateDefault</b>: fallback coerente con i preset protetti.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentBiomeCatalogConfig
    {
        public EnvironmentBiomeProfileConfig[] profiles =
            EnvironmentBiomeProfileConfig.CreateDefaultSet();

        public EnvironmentBiomeCatalog ToCatalog()
        {
            var safeProfiles = profiles ?? new EnvironmentBiomeProfileConfig[0];
            var resolved = new List<EnvironmentBiomeProfile>(safeProfiles.Length);
            for (int i = 0; i < safeProfiles.Length; i++)
            {
                if (safeProfiles[i] == null)
                    continue;

                resolved.Add(safeProfiles[i].ToProfile());
            }

            return resolved.Count == 0
                ? EnvironmentBiomeCatalog.CreateDefault()
                : new EnvironmentBiomeCatalog(resolved);
        }

        public static EnvironmentBiomeCatalogConfig CreateDefault()
        {
            return new EnvironmentBiomeCatalogConfig
            {
                profiles = EnvironmentBiomeProfileConfig.CreateDefaultSet()
            };
        }
    }

    // =============================================================================
    // EnvironmentBiomeCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo read-only dei profili biome disponibili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: lookup esplicito al bordo del sistema</b></para>
    /// <para>
    /// Il catalogo evita switch sparsi sui nomi dei biomi. I consumer risolvono una
    /// chiave in un profilo value-only e poi passano quel profilo ai resolver.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryGetProfile</b>: lookup case-insensitive.</item>
    ///   <item><b>CreateDefault</b>: set minimo grassland/desert/jungle/tundra.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentBiomeCatalog
    {
        private readonly Dictionary<string, EnvironmentBiomeProfile> _profiles =
            new Dictionary<string, EnvironmentBiomeProfile>(StringComparer.OrdinalIgnoreCase);

        public EnvironmentBiomeCatalog(IEnumerable<EnvironmentBiomeProfile> profiles)
        {
            if (profiles == null)
                return;

            foreach (var profile in profiles)
            {
                if (!profile.IsValid)
                    continue;

                _profiles[profile.BiomeKey] = profile;
            }
        }

        public bool TryGetProfile(string biomeKey, out EnvironmentBiomeProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(biomeKey)
                && _profiles.TryGetValue(biomeKey, out profile))
            {
                return true;
            }

            profile = EnvironmentBiomeProfile.Default;
            return false;
        }

        public static EnvironmentBiomeCatalog CreateDefault()
        {
            return new EnvironmentBiomeCatalog(new[]
            {
                EnvironmentBiomeProfile.CreateTemperateGrassland(),
                EnvironmentBiomeProfile.CreateDesert(),
                EnvironmentBiomeProfile.CreateJungle(),
                EnvironmentBiomeProfile.CreateTundra()
            });
        }
    }
}
