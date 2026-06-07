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

## MACRO JOB ATTIVO: v0.30 - ArcGraph Foundation e sostituzione progressiva rendering provvisorio

CHECKPOINT CORRENTE:
`v0.30i - Piano assorbimento legacy grafico`

STATUS:
IN CORSO / BRANCH TASK APERTO / AUDIT-FIRST

RAMO BASE CORRENTE:
`ai-task/v0.30i-arcgraph-legacy-absorption-plan`

BASE DI INTEGRAZIONE:
`ai/codex-main`

OUTPUT ATTESO:

- auditare il rendering legacy che dovra' essere assorbito o eliminato;
- distinguere asset/tecniche riusabili da classi provvisorie da pensionare;
- definire una sequenza futura di assorbimento senza doppio renderer permanente;
- evitare cancellazioni operative premature del sistema MapGrid corrente;
- produrre un piano tecnico leggibile per il passaggio verso `arcgraph` come sistema grafico unico.

DOC SYNC:

- Taskboard e roadmap riallineate per passaggio da `v0.30h` a `v0.30i`;
- branch `ai-task/v0.30h-arcgraph-future-placeholders` pushato con commit `4fbbd8f`;
- diario Notion aggiornato con chiusura `v0.30h` e apertura `v0.30i`.

OBIETTIVO:

Definire il piano di assorbimento del rendering legacy dentro `arcgraph`, separando cio' che va riusato da cio' che andra' eliminato. Il checkpoint deve restare audit-first: prima mappa dipendenze, responsabilita' e rischi, poi propone una sequenza futura di sostituzione.

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

# 2. Checkpoint corrente v0.30

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

- aggiornamento stato: `v0.30g` completato e pushato con commit `ecf20c3`; `v0.30h` aperto e in corso su branch `ai-task/v0.30h-arcgraph-future-placeholders`;
- aggiornamento stato: `v0.30h` completato e pushato con commit `4fbbd8f`; `v0.30i` aperto e in corso su branch `ai-task/v0.30i-arcgraph-legacy-absorption-plan`;
- branch task corrente: `ai-task/v0.30i-arcgraph-legacy-absorption-plan`;
- base di integrazione: `ai/codex-main`;
- branch `ai-task/v0.30-arcgraph-foundation` pushato con commit `d482cdc`;
- branch `ai-task/v0.30b-arcgraph-contracts` pushato con commit `2495135`;
- branch `ai-task/v0.30c-arcgraph-adapter` pushato con commit `01273d4`;
- branch `ai-task/v0.30d-arcgraph-minimal-layers` pushato con commit `b6d912f`;
- branch `ai-task/v0.30e-arcgraph-dirty-state` pushato con commit `a5ebf28`;
- branch `ai-task/v0.30f-arcgraph-z-level-compat` pushato con commit `7b3c106`;
- branch `ai-task/v0.30g-arcgraph-actor-visual` pushato con commit `ecf20c3`;
- branch `ai-task/v0.30h-arcgraph-future-placeholders` pushato con commit `4fbbd8f`;
- `arcgraph` deve sostituire il rendering provvisorio a regime, non diventare un secondo renderer permanente;
- il checkpoint corrente e' sul piano di assorbimento legacy: niente eliminazioni operative del rendering corrente senza audit e `go` dell'operatore;
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

Prima di procedere operativamente dentro `v0.30i` devono essere verificati:

1. quali classi legacy sono indispensabili oggi per bootstrap, input, rendering e debug;
2. quali parti possono essere riusate da `arcgraph` senza portarsi dietro il monolite legacy;
3. quale sequenza futura evita un doppio renderer permanente;
4. quali rischi di regressione visuale o runtime emergono se si rimuove troppo presto MapGrid/WorldView;
5. quali test/compilazioni bastano per validare il checkpoint.

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
-> v0.30i piano assorbimento legacy grafico
-> solo dopo assorbimento progressivo del legacy grafico
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
- branch task corrente `ai-task/v0.30i-arcgraph-legacy-absorption-plan` aperto da `ai-task/v0.30h-arcgraph-future-placeholders`;
- `main` locale allineato a `origin/main` sul commit `8ca3af0`;
- PR #131 integrata su `ai/codex-main`;
- PR #132 integrata su `main` per il bootstrap analisi/audit;
- rami temporanei `ai-task/v0.21-post-merge-fixes` e `ai-task/governance-bootstrap-main-sync` rimossi;
- nessun merge o rebase incompleto rilevato;
- `AI_SESSION_BOOT.md` allineato su `ai/codex-main` e `main`.

Da completare:

- commit e pubblicazione del riallineamento Roadmap/Taskboard per `v0.30i`;
- piano di assorbimento ed eliminazione legacy grafico;
- pulizia dei numerosi branch storici soltanto tramite campagna dedicata e autorizzata.

---

# 8. Comportamento obbligatorio Codex durante questo macro job

Durante `v0.30` Codex deve:

- restare audit-first sui cambiamenti grafici;
- non modificare Decision Layer, Job Layer o sistemi di simulazione salvo checkpoint esplicito;
- non trasformare `arcgraph` in fonte di verita' simulativa;
- non creare doppio renderer permanente;
- preservare il comportamento visivo attuale durante la foundation;
- preparare coordinate x/y/z anche se il runtime opera ancora su z = 0;
- preparare interpolazione visuale multitick senza mutare la posizione simulativa discreta;
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
