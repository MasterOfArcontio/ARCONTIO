using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentConsumerCellFacts
    // =============================================================================
    /// <summary>
    /// <para>
    /// Facts ambientali compatti leggibili da NPC, Decision Layer o debug futuri.
    /// </para>
    ///
    /// <para><b>Principio architetturale: consumer leggono facts, non registry</b></para>
    /// <para>
    /// Un NPC non deve conoscere l'intero snapshot, le liste layer o i dettagli di
    /// composizione della biosfera. Questo record riassume cosa conta in una cella:
    /// acqua, fertilita', vegetazione, clima e risorse vegetali osservabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: cella interrogata.</item>
    ///   <item><b>Climate*</b>: clima globale corrente.</item>
    ///   <item><b>Fertility*</b>: migliore fertilita' disponibile sulla cella.</item>
    ///   <item><b>Water*</b>: acqua piu' rilevante sulla cella.</item>
    ///   <item><b>Vegetation*</b>: vegetazione diffusa sulla cella.</item>
    ///   <item><b>Plant*</b>: piante importanti e raccolti potenziali.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentConsumerCellFacts
    {
        public readonly EnvironmentCellCoord Cell;
        public readonly EnvironmentSeasonKind Season;
        public readonly float Temperature01;
        public readonly float Humidity01;
        public readonly float Aridity01;
        public readonly bool HasFertility;
        public readonly float Fertility01;
        public readonly bool HasWater;
        public readonly float WaterLevel01;
        public readonly bool HasDrinkableWater;
        public readonly bool HasVegetation;
        public readonly float VegetationDensity01;
        public readonly float VegetationHealth01;
        public readonly float SeedBankPressure01;
        public readonly int PlantCount;
        public readonly int HarvestablePlantCount;
        public readonly string BestResourceOutputKey;

        public bool HasHarvestableResource =>
            HarvestablePlantCount > 0
            && !string.IsNullOrWhiteSpace(BestResourceOutputKey);

        // =============================================================================
        // EnvironmentConsumerCellFacts
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce i facts cella gia' aggregati dal resolver.
        /// </para>
        /// </summary>
        public EnvironmentConsumerCellFacts(
            EnvironmentCellCoord cell,
            EnvironmentSeasonKind season,
            float temperature01,
            float humidity01,
            float aridity01,
            bool hasFertility,
            float fertility01,
            bool hasWater,
            float waterLevel01,
            bool hasDrinkableWater,
            bool hasVegetation,
            float vegetationDensity01,
            float vegetationHealth01,
            float seedBankPressure01,
            int plantCount,
            int harvestablePlantCount,
            string bestResourceOutputKey)
        {
            Cell = cell;
            Season = season;
            Temperature01 = EnvironmentMath.Clamp01(temperature01);
            Humidity01 = EnvironmentMath.Clamp01(humidity01);
            Aridity01 = EnvironmentMath.Clamp01(aridity01);
            HasFertility = hasFertility;
            Fertility01 = EnvironmentMath.Clamp01(fertility01);
            HasWater = hasWater;
            WaterLevel01 = EnvironmentMath.Clamp01(waterLevel01);
            HasDrinkableWater = hasDrinkableWater;
            HasVegetation = hasVegetation;
            VegetationDensity01 = EnvironmentMath.Clamp01(vegetationDensity01);
            VegetationHealth01 = EnvironmentMath.Clamp01(vegetationHealth01);
            SeedBankPressure01 = EnvironmentMath.Clamp01(seedBankPressure01);
            PlantCount = plantCount < 0 ? 0 : plantCount;
            HarvestablePlantCount = harvestablePlantCount < 0 ? 0 : harvestablePlantCount;
            BestResourceOutputKey = bestResourceOutputKey ?? string.Empty;
        }
    }

    // =============================================================================
    // EnvironmentConsumerResourceCandidate
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risorsa vegetale potenziale osservabile da un consumer.
    /// </para>
    ///
    /// <para><b>Principio architetturale: disponibilita' leggibile, non raccolta eseguita</b></para>
    /// <para>
    /// Il candidate dice che una pianta puo' produrre una risorsa futura. Non crea
    /// item, non prenota job e non modifica la pianta. Il Decision Layer potra'
    /// leggerlo per decidere se chiedere un'azione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PlantId</b>: pianta sorgente.</item>
    ///   <item><b>SpeciesKey</b>: specie vegetale.</item>
    ///   <item><b>Cell</b>: posizione della pianta.</item>
    ///   <item><b>ResourceOutputKey</b>: risorsa potenziale.</item>
    ///   <item><b>Availability01</b>: quantita' normalizzata.</item>
    ///   <item><b>Quality01</b>: qualita' normalizzata.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentConsumerResourceCandidate
    {
        public readonly EnvironmentPlantId PlantId;
        public readonly string SpeciesKey;
        public readonly EnvironmentCellCoord Cell;
        public readonly string ResourceOutputKey;
        public readonly float Availability01;
        public readonly float Quality01;

        public bool IsAvailable =>
            PlantId.IsValid
            && !string.IsNullOrWhiteSpace(ResourceOutputKey)
            && Availability01 > 0f;

        // =============================================================================
        // EnvironmentConsumerResourceCandidate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un candidate risorsa normalizzando disponibilita' e qualita'.
        /// </para>
        /// </summary>
        public EnvironmentConsumerResourceCandidate(
            EnvironmentPlantId plantId,
            string speciesKey,
            EnvironmentCellCoord cell,
            string resourceOutputKey,
            float availability01,
            float quality01)
        {
            PlantId = plantId;
            SpeciesKey = speciesKey ?? string.Empty;
            Cell = cell;
            ResourceOutputKey = resourceOutputKey ?? string.Empty;
            Availability01 = EnvironmentMath.Clamp01(availability01);
            Quality01 = EnvironmentMath.Clamp01(quality01);
        }
    }

    // =============================================================================
    // EnvironmentConsumerQueryResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Facade read-only per interrogazioni consumer sulla biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: boundary stabile verso Decision/NPC</b></para>
    /// <para>
    /// Il resolver opera su <see cref="EnvironmentFullSnapshot"/> e non riceve
    /// <see cref="EnvironmentState"/>. In questo modo NPC e Decision Layer futuri
    /// consumeranno solo viste read-only, senza accedere a registry o payload mutabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildCellFacts</b>: riassume ambiente e risorse di una cella.</item>
    ///   <item><b>QueryHarvestableResources</b>: trova risorse vegetali entro raggio.</item>
    ///   <item><b>IsInsideRadius</b>: filtro spaziale discreto e deterministico.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentConsumerQueryResolver
    {
        private static readonly EnvironmentConsumerResourceCandidate[] EmptyCandidates =
            new EnvironmentConsumerResourceCandidate[0];

        // =============================================================================
        // BuildCellFacts
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce facts ambientali aggregati per una cella.
        /// </para>
        /// </summary>
        public static EnvironmentConsumerCellFacts BuildCellFacts(
            EnvironmentFullSnapshot snapshot,
            EnvironmentCellCoord cell,
            EnvironmentPlantCatalog catalog)
        {
            if (snapshot == null)
            {
                return new EnvironmentConsumerCellFacts(
                    cell,
                    EnvironmentSeasonKind.Spring,
                    0f,
                    0f,
                    0f,
                    false,
                    0f,
                    false,
                    0f,
                    false,
                    false,
                    0f,
                    0f,
                    0f,
                    0,
                    0,
                    string.Empty);
            }

            bool hasFertility = false;
            float bestFertility = 0f;
            bool hasWater = false;
            float bestWater = 0f;
            bool hasDrinkableWater = false;
            bool hasVegetation = false;
            float bestVegetationDensity = 0f;
            float bestVegetationHealth = 0f;
            float seedBankPressure = 0f;

            for (int i = 0; i < snapshot.Areas.Count; i++)
            {
                var area = snapshot.Areas[i];
                if (!area.Definition.IsEnabled || !area.Definition.Bounds.Contains(cell))
                    continue;

                if (area.HasFertility && area.FertilityState.CurrentFertility01 >= bestFertility)
                {
                    hasFertility = true;
                    bestFertility = area.FertilityState.CurrentFertility01;
                }

                if (area.HasWater && area.WaterState.WaterLevel01 >= bestWater)
                {
                    hasWater = true;
                    bestWater = area.WaterState.WaterLevel01;
                    hasDrinkableWater = area.WaterState.IsDrinkable;
                }

                if (area.HasVegetation && area.VegetationState.Density01 >= bestVegetationDensity)
                {
                    hasVegetation = true;
                    bestVegetationDensity = area.VegetationState.Density01;
                    bestVegetationHealth = area.VegetationState.Health01;
                }

                if (area.HasSeedBank && area.SeedBankState.TotalAmount01 > seedBankPressure)
                    seedBankPressure = area.SeedBankState.TotalAmount01;
            }

            int plantCount = 0;
            int harvestableCount = 0;
            string bestResource = string.Empty;
            float bestAvailability = -1f;

            for (int i = 0; i < snapshot.Plants.Count; i++)
            {
                var plant = snapshot.Plants[i];
                if (!plant.Cell.Equals(cell))
                    continue;

                plantCount++;
                if (!EnvironmentAgricultureFoundationResolver.TryBuildHarvestOutput(
                    plant,
                    catalog,
                    out EnvironmentHarvestOutput output))
                {
                    continue;
                }

                harvestableCount++;
                if (output.Amount01 > bestAvailability)
                {
                    bestAvailability = output.Amount01;
                    bestResource = output.ResourceOutputKey;
                }
            }

            return new EnvironmentConsumerCellFacts(
                cell,
                snapshot.Calendar.Season,
                snapshot.Climate.Temperature01,
                snapshot.Climate.Humidity01,
                snapshot.Climate.Aridity01,
                hasFertility,
                bestFertility,
                hasWater,
                bestWater,
                hasDrinkableWater,
                hasVegetation,
                bestVegetationDensity,
                bestVegetationHealth,
                seedBankPressure,
                plantCount,
                harvestableCount,
                bestResource);
        }

        // =============================================================================
        // QueryHarvestableResources
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce risorse vegetali potenziali entro un raggio discreto.
        /// </para>
        /// </summary>
        public static IReadOnlyList<EnvironmentConsumerResourceCandidate> QueryHarvestableResources(
            EnvironmentFullSnapshot snapshot,
            EnvironmentPlantCatalog catalog,
            EnvironmentCellCoord center,
            int radius)
        {
            if (snapshot == null || catalog == null)
                return EmptyCandidates;

            int safeRadius = radius < 0 ? 0 : radius;
            var candidates = new List<EnvironmentConsumerResourceCandidate>();
            for (int i = 0; i < snapshot.Plants.Count; i++)
            {
                var plant = snapshot.Plants[i];
                if (!IsInsideRadius(center, plant.Cell, safeRadius))
                    continue;

                if (!EnvironmentAgricultureFoundationResolver.TryBuildHarvestOutput(
                    plant,
                    catalog,
                    out EnvironmentHarvestOutput output))
                {
                    continue;
                }

                candidates.Add(new EnvironmentConsumerResourceCandidate(
                    plant.PlantId,
                    plant.SpeciesKey,
                    plant.Cell,
                    output.ResourceOutputKey,
                    output.Amount01,
                    output.Quality01));
            }

            return candidates.Count == 0 ? EmptyCandidates : candidates.ToArray();
        }

        private static bool IsInsideRadius(
            EnvironmentCellCoord center,
            EnvironmentCellCoord candidate,
            int radius)
        {
            if (center.Z != candidate.Z)
                return false;

            int dx = System.Math.Abs(center.X - candidate.X);
            int dy = System.Math.Abs(center.Y - candidate.Y);
            return dx + dy <= radius;
        }
    }
}
