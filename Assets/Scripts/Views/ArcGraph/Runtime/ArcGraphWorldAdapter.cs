using System.Collections.Generic;
using Arcontio.Core;

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
    /// Questo adapter legge dati gia' esistenti dal <c>World</c>
    /// e li converte in snapshot grafici copiati. Non crea GameObject, non carica
    /// sprite, non aggiorna mesh, non chiama command, non muta il <c>World</c> e non
    /// decide cosa un NPC debba fare. Serve soltanto a stabilire il primo confine
    /// tecnico tra simulazione e futuro rendering modulare.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CurrentRuntimeZLevel</b>: livello <c>z = 0</c> usato dal runtime attuale.</item>
    ///   <item><b>FillTerrainSnapshots</b>: converte <c>CellSurfaceLayer</c> in celle ArcGraph.</item>
    ///   <item><b>FillObjectSnapshots</b>: converte gli oggetti del World in snapshot visuali.</item>
    ///   <item><b>FillActorSnapshots</b>: converte gli NPC del World in snapshot actor e motion read-only.</item>
    ///   <item><b>ResolveObjectSpriteKey</b>: copia solo sprite path ArcGraph espliciti.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphWorldAdapter
    {
        public const int CurrentRuntimeZLevel = ArcGraphZLevelPolicy.CurrentRuntimeZLevel;

        private readonly string _defaultNpcSpriteKey;

        // =============================================================================
        // ArcGraphWorldAdapter
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un adapter con fallback visuali dichiarati.
        /// </para>
        ///
        /// <para><b>Compatibilita' controllata con cataloghi espliciti</b></para>
        /// <para>
        /// Il default NPC usa la chiave del catalogo ArcGraph. Gli oggetti non
        /// ricevono piu' un fallback automatico verso MapGrid: devono dichiarare
        /// esplicitamente il proprio path visuale nella sezione <c>Visual</c> del
        /// catalogo oggetti.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>defaultNpcSpriteKey</b>: chiave sprite fallback per gli NPC.</item>
        /// </list>
        /// </summary>
        public ArcGraphWorldAdapter(
            string defaultNpcSpriteKey = "human_default")
        {
            _defaultNpcSpriteKey = string.IsNullOrWhiteSpace(defaultNpcSpriteKey)
                ? "human_default"
                : defaultNpcSpriteKey;
        }

        // =============================================================================
        // FillTerrainSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte le superfici Core del mondo in snapshot terreno ArcGraph.
        /// </para>
        ///
        /// <para><b>Distacco terrain da MapGrid</b></para>
        /// <para>
        /// Il metodo non riceve <c>MapGridData</c>, non legge tile id legacy e non
        /// ricava pavimenti dal renderer storico. Ogni cella viene copiata da
        /// <c>World.CellSurfaces</c>; il <c>TileId</c> dello snapshot resta a 0 come
        /// campo compatibile, ma il mapper ArcGraph usa <c>SurfaceKey</c> e
        /// <c>VisualRuleKey</c>.
        /// </para>
        /// </summary>
        public void FillTerrainSnapshots(
            CellSurfaceLayer cellSurfaces,
            IList<ArcGraphTerrainCellSnapshot> target,
            bool clearTarget = true)
        {
            if (target == null)
                return;

            if (clearTarget)
                target.Clear();

            if (cellSurfaces == null)
                return;

            for (int y = 0; y < cellSurfaces.Height; y++)
            {
                for (int x = 0; x < cellSurfaces.Width; x++)
                {
                    var cell = ArcGraphZLevelPolicy.CreateRuntimeCell(x, y);
                    if (!cellSurfaces.TryGetSurface(x, y, out CellSurfaceSnapshot surface))
                        continue;

                    target.Add(new ArcGraphTerrainCellSnapshot(
                        cell,
                        tileId: 0,
                        isBlocked: false,
                        surface.MacroSurface,
                        surface.SurfaceKey,
                        surface.VisualRuleKey,
                        hasAuthoritativeSurface: true));
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

                // La definizione oggetto viene letta una sola volta per evitare una
                // seconda lookup nella risoluzione sprite e per copiare in blocco i
                // metadati visuali verso lo snapshot ArcGraph.
                world.TryGetObjectDef(instance.DefId, out var def);
                ObjectVisualDef visual = def?.Visual;

                target.Add(new ArcGraphObjectVisualSnapshot(
                    pair.Key,
                    instance.DefId,
                    ArcGraphZLevelPolicy.CreateRuntimeCell(instance.CellX, instance.CellY),
                    ResolveObjectSpriteKey(instance, def),
                    instance.IsHeld,
                    instance.HolderNpcId,
                    stockUnits,
                    ResolvePositive(def?.FootprintWidth ?? 0, 1),
                    ResolvePositive(def?.FootprintHeight ?? 0, 1),
                    visual?.VisualKind ?? string.Empty,
                    visual?.ResolverKey ?? string.Empty,
                    ResolveNonNegative(visual?.WidthPixels ?? 0),
                    ResolveNonNegative(visual?.HeightPixels ?? 0),
                    ResolveNonNegative(visual?.BaseWidthPixels ?? 0),
                    ResolveNonNegative(visual?.BaseHeightPixels ?? 0),
                    visual?.BaseMiniTileMask ?? string.Empty,
                    visual?.Pivot ?? string.Empty,
                    visual?.OffsetX ?? 0,
                    visual?.OffsetY ?? 0,
                    visual?.FadeWhenActorBehind ?? false,
                    visual?.UseShadow ?? false));
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
        /// <para><b>Movimento visuale derivato dal Job Layer</b></para>
        /// <para>
        /// L'adapter non deduce interpolazione dalla differenza tra celle e non
        /// parsa stringhe diagnostiche. Se il <c>JobRuntimeState</c> espone una
        /// running action di movimento tipizzata per l'NPC, copia origine,
        /// destinazione e tick in <c>ArcGraphActorMotionSnapshot</c>. In assenza di
        /// quel contratto usa <c>ArcGraphActorMotionSnapshot.None</c>.
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
                var cell = ArcGraphZLevelPolicy.CreateRuntimeCell(position.X, position.Y);
                var motion = ResolveActorMotion(world, actorId, cell);
                bool hasHungerValue = TryResolveActorHunger(world, actorId, out float hunger01);

                target.Add(new ArcGraphActorVisualSnapshot(
                    actorId,
                    cell,
                    _defaultNpcSpriteKey,
                    motion,
                    hasHungerValue,
                    hunger01));
            }
        }

        // =============================================================================
        // TryResolveActorHunger
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia il valore fame dell'NPC dal componente Needs del World.
        /// </para>
        ///
        /// <para><b>Principio architetturale: snapshot read-only per UI</b></para>
        /// <para>
        /// L'adapter e' il punto autorizzato che legge lo stato simulativo e lo
        /// trasforma in payload visuale. La UI non riceve il dizionario
        /// <c>world.Needs</c>, ma solo un float gia' copiato e normalizzato nella
        /// pipeline ArcGraph.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>actorId</b>: NPC per cui leggere il bisogno Hunger.</item>
        ///   <item><b>hunger01</b>: valore normalizzato 0-1 restituito alla snapshot.</item>
        /// </list>
        /// </summary>
        private static bool TryResolveActorHunger(
            World world,
            int actorId,
            out float hunger01)
        {
            hunger01 = 0f;

            if (world == null || !world.Needs.TryGetValue(actorId, out NpcNeeds needs))
                return false;

            hunger01 = Clamp01(needs.GetValue(NeedKind.Hunger));
            return true;
        }

        // =============================================================================
        // ResolveActorMotion
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il motion snapshot visuale di un actor leggendo solo running
        /// action movement read-only.
        /// </para>
        ///
        /// <para><b>Contratto CPU-leggero</b></para>
        /// <para>
        /// La lookup passa dall'indice interno di <c>RunningActionStore</c>, quindi
        /// evita di allocare una lista di snapshot e non scansiona tutte le action
        /// per ogni actor. Il metodo converte solo quattro interi di cella e due
        /// contatori tick in value type ArcGraph.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>world</b>: sorgente del JobRuntimeState read-only.</item>
        ///   <item><b>actorId</b>: NPC per cui cercare un movimento attivo.</item>
        ///   <item><b>fallbackCell</b>: cella discreta usata se non esiste motion.</item>
        /// </list>
        /// </summary>
        private static ArcGraphActorMotionSnapshot ResolveActorMotion(
            World world,
            int actorId,
            ArcGraphCellCoord fallbackCell)
        {
            if (world?.JobRuntimeState == null
                || !world.JobRuntimeState.RunningActions.TryGetActiveMovementSnapshotForNpc(
                    actorId,
                    out var runningSnapshot))
            {
                return ArcGraphActorMotionSnapshot.None(fallbackCell);
            }

            var movement = runningSnapshot.Movement;
            var fromCell = ArcGraphZLevelPolicy.CreateRuntimeCell(
                movement.FromCellX,
                movement.FromCellY);
            var toCell = ArcGraphZLevelPolicy.CreateRuntimeCell(
                movement.ToCellX,
                movement.ToCellY);

            return ArcGraphActorMotionSnapshot.CreateMovement(
                fromCell,
                toCell,
                runningSnapshot.ElapsedTicks,
                runningSnapshot.RequiredTicks);
        }

        // =============================================================================
        // ResolvePositive
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza un valore intero che deve essere almeno positivo.
        /// </para>
        ///
        /// <para><b>Normalizzazione data-only</b></para>
        /// <para>
        /// I cataloghi possono essere incompleti durante la migrazione. Questa helper
        /// evita che uno zero o un valore negativo generi oggetti con ingombro nullo
        /// nella pipeline visuale.
        /// </para>
        /// </summary>
        private static int ResolvePositive(int value, int fallback)
        {
            return value > 0 ? value : fallback;
        }

        // =============================================================================
        // ResolveNonNegative
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza un valore visuale che puo' essere zero ma non negativo.
        /// </para>
        ///
        /// <para><b>Compatibilita' cataloghi incompleti</b></para>
        /// <para>
        /// Le dimensioni sprite possono mancare nei primi oggetti migrati. In quel
        /// caso <c>0</c> significa "non dichiarato"; valori negativi vengono riportati
        /// allo stesso stato neutro.
        /// </para>
        /// </summary>
        private static int ResolveNonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }

        // =============================================================================
        // Clamp01
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riporta un valore float nel range normalizzato usato dai ViewModel.
        /// </para>
        /// </summary>
        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }

        // =============================================================================
        // ResolveObjectSpriteKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la chiave sprite di un oggetto preferendo la sezione visuale
        /// ArcGraph dichiarata in <c>object_defs.json</c>.
        /// </para>
        ///
        /// <para><b>Nessun fallback implicito verso MapGrid</b></para>
        /// <para>
        /// Se la definizione oggetto contiene <c>Visual.SpritePath</c>, l'adapter
        /// usa quello. Se manca, ritorna stringa vuota. Non chiama
        /// <c>Resources.Load</c> e non inventa path legacy: la risoluzione asset
        /// resta responsabilita' del renderer e i dati incompleti devono emergere
        /// come missing sprite.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>instance</b>: istanza da cui prendere il defId.</item>
        ///   <item><b>def</b>: definizione gia' risolta dal chiamante.</item>
        /// </list>
        /// </summary>
        private static string ResolveObjectSpriteKey(WorldObjectInstance instance, ObjectDef def)
        {
            if (instance == null || string.IsNullOrWhiteSpace(instance.DefId))
                return string.Empty;

            if (def != null)
            {
                string visualSpritePath = def.ResolveArcGraphSpritePath();
                if (!string.IsNullOrWhiteSpace(visualSpritePath))
                    return visualSpritePath;
            }

            return string.Empty;
        }
    }
}
