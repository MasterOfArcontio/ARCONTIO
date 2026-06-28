using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVegetationRuntimeSceneRendererContract
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto scene-side per materializzare vegetazione e piante ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: rendering vegetazione senza autorita' biologica</b></para>
    /// <para>
    /// Il contratto contiene solo parametri di scena: root, scala, offset,
    /// sorting e policy fallback. Non contiene riferimenti a Biosfera, World,
    /// seed bank o cataloghi simulativi. Il renderer usa questi valori per
    /// trasformare snapshot ArcGraph gia' derivati in SpriteRenderer Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RootName</b>: root locale posseduto dal renderer.</item>
    ///   <item><b>TileWorldSize</b>: scala cella -> world.</item>
    ///   <item><b>OriginOffset/ZOffset</b>: posizionamento scene-side.</item>
    ///   <item><b>VegetationScale</b>: scala sprite vegetazione.</item>
    ///   <item><b>BaseSortingOrder</b>: base sorting Unity prima dell'indice queue.</item>
    ///   <item><b>DisableMissingVegetationAfterRender</b>: spegne handle non aggiornati.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphVegetationRuntimeSceneRendererContract
    {
        public readonly string RootName;
        public readonly float TileWorldSize;
        public readonly Vector3 OriginOffset;
        public readonly float ZOffset;
        public readonly float VegetationScale;
        public readonly int BaseSortingOrder;
        public readonly bool DisableMissingVegetationAfterRender;

        public bool IsRuntimeSafe =>
            !string.IsNullOrWhiteSpace(RootName)
            && TileWorldSize > 0.0001f
            && VegetationScale > 0.0001f;

        // =============================================================================
        // ArcGraphVegetationRuntimeSceneRendererContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il contratto normalizzando solo valori che non possono essere
        /// nulli o negativi.
        /// </para>
        /// </summary>
        public ArcGraphVegetationRuntimeSceneRendererContract(
            string rootName,
            float tileWorldSize,
            Vector3 originOffset,
            float zOffset,
            float vegetationScale,
            int baseSortingOrder,
            bool disableMissingVegetationAfterRender)
        {
            RootName = string.IsNullOrWhiteSpace(rootName)
                ? "ArcGraphVegetationRuntimeRoot"
                : rootName.Trim();
            TileWorldSize = tileWorldSize <= 0.0001f ? 1f : tileWorldSize;
            OriginOffset = originOffset;
            ZOffset = zOffset;
            VegetationScale = vegetationScale <= 0.0001f ? 1f : vegetationScale;
            BaseSortingOrder = baseSortingOrder;
            DisableMissingVegetationAfterRender = disableMissingVegetationAfterRender;
        }
    }
}
