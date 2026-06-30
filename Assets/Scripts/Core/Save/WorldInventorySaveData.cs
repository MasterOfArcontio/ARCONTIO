using System;

namespace Arcontio.Core.Save
{
    // =============================================================================
    // WorldInventorySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO radice del modulo save/load inventario typed NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: modulo inventario impermeabile</b></para>
    /// <para>
    /// Questa sezione conserva soltanto lo stato dell'inventario typed introdotto
    /// in v0.71.05.C: entry per NPC e componenti stack degli oggetti fisici. Non
    /// legge, non salva e non migra <c>NpcPrivateFood</c>, che resta materiale
    /// legacy separato fino alla sua rimozione definitiva.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcInventories</b>: inventari typed indicizzati per npcId.</item>
    ///   <item><b>objectStacks</b>: componenti quantita' associati a objectId reali.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class WorldInventorySaveData
    {
        public NpcInventorySaveData[] npcInventories = Array.Empty<NpcInventorySaveData>();
        public ObjectStackSaveData[] objectStacks = Array.Empty<ObjectStackSaveData>();
    }

    // =============================================================================
    // NpcInventorySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per l'inventario typed di un singolo NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato fisico, non conoscenza soggettiva</b></para>
    /// <para>
    /// Il record dice quali oggetti fisici sono collocati addosso all'NPC. Non
    /// duplica defId, quantita', peso o nutrizione: quei dati restano negli oggetti
    /// del World, nel catalogo e nei componenti stack.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcId</b>: proprietario fisico dell'inventario.</item>
    ///   <item><b>nextEntryId</b>: prossimo id locale da preservare dopo load.</item>
    ///   <item><b>entries</b>: collocazioni degli oggetti trasportati.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcInventorySaveData
    {
        public int npcId;
        public int nextEntryId;
        public NpcInventoryEntrySaveData[] entries = Array.Empty<NpcInventoryEntrySaveData>();
    }

    // =============================================================================
    // NpcInventoryEntrySaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per una entry inventario typed.
    /// </para>
    ///
    /// <para><b>Principio architetturale: entry come riferimento a oggetto reale</b></para>
    /// <para>
    /// L'entry salva solo identita' locale, objectId e collocazione. Il tipo
    /// materiale dell'oggetto si risolve tramite <c>WorldSaveData.objects</c>; la
    /// quantita' si risolve tramite <see cref="ObjectStackSaveData"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>entryId</b>: id locale nello scope dell'inventario NPC.</item>
    ///   <item><b>objectId</b>: oggetto fisico held referenziato.</item>
    ///   <item><b>slotKind</b>: enum <c>NpcInventorySlotKind</c> serializzato come int.</item>
    ///   <item><b>containerObjectId</b>: contenitore fisico futuro, 0 per pack MVP.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcInventoryEntrySaveData
    {
        public int entryId;
        public int objectId;
        public int slotKind;
        public int containerObjectId;
    }

    // =============================================================================
    // ObjectStackSaveData
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per il componente quantita' di uno stack fisico.
    /// </para>
    ///
    /// <para><b>Principio architetturale: componente separato dall'inventario</b></para>
    /// <para>
    /// Una pila di cinque bacche e' un oggetto fisico reale piu' un componente
    /// quantita'. Persistiamo il componente separatamente per non trasformare
    /// l'inventario in una lista astratta di defId e quantita'.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>objectId</b>: oggetto fisico che possiede lo stack.</item>
    ///   <item><b>quantity</b>: unita' equivalenti rappresentate dallo stack.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ObjectStackSaveData
    {
        public int objectId;
        public int quantity;
    }
}
