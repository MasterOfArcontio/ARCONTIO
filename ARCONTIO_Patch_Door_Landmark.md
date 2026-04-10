# ARCONTIO — Patch List: Door System + Landmark Improvements

Documento tecnico per implementazione. Elenca tutte le modifiche necessarie in ordine di dipendenza.
Ogni patch è indipendente dalla successiva salvo dove indicato esplicitamente.

---

## PATCH 1 — Struttura dati porta in ObjectDef e WorldObjectInstance

### Prerequisiti
Nessuno. Prima patch da eseguire — tutto il resto dipende da questa.

### File modificati

**`Scripts/Core/World/Objects/ObjectDefDatabase.cs`**
Aggiungere i seguenti campi a `ObjectDef`:
```csharp
public bool IsDoor;       // true = questo oggetto è una porta
public bool IsLockable;   // true = supporta il lock (valido solo se IsDoor=true)
public string KeyId;      // DefId dell'oggetto chiave richiesto (valido solo se IsLockable=true)
```
 
**`Scripts/Core/World/WorldObjectInstance.cs`**
Aggiungere i seguenti campi a `WorldObjectInstance`:
```csharp
public bool IsOpen;    // stato attuale della porta (true = aperta, false = chiusa)
public bool IsLocked;  // questa istanza è bloccata (true = richiede chiave per aprire)
```
Note:
- `IsOpen` e `IsLocked` sono stato runtime per-istanza — due porte dello stesso tipo possono avere stati diversi.
- `KeyId` sta in `ObjectDef` (non in `WorldObjectInstance`) perché è proprietà del tipo, non dell'istanza. La stessa chiave funziona su tutte le istanze dello stesso tipo di porta.
- `IsOpen` default = `false` (porta chiusa alla creazione).
- `IsLocked` default = `false` (porta non bloccata alla creazione).

**`Resources/Arcontio/Config/object_defs.json`**
Aggiornare la definizione `door_wood_good`:
```json
{
  "Id": "door_wood_good",
  "DisplayName": "Door Wood Good",
  "SpriteKey": "MapGrid/Sprites/Objects/door_wood_good",
  "IsOccluder": true,
  "IsInteractable": true,
  "IsDoor": true,
  "IsLockable": false,
  "BlocksVision": true,
  "BlocksMovement": true,
  "VisionCost": 1.0,
  "MaxHp": 50,
  "Hardness": 0.4
}
```
Aggiungere una nuova definizione per porta bloccabile (esempio):
```json
{
  "Id": "door_wood_locked",
  "DisplayName": "Door Wood Locked",
  "SpriteKey": "MapGrid/Sprites/Objects/door_wood_good",
  "IsOccluder": true,
  "IsInteractable": true,
  "IsDoor": true,
  "IsLockable": true,
  "KeyId": "key_door_wood_01",
  "BlocksVision": true,
  "BlocksMovement": true,
  "VisionCost": 1.0,
  "MaxHp": 50,
  "Hardness": 0.4
}
```

**`Scripts/Core/Save/NpcSaveData.cs`** (o file save equivalente per gli oggetti)
Aggiungere serializzazione di `IsOpen` e `IsLocked` per `WorldObjectInstance` nel save/load.

---

## PATCH 2 — OcclusionMap dinamica per porte

### Prerequisiti
PATCH 1 completata.

### Descrizione
Attualmente la OcclusionMap è una cache statica aggiornata solo alla creazione/distruzione di oggetti. Con le porte che cambiano stato (`IsOpen`), la cache deve aggiornarsi dinamicamente quando una porta viene aperta o chiusa.

### File modificati

**`Scripts/Core/World/World.cs`**
Aggiungere il metodo:
```csharp
public void SetDoorOpen(int objectId, bool isOpen)
```
Il metodo deve:
1. Verificare che l'oggetto esista in `world.Objects` e che il suo `ObjectDef` abbia `IsDoor = true`. Se no, loggare errore e uscire.
2. Aggiornare `WorldObjectInstance.IsOpen = isOpen`.
3. Aggiornare la OcclusionMap per la cella della porta:
   - Se `isOpen = true`: la cella smette di bloccare movimento e visione (`BlocksMovement = false`, `BlocksVision = false` nella cache).
   - Se `isOpen = false`: la cella torna a bloccare movimento e visione (`BlocksMovement = true`, `BlocksVision = true` nella cache).
4. NON aggiornare il LandmarkRegistry — le porte sono sempre muri per il calcolo landmark indipendentemente da `IsOpen`.

Note architetturali:
- `SetDoorOpen` è l'unico punto autorizzato a modificare lo stato porta. Nessun sistema deve scrivere `WorldObjectInstance.IsOpen` direttamente.
- La OcclusionMap post-modifica deve essere immediatamente coerente: sistemi che girano nello stesso tick dopo la chiamata devono vedere la cella aggiornata.

---

## PATCH 3 — OpenDoorCommand

### Prerequisiti
PATCH 1 e PATCH 2 completate.

### Descrizione
Command atomico che apre una porta. Prodotto dal MovementSystem quando un NPC deve attraversare una porta chiusa non lockata.

### File nuovi

**`Scripts/Core/Commands/Movement/OpenDoorCommand.cs`**
Implementare `ICommand` con:
- Campo `NpcId` — l'NPC che apre la porta.
- Campo `ObjectId` — l'objectId della porta da aprire.
- In `Execute(World world, MessageBus bus)`:
  1. Recuperare l'oggetto da `world.Objects`. Se non esiste, uscire silenziosamente.
  2. Recuperare l'`ObjectDef` tramite `world.ObjectDefs[instance.DefId]`. Se `IsDoor = false`, uscire con errore.
  3. Se `instance.IsLocked = true`:
     - **NOTA FUTURA**: quando il sistema inventario sarà implementato, verificare qui se l'NPC ha la chiave (`ObjectDef.KeyId` contro inventario NPC). Per ora: se locked, il command fallisce silenziosamente senza aprire.
  4. Se `instance.IsLocked = false`: chiamare `world.SetDoorOpen(ObjectId, true)`.
  5. Pubblicare un evento `DoorOpenedEvent` sul bus (utile per memory encoding futuro — altri NPC nelle vicinanze possono sentire la porta aprirsi).

### File modificati

**`Scripts/Core/Commands/Commands.cs`** (o file di registrazione comandi)
Registrare `OpenDoorCommand`.

---

## PATCH 4 — Riconoscimento porta nel MovementSystem

### Prerequisiti
PATCH 1, PATCH 2, PATCH 3 completate.

### Descrizione
Il MovementSystem attualmente tratta una porta chiusa come un muro invalicabile — `BlockedTicks` cresce fino all'abort. Con questa patch, quando l'NPC trova una porta chiusa sul percorso, emette `OpenDoorCommand` invece di incrementare `BlockedTicks`.

### File modificati

**`Scripts/Core/Systems/Movement/MovementSystem.cs`**
Nel metodo `TryMoveTo` (o equivalente che gestisce il fallimento del passo fisico), aggiungere la seguente logica **prima** di incrementare `BlockedTicks`:

1. Calcolare la cella che l'NPC sta cercando di raggiungere.
2. Chiamare `world.GetObjectAt(cellX, cellY)` per verificare se c'è un oggetto su quella cella.
3. Se esiste un oggetto, recuperare il suo `ObjectDef` e verificare `IsDoor = true`.
4. Se è una porta:
   - Se `IsOpen = true`: la cella dovrebbe essere libera — caso anomalo, loggare warning e procedere normalmente.
   - Se `IsOpen = false` e `IsLocked = false`: emettere `OpenDoorCommand(npcId, objectId)`. NON incrementare `BlockedTicks`. Il movimento avverrà al tick successivo quando la porta sarà aperta.
   - Se `IsOpen = false` e `IsLocked = true`: trattare come muro. Incrementare `BlockedTicks` normalmente. **NOTA FUTURA**: quando il sistema inventario sarà implementato, verificare qui la chiave prima di decidere se bloccare o aprire.
5. Se non è una porta: comportamento attuale invariato.

Note architetturali:
- Il tick di apertura della porta conta come tick di movimento speso — `BlockedTicks` non deve crescere.
- Un NPC non rimane bloccato davanti a una porta aperta: il tick successivo `TryMoveTo` trova la cella libera e avanza normalmente.
- La porta rimane aperta indefinitamente dopo l'apertura. La logica di richiusura automatica (se necessaria) è una feature futura separata.

---

## PATCH 5 — Porte come Landmark nel LandmarkRegistry

### Prerequisiti
PATCH 1 completata. Indipendente da PATCH 2, 3, 4.

### Descrizione
`LandmarkKind.Doorway` esiste nell'enum ma non viene mai assegnato — il vecchio sistema Doorway/Junction è stato rimosso in v0.03.02.a senza sostituzione. Questa patch ripristina il riconoscimento delle porte come landmark oggettivi nel registry globale.

Le porte sono landmark indipendentemente da GVD-DIN o HybridLandmarkExtractor — vengono aggiunte come passo separato in `RebuildFromWorld`.

Le porte sono sempre trattate come landmark con `LandmarkKind.Doorway` indipendentemente dallo stato `IsOpen`. Una porta aperta o chiusa è comunque un separatore semantico dello spazio.

### File modificati

**`Scripts/Core/World/LandmarkRegistry.cs`**
In `RebuildFromWorld`, dopo i branch GVD-DIN / HybridLandmarkExtractor esistenti, aggiungere un terzo passo comune a entrambi:

```csharp
// PASSO COMUNE — Porte come Doorway landmark
// Indipendente dal sistema di estrazione scelto.
// Le porte sono sempre separatori semantici dello spazio.
foreach (var kv in world.Objects)
{
    var instance = kv.Value;
    if (!world.ObjectDefs.TryGetValue(instance.DefId, out var def)) continue;
    if (!def.IsDoor) continue;
    AddOrMergeNode(instance.CellX, instance.CellY, LandmarkKind.Doorway, mergeRadius);
}
```

Note architetturali:
- `LandmarkKind.Doorway` ha già priorità massima nell'eviction policy (non viene rimosso se ci sono slot liberi).
- Il merge radius per i Doorway deve essere piccolo (1-2 celle) per non assorbire landmark vicini di tipo diverso.
- Una porta a 2 celle di larghezza produce due Doorway adiacenti che vengono mergiati in uno solo dal merge radius.

---

## PATCH 6 — Door-as-separator in HybridLandmarkExtractor (Tarjan removal)

### Prerequisiti
PATCH 1 e PATCH 5 completate.

### Descrizione
Il Passo 2 di HybridLandmarkExtractor (bridge detection con Tarjan su grafo contratto) viene eliminato. Le porte, già presenti nel LandmarkRegistry come Doorway (PATCH 5), forniscono direttamente i separatori semantici tra regioni. Il flood fill usa le posizioni delle porte come celle non attraversabili per separare le regioni.

Regola di design confermata: ogni separazione tra aree è sempre una porta esplicita. Aperture senza oggetto porta = spazio continuo = stessa regione.

### File modificati

**`Scripts/Core/Systems/Landmarks/HybridLandmarkExtractor.cs`**

Nel metodo `Extract(bool[,] walkable, int width, int height, HybridLandmarkParams p)`:

1. **Prima del Passo 1 (DT)**: costruire una griglia `bool[,] walkableForDt` copia di `walkable`, dove le celle porta vengono settate a `false` (trattate come muri). Le posizioni delle porte vengono lette da `world.Objects` filtrando per `IsDoor = true`.
   - Nota: `Extract` deve ricevere in input anche la lista delle posizioni delle porte, oppure accettare direttamente `World world` invece della griglia raw. Preferire il passaggio esplicito delle posizioni porta per mantenere l'extractor testabile in isolamento.

2. **Passo 1**: eseguire la DT su `walkableForDt` (non su `walkable`).

3. **Eliminare il Passo 2 completo** (metodo `DetectBridges`, tutto il codice Tarjan, grafo contratto). Eliminare i campi `_lastBridge`.

4. **Passo 3 — Flood Fill**: eseguire su `walkableForDt`. Le porte come muri separano naturalmente le regioni. Ogni stanza diventa una regione distinta.

5. **Passo 4 e 5**: invariati. I ChokePoint non vengono più estratti geometricamente — le porte sono già nel registry come Doorway dal passo comune in LandmarkRegistry (PATCH 5). Eliminare `ExtractChokePointCandidates`.

6. **Passo 6 (Pruning)**: invariato.

**`Resources/Arcontio/Config/game_params.json`**
Dopo aver verificato il funzionamento:
```json
"hybrid_landmark": {
    "use_hybrid_extractor": true,
    ...
}
```
GVD-DIN rimane nel codice come fallback ma viene disattivato.

---

## PATCH 7 — Waypoint Intermedi per spazi aperti

### Prerequisiti
PATCH 6 completata e HybridLandmarkExtractor attivo.

### Descrizione
Nuovo passo nella pipeline HybridLandmarkExtractor, eseguito dopo il pruning finale (Passo 6). Risolve l'assenza di landmark in aree walkable grandi dove GVD-DIN non produceva nodi.

### Logica

Per ogni coppia di landmark candidati nel risultato post-pruning con distanza superiore a `waypointMinDistance` (parametro config, valore suggerito = `npcVisionRangeCells` = 17):

1. **Walk discreto libero**: verificare che tutte le celle sulla linea retta tra i due landmark siano walkable nella griglia originale (non `walkableForDt`). Se una cella non è walkable: saltare questa coppia.

2. **DT minimo sul candidato**: il punto intermedio candidato (punto medio della linea) deve avere un valore DT superiore a `waypointMinDt` (parametro config). Se il DT è troppo basso, il punto è in un corridoio stretto o vicino a un muro — non è uno spazio aperto.

3. Se entrambe le condizioni sono soddisfatte: inserire un landmark `LandmarkType.RoomCenter` nel punto medio. Se la distanza è molto grande (> 2x `waypointMinDistance`), inserire più waypoint intermedi a intervalli regolari di `waypointMinDistance` celle.

### Parametri config da aggiungere in `game_params.json`

```json
"hybrid_landmark": {
    "use_hybrid_extractor": true,
    "dt_open_threshold": 3,
    "merge_radius": 2.5,
    "min_region_area": 4,
    "median_tolerance": 1,
    "waypoint_min_distance": 17,
    "waypoint_min_dt": 3
}
```

Note architetturali:
- `waypoint_min_distance` default = `npcVisionRangeCells`: soglia semantica — se l'NPC non vede il landmark successivo da quello corrente, ha bisogno di un waypoint intermedio.
- `waypoint_min_dt` default = `dt_open_threshold`: coerente con la distinzione zona aperta/corridoio già usata nel resto della pipeline.
- I waypoint intermedi hanno tipo `LandmarkType.RoomCenter` — bassa priorità di eviction, corretti per spazi aperti.

---

## FEATURE FUTURA — Inventario NPC e gestione chiavi

Non implementare ora. Dipendenze esplicite già annotate in PATCH 3 e PATCH 4.

Quando il sistema inventario sarà implementato:
- PATCH 3 (`OpenDoorCommand`): aggiungere verifica `KeyId` contro inventario NPC prima di aprire porta lockata.
- PATCH 4 (`MovementSystem`): aggiungere verifica chiave prima di trattare porta lockata come muro invalicabile.
- La chiave è un `WorldObjectInstance` con `DefId` corrispondente al `KeyId` della porta. Può stare a terra sulla griglia o nell'inventario di un NPC.

---

## Riepilogo dipendenze

```
PATCH 1 (struttura dati)
    ├── PATCH 2 (OcclusionMap dinamica)
    │       └── PATCH 3 (OpenDoorCommand)
    │               └── PATCH 4 (MovementSystem)
    └── PATCH 5 (porte come LM)
            └── PATCH 6 (door-as-separator, Tarjan removal)
                    └── PATCH 7 (waypoint intermedi)
```

PATCH 2-3-4 e PATCH 5-6-7 sono due catene indipendenti che partono entrambe da PATCH 1.
Possono essere sviluppate in parallelo o in sequenza.
