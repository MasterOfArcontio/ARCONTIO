using System.Collections.Generic;
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
            if (store == null)
                return false;

            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (!rule.Matches(trace))
                    continue;

                rule.Apply(trace, store, currentTick);
                return true;
            }

            return false;
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
