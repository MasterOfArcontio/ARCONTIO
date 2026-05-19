using System;

namespace Arcontio.Core
{
    // =============================================================================
    // RunningActionExecutorResultKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classificazione diagnostica del risultato prodotto dall'executor passivo delle
    /// future running action multi-tick.
    /// </para>
    ///
    /// <para><b>Principio architetturale: risultato osservabile, non comando</b></para>
    /// <para>
    /// Questo enum descrive solo cosa e' accaduto allo stato runtime volatile. Non
    /// rappresenta mutazioni del <c>World</c>, non autorizza emissione di
    /// <c>ICommand</c> e non decide avanzamento di job, preemption o reservation.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: esito non inizializzato.</item>
    ///   <item><b>InvalidState</b>: stato assente o non processabile.</item>
    ///   <item><b>AlreadyTerminal</b>: action gia' completata/fallita/interrotta.</item>
    ///   <item><b>NoProgress</b>: richiesta valida ma senza avanzamento temporale.</item>
    ///   <item><b>Advanced</b>: elapsed interno avanzato, action ancora running.</item>
    ///   <item><b>Completed</b>: policy interna soddisfatta e lifecycle completato.</item>
    ///   <item><b>TimedOut</b>: timeout dichiarativo raggiunto e stato marcato failed.</item>
    ///   <item><b>Failed</b>: failure esplicita applicata allo stato.</item>
    ///   <item><b>Interrupted</b>: interruption esplicita applicata allo stato.</item>
    /// </list>
    /// </summary>
    public enum RunningActionExecutorResultKind
    {
        None = 0,
        InvalidState = 10,
        AlreadyTerminal = 20,
        NoProgress = 30,
        Advanced = 40,
        Completed = 50,
        TimedOut = 60,
        Failed = 70,
        Interrupted = 80
    }

    // =============================================================================
    // RunningActionExecutorTickRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta data-pura usata per far avanzare una running action volatile di un
    /// numero controllato di tick.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tick globale, nessuna timeline separata</b></para>
    /// <para>
    /// Il request contiene solo delta tick e tick globale osservato. Non introduce
    /// scheduler autonomi e non conserva riferimenti a <c>World</c>, movement,
    /// save/load o job runtime produttivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DeltaTicks</b>: progresso interno richiesto.</item>
    ///   <item><b>Tick</b>: tick globale corrente per updated tick diagnostico.</item>
    ///   <item><b>ForceFailure</b>: chiusura failed esplicita, senza side effect.</item>
    ///   <item><b>ForceInterruption</b>: chiusura interrupted esplicita, senza preemption.</item>
    ///   <item><b>Reason</b>: messaggio leggibile per QA/explainability futura.</item>
    /// </list>
    /// </summary>
    public readonly struct RunningActionExecutorTickRequest
    {
        public readonly int DeltaTicks;
        public readonly int Tick;
        public readonly bool ForceFailure;
        public readonly bool ForceInterruption;
        public readonly JobFailureReason FailureReason;
        public readonly string Reason;

        private RunningActionExecutorTickRequest(
            int deltaTicks,
            int tick,
            bool forceFailure,
            bool forceInterruption,
            JobFailureReason failureReason,
            string reason)
        {
            DeltaTicks = Math.Max(0, deltaTicks);
            Tick = Math.Max(0, tick);
            ForceFailure = forceFailure;
            ForceInterruption = forceInterruption;
            FailureReason = failureReason;
            Reason = reason ?? string.Empty;
        }

        public static RunningActionExecutorTickRequest Advance(int deltaTicks, int tick, string reason = "")
        {
            // Advance e' il path ordinario: muove solo elapsed interno.
            return new RunningActionExecutorTickRequest(
                deltaTicks,
                tick,
                forceFailure: false,
                forceInterruption: false,
                failureReason: JobFailureReason.None,
                reason);
        }

        public static RunningActionExecutorTickRequest Fail(int tick, JobFailureReason reason, string diagnosticReason = "")
        {
            // Fail rappresenta una chiusura di stato gia' decisa altrove; non
            // rilascia reservation, non chiude job e non produce fallback cognitivo.
            return new RunningActionExecutorTickRequest(
                deltaTicks: 0,
                tick: tick,
                forceFailure: true,
                forceInterruption: false,
                failureReason: reason,
                reason: diagnosticReason);
        }

        public static RunningActionExecutorTickRequest Interrupt(int tick, JobFailureReason reason, string diagnosticReason = "")
        {
            // Interrupt non equivale a preemption. Registra soltanto che lo stato
            // locale e' stato fermato da una authority esterna futura.
            return new RunningActionExecutorTickRequest(
                deltaTicks: 0,
                tick: tick,
                forceFailure: false,
                forceInterruption: true,
                failureReason: reason,
                reason: diagnosticReason);
        }
    }

    // =============================================================================
    // RunningActionExecutorResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato read-only prodotto da un tick dell'executor passivo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' senza authority</b></para>
    /// <para>
    /// Il result espone snapshot prima/dopo e reason diagnostica. Non contiene
    /// <c>ICommand</c>, non contiene <c>JobRequest</c> e non espone callback capaci di
    /// mutare il mondo. Il futuro Job Layer potra' leggerlo e decidere cosa fare, ma
    /// questo tipo non prende quella decisione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: classificazione primaria del tick executor.</item>
    ///   <item><b>Before/After</b>: snapshot difensive del progress volatile.</item>
    ///   <item><b>Reason</b>: messaggio diagnostico stabile per test e trace future.</item>
    ///   <item><b>IsTerminal</b>: comodita' read-only derivata dal kind finale.</item>
    /// </list>
    /// </summary>
    public readonly struct RunningActionExecutorResult
    {
        public readonly RunningActionExecutorResultKind Kind;
        public readonly RunningActionProgressSnapshot Before;
        public readonly RunningActionProgressSnapshot After;
        public readonly string Reason;

        public bool IsTerminal =>
            Kind == RunningActionExecutorResultKind.Completed
            || Kind == RunningActionExecutorResultKind.TimedOut
            || Kind == RunningActionExecutorResultKind.Failed
            || Kind == RunningActionExecutorResultKind.Interrupted
            || Kind == RunningActionExecutorResultKind.AlreadyTerminal;

        public RunningActionExecutorResult(
            RunningActionExecutorResultKind kind,
            RunningActionProgressSnapshot before,
            RunningActionProgressSnapshot after,
            string reason)
        {
            Kind = kind;
            Before = before;
            After = after;
            Reason = reason ?? string.Empty;
        }
    }

    // =============================================================================
    // RunningActionExecutor
    // =============================================================================
    /// <summary>
    /// <para>
    /// Executor minimale e generico per avanzare lo stato volatile di una future
    /// running action multi-tick.
    /// </para>
    ///
    /// <para><b>Principio architetturale: executor skeleton non produttivo</b></para>
    /// <para>
    /// Questo componente implementa solo la logica locale di progress/lifecycle
    /// richiesta da ARC-DEC-020: elapsed, completion, timeout, failure e
    /// interruption. Non e' cablato nel <c>JobExecutionSystem</c>, non conosce
    /// <c>MovementSystem</c>, non emette command finali, non muta il <c>World</c>,
    /// non assegna job, non arbitra preemption e non introduce reservation
    /// temporali. La futura integrazione produttiva dovra' avvenire in un task
    /// separato e coperto da policy runtime esplicita.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Guard</b>: rifiuta stato nullo o gia' terminale.</item>
    ///   <item><b>Explicit closure</b>: applica failure/interruption richieste.</item>
    ///   <item><b>Progress</b>: incrementa elapsed interno sul tick globale.</item>
    ///   <item><b>Completion</b>: chiude completed se la policy e' soddisfatta.</item>
    ///   <item><b>Timeout</b>: marca failed per timeout senza side effect esterni.</item>
    /// </list>
    /// </summary>
    public sealed class RunningActionExecutor
    {
        public RunningActionExecutorResult Tick(RunningActionRuntimeState state, RunningActionExecutorTickRequest request)
        {
            if (state == null)
                return new RunningActionExecutorResult(
                    RunningActionExecutorResultKind.InvalidState,
                    default,
                    default,
                    "RunningActionStateMissing");

            var before = state.ToSnapshot();

            if (state.IsTerminal)
                return BuildResult(RunningActionExecutorResultKind.AlreadyTerminal, before, state, "RunningActionAlreadyTerminal");

            if (request.ForceFailure)
            {
                // Failure esplicita: lo stato cambia localmente, ma nessuna risorsa
                // viene rilasciata qui e nessun job viene chiuso.
                state.MarkFailed(request.FailureReason, request.Tick);
                return BuildResult(RunningActionExecutorResultKind.Failed, before, state, ResolveReason(request, "RunningActionFailed"));
            }

            if (request.ForceInterruption)
            {
                // Interruption esplicita: registra lifecycle Interrupted senza
                // interpretarla come preemption o cancellazione job.
                state.Interrupt(request.FailureReason, request.Tick);
                return BuildResult(RunningActionExecutorResultKind.Interrupted, before, state, ResolveReason(request, "RunningActionInterrupted"));
            }

            bool advanced = state.AdvanceProgress(request.DeltaTicks, request.Tick);
            if (!advanced)
                return BuildResult(RunningActionExecutorResultKind.NoProgress, before, state, ResolveReason(request, "RunningActionNoProgress"));

            if (state.CanComplete)
            {
                // Completion vince sul timeout nello stesso tick: se la durata
                // richiesta e' stata raggiunta, lo stato interno e' completato e il
                // futuro layer produttivo decidera' quando emettere il command finale.
                state.TryMarkCompleted(request.Tick);
                return BuildResult(RunningActionExecutorResultKind.Completed, before, state, ResolveReason(request, "RunningActionCompleted"));
            }

            if (state.IsTimedOut)
            {
                // Timeout resta una transizione di stato. Non produce fallback
                // decisionale immediato e non modifica World/JobRuntimeState.
                state.MarkFailed(state.CompletionPolicy.FailureReason, request.Tick);
                return BuildResult(RunningActionExecutorResultKind.TimedOut, before, state, ResolveReason(request, "RunningActionTimedOut"));
            }

            return BuildResult(RunningActionExecutorResultKind.Advanced, before, state, ResolveReason(request, "RunningActionAdvanced"));
        }

        private static RunningActionExecutorResult BuildResult(
            RunningActionExecutorResultKind kind,
            RunningActionProgressSnapshot before,
            RunningActionRuntimeState state,
            string reason)
        {
            return new RunningActionExecutorResult(kind, before, state.ToSnapshot(), reason);
        }

        private static string ResolveReason(RunningActionExecutorTickRequest request, string fallback)
        {
            return string.IsNullOrWhiteSpace(request.Reason) ? fallback : request.Reason;
        }
    }
}
