using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRuntimeContextProvider
    // =============================================================================
    /// <summary>
    /// <para>
    /// Base comune per i componenti scena che forniscono ad ArcGraph un
    /// <see cref="ArcGraphRuntimeContext"/>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: sorgente runtime neutra</b></para>
    /// <para>
    /// Il wrapper ArcGraph non deve conoscere il vecchio adapter MapGrid concreto.
    /// Deve chiedere un context read-only a una frontiera dichiarata. Questa base
    /// permette di usare un provider neutro basato su <c>World</c> nel runtime
    /// normale e di conservare adapter legacy solo per probe o confronti mirati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildTerrainRuntimeContext</b>: costruisce il context letto dai renderer ArcGraph.</item>
    ///   <item><b>ProviderKind</b>: etichetta diagnostica della sorgente usata.</item>
    /// </list>
    /// </summary>
    public abstract class ArcGraphRuntimeContextProvider : MonoBehaviour
    {
        public virtual string ProviderKind => GetType().Name;

        // =============================================================================
        // BuildTerrainRuntimeContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il context runtime ArcGraph senza mutare la simulazione.
        /// </para>
        /// </summary>
        public abstract ArcGraphRuntimeContext BuildTerrainRuntimeContext();
    }
}
