namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisualResolveInput
    // =============================================================================
    /// <summary>
    /// <para>
    /// Input passivo per risolvere il tile visuale finale di una cella terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: resolver senza accesso alla mappa</b></para>
    /// <para>
    /// Il resolver non deve interrogare direttamente il layer terrain o il World.
    /// Riceve gia' il tipo terreno corrente, le informazioni sintetiche sui vicini
    /// e il tempo visuale opzionale. Un futuro adapter terrain costruira' questo
    /// input leggendo snapshot e vicinato, mantenendo il resolver puro e testabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: coordinata della cella, usata come seed deterministico.</item>
    ///   <item><b>TerrainId</b>: tipo terreno della cella corrente.</item>
    ///   <item><b>NeighborTerrainId</b>: tipo terreno confinante dominante, opzionale.</item>
    ///   <item><b>NeighborMask</b>: maschera cardinale dei lati dove compare il vicino.</item>
    ///   <item><b>VisualTimeSeconds</b>: tempo visuale per animazioni frame-based.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualResolveInput
    {
        public readonly ArcGraphCellCoord Cell;
        public readonly string TerrainId;
        public readonly string NeighborTerrainId;
        public readonly string NeighborMask;
        public readonly float VisualTimeSeconds;

        // =============================================================================
        // ArcGraphTerrainVisualResolveInput
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un input di risoluzione tile visuale normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualResolveInput(
            ArcGraphCellCoord cell,
            string terrainId,
            string neighborTerrainId,
            string neighborMask,
            float visualTimeSeconds)
        {
            Cell = cell;
            TerrainId = terrainId;
            NeighborTerrainId = neighborTerrainId;
            NeighborMask = neighborMask;
            VisualTimeSeconds = visualTimeSeconds < 0f ? 0f : visualTimeSeconds;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualResolveResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Esito immutabile della risoluzione visuale di una cella terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: output spiegabile</b></para>
    /// <para>
    /// Il risultato non contiene solo il tile id finale, ma anche la ragione della
    /// scelta. Questo rende verificabile se il tile arriva da transizione, animazione,
    /// variante o fallback.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TileId</b>: tile visuale finale da passare poi alla UV map.</item>
    ///   <item><b>UsedTransition</b>: true se una regola bordo ha vinto.</item>
    ///   <item><b>UsedAnimation</b>: true se un frame animato ha vinto.</item>
    ///   <item><b>UsedVariant</b>: true se e' stata scelta una variante pesata.</item>
    ///   <item><b>Reason</b>: stringa sintetica per diagnostica.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualResolveResult
    {
        public readonly int TileId;
        public readonly bool UsedTransition;
        public readonly bool UsedAnimation;
        public readonly bool UsedVariant;
        public readonly string Reason;

        // =============================================================================
        // ArcGraphTerrainVisualResolveResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un esito di risoluzione tile visuale.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualResolveResult(
            int tileId,
            bool usedTransition,
            bool usedAnimation,
            bool usedVariant,
            string reason)
        {
            TileId = tileId;
            UsedTransition = usedTransition;
            UsedAnimation = usedAnimation;
            UsedVariant = usedVariant;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver passivo che decide quale tile visuale usare per una cella terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: decisione visuale locale e deterministica</b></para>
    /// <para>
    /// Il resolver non crea mesh e non conosce Unity. Applica una priorita' semplice:
    /// prima transizioni di bordo, poi animazione, poi variante deterministica, poi
    /// fallback al tile default. Questa gerarchia mantiene leggibile il risultato e
    /// impedisce che ogni feature grafica diventi un sistema decisionale parallelo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Resolve</b>: applica catalogo e input a una singola cella.</item>
    ///   <item><b>BuildStableSeed</b>: produce una scelta variante stabile per cella.</item>
    ///   <item><b>ResolveAnimationFrame</b>: converte tempo visuale in frame index.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainVisualResolver
    {
        // =============================================================================
        // Resolve
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il tile visuale finale usando catalogo e input locali.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualResolveResult Resolve(
            ArcGraphTerrainVisualCatalog catalog,
            ArcGraphTerrainVisualResolveInput input)
        {
            if (catalog == null)
                return new ArcGraphTerrainVisualResolveResult(0, false, false, false, "CatalogMissing");

            if (!catalog.TryGetDefinition(input.TerrainId, out var definition) || definition == null)
                return new ArcGraphTerrainVisualResolveResult(0, false, false, false, "TerrainDefinitionMissing");

            if (!string.IsNullOrWhiteSpace(input.NeighborTerrainId)
                && !string.IsNullOrWhiteSpace(input.NeighborMask)
                && catalog.TryGetTransitionSet(input.TerrainId, input.NeighborTerrainId, out var transitionSet)
                && transitionSet.TryResolveTileId(
                    input.NeighborMask,
                    input.VisualTimeSeconds,
                    out int transitionTileId,
                    out bool usedAnimatedTransition))
            {
                return new ArcGraphTerrainVisualResolveResult(
                    transitionTileId,
                    usedTransition: true,
                    usedAnimation: usedAnimatedTransition,
                    usedVariant: false,
                    reason: usedAnimatedTransition ? "AnimatedTransitionRule" : "TransitionRule");
            }

            if (definition.HasAnimation)
            {
                int frameIndex = ResolveAnimationFrame(definition.Animation, input.VisualTimeSeconds);
                return new ArcGraphTerrainVisualResolveResult(
                    definition.Animation.GetFrameTileId(frameIndex),
                    usedTransition: false,
                    usedAnimation: true,
                    usedVariant: false,
                    reason: "AnimationFrame");
            }

            int seed = BuildStableSeed(input.Cell, definition.TerrainId);
            int variantTileId = definition.ResolveVariantTileId(seed);
            return new ArcGraphTerrainVisualResolveResult(
                variantTileId,
                usedTransition: false,
                usedAnimation: false,
                usedVariant: true,
                reason: "DeterministicVariant");
        }

        private static int ResolveAnimationFrame(
            ArcGraphTerrainVisualAnimation animation,
            float visualTimeSeconds)
        {
            if (!animation.IsValid)
                return 0;

            return (int)(visualTimeSeconds / animation.FrameSeconds);
        }

        private static int BuildStableSeed(
            ArcGraphCellCoord cell,
            string terrainId)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = Mix(hash, (uint)cell.X);
                hash = Mix(hash, (uint)cell.Y);
                hash = Mix(hash, (uint)cell.Z);
                hash = Mix(hash, (uint)StableStringHash(terrainId));

                // Una griglia regolare tende a mostrare pattern se il seed resta
                // quasi lineare e poi viene ridotto con modulo piccolo, ad esempio
                // nove varianti di prato. Questo finalizer spezza le correlazioni
                // tra celle vicine mantenendo pero' il risultato deterministico.
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                hash *= 3266489917u;
                hash ^= hash >> 16;

                return (int)hash;
            }
        }

        private static uint Mix(uint hash, uint value)
        {
            unchecked
            {
                hash ^= value + 0x9E3779B9u + (hash << 6) + (hash >> 2);
                return hash;
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                    hash = (hash * 37) + value[i];

                return hash;
            }
        }
    }
}
