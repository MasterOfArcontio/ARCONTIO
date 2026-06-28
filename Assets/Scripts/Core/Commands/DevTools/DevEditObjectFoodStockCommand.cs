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
    /// della quantita' e cambio proprietario coerente fra Community e un NPC esistente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: UI -> Bridge -> Command -> World</b></para>
    /// <para>
    /// Il RightInspector non scrive direttamente <c>World.FoodStocks</c>. La view
    /// produce solo un'intenzione locale, il bridge la accoda sul
    /// <c>SimulationHost</c>, e questo comando valida l'oggetto, il componente stock
    /// e l'eventuale NPC proprietario usando lo stato corrente del <c>World</c>. In
    /// caso di cambio owner, allinea anche l'istanza oggetto e le pinned belief del
    /// vecchio proprietario.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_objectId</b>: oggetto runtime che possiede il FoodStockComponent.</item>
    ///   <item><b>_hasUnitsSet/_hasUnitsDelta</b>: modifica assoluta o incrementale delle unita'.</item>
    ///   <item><b>_hasOwnerChange</b>: cambio owner verso Community o NPC valido.</item>
    ///   <item><b>Owner side effects</b>: sincronizza oggetto e ripulisce vecchie pinned belief.</item>
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
        /// oggetti cancellati, stock non piu' presenti e NPC owner inesistenti. Se
        /// l'owner cambia, il comando sincronizza anche l'owner dell'istanza oggetto
        /// e rimuove la pinned belief del vecchio NPC proprietario. La scrittura
        /// finale passa da <c>World.SetFoodStock</c> per mantenere coerenti gli
        /// indici/belief collegati gia' gestiti dal World.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione target</b>: oggetto e stock devono esistere.</item>
        ///   <item><b>Quantita'</b>: clamp a zero per evitare stock negativi.</item>
        ///   <item><b>Owner</b>: solo Community oppure NPC esistente.</item>
        ///   <item><b>Belief cleanup</b>: rimuove il pin privato del vecchio NPC quando cambia owner.</item>
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

            OwnerKind previousOwnerKind = stock.OwnerKind;
            int previousOwnerId = stock.OwnerId;

            if (_hasUnitsSet)
                stock.Units = Mathf.Max(0, _unitsSet);

            if (_hasUnitsDelta)
                stock.Units = Mathf.Max(0, stock.Units + _unitsDelta);

            if (_hasOwnerChange && !TryApplyOwner(world, ref stock))
                return;

            if (_hasOwnerChange)
                ApplyOwnerSideEffects(world, instance, previousOwnerKind, previousOwnerId, stock);

            world.SetFoodStock(_objectId, stock);
        }

        // =============================================================================
        // TryApplyOwner
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida e applica al componente stock il nuovo owner richiesto.
        /// </para>
        ///
        /// <para><b>Principio architetturale: validazione Core prima della mutazione</b></para>
        /// <para>
        /// Il bridge UI puo' chiedere solo Community o NPC, ma il comando rivalida
        /// comunque contro il <c>World</c> corrente prima di modificare il dato
        /// oggettivo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Community</b>: owner id sempre normalizzato a zero.</item>
        ///   <item><b>Npc</b>: richiede un NPC ancora esistente.</item>
        ///   <item><b>Unsupported</b>: scarta None/Group o valori futuri non gestiti.</item>
        /// </list>
        /// </summary>
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

        // =============================================================================
        // ApplyOwnerSideEffects
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica gli effetti collaterali necessari quando cambia la proprieta'
        /// dello stock: sincronizza l'istanza oggetto e ripulisce il pin del vecchio
        /// proprietario NPC.
        /// </para>
        ///
        /// <para><b>Coerenza fra oggetto food e stock food</b></para>
        /// <para>
        /// Gli oggetti <c>food_stock</c> nascono con owner oggetto e owner stock
        /// allineati. Il dev-edit deve preservare questa relazione, altrimenti una
        /// parte dell'UI legge il nuovo owner dallo stock mentre un'altra continua a
        /// mostrare il vecchio owner dall'istanza oggetto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Cleanup pin</b>: rimuove la belief pinned solo se il vecchio NPC perde lo stock.</item>
        ///   <item><b>Sync oggetto</b>: copia OwnerKind/OwnerId dal FoodStockComponent validato.</item>
        /// </list>
        /// </summary>
        private void ApplyOwnerSideEffects(
            World world,
            WorldObjectInstance instance,
            OwnerKind previousOwnerKind,
            int previousOwnerId,
            FoodStockComponent stock)
        {
            if (previousOwnerKind == OwnerKind.Npc
                && previousOwnerId > 0
                && (stock.OwnerKind != OwnerKind.Npc || stock.OwnerId != previousOwnerId))
            {
                world.RemovePinnedFoodStockBelief(previousOwnerId, _objectId);
            }

            instance.OwnerKind = stock.OwnerKind;
            instance.OwnerId = stock.OwnerKind == OwnerKind.Npc ? stock.OwnerId : 0;
        }
    }
}
