using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectRuntimeSceneRendererContract
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto operativo del renderer runtime minimo per gli oggetti ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: oggetti come viste, non come simulazione</b></para>
    /// <para>
    /// Il renderer oggetti riceve una <c>ArcGraphRenderQueue</c> gia' costruita dal
    /// runtime minimo e materializza solo gli oggetti visibili. Non legge il
    /// <c>World</c>, non consulta MapGrid, non invia comandi e non decide cosa
    /// esista: usa solo snapshot visuali derivati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RootName</b>: root locale sotto cui vengono creati gli oggetti.</item>
    ///   <item><b>TileWorldSize</b>: scala cella usata dal piano actor/object.</item>
    ///   <item><b>OriginOffset</b>: offset scena applicato agli sprite.</item>
    ///   <item><b>ZOffset</b>: piccolo offset di profondita' per separare gli oggetti dal terreno.</item>
    ///   <item><b>ObjectScale</b>: scala uniforme degli sprite oggetto.</item>
    ///   <item><b>DisableMissingObjectsAfterRender</b>: disattiva handle non presenti nel frame.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphObjectRuntimeSceneRendererContract
    {
        public readonly string RootName;
        public readonly float TileWorldSize;
        public readonly Vector3 OriginOffset;
        public readonly float ZOffset;
        public readonly float ObjectScale;
        public readonly bool DisableMissingObjectsAfterRender;

        public bool IsRuntimeSafe =>
            !string.IsNullOrWhiteSpace(RootName)
            && TileWorldSize > 0f
            && ObjectScale > 0f;

        // =============================================================================
        // ArcGraphObjectRuntimeSceneRendererContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un contratto oggetti runtime normalizzando i valori rischiosi.
        /// </para>
        /// </summary>
        public ArcGraphObjectRuntimeSceneRendererContract(
            string rootName,
            float tileWorldSize,
            Vector3 originOffset,
            float zOffset,
            float objectScale,
            bool disableMissingObjectsAfterRender)
        {
            RootName = string.IsNullOrWhiteSpace(rootName)
                ? "ArcGraphObjectRuntimeRoot"
                : rootName;
            TileWorldSize = tileWorldSize > 0f ? tileWorldSize : 1f;
            OriginOffset = originOffset;
            ZOffset = zOffset;
            ObjectScale = objectScale > 0f ? objectScale : 1f;
            DisableMissingObjectsAfterRender = disableMissingObjectsAfterRender;
        }

        // =============================================================================
        // CreateActorObjectPlanContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il contratto passivo usato dal builder actor/object esistente.
        /// </para>
        /// </summary>
        public ArcGraphActorObjectSceneRendererContract CreateActorObjectPlanContract()
        {
            return ArcGraphActorObjectSceneRendererContract.CreateTemporaryProbeContract(TileWorldSize);
        }
    }
}
