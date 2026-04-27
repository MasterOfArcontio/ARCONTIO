using System;

namespace Arcontio.Core
{
    // =============================================================================
    // BeliefQueryConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione data-driven dei pesi MVP usati dal QuerySystem del BeliefStore.
    /// </para>
    ///
    /// <para><b>Pesi nominati per evaluator</b></para>
    /// <para>
    /// Il documento vieta pesi hardcoded inline: gli evaluator devono leggere costanti
    /// nominate da configurazione. Questa struct contiene solo i pesi dei tre
    /// evaluator fase 1: confidence, freshness e distance.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>confidenceWeight</b>: peso della sicurezza soggettiva.</item>
    ///   <item><b>freshnessWeight</b>: peso della freschezza informativa.</item>
    ///   <item><b>distanceWeight</b>: peso della penalita' spaziale.</item>
    ///   <item><b>maxDistanceCells</b>: distanza usata per normalizzare il manhattan.</item>
    ///   <item><b>defaultMinConfidence</b>: soglia minima per i candidati.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct BeliefQueryConfig
    {
        public float confidenceWeight;
        public float freshnessWeight;
        public float distanceWeight;
        public float maxDistanceCells;
        public float highUrgencyDistancePenaltyMultiplier;
        public float defaultMinConfidence;

        // =============================================================================
        // Default
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce i valori di fallback usati quando il file JSON manca o contiene
        /// campi non validi.
        /// </para>
        ///
        /// <para><b>Fallback conservativo</b></para>
        /// <para>
        /// I default consentono alla simulazione di partire anche senza configurazione
        /// esterna, ma i valori effettivi restano comunque nominati e centralizzati.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Confidence</b>: peso principale, ma non esclusivo.</item>
        ///   <item><b>Freshness</b>: peso secondario per favorire tracce recenti.</item>
        ///   <item><b>Distance</b>: penalita' spaziale limitata dalla distanza massima.</item>
        /// </list>
        /// </summary>
        public static BeliefQueryConfig Default()
        {
            return new BeliefQueryConfig
            {
                confidenceWeight = 0.40f,
                freshnessWeight = 0.20f,
                distanceWeight = 0.30f,
                maxDistanceCells = 24f,
                highUrgencyDistancePenaltyMultiplier = 0.35f,
                defaultMinConfidence = 0.05f
            };
        }

        // =============================================================================
        // WithFallbackDefaults
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza una configurazione caricata da JSON sostituendo valori assenti o
        /// non positivi con i default di sistema.
        /// </para>
        ///
        /// <para><b>Configurazione tollerante al JSON parziale</b></para>
        /// <para>
        /// Unity <c>JsonUtility</c> lascia a zero i campi non presenti: questo metodo
        /// impedisce che un file incompleto azzeri di fatto uno scorer o una soglia.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Default merge</b>: riempie i campi non positivi.</item>
        ///   <item><b>Clamp</b>: limita soglie e moltiplicatori che devono restare entro 0..1.</item>
        ///   <item><b>Return value</b>: restituisce una struct pronta per essere salvata nel World.</item>
        /// </list>
        /// </summary>
        public static BeliefQueryConfig WithFallbackDefaults(BeliefQueryConfig raw)
        {
            var defaults = Default();

            // Ogni campo numerico caricato a zero viene trattato come "non configurato":
            // in questo modo il JSON puo' essere esteso progressivamente senza crash.
            raw.confidenceWeight = UseDefaultIfNonPositive(raw.confidenceWeight, defaults.confidenceWeight);
            raw.freshnessWeight = UseDefaultIfNonPositive(raw.freshnessWeight, defaults.freshnessWeight);
            raw.distanceWeight = UseDefaultIfNonPositive(raw.distanceWeight, defaults.distanceWeight);
            raw.maxDistanceCells = UseDefaultIfNonPositive(raw.maxDistanceCells, defaults.maxDistanceCells);
            raw.highUrgencyDistancePenaltyMultiplier = UseDefaultIfNonPositive(raw.highUrgencyDistancePenaltyMultiplier, defaults.highUrgencyDistancePenaltyMultiplier);
            raw.defaultMinConfidence = UseDefaultIfNonPositive(raw.defaultMinConfidence, defaults.defaultMinConfidence);

            if (raw.highUrgencyDistancePenaltyMultiplier > 1f)
                raw.highUrgencyDistancePenaltyMultiplier = 1f;

            if (raw.defaultMinConfidence > 1f)
                raw.defaultMinConfidence = 1f;

            return raw;
        }

        // =============================================================================
        // UseDefaultIfNonPositive
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il fallback quando il valore configurato e' minore o uguale a
        /// zero.
        /// </para>
        ///
        /// <para><b>Helper locale di validazione</b></para>
        /// <para>
        /// La funzione resta privata per evitare di trasformare una piccola regola di
        /// parsing in API pubblica prematura.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Value</b>: valore proveniente dal JSON o dalla struct raw.</item>
        ///   <item><b>Fallback</b>: valore sicuro definito da <c>Default()</c>.</item>
        /// </list>
        /// </summary>
        private static float UseDefaultIfNonPositive(float value, float fallback)
        {
            // Valori negativi o zero non sono significativi per questi pesi MVP.
            return value > 0f ? value : fallback;
        }
    }

    // =============================================================================
    // BeliefQueryConfigDatabase
    // =============================================================================
    /// <summary>
    /// <para>
    /// Wrapper serializzabile richiesto da <c>JsonUtility</c> per caricare i pesi
    /// del QuerySystem da Resources.
    /// </para>
    ///
    /// <para><b>Contratto JSON stabile</b></para>
    /// <para>
    /// Mantiene il file nella forma <c>{ "BeliefQuery": { ... } }</c>, separando i
    /// pesi delle query dal decay delle credenze e dai bisogni.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BeliefQuery</b>: payload principale dei pesi evaluator MVP.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class BeliefQueryConfigDatabase
    {
        public BeliefQueryConfig BeliefQuery;
    }
}
