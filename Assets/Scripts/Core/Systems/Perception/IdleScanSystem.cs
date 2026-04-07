using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    /// <summary>
    /// Scan direzionale (anti-360 gratuito).
    ///
    /// Regola:
    /// - Nessuna visione a 360° gratuita: per “guardarti attorno” devi ruotare.
    /// - Scan = 4 rotazioni consecutive (una per tick).
    ///
    /// Politica minimale:
    /// - se NPC è idle (nessun MoveIntent e nessuno Scan attivo), avvia scan
    ///   ogni N tick (throttle) per non ruotare in continuazione.
    /// </summary>
    public sealed class IdleScanSystem : ISystem
    {
        // Throttle molto semplice: uno scan ogni X tick quando idle.
        private readonly int _scanPeriodTicks;

        public int Period => 1;

        public IdleScanSystem(int scanPeriodTicks = 12)
        {
            _scanPeriodTicks = scanPeriodTicks <= 0 ? 12 : scanPeriodTicks;
        }

        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world == null) return;

            // Nota: Tick.Index è long. Usiamo long per evitare overflow e cast inutili.
            long nowTick = tick.Index;

            // Nota importante (molto verbosa ma utile):
            // Qui iteriamo su NpcCore perché nel Core standard l’elenco “canonico” degli NPC
            // è quello (non world.Npcs).
            foreach (var kv in world.NpcDna)
            {
                int npcId = kv.Key;

                // 1) Se scan è attivo, esegui una rotazione per tick.
                if (world.NpcScanStates.TryGetValue(npcId, out var scan) && scan.Active)
                {
                    // Evita doppi turn nello stesso tick (difensivo).
                    if (scan.LastTurnTick == nowTick)
                        continue;

                    // Se ho finito: stop.
                    if (scan.RemainingTurns <= 0)
                    {
                        world.StopScan(npcId);
                        continue;
                    }

                    // Turn90 (clockwise per determinismo)
                    // Non usiamo GetFacing() qui per evitare dipendenze: leggiamo direttamente NpcFacing.
                    var dir = world.NpcFacing.TryGetValue(npcId, out var curDir)
                        ? curDir
                        : CardinalDirection.North;

                    dir = NextClockwise(dir);
                    world.SetFacing(npcId, dir);

                    scan.RemainingTurns--;
                    scan.LastTurnTick = (int)nowTick;

                    // Scrivo back lo stato aggiornato.
                    world.NpcScanStates[npcId] = scan;
                    continue;
                }

                // 2) Se non sto scan-nando e sono idle, posso iniziare uno scan ogni N tick.
                if (world.IsNpcIdleForScan(npcId))
                {
                    if (nowTick % _scanPeriodTicks == 0)
                    {
                        world.StartScan(npcId, (int)nowTick, turns: 4);
                    }
                }
            }
        }

        private static CardinalDirection NextClockwise(CardinalDirection d)
        {
            // Ordine: North -> East -> South -> West -> North
            return d switch
            {
                CardinalDirection.North => CardinalDirection.East,
                CardinalDirection.East => CardinalDirection.South,
                CardinalDirection.South => CardinalDirection.West,
                _ => CardinalDirection.North
            };
        }
    }
}
