using System.Collections.Generic;
using Arcontio.Core.Config;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // ReservationTargetKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo di risorsa che un job puo' prenotare prima di eseguire uno step.
    /// </para>
    ///
    /// <para><b>Reservation layer indipendente dagli oggetti concreti</b></para>
    /// <para>
    /// Prenotare una cella, un oggetto o uno stock sono operazioni simili ma non
    /// identiche. Questa enum permette allo store di trattarle in modo uniforme
    /// senza conoscere inventario, pathfinding o needs.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: prenotazione di una cella della griglia.</item>
    ///   <item><b>Object</b>: prenotazione di un WorldObjectInstance.</item>
    ///   <item><b>Stock</b>: prenotazione logica di una risorsa consumabile.</item>
    ///   <item><b>Custom</b>: estensione per domini futuri.</item>
    /// </list>
    /// </summary>
    public enum ReservationTargetKind
    {
        None = 0,
        Cell = 10,
        Object = 20,
        Stock = 30,
        Custom = 999
    }

    // =============================================================================
    // ReservationRecord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Record data-puro che descrive una risorsa temporaneamente assegnata a un job.
    /// </para>
    ///
    /// <para><b>Contesa esplicita</b></para>
    /// <para>
    /// Due NPC non devono dedurre implicitamente che una risorsa e' occupata. Un
    /// record esplicito rende testabile chi ha prenotato cosa e fino a quando.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ReservationId</b>: chiave stabile dello store.</item>
    ///   <item><b>JobId/NpcId</b>: proprietario operativo della prenotazione.</item>
    ///   <item><b>TargetKind</b>: categoria della risorsa prenotata.</item>
    ///   <item><b>TargetCell/TargetObjectId</b>: identificatori opzionali della risorsa.</item>
    ///   <item><b>CreatedTick/ExpiresTick</b>: finestra temporale della prenotazione.</item>
    /// </list>
    /// </summary>
    public readonly struct ReservationRecord
    {
        public readonly string ReservationId;
        public readonly string JobId;
        public readonly int NpcId;
        public readonly ReservationTargetKind TargetKind;
        public readonly Vector2Int TargetCell;
        public readonly int TargetObjectId;
        public readonly int CreatedTick;
        public readonly int ExpiresTick;

        public ReservationRecord(
            string reservationId,
            string jobId,
            int npcId,
            ReservationTargetKind targetKind,
            Vector2Int targetCell,
            int targetObjectId,
            int createdTick,
            int expiresTick)
        {
            ReservationId = string.IsNullOrWhiteSpace(reservationId) ? BuildKey(targetKind, targetCell, targetObjectId) : reservationId;
            JobId = jobId ?? string.Empty;
            NpcId = npcId;
            TargetKind = targetKind;
            TargetCell = targetCell;
            TargetObjectId = targetObjectId;
            CreatedTick = createdTick;
            ExpiresTick = expiresTick < createdTick ? createdTick : expiresTick;
        }

        public bool IsExpiredAt(int tick)
        {
            // La prenotazione e' valida fino al tick precedente a ExpiresTick; al
            // tick di scadenza puo' essere rimossa o sostituita.
            return tick >= ExpiresTick;
        }

        public bool MatchesTarget(ReservationTargetKind kind, Vector2Int cell, int objectId)
        {
            // La chiave logica dipende dal tipo target: per celle conta la cella, per
            // oggetti e stock conta l'id oggetto.
            if (TargetKind != kind)
                return false;

            if (kind == ReservationTargetKind.Cell)
                return TargetCell == cell;

            return TargetObjectId == objectId;
        }

        public static string BuildKey(ReservationTargetKind kind, Vector2Int cell, int objectId)
        {
            // La chiave resta leggibile nei log e stabile nei test.
            return kind == ReservationTargetKind.Cell
                ? "cell:" + cell.x + ":" + cell.y
                : kind.ToString().ToLowerInvariant() + ":" + objectId;
        }
    }

    // =============================================================================
    // ReservationStore
    // =============================================================================
    /// <summary>
    /// <para>
    /// Store minimale delle prenotazioni attive del Job System.
    /// </para>
    ///
    /// <para><b>Risorse condivise senza accesso diretto globale</b></para>
    /// <para>
    /// Lo store non conosce il World. Riceve record gia' risolti e risponde solo a
    /// domande di contesa: posso prenotare, chi possiede, cosa e' scaduto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_records</b>: mappa reservation id -> record.</item>
    ///   <item><b>TryReserve</b>: inserisce solo se il target e' libero o posseduto dallo stesso job.</item>
    ///   <item><b>ReleaseByJob</b>: rimuove tutte le prenotazioni di un job.</item>
    ///   <item><b>PruneExpired</b>: pulizia deterministica per tick.</item>
    /// </list>
    /// </summary>
    public sealed class ReservationStore
    {
        private readonly Dictionary<string, ReservationRecord> _records = new();

        public int Count => _records.Count;

        // =============================================================================
        // TryReserve
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload EL-aware che rende osservabile l'esito della reservation senza
        /// cambiare il contratto base dello store.
        /// </para>
        /// </summary>
        public bool TryReserve(
            ReservationRecord record,
            out ReservationRecord existing,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int tick,
            int npcId)
        {
            bool accepted = TryReserve(record, out existing);

            if (explainabilityConfig != null)
            {
                var traceRecord = accepted ? record : existing;
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteReservationTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    accepted ? MemoryBeliefDecisionReservationOperation.Accepted : MemoryBeliefDecisionReservationOperation.Denied,
                    in traceRecord,
                    accepted ? "ReservationAccepted" : "ReservationDenied");
            }

            return accepted;
        }

        public bool TryReserve(ReservationRecord record, out ReservationRecord existing)
        {
            // Prima cerchiamo una contesa sullo stesso target logico, non solo sulla
            // stessa stringa id: due chiamanti potrebbero costruire id diversi.
            foreach (var pair in _records)
            {
                var current = pair.Value;
                if (!current.MatchesTarget(record.TargetKind, record.TargetCell, record.TargetObjectId))
                    continue;

                if (current.JobId == record.JobId)
                {
                    existing = current;
                    _records[record.ReservationId] = record;
                    return true;
                }

                existing = current;
                return false;
            }

            _records[record.ReservationId] = record;
            existing = default;
            return true;
        }

        public bool TryGet(string reservationId, out ReservationRecord record)
        {
            // Lettura diretta per debug e test: nessuna creazione implicita.
            return _records.TryGetValue(reservationId, out record);
        }

        public int ReleaseByJob(string jobId)
        {
            // Raccogliamo prima le chiavi da rimuovere per non mutare il dizionario
            // durante l'enumerazione.
            var keys = new List<string>();
            foreach (var pair in _records)
            {
                if (pair.Value.JobId == jobId)
                    keys.Add(pair.Key);
            }

            for (var i = 0; i < keys.Count; i++)
                _records.Remove(keys[i]);

            return keys.Count;
        }

        // =============================================================================
        // ReleaseByJob
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload EL-aware che emette una trace per ogni reservation rilasciata dal
        /// job indicato.
        /// </para>
        /// </summary>
        public int ReleaseByJob(
            string jobId,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry,
            int tick,
            int npcId)
        {
            var released = new List<ReservationRecord>();
            foreach (var pair in _records)
            {
                if (pair.Value.JobId == jobId)
                    released.Add(pair.Value);
            }

            int count = ReleaseByJob(jobId);
            if (explainabilityConfig == null)
                return count;

            for (int i = 0; i < released.Count; i++)
            {
                var reservation = released[i];
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteReservationTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    npcId,
                    tick,
                    MemoryBeliefDecisionReservationOperation.Released,
                    in reservation,
                    "ReservationReleased");
            }

            return count;
        }

        public int PruneExpired(int tick)
        {
            // La pulizia per tick mantiene lo store piccolo e rende le scadenze
            // verificabili senza timer nascosti.
            var keys = new List<string>();
            foreach (var pair in _records)
            {
                if (pair.Value.IsExpiredAt(tick))
                    keys.Add(pair.Key);
            }

            for (var i = 0; i < keys.Count; i++)
                _records.Remove(keys[i]);

            return keys.Count;
        }

        // =============================================================================
        // PruneExpired
        // =============================================================================
        /// <summary>
        /// <para>
        /// Overload EL-aware che emette una trace per ogni reservation scaduta.
        /// </para>
        /// </summary>
        public int PruneExpired(
            int tick,
            MemoryBeliefDecisionExplainabilityParams explainabilityConfig,
            MemoryBeliefDecisionExplainabilityRegistry explainabilityRegistry)
        {
            var expired = new List<ReservationRecord>();
            foreach (var pair in _records)
            {
                if (pair.Value.IsExpiredAt(tick))
                    expired.Add(pair.Value);
            }

            int count = PruneExpired(tick);
            if (explainabilityConfig == null)
                return count;

            for (int i = 0; i < expired.Count; i++)
            {
                var reservation = expired[i];
                MemoryBeliefDecisionExplainabilityEmitter.TryWriteReservationTrace(
                    explainabilityConfig,
                    explainabilityRegistry,
                    reservation.NpcId,
                    tick,
                    MemoryBeliefDecisionReservationOperation.Expired,
                    in reservation,
                    "ReservationExpired");
            }

            return count;
        }
    }
}
