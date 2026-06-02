using Arcontio.Core.Diagnostics;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // NpcPerceptionSystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sistema di percezione visiva degli altri NPC.
    /// </para>
    ///
    /// <para><b>Percezione soggettiva senza scansione globale</b></para>
    /// <para>
    /// Per ogni NPC osservatore, valuta quali altri NPC sono visibili e produce un
    /// <c>NpcSpottedEvent</c>. Il sistema usa un indice temporaneo cella -> NPC per
    /// evitare il vecchio controllo globale tutti-contro-tutti. La semantica visiva
    /// resta identica: range, cono e linea di vista sono ancora i gate canonici.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Snapshot NPC</b>: copia gli id NPC per evitare mutazioni durante l'iterazione.</item>
    ///   <item><b>Indice per cella</b>: ricostruito ogni tick da <c>world.GridPos</c>.</item>
    ///   <item><b>Celle candidate</b>: per ogni osservatore visita solo il raggio visivo.</item>
    ///   <item><b>Gate visivi</b>: distanza Manhattan, cono opzionale e linea di vista.</item>
    /// </list>
    /// </summary>
    public sealed class NpcPerceptionSystem : ISystem
    {
        public int Period => 1;

        private readonly List<int> _npcIds = new(2048);
        private readonly List<long> _occupiedCellKeys = new(2048);
        private readonly Dictionary<long, List<int>> _npcIdsByCell = new(2048);

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna la percezione NPC -> NPC per il tick corrente.
        /// </para>
        ///
        /// <para><b>Ottimizzazione v0.18</b></para>
        /// <para>
        /// Il percorso precedente controllava ogni NPC contro tutti gli altri NPC.
        /// Questo metodo controlla invece solo le celle nel raggio visivo
        /// dell'osservatore e poi gli eventuali NPC presenti in quelle celle. Il
        /// risultato visivo resta lo stesso, ma il numero di coppie controllate non
        /// cresce piu' come prodotto globale <c>N x (N-1)</c>.
        /// </para>
        /// </summary>
        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world == null || world.NpcDna.Count == 0)
                return;

            var costObserver = world.RuntimeCostObserver;
            bool costSample = costObserver != null && costObserver.ShouldSample(tick.Index);
            bool costPerNpc = costSample && costObserver.TrackPerNpc;
            long costStart = costSample ? costObserver.BeginSample() : 0L;
            int costPairChecks = 0;
            int costCandidateCells = 0;

            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0)
                visionRange = 6;

            bool useCone = world.Global.NpcVisionUseCone;
            float coneSlope = world.Global.NpcVisionConeSlope;

            _npcIds.Clear();
            foreach (var kv in world.NpcDna)
                _npcIds.Add(kv.Key);

            RebuildNpcCellIndex(world);
            world.PerceptionWatchMap?.ClearNpcObservers();

            int spotted = 0;

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int observerId = _npcIds[i];
                int costNpcPairChecks = 0;
                int costNpcSpotted = 0;

                if (!world.GridPos.TryGetValue(observerId, out var observerPos))
                    continue;

                if (!world.NpcFacing.TryGetValue(observerId, out var facing))
                    facing = CardinalDirection.North;

                int ox = observerPos.X;
                int oy = observerPos.Y;

                for (int c = 0; c < _occupiedCellKeys.Count; c++)
                {
                    long cellKey = _occupiedCellKeys[c];
                    if (!_npcIdsByCell.TryGetValue(cellKey, out var targetIds) || targetIds.Count == 0)
                        continue;

                    int firstTargetId = targetIds[0];
                    if (!world.GridPos.TryGetValue(firstTargetId, out var firstTargetPos))
                        continue;

                    if (costSample)
                        costCandidateCells++;

                    int cellDistance = FovUtils.Manhattan(ox, oy, firstTargetPos.X, firstTargetPos.Y);
                    if (cellDistance <= 0 || cellDistance > visionRange)
                        continue;

                    for (int j = 0; j < targetIds.Count; j++)
                    {
                        int targetId = targetIds[j];
                        if (targetId == observerId)
                            continue;

                        if (costSample)
                            costPairChecks++;

                        if (costPerNpc)
                            costNpcPairChecks++;

                        if (!world.GridPos.TryGetValue(targetId, out var targetPos))
                            continue;

                        int tx = targetPos.X;
                        int ty = targetPos.Y;
                        int dist = FovUtils.Manhattan(ox, oy, tx, ty);

                        if (dist <= 0 || dist > visionRange)
                            continue;

                        if (useCone && !FovUtils.IsInCone(ox, oy, facing, tx, ty, coneSlope))
                            continue;

                        if (!world.HasLineOfSight(ox, oy, tx, ty))
                            continue;

                        float quality01 = FovUtils.ObservationQuality(dist, visionRange);

                        bus.Publish(new NpcSpottedEvent(
                            observerNpcId: observerId,
                            observedNpcId: targetId,
                            cellX: tx,
                            cellY: ty,
                            distanceCells: dist,
                            witnessQuality01: quality01));
                        world.PerceptionWatchMap?.RecordNpcObserved(targetId, observerId);

                        spotted++;
                        if (costPerNpc)
                            costNpcSpotted++;
                    }
                }

                if (costPerNpc)
                    costObserver.AddNpcWork(observerId, costNpcPairChecks + costNpcSpotted);
            }

            telemetry.Counter("NpcPerceptionSystem.NpcSpottedEvents", spotted);

            if (costSample)
            {
                costObserver.AddCounter(RuntimeCostCounter.NpcPerceptionPairChecks, costPairChecks);
                costObserver.AddCounter(RuntimeCostCounter.NpcPerceptionCandidateCells, costCandidateCells);
                costObserver.AddCounter(RuntimeCostCounter.NpcPerceptionSpottedEvents, spotted);
                costObserver.EndSample(RuntimeCostChannel.NpcPerception, costStart);
            }
        }

        // =============================================================================
        // RebuildNpcCellIndex
        // =============================================================================
        /// <summary>
        /// <para>
        /// Ricostruisce l'indice temporaneo cella -> NPC presenti usando le posizioni
        /// autorevoli del <c>World</c>.
        /// </para>
        ///
        /// <para><b>Indice runtime non persistente</b></para>
        /// <para>
        /// L'indice viene svuotato e ricostruito a ogni tick, quindi non diventa una
        /// cache persistente da sincronizzare. Il dato vero resta sempre
        /// <c>world.GridPos</c>.
        /// </para>
        /// </summary>
        private void RebuildNpcCellIndex(World world)
        {
            _occupiedCellKeys.Clear();

            foreach (var bucket in _npcIdsByCell.Values)
                bucket.Clear();

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int npcId = _npcIds[i];
                if (!world.GridPos.TryGetValue(npcId, out var position))
                    continue;

                long key = MakeCellKey(position.X, position.Y);
                if (!_npcIdsByCell.TryGetValue(key, out var bucket))
                {
                    bucket = new List<int>(1);
                    _npcIdsByCell[key] = bucket;
                    _occupiedCellKeys.Add(key);
                }

                bucket.Add(npcId);
            }
        }

        private static long MakeCellKey(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }
    }
}
