using System;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // WorldMapConfigLoader
    // =============================================================================
    /// <summary>
    /// <para>
    /// Loader Core del file mappa unico iniziale di ARCONTIO.
    /// </para>
    ///
    /// <para><b>Principio architetturale: una mappa iniziale, piu' consumer derivati</b></para>
    /// <para>
    /// Il file mappa contiene dimensioni, assegnazioni del pavimento e oggetti
    /// iniziali. Questo sostituisce il precedente layout solo-superfici e prepara
    /// il distacco da MapGrid: il <c>World</c> riceve una descrizione iniziale
    /// Core, mentre ArcGraph legge poi snapshot/ViewModel derivati dal <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Map</b>: dimensioni autoritative e metadati minimi.</item>
    ///   <item><b>SurfaceLayer</b>: fill globale piu' patch rettangolari deterministiche.</item>
    ///   <item><b>InitialObjects</b>: oggetti iniziali opzionali, applicati tramite API World.</item>
    ///   <item><b>LoadIntoWorld</b>: entry point unico del bootstrap runtime.</item>
    /// </list>
    /// </summary>
    public static class WorldMapConfigLoader
    {
        private const string ResourcePath = "Arcontio/Config/world_map_default";

        // =============================================================================
        // LoadIntoWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica il file mappa iniziale e lo applica al mondo ricevuto.
        /// </para>
        ///
        /// <para><b>Fail-safe deterministico</b></para>
        /// <para>
        /// Se il file manca o non e' leggibile, il mondo conserva la mappa gia'
        /// inizializzata dal costruttore. Non viene generata una mappa casuale
        /// implicita, perche' i test ArcGraph e Biosfera devono partire da una
        /// baseline ripetibile.
        /// </para>
        /// </summary>
        public static bool LoadIntoWorld(World world)
        {
            if (world == null)
                return false;

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[WorldMapConfigLoader] Missing resource " + ResourcePath + ". Keeping existing World map.");
                return false;
            }

            WorldMapDto map;
            try
            {
                map = JsonUtility.FromJson<WorldMapDto>(asset.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WorldMapConfigLoader] Parse failed for " + ResourcePath + ": " + ex.Message);
                return false;
            }

            if (map == null || map.Map == null)
            {
                Debug.LogWarning("[WorldMapConfigLoader] Parsed map is null for " + ResourcePath + ".");
                return false;
            }

            ApplyDimensions(world, map.Map);
            ApplySurfaceLayer(world, map.SurfaceLayer);
            ApplyInitialObjects(world, map.InitialObjects);
            return true;
        }

        // =============================================================================
        // ApplyDimensions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica le dimensioni autoritative del file mappa al <see cref="World"/>.
        /// </para>
        /// </summary>
        private static void ApplyDimensions(World world, WorldMapHeaderDto header)
        {
            int width = Math.Max(1, header.Width);
            int height = Math.Max(1, header.Height);

            if (world.MapWidth == width && world.MapHeight == height)
                return;

            // Reinizializzare qui e' sicuro nel bootstrap ordinario perche' gli
            // oggetti iniziali non sono ancora stati creati. Nel percorso snapshot,
            // SimulationHost reimposta poi la dimensione salvata prima del load DTO.
            world.InitMap(width, height);
        }

        // =============================================================================
        // ApplySurfaceLayer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica fill e patch superfici usando il catalogo <c>surface_defs</c>.
        /// </para>
        /// </summary>
        private static void ApplySurfaceLayer(World world, WorldMapSurfaceLayerDto layerDto)
        {
            if (world.CellSurfaces == null)
                return;

            ApplySurfaceFill(world, layerDto?.Fill);
            ApplySurfacePatches(world, layerDto?.Patches);
        }

        // =============================================================================
        // ApplySurfaceFill
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copre tutta la mappa con una superficie di base dichiarata.
        /// </para>
        /// </summary>
        private static void ApplySurfaceFill(World world, WorldMapSurfaceCellDto fill)
        {
            ResolveSurface(
                world,
                fill?.SurfaceKey,
                out CellSurfaceMacro macro,
                out string surfaceKey,
                out string visualRuleKey);

            string biomeAreaKey = fill?.BiomeAreaKey;

            for (int y = 0; y < world.CellSurfaces.Height; y++)
            {
                for (int x = 0; x < world.CellSurfaces.Width; x++)
                    world.CellSurfaces.SetSurface(x, y, macro, surfaceKey, visualRuleKey, biomeAreaKey);
            }
        }

        // =============================================================================
        // ApplySurfacePatches
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica rettangoli di superfici specializzate sopra il fill globale.
        /// </para>
        /// </summary>
        private static void ApplySurfacePatches(World world, WorldMapSurfacePatchDto[] patches)
        {
            if (patches == null || patches.Length == 0)
                return;

            for (int i = 0; i < patches.Length; i++)
            {
                WorldMapSurfacePatchDto patch = patches[i];
                if (patch == null || patch.W <= 0 || patch.H <= 0)
                    continue;

                ResolveSurface(
                    world,
                    patch.SurfaceKey,
                    out CellSurfaceMacro macro,
                    out string surfaceKey,
                    out string visualRuleKey);

                for (int y = patch.Y; y < patch.Y + patch.H; y++)
                {
                    for (int x = patch.X; x < patch.X + patch.W; x++)
                    {
                        if (world.CellSurfaces.InBounds(x, y))
                            world.CellSurfaces.SetSurface(x, y, macro, surfaceKey, visualRuleKey, patch.BiomeAreaKey);
                    }
                }
            }
        }

        // =============================================================================
        // ApplyInitialObjects
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea gli oggetti iniziali dichiarati dalla mappa tramite API del World.
        /// </para>
        /// </summary>
        private static void ApplyInitialObjects(World world, WorldMapInitialObjectDto[] objects)
        {
            if (objects == null || objects.Length == 0)
                return;

            for (int i = 0; i < objects.Length; i++)
            {
                WorldMapInitialObjectDto entry = objects[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.DefId))
                    continue;

                OwnerKind ownerKind = ParseOwnerKind(entry.OwnerKind);
                int objectId = world.CreateObject(entry.DefId, entry.X, entry.Y, ownerKind, entry.OwnerId);
                if (objectId < 0)
                    continue;

                if (entry.FoodUnits > 0)
                {
                    world.SetFoodStock(objectId, new FoodStockComponent
                    {
                        Units = entry.FoodUnits,
                        OwnerKind = ownerKind,
                        OwnerId = entry.OwnerId
                    });
                }
            }
        }

        // =============================================================================
        // ResolveSurface
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve una chiave superficie consultando il catalogo se disponibile.
        /// </para>
        /// </summary>
        private static void ResolveSurface(
            World world,
            string requestedSurfaceKey,
            out CellSurfaceMacro macro,
            out string surfaceKey,
            out string visualRuleKey)
        {
            surfaceKey = string.IsNullOrWhiteSpace(requestedSurfaceKey)
                ? CellSurfaceLayer.DefaultNaturalSurfaceKey
                : requestedSurfaceKey;

            if (world.TryGetSurfaceDef(surfaceKey, out CellSurfaceDef def))
            {
                macro = def.ResolveMacroSurface();
                visualRuleKey = def.ResolveVisualRuleKey();
                return;
            }

            macro = CellSurfaceMacro.Natural;
            surfaceKey = CellSurfaceLayer.NormalizeSurfaceKey(surfaceKey, macro);
            visualRuleKey = surfaceKey;
        }

        // =============================================================================
        // ParseOwnerKind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il testo JSON del proprietario nella enum Core.
        /// </para>
        /// </summary>
        private static OwnerKind ParseOwnerKind(string raw)
        {
            if (string.Equals(raw, "Npc", StringComparison.OrdinalIgnoreCase))
                return OwnerKind.Npc;

            if (string.Equals(raw, "Group", StringComparison.OrdinalIgnoreCase))
                return OwnerKind.Group;

            if (string.Equals(raw, "Community", StringComparison.OrdinalIgnoreCase))
                return OwnerKind.Community;

            return OwnerKind.None;
        }

        [Serializable]
        private sealed class WorldMapDto
        {
            public WorldMapHeaderDto Map;
            public WorldMapSurfaceLayerDto SurfaceLayer;
            public WorldMapInitialObjectDto[] InitialObjects;
        }

        [Serializable]
        private sealed class WorldMapHeaderDto
        {
            public string Id;
            public int Width;
            public int Height;
            public int ZLevel;
        }

        [Serializable]
        private sealed class WorldMapSurfaceLayerDto
        {
            public WorldMapSurfaceCellDto Fill;
            public WorldMapSurfacePatchDto[] Patches;
        }

        [Serializable]
        private sealed class WorldMapSurfaceCellDto
        {
            public string SurfaceKey;
            public string BiomeAreaKey;
        }

        [Serializable]
        private sealed class WorldMapSurfacePatchDto
        {
            public int X;
            public int Y;
            public int W;
            public int H;
            public string SurfaceKey;
            public string BiomeAreaKey;
        }

        [Serializable]
        private sealed class WorldMapInitialObjectDto
        {
            public string DefId;
            public int X;
            public int Y;
            public string OwnerKind;
            public int OwnerId = -1;
            public int FoodUnits;
        }
    }
}
