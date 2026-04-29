using System;
using System.Collections.Generic;
using Arcontio.Core;
using UnityEngine;

namespace Arcontio.Core.Save
{
    // =============================================================================
    // WorldSaveBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Utility passiva per estrarre uno <see cref="WorldSaveData"/> dal
    /// <see cref="World"/> corrente senza modificare il runtime, senza scrivere su
    /// disco e senza attivare alcun percorso di load.
    /// </para>
    ///
        /// <para><b>Principio architetturale: snapshot oggettivo minimo con cognizione persistita esplicita</b></para>
    /// <para>
    /// Questo builder appartiene alla migrazione progressiva v0.10 e non sostituisce
    /// <see cref="NpcSaveSystem"/>. Dal checkpoint v0.10.05, la sezione NPC dello
    /// snapshot canonico riusa il contratto legacy gia' maturo per includere anche
    /// le memory traces narrative oggi coperte dai chunk NPC. Dal checkpoint
    /// v0.10.10 i BeliefStore vengono persistiti direttamente; dal checkpoint
    /// v0.10.11 anche object memory, landmark memory e complex edge memory vengono
    /// copiate dagli store soggettivi gia' presenti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildFromWorld</b>: entry point pubblico per costruire la radice snapshot.</item>
    ///   <item><b>BuildNpcEntries</b>: migra nel WorldSaveData la copertura NPC legacy gia' esistente.</item>
    ///   <item><b>BuildObjectEntries</b>: estrae istanze oggetto, ownership e stato runtime locale.</item>
        ///   <item><b>BuildFoodStockEntries</b>: estrae stock cibo oggettivi accessibili dal World.</item>
        ///   <item><b>BuildObjectUseStateEntries</b>: estrae stati d'uso runtime accessibili dal World.</item>
        ///   <item><b>BuildNpcPrivateFoodEntries</b>: estrae il possesso fisico MVP di cibo per NPC.</item>
        ///   <item><b>BuildNpcPrivateFoodConsumeTickEntries</b>: estrae i marker ultimo consumo cibo privato.</item>
        ///   <item><b>BuildNpcPinnedFoodStockBeliefEntries</b>: estrae le belief pinned sugli stock privati.</item>
        ///   <item><b>BuildBeliefStoreEntries</b>: estrae credenze soggettive gia' aggregate.</item>
    /// </list>
    /// </summary>
    public static class WorldSaveBuilder
    {
        // =============================================================================
        // BuildFromWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno snapshot canonico in memoria a partire dal <see cref="World"/>
        /// corrente. Il metodo non ha side effect: non chiama writer, non tocca file,
        /// non invoca bootstrap e non modifica gli store del mondo.
        /// </para>
        ///
        /// <para><b>Principio architetturale: estrazione senza scorciatoie fragili</b></para>
        /// <para>
        /// I campi vengono popolati solo quando il dato e' gia' raggiungibile tramite
        /// API o store pubblici. In particolare, i contatori privati del World non
        /// vengono ricostruiti con max(id)+1, perche' quello produrrebbe un valore
        /// plausibile ma non necessariamente identico allo stato runtime reale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Header</b>: schema, tick, dimensioni mondo e riferimenti opzionali.</item>
        ///   <item><b>Sezioni oggettive</b>: NPC oggettivi, oggetti, stock, uso e cibo privato.</item>
        ///   <item><b>Sezioni soggettive</b>: belief e memorie pratiche/landmark copiate dagli store per-NPC.</item>
        /// </list>
        /// </summary>
        public static WorldSaveData BuildFromWorld(
            World world,
            long savedAtTick,
            string configRef = "",
            string scenarioRef = "")
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            // Questo DTO e' solo una fotografia in RAM. Ogni array viene creato qui
            // per evitare null ambigui quando JsonUtility serializzera' la radice in
            // un checkpoint successivo.
            var data = new WorldSaveData
            {
                schemaVersion = WorldSaveData.CurrentSchemaVersion,
                savedAtTick = savedAtTick,
                worldWidth = world.MapWidth,
                worldHeight = world.MapHeight,

                // v0.10.03:
                // I contatori restano authority privata del World, ma sono ora
                // leggibili tramite getter read-only. Il builder salva quindi il
                // valore reale, non una stima ricostruita dai massimi id presenti.
                nextNpcId = world.NextNpcId,
                nextObjectId = world.NextObjectId,

                simulationConfigResourcePath = configRef ?? string.Empty,

                // La firma pubblica del checkpoint riceve solo configRef e
                // scenarioRef. Il path object_defs resta quindi non popolato finche'
                // il chiamante o il World non esporranno una provenance canonica.
                objectDefsResourcePath = string.Empty,
                scenarioResourceName = scenarioRef ?? string.Empty,

                npcs = BuildNpcEntries(world),
                objects = BuildObjectEntries(world),
                foodStocks = BuildFoodStockEntries(world),
                objectUseStates = BuildObjectUseStateEntries(world),
                npcPrivateFood = BuildNpcPrivateFoodEntries(world),
                npcLastPrivateFoodConsumeTicks = BuildNpcPrivateFoodConsumeTickEntries(world),
                npcPinnedFoodStockBeliefs = BuildNpcPinnedFoodStockBeliefEntries(world),

                // Belief persistence v0.10.10:
                // Le credenze aggregate sono stato soggettivo gia' vissuto, quindi
                // vengono salvate direttamente. Non facciamo rebuild da memory
                // durante il load, perche' quello cambierebbe potenzialmente id,
                // freshness, source count e status.
                memory = Array.Empty<NpcMemorySaveData>(),
                beliefs = BuildBeliefStoreEntries(world),

                // Subjective memory persistence v0.10.11:
                // Queste sezioni vengono copiate dagli store per-NPC gia'
                // esistenti. Non risolviamo informazioni mancanti dal World
                // oggettivo e non insegniamo agli NPC landmark/oggetti nuovi.
                npcObjectMemory = BuildNpcObjectMemoryEntries(world),
                npcLandmarkMemory = BuildNpcLandmarkMemoryEntries(world),
                npcComplexEdgeMemory = BuildNpcComplexEdgeMemoryEntries(world)
            };

            return data;
        }

        // =============================================================================
        // BuildNpcEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Migra nello snapshot canonico la copertura NPC gia' implementata da
        /// <see cref="NpcSaveSystem.BuildEntriesFromWorld"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: migrazione senza fork del contratto NPC</b></para>
        /// <para>
        /// <see cref="NpcSaveEntry"/> e i DTO in <c>NpcSaveData.cs</c> sono gia'
        /// il contratto maturo per DNA, profile, needs, social, posizione, facing e
        /// memory traces narrative. Il WorldSaveData li incorpora senza cambiare il
        /// formato legacy dei chunk e senza cancellare <see cref="NpcSaveSystem"/>.
        /// Questo evita una duplicazione fragile della stessa serializzazione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Legacy bridge</b>: delega a <see cref="NpcSaveSystem.BuildEntriesFromWorld"/>.</item>
        ///   <item><b>Incluso</b>: DNA, profile, needs, social, grid, facing e memoryTraces.</item>
        ///   <item><b>Escluso</b>: belief, object memory e landmark memory restano sezioni world-level separate.</item>
        /// </list>
        /// </summary>
        private static NpcSaveEntry[] BuildNpcEntries(World world)
        {
            // v0.10.05:
            // Questa e' una migrazione del formato NPC legacy DENTRO la radice
            // canonica WorldSaveData, non una rimozione del vecchio sistema. Il
            // writer chunk resta disponibile e il formato npcs_chunk_N.json non
            // viene modificato in questo checkpoint.
            return NpcSaveSystem.BuildEntriesFromWorld(world).ToArray();
        }

        // =============================================================================
        // BuildObjectEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae le istanze oggettive di <see cref="WorldObjectInstance"/> dal
        /// registry pubblico <see cref="World.Objects"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: identita' oggetto stabile</b></para>
        /// <para>
        /// Questo snapshot conserva <c>objectId</c>, <c>defId</c>, cella, ownership e
        /// stato runtime locale. Non tenta di serializzare cache derivate come
        /// occlusion map o indici cella-oggetto: quelle appartengono al rebuild.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Identita'</b>: objectId e defId.</item>
        ///   <item><b>Posizione</b>: cellX/cellY.</item>
        ///   <item><b>Ownership/state</b>: ownerKind, ownerId, occupant, isOpen, isLocked.</item>
        /// </list>
        /// </summary>
        private static WorldObjectSaveData[] BuildObjectEntries(World world)
        {
            var objectIds = new List<int>(world.Objects.Keys);
            objectIds.Sort();

            var result = new List<WorldObjectSaveData>(objectIds.Count);

            for (int i = 0; i < objectIds.Count; i++)
            {
                int objectId = objectIds[i];

                if (!world.Objects.TryGetValue(objectId, out var obj) || obj == null)
                    continue;

                // Usiamo la chiave del registry come objectId canonico, perche'
                // FoodStocks, ObjectUse e futuri riferimenti incrociati puntano a
                // quella chiave. Il campo obj.ObjectId dovrebbe coincidere, ma qui
                // preferiamo la fonte relazionale dello store.
                result.Add(new WorldObjectSaveData
                {
                    objectId = objectId,
                    defId = obj.DefId ?? string.Empty,
                    cellX = obj.CellX,
                    cellY = obj.CellY,
                    ownerKind = (int)obj.OwnerKind,
                    ownerId = obj.OwnerId,
                    occupantNpcId = obj.OccupantNpcId,
                    isOpen = obj.IsOpen,
                    isLocked = obj.IsLocked
                });
            }

            return result.ToArray();
        }

        // =============================================================================
        // BuildFoodStockEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae gli stock di cibo oggettivi dal component store
        /// <see cref="World.FoodStocks"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: componente persistito separatamente</b></para>
        /// <para>
        /// Lo stock e' un componente associato a un objectId, non un campo
        /// dell'istanza oggetto. Per questo viene salvato in una sezione autonoma,
        /// preservando unita' e ownership del componente senza duplicare l'oggetto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>objectId</b>: chiave del componente.</item>
        ///   <item><b>units</b>: quantita' oggettiva corrente.</item>
        ///   <item><b>ownerKind/ownerId</b>: proprieta' logica dello stock.</item>
        /// </list>
        /// </summary>
        private static FoodStockSaveData[] BuildFoodStockEntries(World world)
        {
            var objectIds = new List<int>(world.FoodStocks.Keys);
            objectIds.Sort();

            var result = new FoodStockSaveData[objectIds.Count];

            for (int i = 0; i < objectIds.Count; i++)
            {
                int objectId = objectIds[i];
                var stock = world.FoodStocks[objectId];

                result[i] = new FoodStockSaveData
                {
                    objectId = objectId,
                    units = stock.Units,
                    ownerKind = (int)stock.OwnerKind,
                    ownerId = stock.OwnerId
                };
            }

            return result;
        }

        // =============================================================================
        // BuildObjectUseStateEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae gli stati d'uso runtime dal component store
        /// <see cref="World.ObjectUse"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: stato operativo esplicito</b></para>
        /// <para>
        /// Il builder salva solo gli stati presenti nello store. Non chiama
        /// <see cref="World.GetUseStateOrDefault"/> per ogni oggetto, perche' quello
        /// produrrebbe record "liberi" derivati e non realmente presenti nello
        /// snapshot runtime.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>objectId</b>: oggetto interagibile.</item>
        ///   <item><b>isInUse</b>: stato di occupazione corrente.</item>
        ///   <item><b>usingNpcId</b>: NPC utilizzatore, oppure 0 se libero.</item>
        /// </list>
        /// </summary>
        private static ObjectUseStateSaveData[] BuildObjectUseStateEntries(World world)
        {
            var objectIds = new List<int>(world.ObjectUse.Keys);
            objectIds.Sort();

            var result = new ObjectUseStateSaveData[objectIds.Count];

            for (int i = 0; i < objectIds.Count; i++)
            {
                int objectId = objectIds[i];
                var state = world.ObjectUse[objectId];

                result[i] = new ObjectUseStateSaveData
                {
                    objectId = objectId,
                    isInUse = state.IsInUse,
                    usingNpcId = state.UsingNpcId
                };
            }

            return result;
        }

        // =============================================================================
        // BuildNpcPrivateFoodEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae il cibo privato trasportato dagli NPC dal component store
        /// <see cref="World.NpcPrivateFood"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: inventario MVP come fatto oggettivo</b></para>
        /// <para>
        /// Il cibo trasportato non e' una credenza, ma possesso fisico corrente. Il
        /// builder lo include nello snapshot oggettivo minimo per evitare che sparisca
        /// tra save e load futuri.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>npcId</b>: NPC che trasporta il cibo.</item>
        ///   <item><b>units</b>: unita' trasportate nello store runtime.</item>
        /// </list>
        /// </summary>
        private static NpcPrivateFoodSaveData[] BuildNpcPrivateFoodEntries(World world)
        {
            var npcIds = new List<int>(world.NpcPrivateFood.Keys);
            npcIds.Sort();

            var result = new NpcPrivateFoodSaveData[npcIds.Count];

            for (int i = 0; i < npcIds.Count; i++)
            {
                int npcId = npcIds[i];

                result[i] = new NpcPrivateFoodSaveData
                {
                    npcId = npcId,
                    units = world.NpcPrivateFood[npcId]
                };
            }

            return result;
        }

        // =============================================================================
        // BuildNpcPrivateFoodConsumeTickEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae i marker <see cref="World.NpcLastPrivateFoodConsumeTick"/> che
        /// distinguono consumo volontario e mancanza sospetta di cibo privato.
        /// </para>
        ///
        /// <para><b>Principio architetturale: marker runtime non ricostruibile</b></para>
        /// <para>
        /// Questo tick non si deduce in modo affidabile dalla quantità corrente di
        /// <see cref="World.NpcPrivateFood"/>. Se perso durante il save/load, i sistemi
        /// needs/theft possono interpretare diversamente il primo tick successivo al
        /// restore.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>npcId</b>: NPC proprietario del marker.</item>
        ///   <item><b>lastConsumeTick</b>: valore runtime esatto nello store.</item>
        /// </list>
        /// </summary>
        private static NpcPrivateFoodConsumeTickSaveData[] BuildNpcPrivateFoodConsumeTickEntries(World world)
        {
            var npcIds = new List<int>(world.NpcLastPrivateFoodConsumeTick.Keys);
            npcIds.Sort();

            var result = new NpcPrivateFoodConsumeTickSaveData[npcIds.Count];

            for (int i = 0; i < npcIds.Count; i++)
            {
                int npcId = npcIds[i];

                result[i] = new NpcPrivateFoodConsumeTickSaveData
                {
                    npcId = npcId,
                    lastConsumeTick = world.NpcLastPrivateFoodConsumeTick[npcId]
                };
            }

            return result;
        }

        // =============================================================================
        // BuildNpcPinnedFoodStockBeliefEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae le belief pinned sugli stock privati da
        /// <see cref="World.NpcPinnedFoodStockBeliefs"/>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: conoscenza soggettiva dichiarata</b></para>
        /// <para>
        /// Queste entry non sono FoodStocks oggettivi: rappresentano dove un NPC crede
        /// di avere lasciato cibo privato. Lo snapshot le conserva solo perche' la
        /// struttura e' stabile e primitiva; non vengono confuse con inventory o
        /// ownership reale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>npcId</b>: proprietario soggettivo della belief.</item>
        ///   <item><b>objectId</b>: stock ricordato.</item>
        ///   <item><b>lastKnownX/Y</b>: cella ricordata dall'NPC.</item>
        /// </list>
        /// </summary>
        private static NpcPinnedFoodStockBeliefSaveData[] BuildNpcPinnedFoodStockBeliefEntries(World world)
        {
            var npcIds = new List<int>(world.NpcPinnedFoodStockBeliefs.Keys);
            npcIds.Sort();

            var result = new List<NpcPinnedFoodStockBeliefSaveData>();

            for (int i = 0; i < npcIds.Count; i++)
            {
                int npcId = npcIds[i];

                if (!world.NpcPinnedFoodStockBeliefs.TryGetValue(npcId, out var beliefs) || beliefs == null)
                    continue;

                for (int j = 0; j < beliefs.Count; j++)
                {
                    var belief = beliefs[j];

                    result.Add(new NpcPinnedFoodStockBeliefSaveData
                    {
                        npcId = npcId,
                        objectId = belief.ObjectId,
                        lastKnownX = belief.LastKnownX,
                        lastKnownY = belief.LastKnownY
                    });
                }
            }

            return result.ToArray();
        }

        // =============================================================================
        // BuildBeliefStoreEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae i <see cref="BeliefStore"/> per-NPC gia' aggregati nello snapshot
        /// canonico.
        /// </para>
        ///
        /// <para><b>Principio architetturale: persistenza diretta anti-omniscience</b></para>
        /// <para>
        /// Le belief vengono copiate dallo store soggettivo del singolo NPC. Il builder
        /// non consulta <see cref="World.Objects"/>, food stock reali o registry globali
        /// per dedurre credenze mancanti: salva solo cio' che il runtime cognitivo ha
        /// gia' prodotto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>npcId</b>: proprietario soggettivo dello store.</item>
        ///   <item><b>maxEntries/nextBeliefId</b>: limiti e contatore locale dello store.</item>
        ///   <item><b>entries</b>: copia primitiva delle BeliefEntry presenti.</item>
        /// </list>
        /// </summary>
        private static NpcBeliefStoreSaveData[] BuildBeliefStoreEntries(World world)
        {
            var npcIds = new List<int>(world.Beliefs.Keys);
            npcIds.Sort();

            var result = new List<NpcBeliefStoreSaveData>(npcIds.Count);

            for (int i = 0; i < npcIds.Count; i++)
            {
                int npcId = npcIds[i];

                if (!world.Beliefs.TryGetValue(npcId, out var store) || store == null)
                    continue;

                var entries = store.Entries;
                var dtoEntries = new BeliefEntrySaveData[entries.Count];

                for (int j = 0; j < entries.Count; j++)
                    dtoEntries[j] = FromBeliefEntry(entries[j]);

                result.Add(new NpcBeliefStoreSaveData
                {
                    npcId = npcId,
                    maxEntries = store.MaxEntries,
                    nextBeliefId = store.NextBeliefId,
                    entries = dtoEntries
                });
            }

            return result.ToArray();
        }

        private static BeliefEntrySaveData FromBeliefEntry(in BeliefEntry entry)
        {
            Vector2Int position = entry.EstimatedPosition;

            return new BeliefEntrySaveData
            {
                beliefId = entry.BeliefId,
                category = (int)entry.Category,
                estimatedX = position.x,
                estimatedY = position.y,
                confidence = entry.Confidence,
                freshness = entry.Freshness,
                lastUpdatedTick = entry.LastUpdatedTick,
                sourceCount = entry.SourceCount,
                source = (int)entry.Source,
                status = (int)entry.Status
            };
        }

        // =============================================================================
        // BuildNpcObjectMemoryEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae la memoria pratica soggettiva di oggetti/NPC osservati.
        /// </para>
        ///
        /// <para><b>Principio architetturale: memoria pratica non ricostruita</b></para>
        /// <para>
        /// Il builder legge solo gli slot gia' presenti in <see cref="World.NpcObjectMemory"/>.
        /// Non scandisce <see cref="World.Objects"/> e non aggiunge riferimenti mancanti:
        /// uno slot esiste nello snapshot solo se lo store soggettivo dell'NPC lo
        /// conteneva gia'.
        /// </para>
        /// </summary>
        private static NpcObjectMemorySaveData[] BuildNpcObjectMemoryEntries(World world)
        {
            var npcIds = new List<int>(world.NpcObjectMemory.Keys);
            npcIds.Sort();

            var result = new List<NpcObjectMemorySaveData>(npcIds.Count);

            for (int i = 0; i < npcIds.Count; i++)
            {
                int npcId = npcIds[i];
                if (!world.NpcObjectMemory.TryGetValue(npcId, out var store) || store == null)
                    continue;

                var entries = new List<NpcObjectMemoryEntrySaveData>();
                for (int slotIndex = 0; slotIndex < store.Slots.Length; slotIndex++)
                {
                    var entry = store.Slots[slotIndex];
                    if (!entry.IsValid)
                        continue;

                    entries.Add(FromObjectMemoryEntry(entry));
                }

                result.Add(new NpcObjectMemorySaveData
                {
                    npcId = npcId,
                    capacity = store.Capacity,
                    entries = entries.ToArray()
                });
            }

            return result.ToArray();
        }

        private static NpcObjectMemoryEntrySaveData FromObjectMemoryEntry(in NpcObjectMemoryStore.Entry entry)
        {
            return new NpcObjectMemoryEntrySaveData
            {
                isValid = entry.IsValid,
                subjectKind = (int)entry.Kind,
                subjectId = entry.SubjectId,
                defId = entry.DefId ?? string.Empty,
                objectId = entry.ObjectId,
                lastKnownX = entry.CellX,
                lastKnownY = entry.CellY,
                ownerKind = (int)entry.OwnerKind,
                ownerId = entry.OwnerId,
                lastSeenTick = entry.LastSeenTick,
                reliability01 = entry.Reliability01,
                utilityScore01 = entry.UtilityScore01,
                isPinned = entry.IsPinned,
                observedFlags = (int)entry.Flags,
                carriedFoodUnitsApprox = entry.CarriedFoodUnitsApprox
            };
        }

        // =============================================================================
        // BuildNpcLandmarkMemoryEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae la memoria landmark soggettiva semplice degli NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: niente completamento dal registry</b></para>
        /// <para>
        /// <see cref="NpcLandmarkMemory"/> salva nodeId, edge, cost, confidence e
        /// recency. Lo store non conserva coordinate/kind del nodo; quindi il builder
        /// non li inventa consultando il registry oggettivo. I campi DTO relativi a
        /// coordinate/kind restano sentinelle finche' il dominio non esporra' quei
        /// dati come conoscenza soggettiva esplicita.
        /// </para>
        /// </summary>
        private static NpcLandmarkMemorySaveData[] BuildNpcLandmarkMemoryEntries(World world)
        {
            var npcIds = new List<int>(world.NpcLandmarkMemory.Keys);
            npcIds.Sort();

            var result = new List<NpcLandmarkMemorySaveData>(npcIds.Count);
            var landmarks = new List<NpcLandmarkMemory.SaveLoadLandmarkEntry>();
            var edges = new List<NpcLandmarkMemory.SaveLoadEdgeEntry>();

            for (int i = 0; i < npcIds.Count; i++)
            {
                int npcId = npcIds[i];
                if (!world.NpcLandmarkMemory.TryGetValue(npcId, out var store) || store == null)
                    continue;

                store.FillSaveLoadEntries(landmarks, edges);
                landmarks.Sort((a, b) => a.NodeId.CompareTo(b.NodeId));
                edges.Sort((a, b) =>
                {
                    int byA = a.NodeA.CompareTo(b.NodeA);
                    return byA != 0 ? byA : a.NodeB.CompareTo(b.NodeB);
                });

                var nodeDtos = new LandmarkNodeMemorySaveData[landmarks.Count];
                for (int j = 0; j < landmarks.Count; j++)
                    nodeDtos[j] = FromLandmarkEntry(landmarks[j]);

                var edgeDtos = new LandmarkEdgeMemorySaveData[edges.Count];
                for (int j = 0; j < edges.Count; j++)
                    edgeDtos[j] = FromLandmarkEdgeEntry(edges[j]);

                result.Add(new NpcLandmarkMemorySaveData
                {
                    npcId = npcId,
                    maxLandmarks = store.MaxLandmarksForSaveLoad,
                    maxEdges = store.MaxEdgesForSaveLoad,
                    lastVisitedLandmarkId = store.LastVisitedLandmarkId,
                    lastVisitedLandmarkTick = store.LastVisitedLandmarkTick,
                    knownLandmarks = nodeDtos,
                    knownEdges = edgeDtos
                });
            }

            return result.ToArray();
        }

        private static LandmarkNodeMemorySaveData FromLandmarkEntry(in NpcLandmarkMemory.SaveLoadLandmarkEntry entry)
        {
            return new LandmarkNodeMemorySaveData
            {
                nodeId = entry.NodeId,

                // v0.10.11:
                // NpcLandmarkMemory non conserva kind/coordinate come memoria
                // soggettiva autonoma. Lasciamo sentinelle invece di risolverle
                // dal LandmarkRegistry oggettivo.
                kind = 0,
                cellX = -1,
                cellY = -1,
                lastSeenTick = entry.LastSeenTick,
                confidence01 = entry.Confidence01
            };
        }

        private static LandmarkEdgeMemorySaveData FromLandmarkEdgeEntry(in NpcLandmarkMemory.SaveLoadEdgeEntry entry)
        {
            return new LandmarkEdgeMemorySaveData
            {
                nodeA = entry.NodeA,
                nodeB = entry.NodeB,
                cost = entry.CostCells,
                lastSeenTick = entry.LastSeenTick,
                confidence01 = entry.Confidence01
            };
        }

        // =============================================================================
        // BuildNpcComplexEdgeMemoryEntries
        // =============================================================================
        /// <summary>
        /// <para>
        /// Estrae gli edge complessi appresi dagli NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: pathfinding soggettivo persistito</b></para>
        /// <para>
        /// Gli edge complessi sono esperienza navigazionale dell'NPC. Il builder copia
        /// solo gli edge gia' presenti nello store, inclusi costi, confidence, flags e
        /// segmenti. Non genera nuove connessioni dal registry o dalla mappa.
        /// </para>
        /// </summary>
        private static NpcComplexEdgeMemorySaveData[] BuildNpcComplexEdgeMemoryEntries(World world)
        {
            var npcIds = new List<int>(world.NpcComplexEdgeMemories.Keys);
            npcIds.Sort();

            var result = new List<NpcComplexEdgeMemorySaveData>(npcIds.Count);

            for (int i = 0; i < npcIds.Count; i++)
            {
                int npcId = npcIds[i];
                if (!world.NpcComplexEdgeMemories.TryGetValue(npcId, out var store) || store == null)
                    continue;

                var edgeDtos = new List<ComplexEdgeSaveData>(store.Edges.Count);
                foreach (var kv in store.Edges)
                    edgeDtos.Add(FromComplexEdge(kv.Value));

                edgeDtos.Sort((a, b) =>
                {
                    int byA = a.nodeA.CompareTo(b.nodeA);
                    return byA != 0 ? byA : a.nodeB.CompareTo(b.nodeB);
                });

                result.Add(new NpcComplexEdgeMemorySaveData
                {
                    npcId = npcId,
                    maxEdges = store.MaxEdgesForSaveLoad,
                    maxStepsPerRecording = store.MaxStepsPerRecording,
                    staleTicksBeforeEviction = store.StaleTicksBeforeEviction,
                    minConfidenceToKeep = store.MinConfidenceToKeep,
                    confidenceDecayPerMaintenance = store.ConfidenceDecayPerMaintenance,
                    maintenancePeriodTicks = store.MaintenancePeriodTicks,
                    lastMaintenanceTick = store.LastMaintenanceTickForSaveLoad,
                    edges = edgeDtos.ToArray()
                });
            }

            return result.ToArray();
        }

        private static ComplexEdgeSaveData FromComplexEdge(ComplexEdge edge)
        {
            var segmentDtos = new PathSegmentSaveData[edge.Segments != null ? edge.Segments.Count : 0];
            for (int i = 0; i < segmentDtos.Length; i++)
            {
                var segment = edge.Segments[i];
                segmentDtos[i] = new PathSegmentSaveData
                {
                    direction = (int)segment.Direction,
                    length = segment.Length
                };
            }

            return new ComplexEdgeSaveData
            {
                nodeA = edge.Key.A,
                nodeB = edge.Key.B,
                baseCost = edge.BaseCost,
                confidence01 = edge.Confidence,
                lastSeenTick = edge.LastSeenTick,
                flags = (int)edge.Flags,
                segments = segmentDtos
            };
        }
    }
}
