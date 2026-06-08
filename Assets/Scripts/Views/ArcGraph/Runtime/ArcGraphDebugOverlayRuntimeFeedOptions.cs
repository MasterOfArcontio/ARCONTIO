namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphDebugOverlayRuntimeFeedOptions
    // =============================================================================
    /// <summary>
    /// <para>
    /// Opzioni esplicite per alimentare il feed runtime degli overlay debug ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: debug selettivo senza manager onnisciente</b></para>
    /// <para>
    /// Il feed non decide da solo quali strumenti debug mostrare. Il chiamante passa
    /// flag espliciti per landmark, GVD-DIN e sotto-layer collegati. In questo modo
    /// ArcGraph resta un consumer grafico controllato e non replica i toggle input
    /// oggi contenuti nel vecchio <c>MapGridWorldView</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IncludeLandmark</b>: abilita dati landmark/pathfinding per NPC attivo.</item>
    ///   <item><b>IncludeGvdDin</b>: abilita dati GVD-DIN globali.</item>
    ///   <item><b>IncludeDtHeatmap</b>: include celle Distance Transform.</item>
    ///   <item><b>IncludeGvdRaw</b>: include celle GVD grezze.</item>
    ///   <item><b>IncludeGvdGraph</b>: include nodi ed edge GVD post-pruning.</item>
    ///   <item><b>IncludeHiddenItems</b>: conserva item hidden nella queue per QA.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphDebugOverlayRuntimeFeedOptions
    {
        public bool IncludeLandmark { get; set; } = true;
        public bool IncludeGvdDin { get; set; } = true;
        public bool IncludeDtHeatmap { get; set; } = true;
        public bool IncludeGvdRaw { get; set; } = true;
        public bool IncludeGvdGraph { get; set; } = true;
        public bool IncludeHiddenItems { get; set; }

        // =============================================================================
        // CreateDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il profilo runtime standard del feed debug.
        /// </para>
        ///
        /// <para><b>Default conservativo</b></para>
        /// <para>
        /// Landmark e GVD-DIN sono attivi perche' sono i soli producer pronti per
        /// il bridge runtime. FOV current cone, HUD e strumenti interattivi non
        /// compaiono qui perche' non hanno ancora un producer dati separato.
        /// </para>
        /// </summary>
        public static ArcGraphDebugOverlayRuntimeFeedOptions CreateDefault()
        {
            return new ArcGraphDebugOverlayRuntimeFeedOptions();
        }
    }
}
