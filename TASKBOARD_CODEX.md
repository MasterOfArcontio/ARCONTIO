# TASKBOARD_CODEX.md — Lavagna Operativa Attiva ARCONTIO

Questo file è la console persistente dei task attivi per Codex.

Codex deve leggere questo file prima di iniziare ogni implementazione.

Non assumere vecchie conversazioni come memoria di stato valida.
Usa questo file come verità operativa corrente.

Questo file costituisce la cabina di pilotaggio operativa del macro job AI/Codex attualmente autorizzato.

Non ha funzione di diario minuto né di archivio completo delle campagne concluse.

La Taskboard deve rappresentare:

- il macro cantiere in esecuzione;
- il checkpoint interno attualmente attraversato;
- la sequenza di step già pianificati del blocco;
- i nodi aperti;
- il prossimo gate di validazione umana.

L'unità primaria di governo non è il singolo micro-step, ma il macro job con i suoi checkpoint interni.

---

# 0. Stato operativo corrente

## MACRO JOB ATTIVO: v0.38 - ArcGraph Legacy Absorption / Retirement

CHECKPOINT CORRENTE:
`v0.38h.03 - ArcGraph NPC Runtime Renderer Minimo`

STATUS:
NPC RUNTIME MINIMO IMPLEMENTATO / PROSSIMO STEP WIRING WRAPPER + GATE

RAMO BASE CORRENTE:
`ai-task/v0.38h-arcgraph-terrain-npc-minimal-runtime`

BASE DI INTEGRAZIONE:
`ai/codex-main`

OUTPUT ATTESO:

- auditare le dipendenze residue prima del pensionamento MapGrid;
- distinguere componenti assorbiti, componenti congelati da gate visuali e componenti non ancora migrati;
- registrare i gate visuali gia' recuperati manualmente dall'operatore;
- distinguere test data-only, test visuali e test interaction;
- ordinare il backlog residuo prima di qualunque pensionamento reale;
- registrare il superamento del gate interaction `wrapper -> router -> HUD + selection`;
- aggiornare lo stato dei blocchi ancora non pensionabili dopo il recupero dei tre gate minimi;
- auditare il percorso minimo stabile ArcGraph dopo gate terrain, actor/object e interaction validati;
- distinguere probe temporanei da futuri componenti produttivi;
- individuare il prossimo micro-step tecnico senza cancellare MapGrid;
- introdurre un coordinator C# passivo e riusabile per il percorso runtime minimo;
- introdurre un wrapper scena minimo, spento di default, come frontiera controllata del coordinator;
- non agganciare automaticamente ArcGraph alla scena;
- non cancellare componenti MapGrid;
- non modificare codice runtime;
- non salvare scene e non modificare prefab;
- non dichiarare produttivo ArcGraph actor/object o interaction solo perche' i probe sono stati validati manualmente;
- preparare il prossimo step operativo solo dopo avere chiarito quali gate sono passati e quali restano bloccanti;
- non introdurre DevTools, top bar o comandi;
- non leggere direttamente `MapGridWorldView`;
- mantenere il wrapper `v0.38f.04` come frontiera input, non come tool host;
- evitare di trasformare subito ArcGraph in gestore operativo UI;
- evitare che ArcGraph possieda DevTools, top bar, summary cards o command tools;
- evitare che i tool continuino a dipendere direttamente da `MapGridWorldView`;
- mantenere `MapGridData` come sorgente legacy temporanea, non come modello mappa definitivo;
- non creare renderer terrain produttivi permanenti;
- non rimuovere legacy, non salvare scene e non creare renderer produttivi senza `go` esplicito;
- non implementare simulazione produttiva di meteo, temperatura, umidita', precipitazioni, incendi, acqua, vegetazione o luce;
- non creare renderer produttivi Unity, asset load o modifiche scena;
- mantenere MapGrid come renderer produttivo finche' `v0.38` non avra' assorbito in modo controllato terrain, actor/object e debug minimo;
- rispettare la policy LOD definita in `v0.33f`;
- non migrare strumenti interattivi o dev tools prima dell'audit mirato;
- collegare feed e renderer solo tramite context, NPC e consumer espliciti, senza accessi globali.
- avviare il blocco `v0.38h` concentrandosi solo su terrain + NPC;
- trasformare progressivamente i probe terrain/NPC in runtime renderer controllati;
- non introdurre environment layers in questo blocco;
- non pensionare MapGrid in questo blocco iniziale.

PROMPT OPERATIVO - ROADMAP RESIDUA ARCGRAPH:

```text
v0.37    -> Debug/Overlay Migration: migrazione progressiva overlay diagnostici da MapGridWorldView
v0.38    -> Legacy Absorption / Retirement: assorbimento e pensionamento controllato del rendering MapGrid legacy
```

Regola corrente:

- il test visuale `v0.36.03v.02` ha sbloccato la prosecuzione;
- `v0.36.04` ha completato il builder effetti passivo;
- `v0.36.05` ha completato il builder meteo passivo;
- audit `v0.37a` completato: MapGridWorldView contiene overlay di mappa, HUD, UI interattiva e strumenti operativi;
- `v0.37b` ha introdotto contratti dati debug ArcGraph passivi;
- `v0.37c` ha introdotto builder queue debug passivo da input DTO/snapshot;
- `v0.37d` ha completato audit dei producer reali FOV, landmark e DT/GVD-DIN;
- `v0.37e` ha introdotto bridge passivo Landmark/GVD verso `ArcGraphDebugOverlaySnapshot`;
- `v0.37f` ha auditato il punto di alimentazione runtime del bridge debug;
- `v0.37g` ha introdotto feed runtime debug passivo Landmark/GVD;
- `v0.37h` ha auditato il percorso renderer/probe debug per visualizzare `ArcGraphDebugOverlayQueue`;
- `v0.37i` introduce il renderer scena temporaneo separato per visualizzare `ArcGraphDebugOverlayQueue`;
- `v0.37j` ha completato QA tecnica del renderer e preparato il gate visuale umano;
- `v0.37k` ha auditato il wiring runtime futuro tra context, feed e renderer debug;
- `v0.37l` ha introdotto contratto, coordinator e harness del wiring runtime debug;
- `v0.37m` ha introdotto wrapper scena passivo, spento di default, senza lettura di `SimulationHost`, `MapGridWorldProvider`, `MapGridWorldView` o `NPCSelection`;
- `v0.37n` ha auditato il context adapter: per debug overlay basta `World`; il candidato consigliato e' adapter separato che legge `MapGridWorldView.RuntimeWorld` read-only e passa `NPCSelection` al wrapper, senza far scegliere NPC al wrapper;
- `v0.37o` ha implementato `RuntimeWorld` read-only su `MapGridWorldView` e `ArcGraphDebugRuntimeMapGridAdapter` manuale, senza `Update`, hotkey, UI o scena salvata;
- `v0.37p` ha completato QA tecnica adapter e preparato gate visuale umano su scena non salvata;
- `v0.37q` e' il checkpoint corrente di closeout/fix: resta fermo sul gate visuale manuale, non aggiunge feature e non apre `v0.38` senza esito positivo o correzione mirata;
- gate visuale umano `v0.37q` superato: screenshot operatore con `FramePushedToWrapper`, `mapGridView=True`, `wrapper=True`, `config=True`, `world=True`, `selectedNpc=1`, `wrapperReason=QueueDispatched`, `wrapperBuilt=True`, `wrapperDispatched=True`;
- `v0.37` chiusa come Debug/Overlay Migration preparatoria: la catena MapGrid -> adapter -> wrapper -> coordinator -> feed -> queue -> probe renderer funziona nel gate manuale;
- `v0.38a` apre l'audit di assorbimento legacy: non rimuovere nulla prima della classificazione tecnica;
- audit `v0.38a` completato:
  - `MapGridBootstrap` possiede ancora config, layout, `MapGridData`, atlas, terrain chunks, camera, input pointer e attach di `MapGridWorldView`;
  - `MapGridWorldView` non e' solo renderer: contiene sync NPC/oggetti, sprite cache, stock label, balloon, decision flash, input debug, selection, FOV, Landmark/GVD, DT, summary overlay, top bar, audio feedback e rebind World;
  - `MapGridChunkRenderer` e' quasi interamente riassorbibile come tecnica terrain, perche' ArcGraph possiede gia' `ArcGraphTerrainChunkMeshBuilder`;
  - camera/input legacy non sono riassorbibili direttamente: ArcGraph ha controller view passivo, ma manca wrapper Unity produttivo che legga mouse/camera e applichi lo stato;
  - actor/object ArcGraph hanno snapshot e queue, ma manca renderer scena produttivo con sprite asset reali, pooling e cleanup;
  - debug Landmark/GVD e' migrato come ponte manuale validato; FOV current cone, labels screen-space, pointer coords, top bar, summary cards e DevTools restano fuori dal renderer ArcGraph produttivo iniziale;
  - `v0.38b` deve quindi progettare il bootstrap scena ArcGraph, non cancellare subito `MapGridWorldView`.
- piano `v0.38b` completato:
  - MapGrid e ArcGraph possono vivere in scene separate solo come strategia di test/confronto, non come due bootstrap simulativi divergenti;
  - ArcGraph non deve ricevere dati dal renderer MapGrid come dipendenza definitiva: deve ricevere context/snapshot read-only da una sorgente neutra o da adapter temporanei dichiarati;
  - `MapGridData` resta sorgente legacy temporanea per terrain snapshot, non modello mappa definitivo;
  - il primo wrapper scena ArcGraph deve essere esplicito, spento o gated di default, e deve ricevere riferimenti da Inspector/context senza `FindObjectOfType` operativo;
  - cancellazione fisica di `MapGridBootstrap`, `MapGridWorldView` e componenti collegati ammessa solo dopo assorbimento verificato di terrain, actor/object, debug minimo, camera/input e strumenti UI separati;
  - `v0.38c` deve progettare il primo ponte terrain scena, senza ancora salvare scene o introdurre renderer permanente non approvato.
- piano `v0.38c` completato:
  - il primo test terrain ArcGraph va fatto dentro `Scene_MapGrid` come probe temporaneo/gated, non ancora in scena separata;
  - la scena separata ArcGraph resta utile dopo il primo gate terrain, quando il contratto dati sara' stabile;
  - il blocco tecnico da risolvere prima del rendering e' l'accesso read-only alla `MapGridData` costruita da `MapGridBootstrap`;
  - `ArcGraphWorldAdapter.FillTerrainSnapshots(...)` e `ArcGraphTerrainChunkMeshBuilder` sono gia' pronti come catena data-only;
  - manca un bridge scena che consegni `MapGridData` ad `ArcGraphRuntimeContext` senza far dipendere ArcGraph dal renderer MapGrid;
  - il prossimo micro-step e' quindi `v0.38c.01 - ArcGraph Terrain Runtime Map Access Contract`.
- micro-step `v0.38c.01` completato:
  - `MapGridBootstrap` espone `RuntimeConfig` e `RuntimeMap` come accessi read-only dichiarati;
  - aggiunto `ArcGraphTerrainRuntimeMapGridAdapter`;
  - l'adapter costruisce un `ArcGraphRuntimeContext(config, map, world opzionale)` da riferimenti Inspector espliciti;
  - il probe `ArcGraph/Probe Terrain Runtime Context From MapGrid` inizializza un `ArcGraphBootstrapRuntime` temporaneo in memoria e conta gli snapshot terrain prodotti;
  - nessun renderer, nessuna mesh scena, nessun `Update`, nessuna lettura globale, nessuna modifica scena.
- micro-step `v0.38c.02` completato:
  - aggiunto `ArcGraphTerrainSceneProbeRenderer`;
  - il renderer consuma `ArcGraphTerrainRuntimeMapGridAdapter`;
  - il renderer usa `ArcGraphComparisonGate` con modalita' `TemporaryDebugSceneProbe`;
  - il renderer costruisce mesh terrain temporanee sotto `ArcGraphTerrainSceneProbeRoot`;
  - il materiale terrain deve essere assegnato da Inspector: nessun asset load dentro ArcGraph;
  - aggiunti context menu `ArcGraph/Render Terrain Scene Probe From MapGrid` e `ArcGraph/Clear Terrain Scene Probe`;
  - nessun salvataggio scena, nessun renderer permanente, nessuna rimozione legacy.
- gate visuale umano `v0.38c.03` superato:
  - l'operatore ha eseguito il test terrain ArcGraph in Unity;
  - il probe terrain funziona;
  - `ArcGraphTerrainSceneProbeRenderer` viene considerato sufficiente per chiudere il gate terrain;
  - il terrain ArcGraph puo' diventare candidato per il successivo percorso produttivo controllato;
  - MapGrid resta comunque renderer principale fino all'assorbimento di actor/object, debug minimo, camera/input e UI/dev tools.
- audit `v0.38d` completato:
  - `MapGridWorldView` gestisce insieme sprite NPC/oggetti, cache, sorting, stock label, balloon, collider, flash decisionale, input/debug, selection e overlay;
  - ArcGraph possiede gia' snapshot actor/object, layer actor/object e `ArcGraphRenderQueue` globale ordinata;
  - `ArcGraphWorldAdapter` copia actor, oggetti, stock e movimento NPC multi-tick in sola lettura;
  - il punto mancante non e' il dato actor/object, ma il wrapper scena Unity che consumi la queue;
  - il primo bridge non deve migrare input, UI, collider, balloon, stock label o DevTools;
  - la risoluzione sprite deve restare lato wrapper/asset resolver, non dentro i builder passivi;
  - il prossimo micro-step e' `v0.38d.01 - ArcGraph Actor/Object Scene Renderer Contract`.
- micro-step `v0.38d.01` completato:
  - introdotto `ArcGraphSpriteResolveRequest`;
  - introdotto `IArcGraphSpriteResolver`;
  - introdotto `ArcGraphActorObjectSceneRendererContract`;
  - introdotta diagnostica `ArcGraphActorObjectSceneRendererDiagnostics`;
  - introdotto piano scena passivo `ArcGraphActorObjectSceneRenderPlan`;
  - introdotto `ArcGraphActorObjectSceneRenderPlanBuilder`;
  - introdotto harness `ArcGraphActorObjectSceneRendererContractHarness`;
  - il plan traduce `ArcGraphRenderQueue` in entry scene-side con posizione mondo, sorting order, sprite request e motion actor;
  - nessun GameObject, nessun asset load, nessuna scena salvata, nessuna modifica a `MapGridWorldView`;
  - compilazione isolata dei nuovi file riuscita con `dotnet csc`.
- micro-step `v0.38d.02` completato:
  - introdotto `ArcGraphSerializedSpriteResolver` con mapping sprite serializzati da Inspector;
  - introdotto `ArcGraphActorObjectSceneProbeRenderer`;
  - il probe costruisce `ArcGraphBootstrapRuntime` temporaneo, `ArcGraphRenderQueue` e `ArcGraphActorObjectSceneRenderPlan`;
  - il probe crea `SpriteRenderer` solo sotto `ArcGraphActorObjectSceneProbeRoot`;
  - aggiunti context menu `ArcGraph/Render Actor Object Scene Probe From MapGrid` e `ArcGraph/Clear Actor Object Scene Probe`;
  - nessun `Resources.Load`, nessuna scena salvata, nessuna rimozione legacy;
  - compilazione isolata riuscita; restano solo warning attesi su campi `SerializeField`.
- gate visuale `v0.38d.03` congelato su richiesta operatore:
  - test manuale non eseguito;
  - gate non fallito;
  - actor/object ArcGraph non ancora promosso a candidato produttivo;
  - il test verra' recuperato insieme agli altri gate visuali quando l'operatore potra' provarli in Unity;
  - durante il congelamento non cancellare MapGrid e non procedere al pensionamento produttivo actor/object.
- audit `v0.38e` completato:
  - il debug minimo assorbito in ArcGraph e' limitato a Landmark + GVD-DIN + DT heatmap;
  - la catena MapGridWorldView -> adapter -> wrapper -> coordinator -> feed -> queue -> probe renderer resta valida come ponte manuale read-only;
  - FOV current cone, FOV historical heatmap, pointer HUD, runtime cost HUD, label screen-space, summary cards, top bar, DevTools, click-to-move e selection restano fuori dal debug minimo;
  - questi elementi vanno auditati come strumenti interattivi/UI, non come semplice renderer.
- audit `v0.38f` completato:
  - `MapGridWorldView` concentra ancora rendering, selection, click-to-move, FOV, summary overlay, pointer HUD, top bar, DevTools, audio feedback e rebind World;
  - top bar e DevTools sono strumenti operativi, non renderer;
  - summary cards, movement/MBQD panel, pointer HUD e runtime cost HUD sono UI/debug screen-space, non renderer mappa;
  - `NPCSelection` e' gia' un servizio separato da conservare;
  - il picking NPC/cella resta ancora dentro MapGrid e va spostato dietro un boundary;
  - `v0.38g` non puo' partire davvero finche' il gate actor/object resta congelato e gli strumenti non hanno boundary separato.
- micro-step `v0.38f.01` completato:
  - introdotto `ArcGraphInteractionTargetKind`;
  - introdotto `ArcGraphInteractionFrame`;
  - introdotta diagnostica `ArcGraphInteractionBoundaryDiagnostics`;
  - introdotto `ArcGraphInteractionBoundaryBuilder`;
  - introdotto harness `ArcGraphInteractionBoundaryHarness`;
  - il boundary risolve UI bloccante, cella, actor e object da input view-side gia' normalizzato;
  - priorita' target dichiarata: UI, actor, object, cella;
  - nessun `GameObject`, `SpriteRenderer`, input fisico Unity, command tool, selection globale, `SimulationHost` o `MapGridWorldView`;
  - compilazione isolata riuscita con `dotnet csc`;
  - ricerca statica: dipendenze vietate assenti nel codice operativo, occorrenze solo nei commenti.
- audit `v0.38f.02` completato:
  - `MapGridWorldView` legge direttamente keyboard, mouse, EventSystem, provider puntatore e camera per hotkey, selection, FOV, pointer HUD e click-to-move;
  - `MapGridRuntimeDevToolsOverlay` e' separato ma dipende ancora da MapGridWorldView, provider puntatore, camera, Mouse/Keyboard/EventSystem e SimulationHost per i comandi;
  - `MapGridCameraController` non e' riutilizzabile come base ArcGraph: ArcGraph possiede gia' `ArcGraphViewInputFrame`, `ArcGraphViewController` e mapper viewport/cella;
  - il futuro adapter scena deve leggere input Unity e produrre frame passivi, non selezionare NPC o inviare comandi;
  - pannello laterale, barra superiore, overlay NPC, pointer HUD e DevTools vanno trattati come moduli consumer separati.
- micro-step `v0.38f.03` completato:
  - introdotto `ArcGraphInteractionSceneFrame`;
  - introdotta diagnostica `ArcGraphInteractionSceneAdapterDiagnostics`;
  - introdotta interfaccia `IArcGraphInteractionFrameConsumer`;
  - introdotto `ArcGraphInteractionSceneAdapterContract`;
  - introdotto harness `ArcGraphInteractionSceneAdapterContractHarness`;
  - il contract applica `ArcGraphViewController`, poi `ArcGraphInteractionBoundaryBuilder`, poi consumer opzionale;
  - nessuna lettura di input Unity, nessun `MonoBehaviour`, nessun `GameObject`, nessun `SimulationHost`, nessun `MapGridWorldView`;
  - compilazione isolata riuscita con `dotnet csc`;
  - ricerca statica: dipendenze vietate assenti nel codice operativo, occorrenze solo nei commenti.
- micro-step `v0.38f.04` completato:
  - introdotto `ArcGraphInteractionSceneAdapterWrapper`;
  - introdotta diagnostica `ArcGraphInteractionSceneAdapterWrapperDiagnostics`;
  - il wrapper legge input fisico Unity solo dentro la frontiera autorizzata;
  - gate `adapterEnabled` e `processInUpdate` falsi di default;
  - supporto viewport schermo intero o viewport manuale;
  - supporto consumer opzionale tramite setter o `MonoBehaviour` che implementa `IArcGraphInteractionFrameConsumer`;
  - nessun DevTools, SelectionTool, TopBar, SidePanel, NpcOverlay, `SimulationHost`, `MapGridWorldView`, `Resources.Load`, `GameObject`, `AddComponent` o salvataggio scena;
  - compilazione isolata riuscita con `dotnet csc`, con soli warning attesi sui campi `SerializeField`.
- audit `v0.38f.05` completato:
  - `MapGridPointerCoordsOverlay` e' il consumer piu' sicuro da migrare per primo, perche' e' HUD passivo e non invia comandi;
  - `NPCSelection` resta candidato secondo, perche' e' semplice ma muta stato view-side condiviso;
  - summary card, movement panel e overlay NPC vanno dopo selection e frame interattivo stabile;
  - top bar, DevTools e click-to-move sono strumenti operativi, non renderer, e non devono essere posseduti da ArcGraph;
  - FOV current cone richiede un producer overlay dedicato e non appartiene al primo consumer;
  - ordine consigliato: Pointer HUD, selection, overlay NPC, side panel, top bar separata, DevTools command tool, FOV producer;
  - prossimo checkpoint: `v0.38f.06 - ArcGraph Pointer HUD Passive Contract`.
- micro-step `v0.38f.06` completato:
  - introdotto `ArcGraphPointerHudSnapshot`;
  - introdotta diagnostica `ArcGraphPointerHudDiagnostics`;
  - introdotto `ArcGraphPointerHudSnapshotBuilder`;
  - introdotto harness `ArcGraphPointerHudSnapshotBuilderHarness`;
  - il builder trasforma `ArcGraphInteractionFrame` in testo HUD minimale: cella, actor, object, UI blocked o unknown;
  - nessun `GameObject`, `MonoBehaviour`, `Canvas`, `Resources.Load`, `SimulationHost`, `MapGridWorldView`, `MapGridWorldProvider`, `NPCSelection` o input fisico Unity;
  - compilazione isolata riuscita con Roslyn `csc` e reference pack .NET;
  - prossimo checkpoint: `v0.38f.07 - ArcGraph Pointer HUD Scene Consumer`.
- micro-step `v0.38f.07` completato:
  - introdotto `ArcGraphPointerHudSceneConsumer`;
  - il consumer implementa `IArcGraphInteractionFrameConsumer`;
  - il consumer usa `ArcGraphPointerHudSnapshotBuilder` e conserva `LastSnapshot` / `LastDiagnostics`;
  - il disegno HUD e' gated tramite `hudEnabled`;
  - visualizzazione temporanea tramite `OnGUI`, senza Canvas, prefab o salvataggio scena;
  - nessuna lettura di mouse fisico, `World`, `SimulationHost`, `MapGridWorldView`, `MapGridWorldProvider`, `NPCSelection`, DevTools o top bar;
  - compilazione isolata riuscita con Roslyn `csc` e assembly Unity necessari, con solo warning atteso su campo `SerializeField`;
  - prossimo checkpoint: `v0.38f.08 - ArcGraph Selection Consumer`.
- micro-step `v0.38f.08` completato:
  - introdotto `ArcGraphSelectionSceneConsumer`;
  - esteso `ArcGraphViewInputFrame` con `IsPrimaryPointerPressedThisFrame`;
  - il wrapper Unity propaga `mouse.leftButton.wasPressedThisFrame` nel frame input normalizzato;
  - il consumer selection seleziona solo actor validi su click primario, non su hover;
  - default coerente con MapGrid legacy: niente clear automatico su cella vuota;
  - nessun DevTools, top bar, comando, `SimulationHost` o lettura diretta `MapGridWorldView`;
  - compilazione isolata riuscita sul consumer e sul wrapper modificato con Roslyn `csc`, assembly Unity e `Unity.InputSystem`;
  - nodo tecnico emerso: il wrapper oggi dispatcha a un solo consumer, quindi serve un router per HUD + selection;
  - prossimo checkpoint: `v0.38f.09 - ArcGraph Interaction Consumer Router`.
- micro-step `v0.38f.09` completato:
  - introdotto `ArcGraphInteractionConsumerRouter`;
  - il router implementa `IArcGraphInteractionFrameConsumer`;
  - il wrapper puo' puntare a un solo consumer, cioe' il router;
  - il router inoltra lo stesso frame a piu' consumer modulari, per esempio Pointer HUD e Selection;
  - il router e' gated tramite `routerEnabled`;
  - diagnostica: consumer candidati, dispatchati, saltati, target, actor e reason;
  - nessun DevTools, top bar, comando, `SimulationHost`, input fisico Unity o lettura `MapGridWorldView`;
  - compilazione isolata riuscita con Roslyn `csc` e assembly Unity necessari, con soli warning attesi su campi `SerializeField`;
  - prossimo checkpoint: `v0.38f.10 - Gate visuale consumer modulari ArcGraph`.
- micro-step `v0.38f.10a` completato:
  - introdotto `ArcGraphInteractionRenderQueueWiringProbe`;
  - il probe costruisce una `ArcGraphRenderQueue` actor/object partendo dal `ArcGraphTerrainRuntimeMapGridAdapter`;
  - il probe consegna la queue al `ArcGraphInteractionSceneAdapterWrapper` tramite `SetRenderQueue(...)`;
  - aggiunto context menu `ArcGraph/Push Interaction Render Queue To Wrapper`;
  - nessuna scena salvata, nessun prefab, nessun renderer produttivo, nessun DevTools, nessun top bar e nessun comando;
  - ricerca statica sulle dipendenze vietate superata;
  - `dotnet build Assembly-CSharp.csproj --no-restore` non conclusivo per mancanza del file temporaneo Unity `Temp/obj/Assembly-CSharp/project.assets.json`, restore non eseguito per non toccare cartelle temporanee;
  - il gate `v0.38f.10` resta in attesa di test umano, ora con queue actor/object collegabile al wrapper.
- gate visuale `v0.38f.10` congelato su richiesta dell'operatore:
  - nessun test Unity eseguito ora;
  - gate non fallito;
  - catena tecnica pronta ma non validata in scena;
  - da recuperare insieme agli altri test visuali congelati;
  - non considerare completata la migrazione interattiva ArcGraph finche' Pointer HUD, picking actor e selection non saranno verificati manualmente.
  - aggiornamento successivo: il gate e' stato recuperato e superato in `v0.38g.02`.
- audit `v0.38g.00` completato:
  - `MapGridChunkRenderer` e' il blocco piu' vicino al pensionamento, ma manca ancora renderer terrain produttivo permanente;
  - actor/object ArcGraph esiste come probe ma resta bloccato dal gate visuale congelato;
  - interaction ArcGraph HUD/selection esiste come catena tecnica e, dopo `v0.38g.02`, non e' piu' bloccata dal gate visuale;
  - `MapGridWorldView` non e' pensionabile perche' contiene ancora sync NPC/oggetti, selection, click-to-move, FOV, Landmark/GVD, DT, pointer coords, summary, top bar bridge, audio e rebind World;
  - `MapGridRuntimeDevToolsOverlay` e `MapGridRuntimeControlTopBar` non sono renderer e devono diventare moduli UI/tool separati prima di qualunque rimozione;
  - FOV current cone, label screen-space, balloon NPC, stock label e summary cards non hanno ancora equivalente ArcGraph produttivo;
  - `v0.38g` resta bloccato come pensionamento reale;
  - prossimo step consigliato: `v0.38g.01 - ArcGraph Frozen Visual Gates Backlog Plan`.
- micro-step `v0.38g.01` completato:
  - l'operatore ha recuperato manualmente parte dei gate visuali dentro Unity;
  - test terrain data-only confermato con `TerrainSnapshotsBuilt`, `terrainSnapshots=16384`, `layerCount=4`;
  - test actor/object confermato tecnicamente e visivamente come probe temporaneo: `ActorObjectSceneProbeRendered`, `queueEntries=1`, `plannedEntries=1`, `createdObjects=1`, `missingSprites=0`;
  - actor/object usa ancora fallback sprite quando `spriteResolver=False`, quindi il gate dimostra il flusso dati/render probe ma non valida ancora asset resolver definitivo;
  - interaction non e' ancora validata: nello screenshot il wrapper risulta operativo ma `Interaction Consumer Behaviour` non era collegato al router, con diagnostica `consumer=False`;
  - il fix richiesto per il gate interaction e' wiring Inspector: `ArcGraphInteractionSceneAdapterWrapper.Interaction Consumer Behaviour` deve puntare a `ArcGraphInteractionConsumerRouter`;
  - finche' il gate interaction non produce `consumer=True`, `queue=True` e target cella/actor coerenti, non promuovere HUD/selection ArcGraph;
  - `v0.38g` resta preparatoria: niente cancellazione MapGrid, niente renderer produttivo permanente, niente salvataggio scena.
  - prossimo step consigliato: `v0.38g.02 - ArcGraph Interaction Gate Recovery`, focalizzato solo sul recupero del wiring e sulla conferma del gate interaction.
- micro-step `v0.38g.02` completato:
  - l'operatore ha confermato il superamento del test 3 interaction;
  - il gate `wrapper -> router -> Pointer HUD + Selection` viene considerato superato a livello visuale/manuale;
  - risultano ora recuperati i tre gate minimi: terrain, actor/object probe e interaction base;
  - questa conferma abilita la pianificazione del percorso minimo stabile ArcGraph, ma non abilita ancora il pensionamento fisico di MapGrid;
  - actor/object resta probe temporaneo e non renderer produttivo con pooling;
  - terrain resta probe/contratto validato, ma non ancora bootstrap terrain produttivo permanente;
  - interaction resta base validata, ma DevTools, top bar, click-to-move, summary card, FOV current cone, label screen-space, balloon NPC e stock label non sono ancora migrati;
  - prossimo step consigliato: `v0.38g.03 - ArcGraph Minimal Stable Runtime Path Audit`.
- audit `v0.38g.03` completato:
  - i tre gate minimi sono validati ma vivono ancora come probe/manual wiring;
  - `ArcGraphTerrainRuntimeMapGridAdapter` resta la sorgente temporanea dichiarata: legge `MapGridBootstrap.RuntimeConfig`, `MapGridBootstrap.RuntimeMap` e `MapGridWorldView.RuntimeWorld`;
  - `ArcGraphTerrainSceneProbeRenderer` crea mesh terrain temporanee sotto root dedicato, ma non e' renderer produttivo permanente;
  - `ArcGraphActorObjectSceneProbeRenderer` crea `SpriteRenderer` temporanei sotto root dedicato, ma non possiede pooling, lifecycle stabile o asset resolver obbligatorio;
  - `ArcGraphInteractionSceneAdapterWrapper` e `ArcGraphInteractionConsumerRouter` formano una base valida per input/HUD/selection, ma non devono diventare host di DevTools o comandi;
  - il prossimo passaggio tecnico non deve cancellare MapGrid: deve introdurre un contratto/coordinator di percorso runtime minimo stabile;
  - il candidato prossimo e' `v0.38g.04 - ArcGraph Minimal Runtime Coordinator Contract`;
  - scopo del coordinator: orchestrare context, refresh snapshot, terrain, actor/object e interaction in modo esplicito e gated;
  - divieto confermato: niente DevTools, top bar, click-to-move, summary card, FOV current cone, label screen-space, balloon NPC o stock label in questo passaggio.
- micro-step `v0.38g.04` completato:
  - introdotto `ArcGraphMinimalRuntimeCoordinatorFrame`;
  - introdotto `ArcGraphMinimalRuntimeCoordinatorDiagnostics`;
  - introdotto `ArcGraphMinimalRuntimeCoordinator`;
  - introdotto `ArcGraphMinimalRuntimeCoordinatorHarness`;
  - il coordinator e' C# passivo, non `MonoBehaviour`;
  - il coordinator riceve `ArcGraphRuntimeContext` gia' costruito da un adapter esterno;
  - il coordinator inizializza o riusa `ArcGraphBootstrapRuntime`;
  - il coordinator ricrea il runtime solo quando cambiano le sorgenti config/mappa/world;
  - il coordinator puo' rinfrescare snapshot e costruire `ArcGraphRenderQueue` actor/object;
  - la queue viene pulita sui gate falliti per evitare dati derivati vecchi;
  - nessun `GameObject`, asset load, input fisico, `SimulationHost`, `MapGridWorldProvider`, `FindObjectOfType`, DevTools, top bar o comando introdotto;
  - QA statica sulle dipendenze vietate superata nel codice operativo;
  - `dotnet build Assembly-CSharp.csproj --no-restore` non conclusivo per mancanza del file Unity temporaneo `Temp/obj/Assembly-CSharp/project.assets.json`;
  - prossimo step consigliato: `v0.38g.05 - ArcGraph Minimal Runtime Scene Wrapper Contract`.
- micro-step `v0.38g.05` completato:
  - introdotto `ArcGraphMinimalRuntimeSceneWrapper`;
  - introdotta diagnostica `ArcGraphMinimalRuntimeSceneWrapperDiagnostics`;
  - il wrapper e' un `MonoBehaviour` frontiera, spento di default;
  - riceve da Inspector `ArcGraphTerrainRuntimeMapGridAdapter` e opzionalmente `ArcGraphInteractionSceneAdapterWrapper`;
  - usa internamente `ArcGraphMinimalRuntimeCoordinator`;
  - espone context menu `ArcGraph/Process Minimal Runtime Frame`;
  - puo' costruire context runtime, refresh snapshot e queue actor/object tramite coordinator;
  - puo' consegnare opzionalmente la queue al wrapper interaction gia' validato;
  - non crea `GameObject`, renderer, asset load, DevTools, top bar, click-to-move, comandi o pannelli avanzati;
  - non legge `SimulationHost`, `MapGridWorldProvider` o scene globali tramite `FindObjectOfType`;
  - non salva scene o prefab;
  - QA statica sulle dipendenze vietate superata nel codice operativo, con occorrenze solo testuali/commento o nome Unity `OnDestroy`;
  - `dotnet build Assembly-CSharp.csproj --no-restore` non conclusivo per mancanza del file Unity temporaneo `Temp/obj/Assembly-CSharp/project.assets.json`;
  - prossimo passo logico: chiudere la struttura minima stabile e poi iniziare il blocco terrain + NPC produttivo controllato.
- `main`, `ai/codex-main` e branch task chiuso vengono allineati a fine step;
- eventuale ponte mappa reale andra' pianificato dentro `v0.38` come micro-step esplicitamente approvato;
- non accumulare ulteriori moduli senza harness e diagnostica.
- closeout `v0.38g.06` completato:
  - la struttura minima stabile ArcGraph viene considerata chiusa come base preparatoria;
  - non e' stato introdotto nuovo codice;
  - ArcGraph possiede ora contratti, layer passivi, adapter read-only, bootstrap runtime, coordinator, wrapper scena minimo, queue actor/object, interaction boundary, router, Pointer HUD, selection e probe/gate gia' recuperati;
  - "stabile" significa catena ordinata dati -> context -> bootstrap/coordinator -> snapshot/layer -> queue -> probe/wrapper, non renderer definitivo;
  - restano non produttivi terrain probe, actor/object probe, wiring probe, debug probe e HUD OnGUI temporaneo;
  - restano fuori scope acqua, vegetazione, luci, meteo, incendi, DevTools, top bar e pensionamento MapGrid;
  - prima della promozione produttiva vanno rieseguiti i gate manuali del `ArcGraphMinimalRuntimeSceneWrapper` in Unity;
  - prossimo blocco consigliato: `v0.38h - ArcGraph Terrain + NPC Minimal Runtime`.
- audit `v0.38h.01` completato:
  - auditati `ArcGraphTerrainSceneProbeRenderer`, `ArcGraphActorObjectSceneProbeRenderer`, `ArcGraphMinimalRuntimeSceneWrapper`, `ArcGraphTerrainChunkMeshBuilder` e `ArcGraphActorObjectSceneRenderPlanBuilder`;
  - terrain possiede gia' snapshot, layer, dirty chunk, mesh builder, UV map e probe visuale validato;
  - terrain probe non e' produttivo perche' crea/distrugge root e chunk temporanei, non ha pooling stabile e non consuma direttamente un frame runtime unico;
  - NPC/actor possiede gia' snapshot, layer, render queue, render plan, sprite request, resolver preparatorio e probe visuale validato;
  - NPC probe non e' produttivo perche' crea SpriteRenderer temporanei, non ha pool per actor id, puo' usare fallback sprite e non gestisce lifecycle spawn/update/despawn;
  - `ArcGraphMinimalRuntimeSceneWrapper` e' il punto corretto di orchestrazione, ma non deve diventare renderer;
  - decisione tecnica: introdurre renderer separati terrain e NPC, entrambi gated, spenti di default, con root locale, cleanup confinato, diagnostica e nessun accesso globale;
  - ordine consigliato: terrain contract, terrain renderer minimo, NPC contract, NPC renderer minimo, collegamento al wrapper, gate visuale Unity;
  - prossimo micro-step: `v0.38h.02 - ArcGraph Terrain Runtime Renderer Contract`.
- micro-step `v0.38h.02` completato:
  - introdotto `ArcGraphTerrainRuntimeSceneRendererContract`;
  - introdotto `ArcGraphTerrainRuntimeSceneRendererDiagnostics`;
  - introdotto `ArcGraphTerrainRuntimeSceneRenderer`;
  - il renderer terrain runtime e' spento di default;
  - puo' renderizzare manualmente da `ArcGraphTerrainRuntimeMapGridAdapter`;
  - puo' ricevere in futuro `ArcGraphRuntimeContext` e `ArcGraphBootstrapRuntime` dal wrapper/coordinator;
  - mantiene un root locale stabile;
  - mantiene un pool per `ArcGraphChunkCoord`;
  - riusa `GameObject`, `MeshFilter`, `MeshRenderer` e `Mesh` per chunk gia' esistenti;
  - aggiorna solo chunk dirty;
  - puo' pulire il dirty state dopo render;
  - non legge `SimulationHost`, `MapGridWorldProvider`, `MapGridWorldView`, `NPCSelection` o globali scena;
  - non usa `FindObjectOfType`, non carica asset, non invia comandi e non salva scene;
  - ricerca statica sulle dipendenze vietate superata;
  - `dotnet build Assembly-CSharp.csproj --no-restore` non conclusivo per mancanza del file Unity temporaneo `Temp/obj/Assembly-CSharp/project.assets.json`;
  - controllo Roslyn isolato sui tre file nuovi riuscito, con solo warning atteso su campo `SerializeField` assegnabile da Inspector;
  - prossimo step accelerato: `v0.38h.03 - ArcGraph NPC Runtime Renderer Minimo`.
- micro-step `v0.38h.03` completato:
  - introdotto `ArcGraphNpcRuntimeSceneRendererContract`;
  - introdotto `ArcGraphNpcRuntimeSceneRendererDiagnostics`;
  - introdotto `ArcGraphNpcRuntimeSceneRenderer`;
  - il renderer NPC runtime e' spento di default;
  - consuma una `ArcGraphRenderQueue` gia' prodotta dal wrapper/coordinator;
  - puo' renderizzare manualmente dalla queue esposta da `ArcGraphMinimalRuntimeSceneWrapper`;
  - riusa il builder passivo actor/object per posizione mondo, sorting order e sprite request;
  - filtra solo entry `Actor`, lasciando gli oggetti a un renderer dedicato futuro;
  - mantiene un root locale stabile `ArcGraphNpcRuntimeRoot`;
  - mantiene un pool per `actorId`;
  - riusa `GameObject` e `SpriteRenderer` per NPC gia' esistenti;
  - disattiva opzionalmente gli actor non piu' presenti nel frame;
  - usa `IArcGraphSpriteResolver` se assegnato da Inspector;
  - puo' generare un fallback magenta opzionale per non bloccare il gate senza asset definitivi;
  - non legge `SimulationHost`, `MapGridWorldProvider`, `MapGridWorldView`, `NPCSelection` o globali scena;
  - non usa `FindObjectOfType`, non carica asset, non invia comandi e non salva scene;
  - ricerca statica sulle dipendenze vietate superata;
  - controllo Roslyn isolato sui tre file nuovi riuscito, con solo warning atteso su campo `SerializeField` assegnabile da Inspector;
  - prossimo step: `v0.38h.04 - ArcGraph Minimal Runtime Wiring + Gate`.

DOC SYNC:

- Roadmap ufficiale aggiornata con macro versioni `v0.31`-`v0.38`;
- branch `ai-task/v0.31-arcgraph-bootstrap-analysis` aperto da closeout `v0.30j`;
- audit `v0.31a` completato e documentato;
- contratto bootstrap `v0.31b` definito;
- forma bootstrap `v0.31c` decisa: nucleo C# passivo, wrapper Unity rinviato;
- strategia accesso dati `v0.31d` definita: runtime context esplicito, niente letture globali;
- policy attivazione `v0.31e` definita: `InternalStateOnly`, niente attivazione automatica scena, niente rendering;
- implementazione minima `v0.31f` completata: nucleo C# passivo del bootstrap ArcGraph;
- QA `v0.31g` superata: compilazione isolata, no chiamate vietate, no modifiche a Core/MapGrid/scena/meta;
- closeout `v0.31h` in completamento;
- prossimo macro checkpoint previsto: `v0.32 - ArcGraph Terrain Renderer`;
- apertura operativa `v0.32` autorizzata dall'operatore;
- audit `v0.32a` completato: MapGrid terrain chunking, atlas, mesh, varianti floor e wall-top;
- contratto `v0.32b` definito: terrain builder a chunk, output mesh data, niente aggancio scena automatico;
- UV map terrain `v0.32c` implementata senza asset load e senza dipendenza codice da `MapGridTileAtlas`;
- chunk mesh builder `v0.32d` implementato come produttore passivo di mesh data;
- dirty chunk rebuild `v0.32e` implementato su `ArcGraphRenderState.Dirty.DirtyChunks`;
- harness statico `v0.32f` implementato e compilabile;
- QA `v0.32g` superata: compilazione, diff scope, chiamate vietate, no Core/MapGrid/scena/meta;
- closeout `v0.32h` completato: Definition of Done, debiti residui e preparazione `v0.33`;
- apertura operativa `v0.33` autorizzata dall'operatore;
- branch `ai-task/v0.33a-arcgraph-view-audit` aperto;
- decisioni zoom/pan/LOD registrate per `v0.33`;
- audit `v0.33a` completato: camera legacy, input mouse, zoom/pan, conversione coordinate, confine ArcGraph View/Camera;
- branch `ai-task/v0.33b-arcgraph-view-contract` aperto;
- contratto `v0.33b` implementato: config view, zoom discreto, input frame astratto, stato vista e rettangolo celle visibili;
- branch `ai-task/v0.33c-arcgraph-view-json-config` aperto;
- configurazione `v0.33c` implementata: DTO JSON, parser da stringa e JSON default ArcGraph;
- branch `ai-task/v0.33d-arcgraph-pan-zoom-controller` aperto;
- controller `v0.33d` implementato: zoom discreto, pan astratto, diagnostica e harness smoke;
- branch `ai-task/v0.33e-arcgraph-view-coordinates` aperto;
- coordinate `v0.33e` implementate: mapper viewport/cella, risultato diagnostico e harness smoke;
- branch `ai-task/v0.33f-arcgraph-zoom-lod-policy` aperto;
- policy LOD `v0.33f` implementata: profili actor/vegetation/object/effect per i quattro zoom;
- branch `ai-task/v0.33g-arcgraph-comparison-mode` aperto;
- gate comparativo `v0.33g` implementato: options, diagnostics, comparison gate e harness;
- branch `ai-task/v0.33h-arcgraph-closeout` aperto;
- QA finale `v0.33h` superata sul perimetro documentale/runtime: diff scope, chiamate vietate e assenza modifiche a Core/MapGrid/scena/meta;
- closeout `v0.33h` completato: Definition of Done, debiti residui e preparazione `v0.34`;
- prossimo macro checkpoint previsto: `v0.34 - ArcGraph Actor/Object Renderer`;
- apertura operativa `v0.34` autorizzata dall'operatore;
- branch `ai-task/v0.34a-arcgraph-actor-object-audit` aperto;
- audit `v0.34a` completato: layer actor/object, snapshot, adapter e LOD;
- branch `ai-task/v0.34b-arcgraph-render-items` aperto;
- contratti `v0.34b` implementati: render item actor/object, sort key, diagnostics;
- branch `ai-task/v0.34c-arcgraph-object-render-queue` aperto;
- object queue `v0.34c` implementata;
- branch `ai-task/v0.34d-arcgraph-actor-render-queue` aperto;
- actor queue `v0.34d` implementata;
- branch `ai-task/v0.34e-arcgraph-combined-render-queue` aperto;
- queue combinata `v0.34e` implementata;
- branch `ai-task/v0.34f-arcgraph-render-queue-harness` aperto;
- harness `v0.34f` implementato;
- branch `ai-task/v0.34g-arcgraph-actor-object-closeout` aperto;
- closeout `v0.34g` completato;
- prossimo macro checkpoint previsto: `v0.35 - ArcGraph Actor Motion Runtime Bridge`;
- apertura operativa `v0.35` autorizzata dall'operatore;
- branch `ai-task/v0.35a-arcgraph-motion-audit` aperto;
- audit `v0.35a` completato con stop progettuale: manca origine/destinazione tipizzata nello snapshot running action;
- operatore ha confermato l'opzione consigliata: metadata movement tipizzato dentro `RunningActionRuntimeState`;
- branch `ai-task/v0.35b-arcgraph-motion-metadata` aperto per implementare contratto, bridge ArcGraph e QA;
- metadata movement tipizzato implementato;
- snapshot read-only propagato;
- lookup CPU-leggera per NPC implementata in `RunningActionStore`;
- `ArcGraphWorldAdapter` ora alimenta `ArcGraphActorMotionSnapshot`;
- test EditMode aggiunti per factory movement e lookup store;
- QA statico eseguito: diff check, no chiamate vietate runtime nuove, no `.meta`, no `Library/Temp/Obj`.
- branch `ai-task/v0.36a-arcgraph-environment-audit` aperto come prossimo checkpoint preparatorio.
- audit `v0.36a` completato: placeholder Water, Vegetation, Light, Weather, Effect gia' presenti e registrabili solo su richiesta;
- contratto `ArcGraphEnvironmentVisualLayerContract` aggiunto per dichiarare scope, sorgente esterna, dirty, animazione ArcGraph, overlay e divieto Unity creation;
- catalogo `ArcGraphEnvironmentVisualContractCatalog` aggiunto per i cinque layer ambientali;
- harness `ArcGraphEnvironmentVisualContractHarness` aggiunto;
- decisione animazione fissata: ArcGraph sceglie frame/posa/layer/LOD, Unity disegna soltanto l'output concreto.
- branch `ai-task/v0.36.01-arcgraph-vegetation-renderer` aperto;
- vegetation renderer passivo implementato;
- aggiunto `ArcGraphVegetationLayer.CopySnapshotsTo(...)`;
- aggiunto `ArcGraphVegetationRenderItem`;
- aggiunto `ArcGraphVegetationRenderQueueDiagnostics`;
- aggiunto `ArcGraphVegetationRenderQueueBuilder`;
- aggiunto `ArcGraphVegetationRenderQueueHarness`;
- vegetazione non ancora fusa nella queue globale actor/object;
- branch `ai-task/v0.36.02-arcgraph-water-renderer` aperto;
- water renderer passivo implementato;
- aggiunto `ArcGraphWaterLayer.CopySnapshotsTo(...)`;
- aggiunto `ArcGraphRenderItemKind.Water`;
- aggiunto `ArcGraphWaterRenderItem`;
- aggiunto `ArcGraphWaterRenderQueueDiagnostics`;
- aggiunto `ArcGraphWaterRenderQueueBuilder`;
- aggiunto `ArcGraphWaterRenderQueueHarness`;
- acqua non ancora fusa nella queue globale actor/object;
- branch `ai-task/v0.36.03-arcgraph-light-renderer` aperto;
- apertura `v0.36.03` documentata: Light Renderer preparatorio, nessuna luce Unity, nessuna propagazione luce produttiva;
- light renderer passivo implementato;
- aggiunto `ArcGraphLightLayer.CopySnapshotsTo(...)`;
- aggiunto `ArcGraphRenderItemKind.Light`;
- aggiunto `ArcGraphLightRenderItem`;
- aggiunto `ArcGraphLightRenderQueueDiagnostics`;
- aggiunto `ArcGraphLightRenderQueueBuilder`;
- aggiunto `ArcGraphLightRenderQueueHarness`;
- luce non ancora fusa nella queue globale actor/object;
- branch `ai-task/v0.36.03v-arcgraph-visual-probe` aperto;
- apertura `v0.36.03v` documentata: probe visivo minimo, debug/test, non sostitutivo di MapGrid;
- visual probe data-only implementato;
- aggiunto `ArcGraphVisualProbeDiagnostics`;
- aggiunto `ArcGraphVisualProbeFrame`;
- aggiunto `ArcGraphVisualProbeBuilder`;
- aggiunto `ArcGraphVisualProbeHarness`;
- frame dati pronto per terrain, actor/object, vegetation, water e light;
- branch `ai-task/v0.36.03v.01-arcgraph-scene-probe-renderer` aperto;
- apertura `v0.36.03v.01` documentata: renderer debug temporaneo per disegnare `ArcGraphVisualProbeFrame`;
- scene probe renderer debug implementato;
- aggiunto `ArcGraphSceneProbeRenderer`;
- aggiunto `ArcGraphVisualProbeHarness.CreateDefaultProbeFrame(...)`;
- il probe usa sprite runtime colorati, root temporaneo e context menu manuale;
- branch `ai-task/v0.36.03v.02-arcgraph-first-visual-test-qa` aperto;
- apertura `v0.36.03v.02` documentata: QA visuale manuale del primo scene probe;
- QA visuale manuale `v0.36.03v.02` superata: probe visibile su `Scene_MapGrid`, camera assegnata, layer terrain/water/vegetation/object/actor/light visibili, nessuna modifica scena/prefab richiesta;
- branch `ai-task/v0.36.04-arcgraph-effect-renderer` aperto come checkpoint Effect Renderer;
- apertura `v0.36.04` documentata: Effect Renderer preparatorio, nessuna simulazione incendi o particelle produttive.
- effect renderer passivo implementato;
- aggiunto `ArcGraphEffectLayer.CopySnapshotsTo(...)`;
- aggiunto `ArcGraphRenderItemKind.Effect`;
- aggiunto `ArcGraphEffectRenderItem`;
- aggiunto `ArcGraphEffectRenderQueueDiagnostics`;
- aggiunto `ArcGraphEffectRenderQueueBuilder`;
- aggiunto `ArcGraphEffectRenderQueueHarness`;
- QA Roslyn isolata riuscita sui file toccati e nuovi;
- effetti non ancora fusi nella queue globale actor/object;
- branch `ai-task/v0.36.05-arcgraph-weather-renderer` aperto come checkpoint Weather Renderer;
- aggiunto `ArcGraphRenderItemKind.Weather`;
- aggiunto `ArcGraphWeatherRenderItem`;
- aggiunto `ArcGraphWeatherRenderQueueDiagnostics`;
- aggiunto `ArcGraphWeatherRenderQueueBuilder`;
- aggiunto `ArcGraphWeatherRenderQueueHarness`;
- QA Roslyn isolata riuscita sui file Weather toccati e nuovi;
- meteo non ancora fuso nella queue globale actor/object;
- meteo non ancora disegnato dal scene probe;
- branch `ai-task/v0.37-arcgraph-debug-overlay-migration` aperto come checkpoint Debug/Overlay Migration;
- audit `v0.37a` completato su `MapGridWorldView`, overlay FOV, landmark, DT, pointer coords, summary cards,
  top bar, DevTools e contratti Core collegati;
- classificazione overlay completata: FOV/landmark/DT sono candidati primari; pointer/HUD e landmark labels
  richiedono un canale screen-space; summary cards, top bar e DevTools sono rinviati;
- aggiunto `ArcGraphRenderItemKind.Debug`;
- aggiunto `ArcGraphDebugOverlayKind`;
- aggiunto `ArcGraphDebugOverlaySpace`;
- aggiunti item debug cell/node/edge/label;
- aggiunta `ArcGraphDebugOverlayQueue`;
- aggiunta `ArcGraphDebugOverlayQueueDiagnostics`;
- aggiunto `ArcGraphDebugOverlayContractHarness`;
- QA Roslyn isolata riuscita sui contratti debug;
- prossimo micro-step: `v0.37c - ArcGraph Debug Overlay Queue Builder`.

OBIETTIVO:

Chiudere `v0.37l - ArcGraph Debug Runtime Wiring Contract` introducendo il
contratto passivo che permette a context, feed e renderer debug di incontrarsi
senza wrapper scena automatici, hotkey, UI o global access.

Esito operativo `v0.37h`:

- auditato `ArcGraphSceneProbeRenderer`;
- auditato `ArcGraphVisualProbeFrame` e `ArcGraphVisualProbeBuilder`;
- auditati item debug cell/node/edge/label;
- confrontato il renderer Landmark legacy MapGrid;
- conclusione: non estendere `ArcGraphVisualProbeFrame` per gli overlay debug;
- conclusione: non trasformare `ArcGraphSceneProbeRenderer` in un renderer debug
  multiuso;
- candidato consigliato: nuovo `ArcGraphDebugOverlaySceneProbeRenderer` separato;
- il renderer debug futuro deve consumare solo `ArcGraphDebugOverlayQueue`;
- celle DT/GVD raw possono essere sprite runtime colorati;
- nodi Landmark/GVD possono essere sprite runtime colorati;
- edge Landmark/GVD richiedono `LineRenderer` temporanei o segmenti grafici dedicati;
- labels/HUD restano fuori scope;
- FOV current cone resta fuori scope.

Prossimo micro-step consigliato:

`v0.37i - ArcGraph Debug Overlay Scene Probe Renderer`

Scope consigliato:

- implementare `ArcGraphDebugOverlaySceneProbeRenderer`;
- accettare una `ArcGraphDebugOverlayQueue` gia' prodotta;
- aggiungere context menu per renderizzare una queue finta Landmark/GVD;
- usare root temporaneo e cleanup confinato;
- disegnare celle e nodi con sprite runtime 1x1;
- disegnare edge con `LineRenderer` debug temporanei;
- non leggere `World`;
- non dipendere da `MapGridWorldView`;
- non modificare scene/prefab/meta.

Esito operativo `v0.37i`:

- aggiunto `ArcGraphDebugOverlaySceneProbeRenderer`;
- il renderer consuma `ArcGraphDebugOverlayQueue` gia' prodotte;
- celle debug renderizzate con sprite runtime 1x1;
- nodi debug renderizzati con sprite runtime 1x1;
- edge debug renderizzati con `LineRenderer` temporanei;
- aggiunto context menu `ArcGraph/Render Default Debug Overlay Probe`;
- aggiunto context menu `ArcGraph/Clear Debug Overlay Probe`;
- il probe default crea dati Landmark/GVD finti tramite prepared-data, senza leggere `World`;
- labels/HUD e FOV current cone restano fuori scope;
- nessuna scena, prefab, asset o `.meta` modificati.

Prossimo micro-step consigliato:

`v0.37j - ArcGraph Debug Overlay Visual QA`

Scope consigliato:

- testare manualmente il context menu del renderer in Unity;
- verificare visivamente celle, nodi ed edge;
- verificare il cleanup del root temporaneo;
- controllare la diagnostica in Console;
- non collegare ancora runtime reale, NPC attivo, toggle UI o DevTools.

Esito operativo `v0.37j`:

- Unity Editor aperto sul progetto ARCONTIO;
- log Editor controllato dopo import script;
- ricompilazione Unity riuscita con `Tundra build success`;
- nessun errore C# prodotto dal renderer debug;
- warning presenti solo su codice legacy MapGrid/AtomViewer gia' noto;
- compilazione Roslyn isolata del renderer e dei feed debug riuscita;
- context menu verificati:
  - `ArcGraph/Render Default Debug Overlay Probe`;
  - `ArcGraph/Clear Debug Overlay Probe`;
- controllo dipendenze vietate riuscito:
  - nessun `Resources.Load`;
  - nessun `MapGridWorldView`;
  - nessun `SimulationHost`;
  - nessuna lettura diretta `World.GetNpcLandmarkOverlayData`;
  - nessuna lettura diretta `World.GetGvdDinOverlayData`;
- non creati editor runner automatici;
- non modificate scene, prefab, asset o `.meta`.

Gate visuale umano richiesto:

- aggiungere temporaneamente `ArcGraphDebugOverlaySceneProbeRenderer` a un GameObject di test;
- non salvare la scena;
- opzionalmente assegnare `MainCamera` al campo `Scene Camera`;
- usare `ArcGraph/Render Default Debug Overlay Probe`;
- verificare root temporaneo `ArcGraphDebugOverlaySceneProbeRoot`;
- verificare celle, nodi ed edge colorati;
- verificare log Console atteso:
  `cells=3, nodes=4, edges=8, labelsIgnored=0, visible=15`;
- usare `ArcGraph/Clear Debug Overlay Probe`;
- verificare eliminazione del root temporaneo.

Prossimo micro-step consigliato dopo conferma visuale positiva:

`v0.37k - ArcGraph Debug Runtime Wiring Audit`

Esito operativo `v0.37k`:

- auditato `MapGridWorldView`;
- auditato `MapGridLandmarkOverlay`;
- auditato `MapGridFovHeatmapOverlay`;
- auditato `MapGridRuntimeControlTopBar`;
- auditato `MapGridWorldProvider`;
- auditato `NPCSelection`;
- auditati `ArcGraphRuntimeContext` e `ArcGraphBootstrapRuntime`;
- auditati `ArcGraphDebugOverlayRuntimeFeed`, opzioni e renderer probe;
- conclusione: il renderer non deve diventare punto di wiring runtime;
- conclusione: il feed non deve risolvere input, toggle o NPC attivo;
- conclusione: serve un coordinatore view-side separato, piccolo e disattivato di default;
- il coordinatore futuro deve ricevere context, activeNpcId e opzioni gia' risolti;
- il coordinatore futuro deve chiamare feed e renderer, non leggere globali;
- vietati `SimulationHost.Instance`, `MapGridWorldProvider`, `MapGridWorldView`, hotkey e picking nel coordinatore ArcGraph;
- `NPCSelection.SelectedNpcId` e' candidato valido come sorgente view-only dell'NPC attivo, ma attraverso adapter minimo;
- FOV current cone resta fuori scope perche' non ha ancora producer DTO separato;
- GVD/DT devono restare opzionali per contenere il costo runtime.

Prossimo micro-step consigliato:

`v0.37l - ArcGraph Debug Runtime Wiring Contract`

Scope consigliato:

- definire contratto dati/stato del coordinatore runtime debug;
- non creare wrapper scena automatico;
- non leggere `SimulationHost.Instance`;
- non aggiungere hotkey o UI;
- non migrare FOV;
- preparare solo il punto controllato in cui feed e renderer potranno incontrarsi.

Esito operativo `v0.37l`:

- aggiunto `IArcGraphDebugOverlayQueueConsumer`;
- aggiunto `ArcGraphDebugRuntimeWiringFrame`;
- aggiunto `ArcGraphDebugRuntimeWiringDiagnostics`;
- aggiunto `ArcGraphDebugRuntimeWiringCoordinator`;
- aggiunto `ArcGraphDebugRuntimeWiringHarness`;
- `ArcGraphDebugOverlaySceneProbeRenderer` implementa il consumer tipizzato;
- il coordinator valida frame, context e World prima di chiamare il feed;
- il coordinator non legge `SimulationHost.Instance`;
- il coordinator non usa `MapGridWorldProvider`;
- il coordinator non dipende da `MapGridWorldView`;
- il coordinator non legge input, mouse, tastiera o camera;
- il dispatch verso renderer avviene solo se un consumer viene fornito dal chiamante;
- harness aggiunto per verificare `OverlayDisabled`, `RuntimeContextMissing` e `WorldMissing`;
- nessuna scena, prefab, asset o `.meta` modificati;
- FOV current cone ancora fuori scope.

Prossimo micro-step consigliato:

`v0.37m - ArcGraph Debug Runtime Scene Wrapper`

Scope consigliato:

- creare wrapper MonoBehaviour piccolo e disattivato di default;
- ricevere riferimenti serializzati espliciti;
- costruire il frame di wiring;
- usare `NPCSelection.SelectedNpcId` solo come sorgente view-only dell'active NPC;
- chiamare il coordinator;
- non introdurre hotkey o UI;
- non usare `SimulationHost.Instance`;
- non usare `MapGridWorldProvider`;
- non salvare scene o prefab.

Esito operativo `v0.37m`:

- aggiunto `ArcGraphDebugRuntimeSceneWrapper`;
- il wrapper e' un `MonoBehaviour` piccolo, disattivato di default;
- riceve `ArcGraphRuntimeContext` solo tramite setter/metodo esplicito;
- riceve l'id NPC attivo tramite setter/metodo esplicito;
- non legge `NPCSelection.SelectedNpcId`, scelta rimandata a eventuale adapter dedicato;
- non legge `SimulationHost.Instance`;
- non usa `MapGridWorldProvider`;
- non dipende da `MapGridWorldView`;
- non crea hotkey, UI, picking o polling `Update`;
- costruisce `ArcGraphDebugRuntimeWiringFrame`;
- chiama `ArcGraphDebugRuntimeWiringCoordinator`;
- consegna la queue solo al renderer consumer assegnato esplicitamente;
- espone context menu di smoke test contratto e tentativo manuale sullo stato corrente;
- nessuna scena, prefab, asset o `.meta` modificati.

Prossimo micro-step consigliato:

`v0.37n - ArcGraph Debug Runtime Context Adapter Audit`

Scope consigliato:

- auditare come produrre in modo controllato un `ArcGraphRuntimeContext` reale per il wrapper;
- decidere se l'adapter deve vivere vicino a MapGrid, ArcGraph o bootstrap scena;
- valutare come passare NPC selezionato senza far leggere `NPCSelection` al wrapper;
- non introdurre ancora hotkey o UI;
- non salvare scene o prefab.

Esito operativo `v0.37n`:

- auditato `MapGridBootstrap`;
- auditato `MapGridWorldView`;
- auditato `MapGridWorldProvider`;
- auditato `MapGridData`;
- auditato `NPCSelection`;
- auditato `ArcGraphRuntimeContext`;
- auditato `ArcGraphDebugRuntimeSceneWrapper`;
- conclusione: per il debug overlay `v0.37` serve soprattutto `World`;
- `ArcGraphRuntimeContext` puo' essere parziale: `Config` utile, `Map` non necessaria per Landmark/GVD;
- `MapGridWorldView` gia' gestisce il rebind del `World` dopo load snapshot, ma `_world` non e' esposto;
- `MapGridBootstrap` possiede `_map`, ma non la espone; non serve toccarlo per il debug overlay;
- `MapGridWorldProvider` resta sorgente globale view-side esistente, ma non va letto da ArcGraph wrapper/coordinator/feed;
- `NPCSelection.SelectedNpcId` e' sorgente view-only accettabile solo dentro adapter dedicato;
- il wrapper ArcGraph deve restare passivo e non deve leggere `NPCSelection`;
- candidato consigliato: adapter separato `ArcGraphDebugRuntimeMapGridAdapter`;
- l'adapter deve referenziare esplicitamente `MapGridWorldView` e `ArcGraphDebugRuntimeSceneWrapper`;
- l'adapter deve costruire `ArcGraphRuntimeContext(config, map:null, world)` usando world/config gia' bindati dalla view;
- l'adapter deve passare `NPCSelection.SelectedNpcId` al wrapper;
- l'adapter non deve scegliere fallback tipo "primo NPC", per evitare lavoro CPU e policy nascoste;
- l'adapter non deve avere hotkey o UI;
- eventuale refresh continuo deve restare spento di default.

Prossimo micro-step consigliato:

`v0.37o - ArcGraph Debug Runtime MapGrid Adapter`

Scope consigliato:

- aggiungere property read-only minima `RuntimeWorld` su `MapGridWorldView`;
- implementare adapter ArcGraph separato che costruisce context parziale da `MapGridWorldView`;
- far leggere `NPCSelection.SelectedNpcId` solo all'adapter;
- invocare il wrapper solo tramite metodo esplicito o context menu;
- nessun `Update` automatico attivo di default;
- nessuna scena, prefab o `.meta` modificati.

Esito operativo `v0.37o`:

- aggiunta property read-only `MapGridWorldView.RuntimeWorld`;
- aggiunto `ArcGraphDebugRuntimeMapGridAdapter`;
- aggiunta diagnostica `ArcGraphDebugRuntimeMapGridAdapterDiagnostics`;
- l'adapter referenzia esplicitamente `MapGridWorldView`;
- l'adapter referenzia esplicitamente `ArcGraphDebugRuntimeSceneWrapper`;
- l'adapter legge `NPCSelection.SelectedNpcId`;
- l'adapter costruisce `ArcGraphRuntimeContext(config, map:null, world)`;
- l'adapter usa `world.Global.CurrentTickIndex` solo come tick diagnostico;
- l'adapter chiama il wrapper tramite `ProcessFrame(context, selectedNpcId, sourceTick)`;
- nessun `Update`;
- nessuna hotkey;
- nessuna UI;
- nessuna lettura di `SimulationHost.Instance`;
- nessuna lettura di `MapGridWorldProvider`;
- nessuna scena, prefab, asset o `.meta` modificati.

Prossimo micro-step consigliato:

`v0.37p - ArcGraph Debug Runtime Adapter QA`

Scope consigliato:

- compilazione tecnica del nuovo adapter;
- verifica assenza chiamate vietate operative;
- preparazione istruzioni manuali per test Inspector;
- gate visuale umano su scena non salvata;
- non introdurre ancora polling automatico o UI.

Esito operativo `v0.37p`:

- QA tecnica adapter completata;
- `git diff --check` pulito;
- compilazione Roslyn isolata riuscita sul perimetro:
  - `MapGridWorldView`;
  - `MapGridRuntimeControlTopBar`;
  - `ArcGraphDebugRuntimeMapGridAdapter`;
  - `ArcGraphDebugRuntimeSceneWrapper`;
  - contratti/feed/renderer debug ArcGraph collegati;
- warning presenti solo per:
  - tipi legacy inclusi come sorgente durante compilazione isolata;
  - campi Unity serializzati assegnabili da Inspector;
  - API legacy `FindObjectOfType` gia' presente in MapGrid;
- nessun errore C# rilevato;
- controllo chiamate vietate sul nuovo perimetro ArcGraph riuscito:
  - nessuna chiamata operativa a `SimulationHost`;
  - nessuna chiamata operativa a `MapGridWorldProvider`;
  - nessun `Update`;
  - nessun input `Keyboard`/`Mouse`;
  - nessun `Resources.Load`;
  - nessuna ricerca scena operativa nell'adapter.

Gate visuale umano richiesto:

1. Aprire `Scene_MapGrid` in Unity.
2. Non salvare la scena.
3. Creare o selezionare un GameObject temporaneo di test.
4. Aggiungere questi componenti:
   - `ArcGraphDebugOverlaySceneProbeRenderer`;
   - `ArcGraphDebugRuntimeSceneWrapper`;
   - `ArcGraphDebugRuntimeMapGridAdapter`.
5. Nel wrapper assegnare:
   - `Debug Overlay Renderer` = lo stesso `ArcGraphDebugOverlaySceneProbeRenderer`;
   - `Overlay Enabled` = true;
   - `Dispatch To Renderer` = true.
6. Nell'adapter assegnare:
   - `Map Grid World View` = il GameObject/Component `MapGridWorldView` della scena;
   - `Target Wrapper` = il componente `ArcGraphDebugRuntimeSceneWrapper`.
7. Avviare Play Mode.
8. Selezionare un NPC nella MapGrid, se disponibile.
9. Dal menu contestuale dell'adapter usare:
   - `ArcGraph/Push Debug Runtime Frame From MapGrid`.
10. Verificare Console:
   - log adapter con `FramePushedToWrapper`;
   - `mapGridView=True`;
   - `wrapper=True`;
   - `world=True`;
   - `selectedNpc` uguale all'NPC selezionato oppure `-1`;
   - `wrapperReason=QueueDispatched` quando overlay e world sono validi.
11. Verificare in scena il root temporaneo:
   - `ArcGraphDebugOverlaySceneProbeRoot`.
12. Verificare overlay debug Landmark/GVD visibile se i dati runtime producono item.
13. Usare `ArcGraph/Clear Debug Overlay Probe` per pulire.
14. Uscire da Play Mode.
15. Non salvare la scena.

Esiti ammessi nel test:

- `OverlayDisabled`: il wrapper e' spento;
- `RuntimeContextMissing`: adapter senza MapGridView;
- `WorldMissing`: MapGridView non ha ancora bindato il World;
- `QueueDispatched`: frame valido e dispatch al renderer avvenuto;
- `FramePushedToWrapper`: l'adapter ha consegnato correttamente il frame al wrapper.

Prossimo passaggio consigliato:

Attendere esito gate visuale umano. Se positivo, chiudere `v0.37` con un closeout
Debug/Overlay Migration oppure aprire un micro-step mirato solo se dal test
emerge un problema reale.

Esito operativo `v0.37d`:

- `LandmarkOverlayNode`, `LandmarkOverlayEdge` e `GvdDinOverlaySnapshot` sono pronti per un bridge passivo;
- `DebugFovTelemetry` heatmap storica e' parzialmente pronta, ma richiede policy su NPC attivo e costo scansione buffer;
- FOV current cone non e' pronto: oggi e' calcolato dentro `MapGridFovHeatmapOverlay.RenderCurrentCone(...)`;
- pointer coords, runtime cost HUD, landmark labels e DT numeric labels sono screen-space/UI e vanno rinviati;
- prossimo step tecnico consigliato: bridge landmark/GVD, non renderer Unity.

Esito operativo `v0.37e`:

- aggiunto `ArcGraphDebugOverlayProducerBridge`;
- aggiunto `ArcGraphDebugOverlayProducerBridgeDiagnostics`;
- aggiunto `ArcGraphDebugOverlayProducerBridgeHarness`;
- conversione Landmark: world, known, route, lm path, direct path, jump path, complex edge;
- conversione GVD-DIN: DT cells, GVD raw cells, GVD nodes, GVD edges;
- QA `git diff --check` riuscita;
- compilazione Roslyn isolata riuscita sui file nuovi;
- nessuna lettura diretta `World`, nessun collegamento a `MapGridWorldView`, nessun renderer Unity, nessuna modifica scena/prefab/asset/meta.

La `v0.33` ha costruito la base controllata per verificare ArcGraph contro MapGrid senza trasformare la comparazione in un percorso runtime stabile.

Decisioni operative v0.33:

- mappa prevista: `250x250` celle;
- zoom con quattro livelli fissi;
- rotellina mouse: uno scatto = un livello avanti/indietro;
- zoom 1: `300x300` celle visibili, senza pan;
- zoom 2: `150x150` celle visibili;
- zoom 3: `75x75` celle visibili;
- zoom 4: `20x20` celle visibili;
- dimensione mappa e livelli zoom in JSON di configurazione mappa;
- pan con pressione rotellina mouse mantenuta durante movimento mouse;
- zoom 1 e 2 senza animazioni sprite;
- zoom 1 e 2 senza vestizione NPC a layer;
- zoom 1 e 2 con rappresentazione semplificata: icone, sprite statici, aggregazioni d'area e filtri di visibilita'.

Checkpoint v0.33:

| Checkpoint | Task | Stato |
|---|---|---|
| v0.33a | Audit view/camera legacy e registrazione decisioni zoom/pan/LOD | Completato |
| v0.33b | Contratto ArcGraph View/Camera | Completato |
| v0.33c | Config mappa/zoom JSON | Completato |
| v0.33d | Controller pan/zoom discreto | Completato |
| v0.33e | Coordinate screen/world/cell e clamp viewport | Completato |
| v0.33f | Policy LOD per zoom | Completato |
| v0.33g | Modalita' comparativa ArcGraph/MapGrid | Completato nel perimetro gate/diagnostica |
| v0.33h | QA e closeout | Completato |

Decisioni operative v0.34:

- il renderer actor/object resta passivo;
- input ammessi: `ArcGraphActorLayer`, `ArcGraphObjectLayer`, `ArcGraphZoomLodProfile`, vista/celle visibili;
- output ammesso: render item o render queue value-only;
- output vietato: `GameObject`, `SpriteRenderer`, asset load, `Camera.main`, `Mouse.current`, mutazioni world;
- MapGrid resta renderer produttivo;
- vestizione actor a layer non viene implementata in questa versione;
- movimento multi-tick reale resta in `v0.35`.

Checkpoint v0.34:

| Checkpoint | Task | Stato |
|---|---|---|
| v0.34a | Audit actor/object layer, snapshot, adapter e LOD | Completato |
| v0.34b | Contratti render item passivi actor/object | Completato |
| v0.34c | Builder object render queue | Completato |
| v0.34d | Builder actor render queue | Completato |
| v0.34e | Sorting e filtri LOD per zoom | Completato |
| v0.34f | Harness smoke actor/object senza scena | Completato |
| v0.34g | QA, closeout e preparazione v0.35 | Completato |

Esito v0.34a:

- `ArcGraphActorLayer` e `ArcGraphObjectLayer` esistono gia' come cache snapshot passive;
- `ArcGraphActorVisualSnapshot` contiene gia' posa e motion snapshot opzionale;
- `ArcGraphObjectVisualSnapshot` contiene gia' id, defId, cella, sprite key, held state e stock food;
- `ArcGraphWorldAdapter` produce snapshot read-only senza mutare il mondo;
- `ArcGraphZoomLodPolicy` e' gia' disponibile per distinguere zoom 1/2/3/4;
- manca ancora il contratto `RenderItem` passivo;
- manca ancora una render queue ordinata;
- manca ancora una diagnostica minima della queue;
- manca una lettura sequenziale read-only dei layer.

Indicazione per `v0.34b`:

Definire contratti value-only per actor/object render item.

La queue dovra' poter ricevere snapshot dai layer senza esporre dizionari interni e senza leggere `World`.

Forma preferita:

```text
ArcGraphActorLayer.CopySnapshotsTo(...)
ArcGraphObjectLayer.CopySnapshotsTo(...)
snapshot
-> render item
-> queue ordinata
```

Nota:

La classificazione degli oggetti minori non e' ancora disponibile negli snapshot. In `v0.34` non bisogna inventarla.

Esito v0.34b:

- aggiunto `ArcGraphRenderItemKind`;
- aggiunto `ArcGraphRenderSortKey`;
- aggiunto `ArcGraphActorRenderItem`;
- aggiunto `ArcGraphObjectRenderItem`;
- aggiunto `ArcGraphRenderQueueDiagnostics`;
- i contratti sono value-only;
- nessun asset load;
- nessuna camera;
- nessuna scena;
- nessuna mutazione world.

Indicazione per `v0.34c`:

Costruire il builder object render queue.

Prima serve aggiungere a `ArcGraphObjectLayer` una lettura sequenziale controllata, ad esempio `CopySnapshotsTo(...)`, per evitare di esporre il dizionario interno.

Esito v0.34c:

- aggiunto `ArcGraphObjectLayer.CopySnapshotsTo(...)`;
- aggiunto `ArcGraphObjectRenderQueueBuilder`;
- il builder produce `ArcGraphObjectRenderItem`;
- il builder ordina via `ArcGraphRenderSortKey`;
- gli oggetti held vengono marcati nascosti come `HeldObject`;
- gli oggetti senza sprite key vengono marcati nascosti come `MissingSpriteKey`;
- nessuna lettura `World`;
- nessun asset load;
- nessuna scena.

Indicazione per `v0.34d`:

Costruire il builder actor render queue usando `ArcGraphActorLayer`, `ArcGraphActorVisualSnapshot.ResolvePose()` e la policy LOD actor.

Esito v0.34d:

- aggiunto `ArcGraphActorLayer.CopySnapshotsTo(...)`;
- aggiunto `ArcGraphActorRenderQueueBuilder`;
- il builder produce `ArcGraphActorRenderItem`;
- il builder usa `ResolvePose()` per posizione visuale;
- il builder ordina via `ArcGraphRenderSortKey`;
- actor senza sprite key vengono marcati nascosti come `MissingSpriteKey`;
- nessuna lettura `World`;
- nessuna mutazione NPC;
- nessun asset load;
- nessuna scena.

Indicazione per `v0.34e`:

Creare una queue combinata actor/object con sorting condiviso e diagnostica aggregata, cosi' l'harness finale puo' validare un flusso completo.

Esito v0.34e:

- aggiunto `ArcGraphRenderQueueEntry`;
- aggiunto `ArcGraphRenderQueue`;
- aggiunto `ArcGraphRenderQueueBuilder`;
- la queue conserva item actor/object tipizzati;
- le entries globali ordinano actor e oggetti insieme;
- gli actor hanno `VisualLayerOrder = 20`;
- gli oggetti hanno `VisualLayerOrder = 10`;
- a parita' di cella l'actor viene dopo l'oggetto;
- nessun renderer concreto introdotto.

Indicazione per `v0.34f`:

Costruire harness smoke completo actor/object senza scena, verificando contatori, sorting e casi hidden.

Esito v0.34f:

- aggiunto `ArcGraphRenderQueueHarness`;
- aggiunto `ArcGraphRenderQueueHarnessResult`;
- harness con actor/object visibili sulla stessa cella;
- harness con actor hidden per sprite key mancante;
- harness con object hidden per held object;
- harness con object hidden per sprite key mancante;
- verifica contatori actor/object/visible/hidden;
- verifica ordine oggetto prima di actor sulla stessa cella;
- nessuna scena;
- nessun asset load;
- nessuna lettura `World`.

Indicazione per `v0.34g`:

Chiudere QA e closeout della `v0.34`, aggiornare roadmap/taskboard/Notion e preparare `v0.35`.

Esito v0.34g:

- `v0.34` completata nel perimetro passivo;
- prodotte queue actor/object value-only;
- prodotto harness smoke senza scena;
- nessun `GameObject`;
- nessun `SpriteRenderer`;
- nessun asset load;
- nessuna modifica a scene;
- nessuna modifica a Core/MapGrid;
- QA scope superata;
- `v0.35` preparata come prossimo macro checkpoint.

Indicazione per `v0.35`:

La prossima versione dovra' collegare il movimento multi-tick reale agli snapshot visuali actor, ma solo tramite contratto read-only.

Vietato:

- chiamare `SetNpcPos`;
- completare job dalla view;
- interrompere running action dalla view;
- correggere pathfinding dalla view.

Decisioni operative v0.35:

- il movimento multi-tick reale e' nel Job Layer;
- `MoveToRunningActionDriver` conosce origine e destinazione del passo mentre esegue;
- `RunningActionProgressSnapshot` espone elapsed/required tick ma non origine/destinazione;
- ArcGraph non deve parsare `ActionInstanceId`;
- ArcGraph non deve dedurre movimento dalla differenza tra celle successive;
- serve un contratto read-only tipizzato.

Checkpoint v0.35:

| Checkpoint | Task | Stato |
|---|---|---|
| v0.35a | Audit movimento multi-tick e dati read-only disponibili | Completato con stop progettuale |
| v0.35b | Scelta contratto read-only del segmento movimento | Confermata |
| v0.35c | Implementazione contratto motion read-only | Completato |
| v0.35d | Integrazione adapter ArcGraph actor motion | Completato |
| v0.35e | Harness motion actor senza scena | Completato tramite test EditMode |
| v0.35f | QA e closeout v0.35 | Completato |

Esito v0.35a:

- `RunningActionStore.GetSnapshots()` espone snapshot value-only;
- `RunningActionProgressSnapshot` contiene `NpcId`, `JobId`, `Kind`, `ElapsedTicks`, `RequiredTicks`, `Status`;
- `MoveToRunningActionDriver` crea running action movement per il passo corrente;
- `MoveToRunningActionDriver` conosce `npcCell` e `action.TargetCell`;
- questi valori non sono presenti in un metadata tipizzato;
- il nome `ActionInstanceId` contiene from/to, ma non deve essere usato come contratto.

Stop progettuale:

Scegliere come esporre origine/destinazione del segmento movimento.

Opzione consigliata:

```text
aggiungere metadata movement tipizzato dentro RunningActionRuntimeState
e propagarlo nello snapshot read-only.
```

Motivo:

- evita parsing stringhe;
- mantiene authority nel Job Layer;
- offre ad ArcGraph un input read-only;
- non consente alla view di mutare posizione o job.

Decisione operatore v0.35b:

```text
Confermo aggiungere metadata movement tipizzato dentro RunningActionRuntimeState
e propagarlo nello snapshot read-only.

Mantieni strutture dati piu' agili possibile per non stressare la cpu.
```

Forma implementativa scelta:

- `RunningActionMovementSnapshot` value type minimale con flag presenza e coordinate intere from/to;
- `RunningActionRuntimeState.StartMovement(...)` come factory dedicata;
- `RunningActionProgressSnapshot.Movement` come propagazione read-only;
- indice `npcId -> RunningActionKey` dentro `RunningActionStore` per evitare scansioni per actor;
- `ArcGraphWorldAdapter.FillActorSnapshots(...)` legge solo `TryGetActiveMovementSnapshotForNpc(...)`;
- fallback stabile a `ArcGraphActorMotionSnapshot.None(...)` quando non esiste movimento attivo.

Esito v0.35f:

- `RunningActionMovementSnapshot` aggiunto come metadata value-only;
- `RunningActionRuntimeState.StartMovement(...)` aggiunto per movement action;
- `RunningActionProgressSnapshot` propaga `Movement`;
- `MoveToRunningActionDriver` crea running action movement con from/to tipizzati;
- `RunningActionStore` mantiene indice `npcId -> RunningActionKey` per lookup O(1);
- `ArcGraphWorldAdapter` traduce movement runtime in `ArcGraphActorMotionSnapshot`;
- actor senza movimento restano su `ArcGraphActorMotionSnapshot.None(...)`;
- test EditMode aggiunti:
  - `MovementFactoryPropagatesTypedSegmentIntoSnapshot`;
  - `ActiveMovementLookupUsesTypedMetadataAndClearsIndex`;
- nessuna nuova chiamata runtime vietata in ArcGraph/Core;
- nessuna modifica a `.meta`, `Library`, `Temp`, `Obj`;
- MapGrid resta renderer produttivo.

Prossimo checkpoint:

`v0.36 - ArcGraph Environment Visual Layers`

La prossima fase non deve ancora implementare luci, pioggia, neve, acqua o vegetazione produttive. Il primo step deve essere un audit/contratto preparatorio dei layer ambientali visuali.

Esito audit v0.33a:

- `MapGridCameraController` gestisce oggi zoom/pan legacy;
- il legacy usa zoom rotellina, PixelPerfectCamera e fallback orthographic;
- il legacy usa pan con tasto destro, non con rotellina premuta;
- il legacy e' legato a `MapGridData` e `MapGridConfig`;
- `MapGridWorldView` contiene conversioni mouse/camera/cella per overlay e debug;
- `MapGridPointerInputActionsProvider` espone solo posizione puntatore;
- ArcGraph non possiede ancora ViewController, stato viewport, zoom state o input view dedicato.

Indicazione per `v0.33b`:

Definire un contratto ArcGraph View/Camera separato da TerrainRenderer.

Formula:

```text
ArcGraphMapViewConfig
+ ArcGraphZoomProfile
+ ArcGraphViewState
+ input mouse
+ Camera esplicita
-> ArcGraphViewController
```

Esito v0.33b:

- aggiunto `ArcGraphViewZoomLevelDefinition`;
- aggiunto `ArcGraphMapViewConfig`;
- aggiunto `ArcGraphViewCellRect`;
- aggiunto `ArcGraphViewInputFrame`;
- aggiunto `ArcGraphViewState`;
- nessun `MonoBehaviour`;
- nessuna lettura diretta `Mouse.current`;
- nessuna lettura diretta `Camera.main`;
- nessun `GameObject`;
- nessun renderer;
- nessuna mutazione simulativa.

Indicazione per `v0.33c`:

Definire una configurazione serializzabile per mappa e zoom.

Formula:

```text
JSON config mappa
-> DTO config view
-> ArcGraphMapViewConfig
-> ArcGraphViewState iniziale
```

Esito v0.33c:

- aggiunto `ArcGraphMapViewConfigJson`;
- aggiunto `ArcGraphMapViewConfigDto`;
- aggiunto `ArcGraphZoomLevelConfigDto`;
- aggiunto `Assets/Resources/ArcGraph/Config/ArcGraphViewConfig.json`;
- nessun `Resources.Load` dentro ArcGraph;
- parsing da stringa JSON tramite `JsonUtility`;
- fallback a `ArcGraphMapViewConfig.CreateDefaultV033()`;
- nessun aggancio scena;
- nessuna modifica a MapGrid/Core.

Indicazione per `v0.33d`:

Usare:

```text
ArcGraphMapViewConfig
ArcGraphViewState
ArcGraphViewInputFrame
```

per produrre un controller pan/zoom ancora passivo o testabile, prima del bridge reale con camera Unity.

Esito v0.33d:

- aggiunto `ArcGraphViewController`;
- aggiunto `ArcGraphViewControllerResult`;
- aggiunto `ArcGraphViewControllerHarness`;
- zoom applicato prima del pan;
- pan convertito da pixel a celle;
- input sopra UI ignorato;
- harness smoke eseguito fuori Unity;
- nessuna camera Unity;
- nessun input Unity diretto;
- nessun renderer.

Indicazione per `v0.33e`:

Definire conversione coordinate passiva:

```text
screen pixel
-> viewport normalized
-> visible cell rect
-> cella ArcGraph
```

Esito v0.33e:

- aggiunto `ArcGraphViewCoordinateResult`;
- aggiunto `ArcGraphViewCoordinateMapper`;
- aggiunto `ArcGraphViewCoordinateMapperHarness`;
- convenzione viewport basso/sinistra;
- bordo destro/alto esclusivo;
- harness smoke eseguito fuori Unity;
- nessuna camera Unity;
- nessun input Unity diretto.

Indicazione per `v0.33f`:

Definire policy LOD per zoom:

```text
zoom level
-> animazioni abilitate/disabilitate
-> sprite layered abilitati/disabilitati
-> rappresentazione semplificata
-> layer minori visibili/nascosti
```

Esito v0.33f:

- aggiunto `ArcGraphZoomLodModes`;
- aggiunto `ArcGraphZoomLodProfile`;
- aggiunto `ArcGraphZoomLodPolicy`;
- aggiunto `ArcGraphZoomLodPolicyHarness`;
- zoom 1: marker strategici, aree aggregate, niente animazioni;
- zoom 2: sprite statici semplificati, niente animazioni;
- zoom 3: sprite completi flat, animazioni ammesse;
- zoom 4: actor layered e dettagli locali;
- harness smoke eseguito fuori Unity;
- nessun renderer modificato.

Indicazione per `v0.33g`:

Implementare solo il perimetro sicuro della modalita comparativa:

```text
contratto debug/test
diagnostica comparativa
nessun doppio renderer permanente
nessun aggancio produttivo automatico
```

Esito v0.33g:

- aggiunto `ArcGraphComparisonMode`;
- aggiunto `ArcGraphComparisonOptions`;
- aggiunto `ArcGraphComparisonDiagnostics`;
- aggiunto `ArcGraphComparisonGate`;
- aggiunto `ArcGraphComparisonGateHarness`;
- diagnostics-only ammessa senza scena;
- doppio renderer permanente bloccato;
- scene probe temporaneo ammesso solo con prerequisiti dichiarati;
- nessun aggancio scena implementato.

Debito esplicito:

Il bridge scena comparativo reale richiede decisione su:

- parent GameObject debug;
- materiale/atlas;
- sorting;
- camera;
- modalita sovrapposta/affiancata/alternata;
- spegnimento completo.

Esito v0.33h:

- QA finale `v0.33` completata sul perimetro di closeout;
- compilazione Unity completa non rieseguita in `v0.33h`, perche' il build da `.csproj` richiede restore in `Temp`;
- diff scope verificato rispetto a `v0.32h`;
- chiamate operative vietate assenti nel codice ArcGraph runtime, salvo menzioni descrittive nei commenti;
- nessuna modifica a Core, MapGrid, scene Unity, `.meta`, `Library`, `Temp`, `Obj`;
- roadmap aggiornata con `v0.33` completata nel perimetro sicuro;
- `v0.34` preparata come prossimo macro checkpoint.

Definition of Done v0.33:

- audit view/camera legacy;
- contratto ArcGraph View/Camera;
- configurazione JSON mappa/zoom;
- controller pan/zoom passivo;
- mapper coordinate viewport/cella;
- policy LOD per zoom;
- gate comparativo diagnostico;
- closeout documentale.

Nota critica:

`v0.33` non implementa ancora un bridge scena reale.

Questa scelta e' intenzionale: il bridge richiede decisioni esplicite su camera, parent GameObject, materiale/atlas, sorting, modalita' visuale e spegnimento completo.

Indicazione per `v0.34`:

Partire da actor/object renderer in forma passiva:

- lettura `ArcGraphActorLayer`;
- lettura `ArcGraphObjectLayer`;
- produzione dati renderizzabili;
- rispetto della policy LOD;
- nessun aggancio scena permanente senza decisione esplicita.

---

## Closeout v0.32 - ArcGraph Terrain Renderer

STATUS:
COMPLETATA

Consolidato:

- audit terrain legacy `MapGridChunkRenderer`;
- UV map ArcGraph autonoma;
- policy terrain compatibile con legacy floor/wall/wall-top;
- builder passivo di mesh data per chunk;
- rebuild da dirty chunk;
- harness statico compilabile;
- QA di compilazione e scope diff.

Impatto:

`v0.32` abilita ArcGraph a produrre dati mesh terrain a partire da snapshot e dirty chunk.

Non abilita ancora:

- creazione di `GameObject`;
- creazione di `MeshRenderer` / `MeshFilter`;
- aggancio scena;
- sostituzione MapGrid;
- doppio renderer permanente.

Debiti rinviati a `v0.33`:

- montaggio controllato della mesh ArcGraph in debug/test;
- confronto visuale con MapGrid;
- verifica scala, coordinate, sorting e camera;
- policy di accensione/spegnimento sicura;
- gestione materiali/atlas come input esterni controllati.

---

# 1. Stato tecnico ereditato

## v0.16 — Cognizione Soggettiva Avanzata

STATUS:
COMPLETATA

Consolidato:

- lifecycle delle credenze `Active`, `Weak`, `Stale`, `Discarded`;
- verifica locale e scarto delle credenze cibo smentite;
- memoria soggettiva da eventi needs;
- comunicazione soggettiva minima;
- SearchFood esplorativo iniziale;
- QA anti-onniscienza.

## v0.17 — Osservatorio costi runtime

STATUS:
STRUMENTO PRODUTTIVO / CLOSEOUT FORMALE PARZIALE

Consolidato:

- osservatorio congelabile con costo quasi nullo quando spento;
- misure per sistema e per NPC;
- contatori operativi;
- esportazione JSONL opzionale e limitata;
- protocollo di confronto scalare.

Residuo:

- completare formalmente le prove scalari e il closeout documentale.

## v0.18 — Ottimizzazione runtime percezione / belief / query

STATUS:
IMPLEMENTAZIONE PRINCIPALE COMPLETATA / CLOSEOUT FORMALE PARZIALE

Consolidato:

- indice spaziale per percezione oggetti;
- scansione di celle occupate;
- indice NPC per cella e zona;
- query belief indicizzate per categoria;
- decadimento belief discreto per categoria;
- sottoinsiemi di intenzioni valutabili;
- riduzione allocazioni decisione/query/EL.

Residuo:

- il costo dominante più recente si è spostato verso Decisione → Incarico;
- resta necessario un confronto scalare aggiornato.

## v0.20 — Rifondazione percettiva strutturale

STATUS:
COMPLETATA FINO A `v0.20q`

Consolidato:

- indici persistenti compatti per oggetti e NPC;
- dirty percettivo conservativo;
- separazione `watched` / `observed`;
- stati percettivi configurabili;
- cadenza percettiva per stato;
- limite massimo NPC percettivi per tick;
- distribuzione deterministica del carico;
- percezione oggetti, NPC e landmark sulla stessa selezione;
- movimento e rotazione come sorgenti dirty;
- pulizia dirty centralizzata;
- pensionamento dello scan idle automatico;
- osservazione direzionale tramite Job e Step `LookDirection`;
- stato percettivo dichiarabile nelle fasi Job;
- catalogo dedicato dei pesi delle intenzioni.

---

# 2. Checkpoint corrente v0.31

## v0.31 - ArcGraph Bootstrap controllato

STATUS:
COMPLETATA

Obiettivo:

Accendere `arcgraph` come sistema interno controllato, senza render produttivo e senza sostituire ancora `MapGrid`.

Componenti da valutare:

- punto di bootstrap: nuovo componente dedicato, estensione controllata del bootstrap esistente o test harness separato;
- lifecycle di `ArcGraphRenderState`;
- lifecycle di `ArcGraphLayerStack`;
- registrazione layer foundation;
- uso di `ArcGraphWorldAdapter`;
- eventuali buffer snapshot riusabili;
- policy di attivazione/disattivazione;
- relazione con `MapGridBootstrap` e `MapGridWorldView`.

Domande aperte:

1. Quali criteri della Definition of Done v0.31 sono soddisfatti?
2. Quali debiti restano per v0.32?
3. Quale branch/checkpoint deve aprire il terrain renderer?
4. Quale documentazione va allineata a fine macro job?
5. La fase v0.31 puo' essere dichiarata completata?

Vincoli:

- nessuna modifica a Decision Layer, Job Layer o Core;
- nessuna sostituzione immediata di `MapGridBootstrap`;
- nessuna cancellazione legacy;
- nessun doppio renderer permanente;
- nessun accesso globale non necessario;
- niente modifica codice senza prossimo `go` operativo.

Esito audit `v0.31a`:

1. `MapGridBootstrap` e' il punto unico che costruisce il runtime grafico legacy: config, layout, `MapGridData`, atlas, chunk terrain, camera, input provider e `MapGridWorldView`.
2. `MapGridData` nasce dentro `MapGridBootstrap` e resta privato; viene passato a `MapGridChunkRenderer` e `MapGridCameraController`, ma non a `MapGridWorldView`.
3. `MapGridWorldView` riceve solo `MapGridConfig` e legge il `World` tramite `MapGridWorldProvider.TryGetWorld()`, che a sua volta usa `SimulationHost.Instance`.
4. `ArcGraphWorldAdapter` e' gia' compatibile con `MapGridData` e `World`, ma non ha ancora un runtime context pulito da cui riceverli.
5. Agganciare ArcGraph direttamente dentro `MapGridWorldView` aumenterebbe il monolite legacy; far leggere `SimulationHost` a ogni layer ArcGraph rischierebbe un nuovo accesso globale diffuso.

Conclusione operativa:

```text
v0.31b deve definire il contratto di bootstrap
prima di scegliere se implementarlo come servizio C#,
wrapper Unity minimo o harness debug.
```

Esito `v0.31b`:

1. `ArcGraphBootstrap` deve essere un confine di accensione interna, non un renderer.
2. Deve poter creare `ArcGraphRenderState`, `ArcGraphLayerStack`, `ArcGraphWorldAdapter`, layer foundation e buffer snapshot interni.
3. Deve registrare solo `Terrain`, `Object`, `Actor`, `Debug`.
4. Non deve creare `GameObject`, `SpriteRenderer`, `MeshRenderer`, mesh, asset load, input, camera o comandi runtime.
5. Non deve leggere `SimulationHost.Instance` direttamente e non deve mutare `World` o `MapGridData`.
6. Deve esporre diagnostica minima: `IsInitialized`, `LayerCount`, presenza di render state/layer stack/adapter/runtime context, ultimo stato e ultima ragione.
7. La forma concreta del bootstrap resta da decidere in `v0.31c`.

Contratto sintetico:

```text
ArcGraphBootstrap possiede solo il lifecycle ArcGraph.
Non possiede il mondo.
Non possiede la mappa.
Non possiede la camera.
Non possiede l'input.
Non disegna.
```

Esito `v0.31c`:

1. La forma scelta per il primo bootstrap e' un nucleo C# passivo.
2. Il nome concettuale del nucleo e' `ArcGraphBootstrapRuntime`.
3. Il wrapper Unity viene rinviato: potra' esistere solo come adattatore leggero e non come fonte primaria del lifecycle.
4. L'estensione diretta di `MapGridBootstrap` e' scartata per v0.31, per non aumentare il coupling legacy.
5. Un harness debug puo' aiutare i test, ma non diventa la forma primaria del sistema.

Decisione:

```text
v0.31f implementera' prima il nucleo C# passivo.
Nessun aggancio automatico alla scena e nessun renderer produttivo.
```

Esito `v0.31d`:

1. ArcGraph riceve i dati tramite `ArcGraphRuntimeContext` esplicito.
2. Il context puo' contenere `MapGridConfig`, `MapGridData` e `World`.
3. `ArcGraphBootstrapRuntime` non deve chiamare `SimulationHost.Instance`, `FindObjectOfType` o `MapGridWorldView`.
4. I layer non leggono il context: ricevono solo snapshot.
5. `MapGridData` resta mutabile a livello classe, ma in v0.31 viene trattato per contratto come sorgente di sola lettura.
6. Il bootstrap deve tollerare context parziale o assente con diagnostica, senza fallimento distruttivo.

Decisione:

```text
ArcGraph non cerca dati.
ArcGraph riceve dati.
ArcGraph copia in snapshot.
ArcGraph non muta sorgenti.
```

Esito `v0.31e`:

1. Nessuna attivazione automatica in scena.
2. Modalita' consentita in v0.31: `InternalStateOnly`.
3. Modalita' vietate in v0.31: terrain render, actor render, object render, modalita' comparativa, sostituzione MapGrid.
4. Default: layer foundation attivi, placeholder futuri esclusi, context parziale tollerato, snapshot iniziali ammessi solo come copie interne.
5. Garanzia anti-doppio-renderer: nessun `MonoBehaviour` produttivo, nessun `GameObject`, nessun renderer Unity, nessun asset load, nessuna modifica scena.

Decisione:

```text
v0.31f puo' implementare il nucleo C# passivo.
Il bootstrap si accende solo con chiamata esplicita.
Il rendering resta sempre spento.
```

Esito `v0.31f`:

1. Aggiunto nucleo C# passivo `ArcGraphBootstrapRuntime`.
2. Aggiunti contratti: activation mode, status, options, runtime context, diagnostics.
3. Il runtime puo' creare render state, layer stack, adapter e snapshot interni.
4. Il runtime non e' un `MonoBehaviour`, non crea GameObject, non carica asset e non crea renderer Unity.
5. La compilazione isolata della cartella ArcGraph runtime e' riuscita.

Nuovi file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphBootstrapActivationMode.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphBootstrapStatus.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphBootstrapOptions.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRuntimeContext.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphBootstrapDiagnostics.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphBootstrapRuntime.cs
```

Esito `v0.31g`:

1. Compilazione isolata dell'intera cartella ArcGraph runtime riuscita.
2. Diff limitato a documenti root operativi e `Assets/Scripts/Views/ArcGraph/Runtime`.
3. Nessuna modifica a `Assets/Scripts/Core`, `Assets/Scripts/Views/MapGrid`, scene, `.meta`, `Library`, `Temp`, `Obj`.
4. Nessuna chiamata operativa vietata rilevata.
5. Nessun rendering produttivo introdotto.

Esito `v0.31h`:

1. Definition of Done `v0.31` completata.
2. Roadmap aggiornata: `v0.31` completata, `v0.32` resta prossimo macro checkpoint.
3. `ArcGraphBootstrapRuntime` resta nucleo C# passivo, non agganciato automaticamente alla scena.
4. MapGrid resta renderer visibile.
5. Il prossimo lavoro deve concentrarsi sul terrain renderer chunked, non su altri layer ambientali.

---

# 3. Stato ereditato v0.30

| Checkpoint | Task | Stato |
|---|---|---|
| v0.30a | Audit rendering attuale: MapGrid, chunk terrain, WorldView, SpriteRenderer, overlay, asset e accoppiamenti | ✅ COMPLETATO / PUSHATO |
| v0.30b | Definizione contratti minimi `arcgraph`: coordinate x/y/z, layer id, render state, dirty state | ✅ COMPLETATO / PUSHATO |
| v0.30c | Adapter read-only verso World / MapGrid corrente e primo confine anti-omniscienza grafica | ✅ COMPLETATO / PUSHATO |
| v0.30d | Layer grafici minimi attivi: Terrain, Object, Actor, Debug | ✅ COMPLETATO / PUSHATO |
| v0.30e | Dirty cell / dirty chunk preparatorio, senza ottimizzazione aggressiva | ✅ COMPLETATO / PUSHATO |
| v0.30f | Compatibilita' z-level preparatoria: firme x/y/z con rendering operativo solo su z = 0 | ✅ COMPLETATO / PUSHATO |
| v0.30g | ActorVisual preparatorio: sprite singolo attuale, progress multitick e interpolazione visiva tra celle | ⏳ IN CORSO |
| v0.30h | Placeholder layer futuri: Water, Vegetation, Light, Weather, Effect | ⏳ PENDING |
| v0.30i | Piano di assorbimento e futura eliminazione legacy grafico, senza doppio renderer permanente | ⏳ PENDING |
| v0.30j | QA regressiva visuale e closeout ArcGraph Foundation | ⏳ PENDING |

Note operative:

- closeout stato tabella: `v0.30g`, `v0.30h`, `v0.30i` e `v0.30j` sono completati e pushati; eventuali simboli corrotti nelle righe storiche vanno letti come residuo di codifica, non come stato operativo corrente;
- aggiornamento stato: `v0.30g` completato e pushato con commit `ecf20c3`; `v0.30h` aperto e in corso su branch `ai-task/v0.30h-arcgraph-future-placeholders`;
- aggiornamento stato: `v0.30h` completato e pushato con commit `4fbbd8f`; `v0.30i` aperto e in corso su branch `ai-task/v0.30i-arcgraph-legacy-absorption-plan`;
- aggiornamento stato: `v0.30i` completato e pushato con commit `0bc79a0`; `v0.30j` aperto e in corso su branch `ai-task/v0.30j-arcgraph-foundation-closeout`;
- aggiornamento stato: `v0.30j` QA completata e pronta per push closeout;
- branch task corrente: `ai-task/v0.30j-arcgraph-foundation-closeout`;
- base di integrazione: `ai/codex-main`;
- branch `ai-task/v0.30-arcgraph-foundation` pushato con commit `d482cdc`;
- branch `ai-task/v0.30b-arcgraph-contracts` pushato con commit `2495135`;
- branch `ai-task/v0.30c-arcgraph-adapter` pushato con commit `01273d4`;
- branch `ai-task/v0.30d-arcgraph-minimal-layers` pushato con commit `b6d912f`;
- branch `ai-task/v0.30e-arcgraph-dirty-state` pushato con commit `a5ebf28`;
- branch `ai-task/v0.30f-arcgraph-z-level-compat` pushato con commit `7b3c106`;
- branch `ai-task/v0.30g-arcgraph-actor-visual` pushato con commit `ecf20c3`;
- branch `ai-task/v0.30h-arcgraph-future-placeholders` pushato con commit `4fbbd8f`;
- branch `ai-task/v0.30i-arcgraph-legacy-absorption-plan` pushato con commit `0bc79a0`;
- `arcgraph` deve sostituire il rendering provvisorio a regime, non diventare un secondo renderer permanente;
- il checkpoint corrente e' chiuso: niente nuove feature grafiche o rimozioni legacy senza nuovo macro checkpoint approvato;
- `main` resta il ramo stabile e non deve ricevere lavoro implementativo diretto.

---

# 3. Nodi aperti prioritari

## Stabilità percettiva e SearchFood

- verificare che `WaitAndObserve` produca percezioni valide durante i quattro orientamenti;
- verificare che SearchFood non resti bloccato su landmark o percorsi non risolvibili;
- verificare il comportamento dei margini watched FOV sui quattro lati;
- verificare che il rinforzo mnemonico cadenzato conservi la semantica prevista.

## Costo Decisione → Incarico

- misurare perché il costo Decisione è diventato il nuovo punto dominante;
- verificare la produzione frequente di incarichi brevi;
- distinguere decisioni necessarie, decisioni duplicate e incarichi equivalenti già attivi;
- evitare ottimizzazioni che alterino lo score o la causalità MBQD.

## Cataloghi runtime

- completare il censimento degli Intent implementati e progettati;
- distinguere Job di alto livello, sottopiani riusabili, primitive esecutive e recuperi;
- distinguere Step realmente produttivi da Step dichiarativi o incompleti;
- distinguere Running Action operative da categorie preparatorie;
- chiarire il contratto futuro di composizione senza introdurre Job annidati implicitamente.

## Debiti strutturali rinviati

- isolamento finale di `MoveIntent` e `MovementSystem` come sviluppo/compatibilità;
- strategie di recupero non ancora produttive;
- query food multi-candidato;
- incapsulamento progressivo degli store pubblici del `World`;
- rimozione futura di `Telemetry` dalle firme runtime;
- sistemi lavoro e sociali produttivi.

---

# 4. Prossimo gate di validazione umana

Esito audit `v0.30i`:

1. Le classi legacy indispensabili oggi sono `MapGridBootstrap`, `MapGridData`, `MapGridChunkRenderer`, `MapGridWorldView`, `MapGridCameraController`, `MapGridPointerInputActionsProvider` e gli overlay/debug collegati.
2. `MapGridWorldView` non e' solo renderer: contiene sync NPC/oggetti, sprite cache, stock label, balloon, flash decisionale, input debug, click-to-move, selection, FOV, landmark, DT overlay, summary UI, dev tools, audio feedback e rebind del `World`.
3. `MapGridChunkRenderer` e `MapGridTileAtlas` sono riusabili come tecnica: mesh chunked, atlas regolare, UV mapping, varianti deterministiche di pavimento.
4. `MapGridData` va trattato come buffer view-side temporaneo, utile per produrre snapshot terreno ArcGraph, ma non deve diventare il modello mappa definitivo.
5. Gli overlay diagnostici vanno assorbiti dopo terrain/actor/object, perche' leggono molti dettagli del `World` e sono piu' fragili del rendering base.

Matrice di assorbimento:

| Area legacy | Stato attuale | Destino ArcGraph | Regola v0.30i |
|---|---|---|---|
| `MapGridBootstrap` | Costruisce config, layout, terrain chunk, camera, pointer provider e `MapGridWorldView` | Da sostituire con bootstrap ArcGraph futuro | Non cancellare ora |
| `MapGridData` | Buffer view-side 2D terrain/blocked | Sorgente temporanea per `ArcGraphWorldAdapter.FillTerrainSnapshots` | Assorbire, poi pensionare |
| `MapGridChunkRenderer` | Mesh chunk terreno con atlas | Tecnica da reimplementare come renderer terrain ArcGraph | Riusare logica, non dipendenza diretta permanente |
| `MapGridTileAtlas` | Mapping tileId -> UV | Servizio o helper asset del terrain renderer ArcGraph | Riusabile |
| `MapGridWorldView` | Monolite rendering + input/debug + overlay | Da spezzare in renderer actor/object, overlay, input/debug bridge | Non cancellare prima di moduli sostitutivi |
| `MapGridCameraController` | Camera orthographic, pan, zoom, pixel perfect | Camera controller riusabile o adattabile | Separare da `MapGridData` quando ArcGraph avra' bounds proprie |
| Overlay FOV/Landmark/DT/Summary | Diagnostica visuale dipendente dal `World` | Debug/Observer layer futuri | Rimandare dopo rendering base |
| DevTools/click-to-move | Strumenti runtime che emettono comandi/debug | Tool separato, non renderer | Non inglobare nel renderer ArcGraph |

Sequenza futura consigliata:

1. Creare un bootstrap ArcGraph parallelo ma non permanente, disattivato di default o agganciato solo in test controllato.
2. Portare il terrain renderer in ArcGraph usando snapshot terreno, chunk sporchi, atlas e tecnica mesh chunked.
3. Portare object/actor renderer in ArcGraph usando `ArcGraphObjectLayer` e `ArcGraphActorLayer`; per actor fluido serve prima il contratto read-only origine/destinazione movimento.
4. Separare input/debug tools dal renderer: selection, hover, click-to-move e dev tools non devono vivere nel main renderer.
5. Portare overlay diagnostici uno alla volta: FOV, landmark, DT, summary cards.
6. Solo quando ArcGraph copre terrain + actor + object + debug minimo, pensionare `MapGridWorldView` e poi `MapGridBootstrap`.

Divieti operativi per il prossimo step:

- non eliminare `MapGridWorldView`;
- non eliminare `MapGridBootstrap`;
- non cambiare il bootstrap scena senza test visuale;
- non attivare due renderer permanenti;
- non spostare dev tools dentro il renderer ArcGraph;
- non trasformare `MapGridData` nella futura mappa simulativa.

Esito QA `v0.30j`:

1. Compilazione isolata dei file ArcGraph riuscita con Roslyn contro `Library/ScriptAssemblies/Assembly-CSharp.dll`.
2. Diff complessiva rispetto a `ai/codex-main`: solo `ARCONTIO_Roadmap.md`, `TASKBOARD_CODEX.md` e nuovi file in `Assets/Scripts/Views/ArcGraph/Runtime`.
3. Nessun file `Assets/Scripts/Core`, `Assets/Scripts/Views/MapGrid`, `.meta`, `Library`, `Temp` o `Obj` modificato da `v0.30`.
4. Nessuna chiamata operativa vietata in ArcGraph: niente `SetNpcPos`, niente `Command`, niente `MonoBehaviour`, niente `new GameObject`, niente `Resources.Load`.
5. `arcgraph` non e' ancora renderer produttivo e non viene registrato nel bootstrap scena: non esiste doppio renderer permanente.

Stato pronto:

- contratti coordinate x/y/z e chunk;
- `ArcGraphRenderState` e dirty state;
- layer passivi Terrain/Object/Actor/Debug;
- adapter read-only verso `World` e `MapGridData`;
- policy z-level centralizzata con runtime attuale su `z = 0`;
- posa actor interpolabile a livello grafico;
- placeholder passivi per Water/Vegetation/Light/Weather/Effect;
- piano di assorbimento del legacy MapGrid.

Debiti dichiarati:

- manca renderer terrain produttivo ArcGraph;
- manca bootstrap ArcGraph reale;
- manca collegamento actor motion a origine/destinazione runtime read-only;
- `MapGridWorldView` resta renderer produttivo legacy;
- overlay/debug restano nel mondo MapGrid;
- nessun sistema acqua, vegetazione, luci o meteo produttivo e' stato introdotto.

Prossima decisione umana:

- aprire PR/review della foundation `v0.30`;
- oppure avviare un nuovo macro checkpoint per primo renderer terrain ArcGraph;
- oppure congelare `v0.30` e tornare a un altro macro tema della roadmap.

---

# 5. Campagne future congelate

Le seguenti campagne non devono partire automaticamente:

- `v0.170` — Conseguenze Sociali Emergenti;
- `v0.180` — Observer Layer Pubblico ed Explainability Esterna;
- ampliamento sistemi lavoro e produzione;
- planner globale;
- recovery intelligente completa;
- composizione gerarchica dei Job senza contratto approvato.

La priorita' resta:

```text
v0.30a audit rendering attuale completato
-> v0.30b contratti minimi arcgraph completati
-> v0.30c adapter read-only completato
-> v0.30d layer grafici minimi completati
-> v0.30e dirty cell / dirty chunk preparatorio completato
-> v0.30f compatibilita' z-level preparatoria completata
-> v0.30g ActorVisual preparatorio completato
-> v0.30h placeholder layer futuri completato
-> v0.30i piano assorbimento legacy grafico completato
-> v0.30j QA regressiva visuale e closeout ArcGraph Foundation completato
-> in attesa di decisione umana: PR/review o nuovo macro checkpoint
```

---

# 6. Verità workflow repository

Branch stabile:
`main`

Branch integrazione AI:
`ai/codex-main`

Pattern predefinito branch task Codex:
`ai-task/v0.xx-short-description`

Target integrazione predefinito:
`ai/codex-main`

Chiusura standard:
`ai-task branch -> ai/codex-main -> main`

Non assumere mai implementazione diretta iniziale su `main`.

Più micro-step coerenti appartenenti allo stesso checkpoint possono restare sul medesimo branch task fino alla chiusura del blocco.

Aprire nuovo branch task quando:

- cambia checkpoint;
- cambia dominio tecnico;
- il diff richiede isolamento di merge.

---

# 7. Stato repository attualmente noto

Confermato:

- `ai/codex-main` locale allineato a `origin/ai/codex-main` sul commit `df7f211`;
- branch task `ai-task/v0.30-arcgraph-foundation` aperto da `ai/codex-main`;
- branch task `ai-task/v0.30-arcgraph-foundation` pushato su origin con commit `d482cdc`;
- branch task `ai-task/v0.30b-arcgraph-contracts` aperto da `ai-task/v0.30-arcgraph-foundation`;
- branch task `ai-task/v0.30b-arcgraph-contracts` pushato su origin con commit `2495135`;
- branch task `ai-task/v0.30c-arcgraph-adapter` aperto da `ai-task/v0.30b-arcgraph-contracts`;
- branch task `ai-task/v0.30c-arcgraph-adapter` pushato su origin con commit `01273d4`;
- branch task `ai-task/v0.30d-arcgraph-minimal-layers` aperto da `ai-task/v0.30c-arcgraph-adapter`;
- branch task `ai-task/v0.30d-arcgraph-minimal-layers` pushato su origin con commit `b6d912f`;
- branch task `ai-task/v0.30e-arcgraph-dirty-state` aperto da `ai-task/v0.30d-arcgraph-minimal-layers`;
- branch task `ai-task/v0.30e-arcgraph-dirty-state` pushato su origin con commit `a5ebf28`;
- branch task `ai-task/v0.30f-arcgraph-z-level-compat` aperto da `ai-task/v0.30e-arcgraph-dirty-state`;
- branch task `ai-task/v0.30f-arcgraph-z-level-compat` pushato su origin con commit `7b3c106`;
- branch task `ai-task/v0.30g-arcgraph-actor-visual` aperto da `ai-task/v0.30f-arcgraph-z-level-compat`;
- branch task `ai-task/v0.30g-arcgraph-actor-visual` pushato su origin con commit `ecf20c3`;
- branch task `ai-task/v0.30h-arcgraph-future-placeholders` aperto da `ai-task/v0.30g-arcgraph-actor-visual`;
- branch task `ai-task/v0.30h-arcgraph-future-placeholders` pushato su origin con commit `4fbbd8f`;
- branch task `ai-task/v0.30i-arcgraph-legacy-absorption-plan` aperto da `ai-task/v0.30h-arcgraph-future-placeholders`;
- branch task `ai-task/v0.30i-arcgraph-legacy-absorption-plan` pushato su origin con commit `0bc79a0`;
- branch task corrente `ai-task/v0.30j-arcgraph-foundation-closeout` aperto da `ai-task/v0.30i-arcgraph-legacy-absorption-plan`;
- branch task corrente `ai-task/v0.31-arcgraph-bootstrap-analysis` aperto da `ai-task/v0.30j-arcgraph-foundation-closeout`;
- `main` locale allineato a `origin/main` sul commit `8ca3af0`;
- PR #131 integrata su `ai/codex-main`;
- PR #132 integrata su `main` per il bootstrap analisi/audit;
- rami temporanei `ai-task/v0.21-post-merge-fixes` e `ai-task/governance-bootstrap-main-sync` rimossi;
- nessun merge o rebase incompleto rilevato;
- `AI_SESSION_BOOT.md` allineato su `ai/codex-main` e `main`.

Da completare:

- eventuale PR/review del blocco `v0.31`;
- apertura operativa `v0.32` solo dopo nuovo `go`;
- decisione umana su avvio operativo `v0.32` Terrain Renderer;
- pulizia dei numerosi branch storici soltanto tramite campagna dedicata e autorizzata.

---

# 8. Comportamento obbligatorio Codex durante questo macro job

Durante `v0.31` Codex deve:

- restare audit-first sui cambiamenti grafici;
- non modificare Decision Layer, Job Layer o sistemi di simulazione salvo checkpoint esplicito;
- non trasformare `arcgraph` in fonte di verita' simulativa;
- non creare doppio renderer permanente;
- preservare il comportamento visivo attuale durante il bootstrap controllato;
- preparare coordinate x/y/z anche se il runtime opera ancora su z = 0;
- non aggiungere rendering produttivo prima del checkpoint dedicato;
- evitare pulizie opportunistiche fuori checkpoint;
- riportare separatamente ciò che è integrato e ciò che vive soltanto sul ramo task.

Ogni richiesta locale deve essere letta sotto:

`macro job attivo -> checkpoint corrente -> task locale richiesto`

e non come task isolato.

---

# 9. Reminder hook operatore

Se Codex completa un passaggio che modifica lo step cognitivo corrente, deve dichiarare esplicitamente:

`TASKBOARD/root update recommended.`

Se Codex completa un passaggio con reale valore documentale canonico, deve dichiarare esplicitamente:

`ARCONTIO_docs alignment recommended.`
