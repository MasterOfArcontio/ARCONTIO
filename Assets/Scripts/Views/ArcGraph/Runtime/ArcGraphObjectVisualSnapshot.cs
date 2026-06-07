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
        {
            ObjectId = objectId;
            DefId = defId ?? string.Empty;
            Cell = cell;
            SpriteKey = spriteKey ?? string.Empty;
            IsHeld = isHeld;
            HolderActorId = holderActorId;
            FoodStockUnits = foodStockUnits;
        }
    }
}
