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
    ///   <item><b>FacingDirectionKey</b>: direzione idle/look direction per actor.</item>
    ///   <item><b>VisualWidth/Height</b>: dimensione sprite oggetto, se disponibile.</item>
    ///   <item><b>VisualBaseWidth/Height</b>: base dello sprite appoggiata alla cella.</item>
    ///   <item><b>VisualBaseMiniTileMask</b>: copertura 2x2 della base visuale su quarti 16x16.</item>
    ///   <item><b>VisualPivot</b>: convenzione di ancoraggio, per esempio <c>bottom_center</c>.</item>
    ///   <item><b>VisualOffset</b>: offset grafico in pixel gia' copiato dal catalogo.</item>
    ///   <item><b>FadeWhenActorBehind/UseShadow</b>: flag visuali futuri non simulativi.</item>
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
        public readonly string FacingDirectionKey;
        public readonly ArcGraphActorRunningActionOverlaySnapshot RunningActionOverlay;
        public readonly int VisualWidthPixels;
        public readonly int VisualHeightPixels;
        public readonly int VisualBaseWidthPixels;
        public readonly int VisualBaseHeightPixels;
        public readonly string VisualBaseMiniTileMask;
        public readonly string VisualPivot;
        public readonly int VisualOffsetX;
        public readonly int VisualOffsetY;
        public readonly bool FadeWhenActorBehind;
        public readonly bool UseShadow;
        public readonly bool IsDoor;
        public readonly bool IsDoorLocked;
        public readonly ArcGraphDoorVisualState DoorVisualState;
        public readonly ArcGraphDoorVisualOrientation DoorVisualOrientation;

        public bool HasObjectVisualMetadata =>
            Kind == ArcGraphRenderItemKind.Object
            && (VisualWidthPixels > 0
                || VisualHeightPixels > 0
                || VisualBaseWidthPixels > 0
                || VisualBaseHeightPixels > 0
                || !string.IsNullOrWhiteSpace(VisualBaseMiniTileMask)
                || !string.IsNullOrWhiteSpace(VisualPivot));

        public bool IsTallObjectVisual =>
            Kind == ArcGraphRenderItemKind.Object
            && VisualHeightPixels > 0
            && VisualBaseHeightPixels > 0
            && VisualHeightPixels > VisualBaseHeightPixels;

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
            float motionProgress01,
            string facingDirectionKey = "",
            ArcGraphActorRunningActionOverlaySnapshot runningActionOverlay = default)
            : this(
                kind,
                entityId,
                discreteCell,
                spriteRequest,
                worldX,
                worldY,
                worldZ,
                sortingOrder,
                hasMotion,
                motionProgress01,
                0,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                0,
                0,
                false,
                false,
                facingDirectionKey,
                false,
                false,
                ArcGraphDoorVisualState.None,
                ArcGraphDoorVisualOrientation.Horizontal,
                runningActionOverlay)
        {
        }

        // =============================================================================
        // ArcGraphActorObjectSceneRenderEntry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una entry scena actor/object immutabile includendo anche i
        /// metadati visuali oggetto.
        /// </para>
        ///
        /// <para><b>Oggetti alti senza nuova authority</b></para>
        /// <para>
        /// I metadati permettono al renderer scena di sapere che un oggetto occupa
        /// graficamente piu' pixel della cella base, come un muro 32x83 appoggiato
        /// a una base 32x32. Restano dati visuali: non modificano collisioni,
        /// passabilita', occlusione o stato del mondo.
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
            float motionProgress01,
            int visualWidthPixels,
            int visualHeightPixels,
            int visualBaseWidthPixels,
            int visualBaseHeightPixels,
            string visualBaseMiniTileMask,
            string visualPivot,
            int visualOffsetX,
            int visualOffsetY,
            bool fadeWhenActorBehind,
            bool useShadow,
            string facingDirectionKey = "",
            bool isDoor = false,
            bool isDoorLocked = false,
            ArcGraphDoorVisualState doorVisualState = ArcGraphDoorVisualState.None,
            ArcGraphDoorVisualOrientation doorVisualOrientation = ArcGraphDoorVisualOrientation.Horizontal,
            ArcGraphActorRunningActionOverlaySnapshot runningActionOverlay = default)
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
            FacingDirectionKey = NormalizeDirectionKey(facingDirectionKey);
            RunningActionOverlay = runningActionOverlay;
            VisualWidthPixels = visualWidthPixels < 0 ? 0 : visualWidthPixels;
            VisualHeightPixels = visualHeightPixels < 0 ? 0 : visualHeightPixels;
            VisualBaseWidthPixels = visualBaseWidthPixels < 0 ? 0 : visualBaseWidthPixels;
            VisualBaseHeightPixels = visualBaseHeightPixels < 0 ? 0 : visualBaseHeightPixels;
            VisualBaseMiniTileMask = visualBaseMiniTileMask ?? string.Empty;
            VisualPivot = visualPivot ?? string.Empty;
            VisualOffsetX = visualOffsetX;
            VisualOffsetY = visualOffsetY;
            FadeWhenActorBehind = fadeWhenActorBehind;
            UseShadow = useShadow;
            IsDoor = isDoor;
            IsDoorLocked = isDoor && isDoorLocked;
            DoorVisualState = isDoor ? doorVisualState : ArcGraphDoorVisualState.None;
            DoorVisualOrientation = doorVisualOrientation;
        }

        private static string NormalizeDirectionKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "north":
                case "south":
                case "east":
                case "west":
                    return normalized;
                default:
                    return string.Empty;
            }
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
