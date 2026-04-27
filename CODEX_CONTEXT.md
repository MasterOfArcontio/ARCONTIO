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

* conoscenza soggettiva
* propagazione ritardata delle informazioni
* incertezza
* persistenza della memoria
* distorsione della comunicazione
* catene causali deterministiche

L'obiettivo a lungo termine del progetto è l'emersione di comportamento sociale, normativo e istituzionale da interazioni sistemiche di basso livello.

---

## 2. REGOLE ARCHITETTURALI NON NEGOZIABILI

### 2.1 No God Managers

Evitare manager monolitici tutto-in-uno.
Preferire sistemi granulari con responsabilità stretta.

### 2.2 World come Source of Truth Runtime

`World` deve essere trattato come contenitore canonico dello stato oggettivo della simulazione.

Le mutazioni possono essere distribuite tra più sottosistemi, ma il riferimento ontologico del mondo simulato resta `World`.

Le mutazioni non devono generare sorgenti parallele di verità non dichiarate o cache autoritative silenziose.

Non introdurre sorgenti parallele di verità non dichiarate.

### 2.3 Disciplina Deterministica a Tick

La simulazione ARCONTIO è tick-based e il tick discreto è parte del contratto architetturale.

L'ordine di esecuzione della simulazione conta.

Non introdurre comportamento async incontrollato, update nascosti o mutazioni di stato non deterministiche.

Ogni nuova integrazione deve chiedersi in quale punto della pipeline temporale sta realmente intervenendo.

### 2.4 SimulationHost come Orchestratore Temporale

`SimulationHost` governa:

* tick loop,
* scheduler,
* pipeline eventi,
* pipeline token,
* command pumping,
* telemetry hooks.

Deve rimanere orchestratore temporale e non diventare contenitore progressivo di logica gameplay permanente.

Bootstrap scenario, debug seed e helper locali non devono crescere fino a occultare la leggibilità del backbone runtime.

### 2.5 Tripartizione Systems / Rules / Commands

La simulazione deve essere letta secondo una tripartizione funzionale stabile:

* `Systems` = meccaniche tick-based e aggiornamenti sistemici
* `Rules` = valutazione reattiva e produzione di conseguenze logiche
* `Commands` = mutazioni deliberate applicate al mondo

Queste responsabilità possono dialogare ma non devono collassare in un unico blob operativo senza motivo.

Quando si introduce nuovo comportamento, verificare sempre in quale dei tre layer appartiene naturalmente.

### 2.6 Causalità Event Driven

I cambiamenti del mondo dovrebbero convergere verso record canonici `IWorldEvent` quando rappresentano fatti capaci di generare memoria, reputazione, comunicazione o conseguenze sociali.

I sottosistemi dovrebbero reagire alle conseguenze degli eventi invece di interrogare arbitrariamente la verità del mondo quando ragionevolmente possibile.

Se un fatto è world-significant, deve tendere a diventare evento world-level leggibile.

### 2.7 Cognizione NPC Soggettiva

Le decisioni NPC devono basarsi su:

* memory traces
* belief confidence
* communicated tokens
* percezione locale

Non reintrodurre mai scorciatoie onniscienti in modo silenzioso.

La disponibilità oggettiva di un dato nel `World` non implica automaticamente liceità cognitiva di usarlo nel decision layer.

### 2.8 Disciplina di Accesso al Mondo

Non introdurre ampie letture globali dirette se è possibile un'integrazione modulare locale.

Preferire coerenza delle dipendenze rispetto alla comodità delle scorciatoie.

Le scansioni globali del `World` sono tollerabili per meccaniche sistemiche, non come sostituto sistematico della conoscenza NPC.

### 2.9 Espansione Additiva tramite Rules

Preferire registries, file rule, contratti tipizzati ed evaluator additivi rispetto a giant switch statements.

### 2.10 Editing Chirurgico Preferito

Quando si modifica codice:

* preservare le convenzioni di naming,
* evitare rewrite non necessari,
* evitare di sostituire architettura funzionante con astrazioni non correlate.

### 2.11 La Separazione dei Layer Deve Essere Preservata

Mantenere distinzione tra:

* decision layer
* job execution layer
* world simulation systems
* event propagation
* memory/perception/belief layers
* UI/debug bridges

Non collassare responsabilità tra layer senza richiesta esplicita.

---

## 3. MACRO SISTEMI ATTUALMENTE IMPLEMENTATI

Implementati o sostanzialmente implementati:

* physiological needs decay
* comfort and environmental pressure base
* landmark/pathfinding base layers
* memory encoding pipeline
* belief store base
* decision intent weighted selection
* ownership and reservation base contracts
* explainability layer runtime panels
* EL tracing infrastructure
* tick/event/token/command runtime backbone

Parzialmente implementati / in evoluzione:

* advanced subjective pathfinding reasoning
* full world event normalization
* normative judgment
* crime social propagation
* communication distortion expansion
* social obligation systems
* world snapshot persistence
* deeper job execution backbone
* mutation authority tightening

Pianificati per il futuro:

* institutional judiciary
* economy depth
* family structures
* leadership/politics
* ritual/religious behavior

---

## 4. NOTE STRUTTURALI REPOSITORY

La logica principale gameplay risiede principalmente sotto:

`Assets/Scripts/Core`

Famiglie architetturali ricorrenti importanti includono:

* Runtime
* World
* Commands
* Components
* Events
* Systems
* Rules
* Decision
* Jobs
* Memory
* Beliefs
* Explainability
* ViewModels

Le aree `Runtime`, `World`, `Events`, `Commands`, `Rules` e `Systems` costituiscono il backbone sensibile della simulazione e richiedono audit più severo prima di patch.

UI e pannelli debug devono rimanere consumer dello stato di simulazione,
non autorità nascoste della simulazione.

Le Resources possono contenere alcuni asset legacy intenzionalmente nominati con suffisso `_old`.
Questi non sono necessariamente removibili e non devono essere modificati salvo richiesta esplicita.

La documentazione di design estesa, i documenti costituzionali, i documenti funzionali, gli engineering logs, i prompt templates e le tabelle reference vivono nella repository GitHub separata `ARCONTIO_docs` e nel workspace Notion.

Questa repository codice deve contenere solo documentazione direttamente utile per navigazione codice, comportamento agente e sicurezza implementativa.

---

## 5. REGOLE DI LAVORO AGENTE

Non lavorare mai sul branch `main`.

Il lavoro AI repository si assume su:

`ai/codex-main`

salvo modifica esplicita.

I task implementativi o audit significativi devono normalmente vivere su:

`ai-task/v0.xx.yy-short-description`

fino a PR e merge su `ai/codex-main`.

Più micro-step coerenti appartenenti allo stesso checkpoint possono vivere sul medesimo `ai-task` branch fino a chiusura del blocco.

Non modificare o committare:

* file `.meta` salvo necessità strettamente generata da Unity
* `Library`
* `Temp`
* `Obj`
* altre cartelle cache generate

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

* scopo architetturale,
* flusso interno,
* assunzioni critiche,
* punti delicati di integrazione.

La logica interna importante non deve restare opaca.

Il codice deve rimanere comprensibile per futura ispezione umana.

---

## 7. COMPORTAMENTO OBBLIGATORIO ALLA PRIMA SESSIONE

Alla prima lettura della repository:

* ispezionare l'architettura prima di modificare,
* identificare moduli implementati vs parziali,
* identificare inconsistenze legacy,
* identificare hotspot di technical debt,
* identificare il macro job attivo e il checkpoint corrente,
* proporre la sequenza implementativa più sicura.

Non iniziare codificando feature alla cieca.
