using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // NeedsConfigLoader
    // =============================================================================
    /// <summary>
    /// <para>
    /// Carica da Resources la configurazione data-driven dei bisogni NPC e la
    /// installa nello stato globale del mondo simulativo.
    /// </para>
    ///
    /// <para><b>Progressive integration dei bisogni</b></para>
    /// <para>
    /// Il loader resta volutamente sottile: non interpreta la semantica dei need,
    /// non calcola derivati e non consulta il mondo. In questo modo le estensioni
    /// dello step v0.04.11 aggiungono parametri psicologici senza spostare logica
    /// decisionale dentro il layer di configurazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ResourcePath</b>: path Unity relativo ad <c>Assets/Resources</c>, senza estensione.</item>
    ///   <item><b>LoadIntoWorld</b>: entry point usato dal boot runtime per popolare <c>world.Global.Needs</c>.</item>
    ///   <item><b>Fallback</b>: default conservativi se il JSON manca, non è parsabile o contiene campi non positivi.</item>
    /// </list>
    /// </summary>
    public static class NeedsConfigLoader
    {
        private const string ResourcePath = "Arcontio/Config/needs_config"; // no ".json"

        // =============================================================================
        // LoadIntoWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge il file JSON dei bisogni da Resources e aggiorna la configurazione
        /// globale del mondo con i parametri trovati.
        /// </para>
        ///
        /// <para><b>Fail-safe configuration</b></para>
        /// <para>
        /// Se la risorsa non esiste o il parsing fallisce, il mondo riceve una
        /// configurazione di default completa. Questo evita NPC con bisogni a zero
        /// per errore di asset e mantiene il sistema avviabile anche durante patch
        /// progressive della simulazione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Caricamento</b>: usa <c>Resources.Load&lt;TextAsset&gt;</c> con path Unity.</item>
        ///   <item><b>Parsing</b>: usa <c>JsonUtility.FromJson</c> sul wrapper <c>NeedsConfigDatabase</c>.</item>
        ///   <item><b>Normalizzazione</b>: completa eventuali JSON parziali con <c>NeedsConfig.WithFallbackDefaults</c>.</item>
        ///   <item><b>Telemetria</b>: logga i parametri attivi per diagnosi rapida del runtime.</item>
        /// </list>
        /// </summary>
        public static void LoadIntoWorld(World world)
        {
            if (world == null) return;

            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                // Fallback: manteniamo default completi, inclusi i decay psicologici v0.04.11.
                world.Global.Needs = NeedsConfig.Default();
                ArcontioLogger.Warn(
                    new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsConfig"),
                    new LogBlock(LogLevel.Warn, "log.needsconfig.missing_resource")
                        .AddField("resourcePath", ResourcePath)
                );
                return;
            }

            var db = JsonUtility.FromJson<NeedsConfigDatabase>(ta.text);
            if (db == null)
            {
                world.Global.Needs = NeedsConfig.Default();
                ArcontioLogger.Warn(
                    new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsConfig"),
                    new LogBlock(LogLevel.Warn, "log.needsconfig.parse_failed")
                );
                return;
            }

            // Normalizzazione sessione 12:
            // il JSON rimane la fonte dei numeri, ma se un asset vecchio non contiene
            // i campi nuovi, o contiene valori non positivi, ricadiamo su default
            // espliciti invece di lasciare decay/gain/soglie a 0.
            world.Global.Needs = NeedsConfig.WithFallbackDefaults(db.Needs);

            // Log dei parametri attivi: serve a distinguere velocemente una config JSON caricata
            // da un fallback e a controllare che i nuovi needs psicologici non siano rimasti a 0.
            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "NeedsConfig"),
                new LogBlock(LogLevel.Info, "log.needsconfig.loaded")
                    .AddField("hungerDecay",   world.Global.Needs.satietyDecayPerTick.ToString("0.0000"))
                    .AddField("eatGain",        world.Global.Needs.eatSatietyGain.ToString("0.00"))
                    .AddField("hungryTh",       world.Global.Needs.hungryThreshold.ToString("0.00"))
                    .AddField("thirstDecay",    world.Global.Needs.thirstDecayPerTick.ToString("0.0000"))
                    .AddField("drinkGain",      world.Global.Needs.drinkThirstGain.ToString("0.00"))
                    .AddField("thirstyTh",      world.Global.Needs.thirstyThreshold.ToString("0.00"))
                    .AddField("restDecay",      world.Global.Needs.restDecayPerTick.ToString("0.0000"))
                    .AddField("sleepGain",      world.Global.Needs.sleepRestGainPerTick.ToString("0.00"))
                    .AddField("tiredTh",        world.Global.Needs.tiredThreshold.ToString("0.00"))
                    .AddField("securityDecay",  world.Global.Needs.securityDecayPerTick.ToString("0.0000"))
                    .AddField("stabilityDecay", world.Global.Needs.stabilityDecayPerTick.ToString("0.0000"))
                    .AddField("socialityDecay", world.Global.Needs.socialityDecayPerTick.ToString("0.0000"))
            );
        }
    }
}
