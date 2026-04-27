# ARCONTIO — Sistema di Petizioni e Allocazione del Lavoro

---

# 🧠 Visione generale

Il lavoro in ARCONTIO **non nasce da uno scheduler centrale**, ma da un ciclo sociale emergente:

```text
Mondo → NPC percepiscono problemi → Petizioni
→ Aggregazione (colonia)
→ Lettura istituzionale (manager NPC)
→ Apertura ruoli
→ Candidature / richieste cambio
→ Selezione soggettiva
→ Assegnazione lavoro
```

È un sistema:
- bottom-up (i bisogni emergono dal basso)
- mediato (non tutti i segnali arrivano puliti)
- istituzionale (qualcuno decide)
- imperfetto (bias, errori, incompletezza)

---

# 1. Generazione delle petizioni (bottom-up)

## Cos’è una petizione
Una **petizione è un evento** emesso da un NPC quando rileva una mancanza o un problema.

Non è una verità oggettiva.  
È una **segnalazione soggettiva basata su percezione locale**.

## Quando nasce
Un NPC può generare petizione quando:

- non trova cibo
- trova scorte basse
- rileva strutture danneggiate
- osserva oggetti mancanti
- percepisce inefficienze
- subisce disservizi

## Caratteristiche
Una petizione contiene:
- tipo di problema (cibo, sicurezza, costruzione…)
- posizione / contesto
- intensità percepita
- eventuale urgenza
- fonte (chi l’ha emessa)

## Proprietà fondamentali

### 1. Località
L’NPC non ha visione globale:
- segnala ciò che vede o sperimenta

### 2. Fallibilità
Può essere:
- incompleta
- ridondante
- errata
- obsoleta

### 3. Frequenza variabile
NPC diversi possono:
- segnalare molto
- segnalare poco
- ignorare problemi

Questo è importante per evitare un sistema perfetto.

---

# 2. Aggregazione a livello colonia

## Cos’è la struttura colonia
Ogni colonia ha una struttura dati che:
- raccoglie petizioni
- le aggrega
- le organizza per dominio

## Cosa fa l’aggregazione

Trasforma una massa di segnali grezzi in qualcosa di leggibile:

```text
10 NPC segnalano fame
→ “problema cibo alto”
```

Oppure:

```text
petizioni sparse su riparazioni
→ “manutenzione necessaria”
```

## Output dell’aggregazione

Produce uno stato del tipo:
- domanda per dominio (cibo, sicurezza, produzione…)
- intensità aggregata
- priorità stimata
- eventuali trend (persistente vs temporaneo)

## Punto importante

Questa struttura:
- **non decide**
- non assegna lavoro
- non ottimizza

È solo un **collettore strutturale**.

---

# 3. Lettura istituzionale (Personnel Manager NPC)

## Cos’è
Un NPC con ruolo istituzionale (gestione personale) legge lo stato della colonia.

## Cosa vede
Non vede necessariamente:
- dati perfetti
- tutte le petizioni
- lo stato reale completo

Ma vede:
- una rappresentazione aggregata
- mediata dal sistema
- potenzialmente incompleta o distorta

## Cosa fa

Interpreta la situazione:

```text
“serve più cibo”
“serve sicurezza”
“serve produzione”
```

Ma questa interpretazione è filtrata da:
- suo DNA
- suo NpcProfile
- sua memoria
- suoi bias sociali
- relazioni personali

---

# 4. Apertura delle posizioni lavorative

## Cos’è una posizione
Una **posizione** è un ruolo aperto nella colonia:

```text
Serve: 2 agricoltori
Serve: 1 guardia
Serve: 1 fabbro
```

## Come nasce
Il manager decide:

- se aprire o meno una posizione
- quante aprirne
- in quale dominio

## Importante
Non è automatico.

Due manager diversi possono:
- reagire diversamente allo stesso stato
- ignorare problemi
- sovrastimare altri

---

# 5. Candidature e richieste cambio lavoro

## Due canali

### 1. Candidatura passiva
NPC disponibili o senza ruolo possono essere considerati.

### 2. Richiesta attiva
Un NPC può chiedere cambio lavoro se:

- insoddisfazione supera soglia
- mismatch tra:
  - competence
  - preference
  - obligation
  - ruolo attuale

## Insoddisfazione

Deriva da conflitti tipo:
- lavoro che non piace
- lavoro troppo difficile
- lavoro imposto
- mancanza di riconoscimento

---

# 6. Processo di selezione

## Non è ottimizzazione pura

Il manager NON sceglie solo il migliore tecnicamente.

## Fattori in gioco

Per ogni candidato:

### Tecnici
- CompetenceProfile (quanto è capace)
- esperienza

### Personali
- PreferenceProfile (quanto lo gradisce)
- affidabilità percepita

### Sociali
- classe sociale
- gruppo di appartenenza
- reputazione

### Relazionali
- simpatia
- antipatia
- fiducia
- conflitti passati

### Istituzionali
- obblighi
- legittimità del candidato
- coerenza con struttura sociale

## Formula concettuale

```text
SelectionScore =
competence
+ preference_fit
+ obligation_fit
+ trust
+ group_affinity
+ social_status
- hostility
- stigma
+ manager_bias
```

Non è mai completamente razionale.

---

# 7. Assegnazione del ruolo

## Esito
Il manager:
- assegna il ruolo
- aggiorna AssignedRole dell’NPC
- aggiorna ObligationProfile

## Effetti
Questo modifica:
- il comportamento futuro dell’NPC
- le intenzioni generate
- la struttura sociale della colonia

---

# 8. Feedback loop

Il sistema è ciclico:

```text
Assegnazione lavoro
→ NPC lavorano (o falliscono)
→ nuovi problemi emergono
→ nuove petizioni
→ nuova aggregazione
→ nuove decisioni
```

---

# 9. Punti di forza del sistema

## 1. Emergenza reale
I ruoli nascono da bisogni percepiti, non da design statico.

## 2. Imperfezione strutturale
Errori, bias, ritardi generano dinamiche interessanti.

## 3. Conflitto sociale
- incompetenti promossi
- capaci ignorati
- gruppi favoriti
- tensioni interne

## 4. Dinamicità
La struttura lavorativa evolve continuamente.

---

# 10. Rischi da controllare

## 1. Informazione troppo perfetta
→ torna uno scheduler nascosto

## 2. Manager troppo razionale
→ perde la componente sociale

## 3. Petizioni troppo accurate
→ sistema troppo stabile

## 4. Assenza di inerzia
→ ruoli che cambiano troppo velocemente

---

# 📌 Sintesi finale

Il sistema di lavoro in ARCONTIO è:

> un ciclo emergente in cui bisogni percepiti generano petizioni, le petizioni vengono aggregate a livello di colonia, e un attore istituzionale (NPC) interpreta questi segnali e assegna ruoli attraverso un processo decisionale imperfetto, influenzato da competenze, preferenze, obblighi e fattori sociali.

Non è ottimizzazione.  
È **organizzazione sociale simulata**.
