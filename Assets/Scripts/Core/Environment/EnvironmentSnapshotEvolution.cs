namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentSnapshotEvolutionReport
    // =============================================================================
    /// <summary>
    /// <para>
    /// Report compatto dell'evoluzione di uno snapshot ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: batch ecologico osservabile</b></para>
    /// <para>
    /// L'evoluzione dello snapshot deve poter essere ispezionata da test, debug e
    /// futuri sistemi di salvataggio senza dipendere da log globali. Il report
    /// conserva conteggi semplici e non espone stato mutabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreasVisited</b>: aree lette dallo snapshot sorgente.</item>
    ///   <item><b>FertilityAreasEvolved</b>: payload fertilita' aggiornati.</item>
    ///   <item><b>WaterAreasEvolved</b>: payload acqua aggiornati.</item>
    ///   <item><b>VegetationAreasEvolved</b>: payload vegetazione aggiornati.</item>
    ///   <item><b>ChangedAreas</b>: aree con almeno un delta osservabile.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentSnapshotEvolutionReport
    {
        public readonly int AreasVisited;
        public readonly int FertilityAreasEvolved;
        public readonly int WaterAreasEvolved;
        public readonly int VegetationAreasEvolved;
        public readonly int ChangedAreas;

        public bool HasChanges => ChangedAreas > 0;

        // =============================================================================
        // EnvironmentSnapshotEvolutionReport
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il report normalizzando conteggi negativi.
        /// </para>
        /// </summary>
        public EnvironmentSnapshotEvolutionReport(
            int areasVisited,
            int fertilityAreasEvolved,
            int waterAreasEvolved,
            int vegetationAreasEvolved,
            int changedAreas)
        {
            AreasVisited = areasVisited < 0 ? 0 : areasVisited;
            FertilityAreasEvolved = fertilityAreasEvolved < 0 ? 0 : fertilityAreasEvolved;
            WaterAreasEvolved = waterAreasEvolved < 0 ? 0 : waterAreasEvolved;
            VegetationAreasEvolved = vegetationAreasEvolved < 0 ? 0 : vegetationAreasEvolved;
            ChangedAreas = changedAreas < 0 ? 0 : changedAreas;
        }
    }

    // =============================================================================
    // EnvironmentSnapshotEvolutionResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato dell'evoluzione di uno snapshot ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: nuovo stato invece di mutazione nascosta</b></para>
    /// <para>
    /// Il resolver restituisce uno <see cref="EnvironmentState"/> materializzato,
    /// lasciando al chiamante la scelta se sostituire lo stato corrente, salvarlo o
    /// scartarlo. Nessuna mutazione globale avviene durante il calcolo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>State</b>: stato evoluto e pronto a produrre snapshot.</item>
    ///   <item><b>Report</b>: conteggi diagnostici dell'evoluzione.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentSnapshotEvolutionResult
    {
        public EnvironmentState State { get; }
        public EnvironmentSnapshotEvolutionReport Report { get; }

        // =============================================================================
        // EnvironmentSnapshotEvolutionResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato aggregando stato e report.
        /// </para>
        /// </summary>
        public EnvironmentSnapshotEvolutionResult(
            EnvironmentState state,
            EnvironmentSnapshotEvolutionReport report)
        {
            State = state ?? new EnvironmentState();
            Report = report;
        }
    }

    // =============================================================================
    // EnvironmentSnapshotEvolutionResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only per evolvere uno snapshot ambientale completo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: batch foundation prima del sistema runtime</b></para>
    /// <para>
    /// Questo resolver non e' un sistema di simulazione e non decide quando essere
    /// chiamato. Riceve snapshot, clima, stagione e transizione gia' risolti, poi
    /// produce un nuovo stato passivo con gli stessi layer aggiornati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EvolveSnapshot</b>: evolve tutte le aree dello snapshot sorgente.</item>
    ///   <item><b>BuildContext</b>: prepara il contesto condiviso per le aree.</item>
    ///   <item><b>CreateNeutral*</b>: payload neutrali usati solo per calcoli mancanti.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentSnapshotEvolutionResolver
    {
        // =============================================================================
        // EvolveSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Evolve uno snapshot ambientale e restituisce un nuovo stato passivo.
        /// </para>
        /// </summary>
        public static EnvironmentSnapshotEvolutionResult EvolveSnapshot(
            EnvironmentSnapshot snapshot,
            EnvironmentTemporalTransition transition,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentBiomeProfile biomeProfile = default)
        {
            var nextState = new EnvironmentState();
            var currentCalendar = transition.Current;
            nextState.SetCalendar(currentCalendar);
            nextState.SetClimate(climate);

            var areas = snapshot?.Areas;
            if (areas == null || areas.Count == 0)
            {
                return new EnvironmentSnapshotEvolutionResult(
                    nextState,
                    new EnvironmentSnapshotEvolutionReport(0, 0, 0, 0, 0));
            }

            int fertilityEvolved = 0;
            int waterEvolved = 0;
            int vegetationEvolved = 0;
            int changedAreas = 0;
            var context = BuildContext(
                currentCalendar,
                climate,
                seasonProfile,
                transition,
                biomeProfile);

            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                nextState.SetAreaDefinition(area.Definition);

                var fertility = area.HasFertility
                    ? area.FertilityState
                    : CreateNeutralFertility(area.Definition.AreaId);
                var water = area.HasWater
                    ? area.WaterState
                    : CreateNeutralWater(area.Definition.AreaId);
                var vegetation = area.HasVegetation
                    ? area.VegetationState
                    : CreateNeutralVegetation(area.Definition.AreaId);
                var evolved = EnvironmentAreaEvolutionResolver.Evolve(
                    fertility,
                    water,
                    vegetation,
                    context);

                // I payload assenti restano assenti: i neutrali servono solo a evitare
                // che formule dipendenti da layer opzionali inventino nuovi layer.
                if (area.HasFertility)
                {
                    nextState.SetFertilityArea(evolved.Fertility);
                    fertilityEvolved++;
                }

                if (area.HasWater)
                {
                    nextState.SetWaterArea(evolved.Water);
                    waterEvolved++;
                }

                if (area.HasVegetation)
                {
                    nextState.SetVegetationArea(evolved.Vegetation);
                    vegetationEvolved++;
                }

                if (area.HasSeedBank)
                {
                    // La seed bank viene preservata come layer passivo. Germinazione,
                    // dispersione e consumo semi richiederanno regole dedicate.
                    nextState.SetSeedBankArea(area.SeedBankState);
                }

                if (evolved.Delta.HasAnyDelta)
                    changedAreas++;
            }

            var plants = snapshot?.Plants;
            if (plants != null)
            {
                for (int i = 0; i < plants.Count; i++)
                {
                    var plant = plants[i];
                    nextState.SetPlantInstance(new EnvironmentPlantInstance(
                        plant.PlantId,
                        plant.SpeciesKey,
                        plant.Cell,
                        plant.AgeDays,
                        plant.GrowthStage,
                        plant.GrowthStageKey,
                        plant.HealthState,
                        plant.Health01,
                        plant.Maturity01,
                        plant.IsHarvestable,
                        plant.SourceAreaId));
                }
            }

            return new EnvironmentSnapshotEvolutionResult(
                nextState,
                new EnvironmentSnapshotEvolutionReport(
                    areas.Count,
                    fertilityEvolved,
                    waterEvolved,
                    vegetationEvolved,
                    changedAreas));
        }

        private static EnvironmentAreaEvolutionContext BuildContext(
            EnvironmentCalendarState calendar,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentTemporalTransition transition,
            EnvironmentBiomeProfile biomeProfile)
        {
            return new EnvironmentAreaEvolutionContext(
                calendar,
                climate,
                seasonProfile,
                transition,
                biomeProfile.IsValid
                    ? biomeProfile
                    : EnvironmentBiomeProfile.Default);
        }

        private static EnvironmentFertilityAreaState CreateNeutralFertility(
            EnvironmentAreaId areaId)
        {
            return new EnvironmentFertilityAreaState(
                areaId,
                EnvironmentSoilKind.Generic,
                0.5f,
                0.5f,
                0.5f,
                0f,
                0.5f);
        }

        private static EnvironmentWaterAreaState CreateNeutralWater(
            EnvironmentAreaId areaId)
        {
            return new EnvironmentWaterAreaState(
                areaId,
                EnvironmentWaterKind.Still,
                EnvironmentWaterDepthLevel.Shallow,
                0.5f,
                0f,
                true,
                false);
        }

        private static EnvironmentVegetationAreaState CreateNeutralVegetation(
            EnvironmentAreaId areaId)
        {
            return new EnvironmentVegetationAreaState(
                areaId,
                EnvironmentVegetationKind.None,
                0f,
                0.5f,
                0.5f,
                0.5f,
                0.5f);
        }
    }
}
