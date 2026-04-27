# ARCONTIO EL Memory/Belief/Query/Decision - Runtime QA

Versione di riferimento: v0.05.37

## Scopo

Questo documento serve a chiudere la prima integrazione runtime dell'Explainability
Layer per il ciclo:

MemoryTrace -> BeliefStore -> BeliefQueryService -> Decision Layer -> Bridge legacy

L'obiettivo non e' avere ancora una UI grafica. In questa fase il controllo avviene
tramite file JSONL strutturato, generato solo se la configurazione lo abilita.

## File prodotto

Quando il log e' attivo, il sink scrive in:

`Application.persistentDataPath/Arcontio_EL_MBD/`

Il nome di default del file e':

`arcontio_el_mbd_{yyyyMMdd_HHmmss}.jsonl`

Ogni riga e' un record JSON autonomo. Non e' un JSON array: e' un formato JSONL,
quindi il file va letto riga per riga.

## Configurazione da attivare

Nel file `Assets/Resources/Arcontio/Config/game_params.json`, la sezione consigliata
per il controllo runtime e':

```json
"memory_belief_decision_explainability": {
  "enabled": true,
  "defaultVerbosity": 3,
  "maxTrackedNpcs": 5,
  "trackedNpcIds": [],
  "trackActiveNpcOnly": false,
  "writeJsonLog": true,
  "jsonLogFileNamePattern": "arcontio_el_mbd_runtime.jsonl",
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

Nota operativa: se il file contiene gia' questa sezione, non duplicarla. Modifica solo
i campi necessari.

## Sequenza attesa

Durante una situazione semplice, per esempio un NPC vede cibo, lo aggrega come belief
e poi decide di muoversi/mangiare, nel JSONL dovresti vedere record con questi `kind`:

1. `memory`
2. `belief`
3. `query`
4. `decision`
5. `bridge`

La sequenza puo' non essere perfettamente consecutiva perche' altri NPC o altri eventi
possono intercalare record. Per questo vanno filtrati `npcId` e `tick`.

## Cosa controllare nei record memory

Un record `memory` deve contenere:

- `traceType`: per esempio `ObjectSpotted`, `NpcSpotted`, `PredatorSpotted`.
- `subjectId`: id del soggetto percepito o sentito.
- `subjectDefId`: importante per oggetti come cibo o letto.
- `cellText`: cella associata alla memoria.
- `intensity01` e `reliability01`: qualita' soggettiva della traccia.
- `storeResult`: `Inserted`, `Reinforced`, `Replaced` o `Dropped`.

Regola di controllo:

- se `storeResult` e' `Dropped`, non aspettarti necessariamente un record `belief`
  collegato a quella stessa trace.
- se `storeResult` e' `Inserted`, `Reinforced` o `Replaced`, e la trace e'
  aggregabile, dovrebbe seguire un record `belief`.

## Cosa controllare nei record belief

Un record `belief` deve contenere:

- `operation`: `Created` o `Merged` nella prima integrazione runtime.
- `sourceTraceType`: il tipo della MemoryTrace che ha alimentato il belief.
- `belief.category`: per esempio `Food`, `Rest`, `Danger`, `Social`.
- `belief.status`: normalmente `Active` dopo aggregazione.
- `belief.estimatedCellText`: cella soggettiva stimata.
- `reason`: nome della regola di aggregazione, per esempio `ObjectBeliefAggregationRule`.

Regola di controllo:

- il belief non deve contenere informazioni ricostruite leggendo oggetti globali.
- per il cibo o il letto, la categoria deriva dal `subjectDefId` gia' presente nella
  memoria.

## Cosa controllare nei record query

Un record `query` deve contenere:

- `goalType`: categoria richiesta dal Decision Layer, per esempio `Food`.
- `npcCellText`: posizione dell'NPC usata per la distanza.
- `candidateCount`: belief della categoria richiesta.
- `usableCandidateCount`: belief davvero usabili dopo status e min confidence.
- `winner`: belief vincitore, se `isEmpty` e' false.
- `contributions`: breakdown degli evaluator, per esempio `ConfidenceScore`,
  `FreshnessScore`, `DistancePenalty`.

Regola di controllo:

- se `isEmpty` e' true, controlla `emptyReason`.
- la query non deve avere target preso dal World: deve emergere dal `winner` del
  BeliefStore soggettivo.

## Cosa controllare nei record decision

Un record `decision` deve contenere:

- `auditValid`: deve essere true nei casi normali.
- `candidateCount`: numero di candidati valutati.
- `selectedIntent`: intenzione scelta, per esempio `EatKnownFood`.
- `selectedScore`: score finale dell'intenzione vincente.
- `selectedIndex`: indice del candidato nella lista originale.
- `selectionTopN`, `selectionNoise01`, `impulsivity01`, `effectiveNoise01`.
- `candidates`: lista dei candidati se `includeCandidates` e' true.
- `scoreContributions`: breakdown decisionale se `includeScoreBreakdown` e' true.

Regola di controllo:

- il candidato che vince deve avere score coerente con i suoi contributi.
- se un candidato richiede un belief target, controlla che `beliefResultEmpty` sia
  false e che `belief` sia compilato.

## Cosa controllare nei record bridge

Un record `bridge` deve contenere:

- `selectedIntent`: intenzione ricevuta dal ponte.
- `commandName`: command prodotto, oppure stringa vuota se non esiste ancora un job.
- `handled`: true se il bridge ha prodotto una gestione effettiva.
- `didMove`: true se il risultato e' un movimento.
- `didSteal`: true se il risultato e' furto o uso antisociale.
- `targetSource`: `BeliefQuery`, `LegacyFallback`, `None`.
- `legacyFallbackUsed`: true quando l'intenzione non ha ancora un job eseguibile e
  lascia proseguire la rule legacy.
- `reason`: motivo breve, per esempio `CommandEmittedByLegacyAdapter`.

Regola di controllo:

- `SearchFood`, `SearchRestPlace` e `WaitAndObserve` possono avere
  `legacyFallbackUsed: true`, perche' non hanno ancora un Job System dedicato.
- `EatKnownFood` o `RestKnownPlace` dovrebbero produrre un command quando il ponte
  legacy riesce a tradurre l'intenzione.

## Procedura passo passo

1. Abilita la sezione `memory_belief_decision_explainability` nel JSON di config.
2. Avvia la simulazione in Unity.
3. Crea o usa uno scenario in cui un NPC vede cibo o letto.
4. Lascia passare alcuni tick decisionali.
5. Apri il file JSONL nella cartella `Arcontio_EL_MBD`.
6. Filtra mentalmente o con un editor per lo stesso `npcId`.
7. Controlla che compaiano record `memory`, `belief`, `query`, `decision`, `bridge`.
8. Verifica che il `winner` della query sia compatibile con il belief creato.
9. Verifica che il `selectedIntent` della decisione sia coerente con il bisogno attivo.
10. Verifica che il bridge dica se ha prodotto un command o se ha usato fallback.

## Criterio di chiusura v0.05 EL-MBD

La sessione puo' essere considerata chiusa quando:

- i test EditMode esistenti passano;
- il progetto compila in Unity;
- con JSONL attivo vengono generati record per tutti e cinque i kind principali;
- almeno un caso Food produce la catena `ObjectSpotted -> Food belief -> Food query -> decision -> bridge`;
- non sono necessari strumenti grafici per verificare la catena;
- nessun file `.meta`, `Library`, `Temp` o `Obj` viene modificato come parte del lavoro.

## Limiti noti

- Non esiste ancora pannello UI runtime dedicato.
- Il bridge usa ancora command legacy: non e' il Job System finale.
- I candidati filtrati prima della lista operativa non sono ancora esportati.
- Il tracking fine per singoli NPC configurati e' preparato nei parametri, ma il primo
  sink JSONL si limita ai gate principali `enabled`, `writeJsonLog` e `log*`.
