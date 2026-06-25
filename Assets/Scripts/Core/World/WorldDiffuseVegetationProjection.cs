using Arcontio.Core.Environment;

namespace Arcontio.Core
{
    // =============================================================================
    // WorldDiffuseVegetationProjection
    // =============================================================================
    /// <summary>
    /// <para>
    /// Proiezione World-side minima della vegetazione diffusa prodotta dalla
    /// biosfera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: vegetazione non fisica e non cognitiva</b></para>
    /// <para>
    /// Questa struttura non rappresenta una pianta-oggetto, non blocca movimento,
    /// non blocca visione e non deve entrare nella memoria puntuale degli NPC. Serve
    /// solo a conservare, nel World, il contratto cell-based che un renderer o un
    /// debug adapter potranno leggere senza interrogare direttamente la biosfera.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area biologica sorgente.</item>
    ///   <item><b>Cell</b>: coordinata cella della vegetazione diffusa.</item>
    ///   <item><b>VegetationKind</b>: categoria semantica, non sprite.</item>
    ///   <item><b>CoverageBand</b>: banda discreta di copertura.</item>
    ///   <item><b>ConditionBand</b>: banda discreta di salute/condizione.</item>
    /// </list>
    /// </summary>
    public readonly struct WorldDiffuseVegetationProjection
    {
        public readonly EnvironmentAreaId AreaId;
        public readonly EnvironmentCellCoord Cell;
        public readonly EnvironmentVegetationKind VegetationKind;
        public readonly EnvironmentVegetationCoverageBand CoverageBand;
        public readonly EnvironmentVegetationConditionBand ConditionBand;

        public WorldDiffuseVegetationProjection(
            EnvironmentAreaId areaId,
            EnvironmentCellCoord cell,
            EnvironmentVegetationKind vegetationKind,
            EnvironmentVegetationCoverageBand coverageBand,
            EnvironmentVegetationConditionBand conditionBand)
        {
            AreaId = areaId;
            Cell = cell;
            VegetationKind = vegetationKind;
            CoverageBand = coverageBand;
            ConditionBand = conditionBand;
        }
    }
}
