namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVisualProbeDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica compatta del primo frame di probe visuale ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test visivo preparabile senza renderer produttivo</b></para>
    /// <para>
    /// La diagnostica aggrega i contatori minimi dei layer gia' pronti: terrain,
    /// actor/object, vegetazione, acqua e luce. Non avvia la scena, non carica asset
    /// e non sostituisce MapGrid. Serve a capire se esiste abbastanza materiale
    /// coerente per passare al primo test visivo controllato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CanBuildVisualProbe</b>: true se il frame contiene dati minimi utili.</item>
    ///   <item><b>Reason</b>: esito sintetico leggibile.</item>
    ///   <item><b>TerrainChunkCount</b>: chunk terrain prodotti.</item>
    ///   <item><b>TerrainCellCount</b>: celle terrain trasformate in quad.</item>
    ///   <item><b>ActorObjectEntryCount</b>: entry actor/object ordinate.</item>
    ///   <item><b>VegetationItemCount</b>: item vegetazione visibili.</item>
    ///   <item><b>WaterItemCount</b>: item acqua visibili.</item>
    ///   <item><b>LightItemCount</b>: item luce visibili.</item>
    ///   <item><b>RequiresSceneRenderer</b>: segnala che manca ancora il disegnatore Unity concreto.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphVisualProbeDiagnostics
    {
        public readonly bool CanBuildVisualProbe;
        public readonly string Reason;
        public readonly int TerrainChunkCount;
        public readonly int TerrainCellCount;
        public readonly int ActorObjectEntryCount;
        public readonly int VegetationItemCount;
        public readonly int WaterItemCount;
        public readonly int LightItemCount;
        public readonly bool RequiresSceneRenderer;

        public ArcGraphVisualProbeDiagnostics(
            bool canBuildVisualProbe,
            string reason,
            int terrainChunkCount,
            int terrainCellCount,
            int actorObjectEntryCount,
            int vegetationItemCount,
            int waterItemCount,
            int lightItemCount,
            bool requiresSceneRenderer)
        {
            CanBuildVisualProbe = canBuildVisualProbe;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            TerrainChunkCount = terrainChunkCount < 0 ? 0 : terrainChunkCount;
            TerrainCellCount = terrainCellCount < 0 ? 0 : terrainCellCount;
            ActorObjectEntryCount = actorObjectEntryCount < 0 ? 0 : actorObjectEntryCount;
            VegetationItemCount = vegetationItemCount < 0 ? 0 : vegetationItemCount;
            WaterItemCount = waterItemCount < 0 ? 0 : waterItemCount;
            LightItemCount = lightItemCount < 0 ? 0 : lightItemCount;
            RequiresSceneRenderer = requiresSceneRenderer;
        }
    }
}
