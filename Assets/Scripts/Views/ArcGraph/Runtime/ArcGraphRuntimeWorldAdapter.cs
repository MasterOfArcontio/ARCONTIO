using Arcontio.Core;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRuntimeWorldAdapterDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del provider runtime neutro World -> ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: World come source of truth</b></para>
    /// <para>
    /// La diagnostica espone se ArcGraph sta ricevendo davvero il <c>World</c> del
    /// runtime e se il layer superfici cella e' disponibile. Non contiene dati
    /// legacy e non misura componenti della vecchia vista.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasExplicitHost</b>: indica se il provider ha un SimulationHost assegnato.</item>
    ///   <item><b>UsedSingletonFallback</b>: indica se e' stato usato SimulationHost.Instance.</item>
    ///   <item><b>HasWorld</b>: indica se il World runtime e' disponibile.</item>
    ///   <item><b>HasCellSurfaces</b>: indica se il World espone il layer pavimenti.</item>
    ///   <item><b>MapWidthCells/MapHeightCells</b>: dimensioni lette dal World.</item>
    ///   <item><b>Reason</b>: esito principale del tentativo.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphRuntimeWorldAdapterDiagnostics
    {
        public readonly bool HasExplicitHost;
        public readonly bool UsedSingletonFallback;
        public readonly bool HasWorld;
        public readonly bool HasCellSurfaces;
        public readonly int MapWidthCells;
        public readonly int MapHeightCells;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphRuntimeWorldAdapterDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una fotografia diagnostica immutabile del provider World.
        /// </para>
        /// </summary>
        public ArcGraphRuntimeWorldAdapterDiagnostics(
            bool hasExplicitHost,
            bool usedSingletonFallback,
            bool hasWorld,
            bool hasCellSurfaces,
            int mapWidthCells,
            int mapHeightCells,
            string reason)
        {
            HasExplicitHost = hasExplicitHost;
            UsedSingletonFallback = usedSingletonFallback;
            HasWorld = hasWorld;
            HasCellSurfaces = hasCellSurfaces;
            MapWidthCells = mapWidthCells < 0 ? 0 : mapWidthCells;
            MapHeightCells = mapHeightCells < 0 ? 0 : mapHeightCells;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphRuntimeWorldAdapter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Provider runtime ArcGraph che costruisce il context dalla simulazione
    /// corrente, senza passare da viste legacy.
    /// </para>
    ///
    /// <para><b>Principio architetturale: SimulationHost -> World -> snapshot</b></para>
    /// <para>
    /// Questo componente non legge bootstrap, view o buffer legacy. Recupera il
    /// <c>World</c> gia' orchestrato da <see cref="SimulationHost"/> e produce un
    /// <see cref="ArcGraphRuntimeContext"/> che il bootstrap ArcGraph usera' per
    /// copiare snapshot terrain, oggetti e attori. La direzione resta sola lettura:
    /// vista verso snapshot, non UI verso mutazione simulativa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>simulationHost</b>: host esplicito opzionale assegnabile da scena.</item>
    ///   <item><b>allowSimulationHostSingletonFallback</b>: fallback temporaneo a SimulationHost.Instance.</item>
    ///   <item><b>tileSizeWorld/chunkSizeCells</b>: parametri grafici ArcGraph neutrali.</item>
    ///   <item><b>BuildTerrainRuntimeContext</b>: entry point letto da wrapper e renderer.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRuntimeWorldAdapter : ArcGraphRuntimeContextProvider
    {
        [SerializeField] private SimulationHost simulationHost;
        [SerializeField] private bool allowSimulationHostSingletonFallback = true;
        [SerializeField] private float tileSizeWorld = 1f;
        [SerializeField] private int chunkSizeCells = 16;
        [SerializeField] private string defaultNpcSpriteKey = "human_default";
        [SerializeField] private bool logDiagnostics;

        private ArcGraphRuntimeWorldAdapterDiagnostics _lastDiagnostics;

        public override string ProviderKind => "World";
        public ArcGraphRuntimeWorldAdapterDiagnostics LastDiagnostics => _lastDiagnostics;

        // =============================================================================
        // SetSimulationHost
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna da codice l'host runtime esplicito da cui leggere il World.
        /// </para>
        /// </summary>
        public void SetSimulationHost(SimulationHost host)
        {
            simulationHost = host;
        }

        // =============================================================================
        // BuildTerrainRuntimeContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un context ArcGraph leggendo il World corrente del runtime.
        /// </para>
        /// </summary>
        public override ArcGraphRuntimeContext BuildTerrainRuntimeContext()
        {
            bool usedSingletonFallback;
            World world = ResolveWorld(out usedSingletonFallback);
            bool hasWorld = world != null;
            bool hasCellSurfaces = world?.CellSurfaces != null;

            _lastDiagnostics = new ArcGraphRuntimeWorldAdapterDiagnostics(
                simulationHost != null,
                usedSingletonFallback,
                hasWorld,
                hasCellSurfaces,
                world != null ? world.MapWidth : 0,
                world != null ? world.MapHeight : 0,
                ResolveReason(world, hasCellSurfaces));

            LogLastDiagnostics();

            if (world == null)
                return ArcGraphRuntimeContext.Empty();

            return new ArcGraphRuntimeContext(
                world,
                world.MapWidth,
                world.MapHeight,
                tileSizeWorld,
                chunkSizeCells,
                defaultNpcSpriteKey);
        }

        // =============================================================================
        // ResolveWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il World da host esplicito oppure da fallback controllato.
        /// </para>
        /// </summary>
        private World ResolveWorld(out bool usedSingletonFallback)
        {
            usedSingletonFallback = false;

            if (simulationHost != null)
                return simulationHost.World;

            if (!allowSimulationHostSingletonFallback)
                return null;

            SimulationHost host = SimulationHost.Instance;
            usedSingletonFallback = host != null;
            return host != null ? host.World : null;
        }

        // =============================================================================
        // ResolveReason
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte lo stato del provider in una ragione diagnostica breve.
        /// </para>
        /// </summary>
        private static string ResolveReason(
            World world,
            bool hasCellSurfaces)
        {
            if (world == null)
                return "WorldMissing";

            if (!hasCellSurfaces)
                return "CellSurfaceLayerMissing";

            return "WorldContextReady";
        }

        // =============================================================================
        // LogLastDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Logga opzionalmente lo stato del provider World.
        /// </para>
        /// </summary>
        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphRuntimeWorldAdapter] " + _lastDiagnostics.Reason +
                " explicitHost=" + _lastDiagnostics.HasExplicitHost +
                ", singletonFallback=" + _lastDiagnostics.UsedSingletonFallback +
                ", world=" + _lastDiagnostics.HasWorld +
                ", cellSurfaces=" + _lastDiagnostics.HasCellSurfaces +
                ", map=" + _lastDiagnostics.MapWidthCells + "x" + _lastDiagnostics.MapHeightCells);
        }
    }
}
