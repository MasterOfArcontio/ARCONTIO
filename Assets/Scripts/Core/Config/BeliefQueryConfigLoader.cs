using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // BeliefQueryConfigLoader
    // =============================================================================
    /// <summary>
    /// <para>
    /// Carica da Resources la configurazione dei pesi MVP del QuerySystem belief e
    /// la installa nello stato globale del mondo.
    /// </para>
    ///
    /// <para><b>Configurazione separata dal decay</b></para>
    /// <para>
    /// Il decay modifica lo stato passivo dei belief; la query valuta candidati per
    /// il Decision Layer. Tenere configurazioni separate evita di mescolare tempi di
    /// dimenticanza e pesi decisionali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ResourcePath</b>: path Unity relativo a <c>Assets/Resources</c>, senza estensione.</item>
    ///   <item><b>LoadIntoWorld</b>: entry point usato dal boot runtime.</item>
    ///   <item><b>Fallback</b>: default se la risorsa manca o il JSON e' parziale.</item>
    /// </list>
    /// </summary>
    public static class BeliefQueryConfigLoader
    {
        private const string ResourcePath = "Arcontio/Config/belief_query_config";

        // =============================================================================
        // LoadIntoWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica il JSON dei pesi di query belief e installa la configurazione dentro
        /// <c>world.Global.BeliefQuery</c>.
        /// </para>
        ///
        /// <para><b>Bootstrap progressivo</b></para>
        /// <para>
        /// Il loader viene chiamato dal runtime host durante l'avvio, come gli altri
        /// loader di configurazione, e mantiene il Decision Layer futuro separato dal
        /// formato fisico del file.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Null guard</b>: uno world assente interrompe il caricamento.</item>
        ///   <item><b>Resource load</b>: legge il TextAsset sotto <c>Resources</c>.</item>
        ///   <item><b>Fallback</b>: usa default se la risorsa manca o il parsing fallisce.</item>
        /// </list>
        /// </summary>
        public static void LoadIntoWorld(World world)
        {
            if (world == null) return;

            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                // Mancanza del file: non blocchiamo la simulazione, ma segnaliamo il
                // fallback per rendere visibile l'asset mancante in telemetry/log.
                world.Global.BeliefQuery = BeliefQueryConfig.Default();
                ArcontioLogger.Warn(
                    new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "BeliefQueryConfig"),
                    new LogBlock(LogLevel.Warn, "log.beliefqueryconfig.missing_resource")
                        .AddField("resourcePath", ResourcePath)
                );
                return;
            }

            var db = JsonUtility.FromJson<BeliefQueryConfigDatabase>(ta.text);
            if (db == null)
            {
                // JSON non interpretabile: stessa policy della risorsa mancante,
                // per evitare che il bootstrap fallisca in modo non recuperabile.
                world.Global.BeliefQuery = BeliefQueryConfig.Default();
                ArcontioLogger.Warn(
                    new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "BeliefQueryConfig"),
                    new LogBlock(LogLevel.Warn, "log.beliefqueryconfig.parse_failed")
                );
                return;
            }

            // I campi assenti restano a zero con JsonUtility: il merge con fallback
            // rende la configurazione robusta a file parziali o versioni intermedie.
            world.Global.BeliefQuery = BeliefQueryConfig.WithFallbackDefaults(db.BeliefQuery);

            ArcontioLogger.Info(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "BeliefQueryConfig"),
                new LogBlock(LogLevel.Info, "log.beliefqueryconfig.loaded")
                    .AddField("confidence", world.Global.BeliefQuery.confidenceWeight.ToString("0.00"))
                    .AddField("freshness", world.Global.BeliefQuery.freshnessWeight.ToString("0.00"))
                    .AddField("distance", world.Global.BeliefQuery.distanceWeight.ToString("0.00"))
                    .AddField("maxDist", world.Global.BeliefQuery.maxDistanceCells.ToString("0.0"))
                    .AddField("urgentDistMult", world.Global.BeliefQuery.highUrgencyDistancePenaltyMultiplier.ToString("0.00"))
                    .AddField("minConfidence", world.Global.BeliefQuery.defaultMinConfidence.ToString("0.00"))
            );
        }
    }
}
