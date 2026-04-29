using System;

namespace Arcontio.Core.Save
{
    // =============================================================================
    // WorldSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto serializzabile canonico per uno snapshot completo del mondo
    /// ARCONTIO. Questo DTO nasce come radice stabile del formato save/load v0.10:
    /// non sostituisce ancora <c>NpcSaveSystem</c>, non modifica il bootstrap di
    /// <c>SimulationHost</c> e non cambia il formato debug di <c>DevMapIO</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: snapshot canonico del World</b></para>
    /// <para>
    /// L'audit persistence v0.10.00 ha mostrato che oggi NPC, mappe debug, tick,
    /// oggetti, ownership e memoria soggettiva sono salvati in percorsi separati o
    /// non sono salvati affatto. Questa radice non implementa ancora l'estrazione
    /// completa dei dati: stabilisce il contratto comune in cui i checkpoint
    /// successivi potranno innestare writer, reader e fix-up deterministici.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Header</b>: versione schema, tick salvato, dimensioni mondo e prossimi id runtime.</item>
    ///   <item><b>Riferimenti</b>: path Resources opzionali per config/scenario usati dal bootstrap corrente.</item>
    ///   <item><b>Sezioni oggettive</b>: oggetti, stock cibo, stati uso e inventario privato NPC.</item>
    ///   <item><b>Sezioni soggettive</b>: memoria, belief, object memory, landmark memory e complex edge memory.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class WorldSaveData
    {
        /// <summary>
        /// Versione corrente del contratto WorldSaveData.
        /// Campo non serializzato direttamente da JsonUtility perche' const, ma usato
        /// dai futuri writer come valore canonico di <see cref="schemaVersion"/>.
        /// </summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        /// Versione dello schema JSON. Serve a distinguere snapshot futuri quando il
        /// formato verra' esteso senza confondere salvataggi vecchi con save corrotti.
        /// </summary>
        public int schemaVersion = CurrentSchemaVersion;

        /// <summary>
        /// Tick globale della simulazione al momento del salvataggio.
        /// Dal checkpoint v0.10.13 viene interpretato come "prossimo tick da
        /// eseguire" e ripristinato nel solo path save/load di
        /// <c>SimulationHost</c>, riallineando anche <c>TickContext</c> e
        /// <c>World.Global.CurrentTickIndex</c>. Non e' quindi un contatore di
        /// tick gia' completato a cui sommare uno durante il load.
        /// </summary>
        public long savedAtTick;

        /// <summary>
        /// Dimensione orizzontale del mondo salvato. Il load futuro dovra' usarla
        /// per inizializzare la griglia prima di ricostruire oggetti e cache derivate.
        /// </summary>
        public int worldWidth;

        /// <summary>
        /// Dimensione verticale del mondo salvato. Deve restare accoppiata a
        /// <see cref="worldWidth"/> per evitare snapshot parziali della griglia.
        /// </summary>
        public int worldHeight;

        /// <summary>
        /// Prossimo id NPC disponibile nel <c>World</c> al momento dello snapshot.
        /// L'audit ha classificato la mancata persistenza dei contatori id come gap
        /// critico perche' rompe riferimenti incrociati dopo load.
        /// </summary>
        public int nextNpcId;

        /// <summary>
        /// Prossimo id oggetto disponibile nel <c>World</c> al momento dello snapshot.
        /// Il valore serve a preservare identita' stabili di oggetti, ownership,
        /// memorie soggettive e riferimenti a food stock.
        /// </summary>
        public int nextObjectId;

        /// <summary>
        /// Path Resources della configurazione simulativa usata dal runtime corrente.
        /// Valore atteso oggi: "Arcontio/Config/game_params". Resta opzionale per non
        /// vincolare vecchi snapshot o test minimali.
        /// </summary>
        public string simulationConfigResourcePath;

        /// <summary>
        /// Path Resources delle definizioni oggetto usate dal runtime corrente.
        /// Valore atteso oggi: "Arcontio/Config/object_defs". Futuri loader potranno
        /// usarlo per verificare compatibilita' tra snapshot e database oggetti.
        /// </summary>
        public string objectDefsResourcePath;

        /// <summary>
        /// Nome scenario Resources eventualmente usato per creare il mondo originale.
        /// Non e' un comando di reload: e' solo provenance, utile per debug e audit.
        /// </summary>
        public string scenarioResourceName;

        /// <summary>
        /// Sezione NPC. In v0.10.01 riusa intenzionalmente <see cref="NpcSaveEntry"/>
        /// per non duplicare il contratto gia' maturo di DNA, profile, needs, social,
        /// posizione, facing e memory traces legacy.
        /// </summary>
        public NpcSaveEntry[] npcs = Array.Empty<NpcSaveEntry>();

        /// <summary>
        /// Sezione oggetti oggettivi del mondo. Deve diventare la sorgente canonica
        /// per sostituire il solo formato debug DevMap quando il load sara' integrato.
        /// </summary>
        public WorldObjectSaveData[] objects = Array.Empty<WorldObjectSaveData>();

        /// <summary>
        /// Sezione stock di cibo associati a oggetti del mondo.
        /// Separata da <see cref="objects"/> per rispecchiare i component store del
        /// <c>World</c>: l'oggetto esiste come istanza, lo stock e' un componente.
        /// </summary>
        public FoodStockSaveData[] foodStocks = Array.Empty<FoodStockSaveData>();

        /// <summary>
        /// Sezione stati d'uso degli oggetti interagibili, per esempio letti occupati.
        /// Resta distinta dagli oggetti per evitare di confondere identita' fisica e
        /// stato operativo temporaneo.
        /// </summary>
        public ObjectUseStateSaveData[] objectUseStates = Array.Empty<ObjectUseStateSaveData>();

        /// <summary>
        /// Sezione inventario privato MVP degli NPC. Oggi il World rappresenta il
        /// trasporto cibo con <c>NpcPrivateFood</c>; questa sezione lo rende esplicito.
        /// </summary>
        public NpcPrivateFoodSaveData[] npcPrivateFood = Array.Empty<NpcPrivateFoodSaveData>();

        /// <summary>
        /// Sezione marker runtime dell'ultimo consumo di cibo privato per NPC.
        /// Questo dato non e' un inventario separato: serve ai sistemi needs/theft
        /// per distinguere "ho consumato io" da "mi manca cibo" dopo un reload.
        /// </summary>
        public NpcPrivateFoodConsumeTickSaveData[] npcLastPrivateFoodConsumeTicks = Array.Empty<NpcPrivateFoodConsumeTickSaveData>();

        /// <summary>
        /// Sezione belief pinned sugli stock privati di cibo. E' conoscenza soggettiva
        /// per-NPC, non verita' oggettiva del mondo: viene mantenuta separata dagli
        /// stock reali per non confondere possessione fisica e memoria/proprietà percepita.
        /// </summary>
        public NpcPinnedFoodStockBeliefSaveData[] npcPinnedFoodStockBeliefs = Array.Empty<NpcPinnedFoodStockBeliefSaveData>();

        /// <summary>
        /// Sezione memoria narrativa per-NPC. Rimane separata da <see cref="npcs"/>
        /// anche se <see cref="NpcSaveEntry"/> contiene gia' <c>memoryTraces</c>,
        /// per preparare una migrazione pulita verso snapshot world-level.
        /// </summary>
        public NpcMemorySaveData[] memory = Array.Empty<NpcMemorySaveData>();

        /// <summary>
        /// Sezione belief per-NPC. Dal checkpoint v0.10.10 viene popolata e caricata
        /// direttamente come stato soggettivo gia' aggregato, senza rebuild speculativo
        /// da MemoryTrace e senza consultare lo stato oggettivo del World.
        /// </summary>
        public NpcBeliefStoreSaveData[] beliefs = Array.Empty<NpcBeliefStoreSaveData>();

        /// <summary>
        /// Sezione memoria pratica oggetti/NPC osservati. Dal checkpoint v0.10.11
        /// viene popolata copiando gli slot soggettivi degli NPC, senza derivare
        /// nuove conoscenze da <c>World.Objects</c>.
        /// </summary>
        public NpcObjectMemorySaveData[] npcObjectMemory = Array.Empty<NpcObjectMemorySaveData>();

        /// <summary>
        /// Sezione memoria soggettiva landmark. Il registry oggettivo dei landmark e'
        /// ricostruibile, ma la conoscenza per-NPC dei landmark non lo e'. Dal
        /// checkpoint v0.10.11 vengono persistiti nodeId, edge, confidence e recency;
        /// coordinate/kind restano opzionali quando non sono parte dello store.
        /// </summary>
        public NpcLandmarkMemorySaveData[] npcLandmarkMemory = Array.Empty<NpcLandmarkMemorySaveData>();

        /// <summary>
        /// Sezione edge complessi appresi dagli NPC. Dal checkpoint v0.10.11 conserva
        /// costi, segmenti, confidence, flags e parametri di maintenance dello store,
        /// ma non conserva il recording transitorio eventualmente attivo.
        /// </summary>
        public NpcComplexEdgeMemorySaveData[] npcComplexEdgeMemory = Array.Empty<NpcComplexEdgeMemorySaveData>();
    }

    // =============================================================================
    // WorldObjectSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per una singola istanza oggettiva del mondo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: identita' oggetto stabile</b></para>
    /// <para>
    /// A differenza del formato DevMap, questo record conserva <c>objectId</c>.
    /// La persistenza gameplay deve poter collegare food stock, ownership, memoria
    /// soggettiva e riferimenti futuri allo stesso oggetto dopo il reload.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Identita'</b>: id runtime e defId data-driven.</item>
    ///   <item><b>Posizione</b>: cella griglia X/Y.</item>
    ///   <item><b>Ownership</b>: ownerKind come int e ownerId.</item>
    ///   <item><b>Runtime state</b>: occupantNpcId, isOpen e isLocked.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class WorldObjectSaveData
    {
        public int objectId;
        public string defId;
        public int cellX;
        public int cellY;
        public int ownerKind;
        public int ownerId;
        public int occupantNpcId;
        public bool isOpen;
        public bool isLocked;
    }

    // =============================================================================
    // FoodStockSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per il componente <c>FoodStockComponent</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: componente separato dall'istanza</b></para>
    /// <para>
    /// Il cibo in stock e' stato classificato come gap critico perche' non rientra
    /// nel save NPC e non rientra nel DevMap v0. Questo record lega lo stock al suo
    /// oggetto tramite <c>objectId</c> senza duplicare l'intera istanza oggetto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>objectId</b>: oggetto che possiede il componente stock.</item>
    ///   <item><b>units</b>: quantita' corrente.</item>
    ///   <item><b>ownerKind/ownerId</b>: proprieta' oggettiva dello stock.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class FoodStockSaveData
    {
        public int objectId;
        public int units;
        public int ownerKind;
        public int ownerId;
    }

    // =============================================================================
    // ObjectUseStateSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per lo stato d'uso runtime di un oggetto interagibile.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato operativo esplicito</b></para>
    /// <para>
    /// Lo stato "in uso" non deve essere dedotto dal rendering o da cache UI. Se un
    /// letto o una risorsa sono occupati durante uno snapshot futuro, il contratto
    /// deve avere un posto esplicito in cui conservarlo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>objectId</b>: oggetto a cui appartiene lo stato.</item>
    ///   <item><b>isInUse</b>: flag di occupazione.</item>
    ///   <item><b>usingNpcId</b>: NPC che usa l'oggetto, oppure 0 se libero.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ObjectUseStateSaveData
    {
        public int objectId;
        public bool isInUse;
        public int usingNpcId;
    }

    // =============================================================================
    // NpcPrivateFoodSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per l'inventario privato MVP di un NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: possessione runtime minima</b></para>
    /// <para>
    /// Oggi ARCONTIO rappresenta l'inventario con un numero di unita' di cibo per
    /// NPC. Questo record non introduce un inventario generico, ma impedisce che il
    /// cibo trasportato sparisca dal contratto world-level.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcId</b>: proprietario fisico delle unita'.</item>
    ///   <item><b>units</b>: quantita' trasportata.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcPrivateFoodSaveData
    {
        public int npcId;
        public int units;
    }

    // =============================================================================
    // NpcPrivateFoodConsumeTickSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per <c>NpcLastPrivateFoodConsumeTick</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: marker runtime esplicito</b></para>
    /// <para>
    /// Il tick dell'ultimo consumo non cambia la quantità di cibo trasportata, ma
    /// influenza l'interpretazione sistemica di una perdita di cibo privato. Per
    /// evitare che il bootstrap ricostruisca implicitamente uno stato diverso, lo
    /// snapshot canonico lo conserva come sezione separata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcId</b>: NPC a cui appartiene il marker.</item>
    ///   <item><b>lastConsumeTick</b>: ultimo tick noto di consumo privato.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcPrivateFoodConsumeTickSaveData
    {
        public int npcId;
        public long lastConsumeTick;
    }

    // =============================================================================
    // NpcPinnedFoodStockBeliefSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per una singola belief pinned su uno stock privato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: belief soggettiva separata dal fatto</b></para>
    /// <para>
    /// Questo record non certifica che lo stock esista ancora o che contenga cibo:
    /// conserva soltanto il promemoria soggettivo dell'NPC su dove crede di avere
    /// uno stock privato. Il load deve quindi ripristinarlo come conoscenza pinned,
    /// non usarlo per creare o correggere FoodStocks oggettivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcId</b>: proprietario soggettivo della belief.</item>
    ///   <item><b>objectId</b>: stock ricordato dall'NPC.</item>
    ///   <item><b>lastKnownX/Y</b>: ultima cella nota soggettivamente.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcPinnedFoodStockBeliefSaveData
    {
        public int npcId;
        public int objectId;
        public int lastKnownX;
        public int lastKnownY;
    }

    // =============================================================================
    // NpcMemorySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per la memoria narrativa di un singolo NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: memoria soggettiva separata</b></para>
    /// <para>
    /// Le trace non sono verita' oggettiva: sono rappresentazioni soggettive. La
    /// sezione world-level le conserva per NPC, senza trasformarle in eventi globali
    /// e senza imporre ancora un algoritmo di rebuild dei belief.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcId</b>: proprietario soggettivo della memoria.</item>
    ///   <item><b>maxTraces</b>: cap runtime dello store al momento dello snapshot.</item>
    ///   <item><b>traces</b>: tracce serializzate gia' mature nel contratto NPC legacy.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcMemorySaveData
    {
        public int npcId;
        public int maxTraces;
        public MemoryTraceSaveData[] traces = Array.Empty<MemoryTraceSaveData>();
    }

    // =============================================================================
    // NpcBeliefStoreSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per il BeliefStore aggregato di un NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: belief come stato soggettivo persistito</b></para>
    /// <para>
    /// Le credenze sono derivate da memoria/percezione, ma non devono essere
    /// ricostruite al load se cio' cambierebbe merge, freshness, status o id locali.
    /// Il DTO conserva quindi lo stato aggregato gia' vissuto dall'NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcId</b>: NPC proprietario del belief store.</item>
    ///   <item><b>maxEntries</b>: cap locale dello store.</item>
    ///   <item><b>nextBeliefId</b>: prossimo id locale per preservare continuita' dopo load.</item>
    ///   <item><b>entries</b>: credenze aggregate serializzate in forma primitiva.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcBeliefStoreSaveData
    {
        public int npcId;
        public int maxEntries;
        public int nextBeliefId;
        public BeliefEntrySaveData[] entries = Array.Empty<BeliefEntrySaveData>();
    }

    // =============================================================================
    // BeliefEntrySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile minimale per una singola credenza soggettiva.
    /// </para>
    ///
    /// <para><b>Principio architetturale: campi primitivi per JsonUtility</b></para>
    /// <para>
    /// Questo record evita Dictionary, nullable e tipi Unity non necessari. Gli enum
    /// sono rappresentati come int per lasciare il contratto leggibile e stabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>beliefId/category/status/source</b>: identita' e classificazione cognitiva.</item>
    ///   <item><b>estimatedX/estimatedY</b>: posizione soggettiva associata.</item>
    ///   <item><b>confidence/freshness/sourceCount</b>: qualita' e supporto della credenza.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class BeliefEntrySaveData
    {
        public int beliefId;
        public int category;
        public int estimatedX;
        public int estimatedY;
        public float confidence;
        public float freshness;
        public int lastUpdatedTick;
        public int sourceCount;
        public int source;
        public int status;
    }

    // =============================================================================
    // NpcObjectMemorySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per la memoria pratica di oggetti e NPC osservati.
    /// </para>
    ///
    /// <para><b>Principio architetturale: memoria pratica soggettiva</b></para>
    /// <para>
    /// <c>NpcObjectMemoryStore</c> contiene cio' che l'NPC ricorda di oggetti e altri
    /// NPC osservati. Lo snapshot salva solo slot validi gia' presenti nello store:
    /// non controlla se il subject sia ancora vero o visibile nel mondo oggettivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcId</b>: proprietario della memoria pratica.</item>
    ///   <item><b>capacity</b>: cap dello store runtime.</item>
    ///   <item><b>entries</b>: slot serializzabili, inizialmente anche vuoti.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcObjectMemorySaveData
    {
        public int npcId;
        public int capacity;
        public NpcObjectMemoryEntrySaveData[] entries = Array.Empty<NpcObjectMemoryEntrySaveData>();
    }

    // =============================================================================
    // NpcObjectMemoryEntrySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO minimale per uno slot della memoria pratica oggetti/NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: memoria pratica non onnisciente</b></para>
    /// <para>
    /// I campi descrivono cio' che l'NPC ricorda di aver osservato, non lo stato
    /// oggettivo corrente del World. Il load futuro dovra' quindi ripristinarli come
    /// conoscenza soggettiva, non usarli per correggere oggetti reali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>subject</b>: kind/id/defId della cosa ricordata.</item>
    ///   <item><b>lastKnown cell</b>: posizione ricordata.</item>
    ///   <item><b>quality</b>: tick, reliability e flags osservati.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcObjectMemoryEntrySaveData
    {
        public bool isValid;
        public int subjectKind;
        public int subjectId;
        public string defId;
        public int objectId;
        public int lastKnownX;
        public int lastKnownY;
        public int ownerKind;
        public int ownerId;
        public int lastSeenTick;
        public float reliability01;
        public float utilityScore01;
        public bool isPinned;
        public int observedFlags;
        public int carriedFoodUnitsApprox;
    }

    // =============================================================================
    // NpcLandmarkMemorySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per la memoria landmark soggettiva di un NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: registry oggettivo distinto da conoscenza NPC</b></para>
    /// <para>
    /// Il <c>LandmarkRegistry</c> puo' essere ricostruito dalla mappa, ma cio' che un
    /// NPC conosce dei landmark deve restare una sezione separata e persistibile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcId</b>: proprietario della conoscenza landmark.</item>
    ///   <item><b>knownLandmarks</b>: nodi noti al singolo NPC.</item>
    ///   <item><b>knownEdges</b>: connessioni semplici note al singolo NPC.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcLandmarkMemorySaveData
    {
        public int npcId;
        public int maxLandmarks;
        public int maxEdges;
        public int lastVisitedLandmarkId;
        public long lastVisitedLandmarkTick;
        public LandmarkNodeMemorySaveData[] knownLandmarks = Array.Empty<LandmarkNodeMemorySaveData>();
        public LandmarkEdgeMemorySaveData[] knownEdges = Array.Empty<LandmarkEdgeMemorySaveData>();
    }

    // =============================================================================
    // LandmarkNodeMemorySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO minimale per un landmark conosciuto soggettivamente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: coordinate note, non scansione globale</b></para>
    /// <para>
    /// Il record conserva il riferimento al nodo e la qualita' della conoscenza. I
    /// campi kind/cell sono opzionali: in v0.10.11 <c>NpcLandmarkMemory</c> non li
    /// conserva come dato soggettivo, quindi il builder li lascia a sentinella invece
    /// di risolverli dal registry oggettivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>nodeId/kind</b>: identita' del landmark.</item>
    ///   <item><b>cellX/cellY</b>: posizione conosciuta.</item>
    ///   <item><b>lastSeenTick/confidence</b>: freschezza e affidabilita'.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class LandmarkNodeMemorySaveData
    {
        public int nodeId;
        public int kind;
        public int cellX;
        public int cellY;
        public long lastSeenTick;
        public float confidence01;
    }

    // =============================================================================
    // LandmarkEdgeMemorySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO minimale per un edge landmark semplice conosciuto da un NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: topologia soggettiva persistibile</b></para>
    /// <para>
    /// Due NPC possono conoscere connessioni diverse anche sullo stesso mondo
    /// oggettivo. Per questo l'edge e' salvato nella sezione per-NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>nodeA/nodeB</b>: estremi dell'edge noto.</item>
    ///   <item><b>cost</b>: costo stimato o appreso.</item>
    ///   <item><b>lastSeenTick/confidence</b>: qualita' della conoscenza.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class LandmarkEdgeMemorySaveData
    {
        public int nodeA;
        public int nodeB;
        public int cost;
        public long lastSeenTick;
        public float confidence01;
    }

    // =============================================================================
    // NpcComplexEdgeMemorySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per gli edge complessi appresi da un NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: pathfinding soggettivo persistibile</b></para>
    /// <para>
    /// Gli edge complessi rappresentano esperienza navigazionale accumulata. Se
    /// vengono persi al load, il mondo oggettivo resta simile ma il comportamento
    /// del singolo NPC regredisce implicitamente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcId</b>: proprietario della memoria edge complessa.</item>
    ///   <item><b>Parametri</b>: cap e maintenance dello store al momento dello snapshot.</item>
    ///   <item><b>edges</b>: lista di connessioni complesse note.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcComplexEdgeMemorySaveData
    {
        public int npcId;
        public int maxEdges;
        public int maxStepsPerRecording;
        public int staleTicksBeforeEviction;
        public float minConfidenceToKeep;
        public float confidenceDecayPerMaintenance;
        public int maintenancePeriodTicks;
        public long lastMaintenanceTick;
        public ComplexEdgeSaveData[] edges = Array.Empty<ComplexEdgeSaveData>();
    }

    // =============================================================================
    // ComplexEdgeSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO minimale per un edge complesso soggettivo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: costo e percorso non derivabili dal solo mondo</b></para>
    /// <para>
    /// Il costo, i segmenti e la confidence dipendono dall'esperienza dell'NPC. Il
    /// contratto li tiene insieme per permettere un reload futuro senza rifare
    /// apprendimento artificiale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>nodeA/nodeB</b>: estremi dell'edge.</item>
    ///   <item><b>baseCost/confidence</b>: peso operativo soggettivo.</item>
    ///   <item><b>segments</b>: compressione cardinale del percorso appreso.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ComplexEdgeSaveData
    {
        public int nodeA;
        public int nodeB;
        public int baseCost;
        public float confidence01;
        public long lastSeenTick;
        public int flags;
        public PathSegmentSaveData[] segments = Array.Empty<PathSegmentSaveData>();
    }

    // =============================================================================
    // PathSegmentSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per un segmento cardinale di percorso appreso.
    /// </para>
    ///
    /// <para><b>Principio architetturale: rappresentazione compatta e deterministica</b></para>
    /// <para>
    /// Il pathfinding landmark usa segmenti compressi invece di una lista arbitraria
    /// di celle. Salvare direzione e lunghezza mantiene il contratto piccolo e
    /// compatibile con JsonUtility.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>direction</b>: direzione cardinale come int.</item>
    ///   <item><b>length</b>: numero di passi consecutivi in quella direzione.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class PathSegmentSaveData
    {
        public int direction;
        public int length;
    }
}
