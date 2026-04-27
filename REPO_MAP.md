# REPO_MAP.md — Mappa di Navigazione Repository ARCONTIO

## Root Core

`Assets/Scripts/Core` contiene il core della simulazione. Trattarlo come fonte primaria della verità per la logica gameplay.

## Aree Core Principali

### Commands
`Assets/Scripts/Core/Commands`

Operazioni action-like applicate al mondo.

Sottoaree:
- `Commands/Movement`
- `Commands/Needs`
- `Commands/DevTools`

### Components
`Assets/Scripts/Core/Components`

Componenti dati per NPC, gruppi, oggetti, memoria, comunicazione, percezione e altre entità di simulazione.

### Config
`Assets/Scripts/Core/Config`

Reader di configurazione e strutture config tipizzate.

### Events
`Assets/Scripts/Core/Events`

Definizioni eventi di simulazione.

Sottoarea:
- `Events/World` per facts di mondo world-level.

### Messaging
`Assets/Scripts/Core/Messaging`

Logica interna message/event bus.

Sottoarea:
- `Messaging/Tokens` per infrastruttura token simbolici di comunicazione NPC.

### NPC
`Assets/Scripts/Core/NPC`

Dati, helper di comportamento o strutture dominio specifiche NPC.

### Needs
`Assets/Scripts/Core/Needs`

Modelli o logica di supporto legata ai bisogni.

### Rules
`Assets/Scripts/Core/Rules`

Logica rule-driven. Preferire rules additive rispetto a evaluator centrali monolitici.

Sottoaree:
- `Rules/Memory`
- `Rules/Needs`
- `Rules/Tokens`

### Runtime
`Assets/Scripts/Core/Runtime`

Simulation host runtime, entry point legati al tick o execution glue.

### Save
`Assets/Scripts/Core/Save`

Logica di persistenza/save.

### Scheduling
`Assets/Scripts/Core/Scheduling`

Scheduler e ordine deterministico di esecuzione systems.

### Systems
`Assets/Scripts/Core/Systems`

Sistemi granulari di simulazione.

Sottoaree:
- `Systems/Crime`
- `Systems/Landmarks`
- `Systems/Memory`
- `Systems/Movement`
- `Systems/Needs`
- `Systems/Perception`

### Telemetry
`Assets/Scripts/Core/Telemetry`

Debug counters, traces e osservabilità non invasiva.

Sottoarea:
- `Telemetry/Arcontiolog` per logging custom Arcontio.

### World
`Assets/Scripts/Core/World`

Strutture canoniche del mondo e modelli di stato.

Sottoarea:
- `World/Objects`

## Regola di navigazione agente

Prima di patchare un sistema, ispezionare:

1. la sua cartella,
2. `Rules` adiacenti,
3. `Events` correlati,
4. `Commands` correlati,
5. `Components` correlati,
6. `Systems` correlati,
7. implicazioni runtime/scheduler.

Non assumere che i nomi cartella siano prova architetturale completa. Confermare il comportamento dal codice.
