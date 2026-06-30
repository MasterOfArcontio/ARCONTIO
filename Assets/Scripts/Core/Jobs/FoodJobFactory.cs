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
    /// <para><b>Decision -> JobRequest -> Job senza nuova onniscienza</b></para>
    /// <para>
    /// La factory non cerca cibo nel mondo. Nel path MBQD hardenizzato riceve un
    /// <c>JobRequest</c> gia' costruito dal Legacy Transitional Decision Bridge e
    /// chiede al registry JSON il piano dichiarativo. L'overload storico resta
    /// disponibile solo per compatibilita' dei test e dei chiamanti v0.11B, ma delega
    /// al nuovo overload per evitare due semantiche parallele.
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
        // =============================================================================
        // TryCreateCarriedInventoryFoodJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il job food che consuma cibo gia' presente nell'inventario dell'NPC.
        /// </para>
        ///
        /// <para><b>Inventario come target implicito autorizzato</b></para>
        /// <para>
        /// La factory non sceglie quale oggetto mangiare e non legge il World. Verifica
        /// solo che la request rappresenti <c>EatCarriedFood</c> e materializza il
        /// template dedicato. La scelta dell'entry alimentare resta nello step runtime
        /// che passa dal gateway del World.
        /// </para>
        /// </summary>
        public static bool TryCreateCarriedInventoryFoodJob(
            JobTemplateRegistry registry,
            JobRequest request,
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

            if (request.NpcId <= 0)
            {
                reason = "InvalidNpcId";
                return false;
            }

            if (request.IntentKind != DecisionIntentKind.EatCarriedFood)
            {
                reason = "InvalidCarriedFoodJobIntent";
                return false;
            }

            if (!registry.TryBuildPlan(JobTemplateRegistry.FoodCarriedInventoryTemplateId, request, out var plan, out reason))
                return false;

            job = new Job($"job_food_carried_{request.NpcId}_{request.CreatedTick}", request, plan);
            reason = "CarriedFoodJobCreated";
            return true;
        }

        // =============================================================================
        // TryCreateKnownCommunityFoodJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il job food a partire da un <c>JobRequest</c> gia' pre-costruito dal
        /// boundary Decision -> JobRequest.
        /// </para>
        ///
        /// <para><b>Command Authority preservata</b></para>
        /// <para>
        /// Questo overload non emette <c>ICommand</c>, non assegna il job e non muta
        /// il <c>World</c>. Materializza soltanto il piano food dal template esistente:
        /// l'esecuzione resta a <c>JobRuntimeState</c>, <c>JobExecutionSystem</c>,
        /// <c>JobCommandBuffer</c> e al command pump finale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione request</b>: intent, NPC, target object e target cell devono essere coerenti.</item>
        ///   <item><b>Template</b>: usa solo <c>food.eat_known_community_stock.v1</c>.</item>
        ///   <item><b>Job</b>: conserva il <c>JobRequest</c> ricevuto senza ricostruire semantica decisionale.</item>
        /// </list>
        /// </summary>
        public static bool TryCreateKnownCommunityFoodJob(
            JobTemplateRegistry registry,
            JobRequest request,
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

            if (request.NpcId <= 0)
            {
                reason = "InvalidNpcId";
                return false;
            }

            if (request.IntentKind != DecisionIntentKind.EatKnownFood)
            {
                reason = "InvalidFoodJobIntent";
                return false;
            }

            if (!request.HasTargetCell)
            {
                reason = "InvalidFoodJobTarget";
                return false;
            }

            if (!registry.TryBuildPlan(JobTemplateRegistry.FoodKnownCommunityStockTemplateId, request, out var plan, out reason))
                return false;

            job = new Job($"job_food_{request.NpcId}_{request.TargetObjectId}_{request.CreatedTick}", request, plan);
            reason = "FoodJobCreated";
            return true;
        }

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

            return TryCreateKnownCommunityFoodJob(registry, request, out job, out reason);
        }
    }
}
