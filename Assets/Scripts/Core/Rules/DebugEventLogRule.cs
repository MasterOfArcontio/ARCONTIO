using Arcontio.Core.Diagnostics;
using System.Collections.Generic;

namespace Arcontio.Core
{
    public sealed class DebugEventLogRule : IRule
    {
        public void Handle(World world, ISimEvent e, List<ICommand> outCommands, Telemetry telemetry)
        {
            // Regola mantenuta come ponte transitorio per compatibilita' con la
            // registrazione nel runtime. La diagnostica eventi non passa piu' dal
            // logger legacy: verra' assorbita dai moduli EL quando servira' davvero.
        }
    }
}
