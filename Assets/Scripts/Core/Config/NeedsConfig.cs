using System;

namespace Arcontio.Core
{
    // =============================================================================
    // NeedsConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione data-driven dei bisogni NPC attualmente attivati nel runtime.
    /// I valori continui seguono la convenzione comune di ARCONTIO: 0 indica bisogno
    /// soddisfatto, 1 indica bisogno critico, e il decay per tick fa crescere il
    /// bisogno quando nessun sistema o comando lo soddisfa.
    /// </para>
    ///
    /// <para><b>Separazione tra stato oggettivo, stato soggettivo e bisogno interno</b></para>
    /// <para>
    /// Questa struct non consulta il mondo e non decide comportamenti: contiene solo
    /// parametri globali di baseline. I bisogni psicologici introdotti in v0.04.11
    /// crescono come pressione interna lenta; gli agganci a pericolo percepito,
    /// socialità, memoria o BeliefStore restano responsabilità di sistemi successivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Fame, Sete, Riposo</b>: bisogni fisiologici già attivi, con decay e recuperi espliciti.</item>
    ///   <item><b>Sicurezza, Stabilità, Socialità</b>: bisogni psicologici baseline, per ora alimentati da decay lento.</item>
    ///   <item><b>Normalizzazione</b>: i valori mancanti o non positivi vengono ricondotti ai default per proteggere il runtime da JSON parziali.</item>
    ///   <item><b>Health e Comfort</b>: non vengono aggiunti qui come need autonomi perché lo step 10 è parziale; la salute fisica deriva dal futuro BodyWound System e il comfort resta derivativo.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct NeedsConfig
    {
        // ── Fame ─────────────────────────────────────────────────────────────────
        public float satietyDecayPerTick;
        public float eatSatietyGain;
        public float hungryThreshold;

        // ── Sete ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// v0.04.08: decay attivo. Recovery (DrinkCommand) pendente — richiede
        /// water source WorldObject non ancora presente nel mondo.
        /// </summary>
        public float thirstDecayPerTick;
        public float drinkThirstGain;
        public float thirstyThreshold;

        // ── Riposo ───────────────────────────────────────────────────────────────
        public float restDecayPerTick;
        public float sleepRestGainPerTick;
        public float tiredThreshold;

        // ── Needs psicologici (v0.04.11) ────────────────────────────────────────
        /// <summary>
        /// Decay lento della sicurezza percepita come bisogno interno. Non legge
        /// ancora danger belief o world state: l'integrazione percettiva appartiene
        /// al futuro BeliefStore/QuerySystem.
        /// </summary>
        public float securityDecayPerTick;

        /// <summary>
        /// Decay lento della stabilità emotiva. In futuro potrà essere accelerato
        /// da dolore, fallimenti, drift DNA↔Profile o eventi traumatici, ma qui
        /// rimane una baseline data-driven.
        /// </summary>
        public float stabilityDecayPerTick;

        /// <summary>
        /// Decay lento della socialità. In futuro verrà recuperato da interazioni,
        /// comunicazione e supporto sociale percepito; per ora non anticipa il
        /// Social Layer e non consulta direttamente altri NPC.
        /// </summary>
        public float socialityDecayPerTick;

        /// <summary>
        /// Valori di default usati come fallback se needs_config.json non è presente.
        /// Aggiornato in v0.04.11 con parametri psicologici baseline.
        /// </summary>
        public static NeedsConfig Default()
        {
            return new NeedsConfig
            {
                // Fame
                satietyDecayPerTick = 0.0025f,
                eatSatietyGain      = 0.45f,
                hungryThreshold     = 0.70f,

                // Sete — decay più rapido della fame (liquidi si consumano prima)
                thirstDecayPerTick  = 0.0035f,
                drinkThirstGain     = 0.60f,
                thirstyThreshold    = 0.70f,

                // Riposo
                restDecayPerTick    = 0.0020f,
                sleepRestGainPerTick = 0.030f,
                tiredThreshold      = 0.70f,

                // Psicologici — decay lenti: pressione interna, non trigger decisionale diretto
                securityDecayPerTick  = 0.0007f,
                stabilityDecayPerTick = 0.0005f,
                socialityDecayPerTick = 0.0006f,
            };
        }

        // =============================================================================
        // WithFallbackDefaults
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce una copia normalizzata della configurazione caricata da JSON,
        /// sostituendo con i default ogni parametro numerico che risulta mancante,
        /// nullo o non positivo.
        /// </para>
        ///
        /// <para><b>Compatibilità con JSON parziali</b></para>
        /// <para>
        /// <c>JsonUtility</c> non distingue in modo pratico tra un campo assente e un
        /// campo float presente con valore 0. Per il sistema needs questo è rischioso:
        /// un decay lasciato a 0 per errore bloccherebbe per sempre quel bisogno.
        /// Per questo motivo, nella configurazione runtime, 0 viene trattato come
        /// "usa default". Se in futuro servirà disabilitare davvero un decay, conviene
        /// introdurre un flag esplicito invece di usare 0 come significato implicito.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Default completi</b>: vengono calcolati una sola volta tramite <c>Default()</c>.</item>
        ///   <item><b>Campi fisiologici</b>: decay, gain e soglie operative vengono protetti da valori non positivi.</item>
        ///   <item><b>Campi psicologici</b>: i decay lenti introdotti in v0.04.11 ricevono fallback dedicato.</item>
        /// </list>
        /// </summary>
        public static NeedsConfig WithFallbackDefaults(NeedsConfig raw)
        {
            var defaults = Default();

            // Bisogni fisiologici: questi parametri erano già presenti prima della
            // sessione 12, ma normalizzarli qui rende il loader simmetrico e robusto.
            raw.satietyDecayPerTick = UseDefaultIfNonPositive(raw.satietyDecayPerTick, defaults.satietyDecayPerTick);
            raw.eatSatietyGain      = UseDefaultIfNonPositive(raw.eatSatietyGain,      defaults.eatSatietyGain);
            raw.hungryThreshold     = UseDefaultIfNonPositive(raw.hungryThreshold,     defaults.hungryThreshold);

            raw.thirstDecayPerTick = UseDefaultIfNonPositive(raw.thirstDecayPerTick, defaults.thirstDecayPerTick);
            raw.drinkThirstGain    = UseDefaultIfNonPositive(raw.drinkThirstGain,    defaults.drinkThirstGain);
            raw.thirstyThreshold   = UseDefaultIfNonPositive(raw.thirstyThreshold,   defaults.thirstyThreshold);

            raw.restDecayPerTick     = UseDefaultIfNonPositive(raw.restDecayPerTick,     defaults.restDecayPerTick);
            raw.sleepRestGainPerTick = UseDefaultIfNonPositive(raw.sleepRestGainPerTick, defaults.sleepRestGainPerTick);
            raw.tiredThreshold       = UseDefaultIfNonPositive(raw.tiredThreshold,       defaults.tiredThreshold);

            // Bisogni psicologici: questa è la parte critica per compatibilità con
            // vecchi needs_config.json che non conoscevano ancora questi campi.
            raw.securityDecayPerTick  = UseDefaultIfNonPositive(raw.securityDecayPerTick,  defaults.securityDecayPerTick);
            raw.stabilityDecayPerTick = UseDefaultIfNonPositive(raw.stabilityDecayPerTick, defaults.stabilityDecayPerTick);
            raw.socialityDecayPerTick = UseDefaultIfNonPositive(raw.socialityDecayPerTick, defaults.socialityDecayPerTick);

            return raw;
        }

        // =============================================================================
        // UseDefaultIfNonPositive
        // =============================================================================
        /// <summary>
        /// <para>
        /// Helper minimale per applicare fallback numerici ai parametri di config.
        /// </para>
        ///
        /// <para><b>Regola anti-config muta</b></para>
        /// <para>
        /// Un valore minore o uguale a 0 viene considerato non valido per i parametri
        /// di decay, recupero e soglia attualmente esposti. Questa scelta evita che
        /// un campo mancante nel JSON produca un sistema apparentemente funzionante
        /// ma privo di pressione simulativa.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>value</b>: valore letto dalla configurazione esterna.</item>
        ///   <item><b>fallback</b>: valore di default codificato in <c>NeedsConfig.Default()</c>.</item>
        /// </list>
        /// </summary>
        private static float UseDefaultIfNonPositive(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }

        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    // =============================================================================
    // NeedsConfigDatabase
    // =============================================================================
    /// <summary>
    /// <para>
    /// Wrapper serializzabile richiesto da <c>JsonUtility</c> per caricare la
    /// configurazione dei bisogni da Resources.
    /// </para>
    ///
    /// <para><b>Contratto di caricamento data-driven</b></para>
    /// <para>
    /// Mantiene il file JSON nella forma stabile <c>{ "Needs": { ... } }</c>, così
    /// il loader può sostituire l'intera configurazione globale del mondo senza
    /// conoscere i dettagli dei singoli campi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Needs</b>: payload principale con decay, recuperi e soglie operative.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class NeedsConfigDatabase
    {
        public NeedsConfig Needs;
    }
}
