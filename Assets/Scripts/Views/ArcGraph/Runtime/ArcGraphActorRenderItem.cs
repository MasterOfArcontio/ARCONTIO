namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorRenderItem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Item passivo che descrive come un actor dovrebbe essere consegnato al futuro
    /// renderer ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: actor da disegnare, non actor da controllare</b></para>
    /// <para>
    /// L'item contiene posizione visuale, sprite key e policy LOD risolta. Non
    /// contiene riferimenti all'NPC, non modifica job, non chiama movimento e non
    /// crea componenti Unity. Il suo compito e' preparare un payload stabile per un
    /// wrapper grafico futuro.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActorId</b>: id actor visualizzato.</item>
    ///   <item><b>DiscreteCell</b>: cella simulativa discreta.</item>
    ///   <item><b>VisualX/Y/Z</b>: posizione grafica frazionaria.</item>
    ///   <item><b>SpriteKey</b>: chiave sprite provvisoria.</item>
    ///   <item><b>ActorMode</b>: LOD actor risolto.</item>
    ///   <item><b>Hunger01</b>: valore fame read-only copiato dallo snapshot actor.</item>
    ///   <item><b>SortKey</b>: ordinamento deterministico.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorRenderItem
    {
        public readonly int ActorId;
        public readonly ArcGraphCellCoord DiscreteCell;
        public readonly float VisualX;
        public readonly float VisualY;
        public readonly float VisualZ;
        public readonly string SpriteKey;
        public readonly ArcGraphActorLodMode ActorMode;
        public readonly bool AllowsSpriteAnimation;
        public readonly bool AllowsLayeredActorSprites;
        public readonly bool HasMotion;
        public readonly float MotionProgress01;
        public readonly bool HasHungerValue;
        public readonly float Hunger01;
        public readonly bool IsVisible;
        public readonly string HiddenReason;
        public readonly ArcGraphRenderSortKey SortKey;

        // =============================================================================
        // ArcGraphActorRenderItem
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un item actor renderizzabile.
        /// </para>
        ///
        /// <para><b>Normalizzazione difensiva</b></para>
        /// <para>
        /// Le stringhe nulle diventano vuote, e il motivo di invisibilita' resta
        /// leggibile. L'item non valida asset: una sprite key vuota e' solo un dato
        /// diagnostico che il builder o il wrapper potranno interpretare.
        /// </para>
        /// </summary>
        public ArcGraphActorRenderItem(
            int actorId,
            ArcGraphCellCoord discreteCell,
            float visualX,
            float visualY,
            float visualZ,
            string spriteKey,
            ArcGraphActorLodMode actorMode,
            bool allowsSpriteAnimation,
            bool allowsLayeredActorSprites,
            bool hasMotion,
            float motionProgress01,
            bool isVisible,
            string hiddenReason,
            ArcGraphRenderSortKey sortKey,
            bool hasHungerValue = false,
            float hunger01 = 0f)
        {
            ActorId = actorId;
            DiscreteCell = discreteCell;
            VisualX = visualX;
            VisualY = visualY;
            VisualZ = visualZ;
            SpriteKey = spriteKey ?? string.Empty;
            ActorMode = actorMode;
            AllowsSpriteAnimation = allowsSpriteAnimation;
            AllowsLayeredActorSprites = allowsLayeredActorSprites;
            HasMotion = hasMotion;
            MotionProgress01 = Clamp01(motionProgress01);
            HasHungerValue = hasHungerValue;
            Hunger01 = Clamp01(hunger01);
            IsVisible = isVisible;
            HiddenReason = string.IsNullOrWhiteSpace(hiddenReason) ? "None" : hiddenReason;
            SortKey = sortKey;
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
