namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphBootstrapOptions
    // =============================================================================
    /// <summary>
    /// <para>
    /// Opzioni esplicite per l'accensione controllata del nucleo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: policy dichiarata, non nascosta nella scena</b></para>
    /// <para>
    /// Le opzioni impediscono al bootstrap di dedurre comportamento dalla scena o
    /// dal renderer legacy. Il chiamante deve dichiarare se ArcGraph e' disabilitato,
    /// se deve popolare snapshot interni e se puo' tollerare un context incompleto.
    /// In <c>v0.31</c> nessuna opzione abilita rendering produttivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActivationMode</b>: accensione disabilitata o solo interna.</item>
    ///   <item><b>IncludeFuturePlaceholderLayers</b>: registra placeholder futuri solo su richiesta.</item>
    ///   <item><b>PopulateInitialSnapshots</b>: copia snapshot iniziali se le sorgenti esistono.</item>
    ///   <item><b>AllowPartialRuntimeContext</b>: consente bootstrap anche con dati mancanti.</item>
    ///   <item><b>DefaultTileSizeWorld</b>: fallback per scala cella/world.</item>
    ///   <item><b>DefaultChunkSizeCells</b>: fallback per dimensione chunk.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphBootstrapOptions
    {
        public ArcGraphBootstrapActivationMode ActivationMode { get; set; } =
            ArcGraphBootstrapActivationMode.InternalStateOnly;

        public bool IncludeFuturePlaceholderLayers { get; set; }
        public bool PopulateInitialSnapshots { get; set; } = true;
        public bool AllowPartialRuntimeContext { get; set; } = true;

        public int VisibleZLevel { get; set; } = ArcGraphZLevelPolicy.DefaultVisibleZLevel;
        public float DefaultTileSizeWorld { get; set; } = 1f;
        public int DefaultChunkSizeCells { get; set; } = 16;

        // =============================================================================
        // CreateDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea le opzioni standard per <c>v0.31</c>.
        /// </para>
        ///
        /// <para><b>Default conservativo</b></para>
        /// <para>
        /// Il default accende solo lo stato interno, registra i layer foundation,
        /// permette context parziale e copia snapshot iniziali quando disponibili.
        /// Non include i placeholder futuri e non abilita alcun rendering.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>new ArcGraphBootstrapOptions</b>: ritorna una nuova istanza mutabile dal chiamante.</item>
        /// </list>
        /// </summary>
        public static ArcGraphBootstrapOptions CreateDefault()
        {
            return new ArcGraphBootstrapOptions();
        }

        // =============================================================================
        // CreateDisabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea opzioni che dichiarano il bootstrap esplicitamente spento.
        /// </para>
        ///
        /// <para><b>Disattivazione esplicita</b></para>
        /// <para>
        /// Questa factory serve a verificare la policy senza introdurre flag
        /// impliciti nella scena. Il runtime riceve comunque una chiamata, ma
        /// restituisce diagnostica <c>Disabled</c> e non crea stato ArcGraph.
        /// </para>
        /// </summary>
        public static ArcGraphBootstrapOptions CreateDisabled()
        {
            return new ArcGraphBootstrapOptions
            {
                ActivationMode = ArcGraphBootstrapActivationMode.Disabled
            };
        }
    }
}
