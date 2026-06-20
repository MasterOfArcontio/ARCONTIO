using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWaterRenderQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot acqua ArcGraph in render item
    /// ordinabili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: acqua renderizzabile senza flusso produttivo</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphWaterLayer</c>, il contratto ambientale
    /// visuale e un profilo LOD gia' risolto. Produce item value-only per un futuro
    /// renderer, ma non calcola flusso, pressione, profondita' fisica, sorgenti,
    /// fiumi, laghi, evaporazione o attraversabilita'.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola una lista di item acqua ordinati.</item>
    ///   <item><b>CreateItem</b>: converte snapshot + LOD in item value-only.</item>
    ///   <item><b>ResolveSpriteKey</b>: produce chiave testuale, non asset reale.</item>
    ///   <item><b>Sort</b>: ordina in modo deterministico via sort key.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphWaterRenderQueueBuilder
    {
        private const int WaterVisualLayerOrder = 3;
        private readonly List<ArcGraphWaterVisualSnapshot> _snapshotBuffer = new();

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue acqua a partire dal layer acqua ArcGraph.
        /// </para>
        ///
        /// <para><b>Output controllato</b></para>
        /// <para>
        /// Il metodo copia snapshot read-only dal layer, applica contratto e LOD,
        /// poi produce item nel target. Gli item nascosti possono essere esclusi
        /// dal target ma restano contati nella diagnostica.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>waterLayer</b>: cache snapshot da leggere.</item>
        ///   <item><b>lodProfile</b>: policy visuale gia' risolta.</item>
        ///   <item><b>target</b>: lista render item da popolare.</item>
        ///   <item><b>includeHiddenItems</b>: se true, conserva anche item nascosti.</item>
        /// </list>
        /// </summary>
        public ArcGraphWaterRenderQueueDiagnostics Build(
            ArcGraphWaterLayer waterLayer,
            ArcGraphZoomLodProfile lodProfile,
            IList<ArcGraphWaterRenderItem> target,
            bool clearTarget = true,
            bool includeHiddenItems = false)
        {
            if (target == null)
            {
                return new ArcGraphWaterRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    "TargetMissing");
            }

            if (clearTarget)
                target.Clear();

            if (waterLayer == null)
            {
                return new ArcGraphWaterRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    "WaterLayerMissing");
            }

            if (!ArcGraphEnvironmentVisualContractCatalog.TryGetDefaultContract(
                    ArcGraphLayerId.Water,
                    out var contract))
            {
                return new ArcGraphWaterRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    "WaterContractMissing");
            }

            _snapshotBuffer.Clear();
            waterLayer.CopySnapshotsTo(_snapshotBuffer);

            int visibleCount = 0;
            int hiddenCount = 0;
            int animatedCount = 0;
            int maxDepth = 0;

            for (int i = 0; i < _snapshotBuffer.Count; i++)
            {
                ArcGraphWaterRenderItem item = CreateItem(
                    _snapshotBuffer[i],
                    lodProfile,
                    contract);

                if (item.DepthLevel > maxDepth)
                    maxDepth = item.DepthLevel;

                if (item.IsVisible)
                    visibleCount++;
                else
                    hiddenCount++;

                if (item.IsVisible && item.IsAnimated)
                    animatedCount++;

                if (item.IsVisible || includeHiddenItems)
                    target.Add(item);
            }

            Sort(target);

            return new ArcGraphWaterRenderQueueDiagnostics(
                _snapshotBuffer.Count,
                visibleCount,
                hiddenCount,
                animatedCount,
                maxDepth,
                "WaterQueueBuilt");
        }

        private static ArcGraphWaterRenderItem CreateItem(
            ArcGraphWaterVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile,
            ArcGraphEnvironmentVisualLayerContract contract)
        {
            bool isVisible = true;
            string hiddenReason = "None";

            if (!contract.IsEnvironmentLayer || contract.LayerId != ArcGraphLayerId.Water)
            {
                isVisible = false;
                hiddenReason = "InvalidWaterContract";
            }
            else if (!contract.RequiresExternalSnapshots)
            {
                isVisible = false;
                hiddenReason = "WaterMustUseExternalSnapshots";
            }
            else if (snapshot.DepthLevel <= 0)
            {
                isVisible = false;
                hiddenReason = "EmptyWaterDepth";
            }

            bool allowsAnimation = contract.AllowsArcGraphSpriteAnimation
                                   && lodProfile.AllowsSpriteAnimation
                                   && snapshot.IsAnimated;

            string spriteKey = isVisible
                ? ResolveSpriteKey(snapshot, lodProfile)
                : string.Empty;

            if (isVisible && string.IsNullOrWhiteSpace(spriteKey))
            {
                isVisible = false;
                hiddenReason = "MissingWaterSpriteKey";
            }

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.Cell,
                WaterVisualLayerOrder,
                ArcGraphRenderItemKind.Water,
                ResolveStableEntityId(snapshot));

            return new ArcGraphWaterRenderItem(
                snapshot.Cell,
                snapshot.DepthLevel,
                spriteKey,
                allowsAnimation,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static string ResolveSpriteKey(
            ArcGraphWaterVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.SpriteKey))
                return snapshot.SpriteKey.Trim();

            return "ArcGraph/Water/depth_" + snapshot.DepthLevel;
        }

        private static int ResolveStableEntityId(ArcGraphWaterVisualSnapshot snapshot)
        {
            unchecked
            {
                int hash = 19;
                hash = (hash * 31) + snapshot.Cell.X;
                hash = (hash * 31) + snapshot.Cell.Y;
                hash = (hash * 31) + snapshot.Cell.Z;
                hash = (hash * 31) + snapshot.DepthLevel;
                return hash & int.MaxValue;
            }
        }

        private static void Sort(IList<ArcGraphWaterRenderItem> target)
        {
            if (target == null || target.Count <= 1)
                return;

            if (target is List<ArcGraphWaterRenderItem> list)
            {
                list.Sort(CompareItems);
                return;
            }

            var copy = new List<ArcGraphWaterRenderItem>(target);
            copy.Sort(CompareItems);

            target.Clear();
            for (int i = 0; i < copy.Count; i++)
                target.Add(copy[i]);
        }

        private static int CompareItems(
            ArcGraphWaterRenderItem left,
            ArcGraphWaterRenderItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }
    }
}
