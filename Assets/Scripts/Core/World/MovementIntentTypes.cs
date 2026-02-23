namespace Arcontio.Core
{
    /// <summary>
    /// Intento di movimento minimale.
    ///
    /// Filosofia:
    /// - Questo è volutamente "stupido": TargetCell + optional reason.
    /// - La decisione (Rules/Decision) scrive l'intent.
    /// - Un sistema fisico (MovementSystem) lo esegue step-by-step.
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
    }

    public enum MoveIntentReason
    {
        None = 0,
        SeekFood = 1,
        SeekBed = 2,
        Wander = 3,
        SeekTalkTarget = 4
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
