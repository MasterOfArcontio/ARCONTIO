namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentVegetationCellPlacement
    // =============================================================================
    /// <summary>
    /// <para>
    /// Cella scelta dalla biosfera per vegetazione decorativa diffusa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: vegetazione visiva senza entita' fisica</b></para>
    /// <para>
    /// Questo record non rappresenta una pianta-oggetto e non deve entrare nella
    /// memoria degli NPC come risorsa puntuale. Serve a conservare nello stato
    /// biosfera quali celle possono produrre vegetazione visuale derivata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area biologica sorgente.</item>
    ///   <item><b>Cell</b>: coordinata candidata.</item>
    ///   <item><b>VegetationKind</b>: categoria vegetale astratta.</item>
    ///   <item><b>Density01/Health01</b>: stato medio locale derivato dall'area.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentVegetationCellPlacement
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentCellCoord Cell;
        public readonly EnvironmentVegetationKind VegetationKind;
        public readonly float Density01;
        public readonly float Health01;

        public EnvironmentVegetationCellPlacement(
            EnvironmentAreaId areaId,
            EnvironmentCellCoord cell,
            EnvironmentVegetationKind vegetationKind,
            float density01,
            float health01)
        {
            AreaId = areaId;
            Cell = cell;
            VegetationKind = vegetationKind;
            Density01 = EnvironmentMath.Clamp01(density01);
            Health01 = EnvironmentMath.Clamp01(health01);
        }
    }

    // =============================================================================
    // EnvironmentPhysicalPlantPlacement
    // =============================================================================
    /// <summary>
    /// <para>
    /// Cella scelta dalla biosfera per una pianta fisica importante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato biologico separato dalla proiezione fisica</b></para>
    /// <para>
    /// Il record rimanda alla <see cref="EnvironmentPlantInstance"/> tramite
    /// <see cref="PlantId"/>. La biosfera conserva specie, eta', salute e stadio
    /// nella plant instance; un futuro boundary World potra' creare la proiezione
    /// fisica minima che blocca vista/movimento senza duplicare lo stato biologico.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>PlantId</b>: id della pianta nello stato biosfera.</item>
    ///   <item><b>AreaId</b>: area biologica sorgente.</item>
    ///   <item><b>Cell</b>: cella fisica candidata.</item>
    ///   <item><b>SpeciesKey</b>: specie/catalog key biologica, non sprite.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPhysicalPlantPlacement
    {
        public readonly EnvironmentPlantId PlantId;
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentCellCoord Cell;
        public readonly string SpeciesKey;

        public EnvironmentPhysicalPlantPlacement(
            EnvironmentPlantId plantId,
            EnvironmentAreaId areaId,
            EnvironmentCellCoord cell,
            string speciesKey)
        {
            PlantId = plantId;
            AreaId = areaId;
            Cell = cell;
            SpeciesKey = speciesKey ?? string.Empty;
        }
    }
}
