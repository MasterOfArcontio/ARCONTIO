namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEffectRenderQueueDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica della queue effetti ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' senza sistema incendi</b></para>
    /// <para>
    /// La diagnostica conta gli snapshot effetto trasformati in item visuali, gli
    /// item nascosti, quelli animabili e quelli trattati come segnale statico.
    /// Non misura propagazione, danni, calore, fumo fisico, luce reale o percezione
    /// NPC: tutte responsabilita' di sistemi esterni futuri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SnapshotCount</b>: snapshot letti dal layer.</item>
    ///   <item><b>VisibleItemCount</b>: item effetto visibili.</item>
    ///   <item><b>HiddenItemCount</b>: item effetto nascosti.</item>
    ///   <item><b>AnimatedItemCount</b>: item che ammettono animazione frame-based ArcGraph.</item>
    ///   <item><b>StaticSignalCount</b>: item visibili ridotti a segnale statico.</item>
    ///   <item><b>MaxIntensity01</b>: massima intensita' visuale incontrata.</item>
    ///   <item><b>Reason</b>: descrizione sintetica dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEffectRenderQueueDiagnostics
    {
        public readonly int SnapshotCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly int AnimatedItemCount;
        public readonly int StaticSignalCount;
        public readonly float MaxIntensity01;
        public readonly string Reason;

        public ArcGraphEffectRenderQueueDiagnostics(
            int snapshotCount,
            int visibleItemCount,
            int hiddenItemCount,
            int animatedItemCount,
            int staticSignalCount,
            float maxIntensity01,
            string reason)
        {
            SnapshotCount = snapshotCount < 0 ? 0 : snapshotCount;
            VisibleItemCount = visibleItemCount < 0 ? 0 : visibleItemCount;
            HiddenItemCount = hiddenItemCount < 0 ? 0 : hiddenItemCount;
            AnimatedItemCount = animatedItemCount < 0 ? 0 : animatedItemCount;
            StaticSignalCount = staticSignalCount < 0 ? 0 : staticSignalCount;
            MaxIntensity01 = Clamp01(maxIntensity01);
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }
    }
}
