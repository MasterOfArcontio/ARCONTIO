namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainAnimationClockStep
    // =============================================================================
    /// <summary>
    /// <para>
    /// Esito immutabile di un avanzamento del clock visuale terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tempo grafico separato dal tempo simulativo</b></para>
    /// <para>
    /// Il clock visuale serve solo a decidere quando un tile animato deve cambiare
    /// frame. Non rappresenta un tick del mondo, non fa crescere piante, non muove
    /// NPC e non modifica la simulazione. In questo modo acqua, erba o altri tile
    /// animati possono restare una responsabilita' di ArcGraph.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>VisualTimeSeconds</b>: tempo visuale accumulato.</item>
    ///   <item><b>RefreshDue</b>: true se e' opportuno ridisegnare i chunk animati.</item>
    ///   <item><b>ConsumedRefreshCount</b>: numero di intervalli consumati nello step.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainAnimationClockStep
    {
        public readonly float VisualTimeSeconds;
        public readonly bool RefreshDue;
        public readonly int ConsumedRefreshCount;

        // =============================================================================
        // ArcGraphTerrainAnimationClockStep
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un esito normalizzato dello step di animazione terrain.
        /// </para>
        /// </summary>
        public ArcGraphTerrainAnimationClockStep(
            float visualTimeSeconds,
            bool refreshDue,
            int consumedRefreshCount)
        {
            VisualTimeSeconds = visualTimeSeconds < 0f ? 0f : visualTimeSeconds;
            RefreshDue = refreshDue;
            ConsumedRefreshCount = consumedRefreshCount < 0 ? 0 : consumedRefreshCount;
        }
    }

    // =============================================================================
    // ArcGraphTerrainAnimationClock
    // =============================================================================
    /// <summary>
    /// <para>
    /// Clock data-only per animazioni dei tile terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: refresh grafico limitato</b></para>
    /// <para>
    /// Il clock accumula tempo visuale e segnala quando e' necessario rinfrescare
    /// i chunk che contengono tile animati. Non conosce la mappa, non marca dirty
    /// state e non crea oggetti Unity: decide solo se il tempo trascorso giustifica
    /// un nuovo frame grafico.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_visualTimeSeconds</b>: tempo totale usato dal resolver frame-based.</item>
    ///   <item><b>_secondsSinceRefresh</b>: accumulo locale per la prossima invalidazione.</item>
    ///   <item><b>Advance</b>: avanza tempo e restituisce se il refresh e' dovuto.</item>
    ///   <item><b>Reset</b>: azzera il clock senza toccare mappe o renderer.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainAnimationClock
    {
        private float _visualTimeSeconds;
        private float _secondsSinceRefresh;

        public float VisualTimeSeconds => _visualTimeSeconds;

        // =============================================================================
        // Advance
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avanza il clock visuale e indica se i chunk animati devono essere ridisegnati.
        /// </para>
        /// </summary>
        public ArcGraphTerrainAnimationClockStep Advance(
            float deltaSeconds,
            float refreshSeconds)
        {
            float safeDelta = deltaSeconds > 0f ? deltaSeconds : 0f;
            float safeRefresh = refreshSeconds > 0.0001f ? refreshSeconds : 0.25f;

            if (safeDelta <= 0f)
            {
                return new ArcGraphTerrainAnimationClockStep(
                    _visualTimeSeconds,
                    refreshDue: false,
                    consumedRefreshCount: 0);
            }

            _visualTimeSeconds += safeDelta;
            _secondsSinceRefresh += safeDelta;

            if (_secondsSinceRefresh < safeRefresh)
            {
                return new ArcGraphTerrainAnimationClockStep(
                    _visualTimeSeconds,
                    refreshDue: false,
                    consumedRefreshCount: 0);
            }

            int consumed = (int)(_secondsSinceRefresh / safeRefresh);
            _secondsSinceRefresh -= consumed * safeRefresh;

            return new ArcGraphTerrainAnimationClockStep(
                _visualTimeSeconds,
                refreshDue: true,
                consumed);
        }

        // =============================================================================
        // Reset
        // =============================================================================
        /// <summary>
        /// <para>
        /// Azzera il clock visuale terrain.
        /// </para>
        /// </summary>
        public void Reset()
        {
            _visualTimeSeconds = 0f;
            _secondsSinceRefresh = 0f;
        }
    }
}
