using System.Collections.Generic;

namespace Arcontio.Core
{
    /// <summary>
    /// DebugNpcTokenLog (Patch 0.01P2)
    ///
    /// Cache DIAGNOSTICA (non gameplay) per rendere osservabile in UI
    /// il flusso di TokenEnvelope IN/OUT per un NPC.
    ///
    /// Perché serve:
    /// - TokenBus è effimero: i token "spariscono" dopo il tick.
    /// - La card UI deve poter mostrare "cosa ho detto" e "cosa ho sentito"
    ///   senza agganciarsi alle code runtime (che cambiano frame-to-frame).
    ///
    /// Vincoli:
    /// - Bounded (cap massimo) per evitare crescita indefinita.
    /// - Zero side-effect sulla simulazione (solo debug).
    /// </summary>
    public sealed class DebugNpcTokenLog
    {
        /// <summary>
        /// Snapshot "leggero" di un TokenEnvelope, modellato esattamente
        /// come si aspetta la UI (MapGridEntitySummaryOverlay):
        /// - env.Token
        /// - env.SpeakerId / env.ListenerId
        /// - env.Channel
        /// </summary>
        public struct Entry
        {
            public int SpeakerId;
            public int ListenerId;
            public TokenChannel Channel;
            public long TickIndex;
            public SymbolicToken Token;
        }

        private readonly List<Entry> _incoming = new();
        private readonly List<Entry> _outgoing = new();

        /// <summary>
        /// Cap massimo per direzione (IN e OUT).
        /// </summary>
        public int MaxEntriesPerDirection { get; set; } = 16;

        /// <summary>
        /// Lista tokens IN (arrivati al listener).
        /// La UI legge .Incoming.
        /// </summary>
        public IReadOnlyList<Entry> Incoming => _incoming;

        /// <summary>
        /// Lista tokens OUT (emessi dallo speaker).
        /// La UI legge .Outgoing.
        /// </summary>
        public IReadOnlyList<Entry> Outgoing => _outgoing;

        /// <summary>
        /// Chiamata dal TokenDeliveryPipeline:
        /// registra un token effettivamente arrivato al listener (IN).
        /// Firma attesa dai call-site della Patch 0.01P2: 1 parametro.
        /// </summary>
        public void RecordIncoming(TokenEnvelope env)
        {
            PushBounded(_incoming, new Entry
            {
                SpeakerId = env.SpeakerId,
                ListenerId = env.ListenerId,
                Channel = env.Channel,
                TickIndex = env.TickIndex,
                Token = env.Token
            });
        }

        /// <summary>
        /// Chiamata dal TokenEmissionPipeline:
        /// registra un token emesso dallo speaker (OUT).
        /// Firma attesa dai call-site della Patch 0.01P2: 1 parametro.
        /// </summary>
        public void RecordOutgoing(TokenEnvelope env)
        {
            PushBounded(_outgoing, new Entry
            {
                SpeakerId = env.SpeakerId,
                ListenerId = env.ListenerId,
                Channel = env.Channel,
                TickIndex = env.TickIndex,
                Token = env.Token
            });
        }

        /// <summary>
        /// Inserimento FIFO bounded:
        /// - aggiunge in coda
        /// - se supera cap, rimuove il più vecchio (indice 0)
        /// </summary>
        private void PushBounded(List<Entry> list, Entry entry)
        {
            list.Add(entry);

            if (list.Count > MaxEntriesPerDirection)
            {
                list.RemoveAt(0);
            }
        }

        /// <summary>
        /// Reset completo (solo debug).
        /// </summary>
        public void Clear()
        {
            _incoming.Clear();
            _outgoing.Clear();
        }
    }
}