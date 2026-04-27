using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // BeliefDecayConfigLoader
    // =============================================================================
    /// <summary>
    /// <para>
    /// Carica da Resources la configurazione data-driven del decay belief e la
    /// installa nello stato globale del mondo simulativo.
    /// </para>
    ///
    /// <para><b>Separazione dei parametri cognitivi</b></para>
    /// <para>
    /// Il decay delle credenze non appartiene al file dei bisogni. Questo loader
    /// mantiene una configurazione dedicata al BeliefStore, evitando dipendenze
    /// implicite tra pressione interna degli NPC e memoria soggettiva aggregata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ResourcePath</b>: path Unity relativo ad <c>Assets/Resources</c>, senza estensione.</item>
    ///   <item><b>LoadIntoWorld</b>: entry point usato dal boot runtime per popolare <c>world.Global.BeliefDecay</c>.</item>
    ///   <item><b>Fallback</b>: default conservativi se JSON manca, non e parsabile o contiene campi non positivi.</item>
    /// </list>
    /// </summary>
    public static class BeliefDecayConfigLoader
    {
        private const string ResourcePath = "Arcontio/Config/belief_decay_config";

        // =============================================================================
        // LoadIntoWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge il file JSON del decay belief da Resources e aggiorna la configurazione
        /// globale del mondo con i parametri trovati.
        /// </para>
        ///
        /// <para><b>Fail-safe configuration</b></para>
        /// <para>
        /// Se la risorsa non esiste o il parsing fallisce, viene installata una
        /// configurazione di default completa. In questo modo il BeliefStore resta
        /// operativo anche durante patch progressive degli asset.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Caricamento</b>: usa <c>Resources.Load&lt;TextAsset&gt;</c>.</item>
        ///   <item><b>Parsing</b>: usa <c>JsonUtility.FromJson</c> sul wrapper config.</item>
        ///   <item><b>Normalizzazione</b>: completa JSON parziali con default.</item>
        ///   <item><b>Log</b>: emette i parametri principali per diagnosi runtime.</item>
        /// </list>
        /// </summary>
        public static void LoadIntoWorld(World world)
        {
            if (world == null) return;

            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                world.Global.BeliefDecay = BeliefDecayConfig.Default();
                ArcontioLogger.Warn(
                    new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "BeliefDecayConfig"),
                    new LogBlock(LogLevel.Warn, "log.beliefdecayconfig.missing_resource")
                        .AddField("resourcePath", ResourcePath)
                );
                return;
            }

            var db = JsonUtility.FromJson<BeliefDecayConfigDatabase>(ta.text);
            if (db == null)
            {
                world.Global.BeliefDecay = BeliefDecayConfig.Default();
                ArcontioLogger.Warn(
                    new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "BeliefDecayConfig"),
                    new LogBlock(LogLevel.Warn, "log.beliefdecayconfig.parse_failed")
                );
                return;
            }

            world.Global.BeliefDecay = BeliefDecayConfig.WithFallbackDefaults(db.BeliefDecay);

            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "BeliefDecayConfig"),
                new LogBlock(LogLevel.Info, "log.beliefdecayconfig.loaded")
                    .AddField("food",      world.Global.BeliefDecay.foodConfidenceDecayPerTick.ToString("0.0000"))
                    .AddField("rest",      world.Global.BeliefDecay.restConfidenceDecayPerTick.ToString("0.0000"))
                    .AddField("danger",    world.Global.BeliefDecay.dangerConfidenceDecayPerTick.ToString("0.0000"))
                    .AddField("social",    world.Global.BeliefDecay.socialConfidenceDecayPerTick.ToString("0.0000"))
                    .AddField("ownership", world.Global.BeliefDecay.ownershipConfidenceDecayPerTick.ToString("0.0000"))
                    .AddField("situation", world.Global.BeliefDecay.situationConfidenceDecayPerTick.ToString("0.0000"))
                    .AddField("structure", world.Global.BeliefDecay.structureConfidenceDecayPerTick.ToString("0.0000"))
                    .AddField("freshMult", world.Global.BeliefDecay.freshnessDecayMultiplier.ToString("0.00"))
                    .AddField("weakTh",    world.Global.BeliefDecay.weakConfidenceThreshold.ToString("0.00"))
                    .AddField("staleTh",   world.Global.BeliefDecay.staleFreshnessThreshold.ToString("0.00"))
            );
        }
    }
}
