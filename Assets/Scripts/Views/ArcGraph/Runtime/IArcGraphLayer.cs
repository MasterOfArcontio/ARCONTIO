namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // IArcGraphLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto minimo per un layer grafico gestito da <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: layer grafici modulari</b></para>
    /// <para>
    /// Ogni layer deve sapere inizializzarsi, ricevere dirty state, aggiornare la
    /// propria presentazione e spegnersi. Il contratto non espone il <c>World</c>:
    /// sara' un adapter read-only futuro a fornire snapshot visuali gia' filtrati.
    /// Questo evita che il layer grafico diventi un secondo sistema decisionale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>LayerId</b>: identita' logica del layer.</item>
    ///   <item><b>Initialize</b>: aggancio allo stato render condiviso.</item>
    ///   <item><b>RefreshDirty</b>: consumo delle porzioni sporche.</item>
    ///   <item><b>SetVisible</b>: toggle visuale senza mutare simulazione.</item>
    ///   <item><b>Dispose</b>: rilascio risorse view-side.</item>
    /// </list>
    /// </summary>
    public interface IArcGraphLayer
    {
        ArcGraphLayerId LayerId { get; }
        bool IsVisible { get; }

        void Initialize(ArcGraphRenderState renderState);
        void RefreshDirty(ArcGraphRenderState renderState);
        void SetVisible(bool visible);
        void Dispose();
    }
}
