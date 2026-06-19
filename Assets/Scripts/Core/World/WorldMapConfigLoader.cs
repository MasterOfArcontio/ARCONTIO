using System;
using Arcontio.Core.Environment;
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
            ApplyBiologicalAreas(world, map.BiologicalAreas);
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

                string defId = NormalizeInitialObjectDefId(entry.DefId);
                OwnerKind ownerKind = ParseOwnerKind(entry.OwnerKind);
                int objectId = world.CreateObject(defId, entry.X, entry.Y, ownerKind, entry.OwnerId);
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

                ApplyInitialDoorState(world, objectId, entry);
            }
        }

        // =============================================================================
        // NormalizeInitialObjectDefId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza defId storici presenti negli export DevMap verso il catalogo
        /// oggetti Core attuale.
        /// </para>
        /// </summary>
        private static string NormalizeInitialObjectDefId(string defId)
        {
            if (string.IsNullOrWhiteSpace(defId))
                return string.Empty;

            string key = defId.Trim();
            return string.Equals(key, "door_wood_good", StringComparison.OrdinalIgnoreCase)
                ? "door_wood"
                : key;
        }

        // =============================================================================
        // ApplyInitialDoorState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica stato aperta/chiusa e lock alle porte dichiarate dalla mappa
        /// iniziale, usando <see cref="World.SetDoorOpen"/> per mantenere coerenti
        /// le cache di movimento e visione.
        /// </para>
        /// </summary>
        private static void ApplyInitialDoorState(
            World world,
            int objectId,
            WorldMapInitialObjectDto entry)
        {
            if (world == null || entry == null || objectId < 0)
                return;

            if (!world.Objects.TryGetValue(objectId, out WorldObjectInstance instance)
                || instance == null)
            {
                return;
            }

            if (!world.TryGetObjectDef(instance.DefId, out ObjectDef def)
                || def == null
                || !def.IsDoor)
            {
                return;
            }

            instance.IsLocked = def.IsLockable && entry.IsLocked;
            world.SetDoorOpen(objectId, entry.IsOpen);
        }

        // =============================================================================
        // ApplyBiologicalAreas
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte le aree biologiche minime dichiarate dalla mappa nel DTO
        /// Environment usato dal bootstrap biosfera.
        /// </para>
        ///
        /// <para><b>Principio architetturale: mappa come sorgente spaziale, biosfera come sorgente biologica</b></para>
        /// <para>
        /// Il file mappa dichiara dove esistono aree biologiche iniziali e con quale
        /// intensita' di partenza. Il loader non crea piante, sprite, landmark o
        /// oggetti: prepara solo il pacchetto dati che <see cref="SimulationHost"/>
        /// consegnera' alla Environment Foundation.
        /// </para>
        /// </summary>
        private static void ApplyBiologicalAreas(World world, WorldMapBiologicalAreaDto[] biologicalAreas)
        {
            if (world == null)
                return;

            if (biologicalAreas == null || biologicalAreas.Length == 0)
            {
                world.SetInitialEnvironmentAreaSetConfig(new EnvironmentAreaSetConfig());
                return;
            }

            var areaConfigs = new EnvironmentAreaConfig[biologicalAreas.Length];
            var fertilityConfigs = new EnvironmentFertilityAreaConfig[biologicalAreas.Length];
            var vegetationConfigs = new EnvironmentVegetationAreaConfig[biologicalAreas.Length];
            var seedBankConfigs = new EnvironmentSeedBankAreaConfig[biologicalAreas.Length];

            for (int i = 0; i < biologicalAreas.Length; i++)
            {
                WorldMapBiologicalAreaDto area = biologicalAreas[i] ?? new WorldMapBiologicalAreaDto();
                int areaId = area.AreaId > 0 ? area.AreaId : i + 1;
                int radius = Math.Max(0, area.RadiusCells);
                float intensity = Clamp01(area.Intensity01, 0.5f);
                string biomeKey = ResolveNonEmpty(area.BiomeKey, "grassland");
                string areaKey = ResolveNonEmpty(area.Key, biomeKey + "_" + areaId);

                areaConfigs[i] = new EnvironmentAreaConfig
                {
                    areaId = areaId,
                    kind = "Vegetation",
                    minX = area.CenterX - radius,
                    minY = area.CenterY - radius,
                    maxX = area.CenterX + radius,
                    maxY = area.CenterY + radius,
                    z = area.Z,
                    centerX = area.CenterX,
                    centerY = area.CenterY,
                    radiusCells = radius,
                    priority = area.Priority,
                    isEnabled = area.IsEnabled,
                    key = areaKey
                };

                fertilityConfigs[i] = new EnvironmentFertilityAreaConfig
                {
                    areaId = areaId,
                    soilKind = ResolveNonEmpty(area.SoilKind, biomeKey),
                    baseFertility01 = Clamp01(area.BaseFertility01, intensity),
                    currentFertility01 = Clamp01(area.CurrentFertility01, Clamp01(area.BaseFertility01, intensity)),
                    growthModifier01 = intensity,
                    exhaustion01 = 0f,
                    recovery01 = Clamp01(area.Recovery01, 0.5f)
                };

                vegetationConfigs[i] = new EnvironmentVegetationAreaConfig
                {
                    areaId = areaId,
                    vegetationKind = ResolveNonEmpty(area.VegetationKind, biomeKey),
                    density01 = intensity,
                    growthPotential01 = Clamp01(area.GrowthPotential01, intensity),
                    health01 = Clamp01(area.Health01, 0.75f),
                    fertilityInfluence01 = 0.5f,
                    climateInfluence01 = 0.5f
                };

                seedBankConfigs[i] = new EnvironmentSeedBankAreaConfig
                {
                    areaId = areaId,
                    entries = BuildSeedBankEntries(area, biomeKey, intensity)
                };
            }

            world.SetInitialEnvironmentAreaSetConfig(new EnvironmentAreaSetConfig
            {
                areas = areaConfigs,
                fertilityAreas = fertilityConfigs,
                waterAreas = new EnvironmentWaterAreaConfig[0],
                vegetationAreas = vegetationConfigs,
                seedBankAreas = seedBankConfigs
            });
        }

        private static EnvironmentSeedBankEntryConfig[] BuildSeedBankEntries(
            WorldMapBiologicalAreaDto area,
            string biomeKey,
            float intensity)
        {
            if (area != null && area.SeedBank != null && area.SeedBank.Length > 0)
            {
                var configured = new EnvironmentSeedBankEntryConfig[area.SeedBank.Length];
                for (int i = 0; i < area.SeedBank.Length; i++)
                {
                    WorldMapSeedBankEntryDto entry = area.SeedBank[i] ?? new WorldMapSeedBankEntryDto();
                    configured[i] = new EnvironmentSeedBankEntryConfig
                    {
                        speciesKey = ResolveNonEmpty(entry.SpeciesKey, biomeKey),
                        amount01 = Clamp01(entry.Amount01, intensity),
                        viability01 = Clamp01(entry.Viability01, intensity)
                    };
                }

                return configured;
            }

            return new[]
            {
                new EnvironmentSeedBankEntryConfig
                {
                    speciesKey = biomeKey,
                    amount01 = intensity,
                    viability01 = intensity
                }
            };
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

        private static string ResolveNonEmpty(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? (fallback ?? string.Empty)
                : value;
        }

        private static float Clamp01(float value, float fallback)
        {
            float resolved = float.IsNaN(value) ? fallback : value;
            if (resolved < 0f) return 0f;
            if (resolved > 1f) return 1f;
            return resolved;
        }

        [Serializable]
        private sealed class WorldMapDto
        {
            public WorldMapHeaderDto Map;
            public WorldMapSurfaceLayerDto SurfaceLayer;
            public WorldMapInitialObjectDto[] InitialObjects;
            public WorldMapBiologicalAreaDto[] BiologicalAreas;
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
            public bool IsOpen;
            public bool IsLocked;
        }

        [Serializable]
        private sealed class WorldMapBiologicalAreaDto
        {
            public int AreaId;
            public string Key;
            public string BiomeKey;
            public string VegetationKind;
            public string SoilKind;
            public int CenterX;
            public int CenterY;
            public int RadiusCells;
            public int Z;
            public int Priority;
            public bool IsEnabled = true;
            public float Intensity01 = 0.5f;
            public float BaseFertility01 = 0.5f;
            public float CurrentFertility01 = 0.5f;
            public float GrowthPotential01 = 0.5f;
            public float Health01 = 0.75f;
            public float Recovery01 = 0.5f;
            public WorldMapSeedBankEntryDto[] SeedBank;
        }

        [Serializable]
        private sealed class WorldMapSeedBankEntryDto
        {
            public string SpeciesKey;
            public float Amount01 = 0.5f;
            public float Viability01 = 0.5f;
        }
    }
}
