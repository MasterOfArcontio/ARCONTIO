// =============================================================================
// LandmarkPathfinder.cs
// Namespace: Arcontio.Core
// Patch: 0.02.06.A
// =============================================================================
//
// MOTIVAZIONE
// ─────────────────────────────────────────────────────────────────────────────
// Prima di questa patch la pianificazione A* della macro-route (landmark graph)
// viveva in World.cs. World deve contenere DATI, non algoritmi di navigazione.
//
// Stessa logica di MovementPathfinder (Patch 0.02.05.B) per la navigazione locale:
// gli algoritmi ricevono World come parametro esplicito e non mantengono stato.
//
// METODI ESTRATTI DA World.cs
// ─────────────────────────────────────────────────────────────────────────────
//   TryResolveStartLandmark       — nodo di partenza nella memoria soggettiva NPC
//   TryResolveTargetLandmark      — nodo di arrivo nella memoria soggettiva NPC
//   TryPlanMacroRoute             — A* sul grafo soggettivo NPC tra due nodi
//   TryPlanMacroRouteForCell      — entry point job-friendly cella→cella
//   RebuildDebugMacroRouteForNpc  — aggiorna NpcMacroRoutes per l'overlay debug
//   BeginMacroRouteExecutionForNpc — pianifica + inizializza stato esecutivo
//   HeuristicCost                 — euristica Manhattan tra due nodi landmark
//   ContainsNode                  — helper O(N) per la open list A*
//   GetScore                      — lettura dizionario con fallback +inf
//   PopLowestF                    — estrae il nodo con f-score minimo
//   ReconstructRoute              — ricostruisce il path da cameFrom
//
// World espone thin wrapper pubblici per compatibilità con i consumer esistenti.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// <b>LandmarkPathfinder</b> — pianificazione A* della macro-route su grafo landmark.
    ///
    /// <para>
    /// Classe statica: zero stato, zero allocazioni proprie.
    /// Tutti i metodi ricevono <see cref="World"/> come parametro esplicito.
    /// </para>
    ///
    /// <para><b>Grafo su cui opera:</b></para>
    /// <list type="bullet">
    ///   <item><b>Nodi:</b> landmark conosciuti dall'NPC (<c>NpcLandmarkMemory.KnownLandmarks</c>).
    ///   Subset soggettivo del <c>LandmarkRegistry</c> globale — l'NPC non può usare
    ///   un landmark che non ha mai visitato fisicamente.</item>
    ///   <item><b>Edge:</b> connessioni conosciute dall'NPC — due fonti combinate:
    ///   <c>NpcLandmarkMemory.KnownEdges</c> (edge semplici dal registry) e
    ///   <c>NpcComplexEdgeMemory</c> (edge percorsi fisicamente, non nel registry).
    ///   Peso = CostCells/BaseCost + penalità reliability.</item>
    /// </list>
    ///
    /// <para><b>Patch:</b> 0.02.06.A</para>
    /// </summary>
    public static class LandmarkPathfinder
    {
        // =====================================================================
        // RISOLUZIONE START / TARGET LANDMARK
        // =====================================================================

        /// <summary>
        /// Trova il nodo landmark di partenza per la macro-route dell'NPC.
        ///
        /// <para>
        /// Opera SOLO sulla memoria soggettiva dell'NPC (<see cref="NpcLandmarkMemory"/>),
        /// non sul <see cref="LandmarkRegistry"/> globale. Un NPC non può usare un
        /// landmark che non ha mai percorso.
        /// </para>
        ///
        /// <para><b>Strategia di risoluzione (in ordine di priorità):</b></para>
        /// <list type="number">
        ///   <item>
        ///     Se la cella corrente dell'NPC coincide con un landmark attivo nel registry
        ///     <b>E</b> l'NPC lo conosce nella sua memoria → quel nodo è lo start.
        ///     Questo è il caso ottimale: la partenza è precisa, senza approssimazioni.
        ///   </item>
        ///   <item>
        ///     Altrimenti, <c>TryFindNearestKnownLandmark</c> scansiona la memoria
        ///     soggettiva e sceglie il nodo noto con distanza Manhattan minima
        ///     dalla posizione corrente. Questo introduce una piccola approssimazione:
        ///     l'NPC "cammina mentalmente" verso il landmark più vicino prima di
        ///     iniziare la macro-route vera.
        ///   </item>
        /// </list>
        ///
        /// <para><b>Possibili failReason:</b>
        ///   <c>LandmarkSystemDisabled</c>, <c>NoLandmarkRegistry</c>,
        ///   <c>NoKnownLandmarks</c>, <c>NoResolvableStartLandmark</c>
        /// </para>
        /// </summary>
        public static bool TryResolveStartLandmark(
            World world,
            int npcId,
            int currentX, int currentY,
            out int startNodeId,
            out string failReason)
        {
            startNodeId = 0;
            failReason  = string.Empty;

            // Guard: sistema landmark abilitato in config?
            if (!world.Global.EnableLandmarkSystem)
            {
                failReason = "LandmarkSystemDisabled";
                return false;
            }

            // Guard: registry costruito (avviene durante bootstrap della mappa)?
            if (world.LandmarkRegistry == null)
            {
                failReason = "NoLandmarkRegistry";
                return false;
            }

            // Guard: l'NPC ha almeno un landmark nella memoria soggettiva?
            if (!world.NpcLandmarkMemory.TryGetValue(npcId, out var mem)
                || mem == null || mem.KnownLandmarksCount <= 0)
            {
                failReason = "NoKnownLandmarks";
                return false;
            }

            // Caso 1: l'NPC è già sopra un nodo che conosce → lo usa come start.
            // TryGetActiveNodeIdAtCell è O(1) via indice interno del registry.
            if (world.LandmarkRegistry.TryGetActiveNodeIdAtCell(currentX, currentY, out int nodeAtCell)
                && mem.ContainsLandmark(nodeAtCell))
            {
                startNodeId = nodeAtCell;
                return true;
            }

            // Caso 2: sceglie il landmark noto più vicino (Manhattan sulla memoria soggettiva).
            if (mem.TryFindNearestKnownLandmark(world.LandmarkRegistry, currentX, currentY, out int nearest))
            {
                startNodeId = nearest;
                return true;
            }

            failReason = "NoResolvableStartLandmark";
            return false;
        }

        /// <summary>
        /// Trova il nodo landmark di arrivo per la macro-route dell'NPC.
        ///
        /// <para>
        /// Stessa logica di <see cref="TryResolveStartLandmark"/>, applicata alla
        /// cella di destinazione invece che alla posizione corrente.
        /// </para>
        ///
        /// <para>
        /// Il nodo target non deve coincidere con la cella finale:
        /// è il landmark più vicino al target da cui si esegue il "last mile"
        /// (l'ultimo tratto diretto/locale verso la cella esatta).
        /// </para>
        ///
        /// <para><b>Possibili failReason:</b> stesse di TryResolveStartLandmark.</para>
        /// </summary>
        public static bool TryResolveTargetLandmark(
            World world,
            int npcId,
            int targetX, int targetY,
            out int targetNodeId,
            out string failReason)
        {
            targetNodeId = 0;
            failReason   = string.Empty;

            if (!world.Global.EnableLandmarkSystem) { failReason = "LandmarkSystemDisabled"; return false; }
            if (world.LandmarkRegistry == null)      { failReason = "NoLandmarkRegistry";     return false; }

            if (!world.NpcLandmarkMemory.TryGetValue(npcId, out var mem)
                || mem == null || mem.KnownLandmarksCount <= 0)
            {
                failReason = "NoKnownLandmarks";
                return false;
            }

            // Caso 1: la cella target è esattamente un landmark noto → nodo finale preciso.
            if (world.LandmarkRegistry.TryGetActiveNodeIdAtCell(targetX, targetY, out int nodeAtCell)
                && mem.ContainsLandmark(nodeAtCell))
            {
                targetNodeId = nodeAtCell;
                return true;
            }

            // Caso 2: sceglie il landmark noto più vicino alla destinazione.
            if (mem.TryFindNearestKnownLandmark(world.LandmarkRegistry, targetX, targetY, out int nearest))
            {
                targetNodeId = nearest;
                return true;
            }

            failReason = "NoResolvableTargetLandmark";
            return false;
        }

        // =====================================================================
        // A* SUL GRAFO SOGGETTIVO NPC
        // =====================================================================

        /// <summary>
        /// Pianifica la macro-route tra due nodi landmark con A* sul grafo soggettivo NPC.
        ///
        /// <para><b>Algoritmo A* su grafo sparso:</b></para>
        /// <list type="number">
        ///   <item>Open list inizializzata con il solo nodo start.</item>
        ///   <item>Ad ogni iterazione: <c>PopLowestF</c> estrae il nodo con f-score minimo.</item>
        ///   <item>Se è il target: <c>ReconstructRoute</c> ricostruisce il path e ritorna.</item>
        ///   <item>Espansione vicini: <c>FillKnownNeighbors</c> legge SOLO gli edge
        ///         che l'NPC conosce dalla memoria soggettiva, non il registry globale.</item>
        ///   <item>Aggiornamento g/f-score se il nuovo percorso è migliore del precedente.</item>
        /// </list>
        ///
        /// <para><b>Funzione di costo edge:</b></para>
        /// <para>
        /// <c>f(n) = g(n) + h(n)</c> dove:
        /// </para>
        /// <list type="bullet">
        ///   <item><c>g(n)</c> = costo accumulato = Σ(CostCells + reliabilityPenalty) lungo il path.</item>
        ///   <item><c>reliabilityPenalty = (1 − reliability01) × 2</c>.
        ///         Penalizza edge poco affidabili (non percorsi da molto):
        ///         un edge con reliability 0.5 aggiunge 1.0 al costo, uno con 0.0 aggiunge 2.0.</item>
        ///   <item><c>h(n)</c> = euristica Manhattan tra i centroidi dei nodi.
        ///         Ammissibile: non sovrastima mai il costo reale → A* è ottimale.</item>
        /// </list>
        ///
        /// <para><b>Limiti intenzionali (manifesto Arcontio):</b></para>
        /// <para>
        /// Se il grafo soggettivo è disconnesso (l'NPC non ha mai percorso un corridoio),
        /// il planning fallisce con <c>NoMacroRoute</c>. Questo è il comportamento CORRETTO:
        /// l'NPC non può pianificare attraverso percorsi che non conosce.
        /// </para>
        ///
        /// <para><b>Possibili FailureReason nel piano prodotto:</b>
        ///   <c>LandmarkSystemDisabled</c>, <c>NoLandmarkRegistry</c>, <c>NoLandmarkMemory</c>,
        ///   <c>InvalidEndpoint</c>, <c>EndpointNotKnown</c>, <c>NoMacroRoute</c>
        /// </para>
        /// </summary>
        /// <param name="world">Il mondo simulato.</param>
        /// <param name="npcId">ID dell'NPC (per accedere alla sua memoria soggettiva).</param>
        /// <param name="startNodeId">ID nodo di partenza (già risolto da TryResolveStartLandmark).</param>
        /// <param name="targetNodeId">ID nodo di arrivo (già risolto da TryResolveTargetLandmark).</param>
        /// <param name="plan">Piano prodotto: lista di nodeId da percorrere in ordine start→target.</param>
        public static bool TryPlanMacroRoute(
            World world,
            int npcId,
            int startNodeId,
            int targetNodeId,
            out NpcMacroRoutePlan plan)
        {
            plan = new NpcMacroRoutePlan
            {
                StartNodeId   = startNodeId,
                TargetNodeId  = targetNodeId,
                Succeeded     = false,
                FailureReason = string.Empty,
            };

            if (!world.Global.EnableLandmarkSystem) { plan.FailureReason = "LandmarkSystemDisabled"; return false; }
            if (world.LandmarkRegistry == null)      { plan.FailureReason = "NoLandmarkRegistry";     return false; }

            if (!world.NpcLandmarkMemory.TryGetValue(npcId, out var mem) || mem == null)
            {
                plan.FailureReason = "NoLandmarkMemory";
                return false;
            }

            // Endpoint validi: ID non nullo e noti all'NPC.
            if (startNodeId == 0 || targetNodeId == 0)
            {
                plan.FailureReason = "InvalidEndpoint";
                return false;
            }

            if (!mem.ContainsLandmark(startNodeId) || !mem.ContainsLandmark(targetNodeId))
            {
                plan.FailureReason = "EndpointNotKnown";
                return false;
            }

            // Caso degenere: start == target, il path è già completo con un solo nodo.
            if (startNodeId == targetNodeId)
            {
                plan.NodeIds.Add(startNodeId);
                plan.Succeeded = true;
                return true;
            }

            // ── A* ───────────────────────────────────────────────────────────
            // open:      nodi candidati all'espansione, ordinati per f-score (via PopLowestF)
            // closed:    nodi già espansi (non vengono riesplorati)
            // cameFrom:  per ogni nodo, da quale nodo siamo arrivati (ricostruzione path)
            // gScore:    costo accumulato dallo start a questo nodo
            // fScore:    gScore + euristica (stima costo totale attraverso questo nodo)
            var open           = new List<int>(16) { startNodeId };
            var cameFrom       = new Dictionary<int, int>(32);
            var gScore         = new Dictionary<int, float>(32) { [startNodeId] = 0f };
            var fScore         = new Dictionary<int, float>(32) { [startNodeId] = HeuristicCost(world, startNodeId, targetNodeId) };
            var neighborBuffer = new List<NpcLandmarkMemory.KnownNeighbor>(8);
            var closed         = new HashSet<int>();

            // v0.03.04.b — ComplexEdge: edge percorsi fisicamente ma assenti nel registry.
            // Recuperato una sola volta prima del loop per evitare lookup ripetuti.
            world.NpcComplexEdgeMemories.TryGetValue(npcId, out var complexMem);

            while (open.Count > 0)
            {
                // Estrai il nodo con f-score minimo dalla open list.
                int current = PopLowestF(open, fScore);

                // Target raggiunto: ricostruisce e ritorna il path completo.
                if (current == targetNodeId)
                {
                    ReconstructRoute(current, cameFrom, plan.NodeIds);
                    plan.Succeeded     = true;
                    plan.FailureReason = string.Empty;
                    return true;
                }

                closed.Add(current);

                // FillKnownNeighbors scansiona la memoria SOGGETTIVA dell'NPC.
                // Restituisce solo i vicini che l'NPC ha imparato tramite esperienza
                // (movimento fisico), non tutti i vicini nel registry globale.
                mem.FillKnownNeighbors(current, neighborBuffer);

                // v0.03.04.b — ComplexEdge: appende i vicini degli edge fisicamente percorsi.
                // Non svuota il buffer: i vicini ComplexEdge si accodano a quelli semplici.
                // Se un nodo appare in entrambe le fonti, l'A* usa automaticamente il costo
                // minore tramite il confronto tentativeG >= GetScore(gScore, nb.NodeId).
                complexMem?.FillKnownComplexNeighbors(current, mem, neighborBuffer);

                for (int i = 0; i < neighborBuffer.Count; i++)
                {
                    var nb = neighborBuffer[i];

                    // Non riesploriamo nodi già chiusi.
                    if (closed.Contains(nb.NodeId))
                        continue;

                    // Costo dell'edge: distanza in celle + penalità per bassa reliability.
                    // reliability01 diminuisce col tempo se l'edge non viene rinforzato.
                    // Un edge poco affidabile è potenzialmente bloccato → costa di più.
                    float reliabilityPenalty = (1f - Mathf.Clamp01(nb.Reliability01)) * 2f;
                    float tentativeG         = GetScore(gScore, current)
                                               + Mathf.Max(1, nb.CostCells)
                                               + reliabilityPenalty;

                    // Se il vicino non è ancora nell'open list, aggiungilo.
                    // Se è già nell'open list con g-score uguale o migliore, salta.
                    if (!ContainsNode(open, nb.NodeId))
                        open.Add(nb.NodeId);
                    else if (tentativeG >= GetScore(gScore, nb.NodeId))
                        continue;

                    // Aggiorna il miglior percorso noto verso questo vicino.
                    cameFrom[nb.NodeId] = current;
                    gScore[nb.NodeId]   = tentativeG;
                    fScore[nb.NodeId]   = tentativeG + HeuristicCost(world, nb.NodeId, targetNodeId);
                }
            }

            // Open list esaurita: grafo soggettivo disconnesso tra start e target.
            plan.FailureReason = "NoMacroRoute";
            return false;
        }

        // =====================================================================
        // ENTRY POINT JOB-FRIENDLY
        // =====================================================================

        /// <summary>
        /// Pianifica la macro-route dall'NPC verso una cella target, risolvendo
        /// automaticamente start e target landmark prima di eseguire A*.
        ///
        /// <para>
        /// Entry point principale per <c>MovementSystem.InitializeNavigation</c>.
        /// Combina TryResolveStartLandmark + TryResolveTargetLandmark + TryPlanMacroRoute
        /// in un'unica chiamata coerente e atomica.
        /// </para>
        ///
        /// <para><b>Flusso:</b></para>
        /// <list type="number">
        ///   <item>Legge la posizione corrente dell'NPC da <c>world.GridPos</c>.</item>
        ///   <item>TryResolveStartLandmark → nodo start.</item>
        ///   <item>TryResolveTargetLandmark → nodo target.</item>
        ///   <item>TryPlanMacroRoute → A*.</item>
        ///   <item>Scrive TargetCellX/Y nel piano per il last-mile dell'esecuzione.</item>
        /// </list>
        /// </summary>
        public static bool TryPlanMacroRouteForCell(
            World world,
            int npcId,
            int targetX, int targetY,
            out NpcMacroRoutePlan plan)
        {
            plan = new NpcMacroRoutePlan
            {
                TargetCellX   = targetX,
                TargetCellY   = targetY,
                Succeeded     = false,
                FailureReason = string.Empty,
            };

            if (!world.GridPos.TryGetValue(npcId, out var pos))
            {
                plan.FailureReason = "NpcHasNoGridPos";
                return false;
            }

            if (!TryResolveStartLandmark(world, npcId, pos.X, pos.Y, out int startNodeId, out string startFail))
            {
                plan.FailureReason = startFail;
                return false;
            }

            if (!TryResolveTargetLandmark(world, npcId, targetX, targetY, out int targetNodeId, out string targetFail))
            {
                plan.FailureReason = targetFail;
                return false;
            }

            if (!TryPlanMacroRoute(world, npcId, startNodeId, targetNodeId, out var innerPlan))
            {
                // Anche in caso di fallimento propaga le coordinate del target
                // affinché l'overlay possa mostrare la destinazione nella debug card.
                innerPlan.TargetCellX = targetX;
                innerPlan.TargetCellY = targetY;
                plan = innerPlan;
                return false;
            }

            innerPlan.TargetCellX = targetX;
            innerPlan.TargetCellY = targetY;
            plan = innerPlan;
            return true;
        }

        // =====================================================================
        // DEBUG / OVERLAY HELPERS
        // =====================================================================

        /// <summary>
        /// Aggiorna <c>world.NpcMacroRoutes[npcId]</c> con il piano più recente.
        ///
        /// <para>
        /// Scrive il piano nel dizionario anche in caso di fallimento
        /// (con <c>plan.Succeeded = false</c>): l'overlay debug può così
        /// mostrare il motivo del fallimento nella card dell'NPC.
        /// </para>
        /// </summary>
        public static void RebuildDebugMacroRouteForNpc(
            World world,
            int npcId,
            int targetX, int targetY)
        {
            if (!world.ExistsNpc(npcId))
                return;

            // Pianifica (successo o fallimento) e scrivi sempre il risultato.
            // L'overlay legge questo dizionario ogni frame per aggiornare la card.
            TryPlanMacroRouteForCell(world, npcId, targetX, targetY, out var plan);
            world.NpcMacroRoutes[npcId] = plan;
        }

        /// <summary>
        /// Pianifica la macro-route E inizializza lo stato esecutivo per un NPC.
        ///
        /// <para>
        /// Chiamato da <c>MovementSystem.InitializeNavigation</c> quando l'NPC
        /// deve intraprendere un viaggio che richiede la navigazione landmark.
        /// </para>
        ///
        /// <para><b>Le due operazioni distinte:</b></para>
        /// <list type="number">
        ///   <item>
        ///     <b>Planning A*</b> (<see cref="RebuildDebugMacroRouteForNpc"/>):
        ///     produce la lista di nodi da visitare in ordine (NpcMacroRoutes).
        ///   </item>
        ///   <item>
        ///     <b>Inizializzazione esecutiva</b> (<c>PathfindingState.BeginMacroRouteExecution</c>):
        ///     imposta il primo waypoint intermedio (ImmediateTarget), salta il primo nodo
        ///     se l'NPC ci è già sopra, configura il flag IsDoingLastMile.
        ///   </item>
        /// </list>
        /// </summary>
        public static void BeginMacroRouteExecutionForNpc(
            World world,
            int npcId,
            int targetX, int targetY)
        {
            if (!world.ExistsNpc(npcId))
                return;

            // Step 1: pianifica e aggiorna NpcMacroRoutes (anche in caso di fallimento,
            // per tenere aggiornato l'overlay debug).
            RebuildDebugMacroRouteForNpc(world, npcId, targetX, targetY);

            // Step 2: posizione corrente dell'NPC — serve a BeginMacroRouteExecution
            // per decidere se l'NPC è già sopra il primo nodo (skip immediato).
            world.GridPos.TryGetValue(npcId, out var pos);

            // Step 3: inizializza lo stato esecutivo in PathfindingState.
            // Configura NextRouteNodeIndex, ImmediateTargetX/Y, IsDoingLastMile.
            world.Pathfinding.BeginMacroRouteExecution(
                npcId, targetX, targetY,
                pos,
                world.NpcMacroRoutes,
                world.LandmarkRegistry);
        }

        // =====================================================================
        // HELPER PRIVATI A*
        // =====================================================================

        /// <summary>
        /// Euristica A*: distanza Manhattan tra i centroidi di due nodi landmark.
        ///
        /// <para>
        /// Ammissibile (non sovrastima mai il costo reale): garantisce che A*
        /// trovi sempre il path ottimale nel grafo soggettivo.
        /// Ritorna 0f se uno dei nodi non è attivo (fallback sicuro: A* senza euristica
        /// per quel nodo, non produce path sbagliati ma può essere meno efficiente).
        /// </para>
        /// </summary>
        private static float HeuristicCost(World world, int aNodeId, int bNodeId)
        {
            if (world.LandmarkRegistry == null) return 0f;
            if (!world.LandmarkRegistry.TryGetActiveNodeById(aNodeId, out var a) || a == null) return 0f;
            if (!world.LandmarkRegistry.TryGetActiveNodeById(bNodeId, out var b) || b == null) return 0f;
            return Mathf.Abs(a.CellX - b.CellX) + Mathf.Abs(a.CellY - b.CellY);
        }

        /// <summary>
        /// Verifica se un nodeId è già presente nella open list (O(N)).
        ///
        /// <para>
        /// Accettabile: il grafo soggettivo NPC ha pochi nodi
        /// (cap maxLandmarksPerNpc tipicamente 32-64). Per grafi più grandi
        /// si userebbe una HashSet parallela, ma l'overhead non vale la complessità aggiunta.
        /// </para>
        /// </summary>
        private static bool ContainsNode(List<int> list, int nodeId)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == nodeId) return true;
            return false;
        }

        /// <summary>
        /// Legge il punteggio di un nodo dal dizionario A*.
        /// Ritorna <c>float.PositiveInfinity</c> se il nodo non è stato ancora visitato:
        /// semantica corretta perché un nodo non visto ha costo "infinito",
        /// peggiore di qualsiasi percorso reale.
        /// </summary>
        private static float GetScore(Dictionary<int, float> map, int nodeId)
            => map.TryGetValue(nodeId, out var v) ? v : float.PositiveInfinity;

        /// <summary>
        /// Estrae dalla open list il nodo con f-score minimo e lo rimuove.
        ///
        /// <para>
        /// Implementazione lineare O(N): per i grafi piccoli di Arcontio è più
        /// efficiente di una priority queue heap (overhead allocazione non vale).
        /// </para>
        ///
        /// <para>PRECONDIZIONE: openList non deve essere vuota.</para>
        /// </summary>
        private static int PopLowestF(List<int> openList, Dictionary<int, float> scores)
        {
            int   bestIdx   = 0;
            float bestScore = GetScore(scores, openList[0]);

            for (int i = 1; i < openList.Count; i++)
            {
                float s = GetScore(scores, openList[i]);
                if (s < bestScore) { bestScore = s; bestIdx = i; }
            }

            int node = openList[bestIdx];
            openList.RemoveAt(bestIdx);
            return node;
        }

        /// <summary>
        /// Ricostruisce il path A* percorrendo a ritroso il dizionario cameFrom,
        /// poi inverte per ottenere start → target.
        ///
        /// <para>
        /// Al termine, <paramref name="outNodeIds"/> contiene la sequenza ordinata
        /// di nodeId da start a target (inclusi entrambi gli estremi).
        /// </para>
        /// </summary>
        private static void ReconstructRoute(
            int currentNodeId,
            Dictionary<int, int> parents,
            List<int> outNodeIds)
        {
            outNodeIds.Clear();
            outNodeIds.Add(currentNodeId);

            // Risale la catena cameFrom fino al nodo start (che non ha parent).
            while (parents.TryGetValue(currentNodeId, out int parent))
            {
                currentNodeId = parent;
                outNodeIds.Add(currentNodeId);
            }

            // Il path è stato costruito al contrario (target → start): invertiamo.
            outNodeIds.Reverse();
        }
    }
}
