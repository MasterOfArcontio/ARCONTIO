# ARCONTIO — Development Roadmap

> **Ritmo di lavoro:** 3 sessioni/settimana (Lunedì, Mercoledì, Giovedì) · 2 ore per sessione · 6 ore/settimana
> **Target v1.00:** Prima demo giocabile pubblica
> **Stato documento:** Aprile 2026

---

## Mappa versioni

| Versione | Focus principale | Periodo stimato | Stato |
|----------|-----------------|-----------------|-------|
| v0.01 | Sistemi base: perception, memory, token | ✅ Completata | — |
| v0.02 | Sistemi base 2 | ✅ Completata | — |
| v0.03 | Pathfinding landmark + ComplexEdge + failure ladder | Aprile 2026 | ✅ Completata | — |
| v0.04 | NpcDnaProfile · NpcProfile · Needs System · BeliefStore | Maggio–Giugno 2026 | ⚠️ Parziale: BeliefStore completato, PhysicalProfile e Health/Comfort restano aperti |
| v0.05 | Decision Layer completo | Giugno–Luglio 2026 | ⏳ Pending |
| v0.05.5 | EL-MBQD Runtime UI Registry + pannello laterale | Luglio 2026 | ✅ Completata |
| v0.06 | Job System + Step System | Luglio–Agosto 2026 | ⏳ Pending |
| v0.07 | Role System + CognitiveModulators | Agosto 2026 | ⏳ Pending |
| v0.08 | Petition System + Mobilità Sociale | Settembre 2026 | ⏳ Pending |
| v0.09 | ScheduleFrame + Planner Istituzionale | Ottobre 2026 | ⏳ Pending |
| v0.10 | Integration · Stress Test · Debug Overlay completo | Ottobre–Novembre 2026 | ⏳ Pending |
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
| 9 | Gio | Apr 2026 | Chiusura doc v0.03 + CLAUDE.md aggiornato | Documentazione | ✅ |

### Definition of Done v0.03

| Criterio | Stato |
|----------|-------|
| Landmark pathfinding attivabile da config | ✅ |
| Macro-route + last-mile funzionanti | ✅ |
| NpcObjectMemory: cibo community memorizzato e cercato | ✅ |
| ComplexEdge: strutture dati + recording attivi | ✅ |
| ComplexEdge: integrazione nel planner A* | ✅ |
| ComplexEdge: overlay visivo giallo | ✅ |
| Failure ladder operativa (BackOff / Replan / Blacklist) | ✅ |
| Job GoTo usa landmark con safe point | POSTICIPATO |
| Stress test 10–50 NPC senza thrashing | ✅ |

---

## v0.04 — Fondamenta NPC (DNA · Needs · BeliefStore)

**Obiettivo:** Costruire le strutture dati fondamentali di ogni NPC. Tutto il comportamento emergente dipende da questi layer — vanno fatti bene prima di procedere.

**Sistemi introdotti:**
- `NpcDnaProfile` — struttura immutabile per-NPC
- `NpcProfile` — struttura variabile runtime (Competence, Preference, Obligation)
- Needs System — Fame, Sete, Riposo, Salute, Comfort, Sicurezza, Stabilità, Socialità
- `BeliefStore` — aggregazione lazy della memoria per il Decision Layer

### Tabella sessioni v0.04

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | NpcDnaProfile | Struttura C# completa con tutti i campi | ✅ |
| 2 | Mer | NpcProfile | CompetenceProfile + PreferenceProfile + ObligationProfile | ✅ |
| 3 | Gio | NpcProfile | AssignedRole + serializzazione JSON | ✅ |
| 4 | Lun | NpcProfile | Calcolo distanza DNA↔NpcProfile | ✅ |
| 5 | Mer | NpcProfile | Integrazione con NPC esistenti (migrazione) | ✅ |
| 6 | Gio | Debug | Overlay debug distanza DNA↔NpcProfile | ✅ |
| 7 | Lun | Needs | Struttura Need generica con NeedAlert + NeedCritical | ✅ |
| 8 | Mer | Needs | Fame · Sete · Riposo/Sonno | ✅ |
| 9 | Gio | NpcProfile | PhysicalProfile — tratti fisici mutabili (Strength, Endurance, Agility, Intelligence) | ⏳ |
| 10 | Lun | Needs | Salute fisica · Comfort termico | ⚠️ Parziale |
| 11 | Mer | Needs | Needs psicologici: Sicurezza · Stabilità emotiva · Socialità | ✅ |
| 12 | Gio | Needs | Decay system: rapido/lento per categoria | ✅ |
| 13 | Lun | Debug | Overlay debug needs per NPC | ✅ |
| 14 | Mer | BeliefStore | BeliefEntry: struttura C# completa | ✅ |
| 15 | Gio | BeliefStore | Aggregazione lazy da MemoryStore su nuova traccia | ✅ |
| 16 | Lun | BeliefStore | Decay confidence + trigger su soglia minima | ✅ |
| 17 | Mer | BeliefStore | Invalidazione su job fallito | ✅ |
| 18 | Gio | BeliefStore | Query API per il Decision Layer | ✅ |
| 19 | Lun | QA | Test: BeliefStore vs MemoryStore, verifica omniscience | ✅ |

### Definition of Done v0.04

| Criterio | Stato |
|----------|-------|
| NpcDnaProfile: tutti i campi implementati e serializzabili | ✅ |
| NpcProfile: tre assi (Competence/Preference/Obligation) per dominio | ✅ |
| Distanza DNA↔NpcProfile calcolabile on-demand e visibile in overlay F9 | ✅ |
| PhysicalProfile: tratti fisici mutabili (Strength, Endurance, Agility, Intelligence) | ⏳ |
| Tutti gli 8 NeedKind definiti con NeedAlert + NeedCritical; Health/Comfort restano derivativi/parziali | ⚠️ Parziale |
| Decay differenziato per categoria (rapido/lento) | ✅ |
| BeliefStore attivo con aggiornamento lazy | ✅ |
| Nessuna violazione omniscience nei nuovi sistemi | ⏳ |

> **Nota step 10 — stato parziale:** `Health01` non va implementato come bisogno scalare autonomo. Il documento `ARCONTIO BodyWound System v2` elimina il concetto di HP globale e sposta la salute fisica sul futuro sistema anatomico: `BodyGraph`, `WoundInstance`, `Pain01`, `Blood01`, `IsAlive`. Anche `Comfort` non va trattato come valore isolato: resta un bisogno derivativo da ridefinire, con formula basata almeno su dolore, sangue/stato fisico e contesto ambientale. Lo step è quindi architetturalmente chiarito, ma non ancora completo a livello di implementazione runtime.

> **Nota step 11 — completato come baseline:** `Security`, `Stability` e `Sociality` sono attivati come bisogni psicologici interni con decay data-driven lento e flag `NeedAlert`/`NeedCritical` già derivati dalle soglie DNA. L'integrazione con pericoli percepiti, supporto sociale, social trust e BeliefStore/QuerySystem resta futura per non anticipare il Social Layer né introdurre accessi onniscienti.

> **Nota step 12 — completato:** il decay differenziato è esplicitato in due categorie runtime. `Hunger`, `Thirst` e `Rest` usano il gruppo fisiologico rapido; `Security`, `Stability` e `Sociality` usano il gruppo psicologico lento. I valori numerici restano letti da `needs_config.json` e normalizzati con fallback default per evitare che config parziali lascino decay a 0.

> **Nota step 13 — completato tramite riuso:** l'overlay debug needs era già presente nella card NPC e usa `NeedKind.COUNT`, `NpcNeeds.GetValue`, `NpcNeeds.IsAlert` e `NpcNeeds.IsCritical`. La sessione 13 non aggiunge una seconda pipeline UI: documenta e conferma il riuso di `BuildNeedsBars`/`UpdateNeedsBars`/`NeedBarRow.Set`, che mostrano tutti gli 8 need e i flag alert/critical già calcolati dal sistema needs.

> **Nota step 14 — completato come struttura dati:** `BeliefEntry`, `BeliefCategory`, `BeliefStatus` e `BeliefSource` sono stati introdotti come dati puri aderenti al documento `ARCONTIO BeliefStore QuerySystem Architecture`. Non sono ancora implementati `BeliefStore`, `BeliefUpdater`, aggregazione da `MemoryStore`, decay confidence, invalidazione su job fallito o `QuerySystem`, che restano negli step successivi.

> **Nota step 15 — completato come aggregazione lazy MVP:** `BeliefStore` è ora uno store passivo per-NPC con filtro banale `GetByCategoryAndStatus(...)`, senza metodi tipo "best" e senza ranking decisionale. `BeliefUpdater` aggrega in modo lazy le nuove `MemoryTrace` accettate o rinforzate dal `MemoryStore`, sia dal percorso percettivo diretto (`MemoryEncodingSystem`) sia dal percorso di comunicazione (`TokenAssimilationPipeline`). Le prime regole MVP mappano minacce in `Danger`, oggetti osservati in `Structure` e NPC osservati in `Social`, evitando lookup globali e classificazioni premature non presenti nella trace. Come proposta tecnica accettata, lo store include un cap iniziale conservativo di 64 entry per NPC; il pruning resta minimale e non cognitivo: rimuove prima entry `Discarded`, altrimenti la entry con prodotto `Confidence * Freshness` più basso. Restano futuri decay confidence, trigger su soglia minima, invalidazione su job fallito, policy pruning più ricca e QuerySystem/Explainability dedicati.

> **Nota correttiva step 15 — semantica oggetti nella MemoryTrace:** la memoria percettiva degli oggetti deve portare il tipo osservato. `ObjectSpottedEvent` conteneva già il `DefId`, ma `ObjectSpottedMemoryRule` non lo trasferiva nella `MemoryTrace`. È stato quindi aggiunto `SubjectDefId` alla trace e al DTO di salvataggio, così il `BeliefUpdater` può classificare `ObjectSpotted` in `Food`, `Rest` o `Structure` usando solo la memoria soggettiva. La regola resta conservativa: `food` produce `BeliefCategory.Food`, `bed` produce `BeliefCategory.Rest`, defId assente o non riconosciuto produce `BeliefCategory.Structure`. Nessun lookup su `World.Objects` viene introdotto nel Belief layer.

> **Nota step 16 — completato come decay passivo:** `BeliefDecaySystem` applica decay a `Confidence` e `Freshness` dei `BeliefEntry` per-NPC senza leggere world state oggettivo e senza introdurre logica di query. I rate sono data-driven in `belief_decay_config.json` tramite `BeliefDecayConfig`: Food/Rest decadono rapidamente, Social/Ownership e Situation usano un rate medio provvisorio, Danger decade lentamente e Structure molto lentamente. `Freshness` decade piu rapidamente della confidence tramite moltiplicatore dedicato. Le soglie configurabili marcano le entry come `Weak` o `Stale`; le entry `Discarded` o con confidence arrivata alla soglia di rimozione vengono eliminate dallo store.

> **Nota architetturale step 17 — invalidazione su job fallito:** lo step 17 deve introdurre un ponte MVP tra esecuzione e BeliefStore, non un Job System completo. Il documento `ARCONTIO BeliefStore QuerySystem Architecture` richiede che il `BeliefUpdater` si attivi anche quando un job fallisce e che il belief che ha guidato il job venga invalidato o indebolito. Nel codice attuale il Job System generale non esiste ancora: `activeJobJson` e' ancora un placeholder, mentre l'esecuzione reale passa da rule, command, `MoveIntent` e `MovementSystem`. Per questo lo step 17 deve usare un payload esplicito provvisorio, ad esempio `BeliefFailureSignal` / `BeliefOperationalFeedback`, prodotto dal punto della pipeline che scopre il fallimento. Una smentita diretta locale deve nascere da command/rule che verificano fisicamente il target, per esempio "sono arrivato alla cella dove credevo ci fosse lo stock e non c'e'"; un fallimento ambiguo deve nascere dal movimento/pathfinding, per esempio "non sono riuscito ad arrivare al target", e non deve implicare automaticamente che il belief sia falso; una contraddizione informativa deve nascere da comunicazione, memoria o percezione mediata. La responsabilita' futura sara' divisa cosi': JobSystem/Step/Command rilevano e classificano il fallimento; `BeliefUpdater` traduce il segnale in mutazione cognitiva; `BeliefStore` applica la mutazione passiva su `Confidence`, `Freshness` e `Status`. Quando il Job System definitivo arrivera', il produttore del payload passera' al Job System, ma la semantica cognitiva non deve spostarsi fuori dal `BeliefUpdater`. Policy MVP proposta: smentita diretta locale e certa -> `Discarded`; fallimento operativo ambiguo -> riduzione `Confidence` e possibile `Weak`; informazione contraddittoria -> riduzione `Confidence` e possibile `Conflicted`. Questo evita onniscienza e impedisce di dedurre "il cibo non esiste" da un semplice pathfinding fallito.

> **Nota step 17 — completato come ponte MVP:** `BeliefFailureSignal` e `BeliefFailureKind` rendono esplicito il feedback operativo verso il Belief layer. `BeliefUpdater.UpdateFromOperationalFailure(...)` applica la policy minima documentata: smentita diretta locale -> `Discarded`, fallimento ambiguo -> `Weak`, contraddizione riportata -> `Conflicted`. `BeliefStore` resta passivo e offre solo mutazioni meccaniche per id oppure per fallback categoria + posizione. Il primo produttore concreto e' `NeedsDecisionRule`, che invia una smentita diretta quando l'NPC arriva sulla cella dove credeva di trovare cibo e scopre localmente che il target non e' piu' valido. Il pathfinding non viene ancora agganciato per evitare di trasformare un fallimento di percorso in falsa certezza sul contenuto del belief.

> **Nota step 18 — completato come Query API MVP:** introdotto un `BeliefQueryService` separato dal `BeliefStore`, coerente con il modello Store + QueryService + Evaluators condivisi. Il BeliefStore resta passivo: non calcola ranking e non legge world state. La query riceve un `BeliefQueryContext` minimale (`GoalType`, `Urgency01`, `NpcPosition`, `MinConfidence`), filtra solo belief `Active`/`Weak` sopra soglia, applica gli evaluator MVP `ConfidenceBeliefEvaluator`, `FreshnessBeliefEvaluator` e `DistanceBeliefEvaluator`, quindi restituisce un `BeliefQueryResult` con `IsEmpty`, belief vincitore, score finale e breakdown `BeliefScoreContribution[]`. I pesi degli evaluator sono data-driven in `belief_query_config.json` tramite `BeliefQueryConfig`, rispettando la regola R9 del documento. Non sono stati implementati `RiskToleranceEvaluator`, ownership, social, norm o tuning offline: il documento cita RiskTolerance nella lista Fase 1, ma il piano di implementazione parla di tre evaluator MVP; per evitare overengineering lo si rimanda a quando il Decision Layer portera' realmente `RiskAversion` nel contesto. La sessione non sostituisce ancora `NeedsDecisionRule`: prepara l'API per il Decision Layer senza anticipare la migrazione delle rule legacy.

> **Nota step 19 — completato come QA BeliefStore/MemoryStore senza UI:** aggiunti test EditMode mirati per validare il contratto `MemoryTrace -> MemoryStore -> BeliefUpdater -> BeliefStore`. I test verificano che `ObjectSpotted` con `SubjectDefId` alimentare produca belief `Food`, che `ObjectSpotted` con `SubjectDefId` letto produca belief `Rest`, che tracce di pericolo e NPC osservati producano rispettivamente `Danger` e `Social`, e soprattutto che una trace `Dropped` dal `MemoryStore` non aggiorni il `BeliefStore`. La verifica e' costruita senza creare un `World` e senza leggere `Objects`, `FoodStocks`, database oggetti o altri stati oggettivi: il Belief layer resta derivativo rispetto alla memoria soggettiva accettata. Per decisione di sessione, la card grafica fluttuante dell'NPC non e' stata modificata: la visualizzazione BeliefStore nella UI resta rimandata a una sessione debug/overlay dedicata, cosi il QA architetturale della v0.04 resta separato dalla progettazione grafica.

---

## v0.05 — Decision Layer

**Obiettivo:** Implementare il cervello decisionale dell'NPC. È il layer più complesso dell'intero sistema — la fase più a rischio di slittamento.

**Sistemi introdotti:**
- Catalogo intenzioni (enum + metadati)
- Fase 1: generazione candidati con filtri
- Fase 2: scoring composito
- Fase 3: selezione weighted random con rumore

### Tabella sessioni v0.05

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Catalogo | Intenzioni: struttura enum + metadati per dominio | ⏳ |
| 2 | Mer | Fase 1 | Filtro ScheduleFrame + precondizioni fisiologiche | ⏳ |
| 3 | Gio | Fase 1 | Filtro ObligationProfile + QuerySystem/BeliefStore (confidence > 0) | ⏳ |
| 4 | Lun | Fase 1 | Filtro norme attive + SocialRisk | ⏳ |
| 5 | Mer | Fase 2 | Scoring: NeedUrgency continua (funzione lineare) | ⏳ |
| 6 | Gio | Fase 2 | Scoring: CompetenceAffinity + PreferenceAffinity | ⏳ |
| 7 | Lun | Fase 2 | Scoring: ObligationPressure + floor obbligatorio | ⏳ |
| 8 | Mer | Fase 2 | Scoring: MemoryConfidence + CognitiveModulators | ⏳ |
| 9 | Gio | Fase 3 | Weighted random top-N con parametro rumore | ⏳ |
| 10 | Lun | Fase 3 | Integrazione Impulsività nei CognitiveModulators | ⏳ |
| 11 | Mer | QA | Test end-to-end: DNA → Decision → Intenzione | ⏳ |
| 12 | Gio | QA | Verifica omniscience + audit input Decision Layer | ⏳ |

### Definition of Done v0.05

| Criterio | Stato |
|----------|-------|
| Catalogo intenzioni completo (~20 intenzioni) | ⏳ |
| Fase 1: set candidati 3–8 intenzioni per tick | ⏳ |
| Fase 2: scoring con tutti i termini implementati | ⏳ |
| Floor obbligatorio per obligation alta e need critico | ⏳ |
| Fase 3: weighted random con varianza modulata da DNA | ⏳ |
| NPC diversi con DNA diverso mostrano comportamenti diversi | ⏳ |
| Nessuna query diretta a MemoryStore o BeliefStore dal Decision Layer: accesso solo via QuerySystem | ⏳ |

---

## v0.05.5 — EL-MBQD Runtime UI Registry + pannello laterale

**Obiettivo:** Rendere osservabile in runtime il ciclo Memory → Belief → Query → Decision senza leggere il JSONL dalla UI e senza introdurre accessi onniscienti. Questa fase trasforma l'EL-MBQD da strumento solo testuale/file-based a layer diagnostico live, usando lo stesso modello gia' validato per l'EL Pathfinding: trace diagnostica → registry runtime bounded → ViewModel UI-friendly → pannello laterale.

**Sistemi introdotti:**
- `MemoryBeliefDecisionExplainabilityRegistry` — registry runtime passivo per-NPC
- Store bounded per trace `memory`, `belief`, `query`, `decision`, `bridge`
- ViewModel UI-friendly per pannello laterale
- Pannello EL tabbed: Memory · Belief · Decision · Pathfinding
- Scroll verticale nei tab con contenuto lungo
- Sub-pannelli con cornice, titolo, pallino colorato e righe key/value

### Tabella sessioni v0.05.5

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Registry | MemoryBeliefDecisionExplainabilityRegistry + store per-NPC bounded | ✅ |
| 2 | Mer | Emitters | Doppia uscita trace: JSONL opzionale + registry runtime UI-friendly | ✅ |
| 3 | Gio | ViewModel | Snapshot UI read-only con summary Memory, Belief, Query, Decision e Bridge | ✅ |
| 4 | Lun | UI | Pannello laterale tabbed con Memory, Belief, Decision e Pathfinding | ✅ |
| 5 | Mer | UI | ScrollRect, colori differenziati, sub-pannelli con cornice e pallini colorati | ✅ |
| 6 | Gio | QA | Copertura campi JSONL → UI + test EditMode registry/ViewModel | ✅ |

### Definition of Done v0.05.5

| Criterio | Stato |
|----------|-------|
| Il registry runtime conserva trace EL-MBQD recenti per NPC senza leggere file JSONL | ✅ |
| La UI legge solo ViewModel/snapshot, non MemoryStore, BeliefStore o world state oggettivo | ✅ |
| I tab Memory, Belief, Decision e Pathfinding hanno scroll verticale indipendente | ✅ |
| Colori diagnostici coerenti: verde ok, rosso errore/fallimento, ambra warning/fallback, blu informazione, bianco valore primario, grigio muted | ✅ |
| Ogni sezione usa sub-pannello con cornice, titolo e pallino colorato come linguaggio visuale comune | ✅ |
| Tutti i campi prodotti dal JSONL hanno una posizione UI o una motivazione esplicita di omissione | ✅ |
| Bridge Decision → Command legacy visibile nel tab Decision | ✅ |
| Nessun file `.meta`, `Library`, `Temp` o `Obj` viene modificato | ✅ |

> **Nota architetturale v0.05.5:** questa fase non sostituisce il JSONL. Il JSONL resta il formato persistente append-only per analisi offline; il registry runtime serve soltanto alla UI live. Entrambi devono ricevere snapshot gia' prodotti dai punti legittimi della pipeline, evitando che il pannello diventi un lettore globale di `World`, `MemoryStore`, `BeliefStore`, `Objects` o `FoodStocks`.

> **Numero step necessari:** 6 step/sessioni. Meno di 6 rischierebbe di mescolare registry, ViewModel, UI e QA nello stesso intervento; piu' di 6 sposterebbe lavoro che appartiene alla v0.10, dove il debug overlay completo verra' consolidato con il resto della simulazione.

---

## v0.06 — Job System + Step System

**Obiettivo:** Costruire il layer di esecuzione persistente. Ogni intenzione prodotta dal Decision Layer diventa un Job che sopravvive nel tempo, gestisce sequenze di Step e traccia fallimenti.

**Sistemi introdotti:**
- `JobRequest` · `Job` · `JobPlan` · `JobAction`
- `NpcJobState` (ActiveJob + Suspended + Queued)
- `JobArbiter` con preemption ladder
- `ReservationRecord`
- Step base: MoveTo, Reserve, Release, Wait, Observe, Search, PickUp, Drop, Consume, Communicate, Evaluate
- Failure learning: `(npcId, targetCell) → failureTick`

### Tabella sessioni v0.06

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Job | JobRequest + Job + JobPlan: strutture C# | ⏳ |
| 2 | Mer | Job | NpcJobState: ActiveJob + Suspended + Queued | ⏳ |
| 3 | Gio | Job | JobArbiter: accettazione JobRequest + preemption base | ⏳ |
| 4 | Lun | Job | Macchina a stati: Pending→Running→Completed/Failed/Aborted | ⏳ |
| 5 | Mer | Job | ReservationRecord: lock risorse condivise | ⏳ |
| 6 | Gio | Step | MoveTo · Reserve · Release · Wait | ⏳ |
| 7 | Lun | Step | Observe · Search · PickUp · Drop | ⏳ |
| 8 | Mer | Step | Consume · Communicate · Evaluate | ⏳ |
| 9 | Gio | Pipeline | CommandBuffer: integrazione step→command→system | ⏳ |
| 10 | Lun | Job | Preemption ladder completa (emergenziale / prioritario / normale) | ⏳ |
| 11 | Mer | Job | Failure learning: (npcId, targetCell) → failureTick | ⏳ |
| 12 | Gio | QA | Test end-to-end: Intenzione → Job → Step → Command → World | ⏳ |

### Definition of Done v0.06

| Criterio | Stato |
|----------|-------|
| Job: tutti gli stati implementati e transitabili correttamente | ⏳ |
| NpcJobState: 1 ActiveJob + N Suspended + N Queued | ⏳ |
| Preemption ladder funzionante | ⏳ |
| ReservationRecord: nessun conflitto su risorse condivise | ⏳ |
| Step base (~10): tutti con output Running/Success/Blocked/Failed/Cancelled | ⏳ |
| Nessuno Step modifica il world direttamente | ⏳ |
| Failure learning attivo senza violare omniscience | ⏳ |

---

### Aggiornamento operativo v0.06 — JobPhase

Decisione architetturale aggiornata: la v0.06 introduce `JobPhase` come livello intermedio tra `JobPlan` e `Step`.

Gerarchia operativa:
```text
Job
  -> JobPlan
     -> JobPhase
        -> Step
           -> Command
```

`JobPhase` rappresenta il "mini job" interno necessario per job complessi come `WorkAtAssignedPost` o `DefendCastleGate`. Non è però un job autonomo: non possiede priorità globale propria, non entra nella coda globale e non viene preemptato separatamente dal `Job`.

Sequenza di lavoro aggiornata:

| # | Sistema | Task aggiornato | Stato |
|---|---------|-----------------|-------|
| 1 | Job | `JobRequest` + `Job` + `JobPlan` + `JobPhase`: contratti dati C# puri | ⏳ |
| 2 | Job | `JobAction` + `StepResult`: tipi atomici, stati e failure reason | ⏳ |
| 3 | Job | `NpcJobState`: `ActiveJob` + `Suspended` + `Queued` | ⏳ |
| 4 | Job | `JobArbiter`: accettazione `JobRequest` + preemption base | ⏳ |
| 5 | Job | Macchina a stati: `Pending` → `Running` → `Completed` / `Failed` / `Aborted` | ⏳ |
| 6 | Job | `ReservationRecord`: lock risorse condivise | ⏳ |
| 7 | Step | Step base: `MoveTo` · `Reserve` · `Release` · `Wait` | ⏳ |
| 8 | Step | Step base: `Observe` · `Search` · `PickUp` · `Drop` | ⏳ |
| 9 | Step | Step base: `Consume` · `Communicate` · `Evaluate` | ⏳ |
| 10 | Pipeline | `CommandBuffer`: integrazione `Step` → `Command` → `World System` | ⏳ |
| 11 | Job | Preemption ladder completa: emergenziale / prioritario / normale | ⏳ |
| 12 | Job | Failure learning: `(npcId, targetCell) → failureTick` | ⏳ |
| 13 | QA | Test end-to-end: Intenzione → Job → JobPhase → Step → Command → World | ⏳ |

Definition of Done aggiuntiva:

| Criterio | Stato |
|----------|-------|
| `JobPlan` contiene `JobPhase` ordinate | ⏳ |
| `JobPhase` non è preemptabile separatamente dal `Job` | ⏳ |
| I job complessi possono esprimere sotto-obiettivi locali senza creare job ricorsivi | ⏳ |

## v0.07 — Role System + CognitiveModulators

**Obiettivo:** Dare a ogni NPC un carattere cognitivo e un'identità professionale che influenzi concretamente le sue decisioni. Prima versione di comportamento emergente differenziato per ruolo.

**Sistemi introdotti:**
- CognitiveModulators: Impulsività · AversioneRischio · Conformismo · Ottimismo/Pessimismo · Resilienza · Socievolezza
- Role bias: ObligationProfile attivato da AssignedRole
- Distanza DNA↔NpcProfile come trigger insoddisfazione

### Tabella sessioni v0.07

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Modulators | Impulsività + AversioneRischio: integrazione nello scoring | ⏳ |
| 2 | Mer | Modulators | Conformismo + Ottimismo/Pessimismo: integrazione nello scoring | ⏳ |
| 3 | Gio | Modulators | Resilienza + Socievolezza: effetti su need e soglie | ⏳ |
| 4 | Lun | Role | ObligationProfile attivato da AssignedRole | ⏳ |
| 5 | Mer | Role | Mestieri base: Agricoltore · Guardia · Coordinatore | ⏳ |
| 6 | Gio | Role | Mestieri: Fabbro · Magazziniere · Giudice | ⏳ |
| 7 | Lun | Stress | Test bias per ruolo: verifica comportamento emergente | ⏳ |
| 8 | Mer | Stress | Distanza DNA↔NpcProfile: trigger insoddisfazione attivo | ⏳ |
| 9 | Gio | QA | Audit: NPC con ruoli diversi producono comportamenti diversi | ⏳ |

### Definition of Done v0.07

| Criterio | Stato |
|----------|-------|
| 6 CognitiveModulators implementati e attivi nel Decision Layer | ⏳ |
| Role bias visibile: Guardia e Agricoltore si comportano diversamente | ⏳ |
| Distanza DNA↔NpcProfile supera soglia → insoddisfazione attiva | ⏳ |
| Mestieri base (6) implementati con ObligationProfile corretto | ⏳ |

---

## v0.08 — Petition System + Mobilità Sociale

**Obiettivo:** Implementare il ciclo sociale emergente bottom-up. I bisogni degli NPC diventano petizioni, le petizioni alimentano la struttura della colonia, la struttura abilita la mobilità di ruolo.

**Sistemi introdotti:**
- `PetitionInstance` (IWorldEvent soggettivo)
- Notice Board (world state)
- `ColonyStructure` + `PositionRegistry`
- Manager NPC con lettura bacheca
- Candidatura bottom-up + cooptazione top-down
- Rifiuto con escalation giudice / breakdown emotivo

### Tabella sessioni v0.08

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Petition | PetitionInstance: struttura + IWorldEvent | ⏳ |
| 2 | Mer | Petition | WritePetitionCommand + Notice Board world state | ⏳ |
| 3 | Gio | Petition | Step: porta petizione fisicamente alla bacheca | ⏳ |
| 4 | Lun | Colony | ColonyStructure: aggregazione petizioni per dominio | ⏳ |
| 5 | Mer | Colony | PositionRegistry: slot aperti/occupati per ruolo | ⏳ |
| 6 | Gio | Manager | Manager NPC: lettura bacheca via percezione normale | ⏳ |
| 7 | Lun | Manager | Manager NPC: apertura posizioni da analisi petizioni | ⏳ |
| 8 | Mer | Mobilità | Candidatura bottom-up: RoleDissatisfaction → intenzione Istituzionale | ⏳ |
| 9 | Gio | Mobilità | Cooptazione top-down: AssignRoleCommand → World System | ⏳ |
| 10 | Lun | Mobilità | Rifiuto NPC: escalation giudice (istituzionale) | ⏳ |
| 11 | Mer | Mobilità | Rifiuto NPC: breakdown emotivo (psicologico) | ⏳ |
| 12 | Gio | QA | Test ciclo completo: petizione → posizione → assegnazione | ⏳ |

### Definition of Done v0.08

| Criterio | Stato |
|----------|-------|
| NPC porta fisicamente petizione alla notice board | ⏳ |
| ColonyStructure aggrega petizioni per dominio correttamente | ⏳ |
| Manager NPC legge bacheca senza god mode | ⏳ |
| PositionRegistry: slot aperti/occupati aggiornati dinamicamente | ⏳ |
| Candidatura bottom-up funzionante | ⏳ |
| Cooptazione top-down funzionante | ⏳ |
| Rifiuto NPC genera almeno una delle due escalation | ⏳ |
| Nessuna violazione omniscience nel ciclo completo | ⏳ |

---

## v0.09 — ScheduleFrame + Planner Istituzionale

**Obiettivo:** Completare il layer istituzionale con la gestione degli schedule e il modello emergenziale Scenario C. Prima versione di risposta collettiva coordinata senza god mode.

**Sistemi introdotti:**
- `ScheduleFrame` (struttura dati per dominio/finestra)
- Planner NPC con scrittura schedule su bacheca
- Integrazione ScheduleFrame nel Decision Layer Fase 1
- Emergenza Scenario C: priorità per categoria ruolo

### Tabella sessioni v0.09

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Schedule | ScheduleFrame: struttura dati finestre per dominio | ⏳ |
| 2 | Mer | Schedule | Planner NPC: calcolo schedule + scrittura bacheca | ⏳ |
| 3 | Gio | Schedule | Lettura ScheduleFrame via percezione normale | ⏳ |
| 4 | Lun | Schedule | Integrazione nel Decision Layer Fase 1 | ⏳ |
| 5 | Mer | Emergenza | Scenario C: priorità per categoria ruolo | ⏳ |
| 6 | Gio | Emergenza | ObligationProfile attivato su trigger percepito di emergenza | ⏳ |
| 7 | Lun | QA | Test: risposta collettiva emergente a evento emergenziale | ⏳ |
| 8 | Mer | QA | Audit: il planner non ha god mode | ⏳ |
| 9 | Gio | QA | Stress test emergenza con 20+ NPC | ⏳ |

### Definition of Done v0.09

| Criterio | Stato |
|----------|-------|
| ScheduleFrame letto da bacheca via percezione normale | ⏳ |
| Decision Layer Fase 1 filtrata da ScheduleFrame corrente | ⏳ |
| Planner NPC scrive schedule senza accesso privilegiato | ⏳ |
| Emergenza Scenario C: risposta collettiva emerge senza script centrale | ⏳ |
| NPC con ruolo diverso reagisce diversamente alla stessa emergenza | ⏳ |

---

## v0.10 — Integration + Stress Test + Debug Overlay

**Obiettivo:** Validare l'intero sistema NPC end-to-end, ottimizzare le performance, completare il debug overlay e verificare sistematicamente l'omniscience constraint su tutti i layer.

**Sistemi introdotti:**
- Debug overlay completo (need · belief · intenzione attiva · job corrente)
- Profiling per NPC (tick budget)
- Audit omniscience sistematico

### Tabella sessioni v0.10

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Test | Stress test 50–100 NPC con pipeline completa | ⏳ |
| 2 | Mer | Perf | Profiling: bottleneck Decision Layer | ⏳ |
| 3 | Gio | Perf | Profiling: bottleneck BeliefStore + MemoryStore | ⏳ |
| 4 | Lun | Perf | Ottimizzazione tick budget per NPC | ⏳ |
| 5 | Mer | Debug | Overlay: need attuali + soglie per NPC selezionato | ⏳ |
| 6 | Gio | Debug | Overlay: BeliefStore entries + confidence per NPC | ⏳ |
| 7 | Lun | Debug | Overlay: intenzione attiva + score top-3 | ⏳ |
| 8 | Mer | Debug | Overlay: job corrente + step attivo | ⏳ |
| 9 | Gio | QA | Regressione omniscience: audit sistematico tutti i layer | ⏳ |
| 10 | Lun | QA | Bug fix emergenti dalla regressione | ⏳ |
| 11 | Mer | QA | Bug fix emergenti dalla regressione | ⏳ |
| 12 | Gio | Doc | Definition of Done v0.09 + CLAUDE.md aggiornato | ⏳ |

### Definition of Done v0.10

| Criterio | Stato |
|----------|-------|
| 50 NPC attivi senza thrashing né frame drop significativo | ⏳ |
| Tick budget per NPC stabile e misurabile | ⏳ |
| Debug overlay completo e leggibile in real time | ⏳ |
| Zero violazioni omniscience certificate dall'audit | ⏳ |
| Comportamento emergente osservabile e narrativamente coerente | ⏳ |
| CLAUDE.md aggiornato con architettura completa | ⏳ |

---

## v1.00 — Prima Demo Giocabile

**Obiettivo:** Assemblare tutti i sistemi in una demo giocabile pubblica. La v1.00 non è la fine del progetto — è il primo punto in cui qualcuno di esterno può giocare e capire ARCONTIO.

**Criteri minimi per v1.00:**

| Criterio | Note |
|----------|------|
| Mappa giocabile con almeno una colonia | Con strutture, risorse, NPC attivi |
| NPC con comportamento emergente visibile | Decisioni basate su memoria soggettiva |
| Mobilità sociale funzionante | Almeno 2–3 scambi di ruolo osservabili per partita |
| Sistema di petizioni attivo | Ciclo completo: bisogno → petizione → posizione → assegnazione |
| Debug overlay disponibile | Facoltativo per giocatore, obbligatorio per demo |
| Nessun crash bloccante | Sessione di gioco di almeno 30 minuti stabile |
| Build distribuibile | Esportabile da Unity come build standalone |

> **Nota:** La v1.00 non include necessariamente il layer politico, le fazioni, le elezioni o i sistemi narrativi avanzati documentati nell'Obsidian vault. Questi entrano nelle versioni successive.

---

## Riepilogo tempi

| Versione | Settimane | Sessioni totali | Periodo |
|----------|-----------|-----------------|---------|
| v0.03 | 3 | 9 | Aprile 2026 |
| v0.04 | 7 | 19 | Maggio–Giugno 2026 |
| v0.05 | 4 | 12 | Giugno–Luglio 2026 |
| v0.05.5 | 2 | 6 | Luglio 2026 |
| v0.06 | 4 | 12 | Luglio–Agosto 2026 |
| v0.07 | 3 | 9 | Agosto 2026 |
| v0.08 | 4 | 12 | Settembre 2026 |
| v0.09 | 3 | 9 | Ottobre 2026 |
| v0.10 | 4 | 12 | Ottobre–Novembre 2026 |
| **Totale** | **33** | **99** | **Apr → Nov 2026** |

> **Buffer consigliato:** +2–3 settimane. Le fasi v0.05 (Decision Layer) e v0.06 (Job System) sono le più a rischio di slittamento per bug subdoli legati all'omniscience constraint.

---

## Note architetturali permanenti

Queste regole non vanno mai violate indipendentemente dalla fase di sviluppo.

| Regola | Motivazione |
|--------|-------------|
| Nessun NPC legge il world state globale direttamente | Omniscience constraint fondamentale |
| Il Decision Layer consulta solo il QuerySystem: non legge direttamente né MemoryStore né BeliefStore | Disaccoppiamento memory/decision e mantenimento del BeliefStore come contenitore passivo |
| Nessuno Step modifica il world direttamente | Solo via Commands → World Systems |
| Il DNA non contiene mai stato runtime | Se un valore cambia, appartiene a NpcProfile |
| ExtensionData: non implementare senza caso d'uso concreto | Evita dump di stato nel DNA |
| ComplexEdge: no BFS fisica in InitializeNavigation | Crea onniscienza — failure learning va nel Job layer |
| AreaCenter landmark: non attivare prima di Doorway + Junction stabili | È la fonte #1 di spam landmark |
| Mantieni sempre il fallback micro-path | Se macro-route fallisce, l'NPC non deve bloccarsi |

---

*ARCONTIO Development Roadmap — documento vivo — aggiornato Aprile 2026*
