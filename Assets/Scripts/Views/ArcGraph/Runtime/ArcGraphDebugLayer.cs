namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer debug minimo per osservare il lifecycle dei layer <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica grafica senza simulazione</b></para>
    /// <para>
    /// Il debug layer non legge NPC, oggetti, credenze o job. Per ora registra solo
    /// quanti dirty cell/chunk erano presenti durante l'ultimo refresh. Questo basta
    /// a verificare che il flusso preparatorio funzioni senza introdurre overlay,
    /// UI, GameObject o dipendenze dal <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RefreshCount</b>: numero di refresh ricevuti.</item>
    ///   <item><b>LastDirtyCellCount</b>: celle dirty osservate nell'ultimo refresh.</item>
    ///   <item><b>LastDirtyChunkCount</b>: chunk dirty osservati nell'ultimo refresh.</item>
    ///   <item><b>LastRefreshHadWork</b>: indica se l'ultimo refresh aveva lavoro grafico.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugLayer : ArcGraphLayerBase
    {
        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Debug;

        public int RefreshCount { get; private set; }
        public int LastDirtyCellCount { get; private set; }
        public int LastDirtyChunkCount { get; private set; }
        public bool LastRefreshHadWork { get; private set; }

        // =============================================================================
        // RefreshDirty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una fotografia numerica del dirty state grafico corrente.
        /// </para>
        ///
        /// <para><b>Osservabilita' minima</b></para>
        /// <para>
        /// Il metodo non consuma o cancella il dirty state. Si limita a leggere i
        /// contatori gia' esposti da <c>ArcGraphDirtyState</c>. La scelta evita che
        /// il layer debug diventi proprietario del ciclo dirty: in futuro sara' il
        /// mainframe grafico a decidere quando pulire lo stato condiviso.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>renderState</b>: stato grafico osservato.</item>
        ///   <item><b>RefreshCount</b>: incrementato a ogni chiamata.</item>
        ///   <item><b>LastDirty*</b>: contatori copiati dal dirty state.</item>
        /// </list>
        /// </summary>
        public override void RefreshDirty(ArcGraphRenderState renderState)
        {
            RefreshCount++;

            if (renderState == null || renderState.Dirty == null)
            {
                LastDirtyCellCount = 0;
                LastDirtyChunkCount = 0;
                LastRefreshHadWork = false;
                return;
            }

            LastDirtyCellCount = renderState.Dirty.DirtyCellCount;
            LastDirtyChunkCount = renderState.Dirty.DirtyChunkCount;
            LastRefreshHadWork = renderState.Dirty.HasDirtyWork;
        }

        public override void Dispose()
        {
            RefreshCount = 0;
            LastDirtyCellCount = 0;
            LastDirtyChunkCount = 0;
            LastRefreshHadWork = false;
            base.Dispose();
        }
    }
}
