using Arcontio.View.MapGrid;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainRuntimeMapGridAdapterDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica del ponte data-only tra MapGrid runtime e bootstrap ArcGraph
    /// terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: accesso terrain dichiarato</b></para>
    /// <para>
    /// La struttura rende visibile quali sorgenti sono disponibili e quanti
    /// snapshot ArcGraph vengono prodotti. Serve a verificare il contratto dati
    /// prima di introdurre qualunque renderer Unity o probe mesh.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasMapGridBootstrap</b>: presenza del bootstrap MapGrid esplicito.</item>
    ///   <item><b>HasMapGridWorldView</b>: presenza della view MapGrid opzionale.</item>
    ///   <item><b>HasConfig</b>: presenza della configurazione runtime.</item>
    ///   <item><b>HasMap</b>: presenza della MapGridData runtime.</item>
    ///   <item><b>HasWorld</b>: presenza del World gia' bindato dalla view.</item>
    ///   <item><b>TerrainSnapshotCount</b>: snapshot terrain copiati in ArcGraph.</item>
    ///   <item><b>Reason</b>: esito sintetico del tentativo.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainRuntimeMapGridAdapterDiagnostics
    {
        public readonly bool HasMapGridBootstrap;
        public readonly bool HasMapGridWorldView;
        public readonly bool HasConfig;
        public readonly bool HasMap;
        public readonly bool HasWorld;
        public readonly bool DidInitializeBootstrap;
        public readonly int TerrainSnapshotCount;
        public readonly int LayerCount;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainRuntimeMapGridAdapterDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica immutabile del probe terrain.
        /// </para>
        ///
        /// <para><b>Snapshot diagnostico</b></para>
        /// <para>
        /// I valori vengono copiati in campi readonly per evitare che il risultato
        /// del probe cambi dopo la restituzione al chiamante o dopo il log in
        /// console.
        /// </para>
        /// </summary>
        public ArcGraphTerrainRuntimeMapGridAdapterDiagnostics(
            bool hasMapGridBootstrap,
            bool hasMapGridWorldView,
            bool hasConfig,
            bool hasMap,
            bool hasWorld,
            bool didInitializeBootstrap,
            int terrainSnapshotCount,
            int layerCount,
            string reason)
        {
            HasMapGridBootstrap = hasMapGridBootstrap;
            HasMapGridWorldView = hasMapGridWorldView;
            HasConfig = hasConfig;
            HasMap = hasMap;
            HasWorld = hasWorld;
            DidInitializeBootstrap = didInitializeBootstrap;
            TerrainSnapshotCount = terrainSnapshotCount;
            LayerCount = layerCount;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainRuntimeMapGridAdapter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Adapter manuale che costruisce un context terrain ArcGraph partendo dal
    /// bootstrap MapGrid corrente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: terrain bridge data-only</b></para>
    /// <para>
    /// Questo componente consuma riferimenti espliciti da Inspector e non cerca
    /// globali. Legge <c>MapGridBootstrap.RuntimeConfig</c>,
    /// <c>MapGridBootstrap.RuntimeMap</c> e, se disponibile,
    /// <c>MapGridWorldView.RuntimeWorld</c>. Poi inizializza un
    /// <c>ArcGraphBootstrapRuntime</c> in memoria per copiare snapshot terrain.
    /// Non crea mesh, non crea GameObject, non disegna e non muta la MapGrid.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>mapGridBootstrap</b>: sorgente esplicita di config e MapGridData.</item>
    ///   <item><b>mapGridWorldView</b>: sorgente opzionale del World bindato.</item>
    ///   <item><b>BuildTerrainRuntimeContext</b>: costruzione context read-only.</item>
    ///   <item><b>ProbeTerrainBootstrapDataOnly</b>: smoke manuale senza rendering.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainRuntimeMapGridAdapter : ArcGraphRuntimeContextProvider
    {
        [SerializeField] private MapGridBootstrap mapGridBootstrap;
        [SerializeField] private MapGridWorldView mapGridWorldView;
        [SerializeField] private bool logDiagnostics = true;

        private ArcGraphTerrainRuntimeMapGridAdapterDiagnostics _lastDiagnostics;

        public override string ProviderKind => "LegacyMapGrid";
        public ArcGraphTerrainRuntimeMapGridAdapterDiagnostics LastDiagnostics => _lastDiagnostics;

        // =============================================================================
        // SetMapGridSources
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna da codice le sorgenti MapGrid usate dall'adapter runtime.
        /// </para>
        ///
        /// <para><b>Principio architetturale: wiring scena dichiarato</b></para>
        /// <para>
        /// Il metodo permette a un installer di scena controllato di cablare
        /// l'adapter senza ricorrere a reflection o a modifiche manuali dei campi
        /// privati serializzati. L'adapter continua comunque a leggere solo
        /// riferimenti espliciti gia' risolti dal bordo scena.
        /// </para>
        /// </summary>
        public void SetMapGridSources(
            MapGridBootstrap bootstrap,
            MapGridWorldView worldView)
        {
            mapGridBootstrap = bootstrap;
            mapGridWorldView = worldView;
        }

        // =============================================================================
        // BuildTerrainRuntimeContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un context ArcGraph neutrale usando il World esposto dal ponte legacy.
        /// </para>
        ///
        /// <para><b>Context esplicito</b></para>
        /// <para>
        /// Questo componente resta un ponte transitorio da eliminare. Non passa piu'
        /// <c>MapGridData</c> al context: le dimensioni vengono copiate come valori
        /// primitivi e il terrain viene letto da <c>World.CellSurfaces</c>.
        /// </para>
        /// </summary>
        public override ArcGraphRuntimeContext BuildTerrainRuntimeContext()
        {
            if (mapGridBootstrap == null)
                return ArcGraphRuntimeContext.Empty();

            return new ArcGraphRuntimeContext(
                world: mapGridWorldView != null ? mapGridWorldView.RuntimeWorld : null,
                mapWidthCells: mapGridBootstrap.RuntimeMap != null ? mapGridBootstrap.RuntimeMap.Width : 0,
                mapHeightCells: mapGridBootstrap.RuntimeMap != null ? mapGridBootstrap.RuntimeMap.Height : 0,
                tileSizeWorld: mapGridBootstrap.RuntimeConfig != null ? mapGridBootstrap.RuntimeConfig.tileSizeWorld : 1f,
                chunkSizeCells: mapGridBootstrap.RuntimeConfig != null ? mapGridBootstrap.RuntimeConfig.chunkSize : 16,
                defaultNpcSpriteKey: "human_default");
        }

        // =============================================================================
        // ProbeTerrainBootstrapDataOnlyFromContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue da Inspector il probe data-only del bootstrap terrain.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Probe Terrain Runtime Context From MapGrid")]
        public void ProbeTerrainBootstrapDataOnlyFromContextMenu()
        {
            ProbeTerrainBootstrapDataOnly();
        }

        // =============================================================================
        // ProbeTerrainBootstrapDataOnly
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inizializza un bootstrap ArcGraph temporaneo solo in memoria e misura gli
        /// snapshot terrain copiati.
        /// </para>
        ///
        /// <para><b>Smoke senza scena</b></para>
        /// <para>
        /// Il metodo verifica che il context possa alimentare ArcGraph, ma non
        /// costruisce mesh scene-side. Il bootstrap temporaneo viene eliminato alla
        /// fine del probe, cosi' il componente non conserva cache pesanti.
        /// </para>
        /// </summary>
        public ArcGraphTerrainRuntimeMapGridAdapterDiagnostics ProbeTerrainBootstrapDataOnly()
        {
            ArcGraphRuntimeContext context = BuildTerrainRuntimeContext();

            var runtime = new ArcGraphBootstrapRuntime();
            bool initialized = runtime.Initialize(
                context,
                ArcGraphBootstrapOptions.CreateDefault());

            int terrainSnapshots = initialized && runtime.TerrainSnapshots != null
                ? runtime.TerrainSnapshots.Count
                : 0;
            int layerCount = initialized && runtime.LayerStack != null
                ? runtime.LayerStack.Count
                : 0;

            _lastDiagnostics = new ArcGraphTerrainRuntimeMapGridAdapterDiagnostics(
                mapGridBootstrap != null,
                mapGridWorldView != null,
                context != null && context.HasConfig,
                context != null && context.HasMap,
                context != null && context.HasWorld,
                initialized,
                terrainSnapshots,
                layerCount,
                ResolveReason(context, initialized, terrainSnapshots));

            runtime.Dispose();
            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        // =============================================================================
        // ResolveReason
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte lo stato tecnico del probe in una motivazione sintetica.
        /// </para>
        ///
        /// <para><b>Spiegabilita' del gate</b></para>
        /// <para>
        /// Il metodo restituisce una sola ragione principale, scelta in ordine di
        /// gravita': context mancante, config mancante, mappa mancante, bootstrap
        /// fallito oppure snapshot terrain prodotti/vuoti.
        /// </para>
        /// </summary>
        private static string ResolveReason(
            ArcGraphRuntimeContext context,
            bool initialized,
            int terrainSnapshots)
        {
            // La diagnostica viene tenuta volutamente piatta: non deve diventare
            // logica decisionale, ma solo una spiegazione leggibile del primo
            // blocco incontrato nel ponte dati.
            if (context == null)
                return "RuntimeContextMissing";

            if (!context.HasConfig)
                return "ConfigMissing";

            if (!context.HasMap)
                return "MapMissing";

            if (!initialized)
                return "BootstrapInitializeFailed";

            return terrainSnapshots > 0
                ? "TerrainSnapshotsBuilt"
                : "TerrainSnapshotsEmpty";
        }

        // =============================================================================
        // LogLastDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive in console l'ultimo esito del probe terrain data-only.
        /// </para>
        ///
        /// <para><b>Diagnostica locale e opzionale</b></para>
        /// <para>
        /// Il log e' disattivabile da Inspector e non guida alcun comportamento
        /// runtime. Serve solo durante il gate manuale per capire se ArcGraph ha
        /// ricevuto bootstrap, configurazione, mappa e snapshot terrain.
        /// </para>
        /// </summary>
        private void LogLastDiagnostics()
        {
            // Il componente puo' restare in scena anche dopo il test: il flag
            // evita spam di console quando si richiama il probe da automazioni o
            // da futuri wrapper temporanei.
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphTerrainRuntimeMapGridAdapter] " + _lastDiagnostics.Reason +
                " mapGridBootstrap=" + _lastDiagnostics.HasMapGridBootstrap +
                ", mapGridWorldView=" + _lastDiagnostics.HasMapGridWorldView +
                ", config=" + _lastDiagnostics.HasConfig +
                ", map=" + _lastDiagnostics.HasMap +
                ", world=" + _lastDiagnostics.HasWorld +
                ", initialized=" + _lastDiagnostics.DidInitializeBootstrap +
                ", terrainSnapshots=" + _lastDiagnostics.TerrainSnapshotCount +
                ", layerCount=" + _lastDiagnostics.LayerCount);
        }
    }
}
