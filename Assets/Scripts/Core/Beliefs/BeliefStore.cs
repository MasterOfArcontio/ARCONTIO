using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // BeliefStore
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contenitore passivo per-NPC delle credenze soggettive normalizzate. Conserva
    /// <c>BeliefEntry</c>, assegna identificatori locali e offre filtri banali per
    /// categoria e status.
    /// </para>
    ///
    /// <para><b>BeliefStore passivo</b></para>
    /// <para>
    /// Questo store non esegue ranking, scoring, ricerca del migliore candidato o
    /// valutazione decisionale. Il metodo di filtro per categoria/status è volutamente
    /// semplice: restituisce solo le entry che corrispondono ai criteri ricevuti. Le
    /// query complesse appartengono al futuro QuerySystem.
    /// </para>
    ///
    /// <para><b>Cap e pruning conservativo</b></para>
    /// <para>
    /// Il cap evita crescita infinita come già accade per MemoryStore. Il pruning qui
    /// è minimo e non cognitivo: rimuove prima credenze <c>Discarded</c>; se non ce ne
    /// sono, elimina la credenza con prodotto <c>Confidence * Freshness</c> più basso.
    /// Una policy più ricca per categoria rimane rimandata agli step di decay/pruning.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Entries</b>: lista bounded delle credenze del singolo NPC.</item>
    ///   <item><b>NextBeliefId</b>: contatore locale per-NPC, non globale.</item>
    ///   <item><b>MaxEntries</b>: limite iniziale dello store, default conservativo 64.</item>
    ///   <item><b>Query banali</b>: filtri per categoria/status senza ordinamento né scoring.</item>
    /// </list>
    /// </summary>
    public sealed class BeliefStore
    {
        public const int DefaultMaxEntries = 64;

        private readonly List<BeliefEntry> _entries;
        private int _nextBeliefId = 1;

        public int MaxEntries { get; set; }

        public IReadOnlyList<BeliefEntry> Entries => _entries;

        // =============================================================================
        // NextBeliefId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Espone in sola lettura il prossimo identificativo locale che verra'
        /// assegnato a una nuova credenza del singolo NPC.
        /// </para>
        ///
        /// <para><b>Principio architetturale: authority locale del BeliefStore</b></para>
        /// <para>
        /// Il contatore non e' globale e non autorizza alcuna query sul mondo:
        /// serve solo a preservare continuita' interna dello store dopo un load
        /// canonico. La mutazione resta limitata alle API save/load dedicate.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Getter</b>: restituisce il campo privato <c>_nextBeliefId</c>.</item>
        /// </list>
        /// </summary>
        public int NextBeliefId => _nextBeliefId;

        public BeliefStore(int maxEntries = DefaultMaxEntries)
        {
            MaxEntries = maxEntries > 0 ? maxEntries : DefaultMaxEntries;
            _entries = new List<BeliefEntry>(MaxEntries);
        }

        // =============================================================================
        // TryReplaceAllForSaveLoad
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sostituisce integralmente il contenuto dello store durante un restore
        /// canonico da snapshot, preservando <c>BeliefId</c> e il prossimo ID
        /// locale salvato.
        /// </para>
        ///
        /// <para><b>Principio architetturale: restore cognitivo senza rebuild speculativo</b></para>
        /// <para>
        /// Questa API appartiene solo alla save/load authority. Non ricostruisce
        /// belief da MemoryTrace, non interroga il World e non applica ranking:
        /// ripristina esattamente credenze soggettive gia' aggregate in passato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Validazione</b>: rifiuta null, duplicati, ID invalidi e valori fuori range.</item>
        ///   <item><b>Cap</b>: ripristina <c>MaxEntries</c> e rifiuta snapshot oltre cap.</item>
        ///   <item><b>Counter</b>: impone <c>nextBeliefId</c> solo se supera tutti gli ID presenti.</item>
        /// </list>
        /// </summary>
        public bool TryReplaceAllForSaveLoad(
            IReadOnlyList<BeliefEntry> entries,
            int maxEntries,
            int nextBeliefId,
            out string error)
        {
            if (entries == null)
            {
                error = "BeliefStore.TryReplaceAllForSaveLoad: entries nullo.";
                return false;
            }

            if (maxEntries <= 0)
            {
                error = "BeliefStore.TryReplaceAllForSaveLoad: maxEntries deve essere > 0.";
                return false;
            }

            if (entries.Count > maxEntries)
            {
                error = $"BeliefStore.TryReplaceAllForSaveLoad: entries={entries.Count} supera maxEntries={maxEntries}.";
                return false;
            }

            int maxBeliefId = 0;
            var seenIds = new HashSet<int>();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry.BeliefId <= 0)
                {
                    error = $"BeliefStore.TryReplaceAllForSaveLoad: BeliefId invalido {entry.BeliefId}.";
                    return false;
                }

                if (!seenIds.Add(entry.BeliefId))
                {
                    error = $"BeliefStore.TryReplaceAllForSaveLoad: BeliefId duplicato {entry.BeliefId}.";
                    return false;
                }

                if (!Enum.IsDefined(typeof(BeliefCategory), entry.Category))
                {
                    error = $"BeliefStore.TryReplaceAllForSaveLoad: categoria belief invalida {entry.Category}.";
                    return false;
                }

                if (!Enum.IsDefined(typeof(BeliefSource), entry.Source))
                {
                    error = $"BeliefStore.TryReplaceAllForSaveLoad: source belief invalida {entry.Source}.";
                    return false;
                }

                if (!Enum.IsDefined(typeof(BeliefStatus), entry.Status))
                {
                    error = $"BeliefStore.TryReplaceAllForSaveLoad: status belief invalido {entry.Status}.";
                    return false;
                }

                if (entry.Confidence < 0f || entry.Confidence > 1f || entry.Freshness < 0f || entry.Freshness > 1f)
                {
                    error = $"BeliefStore.TryReplaceAllForSaveLoad: confidence/freshness fuori range per BeliefId {entry.BeliefId}.";
                    return false;
                }

                if (entry.SourceCount < 0)
                {
                    error = $"BeliefStore.TryReplaceAllForSaveLoad: SourceCount negativo per BeliefId {entry.BeliefId}.";
                    return false;
                }

                if (entry.BeliefId > maxBeliefId)
                    maxBeliefId = entry.BeliefId;
            }

            if (nextBeliefId <= maxBeliefId)
            {
                error = $"BeliefStore.TryReplaceAllForSaveLoad: nextBeliefId={nextBeliefId} non supera maxBeliefId={maxBeliefId}.";
                return false;
            }

            MaxEntries = maxEntries;
            _entries.Clear();

            for (int i = 0; i < entries.Count; i++)
                _entries.Add(entries[i]);

            _nextBeliefId = nextBeliefId;
            error = string.Empty;
            return true;
        }

        // =============================================================================
        // AddOrMergeByCategoryAndPosition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea o aggiorna una credenza usando una chiave minimale composta da
        /// categoria e posizione stimata.
        /// </para>
        ///
        /// <para><b>Aggregazione minimale</b></para>
        /// <para>
        /// La sessione 15 non implementa ancora conflict resolution completa. Se una
        /// credenza con stessa categoria e stessa posizione esiste già, viene rinforzata
        /// con la confidence/freshness massima e con incremento del conteggio fonti.
        /// Se non esiste, viene creata una nuova entry con id locale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>existing match</b>: aggiorna confidence, freshness, source count, source e tick.</item>
        ///   <item><b>new entry</b>: assegna un nuovo <c>BeliefId</c> per-NPC.</item>
        ///   <item><b>cap</b>: se necessario libera spazio tramite pruning minimale.</item>
        /// </list>
        /// </summary>
        public void AddOrMergeByCategoryAndPosition(
            BeliefCategory category,
            Vector2Int estimatedPosition,
            float confidence,
            float freshness,
            int currentTick,
            BeliefSource source)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Category != category || entry.EstimatedPosition != estimatedPosition)
                    continue;

                if (confidence > entry.Confidence)
                    entry.Confidence = confidence;

                if (freshness > entry.Freshness)
                    entry.Freshness = freshness;

                entry.LastUpdatedTick = currentTick;
                entry.SourceCount += 1;
                entry.Source = source;
                entry.Status = BeliefStatus.Active;

                _entries[i] = entry;
                return;
            }

            EnsureCapacityForNewEntry();

            if (_entries.Count >= MaxEntries)
                return;

            _entries.Add(new BeliefEntry
            {
                BeliefId = _nextBeliefId++,
                Category = category,
                EstimatedPosition = estimatedPosition,
                Confidence = confidence,
                Freshness = freshness,
                LastUpdatedTick = currentTick,
                SourceCount = 1,
                Source = source,
                Status = BeliefStatus.Active
            });
        }

        // =============================================================================
        // GetByCategoryAndStatus
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia in <paramref name="output"/> tutte le credenze che corrispondono alla
        /// categoria e allo status richiesti.
        /// </para>
        ///
        /// <para><b>Query banale del BeliefStore</b></para>
        /// <para>
        /// Questo metodo non calcola un "migliore" risultato, non ordina e non applica
        /// pesi. Serve solo a esporre il contenuto dello store secondo le due chiavi
        /// passive previste dal documento: categoria e status.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>category</b>: categoria semantica cercata.</item>
        ///   <item><b>status</b>: status operativo cercato.</item>
        ///   <item><b>output</b>: lista riusabile fornita dal chiamante, svuotata prima del riempimento.</item>
        /// </list>
        /// </summary>
        public void GetByCategoryAndStatus(BeliefCategory category, BeliefStatus status, List<BeliefEntry> output)
        {
            if (output == null)
                return;

            output.Clear();

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Category == category && entry.Status == status)
                    output.Add(entry);
            }
        }

        // =============================================================================
        // TryDiscardBelief
        // =============================================================================
        /// <summary>
        /// <para>
        /// Marca come scartata una credenza identificata dal suo id locale.
        /// </para>
        ///
        /// <para><b>Invalidazione passiva</b></para>
        /// <para>
        /// Il metodo non stabilisce perche' la credenza sia falsa. Riceve gia' una
        /// decisione dal BeliefUpdater e applica solo la mutazione dati minima:
        /// confidence azzerata, freshness riportata a 1 per indicare una smentita
        /// recente e status <c>Discarded</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>beliefId</b>: identificatore locale per-NPC della entry.</item>
        ///   <item><b>currentTick</b>: tick della smentita operativa.</item>
        ///   <item><b>return</b>: true se una entry e' stata trovata e modificata.</item>
        /// </list>
        /// </summary>
        public bool TryDiscardBelief(int beliefId, int currentTick)
        {
            if (beliefId <= 0)
                return false;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].BeliefId != beliefId)
                    continue;

                var entry = _entries[i];
                MarkDiscarded(ref entry, currentTick);
                _entries[i] = entry;
                return true;
            }

            return false;
        }

        // =============================================================================
        // TryDiscardByCategoryAndPosition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Marca come scartata una credenza usando il fallback categoria + posizione
        /// stimata.
        /// </para>
        ///
        /// <para><b>Fallback MVP senza QuerySystem</b></para>
        /// <para>
        /// Il flusso provvisorio delle rule non porta ancora sempre con se' il
        /// <c>BeliefId</c> che ha guidato l'azione. Questo metodo permette allo step
        /// 17 di invalidare comunque la credenza corrispondente alla stessa categoria
        /// e cella, coerentemente con il merge minimale gia' usato dallo store.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>category</b>: dominio semantico della credenza da invalidare.</item>
        ///   <item><b>estimatedPosition</b>: cella soggettiva verificata dall'NPC.</item>
        ///   <item><b>currentTick</b>: tick della smentita operativa.</item>
        /// </list>
        /// </summary>
        public bool TryDiscardByCategoryAndPosition(BeliefCategory category, Vector2Int estimatedPosition, int currentTick)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Category != category || entry.EstimatedPosition != estimatedPosition)
                    continue;

                MarkDiscarded(ref entry, currentTick);
                _entries[i] = entry;
                return true;
            }

            return false;
        }

        // =============================================================================
        // TryReduceConfidence
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riduce la confidence di una credenza identificata dal suo id locale.
        /// </para>
        ///
        /// <para><b>Indebolimento operativo</b></para>
        /// <para>
        /// Un fallimento ambiguo non prova che il belief sia falso. Per questo la
        /// mutazione abbassa la confidence e imposta uno status operativo ricevuto
        /// dal BeliefUpdater, senza eliminare immediatamente la entry.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>beliefId</b>: identificatore locale per-NPC della entry.</item>
        ///   <item><b>penalty01</b>: riduzione normalizzata della confidence.</item>
        ///   <item><b>status</b>: nuovo status cognitivo da applicare.</item>
        /// </list>
        /// </summary>
        public bool TryReduceConfidence(int beliefId, float penalty01, int currentTick, BeliefStatus status)
        {
            if (beliefId <= 0)
                return false;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].BeliefId != beliefId)
                    continue;

                var entry = _entries[i];
                ReduceConfidence(ref entry, penalty01, currentTick, status);
                _entries[i] = entry;
                return true;
            }

            return false;
        }

        // =============================================================================
        // TryReduceConfidenceByCategoryAndPosition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riduce la confidence di una credenza tramite fallback categoria + posizione.
        /// </para>
        ///
        /// <para><b>Compatibilita' con le rule provvisorie</b></para>
        /// <para>
        /// Finche' Decision Layer e Job System non propagano il <c>BeliefId</c>, le
        /// rule possono produrre feedback usando la stessa chiave minima gia' usata
        /// per aggregare i belief: categoria e cella stimata.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>category</b>: dominio semantico della credenza da indebolire.</item>
        ///   <item><b>estimatedPosition</b>: cella associata al fallimento operativo.</item>
        ///   <item><b>status</b>: status assegnato dal BeliefUpdater.</item>
        /// </list>
        /// </summary>
        public bool TryReduceConfidenceByCategoryAndPosition(
            BeliefCategory category,
            Vector2Int estimatedPosition,
            float penalty01,
            int currentTick,
            BeliefStatus status)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Category != category || entry.EstimatedPosition != estimatedPosition)
                    continue;

                ReduceConfidence(ref entry, penalty01, currentTick, status);
                _entries[i] = entry;
                return true;
            }

            return false;
        }

        // =============================================================================
        // TickDecay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica un tick di decadimento passivo alle credenze contenute nello store.
        /// </para>
        ///
        /// <para><b>Decay meccanico non decisionale</b></para>
        /// <para>
        /// Il metodo non sceglie target, non ordina credenze e non interroga il mondo.
        /// Aggiorna soltanto <c>Confidence</c>, <c>Freshness</c> e <c>Status</c> in base
        /// alla categoria della credenza e alle soglie configurate.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Confidence</b>: decade secondo il rate della categoria.</item>
        ///   <item><b>Freshness</b>: decade piu rapidamente tramite moltiplicatore dedicato.</item>
        ///   <item><b>Status</b>: passa a <c>Weak</c> sotto soglia o <c>Stale</c> se freshness e troppo bassa.</item>
        ///   <item><b>Rimozione</b>: elimina entry con confidence arrivata alla soglia minima di rimozione.</item>
        /// </list>
        /// </summary>
        public int TickDecay(BeliefDecayConfig config, float tickScale, out int updated, out int weak, out int stale)
        {
            updated = 0;
            weak = 0;
            stale = 0;

            if (_entries.Count == 0)
                return 0;

            if (tickScale <= 0f)
                tickScale = 1f;

            int removed = 0;
            float freshnessMultiplier = config.freshnessDecayMultiplier > 0f ? config.freshnessDecayMultiplier : 2f;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                if (entry.Status == BeliefStatus.Discarded)
                {
                    _entries.RemoveAt(i);
                    removed++;
                    continue;
                }

                float confidenceDecay = config.GetConfidenceDecayFor(entry.Category) * tickScale;
                float freshnessDecay = confidenceDecay * freshnessMultiplier;

                entry.Confidence = Clamp01(entry.Confidence - confidenceDecay);
                entry.Freshness = Clamp01(entry.Freshness - freshnessDecay);
                updated++;

                if (entry.Confidence <= config.removeConfidenceThreshold)
                {
                    _entries.RemoveAt(i);
                    removed++;
                    continue;
                }

                if (entry.Freshness <= config.staleFreshnessThreshold)
                {
                    entry.Status = BeliefStatus.Stale;
                    stale++;
                }
                else if (entry.Confidence <= config.weakConfidenceThreshold)
                {
                    entry.Status = BeliefStatus.Weak;
                    weak++;
                }
                else if (entry.Status != BeliefStatus.Conflicted)
                {
                    entry.Status = BeliefStatus.Active;
                }

                _entries[i] = entry;
            }

            return removed;
        }

        // =============================================================================
        // EnsureCapacityForNewEntry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Libera uno slot se lo store ha raggiunto il cap locale.
        /// </para>
        ///
        /// <para><b>Pruning non decisionale</b></para>
        /// <para>
        /// La rimozione qui non decide cosa sia utile per un'intenzione. È solo una
        /// protezione di memoria. Il comportamento preferisce rimuovere credenze già
        /// <c>Discarded</c>; in assenza di entry scartate elimina quella meno forte
        /// secondo un indicatore tecnico semplice.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Discarded first</b>: rimuove la prima entry già invalidata.</item>
        ///   <item><b>Weakest fallback</b>: rimuove la entry con <c>Confidence * Freshness</c> più basso.</item>
        /// </list>
        /// </summary>
        private void EnsureCapacityForNewEntry()
        {
            if (_entries.Count < MaxEntries)
                return;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Status == BeliefStatus.Discarded)
                {
                    _entries.RemoveAt(i);
                    return;
                }
            }

            int weakestIndex = -1;
            float weakestScore = float.MaxValue;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                float score = entry.Confidence * entry.Freshness;
                if (score < weakestScore)
                {
                    weakestScore = score;
                    weakestIndex = i;
                }
            }

            if (weakestIndex >= 0)
                _entries.RemoveAt(weakestIndex);
        }

        // =============================================================================
        // MarkDiscarded
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la mutazione dati comune per una credenza definitivamente smentita.
        /// </para>
        ///
        /// <para><b>Mutazione locale senza ragionamento</b></para>
        /// <para>
        /// La funzione e' privata per mantenere nel BeliefStore solo operazioni
        /// meccaniche. La scelta di usarla resta nel BeliefUpdater, che conosce il
        /// tipo di feedback operativo ricevuto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Confidence</b>: portata a zero per impedire riuso operativo.</item>
        ///   <item><b>Freshness</b>: portata a uno per indicare che la smentita e' recente.</item>
        ///   <item><b>Status</b>: impostato a <c>Discarded</c>.</item>
        /// </list>
        /// </summary>
        private static void MarkDiscarded(ref BeliefEntry entry, int currentTick)
        {
            entry.Confidence = 0f;
            entry.Freshness = 1f;
            entry.LastUpdatedTick = currentTick;
            entry.Status = BeliefStatus.Discarded;
        }

        // =============================================================================
        // ReduceConfidence
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la mutazione dati comune per una credenza indebolita ma non
        /// definitivamente smentita.
        /// </para>
        ///
        /// <para><b>Fallimento non conclusivo</b></para>
        /// <para>
        /// Il metodo conserva la entry per permettere al futuro QuerySystem di
        /// considerarla con peso minore o come credenza conflittuale. Non interroga
        /// il mondo e non sceglie alternative.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Penalty</b>: valore normalizzato prima della sottrazione.</item>
        ///   <item><b>Freshness</b>: aggiornata a uno per registrare feedback recente.</item>
        ///   <item><b>Status</b>: assegnato dal BeliefUpdater in base al tipo di smentita.</item>
        /// </list>
        /// </summary>
        private static void ReduceConfidence(ref BeliefEntry entry, float penalty01, int currentTick, BeliefStatus status)
        {
            entry.Confidence = Clamp01(entry.Confidence - Clamp01(penalty01));
            entry.Freshness = 1f;
            entry.LastUpdatedTick = currentTick;
            entry.Status = status;
        }

        // =============================================================================
        // Clamp01
        // =============================================================================
        /// <summary>
        /// <para>
        /// Limita un valore float all'intervallo normalizzato 0-1 usato dai belief.
        /// </para>
        ///
        /// <para><b>Protezione numerica locale</b></para>
        /// <para>
        /// Il decay sottrae valori progressivi da <c>Confidence</c> e <c>Freshness</c>.
        /// Il clamp impedisce risultati negativi o superiori a 1 quando i valori
        /// arrivano da merge, config o salvataggi vecchi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>value</b>: valore da normalizzare.</item>
        ///   <item><b>return</b>: valore compreso tra 0 e 1.</item>
        /// </list>
        /// </summary>
        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
