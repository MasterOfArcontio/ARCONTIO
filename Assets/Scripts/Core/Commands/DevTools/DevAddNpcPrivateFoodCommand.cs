// Assets/Scripts/Core/Commands/DevTools/DevAddNpcPrivateFoodCommand.cs
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    // =============================================================================
    // DevAddNpcPrivateFoodCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando di debug runtime che aggiunge cibo privato direttamente addosso a un
    /// NPC, cioe' nello store oggettivo <c>World.NpcPrivateFood</c>. Questo rappresenta
    /// il cibo fisicamente trasportato dall'NPC e non uno stock appoggiato nella mappa.
    /// </para>
    ///
    /// <para><b>Separazione View / Core tramite CommandBuffer</b></para>
    /// <para>
    /// La finestra DevTools non deve modificare direttamente il mondo simulativo.
    /// Per questo l'overlay F3/F2 costruisce un comando, lo accoda al
    /// <c>SimulationHost</c> e lascia che sia il core a validare l'NPC, la quantita'
    /// e la capienza. In questo modo il devtool resta modulare e non introduce un
    /// accesso globale mutabile dalla UI.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_npcId</b>: NPC destinatario del cibo trasportato.</item>
    ///   <item><b>_units</b>: quantita' richiesta dalla UI debug.</item>
    ///   <item><b>Execute</b>: valida lo stato del mondo e applica un incremento clampato alla capienza inventario.</item>
    /// </list>
    /// </summary>
    public sealed class DevAddNpcPrivateFoodCommand : ICommand
    {
        private readonly int _npcId;
        private readonly int _units;

        // =============================================================================
        // DevAddNpcPrivateFoodCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una richiesta di aggiunta cibo per un singolo NPC. Il costruttore
        /// conserva soltanto i parametri grezzi della UI; la validazione vera resta in
        /// <see cref="Execute"/>, quando il comando vede lo stato aggiornato del mondo.
        /// </para>
        ///
        /// <para><b>Command come dato intenzionale</b></para>
        /// <para>
        /// La UI dichiara "voglio aggiungere N unita' a questo NPC", ma non decide se
        /// l'NPC esiste ancora o quanta capienza libera abbia nel tick di esecuzione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>npcId</b>: identificatore runtime dell'NPC selezionato.</item>
        ///   <item><b>units</b>: incremento richiesto, normalizzato solo durante Execute.</item>
        /// </list>
        /// </summary>
        public DevAddNpcPrivateFoodCommand(int npcId, int units)
        {
            _npcId = npcId;
            _units = units;
        }

        // =============================================================================
        // Execute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica l'aggiunta di cibo privato all'NPC, mantenendo lo stato entro la
        /// capienza inventario esposta dal <c>World</c>. Se l'operazione non e'
        /// possibile, il comando esce senza effetti collaterali.
        /// </para>
        ///
        /// <para><b>Coerenza sistemica dell'inventario</b></para>
        /// <para>
        /// Anche se questo e' un devtool, evita di creare inventari oltre capienza:
        /// molti sistemi successivi leggono <c>NpcPrivateFood</c> come fatto oggettivo
        /// e assumono che rappresenti cibo trasportabile dall'NPC.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione NPC</b>: scarta id assenti o non piu' vivi.</item>
        ///   <item><b>Clamp quantita'</b>: limita l'aggiunta alla capienza libera.</item>
        ///   <item><b>Scrittura store</b>: aggiorna <c>NpcPrivateFood</c> come source of truth oggettiva.</item>
        /// </list>
        /// </summary>
        public void Execute(World world, MessageBus bus)
        {
            if (world == null)
                return;

            // L'NPC puo' essere stato cancellato fra click UI ed esecuzione del comando:
            // la command queue deve quindi validare sempre contro lo stato corrente.
            if (!world.ExistsNpc(_npcId))
            {
                Debug.LogWarning($"[DevTools] AddNpcPrivateFood blocked: NPC={_npcId} does not exist.");
                return;
            }

            // Quantita' non positive non hanno significato operativo nel devtool.
            // Se in futuro servira' rimuovere cibo, meglio introdurre un comando esplicito.
            if (_units <= 0)
                return;

            int currentFood = 0;
            if (world.NpcPrivateFood.TryGetValue(_npcId, out int existingFood))
                currentFood = existingFood < 0 ? 0 : existingFood;

            // Usiamo l'helper del World per restare allineati con eventuali override futuri
            // di capienza per archetipo, ruolo o tratto individuale.
            int freeCapacity = world.GetInventoryFreeCapacity(_npcId);
            if (freeCapacity <= 0)
            {
                Debug.LogWarning($"[DevTools] AddNpcPrivateFood blocked: NPC={_npcId} inventory is full.");
                return;
            }

            int addedUnits = Mathf.Min(_units, freeCapacity);
            world.NpcPrivateFood[_npcId] = currentFood + addedUnits;
        }
    }
}
