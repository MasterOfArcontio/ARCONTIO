# EL Pathfinding - Contratto e Mapping

Sessione: v0.04.1.a-EL_Pathfinding_Contratto_Mapping
Base di analisi: v0.04.19-QA_BeliefStore_MemoryStore_Omniscience
Data: 2026-04-13

## Scopo

Questo documento fissa il contratto operativo per integrare l'Explainability Layer
del pathfinding in ARCONTIO senza modificare la semantica della simulazione.

L'EL pathfinding deve rispondere a tre domande:

- Perche' l'NPC vuole raggiungere una cella, un oggetto o una destinazione.
- Perche' il movimento ha scelto una modalita' di navigazione specifica.
- Perche' durante l'esecuzione il movimento ha cambiato piano, si e' bloccato
  o ha fallito.

Il documento non implementa l'EL. Serve come base per le sessioni successive.

## Regole di sviluppo

- Non lavorare mai su main.
- Ogni sessione parte creando un branch dedicato.
- Il formato branch e':
  `v0.04.1.<lettera>-<Titolo_Breve_Con_Underscore>`.
- La sessione corrente usa:
  `v0.04.1.a-EL_Pathfinding_Contratto_Mapping`.
- Non modificare file `.meta`, `Library`, `Temp`, `Obj`.
- Prima analisi e piano, poi modifica solo dopo conferma.
- Al termine di una sessione di sviluppo: commit e push del branch dedicato.

## Stato reale del codice

Il modulo EL pathfinding non esiste ancora come codice.

Esistono invece i sistemi a cui l'EL potra' agganciarsi:

- `MovementSystem.cs`: orchestratore del tick di movimento.
- `MovementPathfinder.cs`: navigazione locale, direct path e local search.
- `LandmarkPathfinder.cs`: pianificazione A* su grafo landmark soggettivo.
- `PathfindingState.cs`: store esecutivo per macro route, direct commit,
  local search, back-off e failure learning.
- `LandmarkDebugTypes.cs`: stati/report debug per macro-route e navigazione.
- `MovementIntentTypes.cs`: `MoveIntent`, `MoveIntentReason`,
  `NpcMoveBackOffState`.
- `BeliefQueryService.cs` e `BeliefQueryTypes.cs`: QuerySystem belief MVP
  con `BeliefQueryResult` e contributi di scoring.
- `SimulationParams.cs` e `game_params.json`: punto naturale per la config
  data-driven.

Il codice ha gia' logging e debug overlay, ma non trace strutturate EL.

## Vincoli architetturali

L'EL deve essere un osservatore passivo.

Regole:

- I sistemi NPC non devono leggere l'EL.
- Il decision layer non deve leggere l'EL.
- Il job execution layer non deve leggere l'EL.
- Il pathfinding non deve dipendere dall'EL per scegliere path o fallback.
- Le API EL devono essere one-way: emettono dati e non restituiscono input
  comportamentale.
- Il registry/store EL puo' essere letto da UI, debug tooling e log/export.

Il punto di emissione preferito e' `MovementSystem`, non il corpo degli algoritmi
di `MovementPathfinder` o `LandmarkPathfinder`.

## Mapping modalita' reali

Il documento originale cita alcune modalita'. Il codice reale usa stringhe e stati
debug/esecutivi gia' presenti.

Mapping proposto:

| Stato reale | Significato EL | Note |
| --- | --- | --- |
| `DIRECT_APPROACHING` | Direct / Direct approaching | Target acquisito o prefix direct attivo. |
| `APPROACHING_LM` | Avvicinamento al primo landmark | Tratto iniziale verso macro-route. |
| `LM_PATH` | Macro-route landmark attiva | Spostamento tra nodi landmark. |
| `LAST_MILE` | Ultimo tratto verso target finale | Puo' convertirsi in direct percettivo. |
| `GOAL_LOCAL_SEARCH` | Local search runtime | Fallback runtime, non candidato iniziale di planning. |
| `DIRECT_COMMIT` | Path diretto committed | Stato esecutivo interno/direct commit. |
| `IDLE` | Nessuna navigazione attiva | Stato utile solo per UI/debug. |

Per l'EL pubblico si puo' usare un enum stabile, mantenendo la stringa reale come
campo diagnostico opzionale.

## Mapping eventi EL

Eventi minimi consigliati:

| Evento EL | Punto reale | Verbosity minima |
| --- | --- | --- |
| `Started` | Primo tick di un intent attivo dopo inizializzazione | 1 |
| `Arrived` | NPC raggiunge il target effettivo/finale | 1 |
| `SwitchedMode` | Cambio di `NavigationMode` | 1 |
| `ReachedWaypoint` | Avanzamento nodo macro-route | 2 |
| `LocalSearchActivated` | Greedy/direct non produce passo e viene provata local search | 2 |
| `Replanned` | Local search/back-off produce un nuovo piano operativo | 2 |
| `Blocked` | `BlockedTicks` passa da 0 a > 0 o blocco significativo | 2 |
| `BackOffStarted` | Failure ladder avvia back-off invece di cancellare subito | 1 |
| `BackOffExpired` | Back-off scade e forza replan | 2 |
| `Failed` | Intent cancellato dopo esaurimento stage o target invalidato | 1 |
| `DoorInteraction` | Apertura o tentativo di porta | 1 |
| `StepSuccess` | Singolo passo riuscito | 3 |

Nota importante: `GOAL_LOCAL_SEARCH` non va trattato come candidato iniziale del
planner. E' un fallback runtime.

## Failure ladder

La specifica originale parla di fallimento quando `BlockedTicks` supera una soglia.
Il codice reale e' piu' ricco: usa una failure ladder con back-off e replan.

Implicazioni per l'EL:

- Non emettere subito `Failed` al primo superamento soglia.
- Emettere `BackOffStarted` quando parte uno stage di back-off.
- Emettere `BackOffExpired` o `Replanned` quando lo stage scade e viene tentato
  un nuovo `InitializeNavigation`.
- Emettere `Failed` solo quando lo stage massimo viene superato o quando il target
  diventa invalido.

Il `FailureDetail` deve includere almeno:

- `failureType`
- `blockedTicks`
- `lastActiveMode`
- eventuale `blockingCell`
- eventuale `blockingNpcId`
- eventuale `backOffStage`
- eventuale `oscillationFlag`

## Trace dati

Trace previste:

- `MovementIntentTrace`
- `PathPlanTrace`
- `PathExecutionEvent`

Strutture ausiliarie:

- `PlannerCandidate`
- `FailureDetail`
- `DoorInteractionDetail`
- `BeliefEntryRef`

Per la prima implementazione, `BeliefEntryRef` puo' restare nullable.

## Intent ID e Plan ID

Il codice attuale non contiene `intentId` e `planId` dentro `MoveIntent`.

Opzioni:

1. Derivare `intentId` da `npcId`, tick, target e reason.
2. Aggiungere `intentId` a `MoveIntent`.
3. Tenere una mappa EL parallela `npcId -> currentIntentId`.

Raccomandazione iniziale:

- Per Fase 1 usare una mappa EL parallela per ridurre invasivita'.
- Valutare un campo `intentId` in `MoveIntent` solo se diventa necessario per
  correlazione robusta tra job, decision e execution.

## Belief basis e QuerySystem

Nel branch base esiste gia' `BeliefQueryService`, con `BeliefQueryResult` e
contributi di scoring. Questo rende fattibile una futura integrazione causale.

Regola:

- L'EL non deve interrogare il BeliefStore per "scoprire" la causa a posteriori.
- Il decision layer, quando usa un `BeliefQueryResult`, puo' passare uno snapshot
  minimale verso il movimento.
- Lo snapshot non deve essere una reference live a `BeliefEntry`.

Campi minimi di `BeliefEntryRef`:

- `category`
- `beliefId` o `entityId`
- `confidence`
- `freshness`
- `ageTicks`

## Configurazione prevista

Sezione proposta in `game_params.json`:

```json
"explainability": {
  "enabled": false,
  "defaultVerbosity": 0,
  "maxTrackedNpcs": 3,
  "trackedNpcIds": [],
  "trackActiveNpcOnly": true,
  "ringBuffer_intent": 10,
  "ringBuffer_plan": 10,
  "ringBuffer_events_low": 60,
  "ringBuffer_events_high": 200,
  "verbosityHighThreshold": 3,
  "writeJsonLog": false,
  "jsonLogFileNamePattern": "arcontio_el_pathfinding_{yyyyMMdd_HHmmss}.jsonl"
}
```

Regole:

- `enabled=false` di default.
- `defaultVerbosity=0` di default.
- `trackedNpcIds` ha priorita' su `maxTrackedNpcs` se valorizzato.
- `writeJsonLog` e' un sink separato, non la fonte primaria dell'EL.

## Log JSON

Il formato consigliato e' JSONL, una trace per riga.

Vantaggi:

- append semplice durante runtime;
- robusto se Unity si chiude in modo imprevisto;
- filtrabile per `npcId`, `intentId`, `planId`, `eventType`;
- adatto ad analisi post-mortem.

La fonte primaria resta lo store EL in memoria; il file e' un export/sink.

## UI runtime

Il mockup `arcontio_el_pathfinding_panel.html` viene assunto come riferimento
grafico, non come file da integrare direttamente.

Decisione:

- Implementare in Unity UI/UGUI.
- Pannello laterale destro ancorato allo schermo.
- Larghezza indicativa: 320 px.
- Header NPC con nome, ruolo/id, cella e tick.
- Tab: `Intent`, `Piano`, `Esecuzione`.
- Footer fisso con verbosity `off`, `1`, `2`, `3`.
- Scroll verticale per il contenuto.
- La UI legge un `MovementExplainabilityViewModel`, non le trace raw.

Appunti dal mockup:

- LocalSearch non va mostrata come candidato planner selezionabile.
- LocalSearch puo' essere indicata come fallback runtime.
- Le stringhe lunghe devono usare ellissi o wrapping controllato.
- La palette scura va armonizzata con la UI MapGrid esistente.
- Il mockup contiene possibili problemi di encoding; usare testo ASCII o verificare
  l'encoding prima di copiare label testuali.

## Sessioni successive

1. `v0.04.1.b-EL_Pathfinding_Tipi_Dati`
   - Tipi dati EL passivi.
   - Enum e struct/classi trace.

2. `v0.04.1.c-EL_Pathfinding_Ring_Buffer_Registry`
   - Store per-NPC.
   - Ring buffer.
   - Registry passivo.

3. `v0.04.1.d-EL_Pathfinding_Config`
   - Parametri in `SimulationParams`.
   - Sezione `game_params.json`.

4. `v0.04.1.e-EL_Pathfinding_Intent_Plan_Trace`
   - Emissione `MovementIntentTrace`.
   - Emissione `PathPlanTrace`.

5. `v0.04.1.f-EL_Pathfinding_Execution_Events`
   - Eventi runtime discreti.
   - Failure ladder.
   - Porte.

6. `v0.04.1.g-EL_Pathfinding_BeliefRef_Query`
   - Snapshot belief.
   - Collegamento con `BeliefQueryResult`.

7. `v0.04.1.h-EL_Pathfinding_ViewModel_Runtime`
   - ViewModel per UI.
   - Adapter trace -> UI data.

8. `v0.04.1.i-EL_Pathfinding_UI_Panel`
   - Pannello laterale destro runtime.
   - Tab e verbosity UI.

9. `v0.04.1.j-EL_Pathfinding_JSON_Log`
   - Sink JSONL.
   - Export diagnostico.

10. `v0.04.1.k-EL_Pathfinding_QA_Hardening`
    - Verifiche, regressioni, overhead, vincolo onniscienza.

## Criterio di completamento della sessione A

La sessione A e' completata quando:

- il contratto e' versionato nel branch dedicato;
- non sono stati modificati file `.meta`;
- non e' stato toccato codice runtime;
- il branch e' pushato sul repository remoto.
