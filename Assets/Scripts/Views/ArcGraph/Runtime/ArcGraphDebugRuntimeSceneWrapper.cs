using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugRuntimeSceneWrapper
    // =============================================================================
    /// <summary>
    /// <para>
    /// Wrapper Unity minimale per usare il contratto di wiring runtime debug dentro
    /// una scena controllata.
    /// </para>
    ///
    /// <para><b>Principio architetturale: adattatore scena, non sorgente dati</b></para>
    /// <para>
    /// Questo componente non cerca il <c>World</c>, non legge
    /// <c>SimulationHost.Instance</c>, non consulta provider legacy, non
    /// legge input e non sceglie autonomamente l'NPC attivo. Riceve invece un
    /// <c>ArcGraphRuntimeContext</c> e un id NPC gia' risolti da un chiamante esterno.
    /// Il suo unico compito e' costruire un <c>ArcGraphDebugRuntimeWiringFrame</c>,
    /// passarlo al coordinatore e, se abilitato, consegnare la queue al renderer
    /// debug esplicitamente assegnato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>debugOverlayRenderer</b>: consumer visuale assegnato manualmente.</item>
    ///   <item><b>overlayEnabled</b>: gate principale, falso di default.</item>
    ///   <item><b>include*</b>: opzioni debug Landmark/GVD controllate da Inspector.</item>
    ///   <item><b>_runtimeContext</b>: sorgente ricevuta, mai cercata globalmente.</item>
    ///   <item><b>_coordinator</b>: nucleo C# passivo che produce e dispatcha la queue.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugRuntimeSceneWrapper : MonoBehaviour
    {
        [SerializeField] private ArcGraphDebugOverlaySceneProbeRenderer debugOverlayRenderer;
        [SerializeField] private bool overlayEnabled;
        [SerializeField] private bool dispatchToRenderer = true;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private int activeNpcId = -1;
        [SerializeField] private bool includeLandmark = true;
        [SerializeField] private bool includeGvdDin = true;
        [SerializeField] private bool includeDtHeatmap = true;
        [SerializeField] private bool includeGvdRaw = true;
        [SerializeField] private bool includeGvdGraph = true;
        [SerializeField] private bool includeHiddenItems;

        private readonly ArcGraphDebugRuntimeWiringCoordinator _coordinator = new();
        private ArcGraphRuntimeContext _runtimeContext;
        private long _sourceTick = -1;
        private ArcGraphDebugRuntimeWiringDiagnostics _lastDiagnostics;

        public ArcGraphDebugRuntimeWiringDiagnostics LastDiagnostics => _lastDiagnostics;
        public ArcGraphRuntimeContext RuntimeContext => _runtimeContext;
        public int ActiveNpcId => activeNpcId;
        public bool OverlayEnabled => overlayEnabled;

        // =============================================================================
        // SetRuntimeContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta il context runtime ricevuto da un chiamante esterno.
        /// </para>
        ///
        /// <para><b>Nessuna risoluzione globale</b></para>
        /// <para>
        /// Il wrapper conserva il riferimento cosi' com'e'. Se il context e' nullo
        /// o non contiene <c>World</c>, sara' il coordinatore a produrre diagnostica
        /// leggibile senza creare render.
        /// </para>
        /// </summary>
        public void SetRuntimeContext(ArcGraphRuntimeContext runtimeContext)
        {
            _runtimeContext = runtimeContext;
        }

        // =============================================================================
        // SetActiveNpcId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta l'NPC attivo gia' deciso da un sistema view-side esterno.
        /// </para>
        /// </summary>
        public void SetActiveNpcId(int npcId)
        {
            activeNpcId = npcId;
        }

        // =============================================================================
        // SetSourceTick
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta il tick sorgente opzionale usato solo per diagnostica.
        /// </para>
        /// </summary>
        public void SetSourceTick(long sourceTick)
        {
            _sourceTick = sourceTick;
        }

        // =============================================================================
        // SetOverlayEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il gate principale del debug overlay runtime.
        /// </para>
        /// </summary>
        public void SetOverlayEnabled(bool enabled)
        {
            overlayEnabled = enabled;
        }

        // =============================================================================
        // ProcessFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa un frame usando il context e l'NPC attivo gia' memorizzati.
        /// </para>
        ///
        /// <para><b>Chiamata controllata</b></para>
        /// <para>
        /// Questo metodo non viene chiamato automaticamente da <c>Update</c>. Il
        /// chiamante deve invocarlo nel punto desiderato del ciclo view/debug, cosi'
        /// il debug overlay non introduce polling nascosto o lavoro CPU non richiesto.
        /// </para>
        /// </summary>
        public ArcGraphDebugRuntimeWiringDiagnostics ProcessFrame()
        {
            // Le opzioni sono ricostruite a ogni chiamata per riflettere eventuali
            // cambi manuali da Inspector senza mantenere cache secondarie.
            ArcGraphDebugOverlayRuntimeFeedOptions options = CreateOptions();

            // Il frame esplicita tutti i gate: context ricevuto, NPC attivo,
            // abilitazione overlay, richiesta di dispatch e tick diagnostico.
            var frame = new ArcGraphDebugRuntimeWiringFrame(
                _runtimeContext,
                activeNpcId,
                options,
                overlayEnabled,
                dispatchToRenderer,
                _sourceTick);

            // Il renderer e' facoltativo: senza consumer il feed puo' comunque
            // costruire diagnostica e queue interna, utile per test non visuali.
            IArcGraphDebugOverlayQueueConsumer consumer = debugOverlayRenderer;
            _lastDiagnostics = _coordinator.Process(frame, consumer);

            LogLastDiagnostics("ProcessFrame");
            return _lastDiagnostics;
        }

        // =============================================================================
        // ProcessFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Processa un frame usando context, NPC e tick forniti nella chiamata.
        /// </para>
        ///
        /// <para>
        /// Questo overload e' comodo per un futuro adapter esterno: aggiorna prima
        /// lo stato ricevuto e poi usa il percorso standard del wrapper.
        /// </para>
        /// </summary>
        public ArcGraphDebugRuntimeWiringDiagnostics ProcessFrame(
            ArcGraphRuntimeContext runtimeContext,
            int npcId,
            long sourceTick = -1)
        {
            SetRuntimeContext(runtimeContext);
            SetActiveNpcId(npcId);
            SetSourceTick(sourceTick);

            return ProcessFrame();
        }

        // =============================================================================
        // RunContractSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue lo smoke test del contratto C# senza usare la scena.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Run Debug Runtime Wiring Smoke")]
        public void RunContractSmoke()
        {
            ArcGraphDebugRuntimeWiringHarnessResult result =
                ArcGraphDebugRuntimeWiringHarness.RunDefaultSmoke();

            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphDebugRuntimeSceneWrapper] Smoke " + result.Reason +
                " disabled=" + result.DisabledReason +
                ", missingContext=" + result.MissingContextReason +
                ", missingWorld=" + result.MissingWorldReason);
        }

        // =============================================================================
        // ProcessCurrentFrameFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Permette un tentativo manuale da Inspector sullo stato corrente.
        /// </para>
        ///
        /// <para>
        /// Se nessun adapter esterno ha gia' fornito il context, il risultato atteso
        /// e' una diagnostica <c>RuntimeContextMissing</c>. Questo comportamento e'
        /// intenzionale: il wrapper non deve recuperare il mondo da solo.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Process Debug Runtime Frame")]
        public void ProcessCurrentFrameFromInspector()
        {
            ProcessFrame();
        }

        private ArcGraphDebugOverlayRuntimeFeedOptions CreateOptions()
        {
            return new ArcGraphDebugOverlayRuntimeFeedOptions
            {
                IncludeLandmark = includeLandmark,
                IncludeGvdDin = includeGvdDin,
                IncludeDtHeatmap = includeDtHeatmap,
                IncludeGvdRaw = includeGvdRaw,
                IncludeGvdGraph = includeGvdGraph,
                IncludeHiddenItems = includeHiddenItems
            };
        }

        private void LogLastDiagnostics(string source)
        {
            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphDebugRuntimeSceneWrapper] " + source +
                " reason=" + _lastDiagnostics.Reason +
                ", overlay=" + _lastDiagnostics.IsOverlayEnabled +
                ", context=" + _lastDiagnostics.HasContext +
                ", world=" + _lastDiagnostics.HasWorld +
                ", npc=" + _lastDiagnostics.ActiveNpcId +
                ", built=" + _lastDiagnostics.DidBuildFeed +
                ", dispatched=" + _lastDiagnostics.DidDispatchToConsumer +
                ", queueItems=" + _lastDiagnostics.QueueItemCount +
                ", visible=" + _lastDiagnostics.VisibleItemCount);
        }
    }
}
