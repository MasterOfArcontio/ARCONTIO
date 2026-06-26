using System;
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
    // EnvironmentConsumerAreaCandidate
    // =============================================================================
    /// <summary>
    /// <para>
    /// Area biologica candidata per una richiesta NPC/Decision Layer.
    /// </para>
    ///
    /// <para><b>Principio architetturale: NPC cercano luoghi, non registry interni</b></para>
    /// <para>
    /// Il candidate non espone la seed bank mutabile e non trasforma le piante in
    /// oggetti. Dice soltanto che una certa area, rappresentata da centro/raggio e
    /// chiavi semantiche, e' un buon luogo in cui cercare una specie o una risorsa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Area</b>: id, key, centro e raggio della sfera biologica.</item>
    ///   <item><b>Match</b>: specie e resource output che hanno soddisfatto la query.</item>
    ///   <item><b>Pressione</b>: seed bank, piante vive e piante harvestable presenti.</item>
    ///   <item><b>Score</b>: valore normalizzato per ordinare candidate vicine e fertili.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentConsumerAreaCandidate
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly string AreaKey;
        public readonly EnvironmentCellCoord CenterCell;
        public readonly int RadiusCells;
        public readonly string MatchedSpeciesKey;
        public readonly string ResourceOutputKey;
        public readonly float SeedPressure01;
        public readonly int LivePlantCount;
        public readonly int HarvestablePlantCount;
        public readonly float Score01;

        public bool IsValid =>
            AreaId.IsValid
            && Score01 > 0f
            && (!string.IsNullOrWhiteSpace(MatchedSpeciesKey)
                || !string.IsNullOrWhiteSpace(ResourceOutputKey));

        // =============================================================================
        // EnvironmentConsumerAreaCandidate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una candidata area biologica normalizzando pressione e score.
        /// </para>
        /// </summary>
        public EnvironmentConsumerAreaCandidate(
            EnvironmentAreaId areaId,
            string areaKey,
            EnvironmentCellCoord centerCell,
            int radiusCells,
            string matchedSpeciesKey,
            string resourceOutputKey,
            float seedPressure01,
            int livePlantCount,
            int harvestablePlantCount,
            float score01)
        {
            AreaId = areaId;
            AreaKey = areaKey ?? string.Empty;
            CenterCell = centerCell;
            RadiusCells = radiusCells < 0 ? 0 : radiusCells;
            MatchedSpeciesKey = matchedSpeciesKey ?? string.Empty;
            ResourceOutputKey = resourceOutputKey ?? string.Empty;
            SeedPressure01 = EnvironmentMath.Clamp01(seedPressure01);
            LivePlantCount = livePlantCount < 0 ? 0 : livePlantCount;
            HarvestablePlantCount = harvestablePlantCount < 0 ? 0 : harvestablePlantCount;
            Score01 = EnvironmentMath.Clamp01(score01);
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
        private static readonly EnvironmentConsumerAreaCandidate[] EmptyAreaCandidates =
            new EnvironmentConsumerAreaCandidate[0];

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

        // =============================================================================
        // QueryBiologicalAreasForSpeciesOrResource
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce aree biologiche candidate in cui cercare una specie vegetale o
        /// una risorsa prodotta da una specie vegetale.
        /// </para>
        ///
        /// <para><b>Principio architetturale: conoscenza spaziale ad area</b></para>
        /// <para>
        /// Gli NPC non devono memorizzare ogni singola pianta decorativa o ogni cella
        /// della seed bank. Questa query produce luoghi biologici stabili: un job o
        /// una belief futura potranno puntare all'area/landmark e solo dopo cercare
        /// piante puntuali quando servira'.
        /// </para>
        /// </summary>
        public static IReadOnlyList<EnvironmentConsumerAreaCandidate> QueryBiologicalAreasForSpeciesOrResource(
            EnvironmentFullSnapshot snapshot,
            EnvironmentPlantCatalog catalog,
            EnvironmentCellCoord requesterCell,
            string speciesOrResourceKey,
            int maxResults)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(speciesOrResourceKey))
                return EmptyAreaCandidates;

            int safeMaxResults = maxResults <= 0 ? 8 : maxResults;
            string requestedKey = speciesOrResourceKey.Trim();
            var candidates = new List<EnvironmentConsumerAreaCandidate>();

            for (int i = 0; i < snapshot.Areas.Count; i++)
            {
                EnvironmentAreaSnapshot area = snapshot.Areas[i];
                if (!area.Definition.IsEnabled || !IsBiologicalSearchArea(area))
                    continue;

                string matchedSpeciesKey = string.Empty;
                string resourceOutputKey = string.Empty;
                float seedPressure01 = ResolveSeedPressureForRequest(
                    area,
                    catalog,
                    requestedKey,
                    out matchedSpeciesKey,
                    out resourceOutputKey);

                CountPlantsForAreaRequest(
                    snapshot,
                    catalog,
                    area.Definition.AreaId,
                    requestedKey,
                    ref matchedSpeciesKey,
                    ref resourceOutputKey,
                    out int livePlantCount,
                    out int harvestablePlantCount);

                if (seedPressure01 <= 0f && livePlantCount <= 0 && harvestablePlantCount <= 0)
                    continue;

                EnvironmentCellCoord center = new EnvironmentCellCoord(
                    area.Definition.CenterX,
                    area.Definition.CenterY,
                    area.Definition.Bounds.Z);
                float score = ResolveAreaCandidateScore(
                    requesterCell,
                    center,
                    seedPressure01,
                    livePlantCount,
                    harvestablePlantCount);

                candidates.Add(new EnvironmentConsumerAreaCandidate(
                    area.Definition.AreaId,
                    area.Definition.Key,
                    center,
                    area.Definition.RadiusCells,
                    matchedSpeciesKey,
                    resourceOutputKey,
                    seedPressure01,
                    livePlantCount,
                    harvestablePlantCount,
                    score));
            }

            if (candidates.Count == 0)
                return EmptyAreaCandidates;

            candidates.Sort(CompareAreaCandidates);
            if (candidates.Count <= safeMaxResults)
                return candidates.ToArray();

            var trimmed = new EnvironmentConsumerAreaCandidate[safeMaxResults];
            for (int i = 0; i < safeMaxResults; i++)
                trimmed[i] = candidates[i];

            return trimmed;
        }

        private static bool IsBiologicalSearchArea(EnvironmentAreaSnapshot area)
        {
            return area.HasSeedBank
                   || area.HasVegetation
                   || area.Definition.Kind == EnvironmentAreaKind.Vegetation;
        }

        private static float ResolveSeedPressureForRequest(
            EnvironmentAreaSnapshot area,
            EnvironmentPlantCatalog catalog,
            string requestedKey,
            out string matchedSpeciesKey,
            out string resourceOutputKey)
        {
            matchedSpeciesKey = string.Empty;
            resourceOutputKey = string.Empty;
            if (!area.HasSeedBank || area.SeedBankState == null)
                return 0f;

            float bestPressure = 0f;
            IReadOnlyList<EnvironmentSeedBankEntry> entries = area.SeedBankState.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                EnvironmentSeedBankEntry entry = entries[i];
                if (!DoesSpeciesMatchRequest(entry.SpeciesKey, catalog, requestedKey, out string outputKey))
                    continue;

                float pressure = EnvironmentMath.Clamp01(entry.Amount01 * entry.Viability01);
                if (pressure <= bestPressure)
                    continue;

                bestPressure = pressure;
                matchedSpeciesKey = entry.SpeciesKey;
                resourceOutputKey = outputKey;
            }

            return bestPressure;
        }

        private static void CountPlantsForAreaRequest(
            EnvironmentFullSnapshot snapshot,
            EnvironmentPlantCatalog catalog,
            EnvironmentAreaId areaId,
            string requestedKey,
            ref string matchedSpeciesKey,
            ref string resourceOutputKey,
            out int livePlantCount,
            out int harvestablePlantCount)
        {
            livePlantCount = 0;
            harvestablePlantCount = 0;
            for (int i = 0; i < snapshot.Plants.Count; i++)
            {
                EnvironmentPlantSnapshot plant = snapshot.Plants[i];
                if (!plant.SourceAreaId.Equals(areaId))
                    continue;

                if (!DoesSpeciesMatchRequest(plant.SpeciesKey, catalog, requestedKey, out string outputKey))
                    continue;

                livePlantCount++;
                if (string.IsNullOrWhiteSpace(matchedSpeciesKey))
                    matchedSpeciesKey = plant.SpeciesKey;

                if (string.IsNullOrWhiteSpace(resourceOutputKey))
                    resourceOutputKey = outputKey;

                if (EnvironmentAgricultureFoundationResolver.TryBuildHarvestOutput(
                    plant,
                    catalog,
                    out EnvironmentHarvestOutput harvest)
                    && harvest.IsAvailable)
                {
                    harvestablePlantCount++;
                    if (string.IsNullOrWhiteSpace(resourceOutputKey))
                        resourceOutputKey = harvest.ResourceOutputKey;
                }
            }
        }

        private static bool DoesSpeciesMatchRequest(
            string speciesKey,
            EnvironmentPlantCatalog catalog,
            string requestedKey,
            out string resourceOutputKey)
        {
            resourceOutputKey = string.Empty;
            if (string.IsNullOrWhiteSpace(speciesKey) || string.IsNullOrWhiteSpace(requestedKey))
                return false;

            if (string.Equals(speciesKey, requestedKey, StringComparison.OrdinalIgnoreCase))
            {
                resourceOutputKey = ResolveResourceOutputKey(catalog, speciesKey);
                return true;
            }

            string outputKey = ResolveResourceOutputKey(catalog, speciesKey);
            if (!string.IsNullOrWhiteSpace(outputKey)
                && string.Equals(outputKey, requestedKey, StringComparison.OrdinalIgnoreCase))
            {
                resourceOutputKey = outputKey;
                return true;
            }

            return false;
        }

        private static string ResolveResourceOutputKey(
            EnvironmentPlantCatalog catalog,
            string speciesKey)
        {
            if (catalog == null || !catalog.TryGetSpecies(speciesKey, out EnvironmentPlantSpeciesDefinition species))
                return string.Empty;

            return species.ResourceOutputKey ?? string.Empty;
        }

        private static float ResolveAreaCandidateScore(
            EnvironmentCellCoord requesterCell,
            EnvironmentCellCoord center,
            float seedPressure01,
            int livePlantCount,
            int harvestablePlantCount)
        {
            int distance = requesterCell.Z == center.Z
                ? System.Math.Abs(requesterCell.X - center.X) + System.Math.Abs(requesterCell.Y - center.Y)
                : 9999;
            float distanceScore = 1f / (1f + (distance / 32f));
            float plantScore = EnvironmentMath.Clamp01(livePlantCount / 24f);
            float harvestScore = EnvironmentMath.Clamp01(harvestablePlantCount / 12f);
            return EnvironmentMath.Clamp01(
                (seedPressure01 * 0.40f)
                + (plantScore * 0.25f)
                + (harvestScore * 0.20f)
                + (distanceScore * 0.15f));
        }

        private static int CompareAreaCandidates(
            EnvironmentConsumerAreaCandidate a,
            EnvironmentConsumerAreaCandidate b)
        {
            int byScore = b.Score01.CompareTo(a.Score01);
            if (byScore != 0)
                return byScore;

            return a.AreaId.Value.CompareTo(b.AreaId.Value);
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
