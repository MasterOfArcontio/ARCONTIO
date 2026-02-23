namespace Arcontio.Core
{
    /// <summary>
    /// NpcActionKind:
    /// Enumerazione ad alto livello delle azioni "osservabili" di un NPC.
    ///
    /// Nota:
    /// - Non è un sistema di animazione.
    /// - Non è un "behavior tree state".
    /// - È un'etichetta descrittiva pensata per:
    ///   - overlay debug (MapGrid SummaryOverlay)
    ///   - telemetria leggibile
    ///
    /// Questo evita che la view debba inferire azioni da segnali indiretti (posizione, fame, ecc.).
    /// </summary>
    public enum NpcActionKind
    {
        None = 0,
        Idle = 1,
        MoveTo = 2,
        Scan = 3,
        Eat = 4,
        Sleep = 5,
        Steal = 6,
        Work = 7,
        Social = 8,
        Combat = 9
    }

    /// <summary>
    /// NpcActionState:
    /// Stato descrittivo dell'azione corrente di un NPC.
    ///
    /// Campi:
    /// - Kind: categoria
    /// - Label: stringa breve (es. "MoveTo", "EatFromStock", "StealPrivateFood")
    /// - StartedTick: tick in cui lo stato è stato impostato
    /// - TargetObjectId: se applicabile (es. stock/letto)
    /// - TargetCell: se applicabile (es. MoveTo)
    ///
    /// IMPORTANTISSIMO:
    /// - Questo stato NON è "verità causale": è un'informazione di osservabilità.
    /// - La coerenza con la simulazione è garantita dal fatto che viene scritto
    ///   dagli stessi Command/System che già mutano il World.
    /// </summary>
    public struct NpcActionState
    {
        public NpcActionKind Kind;
        public string Label;
        public int StartedTick;

        public int TargetObjectId; // 0 = none
        public bool HasTargetCell;
        public int TargetX;
        public int TargetY;

        public static NpcActionState Idle(int tick = -1)
        {
            if (tick < 0) tick = (int)TickContext.CurrentTickIndex;
            return new NpcActionState
            {
                Kind = NpcActionKind.Idle,
                Label = "Idle",
                StartedTick = tick,
                TargetObjectId = 0,
                HasTargetCell = false,
                TargetX = 0,
                TargetY = 0
            };
        }

        public static NpcActionState MoveTo(int targetX, int targetY, string reasonLabel = null, int tick = -1)
        {
            if (tick < 0) tick = (int)TickContext.CurrentTickIndex;

            // reasonLabel è opzionale: ci aiuta a capire perché ci muoviamo ("See", "NeedFood", ecc.)
            string label = string.IsNullOrEmpty(reasonLabel) ? "MoveTo" : ("MoveTo:" + reasonLabel);

            return new NpcActionState
            {
                Kind = NpcActionKind.MoveTo,
                Label = label,
                StartedTick = tick,
                TargetObjectId = 0,
                HasTargetCell = true,
                TargetX = targetX,
                TargetY = targetY
            };
        }

        public static NpcActionState Eat(string label, int targetObjectId = 0, int tick = -1)
        {
            if (tick < 0) tick = (int)TickContext.CurrentTickIndex;
            return new NpcActionState
            {
                Kind = NpcActionKind.Eat,
                Label = string.IsNullOrEmpty(label) ? "Eat" : label,
                StartedTick = tick,
                TargetObjectId = targetObjectId,
                HasTargetCell = false,
                TargetX = 0,
                TargetY = 0
            };
        }

        public static NpcActionState Sleep(string label, int targetObjectId = 0, int tick = -1)
        {
            if (tick < 0) tick = (int)TickContext.CurrentTickIndex;
            return new NpcActionState
            {
                Kind = NpcActionKind.Sleep,
                Label = string.IsNullOrEmpty(label) ? "Sleep" : label,
                StartedTick = tick,
                TargetObjectId = targetObjectId,
                HasTargetCell = false
            };
        }

        public static NpcActionState Steal(string label, int targetObjectId = 0, int tick = -1)
        {
            if (tick < 0) tick = (int)TickContext.CurrentTickIndex;
            return new NpcActionState
            {
                Kind = NpcActionKind.Steal,
                Label = string.IsNullOrEmpty(label) ? "Steal" : label,
                StartedTick = tick,
                TargetObjectId = targetObjectId,
                HasTargetCell = false
            };
        }

        public override string ToString()
        {
            if (Kind == NpcActionKind.MoveTo && HasTargetCell)
                return $"{Label} ({TargetX},{TargetY})";

            if (TargetObjectId != 0)
                return $"{Label} (obj:{TargetObjectId})";

            return Label ?? Kind.ToString();
        }
    }
}
