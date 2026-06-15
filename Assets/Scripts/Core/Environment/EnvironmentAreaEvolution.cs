namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentAreaEvolutionContext
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contesto data-only per evolvere payload ambientali area-based.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dinamica biosfera senza sistema runtime</b></para>
    /// <para>
    /// La crescita vegetale, il recupero della fertilita' e la variazione dell'acqua
    /// devono poter essere calcolati da dati gia' risolti. Il contesto non possiede
    /// tempo, non interroga il mondo e non accede a renderer o pathfinding.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Calendar</b>: calendario corrente.</item>
    ///   <item><b>Climate</b>: clima globale corrente.</item>
    ///   <item><b>SeasonProfile</b>: profilo stagionale usato dai bias ecologici.</item>
    ///   <item><b>Transition</b>: confine temporale che abilita update giornalieri.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAreaEvolutionContext
    {
        public readonly EnvironmentCalendarState Calendar;
        public readonly EnvironmentGlobalClimateState Climate;
        public readonly EnvironmentSeasonProfile SeasonProfile;
        public readonly EnvironmentTemporalTransition Transition;

        public bool ShouldRunDailyEvolution => Transition.DayChanged;

        // =============================================================================
        // EnvironmentAreaEvolutionContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il contesto di evoluzione dai dati ambientali risolti.
        /// </para>
        /// </summary>
        public EnvironmentAreaEvolutionContext(
            EnvironmentCalendarState calendar,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentTemporalTransition transition)
        {
            Calendar = calendar;
            Climate = climate;
            SeasonProfile = seasonProfile;
            Transition = transition;
        }
    }

    // =============================================================================
    // EnvironmentAreaEvolutionDelta
    // =============================================================================
    /// <summary>
    /// <para>
    /// Delta compatto prodotto da una evoluzione giornaliera di area.
    /// </para>
    ///
    /// <para><b>Principio architetturale: risultato osservabile e applicazione separata</b></para>
    /// <para>
    /// Il resolver restituisce sia il nuovo valore sia le variazioni principali. In
    /// futuro diagnostica, log o salvataggi potranno leggere il delta senza dover
    /// ricalcolare la differenza dai payload precedenti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FertilityDelta01</b>: variazione fertilita' corrente.</item>
    ///   <item><b>WaterLevelDelta01</b>: variazione livello acqua.</item>
    ///   <item><b>VegetationDensityDelta01</b>: variazione densita' vegetale.</item>
    ///   <item><b>VegetationHealthDelta01</b>: variazione salute vegetale.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAreaEvolutionDelta
    {
        public readonly float FertilityDelta01;
        public readonly float WaterLevelDelta01;
        public readonly float VegetationDensityDelta01;
        public readonly float VegetationHealthDelta01;

        public bool HasAnyDelta =>
            FertilityDelta01 != 0f
            || WaterLevelDelta01 != 0f
            || VegetationDensityDelta01 != 0f
            || VegetationHealthDelta01 != 0f;

        // =============================================================================
        // EnvironmentAreaEvolutionDelta
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il delta normalizzato dell'evoluzione.
        /// </para>
        /// </summary>
        public EnvironmentAreaEvolutionDelta(
            float fertilityDelta01,
            float waterLevelDelta01,
            float vegetationDensityDelta01,
            float vegetationHealthDelta01)
        {
            FertilityDelta01 = fertilityDelta01;
            WaterLevelDelta01 = waterLevelDelta01;
            VegetationDensityDelta01 = vegetationDensityDelta01;
            VegetationHealthDelta01 = vegetationHealthDelta01;
        }
    }

    // =============================================================================
    // EnvironmentAreaEvolutionResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato aggregato dell'evoluzione di un'area ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: payload specializzati ancora separati</b></para>
    /// <para>
    /// Anche dopo l'evoluzione, fertilita', acqua e vegetazione restano payload
    /// indipendenti. Nessuna struttura monolitica assorbe i layer, cosi' la biosfera
    /// puo' continuare a crescere per integrazione progressiva.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Fertility</b>: nuovo payload fertilita'.</item>
    ///   <item><b>Water</b>: nuovo payload acqua.</item>
    ///   <item><b>Vegetation</b>: nuovo payload vegetazione diffusa.</item>
    ///   <item><b>Delta</b>: variazioni principali prodotte dal calcolo.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAreaEvolutionResult
    {
        public readonly EnvironmentFertilityAreaState Fertility;
        public readonly EnvironmentWaterAreaState Water;
        public readonly EnvironmentVegetationAreaState Vegetation;
        public readonly EnvironmentAreaEvolutionDelta Delta;

        // =============================================================================
        // EnvironmentAreaEvolutionResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato dell'evoluzione area-based.
        /// </para>
        /// </summary>
        public EnvironmentAreaEvolutionResult(
            EnvironmentFertilityAreaState fertility,
            EnvironmentWaterAreaState water,
            EnvironmentVegetationAreaState vegetation,
            EnvironmentAreaEvolutionDelta delta)
        {
            Fertility = fertility;
            Water = water;
            Vegetation = vegetation;
            Delta = delta;
        }
    }

    // =============================================================================
    // EnvironmentAreaEvolutionResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver data-only dell'evoluzione giornaliera dei layer ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: regole ecologiche pure e sostituibili</b></para>
    /// <para>
    /// Le formule di v0.39 sono conservative e servono a fissare la pipeline. Non
    /// sono un modello definitivo: i coefficienti potranno migrare in configurazione
    /// o essere sostituiti da sistemi piu' ricchi senza cambiare i payload base.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Evolve</b>: evolve insieme fertilita', acqua e vegetazione.</item>
    ///   <item><b>EvolveFertility</b>: recupera o consuma fertilita' giornaliera.</item>
    ///   <item><b>EvolveWater</b>: modifica livello acqua da meteo e aridita'.</item>
    ///   <item><b>EvolveVegetation</b>: modifica densita' e salute vegetale.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentAreaEvolutionResolver
    {
        // =============================================================================
        // Evolve
        // =============================================================================
        /// <summary>
        /// <para>
        /// Evolve i tre payload ambientali principali di una stessa area.
        /// </para>
        /// </summary>
        public static EnvironmentAreaEvolutionResult Evolve(
            EnvironmentFertilityAreaState fertility,
            EnvironmentWaterAreaState water,
            EnvironmentVegetationAreaState vegetation,
            EnvironmentAreaEvolutionContext context)
        {
            var nextFertility = EvolveFertility(fertility, context);
            var nextWater = EvolveWater(water, context);
            var nextVegetation = EvolveVegetation(
                vegetation,
                nextFertility,
                nextWater,
                context);

            var delta = new EnvironmentAreaEvolutionDelta(
                nextFertility.CurrentFertility01 - fertility.CurrentFertility01,
                nextWater.WaterLevel01 - water.WaterLevel01,
                nextVegetation.Density01 - vegetation.Density01,
                nextVegetation.Health01 - vegetation.Health01);

            return new EnvironmentAreaEvolutionResult(
                nextFertility,
                nextWater,
                nextVegetation,
                delta);
        }

        // =============================================================================
        // EvolveFertility
        // =============================================================================
        /// <summary>
        /// <para>
        /// Evolve la fertilita' corrente con una cadenza giornaliera.
        /// </para>
        /// </summary>
        public static EnvironmentFertilityAreaState EvolveFertility(
            EnvironmentFertilityAreaState fertility,
            EnvironmentAreaEvolutionContext context)
        {
            if (!context.ShouldRunDailyEvolution)
                return fertility;

            // La fertilita' recupera verso la base quando umidita' e stagione sono
            // favorevoli, ma l'esaurimento trattiene parte del recupero.
            float moistureSupport01 = context.Climate.Humidity01 * 0.6f
                                      + context.SeasonProfile.FertilityBias01 * 0.4f;
            float recoveryStep = fertility.Recovery01 * moistureSupport01 * 0.035f;
            float exhaustionDrag = fertility.Exhaustion01 * 0.020f;
            float target = fertility.BaseFertility01;
            float direction = target >= fertility.CurrentFertility01 ? 1f : -1f;
            float nextCurrent = fertility.CurrentFertility01
                                + (recoveryStep * direction)
                                - exhaustionDrag;

            return new EnvironmentFertilityAreaState(
                fertility.AreaId,
                fertility.SoilKind,
                fertility.BaseFertility01,
                nextCurrent,
                fertility.GrowthModifier01,
                fertility.Exhaustion01,
                fertility.Recovery01);
        }

        // =============================================================================
        // EvolveWater
        // =============================================================================
        /// <summary>
        /// <para>
        /// Evolve il livello acqua con una cadenza giornaliera.
        /// </para>
        /// </summary>
        public static EnvironmentWaterAreaState EvolveWater(
            EnvironmentWaterAreaState water,
            EnvironmentAreaEvolutionContext context)
        {
            if (!context.ShouldRunDailyEvolution)
                return water;

            float precipitationGain = context.Climate.Weather.Precipitation01 * 0.060f;
            float humidityGain = context.Climate.Humidity01 * 0.015f;
            float evaporationLoss = context.Climate.Aridity01 * 0.040f
                                    + context.Climate.Temperature01 * 0.020f;

            // L'acqua stagionale risponde di piu' al meteo; laghi e fiumi stabili
            // cambiano lentamente in questa foundation.
            float seasonalMultiplier = water.IsSeasonal ? 1.35f : 0.65f;
            float nextLevel = water.WaterLevel01
                              + ((precipitationGain + humidityGain - evaporationLoss)
                                 * seasonalMultiplier);

            return new EnvironmentWaterAreaState(
                water.AreaId,
                water.WaterKind,
                ResolveDepthFromLevel(water.DepthLevel, nextLevel),
                nextLevel,
                water.FlowIntensity01,
                water.IsDrinkable,
                water.IsSeasonal);
        }

        // =============================================================================
        // EvolveVegetation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Evolve vegetazione diffusa usando fertilita', acqua, clima e stagione.
        /// </para>
        /// </summary>
        public static EnvironmentVegetationAreaState EvolveVegetation(
            EnvironmentVegetationAreaState vegetation,
            EnvironmentFertilityAreaState fertility,
            EnvironmentWaterAreaState water,
            EnvironmentAreaEvolutionContext context)
        {
            if (!context.ShouldRunDailyEvolution)
                return vegetation;

            float fertilitySupport = fertility.CurrentFertility01 * vegetation.FertilityInfluence01;
            float climateSupport = context.SeasonProfile.VegetationGrowthBias01
                                   * vegetation.ClimateInfluence01;
            float waterSupport = water.WaterLevel01 * 0.5f + context.Climate.Humidity01 * 0.5f;
            float stress = context.Climate.Aridity01 * 0.045f
                           + fertility.Exhaustion01 * 0.030f;
            float growthPressure = (fertilitySupport + climateSupport + waterSupport) / 3f;
            float densityDelta = (growthPressure * vegetation.GrowthPotential01 * 0.035f) - stress;
            float healthDelta = ((growthPressure - 0.45f) * 0.050f) - (context.Climate.Aridity01 * 0.015f);

            return new EnvironmentVegetationAreaState(
                vegetation.AreaId,
                vegetation.VegetationKind,
                vegetation.Density01 + densityDelta,
                vegetation.GrowthPotential01,
                vegetation.Health01 + healthDelta,
                vegetation.FertilityInfluence01,
                vegetation.ClimateInfluence01);
        }

        private static EnvironmentWaterDepthLevel ResolveDepthFromLevel(
            EnvironmentWaterDepthLevel fallback,
            float waterLevel01)
        {
            float level = EnvironmentMath.Clamp01(waterLevel01);

            if (level <= 0f)
                return EnvironmentWaterDepthLevel.None;

            if (level < 0.30f)
                return EnvironmentWaterDepthLevel.Shallow;

            if (level < 0.60f)
                return EnvironmentWaterDepthLevel.Ford;

            if (level < 0.85f)
                return EnvironmentWaterDepthLevel.Deep;

            if (fallback == EnvironmentWaterDepthLevel.None)
                return EnvironmentWaterDepthLevel.Deep;

            return EnvironmentWaterDepthLevel.VeryDeep;
        }
    }
}
