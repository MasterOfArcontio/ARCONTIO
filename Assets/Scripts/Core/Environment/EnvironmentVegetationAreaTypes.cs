namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentVegetationKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Categoria astratta di vegetazione diffusa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: vegetazione diffusa separata da PlantInstance</b></para>
    /// <para>
    /// Erba e sottobosco non devono diventare migliaia di entita'. La categoria
    /// descrive il carattere dominante dell'area; le piante importanti verranno
    /// modellate da istanze dedicate in step successivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna vegetazione rilevante.</item>
    ///   <item><b>Grass</b>: erba o prato.</item>
    ///   <item><b>Underbrush</b>: sottobosco.</item>
    ///   <item><b>Shrubland</b>: arbusti diffusi.</item>
    ///   <item><b>Forest</b>: copertura arborea diffusa.</item>
    ///   <item><b>Cultivated</b>: vegetazione coltivata futura.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentVegetationKind
    {
        None = 0,
        Grass = 10,
        Underbrush = 20,
        Shrubland = 30,
        Forest = 40,
        Cultivated = 50
    }

    // =============================================================================
    // EnvironmentSeedBankEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Quantita' astratta di semi disponibili per una specie o categoria.
    /// </para>
    ///
    /// <para><b>Principio architetturale: semi naturali astratti</b></para>
    /// <para>
    /// La natura selvatica non deve creare oggetti seme fisici per ogni pianta. La
    /// seed bank e' una pressione ecologica dell'area. I semi agricoli concreti
    /// potranno diventare risorse o oggetti in una fase separata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SpeciesKey</b>: chiave specie o categoria.</item>
    ///   <item><b>Amount01</b>: disponibilita' normalizzata.</item>
    ///   <item><b>Viability01</b>: vitalita' media dei semi.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentSeedBankEntry
    {
        public readonly string SpeciesKey;
        public readonly float Amount01;
        public readonly float Viability01;

        public EnvironmentSeedBankEntry(string speciesKey, float amount01, float viability01)
        {
            SpeciesKey = speciesKey ?? string.Empty;
            Amount01 = EnvironmentMath.Clamp01(amount01);
            Viability01 = EnvironmentMath.Clamp01(viability01);
        }
    }

    // =============================================================================
    // EnvironmentVegetationAreaState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload ambientale di un'area di vegetazione diffusa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: area vegetale come pressione ecologica</b></para>
    /// <para>
    /// L'area conserva densita', crescita potenziale e seed bank astratta. Non fa
    /// nascere piante da sola e non modifica oggetti o NPC: un sistema futuro usera'
    /// questi dati con una cadenza giornaliera.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area vegetale.</item>
    ///   <item><b>VegetationKind</b>: categoria dominante.</item>
    ///   <item><b>Density01</b>: densita' diffusa.</item>
    ///   <item><b>GrowthPotential01</b>: potenziale di crescita.</item>
    ///   <item><b>Health01</b>: salute media dell'area.</item>
    ///   <item><b>FertilityInfluence01</b>: influenza fertilita'.</item>
    ///   <item><b>ClimateInfluence01</b>: influenza clima globale.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentVegetationAreaState
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentVegetationKind VegetationKind;
        public readonly float Density01;
        public readonly float GrowthPotential01;
        public readonly float Health01;
        public readonly float FertilityInfluence01;
        public readonly float ClimateInfluence01;

        public EnvironmentVegetationAreaState(
            EnvironmentAreaId areaId,
            EnvironmentVegetationKind vegetationKind,
            float density01,
            float growthPotential01,
            float health01,
            float fertilityInfluence01,
            float climateInfluence01)
        {
            AreaId = areaId;
            VegetationKind = vegetationKind;
            Density01 = EnvironmentMath.Clamp01(density01);
            GrowthPotential01 = EnvironmentMath.Clamp01(growthPotential01);
            Health01 = EnvironmentMath.Clamp01(health01);
            FertilityInfluence01 = EnvironmentMath.Clamp01(fertilityInfluence01);
            ClimateInfluence01 = EnvironmentMath.Clamp01(climateInfluence01);
        }
    }
}
