using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentSnapshotAreaChangeKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo di cambiamento rilevato su un'area tra due snapshot ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: cambiamento come dato, non evento globale</b></para>
    /// <para>
    /// La foundation espone una descrizione passiva delle differenze. Sistemi futuri
    /// potranno trasformarla in eventi, debug o invalidazioni cache, ma questo enum
    /// non notifica e non possiede lifecycle.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Added</b>: area presente solo nello snapshot corrente.</item>
    ///   <item><b>Removed</b>: area presente solo nello snapshot precedente.</item>
    ///   <item><b>Modified</b>: area presente in entrambi ma con dati diversi.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentSnapshotAreaChangeKind
    {
        Added = 0,
        Removed = 10,
        Modified = 20
    }

    // =============================================================================
    // EnvironmentSnapshotAreaChange
    // =============================================================================
    /// <summary>
    /// <para>
    /// Cambiamento read-only di una singola area ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diff leggibile senza accesso allo stato</b></para>
    /// <para>
    /// Il cambiamento contiene copie degli snapshot area precedente e corrente. Chi
    /// lo legge non puo' mutare il registry ambientale e non deve conoscere i
    /// dizionari interni di <see cref="EnvironmentState"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area coinvolta.</item>
    ///   <item><b>Kind</b>: tipo di cambiamento.</item>
    ///   <item><b>Previous</b>: snapshot precedente, se presente.</item>
    ///   <item><b>Current</b>: snapshot corrente, se presente.</item>
    ///   <item><b>LayerMask</b>: bitmask leggera dei layer modificati.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentSnapshotAreaChange
    {
        public const int DefinitionLayer = 1;
        public const int FertilityLayer = 2;
        public const int WaterLayer = 4;
        public const int VegetationLayer = 8;
        public const int SeedBankLayer = 16;

        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentSnapshotAreaChangeKind Kind;
        public readonly EnvironmentAreaSnapshot Previous;
        public readonly EnvironmentAreaSnapshot Current;
        public readonly int LayerMask;

        public bool HasDefinitionChange => (LayerMask & DefinitionLayer) != 0;
        public bool HasFertilityChange => (LayerMask & FertilityLayer) != 0;
        public bool HasWaterChange => (LayerMask & WaterLayer) != 0;
        public bool HasVegetationChange => (LayerMask & VegetationLayer) != 0;
        public bool HasSeedBankChange => (LayerMask & SeedBankLayer) != 0;

        // =============================================================================
        // EnvironmentSnapshotAreaChange
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il cambiamento di una singola area.
        /// </para>
        /// </summary>
        public EnvironmentSnapshotAreaChange(
            EnvironmentAreaId areaId,
            EnvironmentSnapshotAreaChangeKind kind,
            EnvironmentAreaSnapshot previous,
            EnvironmentAreaSnapshot current,
            int layerMask)
        {
            AreaId = areaId;
            Kind = kind;
            Previous = previous;
            Current = current;
            LayerMask = layerMask;
        }
    }

    // =============================================================================
    // EnvironmentSnapshotDiffResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato aggregato del confronto tra due snapshot ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: invalidazione futura senza coupling visuale</b></para>
    /// <para>
    /// Il diff permette a consumer futuri di sapere cosa e' cambiato senza dipendere
    /// da ArcGraph, cache renderer o sistemi di mondo. Rimane un oggetto Core
    /// read-only e materializzato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Changes</b>: lista dei cambiamenti rilevati.</item>
    ///   <item><b>AddedCount</b>: numero di aree aggiunte.</item>
    ///   <item><b>RemovedCount</b>: numero di aree rimosse.</item>
    ///   <item><b>ModifiedCount</b>: numero di aree modificate.</item>
    ///   <item><b>HasChanges</b>: presenza di almeno una differenza.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentSnapshotDiffResult
    {
        private static readonly EnvironmentSnapshotAreaChange[] EmptyChanges =
            new EnvironmentSnapshotAreaChange[0];

        public IReadOnlyList<EnvironmentSnapshotAreaChange> Changes { get; }
        public int AddedCount { get; }
        public int RemovedCount { get; }
        public int ModifiedCount { get; }
        public bool HasChanges => Changes.Count > 0;

        // =============================================================================
        // EnvironmentSnapshotDiffResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato calcolando i conteggi per tipo.
        /// </para>
        /// </summary>
        public EnvironmentSnapshotDiffResult(
            IReadOnlyList<EnvironmentSnapshotAreaChange> changes)
        {
            Changes = changes ?? EmptyChanges;

            // I conteggi vengono materializzati per rendere economici i controlli
            // piu' frequenti di debug, test e future invalidazioni.
            for (int i = 0; i < Changes.Count; i++)
            {
                if (Changes[i].Kind == EnvironmentSnapshotAreaChangeKind.Added)
                    AddedCount++;
                else if (Changes[i].Kind == EnvironmentSnapshotAreaChangeKind.Removed)
                    RemovedCount++;
                else
                    ModifiedCount++;
            }
        }
    }

    // =============================================================================
    // EnvironmentSnapshotDiffResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only del diff tra snapshot ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: confronto Core prima degli adapter</b></para>
    /// <para>
    /// Il resolver confronta solo strutture Core. Non conosce renderer, pathfinding,
    /// job system o salvataggi; espone una lista di cambiamenti che altri strati
    /// potranno interpretare in modo progressivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Diff</b>: confronta snapshot precedente e corrente.</item>
    ///   <item><b>TryFindArea</b>: lookup lineare per id area.</item>
    ///   <item><b>ComputeLayerMask</b>: individua quali layer sono cambiati.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentSnapshotDiffResolver
    {
        // =============================================================================
        // Diff
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta due snapshot ambientali e restituisce i cambiamenti area-based.
        /// </para>
        /// </summary>
        public static EnvironmentSnapshotDiffResult Diff(
            EnvironmentSnapshot previous,
            EnvironmentSnapshot current)
        {
            var changes = new List<EnvironmentSnapshotAreaChange>();
            var previousAreas = previous?.Areas ?? new EnvironmentAreaSnapshot[0];
            var currentAreas = current?.Areas ?? new EnvironmentAreaSnapshot[0];

            for (int i = 0; i < currentAreas.Count; i++)
            {
                var currentArea = currentAreas[i];
                var areaId = currentArea.Definition.AreaId;
                if (!TryFindArea(previousAreas, areaId, out var previousArea))
                {
                    changes.Add(new EnvironmentSnapshotAreaChange(
                        areaId,
                        EnvironmentSnapshotAreaChangeKind.Added,
                        default,
                        currentArea,
                        EnvironmentSnapshotAreaChange.DefinitionLayer
                        | EnvironmentSnapshotAreaChange.FertilityLayer
                        | EnvironmentSnapshotAreaChange.WaterLayer
                        | EnvironmentSnapshotAreaChange.VegetationLayer
                        | EnvironmentSnapshotAreaChange.SeedBankLayer));
                    continue;
                }

                int layerMask = ComputeLayerMask(previousArea, currentArea);
                if (layerMask == 0)
                    continue;

                changes.Add(new EnvironmentSnapshotAreaChange(
                    areaId,
                    EnvironmentSnapshotAreaChangeKind.Modified,
                    previousArea,
                    currentArea,
                    layerMask));
            }

            for (int i = 0; i < previousAreas.Count; i++)
            {
                var previousArea = previousAreas[i];
                var areaId = previousArea.Definition.AreaId;
                if (TryFindArea(currentAreas, areaId, out _))
                    continue;

                changes.Add(new EnvironmentSnapshotAreaChange(
                    areaId,
                    EnvironmentSnapshotAreaChangeKind.Removed,
                    previousArea,
                    default,
                    EnvironmentSnapshotAreaChange.DefinitionLayer
                    | EnvironmentSnapshotAreaChange.FertilityLayer
                    | EnvironmentSnapshotAreaChange.WaterLayer
                    | EnvironmentSnapshotAreaChange.VegetationLayer
                    | EnvironmentSnapshotAreaChange.SeedBankLayer));
            }

            return new EnvironmentSnapshotDiffResult(changes);
        }

        private static bool TryFindArea(
            IReadOnlyList<EnvironmentAreaSnapshot> areas,
            EnvironmentAreaId areaId,
            out EnvironmentAreaSnapshot area)
        {
            area = default;

            if (!areaId.IsValid)
                return false;

            for (int i = 0; i < areas.Count; i++)
            {
                if (!areas[i].Definition.AreaId.Equals(areaId))
                    continue;

                area = areas[i];
                return true;
            }

            return false;
        }

        private static int ComputeLayerMask(
            EnvironmentAreaSnapshot previous,
            EnvironmentAreaSnapshot current)
        {
            int mask = 0;

            if (!AreaDefinitionsEqual(previous.Definition, current.Definition))
                mask |= EnvironmentSnapshotAreaChange.DefinitionLayer;

            if (previous.HasFertility != current.HasFertility
                || (previous.HasFertility
                    && !FertilityEqual(previous.FertilityState, current.FertilityState)))
            {
                mask |= EnvironmentSnapshotAreaChange.FertilityLayer;
            }

            if (previous.HasWater != current.HasWater
                || (previous.HasWater
                    && !WaterEqual(previous.WaterState, current.WaterState)))
            {
                mask |= EnvironmentSnapshotAreaChange.WaterLayer;
            }

            if (previous.HasVegetation != current.HasVegetation
                || (previous.HasVegetation
                    && !VegetationEqual(previous.VegetationState, current.VegetationState)))
            {
                mask |= EnvironmentSnapshotAreaChange.VegetationLayer;
            }

            if (previous.HasSeedBank != current.HasSeedBank
                || (previous.HasSeedBank
                    && !SeedBankEqual(previous.SeedBankState, current.SeedBankState)))
            {
                mask |= EnvironmentSnapshotAreaChange.SeedBankLayer;
            }

            return mask;
        }

        private static bool AreaDefinitionsEqual(
            EnvironmentAreaDefinition left,
            EnvironmentAreaDefinition right)
        {
            return left.AreaId.Equals(right.AreaId)
                   && left.Kind == right.Kind
                   && left.Bounds.MinX == right.Bounds.MinX
                   && left.Bounds.MinY == right.Bounds.MinY
                   && left.Bounds.MaxX == right.Bounds.MaxX
                   && left.Bounds.MaxY == right.Bounds.MaxY
                   && left.Bounds.Z == right.Bounds.Z
                   && left.Priority == right.Priority
                   && left.IsEnabled == right.IsEnabled
                   && left.Key == right.Key;
        }

        private static bool FertilityEqual(
            EnvironmentFertilityAreaState left,
            EnvironmentFertilityAreaState right)
        {
            return left.AreaId.Equals(right.AreaId)
                   && left.SoilKind == right.SoilKind
                   && left.BaseFertility01 == right.BaseFertility01
                   && left.CurrentFertility01 == right.CurrentFertility01
                   && left.GrowthModifier01 == right.GrowthModifier01
                   && left.Exhaustion01 == right.Exhaustion01
                   && left.Recovery01 == right.Recovery01;
        }

        private static bool WaterEqual(
            EnvironmentWaterAreaState left,
            EnvironmentWaterAreaState right)
        {
            return left.AreaId.Equals(right.AreaId)
                   && left.WaterKind == right.WaterKind
                   && left.DepthLevel == right.DepthLevel
                   && left.WaterLevel01 == right.WaterLevel01
                   && left.FlowIntensity01 == right.FlowIntensity01
                   && left.IsDrinkable == right.IsDrinkable
                   && left.IsSeasonal == right.IsSeasonal;
        }

        private static bool VegetationEqual(
            EnvironmentVegetationAreaState left,
            EnvironmentVegetationAreaState right)
        {
            return left.AreaId.Equals(right.AreaId)
                   && left.VegetationKind == right.VegetationKind
                   && left.Density01 == right.Density01
                   && left.GrowthPotential01 == right.GrowthPotential01
                   && left.Health01 == right.Health01
                   && left.FertilityInfluence01 == right.FertilityInfluence01
                   && left.ClimateInfluence01 == right.ClimateInfluence01;
        }

        private static bool SeedBankEqual(
            EnvironmentSeedBankAreaState left,
            EnvironmentSeedBankAreaState right)
        {
            if (left == null || right == null)
                return left == right;

            if (!left.AreaId.Equals(right.AreaId)
                || left.Entries.Count != right.Entries.Count
                || left.TotalAmount01 != right.TotalAmount01
                || left.AverageViability01 != right.AverageViability01)
            {
                return false;
            }

            for (int i = 0; i < left.Entries.Count; i++)
            {
                if (left.Entries[i].SpeciesKey != right.Entries[i].SpeciesKey
                    || left.Entries[i].Amount01 != right.Entries[i].Amount01
                    || left.Entries[i].Viability01 != right.Entries[i].Viability01)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
