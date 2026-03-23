using System.Collections.Generic;

namespace Arcontio.Core
{
    /// <summary>
    /// AddOrMergeResult:
    /// Esito di MemoryStore.AddOrMerge(...) per telemetria e debug.
    /// </summary>
    public enum AddOrMergeResult
    {
        Inserted = 0,   // nuova traccia aggiunta
        Reinforced = 1, // merge su traccia esistente (rinforzo)
        Replaced = 2,   // store pieno: rimpiazza la peggiore
        Dropped = 3     // store pieno: incoming troppo debole, scartata
    }

    /// <summary>
    /// MemoryStore: contenitore di tracce per un NPC.
    ///
    /// Giorno 2:
    /// - Conservare tracce
    /// - Fondere tracce "simili" (AddOrMerge)
    /// - Applicare decadimento per tick (TickDecay)
    ///
    /// Giorno 3: aggiungiamo
    /// - Cap massimo tracce: evita crescita infinita
    /// - Pruning deterministico: se pieno, rimuove le tracce meno importanti
    /// - Helper per leggere le top-N tracce
    /// </summary>
    public sealed class MemoryStore
    {
        // Capacita massima per NPC: scelta conservativa.
        // Se in futuro vuoi piu dettaglio, si alza.
        public int MaxTraces { get; set; } = 32;

        private readonly List<MemoryTrace> _traces = new(16);

        public IReadOnlyList<MemoryTrace> Traces => _traces;

        /// <summary>
        /// Aggiunge o fonde una traccia equivalente.
        ///
        /// Se lo store e pieno e la traccia e "debole",
        /// potrebbe essere scartata.
        /// </summary>
        public AddOrMergeResult AddOrMerge(in MemoryTrace incoming)
        {
            // 1) Prova merge con una traccia equivalente
            //
            // IMPORTANTISSIMO (Patch 0.01P1):
            // In v0.01 la condizione di equivalenza includeva SEMPRE la cella (CellX/CellY).
            // Questo e corretto per eventi "statici" (es: un attacco avvenuto in un punto).
            //
            // Tuttavia e SBAGLIATO per le tracce che rappresentano una "conoscenza di un'entita"
            // il cui stato puo cambiare nel tempo (es: un NPC osservato che si muove).
            //
            // Conseguenza del bug:
            // - ogni volta che l'osservatore rivede lo stesso NPC in una cella diversa,
            //   la traccia NON mergea e viene inserita come nuova;
            // - si generano "tracce fantasma" (posizioni precedenti) che sembrano ancora valide.
            //
            // Fix:
            // - per alcuni MemoryType (attualmente: NpcSpotted) l'identita della traccia e
            //   (Type + SubjectId + metadati di fonte), e NON la cella.
            // - in caso di merge, aggiorniamo la cella alla piu recente (incoming).
            for (int i = 0; i < _traces.Count; i++)
            {
                var t = _traces[i];

                // ------------------------------------------------------------
                // Merge "entity-centric" (Patch 0.01P1)
                // ------------------------------------------------------------
                // Caso d'uso:
                // - MemoryType.NpcSpotted = "so dov'e quell'NPC (ultima posizione nota)".
                //   La cella deve quindi essere aggiornata e non deve frammentare la memoria.
                //
                // Nota su IsHeard/SourceSpeakerId:
                // - Se la traccia e stata "sentita" (token/rumor), vogliamo poter tenere separate
                //   le versioni raccontate da speaker diversi (potrebbero essere incoerenti).
                // - Quindi: per il merge, manteniamo l'uguaglianza su (IsHeard, HeardKind, SourceSpeakerId)
                //   ma ignoriamo CellX/CellY.
                if (t.Type == incoming.Type &&
                    t.Type == MemoryType.NpcSpotted &&
                    t.SubjectId == incoming.SubjectId &&
                    t.SecondarySubjectId == incoming.SecondarySubjectId &&
                    t.IsHeard == incoming.IsHeard &&
                    t.HeardKind == incoming.HeardKind &&
                    t.SourceSpeakerId == incoming.SourceSpeakerId)
                {
                    // Anti-fantasma: la traccia resta UNA e aggiorna le coordinate.
                    t.CellX = incoming.CellX;
                    t.CellY = incoming.CellY;

                    // Merge deterministico (stesso schema della versione originale).
                    float mergedIntensity = (t.Intensity01 > incoming.Intensity01) ? t.Intensity01 : incoming.Intensity01;

                    // Rinforzo: una "nuova occorrenza" rende la memoria piu viva
                    mergedIntensity += 0.05f;
                    if (mergedIntensity > 1f) mergedIntensity = 1f;

                    t.Intensity01 = mergedIntensity;

                    // Affidabilita: prendi la migliore
                    t.Reliability01 = (t.Reliability01 > incoming.Reliability01) ? t.Reliability01 : incoming.Reliability01;

                    // Decay: scegli il piu lento (min) => mantiene piu a lungo
                    t.DecayPerTick01 = (t.DecayPerTick01 < incoming.DecayPerTick01) ? t.DecayPerTick01 : incoming.DecayPerTick01;

                    _traces[i] = t;
                    return AddOrMergeResult.Reinforced;
                }

                // ------------------------------------------------------------
                // Merge "cell-centric" (comportamento originale)
                // ------------------------------------------------------------
                if (t.Type == incoming.Type &&
                    t.SubjectId == incoming.SubjectId &&
                    t.SecondarySubjectId == incoming.SecondarySubjectId &&
                    t.CellX == incoming.CellX &&
                    t.CellY == incoming.CellY &&
                    t.IsHeard == incoming.IsHeard &&
                    t.HeardKind == incoming.HeardKind &&
                    t.SourceSpeakerId == incoming.SourceSpeakerId)
                {
                    // Merge deterministico
                    float mergedIntensity = (t.Intensity01 > incoming.Intensity01) ? t.Intensity01 : incoming.Intensity01;

                    // Rinforzo: una "nuova occorrenza" rende la memoria piu viva
                    mergedIntensity += 0.05f;
                    if (mergedIntensity > 1f) mergedIntensity = 1f;

                    t.Intensity01 = mergedIntensity;

                    // Affidabilita: prendi la migliore
                    t.Reliability01 = (t.Reliability01 > incoming.Reliability01) ? t.Reliability01 : incoming.Reliability01;

                    // Decay: scegli il piu lento (min) => mantiene piu a lungo
                    t.DecayPerTick01 = (t.DecayPerTick01 < incoming.DecayPerTick01) ? t.DecayPerTick01 : incoming.DecayPerTick01;

                    _traces[i] = t;
                    return AddOrMergeResult.Reinforced;
                }
            }

            // 2) Se non c'e merge e lo store e pieno, decidiamo se scartare o sostituire.
            if (_traces.Count >= MaxTraces)
            {
                int worstIndex = -1;
                float worstImportance = float.MaxValue;

                for (int i = 0; i < _traces.Count; i++)
                {
                    var t = _traces[i];
                    float importance = t.Intensity01 * t.Reliability01;

                    if (importance < worstImportance)
                    {
                        worstImportance = importance;
                        worstIndex = i;
                    }
                }

                float incomingImportance = incoming.Intensity01 * incoming.Reliability01;

                if (incomingImportance <= worstImportance)
                    return AddOrMergeResult.Dropped;

                _traces[worstIndex] = incoming;
                return AddOrMergeResult.Replaced;
            }

            // 3) Se c'e spazio, aggiungiamo
            _traces.Add(incoming);
            return AddOrMergeResult.Inserted;
        }

        /// <summary>
        /// Applica decadimento alle tracce.
        ///
        /// tickScale: tipicamente tick.DeltaTime (tempo simulato per tick)
        /// decayMultiplier: modulatore globale/individuale (Giorno 3: viene dai tratti NPC)
        /// </summary>
        public int TickDecay(float tickScale, float decayMultiplier)
        {
            if (_traces.Count == 0) return 0;

            int before = _traces.Count;

            // Compact in-place: manteniamo le tracce vive in testa.
            int write = 0;

            for (int read = 0; read < _traces.Count; read++)
            {
                var t = _traces[read];

                float decay = t.DecayPerTick01 * tickScale * decayMultiplier;
                t.Intensity01 -= decay;

                if (t.Intensity01 > 0f)
                {
                    _traces[write] = t;
                    write++;
                }
            }

            if (write < _traces.Count)
                _traces.RemoveRange(write, _traces.Count - write);

            return before - _traces.Count;
        }

        /// <summary>
        /// Ritorna fino a N tracce "piu importanti".
        ///
        /// Importanza: Intensity01 * Reliability01
        /// - Non alloca liste nuove: scrive su output (riusabile)
        /// - Ordina con selezione semplice (N piccolo)
        /// </summary>
        public void GetTopTraces(int maxCount, List<MemoryTrace> output)
        {
            output.Clear();
            if (maxCount <= 0) return;
            if (_traces.Count == 0) return;

            // Copia tutto in output (poi potiamo)
            for (int i = 0; i < _traces.Count; i++)
                output.Add(_traces[i]);

            // Selection sort parziale: porta davanti le piu importanti.
            int limit = (maxCount < output.Count) ? maxCount : output.Count;

            for (int i = 0; i < limit; i++)
            {
                int bestIndex = i;
                float bestScore = Score(output[i]);

                for (int j = i + 1; j < output.Count; j++)
                {
                    float s = Score(output[j]);
                    if (s > bestScore)
                    {
                        bestScore = s;
                        bestIndex = j;
                    }
                }

                if (bestIndex != i)
                {
                    var tmp = output[i];
                    output[i] = output[bestIndex];
                    output[bestIndex] = tmp;
                }
            }

            // Rimuovi oltre limit
            if (output.Count > limit)
                output.RemoveRange(limit, output.Count - limit);
        }

        private static float Score(in MemoryTrace t)
        {
            return t.Intensity01 * t.Reliability01;
        }
    }
}