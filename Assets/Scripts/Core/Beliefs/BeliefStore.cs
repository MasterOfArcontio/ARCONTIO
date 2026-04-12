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

        public BeliefStore(int maxEntries = DefaultMaxEntries)
        {
            MaxEntries = maxEntries > 0 ? maxEntries : DefaultMaxEntries;
            _entries = new List<BeliefEntry>(MaxEntries);
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
    }
}
