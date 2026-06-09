namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPointerHudSnapshotBuilderHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato dello smoke test del Pointer HUD passivo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: QA data-only</b></para>
    /// <para>
    /// Il risultato permette di verificare testo cella, actor e blocco UI senza
    /// creare Canvas, Text, GameObject, scene o wrapper Unity.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphPointerHudSnapshotBuilderHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly string ActorText;
        public readonly string CellText;
        public readonly string UiText;
        public readonly string EmptyText;

        public ArcGraphPointerHudSnapshotBuilderHarnessResult(
            bool passed,
            string reason,
            string actorText,
            string cellText,
            string uiText,
            string emptyText)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            ActorText = actorText ?? string.Empty;
            CellText = cellText ?? string.Empty;
            UiText = uiText ?? string.Empty;
            EmptyText = emptyText ?? string.Empty;
        }
    }

    // =============================================================================
    // ArcGraphPointerHudSnapshotBuilderHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico del builder Pointer HUD.
    /// </para>
    ///
    /// <para><b>Principio architetturale: consumer verificabile prima della UI</b></para>
    /// <para>
    /// Lo smoke test costruisce frame interattivi artificiali e verifica che il
    /// builder produca snapshot leggibili. Questo consente di bloccare il contratto
    /// dati prima di introdurre pannelli Unity o collegamenti runtime.
    /// </para>
    /// </summary>
    public static class ArcGraphPointerHudSnapshotBuilderHarness
    {
        public static ArcGraphPointerHudSnapshotBuilderHarnessResult RunDefaultSmoke()
        {
            var builder = new ArcGraphPointerHudSnapshotBuilder();

            ArcGraphPointerHudSnapshot actorSnapshot = builder.Build(CreateActorFrame());
            ArcGraphPointerHudSnapshot cellSnapshot = builder.Build(CreateCellFrame());
            ArcGraphPointerHudSnapshot uiSnapshot = builder.Build(CreateUiBlockedFrame());
            ArcGraphPointerHudSnapshot emptySnapshot = builder.BuildEmpty("HarnessEmpty");

            bool passed = actorSnapshot.DisplayText == "Cell: 12,14 | Actor #7"
                          && actorSnapshot.HasActor
                          && cellSnapshot.DisplayText == "Cell: 2,3"
                          && cellSnapshot.TargetKind == ArcGraphInteractionTargetKind.Cell
                          && uiSnapshot.DisplayText == "Cell: -,- | UI blocked"
                          && uiSnapshot.IsPointerOverUi
                          && emptySnapshot.DisplayText == "Cell: -,-"
                          && !emptySnapshot.HasInteractionFrame;

            return new ArcGraphPointerHudSnapshotBuilderHarnessResult(
                passed,
                passed ? "PointerHudSnapshotSmokePassed" : "PointerHudSnapshotSmokeFailed",
                actorSnapshot.DisplayText,
                cellSnapshot.DisplayText,
                uiSnapshot.DisplayText,
                emptySnapshot.DisplayText);
        }

        private static ArcGraphInteractionFrame CreateActorFrame()
        {
            var cell = new ArcGraphCellCoord(12, 14, 0);
            return new ArcGraphInteractionFrame(
                CreateInput(false),
                CreateCoordinate(cell),
                ArcGraphInteractionTargetKind.Actor,
                cell,
                7,
                -1,
                true,
                false,
                "ActorPicked");
        }

        private static ArcGraphInteractionFrame CreateCellFrame()
        {
            var cell = new ArcGraphCellCoord(2, 3, 0);
            return new ArcGraphInteractionFrame(
                CreateInput(false),
                CreateCoordinate(cell),
                ArcGraphInteractionTargetKind.Cell,
                cell,
                -1,
                -1,
                true,
                false,
                "CellPicked");
        }

        private static ArcGraphInteractionFrame CreateUiBlockedFrame()
        {
            return new ArcGraphInteractionFrame(
                CreateInput(true),
                ArcGraphViewCoordinateResult.Invalid("PointerOverUi"),
                ArcGraphInteractionTargetKind.UiBlocked,
                new ArcGraphCellCoord(0, 0, 0),
                -1,
                -1,
                false,
                true,
                "PointerOverUi");
        }

        private static ArcGraphViewInputFrame CreateInput(bool isPointerOverUi)
        {
            return new ArcGraphViewInputFrame(
                wheelStepDelta: 0,
                isMiddleMouseHeld: false,
                mouseDeltaPixelsX: 0f,
                mouseDeltaPixelsY: 0f,
                pointerScreenX: 10f,
                pointerScreenY: 10f,
                hasPointerScreenPosition: true,
                isPointerOverUi: isPointerOverUi);
        }

        private static ArcGraphViewCoordinateResult CreateCoordinate(ArcGraphCellCoord cell)
        {
            return new ArcGraphViewCoordinateResult(
                true,
                cell,
                0.5f,
                0.5f,
                new ArcGraphViewCellRect(0, 0, 20, 20),
                "HarnessCoordinate");
        }
    }
}
