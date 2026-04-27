using Arcontio.Core.Diagnostics;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // LandmarkPerceptionSystem — v0.03.04.c-ComplexEdge_Creation
    // =============================================================================
    /// <summary>
    /// <b>LandmarkPerceptionSystem</b> — apprendimento visivo dei landmark tramite FOV
    /// e creazione di edge soggettivi da percezione visiva.
    ///
    /// <para>
    /// Complementa <c>NotifyNpcMovedForLandmarkLearning</c> (apprendimento fisico):
    /// un NPC impara un landmark anche se lo <b>vede</b> da distanza, senza
    /// doverci camminare sopra.
    /// </para>
    ///
    /// <para><b>Pipeline di visione (Arcontio Core Standard v1.0):</b></para>
    /// <list type="number">
    ///   <item><b>Range gate</b> — Manhattan &lt;= visionRange</item>
    ///   <item><b>Cone gate</b> — <see cref="FovUtils.IsInCone"/></item>
    ///   <item><b>LOS gate</b>  — <c>world.HasLineOfSight</c> (Bresenham)</item>
    /// </list>
    ///
    /// <para><b>Edge soggettivi (v0.03.04.c-ComplexEdge_Creation):</b></para>
    /// <para>
    /// Se <c>landmark_perception.subjective_edges_enabled = true</c>, dopo la scansione
    /// nodi l'NPC inferisce edge tra i landmark visti tramite due meccanismi:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <b>Meccanismo 1 — Simultaneità visiva (priorità):</b>
    ///     due landmark A e B visibili nello stesso tick →
    ///     edge soggettivo diretto se Manhattan(A,B) &lt;= <c>subjective_edge_max_dist</c>.
    ///     Costo = Manhattan(A,B).
    ///   </item>
    ///   <item>
    ///     <b>Meccanismo 2 — Ibrido fisico+visivo (fallback):</b>
    ///     se c'è un recording fisico attivo da nodo A (calpestato) e A NON è visibile
    ///     questo tick (altrimenti Meccanismo 1 lo gestisce già), per ogni nodo B
    ///     visibile crea edge provvisorio A→B.
    ///     Costo = StepCount(passi fisici da A) + Manhattan(npc_pos, B).
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// Il period è configurabile via <c>game_params.json → landmark_perception.period</c>.
    /// Scegliere valori coprimi con il ciclo di IdleScanSystem (12) per garantire
    /// copertura 360° nel tempo. Default 1 (ogni tick).
    /// </para>
    /// </summary>
    public sealed class LandmarkPerceptionSystem : ISystem
    {
        private readonly int _period;

        public int Period => _period;

        // Buffer snapshot NPC ids (evita allocazioni ogni tick)
        private readonly List<int> _npcIds = new(2048);

        // Buffer nodi visibili per NPC corrente (riusato ad ogni NPC, svuotato ogni iterazione)
        private readonly List<int> _visibleNodeIds = new(32);

        public LandmarkPerceptionSystem(int period = 3)
        {
            _period = period < 1 ? 1 : period;
        }

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            // Guard: sistema landmark disabilitato globalmente
            if (!world.Global.EnableLandmarkSystem)
                return;

            // Guard: sottosistema percezione landmark disabilitato nella config
            var lpCfg = world.Config?.Sim?.landmark_perception;
            if (lpCfg != null && !lpCfg.enabled)
                return;

            if (world.LandmarkRegistry == null || world.LandmarkRegistry.ActiveNodesCount == 0)
                return;

            if (world.NpcDna.Count == 0)
                return;

            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0) visionRange = 6;

            bool  useCone   = world.Global.NpcVisionUseCone;
            float coneSlope = world.Global.NpcVisionConeSlope;

            // Parametri edge soggettivi (v0.03.04.c-ComplexEdge_Creation)
            bool  subjectiveEdgesEnabled = lpCfg == null || lpCfg.subjective_edges_enabled;
            int   maxDist    = lpCfg?.subjective_edge_max_dist         ?? 8;
            float reliability = lpCfg?.subjective_edge_base_reliability ?? 0.15f;

            // Snapshot NPC ids (evita iterazioni su Dictionary mentre qualcuno muta lo state)
            _npcIds.Clear();
            foreach (var kv in world.NpcDna)
                _npcIds.Add(kv.Key);

            var nodes    = world.LandmarkRegistry.Nodes;
            var registry = world.LandmarkRegistry;
            int learned      = 0;
            int edgesCreated = 0;

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int npcId = _npcIds[i];

                if (!world.GridPos.TryGetValue(npcId, out var op))
                    continue;

                if (!world.NpcFacing.TryGetValue(npcId, out var facing))
                    facing = CardinalDirection.North;

                int ox = op.X;
                int oy = op.Y;

                // Svuota il buffer dei nodi visibili per questo NPC.
                _visibleNodeIds.Clear();

                // ── SCAN NODI ─────────────────────────────────────────────────
                for (int n = 0; n < nodes.Count; n++)
                {
                    var node = nodes[n];
                    if (!node.IsActive)
                        continue;

                    int nx = node.CellX;
                    int ny = node.CellY;

                    // Range gate: il più economico — elimina subito i landmark lontani.
                    int dist = FovUtils.Manhattan(ox, oy, nx, ny);
                    if (dist <= 0 || dist > visionRange)
                        continue;

                    // Cone gate: esclude landmark dietro o lateralmente fuori cono.
                    // In idle l'NPC ruota in tutte e 4 le direzioni (IdleScanSystem),
                    // quindi la copertura 360° viene garantita nel tempo.
                    if (useCone && !FovUtils.IsInCone(ox, oy, facing, nx, ny, coneSlope))
                        continue;

                    // LOS gate: Bresenham sull'OcclusionMap — applicato per ultimo (più costoso).
                    if (!world.HasLineOfSight(ox, oy, nx, ny))
                        continue;

                    // Notifica: aggiorna il nodo nella memoria soggettiva dell'NPC.
                    world.NotifyNpcSeenLandmark(npcId, node.Id);
                    learned++;

                    // Accumula per i meccanismi di edge soggettivi.
                    if (subjectiveEdgesEnabled)
                        _visibleNodeIds.Add(node.Id);
                }

                // ── EDGE SOGGETTIVI ───────────────────────────────────────────
                if (!subjectiveEdgesEnabled || _visibleNodeIds.Count == 0)
                    continue;

                // Meccanismo 1 — Simultaneità visiva:
                // Due nodi A e B visibili nello stesso tick → edge soggettivo diretto
                // se Manhattan(A,B) <= subjective_edge_max_dist.
                // Costo = Manhattan(A,B) — stima ottimistica (ammissibile per A*).
                for (int a = 0; a < _visibleNodeIds.Count; a++)
                {
                    if (!registry.TryGetActiveNodeById(_visibleNodeIds[a], out var nodeA) || nodeA == null)
                        continue;

                    for (int b = a + 1; b < _visibleNodeIds.Count; b++)
                    {
                        if (!registry.TryGetActiveNodeById(_visibleNodeIds[b], out var nodeB) || nodeB == null)
                            continue;

                        int pairDist = FovUtils.Manhattan(nodeA.CellX, nodeA.CellY, nodeB.CellX, nodeB.CellY);
                        if (pairDist > maxDist)
                            continue;

                        world.NotifyNpcSeenLandmarkPair(npcId, nodeA.Id, nodeB.Id, pairDist, reliability);
                        edgesCreated++;
                    }
                }

                // Meccanismo 2 — Ibrido fisico+visivo:
                // Se esiste un recording fisico attivo da nodo A (calpestato) e A NON è
                // visibile questo tick (se lo fosse, Meccanismo 1 ha già gestito A↔B),
                // crea edge provvisori A→B per ogni nodo B visibile.
                // Costo = StepCount(passi fisici da A) + Manhattan(npc_pos, B).
                // Prerequisito: l'NPC deve aver calpestato fisicamente almeno un landmark
                // (per avviare il recording). Bootstrap puramente visivo non supportato.
                if (!world.NpcComplexEdgeMemories.TryGetValue(npcId, out var complexMem) || complexMem == null)
                    continue;

                if (!complexMem.IsRecording)
                    continue;

                int fromNodeId = complexMem.ActiveRecordingFromNodeId;
                int stepCount  = complexMem.ActiveRecordingStepCount;

                // Se fromNodeId è visibile questo tick, Meccanismo 1 lo ha già coperto
                // per tutte le coppie (fromNodeId, B). Skippa per evitare duplicati.
                if (_visibleNodeIds.Contains(fromNodeId))
                    continue;

                for (int b = 0; b < _visibleNodeIds.Count; b++)
                {
                    int nodeBId = _visibleNodeIds[b];
                    if (nodeBId == fromNodeId) continue;

                    if (!registry.TryGetActiveNodeById(nodeBId, out var nodeB) || nodeB == null)
                        continue;

                    int manDist = FovUtils.Manhattan(ox, oy, nodeB.CellX, nodeB.CellY);
                    int cost    = stepCount + manDist;

                    // Meccanismo 2 → ComplexEdge in NpcComplexEdgeMemory (layer giallo overlay).
                    // Il nodo di partenza è fisicamente calpestato (recording attivo), quindi
                    // l'edge è più fondato di un edge puramente visivo e merita un layer separato.
                    world.NotifyNpcSeenLandmarkPairComplex(npcId, fromNodeId, nodeBId, cost, reliability);
                    edgesCreated++;
                }
            }

            telemetry.Counter("LandmarkPerceptionSystem.LandmarksLearned", learned);
            telemetry.Counter("LandmarkPerceptionSystem.SubjectiveEdgesCreated", edgesCreated);
        }
    }
}
