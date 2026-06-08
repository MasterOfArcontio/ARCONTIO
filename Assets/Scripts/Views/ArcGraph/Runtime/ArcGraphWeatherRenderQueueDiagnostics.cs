namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWeatherRenderQueueDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica dell'overlay meteo ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' senza simulazione climatica</b></para>
    /// <para>
    /// La diagnostica registra se uno snapshot meteo e' stato ricevuto, se e' attivo,
    /// se produce overlay visibile e se l'overlay puo' essere animato. Non misura
    /// temperatura, umidita', precipitazioni, vento fisico o impatto sulle piante.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasSnapshot</b>: indica se il layer ha ricevuto un dato.</item>
    ///   <item><b>ActiveSnapshotCount</b>: numero di snapshot attivi valutati.</item>
    ///   <item><b>VisibleItemCount</b>: overlay visibili prodotti.</item>
    ///   <item><b>HiddenItemCount</b>: overlay nascosti per dato inattivo o non valido.</item>
    ///   <item><b>AnimatedItemCount</b>: overlay che ammettono animazione frame-based ArcGraph.</item>
    ///   <item><b>MaxIntensity01</b>: intensita' visuale massima incontrata.</item>
    ///   <item><b>Reason</b>: descrizione sintetica dell'esito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphWeatherRenderQueueDiagnostics
    {
        public readonly bool HasSnapshot;
        public readonly int ActiveSnapshotCount;
        public readonly int VisibleItemCount;
        public readonly int HiddenItemCount;
        public readonly int AnimatedItemCount;
        public readonly float MaxIntensity01;
        public readonly string Reason;

        public ArcGraphWeatherRenderQueueDiagnostics(
            bool hasSnapshot,
            int activeSnapshotCount,
            int visibleItemCount,
            int hiddenItemCount,
            int animatedItemCount,
            float maxIntensity01,
            string reason)
        {
            HasSnapshot = hasSnapshot;
            ActiveSnapshotCount = activeSnapshotCount < 0 ? 0 : activeSnapshotCount;
            VisibleItemCount = visibleItemCount < 0 ? 0 : visibleItemCount;
            HiddenItemCount = hiddenItemCount < 0 ? 0 : hiddenItemCount;
            AnimatedItemCount = animatedItemCount < 0 ? 0 : animatedItemCount;
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
