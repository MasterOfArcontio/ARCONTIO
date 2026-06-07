namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphComparisonDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica del gate comparativo ArcGraph/MapGrid.
    /// </para>
    ///
    /// <para><b>Principio architetturale: confronto spiegabile</b></para>
    /// <para>
    /// Il gate non deve limitarsi a dire si/no. Deve spiegare se la comparazione e'
    /// bloccata per assenza di legacy, assenza di dati ArcGraph, mancanza di camera
    /// o materiale per probe scena, oppure per rischio di doppio renderer
    /// permanente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsAllowed</b>: la modalita' richiesta e' ammessa.</item>
    ///   <item><b>CanAttachSceneProbe</b>: il futuro aggancio scena temporaneo e' ammesso.</item>
    ///   <item><b>Reason</b>: motivo principale.</item>
    ///   <item><b>HasLegacyRenderer/HasArcGraphTerrainData</b>: sorgenti di confronto.</item>
    ///   <item><b>HasCamera/HasMaterial</b>: prerequisiti per probe scena.</item>
    ///   <item><b>WouldCreatePersistentDoubleRenderer</b>: rischio vietato.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphComparisonDiagnostics
    {
        public readonly bool IsAllowed;
        public readonly bool CanAttachSceneProbe;
        public readonly string Reason;
        public readonly ArcGraphComparisonMode Mode;
        public readonly bool HasLegacyRenderer;
        public readonly bool HasArcGraphTerrainData;
        public readonly bool HasCamera;
        public readonly bool HasMaterial;
        public readonly bool WouldCreatePersistentDoubleRenderer;

        // =============================================================================
        // ArcGraphComparisonDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica comparativa completa.
        /// </para>
        /// </summary>
        public ArcGraphComparisonDiagnostics(
            bool isAllowed,
            bool canAttachSceneProbe,
            string reason,
            ArcGraphComparisonMode mode,
            bool hasLegacyRenderer,
            bool hasArcGraphTerrainData,
            bool hasCamera,
            bool hasMaterial,
            bool wouldCreatePersistentDoubleRenderer)
        {
            IsAllowed = isAllowed;
            CanAttachSceneProbe = canAttachSceneProbe;
            Reason = reason ?? string.Empty;
            Mode = mode;
            HasLegacyRenderer = hasLegacyRenderer;
            HasArcGraphTerrainData = hasArcGraphTerrainData;
            HasCamera = hasCamera;
            HasMaterial = hasMaterial;
            WouldCreatePersistentDoubleRenderer = wouldCreatePersistentDoubleRenderer;
        }
    }
}
