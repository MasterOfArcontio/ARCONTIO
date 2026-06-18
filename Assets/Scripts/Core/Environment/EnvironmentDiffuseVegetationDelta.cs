using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentDiffuseVegetationDeltaKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo di variazione della vegetazione diffusa cell-based.
    /// </para>
    ///
    /// <para><b>Principio architetturale: vegetazione diffusa non-oggetto</b></para>
    /// <para>
    /// La vegetazione diffusa non e' una pianta fisica, non ha PlantId, non blocca
    /// movimento e non diventa target NPC puntuale. Questo enum descrive solo
    /// cambiamenti visuali/ecologici leggeri che un futuro World/ArcGraph adapter
    /// potra' consumare.
    /// </para>
    /// </summary>
    public enum EnvironmentDiffuseVegetationDeltaKind
    {
        None = 0,
        Appeared = 1,
        Disappeared = 2,
        KindChanged = 3,
        CoverageChanged = 4,
        ConditionChanged = 5
    }

    // =============================================================================
    // EnvironmentVegetationCoverageBand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Banda discreta di copertura della vegetazione diffusa.
    /// </para>
    /// </summary>
    public enum EnvironmentVegetationCoverageBand
    {
        None = 0,
        Sparse = 10,
        Medium = 20,
        Dense = 30
    }

    // =============================================================================
    // EnvironmentVegetationConditionBand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Banda discreta di condizione della vegetazione diffusa.
    /// </para>
    /// </summary>
    public enum EnvironmentVegetationConditionBand
    {
        Healthy = 0,
        Dry = 10,
        Dead = 20,
        Burned = 30
    }

    // =============================================================================
    // EnvironmentDiffuseVegetationDelta
    // =============================================================================
    /// <summary>
    /// <para>
    /// Delta data-only che descrive una variazione di vegetazione diffusa in una
    /// cella.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contratto visuale semantico, non renderer</b></para>
    /// <para>
    /// Il delta contiene area, cella, tipo vegetazione, copertura e condizione in
    /// bande discrete. Non contiene sprite, tile, atlas, path PNG, blocchi fisici o
    /// riferimenti a oggetti. Il World/ArcGraph futuro potra' decidere come
    /// conservarlo o visualizzarlo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: motivo del cambiamento.</item>
    ///   <item><b>AreaId</b>: area biologica sorgente.</item>
    ///   <item><b>Cell</b>: cella interessata.</item>
    ///   <item><b>VegetationKind</b>: categoria semantica diffusa.</item>
    ///   <item><b>CoverageBand</b>: copertura discreta.</item>
    ///   <item><b>ConditionBand</b>: condizione discreta.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentDiffuseVegetationDelta
    {
        public readonly EnvironmentDiffuseVegetationDeltaKind Kind;
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentCellCoord Cell;
        public readonly EnvironmentVegetationKind VegetationKind;
        public readonly EnvironmentVegetationCoverageBand CoverageBand;
        public readonly EnvironmentVegetationConditionBand ConditionBand;

        public bool IsValid =>
            Kind != EnvironmentDiffuseVegetationDeltaKind.None
            && AreaId.IsValid
            && VegetationKind != EnvironmentVegetationKind.None;

        public EnvironmentDiffuseVegetationDelta(
            EnvironmentDiffuseVegetationDeltaKind kind,
            EnvironmentAreaId areaId,
            EnvironmentCellCoord cell,
            EnvironmentVegetationKind vegetationKind,
            EnvironmentVegetationCoverageBand coverageBand,
            EnvironmentVegetationConditionBand conditionBand)
        {
            Kind = kind;
            AreaId = areaId;
            Cell = cell;
            VegetationKind = vegetationKind;
            CoverageBand = coverageBand;
            ConditionBand = conditionBand;
        }
    }

    // =============================================================================
    // EnvironmentDiffuseVegetationDeltaProducer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Produce delta di vegetazione diffusa confrontando placement cell-based della
    /// biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bande discrete al boundary</b></para>
    /// <para>
    /// La biosfera puo' conservare densita' e salute fini, ma il boundary non deve
    /// propagare micro-variazioni continue. Il producer converte i valori fini in
    /// bande e produce delta solo quando cambia la presenza, la categoria o una
    /// banda osservabile.
    /// </para>
    /// </summary>
    public static class EnvironmentDiffuseVegetationDeltaProducer
    {
        // =============================================================================
        // DiffPlacements
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta placement precedente e corrente e produce delta cell-based.
        /// </para>
        /// </summary>
        public static IReadOnlyList<EnvironmentDiffuseVegetationDelta> DiffPlacements(
            IReadOnlyList<EnvironmentVegetationCellPlacement> previous,
            IReadOnlyList<EnvironmentVegetationCellPlacement> current,
            int maxDeltaCount = int.MaxValue)
        {
            var deltas = new List<EnvironmentDiffuseVegetationDelta>();
            int budget = NormalizeBudget(maxDeltaCount);
            if (budget <= 0)
                return deltas;

            var previousByKey = BuildLookup(previous);
            var currentByKey = BuildLookup(current);
            var currentList = current ?? new EnvironmentVegetationCellPlacement[0];
            var previousList = previous ?? new EnvironmentVegetationCellPlacement[0];

            for (int i = 0; i < currentList.Count && deltas.Count < budget; i++)
            {
                var currentPlacement = currentList[i];
                var key = new VegetationPlacementKey(currentPlacement.AreaId, currentPlacement.Cell);

                if (!previousByKey.TryGetValue(key, out var previousPlacement))
                {
                    deltas.Add(FromPlacement(EnvironmentDiffuseVegetationDeltaKind.Appeared, currentPlacement));
                    continue;
                }

                var kind = ResolveDeltaKind(previousPlacement, currentPlacement);
                if (kind == EnvironmentDiffuseVegetationDeltaKind.None)
                    continue;

                deltas.Add(FromPlacement(kind, currentPlacement));
            }

            for (int i = 0; i < previousList.Count && deltas.Count < budget; i++)
            {
                var previousPlacement = previousList[i];
                var key = new VegetationPlacementKey(previousPlacement.AreaId, previousPlacement.Cell);
                if (currentByKey.ContainsKey(key))
                    continue;

                deltas.Add(FromPlacement(EnvironmentDiffuseVegetationDeltaKind.Disappeared, previousPlacement));
            }

            return deltas;
        }

        // =============================================================================
        // BuildFullRefresh
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce delta <c>Appeared</c> per tutti i placement correnti.
        /// </para>
        /// </summary>
        public static IReadOnlyList<EnvironmentDiffuseVegetationDelta> BuildFullRefresh(
            IReadOnlyList<EnvironmentVegetationCellPlacement> placements,
            int maxDeltaCount = int.MaxValue)
        {
            var deltas = new List<EnvironmentDiffuseVegetationDelta>();
            int budget = NormalizeBudget(maxDeltaCount);
            var safePlacements = placements ?? new EnvironmentVegetationCellPlacement[0];

            for (int i = 0; i < safePlacements.Count && deltas.Count < budget; i++)
                deltas.Add(FromPlacement(EnvironmentDiffuseVegetationDeltaKind.Appeared, safePlacements[i]));

            return deltas;
        }

        public static EnvironmentVegetationCoverageBand ResolveCoverageBand(float density01)
        {
            float density = EnvironmentMath.Clamp01(density01);
            if (density <= 0f)
                return EnvironmentVegetationCoverageBand.None;
            if (density < 0.34f)
                return EnvironmentVegetationCoverageBand.Sparse;
            if (density < 0.67f)
                return EnvironmentVegetationCoverageBand.Medium;
            return EnvironmentVegetationCoverageBand.Dense;
        }

        public static EnvironmentVegetationConditionBand ResolveConditionBand(float health01)
        {
            float health = EnvironmentMath.Clamp01(health01);
            if (health <= 0.05f)
                return EnvironmentVegetationConditionBand.Dead;
            if (health < 0.35f)
                return EnvironmentVegetationConditionBand.Dry;
            return EnvironmentVegetationConditionBand.Healthy;
        }

        private static EnvironmentDiffuseVegetationDeltaKind ResolveDeltaKind(
            EnvironmentVegetationCellPlacement previous,
            EnvironmentVegetationCellPlacement current)
        {
            if (previous.VegetationKind != current.VegetationKind)
                return EnvironmentDiffuseVegetationDeltaKind.KindChanged;

            if (ResolveCoverageBand(previous.Density01) != ResolveCoverageBand(current.Density01))
                return EnvironmentDiffuseVegetationDeltaKind.CoverageChanged;

            if (ResolveConditionBand(previous.Health01) != ResolveConditionBand(current.Health01))
                return EnvironmentDiffuseVegetationDeltaKind.ConditionChanged;

            return EnvironmentDiffuseVegetationDeltaKind.None;
        }

        private static EnvironmentDiffuseVegetationDelta FromPlacement(
            EnvironmentDiffuseVegetationDeltaKind kind,
            EnvironmentVegetationCellPlacement placement)
        {
            return new EnvironmentDiffuseVegetationDelta(
                kind,
                placement.AreaId,
                placement.Cell,
                placement.VegetationKind,
                ResolveCoverageBand(placement.Density01),
                ResolveConditionBand(placement.Health01));
        }

        private static Dictionary<VegetationPlacementKey, EnvironmentVegetationCellPlacement> BuildLookup(
            IReadOnlyList<EnvironmentVegetationCellPlacement> placements)
        {
            var lookup = new Dictionary<VegetationPlacementKey, EnvironmentVegetationCellPlacement>();
            if (placements == null)
                return lookup;

            for (int i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                if (!placement.AreaId.IsValid || placement.VegetationKind == EnvironmentVegetationKind.None)
                    continue;

                lookup[new VegetationPlacementKey(placement.AreaId, placement.Cell)] = placement;
            }

            return lookup;
        }

        private static int NormalizeBudget(int maxDeltaCount)
        {
            return maxDeltaCount < 0 ? 0 : maxDeltaCount;
        }

        private readonly struct VegetationPlacementKey
        {
            private readonly EnvironmentAreaId _areaId;
            private readonly EnvironmentCellCoord _cell;

            public VegetationPlacementKey(EnvironmentAreaId areaId, EnvironmentCellCoord cell)
            {
                _areaId = areaId;
                _cell = cell;
            }

            public override bool Equals(object obj)
            {
                return obj is VegetationPlacementKey other
                       && _areaId.Equals(other._areaId)
                       && _cell.Equals(other._cell);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 31) + _areaId.GetHashCode();
                    hash = (hash * 31) + _cell.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
