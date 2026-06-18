# ARCGRAPH_VISUAL_ASSET_POLICY.md - Policy visual asset ArcGraph

Questo documento congela la policy corrente per cataloghi visuali, sprite e
resolver ArcGraph.

La policy non migra asset fisici e non autorizza la cancellazione di MapGrid.
Serve a evitare nuovi path sparsi, fallback impliciti e doppie fonti di verita'
visuale mentre ArcGraph assorbe il renderer legacy.

---

## 1. Regola architetturale principale

ArcGraph non decide cosa esiste nel mondo.

Il flusso corretto resta:

```text
Simulation/Core
-> World / snapshot / ViewModel autorizzati
-> adapter ArcGraph read-only
-> catalogo visuale ArcGraph
-> SpriteKey / sheet key
-> resolver scene-side Unity
-> SpriteRenderer / UI
```

Flussi vietati:

```text
ArcGraph -> World diretto per scegliere cosa esiste
ArcGraph -> mutazione diretta di NPC, piante, oggetti o celle
UI/renderer -> accesso a cache mutabili come fonte autoritativa
fallback automatico ArcGraph -> MapGrid/Sprites
```

---

## 2. Tipi di dato e cataloghi ammessi

### 2.1 Pavimenti e superfici cella

Sorgente autoritativa:

```text
surface_defs.json
map file unico
World.CellSurfaces / CellSurfaceLayer
```

Responsabilita':

- `surface_defs.json` definisce tipi di superficie, categoria macro e dati
  visuali/simulativi di base.
- Il file mappa unico definisce dimensioni e superficie iniziale delle celle.
- ArcGraph legge snapshot/celle gia' preparate, non MapGridData.

Regola:

```text
Il pavimento non deve essere descritto dentro _blocksMovement, _occlusion o
cache oggetti. Quelle sono cache derivate, non cataloghi superficie.
```

### 2.2 Oggetti, muri, porte e costruzioni

Sorgente autoritativa:

```text
object_defs.json
```

Uso previsto:

- oggetti fisici manipolabili;
- muri;
- porte;
- mobili;
- risorse concrete;
- costruzioni;
- oggetti raccoglibili o usabili.

La sezione `Visual` del singolo oggetto deve contenere le informazioni
necessarie ad ArcGraph:

```text
SpritePath o SpriteSheetPath
VisualKind
ResolverKey
dimensioni pixel
pivot
offset
regole di fade/shadow
```

Stato attuale:

- il fallback automatico verso `MapGrid/Sprites/Objects/{defId}` e' vietato;
- alcuni `Visual.SpritePath` puntano ancora esplicitamente a
  `MapGrid/Sprites/Objects/...`;
- questi path sono debito dichiarato, non fallback;
- vanno migrati solo quando esistono asset ArcGraph equivalenti.

Regola futura:

```text
Gli oggetti ArcGraph devono puntare a path ArcGraph reali, preferibilmente sotto:
Assets/Resources/ArcGraph/Objects/...
```

Per oggetti con varianti o animazioni, la forma preferita e':

```text
un PNG sheet per famiglia oggetto
+ sub-sprite Unity sliced
+ chiave ArcGraph nel formato sheet#subSprite
```

Esempio:

```text
ArcGraph/Objects/beds/bed_wood#poor_idle_00
ArcGraph/Objects/walls/wall_stone#wall_stone_1010
```

### 2.3 NPC e parti modulari

Sorgente autoritativa visuale:

```text
ArcGraphNpcVisualCatalog.json
```

Uso previsto:

- visualKey dell'NPC;
- parti modulari;
- direzioni;
- animazioni;
- durata frame;
- sorting offset;
- sprite key da risolvere lato scena.

Stato attuale:

```text
ArcGraphNpcVisualCatalog.json usa pattern per frame separati:
ArcGraph/NPC/{visual}/{part}/{direction}_{animation}_{frame00}
```

Questo e' supportato, ma non e' la forma finale preferita.

Regola futura:

```text
Le animazioni NPC devono convergere verso PNG sheet per parte/animazione, non
verso decine di PNG frame sciolti.
```

Esempio desiderato:

```text
ArcGraph/NPC/human_default/body/idle#south_00
ArcGraph/NPC/human_default/body/idle#south_01
ArcGraph/NPC/human_default/body/walk#west_03
ArcGraph/NPC/human_default/head/idle#north_00
```

La scelta del frame resta data-driven dal catalogo NPC. Il renderer non deve
inventare path hardcoded.

### 2.4 Vegetazione diffusa e piante vive

Le piante vive e la vegetazione biosfera non devono essere infilate in
`object_defs.json`, salvo quando diventano oggetti concreti raccolti o
manipolabili.

Flusso dati scelto:

```text
Biosfera produce:
- speciesKey
- growthStage
- healthBand
- season / biome se necessari
- plantVisualStateKey semantica opzionale

World conserva:
- plantId
- cell
- blocking flags
- visualStateKey o riferimento visuale pianta

ArcGraph risolve:
visualStateKey -> SpriteKey / PNG
```

Catalogo visuale previsto:

```text
ArcGraphEnvironmentVisualCatalog.json
```

Questo catalogo deve contenere almeno due sezioni:

```text
VegetationAreas
Plants
```

Uso previsto:

- `VegetationAreas` per vegetazione diffusa, densita', erba, copertura area;
- `Plants` per PlantInstance concrete: alberi, arbusti, colture, piante
  importanti, stadi crescita e salute.

Esempio concettuale:

```json
{
  "Plants": [
    {
      "VisualStateKey": "oak_adult_healthy_summer",
      "SpeciesKey": "oak",
      "GrowthStage": "adult",
      "HealthBand": "healthy",
      "SpriteKey": "ArcGraph/Environment/Plants/oak#adulthood_summer_healthy"
    }
  ]
}
```

Regola:

```text
object_defs.json = oggetti fisici/manipolabili.
ArcGraphEnvironmentVisualCatalog.json = stati visuali ambiente/biosfera.
```

Quando una pianta produce una risorsa raccolta, la risorsa concreta puo'
diventare oggetto in `object_defs.json`.

---

## 3. Resolver sprite ArcGraph

Il resolver scene-side autorizzato deve restare il bordo tra dati ArcGraph e
Unity asset.

Responsabilita' del resolver:

- ricevere `SpriteKey` testuale;
- risolvere sprite singole da `Resources`;
- risolvere sub-sprite sliced con formato `sheet#subSprite`;
- cache hit/miss;
- fallback dichiarati solo se configurati esplicitamente in Inspector;
- diagnostica controllata, non log per-frame.

Responsabilita' vietate:

- leggere il World;
- decidere quale NPC/oggetto/pianta esiste;
- scegliere asset MapGrid come fallback implicito;
- mutare simulazione;
- generare path nascosti non dichiarati dai cataloghi.

---

## 4. Convenzioni path

Path ArcGraph futuri consigliati:

```text
ArcGraph/Terrain/...
ArcGraph/Objects/...
ArcGraph/NPC/...
ArcGraph/Environment/Plants/...
ArcGraph/Environment/Vegetation/...
ArcGraph/Effects/...
ArcGraph/UI/...
```

Path legacy da non introdurre in nuovi dati ArcGraph:

```text
MapGrid/Sprites/Objects/...
MapGrid/Sprites/NPC...
MapGrid/Atlas/...
```

Eccezione temporanea:

```text
Path MapGrid gia' presenti nei cataloghi restano ammessi solo come debito
esplicito fino alla migrazione asset ArcGraph equivalente.
```

---

## 5. Roadmap tecnica minima

Sequenza consigliata:

1. Congelare questa policy e usarla come vincolo per i prossimi step.
2. Migrare `object_defs.json` da path MapGrid a path ArcGraph reali, solo dopo
   avere asset equivalenti.
3. Estendere `ArcGraphNpcVisualCatalog.json` per supportare sheet per
   parte/animazione senza rompere il formato attuale.
4. Introdurre `ArcGraphEnvironmentVisualCatalog.json` per vegetazione diffusa e
   PlantInstance.
5. Collegare `plantVisualStateKey` / `visualStateKey` al catalogo ambiente.
6. Rimuovere o congelare i vecchi pattern frame-sciolti quando le sheet NPC sono
   realmente disponibili.
7. Eliminare ogni path MapGrid residuo dai cataloghi ArcGraph prima della
   cancellazione fisica di MapGrid.

---

## 6. Stato dichiarato dopo questa policy

- Zoom/pan ArcGraph sono scollegati dal movimento camera MapGrid, ma restano da
  validare manualmente in Unity dopo l'ultimo commit.
- La gestione sprite non e' ancora pienamente unificata.
- I muri sono gia' vicini al modello sheet + resolver.
- Gli oggetti sono catalogati in `object_defs.json`, ma alcuni path restano
  MapGrid espliciti.
- Gli NPC usano ancora frame separati e devono migrare gradualmente a sheet per
  parte/animazione.
- Vegetazione e piante hanno gia' dati Core/biosfera, ma il catalogo visuale
  ArcGraph ambiente va ancora creato.

