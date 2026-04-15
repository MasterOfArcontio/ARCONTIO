using System.Collections.Generic;
using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // IBeliefAggregationRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Regola di aggregazione che traduce una <c>MemoryTrace</c> in aggiornamenti
    /// del <c>BeliefStore</c> dell'NPC.
    /// </para>
    ///
    /// <para><b>Layer separato da Memory e Query</b></para>
    /// <para>
    /// La regola non decide intenzioni e non interroga il mondo. Riceve una traccia
    /// già soggettiva e aggiorna uno store di credenze. La valutazione dei belief
    /// rimane responsabilità del futuro QuerySystem.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Matches</b>: dichiara se la regola sa trattare la traccia.</item>
    ///   <item><b>Apply</b>: aggiorna o crea una entry nello store del singolo NPC.</item>
    /// </list>
    /// </summary>
    public interface IBeliefAggregationRule
    {
        bool Matches(MemoryTrace trace);
        void Apply(MemoryTrace trace, BeliefStore store, int currentTick);
    }

    // =============================================================================
    // BeliefUpdater
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coordinatore lazy che aggiorna il BeliefStore quando arriva una nuova
    /// <c>MemoryTrace</c> o una traccia viene rinforzata nel MemoryStore.
    /// </para>
    ///
    /// <para><b>Aggregazione lazy</b></para>
    /// <para>
    /// Questo updater non gira ogni tick e non applica decay. Viene chiamato dal
    /// punto in cui una trace entra nello stato soggettivo dell'NPC. Gli altri trigger
    /// documentati, come job fallito o query esplicita su belief vecchio, restano
    /// per gli step successivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Rule catalog</b>: insieme minimo di regole di aggregazione registrate nel costruttore.</item>
    ///   <item><b>UpdateFromTrace</b>: applica la prima regola compatibile alla trace ricevuta.</item>
    ///   <item><b>No world query</b>: lavora solo su trace e store, non su stato oggettivo globale.</item>
    /// </list>
    /// </summary>
    public sealed class BeliefUpdater
    {
        private readonly List<IBeliefAggregationRule> _rules = new();

        public BeliefUpdater()
        {
            _rules.Add(new DangerBeliefAggregationRule());
            _rules.Add(new ObjectBeliefAggregationRule());
            _rules.Add(new SocialBeliefAggregationRule());
        }

        // =============================================================================
        // UpdateFromTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica l'aggregazione lazy di una singola traccia nello store di credenze
        /// del relativo NPC.
        /// </para>
        ///
        /// <para><b>Single responsibility</b></para>
        /// <para>
        /// Il metodo non decide se la traccia dovesse entrare nel MemoryStore: quella
        /// decisione è già stata presa prima. Qui si limita a tradurre una traccia
        /// ammessa in una credenza sintetica, se esiste una regola compatibile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Trace</b>: memoria soggettiva appena inserita o rinforzata.</item>
        ///   <item><b>Store</b>: BeliefStore del singolo NPC.</item>
        ///   <item><b>CurrentTick</b>: tick usato come LastUpdatedTick della credenza.</item>
        /// </list>
        /// </summary>
        public bool UpdateFromTrace(in MemoryTrace trace, BeliefStore store, int currentTick)
        {
            return UpdateFromTrace(trace, store, currentTick, null, null, 0);
        }

        // =============================================================================
        // UpdateFromTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload di compatibilita' per i chiamanti e i test che conoscono solo la
        /// configurazione JSONL ma non il registry runtime UI-friendly.
        /// </para>
        ///
        /// <para><b>Compatibilita' progressiva</b></para>
        /// <para>
        /// La nuova pipeline puo' scrivere anche nel registry, ma i call site storici
        /// devono continuare a produrre la stessa diagnostica JSONL senza obbligare
        /// ogni test a costruire un World.
        /// </para>
        /// </summary>
        public bool UpdateFromTrace(
            in MemoryTrace trace,
            BeliefStore store,
            int currentTick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            int npcId)
        {
            return UpdateFromTrace(trace, store, currentTick, explainabilityConfig, null, npcId);
        }

        // =============================================================================
        // UpdateFromTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica l'aggregazione lazy di una trace e registra, quando richiesto, il
        /// record EL del belief risultante.
        /// </para>
        ///
        /// <para><b>BeliefUpdater come confine Memory -> Belief</b></para>
        /// <para>
        /// Questo overload non cambia la semantica dell'aggregazione: aggiunge solo
        /// uno snapshot diagnostico dopo che la regola compatibile ha aggiornato lo
        /// store. Il log non decide, non filtra e non consulta il mondo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Resolve category</b>: deduce la categoria dalla stessa trace usata dalla regola.</item>
        ///   <item><b>Existing check</b>: distingue creazione e merge prima dell'apply.</item>
        ///   <item><b>Emit</b>: copia il belief risultante solo se la mutazione e' avvenuta.</item>
        /// </list>
        /// </summary>
        public bool UpdateFromTrace(
            in MemoryTrace trace,
            BeliefStore store,
            int currentTick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int npcId)
        {
            if (store == null)
                return false;

            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (!rule.Matches(trace))
                    continue;

                bool hasCategory = TryResolveBeliefCategory(trace, out var category);
                var estimatedPosition = new Vector2Int(trace.CellX, trace.CellY);
                bool hadExisting = hasCategory && TryFindBelief(store, category, estimatedPosition, out _);

                rule.Apply(trace, store, currentTick);
                if (hasCategory && TryFindBelief(store, category, estimatedPosition, out var updatedBelief))
                {
                    // L'operazione e' diagnostica: AddOrMergeByCategoryAndPosition
                    // nasconde giustamente i dettagli interni, quindi qui distinguiamo
                    // solo tra prima creazione e merge su entry gia' esistente.
                    var operation = hadExisting
                        ? MemoryBeliefDecisionBeliefOperation.Merged
                        : MemoryBeliefDecisionBeliefOperation.Created;

                    MemoryBeliefDecisionExplainabilityEmitter.TryWriteBeliefTrace(
                        explainabilityConfig,
                        explainabilityRegistry,
                        npcId,
                        currentTick,
                        operation,
                        trace,
                        updatedBelief,
                        rule.GetType().Name);
                }

                return true;
            }

            return false;
        }

        // =============================================================================
        // TryResolveBeliefCategory
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la categoria belief prodotta da una trace usando la stessa mappatura
        /// minimale delle regole di aggregazione MVP.
        /// </para>
        ///
        /// <para><b>Diagnostica allineata all'aggregazione</b></para>
        /// <para>
        /// Il metodo serve solo a ritrovare il belief dopo l'apply per esportarlo nel
        /// log. Non introduce una seconda pipeline cognitiva e non legge database o
        /// stato oggettivo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Danger</b>: tracce di minaccia note alle rule.</item>
        ///   <item><b>Object</b>: classificazione conservativa da SubjectDefId.</item>
        ///   <item><b>Social</b>: NPC osservati come belief sociali minimi.</item>
        /// </list>
        /// </summary>
        private static bool TryResolveBeliefCategory(MemoryTrace trace, out BeliefCategory category)
        {
            if (trace.Type == MemoryType.PredatorSpotted
                || trace.Type == MemoryType.PredatorRumor
                || trace.Type == MemoryType.AttackSuffered
                || trace.Type == MemoryType.AttackWitnessed
                || trace.Type == MemoryType.NearDeathExperience
                || trace.Type == MemoryType.DeathWitnessed)
            {
                category = BeliefCategory.Danger;
                return true;
            }

            if (trace.Type == MemoryType.ObjectSpotted)
            {
                category = BeliefAggregationRuleCommon.ClassifyObjectDefId(trace.SubjectDefId);
                return true;
            }

            if (trace.Type == MemoryType.NpcSpotted)
            {
                category = BeliefCategory.Social;
                return true;
            }

            category = default;
            return false;
        }

        // =============================================================================
        // TryFindBelief
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca nello store la credenza corrispondente alla categoria e posizione
        /// prodotte dalla trace appena aggregata.
        /// </para>
        ///
        /// <para><b>Lookup diagnostico passivo</b></para>
        /// <para>
        /// La ricerca non ordina, non valuta e non sceglie target decisionali. Replica
        /// soltanto la chiave minimale usata da <c>AddOrMergeByCategoryAndPosition</c>
        /// per poter copiare il belief risultante nel log.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Category</b>: dominio belief risolto dalla trace.</item>
        ///   <item><b>Position</b>: cella stimata usata dal merge.</item>
        ///   <item><b>Output</b>: entry copiata dallo store, se trovata.</item>
        /// </list>
        /// </summary>
        private static bool TryFindBelief(
            BeliefStore store,
            BeliefCategory category,
            Vector2Int position,
            out BeliefEntry belief)
        {
            belief = default;

            var entries = store.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Category != category || entry.EstimatedPosition != position)
                    continue;

                belief = entry;
                return true;
            }

            return false;
        }

        // =============================================================================
        // UpdateFromOperationalFailure
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica al BeliefStore un feedback operativo prodotto da rule, command o
        /// system che hanno scoperto un fallimento durante l'esecuzione.
        /// </para>
        ///
        /// <para><b>Ponte MVP verso job fallito</b></para>
        /// <para>
        /// Il Job System definitivo non esiste ancora, quindi questo metodo riceve un
        /// payload provvisorio. La responsabilita' resta pero' quella definitiva: il
        /// producer classifica il fallimento, il BeliefUpdater decide la mutazione
        /// cognitiva, il BeliefStore applica solo modifiche passive.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>DirectLocalContradiction</b>: trasforma il belief in <c>Discarded</c>.</item>
        ///   <item><b>AmbiguousExecutionFailure</b>: riduce confidence e marca <c>Weak</c>.</item>
        ///   <item><b>ReportedContradiction</b>: riduce confidence e marca <c>Conflicted</c>.</item>
        /// </list>
        /// </summary>
        public bool UpdateFromOperationalFailure(in BeliefFailureSignal signal, BeliefStore store)
        {
            if (store == null)
                return false;

            if (signal.BeliefId > 0)
                return UpdateByBeliefId(signal, store);

            return UpdateByCategoryAndPosition(signal, store);
        }

        // =============================================================================
        // UpdateByBeliefId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il feedback operativo usando l'identificatore diretto della
        /// credenza, quando il layer chiamante lo possiede.
        /// </para>
        ///
        /// <para><b>Percorso futuro preferito</b></para>
        /// <para>
        /// Quando QuerySystem e Job System saranno presenti, il job dovra' portare
        /// con se' il <c>BeliefId</c> selezionato. Questo percorso evita ambiguita'
        /// su categoria e posizione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Discard</b>: smentita diretta locale.</item>
        ///   <item><b>Weak</b>: fallimento ambiguo.</item>
        ///   <item><b>Conflicted</b>: contraddizione informativa.</item>
        /// </list>
        /// </summary>
        private static bool UpdateByBeliefId(in BeliefFailureSignal signal, BeliefStore store)
        {
            switch (signal.FailureKind)
            {
                case BeliefFailureKind.DirectLocalContradiction:
                    return store.TryDiscardBelief(signal.BeliefId, signal.Tick);
                case BeliefFailureKind.AmbiguousExecutionFailure:
                    return store.TryReduceConfidence(signal.BeliefId, signal.Penalty01, signal.Tick, BeliefStatus.Weak);
                case BeliefFailureKind.ReportedContradiction:
                    return store.TryReduceConfidence(signal.BeliefId, signal.Penalty01, signal.Tick, BeliefStatus.Conflicted);
                default:
                    return false;
            }
        }

        // =============================================================================
        // UpdateByCategoryAndPosition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il feedback operativo usando la chiave provvisoria categoria +
        /// posizione stimata.
        /// </para>
        ///
        /// <para><b>Fallback per rule pre-JobSystem</b></para>
        /// <para>
        /// Le rule attuali non hanno ancora un riferimento esplicito al belief che
        /// ha guidato l'azione. Usiamo quindi la stessa chiave minimale dello store,
        /// sapendo che e' una soluzione transitoria da sostituire con <c>BeliefId</c>
        /// quando il Job System trasportera' la provenienza decisionale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Category</b>: dominio del belief coinvolto.</item>
        ///   <item><b>EstimatedPosition</b>: cella soggettiva verificata o coinvolta dal fallimento.</item>
        ///   <item><b>FailureKind</b>: seleziona la mutazione cognitiva.</item>
        /// </list>
        /// </summary>
        private static bool UpdateByCategoryAndPosition(in BeliefFailureSignal signal, BeliefStore store)
        {
            switch (signal.FailureKind)
            {
                case BeliefFailureKind.DirectLocalContradiction:
                    return store.TryDiscardByCategoryAndPosition(signal.Category, signal.EstimatedPosition, signal.Tick);
                case BeliefFailureKind.AmbiguousExecutionFailure:
                    return store.TryReduceConfidenceByCategoryAndPosition(signal.Category, signal.EstimatedPosition, signal.Penalty01, signal.Tick, BeliefStatus.Weak);
                case BeliefFailureKind.ReportedContradiction:
                    return store.TryReduceConfidenceByCategoryAndPosition(signal.Category, signal.EstimatedPosition, signal.Penalty01, signal.Tick, BeliefStatus.Conflicted);
                default:
                    return false;
            }
        }
    }

    // =============================================================================
    // DangerBeliefAggregationRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Regola MVP che aggrega memorie di pericolo in credenze <c>Danger</c>.
    /// </para>
    ///
    /// <para><b>Mappatura conservativa</b></para>
    /// <para>
    /// Le tracce di predatore, aggressione, quasi morte e morte osservata sono segnali
    /// soggettivi di rischio. La regola non cerca la minaccia migliore e non valuta
    /// distanza o urgenza: produce solo una credenza di categoria <c>Danger</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Matches</b>: riconosce i MemoryType di minaccia documentabili già presenti.</item>
    ///   <item><b>Apply</b>: usa posizione, affidabilità e intensità della trace.</item>
    /// </list>
    /// </summary>
    internal sealed class DangerBeliefAggregationRule : IBeliefAggregationRule
    {
        public bool Matches(MemoryTrace trace)
        {
            return trace.Type == MemoryType.PredatorSpotted
                || trace.Type == MemoryType.PredatorRumor
                || trace.Type == MemoryType.AttackSuffered
                || trace.Type == MemoryType.AttackWitnessed
                || trace.Type == MemoryType.NearDeathExperience
                || trace.Type == MemoryType.DeathWitnessed;
        }

        public void Apply(MemoryTrace trace, BeliefStore store, int currentTick)
        {
            BeliefAggregationRuleCommon.ApplyCommon(trace, store, currentTick, BeliefCategory.Danger);
        }
    }

    // =============================================================================
    // ObjectBeliefAggregationRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Regola MVP che aggrega memorie di oggetti osservati in credenze categorizzate
    /// in base al <c>SubjectDefId</c> già presente nella <c>MemoryTrace</c>.
    /// </para>
    ///
    /// <para><b>Semantica fissata al momento percettivo</b></para>
    /// <para>
    /// La regola non legge <c>World.Objects</c> e non recupera definizioni globali.
    /// Usa soltanto il <c>SubjectDefId</c> trascritto nella memoria quando l'NPC ha
    /// percepito l'oggetto. Se il dato manca, ad esempio per una memoria legacy, la
    /// categoria resta conservativamente <c>Structure</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Matches</b>: tratta solo <c>MemoryType.ObjectSpotted</c>.</item>
    ///   <item><b>Food</b>: defId che contiene <c>food</c>.</item>
    ///   <item><b>Rest</b>: defId che contiene <c>bed</c>.</item>
    ///   <item><b>Structure fallback</b>: defId assente o non riconosciuto.</item>
    /// </list>
    /// </summary>
    internal sealed class ObjectBeliefAggregationRule : IBeliefAggregationRule
    {
        public bool Matches(MemoryTrace trace)
        {
            return trace.Type == MemoryType.ObjectSpotted;
        }

        public void Apply(MemoryTrace trace, BeliefStore store, int currentTick)
        {
            BeliefCategory category = BeliefAggregationRuleCommon.ClassifyObjectDefId(trace.SubjectDefId);
            BeliefAggregationRuleCommon.ApplyCommon(trace, store, currentTick, category);
        }
    }

    // =============================================================================
    // SocialBeliefAggregationRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Regola MVP che aggrega memorie di NPC osservati in credenze <c>Social</c>.
    /// </para>
    ///
    /// <para><b>Nessuna inferenza relazionale prematura</b></para>
    /// <para>
    /// La regola non deduce fiducia, ostilità o alleanza. Registra soltanto che, dal
    /// punto di vista dell'NPC osservatore, esiste una credenza soggettiva relativa a
    /// un altro NPC visto in una posizione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Matches</b>: tratta solo <c>MemoryType.NpcSpotted</c>.</item>
    ///   <item><b>Apply</b>: conserva la localizzazione stimata dell'NPC osservato.</item>
    /// </list>
    /// </summary>
    internal sealed class SocialBeliefAggregationRule : IBeliefAggregationRule
    {
        public bool Matches(MemoryTrace trace)
        {
            return trace.Type == MemoryType.NpcSpotted;
        }

        public void Apply(MemoryTrace trace, BeliefStore store, int currentTick)
        {
            BeliefAggregationRuleCommon.ApplyCommon(trace, store, currentTick, BeliefCategory.Social);
        }
    }

    // =============================================================================
    // BeliefAggregationRuleCommon
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper interno condiviso dalle regole MVP di aggregazione belief.
    /// </para>
    ///
    /// <para><b>Riduzione di duplicazione senza introdurre scoring</b></para>
    /// <para>
    /// L'helper applica solo la mappatura dati comune: posizione, reliability come
    /// confidence, intensity come freshness e origine della fonte. Non calcola ranking
    /// e non interpreta le categorie oltre quanto richiesto dalle regole chiamanti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Source</b>: <c>Heard</c> se la trace arriva da comunicazione, altrimenti <c>Seen</c>.</item>
    ///   <item><b>Position</b>: usa la cella della MemoryTrace come posizione stimata.</item>
    ///   <item><b>Store</b>: delega al BeliefStore la creazione o il merge dell'entry.</item>
    /// </list>
    /// </summary>
    internal static class BeliefAggregationRuleCommon
    {
        public static void ApplyCommon(MemoryTrace trace, BeliefStore store, int currentTick, BeliefCategory category)
        {
            BeliefSource source = trace.IsHeard ? BeliefSource.Heard : BeliefSource.Seen;
            var position = new Vector2Int(trace.CellX, trace.CellY);

            store.AddOrMergeByCategoryAndPosition(
                category,
                position,
                trace.Reliability01,
                trace.Intensity01,
                currentTick,
                source);
        }

        // =============================================================================
        // ClassifyObjectDefId
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il <c>DefId</c> memorizzato nella trace di un oggetto osservato
        /// nella categoria belief minima corrispondente.
        /// </para>
        ///
        /// <para><b>Classificazione conservativa senza lookup globale</b></para>
        /// <para>
        /// Questa funzione non interroga il database oggetti e non legge il world state.
        /// Usa solo il testo già conservato nella <c>MemoryTrace</c>. Il matching su
        /// stringa è volutamente un MVP coerente con le euristiche già presenti per
        /// cibo e letto; in futuro potrà essere sostituito da tag o proprietà
        /// serializzate direttamente nella trace.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>food</b>: produce <c>BeliefCategory.Food</c>.</item>
        ///   <item><b>bed</b>: produce <c>BeliefCategory.Rest</c>.</item>
        ///   <item><b>fallback</b>: produce <c>BeliefCategory.Structure</c>.</item>
        /// </list>
        /// </summary>
        public static BeliefCategory ClassifyObjectDefId(string subjectDefId)
        {
            if (string.IsNullOrWhiteSpace(subjectDefId))
                return BeliefCategory.Structure;

            string normalized = subjectDefId.ToLowerInvariant();

            if (normalized.Contains("food"))
                return BeliefCategory.Food;

            if (normalized.Contains("bed"))
                return BeliefCategory.Rest;

            return BeliefCategory.Structure;
        }
    }
}
