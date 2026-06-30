using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorRenderQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot actor ArcGraph in render item
    /// ordinabili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: actor renderer senza controllo runtime</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphActorLayer</c> e un profilo LOD. Produce
    /// item actor value-only, usando la posa visuale gia' derivata dallo snapshot.
    /// Non chiama movimento, non completa job, non cambia posizione NPC, non legge
    /// input e non crea componenti Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola una lista actor render item ordinata.</item>
    ///   <item><b>CreateItem</b>: risolve posa e LOD per uno snapshot actor.</item>
    ///   <item><b>CompareActors</b>: sorting deterministico via sort key.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphActorRenderQueueBuilder
    {
        private const int ActorVisualLayerOrder = 20;

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue actor a partire dal layer actor ArcGraph.
        /// </para>
        ///
        /// <para><b>Posa visuale derivata</b></para>
        /// <para>
        /// Ogni snapshot actor viene trasformato in posa tramite
        /// <c>ResolvePose()</c>. Se il motion snapshot e' inattivo, la posa resta
        /// sulla cella discreta; se in futuro il motion verra' alimentato dal bridge
        /// v0.35, il builder ricevera' automaticamente coordinate frazionarie senza
        /// cambiare contratto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>actorLayer</b>: cache snapshot da leggere.</item>
        ///   <item><b>lodProfile</b>: policy visuale gia' risolta.</item>
        ///   <item><b>target</b>: lista render item da popolare.</item>
        ///   <item><b>includeHiddenItems</b>: se true, conserva anche item nascosti.</item>
        /// </list>
        /// </summary>
        public ArcGraphRenderQueueDiagnostics Build(
            ArcGraphActorLayer actorLayer,
            ArcGraphZoomLodProfile lodProfile,
            IList<ArcGraphActorRenderItem> target,
            bool clearTarget = true,
            bool includeHiddenItems = false)
        {
            if (target == null)
            {
                return new ArcGraphRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    "TargetMissing");
            }

            if (clearTarget)
                target.Clear();

            if (actorLayer == null)
            {
                return new ArcGraphRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    "ActorLayerMissing");
            }

            var snapshots = new List<ArcGraphActorVisualSnapshot>();
            actorLayer.CopySnapshotsTo(snapshots);

            int visibleCount = 0;
            int hiddenCount = 0;

            for (int i = 0; i < snapshots.Count; i++)
            {
                ArcGraphActorRenderItem item = CreateItem(snapshots[i], lodProfile);

                if (item.IsVisible)
                    visibleCount++;
                else
                    hiddenCount++;

                if (item.IsVisible || includeHiddenItems)
                    target.Add(item);
            }

            Sort(target);

            return new ArcGraphRenderQueueDiagnostics(
                snapshots.Count,
                0,
                visibleCount,
                hiddenCount,
                "ActorQueueBuilt");
        }

        private static ArcGraphActorRenderItem CreateItem(
            ArcGraphActorVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile)
        {
            ArcGraphActorVisualPoseSnapshot pose = snapshot.ResolvePose();

            bool isVisible = true;
            string hiddenReason = "None";

            if (snapshot.ActorId <= 0)
            {
                isVisible = false;
                hiddenReason = "InvalidActorId";
            }
            else if (string.IsNullOrWhiteSpace(snapshot.BaseSpriteKey))
            {
                isVisible = false;
                hiddenReason = "MissingSpriteKey";
            }

            var sortKey = ArcGraphRenderSortKey.FromCell(
                pose.DiscreteCell,
                ActorVisualLayerOrder,
                ArcGraphRenderItemKind.Actor,
                snapshot.ActorId);

            return new ArcGraphActorRenderItem(
                snapshot.ActorId,
                pose.DiscreteCell,
                pose.VisualX,
                pose.VisualY,
                pose.VisualZ,
                pose.BaseSpriteKey,
                lodProfile.ActorMode,
                lodProfile.AllowsSpriteAnimation,
                lodProfile.AllowsLayeredActorSprites,
                pose.HasMotion,
                pose.Progress01,
                isVisible,
                hiddenReason,
                sortKey,
                snapshot.HasHungerValue,
                snapshot.Hunger01,
                snapshot.FacingDirectionKey,
                snapshot.RunningActionOverlay);
        }

        private static void Sort(IList<ArcGraphActorRenderItem> target)
        {
            if (target == null || target.Count <= 1)
                return;

            if (target is List<ArcGraphActorRenderItem> list)
            {
                list.Sort(CompareActors);
                return;
            }

            var copy = new List<ArcGraphActorRenderItem>(target);
            copy.Sort(CompareActors);

            target.Clear();
            for (int i = 0; i < copy.Count; i++)
            {
                target.Add(copy[i]);
            }
        }

        private static int CompareActors(
            ArcGraphActorRenderItem left,
            ArcGraphActorRenderItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }
    }
}
