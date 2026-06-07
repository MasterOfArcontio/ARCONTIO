namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVisualProbeFrame
    // =============================================================================
    /// <summary>
    /// <para>
    /// Pacchetto dati completo per il primo probe visuale ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: frame di prova, non renderer</b></para>
    /// <para>
    /// Il frame raccoglie l'output dei builder gia' disponibili e lo rende
    /// consumabile da un futuro disegnatore debug. Non contiene riferimenti a scena,
    /// camera, materiali o asset. Il suo scopo e' dare al test visivo una forma
    /// unica: prima terrain, poi acqua, vegetazione, actor/object e infine luce.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TerrainChunks</b>: mesh data terrain gia' costruiti.</item>
    ///   <item><b>ActorObjectQueue</b>: queue actor/object gia' ordinata.</item>
    ///   <item><b>VegetationItems</b>: item vegetazione visibili.</item>
    ///   <item><b>WaterItems</b>: item acqua visibili.</item>
    ///   <item><b>LightItems</b>: item luce visibili.</item>
    ///   <item><b>ComparisonDiagnostics</b>: gate di sicurezza ArcGraph/MapGrid.</item>
    ///   <item><b>Diagnostics</b>: contatori aggregati del frame.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphVisualProbeFrame
    {
        public readonly ArcGraphTerrainChunkMeshData[] TerrainChunks;
        public readonly ArcGraphRenderQueue ActorObjectQueue;
        public readonly ArcGraphVegetationRenderItem[] VegetationItems;
        public readonly ArcGraphWaterRenderItem[] WaterItems;
        public readonly ArcGraphLightRenderItem[] LightItems;
        public readonly ArcGraphComparisonDiagnostics ComparisonDiagnostics;
        public readonly ArcGraphVisualProbeDiagnostics Diagnostics;

        // =============================================================================
        // ArcGraphVisualProbeFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame di probe visuale a partire da dati gia' prodotti.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// Array null diventano array vuoti. La queue actor/object null viene
        /// sostituita da una queue vuota. Questo permette al chiamante di ispezionare
        /// sempre il frame senza controlli null ripetuti.
        /// </para>
        /// </summary>
        public ArcGraphVisualProbeFrame(
            ArcGraphTerrainChunkMeshData[] terrainChunks,
            ArcGraphRenderQueue actorObjectQueue,
            ArcGraphVegetationRenderItem[] vegetationItems,
            ArcGraphWaterRenderItem[] waterItems,
            ArcGraphLightRenderItem[] lightItems,
            ArcGraphComparisonDiagnostics comparisonDiagnostics,
            ArcGraphVisualProbeDiagnostics diagnostics)
        {
            TerrainChunks = terrainChunks ?? EmptyTerrainChunks();
            ActorObjectQueue = actorObjectQueue ?? new ArcGraphRenderQueue();
            VegetationItems = vegetationItems ?? EmptyVegetationItems();
            WaterItems = waterItems ?? EmptyWaterItems();
            LightItems = lightItems ?? EmptyLightItems();
            ComparisonDiagnostics = comparisonDiagnostics;
            Diagnostics = diagnostics;
        }

        private static ArcGraphTerrainChunkMeshData[] EmptyTerrainChunks()
        {
            return new ArcGraphTerrainChunkMeshData[0];
        }

        private static ArcGraphVegetationRenderItem[] EmptyVegetationItems()
        {
            return new ArcGraphVegetationRenderItem[0];
        }

        private static ArcGraphWaterRenderItem[] EmptyWaterItems()
        {
            return new ArcGraphWaterRenderItem[0];
        }

        private static ArcGraphLightRenderItem[] EmptyLightItems()
        {
            return new ArcGraphLightRenderItem[0];
        }
    }
}
