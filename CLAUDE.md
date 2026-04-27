# CLAUDE.md — Arcontio

> Questo file viene letto automaticamente da Claude Code all'avvio di ogni sessione.
> Aggiornalo ogni volta che aggiungi un sistema nuovo o cambi un pattern architetturale.
> FONTE PRIMARIA: documentazione Notion. Questo file è derivato, non originale.

---

## 1. Cos'è il progetto

**Arcontio** è un simulatore sociale in Unity (C#) in cui:
- l'ambiente esercita pressione sugli NPC
- i bisogni producono instabilità
- la società emerge come risposta
- il giocatore gestisce le conseguenze

**Principio fondamentale (Vincolo Onniscienza):** gli NPC agiscono ESCLUSIVAMENTE
sulla propria **percezione soggettiva e memoria imperfetta**, mai sullo stato reale
del mondo. Nessun NPC legge mai lo stato globale del mondo direttamente.
Un NPC può andare verso un oggetto che non c'è più perché lo ricorda lì.

**Repository GitHub:** https://github.com/MasterOfArcontio/ARCONTIO
**Script Core:** Assets/Scripts/Core

---

## 2. Versione corrente

- **v0.03.03.b** — Edge Soggettivi da Percezione Visiva (design completato, implementazione prossima)
- **v0.03.03.a** — Landmark Perception (LandmarkPerceptionSystem) — implementato
- **v0.02** — Landmark Pathfinding (GVD-DIN) — implementato, sistema attivo
- **v0.01** — Pathfinding base — completato

**Stato sistemi principali:**
| Sistema | Stato |
|---|---|
| Movement / Pathfinding (GVD-DIN) | Completato |
| Decision Layer | ~70% — in sviluppo |
| NeedsSystem (NeedsDecisionRule) | Attivo, da sostituire progressivamente con Decision Layer |
| BeliefStore | Spec stabile, implementazione parziale |
| Job System | Prossimo milestone principale |
| Inventory System | Stub strutture dati — service layer rimandato |
| Body/Wound System v2 | Spec completata, non implementato |

---

## 3. Architettura — Separazione fondamentale

```
WORLD (stato oggettivo)          VIEW / UI (presentazione)
─────────────────────────        ──────────────────────────
Verità del simulatore            Rendering e debug overlay
Non sa nulla della UI            Non modifica mai il World
                    ↑
                    └── Le modifiche passano SOLO tramite CommandBuffer
```

**REGOLA CRITICA:** La View NON modifica mai direttamente il World.
Ogni modifica passa tramite `CommandBuffer` → Commands → World.

---

## 4. Pipeline NPC — overview completo

```
NpcDnaProfile (immutabile, seed)
    │
    ▼
Decision Layer  ←── BeliefStore (unica interfaccia memoria→decisione)
    │                   ↑
    │               MemoryStore (non interrogato direttamente dal Decision Layer)
    ▼
Job System  (JobRequest → Job → JobPlan → JobAction)
    │
    ▼
Step System  (step atomici, ~35 step)
    │
    ▼
Runtime Primitives
    │
    ▼
Commands → CommandBuffer
    │
    ▼
World Systems (applicano i comandi)
    │
    ▼
World Events (IWorldEvent)
    │
    ▼
Perception System → Memory System → BeliefStore
```

**Separazione obbligatoria tra ogni layer. Mai saltare livelli.**

---

## 5. Tick order (stabile)

1. `WorldSystems.ApplyCommands()`
2. `PerceptionSystem.Update()`
3. `MemorySystem.Decay()`
4. `NeedsSystem.Decay()`
5. `DecisionSystem.Evaluate()` — solo su trigger, MAI ogni tick
6. `JobSystem.Tick()`
7. `StepSystem.Execute()`
8. `EventBus.Flush()`

Commands applicati all'inizio del tick successivo.
Conflict resolution risorse: ordine di arrivo.
Priorità command incompatibili: emergenza > lavoro > normale.

---

## 6. Sistemi principali — descrizione

### 6.1 Commands (Assets/Scripts/Core/Commands/)

I comandi sono **intenti** applicabili al World.
- **Evento:** "è successo X" (notifica)
- **Comando:** "fai Y" (modifica il World)

File chiave:
- `Commands.cs` — definizioni di tutti i comandi
- `Commands/Movement/SetMoveIntentCommand.cs`
- `Commands/Needs/` — comandi bisogni (Eat, Sleep, Steal)
- `Commands/DevTools/` — comandi DevMode (non mutano il World direttamente)

**Pattern da rispettare sempre:**
Ogni nuovo comando deve seguire il pattern già in `Commands.cs`.
Non inventare strutture nuove senza leggere prima quel file.

---

### 6.2 Events (Assets/Scripts/Core/Events/)

- `Events/World/IWorldEvent.cs` — interfaccia base eventi mondo (marker, nessun metodo)
- `Events/IEvent.cs` — qualunque evento interno (debug, telemetria, low-level)
- `IWorldEvent` — solo eventi che possono creare memoria, comunicazione, reputazione

File evento esistenti:
- `Events/World/AttackEvent.cs`
- `Events/World/DeathEvent.cs`
- `Events/World/NpcSpottedEvent.cs`
- `Events/World/ObjectSpottedEvent.cs`
- `Events/World/PredatorSpottedEvent.cs`
- `Events/FoodMissingEvent.cs`
- `Events/FoodMissingSuspectedEvent.cs`
- `Events/FoodStolenEvent.cs`

**Regola:** usa `IWorldEvent` solo se l'evento può creare memoria, reputazione
o comunicazione. Altrimenti usa `IEvent`.

---

### 6.3 Messaging (Assets/Scripts/Core/Messaging/)

- `MessageBus.cs` — coda eventi interni al simulatore
- `Tokens/TokenBus.cs` — bus token comunicazione NPC
- `Tokens/TokenTypes.cs` — tipi token

I Systems **pubblicano** eventi. Le Rules (alto livello) **reagiscono** a eventi
e generano comandi. Separa calcolo (systems) da decisione (rules).

---

### 6.4 Memory System

File:
- `World/MemoryStore.cs` — store tracce memoria per NPC
- `World/MemoryTrace.cs` — struttura singola traccia
- `World/MemoryType.cs` — enum tipi di memoria
- `Systems/MemoryEncodingSystem.cs` — encoding eventi → tracce
- `Systems/MemoryDecaySystem.cs` — decadimento tracce
- `Systems/Memory/ObjectMemoryMaintenanceSystem.cs`
- `World/NpcObjectMemoryStore.cs` — store memoria oggetti per NPC
- `Rules/IMemoryRule.cs` — interfaccia regole di encoding
- `Rules/Memory/` — implementazioni IMemoryRule (Attack, Death, Food, NpcSpotted, ecc.)

**Pipeline:**
1. World produce `IWorldEvent` → MessageQueue
2. `MemoryEncodingSystem` — per ogni NPC esposto: valuta `IMemoryRule[]`, scrive su `IMemoryStore`
3. `MemoryDecaySystem` — decadimento + archiviazione

**IMemoryRule — firma formale:**
```csharp
bool Matches(IWorldEvent e)
MemoryTrace? Encode(NpcContext npc, IWorldEvent e, RelationshipView rel, PerceptionView perc)
```

**MemoryTrace — campi:**
```
MemoryType, SubjectId, ObjectId, Severity, Salience,
CurrentIntensity, DecayModel, Reliability (0..1),
Flags (DirectWitness, Heard, Inferred...)
```

---

### 6.5 BeliefStore

**REGOLA CRITICA:** Il Decision Layer consulta SOLO il BeliefStore.
MAI interrogare MemoryStore direttamente dal Decision Layer.

Il BeliefStore aggrega tracce MemoryStore in credenze sintetiche per NPC.
- Campi per entry: `category`, `confidence` (0–1 con decay), `estimatedPosition`,
  `lastUpdatedTick`, `sourceCount`
- Aggiornamento lazy: su nuova traccia, su decay sotto soglia, su job fallito
- `confidence` a zero → entry rimossa → NPC non sa più che quella cosa esiste
- DecayRate variabile per categoria: pericoli = lento, posizioni oggetti = rapido

---

### 6.6 Decision Layer

**Gira SOLO su trigger, MAI ogni tick.**

**Trigger di rivalutazione:**
1. Job completato o fallito
2. Need scende sotto `NeedCritical`
3. Evento percepito classificato emergenza
4. Intenzione corrente diventa impossibile

**Fasi:**
- **Fase 0:** gates hard (floor/esclusioni pre-scoring). Obligation oltre soglia = floor.
- **Fase 1:** candidati 3–8 intenzioni
- **Fase 2:** scoring softmax con temperatura T modulata da Impulsività
- **Fase 3:** selezione

**Formula scoring:**
```
Score(i) = NeedUrgency + CompetenceAffinity + PreferenceAffinity
         + ObligationPressure + MemoryConfidence - SocialRisk + CognitiveModulator

P(i) = exp(Score(i)/T) / Σexp(Score(j)/T)
```

**Need tra NeedAlert e NeedCritical:** intenzione pending accodata, non interrompe job corrente.
**Step mai interrotti** salvo emergenza. Preemption proporzionale all'avanzamento job.

**File da leggere prima di toccare il Decision Layer:**
- `Rules/Needs/NeedsDecisionRule.cs` — implementazione corrente da sostituire progressivamente
- `NPC/NpcDnaProfile.cs`
- `NPC/NpcProfile.cs`
- `World/MemoryStore.cs`

---

### 6.7 NPC — DNA e Profile

**File:**
- `NPC/NpcDnaProfile.cs` — struttura immutabile, seed iniziali e predisposizioni
- `NPC/NpcProfile.cs` — struttura runtime per NPC, variabile durante partita
- `NPC/NpcDnaDistance.cs` — calcolo distanza DNA↔Profile (misura stress/insoddisfazione)

**NpcDnaProfile (immutabile):**
```
Identity, Capacities, Preferences, Dispositions,
SocialPosition, ObligationFrame, Thresholds,
CognitiveModulators, Traits, Tags, ExtensionData
```
Nessun dato runtime. Nessuna decisione. Non modificare durante partita.

**NpcProfile (runtime):**
```
CompetenceProfile  (float 0–1 per dominio)
PreferenceProfile  (float 0–1 per dominio)
ObligationProfile  (float 0–1 per dominio)
AssignedRole       (nullable)
```

**ExtensionData:** NON implementare finché non esiste un caso d'uso concreto.

---

### 6.8 Needs System

**File:**
- `Needs/NeedKind.cs` — enum tipi di bisogno
- `Config/NeedsConfig.cs` — configurazione soglie bisogni
- `Config/NeedsConfigLoader.cs`
- `Systems/Needs/NeedsDecaySystem.cs` — decadimento continuo bisogni
- `Systems/Needs/FoodInventoryAuditSystem.cs`
- `Systems/Needs/PrivateFoodAuditSystem.cs`
- `Rules/Needs/NeedsDecisionRule.cs` — rule corrente (da sostituire con Decision Layer)

**Bisogni base:**
- Fisiologici: Fame, Sete, Riposo/Sonno, Salute fisica, Comfort termico
- Psicologici: Sicurezza percepita, Stabilità emotiva/stress, Socialità
- Sociali: Riconoscimento/Status, Appartenenza al gruppo

**Due soglie per ogni bisogno in `Thresholds`:**
- `NeedAlert` — finestra anticipazione, NeedUrgency cresce linearmente,
  BeliefStore consultato su trigger. Credenza valida → intenzione Forage.
  Credenza assente/debole → intenzione Search.
- `NeedCritical` — floor obbligatorio, sovrascrive ScheduleFrame.

**ATTENZIONE:** `NeedsDecisionRule` è il sistema legacy. Va rimosso progressivamente
man mano che il Decision Layer copre i relativi domini. Non aggiungere nuova logica
a `NeedsDecisionRule`.

---

### 6.9 Landmark Pathfinding (v0.02)

**Sistema attivo.** `HybridLandmarkExtractor` disabilitato, tenuto per usi futuri.

Gli NPC costruiscono una **mappa mentale compressa** tramite landmark.
Non una lista di tutte le celle, ma una rete di punti notevoli.

**File:**
- `Systems/Movement/MovementSystem.cs` — esecuzione movimento ogni tick
- `Systems/Movement/MovementPathfinder.cs` — navigazione locale (greedy, BFS/JPS)
- `Systems/Movement/LandmarkPathfinder.cs` — pianificazione A* su grafo soggettivo
- `World/PathfindingState.cs` — store stati esecutivi
- `World/LandmarkRegistry.cs` — registro globale landmark (oggettivo)
- `World/NpcLandmarkMemory.cs` — mappa soggettiva per NPC
- `World/NpcComplexEdgeMemory.cs` — memoria edge complessi per NPC
- `World/GvdDinComputer.cs` — algoritmo GVD-DIN
- `Systems/Landmarks/LandmarkPerceptionSystem.cs` — apprendimento visivo landmark
- `Systems/Landmarks/NpcLandmarkMemorySystem.cs`
- `Systems/Landmarks/HybridLandmarkExtractor.cs` — DISABILITATO

**Parametri configurabili in `game_params.json`:**
- `enableLandmarkSystem` (off di default)
- `maxLandmarksPerNpc`
- `maxEdgesPerNpc`

---

### 6.10 Edge Soggettivi da Percezione Visiva (v0.03.03.b — design completato)

**Stato:** design definito, implementazione non ancora avviata.

Due meccanismi distinti applicati in cascata in `LandmarkPerceptionSystem`:

**Meccanismo 1 — Simultaneità visiva (priorità):**
Se due landmark A e B visibili nello stesso tick → edge diretto con costo Manhattan(A,B).

**Meccanismo 2 — Ibrido fisico+visivo (fallback):**
Recording fisico attivo da A + B visibile nel FOV → edge provvisorio con costo
`StepCount + Manhattan(npc_pos, B.cell)`, reliability = `subjective_edge_base_reliability`.

**Reliability:**
| Tipo edge | Confidence iniziale |
|---|---|
| Fisico (camminato) | `0.25f` |
| Visivo simultaneo | `subjective_edge_base_reliability` (es. `0.15f`) |
| Ibrido fisico+visivo | `subjective_edge_base_reliability` |

**File da leggere prima di implementare:**
- `LandmarkPerceptionSystem.cs`
- `World.cs` → aggiungere `NotifyNpcSeenLandmarkPair`
- `NpcComplexEdgeMemory.cs` → `IsRecordingActive`, `StepCount`
- `NpcLandmarkMemory.cs` → `LearnEdge`
- `SimulationParams.cs` → `LandmarkPerceptionParams`

**Parametri da aggiungere a `landmark_perception` in `game_params.json`:**
```json
{
  "subjective_edges_enabled": true,
  "subjective_edge_max_dist": 8,
  "subjective_edge_base_reliability": 0.15
}
```

---

### 6.11 Perception System

**File:**
- `Systems/Perception/NpcPerceptionSystem.cs` — percezione NPC (FOV + LOS)
- `Systems/Perception/ObjectPerceptionSystem.cs` — percezione oggetti
- `Systems/Perception/IdleScanSystem.cs` — rotazione NPC idle (12 tick, 4 direzioni)
- `Systems/Perception/FovUtils.cs` — utility FOV/cono visivo
- `World/OcclusionMap.cs` — mappa occlusione

**Attenzione:** `LandmarkPerceptionSystem` usa `period=1` perché deve essere
coprimo con il ciclo di rotazione di `IdleScanSystem` (12 tick, 4 direzioni).

---

### 6.12 Token / Comunicazione NPC

**File:**
- `Systems/TokenEmissionPipeline.cs`
- `Systems/TokenDeliveryPipeline.cs`
- `Systems/TokenAssimilationPipeline.cs`
- `Messaging/Tokens/TokenBus.cs`
- `Messaging/Tokens/TokenTypes.cs`
- `Rules/Tokens/ITokenEmissionRule.cs`
- `Rules/Tokens/ITokenAssimilationRule.cs`
- `Rules/Tokens/HelpRequestEmissionRule.cs`
- `Rules/Tokens/PredatorAlertEmissionRule.cs`
- `Rules/Tokens/TokenAssimilationRules.cs`
- `World/DebugNpcTokenLog.cs`

---

### 6.13 Ownership System

**File:**
- `World/DomainKind.cs`

Due concetti separati:
- **Owner (proprietario):** chi rivendica legittimità — `OwnerKind = None | Npc | Group | Community`
- **Holder (detentore fisico):** chi la detiene fisicamente

`OwnershipClarity` (0..1): quanto è chiaro che appartiene a qualcuno.

---

### 6.14 Job System (prossimo milestone)

**Stato:** architettura stabile, implementazione da avviare.

**File esistenti:**
- `Systems/WorkSystem.cs` — sistema lavoro corrente (legacy)
- `Systems/SocialSystem.cs`

**Entità target:**
`JobRequest`, `Job`, `JobPlan`, `JobAction`, `NpcJobState`
(1 ActiveJob, N Suspended, N Queued), `JobArbiter`, `ReservationRecord`

**Step decomposition:** GOAP structure. Job → JobAction → Step.
**NON aggiungere logica a `WorkSystem.cs`.**
Il Job System sostituirà `WorkSystem` progressivamente.

---

### 6.15 DevTools / Runtime Developer Mode

**File:**
- `DevTools/DevMapData.cs`
- `DevTools/DevMapIO.cs`
- `Commands/DevTools/` — tutti i comandi dev

Attivazione: `F2` → Toggle Developer Mode.

**Regola:** la UI DevTools non modifica il World direttamente.
Le modifiche passano tramite `CommandBuffer`.

---

### 6.16 Save System

**File:**
- `Save/NpcSaveData.cs`
- `Save/NpcSaveSystem.cs`
- `Save/NpcScenarioLoader.cs`

**Struttura file JSON:**
- `world_state.json` — griglia, oggetti, stato porte, tick corrente
- `npcs_chunk_N.json` — gruppi di 50 NPC: profile + needs + job + memoryTraces
- `objects_state.json` — stato runtime oggetti modificabili (stock quantities, ecc.)

---

### 6.17 Telemetria / Logging

**File:**
- `Telemetry/Arcontiolog/ArcontioLogger.cs`
- `Telemetry/Arcontiolog/SimulationParams.cs` — parametri simulazione (leggi prima di modificare config)
- `Telemetry/Arcontiolog/GameParams.cs`
- `Telemetry/Arcontiolog/LogModels.cs`
- `Telemetry/Arcontiolog/LogSinks.cs`
- `Telemetry/Arcontiolog/HtmlFileSink.cs`
- `Telemetry/Arcontiolog/UnityOverlaySink.cs`
- `Telemetry/Arcontiolog/ArcontioLogOverlay.cs`
- `Telemetry/Arcontiolog/LocalizationDb.cs`
- `Telemetry/DebugFovTelemetry.cs`
- `Telemetry/Telemetry.cs`

---

## 7. Struttura cartelle Scripts (reale)

```
Assets/Scripts/Core/
  ├── Commands/
  │     ├── Commands.cs
  │     ├── Movement/
  │     │     └── SetMoveIntentCommand.cs
  │     ├── Needs/
  │     │     ├── EatFromStockCommand.cs
  │     │     ├── EatPrivateFoodCommand.cs
  │     │     ├── SleepInBedCommand.cs
  │     │     ├── StealFromStockCommand.cs
  │     │     └── StealPrivateFoodCommand.cs
  │     └── DevTools/
  ├── Components/
  │     ├── FoodStockComponent.cs
  │     ├── GroupComponents.cs
  │     ├── NPCComponents.cs
  │     ├── NpcActionState.cs
  │     ├── NpcBalloonSignal.cs
  │     ├── ObjectUseState.cs
  │     └── PersonalityMemoryParams.cs
  ├── Config/
  │     ├── NeedsConfig.cs
  │     └── NeedsConfigLoader.cs
  ├── DevTools/
  │     ├── DevMapData.cs
  │     └── DevMapIO.cs
  ├── Events/
  │     ├── IEvent.cs
  │     ├── FoodMissingEvent.cs
  │     ├── FoodMissingSuspectedEvent.cs
  │     ├── FoodStolenEvent.cs
  │     └── World/
  │           ├── IWorldEvent.cs
  │           ├── AttackEvent.cs
  │           ├── DeathEvent.cs
  │           ├── NpcSpottedEvent.cs
  │           ├── ObjectSpottedEvent.cs
  │           └── PredatorSpottedEvent.cs
  ├── Messaging/
  │     ├── MessageBus.cs
  │     └── Tokens/
  │           ├── TokenBus.cs
  │           └── TokenTypes.cs
  ├── NPC/
  │     ├── NpcDnaProfile.cs
  │     ├── NpcProfile.cs
  │     └── NpcDnaDistance.cs
  ├── Needs/
  │     └── NeedKind.cs
  ├── Rules/
  │     ├── IRule.cs
  │     ├── IMemoryRule.cs
  │     ├── CrisisRules.cs
  │     ├── DebugEventLogRule.cs
  │     ├── Memory/
  │     │     └── [tutte le IMemoryRule implementate]
  │     ├── Needs/
  │     │     └── NeedsDecisionRule.cs  ← LEGACY, non espandere
  │     └── Tokens/
  │           ├── ITokenEmissionRule.cs
  │           ├── ITokenAssimilationRule.cs
  │           ├── HelpRequestEmissionRule.cs
  │           ├── PredatorAlertEmissionRule.cs
  │           └── TokenAssimilationRules.cs
  ├── Runtime/
  │     ├── SimulationHost.cs
  │     ├── Tick.cs
  │     ├── TickContext.cs
  │     ├── DontDestroyOnLoad.cs
  │     └── ViewSwitcherInputActions.cs
  ├── Save/
  │     ├── NpcSaveData.cs
  │     ├── NpcSaveSystem.cs
  │     └── NpcScenarioLoader.cs
  ├── Scheduling/
  │     └── Scheduler.cs
  ├── Systems/
  │     ├── ISystem.cs
  │     ├── MemoryEncodingSystem.cs
  │     ├── MemoryDecaySystem.cs
  │     ├── SocialSystem.cs
  │     ├── TokenEmissionPipeline.cs
  │     ├── TokenDeliveryPipeline.cs
  │     ├── TokenAssimilationPipeline.cs
  │     ├── WorkSystem.cs              ← LEGACY, non espandere
  │     ├── Landmarks/
  │     │     ├── LandmarkPerceptionSystem.cs
  │     │     ├── NpcLandmarkMemorySystem.cs
  │     │     └── HybridLandmarkExtractor.cs  ← DISABILITATO
  │     ├── Memory/
  │     │     └── ObjectMemoryMaintenanceSystem.cs
  │     ├── Movement/
  │     │     ├── MovementSystem.cs
  │     │     ├── MovementPathfinder.cs
  │     │     └── LandmarkPathfinder.cs
  │     ├── Needs/
  │     │     ├── NeedsDecaySystem.cs
  │     │     ├── FoodInventoryAuditSystem.cs
  │     │     └── PrivateFoodAuditSystem.cs
  │     └── Perception/
  │           ├── NpcPerceptionSystem.cs
  │           ├── ObjectPerceptionSystem.cs
  │           ├── IdleScanSystem.cs
  │           └── FovUtils.cs
  ├── Telemetry/
  │     ├── Telemetry.cs
  │     ├── DebugFovTelemetry.cs
  │     └── Arcontiolog/
  │           └── [tutti i file logging]
  └── World/
        ├── World.cs                   ← file centrale, leggi sempre prima
        ├── CardinalDirection.cs
        ├── ComplexEdge.cs
        ├── DebugNpcTokenLog.cs
        ├── DomainKind.cs
        ├── FovMode.cs
        ├── GvdDinComputer.cs
        ├── LandmarkDebugTypes.cs
        ├── LandmarkRegistry.cs
        ├── MemoryStore.cs
        ├── MemoryTrace.cs
        ├── MemoryType.cs
        ├── MovementIntentTypes.cs
        ├── NpcComplexEdgeMemory.cs
        ├── NpcLandmarkMemory.cs
        ├── NpcObjectMemoryStore.cs
        ├── OcclusionMap.cs
        ├── PathfindingState.cs
        ├── SpatialQuantizer.cs
        ├── WorldObjectInstance.cs
        └── Objects/
              ├── ObjectDatabaseLoader.cs
              ├── ObjectDefDatabase.cs
              ├── ObjectProperties.cs
              └── ObjectTypes.cs
```

Scene Unity:
- `Scene_AtomViewer` — vista nodale di debug
- `Scene_Bootstrap`
- `Scene_MapGrid` — vista a griglia principale

---

## 8. Oggetti di gioco

- Letti da file JSON (`object_defs.json` via `Resources.Load<TextAsset>("Arcontio/Config/object_defs")`)
- Ogni `ObjectDef` contiene: `Id`, `DisplayName`, `SpriteKey/IconKey/VariantSpriteKeys`, `Properties`
- Istanziati nel mondo come `WorldObjectInstance` con `OwnerKind/OwnerId`
- La conoscenza dell'esistenza di un oggetto NON è globale: deriva da percezione → memoria (`ObjectSpotted`)

---

## 9. Convenzioni di codice

- **Commenti in italiano** — minimo 50% delle istruzioni dentro le funzioni commentate
- **Pattern Command** già definito in `Commands.cs` — non deviare mai
- **Mai modificare il World dalla View** — sempre tramite CommandBuffer
- Nuovi tipi di memoria → nuova `IMemoryRule` (no modifiche al core)
- Nuovi messaggi → nuovo `ISymbolizationRule` e/o `ITokenAssimilationRule`
- `IEvent` vs `IWorldEvent`: usa `IWorldEvent` solo se può creare memoria/reputazione/comunicazione
- **ExtensionData:** non implementare finché non esiste caso d'uso concreto e spec approvata
- **`NeedsDecisionRule` e `WorkSystem`:** sistemi legacy, non aggiungere logica

---

## 10. Regole architetturali non negoziabili

1. **Vincolo Onniscienza:** nessun NPC legge mai stato globale del mondo
2. **Intenzione ≠ Job ≠ Step** — non confondere i livelli
3. **Step ≠ System** — gli step non modificano il world direttamente
4. **DNA ≠ Runtime** — NpcDnaProfile non contiene mai stato variabile
5. **Decision Layer:** solo su trigger, mai ogni tick
6. **Step:** mai interrotti salvo emergenza
7. **CompetenceProfile:** non decade mai (solo Preference e Obligation soggetti al pull gravitazionale)
8. **BeliefStore:** unica interfaccia tra memoria e decisione

---

## 11. Task frequenti — file da leggere prima

| Task | File da leggere prima |
|---|---|
| Nuovo comando | `Commands.cs` |
| Nuovo evento world | `Events/World/IWorldEvent.cs`, `AttackEvent.cs` (esempio) |
| Nuovo tipo di memoria | `Rules/IMemoryRule.cs`, `World/MemoryTrace.cs` |
| Modifica pathfinding | `Systems/Movement/`, `World/PathfindingState.cs`, `game_params.json` |
| Modifica percezione landmark | `Systems/Landmarks/LandmarkPerceptionSystem.cs`, `World/World.cs` |
| Implementare v0.03.03.b | Sezione 6.10 di questo file |
| Modifica bisogni | `Needs/NeedKind.cs`, `Config/NeedsConfig.cs`, `Systems/Needs/NeedsDecaySystem.cs` |
| Modifica Decision Layer | `Rules/Needs/NeedsDecisionRule.cs`, `NPC/NpcDnaProfile.cs`, `NPC/NpcProfile.cs` |
| Job System (nuovo) | `Systems/WorkSystem.cs` (capire cosa sostituire), sezione 6.14 |
| Modifica config simulazione | `Telemetry/Arcontiolog/SimulationParams.cs`, `GameParams.cs` |
| Modifica DevTools | `Commands/DevTools/`, regola CommandBuffer |

---

## 12. Lacune architetturali aperte (NON implementare senza spec)

1. **Decision Layer:** modello weighting intenzioni non completamente definito
2. **Role→Intention bridge:** meccanismo bias non specificato
3. **Memory→Decision:** BeliefStore query interface non completamente risolta
4. **CognitiveModulators:** spec completa pendente
5. **ExtensionData:** spec pendente

---

*Ultimo aggiornamento: aprile 2026 — versione progetto 0.03.03.b (design)*
