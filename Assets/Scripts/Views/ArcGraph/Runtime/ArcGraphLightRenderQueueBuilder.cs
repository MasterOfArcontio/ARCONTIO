using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphLightRenderQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot luce ArcGraph in item di tinta
    /// ordinabili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: light renderer senza LightSystem</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphLightLayer</c>, il contratto ambientale
    /// visuale e un profilo LOD gia' risolto. Produce item value-only per un futuro
    /// renderer, ma non calcola propagazione, ombre, buio stanza, occlusione,
    /// attenuazione da muri, visibilita' percettiva o luci Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola una lista di item luce ordinati.</item>
    ///   <item><b>CreateItem</b>: converte snapshot + LOD + contratto in item value-only.</item>
    ///   <item><b>ResolveTintKey</b>: produce chiave tinta testuale, non materiale reale.</item>
    ///   <item><b>Sort</b>: ordina in modo deterministico via sort key.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphLightRenderQueueBuilder
    {
        private const int LightVisualLayerOrder = 90;
        private const float DarkCellThreshold = 0.25f;
        private const float NeutralIntensity = 1f;
        private readonly List<ArcGraphLightVisualSnapshot> _snapshotBuffer = new();

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue luce a partire dal layer luce ArcGraph.
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
        ///   <item><b>lightLayer</b>: cache snapshot da leggere.</item>
        ///   <item><b>lodProfile</b>: policy visuale gia' risolta.</item>
        ///   <item><b>target</b>: lista render item da popolare.</item>
        ///   <item><b>includeHiddenItems</b>: se true, conserva anche item nascosti.</item>
        /// </list>
        /// </summary>
        public ArcGraphLightRenderQueueDiagnostics Build(
            ArcGraphLightLayer lightLayer,
            ArcGraphZoomLodProfile lodProfile,
            IList<ArcGraphLightRenderItem> target,
            bool clearTarget = true,
            bool includeHiddenItems = false)
        {
            if (target == null)
            {
                return new ArcGraphLightRenderQueueDiagnostics(
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

            if (lightLayer == null)
            {
                return new ArcGraphLightRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0f,
                    "LightLayerMissing");
            }

            if (!ArcGraphEnvironmentVisualContractCatalog.TryGetDefaultContract(
                    ArcGraphLayerId.Light,
                    out var contract))
            {
                return new ArcGraphLightRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0f,
                    "LightContractMissing");
            }

            _snapshotBuffer.Clear();
            lightLayer.CopySnapshotsTo(_snapshotBuffer);

            int visibleCount = 0;
            int hiddenCount = 0;
            int localSourceCount = 0;
            int darkCellCount = 0;
            float maxIntensity = 0f;

            for (int i = 0; i < _snapshotBuffer.Count; i++)
            {
                ArcGraphLightRenderItem item = CreateItem(
                    _snapshotBuffer[i],
                    lodProfile,
                    contract);

                if (item.Intensity01 > maxIntensity)
                    maxIntensity = item.Intensity01;

                if (item.IsVisible)
                    visibleCount++;
                else
                    hiddenCount++;

                if (item.IsVisible && item.HasLocalSource)
                    localSourceCount++;

                if (item.IsVisible && item.Intensity01 <= DarkCellThreshold)
                    darkCellCount++;

                if (item.IsVisible || includeHiddenItems)
                    target.Add(item);
            }

            Sort(target);

            return new ArcGraphLightRenderQueueDiagnostics(
                _snapshotBuffer.Count,
                visibleCount,
                hiddenCount,
                localSourceCount,
                darkCellCount,
                maxIntensity,
                "LightQueueBuilt");
        }

        private static ArcGraphLightRenderItem CreateItem(
            ArcGraphLightVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile,
            ArcGraphEnvironmentVisualLayerContract contract)
        {
            bool isVisible = true;
            string hiddenReason = "None";

            if (!contract.IsEnvironmentLayer || contract.LayerId != ArcGraphLayerId.Light)
            {
                isVisible = false;
                hiddenReason = "InvalidLightContract";
            }
            else if (!contract.RequiresExternalSnapshots)
            {
                isVisible = false;
                hiddenReason = "LightMustUseExternalSnapshots";
            }
            else if (!contract.AllowsLocalTint)
            {
                isVisible = false;
                hiddenReason = "LightLocalTintNotAllowed";
            }

            string tintKey = isVisible
                ? ResolveTintKey(snapshot, lodProfile)
                : string.Empty;

            if (isVisible && IsNeutral(snapshot, tintKey))
            {
                isVisible = false;
                hiddenReason = "NeutralLight";
            }

            var sortKey = ArcGraphRenderSortKey.FromCell(
                snapshot.Cell,
                LightVisualLayerOrder,
                ArcGraphRenderItemKind.Light,
                ResolveStableEntityId(snapshot));

            return new ArcGraphLightRenderItem(
                snapshot.Cell,
                snapshot.Intensity01,
                tintKey,
                snapshot.HasLocalSource,
                contract.AllowsGlobalOverlay,
                contract.AllowsLocalTint,
                lodProfile.UsesSimplifiedRepresentation,
                isVisible,
                hiddenReason,
                sortKey);
        }

        private static string ResolveTintKey(
            ArcGraphLightVisualSnapshot snapshot,
            ArcGraphZoomLodProfile lodProfile)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.TintKey))
                return snapshot.TintKey.Trim();

            if (snapshot.HasLocalSource)
            {
                return lodProfile.UsesSimplifiedRepresentation
                    ? "ArcGraph/Light/Simple/local"
                    : "ArcGraph/Light/local";
            }

            if (snapshot.Intensity01 <= DarkCellThreshold)
            {
                return lodProfile.UsesSimplifiedRepresentation
                    ? "ArcGraph/Light/Simple/dark"
                    : "ArcGraph/Light/dark";
            }

            if (snapshot.Intensity01 < NeutralIntensity)
            {
                return lodProfile.UsesSimplifiedRepresentation
                    ? "ArcGraph/Light/Simple/dim"
                    : "ArcGraph/Light/dim";
            }

            return string.Empty;
        }

        private static bool IsNeutral(
            ArcGraphLightVisualSnapshot snapshot,
            string tintKey)
        {
            return snapshot.Intensity01 >= NeutralIntensity
                   && !snapshot.HasLocalSource
                   && string.IsNullOrWhiteSpace(tintKey);
        }

        private static int ResolveStableEntityId(ArcGraphLightVisualSnapshot snapshot)
        {
            unchecked
            {
                int hash = 23;
                hash = (hash * 31) + snapshot.Cell.X;
                hash = (hash * 31) + snapshot.Cell.Y;
                hash = (hash * 31) + snapshot.Cell.Z;
                hash = (hash * 31) + (snapshot.HasLocalSource ? 1 : 0);
                hash = (hash * 31) + (int)(snapshot.Intensity01 * 1000f);
                return hash & int.MaxValue;
            }
        }

        private static void Sort(IList<ArcGraphLightRenderItem> target)
        {
            if (target == null || target.Count <= 1)
                return;

            if (target is List<ArcGraphLightRenderItem> list)
            {
                list.Sort(CompareItems);
                return;
            }

            var copy = new List<ArcGraphLightRenderItem>(target);
            copy.Sort(CompareItems);

            target.Clear();
            for (int i = 0; i < copy.Count; i++)
                target.Add(copy[i]);
        }

        private static int CompareItems(
            ArcGraphLightRenderItem left,
            ArcGraphLightRenderItem right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }
    }
}
