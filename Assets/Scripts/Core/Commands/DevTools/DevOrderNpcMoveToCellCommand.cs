using Arcontio.Core.Logging;

namespace Arcontio.Core.Commands.DevTools
{
    // =============================================================================
    // DevOrderNpcMoveToCellCommand — Patch 0.02.05.B
    // =============================================================================
    /// <summary>
    /// <b>DevOrderNpcMoveToCellCommand</b> — comando dev che ordina a un NPC
    /// di muoversi verso una cella cliccata nella MapGrid.
    ///
    /// <para>
    /// Esiste per permettere ai dev tool di testare i percorsi senza aspettare
    /// che una Rule produca l'intent corretto. È un canale view→core esplicito:
    /// la view non scrive mai direttamente nel World.
    /// </para>
    ///
    /// <para><b>Patch 0.02.05.B — allineamento con SetMoveIntentCommand:</b></para>
    /// <para>
    /// Prima di questa patch questo command conteneva logica di planning identica
    /// a quella che era in <c>SetMoveIntentCommand</c> (anche quello rimosso in 0.02.05.B).
    /// Ora entrambi i command sono minimali: impostano <c>IsNew = true</c> e lasciano
    /// che <c>MovementSystem.InitializeNavigation</c> decida la strategia di navigazione
    /// al primo tick.
    /// </para>
    /// </summary>
    public sealed class DevOrderNpcMoveToCellCommand : ICommand
    {
        private readonly int _npcId;
        private readonly int _targetX;
        private readonly int _targetY;

        public DevOrderNpcMoveToCellCommand(int npcId, int targetX, int targetY)
        {
            _npcId   = npcId;
            _targetX = targetX;
            _targetY = targetY;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;
            if (!world.ExistsNpc(_npcId)) return;

            // Pulisce gli stati di navigazione precedenti.
            world.ClearDebugNavigationPathsForNpc(_npcId);
            world.ClearDebugMacroRouteForNpc(_npcId);
            world.ClearNpcLocalSearchState(_npcId, string.Empty);
            world.ClearNpcDirectCommitState(_npcId, string.Empty);

            // Imposta l'intent con IsNew = true.
            // MovementSystem.InitializeNavigation deciderà al primo tick
            // se usare direct path o macro-route, esattamente come fa
            // SetMoveIntentCommand per il gameplay normale.
            world.SetMoveIntent(_npcId, new MoveIntent
            {
                Active        = true,
                TargetX       = _targetX,
                TargetY       = _targetY,
                Reason        = MoveIntentReason.DebugClick,
                TargetObjectId = 0,
                BlockedTicks  = 0,
                IsNew         = true,   // segnala al MovementSystem di inizializzare
            });

            world.SetNpcAction(_npcId, NpcActionState.MoveTo(_targetX, _targetY, "DebugClick"));

            ArcontioLogger.Trace(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "DevMove",
                               npcId: _npcId, cell: (_targetX, _targetY)),
                new LogBlock(LogLevel.Trace, "log.dev.click_move_order")
                    .AddField("npcId",   _npcId)
                    .AddField("targetX", _targetX)
                    .AddField("targetY", _targetY));
        }
    }
}
