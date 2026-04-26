# TASKBOARD_CODEX.md — Lavagna Operativa Attiva ARCONTIO

Questo file è la console persistente dei task attivi per Codex.

Codex deve leggere questo file prima di iniziare ogni implementazione.

Non assumere vecchie conversazioni come memoria di stato valida.
Usa questo file come verità operativa corrente.

---

# 1. Macro campagna corrente in corso

## MACRO JOB ATTIVO
`v0.08 — Repository Stabilization + Codex Workflow Hardening`

Obiettivo:
stabilizzare la governance repository, rimuovere ambiguità di workflow, preparare una pipeline deterministica di sviluppo assistito da AI.

Questo macro job è solo infrastrutturale.
Non è una campagna di implementazione gameplay/sistemi.

---

# 2. Step corrente attivo

## STEP ATTIVO
`v0.08.01 — Codex repository governance installation`

Status: IN PROGRESS

Obiettivo:
- stabilizzare AGENTS / file protocollo CODEX,
- validare workflow `ai/codex-main`,
- validare disciplina di pubblicazione GitHub Codex cloud,
- pulire branch storici.

Codex deve assumere che l'hardening del workflow repository sia ancora la priorità corrente
finché l'operatore non marca questo step come DONE.

---

# 3. Prossimi step in coda (non saltare avanti salvo richiesta)

- `v0.08.02` — verifica cleanup branch storici
- `v0.08.03` — validazione primo task Codex controllato
- `v0.08.04` — protocollo sincronizzazione documentale con ARCONTIO_docs

---

# 4. Campagne implementative congelate

I seguenti macro job sono intenzionalmente in pausa finché la stabilizzazione repository non è completa:

- espansioni Job System
- espansioni architettura Memory
- espansioni Social communication
- espansioni Explainability UI
- aggiunte feature runtime

Codex non deve riprenderle proattivamente salvo richiesta esplicita.

---

# 5. Verità workflow repository

Branch stabile:
`main`

Branch integrazione AI:
`ai/codex-main`

Pattern predefinito branch task Codex:
`ai-task/v0.xx.yy-short-description`

Target PR predefinito:
`ai/codex-main`

Non assumere mai implementazione diretta su `main`.

---

# 6. Stato repository attualmente noto

Confermato:
- GitHub connector operativo
- capacità scrittura Codex cloud verificata
- istruzioni workflow AGENTS installate
- protocollo CODEX installato

Ancora in verifica:
- primo ciclo completo branch task AI -> PR -> merge
- normalizzazione finale branch storici

---

# 7. Comportamento obbligatorio Codex durante questo macro job

Durante v0.08 Codex deve:

- preferire task di audit/verifica,
- evitare broad gameplay coding,
- evitare refactor opportunistici,
- aiutare a normalizzare il workflow,
- segnalare ogni incoerenza repository scoperta.

---

# 8. Reminder hook operatore

Se Codex completa uno step in v0.08, deve dichiarare esplicitamente:

`TASKBOARD update recommended.`
