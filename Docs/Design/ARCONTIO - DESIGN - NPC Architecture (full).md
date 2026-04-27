
# ARCONTIO — NPC SYSTEM ARCHITECTURE (FULL SPEC v1.0)

---

## 1. Visione generale

ARCONTIO è una simulazione emergente basata su:

- mondo oggettivo (stato canonico)
- menti soggettive (percezione limitata, memoria imperfetta)
- comportamento emergente non scriptato

Principi fondamentali:
- No god mode
- Separazione tra verità del mondo e conoscenza degli NPC
- Tutto passa da: percezione → interpretazione → decisione → azione

---

## 2. Layer architetturali

Struttura completa:

DNA / Identity  
→ Decision / Intent / Arbitration  
→ Job Execution  
→ Step Execution  
→ Runtime Primitives  
→ Commands  
→ World Systems  
→ World Events  
→ Perception / Memory  

Separazione obbligatoria tra i layer.

---

## 3. NpcDnaProfile

Struttura modulare:

NpcDnaProfile
- Identity
- Capacities
- Preferences
- Dispositions
- SocialPosition
- ObligationFrame
- Thresholds
- CognitiveModulators
- Traits
- Tags
- ExtensionData

Regole:
- Solo dati strutturali
- Nessun runtime
- Nessuna decisione

---

## 4. Mestieri (Role System)

Lista base:
- Agricoltore
- Fabbro
- Carpentiere
- Cuoco
- Magazziniere
- Guardia
- Medico
- Coordinatore / Capo
- Giudice
- Ladro
- Lavoratore generico

Funzione:
- Generano bias sulle intenzioni
- Impongono obblighi
- NON sono job

---

## 5. Catalogo intenzioni

Domini:
- Sopravvivenza
- Produzione
- Logistica
- Sicurezza
- Sociale
- Istituzionale
- Devianza
- Organizzazione

Regole:
- Astratte
- Riutilizzabili
- Limitate (~20–30)

---

## 6. Modello Intenzione

Intenzione = obiettivo semantico

Flusso:
DNA → genera intenzioni → arbitration → scelta → job

---

## 7. Job System

Definizione:
Job = esecuzione persistente

Componenti:
- JobDefinition
- JobInstance
- RuntimeState

Stati:
Pending, Running, Suspended, Blocked, Completed, Failed, Aborted

Responsabilità:
- Sequenza step
- Stato
- Retry / suspend

NON deve:
- decidere strategia
- conoscere tutto il mondo

---

## 8. Step System

Step = modulo atomico

Catalogo:
MoveTo, MoveAwayFrom, Follow, CheckAccess, Reserve, Release, Wait,
Search, PickUp, Drop, Transfer, Consume, Store, Use, Operate,
ApplyTo, GenerateOutput, Observe, Identify, Evaluate, Attack,
Defend, Restrain, Communicate, Request, Assist, Signal, Enforce,
Judge, StealthMove, Steal, Hide, Select, Interrupt, Abort

Output:
Running, Success, Blocked, Failed, Cancelled

Regole:
- Atomici
- Riusabili
- No logica strategica
- Non modificano il world direttamente

---

## 9. Runtime Primitives

Famiglie:
1. Locomozione
2. Interazione spaziale
3. Targeting
4. Manipolazione
5. Uso oggetti
6. Timing
7. Reservation
8. Percezione
9. Comunicazione
10. Conflitto
11. Lifecycle
12. Validazione

Ruolo:
Primitive → supportano Step

---

## 10. Integrazione pipeline

DNA → Decision → Job → Step → Commands → Systems → Events → Perception

---

## 11. Command / System / Event

Commands:
- richieste di mutazione

Systems:
- applicano modifiche

Events:
- descrivono eventi

---

## 12. Regole architetturali

- Intenzione ≠ Job ≠ Step
- Step ≠ System
- DNA ≠ Runtime

Anti-pattern:
- Job che decide
- Step che pensa
- Systems accoppiati ai job

---

## 13. Estensione

Aggiungere:
- mestieri → solo se cambia comportamento
- intenzioni → solo se nuove
- step → solo se necessari
- primitive → solo se riusabili

---

## 14. Glossario

DNA = struttura NPC  
Intenzione = obiettivo  
Job = esecuzione  
Step = azione  
Primitive = capacità base  
Command = richiesta  
System = esecuzione  
Event = risultato  

---

END
