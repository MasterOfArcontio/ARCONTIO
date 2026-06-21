namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphInteractionSceneFrame
    // =============================================================================
    /// <summary>
    /// <para>
    /// Frame di ingresso passivo per il futuro adapter scena interattivo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: la scena raccoglie input, ArcGraph riceve valori</b></para>
    /// <para>
    /// Questo value object rappresenta il pacchetto minimo che un componente Unity
    /// futuro potra' costruire dopo aver letto mouse, rotellina, viewport e stato UI.
    /// Non contiene riferimenti a <c>Mouse.current</c>, <c>Camera</c>,
    /// <c>EventSystem</c>, <c>SimulationHost</c>, <c>World</c> o oggetti scena.
    /// In questo modo il contratto resta verificabile tramite harness senza avviare
    /// una scena Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Input</b>: input view gia' normalizzato.</item>
    ///   <item><b>ViewportPixelWidth/Height</b>: dimensioni del viewport ArcGraph.</item>
    ///   <item><b>ShouldDispatchToConsumer</b>: autorizza la consegna a consumer esterni.</item>
    ///   <item><b>SceneResolvedCell</b>: cella gia' risolta dal wrapper Unity quando la camera e' disponibile.</item>
    ///   <item><b>SourceFrameIndex</b>: indice diagnostico opzionale del frame sorgente.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphInteractionSceneFrame
    {
        public readonly ArcGraphViewInputFrame Input;
        public readonly int ViewportPixelWidth;
        public readonly int ViewportPixelHeight;
        public readonly bool ShouldDispatchToConsumer;
        public readonly long SourceFrameIndex;
        public readonly bool HasSceneResolvedCell;
        public readonly ArcGraphCellCoord SceneResolvedCell;

        public bool HasValidViewport => ViewportPixelWidth > 0 && ViewportPixelHeight > 0;

        // =============================================================================
        // ArcGraphInteractionSceneFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame scena con input e dimensioni viewport esplicite.
        /// </para>
        /// </summary>
        public ArcGraphInteractionSceneFrame(
            ArcGraphViewInputFrame input,
            int viewportPixelWidth,
            int viewportPixelHeight,
            bool shouldDispatchToConsumer,
            long sourceFrameIndex = -1)
            : this(
                input,
                viewportPixelWidth,
                viewportPixelHeight,
                shouldDispatchToConsumer,
                sourceFrameIndex,
                false,
                new ArcGraphCellCoord(0, 0, 0))
        {
        }

        // =============================================================================
        // ArcGraphInteractionSceneFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame scena con una cella gia' risolta dal wrapper Unity.
        /// </para>
        ///
        /// <para><b>Principio architetturale: una sola cella autorevole per hover e selection</b></para>
        /// <para>
        /// Quando la scena dispone di una camera ortografica reale, il wrapper puo'
        /// convertire il puntatore in world-space prima di chiamare il contratto
        /// passivo. Il contratto resta comunque testabile: riceve solo una cella
        /// value-type, non un riferimento alla camera Unity.
        /// </para>
        /// </summary>
        public ArcGraphInteractionSceneFrame(
            ArcGraphViewInputFrame input,
            int viewportPixelWidth,
            int viewportPixelHeight,
            bool shouldDispatchToConsumer,
            long sourceFrameIndex,
            bool hasSceneResolvedCell,
            ArcGraphCellCoord sceneResolvedCell)
        {
            Input = input;
            ViewportPixelWidth = viewportPixelWidth;
            ViewportPixelHeight = viewportPixelHeight;
            ShouldDispatchToConsumer = shouldDispatchToConsumer;
            SourceFrameIndex = sourceFrameIndex;
            HasSceneResolvedCell = hasSceneResolvedCell;
            SceneResolvedCell = sceneResolvedCell;
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un frame scena vuoto, utile per diagnostica o harness.
        /// </para>
        /// </summary>
        public static ArcGraphInteractionSceneFrame Empty()
        {
            return new ArcGraphInteractionSceneFrame(
                ArcGraphViewInputFrame.Empty(),
                0,
                0,
                false,
                -1);
        }
    }
}
