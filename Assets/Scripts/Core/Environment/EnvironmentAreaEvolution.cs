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
    ///   <item><b>BiomeProfile</b>: profilo biome che definisce target e resistenze.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAreaEvolutionContext
    {
        public readonly EnvironmentCalendarState Calendar;
        public readonly EnvironmentGlobalClimateState Climate;
        public readonly EnvironmentSeasonProfile SeasonProfile;
        public readonly EnvironmentTemporalTransition Transition;
        public readonly EnvironmentBiomeProfile BiomeProfile;

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
            : this(
                calendar,
                climate,
                seasonProfile,
                transition,
                EnvironmentBiomeProfile.Default)
        {
        }

        // =============================================================================
        // EnvironmentAreaEvolutionContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il contesto includendo un profilo biome esplicito.
        /// </para>
        /// </summary>
        public EnvironmentAreaEvolutionContext(
            EnvironmentCalendarState calendar,
            EnvironmentGlobalClimateState climate,
            EnvironmentSeasonProfile seasonProfile,
            EnvironmentTemporalTransition transition,
            EnvironmentBiomeProfile biomeProfile)
        {
            Calendar = calendar;
            Climate = climate;
            SeasonProfile = seasonProfile;
            Transition = transition;
            BiomeProfile = biomeProfile.IsValid
                ? biomeProfile
                : EnvironmentBiomeProfile.Default;
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

            // L'acqua non deve inseguire direttamente il meteo del giorno. Un fiume
            // o lago stabile assorbe precipitazioni e aridita' con molta inerzia,
            // mentre un bacino stagionale puo' oscillare di piu' senza comunque
            // rimbalzare continuamente tra zero e uno.
            float weatherMoisture = (context.Climate.Weather.Precipitation01 * 0.48f)
                                    + (context.Climate.Humidity01 * 0.30f)
                                    + (context.BiomeProfile.BaseMoisture01 * 0.22f);
            float evaporationPressure = (context.Climate.Aridity01 * 0.30f)
                                        + (context.Climate.Temperature01 * 0.12f);
            float climateTarget = EnvironmentMath.Clamp01(weatherMoisture - evaporationPressure);
            float structuralTarget = water.IsSeasonal
                ? climateTarget
                : EnvironmentMath.Clamp01((water.WaterLevel01 * 0.86f) + (climateTarget * 0.14f));
            float approachRate = water.IsSeasonal ? 0.070f : 0.022f;
            float rainImpulse = context.Climate.Weather.Precipitation01 * (water.IsSeasonal ? 0.012f : 0.004f);
            float aridityImpulse = context.Climate.Aridity01 * (water.IsSeasonal ? 0.010f : 0.003f);
            float nextLevel = Approach01(water.WaterLevel01, structuralTarget, approachRate)
                              + rainImpulse
                              - aridityImpulse;

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

            var biome = context.BiomeProfile;
            float fertilitySupport = ResolveFertilitySuitability(fertility, vegetation, biome);
            float moistureSupport = ResolveMoistureSuitability(water, context, biome);
            float temperatureSupport = ResolveTemperatureSuitability(context, biome);
            float seasonSupport = ResolveSeasonSuitability(context, biome);
            float droughtStress = context.Climate.Aridity01 * (1f - biome.DroughtResistance01);
            float exhaustionStress = fertility.Exhaustion01 * 0.35f;
            float stress = EnvironmentMath.Clamp01(
                (droughtStress + exhaustionStress)
                * biome.DisturbanceSensitivity01);
            float ecologicalSupport = EnvironmentMath.Clamp01(
                (fertilitySupport * 0.32f)
                + (moistureSupport * 0.30f)
                + (temperatureSupport * 0.18f)
                + (seasonSupport * 0.20f));
            float targetDensity = EnvironmentMath.Clamp01(
                biome.TargetVegetationDensity01
                * ecologicalSupport
                * (1f - (stress * 0.65f)));
            float targetHealth = EnvironmentMath.Clamp01(
                (biome.TargetVegetationHealth01 * 0.50f)
                + (ecologicalSupport * 0.45f)
                - (stress * 0.35f));
            float densityRate = 0.010f
                                + (biome.NaturalRecoveryRate01 * 0.040f)
                                + (vegetation.GrowthPotential01 * 0.020f);
            float healthRate = 0.014f
                               + (biome.NaturalRecoveryRate01 * 0.050f);
            float nextDensity = Approach01(
                vegetation.Density01,
                targetDensity,
                densityRate);
            float nextHealth = Approach01(
                vegetation.Health01,
                targetHealth,
                healthRate);

            // Solo stress molto alto e persistente deve distruggere davvero una area.
            // Negli altri casi la vegetazione tende al target del biome invece di
            // scivolare verso zero per somma di delta giornalieri.
            if (stress > 0.85f && ecologicalSupport < 0.20f)
            {
                nextDensity -= (stress - 0.85f) * 0.020f;
                nextHealth -= (stress - 0.85f) * 0.030f;
            }

            return new EnvironmentVegetationAreaState(
                vegetation.AreaId,
                vegetation.VegetationKind,
                nextDensity,
                vegetation.GrowthPotential01,
                nextHealth,
                vegetation.FertilityInfluence01,
                vegetation.ClimateInfluence01);
        }

        private static float ResolveFertilitySuitability(
            EnvironmentFertilityAreaState fertility,
            EnvironmentVegetationAreaState vegetation,
            EnvironmentBiomeProfile biome)
        {
            float local = fertility.CurrentFertility01 * vegetation.FertilityInfluence01;
            float target = biome.TargetFertility01 <= 0f ? 0.01f : biome.TargetFertility01;
            return EnvironmentMath.Clamp01(local / target);
        }

        private static float ResolveMoistureSuitability(
            EnvironmentWaterAreaState water,
            EnvironmentAreaEvolutionContext context,
            EnvironmentBiomeProfile biome)
        {
            float available = (water.WaterLevel01 * 0.25f)
                              + (context.Climate.Humidity01 * 0.40f)
                              + (context.Climate.Weather.Precipitation01 * 0.20f)
                              + (biome.BaseMoisture01 * 0.15f);
            float droughtPenalty = context.Climate.Aridity01 * (1f - biome.DroughtResistance01) * 0.50f;
            return EnvironmentMath.Clamp01(available - droughtPenalty);
        }

        private static float ResolveTemperatureSuitability(
            EnvironmentAreaEvolutionContext context,
            EnvironmentBiomeProfile biome)
        {
            float coldStress = EnvironmentMath.Clamp01((0.45f - context.Climate.Temperature01) * 2f)
                               * (1f - biome.ColdResistance01);
            float heatStress = EnvironmentMath.Clamp01((context.Climate.Temperature01 - 0.55f) * 2f)
                               * (1f - biome.HeatResistance01);
            return EnvironmentMath.Clamp01(1f - coldStress - heatStress);
        }

        private static float ResolveSeasonSuitability(
            EnvironmentAreaEvolutionContext context,
            EnvironmentBiomeProfile biome)
        {
            float seasonBias = context.SeasonProfile.VegetationGrowthBias01;
            return EnvironmentMath.Clamp01(
                (1f - biome.Seasonality01)
                + (seasonBias * biome.Seasonality01));
        }

        private static float Approach01(float current, float target, float rate)
        {
            float safeCurrent = EnvironmentMath.Clamp01(current);
            float safeTarget = EnvironmentMath.Clamp01(target);
            float safeRate = EnvironmentMath.Clamp01(rate);
            return EnvironmentMath.Clamp01(
                safeCurrent + ((safeTarget - safeCurrent) * safeRate));
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
