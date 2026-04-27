# 1. DNA / Identity Layer

## Cos’è
È il contenitore strutturale permanente dell’NPC.  
Non rappresenta ciò che l’NPC sta facendo adesso, ma **che tipo di agente è**.

## A cosa serve
Serve a evitare che ogni comportamento sia hardcoded o deciso “dal nulla”.  
È il punto in cui definisci:
- identità
- capacità
- preferenze
- disposizione
- soglie
- vincoli sociali e di ruolo

In pratica è il **telaio stabile** da cui il sistema decisionale ricaverà bias, priorità, incompatibilità e predisposizioni.

## Cosa contiene
Nel tuo schema:
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

## Cosa fa
Non “agisce” direttamente.  
Fornisce i dati di base per rispondere a domande come:
- questo NPC è più portato al comando o all’esecuzione?
- tollera poco la fame?
- ha obblighi forti verso la comunità?
- è incline al rischio?
- è un fabbro, una guardia, un ladro?

## Cosa NON deve fare
- non contiene stato runtime
- non contiene intenzione corrente
- non contiene job attivo
- non contiene memoria episodica del momento
- non prende decisioni


# 2. Role / Identity Layer

## Cos’è
È il livello che traduce parte del DNA in **ruoli sociali e professionali leggibili dal sistema**.

## A cosa serve
Serve a dire **chi è l’NPC nel mondo sociale**:
- mestiere principale
- ruolo secondario
- eventuale funzione istituzionale
- aspettative e doveri associati

## Cosa fa
- abilita certe classi di intenzioni
- ne rende alcune più probabili
- ne rende altre anomale o costose
- impone obblighi strutturali

## Cosa NON deve fare
- non esegue job
- non decide da solo cosa farà in questo tick
- non sostituisce il layer decisionale


# 3. Decision / Intent / Arbitration Layer

## Cos’è
È il cervello funzionale dell’NPC.

## A cosa serve
Serve a decidere tra tutte le possibili azioni quale eseguire.

## Cosa fa
1. Genera intenzioni  
2. Le pesa  
3. Sceglie quella attiva  

## Cosa produce
Produce un’intenzione.

## Cosa NON deve fare
- non esegue azioni fisiche
- non gestisce step


# 4. Global Intention Catalog

## Cos’è
Vocabolario centrale delle intenzioni.

## A cosa serve
Standardizza ciò che un NPC può voler fare.

## Contenuto
- Sopravvivenza
- Produzione
- Logistica
- Sicurezza
- Sociale
- Istituzionale
- Devianza
- Organizzazione


# 5. Job Execution Layer

## Cos’è
Struttura persistente di esecuzione.

## A cosa serve
Mantiene continuità nel tempo.

## Cosa fa
- gestisce sequenza step
- mantiene stato
- gestisce fallimenti


# 6. Step Layer

## Cos’è
Modulo atomico di azione.

## Cosa fa
Esegue una singola azione.

## Output
- Running
- Success
- Blocked
- Failed
- Cancelled


# 7. Runtime Primitives

## Cos’è
Layer tecnico sotto gli step.

## Famiglie
- locomozione
- interazione
- targeting
- manipolazione
- uso oggetti
- timing
- reservation
- percezione
- comunicazione
- conflitto
- lifecycle
- validazione


# 8. Command Layer

## Cos’è
Richieste di mutazione.

## Cosa fa
Gli step emettono command invece di modificare il mondo.


# 9. World Systems

## Cos’è
Applicano i command.

## Cosa fanno
Modificano il mondo reale.


# 10. World Events

## Cos’è
Fatti accaduti nel mondo.

## Cosa fa
Descrive eventi oggettivi.


# 11. Perception / Memory

## Cos’è
Trasforma eventi in esperienza soggettiva.


# 12. JobExecutionSystem

## Cos’è
Gestisce i job attivi.

## Cosa fa
- esegue step
- gestisce stato job


# 13. Pipeline

DNA → Decision → Job → Step → Primitives → Commands → Systems → Events → Perception


# 14. Regole

- Intenzione ≠ Job ≠ Step
- Step ≠ System
- DNA ≠ Runtime


# 15. Glossario

DNA = struttura  
Intenzione = obiettivo  
Job = esecuzione  
Step = azione  
Primitive = base tecnica  
Command = richiesta  
System = applicazione  
Event = risultato  
