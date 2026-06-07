using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectRenderQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot oggetto ArcGraph in render item
    /// ordinabili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: queue oggetti senza asset e senza scena</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphObjectLayer</c> e un profilo LOD gia'
    /// risolto. Produce <c>ArcGraphObjectRenderItem</c>, ma non crea sprite, non
    /// carica asset, non legge il <c>World</c>, non modifica stock e non decide
    /// ownership. E' un traduttore deterministico tra cache visuale e futuro
    /// wrapper renderer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola una lista di item oggetto ordinati.</item>
    ///   <item><b>CreateItem</b>: converte uno snapshot in item value-only.</item>
    ///   <item><b>CompareObjects</b>: sorting deterministico via sort key.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphObjectRenderQueueBuilder
    {
        private const int ObjectVisualLayerOrder = 10;

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue oggetti a partire dal layer oggetti ArcGraph.
        /// </para>
        ///
        /// <para><b>Output controllato</b></para>
        /// <para>
        /// Il metodo usa una lista temporanea di snapshot copiati dal layer, poi
        /// produce item renderizzabili nel target. Gli item nascosti possono essere
        /// esclusi dal target ma restano contati nella diagnostica, cosi' il QA puo'
        /// capire perche' un oggetto non comparirebbe.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>objectLayer</b>: cache snapshot da leggere.</item>
        ///   <item><b>lodProfile</b>: policy visuale gia' risolta.</item>
        ///   <item><b>target</b>: lista render item da popolare.</item>
        ///   <item><b>includeHiddenItems</b>: se true, conserva anche item nascosti.</item>
        /// </list>
        /// </summary>
        public ArcGraphRenderQueueDiagnostics Build(
            ArcGraphObjectLayer objectLayer,
            ArcGraphZoomLodProfile lodProfile,
            IList<ArcGraphObjectRenderItem> target,
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

            if (objectLayer == null)
            {
                return new ArcGraphRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    "ObjectLayerMissing");
            }

            var snapshots = new List<ArcGraphObjectVisualSnapshot>();
            objectLayer.CopySnapshotsTo(snapshots);

            int visibleCount = 0;
            int hiddenCount = 0;

            for (int i = 0; i < snapshots.Count; i++)
            {
                ArcGraphObjectRenderItem item = CreateItem(snapshots[i], lodProfile);

                if (item.IsVisible)
                    visibleCount++;
                else
                    hiddenCount++;

                if (item.IsVisible || includeHiddenItems)
                    target.Add(item);
            }

            Sort(target);

            return new ArcGraphRenderQueueDiagnostics(
                0,
                snapshots.Count,
                visibleCount,
                hiddenCount,
                "ObjectQueueBuilt");
        }

        private static ArcGraphObjectRenderItem CreateItem(
            ArcGraphObjectVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile)
        {
            bool isVisible = true;
            string hiddenReason = "None";

            if (snapshot.ObjectId <= 0)
            {
                isVisible = false;
                hiddenReason = "InvalidObjectId";
            }
            else if (snapshot.IsHeld)
            {
                isVisible = false;
                hiddenReason = "HeldObject";
            }
            else if (string.IsNullOrWhiteSpace(snapshot.SpriteKey))
            {
                isVisible = false;
                hiddenReason = "MissingSpriteKey";
            }

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.Cell,
                ObjectVisualLayerOrder,
                ArcGraphRenderItemKind.Object,
                snapshot.ObjectId);

            return new ArcGraphObjectRenderItem(
                snapshot.ObjectId,
                snapshot.DefId,
                snapshot.Cell,
                snapshot.SpriteKey,
                lodProfile.ObjectMode,
                lodProfile.UsesSimplifiedRepresentation,
                lodProfile.ShowMinorItems,
                snapshot.IsHeld,
                snapshot.HolderActorId,
                snapshot.FoodStockUnits,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static void Sort(IList<ArcGraphObjectRenderItem> target)
        {
            if (target == null || target.Count <= 1)
                return;

            if (target is List<ArcGraphObjectRenderItem> list)
            {
                list.Sort(CompareObjects);
                return;
            }

            var copy = new List<ArcGraphObjectRenderItem>(target);
            copy.Sort(CompareObjects);

            target.Clear();
            for (int i = 0; i < copy.Count; i++)
            {
                target.Add(copy[i]);
            }
        }

        private static int CompareObjects(
            ArcGraphObjectRenderItem left,
            ArcGraphObjectRenderItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }
    }
}
