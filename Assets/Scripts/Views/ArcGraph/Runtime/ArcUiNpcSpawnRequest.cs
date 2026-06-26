namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiNpcSpawnFacing
    // =============================================================================
    /// <summary>
    /// <para>
    /// Direzione iniziale richiesta dalla UI per uno spawn NPC futuro.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contratto UI senza Core diretto</b></para>
    /// <para>
    /// La request UI non usa direttamente <c>CardinalDirection</c> del Core. Il
    /// mapping verso il comando autorizzato verra' fatto nel bridge spawn futuro,
    /// mantenendo questo contratto nel layer ArcGraph UI.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>North/South/East/West</b>: direzioni discrete iniziali.</item>
    /// </list>
    /// </summary>
    public enum ArcUiNpcSpawnFacing
    {
        South = 0,
        North = 1,
        East = 2,
        West = 3
    }

    // =============================================================================
    // ArcUiNpcSpawnConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione minima per la shell di spawn NPC.
    /// </para>
    ///
    /// <para><b>Contratto asciutto</b></para>
    /// <para>
    /// Per ora lo step non introduce DNA editabile, inventario o archetipi. Tiene
    /// solo la variante visuale e il facing iniziale, cioe' i dati necessari a
    /// mostrare una preview sensata e a preparare il futuro comando.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>VisualKey</b>: variante visuale letta dal catalogo NPC ArcGraph.</item>
    ///   <item><b>Facing</b>: direzione iniziale scelta nel pannello.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiNpcSpawnConfig
    {
        public readonly string VisualKey;
        public readonly ArcUiNpcSpawnFacing Facing;

        public ArcUiNpcSpawnConfig(
            string visualKey,
            ArcUiNpcSpawnFacing facing)
        {
            VisualKey = string.IsNullOrWhiteSpace(visualKey) ? "human_default" : visualKey.Trim().ToLowerInvariant();
            Facing = facing;
        }

        public static ArcUiNpcSpawnConfig Default()
        {
            return new ArcUiNpcSpawnConfig("human_default", ArcUiNpcSpawnFacing.South);
        }

        public ArcUiNpcSpawnConfig WithFacing(ArcUiNpcSpawnFacing facing)
        {
            return new ArcUiNpcSpawnConfig(VisualKey, facing);
        }
    }

    // =============================================================================
    // ArcUiNpcSpawnRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta UI minima per uno spawn NPC futuro.
    /// </para>
    ///
    /// <para><b>Principio architetturale: spawn request prima del comando</b></para>
    /// <para>
    /// Questa struttura non spawna NPC e non conosce il <c>World</c>. Raccoglie
    /// solo operation, configurazione e cella target. Il bridge comandi futuro
    /// decidera' come trasformarla in comando autorizzato, eventualmente dopo la
    /// configurazione DNA nel RightInspector.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>OperationKey</b>: chiave UI, ad esempio <c>spawn_npc_human</c>.</item>
    ///   <item><b>TargetCell</b>: cella scelta sulla mappa.</item>
    ///   <item><b>Config</b>: configurazione minima preview/facing.</item>
    ///   <item><b>HasTargetCell</b>: indica se la cella e' gia' stata acquisita.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiNpcSpawnRequest
    {
        public readonly string OperationKey;
        public readonly ArcGraphCellCoord TargetCell;
        public readonly ArcUiNpcSpawnConfig Config;
        public readonly bool HasTargetCell;

        public bool IsValid => !string.IsNullOrWhiteSpace(OperationKey);

        public ArcUiNpcSpawnRequest(
            string operationKey,
            ArcGraphCellCoord targetCell,
            ArcUiNpcSpawnConfig config,
            bool hasTargetCell)
        {
            OperationKey = ArcUiOperationDefinition.NormalizeKey(operationKey);
            TargetCell = targetCell;
            Config = config;
            HasTargetCell = hasTargetCell;
        }

        public static ArcUiNpcSpawnRequest WithoutCell(
            string operationKey,
            ArcUiNpcSpawnConfig config)
        {
            return new ArcUiNpcSpawnRequest(
                operationKey,
                new ArcGraphCellCoord(0, 0, 0),
                config,
                false);
        }
    }
}
