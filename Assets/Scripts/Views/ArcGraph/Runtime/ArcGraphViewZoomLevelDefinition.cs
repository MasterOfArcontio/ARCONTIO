namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewZoomLevelDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione discreta di un livello zoom della view ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: zoom discreto, non zoom libero</b></para>
    /// <para>
    /// ArcGraph non deve nascere con uno zoom continuo copiato dal controller
    /// legacy MapGrid. La view lavora invece con livelli espliciti, leggibili e
    /// configurabili. Ogni livello dichiara quante celle vorrebbe mostrare e quale
    /// complessita' visuale e' ammessa a quel grado di distanza.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Level</b>: indice umano del livello zoom, normalmente 1..4.</item>
    ///   <item><b>VisibleCellsX/Y</b>: ampiezza richiesta della finestra in celle.</item>
    ///   <item><b>AllowsPan</b>: autorizzazione policy per spostare la vista.</item>
    ///   <item><b>AllowsSpriteAnimation</b>: abilita animazioni sprite a questo livello.</item>
    ///   <item><b>AllowsLayeredActorSprites</b>: abilita vestizione actor a layer.</item>
    ///   <item><b>UsesSimplifiedRepresentation</b>: usa icone, statici o aggregazioni.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphViewZoomLevelDefinition
    {
        public readonly int Level;
        public readonly int VisibleCellsX;
        public readonly int VisibleCellsY;
        public readonly bool AllowsPan;
        public readonly bool AllowsSpriteAnimation;
        public readonly bool AllowsLayeredActorSprites;
        public readonly bool UsesSimplifiedRepresentation;

        // =============================================================================
        // ArcGraphViewZoomLevelDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione zoom normalizzata.
        /// </para>
        ///
        /// <para><b>Normalizzazione conservativa</b></para>
        /// <para>
        /// I valori non validi vengono ricondotti a minimi sicuri. Questo permette
        /// di usare il contratto anche durante test o caricamenti parziali della
        /// configurazione, senza creare finestre con zero celle visibili.
        /// </para>
        /// </summary>
        public ArcGraphViewZoomLevelDefinition(
            int level,
            int visibleCellsX,
            int visibleCellsY,
            bool allowsPan,
            bool allowsSpriteAnimation,
            bool allowsLayeredActorSprites,
            bool usesSimplifiedRepresentation)
        {
            Level = level > 0 ? level : 1;
            VisibleCellsX = visibleCellsX > 0 ? visibleCellsX : 1;
            VisibleCellsY = visibleCellsY > 0 ? visibleCellsY : 1;
            AllowsPan = allowsPan;
            AllowsSpriteAnimation = allowsSpriteAnimation;
            AllowsLayeredActorSprites = allowsLayeredActorSprites;
            UsesSimplifiedRepresentation = usesSimplifiedRepresentation;
        }

        public override string ToString()
        {
            return $"zoom{Level} visible={VisibleCellsX}x{VisibleCellsY} pan={AllowsPan}";
        }
    }
}
