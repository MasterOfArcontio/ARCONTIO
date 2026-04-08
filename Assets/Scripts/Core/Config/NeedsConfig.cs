using System;

namespace Arcontio.Core
{
    /// <summary>
    /// NeedsConfig (v0.04.08 — Fame · Sete · Riposo):
    /// Parametri data-driven per i tre bisogni fisiologici primari attivati in questa sessione.
    ///
    /// Convenzione valori:
    ///   0 = bisogno soddisfatto, 1 = bisogno critico
    ///   Il valore cresce nel tempo (decay) e scende quando il bisogno viene soddisfatto (gain).
    ///
    /// ── FAME (Hunger) ────────────────────────────────────────────────────────────
    ///   satietyDecayPerTick
    ///     Quanto cresce Hunger ogni tick (metabolismo base).
    ///     Valore tipico: 0.0025 → fame critica in ~400 tick.
    ///
    ///   eatSatietyGain
    ///     Quanto diminuisce Hunger quando l'NPC consuma 1 unità di cibo.
    ///     Valore tipico: 0.45 → ~2-3 pasti per passare da critico a 0.
    ///
    ///   hungryThreshold
    ///     Soglia sopra la quale NeedsDecisionRule cerca cibo.
    ///     Distinta da NeedAlert01/NeedCritical01 del DNA (quelle sono per-NPC).
    ///
    /// ── SETE (Thirst) ────────────────────────────────────────────────────────────
    ///   thirstDecayPerTick
    ///     Quanto cresce Thirst ogni tick.
    ///     Leggermente più rapido di Hunger (liquidi si consumano prima).
    ///     Valore tipico: 0.0035.
    ///
    ///   drinkThirstGain
    ///     Quanto diminuisce Thirst quando l'NPC beve (1 azione di bere).
    ///     Valore tipico: 0.60 → ~1-2 sorsi per passare da critico a 0.
    ///
    ///   thirstyThreshold
    ///     Soglia sopra la quale NeedsDecisionRule cerca acqua.
    ///     NOTA (v0.04.08): DrinkCommand non ancora implementato (mancano i WorldObject
    ///     sorgente d'acqua). La soglia è definita qui per completezza architetturale;
    ///     verrà usata attivamente quando i water source object saranno introdotti.
    ///
    /// ── RIPOSO (Rest) ────────────────────────────────────────────────────────────
    ///   restDecayPerTick
    ///     Quanto cresce Rest ogni tick (stanchezza accumulata).
    ///     Valore tipico: 0.0020 → stanchezza critica in ~500 tick.
    ///
    ///   sleepRestGainPerTick
    ///     Quanto diminuisce Rest per tick mentre l'NPC dorme in un letto.
    ///     Valore tipico: 0.030.
    ///
    ///   tiredThreshold
    ///     Soglia sopra la quale NeedsDecisionRule cerca un letto.
    ///
    /// Nota architetturale:
    ///   Questa config è GLOBALE (stessa per tutti gli NPC).
    ///   In futuro i decay rate diventeranno per-NPC (campo in NpcDnaProfile).
    ///   Questa struttura rimarrà come baseline / fallback.
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

        /// <summary>
        /// Valori di default usati come fallback se needs_config.json non è presente.
        /// Aggiornato in v0.04.08 con parametri sete.
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
            };
        }

        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    /// <summary>
    /// Wrapper serializzabile per JsonUtility.
    /// JSON atteso: { "Needs": { ... } }
    /// </summary>
    [Serializable]
    public sealed class NeedsConfigDatabase
    {
        public NeedsConfig Needs;
    }
}
