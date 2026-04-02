using Arcontio.Core.Diagnostics;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // LandmarkPerceptionSystem — v0.03.04.a
    // =============================================================================
    /// <summary>
    /// <b>LandmarkPerceptionSystem</b> — apprendimento visivo dei landmark tramite FOV.
    ///
    /// <para>
    /// Complementa <c>NotifyNpcMovedForLandmarkLearning</c> (apprendimento fisico):
    /// un NPC impara un landmark anche se lo <b>vede</b> da distanza, senza
    /// doverci camminare sopra. Questo riduce il numero di situazioni in cui
    /// l'NPC ignora landmark visibili davanti a sé perché non li ha ancora calpestati.
    /// </para>
    ///
    /// <para><b>Pipeline di rilevamento (landmark — senza cono):</b></para>
    /// <list type="number">
    ///   <item><b>Range gate</b> — Manhattan &lt;= visionRange</item>
    ///   <item><b>LOS gate</b>  — <c>world.HasLineOfSight</c> (Bresenham)</item>
    /// </list>
    ///
    /// <para>
    /// <b>Perché non c'è il cone gate:</b> i landmark sono feature topologiche statiche
    /// (porte, junction, area center). L'NPC li percepisce per prossimità in tutte le
    /// direzioni, non solo davanti a sé. Il cono direzionale è corretto per NPC e oggetti
    /// (entità da "guardare"), non per strutture architetturali ambientali.
    /// </para>
    ///
    /// <para>
    /// Per ogni landmark visibile chiama <c>world.NotifyNpcSeenLandmark</c>
    /// che aggiorna solo il nodo nella memoria soggettiva dell'NPC (nessun edge,
    /// nessun path recording, nessun aggiornamento di LastVisitedLandmarkId).
    /// </para>
    ///
    /// <para>
    /// Il period è configurabile via <c>game_params.json → landmark_perception.period</c>
    /// (default 3). I landmark non si spostano: scansionare ogni tick è ridondante.
    /// </para>
    /// </summary>
    public sealed class LandmarkPerceptionSystem : ISystem
    {
        private readonly int _period;

        public int Period => _period;

        // Buffer per snapshot NPC ids (evita allocazioni ogni tick)
        private readonly List<int> _npcIds = new(2048);

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

            if (world.NpcCore.Count == 0)
                return;

            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0) visionRange = 6;

            // Snapshot NPC ids (evita iterazioni su Dictionary mentre qualcuno muta lo state)
            _npcIds.Clear();
            foreach (var kv in world.NpcCore)
                _npcIds.Add(kv.Key);

            var nodes = world.LandmarkRegistry.Nodes;
            int learned = 0;

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int npcId = _npcIds[i];

                if (!world.GridPos.TryGetValue(npcId, out var op))
                    continue;

                int ox = op.X;
                int oy = op.Y;

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

                    // LOS gate: Bresenham sull'OcclusionMap — applicato per ultimo (più costoso).
                    // Nota: nessun cone gate — i landmark sono feature topologiche statiche
                    // (porte, junction, area center). L'NPC li percepisce per prossimità
                    // in tutte le direzioni, non per linea di vista direzionale come NPC/oggetti.
                    if (!world.HasLineOfSight(ox, oy, nx, ny))
                        continue;

                    // Notifica: aggiorna SOLO il nodo nella memoria soggettiva dell'NPC.
                    world.NotifyNpcSeenLandmark(npcId, node.Id);
                    learned++;
                }
            }

            telemetry.Counter("LandmarkPerceptionSystem.LandmarksLearned", learned);
        }
    }
}
