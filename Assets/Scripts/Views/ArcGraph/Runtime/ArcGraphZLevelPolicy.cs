namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphZLevelPolicy
    // =============================================================================
    /// <summary>
    /// <para>
    /// Policy preparatoria per il livello <c>Z</c> usato da <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: compatibilita' multilivello senza gameplay multilivello</b></para>
    /// <para>
    /// ARCONTIO oggi simula e renderizza solo il piano operativo <c>z = 0</c>.
    /// Tuttavia i contratti grafici devono gia' trasportare coordinate
    /// <c>x/y/z</c>, cosi' il futuro supporto a altitudini, sottosuolo, tetti e celle
    /// cielo non richiedera' di riscrivere tutte le firme. Questa policy dichiara il
    /// livello runtime corrente senza introdurre altitudini giocabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CurrentRuntimeZLevel</b>: unico livello operativo attuale.</item>
    ///   <item><b>DefaultVisibleZLevel</b>: livello mostrato di default dal renderer.</item>
    ///   <item><b>CreateRuntimeCell</b>: helper esplicito per costruire celle runtime attuali.</item>
    ///   <item><b>IsCurrentRuntimeLevel</b>: guardia semantica per evitare assunzioni implicite.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphZLevelPolicy
    {
        public const int CurrentRuntimeZLevel = 0;
        public const int DefaultVisibleZLevel = CurrentRuntimeZLevel;

        // =============================================================================
        // CreateRuntimeCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una coordinata cella sul livello runtime attualmente supportato.
        /// </para>
        ///
        /// <para><b>z = 0 esplicito</b></para>
        /// <para>
        /// Questo helper rende intenzionale il fatto che adapter e bootstrap correnti
        /// lavorino su <c>z = 0</c>. Non deve essere interpretato come conversione
        /// definitiva di qualunque cella multilivello: serve solo alla fase in cui il
        /// runtime non possiede ancora livelli giocabili.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>x/y</b>: coordinate della mappa 2D corrente.</item>
        ///   <item><b>CurrentRuntimeZLevel</b>: livello aggiunto in modo esplicito.</item>
        /// </list>
        /// </summary>
        public static ArcGraphCellCoord CreateRuntimeCell(int x, int y)
        {
            return new ArcGraphCellCoord(x, y, CurrentRuntimeZLevel);
        }

        // =============================================================================
        // IsCurrentRuntimeLevel
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se un valore <c>Z</c> appartiene al livello runtime corrente.
        /// </para>
        ///
        /// <para><b>Guardia dichiarativa, non filtro simulativo</b></para>
        /// <para>
        /// Il metodo non decide cosa esiste nel mondo e non nasconde celle future.
        /// Offre solo una guardia leggibile ai moduli grafici che devono distinguere
        /// il supporto operativo attuale dal contratto multilivello futuro.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>zLevel</b>: livello discreto da confrontare.</item>
        /// </list>
        /// </summary>
        public static bool IsCurrentRuntimeLevel(int zLevel)
        {
            return zLevel == CurrentRuntimeZLevel;
        }
    }
}
