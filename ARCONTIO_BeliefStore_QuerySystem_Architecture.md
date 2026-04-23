# ARCONTIO

## Architettura aggiornata - Memory, BeliefStore, QuerySystem, Decision Layer, Job, JobPhase, Step, Command

Documento di progettazione tecnica - v2.0  
Base: `ARCONTIO BeliefStore QuerySystem Architecture.docx`  
Aggiornamento: Aprile 2026, dopo v0.06

---

## 1. Introduzione e principio fondamentale

In ARCONTIO vige il **Vincolo di Onniscienza**: nessun NPC deve avere accesso diretto allo stato oggettivo del mondo per decidere cosa fare.

Ogni decisione deve derivare da:

- percezione soggettiva;
- memoria imperfetta;
- credenze sintetiche;
- comunicazione mediata;
- query cognitive esplicite;
- intenzioni selezionate dal Decision Layer;
- job persistenti eseguiti tramite step atomici;
- comandi applicati dai sistemi del mondo.

Il cuore dell'architettura non e' quindi "NPC legge il mondo e sceglie", ma:

```text
World Event
  -> Perception
  -> Memory
  -> Belief
  -> Query
  -> Decision
  -> Job
  -> JobPhase
  -> Step
  -> Command
  -> World System
  -> World Event
```

### Regola non negoziabile

Il `Decision Layer`:

- non legge mai il `World State` oggettivo;
- non legge mai il `MemoryStore` direttamente;
- non usa il `BeliefStore` come fonte grezza da scandagliare;
- accede alle credenze solo tramite `BeliefQueryService`;
- produce intenzioni o richieste di lavoro, non mutazioni dirette del mondo.

Il `BeliefStore` non e' la verita'. E' il modello operativo soggettivo di un NPC.

---

## 2. Posizione nella pipeline di simulazione

La pipeline completa aggiornata, includendo i layer introdotti fino alla v0.06, e':

```text
World State oggettivo
  -> WorldSystems.ApplyCommands()
      applica i CommandBuffer prodotti nel tick precedente o nello step corrente

  -> PerceptionSystem.Update()
      osserva FOV, LOS, landmark, oggetti, NPC, strutture e produce eventi osservabili

  -> MemoryEncodingSystem
      applica IMemoryRule agli IWorldEvent percepiti o comunicati

  -> MemoryStore
      conserva MemoryTrace episodiche grezze, soggettive e decadenti

  -> BeliefUpdater
      aggrega nuove tracce o feedback operativi in BeliefEntry normalizzati

  -> BeliefStore
      conserva credenze sintetiche per-NPC, indicizzate per categoria e status

  -> BeliefQueryService
      filtra, valuta, pesa e ordina credenze tramite evaluator condivisi

  -> Decision Layer
      genera candidati, calcola score, seleziona intenzione e produce richiesta di job

  -> JobArbiter
      accetta, rifiuta, sospende o preempta job in base alla priorita'

  -> JobStateMachine
      avanza lo stato globale del job

  -> JobPlan
      contiene fasi ordinate

  -> JobPhase
      rappresenta un sotto-obiettivo locale del job

  -> Step
      esegue una singola azione atomica in forma verbo + complemento

  -> Command
      descrive una mutazione world-safe richiesta, senza applicarla direttamente

  -> World Systems
      applicano comandi, producono nuovi eventi e chiudono il ciclo
```

### Ordine di esecuzione per tick

L'ordine progettuale di riferimento e':

1. `WorldSystems.ApplyCommands()`
2. `PerceptionSystem.Update()`
3. `MemorySystem.Decay()`
4. `NeedsSystem.Decay()`
5. `DecisionSystem.Evaluate()`, solo su trigger e non come scansione onnisciente ogni tick
6. `JobSystem.Tick()`
7. `StepSystem.Execute()`
8. `EventBus.Flush()`

Nello stato attuale del codice esiste ancora una gestione legacy tramite `NeedsDecisionRule`. Questa gestione resta provvisoria: serve da ponte runtime finche' il Decision Layer completo non produce job in modo integrato.

---

## 3. Memory System

### 3.1 Responsabilita'

Il `Memory System` trasforma eventi percepiti o comunicati in tracce soggettive.

Non produce decisioni.  
Non sceglie target.  
Non classifica cosa conviene fare.  
Non deve leggere il mondo per correggere retroattivamente una memoria.

Il suo compito e':

- ricevere eventi osservabili;
- applicare regole di codifica;
- generare `MemoryTrace`;
- salvare le trace nel `MemoryStore`;
- far decadere intensita' e affidabilita' nel tempo;
- alimentare indirettamente il `BeliefUpdater`.

### 3.2 MemoryTrace

Una `MemoryTrace` rappresenta un episodio o una informazione soggettiva.

Campi concettuali:

| Campo | Significato |
|---|---|
| `MemoryType` | Tipo semantico dell'evento ricordato |
| `SubjectId` | Entita' principale coinvolta |
| `SubjectDefId` | Definizione soggettiva osservata, per esempio tipo oggetto percepito |
| `ObjectId` | Entita' secondaria coinvolta |
| `Position` | Posizione ricordata o stimata |
| `Severity` | Gravita' soggettiva |
| `Salience` | Quanto la trace e' cognitivamente rilevante |
| `CurrentIntensity` | Intensita' residua dopo decay |
| `Reliability` | Affidabilita' della traccia |
| `Source` | Vista diretta, comunicazione, inferenza o altra fonte |
| `Flags` | Marcatori come `DirectWitness`, `Heard`, `Inferred` |

`SubjectDefId` e' importante per evitare lookup onniscienti. Se un NPC vede un oggetto come `food`, il belief futuro deve derivare da quella informazione nella trace, non da una lettura successiva del database globale degli oggetti.

### 3.3 IMemoryRule

Le `IMemoryRule` sono regole specializzate che traducono eventi del mondo in tracce.

Firma concettuale:

```csharp
bool Matches(IWorldEvent e);
MemoryTrace? Encode(NpcContext npc, IWorldEvent e, RelationshipView rel, PerceptionView perc);
```

Regole attese o gia' formalizzate a livello di sistema:

- `PredatorSpottedMemoryRule`
- `AttackWitnessedMemoryRule`
- `DeathWitnessedMemoryRule`
- `ObjectSpottedMemoryRule`
- `NpcSpottedMemoryRule`
- `FoodStolenMemoryRule`

### 3.4 MemoryStore

Il `MemoryStore` e' uno store episodico per-NPC.

Responsabilita':

- conservare trace;
- applicare limiti di capacita';
- scartare trace non accettate;
- permettere al `BeliefUpdater` di consumare nuove trace;
- non rispondere a query decisionali complesse.

Il `Decision Layer` non deve mai consultare direttamente il `MemoryStore`.

---

## 4. BeliefStore

### 4.1 Responsabilita'

Il `BeliefStore` e' un contenitore passivo per-NPC.

Non ragiona.  
Non sceglie il belief migliore.  
Non pesa distanza, freschezza o urgenza.  
Non legge il mondo oggettivo.  
Non produce intenzioni.

Il suo compito e':

- conservare `BeliefEntry`;
- permettere filtri semplici per categoria e stato;
- applicare mutazioni meccaniche ricevute dal `BeliefUpdater`;
- rimuovere o degradare credenze decadute;
- restare separato dal `QuerySystem`.

Il `BeliefStore` puo' rispondere a domande banali come:

```text
Dammi tutti i belief Food con status Active o Weak.
```

Non deve rispondere a domande come:

```text
Qual e' il cibo migliore per questo NPC affamato?
```

Quella domanda appartiene al `BeliefQueryService`.

### 4.2 BeliefEntry

Ogni credenza sintetica e' rappresentata da una `BeliefEntry`.

| Campo | Tipo | Descrizione |
|---|---|---|
| `BeliefId` | int | Identificatore univoco per-NPC |
| `Category` | `BeliefCategory` | Categoria semantica della credenza |
| `EstimatedPosition` | `Vector2Int` | Posizione stimata dell'entita', risorsa o area |
| `Confidence` | float 0-1 | Quanto l'NPC e' sicuro della credenza |
| `Freshness` | float 0-1 | Quanto l'informazione e' recente |
| `LastUpdatedTick` | int | Tick dell'ultimo aggiornamento |
| `SourceCount` | int | Numero di trace che contribuiscono |
| `Source` | `BeliefSource` | Origine prevalente della credenza |
| `Status` | `BeliefStatus` | Stato operativo della credenza |

### 4.3 BeliefCategory

| Categoria | Contenuto |
|---|---|
| `Food` | Fonti di cibo note o credute |
| `Rest` | Luoghi per dormire o riposare |
| `Danger` | Aree o entita' percepite come pericolose |
| `Social` | Credenze su NPC, affidabilita', ostilita', alleanza |
| `Ownership` | Credenze su proprieta' e accesso a risorse |
| `Situation` | Credenze aggregate di stato, scarsita', crisi, disordine |
| `Structure` | Credenze su strutture permanenti, porte, muri, stanze |

### 4.4 BeliefStatus

| Status | Significato |
|---|---|
| `Active` | Credenza valida e utilizzabile dal Decision Layer tramite query |
| `Weak` | Confidence sotto soglia, ma ancora utilizzabile in emergenza |
| `Conflicted` | Informazioni contraddittorie hanno indebolito il belief |
| `Stale` | Informazione troppo vecchia, non prioritaria |
| `Discarded` | Credenza invalidata, da rimuovere o ignorare |

### 4.5 Decay differenziato

Il decay dei belief e' differenziato per categoria.

| Categoria | Decay | Motivazione |
|---|---|---|
| `Food`, `Rest` | Rapido | Risorse e posti accessibili possono cambiare spesso |
| `Social`, `Ownership` | Medio | Relazioni e proprieta' cambiano con piu' inerzia |
| `Danger` | Lento | I pericoli possono persistere nell'area |
| `Structure` | Molto lento | Muri, porte e stanze cambiano raramente |
| `Situation` | Variabile | Dipende dal tipo di situazione aggregata |

Quando una credenza decade:

- `Freshness` scende piu' velocemente della `Confidence`;
- sotto certe soglie il belief puo' diventare `Weak` o `Stale`;
- con confidence nulla o status `Discarded`, il belief viene rimosso o ignorato;
- l'NPC perde la possibilita' di agire su quel target se nessuna query trova alternative utilizzabili.

---

## 5. BeliefUpdater e aggregation rules

### 5.1 Responsabilita'

Il `BeliefUpdater` e' il layer che traduce memoria e feedback operativi in credenze.

Legge:

- nuove `MemoryTrace`;
- segnali di fallimento operativo;
- eventuali contraddizioni informative;
- eventi cognitivi interni espliciti.

Aggiorna:

- `Confidence`;
- `Freshness`;
- `Status`;
- `SourceCount`;
- posizione stimata;
- origine prevalente.

Non deve:

- scegliere target;
- generare intenzioni;
- leggere il world state oggettivo per verificare se il belief e' vero;
- contenere logica di ranking.

### 5.2 IBeliefAggregationRule

Ogni regola di aggregazione riceve una trace e decide se puo' produrre o aggiornare una `BeliefEntry`.

Firma concettuale:

```csharp
public interface IBeliefAggregationRule
{
    bool Matches(MemoryTrace trace);
    void Apply(MemoryTrace trace, BeliefStore store, int currentTick);
}
```

Regole MVP:

- `DangerBeliefAggregationRule`
- `ObjectBeliefAggregationRule`
- `SocialBeliefAggregationRule`

Estensioni future:

- regole ownership;
- regole rest quality;
- regole social trust;
- regole norm;
- regole situation/scarcity.

### 5.3 Gerarchia di qualita' delle fonti

Quando due informazioni competono, il sistema usa una gerarchia qualitativa.

| Priorita' | Fonte | Effetto |
|---|---|---|
| 1 | Vista diretta recente | Sovrascrive o rinforza con confidence alta |
| 2 | Esperienza operativa diretta | Aggiorna con peso alto, per esempio target assente all'arrivo |
| 3 | Testimonianza affidabile | Modula confidence in base a trust futuro |
| 4 | Sentito dire debole | Incrementa poco la confidence o il source count |
| 5 | Memoria vecchia | Mantiene o degrada, ma non sostituisce evidenze recenti |

Con tracce contraddittorie di qualita' simile:

- il belief puo' diventare `Conflicted`;
- la confidence viene ridotta;
- la query puo' comunque usarlo in emergenza, ma con score peggiore.

### 5.4 Feedback da fallimento operativo

La v0.04 ha introdotto un ponte MVP tra esecuzione e belief:

```text
Job / Rule / Command / Step
  -> BeliefFailureSignal
  -> BeliefUpdater
  -> BeliefStore
```

Tipi di fallimento:

| Tipo | Significato | Effetto cognitivo |
|---|---|---|
| Smentita diretta locale | L'NPC arriva dove credeva ci fosse il target e non lo trova | `Discarded` o forte riduzione |
| Fallimento operativo ambiguo | L'NPC non riesce a raggiungere il target | `Weak`, non falsifica automaticamente il belief |
| Contraddizione informativa | Nuova informazione incompatibile con la precedente | `Conflicted` |

Regola critica: un pathfinding fallito non significa automaticamente che il cibo non esista. Significa solo che l'NPC non e' riuscito a raggiungerlo.

---

## 6. QuerySystem

### 6.1 Architettura scelta

Il `QuerySystem` usa il modello:

```text
BeliefStore passivo
  -> BeliefQueryService
      -> QueryContext
      -> Evaluator condivisi
      -> QueryResult con breakdown
```

Questa scelta serve a mantenere:

- coerenza cognitiva;
- riuso degli evaluator;
- explainability nativa;
- separazione tra dato soggettivo e valutazione contestuale;
- possibilita' di aggiungere nuove query senza sporcare il `BeliefStore`.

### 6.2 Componenti

| Componente | Responsabilita' |
|---|---|
| `BeliefStore` | Conserva entry e offre filtri semplici |
| `BeliefQueryContext` | Descrive il bisogno cognitivo della query |
| `IBeliefEvaluator` | Calcola un contributo riusabile allo score |
| `BeliefScoreContext` | Tiene belief, query e contributi |
| `BeliefQueryService` | Filtra candidati, applica evaluator, ordina risultati |
| `BeliefQueryResult` | Restituisce vincitore, score, breakdown e stato vuoto |

### 6.3 QueryContext

Il contesto MVP contiene:

| Campo | Significato |
|---|---|
| `GoalType` | Categoria cercata, per esempio `Food`, `Rest`, `Danger` |
| `Urgency01` | Urgenza normalizzata del bisogno |
| `NpcPosition` | Posizione soggettivamente rilevante per la distanza |
| `MinConfidence` | Soglia minima accettabile |

Campi progettuali futuri:

- `OwnershipPolicy`;
- `DangerIntensity`;
- `RiskAversion`;
- `SocialTrust`;
- `NormPressure`;
- `RoleContext`;
- `ScheduleContext`.

### Principio di disciplina

Il `QueryContext` deve restare minimo.

Un nuovo campo va aggiunto solo quando:

- esiste un caso d'uso concreto;
- il valore non e' ricavabile da campi gia' presenti;
- almeno due evaluator o query lo useranno, oppure una query critica lo richiede con motivazione esplicita.

### 6.4 Evaluator MVP

| Evaluator | Input | Logica |
|---|---|---|
| `ConfidenceBeliefEvaluator` | `Belief.Confidence` | Premia credenze ritenute affidabili |
| `FreshnessBeliefEvaluator` | `Belief.Freshness` | Penalizza informazioni vecchie |
| `DistanceBeliefEvaluator` | posizione NPC e belief | Penalizza target lontani, modulando con urgenza |

I pesi sono data-driven tramite `belief_query_config.json`, non hardcoded come scelta locale dentro la query.

### 6.5 Evaluator futuri

| Evaluator | Dipendenza | Stato |
|---|---|---|
| `RiskToleranceEvaluator` | `NpcDnaProfile` / cognitive modulators | Futuro |
| `RestQualityEvaluator` | qualità luoghi di riposo | Futuro |
| `OwnershipEvaluator` | ownership belief / norm system | Futuro |
| `DangerPenaltyEvaluator` | danger belief nell'area target | Futuro |
| `AllyPresenceEvaluator` | social layer | Stub progettuale |
| `TrustEvaluator` | social trust | Futuro |
| `LegalityEvaluator` | norm system | Futuro |
| `PunishmentRiskEvaluator` | norm + social perception | Futuro |

`AllyPresenceEvaluator` deve rimanere placeholder finche' il Social Layer non esiste. Non va simulato con accessi globali.

### 6.6 Flusso di query

Esempio generico:

```text
Decision Layer chiede: trova il miglior belief Food per questo NPC.

1. BeliefQueryService legge dal BeliefStore solo candidati Food Active/Weak.
2. Scarta candidati sotto MinConfidence o Discarded.
3. Costruisce ScoreContext per ogni candidato.
4. Applica Confidence, Freshness, Distance e altri evaluator registrati.
5. Somma i contributi.
6. Ordina per score.
7. Restituisce QueryResult.
8. Se QueryResult.IsEmpty, il Decision Layer genera una intenzione Search invece di ActOnKnownTarget.
```

### 6.7 Query MVP e progettuali

Query gia' coerenti con lo stato attuale o immediatamente supportabili:

- `HasAnyUsableBeliefFor(goalType)`
- `GetBestKnownFoodSource(...)`
- `GetBestKnownRestPlace(...)`
- `QueryBest(...)`

Query progettuali future:

- `GetNearestKnownFoodSource()`
- `GetRankedFoodBeliefs()`
- `GetBestOwnedFoodSource()`
- `GetBestCommunityFoodSource()`
- `EstimateFoodScarcityLevel()`
- `GetKnownDangerZonesNear(cell, radius)`
- `IsTargetAreaBelievedDangerous(cell)`
- `GetNearestSafeArea()`
- `GetMostCredibleThreat()`
- `GetKnownPlacesByTag(tag)`
- `EstimateAreaFamiliarity(cell)`
- `CanIProbablyUse(targetId)`
- `GetMostTrustedNearbyNpc()`
- `ShouldSearchInsteadOfAct(goalType)`
- `IsBeliefSetTooConflicted(goalType)`

Molte query restano formalizzate ma non implementate. Questo e' corretto: la struttura architetturale esiste, ma le regole specializzate devono arrivare solo quando i sistemi di dominio esistono davvero.

---

## 7. Decision Layer

### 7.1 Responsabilita'

Il `Decision Layer` trasforma stato interno, needs, profilo, credenze interrogate e contesto in una intenzione.

Non esegue il mondo.  
Non fa pathfinding.  
Non muta inventari.  
Non consuma oggetti.  
Non aggiorna direttamente belief o memory.

La sua responsabilita' e':

- generare candidati;
- filtrare candidati impossibili;
- interrogare il `BeliefQueryService`;
- calcolare score compositi;
- selezionare una intenzione;
- produrre un `JobRequest` o una richiesta equivalente verso il layer di esecuzione.

### 7.2 Fasi

```text
Npc state soggettivo
  -> Candidate generation
  -> Preconditions / gates
  -> Belief queries
  -> Scoring
  -> Selection
  -> Decision audit
  -> JobRequest
```

### 7.3 Catalogo intenzioni

Le intenzioni sono semanticamente piu' alte degli step.

Esempi:

- `EatKnownFood`
- `SearchFood`
- `DrinkKnownWater`
- `SearchWater`
- `RestKnownPlace`
- `SearchRestPlace`
- `TakeRestrictedFood`
- `UseRestrictedRestPlace`
- `SeekSafety`
- `MaintainStability`
- `SeekSocialContact`
- `AskForHelp`
- `CommunicateKnownDanger`
- `PatrolArea`
- `FarmFood`
- `BuildStructure`
- `CraftItem`
- `HaulToStorage`
- `ManageStorage`
- `GovernColony`
- `ExploreArea`
- `WaitAndObserve`

### 7.4 Scoring

Lo scoring decisionale combina:

- urgenza del bisogno;
- competenza;
- preferenza;
- obbligo;
- pressione di ruolo futura;
- qualita' del belief interrogato;
- modulazioni cognitive future;
- rumore controllato;
- floor per bisogni critici o obblighi forti.

Il valore prodotto dal `BeliefQueryService` non sostituisce lo scoring del Decision Layer. Lo alimenta.

### 7.5 Audit

Ogni decisione deve poter spiegare:

- quali candidati erano disponibili;
- quali sono stati scartati;
- quali query sono state chiamate;
- quale belief ha vinto;
- quali contributi hanno formato lo score;
- perche' e' stata scelta una intenzione invece di un'altra;
- quale job e' stato richiesto.

Questo audit alimenta l'Explainability Layer.

### 7.6 Stato attuale e ponte legacy

Lo stato attuale e':

- catalogo intenzioni presente;
- generatori e scoring MVP presenti;
- selezione weighted/randomizzata presente;
- test EditMode superati secondo validazione utente;
- bridge runtime ancora parziale;
- `NeedsDecisionRule` legacy ancora attiva per alcune funzioni operative.

La migrazione futura deve portare:

```text
NeedsDecisionRule legacy
  -> Decision Layer
  -> JobRequest
  -> JobSystem
  -> Step
  -> Command
```

---

## 8. Job System

### 8.1 Responsabilita'

Il `Job System` e' il layer di esecuzione persistente.

Una intenzione selezionata non deve diventare una serie istantanea di mutazioni. Deve diventare un job che:

- ha un owner;
- ha una priorita';
- sopravvive nel tempo;
- puo' essere sospeso;
- puo' fallire;
- puo' produrre feedback cognitivo;
- puo' essere spiegato;
- contiene un piano e fasi.

### 8.2 Gerarchia approvata

La gerarchia stabile e':

```text
Job
  -> JobPlan
     -> JobPhase
        -> Step
           -> Command
```

Il concetto iniziale di "mini job" viene formalizzato come `JobPhase`.

### 8.3 Job

Il `Job` e' il contratto persistente dell'NPC.

Contiene:

- id;
- owner NPC;
- origine decisionale;
- priorita';
- stato globale;
- piano;
- metadata;
- eventuale belief sorgente;
- failure reason;
- tick di creazione / aggiornamento.

Stati concettuali:

- `Pending`;
- `Running`;
- `Completed`;
- `Failed`;
- `Aborted`;
- `Suspended`.

### 8.4 JobPlan

Il `JobPlan` e' il piano corrente del job.

Contiene una sequenza ordinata di `JobPhase`.

Non e' una ricerca GOAP completa nello stato attuale. La sequenza e' definita da template, builder o mapping intenzione->piano. In futuro alcune fasi potranno essere generate o sostituite dinamicamente, ma senza confondere il job persistente con un planner globale onnisciente.

### 8.5 JobPhase

La `JobPhase` e' il sotto-obiettivo locale interno a un job.

Serve a rappresentare job complessi come:

- lavora al tuo posto di lavoro;
- difendi la porta del castello;
- pattuglia una zona;
- raccogli risorsa e portala a deposito;
- cerca cibo in una regione;
- comunica un pericolo.

Una `JobPhase` puo':

- avere stato locale;
- contenere piu' step;
- avere condizioni di completamento;
- avere fallback locale;
- ripetere step in loop controllati.

Una `JobPhase` non deve:

- avere priorita' globale autonoma;
- entrare nella coda globale dei job;
- essere preemptata separatamente dal job;
- diventare un job annidato.

Regola: se un'operazione richiede piu' verbi, probabilmente e' una `JobPhase`, non uno step.

### 8.6 NpcJobState

Ogni NPC possiede uno stato job:

```text
NpcJobState
  -> ActiveJob: massimo uno
  -> SuspendedJobs: zero o piu'
  -> QueuedJobs: zero o piu'
```

Questo impedisce che l'NPC esegua molte intenzioni incompatibili in parallelo, ma permette sospensione e ripresa controllata.

### 8.7 JobArbiter

Il `JobArbiter` decide cosa fare con una nuova `JobRequest`.

Puo':

- accettare;
- rifiutare;
- mettere in coda;
- sospendere il job corrente;
- abortire il job corrente;
- applicare preemption ladder.

### 8.8 Preemption ladder

Classi progettuali:

- emergenziale;
- prioritario;
- normale;
- basso valore;
- non interrompibile localmente.

Una richiesta di fuga per pericolo immediato deve poter preemptare una routine lavorativa. Una richiesta marginale non deve interrompere un job importante gia' in corso.

### 8.9 Reservation

Le `ReservationRecord` impediscono conflitti su risorse condivise.

Esempi:

- due NPC non devono consumare lo stesso oggetto singolo;
- due NPC non devono occupare lo stesso letto assegnato;
- un job deve poter fallire se la risorsa risulta gia' riservata.

La reservation non e' una verita' cognitiva. E' un vincolo operativo del mondo/simulazione.

### 8.10 Failure learning

Il job system puo' registrare fallimenti come:

```text
(npcId, targetCell) -> failureTick
```

Questo non significa automaticamente che il belief fosse falso. Serve a evitare ripetizioni stupide e puo' produrre feedback verso il `BeliefUpdater` solo quando la natura del fallimento e' chiara.

---

## 9. Step System

### 9.1 Responsabilita'

Lo `Step System` esegue unita' atomiche del piano.

Uno step:

- non e' una intenzione;
- non e' un job;
- non e' una fase;
- non modifica direttamente il mondo;
- produce un risultato;
- puo' emettere comandi;
- puo' restare `Running` tra tick.

### 9.2 Formalizzazione verbo + complemento

Ogni `Step` deve essere modellabile come:

```text
Verbo + Complemento
```

Il verbo descrive l'azione atomica.  
Il complemento descrive il target operativo.

Esempi:

| Step | Verbo | Complemento |
|---|---|---|
| `MoveToCell` | `MoveTo` | `Cell` |
| `ReserveResource` | `Reserve` | `Resource` |
| `ObserveArea` | `Observe` | `Area` |
| `SearchFood` | `Search` | `Food` |
| `PickUpItem` | `PickUp` | `Item` |
| `ConsumeFood` | `Consume` | `Food` |
| `CommunicateDanger` | `Communicate` | `Danger` |
| `EvaluateCondition` | `Evaluate` | `Condition` |
| `WaitDuration` | `Wait` | `Duration` |

La stessa azione cambia significato in base al complemento:

- `Search + Food` non e' uguale a `Search + SafePlace`;
- `Consume + Food` non e' uguale a `Consume + Medicine`;
- `Communicate + Danger` non e' uguale a `Communicate + Request`.

### 9.3 Criterio di atomicita'

Uno step e' atomico se:

- puo' produrre un singolo esito operativo;
- non richiede piu' verbi;
- non contiene una sequenza interna complessa;
- non modifica direttamente il mondo;
- puo' essere spiegato come una richiesta locale.

Esiti:

- `Running`;
- `Success`;
- `Blocked`;
- `Failed`.

Se servono piu' verbi, serve una `JobPhase`.

### 9.4 Step base v0.06

Step o azioni base introdotte/formalizzate:

- `MoveTo`;
- `Reserve`;
- `Release`;
- `Wait`;
- `Observe`;
- `Search`;
- `PickUp`;
- `Drop`;
- `Consume`;
- `Communicate`;
- `Evaluate`.

Molti step specializzati futuri saranno combinazioni di questo pattern, non nuove eccezioni architetturali.

### 9.5 Executor

Gli executor traducono step in:

- risultato locale;
- comandi;
- feedback;
- richiesta di continuare il tick successivo.

Executor attuali:

- `BasicJobActionExecutor`;
- `PerceptionInventoryJobActionExecutor`;
- `CognitiveJobActionExecutor`.

Il fatto che un executor sappia produrre un comando non gli autorizza a mutare direttamente il mondo.

---

## 10. Command Layer e World Systems

### 10.1 Responsabilita'

Il `Command Layer` e' il confine tra decisione/esecuzione e mutazione del mondo.

Un comando significa:

```text
Vorrei che il mondo applicasse questa mutazione, se valida.
```

Non significa:

```text
Ho gia' modificato il mondo.
```

### 10.2 CommandBuffer

Il `JobCommandBuffer` raccoglie comandi prodotti dagli step.

Vantaggi:

- separa intenzione operativa e applicazione;
- permette validazione centralizzata;
- rende la pipeline spiegabile;
- consente test end-to-end senza mutazioni immediate;
- riduce accoppiamento tra job e sistemi del mondo.

### 10.3 World Systems

I `World Systems` applicano comandi legittimi.

Esempi:

- movimento;
- inventario;
- consumo;
- apertura porte;
- interazione;
- comunicazione;
- combattimento futuro;
- costruzione futura.

Dopo aver applicato comandi, i sistemi producono eventi. Gli eventi rientrano nel ciclo tramite percezione, memoria e belief.

---

## 11. Explainability Layer

### 11.1 Responsabilita'

L'Explainability Layer deve rendere osservabile il ciclo:

```text
Memory
  -> Belief
  -> Query
  -> Decision
  -> Job
  -> Step
  -> Command
```

Senza trasformarsi in un lettore globale del mondo.

### 11.2 Stato attuale

Sono gia' presenti:

- JSONL append-only per analisi offline;
- registry runtime bounded per NPC;
- ViewModel UI-friendly;
- pannello laterale con tab Memory, Belief, Decision e Pathfinding;
- bridge Decision -> Command legacy visibile;
- test EditMode per registry/ViewModel/log.

### 11.3 Aggiornamento necessario dopo v0.06

Per coprire completamente la v0.06, l'Explainability Layer dovra' aggiungere o estendere sezioni per:

- job attivo;
- job status;
- priorita';
- job origin;
- belief sorgente collegato al job;
- job phase corrente;
- step corrente;
- step result;
- comandi emessi;
- reservation acquisita o fallita;
- preemption;
- failure learning;
- feedback al BeliefUpdater.

Questa estensione e' diagnostica. Non deve introdurre nuove dipendenze decisionali.

---

## 12. Tre query complete - template di progettazione

Queste query restano template progettuali per espansioni future. Non tutte sono implementate.

### 12.1 GetBestFoodSource

Scenario:

Un NPC affamato deve scegliere tra fonti di cibo conosciute.

Evaluator:

| Evaluator | Peso concettuale | Effetto |
|---|---|---|
| Confidence | Alto | Premia cibo creduto affidabile |
| Freshness | Medio | Penalizza informazione vecchia |
| Distance | Medio | Penalizza distanza, modulata da urgenza |
| RiskTolerance | Futuro | Modula propensione a tentare target lontani |

Comportamento atteso:

- se il cibo vicino ha confidence bassa e il cibo lontano ha confidence alta, la scelta puo' favorire il lontano;
- se l'urgenza e' altissima, la distanza deve pesare di piu';
- se la confidence e' troppo bassa, il Decision Layer puo' scegliere `SearchFood`.

### 12.2 GetNearestSafeRestPlace

Scenario:

Un NPC stanco cerca un posto dove riposare.

Evaluator:

- confidence;
- freshness;
- distance;
- rest quality futura;
- ownership/access futuro;
- danger penalty futuro.

Comportamento atteso:

- un letto noto e recente batte un angolo generico;
- se il posto e' troppo vecchio o incerto, puo' scattare ricerca;
- in futuro, posti non accessibili o pericolosi saranno penalizzati.

### 12.3 GetSafeAreaToFleeTo

Scenario:

Un NPC percepisce pericolo e cerca un'area sicura.

Evaluator:

- safety stimata;
- distanza;
- danger intensity;
- ally presence futura;
- confidence.

Comportamento atteso:

- con pericolo altissimo, la distanza puo' dominare;
- con pericolo moderato, aree piu' sicure possono battere aree piu' vicine;
- `AllyPresenceEvaluator` resta stub finche' il Social Layer non esiste.

---

## 13. Tuning dei pesi

### 13.1 Principio

I pesi degli evaluator non hanno una soluzione matematica definitiva.

Sono parametri di design che devono produrre:

- comportamento credibile;
- stabilita';
- differenze osservabili tra NPC;
- assenza di oscillazioni patologiche;
- explainability leggibile.

### 13.2 Catene parallele

Per ogni query esistono due catene concettuali:

```text
EvaluatorChain
  -> attiva durante la decisione
  -> guida la scelta

RewardChain
  -> attiva dopo l'esito
  -> valuta la qualita' della scelta
```

### 13.3 Tuning offline

Ciclo previsto:

1. definire pesi centrali in config;
2. perturbare i pesi in run controllate;
3. raccogliere log;
4. correlare configurazioni e reward;
5. aggiornare valori centrali;
6. ripetere finche' il reward medio si stabilizza.

Il tuning non deve introdurre apprendimento runtime incontrollato prima che la simulazione sia stabile.

---

## 14. Regole architetturali

| # | Regola |
|---|---|
| R1 | Il `BeliefStore` non legge mai il world state oggettivo |
| R2 | Il `Decision Layer` non legge mai il `MemoryStore` direttamente |
| R3 | Il `Decision Layer` accede ai belief tramite `BeliefQueryService` |
| R4 | I belief non sono verita', ma ipotesi operative soggettive |
| R5 | Le intenzioni devono derivare da belief/query, non da sistemi globali |
| R6 | Il `QueryContext` va mantenuto minimo |
| R7 | Ogni query deve restituire breakdown explainability |
| R8 | Un evaluator dedicato serve solo con logica realmente distinta |
| R9 | I pesi evaluator devono stare in config, non hardcoded inline |
| R10 | `AllyPresenceEvaluator` resta stub finche' manca il Social Layer |
| R11 | Un job complesso usa `JobPhase`, non job annidati |
| R12 | Uno step atomico e' sempre modellabile come verbo + complemento |
| R13 | Gli step emettono comandi, non modificano direttamente il mondo |
| R14 | I comandi vengono applicati dai World Systems |
| R15 | Un fallimento operativo ambiguo non falsifica automaticamente un belief |
| R16 | L'Explainability Layer osserva snapshot legittimi, non legge globalmente il mondo |

---

## 15. Stato implementativo dopo v0.06

### 15.1 Completato

| Area | Stato |
|---|---|
| `BeliefEntry` / enum belief | Completato |
| `BeliefStore` passivo | Completato |
| Aggregazione lazy da memoria | Completato MVP |
| Decay confidence/freshness | Completato |
| Feedback da job/fallimento operativo | Completato MVP |
| `BeliefQueryService` | Completato MVP |
| Evaluator base confidence/freshness/distance | Completati |
| Config pesi query | Completata |
| Decision candidate/scoring/selection | Completato MVP |
| Decision audit | Completato MVP |
| Explainability MBQD runtime + JSONL | Completato MVP |
| `JobRequest`, `Job`, `JobPlan` | Completati |
| `JobPhase` | Formalizzata e introdotta nella gerarchia |
| `NpcJobState` | Completato |
| `JobArbiter` | Completato MVP |
| `JobStateMachine` | Completata |
| `ReservationRecord` | Completato MVP |
| Step base v0.06 | Completati MVP |
| `JobCommandBuffer` | Completato |
| Preemption ladder | Completata MVP |
| Failure learning | Completato MVP |
| Test EditMode v0.06 | Validati dall'utente |

### 15.2 Parziale o da integrare

| Area | Stato |
|---|---|
| Bridge runtime Decision -> Job completo | Parziale |
| Rimozione `NeedsDecisionRule` legacy | Futura |
| Explainability Job/Phase/Step completa | Da estendere |
| RiskTolerance evaluator | Futuro |
| Ownership evaluator | Futuro |
| RestQuality evaluator | Futuro |
| DangerPenalty evaluator completo | Futuro |
| Social trust / ally evaluator | Futuro |
| Norm evaluator | Futuro |
| Planner GOAP dinamico | Non previsto ora, valutabile in futuro |
| Tuning offline pesi | Futuro |

---

## 16. Piano di evoluzione

### 16.1 Prossima integrazione tecnica

La prossima direzione naturale e':

```text
DecisionIntent
  -> JobRequest
  -> JobPlan template
  -> JobPhase
  -> Step
  -> CommandBuffer
  -> World Systems
  -> Events
  -> Memory/Belief feedback
```

Obiettivo: ridurre progressivamente il ruolo della gestione legacy basata su `NeedsDecisionRule`.

### 16.2 Estensione Explainability

Prima di aumentare la complessita' dei job runtime, conviene rendere visibile:

- job corrente;
- phase corrente;
- step corrente;
- ultimo command emesso;
- ragione del blocco/fallimento;
- belief sorgente;
- preemption event;
- reservation event.

### 16.3 Estensione query/rules

Le nuove query specializzate devono essere introdotte solo quando esiste:

- un dominio simulativo reale;
- dati soggettivi disponibili;
- evaluator coerenti;
- test EditMode;
- scenario runtime osservabile;
- traccia explainability.

### 16.4 Regola di modularita'

Ogni nuovo comportamento deve attraversare i layer corretti.

Esempio corretto:

```text
NPC ha fame
  -> Decision Layer valuta EatKnownFood
  -> QuerySystem trova belief Food
  -> Decision seleziona intenzione
  -> JobRequest Eat
  -> JobPlan: ReachFoodPhase, AcquireFoodPhase, ConsumeFoodPhase
  -> Step: MoveTo + Cell
  -> Step: Reserve + Resource
  -> Step: PickUp + Item
  -> Step: Consume + Food
  -> Commands applicati dai World Systems
  -> Eventi prodotti
  -> Memory e Belief aggiornati
```

Esempio da evitare:

```text
NeedsDecisionRule legge direttamente world.foods
  -> sceglie il cibo vero piu' vicino
  -> sposta NPC o consuma direttamente
```

Questa seconda forma e' incompatibile con il vincolo di onniscienza.

---

## 17. Sintesi finale

L'architettura aggiornata stabilizza una catena modulare:

```text
Memoria soggettiva
  -> Credenze sintetiche
  -> Query cognitive
  -> Decisione spiegabile
  -> Job persistente
  -> Fasi locali
  -> Step atomici
  -> Comandi world-safe
  -> Eventi
  -> Nuova memoria
```

Il punto centrale non e' solo separare classi, ma separare responsabilita' cognitive:

- la memoria ricorda;
- il belief sintetizza;
- la query valuta;
- la decisione sceglie;
- il job persiste;
- la phase organizza;
- lo step agisce atomicamente;
- il command chiede mutazioni;
- il world system applica;
- gli eventi riaprono il ciclo.

Questa struttura permette integrazione progressiva senza tornare a un NPC onnisciente.
