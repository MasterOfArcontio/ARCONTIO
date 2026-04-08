using Arcontio.Core.Logging;
using UnityEngine;

namespace Arcontio.Core
{
    /// <summary>
    /// NeedsConfigLoader (v0.04.08 — Fame · Sete · Riposo):
    /// Carica config bisogni fisiologici primari da JSON (Resources).
    ///
    /// Path atteso:
    ///   Assets/Resources/Arcontio/Config/needs_config.json
    ///
    /// Nota Unity:
    ///   Resources.Load vuole path RELATIVO ad Assets/Resources e SENZA estensione.
    ///   Quindi: "Arcontio/Config/needs_config"
    /// </summary>
    public static class NeedsConfigLoader
    {
        private const string ResourcePath = "Arcontio/Config/needs_config"; // no ".json"

        public static void LoadIntoWorld(World world)
        {
            if (world == null) return;

            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                // Fallback: manteniamo default (include parametri sete dal v0.04.08)
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

            world.Global.Needs = db.Needs;

            // Log tutti e 9 i parametri (3 per bisogno: decay, gain, threshold)
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
            );
        }
    }
}
