namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentFoundationBuildReport
    // =============================================================================
    /// <summary>
    /// <para>
    /// Report compatto della costruzione di uno stato ambientale da configurazione.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bootstrap osservabile senza eccezioni rumorose</b></para>
    /// <para>
    /// Il builder della foundation deve poter segnalare dati scartati o assenti
    /// senza assumere che esista gia' una console Unity, un logger globale o un
    /// sistema eventi. Il report resta un value type leggibile da test e tooling.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaDefinitionsApplied</b>: definizioni area accettate.</item>
    ///   <item><b>FertilityAreasApplied</b>: payload fertilita' accettati.</item>
    ///   <item><b>WaterAreasApplied</b>: payload acqua accettati.</item>
    ///   <item><b>VegetationAreasApplied</b>: payload vegetazione accettati.</item>
    ///   <item><b>SeedBankAreasApplied</b>: payload seed bank accettati.</item>
    ///   <item><b>RejectedEntries</b>: entry scartate per id o dati non validi.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentFoundationBuildReport
    {
        public readonly int AreaDefinitionsApplied;
        public readonly int FertilityAreasApplied;
        public readonly int WaterAreasApplied;
        public readonly int VegetationAreasApplied;
        public readonly int SeedBankAreasApplied;
        public readonly int RejectedEntries;

        public bool HasRejectedEntries => RejectedEntries > 0;

        // =============================================================================
        // EnvironmentFoundationBuildReport
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un report di applicazione configurazione.
        /// </para>
        /// </summary>
        public EnvironmentFoundationBuildReport(
            int areaDefinitionsApplied,
            int fertilityAreasApplied,
            int waterAreasApplied,
            int vegetationAreasApplied,
            int seedBankAreasApplied,
            int rejectedEntries)
        {
            AreaDefinitionsApplied = areaDefinitionsApplied < 0 ? 0 : areaDefinitionsApplied;
            FertilityAreasApplied = fertilityAreasApplied < 0 ? 0 : fertilityAreasApplied;
            WaterAreasApplied = waterAreasApplied < 0 ? 0 : waterAreasApplied;
            VegetationAreasApplied = vegetationAreasApplied < 0 ? 0 : vegetationAreasApplied;
            SeedBankAreasApplied = seedBankAreasApplied < 0 ? 0 : seedBankAreasApplied;
            RejectedEntries = rejectedEntries < 0 ? 0 : rejectedEntries;
        }
    }

    // =============================================================================
    // EnvironmentFoundationBuildResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato completo del builder della foundation ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato e diagnostica viaggiano separati</b></para>
    /// <para>
    /// Il chiamante riceve lo stato passivo pronto all'uso e un report tecnico sulla
    /// configurazione applicata. Nessuna diagnostica viene scritta globalmente e
    /// nessun sistema runtime viene notificato dal builder.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>State</b>: contenitore ambientale popolato.</item>
    ///   <item><b>Report</b>: conteggio entry accettate o scartate.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentFoundationBuildResult
    {
        public EnvironmentState State { get; }
        public EnvironmentFoundationBuildReport Report { get; }

        // =============================================================================
        // EnvironmentFoundationBuildResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato aggregando stato e report.
        /// </para>
        /// </summary>
        public EnvironmentFoundationBuildResult(
            EnvironmentState state,
            EnvironmentFoundationBuildReport report)
        {
            State = state ?? new EnvironmentState();
            Report = report;
        }
    }

    // =============================================================================
    // EnvironmentFoundationBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder data-only della Environment Foundation.
    /// </para>
    ///
    /// <para><b>Principio architetturale: composizione Core prima del runtime</b></para>
    /// <para>
    /// Questo builder collega config, resolver e stato passivo senza diventare un
    /// sistema. Non possiede loop, non salva file, non cerca asset e non si collega
    /// al renderer. Il suo compito e' produrre uno stato coerente a partire da dati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildState</b>: crea uno stato completo da config e tick.</item>
    ///   <item><b>BuildSnapshot</b>: crea direttamente uno snapshot read-only.</item>
    ///   <item><b>ApplyAreaSet</b>: applica definizioni e payload area.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentFoundationBuilder
    {
        // =============================================================================
        // BuildState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno stato ambientale passivo da configurazioni opzionali.
        /// </para>
        /// </summary>
        public static EnvironmentFoundationBuildResult BuildState(
            long elapsedEnvironmentTicks,
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig,
            EnvironmentAreaSetConfig areaSetConfig)
        {
            var safeCalendarConfig = calendarConfig ?? new EnvironmentCalendarConfig();
            var safeClimateConfig = climateConfig ?? new EnvironmentClimateConfig();
            var state = new EnvironmentState();

            // Calendario e clima vengono risolti prima delle aree per dare ai futuri
            // sistemi un ordine di bootstrap stabile e leggibile.
            var calendar = EnvironmentCalendarResolver.Resolve(
                elapsedEnvironmentTicks,
                safeCalendarConfig);
            var climate = EnvironmentClimateResolver.Resolve(
                calendar,
                safeClimateConfig,
                safeCalendarConfig);

            state.SetCalendar(calendar);
            state.SetClimate(climate);

            var report = ApplyAreaSet(state, areaSetConfig);
            return new EnvironmentFoundationBuildResult(state, report);
        }

        // =============================================================================
        // BuildSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce direttamente uno snapshot read-only da configurazione.
        /// </para>
        /// </summary>
        public static EnvironmentSnapshot BuildSnapshot(
            long elapsedEnvironmentTicks,
            EnvironmentCalendarConfig calendarConfig,
            EnvironmentClimateConfig climateConfig,
            EnvironmentAreaSetConfig areaSetConfig)
        {
            // Lo snapshot e' derivato dallo stato appena costruito, non da una seconda
            // pipeline parallela. In questo modo i contratti restano unificati.
            var result = BuildState(
                elapsedEnvironmentTicks,
                calendarConfig,
                climateConfig,
                areaSetConfig);
            return result.State.CreateSnapshot();
        }

        // =============================================================================
        // ApplyAreaSet
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica a uno stato esistente il set di aree configurate.
        /// </para>
        /// </summary>
        public static EnvironmentFoundationBuildReport ApplyAreaSet(
            EnvironmentState state,
            EnvironmentAreaSetConfig areaSetConfig)
        {
            if (state == null)
                return new EnvironmentFoundationBuildReport(0, 0, 0, 0, 0, 1);

            var safeAreaSet = areaSetConfig ?? new EnvironmentAreaSetConfig();
            int areasApplied = 0;
            int fertilityApplied = 0;
            int waterApplied = 0;
            int vegetationApplied = 0;
            int seedBankApplied = 0;
            int rejected = 0;

            var areaConfigs = safeAreaSet.areas ?? new EnvironmentAreaConfig[0];
            for (int i = 0; i < areaConfigs.Length; i++)
            {
                if (areaConfigs[i] == null)
                {
                    rejected++;
                    continue;
                }

                // Le definizioni senza id valido o con bounds invalidi non entrano nel
                // registry: evitare aree fantasma e' piu' utile di correggerle qui.
                var definition = areaConfigs[i].ToDefinition();
                if (!definition.AreaId.IsValid || !definition.Bounds.IsValid)
                {
                    rejected++;
                    continue;
                }

                if (state.SetAreaDefinition(definition))
                    areasApplied++;
                else
                    rejected++;
            }

            var fertilityConfigs = safeAreaSet.fertilityAreas ?? new EnvironmentFertilityAreaConfig[0];
            for (int i = 0; i < fertilityConfigs.Length; i++)
            {
                if (fertilityConfigs[i] == null)
                {
                    rejected++;
                    continue;
                }

                if (state.SetFertilityArea(fertilityConfigs[i].ToState()))
                    fertilityApplied++;
                else
                    rejected++;
            }

            var waterConfigs = safeAreaSet.waterAreas ?? new EnvironmentWaterAreaConfig[0];
            for (int i = 0; i < waterConfigs.Length; i++)
            {
                if (waterConfigs[i] == null)
                {
                    rejected++;
                    continue;
                }

                if (state.SetWaterArea(waterConfigs[i].ToState()))
                    waterApplied++;
                else
                    rejected++;
            }

            var vegetationConfigs = safeAreaSet.vegetationAreas ?? new EnvironmentVegetationAreaConfig[0];
            for (int i = 0; i < vegetationConfigs.Length; i++)
            {
                if (vegetationConfigs[i] == null)
                {
                    rejected++;
                    continue;
                }

                if (state.SetVegetationArea(vegetationConfigs[i].ToState()))
                    vegetationApplied++;
                else
                    rejected++;
            }

            var seedBankConfigs = safeAreaSet.seedBankAreas ?? new EnvironmentSeedBankAreaConfig[0];
            for (int i = 0; i < seedBankConfigs.Length; i++)
            {
                if (seedBankConfigs[i] == null)
                {
                    rejected++;
                    continue;
                }

                if (state.SetSeedBankArea(seedBankConfigs[i].ToState()))
                    seedBankApplied++;
                else
                    rejected++;
            }

            return new EnvironmentFoundationBuildReport(
                areasApplied,
                fertilityApplied,
                waterApplied,
                vegetationApplied,
                seedBankApplied,
                rejected);
        }
    }
}
