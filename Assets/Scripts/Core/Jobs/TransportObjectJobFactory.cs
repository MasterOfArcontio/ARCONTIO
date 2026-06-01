using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // TransportObjectJobFactory
    // =============================================================================
    /// <summary>
    /// <para>
    /// Factory minimale per costruire il job debug di trasporto oggetto verso cella.
    /// </para>
    ///
    /// <para><b>DevTools forced injection, Job System reale</b></para>
    /// <para>
    /// La factory non decide quale oggetto trasportare e non interroga il World. Riceve
    /// NPC, oggetto, cella origine oggetto e cella destinazione gia' risolti dalla UI
    /// debug, poi materializza il template JSON <c>transport.object_to_cell.v1</c>.
    /// L'esecuzione successiva resta nel <c>JobExecutionSystem</c>, che produce
    /// commands invece di mutare direttamente il mondo. Le route tecniche necessarie
    /// al movimento multi-tick vengono ammesse solo come eccezione dev dichiarata
    /// dal debug label <c>DevToolsForcedTransportObject</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Request</b>: conserva target object id, cella oggetto e cella destinazione.</item>
    ///   <item><b>Template</b>: sequenza MoveToCell, PickUp, MoveToCell, Drop.</item>
    ///   <item><b>Job</b>: istanza runtime assegnabile allo store posseduto dal World.</item>
    /// </list>
    /// </summary>
    public static class TransportObjectJobFactory
    {
        public static bool TryCreateTransportObjectToCellJob(
            JobTemplateRegistry registry,
            int npcId,
            int objectId,
            Vector2Int objectCell,
            Vector2Int destinationCell,
            int tick,
            out Job job,
            out string reason)
        {
            job = null;
            reason = string.Empty;

            if (registry == null)
            {
                reason = "RegistryMissing";
                return false;
            }

            if (npcId <= 0 || objectId <= 0)
            {
                reason = "InvalidTransportTarget";
                return false;
            }

            var request = new JobRequest(
                $"jobreq_transport_object_{npcId}_{objectId}_{tick}",
                npcId,
                DecisionIntentKind.ExploreArea,
                JobPriorityClass.Emergency,
                1f,
                tick,
                true,
                objectCell,
                objectId,
                string.Empty,
                "DevToolsForcedTransportObject",
                true,
                destinationCell);

            if (!registry.TryBuildPlan(JobTemplateRegistry.TransportObjectToCellTemplateId, request, out var plan, out reason))
                return false;

            job = new Job($"job_transport_object_{npcId}_{objectId}_{tick}", request, plan);
            reason = "TransportObjectJobCreated";
            return true;
        }
    }
}
