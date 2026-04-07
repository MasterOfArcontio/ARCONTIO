using Arcontio.Core.Diagnostics;
using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // NpcPerceptionSystem — Patch 0.02.5A
    // Geometria FOV centralizzata in FovUtils.cs
    // =============================================================================
    /// <summary>
    /// <b>NpcPerceptionSystem</b> — percezione visiva degli altri NPC.
    ///
    /// <para>
    /// Per ogni NPC osservatore, valuta quali altri NPC sono visibili e produce
    /// un <c>NpcSpottedEvent</c> per ciascuno. Questo alimenta il sistema di
    /// memoria "Observed entities", eliminando qualsiasi scansione globale
    /// nel planning (no telepatia: un NPC sa solo chi ha visto).
    /// </para>
    ///
    /// <para><b>Pipeline di visione (Arcontio Core Standard v1.0):</b></para>
    /// <list type="number">
    ///   <item><b>Range gate</b> — Manhattan &lt;= visionRange</item>
    ///   <item><b>Cone gate</b> — <see cref="FovUtils.IsInCone"/></item>
    ///   <item><b>LOS gate</b>  — <c>world.HasLineOfSight</c> (Bresenham)</item>
    /// </list>
    ///
    /// <para>
    /// La struttura è volutamente speculare a <c>ObjectPerceptionSystem</c>
    /// per mantenere la coerenza della pipeline. In futuro potrà essere
    /// ottimizzata con spatial hashing o region query.
    /// </para>
    ///
    /// <para>
    /// <b>Patch 0.02.5A:</b> il metodo privato <c>IsInCone</c> è stato rimosso.
    /// Tutta la geometria FOV delega a <see cref="FovUtils"/>.
    /// </para>
    /// </summary>
    public sealed class NpcPerceptionSystem : ISystem
    {
        public int Period => 1;

        private readonly List<int> _npcIds = new(2048);

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world.NpcDna.Count == 0)
                return;

            int visionRange = world.Global.NpcVisionRangeCells;
            if (visionRange <= 0) visionRange = 6;

            bool useCone = world.Global.NpcVisionUseCone;
            float coneSlope = world.Global.NpcVisionConeSlope;

            // Snapshot NPC ids (evita iterazioni su Dictionary mentre qualcuno muta lo state)
            _npcIds.Clear();
            foreach (var kv in world.NpcDna)
                _npcIds.Add(kv.Key);

            int spotted = 0;

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int observerId = _npcIds[i];

                if (!world.GridPos.TryGetValue(observerId, out var op))
                    continue;

                if (!world.NpcFacing.TryGetValue(observerId, out var facing))
                    facing = CardinalDirection.North;

                // Cono/LOS dal punto di vista dell'osservatore
                int ox = op.X;
                int oy = op.Y;

                for (int j = 0; j < _npcIds.Count; j++)
                {
                    int targetId = _npcIds[j];
                    if (targetId == observerId)
                        continue;

                    if (!world.GridPos.TryGetValue(targetId, out var tp))
                        continue;

                    int tx = tp.X;
                    int ty = tp.Y;

                    // Range gate: il più economico — elimina subito i target lontani
                    // senza dover calcolare cono o LOS.
                    int dist = FovUtils.Manhattan(ox, oy, tx, ty);

                    // dist == 0: stessa cella (non si può "vedere" se stessi).
                    if (dist <= 0)
                        continue;

                    // dist > visionRange: fuori range massimo di visione.
                    if (dist > visionRange)
                        continue;

                    // Patch 0.02.5A: delega a FovUtils (fonte canonica del cono)
                    if (useCone && !FovUtils.IsInCone(ox, oy, facing, tx, ty, coneSlope))
                        continue;

                    // LOS gate: Bresenham sull'OcclusionMap — applicato per ultimo perché più costoso.
                    // Se la LOS è bloccata da un muro/porta, l'NPC non vede il target.
                    if (!world.HasLineOfSight(ox, oy, tx, ty))
                        continue;

                    // Patch 0.02.5A: qualità centralizzata in FovUtils.ObservationQuality.
                    // In futuro si potrebbe pesare anche l'orientamento relativo (frontal bonus).
                    float q = FovUtils.ObservationQuality(dist, visionRange);

                    bus.Publish(new NpcSpottedEvent(
                        observerNpcId: observerId,
                        observedNpcId: targetId,
                        cellX: tx,
                        cellY: ty,
                        distanceCells: dist,
                        witnessQuality01: q));

                    spotted++;
                }
            }

            telemetry.Counter("NpcPerceptionSystem.NpcSpottedEvents", spotted);
        }

        // Patch 0.02.5A: IsInCone rimosso — usa FovUtils.IsInCone

    }
}
