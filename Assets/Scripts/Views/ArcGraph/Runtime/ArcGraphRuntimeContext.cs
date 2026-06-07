using Arcontio.Core;
using Arcontio.View.MapGrid;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRuntimeContext
    // =============================================================================
    /// <summary>
    /// <para>
    /// Pacchetto esplicito di sorgenti runtime che il bootstrap ArcGraph puo'
    /// leggere come input.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dati ricevuti, non cercati globalmente</b></para>
    /// <para>
    /// ArcGraph non deve chiamare <c>SimulationHost.Instance</c>, non deve cercare
    /// <c>MapGridBootstrap</c> nella scena e non deve entrare in
    /// <c>MapGridWorldView</c>. Un chiamante esterno costruisce questo context e lo
    /// passa al bootstrap. Il bootstrap usa i riferimenti per copiare snapshot, non
    /// per mutare il mondo o la MapGrid.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Config</b>: configurazione view-side corrente.</item>
    ///   <item><b>Map</b>: buffer terreno legacy, trattato come sorgente di sola lettura.</item>
    ///   <item><b>World</b>: source of truth oggettiva letta solo dall'adapter.</item>
    ///   <item><b>HasAnyRuntimeData</b>: segnala se il context contiene almeno una sorgente.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRuntimeContext
    {
        public MapGridConfig Config { get; }
        public MapGridData Map { get; }
        public World World { get; }

        public bool HasConfig => Config != null;
        public bool HasMap => Map != null;
        public bool HasWorld => World != null;
        public bool HasAnyRuntimeData => HasConfig || HasMap || HasWorld;
        public bool HasCompleteRuntimeData => HasConfig && HasMap && HasWorld;

        // =============================================================================
        // ArcGraphRuntimeContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un context runtime con riferimenti opzionali.
        /// </para>
        ///
        /// <para><b>Context parziale ammesso</b></para>
        /// <para>
        /// In <c>v0.31</c> il bootstrap deve poter nascere anche quando non tutti i
        /// dati sono disponibili. Per questo i parametri sono opzionali e la
        /// diagnostica del bootstrap decide se copiare snapshot o limitarsi allo
        /// stato interno.
        /// </para>
        /// </summary>
        public ArcGraphRuntimeContext(
            MapGridConfig config = null,
            MapGridData map = null,
            World world = null)
        {
            Config = config;
            Map = map;
            World = world;
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un context vuoto per bootstrap puramente interno.
        /// </para>
        ///
        /// <para><b>Uso previsto</b></para>
        /// <para>
        /// Il context vuoto permette di verificare che render state e layer stack
        /// possano accendersi anche senza sorgenti runtime. Non rappresenta una
        /// mappa valida e non deve essere usato come fonte di dati simulativi.
        /// </para>
        /// </summary>
        public static ArcGraphRuntimeContext Empty()
        {
            return new ArcGraphRuntimeContext();
        }
    }
}
