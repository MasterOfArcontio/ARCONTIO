namespace Arcontio.Core
{
    // Needs spostato in Core/Needs/NeedKind.cs (sessione v0.04.07):
    //   struct NeedState, enum NeedKind, struct NpcNeeds
    // La vecchia struct Needs (Hunger01/Fatigue01/Morale01) è rimossa.

    /// <summary>
    /// Stato sociale (placeholder): reputazione, lealt�, legami, ecc.
    /// </summary>
    public struct Social
    {
        public float LeadershipScore;     // "leadership analogica" (non � necessariamente il leader accettato)
        public float LoyaltyToLeader01;   // lealt� verso leader accettato
        public float JusticePerception01; // percezione di giustizia

        // In futuro: relazioni, amicizie, partner, inimicizie, ecc.
    }
}
