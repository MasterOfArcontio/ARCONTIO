using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainRuntimeSceneRendererContract
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto scene-side per il renderer runtime minimo del terreno ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: rendering terrain dichiarato, non implicito</b></para>
    /// <para>
    /// Il contratto contiene solo i parametri necessari per materializzare chunk
    /// terrain in scena: materiale, offset, ordinamento, pulizia del dirty state e
    /// nome root locale. Non contiene <c>World</c>, non contiene <c>MapGridData</c>,
    /// non carica asset e non decide quando il renderer deve essere eseguito.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TerrainMaterial</b>: materiale assegnato dall'esterno.</item>
    ///   <item><b>RootName</b>: nome del root locale sotto cui vivono i chunk ArcGraph.</item>
    ///   <item><b>OriginOffset</b>: offset world del root terrain.</item>
    ///   <item><b>ZOffset</b>: profondita' visuale del layer terrain.</item>
    ///   <item><b>SortingOrder</b>: ordine render dei chunk terrain.</item>
    ///   <item><b>ClearDirtyAfterRender</b>: policy di consumo del dirty state.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainRuntimeSceneRendererContract
    {
        public readonly Material TerrainMaterial;
        public readonly string RootName;
        public readonly Vector3 OriginOffset;
        public readonly float ZOffset;
        public readonly int SortingOrder;
        public readonly bool ClearDirtyAfterRender;

        public bool HasMaterial => TerrainMaterial != null;
        public bool HasRootName => !string.IsNullOrWhiteSpace(RootName);
        public bool IsRuntimeSafe => HasMaterial && HasRootName;

        // =============================================================================
        // ArcGraphTerrainRuntimeSceneRendererContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un contratto terrain runtime normalizzando i valori minimi.
        /// </para>
        ///
        /// <para><b>Normalizzazione locale</b></para>
        /// <para>
        /// Il nome root vuoto viene sostituito con un nome standard. Il materiale
        /// resta invece obbligatorio: ArcGraph non deve cercarlo, caricarlo o
        /// inventarlo autonomamente.
        /// </para>
        /// </summary>
        public ArcGraphTerrainRuntimeSceneRendererContract(
            Material terrainMaterial,
            string rootName,
            Vector3 originOffset,
            float zOffset,
            int sortingOrder,
            bool clearDirtyAfterRender)
        {
            TerrainMaterial = terrainMaterial;
            RootName = string.IsNullOrWhiteSpace(rootName)
                ? "ArcGraphTerrainRuntimeRoot"
                : rootName;
            OriginOffset = originOffset;
            ZOffset = zOffset;
            SortingOrder = sortingOrder;
            ClearDirtyAfterRender = clearDirtyAfterRender;
        }

        // =============================================================================
        // CreateDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il contratto runtime terrain standard per il primo renderer minimo.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainRuntimeSceneRendererContract CreateDefault(
            Material terrainMaterial)
        {
            return new ArcGraphTerrainRuntimeSceneRendererContract(
                terrainMaterial,
                "ArcGraphTerrainRuntimeRoot",
                Vector3.zero,
                -0.05f,
                -5,
                clearDirtyAfterRender: true);
        }
    }
}
