using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcRuntimeSceneRendererContract
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto operativo del renderer runtime minimo per gli NPC ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: NPC renderer confinato e non decisionale</b></para>
    /// <para>
    /// Il renderer NPC deve ricevere una <c>ArcGraphRenderQueue</c> gia' preparata
    /// dal runtime ArcGraph e limitarsi a materializzare gli actor visibili in
    /// scena. Non legge il <c>World</c>, non cerca MapGrid, non invia comandi e non
    /// decide quali NPC esistano: usa solo il payload visuale gia' ordinato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RootName</b>: root locale sotto cui vengono creati gli NPC ArcGraph.</item>
    ///   <item><b>TileWorldSize</b>: scala cella usata dal piano actor/object.</item>
    ///   <item><b>OriginOffset</b>: offset scena applicato agli sprite.</item>
    ///   <item><b>ZOffset</b>: piccolo offset di profondita' per separare layer scena.</item>
    ///   <item><b>ActorScale</b>: scala uniforme degli sprite NPC.</item>
    ///   <item><b>DisableMissingActorsAfterRender</b>: disattiva handle non presenti nel frame.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphNpcRuntimeSceneRendererContract
    {
        public readonly string RootName;
        public readonly float TileWorldSize;
        public readonly Vector3 OriginOffset;
        public readonly float ZOffset;
        public readonly float ActorScale;
        public readonly bool DisableMissingActorsAfterRender;

        public bool IsRuntimeSafe =>
            !string.IsNullOrWhiteSpace(RootName)
            && TileWorldSize > 0f
            && ActorScale > 0f;

        // =============================================================================
        // ArcGraphNpcRuntimeSceneRendererContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un contratto NPC runtime normalizzando i valori rischiosi.
        /// </para>
        /// </summary>
        public ArcGraphNpcRuntimeSceneRendererContract(
            string rootName,
            float tileWorldSize,
            Vector3 originOffset,
            float zOffset,
            float actorScale,
            bool disableMissingActorsAfterRender)
        {
            RootName = string.IsNullOrWhiteSpace(rootName)
                ? "ArcGraphNpcRuntimeRoot"
                : rootName;
            TileWorldSize = tileWorldSize > 0f ? tileWorldSize : 1f;
            OriginOffset = originOffset;
            ZOffset = zOffset;
            ActorScale = actorScale > 0f ? actorScale : 1f;
            DisableMissingActorsAfterRender = disableMissingActorsAfterRender;
        }

        // =============================================================================
        // CreateActorObjectPlanContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il contratto passivo usato dal builder actor/object esistente.
        /// </para>
        ///
        /// <para><b>Riuso controllato</b></para>
        /// <para>
        /// Il renderer NPC usa il builder actor/object solo per calcolare posizione,
        /// sorting e sprite request. Dopo il build filtra le entry actor e ignora le
        /// entry object, che saranno gestite da un renderer oggetti dedicato futuro.
        /// </para>
        /// </summary>
        public ArcGraphActorObjectSceneRendererContract CreateActorObjectPlanContract()
        {
            return ArcGraphActorObjectSceneRendererContract.CreateTemporaryProbeContract(TileWorldSize);
        }
    }
}
