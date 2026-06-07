namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphLightRenderQueueDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica della queue luce ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' senza sistema luce</b></para>
    /// <para>
    /// La diagnostica conta snapshot luce, item visibili, item nascosti, sorgenti
    /// locali e celle scure. Non misura raggi, occlusione, rimbalzi, percezione NPC
    /// o propagazione attraverso stanze e muri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SnapshotCount</b>: snapshot letti dal layer.</item>
    ///   <item><b>VisibleItemCount</b>: item luce visibili.</item>
    ///   <item><b>HiddenItemCount</b>: item luce nascosti.</item>
    ///   <item><b>LocalSourceCount</b>: item visibili con sorgente locale.</item>
    ///   <item><b>DarkCellCount</b>: item visibili sotto la soglia di buio.</item>
    ///   <item><b>MaxIntensity01</b>: massima intensita' visuale incontrata.</item>
    ///   <item><b>Reason</b>: descrizione sintetica dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphLightRenderQueueDiagnostics
    {
        public readonly int SnapshotCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly int LocalSourceCount;
        public readonly int DarkCellCount;
        public readonly float MaxIntensity01;
        public readonly string Reason;

        public ArcGraphLightRenderQueueDiagnostics(
            int snapshotCount,
            int visibleItemCount,
            int hiddenItemCount,
            int localSourceCount,
            int darkCellCount,
            float maxIntensity01,
            string reason)
        {
            SnapshotCount = snapshotCount < 0 ? 0 : snapshotCount;
            VisibleItemCount = visibleItemCount < 0 ? 0 : visibleItemCount;
            HiddenItemCount = hiddenItemCount < 0 ? 0 : hiddenItemCount;
            LocalSourceCount = localSourceCount < 0 ? 0 : localSourceCount;
            DarkCellCount = darkCellCount < 0 ? 0 : darkCellCount;
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
