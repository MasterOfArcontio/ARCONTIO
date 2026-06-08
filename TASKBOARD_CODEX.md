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

## MACRO JOB ATTIVO: v0.37 - ArcGraph Debug/Overlay Migration

CHECKPOINT CORRENTE:
`v0.37m - ArcGraph Debug Runtime Scene Wrapper`

STATUS:
COMPLETATO / IN ATTESA GO v0.37n

RAMO BASE CORRENTE:
`ai-task/v0.37m-arcgraph-debug-runtime-scene-wrapper`

BASE DI INTEGRAZIONE:
`ai/codex-main`

OUTPUT ATTESO:

- chiudere `v0.37m` con wrapper scena passivo del wiring runtime debug ArcGraph;
- non implementare simulazione produttiva di meteo, temperatura, umidita', precipitazioni, incendi, acqua, vegetazione o luce;
- non creare renderer produttivi Unity, asset load o modifiche scena;
- mantenere MapGrid come renderer produttivo finche' non esiste decisione esplicita diversa;
- rispettare la policy LOD definita in `v0.33f`;
- non migrare strumenti interattivi o dev tools prima dell'audit mirato;
- collegare feed e renderer solo tramite context, NPC e consumer espliciti, senza accessi globali.

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
- `main`, `ai/codex-main` e branch task chiuso vengono allineati a fine step;
- eventuale ponte mappa reale andra' pianificato dopo la migrazione overlay o come micro-step esplicitamente approvato;
- non accumulare ulteriori moduli senza harness e diagnostica.

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
