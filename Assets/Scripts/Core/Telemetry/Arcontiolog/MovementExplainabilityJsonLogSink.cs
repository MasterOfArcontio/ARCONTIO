using System;
using System.IO;
using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MovementExplainabilityJsonLogSink
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sink diagnostico append-only per esportare le trace EL pathfinding in formato
    /// JSONL. Ogni chiamata produce al massimo una riga JSON autonoma, pensata per
    /// debug esterno, analisi offline e confronto con il pannello runtime.
    /// </para>
    ///
    /// <para><b>Separazione simulazione / log</b></para>
    /// <para>
    /// Il sink non decide path, non modifica registry, non rilegge BeliefStore e non
    /// restituisce valori al movement. Riceve trace gia' costruite dall'emitter e le
    /// copia su disco solo se la configurazione lo richiede.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryWriteIntent</b>: export della trace intent.</item>
    ///   <item><b>TryWritePlan</b>: export della trace plan.</item>
    ///   <item><b>TryWriteExecutionEvent</b>: export degli eventi runtime.</item>
    ///   <item><b>ResolveLogPath</b>: risolve il file JSONL della sessione corrente.</item>
    /// </list>
    /// </summary>
    public static class MovementExplainabilityJsonLogSink
    {
        private const string SchemaVersion = "el_pathfinding.v1";
        private const string DefaultDirectoryName = "Arcontio_EL_Pathfinding";

        private static string _resolvedPath = string.Empty;
        private static string _resolvedPattern = string.Empty;
        private static bool _hasReportedWriteError;

        // =============================================================================
        // TryWriteIntent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive una riga JSONL per una <see cref="MovementIntentTrace"/>. La funzione
        /// accetta null e configurazioni spente come no-op, cosi' l'emitter puo'
        /// chiamarla senza duplicare controlli di sicurezza.
        /// </para>
        /// </summary>
        public static void TryWriteIntent(MovementExplainabilityParams config, MovementIntentTrace trace)
        {
            if (trace == null)
                return;

            TryWriteRecord(config, new MovementExplainabilityJsonLogRecord
            {
                schema = SchemaVersion,
                kind = "intent",
                npcId = trace.NpcId,
                tick = trace.Tick,
                intentId = trace.IntentId,
                planId = 0,
                intent = trace,
            });
        }

        // =============================================================================
        // TryWritePlan
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive una riga JSONL per una <see cref="PathPlanTrace"/>. La trace resta
        /// un export diagnostico: non viene trasformata in input per il planner.
        /// </para>
        /// </summary>
        public static void TryWritePlan(MovementExplainabilityParams config, PathPlanTrace trace)
        {
            if (trace == null)
                return;

            TryWriteRecord(config, new MovementExplainabilityJsonLogRecord
            {
                schema = SchemaVersion,
                kind = "plan",
                npcId = trace.NpcId,
                tick = trace.Tick,
                intentId = trace.IntentId,
                planId = trace.PlanId,
                plan = trace,
            });
        }

        // =============================================================================
        // TryWriteExecutionEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Scrive una riga JSONL per un <see cref="PathExecutionEvent"/>. Gli eventi
        /// sono gia' filtrati dalla verbosita' nell'emitter; il sink non applica una
        /// seconda policy comportamentale.
        /// </para>
        /// </summary>
        public static void TryWriteExecutionEvent(MovementExplainabilityParams config, PathExecutionEvent evt)
        {
            if (evt == null)
                return;

            TryWriteRecord(config, new MovementExplainabilityJsonLogRecord
            {
                schema = SchemaVersion,
                kind = "event",
                npcId = evt.NpcId,
                tick = evt.Tick,
                intentId = evt.IntentId,
                planId = evt.PlanId,
                executionEvent = evt,
            });
        }

        // =============================================================================
        // TryWriteRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Appende il record al file JSONL corrente. Gli errori di IO vengono assorbiti
        /// e riportati una sola volta tramite Debug.LogWarning, perche' il logging non
        /// deve mai interrompere la simulazione.
        /// </para>
        /// </summary>
        private static void TryWriteRecord(MovementExplainabilityParams config, MovementExplainabilityJsonLogRecord record)
        {
            if (config == null || !config.writeJsonLog || record == null)
                return;

            string path = ResolveLogPath(config);
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                string json = JsonUtility.ToJson(record, prettyPrint: false);
                File.AppendAllText(path, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                if (_hasReportedWriteError)
                    return;

                _hasReportedWriteError = true;
                Debug.LogWarning($"[EL Pathfinding] JSONL log write failed: {ex.Message}");
            }
        }

        // =============================================================================
        // ResolveLogPath
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve una sola volta il percorso del file JSONL per la sessione runtime.
        /// Il pattern puo' contenere il token <c>{yyyyMMdd_HHmmss}</c>, sostituito con
        /// il timestamp locale di creazione del sink.
        /// </para>
        /// </summary>
        private static string ResolveLogPath(MovementExplainabilityParams config)
        {
            string pattern = string.IsNullOrWhiteSpace(config.jsonLogFileNamePattern)
                ? "arcontio_el_pathfinding_{yyyyMMdd_HHmmss}.jsonl"
                : config.jsonLogFileNamePattern;

            if (!string.IsNullOrWhiteSpace(_resolvedPath) && string.Equals(_resolvedPattern, pattern, StringComparison.Ordinal))
                return _resolvedPath;

            string safeFileName = pattern.Replace("{yyyyMMdd_HHmmss}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            if (!safeFileName.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                safeFileName += ".jsonl";

            string directory = Path.Combine(Application.persistentDataPath, DefaultDirectoryName);
            Directory.CreateDirectory(directory);

            _resolvedPattern = pattern;
            _resolvedPath = Path.Combine(directory, safeFileName);
            _hasReportedWriteError = false;

            return _resolvedPath;
        }

        // =============================================================================
        // MovementExplainabilityJsonLogRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// DTO serializzabile della riga JSONL. Contiene un envelope comune e una sola
        /// payload principale valorizzata tra intent, plan ed executionEvent.
        /// </para>
        /// </summary>
        [Serializable]
        private sealed class MovementExplainabilityJsonLogRecord
        {
            public string schema = string.Empty;
            public string kind = string.Empty;
            public int npcId;
            public long tick;
            public int intentId;
            public int planId;
            public MovementIntentTrace intent;
            public PathPlanTrace plan;
            public PathExecutionEvent executionEvent;
        }
    }
}
