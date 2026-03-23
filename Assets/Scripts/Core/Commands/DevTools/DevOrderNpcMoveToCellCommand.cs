using Arcontio.Core.Logging;

namespace Arcontio.Core.Commands.DevTools
{
    /// <summary>
    /// DevOrderNpcMoveToCellCommand:
    /// comando di debug che forza un MoveIntent verso una cella cliccata per un NPC specifico.
    ///
    /// Perche' esiste:
    /// - ci serve un modo artificiale e immediato per testare i percorsi senza aspettare
    ///   che una Rule/Decision produca da sola l'intento corretto;
    /// - ci serve anche un punto unico in cui aggiornare la route macro di debug
    ///   (landmark/edge che l'overlay mostrera' sopra la mappa).
    ///
    /// Nota architetturale:
    /// - il comando e' view->core, quindi la view non scrive direttamente nel World;
    /// - l'esecuzione avviene dentro il pump di SimulationHost come gli altri comandi DevTools.
    /// </summary>
    public sealed class DevOrderNpcMoveToCellCommand : ICommand
    {
        private readonly int _npcId;
        private readonly int _targetX;
        private readonly int _targetY;

        public DevOrderNpcMoveToCellCommand(int npcId, int targetX, int targetY)
        {
            _npcId = npcId;
            _targetX = targetX;
            _targetY = targetY;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;
            if (!world.ExistsNpc(_npcId)) return;

            world.ClearDebugNavigationPathsForNpc(_npcId);

            world.SetMoveIntent(_npcId, new MoveIntent
            {
                Active = true,
                TargetX = _targetX,
                TargetY = _targetY,
                Reason = MoveIntentReason.DebugClick,
                TargetObjectId = 0,
                BlockedTicks = 0,
            });

            world.SetNpcAction(_npcId, NpcActionState.MoveTo(_targetX, _targetY, "DebugClick"));

            // PATCH 0.02.05.2f:
            // anche per il click manuale vogliamo rispettare la stessa regola del gameplay normale:
            // se il target è già raggiungibile con un piano diretto reale, NON dobbiamo sporcare
            // il runtime con una macro-route landmark inutile.
            if (world.CanNpcUseDirectPath(_npcId, _targetX, _targetY))
            {
                world.ClearDebugMacroRouteForNpc(_npcId);
                var directPath = new System.Collections.Generic.List<UnityEngine.Vector2Int>(32);
                if (world.GridPos.TryGetValue(_npcId, out var pos) && world.TryBuildGreedyDirectPath(_npcId, pos.X, pos.Y, _targetX, _targetY, directPath))
                    world.SetDebugDirectPathForNpc(_npcId, directPath);
            }
            else
            {
                world.BeginMacroRouteExecutionForNpc(_npcId, _targetX, _targetY);

                if (!world.NpcMacroRouteExecution.TryGetValue(_npcId, out var macroState) || macroState == null || !macroState.Active)
                {
                    var directPrefix = new System.Collections.Generic.List<UnityEngine.Vector2Int>(32);
                    if (world.GridPos.TryGetValue(_npcId, out var pos) && world.TryBuildGreedyDirectPrefixPath(_npcId, pos.X, pos.Y, _targetX, _targetY, directPrefix))
                        world.SetDebugDirectPathForNpc(_npcId, directPrefix);
                }
            }

            ArcontioLogger.Trace(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "DevMove", npcId: _npcId, cell: (_targetX, _targetY)),
                new LogBlock(LogLevel.Trace, "log.dev.click_move_order")
                    .AddField("npcId", _npcId)
                    .AddField("targetX", _targetX)
                    .AddField("targetY", _targetY));
        }
    }
}
