using System.Collections.Generic;
using Arcontio.Core.Config;

namespace Arcontio.Core
{
    // =============================================================================
    // BeliefQueryService
    // =============================================================================
    /// <summary>
    /// <para>
    /// Servizio di query MVP che valuta credenze del BeliefStore tramite evaluator
    /// condivisi e restituisce un risultato spiegabile.
    /// </para>
    ///
    /// <para><b>Store + QueryService + Evaluators</b></para>
    /// <para>
    /// Il servizio e' il primo punto pensato per il Decision Layer: non legge
    /// <c>MemoryStore</c>, non interroga il world state oggettivo e non modifica i
    /// belief. Riceve uno store gia' soggettivo e lo valuta con una pipeline comune.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Evaluators</b>: confidence, freshness e distance per la fase MVP.</item>
    ///   <item><b>Candidate buffer</b>: lista riusabile per ridurre allocazioni intermedie.</item>
    ///   <item><b>ScoreContext</b>: contesto riusato per accumulare contributi.</item>
    /// </list>
    /// </summary>
    public sealed class BeliefQueryService
    {
        private readonly List<IBeliefEvaluator> _evaluators = new(4);
        private readonly List<BeliefEntry> _candidates = new(32);
        private readonly BeliefScoreContext _scoreContext = new();

        // =============================================================================
        // BeliefQueryService
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inizializza la pipeline MVP con gli evaluator condivisi previsti dalla
        /// sessione 18.
        /// </para>
        ///
        /// <para><b>Composizione esplicita della query</b></para>
        /// <para>
        /// La lista degli evaluator rimane dentro al servizio, non dentro al
        /// <c>BeliefStore</c>, cosi lo store resta una struttura dati passiva e il
        /// Decision Layer puo' dipendere da un punto di query dedicato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Confidence</b>: valorizza la sicurezza soggettiva del belief.</item>
        ///   <item><b>Freshness</b>: valorizza informazioni ancora recenti.</item>
        ///   <item><b>Distance</b>: penalizza candidati lontani dalla posizione NPC.</item>
        /// </list>
        /// </summary>
        public BeliefQueryService()
        {
            _evaluators.Add(new ConfidenceBeliefEvaluator());
            _evaluators.Add(new FreshnessBeliefEvaluator());
            _evaluators.Add(new DistanceBeliefEvaluator());
        }

        // =============================================================================
        // HasAnyUsableBeliefFor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se esiste almeno una credenza utilizzabile per il contesto di
        /// query richiesto, senza calcolare ranking o breakdown.
        /// </para>
        ///
        /// <para><b>Query banale del BeliefStore</b></para>
        /// <para>
        /// Il documento consente query semplici e passive: questo metodo serve al
        /// Decision Layer per sapere se puo' ragionare su conoscenza soggettiva gia'
        /// disponibile oppure se deve generare un'intenzione di ricerca.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Null guard</b>: uno store assente equivale a nessuna credenza utile.</item>
        ///   <item><b>Scan lineare</b>: sufficiente per MVP, senza indici prematuri.</item>
        ///   <item><b>Filtro condiviso</b>: usa la stessa definizione di candidato della query completa.</item>
        /// </list>
        /// </summary>
        public bool HasAnyUsableBeliefFor(BeliefStore store, BeliefQueryContext query)
        {
            if (store == null)
                return false;

            var entries = store.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                // Il metodo non duplica la logica di filtro: una belief "utilizzabile"
                // deve significare la stessa cosa sia nella query rapida sia nel ranking.
                var belief = entries[i];
                if (IsUsableCandidate(belief, query))
                    return true;
            }

            return false;
        }

        // =============================================================================
        // GetBestKnownFoodSource
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una query di comodo per recuperare la migliore fonte di cibo
        /// conosciuta soggettivamente dall'NPC.
        /// </para>
        ///
        /// <para><b>Adapter per Decision Layer futuro</b></para>
        /// <para>
        /// Il metodo non introduce una regola decisionale: prepara solo un accesso
        /// leggibile che il Decision Layer potra' usare quando le regole dei Needs
        /// verranno migrate verso il QuerySystem.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>GoalType</b>: forza la categoria <c>Food</c>.</item>
        ///   <item><b>Urgency</b>: passa il livello di urgenza agli evaluator.</item>
        ///   <item><b>Min confidence</b>: usa la soglia configurata nel JSON.</item>
        /// </list>
        /// </summary>
        public BeliefQueryResult GetBestKnownFoodSource(
            BeliefStore store,
            UnityEngine.Vector2Int npcPosition,
            float urgency01,
            BeliefQueryConfig config)
        {
            var query = new BeliefQueryContext(
                BeliefCategory.Food,
                urgency01,
                npcPosition,
                config.defaultMinConfidence);

            return QueryBest(store, query, config);
        }

        // =============================================================================
        // GetBestKnownFoodSource
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una query di comodo per la migliore fonte di cibo conosciuta e,
        /// quando richiesto dal chiamante, registra anche il record EL della query.
        /// </para>
        ///
        /// <para><b>Explainability passiva del QuerySystem</b></para>
        /// <para>
        /// L'overload mantiene separata la diagnostica dalla logica decisionale: il
        /// risultato della query resta identico, mentre il sink JSONL riceve solo uno
        /// snapshot dei dati gia' attraversati dal servizio.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Query Food</b>: riusa la stessa costruzione del metodo base.</item>
        ///   <item><b>Config EL</b>: abilita o disabilita l'export senza cambiare i chiamanti esistenti.</item>
        ///   <item><b>Identita' runtime</b>: associa tick e NPC al record diagnostico.</item>
        /// </list>
        /// </summary>
        public BeliefQueryResult GetBestKnownFoodSource(
            BeliefStore store,
            UnityEngine.Vector2Int npcPosition,
            float urgency01,
            BeliefQueryConfig config,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            int npcId,
            long tick,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry = null)
        {
            var query = new BeliefQueryContext(
                BeliefCategory.Food,
                urgency01,
                npcPosition,
                config.defaultMinConfidence);

            return QueryBest(store, query, config, explainabilityConfig, npcId, tick, explainabilityRegistry);
        }

        // =============================================================================
        // GetBestKnownRestPlace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una query di comodo per recuperare il miglior luogo di riposo
        /// conosciuto soggettivamente dall'NPC.
        /// </para>
        ///
        /// <para><b>Adapter per Decision Layer futuro</b></para>
        /// <para>
        /// Come la variante per il cibo, questo metodo non decide un job e non legge
        /// il world state oggettivo: incapsula solo il contesto minimo necessario per
        /// interrogare il BeliefStore.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>GoalType</b>: forza la categoria <c>Rest</c>.</item>
        ///   <item><b>Urgency</b>: passa il livello di urgenza agli evaluator.</item>
        ///   <item><b>Min confidence</b>: usa la soglia configurata nel JSON.</item>
        /// </list>
        /// </summary>
        public BeliefQueryResult GetBestKnownRestPlace(
            BeliefStore store,
            UnityEngine.Vector2Int npcPosition,
            float urgency01,
            BeliefQueryConfig config)
        {
            var query = new BeliefQueryContext(
                BeliefCategory.Rest,
                urgency01,
                npcPosition,
                config.defaultMinConfidence);

            return QueryBest(store, query, config);
        }

        // =============================================================================
        // GetBestKnownRestPlace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una query di comodo per il miglior luogo di riposo conosciuto e,
        /// se la diagnostica e' attiva, esporta il record EL corrispondente.
        /// </para>
        ///
        /// <para><b>Adapter progressivo verso Decision Layer</b></para>
        /// <para>
        /// L'overload permette alle regole migrate di produrre una traccia leggibile
        /// senza esporre BeliefStore, MemoryStore o world state al sink JSONL.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Query Rest</b>: riusa il contesto standard per luoghi di riposo.</item>
        ///   <item><b>Snapshot</b>: la diagnostica riceve solo il risultato gia' calcolato.</item>
        ///   <item><b>Compatibilita'</b>: il metodo originale resta invariato.</item>
        /// </list>
        /// </summary>
        public BeliefQueryResult GetBestKnownRestPlace(
            BeliefStore store,
            UnityEngine.Vector2Int npcPosition,
            float urgency01,
            BeliefQueryConfig config,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            int npcId,
            long tick,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry = null)
        {
            var query = new BeliefQueryContext(
                BeliefCategory.Rest,
                urgency01,
                npcPosition,
                config.defaultMinConfidence);

            return QueryBest(store, query, config, explainabilityConfig, npcId, tick, explainabilityRegistry);
        }

        // =============================================================================
        // QueryBest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue la pipeline comune di filtro, scoring e selezione del miglior belief
        /// per una categoria obiettivo.
        /// </para>
        ///
        /// <para><b>QuerySystem MVP</b></para>
        /// <para>
        /// Il metodo legge solo il <c>BeliefStore</c> ricevuto, applica evaluator
        /// condivisi e restituisce un breakdown strutturato. Se non trova candidati,
        /// restituisce <c>BeliefQueryResult.Empty()</c> cosi il futuro Decision Layer
        /// potra' scegliere un'intenzione di ricerca.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Filter</b>: categoria, status e confidence minima.</item>
        ///   <item><b>Evaluate</b>: somma i contributi degli evaluator registrati.</item>
        ///   <item><b>Select</b>: mantiene il candidato con score piu' alto.</item>
        /// </list>
        /// </summary>
        public BeliefQueryResult QueryBest(BeliefStore store, BeliefQueryContext query, BeliefQueryConfig config)
        {
            return QueryBest(store, query, config, null, 0, 0, null);
        }

        // =============================================================================
        // QueryBest
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue la pipeline comune di filtro, scoring e selezione del miglior belief
        /// e, se configurato, registra uno snapshot EL della query.
        /// </para>
        ///
        /// <para><b>Diagnostica senza onniscienza</b></para>
        /// <para>
        /// Il record JSONL viene costruito usando solo lo store soggettivo gia'
        /// passato al servizio, il contesto di query e il risultato appena prodotto.
        /// Non vengono introdotte letture dirette del mondo, di MemoryStore o di
        /// singleton globali.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Null/empty tracing</b>: esporta anche query senza store o senza candidati.</item>
        ///   <item><b>Candidate count</b>: distingue entry della categoria e candidati realmente usabili.</item>
        ///   <item><b>Winner snapshot</b>: serializza belief e contributi senza trattenere riferimenti mutabili.</item>
        /// </list>
        /// </summary>
        public BeliefQueryResult QueryBest(
            BeliefStore store,
            BeliefQueryContext query,
            BeliefQueryConfig config,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            int npcId,
            long tick,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry = null)
        {
            if (store == null)
            {
                var emptyResult = BeliefQueryResult.Empty();
                TryEmitQueryTrace(explainabilityConfig, explainabilityRegistry, npcId, tick, query, emptyResult, 0, 0, "MissingBeliefStore");
                return BeliefQueryResult.Empty();
            }

            _candidates.Clear();

            var entries = store.Entries;
            int matchingCategoryCount = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                // Primo passaggio intenzionalmente semplice: raccogliamo solo credenze
                // soggettive coerenti con la richiesta, senza consultare dati oggettivi.
                var belief = entries[i];

                // Il conteggio di categoria e' diagnostico: aiuta a distinguere tra
                // "non conosco nulla di Food" e "conosco Food ma nessuna entry supera i gate".
                if (belief.Category == query.GoalType)
                    matchingCategoryCount++;

                if (IsUsableCandidate(belief, query))
                    _candidates.Add(belief);
            }

            if (_candidates.Count == 0)
            {
                var emptyResult = BeliefQueryResult.Empty();
                TryEmitQueryTrace(explainabilityConfig, explainabilityRegistry, npcId, tick, query, emptyResult, matchingCategoryCount, 0, "NoUsableBelief");
                return BeliefQueryResult.Empty();
            }

            bool hasBest = false;
            float bestScore = float.NegativeInfinity;
            BeliefEntry bestBelief = default;
            BeliefScoreContribution[] bestContributions = System.Array.Empty<BeliefScoreContribution>();

            for (int i = 0; i < _candidates.Count; i++)
            {
                var belief = _candidates[i];
                _scoreContext.Reset(belief, query);

                float score = 0f;
                for (int e = 0; e < _evaluators.Count; e++)
                {
                    // Ogni evaluator aggiunge un contributo nominato: il breakdown e'
                    // fondamentale per debug, explainability e futura tuning UI.
                    var evaluator = _evaluators[e];
                    float contribution = evaluator.Evaluate(_scoreContext, config);
                    score += contribution;
                    _scoreContext.Contributions.Add(new BeliefScoreContribution(evaluator.Label, contribution));
                }

                if (!hasBest || score > bestScore)
                {
                    // Copiamo i contributi solo quando il candidato diventa il migliore:
                    // il contesto viene riusato al ciclo successivo per limitare allocazioni.
                    hasBest = true;
                    bestScore = score;
                    bestBelief = belief;
                    bestContributions = _scoreContext.Contributions.ToArray();
                }
            }

            var result = hasBest
                ? new BeliefQueryResult(false, bestBelief, bestScore, bestContributions)
                : BeliefQueryResult.Empty();

            TryEmitQueryTrace(explainabilityConfig, explainabilityRegistry, npcId, tick, query, result, matchingCategoryCount, _candidates.Count, string.Empty);
            return result;
        }

        // =============================================================================
        // TryEmitQueryTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il risultato di una query belief in un record EL JSONL, lasciando
        /// al sink la decisione finale su abilitazione, filtri e scrittura fisica.
        /// </para>
        ///
        /// <para><b>Snapshot one-way</b></para>
        /// <para>
        /// La funzione non ricalcola la query e non legge altri sistemi: impacchetta
        /// soltanto contesto, conteggi, winner e breakdown gia' disponibili nello
        /// stack corrente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Guard config</b>: evita lavoro quando la diagnostica non e' stata richiesta.</item>
        ///   <item><b>Belief ref</b>: copia campi primitivi del winner selezionato.</item>
        ///   <item><b>Contributions</b>: preserva le etichette degli evaluator per analisi successive.</item>
        /// </list>
        /// </summary>
        private static void TryEmitQueryTrace(
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId,
            long tick,
            BeliefQueryContext query,
            BeliefQueryResult result,
            int candidateCount,
            int usableCandidateCount,
            string emptyReason)
        {
            if (explainabilityConfig == null)
                return;

            // La normalizzazione in strutture EL avviene qui, vicino al QuerySystem,
            // per evitare che il sink conosca BeliefQueryResult o logica di dominio.
            var trace = new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Query,
                Tick = tick,
                NpcId = npcId,
                Query = new MemoryBeliefDecisionQueryRecord
                {
                    GoalType = query.GoalType,
                    Urgency01 = query.Urgency01,
                    NpcPosition = query.NpcPosition,
                    MinConfidence = query.MinConfidence,
                    CandidateCount = candidateCount,
                    UsableCandidateCount = usableCandidateCount,
                    IsEmpty = result.IsEmpty,
                    EmptyReason = emptyReason ?? string.Empty,
                    Winner = result.IsEmpty ? default : ToBeliefRef(result.Belief),
                    FinalScore = result.FinalScore,
                    Contributions = ToContributionRefs(result.Contributions),
                },
            };

            MemoryBeliefDecisionExplainabilityEmitter.TryWriteTrace(explainabilityConfig, explainabilityRegistry, trace);
        }

        // =============================================================================
        // ToBeliefRef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una copia diagnostica minimale di una <c>BeliefEntry</c> selezionata.
        /// </para>
        ///
        /// <para><b>Isolamento dai dati mutabili</b></para>
        /// <para>
        /// Il record EL non trattiene la entry originale del BeliefStore: copia i soli
        /// campi necessari a capire quale credenza ha guidato la query.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Identita'</b>: id locale del belief e categoria.</item>
        ///   <item><b>Qualita'</b>: confidence, freshness, source count e status.</item>
        ///   <item><b>Target</b>: posizione stimata soggettiva.</item>
        /// </list>
        /// </summary>
        private static MemoryBeliefDecisionBeliefRef ToBeliefRef(BeliefEntry belief)
        {
            return new MemoryBeliefDecisionBeliefRef
            {
                Category = belief.Category,
                Status = belief.Status,
                Source = belief.Source,
                BeliefId = belief.BeliefId,
                EstimatedPosition = belief.EstimatedPosition,
                Confidence = belief.Confidence,
                Freshness = belief.Freshness,
                SourceCount = belief.SourceCount,
            };
        }

        // =============================================================================
        // ToContributionRefs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia il breakdown dello score in un array adatto al layer EL.
        /// </para>
        ///
        /// <para><b>Breakdown stabile</b></para>
        /// <para>
        /// Gli evaluator producono etichette e valori nel QuerySystem; questa funzione
        /// conserva quelle coppie senza esporre il contesto di scoring riusabile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Null/empty guard</b>: restituisce un array vuoto condivisibile.</item>
        ///   <item><b>Loop esplicito</b>: copia label e value uno a uno.</item>
        ///   <item><b>Output</b>: payload passivo serializzabile dal sink JSONL.</item>
        /// </list>
        /// </summary>
        private static MemoryBeliefDecisionScoreContributionRef[] ToContributionRefs(BeliefScoreContribution[] contributions)
        {
            if (contributions == null || contributions.Length == 0)
                return System.Array.Empty<MemoryBeliefDecisionScoreContributionRef>();

            var refs = new MemoryBeliefDecisionScoreContributionRef[contributions.Length];
            for (int i = 0; i < contributions.Length; i++)
            {
                refs[i] = new MemoryBeliefDecisionScoreContributionRef
                {
                    Label = contributions[i].Label,
                    Value = contributions[i].Value,
                };
            }

            return refs;
        }

        private static bool IsUsableCandidate(BeliefEntry belief, BeliefQueryContext query)
        {
            // La categoria e' il primo vincolo: una richiesta Food non deve mai usare
            // una belief Rest solo perche' magari ha score numerico migliore.
            if (belief.Category != query.GoalType)
                return false;

            // Weak resta interrogabile perche' puo' ancora orientare una decisione
            // prudente; Invalidated/Contradicted non devono invece guidare scelta job.
            if (belief.Status != BeliefStatus.Active && belief.Status != BeliefStatus.Weak)
                return false;

            // La soglia minima e' nel QueryContext per permettere regole future piu'
            // severe senza cambiare lo store o gli evaluator.
            return belief.Confidence >= query.MinConfidence;
        }
    }
}
