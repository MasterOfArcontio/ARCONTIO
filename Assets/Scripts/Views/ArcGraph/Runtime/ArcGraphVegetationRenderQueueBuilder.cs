using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVegetationRenderQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot vegetazione ArcGraph in render item
    /// ordinabili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: vegetazione renderizzabile senza biosfera</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphVegetationLayer</c>, il contratto ambientale
    /// visuale e un profilo LOD gia' risolto. Produce item value-only per un futuro
    /// renderer, ma non simula crescita, non decide specie, non calcola fertilita',
    /// non legge meteo, non carica asset e non crea componenti Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola una lista di item vegetazione ordinati.</item>
    ///   <item><b>CreateItem</b>: converte snapshot + LOD in item value-only.</item>
    ///   <item><b>ResolveSpriteKey</b>: produce chiave testuale, non asset reale.</item>
    ///   <item><b>Sort</b>: ordina in modo deterministico via sort key.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphVegetationRenderQueueBuilder
    {
        private const int VegetationVisualLayerOrder = 4;
        private readonly List<ArcGraphVegetationVisualSnapshot> _snapshotBuffer = new();

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue vegetazione a partire dal layer vegetazione ArcGraph.
        /// </para>
        ///
        /// <para><b>Output controllato</b></para>
        /// <para>
        /// Il metodo copia gli snapshot dal layer tramite API read-only, applica il
        /// contratto ambientale e il LOD, poi produce item renderizzabili nel target.
        /// Gli item nascosti possono essere esclusi dal target ma restano contati
        /// nella diagnostica.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>vegetationLayer</b>: cache snapshot da leggere.</item>
        ///   <item><b>lodProfile</b>: policy visuale gia' risolta.</item>
        ///   <item><b>target</b>: lista render item da popolare.</item>
        ///   <item><b>includeHiddenItems</b>: se true, conserva anche item nascosti.</item>
        /// </list>
        /// </summary>
        public ArcGraphVegetationRenderQueueDiagnostics Build(
            ArcGraphVegetationLayer vegetationLayer,
            ArcGraphZoomLodProfile lodProfile,
            IList<ArcGraphVegetationRenderItem> target,
            bool clearTarget = true,
            bool includeHiddenItems = false)
        {
            if (target == null)
            {
                return new ArcGraphVegetationRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    "TargetMissing");
            }

            if (clearTarget)
                target.Clear();

            if (vegetationLayer == null)
            {
                return new ArcGraphVegetationRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    "VegetationLayerMissing");
            }

            if (!ArcGraphEnvironmentVisualContractCatalog.TryGetDefaultContract(
                    ArcGraphLayerId.Vegetation,
                    out var contract))
            {
                return new ArcGraphVegetationRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    "VegetationContractMissing");
            }

            _snapshotBuffer.Clear();
            vegetationLayer.CopySnapshotsTo(_snapshotBuffer);

            int visibleCount = 0;
            int hiddenCount = 0;
            int animatedCount = 0;
            int aggregatedCount = 0;

            for (int i = 0; i < _snapshotBuffer.Count; i++)
            {
                ArcGraphVegetationRenderItem item = CreateItem(
                    _snapshotBuffer[i],
                    lodProfile,
                    contract);

                if (item.IsVisible)
                    visibleCount++;
                else
                    hiddenCount++;

                if (item.IsVisible && item.AllowsSpriteAnimation)
                    animatedCount++;

                if (item.IsVisible && item.IsAreaAggregate)
                    aggregatedCount++;

                if (item.IsVisible || includeHiddenItems)
                    target.Add(item);
            }

            Sort(target);

            return new ArcGraphVegetationRenderQueueDiagnostics(
                _snapshotBuffer.Count,
                visibleCount,
                hiddenCount,
                animatedCount,
                aggregatedCount,
                "VegetationQueueBuilt");
        }

        private static ArcGraphVegetationRenderItem CreateItem(
            ArcGraphVegetationVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile,
            ArcGraphEnvironmentVisualLayerContract contract)
        {
            bool isVisible = true;
            string hiddenReason = "None";

            if (!contract.IsEnvironmentLayer || contract.LayerId != ArcGraphLayerId.Vegetation)
            {
                isVisible = false;
                hiddenReason = "InvalidVegetationContract";
            }
            else if (!contract.RequiresExternalSnapshots)
            {
                isVisible = false;
                hiddenReason = "VegetationMustUseExternalSnapshots";
            }
            else if (string.IsNullOrWhiteSpace(snapshot.SpeciesKey))
            {
                isVisible = false;
                hiddenReason = "MissingSpeciesKey";
            }
            else if (snapshot.Density01 <= 0f)
            {
                isVisible = false;
                hiddenReason = "EmptyVegetationDensity";
            }

            bool isAreaAggregate = lodProfile.VegetationMode == ArcGraphVegetationLodMode.AreaAggregate;
            bool allowsAnimation = contract.AllowsArcGraphSpriteAnimation
                                   && lodProfile.AllowsSpriteAnimation
                                   && lodProfile.VegetationMode == ArcGraphVegetationLodMode.IndividualAnimatedSprite;

            string spriteKey = isVisible
                ? ResolveSpriteKey(snapshot, lodProfile.VegetationMode)
                : string.Empty;

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.Cell,
                VegetationVisualLayerOrder,
                ArcGraphRenderItemKind.Vegetation,
                ResolveStableEntityId(snapshot));

            return new ArcGraphVegetationRenderItem(
                snapshot.Cell,
                snapshot.SpeciesKey,
                snapshot.GrowthStage,
                snapshot.Density01,
                spriteKey,
                lodProfile.VegetationMode,
                lodProfile.UsesSimplifiedRepresentation,
                allowsAnimation,
                isAreaAggregate,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static string ResolveSpriteKey(
            ArcGraphVegetationVisualSnapshot snapshot,
            ArcGraphVegetationLodMode vegetationMode)
        {
            string species = SanitizeKeyPart(snapshot.SpeciesKey);

            if (vegetationMode == ArcGraphVegetationLodMode.AreaAggregate)
                return "ArcGraph/Vegetation/Area/" + species;

            if (vegetationMode == ArcGraphVegetationLodMode.SimplifiedStaticSprite)
                return "ArcGraph/Vegetation/Simple/" + species;

            return "ArcGraph/Vegetation/" + species + "/stage_" + snapshot.GrowthStage;
        }

        private static int ResolveStableEntityId(ArcGraphVegetationVisualSnapshot snapshot)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + snapshot.Cell.X;
                hash = (hash * 31) + snapshot.Cell.Y;
                hash = (hash * 31) + snapshot.Cell.Z;
                hash = (hash * 31) + snapshot.GrowthStage;
                hash = (hash * 31) + StableStringHash(SanitizeKeyPart(snapshot.SpeciesKey));
                return hash & int.MaxValue;
            }
        }

        private static int StableStringHash(string value)
        {
            unchecked
            {
                int hash = 23;
                if (string.IsNullOrEmpty(value))
                    return hash;

                for (int i = 0; i < value.Length; i++)
                    hash = (hash * 31) + value[i];

                return hash;
            }
        }

        private static string SanitizeKeyPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            return value.Trim().Replace(' ', '_');
        }

        private static void Sort(IList<ArcGraphVegetationRenderItem> target)
        {
            if (target == null || target.Count <= 1)
                return;

            if (target is List<ArcGraphVegetationRenderItem> list)
            {
                list.Sort(CompareItems);
                return;
            }

            var copy = new List<ArcGraphVegetationRenderItem>(target);
            copy.Sort(CompareItems);

            target.Clear();
            for (int i = 0; i < copy.Count; i++)
                target.Add(copy[i]);
        }

        private static int CompareItems(
            ArcGraphVegetationRenderItem left,
            ArcGraphVegetationRenderItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }
    }
}
