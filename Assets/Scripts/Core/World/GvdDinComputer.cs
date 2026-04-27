using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// GvdDinComputer (v0.03 - Patch 0.03.01.b):
    /// Calcola il Generalized Voronoi Diagram Dinamico su griglia 2D.
    ///
    /// Pipeline:
    ///   1) Distance Transform (DT) — BFS multi-sorgente dai muri.
    ///      Ogni cella walkable riceve: distanza alla parete più vicina,
    ///      e indice dell'ostacolo più vicino (nearest obstacle index).
    ///
    ///   2) GVD condition-based — una cella è sul GVD se due o più
    ///      ostacoli DISTINTI le sono equidistanti (o quasi: tolerance <=1).
    ///      Produce lo scheletro grezzo (pre-pruning).
    ///
    ///   3) Pruning — elimina rami GVD corti (< pruning_min_branch_length celle).
    ///      Un ramo corto indica un dettaglio geometrico irrilevante
    ///      (piccola nicchia, spigolo di muro) non utile per la navigazione.
    ///
    ///   4) Estrazione vertici — i punti del GVD post-pruning dove convergono
    ///      tre o più rami diventano candidati Junction.
    ///      I massimi locali della DT in zone aperte (dt >= area_center_min_dt_value)
    ///      diventano candidati AreaCenter.
    ///
    /// Principi architetturali ARCONTIO:
    /// - Questo computer è OGGETTIVO: lavora sulla mappa globale, non sulla
    ///   memoria soggettiva degli NPC.
    /// - È coerente con LandmarkRegistry (già oggettivo).
    /// - Gli NPC scoprono i landmark soggettivamente via NpcLandmarkMemory.
    /// - Il compute avviene a load-time e dopo ogni build completata.
    ///
    /// Nota commenti in italiano per convenzione del progetto.
    /// </summary>
    public sealed class GvdDinComputer
    {
        // ============================================================
        // COSTANTI E CONFIGURAZIONE
        // ============================================================

        // Sentinella per "nessun ostacolo" nell'array nearest obstacle.
        private const int NO_OBSTACLE = -1;

        // Tolerance GVD: due ostacoli si considerano "equidistanti" se la
        // differenza di distanza è <= questa soglia (in celle).
        // Valore 1 gestisce gli artefatti della griglia discreta.
        private const int GVD_TOLERANCE = 1;

        // ============================================================
        // DATI CALCOLATI (accessibili da LandmarkRegistry per overlay)
        // ============================================================

        // Distance Transform: distanza di ogni cella al muro più vicino.
        // Indicizzato: idx = y * mapWidth + x.
        // Celle muro/fuori bounds = 0.
        public int[] DtValues { get; private set; }

        // Per ogni cella: indice lineare dell'ostacolo più vicino.
        // Usato per la condizione GVD (confronto tra ostacoli vicini).
        private int[] _nearestObstacle;

        // Celle GVD grezze pre-pruning (indici lineari).
        // Una cella è GVD se il suo nearest obstacle è diverso da almeno
        // un vicino cardinale (con tolleranza DT).
        private readonly HashSet<int> _gvdCellsRaw = new HashSet<int>();

        // Celle GVD post-pruning (indici lineari).
        private readonly HashSet<int> _gvdCellsPruned = new HashSet<int>();

        // Vertici GVD post-pruning: candidati landmark.
        // Distinguiamo Junction (>=3 rami GVD che convergono) e AreaCenter
        // (massimi locali della DT in zone aperte).
        public readonly List<GvdVertex> Vertices = new List<GvdVertex>(64);

        // Edge tra vertici GVD (coppie di indici in Vertices).
        public readonly List<GvdEdge> Edges = new List<GvdEdge>(128);

        // Valore DT massimo trovato (usato per normalizzazione overlay heatmap).
        public int DtMax { get; private set; }

        // Dimensioni dell'ultima mappa computata.
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        // ============================================================
        // TIPI INTERNI
        // ============================================================

        /// <summary>
        /// Tipo di vertice GVD estratto dopo il pruning.
        /// </summary>
        public enum GvdVertexKind
        {
            Junction   = 2, // Corrisponde a LandmarkKind.Junction
            AreaCenter = 3, // Nuovo: massimo locale DT in zona aperta
        }

        /// <summary>
        /// Vertice GVD: candidato landmark per LandmarkRegistry.
        /// </summary>
        public sealed class GvdVertex
        {
            public int CellX;
            public int CellY;
            public GvdVertexKind Kind;
            public int DtValue; // Valore DT in questo vertice (= raggio del disco massimo inscrivibile)
        }

        /// <summary>
        /// Edge tra due vertici GVD (indici in Vertices).
        /// </summary>
        public sealed class GvdEdge
        {
            public int FromVertexIdx;
            public int ToVertexIdx;
            public int LengthCells; // Lunghezza del ramo in celle
        }

        // ============================================================
        // COMPUTE PRINCIPALE
        // ============================================================

        /// <summary>
        /// Esegue la pipeline completa GVD-DIN sulla mappa corrente.
        /// Da chiamare in LandmarkRegistry.RebuildFromWorld quando gvd_din.enabled=true.
        ///
        /// Patch 0.03.01.c:
        /// - areaCenterMinSpacingCells: distanza minima Manhattan tra AreaCenter.
        ///   Evita la griglia regolare di massimi locali in stanze grandi.
        /// - ExtractVerticesAndEdges riscritto con pipeline ibrida:
        ///   AreaCenter = massimi locali DT con spacing.
        ///   Junction   = criterio IsJunction del vecchio sistema (diagonali bloccate).
        /// </summary>
        public void Compute(World world, int pruningMinBranchLength, int areaCenterMinDtValue,
                            int areaCenterMinSpacingCells = 5)
        {
            if (world == null) return;

            MapWidth  = world.MapWidth;
            MapHeight = world.MapHeight;

            int cellCount = MapWidth * MapHeight;

            // Alloca/riusa array (evita GC su rebuild successivi).
            if (DtValues == null || DtValues.Length != cellCount)
                DtValues = new int[cellCount];
            if (_nearestObstacle == null || _nearestObstacle.Length != cellCount)
                _nearestObstacle = new int[cellCount];

            _gvdCellsRaw.Clear();
            _gvdCellsPruned.Clear();
            Vertices.Clear();
            Edges.Clear();
            DtMax = 0;

            // Passo 1: Distance Transform.
            ComputeDistanceTransform(world);

            // Passo 2: Rilevamento celle GVD (scheletro grezzo).
            DetectGvdCells(world);

            // Passo 3: Pruning rami corti.
            PruneShortBranches(pruningMinBranchLength);

            // Passo 4: Estrazione vertici e edge.
            ExtractVerticesAndEdges(world, areaCenterMinDtValue, areaCenterMinSpacingCells);
        }

        // ============================================================
        // PASSO 1: DISTANCE TRANSFORM (BFS multi-sorgente)
        // ============================================================

        /// <summary>
        /// BFS multi-sorgente dai muri.
        /// Ogni cella walkable riceve la distanza al muro più vicino e
        /// l'indice lineare dell'ostacolo sorgente.
        ///
        /// Complessità: O(W*H) — un passaggio sulla griglia.
        /// Metrica: Manhattan (4-connessione cardinale).
        ///
        /// Nota: usiamo Manhattan e non Euclidea perché:
        /// - È O(W*H) con BFS vs O(W*H*log) per Euclidea esatta.
        /// - Su griglia 4-connessa produce GVD topologicamente corretti.
        /// - La differenza visiva con Euclidea è trascurabile per mappe tile.
        /// </summary>
        private void ComputeDistanceTransform(World world)
        {
            int w = MapWidth;
            int h = MapHeight;
            int cellCount = w * h;

            // Inizializza tutto a 0 (muri = distanza 0, walkable = da calcolare).
            for (int i = 0; i < cellCount; i++)
            {
                DtValues[i]        = 0;
                _nearestObstacle[i] = NO_OBSTACLE;
            }

            // Coda BFS: partiamo da tutte le celle muro/bordo.
            var queue = new Queue<int>(cellCount / 4);

            // Semina: ogni cella muro è un ostacolo sorgente.
            // Nota: anche le celle fuori bounds sono considerate muro implicito,
            // ma le gestiamo tramite bounds-check nella BFS.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (world.BlocksMovementAt(x, y))
                    {
                        // Cella muro: distanza 0, ostacolo = sé stessa.
                        DtValues[idx]        = 0;
                        _nearestObstacle[idx] = idx;
                        // Non la mettiamo in coda: è già risolta.
                    }
                    else
                    {
                        // Cella walkable: distanza inizialmente "infinita".
                        DtValues[idx]        = int.MaxValue;
                        _nearestObstacle[idx] = NO_OBSTACLE;
                    }
                }
            }

            // Semina seconda passata: le celle walkable adiacenti ai muri
            // ricevono distanza 1 e vengono messe in coda.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (world.BlocksMovementAt(x, y)) continue;

                    // Controllo vicini cardinali: se almeno uno è muro, siamo in frontiera.
                    int wallNeighbor = FindAdjacentWallIndex(world, x, y, w);
                    if (wallNeighbor == NO_OBSTACLE) continue;

                    int idx = y * w + x;
                    DtValues[idx]        = 1;
                    _nearestObstacle[idx] = wallNeighbor;
                    queue.Enqueue(idx);
                }
            }

            // BFS espansione: propaghiamo la distanza verso l'interno.
            int[] dxs = { 0, 0, 1, -1 };
            int[] dys = { 1, -1, 0, 0 };

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                int cx  = cur % w;
                int cy  = cur / w;
                int curDt = DtValues[cur];

                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dxs[d];
                    int ny = cy + dys[d];

                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    if (world.BlocksMovementAt(nx, ny)) continue;

                    int nidx    = ny * w + nx;
                    int newDist = curDt + 1;

                    // Aggiorna solo se troviamo una distanza minore.
                    if (newDist < DtValues[nidx])
                    {
                        DtValues[nidx]        = newDist;
                        _nearestObstacle[nidx] = _nearestObstacle[cur];
                        queue.Enqueue(nidx);
                    }
                }
            }

            // Calcola DtMax per la normalizzazione overlay.
            DtMax = 0;
            for (int i = 0; i < cellCount; i++)
            {
                if (DtValues[i] != int.MaxValue && DtValues[i] > DtMax)
                    DtMax = DtValues[i];
            }
        }

        /// <summary>
        /// Trova l'indice lineare di un vicino cardinale che è muro.
        /// Restituisce NO_OBSTACLE se nessun vicino è muro.
        /// </summary>
        private static int FindAdjacentWallIndex(World world, int x, int y, int w)
        {
            if (x + 1 < world.MapWidth  && world.BlocksMovementAt(x + 1, y)) return y * w + (x + 1);
            if (x - 1 >= 0              && world.BlocksMovementAt(x - 1, y)) return y * w + (x - 1);
            if (y + 1 < world.MapHeight && world.BlocksMovementAt(x, y + 1)) return (y + 1) * w + x;
            if (y - 1 >= 0              && world.BlocksMovementAt(x, y - 1)) return (y - 1) * w + x;
            return NO_OBSTACLE;
        }

        // ============================================================
        // PASSO 2: RILEVAMENTO CELLE GVD (condition-based)
        // ============================================================

        /// <summary>
        /// Una cella è sul GVD se è equidistante (con tolleranza) da due
        /// ostacoli DISTINTI.
        ///
        /// Condizione pratica su griglia:
        /// - Per ogni vicino cardinale, confronta il nearest obstacle.
        /// - Se due vicini hanno ostacoli diversi E la differenza di DT
        ///   tra questa cella e il vicino è <= GVD_TOLERANCE,
        ///   allora questa cella è sul confine tra due zone di influenza.
        ///
        /// Questo è il metodo "condition-based" (vs thinning iterativo):
        /// - Non richiede iterazioni.
        /// - O(W*H) — una passata sulla griglia.
        /// - Produce linee di 1-2 pixel di spessore (gestite dal pruning).
        /// </summary>
        private void DetectGvdCells(World world)
        {
            int w = MapWidth;
            int h = MapHeight;

            int[] dxs = { 0, 0, 1, -1 };
            int[] dys = { 1, -1, 0, 0 };

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (world.BlocksMovementAt(x, y)) continue;

                    int idx = y * w + x;
                    int myObstacle = _nearestObstacle[idx];
                    if (myObstacle == NO_OBSTACLE) continue;

                    // Controlla i vicini cardinali.
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dxs[d];
                        int ny = y + dys[d];

                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        if (world.BlocksMovementAt(nx, ny)) continue;

                        int nidx = ny * w + nx;
                        int nObstacle = _nearestObstacle[nidx];

                        if (nObstacle == NO_OBSTACLE) continue;

                        // Ostacoli distinti = confine tra zone di influenza = GVD.
                        if (nObstacle != myObstacle)
                        {
                            // Tolleranza: accettiamo solo se le distanze sono "compatibili".
                            // Questo filtra i falsi positivi dovuti alla metrica Manhattan discreta.
                            int dtDiff = Mathf.Abs(DtValues[idx] - DtValues[nidx]);
                            if (dtDiff <= GVD_TOLERANCE)
                            {
                                _gvdCellsRaw.Add(idx);
                                break; // Basta un vicino con ostacolo diverso
                            }
                        }
                    }
                }
            }
        }

        // ============================================================
        // PASSO 3: PRUNING RAMI CORTI
        // ============================================================

        /// <summary>
        /// Elimina i rami GVD più corti di pruningMinBranchLength celle.
        ///
        /// Strategia iterativa:
        /// - Un "endpoint" GVD è una cella GVD con un solo vicino GVD (foglia del grafo).
        /// - Rimuoviamo ripetutamente gli endpoint finché non rimangono rami
        ///   abbastanza lunghi.
        /// - Il numero di iterazioni è pruningMinBranchLength.
        ///
        /// Nota: questo è un thinning semplice, non il pruning gerarchico
        /// (skeleton pyramid). È sufficiente per mappe tile con pareti pulite.
        /// Il pruning gerarchico (multiresolution) è overkill per ARCONTIO v0.03.
        /// </summary>
        private void PruneShortBranches(int pruningMinBranchLength)
        {
            // Copia il raw set nel pruned set come punto di partenza.
            _gvdCellsPruned.Clear();
            foreach (int c in _gvdCellsRaw)
                _gvdCellsPruned.Add(c);

            int w = MapWidth;
            int[] dxs = { 0, 0, 1, -1 };
            int[] dys = { 1, -1, 0, 0 };

            // Lista di lavoro per raccogliere gli endpoint da rimuovere
            // senza modificare il set durante l'iterazione.
            var toRemove = new List<int>(64);

            // Itera pruningMinBranchLength volte.
            // Ogni iterazione rimuove uno strato di foglie.
            for (int iter = 0; iter < pruningMinBranchLength; iter++)
            {
                toRemove.Clear();

                foreach (int idx in _gvdCellsPruned)
                {
                    int cx = idx % w;
                    int cy = idx / w;

                    // Conta vicini GVD cardinali.
                    int gvdNeighbors = 0;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + dxs[d];
                        int ny = cy + dys[d];
                        int nidx = ny * w + nx;
                        if (_gvdCellsPruned.Contains(nidx))
                            gvdNeighbors++;
                    }

                    // Endpoint = un solo vicino GVD (o zero, caso isolato).
                    if (gvdNeighbors <= 1)
                        toRemove.Add(idx);
                }

                // Se non ci sono più endpoint, lo scheletro è convergito.
                if (toRemove.Count == 0)
                    break;

                foreach (int r in toRemove)
                    _gvdCellsPruned.Remove(r);
            }
        }

        // ============================================================
        // PASSO 4: ESTRAZIONE VERTICI E EDGE
        // ============================================================

        /// <summary>
        /// Pipeline ibrida per l'estrazione dei candidati landmark (Patch 0.03.01.c).
        ///
        /// ARCHITETTURA FINALE:
        ///
        /// AreaCenter — massimi locali della Distance Transform con spacing minimo.
        ///   Un massimo locale DT è la cella walkable il cui valore DT è >= tutti
        ///   i vicini cardinali. Corrisponde al "punto più lontano da tutte le pareti"
        ///   in una regione — naturalmente il centro geometrico di una stanza.
        ///   Il parametro areaCenterMinDtValue filtra corridoi stretti (DT bassa).
        ///   Il parametro areaCenterMinSpacingCells evita griglie regolari in stanze grandi.
        ///
        /// Junction — criterio IsJunction del vecchio sistema v0.02 (invariato).
        ///   Una cella è Junction se:
        ///   1) Tutte le diagonali sono bloccate (siamo in spazio stretto / corridoio)
        ///   2) Al massimo 1 uscita cardinale è bloccata (incrocio a + o a T)
        ///   Questo rileva solo gli incroci reali in corridoi stretti.
        ///   NON produce Junction sul perimetro di stanze aperte (diagonali libere).
        ///
        /// Edge — costruiti da BuildMinimalEdges in LandmarkRegistry (invariato).
        ///   Scan cardinale da ogni nodo finché non trova un altro nodo o un muro.
        ///
        /// Perché questo approccio:
        ///   Dopo test approfonditi su stanze 8x9, corridoi, biforcazioni T e stanze
        ///   collegate, il GVD puro produce troppe Junction sul perimetro delle stanze
        ///   aperte — non è un bug, è la geometria. La soluzione corretta è separare
        ///   il problema: GVD produce la DT (e gli AreaCenter), IsJunction rileva
        ///   gli incroci reali nei corridoi. I due sistemi sono ortogonali e complementari.
        /// </summary>
        private void ExtractVerticesAndEdges(World world, int areaCenterMinDtValue,
                                             int areaCenterMinSpacingCells)
        {
            int w = MapWidth;
            int h = MapHeight;

            // ----------------------------------------------------------------
            // Passo 4a: AreaCenter — massimi locali DT con spacing
            // ----------------------------------------------------------------
            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    if (world.BlocksMovementAt(x, y)) continue;

                    int dtHere = DtValues[y * w + x];
                    if (dtHere == int.MaxValue || dtHere < areaCenterMinDtValue) continue;

                    // Massimo locale: DT >= tutti i vicini cardinali walkable.
                    bool isLocalMax = true;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + _dxs[d];
                        int ny = y + _dys[d];
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        if (world.BlocksMovementAt(nx, ny)) continue;

                        int nidx = ny * w + nx;
                        if (DtValues[nidx] != int.MaxValue && DtValues[nidx] > dtHere)
                        {
                            isLocalMax = false;
                            break;
                        }
                    }
                    if (!isLocalMax) continue;

                    // Spacing ADATTIVO (Patch 0.03.01.j):
                    // Formula aggiornata: max(areaCenterMinSpacingCells, dtHere * 3)
                    //
                    // Con DT*2 (patch precedente) una stanza 12x10 con DT_max=5
                    // produceva 2 AreaCenter a distanza 7 > spacing(5*2=10? no, 5*2=10 ok)
                    // ma stanze con DT=3 producevano 2 AC a distanza >6.
                    // Con DT*3 lo spacing diventa:
                    //   DT=4 → spacing=12  (stanza media:  1 AC)
                    //   DT=5 → spacing=15  (stanza grande: 1 AC)
                    //   DT=7 → spacing=21  (zona aperta:   pochi AC distanziati)
                    int adaptiveSpacing = Mathf.Max(areaCenterMinSpacingCells, dtHere * 3);

                    bool tooClose = false;
                    for (int vi = 0; vi < Vertices.Count; vi++)
                    {
                        if (Vertices[vi].Kind != GvdVertexKind.AreaCenter) continue;
                        int dist = Mathf.Abs(Vertices[vi].CellX - x)
                                 + Mathf.Abs(Vertices[vi].CellY - y);
                        if (dist < adaptiveSpacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    Vertices.Add(new GvdVertex
                    {
                        CellX   = x,
                        CellY   = y,
                        Kind    = GvdVertexKind.AreaCenter,
                        DtValue = dtHere
                    });
                }
            }

            // ----------------------------------------------------------------
            // Passo 4b: Junction — criterio combinato (Patch 0.03.01.d)
            // ----------------------------------------------------------------
            // Due criteri complementari per corridoi di qualsiasi larghezza:
            //
            // Criterio A — corridoi 1-cella (IsJunction v0.02):
            //   Tutte le diagonali bloccate + al massimo 1 uscita cardinale bloccata.
            //   Funziona per corridoi di 1 cella di larghezza.
            //   NON funziona in spazio aperto (diagonali libere).
            //
            // Criterio B — corridoi larghi (nuovo):
            //   DT <= areaCenterMinDtValue (siamo in zona stretta, non stanza)
            //   >= 3 "direzioni di corridoio": direzioni in cui il DT non cresce
            //   al passo 1 (dt[passo1] <= dt[cella]) E la cella al passo 2 è walkable.
            //   + almeno 2 di queste direzioni sono perpendicolari tra loro.
            //
            // La distinzione chiave del criterio B rispetto agli approcci precedenti:
            //   Sul perimetro di una stanza il DT CRESCE verso l'interno (dt[passo1] > dtH).
            //   In un corridoio il DT resta costante o sale di 1 lungo la direzione principale.
            //   Quindi "dt[passo1] <= dtH" filtra naturalmente il perimetro delle stanze
            //   senza bisogno di soglie esplicite.

            var seenJunctions = new HashSet<int>();

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    if (world.BlocksMovementAt(x, y)) continue;

                    int cellIdx = y * w + x;
                    if (seenJunctions.Contains(cellIdx)) continue;

                    // --- Criterio A: diagonali bloccate (corridoi 1c) ---
                    bool allDiagBlocked = true;
                    for (int dy = -1; dy <= 1; dy += 2)
                    {
                        for (int dx = -1; dx <= 1; dx += 2)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || nx >= w || ny < 0 || ny >= h
                                || !world.BlocksMovementAt(nx, ny))
                            { allDiagBlocked = false; break; }
                        }
                        if (!allDiagBlocked) break;
                    }

                    if (allDiagBlocked)
                    {
                        int cardinalBlocks = 0;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = x + _dxs[d], ny = y + _dys[d];
                            if (nx < 0 || nx >= w || ny < 0 || ny >= h
                                || world.BlocksMovementAt(nx, ny))
                                cardinalBlocks++;
                        }
                        if (cardinalBlocks <= 1)
                        {
                            seenJunctions.Add(cellIdx);
                            Vertices.Add(new GvdVertex
                            {
                                CellX   = x,
                                CellY   = y,
                                Kind    = GvdVertexKind.Junction,
                                DtValue = DtValues[cellIdx]
                            });
                            continue;
                        }
                    }

                    // --- Criterio B: corridoi larghi ---
                    // Patch 0.03.01.h: soglia ridotta a DT==1.
                    // Con DT<=areaCenterMinDtValue (es. 3) le celle DT=2 al centro
                    // di corridoi larghi (4+ celle) producono decine di Junction spurie.
                    // Patch 0.03.01.i: soglia ripristinata a DT<=2.
                    // Con DT<=1 i corridoi 3c non producevano nessun Junction.
                    // Con DT<=2 i corridoi 3c+T producono ~6 Junction che il
                    // merge_radius_gvd in LandmarkRegistry consolida a 1-2.
                    int dtH = DtValues[cellIdx];
                    if (dtH == int.MaxValue || dtH > 2) continue;

                    // Conta le "direzioni di corridoio":
                    // direzioni in cui il DT non cresce al passo 1
                    // (dt[passo1] <= dtH) E il passo 2 è walkable.
                    int corridorDirCount = 0;
                    bool hasPerp = false;
                    int lastCorridorDx = 0, lastCorridorDy = 0;

                    for (int d = 0; d < 4; d++)
                    {
                        int x1 = x + _dxs[d], y1 = y + _dys[d];
                        if (x1 < 0 || x1 >= w || y1 < 0 || y1 >= h) continue;
                        if (world.BlocksMovementAt(x1, y1)) continue;

                        int nidx1 = y1 * w + x1;
                        // Il DT al passo 1 non deve crescere (siamo in corridoio, non in stanza)
                        if (DtValues[nidx1] == int.MaxValue || DtValues[nidx1] > dtH) continue;

                        int x2 = x + 2 * _dxs[d], y2 = y + 2 * _dys[d];
                        if (x2 < 0 || x2 >= w || y2 < 0 || y2 >= h) continue;
                        if (world.BlocksMovementAt(x2, y2)) continue;

                        // Controlla perpendicolarità con l'ultima direzione valida trovata
                        if (corridorDirCount > 0)
                        {
                            if (_dxs[d] * lastCorridorDx + _dys[d] * lastCorridorDy == 0)
                                hasPerp = true;
                        }

                        lastCorridorDx = _dxs[d];
                        lastCorridorDy = _dys[d];
                        corridorDirCount++;
                    }

                    if (corridorDirCount >= 3 && hasPerp)
                    {
                        seenJunctions.Add(cellIdx);
                        Vertices.Add(new GvdVertex
                        {
                            CellX   = x,
                            CellY   = y,
                            Kind    = GvdVertexKind.Junction,
                            DtValue = dtH
                        });
                    }
                }
            }

            // ----------------------------------------------------------------
            // Passo 4c: Edge tra vertici adiacenti lungo corridoi
            // ----------------------------------------------------------------
            // Gli edge definitivi sono costruiti da BuildMinimalEdges in LandmarkRegistry.
            // Qui costruiamo solo gli edge GVD interni (per il debug overlay).
            // Usiamo scan cardinale identico a BuildMinimalEdges.
            BuildGvdEdges(world);
        }

        // Array di direzioni cardinali — condivisi tra i metodi
        private static readonly int[] _dxs = { 0, 0, 1, -1 };
        private static readonly int[] _dys = { 1, -1, 0, 0 };

        /// <summary>
        /// Costruisce gli edge GVD tra vertici per il debug overlay.
        /// Stesso algoritmo di BuildMinimalEdges in LandmarkRegistry:
        /// scan cardinale da ogni vertice finché trova un altro vertice o un muro.
        /// </summary>
        private void BuildGvdEdges(World world)
        {
            int w = MapWidth;
            int h = MapHeight;

            // Indice rapido: cellIdx → vertexIdx
            var vertexByCell = new Dictionary<int, int>(Vertices.Count);
            for (int vi = 0; vi < Vertices.Count; vi++)
            {
                int cidx = Vertices[vi].CellY * w + Vertices[vi].CellX;
                if (!vertexByCell.ContainsKey(cidx))
                    vertexByCell[cidx] = vi;
            }

            // Dedup edge non orientati
            var seen = new HashSet<long>();

            for (int vi = 0; vi < Vertices.Count; vi++)
            {
                int vx = Vertices[vi].CellX;
                int vy = Vertices[vi].CellY;

                for (int d = 0; d < 4; d++)
                {
                    int x = vx;
                    int y = vy;
                    int steps = 0;

                    while (true)
                    {
                        x += _dxs[d];
                        y += _dys[d];
                        steps++;

                        if (x < 0 || x >= w || y < 0 || y >= h) break;
                        if (world.BlocksMovementAt(x, y)) break;

                        int cidx = y * w + x;
                        if (vertexByCell.TryGetValue(cidx, out int otherVi)
                            && otherVi != vi)
                        {
                            int a = System.Math.Min(vi, otherVi);
                            int b = System.Math.Max(vi, otherVi);
                            long key = ((long)a << 32) | (uint)b;
                            if (!seen.Contains(key))
                            {
                                seen.Add(key);
                                Edges.Add(new GvdEdge
                                {
                                    FromVertexIdx = vi,
                                    ToVertexIdx   = otherVi,
                                    LengthCells   = steps
                                });
                            }
                            break;
                        }
                    }
                }
            }
        }

        // ============================================================
        // OVERLAY DATA (per debug view)
        // ============================================================

        /// <summary>
        /// Popola il GvdDinOverlaySnapshot con i dati dell'ultima computazione.
        /// Chiamato da LandmarkRegistry.FillGvdDinOverlayData() → World.GetGvdDinOverlayData().
        ///
        /// Nota: mapWidth è passato esplicitamente per coerenza con l'indice lineare.
        /// </summary>
        public void FillOverlaySnapshot(GvdDinOverlaySnapshot snapshot, int mapWidth)
        {
            if (snapshot == null) return;
            snapshot.Clear();

            if (DtValues == null || _gvdCellsRaw == null)
                return;

            // Calcola DtMax per normalizzazione (se non già calcolato).
            float dtMaxF = DtMax > 0 ? DtMax : 1f;

            // Layer 1: DT heatmap — tutte le celle con DtValue > 0.
            int cellCount = MapWidth * MapHeight;
            for (int idx = 0; idx < cellCount; idx++)
            {
                int dt = DtValues[idx];
                if (dt <= 0 || dt == int.MaxValue) continue;

                int x = idx % mapWidth;
                int y = idx / mapWidth;
                float normalized = Mathf.Clamp01(dt / dtMaxF);

                snapshot.DtCells.Add(new GvdDinOverlayCellDt(x, y, dt, normalized));
            }

            // Layer 2: GVD raw — celle pre-pruning.
            foreach (int idx in _gvdCellsRaw)
            {
                int x = idx % mapWidth;
                int y = idx / mapWidth;
                snapshot.GvdRawCells.Add(new GvdDinOverlayCellGvd(x, y));
            }

            // Layer 3: nodi GVD post-pruning.
            for (int vi = 0; vi < Vertices.Count; vi++)
            {
                var v = Vertices[vi];
                // Kind 2 = Junction, Kind 3 = AreaCenter
                // Mappiamo su LandmarkOverlayNode.Kind per distinguerli visivamente.
                string label = v.Kind == GvdVertexKind.Junction
                    ? $"GJ#{vi}(dt={v.DtValue})"
                    : $"GA#{vi}(dt={v.DtValue})";

                snapshot.GvdNodes.Add(new LandmarkOverlayNode(
                    cellX: v.CellX,
                    cellY: v.CellY,
                    kind: (int)v.Kind,
                    nodeId: vi,
                    label: label));
            }

            // Layer 3 (edge): rami GVD post-pruning.
            for (int ei = 0; ei < Edges.Count; ei++)
            {
                var e  = Edges[ei];
                var vA = Vertices[e.FromVertexIdx];
                var vB = Vertices[e.ToVertexIdx];
                snapshot.GvdEdges.Add(new LandmarkOverlayEdge(
                    vA.CellX, vA.CellY,
                    vB.CellX, vB.CellY,
                    reliability01: 1f));
            }

            snapshot.IsValid = true;
        }
    }
}
