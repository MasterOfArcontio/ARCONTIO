# REPO_MAP.md — ARCONTIO Repository Navigation Map

## Core root

`Assets/Scripts/Core` contains the simulation core. Treat it as the primary source of truth for gameplay logic.

## Main Core Areas

### Commands
`Assets/Scripts/Core/Commands`

Action-like operations applied to the world.

Subareas:
- `Commands/Movement`
- `Commands/Needs`
- `Commands/DevTools`

### Components
`Assets/Scripts/Core/Components`

Data components for NPCs, groups, objects, memory, communication, perception and other simulation entities.

### Config
`Assets/Scripts/Core/Config`

Configuration readers and typed config structures.

### Events
`Assets/Scripts/Core/Events`

Simulation event definitions.

Subarea:
- `Events/World` for world-level event facts.

### Messaging
`Assets/Scripts/Core/Messaging`

Internal message/event bus logic.

Subarea:
- `Messaging/Tokens` for symbolic NPC communication token infrastructure.

### NPC
`Assets/Scripts/Core/NPC`

NPC-specific data, behavior helpers or domain structures.

### Needs
`Assets/Scripts/Core/Needs`

Need-related models or support logic.

### Rules
`Assets/Scripts/Core/Rules`

Rule-driven logic. Prefer additive rules over central monolithic evaluators.

Subareas:
- `Rules/Memory`
- `Rules/Needs`
- `Rules/Tokens`

### Runtime
`Assets/Scripts/Core/Runtime`

Runtime simulation host, tick-related entry points or execution glue.

### Save
`Assets/Scripts/Core/Save`

Persistence/save-related logic.

### Scheduling
`Assets/Scripts/Core/Scheduling`

Scheduler and deterministic system execution ordering.

### Systems
`Assets/Scripts/Core/Systems`

Granular simulation systems.

Subareas:
- `Systems/Crime`
- `Systems/Landmarks`
- `Systems/Memory`
- `Systems/Movement`
- `Systems/Needs`
- `Systems/Perception`

### Telemetry
`Assets/Scripts/Core/Telemetry`

Debug counters, traces and non-invasive observability.

Subarea:
- `Telemetry/Arcontiolog` for Arcontio custom logging.

### World
`Assets/Scripts/Core/World`

Canonical world structures and state models.

Subarea:
- `World/Objects`

## Agent navigation rule

Before patching a system, inspect:

1. its folder,
2. adjacent `Rules`,
3. related `Events`,
4. related `Commands`,
5. related `Components`,
6. related `Systems`,
7. runtime/scheduler implications.

Do not assume folder names are complete architectural proof. Confirm behavior from code.