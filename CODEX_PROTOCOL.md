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

Ciclo di vita:

1. leggere il contesto del task
2. ispezionare i file coinvolti
3. patchare su branch temporaneo `ai-task`
4. mostrare il diff
5. creare PR verso `ai/codex-main`
6. l'operatore revisiona/mergea
7. il branch temporaneo può essere eliminato

Codex non deve accumulare branch task storici long-lived.

---

# 2. File obbligatori da leggere prima dell'implementazione

Codex deve ispezionare:

- AI_SESSION_BOOT.md
- CODEX_CONTEXT.md
- TASKBOARD_CODEX.md
- REPO_MAP.md

prima di iniziare ogni implementazione non banale.

Se il task è architetturale, la modalità audit-first è obbligatoria.

---

# 3. Disciplina patch

Codex deve preferire:

- patch più piccola praticabile e sicura,
- minimo numero di file toccati,
- nessuna pulizia opportunistica,
- nessuna riscrittura encoding,
- nessun formatting churn,
- nessun line ending churn,
- nessuna riscrittura commenti se non necessaria.

Quando sposta blocchi di codice, deve preservare i commenti originali esattamente quando possibile.

---

# 4. Regola audit-first

Per questi task Codex deve fare audit prima di codificare:

- runtime architecture
- scheduler ordering
- pathfinding
- memory systems
- job systems
- AI decision systems
- save/load
- world contracts
- telemetry/explainability

L'output audit deve identificare:

- file,
- contratti correnti,
- punti di accoppiamento,
- candidato patch più sicuro.

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
diff / stato PR

---

# 6. Consapevolezza documentale

`ARCONTIO_docs` è la repository esterna di memoria del progetto.

Quando l'implementazione modifica architettura o convenzioni di progetto,
Codex deve notificare esplicitamente:

"ARCONTIO_docs alignment recommended."

---

# 7. Bridge Notion privato

Esiste un bridge Notion privato fuori da questa repository in:

`C:\Users\oldkn\Documents\ARCONTIO_private_tools\notion_bridge`

È usato per leggere e appendere note al workspace Notion ARCONTIO.

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
```

## Regole di sicurezza

Codex non deve mai:

- leggere, stampare, copiare, esporre o committare `.env`
- mostrare il token Notion
- spostare il bridge nella repository
- modificare Notion salvo richiesta esplicita
- scrivere grandi cambiamenti documentali senza conferma dell'operatore
- usare il bridge proattivamente durante task di coding normali salvo richiesta esplicita dell'operatore

## Uso previsto

Il bridge può essere usato per:

- leggere la gerarchia Notion ARCONTIO
- trovare pagine roadmap o diario
- appendere note di completamento step
- appendere sintesi engineering log
- sincronizzare esiti implementativi con Notion

## Comportamento predefinito

Codex deve ricordare all'operatore quando la documentazione Notion dovrebbe essere aggiornata, ma non deve aggiornarla automaticamente salvo richiesta.
