namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphMinimalRuntimeCoordinatorHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il contratto del coordinator runtime minimo
    /// ArcGraph senza scena Unity.
    /// </para>
    ///
    /// <para><b>Principio architetturale: QA del contratto prima del wrapper scena</b></para>
    /// <para>
    /// L'harness non crea <c>GameObject</c>, non legge input, non cerca MapGrid e
    /// non usa <c>SimulationHost</c>. Verifica solo i gate fondamentali del
    /// coordinator: frame disabilitato, context vuoto, inizializzazione interna e
    /// richiesta queue con layer disponibili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunSmoke</b>: esegue uno smoke test compatto.</item>
    ///   <item><b>RunDisabledGate</b>: verifica gate spento.</item>
    ///   <item><b>RunEmptyContextGate</b>: verifica context vuoto.</item>
    ///   <item><b>RunPartialMapContextGate</b>: verifica bootstrap con mappa minima.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphMinimalRuntimeCoordinatorHarness
    {
        // =============================================================================
        // RunSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue uno smoke test sintetico del coordinator minimo.
        /// </para>
        /// </summary>
        public static bool RunSmoke()
        {
            return RunDisabledGate()
                   && RunEmptyContextGate()
                   && RunPartialMapContextGate();
        }

        private static bool RunDisabledGate()
        {
            var coordinator = new ArcGraphMinimalRuntimeCoordinator();
            ArcGraphMinimalRuntimeCoordinatorDiagnostics diagnostics = coordinator.Process(
                ArcGraphMinimalRuntimeCoordinatorFrame.CreateDisabled());

            bool ok = diagnostics.Reason == "CoordinatorDisabled"
                      && !diagnostics.DidInitializeRuntime
                      && diagnostics.QueueEntryCount == 0;

            coordinator.Dispose();
            return ok;
        }

        private static bool RunEmptyContextGate()
        {
            var coordinator = new ArcGraphMinimalRuntimeCoordinator();
            var frame = new ArcGraphMinimalRuntimeCoordinatorFrame(
                ArcGraphRuntimeContext.Empty(),
                isCoordinatorEnabled: true,
                shouldRefreshSnapshots: true,
                shouldBuildActorObjectQueue: true,
                zoomLevel: 4);

            ArcGraphMinimalRuntimeCoordinatorDiagnostics diagnostics = coordinator.Process(frame);
            bool ok = diagnostics.Reason == "RuntimeContextEmpty"
                      && !diagnostics.DidInitializeRuntime
                      && diagnostics.QueueEntryCount == 0;

            coordinator.Dispose();
            return ok;
        }

        private static bool RunPartialMapContextGate()
        {
            var coordinator = new ArcGraphMinimalRuntimeCoordinator();
            var map = new Arcontio.View.MapGrid.MapGridData(2, 2);
            var context = new ArcGraphRuntimeContext(map: map);
            var frame = new ArcGraphMinimalRuntimeCoordinatorFrame(
                context,
                isCoordinatorEnabled: true,
                shouldRefreshSnapshots: true,
                shouldBuildActorObjectQueue: false,
                zoomLevel: 1);

            ArcGraphMinimalRuntimeCoordinatorDiagnostics diagnostics = coordinator.Process(frame);
            bool ok = diagnostics.DidInitializeRuntime
                      && diagnostics.DidRefreshSnapshots
                      && diagnostics.HasTerrainLayer
                      && diagnostics.TerrainSnapshotCount == 4
                      && diagnostics.QueueEntryCount == 0;

            coordinator.Dispose();
            return ok;
        }
    }
}
