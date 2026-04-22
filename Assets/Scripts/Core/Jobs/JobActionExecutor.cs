using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // JobActionExecutionContext
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contesto esplicito passato a un executor di <c>JobAction</c>.
    /// </para>
    ///
    /// <para><b>Input espliciti invece di letture globali</b></para>
    /// <para>
    /// L'executor riceve solo i dati necessari allo step corrente: identita', tick,
    /// posizione nota e reservation store opzionale. Non legge il World e non cerca
    /// da solo l'NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId/JobId</b>: proprietario operativo dello step.</item>
    ///   <item><b>Tick</b>: tempo corrente per attese e scadenze.</item>
    ///   <item><b>NpcCell</b>: posizione gia' risolta dal chiamante.</item>
    ///   <item><b>Reservations</b>: store di contesa, opzionale per step che non prenotano.</item>
    /// </list>
    /// </summary>
    public readonly struct JobActionExecutionContext
    {
        public readonly int NpcId;
        public readonly string JobId;
        public readonly int Tick;
        public readonly Vector2Int NpcCell;
        public readonly ReservationStore Reservations;

        public JobActionExecutionContext(int npcId, string jobId, int tick, Vector2Int npcCell, ReservationStore reservations)
        {
            NpcId = npcId;
            JobId = jobId ?? string.Empty;
            Tick = tick;
            NpcCell = npcCell;
            Reservations = reservations;
        }
    }

    // =============================================================================
    // IJobActionExecutor
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto per un modulo che interpreta una singola azione atomica di job.
    /// </para>
    ///
    /// <para><b>Executor modulare</b></para>
    /// <para>
    /// Ogni executor puo' coprire un sottoinsieme di <c>JobActionKind</c>. Il sistema
    /// chiamante puo' comporli progressivamente senza costruire un monolite.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CanExecute</b>: dichiara se lo step e' gestito.</item>
    ///   <item><b>Execute</b>: restituisce StepResult data-puro.</item>
    /// </list>
    /// </summary>
    public interface IJobActionExecutor
    {
        bool CanExecute(JobAction action);
        StepResult Execute(JobAction action, JobActionExecutionContext context);
    }

    // =============================================================================
    // BasicJobActionExecutor
    // =============================================================================
    /// <summary>
    /// <para>
    /// Executor MVP per gli step fondamentali: movimento, prenotazione, rilascio e
    /// attesa.
    /// </para>
    ///
    /// <para><b>Step execution senza side effect di mondo</b></para>
    /// <para>
    /// Il movimento non calcola path e non sposta l'NPC: verifica solo se il target
    /// e' gia' raggiunto o se serve attendere un sistema di movimento esterno.
    /// Prenotazione e rilascio agiscono solo sul <c>ReservationStore</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MoveToCell</b>: successo se NpcCell coincide col target.</item>
    ///   <item><b>ReserveTarget</b>: crea record nel ReservationStore.</item>
    ///   <item><b>ReleaseReservation</b>: libera risorse del job corrente.</item>
    ///   <item><b>WaitTicks</b>: restituisce Waiting con durata dichiarata.</item>
    /// </list>
    /// </summary>
    public sealed class BasicJobActionExecutor : IJobActionExecutor
    {
        public bool CanExecute(JobAction action)
        {
            return action.Kind == JobActionKind.MoveToCell
                || action.Kind == JobActionKind.ReserveTarget
                || action.Kind == JobActionKind.ReleaseReservation
                || action.Kind == JobActionKind.WaitTicks;
        }

        public StepResult Execute(JobAction action, JobActionExecutionContext context)
        {
            if (action.Kind == JobActionKind.MoveToCell)
                return ExecuteMove(action, context);

            if (action.Kind == JobActionKind.ReserveTarget)
                return ExecuteReserve(action, context);

            if (action.Kind == JobActionKind.ReleaseReservation)
                return ExecuteRelease(context);

            if (action.Kind == JobActionKind.WaitTicks)
                return StepResult.Waiting(action.DurationTicks, "WaitTicks");

            return StepResult.Failed(JobFailureReason.StepFailed, "UnsupportedBasicAction");
        }

        private static StepResult ExecuteMove(JobAction action, JobActionExecutionContext context)
        {
            // L'executor non muove: se non siamo arrivati, segnala Running e lascia
            // a un bridge futuro la produzione del comando di movimento.
            if (!action.HasTargetCell)
                return StepResult.Failed(JobFailureReason.MissingTarget, "MoveMissingTarget");

            return context.NpcCell == action.TargetCell
                ? StepResult.Succeeded("MoveTargetReached")
                : StepResult.Running("MoveTargetPending");
        }

        private static StepResult ExecuteReserve(JobAction action, JobActionExecutionContext context)
        {
            // Senza store esplicito non possiamo prenotare in modo coerente.
            if (context.Reservations == null)
                return StepResult.Failed(JobFailureReason.ReservationDenied, "ReservationStoreMissing");

            var kind = action.HasTargetCell ? ReservationTargetKind.Cell : ReservationTargetKind.Object;
            var record = new ReservationRecord(
                string.Empty,
                context.JobId,
                context.NpcId,
                kind,
                action.TargetCell,
                action.TargetObjectId,
                context.Tick,
                context.Tick + 100);

            return context.Reservations.TryReserve(record, out _)
                ? StepResult.Succeeded("ReservationAccepted")
                : StepResult.Blocked(5, "ReservationDenied");
        }

        private static StepResult ExecuteRelease(JobActionExecutionContext context)
        {
            // Il rilascio e' idempotente: liberare zero record non e' un errore.
            context.Reservations?.ReleaseByJob(context.JobId);
            return StepResult.Succeeded("ReservationsReleased");
        }
    }
}
