using System;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentPlantId
    // =============================================================================
    /// <summary>
    /// <para>
    /// Identificatore value-only di una pianta ambientale importante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: piante vive separate dagli oggetti</b></para>
    /// <para>
    /// Una PlantInstance non e' un <c>WorldObject</c>, non e' uno sprite e non e' una
    /// risorsa inventariale. L'id dedicato permette alla biosfera di conservare alberi,
    /// arbusti, colture o piante raccoglibili senza occupare prematuramente il modello
    /// oggetti del mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Value</b>: intero positivo per piante valide; zero rappresenta assenza.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPlantId : IEquatable<EnvironmentPlantId>
    {
        public readonly int Value;

        public bool IsValid => Value > 0;

        // =============================================================================
        // EnvironmentPlantId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un id pianta normalizzando valori negativi ad assenza.
        /// </para>
        /// </summary>
        public EnvironmentPlantId(int value)
        {
            // Gli id negativi non hanno semantica persistibile nella biosfera.
            Value = value < 0 ? 0 : value;
        }

        public bool Equals(EnvironmentPlantId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is EnvironmentPlantId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return IsValid ? Value.ToString() : "None";
        }

        public static EnvironmentPlantId None => new EnvironmentPlantId(0);
    }

    // =============================================================================
    // EnvironmentPlantGrowthStage
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stadio runtime normalizzato di una pianta importante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stadio leggibile senza vincolare il catalogo</b></para>
    /// <para>
    /// Il catalogo conserva stage key configurabili, mentre questa enum offre ai
    /// consumer un vocabolario compatto: germoglio, giovane, matura, secca o morta.
    /// La crescita reale resta fuori da questo tipo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Seedling</b>: pianta nata o appena visibile.</item>
    ///   <item><b>Young</b>: pianta in crescita ma non pienamente matura.</item>
    ///   <item><b>Mature</b>: pianta adulta o produttiva.</item>
    ///   <item><b>Dry</b>: pianta secca, dormiente o degradante.</item>
    ///   <item><b>Dead</b>: pianta morta, candidata a rimozione/decomposizione futura.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentPlantGrowthStage
    {
        Seedling = 0,
        Young = 10,
        Mature = 20,
        Dry = 30,
        Dead = 40
    }

    // =============================================================================
    // EnvironmentPlantHealthState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato salute discreto di una pianta importante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: salute come osservazione, non come sistema</b></para>
    /// <para>
    /// La salute discreta rende piu' leggibili snapshot, debug, decisioni NPC e
    /// adapter futuri. Non applica malattie, non consuma acqua e non modifica la
    /// fertilita' dell'area.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Healthy</b>: pianta in buone condizioni.</item>
    ///   <item><b>Stressed</b>: pianta sotto stress ambientale leggero.</item>
    ///   <item><b>Sick</b>: pianta compromessa ma ancora viva.</item>
    ///   <item><b>Dying</b>: pianta prossima alla morte.</item>
    ///   <item><b>Dead</b>: pianta non viva.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentPlantHealthState
    {
        Healthy = 0,
        Stressed = 10,
        Sick = 20,
        Dying = 30,
        Dead = 40
    }

    // =============================================================================
    // EnvironmentPlantResourceState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato reale, per-prodotto, delle risorse biologiche disponibili su una
    /// singola pianta fisica.
    /// </para>
    ///
    /// <para><b>Principio architetturale: prodotto biologico come stato della Biosfera</b></para>
    /// <para>
    /// Questo dato non e' un oggetto inventariale, non e' uno stock del World e non
    /// abilita direttamente un job. Rappresenta soltanto quante unita' di un certo
    /// prodotto una pianta concreta possiede ora. Il World e ArcGraph possono
    /// osservarlo tramite snapshot/proiezioni, ma la ownership resta biologica.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ProductKey</b>: chiave del prodotto biologico dichiarato dalla specie.</item>
    ///   <item><b>AvailableAmountUnits/MaxAmountUnits</b>: quantita' reale corrente e capacita' massima.</item>
    ///   <item><b>Availability01</b>: rapporto normalizzato usabile da query e UI.</item>
    ///   <item><b>Flags</b>: food, tool richiesto, distruzione pianta e ricrescita futura.</item>
    ///   <item><b>Stage/Season</b>: distinguono prodotto possibile da prodotto disponibile ora.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPlantResourceState
    {
        public readonly string ProductKey;
        public readonly int AvailableAmountUnits;
        public readonly int MaxAmountUnits;
        public readonly float Availability01;
        public readonly bool IsFood;
        public readonly bool DestroysPlantOnHarvest;
        public readonly string RequiresToolKey;
        public readonly string MinGrowthStageKey;
        public readonly int RegrowDays;
        public readonly bool IsStageAvailable;
        public readonly bool IsSeasonallyAvailable;

        public bool IsDefined => !string.IsNullOrWhiteSpace(ProductKey);

        public bool IsAvailable =>
            IsDefined
            && AvailableAmountUnits > 0
            && MaxAmountUnits > 0
            && IsStageAvailable
            && IsSeasonallyAvailable;

        // =============================================================================
        // EnvironmentPlantResourceState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno stato risorsa normalizzando quantita', percentuali e chiavi.
        /// </para>
        /// </summary>
        public EnvironmentPlantResourceState(
            string productKey,
            int availableAmountUnits,
            int maxAmountUnits,
            bool isFood,
            bool destroysPlantOnHarvest,
            string requiresToolKey,
            string minGrowthStageKey,
            int regrowDays,
            bool isStageAvailable,
            bool isSeasonallyAvailable)
        {
            ProductKey = string.IsNullOrWhiteSpace(productKey)
                ? string.Empty
                : productKey.Trim();
            MaxAmountUnits = maxAmountUnits < 0 ? 0 : maxAmountUnits;
            AvailableAmountUnits = availableAmountUnits < 0
                ? 0
                : System.Math.Min(availableAmountUnits, MaxAmountUnits);
            Availability01 = MaxAmountUnits <= 0
                ? 0f
                : EnvironmentMath.Clamp01(AvailableAmountUnits / (float)MaxAmountUnits);
            IsFood = isFood;
            DestroysPlantOnHarvest = destroysPlantOnHarvest;
            RequiresToolKey = requiresToolKey ?? string.Empty;
            MinGrowthStageKey = minGrowthStageKey ?? string.Empty;
            RegrowDays = regrowDays < 0 ? 0 : regrowDays;
            IsStageAvailable = isStageAvailable;
            IsSeasonallyAvailable = isSeasonallyAvailable;
        }
    }

    // =============================================================================
    // EnvironmentPlantInstance
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato data-only di una pianta ambientale importante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: istanza viva senza coupling runtime</b></para>
    /// <para>
    /// Questa struttura rappresenta una pianta concreta nel Core Environment, ma non
    /// crea GameObject, non occupa direttamente una cella MapGrid e non notifica NPC.
    /// I sistemi futuri potranno aggiornarla con cadenza giornaliera e i consumer
    /// potranno leggerla tramite snapshot.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PlantId</b>: identita' runtime/persistibile futura.</item>
    ///   <item><b>SpeciesKey</b>: riferimento al Plant Catalog.</item>
    ///   <item><b>Cell</b>: coordinata discreta x/y/z.</item>
    ///   <item><b>AgeDays</b>: eta' in giorni ambientali.</item>
    ///   <item><b>GrowthStage/GrowthStageKey</b>: stadio normalizzato e chiave catalogo.</item>
    ///   <item><b>Health*</b>: salute discreta e normalizzata.</item>
    ///   <item><b>Maturity01/IsHarvestable</b>: maturita' e potenziale raccolta futuri.</item>
    ///   <item><b>Resources</b>: prodotti reali disponibili su questa pianta concreta.</item>
    ///   <item><b>SourceAreaId</b>: area vegetazione/fertilita' sorgente opzionale.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPlantInstance
    {
        private static readonly EnvironmentPlantResourceState[] EmptyResources =
            new EnvironmentPlantResourceState[0];

        public readonly EnvironmentPlantId PlantId;
        public readonly string SpeciesKey;
        public readonly EnvironmentCellCoord Cell;
        public readonly int AgeDays;
        public readonly EnvironmentPlantGrowthStage GrowthStage;
        public readonly string GrowthStageKey;
        public readonly EnvironmentPlantHealthState HealthState;
        public readonly float Health01;
        public readonly float Maturity01;
        public readonly bool IsHarvestable;
        public readonly EnvironmentAreaId SourceAreaId;
        public readonly EnvironmentPlantResourceState[] Resources;

        public bool IsAlive =>
            PlantId.IsValid
            && HealthState != EnvironmentPlantHealthState.Dead
            && GrowthStage != EnvironmentPlantGrowthStage.Dead;

        // =============================================================================
        // EnvironmentPlantInstance
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una PlantInstance normalizzando eta', salute e maturita'.
        /// </para>
        /// </summary>
        public EnvironmentPlantInstance(
            EnvironmentPlantId plantId,
            string speciesKey,
            EnvironmentCellCoord cell,
            int ageDays,
            EnvironmentPlantGrowthStage growthStage,
            string growthStageKey,
            EnvironmentPlantHealthState healthState,
            float health01,
            float maturity01,
            bool isHarvestable,
            EnvironmentAreaId sourceAreaId,
            System.Collections.Generic.IReadOnlyList<EnvironmentPlantResourceState> resources = null)
        {
            PlantId = plantId;
            SpeciesKey = string.IsNullOrWhiteSpace(speciesKey)
                ? string.Empty
                : speciesKey;
            Cell = cell;
            AgeDays = ageDays < 0 ? 0 : ageDays;
            GrowthStage = growthStage;
            GrowthStageKey = string.IsNullOrWhiteSpace(growthStageKey)
                ? GrowthStage.ToString()
                : growthStageKey;
            Health01 = EnvironmentMath.Clamp01(health01);
            Maturity01 = EnvironmentMath.Clamp01(maturity01);
            IsHarvestable = isHarvestable;
            SourceAreaId = sourceAreaId;
            HealthState = Health01 <= 0f || growthStage == EnvironmentPlantGrowthStage.Dead
                ? EnvironmentPlantHealthState.Dead
                : healthState;
            Resources = CopyResources(resources);
        }

        // =============================================================================
        // CreateFromSpecies
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una PlantInstance usando la definizione specie e l'eta' corrente.
        /// </para>
        /// </summary>
        public static EnvironmentPlantInstance CreateFromSpecies(
            EnvironmentPlantId plantId,
            EnvironmentPlantSpeciesDefinition species,
            EnvironmentCellCoord cell,
            int ageDays,
            float health01,
            EnvironmentAreaId sourceAreaId,
            EnvironmentSeasonKind season = EnvironmentSeasonKind.Spring,
            bool enforceSeason = true)
        {
            string speciesKey = species == null ? string.Empty : species.SpeciesKey;
            string stageKey = "seedling";
            float maturity = 0f;
            bool harvestable = false;

            // La specie decide solo lo stadio leggibile per soglia di eta'. Nessuna
            // crescita viene simulata: il chiamante consegna eta' e salute correnti.
            if (species != null
                && species.TryGetStageForAge(ageDays, out EnvironmentPlantGrowthStageDefinition stage))
            {
                stageKey = stage.StageKey;
                maturity = stage.Maturity01;
                harvestable = stage.IsHarvestable;
            }

            var normalizedStage = ResolveGrowthStage(maturity, health01);
            var healthState = ResolveHealthState(health01);

            return new EnvironmentPlantInstance(
                plantId,
                speciesKey,
                cell,
                ageDays,
                normalizedStage,
                stageKey,
                healthState,
                health01,
                maturity,
                harvestable,
                sourceAreaId,
                EnvironmentPlantResourceStateResolver.BuildInitialResourceStates(
                    species,
                    stageKey,
                    season,
                    enforceSeason,
                    health01));
        }

        // =============================================================================
        // WithResourceStates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce una copia della pianta con lo stesso stato vitale e una nuova
        /// fotografia delle risorse biologiche reali.
        /// </para>
        /// </summary>
        public EnvironmentPlantInstance WithResourceStates(
            System.Collections.Generic.IReadOnlyList<EnvironmentPlantResourceState> resources)
        {
            // La copia mantiene identita', cella e salute; cambia solo lo stato
            // risorse, che resta un payload biologico value-only.
            return new EnvironmentPlantInstance(
                PlantId,
                SpeciesKey,
                Cell,
                AgeDays,
                GrowthStage,
                GrowthStageKey,
                HealthState,
                Health01,
                Maturity01,
                IsHarvestable,
                SourceAreaId,
                resources);
        }

        // =============================================================================
        // ToSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte l'istanza in snapshot read-only.
        /// </para>
        /// </summary>
        public EnvironmentPlantSnapshot ToSnapshot()
        {
            // Lo snapshot copia i value type; nessun riferimento mutabile allo stato
            // interno viene passato a consumer futuri.
            return new EnvironmentPlantSnapshot(
                PlantId,
                SpeciesKey,
                Cell,
                AgeDays,
                GrowthStage,
                GrowthStageKey,
                HealthState,
                Health01,
                Maturity01,
                IsHarvestable,
                SourceAreaId,
                Resources);
        }

        private static EnvironmentPlantResourceState[] CopyResources(
            System.Collections.Generic.IReadOnlyList<EnvironmentPlantResourceState> resources)
        {
            if (resources == null || resources.Count == 0)
                return EmptyResources;

            var copy = new EnvironmentPlantResourceState[resources.Count];
            for (int i = 0; i < resources.Count; i++)
                copy[i] = resources[i];

            return copy;
        }

        private static EnvironmentPlantGrowthStage ResolveGrowthStage(
            float maturity01,
            float health01)
        {
            if (health01 <= 0f)
                return EnvironmentPlantGrowthStage.Dead;

            float maturity = EnvironmentMath.Clamp01(maturity01);
            if (maturity >= 0.75f)
                return EnvironmentPlantGrowthStage.Mature;

            if (maturity >= 0.35f)
                return EnvironmentPlantGrowthStage.Young;

            return EnvironmentPlantGrowthStage.Seedling;
        }

        private static EnvironmentPlantHealthState ResolveHealthState(float health01)
        {
            float health = EnvironmentMath.Clamp01(health01);
            if (health <= 0f)
                return EnvironmentPlantHealthState.Dead;

            if (health < 0.2f)
                return EnvironmentPlantHealthState.Dying;

            if (health < 0.45f)
                return EnvironmentPlantHealthState.Sick;

            if (health < 0.7f)
                return EnvironmentPlantHealthState.Stressed;

            return EnvironmentPlantHealthState.Healthy;
        }
    }

    // =============================================================================
    // EnvironmentPlantSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only di una pianta ambientale importante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservazione pianta senza authority esterna</b></para>
    /// <para>
    /// ArcGraph, debug, save/load e NPC futuri devono poter leggere lo stato di una
    /// pianta senza ricevere la PlantInstance interna. Questo snapshot non contiene
    /// sprite, oggetti Unity o riferimenti a cataloghi mutabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PlantId</b>: identita' della pianta.</item>
    ///   <item><b>SpeciesKey</b>: chiave specie per lookup esterno.</item>
    ///   <item><b>Cell</b>: posizione discreta.</item>
    ///   <item><b>AgeDays</b>: eta' ambientale.</item>
    ///   <item><b>GrowthStage/GrowthStageKey</b>: stato di crescita leggibile.</item>
    ///   <item><b>Health*/Maturity01</b>: valori normalizzati osservabili.</item>
    ///   <item><b>IsHarvestable</b>: flag preparatorio per raccolta futura.</item>
    ///   <item><b>Resources</b>: stato prodotti reale copiato dalla PlantInstance.</item>
    ///   <item><b>SourceAreaId</b>: area ecologica sorgente opzionale.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPlantSnapshot
    {
        private static readonly EnvironmentPlantResourceState[] EmptyResources =
            new EnvironmentPlantResourceState[0];

        public readonly EnvironmentPlantId PlantId;
        public readonly string SpeciesKey;
        public readonly EnvironmentCellCoord Cell;
        public readonly int AgeDays;
        public readonly EnvironmentPlantGrowthStage GrowthStage;
        public readonly string GrowthStageKey;
        public readonly EnvironmentPlantHealthState HealthState;
        public readonly float Health01;
        public readonly float Maturity01;
        public readonly bool IsHarvestable;
        public readonly EnvironmentAreaId SourceAreaId;
        public readonly EnvironmentPlantResourceState[] Resources;

        public bool IsAlive =>
            PlantId.IsValid
            && HealthState != EnvironmentPlantHealthState.Dead
            && GrowthStage != EnvironmentPlantGrowthStage.Dead;

        // =============================================================================
        // EnvironmentPlantSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo snapshot copiando i campi osservabili della pianta.
        /// </para>
        /// </summary>
        public EnvironmentPlantSnapshot(
            EnvironmentPlantId plantId,
            string speciesKey,
            EnvironmentCellCoord cell,
            int ageDays,
            EnvironmentPlantGrowthStage growthStage,
            string growthStageKey,
            EnvironmentPlantHealthState healthState,
            float health01,
            float maturity01,
            bool isHarvestable,
            EnvironmentAreaId sourceAreaId,
            System.Collections.Generic.IReadOnlyList<EnvironmentPlantResourceState> resources = null)
        {
            PlantId = plantId;
            SpeciesKey = speciesKey ?? string.Empty;
            Cell = cell;
            AgeDays = ageDays < 0 ? 0 : ageDays;
            GrowthStage = growthStage;
            GrowthStageKey = string.IsNullOrWhiteSpace(growthStageKey)
                ? growthStage.ToString()
                : growthStageKey;
            Health01 = EnvironmentMath.Clamp01(health01);
            Maturity01 = EnvironmentMath.Clamp01(maturity01);
            IsHarvestable = isHarvestable;
            SourceAreaId = sourceAreaId;
            HealthState = Health01 <= 0f || growthStage == EnvironmentPlantGrowthStage.Dead
                ? EnvironmentPlantHealthState.Dead
                : healthState;
            Resources = CopyResources(resources);
        }

        private static EnvironmentPlantResourceState[] CopyResources(
            System.Collections.Generic.IReadOnlyList<EnvironmentPlantResourceState> resources)
        {
            if (resources == null || resources.Count == 0)
                return EmptyResources;

            var copy = new EnvironmentPlantResourceState[resources.Count];
            for (int i = 0; i < resources.Count; i++)
                copy[i] = resources[i];

            return copy;
        }
    }

    // =============================================================================
    // EnvironmentPlantResourceStateResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only dello stato risorse reale associato a una pianta.
    /// </para>
    ///
    /// <para><b>Principio architetturale: regole biologiche centralizzate</b></para>
    /// <para>
    /// Le query, la crescita e l'inspector non devono duplicare il calcolo di
    /// maturita', stagionalita' e quantita' disponibile. Questo resolver traduce
    /// catalogo specie + stato pianta in un contratto compatto e osservabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildInitialResourceStates</b>: materializza tutti i prodotti dichiarati dalla specie.</item>
    ///   <item><b>TryFindResource</b>: lookup stabile per prodotto dentro snapshot/istanza.</item>
    ///   <item><b>HasAvailableResource</b>: guardia usata dalle query harvestable.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentPlantResourceStateResolver
    {
        private static readonly EnvironmentPlantResourceState[] EmptyResources =
            new EnvironmentPlantResourceState[0];

        // =============================================================================
        // BuildInitialResourceStates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce lo stato iniziale delle risorse della pianta da catalogo,
        /// stadio e stagione corrente.
        /// </para>
        /// </summary>
        public static EnvironmentPlantResourceState[] BuildInitialResourceStates(
            EnvironmentPlantSpeciesDefinition species,
            string growthStageKey,
            EnvironmentSeasonKind season,
            bool enforceSeason,
            float health01)
        {
            if (species == null || species.Products == null || species.Products.Count == 0)
                return EmptyResources;

            var states = new EnvironmentPlantResourceState[species.Products.Count];
            for (int i = 0; i < species.Products.Count; i++)
            {
                EnvironmentPlantProductDefinition product = species.Products[i];
                bool stageAvailable = species.IsStageAtLeast(growthStageKey, product.MinGrowthStageKey);
                bool seasonAvailable = !enforceSeason || product.IsSeasonAvailable(season);
                int maxAmount = product.BaseMaxAmountUnits;
                int availableAmount = stageAvailable && seasonAvailable && health01 > 0f
                    ? maxAmount
                    : 0;

                states[i] = new EnvironmentPlantResourceState(
                    product.ProductKey,
                    availableAmount,
                    maxAmount,
                    product.IsFood,
                    product.DestroysPlantOnHarvest,
                    product.RequiresToolKey,
                    product.MinGrowthStageKey,
                    product.RegrowDays,
                    stageAvailable,
                    seasonAvailable);
            }

            return states;
        }

        // =============================================================================
        // BuildCurrentResourceStates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricostruisce lo stato prodotti corrente per una pianta snapshot.
        /// </para>
        /// </summary>
        public static EnvironmentPlantResourceState[] BuildCurrentResourceStates(
            EnvironmentPlantSnapshot plant,
            EnvironmentPlantCatalog catalog,
            EnvironmentSeasonKind season,
            bool enforceSeason)
        {
            if (plant.Resources != null && plant.Resources.Length > 0)
                return CopyResources(plant.Resources);

            if (catalog == null
                || string.IsNullOrWhiteSpace(plant.SpeciesKey)
                || !catalog.TryGetSpecies(plant.SpeciesKey, out EnvironmentPlantSpeciesDefinition species))
            {
                return EmptyResources;
            }

            return BuildInitialResourceStates(
                species,
                plant.GrowthStageKey,
                season,
                enforceSeason,
                plant.Health01);
        }

        public static bool TryFindResource(
            EnvironmentPlantSnapshot plant,
            string productKey,
            out EnvironmentPlantResourceState resource)
        {
            return TryFindResource(plant.Resources, productKey, out resource);
        }

        public static bool TryFindResource(
            System.Collections.Generic.IReadOnlyList<EnvironmentPlantResourceState> resources,
            string productKey,
            out EnvironmentPlantResourceState resource)
        {
            resource = default;
            if (resources == null || string.IsNullOrWhiteSpace(productKey))
                return false;

            for (int i = 0; i < resources.Count; i++)
            {
                EnvironmentPlantResourceState current = resources[i];
                if (string.Equals(
                        current.ProductKey,
                        productKey,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    resource = current;
                    return current.IsDefined;
                }
            }

            return false;
        }

        public static bool HasAvailableResource(
            System.Collections.Generic.IReadOnlyList<EnvironmentPlantResourceState> resources)
        {
            if (resources == null)
                return false;

            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].IsAvailable)
                    return true;
            }

            return false;
        }

        private static EnvironmentPlantResourceState[] CopyResources(
            System.Collections.Generic.IReadOnlyList<EnvironmentPlantResourceState> resources)
        {
            if (resources == null || resources.Count == 0)
                return EmptyResources;

            var copy = new EnvironmentPlantResourceState[resources.Count];
            for (int i = 0; i < resources.Count; i++)
                copy[i] = resources[i];

            return copy;
        }
    }
}
