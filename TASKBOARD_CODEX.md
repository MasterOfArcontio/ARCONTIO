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

## MACRO JOB ATTIVO: v0.32 - ArcGraph Terrain Renderer

CHECKPOINT CORRENTE:
`v0.32c - Strategia atlas/materiali terrain ArcGraph`

STATUS:
IN ESECUZIONE AUTONOMA / CONTRATTO v0.32b DEFINITO

RAMO BASE CORRENTE:
`ai-task/v0.32c-arcgraph-terrain-atlas`

BASE DI INTEGRAZIONE:
`ai/codex-main`

OUTPUT ATTESO:

- definire e implementare il primo terrain renderer ArcGraph controllato;
- partire da snapshot terrain e chunk sporchi;
- non sostituire ancora MapGrid;
- non introdurre doppio renderer permanente;
- non toccare Core, Decision Layer o Job Layer.

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
- prossimo ramo operativo previsto: `ai-task/v0.32c-arcgraph-terrain-atlas`.

OBIETTIVO:

Definire e implementare la strategia atlas/materiali del terrain renderer ArcGraph: UV map autonoma, niente asset load, niente dipendenza permanente da `MapGridTileAtlas`.

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
