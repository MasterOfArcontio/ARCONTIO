# ARCONTIO — Workflow Operativo AI + Git + Notion

STATUS: ATTIVO
VERSIONE: v2.1
ULTIMO AGGIORNAMENTO: 26/04/2026
SCOPO: definire il flusso operativo quotidiano per gestione macro job, uso Codex, aggiornamento Notion e aggiornamento file `.md` root.

---

## 1. Principio generale

Ogni macro job ARCONTIO deve lasciare tre cose allineate:

1. **Codice / repository**

   * branch corretto
   * commit puliti
   * stato Git tracciabile
   * PR coerenti rispetto al blocco di lavoro

2. **Memoria operativa root**

   * file `.md` aggiornati nella root della repository `ARCONTIO` quando cambia il contesto cognitivo reale degli agenti AI

3. **Memoria lunga**

   * Notion aggiornato ai checkpoint significativi
   * eventuale mirror in `ARCONTIO_docs` quando esiste impatto documentale canonico

Il principio è:

> Notion descrive lo stato vivo e leggibile del progetto.
> I file root `.md` danno agli agenti AI il contesto minimo per non ripartire da zero.
> `ARCONTIO_docs` conserva la documentazione canonica versionata.

Ne consegue una regola importante:

> non ogni micro-step interno richiede sincronizzazione documentale completa.

Il flusso corretto è:

```text
il macro job è l’unità primaria di pianificazione e validazione;
gli step interni sono pianificati in anticipo ed eseguiti in sequenza;
la validazione umana avviene sul macro job o sui gate principali.
Checkpoint significativi eventuali -> update taskboard / Notion
fine macro job -> sincronizzazione documentale completa
```

---

## 2. File root e loro funzione

### `AI_SESSION_BOOT.md`

Funzione: punto di ingresso cognitivo per ogni nuova sessione AI.

Da modificare quando:

* cambia la fase generale del progetto;
* cambia il modo in cui gli agenti devono leggere il contesto;
* viene introdotto un nuovo documento istituzionale prioritario;
* cambia la gerarchia di bootstrap;
* cambia il rapporto tra memoria root e memoria lunga.

Non va modificato a ogni task implementativo e non va modificato per semplici checkpoint interni.

---

### `TASKBOARD_CODEX.md`

Funzione: verità operativa corrente per Codex.

È il file più importante per governare il macro job attivo, ma non deve diventare il diario di ogni micro-passaggio.

Da modificare sempre quando:

* cambia macro job attivo;
* cambia checkpoint operativo significativo;
* viene aperto un nuovo branch task rilevante;
* cambia l'obiettivo richiesto a Codex per la prossima sessione;
* un blocco importante passa da IN PROGRESS a DONE;
* cambiano branch, commit, PR o stato test in modo tale da modificare la prosecuzione del lavoro;
* Codex deve sapere cosa fare nella prossima sessione senza rileggere tutta la cronologia chat.

Non va aggiornato per ogni singolo step interno se il macro obiettivo resta invariato.

---

### `CODEX_PROTOCOL.md`

Funzione: regole operative permanenti di Codex.

Da modificare quando:

* cambia il workflow branch;
* cambia la policy PR;
* cambia il comportamento obbligatorio di Codex;
* cambia il protocollo Notion bridge;
* vengono aggiunte nuove operazioni autorizzate del bridge;
* cambia il contratto di report finale;
* si introducono nuove regole di sicurezza;
* cambia la disciplina di audit o patch.

Non va modificato per normali task implementativi o per avanzamenti interni di macro job.

---

### `CODEX_CONTEXT.md`

Funzione: identità architetturale permanente del progetto.

Da modificare quando:

* viene stabilita una nuova regola architetturale non negoziabile;
* cambia il modello mentale del progetto;
* vengono aggiunti macro-sistemi realmente implementati;
* un sistema passa da “futuro” a “implementato”;
* emergono nuove regole fondamentali da Decision Records;
* cambia la definizione di layer o dominio;
* un audit costituzionale ridefinisce la lettura canonica del codice.

Non va modificato per patch piccole, fix locali o step tecnici interni.

---

### `REPO_MAP.md`

Funzione: mappa navigabile della repository.

Da modificare quando:

* vengono create nuove cartelle core;
* viene spostato un sistema;
* cambia il ruolo di una cartella;
* nasce una nuova famiglia architetturale;
* un modulo diventa abbastanza stabile da meritare voce nel mapping;
* cambia la mappa di navigazione consigliata per gli audit AI.

Non va modificato se si aggiunge solo un file dentro una cartella già descritta, salvo file molto centrale.

---

### `ARCONTIO_Roadmap_Notion.md`

Funzione: roadmap macro importata/allineata da Notion.

Da modificare quando:

* cambia la sequenza versionale;
* cambia una Definition of Done;
* cambia lo stato di una versione;
* vengono aggiunti o rimossi blocchi roadmap;
* una milestone slitta o viene ridefinita;
* un macro job viene assorbito, cancellato o splittato.

Normalmente la roadmap vive prima in Notion e poi viene riallineata qui.

---

### `AGENTS.md`

Funzione: istruzioni di ingresso per agenti AI/coding tools.

Da modificare quando:

* cambia il set minimo di file da leggere;
* cambiano policy critiche branch;
* cambia il ruolo della repository;
* si aggiungono nuove regole agentiche di alto livello;
* cambia il contratto minimo di bootstrap.

Va mantenuto breve. Non duplicare tutto `CODEX_PROTOCOL.md`.

---

## 3. Regola rapida: quali file modificare al cambio task

### Micro-step interno nello stesso macro job

Normalmente NON aggiornare:

* `AI_SESSION_BOOT.md`
* `CODEX_CONTEXT.md`
* `CODEX_PROTOCOL.md`
* `REPO_MAP.md`
* `ARCONTIO_Roadmap_Notion.md`

Aggiornare solo se necessario:

* `TASKBOARD_CODEX.md` se il checkpoint cognitivo cambia davvero

Eventualmente:

* diario Notion se il passaggio è degno di traccia
* `ARCONTIO_docs` patch log solo se il micro-step produce un impatto documentale autonomo

Per tutti gli altri casi basta:

```text
report Codex finale + stato Git coerente
```

---

### Cambio checkpoint significativo nello stesso macro job

Aggiorna:

* `TASKBOARD_CODEX.md`
* Notion Taskboard Operativa AI
* eventualmente Diario di sviluppo Notion

Valuta aggiornamento:

* `ARCONTIO_docs` se il checkpoint produce una fotografia utile

---

### Cambio macro job

Aggiorna:

* `TASKBOARD_CODEX.md`
* Notion Taskboard Operativa AI
* Notion Roadmap / Gestione Progetto
* eventualmente `ARCONTIO_Roadmap_Notion.md`

Valuta aggiornamento:

* `AI_SESSION_BOOT.md`, se cambia la fase generale del progetto
* `CODEX_CONTEXT.md`, se il macro job attiva un nuovo dominio architetturale
* `CODEX_PROTOCOL.md`, se cambia la disciplina di lavoro
* `REPO_MAP.md`, se il nuovo macro job impone nuova navigazione

---

### Chiusura checkpoint importante

Aggiorna:

* `TASKBOARD_CODEX.md`
* Notion Taskboard Operativa AI
* Diario di sviluppo Notion

Contenuto minimo:

* checkpoint completato
* branch usato
* commit/PR
* test eseguiti
* document sync status
* next action

---

### Chiusura macro job

Aggiorna:

* `TASKBOARD_CODEX.md`
* Notion Taskboard Operativa AI
* Roadmap Notion
* `ARCONTIO_Roadmap_Notion.md`
* eventuale changelog / patch log in `ARCONTIO_docs`

Valuta aggiornamento:

* `AI_SESSION_BOOT.md`
* `CODEX_CONTEXT.md`
* `REPO_MAP.md`
* `DOC_REGISTRY.md` in `ARCONTIO_docs`

---

### Nuova regola architetturale emersa

Aggiorna:

* Notion Costituzione
* Notion Decision Records
* `ARCONTIO_docs/00_Costituzione/...`
* `ARCONTIO_docs/00_Costituzione/Decision_Records/...`

Valuta aggiornamento root:

* `CODEX_CONTEXT.md` se la regola deve guidare sempre Codex
* `AI_SESSION_BOOT.md` se diventa documento prioritario di bootstrap
* `CODEX_PROTOCOL.md` se cambia comportamento operativo

---

### Cambiamento struttura repository

Aggiorna:

* `REPO_MAP.md`
* `TASKBOARD_CODEX.md` se impatta il macro job corrente
* `CODEX_CONTEXT.md` se il cambiamento è architetturale
* Notion / `ARCONTIO_docs` se impatta documentazione canonica

---

### Cambiamento regole Codex / branch / PR

Aggiorna:

* `CODEX_PROTOCOL.md`
* `AGENTS.md` se è una regola di ingresso critica
* `TASKBOARD_CODEX.md` se riguarda il job corrente
* Notion Taskboard / governance AI

---

### Cambiamento Notion bridge

Aggiorna:

* `CODEX_PROTOCOL.md`
* eventuale file comandi locali `NOTION_BRIDGE_COMMANDS.txt`
* Notion governance se cambia la policy
* `AI_SESSION_BOOT.md` solo se il bridge diventa parte del bootstrap standard

---

## 4. Workflow inizio task

### 4.1 Preparazione umana

Prima di avviare Codex o ChatGPT:

1. identificare macro job;
2. identificare checkpoint corrente;
3. decidere se il task richiesto è:

   * audit;
   * implementazione;
   * documentazione;
   * refactor;
   * governance;
4. verificare se il task è un semplice micro-step interno o un checkpoint reale;
5. controllare che Notion Taskboard sia coerente;
6. aggiornare `TASKBOARD_CODEX.md` solo se il contesto operativo è realmente cambiato.

---

### 4.2 Aggiornamento minimo `TASKBOARD_CODEX.md`

Ogni macro job attivo deve indicare:

```text
MACRO JOB ATTIVO:
CHECKPOINT CORRENTE:
Status:
Obiettivo:
Branch task previsto:
Documenti richiesti:
Output atteso:
Doc Sync:
```

Non deve contenere il diario di ogni micro-step, ma la fotografia leggibile del cantiere attuale.

---

### 4.3 Prompt standard a Codex App

```text
Prima di procedere leggi in ordine:

AI_SESSION_BOOT.md
TASKBOARD_CODEX.md
CODEX_PROTOCOL.md
CODEX_CONTEXT.md
REPO_MAP.md

Poi riassumi:
- macro job attivo
- checkpoint corrente
- branch corretto
- file probabilmente coinvolti
- rischi principali

Non modificare file finché non hai completato l'audit iniziale.
```

---

## 5. Workflow durante il task

Codex deve:

1. lavorare su branch corretto;
2. fare audit prima di patch su aree sensibili;
3. limitare i file toccati;
4. evitare cleanup opportunistici;
5. mantenere separazione layer;
6. mantenere il focus sul macro obiettivo e non disperdersi in fix collaterali;
7. riportare sempre:

   * file toccati;
   * diff summary;
   * impatto runtime;
   * rischi;
   * test/check eseguiti;
   * stato PR/commit;
   * checkpoint update recommended sì/no;
   * macro sync recommended sì/no.

---

## 6. Workflow fine task

A fine task:

### 6.1 Verifica Git

```bash
git status
git diff
```

Se tutto è corretto:

```bash
git add <file>
git commit -m "<messaggio>"
git push origin <branch>
```

Se il blocco è pronto:

```bash
create PR -> ai/codex-main
merge
cleanup branch
```

---

### 6.2 Aggiornamento Taskboard

Aggiorna `TASKBOARD_CODEX.md` solo se:

* il checkpoint è concluso;
* il checkpoint cambia;
* cambia il branch task di riferimento;
* cambia il prossimo output richiesto a Codex.

Aggiornare con:

* checkpoint concluso o nuovo status;
* commit/PR;
* test;
* next checkpoint;
* doc sync pending/done.

---

### 6.3 Aggiornamento Notion

Aggiorna:

* Taskboard Operativa AI;
* Diario di sviluppo;
* eventuale pagina funzionale / costituzionale;
* eventuale Decision Record se emersa nuova decisione.

solo quando il passaggio è abbastanza rilevante da costituire checkpoint o chiusura blocco.

Comando utile:

```powershell
.\notion_bridge.ps1 -Action logstep -PageId "<DIARIO_PAGE_ID>" -Step "v0.xx.yy" -Status "DONE" -Text "Sintesi completamento."
```

---

### 6.4 Aggiornamento ARCONTIO_docs

Necessario se il task cambia:

* architettura;
* convenzioni;
* decision records;
* funzionali;
* patch log;
* roadmap.

Non necessario per fix tecnici interni senza impatto documentale canonico.

Nella fase attuale questo punto è particolarmente sensibile perché la chiusura `v0.08` coincide con il consolidamento del primo bootstrap documentale canonico.

---

## 7. Workflow export Notion verso ARCONTIO_docs

Per esportare Decision Records:

```powershell
.\notion_bridge.ps1 -Action exportchildrenmdindex `
  -PageId "https://www.notion.so/Decision-Records-34d9c1676366808ca8c0de29e507845d?source=copy_link" `
  -OutDir "C:\Users\oldkn\Documents\ARCONTIO_private_tools\notion_exports\Decision_Records" `
  -TitlePrefix "ARC-DEC-"
```

Poi copiare i file generati in:

```text
ARCONTIO_docs/00_Costituzione/Decision_Records/
```

e committare nella repository documentale.

---

## 8. Matrix decisionale rapida

| Evento                      | TASKBOARD_CODEX | CODEX_CONTEXT | CODEX_PROTOCOL | REPO_MAP | AI_SESSION_BOOT | Roadmap | Notion     |
| --------------------------- | --------------- | ------------- | -------------- | -------- | --------------- | ------- | ---------- |
| Micro-step interno          | no / forse      | no            | no             | no       | no              | no      | no / forse |
| Cambio checkpoint           | sì              | no            | no             | no       | no              | no      | sì         |
| Cambio macro job            | sì              | forse         | no             | no       | forse           | sì      | sì         |
| Nuova regola architetturale | forse           | sì            | forse          | no       | forse           | no      | sì         |
| Cambio workflow Codex       | sì              | no            | sì             | no       | forse           | no      | sì         |
| Nuova cartella core         | forse           | forse         | no             | sì       | no              | no      | forse      |
| Chiusura checkpoint         | sì              | no            | no             | no       | no              | forse   | sì         |
| Chiusura macro job          | sì              | forse         | no             | forse    | forse           | sì      | sì         |
| Nuovo Decision Record       | forse           | forse         | no             | no       | forse           | no      | sì         |
| Nuovo comando bridge        | no              | no            | sì             | no       | no              | no      | forse      |

---

## 9. Regola di non duplicazione

Non ogni documento deve contenere tutto.

* `TASKBOARD_CODEX.md` = cosa stiamo facendo ora a livello di macro cantiere.
* `CODEX_CONTEXT.md` = perché il progetto funziona così.
* `CODEX_PROTOCOL.md` = come deve comportarsi Codex.
* `REPO_MAP.md` = dove sono le cose.
* `AI_SESSION_BOOT.md` = come leggere tutti gli altri file.
* Notion = dashboard e memoria viva.
* `ARCONTIO_docs` = archivio canonico versionato.

Se un'informazione appartiene a un solo livello, non duplicarla altrove salvo riferimento sintetico.

---

## 10. Regola finale

Al cambio di task, la domanda obbligatoria è:

> Un agente AI che apre ora la repository capirebbe cosa fare leggendo solo i file root?

Se la risposta è no, aggiornare almeno `TASKBOARD_CODEX.md`.

Se il task cambia regole, architettura o mappa, aggiornare anche gli altri file pertinenti.

Se il task è solo un micro-passaggio interno dentro un macro job già ben definito, basta il report Codex e la coerenza Git fino al checkpoint successivo.
