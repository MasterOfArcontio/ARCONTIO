// Assets/Scripts/Core/Commands/DevTools/DevPlaceObjectCommand.cs
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    // =============================================================================
    // DevPlaceObjectCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando di debug runtime che piazza un oggetto in una cella della griglia.
    /// Supporta sia oggetti generici sia casi specializzati usati dalla finestra
    /// DevTools: cibo con proprieta' logica e porte con stato aperta/chiusa/locked.
    /// </para>
    ///
    /// <para><b>Separazione View / Core tramite CommandBuffer</b></para>
    /// <para>
    /// La UI non modifica direttamente il <c>World</c>. L'overlay produce una richiesta
    /// intenzionale, il <c>SimulationHost</c> la accoda, e questo comando applica le
    /// scritture al mondo simulativo con le validazioni corrette.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Parametri base</b>: defId e cella di destinazione.</item>
    ///   <item><b>Ownership</b>: proprieta' logica usata da oggetti e food stock.</item>
    ///   <item><b>Food options</b>: quantita' e proprietario dello stock a terra.</item>
    ///   <item><b>Door options</b>: stato iniziale aperta/chiusa e lock opzionale.</item>
    /// </list>
    /// </summary>
    public sealed class DevPlaceObjectCommand : ICommand
    {
        private readonly string _defId;
        private readonly int _x;
        private readonly int _y;
        private readonly OwnerKind _ownerKind;
        private readonly int _ownerId;
        private readonly int _foodUnits;
        private readonly bool? _doorOpen;
        private readonly bool? _doorLocked;

        // =============================================================================
        // DevPlaceObjectCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una richiesta di piazzamento oggetto per i DevTools runtime.
        /// I parametri opzionali mantengono compatibile il vecchio uso generico e
        /// permettono alla UI di nascondere all'utente i defId tecnici di porte/cibo.
        /// </para>
        ///
        /// <para><b>Integrazione progressiva</b></para>
        /// <para>
        /// Il comando resta uno solo per il piazzamento oggetti. La specializzazione
        /// avviene solo dopo la creazione dell'istanza, quando il core sa se il defId
        /// rappresenta davvero cibo o una porta.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>defId</b>: definizione oggetto caricata dal database.</item>
        ///   <item><b>x/y</b>: cella bersaglio della mappa.</item>
        ///   <item><b>ownerKind/ownerId</b>: proprieta' logica oggettiva.</item>
        ///   <item><b>foodUnits</b>: unita' iniziali dello stock se pertinente.</item>
        ///   <item><b>doorOpen/doorLocked</b>: stato iniziale porta se pertinente.</item>
        /// </list>
        /// </summary>
        public DevPlaceObjectCommand(
            string defId,
            int x,
            int y,
            OwnerKind ownerKind = OwnerKind.Community,
            int ownerId = 0,
            int foodUnits = 1,
            bool? doorOpen = null,
            bool? doorLocked = null)
        {
            _defId = defId;
            _x = x;
            _y = y;
            _ownerKind = ownerKind;
            _ownerId = ownerId;
            _foodUnits = foodUnits;
            _doorOpen = doorOpen;
            _doorLocked = doorLocked;
        }

        // =============================================================================
        // Execute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue il piazzamento nel <c>World</c>, sostituendo eventuali oggetti gia'
        /// presenti nella cella e applicando componenti/stati specializzati.
        /// </para>
        ///
        /// <para><b>Source of truth nel Core</b></para>
        /// <para>
        /// Cibo e porte non sono solo elementi visuali: influenzano bisogno, furto,
        /// pathfinding, visione e memoria. Per questo il comando usa API del core come
        /// <c>SetFoodStock</c> e <c>SetDoorOpen</c> invece di lasciare alla View la
        /// responsabilita' di tenere sincronizzate le cache derivate.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione</b>: scarta mondo nullo, defId vuoto, cella fuori bounds o ObjectDef mancante.</item>
        ///   <item><b>Replace sicuro</b>: rimuove un oggetto preesistente solo se non occupato.</item>
        ///   <item><b>CreateObject</b>: crea l'istanza base con ownership logica.</item>
        ///   <item><b>ApplyFoodOptions</b>: configura il componente food stock quando serve.</item>
        ///   <item><b>ApplyDoorOptions</b>: configura stato porta e cache movimento/visione quando serve.</item>
        /// </list>
        /// </summary>
        public void Execute(World world, MessageBus bus)
        {
            if (world == null) return;
            if (string.IsNullOrWhiteSpace(_defId)) return;
            if (!world.InBounds(_x, _y)) return;

            if (!world.TryGetObjectDef(_defId, out var def) || def == null)
            {
                Debug.LogWarning($"[DevTools] Place failed: unknown defId='{_defId}'.");
                return;
            }

            int existing = world.GetObjectAt(_x, _y);
            if (existing >= 0)
            {
                // Un letto o altro oggetto occupato da NPC non va sostituito al volo:
                // evitiamo uno stato in cui il core pensa ancora che un NPC occupi un
                // oggetto ormai distrutto o rimpiazzato.
                if (world.Objects.TryGetValue(existing, out var inst) && inst != null && inst.OccupantNpcId >= 0)
                {
                    Debug.LogWarning($"[DevTools] Place blocked: cell ({_x},{_y}) object={existing} is occupied by NPC={inst.OccupantNpcId}.");
                    return;
                }

                world.DestroyObject(existing);
            }

            int objId = world.CreateObject(_defId, _x, _y, _ownerKind, _ownerId);
            if (objId < 0)
                return;

            ApplyFoodOptions(world, objId);
            ApplyDoorOptions(world, def, objId);

            world.RebuildDerivedCachesGlobal();
        }

        // =============================================================================
        // ApplyFoodOptions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il componente <c>FoodStockComponent</c> quando l'oggetto piazzato e'
        /// il marker di cibo a terra. Per qualunque altro oggetto non produce effetti.
        /// </para>
        ///
        /// <para><b>Fatto oggettivo e proprieta' del cibo</b></para>
        /// <para>
        /// La proprieta' dello stock viene scritta nel componente oggettivo, perche'
        /// i sistemi di decisione, furto e memoria leggono <c>OwnerKind</c> e
        /// <c>OwnerId</c> come fatto simulativo, non come annotazione della UI.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Filtro defId</b>: limita la logica al solo <c>food_stock</c>.</item>
        ///   <item><b>Clamp quantita'</b>: garantisce almeno una porzione iniziale.</item>
        ///   <item><b>SetFoodStock</b>: usa l'API ufficiale del World e aggiorna eventuali belief pinned.</item>
        /// </list>
        /// </summary>
        private void ApplyFoodOptions(World world, int objId)
        {
            if (_defId != "food_stock")
                return;

            int units = Mathf.Max(1, _foodUnits);
            world.SetFoodStock(objId, new FoodStockComponent
            {
                Units = units,
                OwnerKind = _ownerKind,
                OwnerId = _ownerKind == OwnerKind.Npc ? _ownerId : 0
            });
        }

        // =============================================================================
        // ApplyDoorOptions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica lo stato iniziale di una porta appena piazzata. L'apertura passa
        /// sempre da <c>World.SetDoorOpen</c>, mentre il lock viene scritto solo sulle
        /// porte che dichiarano <c>IsLockable</c>.
        /// </para>
        ///
        /// <para><b>Cache derivate coerenti</b></para>
        /// <para>
        /// Lo stato aperta/chiusa modifica movimento, visione e occlusione. Per questo
        /// non scriviamo mai <c>IsOpen</c> direttamente: usiamo il punto unico del
        /// <c>World</c> che aggiorna anche le cache.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Filtro porta</b>: ignora oggetti non porta.</item>
        ///   <item><b>Lock condizionale</b>: accetta locked solo su def lockable.</item>
        ///   <item><b>Open state</b>: usa <c>SetDoorOpen</c> per lo stato aperto/chiuso.</item>
        /// </list>
        /// </summary>
        private void ApplyDoorOptions(World world, ObjectDef def, int objId)
        {
            if (def == null || !def.IsDoor)
                return;

            if (world.Objects.TryGetValue(objId, out var instance) && instance != null)
            {
                bool wantsLocked = _doorLocked.GetValueOrDefault(false);
                instance.IsLocked = def.IsLockable && wantsLocked;
            }

            if (_doorOpen.HasValue)
                world.SetDoorOpen(objId, _doorOpen.Value);
        }
    }
}
