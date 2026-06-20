namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphViewControllerResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Esito diagnostico di un aggiornamento del controller view ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: input spiegabile, non effetto nascosto</b></para>
    /// <para>
    /// Il controller view non deve modificare la view in modo opaco. Questo
    /// risultato rende leggibile cosa e' successo nel frame grafico: pan applicato,
    /// input ignorato per UI o viewport non valido. Il campo zoom resta solo per
    /// compatibilita' diagnostica e oggi resta sempre falso.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DidChangeZoom</b>: compatibilita diagnostica, oggi sempre falso.</item>
    ///   <item><b>DidApplyPan</b>: il centro vista e' stato spostato.</item>
    ///   <item><b>IgnoredBecausePointerOverUi</b>: input bloccato dalla UI.</item>
    ///   <item><b>IgnoredPanBecauseZoomDisallowsPan</b>: compatibilita legacy, oggi sempre falso.</item>
    ///   <item><b>IgnoredPanBecauseViewportInvalid</b>: impossibile convertire pixel in celle.</item>
    ///   <item><b>CenterCellX/Y</b>: centro vista finale.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphViewControllerResult
    {
        public readonly bool DidChangeZoom;
        public readonly bool DidApplyPan;
        public readonly bool IgnoredBecausePointerOverUi;
        public readonly bool IgnoredPanBecauseZoomDisallowsPan;
        public readonly bool IgnoredPanBecauseViewportInvalid;
        public readonly float CenterCellX;
        public readonly float CenterCellY;

        // =============================================================================
        // ArcGraphViewControllerResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un esito diagnostico completo.
        /// </para>
        ///
        /// <para><b>Value object</b></para>
        /// <para>
        /// L'esito contiene solo primitivi. Non conserva riferimenti a input Unity,
        /// camera, scena, stato mondo o renderer.
        /// </para>
        /// </summary>
        public ArcGraphViewControllerResult(
            bool didChangeZoom,
            bool didApplyPan,
            bool ignoredBecausePointerOverUi,
            bool ignoredPanBecauseZoomDisallowsPan,
            bool ignoredPanBecauseViewportInvalid,
            float centerCellX,
            float centerCellY)
        {
            DidChangeZoom = didChangeZoom;
            DidApplyPan = didApplyPan;
            IgnoredBecausePointerOverUi = ignoredBecausePointerOverUi;
            IgnoredPanBecauseZoomDisallowsPan = ignoredPanBecauseZoomDisallowsPan;
            IgnoredPanBecauseViewportInvalid = ignoredPanBecauseViewportInvalid;
            CenterCellX = centerCellX;
            CenterCellY = centerCellY;
        }
    }
}
