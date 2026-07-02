namespace Arcontio.Core
{
    /// <summary>
    /// MemoryType: tassonomia minima delle tracce di memoria.
    /// 
    /// Nota:
    /// - Questo enum cresce nel tempo.
    /// - È usato come "categoria" della traccia (predatore, violenza, morte, crimine...).
    /// </summary>
    public enum MemoryType
    {
        // Threat / Predator
        PredatorSpotted,
        PredatorRumor,

        // Violence
        AttackSuffered,
        AttackWitnessed,
        NearDeathExperience,

        // Death
        DeathWitnessed,
        DeathOfBonded,
        DeathHeard,

        AidRequested,

        ObjectSpotted,
        NpcSpotted,

        FoodConsumed,
        BedRested,

        // Crime (future)
        TheftWitnessed,             // Ho visto un furto
        TheftSuffered,
        RobberyWitnessed,
        RobberySuffered,

        FoodStolenFromMe,
        FoodPossiblyStolenFromMe,
        FoodMissingSuspected,       // Sospetto che mi manchi cibo

        LandmarkSpotted,            // Ho visto un landmark tipizzato
        ResourceSearchFromLandmark, // Ho cercato una risorsa partendo da un landmark

    }
}
