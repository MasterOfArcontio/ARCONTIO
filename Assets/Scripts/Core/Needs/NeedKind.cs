namespace Arcontio.Core
{
    /// <summary>
    /// Tipi di bisogno primario degli NPC.
    ///
    /// Attivazione per sessione:
    ///   Sessione 08 — Hunger, Thirst, Rest    (decay attivo)
    ///   Sessione 09 — Health, Comfort         (decay attivo)
    ///   Sessione 10 — Security, Stability, Sociality (decay attivo)
    ///
    /// In questa sessione (07) vengono definiti tutti e 8, ma solo Hunger e Rest
    /// hanno un decay rate configurato in NeedsConfig. Gli altri rimangono a 0.
    /// </summary>
    public enum NeedKind
    {
        Hunger    = 0,
        Thirst    = 1,
        Rest      = 2,
        Health    = 3,
        Comfort   = 4,
        Security  = 5,
        Stability = 6,
        Sociality = 7,
        COUNT     = 8
    }

    /// <summary>
    /// Stato runtime di un singolo bisogno.
    ///
    /// I flag IsAlert e IsCritical sono valori derivati:
    /// vengono settati da NeedsDecaySystem ogni tick confrontando Value01
    /// con NpcThresholds.NeedAlert01 e NeedCritical01 del DNA dell'NPC.
    /// Non serializzarli — vengono ricalcolati al caricamento.
    /// </summary>
    public struct NeedState
    {
        /// <summary>0 = bisogno soddisfatto, 1 = bisogno al limite critico.</summary>
        public float Value01;

        /// <summary>True se Value01 >= NpcThresholds.NeedAlert01 del DNA.</summary>
        public bool IsAlert;

        /// <summary>True se Value01 >= NpcThresholds.NeedCritical01 del DNA.</summary>
        public bool IsCritical;
    }

    /// <summary>
    /// Componente runtime per-NPC: array di NeedState indicizzato per NeedKind.
    ///
    /// Sostituisce la vecchia struct Needs (Hunger01 / Fatigue01 / Morale01).
    ///
    /// Nota sull'aliasing dell'array:
    ///   States è un array (reference type) dentro una struct.
    ///   Il pattern esistente (TryGetValue → modifica → riassegna al dictionary) rimane
    ///   valido perché l'array è condiviso per riferimento tra copie della struct.
    ///   Usare sempre i metodi helper (GetValue, AddValue, SetValue, SetFlags)
    ///   per modificare gli elementi in modo sicuro (struct NeedState è value type,
    ///   quindi l'accesso diretto via States[i].Campo non modifica l'array).
    /// </summary>
    public struct NpcNeeds
    {
        public NeedState[] States;

        /// <summary>Crea un NpcNeeds con tutti i bisogni a 0 e flag false.</summary>
        public static NpcNeeds Default()
        {
            return new NpcNeeds { States = new NeedState[(int)NeedKind.COUNT] };
        }

        // ── Lettura ────────────────────────────────────────────────────────────

        public float GetValue(NeedKind k)
            => States != null ? States[(int)k].Value01 : 0f;

        public bool IsAlert(NeedKind k)
            => States != null && States[(int)k].IsAlert;

        public bool IsCritical(NeedKind k)
            => States != null && States[(int)k].IsCritical;

        // ── Scrittura ──────────────────────────────────────────────────────────

        /// <summary>Somma delta al valore, risultato clampato a [0, 1].</summary>
        public void AddValue(NeedKind k, float delta)
        {
            if (States == null) return;
            var s = States[(int)k];
            float v = s.Value01 + delta;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            s.Value01 = v;
            States[(int)k] = s;
        }

        /// <summary>Imposta il valore, clampato a [0, 1].</summary>
        public void SetValue(NeedKind k, float value01)
        {
            if (States == null) return;
            var s = States[(int)k];
            float v = value01;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            s.Value01 = v;
            States[(int)k] = s;
        }

        /// <summary>Aggiorna i flag IsAlert/IsCritical per un singolo bisogno.</summary>
        public void SetFlags(NeedKind k, bool isAlert, bool isCritical)
        {
            if (States == null) return;
            var s = States[(int)k];
            s.IsAlert    = isAlert;
            s.IsCritical = isCritical;
            States[(int)k] = s;
        }
    }
}
