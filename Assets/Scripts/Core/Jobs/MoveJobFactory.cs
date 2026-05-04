using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MoveJobFactory
    // =============================================================================
    /// <summary>
    /// <para>
    /// Factory minimale per costruire un job tecnico di solo movimento verso una
    /// cella gia' scelta da un chiamante esterno.
    /// </para>
    ///
    /// <para><b>Generic Move Job senza planner</b></para>
    /// <para>
    /// La factory non introduce pathfinding, query o decisioni. Riceve una cella
    /// target esplicita, materializza il template JSON <c>generic.move_to_cell.v1</c>
    /// e produce un <c>Job</c> che il <c>JobExecutionSystem</c> traduce in
    /// <c>SetMoveIntentCommand</c>. Il MovementSystem resta l'unico responsabile
    /// della navigazione concreta.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Request</b>: contiene NPC, cella target e urgenza diagnostica.</item>
    ///   <item><b>Template</b>: usa una sola fase con una sola action MoveToCell.</item>
    ///   <item><b>Job</b>: istanza runtime assegnabile allo store posseduto dal World.</item>
    /// </list>
    /// </summary>
    public static class MoveJobFactory
    {
        public static bool TryCreateMoveToCellJob(
            JobTemplateRegistry registry,
            int npcId,
            Vector2Int targetCell,
            int tick,
            float urgency01,
            string debugLabel,
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

            if (npcId <= 0)
            {
                reason = "InvalidNpcId";
                return false;
            }

            var request = new JobRequest(
                $"jobreq_move_{npcId}_{targetCell.x}_{targetCell.y}_{tick}",
                npcId,
                DecisionIntentKind.ExploreArea,
                urgency01 >= 0.75f ? JobPriorityClass.Important : JobPriorityClass.Normal,
                urgency01,
                tick,
                true,
                targetCell,
                0,
                string.Empty,
                string.IsNullOrWhiteSpace(debugLabel) ? "GenericMoveJobRoute" : debugLabel);

            if (!registry.TryBuildPlan(JobTemplateRegistry.GenericMoveToCellTemplateId, request, out var plan, out reason))
                return false;

            job = new Job($"job_move_{npcId}_{targetCell.x}_{targetCell.y}_{tick}", request, plan);
            reason = "MoveJobCreated";
            return true;
        }
    }
}
