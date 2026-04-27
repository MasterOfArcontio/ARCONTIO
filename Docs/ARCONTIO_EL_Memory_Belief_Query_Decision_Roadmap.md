# ARCONTIO - Explainability Layer
## Memory - BeliefStore - BeliefQuery - Decision

Stato documento: roadmap tecnica per integrazione EL-MBD.

Versione di partenza: v0.05.31.

Obiettivo principale: chiudere la v0.05 con strumenti runtime non grafici, leggibili e analizzabili, senza introdurre nuovo gameplay e senza anticipare il Job System v0.06.

---

## 1. Scopo

L'Explainability Layer Memory/Belief/Query/Decision, abbreviato EL-MBD, deve rendere ispezionabile il ciclo cognitivo dell'NPC:

```text
Perception -> MemoryTrace -> BeliefStore -> BeliefQueryService -> Decision Layer -> Bridge legacy / Job futuro
```

L'EL-MBD deve permettere di rispondere a domande pratiche durante i test runtime:

- quale informazione soggettiva e' entrata nel MemoryStore;
- quale BeliefEntry e' stata creata, rinforzata, indebolita o scartata;
- quale query e' stata eseguita dal Decision Layer;
- quale belief ha vinto la query e con quale breakdown;
- quali candidati decisionali sono stati generati;
- quale intenzione ha vinto e perche';
- quale command legacy e' stato prodotto dal bridge v0.05;
- quale debito deve passare al Job System v0.06.

---

## 2. Vincolo Architetturale

L'EL-MBD e' un layer diagnostico one-way:

```text
Simulazione -> Snapshot diagnostico -> JSONL / ring buffer / overlay futuro
```

Regole non negoziabili:

- l'EL non modifica `World`;
- l'EL non modifica `MemoryStore`;
- l'EL non modifica `BeliefStore`;
- l'EL non ricalcola score o decisioni;
- l'EL non interroga `World.Objects`, `FoodStocks` o altri store oggettivi per arricchire i log;
- l'EL registra solo dati gia' disponibili nel punto della pipeline in cui viene chiamato;
- se un dato non e' disponibile, il log deve indicare `unknown` o ometterlo, non cercarlo altrove.

Questa regola impedisce che il debug diventi una fonte di onniscienza indiretta.

---

## 3. Formato Log

Il formato principale e' JSONL append-only.

Ogni riga e' un JSON indipendente, cosi puo' essere letta da script, editor testuali o strumenti esterni senza parser custom complessi.

Envelope comune:

```json
{
  "schema": "arcontio_el_mbd.v1",
  "kind": "decision",
  "tick": 120,
  "npcId": 3
}
```

Tipi di record previsti:

- `memory`
- `belief`
- `query`
- `decision`
- `bridge`

---

## 4. Record Memory

Scopo: spiegare la nascita della memoria soggettiva.

Campi principali:

- `eventType`
- `traceType`
- `subjectId`
- `secondarySubjectId`
- `subjectDefId`
- `cell`
- `cellText`
- `intensity01`
- `reliability01`
- `isHeard`
- `heardKind`
- `sourceSpeakerId`
- `storeResult`

Valori ammessi per `storeResult`:

- `Inserted`
- `Reinforced`
- `Replaced`
- `Dropped`
- `Ignored`

Esempio:

```json
{
  "schema": "arcontio_el_mbd.v1",
  "kind": "memory",
  "tick": 100,
  "npcId": 3,
  "memory": {
    "eventType": "ObjectSpottedEvent",
    "traceType": "ObjectSpotted",
    "subjectId": 42,
    "subjectDefId": "food_stock_small",
    "cellText": "(12, 8)",
    "intensity01": 0.9,
    "reliability01": 0.8,
    "isHeard": false,
    "storeResult": "Inserted"
  }
}
```

Punto di emissione previsto:

```text
MemoryEncodingSystem.Update()
```

Il record va emesso subito dopo l'esito di `MemoryStore.AddOrMerge(...)`.

---

## 5. Record Belief

Scopo: spiegare la trasformazione `MemoryTrace -> BeliefEntry` e le mutazioni operative del BeliefStore.

Campi principali:

- `operation`
- `sourceTraceType`
- `category`
- `beliefId`
- `estimatedCell`
- `estimatedCellText`
- `confidence`
- `freshness`
- `source`
- `status`
- `sourceCount`
- `reason`

Valori ammessi per `operation`:

- `Created`
- `Merged`
- `Reinforced`
- `Conflicted`
- `Weakened`
- `Stale`
- `Discarded`
- `RemovedByDecay`
- `Ignored`

Esempio:

```json
{
  "schema": "arcontio_el_mbd.v1",
  "kind": "belief",
  "tick": 100,
  "npcId": 3,
  "belief": {
    "operation": "Created",
    "sourceTraceType": "ObjectSpotted",
    "category": "Food",
    "beliefId": 8,
    "estimatedCellText": "(12, 8)",
    "confidence": 0.9,
    "freshness": 0.8,
    "source": "Seen",
    "status": "Active",
    "sourceCount": 1,
    "reason": "ObjectDefIdClassifiedAsFood"
  }
}
```

Punti di emissione previsti:

```text
BeliefUpdater.UpdateFromTrace(...)
BeliefUpdater.UpdateFromOperationalFailure(...)
BeliefDecaySystem.Update(...)
```

Per la prima integrazione, il punto prioritario e' `BeliefUpdater`: conosce la ragione cognitiva della mutazione senza costringere `BeliefStore` a ragionare.

---

## 6. Record Query

Scopo: spiegare il comportamento del `BeliefQueryService`.

Campi principali:

- `goalType`
- `urgency01`
- `npcCell`
- `npcCellText`
- `minConfidence`
- `candidateCount`
- `usableCandidateCount`
- `isEmpty`
- `emptyReason`
- `winner`
- `contributions`

Il `winner` deve contenere:

- `beliefId`
- `category`
- `estimatedCell`
- `estimatedCellText`
- `confidence`
- `freshness`
- `status`
- `finalScore`

Ogni contribution deve contenere:

- `label`
- `value`

Esempio:

```json
{
  "schema": "arcontio_el_mbd.v1",
  "kind": "query",
  "tick": 120,
  "npcId": 3,
  "query": {
    "goalType": "Food",
    "urgency01": 0.91,
    "npcCellText": "(10, 8)",
    "minConfidence": 0.2,
    "candidateCount": 2,
    "usableCandidateCount": 1,
    "isEmpty": false,
    "winner": {
      "beliefId": 8,
      "category": "Food",
      "estimatedCellText": "(12, 8)",
      "confidence": 0.75,
      "freshness": 0.8,
      "status": "Active",
      "finalScore": 0.63
    },
    "contributions": [
      { "label": "ConfidenceScore", "value": 0.3 },
      { "label": "FreshnessScore", "value": 0.24 },
      { "label": "DistancePenalty", "value": -0.09 }
    ]
  }
}
```

Punto di emissione previsto:

```text
BeliefQueryService.QueryBest(...)
```

Questo e' il record piu' importante per certificare che il Decision Layer lavora su belief soggettivi e non su world state.

---

## 7. Record Decision

Scopo: spiegare generazione candidati, scoring e selezione.

Campi principali:

- `auditValid`
- `candidateCount`
- `selectedIntent`
- `selectedScore`
- `selectedIndex`
- `candidates`
- `selection`

Ogni candidato deve contenere:

- `intent`
- `available`
- `need`
- `needUrgency01`
- `isCritical`
- `requiresBeliefTarget`
- `beliefResultEmpty`
- `belief`
- `score`
- `scoreContributions`

La sezione `selection` deve contenere:

- `topN`
- `noise01`
- `impulsivity01`
- `effectiveNoise01`

Esempio:

```json
{
  "schema": "arcontio_el_mbd.v1",
  "kind": "decision",
  "tick": 120,
  "npcId": 3,
  "decision": {
    "auditValid": true,
    "candidateCount": 4,
    "selectedIntent": "EatKnownFood",
    "selectedScore": 1.43,
    "selectedIndex": 0,
    "candidates": [
      {
        "intent": "EatKnownFood",
        "available": true,
        "need": "Hunger",
        "needUrgency01": 0.91,
        "isCritical": true,
        "requiresBeliefTarget": true,
        "beliefResultEmpty": false,
        "belief": {
          "category": "Food",
          "beliefId": 8,
          "confidence": 0.75,
          "freshness": 0.8,
          "estimatedCellText": "(12, 8)"
        },
        "score": 1.43,
        "scoreContributions": [
          { "label": "NeedUrgency", "value": 0.91 },
          { "label": "CompetenceAffinity", "value": 0.04 },
          { "label": "PreferenceAffinity", "value": 0.12 },
          { "label": "ObligationPressure", "value": 0.15 },
          { "label": "MemoryConfidence", "value": 0.26 },
          { "label": "CognitiveModulators", "value": -0.05 },
          { "label": "MandatoryFloor", "value": 0.0 }
        ]
      }
    ],
    "selection": {
      "topN": 3,
      "noise01": 0.15,
      "impulsivity01": 0.5,
      "effectiveNoise01": 0.325
    }
  }
}
```

Punto di emissione MVP:

```text
NeedsDecisionRule.TryPlanFromDecisionLayer(...)
```

Punto di emissione definitivo:

```text
DecisionSystem.Evaluate(...)
```

---

## 8. Record Bridge

Scopo: spiegare il ponte provvisorio v0.05 tra Decision Layer e command legacy.

Campi principali:

- `selectedIntent`
- `commandName`
- `handled`
- `didMove`
- `didSteal`
- `targetCell`
- `targetCellText`
- `targetSource`
- `legacyFallbackUsed`
- `reason`

Esempio:

```json
{
  "schema": "arcontio_el_mbd.v1",
  "kind": "bridge",
  "tick": 120,
  "npcId": 3,
  "bridge": {
    "selectedIntent": "EatKnownFood",
    "commandName": "SetMoveIntentCommand",
    "handled": true,
    "didMove": true,
    "didSteal": false,
    "targetCellText": "(12, 8)",
    "targetSource": "BeliefQuery",
    "legacyFallbackUsed": false
  }
}
```

Questo record distingue:

- decisione corretta ma non ancora eseguibile;
- fallback legacy;
- command prodotto correttamente;
- intenzione rimandata al Job System v0.06.

---

## 9. Configurazione Proposta

Sezione da aggiungere a `game_params.json` quando parte l'implementazione:

```json
"memory_belief_decision_explainability": {
  "enabled": true,
  "defaultVerbosity": 2,
  "maxTrackedNpcs": 5,
  "trackedNpcIds": [],
  "trackActiveNpcOnly": false,
  "writeJsonLog": true,
  "jsonLogFileNamePattern": "arcontio_el_mbd_{yyyyMMdd_HHmmss}.jsonl",
  "logMemory": true,
  "logBelief": true,
  "logQuery": true,
  "logDecision": true,
  "logBridge": true,
  "includeCandidates": true,
  "includeScoreBreakdown": true,
  "includeRejectedCandidates": false
}
```

Livelli di verbosity:

```text
0 = spento
1 = solo decisione selezionata e bridge command
2 = memory, belief, query e decision sintetici
3 = include candidati e score breakdown
4 = include anche candidati filtrati/scartati e dettagli completi
```

Per chiusura v0.05 si consiglia `2` o `3`.

---

## 10. Relazione Con Movement Explainability

L'EL-MBD non sostituisce l'EL pathfinding.

Catena ideale:

```text
EL-MBD:
Decision: EatKnownFood ha vinto usando belief Food #8

EL Pathfinding:
Intent: ReachFood verso cella (12, 8), beliefBasis Food #8

EL Pathfinding:
Plan: LandmarkAstar scelto

EL Pathfinding:
Event: StepSuccess / Failed / DoorInteraction
```

Chiavi di correlazione:

- `npcId`
- `tick`
- `beliefId`
- `targetCell`
- `intentId`, quando disponibile

In v0.06 verranno aggiunti:

- `jobId`
- `stepId`

---

# Roadmap Di Integrazione

## v0.05.31 - EL_MBD_Roadmap

Obiettivo: creare il contratto tecnico dell'EL-MBD e definire la roadmap di integrazione.

Deliverable:

- questo documento;
- definizione record JSONL;
- sequenza branch da v0.05.31 in avanti.

Definition of Done:

- il progetto ha una roadmap EL-MBD locale;
- nessun file runtime modificato;
- nessun file `.meta` modificato.

---

## v0.05.32 - EL_MBD_Tipi_Dati_Config

Obiettivo: introdurre tipi dati e config minimi per EL-MBD senza agganci runtime.

File previsti:

- `Assets/Scripts/Core/World/MemoryBeliefDecisionExplainabilityTypes.cs`
- eventuale estensione config in `GameParams.cs`
- eventuale sezione config in `game_params.json`

Deliverable:

- record DTO per `memory`, `belief`, `query`, `decision`, `bridge`;
- config dedicata;
- nessuna emissione runtime ancora attiva.

Definition of Done:

- compila;
- nessun comportamento simulativo cambia;
- nessun accesso a `World.Objects` o `FoodStocks`.

---

## v0.05.33 - EL_MBD_JSONL_Sink

Obiettivo: introdurre il sink JSONL append-only.

File previsti:

- `Assets/Scripts/Core/Telemetry/Arcontiolog/MemoryBeliefDecisionJsonLogSink.cs`
- `Assets/Scripts/Editor/MemoryBeliefDecisionJsonLogQaTests.cs`

Deliverable:

- scrittura JSONL `arcontio_el_mbd_{yyyyMMdd_HHmmss}.jsonl`;
- enum esportati come stringhe;
- test EditMode sul formato dei record.

Definition of Done:

- i test verificano `kind`, `schema`, enum leggibili e campi chiave;
- il sink non interrompe la simulazione in caso di errore IO;
- nessun comportamento simulativo cambia.

---

## v0.05.34 - EL_MBD_BeliefQuery_Log

Obiettivo: agganciare il record `query` a `BeliefQueryService.QueryBest(...)`.

File previsti:

- `Assets/Scripts/Core/Beliefs/BeliefQueryService.cs`
- `Assets/Scripts/Core/Systems/Explainability/MemoryBeliefDecisionExplainabilityEmitter.cs`, se necessario

Deliverable:

- record query con goal, urgency, npc cell, winner, final score e contributions;
- logging disattivabile da config;
- nessuna rilettura del mondo.

Definition of Done:

- query con store vuoto produce `isEmpty = true`;
- query con winner produce breakdown;
- il Decision Layer continua a usare il QuerySystem come prima.

---

## v0.05.35 - EL_MBD_Decision_Bridge_Log

Obiettivo: agganciare record `decision` e `bridge` al ponte runtime v0.05.

File previsti:

- `Assets/Scripts/Core/Rules/Needs/NeedsDecisionRule.cs`
- eventuale emitter EL-MBD

Deliverable:

- record decision con candidati, score e selected intent;
- record bridge con command prodotto o fallback legacy;
- nessun cambio di scelta o command rispetto a prima.

Definition of Done:

- runtime test leggibile senza overlay;
- `EatKnownFood`, `SearchFood`, `RestKnownPlace` e fallback sono distinguibili;
- i candidati includono score breakdown quando verbosity >= 3.

---

## v0.05.36 - EL_MBD_Memory_Belief_Log

Obiettivo: agganciare record `memory` e `belief` alla parte MemoryStore/BeliefUpdater.

File previsti:

- `Assets/Scripts/Core/Systems/MemoryEncodingSystem.cs`
- `Assets/Scripts/Core/Beliefs/BeliefUpdater.cs`
- `Assets/Scripts/Core/Systems/Beliefs/BeliefDecaySystem.cs`, solo se necessario

Deliverable:

- trace memory inserite/droppate visibili;
- belief create/merge/invalidazioni visibili;
- decay belief visibile almeno come summary.

Definition of Done:

- nessun accesso globale aggiuntivo;
- BeliefStore resta passivo;
- BeliefUpdater resta lazy.

---

## v0.05.37 - EL_MBD_QA_Hardening

Obiettivo: consolidare test, documentazione e procedure di analisi.

File previsti:

- test EditMode EL-MBD;
- eventuale aggiornamento di questo documento;
- eventuale nota nella roadmap principale.

Deliverable:

- test JSONL completi;
- checklist runtime;
- istruzioni su dove trovare e come leggere i log.

Definition of Done:

- tutti i test EditMode passano;
- la v0.05 puo' essere chiusa con EL runtime non grafico;
- debiti v0.06 esplicitati.

---

## v0.06.x - EL_MBD_Job_Step_Correlation

Obiettivo futuro: collegare decisione, job, step, command e fallimento operativo.

Nuovi campi previsti:

- `jobId`
- `stepId`
- `sourceIntentId`
- `sourceBeliefId`
- `failureKind`
- `preemptionReason`

Definition of Done futura:

- ogni Job conosce l'intenzione che lo ha generato;
- ogni Step puo' registrare failure diagnostico;
- invalidazioni belief da job fallito sono tracciabili.
