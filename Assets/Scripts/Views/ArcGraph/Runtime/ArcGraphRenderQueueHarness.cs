using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRenderQueueHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test actor/object render queue.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica senza scena</b></para>
    /// <para>
    /// Il risultato contiene solo contatori e flag. Non contiene riferimenti a
    /// sprite, GameObject, asset o camera. Serve a capire se la queue passiva
    /// actor/object produce un ordine globale coerente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>Reason</b>: descrizione sintetica.</item>
    ///   <item><b>ActorItemCount/ObjectItemCount</b>: snapshot processati.</item>
    ///   <item><b>VisibleItemCount/HiddenItemCount</b>: diagnostica visibilita'.</item>
    ///   <item><b>EntryCount</b>: entry visibili ordinate.</item>
    ///   <item><b>ObjectBeforeActorOnSameCell</b>: verifica sorting locale.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphRenderQueueHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int ActorItemCount;
        public readonly int ObjectItemCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly int EntryCount;
        public readonly bool ObjectBeforeActorOnSameCell;

        public ArcGraphRenderQueueHarnessResult(
            bool passed,
            string reason,
            int actorItemCount,
            int objectItemCount,
            int visibleItemCount,
            int hiddenItemCount,
            int entryCount,
            bool objectBeforeActorOnSameCell)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            ActorItemCount = actorItemCount;
            ObjectItemCount = objectItemCount;
            VisibleItemCount = visibleItemCount;
            HiddenItemCount = hiddenItemCount;
            EntryCount = entryCount;
            ObjectBeforeActorOnSameCell = objectBeforeActorOnSameCell;
        }
    }

    // =============================================================================
    // ArcGraphRenderQueueHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la queue actor/object passiva.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test del flusso render senza renderer</b></para>
    /// <para>
    /// L'harness costruisce layer, snapshot, profilo LOD, builder e queue interamente
    /// in memoria. Non usa scene, non crea GameObject, non carica sprite e non
    /// legge il World. Verifica solo il contratto: snapshot -> render item -> queue
    /// ordinata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo actor/object.</item>
    ///   <item><b>CreateActors</b>: actor visibile e actor nascosto.</item>
    ///   <item><b>CreateObjects</b>: oggetto visibile e oggetti nascosti.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphRenderQueueHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test default della queue actor/object.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// Lo scenario usa un actor visibile e un oggetto visibile nella stessa
        /// cella. Aggiunge poi un actor senza sprite key, un oggetto held e un
        /// oggetto senza sprite key per verificare i contatori hidden. L'ordine
        /// atteso e': oggetto prima, actor dopo, sulla stessa cella.
        /// </para>
        /// </summary>
        public static ArcGraphRenderQueueHarnessResult RunDefaultSmoke()
        {
            var renderState = new ArcGraphRenderState(
                visibleZLevel: ArcGraphZLevelPolicy.DefaultVisibleZLevel,
                tileSizeWorld: 1f,
                chunkSizeCells: 4);

            var actorLayer = new ArcGraphActorLayer();
            actorLayer.Initialize(renderState);
            actorLayer.ReplaceSnapshots(CreateActors(), renderState);

            var objectLayer = new ArcGraphObjectLayer();
            objectLayer.Initialize(renderState);
            objectLayer.ReplaceSnapshots(CreateObjects(), renderState);

            var config = ArcGraphMapViewConfig.CreateDefaultV033();
            var lodProfile = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(4));
            var queue = new ArcGraphRenderQueue();
            var builder = new ArcGraphRenderQueueBuilder();
            ArcGraphRenderQueueDiagnostics diagnostics = builder.Build(
                actorLayer,
                objectLayer,
                lodProfile,
                queue);

            bool objectBeforeActor = queue.Entries.Count == 2
                                     && queue.Entries[0].Kind == ArcGraphRenderItemKind.Object
                                     && queue.Entries[1].Kind == ArcGraphRenderItemKind.Actor;

            bool passed = diagnostics.ActorItemCount == 2
                          && diagnostics.ObjectItemCount == 3
                          && diagnostics.VisibleItemCount == 2
                          && diagnostics.HiddenItemCount == 3
                          && queue.Entries.Count == 2
                          && objectBeforeActor;

            return new ArcGraphRenderQueueHarnessResult(
                passed,
                passed ? "RenderQueueSmokePassed" : "RenderQueueSmokeFailed",
                diagnostics.ActorItemCount,
                diagnostics.ObjectItemCount,
                diagnostics.VisibleItemCount,
                diagnostics.HiddenItemCount,
                queue.Entries.Count,
                objectBeforeActor);
        }

        private static IEnumerable<ArcGraphActorVisualSnapshot> CreateActors()
        {
            var sharedCell = new ArcGraphCellCoord(1, 1, 0);

            yield return new ArcGraphActorVisualSnapshot(
                1,
                sharedCell,
                "MapGrid/Sprites/NPC_Astro",
                ArcGraphActorMotionSnapshot.None(sharedCell));

            var hiddenCell = new ArcGraphCellCoord(0, 0, 0);
            yield return new ArcGraphActorVisualSnapshot(
                2,
                hiddenCell,
                string.Empty,
                ArcGraphActorMotionSnapshot.None(hiddenCell));
        }

        private static IEnumerable<ArcGraphObjectVisualSnapshot> CreateObjects()
        {
            yield return new ArcGraphObjectVisualSnapshot(
                10,
                "crate",
                new ArcGraphCellCoord(1, 1, 0),
                "MapGrid/Sprites/Objects/crate",
                isHeld: false,
                holderActorId: 0,
                foodStockUnits: -1);

            yield return new ArcGraphObjectVisualSnapshot(
                11,
                "apple",
                new ArcGraphCellCoord(0, 0, 0),
                "MapGrid/Sprites/Objects/apple",
                isHeld: true,
                holderActorId: 1,
                foodStockUnits: 4);

            yield return new ArcGraphObjectVisualSnapshot(
                12,
                "unknown",
                new ArcGraphCellCoord(2, 2, 0),
                string.Empty,
                isHeld: false,
                holderActorId: 0,
                foodStockUnits: -1);
        }
    }
}
