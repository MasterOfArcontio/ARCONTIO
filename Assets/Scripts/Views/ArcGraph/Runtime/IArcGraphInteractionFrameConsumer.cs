namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // IArcGraphInteractionFrameConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Consumer passivo di frame interazione ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tool esterni, renderer non proprietario</b></para>
    /// <para>
    /// I futuri moduli come SelectionTool, PointerHud o DevTools potranno ricevere
    /// un frame interattivo senza essere posseduti da ArcGraph. L'interfaccia non
    /// definisce comandi, selection o UI: espone solo la consegna di un dato gia'
    /// calcolato, accompagnato dalla diagnostica del contratto scena.
    /// </para>
    /// </summary>
    public interface IArcGraphInteractionFrameConsumer
    {
        // =============================================================================
        // ConsumeInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riceve il frame interattivo corrente e la diagnostica associata.
        /// </para>
        /// </summary>
        void ConsumeInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics);
    }
}
