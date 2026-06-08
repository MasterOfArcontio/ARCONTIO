using Arcontio.Core;
using Arcontio.View.MapGrid;
using SocialViewer.UI;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugRuntimeMapGridAdapterDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica del ponte manuale MapGrid -> ArcGraph debug runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: debug spiegabile senza stato nascosto</b></para>
    /// <para>
    /// L'adapter e' un punto di confine tra il renderer legacy MapGrid e il wrapper
    /// ArcGraph. Questa struttura rende visibile quali riferimenti erano presenti,
    /// quale NPC e' stato passato e quale esito ha prodotto il wrapper, senza
    /// trasformare l'adapter in un manager o in una sorgente alternativa di verita'.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasMapGridView</b>: indica se la view MapGrid e' stata fornita.</item>
    ///   <item><b>HasWrapper</b>: indica se il wrapper ArcGraph e' stato fornito.</item>
    ///   <item><b>HasConfig</b>: indica se la config view-side e' disponibile.</item>
    ///   <item><b>HasWorld</b>: indica se la view MapGrid ha gia' un World bindato.</item>
    ///   <item><b>SelectedNpcId</b>: NPC letto da <c>NPCSelection</c>.</item>
    ///   <item><b>SourceTick</b>: tick diagnostico letto dal World, se disponibile.</item>
    ///   <item><b>WrapperDiagnostics</b>: esito restituito dal wrapper ArcGraph.</item>
    ///   <item><b>Reason</b>: motivo sintetico del tentativo adapter.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphDebugRuntimeMapGridAdapterDiagnostics
    {
        public readonly bool HasMapGridView;
        public readonly bool HasWrapper;
        public readonly bool HasConfig;
        public readonly bool HasWorld;
        public readonly int SelectedNpcId;
        public readonly long SourceTick;
        public readonly ArcGraphDebugRuntimeWiringDiagnostics WrapperDiagnostics;
        public readonly string Reason;

        public ArcGraphDebugRuntimeMapGridAdapterDiagnostics(
            bool hasMapGridView,
            bool hasWrapper,
            bool hasConfig,
            bool hasWorld,
            int selectedNpcId,
            long sourceTick,
            ArcGraphDebugRuntimeWiringDiagnostics wrapperDiagnostics,
            string reason)
        {
            HasMapGridView = hasMapGridView;
            HasWrapper = hasWrapper;
            HasConfig = hasConfig;
            HasWorld = hasWorld;
            SelectedNpcId = selectedNpcId;
            SourceTick = sourceTick;
            WrapperDiagnostics = wrapperDiagnostics;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphDebugRuntimeMapGridAdapter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Adapter manuale che costruisce un context debug ArcGraph partendo dalla
    /// view MapGrid corrente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte esplicito tra legacy view e ArcGraph</b></para>
    /// <para>
    /// Questo componente e' volutamente piu' vicino alla scena rispetto al wrapper
    /// ArcGraph. Puo' conoscere <c>MapGridWorldView</c> e <c>NPCSelection</c>, ma
    /// non legge <c>SimulationHost.Instance</c>, non usa <c>MapGridWorldProvider</c>,
    /// non introduce hotkey, non crea UI e non esegue polling automatico. Con un
    /// comando manuale costruisce un <c>ArcGraphRuntimeContext</c> parziale e lo
    /// passa al wrapper gia' esistente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>mapGridWorldView</b>: sorgente view-side esplicita per config e World bindato.</item>
    ///   <item><b>targetWrapper</b>: destinazione ArcGraph esplicita.</item>
    ///   <item><b>PushCurrentFrameFromMapGrid</b>: context menu/manual push.</item>
    ///   <item><b>BuildContext</b>: crea context parziale config/world.</item>
    ///   <item><b>ResolveSourceTick</b>: legge solo il tick diagnostico dal World ricevuto.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugRuntimeMapGridAdapter : MonoBehaviour
    {
        [SerializeField] private MapGridWorldView mapGridWorldView;
        [SerializeField] private ArcGraphDebugRuntimeSceneWrapper targetWrapper;
        [SerializeField] private bool logDiagnostics = true;

        private ArcGraphDebugRuntimeMapGridAdapterDiagnostics _lastDiagnostics;

        public ArcGraphDebugRuntimeMapGridAdapterDiagnostics LastDiagnostics => _lastDiagnostics;

        // =============================================================================
        // PushCurrentFrameFromMapGrid
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge lo stato view-side corrente e lo passa al wrapper ArcGraph.
        /// </para>
        ///
        /// <para><b>Chiamata manuale</b></para>
        /// <para>
        /// Il metodo e' esposto a context menu per QA controllata. Non viene chiamato
        /// da <c>Update</c> e quindi non introduce costo per frame. Se il wrapper e'
        /// spento, il risultato atteso resta <c>OverlayDisabled</c>.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Push Debug Runtime Frame From MapGrid")]
        public void PushCurrentFrameFromMapGrid()
        {
            PushCurrentFrame();
        }

        // =============================================================================
        // PushCurrentFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue il push manuale e restituisce la diagnostica completa.
        /// </para>
        /// </summary>
        public ArcGraphDebugRuntimeMapGridAdapterDiagnostics PushCurrentFrame()
        {
            bool hasMapGridView = mapGridWorldView != null;
            bool hasWrapper = targetWrapper != null;

            int selectedNpcId = NPCSelection.SelectedNpcId > 0
                ? NPCSelection.SelectedNpcId
                : -1;

            World world = hasMapGridView ? mapGridWorldView.RuntimeWorld : null;
            long sourceTick = ResolveSourceTick(world);
            ArcGraphRuntimeContext context = BuildContext(hasMapGridView, world);

            if (!hasWrapper)
            {
                _lastDiagnostics = CreateDiagnostics(
                    hasMapGridView,
                    hasWrapper,
                    context,
                    selectedNpcId,
                    sourceTick,
                    default,
                    "TargetWrapperMissing");

                LogLastDiagnostics();
                return _lastDiagnostics;
            }

            // Il wrapper resta responsabile del gate overlay e del dispatch. L'adapter
            // passa solo context, NPC gia' selezionato e tick diagnostico.
            ArcGraphDebugRuntimeWiringDiagnostics wrapperDiagnostics =
                targetWrapper.ProcessFrame(context, selectedNpcId, sourceTick);

            _lastDiagnostics = CreateDiagnostics(
                hasMapGridView,
                hasWrapper,
                context,
                selectedNpcId,
                sourceTick,
                wrapperDiagnostics,
                "FramePushedToWrapper");

            LogLastDiagnostics();
            return _lastDiagnostics;
        }

        private ArcGraphRuntimeContext BuildContext(
            bool hasMapGridView,
            World world)
        {
            if (!hasMapGridView)
                return null;

            // Per v0.37 il debug overlay Landmark/GVD usa World. La MapGridData non
            // serve: passiamo null per evitare di trasformare questo adapter in un
            // ponte terrain o in un accesso generale alla mappa legacy.
            return new ArcGraphRuntimeContext(
                mapGridWorldView.RuntimeConfig,
                map: null,
                world);
        }

        private static long ResolveSourceTick(World world)
        {
            return world != null
                ? world.Global.CurrentTickIndex
                : -1;
        }

        private static ArcGraphDebugRuntimeMapGridAdapterDiagnostics CreateDiagnostics(
            bool hasMapGridView,
            bool hasWrapper,
            ArcGraphRuntimeContext context,
            int selectedNpcId,
            long sourceTick,
            ArcGraphDebugRuntimeWiringDiagnostics wrapperDiagnostics,
            string reason)
        {
            return new ArcGraphDebugRuntimeMapGridAdapterDiagnostics(
                hasMapGridView,
                hasWrapper,
                context != null && context.HasConfig,
                context != null && context.HasWorld,
                selectedNpcId,
                sourceTick,
                wrapperDiagnostics,
                reason);
        }

        private void LogLastDiagnostics()
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphDebugRuntimeMapGridAdapter] " + _lastDiagnostics.Reason +
                " mapGridView=" + _lastDiagnostics.HasMapGridView +
                ", wrapper=" + _lastDiagnostics.HasWrapper +
                ", config=" + _lastDiagnostics.HasConfig +
                ", world=" + _lastDiagnostics.HasWorld +
                ", selectedNpc=" + _lastDiagnostics.SelectedNpcId +
                ", sourceTick=" + _lastDiagnostics.SourceTick +
                ", wrapperReason=" + _lastDiagnostics.WrapperDiagnostics.Reason +
                ", wrapperBuilt=" + _lastDiagnostics.WrapperDiagnostics.DidBuildFeed +
                ", wrapperDispatched=" + _lastDiagnostics.WrapperDiagnostics.DidDispatchToConsumer);
        }
    }
}
