# ARCONTIO — Development Roadmap

> **Ritmo di lavoro:** 3 sessioni/settimana (Lunedì, Mercoledì, Giovedì) · 2 ore per sessione · 6 ore/settimana
> **Target v1.00:** Prima demo giocabile pubblica
> **Stato documento:** Giugno 2026

---

## Mappa versioni

| Versione | Focus principale | Periodo stimato | Stato |
|----------|-----------------|-----------------|-------|
| v0.01 | Sistemi base: perception, memory, token | Completata | ✅ Completata |
| v0.02 | Sistemi base 2 | Completata | ✅ Completata |
| v0.03 | Pathfinding landmark + ComplexEdge + failure ladder | Aprile 2026 | ✅ Completata |
| v0.04 | NpcDnaProfile · NpcProfile · Needs System · BeliefStore | Aprile 2026 | ✅ Completata con residui parziali documentati |
| v0.05 | Decision Layer baseline / NeedsDecision bridge | Aprile 2026 | ⚠️ Parziale legacy |
| v0.05.5 | EL-MBQD Runtime UI Registry + pannello laterale | Aprile 2026 | ✅ Completata |
| v0.06 | Job System + Step System | Aprile 2026 | ✅ Completata ma da reintegrare |
| v0.07 | Explainability Layer v0.06: Job / Phase / Step / Command | Posticipata | 🧊 Posticipata |
| v0.08 | Repository Stabilization + Constitutional Documentation Bootstrap | Aprile 2026 | ✅ Completata |
| v0.09 | Simulation Backbone Hardening & Constitutional Alignment | Maggio 2026 | ✅ Completata |
| v0.10 | World Persistence Closure & Save/Load Completion | Maggio 2026 | ✅ Completata |
| v0.11A | Job Backbone Reintegration | Maggio 2026 | ✅ Completata |
| v0.11B | Decision Architecture (MBQD) Foundation | Maggio 2026 | ✅ Completata |
| v0.11C | Decision Orchestrator & Temporal Runtime Foundation | Maggio 2026 | ✅ Completata fino a v0.11c.06 |
| v0.11D | Runtime Infrastructure & Dormant Systems Forensic Reintegration | Maggio-Giugno 2026 | ✅ Completata |
| v0.12 | Pulizia Logging, Explainability e Diagnostica Runtime | Giugno 2026 | ✅ Completata |
| v0.13 | Chiusura MBQD/Incarichi e pensionamento NeedsDecisionRule | Giugno 2026 | ✅ Completata |
| v0.14 | Job Recovery Runtime e fallback degli incarichi | Giugno 2026 | ✅ Completata |
| v0.15 | Chiusura Movimento Multi-Tick e pensionamento MoveIntent runtime | Giugno-Luglio 2026 | ✅ Completata |
| v0.16 | Cognizione Soggettiva Avanzata | Luglio 2026 | ✅ Completata |
| v0.17 | Osservatorio costi runtime e profilazione per NPC | Luglio 2026 | ⏳ Pending |
| v0.18 | Ottimizzazione forte runtime percezione / belief / query | Luglio 2026 | 🔄 In corso |
| v0.20 | Rifondazione percettiva strutturale e scheduling percettivo | Luglio 2026 | ✅ Completata fino a v0.20q |
| v0.21 | Stabilizzazione post-rifondazione percettiva | Luglio 2026 | 🔄 In corso |
| v0.30 | ArcGraph Foundation e sostituzione progressiva rendering provvisorio | Agosto 2026 | ✅ Completata come foundation |
| v0.31 | ArcGraph Bootstrap controllato | Agosto 2026 | ✅ Completata |
| v0.32 | ArcGraph Terrain Renderer | Agosto 2026 | ✅ Completata |
| v0.33 | ArcGraph Modalita' comparativa controllata | Agosto 2026 | ✅ Completata nel perimetro sicuro |
| v0.34 | ArcGraph Actor/Object Renderer | Agosto 2026 | ✅ Completata nel perimetro passivo |
| v0.35 | ArcGraph Actor Motion Runtime Bridge | Agosto 2026 | ✅ Completata nel perimetro bridge read-only |
| v0.36 | ArcGraph Environment Visual Layers | Agosto-Settembre 2026 | ✅ Completata come contratti visuali preparatori |
| v0.37 | ArcGraph Debug/Overlay Migration | Settembre 2026 | ✅ Completata come migrazione debug minima |
| v0.38 | ArcGraph Legacy Absorption / Retirement | Settembre 2026 | 🔄 In corso |
| v0.39 | Environment Foundation Data-Only | Post-v0.38 | ✅ Completata foundation/data-only |
| v0.40 | Calendar, Time, Season | Post-v0.39 | ✅ Completata foundation/data-only |
| v0.41 | Global Climate & Weather | Post-v0.40 | ✅ Completata foundation/data-only |
| v0.42 | Environment Area Registry | Post-v0.41 | ✅ Completata foundation/data-only |
| v0.43 | Fertility Areas | Post-v0.42 | ✅ Completata foundation/data-only |
| v0.44 | Water Areas / Water Map | Post-v0.43 | ✅ Completata foundation/data-only |
| v0.45 | Vegetation Areas & SeedBank | Post-v0.44 | ✅ Completata foundation/data-only |
| v0.46 | Plant Catalog | Post-v0.45 | ✅ Completata foundation/data-only |
| v0.47 | PlantInstance Lifecycle | Post-v0.46 | ✅ Completata foundation/data-only |
| v0.48 | Natural Growth Loop | Post-v0.47 | ✅ Completata foundation/data-only |
| v0.49 | Agriculture Foundation | Post-v0.48 | ✅ Completata foundation/data-only |
| v0.50 | Environment Read-Only Snapshots | Post-v0.49 | ✅ Completata foundation/data-only |
| v0.51 | Save/Load Environment | Post-v0.50 | ✅ Completata foundation/data-only |
| v0.52 | ArcGraph Environment Adapter | Post-v0.51 | ✅ Completata adapter passivo |
| v0.53 | Biosphere Runtime Config & Scheduling | Post-v0.52 | ✅ Completata v0.53.1 config/scheduler data-only |
| v0.54 | Core Cell Surface Layer | Integrata in base fec5435 | ✅ Completata da v0.38m.01-v0.38m.14 |
| v0.55 | Biological Area Landmark Definition | Post-v0.54 | ✅ Core iniziale completato, harness pending |
| v0.56 | Biosphere Surface Candidate Query | Post-v0.55 | ✅ Core iniziale completato, harness pending |
| v0.57 | Biosphere / World Physical Mutation Boundary | Post-v0.56 | ✅ Core boundary iniziale completato, delta/save-load pending |
| v0.58 | Physical Plant Placement | Post-v0.57 | ✅ Core iniziale completato, tuning placement pending |
| v0.59 | Plant Physical Runtime Representation | Post-v0.58 | ✅ Core runtime representation iniziale completata |
| v0.60 | Plant Lifecycle Delta Propagation | Post-v0.59 | ✅ Producer delta giornalieri data-only completato |
| v0.61 | Decorative Vegetation Placement | Post-v0.60 | ✅ Contratto/delta vegetazione diffusa data-only completato, World projection pending |
| v0.62 | Dirty Propagation / FOV & Perception | Post-v0.61 | ⏳ Pending |
| v0.63 | ArcGraph Environment Runtime Feed | Post-v0.62 | ⏳ Pending |
| v0.64 | Environment Event / Listener Boundary | Post-v0.63 | ⏳ Pending |
| v0.65 | Biosphere Runtime Save/Load Integration | Post-v0.64 | ⏳ Pending |
| v0.66 | NPC Environment Query Integration | Post-v0.65 | ⏳ Pending |
| v0.67 | Biosphere Runtime Debug & Calibration Panel | Post-v0.66 | ⏳ Pending |
| v0.68 | Biosphere Runtime Budget & Batch Processing | Post-v0.67 | ⏳ Pending |
| v0.69 | Biosphere Physical Integration QA Closeout | Post-v0.68 | ⏳ Pending |
| v0.70 | ArcGraph Runtime UI Architecture | Post-v0.69 / track UI dedicata | 🔄 Pianificata |
| v0.170 | Conseguenze Sociali Emergenti | Dopo biosfera foundation minima | ⏳ Pending |
| v0.180 | Observer Layer Pubblico ed Explainability Esterna | Dopo observer prerequisites | ⏳ Pending |
| v1.00 | Prima demo giocabile pubblica | TBD | 🎯 Target |

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
COMPLETATA

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
COMPLETATA

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
| v0.14a | Audit recovery Job post-NeedsDecisionRule | ✅ DONE |
| v0.14b | Mappa fallimenti step -> strategie recovery | ✅ DONE |
| v0.14c | Integrazione policy recovery nel Job runtime | ✅ DONE |
| v0.14d | Retry locale controllato e limiti anti-loop | ✅ DONE |
| v0.14e | Fallback per target non valido o risorsa sparita | ✅ DONE |
| v0.14f | Failure learning minimo verso memoria/credenze | ✅ DONE |
| v0.14g | Explainability recovery Job | ✅ DONE |
| v0.14h | QA e closeout Job Recovery Runtime | ✅ DONE |

> **Nota closeout v0.14h (2026-05-31):** la fase `v0.14` e' chiusa come Job Recovery Runtime e fallback degli incarichi. Ha trasformato la foundation recovery passiva in un recupero locale governato: classificazione dei fallimenti step, policy lette da `job_recovery_policies.json`, retry locale limitato, fallback su cibo equivalente visibile, ritorno minimo verso memoria/credenze e tracciamento EL del recupero. La fase NON introduce planner globale, ricerca attiva sulla mappa o recovery intelligente completa. Restano rinviati a `v0.15+` chiusura movimento multi-tick, lifecycle credenze, obsolescenza, verifica locale, propagazione sociale e strategie recovery piu' avanzate. Dettaglio: `v0.14h_Closeout_Report.md`.

---

#### v0.15 - Chiusura Movimento Multi-Tick e pensionamento MoveIntent runtime

## Stato
COMPLETATA

## Obiettivo

Chiudere il debito strutturale tra movimento legacy e Job runtime prima di aprire la cognizione soggettiva profonda.

Questa fase deve trasformare il movimento ordinario degli NPC da:

```text
MoveIntent -> MovementSystem -> spostamento immediato
```

a:

```text
Job -> RunningAction MoveTo -> progress multi-tick -> completion/failure -> recovery da matrice JSON
```

La running action di movimento deve poter gestire in autonomia le micro-operazioni fisiche locali necessarie allo spostamento, come attraversamento cella, validazione prossima cella e apertura porte localmente lecite.

Non deve pero' diventare un secondo sistema decisionale: se non conosce una route valida, se il target sparisce, se una porta non e' apribile o se il percorso non e' localmente risolvibile, deve fallire con causa esplicita e restituire il fallimento al Job. Il fallback successivo deve essere deciso dalle policy configurate in `job_recovery_policies.json`, non da euristiche nascoste dentro il movimento.

La scelta architetturale e' intenzionale: un NPC che non conosce la strada non deve inventare un percorso greedy onnisciente. Il fallimento del movimento e' informazione utile per il Job, per le credenze e per la futura cognizione soggettiva.

Questa fase deve lavorare su:

- audit authority reale di `MovementSystem`, `MoveIntent` e movimento Job;
- definizione della running action `MoveTo` come contenitore tecnico del movimento multi-tick;
- separazione tra calcolo percorso lecito e decisione cognitiva;
- rimozione del fallback greedy finale come comportamento ordinario;
- gestione porte come micro-interazione locale, non come decisione globale;
- ritorno dei fallimenti movimento al Job;
- collegamento dei fallimenti movimento alla matrice JSON di recovery;
- migrazione progressiva di path lunghi, landmark, local search e backoff;
- EL movimento/Job coerente con running action e fallimenti;
- feedback visivo diagnostico leggero quando un NPC produce una decisione;
- QA dei casi target sparito, risorsa eliminata, strada ignota, porta bloccata e NPC occupante.

---

## Fuori scope

In questa fase NON si deve:

- introdurre planner globale onnisciente;
- introdurre ricerca attiva sulla mappa come effetto collaterale del movimento;
- trasformare la running action `MoveTo` in un mini-Job decisionale;
- far scegliere alla running action nuovi obiettivi;
- far cercare alla running action cibo alternativo;
- introdurre cognizione soggettiva profonda;
- introdurre sistemi sociali;
- cambiare Save/Load salvo necessita' minima documentata;
- rimuovere strumenti dev/debug prima di averli isolati.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.15.1 | Debug riapertura job e cadenza decisionale | ✅ |
| v0.15.2 | Audit movimento legacy vs RunningAction MoveTo | ✅ |
| v0.15.3 | Bug target cibo eliminato durante movimento e belief obsoleta | ✅ |
| v0.15.4 | Specifica RunningAction MoveTo e cause fallimento | ✅ |
| v0.15.5 | Matrice recovery movimento in `job_recovery_policies.json` | ✅ |
| v0.15.6 | MoveTo multi-cella su route conosciuta | ✅ |
| v0.15.7 | Porte e micro-interazioni locali in MoveTo | ✅ |
| v0.15.8 | Rimozione fallback greedy ordinario | ✅ |
| v0.15.9 | Isolamento MoveIntent/MovementSystem come dev o compatibilita' | ⚠️ |
| v0.15.10 | EL movimento Job e QA anti-onniscienza path | ✅ |
| v0.15.11 | Flash diagnostico NPC su decisione presa | ✅ |
| v0.15.12 | QA target cibo, belief food lifecycle e percezione same-cell | ✅ |
| v0.15.13 | Closeout movimento multi-tick | ✅ |

> **Nota diagnostica v0.15.11:** il flash decisionale deve restare puramente visivo e non simulativo. Il Decision Layer non deve mutare sprite o viste: deve esporre un segnale diagnostico leggero, consumato dalla presentazione, per colorare temporaneamente lo sprite dell'NPC quando una decisione viene prodotta. Il flash non deve cambiare bisogni, job, movimento, tick order, memoria, belief o Save/Load.

> **Nota closeout v0.15.13 (2026-06-01):** la fase `v0.15` e' chiusa come stabilizzazione del movimento multi-tick nel Job runtime. `MoveToCell` puo' attraversare una route conosciuta cella per cella tramite running action, rispetta la durata configurata, mantiene reservation temporali e muta la posizione solo a completion. Le porte non bloccate vengono gestite come micro-interazione locale prima del traversal. Quando il runtime movimento Job e' attivo, un target distante senza route lecita non ricade piu' nel fallback greedy ordinario: fallisce e viene consegnato alla matrice recovery. `MoveIntent` e `MovementSystem` restano presenti come compatibilita' e strumenti dev/debug, non come percorso ordinario desiderato per i job. Restano da allineare in `v0.16a` i template `generic.move_to_cell.v1` e `transport.object_to_cell.v1` alla nuova semantica multi-tick.

---

#### v0.16 - Cognizione Soggettiva Avanzata

## Stato
COMPLETATA / DONE

## Obiettivo

Approfondire la cognizione soggettiva degli NPC usando l'infrastruttura stabilizzata in v0.11D, il tratto MBQD/Incarichi chiuso in v0.13, il recupero locale degli incarichi stabilizzato in v0.14 e il movimento multi-tick chiuso in v0.15.

Questa fase deve lavorare su:

- allineamento dei template `generic.move_to_cell.v1` e `transport.object_to_cell.v1` al nuovo movimento multi-tick;
- memoria soggettiva;
- belief lifecycle;
- freshness e obsolescenza;
- verifica locale delle credenze;
- reazione a eventi mondo;
- comunicazione non onnisciente;
- ricerca attiva del cibo quando non esistono target visibili;
- decisioni basate su conoscenza imperfetta.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.16a | Allineamento template `generic.move_to_cell.v1` e `transport.object_to_cell.v1` al movimento multi-tick | ✅ |
| v0.16b | Audit cognition gap post-v0.15 | ✅ |
| v0.16c | Belief lifecycle e obsolescenza cibo/oggetti | ✅ |
| v0.16d | Memory encoding da world events needs | ✅ |
| v0.16e | Verifica locale credenze obsolete | ✅ |
| v0.16f | Comunicazione soggettiva dei fatti osservati | ✅ |
| v0.16g | Decisioni con belief incerte e parziali | ✅ |
| v0.16h | QA anti-omniscienza cognitiva | ✅ |
| v0.16i | SearchFood avanzato con MoveTo esplorativo quando non esistono target visibili | ✅ |
| v0.16j | Closeout cognition deepening | ✅ |


> **Nota closeout v0.16a (2026-06-01):** il checkpoint `v0.16a` ha allineato `generic.move_to_cell.v1` e `transport.object_to_cell.v1` alla semantica `MoveTo` multi-tick. Il movimento ordinario dei Job passa da `MoveToRunningActionDriver`, attraversa route conosciute cella per cella, usa route dichiarate da belief food senza rifare query decisionali, e fallisce quando non esiste route lecita. Il movimento debug umano da mappa viene instradato come job tecnico `generic.move_to_cell.v1` con label dev esplicita e puo' usare route fisica/greedy solo perche' rappresenta un comando dell'operatore, non conoscenza NPC.

> **Nota audit v0.16b (2026-06-01):** il checkpoint `v0.16b` ha prodotto la fotografia dei gap cognitivi residui dopo `v0.15` e `v0.16a`. Il sistema possiede gia' `BeliefStore`, decay configurabile, stati `Active/Weak/Stale/Discarded`, query soggettiva e invalidazione locale del cibo visto come mancante. Restano da chiudere: route belief-only `EatKnownFood` senza object id fisico, memoria da eventi needs, policy esplicita per credenze stale, query multi-candidato futura e riduzione della recovery `FindEquivalentTarget` che oggi legge ancora World. Dettaglio: `v0.16b_Cognition_Gap_Audit_Report.md`.

> **Nota closeout v0.16j (2026-06-01):** la fase `v0.16` e' chiusa come Cognizione Soggettiva Avanzata iniziale. Ha stabilizzato lifecycle food belief, stato `Stale/Discarded`, invalidazione locale del cibo mancante, memoria da eventi needs, comunicazione soggettiva minima, uso di belief incerte ma non obsolete, QA anti-onniscienza e primo SearchFood esplorativo via MoveTo. Prima di aprire il sociale va completata la fase ponte `v0.17` sui costi runtime per NPC. Restano rinviati a `v0.170+`: query multi-candidato per alternative food, recovery `FindEquivalentTarget` senza nuove query interne, SearchFood esplorativo ricco a settori/copertura e riprogettazione completa del pannello EL pathfinding. Dettaglio: `v0.16j_Closeout_Report.md`.
---

#### v0.17 - Osservatorio costi runtime e profilazione per NPC

## Stato
FUTURA / PRIORITA' ALTA

## Obiettivo

Costruire uno strumento leggero per capire quale parte del runtime consuma risorse quando cresce il numero di NPC.

Questa fase viene prima della crescita sociale perche' ogni nuovo NPC sembra aumentare il costo frame in modo sensibile. Prima di aggiungere reputazione, sospetto, rumor o socialita' emergente bisogna misurare in modo chiaro:

- percezione;
- memoria;
- belief;
- query;
- decisione;
- EL;
- job;
- running action;
- pathfinding;
- fallback/recovery;
- pannelli diagnostici;
- comunicazione/token.

---

## Regola architetturale fondamentale

Lo strumento deve essere congelabile.

Quando e' disattivato deve avere costo praticamente nullo:

```text
profilazione spenta -> niente stringhe, niente liste, niente dizionari, niente JSONL, niente misure per NPC
```

Il percorso spento deve limitarsi a guardie economiche, per evitare di introdurre un nuovo sistema diagnostico che diventa esso stesso causa del calo frame.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.17a | Audit punti caldi runtime per NPC | ✅ |
| v0.17b | Configurazione osservatorio con costo nullo quando spento | ✅ |
| v0.17c | Misure per sistema: percezione, memoria, belief, query, decisione, EL, job, fallback | ✅ |
| v0.17d | Misure per NPC e individuazione NPC piu' costosi | ✅ |
| v0.17e | Contatori operativi: celle viste, oggetti controllati, query, path, trace, fallback | ✅ |
| v0.17f | JSONL opzionale batchato e limitato per profili runtime | ✅ |
| v0.17g | Scenario QA 1/2/4/8/16 NPC e report costo scalare | ⚠️ |
| v0.17h | Closeout osservatorio costi runtime | ⏳ |

> **Nota architetturale v0.17:** questa fase NON deve ottimizzare alla cieca. Deve prima rendere misurabile il costo reale per tick e per NPC. Solo dopo i dati si decidera' se intervenire su FOV/percezione, pathfinding, EL, pannelli, query, job o comunicazione.

---

#### v0.18 - Ottimizzazione forte runtime percezione / belief / query

## Stato
IN CORSO / PRIORITA' ALTA

## Obiettivo

Ridurre i tre punti di crescita runtime piu' evidenti emersi dal JSONL dell'osservatorio costi:

- percezione oggetti;
- query belief;
- decadimento belief.

La fase NON cambia semantica decisionale, memoria, percezione o belief. Ottimizza il modo in cui i dati vengono cercati, letti e aggiornati, preservando conoscenza soggettiva, tick order e autorita' dei layer.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.18a | Ottimizzazione ObjectPerceptionSystem con indice spaziale a griglia e budget massimo oggetti/celle per NPC | ✅ |
| v0.18b | Riduzione BeliefQuery con indice per categoria belief | ✅ |
| v0.18c | BeliefDecay discreto per categoria con parametri da JSON | ✅ |
| v0.18d | QA profiler comparativo pre/post ottimizzazione con 1/2/4/8/16 NPC | ⚠️ |
| v0.18e | Ottimizzazione NpcPerceptionSystem con indice spaziale a zone | ✅ |
| v0.18f | Closeout ottimizzazione runtime percezione/belief/query | ⏳ |

> **Nota architetturale v0.18:** questa fase nasce da misure reali, non da ottimizzazione preventiva. Ogni patch deve ridurre costo runtime senza introdurre onniscienza, senza cambiare score decisionali e senza rendere i sistemi diagnostici nuove sorgenti di costo.

---

#### v0.20 - Rifondazione percettiva strutturale e scheduling percettivo

## Stato
COMPLETATA / DONE

## Scopo

La fase `v0.20` sostituisce il piano precedente di sola percezione cadenzata con una rifondazione strutturale del rapporto tra movimento, percezione, mappe di invalidazione e aggiornamento memoria/belief.

L'obiettivo non e' aggiungere gameplay, ma ridurre drasticamente il costo runtime della percezione quando aumentano gli NPC, evitando cicli globali inutili e sostituendo la scansione periodica di tutti con un modello basato su dirty percettivo, indici persistenti e cadenza per stato percettivo.

Il principio guida e':

```text
percepire solo quando serve,
solo chi deve percepire,
solo nel tick compatibile con il suo stato,
ma senza perdere variazioni del mondo rilevanti.
```

## Checkpoint

| Checkpoint | Task | Stato |
|---|---|---|
| v0.20a | Audit architettura percezione/movimento e documento di rifondazione | ✅ |
| v0.20b | Indici persistenti compatti per oggetti e NPC | ✅ |
| v0.20c | Dirty percettivo conservativo per oggetti/NPC creati, mossi, distrutti o ruotati | ✅ |
| v0.20d | Separazione observed / watched nella mappa percettiva | ✅ |
| v0.20e | Scheduler percettivo per stato NPC con cadenza da `game_params` | ✅ |
| v0.20f | Limite massimo NPC percettivi per tick e distribuzione del carico | ✅ |
| v0.20g | ObjectPerceptionSystem e NpcPerceptionSystem su soli NPC dirty/cadenzati | ✅ |
| v0.20h | Landmark perception allineata a dirty/cadenza/range | ✅ |
| v0.20i | Rotazione movimento e `LookDirection` come sorgenti dirty percettive | ✅ |
| v0.20j | Cleanup strutture obsolete o ridondanti post-rifondazione | ✅ |
| v0.20k | QA profiler 20/50/100 NPC e debug overlay costo percettivo | ✅ |
| v0.20l | Closeout rifondazione percezione runtime | ✅ |
| v0.20m | Riallineamento percezione centrale, porte dirty e rimozione periodo landmark | ✅ |
| v0.20n | Pensionamento IdleScan automatico e osservazione direzionale via Job | ✅ |
| v0.20o | Pesi intent da JSON e stati percettivi di fase nei Job | ✅ |
| v0.20q | Catalogo pesi intent dedicato e rimozione idle scan legacy | ✅ |

> **Nota architetturale v0.20:** questa fase introduce una biforcazione controllata del ramo di sviluppo perche' tocca la struttura del ciclo runtime. La patch deve restare progressiva, ma non deve lasciare a meta' indici persistenti, dirty percettivo o cadenza: ogni checkpoint deve mantenere simulazione e diagnostica in uno stato leggibile.

> **Nota dati v0.20:** tutte le nuove strutture percettive devono privilegiare rappresentazioni piccole e veloci. Usare `byte`, `short`, `ushort`, `int`, liste compatte e buffer riutilizzabili quando bastano; evitare `long`, stringhe, LINQ, liste temporanee e dizionari pesanti nel percorso caldo salvo necessita' reale.

> **Nota ciclo runtime v0.20:** il ciclo obiettivo diventa: movimento, memoria landmark, percezione landmark, decadimento bisogni, percezione dirty/cadenzata, codifica memoria, manutenzione memoria oggetti, decadimento memoria, decadimento belief, esecuzione job, decisione. Le mutazioni di oggetti/NPC fuori ciclo non devono eseguire percezione immediata: devono solo sporcare le strutture percettive che verranno consumate nel punto centrale del ciclo.

> **Nota stati percettivi v0.20:** da `v0.20e` gli NPC possiedono uno stato percettivo configurabile da `game_params`. Ogni stato definisce cadenza, raggio visivo e cono; il dirty conservativo usa il massimo raggio teorico degli stati configurati piu' margine, mantenendo una invalidazione prudente senza eseguire percezione immediata fuori dal ciclo centrale.

> **Nota budget percettivo v0.20f:** il `World` ora seleziona gli NPC percettivi candidati per tick applicando dirty, cadenza dello stato, limite massimo configurato e round-robin deterministico. La selezione produce liste e contatori, ma i sistemi percettivi non la consumano ancora: l'integrazione operativa resta nel checkpoint `v0.20g`.

> **Nota sistemi percettivi v0.20g:** `ObjectPerceptionSystem` e `NpcPerceptionSystem` consumano ora la selezione percettiva condivisa del tick. Gli NPC non dirty, fuori cadenza o oltre budget non producono percezione in quel tick; quelli pending restano dirty e vengono riproposti dal round-robin.

> **Nota landmark perception v0.20h:** `LandmarkPerceptionSystem` consuma la stessa selezione percettiva condivisa e risolve range/cone dallo stato dell'NPC. Il learning landmark non procede piu' come scansione separata su tutti gli NPC quando il sistema e' attivo.

> **Nota rotazione percettiva v0.20i:** rotazioni e spostamenti reali dell'NPC producono ora dirty percettivo immediato, quindi non restano bloccati dalla cadenza lenta dello stato `Idle`. La running action `MoveTo` orienta l'NPC verso la cella attraversata prima del completamento fisico del passo multi-tick; il ponte `MovementSystem` legacy/debug mantiene lo stesso comportamento. Lo scan direzionale deve usare lo stato percettivo `LookDirection` quando viene prodotto da un incarico di osservazione.

> **Nota cleanup v0.20j:** l'audit post-rifondazione non ha individuato strutture percettive eliminabili senza rischio: indici persistenti, dirty, watched/observed e contatori costo risultano vivi. La patch rimuove solo letture ridondanti rimaste da prima degli stati percettivi e aggiorna i commenti interni ormai superati. `MovementSystem` resta ponte tollerato per debug/legacy fino alla chiusura del relativo debito futuro.

> **Nota QA costo percettivo v0.20k:** `game_params.json` espone ora la sezione `logging.runtime_cost_observer`, spenta di default per mantenere costo nullo. Quando e' attiva, il riquadro coordinate mostra un riepilogo leggero di selezione percettiva, pending, dirty e contatori cumulativi principali per oggetti/NPC/FOV, cosi' i test 20/50/100 NPC possono essere letti direttamente in runtime oltre che dai JSONL.

> **Nota closeout v0.20l:** la fase `v0.20` e' chiusa come rifondazione percettiva runtime. La percezione oggetti, NPC e landmark non procede piu' come scansione globale ordinaria: usa dirty percettivo, indici persistenti, cadenza per stato, budget massimo per tick e osservabilita' runtime attivabile. Restano debiti di ottimizzazione avanzata e QA numerico esteso, ma la struttura base e' pronta per misurazioni 20/50/100 NPC e per ulteriori riduzioni mirate.

> **Nota riallineamento v0.20m:** il closeout ha evidenziato alcuni debiti di coerenza percettiva centrale. `landmark_perception.period` viene rimosso, i landmark seguono solo dirty/cadenza/stato/budget, la pulizia dirty viene separata in `PerceptionDirtyCompletionSystem`, le porte aperte/chiuse marcano dirty gli osservatori nel watched cone economico e l'overlay mostra NPC percepiti su NPC totali. Restano rinviati watched cone visualizzato a bordo, rinforzo memoria cadenzato e ottimizzazione strutture dati piu' calde.

> **Nota osservazione direzionale v0.20n:** lo scan automatico idle viene pensionato come comportamento runtime autonomo. Il guardarsi attorno passa dal percorso ordinario Decisione -> JobRequest -> Job tramite l'intent `WaitAndObserve`, il template `perception.look_around.v1` e quattro step `LookDirection` direzionali. `SearchFood` conserva il movimento probe e aggiunge una fase finale di osservazione locale; la percezione resta centralizzata e legge il nuovo facing solo nel blocco percettivo ordinario.

> **Nota configurazione job/intent v0.20o:** i template degli incarichi espongono `PerceptionState` sulle fasi e `exitPerceptionState` sul job, cosi' movimento, osservazione direzionale e ritorno a idle vengono dichiarati dal file `job_templates.json` invece che dedotti solo dal codice. I pesi degli intent vengono consolidati nel checkpoint `v0.20q` tramite catalogo dedicato.

> **Nota catalogo intent e idle cleanup v0.20q:** i pesi degli intent non vivono piu' in `game_params.json`, ma nel file dedicato `decision_intent_score_config.json`, recuperando il layout separato della prima implementazione. Lo scan idle automatico residuo viene eliminato insieme allo stato legacy collegato: il guardarsi attorno deve passare solo da Decisione -> JobRequest -> Job tramite `WaitAndObserve` e step `LookDirection`.

---

#### v0.21 - Stabilizzazione post-rifondazione percettiva

## Stato
IN CORSO / QA POST-MERGE

## Scopo

La fase `v0.21` stabilizza il runtime emerso dopo la rifondazione percettiva `v0.20`.

Non introduce una nuova espansione sistemica. Serve a verificare che percezione dirty/cadenzata, osservazione direzionale via Job, SearchFood, memoria, watched FOV, etichette runtime e diagnostica costi lavorino in modo coerente dopo l'integrazione su `ai/codex-main`.

Il principio guida e':

```text
prima stabilizzare cio' che e' appena stato rifondato,
poi misurare il nuovo collo di bottiglia,
solo dopo ampliare cataloghi, job composti o sistemi sociali.
```

## Checkpoint

| Checkpoint | Task | Stato |
|---|---|---|
| v0.21a | Monitor stato percettivo e audio diagnostico iniziale | ✅ |
| v0.21b | Stabilita' SearchFood, percezione durante osservazione ed etichette runtime | ✅ |
| v0.21c | Rinforzo mnemonico cadenzato e debug watched FOV | ✅ |
| v0.21d | Correzione margine watched FOV sui quattro lati | ✅ |
| v0.21e | QA runtime post-merge e confronto costi aggiornato | ⏳ |
| v0.21f | Censimento e classificazione Intent / Job / Step / Running Action | ⏳ |
| v0.21g | Closeout stabilizzazione post-rifondazione | ⏳ |

> **Nota architetturale v0.21:** questa fase e' una stabilizzazione post-rifondazione, non una nuova architettura. Le patch gia' integrate su `ai/codex-main` hanno corretto stabilita' SearchFood, percezione durante osservazione, etichette runtime, rinforzo mnemonico cadenzato e visualizzazione watched FOV. Restano necessari QA runtime mirato, nuovo confronto JSONL dei costi e censimento dei cataloghi Intent / Job / Step / Running Action prima di aprire espansioni piu' ampie.

---

#### v0.30 - ArcGraph Foundation e sostituzione progressiva rendering provvisorio

## Stato
COMPLETATA COME FOUNDATION / CLOSEOUT v0.30j

## Scopo

La fase `v0.30` apre la fondazione di `arcgraph`, il futuro mainframe grafico modulare di ARCONTIO.

Questa fase non deve produrre subito un salto visivo radicale. La resa runtime deve restare vicina a quella attuale, ma il rendering deve iniziare a essere incapsulato dentro una struttura destinata a sostituire il sistema grafico provvisorio, fino alla futura eliminazione del legacy grafico.

Il principio guida e':

```text
World / Simulation State
-> adapter visuale read-only
-> arcgraph
-> layer grafici, chunk sporchi, animazioni e presentazione
```

`arcgraph` deve leggere lo stato simulativo e produrre rappresentazione. Non deve diventare un nuovo decisore, non deve mutare il `World`, non deve introdurre una seconda fonte di verita' grafico-simulativa.

## Fuori scope

La fase `v0.30` NON deve implementare:

- meteo visivo produttivo;
- luci dinamiche complete;
- acqua fluida;
- vegetazione simulativa;
- NPC modulari completi a testa/busto/gambe/vestiti;
- livelli Z giocabili;
- nuova biosfera produttiva;
- cambiamenti del Decision Layer;
- cambiamenti del Job Layer;
- riscritture larghe non necessarie.

## Checkpoint

| Checkpoint | Task | Stato |
|---|---|---|
| v0.30a | Audit rendering attuale: MapGrid, chunk terrain, WorldView, SpriteRenderer, overlay, asset e accoppiamenti | ✅ |
| v0.30b | Definizione contratti minimi `arcgraph`: coordinate x/y/z, layer id, render state, dirty state | ✅ |
| v0.30c | Adapter read-only verso World / MapGrid corrente e primo confine anti-omniscienza grafica | ✅ |
| v0.30d | Layer grafici minimi attivi: Terrain, Object, Actor, Debug | ✅ |
| v0.30e | Dirty cell / dirty chunk preparatorio, senza ottimizzazione aggressiva | ✅ |
| v0.30f | Compatibilita' z-level preparatoria: firme x/y/z con rendering operativo solo su z = 0 | ✅ |
| v0.30g | ActorVisual preparatorio: sprite singolo attuale, progress multitick e interpolazione visiva tra celle | ✅ |
| v0.30h | Placeholder layer futuri: Water, Vegetation, Light, Weather, Effect | ✅ |
| v0.30i | Piano di assorbimento e futura eliminazione legacy grafico, senza doppio renderer permanente | ✅ |
| v0.30j | QA regressiva visuale e closeout ArcGraph Foundation | ✅ |

## Definition of Done v0.30

| Criterio | Stato |
|----------|-------|
| Rendering attuale auditato e mappato | ✅ |
| Responsabilita' di `arcgraph` definite | ✅ Parziale: contratti minimi introdotti |
| Separazione simulazione/rendering formalizzata | ✅ Parziale: contratti read-only predisposti |
| Layer grafici minimi progettati | ✅ Parziale: stack e layer passivi introdotti |
| Dirty cell / dirty chunk introdotti almeno come contratto | ✅ |
| Coordinate x/y/z previste anche se il runtime usa solo z = 0 | ✅ |
| Movimento visuale multitick previsto come interpolazione grafica, non mutazione simulativa | ✅ Parziale: posa visuale interpolabile predisposta |
| Placeholder per acqua, vegetazione, luci, meteo ed effetti identificati | ✅ Parziale: snapshot e layer passivi predisposti |
| Strategia di eliminazione del rendering legacy dichiarata | ✅ Parziale: piano audit-first dichiarato |
| QA regressiva foundation eseguita | ✅ Compilazione isolata ArcGraph riuscita |
| Nessuna nuova simulazione introdotta | ✅ Adapter/layer read-only senza runtime binding produttivo |
| Nessuna violazione anti-omniscienza | ✅ Adapter di presentazione isolato, nessun comando simulativo |

> **Nota architetturale v0.30:** `arcgraph` deve diventare il sistema grafico unico di ARCONTIO. Nella prima fase puo' assorbire e incapsulare parti del rendering attuale, ma non deve stabilizzare un doppio sistema permanente. Il riuso atteso e' alto per asset, coordinate e alcune tecniche pratiche; e' invece limitato per la struttura complessiva, oggi ancora provvisoria.

> **Nota audit v0.30a:** il checkpoint ha confermato che il rendering operativo vive soprattutto in `MapGridBootstrap`, `MapGridChunkRenderer`, `MapGridData` e `MapGridWorldView`. La parte chunk terrain e gli asset sono i candidati principali al riuso; `MapGridWorldView` e' invece un monolite view/debug/input da assorbire progressivamente in layer `arcgraph`. Il movimento multi-tick espone progresso runtime, ma la view attuale riposiziona gli NPC a centro cella solo a posizione simulativa aggiornata: l'interpolazione visuale richiede un contratto read-only dedicato.

> **Nota tecnica v0.30b:** il checkpoint ha introdotto i contratti minimi di `arcgraph` in forma passiva: coordinate cella/chunk con asse `z`, identificatori layer, stato render, dirty state, interfaccia layer e snapshot visuale actor con movimento multi-tick. Nessun renderer esistente e nessun sistema simulativo sono stati modificati.

> **Nota tecnica v0.30c:** il checkpoint ha introdotto l'adapter read-only `ArcGraphWorldAdapter`, con snapshot terreno, oggetto e actor. L'adapter legge `MapGridData` e `World`, produce copie value-type/listabili e non modifica il renderer legacy, il World, il Decision Layer o il Job Layer.

> **Nota tecnica v0.30d:** il checkpoint ha introdotto lo stack layer `ArcGraphLayerStack` e i layer minimi `Terrain`, `Object`, `Actor` e `Debug`. I layer consumano snapshot, marcano dirty in modo conservativo e non leggono direttamente `World`, `MapGridData`, sprite o GameObject.

> **Nota tecnica v0.30e:** il checkpoint ha centralizzato la marcatura dirty in `ArcGraphRenderState`, aggiungendo helper per cella+chunk, batch semplice di celle e cleanup esplicito. I layer minimi non duplicano piu' la formula cella -> chunk e il dirty resta un fatto di presentazione, non un event bus simulativo.

> **Nota tecnica v0.30f:** il checkpoint ha introdotto `ArcGraphZLevelPolicy`, dichiarando in modo centrale che il runtime attuale opera solo su `z = 0`. Adapter e render state usano questa policy senza introdurre altitudini giocabili; celle, chunk e dirty continuano pero' a preservare sempre il valore `Z`.

> **Nota tecnica v0.30g:** il checkpoint ha introdotto la posa visuale actor derivata (`ArcGraphActorVisualPoseSnapshot`) e helper di interpolazione per movimento multi-tick. L'audit ha confermato che il Job Layer espone progresso tick delle running action, ma non ancora origine/destinazione in un contratto read-only adatto alla view; per questo l'adapter continua a produrre motion inattivo finche' quel dato non sara' esposto senza accoppiamento invasivo.

> **Nota tecnica v0.30h:** il checkpoint ha introdotto snapshot e layer placeholder per Water, Vegetation, Light, Weather ed Effect. I layer sono passivi, non vengono registrati nei default foundation e non simulano acqua, piante, luce, meteo o fuoco; servono solo come slot grafici futuri registrabili esplicitamente tramite `RegisterFuturePlaceholderLayers`.

> **Nota audit v0.30i:** il checkpoint ha fissato il piano di assorbimento del legacy grafico. `MapGridChunkRenderer`, `MapGridTileAtlas`, convenzioni asset e parte della camera sono riusabili come tecniche; `MapGridData` va assorbito come sorgente temporanea di snapshot terreno; `MapGridWorldView` resta il monolite critico da non cancellare subito, perche' contiene actor/object sync, overlay, input debug, summary UI, rebind del World e dev tools. La sostituzione dovra' avvenire per fasi: prima bootstrap ArcGraph, poi terrain, poi actor/object, poi overlay/debug, infine pensionamento di MapGrid/WorldView.

> **Nota closeout v0.30j:** la foundation `arcgraph` compila isolatamente contro l'assembly corrente e la diff rispetto a `ai/codex-main` contiene solo documentazione operativa e nuovi file sotto `Assets/Scripts/Views/ArcGraph/Runtime`. Non sono stati modificati Core, Decision Layer, Job Layer, MapGrid legacy o file `.meta`. `arcgraph` resta preparatorio: non e' ancora un renderer produttivo e non sostituisce ancora `MapGridWorldView`.

---

#### v0.31 - ArcGraph Bootstrap controllato

## Stato
COMPLETATA

## Obiettivo

Accendere `arcgraph` come sistema interno controllato, senza render produttivo e senza sostituire ancora `MapGrid`.

La fase `v0.31` deve trasformare `arcgraph` da libreria di contratti passivi a sistema inizializzabile in modo esplicito e verificabile.

Il risultato desiderato non e' ancora vedere qualcosa di nuovo a schermo. Il risultato desiderato e':

```text
ArcGraph puo' essere inizializzato
-> i suoi layer foundation esistono
-> l'adapter puo' essere collegato
-> il sistema puo' produrre snapshot/cache interne
-> nessun renderer produttivo viene attivato
-> MapGrid continua a essere il renderer visibile
```

## Componenti coinvolti

Il bootstrap dovra' valutare come istanziare e collegare:

- `ArcGraphRenderState`;
- `ArcGraphLayerStack`;
- layer foundation: Terrain, Object, Actor, Debug;
- `ArcGraphWorldAdapter`;
- eventuali buffer snapshot temporanei;
- policy esplicita di attivazione/disattivazione.

## Fuori scope

La fase `v0.31` NON deve implementare:

- terrain renderer produttivo;
- mesh chunk ArcGraph;
- sprite NPC/oggetti ArcGraph visibili;
- sostituzione di `MapGridBootstrap`;
- sostituzione di `MapGridWorldView`;
- modalita' comparativa visuale;
- animazioni;
- luci;
- meteo;
- acqua;
- vegetazione;
- overlay diagnostici migrati;
- cambiamenti a Core, Decision Layer o Job Layer.

## Vincoli

- nessun doppio renderer permanente;
- nessuna modifica al `World`;
- nessuna sostituzione immediata di `MapGridBootstrap`;
- nessun `MonoBehaviour` ArcGraph invasivo prima di avere un contratto chiaro;
- nessun accesso diretto non controllato a Core, Decision Layer o Job Layer.

## Problema tecnico centrale

Oggi `MapGridData` e' creato e tenuto privato dentro `MapGridBootstrap`.

Questo e' il nodo principale della fase:

```text
ArcGraphWorldAdapter puo' gia' leggere MapGridData
ma ArcGraphBootstrap non ha ancora un modo pulito per ottenerlo
senza entrare nel monolite MapGridBootstrap / MapGridWorldView.
```

Quindi `v0.31` deve prima decidere il confine tra:

- bootstrap legacy MapGrid;
- bootstrap ArcGraph;
- dati view-side temporanei;
- futuro renderer ArcGraph.

## Checkpoint v0.31

| Checkpoint | Task | Stato |
|---|---|---|
| v0.31a | Audit bootstrap legacy: `MapGridBootstrap`, ownership di `MapGridData`, camera, input provider, `MapGridWorldView` | Completato |
| v0.31b | Definizione contratto bootstrap ArcGraph: cosa inizializza, cosa non inizializza, cosa espone | Completato |
| v0.31c | Decisione forma bootstrap: servizio C# passivo, wrapper Unity minimo o harness debug separato | Completato |
| v0.31d | Strategia accesso dati: come fornire ad ArcGraph `MapGridData` e `World` senza accoppiamento invasivo | Completato |
| v0.31e | Policy attivazione: flag/config/debug gate per evitare doppio renderer permanente | Completato |
| v0.31f | Implementazione bootstrap minimo controllato, se approvata dopo audit | Completato |
| v0.31g | QA: compilazione, nessun rendering prodotto, nessuna mutazione simulativa, nessun coupling vietato | Completato |
| v0.31h | Closeout v0.31 e preparazione v0.32 Terrain Renderer | Completato |

## Esito audit v0.31a - Bootstrap legacy MapGrid

L'audit `v0.31a` conferma che il bootstrap grafico attuale e' ancora concentrato in `MapGridBootstrap`.

Flusso reale osservato:

```text
MapGridBootstrap.Awake()
-> risolve Camera / Material
-> carica MapGridConfig
-> carica MapGridLayout
-> crea MapGridData
-> applica layout a MapGridData
-> crea MapGridTileAtlas
-> crea TerrainChunks + MapGridChunkRenderer
-> configura MapGridCameraController
-> crea MapGridPointerInputActionsProvider
-> crea MapGridWorldView
```

Punti tecnici emersi:

- `MapGridData` viene creato dentro `MapGridBootstrap` e resta campo privato del bootstrap;
- `MapGridData` viene passato a `MapGridChunkRenderer` per costruire i chunk terreno;
- `MapGridData` viene passato a `MapGridCameraController` per limiti camera e coordinate mappa;
- `MapGridWorldView` non riceve `MapGridData`, ma solo `MapGridConfig`;
- `MapGridWorldView` recupera il `World` tramite `MapGridWorldProvider.TryGetWorld()`;
- `MapGridWorldProvider` usa `SimulationHost.Instance` come ponte statico view-side;
- `ArcGraphWorldAdapter` sa gia' leggere `MapGridData` e `World`, ma non esiste ancora un punto runtime pulito che glieli fornisca insieme.

Conclusione audit:

```text
ArcGraph non deve essere agganciato direttamente dentro MapGridWorldView.
ArcGraph non deve diventare un secondo lettore casuale di SimulationHost.
ArcGraph ha bisogno di un contratto di bootstrap esplicito.
```

Il problema non e' ancora il renderer. Il problema e':

```text
chi possiede il runtime context grafico
e quali dati read-only puo' esporre ad ArcGraph
senza rendere MapGridWorldView ancora piu' monolitico.
```

Il prossimo checkpoint `v0.31b` deve quindi definire il contratto minimo di bootstrap ArcGraph prima di decidere la forma implementativa.

## Esito v0.31b - Contratto minimo bootstrap ArcGraph

Il contratto minimo del bootstrap ArcGraph deve essere trattato come un confine di accensione interna, non come un renderer.

Formula sintetica:

```text
ArcGraphBootstrap
-> crea lo stato grafico interno
-> registra i layer foundation
-> collega l'adapter read-only
-> prepara cache/snapshot interni
-> espone diagnostica minima
-> non disegna nulla
```

### Responsabilita' consentite

Il bootstrap ArcGraph puo':

- creare un `ArcGraphRenderState`;
- creare un `ArcGraphLayerStack`;
- registrare solo i layer foundation: `Terrain`, `Object`, `Actor`, `Debug`;
- creare un `ArcGraphWorldAdapter`;
- inizializzare i layer con lo stato render condiviso;
- preparare liste/buffer snapshot riusabili;
- produrre un primo refresh interno degli snapshot se i dati sorgente sono disponibili;
- esporre diagnostica minimale di bootstrap.

Diagnostica minima consigliata:

```text
IsInitialized
IsDisposed
LayerCount
HasRenderState
HasLayerStack
HasAdapter
HasRuntimeContext
LastBootstrapStatus
LastBootstrapReason
DoesRenderAnything = false
```

### Responsabilita' vietate

Il bootstrap ArcGraph non deve:

- creare `GameObject` visuali;
- creare `SpriteRenderer`;
- creare `MeshRenderer`;
- creare mesh terrain;
- caricare asset tramite `Resources.Load`;
- leggere `SimulationHost.Instance` direttamente;
- modificare `World`;
- modificare `MapGridData`;
- sostituire `MapGridBootstrap`;
- sostituire `MapGridWorldView`;
- gestire camera, input o click debug;
- emettere comandi verso NPC, Job Layer o Decision Layer;
- diventare un secondo renderer permanente.

### Input logici del contratto

Il bootstrap non deve inventare le proprie sorgenti dati. Deve ricevere o poter interrogare un runtime context controllato.

Input minimi futuri:

```text
MapGridConfig
MapGridData
World read-only o provider controllato
```

Input utili ma non obbligatori nel primo bootstrap:

```text
Camera bounds / tile size
stato attivazione debug
flag include future placeholder layers
```

La decisione su come fornire questi input resta nel checkpoint `v0.31d`. In `v0.31b` il punto chiave e' solo il contratto: ArcGraph deve ricevere dati da un confine esplicito, non andare a cercarli autonomamente.

### Output logici del contratto

Il bootstrap puo' esporre solo stato diagnostico e riferimenti interni read-only.

Output minimi:

```text
stato inizializzazione
numero layer registrati
ultimo errore diagnostico
presenza adapter
presenza render state
presenza layer stack
```

Non deve esporre API operative per:

- muovere actor;
- modificare tile;
- forzare job;
- cambiare decisioni;
- creare oggetti nel mondo;
- inviare comandi runtime.

### Lifecycle minimo

Stati consigliati:

```text
Uninitialized
Initializing
Initialized
Failed
Disposed
```

Regole:

- `Initialize` deve essere idempotente o fallire in modo spiegabile se chiamato due volte;
- `Dispose` deve liberare solo cache grafiche interne;
- un fallimento bootstrap non deve bloccare il renderer MapGrid legacy;
- un fallimento bootstrap non deve mutare il `World`;
- un fallimento bootstrap deve produrre una ragione diagnostica leggibile.

### Layer foundation inclusi

Il bootstrap `v0.31` deve registrare soltanto:

```text
ArcGraphTerrainLayer
ArcGraphObjectLayer
ArcGraphActorLayer
ArcGraphDebugLayer
```

I placeholder futuri `Water`, `Vegetation`, `Light`, `Weather`, `Effect` restano fuori dal bootstrap default. Possono essere attivati solo da flag/debug gate esplicito in checkpoint futuri.

### Verifica minima del contratto

Un bootstrap ArcGraph e' considerato acceso correttamente se:

```text
RenderState != null
LayerStack != null
Adapter != null
LayerStack.Count == 4
IsInitialized == true
DoesRenderAnything == false
nessun GameObject visuale creato
nessun renderer legacy sostituito
nessuna mutazione World/MapGridData
```

### Conclusione v0.31b

Il contratto non richiede ancora di scegliere se il bootstrap sara' un servizio C# passivo, un `MonoBehaviour` minimo o un harness debug. Questa scelta appartiene a `v0.31c`.

La decisione importante gia' presa a livello progettuale e':

```text
ArcGraphBootstrap possiede solo il lifecycle ArcGraph.
Non possiede il mondo.
Non possiede la mappa.
Non possiede la camera.
Non possiede l'input.
Non disegna.
```

## Esito v0.31c - Forma concreta del bootstrap

La forma scelta per il primo bootstrap controllato ArcGraph e':

```text
nucleo C# passivo
+ eventuale wrapper Unity futuro
+ nessun aggancio automatico alla scena in v0.31
```

Nome concettuale del nucleo:

```text
ArcGraphBootstrapRuntime
```

Questa forma e' preferibile perche':

- non dipende da `MonoBehaviour`;
- non usa `Awake`, `Start`, `Update` o coroutine;
- non crea un secondo lifecycle grafico Unity accanto a `MapGridBootstrap`;
- e' testabile con compilazione isolata o test EditMode futuri;
- puo' essere inizializzata esplicitamente da un harness, da un wrapper o da un bootstrap scena solo quando la policy di attivazione sara' definita;
- mantiene `arcgraph` come sistema interno controllato, non come renderer automatico.

### Alternative valutate

| Forma | Esito | Motivo |
|---|---|---|
| Servizio C# passivo puro | Scelto come nucleo | Massimo controllo, nessun lifecycle Unity nascosto |
| `MonoBehaviour` minimo dedicato | Rinviato | Utile come wrapper futuro, ma troppo presto per legarlo alla scena |
| Estensione di `MapGridBootstrap` | Scartata per v0.31 | Darebbe accesso facile a `MapGridData`, ma aumenterebbe il coupling col legacy |
| Harness debug separato | Utile per test, non come forma primaria | Sicuro, ma rischia di non rappresentare il runtime reale |

### Forma consigliata per v0.31f

La patch minima futura dovrebbe introdurre:

```text
ArcGraphBootstrapRuntime
ArcGraphBootstrapOptions
ArcGraphBootstrapDiagnostics
ArcGraphBootstrapStatus
ArcGraphRuntimeContext
```

Il runtime deve poter essere inizializzato da codice, ma non deve ancora essere inserito automaticamente nella scena.

Esempio logico:

```text
var bootstrap = new ArcGraphBootstrapRuntime();
bootstrap.Initialize(context, options);

diagnostics = bootstrap.Diagnostics;
```

Questo esempio non implica ancora un uso produttivo in scena. Serve solo a rendere verificabile che ArcGraph possa accendersi.

### Wrapper Unity

Il wrapper Unity resta ammesso solo come strato successivo, con responsabilita' limitata:

```text
ArcGraphBootstrapBehaviour
-> riceve riferimenti/flag dalla scena
-> costruisce un ArcGraphRuntimeContext
-> chiama ArcGraphBootstrapRuntime.Initialize(...)
-> espone diagnostica
-> non disegna
```

Non deve essere introdotto se per implementarlo bisogna modificare subito `MapGridBootstrap`, scene, prefab o asset.

### Decisione v0.31c

Decisione operativa:

```text
v0.31f implementera' prima il nucleo C# passivo.
Il wrapper Unity resta futuro o opzionale, non requisito della chiusura v0.31.
```

Questa scelta evita due errori:

- trasformare ArcGraph in un secondo renderer prima del terrain renderer;
- aumentare `MapGridBootstrap` per comodita' di accesso ai dati.

## Esito v0.31d - Strategia accesso dati

La strategia scelta per fornire dati ad ArcGraph e':

```text
ArcGraphRuntimeContext esplicito
-> creato da un chiamante esterno
-> passato al bootstrap
-> usato solo dal bootstrap/adapters
-> non letto direttamente dai layer
```

Il bootstrap ArcGraph non deve cercare le sorgenti dati da solo.

In particolare non deve:

- chiamare `SimulationHost.Instance`;
- cercare `MapGridBootstrap` con `FindObjectOfType`;
- agganciarsi a `MapGridWorldView`;
- leggere dalla scena in modo implicito;
- trasformare `MapGridData` in stato autoritativo.

### Contenuto minimo del runtime context

Il context dati minimo deve poter contenere:

```text
MapGridConfig Config
MapGridData Map
World World
```

Questi riferimenti hanno ruoli diversi:

- `MapGridConfig` fornisce parametri grafici gia' esistenti, come tile size e chunk size;
- `MapGridData` fornisce terreno e blocked view-side temporanei;
- `World` fornisce oggetti e actor al solo `ArcGraphWorldAdapter`;
- i layer ArcGraph ricevono snapshot, non il `World`;
- il bootstrap orchestra la conversione, ma non muta le sorgenti.

### Read-only pratico

`MapGridData` oggi e' una classe mutabile. Non esiste ancora un'interfaccia read-only dedicata.

Per `v0.31` la scelta conservativa e':

```text
non introdurre subito una nuova interfaccia MapGridData read-only;
documentare il contratto di sola lettura;
centralizzare l'accesso nel runtime context;
vietare mutazioni dal bootstrap e dall'adapter.
```

Una futura interfaccia read-only potra' essere introdotta se il bootstrap iniziera' a essere usato in scena o se piu' moduli dovranno leggere la mappa.

### Gestione dati mancanti

Il bootstrap deve poter inizializzare anche senza dati runtime completi.

Casi ammessi:

```text
context null
context senza MapGridData
context senza World
context senza Config
```

In questi casi:

- `ArcGraphRenderState` e `ArcGraphLayerStack` possono comunque nascere;
- gli snapshot non vengono popolati;
- la diagnostica deve segnalare il motivo;
- il bootstrap non deve fallire in modo distruttivo;
- il renderer legacy MapGrid deve continuare a funzionare.

### Flusso dati previsto

Flusso consentito:

```text
chiamante esterno
-> crea ArcGraphRuntimeContext
-> passa il context ad ArcGraphBootstrapRuntime
-> ArcGraphBootstrapRuntime usa ArcGraphWorldAdapter
-> adapter copia dati in snapshot
-> layer ArcGraph ricevono snapshot
```

Flusso vietato:

```text
Layer ArcGraph
-> SimulationHost.Instance
-> World
```

oppure:

```text
ArcGraphBootstrapRuntime
-> FindObjectOfType<MapGridBootstrap>
-> campo privato MapGridData
```

### Relazione con MapGridBootstrap

`MapGridBootstrap` resta il proprietario pratico del buffer view-side legacy.

In `v0.31d` non si modifica ancora `MapGridBootstrap`.

Il problema di come ottenere concretamente `MapGridData` dalla scena resta rinviato a una patch futura e controllata. In `v0.31f` il bootstrap minimo potra' essere verificato con context esplicito costruito da codice/test, senza aggancio automatico alla scena.

### Decisione v0.31d

Decisione operativa:

```text
ArcGraph non legge globali.
ArcGraph non entra in MapGridWorldView.
ArcGraph riceve dati tramite ArcGraphRuntimeContext.
I layer leggono snapshot, non sorgenti runtime.
```

## Esito v0.31e - Policy di attivazione

La policy scelta per `v0.31` e':

```text
nessuna attivazione automatica in scena
nessun renderer produttivo
nessun doppio renderer permanente
bootstrap esplicito solo da codice
modalita' interna: InternalStateOnly
```

### Modalita' operative

Le modalita' concettuali del bootstrap sono:

```text
Disabled
InternalStateOnly
```

`Disabled`:

- non inizializza il bootstrap;
- restituisce diagnostica spiegabile;
- non crea stato, layer o snapshot.

`InternalStateOnly`:

- crea `ArcGraphRenderState`;
- crea `ArcGraphLayerStack`;
- registra i layer foundation;
- crea `ArcGraphWorldAdapter`;
- puo' copiare snapshot interni se il context e' disponibile;
- non crea renderer, GameObject, mesh o sprite.

Modalita' non ammesse in `v0.31`:

```text
RenderTerrain
RenderActors
RenderObjects
CompareWithMapGrid
ReplaceMapGrid
```

Queste appartengono alle versioni successive `v0.32`-`v0.38`.

### Default

Default raccomandato:

```text
ActivationMode = InternalStateOnly
IncludeFuturePlaceholderLayers = false
AllowPartialRuntimeContext = true
PopulateInitialSnapshots = true
DoesRenderAnything = false
```

Il default `InternalStateOnly` e' accettabile per un servizio C# passivo perche' il servizio non si accende da solo. Serve comunque una chiamata esplicita a `Initialize`.

### Flag futuri

Flag possibili:

```text
IncludeFuturePlaceholderLayers
PopulateInitialSnapshots
AllowPartialRuntimeContext
```

`IncludeFuturePlaceholderLayers` deve restare `false` nel default. I layer futuri `Water`, `Vegetation`, `Light`, `Weather`, `Effect` non devono apparire come sistemi produttivi prima delle versioni dedicate.

`PopulateInitialSnapshots` puo' essere `true`, ma deve limitarsi a copiare dati in liste interne. Non deve produrre rendering.

`AllowPartialRuntimeContext` deve essere `true` per evitare che l'assenza temporanea di `World` o `MapGridData` blocchi la scena o renda fragile il bootstrap.

### Garanzie anti-doppio-renderer

In `v0.31` la garanzia principale e':

```text
nessun codice ArcGraph viene agganciato automaticamente alla scena
e nessuna classe bootstrap crea oggetti visuali Unity.
```

Ulteriori garanzie:

- nessun `MonoBehaviour` produttivo;
- nessun `new GameObject`;
- nessun `SpriteRenderer`;
- nessun `MeshRenderer`;
- nessun `Resources.Load`;
- nessun inserimento in `MapGridBootstrap.Awake`;
- nessuna modifica a `Scene_MapGrid`.

### Diagnostica attesa

La diagnostica deve poter dire:

```text
ArcGraph e' disabilitato
ArcGraph e' inizializzato internamente
ArcGraph ha inizializzato N layer
ArcGraph ha / non ha context runtime
ArcGraph ha / non ha popolato snapshot
ArcGraph non renderizza nulla
```

### Decisione v0.31e

Decisione operativa:

```text
v0.31f puo' implementare il bootstrap minimo.
Il bootstrap si attiva solo con chiamata esplicita.
La modalita' consentita e' InternalStateOnly.
Il rendering resta sempre spento.
```

## Esito v0.31f - Implementazione bootstrap minimo controllato

Implementato il nucleo C# passivo del bootstrap ArcGraph.

Nuovi file:

```text
ArcGraphBootstrapActivationMode.cs
ArcGraphBootstrapStatus.cs
ArcGraphBootstrapOptions.cs
ArcGraphRuntimeContext.cs
ArcGraphBootstrapDiagnostics.cs
ArcGraphBootstrapRuntime.cs
```

### Cosa fa

`ArcGraphBootstrapRuntime` puo':

- inizializzare `ArcGraphRenderState`;
- inizializzare `ArcGraphLayerStack`;
- registrare i layer foundation;
- creare `ArcGraphWorldAdapter`;
- ricevere `ArcGraphRuntimeContext`;
- copiare snapshot terreno, oggetti e actor in liste interne;
- alimentare le cache dei layer tramite `ReplaceSnapshots`;
- esporre diagnostica leggibile.

### Cosa non fa

Il bootstrap implementato non:

- eredita da `MonoBehaviour`;
- crea `GameObject`;
- crea `SpriteRenderer`;
- crea `MeshRenderer`;
- crea mesh;
- carica asset;
- legge `SimulationHost.Instance`;
- cerca oggetti scena;
- modifica `World`;
- modifica `MapGridData`;
- modifica `MapGridBootstrap`;
- modifica `MapGridWorldView`;
- sostituisce il renderer visibile.

### Runtime context

`ArcGraphRuntimeContext` contiene riferimenti opzionali a:

```text
MapGridConfig
MapGridData
World
```

Il context puo' essere parziale o vuoto. In quel caso il bootstrap puo' comunque accendere stato e layer, ma non popola gli snapshot mancanti.

### Opzioni

`ArcGraphBootstrapOptions` espone:

```text
ActivationMode
IncludeFuturePlaceholderLayers
PopulateInitialSnapshots
AllowPartialRuntimeContext
VisibleZLevel
DefaultTileSizeWorld
DefaultChunkSizeCells
```

Default operativo:

```text
ActivationMode = InternalStateOnly
IncludeFuturePlaceholderLayers = false
PopulateInitialSnapshots = true
AllowPartialRuntimeContext = true
```

### Diagnostica

`ArcGraphBootstrapDiagnostics` espone:

```text
Status
Reason
HasRenderState
HasLayerStack
HasAdapter
HasRuntimeContext
HasConfig
HasMap
HasWorld
LayerCount
TerrainSnapshotCount
ObjectSnapshotCount
ActorSnapshotCount
DoesRenderAnything = false
```

### QA preliminare v0.31f

Compilazione isolata riuscita sull'intera cartella:

```text
Assets/Scripts/Views/ArcGraph/Runtime
```

Controllo testuale delle chiamate vietate:

```text
nessun new GameObject(...)
nessun Resources.Load(...)
nessun AddComponent/GetComponent operativo
nessun FindObjectOfType operativo
nessuna ereditarieta' MonoBehaviour
nessuna chiamata SetNpcPos(...)
```

Resta da eseguire il checkpoint QA dedicato `v0.31g`.

## Esito v0.31g - QA bootstrap controllato

QA eseguita sul bootstrap minimo ArcGraph.

### Compilazione

Compilazione isolata riuscita sull'intera cartella:

```text
Assets/Scripts/Views/ArcGraph/Runtime
```

Metodo:

```text
Roslyn via dotnet
reference netstandard2.1
reference Library/ScriptAssemblies/Assembly-CSharp.dll
output temporaneo fuori repository
```

Nota: il `csc.exe` bundled Unity locale non e' stato usabile per una dipendenza mancante (`System.Text.Encoding.CodePages`), quindi la verifica e' stata eseguita con Roslyn/dotnet.

### Diff scope

Il diff rispetto a `ai/codex-main` resta limitato a:

```text
documenti root operativi
Assets/Scripts/Views/ArcGraph/Runtime
```

Nessuna modifica rilevata in:

```text
Assets/Scripts/Core
Assets/Scripts/Views/MapGrid
Assets/Scenes
*.meta
Library
Temp
Obj
```

### Chiamate vietate

Controllo testuale operativo:

```text
new GameObject(...)
Resources.Load(...)
: MonoBehaviour
AddComponent<...>
GetComponent<...>
FindObjectOfType<...>
SimulationHost.Instance
SetNpcPos(...)
CommandBuffer / ICommand / Decision / Job nei file bootstrap
```

Esito:

```text
nessuna chiamata operativa vietata nei file bootstrap
nessuna dipendenza operativa da Core/Decision/Job
unico riferimento a SimulationHost.Instance e' in commento di divieto nel runtime context
```

### Esito QA

```text
QA v0.31g superata.
ArcGraph Bootstrap e' compilabile come nucleo passivo.
Non renderizza.
Non muta il World.
Non muta MapGridData.
Non sostituisce MapGrid.
```

## Ipotesi iniziale consigliata

L'ipotesi piu' prudente e':

```text
ArcGraphBootstrap = componente o servizio minimo
che inizializza solo lo stato ArcGraph
e non disegna nulla.
```

Prima versione desiderabile:

- crea `ArcGraphRenderState`;
- crea `ArcGraphLayerStack`;
- registra i layer foundation;
- crea `ArcGraphWorldAdapter`;
- opzionalmente prepara liste snapshot riusabili;
- espone uno stato diagnostico tipo `IsInitialized`;
- non crea GameObject visuali;
- non crea mesh;
- non crea SpriteRenderer;
- non modifica `World`;
- non chiama metodi di movimento, job o decisione.

## Alternative progettuali da valutare

| Alternativa | Vantaggio | Rischio |
|---|---|---|
| Servizio C# passivo puro | Molto testabile, nessuna dipendenza Unity | Va comunque agganciato da qualche punto runtime |
| `MonoBehaviour` minimo dedicato | Facile da vedere in scena e controllare | Rischio di iniziare a comportarsi come renderer troppo presto |
| Estensione di `MapGridBootstrap` | Accesso facile a `MapGridData` | Aumenta il coupling col legacy |
| Harness debug separato | Sicuro per test e audit | Potrebbe restare scollegato dal runtime reale |

## Definition of Done v0.31

| Criterio | Stato |
|---|---|
| Punto di bootstrap ArcGraph deciso | Completato |
| Lifecycle `ArcGraphRenderState` definito | Completato |
| Lifecycle `ArcGraphLayerStack` definito | Completato |
| Registrazione layer foundation definita | Completato |
| Strategia accesso `MapGridData` chiarita | Completato |
| Strategia accesso `World` chiarita senza nuova onniscienza | Completato |
| Nessun renderer produttivo attivato | Completato |
| Nessun doppio renderer permanente introdotto | Completato |
| Nessuna modifica a Core/Decision/Job | Completato |
| QA minima documentata | Completato |

## Nota architetturale v0.31

`v0.31` e' completata come fase di accensione controllata, non di resa visiva.

`v0.32` potra' concentrarsi sul terrain renderer senza dover decidere anche bootstrap, ownership dei dati, lifecycle dei layer e policy anti-doppio-renderer.

## Closeout v0.31 - ArcGraph Bootstrap controllato

`v0.31` chiude il passaggio da ArcGraph come sola foundation passiva a ArcGraph come sistema inizializzabile internamente.

Output consolidato:

```text
ArcGraph puo' essere inizializzato
-> render state creato
-> layer stack creato
-> layer foundation registrati
-> adapter collegato
-> snapshot interni copiabili
-> diagnostica disponibile
-> nessun rendering produttivo
-> MapGrid resta renderer visibile
```

### Nuovo nucleo tecnico disponibile

Il nucleo bootstrap e' composto da:

```text
ArcGraphBootstrapRuntime
ArcGraphBootstrapActivationMode
ArcGraphBootstrapStatus
ArcGraphBootstrapOptions
ArcGraphRuntimeContext
ArcGraphBootstrapDiagnostics
```

### Stato architetturale finale

Decisioni consolidate:

- bootstrap come nucleo C# passivo;
- nessun wrapper Unity in `v0.31`;
- nessun aggancio automatico alla scena;
- context dati esplicito;
- snapshot copiati dall'adapter;
- layer alimentati da snapshot, non da `World`;
- policy `InternalStateOnly`;
- placeholder futuri esclusi dal default;
- diagnostica come output primario del checkpoint.

### Cosa resta fuori

Restano fuori da `v0.31`:

- terrain renderer produttivo;
- mesh chunk ArcGraph;
- sprite actor/object ArcGraph visibili;
- modalita' comparativa;
- sostituzione MapGrid;
- wrapper Unity di scena;
- accesso concreto a `MapGridData` dal runtime scena;
- overlay/debug migration;
- environment visual layers.

### Preparazione v0.32

`v0.32` dovra' occuparsi del primo renderer produttivo: il terreno.

Input gia' disponibili per `v0.32`:

- `ArcGraphTerrainCellSnapshot`;
- `ArcGraphTerrainLayer`;
- `ArcGraphRenderState`;
- `ArcGraphDirtyState`;
- `ArcGraphBootstrapRuntime`;
- snapshot terrain popolabili da `MapGridData`;
- chunk dirty preparatorio.

Domanda tecnica principale per `v0.32`:

```text
come trasformare ArcGraphTerrainLayer + dirty chunks
in un renderer terrain chunked visibile
senza attivare un doppio renderer permanente.
```

---

#### v0.32 - ArcGraph Terrain Renderer

## Stato
COMPLETATA

## Obiettivo

Realizzare il primo renderer produttivo ArcGraph: il terreno.

Il renderer dovra':

- leggere `ArcGraphTerrainCellSnapshot`;
- usare dirty cell / dirty chunk;
- ricostruire chunk terrain in modo localizzato;
- riusare concettualmente la tecnica di `MapGridChunkRenderer`;
- mantenere resa visiva compatibile con il terreno MapGrid attuale.

## Checkpoint v0.32

| Checkpoint | Task | Stato |
|---|---|---|
| v0.32a | Audit terrain legacy: `MapGridChunkRenderer`, atlas, mesh, UV, varianti floor, muro/wall-top | Completato |
| v0.32b | Contratto terrain renderer ArcGraph: input, output, vietati, diagnostica | Completato |
| v0.32c | Strategia atlas/materiali: UV map ArcGraph senza asset load e senza dipendenza permanente MapGrid | Completato |
| v0.32d | Renderer chunk passivo: builder mesh data per chunk da snapshot terrain | Completato |
| v0.32e | Dirty chunk rebuild: aggiornare solo chunk sporchi o richiesti | Completato |
| v0.32f | Harness/test controllato: costruzione mesh da snapshot senza scena produttiva | Completato |
| v0.32g | QA: compilazione, scope diff, no doppio renderer, no mutazioni simulazione | Completato |
| v0.32h | Closeout v0.32 e preparazione v0.33 modalita' comparativa controllata | Completato |

## Esito v0.32a - Audit terrain legacy

Il renderer terrain legacy rilevante e':

```text
MapGridBootstrap
-> BuildTerrainChunks()
-> GameObject TerrainChunks
-> GameObject Chunk_x_y
-> MeshRenderer + MeshFilter
-> MapGridChunkRenderer.Build(...)
```

`MapGridChunkRenderer` costruisce una mesh per chunk:

- una cella = un quad;
- ogni quad ha 4 vertici;
- ogni quad ha 6 indici triangolo;
- le coordinate world sono calcolate come `x * tileWorld`, `y * tileWorld`;
- il chunk non fa culling avanzato;
- il chunk ricostruisce l'intera mesh quando viene chiamato `Build`;
- il renderer usa `MeshFilter.sharedMesh`;
- il materiale e la texture atlas sono assegnati dal bootstrap legacy.

### Atlas legacy

`MapGridTileAtlas`:

- riceve una `Texture2D`;
- riceve `tilePixels`;
- calcola `TilesPerRow` e `TilesPerCol`;
- registra `tileId -> uvX/uvY`;
- converte `uvY` da origine alto a origine basso Unity;
- produce quattro UV per quad.

Questa logica e' riusabile concettualmente, ma `v0.32` non deve rendere ArcGraph dipendente in modo permanente da `MapGridTileAtlas`.

### Policy visuale legacy

Il legacy usa una policy minimale "DF Steam-like":

```text
se cella bloccata:
    se la cella a nord e' floor -> wallTopTileId
    altrimenti -> wallTileId
se cella non bloccata:
    floorBaseTileId + variante deterministica hash(x,y)
```

Questa scelta significa che il `tileId` sorgente non viene sempre disegnato letteralmente. Per compatibilita' iniziale, ArcGraph deve poter replicare questa policy, almeno come default terrain visual.

### Implicazioni per ArcGraph

ArcGraph deve separare tre concetti:

```text
snapshot terrain
-> contiene TileId e IsBlocked

policy visuale terrain
-> decide quale tile visuale disegnare

builder mesh chunk
-> trasforma celle + UV in dati mesh
```

Per `v0.32` la soluzione piu' prudente e':

- creare una UV map ArcGraph autonoma;
- creare una policy visuale terrain ArcGraph;
- creare un builder mesh-data per chunk;
- non creare ancora un `MonoBehaviour`;
- non creare automaticamente `GameObject`;
- non modificare `MapGridBootstrap`;
- non modificare la scena.

### Nodo tecnico

Per costruire i chunk ArcGraph serve leggere `ArcGraphTerrainLayer`, non `MapGridData`.

Flusso desiderato:

```text
MapGridData
-> ArcGraphWorldAdapter
-> ArcGraphTerrainCellSnapshot
-> ArcGraphTerrainLayer
-> ArcGraphTerrainChunkMeshBuilder
-> mesh data terrain
```

## Esito v0.32b - Contratto terrain renderer ArcGraph

Il terrain renderer ArcGraph deve essere costruito come renderer controllato a chunk.

Formula:

```text
ArcGraphTerrainLayer
+ ArcGraphRenderState
+ ArcGraphTerrainVisualPolicy
+ ArcGraphTerrainTileUvMap
-> ArcGraphTerrainChunkMeshBuilder
-> ArcGraphTerrainChunkMeshData
```

### Input ammessi

Il terrain renderer puo' leggere:

- `ArcGraphTerrainLayer`;
- `ArcGraphTerrainCellSnapshot`;
- `ArcGraphRenderState`;
- `ArcGraphDirtyState`;
- una mappa UV ArcGraph ricevuta dal chiamante;
- una policy visuale terrain ricevuta dal chiamante;
- coordinate chunk esplicite.

Non deve leggere:

- `MapGridData` direttamente;
- `World`;
- `SimulationHost.Instance`;
- `MapGridWorldView`;
- `MapGridBootstrap` tramite ricerca scena.

### Output ammessi

Il renderer/builder puo' produrre:

- array vertici;
- array UV;
- array triangoli;
- conteggio celle disegnate;
- diagnostica chunk;
- eventuale helper per applicare quei dati a una `Mesh` fornita dal chiamante.

In `v0.32` l'output principale deve restare dati mesh controllati, non un aggancio automatico alla scena.

### Responsabilita' vietate

Il terrain renderer non deve:

- creare `GameObject` automaticamente;
- aggiungere `MeshRenderer` o `MeshFilter` automaticamente;
- caricare texture o materiali con `Resources.Load`;
- mutare `World`;
- mutare `MapGridData`;
- sostituire `MapGridChunkRenderer`;
- disattivare MapGrid;
- decidere pathfinding, fertilita', movimento o collisioni;
- introdurre actor/object rendering.

### Compatibilita' visiva iniziale

La policy default deve poter replicare il legacy:

```text
blocked -> wall / wallTop
floor -> floorBase + variante deterministica hash(x,y)
```

Il `TileId` dello snapshot resta comunque disponibile. In futuro si potra' decidere se disegnare letteralmente il `TileId` o usare policy per categoria. In `v0.32` la compatibilita' con MapGrid e' prioritaria.

### Diagnostica minima

La costruzione chunk deve esporre:

```text
ChunkCoord
CellCount
VertexCount
TriangleIndexCount
UsedFallbackUv
IsEmpty
Reason
```

### Regola anti-doppio-renderer

In `v0.32` la regola resta:

```text
si puo' costruire mesh data ArcGraph;
non si puo' agganciare automaticamente il renderer alla scena;
non si puo' sostituire MapGrid;
la comparazione visuale appartiene a v0.33.
```

## Esito v0.32c - Strategia atlas/materiali

Implementata una UV map terrain autonoma per ArcGraph.

Nuovi file:

```text
ArcGraphTerrainTileUvDefinition.cs
ArcGraphTerrainTileUvMap.cs
```

### Strategia

ArcGraph non usa direttamente `MapGridTileAtlas`.

La nuova UV map:

- riceve dimensioni atlas primitive;
- riceve `tilePixels`;
- registra `tileId -> uvX/uvY`;
- calcola `TilesPerRow` e `TilesPerColumn`;
- converte `uvY` da origine alto a origine basso Unity;
- produce quattro UV per quad;
- usa fallback UV zero se il tile non e' registrato;
- non carica texture;
- non crea materiali;
- non possiede asset.

### Materiali

In `v0.32c` il materiale resta fuori dal renderer ArcGraph.

Regola:

```text
texture/materiale vengono forniti da un chiamante futuro;
ArcGraphTerrainTileUvMap conosce solo dimensioni e coordinate UV.
```

Questo evita asset load, dipendenze scena e accoppiamento prematuro con MapGrid.

### Compatibilita'

La convenzione UV replica quella legacy:

```text
uvX cresce verso destra
uvY = 0 indica la riga in alto dell'atlas
Unity UV viene convertita a origine basso
```

### QA preliminare

Compilazione isolata ArcGraph riuscita con:

```text
Library/ScriptAssemblies/Assembly-CSharp.dll
UnityEngine.CoreModule.dll
netstandard2.1
```

Controllo chiamate vietate:

```text
nessun Resources.Load(...)
nessuna dipendenza codice da MapGridTileAtlas
nessun GameObject
nessun MonoBehaviour
```

## Esito v0.32d - Renderer chunk passivo

Implementato il builder passivo di mesh data terrain per chunk.

Nuovi file:

```text
ArcGraphTerrainVisualPolicy.cs
ArcGraphTerrainChunkMeshDiagnostics.cs
ArcGraphTerrainChunkMeshData.cs
ArcGraphTerrainChunkMeshBuilder.cs
```

### Funzionamento

`ArcGraphTerrainChunkMeshBuilder`:

- legge `ArcGraphTerrainLayer`;
- riceve `ArcGraphTerrainTileUvMap`;
- riceve `ArcGraphChunkCoord`;
- riceve chunk size e tile world size;
- riceve `ArcGraphTerrainVisualPolicy`;
- trasforma le celle presenti nel chunk in quad;
- produce array `Vector3[]`, `Vector2[]`, `int[]`;
- produce diagnostica del chunk.

### Compatibilita' legacy

La policy default replica:

```text
floorBaseTileId = 0
floorVariantCount = 4
wallTileId = 10
wallTopTileId = 11
```

La logica visuale:

```text
se IsBlocked:
    se cella nord esiste ed e' floor -> wallTop
    altrimenti -> wall
se non blocked:
    floorBase + variante deterministica hash(x,y)
```

### Garanzia anti-scena

Il builder:

- non crea `GameObject`;
- non crea `Mesh`;
- non crea `MeshRenderer`;
- non crea `MeshFilter`;
- non carica asset;
- non legge `MapGridData`;
- non legge `World`;
- non modifica sorgenti.

### QA preliminare

Compilazione isolata ArcGraph riuscita.

Controllo chiamate vietate:

```text
nessun new GameObject(...)
nessun Resources.Load(...)
nessun MonoBehaviour
nessun AddComponent/GetComponent
nessun FindObjectOfType
nessun SimulationHost.Instance
```

## Esito v0.32e - Dirty chunk rebuild

Collegato il builder terrain al dirty state ArcGraph.

Modifica:

```text
ArcGraphTerrainChunkMeshBuilder.BuildDirtyChunks(...)
```

### Funzionamento

`BuildDirtyChunks`:

- legge `ArcGraphRenderState.Dirty.DirtyChunks`;
- copia i chunk sporchi in una lista locale;
- ordina i chunk in modo deterministico per `Z`, poi `Y`, poi `X`;
- opzionalmente filtra i chunk fuori dal livello visibile;
- chiama `BuildChunk` per ogni chunk sporco;
- restituisce una lista di `ArcGraphTerrainChunkMeshData`.

### Scelta importante

Il metodo non chiama:

```text
renderState.ClearDirty()
```

Il cleanup del dirty resta responsabilita' del chiamante. Questo evita che il terrain builder consumi accidentalmente dirty state prima di altri layer, overlay o renderer futuri.

### QA preliminare

Compilazione isolata ArcGraph riuscita.

Controllo chiamate vietate:

```text
nessun ClearDirty automatico
nessun new GameObject(...)
nessun Resources.Load(...)
nessun MonoBehaviour
nessun accesso globale
```

## Esito v0.32f - Harness/test controllato

Implementato un harness statico per validare la costruzione mesh terrain da snapshot.

Nuovo file:

```text
ArcGraphTerrainChunkMeshHarness.cs
```

### Cosa fa

`ArcGraphTerrainChunkMeshHarness.RunTwoByTwoSmoke()`:

- crea un `ArcGraphRenderState` con chunk size 2;
- crea un `ArcGraphTerrainLayer`;
- inserisce quattro snapshot terrain 2x2;
- registra UV minime per floor, wall e wall-top;
- usa `ArcGraphTerrainChunkMeshBuilder.BuildDirtyChunks`;
- verifica contatori attesi:
  - 1 chunk;
  - 4 celle;
  - 16 vertici;
  - 24 indici triangolo;
  - nessuna UV fallback;
  - dirty ancora presente.

### Cosa non fa

L'harness:

- non crea scena;
- non crea `GameObject`;
- non crea `MeshRenderer`;
- non crea `MeshFilter`;
- non carica asset;
- non sostituisce MapGrid;
- non usa framework test Unity.

### Nota QA

La compilazione isolata dell'harness e' riuscita.

L'invocazione diretta via PowerShell reflection fuori dal dominio Unity non e' considerata QA valida, perche' gli assembly Unity compilati fuori dal runtime Unity non vengono caricati correttamente da `Add-Type`. Il test resta quindi un harness compilabile e pronto per un futuro wrapper EditMode/Unity batch, non un test eseguito in questa fase.

## Esito v0.32g - QA terrain renderer

QA tecnica eseguita sul terrain renderer ArcGraph.

### Compilazione

Compilazione isolata riuscita sull'intera cartella:

```text
Assets/Scripts/Views/ArcGraph/Runtime
```

Reference usate:

```text
Library/ScriptAssemblies/Assembly-CSharp.dll
UnityEngine.CoreModule.dll
netstandard2.1
```

### Diff scope v0.32

Diff specifico da `ai-task/v0.31h-arcgraph-bootstrap-closeout` a `HEAD`:

```text
ARCONTIO_Roadmap.md
TASKBOARD_CODEX.md
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphTerrainChunkMeshBuilder.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphTerrainChunkMeshData.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphTerrainChunkMeshDiagnostics.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphTerrainChunkMeshHarness.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphTerrainTileUvDefinition.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphTerrainTileUvMap.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphTerrainVisualPolicy.cs
```

Nessuna modifica in:

```text
Assets/Scripts/Core
Assets/Scripts/Views/MapGrid
Assets/Scenes
*.meta
Library
Temp
Obj
```

### Chiamate vietate

Controllo testuale operativo:

```text
new GameObject(...)
Resources.Load(...)
: MonoBehaviour
AddComponent<...>
GetComponent<...>
FindObjectOfType<...>
SimulationHost.Instance
SetNpcPos(...)
CommandBuffer / ICommand
```

Esito:

```text
nessuna chiamata operativa vietata
unico riferimento a SimulationHost.Instance resta commento di divieto nel runtime context
```

### Esito QA

```text
QA v0.32g superata.
Terrain renderer ArcGraph produce mesh data passivi.
Non crea scena.
Non crea renderer Unity.
Non carica asset.
Non muta World.
Non muta MapGridData.
Non sostituisce MapGrid.
```

## Esito v0.32h - Closeout terrain renderer

`v0.32` e' chiusa come checkpoint di preparazione tecnica del primo renderer terrain ArcGraph.

### Definition of Done v0.32

Completato:

- audit del terrain renderer legacy `MapGridChunkRenderer`;
- contratto ArcGraph terrain separato da scena, `GameObject`, `MeshRenderer` e `MeshFilter`;
- UV map terrain autonoma, senza asset load e senza dipendenza diretta da `MapGridTileAtlas`;
- policy visuale compatibile con la resa legacy floor/wall/wall-top;
- builder passivo di mesh data per chunk;
- rebuild localizzato dei soli chunk dirty;
- harness statico compilabile per smoke test 2x2;
- QA di compilazione isolata;
- QA su chiamate vietate;
- QA su scope diff.

### Impatto architetturale

ArcGraph ora possiede il primo nucleo tecnico capace di trasformare snapshot terrain in dati mesh.

La catena e':

```text
ArcGraphTerrainLayer
+ ArcGraphTerrainTileUvMap
+ ArcGraphTerrainVisualPolicy
+ ArcGraphRenderState.Dirty
-> ArcGraphTerrainChunkMeshBuilder
-> ArcGraphTerrainChunkMeshData
```

Questo non sostituisce ancora il renderer MapGrid.

Il risultato della `v0.32` e' volutamente intermedio:

- produce dati mesh;
- non li monta ancora in scena;
- non crea renderer Unity;
- non decide materiali;
- non attiva una visualizzazione alternativa stabile.

### Debiti residui dichiarati

Restano fuori da `v0.32`:

- modalita' comparativa visuale ArcGraph/MapGrid;
- bridge verso `Mesh` Unity reale;
- policy di attivazione debug in scena;
- gestione materiali/texture come asset runtime controllati;
- confronto pixel/scala/camera;
- sostituzione del renderer legacy.

Questi punti appartengono a `v0.33` e versioni successive.

### Preparazione v0.33

`v0.33` dovra' costruire una modalita' comparativa controllata.

Obiettivo:

```text
MapGrid legacy resta renderer produttivo.
ArcGraph terrain viene acceso solo in debug/test.
I due output possono essere confrontati senza rendere permanente il doppio renderer.
```

La `v0.33` dovra' decidere con attenzione:

- dove montare fisicamente la mesh ArcGraph;
- come evitare doppio rendering persistente;
- come leggere materiali e atlas senza asset load impliciti;
- come verificare scala, coordinate chunk, ordinamento Z e camera;
- come disattivare completamente ArcGraph terrain fuori dal test.

---

#### v0.33 - ArcGraph Modalita' comparativa controllata

## Stato
COMPLETATA NEL PERIMETRO SICURO

## Obiettivo

Confrontare ArcGraph terrain e MapGrid legacy in modo controllato, evitando un doppio renderer permanente.

La modalita' comparativa dovra':

- essere attivabile solo in debug/test;
- verificare scala, tile, chunk, ordinamento e camera;
- permettere audit visuale del terrain renderer;
- non diventare percorso runtime stabile.

## Checkpoint v0.33

| Checkpoint | Task | Stato |
|---|---|---|
| v0.33a | Audit view/camera legacy e registrazione decisioni zoom, pan, LOD visuale | Completato |
| v0.33b | Contratto ArcGraph View/Camera: config, stato vista, input ammessi, output vietati | Completato |
| v0.33c | Configurazione mappa e zoom: dimensione 250x250, livelli zoom JSON, celle visibili | Completato |
| v0.33d | Controller pan/zoom discreto: rotellina per zoom, rotellina premuta per pan | Completato |
| v0.33e | Conversione coordinate: screen -> world -> cella, clamp viewport e no pan a zoom 1 | Completato |
| v0.33f | Policy LOD per zoom: icone, sprite statici, aggregazioni, animazioni disabilitate ai livelli 1/2 | Completato |
| v0.33g | Modalita' comparativa terrain ArcGraph/MapGrid: aggancio debug/test senza doppio renderer permanente | Completato nel perimetro gate/diagnostica |
| v0.33h | QA, diff scope, closeout e preparazione v0.34 | Completato |

## Decisioni v0.33 - Zoom, pan e rappresentazione semplificata

Decisioni operative registrate:

- la mappa prevista per la nuova fase e' `250x250` celle;
- lo zoom ha quattro livelli fissi;
- la rotellina mouse modifica lo zoom di un livello per scatto;
- il verso della rotellina decide zoom avanti o indietro;
- zoom livello 1: `300x300` celle visibili;
- zoom livello 2: `150x150` celle visibili;
- zoom livello 3: `75x75` celle visibili;
- zoom livello 4: `20x20` celle visibili;
- a zoom livello 1 non e' previsto pan;
- dimensione mappa e livelli zoom devono essere salvati in JSON di configurazione mappa;
- il pan si ottiene premendo la rotellina mouse e tenendola premuta mentre si muove il mouse;
- zoom 1 e zoom 2 non usano animazioni sprite;
- zoom 1 e zoom 2 non usano vestizione NPC a layer;
- zoom 1 e zoom 2 usano rappresentazioni semplificate: icone, sprite statici di categoria, aggregazioni d'area e filtri di visibilita'.

Policy visuale consigliata:

```text
Zoom 1:
vista strategica/cartografica
no animazioni
no pan
NPC/animali come marker o icone
vegetazione come aree aggregate
oggetti piccoli nascosti

Zoom 2:
vista ampia della colonia
no animazioni
NPC/animali come mini sagome o icone ruolo
piante e strutture come sprite statici semplificati
oggetti minori nascosti salvo debug/selezione

Zoom 3:
vista normale
sprite normali statici o animazione ridotta
oggetti principali visibili

Zoom 4:
vista ravvicinata
sprite completi
animazioni
vestizione NPC a layer
effetti visuali locali piu' leggibili
```

Regola architetturale:

```text
La simulazione non cambia con lo zoom.
Cambia solo la RenderPolicy.
```

Esempio:

```text
NPC Marco in cella 120,80

Zoom 4 -> corpo completo, vestiti, animazione camminata.
Zoom 3 -> sprite completo statico o animazione ridotta.
Zoom 2 -> mini sagoma contadino.
Zoom 1 -> marker/colore ruolo.
```

## Esito v0.33a - Audit view/camera legacy

Audit eseguito sui moduli legacy e sui contratti ArcGraph esistenti.

File principali ispezionati:

```text
Assets/Scripts/Views/MapGrid/Runtime/MapGridBootstrap.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridCameraController.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridWorldView.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridPointerInputActionsProvider.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridConfig.cs
Assets/Resources/MapGrid/Config/MapGridConfig.json
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphBootstrapRuntime.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderState.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRuntimeContext.cs
```

### Stato legacy rilevato

`MapGridBootstrap`:

- carica `MapGridConfig` da JSON;
- carica layout e atlas;
- costruisce terrain chunk legacy;
- posiziona la camera al centro mappa;
- aggancia `MapGridCameraController`;
- aggancia `MapGridPointerInputActionsProvider`;
- aggancia `MapGridWorldView`.

`MapGridCameraController`:

- gestisce zoom con rotellina;
- supporta PixelPerfectCamera tramite `assetsPPU`;
- contiene fallback su `orthographicSize`;
- usa pan con tasto destro del mouse;
- contiene edge-pan legacy, ma attualmente e' commentato nel ciclo update;
- applica inerzia con `SmoothDamp`;
- clampa camera sui bounds mappa;
- usa `Mouse.current` e `EventSystem.current`;
- e' legato a `MapGridData` e `MapGridConfig`.

`MapGridWorldView`:

- risolve la camera con priorita' Inspector, cache, `Camera.main`, prima camera attiva;
- usa la camera per `ScreenToWorldPoint`;
- converte posizione mouse in cella;
- usa queste conversioni per overlay, tooltip, FOV, selezione NPC e click debug.

`MapGridPointerInputActionsProvider`:

- espone solo la posizione puntatore;
- non gestisce scroll;
- non gestisce pan;
- non gestisce pressione rotellina.

### Stato ArcGraph rilevato

ArcGraph oggi possiede:

- bootstrap C# passivo;
- runtime context esplicito;
- render state con `VisibleZLevel`, `TileSizeWorld`, `ChunkSizeCells`, dirty state;
- layer stack;
- adapter read-only;
- terrain mesh data builder.

ArcGraph oggi non possiede ancora:

- controller camera;
- stato viewport;
- stato zoom;
- stato pan;
- conversione screen/world/cell;
- input view dedicato;
- bridge verso `Camera`;
- policy LOD per zoom;
- wrapper Unity comparativo.

### Valutazione

Il controller legacy non va copiato direttamente.

Motivi:

- usa una logica zoom continua o semi-continua, mentre ArcGraph richiede quattro livelli discreti;
- usa RMB drag pan, mentre la decisione v0.33 richiede pan con rotellina premuta;
- e' legato a `MapGridData`;
- contiene policy PixelPerfectCamera specifica e reflection su campi opzionali;
- appartiene al renderer legacy che dovra' essere assorbito, non esteso.

La parte riusabile e':

- concetto di clamp ai bounds mappa;
- conversione screen -> world -> cella;
- controllo `EventSystem` per non catturare input sopra UI;
- attenzione pixel-perfect;
- distinzione tra camera assegnata e fallback.

### Direzione consigliata

`v0.33b` deve definire un contratto ArcGraph View/Camera separato dal terrain renderer.

Formula desiderata:

```text
ArcGraphMapViewConfig
+ ArcGraphZoomProfile
+ ArcGraphViewState
+ input mouse
+ Camera esplicita
-> ArcGraphViewController
-> stato camera/view aggiornato
```

Il ViewController dovra':

- essere view-only;
- non leggere `World`;
- non leggere decision layer;
- non mutare simulazione;
- non creare goal/job/eventi;
- non decidere rendering dei layer;
- limitarsi a gestire finestra visibile, zoom, pan, clamp e conversione coordinate.

## Esito v0.33b - Contratto ArcGraph View/Camera

Implementato il contratto passivo View/Camera per ArcGraph.

Nuovi file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewZoomLevelDefinition.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphMapViewConfig.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewCellRect.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewInputFrame.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewState.cs
```

### Cosa rappresentano

`ArcGraphViewZoomLevelDefinition`:

- definisce un livello zoom discreto;
- contiene celle visibili X/Y;
- dichiara se il pan e' ammesso;
- dichiara se le animazioni sprite sono ammesse;
- dichiara se la vestizione actor a layer e' ammessa;
- dichiara se il livello usa rappresentazione semplificata.

`ArcGraphMapViewConfig`:

- contiene dimensione mappa;
- contiene profilo zoom;
- contiene livello zoom iniziale;
- contiene policy rotellina;
- contiene policy pan con rotellina premuta;
- espone `CreateDefaultV033()` con la configurazione decisa:
  - mappa `250x250`;
  - zoom 1 `300x300`, no pan, no animazioni, no layer actor, rappresentazione semplificata;
  - zoom 2 `150x150`, pan, no animazioni, no layer actor, rappresentazione semplificata;
  - zoom 3 `75x75`, pan, animazioni ammesse, no layer actor;
  - zoom 4 `20x20`, pan, animazioni ammesse, layer actor ammessi.

`ArcGraphViewCellRect`:

- rappresenta il rettangolo discreto di celle visibili;
- usa limiti massimi esclusivi;
- non modifica mappa, dirty state o simulazione.

`ArcGraphViewInputFrame`:

- rappresenta input view gia' letto da un wrapper esterno;
- contiene scatti rotellina, stato rotellina premuta, delta mouse e posizione puntatore;
- non legge `Mouse.current`;
- non legge input Unity direttamente.

`ArcGraphViewState`:

- conserva centro vista e zoom attivo;
- calcola finestra celle visibili;
- clampa il centro ai bounds mappa;
- applica zoom discreto da delta rotellina gia' astratto;
- applica pan in coordinate cella solo se il livello zoom lo consente.

### Garanzie architetturali

Il contratto View/Camera:

- non e' `MonoBehaviour`;
- non legge `Camera.main`;
- non legge `Mouse.current`;
- non chiama `ScreenToWorldPoint`;
- non crea `GameObject`;
- non crea `Mesh`;
- non crea renderer;
- non legge `World`;
- non muta simulazione;
- non sostituisce MapGrid.

### Regola centrale

```text
ArcGraphViewState descrive come guardiamo la mappa.
ArcGraphRenderState descrive cosa il renderer deve aggiornare.
World descrive cosa esiste davvero nella simulazione.
```

Questi tre piani devono restare separati.

### Preparazione v0.33c

`v0.33c` dovra' trasformare il contratto in configurazione serializzabile.

Obiettivo:

```text
JSON config mappa
-> DTO configurazione view
-> ArcGraphMapViewConfig
-> ArcGraphViewState iniziale
```

La patch dovra' evitare:

- accesso diretto a scena;
- sostituzione di `MapGridConfig` prima del gate comparativo;
- modifiche a Core;
- modifica `.meta`;
- aggancio renderer automatico.

## Esito v0.33c - Config mappa/zoom JSON

Implementata la configurazione serializzabile per la view ArcGraph.

Nuovi file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphMapViewConfigJson.cs
Assets/Resources/ArcGraph/Config/ArcGraphViewConfig.json
```

### Strategia

Il caricamento e' separato in due fasi:

```text
TextAsset o stringa JSON ricevuta da un wrapper futuro
-> ArcGraphMapViewConfigJson.ParseOrDefault(json)
-> ArcGraphMapViewConfig
-> ArcGraphViewState.CreateDefault(config)
```

`v0.33c` non introduce ancora un loader con `Resources.Load`.

Motivo:

- ArcGraph deve restare passivo;
- il wrapper Unity futuro decidera' quando caricare il `TextAsset`;
- il contratto e' testabile passando direttamente una stringa JSON;
- il parser non dipende da scena, camera o input.

### JSON default

Il file default contiene:

- mappa `250x250`;
- zoom iniziale livello `1`;
- uno scatto rotellina per livello zoom;
- pan con rotellina premuta;
- zoom 1: `300x300`, no pan, no animazioni, no layer actor, rappresentazione semplificata;
- zoom 2: `150x150`, pan, no animazioni, no layer actor, rappresentazione semplificata;
- zoom 3: `75x75`, pan, animazioni ammesse, no layer actor;
- zoom 4: `20x20`, pan, animazioni ammesse, layer actor ammessi.

### DTO e conversione

`ArcGraphMapViewConfigJson.cs` contiene:

- `ArcGraphMapViewConfigJson`;
- `ArcGraphMapViewConfigDto`;
- `ArcGraphZoomLevelConfigDto`.

Il DTO:

- e' compatibile con `JsonUtility`;
- usa campi pubblici serializzabili;
- normalizza campi numerici assenti o invalidi;
- usa il profilo `CreateDefaultV033()` come fallback;
- converte in `ArcGraphMapViewConfig`;
- puo' creare anche `ArcGraphViewState` iniziale.

### Garanzie architetturali

La patch:

- non modifica `MapGridConfig`;
- non modifica `MapGridConfig.json`;
- non modifica `Core`;
- non modifica scene;
- non crea `.meta`;
- non crea camera controller;
- non crea `GameObject`;
- non crea renderer;
- non legge `Mouse.current`;
- non legge `Camera.main`;
- non chiama `ScreenToWorldPoint`.

### Nota QA

La compilazione isolata ArcGraph richiede ora anche:

```text
UnityEngine.JSONSerializeModule.dll
```

perche' il parser usa `JsonUtility`.

### Preparazione v0.33d

`v0.33d` potra' implementare il controller pan/zoom discreto usando:

```text
ArcGraphMapViewConfig
ArcGraphViewState
ArcGraphViewInputFrame
```

Il controller dovra' ancora evitare:

- aggancio produttivo alla scena;
- sostituzione MapGrid;
- letture globali;
- mutazioni simulazione.

## Esito v0.33d - Controller pan/zoom discreto

Implementato il controller passivo per zoom e pan ArcGraph.

Nuovi file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewController.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewControllerResult.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewControllerHarness.cs
```

### Funzionamento

`ArcGraphViewController`:

- riceve `ArcGraphMapViewConfig`;
- riceve `ArcGraphViewState`;
- riceve `ArcGraphViewInputFrame`;
- riceve dimensioni viewport in pixel;
- applica prima zoom discreto e poi pan;
- converte il delta mouse da pixel a celle in base allo zoom attivo;
- ignora l'input se `IsPointerOverUi` e' true;
- non applica pan se il livello zoom non lo consente;
- produce `ArcGraphViewControllerResult` diagnostico.

### Scelte operative

Ordine interno:

```text
input frame
-> blocco UI
-> zoom discreto
-> conversione pan pixel/celle
-> clamp via ArcGraphViewState
-> diagnostica
```

Il pan usa semantica "trascina la mappa":

```text
mouse verso destra
-> mappa percepita verso destra
-> centro view verso sinistra
```

Quindi il delta mouse viene convertito con segno negativo.

### Harness

`ArcGraphViewControllerHarness.RunDefaultSmoke()` verifica:

- zoom iniziale livello 1;
- zoom 1 clampa la vista all'intera mappa `250x250`;
- uno scatto rotellina porta da zoom 1 a zoom 2;
- pan con rotellina premuta applica movimento a zoom 2;
- input sopra UI viene ignorato e non modifica il centro vista.

### Garanzie architetturali

Il controller:

- non e' `MonoBehaviour`;
- non legge `Mouse.current`;
- non legge `Camera.main`;
- non chiama `ScreenToWorldPoint`;
- non crea `GameObject`;
- non crea renderer;
- non legge `World`;
- non muta simulazione;
- non sostituisce MapGrid.

### Preparazione v0.33e

`v0.33e` dovra' introdurre il contratto coordinate:

```text
screen pixel
-> normalized viewport point
-> visible cell rect
-> cella ArcGraph
```

Anche questo deve restare passivo e non usare ancora una camera Unity produttiva.

## Esito v0.33e - Coordinate screen/viewport/cell

Implementata la conversione coordinate passiva per ArcGraph.

Nuovi file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewCoordinateResult.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewCoordinateMapper.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewCoordinateMapperHarness.cs
```

### Funzionamento

Il mapper converte:

```text
punto viewport in pixel
-> coordinate normalizzate 0..1
-> ArcGraphViewState.ResolveVisibleCellRect()
-> ArcGraphCellCoord
```

La convenzione usata e':

```text
origine viewport = basso/sinistra
limite destro/alto = esclusivo
```

Quindi:

```text
x >= viewportWidth  -> fuori viewport
y >= viewportHeight -> fuori viewport
```

### Harness

`ArcGraphViewCoordinateMapperHarness.RunDefaultSmoke()` verifica:

- centro viewport a zoom 1 risolto sulla cella `125,125`;
- bordo destro esclusivo considerato fuori viewport;
- zoom 2 produce rettangolo visibile `150x150`;
- bottom-left a zoom 2 risolto sulla cella `50,50`.

### Garanzie architetturali

Il mapper:

- non usa `Camera`;
- non chiama `ScreenToWorldPoint`;
- non legge `Mouse.current`;
- non crea `GameObject`;
- non crea renderer;
- non legge `World`;
- non muta simulazione.

### Preparazione v0.33f

`v0.33f` dovra' trasformare la policy visuale per zoom in un contratto leggibile dai renderer:

```text
zoom level
-> render detail policy
-> animazioni on/off
-> sprite layered on/off
-> rappresentazione semplificata on/off
-> visibilita' layer minori
```

## Esito v0.33f - Policy LOD per zoom

Implementata la policy LOD visuale per i livelli zoom ArcGraph.

Nuovi file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodModes.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodProfile.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodPolicy.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodPolicyHarness.cs
```

### Funzionamento

`ArcGraphZoomLodPolicy` traduce un livello zoom in un profilo leggibile:

```text
zoom level
-> actor mode
-> vegetation mode
-> object mode
-> effect mode
-> animazioni on/off
-> actor layered on/off
-> rappresentazione semplificata on/off
-> item minori visibili/nascosti
```

### Profili default

Zoom 1:

- actor come marker strategico;
- vegetazione aggregata per area;
- oggetti minori nascosti;
- effetti come segnali statici;
- niente animazioni;
- niente actor layered;
- rappresentazione semplificata.

Zoom 2:

- actor come sprite statico semplificato;
- vegetazione semplificata;
- solo oggetti importanti semplificati;
- effetti statici semplificati;
- niente animazioni;
- niente actor layered;
- rappresentazione semplificata.

Zoom 3:

- actor come sprite completo flat;
- animazioni ammesse;
- niente actor layered;
- oggetti statici visibili;
- item minori visibili.

Zoom 4:

- actor layered ammessi;
- animazioni ammesse;
- vegetazione animata individuale;
- oggetti dettagliati;
- effetti locali completi;
- item minori visibili.

### Harness

`ArcGraphZoomLodPolicyHarness.RunDefaultSmoke()` verifica i profili dei quattro zoom default.

### Garanzie architetturali

La policy:

- non modifica renderer esistenti;
- non carica asset;
- non crea sprite;
- non crea `GameObject`;
- non legge `World`;
- non muta simulazione;
- non sostituisce MapGrid.

### Preparazione v0.33g

`v0.33g` resta il punto piu' delicato della versione, perche' introduce la modalita' comparativa.

La direzione sicura e':

```text
contratto comparativo debug/test
-> diagnostica comparativa
-> nessun doppio renderer permanente
-> nessun aggancio produttivo automatico
```

Se l'aggancio scena/materiale/camera richiede una decisione non deducibile, lo step dovra' fermarsi prima della patch runtime.

## Esito v0.33g - Gate comparativo ArcGraph/MapGrid

Implementato il perimetro sicuro della modalita' comparativa.

Nuovi file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphComparisonMode.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphComparisonOptions.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphComparisonDiagnostics.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphComparisonGate.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphComparisonGateHarness.cs
```

### Scelta progettuale

`v0.33g` non aggancia ancora ArcGraph alla scena.

Motivo:

- l'aggancio reale richiede scelta esplicita su camera, materiali, sorting, parent GameObject e disattivazione;
- un aggancio automatico rischierebbe di creare un doppio renderer permanente;
- la roadmap richiede comparazione controllata, non sostituzione implicita.

### Cosa e' stato implementato

`ArcGraphComparisonGate` valuta se una richiesta comparativa e' ammessa.

Input dichiarativi:

- esiste renderer legacy;
- esistono dati terrain ArcGraph;
- esiste camera;
- esiste materiale;
- modalita' richiesta;
- MapGrid resta primario;
- scena attachment ammesso o no;
- doppio renderer permanente ammesso o no.

Output:

- `ArcGraphComparisonDiagnostics`;
- `IsAllowed`;
- `CanAttachSceneProbe`;
- `Reason`;
- flag dei prerequisiti;
- flag rischio doppio renderer permanente.

### Modalita'

```text
Disabled
DiagnosticsOnly
TemporaryDebugSceneProbe
```

`DiagnosticsOnly` puo' essere ammessa senza camera o materiale.

`TemporaryDebugSceneProbe` puo' essere ammessa solo se:

- MapGrid resta primario;
- attivazione debug esplicita richiesta;
- scene attachment e' consentito;
- doppio renderer permanente e' vietato;
- legacy renderer presente;
- dati terrain ArcGraph presenti;
- camera presente;
- materiale presente.

### Harness

`ArcGraphComparisonGateHarness.RunDefaultSmoke()` verifica:

- diagnostics-only ammessa senza scene probe;
- doppio renderer permanente bloccato;
- scene probe temporaneo ammesso con prerequisiti;
- scene probe bloccato se manca materiale.

### Garanzie architetturali

La patch:

- non crea `GameObject`;
- non crea `MeshRenderer`;
- non crea `MeshFilter`;
- non carica materiali;
- non legge camera;
- non accende ArcGraph in scena;
- non sostituisce MapGrid;
- non modifica Core;
- non modifica MapGrid.

### Debito esplicito

Il vero bridge scena comparativo resta fuori da `v0.33g`.

Prima di implementarlo serve decisione operatore su:

- dove montare il GameObject debug;
- come fornire materiale/atlas;
- come garantire spegnimento completo;
- se mostrare overlay affiancato, sovrapposto o alternato;
- come evitare sorting ambiguo con MapGrid.

## Esito v0.33h - QA finale e closeout

La `v0.33` e' chiusa nel perimetro sicuro.

La versione ha prodotto la base controllata per:

- configurare dimensione mappa e zoom discreto;
- rappresentare lo stato vista senza dipendere direttamente da Unity `Camera`;
- ricevere input mouse gia' filtrato da un adapter esterno;
- applicare zoom e pan in modo deterministico;
- convertire coordinate viewport in coordinate cella;
- decidere la policy LOD visuale per i quattro livelli zoom;
- valutare se una modalita' comparativa ArcGraph/MapGrid sia ammessa;
- impedire che ArcGraph diventi un secondo renderer permanente non controllato.

### Definition of Done v0.33

Completato:

- audit del sistema view/camera legacy;
- registrazione decisioni zoom/pan/LOD;
- contratto ArcGraph View/Camera;
- configurazione JSON per mappa `250x250` e quattro zoom;
- controller pan/zoom passivo;
- mapper coordinate viewport/cella;
- policy LOD per zoom;
- gate diagnostico per comparazione ArcGraph/MapGrid;
- QA finale su scope diff, chiamate vietate e assenza di modifiche fuori perimetro.

### Cosa non e' stato implementato

Non sono stati implementati:

- aggancio reale di ArcGraph alla scena;
- creazione di `GameObject` debug;
- creazione o assegnazione di `MeshRenderer` / `MeshFilter`;
- caricamento materiale o atlas;
- lettura diretta di `Camera.main`;
- lettura diretta di input mouse;
- doppio renderer permanente;
- sostituzione operativa di MapGrid;
- modifiche a Core, Decision Layer, Job Layer o scene.

Questa scelta e' intenzionale.

La `v0.33` doveva costruire una modalita' comparativa controllata, non introdurre un bridge scena ambiguo.

### QA finale

Verifiche eseguite:

- ricerca chiamate operative vietate dentro `Assets/Scripts/Views/ArcGraph/Runtime`;
- verifica diff scope rispetto a `v0.32h`;
- controllo assenza modifiche a `Core`, `MapGrid`, scene Unity, `.meta`, `Library`, `Temp`, `Obj`.

Esito:

```text
QA scope superata.
```

Nota compilazione:

La compilazione completa Unity non e' stata rieseguita in `v0.33h`, perche' il build da `Assembly-CSharp.csproj` richiede il ripristino di asset in `Temp`, area che Codex non deve modificare.

Lo step `v0.33h` modifica solo documentazione operativa.

### Debiti residui

Prima di un vero bridge visuale comparativo serviranno decisioni esplicite su:

- parent `GameObject` del probe ArcGraph;
- materiale e atlas da usare;
- sorting rispetto a MapGrid;
- camera di riferimento;
- modalita' di confronto visuale: overlay, affiancata o alternata;
- spegnimento completo del probe debug;
- responsabilita' del wrapper Unity che colleghera' i contratti passivi alla scena.

### Preparazione v0.34

La `v0.34` puo' partire come step actor/object renderer.

Per coerenza con `v0.31`-`v0.33`, la prima parte della `v0.34` dovrebbe restare passiva:

- leggere `ArcGraphActorLayer`;
- leggere `ArcGraphObjectLayer`;
- produrre dati renderizzabili;
- rispettare la policy LOD gia' definita;
- evitare ancora agganci scena permanenti fino a decisione esplicita.

---

#### v0.34 - ArcGraph Actor/Object Renderer

## Stato
COMPLETATA NEL PERIMETRO PASSIVO

## Obiettivo

Portare dentro ArcGraph la visualizzazione base di oggetti e attori.

Il renderer dovra':

- usare `ArcGraphObjectLayer`;
- usare `ArcGraphActorLayer`;
- disegnare sprite singoli provvisori;
- mantenere sorting semplice e leggibile;
- non introdurre ancora vestizione modulare completa;
- non spostare la posizione simulativa degli NPC.

## Checkpoint v0.34

| Checkpoint | Task | Stato |
|---|---|---|
| v0.34a | Audit actor/object layer, snapshot, adapter e policy LOD | Completato |
| v0.34b | Contratti render item passivi per actor/object | Completato |
| v0.34c | Builder object render queue | Completato |
| v0.34d | Builder actor render queue | Completato |
| v0.34e | Sorting e filtri LOD per zoom | Completato |
| v0.34f | Harness smoke actor/object senza scena | Completato |
| v0.34g | QA, closeout e preparazione v0.35 | Completato |

## Vincolo v0.34

La `v0.34` resta nel perimetro passivo.

Non deve ancora:

- creare `GameObject`;
- creare `SpriteRenderer`;
- caricare sprite, atlas o materiali;
- modificare scene Unity;
- sostituire MapGrid;
- introdurre doppio renderer permanente;
- implementare vestizione NPC a layer;
- collegare il movimento multi-tick reale.

Formula operativa:

```text
ArcGraphActorLayer / ArcGraphObjectLayer
-> snapshot visuali
-> render item passivi
-> render queue ordinata
-> futuro wrapper Unity
```

## Esito v0.34a - Audit actor/object renderer passivo

Audit eseguito sui contratti ArcGraph gia' presenti.

File principali ispezionati:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphActorLayer.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphActorVisualSnapshot.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphObjectLayer.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphObjectVisualSnapshot.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphWorldAdapter.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodModes.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodProfile.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodPolicy.cs
```

### Stato esistente

`ArcGraphObjectVisualSnapshot` contiene gia':

- id oggetto runtime;
- `DefId`;
- cella discreta;
- `SpriteKey`;
- stato trasportato;
- eventuale holder actor;
- eventuale stock food.

`ArcGraphActorVisualSnapshot` contiene gia':

- id actor;
- cella discreta;
- sprite base provvisorio;
- motion snapshot opzionale;
- metodo `ResolvePose()` per ottenere posa visuale frazionaria.

`ArcGraphObjectLayer` e `ArcGraphActorLayer`:

- conservano snapshot in dizionari interni;
- sostituiscono snapshot in modo conservativo;
- marcano celle/chunk dirty;
- espongono lettura puntuale per id;
- non leggono `World`;
- non caricano sprite;
- non creano oggetti Unity.

`ArcGraphWorldAdapter`:

- produce snapshot terrain/object/actor;
- legge `World` solo come sorgente oggettiva per la view;
- non muta simulazione;
- non risolve asset;
- per gli actor usa ancora motion inattivo, perche' il bridge movimento reale appartiene a `v0.35`.

`ArcGraphZoomLodPolicy`:

- distingue quattro livelli zoom;
- disabilita animazioni e layered actor a zoom 1/2;
- abilita sprite completi a zoom 3;
- abilita actor layered solo a zoom 4, se consentito dal profilo.

### Mancanze rilevate

Per costruire un actor/object renderer passivo mancano:

- contratti `RenderItem` value-only per actor e oggetti;
- una render queue ordinata;
- una diagnostica minima della queue;
- una policy di sorting stabile;
- filtri LOD applicati in modo centralizzato;
- un modo read-only per enumerare gli snapshot contenuti nei layer.

### Punto delicato: enumerazione dei layer

I layer attuali espongono `TryGetActor` e `TryGetObject`, ma non espongono ancora tutti gli snapshot.

Una render queue non deve interrogare il `World`, quindi non puo' ricostruire la lista degli id da fuori.

Soluzione consigliata:

```text
ArcGraphActorLayer.CopySnapshotsTo(...)
ArcGraphObjectLayer.CopySnapshotsTo(...)
```

Questa forma e' preferibile a esporre direttamente il dizionario interno.

Motivo:

- mantiene la cache privata;
- produce copie value-only;
- permette al builder di ordinare fuori dal layer;
- non introduce authority simulativa;
- non apre accesso mutabile allo stato interno.

### Punto delicato: oggetti minori

La policy LOD prevede `HideMinorObjects`, ma lo snapshot oggetto attuale non contiene ancora un campo affidabile per distinguere oggetto minore da oggetto importante.

In `v0.34` non conviene inventare una classificazione.

Scelta consigliata:

- non nascondere oggetti solo per supposizione;
- propagare nel render item il fatto che il profilo LOD richiede rappresentazione semplificata;
- rinviare la vera classificazione `minor/important` a quando esistera' un dato esplicito nel catalogo oggetti o nello snapshot.

### Prossimo passo v0.34b

Definire i contratti passivi:

- `ArcGraphRenderItemKind`;
- `ArcGraphActorRenderItem`;
- `ArcGraphObjectRenderItem`;
- `ArcGraphRenderQueueDiagnostics`;
- eventuale helper di sorting/priority value-only.

Questi contratti non dovranno:

- creare sprite;
- leggere asset;
- leggere camera;
- leggere input;
- modificare layer o world.

## Esito v0.34b - Contratti render item passivi

Implementati i contratti value-only per rappresentare actor e oggetti come item renderizzabili.

Nuovi file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderItemKind.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderSortKey.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphActorRenderItem.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphObjectRenderItem.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderQueueDiagnostics.cs
```

### Cosa rappresentano

`ArcGraphActorRenderItem`:

- id actor;
- cella discreta;
- posizione visuale frazionaria;
- sprite key provvisoria;
- modalita' LOD actor;
- stato movimento visuale;
- flag animazione/layered/simplified;
- sorting key;
- stato visibile/nascosto con motivo.

`ArcGraphObjectRenderItem`:

- id oggetto;
- `DefId`;
- cella;
- sprite key provvisoria;
- modalita' LOD oggetto;
- stato held/holder;
- eventuale food stock;
- flag simplified/show minor items;
- sorting key;
- stato visibile/nascosto con motivo.

`ArcGraphRenderSortKey`:

- prepara ordinamento deterministico senza usare sorting Unity;
- ordina per `Z`, `Y`, `X`, layer visuale, tipo item e id;
- non legge camera o scena.

`ArcGraphRenderQueueDiagnostics`:

- contiene contatori actor/object/visible/hidden;
- permette harness e QA senza renderer concreto.

### Garanzie

La patch:

- non crea `GameObject`;
- non crea `SpriteRenderer`;
- non carica asset;
- non legge `World`;
- non legge camera;
- non modifica MapGrid;
- non modifica Core.

### Prossimo passo v0.34c

Costruire il builder object queue:

```text
ArcGraphObjectLayer
-> snapshot oggetto
-> ArcGraphObjectRenderItem
-> lista ordinabile
```

Per farlo serve aggiungere al layer un metodo read-only di copia snapshot, senza esporre il dizionario interno.

## Esito v0.34c - Builder object render queue

Implementata la costruzione passiva della queue oggetti.

File modificati:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphObjectLayer.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphObjectRenderQueueBuilder.cs
```

### Aggiunte principali

`ArcGraphObjectLayer.CopySnapshotsTo(...)`:

- copia gli snapshot oggetto in una lista fornita dal chiamante;
- non espone il dizionario interno;
- non legge `World`;
- non modifica gli oggetti;
- consente al builder di lavorare solo su snapshot value-only.

`ArcGraphObjectRenderQueueBuilder`:

- legge `ArcGraphObjectLayer`;
- applica `ArcGraphZoomLodProfile`;
- produce `ArcGraphObjectRenderItem`;
- calcola `ArcGraphRenderSortKey`;
- ordina la lista target;
- produce `ArcGraphRenderQueueDiagnostics`.

### Policy v0.34c

Gli oggetti trasportati (`IsHeld`) vengono marcati nascosti con motivo `HeldObject`.

Motivo:

- un oggetto tenuto da un actor non dovrebbe essere disegnato come oggetto appoggiato alla cella;
- in futuro potra' essere mostrato come parte dell'actor o come attachment;
- in `v0.34` non viene ancora implementato quel caso.

Gli oggetti senza `SpriteKey` vengono marcati nascosti con motivo `MissingSpriteKey`.

La policy `HideMinorObjects` non viene usata per nascondere oggetti in assenza di un dato esplicito `minor/important`.

### Prossimo passo v0.34d

Costruire il builder actor queue:

```text
ArcGraphActorLayer
-> snapshot actor
-> posa visuale risolta
-> ArcGraphActorRenderItem
-> lista ordinabile
```

Anche qui servira' un metodo read-only di copia snapshot su `ArcGraphActorLayer`.

## Esito v0.34d - Builder actor render queue

Implementata la costruzione passiva della queue actor.

File modificati:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphActorLayer.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphActorRenderQueueBuilder.cs
```

### Aggiunte principali

`ArcGraphActorLayer.CopySnapshotsTo(...)`:

- copia gli snapshot actor in una lista fornita dal chiamante;
- non espone il dizionario interno;
- non legge `World`;
- non modifica NPC;
- permette al builder di restare completamente snapshot-only.

`ArcGraphActorRenderQueueBuilder`:

- legge `ArcGraphActorLayer`;
- applica `ArcGraphZoomLodProfile`;
- risolve la posa visuale tramite `ArcGraphActorVisualSnapshot.ResolvePose()`;
- produce `ArcGraphActorRenderItem`;
- calcola `ArcGraphRenderSortKey`;
- ordina la lista target;
- produce `ArcGraphRenderQueueDiagnostics`.

### Policy v0.34d

Actor con id non valido vengono marcati nascosti con motivo `InvalidActorId`.

Actor senza sprite key vengono marcati nascosti con motivo `MissingSpriteKey`.

La posa visuale usa gia' `ArcGraphActorVisualPoseSnapshot`.

Questo significa che quando `v0.35` alimentera' il motion reale, la queue actor potra' mostrare coordinate frazionarie senza cambiare contratto.

### Prossimo passo v0.34e

Costruire una queue combinata actor/object o un helper comune che:

- raccolga actor item;
- raccolga object item;
- applichi sorting condiviso;
- produca diagnostica aggregata;
- renda piu' semplice l'harness `v0.34f`.

## Esito v0.34e - Queue combinata e sorting globale

Implementata la queue actor/object combinata.

Nuovi file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderQueueEntry.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderQueue.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderQueueBuilder.cs
```

### Aggiunte principali

`ArcGraphRenderQueueEntry`:

- collega l'ordine globale a un item actor o object;
- conserva `Kind`, indice item, entity id e sort key;
- evita di fondere payload actor e payload object in un tipo generico ambiguo.

`ArcGraphRenderQueue`:

- conserva liste tipizzate actor/object;
- conserva entries ordinate globali;
- conserva diagnostica aggregata;
- non crea renderer concreto.

`ArcGraphRenderQueueBuilder`:

- usa `ArcGraphActorRenderQueueBuilder`;
- usa `ArcGraphObjectRenderQueueBuilder`;
- costruisce entries globali;
- ordina actor e oggetti con `ArcGraphRenderSortKey`;
- aggrega diagnostica actor/object.

### Sorting

L'ordinamento globale usa:

```text
Z
Y
X
VisualLayerOrder
Kind
EntityId
```

Con la policy attuale:

- oggetti: `VisualLayerOrder = 10`;
- actor: `VisualLayerOrder = 20`.

Quindi, a parita' di cella, l'actor viene dopo l'oggetto e puo' essere disegnato sopra nel wrapper futuro.

### Prossimo passo v0.34f

Costruire un harness smoke che:

- crea actor layer;
- crea object layer;
- inserisce snapshot minimi;
- risolve un profilo LOD;
- costruisce la queue combinata;
- verifica contatori, ordine e motivi hidden;
- non usa scena, asset o renderer Unity.

## Esito v0.34f - Harness smoke actor/object

Implementato harness statico per validare la queue actor/object.

Nuovo file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderQueueHarness.cs
```

### Scenario verificato

Lo smoke test crea:

- un actor visibile;
- un actor nascosto per sprite key mancante;
- un oggetto visibile;
- un oggetto hidden perche' held;
- un oggetto hidden per sprite key mancante.

Actor visibile e oggetto visibile vengono posizionati sulla stessa cella.

Verifica attesa:

```text
ActorItemCount = 2
ObjectItemCount = 3
VisibleItemCount = 2
HiddenItemCount = 3
EntryCount = 2
ObjectBeforeActorOnSameCell = true
```

### Garanzie

L'harness:

- non crea `GameObject`;
- non crea `SpriteRenderer`;
- non carica asset;
- non legge `World`;
- non legge camera;
- non usa input;
- non modifica MapGrid;
- non modifica Core.

### Prossimo passo v0.34g

Eseguire QA finale e closeout:

- diff scope;
- ricerca chiamate vietate;
- verifica documentale roadmap/taskboard;
- diario Notion;
- preparazione `v0.35`.

## Esito v0.34g - QA finale e closeout

La `v0.34` e' chiusa nel perimetro passivo.

La versione ha prodotto:

- contratti actor/object render item;
- sort key deterministica;
- diagnostica render queue;
- copia sequenziale read-only da actor/object layer;
- builder object render queue;
- builder actor render queue;
- queue combinata actor/object;
- entries globali ordinate;
- harness smoke senza scena.

### Definition of Done v0.34

Completato:

- audit actor/object layer e snapshot;
- contratti `ArcGraphActorRenderItem` e `ArcGraphObjectRenderItem`;
- `ArcGraphRenderSortKey`;
- `ArcGraphRenderQueueDiagnostics`;
- `ArcGraphObjectRenderQueueBuilder`;
- `ArcGraphActorRenderQueueBuilder`;
- `ArcGraphRenderQueue`;
- `ArcGraphRenderQueueBuilder`;
- `ArcGraphRenderQueueHarness`.

### Cosa non e' stato implementato

Non sono stati implementati:

- `GameObject`;
- `SpriteRenderer`;
- caricamento sprite;
- caricamento atlas;
- caricamento materiali;
- aggancio camera;
- modifica scene Unity;
- sostituzione MapGrid;
- doppio renderer permanente;
- vestizione actor a layer;
- movimento multi-tick reale.

### QA finale

Verifiche eseguite:

- diff scope rispetto a `v0.33h`;
- ricerca chiamate operative vietate in `Assets/Scripts/Views/ArcGraph/Runtime`;
- controllo assenza modifiche a `Core`, `MapGrid`, scene Unity, `.meta`, `Library`, `Temp`, `Obj`;
- controllo `git diff --check`.

Esito:

```text
QA scope superata.
```

Nota compilazione:

La build Unity completa non e' stata rieseguita in `v0.34g`, per lo stesso motivo documentato in `v0.33h`: il build da `.csproj` richiede restore in `Temp`, area che Codex non deve modificare.

### Debiti residui

Restano fuori dalla `v0.34`:

- bridge scena reale;
- risoluzione sprite/atlas/materiali;
- classificazione oggetti `minor/important`;
- actor layered sprites effettivi;
- interpolazione movimento reale da Job Layer.

### Preparazione v0.35

La `v0.35` dovra' concentrarsi solo sul bridge movimento actor:

```text
running movement read-only
-> origine/destinazione/progresso
-> ArcGraphActorMotionSnapshot
-> ArcGraphActorVisualPoseSnapshot
-> actor render item con posizione frazionaria
```

La `v0.35` non dovra' permettere alla view di:

- chiamare `SetNpcPos`;
- completare job;
- interrompere running action;
- correggere pathfinding;
- decidere destinazioni.

---

#### v0.35 - ArcGraph Actor Motion Runtime Bridge

## Stato
AUDIT-FIRST / STOP PROGETTUALE

## Obiettivo

Collegare il movimento multi-tick reale alla posa visuale ArcGraph.

Il bridge dovra':

- ottenere origine e destinazione del segmento movimento in modo read-only;
- usare tick trascorsi e tick richiesti della running action;
- alimentare `ArcGraphActorVisualPoseSnapshot`;
- evitare che il renderer chiami `SetNpcPos`;
- evitare che la view completi o interrompa job.

## Checkpoint v0.35

| Checkpoint | Task | Stato |
|---|---|---|
| v0.35a | Audit movimento multi-tick e dati read-only disponibili | Completato con stop progettuale |
| v0.35b | Scelta contratto read-only del segmento movimento | Confermata |
| v0.35c | Implementazione contratto motion read-only | Completato |
| v0.35d | Integrazione adapter ArcGraph actor motion | Completato |
| v0.35e | Harness motion actor senza scena | Completato tramite test EditMode |
| v0.35f | QA e closeout v0.35 | Completato |

## Esito v0.35a - Audit movimento multi-tick e stop progettuale

Audit eseguito sui file:

```text
Assets/Scripts/Core/Jobs/RunningActionStore.cs
Assets/Scripts/Core/Jobs/RunningActionRuntimeState.cs
Assets/Scripts/Core/Jobs/RunningActionExecutor.cs
Assets/Scripts/Core/Jobs/MoveToRunningActionDriver.cs
Assets/Scripts/Core/Jobs/JobRuntimeState.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphWorldAdapter.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphActorVisualSnapshot.cs
```

### Stato attuale del movimento Job

Il movimento multi-tick reale esiste gia'.

`MoveToRunningActionDriver`:

- riceve la cella NPC corrente;
- riceve la cella target del singolo passo;
- crea una `RunningActionRuntimeState` di tipo `Movement`;
- avanza `ElapsedTicks`;
- usa `RequiredTicks`;
- chiama `world.SetNpcPos(...)` solo a completion;
- pulisce la running action quando il passo termina o fallisce.

`RunningActionStore`:

- conserva running action indicizzate da `RunningActionKey`;
- espone `GetSnapshots()`;
- restituisce snapshot value-only;
- non espone il dizionario interno.

`RunningActionProgressSnapshot` contiene:

- `NpcId`;
- `JobId`;
- `PhaseId`;
- `JobActionId`;
- `Kind`;
- `ElapsedTicks`;
- `RequiredTicks`;
- `Status`;
- `CanComplete`;
- `IsTimedOut`.

### Dato mancante

Lo snapshot running action non contiene:

- cella origine del segmento;
- cella destinazione del segmento.

Il driver MoveTo conosce questi dati mentre esegue:

```text
npcCell              = origine del passo corrente
action.TargetCell    = destinazione del passo corrente
```

Ma questi valori non vengono salvati nello stato read-only della running action.

### Perche' non si puo' procedere in modo sicuro senza decisione

Tecnicamente l'origine e la destinazione sono presenti nel nome:

```text
move_{jobId}_{phaseIndex}_{actionIndex}_{fromX}_{fromY}_{toX}_{toY}
```

Pero' usare questa stringa come fonte dati sarebbe sbagliato.

Motivi:

- e' parsing fragile;
- non e' un contratto tipizzato;
- puo' rompersi se cambia naming;
- mescola diagnostica e dati runtime;
- renderebbe ArcGraph dipendente da una convenzione interna del Job Layer.

### Stop progettuale

Prima di implementare `v0.35c` serve scegliere come esporre il segmento movimento.

Opzione consigliata:

```text
aggiungere un metadata read-only tipizzato alla running action Movement
```

Esempio concettuale:

```text
RunningActionMovementSnapshot
├─ HasMovementSegment
├─ FromCellX
├─ FromCellY
├─ ToCellX
├─ ToCellY
├─ ElapsedTicks
└─ RequiredTicks
```

Oppure estendere `RunningActionProgressSnapshot` con un blocco opzionale movement.

### Scelta richiesta

Serve approvazione su una di queste strade:

1. **Estendere `RunningActionProgressSnapshot` con dati movement opzionali.**
   Soluzione piu' diretta. Tocca il Job Layer ma resta read-only.

2. **Aggiungere un nuovo `RunningActionMovementMetadata` dentro `RunningActionRuntimeState`.**
   Soluzione piu' pulita e scalabile. Richiede piu' codice ma separa progress generico e metadata movement.

3. **Creare un adapter ArcGraph che parsifica `ActionInstanceId`.**
   Sconsigliata. Piu' veloce ma fragile e non architetturalmente pulita.

Decisione consigliata:

```text
Opzione 2: metadata movement tipizzato dentro la running action,
poi snapshot read-only per ArcGraph.
```

Motivo:

- preserva il Job Layer come sorgente del movimento;
- evita parsing stringhe;
- non permette alla view di mutare nulla;
- prepara futuri motion kind come walk/run/slow;
- tiene ArcGraph su un contratto leggibile.

### Stato v0.35a

`v0.35a` si e' chiusa con stop progettuale. La scelta e' stata poi confermata
dall'operatore nel checkpoint successivo.

## Esito v0.35b - Decisione contratto motion read-only tipizzato

Decisione operatore:

```text
Confermo aggiungere metadata movement tipizzato dentro RunningActionRuntimeState
e propagarlo nello snapshot read-only.

Mantieni strutture dati piu' agili possibile per non stressare la cpu.
```

Forma implementativa approvata:

- aggiungere `RunningActionMovementSnapshot` come value type minimale;
- conservare solo flag presenza e coordinate intere from/to;
- non usare `Vector2Int` nel Core per evitare dipendenza Unity aggiuntiva;
- aggiungere `RunningActionRuntimeState.StartMovement(...)`;
- propagare il dato in `RunningActionProgressSnapshot.Movement`;
- non parsare `ActionInstanceId`;
- non dedurre movimento dalla differenza tra celle successive;
- non permettere ad ArcGraph di chiamare `SetNpcPos`;
- non permettere ad ArcGraph di completare o interrompere job.

Scelta CPU:

```text
RunningActionStore
├─ _states: RunningActionKey -> RunningActionRuntimeState
└─ _activeMovementByNpc: npcId -> RunningActionKey
```

Motivo:

- ArcGraph puo' interrogare il movimento di un NPC in modo diretto;
- evita `GetSnapshots()` durante la costruzione degli actor snapshot;
- evita una scansione completa delle running action per ogni actor;
- mantiene l'indice derivato piccolo e pulito dai normali path di clear.

Bridge previsto:

```text
MoveToRunningActionDriver
-> RunningActionRuntimeState.StartMovement(...)
-> RunningActionProgressSnapshot.Movement
-> RunningActionStore.TryGetActiveMovementSnapshotForNpc(...)
-> ArcGraphWorldAdapter.FillActorSnapshots(...)
-> ArcGraphActorMotionSnapshot.CreateMovement(...)
-> ArcGraphActorVisualPoseSnapshot.ResolvePose()
```

## Esito v0.35f - Closeout bridge motion actor

La `v0.35` ha collegato il movimento multi-tick reale alla posa visuale ArcGraph
senza spostare authority verso la view.

Implementato:

- `RunningActionMovementSnapshot` come value type minimale;
- `RunningActionRuntimeState.StartMovement(...)`;
- propagazione `Movement` dentro `RunningActionProgressSnapshot`;
- creazione movement metadata in `MoveToRunningActionDriver`;
- lookup read-only `RunningActionStore.TryGetActiveMovementSnapshotForNpc(...)`;
- indice interno `npcId -> RunningActionKey` per evitare scansioni per actor;
- traduzione in `ArcGraphActorMotionSnapshot` dentro `ArcGraphWorldAdapter`;
- fallback a motion inattivo quando non esiste segmento attivo;
- test EditMode sul metadata movement e sulla lookup indicizzata.

Vincoli rispettati:

- ArcGraph non chiama `SetNpcPos`;
- ArcGraph non completa job;
- ArcGraph non interrompe running action;
- ArcGraph non parsa `ActionInstanceId`;
- ArcGraph non deduce movimento da differenze tra celle successive;
- MapGrid resta renderer produttivo;
- nessun asset load;
- nessun `GameObject`;
- nessuna scena modificata.

QA eseguito:

- `git diff --check`;
- ricerca nuove chiamate operative vietate nel diff runtime;
- verifica assenza modifiche a `.meta`, `Library`, `Temp`, `Obj`;
- verifica riferimenti a `StartMovement`, `TryGetActiveMovementSnapshotForNpc` e test dedicati.

Nota QA:

Non e' stata eseguita compilazione Unity batch per evitare scritture automatiche in
`Temp` / `Library` dentro questo step controllato.

---

#### v0.36 - ArcGraph Environment Visual Layers

## Stato
APERTO IN AUDIT PREPARATORIO

## Obiettivo

Introdurre i layer visuali ambientali, mantenendo separazione netta tra simulazione e resa grafica.

Questa versione non deve decidere se piove, se una pianta cresce, se una stanza e' buia o se il fuoco si propaga. Deve solo mostrare snapshot gia' prodotti da sistemi esterni.

| Versione | Sottopunto | Stato |
|---|---|---|
| v0.36a | Audit e contratto preparatorio layer ambientali visuali | Completato |
| v0.36.01 | Vegetation Renderer: erba animata, piante, variazioni stagionali visuali | Completato nel perimetro passivo |
| v0.36.02 | Water Renderer: acqua animata, profondita', bordi acqua/terra | Completato nel perimetro passivo |
| v0.36.03 | Light Renderer: giorno/notte, tinta globale, buio stanze, luci locali | Completato nel perimetro passivo |
| v0.36.03v | ArcGraph Visual Probe: frame dati controllato dei layer base | Completato nel perimetro data-only |
| v0.36.03v.01 | ArcGraph Scene Probe Renderer: primo disegno debug in Unity | Completato come renderer debug temporaneo |
| v0.36.03v.02 | ArcGraph First Visual Test QA: esecuzione e raccolta difetti visivi | Completato con test manuale positivo |
| v0.36.04 | Effect Renderer: fiamme, fumo, scintille, effetti locali | Completato nel perimetro passivo |
| v0.36.05 | Weather Renderer: pioggia, neve, vento visuale, overlay atmosferico | Completato nel perimetro passivo |

## Prompt operativo - roadmap residua ArcGraph

Prima di proseguire oltre il primo test visivo, il lavoro residuo della roadmap ArcGraph e' questo:

```text
v0.37    -> Debug/Overlay Migration: migrazione progressiva overlay diagnostici da MapGridWorldView
v0.38    -> Legacy Absorption / Retirement: assorbimento e pensionamento controllato del rendering MapGrid legacy
```

Regola di sequenza:

- non introdurre effetti o meteo produttivi;
- non sostituire MapGrid;
- non fondere i layer ambientali in una queue globale finche' sorting e composizione non sono stati verificati;
- usare il probe visuale appena validato come controllo anti-accumulo prima dei moduli successivi.

## Esito v0.36.05 - Weather Renderer passivo

La `v0.36.05` ha completato la preparazione passiva del renderer meteo ArcGraph.

Il meteo non viene ancora disegnato in scena, non genera precipitazioni produttive, non modifica temperatura,
umidita' o altri stati simulativi, e non usa `ParticleSystem`, `Animator`, `Resources.Load` o creazione di
oggetti Unity.

File/runtime aggiunti o aggiornati:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderItemKind.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphWeatherRenderItem.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphWeatherRenderQueueDiagnostics.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphWeatherRenderQueueBuilder.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphWeatherRenderQueueHarness.cs
```

Elementi introdotti:

- aggiunto `ArcGraphRenderItemKind.Weather`;
- aggiunto `ArcGraphWeatherRenderItem`;
- aggiunto `ArcGraphWeatherRenderQueueDiagnostics`;
- aggiunto `ArcGraphWeatherRenderQueueBuilder`;
- aggiunto `ArcGraphWeatherRenderQueueHarness`.

Comportamento preparato:

- `ArcGraphWeatherRenderQueueBuilder` legge solo `ArcGraphWeatherLayer`;
- produce al massimo un item meteo globale per livello Z visibile;
- applica il contratto visuale `Weather` gia' definito nella `v0.36a`;
- rispetta la policy LOD `v0.33f`;
- mantiene overlay meteo visibile anche agli zoom semplificati;
- disattiva l'animazione agli zoom 1 e 2;
- abilita l'animazione solo quando il profilo LOD lo consente;
- registra motivi espliciti di mancata visibilita', come snapshot assente, meteo inattivo,
  intensita' nulla o livello Z non visibile.

QA eseguita:

- controllo testuale su chiamate vietate superato;
- compilazione Roslyn isolata riuscita sui file toccati e nuovi;
- harness smoke previsto per validare pioggia attiva, LOD semplificato, animazione a zoom alto,
  meteo inattivo e meteo su livello Z non visibile.

Debiti residui:

- il meteo non e' ancora fuso nella render queue globale;
- il meteo non e' ancora disegnato da `ArcGraphSceneProbeRenderer`;
- non esiste ancora bridge produttivo dal sistema climatico reale;
- la scelta concreta di sprite, overlay, shader o particle-like rendering resta rinviata alle fasi renderer Unity successive.

Prossimo checkpoint:

```text
v0.37 - ArcGraph Debug/Overlay Migration
```

Branch previsto:

```text
ai-task/v0.37-arcgraph-debug-overlay-migration
```

Scope iniziale:

- audit degli overlay diagnostici e debug ancora presenti nel renderer MapGrid legacy;
- classificazione di quali overlay possono migrare in ArcGraph;
- priorita' a pointer cell coords, FOV heatmap, landmark overlay, DT overlay e summary cards;
- nessuna migrazione dei dev tools interattivi prima di separare chiaramente input/debug/rendering;
- nessuna sostituzione di MapGrid.

## Apertura v0.36a - Audit e contratto preparatorio

Branch:

```text
ai-task/v0.36a-arcgraph-environment-audit
```

Scope previsto:

- audit dei placeholder/layer ArcGraph ambientali gia' disponibili;
- verifica di quanto `v0.30h` ha preparato per Water, Vegetation, Light, Weather ed Effect;
- definizione input/output ammessi per ogni layer visuale;
- separazione tra simulazione ambientale e rendering;
- relazione con LOD zoom `v0.33f`;
- relazione con render queue actor/object `v0.34`;
- relazione con actor motion `v0.35`;
- nessuna implementazione produttiva prima del go.

Vietato in `v0.36a`:

- creare `GameObject`;
- creare `SpriteRenderer`, `MeshRenderer`, `ParticleSystem` o luci Unity;
- caricare asset con `Resources.Load`;
- modificare scene;
- simulare crescita piante, acqua, meteo, luce o incendi;
- sostituire MapGrid.

## Esito v0.36a - Audit layer ambientali e contratto visuale

Audit eseguito sui file:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphFuturePlaceholderLayers.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphFutureVisualSnapshots.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphLayerStack.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphBootstrapRuntime.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodModes.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodProfile.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphZoomLodPolicy.cs
```

Stato rilevato:

- esistono gia' placeholder per `Water`, `Vegetation`, `Light`, `Weather`, `Effect`;
- i placeholder sono registrabili solo con `RegisterFuturePlaceholderLayers()`;
- i placeholder non sono inclusi nei layer foundation di default;
- i layer conservano snapshot e marcano dirty dove necessario;
- il bootstrap puo' includerli solo se `IncludeFuturePlaceholderLayers` e' esplicitamente true;
- la policy LOD sa gia' distinguere vegetazione, effetti, weather overlay e animazioni sprite;
- la render queue attuale riguarda solo actor/object;
- non esiste ancora una queue ambientale;
- non esiste ancora un renderer Unity ambientale.

Contratto aggiunto:

```text
ArcGraphEnvironmentVisualLayerContract
├─ LayerId
├─ Scope
├─ SourceSystemKey
├─ RegisteredByDefault
├─ RequiresExternalSnapshots
├─ UsesDirtyCells
├─ AllowsArcGraphSpriteAnimation
├─ AllowsGlobalOverlay
├─ AllowsLocalTint
└─ AllowsUnityObjectCreation
```

Catalogo default:

```text
Vegetation -> CellCache    -> VegetationSystem -> animazione ArcGraph ammessa
Water      -> CellCache    -> WaterSystem      -> animazione ArcGraph ammessa
Light      -> CellCache    -> LightSystem      -> tinta/overlay ammessi, animazione sprite no
Effect     -> LocalEffect  -> EffectSystem     -> animazione ArcGraph ammessa
Weather    -> GlobalOverlay -> WeatherSystem   -> animazione ArcGraph ammessa
```

Decisione su animazione:

```text
Simulazione
produce stato oggettivo

ArcGraph
sceglie frame, posa, layer, LOD, tint e overlay

Unity
disegna sprite, mesh, materiali, shader, luci/overlay concreti
```

In questa fase ArcGraph non usa `Animator` Unity e non crea `ParticleSystem`.
L'animazione futura deve essere trattata come scelta frame/posa dentro ArcGraph,
poi consegnata a un renderer Unity separato.

Harness aggiunto:

- `ArcGraphEnvironmentVisualContractHarness`;
- verifica cinque contratti ambientali;
- verifica che tutti richiedano snapshot esterni;
- verifica che nessuno sia registrato di default;
- verifica che nessuno autorizzi creazione Unity;
- verifica che quattro layer possano usare animazione sprite ArcGraph.

Vincoli preservati:

- nessun sistema ambientale produttivo;
- nessun asset load;
- nessun `GameObject`;
- nessuna scena modificata;
- nessuna modifica a Core, Decision Layer, Job Layer o MapGrid;
- MapGrid resta renderer produttivo.

Prossimo step:

`v0.36.01 - Vegetation Renderer`

Lo step dovra' partire da vegetazione/erba come layer visuale, ma ancora con scope
controllato: snapshot, LOD, contratto animazione frame-based e nessuna biosfera
produttiva.

Branch aperto:

```text
ai-task/v0.36.01-arcgraph-vegetation-renderer
```

Gate:

Attendere go operatore prima di modifiche operative sul codice della vegetazione.

## Esito v0.36.01 - Vegetation Renderer passivo

La `v0.36.01` ha introdotto il primo builder visuale ambientale specifico:
vegetazione/erba/piante come item renderizzabili passivi.

Implementato:

- `ArcGraphVegetationLayer.CopySnapshotsTo(...)`;
- `ArcGraphRenderItemKind.Vegetation`;
- `ArcGraphVegetationRenderItem`;
- `ArcGraphVegetationRenderQueueDiagnostics`;
- `ArcGraphVegetationRenderQueueBuilder`;
- `ArcGraphVegetationRenderQueueHarness`.

Flusso:

```text
ArcGraphVegetationVisualSnapshot
-> ArcGraphVegetationLayer
-> ArcGraphVegetationRenderQueueBuilder
-> ArcGraphVegetationRenderItem
```

Regole:

- il builder legge solo il layer vegetazione;
- il builder applica `ArcGraphZoomLodProfile`;
- zoom 1 produce rappresentazione aggregata d'area;
- zoom 2 produce sprite statici semplificati;
- zoom 3/4 possono produrre item individuali animabili se il LOD lo consente;
- la chiave sprite e' solo una stringa derivata, non un asset caricato;
- nessuna crescita pianta viene simulata;
- nessuna seed bank viene letta;
- nessuna fertilita', umidita', stagione o acqua vicina viene calcolata;
- nessun renderer Unity viene creato.

Decisione di scope:

La vegetazione non viene ancora fusa nella queue globale actor/object.

Motivo:

- la queue actor/object e' stata chiusa in `v0.34`;
- inserire subito vegetazione nella queue globale obbligherebbe a decidere sorting
  completo tra terrain, water, vegetation, object, actor, effect e light;
- questa decisione appartiene a un checkpoint successivo di composizione ambiente.

Harness:

`ArcGraphVegetationRenderQueueHarness.RunDefaultSmoke()` verifica:

- due snapshot vegetazione validi;
- uno snapshot nascosto per species key mancante;
- zoom 1 con item aggregati;
- zoom 4 con item animabili;
- nessuna scena, nessun asset, nessun sistema biosfera.

Vincoli preservati:

- nessuna biosfera produttiva;
- nessun asset load;
- nessun `GameObject`;
- nessun `SpriteRenderer`;
- nessun `ParticleSystem`;
- nessuna modifica a Core, Decision Layer, Job Layer o MapGrid;
- MapGrid resta renderer produttivo.

Prossimo step:

`v0.36.02 - Water Renderer`

Il prossimo step dovra' seguire lo stesso schema: snapshot acqua gia' prodotti da
sistemi esterni, item renderizzabili passivi, LOD, nessun flusso idraulico
produttivo.

Branch aperto:

```text
ai-task/v0.36.02-arcgraph-water-renderer
```

Gate:

Attendere go operatore prima di modifiche operative sul codice acqua.

## Esito v0.36.02 - Water Renderer passivo

La `v0.36.02` ha introdotto il builder visuale ambientale per l'acqua:
profondita', sprite key, LOD e animazione frame-based come dati passivi.

Implementato:

- `ArcGraphWaterLayer.CopySnapshotsTo(...)`;
- `ArcGraphRenderItemKind.Water`;
- `ArcGraphWaterRenderItem`;
- `ArcGraphWaterRenderQueueDiagnostics`;
- `ArcGraphWaterRenderQueueBuilder`;
- `ArcGraphWaterRenderQueueHarness`.

Flusso:

```text
ArcGraphWaterVisualSnapshot
-> ArcGraphWaterLayer
-> ArcGraphWaterRenderQueueBuilder
-> ArcGraphWaterRenderItem
```

Regole:

- il builder legge solo il layer acqua;
- il builder applica `ArcGraphZoomLodProfile`;
- `DepthLevel <= 0` produce item nascosto, se gli item nascosti sono richiesti;
- la chiave sprite e' solo una stringa derivata o ricevuta dallo snapshot;
- zoom con rappresentazione semplificata usa sprite key `ArcGraph/Water/Simple/depth_N`;
- zoom con rappresentazione dettagliata usa sprite key `ArcGraph/Water/depth_N`;
- l'animazione e' ammessa solo se contratto acqua, LOD e snapshot la consentono insieme;
- nessun flusso idraulico viene simulato;
- nessuna pressione acqua viene calcolata;
- nessuna sorgente, fiume, lago o cascata viene generata;
- nessun pathfinding viene modificato;
- nessun renderer Unity viene creato.

Decisione di scope:

L'acqua non viene ancora fusa nella queue globale actor/object.

Motivo:

- il sorting definitivo tra terrain, water, vegetation, object, actor, effect e light
  non e' ancora stato deciso;
- acqua e vegetazione restano per ora queue ambientali specializzate;
- la composizione globale dell'ambiente andra' affrontata dopo i renderer
  ambientali minimi.

Harness:

`ArcGraphWaterRenderQueueHarness.RunDefaultSmoke()` verifica:

- tre snapshot acqua;
- due celle visibili con profondita' positiva;
- una cella nascosta con profondita' zero;
- zoom 1 con rappresentazione semplificata e nessuna animazione sprite;
- zoom 4 con animazione ammessa su acqua animabile;
- profondita' massima tracciata in diagnostica;
- nessuna scena, nessun asset, nessun sistema acqua produttivo.

Vincoli preservati:

- nessuna simulazione acqua produttiva;
- nessun asset load;
- nessun `GameObject`;
- nessun `SpriteRenderer`;
- nessun `ParticleSystem`;
- nessuna modifica a Core, Decision Layer, Job Layer o MapGrid;
- MapGrid resta renderer produttivo.

Prossimo step:

`v0.36.03 - Light Renderer`

Il prossimo step dovra' seguire lo stesso schema: snapshot luce gia' prodotti da
sistemi esterni, item o overlay passivi per giorno/notte, tinta globale, buio
stanza e luci locali, senza creare luci Unity e senza simulare propagazione luce.

Branch aperto:

```text
ai-task/v0.36.03-arcgraph-light-renderer
```

Gate:

Attendere go operatore prima di modifiche operative sul codice luce.

## Apertura v0.36.03 - Light Renderer preparatorio

Branch:

```text
ai-task/v0.36.03-arcgraph-light-renderer
```

Scope previsto:

- audit del placeholder `ArcGraphLightLayer` e dello snapshot luce gia' presente;
- definizione item/overlay passivi per tinta globale e luce locale;
- gestione preparatoria di giorno/notte come dato visuale ricevuto dall'esterno;
- gestione preparatoria di buio stanza come snapshot/overlay, non come calcolo stanza;
- relazione con LOD zoom `v0.33f`;
- nessuna luce Unity creata;
- nessuna simulazione produttiva di propagazione luce;
- nessuna modifica scena.

Gate:

Attendere go operatore prima di modifiche operative sul codice luce.

## Esito v0.36.03 - Light Renderer passivo

La `v0.36.03` ha introdotto il builder visuale ambientale per la luce:
tinta locale, buio cella, sorgenti locali e predisposizione a overlay globale
come dati passivi.

Implementato:

- `ArcGraphLightLayer.CopySnapshotsTo(...)`;
- `ArcGraphRenderItemKind.Light`;
- `ArcGraphLightRenderItem`;
- `ArcGraphLightRenderQueueDiagnostics`;
- `ArcGraphLightRenderQueueBuilder`;
- `ArcGraphLightRenderQueueHarness`.

Flusso:

```text
ArcGraphLightVisualSnapshot
-> ArcGraphLightLayer
-> ArcGraphLightRenderQueueBuilder
-> ArcGraphLightRenderItem
```

Regole:

- il builder legge solo il layer luce;
- il builder applica `ArcGraphZoomLodProfile`;
- una cella neutra, senza tinta e senza sorgente locale, viene nascosta;
- una cella scura produce una tinta `dark`;
- una cella con sorgente locale produce una tinta `local`;
- la chiave tinta e' solo una stringa derivata o ricevuta dallo snapshot;
- la luce non usa animazione sprite;
- il contratto luce conserva i flag `AllowsGlobalOverlay` e `AllowsLocalTint`;
- nessuna propagazione luminosa viene simulata;
- nessuna occlusione da muri o stanze viene calcolata;
- nessuna luce Unity viene creata;
- nessun renderer Unity viene creato.

Decisione di scope:

La luce non viene ancora fusa nella queue globale actor/object e non viene ancora
combinata con acqua e vegetazione in una scena ArcGraph produttiva.

Motivo:

- il sorting definitivo tra terrain, water, vegetation, object, actor, effect e light
  non e' ancora stato deciso;
- la luce e' un overlay/tint compositivo e richiede un probe visivo prima di
  introdurre effetti e meteo;
- evitare accumulo di troppi moduli non verificati visivamente.

Harness:

`ArcGraphLightRenderQueueHarness.RunDefaultSmoke()` verifica:

- tre snapshot luce;
- una cella scura visibile;
- una sorgente locale visibile;
- una cella neutra nascosta;
- uso del LOD semplificato a zoom 1;
- conteggio sorgenti locali e celle scure;
- nessuna scena, nessun asset, nessun sistema luce produttivo.

Vincoli preservati:

- nessuna simulazione luce produttiva;
- nessun asset load;
- nessun `GameObject`;
- nessun `SpriteRenderer`;
- nessun `ParticleSystem`;
- nessuna luce Unity;
- nessuna modifica a Core, Decision Layer, Job Layer o MapGrid;
- MapGrid resta renderer produttivo.

Prossimo step consigliato:

`v0.36.03v - ArcGraph Visual Probe`

Questo micro-step deve introdurre un primo test visivo controllato prima di
procedere con fiamme/effetti e meteo.

Obiettivo del probe:

- accendere una scena/probe debug minima, non produttiva;
- usare snapshot controllati o finti;
- visualizzare terrain + actor/object + vegetation + water + light;
- verificare sorting visuale, LOD, tinta luce e sovrapposizione layer;
- non sostituire MapGrid;
- non introdurre sistemi ambientali produttivi.

Branch aperto:

```text
ai-task/v0.36.03v-arcgraph-visual-probe
```

Gate:

Attendere go operatore prima di modifiche operative sul probe visivo.

## Apertura v0.36.03v - ArcGraph Visual Probe

Branch:

```text
ai-task/v0.36.03v-arcgraph-visual-probe
```

Scope previsto:

- audit dei punti di aggancio debug/test per una visualizzazione ArcGraph minima;
- definizione di un probe non produttivo e non sostitutivo di MapGrid;
- uso di snapshot controllati o finti per terrain, actor/object, vegetation, water e light;
- verifica visuale di sorting layer, LOD e tinta luce;
- nessuna introduzione di biosfera, acqua, luce, meteo o effetti produttivi;
- nessuna modifica scena/prefab prima di decisione esplicita.

Gate:

Attendere go operatore prima di modifiche operative sul probe visivo.

## Esito v0.36.03v - ArcGraph Visual Probe data-only

La `v0.36.03v` ha introdotto il primo frame dati unitario per un probe visuale
ArcGraph, senza ancora disegnare in Unity.

Implementato:

- `ArcGraphVisualProbeDiagnostics`;
- `ArcGraphVisualProbeFrame`;
- `ArcGraphVisualProbeBuilder`;
- `ArcGraphVisualProbeHarness`.

Flusso:

```text
ArcGraphTerrainLayer
+ ArcGraphActorLayer
+ ArcGraphObjectLayer
+ ArcGraphVegetationLayer
+ ArcGraphWaterLayer
+ ArcGraphLightLayer
-> ArcGraphVisualProbeBuilder
-> ArcGraphVisualProbeFrame
```

Il frame contiene:

- chunk terrain gia' prodotti dal terrain mesh builder;
- queue actor/object gia' ordinata;
- item vegetazione visibili;
- item acqua visibili;
- item luce visibili;
- diagnostica aggregata;
- diagnostica del gate comparativo ArcGraph/MapGrid.

Harness:

`ArcGraphVisualProbeHarness.RunDefaultSmoke()` costruisce una mini scena dati
controllata:

- mappa 4x4;
- 16 celle terrain;
- 1 actor;
- 1 oggetto;
- 1 vegetazione;
- 1 acqua;
- 2 overlay luce;
- gate scena temporanea dichiarato come ammesso.

Vincoli preservati:

- nessuna scena modificata;
- nessun prefab modificato;
- nessun asset load;
- nessun disegno Unity ancora attivo;
- nessuna sostituzione di MapGrid;
- nessuna simulazione ambientale produttiva;
- nessuna modifica a Core, Decision Layer, Job Layer o MapGrid.

Per iniziare il primo test visivo reale manca ancora il renderer debug concreto.

Prossimo step:

`v0.36.03v.01 - ArcGraph Scene Probe Renderer`

Lo step dovra' creare un disegnatore debug temporaneo che consuma
`ArcGraphVisualProbeFrame` e lo mostra in Unity con risorse provvisorie, senza
diventare renderer produttivo e senza pensionare MapGrid.

Branch aperto:

```text
ai-task/v0.36.03v.01-arcgraph-scene-probe-renderer
```

Gate:

Attendere go operatore prima di modifiche operative sul renderer debug.

## Apertura v0.36.03v.01 - ArcGraph Scene Probe Renderer

Branch:

```text
ai-task/v0.36.03v.01-arcgraph-scene-probe-renderer
```

Scope previsto:

- audit del modo piu' sicuro per disegnare un `ArcGraphVisualProbeFrame` in Unity;
- scelta esplicita di risorse provvisorie per terrain, actor/object, vegetazione, acqua e luce;
- definizione del componente debug temporaneo;
- nessuna sostituzione di MapGrid;
- nessun aggancio permanente alla scena;
- nessuna modifica a prefab o scene senza decisione esplicita.

Gate:

Attendere go operatore prima di modifiche operative sul renderer debug.

## Esito v0.36.03v.01 - ArcGraph Scene Probe Renderer

La `v0.36.03v.01` ha introdotto il primo renderer debug temporaneo capace di
disegnare un `ArcGraphVisualProbeFrame` dentro Unity.

Implementato:

- `ArcGraphSceneProbeRenderer`;
- `ArcGraphVisualProbeHarness.CreateDefaultProbeFrame(...)`.

Comportamento:

- il componente e' un `MonoBehaviour` debug;
- non parte automaticamente di default;
- crea un root temporaneo chiamato `ArcGraphSceneProbeRoot`;
- usa sprite runtime 1x1 colorati;
- non carica asset con `Resources.Load`;
- non richiede sprite o materiali esterni per il probe minimo;
- puo' posizionare automaticamente il probe vicino alla camera assegnata;
- espone `Render Default Probe` da context menu;
- espone `Clear Probe` da context menu;
- distrugge solo gli oggetti temporanei creati dal probe.

Layer visualizzati:

```text
terrain    -> grigio
water      -> blu
vegetation -> verde
object     -> arancione
actor      -> magenta
light      -> overlay giallo/nero/blu scuro
```

Vincoli preservati:

- nessuna scena modificata;
- nessun prefab modificato;
- nessun asset aggiunto;
- nessun file `.meta` modificato;
- nessuna sostituzione di MapGrid;
- nessun accesso a `SimulationHost`;
- nessuna lettura diretta del `World`;
- nessuna simulazione ambientale produttiva.

Eccezione controllata:

Questo step usa intenzionalmente `new GameObject` e `SpriteRenderer`, ma solo
dentro il componente debug temporaneo. L'uso e' confinato al probe e non va
confuso con il renderer produttivo ArcGraph.

Prossimo step:

`v0.36.03v.02 - ArcGraph First Visual Test QA`

Lo step successivo non deve aggiungere nuovi moduli. Deve servire a eseguire il
primo test visivo, raccogliere difetti e decidere eventuali micro-fix prima di
procedere con effetti e meteo.

Branch aperto:

```text
ai-task/v0.36.03v.02-arcgraph-first-visual-test-qa
```

Gate:

Attendere esito del test operatore prima di nuove modifiche.

## Apertura v0.36.03v.02 - ArcGraph First Visual Test QA

Branch:

```text
ai-task/v0.36.03v.02-arcgraph-first-visual-test-qa
```

Scope previsto:

- esecuzione del primo test visivo tramite `ArcGraphSceneProbeRenderer`;
- raccolta difetti osservati a schermo;
- verifica di posizione, scala, sorting, colori e pulizia root temporaneo;
- nessuna aggiunta di nuovi layer ambientali;
- nessuna modifica a scene/prefab prima dell'esito del test.

Gate:

Attendere esito del test operatore prima di nuove modifiche.

## Esito v0.36.03v.02 - ArcGraph First Visual Test QA

Il primo test visivo manuale del probe ArcGraph e' stato eseguito in Unity su
`Scene_MapGrid`.

Risultato osservato:

- `ArcGraphSceneProbeRenderer` e' stato aggiunto a un GameObject temporaneo;
- `MainCamera` e' stata assegnata correttamente al campo `Scene Camera`;
- il context menu `ArcGraph/Render Default Probe` ha generato il probe;
- il probe e' apparso nella scena sopra la mappa legacy senza sostituire MapGrid;
- terrain, water, vegetation, object, actor e light sono risultati visibili;
- il log runtime ha confermato il render del probe frame;
- non sono state richieste modifiche a scene, prefab o asset per completare il test.

Valutazione:

```text
v0.36.03v.02 superata come QA visuale minima.
```

Difetti rilevati:

- nessun difetto bloccante rilevato nel test manuale;
- la leggibilita' resta volutamente provvisoria, perche' il probe usa colori runtime
  e sprite 1x1 generati;
- il probe resta strumento debug temporaneo, non renderer produttivo.

Prossimo step coerente con roadmap:

`v0.36.04 - Effect Renderer`

Lo step deve introdurre effetti locali passivi come fiamme, fumo, scintille o altri
segnali visuali, mantenendo lo stesso modello gia' usato per vegetazione, acqua e luce:
snapshot esterni, item renderizzabili, LOD, diagnostica e harness, senza simulare
incendi o propagazione effetti.

Branch aperto:

```text
ai-task/v0.36.04-arcgraph-effect-renderer
```

Gate:

Procedere prima con audit dei contratti gia' presenti per `Effect`, poi proporre la
patch minima solo se non emergono scelte progettuali bloccanti.

## Esito v0.36.04 - Effect Renderer passivo

La `v0.36.04` ha introdotto il builder visuale ambientale per effetti locali:
fiamme, fumo, scintille e segnali locali come item renderizzabili passivi.

Implementato:

- `ArcGraphEffectLayer.CopySnapshotsTo(...)`;
- `ArcGraphRenderItemKind.Effect`;
- `ArcGraphEffectRenderItem`;
- `ArcGraphEffectRenderQueueDiagnostics`;
- `ArcGraphEffectRenderQueueBuilder`;
- `ArcGraphEffectRenderQueueHarness`.

Flusso:

```text
ArcGraphEffectVisualSnapshot
-> ArcGraphEffectLayer
-> ArcGraphEffectRenderQueueBuilder
-> ArcGraphEffectRenderItem
```

Regole:

- il builder legge solo il layer effetti;
- il builder applica `ArcGraphZoomLodProfile`;
- `EffectId <= 0` produce item nascosto;
- `EffectKey` vuoto produce item nascosto;
- `Intensity01 <= 0` produce item nascosto;
- zoom 1 usa `StaticSignalOnly`;
- zoom 2 usa `SimplifiedStaticEffect`;
- zoom 3 usa `AnimatedMajorEffects`, ma abbassa gli effetti deboli a statici;
- zoom 4 usa `FullLocalEffects`;
- la chiave sprite e' solo una stringa derivata;
- l'animazione e' ammessa solo se contratto, LOD e intensita' la consentono;
- nessun incendio viene propagato;
- nessun fumo viene simulato;
- nessun danno viene calcolato;
- nessun `ParticleSystem` viene creato;
- nessun renderer Unity viene creato.

Decisione di scope:

Gli effetti non vengono ancora fusi nella queue globale actor/object e non vengono
ancora disegnati dal scene probe.

Motivo:

- il sorting definitivo tra terrain, water, vegetation, object, actor, effect e light
  non e' ancora stato deciso;
- il probe visivo appena validato resta il controllo minimo dei layer gia' provati;
- gli effetti sono ora pronti come queue passiva, ma la composizione globale resta
  un passaggio successivo.

Harness:

`ArcGraphEffectRenderQueueHarness.RunDefaultSmoke()` verifica:

- quattro snapshot effetto;
- due effetti visibili;
- due effetti nascosti per dati non validi;
- zoom 1 con segnali statici e nessuna animazione;
- zoom 4 con effetti animabili;
- nessuna scena, nessun asset, nessun sistema incendi produttivo.

QA:

- compilazione Roslyn isolata dei file toccati e nuovi riuscita;
- `dotnet build --no-restore` del progetto Unity non e' stato usato come verifica
  valida perche' manca `Temp/obj/Assembly-CSharp/project.assets.json`;
- non e' stato eseguito restore per non scrivere in `Temp`;
- la compilazione Roslyn isolata usa output in `%TEMP%` e reference Unity
  `netstandard 2.1`.

Vincoli preservati:

- nessuna simulazione effetti produttiva;
- nessun asset load;
- nessun `GameObject`;
- nessun `SpriteRenderer`;
- nessun `ParticleSystem`;
- nessuna luce Unity;
- nessuna modifica a scene o prefab;
- nessuna modifica a Core, Decision Layer, Job Layer o MapGrid;
- MapGrid resta renderer produttivo.

Prossimo step:

`v0.36.05 - Weather Renderer`

Lo step dovra' seguire lo stesso schema: snapshot meteo gia' prodotti da sistemi
esterni, overlay atmosferico passivo, LOD, diagnostica e harness, senza simulare
clima, temperatura, umidita' o precipitazioni produttive.

Branch aperto:

```text
ai-task/v0.36.05-arcgraph-weather-renderer
```

Gate:

Attendere go operatore prima di modifiche operative sul codice meteo.

---

#### v0.37 - ArcGraph Debug/Overlay Migration

## Stato
IN CORSO

## Obiettivo

Migrare progressivamente gli overlay diagnostici fuori dal monolite `MapGridWorldView`.

Ordine valutato prima dell'audit:

1. pointer cell coords;
2. FOV heatmap;
3. landmark overlay;
4. DT overlay;
5. summary cards;
6. dev tools solo dopo separazione dai renderer.

## Esito v0.37a - Audit overlay/debug MapGrid

Audit eseguito in sola lettura sui file MapGrid e sui contratti Core collegati.

File principali ispezionati:

```text
Assets/Scripts/Views/MapGrid/Runtime/MapGridWorldView.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridFovHeatmapOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridLandmarkOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridLandmarkLabelOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridDtValueOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridPointerCoordsOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridEntitySummaryOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridRuntimeControlTopBar.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridRuntimeDevToolsOverlay.cs
Assets/Scripts/Core/Telemetry/DebugFovTelemetry.cs
Assets/Scripts/Core/World/LandmarkDebugTypes.cs
Assets/Scripts/Core/World/World.cs
```

Stato rilevato:

- `MapGridWorldView` oggi coordina binding NPC/oggetti, overlay diagnostici, HUD, top bar runtime,
  selezione debug e click-to-move;
- gli overlay non sono tutti della stessa natura: alcuni sono overlay di mappa, altri sono HUD,
  altri sono pannelli UI interattivi;
- `ArcGraphDebugLayer` esiste gia', ma registra solo contatori dirty e non contiene ancora contratti
  per overlay reali;
- i contratti Core per landmark/GVD-DIN sono gia' abbastanza separati dalla view:
  `LandmarkOverlayNode`, `LandmarkOverlayEdge`, `GvdDinOverlaySnapshot`;
- il FOV ha due fonti distinte:
  `DebugFovTelemetry` come buffer storico read-only e `MapGridFovHeatmapOverlay.RenderCurrentCone(...)`
  come visualizzazione immediata del cono;
- pointer coords e runtime cost observer sono HUD screen-space, non celle renderizzate nella mappa;
- summary cards sono UI diagnostica complessa, con drag, linee canvas, testo aggregato e pannello
  explainability;
- DevTools e TopBar sono strumenti operativi: emettono comandi, controllano pausa/step e non devono
  entrare nel DebugLayer passivo di ArcGraph.

Classificazione dopo audit:

```text
MIGRAZIONE ALTA PRIORITA'
- FOV current cone / FOV heatmap: overlay cell-based, buon candidato per snapshot debug ArcGraph.
- Landmark nodes/edges/path: overlay cell/edge-based, gia' supportato da DTO Core.
- DT/GVD-DIN heatmap: overlay cell-based, gia' supportato da GvdDinOverlaySnapshot.

MIGRAZIONE MEDIA PRIORITA'
- Pointer cell coords: utile, ma e' HUD screen-space; serve prima un canale Debug/HUD separato.
- Landmark labels: sono UI label screen-space sopra marker, migrabili dopo i nodi/edge.

MIGRAZIONE BASSA PRIORITA' / DA RINVIARE
- Summary cards: troppo ampie e interattive per essere il primo step.
- RuntimeControlTopBar: controllo operativo, non overlay di rendering.
- RuntimeDevToolsOverlay: tool operativo che genera comandi; non deve entrare in ArcGraph finche'
  input, comandi e debug renderer non sono separati.
```

Decisione tecnica provvisoria:

La `v0.37` non deve iniziare disegnando UI o creando oggetti Unity.
Il primo passo corretto e' introdurre contratti dati passivi per overlay debug ArcGraph:

- item cell-based per FOV/DT;
- item node/edge-based per landmark/path;
- diagnostica della queue debug;
- builder passivo che legge snapshot gia' prodotti o DTO espliciti;
- nessun `GameObject`, `SpriteRenderer`, `LineRenderer`, `Canvas`, `Button`, `Mouse.current`,
  `Keyboard.current`, `SimulationHost` o comando core.

Prossimo micro-step:

```text
v0.37b - ArcGraph Debug Overlay Data Contracts
```

Scope previsto:

- definire contratti value-only per celle, nodi, edge e label debug;
- separare overlay di mappa da HUD screen-space;
- preparare una queue debug passiva ordinabile;
- non collegare ancora MapGridWorldView;
- non migrare DevTools, TopBar o summary cards.

## Esito v0.37b - ArcGraph Debug Overlay Data Contracts

La `v0.37b` introduce il vocabolario dati passivo per gli overlay debug ArcGraph.

File/runtime aggiunti o aggiornati:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphRenderItemKind.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlayKind.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlaySpace.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugCellOverlayItem.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugNodeOverlayItem.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugEdgeOverlayItem.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugLabelOverlayItem.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlayQueueDiagnostics.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlayQueue.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlayContractHarness.cs
```

Elementi introdotti:

- aggiunto `ArcGraphRenderItemKind.Debug`;
- aggiunto `ArcGraphDebugOverlayKind`;
- aggiunto `ArcGraphDebugOverlaySpace`;
- aggiunto item cell-based per FOV, DT e GVD raw;
- aggiunto item node-based per marker landmark/GVD;
- aggiunto item edge-based per edge landmark, path, route e GVD;
- aggiunto item label/HUD per label screen-space e pannelli informativi futuri;
- aggiunta queue debug passiva separata dalla queue produttiva actor/object;
- aggiunta diagnostica della queue debug;
- aggiunto harness smoke dei contratti.

Separazioni fissate:

```text
MapCell     -> celle FOV, celle DT, celle GVD raw
MapNode     -> landmark world/known/route/GVD
MapEdge     -> landmark edge, route edge, path edge
ScreenLabel -> label ancorate alla mappa
ScreenHud   -> pointer coords, runtime cost HUD, futuri pannelli debug
```

Vincoli rispettati:

- nessun `GameObject`;
- nessun `SpriteRenderer`;
- nessun `LineRenderer`;
- nessun `Canvas`;
- nessun `Resources.Load`;
- nessuna lettura `World`;
- nessuna dipendenza da `MapGridWorldView`;
- nessun input mouse/tastiera;
- nessuna emissione di comandi.

QA eseguita:

- controllo statico stretto su chiamate vietate superato;
- compilazione Roslyn isolata riuscita sui file nuovi e sulle dipendenze ArcGraph minime;
- warning Roslyn attesi dovuti al confronto tra sorgenti locali e `Assembly-CSharp.dll`.

Debiti residui:

- i contratti non sono ancora alimentati da `DebugFovTelemetry`, `LandmarkOverlayNode`,
  `LandmarkOverlayEdge` o `GvdDinOverlaySnapshot`;
- non esiste ancora renderer Unity per questi item;
- non esiste ancora bridge da MapGridWorldView;
- summary cards, top bar e DevTools restano fuori scope.

Prossimo micro-step:

```text
v0.37c - ArcGraph Debug Overlay Queue Builder
```

Scope previsto:

- costruire un builder passivo che trasformi input DTO/snapshot in `ArcGraphDebugOverlayQueue`;
- accettare dati gia' forniti dal chiamante, senza leggere direttamente il `World`;
- supportare FOV cell-based, landmark node/edge e DT/GVD-DIN;
- mantenere separati label/HUD da overlay di mappa;
- aggiungere harness smoke;
- non collegare ancora `MapGridWorldView`;
- non creare renderer Unity.

## Esito v0.37c - ArcGraph Debug Overlay Queue Builder

La `v0.37c` introduce il primo builder passivo per alimentare la queue debug ArcGraph.

Allineamento Git preliminare:

- `main`, `ai/codex-main` e `ai-task/v0.37c-arcgraph-debug-overlay-builder` sono stati
  allineati allo stesso commit di integrazione;
- la storia di `main` e la storia ArcGraph sono state conservate tramite merge commit;
- non sono stati usati force-push;
- il controllo finale ha mostrato differenze `0/0` tra `main`, `ai/codex-main` e i rispettivi remoti.

File/runtime aggiunti:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlaySnapshot.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlayQueueBuilder.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlayQueueBuilderHarness.cs
```

Elementi introdotti:

- aggiunto snapshot DTO cell-based per FOV, DT e GVD raw;
- aggiunto snapshot DTO node-based per landmark world/known/route/GVD;
- aggiunto snapshot DTO edge-based per landmark edge, path, route e GVD;
- aggiunto snapshot DTO label/HUD;
- aggiunto contenitore `ArcGraphDebugOverlaySnapshot` separato dalla queue renderizzabile;
- aggiunto `ArcGraphDebugOverlayQueueBuilder`;
- aggiunto harness smoke `ArcGraphDebugOverlayQueueBuilderHarness`.

Flusso dati fissato:

```text
producer debug esterno
-> ArcGraphDebugOverlaySnapshot
-> ArcGraphDebugOverlayQueueBuilder
-> ArcGraphDebugOverlayQueue
-> renderer Unity futuro
```

Comportamento del builder:

- pulisce la queue target se richiesto;
- accetta solo snapshot gia' preparati dal chiamante;
- normalizza item visibili e hidden;
- produce sort key deterministiche;
- ordina separatamente celle, nodi, edge e label;
- separa label/HUD dagli overlay di mappa;
- puo' includere item hidden per QA tramite `includeHiddenItems`;
- produce diagnostica tramite `ArcGraphDebugOverlayQueueDiagnostics`.

Hidden state gestiti:

- snapshot disabilitato -> `DisabledBySnapshot`;
- kind debug non valido -> `InvalidDebugKind`;
- edge con estremi identici -> `DegenerateEdge`;
- label vuota -> `MissingLabelText`.

Vincoli rispettati:

- nessuna lettura `World`;
- nessuna dipendenza da `MapGridWorldView`;
- nessun `GameObject`;
- nessun `SpriteRenderer`;
- nessun `LineRenderer`;
- nessun `Canvas`;
- nessun `Resources.Load`;
- nessun input mouse/tastiera;
- nessuna emissione di comandi;
- nessuna modifica a scene, prefab o asset.

QA eseguita:

- `git diff --check` superato;
- controllo statico sulle chiamate vietate eseguito: le parole vietate compaiono solo in commenti descrittivi dei vincoli;
- compilazione Roslyn isolata riuscita sui file nuovi e sulle dipendenze ArcGraph minime.

Debiti residui:

- il builder non e' ancora alimentato da `DebugFovTelemetry`, `LandmarkOverlayNode`,
  `LandmarkOverlayEdge` o `GvdDinOverlaySnapshot`;
- non esiste ancora renderer Unity debug per disegnare cell, node, edge e label;
- non esiste ancora bridge da `MapGridWorldView`;
- summary cards, top bar e DevTools restano fuori scope.

Prossimo micro-step consigliato:

```text
v0.37d - ArcGraph Debug Overlay Producer Bridge Audit
```

Scope consigliato:

- audit mirato dei producer reali FOV, landmark e DT/GVD-DIN;
- decidere quali DTO possono alimentare direttamente `ArcGraphDebugOverlaySnapshot`;
- non creare ancora renderer Unity;
- non migrare DevTools, TopBar o summary cards.

## Esito v0.37d - ArcGraph Debug Overlay Producer Bridge Audit

La `v0.37d` esegue l'audit dei producer reali che oggi alimentano gli overlay debug
di `MapGridWorldView`, per capire quali dati possono essere convertiti in
`ArcGraphDebugOverlaySnapshot` senza introdurre un renderer Unity e senza creare
un secondo manager diagnostico.

File ispezionati:

```text
Assets/Scripts/Core/Telemetry/DebugFovTelemetry.cs
Assets/Scripts/Core/World/LandmarkDebugTypes.cs
Assets/Scripts/Core/World/LandmarkRegistry.cs
Assets/Scripts/Core/World/World.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridWorldView.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridFovHeatmapOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridLandmarkOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridLandmarkLabelOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridDtValueOverlay.cs
Assets/Scripts/Views/MapGrid/Runtime/MapGridPointerCoordsOverlay.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphWorldAdapter.cs
```

Classificazione delle sorgenti:

```text
PRONTE PER BRIDGE PASSIVO
- LandmarkOverlayNode
- LandmarkOverlayEdge
- GvdDinOverlaySnapshot.DtCells
- GvdDinOverlaySnapshot.GvdRawCells
- GvdDinOverlaySnapshot.GvdNodes
- GvdDinOverlaySnapshot.GvdEdges

PARZIALMENTE PRONTE
- DebugFovTelemetry heatmap storica

NON ANCORA PRONTE SENZA ESTRAZIONE PRODUCER
- FOV current cone / watched margin
- Pointer cell coords HUD
- Runtime cost HUD
- Landmark labels screen-space
- DT numeric labels screen-space
```

Dettaglio FOV:

- `DebugFovTelemetry` e' un buffer read-only gia' separato dal renderer;
- la heatmap storica puo' essere tradotta in `ArcGraphDebugCellOverlaySnapshot`
  scansionando il read buffer dell'NPC attivo;
- il cono FOV corrente non e' ancora un producer separato;
- `MapGridFovHeatmapOverlay.RenderCurrentCone(...)` calcola direttamente le celle
  osservate e watched usando `World`, `GridPos`, facing, range, cone slope e LOS;
- questo metodo oggi e' contemporaneamente producer e renderer;
- portarlo in ArcGraph senza estrazione creerebbe un bridge troppo accoppiato al
  `World` e ripeterebbe il problema del renderer legacy.

Dettaglio landmark:

- `World.GetNpcLandmarkOverlayData(...)` produce gia' liste view-only per:
  - grafo mondo;
  - grafo conosciuto dall'NPC;
  - route macro;
  - path runtime cella-per-cella;
  - direct path;
  - jump/local search path;
  - complex edge fisicamente percorsi;
- i dati sono gia' nel formato `LandmarkOverlayNode` e `LandmarkOverlayEdge`;
- questi DTO sono candidati forti per un bridge ArcGraph immediato;
- il bridge dovra' solo assegnare il corretto `ArcGraphDebugOverlayKind`.

Dettaglio GVD-DIN:

- `GvdDinOverlaySnapshot` e' gia' un contenitore riusabile e passivo;
- contiene DT heatmap, GVD raw, GVD nodes e GVD edges;
- `World.GetGvdDinOverlayData(...)` pulisce e ripopola lo snapshot;
- la sorgente effettiva e' `LandmarkRegistry.FillGvdDinOverlayData(...)`;
- il bridge puo' convertire:
  - `DtCells` -> `DtHeatCell`;
  - `GvdRawCells` -> `GvdRawCell`;
  - `GvdNodes` -> `LandmarkGvdNode`;
  - `GvdEdges` -> `LandmarkGvdEdge`.

Dettaglio HUD / label:

- pointer coords e runtime cost sono ancora costruiti in
  `MapGridPointerCoordsOverlay`;
- la conversione mouse/camera/cella avviene in `MapGridWorldView`;
- landmark labels e DT numeric labels sono screen-space e dipendono da camera,
  canvas, font e frustum check;
- questi elementi possono entrare in `ArcGraphDebugLabelOverlaySnapshot`, ma solo
  dopo aver separato producer testuale e renderer UI;
- non devono essere il primo bridge operativo.

Forma consigliata del prossimo bridge:

```text
ArcGraphDebugOverlayProducerBridge
├─ FillLandmarkDebugSnapshot(...)
├─ FillGvdDinDebugSnapshot(...)
├─ FillHistoricalFovHeatSnapshot(...)
└─ non implementa FOV current cone finche' non esiste producer separato
```

Vincoli per il bridge futuro:

- leggere dati gia' disponibili tramite contratti debug esistenti;
- non creare `GameObject`;
- non creare `SpriteRenderer`;
- non creare `LineRenderer`;
- non creare `Canvas`;
- non leggere input mouse/tastiera;
- non emettere comandi;
- non chiamare `SimulationHost`;
- non duplicare logica percettiva produttiva;
- non spostare in ArcGraph il calcolo del cono FOV corrente finche' resta dentro
  `MapGridFovHeatmapOverlay`.

Decisione tecnica consigliata:

La `v0.37e` dovrebbe implementare solo il bridge passivo per landmark e GVD-DIN,
piu' eventualmente la heatmap storica `DebugFovTelemetry`.

Il FOV current cone va rinviato o preceduto da un micro-step dedicato di estrazione
producer:

```text
Fov current cone producer
-> lista celle observed/watched
-> ArcGraphDebugOverlaySnapshot
```

Rischi principali:

- se il bridge legge direttamente troppi dettagli di `World`, ArcGraph diventa un
  secondo `MapGridWorldView`;
- se il FOV current cone viene copiato nel bridge, duplichiamo logica percettiva e
  aumentiamo rischio di divergenza;
- se HUD e label vengono migrati troppo presto, si mescolano dati di mappa,
  screen-space, input e UI;
- se landmark/GVD vengono agganciati senza classificazione dei kind, il renderer
  futuro ricevera' dati corretti ma poco spiegabili.

Prossimo micro-step consigliato:

```text
v0.37e - ArcGraph Landmark/GVD Debug Producer Bridge
```

Scope consigliato:

- implementare un adapter passivo da `LandmarkOverlayNode`, `LandmarkOverlayEdge`
  e `GvdDinOverlaySnapshot` verso `ArcGraphDebugOverlaySnapshot`;
- non implementare FOV current cone;
- non implementare renderer Unity;
- aggiungere harness smoke con dati DTO fittizi;
- mantenere fuori scope pointer coords, runtime cost HUD, summary cards, top bar
  e DevTools.

## Esito v0.37e - ArcGraph Landmark/GVD Debug Producer Bridge

La `v0.37e` implementa il primo bridge operativo passivo tra i DTO debug gia'
esistenti del Core e `ArcGraphDebugOverlaySnapshot`.

File/runtime aggiunti:

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlayProducerBridge.cs
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphDebugOverlayProducerBridgeHarness.cs
```

Elementi introdotti:

- aggiunto `ArcGraphDebugOverlayProducerBridgeDiagnostics`;
- aggiunto `ArcGraphDebugOverlayProducerBridge`;
- aggiunto `ArcGraphDebugOverlayProducerBridgeHarness`;
- conversione passiva di `LandmarkOverlayNode`;
- conversione passiva di `LandmarkOverlayEdge`;
- conversione passiva di `GvdDinOverlaySnapshot.DtCells`;
- conversione passiva di `GvdDinOverlaySnapshot.GvdRawCells`;
- conversione passiva di `GvdDinOverlaySnapshot.GvdNodes`;
- conversione passiva di `GvdDinOverlaySnapshot.GvdEdges`.

Flusso dati fissato:

```text
DTO Core gia' prodotti
-> ArcGraphDebugOverlayProducerBridge
-> ArcGraphDebugOverlaySnapshot
-> ArcGraphDebugOverlayQueueBuilder
-> ArcGraphDebugOverlayQueue
-> renderer Unity futuro
```

Mappatura Landmark:

```text
worldNodes      -> LandmarkWorldNode
worldEdges      -> LandmarkWorldEdge
knownNodes      -> LandmarkKnownNode
knownEdges      -> LandmarkKnownEdge
routeNodes      -> LandmarkRouteNode
routeEdges      -> LandmarkRouteEdge
lmPathEdges     -> LandmarkLmPathEdge
directPathEdges -> LandmarkDirectPathEdge
jumpPathEdges   -> LandmarkJumpPathEdge
complexEdges    -> LandmarkComplexEdge
```

Mappatura GVD-DIN:

```text
DtCells     -> DtHeatCell
GvdRawCells -> GvdRawCell
GvdNodes    -> LandmarkGvdNode
GvdEdges    -> LandmarkGvdEdge
```

Vincoli rispettati:

- nessuna lettura diretta `World`;
- nessuna chiamata a `World.GetNpcLandmarkOverlayData(...)`;
- nessuna dipendenza da `MapGridWorldView`;
- nessun `GameObject`;
- nessun `SpriteRenderer`;
- nessun `LineRenderer`;
- nessun `Canvas`;
- nessun `Resources.Load`;
- nessun input mouse/tastiera;
- nessuna chiamata a `SimulationHost`;
- nessun renderer Unity;
- nessuna modifica a scene, prefab, asset o `.meta`;
- FOV current cone non implementato.

QA eseguita:

- `git diff --check` superato;
- controllo statico sulle chiamate vietate eseguito: le parole vietate compaiono solo
  in commenti descrittivi o nomi enum gia' esistenti;
- compilazione Roslyn isolata riuscita sui file nuovi usando `Assembly-CSharp.dll`
  come riferimento per i DTO Core e i contratti ArcGraph gia' compilati.

Debiti residui:

- il bridge non e' ancora collegato a un chiamante runtime;
- non esiste ancora renderer Unity debug per disegnare questi item;
- FOV current cone resta da estrarre in un producer separato;
- `DebugFovTelemetry` heatmap storica non e' ancora bridgiata;
- HUD, pointer coords, runtime cost, labels e summary cards restano fuori scope.

Prossimo micro-step consigliato:

```text
v0.37f - ArcGraph Debug Overlay Runtime Feed Audit
```

Scope consigliato:

- audit del punto piu' sicuro in cui alimentare il bridge con DTO reali;
- decidere se il feed deve stare in `ArcGraphWorldAdapter`, in un adapter debug
  separato o in un wrapper runtime ArcGraph;
- non creare ancora renderer Unity;
- non migrare FOV current cone;
- non migrare HUD o strumenti interattivi.

---

## Esito v0.37f - ArcGraph Debug Overlay Runtime Feed Audit

La `v0.37f` chiude l'audit del punto piu' sicuro in cui alimentare il bridge
debug ArcGraph con DTO reali.

Conclusione principale:

```text
World / Core debug DTO
-> feed runtime debug separato
-> ArcGraphDebugOverlayProducerBridge
-> ArcGraphDebugOverlaySnapshot
-> ArcGraphDebugOverlayQueueBuilder
-> ArcGraphDebugOverlayQueue
-> renderer debug futuro
```

Scelta consigliata:

- non innestare il feed dentro `MapGridWorldView`, perche' quello e' il vecchio
  renderer Unity misto a input, HUD, overlay e strumenti debug;
- non gonfiare `ArcGraphWorldAdapter`, che oggi ha gia' una responsabilita'
  chiara: terreno, oggetti, actor e motion visuale;
- non mettere il feed dentro `ArcGraphLayerStack` o `ArcGraphDebugLayer`, perche'
  i layer devono restare cache/consumer di dati, non producer;
- introdurre un adapter/feed debug separato, ad esempio
  `ArcGraphDebugOverlayRuntimeFeed`, con cache interne riusabili e input espliciti.

Fonti runtime adatte al primo feed:

- `World.GetNpcLandmarkOverlayData(...)` per world nodes/edges, known
  nodes/edges, route nodes/edges e path debug;
- `World.GetGvdDinOverlayData(...)` per DT heatmap, GVD raw, GVD nodes e GVD
  edges;
- `activeNpcId` passato dal chiamante, non risolto con logica mouse dentro il feed.

Vincoli fissati:

- il feed puo' leggere `World` solo come consumer view/debug read-only;
- il feed non deve mutare `World`, job, pathfinding, percezione, memoria o input;
- il feed non deve creare `GameObject`, `SpriteRenderer`, `LineRenderer`,
  `Canvas`, asset o renderer Unity;
- il feed non deve dipendere da `MapGridWorldView`;
- il feed deve riusare liste/snapshot per evitare allocazioni superflue;
- FOV current cone resta escluso, perche' oggi e' ancora calcolato dentro
  `MapGridFovHeatmapOverlay.RenderCurrentCone(...)`;
- HUD, pointer coords, top bar, summary cards, DevTools e labels screen-space
  restano fuori scope.

Rischio principale rilevato:

- se il feed viene messo nel vecchio MapGrid, ArcGraph resta dipendente dal
  renderer da pensionare;
- se il feed viene messo nell'adapter generale, `ArcGraphWorldAdapter` diventa un
  contenitore crescente di logiche non omogenee;
- se il feed calcola FOV corrente prima di estrarre un producer dedicato, si
  duplica logica percettiva e si aumenta il rischio di divergenza.

Prossimo micro-step consigliato:

```text
v0.37g - ArcGraph Debug Overlay Runtime Feed
```

Scope consigliato:

- introdurre `ArcGraphDebugOverlayRuntimeFeed` come adapter/feed passivo;
- usare input espliciti: `World`, `activeNpcId`, flag Landmark/GVD;
- produrre `ArcGraphDebugOverlaySnapshot` e `ArcGraphDebugOverlayQueue`;
- aggiungere diagnostica e harness smoke;
- non creare renderer visuale;
- non migrare FOV current cone.

---

## Esito v0.37g - ArcGraph Debug Overlay Runtime Feed

La `v0.37g` introduce il primo feed runtime debug passivo di ArcGraph.

Componenti aggiunti:

- `ArcGraphDebugOverlayRuntimeFeed`;
- `ArcGraphDebugOverlayRuntimeFeedOptions`;
- `ArcGraphDebugOverlayRuntimeFeedDiagnostics`;
- `ArcGraphDebugOverlayRuntimeFeedHarness`.

Flusso dati implementato:

```text
World.GetNpcLandmarkOverlayData(...)
World.GetGvdDinOverlayData(...)
-> ArcGraphDebugOverlayRuntimeFeed
-> ArcGraphDebugOverlayProducerBridge
-> ArcGraphDebugOverlaySnapshot
-> ArcGraphDebugOverlayQueueBuilder
-> ArcGraphDebugOverlayQueue
```

Contratto runtime:

- il feed riceve `World`, `activeNpcId` e opzioni debug esplicite;
- il feed non decide autonomamente quale NPC sia attivo;
- il feed legge `World` solo come consumer view/debug read-only;
- il feed non muta `World`, pathfinding, job, memoria, percezione o input;
- il feed non consulta `MapGridWorldView`;
- il feed non crea `GameObject`, `SpriteRenderer`, `LineRenderer`, `Canvas`,
  asset, scena o prefab;
- il feed usa liste Landmark e snapshot GVD-DIN interni riusabili;
- il feed espone snapshot e queue finali per renderer futuri;
- il feed possiede diagnostica dedicata per distinguere world mancante, producer
  richiesti, producer tentati, item snapshot e item queue.

Smoke harness:

- caso `World == null`: produce queue vuota con reason `WorldMissing`;
- caso DTO minimi preparati a mano: produce queue Landmark/GVD valida;
- lo smoke non richiede scena Unity, camera, asset o MapGrid.

Vincoli preservati:

- nessun renderer visuale introdotto;
- nessuna sostituzione di MapGrid;
- nessuna migrazione FOV current cone;
- nessuna migrazione HUD, DevTools, top bar, pointer coords o summary cards;
- nessuna modifica a scene, prefab, asset o `.meta`.

QA eseguita:

- `git diff --check` superato;
- controllo statico sulle chiamate vietate eseguito: le occorrenze di
  `MapGridWorldView` nei nuovi file sono solo commenti/documentazione;
- compilazione Roslyn isolata riuscita includendo i contratti debug ArcGraph
  necessari. I warning prodotti sono conflitti attesi da compilazione isolata,
  perche' alcuni tipi ArcGraph sono gia' presenti in `Assembly-CSharp.dll`.

Debiti residui:

- il feed non e' ancora collegato al bootstrap/mainframe ArcGraph;
- non esiste ancora renderer visuale per `ArcGraphDebugOverlayQueue`;
- FOV current cone richiede ancora un producer separato;
- HUD e labels screen-space restano da trattare in un passaggio dedicato.

Prossimo micro-step consigliato:

```text
v0.37h - ArcGraph Debug Overlay Renderer Audit
```

Scope consigliato:

- audit del modo piu' sicuro per visualizzare la queue debug ArcGraph;
- decidere se estendere temporaneamente `ArcGraphSceneProbeRenderer` o creare un
  renderer debug separato;
- preparare un test visuale piccolo per Landmark/GVD;
- non creare ancora renderer produttivo definitivo;
- non migrare FOV current cone, HUD o DevTools.

---

## Esito v0.37h - ArcGraph Debug Overlay Renderer Audit

La `v0.37h` chiude l'audit del percorso piu' sicuro per visualizzare
`ArcGraphDebugOverlayQueue`.

File/contratti ispezionati:

- `ArcGraphSceneProbeRenderer`;
- `ArcGraphVisualProbeFrame`;
- `ArcGraphVisualProbeBuilder`;
- `ArcGraphDebugOverlayQueue`;
- `ArcGraphDebugCellOverlayItem`;
- `ArcGraphDebugNodeOverlayItem`;
- `ArcGraphDebugEdgeOverlayItem`;
- `ArcGraphDebugLabelOverlayItem`;
- `ArcGraphDebugOverlayRuntimeFeed`;
- `MapGridLandmarkOverlay` come reference legacy.

Conclusione principale:

```text
ArcGraphDebugOverlayQueue
-> ArcGraphDebugOverlaySceneProbeRenderer dedicato
-> root Unity temporaneo debug
-> sprite runtime per celle/nodi
-> LineRenderer temporanei per edge
```

Scelta consigliata:

- non estendere `ArcGraphVisualProbeFrame`, perche' quel frame e' nato per
  terrain, actor/object e layer ambientali;
- non gonfiare `ArcGraphSceneProbeRenderer`, che oggi e' un probe generico di
  fondazione e non deve diventare contenitore di tutti i debug futuri;
- introdurre un renderer probe separato, ad esempio
  `ArcGraphDebugOverlaySceneProbeRenderer`, specializzato su
  `ArcGraphDebugOverlayQueue`.

Motivazione:

- gli overlay debug hanno famiglie proprie: celle, nodi, edge, label/HUD;
- le celle DT/GVD raw possono essere rese con sprite colorati come il probe
  attuale;
- i nodi Landmark/GVD possono essere resi con sprite colorati e scala diversa;
- gli edge Landmark/GVD richiedono una primitiva diversa dagli sprite quadrati:
  nel legacy `MapGridLandmarkOverlay` sono `LineRenderer`;
- le label e HUD richiedono un canale screen-space separato e non vanno incluse
  nel primo probe visuale.

Vincoli del renderer debug futuro:

- deve consumare solo `ArcGraphDebugOverlayQueue`;
- non deve leggere `World`;
- non deve chiamare `ArcGraphDebugOverlayRuntimeFeed`;
- non deve dipendere da `MapGridWorldView`;
- non deve risolvere input, NPC attivo, toggle o camera da solo salvo riferimenti
  serializzati espliciti;
- deve creare solo oggetti runtime temporanei sotto root dedicato;
- deve avere cleanup confinato;
- deve restare un probe di test, non renderer produttivo definitivo;
- non deve modificare scene, prefab, asset o `.meta`.

Policy visuale minima consigliata:

- `DtHeatCell`: sprite cella con colore/intensita' da heatmap;
- `GvdRawCell`: sprite cella ciano/azzurro trasparente;
- `LandmarkWorldNode`: marker bianco;
- `LandmarkKnownNode`: marker verde;
- `LandmarkRouteNode`: marker arancione;
- `LandmarkGvdNode`: marker viola;
- edge world/known/route/path/GVD: `LineRenderer` con colori coerenti al legacy;
- labels/HUD: ignorati nel primo renderer probe.

Prossimo micro-step consigliato:

```text
v0.37i - ArcGraph Debug Overlay Scene Probe Renderer
```

Scope consigliato:

- aggiungere `ArcGraphDebugOverlaySceneProbeRenderer`;
- aggiungere context menu `ArcGraph/Render Default Debug Overlay Probe`;
- generare una queue finta Landmark/GVD tramite DTO preparati o feed prepared-data;
- disegnare celle, nodi ed edge;
- loggare diagnostica della queue renderizzata;
- non collegare il renderer al runtime reale;
- non usare `World`;
- non migrare FOV current cone, labels, HUD, DevTools o top bar.

## Esito v0.37i - ArcGraph Debug Overlay Scene Probe Renderer

La `v0.37i` introduce il primo renderer scena temporaneo per visualizzare una
`ArcGraphDebugOverlayQueue` senza agganciare ancora il runtime reale.

Componente introdotto:

- `ArcGraphDebugOverlaySceneProbeRenderer`

Responsabilita' del componente:

- consumare una `ArcGraphDebugOverlayQueue` gia' pronta;
- disegnare celle debug con sprite runtime 1x1;
- disegnare nodi debug con sprite runtime 1x1;
- disegnare edge debug con `LineRenderer` temporanei;
- creare un root temporaneo dedicato;
- pulire solo gli oggetti generati dal probe;
- offrire context menu `ArcGraph/Render Default Debug Overlay Probe`;
- offrire context menu `ArcGraph/Clear Debug Overlay Probe`;
- loggare una diagnostica sintetica con conteggi cell/node/edge/label.

Vincoli mantenuti:

- nessuna lettura diretta di `World`;
- nessuna dipendenza da `MapGridWorldView`;
- nessuna modifica a scene, prefab, asset o `.meta`;
- nessun collegamento automatico al runtime produttivo;
- labels/HUD ignorati intenzionalmente nel primo renderer;
- FOV current cone ancora fuori scope.

Il renderer usa il feed prepared-data solo per costruire il probe finto da
context menu. Questo non e' un aggancio runtime reale: serve a generare una
queue dimostrativa Landmark/GVD verificabile a schermo.

Prossimo micro-step consigliato:

```text
v0.37j - ArcGraph Debug Overlay Visual QA
```

Scope consigliato:

- aggiungere il componente a un GameObject temporaneo in scena senza salvare asset;
- eseguire `ArcGraph/Render Default Debug Overlay Probe`;
- verificare visivamente celle, nodi ed edge;
- verificare `ArcGraph/Clear Debug Overlay Probe`;
- controllare Console Unity per diagnostica e warning;
- non agganciare ancora `World`, NPC attivo, toggle UI o DevTools.

## Esito v0.37j - ArcGraph Debug Overlay Visual QA

La `v0.37j` esegue la QA tecnica del renderer probe debug introdotto in
`v0.37i` e prepara il gate visuale manuale in Unity.

Verifiche tecniche completate:

- Unity Editor aperto sul progetto ARCONTIO;
- log Editor controllato dopo import script;
- ricompilazione Unity riuscita con `Tundra build success`;
- nessun errore C# prodotto dal renderer debug;
- warning presenti solo su codice legacy MapGrid/AtomViewer gia' noto;
- compilazione Roslyn isolata del renderer e dei feed debug riuscita;
- context menu verificati sul componente:
  - `ArcGraph/Render Default Debug Overlay Probe`;
  - `ArcGraph/Clear Debug Overlay Probe`;
- controllo dipendenze vietate riuscito:
  - nessun `Resources.Load`;
  - nessun `MapGridWorldView`;
  - nessun `SimulationHost`;
  - nessuna lettura diretta producer `World.GetNpcLandmarkOverlayData`;
  - nessuna lettura diretta producer `World.GetGvdDinOverlayData`.

Limite della QA:

- la verifica visiva reale non puo' essere chiusa solo da shell;
- non e' stato introdotto un editor runner automatico, per evitare uno script
  QA fuori scope e modifiche scena/prefab;
- la conferma finale richiede osservazione umana in Unity.

Gate visuale manuale richiesto:

1. aggiungere temporaneamente il componente `ArcGraphDebugOverlaySceneProbeRenderer`
   a un GameObject di test;
2. non salvare la scena;
3. opzionalmente assegnare `MainCamera` al campo `Scene Camera`;
4. usare il context menu del componente:
   `ArcGraph/Render Default Debug Overlay Probe`;
5. verificare la creazione del root temporaneo:
   `ArcGraphDebugOverlaySceneProbeRoot`;
6. verificare la presenza di celle, nodi ed edge colorati;
7. controllare in Console il log atteso:
   `cells=3, nodes=4, edges=8, labelsIgnored=0, visible=15`;
8. usare `ArcGraph/Clear Debug Overlay Probe`;
9. verificare che il root temporaneo venga eliminato.

Decisione per proseguire:

- se il gate visuale manuale e' positivo, si puo' passare a
  `v0.37k - ArcGraph Debug Runtime Wiring Audit`;
- se il probe non e' visibile, prima va corretta posizione/camera/sorting del
  probe, senza ancora collegarlo al runtime reale.

## Esito v0.37k - ArcGraph Debug Runtime Wiring Audit

La `v0.37k` analizza dove e come collegare in futuro il feed runtime debug
ArcGraph al renderer probe, senza implementare ancora il collegamento operativo.

Codice ispezionato:

- `MapGridWorldView`;
- `MapGridLandmarkOverlay`;
- `MapGridFovHeatmapOverlay`;
- `MapGridRuntimeControlTopBar`;
- `MapGridWorldProvider`;
- `NPCSelection`;
- `ArcGraphRuntimeContext`;
- `ArcGraphBootstrapRuntime`;
- `ArcGraphDebugOverlayRuntimeFeed`;
- `ArcGraphDebugOverlayRuntimeFeedOptions`;
- `ArcGraphDebugOverlaySceneProbeRenderer`.

Conclusione principale:

- il renderer `ArcGraphDebugOverlaySceneProbeRenderer` non deve diventare il punto
  di wiring runtime;
- il feed `ArcGraphDebugOverlayRuntimeFeed` non deve decidere input, toggle o NPC
  attivo;
- il wiring futuro deve stare in un coordinatore view-side separato, piccolo,
  disattivato di default e alimentato da context esplicito.

Perimetro corretto del futuro coordinatore:

- ricevere un `ArcGraphRuntimeContext` gia' costruito;
- leggere `context.World` come sorgente runtime read-only;
- ricevere un `activeNpcId` gia' deciso da un selector esterno;
- ricevere opzioni esplicite `ArcGraphDebugOverlayRuntimeFeedOptions`;
- chiamare `ArcGraphDebugOverlayRuntimeFeed.BuildFromWorld(...)`;
- passare `feed.Queue` al renderer debug;
- non leggere `SimulationHost.Instance`;
- non usare `MapGridWorldProvider`;
- non dipendere da `MapGridWorldView`;
- non leggere direttamente tastiera, mouse o camera;
- non risolvere hover/picking;
- non creare GameObject produttivi permanenti;
- non modificare scene, prefab, asset o `.meta`.

Sorgenti candidate per gli input:

- `World`: deve arrivare da `ArcGraphRuntimeContext`, coerente con la policy
  definita in `v0.31`;
- `activeNpcId`: puo' arrivare da `NPCSelection.SelectedNpcId`, perche' e' uno
  stato view-only condiviso, ma il coordinatore deve riceverlo o leggerlo tramite
  un adapter minimo, non duplicare il picking MapGrid;
- toggle Landmark/GVD/DT: devono diventare stato esplicito ArcGraph debug, non
  hotkey sparse dentro il renderer;
- camera/posizionamento: restano responsabilita' del renderer o della scena, non
  del feed.

Perche' non copiare `MapGridWorldView.Update()`:

- oggi `MapGridWorldView` concentra input, picking, overlay, selezione, render,
  label e comandi debug;
- copiare quel modello dentro ArcGraph creerebbe un secondo Decision/UI Layer
  parallelo;
- ArcGraph deve invece consumare dati gia' risolti e renderizzarli;
- il renderer deve restare consumer grafico, non orchestratore simulativo.

FOV current cone:

- resta fuori scope;
- oggi viene calcolato dentro `MapGridFovHeatmapOverlay.RenderCurrentCone(...)`;
- non esiste ancora un producer DTO separato equivalente a Landmark/GVD;
- portarlo ora richiederebbe estrarre un contratto dati dedicato e rischierebbe di
  gonfiare `v0.37`.

Policy prestazionale consigliata:

- aggiornare il debug runtime solo quando overlay ArcGraph debug e' attivo;
- non aggiornare ogni frame se non necessario;
- usare una cadenza configurabile o legata a tick simulativi osservabili;
- riusare il feed e le liste interne gia' allocate;
- mantenere GVD/DT opzionali, perche' possono produrre molte celle;
- loggare diagnostica sintetica, non dettagli per item.

Prossimo micro-step consigliato:

```text
v0.37l - ArcGraph Debug Runtime Wiring Contract
```

Scope consigliato:

- definire un contratto dati/stato per il coordinatore runtime debug;
- non creare ancora un wrapper scena automatico;
- non leggere `SimulationHost.Instance`;
- non creare hotkey;
- non aggiungere UI;
- non migrare FOV;
- preparare solo il punto in cui un futuro componente potra' chiamare feed e
  renderer in modo controllato.

## Esito v0.37l - ArcGraph Debug Runtime Wiring Contract

La `v0.37l` introduce il contratto C# passivo del futuro wiring runtime debug
ArcGraph.

File introdotti:

- `IArcGraphDebugOverlayQueueConsumer`;
- `ArcGraphDebugRuntimeWiringFrame`;
- `ArcGraphDebugRuntimeWiringDiagnostics`;
- `ArcGraphDebugRuntimeWiringCoordinator`;
- `ArcGraphDebugRuntimeWiringHarness`.

File aggiornato:

- `ArcGraphDebugOverlaySceneProbeRenderer`.

Contratto introdotto:

- `IArcGraphDebugOverlayQueueConsumer` definisce il consumer tipizzato di una
  `ArcGraphDebugOverlayQueue`;
- `ArcGraphDebugOverlaySceneProbeRenderer` implementa il consumer senza cambiare
  comportamento;
- `ArcGraphDebugRuntimeWiringFrame` trasporta context, active NPC, opzioni debug,
  gate overlay e richiesta di dispatch;
- `ArcGraphDebugRuntimeWiringCoordinator` valida il frame, chiama il feed e,
  solo se richiesto, consegna la queue a un consumer fornito dall'esterno;
- `ArcGraphDebugRuntimeWiringDiagnostics` spiega perche' il coordinatore ha
  lavorato o si e' fermato;
- `ArcGraphDebugRuntimeWiringHarness` verifica i gate base senza World reale e
  senza scena Unity.

Vincoli mantenuti:

- nessuna lettura di `SimulationHost.Instance`;
- nessuna dipendenza da `MapGridWorldProvider`;
- nessuna dipendenza da `MapGridWorldView`;
- nessuna hotkey;
- nessuna UI;
- nessun picking;
- nessuna ricerca scena;
- nessun wrapper automatico;
- nessuna modifica a scene, prefab, asset o `.meta`;
- FOV current cone ancora fuori scope.

Comportamento del coordinatore:

- se il frame manca, ritorna `FrameMissing`;
- se l'overlay e' spento, ritorna `OverlayDisabled`;
- se il context manca, ritorna `RuntimeContextMissing`;
- se il context non contiene World, ritorna `WorldMissing`;
- se il frame e' valido, invoca `ArcGraphDebugOverlayRuntimeFeed.BuildFromWorld`;
- se il dispatch e' richiesto e il consumer e' presente, consegna `feed.Queue`;
- se il consumer manca, produce comunque diagnostica e queue, senza renderizzare.

Prossimo micro-step consigliato:

```text
v0.37m - ArcGraph Debug Runtime Scene Wrapper
```

Scope consigliato:

- creare un componente scena piccolo e disattivato di default;
- ricevere riferimenti serializzati espliciti;
- costruire `ArcGraphDebugRuntimeWiringFrame`;
- usare `NPCSelection.SelectedNpcId` solo come sorgente view-only dell'active NPC;
- chiamare `ArcGraphDebugRuntimeWiringCoordinator`;
- non introdurre hotkey;
- non creare UI;
- non usare `SimulationHost.Instance`;
- non usare `MapGridWorldProvider`;
- non salvare scene o prefab.

## Esito v0.37m - ArcGraph Debug Runtime Scene Wrapper

La `v0.37m` introduce il wrapper Unity passivo che permette, in una scena di
test controllata, di chiamare il contratto di wiring runtime debug senza
trasformare ArcGraph in un nuovo manager globale.

File introdotto:

- `ArcGraphDebugRuntimeSceneWrapper`.

Contratto del wrapper:

- e' un `MonoBehaviour` scene-side;
- resta spento di default tramite `overlayEnabled = false`;
- non chiama `Update`;
- non cerca il `World`;
- non legge `SimulationHost.Instance`;
- non usa `MapGridWorldProvider`;
- non dipende da `MapGridWorldView`;
- non legge `NPCSelection.SelectedNpcId`;
- non crea hotkey, UI, picking o strumenti interattivi;
- riceve `ArcGraphRuntimeContext` tramite `SetRuntimeContext(...)` o
  `ProcessFrame(context, npcId, sourceTick)`;
- riceve l'id NPC attivo tramite `SetActiveNpcId(...)` o overload di
  `ProcessFrame(...)`;
- costruisce `ArcGraphDebugRuntimeWiringFrame`;
- chiama `ArcGraphDebugRuntimeWiringCoordinator`;
- consegna la queue solo al consumer visuale assegnato esplicitamente
  nell'Inspector.

Decisione prudenziale:

- la nota precedente indicava `NPCSelection.SelectedNpcId` come sorgente
  view-only possibile;
- in `v0.37m` il wrapper non la legge direttamente;
- questa scelta mantiene il wrapper ancora piu' passivo;
- un eventuale adapter `NPCSelection -> activeNpcId` resta micro-step separato,
  cosi' la selezione globale non viene fusa col wiring debug.

Context menu disponibili:

- `ArcGraph/Run Debug Runtime Wiring Smoke`;
- `ArcGraph/Process Debug Runtime Frame`.

Comportamento atteso:

- se nessun context e' stato fornito, il wrapper produce diagnostica
  `RuntimeContextMissing`;
- se il context esiste ma non contiene `World`, produce `WorldMissing`;
- se overlay e' spento, produce `OverlayDisabled`;
- se context, World, NPC e consumer sono forniti, il coordinator puo' costruire
  la queue e dispatcharla al renderer debug.

Vincoli mantenuti:

- nessuna scena modificata;
- nessun prefab modificato;
- nessun asset modificato;
- nessun `.meta` aggiunto;
- nessuna sostituzione del renderer MapGrid;
- FOV current cone ancora fuori scope.

Prossimo micro-step consigliato:

```text
v0.37n - ArcGraph Debug Runtime Context Adapter Audit
```

Scope consigliato:

- auditare come produrre in modo controllato un `ArcGraphRuntimeContext` reale
  per il wrapper;
- decidere se l'adapter deve stare vicino a MapGrid, ArcGraph o bootstrap scena;
- valutare come passare l'NPC selezionato senza far leggere `NPCSelection` al
  wrapper;
- non introdurre ancora hotkey o UI;
- non salvare scene o prefab.

## Esito v0.37n - ArcGraph Debug Runtime Context Adapter Audit

La `v0.37n` e' un audit mirato: verifica da dove puo' arrivare il
`ArcGraphRuntimeContext` reale usato dal wrapper debug senza trasformare
ArcGraph in un lettore globale della scena o del core.

File auditati:

- `MapGridBootstrap`;
- `MapGridWorldView`;
- `MapGridWorldProvider`;
- `MapGridData`;
- `NPCSelection`;
- `ArcGraphRuntimeContext`;
- `ArcGraphDebugRuntimeSceneWrapper`.

Conclusione principale:

- per il debug overlay Landmark/GVD di `v0.37` non serve un context completo;
- serve soprattutto il riferimento a `World`;
- `MapGridConfig` e' utile per coerenza view-side, ma non indispensabile per
  produrre la queue debug;
- `MapGridData` non serve in questo micro-perimetro, perche' Landmark/GVD sono
  letti dal `World`;
- il context puo' quindi essere:

```text
ArcGraphRuntimeContext(
    config: RuntimeConfig della MapGrid, se disponibile,
    map: null,
    world: RuntimeWorld della MapGrid
)
```

Stato attuale delle sorgenti:

- `MapGridWorldView` legge gia' il `World` corrente tramite
  `MapGridWorldProvider.TryGetWorld()`;
- `MapGridWorldView` gestisce gia' il rebind quando il `World` cambia dopo load
  snapshot;
- questo rende `MapGridWorldView` il punto view-side piu' stabile da cui
  ricavare il `World` per ArcGraph debug;
- pero' il campo `_world` e' privato e oggi non esiste una property
  `RuntimeWorld`;
- `MapGridBootstrap` possiede `_map`, ma non la espone;
- non e' necessario toccare `_map` per il debug overlay `v0.37`;
- `NPCSelection.SelectedNpcId` e' una sorgente view-only gia' esistente e
  coerente per scegliere l'NPC attivo.

Decisione consigliata:

- non far leggere `MapGridWorldProvider` al wrapper ArcGraph;
- non far leggere `NPCSelection` al wrapper ArcGraph;
- non far scegliere al wrapper un fallback come "primo NPC disponibile";
- introdurre invece un adapter separato:

```text
ArcGraphDebugRuntimeMapGridAdapter
```

Responsabilita' dell'adapter:

- referenziare esplicitamente `MapGridWorldView`;
- referenziare esplicitamente `ArcGraphDebugRuntimeSceneWrapper`;
- leggere `RuntimeConfig` dalla view MapGrid;
- leggere una futura property read-only `RuntimeWorld` dalla view MapGrid;
- leggere `NPCSelection.SelectedNpcId`;
- costruire un `ArcGraphRuntimeContext` parziale;
- chiamare il wrapper con `ProcessFrame(context, npcId, sourceTick)`;
- non creare renderer;
- non leggere input;
- non introdurre hotkey;
- non introdurre UI;
- non fare polling automatico acceso di default.

Micro-modifica MapGrid consigliata:

```csharp
public World RuntimeWorld => _world;
```

Questa property e' preferibile a far usare direttamente
`MapGridWorldProvider.TryGetWorld()` all'adapter, perche':

- riusa il rebind gia' gestito da `MapGridWorldView`;
- evita una seconda sorgente view-side parallela;
- rende esplicito che ArcGraph debug sta consumando la view MapGrid corrente;
- non muta il `World`;
- non espone API di comando.

Gestione NPC attivo:

- l'adapter puo' leggere `NPCSelection.SelectedNpcId`;
- se l'id e' `<= 0`, passa `-1` al wrapper;
- non deve cercare il primo NPC esistente;
- non deve fare scansioni di `World.NpcDna`;
- in questo modo si evita costo CPU e si evita una policy nascosta di selezione.

Gestione tick:

- se `World` e' disponibile, l'adapter puo' usare
  `world.Global.CurrentTickIndex` come `sourceTick`;
- il tick resta diagnostico;
- il wrapper e il coordinator non devono leggere `TickContext` o
  `SimulationHost`.

Prossimo micro-step consigliato:

```text
v0.37o - ArcGraph Debug Runtime MapGrid Adapter
```

Scope consigliato:

- aggiungere property read-only minima `RuntimeWorld` su `MapGridWorldView`;
- implementare `ArcGraphDebugRuntimeMapGridAdapter`;
- lasciare refresh automatico spento di default;
- fornire context menu manuale per pushare il frame corrente;
- non salvare scene o prefab;
- non aggiungere `.meta`;
- non introdurre hotkey o UI.

## Esito v0.37o - ArcGraph Debug Runtime MapGrid Adapter

La `v0.37o` implementa il ponte manuale e controllato tra la view MapGrid
attualmente produttiva e il wrapper runtime debug ArcGraph.

File modificato:

- `MapGridWorldView`.

File introdotto:

- `ArcGraphDebugRuntimeMapGridAdapter`.

Elemento aggiunto su MapGrid:

```csharp
public World RuntimeWorld => _world;
```

La property e' read-only e restituisce il `World` gia' bindato da
`MapGridWorldView`. Non cerca il mondo, non chiama provider e non muta stato
simulativo.

Adapter introdotto:

```text
ArcGraphDebugRuntimeMapGridAdapter
```

Responsabilita':

- ricevere da Inspector un riferimento esplicito a `MapGridWorldView`;
- ricevere da Inspector un riferimento esplicito a
  `ArcGraphDebugRuntimeSceneWrapper`;
- leggere `RuntimeConfig` dalla MapGrid;
- leggere `RuntimeWorld` dalla MapGrid;
- leggere `NPCSelection.SelectedNpcId`;
- costruire un context parziale:

```text
ArcGraphRuntimeContext(
    config: mapGridWorldView.RuntimeConfig,
    map: null,
    world: mapGridWorldView.RuntimeWorld
)
```

- leggere `world.Global.CurrentTickIndex` solo come dato diagnostico;
- chiamare il wrapper tramite:

```text
targetWrapper.ProcessFrame(context, selectedNpcId, sourceTick)
```

Diagnostica introdotta:

```text
ArcGraphDebugRuntimeMapGridAdapterDiagnostics
```

Campi principali:

- presenza della MapGrid view;
- presenza del wrapper;
- presenza della config;
- presenza del World;
- NPC selezionato;
- tick sorgente;
- diagnostica restituita dal wrapper;
- reason sintetica.

Context menu introdotto:

```text
ArcGraph/Push Debug Runtime Frame From MapGrid
```

Vincoli mantenuti:

- nessun `Update`;
- nessun polling automatico;
- nessuna hotkey;
- nessuna UI;
- nessuna lettura di `SimulationHost.Instance`;
- nessuna lettura di `MapGridWorldProvider`;
- nessuna lettura diretta del `World` da parte del wrapper;
- nessuna selezione NPC interna al wrapper;
- nessuna scena modificata;
- nessun prefab modificato;
- nessun asset modificato;
- nessun `.meta` aggiunto.

Comportamento atteso:

- se manca il wrapper, diagnostica `TargetWrapperMissing`;
- se manca MapGrid, viene passato context nullo e il wrapper potra' produrre
  `RuntimeContextMissing`;
- se MapGrid esiste ma non ha ancora World, viene passato context parziale e il
  wrapper potra' produrre `WorldMissing`;
- se l'overlay del wrapper e' spento, il wrapper produce `OverlayDisabled`;
- se wrapper, MapGrid, World e overlay sono pronti, il feed puo' costruire e
  dispatchare la queue al renderer debug.

Prossimo micro-step consigliato:

```text
v0.37p - ArcGraph Debug Runtime Adapter QA
```

Scope consigliato:

- compilare il nuovo adapter;
- verificare le chiamate vietate;
- preparare istruzioni di test Inspector;
- eseguire gate visuale umano su scena non salvata;
- decidere se serve un micro-step successivo per refresh manuale/periodico.

## Esito v0.37p - ArcGraph Debug Runtime Adapter QA

La `v0.37p` completa la QA tecnica dell'adapter MapGrid -> ArcGraph e prepara
il gate visuale umano.

QA tecnica eseguita:

- `git diff --check` riuscito;
- compilazione Roslyn isolata riuscita;
- controllo statico chiamate vietate riuscito sul nuovo perimetro ArcGraph.

Perimetro compilato:

- `MapGridWorldView`;
- `MapGridRuntimeControlTopBar`;
- `ArcGraphDebugRuntimeMapGridAdapter`;
- `ArcGraphDebugRuntimeSceneWrapper`;
- `IArcGraphDebugOverlayQueueConsumer`;
- `ArcGraphDebugRuntimeWiringFrame`;
- `ArcGraphDebugRuntimeWiringDiagnostics`;
- `ArcGraphDebugRuntimeWiringCoordinator`;
- `ArcGraphDebugRuntimeWiringHarness`;
- `ArcGraphDebugOverlayRuntimeFeedOptions`;
- `ArcGraphDebugOverlayRuntimeFeedDiagnostics`;
- `ArcGraphDebugOverlayProducerBridge`;
- `ArcGraphDebugOverlayRuntimeFeed`;
- `ArcGraphDebugOverlaySceneProbeRenderer`.

Warning osservati:

- conflitti di tipo dovuti alla compilazione isolata di file legacy gia'
  presenti in `Assembly-CSharp`;
- campi Unity serializzati assegnabili da Inspector;
- chiamate legacy `FindObjectOfType` gia' presenti in MapGrid.

Nessuno di questi warning indica un errore introdotto dall'adapter.

Controllo vincoli:

- nessuna chiamata operativa a `SimulationHost` nel nuovo adapter;
- nessuna chiamata operativa a `MapGridWorldProvider` nel nuovo adapter;
- nessun `Update`;
- nessun polling automatico;
- nessuna hotkey;
- nessuna UI;
- nessun `Resources.Load`;
- nessuna modifica a scena, prefab, asset o `.meta`.

Gate visuale umano richiesto:

```text
1. Aprire Scene_MapGrid.
2. Non salvare la scena.
3. Creare o selezionare un GameObject temporaneo di test.
4. Aggiungere:
   - ArcGraphDebugOverlaySceneProbeRenderer;
   - ArcGraphDebugRuntimeSceneWrapper;
   - ArcGraphDebugRuntimeMapGridAdapter.
5. Nel wrapper assegnare:
   - Debug Overlay Renderer = ArcGraphDebugOverlaySceneProbeRenderer;
   - Overlay Enabled = true;
   - Dispatch To Renderer = true.
6. Nell'adapter assegnare:
   - Map Grid World View = componente MapGridWorldView della scena;
   - Target Wrapper = componente ArcGraphDebugRuntimeSceneWrapper.
7. Entrare in Play Mode.
8. Selezionare un NPC, se disponibile.
9. Dal context menu dell'adapter usare:
   ArcGraph/Push Debug Runtime Frame From MapGrid.
10. Controllare Console.
11. Controllare la presenza del root:
   ArcGraphDebugOverlaySceneProbeRoot.
12. Usare:
   ArcGraph/Clear Debug Overlay Probe.
13. Uscire da Play Mode.
14. Non salvare la scena.
```

Log attesi:

- adapter: `FramePushedToWrapper`;
- `mapGridView=True`;
- `wrapper=True`;
- `world=True`, se MapGrid ha gia' bindato il World;
- `selectedNpc=<id>` oppure `selectedNpc=-1`;
- wrapper: `QueueDispatched`, se overlay, World e renderer sono validi;
- renderer: conteggi celle/nodi/edge visibili, se il runtime produce item.

Esiti ammessi:

- `OverlayDisabled`: wrapper spento;
- `RuntimeContextMissing`: adapter senza MapGridView;
- `WorldMissing`: World non ancora bindato dalla MapGrid;
- `QueueDispatched`: dispatch riuscito al renderer;
- `FramePushedToWrapper`: adapter riuscito nel proprio compito.

Decisione post-gate:

- se il test e' positivo, `v0.37` puo' andare verso closeout della Debug/Overlay
  Migration;
- se il test fallisce, aprire solo un micro-step correttivo mirato alla ragione
  osservata;
- non introdurre refresh automatico, hotkey o UI finche' il gate manuale non e'
  stabile.

## v0.37q - ArcGraph Debug Overlay Closeout or Fix Gate

La `v0.37q` e' il punto operativo successivo alla QA tecnica `v0.37p`.

Non aggiunge nuove feature ArcGraph.
Non introduce renderer produttivi.
Non abilita polling automatico.
Non apre ancora la `v0.38`.

Il suo compito e' mantenere esplicita una scelta molto semplice:

- se il gate visuale umano dell'adapter MapGrid -> ArcGraph e' positivo, `v0.37`
  puo' essere chiusa come Debug/Overlay Migration preparatoria;
- se il gate visuale fallisce, il branch `v0.37q` deve ospitare solo il fix minimo
  collegato alla causa osservata;
- se il gate non e' ancora stato eseguito o registrato, `v0.37` resta in attesa e
  la `v0.38` non deve partire.

Condizione per passare alla fase successiva:

```text
ArcGraphDebugRuntimeMapGridAdapter
-> ArcGraphDebugRuntimeSceneWrapper
-> ArcGraphDebugRuntimeWiringCoordinator
-> ArcGraphDebugOverlayRuntimeFeed
-> ArcGraphDebugOverlayQueue
-> ArcGraphDebugOverlaySceneProbeRenderer
```

deve produrre un esito manuale leggibile in Unity, almeno come dispatch corretto e
assenza di errori runtime nel probe debug.

Esito gate visuale umano:

- gate superato su `Scene_MapGrid`;
- `ArcGraphDebugRuntimeMapGridAdapter` ha prodotto `FramePushedToWrapper`;
- diagnostica positiva osservata: `mapGridView=True`, `wrapper=True`,
  `config=True`, `world=True`, `selectedNpc=1`;
- `ArcGraphDebugRuntimeSceneWrapper` ha prodotto `wrapperReason=QueueDispatched`;
- diagnostica positiva osservata: `wrapperBuilt=True`,
  `wrapperDispatched=True`;
- il componente `MapGridWorldView` e' stato confermato come componente aggiunto
  runtime sullo stesso GameObject di `MapGridBootstrap`, non come GameObject
  separato in Hierarchy.

Conclusione:

```text
v0.37 - ArcGraph Debug/Overlay Migration
= COMPLETATA NEL PERIMETRO PREPARATORIO
```

La catena manuale:

```text
MapGridWorldView
-> ArcGraphDebugRuntimeMapGridAdapter
-> ArcGraphDebugRuntimeSceneWrapper
-> ArcGraphDebugRuntimeWiringCoordinator
-> ArcGraphDebugOverlayRuntimeFeed
-> ArcGraphDebugOverlayQueue
-> ArcGraphDebugOverlaySceneProbeRenderer
```

e' stata validata come ponte debug read-only. Non e' ancora un renderer
produttivo e non sostituisce `MapGridWorldView`, ma permette di aprire la fase
successiva di assorbimento legacy.

---

#### v0.38 - ArcGraph Legacy Absorption / Retirement

## Stato
APERTO / AUDIT-FIRST

## Obiettivo

Assorbire e poi pensionare il rendering legacy MapGrid, evitando un doppio sistema permanente.

La chiusura di questa fase richiedera':

- ArcGraph terrain produttivo;
- actor/object renderer produttivo;
- debug minimo funzionante;
- piano di dismissione `MapGridWorldView`;
- piano di dismissione `MapGridBootstrap`;
- mantenimento dei soli asset/helper utili.

## Checkpoint v0.38

| Checkpoint | Task | Stato |
|---|---|---|
| v0.38a | Audit assorbimento legacy MapGrid: bootstrap, terrain, actor/object, overlay, input, UI debug e dipendenze scena | Completato |
| v0.38b | Piano di sostituzione bootstrap scena ArcGraph, senza doppio renderer permanente | Completato |
| v0.38c | Piano ponte terrain ArcGraph controllato o comparativo finale | Completato |
| v0.38c.01 | Contratto accesso read-only alla mappa runtime per snapshot terrain ArcGraph | Completato |
| v0.38c.02 | Probe scena terrain ArcGraph temporaneo e gated | Completato |
| v0.38c.03 | Gate visuale terrain ArcGraph vs MapGrid | Completato |
| v0.38d | Aggancio actor/object ArcGraph produttivo con movimento multi-tick | Gate visuale congelato |
| v0.38e | Aggancio overlay debug minimo validato | Completato audit |
| v0.38f | Separazione strumenti interattivi/dev tools dal renderer legacy | Completato audit |
| v0.38f.01 | Contratto passivo boundary interattivo ArcGraph per picking cella/actor/object | Completato |
| v0.38f.02 | Audit adapter scena interazione ArcGraph, input, camera e pannelli debug modulari | Completato audit |
| v0.38f.03 | Contratto adapter scena interazione ArcGraph, senza migrazione pannelli | Completato |
| v0.38f.04 | Wrapper Unity passivo per input scena ArcGraph, spento/gated e senza tool migration | Completato |
| v0.38f.05 | Audit consumer interattivi: selection, pointer HUD, DevTools, top bar e side panel | Completato audit |
| v0.38f.06 | Contratto passivo Pointer HUD ArcGraph, senza UI scena e senza comandi | Completato |
| v0.38f.07 | Consumer scena Pointer HUD ArcGraph, gated e senza salvataggio scena | Completato |
| v0.38f.08 | Consumer selection ArcGraph, separato dal renderer e senza DevTools | Completato |
| v0.38f.09 | Router consumer interattivi ArcGraph per HUD + selection modulari | Completato |
| v0.38f.10a | Probe manuale per consegnare la render queue actor/object al wrapper interattivo | Completato |
| v0.38f.10 | Gate visuale consumer modulari ArcGraph: wrapper -> router -> HUD + selection | Completato |
| v0.38g.00 | Audit dipendenze pensionamento MapGrid dopo gate visuali congelati | Completato audit |
| v0.38g.01 | Backlog ordinato dei gate visuali ArcGraph recuperati/congelati | Completato |
| v0.38g.02 | Recupero gate interaction ArcGraph: wrapper -> router -> HUD + selection | Completato |
| v0.38g.03 | Audit percorso runtime minimo stabile ArcGraph dopo gate validati | Completato audit |
| v0.38g.04 | Contratto coordinator runtime minimo ArcGraph, gated e senza pensionamento MapGrid | Completato |
| v0.38g.05 | Wrapper scena minimo per alimentare il coordinator runtime ArcGraph | Pending |
| v0.38g | Pensionamento controllato componenti MapGrid assorbiti | Bloccato da mancanza percorso produttivo stabile |
| v0.38h | QA finale ArcGraph come renderer principale o decisione stop-go motivata | Pending |
| v0.38j.01 | Metadati traversabilita' terrain: acqua non camminabile e pavimento a piastrella configurabile | Completato |
| v0.38j.02 | Metadati muri: struttura 32x83, footprint 1x1, blocco movimento e blocco visione | Completato |
| v0.38j.03 | Resolver muri cardinali: scelta sprite in base ai vicini N/E/S/W | Completato data-only |
| v0.38j.04 | Renderer oggetti/muri ArcGraph con altezza sprite e ordinamento dietro/davanti NPC | Completato data-only |
| v0.38j.05 | Contratto mini-tile 16x16 per base muri sottili e giunzioni pavimento | Completato data-only |
| v0.38j.06 | Resolver spritesheet muri: striscia unica 17 slot 32x83 e sub-sprite sliced | Completato data-only |

## Roadmap operativa v0.38j - Terreno, muri e blocchi fisici

Questa micro-roadmap completa la parte minima di ArcGraph necessaria a trattare
terreno, acqua, pavimenti e muri come elementi grafici e semi-fisici coerenti.

1. `v0.38j.01` - Traversabilita' terrain.
   L'acqua deve essere visibile come terrain ma non attraversabile. Il blocco
   terrain non deve essere confuso con i muri legacy, altrimenti il vecchio
   renderer MapGrid trasformerebbe l'acqua in ostacolo visuale.

2. `v0.38j.02` - Metadati muri.
   Il muro occupa formalmente una cella 32x32, ma lo sprite e' alto 83 pixel.
   La base resta nella cella, mentre la parte superiore sale sopra. Il muro
   blocca movimento e visione, ma non diventa terrain.

3. `v0.38j.03` - Resolver muri cardinali.
   Il renderer deve scegliere la variante corretta del muro osservando i vicini
   cardinali. Esempi: linea orizzontale, linea verticale, angolo a L, T,
   incrocio a croce.

4. `v0.38j.04` - Rendering muri/oggetti alti.
   ArcGraph deve disegnare oggetti piu' alti di una cella con pivot coerente,
   sorting leggibile e base ancorata alla griglia. La semi-trasparenza quando
   un NPC passa dietro resta una fase successiva se richiede regole occlusive
   dedicate.

5. `v0.38j.05` - Contratto mini-tile base muri.
   Gli oggetti alti, in particolare i muri sottili, dichiarano quali quarti
   16x16 della cella base coprono. Il dato resta visuale e non cambia
   passabilita', occlusione o simulazione.

6. `v0.38j.06` - Resolver spritesheet muri.
   Il muro viene letto da una striscia unica alta 83 pixel, composta da 17 slot
   larghi 32 pixel. Le varianti cardinali non richiedono piu' file separati:
   ArcGraph produce chiavi `sheet#subSprite` risolte dal resolver scene-side.

7. `v0.38j.07` - Mini-tile pavimento.
   I muri sottili lasciano vedere porzioni di pavimento ai lati. Quando interno
   ed esterno hanno pavimenti diversi, alcuni casi richiederanno composizione
   16x16 o overlay dedicati.

## Esito v0.38j.02 - Metadati muri e ponte visuale oggetti

La `v0.38j.02` ha completato il passaggio preparatorio necessario per trattare
i muri come oggetti alti e non come semplici tile terrain.

Decisioni consolidate:

- il muro resta un oggetto, non un tile di terreno;
- il footprint logico del muro resta 1x1 cella;
- la base visuale dichiarata e' 32x32 pixel;
- lo sprite muro dichiarato e' alto 83 pixel;
- il muro blocca movimento e visione tramite `object_defs.json`;
- la sezione visuale dell'oggetto puo' ora esporre `VisualKind`,
  `ResolverKey`, dimensione sprite, dimensione base e pivot;
- ArcGraph propaga questi dati da `ObjectDef` fino a snapshot e render item,
  senza creare ancora un renderer oggetti/muri definitivo;
- la trasparenza dei muri quando un NPC passa dietro resta demandata alla fase
  di rendering oggetti alti.

Prossimo step:

```text
v0.38j.03 - Resolver muri cardinali
```

Il resolver dovra' scegliere quale variante del muro usare in base ai vicini
cardinali N/E/S/W, senza leggere dati globali in modo onnisciente e senza
trasformare il muro in terrain.

## Esito v0.38j.03 - Resolver muri cardinali

La `v0.38j.03` ha introdotto il resolver passivo per le varianti dei muri.

Regola implementata:

- vengono considerati solo oggetti con `VisualKind = wall`;
- i muri trasportati o con id non valido non partecipano al resolver;
- i muri vengono collegati solo ad altri muri della stessa famiglia visuale;
- la famiglia visuale deriva da `VisualResolverKey`, oppure da `DefId` se la
  chiave resolver non e' presente;
- la maschera cardinale usa l'ordine `N/E/S/W`;
- esempio: `1010` indica collegamento nord + sud;
- esempio: `0101` indica collegamento est + ovest;
- la sprite key finale diventa `spriteKeyBase_maschera`, per esempio
  `MapGrid/Sprites/Objects/wall_stone_1010`.

Confini preservati:

- nessun renderer Unity nuovo;
- nessun `Resources.Load`;
- nessun `GameObject`;
- nessuna modifica a scene o prefab;
- nessuna lettura diretta di `World` dal resolver;
- nessuna trasformazione del muro in terrain.

Il resolver viene agganciato alla build della render queue oggetti: ArcGraph
costruisce un indice temporaneo delle celle muro e poi risolve la variante
sprite di ogni muro prima che il piano scena consumi la queue.

Prossimo step:

```text
v0.38j.04 - Rendering muri/oggetti alti
```

Il renderer dovra' usare le sprite key gia' risolte e gestire pivot, altezza
83px, sorting e base cella.

## Esito v0.38j.04 - Rendering muri e oggetti alti

La `v0.38j.04` ha completato il primo passaggio tecnico per permettere ad
ArcGraph di rappresentare correttamente oggetti piu' alti della cella base,
come i muri 32x83.

Comportamento introdotto:

- `ArcGraphActorObjectSceneRenderEntry` conserva ora anche i metadati visuali
  oggetto: dimensione sprite, dimensione base, pivot, offset e flag visuali;
- il piano scena actor/object propaga questi dati dalla render queue fino alla
  entry consumabile dal renderer;
- gli oggetti normali restano centrati nella footprint logica;
- gli oggetti con pivot basso, per esempio `bottom_center`, vengono ancorati al
  bordo basso della cella invece che al centro;
- un muro 32x83 con base 32x32 cresce quindi verso l'alto senza slittare di
  mezza cella;
- gli offset visuali in pixel vengono convertiti in world units usando la base
  dichiarata dell'oggetto;
- il probe actor/object non applica piu' la scala placeholder agli oggetti che
  dichiarano metadati visuali reali, cosi' un muro alto resta leggibile alla
  scala prevista.

QA eseguita:

- `git diff --check` mirato sui file modificati: pulito;
- compilazione Roslyn data-only del sottoinsieme ArcGraph actor/object, render
  queue, wall resolver e harness: `exit=0`, `warning_count=0`;
- il check completo di tutto `ArcGraph/Runtime` non e' stato usato come
  criterio perche' l'`Assembly-CSharp.dll` locale risulta non aggiornato rispetto
  agli ultimi commit e genera falsi errori su tipi gia' presenti nel sorgente.

Confini preservati:

- nessun nuovo renderer produttivo oggetti;
- nessun asset load;
- nessuna scena o prefab modificati;
- nessuna lettura diretta di `World`;
- nessuna implementazione della trasparenza NPC dietro muro;
- nessuna modifica a MapGrid.

Prossimo step:

```text
v0.38j.05 - Mini-tile pavimento 16x16
```

Lo step successivo dovra' affrontare il problema dei muri sottili che lasciano
vedere porzioni di pavimento ai lati, specialmente quando pavimento interno ed
esterno sono diversi.

## Esito v0.38j.05 - Contratto mini-tile base muri

La `v0.38j.05` ha introdotto il contratto data-only per descrivere quali quarti 16x16 della cella base sono coperti da un oggetto alto, in particolare dai muri sottili.

Comportamento introdotto:

- `ObjectVisualDef` espone ora `BaseMiniTileMask` dentro `object_defs.json`;
- la maschera usa quattro cifre in ordine alto-sinistra, alto-destra, basso-sinistra, basso-destra;
- `1` significa porzione coperta dall'oggetto;
- `0` significa porzione in cui il pavimento resta visibile o dovra' essere composto dal renderer futuro;
- `wall_stone` dichiara `BaseMiniTileMask = 0110` come primo dato di test per muro sottile;
- `ArcGraphWorldAdapter` copia la maschera dalla definizione oggetto allo snapshot visuale;
- `ArcGraphObjectVisualSnapshot`, `ArcGraphObjectRenderItem` e `ArcGraphActorObjectSceneRenderEntry` propagano la maschera senza leggere il World e senza creare renderer;
- gli harness actor/object e wall cardinal verificano che la maschera arrivi fino al piano scena.

Confini preservati:

- nessun renderer pavimento 16x16 produttivo;
- nessuna mesh terrain modificata;
- nessun nuovo asset;
- nessun `GameObject`;
- nessuna scena o prefab modificati;
- nessuna modifica a MapGrid;
- nessuna logica simulativa di muro o pavimento introdotta.

Prossimo step:

```text
v0.38j.06 - Resolver spritesheet muri 17 slot
```

Lo step successivo deve prima adattare il resolver muri alla striscia PNG unica
32x83, evitando il modello precedente basato su file separati.

## Esito v0.38j.06 - Resolver spritesheet muri 17 slot

La `v0.38j.06` ha adattato il percorso visuale dei muri alla scelta grafica
definitiva per questa fase: una striscia PNG unica alta 83 pixel, con slot fissi
da 32 pixel.

Contratto grafico consolidato:

| Slot | Maschera `N/E/S/W` | Nome sub-sprite | Significato |
|---:|---|---|---|
| 0 | `0101` | `wall_stone_0101_0` | muro orizzontale variante 1 |
| 1 | `0101` | `wall_stone_0101_1` | muro orizzontale variante 2 |
| 2 | `1010` | `wall_stone_1010` | muro verticale |
| 3 | `1111` | `wall_stone_1111` | incrocio a croce |
| 4 | `1101` | `wall_stone_1101` | T sopra, sinistra, destra |
| 5 | `0111` | `wall_stone_0111` | T sotto, sinistra, destra |
| 6 | `1110` | `wall_stone_1110` | T sopra, sotto, destra |
| 7 | `1011` | `wall_stone_1011` | T sopra, sotto, sinistra |
| 8 | `0110` | `wall_stone_0110` | L sotto, destra |
| 9 | `0011` | `wall_stone_0011` | L sotto, sinistra |
| 10 | `1100` | `wall_stone_1100` | L sopra, destra |
| 11 | `1001` | `wall_stone_1001` | L sopra, sinistra |
| 12 | `0001` | `wall_stone_0001` | terminale verso sinistra |
| 13 | `0100` | `wall_stone_0100` | terminale verso destra |
| 14 | `0010` | `wall_stone_0010` | terminale verso sotto |
| 15 | `1000` | `wall_stone_1000` | terminale verso sopra |
| 16 | `0000` | `wall_stone_0000` | muro realmente isolato |

Nota terminologica:

```text
I casi "isolato a sinistra/destra/sotto/sopra" sono terminali con un solo
vicino. Il solo isolato vero e' la maschera 0000.
```

Comportamento introdotto:

- `ArcGraphWallCardinalResolver` produce chiavi nel formato `sheet#subSprite`;
- esempio: `MapGrid/Sprites/Objects/wall_stone#wall_stone_1010`;
- il resolver scene-side `ArcGraphSerializedSpriteResolver` supporta ora
  `Resources.LoadAll<Sprite>` per PNG sliced;
- il vecchio lookup `Resources.Load<Sprite>` resta funzionante per PNG singole;
- la variante orizzontale `0101` viene scelta con hash stabile della cella e
  dell'oggetto, senza random runtime;
- lo harness del resolver muri verifica il nuovo formato delle chiavi.

Confini preservati:

- nessuna scena modificata;
- nessun prefab modificato;
- nessun asset PNG aggiunto o modificato;
- nessuna modifica alla simulazione;
- nessuna modifica a collisioni, blocco movimento o blocco visione.

Prossimo step:

```text
v0.38j.07 - Renderer/overlay mini-tile pavimento 16x16
```

Lo step successivo potra' usare `BaseMiniTileMask` per decidere quali quarti di
pavimento comporre quando un muro sottile separa interno ed esterno con
pavimenti diversi.
## Esito v0.38a - ArcGraph Legacy Absorption Audit

La `v0.38a` ha auditato il perimetro legacy MapGrid che dovra' essere assorbito
o pensionato durante `v0.38`.

File principali auditati:

- `MapGridBootstrap`;
- `MapGridWorldView`;
- `MapGridChunkRenderer`;
- `MapGridCameraController`;
- `MapGridPointerInputActionsProvider`;
- `MapGridRuntimeDevToolsOverlay`;
- `MapGridRuntimeControlTopBar`;
- `MapGridLandmarkOverlay`;
- `MapGridFovHeatmapOverlay`;
- controparti ArcGraph gia' presenti: `ArcGraphWorldAdapter`,
  `ArcGraphTerrainChunkMeshBuilder`, `ArcGraphRenderQueueBuilder`,
  `ArcGraphViewController`, `ArcGraphDebugRuntimeMapGridAdapter`,
  `ArcGraphSceneProbeRenderer`, `ArcGraphDebugOverlaySceneProbeRenderer`.

Conclusione generale:

```text
ArcGraph ha gia' molti contratti e builder passivi.
Non ha ancora il wrapper produttivo di scena che sostituisce MapGrid.
```

Quindi `v0.38` non puo' iniziare cancellando `MapGridWorldView`.
Deve prima creare un percorso produttivo ArcGraph minimo, controllato e
comparabile.

### Classificazione legacy

| Area legacy | Stato rilevato | Destino consigliato |
|---|---|---|
| `MapGridBootstrap` | Costruisce config, layout, `MapGridData`, atlas, chunk terrain, camera, pointer provider e `MapGridWorldView` | Da sostituire gradualmente con bootstrap scena ArcGraph |
| `MapGridData` | Buffer view-side terreno/blocco, ancora necessario per terrain corrente | Sorgente temporanea per snapshot ArcGraph, poi da pensionare |
| `MapGridChunkRenderer` | Mesh chunk terreno con UV atlas e policy floor/wall/wall-top | Tecnica gia' quasi riassorbita da `ArcGraphTerrainChunkMeshBuilder` |
| `MapGridTileAtlas` | Risoluzione tileId -> UV legacy | Riusabile come concetto, non come dipendenza permanente obbligata |
| `MapGridCameraController` | Camera Unity concreta: zoom, pan, PixelPerfectCamera, input mouse | Non riassorbibile direttamente; serve wrapper Unity per `ArcGraphViewController` |
| `MapGridPointerInputActionsProvider` | Provider input puntatore via New Input System | Riusabile come ponte temporaneo o da replicare in wrapper ArcGraph |
| `MapGridWorldView` | Monolite view/debug/input: NPC, oggetti, overlay, UI, dev tools, rebind World | Non cancellare subito; smontare per parti |
| Sync NPC/oggetti | Crea `SpriteRenderer`, cache, label stock, collider, balloon, flash decisionale | Da sostituire dopo renderer actor/object produttivo ArcGraph |
| Overlay Landmark/GVD | Gia' migrato verso queue/debug ArcGraph e validato manualmente | Assorbibile nel debug ArcGraph minimo |
| FOV current cone | Calcolato ancora dentro `MapGridFovHeatmapOverlay` | Rinviare: richiede bridge dedicato, non e' pronto come producer ArcGraph |
| Label Landmark, DT numerico, pointer coords | Screen-space/UI o testo debug | Rinviare a canale UI/screen-space separato |
| Top bar e DevTools | Controlli runtime e strumenti che inviano comandi al core | Rinviare: non sono renderer, sono UI/dev tools |
| Summary cards / Explainability panel | UI diagnostica ricca | Fuori dal primo renderer produttivo ArcGraph |

### Stato ArcGraph rispetto al legacy

ArcGraph possiede gia':

- adapter read-only da `World` e `MapGridData`;
- layer terrain/object/actor/debug;
- render state e dirty state;
- builder mesh terrain passivo;
- queue actor/object passiva;
- motion snapshot NPC multi-tick;
- controller view passivo per zoom/pan/LOD;
- layer ambientali passivi;
- debug overlay queue;
- feed e adapter manuale MapGrid -> ArcGraph debug;
- probe visuali temporanei.

ArcGraph non possiede ancora:

- bootstrap scena produttivo;
- renderer terrain Unity permanente;
- renderer actor/object Unity permanente;
- pooling produttivo di sprite e GameObject;
- asset resolver sprite definitivo;
- wrapper Unity che trasformi input/camera in `ArcGraphViewInputFrame`;
- sistema UI/screen-space ArcGraph;
- dismissione reale dei componenti MapGrid.

### Decisione tecnica v0.38a

Il prossimo step non deve rimuovere nulla.

Il prossimo step deve progettare:

```text
v0.38b - ArcGraph Scene Bootstrap Replacement Plan
```

Scopo di `v0.38b`:

- definire quale GameObject/wrapper ArcGraph vive in scena;
- definire quali riferimenti riceve da Inspector;
- decidere come ottiene config, camera, map data temporanea e World;
- decidere come evita il doppio renderer permanente;
- decidere quale renderer viene acceso per primo;
- decidere cosa resta spento o legacy durante la transizione.

Stop progettuale:

```text
Non cancellare MapGridBootstrap.
Non cancellare MapGridWorldView.
Non salvare scene.
Non introdurre renderer produttivo prima del piano bootstrap.
```

## Esito v0.38b - ArcGraph Scene Bootstrap Replacement Plan

La `v0.38b` ha fissato il piano di sostituzione del bootstrap scena senza
introdurre codice runtime, renderer produttivi, asset load, modifiche scena o
rimozioni legacy.

La decisione principale e':

```text
ArcGraph deve diventare un consumer alternativo e poi sostitutivo della view.
Non deve diventare un figlio del renderer MapGrid.
```

### Risposta tecnica alle tre domande operative

1. Scene separate MapGrid / ArcGraph.

Sono possibili e utili come strategia di test, ma non devono diventare due
runtime simulativi separati. La scena MapGrid puo' restare scena produttiva
legacy, mentre una scena ArcGraph dedicata puo' servire per testare bootstrap,
terrain e gate visuali. Entrambe devono pero' leggere dati equivalenti tramite
context/snapshot o adapter dichiarati; non devono creare due simulazioni diverse.

2. Origine dei dati ArcGraph.

ArcGraph non deve ricevere dati dal renderer MapGrid come dipendenza
architetturale definitiva. Puo' usare temporaneamente strutture legacy come
`MapGridData` e `MapGridWorldView.RuntimeWorld`, ma solo tramite adapter
espliciti e read-only. La forma corretta resta:

```text
World / map data / context esplicito
-> snapshot read-only
-> ArcGraph
```

e non:

```text
MapGrid renderer
-> ArcGraph renderer
```

3. Eliminazione fisica MapGrid.

MapGrid potra' essere eliminato fisicamente solo dopo assorbimento verificato.
Non basta sostituire le tile terrain. Oggi `MapGridBootstrap` e
`MapGridWorldView` contengono ancora bootstrap, terrain chunk, camera/input,
sync NPC/oggetti, overlay debug, UI diagnostica, DevTools e rebind del `World`.

La cancellazione e' quindi ammessa solo dopo che ogni responsabilita' e' stata:

- migrata in ArcGraph;
- spostata in un sistema UI/debug separato;
- oppure dichiarata obsoleta e rimossa con gate esplicito.

### Piano bootstrap scena consigliato

Il primo wrapper scena ArcGraph dovra' essere un componente Unity leggero e
dichiarativo, con responsabilita' limitata:

- vivere su un GameObject esplicito, ad esempio `ArcGraphRuntimeRoot`;
- ricevere riferimenti da Inspector o da un factory/bootstrap autorizzato;
- costruire un `ArcGraphRuntimeContext`;
- inizializzare `ArcGraphBootstrapRuntime`;
- esporre diagnostica leggibile;
- restare spento, debug-only o gated finche' MapGrid resta renderer produttivo;
- non leggere `SimulationHost.Instance` direttamente;
- non usare `FindObjectOfType` come canale operativo stabile;
- non mutare `World`, `MapGridData` o sistemi core;
- non creare due renderer permanenti sovrapposti.

### Strategia scene

La transizione ammette due forme:

```text
Scene_MapGrid
-> scena produttiva legacy corrente
-> puo' ospitare wrapper/probe ArcGraph temporanei per gate manuali
```

```text
Scene_ArcGraphRuntime / Scene_ArcGraphProbe
-> scena separata futura di test ArcGraph
-> utile per validare terrain, camera e bootstrap senza salvare modifiche nella scena MapGrid
```

La scelta operativa per `v0.38c` dovra' decidere se il primo terrain bridge viene
testato dentro `Scene_MapGrid` come probe temporaneo o dentro una scena separata
di prova. In entrambi i casi, il renderer MapGrid non viene cancellato.

### Sequenza di assorbimento fissata

La sequenza v0.38 diventa:

1. `v0.38c`: progettare il ponte terrain scena ArcGraph.
2. Validare se il terrain ArcGraph puo' consumare `MapGridData` come snapshot
   temporaneo senza diventare dipendente dal renderer MapGrid.
3. Accendere il terrain solo in modalita' controllata, comparativa o scena di
   test.
4. Solo dopo terrain stabile, procedere con actor/object.
5. Solo dopo actor/object, procedere con debug minimo.
6. Solo dopo camera/input e strumenti UI separati, pianificare spegnimento
   completo MapGrid.
7. Solo dopo gate finale, cancellare fisicamente i componenti legacy assorbiti.

### Stop progettuali confermati

```text
Non cancellare MapGridBootstrap.
Non cancellare MapGridWorldView.
Non salvare scene Unity.
Non introdurre renderer produttivo permanente senza gate.
Non trasformare MapGridData nella mappa simulativa definitiva.
Non far dipendere ArcGraph dal renderer MapGrid come parent logico.
```

### Prossimo checkpoint

```text
v0.38c - ArcGraph Terrain Scene Bridge Plan
```

Scopo del prossimo step:

- decidere dove testare il primo terrain bridge;
- definire il wrapper o adapter terrain minimo;
- chiarire quali dati terrain passano ad ArcGraph;
- chiarire come viene impedito il doppio renderer permanente;
- preparare il gate visuale prima di qualunque sostituzione.

## Esito v0.38c - ArcGraph Terrain Scene Bridge Plan

La `v0.38c` ha auditato il percorso terrain gia' disponibile e ha fissato il
piano del primo ponte scena terrain ArcGraph.

File principali auditati:

- `ArcGraphTerrainLayer`;
- `ArcGraphTerrainChunkMeshBuilder`;
- `ArcGraphTerrainChunkMeshData`;
- `ArcGraphWorldAdapter`;
- `MapGridChunkRenderer`;
- `MapGridBootstrap`;
- `ArcGraphBootstrapRuntime`;
- `ArcGraphRuntimeContext`.

### Stato tecnico rilevato

ArcGraph possiede gia' la catena data-only:

```text
MapGridData
-> ArcGraphWorldAdapter.FillTerrainSnapshots(...)
-> ArcGraphTerrainLayer.ReplaceSnapshots(...)
-> ArcGraphTerrainChunkMeshBuilder.BuildDirtyChunks(...)
-> ArcGraphTerrainChunkMeshData
```

Questa catena produce dati mesh, ma non crea ancora oggetti Unity in scena.
Questo e' corretto: il terrain ArcGraph e' pronto come builder passivo, non come
renderer produttivo.

Il punto mancante non e' il calcolo mesh.
Il punto mancante e':

```text
come consegnare la MapGridData runtime ad ArcGraph
senza far dipendere ArcGraph dal renderer MapGrid
```

Oggi `MapGridBootstrap` costruisce e possiede la `MapGridData`, ma la conserva
come campo interno privato. `ArcGraphRuntimeContext` puo' gia' contenere una
`MapGridData`, ma manca un contratto scena esplicito che la passi ad ArcGraph in
sola lettura.

### Decisione: prima Scene_MapGrid, poi scena separata

Il primo test terrain ArcGraph deve avvenire dentro `Scene_MapGrid` come probe
temporaneo, spento o gated di default.

Motivazione:

- `Scene_MapGrid` possiede gia' config, layout, `MapGridData`, camera e World;
- consente di confrontare ArcGraph e MapGrid sullo stesso dato di partenza;
- evita di creare subito una seconda scena con bootstrap divergente;
- riduce il rischio di scambiare un problema di rendering per un problema di
  scenario o caricamento dati.

La scena separata ArcGraph resta una soluzione futura utile, ma solo dopo che il
contratto terrain sara' stabile e verificato.

### Regola sul doppio renderer

Il terrain ArcGraph non deve restare acceso permanentemente sopra MapGrid.

Le sole forme ammesse per il primo ponte sono:

- probe temporaneo manuale;
- modalita' comparativa gated;
- scena di test futura separata;
- nessuna attivazione automatica permanente.

In `v0.38c` non viene ancora introdotto alcun `MeshRenderer` ArcGraph produttivo.

### Micro-roadmap terrain dentro v0.38c

La fase terrain viene spezzata cosi':

1. `v0.38c.01 - ArcGraph Terrain Runtime Map Access Contract`
   - esporre o definire il modo minimo read-only per ottenere `MapGridData`;
   - costruire un `ArcGraphRuntimeContext` completo per terrain;
   - non disegnare ancora in scena.

2. `v0.38c.02 - ArcGraph Terrain Scene Probe`
   - consumare il context terrain;
   - costruire mesh data ArcGraph;
   - applicarle a un probe temporaneo gated;
   - non salvare scene.

3. `v0.38c.03 - ArcGraph Terrain Visual Gate`
   - confrontare output MapGrid e output ArcGraph;
   - verificare scala, posizione, UV, chunk, sorting e cleanup;
   - decidere se il terrain ArcGraph puo' diventare candidato produttivo.

Solo dopo questi tre passaggi ha senso procedere ad actor/object.

### Stop progettuali confermati

```text
Non passare direttamente ad actor/object.
Non cancellare MapGridChunkRenderer.
Non cancellare MapGridBootstrap.
Non salvare Scene_MapGrid.
Non introdurre renderer terrain permanente.
Non creare scena separata ArcGraph prima del contratto dati terrain.
```

### Prossimo checkpoint

```text
v0.38c.01 - ArcGraph Terrain Runtime Map Access Contract
```

Scopo del prossimo step:

- rendere disponibile a un adapter/wrapper ArcGraph la `MapGridData` runtime;
- mantenere accesso read-only e dichiarato;
- verificare che `ArcGraphRuntimeContext` possa contenere config + map + world;
- restare data-only, senza disegnare in Unity.

## Esito v0.38c.01 - ArcGraph Terrain Runtime Map Access Contract

La `v0.38c.01` ha introdotto il contratto minimo per consegnare ad ArcGraph la
mappa terrain runtime gia' costruita da MapGrid, senza far dipendere ArcGraph
dal renderer legacy e senza introdurre ancora un renderer terrain in scena.

### Modifiche introdotte

- `MapGridBootstrap` espone `RuntimeConfig`;
- `MapGridBootstrap` espone `RuntimeMap`;
- aggiunto `ArcGraphTerrainRuntimeMapGridAdapter`;
- aggiunta diagnostica `ArcGraphTerrainRuntimeMapGridAdapterDiagnostics`;
- aggiunto context menu manuale:

```text
ArcGraph/Probe Terrain Runtime Context From MapGrid
```

### Forma del contratto

Il passaggio dati ammesso ora e':

```text
MapGridBootstrap.RuntimeConfig
MapGridBootstrap.RuntimeMap
MapGridWorldView.RuntimeWorld opzionale
-> ArcGraphTerrainRuntimeMapGridAdapter
-> ArcGraphRuntimeContext(config, map, world)
-> ArcGraphBootstrapRuntime temporaneo
-> TerrainSnapshots
```

Questo contratto e' read-only dal punto di vista di ArcGraph: l'adapter legge i
riferimenti runtime gia' costruiti, ma non modifica `MapGridData`, non ricarica
JSON, non interroga provider globali e non entra nel `MapGridChunkRenderer`.

### Limiti confermati

La `v0.38c.01` non disegna nulla.

Non introduce:

- `MeshRenderer` ArcGraph;
- `MeshFilter` ArcGraph;
- GameObject di terrain;
- `Update`;
- hotkey;
- letture globali;
- salvataggi scena;
- modifiche a prefab o file `.meta`;
- sostituzione di `MapGridChunkRenderer`.

Il probe e' solo diagnostico: inizializza un `ArcGraphBootstrapRuntime`
temporaneo in memoria, misura quanti snapshot terrain sono stati prodotti e poi
rilascia lo stato.

### Prossimo checkpoint

```text
v0.38c.02 - ArcGraph Terrain Scene Probe
```

Scopo del prossimo step:

- consumare il context terrain gia' disponibile;
- costruire mesh data ArcGraph;
- applicarle a un probe scena temporaneo e gated;
- mantenere MapGrid come renderer produttivo;
- non salvare scene;
- non introdurre ancora un renderer terrain permanente.

## Esito v0.38c.02 - ArcGraph Terrain Scene Probe

La `v0.38c.02` ha introdotto il primo probe scena terrain ArcGraph capace di
trasformare la catena data-only gia' esistente in oggetti Unity temporanei.

Il probe non e' un renderer produttivo.
Serve solo a costruire il primo gate visuale tra MapGrid e ArcGraph.

### Modifiche introdotte

- aggiunto `ArcGraphTerrainSceneProbeRenderer`;
- aggiunta diagnostica `ArcGraphTerrainSceneProbeRendererDiagnostics`;
- aggiunto context menu manuale:

```text
ArcGraph/Render Terrain Scene Probe From MapGrid
```

- aggiunto context menu di cleanup:

```text
ArcGraph/Clear Terrain Scene Probe
```

### Flusso operativo

Il percorso visuale temporaneo diventa:

```text
ArcGraphTerrainRuntimeMapGridAdapter
-> ArcGraphRuntimeContext(config, map, world opzionale)
-> ArcGraphBootstrapRuntime temporaneo
-> ArcGraphTerrainLayer
-> ArcGraphTerrainChunkMeshBuilder
-> ArcGraphTerrainChunkMeshData
-> ArcGraphTerrainSceneProbeRoot
-> MeshFilter + MeshRenderer temporanei
```

### Gate di sicurezza

Il renderer probe usa `ArcGraphComparisonGate` con:

```text
ArcGraphComparisonMode.TemporaryDebugSceneProbe
```

Il probe viene bloccato se mancano:

- adapter runtime;
- config;
- mappa runtime;
- camera scena;
- materiale terrain.

Il materiale terrain deve essere assegnato da Inspector.
Questa scelta evita che ArcGraph carichi asset tramite `Resources.Load` o legga
direttamente il renderer MapGrid per recuperare il materiale.

### Limiti confermati

La `v0.38c.02` non introduce:

- renderer terrain permanente;
- salvataggi scena;
- prefab;
- asset load;
- hotkey;
- `Update`;
- letture globali;
- modifiche a `World`;
- modifiche a `MapGridData`;
- cancellazione di `MapGridChunkRenderer`.

I GameObject creati dal probe sono figli di:

```text
ArcGraphTerrainSceneProbeRoot
```

e vengono rimossi dal comando:

```text
ArcGraph/Clear Terrain Scene Probe
```

### Prossimo checkpoint

```text
v0.38c.03 - ArcGraph Terrain Visual Gate
```

Scopo del prossimo step:

- guidare il test manuale in Unity;
- verificare che il probe venga renderizzato;
- verificare scala e posizione rispetto a MapGrid;
- verificare che le UV terrain siano coerenti;
- verificare che il cleanup rimuova il root temporaneo;
- decidere se il terrain ArcGraph puo' diventare candidato produttivo.

## Esito v0.38c.03 - ArcGraph Terrain Visual Gate

La `v0.38c.03` ha chiuso il gate visuale umano del terrain ArcGraph.

L'operatore ha eseguito il test in Unity e ha confermato:

```text
funziona
```

Questa conferma rende valido il primo ponte terrain controllato:

```text
MapGrid runtime
-> ArcGraphRuntimeContext
-> ArcGraphTerrainLayer
-> ArcGraphTerrainChunkMeshBuilder
-> ArcGraphTerrainSceneProbeRenderer
```

### Esito tecnico

- il probe terrain ArcGraph viene considerato visualmente funzionante;
- il context MapGrid -> ArcGraph e' sufficiente per alimentare il terrain;
- il renderer temporaneo e gated e' valido per test comparativi;
- il cleanup del probe resta parte del flusso manuale;
- il terrain ArcGraph puo' diventare candidato per il successivo percorso
  produttivo controllato.

### Limiti ancora attivi

Il superamento del gate terrain non chiude ancora `v0.38`.

MapGrid resta renderer principale perche' devono ancora essere assorbiti:

- actor/object runtime;
- movimento visuale multi-tick;
- debug minimo validato;
- camera/input;
- UI e dev tools non renderer;
- piano di pensionamento componenti legacy.

### Prossimo checkpoint

```text
v0.38d - ArcGraph Actor/Object Runtime Bridge Audit
```

Scopo del prossimo step:

- auditare la gestione attuale di NPC e oggetti in `MapGridWorldView`;
- verificare quali snapshot actor/object ArcGraph possiede gia';
- capire come collegare actor/object runtime senza duplicare renderer permanenti;
- mantenere MapGridWorldView attivo finche' actor/object ArcGraph non sono
  verificati.

## Esito v0.38d - ArcGraph Actor/Object Runtime Bridge Audit

La `v0.38d` ha auditato il percorso necessario per portare actor e oggetti
runtime dentro ArcGraph dopo il gate terrain riuscito.

L'esito principale e':

```text
ArcGraph possiede gia' i dati actor/object fino alla render queue.
Manca il wrapper scena Unity che consumi la queue e disegni sprite reali.
```

### File principali auditati

- `MapGridWorldView`;
- `ArcGraphWorldAdapter`;
- `ArcGraphRuntimeContext`;
- `ArcGraphBootstrapRuntime`;
- `ArcGraphActorVisualSnapshot`;
- `ArcGraphObjectVisualSnapshot`;
- `ArcGraphActorLayer`;
- `ArcGraphObjectLayer`;
- `ArcGraphActorRenderQueueBuilder`;
- `ArcGraphObjectRenderQueueBuilder`;
- `ArcGraphRenderQueueBuilder`;
- `ArcGraphRenderQueue`;
- `ArcGraphSceneProbeRenderer`;
- `ArcGraphTerrainRuntimeMapGridAdapter`.

### Stato tecnico rilevato

Il vecchio `MapGridWorldView` gestisce insieme molte responsabilita':

- crea `SpriteRenderer` per NPC e oggetti;
- carica sprite via `Resources.Load`;
- conserva cache sprite;
- posiziona NPC e oggetti al centro cella;
- gestisce sorting order;
- nasconde oggetti trasportati;
- disegna label degli stock alimentari;
- aggiunge collider e handle agli NPC;
- aggiunge balloon sugli NPC;
- applica flash decisionale;
- gestisce input/debug, selezione, click-to-move e overlay diagnostici.

Questa classe non puo' essere copiata dentro ArcGraph come renderer definitivo,
perche' mescola rendering, input, diagnostica, UI e lettura diretta del `World`.

ArcGraph, invece, possiede gia' una catena passiva:

```text
World
-> ArcGraphWorldAdapter
-> ArcGraphObjectVisualSnapshot / ArcGraphActorVisualSnapshot
-> ArcGraphObjectLayer / ArcGraphActorLayer
-> ArcGraphObjectRenderQueueBuilder / ArcGraphActorRenderQueueBuilder
-> ArcGraphRenderQueueBuilder
-> ArcGraphRenderQueue
```

Questa catena:

- legge il `World` solo tramite adapter read-only;
- copia snapshot;
- include gli oggetti non trasportati;
- include gli stock alimentari come dato visuale;
- assegna sprite key a oggetti e NPC;
- legge il movimento NPC multi-tick tipizzato dal `JobRuntimeState`;
- produce posizione visuale frazionaria degli actor;
- produce una queue globale ordinata actor/object;
- non crea `GameObject`;
- non carica asset;
- non modifica `World`, job, movimento o inventario.

### Punto mancante

Il punto mancante non e' il modello dati.

Il punto mancante e':

```text
ArcGraph actor/object scene renderer
```

Questo componente futuro deve:

- ricevere un `ArcGraphRuntimeContext` esplicito;
- inizializzare o aggiornare un `ArcGraphBootstrapRuntime`;
- leggere `ArcGraphActorLayer` e `ArcGraphObjectLayer`;
- costruire una `ArcGraphRenderQueue`;
- creare o aggiornare sprite scene-side;
- usare un root temporaneo o dedicato;
- mantenere pooling e cleanup confinati;
- restare gated/spento/manuale finche' MapGrid resta produttivo.

### Scelta progettuale emersa

La scelta da non fare in modo cieco riguarda la risoluzione delle sprite.

Il vecchio renderer usa:

```text
Resources.Load<Sprite>(spriteKey)
```

ArcGraph core finora ha evitato asset load.

Per mantenere la separazione, la soluzione consigliata e':

```text
core ArcGraph
-> produce sprite key

wrapper scena ArcGraph
-> risolve sprite key in Sprite
```

Questa risoluzione deve restare lato scena o lato asset resolver, non dentro i
builder passivi. In questo modo ArcGraph continua a preparare dati visuali, ma
non diventa un sistema onnisciente che cerca asset o interroga la scena.

### Perimetro del primo bridge actor/object

Il primo bridge deve disegnare solo:

- NPC come sprite singola;
- oggetti come sprite singola;
- posizione actor interpolata se esiste motion snapshot;
- oggetti trasportati nascosti;
- ordinamento stabile actor/object;
- diagnostica minima in console.

Non deve ancora migrare:

- balloon NPC;
- stock label;
- collider hover;
- click-to-move;
- selezione NPC;
- summary card;
- decision flash;
- overlay FOV;
- Landmark/GVD debug;
- top bar e DevTools;
- UI screen-space.

Questi elementi non sono puro rendering base: vanno portati in step separati,
altrimenti il nuovo renderer rischia di ricostruire il monolite
`MapGridWorldView`.

### Rischi principali

1. Copiare `MapGridWorldView` dentro ArcGraph.

   Rischio alto. Porterebbe dentro ArcGraph input, debug, UI e dipendenze dal
   `World`, annullando la separazione ottenuta finora.

2. Mettere `Resources.Load` nei builder ArcGraph.

   Rischio medio/alto. I builder diventerebbero dipendenti dagli asset Unity e
   non sarebbero piu' contratti passivi testabili.

3. Disegnare actor/object sopra MapGrid in modo permanente.

   Rischio medio. Per ora e' ammesso solo un probe temporaneo o gated, per
   evitare doppio renderer produttivo.

4. Migrare label, collider e UI insieme agli sprite.

   Rischio medio. Allarga troppo il perimetro e rende difficile capire se il
   bridge base funziona.

5. Ignorare la motion queue gia' esistente.

   Rischio basso ma concreto. La posizione frazionaria degli actor e' gia'
   preparata da `ArcGraphActorRenderItem`; il renderer scena deve usarla, non
   ricalcolarla dal `World`.

### Micro-roadmap consigliata dopo audit

1. `v0.38d.01 - ArcGraph Actor/Object Scene Renderer Contract`
   - definire il contratto del renderer scena actor/object;
   - definire asset resolver temporaneo;
   - definire root, pooling, cleanup, diagnostica e gate;
   - nessun rendering produttivo.

2. `v0.38d.02 - ArcGraph Actor/Object Scene Probe`
   - implementare probe temporaneo actor/object;
   - consumare `ArcGraphRenderQueue`;
   - disegnare sprite reali o fallback controllati;
   - non salvare scena.

3. `v0.38d.03 - ArcGraph Actor/Object Visual Gate`
   - test manuale in Unity;
   - verifica posizione, sorting, sprite, oggetti nascosti e movimento;
   - confronto con MapGrid.

4. `v0.38d.04 - Actor/Object Bridge Closeout`
   - documentare esito;
   - decidere se actor/object ArcGraph possono diventare candidati produttivi;
   - non cancellare ancora `MapGridWorldView`.

### Stop progettuali confermati

```text
Non cancellare MapGridWorldView.
Non salvare Scene_MapGrid.
Non creare renderer actor/object permanente.
Non migrare input, UI o DevTools nel primo bridge.
Non mettere asset load nei builder passivi.
Non far leggere il World direttamente al renderer scena.
```

### Prossimo checkpoint

```text
v0.38d.01 - ArcGraph Actor/Object Scene Renderer Contract
```

Scopo del prossimo step:

- progettare il componente scena che consumera' `ArcGraphRenderQueue`;
- stabilire dove risolvere le sprite;
- stabilire come usare posizione frazionaria actor;
- stabilire come fare pooling/cleanup senza scena salvata;
- preparare il probe actor/object senza ancora implementare sostituzione
  produttiva.

## Esito v0.38d.01 - ArcGraph Actor/Object Scene Renderer Contract

La `v0.38d.01` ha introdotto il contratto passivo del futuro renderer scena
actor/object ArcGraph.

Non e' stato ancora creato alcun renderer Unity produttivo.
Non e' stato creato alcun `GameObject`.
Non e' stato introdotto asset load.
Non e' stata salvata alcuna scena.

### Modifiche introdotte

- aggiunto `ArcGraphSpriteResolveRequest`;
- aggiunto `IArcGraphSpriteResolver`;
- aggiunto `ArcGraphActorObjectSceneRendererContract`;
- aggiunto `ArcGraphActorObjectSceneRendererDiagnostics`;
- aggiunto `ArcGraphActorObjectSceneRenderEntry`;
- aggiunto `ArcGraphActorObjectSceneRenderPlan`;
- aggiunto `ArcGraphActorObjectSceneRenderPlanBuilder`;
- aggiunto `ArcGraphActorObjectSceneRendererContractHarness`.

### Forma del contratto

Il nuovo percorso passivo e':

```text
ArcGraphRenderQueue
-> ArcGraphActorObjectSceneRendererContract
-> ArcGraphActorObjectSceneRenderPlanBuilder
-> ArcGraphActorObjectSceneRenderPlan
-> ArcGraphActorObjectSceneRenderEntry
```

Il plan contiene:

- tipo item actor/object;
- id entita';
- cella discreta;
- posizione mondo gia' scalata;
- sorting order;
- richiesta sprite;
- stato movimento actor;
- progresso movimento actor.

Questo permette di testare il ponte scene-side prima di introdurre il
`MonoBehaviour` che creera' materialmente gli sprite in Unity.

### Decisioni tecniche fissate

1. La sprite key resta un dato.

   ArcGraph core produce `SpriteKey`, ma non carica asset.

2. La risoluzione asset vive in un resolver scene-side.

   Il contratto `IArcGraphSpriteResolver` potra' essere implementato nel wrapper
   futuro, ma non nei builder passivi.

3. Il sorting order deriva dall'ordine della `ArcGraphRenderQueue`.

   Il renderer scena non deve rileggere `World` o ricalcolare il sorting dal
   legacy `MapGridWorldView`.

4. Gli actor usano la posa visuale gia' interpolata.

   Il piano scena usa `VisualX` e `VisualY` prodotti da
   `ArcGraphActorRenderItem`. Non ricalcola movimento dal job runtime.

5. Il root resta temporaneo e confinato.

   Il contratto default usa:

```text
ArcGraphActorObjectSceneProbeRoot
```

6. Input e UI restano esclusi.

   Il contratto blocca migrazione di input, UI, lettura diretta del `World` e
   asset load nei builder.

### QA eseguita

Eseguita compilazione isolata dei nuovi file con:

```text
dotnet csc
netstandard2.1 refs
Assembly-CSharp.dll
UnityEngine.CoreModule.dll
```

Esito:

```text
compilazione riuscita
```

Controlli statici:

- nessun `new GameObject`;
- nessun `AddComponent`;
- nessun `Resources.Load` eseguibile;
- nessun `FindObjectOfType`;
- nessuna lettura `SimulationHost.Instance`;
- nessuna modifica a `World`;
- nessuna modifica a `MapGridWorldView`;
- nessuna modifica scena;
- nessun file `.meta` tracciato.

### Limiti confermati

La `v0.38d.01` non disegna ancora nulla in Unity.

Il renderer scena reale resta da introdurre nel prossimo micro-step come probe
temporaneo e gated.

### Prossimo checkpoint

```text
v0.38d.02 - ArcGraph Actor/Object Scene Probe
```

Scopo del prossimo step:

- creare un probe `MonoBehaviour` temporaneo;
- consumare `ArcGraphRenderQueue`;
- costruire `ArcGraphActorObjectSceneRenderPlan`;
- risolvere sprite tramite resolver scene-side;
- creare `SpriteRenderer` solo sotto root temporaneo;
- aggiungere context menu manuale;
- non salvare scena;
- non migrare input, UI, label, collider, balloon o DevTools.

## Esito v0.38d.02 - ArcGraph Actor/Object Scene Probe

La `v0.38d.02` ha introdotto il primo probe scena actor/object ArcGraph.

Il probe e' temporaneo, manuale e confinato.
Non sostituisce `MapGridWorldView`.
Non salva scena.
Non migra input, UI, collider, balloon, stock label o DevTools.

### Modifiche introdotte

- aggiunto `ArcGraphSerializedSpriteResolver`;
- aggiunta entry serializzabile `ArcGraphSerializedSpriteResolverEntry`;
- aggiunto `ArcGraphActorObjectSceneProbeRenderer`;
- aggiunta diagnostica `ArcGraphActorObjectSceneProbeRendererDiagnostics`;
- aggiunto context menu manuale:

```text
ArcGraph/Render Actor Object Scene Probe From MapGrid
```

- aggiunto context menu cleanup:

```text
ArcGraph/Clear Actor Object Scene Probe
```

### Flusso operativo

Il percorso visuale temporaneo diventa:

```text
ArcGraphTerrainRuntimeMapGridAdapter
-> ArcGraphRuntimeContext(config, map, world)
-> ArcGraphBootstrapRuntime temporaneo
-> ArcGraphActorLayer / ArcGraphObjectLayer
-> ArcGraphRenderQueueBuilder
-> ArcGraphActorObjectSceneRenderPlanBuilder
-> ArcGraphActorObjectSceneProbeRoot
-> SpriteRenderer temporanei
```

### Resolver sprite

Il resolver introdotto e':

```text
ArcGraphSerializedSpriteResolver
```

Funziona tramite mapping assegnati da Inspector:

```text
sprite key -> Sprite
```

Non chiama `Resources.Load`.
Non cerca asset globalmente.
Non legge il `World`.

Se il resolver non viene assegnato o non trova una sprite, il probe puo' usare
fallback generati 1x1 colorati, solo per rendere visibile il gate:

- actor: colore magenta;
- object: colore arancione.

### Root temporaneo

Gli oggetti vengono creati solo sotto:

```text
ArcGraphActorObjectSceneProbeRoot
```

Il cleanup agisce solo su questo root.

### QA eseguita

Eseguita compilazione isolata dei file actor/object scene probe con:

```text
dotnet csc
netstandard2.1 refs
Assembly-CSharp.dll
UnityEngine.CoreModule.dll
```

Esito:

```text
compilazione riuscita
```

Warning residui:

- campi `SerializeField` non assegnati da codice.

Questi warning sono attesi per componenti Unity configurabili da Inspector.

### Limiti confermati

Il probe non e' ancora validato visualmente dall'operatore.

La `v0.38d.02` non abilita renderer permanente.
La `v0.38d.02` non cancella MapGrid.
La `v0.38d.02` non crea una scena ArcGraph separata.

### Prossimo checkpoint

```text
v0.38d.03 - ArcGraph Actor/Object Visual Gate
```

Scopo del prossimo step:

- guidare il test manuale in Unity;
- verificare che il probe actor/object riceva il `World`;
- verificare che il root temporaneo venga creato;
- verificare actor e oggetti visibili;
- verificare sorting object/actor;
- verificare interpolazione actor durante movimento;
- verificare cleanup del root;
- decidere se actor/object ArcGraph possono diventare candidati produttivi.

## Stato v0.38d.03 - ArcGraph Actor/Object Visual Gate congelato

La `v0.38d.03` resta aperta come gate visuale manuale congelato.

L'operatore non puo' eseguire ora i test in Unity.
Il gate viene quindi sospeso, non fallito.

Questa distinzione e' importante:

- il probe actor/object esiste;
- la compilazione tecnica della `v0.38d.02` e' riuscita;
- il test visuale umano non e' ancora stato eseguito;
- actor/object ArcGraph non sono ancora promossi a candidato produttivo;
- MapGrid resta renderer principale;
- non e' autorizzata nessuna cancellazione legacy basata su questo gate.

### Verifiche congelate

Quando l'operatore potra' riprendere i test visuali, la `v0.38d.03` dovra'
verificare:

- presenza del componente probe in scena;
- corretto collegamento con `ArcGraphTerrainRuntimeMapGridAdapter`;
- presenza del `World` nel context runtime;
- creazione del root temporaneo `ArcGraphActorObjectSceneProbeRoot`;
- visibilita' di NPC e oggetti;
- sorting coerente tra actor e object;
- movimento actor leggibile durante transizione multi-tick;
- cleanup del root temporaneo tramite context menu.

### Regola di prosecuzione

Durante il congelamento della `v0.38d.03` e' ammesso procedere solo su step
che non dipendono dalla validazione visuale actor/object.

Non e' ammesso:

- dichiarare chiuso il pensionamento MapGrid actor/object;
- eliminare componenti legacy;
- sostituire il renderer produttivo;
- promuovere il probe actor/object a renderer stabile.

### Prossimo checkpoint

```text
v0.38e - ArcGraph Debug Minimum Absorption Audit
```

Scopo del prossimo step:

- auditare la catena debug minima gia' validata nella `v0.37`;
- distinguere debug gia' migrato, debug ancora legacy e debug da escludere dal primo ArcGraph produttivo;
- verificare se serve un ulteriore bridge o solo documentare il perimetro minimo;
- non richiedere nuovi test visuali manuali in questa fase congelata.

## Esito v0.38e - ArcGraph Debug Minimum Absorption Audit

La `v0.38e` ha auditato il perimetro debug minimo assorbibile da ArcGraph senza
richiedere nuovi test visuali manuali.

Il punto di partenza e' la catena gia' validata nella `v0.37`:

```text
MapGridWorldView
-> ArcGraphDebugRuntimeMapGridAdapter
-> ArcGraphDebugRuntimeSceneWrapper
-> ArcGraphDebugRuntimeWiringCoordinator
-> ArcGraphDebugOverlayRuntimeFeed
-> ArcGraphDebugOverlayQueue
-> ArcGraphDebugOverlaySceneProbeRenderer
```

Questa catena resta manuale, esplicita e read-only.
Non e' un renderer produttivo e non sostituisce MapGrid.

### Debug gia' assorbito nel perimetro minimo

Il perimetro debug minimo gia' disponibile in ArcGraph comprende:

- Landmark world nodes;
- Landmark known nodes;
- Landmark route nodes;
- Landmark world edges;
- Landmark known edges;
- Landmark route edges;
- path edges Landmark/Direct/Jump/Complex;
- celle DT heatmap;
- celle GVD raw;
- nodi GVD;
- edge GVD.

Questi dati arrivano tramite:

```text
World.GetNpcLandmarkOverlayData(...)
World.GetGvdDinOverlayData(...)
```

Il feed ArcGraph li legge solo come dati debug gia' prodotti dal Core.
Non calcola pathfinding.
Non calcola percezione.
Non modifica il World.
Non sceglie autonomamente l'NPC attivo.

### Contratti ArcGraph coinvolti

I file principali del perimetro sono:

- `ArcGraphDebugOverlayKind`;
- `ArcGraphDebugOverlaySnapshot`;
- `ArcGraphDebugOverlayProducerBridge`;
- `ArcGraphDebugOverlayQueueBuilder`;
- `ArcGraphDebugOverlayRuntimeFeed`;
- `ArcGraphDebugRuntimeWiringFrame`;
- `ArcGraphDebugRuntimeWiringCoordinator`;
- `ArcGraphDebugRuntimeSceneWrapper`;
- `ArcGraphDebugRuntimeMapGridAdapter`;
- `ArcGraphDebugOverlaySceneProbeRenderer`.

Il disegno temporaneo resta confinato al probe:

```text
ArcGraphDebugOverlaySceneProbeRoot
```

### Debug ancora legacy

Restano dentro MapGrid o fuori dal perimetro minimo ArcGraph:

- FOV current cone;
- FOV historical heatmap;
- pointer cell coordinates HUD;
- runtime cost HUD;
- Landmark labels screen-space;
- DT value labels;
- summary cards;
- top bar runtime;
- DevTools;
- click-to-move debug;
- selection UX;
- tooltip e pannelli interattivi.

Questi elementi non sono equivalenti al debug minimo gia' migrato.
Molti di essi sono UI, input o strumenti operativi, non semplici overlay mappa.

### Decisione tecnica v0.38e

La `v0.38e` considera assorbito solo il debug minimo Landmark/GVD/DT.

Non viene aggiunto un nuovo bridge FOV in questa fase.
Il FOV e gli HUD richiedono un audit separato perche':

- leggono stato di selezione/puntatore;
- dipendono da input e camera;
- producono output screen-space;
- possono richiedere policy UI diversa dal renderer mappa;
- non devono entrare nel renderer ArcGraph come responsabilita' nascosta.

### Vincoli confermati

- ArcGraph debug non deve leggere `SimulationHost.Instance`.
- ArcGraph debug non deve usare `FindObjectOfType` per recuperare il mondo.
- ArcGraph debug non deve dipendere da `MapGridWorldProvider`.
- Il wrapper scena non deve scegliere autonomamente l'NPC.
- Il renderer probe deve consumare solo queue gia' prodotte.
- Nessun DevTools deve essere inglobato nel renderer ArcGraph.

### Esito

```text
v0.38e = COMPLETATA COME AUDIT DI ASSORBIMENTO DEBUG MINIMO
```

Il debug minimo da considerare assorbito per la futura chiusura `v0.38` e':

```text
Landmark + GVD-DIN + DT heatmap
```

Il resto va trattato nel checkpoint successivo come separazione di strumenti
interattivi e UI debug, non come semplice estensione del renderer.

### Prossimo checkpoint

```text
v0.38f - Separazione strumenti interattivi/dev tools dal renderer legacy
```

Scopo del prossimo step:

- auditare top bar, DevTools, click-to-move, pointer HUD, summary cards e selection;
- distinguere cosa deve restare UI/tool separato da cosa puo' diventare overlay ArcGraph;
- impedire che ArcGraph diventi un contenitore di comandi e strumenti operativi;
- preparare il futuro pensionamento MapGrid senza perdere strumenti utili.

## Esito v0.38f - Separazione strumenti interattivi/dev tools dal renderer legacy

La `v0.38f` ha auditato gli strumenti interattivi e debug operativi che oggi
vivono dentro o attorno a `MapGridWorldView`.

L'obiettivo non era spostarli subito in ArcGraph, ma classificarli.
Il risultato dell'audit e' netto: molti di questi elementi non sono rendering.
Sono UI, input, selezione, strumenti operativi o comandi debug.

### File principali auditati

- `MapGridWorldView`;
- `MapGridRuntimeControlTopBar`;
- `MapGridRuntimeDevToolsOverlay`;
- `MapGridPointerCoordsOverlay`;
- `MapGridEntitySummaryOverlay`;
- `MapGridRuntimeDebugAudioFeedback`;
- `MapGridNpcViewHandle`;
- `NPCSelection`.

### Classificazione funzionale

| Blocco | Natura reale | Destinazione corretta |
|---|---|---|
| Terrain legacy | Rendering mappa | ArcGraph terrain |
| NPC/object sprite | Rendering entita' | ArcGraph actor/object |
| FOV heatmap | Overlay diagnostico mappa | Futuro overlay ArcGraph, separato dal renderer principale |
| Landmark/GVD/DT | Overlay diagnostico mappa | Gia' coperto dal debug minimo ArcGraph |
| Landmark labels | UI screen-space | Canale UI separato, non mesh renderer |
| Pointer cell coords | HUD screen-space | UI/debug HUD separato |
| Runtime cost HUD | HUD diagnostico | UI/debug HUD separato |
| Summary cards | UI diagnostica complessa | Modulo observer/debug separato |
| Movement/MBQD panel | Explainability UI | Modulo observer/debug separato |
| Top bar | Controllo runtime | Tool operativo separato |
| DevTools overlay | Editor/debug operativo | Tool operativo separato |
| Click-to-move | Comando debug verso job system | Tool operativo separato |
| Selection NPC | Stato view-side condiviso | Servizio selection separato dal renderer |
| Debug audio feedback | Presentazione/feedback audio | Modulo feedback separato |

### Responsabilita' oggi concentrate in MapGridWorldView

`MapGridWorldView` oggi contiene o coordina:

- sync NPC e oggetti;
- cache sprite;
- stock label;
- balloon;
- flash decisionale;
- collider/handle NPC;
- selection tramite click;
- click-to-move debug;
- toggle FOV;
- toggle landmark/GVD/DT;
- pointer coordinates HUD;
- summary overlay;
- top bar runtime;
- DevTools;
- audio feedback;
- rebind al `World` dopo load.

Questo conferma che `MapGridWorldView` non puo' essere cancellato solo perche'
ArcGraph disegna terrain o actor/object.
Prima bisogna separare gli strumenti non-renderer.

### Top bar

`MapGridRuntimeControlTopBar` e' una UI operativa.

Invia o attiva:

- toggle DevTools/spawn;
- pausa/riprendi;
- step singolo;
- step multiplo;
- toggle FOV;
- pin FOV.

La top bar usa `SimulationHost.Instance` per pausa e step.
Questo e' accettabile come tool debug/runtime, ma non deve entrare nel renderer
ArcGraph.

Destinazione corretta:

```text
RuntimeControlUI / DebugControlPanel
```

non:

```text
ArcGraphRenderer
```

### DevTools

`MapGridRuntimeDevToolsOverlay` e' un editor/debug operativo runtime.

Gestisce:

- placement oggetti;
- erase oggetti;
- spawn NPC;
- orientamento NPC;
- erase NPC;
- piazzamento porte;
- piazzamento cibo;
- save/load dev map;
- save/load world snapshot;
- assegnazione forzata transport object job;
- comandi esterni verso `SimulationHost`.

Usa:

- `SimulationHost.Instance`;
- `MapGridWorldProvider.TryGetWorld()`;
- input tastiera/mouse;
- IMGUI;
- camera;
- provider puntatore;
- comandi `ICommand`.

Quindi non e' un renderer e non deve essere assorbito da ArcGraph.

Destinazione corretta:

```text
DevTools runtime separato con adapter mappa
```

L'adapter mappa potra' cambiare da MapGrid ad ArcGraph, ma il tool resta tool.

### Selection

`NPCSelection` e' gia' un servizio separato e condiviso.

Aspetto positivo:

- non dipende da MapGrid;
- espone `SelectedNpcId`;
- notifica tramite `OnSelectionChanged`;
- viene usato anche da AtomViewer.

Aspetto da migliorare in futuro:

- il picking dell'NPC e' ancora dentro `MapGridWorldView`;
- la selezione tramite click dipende dal renderer legacy;
- ArcGraph dovra' avere un proprio adapter di picking che chiama lo stesso
  servizio `NPCSelection`.

Decisione:

```text
NPCSelection resta.
Il picking va separato dal renderer.
```

### Click-to-move

Il click-to-move debug oggi vive dentro `MapGridWorldView`.

Fa due cose diverse:

- interpreta input mappa;
- chiama `SimulationHost.ForceAssignMoveToCellJobFromDevTools(...)`.

Questo non e' rendering.
E' un tool operativo che genera un job debug.

Destinazione corretta:

```text
DebugCommandTool / ClickMoveTool
```

con input/picking forniti da un adapter visuale.

### Summary cards e explainability

`MapGridEntitySummaryOverlay` e' UI diagnostica complessa.

Contiene:

- card NPC;
- card oggetti;
- drag delle card;
- linee UI;
- dati memoria;
- dati comunicazione;
- dati inventory;
- dati action/job;
- explainability movement;
- explainability MBQD;
- refresh testuale throttled.

Non deve essere trasformato in overlay ArcGraph.
Deve diventare un modulo observer/debug separato, alimentato da:

- `World`;
- `NPCSelection`;
- camera/coordinate mapping.

ArcGraph puo' solo fornire coordinate/anchor, non possedere la logica della card.

### Pointer HUD e runtime cost

`MapGridPointerCoordsOverlay` e' HUD screen-space.

Mostra:

- coordinate cella;
- stato in/out bounds;
- costi runtime se `RuntimeCostObserver` e' attivo;
- stato percettivo dell'NPC selezionato.

Non e' rendering mappa.
E' HUD diagnostico.

Destinazione corretta:

```text
DebugHud separato
```

che riceve coordinate cella da un adapter mappa.

### Debug audio

`MapGridRuntimeDebugAudioFeedback` e' presentazione audio debug.

Legge:

- NPC selezionato;
- movimento;
- decision flash;
- stato job;
- job failure.

Non produce comandi e non muta il mondo.
Non e' renderer.

Destinazione corretta:

```text
DebugFeedback separato
```

### Decisione tecnica v0.38f

ArcGraph deve assorbire il rendering, non gli strumenti.

La separazione corretta e':

```text
Simulation/World
-> snapshot/render queue
-> ArcGraph renderer

Simulation/World + Selection + Input Adapter
-> Debug/Observer/UI tools

Debug/Observer/UI tools
-> opzionalmente commands verso SimulationHost
```

ArcGraph puo' fornire:

- conversione coordinate schermo/cella;
- anchor visuali;
- dati di picking;
- eventi view-side come "cella cliccata" o "actor cliccato".

ArcGraph non deve possedere:

- DevTools;
- top bar;
- save/load;
- spawn;
- forced job assignment;
- summary cards;
- explainability panels;
- audio debug;
- policy di selezione globale.

### Implicazione sulla v0.38g

La `v0.38g`, cioe' il pensionamento controllato dei componenti MapGrid assorbiti,
non puo' partire in modo completo ora.

Motivi:

- il gate visuale actor/object `v0.38d.03` e' congelato;
- gli strumenti interattivi sono ancora agganciati a `MapGridWorldView`;
- top bar, DevTools, summary e pointer HUD non hanno ancora adapter ArcGraph;
- cancellare MapGrid ora farebbe perdere strumenti operativi utili.

### Prossimo lavoro consigliato

Prima del pensionamento MapGrid serve un micro-step preparatorio:

```text
v0.38f.01 - ArcGraph Interactive Tool Boundary Contract
```

Scopo:

- definire un contratto dati/eventi tra renderer mappa e strumenti debug;
- evitare che DevTools dipenda direttamente da MapGrid;
- evitare che ArcGraph possieda DevTools;
- preparare adapter futuri per picking cella, picking actor, camera e coordinate.

Questo micro-step puo' essere progettuale o implementativo leggero, ma non deve
ancora cancellare legacy.

## Esito v0.38f.01 - ArcGraph Interactive Tool Boundary Contract

La `v0.38f.01` introduce il primo contratto passivo per separare strumenti
interattivi/debug dal renderer MapGrid legacy.

Il punto non migra ancora pannelli, DevTools, top bar, summary cards o
click-to-move. Introduce invece il vocabolario minimo che servira' a questi
moduli per non leggere direttamente `MapGridWorldView`.

### Cosa viene introdotto

- `ArcGraphInteractionTargetKind`;
- `ArcGraphInteractionFrame`;
- `ArcGraphInteractionBoundaryDiagnostics`;
- `ArcGraphInteractionBoundaryBuilder`;
- `ArcGraphInteractionBoundaryHarness`.

Il boundary riceve:

- input view-side gia' normalizzato;
- stato vista ArcGraph;
- dimensione viewport;
- queue o liste actor/object gia' prodotte.

Il boundary restituisce:

- cella sotto il puntatore;
- actor visibile sotto il puntatore, se presente;
- oggetto visibile sotto il puntatore, se presente;
- blocco UI quando il puntatore e' sopra interfaccia;
- reason diagnostica spiegabile.

### Regola di priorita'

La priorita' dichiarata e':

```text
UI bloccante
-> actor
-> object
-> cella
-> nessun target
```

Esempio:

- se il mouse e' sopra un pannello UI, ArcGraph restituisce `UiBlocked`;
- se il mouse e' su una cella con NPC, restituisce `Actor`;
- se il mouse e' su una cella con oggetto ma senza NPC, restituisce `Object`;
- se il mouse e' su una cella vuota valida, restituisce `Cell`.

### Confine architetturale

La `v0.38f.01` non introduce:

- `GameObject`;
- `SpriteRenderer`;
- `Resources.Load`;
- `FindObjectOfType`;
- lettura diretta di `SimulationHost`;
- lettura diretta di `MapGridWorldView`;
- input fisico Unity;
- comandi di simulazione;
- selection globale;
- pannelli UI concreti.

ArcGraph espone solo fatti view-side.
I tool futuri decideranno cosa fare con quei fatti.

### QA tecnica

La compilazione isolata dei nuovi file e' riuscita con `dotnet csc`.

La ricerca statica sulle chiamate vietate non trova dipendenze operative vietate:
le occorrenze residue sono solo citazioni nei commenti architetturali.

### Prossimo lavoro consigliato

Il prossimo micro-step e':

```text
v0.38f.02 - ArcGraph Interaction Scene Adapter Audit
```

Scopo:

- auditare dove leggere mouse, camera e dimensioni viewport in Unity;
- definire un adapter scena che produca `ArcGraphInteractionFrame`;
- mantenere separati pannello laterale, barra superiore, overlay NPC e strumenti debug;
- evitare che ArcGraph diventi host dei tool;
- evitare che i tool restino agganciati direttamente a `MapGridWorldView`.

## Esito v0.38f.02 - ArcGraph Interaction Scene Adapter Audit

La `v0.38f.02` auditata stabilisce dove deve vivere il futuro adapter scena
responsabile di input, viewport, stato UI e dispatch dei frame interattivi
ArcGraph.

Il risultato principale e' che non bisogna copiare `MapGridWorldView` dentro
ArcGraph. La MapGrid oggi concentra troppe responsabilita' nello stesso
componente:

- rendering NPC/oggetti;
- hotkey debug;
- selezione NPC;
- click-to-move;
- FOV/landmark/GVD/DT;
- pointer coordinates HUD;
- summary cards;
- top bar;
- audio debug;
- rebind del World;
- bridge DevTools.

ArcGraph non deve ereditare questa forma.

### Stato MapGrid attuale

`MapGridWorldView` legge direttamente input Unity:

- `Keyboard.current` per F9, L, G, D e K;
- `Mouse.current.leftButton` per selezione NPC e click-to-move;
- `EventSystem.current.IsPointerOverGameObject()` per evitare click sulla UI;
- `MapGridPointerInputActionsProvider` per leggere il puntatore;
- `Camera.ScreenToWorldPoint` per trasformare lo schermo in celle.

Inoltre `MapGridWorldView` modifica `NPCSelection` quando il click sinistro
colpisce un NPC e puo' chiamare `SimulationHost.ForceAssignMoveToCellJobFromDevTools`
quando il click-to-move debug e' attivo.

Questo comportamento non deve passare nel renderer ArcGraph.

### Stato DevTools attuale

`MapGridRuntimeDevToolsOverlay` e' gia' concettualmente separato dalla view, ma
ha ancora dipendenze legacy:

- auto-bind tramite `FindObjectOfType<MapGridWorldView>`;
- fallback su `FindObjectOfType<MapGridPointerInputActionsProvider>`;
- fallback su `Camera.main` o `FindObjectOfType<Camera>`;
- lettura diretta di `Mouse.current`;
- lettura diretta di `Keyboard.current`;
- uso di `EventSystem.current.IsPointerOverGameObject()`;
- conversione puntatore/cella interna;
- emissione comandi verso `SimulationHost.Instance.EnqueueExternalCommand`.

Per ArcGraph questo significa: i DevTools non devono essere assorbiti dal renderer,
ma devono ricevere un frame interattivo gia' pronto da un adapter esterno.

### Stato camera/pan/zoom legacy

`MapGridCameraController` gestisce zoom e pan leggendo direttamente:

- rotellina mouse;
- tasto destro per drag pan;
- `Mouse.delta`;
- `EventSystem.current`;
- coordinate mouse per zoom-to-cursor.

ArcGraph ha gia' un modello diverso e piu' pulito:

- `ArcGraphViewInputFrame`;
- `ArcGraphViewController`;
- `ArcGraphViewCoordinateMapper`;
- `ArcGraphInteractionBoundaryBuilder`.

Quindi il futuro adapter scena non deve riusare `MapGridCameraController`.
Deve solo leggere input Unity e convertirlo in `ArcGraphViewInputFrame`.

### Modello consigliato

Il futuro componente scena consigliato e':

```text
ArcGraphInteractionSceneAdapter
```

Responsabilita':

- leggere mouse/rotellina/tasto centrale da Unity;
- leggere se il puntatore e' sopra UI;
- conoscere camera o viewport solo per ricavare coordinate viewport;
- costruire `ArcGraphViewInputFrame`;
- aggiornare `ArcGraphViewState` tramite `ArcGraphViewController`;
- costruire `ArcGraphInteractionFrame` tramite `ArcGraphInteractionBoundaryBuilder`;
- esporre `LastInputFrame`, `LastInteractionFrame` e diagnostica;
- opzionalmente notificare consumer esterni tramite metodo o interfaccia passiva.

Non deve:

- selezionare NPC;
- emettere comandi;
- aprire DevTools;
- creare pannelli;
- leggere `SimulationHost`;
- leggere `World` direttamente;
- leggere `MapGridWorldView`;
- modificare `NPCSelection`;
- possedere top bar, summary cards o overlay NPC.

### Separazione pannelli consigliata

La riorganizzazione debug/UI deve procedere a moduli separati:

```text
ArcGraph renderer
-> disegna mappa, actor, oggetti, debug minimo

ArcGraphInteractionSceneAdapter
-> produce input frame e interaction frame

SelectionTool
-> usa InteractionFrame e aggiorna NPCSelection

DebugCommandTool / DevTools
-> usa InteractionFrame e invia comandi espliciti

SidePanel
-> legge World/NPCSelection/explainability

TopBar
-> controlli runtime, pausa, step, toggle tool

NpcOverlay
-> label/card/anchor sopra actor selezionati

PointerHud
-> coordinate cella e diagnostica puntatore
```

Questa separazione evita due errori:

1. trasformare ArcGraph in un God Manager UI;
2. mantenere i tool legati a `MapGridWorldView`.

### Scelta su UI bloccante

La UI bloccante deve essere decisa dall'adapter scena, non dal builder passivo.

Esempio:

- il mouse e' sopra top bar;
- l'adapter legge `EventSystem.current.IsPointerOverGameObject()`;
- produce `ArcGraphViewInputFrame.IsPointerOverUi = true`;
- il controller ArcGraph non applica pan/zoom;
- il boundary restituisce `UiBlocked`;
- i tool non interpretano quel click come click mappa.

### Scelta su coordinate viewport

`ArcGraphViewCoordinateMapper` lavora gia' con coordinate viewport, non con
coordinate world Unity.

Questo e' corretto per il futuro perche':

- ArcGraph vuole controllare zoom discreto e pan tramite `ArcGraphViewState`;
- la mappa 250x250 non deve dipendere dalla posizione fisica della camera legacy;
- i test restano eseguibili senza scena;
- il renderer puo' cambiare implementazione senza cambiare il picking logico.

Il futuro adapter dovra' quindi convertire la posizione schermo in punto viewport
ArcGraph, non chiamare `ScreenToWorldPoint` per dedurre la cella come fa MapGrid.

### Prossimo micro-step consigliato

Il prossimo micro-step e':

```text
v0.38f.03 - ArcGraph Interaction Scene Adapter Contract
```

Scopo:

- introdurre un contratto C# passivo per l'adapter scena;
- definire diagnostica minima;
- definire eventuale interfaccia consumer;
- non implementare ancora DevTools, SelectionTool, TopBar o SidePanel;
- non salvare scene;
- non creare pannelli;
- non rimuovere MapGrid.

## Esito v0.38f.03 - ArcGraph Interaction Scene Adapter Contract

La `v0.38f.03` introduce il contratto C# passivo del futuro adapter scena
interattivo ArcGraph.

Il contratto non legge input Unity.
Il contratto non e' un `MonoBehaviour`.
Il contratto non crea oggetti scena.
Il contratto non seleziona NPC.
Il contratto non invia comandi.

### File introdotti

- `ArcGraphInteractionSceneFrame`;
- `ArcGraphInteractionSceneAdapterDiagnostics`;
- `IArcGraphInteractionFrameConsumer`;
- `ArcGraphInteractionSceneAdapterContract`;
- `ArcGraphInteractionSceneAdapterContractHarness`.

### Funzionamento

Il flusso del contratto e':

```text
ArcGraphInteractionSceneFrame
-> ArcGraphViewController
-> ArcGraphInteractionBoundaryBuilder
-> ArcGraphInteractionFrame
-> consumer opzionale
```

Il frame scena contiene:

- input view-side gia' normalizzato;
- dimensione viewport;
- flag di dispatch verso consumer;
- indice frame sorgente opzionale.

La diagnostica contiene:

- presenza config;
- presenza view state;
- validita' viewport;
- presenza puntatore;
- blocco UI;
- zoom/pan applicati;
- target risolto;
- actor/object id;
- dispatch o mancato dispatch.

### Consumer esterno

E' stata introdotta l'interfaccia:

```text
IArcGraphInteractionFrameConsumer
```

Questa interfaccia permette a tool futuri di ricevere il frame interattivo senza
essere posseduti dal renderer ArcGraph.

Esempi futuri:

- `SelectionTool`;
- `PointerHud`;
- `DebugCommandTool`;
- `NpcOverlay`;
- pannello laterale diagnostico.

### Confine preservato

La `v0.38f.03` non introduce:

- `Mouse.current`;
- `Keyboard.current`;
- `EventSystem.current`;
- `Camera.main`;
- `ScreenToWorldPoint`;
- `SimulationHost`;
- `MapGridWorldView`;
- `MapGridWorldProvider`;
- `Resources.Load`;
- `GameObject`;
- `AddComponent`;
- DevTools;
- SelectionTool;
- TopBar;
- SidePanel;
- NpcOverlay.

### QA tecnica

La compilazione isolata dei nuovi file e dei contratti `v0.38f.01` e' riuscita
con `dotnet csc`.

La ricerca statica sulle dipendenze vietate non trova chiamate operative vietate.
Le occorrenze residue sono solo citazioni nei commenti architetturali.

### Prossimo micro-step consigliato

Il prossimo micro-step e':

```text
v0.38f.04 - ArcGraph Interaction Scene Adapter Wrapper
```

Scopo:

- introdurre un wrapper Unity passivo e gated;
- leggere input fisico Unity solo nel wrapper;
- trasformare input fisico in `ArcGraphInteractionSceneFrame`;
- chiamare `ArcGraphInteractionSceneAdapterContract`;
- esporre diagnostica da Inspector/log;
- non migrare ancora DevTools, SelectionTool, TopBar, SidePanel o NpcOverlay;
- non salvare scene;
- non rimuovere MapGrid.

## Esito v0.38f.04 - ArcGraph Interaction Scene Adapter Wrapper

La `v0.38f.04` introduce il primo wrapper Unity passivo per alimentare il
contratto interattivo ArcGraph con input fisico.

Il wrapper e' spento di default.
Il wrapper non salva scene.
Il wrapper non crea pannelli.
Il wrapper non seleziona NPC.
Il wrapper non invia comandi.
Il wrapper non legge `World`.
Il wrapper non cerca `MapGridWorldView`.

### File introdotto

- `ArcGraphInteractionSceneAdapterWrapper`;
- `ArcGraphInteractionSceneAdapterWrapperDiagnostics`.

### Funzionamento

Il flusso runtime del wrapper e':

```text
Mouse.current / EventSystem.current
-> ArcGraphViewInputFrame
-> ArcGraphInteractionSceneFrame
-> ArcGraphInteractionSceneAdapterContract
-> ArcGraphInteractionFrame
-> consumer opzionale
```

### Gate di sicurezza

Il wrapper ha due gate:

- `adapterEnabled`;
- `processInUpdate`.

Entrambi sono falsi di default.

Quindi il componente non introduce costo per frame se non viene esplicitamente
abilitato. In alternativa puo' essere chiamato manualmente da Inspector tramite
context menu.

### Viewport

Il wrapper supporta due modalita':

- viewport = schermo intero;
- viewport manuale con dimensioni e origine in pixel.

La modalita' schermo intero e' il default provvisorio.
La modalita' manuale prepara il futuro aggancio a un viewport ArcGraph dedicato.

### Consumer

Il wrapper puo' ricevere:

- un consumer impostato via metodo `SetConsumer`;
- un `MonoBehaviour` serializzato che implementa `IArcGraphInteractionFrameConsumer`.

Se nessun consumer e' presente, il wrapper produce comunque diagnostica e
`LastInteractionFrame`.

### Confine preservato

La `v0.38f.04` non introduce:

- DevTools;
- SelectionTool;
- TopBar;
- SidePanel;
- NpcOverlay;
- `SimulationHost`;
- `MapGridWorldView`;
- `MapGridWorldProvider`;
- `Resources.Load`;
- `GameObject`;
- `AddComponent`;
- salvataggio scena.

Le uniche letture fisiche Unity presenti sono confinate nel wrapper:

- `Mouse.current`;
- `EventSystem.current`.

Questo e' intenzionale: il wrapper e' il punto di frontiera autorizzato.

### QA tecnica

La compilazione isolata del wrapper e dei contratti dipendenti e' riuscita con
`dotnet csc`.

Sono presenti solo warning attesi su campi `SerializeField` non assegnati nel
controllo isolato. In Unity quei campi sono pensati per Inspector o per setter
espliciti.

### Prossimo micro-step consigliato

Il prossimo micro-step e':

```text
v0.38f.05 - ArcGraph Interaction Consumer Audit
```

Scopo:

- auditare quale consumer migrare per primo;
- distinguere selection, pointer HUD, DevTools, top bar, side panel e overlay NPC;
- decidere quali consumer possono essere passivi e quali invece inviano comandi;
- evitare di trasformare subito il wrapper in un gestore operativo.

## Esito v0.38f.05 - ArcGraph Interaction Consumer Audit

La `v0.38f.05` ha auditato i consumer interattivi ancora assorbiti nel
perimetro MapGrid legacy.

L'obiettivo non era implementare nuovi pannelli.
L'obiettivo era decidere in quale ordine migrare i consumer sopra il nuovo
boundary interattivo ArcGraph introdotto tra `v0.38f.01` e `v0.38f.04`.

### File auditati

- `MapGridPointerCoordsOverlay`;
- `NPCSelection`;
- `MapGridRuntimeDevToolsOverlay`;
- `MapGridRuntimeControlTopBar`;
- `MapGridEntitySummaryOverlay`;
- `MapGridMovementExplainabilityPanelView`;
- `MapGridFovHeatmapOverlay`;
- `MapGridLandmarkOverlay`;
- `MapGridDtValueOverlay`;
- punti interattivi ancora presenti in `MapGridWorldView`.

### Classificazione dei consumer

| Consumer legacy | Tipo | Rischio | Destino consigliato |
|---|---|---|---|
| Pointer cell HUD | HUD screen-space passivo | Basso | Primo consumer da migrare |
| `NPCSelection` | Servizio selection view-side | Medio-basso | Secondo step, dopo verifica del frame |
| Overlay NPC / summary card | UI diagnostica su NPC selezionato | Medio | Dopo selection stabile |
| Movement explainability panel | Pannello laterale diagnostico | Medio | Dopo selection e World read-only dichiarato |
| Top bar runtime | UI operativa pausa/step/toggle | Medio-alto | Separare da ArcGraph renderer |
| DevTools runtime | Tool operativo che invia comandi | Alto | Migrare per ultimo, come command tool esplicito |
| Click-to-move debug | Comando debug verso simulazione | Alto | Non appartiene al renderer |
| FOV current cone | Overlay diagnostico dinamico | Medio | Rinviare a producer dedicato |

### Conclusione tecnica

Il primo consumer da migrare deve essere il Pointer HUD.

Motivo:

- consuma gia' informazione di cella;
- non seleziona NPC;
- non invia comandi;
- non legge `SimulationHost`;
- non richiede DevTools;
- permette di verificare subito se `ArcGraphInteractionFrame` e' leggibile;
- puo' mostrare `Cell`, `Actor`, `Object`, `UI blocked` e motivo diagnostico;
- puo' restare data-only prima di creare qualunque UI Unity.

Esempio pratico:

```text
ArcGraphInteractionSceneAdapterWrapper
-> ArcGraphInteractionFrame
-> ArcGraphPointerHudSnapshot
-> futuro pannello HUD
```

In questa forma il Pointer HUD non e' ancora un pannello Unity.
E' solo uno snapshot leggibile che prepara il pannello.

### Perche' non partire da SelectionTool

La selection e' piccola, ma modifica stato condiviso tramite `NPCSelection`.

Quindi e' meno rischiosa dei DevTools, ma piu' invasiva del Pointer HUD.

Ordine consigliato:

```text
prima: verifico cosa sto puntando
poi: permetto di selezionare cio' che sto puntando
```

### Perche' non partire da DevTools o top bar

Top bar e DevTools sono strumenti operativi.

Non sono renderer.
Non sono semplici consumer passivi.

Il loro rischio principale e' reintrodurre dentro ArcGraph un secondo punto di
comando della simulazione.

Il futuro DevTools ArcGraph dovra' quindi essere un modulo separato:

```text
InteractionFrame
-> DebugCommandTool
-> comando esplicito verso SimulationHost
```

Questo non deve accadere dentro il renderer.

### Ordine di migrazione consigliato

1. Pointer HUD passivo.
2. Selection consumer.
3. Overlay NPC sopra actor selezionato.
4. Side panel / explainability panel modulare.
5. Top bar runtime separata.
6. DevTools e click-to-move come command tool espliciti.
7. FOV current cone come producer overlay dedicato.

### Prossimo micro-step consigliato

Il prossimo micro-step e':

```text
v0.38f.06 - ArcGraph Pointer HUD Passive Contract
```

Scopo:

- introdurre `ArcGraphPointerHudSnapshot`;
- introdurre diagnostica passiva del Pointer HUD;
- introdurre un builder data-only che legge `ArcGraphInteractionFrame`;
- non creare UI Unity;
- non creare `GameObject`;
- non leggere `World`;
- non leggere `SimulationHost`;
- non agganciarsi a `MapGridWorldView`;
- non salvare scene.

## Esito v0.38f.06 - ArcGraph Pointer HUD Passive Contract

La `v0.38f.06` introduce il primo consumer passivo concreto del boundary
interattivo ArcGraph.

Non e' ancora un pannello Unity.
Non crea Canvas.
Non crea GameObject.
Non legge mouse fisico.
Non legge `World`.
Non legge `SimulationHost`.
Non dipende da `MapGridWorldView`.

### File introdotti

- `ArcGraphPointerHudSnapshot`;
- `ArcGraphPointerHudDiagnostics`;
- `ArcGraphPointerHudSnapshotBuilder`;
- `ArcGraphPointerHudSnapshotBuilderHarness`.

### Funzionamento

Il flusso dati e':

```text
ArcGraphInteractionFrame
-> ArcGraphPointerHudSnapshotBuilder
-> ArcGraphPointerHudSnapshot
-> futuro consumer UI
```

Lo snapshot contiene:

- visibilita' logica;
- presenza del frame interattivo;
- presenza puntatore;
- cella valida;
- blocco UI;
- target prioritario;
- id actor;
- id object;
- motivo interattivo;
- motivo adapter;
- testo display minimale.

Esempi di testo prodotto:

```text
Cell: 12,14 | Actor #7
Cell: 2,3
Cell: -,- | UI blocked
Cell: -,-
```

### Scopo tecnico

Questo step permette di testare il primo consumer senza introdurre UI reale.

Il Pointer HUD diventa quindi:

```text
consumer passivo del frame
```

e non:

```text
lettore autonomo di mouse/camera/world
```

### QA tecnica

La compilazione isolata dei file nuovi e delle dipendenze strette e' riuscita
con Roslyn `csc` e reference pack .NET.

La ricerca statica non trova dipendenze operative da:

- `GameObject`;
- `MonoBehaviour`;
- `Canvas`;
- `Resources.Load`;
- `SimulationHost`;
- `MapGridWorldView`;
- `MapGridWorldProvider`;
- `NPCSelection`;
- input fisico Unity.

Le occorrenze residue di parole come `GameObject`, `Canvas` e `Text` sono solo
commenti o campi dati come `DisplayText`.

### Prossimo micro-step consigliato

Il prossimo micro-step e':

```text
v0.38f.07 - ArcGraph Pointer HUD Scene Consumer
```

Scopo:

- introdurre un consumer Unity gated che implementi `IArcGraphInteractionFrameConsumer`;
- usare lo snapshot `v0.38f.06`;
- creare un HUD di scena temporaneo solo se abilitato;
- non salvare scene;
- non copiare il monolite `MapGridPointerCoordsOverlay`;
- non introdurre DevTools, selection o comandi.

## Esito v0.38f.07 - ArcGraph Pointer HUD Scene Consumer

La `v0.38f.07` introduce il primo consumer scena del Pointer HUD ArcGraph.

Il consumer implementa:

```text
IArcGraphInteractionFrameConsumer
```

e riceve frame dal wrapper `v0.38f.04`.

### File introdotto

- `ArcGraphPointerHudSceneConsumer`.

### Funzionamento

Il flusso dati e':

```text
ArcGraphInteractionSceneAdapterWrapper
-> IArcGraphInteractionFrameConsumer
-> ArcGraphPointerHudSceneConsumer
-> ArcGraphPointerHudSnapshotBuilder
-> OnGUI provvisorio
```

Il consumer:

- consuma `ArcGraphInteractionFrame`;
- conserva `LastSnapshot`;
- conserva `LastDiagnostics`;
- mostra il testo solo se `hudEnabled` e' attivo;
- usa `OnGUI` come visualizzazione temporanea;
- non crea Canvas;
- non crea prefab;
- non salva scene;
- non legge mouse fisico;
- non legge `World`;
- non legge `SimulationHost`;
- non seleziona NPC;
- non invia comandi.

### Perche' OnGUI in questo micro-step

L'uso di `OnGUI` e' temporaneo e intenzionale.

Permette di vedere subito il valore del Pointer HUD senza costruire il sistema
definitivo dei pannelli modulari.

Il sistema pannelli definitivo resta un tema successivo e dovra' separare:

- pannello superiore;
- pannello laterale;
- overlay sopra NPC;
- command tools;
- debug tools.

### QA tecnica

La compilazione isolata con Roslyn `csc` e assembly Unity necessari e' riuscita.

E' presente solo un warning atteso su un campo `SerializeField` non assegnato nel
controllo isolato.

La ricerca statica non trova dipendenze operative da:

- `SimulationHost`;
- `MapGridWorldView`;
- `MapGridWorldProvider`;
- `NPCSelection`;
- input fisico Unity;
- DevTools;
- SelectionTool;
- runtime control top bar.

### Prossimo micro-step consigliato

Il prossimo micro-step e':

```text
v0.38f.08 - ArcGraph Selection Consumer
```

Scopo:

- introdurre un consumer selection separato dal renderer;
- usare `ArcGraphInteractionFrame` come input;
- selezionare solo actor validi;
- non introdurre DevTools;
- non inviare comandi;
- non leggere direttamente `MapGridWorldView`;
- mantenere `NPCSelection` come servizio view-side esistente.

## Esito v0.38f.08 - ArcGraph Selection Consumer

La `v0.38f.08` introduce il consumer selection ArcGraph separato dal renderer.

### File introdotto

- `ArcGraphSelectionSceneConsumer`.

### File modificati

- `ArcGraphViewInputFrame`;
- `ArcGraphInteractionSceneAdapterWrapper`.

### Perche' e' stato esteso `ArcGraphViewInputFrame`

Il boundary interattivo sapeva gia':

- quale cella e' sotto il puntatore;
- quale actor e' sotto il puntatore;
- quale object e' sotto il puntatore;
- se la UI blocca il puntatore.

Pero' non sapeva se era stato premuto il click primario.

Per fare selection corretta non basta il puntamento.
Selezionare su hover sarebbe sbagliato.

Quindi e' stato aggiunto:

```text
IsPrimaryPointerPressedThisFrame
```

Questo campo viene valorizzato solo dal wrapper Unity, cioe' dall'unico punto
autorizzato a leggere input fisico.

### Funzionamento

Il flusso selection e':

```text
Mouse.leftButton.wasPressedThisFrame
-> ArcGraphViewInputFrame.IsPrimaryPointerPressedThisFrame
-> ArcGraphInteractionFrame
-> ArcGraphSelectionSceneConsumer
-> NPCSelection.Select(actorId)
```

Il consumer seleziona solo quando:

- `selectionEnabled` e' attivo;
- il puntatore non e' sopra UI;
- il frame contiene click primario;
- il target e' `Actor`;
- l'actor id e' valido.

Non seleziona su hover.
Non fa clear automatico su cella vuota.
Non seleziona object.
Non invia comandi.

### Coerenza con MapGrid legacy

Il comportamento legacy di `MapGridWorldView` seleziona solo su left click sopra
NPC e non fa nulla su cella vuota.

Il consumer ArcGraph mantiene la stessa semantica di base, ma sposta la
responsabilita' fuori dal renderer.

### QA tecnica

La compilazione isolata del consumer selection e del wrapper modificato e'
riuscita con Roslyn `csc`, reference pack .NET, assembly Unity e
`Unity.InputSystem`.

Sono presenti solo warning attesi su campi `SerializeField` non assegnati nel
controllo isolato.

La ricerca statica conferma che:

- l'input fisico resta nel wrapper;
- il consumer selection non legge mouse;
- il consumer selection non legge `MapGridWorldView`;
- il consumer selection non legge `SimulationHost`;
- il consumer selection non apre DevTools.

### Nodo tecnico emerso

Ora esistono almeno due consumer interattivi:

- Pointer HUD;
- Selection.

Il wrapper attuale puo' dispatchare a un solo consumer.

Serve quindi un router/fan-out consumer per collegare piu' moduli senza farli
dipendere tra loro.

### Prossimo micro-step consigliato

Il prossimo micro-step e':

```text
v0.38f.09 - ArcGraph Interaction Consumer Router
```

Scopo:

- introdurre un consumer composito;
- inoltrare lo stesso frame a piu' consumer espliciti;
- mantenere ogni consumer modulare;
- non introdurre DevTools;
- non inviare comandi;
- non salvare scene.

## Esito v0.38f.09 - ArcGraph Interaction Consumer Router

La `v0.38f.09` introduce un router/fan-out per consumer interattivi ArcGraph.

### File introdotto

- `ArcGraphInteractionConsumerRouter`.

### Funzionamento

Il wrapper scena continua a vedere un solo consumer:

```text
ArcGraphInteractionSceneAdapterWrapper
-> ArcGraphInteractionConsumerRouter
```

Il router inoltra poi lo stesso frame a piu' consumer:

```text
ArcGraphInteractionConsumerRouter
-> ArcGraphPointerHudSceneConsumer
-> ArcGraphSelectionSceneConsumer
-> futuri consumer modulari
```

### Scopo tecnico

Questo evita che:

- il wrapper diventi un host di tool;
- il Pointer HUD conosca la selection;
- la selection conosca il Pointer HUD;
- ArcGraph ricrei il monolite `MapGridWorldView`.

Ogni consumer resta responsabile della propria semantica.

### Gate e diagnostica

Il router e' gated tramite:

```text
routerEnabled
```

La diagnostica registra:

- frame ricevuto;
- router abilitato;
- consumer candidati;
- consumer chiamati;
- consumer saltati;
- target del frame;
- actor id;
- reason.

### QA tecnica

La compilazione isolata del router insieme a HUD e selection e' riuscita con
Roslyn `csc` e assembly Unity necessari.

Sono presenti solo warning attesi su campi `SerializeField` non assegnati nel
controllo isolato.

La ricerca statica non trova dipendenze operative da:

- `SimulationHost`;
- `MapGridWorldView`;
- `MapGridWorldProvider`;
- input fisico Unity;
- DevTools;
- top bar.

### Prossimo micro-step consigliato

Il prossimo micro-step e':

```text
v0.38f.10 - Gate visuale consumer modulari ArcGraph
```

Scopo:

- verificare in Unity il wiring:

```text
wrapper -> router -> Pointer HUD + Selection
```

- non aggiungere codice nuovo prima del gate;
- non salvare scene senza conferma;
- verificare che HUD e selection possano convivere;
- verificare che il click su actor selezioni solo quando il consumer selection e'
  abilitato;
- verificare che il Pointer HUD mostri cella/actor/UI blocked.

## Esito v0.38f.10a - ArcGraph Interaction RenderQueue Wiring Probe

La `v0.38f.10a` introduce un micro-ponte tecnico necessario per rendere
testabile il gate visuale dei consumer modulari ArcGraph.

Il problema rilevato era semplice:

```text
wrapper -> router -> Pointer HUD + Selection
```

esisteva gia', ma il wrapper interattivo non riceveva ancora una
`ArcGraphRenderQueue` actor/object reale. Senza quella queue, il boundary poteva
risolvere la cella sotto il mouse, ma non poteva riconoscere actor e object in
modo utile per HUD e selection.

### File introdotto

- `Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphInteractionRenderQueueWiringProbe.cs`.

### Funzionamento

Il probe usa riferimenti espliciti assegnati da Inspector:

```text
ArcGraphTerrainRuntimeMapGridAdapter
-> ArcGraphRuntimeContext
-> ArcGraphBootstrapRuntime temporaneo
-> ArcGraphActorLayer + ArcGraphObjectLayer
-> ArcGraphRenderQueue
-> ArcGraphInteractionSceneAdapterWrapper.SetRenderQueue(...)
```

Il componente aggiunge un context menu manuale:

```text
ArcGraph/Push Interaction Render Queue To Wrapper
```

Questo consente al gate umano di spingere la queue actor/object nel wrapper
senza salvare scene, senza introdurre un renderer produttivo e senza creare
tool operativi.

### Confini preservati

La `v0.38f.10a` non introduce:

- salvataggio scena;
- prefab;
- DevTools;
- top bar;
- comandi;
- input fisico Unity;
- `SimulationHost`;
- lettura diretta di `MapGridWorldView`;
- `Resources.Load`;
- renderer produttivo permanente;
- accessi globali nascosti.

Il probe resta un componente di gate manuale. Costruisce una queue temporanea,
la consegna al wrapper e conserva diagnostica leggibile.

### QA tecnica

La ricerca statica sul nuovo file non trova dipendenze vietate da:

- `SimulationHost`;
- `MapGridWorldView`;
- `MapGridWorldProvider`;
- input fisico Unity;
- `Resources.Load`;
- DevTools;
- top bar;
- `NPCSelection`.

Il tentativo di `dotnet build Assembly-CSharp.csproj --no-restore` non e'
conclusivo perche' il progetto Unity generato richiede il file temporaneo
`Temp/obj/Assembly-CSharp/project.assets.json`. Non e' stato eseguito restore
per evitare modifiche nelle cartelle temporanee Unity.

### Stato dopo il micro-step

Il gate visuale `v0.38f.10` torna eseguibile:

```text
runtime adapter
-> render queue wiring probe
-> interaction wrapper
-> router
-> Pointer HUD + Selection
```

Il prossimo passaggio non deve aggiungere codice. Deve verificare in Unity che:

- il wrapper riceva una queue actor/object;
- il Pointer HUD mostri cella, actor o UI blocked;
- il click primario sopra actor attivi il consumer selection;
- HUD e selection convivano senza conoscersi direttamente.

## Esito v0.38f.10 - Gate visuale consumer modulari ArcGraph congelato e poi recuperato

Il gate visuale `v0.38f.10` viene segnato come congelato su richiesta
dell'operatore.

Il congelamento non rappresenta un fallimento tecnico.
Significa invece che il test manuale in Unity non viene eseguito ora e sara'
recuperato piu' avanti insieme agli altri gate visuali congelati.

### Stato della catena da testare

La catena tecnica resta pronta:

```text
ArcGraphTerrainRuntimeMapGridAdapter
-> ArcGraphInteractionRenderQueueWiringProbe
-> ArcGraphInteractionSceneAdapterWrapper
-> ArcGraphInteractionConsumerRouter
-> ArcGraphPointerHudSceneConsumer
-> ArcGraphSelectionSceneConsumer
```

### Cosa resta da verificare quando i test verranno riaperti

Quando l'operatore potra' eseguire i test visuali, questo gate dovra'
confermare:

- il push della `ArcGraphRenderQueue` actor/object verso il wrapper;
- la presenza di `queue=True` nella diagnostica del wrapper;
- la visualizzazione del Pointer HUD;
- il riconoscimento di cella, actor e UI blocked;
- la selection su click primario sopra actor;
- la convivenza di Pointer HUD e Selection tramite router, senza dipendenze
  dirette tra consumer.

### Vincoli durante il congelamento

Durante il congelamento non bisogna considerare completata la parte visuale
interattiva.

Aggiornamento successivo: il gate interaction e' stato recuperato e superato in
`v0.38g.02`. La sezione resta come memoria storica del congelamento iniziale,
non come stato operativo corrente.

Non bisogna ancora:

- promuovere i consumer ArcGraph a sistema UI definitivo;
- cancellare la selection legacy MapGrid;
- cancellare o pensionare `MapGridWorldView`;
- procedere al pensionamento reale degli strumenti interattivi legacy;
- assumere che picking actor, HUD e selection siano validati in scena.

Il prossimo lavoro puo' proseguire solo su blocchi che non dipendono dall'esito
visivo di questo gate, oppure deve restare preparatorio/audit-first.

## Esito v0.38g.00 - ArcGraph Legacy Retirement Dependency Audit

La `v0.38g.00` auditata chiarisce cosa puo' essere preparato per il
pensionamento di MapGrid e cosa invece resta bloccato dai gate visuali
congelati.

L'audit e' stato eseguito in sola lettura.
Non sono stati modificati file runtime, scene, prefab o asset.

### Conclusione principale

Il pensionamento reale di `MapGridWorldView`, `MapGridBootstrap` e strumenti
collegati non puo' partire ora.

Il motivo non e' che ArcGraph non abbia contratti: molti contratti esistono.
Il motivo e' che una parte critica della catena visuale non e' ancora validata
in Unity:

- actor/object ArcGraph esiste come probe, ma il gate visuale resta congelato;
- interazione ArcGraph HUD + selection esiste come catena tecnica, ma il gate
  visuale resta congelato;
- il terrain ArcGraph e' il solo blocco visuale gia' verificato positivamente;
- top bar, DevTools, summary UI, click-to-move, FOV current cone e label
  screen-space restano ancora MapGrid-centrici o non migrati.

### Matrice dipendenze

| Componente MapGrid | Responsabilita' attuale | Equivalente ArcGraph esiste? | Validato? | Pensionabile ora? | Bloccante residuo |
|---|---|---|---|---|---|
| `MapGridChunkRenderer` | Rendering mesh terrain chunked | Si': `ArcGraphTerrainChunkMeshBuilder` + `ArcGraphTerrainSceneProbeRenderer` | Si', gate terrain superato | Non ancora fisicamente | Manca renderer terrain produttivo permanente e piano bootstrap ArcGraph |
| `MapGridData` | Buffer view-side terrain/blocco | Parziale: sorgente legacy via `ArcGraphTerrainRuntimeMapGridAdapter` | Si' per terrain snapshot | No | Resta sorgente temporanea finche' non esiste modello mappa ArcGraph/World definitivo |
| `MapGridTileAtlas` | UV atlas tile legacy | Parziale: `ArcGraphTerrainTileUvMap` deriva da config | Si' nel probe terrain | No | Serve asset/UV policy definitiva, non solo bridge legacy |
| `MapGridBootstrap` | Carica config/layout, costruisce `MapGridData`, chunk terrain, camera, pointer provider e attach view | Parziale: ArcGraph ha bootstrap runtime data-only, non bootstrap scena produttivo | No | No | Troppa responsabilita' ancora concentrata nel bootstrap MapGrid |
| `MapGridCameraController` | Camera concreta, pan, zoom, PixelPerfectCamera, input mouse | Parziale: `ArcGraphViewController`, `ArcGraphViewState`, `ArcGraphInteractionSceneAdapterWrapper` | No | No | Manca wrapper camera/viewport produttivo e gate zoom/pan visuale completo |
| `MapGridPointerInputActionsProvider` | Provider puntatore New Input System per MapGrid | Parziale: wrapper interazione ArcGraph legge input fisico direttamente | No | No | Gate interazione congelato, politica definitiva input ancora da stabilizzare |
| `MapGridWorldView` actor sync | Crea/aggiorna `SpriteRenderer` NPC, collider, handle, flash decisionale e balloon | Parziale: `ArcGraphWorldAdapter`, actor layer, render queue, actor/object probe | Gate actor/object congelato | No | Manca renderer actor/object produttivo, pooling, asset resolver definitivo, validazione scena |
| `MapGridWorldView` object sync | Crea/aggiorna sprite oggetti, stock label, cleanup | Parziale: object snapshot, object layer, render queue, actor/object probe | Gate actor/object congelato | No | Stock label e asset resolver non sono ancora equivalenti produttivi |
| `MapGridWorldView` selection | Click sinistro su NPC aggiorna `NPCSelection` | Si': `ArcGraphSelectionSceneConsumer` | Gate interazione congelato | No | Picking actor e click selection non validati in Unity |
| `MapGridWorldView` click-to-move | Tool debug K + click cella verso `SimulationHost` | No come ArcGraph tool | No | No | Va separato come DevTools command tool esterno ad ArcGraph renderer |
| `MapGridRuntimeDevToolsOverlay` | DevTools F3, place/erase/spawn/orient/transport, comandi core | No come modulo separato ArcGraph | No | No | Dipende ancora da MapGridWorldView, pointer provider, camera, `SimulationHost` |
| `MapGridRuntimeControlTopBar` | Barra runtime pausa/step/spawn/FOV | No come modulo UI separato ArcGraph | No | No | Usa `SimulationHost`, `MapGridWorldView`, DevTools e Canvas creato runtime |
| `MapGridPointerCoordsOverlay` | HUD coordinate cella + runtime cost | Parziale: `ArcGraphPointerHudSceneConsumer` | Gate interazione congelato | No | HUD ArcGraph non validato in scena; runtime cost non ancora migrato |
| `MapGridFovHeatmapOverlay` | FOV current cone e heatmap debug | Parziale per overlay debug generico, non per current cone | No | No | Serve producer ArcGraph dedicato per FOV current cone |
| `MapGridLandmarkOverlay` | Landmark, route, GVD-DIN, edge, DT heatmap | Parziale/si': debug overlay queue + runtime feed Landmark/GVD | Si' per ponte manuale debug v0.37 | Non fisicamente | Label screen-space e wiring produttivo restano fuori |
| `MapGridLandmarkLabelOverlay` | Label Canvas per nodi Landmark/GVD | No equivalente definitivo | No | No | Serve canale UI/screen-space separato |
| `MapGridDtValueOverlay` | Valori numerici DT su Canvas/screen-space | Parziale come debug data, non come label UI | No | No | Serve canale testo overlay ArcGraph |
| `MapGridEntitySummaryOverlay` | Summary cards NPC/oggetti | No | No | No | Da riprogettare come pannello/overlay UI modulare, non renderer |
| `MapGridNpcBalloonView` | Balloon sopra NPC per eventi/token | No equivalente ArcGraph | No | No | Richiede overlay NPC/anchor layer separato |
| `MapGridStockLabel` | Label quantita' stock su oggetti | No equivalente produttivo | No | No | Va deciso se label world-space o UI overlay |
| `MapGridRuntimeDebugAudioFeedback` | Feedback audio debug da World | No equivalente ArcGraph | No | No | Non e' renderer, va separato come consumer debug/audio |
| `MapGridWorldProvider` | Provider statico del World per MapGrid | ArcGraph evita accesso globale, usa adapter espliciti | Parziale | No | Serve sorgente neutra/bootstrap scena prima della rimozione |

### Classificazione operativa

Blocchi quasi pronti ma non eliminabili subito:

- terrain rendering legacy;
- Landmark/GVD debug bridge preparatorio.

Blocchi tecnicamente implementati ma congelati da test visuali:

- actor/object scene probe;
- render queue actor/object verso wrapper interattivo;
- Pointer HUD ArcGraph;
- selection ArcGraph;
- router consumer interattivi.

Blocchi ancora da progettare o migrare:

- camera/input produttivi ArcGraph;
- renderer terrain produttivo permanente;
- renderer actor/object produttivo con pooling;
- asset resolver sprite definitivo;
- top bar modulare;
- DevTools command tool separato;
- click-to-move debug;
- summary cards;
- overlay NPC/balloon;
- stock label;
- FOV current cone;
- label screen-space Landmark/GVD/DT;
- audio feedback debug.

### Decisione tecnica v0.38g.00

`v0.38g` non deve ancora cancellare componenti MapGrid.

La forma corretta del prossimo lavoro non e' "rimuovere MapGrid", ma consolidare
il backlog dei gate visuali e preparare un piano di recupero test:

```text
v0.38g.01 - ArcGraph Frozen Visual Gates Backlog Plan
```

Scopo consigliato:

- raccogliere in una sola lista i gate visuali congelati;
- definire l'ordine in cui testarli quando l'operatore potra' usare Unity;
- indicare per ogni gate quali componenti Inspector servono;
- indicare quali blocchi `v0.38g` restano bloccati da quel gate;
- evitare di aggiungere nuovi moduli prima di sapere quali probe funzionano in
  scena.

### Stop confermato

```text
Non cancellare MapGridWorldView.
Non cancellare MapGridBootstrap.
Non cancellare MapGridRuntimeDevToolsOverlay.
Non pensionare selection legacy.
Non dichiarare actor/object ArcGraph produttivo.
Non dichiarare interaction HUD/selection validati.
Non procedere a rimozione fisica finche' i gate congelati non sono recuperati.
```

## Esito v0.38g.01 - ArcGraph Frozen Visual Gates Backlog Plan

La `v0.38g.01` ordina lo stato reale dei gate visuali ArcGraph dopo il recupero
manuale parziale eseguito in Unity dall'operatore.

Questo step non introduce codice runtime, non salva scene, non crea prefab e non
pensiona componenti MapGrid. Serve solo a rendere esplicito quali pezzi sono
stati verificati e quali restano bloccanti.

### Stato dei gate recuperati

| Gate | Esito attuale | Evidenza | Cosa valida | Cosa non valida ancora |
|---|---|---|---|---|
| Terrain data-only | OK | `TerrainSnapshotsBuilt`, `terrainSnapshots=16384`, `layerCount=4` | L'adapter legge config, mappa e world runtime e produce snapshot terrain | Non e' una validazione del renderer produttivo permanente |
| Terrain visuale probe | Gia' considerato OK dal gate `v0.38c.03` | Test Unity precedente dell'operatore | Il probe terrain puo' disegnare la mappa come scena temporanea | Non sostituisce ancora `MapGridChunkRenderer` in produzione |
| Actor/Object probe | OK tecnico e visuale provvisorio | `ActorObjectSceneProbeRendered`, `queueEntries=1`, `createdObjects=1`, `missingSprites=0` | La queue actor/object arriva al probe e crea almeno un oggetto scena | `spriteResolver=False` implica fallback sprite; asset resolver, pooling, label stock e renderer permanente non sono validati |
| Interaction wrapper/router/HUD/selection | Non ancora OK | Diagnostica screenshot con `consumer=False` | Il wrapper esiste, legge input e puo' ricevere queue | Il consumer router non era collegato nel campo Inspector `Interaction Consumer Behaviour`; HUD e selection non sono ancora validati |

### Ordine consigliato dei prossimi test manuali

1. Interaction wiring minimo:

   ```text
   ArcGraphInteractionSceneAdapterWrapper.Interaction Consumer Behaviour
   -> ArcGraphInteractionConsumerRouter
   ```

   Esito atteso in Console:

   ```text
   consumer=True
   queue=True
   ```

2. Pointer HUD:

   - mouse dentro la Game View;
   - HUD deve mostrare cella, UI blocked, actor o object;
   - se compare `PointOutsideViewport`, il mouse e' fuori dalla viewport usata dal wrapper o sopra finestre non Game View.

3. Selection:

   - click sinistro sopra NPC;
   - il target deve essere `Actor`;
   - `ArcGraphSelectionSceneConsumer` deve aggiornare `NPCSelection`;
   - il click su cella vuota non deve fare clear automatico, coerentemente con MapGrid legacy.

4. Actor/Object con sprite resolver:

   - collegare un `ArcGraphSerializedSpriteResolver`;
   - sostituire fallback sprite con sprite reale;
   - verificare `generatedFallback=False` o riduzione dei fallback attesi.

5. Terrain visuale ripetibile:

   - ripetere render probe terrain dopo eventuali cambi scena;
   - confermare che il materiale terrain assegnato da Inspector resta leggibile e non viene caricato via codice.

### Blocchi ancora non pensionabili

Anche con Terrain e Actor/Object probe positivi, non si puo' ancora cancellare
`MapGridWorldView`.

I motivi sono:

- non esiste renderer actor/object produttivo con pooling;
- non esiste asset resolver definitivo obbligatorio;
- interaction HUD/selection non e' ancora validata;
- top bar, DevTools, click-to-move, summary cards, FOV current cone, label
  screen-space, balloon NPC e stock label restano fuori da ArcGraph produttivo;
- ArcGraph non possiede ancora bootstrap scena produttivo completo.

### Decisione operativa

Il prossimo sviluppo non deve aggiungere moduli ambientali nuovi.

La direzione piu' sicura e' completare il nucleo minimo:

```text
terrain + actor/object + interaction base
```

solo dopo:

```text
camera/input produttivi
-> debug minimo
-> UI/tool modulari
-> pensionamento progressivo MapGrid
```

## Esito v0.38g.02 - ArcGraph Interaction Gate Recovery

La `v0.38g.02` registra il recupero positivo del gate interaction ArcGraph.

L'operatore ha confermato che il test 3 e' superato. Questo significa che il
percorso:

```text
ArcGraphInteractionSceneAdapterWrapper
-> ArcGraphInteractionConsumerRouter
-> ArcGraphPointerHudSceneConsumer
-> ArcGraphSelectionSceneConsumer
```

puo' essere considerato validato nel gate manuale Unity.

### Cosa viene validato

Il gate conferma:

- il wrapper interaction puo' ricevere input scena;
- il wrapper puo' dispatchare verso il router;
- il router puo' fan-out verso piu' consumer modulari;
- Pointer HUD e Selection possono convivere senza dipendere direttamente l'uno
  dall'altra;
- la catena interaction base non resta piu' congelata.

### Cosa non viene ancora validato

Il gate non rende ancora ArcGraph renderer produttivo principale.

Restano fuori:

- renderer terrain permanente;
- renderer actor/object produttivo con pooling;
- asset resolver definitivo;
- camera/input produttivi completi;
- DevTools;
- top bar;
- click-to-move;
- summary cards;
- FOV current cone;
- label screen-space Landmark/GVD/DT;
- balloon NPC;
- stock label.

### Stato dopo i tre gate minimi

I tre gate minimi sono ora recuperati:

```text
terrain
actor/object probe
interaction base
```

Questo consente di spostare il lavoro dal semplice recupero dei gate al disegno
del percorso runtime minimo stabile.

Non consente ancora il pensionamento fisico di `MapGridWorldView` o
`MapGridBootstrap`.

## Esito v0.38g.03 - ArcGraph Minimal Stable Runtime Path Audit

La `v0.38g.03` chiarisce cosa fare dopo il recupero dei tre gate minimi.

I gate validati dimostrano che ArcGraph puo' ricevere dati, visualizzare
terrain, visualizzare actor/object in probe e gestire una base interaction
modulare. Non dimostrano ancora che esista un percorso runtime stabile.

### Stato reale dei blocchi validati

| Blocco | Stato attuale | Perche' non e' ancora produttivo |
|---|---|---|
| Terrain | Probe visuale validato | Crea mesh temporanee sotto root dedicato; manca un renderer terrain stabile con lifecycle dichiarato |
| Actor/Object | Probe visuale validato | Crea `SpriteRenderer` temporanei; manca pooling, cleanup stabile, asset resolver obbligatorio e policy lifecycle |
| Interaction | Gate wrapper/router/HUD/selection validato | Funziona come base consumer, ma non deve diventare host DevTools o command tool |
| Runtime context | Funziona tramite adapter MapGrid | Dipende ancora da `MapGridBootstrap.RuntimeMap` e `MapGridWorldView.RuntimeWorld` |
| Bootstrap ArcGraph | Nucleo C# passivo valido | Viene ancora creato spesso come runtime temporaneo nei probe |

### Dipendenze MapGrid ancora necessarie

ArcGraph oggi riceve i dati minimi da:

```text
MapGridBootstrap.RuntimeConfig
MapGridBootstrap.RuntimeMap
MapGridWorldView.RuntimeWorld
```

Questa dipendenza e' accettabile solo come sorgente temporanea dichiarata.

Non e' invece accettabile trasformare ArcGraph in figlio logico di:

```text
MapGridChunkRenderer
MapGridWorldView actor/object sync
MapGridRuntimeDevToolsOverlay
MapGridRuntimeControlTopBar
```

### Forma consigliata del prossimo passaggio

Il prossimo passaggio non deve essere una cancellazione MapGrid.

Il prossimo passaggio deve essere un contratto/coordinator minimo:

```text
ArcGraphMinimalRuntimeCoordinator
```

Il coordinator dovrebbe:

- ricevere riferimenti espliciti da Inspector o setter;
- costruire un solo `ArcGraphRuntimeContext`;
- inizializzare o aggiornare un solo `ArcGraphBootstrapRuntime`;
- ordinare il refresh snapshot;
- consegnare dati terrain al renderer terrain stabile futuro;
- consegnare queue actor/object al renderer actor/object stabile futuro;
- consegnare queue actor/object al wrapper interaction;
- mantenere tutto gated e spento di default;
- esporre diagnostica chiara.

### Cosa non deve fare il coordinator

Il coordinator non deve:

- leggere `SimulationHost.Instance`;
- chiamare `MapGridWorldProvider`;
- cercare oggetti con `FindObjectOfType`;
- salvare scene;
- creare DevTools;
- inviare comandi;
- possedere top bar;
- possedere click-to-move;
- possedere summary cards;
- diventare un nuovo `MapGridWorldView`.

### Decisione operativa

Il percorso minimo stabile deve nascere cosi':

```text
adapter dichiarato MapGrid temporaneo
-> runtime context ArcGraph
-> bootstrap runtime riusabile
-> coordinator minimo
-> renderer terrain/actor/object separati
-> interaction wrapper/router gia' validati
```

La cancellazione di MapGrid resta bloccata finche' non esiste almeno:

- coordinator minimo;
- renderer terrain stabile;
- renderer actor/object stabile;
- asset resolver reale;
- camera/input runtime controllati;
- piano separato per UI/tool legacy.

## Esito v0.38g.04 - ArcGraph Minimal Runtime Coordinator Contract

La `v0.38g.04` introduce il contratto C# passivo del coordinator runtime minimo
ArcGraph.

### File introdotti

- `ArcGraphMinimalRuntimeCoordinatorFrame`;
- `ArcGraphMinimalRuntimeCoordinatorDiagnostics`;
- `ArcGraphMinimalRuntimeCoordinator`;
- `ArcGraphMinimalRuntimeCoordinatorHarness`.

### Funzione del coordinator

Il coordinator riceve un `ArcGraphRuntimeContext` gia' costruito da un adapter
esterno e orchestra:

```text
ArcGraphRuntimeContext
-> ArcGraphBootstrapRuntime
-> RefreshSnapshots
-> ArcGraphRenderQueue actor/object
```

Il coordinator non disegna e non crea oggetti scena.

### Confini preservati

Il coordinator non e':

- un `MonoBehaviour`;
- un renderer;
- un tool host;
- un DevTools manager;
- un sostituto di `MapGridWorldView`;
- un accesso globale al `World`.

Non introduce:

- `GameObject`;
- `AddComponent`;
- input fisico Unity;
- `SimulationHost`;
- `MapGridWorldProvider`;
- `FindObjectOfType`;
- `Resources.Load`;
- comandi;
- top bar;
- click-to-move;
- summary cards.

### Comportamento tecnico

Il coordinator:

- inizializza un `ArcGraphBootstrapRuntime` quando il context e' valido;
- riusa il runtime se config, mappa e world sono gli stessi riferimenti;
- ricrea il runtime se cambia una sorgente, per esempio dopo load snapshot;
- puo' rinfrescare snapshot;
- puo' costruire una `ArcGraphRenderQueue` actor/object;
- pulisce la queue nei gate falliti, evitando dati derivati vecchi;
- espone diagnostica con sorgenti disponibili, layer presenti, snapshot e conteggi queue.

### QA

La ricerca statica sui nuovi file non trova dipendenze operative vietate.

`dotnet build Assembly-CSharp.csproj --no-restore` non e' conclusivo perche'
manca il file temporaneo Unity:

```text
Temp/obj/Assembly-CSharp/project.assets.json
```

Non e' stato eseguito restore per evitare modifiche nelle cartelle temporanee
Unity.

### Prossimo step consigliato

Il prossimo micro-step e':

```text
v0.38g.05 - ArcGraph Minimal Runtime Scene Wrapper Contract
```

Scopo:

- creare un wrapper scena minimale, spento di default;
- alimentare il coordinator con `ArcGraphTerrainRuntimeMapGridAdapter`;
- consegnare la queue prodotta al wrapper interaction gia' validato;
- non creare renderer produttivi;
- non cancellare ancora MapGrid;
- non salvare scene;
- non introdurre DevTools, top bar, click-to-move o pannelli avanzati.

## Esito v0.38g.05 - ArcGraph Minimal Runtime Scene Wrapper Contract

La `v0.38g.05` introduce il wrapper Unity minimale che collega il coordinator
runtime minimo ArcGraph alla scena in modo esplicito e controllato.

### File introdotti

- `ArcGraphMinimalRuntimeSceneWrapper`;
- `ArcGraphMinimalRuntimeSceneWrapperDiagnostics`.

### Funzione del wrapper

Il wrapper e' il primo punto scena unico per il percorso minimo:

```text
ArcGraphTerrainRuntimeMapGridAdapter
-> ArcGraphRuntimeContext
-> ArcGraphMinimalRuntimeCoordinator
-> ArcGraphRenderQueue actor/object
-> opzionale ArcGraphInteractionSceneAdapterWrapper
```

Il componente resta spento di default.

### Gate principali

Il wrapper espone gate separati:

- `wrapperEnabled`: abilita il wrapper;
- `processInUpdate`: permette processing continuo, falso di default;
- `refreshSnapshots`: richiede refresh snapshot;
- `buildActorObjectQueue`: richiede costruzione queue actor/object;
- `pushQueueToInteractionWrapper`: consegna opzionalmente la queue al wrapper input;
- `enableInteractionWrapperAfterPush`: puo' accendere il wrapper interaction, falso di default.

### Confini preservati

Il wrapper:

- non crea `GameObject`;
- non crea renderer;
- non carica asset;
- non legge `SimulationHost`;
- non legge `MapGridWorldProvider`;
- non usa `FindObjectOfType`;
- non invia comandi;
- non possiede DevTools, top bar, click-to-move o pannelli avanzati;
- non salva scene o prefab.

### Diagnostica

La diagnostica espone:

- presenza del runtime adapter;
- presenza del wrapper interaction opzionale;
- stato dei gate;
- esito del coordinator;
- conteggio snapshot/queue tramite diagnostica del coordinator;
- eventuale push della queue al wrapper interaction;
- ragione sintetica del frame.

### QA

La ricerca statica sui nuovi file non rileva dipendenze operative vietate. Le
occorrenze trovate sono solo testo nei commenti o il nome Unity `OnDestroy`,
non chiamate operative distruttive.

`dotnet build Assembly-CSharp.csproj --no-restore` resta non conclusivo per la
mancanza del file temporaneo Unity:

```text
Temp/obj/Assembly-CSharp/project.assets.json
```

Non e' stato eseguito restore per evitare modifiche nelle cartelle temporanee.

### Stato dopo v0.38g.05

La struttura minima stabile e' ora piu' vicina alla chiusura perche' non dipende
piu' solo da probe manuali separati per costruire context, refresh snapshot,
queue actor/object e input wrapper.

Restano comunque fuori scope:

- renderer terrain produttivo;
- renderer actor/object produttivo;
- pooling;
- asset resolver obbligatorio;
- salvataggio scena;
- pensionamento MapGrid.

## Esito v0.38g.06 - ArcGraph Minimal Stable Structure Closeout

La `v0.38g.06` chiude la struttura minima stabile di ArcGraph come base
preparatoria per il primo runtime controllato terreno + NPC.

Questo closeout non introduce nuovo codice. Serve a fissare il confine tra:

```text
struttura minima ArcGraph pronta
-> primo runtime terrain + NPC ancora da implementare
-> pensionamento MapGrid ancora non autorizzato
```

### Struttura minima considerata pronta

ArcGraph dispone ora di:

- contratti coordinate, chunk, layer, dirty state e z-level;
- layer passivi foundation: Terrain, Object, Actor, Debug;
- placeholder passivi per Water, Vegetation, Light, Weather ed Effect;
- adapter read-only verso MapGrid/World;
- bootstrap runtime C# passivo;
- terrain snapshot e terrain mesh builder;
- policy zoom/LOD;
- actor/object snapshot;
- actor/object render queue;
- contratti actor/object scene-side;
- sprite resolver serializzato preparatorio;
- probe terrain validato;
- probe actor/object validato;
- boundary interazione;
- wrapper input Unity confinato;
- router consumer interattivi;
- Pointer HUD passivo;
- selection consumer;
- debug overlay queue/feed/wrapper/probe;
- coordinator runtime minimo;
- wrapper scena minimo per alimentare coordinator e interaction wrapper.

### Cosa significa "struttura minima stabile"

In questa fase "stabile" non significa "renderer definitivo".

Significa invece che ArcGraph ha una catena ordinata:

```text
dati runtime espliciti
-> context ArcGraph
-> bootstrap/coordinator
-> snapshot/layer
-> queue render
-> probe o wrapper controllati
```

La catena non deve piu' essere inventata ogni volta con componenti scollegati.
I dati arrivano tramite adapter dichiarati, la logica resta passiva e i punti
Unity sono frontiere sottili.

### Gate visuali e tecnici gia' recuperati

Sono considerati recuperati:

- terrain data-only: `TerrainSnapshotsBuilt`, `terrainSnapshots=16384`, `layerCount=4`;
- probe terrain visuale;
- probe actor/object visuale temporaneo;
- interaction base `wrapper -> router -> Pointer HUD + Selection`.

### Test manuali da rieseguire prima della promozione produttiva

Prima di dichiarare produttivo terrain + NPC servira' rieseguire in Unity:

1. `ArcGraphMinimalRuntimeSceneWrapper` con `wrapperEnabled = true`;
2. `ArcGraph/Process Minimal Runtime Frame`;
3. verifica log con context valido, snapshot terrain e queue actor/object;
4. push opzionale verso `ArcGraphInteractionSceneAdapterWrapper`;
5. verifica hover/cella/actor nel Pointer HUD;
6. verifica selection su click sinistro sopra NPC;
7. verifica che MapGrid continui a funzionare come renderer produttivo principale.

### Componenti non ancora produttivi

Restano non produttivi:

- `ArcGraphTerrainSceneProbeRenderer`;
- `ArcGraphActorObjectSceneProbeRenderer`;
- `ArcGraphInteractionRenderQueueWiringProbe`;
- tutti i renderer ambientali futuri;
- debug overlay scene probe;
- `OnGUI` temporaneo del Pointer HUD.

Questi componenti sono validi come probe, gate o diagnostica, non come sistema
grafico finale.

### Prossimo blocco operativo

Il prossimo blocco deve iniziare il runtime controllato terreno + NPC.

Nome consigliato:

```text
v0.38h - ArcGraph Terrain + NPC Minimal Runtime
```

Obiettivo:

- trasformare terrain e NPC da probe separati a primo runtime visuale controllato;
- mantenere MapGrid come renderer produttivo principale durante la transizione;
- introdurre renderer produttivi solo con lifecycle, pooling e cleanup dichiarati;
- non migrare ancora acqua, vegetazione, luci, meteo, incendi, DevTools o top bar;
- non cancellare MapGrid.

### Stop confermato

```text
Struttura minima ArcGraph chiusa.
Non dichiarare ArcGraph sistema grafico definitivo.
Non cancellare MapGrid.
Non avviare environment layers.
Non migrare DevTools o top bar.
Partire dal binomio terreno + NPC.
```

### Stop confermato

```text
Non cancellare MapGridWorldView.
Non cancellare MapGridBootstrap.
Non dichiarare ArcGraph renderer principale.
Non trasformare interaction validata in tool host operativo.
Non trasformare i probe temporanei in sistemi produttivi senza checkpoint dedicato.
```

---

#### v0.38h - ArcGraph Terrain + NPC Minimal Runtime

## Stato
APERTA / AUDIT OPERATIVO INIZIALE

## Obiettivo

Trasformare terrain e NPC da probe separati a primo runtime visuale ArcGraph
controllato, mantenendo MapGrid come renderer produttivo principale durante la
transizione.

Questa fase non deve introdurre acqua, vegetazione, luci, meteo, incendi,
DevTools, top bar o pensionamento MapGrid.

## Regola di transizione

Il runtime minimo terrain + NPC deve nascere come sistema gated:

```text
MapGrid resta renderer produttivo
ArcGraph riceve dati read-only
ArcGraph aggiorna renderer minimi controllati
ArcGraph espone diagnostica
MapGrid non viene cancellato
```

## Esito v0.38h.01 - Audit renderer produttivo minimo terrain + NPC

La `v0.38h.01` auditata i componenti ArcGraph gia' presenti che oggi
visualizzano terrain e actor/object.

File auditati:

- `ArcGraphTerrainSceneProbeRenderer`;
- `ArcGraphActorObjectSceneProbeRenderer`;
- `ArcGraphMinimalRuntimeSceneWrapper`;
- `ArcGraphTerrainChunkMeshBuilder`;
- `ArcGraphActorObjectSceneRenderPlanBuilder`.

### Stato terrain

Il terrain ArcGraph possiede gia':

- snapshot terrain;
- layer terrain;
- dirty chunk;
- builder mesh chunk;
- UV map derivabile dalla config MapGrid;
- probe scena validato.

Il probe terrain pero' non e' ancora un renderer produttivo perche':

- crea `GameObject` chunk temporanei;
- distrugge e ricrea il root nel cleanup;
- non possiede pooling stabile;
- non possiede lifecycle incrementale;
- non consuma direttamente il coordinator minimo;
- non dichiara ancora una policy runtime per quando aggiornare solo chunk sporchi;
- resta legato a context menu/manual probe.

### Stato NPC / actor

Actor/object ArcGraph possiede gia':

- snapshot actor/object;
- actor/object layer;
- render queue ordinata;
- render plan scene-side;
- sprite resolve request;
- sprite resolver serializzato preparatorio;
- probe scena validato;
- interazione base validata tramite queue.

Il probe actor/object pero' non e' ancora renderer produttivo perche':

- crea `SpriteRenderer` temporanei;
- non possiede pooling per entity id;
- non possiede lifecycle stabile per spawn/update/despawn;
- puo' usare fallback sprite generati;
- non impone ancora asset resolver reale;
- non gestisce label stock, balloon, collider, flash decisionale o overlay NPC;
- non e' ancora integrato in un runtime frame unico terrain + NPC.

### Stato wrapper minimo

`ArcGraphMinimalRuntimeSceneWrapper` e' il punto corretto da cui partire per il
runtime minimo perche':

- riceve un adapter dati esplicito;
- costruisce context runtime;
- chiama il coordinator;
- produce queue actor/object;
- puo' passare la queue all'interaction wrapper.

Pero' il wrapper non deve diventare renderer. Deve solo orchestrare e inoltrare
dati derivati a renderer separati.

### Decisione tecnica per v0.38h

Il prossimo blocco deve introdurre due renderer minimi distinti:

```text
ArcGraphTerrainRuntimeSceneRenderer
ArcGraphNpcRuntimeSceneRenderer
```

Nomi indicativi, da confermare nello step implementativo.

Entrambi devono essere:

- `MonoBehaviour` scena;
- spenti di default;
- alimentati da frame/queue espliciti;
- privi di accessi globali;
- privi di comandi;
- privi di salvataggio scena;
- dotati di diagnostica;
- dotati di root locale;
- dotati di cleanup confinato.

### Differenza tra probe e renderer runtime

Probe:

```text
click manuale
-> costruisce tutto
-> crea oggetti temporanei
-> serve a vedere se il dato funziona
```

Renderer runtime:

```text
frame controllato
-> riceve dati gia' preparati
-> aggiorna solo cio' che serve
-> riusa oggetti scena dove possibile
-> conserva diagnostica
```

### Ordine consigliato

1. Definire contratto renderer terrain runtime.
2. Implementare renderer terrain runtime minimo con root stabile e riuso chunk.
3. Definire contratto renderer NPC runtime.
4. Implementare renderer NPC runtime minimo con pool per actor id.
5. Collegare entrambi al wrapper minimo.
6. Preparare gate visuale Unity terrain + NPC.

### Fuori scope confermato

Restano fuori:

- acqua;
- vegetazione;
- luci;
- meteo;
- incendi;
- animazioni sprite;
- vestizione NPC;
- UI avanzata;
- DevTools;
- top bar;
- click-to-move;
- cancellazione MapGrid.

### Prossimo micro-step consigliato

```text
v0.38h.02 - ArcGraph Terrain Runtime Renderer Contract
```

Scopo:

- definire input, output e diagnostica del renderer terrain produttivo minimo;
- stabilire come riceve mesh chunk o layer terrain;
- decidere root, pooling e cleanup;
- non implementare ancora NPC nello stesso step;
- non cancellare probe terrain.

## Esito v0.38h.02 - ArcGraph Terrain Runtime Renderer Minimo

La `v0.38h.02` accelera contratto e implementazione del renderer terrain runtime
minimo nello stesso step.

### File introdotti

- `ArcGraphTerrainRuntimeSceneRendererContract`;
- `ArcGraphTerrainRuntimeSceneRendererDiagnostics`;
- `ArcGraphTerrainRuntimeSceneRenderer`.

### Funzione

Il nuovo renderer materializza chunk terrain ArcGraph in scena usando dati gia'
preparati da ArcGraph:

```text
ArcGraphRuntimeContext
-> ArcGraphBootstrapRuntime
-> ArcGraphTerrainLayer
-> ArcGraphTerrainChunkMeshBuilder
-> ArcGraphTerrainRuntimeSceneRenderer
```

Il renderer e' diverso dal probe terrain:

- il probe crea chunk temporanei per un gate manuale;
- il renderer runtime mantiene un root stabile;
- il renderer runtime conserva un pool locale di chunk;
- il renderer runtime riusa `GameObject` e `Mesh` per chunk gia' esistenti;
- il renderer runtime aggiorna solo chunk dirty.

### Entry point

Il renderer espone:

- `RenderFromMapGridRuntime`, per test manuale da adapter MapGrid;
- `RenderFromRuntime`, per il futuro collegamento col wrapper/coordinator;
- context menu `ArcGraph/Render Terrain Runtime From MapGrid`;
- context menu `ArcGraph/Clear Terrain Runtime Renderer`.

### Confini preservati

Il renderer:

- e' spento di default;
- non legge `SimulationHost`;
- non legge `MapGridWorldProvider`;
- non legge `MapGridWorldView`;
- non usa `FindObjectOfType`;
- non carica asset;
- non invia comandi;
- non salva scene o prefab;
- non cancella MapGrid;
- non sostituisce ancora `MapGridChunkRenderer`.

### Diagnostica

La diagnostica misura:

- renderer abilitato;
- contratto valido;
- context/config/mappa disponibili;
- runtime/render state/terrain layer disponibili;
- chunk dirty iniziali;
- chunk costruiti;
- chunk non vuoti;
- chunk applicati;
- oggetti chunk creati;
- oggetti chunk riusati;
- oggetti chunk disattivati;
- chunk attivi;
- fallback UV;
- dirty state pulito o meno.

### QA tecnica

La ricerca statica sui nuovi file non rileva dipendenze vietate.

`dotnet build Assembly-CSharp.csproj --no-restore` resta non conclusivo per la
mancanza del file temporaneo Unity:

```text
Temp/obj/Assembly-CSharp/project.assets.json
```

E' stato eseguito un controllo Roslyn isolato sui tre file nuovi usando assembly
Unity gia' disponibili. Il controllo e' riuscito con un solo warning atteso su
campo `SerializeField` assegnabile da Inspector.

### Stato dopo v0.38h.02

Terrain ora ha un primo renderer runtime minimo. Non e' ancora collegato in modo
automatico al wrapper minimo e non e' ancora stato validato in Unity come gate
visuale.

Il prossimo step accelerato puo' passare al renderer NPC minimo.

---

## Esito v0.38h.03 - ArcGraph NPC Runtime Renderer Minimo

La `v0.38h.03` accelera contratto e implementazione del renderer NPC runtime
minimo nello stesso step.

### File introdotti

- `ArcGraphNpcRuntimeSceneRendererContract`;
- `ArcGraphNpcRuntimeSceneRendererDiagnostics`;
- `ArcGraphNpcRuntimeSceneRenderer`.

### Funzione

Il nuovo renderer materializza gli NPC ArcGraph in scena usando una
`ArcGraphRenderQueue` gia' preparata dal percorso runtime:

```text
ArcGraphMinimalRuntimeSceneWrapper
-> ArcGraphMinimalRuntimeCoordinator
-> ArcGraphRenderQueue
-> ArcGraphActorObjectSceneRenderPlanBuilder
-> ArcGraphNpcRuntimeSceneRenderer
```

Il renderer non disegna oggetti generici. Filtra solo le entry `Actor` e lascia
gli object a un renderer dedicato futuro.

### Differenza rispetto al probe actor/object

Il probe actor/object resta un test temporaneo:

- costruisce un root di probe;
- crea sprite scene-side per validare il percorso;
- non mantiene un pool runtime pensato per uso stabile.

Il renderer NPC runtime invece:

- mantiene un root locale stabile;
- conserva un pool per `actorId`;
- riusa `GameObject` e `SpriteRenderer` per NPC gia' presenti;
- aggiorna posizione, sorting e sprite;
- disattiva opzionalmente gli actor non piu' presenti nella queue;
- e' pronto per essere collegato al wrapper minimo nello step successivo.

### Entry point

Il renderer espone:

- `RenderFromQueue`, per il futuro collegamento diretto col wrapper/coordinator;
- `RenderFromRuntimeWrapper`, per test manuale usando la queue gia' esposta dal wrapper;
- context menu `ArcGraph/Render NPC Runtime From Wrapper Queue`;
- context menu `ArcGraph/Clear NPC Runtime Renderer`.

### Sprite

Il renderer usa `IArcGraphSpriteResolver` se un resolver e' assegnato da
Inspector.

Se non esiste ancora uno sprite asset definitivo, il renderer puo' generare un
fallback magenta opzionale. Questo fallback serve solo a non bloccare il gate
visuale tecnico: non e' una soluzione artistica definitiva.

### Confini preservati

Il renderer:

- e' spento di default;
- non legge `SimulationHost`;
- non legge `MapGridWorldProvider`;
- non legge `MapGridWorldView`;
- non legge `NPCSelection`;
- non usa `FindObjectOfType`;
- non usa `Resources.Load`;
- non invia comandi;
- non salva scene o prefab;
- non cancella MapGrid;
- non renderizza oggetti generici;
- non sostituisce ancora `MapGridWorldView`.

### Diagnostica

La diagnostica misura:

- renderer abilitato;
- contratto valido;
- queue presente;
- resolver sprite presente;
- piano actor/object costruito;
- entry totali in queue;
- actor item presenti;
- actor entry pianificate;
- NPC renderizzati;
- oggetti NPC creati;
- oggetti NPC riusati;
- oggetti NPC disattivati;
- oggetti NPC attivi;
- sprite mancanti;
- fallback generati.

### QA tecnica

La ricerca statica sui nuovi file non rileva dipendenze vietate.

E' stato eseguito un controllo Roslyn isolato sui tre file nuovi usando assembly
Unity gia' disponibili. Il controllo e' riuscito con un solo warning atteso su
campo `SerializeField` assegnabile da Inspector.

### Stato dopo v0.38h.03

ArcGraph possiede ora:

- renderer runtime terrain minimo;
- renderer runtime NPC minimo;
- wrapper/coordinator minimo che gia' produce una queue actor/object;
- probe e gate precedenti ancora disponibili per confronto.

Manca ancora il cablaggio automatico tra wrapper/coordinator e renderer terrain +
NPC. Il prossimo step deve collegare questi componenti in modo esplicito, gated e
spento di default, preparando un gate visuale unico.

---

## Esito v0.38h.04 - ArcGraph Minimal Runtime Wiring + Gate

La `v0.38h.04` collega il wrapper runtime minimo ai renderer runtime terrain e
NPC.

### Funzione

Il flusso minimo ArcGraph puo' ora essere eseguito in un unico passaggio:

```text
ArcGraphTerrainRuntimeMapGridAdapter
-> ArcGraphRuntimeContext
-> ArcGraphMinimalRuntimeCoordinator
-> ArcGraphBootstrapRuntime
-> ArcGraphTerrainRuntimeSceneRenderer
-> ArcGraphNpcRuntimeSceneRenderer
-> ArcGraphInteractionSceneAdapterWrapper opzionale
```

Il wrapper non renderizza direttamente. Coordina soltanto:

- costruzione context;
- processing del coordinator;
- chiamata opzionale al renderer terrain;
- chiamata opzionale al renderer NPC;
- inoltro opzionale della queue al wrapper interattivo.

### Modifiche al wrapper

`ArcGraphMinimalRuntimeSceneWrapper` ora puo' ricevere da Inspector:

- `ArcGraphTerrainRuntimeSceneRenderer`;
- `ArcGraphNpcRuntimeSceneRenderer`.

Sono stati aggiunti gate separati:

- `renderTerrainRuntime`;
- `renderNpcRuntime`;
- `enableTerrainRendererBeforeRender`;
- `enableNpcRendererBeforeRender`.

Questi flag mantengono il comportamento spento di default. Il wrapper puo'
restare in scena senza creare mesh o sprite finche' l'operatore non abilita
esplicitamente il percorso.

### Sequenza runtime

Dopo il processing del coordinator:

- il terrain renderer riceve `context` e `ArcGraphBootstrapRuntime`;
- il renderer NPC riceve la `ArcGraphRenderQueue`;
- l'interaction wrapper puo' ancora ricevere la stessa queue se il relativo gate
  e' attivo.

Il terrain renderer usa quindi:

```text
ArcGraphRuntimeContext + ArcGraphBootstrapRuntime
```

Il renderer NPC usa invece:

```text
ArcGraphRenderQueue
```

Questa distinzione e' importante: il terreno lavora sui layer/chunk, gli NPC
lavorano sulla queue actor/object.

### Diagnostica

La diagnostica del wrapper ora espone anche:

- presenza terrain renderer;
- presenza NPC renderer;
- richiesta render terrain;
- richiesta render NPC;
- render terrain effettivamente chiamato;
- render NPC effettivamente chiamato;
- diagnostica nested del terrain renderer;
- diagnostica nested del renderer NPC.

Il log del wrapper include anche le ragioni dei renderer:

```text
terrainReason=...
npcReason=...
```

### Confini preservati

Il wrapper:

- non crea `GameObject`;
- non crea `Mesh`;
- non crea `SpriteRenderer`;
- non carica asset;
- non legge direttamente il World;
- non cerca oggetti in scena;
- non invia comandi;
- non salva scene o prefab;
- non sostituisce ancora MapGrid;
- non possiede pool terrain o NPC.

Le responsabilita' restano separate:

- coordinator: prepara runtime e queue;
- terrain renderer: materializza chunk terrain;
- NPC renderer: materializza actor/NPC;
- interaction wrapper: gestisce boundary/input se abilitato.

### QA tecnica

La ricerca statica sulle dipendenze vietate non rileva usi operativi. L'unica
occorrenza trovata e' dentro un commento architetturale gia' esistente.

Il controllo Roslyn isolato e' riuscito includendo wrapper, renderer terrain e
renderer NPC. I warning rilevati sono attesi:

- campi `SerializeField` assegnabili da Inspector;
- conflitti fittizi del controllo isolato, dovuti al confronto con
  `Assembly-CSharp.dll` gia' compilato.

### Stato dopo v0.38h.04

ArcGraph ha ora un percorso minimo unitario per:

- costruire dati runtime;
- aggiornare snapshot;
- costruire queue actor/object;
- renderizzare terreno;
- renderizzare NPC;
- inoltrare interaction in modo opzionale.

Il prossimo step non deve aggiungere feature: deve preparare ed eseguire il gate
visuale umano unico terrain + NPC runtime.

---

## Esito v0.38i.01 - ArcGraph Terrain Catalog Data-Driven

La `v0.38i.01` apre il ramo data-driven per completare progressivamente terreno
e NPC visuali in ArcGraph.

### Branch

Branch operativo:

```text
ai-task/v0.38i-arcgraph-data-driven-terrain-npc
```

### Obiettivo dello step

Separare la resa visuale del terreno dalla configurazione legacy MapGrid.

Prima di questo step il terrain renderer runtime costruiva la UV map usando le
`tileDefs` di `MapGridConfig`. Dopo questo step puo' invece ricevere un catalogo
ArcGraph dedicato.

### File introdotti

- `ArcGraphTerrainCatalogEntry`;
- `ArcGraphTerrainCatalog`;
- `ArcGraphTerrainCatalogJson`;
- `Assets/Resources/ArcGraph/Config/ArcGraphTerrainCatalog.json`.

### Struttura del catalogo JSON

Il file JSON contiene:

```text
terrainAtlasResourcePath
tilePixels
atlasWidthPixels
atlasHeightPixels
tiles[]
```

Ogni tile dichiara:

```text
id
name
uvX
uvY
```

Per ora il catalogo di esempio replica le due tile gia' presenti nella config
MapGrid:

- `0 -> iron_barren`;
- `1 -> rocky`.

### Uso nel renderer terrain

`ArcGraphTerrainRuntimeSceneRenderer` ora possiede un campo:

```text
terrainCatalogJson
```

Il campo e' un `TextAsset` assegnabile da Inspector.

Se il catalogo e' assegnato e valido:

```text
TextAsset JSON
-> ArcGraphTerrainCatalogJson
-> ArcGraphTerrainCatalog
-> ArcGraphTerrainTileUvMap
-> ArcGraphTerrainChunkMeshBuilder
```

Se il catalogo manca o non e' valido, il renderer resta compatibile con il
fallback precedente basato su `MapGridConfig`.

### Confine importante

Questo step non migra ancora la conformazione della mappa.

Quindi:

- la mappa/celle possono ancora arrivare da `MapGridData`;
- lo sprite/UV del terreno puo' arrivare dal catalogo ArcGraph;
- il renderer non dipende piu' necessariamente dalle `tileDefs` MapGrid per la
  resa visuale.

Questa separazione prepara il passaggio successivo:

```text
ArcGraph map data file
-> celle terrain
-> tileId
-> ArcGraph terrain catalog
-> sprite/UV
```

### Performance

Il JSON non viene parsato a ogni frame.

Il renderer conserva:

- ultimo catalogo parsato;
- ultimo testo sorgente.

Se il `TextAsset` non cambia, il catalogo runtime viene riusato.

### Confini preservati

Il parser:

- non usa `Resources.Load`;
- non carica texture;
- non crea materiali;
- non cerca oggetti in scena;
- non tocca World;
- non tocca MapGrid;
- non invia comandi.

Il renderer:

- usa il catalogo solo per costruire la UV map;
- mantiene fallback legacy;
- non salva scene;
- non elimina MapGrid.

### QA tecnica

Il JSON e' stato validato con `ConvertFrom-Json` PowerShell:

```text
MapGrid/Atlas/TerrainAtlas 32 tiles=2
```

Il controllo Roslyn isolato e' riuscito includendo il modulo Unity
`UnityEngine.JSONSerializeModule`.

Warning atteso:

- campo `SerializeField` assegnabile da Inspector.

La ricerca statica non rileva usi operativi di dipendenze vietate. L'unica
occorrenza di `Resources.Load` e' in un commento che spiega che il parser non lo
usa.

### Stato dopo v0.38i.01

Il terreno ArcGraph ha ora:

- dati cella ancora compatibili col ponte MapGrid;
- renderer runtime a chunk;
- catalogo visuale JSON ArcGraph;
- fallback compatibile con `MapGridConfig`.

Il prossimo step puo' essere uno tra:

- catalogo/file della conformazione mappa ArcGraph;
- catalogo visuale NPC modulare;
- gate visuale terrain + NPC runtime se l'operatore puo' testare Unity.

---

## Esito v0.38i.02 - ArcGraph NPC Visual Catalog Data-Driven

La `v0.38i.02` introduce il primo catalogo visuale data-driven per NPC modulari.

### Obiettivo dello step

Preparare la futura costruzione dell'NPC a layer:

```text
NPC root
├─ body
├─ head
├─ legs
└─ feet
```

Questo step non modifica ancora il renderer NPC. Definisce prima il formato dati
che il renderer usera' nello step successivo.

### File introdotti

- `ArcGraphNpcVisualFrame`;
- `ArcGraphNpcVisualCatalog`;
- `ArcGraphNpcVisualCatalogJson`;
- `Assets/Resources/ArcGraph/Config/ArcGraphNpcVisualCatalog.json`.

### Struttura del catalogo

Il catalogo JSON contiene:

```text
defaultVisualKey
defaultAnimationKey
pixelsPerUnit
parts[]
frames[]
```

Le parti iniziali sono:

```text
body
head
legs
feet
```

Le direzioni iniziali sono:

```text
north
south
east
west
```

Le animazioni iniziali sono:

```text
idle
walk
```

### Frame

Ogni frame dichiara:

```text
visualKey
partKey
directionKey
animationKey
frameIndex
spriteKey
durationTicks
sortingOffset
```

Il catalogo di esempio contiene 48 frame:

- `idle`: 4 direzioni x 1 frame x 4 parti = 16 frame;
- `walk`: 4 direzioni x 2 frame x 4 parti = 32 frame.

### Naming sprite previsto

Il catalogo prepara questa convenzione:

```text
ArcGraph/NPC/human_default/body/south_idle_00
ArcGraph/NPC/human_default/head/south_idle_00
ArcGraph/NPC/human_default/legs/south_idle_00
ArcGraph/NPC/human_default/feet/south_idle_00
```

Per la camminata:

```text
ArcGraph/NPC/human_default/body/south_walk_00
ArcGraph/NPC/human_default/body/south_walk_01
```

Lo stesso schema vale per:

```text
north
south
east
west
```

### Confine importante

Il catalogo non contiene asset Unity.

Contiene solo `spriteKey`.

La risoluzione:

```text
spriteKey -> Sprite Unity
```

resta responsabilita' di un resolver scene-side.

### Performance

Il catalogo runtime costruisce un dizionario interno:

```text
visualKey|partKey|directionKey|animationKey|frameIndex -> frame
```

Questo evita che il renderer debba scansionare tutti i frame a ogni update.

### Confini preservati

Il parser:

- non usa `Resources.Load`;
- non crea sprite;
- non crea GameObject;
- non modifica World;
- non modifica job;
- non tocca MapGrid;
- non salva scene o prefab.

### QA tecnica

Il JSON e' stato validato con `ConvertFrom-Json` PowerShell:

```text
human_default parts=4 frames=48
```

Il controllo Roslyn isolato e' riuscito includendo il modulo Unity
`UnityEngine.JSONSerializeModule`.

La ricerca statica non rileva usi operativi di dipendenze vietate. L'unica
occorrenza di `Resources.Load` e' in un commento che spiega che il parser non lo
usa.

### Stato dopo v0.38i.02

ArcGraph possiede ora:

- catalogo terrain da file;
- catalogo visuale NPC da file;
- convenzione di naming per sprite NPC modulari;
- base dati per animazione a 4 direzioni.

Il prossimo step deve collegare questo catalogo al renderer NPC, trasformando
l'attuale sprite singolo in handle modulare corpo/testa/gambe/piedi.

---

## Esito v0.38i.03 - ArcGraph NPC Layered Runtime Renderer

La `v0.38i.03` collega il catalogo visuale NPC al renderer runtime NPC.

### Obiettivo dello step

Trasformare il renderer NPC da solo sprite singolo a renderer capace di montare
un NPC da piu' parti:

```text
NPC root
├─ body renderer
├─ head renderer
├─ legs renderer
└─ feet renderer
```

Il comportamento resta gated e spento di default.

### Modifiche principali

`ArcGraphNpcRuntimeSceneRenderer` ora possiede:

```text
npcVisualCatalogJson
useLayeredActorCatalog
```

`npcVisualCatalogJson` e' un `TextAsset` assegnabile da Inspector.

`useLayeredActorCatalog` abilita il tentativo di rendering modulare.

Se il flag e' spento, il renderer continua a usare lo sprite singolo precedente.

### ActorHandle

L'handle runtime NPC mantiene ora:

```text
GameObject root actor
SpriteRenderer singolo fallback
Dictionary<string, SpriteRenderer> partRenderers
```

Questo permette di riusare i renderer delle parti senza crearli ogni frame.

### Flusso modulare

Quando il renderer riceve una entry actor:

```text
entry actor
-> direzione
-> animazione
-> frame index
-> catalogo NPC
-> frame per body/head/legs/feet
-> sprite resolver
-> SpriteRenderer per parte
```

Se il catalogo non e' valido o non produce parti renderizzabili, il renderer torna
al fallback a sprite singolo.

### Direzione

La direzione viene risolta in modo semplice:

- se non c'e' motion: `south`;
- se c'e' motion: confronto tra posizione visuale e cella discreta;
- asse principale X: `east` / `west`;
- asse principale Y: `north` / `south`.

Questa e' una soluzione minima. Una direzione esplicita read-only potra' essere
aggiunta in futuro allo snapshot actor.

### Animazione

La scelta animazione e':

```text
motion attivo -> walk
nessun motion -> idle
```

Per `walk`, il frame index iniziale usa:

```text
motionProgress01 < 0.5 -> frame 0
motionProgress01 >= 0.5 -> frame 1
```

Questa e' una prima animazione leggera a due frame. Non usa ancora un timer
grafico indipendente.

### Sorting

Ogni parte usa:

```text
entry.SortingOrder + visualFrame.SortingOffset
```

Il catalogo iniziale usa:

```text
body = 0
legs = 1
feet = 2
head = 3
```

### Diagnostica

La diagnostica NPC ora espone anche:

- `LayeredActorCount`;
- `CreatedPartRendererCount`;
- `ReusedPartRendererCount`;
- `MissingCatalogFrameCount`.

Il log del renderer include:

```text
layeredActors
createdParts
reusedParts
missingCatalogFrames
```

### Confini preservati

Il renderer:

- non carica sprite direttamente;
- non usa `Resources.Load`;
- non legge World;
- non modifica job o movimento;
- non cerca MapGrid;
- non salva scena;
- non richiede asset reali per compilare.

La risoluzione sprite resta demandata a `IArcGraphSpriteResolver`.

### QA tecnica

Il controllo Roslyn isolato e' riuscito includendo:

- catalogo NPC;
- parser JSON;
- renderer NPC;
- diagnostica renderer NPC;
- modulo Unity JSONSerialize.

Warning attesi:

- campi `SerializeField` assegnabili da Inspector.

La ricerca statica non rileva usi operativi di dipendenze vietate. L'unica
occorrenza di `Resources.Load` e' in un commento esplicativo del parser JSON.

### Stato dopo v0.38i.03

ArcGraph puo' ora:

- leggere catalogo terrain da file;
- leggere catalogo NPC visuale da file;
- usare renderer NPC a sprite singolo;
- usare renderer NPC modulare corpo/testa/gambe/piedi se abilitato;
- scegliere direzione base a 4 sensi;
- scegliere animazione `idle` o `walk` minima.

Mancano ancora:

- asset sprite reali nelle path dichiarate;
- resolver configurato con quegli asset;
- gate visuale Unity del rendering modulare;
- eventuale direzione actor esplicita nello snapshot invece dell'inferenza minima.

---

## Esito v0.38i.04 - ArcGraph F12 View Switch + NPC Animation Frame Counts

La `v0.38i.04` introduce il primo switch visuale esplicito tra MapGrid e
ArcGraph e aggiorna il sistema NPC modulare ai conteggi frame richiesti.

### Obiettivo dello step

Rendere possibile un confronto runtime rapido:

```text
F12
-> MapGrid visibile / ArcGraph spento
-> ArcGraph visibile / MapGrid spento
```

Lo switch riguarda solo la visualizzazione. Non ferma la simulazione, non cambia
la sorgente dati e non cancella MapGrid.

### Switch visuale

E' stato introdotto:

```text
ArcGraphViewModeSwitcher
```

Il componente usa:

```text
toggleKey = F12
```

Da Inspector riceve:

- radici visuali MapGrid;
- radici visuali ArcGraph;
- `ArcGraphMinimalRuntimeSceneWrapper`;
- `ArcGraphTerrainRuntimeSceneRenderer`;
- `ArcGraphNpcRuntimeSceneRenderer`.

Quando passa ad ArcGraph:

- spegne le radici visuali MapGrid assegnate;
- accende le radici visuali ArcGraph assegnate;
- abilita wrapper e renderer runtime;
- opzionalmente abilita il processing in `Update`;
- opzionalmente processa subito un frame ArcGraph.

Quando torna a MapGrid:

- riaccende le radici MapGrid;
- spegne le radici ArcGraph;
- disabilita wrapper e renderer ArcGraph;
- opzionalmente pulisce i renderer runtime ArcGraph.

### Confine importante

Lo switcher non usa:

- `FindObjectOfType`;
- `SimulationHost`;
- accessi globali;
- comandi verso NPC;
- modifiche al World;
- salvataggio scena;
- cancellazione legacy.

Tutto avviene tramite riferimenti espliciti assegnati da Inspector.

### Animazioni NPC

Il catalogo visuale NPC passa a:

```text
idle = 4 frame
walk = 9 frame
```

Per ogni:

```text
4 parti: body, head, legs, feet
4 direzioni: south, north, east, west
```

Il totale runtime generato e':

```text
idle: 4 parti x 4 direzioni x 4 frame = 64 frame
walk: 4 parti x 4 direzioni x 9 frame = 144 frame
totale = 208 frame
```

Il JSON non elenca piu' tutti i 208 frame manualmente. Usa `framePatterns`, che
generano i frame runtime al caricamento del catalogo.

### Movimento e frame walk

La camminata usa:

```text
MotionProgress01
```

Esempio:

- progresso 0.00 -> frame 0;
- progresso 0.50 -> frame circa centrale;
- progresso 0.99 -> ultimo frame;
- progresso 1.00 -> ultimo frame clampato.

Questo collega l'animazione walk al movimento multi-tick tra celle.

### Idle

L'idle non ha progresso di movimento. Per questo usa un contatore visuale locale:

```text
idleFrameStep
```

Default:

```text
12 frame Unity per cambio frame idle
```

Il costo e' minimo: nessuna allocazione e nessun accesso al World.

### PNG richiesti

Per il visuale iniziale `human_default` servono 208 sprite PNG.

Dimensione consigliata:

```text
32 x 48 px
```

Nota aggiornata in `v0.38i.05`: il terreno resta `32x32`, mentre i frame NPC
usano canvas `32x48`.

Import Unity consigliato:

```text
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 32
Filter Mode: Point
Compression: None
```

Percorsi logici attesi dal catalogo:

```text
ArcGraph/NPC/human_default/body/{direction}_{animation}_{frame00}
ArcGraph/NPC/human_default/head/{direction}_{animation}_{frame00}
ArcGraph/NPC/human_default/legs/{direction}_{animation}_{frame00}
ArcGraph/NPC/human_default/feet/{direction}_{animation}_{frame00}
```

In `Resources`, questi sprite dovranno essere collocati idealmente sotto:

```text
Assets/Resources/ArcGraph/NPC/human_default/body/
Assets/Resources/ArcGraph/NPC/human_default/head/
Assets/Resources/ArcGraph/NPC/human_default/legs/
Assets/Resources/ArcGraph/NPC/human_default/feet/
```

Nomi idle richiesti per ogni parte:

```text
south_idle_00.png
south_idle_01.png
south_idle_02.png
south_idle_03.png
north_idle_00.png
north_idle_01.png
north_idle_02.png
north_idle_03.png
east_idle_00.png
east_idle_01.png
east_idle_02.png
east_idle_03.png
west_idle_00.png
west_idle_01.png
west_idle_02.png
west_idle_03.png
```

Nomi walk richiesti per ogni parte:

```text
south_walk_00.png
south_walk_01.png
south_walk_02.png
south_walk_03.png
south_walk_04.png
south_walk_05.png
south_walk_06.png
south_walk_07.png
south_walk_08.png
north_walk_00.png
north_walk_01.png
north_walk_02.png
north_walk_03.png
north_walk_04.png
north_walk_05.png
north_walk_06.png
north_walk_07.png
north_walk_08.png
east_walk_00.png
east_walk_01.png
east_walk_02.png
east_walk_03.png
east_walk_04.png
east_walk_05.png
east_walk_06.png
east_walk_07.png
east_walk_08.png
west_walk_00.png
west_walk_01.png
west_walk_02.png
west_walk_03.png
west_walk_04.png
west_walk_05.png
west_walk_06.png
west_walk_07.png
west_walk_08.png
```

La lista sopra va ripetuta in ciascuna cartella parte:

```text
body
head
legs
feet
```

### JSON da modificare in futuro

Il file interessato e':

```text
Assets/Resources/ArcGraph/Config/ArcGraphNpcVisualCatalog.json
```

Per cambiare numero frame:

```text
"animationKey": "idle",
"frameCount": 4
```

oppure:

```text
"animationKey": "walk",
"frameCount": 9
```

Per cambiare convenzione nome asset:

```text
"spriteKeyPattern": "ArcGraph/NPC/{visual}/{part}/{direction}_{animation}_{frame00}"
```

Per cambiare l'ordine visivo delle parti:

```text
"sortingOffsets": [0, 3, 1, 2]
```

Con l'ordine attuale:

- body = 0;
- head = 3;
- legs = 1;
- feet = 2.

### QA tecnica

Il JSON e' stato validato con `ConvertFrom-Json`.

Il build `dotnet build --no-restore` non e' stato eseguito con successo perche'
manca:

```text
Temp/obj/Assembly-CSharp/project.assets.json
```

Non e' stato lanciato `restore`, per non scrivere in `Temp`.

### Stato dopo v0.38i.04

ArcGraph possiede ora:

- switch visuale F12 MapGrid/ArcGraph;
- renderer terrain runtime minimo;
- renderer NPC runtime minimo;
- NPC modulare a parti;
- catalogo NPC compatto con `idle` a 4 frame;
- catalogo NPC compatto con `walk` a 9 frame;
- animazione walk agganciata a `MotionProgress01`;
- animazione idle ciclica leggera.

Restano da validare in Unity:

- cablaggio Inspector dello switcher;
- scelta delle radici MapGrid da spegnere;
- scelta delle radici ArcGraph da accendere;
- comportamento F12 in Play Mode;
- assegnazione sprite reali o resolver;
- resa visiva del movimento NPC.

---

## Esito v0.38i.05 - ArcGraph NPC Visual Asset Contract Refinement

La `v0.38i.05` assesta il contratto visuale degli NPC prima del gate con sprite
reali.

### Obiettivo dello step

Chiarire e implementare tre punti emersi dopo la prima definizione del catalogo
NPC:

- il terreno resta basato su tile `32x32`;
- l'NPC usa frame `32x48`;
- gli NPC devono avere un'ombra visuale separata;
- il numero di frame dell'animazione non deve imporre la durata simulativa del
  movimento.

### Dimensione terreno e NPC

Il contratto corretto diventa:

```text
tile terreno = 32x32 px
frame NPC = 32x48 px
pixelsPerUnit = 32
```

Quindi, in unita' mondo Unity:

```text
NPC largo = 1 cella
NPC alto = 1.5 celle
```

Il catalogo NPC dichiara ora:

```json
"frameWidthPixels": 32,
"frameHeightPixels": 48
```

Questi valori non cambiano il terreno. Descrivono solo il canvas previsto per le
parti visuali NPC.

### Parti NPC

Per il primo contratto stabile, ogni parte usa lo stesso canvas:

```text
body = PNG 32x48
head = PNG 32x48
legs = PNG 32x48
feet = PNG 32x48
```

Ogni PNG contiene solo la propria parte visibile e lascia il resto trasparente.

Esempio:

```text
body/south_idle_00.png -> corpo nella posizione corretta dentro canvas 32x48
head/south_idle_00.png -> testa nella posizione corretta dentro canvas 32x48
legs/south_idle_00.png -> gambe nella posizione corretta dentro canvas 32x48
feet/south_idle_00.png -> piedi nella posizione corretta dentro canvas 32x48
```

Questa scelta evita per ora offset per parte/frame.

Gli offset potranno essere aggiunti in futuro se si decidera' di usare sprite
ritagliati piccoli invece di canvas comuni.

### Ombra NPC

Il renderer NPC ora supporta una ombra visuale separata.

La gerarchia logica diventa:

```text
NPC root
├─ shadow renderer
├─ body renderer
├─ head renderer
├─ legs renderer
└─ feet renderer
```

L'ombra:

- segue il root NPC;
- usa offset locale configurabile;
- usa scala locale configurabile;
- usa tint/alpha configurabile;
- viene disegnata con sorting sotto le parti NPC.

Sprite key prevista per una ombra reale:

```text
ArcGraph/NPC/common/shadow/soft_ellipse_32x16
```

Path asset consigliata:

```text
Assets/Resources/ArcGraph/NPC/common/shadow/soft_ellipse_32x16.png
```

Dimensione consigliata:

```text
32x16 px
```

Se la sprite non viene risolta dal resolver, il renderer puo' generare una
ellisse fallback semitrasparente una sola volta e riusarla per tutti gli NPC.

### Frame animazione e tick movimento

Resta confermata la separazione:

```text
tick simulativo = durata reale del movimento
frame animazione = rappresentazione visuale del movimento
```

Quindi una animazione `walk` da 8 o 9 frame non obbliga il movimento a durare 8
o 9 tick.

Esempio:

```text
walk = 8 frame
movimento = 4 tick
-> alcuni frame possono essere saltati

walk = 8 frame
movimento = 16 tick
-> alcuni frame possono restare visibili per piu' tick
```

ArcGraph usa:

```text
MotionProgress01
```

per distribuire i frame lungo il movimento effettivo.

Questo preserva il principio:

```text
la simulazione decide il tempo
la grafica si adatta al tempo simulativo
```

### Confini preservati

Lo step:

- non modifica World;
- non modifica movimento simulativo;
- non modifica Job Layer;
- non modifica Decision Layer;
- non salva scene;
- non salva prefab;
- non rimuove MapGrid;
- non introduce asset load nel core ArcGraph.

### Stato dopo v0.38i.05

ArcGraph NPC possiede ora:

- catalogo data-driven con frame NPC `32x48`;
- parti NPC modulari su canvas comune;
- walk frame agganciati a `MotionProgress01`;
- idle frame agganciati a timer visuale leggero;
- ombra NPC separata, configurabile e con fallback generato.

Prossimo step consigliato:

```text
v0.38i.06 - ArcGraph NPC Sprite Resolver Practical Wiring
```

Motivo: con 208 PNG per un solo set visuale, il mapping manuale in Inspector non
e' praticabile come soluzione di lavoro ordinaria.

---

## Esito v0.38i.06 - ArcGraph NPC Sprite Resolver Practical Wiring

La `v0.38i.06` rende praticabile l'uso degli sprite NPC modulari reali senza
obbligare l'operatore a trascinare manualmente centinaia di PNG nel resolver
Unity.

### Obiettivo dello step

Il problema operativo era questo:

```text
4 parti NPC
4 direzioni
idle 4 frame
walk 9 frame
= 208 PNG per un solo set visuale
```

Un mapping manuale in Inspector e' utile per override piccoli, ma non e'
sostenibile come metodo ordinario per ogni frame.

Lo step introduce quindi un lookup automatico da `Assets/Resources`, mantenendo
pero' il caricamento asset dentro il bordo scena e fuori dal core passivo
ArcGraph.

### Modello di risoluzione sprite

`ArcGraphSerializedSpriteResolver` ora risolve una richiesta sprite in questo
ordine:

```text
1. mapping manuale da Inspector
2. lookup opzionale in Assets/Resources
3. fallback actor/object
4. fallimento diagnostico
```

Il mapping manuale resta il primo livello perche' permette override mirati.

Esempio:

```text
spriteKey richiesta:
ArcGraph/NPC/human_default/body/south_idle_00

path file atteso:
Assets/Resources/ArcGraph/NPC/human_default/body/south_idle_00.png
```

Unity carica da `Resources` usando il path senza estensione:

```text
ArcGraph/NPC/human_default/body/south_idle_00
```

### Cache

Il resolver mantiene cache locali per:

- hit manuali da Inspector;
- hit da `Resources`;
- miss da `Resources`;
- fallback usati.

Questo evita di ripetere ogni frame lookup falliti sugli stessi path mancanti.

Il principio e' semplice:

```text
se uno sprite esiste, lo ricordo
se uno sprite manca, ricordo anche il miss
```

In questo modo il gate visuale puo' essere fatto anche con set incompleti di
asset senza trasformare ogni frame in una scansione ripetuta.

### Menu di servizio

Il resolver espone il context menu:

```text
ArcGraph/Clear Sprite Resolver Runtime Cache
```

Serve durante i test Unity quando si importano o si sostituiscono PNG mentre la
scena e' aperta.

### Confini architetturali preservati

Lo step:

- non modifica World;
- non modifica Decision Layer;
- non modifica Job Layer;
- non modifica movimento;
- non salva scene;
- non salva prefab;
- non elimina MapGrid;
- non carica asset dentro builder passivi;
- non trasforma ArcGraph in sorgente di verita' simulativa.

Il caricamento `Resources.Load<Sprite>` e' presente solo nel resolver
scene-side:

```text
ArcGraphSerializedSpriteResolver
```

Cataloghi, snapshot, queue e builder ArcGraph restano data-only.

### Stato dopo v0.38i.06

ArcGraph NPC possiede ora:

- catalogo visuale data-driven;
- frame `32x48`;
- parti `body/head/legs/feet`;
- idle a 4 frame;
- walk a 9 frame;
- ombra NPC separata;
- resolver sprite pratico da `Resources`;
- fallback manuali ancora disponibili;
- cache hit/miss per ridurre lavoro inutile.

Prossimo step consigliato:

```text
gate visuale Unity con sprite reali minimi o set completo
```

Il test consigliato e':

```text
1. mettere alcuni PNG reali sotto Assets/Resources/ArcGraph/NPC/...
2. attivare Use Layered Actor Catalog
3. assegnare ArcGraphSerializedSpriteResolver
4. lasciare Enable Resources Lookup attivo
5. entrare in Play Mode
6. premere F12
7. verificare MissingSprites, fallback e frame visibili
```

---

## Esito v0.38i.07 - ArcGraph NPC Sprite Resource Probe

La `v0.38i.07` aggiunge un probe tecnico per verificare le sprite NPC prima del
gate visuale Unity.

### Perche' serve

Dopo la `v0.38i.06`, il resolver puo' caricare sprite da `Assets/Resources`.
Resta pero' un problema pratico:

```text
se l'NPC non appare, il problema e' nel renderer, nel catalogo, nei path PNG o negli import settings?
```

Per evitare un gate visuale cieco, e' stato aggiunto un componente separato:

```text
ArcGraphNpcSpriteResourceProbe
```

Questo componente non disegna nulla. Serve solo a rispondere alla domanda:

```text
le sprite key dichiarate dal catalogo NPC sono risolvibili dal resolver?
```

### Funzionamento

Il probe riceve da Inspector:

- `Npc Visual Catalog Json`;
- un componente `Sprite Resolver Behaviour`.

Poi, tramite menu contestuale:

```text
ArcGraph/Probe NPC Sprite Resources
```

esegue questo controllo:

```text
catalogo NPC
-> frame espansi
-> sprite key dichiarate
-> resolver sprite
-> conteggio sprite risolte/mancanti
```

### Diagnostica

La diagnostica prodotta contiene:

- presenza catalogo JSON;
- parse catalogo riuscito o fallito;
- presenza resolver;
- numero frame del catalogo;
- numero sprite key controllate;
- numero sprite key vuote;
- numero sprite risolte;
- numero sprite mancanti;
- prima sprite key mancante;
- ragione sintetica.

Esempio di esito positivo atteso con set completo:

```text
reason=NpcSpriteResourcesReady
catalogFrames=208
checkedSpriteKeys=208
resolvedSprites=208
missingSprites=0
```

Esempio di esito utile con asset mancanti:

```text
reason=NpcSpriteResourcesIncomplete
catalogFrames=208
checkedSpriteKeys=208
resolvedSprites=64
missingSprites=144
firstMissing='ArcGraph/NPC/human_default/head/north_walk_03'
```

### Confini architetturali

Lo step:

- non crea `GameObject`;
- non crea `SpriteRenderer`;
- non disegna;
- non legge `World`;
- non legge `MapGridWorldView`;
- non usa `FindObjectOfType`;
- non invia comandi;
- non usa `Resources.Load` direttamente;
- non modifica la simulazione.

Il caricamento asset resta nel resolver scene-side introdotto in `v0.38i.06`.

### Stato dopo v0.38i.07

ArcGraph possiede ora un controllo tecnico oggettivo prima del gate visuale NPC.

Questo non significa che il gate visuale sia superato.

Significa invece:

```text
prima controlliamo path e risoluzione sprite
poi testiamo resa visiva, sorting, ombra e movimento
```

Prossimo step consigliato:

```text
v0.38i.08 - stabilizzazione renderer NPC:
fallback, missing sprites, sorting parti, ombra e diagnostica
```

---

## Esito v0.38i.08 - ArcGraph NPC Runtime Renderer Stabilization

La `v0.38i.08` stabilizza un comportamento pratico del renderer NPC modulare
prima del gate visuale con sprite reali.

### Problema individuato

Il renderer NPC mantiene un pool di `SpriteRenderer` per ogni actor.

Nel percorso modulare, ogni NPC puo' avere parti separate:

```text
body
head
legs
feet
shadow
```

Se in un frame precedente una parte era stata disegnata, ma nel frame corrente
quella stessa parte non viene risolta dal catalogo o dal resolver, esiste un
rischio visivo:

```text
la vecchia parte resta accesa
```

Esempio:

```text
frame 1:
body ok
head ok
legs ok
feet ok

frame 2:
body ok
head mancante
legs ok
feet ok

rischio precedente:
head del frame 1 resta visibile
```

### Correzione

Nel percorso `TryApplyLayeredActorEntry`, prima di applicare il frame modulare
corrente, il renderer ora spegne tutte le parti gia' presenti nel pool
dell'actor.

Poi riaccende solo le parti realmente risolte nel frame corrente.

Il comportamento diventa:

```text
inizio render actor modulare
-> spegni tutte le parti vecchie
-> risolvi frame corrente
-> accendi solo body/head/legs/feet disponibili
```

Questo evita residui visuali durante:

- asset mancanti;
- frame mancanti nel catalogo;
- fallback parziali;
- cambio direzione;
- cambio animazione;
- test con set PNG incompleto.

### Supporto diagnostico

Aggiunto menu contestuale:

```text
ArcGraph/Log Last NPC Runtime Diagnostics
```

Serve a ristampare l'ultima diagnostica del renderer NPC senza rieseguire il
render.

Questo e' utile durante il gate Unity quando si vuole rileggere in Console:

- actor renderizzati;
- actor layered;
- parti create;
- parti riusate;
- sprite mancanti;
- fallback generati;
- frame catalogo mancanti.

### Confini preservati

Lo step:

- non modifica `World`;
- non modifica Decision Layer;
- non modifica Job Layer;
- non modifica movimento;
- non modifica MapGrid;
- non salva scene;
- non salva prefab;
- non introduce asset load nel renderer;
- non cambia il resolver;
- non cambia il catalogo NPC.

### Stato dopo v0.38i.08

Il renderer NPC modulare e' piu' robusto in presenza di asset incompleti.

Il gate visuale resta da eseguire, ma ora un set sprite incompleto non dovrebbe
lasciare residui di parti vecchie accese tra frame successivi.

Prossimo step consigliato:

```text
v0.38i.09 - verifica e rifinitura animazione NPC:
idle 4 frame, walk 9 frame, direzione e motion progress
```

---

## Esito v0.38i.09 - ArcGraph NPC Direction Persistence

La `v0.38i.09` rifinisce il passaggio tra animazione di camminata e animazione
idle degli NPC modulari.

### Problema individuato

Il renderer NPC modulare sceglie la direzione della camminata usando il movimento
visuale.

Quando l'NPC e' in movimento:

```text
dx/dy del movimento
-> north / south / east / west
-> frame walk coerente
```

Quando pero' l'NPC non e' in movimento, prima di questo step il renderer non
aveva un vettore da cui ricavare la direzione e tornava sempre a:

```text
south
```

Questo poteva produrre uno scatto visivo.

Esempio:

```text
NPC cammina verso est
-> usa walk east

NPC si ferma
-> prima tornava subito idle south
```

### Correzione

Ogni handle NPC del pool mantiene ora:

```text
LastDirection
```

Quando l'NPC si muove, ArcGraph calcola la direzione e la salva:

```text
walk north -> LastDirection = north
walk east  -> LastDirection = east
walk west  -> LastDirection = west
```

Quando l'NPC non si muove, l'idle usa l'ultima direzione nota:

```text
idle -> usa LastDirection
```

Quindi:

```text
walk east
-> idle east
```

invece di:

```text
walk east
-> idle south
```

### Confini preservati

La direzione persistente e' solo stato visuale locale del renderer.

Non modifica:

- posizione NPC;
- movimento simulativo;
- `World`;
- snapshot actor;
- Decision Layer;
- Job Layer;
- MapGrid;
- catalogo NPC;
- resolver sprite.

### Stato dopo v0.38i.09

Il renderer NPC modulare e' piu' coerente nel passaggio:

```text
walk -> idle
```

Resta da validare in Unity:

- cambio frame idle 4 frame;
- cambio frame walk 9 frame;
- direzione corretta durante movimento;
- idle nella stessa direzione dell'ultima camminata;
- compatibilita' con sprite reali.

Prossimo step consigliato:

```text
v0.38i.10 - terrain data-driven reale
```

oppure gate visuale Unity NPC se gli asset minimi sono pronti.

---

## Esito v0.38i.10 - ArcGraph Terrain UV Source Diagnostics

La `v0.38i.10` rende verificabile quale sorgente UV sta usando il renderer terrain
runtime di ArcGraph.

### Contesto

Il terreno ArcGraph ha gia' un catalogo visuale:

```text
Assets/Resources/ArcGraph/Config/ArcGraphTerrainCatalog.json
```

Questo catalogo descrive:

- atlas terrain;
- dimensione tile;
- tile id;
- coordinate UV nell'atlas.

La conformazione della mappa, invece, non e' ancora stata migrata a un modello
mappa ArcGraph definitivo.

Per ora resta valida questa separazione:

```text
MapGridData / adapter temporaneo
-> dice quale tile id sta in ogni cella

ArcGraphTerrainCatalog
-> dice dove quel tile id sta nell'atlas
```

### Problema

Prima di questo step, dal log non era immediatamente chiaro se il renderer stesse
usando:

```text
catalogo ArcGraph
```

oppure:

```text
fallback legacy MapGridConfig
```

Questo rendeva il gate terrain meno leggibile.

### Correzione

La diagnostica terrain ora espone:

- `HasTerrainCatalogJson`;
- `TerrainCatalogParsed`;
- `TerrainCatalogEntryCount`;
- `UsedCatalogUvMap`;
- `UsedLegacyConfigUvMap`;

Nel log del renderer compaiono ora campi come:

```text
catalogJson=True
catalogParsed=True
catalogEntries=2
catalogUv=True
legacyConfigUv=False
```

Oppure, in fallback:

```text
catalogJson=False
catalogParsed=False
catalogEntries=0
catalogUv=False
legacyConfigUv=True
```

### Menu diagnostico

Aggiunto context menu:

```text
ArcGraph/Log Last Terrain Runtime Diagnostics
```

Serve a ristampare l'ultimo stato terrain senza ricostruire i chunk.

### Confini preservati

Lo step:

- non introduce una nuova mappa ArcGraph;
- non cambia la conformazione delle celle;
- non modifica `MapGridData`;
- non modifica `World`;
- non modifica Decision Layer;
- non modifica Job Layer;
- non salva scene;
- non salva prefab;
- non carica asset nel renderer;
- non rimuove fallback legacy.

### Stato dopo v0.38i.10

Il terreno ArcGraph e' piu' verificabile.

Non siamo ancora al modello mappa definitivo, ma possiamo distinguere chiaramente:

```text
tile id da adapter MapGrid temporaneo
UV da catalogo ArcGraph
```

oppure:

```text
tile id da adapter MapGrid temporaneo
UV da fallback MapGridConfig
```

Prossimo step consigliato:

```text
v0.38i.11 - rifinitura visuale terrain:
materiale, fallback UV, diagnostica tile mancanti e gate terrain/NPC
```

---

## Esito v0.38i.11 - ArcGraph Terrain Missing UV Diagnostics

La `v0.38i.11` raffina la diagnostica terrain data-driven: non basta sapere che
il renderer e' caduto su UV fallback, serve sapere anche quante celle sono cadute
in fallback e quale tile id manca per primo.

### Contesto

Dopo la `v0.38i.10`, il renderer terrain distingue gia':

```text
UV da catalogo ArcGraph
```

da:

```text
UV da fallback legacy MapGridConfig
```

Pero' il campo `UsedFallbackUv` era solo booleano.

Questo significava:

```text
fallbackUv=True
```

ma non diceva:

- se il problema riguardava una sola cella o molte celle;
- quale `tileId` non era coperto dal catalogo;
- se il catalogo era quasi completo oppure molto incompleto.

### Correzione

La diagnostica del singolo chunk terrain ora espone:

- `MissingUvTileCount`;
- `FirstMissingUvTileId`.

La diagnostica del renderer runtime terrain aggrega questi dati e li rende
visibili nel log generale.

Esempio di output atteso:

```text
fallbackUv=True
missingUvTiles=42
firstMissingUvTileId=7
```

Interpretazione:

```text
ArcGraph ha trovato 42 celle che usano tile id senza UV nel catalogo.
Il primo id mancante e' 7.
Prima correzione consigliata: aggiungere o correggere il tile id 7 nel catalogo terrain JSON.
```

### File tecnici coinvolti

- `ArcGraphTerrainChunkMeshDiagnostics`;
- `ArcGraphTerrainChunkMeshBuilder`;
- `ArcGraphTerrainChunkMeshData`;
- `ArcGraphTerrainRuntimeSceneRendererDiagnostics`;
- `ArcGraphTerrainRuntimeSceneRenderer`.

### Confini preservati

Lo step:

- non cambia la mesh generata;
- non cambia il materiale terrain;
- non cambia l'atlas;
- non cambia il catalogo terrain JSON;
- non cambia `MapGridData`;
- non cambia la conformazione della mappa;
- non modifica `World`;
- non modifica Decision Layer;
- non modifica Job Layer;
- non salva scene;
- non salva prefab;
- non rimuove fallback legacy.

### Stato dopo v0.38i.11

Il terreno ArcGraph e' piu' diagnosticabile prima del gate visuale.

Ora, se il terrain viene renderizzato con tile fallback, il log non si limita a
dire:

```text
qualcosa manca
```

ma indica:

```text
quante celle mancano
quale tile id correggere per primo
```

Prossimo step consigliato:

```text
gate visuale Unity terrain+NPC
```

oppure, se il gate segnala asset mancanti:

```text
rifinitura resolver / cataloghi / asset path
```

---

## Esito v0.38i.12 - ArcGraph Terrain Visual Catalog v1

La `v0.38i.12` introduce il primo catalogo visuale terrain data-driven di
ArcGraph.

Questo step non cambia ancora il renderer terrain produttivo. Serve a definire
la struttura dati che useremo per gestire:

- tile default;
- varianti visuali pesate;
- animazioni tile;
- transizioni di bordo tra tipi terreno;
- scelta deterministica della variante per cella.

### Problema progettuale

Il modello precedente ragionava soprattutto cosi':

```text
cella
-> tileId
-> UV atlas
```

Questo e' sufficiente per disegnare una mappa minima, ma diventa fragile appena
vogliamo introdurre varieta' visuale.

Esempio:

```text
grass
-> prato base
-> prato con fiori
-> prato con erba piu' scura
-> prato vicino a piastrelle
```

Se la mappa deve salvare direttamente ogni singola variante, allora la mappa
diventa troppo grafica e perde significato simulativo.

La direzione corretta e':

```text
cella
-> terrainType = grass
-> resolver visuale
-> tileId finale
-> UV atlas
```

### Nuovo catalogo

Aggiunto:

```text
Assets/Resources/ArcGraph/Config/ArcGraphTerrainVisualCatalog.json
```

Il file di esempio contiene:

- `grass`;
- `stone_floor`;
- `water`;
- varianti pesate per `grass`;
- animazione frame-based per `water`;
- transizioni cardinali da `grass` a `stone_floor`.

Esempio concettuale:

```json
{
  "terrainId": "grass",
  "defaultTileId": 0,
  "variants": [
    { "tileId": 0, "weight": 70 },
    { "tileId": 1, "weight": 20 },
    { "tileId": 2, "weight": 10 }
  ]
}
```

### Resolver visuale

Introdotto:

```text
ArcGraphTerrainVisualResolver
```

Il resolver applica questa priorita':

```text
1. transizione di bordo
2. animazione tile
3. variante deterministica
4. fallback
```

Esempio:

```text
cella grass
vicino est stone_floor
mask E
-> usa tile transizione grass_to_stone_east
```

Altro esempio:

```text
cella grass senza bordo
coordinate 12,9
-> sceglie una variante prato in modo stabile
```

La variante non viene scelta con random runtime.

Viene scelta con hash stabile da:

```text
x
y
z
terrainId
```

Quindi:

```text
la stessa cella produce sempre la stessa variante
```

senza flickering e senza dipendere dalla sessione.

### Animazioni

Il catalogo supporta animazioni passivamente:

```json
{
  "terrainId": "water",
  "defaultTileId": 30,
  "animation": {
    "frameTileIds": [30, 31, 32, 33],
    "frameSeconds": 0.25
  }
}
```

Questo non anima ancora la scena.

Definisce solo il contratto:

```text
tempo visuale
-> frame index
-> tile id animato
```

Il tick simulativo resta separato dal tempo visuale.

### Transizioni

Il catalogo supporta maschere cardinali semplici:

```text
N
E
S
W
NE
SE
SW
NW
```

Questo e' sufficiente per il primo sistema di bordi tra prato e pavimenti.

Non e' ancora un sistema Wang tiles completo, ma lascia aperta la strada a un
autotile piu' ricco senza cambiare il modello mappa.

### File tecnici introdotti

- `ArcGraphTerrainVisualCatalog`;
- `ArcGraphTerrainVisualCatalogJson`;
- `ArcGraphTerrainVisualResolver`;
- `ArcGraphTerrainVisualCatalogHarness`;
- `ArcGraphTerrainVisualCatalog.json`.

### Confini preservati

Lo step:

- non collega ancora il resolver al mesh builder;
- non cambia `ArcGraphTerrainChunkMeshBuilder`;
- non cambia `ArcGraphTerrainRuntimeSceneRenderer`;
- non cambia `MapGridData`;
- non cambia `World`;
- non modifica Decision Layer;
- non modifica Job Layer;
- non salva scene;
- non salva prefab;
- non carica asset;
- non usa `Resources.Load`;
- non crea GameObject, mesh o materiali.

### Stato dopo v0.38i.12

ArcGraph ora possiede un contratto dati per rappresentare il terreno in modo piu'
ricco del semplice tile id.

La catena futura diventa:

```text
terrain snapshot
-> terrain type / tile legacy temporaneo
-> visual catalog
-> visual resolver
-> tile id finale
-> UV map
-> mesh chunk
```

Prossimo step consigliato:

```text
collegare il resolver visuale al mesh builder terrain dietro fallback legacy
```

oppure, prima del collegamento:

```text
implementare filtro viewport XY per renderizzare solo i chunk visibili
```

---

## Roadmap operativa v0.38i.13 - ArcGraph Terrain Tile Roadmap

La `v0.38i.13` definisce la roadmap operativa per completare la gestione tile
terrain di ArcGraph sulla base dei cinque punti progettuali aperti:

1. tile default con varianti;
2. scelta atlas / piu' atlas / sprite separati;
3. rendering solo della porzione visibile;
4. tile animati;
5. tile di confine e transizioni tra aree.

Questa roadmap non implementa ancora codice nuovo.

Serve a ordinare il lavoro in modo che il terreno non diventi pesante o fragile
prima ancora di avere un renderer viewport-aware.

### Principio guida

La priorita' tecnica e':

```text
prima ridurre cosa renderizziamo
poi aumentare la ricchezza di cosa renderizziamo
```

Per questo il primo step non deve essere animare acqua o collegare tutte le
varianti.

Il primo step deve essere:

```text
renderizzare / aggiornare solo chunk visibili
```

Altrimenti varianti, transizioni e animazioni rischiano di aumentare il costo di
una pipeline che non filtra ancora bene la porzione visibile.

---

### v0.38i.14 - ArcGraph Terrain Visible Chunk Filter

**Obiettivo**

Introdurre il filtro viewport XY per il renderer terrain runtime.

Oggi ArcGraph possiede:

```text
DirtyChunks
chunk mesh
pool chunk
filtro Z level
```

ma non possiede ancora in modo completo:

```text
VisibleChunkSet XY
```

Questo step deve fare in modo che il renderer terrain lavori solo sui chunk che
intersecano il rettangolo visibile della camera/vista.

**Output atteso**

- calcolo dei chunk visibili da `ArcGraphViewCellRect`;
- filtro dei dirty chunks fuori viewport;
- disattivazione o mantenimento spento dei chunk non visibili;
- diagnostica:
  - dirty chunks totali;
  - chunk visibili;
  - chunk scartati per viewport;
  - chunk applicati;
  - chunk disattivati.

**Vincoli**

- non cambiare ancora il resolver tile;
- non introdurre animazioni;
- non salvare scene;
- non modificare MapGrid;
- non leggere direttamente camera o input dentro il mesh builder.

**Criterio di completamento**

Il renderer deve poter dire:

```text
questa mappa ha molti chunk
ma in questo frame ne considero solo N visibili
```

---

### v0.38i.15 - Collegamento Terrain Visual Resolver al Mesh Builder

**Obiettivo**

Collegare `ArcGraphTerrainVisualResolver` alla costruzione mesh terrain.

La catena futura diventa:

```text
ArcGraphTerrainCellSnapshot
-> terrain type / tile legacy temporaneo
-> ArcGraphTerrainVisualCatalog
-> ArcGraphTerrainVisualResolver
-> tileId finale
-> ArcGraphTerrainTileUvMap
-> mesh chunk
```

**Nota importante**

La mappa definitiva non possiede ancora un vero `terrainType`.

Per questa fase serve una compatibilita' temporanea:

```text
tileId legacy
-> terrainId provvisorio
```

Esempio:

```text
tileId 0 -> grass
tileId 1 -> grass
blocked -> wall / stone_floor provvisorio
```

**Output atteso**

- contratto per passare il catalogo visuale al builder;
- fallback legacy se il catalogo visuale manca;
- diagnostica:
  - tile risolti da resolver;
  - tile risolti da policy legacy;
  - transition usate;
  - animation usate;
  - variant usate;
  - fallback usati.

**Criterio di completamento**

La resa visiva deve poter usare il catalogo:

```text
grass -> varianti deterministiche
```

senza rompere il rendering precedente.

---

### v0.38i.16 - Terrain Atlas Strategy e Coverage Diagnostics

**Obiettivo**

Consolidare la scelta atlas.

Decisione consigliata:

```text
piu' atlas tematici
```

Non:

```text
un atlas unico infinito
```

e non:

```text
un PNG separato per ogni tile come strategia finale terrain
```

**Strategia consigliata**

```text
Terrain Base Atlas
-> erba, terra, pietra, sabbia, pavimenti

Terrain Transition Atlas
-> bordi, angoli, raccordi

Water Atlas
-> acqua animata, rive, schiuma

Structure/Object Atlas
-> muri alti, porte, strutture
```

**Output atteso**

- estensione cataloghi per distinguere `atlasId`;
- diagnostica coverage:
  - tile dichiarati dal visual catalog;
  - tile presenti nell'UV catalog;
  - tile mancanti;
  - atlas mancanti;
  - primo tile mancante.

**Criterio di completamento**

Prima del gate visuale, ArcGraph deve poter dire:

```text
tutti i tile richiesti dal visual catalog sono coperti da UV
```

oppure:

```text
manca il tile X nell'atlas Y
```

---

### v0.38i.17 - Tile Variants Runtime

**Obiettivo**

Rendere operative le varianti tile dichiarate nel visual catalog.

Esempio:

```text
grass
-> grass_base 70%
-> grass_flowers_01 20%
-> grass_flowers_02 10%
```

**Regola fondamentale**

La variante deve essere:

```text
deterministica per cella
```

Non:

```text
random ogni frame
```

Esempio:

```text
cella 10,20 grass -> grass_flowers_01 sempre
cella 11,20 grass -> grass_base sempre
```

**Output atteso**

- varianti risolte dal catalogo;
- peso rispettato in modo stabile;
- diagnostica conteggio varianti per chunk;
- nessun flickering.

**Criterio di completamento**

La stessa mappa deve produrre sempre la stessa distribuzione visuale di varianti
a parita' di coordinate e catalogo.

---

### v0.38i.18 - Animated Terrain Tiles

**Obiettivo**

Supportare tile animati, soprattutto acqua.

Il catalogo gia' ammette:

```json
{
  "terrainId": "water",
  "animation": {
    "frameTileIds": [30, 31, 32, 33],
    "frameSeconds": 0.25
  }
}
```

Lo step deve trasformare questo contratto in aggiornamento visuale.

**Regola fondamentale**

Il tempo visuale deve restare separato dal tick simulativo:

```text
tick simulazione
!=
frame animazione tile
```

**Output atteso**

- aggiornamento solo dei chunk visibili con tile animati;
- dirty visuale dedicato o refresh controllato;
- nessun aggiornamento di tile animati fuori viewport;
- diagnostica:
  - chunk animati visibili;
  - tile animati aggiornati;
  - frame corrente;
  - tempo visuale usato.

**Criterio di completamento**

L'acqua puo' animarsi senza obbligare tutta la mappa a ricostruirsi a ogni frame.

---

### v0.38i.19 - Terrain Transitions / Autotile Semplice

**Obiettivo**

Rendere operative le transizioni di bordo tra terrain type.

Esempio:

```text
grass vicino a stone_floor
-> bordo grass/stone
```

Il primo modello resta semplice:

```text
maschere cardinali N/E/S/W
combinazioni NE/SE/SW/NW
```

Non si implementa subito un Wang tile system completo.

**Output atteso**

- lettura dei vicini cardinali;
- costruzione maschera;
- risoluzione transition set;
- fallback se manca una regola;
- diagnostica:
  - transizioni risolte;
  - transizioni mancanti;
  - prima maschera mancante;
  - terrain pair mancante.

**Criterio di completamento**

Una zona prato accanto a una zona pavimento deve poter mostrare un bordo grafico
coerente, senza cambiare il dato simulativo della cella.

---

### v0.38i.20 - Terrain Tile Visual Gate

**Obiettivo**

Validare in Unity la pipeline terrain completa.

Il gate deve verificare:

- chunk visibili;
- varianti prato;
- atlas coverage;
- acqua animata;
- transizioni prato/pavimento;
- fallback UV leggibili;
- assenza di flickering;
- assenza di ricostruzione inutile su tutta la mappa;
- compatibilita' con F12 MapGrid/ArcGraph.

**Output atteso in Console**

Esempio:

```text
visibleChunks=...
culledChunks=...
resolverTiles=...
variantTiles=...
transitionTiles=...
animatedTiles=...
missingUvTiles=0
```

**Criterio di completamento**

Il terreno ArcGraph puo' essere considerato candidato minimo stabile per:

```text
tile base
varianti
animazioni
transizioni
viewport-aware rendering
```

Non significa ancora pensionare MapGrid.

Significa solo che il terreno ArcGraph e' abbastanza maturo per il confronto
visuale serio.

---

### Rischi principali

**Rischio 1: troppa ricchezza prima del culling**

Se colleghiamo subito animazioni e transizioni senza filtro viewport, rischiamo
di aggiornare troppa mappa.

Mitigazione:

```text
v0.38i.14 prima di tutto
```

**Rischio 2: confondere terrain con object/structure**

Muri alti, alberi grandi e oggetti che possono coprire NPC non devono stare nel
terrain base.

Devono stare in layer structure/object.

Mitigazione:

```text
terrain = pavimento/suolo/liquidi base
object/structure = elementi verticali o interattivi
```

**Rischio 3: transizioni troppo sofisticate troppo presto**

Un Wang tile system completo puo' diventare un mini-editor dentro ArcGraph.

Mitigazione:

```text
prima maschere cardinali semplici
poi eventuale estensione
```

**Rischio 4: atlas troppo grande o troppo frammentato**

Un solo atlas gigante e' difficile da gestire.
Troppi atlas piccoli rompono batching e materiali.

Mitigazione:

```text
atlas tematici
```

---

### Ordine operativo raccomandato

```text
v0.38i.14 - Visible Chunk Filter
v0.38i.15 - Resolver -> Mesh Builder
v0.38i.16 - Atlas Strategy + Coverage
v0.38i.17 - Variants Runtime
v0.38i.18 - Animated Tiles
v0.38i.19 - Terrain Transitions
v0.38i.20 - Visual Gate
```

Questo ordine conserva una progressione sana:

```text
prestazioni
-> aggancio dati
-> asset coverage
-> varieta'
-> movimento visuale
-> bordi
-> gate
```

---

## Esito v0.38i.14 - ArcGraph Terrain Visible Chunk Filter

La `v0.38i.14` introduce il primo filtro viewport reale nel percorso terrain
runtime di ArcGraph.

L'obiettivo non e' ancora rendere il terreno piu' ricco.

L'obiettivo e':

```text
prima decidere quali chunk vale la pena renderizzare
poi arricchire cosa viene renderizzato
```

---

### Cosa e' stato aggiunto

Sono stati introdotti:

- `ArcGraphTerrainVisibleChunkFilter`;
- `ArcGraphTerrainVisibleChunkFilterResult`;
- `ArcGraphTerrainVisibleChunkFilterHarness`;
- overload di `ArcGraphTerrainChunkMeshBuilder.BuildChunks(...)` da lista esplicita di chunk;
- campi viewport nel `ArcGraphTerrainRuntimeSceneRenderer`;
- diagnostica viewport nel `ArcGraphTerrainRuntimeSceneRendererDiagnostics`.

Il filtro riceve:

```text
dirty chunks
chunk size
visible Z level
ArcGraphViewCellRect
```

e produce:

```text
chunk ammessi
chunk ricevuti
chunk visibili
chunk scartati
reason diagnostica
```

---

### Comportamento runtime

Il renderer terrain runtime ora puo':

- conservare un rettangolo celle visibile;
- filtrare i dirty chunks contro quel rettangolo;
- costruire mesh solo per i chunk ammessi;
- disattivare chunk del pool che sono usciti dal viewport;
- lasciare spento il culling di default per non rompere i gate esistenti.

Il flusso diventa:

```text
ArcGraphRenderState.Dirty.DirtyChunks
-> ArcGraphTerrainVisibleChunkFilter
-> BuildChunks(lista filtrata)
-> ApplyChunks
-> pool chunk runtime
```

---

### Regola importante sul dirty state

Se il viewport scarta alcuni dirty chunks, il dirty state non viene pulito.

Motivo:

```text
un chunk fuori viewport puo' essere sporco ma non visibile ora
se pulisco il dirty state, quando torno su quel chunk perdo l'aggiornamento
```

Quindi:

```text
dirty chunks tutti visibili/applicati
-> dirty pulibile

dirty chunks parzialmente scartati dal viewport
-> dirty conservato
```

Questa scelta e' conservativa.

Evita bug visivi nascosti durante pan/zoom, anche se in futuro potra' essere
raffinata con dirty state parziale per chunk.

---

### Diagnostica aggiunta

Il renderer terrain ora puo' spiegare:

```text
viewportCulling=...
visibleRect=minX,minY->maxX,maxY
visibleChunks=...
culledDirtyChunks=...
disabledOutsideViewport=...
```

Questi dati si aggiungono alla diagnostica gia' presente:

```text
dirtyChunks
built
nonEmpty
applied
created
reused
disabled
active
catalogUv
legacyConfigUv
missingUvTiles
firstMissingUvTileId
```

---

### Cosa NON cambia

Questo step non collega ancora:

- `ArcGraphTerrainVisualResolver`;
- varianti tile;
- animazioni tile;
- transizioni/autotile;
- atlas tematici;
- nuova conformazione mappa ArcGraph.

Non modifica:

- `World`;
- `Decision Layer`;
- `Job Layer`;
- `MapGrid`;
- scene;
- prefab.

---

### Stato dopo v0.38i.14

ArcGraph terrain ora ha una base piu' sana per mappe grandi.

Con mappe future da 250x250, il percorso non deve piu' ragionare solo in termini
di:

```text
tutta la mappa
```

ma puo' ragionare in termini di:

```text
chunk dirty visibili
```

Il prossimo passo logico resta:

```text
v0.38i.15 - Collegamento Terrain Visual Resolver al Mesh Builder
```

---

## Esito v0.38i.15 - Collegamento Terrain Visual Resolver al Mesh Builder

La `v0.38i.15` collega il catalogo visuale terrain al percorso di costruzione
mesh, mantenendo pero' il fallback legacy come comportamento sicuro.

Il principio dello step e':

```text
se il catalogo visuale e' disponibile, usalo
se non e' disponibile, continua come prima
```

Questo evita che ArcGraph diventi fragile mentre il nuovo sistema tile e' ancora
in costruzione.

---

### Cosa e' stato aggiunto

E' stato introdotto:

- `ArcGraphTerrainVisualBuildOptions`;
- overload del mesh builder con opzioni visuali;
- parsing/cache del `terrainVisualCatalogJson` dentro il terrain runtime renderer;
- contatori diagnostici per resolver visuale, legacy, varianti, animazioni,
  transizioni e fallback resolver.

Il nuovo flusso puo' diventare:

```text
ArcGraphTerrainCellSnapshot
-> terrain id provvisorio
-> ArcGraphTerrainVisualCatalog
-> ArcGraphTerrainVisualResolver
-> tile id finale
-> ArcGraphTerrainTileUvMap
-> mesh chunk
```

Se questo flusso non e' possibile, resta:

```text
ArcGraphTerrainCellSnapshot
-> ArcGraphTerrainVisualPolicy legacy
-> tile id finale
-> ArcGraphTerrainTileUvMap
-> mesh chunk
```

---

### Mapping provvisorio tile legacy -> terrain id

La mappa definitiva ArcGraph non possiede ancora un vero campo `terrainType`.

Per questo lo step usa una compatibilita' provvisoria:

```text
tileId 30-33 -> water
tileId 10/11 -> stone_floor
altri tile non bloccati -> grass
celle bloccate -> legacy
```

Le celle bloccate restano volutamente nel percorso legacy.

Motivo:

```text
blocked oggi rappresenta spesso muri/strutture
terrain visuale deve restare pavimento/suolo/liquido base
```

Questo evita di confondere il terrain layer con lo structure/object layer futuro.

---

### Prestazioni

Il resolver visuale viene creato una volta per chunk, non una volta per cella.

Il catalogo visuale JSON viene parsato solo quando cambia il testo sorgente.

Quindi il percorso resta leggero:

```text
parse catalogo -> cache
chunk build -> resolver riusato nel chunk
cella -> scelta tile locale
```

---

### Diagnostica aggiunta

La diagnostica chunk ora puo' dire:

```text
VisualResolverTileCount
LegacyVisualTileCount
VisualVariantTileCount
VisualAnimationTileCount
VisualTransitionTileCount
VisualResolverFallbackCount
```

La diagnostica del renderer ora puo' dire:

```text
visualCatalogJson=...
visualCatalogParsed=...
visualDefinitions=...
visualResolver=...
visualResolverTiles=...
legacyVisualTiles=...
visualVariants=...
visualAnimations=...
visualTransitions=...
visualResolverFallbacks=...
```

Questo serve per il gate visuale: non basta vedere il terreno a video, bisogna
sapere se il terreno sta arrivando davvero dal nuovo catalogo visuale.

---

### Cosa resta fuori

Questo step non implementa ancora:

- transizioni/autotile basate sul vicinato reale;
- aggiornamento temporale dei frame animati;
- strategia atlas tematica;
- coverage diagnostica tra visual catalog e UV catalog;
- nuova mappa ArcGraph con `terrainType` nativo.

Questi punti restano nei passaggi successivi:

```text
v0.38i.16 - Atlas Strategy + Coverage
v0.38i.17 - Variants Runtime
v0.38i.18 - Animated Tiles
v0.38i.19 - Terrain Transitions
```

---

### Stato dopo v0.38i.15

ArcGraph terrain puo' ora iniziare a usare un catalogo visuale data-driven nel
percorso mesh, ma senza perdere compatibilita' con la resa precedente.

Il prossimo passo logico diventa:

```text
v0.38i.16 - Terrain Atlas Strategy e Coverage Diagnostics
```

---

## Esito v0.38i.16 - Terrain Atlas Strategy e Coverage Diagnostics

La `v0.38i.16` introduce il controllo passivo di coverage tra:

```text
ArcGraphTerrainVisualCatalog
-> tile richiesti

ArcGraphTerrainCatalog
-> tile coperti da UV/atlas
```

Questo step non rende ancora operativo il multi-atlas Unity.

Serve invece a sapere, prima del gate visuale, se il catalogo visuale chiede tile
che il catalogo UV non conosce.

---

### Cosa e' stato aggiunto

Sono stati introdotti:

- `ArcGraphTerrainVisualCoverageDiagnostics`;
- `ArcGraphTerrainVisualCoverageAnalyzer`;
- `ArcGraphTerrainVisualCoverageHarness`.

L'analizzatore raccoglie dal visual catalog:

- default tile;
- varianti;
- frame animati;
- tile delle transition rules.

Poi confronta questi tile con gli id presenti nel catalogo UV.

---

### Output diagnostico

La diagnostica produce:

```text
HasVisualCatalog
HasUvCatalog
IsFullyCovered
RequiredTileCount
CoveredTileCount
MissingTileCount
FirstMissingTileId
Reason
```

Il renderer terrain runtime espone ora anche:

```text
coverageChecked
coverageComplete
coverageRequired
coverageCovered
coverageMissing
coverageFirstMissing
```

Questo significa che il gate visuale potra' distinguere:

```text
il tile non si vede per problema di sprite/materiale
```

da:

```text
il tile non e' proprio dichiarato nel catalogo UV
```

---

### Rapporto con atlas tematici

La decisione progettuale resta:

```text
atlas tematici
```

Esempio futuro:

```text
terrain_base
terrain_transition
water
structure_object
```

La `v0.38i.16` non implementa ancora un renderer multi-materiale per questi
atlas.

Implementa pero' il controllo minimo necessario:

```text
il visual catalog chiede tile X
il catalogo UV deve coprire tile X
```

Il passaggio a piu' atlas fisici richiedera' una fase successiva sul renderer,
perche' ogni atlas/materiale puo' implicare mesh separate o submesh.

---

### Cosa resta fuori

Restano fuori:

- multi-atlas Unity operativo;
- scelta automatica del materiale in base ad atlas id;
- batching multi-material;
- submesh per atlas;
- transizioni basate sul vicinato reale;
- animazioni tile aggiornate nel tempo.

Il prossimo step logico e':

```text
v0.38i.17 - Tile Variants Runtime
```

---

## Esito v0.38i.17 - Tile Variants Runtime

La `v0.38i.17` consolida il comportamento delle varianti tile del terreno.

Il punto progettuale e' semplice:

```text
una cella prato puo' usare tile prato diversi
ma la stessa cella deve mantenere sempre lo stesso tile
```

Questo evita il problema peggiore delle varianti grafiche, cioe' il flickering:
se a ogni frame una cella scegliesse casualmente un tile diverso, il terreno
sembrerebbe tremare.

---

### Cosa e' stato aggiunto

Sono stati introdotti:

- `ArcGraphTerrainVariantDiagnostics`;
- `ArcGraphTerrainVariantAnalyzer`;
- `ArcGraphTerrainVariantHarness`.

L'analizzatore lavora su:

- `ArcGraphTerrainVisualCatalog`;
- terrain id, per esempio `grass`;
- dimensione del campione, per esempio `16x16`;
- livello Z;
- tempo visuale opzionale.

Per ogni cella del campione risolve il tile due volte.

Se la stessa cella produce due tile diversi, la diagnostica segnala instabilita'.

Se invece tutte le celle sono stabili e nel campione appaiono piu' tile diversi,
il catalogo sta producendo varieta' visuale corretta.

---

### Output diagnostico

La diagnostica produce:

```text
HasCatalog
HasTerrainDefinition
IsStable
HasVariation
SampleCount
StableSampleCount
ChangedSampleCount
DistinctTileCount
FirstTileId
FirstChangedCellX/Y/Z
Reason
```

Esempio interpretativo:

```text
IsStable = true
HasVariation = true
SampleCount = 256
ChangedSampleCount = 0
DistinctTileCount = 3
Reason = StableVariantDistribution
```

significa:

```text
ho controllato 256 celle
nessuna cella cambia tile tra due risoluzioni consecutive
il campione usa 3 tile diversi
```

Questo e' il comportamento desiderato per prato, terra, pavimento o altri tile
statici con varianti.

---

### Rapporto con il rendering

Il mesh builder era gia' stato collegato al `ArcGraphTerrainVisualResolver` in
`v0.38i.15`.

La `v0.38i.17` non cambia quindi la mesh in modo nuovo.

Serve invece a rendere verificabile il contratto:

```text
le varianti che arrivano al rendering sono stabili per coordinate
```

La scelta resta deterministica e non usa random per-frame.

---

### Cosa resta fuori

Restano fuori:

- animazione tile nel tempo;
- refresh dei chunk quando cambia il frame visuale di acqua o altri terreni animati;
- transizioni/autotile basate sul vicinato reale;
- multi-atlas Unity operativo;
- gate visuale completo terrain.

Il prossimo step logico e':

```text
v0.38i.18 - Runtime Terrain Map e Cache Visuale Statica
```

---

## Esito v0.38i.18 - Runtime Terrain Map e Cache Visuale Statica

La `v0.38i.18` introduce la struttura runtime che mancava tra:

```text
mappa letta / snapshot terrain
```

e:

```text
mesh finale disegnata da ArcGraph
```

La decisione progettuale applicata e':

```text
la mappa runtime non deve essere una mappa di sprite
deve essere una mappa semantica con una cache grafica stabile
```

---

### Cosa e' stato aggiunto

Sono stati introdotti:

- `ArcGraphTerrainTypeMapper`;
- `ArcGraphTerrainVisualCache`;
- `ArcGraphRuntimeTerrainCell`;
- `ArcGraphRuntimeTerrainMap`;
- `ArcGraphRuntimeTerrainMapBuilder`;
- `ArcGraphRuntimeTerrainMapHarness`.

Il mapper provvisorio traduce i tile legacy in terrain id:

```text
tile 30-33 -> water
tile 10/11 -> stone_floor
altri tile -> grass
```

Questa traduzione e' temporanea e dichiarata.

In futuro sara' sostituita da una mappa ArcGraph letta direttamente da file con
terrain id gia' espliciti.

---

### Nuova struttura della cella runtime

Una cella runtime terrain ora puo' essere letta concettualmente cosi':

```text
ArcGraphRuntimeTerrainCell
├─ Cell
├─ TerrainId
├─ SourceTileId
├─ IsBlocked
├─ MovementCost
└─ VisualCache
   ├─ StaticTileId
   ├─ HasStaticTile
   ├─ HasAnimatedVisual
   ├─ UsedVisualResolver
   ├─ UsedVariant
   └─ UsedFallback
```

Questo separa:

```text
cosa e' la cella
```

da:

```text
come viene disegnata
```

Esempio:

```text
TerrainId = grass
StaticTileId = 2
```

significa:

```text
la cella e' prato
ArcGraph la disegna con la variante prato 2
```

Il tile statico non diventa la verita' della mappa.

---

### Rapporto con le varianti statiche

Prima di questo step, il mesh builder poteva risolvere la variante durante la
costruzione del chunk.

Ora il flusso corretto diventa:

```text
snapshot terrain
↓
ArcGraphRuntimeTerrainMapBuilder
↓
RuntimeTerrainMap con cache statica
↓
ArcGraphTerrainChunkMeshBuilder
↓
mesh
```

Quindi per i tile statici:

```text
la variante viene scelta una volta nella runtime map
il builder legge StaticTileId
```

Questo riduce lavoro inutile e rende piu' chiaro dove viene prodotta la varieta'
grafica.

---

### Rapporto con acqua e tile animati

Le celle animate non vengono congelate come tile finale statico.

Esempio:

```text
TerrainId = water
HasAnimatedVisual = true
HasStaticTile = false
```

significa:

```text
la cella resta acqua a livello semantico
il frame grafico verra' scelto dal sistema animazione visuale
```

Questo prepara il prossimo step senza confondere:

```text
cache statica
```

con:

```text
frame animato nel tempo
```

---

### Cosa resta fuori

Restano fuori:

- loader mappa ArcGraph definitivo da file;
- salvataggio file con terrain id nativi;
- transizioni/autotile basate sul vicinato reale;
- aggiornamento temporale dei frame animati;
- invalidazione selettiva della cache quando cambia una cella o un vicino;
- sistema movement cost definitivo per tipo terreno;
- sostituzione completa di `MapGridData`.

Il prossimo step logico diventa:

```text
v0.38i.19 - Animated Terrain Tiles
```

---

## Esito v0.38i.19 - Animated Terrain Tiles

La `v0.38i.19` rende operativa la prima animazione dei tile terrain dentro
ArcGraph.

Lo step parte dalla struttura introdotta in `v0.38i.18`:

```text
RuntimeTerrainMap
├─ celle statiche con StaticTileId gia' risolto
└─ celle animate con HasAnimatedVisual = true
```

e aggiunge il pezzo mancante:

```text
tempo visuale ArcGraph
↓
refresh selettivo dei chunk animati
↓
resolver frame-based
↓
mesh aggiornata solo dove serve
```

---

### Cosa e' stato aggiunto

Sono stati introdotti:

- `ArcGraphTerrainAnimationClock`;
- `ArcGraphTerrainAnimationClockStep`;
- `ArcGraphTerrainAnimationClockHarness`;
- diagnostica animation-side dentro `ArcGraphTerrainRuntimeSceneRendererDiagnostics`;
- refresh selettivo dei chunk terrain animati dentro `ArcGraphTerrainRuntimeSceneRenderer`.

Il clock e' solo grafico.

Non e' un tick simulativo.

Non modifica il World.

Non modifica MapGrid.

Non decide eventi.

Serve solo a far avanzare frame visuali come:

```text
water frame 30
water frame 31
water frame 32
water frame 33
```

---

### Come funziona ora l'animazione terrain

Il renderer conserva un indice dei chunk che contengono almeno una cella animata:

```text
_animatedTerrainChunks
```

Questo indice viene ricostruito quando viene ricostruita la runtime terrain map.

Poi, durante il runtime:

```text
ArcGraphTerrainAnimationClock
↓
se passa l'intervallo configurato
↓
marca dirty solo i chunk animati visibili
↓
il mesh builder ricostruisce quei chunk
↓
il resolver sceglie il frame corrente usando VisualTimeSeconds
```

Questa soluzione evita di ridisegnare tutta la mappa a ogni frame.

---

### Parametri introdotti nel renderer

Nel renderer terrain sono presenti due parametri:

```text
animateTerrainTiles = true
terrainAnimationRefreshSeconds = 0.25
```

`animateTerrainTiles` abilita/disabilita il sistema.

`terrainAnimationRefreshSeconds` indica ogni quanto ArcGraph deve provare a
ridisegnare i chunk animati.

Il valore `0.25` significa:

```text
circa 4 frame visuali al secondo
```

per i tile animati.

---

### Diagnostica aggiunta

La diagnostica del renderer ora espone:

```text
TerrainAnimationEnabled
TerrainAnimationVisualTimeSeconds
TerrainAnimationRefreshSeconds
AnimatedTerrainChunkCount
TerrainAnimationRefreshQueued
```

Questo permette di capire da Console:

- se l'animazione terrain e' attiva;
- quanto tempo visuale sta usando il renderer;
- quanti chunk animati sono stati rilevati;
- se nello specifico frame e' stato accodato un refresh.

---

### Cosa resta fuori

Restano fuori:

- transizioni/autotile basate sul vicinato reale;
- animazioni differenziate per LOD/zoom;
- gestione separata di acqua come layer liquido autonomo;
- animazioni erba/vegetazione nel layer vegetazione;
- invalidazione fine per singola cella animata;
- editor tooling per preview catalogo;
- test visuale completo in Unity.

Il prossimo step logico diventa:

```text
v0.38i.20 - Terrain Transitions / Autotile Semplice
```

---

## Esito v0.38i.20 - Terrain Transitions / Autotile Semplice

La `v0.38i.20` collega al rendering terrain il primo sistema di transizioni
cardinali tra tipi di terreno.

Lo step risolve il problema rimasto dopo `v0.38i.18` e `v0.38i.19`:

```text
la cella aveva gia' un terrain id
la cella aveva gia' una cache statica o animata
ma il renderer non guardava ancora i vicini reali
```

Ora il mesh builder puo' usare la `RuntimeTerrainMap` per capire se una cella
confina con un altro tipo di terreno.

---

### Cosa e' stato aggiunto

Sono stati aggiunti:

- risoluzione delle transizioni dentro `ArcGraphTerrainChunkMeshBuilder`;
- lettura locale dei vicini cardinali `N/E/S/W` dalla `ArcGraphRuntimeTerrainMap`;
- costruzione di maschere come `E`, `N`, `NE`, `SW`;
- uso del resolver visuale prima della cache statica quando esiste una transizione valida;
- `ArcGraphTerrainTransitionHarness` come smoke test data-only.

---

### Flusso logico

Il nuovo flusso per una cella terrain e':

```text
RuntimeTerrainCell
↓
leggi vicini cardinali nella RuntimeTerrainMap
↓
costruisci maschera per terrain confinante
↓
prova regola di transizione dal VisualCatalog
↓
se esiste: usa tile transizione
↓
se non esiste: usa animazione/cache statica/fallback legacy
```

Esempio:

```text
cella corrente: grass
vicino est: stone_floor
maschera: E
regola catalogo: grass -> stone_floor, E = tile 20
risultato: tile 20
```

---

### Priorita' visuale aggiornata

La priorita' attuale diventa:

```text
1. transizione terrain da vicinato runtime
2. animazione terrain
3. cache statica pre-risolta
4. fallback tile sorgente / legacy
```

Questa scelta permette ai bordi di vincere sulle varianti statiche.

Una cella resta semanticamente:

```text
grass
```

ma puo' essere disegnata come:

```text
grass_edge_east
```

se il catalogo lo richiede.

---

### Vincoli rispettati

La patch non introduce:

- lettura diretta del World;
- lettura diretta di MapGridData nel builder;
- comandi verso la simulazione;
- oggetti scena;
- caricamento asset;
- planner grafico generale.

La transizione e' locale e limitata ai quattro vicini cardinali.

---

### Cosa resta fuori

Restano fuori:

- regole diagonali complesse stile Wang tile completo;
- transizioni tra piu' di due terrain type con priorita' configurabile;
- fallback parziale quando esiste una maschera combinata non dichiarata;
- editor di preview del catalogo;
- test visuale Unity completo;
- separazione definitiva del layer acqua come liquid layer autonomo.

Il prossimo step logico diventa:

```text
v0.38i.21 - Terrain Visual Gate Finale
```

Obiettivo:

```text
verificare in un unico gate terrain tile base, varianti, animazioni e transizioni
```

---

## Roadmap residua v0.38 - Chiusura ArcGraph minimo stabile

La chiusura di `v0.38` non coincide con l'aggiunta di tutti i futuri layer
ambientali. In questa fase ArcGraph deve diventare una vista minima stabile:
terreno, NPC, switch runtime, interazione minima e base UI modulare.

Restano fuori da questa chiusura:

- biosfera produttiva;
- simulazione acqua;
- vegetazione runtime;
- luci locali produttive;
- meteo produttivo;
- incendi, fumo ed effetti particellari produttivi;
- eliminazione fisica immediata di MapGrid.

Questi moduli verranno introdotti dopo che ArcGraph sara' abbastanza stabile da
ricevere dati visuali senza dipendere dal monolite `MapGridWorldView`.

---

### v0.38i.21 - Terrain Visual Gate Finale

Obiettivo:

```text
verificare che il terrain renderer ArcGraph mostri correttamente tile base,
varianti, animazioni e transizioni usando cataloghi dati e atlas coerenti
```

Compiti:

- allineare `ArcGraphTerrainCatalog.json` con tutti i tile id richiesti da
  `ArcGraphTerrainVisualCatalog.json`;
- verificare che l'atlas terrain contenga i tile dichiarati;
- testare terrain base, per esempio `grass`;
- testare varianti statiche deterministicamente stabili;
- testare `stone_floor`;
- testare acqua animata;
- testare transizioni `grass -> stone_floor`;
- leggere la diagnostica di coverage tile/atlas;
- verificare che il rendering lavori sui chunk previsti e non ricostruisca tutta
  la mappa senza motivo.

Output atteso:

```text
Terrain ArcGraph configurabile da cataloghi e verificato in Unity
```

Esito operativo:

```text
SUPERATO NEL GATE VISUALE OPERATORE
```

Risultato consolidato:

- `TerrainAtlas.png` viene letto tramite catalogo visuale dati;
- il prato supporta varianti statiche pesate;
- la pietra supporta varianti statiche pesate;
- l'acqua supporta animazione catalogata;
- la distribuzione delle varianti e' deterministica per cella, ma non piu'
  bloccata su un pattern troppo evidente;
- il renderer terrain resta guidato da dati runtime, non da logica hardcoded in
  scena.

---

### v0.38i.22 - Terrain Visual Gate Fix

Obiettivo:

```text
correggere solo eventuali problemi emersi dal gate visuale terrain
```

Compiti possibili:

- correggere UV sbagliate;
- correggere tile id mancanti;
- correggere atlas non coerente col catalogo;
- correggere refresh dei chunk animati;
- correggere transizioni non applicate;
- correggere fallback visuali troppo invasivi;
- aggiornare istruzioni asset terrain se il gate mostra ambiguita'.

Output atteso:

```text
Terrain renderer stabile per test minimi ripetibili
```

Esito operativo:

```text
COMPLETATO COME FIX MIRATO POST-GATE
```

Correzioni consolidate:

- le transizioni prato/roccia sono state spostate verso una logica dual-grid;
- la riga dual-grid dell'atlas contiene 14 maschere operative, escludendo i casi
  pieni `0000` e `1111`;
- la lettura della maschera segue l'ordine:
  `alto-sinistra`, `alto-destra`, `basso-sinistra`, `basso-destra`;
- la regola visuale usa il prato come layer superiore e lascia il materiale
  sottostante indipendente;
- i bordi non vengono piu' trattati come semplice autotile cardinale;
- l'acqua resta catalogata come tile animato separato: le transizioni animate
  acqua/prato e acqua/pietra sono rimandate al blocco specifico acqua;
- i dettagli decorativi terrain restano supportati a catalogo come possibilita',
  ma non sostituiscono gli oggetti simulativi veri.

Nota di confine:

```text
Questo step chiude il terreno minimo ArcGraph, non introduce ancora il sistema
oggetti/alberi/vegetazione produttiva.
```

---

### v0.38j - NPC Animation Gate

Obiettivo:

```text
chiudere la resa visuale NPC base con sprite modulari reali
```

Compiti:

- verificare idle a 4 frame;
- verificare walk a 8/9 frame secondo scelta catalogo corrente;
- verificare direzioni `north`, `south`, `east`, `west`;
- verificare movimento fluido multitick da cella a cella;
- verificare ordine layer;
- formalizzare la lista layer NPC minima;
- decidere se mantenere `arms` separato da `body`;
- decidere quando introdurre `hair`, `clothes`, `held_item`;
- mantenere l'ombra automatica come fallback grafico controllato.

Output atteso:

```text
NPC ArcGraph minimo stabile e leggibile con sprite reali
```

Nota corrente:

```text
Il gate visuale base NPC/F12 e' stato superato: sprite modulare visibile, offset
verticale corretto, shadow automatica mantenuta e fallback magenta rimosso dalle
parti modulari mancanti.
```

Nota operativa:

```text
I test visuali sono congelati su richiesta dell'operatore.
Durante il congelamento v0.38j procede solo con preparazione tecnica data-only.
Il probe risorse NPC ora deve riportare parti, direzioni, animazioni, slot catalogo,
prima sprite mancante e summary frame idle/walk prima del recupero del gate Unity.
L'auto-installer runtime prepara il probe sul visual root ArcGraph usando lo stesso
catalogo e lo stesso resolver del renderer NPC.
```

Aggiornamento operativo:

```text
Il gate visuale base NPC/F12 e' stato recuperato e superato. Restano da chiudere
le parti di animazione completa: idle/walk per direzione, movimento fluido in
runtime e completamento del catalogo layer definitivo.
```

Ponte preparatorio collegato:

- `object_defs.json` diventa il punto naturale per unire definizione simulativa e
  definizione visuale degli oggetti;
- il catalogo oggetti puo' esporre footprint XY e dati visuali senza creare un
  secondo catalogo parallelo;
- ArcGraph deve leggere questi dati come snapshot/passaggio read-only, non come
  autorita' simulativa.

Micro-step completato:

- `ArcGraphObjectVisualSnapshot` trasporta ora footprint XY, dimensione sprite,
  offset visuale e flag grafici futuri;
- `ArcGraphObjectRenderItem` conserva gli stessi metadati per i renderer futuri;
- `ArcGraphObjectRenderQueueBuilder` propaga i dati senza interpretare regole
  simulativo/logiche;
- `ArcGraphWorldAdapter` legge `ObjectDef.Visual` una sola volta per oggetto e
  copia i metadati nello snapshot;
- non e' stato introdotto alcun renderer produttivo per alberi, mobili grandi o
  vegetazione.

Esito operativo `v0.38j.08 - Gate visuale muri/F3`:

```text
SUPERATO NEL GATE VISUALE OPERATORE
```

Risultato consolidato:

- il tool F3 puo' inserire `wall_stone` e ArcGraph riceve gli oggetti dalla queue
  runtime;
- il renderer oggetti ArcGraph mostra i muri come sprite alti `32x83`;
- il resolver cardinalita' muri usa la maschera `N/W/S/E` e sceglie la variante
  corretta nella striscia sprite;
- la preview rossa semi-trasparente usa la cella reale risolta dal DevTools, non
  una seconda conversione camera/mouse divergente;
- lo sorting davanti/dietro e' stato corretto per ridurre inversioni visive tra
  segmenti muro;
- il problema di slicing della spritesheet e il successivo disallineamento di
  alcuni sprite sono stati risolti lato asset dall'operatore;
- l'acqua resta non camminabile come dato terrain/oggetto separato gia'
  consolidato nel blocco;
- i test visuali muri non autorizzano ancora il pensionamento fisico di MapGrid:
  confermano solo che il percorso minimo object/wall e' abbastanza stabile per
  procedere con lo switch runtime.

Debito rinviato:

```text
v0.38j.07 - Renderer/overlay mini-tile pavimento 16x16 sotto muri sottili
```

Il mini-tile resta necessario per gestire in modo elegante muri sottili sopra
pavimenti diversi tra interno ed esterno. Non blocca pero' il passaggio a
`v0.38k`, perche' il gate attuale ha validato muri su pavimenti omogenei e non
richiede ancora composizione 16x16 della base.

Prossimo step:

```text
v0.38k - ArcGraph Runtime Switch Stabilization
```

---

### v0.38k - ArcGraph Runtime Switch Stabilization

Obiettivo:

```text
stabilizzare il passaggio MapGrid / ArcGraph senza rompere la simulazione
```

Compiti:

- verificare comportamento `F12`;
- separare root visuali da root runtime;
- evitare overlay doppi;
- evitare spegnimenti di componenti che servono ancora come sorgente dati;
- chiarire se l'auto-installer runtime resta solo gate temporaneo o diventa
  cablaggio ufficiale;
- rendere la diagnostica dello switch leggibile;
- documentare quali oggetti sono accesi in modalita' MapGrid e quali in
  modalita' ArcGraph.

Output atteso:

```text
Switch visuale ripetibile, diagnosticabile e non distruttivo
```

Nota operativa:

```text
Durante il congelamento dei test visuali, lo switcher viene preparato con
diagnostica non visuale aggiuntiva: root assegnati e root effettivamente attivi
per MapGrid/ArcGraph dopo il cambio modalita'.
```

Esito operativo `v0.38k.01 - Late binding robusto F12`:

```text
COMPLETATO COME STABILIZZAZIONE TECNICA NON VISUALE
```

Risultato:

- l'auto-installer ArcGraph non usa piu' `GameObject.Find(...)` per i root
  visuali MapGrid da spegnere/riaccendere;
- la ricerca dei root e dei componenti MapGrid avviene dentro la scena attiva
  includendo anche GameObject inattivi;
- lo switcher riapplica lo stato corrente quando riceve un nuovo cablaggio
  durante il late binding;
- la riapplicazione non processa un frame ArcGraph extra e non ricostruisce
  mesh/queue inutilmente;
- la Console riceve una diagnostica `BindingState` solo quando cambiano root o
  componenti sorgente rilevanti.

Motivazione:

```text
F12 deve essere reversibile anche dopo avere spento root MapGrid. Se il
late-binding perde i riferimenti agli oggetti inattivi, ArcGraph puo' accendersi
ma il ritorno a MapGrid diventa fragile.
```

Prossimo gate:

```text
v0.38k.02 - Test manuale F12:
MapGrid -> ArcGraph -> MapGrid, verificando terrain, NPC, muri e preview F3.
```

Esito operativo `v0.38k.02 - Preparazione gate manuale F12`:

```text
SUPERATO NEL GATE VISUALE OPERATORE
```

Supporto aggiunto:

- `ArcGraphViewModeSwitcher` espone il context menu
  `ArcGraph/Log F12 Manual Gate Probe`;
- il probe non cambia modalita', non processa frame, non crea oggetti e non
  modifica il `World`;
- il probe scrive in Console una riga `F12ManualGateProbe`;
- il campo `readyForVisualCheck=True` indica solo che il cablaggio tecnico e'
  coerente per eseguire il controllo umano;
- il campo non sostituisce la validazione visiva di terrain, NPC, muri e preview
  F3.

Formato atteso della riga Console:

```text
[ArcGraphViewModeSwitcher] F12ManualGateProbe
readyForVisualCheck=True/False,
mode=MapGrid/ArcGraph,
mapGridRoots=N,
arcGraphRoots=N,
mapGridActiveRoots=N,
arcGraphActiveRoots=N,
wrapper=True/False,
terrainRenderer=True/False,
npcRenderer=True/False,
objectRenderer=True/False,
rootsCoherent=True/False
```

Lettura operativa:

- in modalita' MapGrid e' corretto avere `mapGridActiveRoots > 0` e
  `arcGraphActiveRoots = 0`;
- in modalita' ArcGraph e' corretto avere `mapGridActiveRoots = 0` e
  `arcGraphActiveRoots > 0`;
- se `rootsCoherent=False`, il problema e' nello switch dei root visuali;
- se uno tra `wrapper`, `terrainRenderer`, `npcRenderer` o `objectRenderer` e'
  `False`, il problema e' nel cablaggio auto-installer;
- se il probe e' verde ma il Game view e' sbagliato, il problema e' nel renderer
  specifico o negli asset, non nello switch F12.

Validazione operatore:

```text
CONFERMATA
```

Valori confermati in modalita' ArcGraph:

```text
readyForVisualCheck=True
rootsCoherent=True
arcGraphActiveRoots > 0
mapGridActiveRoots = 0
```

Valori confermati in modalita' MapGrid:

```text
readyForVisualCheck=True
rootsCoherent=True
mapGridActiveRoots > 0
arcGraphActiveRoots = 0
```

Conclusione `v0.38k`:

```text
COMPLETATA
```

Lo switch F12 e' ora considerato tecnicamente stabile nel gate minimo:

- MapGrid e ArcGraph alternano correttamente i root visuali;
- il cablaggio auto-installer risulta presente;
- wrapper, terrain renderer, NPC renderer e object renderer risultano collegati;
- non sono richiesti fix mirati `v0.38k.03`;
- il passaggio successivo puo' concentrarsi sull'interazione minima ArcGraph.

---

### v0.38l - ArcGraph Interaction Minimum Gate

Obiettivo:

```text
validare l'interazione minima dentro ArcGraph senza portare dentro tutto il debug
legacy MapGrid
```

Compiti:

- hover cella;
- hover NPC;
- selezione NPC;
- pointer HUD minimo;
- pannello info NPC minimo;
- verifica che l'interazione consumi queue/snapshot ArcGraph;
- evitare dipendenze dirette non necessarie da `MapGridWorldView`;
- mantenere eventuali tool operativi fuori da ArcGraph fino a decisione dedicata.

Output atteso:

```text
ArcGraph permette ispezione minima del mondo visualizzato
```

---

### v0.38m - ArcGraph UI Archetype Foundation

Obiettivo:

```text
creare la base riutilizzabile per pulsanti, menu, pannelli e overlay definitivi
```

Per pulsanti e menu definitivi ArcGraph deve usare archetipi riutilizzabili.
In Unity questo significa combinare:

```text
Prefab UI riutilizzabili
+
componenti C# view/controller piccoli e riusabili
```

Esempi di archetipi previsti:

```text
ArcGraphButton.prefab
ArcGraphTopBar.prefab
ArcGraphSidePanel.prefab
ArcGraphNpcOverlay.prefab
ArcGraphDebugRow.prefab
ArcGraphMenuItem.prefab
```

Principio:

```text
il prefab definisce forma e gerarchia visuale
il componente C# definisce stato e comportamento view-side
la simulazione non viene modificata direttamente da un bottone
```

Flusso corretto:

```text
click bottone
-> evento UI
-> controller/tool autorizzato
-> command ufficiale se serve mutare il World
```

Flusso vietato:

```text
click bottone
-> modifica diretta del World dal componente grafico
```

Compiti:

- definire `ArcGraphUIRoot`;
- definire top bar;
- definire side panel;
- definire area overlay world-space/screen-space;
- definire componente pulsante base;
- definire stati comuni: normale, hover, pressed, disabled, selected;
- definire stile comune: font, colori, padding, bordo, dimensioni;
- definire evento UI neutro, non simulativo;
- creare una prima checklist per prefab UI da costruire manualmente in Unity;
- separare UI definitiva da debug temporaneo.

Output atteso:

```text
base UI ArcGraph riutilizzabile senza duplicare pannelli MapGrid
```

Sotto-roadmap tecnica collegata al distacco MapGrid:

| Step | Obiettivo | Stato |
|---|---|---|
| v0.38m.01 | Introdurre `CellSurfaceLayer` Core come sorgente autoritativa del pavimento cella | ✅ Completato |
| v0.38m.02 | Far leggere ad ArcGraph il terrain da `World.CellSurfaces`, non da `MapGridData` | ✅ Completato |
| v0.38m.03 | Spostare la mappa debug iniziale su file Core `cell_surface_layout.json`, con random map solo futura e seeded | ✅ Completato |
| v0.38m.04 | Sostituire `ArcGraphTerrainRuntimeMapGridAdapter` con sorgente runtime neutra non MapGrid | ✅ Completato |
| v0.38m.05 | Rimuovere dipendenza di `ArcGraphRuntimeSceneAutoInstaller` da `MapGridBootstrap`, `MapGridWorldView` e root visuali MapGrid | ⏳ Parziale |
| v0.38m.06 | Eliminare i probe ArcGraph storici basati su MapGrid o marcarli legacy non runtime | ⏳ Pending |
| v0.38m.07 | Migrare preview/placement ArcGraph fuori da `MapGridWorldProvider` | ⏳ Pending |
| v0.38m.08 | Migrare asset/path oggetti ArcGraph fuori da path `MapGrid/Sprites/Objects` | ⏳ Pending |
| v0.38m.09 | Definire profilo visuale NPC Core/ViewModel invece del fallback renderer hardcoded | ⏳ Pending |
| v0.38m.10 | Creare catalogo unico `surface_defs.json` con dati simulativi e visuali dei pavimenti | ✅ Completato |
| v0.38m.11 | Creare file mappa unico con metadati, layer pavimenti e oggetti iniziali | ✅ Completato |
| v0.38m.12 | Spostare dimensioni mappa fuori da `game_params.json` e rimuovere duplicati tipo `worldWidth/worldHeight` da sorgenti non autoritative | ✅ Completato |
| v0.38m.13 | Implementare loader mappa Core che inizializza dimensioni, `CellSurfaceLayer` e oggetti iniziali dal file mappa unico | ✅ Completato |
| v0.38m.14 | Collassare i cataloghi terrain ArcGraph legacy dentro il catalogo superfici unico, mantenendo solo dati visuali strettamente renderer-side dove necessario | ✅ Completato |
| v0.38m.15 | Pensionare layout/config MapGrid dal percorso runtime ArcGraph dopo verifica del loader mappa unico | ✅ Completato |

Decisione tecnica `v0.38m`:

```text
La mappa debug iniziale deve essere letta da file Core ripetibile.
La generazione random resta ammessa solo come futura modalita' dev con seed esplicito.
ArcGraph non deve piu' usare MapGridData come sorgente terrain.
Le dimensioni mappa devono appartenere al file mappa unico, non a game_params.json.
```

Decisione cataloghi e mappa `v0.38m.10+`:

```text
surface_defs.json
-> definisce i tipi di pavimento possibili, con dati simulativi e visuali.

map file unico
-> definisce dimensioni mappa, pavimento di tutte le celle e oggetti iniziali.

game_params.json
-> non deve piu' essere sorgente autoritativa per worldWidth/worldHeight.
```

Connessioni MapGrid residue da eliminare esplicitamente:

- `ArcGraphTerrainRuntimeMapGridAdapter`: ancora presente come ponte legacy/probe, non piu' sorgente del runtime auto-installato;
- `ArcGraphDebugRuntimeMapGridAdapter`: ancora ponte debug storico per Landmark/GVD;
- `ArcGraphRuntimeSceneAutoInstaller`: ancora agganciato a `Scene_MapGrid`, root visuali MapGrid, DevTools/camera legacy e spegnimento del renderer MapGrid, ma non piu' a `MapGridBootstrap`/`MapGridWorldView` come sorgente context;
- `ArcGraphMinimalRuntimeSceneWrapper`: serializza un `ArcGraphRuntimeContextProvider`, con alias legacy per adapter MapGrid;
- `ArcGraphTerrainRuntimeSceneRenderer`, `ArcGraphTerrainSceneProbeRenderer`, `ArcGraphActorObjectSceneProbeRenderer` e probe collegati: ricevono un provider runtime neutro, ma mantengono alias/nomi legacy per compatibilita' temporanea;
- `ArcGraphPlacementCellHighlightSceneConsumer`: legge ancora `MapGridWorldProvider`;
- camera/viewport: esistono ancora bridge e configurazioni derivate dal vecchio percorso MapGrid;
- object visual path: molti oggetti puntano ancora a sprite sotto `MapGrid/Sprites/Objects`;
- `MapGridFovHeatmapOverlay`: contiene ancora il riferimento operativo per FOV current cone e FOV heatmap debug;
- debug/probe storici: alcune classi restano utili come riferimento, ma non devono restare nel runtime definitivo.

Aggiornamento `v0.38m.15`:

- `ArcGraphRuntimeSceneAutoInstaller` non usa piu' `MapGridBootstrap` o `MapGridWorldView` per costruire il context runtime ArcGraph;
- il percorso runtime auto-installato usa un provider neutro `SimulationHost.World -> ArcGraphRuntimeContext`;
- `ArcGraphMinimalRuntimeSceneWrapper` e `ArcGraphTerrainRuntimeSceneRenderer` ricevono un `ArcGraphRuntimeContextProvider`, non piu' l'adapter MapGrid concreto;
- `ArcGraphTerrainRuntimeMapGridAdapter` resta fisicamente presente come legacy/probe, ma non e' piu' la sorgente del runtime auto-installato;
- i probe terrain/actor/object/wiring possono ricevere lo stesso provider neutro, pur mantenendo nomi diagnostici legacy per compatibilita';
- resta parziale `v0.38m.05`: i root visuali MapGrid sono ancora cercati e spenti per evitare doppio rendering dentro `Scene_MapGrid`;
- restano da eliminare in step successivi: DevTools/placement MapGrid, camera bridge legacy, path sprite oggetti sotto `MapGrid/Sprites/Objects`, debug FOV MapGrid e probe storici.

Regola di avanzamento:

```text
Ogni dipendenza MapGrid rimasta deve diventare una delle tre cose:
1. assorbita in un contratto ArcGraph/Core neutro;
2. marcata legacy/probe e non usata nel runtime;
3. rimossa quando il sostituto ArcGraph e' validato.

Vincolo aggiuntivo:
MapGridFovHeatmapOverlay non puo' essere cancellato fisicamente finche'
ArcGraph non possiede un equivalente verificato per FOV current cone e FOV
heatmap debug, attivabile come modalita' di visualizzazione e alimentato da
snapshot/producer autorizzato.
```

---

### v0.38n - ArcGraph Debug/UI Migration Cleanup

Obiettivo:

```text
spostare solo i debug utili nel nuovo schema modulare ArcGraph
```

Compiti:

- non copiare `MapGridWorldView` come blocco unico;
- distinguere debug temporaneo da UI definitiva;
- migrare pointer, selection e pannelli informativi minimi;
- assorbire il comportamento utile di `MapGridFovHeatmapOverlay` in un modulo ArcGraph dedicato, senza copiare il monolite `MapGridWorldView`;
- attivare FOV current cone / FOV heatmap da icona visualizzazione, non da DevTools runtime;
- mantenere DevTools e command tools fuori dal core ArcGraph;
- preparare pannelli leggibili ma non onniscienti per il giocatore/operatore.

Sotto-step FOV bloccante:

| Step | Obiettivo | Stato |
|---|---|---|
| v0.38n.01 | Auditare `MapGridFovHeatmapOverlay` e separare calcolo dati FOV da resa visuale debug | ✅ Completato |
| v0.38n.02 | Creare producer/snapshot ArcGraph autorizzato per FOV current cone e/o heatmap | ✅ Completato |
| v0.38n.03 | Creare consumer overlay ArcGraph attivabile dalla ViewModeBar / icona visualizzazione | ✅ Completato |
| v0.38n.04 | Confrontare visivamente ArcGraph FOV con MapGrid FOV prima di autorizzare rimozione legacy | ⏳ Pending |

Aggiornamento `v0.38n.01-v0.38n.03`:

- il metodo utile di `MapGridFovHeatmapOverlay.RenderCurrentCone(...)` e' stato assorbito come producer ArcGraph read-only;
- il calcolo del cono usa ancora la geometria Core autorizzata: `FovUtils.Manhattan`, `FovUtils.IsInCone`, `FovUtils.IsInFront` e `World.HasLineOfSight`;
- il producer non disegna, non muta il World, non marca dirty e non aggiorna telemetry;
- ArcGraph possiede un consumer pooled world-space per celle FOV osservate e celle watched-margin;
- la ViewModeBar collega il pulsante `FOV` a un controller ArcGraph, non a `MapGridWorldView`;
- il comportamento resta da validare in Unity: selezione NPC, FOV ON/OFF, confronto visivo con MapGrid e costo runtime.

Nota di pensionamento MapGrid:

```text
MapGrid non e' ancora cancellabile fisicamente solo perche' esiste il producer FOV.
Prima della rimozione servono:
1. compile Unity pulita;
2. test Play Mode;
3. conferma visiva ArcGraph FOV;
4. audit finale delle dipendenze runtime ancora attive.
```

Audit statico residui MapGrid dopo assorbimento FOV:

| Area | Stato | Decisione |
|---|---|---|
| `MapGridFovHeatmapOverlay` | metodo current cone assorbito in producer ArcGraph | non piu' blocco concettuale, ma serve confronto Unity |
| `ArcGraphPlacementCellHighlightSceneConsumer` | usa ancora `MapGridRuntimeDevToolsOverlay` e `MapGridWorldProvider` | blocco attivo prima della cancellazione |
| `ArcGraphRuntimeSceneAutoInstaller` | usa ancora `MapGridRuntimeDevToolsOverlay`, `MapGridCameraController` e root visuali legacy da spegnere | blocco attivo prima della cancellazione |
| `ArcGraphCameraViewportController` / `ArcGraphInteractionSceneAdapterWrapper` | conservano bridge verso `MapGridCameraController` | blocco attivo prima della cancellazione |
| `ArcGraphTerrainRuntimeMapGridAdapter` / `ArcGraphDebugRuntimeMapGridAdapter` | ancora presenti come legacy/probe | da marcare non-runtime o rimuovere dopo sostituti |
| sprite oggetti | fallback ancora verso `MapGrid/Sprites/Objects` | da migrare su path/catalogo ArcGraph |
| `ArcGraphViewModeSwitcher` | conserva enum/root MapGrid per transizione e diagnostica | da semplificare quando ArcGraph resta unica vista |

Conclusione:

```text
Dopo v0.38n.03 MapGrid non e' ancora cancellabile fisicamente.
Il prossimo passaggio deve essere v0.38o audit/fix dei bridge attivi, non delete.
```

Output atteso:

```text
debug ArcGraph leggibile, modulare e separato dai tool legacy
```

---

### v0.38o - MapGrid Dependency Audit Finale

Obiettivo:

```text
capire cosa impedisce ancora di pensionare MapGrid
```

Compiti:

| Step | Obiettivo | Stato |
|---|---|---|
| v0.38o.01 | Auditare bridge attivi MapGrid ancora usati da ArcGraph | ✅ |
| v0.38o.02 | Eliminare fallback sprite oggetti ArcGraph verso `MapGrid/Sprites/Objects/{defId}` | ✅ |
| v0.38o.03 | Separare preview placement/F3 da tipo concreto `MapGridRuntimeDevToolsOverlay` | ✅ |
| v0.38o.04 | Scollegare pan/zoom ArcGraph da offset camera MapGrid | ✅ |
| v0.38o.05 | Formalizzare `ARCGRAPH_VISUAL_ASSET_POLICY.md` per oggetti, NPC, piante, vegetazione e resolver sprite | ✅ |
| v0.38o.06 | Progettare e implementare controller placement/F3 autonomo ArcGraph | ✅ |
| v0.38o.07 | Migrare path asset oggetti da `MapGrid/Sprites/Objects` a catalogo/path ArcGraph reali | ✅ |
| v0.38o.08 | Introdurre `ArcGraphEnvironmentVisualCatalog.json` per vegetazione diffusa e PlantInstance | ✅ |
| v0.38o.09 | Collegare `plantVisualStateKey` / `visualStateKey` al catalogo ambiente ArcGraph | ⏳ |
| v0.38o.10 | Estendere `ArcGraphNpcVisualCatalog.json` per supportare sheet per parte/animazione | ⏳ |
| v0.38o.10a | Introdurre importer Aseprite NPC per generare sheet PNG sliced per parte corpo e direzione | ✅ |
| v0.38o.11 | Eliminare o congelare adapter/probe MapGrid non-runtime | ⏳ |
| v0.38o.12 | Rimuovere `ArcGraphViewModeSwitcher`/root switch legacy quando ArcGraph resta unica vista | ⏳ |
| v0.38o.13 | Autorizzare cancellazione fisica MapGrid solo dopo test Unity e zero dipendenze runtime | ⏳ |

Sotto-roadmap operativa derivata da `ARCGRAPH_VISUAL_ASSET_POLICY.md`:

| Policy step | Obiettivo policy | Step operativo | Stato |
|---|---|---|---|
| 1 | Congelare la policy e usarla come vincolo per i prossimi step | `v0.38o.05` | ✅ |
| 2 | Migrare `object_defs.json` da path MapGrid a path ArcGraph reali solo dopo asset equivalenti | `v0.38o.07` | ✅ |
| 3 | Estendere `ArcGraphNpcVisualCatalog.json` per supportare sheet per parte/animazione senza rompere il formato attuale | `v0.38o.10` + `v0.38o.10a` | ⏳ |
| 4 | Introdurre `ArcGraphEnvironmentVisualCatalog.json` per vegetazione diffusa e PlantInstance | `v0.38o.08` | ✅ |
| 5 | Collegare `plantVisualStateKey` / `visualStateKey` al catalogo ambiente | `v0.38o.09` | ⏳ |
| 6 | Rimuovere o congelare i vecchi pattern frame-sciolti quando le sheet NPC sono realmente disponibili | dopo `v0.38o.10`, solo con asset sheet disponibili | ⏳ |
| 7 | Eliminare ogni path MapGrid residuo dai cataloghi ArcGraph prima della cancellazione fisica di MapGrid | `v0.38o.07` + gate finale `v0.38o.13` | ⏳ |

Nota di sequenza:

```text
Il prossimo step e' v0.38o.09: il placement/F3 operativo ha ora un controller
ArcGraph autonomo, object_defs usa path ArcGraph e il catalogo visuale ambiente
esiste come contratto data-only. Resta da collegare il feed `visualStateKey` /
`plantVisualStateKey` al catalogo senza far leggere ArcGraph direttamente alla
biosfera mutabile.
```

Aggiornamento `v0.38o.01-v0.38o.04`:

- ArcGraph non genera piu' fallback automatici per oggetti mancanti con path `MapGrid/Sprites/Objects/{defId}`;
- `ObjectDef.ResolveArcGraphSpritePath()` restituisce solo `Visual.SpritePath`;
- se una definizione oggetto non dichiara path visuale, ArcGraph produce sprite key vuota e il renderer la segnala come missing;
- la preview placement ArcGraph non dipende piu' dal tipo concreto `MapGridRuntimeDevToolsOverlay`;
- aggiunto contratto neutro `IArcGraphPlacementPreviewSource`;
- il DevTools legacy implementa temporaneamente questo contratto, ma ArcGraph consuma solo l'interfaccia;
- la preview placement legge il `World` tramite `ArcGraphRuntimeContextProvider`, non tramite `MapGridWorldProvider`;
- `ArcGraphCameraViewportController` non chiama piu' `MapGridCameraController.ApplyExternalCameraOffset`;
- `ArcGraphInteractionSceneAdapterWrapper` non chiama piu' `MapGridCameraController.ApplyExternalCameraOffset`;
- `ArcGraphRuntimeSceneAutoInstaller` disabilita il `MapGridCameraController` legacy quando ArcGraph e' installato.

Aggiornamento `v0.38o.05`:

- aggiunto `ARCGRAPH_VISUAL_ASSET_POLICY.md` come policy root operativa per
  cataloghi visuali ArcGraph;
- confermato che `object_defs.json` resta il catalogo per oggetti fisici,
  muri, porte, mobili, costruzioni e risorse concrete;
- confermato che piante vive e vegetazione biosfera non devono essere infilate
  in `object_defs.json`, salvo quando diventano oggetti raccolti o manipolabili;
- definito il flusso biosfera:
  `speciesKey/growthStage/healthBand/visualStateKey -> World -> ArcGraph -> SpriteKey`;
- definito il catalogo futuro `ArcGraphEnvironmentVisualCatalog.json` per
  `VegetationAreas` e `Plants`;
- confermato che il resolver sprite ArcGraph puo' risolvere sprite singole e
  sheet sliced tramite chiave `sheet#subSprite`, ma non deve leggere World o
  generare fallback impliciti verso MapGrid;
- dichiarata la migrazione futura NPC verso sheet per parte/animazione, lasciando
  supportato il formato corrente a frame separati finche' gli asset sheet non
  esistono.

Aggiornamento `v0.38o.10a`:

- introdotto `ArcGraphAsepriteNpcSheetImporter` come componente GameObject
  attivabile manualmente da Inspector/context menu;
- introdotto `ArcGraphAsepriteNpcSheetImporterEditorService` come servizio
  Editor-only confinato sotto `Assets/Scripts/Editor`;
- l'importer legge file `.aseprite` / `.ase` con 12 layer sorgente
  `south_*`, `north_*`, `west_*`;
- genera quattro PNG sheet per parte corpo: `legs`, `body`, `arms`, `head`;
- ogni sheet produce slice `south_00`, `east_00`, `north_00`, `west_00`
  per ogni frame disponibile;
- la direzione `east` viene derivata dalla finestra laterale `west` tramite
  ribaltamento orizzontale;
- lo slicing usa `sliceWidthPixels` e `sliceHeightPixels`, con layout sorgente
  orizzontale in tre finestre: south, north, laterale;
- il componente non modifica runtime simulativo, non parte in automatico e non
  aggancia ancora `ArcGraphNpcVisualCatalog.json`;
- `v0.38o.10` resta aperta per l'estensione effettiva del catalogo NPC e per
  l'eventuale passaggio runtime dai pattern frame-sciolti alle sheet.

Audit residuo dopo `v0.38o.04`:

| Area | Stato | Decisione |
|---|---|---|
| F3 / placement operativo | `ArcGraphPlacementToolController` accoda `DevPlaceObjectCommand` e l'installer preferisce questa sorgente al DevTools legacy | validare in Unity, poi migrare la scelta operazione nel pannello azione |
| Path oggetti catalogo | `object_defs.json` dichiara path `ArcGraph/Objects/...` e non contiene piu' duplicati bed/chair/door legacy | produrre asset PNG/sheet equivalenti secondo `ARCGRAPH_VISUAL_ASSET_POLICY.md` |
| Catalogo ambiente visuale | `ArcGraphEnvironmentVisualCatalog.json` esiste e indicizza `VisualKey`, piante fisiche e vegetazione diffusa sui contratti biosfera reali | collegare il feed runtime nello step v0.38o.09 |
| Catalogo NPC visuale | `ArcGraphNpcVisualCatalog.json` usa ancora pattern frame separati | migrare gradualmente verso sheet per parte/animazione quando gli asset esistono |
| Camera / pan / zoom | ArcGraph sposta direttamente la camera, ma l'installer cerca ancora il controller MapGrid solo per spegnerlo | rimuovere quando la scena non contiene piu' MapGrid |
| Adapter/probe MapGrid | `ArcGraphTerrainRuntimeMapGridAdapter` e `ArcGraphDebugRuntimeMapGridAdapter` restano nel codice | congelare come legacy/probe o rimuovere dopo test |
| View switcher | `ArcGraphViewModeSwitcher` conserva modalita' MapGrid | eliminare quando non serve piu' F12/switch legacy |
| Root visuali legacy | l'installer spegne ancora root MapGrid noti | eliminare dopo scena ArcGraph autonoma |

Decisione pan/zoom:

```text
Non riusare MapGridCameraController.
Il sistema corretto e':
ArcGraphViewConfig JSON -> ArcGraphViewController puro -> ArcGraphCameraViewportController Unity.
Il wrapper Unity deve gestire solo:
- input rotellina;
- pan con rotellina premuta;
- zoom-to-cursor;
- clamp ai bordi;
- eventuale smoothing locale;
- aggiornamento diretto della camera.
```

La cancellazione fisica di MapGrid resta non autorizzata dopo questo step:

```text
MapGrid contiene ancora DevTools operativo, asset oggetti dichiarati dal catalogo,
componenti legacy/probe e root/switch di scena. Prima bisogna sostituire questi
blocchi uno alla volta e validarli in Unity.
```

Output atteso:

```text
piano tecnico esplicito per il pensionamento controllato di MapGrid
```

---

### v0.38p - ArcGraph Minimum Stable Closure

Obiettivo:

```text
chiudere ArcGraph come vista minima stabile
```

Criteri di chiusura:

- terrain visibile e configurabile;
- NPC visibili e animabili;
- switch runtime funzionante;
- interaction minima funzionante;
- UI archetype foundation presente;
- debug minimo presente;
- FOV debug MapGrid assorbito o dichiarato esplicitamente bloccante per la rimozione fisica di MapGrid;
- dipendenze MapGrid residue dichiarate;
- nessuna mutazione simulativa dal renderer;
- nessun accesso onnisciente introdotto;
- documentazione e taskboard aggiornate.

Output atteso:

```text
ArcGraph minimo stabile, pronto per ricevere moduli visuali futuri uno alla volta
```

---

## Roadmap v0.39-v0.52 - Biosfera / Environment Foundation

La roadmap BIOSFERA nasce dalla pagina Notion `BIOSFERA` e dal rapido audit di
compatibilita' con ArcGraph.

Obiettivo generale:

```text
introdurre una biosfera sistemica, stratificata e compatibile con la futura vista
ArcGraph, senza trasformare oggetti, rendering o MapGrid in source of truth
ambientale
```

Principio architetturale:

```text
Core Environment possiede dati e regole ambientali.
ArcGraph ricevera' in futuro snapshot visuali derivati.
ArcGraph non deve possedere clima, crescita, acqua, fertilita' o seed bank.
```

Regole iniziali:

- non introdurre rendering dentro `Assets/Scripts/Core/Environment`;
- non creare ponte diretto con ArcGraph prima di `v0.52`;
- non modificare `World.cs` o `SimulationHost` nella foundation data-only;
- non usare `WorldObjectInstance` come contenitore universale della biosfera;
- distinguere sempre proprieta' di cella, area ambientale, oggetto naturale, pianta importante e actor;
- usare coordinate compatibili con `x/y/z`, anche se il primo runtime opera solo su `z = 0`;
- produrre snapshot read-only Core-side, non snapshot ArcGraph-side;
- mantenere update rari e cadenzati: orario/giornaliero/stagionale, non frame-based.

Problemi progettuali da chiudere prima del runtime produttivo:

| Nodo | Rischio | Decisione provvisoria |
|---|---|---|
| Scala tempo reale/simulato | La pagina BIOSFERA conteneva durate non coerenti tra loro | Decisione: `24 ore simulate = 20 minuti reali`; valori futuri in file di configurazione |
| Cella come contenitore unico | Saturazione del vincolo `1 oggetto per cella` | Cella come coordinata + layer sovrapposti |
| Aree sovrapposte | Un solo `AreaId` non basta | Registry per layer: fertility/water/vegetation/room/territory |
| Vegetazione diffusa | Troppe entita' se ogni filo d'erba diventa oggetto | `VegetationArea` + seedBank astratta |
| Piante importanti | Ambiguita' tra oggetto e pianta viva | `PlantInstance` ambientale, con output risorse futuro |
| Acqua | Rischio fluidodinamica precoce | Profondita' discreta e aree stabili, niente simulazione continua |
| Z-level | Rischio esplosione scope | Contratti `x/y/z` subito, uso produttivo multilivello rinviato |
| ArcGraph | Rischio coupling Core -> View | Adapter differito a `v0.52` |

Schema concettuale canonico:

```text
Mappa = griglia stabile
Cella = coordinate + layer
Oggetti = solo uno dei layer
Piante = layer separato
NPC/animali = actor layer
Acqua = liquid layer
Altitudine = terrain/elevation layer
Ecosistema = aree chiuse sovrapposte
Rendering = consumer separato che legge snapshot derivati
```

---

### v0.39 - Environment Foundation Data-Only

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
creare il vocabolario dati minimo della biosfera senza attivare sistemi runtime
```

Perimetro:

- nuovi file solo sotto `Assets/Scripts/Core/Environment`;
- contratti value-only e DTO passivi;
- nessun riferimento a `Arcontio.View.ArcGraph`;
- nessun riferimento operativo a MapGrid;
- nessun renderer;
- nessun `MonoBehaviour`;
- nessuna modifica a scene, prefab o `.meta`.

Contratti previsti:

| Blocco | Strutture candidate | Stato |
|---|---|---|
| Coordinate | `EnvironmentCellCoord`, coordinate `x/y/z` | ✅ Completato |
| Aree | `EnvironmentAreaId`, `EnvironmentAreaKind`, `EnvironmentAreaBounds`, `EnvironmentAreaDefinition` | ✅ Completato |
| Calendario | `EnvironmentCalendarState`, `EnvironmentDate`, `EnvironmentTimeOfDay` | ✅ Completato |
| Stagioni | `EnvironmentSeasonKind`, `EnvironmentSeasonProfile` | ✅ Completato |
| Clima | `EnvironmentGlobalClimateState`, `EnvironmentWeatherKind` | ✅ Completato |
| Fertilita' | `EnvironmentFertilityAreaState` | ✅ Completato |
| Acqua | `EnvironmentWaterAreaState`, `EnvironmentWaterDepthLevel` | ✅ Completato |
| Vegetazione | `EnvironmentVegetationAreaState`, `EnvironmentSeedBankEntry` | ✅ Completato |
| Snapshot | `EnvironmentSnapshot`, `EnvironmentAreaSnapshot`, snapshot per dominio | ✅ Completato |

Definition of Done:

- contratti compilabili e commentati;
- nessun codice eseguito automaticamente;
- nessun coupling con ArcGraph/MapGrid;
- nessuna modifica a `World.cs` o `SimulationHost`;
- snapshot Core-side pronti per consumer futuri.

Esito implementativo:

- commit `8e2d159055772e43004e498034d334614acff7a4`;
- namespace Core: `Assets/Scripts/Core/Environment`;
- nessun rendering produttivo;
- nessun `MonoBehaviour`;
- nessuna modifica a `World.cs`, `SimulationHost`, `MapGrid`, scene, prefab o `.meta`;
- snapshot e query read-only pronti per consumer futuri.

---

### v0.40 - Calendar, Time, Season

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
definire il tempo ambientale canonico e derivare ora, giorno, mese, anno e stagione
```

Decisione obbligatoria iniziale:

- usare come baseline `24 ore simulate = 20 minuti reali`;
- derivare da questa scala la durata dell'ora simulata: `1 ora simulata = 50 secondi reali`;
- dichiarare tutti questi valori come configurabili, non hardcoded;
- collocare giorni per mese, mesi per anno, durata giorno, stagioni e profili luce in file di configurazione dedicati.

Componenti previsti:

| Componente | Funzione | Stato |
|---|---|---|
| Calendar resolver | converte tick ambientali in data/ora | ✅ Completato |
| Season resolver | ricava stagione da giorno/mese | ✅ Completato |
| Daylight resolver | calcola ore luce e intensita' luce | ✅ Completato |
| Config data | giorni per mese, mesi per anno, stagioni | ✅ Completato come DTO/config foundation |

Valori baseline da configurare:

| Parametro | Valore baseline | Stato |
|---|---|---|
| ore per giorno | 24 | ✅ Configurato come baseline |
| durata giorno simulato | 20 minuti reali | ✅ Configurato come baseline |
| durata ora simulata | 50 secondi reali | ✅ Derivato dalla baseline |
| giorni per mese | 25 | ✅ Configurato come baseline |
| mesi per anno | 12 | ✅ Configurato come baseline |
| giorni per anno | 300 | ✅ Derivato dalla baseline |
| stagione primavera | 12 ore luce / 12 buio | ✅ Configurato come baseline |
| stagione estate | 16 ore luce / 8 buio | ✅ Configurato come baseline |
| stagione autunno | 12 ore luce / 12 buio | ✅ Configurato come baseline |
| stagione inverno | 8 ore luce / 16 buio | ✅ Configurato come baseline |

Vincoli:

- il calendario non deve modificare NPC, World o Jobs;
- il giorno/notte produce solo dati ambientali;
- l'eventuale impatto su visione, memoria o decisione resta fuori da questa fase.

---

### v0.41 - Global Climate & Weather

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
creare clima e meteo globale leggeri, derivati da stagione e calendario
```

Strutture previste:

- `EnvironmentSeasonClimateProfile`;
- `EnvironmentWeatherState`;
- `EnvironmentWeatherKind`;
- `EnvironmentDailyWeatherPlan`;
- `EnvironmentGlobalClimateState`.

Regole previste:

| Regola | Descrizione | Stato |
|---|---|---|
| Generazione giornaliera | meteo principale scelto a inizio giorno | ✅ Foundation pronta |
| Variazione oraria | piccole oscillazioni temperatura/umidita' | ✅ Foundation pronta |
| Persistenza | parte del meteo precedente influenza il successivo | ✅ Foundation pronta |
| Stagionalita' | primavera/estate/autunno/inverno pesano eventi diversi | ✅ Foundation pronta |
| Aridita' | caldo e vento aumentano aridita', pioggia/neve la riducono | ✅ Foundation pronta |

Fuori scope:

- rendering pioggia/neve;
- effetti su pathfinding;
- modifiche dirette a bisogni NPC;
- eventi estremi produttivi.

---

### v0.42 - Environment Area Registry

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
introdurre il registro delle aree ambientali sovrapposte
```

Concetto:

```text
una cella puo' appartenere contemporaneamente a un'area fertile, umida,
vegetata, coperta, interna, territoriale o di caccia
```

Componenti previsti:

| Componente | Funzione | Stato |
|---|---|---|
| Area registry | elenco aree e lookup per id | ✅ Completato |
| Area layer index | separa fertility/water/vegetation/room/territory | ✅ Completato per layer foundation |
| Cell membership | appartenenza cella -> area per layer | ✅ Completato come query/bounds |
| Bounds/mask | bounding box e futura maschera celle | ✅ Bounds completati, maschera fine rinviata |
| Chunk mask | elenco aree presenti per chunk | ⏳ Pending runtime/ottimizzazione futura |

Vincoli:

- nessuna scansione totale calda a ogni tick;
- no `AreaIdGrid` unico per tutti i layer;
- no dipendenza da MapGridData;
- no rendering.

---

### v0.43 - Fertility Areas

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
modellare la fertilita' come proprieta' di area, non come oggetto o umidita' fine-grained
```

Strutture previste:

- `EnvironmentFertilityAreaState`;
- `EnvironmentSoilKind`;
- `EnvironmentFertilitySnapshot`.

Dati minimi:

| Campo | Significato | Stato |
|---|---|---|
| `BaseFertility01` | fertilita' naturale dell'area | ✅ Completato |
| `CurrentFertility01` | fertilita' runtime modificabile lentamente | ✅ Completato |
| `SoilKind` | tipo terreno/suolo astratto | ✅ Completato |
| `GrowthModifier01` | modificatore crescita piante | ✅ Completato |
| `Exhaustion01` | esaurimento da uso agricolo futuro | ✅ Completato |
| `Recovery01` | ripristino naturale lento | ✅ Completato |

Fuori scope:

- umidita' per cella;
- chimica del suolo;
- erosione;
- agricoltura produttiva.

---

### v0.44 - Water Areas / Water Map

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
modellare acqua stabile e profondita' discreta come layer ambientale
```

Strutture previste:

- `EnvironmentWaterAreaState`;
- `EnvironmentWaterKind`;
- `EnvironmentWaterDepthLevel`;
- `EnvironmentWaterCellSnapshot`.

Profondita' iniziale:

| Livello | Significato | Stato |
|---|---|---|
| 0 | nessuna acqua | ✅ Completato |
| 1 | pozzanghera / basso | ✅ Completato |
| 2 | guado / medio | ✅ Completato |
| 3 | profondo | ✅ Completato |
| 4 | molto profondo / pericolo | ✅ Completato |

Vincoli:

- niente fluidodinamica continua;
- niente propagazione pressione;
- niente pathfinding acquatico;
- niente renderer acqua;
- valori leggibili da snapshot futuri.

---

### v0.45 - Vegetation Areas & SeedBank

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
gestire vegetazione diffusa come area astratta con seedBank, non come migliaia di oggetti
```

Strutture previste:

- `EnvironmentVegetationAreaState`;
- `EnvironmentVegetationKind`;
- `EnvironmentSeedBankEntry`;
- `EnvironmentVegetationDensitySnapshot`.

Dati minimi:

| Campo | Significato | Stato |
|---|---|---|
| `Density01` | densita' vegetazione diffusa | ✅ Completato |
| `SpeciesKeys` | specie/categorie possibili | ✅ Completato |
| `SeedBank` | pressione semi astratta per specie | ✅ Completato |
| `FertilityInfluence01` | influenza fertilita' | ✅ Completato |
| `ClimateInfluence01` | influenza clima globale | ✅ Completato |
| `GrowthPotential01` | capacita' di crescita corrente | ✅ Completato |

Regola chiave:

```text
seed naturali diffusi = valore dell'area
semi agricoli posseduti dagli NPC = risorsa concreta futura
```

---

### v0.46 - Plant Catalog

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
definire un catalogo data-driven delle specie vegetali importanti
```

Campi previsti per specie:

- `SpeciesKey`;
- categoria: erba, arbusto, albero, coltura, medicinale;
- stadi crescita;
- giorni richiesti per stadio;
- stagioni favorevoli;
- temperatura ideale;
- umidita' ideale;
- fertilita' minima;
- risorse prodotte;
- comportamento stagionale: annuale, decidua, sempreverde.

Fuori scope:

- loader produttivo se non necessario al primo pass;
- sprite reali;
- rendering stadi;
- job agricoli.

---

### v0.47 - PlantInstance Lifecycle

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
introdurre piante importanti come entita' ambientali concrete
```

Strutture previste:

- `EnvironmentPlantInstance`;
- `EnvironmentPlantGrowthStage`;
- `EnvironmentPlantHealthState`;
- `EnvironmentPlantSnapshot`.

Dati minimi:

| Campo | Significato | Stato |
|---|---|---|
| `PlantId` | identita' runtime/persistibile futura | ✅ Completato |
| `SpeciesKey` | specie dal catalogo | ✅ Completato |
| `Cell` | coordinata x/y/z | ✅ Completato |
| `AgeDays` | eta' in giorni ambientali | ✅ Completato |
| `GrowthStage` | germoglio/giovane/adulta/secca/morta | ✅ Completato |
| `Health01` | salute normalizzata | ✅ Completato |
| `Maturity01` | maturita' produttiva | ✅ Completato |

Regola:

```text
erba diffusa = area
albero / arbusto raccoglibile / coltura = PlantInstance
```

---

### v0.48 - Natural Growth Loop

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
chiudere il primo ciclo naturale area -> seedBank -> PlantInstance -> crescita -> morte
```

Pipeline prevista:

```text
VegetationArea
-> legge clima globale, luce, stagione, fertilita', acqua vicina
-> calcola GrowthScore giornaliero
-> aggiorna seedBank
-> genera alcune PlantInstance se ci sono celle adatte
-> aggiorna crescita/salute piante
-> gestisce morte/secchezza/decomposizione leggera
```

Vincoli:

- update giornaliero, non per tick;
- limiti massimi per area;
- nessuna scansione completa continua;
- nessun impatto diretto su NPC;
- nessun rendering.

---

### v0.49 - Agriculture Foundation

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
separare coltivazione intenzionale dalla vegetazione naturale
```

Blocchi previsti:

| Blocco | Funzione | Stato |
|---|---|---|
| Crop plant | coltura come PlantInstance controllata | ✅ Completato come contratto |
| Cultivated area | area coltivabile o campo | ✅ Completato come contratto |
| Seed resource boundary | semi agricoli come risorsa concreta futura | ✅ Completato come boundary |
| Harvest output | produzione raccolto come dato, non ancora job | ✅ Completato come dato |
| Job hooks futuri | punti di integrazione semina/raccolta | ✅ Completato come hook passivo |

Fuori scope:

- implementazione Job semina/raccolta;
- economia agricola;
- magazzini agricoli;
- UI agricola.

---

### v0.50 - Environment Read-Only Snapshots

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
stabilizzare il modello di osservazione read-only della biosfera
```

Snapshot previsti:

- `EnvironmentCalendarSnapshot`;
- `EnvironmentClimateSnapshot`;
- `EnvironmentWeatherSnapshot`;
- `EnvironmentFertilitySnapshot`;
- `EnvironmentWaterSnapshot`;
- `EnvironmentVegetationSnapshot`;
- `EnvironmentPlantSnapshot`;
- `EnvironmentAreaSnapshot`;
- `EnvironmentFullSnapshot`.

Vincoli:

- snapshot Core-side;
- nessun tipo ArcGraph;
- nessun riferimento mutabile a registry interni;
- adatti a UI/debug/save/load/adapter futuri;
- compatibili con `x/y/z`.

---

### v0.51 - Save/Load Environment

**Stato:** ✅ Completata foundation/data-only

Obiettivo:

```text
rendere persistibile lo stato ambientale realmente necessario
```

Dati candidati:

| Dato | Persistenza | Stato |
|---|---|---|
| calendario | persistente | ✅ Completato come DTO |
| clima/meteo corrente | persistente o ricostruibile dichiarato | ✅ Completato come DTO |
| area registry | persistente | ✅ Completato come DTO |
| fertility areas | persistente | ✅ Completato come DTO |
| water areas | persistente | ✅ Completato come DTO |
| vegetation areas | persistente | ✅ Completato come DTO |
| seedBank | persistente | ✅ Completato come DTO |
| PlantInstance | persistente | ✅ Completato come DTO |
| snapshot visuali | non persistenti | ✅ Dichiarato ricostruibile/non persistente |

Vincoli:

- distinguere stato persistente, volatile e ricostruibile;
- non salvare dati puramente visuali;
- non rendere ArcGraph parte del save.

---

### v0.52 - ArcGraph Environment Adapter

**Stato:** ✅ Completata adapter passivo

Obiettivo:

```text
collegare la biosfera Core ad ArcGraph solo tramite adapter read-only
```

Conversioni future:

| Core Environment | ArcGraph visuale | Stato |
|---|---|---|
| water snapshot | `ArcGraphWaterVisualSnapshot` | ✅ Completato |
| vegetation snapshot | `ArcGraphVegetationVisualSnapshot` | ✅ Completato |
| weather snapshot | `ArcGraphWeatherVisualSnapshot` | ✅ Completato |
| light/day-night snapshot | `ArcGraphLightVisualSnapshot` | ✅ Completato |
| effects ambientali futuri | `ArcGraphEffectVisualSnapshot` | ✅ Completato come proiezione passiva |

Vincoli:

- adapter fuori dal Core Environment;
- ArcGraph resta consumer;
- nessuna simulazione dentro ArcGraph;
- nessuna dipendenza inversa Core -> View;
- nessuna attivazione automatica senza gate dedicato.

Closeout documentale:

- pagine diario Notion create per `v0.39`-`v0.52`;
- adapter mantenuto fuori dal Core Environment;
- ArcGraph resta consumer di snapshot/proiezioni;
- integrazione runtime con tick ufficiale ancora da pianificare tramite boundary neutro;
- nessuna attivazione automatica della biosfera nel runtime produttivo.

---

## Roadmap v0.53-v0.69 - Biosfera / World Physical Integration

La roadmap `v0.53-v0.69` formalizza il passaggio dalla foundation data-only alla
presenza fisica controllata della biosfera nel `World`.

Principio architetturale:

```text
Biosfera = verita' biologica
World = verita' spaziale/fisica
ArcGraph = verita' visuale derivata
NPC = consumer tramite query, memoria, landmark ed eventi
```

Regole di separazione:

- la biosfera conserva specie, eta', salute, stadio, maturita', seed bank, fertilita',
  acqua, clima e densita' vegetazione;
- il `World` conserva celle, superfici, occupazione, blocco movimento, blocco vista,
  coordinate fisiche e dirty propagation;
- ArcGraph non legge la biosfera come authority, ma riceve snapshot/delta derivati;
- la vegetazione decorativa pura sporca solo la vista;
- le piante fisiche che bloccano movimento o vista sporcano `World`, FOV e percezione;
- le piante non devono duplicare tutto lo stato biologico dentro `World`;
- `World` puo' conservare solo il riferimento fisico minimo alla pianta biologica:
  id biosfera, cella, blocchi fisici e visual key corrente.

Base Core gia' disponibile da `fec5435`:

| Elemento | Funzione | Stato |
|---|---|---|
| `surface_defs.json` | catalogo tipi superficie, con dati simulativi e visuali | ✅ Completato |
| `world_map_default.json` | file mappa iniziale unico con dimensioni e layer pavimenti | ✅ Completato |
| `World.CellSurfaces` / `CellSurfaceLayer` | stato per-cella della superficie presente | ✅ Completato |
| `World.SurfaceDefs` | catalogo runtime delle definizioni superficie | ✅ Completato |
| dimensioni mappa fuori da `game_params.json` | `World` prende dimensioni dal file mappa | ✅ Completato |

Vincoli per la biosfera:

- usare `World.CellSurfaces` come sorgente delle superfici per cella;
- usare `World.SurfaceDefs` / `surface_defs.json` come catalogo dei tipi superficie;
- non leggere `MapGridData`;
- non leggere direttamente sprite o tile id per logica simulativa;
- usare solo `MacroSurface`, `BaseFertility01`, `BaseMoisture01`,
  `CanHostNaturalVegetation`, `CanHostPhysicalPlant`, `BlocksMovement`,
  `BlocksVision` quando pertinenti.

---

### v0.53 - Biosphere Runtime Config & Scheduling

**Stato:** ✅ Completata v0.53.1 - config e scheduler data-only

Obiettivo:

```text
spostare cadenze, budget e peso runtime della biosfera in file di configurazione
```

Blocchi:

| Blocco | Funzione | Stato |
|---|---|---|
| Config schema | sezione runtime biosfera in config produttiva | ✅ Completato v0.53.1 |
| Daily update cadence | update ordinario una volta al giorno simulato | ✅ Completato v0.53.1 |
| Tick bridge | mapping configurabile data-only tra `SimulationHost` tick e update biosfera | ✅ Completato v0.53.1 |
| Debug acceleration | flag protetto per futuri test giorni/mesi simulati rapidi | ✅ Completato v0.53.1 |
| Runtime budget | limiti mutazioni piante/vegetazione per update | ✅ Completato v0.53.1 |

---

### v0.54 - Core Cell Surface Layer

**Stato:** ✅ Completata da base `fec5435`

Nota:

```text
questo step non va reimplementato nella biosfera: e' gia' stato prodotto
nel lavoro ArcGraph/Core v0.38m.01-v0.38m.14
```

---

### v0.55 - Biological Area Landmark Definition

**Stato:** ✅ Implementazione Core iniziale completata - pending harness dedicato

Obiettivo:

```text
definire anchor landmark biologici a partire da area circolare, centro e raggio
e comunicarli al sistema landmark come LM manuali stabili dentro la rebuild
```

Blocchi previsti:

| Blocco | Funzione | Stato |
|---|---|---|
| Biological area center/radius | centro, raggio e containment circolare nell'area biologica | ✅ Implementato |
| Perimeter anchor selection | selezione anchor su celle naturali libere del perimetro biologico | ✅ Implementato |
| Landmark proposal DTO | payload Core leggero con posizione, kind enum, merge radius e owner area | ✅ Implementato |
| LM system handoff | input manuale alla rebuild del LandmarkRegistry senza appesantire LandmarkNode | ✅ Implementato |
| Area-to-LM reference | sidecar biosfera area biologica -> nodeIds risolti dal registry | ✅ Implementato |
| Debug overlay kind/color | colore e tipo debug dedicato per LM biologici in MapGrid/ArcGraph debug | ✅ Implementato |
| Harness | test senza MapGrid e senza ArcGraph | ⏳ Pending |

Vincoli:

- il landmark biologico non viene cercato tra LM esistenti: la biosfera produce candidati manuali e il registry li integra durante la rebuild;
- il centro area viene definito prima di enumerare le celle candidate;
- il raggio operativo dell'area biologica delimita le celle naturali libere interrogate tramite `World.CellSurfaces`;
- i LM biologici restano nodi leggeri: il riferimento area -> nodeIds vive nello stato biosfera, non dentro `LandmarkNode`;
- piu' anchor sul perimetro riducono il rischio che una foresta venga scoperta solo arrivando al centro occluso.

---

### v0.56 - Biosphere Surface Candidate Query

**Stato:** ✅ Core iniziale completato - pending harness dedicato

Obiettivo:

```text
usare centro/raggio dell'area biologica per chiedere al World quali celle
sono candidate per landmark biologici, vegetazione naturale e piante fisiche
```

Blocchi previsti:

| Blocco | Funzione | Stato |
|---|---|---|
| Surface read facade | lettura read-only di `World.CellSurfaces` e `World.SurfaceDefs` | ✅ Implementato |
| Radius cell scan | enumerazione celle entro raggio dell'area biologica | ✅ Implementato |
| Natural vegetation candidates | celle con superficie naturale e `CanHostNaturalVegetation=true` | ✅ Implementato |
| Biological occupancy output | vegetazione decorativa e piante fisiche come stato biosfera data-only | ✅ Implementato |
| Map biological areas | aree biologiche minime in `world_map_default.json` | ✅ Implementato |
| Sprite independence | nessun path sprite o PNG dentro biosfera | ✅ Implementato |
| Harness | test senza MapGrid e senza ArcGraph | ⏳ Pending |

---

### v0.57-v0.69 - Physical Biosphere Integration

| Versione | Obiettivo | Stato |
|---|---|---|
| v0.57 | definire boundary Biosfera -> World per mutazioni fisiche | ✅ Core boundary iniziale completato, delta/save-load pending |
| v0.58 | piazzare piante fisiche su celle candidate | ✅ Core iniziale completato, tuning placement pending |
| v0.59 | salvare rappresentazione runtime minima delle piante fisiche | ✅ Core runtime representation iniziale completata |
| v0.60 | propagare nascita/morte/cambio stadio pianta come delta | ✅ Producer delta giornalieri data-only completato |
| v0.61 | piazzare vegetazione decorativa non oggetto | ✅ Contratto/delta vegetazione diffusa data-only completato, World projection pending |
| v0.62 | sporcare FOV/percezione quando cambiano celle fisiche | ⏳ Pending |
| v0.63 | produrre feed runtime derivato per ArcGraph | ⏳ Pending |
| v0.64 | esporre eventi/listener ambiente per UI e sistemi | ⏳ Pending |
| v0.65 | integrare save/load biosfera runtime | ⏳ Pending |
| v0.66 | integrare query NPC verso aree/risorse biologiche | ⏳ Pending |
| v0.67 | pannello debug/calibrazione runtime biosfera | ⏳ Pending |
| v0.68 | budget e batch processing produttivo | ⏳ Pending |
| v0.69 | QA closeout integrazione fisica biosfera | ⏳ Pending |

Nota boundary piante fisiche:

```text
World riceve solo stato semantico compatto:
PlantId, AreaId, Cell, SpeciesKey, GrowthStageKey, HealthState, IsAlive,
BlocksMovement, BlocksVision, VisionCost.

Non passano nel boundary World:
AgeDays, Maturity01, Health01, IsHarvestable, plantVisualStateKey,
path sprite, tile id, atlas id o riferimenti PNG.
```

---

## Roadmap v0.70 - ArcGraph Runtime UI Architecture

La versione `v0.70` introduce la nuova architettura UI runtime di ArcGraph.

Questa fase non deve trasformare ArcGraph in un pannello debug monolitico e non
deve copiare `MapGridRuntimeDevToolsOverlay`. L'obiettivo e' costruire una UI
runtime modulare, basata su UGUI, prefab riutilizzabili, ViewModel/snapshot e
controller autorizzati.

Principio architetturale:

```text
Simulation / World
-> Snapshot / ViewModel
-> UI
-> Controller autorizzato
-> Command Gateway
-> Simulation / World
```

Divieti permanenti della fase `v0.70`:

- la UI non legge direttamente `World`;
- la UI non modifica direttamente NPC, oggetti, muri, celle o job runtime;
- `Button.onClick` non deve contenere mutazioni del mondo;
- i pannelli runtime non devono usare scorciatoie debug come comportamento produttivo;
- MapGrid resta legacy e non deve essere copiato come nuovo monolite;
- il vecchio F3 resta disponibile finche' il nuovo ponte non e' pronto, ma viene migrato una funzione alla volta.

Layout target:

```text
ArcUIRoot
├─ TopBar
├─ MapViewport
├─ RightInspector
├─ BottomActionBar
├─ ActionPanel
├─ OverlayRoot
└─ DebugRoot
```

Regole layout:

- `TopBar` e `BottomActionBar` sono larghe tutto lo schermo;
- `MapViewport` sta tra TopBar e BottomActionBar;
- `RightInspector` e `ActionPanel` sono overlay sopra la viewport;
- i pannelli non ridimensionano la simulazione salvo decisione futura esplicita;
- `ViewModeBar` e `PointerHUD` non diventano root permanenti separati in questa fase.

Componenti UI riutilizzabili da prevedere:

| Componente | Scopo |
|---|---|
| `ArcButton` | base comune per pulsanti runtime |
| `ArcButton_Primary` | azione positiva o principale |
| `ArcButton_Secondary` | azione ordinaria secondaria |
| `ArcButton_Danger` | azione distruttiva o urgente |
| `ArcButton_Icon` | comando compatto con icona |
| `ArcButton_Tab` | tab selezionabile |
| `ArcPanelFrame` | cornice comune dei pannelli |
| `ArcActionPanel` | contenitore operazioni |
| `ArcOperationGrid` | griglia operazioni disponibili |
| `ArcOperationButton` | bottone legato a una operation definition |
| `ArcInfoRow` | riga etichetta/valore |
| `ArcDataTable` | tabella dati read-only |
| `ArcTooltip` | tooltip runtime |
| `ArcStatusBadge` | badge stato sintetico |
| `ArcProgressBar` | barra progresso compatta |
| `ArcToggle` | toggle binario |
| `ArcSlider` | valore numerico continuo |
| `ArcDropdown` | scelta da insieme finito |
| `ArcRightInspector` | contenitore inspector a tab |
| `ArcTopBar` | barra stato e simulazione |
| `ArcBottomActionBar` | barra categorie/azioni |
| `ArcPlacementConfigPanel` | pannello configurazione placement |

Roadmap operativa:

| Versione | Nome | Stato |
|---|---|---|
| v0.70.01 | Foundation dati UI | Fatto su branch `ai-task/v0.70.01-arcgraph-ui-foundation-data` |
| v0.70.02 | Operation catalog minimo e gerarchia ActionPanel | Fatto su branch `ai-task/v0.70.02-arcgraph-ui-operation-catalog` |
| v0.70.03 | Ponte placement | ⏳ Pending |
| v0.70.04 | Selezione e hover | In corso - sottostep v0.70.04.04 completato |
| v0.70.05 | Inspector ViewModel/tab | In corso - sottostep v0.70.05.06 completato |
| v0.70.06 | Simulation control | In corso - sottostep v0.70.06.02 stabilizzato dopo v0.70.06.04 |
| v0.70.07 | Visual overlay toggles | Fatto - LM/LOS/PATH collegati, stabilizzati e separati semanticamente |
| v0.70.08 | Migrazione progressiva F3 | In corso - audit MapGrid/F3 legacy completato, serve bootstrap ArcGraph autonomo prima del delete fisico |

---

### v0.70.01 - Foundation dati UI

Obiettivo:

```text
creare i contratti minimi della UI runtime ArcGraph senza cambiare il comportamento
runtime e senza collegare ancora pulsanti produttivi
```

Scopo:

- introdurre tipi dati piccoli, serializzabili o facilmente ispezionabili;
- definire il vocabolario condiviso tra UI, controller e futuri adapter;
- evitare accessi diretti a `World`;
- preparare controller shell senza logica invasiva;
- lasciare F3 legacy e runtime corrente invariati.

Contratti minimi previsti:

| Contratto | Responsabilita' | Note |
|---|---|---|
| `ArcOperationDefinition` | descrive una operazione UI disponibile | Non esegue comandi |
| `ArcSelectionTarget` | descrive cosa e' sotto puntatore o selezionato | Non contiene riferimenti mutabili |
| `ArcInspectorViewModel` | dati read-only per inspector | UI visualizza soltanto |
| `ArcPlacementRequest` | intenzione di placement configurata | Non e' ancora comando Core |
| `ArcViewModeDefinition` | descrive un modo di visualizzazione | Non modifica il mondo |

Dettaglio `ArcOperationDefinition`:

- `operationKey`: chiave stabile, esempio `build_wall_stone`;
- `label`: testo UI;
- `iconKey`: chiave icona, non riferimento diretto a sprite;
- `category`: categoria primaria, esempio `Costruisci`, `Inserisci`, `NPC`;
- `subcategory`: categoria secondaria, esempio `Strutture`, `Oggetti`, `Umani`;
- `targetKind`: tipo di target operativo;
- `requiresPreview`: indica se serve preview su cella;
- `requiresConfigurationPanel`: indica se serve pannello di configurazione;
- `controllerKey`: controller autorizzato responsabile;
- `commandRequestType`: tipo di richiesta futura;
- `enabledInRuntime`: visibile/usabile in runtime ordinario;
- `debugOnly`: visibile solo in sviluppo/debug.

Dettaglio `ArcSelectionTarget`:

- `kind`: `Npc`, `Object`, `Wall`, `Cell`, `Plant`, `Zone`, `Debug`;
- `id`: identificatore stabile quando disponibile;
- `cellX`, `cellY`, `cellZ`: coordinate target;
- `displayName`: nome mostrabile;
- `sourceView`: origine autorizzata del target;
- `optionalDebugInfo`: testo diagnostico opzionale.

Dettaglio `ArcInspectorViewModel`:

- header principale;
- target selezionato;
- lista tab disponibili;
- tab attiva;
- righe read-only;
- sezioni read-only;
- flag debug/development quando serve.

Dettaglio `ArcPlacementRequest`:

- operation key;
- cella target;
- target definition id opzionale;
- payload configurazione opzionale;
- stato preview richiesto;
- nessun riferimento diretto a `World`;
- nessuna mutazione diretta.

Dettaglio `ArcViewModeDefinition`:

- `viewModeKey`;
- `label`;
- `iconKey`;
- `isDebugOnly`;
- `requiresSelectedNpc`;
- `overlayType`;
- `enabled`.

Controller shell previsti:

| Controller | Ruolo iniziale |
|---|---|
| `ArcSelectionController` | accetta/espone selection target senza interrogare World |
| `ArcPlacementController` | conserva intenzione placement corrente senza eseguire comandi |
| `ArcInspectionController` | riceve target e richiede ViewModel futuro |
| `ArcSimulationControlController` | shell per pause/play/speed future |
| `ArcViewModeController` | conserva view mode corrente |
| `ArcCommandRequestFactory` | placeholder per costruzione richieste autorizzate future |

Output atteso:

- file C# piccoli e isolati sotto area ArcGraph Runtime/UI o area equivalente approvata;
- nessun prefab nuovo obbligatorio in questo step;
- nessun cambio a scena o runtime installer salvo necessita' minima;
- nessun accesso diretto a `SimulationHost.Instance.World` dai nuovi tipi UI;
- nessun comando inviato al gateway;
- nessuna sostituzione del vecchio F3.

Criteri di accettazione:

- il progetto compila;
- i contratti sono leggibili e commentati;
- non ci sono mutazioni runtime;
- MapGrid legacy non viene toccato;
- il vecchio F3 continua a essere il comportamento operativo esistente;
- la foundation puo' essere usata dagli step successivi senza refactor grande.

---

### v0.70.02 - Operation catalog minimo e gerarchia ActionPanel

Obiettivo:

```text
definire il primo catalogo operazioni UI senza collegarlo ancora a tutte le azioni runtime
```

Decisione layout ActionPanel:

```text
BottomActionBar action button
-> macrogruppo sinistro del pannello
-> icona operazione concreta a destra
```

Esempio:

```text
Costruisci
-> Strutture
-> Muro di pietra / Porta di legno / Finestra
```

Principio:

- i pulsanti della bottom bar non sono comandi diretti;
- il macrogruppo sinistro filtra le operation;
- le icone operative devono rappresentare varianti concrete, non famiglie astratte;
- una operation come `place_bed_wood` e' distinta da un eventuale futuro `place_bed_metal`;
- `object_defs.json` resta catalogo oggetti, mentre il catalogo UI descrive cosa la UI puo' proporre.

Operazioni minime:

| Operation key | Categoria | Target | Note |
|---|---|---|---|
| `build_wall_stone` | Costruisci / Strutture | Wall | Usa preview cella |
| `place_door_wood` | Costruisci / Strutture | Object/Door | Usa preview cella |
| `place_bed_wood` | Inserisci / Oggetto | Object | Da object catalog |
| `place_food_stock` | Inserisci / Oggetto | Object | Da object catalog |
| `spawn_npc_human` | NPC | Npc | Richiede configurazione futura |
| `edit_selected_npc` | NPC / Selezione | Npc | Apre configurazione parametri quando il target lo consente |
| `delete_selected_npc` | NPC / Selezione | Npc | Richiesta eliminazione su target selezionato |
| `edit_selected_object` | Oggetti / Selezione | Object | Attiva solo per oggetti con parametri modificabili |
| `delete_selected_object` | Oggetti / Selezione | Object | Richiesta eliminazione su target selezionato |
| `edit_selected_wall` | Costruisci / Strutture | Wall | Modifica futura di parametri o sostituzione controllata |
| `delete_selected_wall` | Costruisci / Strutture | Wall | Richiesta eliminazione su target selezionato |

Tipi di operation previsti:

| Tipo | Uso |
|---|---|
| `Insert` | inserire muro, porta, oggetto, NPC |
| `Edit` | modificare target gia' selezionato tramite pannello autorizzato |
| `Delete` | eliminare target gia' selezionato |
| `SetState` | scegliere uno stato discreto, ad esempio velocita' 2x/3x/4x nello step Simulation Control |
| `ToggleView` | accendere/spegnere visualizzazioni, ad esempio landmark o linee di vista |
| `ToolMode` | cambiare modo strumento, ad esempio single/brush |

Decisione su single/brush:

- `single` e `brush` non devono generare due operation diverse;
- sono una modalita' dello strumento di placement;
- la stessa operation, ad esempio `build_wall_stone`, puo' essere usata con click singolo o brush se `SupportsBrush` lo consente;
- il bridge placement dovra' bloccare brush per operation che non lo supportano.

Decisione su configurazione e modificabilita':

- alcune operation richiedono una pagina di conferma/configurazione nel RightInspector o in un pannello configurazione dedicato;
- NPC: la configurazione iniziale e la modifica futura possono includere parametri DNA e altri valori autorizzati;
- food stock: puo' richiedere un parametro quantita', ad esempio 1-10;
- altri oggetti, come una sedia semplice, possono non essere modificabili;
- la presenza della operation `Edit` non significa che ogni istanza sia modificabile: la disponibilita' reale verra' decisa da ViewModel/controller autorizzati.

Attivita':

- creare catalogo statico o loader minimo;
- separare operation catalog da `object_defs.json`;
- mantenere `object_defs.json` come catalogo oggetti, non come catalogo azioni UI;
- aggiungere validazioni leggere su chiavi duplicate;
- esporre elenco filtrabile per categoria/subcategoria.
- introdurre il vocabolario `ActionKey`, `GroupKey`, `OperationKey`;
- introdurre flag `DebugOnly` su action, gruppi e operation;
- predisporre, senza collegarla, la distinzione tra inserimento, modifica, eliminazione, stato, visualizzazione e modo strumento.

Criteri di accettazione:

- le operation esistono come dati;
- nessuna operation modifica il mondo;
- il catalogo puo' alimentare una futura griglia UI;
- debug-only e runtime-enabled sono distinguibili.
- il catalogo non legge cataloghi Core e non conosce `World`;
- i dati possono rappresentare varianti concrete come muro di pietra, porta di legno, letto di legno.

---

### v0.70.03 - Ponte placement

Obiettivo:

```text
tradurre una operation selezionata in una PlacementRequest e poi, solo tramite ponte autorizzato, nel comando esistente
```

Flussi iniziali:

```text
OperationDefinition
-> ArcPlacementController
-> ArcPlacementRequest
-> ArcCommandRequestFactory
-> comando autorizzato temporaneo
-> SimulationHost / gateway esistente
```

Attivita':

- collegare muri, porte e oggetti al placement controller;
- mantenere preview cella esistente;
- gestire modalita' `single` e `brush` come stato strumento;
- impedire brush quando la operation selezionata non lo supporta;
- aprire o preparare il pannello configurazione prima del click finale quando `RequiresConfiguration` e' attivo;
- non introdurre accesso diretto a `World` dalla UI;
- usare temporaneamente `DevPlaceObjectCommand` solo come bridge dichiarato;
- isolare la conversione in un punto solo.

Criteri di accettazione:

- una operation puo' diventare request;
- il click cella produce una richiesta chiara;
- il comando finale passa da boundary autorizzato;
- il vecchio F3 resta disponibile come fallback.

---

### v0.70.04 - Selezione e hover

Obiettivo:

```text
introdurre SelectionTarget come contratto unico per hover, click e selezione singola
```

Sottostep operativi:

| Versione | Nome | Stato |
|---|---|---|
| v0.70.04.01 | Hover cella sotto mouse | Fatto su branch `ai-task/v0.70.04.01-arcgraph-pointer-cell-hover` |
| v0.70.04.02 | Selezione NPC/Oggetto | Fatto su branch `ai-task/v0.70.04.02-arcgraph-ui-selection` |
| v0.70.04.03 | Hover menu Modifica/Elimina | Fatto su branch `ai-task/v0.70.04.03-arcgraph-selection-action-menu` |
| v0.70.04.04 | Selection Action Bridge | Fatto su branch `ai-task/v0.70.04.04-selection-action-bridge` |

Target previsti:

- NPC;
- oggetti;
- muri;
- celle;
- piante future;
- zone future;
- elementi debug futuri.

Attivita':

- mappare il risultato di hit test ArcGraph in `ArcSelectionTarget`;
- mantenere un hover cella sempre disponibile sotto il puntatore;
- disegnare highlight cella con una illuminazione visiva leggera, ad esempio brightening circa 20%;
- introdurre stato selezione singola;
- separare hover da selezione confermata;
- predisporre una hover label sopra NPC/oggetto/muro selezionabile con informazioni sintetiche;
- predisporre azioni rapide edit/delete nella hover label, ma sempre tramite controller autorizzati;
- non far leggere l'inspector direttamente al mondo;
- mantenere compatibilita' temporanea con `NPCSelection`.

Criteri di accettazione:

- hover produce target candidato;
- click produce target selezionato;
- selezione singola chiara;
- RightInspector puo' ricevere un target astratto.
- i pulsanti rapidi Modifica/Elimina producono una richiesta UI pending non distruttiva.

---

### v0.70.05 - Inspector ViewModel/tab

Obiettivo:

```text
costruire RightInspector come contenitore a tab alimentato da ViewModel read-only
```

Sottostep operativi previsti:

| Versione | Nome | Stato |
|---|---|---|
| v0.70.05.01 | RightInspector shell e consumer richiesta selezione | Fatto su branch `ai-task/v0.70.05.01-right-inspector-shell` |
| v0.70.05.02 | Inspector ViewModelFactory minima per NPC/Object/Wall/Cell | Fatto su branch `ai-task/v0.70.05.02-inspector-viewmodel-factory` |
| v0.70.05.03 | Tab read-only iniziali | Fatto su branch `ai-task/v0.70.05.03-inspector-readonly-tabs` |
| v0.70.05.04 | Inspector informazioni read-only | In corso |
| v0.70.05.04.01 | NPC read-only inspector con slot portrait | Fatto su branch `ai-task/v0.70.05.04.01-npc-readonly-inspector` |
| v0.70.05.04.01b | NPC inspector layout ricco: Info/Stato/Inventario unificati, barre, metriche e righe espandibili | Fatto su branch `ai-task/v0.70.05.04.01b-npc-inspector-layout` |
| v0.70.05.04.01c | Fix runtime inspector scroll vuoto e clamp hover menu dentro MapViewport | Fatto su branch `ai-task/v0.70.05.04.01c-inspector-hover-runtime-fix` |
| v0.70.05.04.01d | Pannello diagnostico provvisorio RightInspector su DebugRoot | Fatto su branch `ai-task/v0.70.05.04.01d-inspector-debug-panel` |
| v0.70.05.04.01e | Fix rendering testo pannello diagnostico RightInspector | Fatto su branch `ai-task/v0.70.05.04.01e-inspector-debug-text-fix` |
| v0.70.05.04.01f | Fix altezza layout RightInspector: usa preferredHeight dei blocchi e delle righe | Fatto su branch `ai-task/v0.70.05.04.01f-inspector-layout-height-fix` |
| v0.70.05.04.01g | Cleanup debug panel, compattezza dati inspector, scrollbar visibile e layer hover sotto pannelli UI | Fatto su branch `ai-task/v0.70.05.04.01g-inspector-ui-cleanup-compact-scroll` |
| v0.70.05.04.01h | Compattezza dense per tutti i gruppi espandibili del RightInspector | Fatto su branch `ai-task/v0.70.05.04.01h-inspector-expanded-compact` |
| v0.70.05.04.01i | Fix spaziatura reale dei dettagli espandibili e leggibilita' del toggle +/- | Fatto su branch `ai-task/v0.70.05.04.01i-inspector-expanded-spacing-fix` |
| v0.70.05.04.01j | Tuning leggibilita' testi RightInspector con font e altezze leggermente maggiori | Fatto su branch `ai-task/v0.70.05.04.01j-inspector-text-readability` |
| v0.70.05.04.01k | Font provider IBM Plex: Medium per testo UI e SemiBold reale per testi marcati Bold | Fatto su branch `ai-task/v0.70.05.04.01k-ibmplex-font-provider` |
| v0.70.05.04.01l | Asset font/png considerati parte progetto per UI runtime locale | Fatto su branch `ai-task/v0.70.05.04.02-object-wall-inspector` |
| v0.70.05.04.02 | Oggetti, muri e porte read-only inspector | Fatto su branch `ai-task/v0.70.05.04.02-object-wall-inspector` |
| v0.70.05.04.02b | Fix header NPC RightInspector: info visibili accanto al portrait e portrait 2:3 | Fatto su branch `ai-task/v0.70.05.04.02b-npc-header-layout-fix` |
| v0.70.05.05 | Edit shell da SelectionActionRequest | Fatto |
| v0.70.05.05.01 | NPC edit shell nel RightInspector: DNA e inventario preparati senza scrittura World | Fatto su branch `ai-task/v0.70.05.05.01-npc-edit-shell` |
| v0.70.05.05.02 | Object/Wall/Door edit shell nel RightInspector: parametri, storage, porta e materiale senza scrittura World | Fatto su branch `ai-task/v0.70.05.05.02-object-edit-shell` |
| v0.70.05.06 | Delete confirmation shell non distruttiva | Fatto su branch `ai-task/v0.70.05.06-delete-confirm-shell` |

Tab iniziali:

| Target | Tab |
|---|---|
| NPC | Info, Job, Memory Trace, Belief, Decision, Debug |
| Object | Info, Stato, Uso, Storage, Debug |
| Wall | Info, Materiale, Connessioni, Stato, Debug |
| Cell | Info superficie, Coordinate, Occupazione, Debug FOV/percezione |

Configurazione e modifica parametri:

- il RightInspector deve poter mostrare pagine di conferma/configurazione per operation di inserimento e modifica;
- NPC: campi autorizzati per DNA e altri valori esposti da snapshot/ViewModel, non scrittura diretta su NPC runtime;
- oggetti: solo alcuni oggetti avranno campi modificabili, ad esempio quantita' food stock;
- muri e porte: eventuali modifiche devono passare da varianti o parametri autorizzati, non da sostituzioni dirette del World;
- un target non modificabile deve esporre chiaramente `canEdit = false` nel ViewModel futuro;
- le azioni rapide della hover label possono aprire edit/delete, ma non devono eseguire mutazioni dirette.

Attivita':

- creare `InspectorViewModelFactory`;
- creare primi ViewModel read-only;
- introdurre tab descriptors;
- usare `ArcInfoRow` e `ArcDataTable`, non righe hardcoded nel pannello;
- nascondere tab debug fuori dallo sviluppo.

Criteri di accettazione:

- inspector cambia contenuto in base a target;
- nessun accesso diretto a strutture runtime mutabili;
- dati mancanti vengono mostrati come assenti, non generano scorciatoie nel World.

---

### v0.70.06 - Simulation control

Obiettivo:

```text
spostare pausa, play e velocita' simulazione dentro un controller autorizzato
```

TopBar deve mostrare:

- giorno;
- mese;
- anno;
- stagione;
- ora;
- temperatura;
- umidita';
- meteo;
- play / pausa;
- velocita' simulazione.

Richieste previste:

- `PauseSimulationRequest`;
- `ResumeSimulationRequest`;
- `SetSimulationSpeedRequest`.

Attivita':

- creare shell request/control;
- sostituire accessi diretti UI -> `SimulationHost.TogglePause()` dove possibile;
- mantenere compatibilita' con runtime attuale;
- distinguere step debug da controlli produttivi.

Sottostep:

| Step | Contenuto | Stato |
|------|-----------|-------|
| v0.70.06.01 | SimulationControl request/state + controller autorizzato + wiring TopBar pausa/play/speed UI | Fatto su branch `ai-task/v0.70.06.01-simulation-control-foundation` |
| v0.70.06.02 | Snapshot tempo/ambiente TopBar: giorno, mese, anno, stagione, ora, temperatura, umidita', meteo | Fatto su branch `ai-task/v0.70.06.02-topbar-environment-snapshot` |
| v0.70.06.03a | Accelerazioni normali x1-x4 operative nel `SimulationHost` con cap anti-freeze per frame Unity | Fatto su branch `ai-task/v0.70.06.03a-normal-speed-cap` |
| v0.70.06.03b | Modalita' debug Biosfera-only x50/x100/x200 con freeze sociale, calendario ambiente attivo e placeholder refresh conservativi | Fatto su branch `ai-task/v0.70.06.03b-biosphere-debug-fast-forward` |
| v0.70.06.04 | Rifinitura visuale TopBar: stati attivi/disabilitati, separazione Bio debug e blocco input temporale diretto durante fast-forward | Fatto su branch `ai-task/v0.70.06.04-topbar-visual-states` |

Nota di integrazione Biosfera:

- `v0.70.06.02` stabilizza il contratto `ArcUiEnvironmentStatusSnapshot`;
- la TopBar continua a leggere label gia' pronte, ma il contratto espone anche valori numerici di calendario, ora, temperatura, umidita' e meteo;
- la UI non legge `World` o `EnvironmentState` direttamente: il controller autorizzato costruisce lo snapshot da `SimulationHost`;
- la chat Biosfera puo' usare questo branch come base UI per merge e per sviluppare la futura visualizzazione ambiente senza duplicare un secondo contratto tempo/clima.

Criteri di accettazione:

- TopBar non muta direttamente la simulazione;
- il controller e' l'unico punto UI autorizzato;
- stato temporale mostrato via ViewModel/snapshot.

---

### v0.70.07 - Visual overlay toggles

Obiettivo:

```text
separare overlay visuali indipendenti dalle azioni operative sul mondo
```

Decisione architetturale:

- la visuale normale non e' una modalita' selezionabile: resta sempre attiva;
- landmark, LOS, pathfinding e overlay debug sono toggle on/off indipendenti;
- piu' overlay possono essere attivi contemporaneamente;
- questi toggle non modificano il mondo e non producono comandi simulativi;
- modifica/elimina NPC, oggetti, muri e porte restano richieste operative separate su target selezionato.

Overlay previsti:

- landmark on/off;
- LOS NPC on/off;
- pathfinding on/off;
- percezione debug on/off;
- belief overlay on/off;
- memory overlay on/off;
- biosphere overlay futuro on/off;
- resources overlay futuro on/off.

Attivita':

- introdurre catalogo `ArcUiVisualOverlayDefinition`;
- introdurre `ArcUiVisualOverlayController`;
- introdurre `ArcUiVisualOverlayState`;
- introdurre request UI `Toggle`, `SetEnabled`, `ClearAll`;
- collegare in futuro le icone visuali della TopBar;
- collegare in futuro `OverlayRoot`;
- richiedere NPC selezionato quando serve;
- separare debug-only da runtime ordinario.

Sottostep:

| Step | Contenuto | Stato |
|------|-----------|-------|
| v0.70.07.01 | Foundation toggle overlay visuali: catalogo, request, state e controller UI non operativo | Fatto su branch `ai-task/v0.70.07.01-visual-overlay-toggle-foundation` |
| v0.70.07.02 | Collegamento icone visuali TopBar senza root separata permanente | Fatto su branch `ai-task/v0.70.07.02-topbar-overlay-toggle-icons` |
| v0.70.07.03 | LOS NPC on/off con vincolo target NPC selezionato | Fatto su branch `ai-task/v0.70.07.03-los-overlay-toggle-runtime` |
| v0.70.07.04 | Landmark on/off tramite overlay esistente/adattato | Fatto su branch `ai-task/v0.70.07.04-landmark-overlay-toggle-runtime` |
| v0.70.07.05 | Pathfinding/debug overlay on/off | Fatto su branch `ai-task/v0.70.07.05-pathfinding-overlay-toggle-runtime` |
| v0.70.07.06 | Stabilizzazione overlay: LM/PATH throttled, facing NPC da snapshot, TopBar compatta | Fatto su branch `ai-task/v0.70.07.06-overlay-facing-layout-stabilization` |
| v0.70.07.08 | Split semantico overlay: LM mostra solo landmark globali con label/colori per tipo, PATH resta sul percorso dell'NPC selezionato | Fatto su branch `ai-task/v0.70.07.08-overlay-semantic-split` |

Criteri di accettazione:

- i toggle overlay sono componibili, non esclusivi;
- la UI non legge direttamente il mondo;
- il controller conserva solo stato UI;
- nessun renderer viene acceso automaticamente in foundation;
- azioni operative su selezione restano fuori da questo layer.

---

### v0.70.08 - Migrazione progressiva F3

Obiettivo:

```text
assorbire le funzioni utili del vecchio F3 dentro la nuova architettura senza copiare il monolite MapGrid
```

Strategia:

- mantenere F3 legacy finche' il nuovo flusso non copre la funzione equivalente;
- migrare una funzione alla volta;
- ogni funzione migrata deve passare da operation/request/controller;
- cancellare o spegnere il legacy solo dopo test manuale e verifica dipendenze.

Ordine consigliato:

1. placement muro;
2. placement porta;
3. placement oggetti base;
4. spawn NPC default;
5. spawn NPC configurato;
6. erase/debug solo se autorizzato;
7. strumenti save/load solo come debug separato;
8. chiusura F3 legacy.

Sottostep operativi:

| Step | Contenuto | Stato |
|------|-----------|-------|
| v0.70.08.01 | Audit F3 legacy: funzioni, comandi DevTools, cataloghi e boundary SimulationHost | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.02 | ActionPanel Inserisci: gruppi STRUTTURE/OGGETTI/NPC, lettura `object_defs.json`, preview passiva ArcGraph | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.03 | PlacementRequest bridge: operation selezionata + parametri UI -> richiesta placement verificabile, ancora senza cancellare F3 | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.04 | Command bridge temporaneo: richiesta placement -> comando esistente autorizzato per muri, porte e oggetti | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.05 | NPC spawn shell: preview/config iniziale NPC e richiesta spawn configurata | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.06 | NPC spawn command bridge: richiesta NPC -> comando autorizzato temporaneo, senza DNA reale | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.07 | Delete bridge: eliminazione selezione tramite richiesta UI e comando autorizzato | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.08 | Edit bridge: draft modifica per NPC/oggetti/muri senza scrittura diretta World | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.09 | Gate spegnimento F3: disconnettere ArcGraph runtime da F3 legacy e dal fallback preview MapGrid | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.10 | Rimozione fisica controllata dei residui F3 ArcGraph non piu' referenziati | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.11 | Audit cancellazione MapGrid/F3 legacy: dipendenze residue prima del delete fisico completo | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.12 | Bootstrap ArcGraph autonomo: togliere avvio obbligato da `Scene_MapGrid` | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.13 | Creazione/configurazione scena ArcGraph autonoma o switch bootstrap effettivo | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.14 | Gate runtime Scene_ArcGraph e delete fisico MapGrid legacy | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |
| v0.70.08.15 | Validazione Unity Editor post-delete e pulizia riferimenti testuali residui | Fatto su branch `ai-task/v0.70.08-f3-progressive-migration` |

Esito `v0.70.08.04`:

- aggiunto bridge runtime `ArcGraphUiPlacementCommandBridge`;
- il bridge lavora solo quando esiste una `ArcUiPlacementRequest` valida prodotta dall'ActionPanel e una preview UI attiva;
- il bridge ignora la preview fallback F3, cosi' non confonde il nuovo flusso con il tool legacy;
- click singolo e brush vengono trasformati in `DevPlaceObjectCommand` accodato a `SimulationHost.Instance.EnqueueExternalCommand`;
- supportati in questo step muri, porte, oggetti generici e `food_stock` con quantita' e owner community;
- la validazione finale resta dentro il comando Core e nel `World`, non nella UI;
- chiudere il pannello Inserisci cancella preview e request pending, evitando placement stale.

Prossimo step `v0.70.08.05`:

- preparare una shell NPC separata dal placement oggetti;
- non spawnare ancora NPC direttamente dal pannello;
- introdurre richiesta UI minima per spawn NPC con cella target e configurazione placeholder;
- predisporre preview NPC semitrasparente coerente con la preview oggetti;
- rimandare DNA reale e owner/inventario a pannello configurazione/RightInspector dedicato.

Esito `v0.70.08.05`:

- aggiunto contratto `ArcUiNpcSpawnRequest` con config minima visuale/facing;
- aggiunto `ArcUiNpcSpawnController` come stato pending non distruttivo;
- il gruppo `NPC` del pannello Inserisci attiva una preview semitrasparente del futuro umano;
- la preview usa `ArcGraphNpcVisualCatalog.json` e `ArcGraphSerializedSpriteResolver`, quindi resta nel sistema visuale ArcGraph e non copia MapGrid;
- il click sulla mappa registra solo la cella target nella request pending;
- nessun comando spawn viene ancora accodato;
- nessun accesso diretto al `World` dalla UI.

Prossimo step `v0.70.08.06`:

- introdurre un bridge comando NPC separato;
- leggere solo `ArcUiNpcSpawnRequest` valida e completa di cella;
- mappare il facing UI verso il facing Core;
- accodare temporaneamente `DevSpawnNpcCommand`;
- mantenere DNA reale e pannello configurazione avanzato fuori da questo step.

Esito `v0.70.08.06`:

- il bridge NPC ora completa la request con la cella cliccata e accoda `DevSpawnNpcCommand`;
- il facing UI viene convertito in `CardinalDirection` Core nel bridge, senza esporre tipi Core al contratto UI;
- la UI non crea NPC direttamente e non legge il `World`;
- le guardie su cella bloccata e cella gia' occupata restano dentro il comando autorizzato;
- i pulsanti del pannello azione generati per categorie, operation e parametri sono stati uniformati a 24 px di altezza.

Prossimo step `v0.70.08.07`:

- collegare la richiesta `DeleteSelectionRequest` gia' prevista all'elemento selezionato;
- introdurre un bridge cancellazione separato per NPC/oggetto/muro;
- usare solo selezione corrente e comando autorizzato, senza accesso diretto UI -> `World`;
- mantenere conferma/cancellazione come flusso provvisorio verificabile prima dell'edit bridge.

Esito `v0.70.08.07`:

- aggiunto bridge `ArcGraphUiSelectionDeleteCommandBridge`;
- la richiesta Delete prodotta dal menu selezione viene consumata una sola volta;
- NPC selezionato -> `DevEraseNpcAtCellCommand`;
- oggetto o muro selezionato -> `DevEraseObjectCommand`;
- dopo invio comando il bridge pulisce request e selezione UI, cosi' il menu hover e il RightInspector si chiudono;
- la UI non legge il `World` e non distrugge entita' direttamente;
- corretta l'altezza effettiva dei chip parametrici del pannello azione, impedendo al contenitore di stirare pulsanti come `Click`, `Brush`, quantita', owner e facing oltre 24 px.

Prossimo step `v0.70.08.08`:

- usare la richiesta Edit del menu selezione come ingresso del flusso di modifica;
- aprire RightInspector in modalita' edit per NPC, oggetti e muri;
- mantenere modifiche reali fuori dal pannello finche' non esistono request/command autorizzati dedicati;
- iniziare da shell provvisoria verificabile, senza scrittura diretta su `World`.

Esito `v0.70.08.08`:

- aggiunto contratto `ArcUiEditSelectionRequest`;
- aggiunto controller `ArcUiEditSelectionController` per conservare una draft edit separata;
- aggiunto bridge `ArcGraphUiSelectionEditRequestBridge`;
- il tasto Modifica del menu selezione produce una draft edit per NPC, oggetto o muro;
- il RightInspector continua ad aprirsi in modalita' edit usando la richiesta UI gia' esistente;
- le tab edit indicano che la draft request e' attiva, ma nessuna modifica viene applicata;
- nessun accesso diretto UI -> `World`, nessun comando di modifica inviato.

Prossimo step `v0.70.08.09`:

- valutare quali funzioni F3 sono ormai coperte dal nuovo pannello ArcGraph;
- disattivare solo le funzioni migrate, evitando di cancellare fisicamente MapGrid;
- mantenere eventuali tool debug non migrati separati dalla UI produttiva;
- preparare un handoff pulito verso integrazione con Biosfera.

Esito `v0.70.08.09`:

- `ArcGraphRuntimeSceneAutoInstaller` non crea piu' `ArcGraphPlacementToolController`;
- ArcGraph non usa piu' `MapGridRuntimeDevToolsOverlay` come sorgente placement/preview;
- `ArcGraphUiPlacementPreviewSource` non possiede piu' fallback F3: preview e cella placement arrivano solo dal nuovo pannello azione;
- `MapGridRuntimeDevToolsOverlay` e MapGrid legacy non sono stati cancellati in questo step, ma non sono piu' riattivati dall'installer ArcGraph;
- il progetto compila con `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`.

Prossimo step `v0.70.08.10`:

- verificare con `rg` che `ArcGraphPlacementToolController` non abbia piu' chiamanti runtime;
- eliminare fisicamente il file `ArcGraphPlacementToolController.cs` se resta davvero non referenziato;
- non toccare ancora `MapGridRuntimeDevToolsOverlay`, `MapGridWorldView` o componenti MapGrid finche' non e' completato un audit separato delle dipendenze residue;
- preparare la lista dei residui MapGrid che bloccano la cancellazione fisica completa del vecchio sistema.

Esito `v0.70.08.10`:

- auditato `ArcGraphPlacementToolController`: nessun chiamante runtime ArcGraph residuo;
- eliminato fisicamente `Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphPlacementToolController.cs`;
- il comando placement resta disponibile attraverso `ArcGraphUiPlacementCommandBridge`, che usa `ArcUiPlacementRequest` prodotta dal nuovo pannello azione;
- non sono stati toccati `MapGridRuntimeDevToolsOverlay`, `MapGridWorldView`, `MapGridRuntimeControlTopBar`, scene, prefab o `.meta`.

Prossimo step `v0.70.08.11`:

- auditare tutte le dipendenze runtime residue verso MapGrid e F3 legacy;
- separare cio' che e' cancellabile subito da cio' che serve ancora come bootstrap, dati terrain, topbar legacy o debug;
- produrre una tabella file-per-file con decisione: eliminare, congelare, sostituire, rimandare;
- non cancellare ancora MapGrid in blocco prima della verifica runtime e della lista completa dei sostituti ArcGraph.

Esito `v0.70.08.11`:

| Area/file | Stato audit | Decisione |
|---|---|---|
| `Assets/Scenes/Scene_Bootstrap.unity` + `ViewSwitcherInputActions` | caricano ancora `Scene_MapGrid` all'avvio (`mapGridName = Scene_MapGrid`, `loadMapGridOnStart = true`) | sostituire prima del delete con bootstrap/scena ArcGraph autonoma |
| `Assets/Scenes/Scene_MapGrid.unity` | contiene `MapGridBootstrap`, `MapGridRuntimeDevToolsOverlay`, root e input provider MapGrid | non cancellabile finche' non esiste scena ArcGraph equivalente |
| `ArcGraphRuntimeSceneAutoInstaller` | si installa solo se la scena attiva si chiama `Scene_MapGrid`; cerca ancora `MapGridCameraController`, `MapGridWorldView` e root visuali legacy per spegnerli | modificare nello step successivo: supporto ad avvio ArcGraph autonomo e riduzione bridge legacy |
| `ArcGraphRuntimeWorldAdapter` | legge `SimulationHost -> World`, non `MapGridData`, `MapGridBootstrap` o `MapGridWorldView` | mantenere: e' il provider corretto per ArcGraph |
| `ArcGraphTerrainRuntimeMapGridAdapter` | adapter/probe legacy che legge `MapGridBootstrap.RuntimeMap/RuntimeConfig` | congelare come legacy/probe o eliminare dopo bootstrap autonomo |
| `ArcGraphDebugRuntimeMapGridAdapter` | bridge manuale debug da `MapGridWorldView.RuntimeWorld` | congelare/eliminare dopo che debug runtime ArcGraph usa solo provider neutri |
| `ArcGraphViewModeSwitcher` | conserva modalita' `MapGrid` e F12/switch visuale comparativo | rimuovere dopo che ArcGraph resta unica vista runtime |
| `MapGridWorldView` | crea `MapGridRuntimeDevToolsOverlay`, `MapGridRuntimeControlTopBar`, rendering NPC/oggetti, overlay, cache e rebind World legacy | non cancellabile singolarmente: va eliminato insieme alla scena MapGrid o dopo sostituti completi |
| `MapGridRuntimeDevToolsOverlay` | vero F3 legacy; usa `MapGridWorldProvider`, input/camera MapGrid, comandi DevTools e save/load debug | cancellabile solo dopo rimozione scena MapGrid o separazione debug residua |
| `MapGridRuntimeControlTopBar` | topbar legacy collegata a F3 e `MapGridWorldView` | cancellabile con MapGrid UI legacy, non prima |
| `Assets/Scripts/Views/MapGrid/**` | circa 60 file inclusi `.meta`, runtime, debug e input legacy | delete fisico finale in step dedicato, senza toccare `.meta` sporchi separatamente |
| `Assets/Resources/MapGrid/**` | config, layout e sprite legacy MapGrid | delete fisico finale dopo verifica che cataloghi ArcGraph non referenzino piu' path MapGrid |

Conclusione:

- non esiste piu' un blocco F3 ArcGraph da rimuovere;
- il vero blocco residuo e' l'avvio dentro `Scene_MapGrid`;
- cancellare subito MapGrid romperebbe bootstrap, scena e alcuni bridge transitori;
- la prossima patch non deve cancellare MapGrid: deve creare o abilitare un percorso ArcGraph autonomo.

Prossimo step `v0.70.08.12`:

- cambiare la logica di avvio per permettere una scena/entry ArcGraph autonoma;
- far installare `ArcGraphRuntimeSceneAutoInstaller` anche fuori da `Scene_MapGrid`, con criterio controllato;
- mantenere compatibilita' temporanea con `Scene_MapGrid`;
- non cancellare ancora `Scene_MapGrid`, `MapGridWorldView` o `MapGridRuntimeDevToolsOverlay`;
- dopo test runtime, iniziare la rimozione dello switch MapGrid/F12 e dei bridge legacy.

Criteri di accettazione:

- nessuna perdita di funzionalita' runtime utile;
- nessuna nuova dipendenza UI -> World;
- MapGrid non e' piu' necessario per le operazioni migrate;
- vecchi strumenti debug restano separati dalla UI produttiva.

Esito `v0.70.08.12`:

- `ViewSwitcherInputActions` espone ora una scelta di avvio controllata: `MapGrid`, `ArcGraph` o `CurrentScene`;
- il default resta `MapGrid`, quindi il comportamento runtime attuale non cambia finche' non viene configurato esplicitamente lo switch;
- `ArcGraphRuntimeSceneAutoInstaller` puo' installarsi sia in `Scene_MapGrid` sia nella futura `Scene_ArcGraph`;
- non sono state create o modificate scene, prefab, asset grafici o `.meta`;
- build C# verificata con 0 errori.

Prossimo step `v0.70.08.13`:

- decidere se creare una vera `Scene_ArcGraph` autonoma o configurare `Scene_Bootstrap` per avviare ArcGraph;
- collegare lo switch effettivo senza cancellare ancora `Scene_MapGrid`;
- verificare in Unity che ArcGraph parta senza il vecchio F3 e senza dipendere da MapGrid come scena operativa;
- solo dopo il test runtime, preparare il delete fisico dei componenti MapGrid/F3 rimasti.

Esito `v0.70.08.13`:

- la scena runtime principale e' stata rinominata dall'operatore in `Scene_ArcGraph`;
- `ViewSwitcherInputActions` carica ora `Scene_ArcGraph` come startup view e non possiede piu' un percorso di avvio MapGrid;
- l'azione input storica `SwitchToMap` viene mantenuta solo come binding esistente, ma ora porta ad ArcGraph;
- `ArcGraphRuntimeSceneAutoInstaller` si installa solo in `Scene_ArcGraph`, non piu' in `Scene_MapGrid`;
- `ProjectSettings/EditorBuildSettings.asset` punta al path `Assets/Scenes/Scene_ArcGraph.unity`;
- i componenti legacy MapGrid possono essere ancora presenti dentro la scena rinominata, ma non sono piu' il nome/entrypoint della vista runtime principale;
- build mirata `Assembly-CSharp` verificata con 0 errori; build completa bloccata da lock Unity su file in `Temp`, non da errore codice.

Prossimo step `v0.70.08.14`:

- validare in Play Mode che `Scene_Bootstrap` apra `Scene_ArcGraph`;
- verificare camera, input, UI, placement, selezione, inspector, overlay LM/LOS/PF;
- se il gate runtime passa, preparare la lista di delete fisico MapGrid/F3 legacy;
- cancellare fisicamente MapGrid solo in uno step dedicato e dopo verifica dei riferimenti residui.

Esito `v0.70.08.14`:

- cancellata fisicamente la cartella legacy `Assets/Scripts/Views/MapGrid`;
- cancellata fisicamente la cartella legacy `Assets/Resources/MapGrid`;
- cancellati i due adapter/probe ArcGraph ancora dipendenti da MapGrid:
  - `ArcGraphDebugRuntimeMapGridAdapter`;
  - `ArcGraphTerrainRuntimeMapGridAdapter`;
- `Scene_ArcGraph` e' stata ripulita dai root/componenti serializzati MapGrid:
  - `InputBridge_MapGrid`;
  - `MapGridRoot`;
  - `MapViewBridge`;
  - `MapGridRuntimeDevToolsOverlay`;
  - vecchio controller camera MapGrid sulla `MainCamera`;
- `Scene_Bootstrap` serializza ora `arcGraphSceneName: Scene_ArcGraph` e non piu' `mapGridName: Scene_MapGrid`;
- `ArcGraphRuntimeSceneAutoInstaller` non cerca piu' classi MapGrid concrete e configura lo switcher con soli root ArcGraph;
- build mirata `Assembly-CSharp` verificata con 0 errori dopo aggiornamento locale del `.csproj` generato;
- non e' ancora stato eseguito il gate visuale umano in Unity Editor dopo il delete.

Prossimo step `v0.70.08.15`:

- aprire Unity e verificare che non compaiano Missing Script in `Scene_ArcGraph`;
- eseguire Play Mode da `Scene_Bootstrap`;
- verificare camera, UI, placement, selezione, inspector, LM/LOS/PF;
- se il gate passa, rimuovere o aggiornare i soli commenti/testi storici che citano MapGrid come sistema ancora presente.

Esito `v0.70.08.15`:

- gate Unity Editor confermato dall'operatore: `Scene_ArcGraph` funziona dopo il delete fisico MapGrid;
- Play Mode e flusso runtime risultano operativi;
- verificati lato operatore camera, UI, placement, selezione, inspector e overlay principali;
- puliti i pochi riferimenti testuali residui nei commenti runtime ArcGraph che citavano MapGrid come componente ancora presente;
- non sono stati toccati file `.meta` sporchi/untracked non correlati.

Prossimo step consigliato:

- preparare handoff/integrazione con Biosfera su branch dedicato;
- oppure aprire il prossimo checkpoint UI post-F3, separando hardening UI da nuove feature.

---

#### v0.170 - Conseguenze Sociali Emergenti

## Stato
⏳ FUTURA / PENDING

## Obiettivo

Introdurre conseguenze sociali emergenti sopra world events, memoria soggettiva e comunicazione.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.170a | Audit reputazione/sospetto post-v0.16 | ⏳ |
| v0.170b | Catene sospetto/furto da eventi osservati | ⏳ |
| v0.170c | Giudizio sociale locale | ⏳ |
| v0.170d | Prime norme emergenti | ⏳ |
| v0.170e | Istituzioni runtime leggere | ⏳ |
| v0.170f | QA scenario sociale osservabile | ⏳ |

---

#### v0.180 - Observer Layer Pubblico ed Explainability Esterna

## Stato
⏳ FUTURA / PENDING

## Obiettivo

Costruire uno strato observer esterno leggibile sopra eventi, memoria, decisioni e conseguenze sociali.

---

| Checkpoint | Task | Stato |
|---|---|---|
| v0.180a | Timeline eventi mondo | ⏳ |
| v0.180b | Reason graph NPC | ⏳ |
| v0.180c | Pannelli observer leggibili | ⏳ |
| v0.180d | Reinserimento job traces v0.07 | ⏳ |
| v0.180e | QA observer end-to-end | ⏳ |

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
- recovery locale governata;
- classificazione failure produttiva;
- retry locale limitato;
- fallback target equivalente visibile;
- ritorno minimo verso memoria/credenze;
- EL del recupero Job.

---

# Funzionalità NON ancora implementate

NON esistono ancora o non sono ancora completi:

- movimento ordinario interamente governato da RunningAction multi-tick;
- path lungo Job-native;
- rimozione del fallback greedy ordinario;
- isolamento finale di `MoveIntent` e `MovementSystem` come dev/compatibilita';
- `LookAround` come Job esplicito invece di comportamento sistemico generico;
- search attiva come Job pienamente configurato e non come effetto laterale del movimento;
- belief lifecycle profondo;
- obsolescenza credenze cibo/oggetti;
- memoria completa da eventi needs;
- verifica locale delle credenze obsolete;
- escalation cognitiva produttiva;
- planner globale;
- recovery automatico intelligente.

Questi aspetti verranno introdotti solo dopo la chiusura della nuova fase `v0.15` sul movimento multi-tick e sulla authority del movimento.

---

*ARCONTIO Development Roadmap — documento vivo full fidelity — aggiornato Giugno 2026*


