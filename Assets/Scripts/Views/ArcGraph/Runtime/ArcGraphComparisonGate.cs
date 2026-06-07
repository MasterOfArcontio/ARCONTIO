namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphComparisonGate
    // =============================================================================
    /// <summary>
    /// <para>
    /// Gate di sicurezza per la modalita' comparativa ArcGraph/MapGrid.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bloccare prima di montare</b></para>
    /// <para>
    /// La comparazione visuale puo' diventare pericolosa se aggancia ArcGraph alla
    /// scena in modo permanente mentre MapGrid resta attivo. Questo gate valuta
    /// prerequisiti e policy prima che un futuro wrapper Unity possa creare oggetti,
    /// materiali o camera bridge.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Evaluate</b>: valuta una richiesta di comparazione.</item>
    ///   <item><b>Allow/Block</b>: costruiscono diagnostica coerente.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphComparisonGate
    {
        // =============================================================================
        // Evaluate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valuta se una modalita' comparativa puo' essere usata.
        /// </para>
        ///
        /// <para><b>Input primitivi e dichiarativi</b></para>
        /// <para>
        /// Il metodo non cerca renderer nella scena. Riceve booleani dichiarati da un
        /// chiamante futuro: esiste il renderer legacy, esistono dati terrain
        /// ArcGraph, esiste camera, esiste materiale. Questo mantiene il gate
        /// testabile e privo di dipendenze Unity operative.
        /// </para>
        /// </summary>
        public static ArcGraphComparisonDiagnostics Evaluate(
            ArcGraphComparisonOptions options,
            bool hasLegacyRenderer,
            bool hasArcGraphTerrainData,
            bool hasCamera,
            bool hasMaterial)
        {
            options = options ?? ArcGraphComparisonOptions.CreateDiagnosticsOnly();

            if (options.Mode == ArcGraphComparisonMode.Disabled)
            {
                return Block(
                    "ComparisonDisabled",
                    options,
                    hasLegacyRenderer,
                    hasArcGraphTerrainData,
                    hasCamera,
                    hasMaterial,
                    false);
            }

            if (!options.KeepLegacyMapGridPrimary)
            {
                return Block(
                    "LegacyMapGridMustRemainPrimary",
                    options,
                    hasLegacyRenderer,
                    hasArcGraphTerrainData,
                    hasCamera,
                    hasMaterial,
                    false);
            }

            if (options.AllowPersistentDoubleRenderer)
            {
                return Block(
                    "PersistentDoubleRendererForbidden",
                    options,
                    hasLegacyRenderer,
                    hasArcGraphTerrainData,
                    hasCamera,
                    hasMaterial,
                    true);
            }

            if (!hasLegacyRenderer)
            {
                return Block(
                    "LegacyRendererMissing",
                    options,
                    hasLegacyRenderer,
                    hasArcGraphTerrainData,
                    hasCamera,
                    hasMaterial,
                    false);
            }

            if (!hasArcGraphTerrainData)
            {
                return Block(
                    "ArcGraphTerrainDataMissing",
                    options,
                    hasLegacyRenderer,
                    hasArcGraphTerrainData,
                    hasCamera,
                    hasMaterial,
                    false);
            }

            if (options.Mode == ArcGraphComparisonMode.DiagnosticsOnly)
            {
                return Allow(
                    "DiagnosticsOnlyAllowed",
                    canAttachSceneProbe: false,
                    options,
                    hasLegacyRenderer,
                    hasArcGraphTerrainData,
                    hasCamera,
                    hasMaterial);
            }

            if (options.Mode == ArcGraphComparisonMode.TemporaryDebugSceneProbe)
            {
                if (!options.RequireExplicitDebugActivation)
                {
                    return Block(
                        "ExplicitDebugActivationRequired",
                        options,
                        hasLegacyRenderer,
                        hasArcGraphTerrainData,
                        hasCamera,
                        hasMaterial,
                        false);
                }

                if (!options.AllowSceneAttachment)
                {
                    return Block(
                        "SceneAttachmentNotAllowed",
                        options,
                        hasLegacyRenderer,
                        hasArcGraphTerrainData,
                        hasCamera,
                        hasMaterial,
                        false);
                }

                if (!hasCamera)
                {
                    return Block(
                        "CameraMissingForSceneProbe",
                        options,
                        hasLegacyRenderer,
                        hasArcGraphTerrainData,
                        hasCamera,
                        hasMaterial,
                        false);
                }

                if (!hasMaterial)
                {
                    return Block(
                        "MaterialMissingForSceneProbe",
                        options,
                        hasLegacyRenderer,
                        hasArcGraphTerrainData,
                        hasCamera,
                        hasMaterial,
                        false);
                }

                return Allow(
                    "TemporaryDebugSceneProbeAllowed",
                    canAttachSceneProbe: true,
                    options,
                    hasLegacyRenderer,
                    hasArcGraphTerrainData,
                    hasCamera,
                    hasMaterial);
            }

            return Block(
                "UnknownComparisonMode",
                options,
                hasLegacyRenderer,
                hasArcGraphTerrainData,
                hasCamera,
                hasMaterial,
                false);
        }

        private static ArcGraphComparisonDiagnostics Allow(
            string reason,
            bool canAttachSceneProbe,
            ArcGraphComparisonOptions options,
            bool hasLegacyRenderer,
            bool hasArcGraphTerrainData,
            bool hasCamera,
            bool hasMaterial)
        {
            return new ArcGraphComparisonDiagnostics(
                true,
                canAttachSceneProbe,
                reason,
                options.Mode,
                hasLegacyRenderer,
                hasArcGraphTerrainData,
                hasCamera,
                hasMaterial,
                false);
        }

        private static ArcGraphComparisonDiagnostics Block(
            string reason,
            ArcGraphComparisonOptions options,
            bool hasLegacyRenderer,
            bool hasArcGraphTerrainData,
            bool hasCamera,
            bool hasMaterial,
            bool wouldCreatePersistentDoubleRenderer)
        {
            return new ArcGraphComparisonDiagnostics(
                false,
                false,
                reason,
                options.Mode,
                hasLegacyRenderer,
                hasArcGraphTerrainData,
                hasCamera,
                hasMaterial,
                wouldCreatePersistentDoubleRenderer);
        }
    }
}
