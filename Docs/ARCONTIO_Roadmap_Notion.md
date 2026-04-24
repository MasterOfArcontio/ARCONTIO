# ARCONTIO â€” Development Roadmap

> **Ritmo di lavoro:** 3 sessioni/settimana (LunedÃ¬, MercoledÃ¬, GiovedÃ¬) Â· 2 ore per sessione Â· 6 ore/settimana
> **Target v1.00:** Prima demo giocabile pubblica
> **Stato documento:** Aprile 2026

---

## Mappa versioni

| Versione | Focus principale | Periodo stimato | Stato |
|----------|-----------------|-----------------|-------|
| v0.01 | Sistemi base: perception, memory, token | Completata | - |
| v0.02 | Sistemi base 2 | Completata | - |
| v0.03 | Pathfinding landmark + ComplexEdge + failure ladder | Aprile 2026 | Completata |
| v0.04 | NpcDnaProfile · NpcProfile · Needs System · BeliefStore | Maggio-Giugno 2026 | Parziale: BeliefStore completato, PhysicalProfile e Health/Comfort restano aperti |
| v0.05 | Decision Layer completo | Giugno-Luglio 2026 | Pending |
| v0.05.5 | EL-MBQD Runtime UI Registry + pannello laterale | Luglio 2026 | Completata |
| v0.06 | Job System + Step System | Luglio-Agosto 2026 | Completata |
| v0.07 | Explainability Layer v0.06: Job / Phase / Step / Command | Agosto 2026 | Pending |
| v0.08 | Role System + CognitiveModulators | Agosto-Settembre 2026 | Pending |
| v0.09 | Petition System + Mobilita Sociale | Settembre-Ottobre 2026 | Pending |
| v0.10 | ScheduleFrame + Planner Istituzionale | Ottobre 2026 | Pending |
| v0.11 | Integration · Stress Test · Debug Overlay completo | Ottobre-Novembre 2026 | Pending |
| v1.00 | Prima demo giocabile pubblica | TBD | Target |
---

## v0.03 â€” Chiusura Landmark Pathfinding

**Obiettivo:** Liquidare il debito tecnico accumulato e portare il sistema di navigazione a uno stato completo e stabile prima di costruire i layer NPC sopra.

**Sistemi giÃ  completati in v0.02:**
- Landmark pathfinding (macro-route + last-mile) âœ…
- Direct Path con Commitment Percettivo âœ…
- NpcObjectMemory (cibo community) âœ…
- ComplexEdge strutture dati + recording âœ…

### Tabella sessioni v0.03

| # | Giorno | Data stimata | Task | Sistema | Stato |
|---|--------|-------------|------|---------|-------|
| 1 | Lun | Apr 2026 | ComplexEdge: integrazione planner A* | Pathfinding | âœ… |
| 2 | Mer | Apr 2026 | ComplexEdge: overlay visivo giallo + test | Pathfinding | âœ… |
| 3 | Gio | Apr 2026 | Job GoTo: integrazione landmark + safe point | Job System | âœ… |
| 4 | Lun | Apr 2026 | Failure ladder: BackOff / Replan | Pathfinding | âœ… |
| 5 | Mer | Apr 2026 | Failure ladder: Blacklist edge | Pathfinding | âœ… |
| 6 | Gio | Apr 2026 | Stress test 10â€“50 NPC, tuning parametri | Performance | âœ… |
| 7 | Lun | Apr 2026 | Definition of Done v0.03: verifica criteri | QA | âœ… |
| 8 | Mer | Apr 2026 | Bug fix emergenti dallo stress test | QA | âœ… |
| 9 | Gio | Apr 2026 | Chiusura doc v0.03 + CLAUDE.md aggiornato | Documentazione | âœ… |

### Definition of Done v0.03

| Criterio | Stato |
|----------|-------|
| Landmark pathfinding attivabile da config | âœ… |
| Macro-route + last-mile funzionanti | âœ… |
| NpcObjectMemory: cibo community memorizzato e cercato | âœ… |
| ComplexEdge: strutture dati + recording attivi | âœ… |
| ComplexEdge: integrazione nel planner A* | âœ… |
| ComplexEdge: overlay visivo giallo | âœ… |
| Failure ladder operativa (BackOff / Replan / Blacklist) | âœ… |
| Job GoTo usa landmark con safe point | POSTICIPATO |
| Stress test 10â€“50 NPC senza thrashing | âœ… |

---

## v0.04 â€” Fondamenta NPC (DNA Â· Needs Â· BeliefStore)

**Obiettivo:** Costruire le strutture dati fondamentali di ogni NPC. Tutto il comportamento emergente dipende da questi layer â€” vanno fatti bene prima di procedere.

**Sistemi introdotti:**
- `NpcDnaProfile` â€” struttura immutabile per-NPC
- `NpcProfile` â€” struttura variabile runtime (Competence, Preference, Obligation)
- Needs System â€” Fame, Sete, Riposo, Salute, Comfort, Sicurezza, StabilitÃ , SocialitÃ 
- `BeliefStore` â€” aggregazione lazy della memoria per il Decision Layer

### Tabella sessioni v0.04

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | NpcDnaProfile | Struttura C# completa con tutti i campi | âœ… |
| 2 | Mer | NpcProfile | CompetenceProfile + PreferenceProfile + ObligationProfile | âœ… |
| 3 | Gio | NpcProfile | AssignedRole + serializzazione JSON | âœ… |
| 4 | Lun | NpcProfile | Calcolo distanza DNAâ†”NpcProfile | âœ… |
| 5 | Mer | NpcProfile | Integrazione con NPC esistenti (migrazione) | âœ… |
| 6 | Gio | Debug | Overlay debug distanza DNAâ†”NpcProfile | âœ… |
| 7 | Lun | Needs | Struttura Need generica con NeedAlert + NeedCritical | âœ… |
| 8 | Mer | Needs | Fame Â· Sete Â· Riposo/Sonno | âœ… |
| 9 | Gio | NpcProfile | PhysicalProfile â€” tratti fisici mutabili (Strength, Endurance, Agility, Intelligence) | â³ |
| 10 | Lun | Needs | Salute fisica Â· Comfort termico | âš ï¸ Parziale |
| 11 | Mer | Needs | Needs psicologici: Sicurezza Â· StabilitÃ  emotiva Â· SocialitÃ  | âœ… |
| 12 | Gio | Needs | Decay system: rapido/lento per categoria | âœ… |
| 13 | Lun | Debug | Overlay debug needs per NPC | âœ… |
| 14 | Mer | BeliefStore | BeliefEntry: struttura C# completa | âœ… |
| 15 | Gio | BeliefStore | Aggregazione lazy da MemoryStore su nuova traccia | âœ… |
| 16 | Lun | BeliefStore | Decay confidence + trigger su soglia minima | âœ… |
| 17 | Mer | BeliefStore | Invalidazione su job fallito | âœ… |
| 18 | Gio | BeliefStore | Query API per il Decision Layer | âœ… |
| 19 | Lun | QA | Test: BeliefStore vs MemoryStore, verifica omniscience | âœ… |

### Definition of Done v0.04

| Criterio | Stato |
|----------|-------|
| NpcDnaProfile: tutti i campi implementati e serializzabili | âœ… |
| NpcProfile: tre assi (Competence/Preference/Obligation) per dominio | âœ… |
| Distanza DNAâ†”NpcProfile calcolabile on-demand e visibile in overlay F9 | âœ… |
| PhysicalProfile: tratti fisici mutabili (Strength, Endurance, Agility, Intelligence) | â³ |
| Tutti gli 8 NeedKind definiti con NeedAlert + NeedCritical; Health/Comfort restano derivativi/parziali | âš ï¸ Parziale |
| Decay differenziato per categoria (rapido/lento) | âœ… |
| BeliefStore attivo con aggiornamento lazy | âœ… |
| Nessuna violazione omniscience nei nuovi sistemi | â³ |

> **Nota step 10 â€” stato parziale:** `Health01` non va implementato come bisogno scalare autonomo. Il documento `ARCONTIO BodyWound System v2` elimina il concetto di HP globale e sposta la salute fisica sul futuro sistema anatomico: `BodyGraph`, `WoundInstance`, `Pain01`, `Blood01`, `IsAlive`. Anche `Comfort` non va trattato come valore isolato: resta un bisogno derivativo da ridefinire, con formula basata almeno su dolore, sangue/stato fisico e contesto ambientale. Lo step Ã¨ quindi architetturalmente chiarito, ma non ancora completo a livello di implementazione runtime.

> **Nota step 11 â€” completato come baseline:** `Security`, `Stability` e `Sociality` sono attivati come bisogni psicologici interni con decay data-driven lento e flag `NeedAlert`/`NeedCritical` giÃ  derivati dalle soglie DNA. L'integrazione con pericoli percepiti, supporto sociale, social trust e BeliefStore/QuerySystem resta futura per non anticipare il Social Layer nÃ© introdurre accessi onniscienti.

> **Nota step 12 â€” completato:** il decay differenziato Ã¨ esplicitato in due categorie runtime. `Hunger`, `Thirst` e `Rest` usano il gruppo fisiologico rapido; `Security`, `Stability` e `Sociality` usano il gruppo psicologico lento. I valori numerici restano letti da `needs_config.json` e normalizzati con fallback default per evitare che config parziali lascino decay a 0.

> **Nota step 13 â€” completato tramite riuso:** l'overlay debug needs era giÃ  presente nella card NPC e usa `NeedKind.COUNT`, `NpcNeeds.GetValue`, `NpcNeeds.IsAlert` e `NpcNeeds.IsCritical`. La sessione 13 non aggiunge una seconda pipeline UI: documenta e conferma il riuso di `BuildNeedsBars`/`UpdateNeedsBars`/`NeedBarRow.Set`, che mostrano tutti gli 8 need e i flag alert/critical giÃ  calcolati dal sistema needs.

> **Nota step 14 â€” completato come struttura dati:** `BeliefEntry`, `BeliefCategory`, `BeliefStatus` e `BeliefSource` sono stati introdotti come dati puri aderenti al documento `ARCONTIO BeliefStore QuerySystem Architecture`. Non sono ancora implementati `BeliefStore`, `BeliefUpdater`, aggregazione da `MemoryStore`, decay confidence, invalidazione su job fallito o `QuerySystem`, che restano negli step successivi.

> **Nota step 15 â€” completato come aggregazione lazy MVP:** `BeliefStore` Ã¨ ora uno store passivo per-NPC con filtro banale `GetByCategoryAndStatus(...)`, senza metodi tipo "best" e senza ranking decisionale. `BeliefUpdater` aggrega in modo lazy le nuove `MemoryTrace` accettate o rinforzate dal `MemoryStore`, sia dal percorso percettivo diretto (`MemoryEncodingSystem`) sia dal percorso di comunicazione (`TokenAssimilationPipeline`). Le prime regole MVP mappano minacce in `Danger`, oggetti osservati in `Structure` e NPC osservati in `Social`, evitando lookup globali e classificazioni premature non presenti nella trace. Come proposta tecnica accettata, lo store include un cap iniziale conservativo di 64 entry per NPC; il pruning resta minimale e non cognitivo: rimuove prima entry `Discarded`, altrimenti la entry con prodotto `Confidence * Freshness` piÃ¹ basso. Restano futuri decay confidence, trigger su soglia minima, invalidazione su job fallito, policy pruning piÃ¹ ricca e QuerySystem/Explainability dedicati.

> **Nota correttiva step 15 â€” semantica oggetti nella MemoryTrace:** la memoria percettiva degli oggetti deve portare il tipo osservato. `ObjectSpottedEvent` conteneva giÃ  il `DefId`, ma `ObjectSpottedMemoryRule` non lo trasferiva nella `MemoryTrace`. Ãˆ stato quindi aggiunto `SubjectDefId` alla trace e al DTO di salvataggio, cosÃ¬ il `BeliefUpdater` puÃ² classificare `ObjectSpotted` in `Food`, `Rest` o `Structure` usando solo la memoria soggettiva. La regola resta conservativa: `food` produce `BeliefCategory.Food`, `bed` produce `BeliefCategory.Rest`, defId assente o non riconosciuto produce `BeliefCategory.Structure`. Nessun lookup su `World.Objects` viene introdotto nel Belief layer.

> **Nota step 16 â€” completato come decay passivo:** `BeliefDecaySystem` applica decay a `Confidence` e `Freshness` dei `BeliefEntry` per-NPC senza leggere world state oggettivo e senza introdurre logica di query. I rate sono data-driven in `belief_decay_config.json` tramite `BeliefDecayConfig`: Food/Rest decadono rapidamente, Social/Ownership e Situation usano un rate medio provvisorio, Danger decade lentamente e Structure molto lentamente. `Freshness` decade piu rapidamente della confidence tramite moltiplicatore dedicato. Le soglie configurabili marcano le entry come `Weak` o `Stale`; le entry `Discarded` o con confidence arrivata alla soglia di rimozione vengono eliminate dallo store.

> **Nota architetturale step 17 â€” invalidazione su job fallito:** lo step 17 deve introdurre un ponte MVP tra esecuzione e BeliefStore, non un Job System completo. Il documento `ARCONTIO BeliefStore QuerySystem Architecture` richiede che il `BeliefUpdater` si attivi anche quando un job fallisce e che il belief che ha guidato il job venga invalidato o indebolito. Nel codice attuale il Job System generale non esiste ancora: `activeJobJson` e' ancora un placeholder, mentre l'esecuzione reale passa da rule, command, `MoveIntent` e `MovementSystem`. Per questo lo step 17 deve usare un payload esplicito provvisorio, ad esempio `BeliefFailureSignal` / `BeliefOperationalFeedback`, prodotto dal punto della pipeline che scopre il fallimento. Una smentita diretta locale deve nascere da command/rule che verificano fisicamente il target, per esempio "sono arrivato alla cella dove credevo ci fosse lo stock e non c'e'"; un fallimento ambiguo deve nascere dal movimento/pathfinding, per esempio "non sono riuscito ad arrivare al target", e non deve implicare automaticamente che il belief sia falso; una contraddizione informativa deve nascere da comunicazione, memoria o percezione mediata. La responsabilita' futura sara' divisa cosi': JobSystem/Step/Command rilevano e classificano il fallimento; `BeliefUpdater` traduce il segnale in mutazione cognitiva; `BeliefStore` applica la mutazione passiva su `Confidence`, `Freshness` e `Status`. Quando il Job System definitivo arrivera', il produttore del payload passera' al Job System, ma la semantica cognitiva non deve spostarsi fuori dal `BeliefUpdater`. Policy MVP proposta: smentita diretta locale e certa -> `Discarded`; fallimento operativo ambiguo -> riduzione `Confidence` e possibile `Weak`; informazione contraddittoria -> riduzione `Confidence` e possibile `Conflicted`. Questo evita onniscienza e impedisce di dedurre "il cibo non esiste" da un semplice pathfinding fallito.

> **Nota step 17 â€” completato come ponte MVP:** `BeliefFailureSignal` e `BeliefFailureKind` rendono esplicito il feedback operativo verso il Belief layer. `BeliefUpdater.UpdateFromOperationalFailure(...)` applica la policy minima documentata: smentita diretta locale -> `Discarded`, fallimento ambiguo -> `Weak`, contraddizione riportata -> `Conflicted`. `BeliefStore` resta passivo e offre solo mutazioni meccaniche per id oppure per fallback categoria + posizione. Il primo produttore concreto e' `NeedsDecisionRule`, che invia una smentita diretta quando l'NPC arriva sulla cella dove credeva di trovare cibo e scopre localmente che il target non e' piu' valido. Il pathfinding non viene ancora agganciato per evitare di trasformare un fallimento di percorso in falsa certezza sul contenuto del belief.

> **Nota step 18 â€” completato come Query API MVP:** introdotto un `BeliefQueryService` separato dal `BeliefStore`, coerente con il modello Store + QueryService + Evaluators condivisi. Il BeliefStore resta passivo: non calcola ranking e non legge world state. La query riceve un `BeliefQueryContext` minimale (`GoalType`, `Urgency01`, `NpcPosition`, `MinConfidence`), filtra solo belief `Active`/`Weak` sopra soglia, applica gli evaluator MVP `ConfidenceBeliefEvaluator`, `FreshnessBeliefEvaluator` e `DistanceBeliefEvaluator`, quindi restituisce un `BeliefQueryResult` con `IsEmpty`, belief vincitore, score finale e breakdown `BeliefScoreContribution[]`. I pesi degli evaluator sono data-driven in `belief_query_config.json` tramite `BeliefQueryConfig`, rispettando la regola R9 del documento. Non sono stati implementati `RiskToleranceEvaluator`, ownership, social, norm o tuning offline: il documento cita RiskTolerance nella lista Fase 1, ma il piano di implementazione parla di tre evaluator MVP; per evitare overengineering lo si rimanda a quando il Decision Layer portera' realmente `RiskAversion` nel contesto. La sessione non sostituisce ancora `NeedsDecisionRule`: prepara l'API per il Decision Layer senza anticipare la migrazione delle rule legacy.

> **Nota step 19 â€” completato come QA BeliefStore/MemoryStore senza UI:** aggiunti test EditMode mirati per validare il contratto `MemoryTrace -> MemoryStore -> BeliefUpdater -> BeliefStore`. I test verificano che `ObjectSpotted` con `SubjectDefId` alimentare produca belief `Food`, che `ObjectSpotted` con `SubjectDefId` letto produca belief `Rest`, che tracce di pericolo e NPC osservati producano rispettivamente `Danger` e `Social`, e soprattutto che una trace `Dropped` dal `MemoryStore` non aggiorni il `BeliefStore`. La verifica e' costruita senza creare un `World` e senza leggere `Objects`, `FoodStocks`, database oggetti o altri stati oggettivi: il Belief layer resta derivativo rispetto alla memoria soggettiva accettata. Per decisione di sessione, la card grafica fluttuante dell'NPC non e' stata modificata: la visualizzazione BeliefStore nella UI resta rimandata a una sessione debug/overlay dedicata, cosi il QA architetturale della v0.04 resta separato dalla progettazione grafica.

---

## v0.05 â€” Decision Layer

**Obiettivo:** Implementare il cervello decisionale dell'NPC. Ãˆ il layer piÃ¹ complesso dell'intero sistema â€” la fase piÃ¹ a rischio di slittamento.

**Sistemi introdotti:**
- Catalogo intenzioni (enum + metadati)
- Fase 1: generazione candidati con filtri
- Fase 2: scoring composito
- Fase 3: selezione weighted random con rumore

### Tabella sessioni v0.05

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Catalogo | Intenzioni: struttura enum + metadati per dominio | â³ |
| 2 | Mer | Fase 1 | Filtro ScheduleFrame + precondizioni fisiologiche | â³ |
| 3 | Gio | Fase 1 | Filtro ObligationProfile + QuerySystem/BeliefStore (confidence > 0) | â³ |
| 4 | Lun | Fase 1 | Filtro norme attive + SocialRisk | â³ |
| 5 | Mer | Fase 2 | Scoring: NeedUrgency continua (funzione lineare) | â³ |
| 6 | Gio | Fase 2 | Scoring: CompetenceAffinity + PreferenceAffinity | â³ |
| 7 | Lun | Fase 2 | Scoring: ObligationPressure + floor obbligatorio | â³ |
| 8 | Mer | Fase 2 | Scoring: MemoryConfidence + CognitiveModulators | â³ |
| 9 | Gio | Fase 3 | Weighted random top-N con parametro rumore | â³ |
| 10 | Lun | Fase 3 | Integrazione ImpulsivitÃ  nei CognitiveModulators | â³ |
| 11 | Mer | QA | Test end-to-end: DNA â†’ Decision â†’ Intenzione | â³ |
| 12 | Gio | QA | Verifica omniscience + audit input Decision Layer | â³ |

### Definition of Done v0.05

| Criterio | Stato |
|----------|-------|
| Catalogo intenzioni completo (~20 intenzioni) | â³ |
| Fase 1: set candidati 3â€“8 intenzioni per tick | â³ |
| Fase 2: scoring con tutti i termini implementati | â³ |
| Floor obbligatorio per obligation alta e need critico | â³ |
| Fase 3: weighted random con varianza modulata da DNA | â³ |
| NPC diversi con DNA diverso mostrano comportamenti diversi | â³ |
| Nessuna query diretta a MemoryStore o BeliefStore dal Decision Layer: accesso solo via QuerySystem | â³ |

---

## v0.05.5 â€” EL-MBQD Runtime UI Registry + pannello laterale

**Obiettivo:** Rendere osservabile in runtime il ciclo Memory â†’ Belief â†’ Query â†’ Decision senza leggere il JSONL dalla UI e senza introdurre accessi onniscienti. Questa fase trasforma l'EL-MBQD da strumento solo testuale/file-based a layer diagnostico live, usando lo stesso modello gia' validato per l'EL Pathfinding: trace diagnostica â†’ registry runtime bounded â†’ ViewModel UI-friendly â†’ pannello laterale.

**Sistemi introdotti:**
- `MemoryBeliefDecisionExplainabilityRegistry` â€” registry runtime passivo per-NPC
- Store bounded per trace `memory`, `belief`, `query`, `decision`, `bridge`
- ViewModel UI-friendly per pannello laterale
- Pannello EL tabbed: Memory Â· Belief Â· Decision Â· Pathfinding
- Scroll verticale nei tab con contenuto lungo
- Sub-pannelli con cornice, titolo, pallino colorato e righe key/value

### Tabella sessioni v0.05.5

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Registry | MemoryBeliefDecisionExplainabilityRegistry + store per-NPC bounded | âœ… |
| 2 | Mer | Emitters | Doppia uscita trace: JSONL opzionale + registry runtime UI-friendly | âœ… |
| 3 | Gio | ViewModel | Snapshot UI read-only con summary Memory, Belief, Query, Decision e Bridge | âœ… |
| 4 | Lun | UI | Pannello laterale tabbed con Memory, Belief, Decision e Pathfinding | âœ… |
| 5 | Mer | UI | ScrollRect, colori differenziati, sub-pannelli con cornice e pallini colorati | âœ… |
| 6 | Gio | QA | Copertura campi JSONL â†’ UI + test EditMode registry/ViewModel | âœ… |

### Definition of Done v0.05.5

| Criterio | Stato |
|----------|-------|
| Il registry runtime conserva trace EL-MBQD recenti per NPC senza leggere file JSONL | âœ… |
| La UI legge solo ViewModel/snapshot, non MemoryStore, BeliefStore o world state oggettivo | âœ… |
| I tab Memory, Belief, Decision e Pathfinding hanno scroll verticale indipendente | âœ… |
| Colori diagnostici coerenti: verde ok, rosso errore/fallimento, ambra warning/fallback, blu informazione, bianco valore primario, grigio muted | âœ… |
| Ogni sezione usa sub-pannello con cornice, titolo e pallino colorato come linguaggio visuale comune | âœ… |
| Tutti i campi prodotti dal JSONL hanno una posizione UI o una motivazione esplicita di omissione | âœ… |
| Bridge Decision â†’ Command legacy visibile nel tab Decision | âœ… |
| Nessun file `.meta`, `Library`, `Temp` o `Obj` viene modificato | âœ… |

> **Nota architetturale v0.05.5:** questa fase non sostituisce il JSONL. Il JSONL resta il formato persistente append-only per analisi offline; il registry runtime serve soltanto alla UI live. Entrambi devono ricevere snapshot gia' prodotti dai punti legittimi della pipeline, evitando che il pannello diventi un lettore globale di `World`, `MemoryStore`, `BeliefStore`, `Objects` o `FoodStocks`.

> **Numero step necessari:** 6 step/sessioni. Meno di 6 rischierebbe di mescolare registry, ViewModel, UI e QA nello stesso intervento; piu' di 6 sposterebbe lavoro che appartiene alla v0.10, dove il debug overlay completo verra' consolidato con il resto della simulazione.

---

## v0.06 â€” Job System + Step System

**Obiettivo:** Costruire il layer di esecuzione persistente. Ogni intenzione prodotta dal Decision Layer diventa un Job che sopravvive nel tempo, gestisce sequenze di Step e traccia fallimenti.

**Sistemi introdotti:**
- `JobRequest` Â· `Job` Â· `JobPlan` Â· `JobAction`
- `NpcJobState` (ActiveJob + Suspended + Queued)
- `JobArbiter` con preemption ladder
- `ReservationRecord`
- Step base: MoveTo, Reserve, Release, Wait, Observe, Search, PickUp, Drop, Consume, Communicate, Evaluate
- Failure learning: `(npcId, targetCell) â†’ failureTick`

### Tabella sessioni v0.06

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Job | JobRequest + Job + JobPlan: strutture C# | âœ… |
| 2 | Mer | Job | NpcJobState: ActiveJob + Suspended + Queued | âœ… |
| 3 | Gio | Job | JobArbiter: accettazione JobRequest + preemption base | âœ… |
| 4 | Lun | Job | Macchina a stati: Pendingâ†’Runningâ†’Completed/Failed/Aborted | âœ… |
| 5 | Mer | Job | ReservationRecord: lock risorse condivise | âœ… |
| 6 | Gio | Step | MoveTo Â· Reserve Â· Release Â· Wait | âœ… |
| 7 | Lun | Step | Observe Â· Search Â· PickUp Â· Drop | âœ… |
| 8 | Mer | Step | Consume Â· Communicate Â· Evaluate | âœ… |
| 9 | Gio | Pipeline | CommandBuffer: integrazione stepâ†’commandâ†’system | âœ… |
| 10 | Lun | Job | Preemption ladder completa (emergenziale / prioritario / normale) | âœ… |
| 11 | Mer | Job | Failure learning: (npcId, targetCell) â†’ failureTick | âœ… |
| 12 | Gio | QA | Test end-to-end: Intenzione â†’ Job â†’ Step â†’ Command â†’ World | âœ… |

### Definition of Done v0.06

| Criterio | Stato |
|----------|-------|
| Job: tutti gli stati implementati e transitabili correttamente | âœ… |
| NpcJobState: 1 ActiveJob + N Suspended + N Queued | âœ… |
| Preemption ladder funzionante | âœ… |
| ReservationRecord: nessun conflitto su risorse condivise | âœ… |
| Step base (~10): tutti con output Running/Success/Blocked/Failed/Cancelled | âœ… |
| Nessuno Step modifica il world direttamente | âœ… |
| Failure learning attivo senza violare omniscience | âœ… |

---

### Aggiornamento operativo v0.06 â€” JobPhase

Decisione architetturale aggiornata: la v0.06 introduce `JobPhase` come livello intermedio tra `JobPlan` e `Step`.

Gerarchia operativa:
```text
Job
  -> JobPlan
     -> JobPhase
        -> Step
           -> Command
```

`JobPhase` rappresenta il "mini job" interno necessario per job complessi come `WorkAtAssignedPost` o `DefendCastleGate`. Non Ã¨ perÃ² un job autonomo: non possiede prioritÃ  globale propria, non entra nella coda globale e non viene preemptato separatamente dal `Job`.

Sequenza di lavoro aggiornata:

| # | Sistema | Task aggiornato | Stato |
|---|---------|-----------------|-------|
| 1 | Job | `JobRequest` + `Job` + `JobPlan` + `JobPhase`: contratti dati C# puri | âœ… |
| 2 | Job | `JobAction` + `StepResult`: tipi atomici, stati e failure reason | âœ… |
| 3 | Job | `NpcJobState`: `ActiveJob` + `Suspended` + `Queued` | âœ… |
| 4 | Job | `JobArbiter`: accettazione `JobRequest` + preemption base | âœ… |
| 5 | Job | Macchina a stati: `Pending` â†’ `Running` â†’ `Completed` / `Failed` / `Aborted` | âœ… |
| 6 | Job | `ReservationRecord`: lock risorse condivise | âœ… |
| 7 | Step | Step base: `MoveTo` Â· `Reserve` Â· `Release` Â· `Wait` | âœ… |
| 8 | Step | Step base: `Observe` Â· `Search` Â· `PickUp` Â· `Drop` | âœ… |
| 9 | Step | Step base: `Consume` Â· `Communicate` Â· `Evaluate` | âœ… |
| 10 | Pipeline | `CommandBuffer`: integrazione `Step` â†’ `Command` â†’ `World System` | âœ… |
| 11 | Job | Preemption ladder completa: emergenziale / prioritario / normale | âœ… |
| 12 | Job | Failure learning: `(npcId, targetCell) â†’ failureTick` | âœ… |
| 13 | QA | Test end-to-end: Intenzione â†’ Job â†’ JobPhase â†’ Step â†’ Command â†’ World | âœ… |

Definition of Done aggiuntiva:

| Criterio | Stato |
|----------|-------|
| `JobPlan` contiene `JobPhase` ordinate | âœ… |
| `JobPhase` non Ã¨ preemptabile separatamente dal `Job` | âœ… |
| I job complessi possono esprimere sotto-obiettivi locali senza creare job ricorsivi | âœ… |

## v0.07 - Explainability Layer v0.06 (Job / Phase / Step / Command)

**Obiettivo:** Estendere l'Explainability Layer oltre il ciclo Memory -> Belief -> Query -> Decision, rendendo osservabile anche il layer di esecuzione introdotto dalla v0.06: `JobRequest`, `Job`, `JobPhase`, `Step`, `StepResult`, `JobStateMachine`, `Reservation`, `CommandBuffer` e `FailureLearning`.

**Sistemi introdotti:**
- Nuovi `TraceKind` EL per il layer Job/Step/Command
- Emitter e sink JSONL estesi per i record v0.06
- Registry runtime bounded esteso con famiglie `job`, `step`, `reservation`, `command`, `failure`
- ViewModel runtime read-only esteso con snapshot di job execution
- Tab UI dedicata `Job` nel pannello laterale
- QA EditMode e scenario runtime per Explainability v0.06

### Tabella sessioni v0.07

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Types | Estendere i contratti dati EL con `JobRequest`, `JobLifecycle`, `JobPhase`, `Step`, `StepResult`, `JobState`, `JobArbitration`, `Reservation`, `Command`, `FailureLearning` | Pending |
| 2 | Mer | Emitters | Estendere emitter e sink JSONL con i nuovi record v0.06 | Pending |
| 3 | Gio | Bridge | Tracciare il passaggio `Decision -> JobRequest` senza rompere il bridge legacy | Pending |
| 4 | Lun | Job | Trace ciclo di vita del `Job`: created, activated, suspended, completed, failed, cancelled | Pending |
| 5 | Mer | Job | Trace di `JobPhase`: entered, completed, interrupted | Pending |
| 6 | Gio | Step | Trace di `Step` e `StepResult`, con distinzione `Running`, `Succeeded`, `Waiting`, `Blocked`, `Failed` | Pending |
| 7 | Lun | StateMachine | Trace di `JobStateMachine` e snapshot runtime di `NpcJobState` | Pending |
| 8 | Mer | Arbitration | Trace di `JobArbiter` e `JobPreemptionLadder` | Pending |
| 9 | Gio | Reservation | Trace di `ReservationStore`: accepted, denied, released, expired | Pending |
| 10 | Lun | Commands | Trace di `JobCommandBuffer`: enqueue e snapshot diagnostico comandi | Pending |
| 11 | Mer | Failure | Trace di `JobFailureLearningStore` e aggiornamento penalty | Pending |
| 12 | Gio | Registry | Estendere il registry runtime bounded con buffer separati per job/step/command/reservation/failure | Pending |
| 13 | Lun | ViewModel | Estendere il ViewModel con `LatestJob*`, `LatestStep*`, `CurrentNpcJobState` e liste recenti | Pending |
| 14 | Mer | Timeline | Integrare i nuovi kind nella timeline combinata con summary corte e leggibili | Pending |
| 15 | Gio | UI | Aggiungere tab `Job` con sezioni: job corrente, fase, step, state machine, arbitration, reservation, command, failure | Pending |
| 16 | Lun | UI | Rifinire il tab `Decision` separando `legacy bridge` e `Decision -> JobRequest` | Pending |
| 17 | Mer | QA | Test EditMode dedicati: emitter -> registry -> ViewModel -> timeline | Pending |
| 18 | Gio | QA | Scenario runtime: job semplice, multi-phase, waiting, blocked, reservation denied, preemption, failure learning | Pending |

### Definition of Done v0.07

| Criterio | Stato |
|----------|-------|
| L'EL conserva trace runtime e JSONL per `JobRequest`, `Job`, `JobPhase`, `Step`, `StepResult`, `JobState`, `JobArbitration`, `Reservation`, `Command`, `FailureLearning` | Pending |
| La UI legge solo ViewModel e registry, senza accesso diretto a `Job`, `NpcJobState`, `ReservationStore` o `CommandBuffer` | Pending |
| Il tab `Job` mostra chiaramente job corrente, fase corrente, step corrente e ultimo `StepResult` | Pending |
| Le reservation e la preemption sono visibili e spiegabili in runtime | Pending |
| Il bridge legacy resta distinguibile dal nuovo path `Decision -> JobRequest` | Pending |
| La timeline combinata include anche gli eventi v0.06 senza perdere leggibilita | Pending |
| Esistono test EditMode per tutti i nuovi payload EL v0.06 | Pending |
| Nessuna modifica dell'EL introduce accessi onniscienti al world state | Pending |

---
## v0.08 â€” Role System + CognitiveModulators

**Obiettivo:** Dare a ogni NPC un carattere cognitivo e un'identitÃ  professionale che influenzi concretamente le sue decisioni. Prima versione di comportamento emergente differenziato per ruolo.

**Sistemi introdotti:**
- CognitiveModulators: ImpulsivitÃ  Â· AversioneRischio Â· Conformismo Â· Ottimismo/Pessimismo Â· Resilienza Â· Socievolezza
- Role bias: ObligationProfile attivato da AssignedRole
- Distanza DNAâ†”NpcProfile come trigger insoddisfazione

### Tabella sessioni v0.08

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Modulators | ImpulsivitÃ  + AversioneRischio: integrazione nello scoring | â³ |
| 2 | Mer | Modulators | Conformismo + Ottimismo/Pessimismo: integrazione nello scoring | â³ |
| 3 | Gio | Modulators | Resilienza + Socievolezza: effetti su need e soglie | â³ |
| 4 | Lun | Role | ObligationProfile attivato da AssignedRole | â³ |
| 5 | Mer | Role | Mestieri base: Agricoltore Â· Guardia Â· Coordinatore | â³ |
| 6 | Gio | Role | Mestieri: Fabbro Â· Magazziniere Â· Giudice | â³ |
| 7 | Lun | Stress | Test bias per ruolo: verifica comportamento emergente | â³ |
| 8 | Mer | Stress | Distanza DNAâ†”NpcProfile: trigger insoddisfazione attivo | â³ |
| 9 | Gio | QA | Audit: NPC con ruoli diversi producono comportamenti diversi | â³ |

### Definition of Done v0.08

| Criterio | Stato |
|----------|-------|
| 6 CognitiveModulators implementati e attivi nel Decision Layer | â³ |
| Role bias visibile: Guardia e Agricoltore si comportano diversamente | â³ |
| Distanza DNAâ†”NpcProfile supera soglia â†’ insoddisfazione attiva | â³ |
| Mestieri base (6) implementati con ObligationProfile corretto | â³ |

---

## v0.09 â€” Petition System + MobilitÃ  Sociale

**Obiettivo:** Implementare il ciclo sociale emergente bottom-up. I bisogni degli NPC diventano petizioni, le petizioni alimentano la struttura della colonia, la struttura abilita la mobilitÃ  di ruolo.

**Sistemi introdotti:**
- `PetitionInstance` (IWorldEvent soggettivo)
- Notice Board (world state)
- `ColonyStructure` + `PositionRegistry`
- Manager NPC con lettura bacheca
- Candidatura bottom-up + cooptazione top-down
- Rifiuto con escalation giudice / breakdown emotivo

### Tabella sessioni v0.09

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Petition | PetitionInstance: struttura + IWorldEvent | â³ |
| 2 | Mer | Petition | WritePetitionCommand + Notice Board world state | â³ |
| 3 | Gio | Petition | Step: porta petizione fisicamente alla bacheca | â³ |
| 4 | Lun | Colony | ColonyStructure: aggregazione petizioni per dominio | â³ |
| 5 | Mer | Colony | PositionRegistry: slot aperti/occupati per ruolo | â³ |
| 6 | Gio | Manager | Manager NPC: lettura bacheca via percezione normale | â³ |
| 7 | Lun | Manager | Manager NPC: apertura posizioni da analisi petizioni | â³ |
| 8 | Mer | MobilitÃ  | Candidatura bottom-up: RoleDissatisfaction â†’ intenzione Istituzionale | â³ |
| 9 | Gio | MobilitÃ  | Cooptazione top-down: AssignRoleCommand â†’ World System | â³ |
| 10 | Lun | MobilitÃ  | Rifiuto NPC: escalation giudice (istituzionale) | â³ |
| 11 | Mer | MobilitÃ  | Rifiuto NPC: breakdown emotivo (psicologico) | â³ |
| 12 | Gio | QA | Test ciclo completo: petizione â†’ posizione â†’ assegnazione | â³ |

### Definition of Done v0.09

| Criterio | Stato |
|----------|-------|
| NPC porta fisicamente petizione alla notice board | â³ |
| ColonyStructure aggrega petizioni per dominio correttamente | â³ |
| Manager NPC legge bacheca senza god mode | â³ |
| PositionRegistry: slot aperti/occupati aggiornati dinamicamente | â³ |
| Candidatura bottom-up funzionante | â³ |
| Cooptazione top-down funzionante | â³ |
| Rifiuto NPC genera almeno una delle due escalation | â³ |
| Nessuna violazione omniscience nel ciclo completo | â³ |

---

## v0.10 â€” ScheduleFrame + Planner Istituzionale

**Obiettivo:** Completare il layer istituzionale con la gestione degli schedule e il modello emergenziale Scenario C. Prima versione di risposta collettiva coordinata senza god mode.

**Sistemi introdotti:**
- `ScheduleFrame` (struttura dati per dominio/finestra)
- Planner NPC con scrittura schedule su bacheca
- Integrazione ScheduleFrame nel Decision Layer Fase 1
- Emergenza Scenario C: prioritÃ  per categoria ruolo

### Tabella sessioni v0.10

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Schedule | ScheduleFrame: struttura dati finestre per dominio | â³ |
| 2 | Mer | Schedule | Planner NPC: calcolo schedule + scrittura bacheca | â³ |
| 3 | Gio | Schedule | Lettura ScheduleFrame via percezione normale | â³ |
| 4 | Lun | Schedule | Integrazione nel Decision Layer Fase 1 | â³ |
| 5 | Mer | Emergenza | Scenario C: prioritÃ  per categoria ruolo | â³ |
| 6 | Gio | Emergenza | ObligationProfile attivato su trigger percepito di emergenza | â³ |
| 7 | Lun | QA | Test: risposta collettiva emergente a evento emergenziale | â³ |
| 8 | Mer | QA | Audit: il planner non ha god mode | â³ |
| 9 | Gio | QA | Stress test emergenza con 20+ NPC | â³ |

### Definition of Done v0.10

| Criterio | Stato |
|----------|-------|
| ScheduleFrame letto da bacheca via percezione normale | â³ |
| Decision Layer Fase 1 filtrata da ScheduleFrame corrente | â³ |
| Planner NPC scrive schedule senza accesso privilegiato | â³ |
| Emergenza Scenario C: risposta collettiva emerge senza script centrale | â³ |
| NPC con ruolo diverso reagisce diversamente alla stessa emergenza | â³ |

---

## v0.11 â€” Integration + Stress Test + Debug Overlay

**Obiettivo:** Validare l'intero sistema NPC end-to-end, ottimizzare le performance, completare il debug overlay e verificare sistematicamente l'omniscience constraint su tutti i layer.

**Sistemi introdotti:**
- Debug overlay completo (need Â· belief Â· intenzione attiva Â· job corrente)
- Profiling per NPC (tick budget)
- Audit omniscience sistematico

### Tabella sessioni v0.11

| # | Giorno | Sistema | Task | Stato |
|---|--------|---------|------|-------|
| 1 | Lun | Test | Stress test 50â€“100 NPC con pipeline completa | â³ |
| 2 | Mer | Perf | Profiling: bottleneck Decision Layer | â³ |
| 3 | Gio | Perf | Profiling: bottleneck BeliefStore + MemoryStore | â³ |
| 4 | Lun | Perf | Ottimizzazione tick budget per NPC | â³ |
| 5 | Mer | Debug | Overlay: need attuali + soglie per NPC selezionato | â³ |
| 6 | Gio | Debug | Overlay: BeliefStore entries + confidence per NPC | â³ |
| 7 | Lun | Debug | Overlay: intenzione attiva + score top-3 | â³ |
| 8 | Mer | Debug | Overlay: job corrente + step attivo | â³ |
| 9 | Gio | QA | Regressione omniscience: audit sistematico tutti i layer | â³ |
| 10 | Lun | QA | Bug fix emergenti dalla regressione | â³ |
| 11 | Mer | QA | Bug fix emergenti dalla regressione | â³ |
| 12 | Gio | Doc | Definition of Done v0.11 + CLAUDE.md aggiornato | â³ |

### Definition of Done v0.11

| Criterio | Stato |
|----------|-------|
| 50 NPC attivi senza thrashing nÃ© frame drop significativo | â³ |
| Tick budget per NPC stabile e misurabile | â³ |
| Debug overlay completo e leggibile in real time | â³ |
| Zero violazioni omniscience certificate dall'audit | â³ |
| Comportamento emergente osservabile e narrativamente coerente | â³ |
| CLAUDE.md aggiornato con architettura completa | â³ |

---

## v1.00 â€” Prima Demo Giocabile

**Obiettivo:** Assemblare tutti i sistemi in una demo giocabile pubblica. La v1.00 non Ã¨ la fine del progetto â€” Ã¨ il primo punto in cui qualcuno di esterno puÃ² giocare e capire ARCONTIO.

**Criteri minimi per v1.00:**

| Criterio | Note |
|----------|------|
| Mappa giocabile con almeno una colonia | Con strutture, risorse, NPC attivi |
| NPC con comportamento emergente visibile | Decisioni basate su memoria soggettiva |
| MobilitÃ  sociale funzionante | Almeno 2â€“3 scambi di ruolo osservabili per partita |
| Sistema di petizioni attivo | Ciclo completo: bisogno â†’ petizione â†’ posizione â†’ assegnazione |
| Debug overlay disponibile | Facoltativo per giocatore, obbligatorio per demo |
| Nessun crash bloccante | Sessione di gioco di almeno 30 minuti stabile |
| Build distribuibile | Esportabile da Unity come build standalone |

> **Nota:** La v1.00 non include necessariamente il layer politico, le fazioni, le elezioni o i sistemi narrativi avanzati documentati nell'Obsidian vault. Questi entrano nelle versioni successive.

---

## Riepilogo tempi

| Versione | Settimane | Sessioni totali | Periodo |
|----------|-----------|-----------------|---------|
| v0.03 | 3 | 9 | Aprile 2026 |
| v0.04 | 7 | 19 | Maggio-Giugno 2026 |
| v0.05 | 4 | 12 | Giugno-Luglio 2026 |
| v0.05.5 | 2 | 6 | Luglio 2026 |
| v0.06 | 4 | 12 | Luglio-Agosto 2026 |
| v0.07 | 6 | 18 | Agosto 2026 |
| v0.08 | 3 | 9 | Agosto-Settembre 2026 |
| v0.09 | 4 | 12 | Settembre-Ottobre 2026 |
| v0.10 | 3 | 9 | Ottobre 2026 |
| v0.11 | 4 | 12 | Ottobre-Novembre 2026 |
| **Totale** | **39** | **117** | **Apr -> Nov 2026** |
> **Buffer consigliato:** +2â€“3 settimane. Le fasi v0.05 (Decision Layer) e v0.06 (Job System) sono le piÃ¹ a rischio di slittamento per bug subdoli legati all'omniscience constraint.

---

## Note architetturali permanenti

Queste regole non vanno mai violate indipendentemente dalla fase di sviluppo.

| Regola | Motivazione |
|--------|-------------|
| Nessun NPC legge il world state globale direttamente | Omniscience constraint fondamentale |
| Il Decision Layer consulta solo il QuerySystem: non legge direttamente nÃ© MemoryStore nÃ© BeliefStore | Disaccoppiamento memory/decision e mantenimento del BeliefStore come contenitore passivo |
| Nessuno Step modifica il world direttamente | Solo via Commands â†’ World Systems |
| Il DNA non contiene mai stato runtime | Se un valore cambia, appartiene a NpcProfile |
| ExtensionData: non implementare senza caso d'uso concreto | Evita dump di stato nel DNA |
| ComplexEdge: no BFS fisica in InitializeNavigation | Crea onniscienza â€” failure learning va nel Job layer |
| AreaCenter landmark: non attivare prima di Doorway + Junction stabili | Ãˆ la fonte #1 di spam landmark |
| Mantieni sempre il fallback micro-path | Se macro-route fallisce, l'NPC non deve bloccarsi |

---

*ARCONTIO Development Roadmap â€” documento vivo â€” aggiornato Aprile 2026*


