using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEffectRenderQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot effetto ArcGraph in render item
    /// ordinabili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: effetti renderizzabili senza EffectSystem</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphEffectLayer</c>, il contratto ambientale
    /// visuale e un profilo LOD gia' risolto. Produce item value-only per un futuro
    /// renderer, ma non propaga fuoco, non genera fumo, non decide danni, non
    /// accende luci Unity, non crea particelle e non modifica celle della mappa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola una lista di item effetto ordinati.</item>
    ///   <item><b>CreateItem</b>: converte snapshot + LOD + contratto in item value-only.</item>
    ///   <item><b>ResolveSpriteKey</b>: produce chiave testuale, non asset reale.</item>
    ///   <item><b>Sort</b>: ordina in modo deterministico via sort key.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphEffectRenderQueueBuilder
    {
        private const int EffectVisualLayerOrder = 80;
        private readonly List<ArcGraphEffectVisualSnapshot> _snapshotBuffer = new();

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue effetti a partire dal layer effetti ArcGraph.
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
        ///   <item><b>effectLayer</b>: cache snapshot da leggere.</item>
        ///   <item><b>lodProfile</b>: policy visuale gia' risolta.</item>
        ///   <item><b>target</b>: lista render item da popolare.</item>
        ///   <item><b>includeHiddenItems</b>: se true, conserva anche item nascosti.</item>
        /// </list>
        /// </summary>
        public ArcGraphEffectRenderQueueDiagnostics Build(
            ArcGraphEffectLayer effectLayer,
            ArcGraphZoomLodProfile lodProfile,
            IList<ArcGraphEffectRenderItem> target,
            bool clearTarget = true,
            bool includeHiddenItems = false)
        {
            if (target == null)
            {
                return new ArcGraphEffectRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0f,
                    "TargetMissing");
            }

            if (clearTarget)
                target.Clear();

            if (effectLayer == null)
            {
                return new ArcGraphEffectRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0f,
                    "EffectLayerMissing");
            }

            if (!ArcGraphEnvironmentVisualContractCatalog.TryGetDefaultContract(
                    ArcGraphLayerId.Effect,
                    out var contract))
            {
                return new ArcGraphEffectRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0f,
                    "EffectContractMissing");
            }

            _snapshotBuffer.Clear();
            effectLayer.CopySnapshotsTo(_snapshotBuffer);

            int visibleCount = 0;
            int hiddenCount = 0;
            int animatedCount = 0;
            int staticSignalCount = 0;
            float maxIntensity = 0f;

            for (int i = 0; i < _snapshotBuffer.Count; i++)
            {
                ArcGraphEffectRenderItem item = CreateItem(
                    _snapshotBuffer[i],
                    lodProfile,
                    contract);

                if (item.Intensity01 > maxIntensity)
                    maxIntensity = item.Intensity01;

                if (item.IsVisible)
                    visibleCount++;
                else
                    hiddenCount++;

                if (item.IsVisible && item.IsAnimated)
                    animatedCount++;

                if (item.IsVisible && item.EffectMode == ArcGraphEffectLodMode.StaticSignalOnly)
                    staticSignalCount++;

                if (item.IsVisible || includeHiddenItems)
                    target.Add(item);
            }

            Sort(target);

            return new ArcGraphEffectRenderQueueDiagnostics(
                _snapshotBuffer.Count,
                visibleCount,
                hiddenCount,
                animatedCount,
                staticSignalCount,
                maxIntensity,
                "EffectQueueBuilt");
        }

        private static ArcGraphEffectRenderItem CreateItem(
            ArcGraphEffectVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile,
            ArcGraphEnvironmentVisualLayerContract contract)
        {
            bool isVisible = true;
            string hiddenReason = "None";

            if (!contract.IsEnvironmentLayer || contract.LayerId != ArcGraphLayerId.Effect)
            {
                isVisible = false;
                hiddenReason = "InvalidEffectContract";
            }
            else if (!contract.RequiresExternalSnapshots)
            {
                isVisible = false;
                hiddenReason = "EffectMustUseExternalSnapshots";
            }
            else if (snapshot.EffectId <= 0)
            {
                isVisible = false;
                hiddenReason = "InvalidEffectId";
            }
            else if (string.IsNullOrWhiteSpace(snapshot.EffectKey))
            {
                isVisible = false;
                hiddenReason = "MissingEffectKey";
            }
            else if (snapshot.Intensity01 <= 0f)
            {
                isVisible = false;
                hiddenReason = "EmptyEffectIntensity";
            }

            ArcGraphEffectLodMode resolvedMode = ResolveEffectMode(snapshot, lodProfile);
            bool allowsAnimation = AllowsAnimation(snapshot, lodProfile, contract, resolvedMode);

            string spriteKey = isVisible
                ? ResolveSpriteKey(snapshot, resolvedMode)
                : string.Empty;

            if (isVisible && string.IsNullOrWhiteSpace(spriteKey))
            {
                isVisible = false;
                hiddenReason = "MissingEffectSpriteKey";
            }

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.Cell,
                EffectVisualLayerOrder,
                ArcGraphRenderItemKind.Effect,
                snapshot.EffectId);

            return new ArcGraphEffectRenderItem(
                snapshot.EffectId,
                snapshot.Cell,
                snapshot.EffectKey,
                snapshot.Intensity01,
                spriteKey,
                resolvedMode,
                allowsAnimation,
                contract.AllowsLocalTint,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static ArcGraphEffectLodMode ResolveEffectMode(
            ArcGraphEffectVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile)
        {
            return lodProfile.EffectMode;
        }

        private static bool AllowsAnimation(
            ArcGraphEffectVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile,
            ArcGraphEnvironmentVisualLayerContract contract,
            ArcGraphEffectLodMode resolvedMode)
        {
            if (!contract.AllowsArcGraphSpriteAnimation || !lodProfile.AllowsSpriteAnimation)
                return false;

            if (snapshot.Intensity01 <= 0f)
                return false;

            return resolvedMode == ArcGraphEffectLodMode.AnimatedMajorEffects
                   || resolvedMode == ArcGraphEffectLodMode.FullLocalEffects;
        }

        private static string ResolveSpriteKey(
            ArcGraphEffectVisualSnapshot snapshot,
            ArcGraphEffectLodMode resolvedMode)
        {
            string effect = SanitizeKeyPart(snapshot.EffectKey);

            if (resolvedMode == ArcGraphEffectLodMode.StaticSignalOnly)
                return "ArcGraph/Effect/Signal/" + effect;

            if (resolvedMode == ArcGraphEffectLodMode.AnimatedMajorEffects)
                return "ArcGraph/Effect/AnimatedMajor/" + effect;

            return "ArcGraph/Effect/Full/" + effect;
        }

        private static string SanitizeKeyPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            return value.Trim().Replace(' ', '_');
        }

        private static void Sort(IList<ArcGraphEffectRenderItem> target)
        {
            if (target == null || target.Count <= 1)
                return;

            if (target is List<ArcGraphEffectRenderItem> list)
            {
                list.Sort(CompareItems);
                return;
            }

            var copy = new List<ArcGraphEffectRenderItem>(target);
            copy.Sort(CompareItems);

            target.Clear();
            for (int i = 0; i < copy.Count; i++)
                target.Add(copy[i]);
        }

        private static int CompareItems(
            ArcGraphEffectRenderItem left,
            ArcGraphEffectRenderItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }
    }
}
