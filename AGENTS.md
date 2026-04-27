<!-- UNITY CODE ASSIST INSTRUCTIONS START -->

* Project name: Arcontio
* Unity version: Unity 6000.3.3f1

<!-- UNITY CODE ASSIST INSTRUCTIONS END -->

# AGENTS.md — Istruzioni di Ingresso Codex ARCONTIO

Questa repository opera sotto una disciplina rigorosa di patch AI controllata.

Prima di qualsiasi attività di coding, Codex deve leggere:

* AI_SESSION_BOOT.md
* CODEX_CONTEXT.md
* CODEX_PROTOCOL.md
* TASKBOARD_CODEX.md
* REPO_MAP.md

Questi file definiscono architettura repository, regole di workflow, gerarchia di bootstrap e stato del macro job attivo.

Per task architetturali, costituzionali, di governance o sensibili rispetto alla roadmap, Codex deve inoltre consultare:

* ARCONTIO_Roadmap_Notion.md
* documenti ARC-CON pertinenti
* documenti ARC-DEC pertinenti

quando tali materiali sono disponibili in `ARCONTIO_docs`.

---

## Ruolo della repository

Questa repository (`ARCONTIO`) contiene esclusivamente codice Unity di produzione.

Pianificazione progetto, engineering logs, note costituzionali di architettura, nomenclature
e memoria di sviluppo estesa sono mantenuti nella repository separata:

`ARCONTIO_docs`

Codex non modifica la repository documentale salvo richiesta esplicita.

---

## Politica critica dei branch

Branch stabile protetto:

`main`

Branch base di integrazione AI:

`ai/codex-main`

Codex non deve MAI eseguire lavoro implementativo direttamente su `main`.

Per ogni blocco di implementazione o audit Codex deve creare o usare un branch task temporaneo nominato:

`ai-task/v0.xx.yy-short-description`

salvo richiesta esplicita diversa da parte dell'operatore.

Più micro-step coerenti appartenenti allo stesso macro checkpoint possono restare sul medesimo `ai-task` branch fino alla chiusura del checkpoint.

Un nuovo branch task è normalmente richiesto quando:

* cambia il macro obiettivo tecnico;
* cambia in modo significativo l'area architetturale;
* il diff accumulato diventa troppo ampio;
* è necessaria una PR separata e pulita.

Tutte le pull request devono avere come target:

`ai/codex-main`

mai `main`, salvo istruzione esplicita.

---

## Filosofia operativa Codex

ARCONTIO è una architettura di simulazione deterministica con runtime coupling fragile.

Per questo motivo:

* nessun feature coding cieco;
* nessun broad refactor senza audit;
* nessuna espansione multi-file salvo necessità;
* nessun cleanup non correlato durante task implementativi.

I task architetturali devono iniziare con ispezione in sola lettura dei contratti e dei file coinvolti.

Codex deve preferire:

smallest safe patch,
single-responsibility edits,
behavior-preserving modifications first.

Codex deve sempre interpretare la richiesta locale dell'utente sotto la gerarchia:

`macro job attivo -> checkpoint corrente -> task locale richiesto`

e non come prompt isolato scollegato dallo stato reale della repository.

---

## Output richiesto dopo ogni task

Codex deve sempre riportare:

1. touched files,
2. exact behavioral impact,
3. exact diff summary,
4. risks / follow-up notes,
5. PR readiness status,
6. checkpoint update recommended yes/no,
7. ARCONTIO_docs alignment recommended yes/no.

Non deve patchare in silenzio e fermarsi.

---

## Promemoria sincronizzazione documentale

Se una patch modifica:

* contratti architetturali,
* comportamento simulativo,
* workflow policy,
* struttura repository,

Codex deve ricordare esplicitamente all'operatore se è raccomandato:

* solo un aggiornamento checkpoint/root files,

oppure

* un allineamento completo di `ARCONTIO_docs`.
