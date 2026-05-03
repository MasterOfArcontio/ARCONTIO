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

# 3. Prossimi Main Job in coda (non saltare avanti salvo richiesta)

* `v0.11` — Domain Reintegration: Jobs · Work · Social · Dormant Systems
* `v0.12` — Jobs Backbone Primary Integration (provvisorio, da confermare dopo audit)

---

# 4. Campagne implementative congelate

I seguenti macro job restano intenzionalmente in pausa finché la closure backbone repository non avanza:

* espansioni Job System
* espansioni architettura Memory
* espansioni Social communication
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
