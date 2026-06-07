namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphZoomLodPolicyHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare la policy LOD zoom ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: policy verificabile senza renderer</b></para>
    /// <para>
    /// La policy LOD deve essere verificabile prima di avere renderer actor,
    /// vegetation, object o effect. Questo harness controlla solo i profili risolti
    /// dai default <c>v0.33</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: verifica zoom 1, 2, 3 e 4.</item>
    ///   <item><b>Fail</b>: restituisce motivo esplicito.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphZoomLodPolicyHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica la policy LOD sui quattro zoom default.
        /// </para>
        /// </summary>
        public static bool RunDefaultSmoke(out string failureReason)
        {
            failureReason = string.Empty;

            var config = ArcGraphMapViewConfig.CreateDefaultV033();

            var zoom1 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(1));
            if (zoom1.ActorMode != ArcGraphActorLodMode.StrategicMarker ||
                zoom1.AllowsSpriteAnimation ||
                !zoom1.UsesSimplifiedRepresentation ||
                zoom1.ShowMinorItems)
            {
                return Fail("Unexpected zoom 1 LOD profile.", out failureReason);
            }

            var zoom2 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(2));
            if (zoom2.ActorMode != ArcGraphActorLodMode.SimplifiedStaticSprite ||
                zoom2.AllowsSpriteAnimation ||
                !zoom2.UsesSimplifiedRepresentation)
            {
                return Fail("Unexpected zoom 2 LOD profile.", out failureReason);
            }

            var zoom3 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(3));
            if (zoom3.ActorMode != ArcGraphActorLodMode.FullFlatSprite ||
                !zoom3.AllowsSpriteAnimation ||
                zoom3.AllowsLayeredActorSprites ||
                zoom3.UsesSimplifiedRepresentation)
            {
                return Fail("Unexpected zoom 3 LOD profile.", out failureReason);
            }

            var zoom4 = ArcGraphZoomLodPolicy.ResolveFromZoom(config.ResolveZoomLevel(4));
            if (zoom4.ActorMode != ArcGraphActorLodMode.LayeredSprite ||
                !zoom4.AllowsSpriteAnimation ||
                !zoom4.AllowsLayeredActorSprites ||
                zoom4.UsesSimplifiedRepresentation)
            {
                return Fail("Unexpected zoom 4 LOD profile.", out failureReason);
            }

            return true;
        }

        private static bool Fail(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }
    }
}
