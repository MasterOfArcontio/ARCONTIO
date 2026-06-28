using System;
using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentPlantCategory
    // =============================================================================
    /// <summary>
    /// <para>
    /// Categoria ecologica e produttiva di una specie vegetale importante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Plant Catalog prima delle istanze</b></para>
    /// <para>
    /// Il catalogo descrive cosa una specie puo' essere; non crea piante vive, non
    /// modifica aree e non produce risorse. Le istanze concrete arriveranno in uno
    /// step successivo come stato ambientale separato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Grass</b>: specie erbacee diffuse o importanti.</item>
    ///   <item><b>Shrub</b>: arbusti e cespugli.</item>
    ///   <item><b>Tree</b>: alberi e piante legnose grandi.</item>
    ///   <item><b>Crop</b>: colture intenzionali future.</item>
    ///   <item><b>Medicinal</b>: piante utili per medicina o preparati futuri.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentPlantCategory
    {
        Grass = 0,
        Shrub = 10,
        Tree = 20,
        Crop = 30,
        Medicinal = 40
    }

    // =============================================================================
    // EnvironmentPlantSeasonalBehavior
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comportamento stagionale generale di una specie vegetale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ciclo vegetale come dato</b></para>
    /// <para>
    /// Annuale, decidua o sempreverde non sono ancora logica di crescita. Sono
    /// metadati che il futuro ciclo naturale potra' leggere per decidere dormienza,
    /// produzione e morte stagionale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Annual</b>: completa il ciclo in una stagione/anno.</item>
    ///   <item><b>Perennial</b>: sopravvive piu' anni senza perdere tutta la struttura.</item>
    ///   <item><b>Deciduous</b>: entra in dormienza e perde produzione in inverno.</item>
    ///   <item><b>Evergreen</b>: mantiene presenza vegetativa tutto l'anno.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentPlantSeasonalBehavior
    {
        Annual = 0,
        Perennial = 10,
        Deciduous = 20,
        Evergreen = 30
    }

    // =============================================================================
    // EnvironmentPlantGrowthStageDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione passiva di uno stadio di crescita vegetale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: crescita configurata, non tickata qui</b></para>
    /// <para>
    /// Lo stadio dichiara soglie e caratteristiche. Non avanza eta', non calcola
    /// salute e non genera raccolti. Il futuro lifecycle delle PlantInstance potra'
    /// consumare questi dati con cadenza giornaliera.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>StageKey</b>: chiave stabile dello stadio.</item>
    ///   <item><b>RequiredAgeDays</b>: eta' minima richiesta in giorni ambientali.</item>
    ///   <item><b>Maturity01</b>: maturita' normalizzata associata allo stadio.</item>
    ///   <item><b>IsHarvestable</b>: indica se lo stadio puo' produrre raccolto futuro.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPlantGrowthStageDefinition
    {
        public readonly string StageKey;
        public readonly int RequiredAgeDays;
        public readonly float Maturity01;
        public readonly bool IsHarvestable;

        // =============================================================================
        // EnvironmentPlantGrowthStageDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno stadio normalizzando eta' e maturita'.
        /// </para>
        /// </summary>
        public EnvironmentPlantGrowthStageDefinition(
            string stageKey,
            int requiredAgeDays,
            float maturity01,
            bool isHarvestable)
        {
            StageKey = string.IsNullOrWhiteSpace(stageKey)
                ? "stage"
                : stageKey;
            RequiredAgeDays = requiredAgeDays < 0 ? 0 : requiredAgeDays;
            Maturity01 = EnvironmentMath.Clamp01(maturity01);
            IsHarvestable = isHarvestable;
        }
    }

    // =============================================================================
    // EnvironmentPlantProductDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Prodotto biologico potenziale dichiarato da una specie vegetale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: la pianta produce chiavi, non oggetti concreti</b></para>
    /// <para>
    /// La biosfera deve sapere che una specie puo' generare <c>apple</c>,
    /// <c>acorn</c> o <c>wood_log</c>, ma non deve creare inventario, food stock o item.
    /// Questo record e' quindi un contratto semantico: collega specie e prodotto
    /// futuro senza trasformare la pianta in un catalogo oggetti duplicato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ProductKey</b>: chiave stabile del prodotto ottenibile.</item>
    ///   <item><b>IsFood</b>: indica se il prodotto e' alimentare per decisioni future.</item>
    ///   <item><b>DestroysPlantOnHarvest</b>: true se ottenere il prodotto consuma la pianta.</item>
    ///   <item><b>RequiresToolKey</b>: strumento futuro richiesto, vuoto se non necessario.</item>
    ///   <item><b>MinGrowthStageKey</b>: stadio minimo richiesto per rendere il prodotto disponibile.</item>
    ///   <item><b>AvailableSeasonMask</b>: stagioni in cui il prodotto puo' essere raccolto.</item>
    ///   <item><b>BaseMaxAmountUnits</b>: quantita' base producibile da una pianta sana e matura.</item>
    ///   <item><b>RegrowDays</b>: giorni indicativi di ricrescita dopo il prelievo.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPlantProductDefinition
    {
        public readonly string ProductKey;
        public readonly bool IsFood;
        public readonly bool DestroysPlantOnHarvest;
        public readonly string RequiresToolKey;
        public readonly string MinGrowthStageKey;
        public readonly int AvailableSeasonMask;
        public readonly int BaseMaxAmountUnits;
        public readonly int RegrowDays;

        public bool IsValid => !string.IsNullOrWhiteSpace(ProductKey);

        // =============================================================================
        // EnvironmentPlantProductDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione prodotto normalizzando chiavi null a stringhe vuote.
        /// </para>
        /// </summary>
        public EnvironmentPlantProductDefinition(
            string productKey,
            bool isFood,
            bool destroysPlantOnHarvest,
            string requiresToolKey,
            string minGrowthStageKey,
            int availableSeasonMask,
            int baseMaxAmountUnits,
            int regrowDays)
        {
            ProductKey = string.IsNullOrWhiteSpace(productKey)
                ? string.Empty
                : productKey;
            IsFood = isFood;
            DestroysPlantOnHarvest = destroysPlantOnHarvest;
            RequiresToolKey = requiresToolKey ?? string.Empty;
            MinGrowthStageKey = minGrowthStageKey ?? string.Empty;
            AvailableSeasonMask = availableSeasonMask == 0
                ? EnvironmentPlantCatalogParsing.BuildAllSeasonMask()
                : availableSeasonMask;
            BaseMaxAmountUnits = baseMaxAmountUnits < 0 ? 0 : baseMaxAmountUnits;
            RegrowDays = regrowDays < 0 ? 0 : regrowDays;
        }

        // =============================================================================
        // IsSeasonAvailable
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se il prodotto e' biologicamente disponibile nella stagione
        /// richiesta.
        /// </para>
        /// </summary>
        public bool IsSeasonAvailable(EnvironmentSeasonKind season)
        {
            int bit = 1 << (int)season;
            return (AvailableSeasonMask & bit) != 0;
        }
    }

    // =============================================================================
    // EnvironmentPlantSpeciesDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione passiva di una specie vegetale importante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: specie come contratto dati</b></para>
    /// <para>
    /// La specie descrive requisiti ecologici, stagioni favorevoli, stadi e output
    /// potenziale. Non e' una risorsa concreta, non e' un oggetto nel World e non
    /// e' uno sprite. Questo evita di confondere catalogo biologico, istanze vive e
    /// rendering.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SpeciesKey</b>: chiave stabile della specie.</item>
    ///   <item><b>Category</b>: categoria vegetale.</item>
    ///   <item><b>GrowthStages</b>: stadi ordinati per eta' richiesta.</item>
    ///   <item><b>FavorableSeasons</b>: maschera stagioni favorevoli.</item>
    ///   <item><b>IdealTemperature01/IdealHumidity01</b>: preferenze climatiche normalizzate.</item>
    ///   <item><b>MinimumFertility01</b>: fertilita' minima consigliata.</item>
    ///   <item><b>ResourceOutputKey</b>: chiave output futura, non risorsa concreta.</item>
    ///   <item><b>SeasonalBehavior</b>: comportamento stagionale generale.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentPlantSpeciesDefinition
    {
        private static readonly EnvironmentPlantGrowthStageDefinition[] EmptyStages =
            new EnvironmentPlantGrowthStageDefinition[0];
        private static readonly EnvironmentPlantProductDefinition[] EmptyProducts =
            new EnvironmentPlantProductDefinition[0];

        public string SpeciesKey { get; }
        public EnvironmentPlantCategory Category { get; }
        public IReadOnlyList<EnvironmentPlantGrowthStageDefinition> GrowthStages { get; }
        public IReadOnlyList<EnvironmentPlantProductDefinition> Products { get; }
        public int FavorableSeasonMask { get; }
        public float IdealTemperature01 { get; }
        public float IdealHumidity01 { get; }
        public float MinimumFertility01 { get; }
        public string ResourceOutputKey { get; }
        public EnvironmentPlantSeasonalBehavior SeasonalBehavior { get; }

        // =============================================================================
        // EnvironmentPlantSpeciesDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione specie copiando gli stadi in una lista stabile.
        /// </para>
        /// </summary>
        public EnvironmentPlantSpeciesDefinition(
            string speciesKey,
            EnvironmentPlantCategory category,
            IReadOnlyList<EnvironmentPlantGrowthStageDefinition> growthStages,
            int favorableSeasonMask,
            float idealTemperature01,
            float idealHumidity01,
            float minimumFertility01,
            string resourceOutputKey,
            IReadOnlyList<EnvironmentPlantProductDefinition> products,
            EnvironmentPlantSeasonalBehavior seasonalBehavior)
        {
            SpeciesKey = string.IsNullOrWhiteSpace(speciesKey)
                ? string.Empty
                : speciesKey;
            Category = category;
            GrowthStages = CopyStages(growthStages);
            Products = CopyProducts(products, resourceOutputKey);
            FavorableSeasonMask = favorableSeasonMask;
            IdealTemperature01 = EnvironmentMath.Clamp01(idealTemperature01);
            IdealHumidity01 = EnvironmentMath.Clamp01(idealHumidity01);
            MinimumFertility01 = EnvironmentMath.Clamp01(minimumFertility01);
            ResourceOutputKey = ResolvePrimaryResourceOutputKey(resourceOutputKey, Products);
            SeasonalBehavior = seasonalBehavior;
        }

        // =============================================================================
        // IsSeasonFavorable
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se una stagione e' inclusa nella maschera favorevole della specie.
        /// </para>
        /// </summary>
        public bool IsSeasonFavorable(EnvironmentSeasonKind season)
        {
            int bit = 1 << (int)season;
            return (FavorableSeasonMask & bit) != 0;
        }

        // =============================================================================
        // TryGetStageForAge
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve lo stadio piu' avanzato compatibile con una eta' in giorni.
        /// </para>
        /// </summary>
        public bool TryGetStageForAge(
            int ageDays,
            out EnvironmentPlantGrowthStageDefinition stage)
        {
            stage = default;

            if (GrowthStages.Count == 0)
                return false;

            int safeAge = ageDays < 0 ? 0 : ageDays;
            bool found = false;
            for (int i = 0; i < GrowthStages.Count; i++)
            {
                // Gli stadi sono dati dichiarativi: scegliamo quello con soglia piu'
                // alta ma ancora raggiunta, senza fare avanzamento di stato.
                if (GrowthStages[i].RequiredAgeDays > safeAge)
                    continue;

                stage = GrowthStages[i];
                found = true;
            }

            if (!found)
                stage = GrowthStages[0];

            return true;
        }

        // =============================================================================
        // TryGetProduct
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un prodotto biologico dichiarato dalla specie.
        /// </para>
        /// </summary>
        public bool TryGetProduct(
            string productKey,
            out EnvironmentPlantProductDefinition product)
        {
            product = default;

            if (string.IsNullOrWhiteSpace(productKey))
                return false;

            for (int i = 0; i < Products.Count; i++)
            {
                EnvironmentPlantProductDefinition candidate = Products[i];
                if (!string.Equals(candidate.ProductKey, productKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                product = candidate;
                return true;
            }

            return false;
        }

        // =============================================================================
        // IsStageAtLeast
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se lo stadio corrente della pianta soddisfa uno stadio minimo
        /// dichiarato dal prodotto biologico.
        /// </para>
        /// </summary>
        public bool IsStageAtLeast(
            string currentStageKey,
            string minimumStageKey)
        {
            if (string.IsNullOrWhiteSpace(minimumStageKey))
                return true;

            int currentIndex = FindStageIndex(currentStageKey);
            int minimumIndex = FindStageIndex(minimumStageKey);
            if (currentIndex < 0 || minimumIndex < 0)
                return false;

            return currentIndex >= minimumIndex;
        }

        private static IReadOnlyList<EnvironmentPlantGrowthStageDefinition> CopyStages(
            IReadOnlyList<EnvironmentPlantGrowthStageDefinition> stages)
        {
            if (stages == null || stages.Count == 0)
                return EmptyStages;

            var copy = new EnvironmentPlantGrowthStageDefinition[stages.Count];
            for (int i = 0; i < stages.Count; i++)
            {
                copy[i] = stages[i];
            }

            Array.Sort(copy, CompareStages);
            return copy;
        }

        private static IReadOnlyList<EnvironmentPlantProductDefinition> CopyProducts(
            IReadOnlyList<EnvironmentPlantProductDefinition> products,
            string legacyResourceOutputKey)
        {
            var accepted = new List<EnvironmentPlantProductDefinition>();
            if (products != null)
            {
                for (int i = 0; i < products.Count; i++)
                {
                    EnvironmentPlantProductDefinition product = products[i];
                    if (!product.IsValid || ContainsProduct(accepted, product.ProductKey))
                        continue;

                    accepted.Add(product);
                }
            }

            if (accepted.Count == 0 && !string.IsNullOrWhiteSpace(legacyResourceOutputKey))
            {
                accepted.Add(new EnvironmentPlantProductDefinition(
                    legacyResourceOutputKey,
                    false,
                    false,
                    string.Empty,
                    string.Empty,
                    EnvironmentPlantCatalogParsing.BuildAllSeasonMask(),
                    0,
                    0));
            }

            return accepted.Count == 0 ? EmptyProducts : accepted.ToArray();
        }

        private static bool ContainsProduct(
            IReadOnlyList<EnvironmentPlantProductDefinition> products,
            string productKey)
        {
            for (int i = 0; i < products.Count; i++)
            {
                if (string.Equals(products[i].ProductKey, productKey, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string ResolvePrimaryResourceOutputKey(
            string resourceOutputKey,
            IReadOnlyList<EnvironmentPlantProductDefinition> products)
        {
            if (!string.IsNullOrWhiteSpace(resourceOutputKey))
                return resourceOutputKey;

            return products != null && products.Count > 0
                ? products[0].ProductKey
                : string.Empty;
        }

        private int FindStageIndex(string stageKey)
        {
            if (string.IsNullOrWhiteSpace(stageKey))
                return -1;

            for (int i = 0; i < GrowthStages.Count; i++)
            {
                if (string.Equals(GrowthStages[i].StageKey, stageKey, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static int CompareStages(
            EnvironmentPlantGrowthStageDefinition left,
            EnvironmentPlantGrowthStageDefinition right)
        {
            int age = left.RequiredAgeDays.CompareTo(right.RequiredAgeDays);
            if (age != 0)
                return age;

            return string.Compare(
                left.StageKey,
                right.StageKey,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    // =============================================================================
    // EnvironmentPlantCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo read-only delle specie vegetali configurate.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo senza lifecycle</b></para>
    /// <para>
    /// Il catalogo offre lookup e lista specie. Non carica file, non istanzia piante
    /// e non possiede stato runtime. Questo lo rende adatto a loader futuri, test e
    /// sistemi di crescita senza introdurre una authority parallela.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Species</b>: lista read-only delle definizioni.</item>
    ///   <item><b>TryGetSpecies</b>: lookup per chiave specie.</item>
    ///   <item><b>ContainsSpecies</b>: controllo presenza leggero.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentPlantCatalog
    {
        private static readonly EnvironmentPlantSpeciesDefinition[] EmptySpecies =
            new EnvironmentPlantSpeciesDefinition[0];

        private readonly Dictionary<string, EnvironmentPlantSpeciesDefinition> _byKey =
            new Dictionary<string, EnvironmentPlantSpeciesDefinition>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<EnvironmentPlantSpeciesDefinition> Species { get; }
        public int SpeciesCount => Species.Count;

        // =============================================================================
        // EnvironmentPlantCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il catalogo copiando solo specie con chiave non vuota.
        /// </para>
        /// </summary>
        public EnvironmentPlantCatalog(
            IReadOnlyList<EnvironmentPlantSpeciesDefinition> species)
        {
            if (species == null || species.Count == 0)
            {
                Species = EmptySpecies;
                return;
            }

            var accepted = new List<EnvironmentPlantSpeciesDefinition>(species.Count);
            for (int i = 0; i < species.Count; i++)
            {
                var definition = species[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.SpeciesKey))
                    continue;

                // La prima definizione vince: il validatore segnala duplicati, mentre
                // il catalogo resta deterministico anche con config sporche.
                if (_byKey.ContainsKey(definition.SpeciesKey))
                    continue;

                _byKey.Add(definition.SpeciesKey, definition);
                accepted.Add(definition);
            }

            Species = accepted.ToArray();
        }

        // =============================================================================
        // TryGetSpecies
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca una specie per chiave stabile.
        /// </para>
        /// </summary>
        public bool TryGetSpecies(
            string speciesKey,
            out EnvironmentPlantSpeciesDefinition species)
        {
            species = null;

            if (string.IsNullOrWhiteSpace(speciesKey))
                return false;

            return _byKey.TryGetValue(speciesKey, out species);
        }

        // =============================================================================
        // ContainsSpecies
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se il catalogo contiene una specie con la chiave richiesta.
        /// </para>
        /// </summary>
        public bool ContainsSpecies(string speciesKey)
        {
            return TryGetSpecies(speciesKey, out _);
        }
    }
}
