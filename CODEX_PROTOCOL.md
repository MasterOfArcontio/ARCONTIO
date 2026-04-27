# CODEX_PROTOCOL.md — Protocollo Workflow AI ARCONTIO

Questo file definisce la disciplina permanente di workflow per le operazioni Codex dentro ARCONTIO.

---

# 1. Disciplina workflow branch

Branch stabile:
`main`

Branch di integrazione AI:
`ai/codex-main`

Branch task temporanei Codex:
`ai-task/v0.xx.yy-short-description`

Ciclo di vita corretto:

1. leggere bootstrap e contesto del macro job
2. verificare `ai/codex-main`
3. aprire o verificare branch temporaneo `ai-task`
4. ispezionare i file coinvolti
5. audit se necessario
6. patchare su branch temporaneo
7. mostrare il diff
8. commit/push
9. creare PR verso `ai/codex-main`
10. l'operatore revisiona/mergea
11. il branch temporaneo può essere eliminato

Codex non deve accumulare branch task storici long-lived.

Non deve lavorare direttamente su `main`.

Non è necessario creare un branch nuovo per ogni micro-step minimo se il blocco tecnico resta coerente dentro lo stesso macro checkpoint.

Il cambio branch è normalmente richiesto quando:

* cambia il macro obiettivo tecnico,
* cambia il checkpoint in modo netto,
* cambia il dominio architetturale toccato,
* il diff richiede una PR separata e leggibile.

---

# 2. File obbligatori da leggere prima dell'implementazione

Codex deve ispezionare:

* AI_SESSION_BOOT.md
* CODEX_CONTEXT.md
* TASKBOARD_CODEX.md
* REPO_MAP.md
* ARCONTIO_Roadmap_Notion.md

prima di iniziare ogni implementazione non banale.

Se il task tocca architettura, governance, costituzione o blocchi roadmap sensibili, devono essere inoltre letti:

* documenti ARC-CON pertinenti
* documenti ARC-DEC pertinenti
* eventuali documenti funzionali collegati

quando disponibili in `ARCONTIO_docs`.

Se il task è architetturale, la modalità audit-first è obbligatoria.

Se il task è semplice micro-passaggio interno, può bastare il contesto già letto nella sessione corrente purché il macro job non sia cambiato.

Ogni richiesta locale deve comunque essere interpretata sotto:

`macro job attivo -> checkpoint corrente -> task locale richiesto`

e non come task isolato.

---

# 3. Disciplina patch

Codex deve preferire:

* patch più piccola praticabile e sicura,
* minimo numero di file toccati,
* nessuna pulizia opportunistica,
* nessuna riscrittura encoding,
* nessun formatting churn,
* nessun line ending churn,
* nessuna riscrittura commenti se non necessaria.

Quando sposta blocchi di codice, deve preservare i commenti originali esattamente quando possibile.

Deve mantenere il focus sul macro obiettivo evitando fix collaterali dispersivi.

Deve inoltre evitare di disperdere il lavoro su fix laterali se il checkpoint corrente richiede ancora una chiusura coerente del blocco principale.

---

# 4. Regola audit-first

Per questi task Codex deve fare audit prima di codificare:

* runtime architecture
* scheduler ordering
* pathfinding
* memory systems
* job systems
* AI decision systems
* save/load
* world contracts
* telemetry/explainability

L'output audit deve identificare:

* file,
* contratti correnti,
* punti di accoppiamento,
* candidato patch più sicuro.

Solo dopo può patchare.

---

# 5. Contratto output/reporting

Dopo ogni implementazione Codex deve riportare:

## File modificati

elenco esatto

## Cosa è cambiato

sintesi chirurgica

## Impatto runtime

cosa cambia / cosa è preservato

## Rischi

possibili effetti collaterali

## Stato Git

diff / commit / stato PR

## Disciplina documentale consigliata

* checkpoint/root update recommended sì/no
* ARCONTIO_docs alignment recommended sì/no

---

# 6. Consapevolezza documentale

`ARCONTIO_docs` è la repository esterna di memoria canonica del progetto.

Notion è la memoria viva navigabile.

I file root della repository codice costituiscono invece la memoria corta operativa degli agenti.

I micro-step interni non richiedono automaticamente sincronizzazione documentale.

Codex deve notificare esplicitamente:

`Checkpoint/root update recommended.`

quando cambia il contesto cognitivo operativo locale,

oppure

`ARCONTIO_docs alignment recommended.`

quando il passaggio ha reale valore documentale canonico.

I due livelli non vanno confusi.

---

# 7. Bridge Notion privato

Esiste un bridge Notion privato fuori da questa repository in:

`C:\Users\oldkn\Documents\ARCONTIO_private_tools\notion_bridge`

È usato per leggere, esportare e appendere note al workspace Notion ARCONTIO.

Codex può usare questo bridge solo quando richiesto esplicitamente dall'operatore.

## Operazioni consentite

Codex può eseguire:

```powershell
.\notion_bridge.ps1 -Action search
.\notion_bridge.ps1 -Action findtitle -Title "..."
.\notion_bridge.ps1 -Action children -PageId "..."
.\notion_bridge.ps1 -Action readpage -PageId "..."
.\notion_bridge.ps1 -Action append -PageId "..." -Text "..."
.\notion_bridge.ps1 -Action logstep -PageId "..." -Step "..." -Status "..." -Text "..."
.\notion_bridge.ps1 -Action exportchildrenmd -PageId "..." -OutDir "..."
.\notion_bridge.ps1 -Action exportchildrenmdindex -PageId "..." -OutDir "..." -TitlePrefix "ARC-DEC-"
```

## Regole di sicurezza

Codex non deve mai:

* leggere, stampare, copiare, esporre o committare `.env`
* mostrare il token Notion
* spostare il bridge nella repository
* modificare Notion salvo richiesta esplicita
* scrivere grandi cambiamenti documentali senza conferma dell'operatore
* usare il bridge proattivamente durante task di coding normali salvo richiesta esplicita dell'operatore

## Uso previsto

Il bridge può essere usato per:

* leggere la gerarchia Notion ARCONTIO
* trovare pagine roadmap o diario
* appendere note di completamento checkpoint
* appendere sintesi engineering log
* esportare sottopagine documentali verso markdown locale
* sincronizzare esiti implementativi con Notion

## Comportamento predefinito

Codex deve ricordare all'operatore quando la documentazione Notion dovrebbe essere aggiornata, ma non deve aggiornarla automaticamente salvo richiesta.
