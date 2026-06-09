namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorObjectSceneRenderEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Entry passiva gia' pronta per essere consumata da un futuro renderer scena
    /// actor/object.
    /// </para>
    ///
    /// <para><b>Principio architetturale: piano scena prima degli oggetti Unity</b></para>
    /// <para>
    /// Questa entry traduce un item della <c>ArcGraphRenderQueue</c> in dati concreti
    /// per la scena: posizione mondo, sorting order e richiesta sprite. Non crea
    /// ancora <c>GameObject</c>, non risolve asset e non tocca la simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind/EntityId</b>: identita' item.</item>
    ///   <item><b>SpriteRequest</b>: richiesta per il resolver scene-side.</item>
    ///   <item><b>WorldX/Y/Z</b>: posizione visuale gia' scalata.</item>
    ///   <item><b>SortingOrder</b>: ordine SpriteRenderer futuro.</item>
    ///   <item><b>HasMotion/MotionProgress01</b>: stato movimento actor copiato.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorObjectSceneRenderEntry
    {
        public readonly ArcGraphRenderItemKind Kind;
        public readonly int EntityId;
        public readonly ArcGraphCellCoord DiscreteCell;
        public readonly ArcGraphSpriteResolveRequest SpriteRequest;
        public readonly float WorldX;
        public readonly float WorldY;
        public readonly float WorldZ;
        public readonly int SortingOrder;
        public readonly bool HasMotion;
        public readonly float MotionProgress01;

        // =============================================================================
        // ArcGraphActorObjectSceneRenderEntry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una entry scena actor/object immutabile.
        /// </para>
        /// </summary>
        public ArcGraphActorObjectSceneRenderEntry(
            ArcGraphRenderItemKind kind,
            int entityId,
            ArcGraphCellCoord discreteCell,
            ArcGraphSpriteResolveRequest spriteRequest,
            float worldX,
            float worldY,
            float worldZ,
            int sortingOrder,
            bool hasMotion,
            float motionProgress01)
        {
            Kind = kind;
            EntityId = entityId;
            DiscreteCell = discreteCell;
            SpriteRequest = spriteRequest;
            WorldX = worldX;
            WorldY = worldY;
            WorldZ = worldZ;
            SortingOrder = sortingOrder;
            HasMotion = hasMotion;
            MotionProgress01 = Clamp01(motionProgress01);
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }
    }
}
