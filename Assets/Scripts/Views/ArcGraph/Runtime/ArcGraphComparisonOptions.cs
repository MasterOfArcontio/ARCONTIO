namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphComparisonOptions
    // =============================================================================
    /// <summary>
    /// <para>
    /// Opzioni di sicurezza per la modalita' comparativa ArcGraph/MapGrid.
    /// </para>
    ///
    /// <para><b>Principio architetturale: debug gate prima dell'aggancio scena</b></para>
    /// <para>
    /// Prima di montare qualunque renderer ArcGraph in scena, il chiamante deve
    /// dichiarare se la comparazione e' solo diagnostica o se chiede un probe scena
    /// temporaneo. Le opzioni rendono esplicito che MapGrid resta primario e che un
    /// doppio renderer permanente e' vietato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Mode</b>: modalita' richiesta.</item>
    ///   <item><b>KeepLegacyMapGridPrimary</b>: MapGrid resta renderer produttivo.</item>
    ///   <item><b>RequireExplicitDebugActivation</b>: vieta attivazioni implicite.</item>
    ///   <item><b>AllowSceneAttachment</b>: consente solo un probe scena temporaneo.</item>
    ///   <item><b>AllowPersistentDoubleRenderer</b>: deve restare false.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphComparisonOptions
    {
        public ArcGraphComparisonMode Mode { get; set; } = ArcGraphComparisonMode.DiagnosticsOnly;
        public bool KeepLegacyMapGridPrimary { get; set; } = true;
        public bool RequireExplicitDebugActivation { get; set; } = true;
        public bool AllowSceneAttachment { get; set; }
        public bool AllowPersistentDoubleRenderer { get; set; }

        // =============================================================================
        // CreateDiagnosticsOnly
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea opzioni per comparazione diagnostica senza aggancio scena.
        /// </para>
        /// </summary>
        public static ArcGraphComparisonOptions CreateDiagnosticsOnly()
        {
            return new ArcGraphComparisonOptions
            {
                Mode = ArcGraphComparisonMode.DiagnosticsOnly,
                KeepLegacyMapGridPrimary = true,
                RequireExplicitDebugActivation = true,
                AllowSceneAttachment = false,
                AllowPersistentDoubleRenderer = false
            };
        }

        // =============================================================================
        // CreateTemporaryDebugSceneProbe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea opzioni per un futuro probe scena temporaneo.
        /// </para>
        ///
        /// <para><b>Non attiva ancora nulla</b></para>
        /// <para>
        /// Questa factory dichiara una richiesta. Non crea oggetti scena e non
        /// aggancia renderer. Il gate diagnostico decidera' se la richiesta e'
        /// compatibile con i vincoli anti-doppio-renderer.
        /// </para>
        /// </summary>
        public static ArcGraphComparisonOptions CreateTemporaryDebugSceneProbe()
        {
            return new ArcGraphComparisonOptions
            {
                Mode = ArcGraphComparisonMode.TemporaryDebugSceneProbe,
                KeepLegacyMapGridPrimary = true,
                RequireExplicitDebugActivation = true,
                AllowSceneAttachment = true,
                AllowPersistentDoubleRenderer = false
            };
        }
    }
}
