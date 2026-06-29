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
    // NpcInventoryEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Entry oggettiva dell'inventario typed di un singolo NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: item typed separato dal job</b></para>
    /// <para>
    /// L'entry conserva solo stato reale e serializzabile: tipo oggetto, quantita',
    /// slot e, quando esiste, riferimento a un'istanza fisica del mondo. Non
    /// contiene intenzioni, sorgenti percettive, score decisionali o logica di
    /// raccolta. Questo permette ai job futuri di produrre comandi semplici senza
    /// diventare proprietari dei dati inventario.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>EntryId</b>: identificatore locale stabile nello scope dell'inventario NPC.</item>
    ///   <item><b>DefId</b>: tipo catalogo oggetto trasportato.</item>
    ///   <item><b>Quantity</b>: quantita' dello stack o 1 per oggetti fisici unici.</item>
    ///   <item><b>SlotKind</b>: slot fisico personale.</item>
    ///   <item><b>ObjectId</b>: oggetto fisico collegato, oppure 0 per stack astratti typed.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NpcInventoryEntry
    {
        public int EntryId;
        public string DefId;
        public int Quantity;
        public NpcInventorySlotKind SlotKind;
        public int ObjectId;

        public bool HasObject => ObjectId > 0;

        public NpcInventoryEntry()
        {
            DefId = string.Empty;
            SlotKind = NpcInventorySlotKind.Pack;
            Quantity = 0;
            ObjectId = 0;
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
