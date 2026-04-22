using System;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // JobActionKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Vocabolario stabile delle azioni atomiche che una fase di job potra'
    /// richiedere alla futura macchina di esecuzione.
    /// </para>
    ///
    /// <para><b>Step atomici prima dei sistemi concreti</b></para>
    /// <para>
    /// La enum non esegue nulla e non conosce il World. Serve a descrivere, in modo
    /// serializzabile e testabile, quali mattoni operativi compongono un mini job.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MoveToCell</b>: richiesta di movimento verso una cella.</item>
    ///   <item><b>ReserveTarget</b>: tentativo di prenotazione risorsa o cella.</item>
    ///   <item><b>ReleaseReservation</b>: rilascio esplicito di una prenotazione.</item>
    ///   <item><b>WaitTicks</b>: attesa controllata da tick.</item>
    ///   <item><b>Observe</b>: lettura percettiva locale futura.</item>
    ///   <item><b>Search</b>: esplorazione o ricerca senza target certo.</item>
    ///   <item><b>PickUp</b>: acquisizione oggetto o stock.</item>
    ///   <item><b>Drop</b>: deposito oggetto o stock.</item>
    ///   <item><b>Consume</b>: consumo di risorsa per bisogno.</item>
    ///   <item><b>Communicate</b>: emissione di informazione sociale.</item>
    ///   <item><b>Evaluate</b>: controllo locale di una condizione.</item>
    ///   <item><b>Custom</b>: estensione temporanea per domini non ancora modellati.</item>
    /// </list>
    /// </summary>
    public enum JobActionKind
    {
        None = 0,
        MoveToCell = 10,
        ReserveTarget = 20,
        ReleaseReservation = 30,
        WaitTicks = 40,
        Observe = 50,
        Search = 60,
        PickUp = 70,
        Drop = 80,
        Consume = 90,
        Communicate = 100,
        Evaluate = 110,
        Custom = 999
    }

    // =============================================================================
    // StepResultStatus
    // =============================================================================
    /// <summary>
    /// <para>
    /// Esito normalizzato restituito dall'esecuzione di una singola azione di job.
    /// </para>
    ///
    /// <para><b>Contratto tra step e state machine</b></para>
    /// <para>
    /// Lo step non decide l'intero ciclo di vita del job. Restituisce un risultato
    /// piccolo e la state machine traduce quel risultato in avanzamento, attesa,
    /// fallimento o completamento.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Running</b>: lo step ha iniziato o continua lavoro asincrono.</item>
    ///   <item><b>Succeeded</b>: lo step e' completato e si puo' avanzare.</item>
    ///   <item><b>Waiting</b>: lo step chiede di restare fermo per tick futuri.</item>
    ///   <item><b>Blocked</b>: lo step non puo' procedere ora ma non e' fallito.</item>
    ///   <item><b>Failed</b>: lo step richiede chiusura o recupero del job.</item>
    /// </list>
    /// </summary>
    public enum StepResultStatus
    {
        Running = 0,
        Succeeded = 10,
        Waiting = 20,
        Blocked = 30,
        Failed = 40
    }

    // =============================================================================
    // JobAction
    // =============================================================================
    /// <summary>
    /// <para>
    /// Descrizione data-pura di uno step atomico dentro una <c>JobPhase</c>.
    /// </para>
    ///
    /// <para><b>Sequenza fissa locale, non GOAP globale</b></para>
    /// <para>
    /// In v0.06 una fase contiene azioni ordinate e dichiarative. Il GOAP potra'
    /// generare piani in futuro, ma il runtime iniziale deve poter testare sequenze
    /// deterministiche e leggibili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActionId</b>: identificatore stabile nello scope della fase.</item>
    ///   <item><b>Kind</b>: tipo operativo richiesto.</item>
    ///   <item><b>Label</b>: testo diagnostico per log e UI di debug.</item>
    ///   <item><b>HasTargetCell/TargetCell</b>: target cella opzionale gia' risolto.</item>
    ///   <item><b>TargetObjectId</b>: oggetto opzionale gia' risolto.</item>
    ///   <item><b>DurationTicks</b>: durata o attesa minima richiesta.</item>
    ///   <item><b>PayloadKey</b>: chiave testuale per dati esterni controllati.</item>
    /// </list>
    /// </summary>
    public readonly struct JobAction
    {
        public readonly string ActionId;
        public readonly JobActionKind Kind;
        public readonly string Label;
        public readonly bool HasTargetCell;
        public readonly Vector2Int TargetCell;
        public readonly int TargetObjectId;
        public readonly int DurationTicks;
        public readonly string PayloadKey;

        public JobAction(
            string actionId,
            JobActionKind kind,
            string label,
            bool hasTargetCell,
            Vector2Int targetCell,
            int targetObjectId,
            int durationTicks,
            string payloadKey)
        {
            ActionId = string.IsNullOrWhiteSpace(actionId) ? kind.ToString() : actionId;
            Kind = kind;
            Label = label ?? string.Empty;
            HasTargetCell = hasTargetCell;
            TargetCell = targetCell;
            TargetObjectId = targetObjectId;
            DurationTicks = Math.Max(0, durationTicks);
            PayloadKey = payloadKey ?? string.Empty;
        }

        public static JobAction MoveTo(string actionId, Vector2Int targetCell, string label)
        {
            // Il movimento viene descritto come intenzione atomica; il pathfinding
            // concreto verra' invocato solo dallo step executor dedicato.
            return new JobAction(actionId, JobActionKind.MoveToCell, label, true, targetCell, -1, 0, string.Empty);
        }

        public static JobAction Simple(string actionId, JobActionKind kind, string label)
        {
            // Factory per step senza target materiale, utile per evaluate, observe e
            // azioni di cleanup che dipendono dallo stato corrente del job.
            return new JobAction(actionId, kind, label, false, Vector2Int.zero, -1, 0, string.Empty);
        }

        public static JobAction Wait(string actionId, int durationTicks, string label)
        {
            // L'attesa conserva la durata nel contratto dati, evitando timer nascosti
            // dentro la futura state machine.
            return new JobAction(actionId, JobActionKind.WaitTicks, label, false, Vector2Int.zero, -1, durationTicks, string.Empty);
        }
    }

    // =============================================================================
    // StepResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato data-puro prodotto dall'esecuzione di una <c>JobAction</c>.
    /// </para>
    ///
    /// <para><b>Esito esplicito e diagnosticabile</b></para>
    /// <para>
    /// Il risultato evita bool ambigui. Ogni esito puo' portare una ragione di
    /// fallimento, un'attesa suggerita e un messaggio diagnostico senza introdurre
    /// dipendenze dirette da sistemi globali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Status</b>: classificazione primaria dell'esito.</item>
    ///   <item><b>FailureReason</b>: motivo normalizzato quando lo status e' Failed.</item>
    ///   <item><b>SuggestedWaitTicks</b>: tick consigliati per Waiting o Blocked.</item>
    ///   <item><b>DiagnosticMessage</b>: messaggio umano per QA e telemetria.</item>
    /// </list>
    /// </summary>
    public readonly struct StepResult
    {
        public readonly StepResultStatus Status;
        public readonly JobFailureReason FailureReason;
        public readonly int SuggestedWaitTicks;
        public readonly string DiagnosticMessage;

        public bool IsTerminalFailure => Status == StepResultStatus.Failed;
        public bool CanAdvance => Status == StepResultStatus.Succeeded;

        public StepResult(
            StepResultStatus status,
            JobFailureReason failureReason,
            int suggestedWaitTicks,
            string diagnosticMessage)
        {
            Status = status;
            FailureReason = status == StepResultStatus.Failed && failureReason == JobFailureReason.None
                ? JobFailureReason.Unknown
                : failureReason;
            SuggestedWaitTicks = Math.Max(0, suggestedWaitTicks);
            DiagnosticMessage = diagnosticMessage ?? string.Empty;
        }

        public static StepResult Running(string diagnosticMessage)
        {
            // Running indica lavoro ancora in corso e non muove il cursore di step.
            return new StepResult(StepResultStatus.Running, JobFailureReason.None, 0, diagnosticMessage);
        }

        public static StepResult Succeeded(string diagnosticMessage)
        {
            // Succeeded e' l'unico esito che autorizza la state machine ad avanzare.
            return new StepResult(StepResultStatus.Succeeded, JobFailureReason.None, 0, diagnosticMessage);
        }

        public static StepResult Waiting(int waitTicks, string diagnosticMessage)
        {
            // Waiting non e' un errore: protegge step multi-tick e azioni di pausa.
            return new StepResult(StepResultStatus.Waiting, JobFailureReason.None, waitTicks, diagnosticMessage);
        }

        public static StepResult Blocked(int retryTicks, string diagnosticMessage)
        {
            // Blocked lascia spazio a retry, reservation contention e future policy.
            return new StepResult(StepResultStatus.Blocked, JobFailureReason.None, retryTicks, diagnosticMessage);
        }

        public static StepResult Failed(JobFailureReason reason, string diagnosticMessage)
        {
            // Failed normalizza reason None a Unknown nel costruttore, cosi' i log non
            // ricevono fallimenti muti.
            return new StepResult(StepResultStatus.Failed, reason, 0, diagnosticMessage);
        }
    }
}
