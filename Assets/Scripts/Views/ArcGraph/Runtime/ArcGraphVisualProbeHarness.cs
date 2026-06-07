using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVisualProbeHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sul frame di probe visuale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test di composizione, non test di scena</b></para>
    /// <para>
    /// Il risultato dice se i builder ArcGraph possono gia' produrre un frame
    /// completo con terreno, actor/object, vegetazione, acqua e luce. Non verifica
    /// colori reali, asset, camera o montaggio scena.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>Reason</b>: motivo sintetico.</item>
    ///   <item><b>TerrainCellCount</b>: celle terrain nel probe.</item>
    ///   <item><b>ActorObjectEntryCount</b>: entry actor/object nel probe.</item>
    ///   <item><b>VegetationItemCount</b>: item vegetazione nel probe.</item>
    ///   <item><b>WaterItemCount</b>: item acqua nel probe.</item>
    ///   <item><b>LightItemCount</b>: item luce nel probe.</item>
    ///   <item><b>CanAttachSceneProbe</b>: gate scena temporanea ammesso.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphVisualProbeHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int TerrainCellCount;
        public readonly int ActorObjectEntryCount;
        public readonly int VegetationItemCount;
        public readonly int WaterItemCount;
        public readonly int LightItemCount;
        public readonly bool CanAttachSceneProbe;

        public ArcGraphVisualProbeHarnessResult(
            bool passed,
            string reason,
            int terrainCellCount,
            int actorObjectEntryCount,
            int vegetationItemCount,
            int waterItemCount,
            int lightItemCount,
            bool canAttachSceneProbe)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            TerrainCellCount = terrainCellCount;
            ActorObjectEntryCount = actorObjectEntryCount;
            VegetationItemCount = vegetationItemCount;
            WaterItemCount = waterItemCount;
            LightItemCount = lightItemCount;
            CanAttachSceneProbe = canAttachSceneProbe;
        }
    }

    // =============================================================================
    // ArcGraphVisualProbeHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la composizione minima del Visual Probe.
    /// </para>
    ///
    /// <para><b>Principio architetturale: scena finta, dati reali ArcGraph</b></para>
    /// <para>
    /// Lo smoke test costruisce layer ArcGraph in memoria e li popola con snapshot
    /// controllati. Il builder produce poi un frame dati completo. Questo permette
    /// di controllare se il prossimo passo puo' diventare un test visivo concreto
    /// senza ancora toccare scene o prefab.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario 4x4 con tutti i layer base.</item>
    ///   <item><b>CreateTerrainSnapshots</b>: celle terrain di test.</item>
    ///   <item><b>CreateUvMap</b>: UV map minima per il builder terrain.</item>
    ///   <item><b>PopulateEnvironment</b>: snapshot ambiente controllati.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphVisualProbeHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test default del frame di probe visuale.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// La scena finta contiene 16 celle terreno, un actor, un oggetto, una
        /// vegetazione, un'acqua e due overlay luce visibili. I prerequisiti scena
        /// vengono dichiarati come presenti per validare il gate di aggancio
        /// temporaneo, ma nessun aggancio viene realmente eseguito.
        /// </para>
        /// </summary>
        public static ArcGraphVisualProbeHarnessResult RunDefaultSmoke()
        {
            ArcGraphVisualProbeFrame frame = CreateDefaultProbeFrame(
                hasLegacyRenderer: true,
                hasCamera: true,
                hasMaterial: true,
                requestSceneProbe: true);

            bool passed = frame.Diagnostics.CanBuildVisualProbe
                          && frame.Diagnostics.TerrainCellCount == 16
                          && frame.Diagnostics.ActorObjectEntryCount == 2
                          && frame.Diagnostics.VegetationItemCount == 1
                          && frame.Diagnostics.WaterItemCount == 1
                          && frame.Diagnostics.LightItemCount == 2
                          && frame.ComparisonDiagnostics.CanAttachSceneProbe;

            return new ArcGraphVisualProbeHarnessResult(
                passed,
                passed ? "VisualProbeSmokePassed" : frame.Diagnostics.Reason,
                frame.Diagnostics.TerrainCellCount,
                frame.Diagnostics.ActorObjectEntryCount,
                frame.Diagnostics.VegetationItemCount,
                frame.Diagnostics.WaterItemCount,
                frame.Diagnostics.LightItemCount,
                frame.ComparisonDiagnostics.CanAttachSceneProbe);
        }

        // =============================================================================
        // CreateDefaultProbeFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il frame default usato dal primo test visivo controllato.
        /// </para>
        ///
        /// <para><b>Scenario riusabile dal renderer debug</b></para>
        /// <para>
        /// Il metodo espone lo stesso scenario dello smoke test come frame dati.
        /// Questo permette al componente scena temporaneo di disegnare esattamente
        /// gli stessi dati validati dal probe data-only, senza duplicare la logica
        /// di popolamento dei layer.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>hasLegacyRenderer</b>: prerequisito dichiarato per il gate comparativo.</item>
        ///   <item><b>hasCamera</b>: prerequisito dichiarato per il probe scena.</item>
        ///   <item><b>hasMaterial</b>: prerequisito dichiarato per il probe scena.</item>
        ///   <item><b>requestSceneProbe</b>: richiede modalita' temporanea di scena.</item>
        /// </list>
        /// </summary>
        public static ArcGraphVisualProbeFrame CreateDefaultProbeFrame(
            bool hasLegacyRenderer,
            bool hasCamera,
            bool hasMaterial,
            bool requestSceneProbe)
        {
            var renderState = new ArcGraphRenderState(
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                tileSizeWorld: 1f,
                chunkSizeCells: 4);

            var terrainLayer = new ArcGraphTerrainLayer();
            var actorLayer = new ArcGraphActorLayer();
            var objectLayer = new ArcGraphObjectLayer();
            var vegetationLayer = new ArcGraphVegetationLayer();
            var waterLayer = new ArcGraphWaterLayer();
            var lightLayer = new ArcGraphLightLayer();

            terrainLayer.Initialize(renderState);
            actorLayer.Initialize(renderState);
            objectLayer.Initialize(renderState);
            vegetationLayer.Initialize(renderState);
            waterLayer.Initialize(renderState);
            lightLayer.Initialize(renderState);

            terrainLayer.ReplaceSnapshots(CreateTerrainSnapshots(), renderState);
            PopulateActorObject(actorLayer, objectLayer, renderState);
            PopulateEnvironment(vegetationLayer, waterLayer, lightLayer, renderState);

            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var lodProfile = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(4));

            var builder = new ArcGraphVisualProbeBuilder();
            return builder.Build(
                terrainLayer,
                actorLayer,
                objectLayer,
                vegetationLayer,
                waterLayer,
                lightLayer,
                CreateUvMap(),
                ArcGraphTerrainVisualPolicy.CreateLegacyDefault(),
                renderState,
                lodProfile,
                hasLegacyRenderer,
                hasCamera,
                hasMaterial,
                requestSceneProbe);
        }

        private static IEnumerable<ArcGraphTerrainCellSnapshot> CreateTerrainSnapshots()
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    bool isBlocked = x == 3 && y == 3;
                    int tileId = isBlocked ? 10 : 0;

                    yield return new ArcGraphTerrainCellSnapshot(
                        new ArcGraphCellCoord(x, y, 0),
                        tileId,
                        isBlocked);
                }
            }
        }

        private static void PopulateActorObject(
            ArcGraphActorLayer actorLayer,
            ArcGraphObjectLayer objectLayer,
            ArcGraphRenderState renderState)
        {
            actorLayer.ReplaceSnapshots(
                new[]
                {
                    new ArcGraphActorVisualSnapshot(
                        actorId: 1,
                        cell: new ArcGraphCellCoord(1, 2, 0),
                        baseSpriteKey: "ArcGraph/Probe/Actor",
                        motion: ArcGraphActorMotionSnapshot.None(new ArcGraphCellCoord(1, 2, 0)))
                },
                renderState);

            objectLayer.ReplaceSnapshots(
                new[]
                {
                    new ArcGraphObjectVisualSnapshot(
                        objectId: 10,
                        defId: "probe_crate",
                        cell: new ArcGraphCellCoord(2, 2, 0),
                        spriteKey: "ArcGraph/Probe/Object",
                        isHeld: false,
                        holderActorId: 0,
                        foodStockUnits: -1)
                },
                renderState);
        }

        private static void PopulateEnvironment(
            ArcGraphVegetationLayer vegetationLayer,
            ArcGraphWaterLayer waterLayer,
            ArcGraphLightLayer lightLayer,
            ArcGraphRenderState renderState)
        {
            vegetationLayer.ReplaceSnapshots(
                new[]
                {
                    new ArcGraphVegetationVisualSnapshot(
                        new ArcGraphCellCoord(0, 1, 0),
                        speciesKey: "grass",
                        growthStage: 1,
                        density01: 0.8f)
                },
                renderState);

            waterLayer.ReplaceSnapshots(
                new[]
                {
                    new ArcGraphWaterVisualSnapshot(
                        new ArcGraphCellCoord(1, 1, 0),
                        depthLevel: 2,
                        spriteKey: "ArcGraph/Probe/Water",
                        isAnimated: true)
                },
                renderState);

            lightLayer.ReplaceSnapshots(
                new[]
                {
                    new ArcGraphLightVisualSnapshot(
                        new ArcGraphCellCoord(0, 0, 0),
                        intensity01: 0.1f,
                        tintKey: string.Empty,
                        hasLocalSource: false),
                    new ArcGraphLightVisualSnapshot(
                        new ArcGraphCellCoord(2, 1, 0),
                        intensity01: 1f,
                        tintKey: string.Empty,
                        hasLocalSource: true)
                },
                renderState);
        }

        private static ArcGraphTerrainTileUvMap CreateUvMap()
        {
            var uvMap = new ArcGraphTerrainTileUvMap(
                atlasWidthPixels: 64,
                atlasHeightPixels: 64,
                tilePixels: 16);

            uvMap.Register(0, 0, 0);
            uvMap.Register(1, 1, 0);
            uvMap.Register(2, 2, 0);
            uvMap.Register(3, 3, 0);
            uvMap.Register(10, 0, 1);
            uvMap.Register(11, 1, 1);

            return uvMap;
        }
    }
}
