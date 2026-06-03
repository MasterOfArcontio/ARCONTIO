# TASKBOARD_CODEX.md — Lavagna Operativa Attiva ARCONTIO

Questo file è la console persistente dei task attivi per Codex.

Codex deve leggere questo file prima di iniziare ogni implementazione.

Non assumere vecchie conversazioni come memoria di stato valida.
Usa questo file come verità operativa corrente.

Questo file costituisce la cabina di pilotaggio operativa del macro job AI/Codex attualmente autorizzato.

Non ha funzione di diario minuto né di tracker di micro-step conversazionali.

La Taskboard deve rappresentare:

- il macro cantiere in esecuzione;
- il checkpoint interno attualmente attraversato;
- la sequenza di step già pianificati del blocco;
- i nodi aperti;
- il prossimo gate di validazione umana.

L'unità primaria di governo non è il singolo micro-step, ma il macro job con i suoi checkpoint interni.

---

# 0. Stato operativo corrente

## MACRO JOB COMPLETED: v0.16 - Cognizione Soggettiva Avanzata

CHECKPOINT CORRENTE:
v0.16j - Closeout cognizione soggettiva avanzata

STATUS:
COMPLETED / READY FOR PR

OUTPUT ATTESO:
closeout della fase cognitiva soggettiva iniziale, con lifecycle belief, eventi needs in memoria, comunicazione soggettiva minima, decisioni su belief incerte, SearchFood esplorativo e QA anti-onniscienza consolidati

DOC SYNC:
root taskboard e roadmap aggiornate alla chiusura tecnica di `v0.16`;
allineamento esteso `ARCONTIO_docs` / Notion raccomandato prima dell'apertura stabile della fase sociale `v0.170`

OBIETTIVO:
Chiudere il debito tra movimento legacy e Job runtime prima di aprire la cognizione soggettiva avanzata.

Esito tecnico ereditato da `v0.12`:

- logging e diagnostica runtime consolidati;
- console, TXT, HTML e overlay logger legacy rimossi;
- `Telemetry` assorbita come ponte inerte quando disattiva;
- `ArcontioLogger` ridotto a ponte transitorio;
- `SimulationParams` e `GameParams` ricollocati sotto `Core/Config`;
- EL modulare reso piu' leggero quando disattivo;
- pannelli EL ottimizzati per ridurre produzione nascosta di stringhe, liste e viewmodel.

Esito tecnico ereditato da `v0.13`:

- rendere il percorso Decisione -> JobRequest -> Job il percorso ordinario;
- estrarre da `NeedsDecisionRule` solo gli helper ancora utili come servizi dedicati;
- scollegare `NeedsDecisionRule` come autorita' runtime;
- ricostruire i bisogni principali minimi sopra JobRequest e Job scriptati a fasi;
- introdurre configurazione fallback/recovery locale coerente con la matrice Job;
- classificare fallimenti minimi e ritorno cognitivo leggero;
- eliminare definitivamente `NeedsDecisionRule` dopo estrazione e ricostruzione dei percorsi minimi.

Esito tecnico ereditato da `v0.14`:

- verificare come falliscono oggi Job, Phase e Step;
- collegare le classificazioni di fallimento alle policy di recovery gia' introdotte;
- introdurre retry locale controllato senza creare loop;
- gestire fallback per target non valido, risorsa sparita, reservation negata e movimento bloccato;
- produrre ritorno minimo verso memoria/credenze quando l'incarico fallisce;
- mantenere `JobExecutionSystem` come esecutore, non come secondo decisore;
- rendere osservabile recovery/fallimento tramite EL.

Obiettivo tecnico corrente `v0.16`:

- auditare i gap cognitivi rimasti dopo movimento multi-tick e Job recovery;
- stabilizzare lifecycle, freshness, obsolescenza e stato delle belief;
- collegare eventi mondo needs a memoria soggettiva senza onniscienza;
- verificare localmente credenze obsolete prima di agire;
- sviluppare SearchFood avanzato con MoveTo esplorativo quando non esistono target visibili;
- preparare decisioni su conoscenza incerta e parziale;
- introdurre comunicazione soggettiva minima dei fatti osservati;
- chiudere QA anti-onniscienza cognitiva prima della fase sociale.

Prossimo macro job consigliato:
`v0.20a - Audit architettura percezione/movimento e documento di rifondazione`

Residual follow-ups / future hardening:

- `v0.17`: osservatorio costi runtime, profilazione per NPC, misure per percezione/memoria/belief/query/decisione/EL/job/fallback/pathfinding;
- `v0.18`: ottimizzazione forte runtime percezione/belief/query basata sui dati JSONL dell'osservatorio;
- `v0.170`: query multi-candidato food, recovery `FindEquivalentTarget` senza query interne, sospetto, reputazione, audit cibo privato, rumor e conseguenze sociali;
- `v0.180+`: observer layer pubblico, incapsulamento store `World`, cleanup completo command/event.

Checkpoint v0.13 pianificati:

- `v0.13a`: audit responsabilita' residue NeedsDecisionRule - DONE;
- `v0.13b`: estrazione servizi utili da NeedsDecisionRule - DONE;
- `v0.13c`: scollegamento NeedsDecisionRule come autorita' runtime - DONE;
- `v0.13d`: ricostruzione bisogni principali minimi via JobRequest/Job - DONE;
- `v0.13e`: configurazione fallback/recovery locale da matrice Job - DONE;
- `v0.13f`: fallimenti minimi e ritorno cognitivo leggero - DONE;
- `v0.13g`: eliminazione definitiva NeedsDecisionRule legacy - DONE;
- `v0.13h`: QA e closeout pensionamento legacy - DONE.

Checkpoint v0.14 pianificati:

- `v0.14a`: audit recovery Job post-NeedsDecisionRule - DONE;
- `v0.14b`: mappa fallimenti step -> strategie recovery - DONE;
- `v0.14c`: integrazione policy recovery nel Job runtime - DONE;
- `v0.14d`: retry locale controllato e limiti anti-loop - DONE;
- `v0.14e`: fallback per target non valido o risorsa sparita - DONE;
- `v0.14f`: failure learning minimo verso memoria/credenze - DONE;
- `v0.14g`: explainability recovery Job - DONE;
- `v0.14h`: QA e closeout Job Recovery Runtime - DONE.

Checkpoint v0.15 pianificati:

- `v0.15.1`: debug riapertura job e cadenza decisionale - DONE;
- `v0.15.2`: audit movimento legacy vs RunningAction MoveTo - DONE;
- `v0.15.3`: bug target cibo eliminato durante movimento e belief obsoleta - DONE;
- `v0.15.4`: specifica RunningAction MoveTo e cause fallimento - DONE;
- `v0.15.5`: matrice recovery movimento in `job_recovery_policies.json` - DONE;
- `v0.15.6`: MoveTo multi-cella su route conosciuta - DONE;
- `v0.15.7`: porte e micro-interazioni locali in MoveTo - DONE;
- `v0.15.8`: rimozione fallback greedy ordinario - DONE;
- `v0.15.9`: isolamento MoveIntent/MovementSystem come dev o compatibilita' - PARTIAL / COMPAT;
- `v0.15.10`: EL movimento Job e QA anti-onniscienza path - DONE;
- `v0.15.11`: flash diagnostico NPC su decisione presa - DONE;
- `v0.15.12`: QA target cibo, belief food lifecycle e percezione same-cell - DONE;
- `v0.15.13`: closeout movimento multi-tick - DONE.

Checkpoint v0.16 pianificati:

- `v0.16a`: allineamento template `generic.move_to_cell.v1` e `transport.object_to_cell.v1` al movimento multi-tick - DONE;
- `v0.16b`: audit cognition gap post-v0.15 - DONE;
- `v0.16c`: lifecycle credenze e obsolescenza cibo/oggetti - DONE;
- `v0.16d`: memoria da eventi needs - DONE;
- `v0.16e`: verifica locale credenze - DONE;
- `v0.16f`: comunicazione soggettiva minima - DONE;
- `v0.16g`: decisione su conoscenza incerta - DONE;
- `v0.16h`: QA anti-onniscienza cognitiva - DONE;
- `v0.16i`: SearchFood avanzato con MoveTo esplorativo quando non esistono target visibili - DONE;
- `v0.16j`: closeout cognizione soggettiva avanzata - DONE.

Checkpoint v0.17 pianificati:

- `v0.17a`: audit punti caldi runtime per NPC - DONE;
- `v0.17b`: configurazione osservatorio con costo nullo quando spento - DONE;
- `v0.17c`: misure per sistema: percezione, memoria, belief, query, decisione, EL, job, fallback - DONE;
- `v0.17d`: misure per NPC e individuazione NPC piu' costosi - DONE;
- `v0.17e`: contatori operativi: celle viste, oggetti controllati, query, path, trace, fallback - DONE;
- `v0.17f`: JSONL opzionale batchato e limitato per profili runtime - DONE;
- `v0.17g`: scenario QA 1/2/4/8/16 NPC e report costo scalare - PARTIAL, protocollo pronto e prova runtime/profiler da eseguire;
- `v0.17h`: closeout osservatorio costi runtime.

Checkpoint v0.18 pianificati:

- `v0.18a`: ottimizzazione ObjectPerceptionSystem con indice spaziale a griglia e budget massimo oggetti/celle per NPC - DONE;
- `v0.18b`: riduzione BeliefQuery con indice per categoria belief - DONE;
- `v0.18c`: BeliefDecay discreto per categoria con parametri da JSON - DONE;
- `v0.18d`: QA profiler comparativo pre/post ottimizzazione con 1/2/4/8/16 NPC - PARTIAL, protocollo pronto;
- `v0.18e`: ottimizzazione NpcPerceptionSystem con indice spaziale a zone - DONE;
- `v0.18f`: closeout ottimizzazione runtime percezione/belief/query.

Checkpoint v0.20 pianificati:

- `v0.20a`: audit architettura percezione/movimento e documento di rifondazione - DONE;
- `v0.20b`: indici persistenti compatti per oggetti e NPC - DONE;
- `v0.20c`: dirty percettivo conservativo per oggetti/NPC creati, mossi, distrutti o ruotati - DONE;
- `v0.20d`: separazione observed / watched nella mappa percettiva - DONE;
- `v0.20e`: scheduler percettivo per stato NPC con cadenza da `game_params` - DONE;
- `v0.20f`: limite massimo NPC percettivi per tick e distribuzione del carico - DONE;
- `v0.20g`: percezione oggetti/NPC su soli NPC dirty/cadenzati - DONE;
- `v0.20h`: Landmark perception allineata a dirty/cadenza/range - DONE;
- `v0.20i`: rotazione movimento e `LookDirection` come sorgenti dirty percettive - DONE;
- `v0.20j`: cleanup strutture obsolete o ridondanti post-rifondazione - DONE;
- `v0.20k`: QA profiler 20/50/100 NPC e debug overlay costo percettivo - DONE;
- `v0.20l`: closeout rifondazione percezione runtime - DONE;
- `v0.20m`: riallineamento percezione centrale, porte dirty e rimozione periodo landmark - DONE.

Nota v0.20e: gli NPC possiedono ora uno stato percettivo configurabile da `game_params`; ogni stato espone cadenza, raggio visivo e cono. Il raggio dirty conservativo usa il massimo teorico tra gli stati configurati piu' margine, cosi' oggetti/NPC creati o modificati sporcano i possibili osservatori senza eseguire percezione immediata.
Nota v0.20f: il `World` espone ora un selettore percettivo per tick che applica dirty, cadenza per stato, limite massimo configurato e round-robin deterministico. Il selettore produce liste selected/pending e contatori numerici, ma non chiama ancora i sistemi percettivi e non pulisce i dirty; il consumo operativo avverra' in `v0.20g`.
Nota v0.20g: `ObjectPerceptionSystem` e `NpcPerceptionSystem` usano ora la stessa selezione percettiva del tick invece di iterare tutti gli NPC. La pulizia dirty avviene al termine del blocco NPC perception per i soli NPC selezionati; gli NPC pending restano dirty per i tick successivi.
Nota v0.20h: `LandmarkPerceptionSystem` usa ora la stessa selezione percettiva condivisa del tick e risolve range/cone dallo stato percettivo dell'NPC, evitando che il learning landmark resti un percorso percettivo separato e globale.
Nota v0.20i: `SetFacing` e `SetNpcPos` producono dirty percettivo immediato, quindi rotazione e spostamento reale non attendono la cadenza lenta dello stato idle. `MoveToRunningActionDriver` orienta l'NPC verso la prossima cella prima del traversal multi-tick, `MovementSystem` legacy/debug mantiene lo stesso orientamento prima dello spostamento, e `IdleScanSystem` usa lo stato `LookDirection` durante le rotazioni dello scan.
Nota v0.20j: audit cleanup completato. Non sono state eliminate strutture percettive vive: indici persistenti, dirty, watched/observed e contatori costo sono ancora necessari. La patch ha rimosso letture ridondanti in `ObjectPerceptionSystem` e riallineato commenti interni di `World`; `IdleScanSystem` e `MovementSystem` restano ponti tollerati.
Nota v0.20k: `logging.runtime_cost_observer` e' ora presente in `game_params.json` ma spento di default. Quando viene attivato, l'overlay coordinate mostra budget percettivo ultimo tick e contatori cumulativi principali per oggetti, NPC e FOV, utile per QA 20/50/100 NPC senza aprire subito i file JSONL.
Nota v0.20l: fase `v0.20` chiusa. La rifondazione percettiva ha introdotto indici persistenti, dirty conservativo, watched/observed, cadenza per stato, budget per tick, consumo condiviso da percezione oggetti/NPC/landmark e overlay costi attivabile. I prossimi lavori devono usare i dati del runtime cost observer per ottimizzazioni mirate, non riaprire scansioni globali.
Nota v0.20m: riallineamento post-closeout. `landmark_perception.period` viene eliminato e la percezione landmark e' governata solo dagli stati percettivi; `IdleScanSystem` precede tutto il blocco percettivo; la pulizia dirty passa a `PerceptionDirtyCompletionSystem`; le porte aperte/chiuse marcano dirty gli NPC nel watched cone economico; l'overlay mostra NPC percepiti su totale NPC.
Checkpoint v0.12 completati:

- `v0.12a`: audit logging, explainability e diagnostica runtime;
- `v0.12b`: roadmap riallineata alla nuova fase diagnostica;
- `v0.12c`: configurazione runtime consolidata senza modificare `game_params.json`;
- `v0.12d`: canali legacy runtime rimossi;
- `v0.12e`: `ArcontioLogger` chiuso come ponte transitorio, con ciclo vita JSONL spostato su servizio dedicato;
- `v0.12f`: `Telemetry` assorbita come ponte diagnostico inerte quando disattiva;
- `v0.12g`: EL runtime reso piu' leggero quando disattivo;
- `v0.12h`: pannelli EL ottimizzati e fase diagnostica chiusa.

Nota `v0.12c`:

- `game_params.json` resta il file portante della configurazione;
- `SimulationParams` e `GameParams` vivono sotto `Core/Config`;
- `SimulationHost` legge il file una sola volta;
- `SimulationParams` e' il modello principale del bootstrap;
- `GameParams.cs` resta ponte compatibile, ma non e' piu' il percorso ordinario del runtime.

Checkpoint v0.12d completato:

- rimossi fisicamente `UnityConsoleSink`, `FileSink` TXT, `HtmlFileSink`, `UnityOverlaySink` e `ArcontioLogOverlay`;
- rimossi i relativi `.meta` Unity per evitare asset orfani;
- `ArcontioLogger` non apre piu' canali console, TXT, HTML o overlay;
- `JsonlRuntimeLogHub` e i sink JSONL EL restano preservati;
- nodo successivo completato in `v0.12e`: `ArcontioLogger` resta ponte transitorio, non logger runtime futuro.

Checkpoint v0.12e completato:

- `ArcontioLogger` mantenuto solo come ponte transitorio di compatibilita';
- ciclo vita JSONL spostato nel servizio dedicato `RuntimeDiagnosticsLifecycle`;
- rimossi `LocalizationDb` e `localization_logs.json`;
- potate le chiamate legacy sicure da comandi needs, loader configurazione e audit/debug evento;
- restano fuori scope i log movimento/landmark, scenario seed e sociali/furto, da gestire con step piu' mirati;
- prossimo nodo operativo: audit e riduzione di `Telemetry` in `v0.12f`.

Checkpoint v0.12f completato:

- `Telemetry` resta temporaneamente nelle firme di `ISystem` e `IRule`;
- il runtime ordinario la costruisce leggendo `logging.telemetry.enabled`;
- default sicuro: Telemetry spenta;
- quando e' spenta non crea dizionari e non accumula contatori;
- rimosso lo scarico console `DumpToConsole`;
- protetto l'unico contatore con nome dinamico per evitare costruzione stringa quando Telemetry e' spenta;
- prossimo nodo operativo: rendere EL modulare e a produzione zero quando disattivo in `v0.12g`.

Checkpoint v0.12g completato:

- i registri Movement EL e MBQD non vengono creati se i rispettivi moduli sono spenti;
- `MemoryBeliefDecisionExplainabilityEmitter` espone un gate economico per kind diagnostico;
- query, decision, bridge, needs e job usano il gate prima di costruire trace dirette;
- gli helper MBQD interni escono prima di creare record quando il modulo o il kind sono spenti;
- Movement EL non considera piu' un registry assente come tracciabile;
- prossimo nodo operativo: ridurre il costo dei pannelli EL e chiudere la fase `v0.12h`.

Checkpoint v0.12h completato:

- il builder UI MBQD puo' costruire solo la famiglia diagnostica richiesta dalla pagina visibile;
- il pannello laterale aggiorna solo la pagina attiva quando non c'e' selezione valida;
- le card NPC estese restano costruite solo per NPC selezionato e sezioni aperte;
- aggiunta QA sullo scope del ViewModel MBQD per evitare regressioni;
- la fase `v0.12` e' pronta al merge/closeout e il prossimo lavoro consigliato e' `v0.13a`.

---

# 1. Macro campagna corrente completata

## MACRO JOB COMPLETED: v0.10 — World Persistence Closure & Save/Load Completion

CHECKPOINT CORRENTE:
v0.10.16 - Documentation Closeout

STATUS:
COMPLETED

OUTPUT ATTESO:
documentazione root locale allineata alla chiusura macro job v0.10:
`[Save] add canonical world snapshot save/load baseline`

DOC SYNC:
root taskboard aggiornata al completamento tecnico v0.10;
allineamento esteso `ARCONTIO_docs` raccomandato prima/insieme alla PR

OBIETTIVO:
Chiudere il debito storico di persistenza mondo/NPC e portare ARCONTIO
ad una prima baseline di serializzazione e reload runtime costituzionalmente coerente.

La campagna v0.10 deve affrontare:

- stato mondo oggettivo salvabile;
- stato NPC persistibile;
- beliefs/memory persistibili o dichiaratamente esclusi;
- bootstrap da save invece che da seed;
- ricostruzione runtime pulita dopo load;
- coerenza tra `World`, `SimulationHost` e file json di persistenza.

Esito tecnico corrente:

Outcome v0.10:

- canonical `WorldSaveData` introduced;
- `WorldSaveBuilder` / `WorldSaveIO` / `WorldSaveLoader` introduced;
- `SimulationHost` snapshot bootstrap/save/load integrated;
- ID preserving load authority introduced;
- F3 developer world snapshot controls introduced;
- smoke PlayMode roundtrip validated.

- audit forense persistence v0.10.00 completato;
- introdotto contratto serializzabile canonico `WorldSaveData`;
- introdotti `WorldSaveBuilder`, `WorldSaveIO` e `WorldSaveLoader`;
- aggiunte API save/load authority in `World` per preservare ID NPC e ID oggetto;
- migrata nello snapshot canonico la copertura NPC gia' presente nel percorso legacy;
- aggiunta persistenza NPC/object/food/object use/cibo privato NPC;
- aggiunta persistenza diretta `BeliefStore` senza rebuild onnisciente da stato oggettivo;
- aggiunta persistenza object memory, landmark memory e complex edge memory soggettive;
- separati in `SimulationHost` i path `baseline neutral`, `legacy/debug scenario` e `load da world snapshot`;
- ripristinati `savedAtTick`, `TickContext` e `World.Global.CurrentTickIndex` nel solo path save/load;
- aggiunti entry point tecnici/dev `SaveCurrentWorldSnapshot(slot)` e `LoadWorldSnapshot(slot)`;
- aggiunti controlli DEV/DEBUG nel pannello F3 per `Save World Snapshot` e `Load World Snapshot`;
- corretto il binding visuale MapGrid dopo swap del `World`, evitando reference stale dopo load;
- smoke PlayMode roundtrip positivo: save creato, load runtime funzionante, due NPC preservati con valori differenti, oggetto cibo preservato, tick/bisogni ripristinati correttamente, nessun errore console WorldSnapshot;
- Company/Product settings corretti per stabilizzare il contesto Unity/prodotto del progetto.

Residual follow-ups / future hardening:

- validazione futura di compatibilita' `configRef`/scenario/config rispetto allo snapshot caricato;
- transient runtime buffers non persistiti: event buffer, command buffer, scheduler staging e segnali puramente runtime restano esclusi e vengono ricostruiti/reset;
- UI save/load definitiva futura: gli entry point attuali restano DEV/DEBUG e non costituiscono UX gameplay, autosave o profilo utente finale.

Questo macro job è stato infrastrutturale e backbone-level.
Non è una campagna gameplay.

---

# 2. Macro job appena chiuso

## v0.09 — Simulation Backbone Hardening & Constitutional Alignment

STATUS:
CHIUSO TECNICAMENTE E INTEGRATO SU `main` + `ai/codex-main`

Esito tecnico consolidato:

* `FoodStolenEvent` riallineato a `IWorldEvent`
* `EatFromStockCommand` riallineato a `World.DestroyObject(...)`
* `SimulationHost` corretto sul ciclo eventi post-command
* baseline neutral bootstrap introdotta
* `NPC_Runtime` spawnato al centro mappa
* legacy debug scenarios resi opt-in gated
* smoke test manuale PlayMode completato con esito positivo
* repository fully synced su branch stabili

Questo checkpoint ha portato la backbone da runtime recovery riallineata
a primo livello di runtime costituzionalmente hardenizzato.

---

# 3. Stato macro roadmap v0.11 split

La campagna originaria:

`v0.11` — Domain Reintegration: Jobs · Work · Social · Dormant Systems

è stata splittata in sottofasi per evitare di mescolare Job runtime,
Decision architecture, temporal runtime e Work/Social audit.

La v0.11 NON è chiusa integralmente.
Sono chiuse le fasi `v0.11A`, `v0.11B` e il checkpoint `v0.11c.01`.

## v0.11A — Job Backbone Reintegration

STATUS:
COMPLETED / DONE

SCOPE:
riattivare il Job System come runtime reale e tick-based.

Task completati:

* `v0.11.00` — Job System Forensic Audit
* `v0.11.01` — Food Job Vertical Slice
* `v0.11.02` — Generic Move Job Route
* `v0.11.03` — Job Arbiter Runtime Activation
* `v0.11.04` — Reservation Runtime Integration
* `v0.11.05` — Job Runtime Snapshot
* `v0.11.06` — NeedsDecisionRule Job Bridge

Consolidato:

* Decision Records `ARC-DEC-010` → `ARC-DEC-017`
* `ARC-CON-011` — Architettura della Memoria NPC
* `FoodJobVerticalSliceQaTests`: 25/25 passed
* commit job backbone: `676b443`, `d0a844e`, `325fafd`, `cafb4fe`, `585279b`, `3046958`

## v0.11B — Decision Architecture (MBQD) Foundation

STATUS:
COMPLETED / DONE

SCOPE:
rendere osservabile il primo loop runtime:

`Decision → JobRequest → Job → Step → Command → World → Perception → Memory → Belief → next decision`

Consolidato:

* recovery QA `v0.11b.05` reintegrata con PR #10;
* SearchFood perception-to-belief closure testata;
* `ARC-CON-014` MBQD v1.0 consolidato;
* `ARC-DEC-018` apre la fase multi-tick e separazione cadence runtime.

## v0.11C — Decision Orchestrator & Temporal Runtime Foundation

STATUS:
IN PROGRESS

### Checkpoint corrente completato: v0.11c.01 — Decision Orchestrator Skeleton Audit / Foundation

STATUS:
COMPLETED / DONE

PR incluse:

* PR #9 — `v0.11c.01a` Decision Orchestrator skeleton no-op;
* PR #11 — `v0.11c.01b` DecisionContextBuilder extraction;
* PR #12 — `v0.11c.01c` IntentExecutionRouter / JobRequestBuilder extraction;
* PR #13 — `v0.11c.01d` DecisionExplainabilityBridge extraction;
* PR #14 — `v0.11c.01e` NeedsDecisionRule compatibility shim.

Commit / merge principali:

* `a6b999a`
* `0ab8fb3`
* `5315714`
* `6fc25a5`
* `c7fc548`

Esito tecnico:

* introdotto skeleton no-op del futuro Decision Orchestrator;
* estratta la costruzione del contesto decisionale in `DecisionContextBuilder`;
* estratto il boundary SelectedDecision → JobRequest in `IntentExecutionRouter` / `JobRequestBuilder`;
* estratto il boundary di trace decisionale in `DecisionExplainabilityBridge`;
* marcato `NeedsDecisionRule` come compatibility shim / legacy transitional bridge;
* nessun cablaggio runtime nuovo dell'Orchestrator come primary;
* nessuna modifica a JobArbiter, JobRuntimeState o SimulationHost;
* nessuna implementazione di preemption o migrazione fallback legacy.

Validazione aggregata:

* `DecisionOrchestratorNoOpQaTests`: passed;
* `DecisionContextBuilderQaTests`: passed;
* `IntentExecutionRouterQaTests`: passed;
* `DecisionExplainabilityBridgeQaTests`: passed;
* `DecisionLayerQaTests`: passed;
* `SearchFoodJobVerticalSliceQaTests`: passed;
* `FoodJobVerticalSliceQaTests`: passed;
* `JobSystemEndToEndQaTests`: passed quando eseguiti nel blocco 01a;
* `MemoryBeliefDecisionRuntimeJobScenarioQaTests`: passed nei blocchi di recovery/01c/01d/01e.

### Checkpoint corrente completato: v0.11c.02 - Multi-Tick Action Runtime

STATUS:
COMPLETED / DONE

PR incluse:

* PR #16 - `v0.11c.02b` RunningActionRuntimeState Skeleton;
* PR #17 - `v0.11c.02c` RunningAction Executor Integration;
* PR #19 - `v0.11c.02d` RunningActionStore introduction;
* PR #21 - `v0.11c.02e` RunningAction productive ticking integration;
* PR #22 - `v0.11c.02f` RunningAction lifecycle explainability traces;
* PR #23 - `v0.11c.02g` Multi-tick cell traversal foundation;
* PR #24 - `v0.11c.02h` Temporal reservation robustness;
* PR corrente - `v0.11c.02i` Deterministic multi-tick QA sweep / hardening finale.

Esito tecnico:

* introdotto stato runtime volatile per running action sotto `JobRuntimeState`;
* introdotto executor generico per progress/completion/failure/interruption senza side effect;
* cablato ticking produttivo controllato su `WaitTicks`;
* aggiunte trace lifecycle running action;
* introdotto traversal one-cell gated senza posizioni intermedie;
* aggiunta reservation temporale minima della destination cell per traversal gated;
* completata QA deterministica su gate off/on, target distante/diagonale, cleanup running action, cleanup reservation, contention e vertical slice food/search;
* `MovementSystem`, `SimulationHost`, save/load e Decision Layer restano invariati.

Validazione aggregata v0.11c.02i:

* `RunningActionProductiveTickingQaTests`: passed;
* `RunningActionRuntimeStateQaTests`: passed;
* `RunningActionExecutorQaTests`: passed;
* `RunningActionStoreQaTests`: passed;
* `ReservationStoreQaTests`: passed;
* `FoodJobVerticalSliceQaTests`: passed;
* `SearchFoodJobVerticalSliceQaTests`: passed;
* `JobSystemEndToEndQaTests`: passed;
* matrice aggregata eseguita nel closeout 02i: 81/81 passed.

### Checkpoint corrente completato: v0.11c.03 - Runtime Cadence Separation

STATUS:
COMPLETED / DONE

Esito tecnico:

* introdotto modello passivo dei motivi di rivalutazione decisionale;
* resa configurabile la cadence decisionale ordinaria legacy-compatible `decisionEveryTicks`;
* protetta con QA la separazione tra avanzamento esecutivo e cadence decisionale;
* protetta con QA l'eligibility decisionale ordinaria;
* confermato che non sono stati introdotti eventi soglia produttivi, bypass cadence, cooldown, debounce, batching o scheduler produttivo.

### Checkpoint corrente completato: v0.11c.04 - Job Runtime Stabilization & Local Step Recovery Foundation

STATUS:
COMPLETED / DONE

Esito tecnico:

* completato audit Job step failure/recovery;
* introdotti vocabolari e DTO passivi `JobStepFailureKind`, `StepRecoveryStrategy`, `StepRecoveryPolicy`, `JobRecoveryResult`;
* confermato boundary futuro `ExecuteCurrentAction(...) -> StepResult -> recovery boundary futuro -> JobStateMachine.ApplyStepResult(...)`;
* aggiunta Recovery QA matrix;
* nessun recovery runtime reale introdotto;
* `StepResultStatus.Failed` resta terminale per il job;
* `Blocked` e `Waiting` restano wait gate tecnico;
* nessun mapping produttivo, nessuna escalation cognitiva reale, nessuna preemption nuova.

Report closeout root:
`v0.11c.04_Closeout_Report.md`

### Checkpoint corrente completato: v0.11c.06 - Stabilizzazione Movimento Multi-Tick

STATUS:
COMPLETED / DONE

Esito tecnico:

* completato audit movimento legacy vs movimento temporale;
* confermato movimento Job multi-tick reale per target cardinale adiacente;
* aggiunta QA che dimostra N tick configurabili per attraversare una cella;
* posizione NPC aggiornata solo a completion della running action;
* reservation cella destinazione mantenuta durante la traversata;
* reservation traversal allineata alla durata reale configurata;
* completato audit riduzione movimento legacy senza patch runtime;
* aggiunta QA inventory dei path legacy e Job;
* `MovementSystem` resta attivo e necessario;
* `MoveIntent` resta attivo e necessario;
* target lontani, diagonali o con gate spento restano fallback legacy;
* nessuna migrazione completa movimento, nessun path lungo Job-native, nessun recovery movimento.

Report closeout root:
`v0.11c.06_Closeout_Report.md`

Prossimo checkpoint operativo:
NON DECISO in questa patch. Ogni step successivo deve restare audit-first.

---

# 4. Campagne implementative congelate

I seguenti macro job restano intenzionalmente in pausa finché la closure backbone repository non avanza:

* espansioni Job System fuori dal backbone v0.11A già chiuso
* espansioni architettura Memory fuori da `ARC-CON-011`
* espansioni Social communication prima dell'audit v0.11C
* espansioni Explainability UI
* aggiunte feature runtime ampie

Codex non deve riprenderle proattivamente salvo richiesta esplicita.

La priorità resta:
chiudere backbone, persistenza, bootstrap e authority strutturali prima di accelerare nuove feature.

---

# 5. Verità workflow repository

Branch stabile:
`main`

Branch integrazione AI:
`ai/codex-main`

Pattern predefinito branch task Codex:
`work/v0.xx-short-description`

Target integrazione predefinito:
`ai/codex-main`

Chiusura standard:
`work branch -> ai/codex-main -> main`

Non assumere mai implementazione diretta iniziale su `main`.

Più micro-step coerenti appartenenti allo stesso checkpoint possono restare sul medesimo branch task fino a chiusura del blocco.

Aprire nuovo branch task quando:

* cambia checkpoint,
* cambia dominio tecnico,
* il diff richiede isolamento di merge.

---

# 6. Stato repository attualmente noto

Confermato:

* GitHub connector operativo
* capacità scrittura Codex cloud verificata
* istruzioni workflow AGENTS installate
* protocollo CODEX installato
* bridge Notion operativo
* exporter markdown Decision Records operativo
* bootstrap root files in consolidamento avanzato
* costituzione ARCONTIO avviata
* primi Decision Records formalizzati
* roadmap v0.09 chiusa
* checkpoint tecnico v0.09 completato, committato, mergiato e sincronizzato
* branch stabili `main` e `ai/codex-main` allineati
* branch `work/v0.10-world-save-contract` contiene la baseline implementativa v0.10 world save/load
* commit tecnico v0.10 eseguito: `[Save] add canonical world snapshot save/load baseline`
* formato canonico world-level separato da chunk NPC legacy e DevMap debug
* load snapshot tecnico/dev disponibile ma non ancora promosso a UX o autosave
* smoke PlayMode manuale roundtrip v0.10 completato con esito positivo
* Company/Product settings corretti

Ancora in verifica / completamento:

* sync estesa `ARCONTIO_docs`
* pending delle feature precedenti congelate

---

# 7. Comportamento obbligatorio Codex durante questo macro job

Durante la chiusura v0.10 Codex deve:

* evitare broad gameplay coding,
* evitare refactor opportunistici fuori persistence,
* limitare eventuali fix a bug direttamente collegati al roundtrip save/load,
* non rimuovere `NpcSaveSystem`, `NpcScenarioLoader` o `DevMapIO`,
* non trasformare gli entry point tecnici in UI/autosave senza nuovo checkpoint,
* preservare la separazione fra scenario bootstrap e snapshot load,
* riportare esplicitamente i residui PlayMode e documentali prima della PR.

Ogni richiesta locale deve essere letta sotto:

`macro job attivo -> checkpoint corrente -> task locale richiesto`

e non come task isolato.

Regola di conduzione:

una volta autorizzato un macro job, gli step interni coerenti appartenenti allo
stesso blocco vengono pianificati ed eseguiti in continuità senza necessità di
micro-validazione ad ogni singolo passaggio, salvo emergere di deviazioni architetturali,
fork di repository o decisioni costituzionali.

La validazione umana ordinaria avviene ai gate di checkpoint o alla chiusura del macro job.

---

# 8. Reminder hook operatore

Se Codex completa un passaggio che modifica lo step cognitivo corrente, deve dichiarare esplicitamente:

`TASKBOARD/root update recommended.`

Se Codex completa un passaggio con reale valore documentale canonico, deve dichiarare esplicitamente:

`ARCONTIO_docs alignment recommended.`
