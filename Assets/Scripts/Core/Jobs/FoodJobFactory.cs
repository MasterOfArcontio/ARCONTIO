using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // FoodJobFactory
    // =============================================================================
    /// <summary>
    /// <para>
    /// Factory minimale per trasformare un target food gia' conosciuto in un job
    /// eseguibile dalla prima vertical slice v0.11.01.
    /// </para>
    ///
    /// <para><b>Decision -> Job senza nuova onniscienza</b></para>
    /// <para>
    /// La factory non cerca cibo nel mondo. Riceve un objectId e una cella gia'
    /// scelti dal ponte decisionale, costruisce una <c>JobRequest</c> e chiede al
    /// registry JSON il piano dichiarativo. In questo modo la conoscenza resta nel
    /// decision layer legacy/transitorio e la factory resta solo un adattatore.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Request</b>: contiene NPC, target cella e target object id.</item>
    ///   <item><b>Template</b>: usa il template food minimale dal registry JSON.</item>
    ///   <item><b>Job</b>: istanza runtime assegnabile a <c>JobRuntimeState</c>.</item>
    /// </list>
    /// </summary>
    public static class FoodJobFactory
    {
        public static bool TryCreateKnownCommunityFoodJob(
            JobTemplateRegistry registry,
            int npcId,
            int foodObjectId,
            Vector2Int targetCell,
            int tick,
            float urgency01,
            string beliefKey,
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

            if (npcId <= 0 || foodObjectId <= 0)
            {
                reason = "InvalidFoodJobTarget";
                return false;
            }

            var request = new JobRequest(
                $"jobreq_food_{npcId}_{foodObjectId}_{tick}",
                npcId,
                DecisionIntentKind.EatKnownFood,
                urgency01 >= 0.85f ? JobPriorityClass.Critical : JobPriorityClass.Important,
                urgency01,
                tick,
                true,
                targetCell,
                foodObjectId,
                beliefKey,
                "FoodJobVerticalSlice");

            if (!registry.TryBuildPlan(JobTemplateRegistry.FoodKnownCommunityStockTemplateId, request, out var plan, out reason))
                return false;

            job = new Job($"job_food_{npcId}_{foodObjectId}_{tick}", request, plan);
            reason = "FoodJobCreated";
            return true;
        }
    }
}
