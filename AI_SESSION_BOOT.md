# AI_SESSION_BOOT.md — File Master di Bootstrap Sessione ARCONTIO

---

## 1. SCOPO DELLA SESSIONE

Questo file è il bootstrap contestuale master per ogni nuova sessione AI-assisted di ARCONTIO.

Esiste per evitare che Codex, ChatGPT o altri agenti ricostruiscano da zero lo stato del progetto a ogni conversazione.

Non assumere le chat precedenti come memoria persistente affidabile.

Usa questo file come layer di orientamento principale.

---

## 2. ORDINE OBBLIGATORIO DI LETTURA DEL CONTESTO

Prima di ogni ragionamento, audit o implementazione non banale, leggi e assimila in questo esatto ordine:

1. `TASKBOARD_CODEX.md`
   = verità operativa corrente, macro job attivo, checkpoint corrente, campagne congelate, priorità ingegneristica attuale.

2. `CODEX_PROTOCOL.md`
   = disciplina di workflow, protocollo branch, regole audit-first, contratto di reporting, governance del bridge Notion.

3. `CODEX_CONTEXT.md`
   = identità architetturale permanente, principi non negoziabili della simulazione, regole di cognizione sistemica.

4. `REPO_MAP.md`
   = mappa di navigazione della repository e indizi di accoppiamento tra cartelle.

5. Documenti costituzionali e decisionali rilevanti da `ARCONTIO_docs` / Notion:

   * documenti ARC-CON
   * documenti ARC-DEC
   * documenti funzionali collegati al modulo toccato

6. `ARCONTIO_Roadmap_Notion.md`
   = sequenziamento macro di implementazione a lungo termine e collocazione cronologica del task corrente.

7. `ARCONTIO_Workflow_Operativo_AI_Git_Notion.md`
   = policy di workflow operativo, regole di branch, regole di pubblicazione, regole di sincronizzazione documentale.

Nessuna implementazione deve iniziare prima che questa gerarchia contestuale sia assimilata.

---

## 3. SNAPSHOT DELLO STATO CORRENTE DEL PROGETTO (APRILE 2026)

La fase corrente della repository non è una normale campagna di implementazione gameplay.

ARCONTIO è attualmente in una fase avanzata di preload costituzionale e consolidamento cognitivo della governance AI.

La macro campagna attiva `v0.09` riguarda il consolidamento del backbone runtime.
v0.09 è stata aperta dopo chiusura del preload costituzionale v0.08
e si innesta sui forensic findings già emersi.

Questo significa che ARCONTIO si trova in una fase di governance infrastrutturale, non di gameplay coding.

---

## 4. POSIZIONI CANONICHE DELLA MEMORIA DI PROGETTO

### Repository codice Unity (`ARCONTIO`)

Contiene:

* codice eseguibile
* sistemi runtime
* file operativi AI
* verità ingegneristica locale
* memoria corta di bootstrap cognitivo per gli agenti

### Repository documentale (`ARCONTIO_docs`)

Contiene:

* documenti costituzionali
* decision records
* specifiche funzionali
* patch log
* memoria architetturale AI
* mirror markdown canonico delle pagine istituzionali Notion

### Workspace Notion (`ARCONTIO NEW`)

Contiene:

* interfaccia leggibile di pianificazione
* dashboard umana
* pagine costituzionali vive
* taskboard viva
* diario di sviluppo

Notion non è l'unica fonte della verità.

Devono esistere progressivamente mirror markdown su GitHub.

La memoria del progetto va quindi letta come:

```text
memoria corta root repository + memoria lunga Notion/ARCONTIO_docs
```

e non come singolo contenitore unico.

---

## 5. COMPORTAMENTO AI NON NEGOZIABILE

Gli agenti devono ricordare:

* non iniziare coding cieco dal solo prompt utente;
* ricostruire prima lo stato corrente del progetto;
* leggere il task locale sotto la gerarchia `macro job attivo -> checkpoint corrente -> task richiesto`;
* preservare i vincoli anti-omniscience;
* preservare la disciplina deterministica a tick;
* preservare la separazione dei layer;
* preferire audit-first sui moduli architetturali;
* preferire patch chirurgiche;
* notificare quando è consigliato allineare checkpoint/root files;
* notificare quando è consigliato allineare ARCONTIO_docs.

---

## 6. DOCUMENTI PRIORITARI DI CONTESTO CORRENTE

Nella fase corrente, i seguenti documenti istituzionali sono considerati contesto ad alta priorità:

* Decision Records ARC-DEC-001 fino all'ultimo attivo
* ARC-CON-010 Architettura Mondo e Simulazione
* pagine costituzionali attive sotto 00 — COSTITUZIONE
* Taskboard Operativa AI
* registry ARCONTIO_docs corrente quando disponibile
* roadmap v0.09 aggiornata

Gli agenti devono chiedere o ispezionare esplicitamente questi documenti quando toccano moduli sensibili all'architettura.

---

## 7. AUTOCONTROLLO DI SESSIONE PRIMA DELL'AZIONE

Prima di codificare o proporre cambiamenti architetturali, l'agente deve rispondere internamente:

* qual è il macro job attivo?
* qual è il checkpoint corrente?
* questa è una fase di gameplay coding o di governance infrastrutturale?
* quali documenti costituzionali vincolano questo task?
* quali decision records vincolano questo task?
* quali sistemi adiacenti vengono toccati?
* il task appartiene a semplice micro-step interno o a checkpoint significativo?
* è probabile che serva allineamento checkpoint/root files?
* è probabile che serva allineamento ARCONTIO_docs / Notion?

Se questi punti non sono chiari, ispezionare prima.

---

## 8. REGOLA FINALE

Questo file è il punto di ingresso cognitivo principale.

Ogni futuro file di contesto AI deve essere interpretato attraverso questa gerarchia di bootstrap, non come documento isolato.
