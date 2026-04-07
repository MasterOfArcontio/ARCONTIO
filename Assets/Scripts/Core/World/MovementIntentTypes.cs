using System;

namespace Arcontio.Core
{
    // =============================================================================
    // MoveIntent — Patch 0.02.05.B: aggiunto campo IsNew
    // =============================================================================
    /// <summary>
    /// <b>MoveIntent</b> — intento di movimento minimale per un NPC.
    ///
    /// <para>
    /// Filosofia: struct volutamente "stupida" — solo target cell + reason debuggabile.
    /// Nessuna logica di navigazione interna.
    /// </para>
    ///
    /// <para><b>Chi scrive:</b> Rule/Decision tramite <c>SetMoveIntentCommand</c>.</para>
    /// <para><b>Chi esegue:</b> <c>MovementSystem</c> ogni tick.</para>
    ///
    /// <para>
    /// <b>Patch 0.02.05.B:</b> aggiunto <see cref="IsNew"/> per segnalare al
    /// <c>MovementSystem</c> che l'intent è appena stato scritto e non ancora
    /// inizializzato (nessuna macro-route o direct path preparati).
    /// </para>
    /// </summary>
    public struct MoveIntent
    {
        public bool Active;

        public int TargetX;
        public int TargetY;

        /// <summary>
        /// Motivazione “debuggable”. Non guida la logica, serve solo a leggibilità.
        /// </summary>
        public MoveIntentReason Reason;

        /// <summary>
        /// Opzionale: oggetto a cui stai andando (stock cibo, letto, ecc.)
        /// </summary>
        public int TargetObjectId;

        /// <summary>
        /// Contatore di tick consecutivi in cui il movimento è bloccato.
        /// Incrementato dal MovementSystem se in un tick l'NPC non riesce ad avanzare.
        /// Azzerato ogni volta che l'NPC fa almeno uno step.
        /// Quando supera DefaultIntentStuckTicks, il MovementSystem cancella l'intent.
        /// </summary>
        public int BlockedTicks;

        /// <summary>
        /// Flag "primo tick" (Patch 0.02.05.B).
        ///
        /// Impostato a true da SetMoveIntentCommand quando scrive un nuovo intent attivo.
        /// Il MovementSystem lo legge al primo tick, esegue InitializeNavigation
        /// (sceglie direct path o macro-route, prepara i path debug), poi lo azzera.
        ///
        /// Questo meccanismo permette di spostare il planning di navigazione
        /// dal Command (dove non appartiene) al MovementSystem (dove appartiene).
        /// </summary>
        public bool IsNew;
    }

    // I tipi di movimento che abbiamo finora
    public enum MoveIntentReason
    {
        None = 0,
        SeekFood = 1,
        SeekBed = 2,
        Wander = 3,
        SeekTalkTarget = 4,
        DebugClick = 5
    }

    // =========================================================================
    // NpcMoveBackOffState — v0.03.05-FailureLadder
    // =========================================================================
    /// <summary>
    /// Stato del back-off per la failure ladder del movimento.
    ///
    /// <para>
    /// Quando un NPC rimane bloccato per <c>intentStuckTicksDefault</c> tick
    /// consecutivi, invece di cancellare immediatamente l'intent, entra in
    /// back-off: pausa il movimento per un periodo configurabile, poi tenta
    /// un replan della navigazione (<c>InitializeNavigation</c> con <c>IsNew=true</c>).
    /// </para>
    ///
    /// <para><b>Stage:</b> conta i fallimenti consecutivi per lo stesso intent.
    /// Stage 1 = primo stuck, Stage 2 = secondo stuck, ecc.
    /// Dopo <c>backoff_max_stages</c> stage, l'intent viene cancellato.</para>
    /// </summary>
    [Serializable]
    public sealed class NpcMoveBackOffState
    {
        /// <summary>True se l'NPC è attualmente in back-off.</summary>
        public bool Active;

        /// <summary>
        /// Tick a cui il back-off scade e si tenta il replan.
        /// </summary>
        public long ResumeAtTick;

        /// <summary>
        /// Stage corrente (quanti back-off consecutivi per questo intent).
        /// 1-based: Stage 1 = primo fallimento, Stage 2 = secondo, ecc.
        /// </summary>
        public int Stage;
    }

    /// <summary>
    /// Stato di “scan” direzionale:
    /// - Non esiste 360° gratuito.
    /// - Lo scan è modellato come 4 rotazioni consecutive (una per tick).
    /// </summary>
    public struct ScanState
    {
        public bool Active;

        /// <summary>
        /// Quante rotazioni da fare ancora (tipicamente 4).
        /// Ogni tick: Turn90 + RemainingTurns--.
        /// </summary>
        public int RemainingTurns;

        /// <summary>
        /// Tick dell’ultimo “turn” fatto durante scan, per evitare doppi turn in stesso tick.
        /// </summary>
        public int LastTurnTick;
    }
}
