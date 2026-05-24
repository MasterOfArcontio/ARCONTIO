using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // SearchFoodJobFactory
    // =============================================================================
    /// <summary>
    /// <para>
    /// Factory minimale per trasformare una intenzione <c>SearchFood</c> gia'
    /// selezionata dal Decision Layer in un job locale di esplorazione fisica.
    /// </para>
    ///
    /// <para><b>SearchFood come probe locale, non telepatia</b></para>
    /// <para>
    /// Questa factory non cerca cibo, non legge <c>World.Objects</c>, non legge
    /// <c>FoodStocks</c> e non aggiorna memoria o belief. Riceve un
    /// <c>JobRequest</c> gia' costruito dal bridge transitorio, materializza il
    /// template <c>food.search_local_probe.v1</c> e produce un <c>Job</c> che si
    /// limita a muovere l'NPC verso una cella locale. La scoperta di cibo resta
    /// responsabilita' dei sistemi successivi di movimento, percezione, memoria e
    /// belief.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Validazione request</b>: accetta solo <c>SearchFood</c> con target cell locale e senza target object.</item>
    ///   <item><b>Template</b>: usa solo <c>JobTemplateRegistry.SearchFoodLocalProbeTemplateId</c>.</item>
    ///   <item><b>Job</b>: conserva il <c>JobRequest</c> ricevuto senza ricostruire semantica decisionale.</item>
    /// </list>
    /// </summary>
    public static class SearchFoodJobFactory
    {
        // =============================================================================
        // TryCreateSearchFoodLocalProbeJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un job SearchFood locale a partire da un <c>JobRequest</c>
        /// pre-costruito e gia' targettizzato su una probe cell.
        /// </para>
        ///
        /// <para><b>Command Authority preservata</b></para>
        /// <para>
        /// Il metodo non assegna il job, non emette <c>ICommand</c> e non muta il
        /// <c>World</c>. La factory resta data-pura: se la richiesta e' valida,
        /// chiede al registry di materializzare il piano e restituisce l'istanza
        /// runtime assegnabile a <c>JobRuntimeState</c>.
        /// </para>
        /// </summary>
        public static bool TryCreateSearchFoodLocalProbeJob(
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

            if (request.IntentKind != DecisionIntentKind.SearchFood)
            {
                reason = "InvalidSearchFoodJobIntent";
                return false;
            }

            if (!request.HasTargetCell)
            {
                reason = "MissingSearchFoodProbeCell";
                return false;
            }

            if (request.TargetObjectId != 0)
            {
                reason = "SearchFoodMustNotTargetObject";
                return false;
            }

            if (!registry.TryBuildPlan(JobTemplateRegistry.SearchFoodLocalProbeTemplateId, request, out var plan, out reason))
                return false;

            job = new Job($"job_search_food_probe_{request.NpcId}_{request.TargetCell.x}_{request.TargetCell.y}_{request.CreatedTick}", request, plan);
            reason = "SearchFoodJobCreated";
            return true;
        }
    }
}
