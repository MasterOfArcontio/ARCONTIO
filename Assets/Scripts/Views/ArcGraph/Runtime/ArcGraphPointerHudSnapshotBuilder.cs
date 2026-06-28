namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPointerHudSnapshotBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder data-only che traduce un <c>ArcGraphInteractionFrame</c> in uno
    /// snapshot leggibile per il futuro HUD puntatore.
    /// </para>
    ///
    /// <para><b>Principio architetturale: primo consumer passivo del boundary interattivo</b></para>
    /// <para>
    /// Questo builder non decide cosa fare con il puntatore. Si limita a trasformare
    /// il frame gia' prodotto dal boundary in un messaggio stabile e testabile. In
    /// questo modo possiamo verificare l'interazione ArcGraph prima di introdurre
    /// selection, pannelli Unity, DevTools o comandi verso la simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Build</b>: crea snapshot da frame interattivo.</item>
    ///   <item><b>BuildWithAdapterDiagnostics</b>: conserva anche motivo/frame index dell'adapter.</item>
    ///   <item><b>BuildDisplayText</b>: genera testo minimale e stabile.</item>
    ///   <item><b>StoreAndReturn</b>: aggiorna la diagnostica locale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphPointerHudSnapshotBuilder
    {
        public ArcGraphPointerHudDiagnostics LastDiagnostics { get; private set; }

        // =============================================================================
        // Build
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno snapshot HUD usando solo il frame interattivo.
        /// </para>
        /// </summary>
        public ArcGraphPointerHudSnapshot Build(ArcGraphInteractionFrame frame)
        {
            return BuildInternal(frame, 0, "None");
        }

        // =============================================================================
        // BuildWithAdapterDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno snapshot HUD includendo anche la diagnostica dell'adapter
        /// scena che ha prodotto il frame.
        /// </para>
        ///
        /// <para><b>Separazione tra interazione e UI</b></para>
        /// <para>
        /// Il Pointer HUD non deve leggere direttamente il wrapper. Se il chiamante
        /// possiede gia' la diagnostica dell'adapter, puo' passarla qui come dato
        /// primitivo. Il builder resta comunque indipendente da MonoBehaviour e scena.
        /// </para>
        /// </summary>
        public ArcGraphPointerHudSnapshot BuildWithAdapterDiagnostics(
            ArcGraphInteractionFrame frame,
            ArcGraphInteractionSceneAdapterDiagnostics adapterDiagnostics)
        {
            return BuildInternal(frame, adapterDiagnostics.SourceFrameIndex, adapterDiagnostics.Reason);
        }

        // =============================================================================
        // BuildEmpty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce uno snapshot vuoto quando nessun frame interattivo e' disponibile.
        /// </para>
        /// </summary>
        public ArcGraphPointerHudSnapshot BuildEmpty(string reason)
        {
            ArcGraphPointerHudSnapshot snapshot = ArcGraphPointerHudSnapshot.Empty(reason);
            return StoreAndReturn(snapshot, reason);
        }

        private ArcGraphPointerHudSnapshot BuildInternal(
            ArcGraphInteractionFrame frame,
            long sourceFrameIndex,
            string adapterReason)
        {
            string displayText = BuildDisplayText(frame);

            var snapshot = new ArcGraphPointerHudSnapshot(
                true,
                true,
                frame.Input.HasPointerScreenPosition,
                frame.HasValidCell,
                frame.IsPointerOverUi,
                frame.Cell,
                frame.TargetKind,
                frame.ActorId,
                frame.ObjectId,
                sourceFrameIndex,
                frame.Reason,
                adapterReason,
                displayText);

            return StoreAndReturn(snapshot, frame.Reason);
        }

        private ArcGraphPointerHudSnapshot StoreAndReturn(
            ArcGraphPointerHudSnapshot snapshot,
            string reason)
        {
            LastDiagnostics = new ArcGraphPointerHudDiagnostics(
                true,
                snapshot.HasInteractionFrame,
                snapshot.HasPointer,
                snapshot.HasValidCell,
                snapshot.IsPointerOverUi,
                snapshot.TargetKind,
                snapshot.ActorId,
                snapshot.ObjectId,
                snapshot.SourceFrameIndex,
                reason,
                snapshot.DisplayText);

            return snapshot;
        }

        private static string BuildDisplayText(ArcGraphInteractionFrame frame)
        {
            if (frame.IsPointerOverUi || frame.TargetKind == ArcGraphInteractionTargetKind.UiBlocked)
                return "col -- | riga -- | UI blocked";

            if (!frame.Input.HasPointerScreenPosition)
                return "col -- | riga -- | Pointer missing";

            if (!frame.HasValidCell)
                return "col -- | riga -- | " + NormalizeReason(frame.Reason);

            string cellText = FormatCell(frame.Cell);

            switch (frame.TargetKind)
            {
                case ArcGraphInteractionTargetKind.Actor:
                    return cellText + " | Actor #" + frame.ActorId;
                case ArcGraphInteractionTargetKind.Object:
                    return cellText + " | Object #" + frame.ObjectId;
                case ArcGraphInteractionTargetKind.Plant:
                    return cellText + " | Plant #" + frame.PlantId;
                case ArcGraphInteractionTargetKind.Cell:
                    return cellText;
                default:
                    return cellText + " | " + NormalizeReason(frame.Reason);
            }
        }

        private static string FormatCell(ArcGraphCellCoord cell)
        {
            if (cell.Z == 0)
                return "col " + cell.X + " | riga " + cell.Y;

            return "col " + cell.X + " | riga " + cell.Y + " | z " + cell.Z;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }
}
