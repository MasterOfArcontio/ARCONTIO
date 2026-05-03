using System.Collections.Generic;
using Arcontio.Core;
using UnityEngine;

namespace Arcontio.Core.Save
{
    // =============================================================================
    // WorldSaveLoader
    // =============================================================================
    /// <summary>
    /// <para>
    /// Punto di ingresso passivo per l'applicazione di uno snapshot canonico
    /// <see cref="WorldSaveData"/> a una nuova istanza di <see cref="World"/>.
    /// La classe definisce il confine sicuro del load world-level: applica
    /// solo le sezioni per cui esiste una authority esplicita nel
    /// <see cref="World"/> e rifiuta i rami ancora privi di primitive sicure.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Load deterministico senza fix-up silenziosi</b></para>
    /// <para>
    /// Uno snapshot mondo non puo' essere considerato applicato se gli oggetti
    /// vengono reinseriti con i loro ID storici ma il contatore interno del
    /// <see cref="World"/> resta inconsistente. In assenza di una API esplicita
    /// per registrare oggetti/NPC con ID gia' assegnato e ripristinare
    /// nextObjectId/nextNpcId, questo loader rifiuta intenzionalmente gli
    /// snapshot non supportati invece di produrre un runtime solo
    /// apparentemente valido. Dal checkpoint v0.10.10 i belief vengono ripristinati
    /// direttamente; dal checkpoint v0.10.11 anche object memory, landmark memory
    /// e complex edge memory vengono applicati come stato soggettivo per-NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryApplyObjectiveWorld:</b> entry point futuro per la parte oggettiva dello snapshot.</item>
    ///   <item><b>TryApplyNpcSection:</b> applica gli NPC preservando npcId e memoria narrativa legacy.</item>
    ///   <item><b>TryApplyObjectiveObjects:</b> confine esplicito della futura ricostruzione oggetti.</item>
    ///   <item><b>CanApplyObjectiveObjectsSafely:</b> preflight che documenta perche' il load oggetti non e' ancora sicuro.</item>
    /// </list>
    /// </summary>
    public static class WorldSaveLoader
    {
        // =============================================================================
        // TryApplyObjectiveWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point destinato ad applicare la parte oggettiva del salvataggio
        /// canonico a un <see cref="World"/> nuovo. Nel checkpoint v0.10.11
        /// applica NPC, oggetti, food/inventory/object-use, belief e memorie
        /// soggettive pratiche, senza integrare ancora <c>SimulationHost</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: separazione fra DTO e runtime</b></para>
        /// <para>
        /// La lettura JSON rimane responsabilita' di <see cref="WorldSaveIO"/>,
        /// mentre questo tipo descrive esclusivamente l'applicazione controllata
        /// dei dati gia' deserializzati. Nessuna integrazione con
        /// SimulationHost viene introdotta qui.
        /// </para>
        /// </summary>
        public static bool TryApplyObjectiveWorld(World world, WorldSaveData data, out string error)
        {
            // Prima validiamo TUTTE le sezioni che questo entry point tocchera'
            // o rifiutera'. In questo modo evitiamo il caso peggiore: NPC gia'
            // applicati e poi fallimento sugli oggetti o sugli store food.
            if (!CanApplyNpcSectionSafely(world, data, out error))
            {
                return false;
            }

            if (!CanApplyObjectiveObjectsSafely(world, data, out error))
            {
                return false;
            }

            if (!CanApplyFoodInventoryAndObjectUseSafely(world, data, out error))
            {
                return false;
            }

            if (!CanApplyBeliefSectionSafely(world, data, out error))
            {
                return false;
            }

            if (!CanApplySubjectiveMemorySectionsSafely(world, data, out error))
            {
                return false;
            }

            // La sezione NPC e' ora applicabile perche' World espone una API
            // save/load authority che preserva npcId e riallinea nextNpcId.
            if (!TryApplyNpcSection(world, data, out error))
            {
                return false;
            }

            if (!TryApplyObjectiveObjects(world, data, out error))
            {
                return false;
            }

            if (!TryApplyFoodInventoryAndObjectUse(world, data, out error))
            {
                return false;
            }

            if (!TryApplyBeliefSection(world, data, out error))
            {
                return false;
            }

            return TryApplySubjectiveMemorySections(world, data, out error);
        }

        // =============================================================================
        // TryApplyNpcSection
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la sezione <see cref="WorldSaveData.npcs"/> al
        /// <see cref="World"/> preservando gli <c>npcId</c> originali dello
        /// snapshot. Questo percorso e' diverso dallo scenario bootstrap
        /// legacy: non chiama <see cref="NpcSaveSystem.SpawnFromEntries"/>,
        /// non crea una mappa oldId-&gt;newId e non rigenera identita'.
        /// </para>
        ///
        /// <para><b>Principio architetturale: snapshot diverso da scenario</b></para>
        /// <para>
        /// Uno scenario puo' descrivere archetipi iniziali e lasciare al World
        /// l'assegnazione degli ID. Uno snapshot, invece, rappresenta un runtime
        /// gia' vissuto: ownership, memorie e futuri belief puntano agli stessi
        /// <c>npcId</c>. Per questo il load canonico deve preservare gli ID e
        /// fallire esplicitamente in caso di collisione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Preflight</b>: valida null, duplicati, collisioni e contratti minimi.</item>
        ///   <item><b>Registrazione NPC</b>: usa <c>World.TryRegisterLoadedNpcForSaveLoad</c>.</item>
        ///   <item><b>MemoryTrace</b>: ripristina le trace gia' presenti in <c>NpcSaveEntry</c>.</item>
        ///   <item><b>Counter</b>: ripristina <c>nextNpcId</c> dallo snapshot.</item>
        /// </list>
        /// </summary>
        public static bool TryApplyNpcSection(World world, WorldSaveData data, out string error)
        {
            // Tutto il controllo strutturale avviene prima di qualsiasi
            // mutazione. Se una entry e' corrotta, il World resta intatto.
            if (!CanApplyNpcSectionSafely(world, data, out error))
            {
                return false;
            }

            var entries = data.npcs;
            if (entries == null || entries.Length == 0)
            {
                // Snapshot senza NPC: applichiamo comunque il counter se il
                // contratto lo fornisce, cosi un mondo vuoto puo' preservare
                // una sequenza ID gia' avanzata in passato.
                if (data.nextNpcId > 0)
                {
                    return world.TryRestoreNextNpcIdForSaveLoad(data.nextNpcId, out error);
                }

                error = string.Empty;
                return true;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                // I DTO legacy hanno gia' funzioni To() mature. Qui le usiamo
                // solo come conversione dati, non come percorso SpawnFromEntries
                // che rigenererebbe gli ID.
                NpcDnaProfile dna = entry.dna.To();
                NpcProfile profile = entry.profile.ToProfile();
                NpcNeeds needs = entry.needs.ToNpcNeeds();
                Social social = entry.social.To();
                CardinalDirection facing = (CardinalDirection)entry.facingDir;

                if (!world.TryRegisterLoadedNpcForSaveLoad(
                    entry.npcId,
                    dna,
                    profile,
                    needs,
                    social,
                    entry.spawnX,
                    entry.spawnY,
                    facing,
                    out error))
                {
                    return false;
                }

                // Le memoryTraces dentro NpcSaveEntry sono il contratto legacy
                // gia' maturo. Le ripristiniamo come memoria narrativa grezza,
                // ma NON ricostruiamo BeliefStore, NpcObjectMemory o
                // NpcLandmarkMemory: quei domini hanno sezioni dedicate e
                // richiedono checkpoint separati.
                if (entry.memoryTraces != null && entry.memoryTraces.Length > 0)
                {
                    if (!world.Memory.TryGetValue(entry.npcId, out var store) || store == null)
                    {
                        error = $"WorldSaveLoader: MemoryStore mancante dopo restore NPC {entry.npcId}.";
                        return false;
                    }

                    for (int traceIndex = 0; traceIndex < entry.memoryTraces.Length; traceIndex++)
                    {
                        var traceDto = entry.memoryTraces[traceIndex];
                        if (traceDto == null)
                        {
                            error = $"WorldSaveLoader: memoryTrace nulla per npcId {entry.npcId} indice {traceIndex}.";
                            return false;
                        }

                        store.AddOrMerge(traceDto.ToTrace());
                    }
                }
            }

            // Dopo aver materializzato tutti gli NPC, imponiamo il counter
            // canonico salvato. La API World rifiuta valori che collidono con
            // gli ID appena registrati.
            return world.TryRestoreNextNpcIdForSaveLoad(data.nextNpcId, out error);
        }

        // =============================================================================
        // CanApplyNpcSectionSafely
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida la sezione NPC dello snapshot senza mutare il
        /// <see cref="World"/>. Il metodo controlla collisioni con NPC gia'
        /// presenti, duplicati interni allo snapshot e presenza dei DTO minimi
        /// necessari per un restore canonico.
        /// </para>
        ///
        /// <para><b>Principio architetturale: niente fix-up implicito</b></para>
        /// <para>
        /// A differenza del bootstrap scenario, questo preflight non inventa DNA,
        /// profile, needs o social mancanti. Se il contratto canonico e' incompleto
        /// il load fallisce con una ragione esplicita.
        /// </para>
        /// </summary>
        public static bool CanApplyNpcSectionSafely(World world, WorldSaveData data, out string error)
        {
            if (world == null)
            {
                error = "WorldSaveLoader: world nullo. Serve un World gia' creato dal bootstrap prima di applicare gli NPC.";
                return false;
            }

            if (data == null)
            {
                error = "WorldSaveLoader: WorldSaveData nullo. La lettura DTO deve avvenire prima tramite WorldSaveIO.";
                return false;
            }

            var entries = data.npcs;
            if (entries == null || entries.Length == 0)
            {
                if (data.nextNpcId < 0)
                {
                    error = "WorldSaveLoader: nextNpcId negativo in snapshot senza NPC.";
                    return false;
                }

                error = string.Empty;
                return true;
            }

            if (data.nextNpcId < 1)
            {
                error = "WorldSaveLoader: nextNpcId mancante o invalido per snapshot NPC non vuoto.";
                return false;
            }

            var seenIds = new HashSet<int>();
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    error = $"WorldSaveLoader: NpcSaveEntry nulla all'indice {i}.";
                    return false;
                }

                if (entry.npcId <= 0)
                {
                    error = $"WorldSaveLoader: npcId invalido {entry.npcId} all'indice {i}.";
                    return false;
                }

                if (!seenIds.Add(entry.npcId))
                {
                    error = $"WorldSaveLoader: npcId duplicato nello snapshot {entry.npcId}.";
                    return false;
                }

                if (world.ExistsNpc(entry.npcId))
                {
                    error = $"WorldSaveLoader: collisione con NPC gia' presente nel World: npcId {entry.npcId}.";
                    return false;
                }

                if (entry.npcId >= data.nextNpcId)
                {
                    error = $"WorldSaveLoader: nextNpcId={data.nextNpcId} non supera npcId salvato {entry.npcId}.";
                    return false;
                }

                if (entry.dna == null)
                {
                    error = $"WorldSaveLoader: DNA mancante per npcId {entry.npcId}.";
                    return false;
                }

                if (entry.profile == null)
                {
                    error = $"WorldSaveLoader: profile mancante per npcId {entry.npcId}.";
                    return false;
                }

                if (entry.needs == null)
                {
                    error = $"WorldSaveLoader: needs mancanti per npcId {entry.npcId}.";
                    return false;
                }

                if (entry.social == null)
                {
                    error = $"WorldSaveLoader: social mancante per npcId {entry.npcId}.";
                    return false;
                }

                if (entry.facingDir < (int)CardinalDirection.North || entry.facingDir > (int)CardinalDirection.West)
                {
                    error = $"WorldSaveLoader: facingDir invalido {entry.facingDir} per npcId {entry.npcId}.";
                    return false;
                }

                if (world.MapWidth > 0 && world.MapHeight > 0 && !world.InBounds(entry.spawnX, entry.spawnY))
                {
                    error = $"WorldSaveLoader: posizione NPC fuori mappa per npcId {entry.npcId} ({entry.spawnX},{entry.spawnY}).";
                    return false;
                }

                if (entry.memoryTraces != null)
                {
                    for (int traceIndex = 0; traceIndex < entry.memoryTraces.Length; traceIndex++)
                    {
                        if (entry.memoryTraces[traceIndex] == null)
                        {
                            error = $"WorldSaveLoader: memoryTrace nulla per npcId {entry.npcId} indice {traceIndex}.";
                            return false;
                        }
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        // =============================================================================
        // TryApplyObjectiveObjects
        // =============================================================================
        /// <summary>
        /// <para>
        /// Tenta di applicare la sezione oggetti dello snapshot. Al momento il
        /// metodo esegue solo validazione e rifiuta snapshot non vuoti, perche'
        /// <see cref="World"/> non espone ancora una API pubblica/autoritativa
        /// per creare oggetti con ID esplicito e riallineare il contatore
        /// nextObjectId.
        /// </para>
        ///
        /// <para><b>Principio architetturale: fallimento esplicito</b></para>
        /// <para>
        /// Un loader canonico deve fallire con una ragione leggibile quando
        /// mancano le primitive minime, invece di popolare direttamente
        /// dizionari pubblici e lasciare collisioni future al runtime.
        /// </para>
        /// </summary>
        public static bool TryApplyObjectiveObjects(World world, WorldSaveData data, out string error)
        {
            // Il preflight centralizza tutte le condizioni di sicurezza
            // conosciute. Dopo v0.10.07 World espone API dedicate per ID
            // espliciti e restore dei counter, quindi possiamo materializzare
            // gli oggetti senza passare da CreateObject.
            if (!CanApplyObjectiveObjectsSafely(world, data, out error))
            {
                return false;
            }

            var objects = data.objects;
            if (objects == null || objects.Length == 0)
            {
                if (data.nextObjectId > 0)
                {
                    return world.TryRestoreNextObjectIdForSaveLoad(data.nextObjectId, out error);
                }

                error = string.Empty;
                return true;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                var dto = objects[i];

                var instance = new WorldObjectInstance
                {
                    ObjectId = dto.objectId,
                    DefId = dto.defId ?? string.Empty,
                    CellX = dto.cellX,
                    CellY = dto.cellY,
                    OwnerKind = (OwnerKind)dto.ownerKind,
                    OwnerId = dto.ownerId,
                    OccupantNpcId = dto.occupantNpcId,
                    IsOpen = dto.isOpen,
                    IsLocked = dto.isLocked
                };

                if (!world.TryRegisterLoadedObjectForSaveLoad(instance, out error))
                {
                    return false;
                }
            }

            // Ricostruzione globale difensiva: il restore incrementale aggiorna
            // gia' le cache per oggetto, ma a fine batch vogliamo un punto unico
            // che riallinei anche LandmarkRegistry e indici derivati.
            world.RebuildDerivedCachesGlobal();

            return world.TryRestoreNextObjectIdForSaveLoad(data.nextObjectId, out error);
        }

        // =============================================================================
        // CanApplyObjectiveObjectsSafely
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la sezione oggetti puo' essere applicata senza violare
        /// l'authority interna di <see cref="World"/> sugli ID stabili.
        /// Dal checkpoint v0.10.09 il metodo accetta snapshot oggetto non vuoti,
        /// purche' non contengano duplicati, collisioni o riferimenti NPC gia'
        /// palesemente incoerenti.
        /// </para>
        ///
        /// <para><b>Principio architetturale: authority degli identificativi</b></para>
        /// <para>
        /// Gli ID sono referenziati da ownership, occupant, object use state,
        /// memoria oggetti e futuri sistemi belief. Applicare una parte dello
        /// snapshot senza poter riallineare i generatori ID produrrebbe una
        /// simulazione non deterministica e difficile da diagnosticare.
        /// </para>
        /// </summary>
        public static bool CanApplyObjectiveObjectsSafely(World world, WorldSaveData data, out string error)
        {
            // Validazione base: il loader non crea mai WorldSaveData o World
            // impliciti, per non confondere bootstrap, scenario e persistence.
            if (world == null)
            {
                error = "WorldSaveLoader: world nullo. Serve un World gia' creato dal bootstrap prima di applicare lo snapshot.";
                return false;
            }

            if (data == null)
            {
                error = "WorldSaveLoader: WorldSaveData nullo. La lettura DTO deve avvenire prima tramite WorldSaveIO.";
                return false;
            }

            var objects = data.objects;
            if (objects == null || objects.Length == 0)
            {
                if (data.nextObjectId < 0)
                {
                    error = "WorldSaveLoader: nextObjectId negativo in snapshot senza oggetti.";
                    return false;
                }

                error = string.Empty;
                return true;
            }

            if (data.nextObjectId < 1)
            {
                error = "WorldSaveLoader: nextObjectId mancante o invalido per snapshot oggetti non vuoto.";
                return false;
            }

            var seenObjectIds = new HashSet<int>();
            var seenCells = new HashSet<string>();

            for (int i = 0; i < objects.Length; i++)
            {
                var dto = objects[i];
                if (dto == null)
                {
                    error = $"WorldSaveLoader: WorldObjectSaveData nullo all'indice {i}.";
                    return false;
                }

                if (dto.objectId <= 0)
                {
                    error = $"WorldSaveLoader: objectId invalido {dto.objectId} all'indice {i}.";
                    return false;
                }

                if (!seenObjectIds.Add(dto.objectId))
                {
                    error = $"WorldSaveLoader: objectId duplicato nello snapshot {dto.objectId}.";
                    return false;
                }

                if (world.Objects.ContainsKey(dto.objectId))
                {
                    error = $"WorldSaveLoader: collisione con oggetto gia' presente nel World: objectId {dto.objectId}.";
                    return false;
                }

                if (dto.objectId >= data.nextObjectId)
                {
                    error = $"WorldSaveLoader: nextObjectId={data.nextObjectId} non supera objectId salvato {dto.objectId}.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(dto.defId))
                {
                    error = $"WorldSaveLoader: defId mancante per objectId {dto.objectId}.";
                    return false;
                }

                if (!world.TryGetObjectDef(dto.defId, out _))
                {
                    error = $"WorldSaveLoader: defId sconosciuto '{dto.defId}' per objectId {dto.objectId}.";
                    return false;
                }

                if (world.MapWidth > 0 && world.MapHeight > 0 && !world.InBounds(dto.cellX, dto.cellY))
                {
                    error = $"WorldSaveLoader: oggetto fuori mappa objectId {dto.objectId} ({dto.cellX},{dto.cellY}).";
                    return false;
                }

                if (world.HasAnyObjectAt(dto.cellX, dto.cellY))
                {
                    error = $"WorldSaveLoader: cella gia' occupata nel World prima del load oggetti ({dto.cellX},{dto.cellY}).";
                    return false;
                }

                if (!IsValidOwnerKind(dto.ownerKind))
                {
                    error = $"WorldSaveLoader: ownerKind invalido {dto.ownerKind} per objectId {dto.objectId}.";
                    return false;
                }

                string cellKey = dto.cellX + ":" + dto.cellY;
                if (!seenCells.Add(cellKey))
                {
                    error = $"WorldSaveLoader: due oggetti nello snapshot occupano la cella ({dto.cellX},{dto.cellY}).";
                    return false;
                }

                if ((OwnerKind)dto.ownerKind == OwnerKind.Npc && !WillNpcExistAfterLoad(world, data, dto.ownerId))
                {
                    error = $"WorldSaveLoader: owner NPC mancante per objectId {dto.objectId}, ownerId {dto.ownerId}.";
                    return false;
                }

                if (dto.occupantNpcId > 0 && !WillNpcExistAfterLoad(world, data, dto.occupantNpcId))
                {
                    error = $"WorldSaveLoader: occupant NPC mancante per objectId {dto.objectId}, occupantNpcId {dto.occupantNpcId}.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        // =============================================================================
        // TryApplyFoodInventoryAndObjectUse
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica le sezioni food/inventory/object-use dello snapshot canonico:
        /// <see cref="WorldSaveData.foodStocks"/>,
        /// <see cref="WorldSaveData.objectUseStates"/>,
        /// <see cref="WorldSaveData.npcPrivateFood"/>,
        /// <see cref="WorldSaveData.npcLastPrivateFoodConsumeTicks"/> e
        /// <see cref="WorldSaveData.npcPinnedFoodStockBeliefs"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: stato esistente, nessun inventario nuovo</b></para>
        /// <para>
        /// Questo metodo non introduce un sistema inventory generico e non cambia
        /// le regole gameplay. Ripristina soltanto component store gia' presenti
        /// nel <see cref="World"/> e fallisce se i riferimenti a NPC/oggetti non
        /// sono coerenti.
        /// </para>
        /// </summary>
        public static bool TryApplyFoodInventoryAndObjectUse(World world, WorldSaveData data, out string error)
        {
            if (!CanApplyFoodInventoryAndObjectUseSafely(world, data, out error))
            {
                return false;
            }

            // Sezione authoritative: puliamo gli store coperti dallo snapshot.
            // Non usiamo SetFoodStock perche' quel metodo genera/aggiorna pinned
            // belief implicitamente; qui vogliamo ripristinare esattamente anche
            // la sezione belief pinned salvata, senza inventare conoscenza.
            world.FoodStocks.Clear();
            world.ObjectUse.Clear();
            world.NpcPrivateFood.Clear();
            world.NpcLastPrivateFoodConsumeTick.Clear();
            world.NpcPinnedFoodStockBeliefs.Clear();

            var foodStocks = data.foodStocks;
            if (foodStocks != null)
            {
                for (int i = 0; i < foodStocks.Length; i++)
                {
                    var dto = foodStocks[i];
                    world.FoodStocks[dto.objectId] = new FoodStockComponent
                    {
                        Units = dto.units,
                        OwnerKind = (OwnerKind)dto.ownerKind,
                        OwnerId = dto.ownerId
                    };
                }
            }

            var useStates = data.objectUseStates;
            if (useStates != null)
            {
                for (int i = 0; i < useStates.Length; i++)
                {
                    var dto = useStates[i];
                    world.ObjectUse[dto.objectId] = new ObjectUseState
                    {
                        IsInUse = dto.isInUse,
                        UsingNpcId = dto.usingNpcId
                    };
                }
            }

            var privateFood = data.npcPrivateFood;
            if (privateFood != null)
            {
                for (int i = 0; i < privateFood.Length; i++)
                {
                    var dto = privateFood[i];
                    world.NpcPrivateFood[dto.npcId] = dto.units;
                }
            }

            var consumeTicks = data.npcLastPrivateFoodConsumeTicks;
            if (consumeTicks != null)
            {
                for (int i = 0; i < consumeTicks.Length; i++)
                {
                    var dto = consumeTicks[i];
                    world.NpcLastPrivateFoodConsumeTick[dto.npcId] = dto.lastConsumeTick;
                }
            }

            var pinned = data.npcPinnedFoodStockBeliefs;
            if (pinned != null)
            {
                for (int i = 0; i < pinned.Length; i++)
                {
                    var dto = pinned[i];
                    world.EnsurePinnedFoodStockBelief(dto.npcId, dto.objectId, dto.lastKnownX, dto.lastKnownY);
                }
            }

            error = string.Empty;
            return true;
        }

        // =============================================================================
        // CanApplyFoodInventoryAndObjectUseSafely
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida tutte le sezioni food/inventory/object-use senza mutare il
        /// <see cref="World"/>. I riferimenti sono controllati contro lo stato gia'
        /// presente e contro le sezioni NPC/oggetti contenute nello stesso snapshot.
        /// </para>
        ///
        /// <para><b>Principio architetturale: riferimenti ID espliciti</b></para>
        /// <para>
        /// FoodStocks e ObjectUse non possiedono identita' autonome: dipendono da
        /// <c>objectId</c> e talvolta da <c>npcId</c>. Il loader non crea oggetti,
        /// NPC o ownership mancanti per correggere questi riferimenti.
        /// </para>
        /// </summary>
        public static bool CanApplyFoodInventoryAndObjectUseSafely(World world, WorldSaveData data, out string error)
        {
            if (world == null)
            {
                error = "WorldSaveLoader: world nullo. Serve un World prima di applicare food/inventory/object-use.";
                return false;
            }

            if (data == null)
            {
                error = "WorldSaveLoader: WorldSaveData nullo prima di applicare food/inventory/object-use.";
                return false;
            }

            var seenFoodStocks = new HashSet<int>();
            if (data.foodStocks != null)
            {
                for (int i = 0; i < data.foodStocks.Length; i++)
                {
                    var dto = data.foodStocks[i];
                    if (dto == null)
                    {
                        error = $"WorldSaveLoader: FoodStockSaveData nullo all'indice {i}.";
                        return false;
                    }

                    if (dto.objectId <= 0 || !WillObjectExistAfterLoad(world, data, dto.objectId))
                    {
                        error = $"WorldSaveLoader: foodStock riferisce objectId inesistente {dto.objectId}.";
                        return false;
                    }

                    if (!seenFoodStocks.Add(dto.objectId))
                    {
                        error = $"WorldSaveLoader: foodStock duplicato per objectId {dto.objectId}.";
                        return false;
                    }

                    if (dto.units < 0)
                    {
                        error = $"WorldSaveLoader: foodStock con units negative per objectId {dto.objectId}.";
                        return false;
                    }

                    if (!IsValidOwnerKind(dto.ownerKind))
                    {
                        error = $"WorldSaveLoader: foodStock objectId {dto.objectId} ha ownerKind invalido {dto.ownerKind}.";
                        return false;
                    }

                    if ((OwnerKind)dto.ownerKind == OwnerKind.Npc && !WillNpcExistAfterLoad(world, data, dto.ownerId))
                    {
                        error = $"WorldSaveLoader: foodStock objectId {dto.objectId} riferisce owner NPC mancante {dto.ownerId}.";
                        return false;
                    }
                }
            }

            var seenUseStates = new HashSet<int>();
            if (data.objectUseStates != null)
            {
                for (int i = 0; i < data.objectUseStates.Length; i++)
                {
                    var dto = data.objectUseStates[i];
                    if (dto == null)
                    {
                        error = $"WorldSaveLoader: ObjectUseStateSaveData nullo all'indice {i}.";
                        return false;
                    }

                    if (dto.objectId <= 0 || !WillObjectExistAfterLoad(world, data, dto.objectId))
                    {
                        error = $"WorldSaveLoader: objectUse riferisce objectId inesistente {dto.objectId}.";
                        return false;
                    }

                    if (!seenUseStates.Add(dto.objectId))
                    {
                        error = $"WorldSaveLoader: objectUse duplicato per objectId {dto.objectId}.";
                        return false;
                    }

                    if (dto.isInUse && !WillNpcExistAfterLoad(world, data, dto.usingNpcId))
                    {
                        error = $"WorldSaveLoader: objectUse objectId {dto.objectId} riferisce usingNpcId mancante {dto.usingNpcId}.";
                        return false;
                    }
                }
            }

            var seenPrivateFood = new HashSet<int>();
            if (data.npcPrivateFood != null)
            {
                for (int i = 0; i < data.npcPrivateFood.Length; i++)
                {
                    var dto = data.npcPrivateFood[i];
                    if (dto == null)
                    {
                        error = $"WorldSaveLoader: NpcPrivateFoodSaveData nullo all'indice {i}.";
                        return false;
                    }

                    if (dto.npcId <= 0 || !WillNpcExistAfterLoad(world, data, dto.npcId))
                    {
                        error = $"WorldSaveLoader: npcPrivateFood riferisce npcId inesistente {dto.npcId}.";
                        return false;
                    }

                    if (!seenPrivateFood.Add(dto.npcId))
                    {
                        error = $"WorldSaveLoader: npcPrivateFood duplicato per npcId {dto.npcId}.";
                        return false;
                    }

                    if (dto.units < 0)
                    {
                        error = $"WorldSaveLoader: npcPrivateFood negativo per npcId {dto.npcId}.";
                        return false;
                    }
                }
            }

            var seenConsumeTicks = new HashSet<int>();
            if (data.npcLastPrivateFoodConsumeTicks != null)
            {
                for (int i = 0; i < data.npcLastPrivateFoodConsumeTicks.Length; i++)
                {
                    var dto = data.npcLastPrivateFoodConsumeTicks[i];
                    if (dto == null)
                    {
                        error = $"WorldSaveLoader: NpcPrivateFoodConsumeTickSaveData nullo all'indice {i}.";
                        return false;
                    }

                    if (dto.npcId <= 0 || !WillNpcExistAfterLoad(world, data, dto.npcId))
                    {
                        error = $"WorldSaveLoader: consume tick riferisce npcId inesistente {dto.npcId}.";
                        return false;
                    }

                    if (!seenConsumeTicks.Add(dto.npcId))
                    {
                        error = $"WorldSaveLoader: consume tick duplicato per npcId {dto.npcId}.";
                        return false;
                    }
                }
            }

            var seenPinned = new HashSet<string>();
            if (data.npcPinnedFoodStockBeliefs != null)
            {
                for (int i = 0; i < data.npcPinnedFoodStockBeliefs.Length; i++)
                {
                    var dto = data.npcPinnedFoodStockBeliefs[i];
                    if (dto == null)
                    {
                        error = $"WorldSaveLoader: NpcPinnedFoodStockBeliefSaveData nullo all'indice {i}.";
                        return false;
                    }

                    if (dto.npcId <= 0 || !WillNpcExistAfterLoad(world, data, dto.npcId))
                    {
                        error = $"WorldSaveLoader: pinned food belief riferisce npcId inesistente {dto.npcId}.";
                        return false;
                    }

                    if (dto.objectId <= 0 || !WillObjectExistAfterLoad(world, data, dto.objectId))
                    {
                        error = $"WorldSaveLoader: pinned food belief riferisce objectId inesistente {dto.objectId}.";
                        return false;
                    }

                    string key = dto.npcId + ":" + dto.objectId;
                    if (!seenPinned.Add(key))
                    {
                        error = $"WorldSaveLoader: pinned food belief duplicata per npcId/objectId {key}.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        // =============================================================================
        // TryApplyBeliefSection
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la sezione <see cref="WorldSaveData.beliefs"/> ripristinando
        /// direttamente i <see cref="BeliefStore"/> per-NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: no rebuild da World oggettivo</b></para>
        /// <para>
        /// Il load non consulta oggetti, stock o registry globali per generare
        /// conoscenza. Le credenze vengono caricate solo se gia' presenti nello
        /// snapshot, cioe' se erano gia' stato soggettivo del NPC al momento del save.
        /// </para>
        /// </summary>
        public static bool TryApplyBeliefSection(World world, WorldSaveData data, out string error)
        {
            if (!CanApplyBeliefSectionSafely(world, data, out error))
            {
                return false;
            }

            var stores = data.beliefs;
            if (stores == null || stores.Length == 0)
            {
                error = string.Empty;
                return true;
            }

            for (int i = 0; i < stores.Length; i++)
            {
                var dto = stores[i];

                if (!world.Beliefs.TryGetValue(dto.npcId, out var store) || store == null)
                {
                    store = new BeliefStore(dto.maxEntries);
                    world.Beliefs[dto.npcId] = store;
                }

                var entries = new BeliefEntry[dto.entries != null ? dto.entries.Length : 0];
                for (int j = 0; j < entries.Length; j++)
                    entries[j] = ToBeliefEntry(dto.entries[j]);

                if (!store.TryReplaceAllForSaveLoad(entries, dto.maxEntries, dto.nextBeliefId, out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        // =============================================================================
        // CanApplyBeliefSectionSafely
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida la sezione belief dello snapshot senza mutare il World. I controlli
        /// sono locali allo store soggettivo: NPC esistente, enum validi, range 0-1,
        /// ID locali univoci e contatore coerente.
        /// </para>
        ///
        /// <para><b>Principio architetturale: persistenza cognitiva senza telepatia</b></para>
        /// <para>
        /// Una belief puo' essere caricata solo per un NPC che esiste nel runtime
        /// post-load. Il preflight non verifica che la belief corrisponda al mondo
        /// oggettivo corrente, perche' una credenza puo' essere vecchia, falsa o
        /// contraddetta: verificarla contro il World introdurrebbe onniscienza.
        /// </para>
        /// </summary>
        public static bool CanApplyBeliefSectionSafely(World world, WorldSaveData data, out string error)
        {
            if (world == null)
            {
                error = "WorldSaveLoader: world nullo prima di applicare beliefs.";
                return false;
            }

            if (data == null)
            {
                error = "WorldSaveLoader: WorldSaveData nullo prima di applicare beliefs.";
                return false;
            }

            if (data.beliefs == null || data.beliefs.Length == 0)
            {
                error = string.Empty;
                return true;
            }

            var seenNpcIds = new HashSet<int>();
            for (int i = 0; i < data.beliefs.Length; i++)
            {
                var storeDto = data.beliefs[i];
                if (storeDto == null)
                {
                    error = $"WorldSaveLoader: NpcBeliefStoreSaveData nullo all'indice {i}.";
                    return false;
                }

                if (storeDto.npcId <= 0 || !WillNpcExistAfterLoad(world, data, storeDto.npcId))
                {
                    error = $"WorldSaveLoader: belief store riferisce npcId inesistente {storeDto.npcId}.";
                    return false;
                }

                if (!seenNpcIds.Add(storeDto.npcId))
                {
                    error = $"WorldSaveLoader: belief store duplicato per npcId {storeDto.npcId}.";
                    return false;
                }

                if (storeDto.maxEntries <= 0)
                {
                    error = $"WorldSaveLoader: maxEntries belief invalido per npcId {storeDto.npcId}.";
                    return false;
                }

                var entries = storeDto.entries ?? System.Array.Empty<BeliefEntrySaveData>();
                if (entries.Length > storeDto.maxEntries)
                {
                    error = $"WorldSaveLoader: belief entries supera maxEntries per npcId {storeDto.npcId}.";
                    return false;
                }

                int maxBeliefId = 0;
                var seenBeliefIds = new HashSet<int>();
                for (int j = 0; j < entries.Length; j++)
                {
                    var entry = entries[j];
                    if (entry == null)
                    {
                        error = $"WorldSaveLoader: BeliefEntrySaveData nullo per npcId {storeDto.npcId}, indice {j}.";
                        return false;
                    }

                    if (entry.beliefId <= 0)
                    {
                        error = $"WorldSaveLoader: beliefId invalido {entry.beliefId} per npcId {storeDto.npcId}.";
                        return false;
                    }

                    if (!seenBeliefIds.Add(entry.beliefId))
                    {
                        error = $"WorldSaveLoader: beliefId duplicato {entry.beliefId} per npcId {storeDto.npcId}.";
                        return false;
                    }

                    if (!IsValidBeliefCategory(entry.category))
                    {
                        error = $"WorldSaveLoader: belief category invalida {entry.category} per npcId {storeDto.npcId}.";
                        return false;
                    }

                    if (!IsValidBeliefSource(entry.source))
                    {
                        error = $"WorldSaveLoader: belief source invalida {entry.source} per npcId {storeDto.npcId}.";
                        return false;
                    }

                    if (!IsValidBeliefStatus(entry.status))
                    {
                        error = $"WorldSaveLoader: belief status invalido {entry.status} per npcId {storeDto.npcId}.";
                        return false;
                    }

                    if (entry.confidence < 0f || entry.confidence > 1f || entry.freshness < 0f || entry.freshness > 1f)
                    {
                        error = $"WorldSaveLoader: confidence/freshness fuori range per beliefId {entry.beliefId}.";
                        return false;
                    }

                    if (entry.sourceCount < 0)
                    {
                        error = $"WorldSaveLoader: sourceCount negativo per beliefId {entry.beliefId}.";
                        return false;
                    }

                    if (entry.beliefId > maxBeliefId)
                        maxBeliefId = entry.beliefId;
                }

                if (storeDto.nextBeliefId <= maxBeliefId)
                {
                    error = $"WorldSaveLoader: nextBeliefId={storeDto.nextBeliefId} non supera max belief id {maxBeliefId} per npcId {storeDto.npcId}.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        // =============================================================================
        // TryApplySubjectiveMemorySections
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica le memorie soggettive pratiche dello snapshot canonico:
        /// object memory, landmark memory e complex edge memory.
        /// </para>
        ///
        /// <para><b>Principio architetturale: restore della conoscenza per-NPC</b></para>
        /// <para>
        /// Questo metodo non ricostruisce conoscenza dal World oggettivo. Ogni sezione
        /// viene caricata solo per l'NPC proprietario indicato dal DTO e sostituisce lo
        /// store corrispondente, preservando il confine tra fatti del mondo e memoria
        /// soggettiva dell'agente.
        /// </para>
        /// </summary>
        public static bool TryApplySubjectiveMemorySections(World world, WorldSaveData data, out string error)
        {
            if (!CanApplySubjectiveMemorySectionsSafely(world, data, out error))
            {
                return false;
            }

            if (data.npcObjectMemory != null)
            {
                for (int i = 0; i < data.npcObjectMemory.Length; i++)
                {
                    var dto = data.npcObjectMemory[i];
                    var store = new NpcObjectMemoryStore(dto.capacity);
                    var entries = new NpcObjectMemoryStore.Entry[dto.entries != null ? dto.entries.Length : 0];

                    for (int j = 0; j < entries.Length; j++)
                        entries[j] = ToObjectMemoryEntry(dto.entries[j]);

                    if (!store.TryReplaceAllForSaveLoad(entries, out error))
                        return false;

                    world.NpcObjectMemory[dto.npcId] = store;
                }
            }

            if (data.npcLandmarkMemory != null)
            {
                for (int i = 0; i < data.npcLandmarkMemory.Length; i++)
                {
                    var dto = data.npcLandmarkMemory[i];
                    var store = new NpcLandmarkMemory(dto.maxLandmarks, dto.maxEdges);

                    var landmarks = new NpcLandmarkMemory.SaveLoadLandmarkEntry[dto.knownLandmarks != null ? dto.knownLandmarks.Length : 0];
                    for (int j = 0; j < landmarks.Length; j++)
                    {
                        var node = dto.knownLandmarks[j];
                        landmarks[j] = new NpcLandmarkMemory.SaveLoadLandmarkEntry(node.nodeId, node.lastSeenTick, node.confidence01);
                    }

                    var edges = new NpcLandmarkMemory.SaveLoadEdgeEntry[dto.knownEdges != null ? dto.knownEdges.Length : 0];
                    for (int j = 0; j < edges.Length; j++)
                    {
                        var edge = dto.knownEdges[j];
                        edges[j] = new NpcLandmarkMemory.SaveLoadEdgeEntry(edge.nodeA, edge.nodeB, edge.cost, edge.lastSeenTick, edge.confidence01);
                    }

                    if (!store.TryReplaceAllForSaveLoad(landmarks, edges, dto.lastVisitedLandmarkId, dto.lastVisitedLandmarkTick, out error))
                        return false;

                    world.NpcLandmarkMemory[dto.npcId] = store;
                }
            }

            if (data.npcComplexEdgeMemory != null)
            {
                for (int i = 0; i < data.npcComplexEdgeMemory.Length; i++)
                {
                    var dto = data.npcComplexEdgeMemory[i];
                    var store = new NpcComplexEdgeMemory(dto.maxEdges, dto.maxStepsPerRecording);
                    var edges = new ComplexEdge[dto.edges != null ? dto.edges.Length : 0];

                    for (int j = 0; j < edges.Length; j++)
                        edges[j] = ToComplexEdge(dto.edges[j]);

                    if (!store.TryReplaceAllForSaveLoad(
                        edges,
                        dto.lastMaintenanceTick,
                        dto.maxStepsPerRecording,
                        dto.staleTicksBeforeEviction,
                        dto.minConfidenceToKeep,
                        dto.confidenceDecayPerMaintenance,
                        dto.maintenancePeriodTicks,
                        out error))
                    {
                        return false;
                    }

                    world.NpcComplexEdgeMemories[dto.npcId] = store;
                }
            }

            error = string.Empty;
            return true;
        }

        // =============================================================================
        // CanApplySubjectiveMemorySectionsSafely
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida object memory, landmark memory e complex edge memory senza mutare
        /// il <see cref="World"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: coerenza interna, non verifica onnisciente</b></para>
        /// <para>
        /// Il preflight controlla proprietario NPC, duplicati, range e riferimenti
        /// interni tra memorie landmark. Non verifica che oggetti o landmark esistano
        /// ancora nel mondo oggettivo: una memoria puo' essere obsoleta o falsa.
        /// </para>
        /// </summary>
        public static bool CanApplySubjectiveMemorySectionsSafely(World world, WorldSaveData data, out string error)
        {
            if (world == null)
            {
                error = "WorldSaveLoader: world nullo prima di applicare memorie soggettive.";
                return false;
            }

            if (data == null)
            {
                error = "WorldSaveLoader: WorldSaveData nullo prima di applicare memorie soggettive.";
                return false;
            }

            if (!CanApplyObjectMemorySafely(world, data, out error))
                return false;

            if (!CanApplyLandmarkMemorySafely(world, data, out error))
                return false;

            return CanApplyComplexEdgeMemorySafely(world, data, out error);
        }

        private static bool CanApplyObjectMemorySafely(World world, WorldSaveData data, out string error)
        {
            var stores = data.npcObjectMemory;
            if (stores == null || stores.Length == 0)
            {
                error = string.Empty;
                return true;
            }

            var seenNpcIds = new HashSet<int>();
            for (int i = 0; i < stores.Length; i++)
            {
                var store = stores[i];
                if (store == null)
                {
                    error = $"WorldSaveLoader: NpcObjectMemorySaveData nullo all'indice {i}.";
                    return false;
                }

                if (store.npcId <= 0 || !WillNpcExistAfterLoad(world, data, store.npcId))
                {
                    error = $"WorldSaveLoader: object memory riferisce npcId inesistente {store.npcId}.";
                    return false;
                }

                if (!seenNpcIds.Add(store.npcId))
                {
                    error = $"WorldSaveLoader: object memory duplicata per npcId {store.npcId}.";
                    return false;
                }

                if (store.capacity <= 0)
                {
                    error = $"WorldSaveLoader: object memory capacity invalida per npcId {store.npcId}.";
                    return false;
                }

                var entries = store.entries ?? System.Array.Empty<NpcObjectMemoryEntrySaveData>();
                if (entries.Length > store.capacity)
                {
                    error = $"WorldSaveLoader: object memory entries supera capacity per npcId {store.npcId}.";
                    return false;
                }

                for (int j = 0; j < entries.Length; j++)
                {
                    var entry = entries[j];
                    if (entry == null)
                    {
                        error = $"WorldSaveLoader: object memory entry nulla per npcId {store.npcId}, indice {j}.";
                        return false;
                    }

                    if (!entry.isValid)
                    {
                        error = $"WorldSaveLoader: object memory contiene entry non valida serializzata per npcId {store.npcId}, indice {j}.";
                        return false;
                    }

                    if (!IsValidObjectMemorySubjectKind(entry.subjectKind))
                    {
                        error = $"WorldSaveLoader: object memory subjectKind invalido {entry.subjectKind} per npcId {store.npcId}.";
                        return false;
                    }

                    if (entry.subjectId < 0 || entry.objectId < 0 || entry.reliability01 < 0f || entry.reliability01 > 1f || entry.utilityScore01 < 0f || entry.utilityScore01 > 1f)
                    {
                        error = $"WorldSaveLoader: object memory entry fuori range per npcId {store.npcId}, indice {j}.";
                        return false;
                    }

                    if (!IsValidOwnerKind(entry.ownerKind))
                    {
                        error = $"WorldSaveLoader: object memory ownerKind invalido {entry.ownerKind} per npcId {store.npcId}.";
                        return false;
                    }

                    if (entry.carriedFoodUnitsApprox < 0)
                    {
                        error = $"WorldSaveLoader: object memory carriedFoodUnitsApprox negativo per npcId {store.npcId}.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool CanApplyLandmarkMemorySafely(World world, WorldSaveData data, out string error)
        {
            var stores = data.npcLandmarkMemory;
            if (stores == null || stores.Length == 0)
            {
                error = string.Empty;
                return true;
            }

            var seenNpcIds = new HashSet<int>();
            for (int i = 0; i < stores.Length; i++)
            {
                var store = stores[i];
                if (store == null)
                {
                    error = $"WorldSaveLoader: NpcLandmarkMemorySaveData nullo all'indice {i}.";
                    return false;
                }

                if (store.npcId <= 0 || !WillNpcExistAfterLoad(world, data, store.npcId))
                {
                    error = $"WorldSaveLoader: landmark memory riferisce npcId inesistente {store.npcId}.";
                    return false;
                }

                if (!seenNpcIds.Add(store.npcId))
                {
                    error = $"WorldSaveLoader: landmark memory duplicata per npcId {store.npcId}.";
                    return false;
                }

                if (store.maxLandmarks <= 0 || store.maxEdges <= 0)
                {
                    error = $"WorldSaveLoader: landmark memory cap invalidi per npcId {store.npcId}.";
                    return false;
                }

                var nodes = store.knownLandmarks ?? System.Array.Empty<LandmarkNodeMemorySaveData>();
                var edges = store.knownEdges ?? System.Array.Empty<LandmarkEdgeMemorySaveData>();
                if (nodes.Length > store.maxLandmarks || edges.Length > store.maxEdges)
                {
                    error = $"WorldSaveLoader: landmark memory oltre cap per npcId {store.npcId}.";
                    return false;
                }

                var knownNodeIds = new HashSet<int>();
                for (int j = 0; j < nodes.Length; j++)
                {
                    var node = nodes[j];
                    if (node == null || node.nodeId <= 0 || node.confidence01 < 0f || node.confidence01 > 1f)
                    {
                        error = $"WorldSaveLoader: landmark node invalido per npcId {store.npcId}, indice {j}.";
                        return false;
                    }

                    if (!knownNodeIds.Add(node.nodeId))
                    {
                        error = $"WorldSaveLoader: landmark node duplicato {node.nodeId} per npcId {store.npcId}.";
                        return false;
                    }
                }

                var seenEdges = new HashSet<string>();
                for (int j = 0; j < edges.Length; j++)
                {
                    var edge = edges[j];
                    if (edge == null || edge.nodeA <= 0 || edge.nodeB <= 0 || edge.nodeA == edge.nodeB || edge.cost < 1 || edge.confidence01 < 0f || edge.confidence01 > 1f)
                    {
                        error = $"WorldSaveLoader: landmark edge invalido per npcId {store.npcId}, indice {j}.";
                        return false;
                    }

                    if (!knownNodeIds.Contains(edge.nodeA) || !knownNodeIds.Contains(edge.nodeB))
                    {
                        error = $"WorldSaveLoader: landmark edge {edge.nodeA}-{edge.nodeB} riferisce endpoint non conosciuto per npcId {store.npcId}.";
                        return false;
                    }

                    string key = MakeEdgeKey(edge.nodeA, edge.nodeB);
                    if (!seenEdges.Add(key))
                    {
                        error = $"WorldSaveLoader: landmark edge duplicato {key} per npcId {store.npcId}.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool CanApplyComplexEdgeMemorySafely(World world, WorldSaveData data, out string error)
        {
            var stores = data.npcComplexEdgeMemory;
            if (stores == null || stores.Length == 0)
            {
                error = string.Empty;
                return true;
            }

            var seenNpcIds = new HashSet<int>();
            for (int i = 0; i < stores.Length; i++)
            {
                var store = stores[i];
                if (store == null)
                {
                    error = $"WorldSaveLoader: NpcComplexEdgeMemorySaveData nullo all'indice {i}.";
                    return false;
                }

                if (store.npcId <= 0 || !WillNpcExistAfterLoad(world, data, store.npcId))
                {
                    error = $"WorldSaveLoader: complex edge memory riferisce npcId inesistente {store.npcId}.";
                    return false;
                }

                if (!seenNpcIds.Add(store.npcId))
                {
                    error = $"WorldSaveLoader: complex edge memory duplicata per npcId {store.npcId}.";
                    return false;
                }

                if (store.maxEdges <= 0 || store.maxStepsPerRecording <= 0 || store.staleTicksBeforeEviction <= 0 || store.maintenancePeriodTicks <= 0)
                {
                    error = $"WorldSaveLoader: complex edge memory parametri invalidi per npcId {store.npcId}.";
                    return false;
                }

                var edges = store.edges ?? System.Array.Empty<ComplexEdgeSaveData>();
                if (edges.Length > store.maxEdges)
                {
                    error = $"WorldSaveLoader: complex edge count supera maxEdges per npcId {store.npcId}.";
                    return false;
                }

                var knownNodeIds = CollectKnownLandmarkIds(data, store.npcId);
                var seenEdges = new HashSet<string>();
                for (int j = 0; j < edges.Length; j++)
                {
                    var edge = edges[j];
                    if (edge == null || edge.nodeA <= 0 || edge.nodeB <= 0 || edge.nodeA == edge.nodeB || edge.baseCost < 1 || edge.confidence01 < 0f || edge.confidence01 > 1f)
                    {
                        error = $"WorldSaveLoader: complex edge invalido per npcId {store.npcId}, indice {j}.";
                        return false;
                    }

                    if (knownNodeIds.Count > 0 && (!knownNodeIds.Contains(edge.nodeA) || !knownNodeIds.Contains(edge.nodeB)))
                    {
                        error = $"WorldSaveLoader: complex edge {edge.nodeA}-{edge.nodeB} riferisce endpoint non conosciuto per npcId {store.npcId}.";
                        return false;
                    }

                    string key = MakeEdgeKey(edge.nodeA, edge.nodeB);
                    if (!seenEdges.Add(key))
                    {
                        error = $"WorldSaveLoader: complex edge duplicato {key} per npcId {store.npcId}.";
                        return false;
                    }

                    var segments = edge.segments ?? System.Array.Empty<PathSegmentSaveData>();
                    int segmentCost = 0;
                    for (int s = 0; s < segments.Length; s++)
                    {
                        var segment = segments[s];
                        if (segment == null || !IsValidCardinalDirection(segment.direction) || segment.length < 1)
                        {
                            error = $"WorldSaveLoader: complex edge segment invalido per npcId {store.npcId}, edge {key}, indice {s}.";
                            return false;
                        }

                        segmentCost += segment.length;
                    }

                    if (segments.Length > 0 && segmentCost != edge.baseCost)
                    {
                        error = $"WorldSaveLoader: complex edge {key} ha baseCost={edge.baseCost} diverso dalla somma segmenti={segmentCost}.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool WillNpcExistAfterLoad(World world, WorldSaveData data, int npcId)
        {
            if (npcId <= 0)
                return false;

            if (world.ExistsNpc(npcId))
                return true;

            if (data?.npcs == null)
                return false;

            for (int i = 0; i < data.npcs.Length; i++)
            {
                if (data.npcs[i] != null && data.npcs[i].npcId == npcId)
                    return true;
            }

            return false;
        }

        private static bool IsValidOwnerKind(int ownerKind)
        {
            return ownerKind >= (int)OwnerKind.None && ownerKind <= (int)OwnerKind.Community;
        }

        private static bool IsValidObjectMemorySubjectKind(int subjectKind)
        {
            return subjectKind >= (int)NpcObjectMemoryStore.SubjectKind.WorldObject
                && subjectKind <= (int)NpcObjectMemoryStore.SubjectKind.Npc;
        }

        private static bool IsValidCardinalDirection(int direction)
        {
            return direction >= (int)CardinalDirection.North && direction <= (int)CardinalDirection.West;
        }

        private static bool IsValidBeliefCategory(int category)
        {
            return category >= (int)BeliefCategory.Food && category <= (int)BeliefCategory.Structure;
        }

        private static bool IsValidBeliefSource(int source)
        {
            return source >= (int)BeliefSource.Seen && source <= (int)BeliefSource.Inferred;
        }

        private static bool IsValidBeliefStatus(int status)
        {
            return status >= (int)BeliefStatus.Active && status <= (int)BeliefStatus.Discarded;
        }

        private static BeliefEntry ToBeliefEntry(BeliefEntrySaveData dto)
        {
            return new BeliefEntry
            {
                BeliefId = dto.beliefId,
                Category = (BeliefCategory)dto.category,
                EstimatedPosition = new Vector2Int(dto.estimatedX, dto.estimatedY),
                Confidence = dto.confidence,
                Freshness = dto.freshness,
                LastUpdatedTick = dto.lastUpdatedTick,
                SourceCount = dto.sourceCount,
                Source = (BeliefSource)dto.source,
                Status = (BeliefStatus)dto.status
            };
        }

        private static NpcObjectMemoryStore.Entry ToObjectMemoryEntry(NpcObjectMemoryEntrySaveData dto)
        {
            return new NpcObjectMemoryStore.Entry
            {
                IsValid = dto.isValid,
                Kind = (NpcObjectMemoryStore.SubjectKind)dto.subjectKind,
                SubjectId = dto.subjectId,
                DefId = dto.defId ?? string.Empty,
                ObjectId = dto.objectId,
                CellX = dto.lastKnownX,
                CellY = dto.lastKnownY,
                OwnerKind = (OwnerKind)dto.ownerKind,
                OwnerId = dto.ownerId,
                LastSeenTick = dto.lastSeenTick,
                Reliability01 = dto.reliability01,
                UtilityScore01 = dto.utilityScore01,
                IsPinned = dto.isPinned,
                Flags = (NpcObjectMemoryStore.ObservedFlags)dto.observedFlags,
                CarriedFoodUnitsApprox = dto.carriedFoodUnitsApprox
            };
        }

        private static ComplexEdge ToComplexEdge(ComplexEdgeSaveData dto)
        {
            var segments = new List<PathSegment>(dto.segments != null ? dto.segments.Length : 0);
            if (dto.segments != null)
            {
                for (int i = 0; i < dto.segments.Length; i++)
                {
                    var segment = dto.segments[i];
                    segments.Add(new PathSegment((CardinalDirection)segment.direction, segment.length));
                }
            }

            var edge = new ComplexEdge(new NpcLandmarkMemory.EdgeKey(dto.nodeA, dto.nodeB), segments, dto.lastSeenTick)
            {
                BaseCost = dto.baseCost,
                Confidence = dto.confidence01,
                LastSeenTick = dto.lastSeenTick,
                Flags = (ComplexEdgeFlags)dto.flags
            };

            return edge;
        }

        private static HashSet<int> CollectKnownLandmarkIds(WorldSaveData data, int npcId)
        {
            var result = new HashSet<int>();

            if (data?.npcLandmarkMemory == null)
                return result;

            for (int i = 0; i < data.npcLandmarkMemory.Length; i++)
            {
                var store = data.npcLandmarkMemory[i];
                if (store == null || store.npcId != npcId || store.knownLandmarks == null)
                    continue;

                for (int j = 0; j < store.knownLandmarks.Length; j++)
                {
                    var node = store.knownLandmarks[j];
                    if (node != null && node.nodeId > 0)
                        result.Add(node.nodeId);
                }

                break;
            }

            return result;
        }

        private static string MakeEdgeKey(int nodeA, int nodeB)
        {
            int a = nodeA < nodeB ? nodeA : nodeB;
            int b = nodeA < nodeB ? nodeB : nodeA;
            return a + ":" + b;
        }

        private static bool WillObjectExistAfterLoad(World world, WorldSaveData data, int objectId)
        {
            if (objectId <= 0)
                return false;

            if (world.Objects.ContainsKey(objectId))
                return true;

            if (data?.objects == null)
                return false;

            for (int i = 0; i < data.objects.Length; i++)
            {
                if (data.objects[i] != null && data.objects[i].objectId == objectId)
                    return true;
            }

            return false;
        }
    }
}
