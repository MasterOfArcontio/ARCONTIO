using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.View.MapGrid;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWorldAdapter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Adapter read-only tra lo stato runtime corrente di ARCONTIO e i contratti
    /// minimi di <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte visuale senza authority simulativa</b></para>
    /// <para>
    /// Questo adapter legge dati gia' esistenti da <c>MapGridData</c> e <c>World</c>
    /// e li converte in snapshot grafici copiati. Non crea GameObject, non carica
    /// sprite, non aggiorna mesh, non chiama command, non muta il <c>World</c> e non
    /// decide cosa un NPC debba fare. Serve soltanto a stabilire il primo confine
    /// tecnico tra simulazione e futuro rendering modulare.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CurrentRuntimeZLevel</b>: livello <c>z = 0</c> usato dal runtime attuale.</item>
    ///   <item><b>FillTerrainSnapshots</b>: converte il buffer terreno MapGrid in celle ArcGraph.</item>
    ///   <item><b>FillObjectSnapshots</b>: converte gli oggetti del World in snapshot visuali.</item>
    ///   <item><b>FillActorSnapshots</b>: converte gli NPC del World in snapshot actor.</item>
    ///   <item><b>ResolveObjectSpriteKey</b>: replica solo la policy di fallback sprite corrente.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphWorldAdapter
    {
        public const int CurrentRuntimeZLevel = 0;

        private readonly string _defaultNpcSpriteKey;
        private readonly string _fallbackObjectSpritePrefix;

        // =============================================================================
        // ArcGraphWorldAdapter
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un adapter con path fallback coerenti con la MapGrid attuale.
        /// </para>
        ///
        /// <para><b>Compatibilita' con renderer legacy</b></para>
        /// <para>
        /// Il default NPC replica lo sprite provvisorio oggi usato da
        /// <c>MapGridWorldView</c>. Il prefisso oggetti replica il fallback
        /// <c>MapGrid/Sprites/Objects/{defId}</c>. Questi valori non caricano asset:
        /// vengono solo copiati negli snapshot per il renderer futuro.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>defaultNpcSpriteKey</b>: chiave sprite fallback per gli NPC.</item>
        ///   <item><b>fallbackObjectSpritePrefix</b>: prefisso fallback per oggetti senza SpriteKey.</item>
        /// </list>
        /// </summary>
        public ArcGraphWorldAdapter(
            string defaultNpcSpriteKey = "MapGrid/Sprites/NPC_Astro",
            string fallbackObjectSpritePrefix = "MapGrid/Sprites/Objects/")
        {
            _defaultNpcSpriteKey = string.IsNullOrWhiteSpace(defaultNpcSpriteKey)
                ? "MapGrid/Sprites/NPC_Astro"
                : defaultNpcSpriteKey;

            _fallbackObjectSpritePrefix = string.IsNullOrWhiteSpace(fallbackObjectSpritePrefix)
                ? "MapGrid/Sprites/Objects/"
                : fallbackObjectSpritePrefix;
        }

        // =============================================================================
        // FillTerrainSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte tutte le celle di <c>MapGridData</c> in snapshot terreno ArcGraph.
        /// </para>
        ///
        /// <para><b>Confine MapGrid -> ArcGraph</b></para>
        /// <para>
        /// La sorgente e' ancora il buffer view-side della MapGrid attuale. Questo
        /// metodo non legge il <c>World</c> e non modifica il buffer: percorre la
        /// griglia, copia tile e blocco, assegna <c>z = 0</c> e riempie la lista
        /// fornita dal chiamante.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>map</b>: sorgente terreno corrente.</item>
        ///   <item><b>target</b>: lista riusabile da popolare.</item>
        ///   <item><b>clearTarget</b>: se true, svuota la lista prima di aggiungere snapshot.</item>
        /// </list>
        /// </summary>
        public void FillTerrainSnapshots(
            MapGridData map,
            IList<ArcGraphTerrainCellSnapshot> target,
            bool clearTarget = true)
        {
            if (target == null)
                return;

            if (clearTarget)
                target.Clear();

            if (map == null)
                return;

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    // MapGridData garantisce l'indicizzazione per coordinate in bounds.
                    // L'adapter aggiunge solo il livello z=0, senza inventare altitudini.
                    var cell = new ArcGraphCellCoord(x, y, CurrentRuntimeZLevel);
                    target.Add(new ArcGraphTerrainCellSnapshot(
                        cell,
                        map.GetTerrain(x, y),
                        map.IsBlocked(x, y)));
                }
            }
        }

        // =============================================================================
        // FillObjectSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte gli oggetti presenti nel <c>World</c> in snapshot visuali copiati.
        /// </para>
        ///
        /// <para><b>World come sorgente oggettiva, ArcGraph come consumer</b></para>
        /// <para>
        /// Il metodo legge <c>World.Objects</c>, <c>World.ObjectDefs</c> attraverso
        /// <c>TryGetObjectDef</c> e <c>World.FoodStocks</c>. Queste letture sono
        /// lecite per la presentazione grafica, perche' non alimentano decisioni NPC
        /// e non modificano alcuno store. Gli oggetti nulli vengono ignorati.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>world</b>: source of truth oggettiva della simulazione.</item>
        ///   <item><b>target</b>: lista riusabile da popolare.</item>
        ///   <item><b>includeHeldObjects</b>: include o esclude oggetti trasportati.</item>
        ///   <item><b>clearTarget</b>: se true, svuota la lista prima di aggiungere snapshot.</item>
        /// </list>
        /// </summary>
        public void FillObjectSnapshots(
            World world,
            IList<ArcGraphObjectVisualSnapshot> target,
            bool includeHeldObjects = false,
            bool clearTarget = true)
        {
            if (target == null)
                return;

            if (clearTarget)
                target.Clear();

            if (world == null)
                return;

            foreach (var pair in world.Objects)
            {
                WorldObjectInstance instance = pair.Value;
                if (instance == null)
                    continue;

                if (instance.IsHeld && !includeHeldObjects)
                    continue;

                int stockUnits = -1;
                if (world.FoodStocks.TryGetValue(pair.Key, out var stock))
                    stockUnits = stock.Units;

                target.Add(new ArcGraphObjectVisualSnapshot(
                    pair.Key,
                    instance.DefId,
                    new ArcGraphCellCoord(instance.CellX, instance.CellY, CurrentRuntimeZLevel),
                    ResolveObjectSpriteKey(world, instance),
                    instance.IsHeld,
                    instance.HolderNpcId,
                    stockUnits));
            }
        }

        // =============================================================================
        // FillActorSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte le posizioni NPC correnti del <c>World</c> in snapshot actor.
        /// </para>
        ///
        /// <para><b>Movimento visuale non ancora inventato</b></para>
        /// <para>
        /// In questo checkpoint l'adapter non deduce interpolazione da
        /// <c>NpcAction.MoveTo</c>, perche' quello stato non contiene origine e
        /// progresso affidabili del segmento in corso. Gli snapshot usano quindi
        /// <c>ArcGraphActorMotionSnapshot.None</c>. Il collegamento ai progress
        /// multi-tick reali appartiene al checkpoint <c>v0.30g</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>world</b>: source of truth per le posizioni NPC discrete.</item>
        ///   <item><b>target</b>: lista riusabile da popolare.</item>
        ///   <item><b>clearTarget</b>: se true, svuota la lista prima di aggiungere snapshot.</item>
        /// </list>
        /// </summary>
        public void FillActorSnapshots(
            World world,
            IList<ArcGraphActorVisualSnapshot> target,
            bool clearTarget = true)
        {
            if (target == null)
                return;

            if (clearTarget)
                target.Clear();

            if (world == null)
                return;

            foreach (var pair in world.GridPos)
            {
                // GridPos e' la sorgente oggettiva della posizione discreta. Il fatto
                // che l'NPC esista viene verificato per evitare snapshot orfani.
                int actorId = pair.Key;
                if (!world.ExistsNpc(actorId))
                    continue;

                var position = pair.Value;
                var cell = new ArcGraphCellCoord(position.X, position.Y, CurrentRuntimeZLevel);
                target.Add(new ArcGraphActorVisualSnapshot(
                    actorId,
                    cell,
                    _defaultNpcSpriteKey,
                    ArcGraphActorMotionSnapshot.None(cell)));
            }
        }

        // =============================================================================
        // ResolveObjectSpriteKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la chiave sprite di un oggetto usando la stessa policy base della
        /// view MapGrid corrente.
        /// </para>
        ///
        /// <para><b>Riuso controllato della convenzione legacy</b></para>
        /// <para>
        /// Se la definizione oggetto contiene <c>SpriteKey</c>, l'adapter usa quel
        /// valore. In caso contrario produce il fallback storico
        /// <c>MapGrid/Sprites/Objects/{defId}</c>. Non chiama <c>Resources.Load</c>:
        /// la risoluzione asset resta responsabilita' del renderer.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>world</b>: necessario solo per leggere la definizione oggetto.</item>
        ///   <item><b>instance</b>: istanza da cui prendere il defId.</item>
        /// </list>
        /// </summary>
        private string ResolveObjectSpriteKey(World world, WorldObjectInstance instance)
        {
            if (instance == null || string.IsNullOrWhiteSpace(instance.DefId))
                return string.Empty;

            if (world != null
                && world.TryGetObjectDef(instance.DefId, out var def)
                && def != null
                && !string.IsNullOrWhiteSpace(def.SpriteKey))
            {
                return def.SpriteKey;
            }

            return _fallbackObjectSpritePrefix + instance.DefId;
        }
    }
}
