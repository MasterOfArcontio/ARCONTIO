namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentSoilKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo astratto di suolo usato dalle aree di fertilita'.
    /// </para>
    ///
    /// <para><b>Principio architetturale: suolo come proprieta' di area</b></para>
    /// <para>
    /// Il suolo non e' un oggetto piazzato nella cella. E' un dato ambientale che
    /// influenza crescita, recupero e agricoltura futura.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Generic</b>: suolo non specializzato.</item>
    ///   <item><b>Grassland</b>: suolo erboso fertile.</item>
    ///   <item><b>Forest</b>: suolo organico/sottobosco.</item>
    ///   <item><b>Rocky</b>: suolo roccioso o povero.</item>
    ///   <item><b>Riverbed</b>: letto o bordo umido di acqua.</item>
    ///   <item><b>Farmland</b>: area coltivata futura.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentSoilKind
    {
        Generic = 0,
        Grassland = 10,
        Forest = 20,
        Rocky = 30,
        Riverbed = 40,
        Farmland = 50
    }

    // =============================================================================
    // EnvironmentFertilityAreaState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload ambientale di un'area fertile.
    /// </para>
    ///
    /// <para><b>Principio architetturale: fertilita' a blocchi</b></para>
    /// <para>
    /// La pagina BIOSFERA stabilisce che la fertilita' non deve diventare un oggetto
    /// o un valore ultra-fine per ogni cella. Questa struttura modella una fertilita'
    /// area-based, leggera e compatibile con update rari.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area a cui appartiene il payload.</item>
    ///   <item><b>SoilKind</b>: tipo suolo astratto.</item>
    ///   <item><b>BaseFertility01</b>: fertilita' naturale.</item>
    ///   <item><b>CurrentFertility01</b>: fertilita' runtime corrente.</item>
    ///   <item><b>GrowthModifier01</b>: supporto alla crescita vegetale.</item>
    ///   <item><b>Exhaustion01</b>: esaurimento futuro da coltivazione o uso.</item>
    ///   <item><b>Recovery01</b>: capacita' di recupero naturale.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentFertilityAreaState
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentSoilKind SoilKind;
        public readonly float BaseFertility01;
        public readonly float CurrentFertility01;
        public readonly float GrowthModifier01;
        public readonly float Exhaustion01;
        public readonly float Recovery01;

        public EnvironmentFertilityAreaState(
            EnvironmentAreaId areaId,
            EnvironmentSoilKind soilKind,
            float baseFertility01,
            float currentFertility01,
            float growthModifier01,
            float exhaustion01,
            float recovery01)
        {
            AreaId = areaId;
            SoilKind = soilKind;
            BaseFertility01 = EnvironmentMath.Clamp01(baseFertility01);
            CurrentFertility01 = EnvironmentMath.Clamp01(currentFertility01);
            GrowthModifier01 = EnvironmentMath.Clamp01(growthModifier01);
            Exhaustion01 = EnvironmentMath.Clamp01(exhaustion01);
            Recovery01 = EnvironmentMath.Clamp01(recovery01);
        }
    }
}
