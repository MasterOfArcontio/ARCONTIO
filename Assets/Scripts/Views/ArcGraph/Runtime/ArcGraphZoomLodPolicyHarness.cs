namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphZoomLodPolicyHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness minimale della policy LOD ArcGraph senza livelli zoom.
    /// </para>
    /// </summary>
    public static class ArcGraphZoomLodPolicyHarness
    {
        public static bool Run()
        {
            ArcGraphZoomLodProfile profile = ArcGraphZoomLodPolicy.ResolveFullDetail();

            return profile.AllowsSpriteAnimation &&
                   profile.AllowsLayeredActorSprites &&
                   profile.ShowMinorItems &&
                   profile.ActorMode == ArcGraphActorLodMode.LayeredSprite &&
                   profile.ObjectMode == ArcGraphObjectLodMode.DetailedSprites;
        }
    }
}
