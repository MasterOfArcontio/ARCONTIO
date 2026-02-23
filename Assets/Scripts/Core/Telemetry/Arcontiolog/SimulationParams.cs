using System;
using UnityEngine;

namespace Arcontio.Core.Config
{
    [Serializable]
    public sealed class SimulationParams
    {
        // ============================================================
        // NOTE IMPORTANTI (DayXX / Debug FOV patch)
        // ============================================================
        // Questo file viene deserializzato da game_params.json tramite JsonUtility.
        // Alcune sezioni del JSON (es: Logging) sono consumate da GameParams (logger)
        // e NON da SimulationParams: è ok, JsonUtility ignora i campi sconosciuti.
        //
        // Qui mettiamo SOLO parametri che influenzano la simulazione (o debug tooling
        // strettamente legato alla simulazione), evitando di duplicare cose del logger.
        // ============================================================

        // ---------------- Mappa ----------------
        public int worldWidth = 64;
        public int worldHeight = 64;

        // ---------------- Localizzazione (usata anche dal logger) ----------------
        public string Language = "it";

        // ---------------- Perception cone params (ARCONTIO Standard) ----------------
        // NOTA: il core oggi usa:
        // - range: Global.NpcVisionRangeCells
        // - cone gate: Global.NpcVisionUseCone + Global.NpcVisionConeSlope
        // - LOS: World.HasLineOfSight(...) (OcclusionMap)
        //
        // Qui esponiamo i parametri in game_params.json, così non sono hardcoded.

        /// <summary>
        /// Range massimo di visione dell'NPC (in celle, Manhattan gate).
        /// </summary>
        public int npcVisionRangeCells = 6;

        /// <summary>
        /// Se true: usiamo il cono (IsInCone). Se false: modalità legacy "davanti".
        /// </summary>
        public bool npcVisionUseCone = true;

        /// <summary>
        /// Slope del cono su griglia:
        /// maxSide = floor(forward * slope)
        ///  - slope=1.0 ~ half-angle 45° -> FOV 90°.
        /// Se vuoi usare gradi invece di slope, imposta npcVisionFovDegrees.
        /// </summary>
        public float npcVisionConeSlope = 1.0f;

        /// <summary>
        /// FOV in gradi (solo se vuoi esprimere il cono in gradi in game_params.json).
        /// Se > 0 e npcVisionConeSlope <= 0, il World calcolerà slope = tan(fov/2).
        /// Esempio: 90° => tan(45°)=1.0.
        /// </summary>
        public int npcVisionFovDegrees = 90;

        // ---------------- Debug FOV heatmap (view overlay) ----------------
        public DebugFovParams debug_fov = new DebugFovParams();
    }

    /// <summary>
    /// Parametri per la telemetria FOV di debug.
    ///
    /// Obiettivo:
    /// - accumulare (per NPC) quante volte ogni cella è stata vista
    ///   in una finestra di N tick.
    /// - ogni N tick: swap buffer e reset.
    /// - la view legge SOLO il buffer "read" (stabile).
    ///
    /// Nota:
    /// - Questo NON influenza la simulazione.
    /// - Serve per debug/visualizzazione e può essere disattivato da config.
    /// </summary>
    [Serializable]
    public sealed class DebugFovParams
    {
        public bool enabled = false;

        /// <summary>
        /// Ogni window_ticks tick avviene lo swap e la finestra riparte da zero.
        /// </summary>
        public int window_ticks = 8;

        /// <summary>
        /// Se true la heatmap usa gli stessi gate del core (Range+Cone+LOS).
        /// Se false: registra celle in Range+Cone ignorando LOS (overlay NON fedele).
        /// </summary>
        public bool use_los = true;

        /// <summary>
        /// Se true, la view renderizza solo l'NPC attivo (hover o fallback primo NPC).
        /// In questa patch la view si comporta sempre così; il flag resta per completezza.
        /// </summary>
        public bool activeNpcOnly = true;
    }

    public static class SimulationParamsLoader
    {
        public static SimulationParams LoadFromResources(string resourcesPathNoExt)
        {
            var ta = Resources.Load<TextAsset>(resourcesPathNoExt);
            if (ta == null)
            {
                Debug.LogWarning($"[Arcontio] Missing sim params at Resources/{resourcesPathNoExt}.json. Using defaults.");
                return new SimulationParams();
            }

            try
            {
                return JsonUtility.FromJson<SimulationParams>(ta.text) ?? new SimulationParams();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Arcontio] Failed parsing sim params: {ex}");
                return new SimulationParams();
            }
        }
    }
}
