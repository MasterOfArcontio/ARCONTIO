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

## MACRO JOB ATTIVO: v0.21 — Stabilizzazione post-rifondazione percettiva

CHECKPOINT CORRENTE:
`v0.21d — Correzione watched FOV e consolidamento post-merge`

STATUS:
IN CORSO / RAMO TASK PUBBLICATO / INTEGRAZIONE SU `ai/codex-main` PENDENTE

RAMO CORRENTE:
`ai-task/v0.21-post-merge-fixes`

BASE DI INTEGRAZIONE:
`ai/codex-main`

OUTPUT ATTESO:

- stabilizzare i comportamenti emersi durante i test runtime successivi alla rifondazione percettiva;
- integrare le correzioni post-merge già pubblicate;
- verificare SearchFood, `WaitAndObserve`, rinforzo mnemonico cadenzato e visualizzazione watched FOV;
- misurare il nuovo costo dominante Decisione → Incarico;
- rifinire il censimento di Intent, Job, Step, Running Action e politiche di recupero;
- preparare il prossimo checkpoint senza riaprire scansioni percettive globali.

DOC SYNC:

- diario di sviluppo Notion aggiornato fino allo stato corrente `v0.21`;
- Taskboard riallineata allo stato reale dopo la chiusura `v0.20q`;
- allineamento esteso `ARCONTIO_docs` raccomandato quando `v0.21` avrà un closeout stabile.

OBIETTIVO:

Consolidare la rifondazione percettiva completata in `v0.20`, correggere i problemi osservati durante i test runtime e individuare il nuovo collo di bottiglia prima di ampliare cataloghi decisionali, sistemi sociali o popolazione simulata.

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

# 2. Checkpoint corrente v0.21

| Checkpoint | Task | Stato |
|---|---|---|
| v0.21a | Monitor stato percettivo e audio diagnostico iniziale | ✅ INTEGRATO SU `ai/codex-main` |
| v0.21b | Stabilità SearchFood, percezione durante osservazione ed etichette runtime | ⚠️ PUBBLICATO SU RAMO TASK |
| v0.21c | Rinforzo mnemonico cadenzato e debug watched FOV | ⚠️ PUBBLICATO SU RAMO TASK |
| v0.21d | Correzione margine watched FOV sui quattro lati | ⚠️ PUBBLICATO SU RAMO TASK |
| v0.21e | QA runtime post-merge e confronto costi aggiornato | ⏳ PENDING |
| v0.21f | Censimento e classificazione Intent / Job / Step / Running Action | ⏳ IN ANALISI |
| v0.21g | Closeout stabilizzazione post-rifondazione | ⏳ PENDING |

Note operative:

- `ai/codex-main` contiene la PR #130 fino al commit `13e0e8c`;
- `ai-task/v0.21-post-merge-fixes` contiene sei commit aggiuntivi pubblicati e non ancora integrati;
- le correzioni del ramo corrente non devono essere dichiarate completate su `ai/codex-main` prima del merge;
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

Prima di aprire una nuova fase implementativa ampia devono essere verificati:

1. merge del ramo `ai-task/v0.21-post-merge-fixes` su `ai/codex-main`;
2. prova runtime mirata su SearchFood, `WaitAndObserve`, watched FOV e memoria;
3. nuovo file JSONL dell'osservatorio costi;
4. conferma del collo di bottiglia Decisione → Incarico;
5. approvazione del modello di composizione tra Job di alto livello e unità riusabili di basso livello.

---

# 5. Campagne future congelate

Le seguenti campagne non devono partire automaticamente:

- `v0.170` — Conseguenze Sociali Emergenti;
- `v0.180` — Observer Layer Pubblico ed Explainability Esterna;
- ampliamento sistemi lavoro e produzione;
- planner globale;
- recovery intelligente completa;
- composizione gerarchica dei Job senza contratto approvato.

La priorità resta:

```text
stabilizzare v0.21
→ misurare Decisione / Incarichi
→ chiarire cataloghi e composizione
→ solo dopo aprire nuove espansioni sistemiche
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

- ramo corrente `ai-task/v0.21-post-merge-fixes`;
- ramo corrente pubblicato e allineato al proprio remoto prima dell'aggiornamento Taskboard;
- `ai/codex-main` locale allineato a `origin/ai/codex-main` sul commit `13e0e8c`;
- `main` locale allineato a `origin/main` sul commit `7f2b924`;
- ramo corrente avanti di sei commit rispetto a `ai/codex-main`;
- nessun merge o rebase incompleto rilevato;
- `AI_SESSION_BOOT.md` contiene una modifica locale preesistente da non includere automaticamente.

Da completare:

- commit e pubblicazione del presente riallineamento Taskboard;
- integrazione del ramo corrente tramite PR verso `ai/codex-main`;
- eventuale avanzamento di `main` soltanto dopo decisione esplicita dell'operatore;
- pulizia dei numerosi branch storici soltanto tramite campagna dedicata e autorizzata.

---

# 8. Comportamento obbligatorio Codex durante questo macro job

Durante `v0.21` Codex deve:

- restare audit-first sui cambiamenti a percezione, decisione e Job;
- non reintrodurre scansioni globali ordinarie;
- non trasformare dirty percettivo in conoscenza soggettiva;
- non alterare score decisionali per risolvere problemi di prestazioni;
- non trasformare JobExecutionSystem in un secondo decisore;
- mantenere i percorsi diagnostici congelabili;
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
