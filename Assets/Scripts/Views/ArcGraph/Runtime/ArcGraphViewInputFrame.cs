namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewInputFrame
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot astratto dell'input view ricevuto in un singolo frame grafico.
    /// </para>
    ///
    /// <para><b>Principio architetturale: input ricevuto, non letto globalmente</b></para>
    /// <para>
    /// Il futuro controller ArcGraph non deve spargere letture dirette di mouse,
    /// tastiera o UI dentro i renderer. Un wrapper Unity potra' leggere
    /// <c>Mouse.current</c> e trasformarlo in questo snapshot, mentre il contratto
    /// ArcGraph resta testabile con valori primitivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>WheelStepDelta</b>: scatti logici della rotellina.</item>
    ///   <item><b>IsMiddleMouseHeld</b>: pan attivo se la policy lo consente.</item>
    ///   <item><b>MouseDeltaPixelsX/Y</b>: movimento mouse in pixel del frame.</item>
    ///   <item><b>PointerScreenX/Y</b>: posizione puntatore, se disponibile.</item>
    ///   <item><b>IsPointerOverUi</b>: blocco input quando la UI ha priorita'.</item>
    ///   <item><b>IsPrimaryPointerPressedThisFrame</b>: click primario normalizzato per consumer selection/debug.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphViewInputFrame
    {
        public readonly int WheelStepDelta;
        public readonly bool IsMiddleMouseHeld;
        public readonly float MouseDeltaPixelsX;
        public readonly float MouseDeltaPixelsY;
        public readonly float PointerScreenX;
        public readonly float PointerScreenY;
        public readonly bool HasPointerScreenPosition;
        public readonly bool IsPointerOverUi;
        public readonly bool IsPrimaryPointerPressedThisFrame;

        // =============================================================================
        // ArcGraphViewInputFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno snapshot input view completo.
        /// </para>
        ///
        /// <para><b>Value object di frontiera</b></para>
        /// <para>
        /// Tutti i campi sono primitivi. Lo snapshot non conserva riferimenti a
        /// device Unity, action map, camera, UI o scene object.
        /// </para>
        /// </summary>
        public ArcGraphViewInputFrame(
            int wheelStepDelta,
            bool isMiddleMouseHeld,
            float mouseDeltaPixelsX,
            float mouseDeltaPixelsY,
            float pointerScreenX,
            float pointerScreenY,
            bool hasPointerScreenPosition,
            bool isPointerOverUi,
            bool isPrimaryPointerPressedThisFrame = false)
        {
            WheelStepDelta = wheelStepDelta;
            IsMiddleMouseHeld = isMiddleMouseHeld;
            MouseDeltaPixelsX = mouseDeltaPixelsX;
            MouseDeltaPixelsY = mouseDeltaPixelsY;
            PointerScreenX = pointerScreenX;
            PointerScreenY = pointerScreenY;
            HasPointerScreenPosition = hasPointerScreenPosition;
            IsPointerOverUi = isPointerOverUi;
            IsPrimaryPointerPressedThisFrame = isPrimaryPointerPressedThisFrame;
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un frame input vuoto.
        /// </para>
        ///
        /// <para><b>Fallback testabile</b></para>
        /// <para>
        /// Il frame vuoto permette a harness e controller futuri di eseguire un tick
        /// view senza simulare input fisico.
        /// </para>
        /// </summary>
        public static ArcGraphViewInputFrame Empty()
        {
            return new ArcGraphViewInputFrame(
                0,
                false,
                0f,
                0f,
                0f,
                0f,
                false,
                false);
        }
    }
}
