// Assets/Scripts/Core/Commands/DevTools/DevSaveMapCommand.cs
using Arcontio.Core.DevTools;
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    /// <summary>
    /// DevSaveMapCommand (DevMode v0 - MVP):
    /// serializza lo stato attuale della mappa (oggetti) in JSON.
    ///
    /// Nota:
    /// - In v0 salviamo SOLO oggetti (non NPC).
    /// - Il path viene risolto su persistentDataPath/DevMaps se si passa solo un nome.
    /// </summary>
    public sealed class DevSaveMapCommand : ICommand
    {
        private readonly string _pathOrName;

        public DevSaveMapCommand(string pathOrName)
        {
            _pathOrName = pathOrName;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;

            var data = world.ExportDevMapData();
            bool ok = DevMapIO.Save(_pathOrName, data);

            if (!ok)
                Debug.LogWarning($"[DevTools] SaveDevMap failed: '{_pathOrName}'.");
        }
    }
}
