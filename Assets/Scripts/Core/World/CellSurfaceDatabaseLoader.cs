using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // CellSurfaceDatabaseLoader
    // =============================================================================
    /// <summary>
    /// <para>
    /// Loader del catalogo unico dei tipi di superficie cella.
    /// </para>
    ///
    /// <para><b>Principio architetturale: definizioni data-driven centralizzate</b></para>
    /// <para>
    /// Le superfici vengono caricate in <see cref="World.SurfaceDefs"/> come dati
    /// read-only di catalogo. Il loader non crea mappa, non modifica oggetti, non
    /// aggiorna cache calde e non decide visuali runtime: rende solo disponibili
    /// definizioni stabili a Core, Biosfera futura e ArcGraph.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ResourcePath</b>: path Resources del file senza estensione.</item>
    ///   <item><b>LoadIntoWorld</b>: entry point chiamato dal bootstrap runtime.</item>
    ///   <item><b>NormalizeDefinition</b>: completa default minimi per definizioni parziali.</item>
    /// </list>
    /// </summary>
    public static class CellSurfaceDatabaseLoader
    {
        private const string ResourcePath = "Arcontio/Config/surface_defs";

        // =============================================================================
        // LoadIntoWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica <c>surface_defs.json</c> dentro il catalogo superfici del mondo.
        /// </para>
        /// </summary>
        public static void LoadIntoWorld(World world)
        {
            if (world == null)
                return;

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[SurfaceDB] Missing resource at Assets/Resources/" + ResourcePath + ".json");
                return;
            }

            CellSurfaceDefDatabase db = JsonUtility.FromJson<CellSurfaceDefDatabase>(asset.text);
            if (db == null || db.Surfaces == null)
            {
                Debug.LogWarning("[SurfaceDB] JSON parsed null/empty. Root key must be 'Surfaces'.");
                return;
            }

            world.SurfaceDefs.Clear();

            int added = 0;
            for (int i = 0; i < db.Surfaces.Count; i++)
            {
                CellSurfaceDef def = db.Surfaces[i];
                if (def == null || string.IsNullOrWhiteSpace(def.Id))
                    continue;

                NormalizeDefinition(def);
                world.SurfaceDefs[def.Id] = def;
                added++;
            }

            Debug.Log("[SurfaceDB] Loaded surface defs: " + added);
        }

        // =============================================================================
        // NormalizeDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica default conservativi alle definizioni incomplete.
        /// </para>
        /// </summary>
        private static void NormalizeDefinition(CellSurfaceDef def)
        {
            if (string.IsNullOrWhiteSpace(def.DisplayName))
                def.DisplayName = def.Id;

            if (string.IsNullOrWhiteSpace(def.MacroSurface))
                def.MacroSurface = "Natural";

            if (def.MovementCost <= 0f)
                def.MovementCost = 1f;

            if (def.Visual == null)
                def.Visual = new CellSurfaceVisualDef();

            if (string.IsNullOrWhiteSpace(def.Visual.VisualRuleKey))
                def.Visual.VisualRuleKey = def.Id;

            if (string.IsNullOrWhiteSpace(def.Visual.ArcGraphTileKey))
                def.Visual.ArcGraphTileKey = def.Visual.VisualRuleKey;
        }
    }
}
