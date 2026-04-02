# CLAUDE.md — Arcontio (faster-than-rim-mvp)

> Questo file viene letto automaticamente da Claude Code all'avvio di ogni sessione.
> Aggiornalo ogni volta che aggiungi un sistema nuovo o cambi un pattern architetturale.

---

## 1. Cos'è il progetto

**Arcontio** è un simulatore sociale in Unity (C#) in cui:
- l'ambiente esercita pressione sugli NPC
- i bisogni producono instabilità
- la società emerge come risposta
- il giocatore gestisce le conseguenze

**Principio fondamentale:** gli NPC agiscono sulla propria **memoria soggettiva**,
non sullo stato reale del mondo. Un NPC può andare verso un oggetto che non c'è più
perché lo ricorda lì.

**Repository GitHub:** https://github.com/MasterOfArcontio/faster-than-rim-mvp
**Script Core:** Assets/Scripts/Core

---

## 2. Versione corrente

- **v0.02** — Landmark Pathfinding (in sviluppo)
- **v0.01** — Pathfinding base completato

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

## 4. Pipeline principale

```
WORLD
  ├── PHYSIO/NEEDS SYSTEMS (NeedsDecay, HealthRegen, ComfortCompute)
  ├── ENVIRONMENT SYSTEMS (Temp/Wet, Weather, Exposure)
  ├── MOVEMENT/PATH SYSTEMS (Pathfinding, Flee/Chase)
  ├── INTERACTION SYSTEMS (Harvest, Carry, Deposit, Take, Combat)
  └── AI / DECISION PIPE (Goal selection, Tasks, ActionIntents)
         │
         ▼
    EVENT BUS / MESSAGE QUEUE (IWorldEvent)
         │
         ├── MEMORY PIPE
         │     └── MemoryEncodingSystem → IMemoryRule[] → IMemoryStore
         │         MemoryDecaySystem
         │
         └── OTHER CONSUMERS (Reputazione, Comunicazione, ecc.)
```

---

## 5. Sistemi principali — descrizione

### 5.1 Commands (Assets/Scripts/Core/Commands/)

I comandi sono **intenti** applicabili al World.
- **Evento:** "è successo X" (notifica)
- **Comando:** "fai Y" (modifica il World)

File chiave:
- `Commands.cs` — definizioni dei comandi
- `DevTools/` — comandi DevMode (non mutano il World direttamente)

**Pattern da rispettare sempre:**
Ogni nuovo comando deve seguire il pattern già in `Commands.cs`.
Non inventare strutture nuove senza leggere prima quel file.

### 5.2 Events (Assets/Scripts/Core/Events/)

- `IWorldEvent.cs` — interfaccia base eventi mondo (marker, nessun metodo)
- `ISimEvent` — qualunque evento interno (debug, telemetria, low-level)
- `IWorldEvent` — solo eventi che possono creare memoria, comunicazione, reputazione

File evento esistenti:
- `AttackEvent.cs`
- `DeathEvent.cs`
- `NpcSpottedEvent.cs` — observer ha visto observedNpc in cella (x,y)
- `ObjectSpottedEvent.cs`
- `PredatorSpotted.cs`

### 5.3 Messaging (Assets/Scripts/Core/Messaging/)

- `MessageBus.cs` — coda di eventi interni al simulatore
  - I Systems **pubblicano** eventi (es. "NpcStarving", "LawBroken")
  - Le Rules (alto livello) **reagiscono** a eventi e generano comandi
  - Separa calcolo (systems) da decisione/plot (rules)

### 5.4 Memory System

Pipeline completa:
1. `World` produce `IWorldEvent` → `MessageQueue`
2. `MemoryEncodingSystem` — per ogni NPC esposto: valuta `IMemoryRule[]`, scrive su `IMemoryStore`
3. `MemoryDecaySystem` — decadimento + archiviazione
4. `CommunicationEmissionSystem` — seleziona tracce "attive", simbolizza con `ISymbolizationRule[]`
5. `CommunicationReceptionSystem` — applica `ITokenAssimilationRule[]`, produce rumor/memorie indotte

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

### 5.5 Landmark Pathfinding (v0.02 — in sviluppo)

Gli NPC costruiscono una **mappa mentale compressa** del mondo tramite landmark.
Non una lista di tutte le celle, ma una rete di punti notevoli.

Strati architetturali:
- **World Layer** → `LandmarkRegistry`, `LandmarkGraph` globale (oggettivo)
- **NPC Layer** → `NpcLandmarkMemory`, `PoiAnchorStore` (soggettivo)

Flow quando un Job richiede "Raggiungi deposito nord":
1. Converte "deposito nord" in `LandmarkId`
2. Consulta `NpcLandmarkMemory`
3. Se percorso noto → pianificazione su grafo
4. Se percorso ignoto → esplorazione incrementale

Parametri configurabili in `game_params.json`:
- `enableLandmarkSystem` (off di default)
- `maxLandmarksPerNpc`
- `maxEdgesPerNpc`

### 5.6 Sistema di Incarichi Strutturati (Jobs)

I job sono composti da step: "Vai lì", "Prenota", "Prendi", "Usa", "Riprova", "Aspetta".
Il Landmark Pathfinding è il motore GoTo dei job.

### 5.7 Ownership System

Due concetti separati:
- **Owner (proprietario):** chi rivendica legittimità — `OwnerKind = None | Npc | Group | Community`
- **Holder (detentore fisico):** chi la detiene fisicamente — `HeldByNpcId?` o `InCellId`

`OwnershipClarity` (0..1): quanto è chiaro che appartiene a qualcuno.
- Alto = deposito comunitario, contenitori
- Basso = risorsa a terra in natura

### 5.8 DevTools / Runtime Developer Mode

Attivazione: `F2` → Toggle Developer Mode

Quando attiva:
- compare overlay di editing
- input gameplay sospesi
- UI mostra palette oggetti e strumenti

Regola architetturale DevTools: la UI DevTools non modifica il World direttamente.
Le modifiche passano tramite `CommandBuffer`.

Salvataggio mappa: formato JSON
```json
{
  "width": 64, "height": 64,
  "objects": [{ "id": "wall_stone", "x": 10, "y": 10 }],
  "npcs": [{ "template": "citizen_basic", "x": 20, "y": 20, "dir": "E" }]
}
```

---

## 6. Struttura cartelle Scripts

```
Assets/Scripts/Core/
  ├── Commands/
  │     ├── Commands.cs
  │     └── DevTools/          ← comandi dev mode
  ├── Events/
  │     ├── IWorldEvent.cs
  │     ├── AttackEvent.cs
  │     ├── DeathEvent.cs
  │     ├── NpcSpottedEvent.cs
  │     ├── ObjectSpottedEvent.cs
  │     └── PredatorSpotted.cs
  ├── Messaging/
  │     └── MessageBus.cs
  └── NPC/                     ← gestione NPC e memoria
```

Scene Unity:
- `Scene_AtomViewer` — vista nodale di debug
- `Scene_Bootstrap`
- `Scene_MapGrid` — vista a griglia principale

---

## 7. Oggetti di gioco

- Letti da file JSON (`object_defs.json` via `Resources.Load<TextAsset>("Arcontio/Config/object_defs")`)
- Ogni `ObjectDef` contiene: `Id`, `DisplayName`, `SpriteKey/IconKey`, `Properties`
- Istanziati nel mondo come `WorldObjectInstance` con `OwnerKind/OwnerId`

---

## 8. Bisogni primari NPC

Variabili continue, individuali e degradabili nel tempo:
- **Hunger** — valore a 1 → morte NPC
- **Sleep** — (presumibilmente simile)
- **Comfort** — derivato da altri bisogni

---

## 9. Convenzioni di codice

- **Commenti in italiano**
- **Pattern Command** già definito in `Commands.cs` — non deviare mai
- **Mai modificare il World dalla View** — sempre tramite CommandBuffer
- Nuovi tipi di memoria → nuova `IMemoryRule` (no modifiche al core)
- Nuovi messaggi → nuovo `ISymbolizationRule` e/o `ITokenAssimilationRule`
- `ISimEvent` vs `IWorldEvent`: usa `IWorldEvent` solo se può creare memoria/reputazione/comunicazione

---

## 10. Come lavorare su questo progetto

### Prima di modificare qualcosa:
1. Leggi i file coinvolti nel task
2. Verifica il pattern già usato in file simili
3. Non inventare strutture nuove senza prima controllare se esistono già

### Quando aggiungi un nuovo sistema:
- Segui la separazione World / View
- Aggiungi il nuovo sistema a questo file CLAUDE.md nella sezione appropriata

### Task frequenti e file da leggere prima:
| Task | File da leggere prima |
|------|-----------------------|
| Nuovo comando | `Commands.cs` |
| Nuovo evento world | `IWorldEvent.cs`, `AttackEvent.cs` (esempio) |
| Nuovo tipo di memoria | `IMemoryRule`, `MemoryTrace` |
| Modifica pathfinding | File in `Core/Pathfinding/`, `game_params.json` |
| Modifica DevTools | `DevTools/` commands, regola CommandBuffer |

---

*Ultimo aggiornamento: marzo 2026 — versione progetto 0.02*
