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
    /// vegetation, object o effect. Questo harness controlla i profili default e
    /// un profilo extra non canonico, cosi' la policy resta indipendente dal numero
    /// storico di livelli zoom.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: verifica zoom default e un livello extra arbitrario.</item>
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
        /// Verifica la policy LOD sui default e su un livello extra arbitrario.
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

            var customZoom = new ArcGraphViewZoomLevelDefinition(
                10,
                12,
                12,
                true,
                false,
                false,
                false);
            var zoom10 = ArcGraphZoomLodPolicy.ResolveFromZoom(customZoom);
            if (zoom10.ActorMode != ArcGraphActorLodMode.FullFlatSprite ||
                zoom10.AllowsSpriteAnimation ||
                zoom10.AllowsLayeredActorSprites ||
                zoom10.UsesSimplifiedRepresentation ||
                zoom10.ObjectMode != ArcGraphObjectLodMode.StaticSprites)
            {
                return Fail("Expected arbitrary zoom level 10 to follow JSON flags, not hardcoded level buckets.", out failureReason);
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
