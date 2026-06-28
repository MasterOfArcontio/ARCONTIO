// Assets/Scripts/Core/Commands/DevTools/DevEditObjectFoodStockCommand.cs
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    // =============================================================================
    // DevEditObjectFoodStockCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando runtime autorizzato per modificare lo stock di cibo collegato a un
    /// oggetto fisico del <c>World</c>. Gestisce due famiglie di modifica: variazione
    /// della quantita' e cambio proprietario fra Community e un NPC esistente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI -> Bridge -> Command -> World</b></para>
    /// <para>
    /// Il RightInspector non scrive direttamente <c>World.FoodStocks</c>. La view
    /// produce solo un'intenzione locale, il bridge la accoda sul
    /// <c>SimulationHost</c>, e questo comando valida l'oggetto, il componente stock
    /// e l'eventuale NPC proprietario usando lo stato corrente del <c>World</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_objectId</b>: oggetto runtime che possiede il FoodStockComponent.</item>
    ///   <item><b>_hasUnitsSet/_hasUnitsDelta</b>: modifica assoluta o incrementale delle unita'.</item>
    ///   <item><b>_hasOwnerChange</b>: cambio owner verso Community o NPC valido.</item>
    ///   <item><b>Execute</b>: valida e applica tramite <c>World.SetFoodStock</c>.</item>
    /// </list>
    /// </summary>
    public sealed class DevEditObjectFoodStockCommand : ICommand
    {
        private readonly int _objectId;
        private readonly bool _hasUnitsSet;
        private readonly int _unitsSet;
        private readonly bool _hasUnitsDelta;
        private readonly int _unitsDelta;
        private readonly bool _hasOwnerChange;
        private readonly OwnerKind _ownerKind;
        private readonly int _ownerId;

        // =============================================================================
        // DevEditObjectFoodStockCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una richiesta grezza di modifica stock. Il costruttore non
        /// consulta il <c>World</c>: la validazione avviene in <see cref="Execute"/>
        /// quando il comando vede lo stato aggiornato della simulazione.
        /// </para>
        ///
        /// <para><b>Command come intenzione differita</b></para>
        /// <para>
        /// Fra click UI ed esecuzione possono cambiare oggetto, NPC o stock. Per
        /// questo il comando conserva solo parametri e ricontrolla tutto nel pump
        /// runtime, evitando mutazioni cieche partite dalla UI.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>objectId</b>: id oggetto target.</item>
        ///   <item><b>unitsSet/unitsDelta</b>: canali separati per set assoluto e stepper.</item>
        ///   <item><b>ownerKind/ownerId</b>: nuovo owner richiesto se presente.</item>
        /// </list>
        /// </summary>
        private DevEditObjectFoodStockCommand(
            int objectId,
            bool hasUnitsSet,
            int unitsSet,
            bool hasUnitsDelta,
            int unitsDelta,
            bool hasOwnerChange,
            OwnerKind ownerKind,
            int ownerId)
        {
            _objectId = objectId;
            _hasUnitsSet = hasUnitsSet;
            _unitsSet = unitsSet;
            _hasUnitsDelta = hasUnitsDelta;
            _unitsDelta = unitsDelta;
            _hasOwnerChange = hasOwnerChange;
            _ownerKind = ownerKind;
            _ownerId = ownerId;
        }

        // =============================================================================
        // AdjustUnits
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un comando incrementale per lo stepper UI dello stock.
        /// </para>
        /// </summary>
        public static DevEditObjectFoodStockCommand AdjustUnits(int objectId, int deltaUnits)
        {
            return new DevEditObjectFoodStockCommand(
                objectId,
                hasUnitsSet: false,
                unitsSet: 0,
                hasUnitsDelta: true,
                unitsDelta: deltaUnits,
                hasOwnerChange: false,
                ownerKind: OwnerKind.None,
                ownerId: 0);
        }

        // =============================================================================
        // SetUnits
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un comando assoluto per impostare direttamente la quantita' stock.
        /// </para>
        /// </summary>
        public static DevEditObjectFoodStockCommand SetUnits(int objectId, int units)
        {
            return new DevEditObjectFoodStockCommand(
                objectId,
                hasUnitsSet: true,
                unitsSet: units,
                hasUnitsDelta: false,
                unitsDelta: 0,
                hasOwnerChange: false,
                ownerKind: OwnerKind.None,
                ownerId: 0);
        }

        // =============================================================================
        // SetOwnerCommunity
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un comando per rendere comunitario lo stock dell'oggetto.
        /// </para>
        /// </summary>
        public static DevEditObjectFoodStockCommand SetOwnerCommunity(int objectId)
        {
            return new DevEditObjectFoodStockCommand(
                objectId,
                hasUnitsSet: false,
                unitsSet: 0,
                hasUnitsDelta: false,
                unitsDelta: 0,
                hasOwnerChange: true,
                ownerKind: OwnerKind.Community,
                ownerId: 0);
        }

        // =============================================================================
        // SetOwnerNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un comando per assegnare lo stock a un NPC esistente.
        /// </para>
        /// </summary>
        public static DevEditObjectFoodStockCommand SetOwnerNpc(int objectId, int npcId)
        {
            return new DevEditObjectFoodStockCommand(
                objectId,
                hasUnitsSet: false,
                unitsSet: 0,
                hasUnitsDelta: false,
                unitsDelta: 0,
                hasOwnerChange: true,
                ownerKind: OwnerKind.Npc,
                ownerId: npcId);
        }

        // =============================================================================
        // Execute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la modifica allo stock dell'oggetto se target, componente e owner
        /// richiesto sono ancora validi nel <c>World</c> corrente.
        /// </para>
        ///
        /// <para><b>Validazione sul boundary simulativo</b></para>
        /// <para>
        /// La UI puo' mostrare dati leggermente vecchi. Il comando quindi scarta
        /// oggetti cancellati, stock non piu' presenti e NPC owner inesistenti. La
        /// scrittura finale passa da <c>World.SetFoodStock</c> per mantenere coerenti
        /// gli indici/belief collegati gia' gestiti dal World.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione target</b>: oggetto e stock devono esistere.</item>
        ///   <item><b>Quantita'</b>: clamp a zero per evitare stock negativi.</item>
        ///   <item><b>Owner</b>: solo Community oppure NPC esistente.</item>
        /// </list>
        /// </summary>
        public void Execute(World world, MessageBus bus)
        {
            if (world == null || _objectId <= 0)
                return;

            // L'oggetto puo' essere stato eliminato fra click UI ed esecuzione.
            // In quel caso la richiesta non ha piu' un target fisico valido.
            if (!world.Objects.TryGetValue(_objectId, out WorldObjectInstance instance) || instance == null)
            {
                Debug.LogWarning($"[DevTools] EditObjectFoodStock blocked: object={_objectId} does not exist.");
                return;
            }

            // Modifichiamo solo oggetti che hanno gia' un FoodStockComponent. Questo
            // micro-step non crea nuovi stock e non trasforma oggetti generici in cibo.
            if (!world.FoodStocks.TryGetValue(_objectId, out FoodStockComponent stock))
            {
                Debug.LogWarning($"[DevTools] EditObjectFoodStock blocked: object={_objectId} has no food stock.");
                return;
            }

            if (_hasUnitsSet)
                stock.Units = Mathf.Max(0, _unitsSet);

            if (_hasUnitsDelta)
                stock.Units = Mathf.Max(0, stock.Units + _unitsDelta);

            if (_hasOwnerChange && !TryApplyOwner(world, ref stock))
                return;

            world.SetFoodStock(_objectId, stock);
        }

        private bool TryApplyOwner(World world, ref FoodStockComponent stock)
        {
            if (_ownerKind == OwnerKind.Community)
            {
                stock.OwnerKind = OwnerKind.Community;
                stock.OwnerId = 0;
                return true;
            }

            if (_ownerKind == OwnerKind.Npc)
            {
                if (_ownerId <= 0 || !world.ExistsNpc(_ownerId))
                {
                    Debug.LogWarning($"[DevTools] EditObjectFoodStock blocked: owner NPC={_ownerId} does not exist.");
                    return false;
                }

                stock.OwnerKind = OwnerKind.Npc;
                stock.OwnerId = _ownerId;
                return true;
            }

            Debug.LogWarning($"[DevTools] EditObjectFoodStock blocked: owner kind={_ownerKind} is not supported.");
            return false;
        }
    }
}
