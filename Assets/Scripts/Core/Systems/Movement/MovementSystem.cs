using Arcontio.Core.Diagnostics;
using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// MovementSystem (Day10):
    /// Consuma MoveIntent e prova ad avanzare di 1 cella per tick.
    ///
    /// Baseline volutamente semplice (v0.01):
    /// - distanza Manhattan
    /// - 1 step per tick
    /// - collisione minima: non entra fuori bounds, non entra in celle bloccate da movimento,
    ///   non entra in cella con NPC (1 NPC per cella, standard).
    ///
    /// Questo è sufficiente per:
    /// - muoversi verso cibo/letto
    /// - testare occlusione/LOS e witness dei furti
    ///
    /// In futuro:
    /// - pathfinding
    /// - gestione porte (auto-open durante MoveTo)
    /// - NpcMovedEvent / PerceptionDirty event-driven
    /// </summary>
    public sealed class MovementSystem : ISystem
    {
        public int Period => 1;

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
             // Nota: iteriamo su NpcCore.Keys (source of truth degli NPC esistenti)
            foreach (var npcId in world.NpcCore.Keys)
            {
                if (!world.NpcMoveIntents.TryGetValue(npcId, out var intent) || !intent.Active)
                    continue;
                if (!world.GridPos.TryGetValue(npcId, out var pos))
                    continue;

                // ACTION TRACE (debug/overlay): durante un MoveIntent attivo, l'NPC è in azione MoveTo.
                // IMPORTANTISSIMO: non vogliamo resettare StartedTick ogni tick; quindi settiamo solo se serve.
                if (world.TryGetNpcAction(npcId, out var act))
                {
                    if (act.Kind != NpcActionKind.MoveTo || !act.HasTargetCell || act.TargetX != intent.TargetX || act.TargetY != intent.TargetY)
                        world.SetNpcAction(npcId, NpcActionState.MoveTo(intent.TargetX, intent.TargetY, intent.Reason.ToString()));
                }
                else
                {
                    world.SetNpcAction(npcId, NpcActionState.MoveTo(intent.TargetX, intent.TargetY, intent.Reason.ToString()));
                }

                int x = pos.X;
                int y = pos.Y;

                // Se siamo arrivati, disattiviamo.
                if (x == intent.TargetX && y == intent.TargetY)
                {
                    intent.Active = false;
                    world.NpcMoveIntents[npcId] = intent;
                    world.SetNpcIdle(npcId);
                    continue;
                }

                // Step greedy: preferisci asse con maggiore distanza.
                int dx = intent.TargetX - x;
                int dy = intent.TargetY - y;

                int stepX = 0;
                int stepY = 0;

                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                    stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
                else
                    stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

                // Se l'asse scelto è 0 (perché dx==0 e Abs(dx)>=Abs(dy)), proviamo l'altro.
                if (stepX == 0 && stepY == 0)
                {
                    // Non dovrebbe succedere perché abbiamo gestito "arrivati" sopra, ma restiamo difensivi.
                    intent.Active = false;
                    world.NpcMoveIntents[npcId] = intent;
                    world.SetNpcIdle(npcId);
                    continue;
                }

                // Tentativo 1: step scelto
                if (!TryMoveTo(world, npcId, x + stepX, y + stepY))
                {
                    // Tentativo 2: prova sull'altro asse (fallback minimo)
                    if (stepX != 0)
                    {
                        stepX = 0;
                        stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);
                    }
                    else
                    {
                        stepY = 0;
                        stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
                    }

                    // Se anche il fallback non è possibile, restiamo fermi.
                    TryMoveTo(world, npcId, x + stepX, y + stepY);
                }

                // Nota importante (molto verbosa ma utile):
                // In futuro, qui è il punto giusto per:
                // - emettere un "NpcMovedEvent"
                // - settare PerceptionDirty (event-driven perception)
                // Attualmente molti sistemi fanno polling e quindi non è strettamente necessario,
                // ma il documento sight spinge verso trigger dopo movimento/rotazione.
            }
        }

        private static bool TryMoveTo(World world, int npcId, int tx, int ty)
        {
            // Bounds
            if (!world.InBounds(tx, ty))
                return false;

            // Blocco movimento
            if (world.IsMovementBlocked(tx, ty))
                return false;

            // 1 NPC per cell: se c'è già qualcuno non entriamo.
            if (world.TryGetNpcAt(tx, ty, out _))
                return false;

            // Muovi
            world.GridPos[npcId] = new GridPosition(tx, ty);

            ArcontioLogger.Trace(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: npcId, cell: (tx, ty)),
                new LogBlock(LogLevel.Trace, "log.move.step")
                    .AddField("x", tx)
                    .AddField("y", ty)
            );

            return true;
        }
    }
}
