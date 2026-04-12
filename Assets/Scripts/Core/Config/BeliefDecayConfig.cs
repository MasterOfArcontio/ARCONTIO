using System;

namespace Arcontio.Core
{
    // =============================================================================
    // BeliefDecayConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione data-driven del decadimento passivo delle credenze soggettive
    /// conservate nel <c>BeliefStore</c> di ogni NPC.
    /// </para>
    ///
    /// <para><b>Decay differenziato per categoria</b></para>
    /// <para>
    /// Il documento BeliefStore/QuerySystem richiede che categorie diverse decadano
    /// con velocita diverse: cibo e riposo cambiano spesso, struttura cambia molto
    /// lentamente, pericolo persiste piu a lungo, mentre Situation resta provvisoria
    /// finche non esistono sottotipi dedicati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Confidence rates</b>: decadimento base per categoria.</item>
    ///   <item><b>Freshness multiplier</b>: moltiplicatore che rende Freshness piu rapida di Confidence.</item>
    ///   <item><b>Soglie</b>: limiti per passare a Weak, Stale o rimuovere la credenza.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct BeliefDecayConfig
    {
        public float foodConfidenceDecayPerTick;
        public float restConfidenceDecayPerTick;
        public float dangerConfidenceDecayPerTick;
        public float socialConfidenceDecayPerTick;
        public float ownershipConfidenceDecayPerTick;
        public float situationConfidenceDecayPerTick;
        public float structureConfidenceDecayPerTick;

        public float freshnessDecayMultiplier;
        public float weakConfidenceThreshold;
        public float staleFreshnessThreshold;
        public float removeConfidenceThreshold;

        // =============================================================================
        // Default
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce valori di default conservativi per il decay belief.
        /// </para>
        ///
        /// <para><b>Baseline provvisoria</b></para>
        /// <para>
        /// I numeri sono intenzionalmente piccoli per evitare che il BeliefStore perda
        /// informazione troppo presto prima dell'arrivo del QuerySystem. <c>Situation</c>
        /// usa un valore medio perche il documento la dichiara variabile ma non espone
        /// ancora sottotipi come scarsita, crisi o disordine.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Rapido</b>: Food e Rest.</item>
        ///   <item><b>Medio</b>: Social, Ownership e Situation provvisoria.</item>
        ///   <item><b>Lento</b>: Danger.</item>
        ///   <item><b>Molto lento</b>: Structure.</item>
        /// </list>
        /// </summary>
        public static BeliefDecayConfig Default()
        {
            return new BeliefDecayConfig
            {
                foodConfidenceDecayPerTick      = 0.0018f,
                restConfidenceDecayPerTick      = 0.0015f,
                dangerConfidenceDecayPerTick    = 0.0005f,
                socialConfidenceDecayPerTick    = 0.0010f,
                ownershipConfidenceDecayPerTick = 0.0010f,
                situationConfidenceDecayPerTick = 0.0010f,
                structureConfidenceDecayPerTick = 0.0002f,

                freshnessDecayMultiplier = 2.0f,
                weakConfidenceThreshold  = 0.20f,
                staleFreshnessThreshold  = 0.01f,
                removeConfidenceThreshold = 0.001f
            };
        }

        // =============================================================================
        // WithFallbackDefaults
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza una configurazione caricata da JSON sostituendo con default i
        /// valori mancanti o non positivi.
        /// </para>
        ///
        /// <para><b>Compatibilita con JSON parziali</b></para>
        /// <para>
        /// <c>JsonUtility</c> inizializza a zero i campi assenti. Qui zero viene
        /// trattato come "usa default", per evitare che un file vecchio disattivi
        /// accidentalmente il decay dei belief.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Rates</b>: tutti i decay per categoria richiedono valori positivi.</item>
        ///   <item><b>Multiplier</b>: Freshness deve decadere piu rapidamente di Confidence.</item>
        ///   <item><b>Soglie</b>: vengono protette da valori non positivi.</item>
        /// </list>
        /// </summary>
        public static BeliefDecayConfig WithFallbackDefaults(BeliefDecayConfig raw)
        {
            var defaults = Default();

            raw.foodConfidenceDecayPerTick      = UseDefaultIfNonPositive(raw.foodConfidenceDecayPerTick,      defaults.foodConfidenceDecayPerTick);
            raw.restConfidenceDecayPerTick      = UseDefaultIfNonPositive(raw.restConfidenceDecayPerTick,      defaults.restConfidenceDecayPerTick);
            raw.dangerConfidenceDecayPerTick    = UseDefaultIfNonPositive(raw.dangerConfidenceDecayPerTick,    defaults.dangerConfidenceDecayPerTick);
            raw.socialConfidenceDecayPerTick    = UseDefaultIfNonPositive(raw.socialConfidenceDecayPerTick,    defaults.socialConfidenceDecayPerTick);
            raw.ownershipConfidenceDecayPerTick = UseDefaultIfNonPositive(raw.ownershipConfidenceDecayPerTick, defaults.ownershipConfidenceDecayPerTick);
            raw.situationConfidenceDecayPerTick = UseDefaultIfNonPositive(raw.situationConfidenceDecayPerTick, defaults.situationConfidenceDecayPerTick);
            raw.structureConfidenceDecayPerTick = UseDefaultIfNonPositive(raw.structureConfidenceDecayPerTick, defaults.structureConfidenceDecayPerTick);

            raw.freshnessDecayMultiplier = UseDefaultIfNonPositive(raw.freshnessDecayMultiplier, defaults.freshnessDecayMultiplier);
            raw.weakConfidenceThreshold  = UseDefaultIfNonPositive(raw.weakConfidenceThreshold,  defaults.weakConfidenceThreshold);
            raw.staleFreshnessThreshold  = UseDefaultIfNonPositive(raw.staleFreshnessThreshold,  defaults.staleFreshnessThreshold);
            raw.removeConfidenceThreshold = UseDefaultIfNonPositive(raw.removeConfidenceThreshold, defaults.removeConfidenceThreshold);

            return raw;
        }

        // =============================================================================
        // GetConfidenceDecayFor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il decay di confidence associato alla categoria belief ricevuta.
        /// </para>
        ///
        /// <para><b>Policy meccanica, non decisionale</b></para>
        /// <para>
        /// La funzione non valuta l'utilita di una credenza e non legge altri sistemi.
        /// Espone solo la tabella dei rate configurati, cosi il <c>BeliefStore</c>
        /// puo aggiornare i propri dati passivi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>category</b>: categoria semantica della credenza.</item>
        ///   <item><b>return</b>: decay per tick da moltiplicare per il delta simulativo.</item>
        /// </list>
        /// </summary>
        public float GetConfidenceDecayFor(BeliefCategory category)
        {
            switch (category)
            {
                case BeliefCategory.Food:      return foodConfidenceDecayPerTick;
                case BeliefCategory.Rest:      return restConfidenceDecayPerTick;
                case BeliefCategory.Danger:    return dangerConfidenceDecayPerTick;
                case BeliefCategory.Social:    return socialConfidenceDecayPerTick;
                case BeliefCategory.Ownership: return ownershipConfidenceDecayPerTick;
                case BeliefCategory.Situation: return situationConfidenceDecayPerTick;
                case BeliefCategory.Structure: return structureConfidenceDecayPerTick;
                default:                       return situationConfidenceDecayPerTick;
            }
        }

        // =============================================================================
        // UseDefaultIfNonPositive
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica un fallback a un parametro numerico quando il valore caricato non
        /// e positivo.
        /// </para>
        ///
        /// <para><b>Fail-safe configuration</b></para>
        /// <para>
        /// La configurazione deve restare robusta anche con JSON parziali. Per questo
        /// i valori non positivi non entrano nel runtime del decay belief.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>value</b>: valore letto da file o default della struct.</item>
        ///   <item><b>fallback</b>: valore stabile definito in <c>Default()</c>.</item>
        /// </list>
        /// </summary>
        private static float UseDefaultIfNonPositive(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }
    }

    // =============================================================================
    // BeliefDecayConfigDatabase
    // =============================================================================
    /// <summary>
    /// <para>
    /// Wrapper serializzabile richiesto da <c>JsonUtility</c> per caricare la
    /// configurazione belief da Resources.
    /// </para>
    ///
    /// <para><b>Contratto JSON stabile</b></para>
    /// <para>
    /// Mantiene il file nella forma <c>{ "BeliefDecay": { ... } }</c>, separando i
    /// parametri delle credenze dai parametri dei bisogni e dagli oggetti del mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BeliefDecay</b>: payload principale dei rate e delle soglie.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class BeliefDecayConfigDatabase
    {
        public BeliefDecayConfig BeliefDecay;
    }
}
