// Assets/Scripts/Core/Commands/DevTools/DevLoadMapCommand.cs
using Arcontio.Core.DevTools;
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    /// <summary>
    /// DevLoadMapCommand (DevMode v0 - MVP):
    /// carica un JSON DevMap e applica gli oggetti al World.
    ///
    /// Policy v0:
    /// - ClearObjects=true: l'import sostituisce completamente il layout oggetti corrente.
    ///   (È il comportamento più utile in debug: "carico lo scenario e lo ottengo identico".)
    /// </summary>
    public sealed class DevLoadMapCommand : ICommand
    {
        private readonly string _pathOrName;
        private readonly bool _clearObjects;

        public DevLoadMapCommand(string pathOrName, bool clearObjects = true)
        {
            _pathOrName = pathOrName;
            _clearObjects = clearObjects;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;

            if (!DevMapIO.TryLoad(_pathOrName, out var data) || data == null)
            {
                Debug.LogWarning($"[DevTools] LoadDevMap failed: '{_pathOrName}'.");
                return;
            }

            world.ImportDevMapData(data, clearObjects: _clearObjects);

            // Rebuild globale delle cache derivate (MVP).
            world.RebuildDerivedCachesGlobal();
        }
    }
}
