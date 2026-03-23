using System;

namespace Arcontio.Core
{
    /// <summary>
    /// NpcObjectMemoryStore (GENERALIZED):
    /// Memoria "sparsa" a slot fissi per ENTITÀ OSSERVATE dall'NPC.
    ///
    /// Day10 baseline (storico):
    /// - questo store nasceva come memoria ad-hoc solo per "oggetti interagibili" (letto/cibo/workbench).
    ///
    /// Step3 (questa patch):
    /// - lo GENERALIZZIAMO per includere anche NPC (e, in futuro, "oggetti importanti trasportati da NPC").
    ///
    /// Perché questa scelta è importante:
    /// - Le MemoryTrace narrative restano perfette per "storie" (predatori, crimini, morte…)
    /// - Ma per query pratiche del tipo:
    ///     - "dov'è l'ultimo letto che ricordo?"
    ///     - "chi aveva cibo addosso l'ultima volta che l'ho visto?"
    ///   una struttura a slot fissi è più compatta, più deterministica e più semplice da interrogare
    ///   rispetto ad un mare di MemoryTrace eterogenee.
    ///
    /// Nota di compatibilità:
    /// - Manteniamo il nome NpcObjectMemoryStore perché è già cablato in:
    ///   World.NpcObjectMemory + MapGrid overlay + MemoryEncodingSystem.
    /// - Internamente però ora lavoriamo con un "subject" generico:
    ///   SubjectKind + SubjectId.
    /// </summary>
    public sealed class NpcObjectMemoryStore
    {
        /// <summary>
        /// SubjectKind:
        /// Che tipo di "cosa osservata" rappresenta lo slot.
        ///
        /// - WorldObject: un oggetto del mondo (es. FoodStock, Bed, Door…)
        /// - Npc: un altro NPC osservato
        ///
        /// Nota:
        /// - In futuro potremmo estendere a "Animal", "FactionAgent", ecc.,
        ///   ma per ora manteniamo il minimo indispensabile.
        /// </summary>
        public enum SubjectKind : byte
        {
            None = 0,
            WorldObject = 1,
            Npc = 2
        }

        /// <summary>
        /// Flags:
        /// Bitmask per "facts" leggeri osservati sul subject.
        ///
        /// Importante:
        /// - Non vogliamo trasformare questo store in un database arbitrario.
        /// - Lo usiamo per un set MOLTO piccolo di feature che servono spesso alle Rules.
        ///
        /// Oggi:
        /// - HasCarriedFood: l'NPC osservato aveva del cibo addosso (quantità potenzialmente stimata).
        /// </summary>
        [Flags]
        public enum ObservedFlags : ushort
        {
            None = 0,
            HasCarriedFood = 1 << 0
        }

        public readonly Entry[] Slots;
        public readonly int Capacity;
        public int Count;

        public NpcObjectMemoryStore(int capacity)
        {
            Capacity = capacity < 1 ? 1 : capacity;
            Slots = new Entry[Capacity];
            Count = 0;
        }

        public struct Entry
        {
            public bool IsValid;

            // ============================================================
            // SUBJECT (identità generica)
            // ============================================================

            public SubjectKind Kind;

            /// <summary>
            /// SubjectId:
            /// - per WorldObject: di solito = ObjectId (istanza), se disponibile
            /// - per Npc: = npcId
            /// </summary>
            public int SubjectId;

            // ============================================================
            // COMPAT FIELDS (storici, per oggetti)
            // ============================================================

            // Identità “logica” (solo per WorldObject, utile per debug/UI)
            public string DefId;

            /// <summary>
            /// ObjectId:
            /// Campo storico (Day10). Per compat:
            /// - per Kind=WorldObject, coincide tipicamente con SubjectId (se != 0).
            /// - per Kind=Npc, rimane 0.
            /// </summary>
            public int ObjectId;

            // Dove (vale sia per oggetti che per NPC: è "last known cell")
            public int CellX;
            public int CellY;

            // Ownership (utile per “è mio/non è mio”, vale soprattutto per WorldObject)
            public OwnerKind OwnerKind;
            public int OwnerId;

            // Recenza/affidabilità/utility
            public int LastSeenTick;
            public float Reliability01;
            public float UtilityScore01;

            // Pin: mai buttare se è “mio”
            public bool IsPinned;

            // ============================================================
            // FACTS (per NPC osservati / future estensioni)
            // ============================================================

            public ObservedFlags Flags;

            /// <summary>
            /// CarriedFoodUnitsApprox:
            /// Stima/quantità osservata di cibo portato dall'NPC (solo se Flags.HasCarriedFood).
            ///
            /// Nota:
            /// - Questa non deve essere usata come "verità": è una stima/ricordo.
            /// - La verità viene verificata a runtime in execution (Command handler),
            ///   secondo la policy ARCONTIO: il mondo è oggettivo, la mente è soggettiva.
            /// </summary>
            public int CarriedFoodUnitsApprox;
        }

        // ============================================================
        // UPSERT: WorldObject
        // ============================================================

        /// <summary>
        /// UpsertWorldObject:
        /// Inserisce o aggiorna una entry che rappresenta un OGGETTO osservato.
        ///
        /// Policy:
        /// - merge se stesso subject (ObjectId) o stesso DefId+cella (fallback)
        /// - altrimenti inserisci in slot libero o rimpiazza il “peggiore”
        /// </summary>
        public void UpsertWorldObject(
            int nowTick,
            string defId,
            int objectId,
            int x, int y,
            OwnerKind ownerKind,
            int ownerId,
            float reliability01,
            float utility01,
            bool pinIfOwnedByNpc,
            int npcIdForPinLogic
        )
        {
            // Implementation detail:
            // - per WorldObject, se objectId != 0 usiamo quello come SubjectId (più stabile).
            // - se objectId == 0, manteniamo la logica storica DefId+cella.
            int subjectId = objectId;

            UpsertInternal(
                nowTick,
                SubjectKind.WorldObject,
                subjectId,
                defId,
                objectId,
                x, y,
                ownerKind,
                ownerId,
                reliability01,
                utility01,
                pinIfOwnedByNpc,
                npcIdForPinLogic,
                ObservedFlags.None,
                0
            );
        }

        // ============================================================
        // UPSERT: NPC
        // ============================================================

        /// <summary>
        /// UpsertNpc:
        /// Inserisce o aggiorna una entry che rappresenta un NPC osservato.
        ///
        /// Parametri chiave:
        /// - npcIdObserved: ID dell'NPC "oggetto" di memoria
        /// - lastKnownCell: dove l'ho visto l'ultima volta (o dove credo sia)
        ///
        /// Facts attuali:
        /// - carriedFoodUnitsApprox + HasCarriedFood
        ///
        /// Nota:
        /// - utility01 qui è un placeholder: in futuro dovrebbe dipendere da Goals/Needs.
        /// </summary>
        public void UpsertNpc(
            int nowTick,
            int npcIdObserved,
            int x, int y,
            float reliability01,
            float utility01,
            ObservedFlags flags,
            int carriedFoodUnitsApprox
        )
        {
            UpsertInternal(
                nowTick,
                SubjectKind.Npc,
                npcIdObserved,
                defId: "npc",         // debug-friendly: non è una "def" reale
                objectId: 0,          // non è un oggetto-world
                x, y,
                ownerKind: OwnerKind.None,
                ownerId: -1,
                reliability01,
                utility01,
                pinIfOwnedByNpc: false, // non ha senso "pin" su NPC in v0
                npcIdForPinLogic: -1,
                flags,
                carriedFoodUnitsApprox
            );
        }

        // ============================================================
        // INTERNAL UPSERT CORE
        // ============================================================

        /// <summary>
        /// UpsertInternal:
        /// Nucleo comune di upsert per WorldObject e Npc.
        ///
        /// Matching:
        /// - prima prova: stesso Kind + SubjectId (quando SubjectId != 0)
        /// - fallback (solo per WorldObject): DefId + cella
        ///
        /// Eviction:
        /// - non rimpiazza mai IsPinned
        /// - metrica semplice: più vecchio e meno utile/affidabile = peggio
        /// </summary>
        private void UpsertInternal(
            int nowTick,
            SubjectKind kind,
            int subjectId,
            string defId,
            int objectId,
            int x, int y,
            OwnerKind ownerKind,
            int ownerId,
            float reliability01,
            float utility01,
            bool pinIfOwnedByNpc,
            int npcIdForPinLogic,
            ObservedFlags flags,
            int carriedFoodUnitsApprox
        )
        {
            // 1) merge se già presente
            for (int i = 0; i < Slots.Length; i++)
            {
                if (!Slots[i].IsValid) continue;

                bool same =
                    (Slots[i].Kind == kind && Slots[i].SubjectId != 0 && subjectId != 0 && Slots[i].SubjectId == subjectId)
                    ||
                    (kind == SubjectKind.WorldObject && Slots[i].Kind == SubjectKind.WorldObject && Slots[i].DefId == defId && Slots[i].CellX == x && Slots[i].CellY == y);

                if (!same) continue;

                var e = Slots[i];

                e.Kind = kind;
                e.SubjectId = subjectId;

                // compat fields
                e.DefId = defId;
                e.ObjectId = objectId;

                // pos e recenza
                e.CellX = x;
                e.CellY = y;
                e.LastSeenTick = nowTick;

                // Merge “prendi la migliore”
                if (reliability01 > e.Reliability01) e.Reliability01 = reliability01;
                if (utility01 > e.UtilityScore01) e.UtilityScore01 = utility01;

                // Owner (solo per world object; per npc resta None/-1)
                e.OwnerKind = ownerKind;
                e.OwnerId = ownerId;

                // Facts
                e.Flags |= flags; // merge: se ho visto un fact almeno una volta, lo mantengo finché non decade/cleanup
                if ((flags & ObservedFlags.HasCarriedFood) != 0)
                {
                    // Merge: conserviamo la stima più alta (euristica neutra).
                    if (carriedFoodUnitsApprox > e.CarriedFoodUnitsApprox)
                        e.CarriedFoodUnitsApprox = carriedFoodUnitsApprox;
                }

                // Pin se è mio (vale solo per oggetti, ma lasciamo la condizione generale)
                if (pinIfOwnedByNpc && ownerKind == OwnerKind.Npc && ownerId == npcIdForPinLogic)
                    e.IsPinned = true;

                Slots[i] = e;
                return;
            }

            // 2) inserisci in slot libero
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].IsValid) continue;

                Slots[i] = new Entry
                {
                    IsValid = true,

                    Kind = kind,
                    SubjectId = subjectId,

                    DefId = defId,
                    ObjectId = objectId,

                    CellX = x,
                    CellY = y,

                    OwnerKind = ownerKind,
                    OwnerId = ownerId,

                    LastSeenTick = nowTick,
                    Reliability01 = reliability01,
                    UtilityScore01 = utility01,

                    IsPinned = (pinIfOwnedByNpc && ownerKind == OwnerKind.Npc && ownerId == npcIdForPinLogic),

                    Flags = flags,
                    CarriedFoodUnitsApprox = carriedFoodUnitsApprox
                };
                return;
            }

            // 3) store pieno: rimpiazza il peggiore NON pinned
            int worstIdx = -1;
            float worstScore = float.MinValue;

            for (int i = 0; i < Slots.Length; i++)
            {
                var e = Slots[i];
                if (!e.IsValid) continue;
                if (e.IsPinned) continue; // mai rimpiazzare pinned

                // Metrica semplice: più vecchio e meno utile = peggio.
                // Nota: usiamo una score "alto=peggio" (più comodo con worstScore=MinValue).
                int age = nowTick - e.LastSeenTick;
                float score = (age * 0.01f) + (1f - e.UtilityScore01) + (1f - e.Reliability01);

                if (score <= worstScore) continue;
                worstScore = score;
                worstIdx = i;
            }

            if (worstIdx < 0)
                return; // tutti pinned => non inseriamo

            Slots[worstIdx] = new Entry
            {
                IsValid = true,

                Kind = kind,
                SubjectId = subjectId,

                DefId = defId,
                ObjectId = objectId,

                CellX = x,
                CellY = y,

                OwnerKind = ownerKind,
                OwnerId = ownerId,

                LastSeenTick = nowTick,
                Reliability01 = reliability01,
                UtilityScore01 = utility01,

                IsPinned = (pinIfOwnedByNpc && ownerKind == OwnerKind.Npc && ownerId == npcIdForPinLogic),

                Flags = flags,
                CarriedFoodUnitsApprox = carriedFoodUnitsApprox
            };
        }

        /// <summary>
        /// Cleanup:
        /// rimuove entries non-pinned troppo vecchie o inutili.
        ///
        /// Politica v0:
        /// - non rimuove mai IsPinned (oggetti posseduti dall'NPC)
        /// - rimuove entries non-pinned troppo vecchie (maxAgeTicks)
        ///
        /// Nota:
        /// - Per gli NPC osservati (Kind=Npc) NON pinniamo mai.
        /// - Quindi, se non li vedi da un po', vengono rimossi.
        /// </summary>
        public void Cleanup(int nowTick, int maxAgeTicks)
        {
            if (maxAgeTicks < 1) return;

            for (int i = 0; i < Slots.Length; i++)
            {
                var e = Slots[i];
                if (!e.IsValid) continue;
                if (e.IsPinned) continue;

                int age = nowTick - e.LastSeenTick;
                if (age > maxAgeTicks)
                    Slots[i].IsValid = false;
            }
        }
    }
}
