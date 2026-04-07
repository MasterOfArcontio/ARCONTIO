namespace Arcontio.Core
{
    /// <summary>
    /// Bisogni e stati interni che cambiano spesso.
    /// </summary>
    public struct Needs
    {
        public float Hunger01;   // 0=ok, 1=affamato
        public float Fatigue01;  // 0=ok, 1=stanco
        public float Morale01;   // 0=depresso, 1=ottimo

        // timer/accumulatori
        //public float HungerRate;
        //public float FatigueRate;

        // Cache/flag derivati (settati dal NeedsDecaySystem)
        public bool IsHungry;
        public bool IsTired;
    }

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
