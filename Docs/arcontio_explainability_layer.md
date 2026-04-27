# ARCONTIO – Explainability Layer Specification

## 1. Definizione

L’**Explainability Layer** è uno strato architetturale dedicato che cattura, struttura ed espone l’intera catena causale dietro le decisioni degli NPC.

Il suo scopo è rispondere in modo deterministico alla domanda:

> “Perché questo NPC ha fatto X in questo momento?”

Funziona come ponte tra:
- Perception / Belief Layer
- Decision Layer
- Job Execution Layer

---

## 2. Problema

Senza explainability:
- Le azioni sono visibili ma le motivazioni no
- Mancano:
  - alternative scartate
  - percezioni usate
  - pesi decisionali
  - vincoli

---

## 3. Concetto base

Le decisioni sono rappresentate come **strutture dati**, non log testuali.

### DecisionTrace

```
DecisionTrace
- npcId
- tick
- selectedIntention
- candidates[]
- constraints[]
- selectedBecause
```

### Candidate

```
Candidate
- intentionId
- score
- baseScore
- modifiers[]
- validity
- invalidReason
```

### Modifier

```
Modifier
- name
- value
- source
```

---

## 4. Integrazione pipeline

Perception → BeliefStore → Decision → Job → Step → Command

### PerceptionTrace

```
PerceptionTrace
- source
- confidence
- timestamp
```

### BeliefSnapshot

```
BeliefSnapshot
- knownEntities
- knownLocations
- ownedResources
```

### IntentionEvaluation

```
IntentionEvaluation
- intentionId
- baseScore
- modifiers[]
- totalScore
- validity
```

### Selection

```
Selection
- selectedIntention
- selectionRule
- rejectedAlternatives[]
```

### ExecutionLink

```
ExecutionLink
- jobId
- stepSequence[]
```

---

## 5. Tipologie output

### Human-readable
Spiegazione sintetica per UI.

### Structured
Dati completi interrogabili.

### Timeline
Sequenza temporale decisioni.

---

## 6. Differenze con logging

- Strutturato vs stringhe
- Queryable vs statico
- Contestuale vs piatto

---

## 7. Vincoli architetturali

### NON deve:
- Essere logging sparso
- Stare nei Systems o Step

### Deve:
- Essere centralizzato
- Essere data-oriented
- Essere interrogabile

---

## 8. Ruolo nei layer

- Perception: dati osservati
- Belief: stato soggettivo
- Decision: genera trace
- Job: esecuzione

---

## 9. Obiettivi

- Debug avanzato
- Validazione comportamento
- Supporto gameplay
- Narrazione emergente

---

## 10. Implementazione minima

1. DecisionTrace base
2. BeliefSnapshot
3. UI debug
4. Timeline

---

## 11. Insight

Explainability non è debug accessorio.

È un sistema strutturale che rende le decisioni:
- ricostruibili
- analizzabili
- comprensibili

---

## 12. Sintesi

Explainability Layer:
- cattura causalità
- struttura dati
- collega percezione → decisione → azione
