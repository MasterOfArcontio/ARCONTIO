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

# 1. Macro campagna corrente in corso

## MACRO JOB ATTIVO: v0.09 — Simulation Backbone Hardening & Constitutional Alignment

CHECKPOINT CORRENTE:
v0.09 — Hardening package completato e validato

STATUS:
COMPLETATO TECNICAMENTE — IN ATTESA DI COMMIT / MERGE

BRANCH TASK PREVISTO:
work/v0.09-simulation-backbone-hardening

OUTPUT ATTESO:
chiusura del pacchetto backbone con hardening runtime minimo, smoke test positivo
e preparazione della transizione verso `v0.10`

DOC SYNC:
roadmap locale aggiornata; sync documentale estesa ancora separata se richiesta

OBIETTIVO:
Irrigidire la backbone simulativa esistente una volta chiusa la fase documentale v0.08.

Questo macro job resta infrastrutturale.
Non è ancora una campagna di implementazione gameplay/sistemi ampia.
Si tratta di un consolidamento strutturale del codice con enfasi nell'allineamento costituzionale
e nella stabilizzazione della memoria architetturale prima di nuove feature.

Esito tecnico del checkpoint v0.09:

* `FoodStolenEvent` riallineato a `IWorldEvent`
* `EatFromStockCommand` riallineato a `World.DestroyObject(...)`
* `SimulationHost` corretto sul ciclo eventi post-command
* baseline neutral bootstrap introdotta e validata
* legacy debug scenarios resi opt-in gated
* smoke test manuale PlayMode completato con esito positivo

---

# 2. Prossimi Main Job in coda (non saltare avanti salvo richiesta)

* `v0.10` — World Persistence Closure & Save/Load Completion

---

# 3. Campagne implementative congelate

I seguenti macro job restano intenzionalmente in pausa finché la chiusura repository/bootstrap non è completa:

* espansioni Job System
* espansioni architettura Memory
* espansioni Social communication
* espansioni Explainability UI
* aggiunte feature runtime ampie

Codex non deve riprenderle proattivamente salvo richiesta esplicita.

La priorità resta:
chiudere governance cognitiva e memoria strutturale prima di accelerare nuove feature.

---

# 4. Verità workflow repository

Branch stabile:
`main`

Branch integrazione AI:
`ai/codex-main`

Pattern predefinito branch task Codex:
`ai-task/v0.xx.yy-short-description`

Target PR predefinito:
`ai/codex-main`

Non assumere mai implementazione diretta su `main`.

Più micro-step coerenti appartenenti allo stesso checkpoint possono restare sul medesimo branch task fino a chiusura del blocco.

Aprire nuovo branch task quando:

* cambia checkpoint,
* cambia dominio tecnico,
* il diff richiede PR separata.

---

# 5. Stato repository attualmente noto

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
* roadmap v0.09 definita
* checkpoint tecnico v0.09 completato e validato
* roadmap locale aggiornata sullo stato v0.09

Ancora in verifica / completamento:

* eventuale commit tecnico del branch corrente
* eventuale sync estesa `ARCONTIO_docs`
* pending delle feature precedenti congelate

---

# 6. Comportamento obbligatorio Codex durante questo macro job

Durante la finalizzazione v0.09 Codex deve:

* preferire task di audit, verifica, consolidamento e root alignment,
* evitare broad gameplay coding,
* evitare refactor opportunistici,
* aiutare a normalizzare la memoria cognitiva repository,
* segnalare ogni incoerenza tra file root, roadmap e documentazione costituzionale,
* chiudere il branch corrente in modo pulito,
* preparare il terreno per World Persistence Closure & Save/Load Completion `v0.10`.

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

# 7. Reminder hook operatore

Se Codex completa un passaggio che modifica lo step cognitivo corrente, deve dichiarare esplicitamente:

`TASKBOARD/root update recommended.`

Se Codex completa un passaggio con reale valore documentale canonico, deve dichiarare esplicitamente:

`ARCONTIO_docs alignment recommended.`