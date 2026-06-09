namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorObjectSceneRendererContract
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto operativo del futuro renderer scena actor/object ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: renderer scena vincolato prima del MonoBehaviour</b></para>
    /// <para>
    /// Prima di creare <c>GameObject</c> e <c>SpriteRenderer</c> serve fissare cosa
    /// il renderer puo' e non puo' fare. Questo contratto descrive un bridge
    /// temporaneo e controllato: consuma <c>ArcGraphRenderQueue</c>, usa un resolver
    /// sprite esterno, crea viste sotto un root dedicato e non migra input, UI o
    /// DevTools.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RootName</b>: nome del root scene-side confinato.</item>
    ///   <item><b>TileWorldSize</b>: scala cella usata per convertire coordinate visuali.</item>
    ///   <item><b>BaseSortingOrder</b>: sorting order iniziale della queue.</item>
    ///   <item><b>SortingOrderStep</b>: incremento per entry ordinata.</item>
    ///   <item><b>RequireExplicitActivation</b>: vieta accensione automatica implicita.</item>
    ///   <item><b>AllowSceneObjectCreation</b>: permette solo oggetti scena temporanei.</item>
    ///   <item><b>UseTemporaryRoot</b>: forza root dedicato e cancellabile.</item>
    ///   <item><b>UseRenderQueueOrder</b>: vieta sorting ricalcolato dal World.</item>
    ///   <item><b>UseActorInterpolatedPose</b>: usa VisualX/Y gia' preparati.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorObjectSceneRendererContract
    {
        public readonly string RootName;
        public readonly float TileWorldSize;
        public readonly int BaseSortingOrder;
        public readonly int SortingOrderStep;
        public readonly bool RequireExplicitActivation;
        public readonly bool AllowSceneObjectCreation;
        public readonly bool UseTemporaryRoot;
        public readonly bool UseRenderQueueOrder;
        public readonly bool UseActorInterpolatedPose;
        public readonly bool AllowInputMigration;
        public readonly bool AllowUiMigration;
        public readonly bool AllowDirectWorldRead;
        public readonly bool AllowBuilderAssetLoad;

        // =============================================================================
        // ArcGraphActorObjectSceneRendererContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un contratto renderer actor/object completo.
        /// </para>
        ///
        /// <para><b>Contratto esplicito</b></para>
        /// <para>
        /// Tutti i flag vengono passati dal chiamante o dalla factory. Questo rende
        /// leggibile quando un futuro renderer dovesse provare a uscire dal
        /// perimetro del probe temporaneo.
        /// </para>
        /// </summary>
        public ArcGraphActorObjectSceneRendererContract(
            string rootName,
            float tileWorldSize,
            int baseSortingOrder,
            int sortingOrderStep,
            bool requireExplicitActivation,
            bool allowSceneObjectCreation,
            bool useTemporaryRoot,
            bool useRenderQueueOrder,
            bool useActorInterpolatedPose,
            bool allowInputMigration,
            bool allowUiMigration,
            bool allowDirectWorldRead,
            bool allowBuilderAssetLoad)
        {
            RootName = string.IsNullOrWhiteSpace(rootName)
                ? "ArcGraphActorObjectSceneProbeRoot"
                : rootName;
            TileWorldSize = tileWorldSize > 0f ? tileWorldSize : 1f;
            BaseSortingOrder = baseSortingOrder;
            SortingOrderStep = sortingOrderStep <= 0 ? 1 : sortingOrderStep;
            RequireExplicitActivation = requireExplicitActivation;
            AllowSceneObjectCreation = allowSceneObjectCreation;
            UseTemporaryRoot = useTemporaryRoot;
            UseRenderQueueOrder = useRenderQueueOrder;
            UseActorInterpolatedPose = useActorInterpolatedPose;
            AllowInputMigration = allowInputMigration;
            AllowUiMigration = allowUiMigration;
            AllowDirectWorldRead = allowDirectWorldRead;
            AllowBuilderAssetLoad = allowBuilderAssetLoad;
        }

        // =============================================================================
        // CreateTemporaryProbeContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il contratto ammesso per il primo probe actor/object.
        /// </para>
        ///
        /// <para><b>Perimetro v0.38d.01</b></para>
        /// <para>
        /// Il contratto ammette creazione scena solo sotto root temporaneo e solo
        /// dopo attivazione esplicita. Blocca input, UI, lettura diretta del
        /// <c>World</c> e asset load nei builder passivi.
        /// </para>
        /// </summary>
        public static ArcGraphActorObjectSceneRendererContract CreateTemporaryProbeContract(
            float tileWorldSize = 1f)
        {
            return new ArcGraphActorObjectSceneRendererContract(
                "ArcGraphActorObjectSceneProbeRoot",
                tileWorldSize,
                baseSortingOrder: 40,
                sortingOrderStep: 1,
                requireExplicitActivation: true,
                allowSceneObjectCreation: true,
                useTemporaryRoot: true,
                useRenderQueueOrder: true,
                useActorInterpolatedPose: true,
                allowInputMigration: false,
                allowUiMigration: false,
                allowDirectWorldRead: false,
                allowBuilderAssetLoad: false);
        }

        // =============================================================================
        // IsTemporaryProbeSafe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se il contratto rispetta il perimetro del probe temporaneo.
        /// </para>
        /// </summary>
        public bool IsTemporaryProbeSafe()
        {
            // La sicurezza qui e' volutamente esplicita: se un flag rischioso viene
            // cambiato, il futuro renderer deve poter bloccare il probe prima di
            // creare qualunque GameObject.
            return RequireExplicitActivation
                   && AllowSceneObjectCreation
                   && UseTemporaryRoot
                   && UseRenderQueueOrder
                   && UseActorInterpolatedPose
                   && !AllowInputMigration
                   && !AllowUiMigration
                   && !AllowDirectWorldRead
                   && !AllowBuilderAssetLoad;
        }
    }
}
