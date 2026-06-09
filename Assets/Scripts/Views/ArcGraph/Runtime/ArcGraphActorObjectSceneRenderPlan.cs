using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorObjectSceneRenderPlan
    // =============================================================================
    /// <summary>
    /// <para>
    /// Piano passivo delle entry actor/object che un futuro renderer scena dovra'
    /// materializzare.
    /// </para>
    ///
    /// <para><b>Principio architetturale: output scene-side testabile senza scena</b></para>
    /// <para>
    /// Il plan contiene entry gia' ordinate e diagnostica sintetica. Non conserva
    /// riferimenti a <c>SpriteRenderer</c>, <c>GameObject</c>, camera, materiali o
    /// asset. Permette di validare il contratto del renderer prima di introdurre il
    /// componente Unity concreto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Entries</b>: entry scene-side in ordine di disegno.</item>
    ///   <item><b>Diagnostics</b>: contatori e ragione dell'ultimo build.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphActorObjectSceneRenderPlan
    {
        private readonly List<ArcGraphActorObjectSceneRenderEntry> _entries = new();
        private ArcGraphActorObjectSceneRendererDiagnostics _diagnostics;

        public IReadOnlyList<ArcGraphActorObjectSceneRenderEntry> Entries => _entries;
        public ArcGraphActorObjectSceneRendererDiagnostics Diagnostics => _diagnostics;

        internal List<ArcGraphActorObjectSceneRenderEntry> MutableEntries => _entries;

        // =============================================================================
        // Clear
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota il plan senza toccare queue, layer, asset o scena.
        /// </para>
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _diagnostics = default;
        }

        // =============================================================================
        // SetDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra la diagnostica prodotta dal builder del plan.
        /// </para>
        /// </summary>
        internal void SetDiagnostics(
            ArcGraphActorObjectSceneRendererDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }
    }
}
