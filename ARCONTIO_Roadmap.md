# ARCONTIO — Development Roadmap

> **Ritmo di lavoro:** 3 sessioni/settimana (Lunedì, Mercoledì, Giovedì) · 2 ore per sessione · 6 ore/settimana
> **Target v1.00:** Prima demo giocabile pubblica
> **Stato documento:** Maggio 2026

---

## Mappa versioni

| Versione | Focus principale | Periodo stimato | Stato |
|----------|-----------------|-----------------|-------|
| v0.01 | Sistemi base: perception, memory, token | Completata | - |
| v0.02 | Sistemi base 2 | Completata | - |
| v0.03 | Pathfinding landmark + ComplexEdge + failure ladder | Aprile 2026 | Completata |
| v0.04 | NpcDnaProfile · NpcProfile · Needs System · BeliefStore | Aprile 2026 | Completata con residui parziali documentati |
| v0.05 | Decision Layer baseline / NeedsDecision bridge | Aprile 2026 | Parziale legacy |
| v0.05.5 | EL-MBQD Runtime UI Registry + pannello laterale | Aprile 2026 | Completata |
| v0.06 | Job System + Step System | Aprile 2026 | Completata ma da reintegrare |
| v0.07 | Explainability Layer v0.06: Job / Phase / Step / Command | Posticipata |
| v0.08 | Repository Stabilization + Constitutional Documentation Bootstrap | Aprile 2026 | IN CORSO |
| v0.09 | Simulation Backbone Hardening & Constitutional Alignment | Maggio 2026 | Completata |
| v0.10 | World Persistence Closure & Save/Load Completion | Maggio 2026 | Completata |
| v0.11A | Job Backbone Reintegration | Maggio 2026 | Completata |
| v0.11B | Decision Architecture (MBQD) Foundation | Maggio 2026 | Completata |
| v0.11C | Decision Orchestrator & Temporal Runtime Foundation | Maggio 2026 | Completata fino a v0.11c.06 |
| v0.11D | Runtime Infrastructure & Dormant Systems Forensic Reintegration | Maggio-Giugno 2026 | Completata |
| v0.12 | Pulizia Logging, Explainability e Diagnostica Runtime | Giugno 2026 | Completata |
| v0.13 | Chiusura MBQD/Incarichi e pensionamento NeedsDecisionRule | Giugno 2026 | Completata |
| v0.14 | Job Recovery Runtime e fallback degli incarichi | Giugno 2026 | Prossima fase |
| v0.15 | Cognizione Soggettiva Avanzata | Giugno-Luglio 2026 | Pending |
| v0.16 | Conseguenze Sociali Emergenti | Luglio 2026 | Pending |
| v0.17 | Observer Layer Pubblico ed Explainability Esterna | Luglio-Agosto 2026 | Pending |
| v1.00 | Prima demo giocabile pubblica | TBD | Target |

---

## v0.03 — Chiusura Landmark Pathfinding

**Obiettivo:** Liquidare il debito tecnico accumulato e portare il sistema di navigazione a uno stato completo e stabile prima di costruire i layer NPC sopra.

**Sistemi già completati in v0.02:**
- Landmark pathfinding (macro-route + last-mile) ✅
- Direct Path con Commitment Percettivo ✅
- NpcObjectMemory (cibo community) ✅
- ComplexEdge strutture dati + recording ✅

### Tabella sessioni v0.03

| # | Giorno | Data stimata | Task | Sistema | Stato |
|---|--------|-------------|------|---------|-------|
| 1 | Lun | Apr 2026 | ComplexEdge: integrazione planner A* | Pathfinding | ✅ |
| 2 | Mer | Apr 2026 | ComplexEdge: overlay visivo giallo + test | Pathfinding | ✅ |
| 3 | Gio | Apr 2026 | Job GoTo: integrazione landmark + safe point | Job System | ✅ |
| 4 | Lun | Apr 2026 | Failure ladder: BackOff / Replan | Pathfinding | ✅ |
| 5 | Mer | Apr 2026 | Failure ladder: Blacklist edge | Pathfinding | ✅ |
| 6 | Gio | Apr 2026 | Stress test 10–50 NPC, tuning parametri | Performance | ✅ |
| 7 | Lun | Apr 2026 | Definition of Done v0.03: verifica criteri | QA | ✅ |
| 8 | Mer | Apr 2026 | Bug fix emergenti dallo stress test | QA | ✅ |
| 9 | Gio | Apr 2026 | Chiusura doc v0.03 + allineamento contesto | Documentazione | ✅ |

### Definition of Done v0.03

| Criterio | Stato |
|----------|-------|
| Landmark pathfinding attivabile da config | ✅ |
| Macro-route + last-mile funzionanti | ✅ |
| ComplexEdge recording e planner integration attivi | ✅ |
| Failure ladder operativa | ✅ |
| Stress test senza thrashing significativo | ✅ |

---

## v0.04 — Fondamenta NPC (DNA · Needs · BeliefStore)

**Obiettivo:** Costruire le strutture dati fondamentali di ogni NPC. Tutto il comportamento emergente dipende da questi layer.

### Tabella sessioni v0.04

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | NpcDnaProfile | Struttura C# completa con tutti i campi | ✅ |
| 2 | Mer | NpcProfile | CompetenceProfile + PreferenceProfile + ObligationProfile | ✅ |
| 3 | Gio | NpcProfile | AssignedRole + serializzazione JSON | ✅ |
| 4 | Lun | NpcProfile | Calcolo distanza DNA↔NpcProfile | ✅ |
| 5 | Mer | NpcProfile | Integrazione con NPC esistenti | ✅ |
| 6 | Gio | Debug | Overlay debug distanza DNA↔NpcProfile | ✅ |
| 7 | Lun | Needs | Struttura Need generica con NeedAlert + NeedCritical | ✅ |
| 8 | Mer | Needs | Fame · Sete · Riposo/Sonno | ✅ |
| 9 | Gio | NpcProfile | PhysicalProfile mutabile | ⏳ |
| 10 | Lun | Needs | Salute fisica · Comfort termico | ⚠️ Parziale |
| 11 | Mer | Needs | Needs psicologici: Sicurezza · Stabilità · Socialità | ✅ |
| 12 | Gio | Needs | Decay system rapido/lento | ✅ |
| 13 | Lun | Debug | Overlay debug needs per NPC | ✅ |
| 14 | Mer | BeliefStore | BeliefEntry struttura completa | ✅ |
| 15 | Gio | BeliefStore | Aggregazione lazy da MemoryStore | ✅ |
| 16 | Lun | BeliefStore | Decay confidence | ✅ |
| 17 | Mer | BeliefStore | Invalidazione su job fallito | ✅ |
| 18 | Gio | BeliefStore | Query API Decision Layer | ✅ |
| 19 | Lun | QA | Test BeliefStore vs MemoryStore | ✅ |

### Definition of Done v0.04

| Criterio | Stato |
|----------|-------|
| NpcDnaProfile completo e serializzabile | ✅ |
| NpcProfile con assi principali attivi | ✅ |
| Needs fisiologici e psicologici baseline attivi | ✅ |
| BeliefStore attivo con decay, invalidazione e query MVP | ✅ |
| Nessuna violazione onnisciente nel Belief layer | ✅ |
| PhysicalProfile e Health/Comfort completi | ⏳ |

> **Nota architetturale v0.04:** la fase è da considerarsi sostanzialmente completata. Restano incompleti `PhysicalProfile` e la piena definizione futura di `Health/Comfort`, ma il backbone cognitivo minimo NPC è presente e funzionante. Il Belief layer è oggi uno dei moduli più coerenti del repository.

---

## v0.05 — Decision Layer

**Obiettivo:** Implementare il cervello decisionale dell'NPC.

### Tabella sessioni v0.05

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Catalogo | Intenzioni: struttura enum + metadati per dominio | ⏳ Legacy |
| 2 | Mer | Fase 1 | Filtro ScheduleFrame + precondizioni fisiologiche | ⏳ Rinviato |
| 3 | Gio | Fase 1 | Filtro ObligationProfile + QuerySystem/BeliefStore | ⏳ Rinviato |
| 4 | Lun | Fase 1 | Filtro norme attive + SocialRisk | ⏳ Rinviato |
| 5 | Mer | Fase 2 | Scoring NeedUrgency continua | ⏳ Parziale |
| 6 | Gio | Fase 2 | CompetenceAffinity + PreferenceAffinity | ⏳ Rinviato |
| 7 | Lun | Fase 2 | ObligationPressure + floor obbligatorio | ⏳ Rinviato |
| 8 | Mer | Fase 2 | MemoryConfidence + CognitiveModulators | ⏳ Rinviato |
| 9 | Gio | Fase 3 | Weighted random top-N | ⏳ Rinviato |
| 10 | Lun | Fase 3 | Integrazione Impulsività | ⏳ Rinviato |
| 11 | Mer | QA | Test end-to-end DNA → Decision | ⏳ Rinviato |
| 12 | Gio | QA | Verifica omniscience Decision Layer | ⏳ Rinviato |

### Definition of Done v0.05

| Criterio | Stato |
|----------|-------|
| Baseline decisionale need-driven presente | ✅ |
| Decision brain completo modulare | ⏳ |
| QuerySystem decisionale completo | ⏳ |
| Zero legacy bridge rule-based | ⏳ |

> **Nota architetturale v0.05:** il piano originario non è stato completato nella forma teorica prevista. Nel runtime attuale la decisione è sostenuta soprattutto da `NeedsDecisionRule` e da ponti legacy rule-driven. Questa fase va considerata parzialmente assorbita ma strutturalmente da riaprire dopo l'irrigidimento backbone.

---

## v0.05.5 — EL-MBQD Runtime UI Registry + pannello laterale

**Obiettivo:** Rendere osservabile in runtime il ciclo Memory → Belief → Query → Decision.

### Tabella sessioni v0.05.5

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Registry | Registry bounded per-NPC | ✅ |
| 2 | Mer | Emitters | Doppia uscita trace JSONL + registry | ✅ |
| 3 | Gio | ViewModel | Snapshot UI read-only | ✅ |
| 4 | Lun | UI | Pannello laterale tabbed | ✅ |
| 5 | Mer | UI | ScrollRect + sub-pannelli diagnostici | ✅ |
| 6 | Gio | QA | Copertura campi + test EditMode | ✅ |

### Definition of Done v0.05.5

| Criterio | Stato |
|----------|-------|
| Registry runtime MBQD attivo | ✅ |
| UI legge solo snapshot/ViewModel | ✅ |
| Tab diagnostici Memory/Belief/Decision attivi | ✅ |
| Nessun accesso onnisciente dalla UI | ✅ |

> **Nota architetturale v0.05.5:** milestone pienamente valida e stabile. Questo layer resta uno dei migliori strumenti di explainability del progetto.

---

## v0.06 — Job System + Step System

**Obiettivo:** Costruire il layer di esecuzione persistente.

### Tabella sessioni v0.06

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Job | JobRequest + Job + JobPlan + JobPhase | ✅ |
| 2 | Mer | Job | JobAction + StepResult | ✅ |
| 3 | Gio | Job | NpcJobState | ✅ |
| 4 | Lun | Job | JobArbiter | ✅ |
| 5 | Mer | Job | Macchina a stati | ✅ |
| 6 | Gio | Job | ReservationRecord | ✅ |
| 7 | Lun | Step | MoveTo · Reserve · Release · Wait | ✅ |
| 8 | Mer | Step | Observe · Search · PickUp · Drop | ✅ |
| 9 | Gio | Step | Consume · Communicate · Evaluate | ✅ |
| 10 | Lun | Pipeline | CommandBuffer integrazione | ✅ |
| 11 | Mer | Job | Preemption ladder + failure learning | ✅ |
| 12 | Gio | QA | Test end-to-end | ✅ |

### Definition of Done v0.06

| Criterio | Stato |
|----------|-------|
| Contratti Job/Phase/Step implementati | ✅ |
| Reservation e preemption attivi | ✅ |
| Failure learning MVP attivo | ✅ |
| Pipeline Step → Command funzionante | ✅ |
| Integrazione piena nel backbone runtime auditato | ⏳ |

> **Nota architetturale v0.06:** il layer è costruito ma il forensic audit ha mostrato che non è ancora perfettamente reincastonato nella backbone reale oggi centrata su systems/rules/commands legacy. La fase non va rifatta, ma va reintegrata dopo l'hardening.

---

## v0.07 — Explainability Layer v0.06 (Job / Phase / Step / Command)

**Obiettivo:** Estendere l'Explainability Layer al layer di esecuzione.

### Tabella sessioni v0.07

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Types | Payload EL Job layer | Pending |
| 2 | Mer | Emitters | Sink JSONL estesi | Pending |
| 3 | Gio | Bridge | Trace Decision → JobRequest | Pending |
| 4 | Lun | Job | Trace lifecycle job | Pending |
| 5 | Mer | Job | Trace JobPhase | Pending |
| 6 | Gio | Step | Trace Step/StepResult | Pending |
| 7 | Lun | StateMachine | Trace NpcJobState | Pending |
| 8 | Mer | Arbitration | Trace JobArbiter | Pending |
| 9 | Gio | Reservation | Trace ReservationStore | Pending |
| 10 | Lun | Commands | Trace JobCommandBuffer | Pending |
| 11 | Mer | Failure | Trace FailureLearning | Pending |
| 12 | Gio | Registry | Registry runtime esteso | Pending |
| 13 | Lun | ViewModel | Snapshot job execution | Pending |
| 14 | Mer | Timeline | Timeline combinata | Pending |
| 15 | Gio | UI | Tab Job completo | Pending |
| 16 | Lun | UI | Rifinitura tab Decision | Pending |
| 17 | Mer | QA | Test EditMode EL Job | Pending |
| 18 | Gio | QA | Scenario runtime completo | Pending |

> **Nota architetturale v0.07:** formalmente congelata. La explainability profonda del Job layer avrebbe scarso valore se costruita prima della chiusura causale della backbone.

---

## v0.08 — Repository Stabilization + Constitutional Documentation Bootstrap

**Obiettivo:** Stabilizzare governance repository, workflow AI e costruzione della memoria architetturale prima di nuovi macro refactor.

### Tabella sessioni v0.08

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Git | Installazione branch discipline ai/codex-main | ✅ |
| 2 | Mer | Git | Verifica cleanup branch storici | ✅ |
| 3 | Gio | Codex | Validazione primo task controllato | ✅ |
| 4 | Lun | Root Files | AGENTS + CODEX_PROTOCOL + TASKBOARD + REPO_MAP | ✅ |
| 5 | Mer | Notion | Pipeline sincronizzazione documentale base | ✅ |
| 6 | Gio | Costituzione | Principi Fondativi + Glossario + Decision Records | ✅ |
| 7 | Lun | Audit | ARC-CON-010 forensic audit backbone simulativa | ✅ |
| 8 | Mer | Roadmap | Riallineamento roadmap post audit | ✅ |
| 9 | Gio | Consolidamento | Chiusura documentazione costituzionale + root sync finale | ✅ |

### Definition of Done v0.08

| Criterio | Stato |
|----------|-------|
| Workflow Codex stabile | ✅ |
| Root AI files coerenti | ✅ |
| Decision Records istituzionalizzati | ✅ |
| Audit backbone completato | ✅ |
| Roadmap / Notion riallineati | ✅ |
| Apertura macro job tecnico successivo pronta | ✅ |

> **Nota architetturale v0.08:** v0.08 è la fase appena chiusa che ha prodotto il bootstrap cognitivo/documentale
necessario per l'apertura della campagna tecnica v0.09.

---

## v0.09 — Simulation Backbone Hardening & Constitutional Alignment

**Obiettivo:** Irrigidire la backbone simulativa esistente una volta chiusa la fase documentale v0.08.

### Tabella sessioni v0.09

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Audit | World Mutation Authority Audit & Seal | ✅ |
| 2 | Mer | Runtime | SimulationHost Decompression audit | ✅ |
| 3 | Gio | Runtime | Tick Phase Constitution formale | ✅ |
| 4 | Lun | Events | Audit totale ISimEvent / IWorldEvent | ✅ |
| 5 | Mer | Systems | Dormant/placeholder systems classification | ✅ |
| 6 | Gio | Patch | Prime sealing patch mutation authority | ✅ |
| 7 | Lun | Patch | SimulationHost legacy governance cleanup + neutral bootstrap | ✅ |
| 8 | Mer | QA | Re-audit backbone post patch + smoke test manuale | ✅ |
| 9 | Gio | Docs | ARC-CON-011 + sync documentale | ✅ |

### Definition of Done v0.09

| Criterio | Stato |
|----------|-------|
| Mutation authority mappata e parzialmente sigillata | ✅ |
| SimulationHost alleggerito | ✅ |
| Tick phases formalizzate | ✅ |
| Event architecture riallineata | ✅ |
| Dormant systems classificati | ✅ |

> **Nota architetturale v0.09:** questa è la prima vera campagna tecnica post-documentale. Non aggiunge feature gameplay; irrigidisce il nucleo causale già esistente per evitare che le fasi successive costruiscano su contratti molli.

>
> **Esito checkpoint:** completato l'hardening minimo della backbone con riallineamento dei world facts (`IWorldEvent`), ripristino della object lifecycle authority su depletion, correzione del ciclo eventi post-command in `SimulationHost`, introduzione del neutral bootstrap costituzionale e segregazione opt-in dei legacy debug scenarios. La sync documentale estesa resta separata dal completamento tecnico del checkpoint.
---

## v0.10 — World Persistence Closure & Save/Load Completion

**Obiettivo:** chiudere il debito storico di persistenza mondo/NPC con una baseline canonica `WorldSaveData` capace di salvare e ricaricare uno snapshot world-level runtime, preservando ID, tick, NPC, oggetti, food/object-use, belief e memorie soggettive pratiche.

**Milestone persistence closure baseline:** completata con commit tecnico `[Save] add canonical world snapshot save/load baseline` e smoke PlayMode roundtrip positivo. Il formato canonico world-level resta separato dai chunk NPC legacy e da `DevMapIO`; i controlli F3 sono strumenti DEV/DEBUG, non UI finale.

### Tabella sessioni v0.10

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Save | Audit completo world persistence gap | ✅ |
| 2 | Mer | Save | Oggetti + ownership persistence | ✅ |
| 3 | Gio | Save | Food stocks + global stores persistence | ✅ |
| 4 | Lun | Save | Landmark memory persistence | ✅ |
| 5 | Mer | Save | Tick/global state persistence | ✅ |
| 6 | Gio | Load | Distinzione scenario load vs save snapshot | ✅ |
| 7 | Lun | QA | Test save/load mondo completo | ✅ |

> **Nota:** questa fase assorbe ogni vecchio “cleanup save/load” sparso e lo ricolloca come campagna unica.

---

## v0.11 — Domain Reintegration split

> **Nota roadmap:** v0.11 è stata splittata in sottofasi per evitare di mescolare Job runtime, Decision architecture, temporal runtime e Work/Social audit. La v0.11 non è chiusa integralmente: sono chiuse v0.11A, v0.11B e il checkpoint v0.11c.01 della fase v0.11C.

### v0.11A — Job Backbone Reintegration

**Status:** COMPLETATA / ✅

**Scopo:** riattivare il Job System come runtime reale e tick-based.

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11.00 | Job System Forensic Audit | ✅ |
| v0.11.01 | Food Job Vertical Slice | ✅ |
| v0.11.02 | Generic Move Job Route | ✅ |
| v0.11.03 | Job Arbiter Runtime Activation | ✅ |
| v0.11.04 | Reservation Runtime Integration | ✅ |
| v0.11.05 | Job Runtime Snapshot | ✅ |
| v0.11.06 | NeedsDecisionRule Job Bridge | ✅ |

**Esito consolidato v0.11A:**

- Job backbone runtime riattivato su slice food e move;
- `JobArbiter`, preemption minima, reservation runtime e snapshot runtime resi vivi;
- bridge `NeedsDecisionRule` → Job mantenuto come percorso legacy/transitorio controllato;
- Decision Records `ARC-DEC-010` → `ARC-DEC-017` consolidati;
- `ARC-CON-011 — Architettura della Memoria NPC` prodotto/consolidato;
- test finali `FoodJobVerticalSliceQaTests`: 25/25 passed;
- branch task job backbone committato/pushato con commit `676b443`, `d0a844e`, `325fafd`, `cafb4fe`, `585279b`, `3046958`.

### v0.11B — Decision Architecture (MBQD) Foundation

**Status:** COMPLETATA / ✅

**Scopo:** progettare e avviare la pipeline `Memory → Belief → Query → Decision → Job`.

**Esito consolidato v0.11B:**

- primo loop runtime osservabile Decision → JobRequest → Job → Step → Command → World → Perception → Memory → Belief → next decision;
- recovery QA `v0.11b.05` reintegrata tramite PR #10;
- SearchFood perception-to-belief closure coperta da test;
- `ARC-CON-014` MBQD v1.0 consolidato;
- `ARC-DEC-018` formalizza azioni multi-tick e separazione delle cadence runtime.

### v0.11C — Decision Orchestrator & Temporal Runtime Foundation

**Status:** IN CORSO / NEXT: v0.11c.03a

**Scopo:** separare progressivamente orchestration decisionale, costruzione del contesto, routing intenzione→esecuzione, explainability decisionale e cadence runtime, senza trasformare `NeedsDecisionRule` in nuovo monolite e senza spostare nel Decision Layer autorità di preemption.

La v0.11C è la fase di fondazione che collega la nuova architettura decisionale al runtime temporale. I checkpoint sono volutamente piccoli: prima si separano responsabilità e confini, poi si introduce stato temporale volatile, poi si cabla il tick produttivo, e solo dopo si potrà trasformare movimento, reservation e lifecycle multi-tick reale.

#### v0.11c.01 — Decision Orchestrator Skeleton Audit / Foundation

**Stato:** COMPLETATA / ✅

Questo checkpoint ha chiuso la decomposizione iniziale di `NeedsDecisionRule` senza rimuoverlo e senza cablare il nuovo orchestrator come primary runtime. L'obiettivo non era cambiare comportamento, ma rendere espliciti i confini tra contesto decisionale, selezione intenzione, routing verso JobRequest ed explainability.

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11c.01a | Decision Orchestrator skeleton no-op | ✅ |
| v0.11c.01b | DecisionContextBuilder extraction | ✅ |
| v0.11c.01c | IntentExecutionRouter / JobRequestBuilder extraction | ✅ |
| v0.11c.01d | DecisionExplainabilityBridge extraction | ✅ |
| v0.11c.01e | NeedsDecisionRule compatibility shim | ✅ |
| v0.11c.01f | Orchestrator audit post-extraction / readiness | ✅ Assorbita in closeout v0.11c.01 |

**Esito consolidato v0.11c.01:**

- introdotto skeleton no-op del futuro `DecisionOrchestratorSystem`;
- separata la costruzione del `DecisionEvaluationContext` in `DecisionContextBuilder`;
- separato il boundary SelectedDecision → JobRequest in `IntentExecutionRouter` / `JobRequestBuilder`;
- separato il boundary di explainability decisionale in `DecisionExplainabilityBridge`;
- dichiarato `NeedsDecisionRule` come compatibility shim / legacy transitional bridge;
- preservato il comportamento runtime esistente;
- nessun cablaggio produttivo dell'Orchestrator come primary;
- nessuna modifica a `SimulationHost`, `JobArbiter`, `JobRuntimeState` o `JobExecutionSystem`;
- nessuna implementazione di preemption, nuova cadence produttiva o migrazione fallback legacy.

**PR incluse in v0.11c.01:**

- PR #9 — `v0.11c.01a` Decision Orchestrator skeleton no-op;
- PR #11 — `v0.11c.01b` DecisionContextBuilder extraction;
- PR #12 — `v0.11c.01c` IntentExecutionRouter / JobRequestBuilder extraction;
- PR #13 — `v0.11c.01d` DecisionExplainabilityBridge extraction;
- PR #14 — `v0.11c.01e` NeedsDecisionRule compatibility shim.

**Validazione aggregata v0.11c.01:**

- `DecisionOrchestratorNoOpQaTests`: passed;
- `DecisionContextBuilderQaTests`: passed;
- `IntentExecutionRouterQaTests`: passed;
- `DecisionExplainabilityBridgeQaTests`: passed;
- `DecisionLayerQaTests`: passed;
- `SearchFoodJobVerticalSliceQaTests`: passed;
- `FoodJobVerticalSliceQaTests`: passed;
- `JobSystemEndToEndQaTests`: passed quando eseguiti nel blocco 01a;
- `MemoryBeliefDecisionRuntimeJobScenarioQaTests`: passed nei blocchi di recovery/01c/01d/01e.

#### v0.11c.02 - Multi-Tick Action Runtime

**Stato:** COMPLETATA / DONE

Questo checkpoint ha implementato la foundation temporale fissata da `ARC-DEC-020`: tick globale unico, progress interno volatile, distinzione tra atomic action e running action, nessuna posizione intermedia tra celle e mutazione del World solo a completamento. La chiusura 02i ha aggiunto la QA deterministica finale per assicurare che il gate traversal resti stretto, che il path legacy sia preservato quando il gate non si applica, e che cleanup running action/reservation resti stabile.

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11c.02a | Multi-Tick Runtime Audit + ARC-DEC-020 | ✅ |
| v0.11c.02b | RunningActionRuntimeState Skeleton | ✅ |
| v0.11c.02c | RunningActionExecutor Skeleton | ✅ |
| v0.11c.02d | RunningActionStore introduction | ✅ |
| v0.11c.02e | RunningAction productive ticking integration | ✅ |
| v0.11c.02f | RunningAction lifecycle explainability traces | ✅ |
| v0.11c.02g | Multi-tick cell traversal foundation | ✅ |
| v0.11c.02h | Temporal reservation robustness | ✅ |
| v0.11c.02i | Deterministic multi-tick QA sweep | ✅ |

**Esito consolidato v0.11c.02:**

- `v0.11c.02b` ha introdotto il vocabolario e lo stato volatile delle running action;
- `v0.11c.02c` ha introdotto un executor generico e passivo;
- `v0.11c.02d` ha introdotto lo storage produttivo volatile sotto `JobRuntimeState`;
- `v0.11c.02e` ha cablato ticking produttivo controllato su `WaitTicks`;
- `v0.11c.02f` ha reso osservabile il lifecycle delle running action;
- `v0.11c.02g` ha introdotto il traversal one-cell gated senza posizioni intermedie;
- `v0.11c.02h` ha aggiunto reservation temporale minima della destination cell e contention deterministica;
- `v0.11c.02i` ha chiuso la matrice QA deterministica con 81/81 test passed;
- `MovementSystem`, `SimulationHost`, save/load, scene e config restano fuori scope e invariati;
- il movimento multi-step, avoidance avanzata, reservation temporali complete e cadence separation restano futuri.

#### v0.11c.03 — Runtime Cadence Separation

**Stato:** FUTURA / PENDING

Questo checkpoint separerà le frequenze operative senza introdurre timeline parallele. La decision cadence, l'execution cadence e la futura planning cadence devono restare derivate dal tick globale canonico, con override emergenziali espliciti e tracciabili.

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11c.03a | Tick phase audit | Pending |
| v0.11c.03b | Cognitive decision cadence config | Pending |
| v0.11c.03c | Execution cadence separation | Pending |
| v0.11c.03d | Emergency override cadence | Pending |
| v0.11c.03e | Long-term planning placeholder cadence | Pending |
| v0.11c.03f | Cadence explainability traces | Pending |
| v0.11c.03g | Cadence QA matrix | Pending |

#### v0.11c.04 — Job Runtime Stabilization & Local Step Recovery Foundation

**Stato:** COMPLETATA / DONE

Questo checkpoint ha consolidato una foundation passiva per il recupero locale limitato degli step Job. Il focus non e' spostare decisione nel Job System, ma preparare vocabolario, DTO, boundary audit e QA prima di qualsiasi recovery produttivo.

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11c.04a | Job step failure/recovery audit | ✅ |
| v0.11c.04b | JobStepFailureKind vocabulary skeleton | ✅ |
| v0.11c.04c | StepRecoveryStrategy vocabulary skeleton | ✅ |
| v0.11c.04d | StepRecoveryPolicy passive model | ✅ |
| v0.11c.04e | JobRecoveryResult passive model | ✅ |
| v0.11c.04f | No-op recovery boundary audit | ✅ |
| v0.11c.04g | Recovery QA matrix | ✅ |
| v0.11c.04h | Closeout report | ✅ |

> **Nota closeout v0.11c.04h (2026-05-24):** il piano operativo reale di `v0.11c.04` e' stato riallineato a `Job Runtime Stabilization & Local Step Recovery Foundation` dopo l'introduzione di `ARC-DEC-021`. Il checkpoint e' chiuso come foundation passiva: audit recovery, `JobStepFailureKind`, `StepRecoveryStrategy`, `StepRecoveryPolicy`, `JobRecoveryResult`, boundary audit e Recovery QA matrix. Nessun recovery runtime reale e' stato introdotto; `StepResultStatus.Failed` resta terminale per il job e `Blocked` / `Waiting` restano wait gate tecnico. Dettaglio: `v0.11c.04_Closeout_Report.md`.

#### v0.11c.05 - Recovery Evaluation Foundation

## Stato
COMPLETATA / DONE

## Obiettivo

Preparare il futuro sistema di:

- classificazione fallimenti;
- valutazione recovery;
- retry locale limitato;
- escalation decisionale eventuale;

senza introdurre recovery reale.

---

## Filosofia architetturale

La recovery deve restare:

- locale;
- limitata;
- explainable;
- tipizzata;
- non onnisciente;
- non globale;
- non cognitiva.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11c.05a | Audit classificazione fallimenti | DONE |
| v0.11c.05b | Modello passivo StepFailureClassification | DONE |
| v0.11c.05c | Skeleton evaluator recovery no-op | DONE |
| v0.11c.05d | QA evaluator recovery | DONE |
| v0.11c.05e | Closeout recovery evaluation | DONE |

> **Nota closeout v0.11c.05e (2026-05-24):** il checkpoint `v0.11c.05` e' chiuso come Recovery Evaluation Foundation passiva. Sono stati completati audit classificazione failure, `StepFailureClassification`, `StepRecoveryEvaluator` no-op e matrice QA evaluator. Il runtime recovery reale NON e' implementato: `JobExecutionSystem` non usa l'evaluator, `JobStateMachine` mantiene `Failed -> JobFailed`, `Blocked` / `Waiting` restano wait gate tecnici e non esistono mapping produttivi. Dettaglio: `v0.11c.05_Closeout_Report.md`.

---

#### v0.11c.06 - Stabilizzazione Movimento Multi-Tick

## Stato
COMPLETATA / DONE

## Obiettivo

Stabilizzare il primo movimento Job multi-tick reale senza spegnere il movimento legacy.

La fase ha consolidato:

- traversal Job one-cell multi-tick;
- durata cella configurabile;
- mutazione posizione solo a completion;
- reservation cella destinazione durante progress;
- inventario dei path legacy ancora necessari.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11c.06a | Audit movimento legacy vs movimento temporale | DONE |
| v0.11c.06b | QA N tick per attraversamento cella | DONE |
| v0.11c.06c | Allineamento prenotazione cella e durata movimento | DONE |
| v0.11c.06d | Audit riduzione movimento legacy | DONE |
| v0.11c.06e | Inventario QA movimento legacy | DONE |
| v0.11c.06f | Closeout stabilizzazione movimento multi-tick | DONE |

> **Nota closeout v0.11c.06f (2026-05-25):** il checkpoint `v0.11c.06` e' chiuso come Stabilizzazione Movimento Multi-Tick. Il Job Layer puo' attraversare una cella in N tick configurabili, aggiornando la posizione solo a completion e mantenendo una reservation cella allineata alla durata reale. `MovementSystem` e `MoveIntent` restano attivi e necessari per path lunghi, landmark, local search, backoff, porte, debug movement e fallback legacy. La migrazione completa del movimento NON e' implementata. Dettaglio: `v0.11c.06_Closeout_Report.md`.

---

#### v0.11D - Runtime Infrastructure & Dormant Systems Forensic Reintegration

## Stato
COMPLETATA / DONE

## Obiettivo

Questa fase NON introduce gameplay importante.

Serve a stabilizzare l'infrastruttura runtime prima dell'espansione sistemica futura.

La fase ha lo scopo di:

- ridurre freeze e stalli runtime;
- stabilizzare logging, diagnostica ed explainability;
- mappare sistemi dormienti, lavoro e sociale;
- isolare debito legacy ancora vivo;
- pulire authority runtime duplicate;
- preparare multi-tick, sistemi sociali, sistemi lavoro e crescita NPC.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11d.00 | Stabilizzazione urgente logging e diagnostica runtime | DONE |
| v0.11d.01 | Audit completo diagnostica runtime ed explainability | DONE |
| v0.11d.02 | Consolidamento diagnostica runtime e pannelli | DONE |
| v0.11d.03 | Audit sistemi dormienti | DONE |
| v0.11d.04 | Audit sistemi lavoro | DONE |
| v0.11d.05 | Audit sistemi sociali | DONE |
| v0.11d.06 | Audit e cleanup authority runtime | DONE |
| v0.11d.07 | Stabilizzazione scheduler e prestazioni runtime | DONE |
| v0.11d.08 | Chiusura costituzionale infrastruttura runtime | DONE |

> **Nota closeout v0.11d.08 (2026-05-27):** la fase `v0.11D` e' chiusa come Runtime Infrastructure & Dormant Systems Forensic Reintegration. Ha stabilizzato logging, diagnostica, explainability runtime, memoria/debug, autorita' needs/eventi e audit residui. Non chiude i debiti strutturali di movimento legacy, `NeedsDecisionRule`, recovery Job produttiva, dizionari pubblici del `World`, pulizia completa logging/EL o sistemi sociali: questi restano esplicitamente rinviati alle fasi `v0.12`, `v0.13`, `v0.14`, `v0.15`, `v0.16` e `v0.17+`. Dettaglio: `v0.11d_Closeout_Report.md`.

---

#### v0.11d.00 - Stabilizzazione urgente logging e diagnostica runtime

## Stato
COMPLETATA / DONE

## Obiettivo

Eliminare le cause immediate di freeze legate a logging runtime e I/O, mantenendo registri runtime ed explainability.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11d.00a | Rimozione Console Unity / HTML / TXT dal logger runtime | DONE |
| v0.11d.00b | Introduzione scrittore JSONL batchato, limitato e congelabile | DONE |
| v0.11d.00c | Conversione e isolamento sink JSONL movimento e MBQD | DONE |
| v0.11d.00d | Configurazione logging runtime provvisoria pulita | DONE |
| v0.11d.00e | QA stress freeze/logging/saturazione | DONE |

---

#### v0.11d.06 - Audit e cleanup authority runtime

## Stato
COMPLETATA / DONE

## Obiettivo

Mappare e ridurre mutazioni silenziose, bypass di eventi, authority duplicate e letture onniscienti, senza introdurre refactor larghi.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11d.06a | Audit authority runtime reale | DONE |
| v0.11d.06b | Eventi mondo minimi per azioni needs | DONE |
| v0.11d.06c | QA chiusura authority/eventi needs | DONE |
| v0.11d.06d | Closeout cleanup authority runtime | DONE |

> **Nota v0.11d.06b:** `FoodConsumedEvent` e `BedRestedEvent` rendono osservabili consumo cibo e uso letto solo dopo mutazione riuscita. Non aggiungono nuovi consumatori evento, non modificano MBQD, Job, Movement, Save/Load o ordine tick.

> **Nota closeout v0.11d.06d (2026-05-27):** il checkpoint `v0.11d.06` e' chiuso come Runtime Authority / Needs World Events. La fase ha completato l'audit authority reale, introdotto eventi mondo minimi per consumo cibo e uso letto, recuperato i `.meta` Unity dei nuovi eventi e confermato che la patch e' behavior-preserving: nessun nuovo consumer implicito, nessuna modifica a bisogni, inventario, uso letto, MBQD, Job, Movement, Save/Load o ordine tick. Dettaglio: `v0.11d.06_Closeout_Report.md`.

---

#### v0.11D - Hardening memoria e GC runtime emerso durante la fase

## Stato
COMPLETATA / RESIDUI RINVIATI

## Obiettivo

Ridurre retention runtime, dati debug orfani e churn GC osservato dal Profiler, senza refactor larghi.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.11d.MEMORY-AUDIT | Audit crescita memoria runtime | DONE |
| v0.11d.MEMORY-FIX-01 | Pruning cooldown token `_lastShareTick` | DONE |
| v0.11d.MEMORY-HARDENING | Cleanup runtime debug memory retention | RINVIATO / NON BLOCCANTE |
| v0.11d.MEMORY-GC-01 | Riduzione allocazioni overlay diagnostico | DONE |

> **Nota memoria runtime:** il Profiler ha mostrato che il problema principale residuo non era piu' solo I/O logging, ma churn da overlay/debug e retention locale. La riduzione del rebuild testuale in `MapGridEntitySummaryOverlay` ha gia' mostrato un calo visibile degli spike di allocazione.

---

#### v0.12 - Pulizia Logging, Explainability e Diagnostica Runtime

## Stato
COMPLETATA / ✅

## Obiettivo

Chiudere il debito rimasto dopo la stabilizzazione infrastrutturale `v0.11D`.

Questa fase NON introduce gameplay importante.

Serve a trasformare logging, diagnostica ed explainability in un sistema unico, leggero, modulare e governabile.

La fase deve portare ARCONTIO verso una situazione in cui:

- i file JSONL siano l'unica uscita persistente dei log diagnostici runtime;
- i pannelli EL siano l'unica visualizzazione diagnostica viva utile;
- console Unity, TXT, HTML e overlay logger legacy vengano rimossi se non piu' necessari;
- `Telemetry` venga eliminata o assorbita solo dove produce valore reale;
- `ArcontioLogger` venga rimosso oppure trasformato in un ponte leggero sopra la stessa infrastruttura JSONL/EL;
- `GameParams` e `SimulationParams` vengano ricollocati in una configurazione unica e leggibile, non sparsa sotto cartelle di telemetria;
- EL non produca nulla quando e' disattivo;
- EL, quando e' attivo, produca dati con costo minimo e solo nei moduli richiesti.

---

## Filosofia architetturale

EL deve diventare un sistema complessivo, non una somma di registri isolati.

Internamente deve essere modulare per dominio:

- Memory;
- Belief;
- Query;
- Decision;
- Job;
- Running Action;
- Step;
- Movement.

I moduli Reservation, Command, Failure/Recovery e diagnostica di supporto possono restare sotto-canali collegati, se utili alla ricostruzione causale.

La regola principale e':

```text
EL spento -> nessuna produzione diagnostica
EL acceso -> produzione modulare, limitata, batchata, leggibile
```

Questa fase deve anche verificare se la struttura attuale dei pannelli EL consuma troppe risorse nel solo prodursi. Se i pannelli costruiscono stringhe, liste o viewmodel quando sono chiusi, nascosti o non selezionati, quel costo deve essere eliminato o ridotto.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.12a | Audit logging, explainability e diagnostica runtime | ✅ |
| v0.12b | Riallineamento roadmap e definizione fase diagnostica | ✅ |
| v0.12c | Consolidamento GameParams / SimulationParams | ✅ |
| v0.12d | Rimozione canali legacy console / TXT / HTML / overlay | ✅ |
| v0.12e | Decisione ArcontioLogger: rimozione o ponte JSONL/EL | ✅ |
| v0.12f | Eliminazione o assorbimento Telemetry | ✅ |
| v0.12g | EL modulare leggero e produzione zero quando disattivo | ✅ |
| v0.12h | Ottimizzazione pannelli EL e closeout diagnostica runtime | ✅ |

> **Nota closeout v0.12c (2026-05-27):** il checkpoint `v0.12c` e' chiuso come consolidamento della configurazione runtime. `game_params.json` resta il file portante e non e' stato modificato. `SimulationParams` e `GameParams` sono stati ricollocati sotto `Core/Config`; `SimulationHost` legge il file una sola volta; `SimulationParams` e' ora il modello principale del bootstrap e risolve anche la configurazione logger ordinaria. `GameParams.cs` resta temporaneamente come ponte compatibile, ma il percorso runtime ordinario non dipende piu' da lui. Dettaglio: `v0.12c_Closeout_Report.md`.

> **Nota v0.12d (2026-05-27):** rimossi fisicamente i canali legacy di output runtime `UnityConsoleSink`, `FileSink` TXT, `HtmlFileSink`, `UnityOverlaySink` e `ArcontioLogOverlay`, inclusi i relativi `.meta` Unity. `ArcontioLogger` non conosce piu' console, TXT, HTML o overlay; resta solo come ponte configurabile/no-op verso il consolidamento successivo `v0.12e`.

> **Nota closeout v0.12e (2026-05-29):** il checkpoint `v0.12e` e' chiuso come decisione controllata su `ArcontioLogger`. La scelta corrente e' mantenerlo come ponte transitorio per compatibilita' con chiamate storiche, ma non come logger runtime futuro. Il ciclo vita JSONL e' stato spostato nel servizio dedicato `RuntimeDiagnosticsLifecycle`; la localizzazione legacy del logger (`LocalizationDb` e `localization_logs.json`) e' stata rimossa; un primo gruppo di chiamate legacy sicure e' stato potato da comandi needs, loader config e audit/debug evento. Restano intenzionalmente fuori da questa chiusura i log movimento/landmark, i log scenario seed e i log sociali/furto, da assorbire in step successivi piu' mirati. Dettaglio: `v0.12e_Closeout_Report.md`.

> **Nota closeout v0.12f (2026-05-29):** il checkpoint `v0.12f` e' chiuso come assorbimento controllato di `Telemetry`. Il tipo resta temporaneamente nelle firme `ISystem` e `IRule` per evitare una migrazione larga, ma nel runtime ordinario viene inizializzato leggendo `logging.telemetry.enabled` e, con il default spento, non crea dizionari, non accumula contatori e non espone piu' scarico console. L'unico contatore con nome dinamico e' stato protetto per evitare costruzione stringa quando Telemetry e' spenta. La rimozione fisica completa dalle firme runtime resta un debito futuro, da affrontare solo quando EL modulare avra' una destinazione diagnostica sostitutiva. Dettaglio: `v0.12f_Closeout_Report.md`.

> **Nota closeout v0.12g (2026-05-29):** il checkpoint `v0.12g` e' chiuso come hardening del percorso EL disattivo. I registri Movement EL e MBQD non vengono piu' creati quando i rispettivi moduli sono spenti; l'emitter MBQD espone un gate economico per kind diagnostico e i producer principali lo usano prima di costruire trace, record o liste diagnostiche. Movement EL mantiene il gate esistente e non considera piu' un registry assente come tracciabile. Il comportamento simulativo non cambia: viene ridotta solo la produzione diagnostica quando EL non e' attivo. Dettaglio: `v0.12g_Closeout_Report.md`.

> **Nota closeout v0.12h (2026-05-29):** il checkpoint `v0.12h` chiude la fase `v0.12` come pulizia logging, explainability e diagnostica runtime. I pannelli EL restano vivi e leggibili, ma il pannello MBQD costruisce ora solo la famiglia diagnostica della pagina visibile; le card NPC estese restano costruite solo per NPC selezionato e sezioni aperte; gli stati senza selezione aggiornano solo la pagina attiva. Questo riduce churn UI e produzione testuale nascosta senza cambiare simulazione, tick, Job, Decision, Movement o Save/Load. Dettaglio: `v0.12h_Closeout_Report.md`.

> **Nota architetturale v0.12:** questa fase assorbe il debito emerso dal Profiler e dagli audit `v0.11D`. Non deve costruire nuova cognizione NPC. Prima di espandere memoria, belief, sociale e observer layer pubblico, bisogna rendere la diagnostica sostenibile: pochi canali, configurazione unica, JSONL come uscita persistente, pannelli EL come lettura viva, nessun costo nascosto quando il sistema e' spento.

---

#### v0.13 - Chiusura MBQD/Incarichi e pensionamento NeedsDecisionRule

## Stato
FUTURA / PENDING

## Obiettivo

Chiudere il debito tra MBQD e Job prima di aprire la cognizione soggettiva profonda.

Questa fase NON introduce nuove feature sociali o cognitive importanti.

Serve a rendere il percorso Decisione -> Richiesta di incarico -> Incarico il percorso ordinario unico, estrarre da `NeedsDecisionRule` solo le parti ancora utili, scollegare il vecchio ponte come autorita' runtime e ricostruire i bisogni minimi sopra JobRequest, Job scriptati a fasi e recovery locale configurabile.

La fase deve chiarire:

- chi decide;
- chi costruisce la richiesta di incarico;
- chi accetta o rifiuta l'incarico;
- come viene gestito il fallimento;
- come il fallimento torna verso memoria e credenze;
- quali parti di `NeedsDecisionRule` vanno conservate come servizi dedicati;
- quali fallback legacy vanno cancellati invece di preservati;
- come i fallback futuri vengono descritti da configurazione;
- quando `NeedsDecisionRule` smette di essere autorita' reale;
- quando il file puo' essere rimosso senza trascinare logica utile.

---

## Scopo architetturale

Questa fase chiude il tratto MBQD -> Job prima della cognizione soggettiva profonda.
Il focus non e' introdurre nuove feature sociali o cognitive, ma rendere il percorso Decisione -> JobRequest -> Job il percorso ordinario, estrarre da `NeedsDecisionRule` i servizi ancora utili, scollegare il ponte legacy come autorita' runtime, ricostruire i bisogni minimi sopra Job e introdurre la base configurabile per fallback e recupero locale degli step.

La motivazione e' strutturale: non conviene costruire lifecycle credenze, obsolescenza, conflitti cognitivi e apprendimento soggettivo sopra un bridge Decision/Job ancora incompleto.

La scelta operativa aggiornata e' accettare una transizione in cui il simulatore puo' non restare pienamente funzionante nel vecchio percorso legacy. Questo permette di eliminare piu' presto l'autorita' reale di `NeedsDecisionRule`, invece di mantenerla come paracadute indefinito. La rimozione deve comunque essere chirurgica: prima si conservano gli helper utili in servizi dedicati, poi si scollega la rule, poi si ricostruiscono i bisogni principali come incarichi.

La fase deve inoltre tenere conto dei documenti di supporto Notion letti il 2026-05-29:

- `ARCONTIO - Operational Job Step & Local Recovery Matrix`;
- `Belief Category e Intenzioni Decisionali`;
- `Reference: Funzionamento del Sistema Decisione, Query e Belief`;
- `Le Sorgenti delle Intenzioni`;
- `CENSIMENTO CATALOGHI`;
- `ARCHITETTURA SCALABILITA' PERCETTIVA`;
- `Ottimizzazioni del Catalogo delle Intenzioni per il Runtime`;
- `Cosa c'e' e cosa manca nel MBQD + JOB`;
- `Livello Narrativo per ARCONTIO`;
- `Azione sul Medio Periodo per ARCONTIO`.

Questi documenti non diventano automaticamente decisioni runtime produttive, ma orientano la sequenza: prima il tratto MBQD/Incarichi, poi fallback configurabili e recupero locale limitato, poi ottimizzazioni del catalogo intenzioni e solo dopo cognizione soggettiva profonda, sociale e narrativa.

---

## Fuori scope

In questa fase NON si deve:

- introdurre nuove feature sociali;
- cambiare Save/Load;
- cambiare Movement salvo necessita' strettamente collegata;
- introdurre planner globale;
- introdurre recovery intelligente completa;
- introdurre belief lifecycle profondo;
- introdurre sorgenti intenzionali proattive complete;
- introdurre Goal di medio periodo;
- introdurre narrative layer produttivo;
- emettere `ICommand` dal Decision Layer;
- spostare autorita' di preemption nel Decision Layer;
- trasformare `JobExecutionSystem` in un secondo sistema decisionale.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.13a | Audit responsabilita' residue NeedsDecisionRule | ✅ DONE |
| v0.13b | Estrazione servizi utili da NeedsDecisionRule | ✅ DONE |
| v0.13c | Scollegamento NeedsDecisionRule come autorita' runtime | ✅ DONE |
| v0.13d | Ricostruzione bisogni principali minimi via JobRequest/Job | ✅ DONE |
| v0.13e | Configurazione fallback/recovery locale da matrice Job | ✅ DONE |
| v0.13f | Fallimenti minimi e ritorno cognitivo leggero | ✅ DONE |
| v0.13g | Eliminazione definitiva NeedsDecisionRule legacy | ✅ DONE |
| v0.13h | QA e closeout pensionamento legacy | ✅ DONE |

> **Nota closeout v0.13h (2026-05-30):** la fase `v0.13` e' chiusa come Chiusura MBQD/Incarichi e pensionamento NeedsDecisionRule. Il percorso ordinario per Fame/SearchFood/EatKnownFood passa da Decisione -> Richiesta di incarico -> Incarico, i servizi utili sono stati estratti dal vecchio ponte, il fallback legacy needs e' stato rimosso e `NeedsDecisionRule` non esiste piu' come file runtime. Restano fuori scope la cognizione soggettiva profonda, l'obsolescenza delle credenze, la memoria da eventi needs, il sociale e la rimozione completa di tutti i concetti diagnostici legacy legati al bridge. Dettaglio: `v0.13h_Closeout_Report.md`.

---

#### v0.14 - Job Recovery Runtime e fallback degli incarichi

## Stato
FUTURA / PENDING

## Obiettivo

Rendere produttivo il recupero locale degli incarichi prima di aprire la cognizione soggettiva profonda.

Questa fase deve lavorare su:

- fallimenti degli step;
- mapping da fallimento a strategia di recupero;
- fallback configurabili per tipo di incarico;
- retry locale controllato;
- chiusura pulita degli incarichi non recuperabili;
- ritorno minimo verso memoria/credenze;
- explainability del fallimento e del recupero;
- QA di scenari bloccati, target non validi e risorse non piu' disponibili.

La motivazione e' strutturale: non conviene costruire obsolescenza delle credenze, verifica locale e cognizione soggettiva avanzata sopra incarichi che sanno fallire ma non sanno ancora recuperare o chiudersi in modo governato.

Questa fase NON deve introdurre recovery intelligente completa, planner globale o nuova autorita' decisionale dentro `JobExecutionSystem`.

Il recupero deve restare locale, configurabile, osservabile e limitato. Se un incarico non puo' recuperare, deve fallire in modo chiaro e produrre un ritorno minimo leggibile dal livello cognitivo futuro.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.14a | Audit recovery Job post-NeedsDecisionRule | ⏳ |
| v0.14b | Mappa fallimenti step -> strategie recovery | ⏳ |
| v0.14c | Integrazione policy recovery nel Job runtime | ⏳ |
| v0.14d | Retry locale controllato e limiti anti-loop | ⏳ |
| v0.14e | Fallback per target non valido o risorsa sparita | ⏳ |
| v0.14f | Failure learning minimo verso memoria/credenze | ⏳ |
| v0.14g | Explainability recovery Job | ⏳ |
| v0.14h | QA e closeout Job Recovery Runtime | ⏳ |

---

#### v0.15 - Cognizione Soggettiva Avanzata

## Stato
FUTURA / PENDING

## Obiettivo

Approfondire la cognizione soggettiva degli NPC usando l'infrastruttura stabilizzata in v0.11D, il tratto MBQD/Incarichi chiuso in v0.13 e il recupero locale degli incarichi stabilizzato in v0.14.

Questa fase deve lavorare su:

- memoria soggettiva;
- belief lifecycle;
- freshness e obsolescenza;
- verifica locale delle credenze;
- reazione a eventi mondo;
- comunicazione non onnisciente;
- decisioni basate su conoscenza imperfetta.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.15a | Audit cognition gap post-v0.14 | ⏳ |
| v0.15b | Belief lifecycle e obsolescenza cibo/oggetti | ⏳ |
| v0.15c | Memory encoding da world events needs | ⏳ |
| v0.15d | Verifica locale credenze obsolete | ⏳ |
| v0.15e | Comunicazione soggettiva dei fatti osservati | ⏳ |
| v0.15f | Decisioni con belief incerte e parziali | ⏳ |
| v0.15g | QA anti-omniscienza cognitiva | ⏳ |
| v0.15h | Closeout cognition deepening | ⏳ |

---

#### v0.16 - Conseguenze Sociali Emergenti

## Stato
FUTURA / PENDING

## Obiettivo

Introdurre conseguenze sociali emergenti sopra world events, memoria soggettiva e comunicazione.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.16a | Audit reputazione/sospetto post-v0.15 | ⏳ |
| v0.16b | Catene sospetto/furto da eventi osservati | ⏳ |
| v0.16c | Giudizio sociale locale | ⏳ |
| v0.16d | Prime norme emergenti | ⏳ |
| v0.16e | Istituzioni runtime leggere | ⏳ |
| v0.16f | QA scenario sociale osservabile | ⏳ |

---

#### v0.17 - Observer Layer Pubblico ed Explainability Esterna

## Stato
FUTURA / PENDING

## Obiettivo

Costruire uno strato observer esterno leggibile sopra eventi, memoria, decisioni e conseguenze sociali.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.17a | Timeline eventi mondo | ⏳ |
| v0.17b | Reason graph NPC | ⏳ |
| v0.17c | Pannelli observer leggibili | ⏳ |
| v0.17d | Reinserimento job traces v0.07 | ⏳ |
| v0.17e | QA observer end-to-end | ⏳ |

> **Nota:** questa fase riassorbe la vecchia v0.07 e la porta a uno strato observer realmente utile.

---

## v1.00 — Prima Demo Giocabile

**Criteri minimi per v1.00:**
- mappa giocabile con colonia persistente;
- NPC con comportamento emergente leggibile;
- fame/sonno/possesso/furto/conseguenza sociale;
- job e ruoli osservabili;
- debug/explainability disponibile;
- save/load stabile;
- build distribuibile.

---

## Note architetturali permanenti

| Regola | Motivazione |
|--------|-------------|
| Nessun NPC legge il world state globale direttamente | Omniscience constraint fondamentale |
| Il World resta fonte primaria della verità runtime | Evitare leakage view-side o cache spurie |
| Il tick discreto resta il tempo simulativo canonico | Determinismo e auditabilità |
| I facts di mondo devono passare da IWorldEvent | Causalità, memoria, reputazione, explainability |
| Nessuna feature deve gonfiare SimulationHost in God Manager | Separazione dei layer |
| I task Codex su Core restano audit-first | Repository fragile e sistemica |

---

# Stato Runtime Reale Attuale

Il sistema oggi possiede già:

- Job runtime vivo;
- esecuzione multi-tick;
- RunningAction;
- state machine;
- traversal temporale;
- explainability runtime;
- foundation recovery passiva;
- classificazione failure passiva.

---

# Funzionalità NON ancora implementate

NON esistono ancora:

- recovery locale reale;
- retry runtime;
- replanning locale;
- escalation cognitiva produttiva;
- evaluator recovery produttivo;
- planner globale;
- recovery automatico intelligente.

Questi aspetti verranno introdotti solo dopo la chiusura delle foundation passive.

---

*ARCONTIO Development Roadmap — documento vivo full fidelity — aggiornato Maggio 2026*


