using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentPhysicalPlantDeltaKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo di variazione fisicamente rilevante prodotta dalla biosfera per una
    /// pianta concreta.
    /// </para>
    ///
    /// <para><b>Principio architetturale: delta espliciti, nessun polling globale obbligatorio</b></para>
    /// <para>
    /// La biosfera non deve costringere il <c>World</c> a ricostruire sempre tutte
    /// le piante quando una sola pianta nasce, muore o cambia stadio. Questo enum
    /// descrive il motivo minimo del delta in modo deterministico e serializzabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: valore nullo difensivo.</item>
    ///   <item><b>Born</b>: nuova pianta fisica da proiettare nel World.</item>
    ///   <item><b>Died</b>: pianta fisica da rimuovere dal World.</item>
    ///   <item><b>StateChanged</b>: banda salute o stato semantico cambiati senza cambio cella.</item>
    ///   <item><b>StageChanged</b>: cambio stadio, quindi potenzialmente cambio visuale.</item>
    ///   <item><b>Relocated</b>: cella fisica cambiata, caso raro ma utile per restore/tools futuri.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentPhysicalPlantDeltaKind
    {
        None = 0,
        Born = 1,
        Died = 2,
        StateChanged = 3,
        StageChanged = 4,
        Relocated = 5,
        ResourceChanged = 6
    }

    // =============================================================================
    // EnvironmentPhysicalPlantDelta
    // =============================================================================
    /// <summary>
    /// <para>
    /// Delta data-only che comunica al <c>World</c> una variazione fisica o
    /// visualmente rilevante di una pianta della biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Biosfera decide il dato, World applica la fisica</b></para>
    /// <para>
    /// Il delta contiene coordinate, specie, stadio semantico compatto e banda
    /// salute, ma non contiene sprite o valori biologici fini. La scelta dello sprite
    /// resta di un futuro adapter visuale/ArcGraph, mentre il <c>World</c> usa il
    /// delta solo per mantenere occupazione fisica, occlusione e dati minimi
    /// consultabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: motivo della variazione.</item>
    ///   <item><b>PlantId</b>: identita' stabile della pianta nella biosfera.</item>
    ///   <item><b>AreaId</b>: area biologica sorgente.</item>
    ///   <item><b>Cell/PreviousCell</b>: cella nuova e cella precedente, se nota.</item>
    ///   <item><b>SpeciesKey</b>: tipo biologico della pianta, non sprite.</item>
    ///   <item><b>GrowthStageKey/HealthState</b>: stato semantico minimo da proiettare.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPhysicalPlantDelta
    {
        public readonly EnvironmentPhysicalPlantDeltaKind Kind;
        public readonly EnvironmentPlantId PlantId;
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentCellCoord Cell;
        public readonly EnvironmentCellCoord PreviousCell;
        public readonly string SpeciesKey;
        public readonly string GrowthStageKey;
        public readonly EnvironmentPlantHealthState HealthState;
        public readonly bool IsAlive;

        public bool IsValid =>
            Kind != EnvironmentPhysicalPlantDeltaKind.None
            && PlantId.IsValid;

        // =============================================================================
        // EnvironmentPhysicalPlantDelta
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un delta pianta fisica normalizzando campi numerici e stringhe.
        /// </para>
        /// </summary>
        public EnvironmentPhysicalPlantDelta(
            EnvironmentPhysicalPlantDeltaKind kind,
            EnvironmentPlantId plantId,
            EnvironmentAreaId areaId,
            EnvironmentCellCoord cell,
            EnvironmentCellCoord previousCell,
            string speciesKey,
            string growthStageKey,
            EnvironmentPlantHealthState healthState,
            bool isAlive)
        {
            Kind = kind;
            PlantId = plantId;
            AreaId = areaId;
            Cell = cell;
            PreviousCell = previousCell;
            SpeciesKey = speciesKey ?? string.Empty;
            GrowthStageKey = string.IsNullOrWhiteSpace(growthStageKey)
                ? "unknown"
                : growthStageKey;
            HealthState = healthState;
            IsAlive = isAlive
                && PlantId.IsValid
                && HealthState != EnvironmentPlantHealthState.Dead;
        }

        // =============================================================================
        // FromPlant
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un delta copiando lo stato corrente di una <see cref="EnvironmentPlantInstance"/>.
        /// </para>
        /// </summary>
        public static EnvironmentPhysicalPlantDelta FromPlant(
            EnvironmentPhysicalPlantDeltaKind kind,
            EnvironmentPlantInstance plant,
            EnvironmentCellCoord previousCell)
        {
            return new EnvironmentPhysicalPlantDelta(
                kind,
                plant.PlantId,
                plant.SourceAreaId,
                plant.Cell,
                previousCell,
                plant.SpeciesKey,
                plant.GrowthStageKey,
                plant.HealthState,
                plant.IsAlive);
        }
    }

    // =============================================================================
    // EnvironmentPhysicalPlantDeltaProducer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Produce automaticamente i delta fisici delle piante confrontando due snapshot
    /// ambientali consecutivi.
    /// </para>
    ///
    /// <para><b>Principio architetturale: la biosfera produce fatti minimi, il World applica</b></para>
    /// <para>
    /// Questo producer e' il punto data-only che trasforma il risultato di un ciclo
    /// giornaliero in messaggi compatti per il <c>World</c>. Non chiama il World, non
    /// marca celle sporche e non sceglie sprite. Si limita a dire quali piante sono
    /// nate, morte, spostate o cambiate in modo rilevante per il boundary.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DiffSnapshots</b>: confronta due snapshot e materializza i delta.</item>
    ///   <item><b>ResolveDeltaKind</b>: sceglie il motivo principale del cambiamento.</item>
    ///   <item><b>BuildLookup</b>: indicizza le piante per <see cref="EnvironmentPlantId"/>.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentPhysicalPlantDeltaProducer
    {
        // =============================================================================
        // DiffSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta snapshot precedente e corrente e produce delta pianta fisica.
        /// </para>
        /// </summary>
        public static IReadOnlyList<EnvironmentPhysicalPlantDelta> DiffSnapshots(
            EnvironmentSnapshot previous,
            EnvironmentSnapshot current,
            int maxDeltaCount = int.MaxValue)
        {
            var deltas = new List<EnvironmentPhysicalPlantDelta>();
            int budget = NormalizeBudget(maxDeltaCount);
            if (budget <= 0)
                return deltas;

            var previousPlants = previous?.Plants ?? new EnvironmentPlantSnapshot[0];
            var currentPlants = current?.Plants ?? new EnvironmentPlantSnapshot[0];
            var previousById = BuildLookup(previousPlants);
            var currentById = BuildLookup(currentPlants);

            for (int i = 0; i < currentPlants.Count && deltas.Count < budget; i++)
            {
                var currentPlant = currentPlants[i];
                if (!currentPlant.PlantId.IsValid)
                    continue;

                if (!previousById.TryGetValue(currentPlant.PlantId, out var previousPlant))
                {
                    if (currentPlant.IsAlive)
                        deltas.Add(FromSnapshot(EnvironmentPhysicalPlantDeltaKind.Born, currentPlant, default));

                    continue;
                }

                var kind = ResolveDeltaKind(previousPlant, currentPlant);
                if (kind == EnvironmentPhysicalPlantDeltaKind.None)
                    continue;

                deltas.Add(FromSnapshot(kind, currentPlant, previousPlant.Cell));
            }

            for (int i = 0; i < previousPlants.Count && deltas.Count < budget; i++)
            {
                var previousPlant = previousPlants[i];
                if (!previousPlant.PlantId.IsValid || currentById.ContainsKey(previousPlant.PlantId))
                    continue;

                deltas.Add(FromSnapshot(EnvironmentPhysicalPlantDeltaKind.Died, previousPlant, previousPlant.Cell));
            }

            return deltas;
        }

        private static EnvironmentPhysicalPlantDeltaKind ResolveDeltaKind(
            EnvironmentPlantSnapshot previous,
            EnvironmentPlantSnapshot current)
        {
            if (previous.IsAlive && !current.IsAlive)
                return EnvironmentPhysicalPlantDeltaKind.Died;

            if (!previous.IsAlive && current.IsAlive)
                return EnvironmentPhysicalPlantDeltaKind.Born;

            if (!previous.Cell.Equals(current.Cell))
                return EnvironmentPhysicalPlantDeltaKind.Relocated;

            if (previous.GrowthStageKey != current.GrowthStageKey)
                return EnvironmentPhysicalPlantDeltaKind.StageChanged;

            if (previous.HealthState != current.HealthState)
                return EnvironmentPhysicalPlantDeltaKind.StateChanged;

            if (!EnvironmentPlantResourceStateResolver.AreEquivalent(
                    previous.Resources,
                    current.Resources))
            {
                return EnvironmentPhysicalPlantDeltaKind.ResourceChanged;
            }

            // Cambiamenti biologici fini restano nella biosfera: non devono produrre
            // lavoro fisico/visuale ogni giorno se non cambiano una banda esposta.
            return EnvironmentPhysicalPlantDeltaKind.None;
        }

        private static EnvironmentPhysicalPlantDelta FromSnapshot(
            EnvironmentPhysicalPlantDeltaKind kind,
            EnvironmentPlantSnapshot plant,
            EnvironmentCellCoord previousCell)
        {
            return new EnvironmentPhysicalPlantDelta(
                kind,
                plant.PlantId,
                plant.SourceAreaId,
                plant.Cell,
                previousCell,
                plant.SpeciesKey,
                plant.GrowthStageKey,
                plant.HealthState,
                plant.IsAlive && kind != EnvironmentPhysicalPlantDeltaKind.Died);
        }

        private static Dictionary<EnvironmentPlantId, EnvironmentPlantSnapshot> BuildLookup(
            IReadOnlyList<EnvironmentPlantSnapshot> plants)
        {
            var lookup = new Dictionary<EnvironmentPlantId, EnvironmentPlantSnapshot>();
            if (plants == null)
                return lookup;

            for (int i = 0; i < plants.Count; i++)
            {
                var plant = plants[i];
                if (!plant.PlantId.IsValid)
                    continue;

                lookup[plant.PlantId] = plant;
            }

            return lookup;
        }

        private static int NormalizeBudget(int maxDeltaCount)
        {
            return maxDeltaCount < 0 ? 0 : maxDeltaCount;
        }
    }
}
