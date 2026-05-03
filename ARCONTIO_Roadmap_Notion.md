# ARCONTIO — Development Roadmap

> **Ritmo di lavoro:** 3 sessioni/settimana (Lunedì, Mercoledì, Giovedì) · 2 ore per sessione · 6 ore/settimana
> **Target v1.00:** Prima demo giocabile pubblica
> **Stato documento:** Aprile 2026

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
| v0.11 | Domain Reintegration: Jobs · Work · Social · Dormant Systems | Giugno 2026 | Pending |
| v0.12 | NPC Subjective Cognition Deepening | Giugno-Luglio 2026 | Pending |
| v0.13 | Social Consequence & Normative Emergence | Luglio 2026 | Pending |
| v0.14 | Explainability Public Layer / Observer Tools | Luglio-Agosto 2026 | Pending |
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

## v0.11 — Domain Reintegration: Jobs · Work · Social · Dormant Systems

> **Placeholder prossimo macro cantiere:** da aprire con audit dedicato prima di qualunque implementazione. Lo scope operativo resta da confermare alla luce della closure v0.10; nessun nuovo coding e' implicato da questa roadmap closeout.

### Tabella sessioni v0.11

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Jobs | Reinnesto Job runtime contracts | Pending |
| 2 | Mer | Work | WorkSystem activation | Pending |
| 3 | Gio | Social | SocialSystem activation | Pending |
| 4 | Lun | Rules | Dormant rules reintegro | Pending |
| 5 | Mer | Commands | Bridge job/social → command pipeline | Pending |
| 6 | Gio | QA | Test integrazione domini superiori | Pending |

> **Nota:** qui tornano dentro i layer già abbozzati ma oggi sospesi o solo parzialmente innestati.

---

## v0.12 — NPC Subjective Cognition Deepening

### Tabella sessioni v0.12

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Cognition | Rimozione scansioni onniscienti residue | Pending |
| 2 | Mer | Belief | Confidence refinement | Pending |
| 3 | Gio | Communication | Distorsione token ampliata | Pending |
| 4 | Lun | Decision | Local verification loops | Pending |
| 5 | Mer | QA | Audit anti-telepatia completo | Pending |

---

## v0.13 — Social Consequence & Normative Emergence

### Tabella sessioni v0.13

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Reputation | Tracce reputazionali | Pending |
| 2 | Mer | Suspicion | Catene sospetto/furto | Pending |
| 3 | Gio | Social | Giudizio di gruppo | Pending |
| 4 | Lun | Norms | Prime norme emergenti | Pending |
| 5 | Mer | QA | Scenario sociale osservabile | Pending |

---

## v0.14 — Explainability Public Layer / Observer Tools

### Tabella sessioni v0.14

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Observer | Timeline eventi mondo | Pending |
| 2 | Mer | Observer | Reason graph NPC | Pending |
| 3 | Gio | UI | Pannelli observer leggibili | Pending |
| 4 | Lun | Explainability | Reinserimento v0.07 job traces | Pending |
| 5 | Mer | QA | Observer end-to-end | Pending |

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

*ARCONTIO Development Roadmap — documento vivo full fidelity — aggiornato Aprile 2026*
