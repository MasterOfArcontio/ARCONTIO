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
