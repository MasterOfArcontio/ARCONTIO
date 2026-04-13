# EL Pathfinding Istruzioni

## Scopo del documento

Questo documento spiega cosa aspettarsi nel pannello runtime **EL Pathfinding** e come leggere le informazioni mostrate nelle sezioni **Intent**, **Plan** ed **Events**.

L'EL Pathfinding e' un livello di osservabilita' diagnostica: non decide il pathfinding, non modifica il comportamento dell'NPC e non sostituisce i sistemi simulativi. Mostra e, se configurato, esporta tracce gia' prodotte dal movimento.

## Struttura del pannello runtime

Il pannello laterale destro e' organizzato in tre blocchi verticali:

```text
[HEADER]
[INTENT / PLAN]
[EVENTS]
```

### Header

L'header e' il blocco superiore del pannello. Dovrebbe contenere una sintesi compatta simile a questa:

```text
EL Pathfinding - NPC #id
Tick = ...   Intent = ...   Plan = ...
```

Campi:

| Campo | Significato |
|---|---|
| `NPC #id` | NPC attualmente selezionato nella MapGrid. |
| `Tick` | Tick simulativo piu' recente tra intent, plan ed eventi EL disponibili. |
| `Intent` | Id diagnostico dell'ultimo intent EL noto per quell'NPC. |
| `Plan` | Id diagnostico dell'ultimo plan EL noto per quell'NPC. |

L'header serve a capire rapidamente **chi** stai osservando e **quale snapshot EL** e' attivo.

## Pannello Intent / Plan

Il pannello **Intent / Plan** sta sotto l'header e raccoglie le informazioni di alto livello: perche' l'NPC vuole muoversi e quale modalita' di pathfinding e' stata scelta.

### Sezione Intent

Formato previsto:

```text
[INTENT]
Purpose = ...
Target = ...
Belief = ...
Urgency = ...   Verbosity = ...
```

Campi:

| Campo | Significato |
|---|---|
| `Purpose` | Scopo normalizzato del movimento. Esempio: raggiungere cibo, acqua, letto, workstation, movimento debug. |
| `Target` | Destinazione dell'intent. Puo' essere una cella, un oggetto mondo o altro tipo target supportato dall'EL. |
| `Belief` | Snapshot della belief causale, se presente. Non e' una lettura globale live: e' una copia diagnostica passiva. |
| `Urgency` | Urgenza normalizzata dell'intent, tra `0.00` e `1.00`. |
| `Verbosity` | Livello di dettaglio EL attivo quando la trace e' stata emessa. |

Se non esiste una trace intent, il pannello mostra:

```text
[INTENT] nessuna trace
```

### Sezione Plan

Formato previsto:

```text
[PLAN]
Mode = ...   Why = ...
Route = ...
FirstStep = ...   Verbosity = ...
Candidates:
- ...
- ...
- ...
```

Campi:

| Campo | Significato |
|---|---|
| `Mode` | Modalita' scelta dal planner iniziale. Vedi sezione **Plan Mode**. |
| `Why` | Ragione sintetica per cui quella modalita' e' stata scelta. Vedi sezione **Plan Why**. |
| `Route` | Riassunto diagnostico della route: cella start, cella goal, eventuale conteggio landmark e costo macro-route. |
| `FirstStep` | Primo passo locale noto del piano, se disponibile. |
| `Verbosity` | Livello EL attivo quando la trace plan e' stata emessa. |
| `Candidates` | Modalita' valutate dal planner, con stato valido/scartato, costo stimato e nota diagnostica. |

Se non esiste una trace plan, il pannello mostra:

```text
[PLAN] nessuna trace
```

## Plan Mode

`Plan Mode` viene dall'enum `PlannerMode`. Descrive la **modalita' scelta nella pianificazione iniziale**, non i fallback runtime successivi.

| Stringa | Significato |
|---|---|
| `Unknown` | Planner non classificato o trace incompleta. Valore difensivo. |
| `Direct` | Il planner ha scelto un path diretto verso il target. In pratica, il percorso diretto era praticabile. |
| `LandmarkAstar` | Il planner ha scelto A* su grafo landmark soggettivo dell'NPC. In pratica, il direct non era sufficiente/valido e il sistema ha usato landmark intermedi. |
| `DirectFallback` | La macro-route landmark non era disponibile o non e' diventata attiva, quindi il sistema usa un fallback direct o un prefix direct parziale. |

Esempio:

```text
Mode = LandmarkAstar
```

Lettura: il piano iniziale ha scelto la navigazione via landmark.

## Plan Why

`Plan Why` viene dall'enum `SelectionReason`. Descrive **perche'** e' stato scelto quel `Plan Mode`.

| Stringa | Significato |
|---|---|
| `Unknown` | Ragione non popolata o trace incompleta. |
| `DirectValid` | Il direct path era praticabile, quindi e' stato scelto `Direct`. |
| `DirectInvalidLmChosen` | Il direct non era praticabile, ma la macro-route landmark era disponibile, quindi e' stato scelto `LandmarkAstar`. |
| `NoLmFallbackDirect` | La route landmark non era disponibile o non e' diventata attiva, quindi si e' usato un fallback direct/parziale. |
| `NoKnownLandmarks` | L'NPC non conosce landmark utili. Nel flusso attuale puo' comparire soprattutto come ragione diagnostica nei candidati scartati. |
| `LmPlanFailed` | A* landmark ha fallito nonostante ci fossero dati disponibili. Nel flusso attuale puo' comparire soprattutto nei candidati o nella diagnostica. |
| `ForcedDebug` | Scelta imposta da DevTools o flusso debug. E' definita nel contratto dati; puo' non comparire nel flusso runtime ordinario. |

Esempio:

```text
Mode = LandmarkAstar   Why = DirectInvalidLmChosen
```

Lettura: il planner ha scelto landmark A* perche' il direct non era valido ma la macro-route era utilizzabile.

## Candidates nel Plan

I `Candidates` sono le alternative che il planner ha valutato durante la pianificazione iniziale.

Formato tipico:

```text
Candidates:
- Direct | scartato: PathBlocked | costo n/d | direct_not_selected
- LandmarkAstar | valido | costo 4.00
- DirectFallback | valido | costo 7.00 | macro_unavailable_direct_prefix
```

Ogni riga contiene:

| Parte | Significato |
|---|---|
| Modalita' | Modalita' candidata, per esempio `Direct`, `LandmarkAstar`, `DirectFallback`. |
| Stato | `valido` se il candidato era praticabile, oppure `scartato: <InvalidReason>` se non lo era. |
| Costo | Stima diagnostica del costo/percorso per quel candidato. |
| Nota | Nota breve prodotta dall'emitter, utile per capire quale ramo del codice ha generato il candidato. |

### Cosa significa il costo nei candidates

Il `costo` nei candidates e' un valore **diagnostico stimato**, non una nuova decisione simulativa.

Nel codice attuale:

| Caso | Significato del costo |
|---|---|
| `Direct` | Di norma e' il numero di step del path locale/direct, calcolato come lunghezza del path meno 1. |
| `LandmarkAstar` | Di norma e' il numero di passaggi tra nodi landmark, calcolato come numero nodi landmark meno 1. |
| `DirectFallback` | Di norma e' il numero di step del prefix/fallback direct, calcolato come lunghezza del path meno 1. |
| `costo n/d` | Il costo non e' disponibile o non e' applicabile. Internamente corrisponde a un costo stimato negativo, per esempio `-1`. |

Importante: il costo serve per leggere la trace. Non viene riletto dalla simulazione per decidere il movimento.

## Pannello Events

Il pannello **Events** mostra la timeline runtime dell'esecuzione del movimento.

Formato previsto:

```text
[EVENTS]
- t2145 StepSuccess  mode=DIRECT_COMMIT  (12, 8) -> (15, 8)  | step_committed

- t2144 Blocked  mode=GOAL_LOCAL_SEARCH  (11, 8) -> (15, 8)  | blocked_by_occupant
  detail: ...
```

Campi:

| Campo | Significato |
|---|---|
| `t...` | Tick dell'evento. |
| `EventType` | Tipo evento, per esempio `Started`, `Blocked`, `Arrived`, `Failed`. |
| `mode=...` | Modalita' runtime attiva al momento dell'evento. Vedi sezione **Events Mode**. |
| `(x, y) -> (x, y)` | Cella corrente dell'NPC verso cella target dell'evento. |
| testo dopo `|` | Sommario breve dell'evento. |
| `detail:` | Dettaglio aggiuntivo, presente soprattutto per fallimenti e porte. |

Regole di presentazione:

| Regola | Effetto |
|---|---|
| Ordine | Gli eventi sono mostrati dal piu' recente in alto al piu' vecchio in basso. |
| Separazione | Tra eventi consecutivi c'e' una riga vuota. |
| Colore normale | Gli eventi normali alternano bianco e grigio scuro. |
| Colore errore | Errori e fallimenti sono mostrati in rosso. |
| Limite | Il pannello runtime richiede fino a 32 eventi recenti. |

Se non ci sono eventi:

```text
[EVENTS]
(nessun evento runtime)
```

## Events Mode

`Events mode` viene dallo stato runtime attivo del movimento, non dall'enum `PlannerMode`.

Questo e' importante: `Plan Mode` descrive la scelta iniziale del planner; `Events mode` descrive cosa stava facendo davvero l'esecuzione in quel momento.

| Stringa | Significato |
|---|---|
| `GOAL_LOCAL_SEARCH` | E' attivo il fallback/ricerca locale verso il goal. E' runtime, non candidato iniziale del planner. |
| `DIRECT_COMMIT` | E' attivo un path diretto committed, cioe' un percorso diretto preparato ed eseguito. |
| `DIRECT_APPROACHING` | L'NPC sta usando un avvicinamento diretto o prefix diretto verso target/fase finale. |
| `APPROACHING_LM` | L'NPC sta andando verso un landmark intermedio. |
| `LM_PATH` | L'NPC sta seguendo il path landmark dopo l'avvio del percorso landmark. |
| `LAST_MILE` | L'NPC e' nella fase finale verso il target dopo i landmark oppure in fallback greedy finale. |
| `Modo non noto` | Nessuno stato runtime attivo ha fornito una modalita' valida. E' il fallback del ViewModel quando `ActiveMode` e' vuoto. |

Esempio:

```text
Mode = LandmarkAstar   Why = DirectInvalidLmChosen
```

e poi negli eventi:

```text
- t2145 ReachedWaypoint  mode=APPROACHING_LM  ...
- t2148 SwitchedMode     mode=LAST_MILE       ...
- t2150 StepSuccess      mode=DIRECT_COMMIT   ...
```

Lettura: il piano iniziale e' nato come landmark A*, ma durante l'esecuzione l'NPC puo' attraversare piu' modalita' runtime.

## EventType principali

Il campo evento vicino al tick puo' assumere i valori dell'enum `PathEventType`:

| Stringa | Significato |
|---|---|
| `Unknown` | Evento non classificato. |
| `Started` | Esecuzione iniziata dopo la pianificazione. |
| `StepSuccess` | Singolo passo riuscito; di solito richiede verbosita' alta. |
| `ReachedWaypoint` | Waypoint landmark raggiunto. |
| `SwitchedMode` | Cambio di modalita' di navigazione. |
| `LocalSearchActivated` | Fallback local search attivato. |
| `Replanned` | Piano locale o replan ricostruito. |
| `Blocked` | Blocco o transizione di blocco significativa. |
| `BackOffStarted` | La failure ladder ha avviato uno stage di back-off. |
| `BackOffExpired` | Back-off scaduto; replan da tentare o gia' tentato. |
| `Arrived` | Target raggiunto. |
| `Failed` | Intent cancellato con dettaglio fallimento. |
| `DoorInteraction` | Porta valutata o aperta dal movimento. |

## Log JSONL

La sessione JSON Log ha introdotto un sink append-only. Il log resta disattivato se in `game_params.json`:

```json
"writeJsonLog": false
```

Quando sara' impostato a `true`, il sink scrivera' righe JSONL sotto:

```text
Application.persistentDataPath/Arcontio_EL_Pathfinding/
```

Il file usa il pattern:

```text
arcontio_el_pathfinding_{yyyyMMdd_HHmmss}.jsonl
```

Ogni riga JSONL contiene un envelope comune (`schema`, `kind`, `npcId`, `tick`, `intentId`, `planId`) e una payload principale tra `intent`, `plan` o `executionEvent`.
