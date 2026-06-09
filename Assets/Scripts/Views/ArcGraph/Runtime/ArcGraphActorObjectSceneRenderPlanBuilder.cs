namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorObjectSceneRenderPlanBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma una <c>ArcGraphRenderQueue</c> actor/object in
    /// un piano scena materializzabile da Unity.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bridge scena senza World e senza asset</b></para>
    /// <para>
    /// Il builder non legge <c>World</c>, non consulta <c>MapGridWorldView</c>, non
    /// risolve sprite e non crea <c>GameObject</c>. Usa solo queue e contratto
    /// scene-side per calcolare posizione mondo, sorting order e richieste sprite.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola il plan scene-side.</item>
    ///   <item><b>CreateActorEntry</b>: converte un actor render item.</item>
    ///   <item><b>CreateObjectEntry</b>: converte un object render item.</item>
    ///   <item><b>ResolveSortingOrder</b>: traduce indice queue in sorting order.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphActorObjectSceneRenderPlanBuilder
    {
        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un piano scena actor/object a partire dalla queue globale.
        /// </para>
        ///
        /// <para><b>Ordine globale conservato</b></para>
        /// <para>
        /// Le entry vengono percorse nell'ordine gia' deciso da
        /// <c>ArcGraphRenderQueueBuilder</c>. Lo sorting order futuro dello
        /// <c>SpriteRenderer</c> deriva da quell'indice, non da una nuova lettura di
        /// celle, y, World o renderer legacy.
        /// </para>
        /// </summary>
        public ArcGraphActorObjectSceneRendererDiagnostics Build(
            ArcGraphRenderQueue queue,
            ArcGraphActorObjectSceneRendererContract contract,
            ArcGraphActorObjectSceneRenderPlan plan,
            bool hasSpriteResolver,
            bool clearPlan = true)
        {
            if (plan == null)
            {
                return CreateDiagnostics(
                    queue,
                    hasSpriteResolver,
                    contract,
                    0,
                    0,
                    0,
                    "PlanMissing");
            }

            if (clearPlan)
                plan.Clear();

            bool contractSafe = contract.IsTemporaryProbeSafe();
            if (!contractSafe)
            {
                var diagnostics = CreateDiagnostics(
                    queue,
                    hasSpriteResolver,
                    contract,
                    0,
                    0,
                    0,
                    "UnsafeContract");
                plan.SetDiagnostics(diagnostics);
                return diagnostics;
            }

            if (queue == null)
            {
                var diagnostics = CreateDiagnostics(
                    queue,
                    hasSpriteResolver,
                    contract,
                    0,
                    0,
                    0,
                    "QueueMissing");
                plan.SetDiagnostics(diagnostics);
                return diagnostics;
            }

            int actorEntries = 0;
            int objectEntries = 0;
            int missingSpriteKeys = 0;

            for (int i = 0; i < queue.Entries.Count; i++)
            {
                ArcGraphRenderQueueEntry queueEntry = queue.Entries[i];

                if (queueEntry.Kind == ArcGraphRenderItemKind.Actor
                    && TryCreateActorEntry(queue, queueEntry, contract, i, out var actorEntry))
                {
                    plan.MutableEntries.Add(actorEntry);
                    actorEntries++;

                    if (string.IsNullOrWhiteSpace(actorEntry.SpriteRequest.SpriteKey))
                        missingSpriteKeys++;
                }
                else if (queueEntry.Kind == ArcGraphRenderItemKind.Object
                         && TryCreateObjectEntry(queue, queueEntry, contract, i, out var objectEntry))
                {
                    plan.MutableEntries.Add(objectEntry);
                    objectEntries++;

                    if (string.IsNullOrWhiteSpace(objectEntry.SpriteRequest.SpriteKey))
                        missingSpriteKeys++;
                }
            }

            var result = CreateDiagnostics(
                queue,
                hasSpriteResolver,
                contract,
                actorEntries,
                objectEntries,
                missingSpriteKeys,
                "SceneRenderPlanBuilt");
            plan.SetDiagnostics(result);
            return result;
        }

        private static bool TryCreateActorEntry(
            ArcGraphRenderQueue queue,
            ArcGraphRenderQueueEntry queueEntry,
            ArcGraphActorObjectSceneRendererContract contract,
            int queueIndex,
            out ArcGraphActorObjectSceneRenderEntry entry)
        {
            entry = default;

            if (queueEntry.ItemIndex < 0 || queueEntry.ItemIndex >= queue.ActorItems.Count)
                return false;

            ArcGraphActorRenderItem item = queue.ActorItems[queueEntry.ItemIndex];
            if (!item.IsVisible)
                return false;

            // Gli actor usano la posa visuale gia' risolta dal builder ArcGraph.
            // Aggiungiamo 0.5 cella per mantenere la stessa convenzione del probe
            // visuale e della MapGrid: sprite centrato nella cella.
            float tileSize = contract.TileWorldSize;
            float worldX = (item.VisualX + 0.5f) * tileSize;
            float worldY = (item.VisualY + 0.5f) * tileSize;
            float worldZ = item.VisualZ * tileSize;

            var spriteRequest = new ArcGraphSpriteResolveRequest(
                ArcGraphRenderItemKind.Actor,
                item.ActorId,
                item.SpriteKey,
                string.Empty,
                item.UsesSimplifiedRepresentation);

            entry = new ArcGraphActorObjectSceneRenderEntry(
                ArcGraphRenderItemKind.Actor,
                item.ActorId,
                item.DiscreteCell,
                spriteRequest,
                worldX,
                worldY,
                worldZ,
                ResolveSortingOrder(contract, queueIndex),
                item.HasMotion,
                item.MotionProgress01);
            return true;
        }

        private static bool TryCreateObjectEntry(
            ArcGraphRenderQueue queue,
            ArcGraphRenderQueueEntry queueEntry,
            ArcGraphActorObjectSceneRendererContract contract,
            int queueIndex,
            out ArcGraphActorObjectSceneRenderEntry entry)
        {
            entry = default;

            if (queueEntry.ItemIndex < 0 || queueEntry.ItemIndex >= queue.ObjectItems.Count)
                return false;

            ArcGraphObjectRenderItem item = queue.ObjectItems[queueEntry.ItemIndex];
            if (!item.IsVisible)
                return false;

            // Gli oggetti non hanno interpolazione: vengono centrati nella cella
            // discreta gia' decisa dalla snapshot object.
            float tileSize = contract.TileWorldSize;
            float worldX = (item.Cell.X + 0.5f) * tileSize;
            float worldY = (item.Cell.Y + 0.5f) * tileSize;
            float worldZ = item.Cell.Z * tileSize;

            var spriteRequest = new ArcGraphSpriteResolveRequest(
                ArcGraphRenderItemKind.Object,
                item.ObjectId,
                item.SpriteKey,
                item.DefId,
                item.UsesSimplifiedRepresentation);

            entry = new ArcGraphActorObjectSceneRenderEntry(
                ArcGraphRenderItemKind.Object,
                item.ObjectId,
                item.Cell,
                spriteRequest,
                worldX,
                worldY,
                worldZ,
                ResolveSortingOrder(contract, queueIndex),
                hasMotion: false,
                motionProgress01: 0f);
            return true;
        }

        private static int ResolveSortingOrder(
            ArcGraphActorObjectSceneRendererContract contract,
            int queueIndex)
        {
            int safeIndex = queueIndex < 0 ? 0 : queueIndex;
            return contract.BaseSortingOrder + (safeIndex * contract.SortingOrderStep);
        }

        private static ArcGraphActorObjectSceneRendererDiagnostics CreateDiagnostics(
            ArcGraphRenderQueue queue,
            bool hasSpriteResolver,
            ArcGraphActorObjectSceneRendererContract contract,
            int actorEntries,
            int objectEntries,
            int missingSpriteKeys,
            string reason)
        {
            return new ArcGraphActorObjectSceneRendererDiagnostics(
                queue != null,
                hasSpriteResolver,
                contract.IsTemporaryProbeSafe(),
                contract.UseRenderQueueOrder,
                contract.UseActorInterpolatedPose,
                queue != null ? queue.ActorItems.Count : 0,
                queue != null ? queue.ObjectItems.Count : 0,
                queue != null ? queue.Entries.Count : 0,
                actorEntries + objectEntries,
                actorEntries,
                objectEntries,
                missingSpriteKeys,
                reason);
        }
    }
}
