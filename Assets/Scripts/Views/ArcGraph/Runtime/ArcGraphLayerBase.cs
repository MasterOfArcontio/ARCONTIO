namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphLayerBase
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classe base minimale per i layer grafici passivi di <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: layer view-side senza authority</b></para>
    /// <para>
    /// Questa base implementa solo lifecycle, visibilita' e riferimento allo stato
    /// render condiviso. Non conosce <c>World</c>, non conosce <c>MapGridData</c>,
    /// non crea oggetti Unity e non emette comandi. I layer derivati devono quindi
    /// consumare snapshot gia' preparati dall'adapter, mantenendo una separazione
    /// netta tra simulazione e presentazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>LayerId</b>: identificatore logico fornito dal layer concreto.</item>
    ///   <item><b>IsVisible</b>: stato visuale locale, senza effetti sul World.</item>
    ///   <item><b>RenderState</b>: riferimento view-side inizializzato esplicitamente.</item>
    ///   <item><b>RefreshDirty</b>: hook virtuale per consumare dirty state.</item>
    ///   <item><b>Dispose</b>: reset locale del layer.</item>
    /// </list>
    /// </summary>
    public abstract class ArcGraphLayerBase : IArcGraphLayer
    {
        protected ArcGraphRenderState RenderState { get; private set; }

        public abstract ArcGraphLayerId LayerId { get; }
        public bool IsVisible { get; private set; } = true;
        public bool IsInitialized => RenderState != null;

        // =============================================================================
        // Initialize
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggancia il layer allo stato render condiviso.
        /// </para>
        ///
        /// <para><b>Lifecycle esplicito</b></para>
        /// <para>
        /// Il metodo non alloca risorse Unity e non legge dati simulativi. Serve solo
        /// a dichiarare con quale stato grafico il layer sta lavorando. In caso di
        /// <c>null</c>, il layer resta non inizializzato ma non solleva eccezioni:
        /// questo mantiene l'oggetto sicuro durante bootstrap parziali.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>renderState</b>: stato render view-side condiviso.</item>
        /// </list>
        /// </summary>
        public virtual void Initialize(ArcGraphRenderState renderState)
        {
            RenderState = renderState;
        }

        // =============================================================================
        // RefreshDirty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Hook chiamato quando il sistema grafico vuole consumare dirty state.
        /// </para>
        ///
        /// <para><b>No-op intenzionale</b></para>
        /// <para>
        /// La base non sa cosa significhi aggiornare terreno, oggetti o attori.
        /// Lascia quindi il metodo vuoto. I layer concreti possono sovrascriverlo
        /// per aggiornare cache visuali future, ma non devono trasformarlo in un
        /// punto di lettura diretta del <c>World</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>renderState</b>: dirty state e parametri grafici disponibili.</item>
        /// </list>
        /// </summary>
        public virtual void RefreshDirty(ArcGraphRenderState renderState)
        {
        }

        // =============================================================================
        // SetVisible
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cambia la visibilita' locale del layer.
        /// </para>
        ///
        /// <para><b>Toggle di presentazione</b></para>
        /// <para>
        /// La visibilita' non modifica snapshot, oggetti, NPC o dati del mondo.
        /// Serve soltanto al futuro renderer per sapere se un layer debba essere
        /// mostrato o ignorato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>visible</b>: nuovo stato visuale desiderato.</item>
        /// </list>
        /// </summary>
        public virtual void SetVisible(bool visible)
        {
            IsVisible = visible;
        }

        // =============================================================================
        // Dispose
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sgancia il layer dallo stato render condiviso.
        /// </para>
        ///
        /// <para><b>Cleanup view-side</b></para>
        /// <para>
        /// La base libera solo il riferimento allo stato grafico. I layer concreti
        /// devono sovrascrivere questo metodo se possiedono cache locali da svuotare.
        /// Anche in quel caso il cleanup non deve mutare la simulazione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>RenderState</b>: impostato a null.</item>
        /// </list>
        /// </summary>
        public virtual void Dispose()
        {
            RenderState = null;
        }
    }
}
