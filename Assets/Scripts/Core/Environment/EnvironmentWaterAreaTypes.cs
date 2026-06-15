namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentWaterKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo ambientale di acqua.
    /// </para>
    ///
    /// <para><b>Principio architetturale: acqua come layer ambientale</b></para>
    /// <para>
    /// L'acqua non deve essere modellata come oggetto standard. Questo enum prepara
    /// un layer acqua stabile che potra' essere letto da movimento, vegetazione e
    /// rendering tramite snapshot futuri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Still</b>: acqua ferma generica.</item>
    ///   <item><b>River</b>: acqua corrente astratta.</item>
    ///   <item><b>Lake</b>: bacino stabile.</item>
    ///   <item><b>Puddle</b>: acqua bassa temporanea.</item>
    ///   <item><b>Sea</b>: massa d'acqua ampia futura.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentWaterKind
    {
        Still = 0,
        River = 10,
        Lake = 20,
        Puddle = 30,
        Sea = 40
    }

    // =============================================================================
    // EnvironmentWaterDepthLevel
    // =============================================================================
    /// <summary>
    /// <para>
    /// Profondita' discreta dell'acqua.
    /// </para>
    ///
    /// <para><b>Principio architetturale: niente fluidodinamica precoce</b></para>
    /// <para>
    /// La prima biosfera usa livelli discreti per restare leggibile e poco costosa.
    /// Eventuali sistemi futuri potranno aggiornare questi livelli raramente o per
    /// evento, senza introdurre simulazione continua del fluido.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: niente acqua.</item>
    ///   <item><b>Shallow</b>: basso o pozzanghera.</item>
    ///   <item><b>Ford</b>: guado o medio.</item>
    ///   <item><b>Deep</b>: profondo.</item>
    ///   <item><b>VeryDeep</b>: molto profondo o pericoloso.</item>
    /// </list>
    /// </summary>
    public enum EnvironmentWaterDepthLevel
    {
        None = 0,
        Shallow = 1,
        Ford = 2,
        Deep = 3,
        VeryDeep = 4
    }

    // =============================================================================
    // EnvironmentWaterAreaState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload ambientale di un'area acqua.
    /// </para>
    ///
    /// <para><b>Principio architetturale: acqua stabile prima del flusso</b></para>
    /// <para>
    /// Questa struttura descrive un corpo o tratto d'acqua come dato ambientale. Non
    /// propaga pressione, non cerca vicini e non aggiorna pathfinding.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area acqua.</item>
    ///   <item><b>WaterKind</b>: tipo di acqua.</item>
    ///   <item><b>DepthLevel</b>: profondita' discreta media o dominante.</item>
    ///   <item><b>WaterLevel01</b>: livello normalizzato.</item>
    ///   <item><b>FlowIntensity01</b>: intensita' astratta di flusso.</item>
    ///   <item><b>IsDrinkable</b>: potabilita' futura.</item>
    ///   <item><b>IsSeasonal</b>: acqua stagionale o temporanea.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentWaterAreaState
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentWaterKind WaterKind;
        public readonly EnvironmentWaterDepthLevel DepthLevel;
        public readonly float WaterLevel01;
        public readonly float FlowIntensity01;
        public readonly bool IsDrinkable;
        public readonly bool IsSeasonal;

        public EnvironmentWaterAreaState(
            EnvironmentAreaId areaId,
            EnvironmentWaterKind waterKind,
            EnvironmentWaterDepthLevel depthLevel,
            float waterLevel01,
            float flowIntensity01,
            bool isDrinkable,
            bool isSeasonal)
        {
            AreaId = areaId;
            WaterKind = waterKind;
            DepthLevel = depthLevel;
            WaterLevel01 = EnvironmentMath.Clamp01(waterLevel01);
            FlowIntensity01 = EnvironmentMath.Clamp01(flowIntensity01);
            IsDrinkable = isDrinkable;
            IsSeasonal = isSeasonal;
        }
    }
}
