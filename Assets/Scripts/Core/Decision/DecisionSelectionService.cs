using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // DecisionSelectionConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione della Fase 3: selezione weighted random tra i candidati migliori.
    /// </para>
    ///
    /// <para><b>Varianza controllata</b></para>
    /// <para>
    /// Il Decision Layer non deve scegliere sempre il massimo deterministico, ma la
    /// varianza deve essere limitata. Il top-N restringe il campo, il noise regola
    /// quanto candidati vicini possano superarsi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>topN</b>: numero massimo di candidati considerati.</item>
    ///   <item><b>noise01</b>: rumore normalizzato di selezione.</item>
    ///   <item><b>minimumWeight</b>: peso minimo per evitare liste a peso zero.</item>
    /// </list>
    /// </summary>
    public struct DecisionSelectionConfig
    {
        public int topN;
        public float noise01;
        public float minimumWeight;

        public static DecisionSelectionConfig Default()
        {
            return new DecisionSelectionConfig
            {
                topN = 3,
                noise01 = 0.15f,
                minimumWeight = 0.001f
            };
        }
    }

    // =============================================================================
    // DecisionSelectionResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato della selezione finale dell'intenzione.
    /// </para>
    ///
    /// <para><b>Output separato dal candidato</b></para>
    /// <para>
    /// La selezione restituisce il candidato vincitore e l'indice nella lista di
    /// input, senza mutare il World e senza produrre Command o Job.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsEmpty</b>: true se nessun candidato disponibile esiste.</item>
    ///   <item><b>SelectedIndex</b>: indice del candidato nella lista originale.</item>
    ///   <item><b>Candidate</b>: snapshot del candidato scelto.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionSelectionResult
    {
        public readonly bool IsEmpty;
        public readonly int SelectedIndex;
        public readonly DecisionCandidate Candidate;

        public DecisionSelectionResult(bool isEmpty, int selectedIndex, DecisionCandidate candidate)
        {
            IsEmpty = isEmpty;
            SelectedIndex = selectedIndex;
            Candidate = candidate;
        }

        public static DecisionSelectionResult Empty()
        {
            return new DecisionSelectionResult(true, -1, default);
        }
    }

    // =============================================================================
    // DecisionSelectionService
    // =============================================================================
    /// <summary>
    /// <para>
    /// Servizio della Fase 3 che seleziona una intenzione tra i candidati gia'
    /// filtrati e score-ati.
    /// </para>
    ///
    /// <para><b>Weighted random top-N</b></para>
    /// <para>
    /// Il servizio ordina i candidati disponibili, considera solo i migliori N e
    /// sceglie con probabilita' proporzionale allo score corretto da un piccolo
    /// rumore. La casualita' e' iniettata per rendere testabile il comportamento.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Rank buffer</b>: indici dei candidati ordinati per score.</item>
    ///   <item><b>Weight pass</b>: calcola il peso positivo di ciascun candidato top-N.</item>
    ///   <item><b>Roll</b>: usa <c>System.Random</c> iniettato dal chiamante.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionSelectionService
    {
        private readonly List<int> _rankedIndexes = new(16);

        // =============================================================================
        // Select
        // =============================================================================
        /// <summary>
        /// <para>
        /// Seleziona un candidato disponibile usando weighted random sul top-N.
        /// </para>
        ///
        /// <para><b>Selezione senza side effect</b></para>
        /// <para>
        /// Il metodo non scrive nel mondo e non modifica i candidati. Restituisce una
        /// snapshot della scelta, lasciando al chiamante la traduzione in intenzione
        /// attiva, Job o Command.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Rank</b>: ordina indici, non copia candidati.</item>
        ///   <item><b>Top-N</b>: limita la varianza ai migliori candidati.</item>
        ///   <item><b>Fallback</b>: se i pesi degenerano, sceglie il miglior candidato.</item>
        /// </list>
        /// </summary>
        public DecisionSelectionResult Select(
            List<DecisionCandidate> candidates,
            DecisionSelectionConfig config,
            Random random)
        {
            if (candidates == null || candidates.Count == 0)
                return DecisionSelectionResult.Empty();

            random ??= new Random(0);
            RankAvailableCandidates(candidates);

            if (_rankedIndexes.Count == 0)
                return DecisionSelectionResult.Empty();

            int topCount = config.topN > 0 ? Math.Min(config.topN, _rankedIndexes.Count) : _rankedIndexes.Count;
            float totalWeight = 0f;

            for (int i = 0; i < topCount; i++)
                totalWeight += GetSelectionWeight(candidates[_rankedIndexes[i]], config);

            if (totalWeight <= 0f)
            {
                int bestIndex = _rankedIndexes[0];
                return new DecisionSelectionResult(false, bestIndex, candidates[bestIndex]);
            }

            double roll = random.NextDouble() * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < topCount; i++)
            {
                int candidateIndex = _rankedIndexes[i];
                cumulative += GetSelectionWeight(candidates[candidateIndex], config);
                if (roll <= cumulative)
                    return new DecisionSelectionResult(false, candidateIndex, candidates[candidateIndex]);
            }

            int fallbackIndex = _rankedIndexes[0];
            return new DecisionSelectionResult(false, fallbackIndex, candidates[fallbackIndex]);
        }

        // =============================================================================
        // RankAvailableCandidates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il buffer degli indici disponibili ordinati per score
        /// decrescente.
        /// </para>
        ///
        /// <para><b>Ordinamento senza copiare candidati</b></para>
        /// <para>
        /// Il servizio conserva solo indici verso la lista originale. In questo modo
        /// la selezione finale puo' restituire l'indice sorgente senza duplicare
        /// strutture o perdere il legame con il chiamante.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Clear</b>: resetta il buffer riusabile.</item>
        ///   <item><b>Collect</b>: aggiunge solo candidati disponibili.</item>
        ///   <item><b>Sort</b>: ordina per <c>FinalScore</c> decrescente.</item>
        /// </list>
        /// </summary>
        private void RankAvailableCandidates(List<DecisionCandidate> candidates)
        {
            _rankedIndexes.Clear();

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].IsAvailable)
                    _rankedIndexes.Add(i);
            }

            _rankedIndexes.Sort((a, b) => candidates[b].FinalScore.CompareTo(candidates[a].FinalScore));
        }

        // =============================================================================
        // GetSelectionWeight
        // =============================================================================
        /// <summary>
        /// <para>
        /// Trasforma lo score finale di un candidato in peso positivo per la roulette
        /// weighted random.
        /// </para>
        ///
        /// <para><b>Noise come appiattimento probabilistico</b></para>
        /// <para>
        /// Il noise non riscrive lo score: aggiunge una quota comune che rende meno
        /// deterministico il top-N senza cancellare il vantaggio dei candidati migliori.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Minimum</b>: evita pesi nulli o negativi.</item>
        ///   <item><b>ScoreWeight</b>: parte positiva dello score finale.</item>
        ///   <item><b>Noise</b>: quota comune normalizzata 0-1.</item>
        /// </list>
        /// </summary>
        private static float GetSelectionWeight(DecisionCandidate candidate, DecisionSelectionConfig config)
        {
            float minimum = config.minimumWeight > 0f ? config.minimumWeight : 0.001f;
            float noise = Clamp01(config.noise01);

            // Il rumore qui non cambia lo score registrato: appiattisce solo i pesi
            // verso una quota comune, lasciando leggibile il breakdown della Fase 2.
            float scoreWeight = Math.Max(0f, candidate.FinalScore);
            return minimum + (scoreWeight * (1f - noise)) + noise;
        }

        // =============================================================================
        // Clamp01
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza un valore nel range 0-1 usato dai parametri di rumore.
        /// </para>
        ///
        /// <para><b>Validazione locale</b></para>
        /// <para>
        /// La funzione resta privata perche' serve solo a rendere robusto il calcolo
        /// del peso di selezione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Lower bound</b>: valori negativi diventano 0.</item>
        ///   <item><b>Upper bound</b>: valori sopra 1 diventano 1.</item>
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
