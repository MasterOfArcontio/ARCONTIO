namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphZoomLodPolicy
    // =============================================================================
    /// <summary>
    /// <para>
    /// Policy LOD ArcGraph scollegata dallo zoom camera.
    /// </para>
    ///
    /// <para><b>Principio architetturale: zoom non decide sprite</b></para>
    /// <para>
    /// La policy restituisce sempre dettaglio pieno. Resta come punto centrale per
    /// i builder che gia' ricevono un profilo LOD, ma non legge piu' livelli zoom e
    /// non puo' attivare asset alternativi.
    /// </para>
    /// </summary>
    public static class ArcGraphZoomLodPolicy
    {
        public static ArcGraphZoomLodProfile Resolve(
            ArcGraphMapViewConfig config,
            ArcGraphViewState state)
        {
            return ArcGraphZoomLodProfile.CreateFullDetail();
        }

        public static ArcGraphZoomLodProfile ResolveFullDetail()
        {
            return ArcGraphZoomLodProfile.CreateFullDetail();
        }
    }
}
