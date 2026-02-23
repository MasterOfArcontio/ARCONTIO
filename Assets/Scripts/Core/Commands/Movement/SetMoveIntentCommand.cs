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

            // Azione osservabile: se l'intento è attivo, l'NPC sta "andando verso" un target.
            // Nota: reason è opzionale ma utile per debug.
            if (_intent.Active)
                world.SetNpcAction(_npcId, NpcActionState.MoveTo(_intent.TargetX, _intent.TargetY, _intent.Reason.ToString()));
            else
                world.SetNpcIdle(_npcId);
}
    }
}
