using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MemoryBeliefDecisionExplainabilityEmitter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Emitter statico minimale per trasformare eventi Memory/Belief gia' avvenuti
    /// in snapshot EL-MBD append-only.
    /// </para>
    ///
    /// <para><b>Emitter one-way senza accesso globale</b></para>
    /// <para>
    /// L'emitter non cerca dati nel <c>World</c>, non interroga <c>MemoryStore</c> e
    /// non modifica <c>BeliefStore</c>. Riceve trace, esito dello store o belief gia'
    /// aggiornato dai sistemi proprietari e li inoltra al sink JSONL.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryWriteMemoryTrace</b>: esporta la trace accettata, rinforzata o droppata.</item>
    ///   <item><b>TryWriteBeliefTrace</b>: esporta il belief risultante da aggregazione o feedback.</item>
    ///   <item><b>ToBeliefRef</b>: copia campi primitivi evitando riferimenti live allo store.</item>
    /// </list>
    /// </summary>
    public static class MemoryBeliefDecisionExplainabilityEmitter
    {
        // =============================================================================
        // TryWriteMemoryTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta un record EL per una <c>MemoryTrace</c> appena processata dal
        /// <c>MemoryStore</c>.
        /// </para>
        ///
        /// <para><b>Memoria come ingresso soggettivo</b></para>
        /// <para>
        /// Il record conserva tipo, soggetto, cella e qualita' della traccia, piu'
        /// l'esito di <c>AddOrMerge</c>. In questo modo un log runtime puo' distinguere
        /// memoria inserita, rinforzata, rimpiazzata o scartata.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Config</b>: no-op quando EL o JSONL sono disabilitati.</item>
        ///   <item><b>Trace</b>: copia solo dati gia' presenti nella memoria.</item>
        ///   <item><b>StoreResult</b>: collega la trace all'esito del MemoryStore.</item>
        /// </list>
        /// </summary>
        public static void TryWriteMemoryTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            int npcId,
            long tick,
            in MemoryTrace trace,
            AddOrMergeResult storeResult,
            string eventType)
        {
            if (config == null)
                return;

            MemoryBeliefDecisionJsonLogSink.TryWriteTrace(config, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Memory,
                Tick = tick,
                NpcId = npcId,
                Memory = new MemoryBeliefDecisionMemoryTraceRecord
                {
                    EventType = eventType ?? string.Empty,
                    TraceType = trace.Type,
                    SubjectId = trace.SubjectId,
                    SecondarySubjectId = trace.SecondarySubjectId,
                    SubjectDefId = trace.SubjectDefId ?? string.Empty,
                    Cell = new Vector2Int(trace.CellX, trace.CellY),
                    Intensity01 = trace.Intensity01,
                    Reliability01 = trace.Reliability01,
                    IsHeard = trace.IsHeard,
                    HeardKind = trace.HeardKind,
                    SourceSpeakerId = trace.SourceSpeakerId,
                    StoreResult = storeResult,
                },
            });
        }

        // =============================================================================
        // TryWriteBeliefTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esporta un record EL per una mutazione del BeliefStore appena completata.
        /// </para>
        ///
        /// <para><b>Belief come risultato, non come sorgente logica</b></para>
        /// <para>
        /// Il metodo riceve il belief gia' trovato dal chiamante e lo copia in un
        /// riferimento serializzabile. Non decide se la mutazione sia corretta e non
        /// rilegge la lista delle credenze.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Operation</b>: creazione, merge, rinforzo o indebolimento.</item>
        ///   <item><b>SourceTrace</b>: conserva il tipo della memory che ha alimentato il belief.</item>
        ///   <item><b>Reason</b>: stringa breve per correlare regola o feedback operativo.</item>
        /// </list>
        /// </summary>
        public static void TryWriteBeliefTrace(
            MemoryBeliefDecisionExplainabilityParams config,
            int npcId,
            long tick,
            MemoryBeliefDecisionBeliefOperation operation,
            in MemoryTrace sourceTrace,
            BeliefEntry belief,
            string reason)
        {
            if (config == null)
                return;

            MemoryBeliefDecisionJsonLogSink.TryWriteTrace(config, new MemoryBeliefDecisionTrace
            {
                Kind = MemoryBeliefDecisionTraceKind.Belief,
                Tick = tick,
                NpcId = npcId,
                Belief = new MemoryBeliefDecisionBeliefRecord
                {
                    Operation = operation,
                    HasSourceTrace = true,
                    SourceTraceType = sourceTrace.Type,
                    Belief = ToBeliefRef(belief),
                    Reason = reason ?? string.Empty,
                },
            });
        }

        // =============================================================================
        // ToBeliefRef
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una <c>BeliefEntry</c> in snapshot EL serializzabile.
        /// </para>
        ///
        /// <para><b>Nessun riferimento live allo store</b></para>
        /// <para>
        /// La copia contiene solo valori primitivi e la posizione stimata. Il file
        /// JSONL non puo' quindi diventare un canale alternativo per leggere o mutare
        /// la struttura interna del BeliefStore.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Categoria/status/source</b>: identita' semantica e stato operativo.</item>
        ///   <item><b>BeliefId</b>: id locale per-NPC.</item>
        ///   <item><b>Qualita'</b>: confidence, freshness e source count.</item>
        /// </list>
        /// </summary>
        private static MemoryBeliefDecisionBeliefRef ToBeliefRef(BeliefEntry belief)
        {
            return new MemoryBeliefDecisionBeliefRef
            {
                Category = belief.Category,
                Status = belief.Status,
                Source = belief.Source,
                BeliefId = belief.BeliefId,
                EstimatedPosition = belief.EstimatedPosition,
                Confidence = belief.Confidence,
                Freshness = belief.Freshness,
                SourceCount = belief.SourceCount,
            };
        }
    }
}
