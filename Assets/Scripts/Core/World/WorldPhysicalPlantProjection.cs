using Arcontio.Core.Environment;

namespace Arcontio.Core
{
    // =============================================================================
    // WorldPhysicalPlantProjection
    // =============================================================================
    /// <summary>
    /// <para>
    /// Proiezione fisica minima nel <see cref="World"/> di una pianta posseduta
    /// biologicamente dalla biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: fisica nel World, biologia nella biosfera</b></para>
    /// <para>
    /// Questa struttura non possiede la simulazione biologica, ma conserva una copia
    /// read-only dello stato minimo necessario ai consumer fisici e visuali: cella,
    /// specie, stadio semantico compatto, banda salute e impatto su movimento/visione.
    /// ArcGraph non deve trovare qui path sprite o tile id: potra' usare questi dati
    /// come input per un resolver visuale esterno e derivato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PlantId</b>: riferimento alla plant instance in EnvironmentState.</item>
    ///   <item><b>AreaId</b>: area biologica sorgente.</item>
    ///   <item><b>Cell</b>: cella fisica occupata.</item>
    ///   <item><b>SpeciesKey</b>: chiave specie biologica, non sprite.</item>
    ///   <item><b>GrowthStageKey</b>: stadio semantico visualmente rilevante, senza sprite diretto.</item>
    ///   <item><b>HealthState</b>: banda salute discreta, non valore fine.</item>
    ///   <item><b>BlocksMovement/BlocksVision/VisionCost</b>: impatto fisico sulla mappa.</item>
    /// </list>
    /// </summary>
    public readonly struct WorldPhysicalPlantProjection
    {
        private static readonly EnvironmentPlantResourceState[] EmptyResources =
            new EnvironmentPlantResourceState[0];

        public readonly EnvironmentPlantId PlantId;
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentCellCoord Cell;
        public readonly string SpeciesKey;
        public readonly string GrowthStageKey;
        public readonly EnvironmentPlantHealthState HealthState;
        public readonly bool IsAlive;
        public readonly bool BlocksMovement;
        public readonly bool BlocksVision;
        public readonly float VisionCost;
        public readonly EnvironmentPlantResourceState[] Resources;

        public WorldPhysicalPlantProjection(
            EnvironmentPlantId plantId,
            EnvironmentAreaId areaId,
            EnvironmentCellCoord cell,
            string speciesKey,
            string growthStageKey,
            EnvironmentPlantHealthState healthState,
            bool isAlive,
            bool blocksMovement,
            bool blocksVision,
            float visionCost,
            System.Collections.Generic.IReadOnlyList<EnvironmentPlantResourceState> resources = null)
        {
            PlantId = plantId;
            AreaId = areaId;
            Cell = cell;
            SpeciesKey = speciesKey ?? string.Empty;
            GrowthStageKey = string.IsNullOrWhiteSpace(growthStageKey)
                ? "unknown"
                : growthStageKey;
            HealthState = healthState;
            IsAlive = isAlive
                && PlantId.IsValid
                && HealthState != EnvironmentPlantHealthState.Dead;
            BlocksMovement = blocksMovement;
            BlocksVision = blocksVision;
            VisionCost = visionCost <= 0f ? 1f : visionCost;
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
}
