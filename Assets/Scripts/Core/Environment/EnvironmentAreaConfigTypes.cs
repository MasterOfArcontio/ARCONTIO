using System;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentAreaSetConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del set di aree ambientali configurate.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dati ambientali dichiarativi</b></para>
    /// <para>
    /// La biosfera deve poter nascere da file di configurazione senza obbligare il
    /// loader futuro a conoscere dizionari, snapshot o dettagli di stato. Questo DTO
    /// conserva il materiale grezzo che un builder Core puo' trasformare in
    /// <see cref="EnvironmentState"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>areas</b>: definizioni base delle aree.</item>
    ///   <item><b>fertilityAreas</b>: payload di fertilita' collegati agli id area.</item>
    ///   <item><b>waterAreas</b>: payload acqua collegati agli id area.</item>
    ///   <item><b>vegetationAreas</b>: payload vegetazione collegati agli id area.</item>
    ///   <item><b>seedBankAreas</b>: payload seed bank collegati agli id area.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentAreaSetConfig
    {
        public EnvironmentAreaConfig[] areas = new EnvironmentAreaConfig[0];
        public EnvironmentFertilityAreaConfig[] fertilityAreas = new EnvironmentFertilityAreaConfig[0];
        public EnvironmentWaterAreaConfig[] waterAreas = new EnvironmentWaterAreaConfig[0];
        public EnvironmentVegetationAreaConfig[] vegetationAreas = new EnvironmentVegetationAreaConfig[0];
        public EnvironmentSeedBankAreaConfig[] seedBankAreas = new EnvironmentSeedBankAreaConfig[0];
    }

    // =============================================================================
    // EnvironmentSeedBankEntryConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile di una entry seed bank.
    /// </para>
    ///
    /// <para><b>Principio architetturale: specie naturali come chiavi dati</b></para>
    /// <para>
    /// La specie o categoria viene descritta da una chiave testuale stabile. Non e'
    /// un riferimento a prefab, asset, item o oggetto del mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>speciesKey</b>: chiave specie o categoria.</item>
    ///   <item><b>amount01</b>: disponibilita' normalizzata.</item>
    ///   <item><b>viability01</b>: vitalita' normalizzata.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentSeedBankEntryConfig
    {
        public string speciesKey = string.Empty;
        public float amount01 = 0.5f;
        public float viability01 = 0.5f;

        public EnvironmentSeedBankEntry ToEntry()
        {
            return new EnvironmentSeedBankEntry(
                speciesKey,
                amount01,
                viability01);
        }
    }

    // =============================================================================
    // EnvironmentSeedBankAreaConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del payload seed bank di un'area.
    /// </para>
    ///
    /// <para><b>Principio architetturale: seed bank separata dalla vegetazione visibile</b></para>
    /// <para>
    /// La vegetazione diffusa descrive cio' che e' presente ora; la seed bank
    /// descrive potenziale ecologico futuro. Tenerle separate evita di confondere
    /// densita' attuale, semi naturali e risorse agricole concrete.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>areaId</b>: area target.</item>
    ///   <item><b>entries</b>: specie/categorie con disponibilita' e vitalita'.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentSeedBankAreaConfig
    {
        public int areaId;
        public EnvironmentSeedBankEntryConfig[] entries = new EnvironmentSeedBankEntryConfig[0];

        public EnvironmentSeedBankAreaState ToState()
        {
            var safeEntries = entries ?? new EnvironmentSeedBankEntryConfig[0];
            var result = new EnvironmentSeedBankEntry[safeEntries.Length];
            for (int i = 0; i < safeEntries.Length; i++)
            {
                // Entry nulle vengono normalizzate come chiavi vuote a zero pressione:
                // il validatore le segnalera', ma il builder resta robusto.
                result[i] = safeEntries[i] != null
                    ? safeEntries[i].ToEntry()
                    : new EnvironmentSeedBankEntry(string.Empty, 0f, 0f);
            }

            return new EnvironmentSeedBankAreaState(
                new EnvironmentAreaId(areaId),
                result);
        }
    }

    // =============================================================================
    // EnvironmentAreaConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile della definizione base di un'area ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: coordinate e layer prima del renderer</b></para>
    /// <para>
    /// La configurazione parla in coordinate ambientali discrete e kind logici. Non
    /// contiene sprite, tile visuali o riferimenti a scene Unity, cosi' resta adatta
    /// tanto alla simulazione quanto a futuri adapter grafici.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>areaId</b>: identita' numerica stabile.</item>
    ///   <item><b>kind</b>: tipo area espresso come stringa configurabile.</item>
    ///   <item><b>minX/minY/maxX/maxY/z</b>: bounding box discreta.</item>
    ///   <item><b>priority</b>: precedenza futura tra aree dello stesso layer.</item>
    ///   <item><b>isEnabled</b>: gate dichiarativo.</item>
    ///   <item><b>key</b>: chiave leggibile per debug/config.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentAreaConfig
    {
        public int areaId;
        public string kind = "Generic";
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
        public int z;
        public int centerX;
        public int centerY;
        public int radiusCells;
        public int priority;
        public bool isEnabled = true;
        public string key = string.Empty;

        // =============================================================================
        // ToDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il DTO in definizione area value-only.
        /// </para>
        /// </summary>
        public EnvironmentAreaDefinition ToDefinition()
        {
            // Il bounds ordina gia' minimi e massimi, quindi il file config puo'
            // essere scritto in modo umano senza rompere il contratto interno.
            return new EnvironmentAreaDefinition(
                new EnvironmentAreaId(areaId),
                EnvironmentAreaConfigParsing.ParseAreaKind(kind),
                new EnvironmentAreaBounds(minX, minY, maxX, maxY, z),
                radiusCells > 0 ? centerX : (minX + maxX) / 2,
                radiusCells > 0 ? centerY : (minY + maxY) / 2,
                radiusCells,
                priority,
                isEnabled,
                key);
        }
    }

    // =============================================================================
    // EnvironmentFertilityAreaConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del payload fertilita' di un'area.
    /// </para>
    ///
    /// <para><b>Principio architetturale: fertilita' come layer configurabile</b></para>
    /// <para>
    /// I valori di suolo, fertilita', recupero ed esaurimento vengono dichiarati
    /// fuori dal codice caldo. Il builder li normalizza poi nella struttura runtime
    /// passiva <see cref="EnvironmentFertilityAreaState"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>areaId</b>: area target.</item>
    ///   <item><b>soilKind</b>: tipo suolo configurabile.</item>
    ///   <item><b>baseFertility01</b>: fertilita' naturale.</item>
    ///   <item><b>currentFertility01</b>: fertilita' iniziale o caricata.</item>
    ///   <item><b>growthModifier01</b>: supporto alla crescita.</item>
    ///   <item><b>exhaustion01</b>: esaurimento corrente.</item>
    ///   <item><b>recovery01</b>: recupero naturale.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentFertilityAreaConfig
    {
        public int areaId;
        public string soilKind = "Generic";
        public float baseFertility01 = 0.5f;
        public float currentFertility01 = 0.5f;
        public float growthModifier01 = 0.5f;
        public float exhaustion01;
        public float recovery01 = 0.5f;

        public EnvironmentFertilityAreaState ToState()
        {
            return new EnvironmentFertilityAreaState(
                new EnvironmentAreaId(areaId),
                EnvironmentAreaConfigParsing.ParseSoilKind(soilKind),
                baseFertility01,
                currentFertility01,
                growthModifier01,
                exhaustion01,
                recovery01);
        }
    }

    // =============================================================================
    // EnvironmentWaterAreaConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del payload acqua di un'area.
    /// </para>
    ///
    /// <para><b>Principio architetturale: acqua configurata senza fluidodinamica</b></para>
    /// <para>
    /// La configurazione descrive corpi o tratti d'acqua in modo compatto. Non
    /// esprime flusso continuo, vicini o pathfinding: quei sistemi potranno leggere
    /// lo stato normalizzato in step successivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>areaId</b>: area target.</item>
    ///   <item><b>waterKind</b>: tipo acqua configurabile.</item>
    ///   <item><b>depthLevel</b>: profondita' discreta configurabile.</item>
    ///   <item><b>waterLevel01</b>: livello normalizzato.</item>
    ///   <item><b>flowIntensity01</b>: flusso astratto.</item>
    ///   <item><b>isDrinkable</b>: potabilita'.</item>
    ///   <item><b>isSeasonal</b>: presenza stagionale.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentWaterAreaConfig
    {
        public int areaId;
        public string waterKind = "Still";
        public string depthLevel = "Shallow";
        public float waterLevel01 = 0.5f;
        public float flowIntensity01;
        public bool isDrinkable = true;
        public bool isSeasonal;

        public EnvironmentWaterAreaState ToState()
        {
            return new EnvironmentWaterAreaState(
                new EnvironmentAreaId(areaId),
                EnvironmentAreaConfigParsing.ParseWaterKind(waterKind),
                EnvironmentAreaConfigParsing.ParseWaterDepthLevel(depthLevel),
                waterLevel01,
                flowIntensity01,
                isDrinkable,
                isSeasonal);
        }
    }

    // =============================================================================
    // EnvironmentVegetationAreaConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del payload vegetazione diffusa di un'area.
    /// </para>
    ///
    /// <para><b>Principio architetturale: vegetazione diffusa prima delle istanze</b></para>
    /// <para>
    /// Densita', salute e potenziale di crescita restano dati di area. Le piante
    /// importanti, agricole o interagibili avranno contratti dedicati in checkpoint
    /// successivi e non vengono introdotte da questo DTO.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>areaId</b>: area target.</item>
    ///   <item><b>vegetationKind</b>: categoria vegetale configurabile.</item>
    ///   <item><b>density01</b>: densita' diffusa.</item>
    ///   <item><b>growthPotential01</b>: potenziale crescita.</item>
    ///   <item><b>health01</b>: salute media.</item>
    ///   <item><b>fertilityInfluence01</b>: influenza della fertilita'.</item>
    ///   <item><b>climateInfluence01</b>: influenza del clima globale.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentVegetationAreaConfig
    {
        public int areaId;
        public string vegetationKind = "Grass";
        public float density01 = 0.5f;
        public float growthPotential01 = 0.5f;
        public float health01 = 0.75f;
        public float fertilityInfluence01 = 0.5f;
        public float climateInfluence01 = 0.5f;

        public EnvironmentVegetationAreaState ToState()
        {
            return new EnvironmentVegetationAreaState(
                new EnvironmentAreaId(areaId),
                EnvironmentAreaConfigParsing.ParseVegetationKind(vegetationKind),
                density01,
                growthPotential01,
                health01,
                fertilityInfluence01,
                climateInfluence01);
        }
    }

    // =============================================================================
    // EnvironmentAreaConfigParsing
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper interno di parsing per DTO area ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stringhe config al bordo, enum nel Core</b></para>
    /// <para>
    /// I file futuri possono usare parole leggibili, mentre il Core continua a
    /// lavorare su enum canoniche. Il fallback e' conservativo e impedisce che una
    /// stringa sconosciuta generi eccezioni in fase di bootstrap.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ParseAreaKind</b>: converte il tipo area.</item>
    ///   <item><b>ParseSoilKind</b>: converte il tipo suolo.</item>
    ///   <item><b>ParseWaterKind</b>: converte il tipo acqua.</item>
    ///   <item><b>ParseWaterDepthLevel</b>: converte la profondita'.</item>
    ///   <item><b>ParseVegetationKind</b>: converte la categoria vegetale.</item>
    /// </list>
    /// </summary>
    internal static class EnvironmentAreaConfigParsing
    {
        public static EnvironmentAreaKind ParseAreaKind(string value)
        {
            if (string.Equals(value, "Fertility", StringComparison.OrdinalIgnoreCase))
                return EnvironmentAreaKind.Fertility;

            if (string.Equals(value, "Water", StringComparison.OrdinalIgnoreCase))
                return EnvironmentAreaKind.Water;

            if (string.Equals(value, "Vegetation", StringComparison.OrdinalIgnoreCase))
                return EnvironmentAreaKind.Vegetation;

            if (string.Equals(value, "Room", StringComparison.OrdinalIgnoreCase))
                return EnvironmentAreaKind.Room;

            if (string.Equals(value, "Territory", StringComparison.OrdinalIgnoreCase))
                return EnvironmentAreaKind.Territory;

            return EnvironmentAreaKind.Generic;
        }

        public static EnvironmentSoilKind ParseSoilKind(string value)
        {
            if (string.Equals(value, "Grassland", StringComparison.OrdinalIgnoreCase))
                return EnvironmentSoilKind.Grassland;

            if (string.Equals(value, "Forest", StringComparison.OrdinalIgnoreCase))
                return EnvironmentSoilKind.Forest;

            if (string.Equals(value, "Rocky", StringComparison.OrdinalIgnoreCase))
                return EnvironmentSoilKind.Rocky;

            if (string.Equals(value, "Riverbed", StringComparison.OrdinalIgnoreCase))
                return EnvironmentSoilKind.Riverbed;

            if (string.Equals(value, "Farmland", StringComparison.OrdinalIgnoreCase))
                return EnvironmentSoilKind.Farmland;

            return EnvironmentSoilKind.Generic;
        }

        public static EnvironmentWaterKind ParseWaterKind(string value)
        {
            if (string.Equals(value, "River", StringComparison.OrdinalIgnoreCase))
                return EnvironmentWaterKind.River;

            if (string.Equals(value, "Lake", StringComparison.OrdinalIgnoreCase))
                return EnvironmentWaterKind.Lake;

            if (string.Equals(value, "Puddle", StringComparison.OrdinalIgnoreCase))
                return EnvironmentWaterKind.Puddle;

            if (string.Equals(value, "Sea", StringComparison.OrdinalIgnoreCase))
                return EnvironmentWaterKind.Sea;

            return EnvironmentWaterKind.Still;
        }

        public static EnvironmentWaterDepthLevel ParseWaterDepthLevel(string value)
        {
            if (string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
                return EnvironmentWaterDepthLevel.None;

            if (string.Equals(value, "Ford", StringComparison.OrdinalIgnoreCase))
                return EnvironmentWaterDepthLevel.Ford;

            if (string.Equals(value, "Deep", StringComparison.OrdinalIgnoreCase))
                return EnvironmentWaterDepthLevel.Deep;

            if (string.Equals(value, "VeryDeep", StringComparison.OrdinalIgnoreCase))
                return EnvironmentWaterDepthLevel.VeryDeep;

            return EnvironmentWaterDepthLevel.Shallow;
        }

        public static EnvironmentVegetationKind ParseVegetationKind(string value)
        {
            if (string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
                return EnvironmentVegetationKind.None;

            if (string.Equals(value, "Underbrush", StringComparison.OrdinalIgnoreCase))
                return EnvironmentVegetationKind.Underbrush;

            if (string.Equals(value, "Shrubland", StringComparison.OrdinalIgnoreCase))
                return EnvironmentVegetationKind.Shrubland;

            if (string.Equals(value, "Forest", StringComparison.OrdinalIgnoreCase))
                return EnvironmentVegetationKind.Forest;

            if (string.Equals(value, "Cultivated", StringComparison.OrdinalIgnoreCase))
                return EnvironmentVegetationKind.Cultivated;

            return EnvironmentVegetationKind.Grass;
        }
    }
}
