namespace Arcontio.Core
{
    /// <summary>
    /// Comando minimale per scrivere un MoveIntent nel World.
    /// Serve per mantenere la regola: le Rules producono comandi, non mutano direttamente il World.
    /// </summary>
    public sealed class SetMoveIntentCommand : ICommand
    {
        private readonly int _npcId;
        private readonly MoveIntent _intent;

        public SetMoveIntentCommand(int npcId, MoveIntent intent)
        {
            _npcId = npcId;
            _intent = intent;
        }

        public void Execute(World world, MessageBus bus)
        {
            world.SetMoveIntent(_npcId, _intent);
            world.ClearDebugNavigationPathsForNpc(_npcId);

            if (_intent.Active)
            {
                world.SetNpcAction(_npcId, NpcActionState.MoveTo(_intent.TargetX, _intent.TargetY, _intent.Reason.ToString()));

                // PATCH 0.02.05.2f:
                // prima di armare la macro-route landmark, verifichiamo se il target è già
                // raggiungibile con un vero percorso diretto coerente con il MovementSystem.
                // In quel caso NON dobbiamo costruire una deviazione verso landmark lontani,
                // perché violeremmo la priorità architetturale "direct first".
                if (world.CanNpcUseDirectPath(_npcId, _intent.TargetX, _intent.TargetY))
                {
                    world.ClearDebugMacroRouteForNpc(_npcId);
                    var directPath = new System.Collections.Generic.List<UnityEngine.Vector2Int>(32);
                    if (world.GridPos.TryGetValue(_npcId, out var pos) && world.TryBuildGreedyDirectPath(_npcId, pos.X, pos.Y, _intent.TargetX, _intent.TargetY, directPath))
                        world.SetDebugDirectPathForNpc(_npcId, directPath);
                }
                else
                {
                    world.BeginMacroRouteExecutionForNpc(_npcId, _intent.TargetX, _intent.TargetY);

                    if (!world.NpcMacroRouteExecution.TryGetValue(_npcId, out var macroState) || macroState == null || !macroState.Active)
                    {
                        var directPrefix = new System.Collections.Generic.List<UnityEngine.Vector2Int>(32);
                        if (world.GridPos.TryGetValue(_npcId, out var pos) && world.TryBuildGreedyDirectPrefixPath(_npcId, pos.X, pos.Y, _intent.TargetX, _intent.TargetY, directPrefix))
                            world.SetDebugDirectPathForNpc(_npcId, directPrefix);
                    }
                }
            }
            else
            {
                world.SetNpcIdle(_npcId);
                world.ClearDebugMacroRouteForNpc(_npcId);
                world.ClearNpcLocalSearchState(_npcId, string.Empty);
                world.ClearNpcDirectCommitState(_npcId, string.Empty);
            }
        }
    }
}
