namespace Arcontio.Core
{
    // =============================================================================
    // FoodConsumedEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento world-level minimale emesso quando un NPC consuma davvero una unita'
    /// di cibo, da uno stock oggettivo o dal proprio cibo privato.
    /// </para>
    ///
    /// <para><b>Fatto di mondo senza nuova logica sociale</b></para>
    /// <para>
    /// L'evento viene pubblicato solo dopo la mutazione riuscita del command. Non
    /// decide memoria, sospetto, rumor o nuove reazioni: conserva il fatto causale
    /// per future pipeline, mantenendo invariato il comportamento simulativo attuale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Tick/NpcId</b>: quando e chi ha consumato.</item>
    ///   <item><b>SourceKind</b>: origine diagnostica, per esempio Stock o PrivateFood.</item>
    ///   <item><b>FoodObjectId</b>: oggetto stock quando esiste, zero per cibo privato.</item>
    ///   <item><b>Cell</b>: cella osservabile del consumo o posizione dell'NPC.</item>
    ///   <item><b>RemainingUnits/HungerAfter</b>: stato gia' risultante dalla mutazione.</item>
    ///   <item><b>FoodDefId/NutritionValue</b>: dato alimentare risolto dal catalogo o dal fallback legacy.</item>
    /// </list>
    /// </summary>
    public sealed class FoodConsumedEvent : IWorldEvent
    {
        public readonly long Tick;
        public readonly int NpcId;
        public readonly string SourceKind;
        public readonly int FoodObjectId;
        public readonly int Units;
        public readonly int RemainingUnits;
        public readonly bool Depleted;
        public readonly int CellX;
        public readonly int CellY;
        public readonly float HungerAfter;
        public readonly string FoodDefId;
        public readonly float NutritionValue;
        public readonly bool UsedNutritionFallback;

        public FoodConsumedEvent(
            long tick,
            int npcId,
            string sourceKind,
            int foodObjectId,
            int units,
            int remainingUnits,
            bool depleted,
            int cellX,
            int cellY,
            float hungerAfter,
            string foodDefId = "",
            float nutritionValue = 0f,
            bool usedNutritionFallback = false)
        {
            Tick = tick;
            NpcId = npcId;
            SourceKind = sourceKind ?? string.Empty;
            FoodObjectId = foodObjectId;
            Units = units;
            RemainingUnits = remainingUnits;
            Depleted = depleted;
            CellX = cellX;
            CellY = cellY;
            HungerAfter = hungerAfter;
            FoodDefId = foodDefId ?? string.Empty;
            NutritionValue = nutritionValue;
            UsedNutritionFallback = usedNutritionFallback;
        }

        public string Describe()
            => $"FoodConsumed tick={Tick} npc={NpcId} source={SourceKind} foodObj={FoodObjectId} foodDef={FoodDefId} nutrition={NutritionValue:0.###} fallback={UsedNutritionFallback} units={Units} left={RemainingUnits} at=({CellX},{CellY})";
    }
}
