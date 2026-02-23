namespace Arcontio.Core
{
    /// <summary>
    /// NpcBalloonKind:
    /// Categoria ad alto livello per "balloon"/fumetti sopra la testa dell'NPC.
    ///
    /// NOTE ARCHITETTURALI (ARCONTIO):
    /// - Questo NON è UI.
    /// - Questo NON è animazione.
    /// - Questo è solo un segnale osservabile, scritto dal core quando un fatto rilevante accade,
    ///   così la view può visualizzarlo senza fare inferenza fragile.
    ///
    /// Esempi:
    /// - Eat: l'NPC ha mangiato
    /// - Steal: l'NPC ha rubato
    /// - TheftWitnessed: l'NPC ha visto un furto
    /// - TheftSuffered: l'NPC è la vittima (e lo ha percepito)
    /// </summary>
    public enum NpcBalloonKind
    {
        None = 0,
        Eat = 1,
        Steal = 2,
        TheftWitnessed = 3,
        TheftSuffered = 4
    }

    /// <summary>
    /// NpcBalloonSignal:
    /// Ultimo segnale emesso per un NPC.
    ///
    /// Motivazione:
    /// - Un balloon è una UI transiente.
    /// - Serve solo sapere "l'ultima cosa importante" + "quando".
    /// - Evitiamo di costruire log/event-stream per la view.
    ///
    /// Campi:
    /// - Kind: tipo balloon
    /// - Tick: tick del simulatore in cui è stato emesso
    /// - SubjectId / SecondarySubjectId: opzionali, utili per debug (es. thief/victim)
    /// </summary>
    public struct NpcBalloonSignal
    {
        public NpcBalloonKind Kind;
        public int Tick;

        public int SubjectId;
        public int SecondarySubjectId;

        public override string ToString()
        {
            return $"{Kind} tick={Tick} subj={SubjectId} subj2={SecondarySubjectId}";
        }
    }
}
