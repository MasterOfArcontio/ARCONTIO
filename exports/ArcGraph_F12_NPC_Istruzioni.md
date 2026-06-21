# ARCONTIO - ArcGraph F12 e Sprite NPC Modulari

Documento operativo per configurare e testare:

- switch visuale `MapGrid <-> ArcGraph` con tasto `F12`;
- renderer terrain runtime ArcGraph;
- renderer NPC runtime ArcGraph;
- NPC modulare composto da `body`, `head`, `legs`, `feet`;
- animazioni `idle` a 4 frame;
- animazioni `walk` a 9 frame.

---

## 1. Stato tecnico attuale

ArcGraph oggi non sostituisce ancora definitivamente MapGrid.

La situazione corretta da tenere a mente e':

```text
MapGrid
-> continua a essere il sistema legacy visibile e sorgente provvisoria dei dati mappa

ArcGraph
-> legge dati gia' preparati tramite adapter
-> costruisce snapshot e render queue
-> puo' disegnare terrain e NPC runtime
-> puo' essere acceso/spento come vista tramite F12
```

Lo switch `F12` non cambia la simulazione.

Non ferma gli NPC.
Non modifica il World.
Non cambia Decision Layer.
Non cambia Job Layer.
Non cancella MapGrid.

Lo switch serve solo a dire:

```text
mostra MapGrid
oppure
mostra ArcGraph
```

---

## 2. File principali coinvolti

### Switch visuale

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphViewModeSwitcher.cs
```

Serve a passare da MapGrid ad ArcGraph con `F12`.

### Wrapper runtime minimo

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphMinimalRuntimeSceneWrapper.cs
```

Coordina il percorso:

```text
adapter dati
-> runtime ArcGraph
-> render queue
-> renderer terrain
-> renderer NPC
```

### Renderer NPC

```text
Assets/Scripts/Views/ArcGraph/Runtime/ArcGraphNpcRuntimeSceneRenderer.cs
```

Disegna gli NPC.

Puo' usare:

- sprite singolo fallback;
- oppure catalogo modulare con parti `body/head/legs/feet`.

### Catalogo NPC JSON

```text
Assets/Resources/ArcGraph/Config/ArcGraphNpcVisualCatalog.json
```

Definisce:

- parti dell'NPC;
- direzioni;
- animazioni;
- numero frame;
- pattern dei nomi sprite;
- ordine visuale delle parti.

---

## 3. Come configurare lo switch F12 in Unity

### 3.1 Creare o scegliere un GameObject per lo switcher

Nella scena Unity, crea un GameObject vuoto o usa un GameObject tecnico gia'
dedicato ad ArcGraph.

Nome consigliato:

```text
ArcGraphViewModeSwitcher
```

Sul GameObject aggiungi il componente:

```text
ArcGraphViewModeSwitcher
```

Percorso menu Unity:

```text
Inspector
-> Add Component
-> cerca "ArcGraph View Mode Switcher"
```

---

## 4. Campi del componente ArcGraphViewModeSwitcher

Il componente ha diversi campi da configurare.

### 4.1 Switcher Enabled

Valore consigliato:

```text
true
```

Se e' `false`, il tasto `F12` non fa nulla.

### 4.2 Toggle Key

Valore consigliato:

```text
F12
```

Questo e' il tasto che alterna le due viste.

### 4.3 Start Mode

Valore consigliato iniziale:

```text
MapGrid
```

Significa:

```text
quando parte la scena, mostra prima MapGrid
```

Poi con `F12` passi ad ArcGraph.

### 4.4 Map Grid Visual Roots

Qui devi inserire i GameObject visuali legacy MapGrid che devono essere spenti
quando passi ad ArcGraph.

Esempi possibili:

```text
MapGridRoot
MapGridWorldView
MapGridRuntimeDevToolsOverlay
MapGridPointerCoordsOverlay
```

La scelta esatta dipende dalla gerarchia della scena.

Regola pratica:

```text
metti qui tutto cio' che appartiene alla visualizzazione MapGrid
e che non vuoi vedere quando ArcGraph e' attivo
```

Non mettere qui oggetti che servono alla simulazione.

Se un GameObject contiene solo rendering/UI MapGrid, puo' stare qui.
Se un GameObject contiene bootstrap o dati necessari, meglio non spegnerlo.

### 4.5 Arc Graph Visual Roots

Qui devi inserire i GameObject visuali ArcGraph che devono essere accesi quando
passi ad ArcGraph.

Esempi possibili:

```text
ArcGraph
ArcGraphTerrainRuntimeRoot
ArcGraphNpcRuntimeRoot
ArcGraphDebugRoot
```

Regola pratica:

```text
metti qui solo oggetti visuali ArcGraph
```

Se ArcGraph e' ancora tutto sotto un solo GameObject root, puoi mettere solo
quello.

### 4.6 Runtime Wrapper

Trascina qui il componente:

```text
ArcGraphMinimalRuntimeSceneWrapper
```

Questo e' il componente che produce il frame ArcGraph.

### 4.7 Terrain Renderer

Trascina qui il componente:

```text
ArcGraphTerrainRuntimeSceneRenderer
```

Questo e' il renderer terrain ArcGraph.

### 4.8 NPC Renderer

Trascina qui il componente:

```text
ArcGraphNpcRuntimeSceneRenderer
```

Questo e' il renderer NPC ArcGraph.

---

## 5. Flag consigliati dello switcher

Valori consigliati per il primo test:

```text
Switcher Enabled = true
Toggle Key = F12
Start Mode = MapGrid
Configure Wrapper Rendering On Switch = true
Enable Wrapper Update In ArcGraph Mode = true
Process ArcGraph Frame On Switch = true
Clear ArcGraph When Returning To MapGrid = false
Apply Start Mode On Start = true
Log Diagnostics = true
```

### Significato semplice

```text
Configure Wrapper Rendering On Switch
```

Quando passi ad ArcGraph, lo switcher abilita i gate del wrapper per terrain e
NPC.

```text
Enable Wrapper Update In ArcGraph Mode
```

Quando ArcGraph e' attivo, il wrapper puo' processare i frame in `Update`.

```text
Process ArcGraph Frame On Switch
```

Quando premi `F12`, ArcGraph prova subito a produrre un frame.

```text
Clear ArcGraph When Returning To MapGrid
```

Se e' `true`, quando torni a MapGrid vengono puliti i renderer ArcGraph.

Per il primo test meglio lasciarlo `false`, cosi' puoi vedere se gli oggetti
restano coerenti.

---

## 6. Test rapido dello switch F12

### 6.1 Preparazione

Prima di premere Play:

1. Apri la scena di test corrente.
2. Controlla che il GameObject con `ArcGraphViewModeSwitcher` sia attivo.
3. Controlla che `Start Mode` sia `MapGrid`.
4. Controlla che i riferimenti a wrapper, terrain renderer e NPC renderer siano
   assegnati.
5. Controlla che almeno un root MapGrid sia inserito in `Map Grid Visual Roots`.
6. Controlla che almeno un root ArcGraph sia inserito in `Arc Graph Visual Roots`.

### 6.2 Play Mode

Entra in Play Mode.

All'avvio dovresti vedere MapGrid.

Premi:

```text
F12
```

Risultato atteso:

```text
MapGrid si spegne visivamente
ArcGraph si accende visivamente
```

Premi di nuovo:

```text
F12
```

Risultato atteso:

```text
ArcGraph si spegne visivamente
MapGrid torna visibile
```

### 6.3 Log atteso in Console

Dovresti vedere un log simile:

```text
[ArcGraphViewModeSwitcher] ArcGraphViewActive enabled=True, mode=ArcGraph, key=F12, ...
```

Quando torni a MapGrid:

```text
[ArcGraphViewModeSwitcher] MapGridViewActive enabled=True, mode=MapGrid, key=F12, ...
```

---

## 7. Configurazione renderer NPC modulare

Sul componente:

```text
ArcGraphNpcRuntimeSceneRenderer
```

controlla questi campi:

```text
Npc Visual Catalog Json
Use Layered Actor Catalog
Renderer Enabled
Idle Frame Step
Allow Generated Fallback Sprites
```

### 7.1 Npc Visual Catalog Json

Assegna il file:

```text
Assets/Resources/ArcGraph/Config/ArcGraphNpcVisualCatalog.json
```

In Inspector verra' visto come `TextAsset`.

### 7.2 Use Layered Actor Catalog

Valore richiesto per testare NPC modulare:

```text
true
```

Se resta `false`, il renderer usa lo sprite singolo fallback.

### 7.3 Renderer Enabled

Puo' anche stare spento all'inizio, perche' lo switcher puo' abilitarlo quando
passi ad ArcGraph.

Per test manuali puoi metterlo:

```text
true
```

### 7.4 Idle Frame Step

Valore attuale consigliato:

```text
12
```

Significa:

```text
cambia frame idle ogni 12 frame Unity
```

Se vuoi idle piu' veloce, usa un numero piu' basso.

Esempio:

```text
6 = idle piu' veloce
18 = idle piu' lenta
```

### 7.5 Allow Generated Fallback Sprites

Valore consigliato durante i primi test:

```text
true
```

Se mancano sprite reali, ArcGraph puo' generare fallback magenta.

Questo serve solo per capire se il renderer funziona.
Non e' una soluzione artistica definitiva.

---

## 8. Come funzionano le animazioni NPC

### 8.1 Idle

Idle viene usata quando l'NPC non sta attraversando una cella.

Schema:

```text
NPC fermo
-> animazione idle
-> 4 frame
-> frame scelto tramite Time.frameCount / idleFrameStep
```

Esempio:

```text
idleFrameStep = 12

frame Unity 0-11   -> idle_00
frame Unity 12-23  -> idle_01
frame Unity 24-35  -> idle_02
frame Unity 36-47  -> idle_03
frame Unity 48-59  -> idle_00
```

### 8.2 Walk

Walk viene usata quando l'NPC ha un movimento visuale in corso.

Schema:

```text
NPC in movimento tra due celle
-> MotionProgress01
-> valore da 0.0 a 1.0
-> selezione di uno dei 9 frame walk
```

Esempio:

```text
MotionProgress01 = 0.00 -> walk_00
MotionProgress01 = 0.12 -> walk_01
MotionProgress01 = 0.25 -> walk_02
MotionProgress01 = 0.50 -> walk_04
MotionProgress01 = 0.75 -> walk_06
MotionProgress01 = 0.99 -> walk_08
```

Questo e' importante per ARCONTIO:

```text
se una cella richiede piu' tick per essere attraversata,
l'animazione si distribuisce lungo tutto il movimento
```

Quindi non e' una camminata a tempo fisso scollegata dal multitick.

Regola importante:

```text
il numero di frame walk non obbliga la simulazione a usare lo stesso numero di tick
```

Esempio:

```text
walk = 8 frame
movimento = 4 tick
-> alcuni frame possono essere saltati

walk = 8 frame
movimento = 16 tick
-> alcuni frame restano visibili per piu' tick
```

La durata del movimento resta simulativa.
I frame della camminata sono solo resa visuale.

---

## 9. Struttura PNG richiesta

Dimensione consigliata per ogni PNG:

```text
32 x 48 px
```

Ogni sprite rappresenta una sola parte del corpo, ma tutte le parti usano lo
stesso canvas `32x48`.

Questo significa:

```text
body/south_idle_00.png = 32x48 con solo il corpo visibile
head/south_idle_00.png = 32x48 con solo la testa visibile
legs/south_idle_00.png = 32x48 con solo le gambe visibili
feet/south_idle_00.png = 32x48 con solo i piedi visibili
```

Le zone vuote devono essere trasparenti.

Per ora non usiamo sprite ritagliati piccoli con offset separati.
Questa scelta rende il montaggio molto piu' semplice:

```text
tutte le parti vengono sovrapposte nello stesso punto
e combaciano perche' hanno lo stesso canvas
```

Le parti sono:

```text
body
head
legs
feet
```

Cartelle consigliate:

```text
Assets/Resources/ArcGraph/NPC/human_default/body/
Assets/Resources/ArcGraph/NPC/human_default/head/
Assets/Resources/ArcGraph/NPC/human_default/legs/
Assets/Resources/ArcGraph/NPC/human_default/feet/
```

Il catalogo genera sprite key di questo tipo:

```text
ArcGraph/NPC/human_default/body/south_idle_00
ArcGraph/NPC/human_default/head/south_idle_00
ArcGraph/NPC/human_default/legs/south_idle_00
ArcGraph/NPC/human_default/feet/south_idle_00
```

Nota:

```text
la sprite key non include ".png"
```

Unity usa il nome asset senza estensione.

### 9.1 Ombra NPC

L'ombra NPC e' separata dalle parti del corpo.

Il renderer puo':

```text
usare una shadow sprite risolta dal resolver
oppure
generare una ellisse fallback semitrasparente
```

Sprite ombra consigliata, opzionale:

```text
Assets/Resources/ArcGraph/NPC/common/shadow/soft_ellipse_32x16.png
```

Dimensione:

```text
32 x 16 px
```

Sprite key logica:

```text
ArcGraph/NPC/common/shadow/soft_ellipse_32x16
```

Se questa sprite non e' disponibile, il renderer puo' generare una ombra
placeholder in runtime.

---

## 10. Nomi PNG idle

Questi nomi vanno creati in ciascuna cartella parte:

```text
body/
head/
legs/
feet/
```

Lista nomi:

```text
south_idle_00.png
south_idle_01.png
south_idle_02.png
south_idle_03.png

north_idle_00.png
north_idle_01.png
north_idle_02.png
north_idle_03.png

east_idle_00.png
east_idle_01.png
east_idle_02.png
east_idle_03.png

west_idle_00.png
west_idle_01.png
west_idle_02.png
west_idle_03.png
```

Totale idle:

```text
16 PNG per parte
4 parti
64 PNG idle totali
```

---

## 11. Nomi PNG walk

Questi nomi vanno creati in ciascuna cartella parte:

```text
body/
head/
legs/
feet/
```

Lista nomi:

```text
south_walk_00.png
south_walk_01.png
south_walk_02.png
south_walk_03.png
south_walk_04.png
south_walk_05.png
south_walk_06.png
south_walk_07.png
south_walk_08.png

north_walk_00.png
north_walk_01.png
north_walk_02.png
north_walk_03.png
north_walk_04.png
north_walk_05.png
north_walk_06.png
north_walk_07.png
north_walk_08.png

east_walk_00.png
east_walk_01.png
east_walk_02.png
east_walk_03.png
east_walk_04.png
east_walk_05.png
east_walk_06.png
east_walk_07.png
east_walk_08.png

west_walk_00.png
west_walk_01.png
west_walk_02.png
west_walk_03.png
west_walk_04.png
west_walk_05.png
west_walk_06.png
west_walk_07.png
west_walk_08.png
```

Totale walk:

```text
36 PNG per parte
4 parti
144 PNG walk totali
```

---

## 12. Totale asset NPC richiesti

Per un solo visual set:

```text
visualKey = human_default
```

servono:

```text
64 PNG idle
144 PNG walk
208 PNG totali
```

Distribuzione:

```text
body  = 52 PNG
head  = 52 PNG
legs  = 52 PNG
feet  = 52 PNG
```

---

## 13. Import Unity consigliato per i PNG

Per ogni PNG:

```text
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 32
Filter Mode: Point
Compression: None
```

Consigli:

- usa trasparenza;
- mantieni tutti i frame NPC allineati sulla stessa griglia `32x48`;
- non cambiare pivot tra frame diversi della stessa parte;
- evita margini casuali diversi tra un frame e l'altro;
- mantieni stessa dimensione per tutte le parti.

Per l'ombra:

```text
dimensione consigliata = 32x16
trasparenza morbida
colore nero o grigio scuro semitrasparente
```

Pivot consigliato:

```text
center
```

Se in futuro il personaggio avra' un punto piedi piu' preciso, potremo decidere
un pivot diverso, ma per ora `center` e' piu' semplice.

---

## 14. Come modificare il JSON NPC

File:

```text
Assets/Resources/ArcGraph/Config/ArcGraphNpcVisualCatalog.json
```

Struttura attuale:

```json
{
  "defaultVisualKey": "human_default",
  "defaultAnimationKey": "idle",
  "pixelsPerUnit": 32,
  "frameWidthPixels": 32,
  "frameHeightPixels": 48,
  "parts": [
    "body",
    "head",
    "legs",
    "feet"
  ],
  "framePatterns": [
    {
      "visualKey": "human_default",
      "partKeys": [
        "body",
        "head",
        "legs",
        "feet"
      ],
      "directionKeys": [
        "south",
        "north",
        "east",
        "west"
      ],
      "animationKey": "idle",
      "frameCount": 4,
      "spriteKeyPattern": "ArcGraph/NPC/{visual}/{part}/{direction}_{animation}_{frame00}",
      "durationTicks": 8,
      "sortingOffsets": [
        0,
        3,
        1,
        2
      ]
    },
    {
      "visualKey": "human_default",
      "partKeys": [
        "body",
        "head",
        "legs",
        "feet"
      ],
      "directionKeys": [
        "south",
        "north",
        "east",
        "west"
      ],
      "animationKey": "walk",
      "frameCount": 9,
      "spriteKeyPattern": "ArcGraph/NPC/{visual}/{part}/{direction}_{animation}_{frame00}",
      "durationTicks": 4,
      "sortingOffsets": [
        0,
        3,
        1,
        2
      ]
    }
  ]
}
```

### 14.1 Cambiare numero frame idle

Modifica:

```json
"animationKey": "idle",
"frameCount": 4
```

Esempio:

```json
"frameCount": 6
```

In quel caso dovresti creare:

```text
south_idle_00.png ... south_idle_05.png
```

e cosi' per ogni direzione e parte.

### 14.1.1 Cambiare dimensione canvas NPC

Campi:

```json
"frameWidthPixels": 32,
"frameHeightPixels": 48
```

Questi campi descrivono il canvas atteso per ogni parte NPC.

Per ora il valore corretto e':

```text
32x48
```

Non confonderlo con il terreno:

```text
tile terreno = 32x32
frame NPC = 32x48
```

### 14.2 Cambiare numero frame walk

Modifica:

```json
"animationKey": "walk",
"frameCount": 9
```

Esempio:

```json
"frameCount": 12
```

In quel caso dovresti creare:

```text
south_walk_00.png ... south_walk_11.png
```

e cosi' per ogni direzione e parte.

### 14.3 Cambiare percorso sprite

Campo:

```json
"spriteKeyPattern": "ArcGraph/NPC/{visual}/{part}/{direction}_{animation}_{frame00}"
```

Significato:

```text
{visual}    -> human_default
{part}      -> body / head / legs / feet
{direction} -> south / north / east / west
{animation} -> idle / walk
{frame00}   -> 00 / 01 / 02 ...
```

Esempio generato:

```text
ArcGraph/NPC/human_default/body/south_walk_03
```

### 14.4 Cambiare ordine visuale delle parti

Campo:

```json
"sortingOffsets": [
  0,
  3,
  1,
  2
]
```

L'ordine corrisponde a:

```text
body = 0
head = 3
legs = 1
feet = 2
```

Valore piu' alto significa disegnato piu' sopra.

---

## 15. Resolver sprite: cosa aspettarsi ora

Il componente esistente:

```text
ArcGraphSerializedSpriteResolver
```

non carica automaticamente tutti gli sprite da `Resources`.

Funziona cosi':

```text
sprite key ArcGraph
-> mapping esplicito in Inspector
-> Sprite Unity assegnata manualmente
```

Ha anche fallback:

```text
Default Actor Sprite
Default Object Sprite
```

Quindi, per un test veloce, puoi:

1. assegnare un `Default Actor Sprite`;
2. lasciare i mapping specifici vuoti;
3. verificare che gli NPC appaiano;
4. poi aggiungere mapping specifici per testare il catalogo modulare vero.

Per testare davvero le 4 parti animate, ogni sprite key deve poter essere risolta.

Esempio mapping:

```text
Kind: Actor
Sprite Key: ArcGraph/NPC/human_default/body/south_idle_00
Sprite: south_idle_00 nella cartella body
```

---

## 16. Checklist primo test visuale

### Test A - Switch F12

Obiettivo:

```text
verificare che F12 alterni MapGrid e ArcGraph
```

Passi:

1. Entra in Play Mode.
2. Verifica che MapGrid sia visibile.
3. Premi `F12`.
4. Verifica che MapGrid sparisca.
5. Verifica che ArcGraph compaia.
6. Premi di nuovo `F12`.
7. Verifica che MapGrid torni visibile.

Esito da riportare:

```text
Test A:
F12 funziona: si/no
MapGrid sparisce: si/no
ArcGraph compare: si/no
Console error: testo eventuale
```

### Test B - Terrain ArcGraph

Obiettivo:

```text
verificare che ArcGraph disegni il terreno
```

Passi:

1. Entra in Play Mode.
2. Premi `F12`.
3. Guarda se il terreno ArcGraph appare.
4. Controlla Console.

Esito da riportare:

```text
Test B:
Terrain visibile: si/no
Numero chunk/mesh: se leggibile
Console error: testo eventuale
```

### Test C - NPC ArcGraph fallback

Obiettivo:

```text
verificare che almeno un NPC venga disegnato
```

Passi:

1. Sul renderer NPC lascia `Allow Generated Fallback Sprites = true`.
2. Entra in Play Mode.
3. Premi `F12`.
4. Guarda se compare un quadrato/sprite NPC fallback.

Esito da riportare:

```text
Test C:
NPC visibile: si/no
Fallback magenta visibile: si/no
Console error: testo eventuale
```

### Test D - NPC modulare

Obiettivo:

```text
verificare che body/head/legs/feet vengano montati
```

Passi:

1. Assegna `Npc Visual Catalog Json`.
2. Metti `Use Layered Actor Catalog = true`.
3. Configura resolver sprite o fallback.
4. Entra in Play Mode.
5. Premi `F12`.
6. Verifica se l'NPC ha piu' parti.

Esito da riportare:

```text
Test D:
Layered actor count > 0: si/no
Created parts > 0: si/no
Missing catalog frames: numero
Missing sprites: numero
Console error: testo eventuale
```

### Test E - Animazione walk

Obiettivo:

```text
verificare che l'NPC cambi frame mentre si muove tra celle
```

Passi:

1. Avvia Play Mode.
2. Passa ad ArcGraph con `F12`.
3. Fai muovere un NPC.
4. Osserva se la camminata cambia frame.

Esito da riportare:

```text
Test E:
NPC si muove fluido tra celle: si/no
Frame walk cambiano: si/no
Direzione corretta: si/no
Console error: testo eventuale
```

---

## 17. Problemi comuni

### 17.1 Premo F12 e non succede nulla

Controllare:

```text
Switcher Enabled = true
Toggle Key = F12
GameObject dello switcher attivo
Play Mode attivo
Console senza errori di compilazione
```

### 17.2 MapGrid sparisce ma ArcGraph non compare

Controllare:

```text
Arc Graph Visual Roots assegnati
Runtime Wrapper assegnato
Terrain Renderer assegnato
NPC Renderer assegnato
Process ArcGraph Frame On Switch = true
Configure Wrapper Rendering On Switch = true
```

### 17.3 ArcGraph compare ma non vedo NPC

Controllare:

```text
NPC Renderer assegnato nello switcher
NPC Renderer Enabled true oppure abilitabile dallo switcher
Allow Generated Fallback Sprites = true
Render queue contiene actor
Console log ArcGraphNpcRuntimeSceneRenderer
```

### 17.4 NPC modulare non appare

Controllare:

```text
Use Layered Actor Catalog = true
Npc Visual Catalog Json assegnato
Sprite resolver assegnato
Enable Resources Lookup = true sul resolver
Sprite key corrette
MissingSprites nel log
MissingCatalogFrames nel log
```

Se mancano sprite reali, il renderer puo' tornare al fallback.

### 17.5 Idle non si anima

Controllare:

```text
Use Layered Actor Catalog = true
idle frame presenti o generati dal catalogo
idleFrameStep > 0
NPC non in motion
```

### 17.6 Walk non si anima

Controllare:

```text
NPC ha motion runtime
MotionProgress01 cambia da 0 a 1
walk frameCount = 9
sprite walk risolvibili
```

### 17.7 Ombra NPC non visibile

Controllare:

```text
Render Actor Shadow = true
Allow Generated Fallback Sprites = true
Actor Shadow Tint alpha > 0
Actor Shadow Sorting Offset sotto le parti NPC
```

Se vuoi usare una sprite ombra reale, controllare anche:

```text
Actor Shadow Sprite Key = ArcGraph/NPC/common/shadow/soft_ellipse_32x16
resolver configurato per quella sprite
oppure PNG presente in Assets/Resources/ArcGraph/NPC/common/shadow/soft_ellipse_32x16.png
```

---

## 18. Resolver sprite automatico da Resources

Ora il percorso consigliato non e' mappare manualmente tutti gli sprite
nell'Inspector.

Il resolver:

```text
ArcGraphSerializedSpriteResolver
```

puo' caricare automaticamente gli sprite da:

```text
Assets/Resources
```

usando la stessa sprite key prodotta dal catalogo.

Esempio:

```text
sprite key:
ArcGraph/NPC/human_default/body/south_idle_00

file PNG:
Assets/Resources/ArcGraph/NPC/human_default/body/south_idle_00.png
```

Regola importante:

```text
il path nel catalogo non contiene ".png"
il file fisico invece contiene ".png"
```

Quindi non devi trascinare manualmente tutti i 208 PNG nel campo `Entries`.

Devi invece:

1. mettere i PNG nei path corretti;
2. controllare che siano importati come `Sprite`;
3. lasciare `Enable Resources Lookup = true`;
4. assegnare il resolver al renderer NPC;
5. usare `Entries` solo per override manuali o test mirati.

Ordine di risoluzione:

```text
1. Entries manuali da Inspector
2. Assets/Resources tramite sprite key
3. Default Actor Sprite / Default Object Sprite
4. Fallback generato dal renderer, se abilitato
```

Se cambi PNG mentre la scena e' aperta, puoi usare il menu contestuale:

```text
ArcGraph/Clear Sprite Resolver Runtime Cache
```

---

## 19. Probe tecnico sprite NPC

Prima di fare il gate visuale completo, puoi verificare se i PNG sono nei path
giusti.

Aggiungi a un GameObject tecnico il componente:

```text
ArcGraphNpcSpriteResourceProbe
```

Configura:

```text
Npc Visual Catalog Json -> ArcGraphNpcVisualCatalog
Sprite Resolver Behaviour -> ArcGraphSerializedSpriteResolver
Log Diagnostics -> true
```

Poi, dal menu contestuale del componente:

```text
ArcGraph/Probe NPC Sprite Resources
```

Esito positivo atteso con set completo:

```text
reason=NpcSpriteResourcesReady
catalogFrames=208
checkedSpriteKeys=208
resolvedSprites=208
missingSprites=0
```

Esito utile se mancano PNG o nomi:

```text
reason=NpcSpriteResourcesIncomplete
missingSprites=...
firstMissing='ArcGraph/NPC/human_default/body/south_idle_00'
```

Se compare una `firstMissing`, controlla:

```text
1. il file PNG esiste davvero;
2. il path parte da Assets/Resources;
3. il nome non contiene estensione nel catalogo;
4. Unity lo ha importato come Sprite;
5. il resolver ha Enable Resources Lookup attivo.
```

---

## 20. Come creare i tile terrain ArcGraph

ArcGraph terrain usa due file JSON distinti:

```text
Assets/Resources/ArcGraph/Config/ArcGraphTerrainCatalog.json
Assets/Resources/ArcGraph/Config/ArcGraphTerrainVisualCatalog.json
```

Il primo dice:

```text
quale tile id esiste
e dove si trova nell'atlas
```

Il secondo dice:

```text
quale terrain type usa quali tile
quali varianti possiede
quali frame animati possiede
quali transizioni di bordo possiede
```

Esempio semplice:

```text
tile id 0 = prato base
tile id 1 = prato con fiori
tile id 2 = prato con ciuffi
tile id 30 = acqua frame 0
tile id 31 = acqua frame 1
```

La mappa non deve ragionare in termini di:

```text
usa lo sprite prato con fiori
```

ma in termini di:

```text
questa cella e' grass
```

Poi ArcGraph decide quale tile visuale usare.

---

## 21. Dimensioni PNG terrain

Dimensione attuale dei tile terreno:

```text
32 x 32 px
```

Formato consigliato:

```text
PNG con trasparenza solo se serve
pixel art pulita
nessun filtro sfumato indesiderato
```

Import Unity consigliato:

```text
Texture Type: Sprite (2D and UI) oppure texture atlas usata da materiale terrain
Pixels Per Unit: 32
Filter Mode: Point
Compression: None
Generate Mip Maps: false
```

Per il terreno ArcGraph la strada attuale e' usare un atlas.

Questo significa:

```text
non un PNG separato per ogni tile terrain
ma una singola immagine atlas con molti tile 32x32
```

Esempio atlas:

```text
tile 0  -> colonna 0, riga 0
tile 1  -> colonna 1, riga 0
tile 2  -> colonna 2, riga 0
tile 10 -> colonna 0, riga 1
tile 20 -> colonna 0, riga 2
```

La posizione del tile nell'atlas viene dichiarata nel catalogo terrain.

---

## 22. ArcGraphTerrainCatalog.json

File:

```text
Assets/Resources/ArcGraph/Config/ArcGraphTerrainCatalog.json
```

Serve a dire ad ArcGraph:

```text
questo tile id si trova in questa cella dell'atlas
```

Struttura concettuale:

```json
{
  "terrainAtlasResourcePath": "MapGrid/Atlas/TerrainAtlas",
  "tilePixels": 32,
  "atlasWidthPixels": 128,
  "atlasHeightPixels": 128,
  "tiles": [
    {
      "id": 0,
      "name": "grass_base",
      "uvX": 0,
      "uvY": 0
    },
    {
      "id": 1,
      "name": "grass_flowers",
      "uvX": 1,
      "uvY": 0
    },
    {
      "id": 2,
      "name": "grass_tufts",
      "uvX": 2,
      "uvY": 0
    }
  ]
}
```

Significato:

```text
id   = numero logico del tile
name = nome leggibile per umani
uvX  = colonna del tile nell'atlas
uvY  = riga del tile nell'atlas
```

Nota importante:

```text
uvY = 0 indica la prima riga in alto dell'immagine atlas
```

Se aggiungi un tile nell'atlas ma non lo registri qui, ArcGraph non sa dove
trovarlo.

---

## 23. ArcGraphTerrainVisualCatalog.json

File:

```text
Assets/Resources/ArcGraph/Config/ArcGraphTerrainVisualCatalog.json
```

Serve a dire:

```text
grass usa questi tile
water usa questi frame
grass vicino a stone_floor usa questi bordi
```

### 23.1 Variante statica

Esempio:

```json
{
  "terrainId": "grass",
  "defaultTileId": 0,
  "variants": [
    {
      "tileId": 0,
      "weight": 70
    },
    {
      "tileId": 1,
      "weight": 20
    },
    {
      "tileId": 2,
      "weight": 10
    }
  ]
}
```

Significato:

```text
grass puo' essere disegnato con tile 0, 1 o 2
tile 0 e' piu' frequente
tile 2 e' piu' raro
```

La scelta e' deterministica per coordinata.

Questo vuol dire:

```text
una cella non cambia variante a ogni frame
```

Esempio:

```text
cella 10,5 -> grass_flowers
rimane grass_flowers anche dopo refresh
```

---

### 23.2 Tile animato

Esempio acqua:

```json
{
  "terrainId": "water",
  "defaultTileId": 30,
  "animation": {
    "frameTileIds": [
      30,
      31,
      32,
      33
    ],
    "frameSeconds": 0.25
  }
}
```

Significato:

```text
water usa 4 frame
cambia frame ogni 0.25 secondi visuali
```

Quindi devi creare nell'atlas:

```text
tile 30 = acqua frame 0
tile 31 = acqua frame 1
tile 32 = acqua frame 2
tile 33 = acqua frame 3
```

Questi tile devono essere registrati anche in:

```text
ArcGraphTerrainCatalog.json
```

---

### 23.3 Transizioni / autotile semplice

Esempio:

```json
{
  "fromTerrainId": "grass",
  "toTerrainId": "stone_floor",
  "rules": [
    {
      "mask": "N",
      "tileId": 21
    },
    {
      "mask": "E",
      "tileId": 20
    },
    {
      "mask": "S",
      "tileId": 23
    },
    {
      "mask": "W",
      "tileId": 22
    }
  ]
}
```

Significato:

```text
se una cella grass confina con stone_floor
ArcGraph puo' usare un tile di bordo
```

Maschere cardinali:

```text
N  = vicino a nord
E  = vicino a est
S  = vicino a sud
W  = vicino a ovest
NE = vicino a nord e a est
SW = vicino a sud e a ovest
```

Esempio pratico:

```text
cella corrente = grass
vicino est = stone_floor
maschera = E
tile scelto = 20
```

Quindi nell'atlas devi avere:

```text
tile 20 = bordo grass verso pietra a est
tile 21 = bordo grass verso pietra a nord
tile 22 = bordo grass verso pietra a ovest
tile 23 = bordo grass verso pietra a sud
```

Nota importante:

```text
la transizione vince sulla variante statica
```

Questo vuol dire:

```text
grass normale -> puo' usare variante prato
grass vicino a stone_floor -> usa tile bordo, se esiste una regola
```

---

## 24. Checklist tile terrain da creare

Per un primo set minimo utile:

```text
grass_base             -> tile 0
grass_variant_flowers  -> tile 1
grass_variant_tufts    -> tile 2
stone_floor_base       -> tile 10
water_frame_00         -> tile 30
water_frame_01         -> tile 31
water_frame_02         -> tile 32
water_frame_03         -> tile 33
grass_to_stone_east    -> tile 20
grass_to_stone_north   -> tile 21
grass_to_stone_west    -> tile 22
grass_to_stone_south   -> tile 23
```

Se vuoi supportare angoli semplici:

```text
grass_to_stone_ne      -> tile 24
grass_to_stone_se      -> tile 25
grass_to_stone_sw      -> tile 26
grass_to_stone_nw      -> tile 27
```

Totale primo pacchetto consigliato:

```text
12 tile senza angoli
16 tile con angoli
```

Tutti:

```text
32x32 px
dentro lo stesso atlas terrain
registrati in ArcGraphTerrainCatalog.json
usati da ArcGraphTerrainVisualCatalog.json
```

---

## 25. Regola pratica per evitare errori

Quando aggiungi un tile terrain, devi aggiornare sempre due livelli:

```text
1. ArcGraphTerrainCatalog.json
   -> dice dove sta il tile nell'atlas

2. ArcGraphTerrainVisualCatalog.json
   -> dice quando quel tile viene usato
```

Esempio:

```text
aggiungo grass_to_stone_east
```

Devo:

```text
1. disegnarlo nell'atlas
2. assegnargli un tile id, ad esempio 20
3. registrare id 20 con uvX/uvY nel TerrainCatalog
4. usarlo nella regola mask E del TerrainVisualCatalog
```

Se manca il punto 3:

```text
ArcGraph sa che dovrebbe usare tile 20
ma non sa dove trovarlo nell'atlas
```

Se manca il punto 4:

```text
ArcGraph conosce tile 20
ma non lo usera' mai
```

---

## 26. Cosa non e' ancora definitivo

Non e' ancora definitivo:

- asset reali definitivi;
- direzione actor esplicita nello snapshot;
- scena ArcGraph separata completa;
- pensionamento fisico di MapGrid;
- UI/debug completa in ArcGraph;
- tool operativi separati da MapGrid.

Questa fase serve a ottenere:

```text
ArcGraph minimo funzionante
-> terrain visibile
-> NPC visibile
-> switch F12
-> base animazioni modulare
```

---

## 27. Riassunto essenziale

Per testare:

1. Aggiungi `ArcGraphViewModeSwitcher` in scena.
2. Assegna root MapGrid da spegnere.
3. Assegna root ArcGraph da accendere.
4. Assegna wrapper, terrain renderer e NPC renderer.
5. Entra in Play Mode.
6. Premi `F12`.
7. Verifica Console.
8. Poi passa agli sprite reali.
9. Usa `ArcGraph/Probe NPC Sprite Resources` per verificare i path PNG.

Per creare gli asset:

```text
4 parti
4 direzioni
idle 4 frame
walk 9 frame
totale 208 PNG
dimensione NPC 32x48
ombra opzionale 32x16
```

Path consigliati:

```text
Assets/Resources/ArcGraph/NPC/human_default/body/south_idle_00.png
Assets/Resources/ArcGraph/NPC/human_default/head/south_idle_00.png
Assets/Resources/ArcGraph/NPC/human_default/legs/south_idle_00.png
Assets/Resources/ArcGraph/NPC/human_default/feet/south_idle_00.png
```

Per cambiare conteggi o nomi:

```text
Assets/Resources/ArcGraph/Config/ArcGraphNpcVisualCatalog.json
```

Per creare i tile terrain:

```text
tile terrain = 32x32
atlas terrain = griglia di tile 32x32
ArcGraphTerrainCatalog.json = tile id -> posizione atlas
ArcGraphTerrainVisualCatalog.json = terrain id -> varianti / animazioni / transizioni
```

Primo set minimo terrain consigliato:

```text
grass base + 2 varianti
stone floor base
water 4 frame
grass/stone 4 bordi cardinali
eventuali 4 angoli
```
