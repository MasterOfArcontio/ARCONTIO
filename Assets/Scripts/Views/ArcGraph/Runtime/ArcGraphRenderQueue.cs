using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRenderQueue
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contenitore passivo della queue actor/object prodotta da ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: render queue consumabile, non renderer</b></para>
    /// <para>
    /// Questa classe conserva item gia' costruiti e ordinati. Non crea sprite, non
    /// carica asset, non conosce camera e non modifica layer. E' un buffer
    /// value-oriented che un wrapper Unity futuro potra' leggere.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActorItems</b>: item actor tipizzati.</item>
    ///   <item><b>ObjectItems</b>: item oggetto tipizzati.</item>
    ///   <item><b>Entries</b>: ordine globale actor/object.</item>
    ///   <item><b>Diagnostics</b>: contatori aggregati.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphRenderQueue
    {
        private readonly List<ArcGraphActorRenderItem> _actorItems = new();
        private readonly List<ArcGraphObjectRenderItem> _objectItems = new();
        private readonly List<ArcGraphRenderQueueEntry> _entries = new();

        private ArcGraphRenderQueueDiagnostics _diagnostics;

        public IReadOnlyList<ArcGraphActorRenderItem> ActorItems => _actorItems;
        public IReadOnlyList<ArcGraphObjectRenderItem> ObjectItems => _objectItems;
        public IReadOnlyList<ArcGraphRenderQueueEntry> Entries => _entries;
        public ArcGraphRenderQueueDiagnostics Diagnostics => _diagnostics;

        internal List<ArcGraphActorRenderItem> MutableActorItems => _actorItems;
        internal List<ArcGraphObjectRenderItem> MutableObjectItems => _objectItems;
        internal List<ArcGraphRenderQueueEntry> MutableEntries => _entries;

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota la queue senza toccare layer, world o asset.
        /// </para>
        /// </summary>
        public void Clear()
        {
            _actorItems.Clear();
            _objectItems.Clear();
            _entries.Clear();
            _diagnostics = default;
        }

        // =============================================================================
        // SetDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna la diagnostica aggregata della queue.
        /// </para>
        ///
        /// <para><b>Uso interno al builder</b></para>
        /// <para>
        /// La diagnostica e' scritta dal builder dopo aver popolato item ed entry.
        /// Tenerla separata dai metodi pubblici evita che codice esterno modifichi
        /// arbitrariamente i contatori.
        /// </para>
        /// </summary>
        internal void SetDiagnostics(ArcGraphRenderQueueDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }
    }
}
