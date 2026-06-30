using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // NpcInventorySlotKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Identifica gli slot fisici minimi dell'inventario typed personale di un NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: inventario reale, non credenza</b></para>
    /// <para>
    /// Questo enum descrive dove il <see cref="World"/> conserva oggettivamente un
    /// item trasportato. Non dice cosa l'NPC ricorda, crede o decide: quei livelli
    /// restano nella catena Perception/Memory/Belief/Decision. Le mani sono gia'
    /// slot espliciti per evitare una futura migrazione concettuale, ma in C1 gli
    /// stack di prodotti biologici e cibo vengono messi nel Pack.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: valore invalido/assente, normalizzato dalle API del World.</item>
    ///   <item><b>HandLeft</b>: mano sinistra visibile e non hidden.</item>
    ///   <item><b>HandRight</b>: mano destra visibile e non hidden.</item>
    ///   <item><b>Pack</b>: contenitore personale MVP per stack typed trasportabili.</item>
    /// </list>
    /// </summary>
    public enum NpcInventorySlotKind
    {
        None = 0,
        HandLeft = 10,
        HandRight = 20,
        Pack = 30
    }

    // =============================================================================
    // InventoryPlacementFlags
    // =============================================================================
    /// <summary>
    /// <para>
    /// Descrive in forma typed le collocazioni consentite da un <see cref="ObjectDef"/>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: authoring leggibile, runtime tipizzato</b></para>
    /// <para>
    /// Il catalogo JSON resta leggibile tramite bool espliciti come
    /// <c>CanPlaceInHand</c> o <c>CanEquipHead</c>. Il runtime pero' non deve
    /// propagare stringhe libere o controlli duplicati: questi flag sono la forma
    /// normalizzata usata dai sistemi inventario, save/load e job futuri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Hand/Container</b>: collocazioni operative MVP per mani e pack.</item>
    ///   <item><b>Equip...</b>: collocazioni equipaggiamento dichiarative, non ancora operative in C8.3.</item>
    /// </list>
    /// </summary>
    [Flags]
    public enum InventoryPlacementFlags
    {
        None = 0,
        Hand = 1 << 0,
        Container = 1 << 1,
        EquipHead = 1 << 2,
        EquipHands = 1 << 3,
        EquipUndergarment = 1 << 4,
        EquipOvergarment = 1 << 5,
        EquipArmor = 1 << 6,
        EquipFeet = 1 << 7,
        EquipSidearm = 1 << 8,
        EquipBack = 1 << 9
    }

    // =============================================================================
    // InventoryContainerKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Categoria typed minima di un contenitore inventario dichiarato nel catalogo oggetti.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contratto contenitori prima della feature contenitori</b></para>
    /// <para>
    /// C8.3 non rende zaini, borse o cinture operativi. Introduce pero' il tipo
    /// runtime stabile che i passi successivi useranno per distinguere contenitori
    /// piccoli, medi e grandi senza confrontare stringhe del JSON in piu' punti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: oggetto non contenitore o categoria non riconosciuta.</item>
    ///   <item><b>Small/Medium/Large</b>: famiglie semplici previste dal modello inventario.</item>
    /// </list>
    /// </summary>
    public enum InventoryContainerKind
    {
        None = 0,
        Small = 10,
        Medium = 20,
        Large = 30
    }

    // =============================================================================
    // ObjectInventoryContractResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver statico dei contratti inventario dichiarati da <see cref="ObjectDef"/>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: una sola regola runtime per collocazione e trasporto</b></para>
    /// <para>
    /// Prima di C8.3, il <see cref="World"/> e il loader save/load ripetevano localmente
    /// controlli simili su mano, pack e proprieta' legacy <c>Item</c>. Questo resolver
    /// centralizza la normalizzazione: il catalogo resta authoring data-driven, mentre
    /// il runtime consuma enum e flag coerenti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Placement</b>: converte i bool catalogo in <see cref="InventoryPlacementFlags"/>.</item>
    ///   <item><b>Container</b>: normalizza la stringa <c>ContainerKind</c>.</item>
    ///   <item><b>Runtime checks</b>: espone le regole condivise per trasportabilita' e slot MVP.</item>
    /// </list>
    /// </summary>
    public static class ObjectInventoryContractResolver
    {
        // =============================================================================
        // ResolvePlacementFlags
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte le collocazioni booleane del catalogo oggetti nei flag runtime typed.
        /// </para>
        /// </summary>
        public static InventoryPlacementFlags ResolvePlacementFlags(ObjectDef def)
        {
            if (def == null)
                return InventoryPlacementFlags.None;

            var flags = InventoryPlacementFlags.None;

            // Le due collocazioni operative C8 sono mani e contenitore/pack.
            if (def.CanPlaceInHand)
                flags |= InventoryPlacementFlags.Hand;

            if (def.CanPlaceInContainer)
                flags |= InventoryPlacementFlags.Container;

            // Le collocazioni equipaggiamento restano dichiarative in C8.3, ma
            // vengono gia' normalizzate qui per evitare un secondo passaggio futuro.
            if (def.CanEquipHead)
                flags |= InventoryPlacementFlags.EquipHead;

            if (def.CanEquipHands)
                flags |= InventoryPlacementFlags.EquipHands;

            if (def.CanEquipUndergarment)
                flags |= InventoryPlacementFlags.EquipUndergarment;

            if (def.CanEquipOvergarment)
                flags |= InventoryPlacementFlags.EquipOvergarment;

            if (def.CanEquipArmor)
                flags |= InventoryPlacementFlags.EquipArmor;

            if (def.CanEquipFeet)
                flags |= InventoryPlacementFlags.EquipFeet;

            if (def.CanEquipSidearm)
                flags |= InventoryPlacementFlags.EquipSidearm;

            if (def.CanEquipBack)
                flags |= InventoryPlacementFlags.EquipBack;

            return flags;
        }

        // =============================================================================
        // ResolveContainerKind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza la categoria contenitore dichiarata nel catalogo oggetti.
        /// </para>
        /// </summary>
        public static InventoryContainerKind ResolveContainerKind(ObjectDef def)
        {
            if (!IsContainer(def))
                return InventoryContainerKind.None;

            string rawKind = def.ContainerKind;
            if (string.IsNullOrWhiteSpace(rawKind))
                return InventoryContainerKind.None;

            string normalizedKind = rawKind.Trim();

            // Accettiamo sia la forma breve sia quella esplicita usabile nel JSON.
            if (string.Equals(normalizedKind, "Small", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedKind, "SmallContainer", StringComparison.OrdinalIgnoreCase))
            {
                return InventoryContainerKind.Small;
            }

            if (string.Equals(normalizedKind, "Medium", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedKind, "MediumContainer", StringComparison.OrdinalIgnoreCase))
            {
                return InventoryContainerKind.Medium;
            }

            if (string.Equals(normalizedKind, "Large", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedKind, "LargeContainer", StringComparison.OrdinalIgnoreCase))
            {
                return InventoryContainerKind.Large;
            }

            return InventoryContainerKind.None;
        }

        // =============================================================================
        // IsTransportable
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se la definizione puo' entrare nel modello inventario typed.
        /// </para>
        /// </summary>
        public static bool IsTransportable(ObjectDef def, bool hasPhysicalObjectId)
        {
            if (def == null)
                return false;

            // Gli oggetti fisici reali conservano la compatibilita' C5: la loro
            // trasportabilita' finale verra' comunque filtrata da CanPlaceInSlot.
            if (hasPhysicalObjectId)
                return true;

            if (ResolvePlacementFlags(def) != InventoryPlacementFlags.None)
                return true;

            // Fallback legacy controllato: evita di rompere item gia' dichiarati
            // prima dell'introduzione dei bool di collocazione.
            return HasPositiveObjectProperty(def, "Item")
                || HasPositiveObjectProperty(def, "FoodItem")
                || HasPositiveObjectProperty(def, "FoodStock")
                || HasPositiveObjectProperty(def, "Material")
                || HasPositiveObjectProperty(def, "SeedItem")
                || HasPositiveObjectProperty(def, "Tool")
                || HasPositiveObjectProperty(def, "BiologicalProduct");
        }

        // =============================================================================
        // CanPlaceInSlot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se una definizione oggetto puo' stare nello slot inventario MVP richiesto.
        /// </para>
        /// </summary>
        public static bool CanPlaceInSlot(ObjectDef def, NpcInventorySlotKind slot)
        {
            if (def == null)
                return false;

            if (slot == NpcInventorySlotKind.HandLeft || slot == NpcInventorySlotKind.HandRight)
                return HasPlacement(def, InventoryPlacementFlags.Hand)
                    || HasPositiveObjectProperty(def, "Item");

            if (slot == NpcInventorySlotKind.Pack)
                return HasPlacement(def, InventoryPlacementFlags.Container)
                    || HasPositiveObjectProperty(def, "Item");

            return false;
        }

        // =============================================================================
        // IsContainer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se la definizione e' dichiarata come contenitore nel catalogo oggetti.
        /// </para>
        /// </summary>
        public static bool IsContainer(ObjectDef def)
        {
            return def != null && def.IsContainer;
        }

        // =============================================================================
        // HasPlacement
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la definizione possiede tutti i flag di collocazione richiesti.
        /// </para>
        /// </summary>
        public static bool HasPlacement(ObjectDef def, InventoryPlacementFlags flag)
        {
            if (flag == InventoryPlacementFlags.None)
                return false;

            var flags = ResolvePlacementFlags(def);
            return (flags & flag) == flag;
        }

        private static bool HasPositiveObjectProperty(ObjectDef def, string key)
        {
            return def != null
                && def.TryGetPropertyValue(key, out float value)
                && value > 0f;
        }
    }

    // =============================================================================
    // InventoryMutationResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato compatto prodotto dalle API autorizzate di mutazione inventario.
    /// </para>
    ///
    /// <para><b>Principio architetturale: evento derivato dalla mutazione, non da rilettura fragile</b></para>
    /// <para>
    /// I command devono pubblicare eventi usando il fatto appena applicato dal
    /// <see cref="World"/>, non ricostruendo a posteriori lo stato dagli store. Il
    /// result conserva quindi l'oggetto fisico coinvolto, la quantita' realmente
    /// cambiata e gli slot prima/dopo quando la mutazione riguarda una collocazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: NPC proprietario fisico dell'inventario mutato.</item>
    ///   <item><b>ObjectId/DefId</b>: oggetto fisico e definizione catalogo coinvolti.</item>
    ///   <item><b>QuantityChanged</b>: quantita' realmente aggiunta, rimossa o spostata.</item>
    ///   <item><b>SlotKind/PreviousSlotKind</b>: collocazione finale e precedente.</item>
    /// </list>
    /// </summary>
    public readonly struct InventoryMutationResult
    {
        public readonly int NpcId;
        public readonly int ObjectId;
        public readonly string DefId;
        public readonly int QuantityChanged;
        public readonly NpcInventorySlotKind SlotKind;
        public readonly NpcInventorySlotKind PreviousSlotKind;

        public bool HasMutation => NpcId > 0 && QuantityChanged > 0;

        public InventoryMutationResult(
            int npcId,
            int objectId,
            string defId,
            int quantityChanged,
            NpcInventorySlotKind slotKind,
            NpcInventorySlotKind previousSlotKind)
        {
            NpcId = npcId;
            ObjectId = objectId;
            DefId = defId ?? string.Empty;
            QuantityChanged = quantityChanged < 0 ? 0 : quantityChanged;
            SlotKind = slotKind;
            PreviousSlotKind = previousSlotKind;
        }

        public static InventoryMutationResult None =>
            new InventoryMutationResult(0, 0, string.Empty, 0, NpcInventorySlotKind.None, NpcInventorySlotKind.None);
    }

    // =============================================================================
    // ObjectStackComponent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Componente oggetto che rappresenta una pila fisica reale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stack fisico, non quantita' astratta</b></para>
    /// <para>
    /// Uno stack di mele, bacche o assi non e' una riga astratta dentro
    /// l'inventario: e' un <see cref="WorldObjectInstance"/> reale con un
    /// componente quantita'. L'entry inventario dice solo dove si trova quello
    /// stack fisico.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Quantity</b>: numero di unita' equivalenti rappresentate dall'oggetto fisico.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ObjectStackComponent
    {
        public int Quantity;

        public ObjectStackComponent()
        {
            Quantity = 1;
        }

        public ObjectStackComponent(int quantity)
        {
            Quantity = quantity <= 0 ? 1 : quantity;
        }
    }

    // =============================================================================
    // NpcInventoryEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Entry oggettiva dell'inventario fisico di un singolo NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: inventario come collocazione</b></para>
    /// <para>
    /// L'entry non duplica tipo catalogo, quantita', peso, ingombro o stato
    /// dell'oggetto. Conserva soltanto il riferimento all'oggetto fisico reale e
    /// la collocazione personale. Tutti i dati materiali vengono risolti da
    /// <see cref="World.Objects"/>, dal catalogo oggetti e dai component store
    /// oggetto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EntryId</b>: identificatore locale stabile nello scope dell'inventario NPC.</item>
    ///   <item><b>ObjectId</b>: oggetto fisico reale trasportato; 0 e' invalido.</item>
    ///   <item><b>SlotKind</b>: collocazione fisica personale.</item>
    ///   <item><b>ContainerObjectId</b>: contenitore fisico specifico, futuro; 0 indica pack MVP.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcInventoryEntry
    {
        public int EntryId;
        public int ObjectId;
        public NpcInventorySlotKind SlotKind;
        public int ContainerObjectId;

        public NpcInventoryEntry()
        {
            ObjectId = 0;
            SlotKind = NpcInventorySlotKind.Pack;
            ContainerObjectId = 0;
        }
    }

    // =============================================================================
    // NpcInventoryState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Store oggettivo dell'inventario typed di un NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: piccolo modulo dati isolato</b></para>
    /// <para>
    /// Lo stato e' volutamente semplice: una lista di entry e un contatore locale.
    /// Non implementa pathfinding, UI, fame, furto o save/load. Quelle parti
    /// useranno API del <see cref="World"/> nei sotto-step successivi, senza
    /// accedere direttamente alla lista quando non serve.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Entries</b>: lista mutabile posseduta dal World.</item>
    ///   <item><b>NextEntryId</b>: prossimo id locale per entry nuove.</item>
    /// </list>
    /// </summary>
    public sealed class NpcInventoryState
    {
        public readonly List<NpcInventoryEntry> Entries = new(8);
        public int NextEntryId = 1;

        // =============================================================================
        // AllocateEntryId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il prossimo identificatore locale libero dell'inventario.
        /// </para>
        ///
        /// <para><b>Principio architetturale: identita' locale piccola</b></para>
        /// <para>
        /// L'id entry non e' globale come gli objectId del World. Serve solo a
        /// distinguere due entry nello stesso inventario, per esempio quando in
        /// futuro un comando vorra' consumare o spostare una specifica entry.
        /// </para>
        /// </summary>
        public int AllocateEntryId()
        {
            // Manteniamo il contatore positivo anche se un load futuro dovesse
            // ripristinare dati minimi o parziali: EntryId <= 0 resta invalido.
            if (NextEntryId <= 0)
                NextEntryId = 1;

            int id = NextEntryId;
            NextEntryId++;
            return id;
        }
    }
}
