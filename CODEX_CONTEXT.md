# CODEX_CONTEXT.md — Contesto Canonico Repository ARCONTIO

---

## 1. IDENTITÀ DEL PROGETTO

Arcontio è un progetto di simulazione sociale sistemica sviluppato in Unity/C#.

Non è un prototipo gestionale Unity convenzionale e non deve essere trattato come un generico colony sandbox.

La simulazione è costruita attorno al seguente principio causale:

PRESSIONE AMBIENTALE -> INSTABILITÀ DEI BISOGNI NPC -> AZIONE INDIVIDUALE -> CONSEGUENZA SOCIALE -> STRUTTURA EMERGENTE

Gli NPC non sono agenti onniscienti che agiscono sulla verità oggettiva del mondo.
Gli NPC agiscono su rappresentazioni soggettive interiorizzate, generate tramite percezione, memoria, comunicazione e confidence delle credenze.

Pertanto ogni implementazione deve preservare:

- conoscenza soggettiva
- propagazione ritardata delle informazioni
- incertezza
- persistenza della memoria
- distorsione della comunicazione
- catene causali deterministiche

L'obiettivo a lungo termine del progetto è l'emersione di comportamento sociale, normativo e istituzionale da interazioni sistemiche di basso livello.

---

## 2. REGOLE ARCHITETTURALI NON NEGOZIABILI

### 2.1 No God Managers
Evitare manager monolitici tutto-in-uno.
Preferire sistemi granulari con responsabilità stretta.

### 2.2 Causalità Event Driven
I cambiamenti del mondo generano record canonici `IWorldEvent`.
I sottosistemi dovrebbero reagire alle conseguenze degli eventi invece di interrogare arbitrariamente la verità del mondo quando ragionevolmente possibile.

### 2.3 Cognizione NPC Soggettiva
Le decisioni NPC devono basarsi su:
- memory traces
- belief confidence
- communicated tokens
- percezione locale

Non reintrodurre mai scorciatoie onniscienti in modo silenzioso.

### 2.4 Disciplina Deterministica a Tick
L'ordine di esecuzione della simulazione conta.
Non introdurre comportamento async incontrollato, update nascosti o mutazioni di stato non deterministiche.

### 2.5 Espansione Additiva tramite Rules
Preferire registries, file rule, contratti tipizzati ed evaluator additivi rispetto a giant switch statements.

### 2.6 Editing Chirurgico Preferito
Quando si modifica codice:
- preservare le convenzioni di naming,
- evitare rewrite non necessari,
- evitare di sostituire architettura funzionante con astrazioni non correlate.

### 2.7 La Separazione dei Layer Deve Essere Preservata
Mantenere distinzione tra:
- decision layer
- job execution layer
- world simulation systems
- event propagation
- memory/perception/belief layers
- UI/debug bridges

Non collassare responsabilità tra layer senza richiesta esplicita.

### 2.8 Evitare Accesso Globale Non Necessario
Non introdurre ampie letture globali dirette se è possibile un'integrazione modulare locale.

Preferire coerenza delle dipendenze rispetto alla comodità delle scorciatoie.

---

## 3. MACRO SISTEMI ATTUALMENTE IMPLEMENTATI

Implementati o sostanzialmente implementati:

- physiological needs decay
- comfort and environmental pressure base
- landmark/pathfinding base layers
- memory encoding pipeline
- belief store base
- decision intent weighted selection
- job system core execution
- ownership and reservation base contracts
- explainability layer runtime panels
- EL tracing infrastructure

Parzialmente implementati / in evoluzione:

- advanced pathfinding subjective reasoning
- normative judgment
- crime social propagation
- communication distortion expansion
- social obligation systems

Pianificati per il futuro:

- institutional judiciary
- economy depth
- family structures
- leadership/politics
- ritual/religious behavior

---

## 4. NOTE STRUTTURALI REPOSITORY

La logica principale gameplay risiede principalmente sotto:

`Assets/Scripts/Core`

Famiglie architetturali ricorrenti importanti includono:

- Commands
- Components
- Events
- Systems
- Rules
- Decision
- Jobs
- Memory
- Beliefs
- Explainability
- ViewModels

UI e pannelli debug devono rimanere consumer dello stato di simulazione,
non autorità nascoste della simulazione.

Le Resources possono contenere alcuni asset legacy intenzionalmente nominati con suffisso `_old`.
Questi non sono necessariamente removibili e non devono essere modificati salvo richiesta esplicita.

La documentazione di design estesa, i documenti costituzionali, i documenti funzionali, gli engineering logs, i prompt templates e le tabelle reference possono vivere nella repository GitHub separata `ARCONTIO_docs`.

Questa repository codice deve contenere solo documentazione direttamente utile per navigazione codice, comportamento agente e sicurezza implementativa.

---

## 5. REGOLE DI LAVORO AGENTE

Non lavorare mai sul branch `main`.
Il lavoro AI repository si assume su `ai/codex-main` salvo modifica esplicita.

Non modificare o committare:
- file `.meta` salvo necessità strettamente generata da Unity
- `Library`
- `Temp`
- `Obj`
- altre cartelle cache generate

Prima di ogni implementazione non banale:

1. ispezionare dipendenze toccate,
2. ispezionare sistemi vicini,
3. spiegare i file che si intende toccare,
4. proporre piano patch,
5. poi implementare.

Preferire commit minimi e coerenti.

Non eseguire refactor ampi e speculativi senza richiesta esplicita.

La coerenza architetturale ha priorità sulla velocità implementativa.

---

## 6. REGOLE COMMENTI CODICE

Tutte le nuove classi, struct o funzioni principali introdotte o modificate in modo sostanziale dovrebbero ricevere commenti architetturali verbosi in italiano quando appropriato.

Usare commenti esplicativi molto leggibili per:

- scopo architetturale,
- flusso interno,
- assunzioni critiche,
- punti delicati di integrazione.

La logica interna importante non deve restare opaca.

Il codice deve rimanere comprensibile per futura ispezione umana.

---

## 7. COMPORTAMENTO OBBLIGATORIO ALLA PRIMA SESSIONE

Alla prima lettura della repository:

- ispezionare l'architettura prima di modificare,
- identificare moduli implementati vs parziali,
- identificare inconsistenze legacy,
- identificare hotspot di technical debt,
- proporre la sequenza implementativa più sicura.

Non iniziare codificando feature alla cieca.
