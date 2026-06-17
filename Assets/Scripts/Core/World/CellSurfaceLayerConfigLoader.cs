using System;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // CellSurfaceLayerConfigLoader
    // =============================================================================
    /// <summary>
    /// <para>
    /// Loader Core per popolare <see cref="CellSurfaceLayer"/> da un file JSON
    /// sotto <c>Assets/Resources</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: mappa di debug ripetibile, non renderer legacy</b></para>
    /// <para>
    /// In questa fase ArcGraph non deve piu' ricavare il pavimento da
    /// <c>MapGridData</c>. Il pavimento viene letto dal <c>World</c>, e il
    /// <c>World</c> viene inizializzato da un file Core esplicito. Questo mantiene
    /// i test ripetibili e permette di sostituire in futuro il file con un
    /// generatore random seeded o con un loader di scenario senza cambiare
    /// ArcGraph.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ResourcePath</b>: path del layout superfici senza estensione.</item>
    ///   <item><b>LoadIntoWorld</b>: entry point chiamato dal bootstrap runtime.</item>
    ///   <item><b>ApplyFill</b>: imposta una superficie di base su tutta la mappa.</item>
    ///   <item><b>ApplyPatches</b>: applica rettangoli dichiarati dal file.</item>
    ///   <item><b>ParseMacroSurface</b>: converte testo JSON in enum Core.</item>
    /// </list>
    /// </summary>
    public static class CellSurfaceLayerConfigLoader
    {
        private const string ResourcePath = "Arcontio/Config/cell_surface_layout";

        // =============================================================================
        // LoadIntoWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica il layout superfici e lo applica al <see cref="World"/> ricevuto.
        /// </para>
        ///
        /// <para><b>Fail-safe controllato</b></para>
        /// <para>
        /// Se il file manca o non e' valido, il mondo conserva il default creato da
        /// <c>World.InitMap</c>: superficie naturale <c>grass</c> su tutte le celle.
        /// Non viene generata una mappa random implicita, per evitare test non
        /// ripetibili.
        /// </para>
        /// </summary>
        public static bool LoadIntoWorld(World world)
        {
            if (world == null || world.CellSurfaces == null)
                return false;

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[CellSurfaceLayerConfigLoader] Missing resource " + ResourcePath + ". Keeping default grass surface layer.");
                return false;
            }

            CellSurfaceLayoutDto layout;
            try
            {
                layout = JsonUtility.FromJson<CellSurfaceLayoutDto>(asset.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CellSurfaceLayerConfigLoader] Parse failed for " + ResourcePath + ": " + ex.Message);
                return false;
            }

            if (layout == null)
            {
                Debug.LogWarning("[CellSurfaceLayerConfigLoader] Parsed layout is null for " + ResourcePath + ".");
                return false;
            }

            ApplyFill(world.CellSurfaces, layout.fill);
            ApplyPatches(world.CellSurfaces, layout.patches);
            return true;
        }

        // =============================================================================
        // ApplyFill
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica una superficie di base a tutte le celle del layer.
        /// </para>
        /// </summary>
        private static void ApplyFill(CellSurfaceLayer layer, CellSurfaceDefinitionDto fill)
        {
            CellSurfaceMacro macro = ParseMacroSurface(fill?.macroSurface, CellSurfaceMacro.Natural);
            string surfaceKey = string.IsNullOrWhiteSpace(fill?.surfaceKey)
                ? CellSurfaceLayer.NormalizeSurfaceKey(string.Empty, macro)
                : fill.surfaceKey;
            string visualRuleKey = string.IsNullOrWhiteSpace(fill?.visualRuleKey)
                ? surfaceKey
                : fill.visualRuleKey;
            string biomeAreaKey = fill?.biomeAreaKey;

            for (int y = 0; y < layer.Height; y++)
            {
                for (int x = 0; x < layer.Width; x++)
                {
                    // Il fill viene considerato assegnazione esplicita: ArcGraph puo'
                    // sapere che il layer Core e' popolato e smettere di usare
                    // fallback legacy.
                    layer.SetSurface(x, y, macro, surfaceKey, visualRuleKey, biomeAreaKey);
                }
            }
        }

        // =============================================================================
        // ApplyPatches
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica le patch rettangolari dichiarate nel layout.
        /// </para>
        /// </summary>
        private static void ApplyPatches(CellSurfaceLayer layer, CellSurfacePatchDto[] patches)
        {
            if (patches == null || patches.Length == 0)
                return;

            for (int i = 0; i < patches.Length; i++)
            {
                CellSurfacePatchDto patch = patches[i];
                if (patch == null || patch.w <= 0 || patch.h <= 0)
                    continue;

                CellSurfaceMacro macro = ParseMacroSurface(patch.macroSurface, CellSurfaceMacro.Natural);
                string surfaceKey = string.IsNullOrWhiteSpace(patch.surfaceKey)
                    ? CellSurfaceLayer.NormalizeSurfaceKey(string.Empty, macro)
                    : patch.surfaceKey;
                string visualRuleKey = string.IsNullOrWhiteSpace(patch.visualRuleKey)
                    ? surfaceKey
                    : patch.visualRuleKey;

                for (int y = patch.y; y < patch.y + patch.h; y++)
                {
                    for (int x = patch.x; x < patch.x + patch.w; x++)
                    {
                        if (layer.InBounds(x, y))
                            layer.SetSurface(x, y, macro, surfaceKey, visualRuleKey, patch.biomeAreaKey);
                    }
                }
            }
        }

        // =============================================================================
        // ParseMacroSurface
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una stringa JSON nella macro superficie Core.
        /// </para>
        /// </summary>
        private static CellSurfaceMacro ParseMacroSurface(string raw, CellSurfaceMacro fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            if (string.Equals(raw, "water", StringComparison.OrdinalIgnoreCase))
                return CellSurfaceMacro.Water;

            if (string.Equals(raw, "artificial", StringComparison.OrdinalIgnoreCase))
                return CellSurfaceMacro.Artificial;

            if (string.Equals(raw, "natural", StringComparison.OrdinalIgnoreCase))
                return CellSurfaceMacro.Natural;

            return fallback;
        }

        [Serializable]
        private sealed class CellSurfaceLayoutDto
        {
            public int width;
            public int height;
            public CellSurfaceDefinitionDto fill;
            public CellSurfacePatchDto[] patches;
        }

        [Serializable]
        private sealed class CellSurfaceDefinitionDto
        {
            public string macroSurface;
            public string surfaceKey;
            public string visualRuleKey;
            public string biomeAreaKey;
        }

        [Serializable]
        private sealed class CellSurfacePatchDto
        {
            public int x;
            public int y;
            public int w;
            public int h;
            public string macroSurface;
            public string surfaceKey;
            public string visualRuleKey;
            public string biomeAreaKey;
        }
    }
}
