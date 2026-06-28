// Assets/Scripts/Core/Commands/DevTools/DevEditObjectCommand.cs
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    // =============================================================================
    // DevEditObjectDoorState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato porta esplicito richiesto dai DevTools runtime quando l'operatore
    /// modifica un oggetto porta dal RightInspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Contratto esplicito invece di flag sparsi</b></para>
    /// <para>
    /// La UI non deve inviare combinazioni arbitrarie di <c>IsOpen</c> e
    /// <c>IsLocked</c>. Questo enum limita la modifica ai tre stati realmente
    /// supportati dalla simulazione corrente: chiusa, aperta e chiusa a chiave.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Closed</b>: porta chiusa e non locked.</item>
    ///   <item><b>Open</b>: porta aperta e non locked.</item>
    ///   <item><b>Locked</b>: porta chiusa e locked, solo se il def e' lockable.</item>
    /// </list>
    /// </summary>
    public enum DevEditObjectDoorState
    {
        Closed = 0,
        Open = 1,
        Locked = 2
    }

    // =============================================================================
    // DevEditObjectCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando runtime autorizzato che modifica campi generici di un oggetto
    /// esistente senza passare dalla UI direttamente al <c>World</c>.
    /// </para>
    ///
    /// <para><b>UI -> Bridge -> Command Gateway -> World</b></para>
    /// <para>
    /// Il RightInspector produce solo intenzioni utente. Il bridge ArcGraph traduce
    /// quelle intenzioni in questo comando e il comando rivalida oggetto, NPC owner
    /// e definizione porta al momento dell'esecuzione. In questo modo l'editor
    /// runtime resta utile, ma non diventa un accesso globale libero allo stato
    /// simulativo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Owner edit</b>: cambia <c>WorldObjectInstance.OwnerKind/OwnerId</c> per oggetti non food-stock.</item>
    ///   <item><b>Door edit</b>: applica chiusa/aperta/locked usando <c>World.SetDoorOpen</c> per le cache fisiche.</item>
    ///   <item><b>Factory methods</b>: espongono richieste piccole e leggibili al bridge UI.</item>
    /// </list>
    /// </summary>
    public sealed class DevEditObjectCommand : ICommand
    {
        private readonly int _objectId;
        private readonly bool _hasOwnerEdit;
        private readonly OwnerKind _ownerKind;
        private readonly int _ownerId;
        private readonly bool _hasDoorEdit;
        private readonly DevEditObjectDoorState _doorState;

        // =============================================================================
        // DevEditObjectCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Conserva una modifica intenzionale singola o composta. La validazione
        /// resta in <see cref="Execute"/>, perche' il mondo puo' cambiare tra click
        /// UI e tick simulativo.
        /// </para>
        /// </summary>
        private DevEditObjectCommand(
            int objectId,
            bool hasOwnerEdit,
            OwnerKind ownerKind,
            int ownerId,
            bool hasDoorEdit,
            DevEditObjectDoorState doorState)
        {
            _objectId = objectId;
            _hasOwnerEdit = hasOwnerEdit;
            _ownerKind = ownerKind;
            _ownerId = ownerId;
            _hasDoorEdit = hasDoorEdit;
            _doorState = doorState;
        }

        // =============================================================================
        // SetOwnerCommunity
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta per rendere comunitaria la proprieta' dell'oggetto.
        /// </para>
        /// </summary>
        public static DevEditObjectCommand SetOwnerCommunity(int objectId)
        {
            return new DevEditObjectCommand(
                objectId,
                hasOwnerEdit: true,
                ownerKind: OwnerKind.Community,
                ownerId: 0,
                hasDoorEdit: false,
                doorState: DevEditObjectDoorState.Closed);
        }

        // =============================================================================
        // SetOwnerNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta per assegnare la proprieta' dell'oggetto a un NPC.
        /// </para>
        /// </summary>
        public static DevEditObjectCommand SetOwnerNpc(int objectId, int npcId)
        {
            return new DevEditObjectCommand(
                objectId,
                hasOwnerEdit: true,
                ownerKind: OwnerKind.Npc,
                ownerId: npcId,
                hasDoorEdit: false,
                doorState: DevEditObjectDoorState.Closed);
        }

        // =============================================================================
        // SetDoorClosed
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta per portare una porta allo stato chiuso non locked.
        /// </para>
        /// </summary>
        public static DevEditObjectCommand SetDoorClosed(int objectId)
        {
            return DoorState(objectId, DevEditObjectDoorState.Closed);
        }

        // =============================================================================
        // SetDoorOpen
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta per aprire una porta.
        /// </para>
        /// </summary>
        public static DevEditObjectCommand SetDoorOpen(int objectId)
        {
            return DoorState(objectId, DevEditObjectDoorState.Open);
        }

        // =============================================================================
        // SetDoorLocked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una richiesta per chiudere a chiave una porta lockable.
        /// </para>
        /// </summary>
        public static DevEditObjectCommand SetDoorLocked(int objectId)
        {
            return DoorState(objectId, DevEditObjectDoorState.Locked);
        }

        // =============================================================================
        // Execute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la modifica richiesta al World dopo aver rivalidato target,
        /// componenti e definizione oggetto.
        /// </para>
        /// </summary>
        public void Execute(World world, MessageBus bus)
        {
            if (world == null || _objectId <= 0)
                return;

            if (!world.Objects.TryGetValue(_objectId, out WorldObjectInstance instance) || instance == null)
            {
                Debug.LogWarning($"[DevTools] EditObject blocked: object={_objectId} does not exist.");
                return;
            }

            if (_hasOwnerEdit && !TryApplyOwner(world, instance))
                return;

            if (_hasDoorEdit)
                TryApplyDoorState(world, instance);
        }

        private static DevEditObjectCommand DoorState(int objectId, DevEditObjectDoorState state)
        {
            return new DevEditObjectCommand(
                objectId,
                hasOwnerEdit: false,
                ownerKind: OwnerKind.None,
                ownerId: 0,
                hasDoorEdit: true,
                doorState: state);
        }

        private bool TryApplyOwner(World world, WorldObjectInstance instance)
        {
            if (world.FoodStocks.ContainsKey(_objectId))
            {
                Debug.LogWarning($"[DevTools] EditObject owner blocked: object={_objectId} is a food stock. Use DevEditObjectFoodStockCommand.");
                return false;
            }

            if (_ownerKind == OwnerKind.Community)
            {
                instance.OwnerKind = OwnerKind.Community;
                instance.OwnerId = 0;
                return true;
            }

            if (_ownerKind == OwnerKind.Npc)
            {
                if (_ownerId <= 0 || !world.ExistsNpc(_ownerId))
                {
                    Debug.LogWarning($"[DevTools] EditObject owner blocked: owner NPC={_ownerId} does not exist.");
                    return false;
                }

                instance.OwnerKind = OwnerKind.Npc;
                instance.OwnerId = _ownerId;
                return true;
            }

            Debug.LogWarning($"[DevTools] EditObject owner blocked: owner kind={_ownerKind} is not supported.");
            return false;
        }

        private bool TryApplyDoorState(World world, WorldObjectInstance instance)
        {
            if (!world.TryGetObjectDef(instance.DefId, out ObjectDef def) || def == null || !def.IsDoor)
            {
                Debug.LogWarning($"[DevTools] EditObject door blocked: object={_objectId} is not a door.");
                return false;
            }

            if (_doorState == DevEditObjectDoorState.Locked && !def.IsLockable)
            {
                Debug.LogWarning($"[DevTools] EditObject door locked blocked: object={_objectId} def='{instance.DefId}' is not lockable.");
                return false;
            }

            if (_doorState == DevEditObjectDoorState.Open)
            {
                instance.IsLocked = false;
                world.SetDoorOpen(_objectId, true);
                return true;
            }

            if (_doorState == DevEditObjectDoorState.Locked)
            {
                instance.IsLocked = true;
                world.SetDoorOpen(_objectId, false);
                return true;
            }

            instance.IsLocked = false;
            world.SetDoorOpen(_objectId, false);
            return true;
        }
    }
}
