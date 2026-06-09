using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // IArcGraphSpriteResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto scene-side per risolvere una sprite key ArcGraph in una
    /// <see cref="Sprite"/> Unity.
    /// </para>
    ///
    /// <para><b>Principio architetturale: risoluzione asset confinata</b></para>
    /// <para>
    /// ArcGraph core e i builder di queue non devono chiamare <c>Resources.Load</c>,
    /// non devono leggere asset database e non devono conoscere prefab o cartelle
    /// Unity. Un futuro wrapper scena potra' possedere un resolver concreto,
    /// eventualmente temporaneo, e usarlo solo quando sta davvero disegnando.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryResolveSprite</b>: tenta la conversione chiave -> sprite.</item>
    /// </list>
    /// </summary>
    public interface IArcGraphSpriteResolver
    {
        // =============================================================================
        // TryResolveSprite
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a risolvere una richiesta sprite senza mutare la simulazione.
        /// </para>
        ///
        /// <para><b>Uso previsto</b></para>
        /// <para>
        /// Il metodo appartiene al lato scena. Puo' usare una cache o un catalogo
        /// asset, ma non deve cambiare <c>World</c>, job, oggetti o layer ArcGraph.
        /// Se fallisce, il chiamante deve poter produrre diagnostica o fallback
        /// visuale senza bloccare la simulazione.
        /// </para>
        /// </summary>
        bool TryResolveSprite(
            ArcGraphSpriteResolveRequest request,
            out Sprite sprite);
    }
}
