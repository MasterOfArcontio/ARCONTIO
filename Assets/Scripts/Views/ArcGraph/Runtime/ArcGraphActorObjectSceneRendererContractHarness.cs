using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorObjectSceneRendererContractHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test del contratto scene renderer
    /// actor/object.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica scene-side senza scena</b></para>
    /// <para>
    /// Il risultato conserva solo contatori e flag. Non contiene sprite, renderer,
    /// GameObject o riferimenti a Unity scene. Serve a validare il passaggio
    /// <c>ArcGraphRenderQueue</c> -> piano scena.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>Reason</b>: descrizione sintetica.</item>
    ///   <item><b>PlannedEntryCount</b>: entry scene-side prodotte.</item>
    ///   <item><b>ObjectBeforeActor</b>: verifica ordine su stessa cella.</item>
    ///   <item><b>ActorUsesInterpolatedPose</b>: verifica posizione actor frazionaria.</item>
    ///   <item><b>ObjectCarriesVisualMetadata</b>: verifica propagazione dati visuali oggetto.</item>
    ///   <item><b>TallObjectUsesBottomPivot</b>: verifica ancoraggio basso centrato sulla cella di oggetti alti.</item>
    ///   <item><b>ObjectCarriesMiniTileMask</b>: verifica propagazione della maschera base 2x2.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorObjectSceneRendererContractHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int PlannedEntryCount;
        public readonly int ActorEntryCount;
        public readonly int ObjectEntryCount;
        public readonly bool ObjectBeforeActor;
        public readonly bool ActorUsesInterpolatedPose;
        public readonly bool ObjectCarriesVisualMetadata;
        public readonly bool TallObjectUsesBottomPivot;
        public readonly bool ObjectCarriesMiniTileMask;
        public readonly bool ContractSafe;

        public ArcGraphActorObjectSceneRendererContractHarnessResult(
            bool passed,
            string reason,
            int plannedEntryCount,
            int actorEntryCount,
            int objectEntryCount,
            bool objectBeforeActor,
            bool actorUsesInterpolatedPose,
            bool objectCarriesVisualMetadata,
            bool tallObjectUsesBottomPivot,
            bool objectCarriesMiniTileMask,
            bool contractSafe)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            PlannedEntryCount = plannedEntryCount;
            ActorEntryCount = actorEntryCount;
            ObjectEntryCount = objectEntryCount;
            ObjectBeforeActor = objectBeforeActor;
            ActorUsesInterpolatedPose = actorUsesInterpolatedPose;
            ObjectCarriesVisualMetadata = objectCarriesVisualMetadata;
            TallObjectUsesBottomPivot = tallObjectUsesBottomPivot;
            ObjectCarriesMiniTileMask = objectCarriesMiniTileMask;
            ContractSafe = contractSafe;
        }
    }

    // =============================================================================
    // ArcGraphActorObjectSceneRendererContractHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il contratto scene-side actor/object ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test del ponte scena prima del MonoBehaviour</b></para>
    /// <para>
    /// L'harness costruisce layer actor/object, render queue e piano scena in
    /// memoria. Non usa scene, non crea <c>GameObject</c>, non risolve sprite e non
    /// legge il <c>World</c>. Verifica solo che il contratto produca entry ordinate,
    /// posizione actor interpolata e sorting order coerente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario minimo actor/object.</item>
    ///   <item><b>CreateActors</b>: actor in movimento con sprite valido.</item>
    ///   <item><b>CreateObjects</b>: oggetto visibile sulla cella di arrivo.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphActorObjectSceneRendererContractHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test default del piano scena actor/object.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// L'actor si muove da (1,1) a (2,1) con progresso 50%. La sua posizione
        /// visuale attesa e' quindi centrata a x = 2.0, y = 1.5 in world units
        /// quando tile size = 1. L'oggetto e l'actor passano dalla render queue,
        /// quindi lo sorting order deve seguire l'ordine globale gia' prodotto da
        /// ArcGraph.
        /// </para>
        /// </summary>
        public static ArcGraphActorObjectSceneRendererContractHarnessResult RunDefaultSmoke()
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
            var queueBuilder = new ArcGraphRenderQueueBuilder();
            queueBuilder.Build(actorLayer, objectLayer, lodProfile, queue);

            var contract = ArcGraphActorObjectSceneRendererContract.CreateTemporaryProbeContract();
            var plan = new ArcGraphActorObjectSceneRenderPlan();
            var planBuilder = new ArcGraphActorObjectSceneRenderPlanBuilder();
            ArcGraphActorObjectSceneRendererDiagnostics diagnostics = planBuilder.Build(
                queue,
                contract,
                plan,
                hasSpriteResolver: true);

            bool objectBeforeActor = plan.Entries.Count == 2
                                     && plan.Entries[0].Kind == ArcGraphRenderItemKind.Object
                                     && plan.Entries[1].Kind == ArcGraphRenderItemKind.Actor
                                     && plan.Entries[0].SortingOrder < plan.Entries[1].SortingOrder;

            bool actorInterpolated = plan.Entries.Count == 2
                                     && plan.Entries[1].Kind == ArcGraphRenderItemKind.Actor
                                     && Approximately(plan.Entries[1].WorldX, 2.0f)
                                     && Approximately(plan.Entries[1].WorldY, 1.5f)
                                     && plan.Entries[1].HasMotion
                                     && Approximately(plan.Entries[1].MotionProgress01, 0.5f);

            bool passed = diagnostics.ContractSafe
                          && diagnostics.PlannedEntryCount == 2
                          && diagnostics.ActorEntryCount == 1
                          && diagnostics.ObjectEntryCount == 1
                          && objectBeforeActor
                          && actorInterpolated
                          && ObjectCarriesVisualMetadata(plan)
                          && TallObjectUsesBottomPivot(plan)
                          && ObjectCarriesMiniTileMask(plan);

            return new ArcGraphActorObjectSceneRendererContractHarnessResult(
                passed,
                passed ? "ActorObjectSceneRendererContractSmokePassed" : "ActorObjectSceneRendererContractSmokeFailed",
                diagnostics.PlannedEntryCount,
                diagnostics.ActorEntryCount,
                diagnostics.ObjectEntryCount,
                objectBeforeActor,
                actorInterpolated,
                ObjectCarriesVisualMetadata(plan),
                TallObjectUsesBottomPivot(plan),
                ObjectCarriesMiniTileMask(plan),
                diagnostics.ContractSafe);
        }

        private static IEnumerable<ArcGraphActorVisualSnapshot> CreateActors()
        {
            var from = new ArcGraphCellCoord(1, 1, 0);
            var to = new ArcGraphCellCoord(2, 1, 0);

            yield return new ArcGraphActorVisualSnapshot(
                1,
                from,
                "MapGrid/Sprites/NPC_Astro",
                ArcGraphActorMotionSnapshot.CreateMovement(
                    from,
                    to,
                    elapsedTicks: 5,
                    requiredTicks: 10));
        }

        private static IEnumerable<ArcGraphObjectVisualSnapshot> CreateObjects()
        {
            yield return new ArcGraphObjectVisualSnapshot(
                10,
                "tall_crate",
                new ArcGraphCellCoord(1, 1, 0),
                "MapGrid/Sprites/Objects/crate",
                isHeld: false,
                holderActorId: 0,
                foodStockUnits: -1,
                footprintWidth: 1,
                footprintHeight: 1,
                visualKind: string.Empty,
                visualResolverKey: string.Empty,
                visualWidthPixels: 32,
                visualHeightPixels: 83,
                visualBaseWidthPixels: 32,
                visualBaseHeightPixels: 32,
                visualBaseMiniTileMask: "0110",
                visualPivot: "bottom_center",
                visualOffsetX: 0,
                visualOffsetY: 0,
                fadeWhenActorBehind: true,
                useShadow: false);
        }

        private static bool ObjectCarriesVisualMetadata(
            ArcGraphActorObjectSceneRenderPlan plan)
        {
            if (plan == null || plan.Entries.Count <= 0)
                return false;

            ArcGraphActorObjectSceneRenderEntry entry = plan.Entries[0];
            return entry.Kind == ArcGraphRenderItemKind.Object
                   && entry.HasObjectVisualMetadata
                   && entry.IsTallObjectVisual
                   && entry.VisualWidthPixels == 32
                   && entry.VisualHeightPixels == 83
                   && entry.VisualBaseWidthPixels == 32
                   && entry.VisualBaseHeightPixels == 32
                   && entry.VisualBaseMiniTileMask == "0110"
                   && entry.VisualPivot == "bottom_center"
                   && entry.FadeWhenActorBehind;
        }

        private static bool ObjectCarriesMiniTileMask(
            ArcGraphActorObjectSceneRenderPlan plan)
        {
            if (plan == null || plan.Entries.Count <= 0)
                return false;

            ArcGraphActorObjectSceneRenderEntry entry = plan.Entries[0];
            return entry.Kind == ArcGraphRenderItemKind.Object
                   && entry.VisualBaseMiniTileMask == "0110";
        }

        private static bool TallObjectUsesBottomPivot(
            ArcGraphActorObjectSceneRenderPlan plan)
        {
            if (plan == null || plan.Entries.Count <= 0)
                return false;

            ArcGraphActorObjectSceneRenderEntry entry = plan.Entries[0];
            return entry.Kind == ArcGraphRenderItemKind.Object
                   && Approximately(entry.WorldX, 1.5f)
                   && Approximately(entry.WorldY, 1.5f);
        }

        private static bool Approximately(float left, float right)
        {
            float delta = left - right;
            if (delta < 0f)
                delta = -delta;

            return delta <= 0.0001f;
        }
    }
}
