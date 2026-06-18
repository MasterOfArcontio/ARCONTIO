using Arcontio.Core;
using SocialViewer.UI;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphFovDebugOverlayRuntimeController
    // =============================================================================
    /// <summary>
    /// <para>
    /// Controller runtime del debug FOV corrente in ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI -> controller -> producer -> consumer</b></para>
    /// <para>
    /// Il bottone UI non legge il mondo e non disegna direttamente. Invoca questo
    /// controller, che risolve un context read-only gia' autorizzato, sceglie l'NPC
    /// view-side attivo e consegna snapshot/queue al consumer FOV. Il controller non
    /// muta la simulazione, non invia comandi e non dipende da <c>MapGridWorldView</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>runtimeContextProvider</b>: sorgente read-only del World.</item>
    ///   <item><b>overlayConsumer</b>: renderer pooled delle celle FOV.</item>
    ///   <item><b>_producer/_snapshot/_queueBuilder</b>: pipeline dati ArcGraph.</item>
    ///   <item><b>ResolveActiveNpcId</b>: selezione view-side, non decisionale.</item>
    ///   <item><b>ProcessFrame</b>: build e dispatch controllati.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphFovDebugOverlayRuntimeController : MonoBehaviour
    {
        [SerializeField] private ArcGraphRuntimeContextProvider runtimeContextProvider;
        [SerializeField] private ArcGraphFovDebugOverlaySceneConsumer overlayConsumer;
        [SerializeField] private bool overlayEnabled;
        [SerializeField] private bool processInUpdate = true;
        [SerializeField] private bool includeWatchedMargin = true;
        [SerializeField] private bool logDiagnostics;

        private readonly ArcGraphFovDebugOverlayProducer _producer = new ArcGraphFovDebugOverlayProducer();
        private readonly ArcGraphDebugOverlaySnapshot _snapshot = new ArcGraphDebugOverlaySnapshot();
        private readonly ArcGraphDebugOverlayQueue _queue = new ArcGraphDebugOverlayQueue();
        private readonly ArcGraphDebugOverlayQueueBuilder _queueBuilder = new ArcGraphDebugOverlayQueueBuilder();

        private ArcGraphFovDebugOverlayProducerDiagnostics _lastProducerDiagnostics;
        private ArcGraphDebugOverlayQueueDiagnostics _lastQueueDiagnostics;

        public bool OverlayEnabled => overlayEnabled;
        public ArcGraphFovDebugOverlayProducerDiagnostics LastProducerDiagnostics => _lastProducerDiagnostics;
        public ArcGraphDebugOverlayQueueDiagnostics LastQueueDiagnostics => _lastQueueDiagnostics;

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna opzionalmente il FOV debug quando il toggle e' acceso.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!processInUpdate || !overlayEnabled)
                return;

            ProcessFrame();
        }

        // =============================================================================
        // SetRuntimeContextProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la sorgente context autorizzata per leggere il World.
        /// </para>
        /// </summary>
        public void SetRuntimeContextProvider(ArcGraphRuntimeContextProvider provider)
        {
            runtimeContextProvider = provider;
        }

        // =============================================================================
        // SetOverlayConsumer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il consumer visuale delle celle FOV.
        /// </para>
        /// </summary>
        public void SetOverlayConsumer(ArcGraphFovDebugOverlaySceneConsumer consumer)
        {
            overlayConsumer = consumer;
        }

        // =============================================================================
        // SetProcessInUpdate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Controlla se il controller deve aggiornarsi automaticamente in Update.
        /// </para>
        /// </summary>
        public void SetProcessInUpdate(bool enabled)
        {
            processInUpdate = enabled;
        }

        // =============================================================================
        // ToggleOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inverte lo stato del FOV debug ArcGraph.
        /// </para>
        /// </summary>
        public void ToggleOverlay()
        {
            SetOverlayEnabled(!overlayEnabled);
        }

        // =============================================================================
        // SetOverlayEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il FOV debug. Quando viene spento, pulisce il
        /// consumer senza distruggere il pool.
        /// </para>
        /// </summary>
        public void SetOverlayEnabled(bool enabled)
        {
            overlayEnabled = enabled;

            if (!overlayEnabled)
                overlayConsumer?.ClearOverlay();
        }

        // =============================================================================
        // ProcessFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce e renderizza un frame FOV ArcGraph.
        /// </para>
        /// </summary>
        public void ProcessFrame()
        {
            if (!overlayEnabled)
            {
                overlayConsumer?.ClearOverlay();
                return;
            }

            if (runtimeContextProvider == null || overlayConsumer == null)
            {
                overlayConsumer?.ClearOverlay();
                Log("RuntimeBindingMissing");
                return;
            }

            ArcGraphRuntimeContext context = runtimeContextProvider.BuildTerrainRuntimeContext();
            World world = context?.World;
            if (world == null)
            {
                overlayConsumer.ClearOverlay();
                Log("WorldMissing");
                return;
            }

            int activeNpcId = ResolveActiveNpcId(world);
            bool useLineOfSight = ResolveUseLineOfSight(world);
            overlayConsumer.SetTileWorldSize(context.TileSizeWorld);

            _lastProducerDiagnostics = _producer.FillCurrentConeSnapshot(
                world,
                activeNpcId,
                useLineOfSight,
                includeWatchedMargin,
                _snapshot,
                true);

            _lastQueueDiagnostics = _queueBuilder.Build(_snapshot, _queue, true, false);
            overlayConsumer.RenderQueue(_queue);

            Log(
                _lastProducerDiagnostics.Reason +
                " npc=" + activeNpcId +
                ", cells=" + _lastProducerDiagnostics.TotalCellCount +
                ", visible=" + _lastQueueDiagnostics.VisibleItemCount);
        }

        private static int ResolveActiveNpcId(World world)
        {
            if (world == null)
                return -1;

            int selectedNpcId = NPCSelection.SelectedNpcId;
            if (selectedNpcId > 0 && world.ExistsNpc(selectedNpcId))
                return selectedNpcId;

            foreach (var pair in world.GridPos)
            {
                if (pair.Key > 0 && world.ExistsNpc(pair.Key))
                    return pair.Key;
            }

            return -1;
        }

        private static bool ResolveUseLineOfSight(World world)
        {
            return world?.Config?.Sim?.debug_fov != null
                ? world.Config.Sim.debug_fov.use_los
                : true;
        }

        private void Log(string reason)
        {
            if (!logDiagnostics)
                return;

            Debug.Log("[ArcGraphFovDebugOverlayRuntimeController] " + reason);
        }
    }
}
