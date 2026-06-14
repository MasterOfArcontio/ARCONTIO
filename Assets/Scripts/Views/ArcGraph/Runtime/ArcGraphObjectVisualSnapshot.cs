namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectVisualSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only minimo per rappresentare un oggetto del <c>World</c> nel
    /// futuro layer grafico oggetti di <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: oggetto visuale derivato dal World</b></para>
    /// <para>
    /// Il <c>World</c> resta la sorgente oggettiva di posizione, definizione e stato
    /// dell'oggetto. Questo snapshot e' solo una proiezione grafica: contiene id,
    /// defId, cella, sprite key e poche informazioni osservabili utili alla view.
    /// Non espone <c>WorldObjectInstance</c>, non espone <c>ObjectDef</c> e non
    /// conserva riferimenti a dizionari mutabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ObjectId</b>: id runtime dell'oggetto.</item>
    ///   <item><b>DefId</b>: identificatore data-driven della definizione oggetto.</item>
    ///   <item><b>Cell</b>: posizione discreta visuale.</item>
    ///   <item><b>SpriteKey</b>: chiave Resources/catalogo sprite da risolvere lato renderer.</item>
    ///   <item><b>IsHeld</b>: indica se l'oggetto e' trasportato e quindi non appoggiato a cella.</item>
    ///   <item><b>HolderActorId</b>: NPC che trasporta l'oggetto, se noto.</item>
    ///   <item><b>FoodStockUnits</b>: quantita' stock osservabile per label/debug visuale.</item>
    ///   <item><b>FootprintWidth/FootprintHeight</b>: ingombro logico XY copiato dal catalogo oggetti.</item>
    ///   <item><b>VisualKind/VisualResolverKey</b>: classificazione visuale passiva per resolver futuri.</item>
    ///   <item><b>VisualWidthPixels/VisualHeightPixels</b>: dimensione nominale sprite letta dalla sezione visuale.</item>
    ///   <item><b>VisualBaseWidthPixels/VisualBaseHeightPixels</b>: base logica dello sprite appoggiata alla cella.</item>
    ///   <item><b>VisualBaseMiniTileMask</b>: copertura 2x2 della base visuale su quarti da 16x16.</item>
    ///   <item><b>VisualPivot</b>: convenzione testuale del punto di ancoraggio, per esempio <c>bottom_center</c>.</item>
    ///   <item><b>VisualOffsetX/VisualOffsetY</b>: offset grafico futuro, senza effetto sulla cella logica.</item>
    ///   <item><b>FadeWhenActorBehind/UseShadow</b>: flag visuali futuri copiati in sola lettura.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphObjectVisualSnapshot
    {
        public readonly int ObjectId;
        public readonly string DefId;
        public readonly ArcGraphCellCoord Cell;
        public readonly string SpriteKey;
        public readonly bool IsHeld;
        public readonly int HolderActorId;
        public readonly int FoodStockUnits;
        public readonly int FootprintWidth;
        public readonly int FootprintHeight;
        public readonly string VisualKind;
        public readonly string VisualResolverKey;
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

        public bool HasFoodStock => FoodStockUnits >= 0;

        // =============================================================================
        // ArcGraphObjectVisualSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una proiezione visuale completa di un oggetto.
        /// </para>
        ///
        /// <para><b>Snapshot derivato</b></para>
        /// <para>
        /// Tutti i campi vengono copiati come valori o stringhe normalizzate. Il
        /// renderer potra' usare questi dati per creare sprite e label, ma non per
        /// riscrivere ownership, posizione, stock, porte o stato di simulazione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>objectId/defId</b>: identita' dell'oggetto.</item>
        ///   <item><b>cell</b>: coordinata visuale discreta.</item>
        ///   <item><b>spriteKey</b>: path Resources o chiave catalogo futura.</item>
        ///   <item><b>isHeld/holderActorId</b>: stato fisico copiato.</item>
        ///   <item><b>foodStockUnits</b>: <c>-1</c> se l'oggetto non ha stock associato.</item>
        /// </list>
        /// </summary>
        public ArcGraphObjectVisualSnapshot(
            int objectId,
            string defId,
            ArcGraphCellCoord cell,
            string spriteKey,
            bool isHeld,
            int holderActorId,
            int foodStockUnits)
            : this(
                objectId,
                defId,
                cell,
                spriteKey,
                isHeld,
                holderActorId,
                foodStockUnits,
                1,
                1,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                0,
                0,
                false,
                false)
        {
        }

        // =============================================================================
        // ArcGraphObjectVisualSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una proiezione visuale completa includendo anche metadati
        /// di footprint e authoring grafico letti da <c>object_defs.json</c>.
        /// </para>
        ///
        /// <para><b>Catalogo unico, snapshot passivo</b></para>
        /// <para>
        /// I dati visuali entrano in ArcGraph come copie value-only. Questo consente
        /// al futuro renderer di sapere quanto e' grande un oggetto, se avra' ombra
        /// o se dovra' diventare trasparente dietro un NPC, senza interrogare di
        /// nuovo il <c>World</c> e senza possedere regole simulativo/logiche.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>footprintWidth/footprintHeight</b>: normalizzati almeno a <c>1</c>.</item>
        ///   <item><b>visualKind/visualResolverKey</b>: stringhe normalizzate, vuote se non dichiarate.</item>
        ///   <item><b>visualWidthPixels/visualHeightPixels</b>: <c>0</c> se non dichiarati.</item>
        ///   <item><b>visualBaseWidthPixels/visualBaseHeightPixels</b>: base grafica/logica dello sprite.</item>
        ///   <item><b>visualBaseMiniTileMask</b>: stringa 2x2 normalizzata, vuota se non dichiarata.</item>
        ///   <item><b>visualPivot</b>: pivot testuale copiato dalla definizione oggetto.</item>
        ///   <item><b>visualOffsetX/visualOffsetY</b>: offset in pixel copiato senza applicazione runtime.</item>
        ///   <item><b>fadeWhenActorBehind/useShadow</b>: flag grafici futuri non ancora consumati dal renderer.</item>
        /// </list>
        /// </summary>
        public ArcGraphObjectVisualSnapshot(
            int objectId,
            string defId,
            ArcGraphCellCoord cell,
            string spriteKey,
            bool isHeld,
            int holderActorId,
            int foodStockUnits,
            int footprintWidth,
            int footprintHeight,
            int visualWidthPixels,
            int visualHeightPixels,
            int visualOffsetX,
            int visualOffsetY,
            bool fadeWhenActorBehind,
            bool useShadow)
            : this(
                objectId,
                defId,
                cell,
                spriteKey,
                isHeld,
                holderActorId,
                foodStockUnits,
                footprintWidth,
                footprintHeight,
                string.Empty,
                string.Empty,
                visualWidthPixels,
                visualHeightPixels,
                0,
                0,
                string.Empty,
                string.Empty,
                visualOffsetX,
                visualOffsetY,
                fadeWhenActorBehind,
                useShadow)
        {
        }

        // =============================================================================
        // ArcGraphObjectVisualSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una proiezione visuale completa includendo anche metadati
        /// di classificazione, base sprite e pivot.
        /// </para>
        /// </summary>
        public ArcGraphObjectVisualSnapshot(
            int objectId,
            string defId,
            ArcGraphCellCoord cell,
            string spriteKey,
            bool isHeld,
            int holderActorId,
            int foodStockUnits,
            int footprintWidth,
            int footprintHeight,
            string visualKind,
            string visualResolverKey,
            int visualWidthPixels,
            int visualHeightPixels,
            int visualBaseWidthPixels,
            int visualBaseHeightPixels,
            string visualBaseMiniTileMask,
            string visualPivot,
            int visualOffsetX,
            int visualOffsetY,
            bool fadeWhenActorBehind,
            bool useShadow)
        {
            ObjectId = objectId;
            DefId = defId ?? string.Empty;
            Cell = cell;
            SpriteKey = spriteKey ?? string.Empty;
            IsHeld = isHeld;
            HolderActorId = holderActorId;
            FoodStockUnits = foodStockUnits;
            FootprintWidth = footprintWidth <= 0 ? 1 : footprintWidth;
            FootprintHeight = footprintHeight <= 0 ? 1 : footprintHeight;
            VisualKind = visualKind ?? string.Empty;
            VisualResolverKey = visualResolverKey ?? string.Empty;
            VisualWidthPixels = visualWidthPixels < 0 ? 0 : visualWidthPixels;
            VisualHeightPixels = visualHeightPixels < 0 ? 0 : visualHeightPixels;
            VisualBaseWidthPixels = visualBaseWidthPixels < 0 ? 0 : visualBaseWidthPixels;
            VisualBaseHeightPixels = visualBaseHeightPixels < 0 ? 0 : visualBaseHeightPixels;
            VisualBaseMiniTileMask = NormalizeMiniTileMask(visualBaseMiniTileMask);
            VisualPivot = visualPivot ?? string.Empty;
            VisualOffsetX = visualOffsetX;
            VisualOffsetY = visualOffsetY;
            FadeWhenActorBehind = fadeWhenActorBehind;
            UseShadow = useShadow;
        }

        // =============================================================================
        // NormalizeMiniTileMask
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza la maschera 2x2 della base visuale.
        /// </para>
        ///
        /// <para><b>Contratto mini-tile passivo</b></para>
        /// <para>
        /// La maschera usa quattro cifre in ordine alto-sinistra, alto-destra,
        /// basso-sinistra, basso-destra. <c>1</c> significa coperto
        /// dall'oggetto; <c>0</c> significa pavimento visibile o componibile.
        /// Valori incompleti o sporchi vengono trattati come assenti.
        /// </para>
        /// </summary>
        private static string NormalizeMiniTileMask(
            string mask)
        {
            if (string.IsNullOrWhiteSpace(mask) || mask.Length != 4)
                return string.Empty;

            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] != '0' && mask[i] != '1')
                    return string.Empty;
            }

            return mask;
        }
    }
}
