namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphVegetationRuntimeSceneRendererDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica del renderer runtime vegetazione ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservabilita' del bordo visuale</b></para>
    /// <para>
    /// La diagnostica espone gate, prerequisiti, conteggi snapshot/item e fallback
    /// sprite. Non contiene riferimenti a GameObject, SpriteRenderer, World o
    /// Biosfera: serve solo a capire se la pipeline visiva ha ricevuto dati e li
    /// ha materializzati in scena.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Renderer gate</b>: abilitazione, contratto, runtime e layer.</item>
    ///   <item><b>Queue</b>: snapshot letti e item renderizzabili prodotti.</item>
    ///   <item><b>Scene</b>: creati, riusati, disattivati e attivi.</item>
    ///   <item><b>Fallback</b>: sprite mancanti e fallback generati.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphVegetationRuntimeSceneRendererDiagnostics
    {
        public readonly bool RendererEnabled;
        public readonly bool HasContract;
        public readonly bool ContractSafe;
        public readonly bool HasRuntime;
        public readonly bool HasVegetationLayer;
        public readonly bool HasSpriteResolver;
        public readonly int SnapshotCount;
        public readonly int RenderItemCount;
        public readonly int RenderedVegetationCount;
        public readonly int CreatedObjectCount;
        public readonly int ReusedObjectCount;
        public readonly int DisabledObjectCount;
        public readonly int ActiveObjectCount;
        public readonly int MissingSpriteCount;
        public readonly int GeneratedFallbackSpriteCount;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphVegetationRuntimeSceneRendererDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una fotografia diagnostica immutabile normalizzando i conteggi.
        /// </para>
        /// </summary>
        public ArcGraphVegetationRuntimeSceneRendererDiagnostics(
            bool rendererEnabled,
            bool hasContract,
            bool contractSafe,
            bool hasRuntime,
            bool hasVegetationLayer,
            bool hasSpriteResolver,
            int snapshotCount,
            int renderItemCount,
            int renderedVegetationCount,
            int createdObjectCount,
            int reusedObjectCount,
            int disabledObjectCount,
            int activeObjectCount,
            int missingSpriteCount,
            int generatedFallbackSpriteCount,
            string reason)
        {
            RendererEnabled = rendererEnabled;
            HasContract = hasContract;
            ContractSafe = contractSafe;
            HasRuntime = hasRuntime;
            HasVegetationLayer = hasVegetationLayer;
            HasSpriteResolver = hasSpriteResolver;
            SnapshotCount = NormalizeCount(snapshotCount);
            RenderItemCount = NormalizeCount(renderItemCount);
            RenderedVegetationCount = NormalizeCount(renderedVegetationCount);
            CreatedObjectCount = NormalizeCount(createdObjectCount);
            ReusedObjectCount = NormalizeCount(reusedObjectCount);
            DisabledObjectCount = NormalizeCount(disabledObjectCount);
            ActiveObjectCount = NormalizeCount(activeObjectCount);
            MissingSpriteCount = NormalizeCount(missingSpriteCount);
            GeneratedFallbackSpriteCount = NormalizeCount(generatedFallbackSpriteCount);
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
