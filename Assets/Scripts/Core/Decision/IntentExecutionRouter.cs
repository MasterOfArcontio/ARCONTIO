using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // IntentExecutionRouteKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classifica il tipo di route esecutiva ottenuta da una intenzione selezionata.
    /// </para>
    ///
    /// <para><b>Diagnostica passiva del boundary</b></para>
    /// <para>
    /// L'enum non rappresenta una decisione di JobArbiter e non implica assignment.
    /// Serve solo a rendere leggibile il risultato del boundary
    /// SelectedDecision -> JobRequest.
    /// </para>
    /// </summary>
    public enum IntentExecutionRouteKind
    {
        None = 0,
        EatKnownFoodJobRequest = 10,
        SearchFoodJobRequest = 20
    }

    // =============================================================================
    // IntentExecutionRouteResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato passivo del tentativo di tradurre una intenzione selezionata in
    /// <c>JobRequest</c>.
    /// </para>
    ///
    /// <para><b>Route, non esecuzione</b></para>
    /// <para>
    /// Il result puo' contenere una richiesta job, ma non contiene job assegnati,
    /// command, preemption o fallback legacy. La sua presenza non significa che il
    /// Job Layer accettera' la richiesta.
    /// </para>
    /// </summary>
    public readonly struct IntentExecutionRouteResult
    {
        public readonly bool HasJobRequest;
        public readonly IntentExecutionRouteKind Kind;
        public readonly JobRequest Request;
        public readonly string Reason;

        public IntentExecutionRouteResult(
            bool hasJobRequest,
            IntentExecutionRouteKind kind,
            JobRequest request,
            string reason)
        {
            HasJobRequest = hasJobRequest;
            Kind = kind;
            Request = request;
            Reason = reason ?? string.Empty;
        }

        public static IntentExecutionRouteResult Rejected(IntentExecutionRouteKind kind, string reason)
        {
            return new IntentExecutionRouteResult(false, kind, default, reason);
        }

        public static IntentExecutionRouteResult Accepted(IntentExecutionRouteKind kind, JobRequest request, string reason)
        {
            return new IntentExecutionRouteResult(true, kind, request, reason);
        }
    }

    // =============================================================================
    // IntentExecutionRouter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Router sottile tra intenzione selezionata e richiesta esecutiva job.
    /// </para>
    ///
    /// <para><b>ARC-DEC-019 - boundary senza preemption authority</b></para>
    /// <para>
    /// Il router puo' proporre un <c>JobRequest</c>, ma non puo' accettarlo,
    /// rifiutarlo in nome del Job Layer, assegnarlo, cancellare job attivi o
    /// preemptare. L'autorita' runtime resta in <c>JobRuntimeState</c> /
    /// <c>JobArbiter</c>.
    /// </para>
    ///
    /// <para><b>World non ammesso come conoscenza cognitiva</b></para>
    /// <para>
    /// Il router non riceve <c>World</c>. Eventuali risoluzioni legacy ancora basate
    /// su helper runtime restano nel bridge transitorio e arrivano qui solo come
    /// valori gia' risolti.
    /// </para>
    /// </summary>
    public sealed class IntentExecutionRouter
    {
        private readonly JobRequestBuilder _jobRequestBuilder;

        public IntentExecutionRouter()
            : this(new JobRequestBuilder())
        {
        }

        public IntentExecutionRouter(JobRequestBuilder jobRequestBuilder)
        {
            _jobRequestBuilder = jobRequestBuilder ?? new JobRequestBuilder();
        }

        // =============================================================================
        // TryRouteEatKnownFood
        // =============================================================================
        /// <summary>
        /// <para>
        /// Tenta la route dati <c>EatKnownFood -> JobRequest</c>.
        /// </para>
        /// </summary>
        public bool TryRouteEatKnownFood(
            int tick,
            int npcId,
            DecisionCandidate selectedCandidate,
            int targetObjectId,
            out IntentExecutionRouteResult result)
        {
            bool built = _jobRequestBuilder.TryBuildEatKnownFoodRequest(
                tick,
                npcId,
                selectedCandidate,
                targetObjectId,
                out var request,
                out string reason);

            result = built
                ? IntentExecutionRouteResult.Accepted(IntentExecutionRouteKind.EatKnownFoodJobRequest, request, reason)
                : IntentExecutionRouteResult.Rejected(IntentExecutionRouteKind.EatKnownFoodJobRequest, reason);

            return built;
        }

        // =============================================================================
        // TryRouteSearchFood
        // =============================================================================
        /// <summary>
        /// <para>
        /// Tenta la route dati <c>SearchFood -> JobRequest</c>.
        /// </para>
        /// </summary>
        public bool TryRouteSearchFood(
            int tick,
            int npcId,
            DecisionCandidate selectedCandidate,
            Vector2Int targetCell,
            out IntentExecutionRouteResult result)
        {
            bool built = _jobRequestBuilder.TryBuildSearchFoodRequest(
                tick,
                npcId,
                selectedCandidate,
                targetCell,
                out var request,
                out string reason);

            result = built
                ? IntentExecutionRouteResult.Accepted(IntentExecutionRouteKind.SearchFoodJobRequest, request, reason)
                : IntentExecutionRouteResult.Rejected(IntentExecutionRouteKind.SearchFoodJobRequest, reason);

            return built;
        }
    }
}
