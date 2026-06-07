using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRenderQueueBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che combina actor e oggetti in una queue globale ordinata.
    /// </para>
    ///
    /// <para><b>Principio architetturale: composizione di renderer passivi</b></para>
    /// <para>
    /// Il builder orchestra i builder actor/object gia' passivi e costruisce una
    /// lista globale di entry. Non introduce un manager grafico onnisciente: non
    /// legge <c>World</c>, non carica asset, non crea <c>GameObject</c>, non decide
    /// cosa esista nella simulazione. Coordina soltanto payload visuali gia'
    /// presenti nei layer ArcGraph.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: popola una <c>ArcGraphRenderQueue</c>.</item>
    ///   <item><b>BuildEntries</b>: crea l'ordine globale actor/object.</item>
    ///   <item><b>CompareEntries</b>: sorting deterministico condiviso.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRenderQueueBuilder
    {
        private readonly ArcGraphActorRenderQueueBuilder _actorBuilder = new();
        private readonly ArcGraphObjectRenderQueueBuilder _objectBuilder = new();

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una queue actor/object completa.
        /// </para>
        ///
        /// <para><b>Flusso controllato</b></para>
        /// <para>
        /// La queue viene prima pulita, poi popolata con actor e oggetti visibili.
        /// Gli item nascosti restano contati dalle diagnostiche dei singoli builder,
        /// ma non entrano nell'ordine globale salvo futura richiesta esplicita.
        /// </para>
        /// </summary>
        public ArcGraphRenderQueueDiagnostics Build(
            ArcGraphActorLayer actorLayer,
            ArcGraphObjectLayer objectLayer,
            ArcGraphZoomLodProfile lodProfile,
            ArcGraphRenderQueue queue)
        {
            if (queue == null)
            {
                return new ArcGraphRenderQueueDiagnostics(
                    0,
                    0,
                    0,
                    0,
                    "QueueMissing");
            }

            queue.Clear();

            ArcGraphRenderQueueDiagnostics actorDiagnostics = _actorBuilder.Build(
                actorLayer,
                lodProfile,
                queue.MutableActorItems);

            ArcGraphRenderQueueDiagnostics objectDiagnostics = _objectBuilder.Build(
                objectLayer,
                lodProfile,
                queue.MutableObjectItems);

            BuildEntries(queue);

            int visible = actorDiagnostics.VisibleItemCount + objectDiagnostics.VisibleItemCount;
            int hidden = actorDiagnostics.HiddenItemCount + objectDiagnostics.HiddenItemCount;

            var diagnostics = new ArcGraphRenderQueueDiagnostics(
                actorDiagnostics.ActorItemCount,
                objectDiagnostics.ObjectItemCount,
                visible,
                hidden,
                "CombinedQueueBuilt");

            queue.SetDiagnostics(diagnostics);
            return diagnostics;
        }

        private static void BuildEntries(ArcGraphRenderQueue queue)
        {
            List<ArcGraphRenderQueueEntry> entries = queue.MutableEntries;
            entries.Clear();

            IReadOnlyList<ArcGraphObjectRenderItem> objects = queue.ObjectItems;
            for (int i = 0; i < objects.Count; i++)
            {
                entries.Add(ArcGraphRenderQueueEntry.ForObject(i, objects[i]));
            }

            IReadOnlyList<ArcGraphActorRenderItem> actors = queue.ActorItems;
            for (int i = 0; i < actors.Count; i++)
            {
                entries.Add(ArcGraphRenderQueueEntry.ForActor(i, actors[i]));
            }

            entries.Sort(CompareEntries);
        }

        private static int CompareEntries(
            ArcGraphRenderQueueEntry left,
            ArcGraphRenderQueueEntry right)
        {
            return left.SortKey.CompareTo(right.SortKey);
        }
    }
}
