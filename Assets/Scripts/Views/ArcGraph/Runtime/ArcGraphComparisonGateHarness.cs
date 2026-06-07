namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphComparisonGateHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il gate comparativo ArcGraph/MapGrid.
    /// </para>
    ///
    /// <para><b>Principio architetturale: sicurezza comparativa testabile</b></para>
    /// <para>
    /// Il gate viene verificato senza scena e senza renderer reali. In questo modo
    /// la policy anti-doppio-renderer viene stabilita prima del futuro bridge Unity.
    /// </para>
    /// </summary>
    public static class ArcGraphComparisonGateHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica i casi base del gate comparativo.
        /// </para>
        /// </summary>
        public static bool RunDefaultSmoke(out string failureReason)
        {
            failureReason = string.Empty;

            var diagnosticsOnly = ArcGraphComparisonGate.Evaluate(
                ArcGraphComparisonOptions.CreateDiagnosticsOnly(),
                hasLegacyRenderer: true,
                hasArcGraphTerrainData: true,
                hasCamera: false,
                hasMaterial: false);

            if (!diagnosticsOnly.IsAllowed || diagnosticsOnly.CanAttachSceneProbe)
                return Fail("Expected diagnostics-only comparison to be allowed without scene probe.", out failureReason);

            var forbiddenPersistent = ArcGraphComparisonOptions.CreateTemporaryDebugSceneProbe();
            forbiddenPersistent.AllowPersistentDoubleRenderer = true;

            var persistentResult = ArcGraphComparisonGate.Evaluate(
                forbiddenPersistent,
                hasLegacyRenderer: true,
                hasArcGraphTerrainData: true,
                hasCamera: true,
                hasMaterial: true);

            if (persistentResult.IsAllowed || !persistentResult.WouldCreatePersistentDoubleRenderer)
                return Fail("Expected persistent double renderer request to be blocked.", out failureReason);

            var sceneProbe = ArcGraphComparisonGate.Evaluate(
                ArcGraphComparisonOptions.CreateTemporaryDebugSceneProbe(),
                hasLegacyRenderer: true,
                hasArcGraphTerrainData: true,
                hasCamera: true,
                hasMaterial: true);

            if (!sceneProbe.IsAllowed || !sceneProbe.CanAttachSceneProbe)
                return Fail("Expected temporary scene probe to be allowed when prerequisites exist.", out failureReason);

            var missingMaterial = ArcGraphComparisonGate.Evaluate(
                ArcGraphComparisonOptions.CreateTemporaryDebugSceneProbe(),
                hasLegacyRenderer: true,
                hasArcGraphTerrainData: true,
                hasCamera: true,
                hasMaterial: false);

            if (missingMaterial.IsAllowed || missingMaterial.Reason != "MaterialMissingForSceneProbe")
                return Fail("Expected missing material to block scene probe.", out failureReason);

            return true;
        }

        private static bool Fail(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }
    }
}
