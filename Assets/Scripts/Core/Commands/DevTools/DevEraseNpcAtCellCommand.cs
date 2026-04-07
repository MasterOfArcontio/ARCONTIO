// Assets/Scripts/Core/Commands/DevTools/DevEraseNpcAtCellCommand.cs
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    /// <summary>
    /// DevEraseNpcAtCellCommand (DevMode v1.1):
    /// rimuove l'NPC presente nella cella target.
    ///
    /// NOTE DI DESIGN (ARCONTIO):
    /// - Questo è un comando DEV/DEBUG: non deve essere gameplay-safe.
    /// - Obiettivo: permettere di ripulire la scena rapidamente durante test e tuning.
    /// - Implementazione: rimuoviamo l'NPC da TUTTI gli store world-side noti.
    ///
    /// Motivazione della pulizia estesa:
    /// - In ARCONTIO gli NPC hanno più store paralleli (Needs, Memory, Intent, ecc.).
    /// - Se eliminiamo solo NpcDna/GridPos, rimangono entry "orfane" che possono:
    ///   - inquinare debug overlay
    ///   - far fallire assert / controlli ExistsNpc
    ///   - generare side-effect nei sistemi (es. movement/scan)
    ///
    /// Nota:
    /// - Se in futuro introdurrai un metodo unico world-side (es. World.DestroyNpc),
    ///   questa logica va centralizzata lì e questo command deve solo chiamarlo.
    /// </summary>
    public sealed class DevEraseNpcAtCellCommand : ICommand
    {
        private readonly int _x;
        private readonly int _y;

        public DevEraseNpcAtCellCommand(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;
            if (!world.InBounds(_x, _y)) return;

            if (!world.TryGetNpcAt(_x, _y, out int npcId) || npcId <= 0)
            {
                // Silenzioso: tool di debug, click a vuoto è normale.
                return;
            }

            if (!world.ExistsNpc(npcId))
                return;

            // ============================================================
            // RIMOZIONE DAGLI STORE "CORE"
            // ============================================================

            world.NpcDna.Remove(npcId);
            world.NpcProfiles.Remove(npcId);
            world.Needs.Remove(npcId);
            world.Social.Remove(npcId);
            world.GridPos.Remove(npcId);
            world.NpcFacing.Remove(npcId);

            // ============================================================
            // ACTION / BALLOON (osservabilità)
            // ============================================================

            world.NpcAction.Remove(npcId);
            world.NpcBalloonSignals.Remove(npcId);

            // ============================================================
            // MEMORY
            // ============================================================

            world.Memory.Remove(npcId);
            world.MemoryParams.Remove(npcId);
            world.NpcObjectMemory.Remove(npcId);

            // Ownership pinned belief
            world.NpcPinnedFoodStockBeliefs.Remove(npcId);

            // ============================================================
            // PRIVATE FOOD / MARKERS
            // ============================================================

            world.NpcPrivateFood.Remove(npcId);
            world.NpcLastPrivateFoodConsumeTick.Remove(npcId);

            // ============================================================
            // MOVEMENT / SCAN
            // ============================================================

            world.NpcMoveIntents.Remove(npcId);
            world.NpcScanStates.Remove(npcId);

            // Nota:
            // - Non chiamiamo rebuild cache globale: NPC non modifica occlusion.
            // - L'occupancy su GridPos è già rimossa, quindi TryGetNpcAt non lo troverà più.

            Debug.Log($"[DevTools] Erased NPC {npcId} at cell ({_x},{_y}).");
        }
    }
}
