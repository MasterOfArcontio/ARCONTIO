namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugRuntimeWiringHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test del contratto wiring runtime debug.
    /// </para>
    ///
    /// <para><b>Principio architetturale: QA senza World reale e senza scena</b></para>
    /// <para>
    /// Lo smoke test valida i gate del coordinatore senza creare GameObject e senza
    /// cercare <c>SimulationHost</c>. Non prova il render visuale e non richiede una
    /// mappa simulativa.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphDebugRuntimeWiringHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly string DisabledReason;
        public readonly string MissingContextReason;
        public readonly string MissingWorldReason;

        public ArcGraphDebugRuntimeWiringHarnessResult(
            bool passed,
            string reason,
            string disabledReason,
            string missingContextReason,
            string missingWorldReason)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            DisabledReason = string.IsNullOrWhiteSpace(disabledReason) ? "None" : disabledReason;
            MissingContextReason = string.IsNullOrWhiteSpace(missingContextReason) ? "None" : missingContextReason;
            MissingWorldReason = string.IsNullOrWhiteSpace(missingWorldReason) ? "None" : missingWorldReason;
        }
    }

    // =============================================================================
    // ArcGraphDebugRuntimeWiringHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il contratto di wiring runtime debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: validazione dei gate prima del runtime</b></para>
    /// <para>
    /// Il test verifica che il coordinatore non faccia nulla quando overlay,
    /// context o World mancano. Questo e' il comportamento piu' importante prima
    /// di collegare un wrapper scena reale.
    /// </para>
    /// </summary>
    public static class ArcGraphDebugRuntimeWiringHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test minimo del contratto wiring runtime debug.
        /// </para>
        /// </summary>
        public static ArcGraphDebugRuntimeWiringHarnessResult RunDefaultSmoke()
        {
            var coordinator = new ArcGraphDebugRuntimeWiringCoordinator();

            ArcGraphDebugRuntimeWiringDiagnostics disabled =
                coordinator.Process(ArcGraphDebugRuntimeWiringFrame.CreateDisabled());

            var missingContextFrame = new ArcGraphDebugRuntimeWiringFrame(
                context: null,
                activeNpcId: 1,
                options: ArcGraphDebugOverlayRuntimeFeedOptions.CreateDefault(),
                isOverlayEnabled: true,
                shouldDispatchToConsumer: true);

            ArcGraphDebugRuntimeWiringDiagnostics missingContext =
                coordinator.Process(missingContextFrame);

            var missingWorldFrame = new ArcGraphDebugRuntimeWiringFrame(
                context: ArcGraphRuntimeContext.Empty(),
                activeNpcId: 1,
                options: ArcGraphDebugOverlayRuntimeFeedOptions.CreateDefault(),
                isOverlayEnabled: true,
                shouldDispatchToConsumer: true);

            ArcGraphDebugRuntimeWiringDiagnostics missingWorld =
                coordinator.Process(missingWorldFrame);

            bool passed = disabled.Reason == "OverlayDisabled"
                          && !disabled.DidBuildFeed
                          && missingContext.Reason == "RuntimeContextMissing"
                          && !missingContext.DidBuildFeed
                          && missingWorld.Reason == "WorldMissing"
                          && !missingWorld.DidBuildFeed
                          && !missingWorld.DidDispatchToConsumer;

            return new ArcGraphDebugRuntimeWiringHarnessResult(
                passed,
                passed ? "DebugRuntimeWiringSmokePassed" : "DebugRuntimeWiringSmokeFailed",
                disabled.Reason,
                missingContext.Reason,
                missingWorld.Reason);
        }
    }
}
