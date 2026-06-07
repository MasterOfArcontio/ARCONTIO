using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVisualProbeBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo del frame usato dal primo probe visuale ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: composizione debug senza authority di scena</b></para>
    /// <para>
    /// Il builder orchestra i builder gia' esistenti per terrain, actor/object,
    /// vegetazione, acqua e luce. Non legge il mondo, non carica asset e non monta
    /// elementi nella scena. Produce un frame dati che un futuro componente debug
    /// potra' disegnare solo dopo aver superato il gate comparativo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: compone un frame completo da layer ArcGraph.</item>
    ///   <item><b>BuildTerrainChunks</b>: raccoglie mesh data terrain dirty.</item>
    ///   <item><b>CreateDiagnostics</b>: aggrega contatori e readiness.</item>
    ///   <item><b>CopyToArray</b>: congela liste ambientali in array leggibili.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphVisualProbeBuilder
    {
        private readonly ArcGraphTerrainChunkMeshBuilder _terrainBuilder = new();
        private readonly ArcGraphRenderQueueBuilder _actorObjectBuilder = new();
        private readonly ArcGraphVegetationRenderQueueBuilder _vegetationBuilder = new();
        private readonly ArcGraphWaterRenderQueueBuilder _waterBuilder = new();
        private readonly ArcGraphLightRenderQueueBuilder _lightBuilder = new();

        private readonly List<ArcGraphTerrainChunkMeshData> _terrainChunks = new();
        private readonly List<ArcGraphVegetationRenderItem> _vegetationItems = new();
        private readonly List<ArcGraphWaterRenderItem> _waterItems = new();
        private readonly List<ArcGraphLightRenderItem> _lightItems = new();

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame dati per il probe visuale.
        /// </para>
        ///
        /// <para><b>Prerequisiti dichiarati</b></para>
        /// <para>
        /// I booleani finali descrivono cio' che il chiamante sa della scena o del
        /// test esterno: MapGrid presente, camera presente e materiale presente.
        /// Il builder non cerca questi elementi autonomamente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>layer</b>: cache ArcGraph gia' popolate.</item>
        ///   <item><b>uvMap/policy</b>: input terrain gia' preparati.</item>
        ///   <item><b>renderState</b>: dirty state e dimensioni chunk.</item>
        ///   <item><b>lodProfile</b>: livello LOD scelto per il probe.</item>
        ///   <item><b>requestSceneProbe</b>: true se si vuole validare anche aggancio visuale futuro.</item>
        /// </list>
        /// </summary>
        public ArcGraphVisualProbeFrame Build(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphActorLayer actorLayer,
            ArcGraphObjectLayer objectLayer,
            ArcGraphVegetationLayer vegetationLayer,
            ArcGraphWaterLayer waterLayer,
            ArcGraphLightLayer lightLayer,
            ArcGraphTerrainTileUvMap uvMap,
            ArcGraphTerrainVisualPolicy visualPolicy,
            ArcGraphRenderState renderState,
            ArcGraphZoomLodProfile lodProfile,
            bool hasLegacyRenderer,
            bool hasCamera,
            bool hasMaterial,
            bool requestSceneProbe)
        {
            _terrainChunks.Clear();
            _vegetationItems.Clear();
            _waterItems.Clear();
            _lightItems.Clear();

            BuildTerrainChunks(terrainLayer, uvMap, renderState, visualPolicy, _terrainChunks);

            var actorObjectQueue = new ArcGraphRenderQueue();
            _actorObjectBuilder.Build(actorLayer, objectLayer, lodProfile, actorObjectQueue);

            _vegetationBuilder.Build(vegetationLayer, lodProfile, _vegetationItems);
            _waterBuilder.Build(waterLayer, lodProfile, _waterItems);
            _lightBuilder.Build(lightLayer, lodProfile, _lightItems);

            bool hasArcGraphTerrainData = _terrainChunks.Count > 0;
            ArcGraphComparisonOptions options = requestSceneProbe
                ? ArcGraphComparisonOptions.CreateTemporaryDebugSceneProbe()
                : ArcGraphComparisonOptions.CreateDiagnosticsOnly();

            ArcGraphComparisonDiagnostics comparisonDiagnostics = ArcGraphComparisonGate.Evaluate(
                options,
                hasLegacyRenderer,
                hasArcGraphTerrainData,
                hasCamera,
                hasMaterial);

            ArcGraphVisualProbeDiagnostics diagnostics = CreateDiagnostics(
                _terrainChunks,
                actorObjectQueue,
                _vegetationItems,
                _waterItems,
                _lightItems,
                comparisonDiagnostics);

            return new ArcGraphVisualProbeFrame(
                _terrainChunks.ToArray(),
                actorObjectQueue,
                _vegetationItems.ToArray(),
                _waterItems.ToArray(),
                _lightItems.ToArray(),
                comparisonDiagnostics,
                diagnostics);
        }

        private void BuildTerrainChunks(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainTileUvMap uvMap,
            ArcGraphRenderState renderState,
            ArcGraphTerrainVisualPolicy visualPolicy,
            List<ArcGraphTerrainChunkMeshData> target)
        {
            if (target == null)
                return;

            target.Clear();

            List<ArcGraphTerrainChunkMeshData> chunks = _terrainBuilder.BuildDirtyChunks(
                terrainLayer,
                uvMap,
                renderState,
                visualPolicy);

            for (int i = 0; i < chunks.Count; i++)
                target.Add(chunks[i]);
        }

        private static ArcGraphVisualProbeDiagnostics CreateDiagnostics(
            IReadOnlyList<ArcGraphTerrainChunkMeshData> terrainChunks,
            ArcGraphRenderQueue actorObjectQueue,
            IReadOnlyList<ArcGraphVegetationRenderItem> vegetationItems,
            IReadOnlyList<ArcGraphWaterRenderItem> waterItems,
            IReadOnlyList<ArcGraphLightRenderItem> lightItems,
            ArcGraphComparisonDiagnostics comparisonDiagnostics)
        {
            int terrainChunkCount = terrainChunks?.Count ?? 0;
            int terrainCellCount = CountTerrainCells(terrainChunks);
            int actorObjectEntryCount = actorObjectQueue?.Entries.Count ?? 0;
            int vegetationItemCount = vegetationItems?.Count ?? 0;
            int waterItemCount = waterItems?.Count ?? 0;
            int lightItemCount = lightItems?.Count ?? 0;

            bool hasMinimumData = terrainCellCount > 0
                                  && actorObjectEntryCount > 0
                                  && vegetationItemCount > 0
                                  && waterItemCount > 0
                                  && lightItemCount > 0;

            bool gateAllows = comparisonDiagnostics.IsAllowed;
            string reason = hasMinimumData && gateAllows
                ? "VisualProbeFrameReady"
                : ResolveFailureReason(hasMinimumData, comparisonDiagnostics);

            return new ArcGraphVisualProbeDiagnostics(
                hasMinimumData && gateAllows,
                reason,
                terrainChunkCount,
                terrainCellCount,
                actorObjectEntryCount,
                vegetationItemCount,
                waterItemCount,
                lightItemCount,
                requiresSceneRenderer: true);
        }

        private static int CountTerrainCells(IReadOnlyList<ArcGraphTerrainChunkMeshData> terrainChunks)
        {
            if (terrainChunks == null)
                return 0;

            int total = 0;
            for (int i = 0; i < terrainChunks.Count; i++)
                total += terrainChunks[i].Diagnostics.CellCount;

            return total;
        }

        private static string ResolveFailureReason(
            bool hasMinimumData,
            ArcGraphComparisonDiagnostics comparisonDiagnostics)
        {
            if (!hasMinimumData)
                return "VisualProbeDataIncomplete";

            if (!comparisonDiagnostics.IsAllowed)
                return comparisonDiagnostics.Reason;

            return "VisualProbeNotReady";
        }
    }
}
