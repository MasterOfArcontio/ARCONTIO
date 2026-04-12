using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // BeliefCategory
    // =============================================================================
    /// <summary>
    /// <para>
    /// Categoria semantica di una credenza soggettiva conservata nel futuro
    /// BeliefStore per-NPC. La categoria indica a quale dominio cognitivo appartiene
    /// la credenza, senza dichiararla vera: in ARCONTIO un belief è sempre un'ipotesi
    /// operativa derivata da percezione, memoria o inferenza.
    /// </para>
    ///
    /// <para><b>Vincolo di Onniscienza</b></para>
    /// <para>
    /// Questa enum non dà accesso allo stato oggettivo del mondo. Serve solo a
    /// classificare informazioni già entrate nel percorso soggettivo
    /// Perception → Memory → Belief, così il Decision Layer futuro potrà passare
    /// dal QuerySystem senza interrogare direttamente il world state.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Food</b>: fonti di cibo note o credute.</item>
    ///   <item><b>Rest</b>: luoghi per dormire o riposare.</item>
    ///   <item><b>Danger</b>: aree o entità percepite come pericolose.</item>
    ///   <item><b>Social</b>: credenze su NPC, relazioni, affidabilità o ostilità.</item>
    ///   <item><b>Ownership</b>: credenze su proprietà e accesso alle risorse.</item>
    ///   <item><b>Situation</b>: credenze aggregate su scarsità, crisi o disordine.</item>
    ///   <item><b>Structure</b>: credenze su strutture permanenti, porte, muri o stanze.</item>
    /// </list>
    /// </summary>
    public enum BeliefCategory
    {
        Food,
        Rest,
        Danger,
        Social,
        Ownership,
        Situation,
        Structure
    }

    // =============================================================================
    // BeliefStatus
    // =============================================================================
    /// <summary>
    /// <para>
    /// Stato operativo di una credenza nel futuro BeliefStore. Lo status rappresenta
    /// la qualità corrente della credenza, non la verità oggettiva del contenuto.
    /// </para>
    ///
    /// <para><b>Credenze come ipotesi operative</b></para>
    /// <para>
    /// Il documento architetturale BeliefStore/QuerySystem stabilisce che i belief
    /// devono poter essere sbagliati, vecchi o contraddittori. Questa enum conserva
    /// tale informazione in forma esplicita, lasciando la valutazione e il ranking
    /// al QuerySystem degli step successivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Active</b>: credenza valida e utilizzabile dal futuro Decision Layer tramite QuerySystem.</item>
    ///   <item><b>Weak</b>: confidence sotto soglia minima ma ancora presente, usabile eventualmente in emergenza.</item>
    ///   <item><b>Conflicted</b>: credenza contraddetta da una o più tracce di qualità simile.</item>
    ///   <item><b>Stale</b>: credenza troppo vecchia, non rimossa ma non prioritaria.</item>
    ///   <item><b>Discarded</b>: credenza invalidata definitivamente, da rimuovere nel futuro pruning.</item>
    /// </list>
    /// </summary>
    public enum BeliefStatus
    {
        Active,
        Weak,
        Conflicted,
        Stale,
        Discarded
    }

    // =============================================================================
    // BeliefSource
    // =============================================================================
    /// <summary>
    /// <para>
    /// Fonte principale da cui deriva una credenza. La fonte è parte della qualità
    /// cognitiva della credenza: una vista diretta, una testimonianza e un'inferenza
    /// non devono avere lo stesso peso nelle fasi future di aggregazione e query.
    /// </para>
    ///
    /// <para><b>Gerarchia delle fonti soggettive</b></para>
    /// <para>
    /// Questa enum prepara il terreno alla gerarchia descritta nel documento
    /// BeliefStore/QuerySystem, ma non implementa ancora alcuna regola di priorità.
    /// La risoluzione dei conflitti appartiene al futuro BeliefUpdater.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Seen</b>: informazione vista direttamente dall'NPC.</item>
    ///   <item><b>Heard</b>: informazione ricevuta tramite comunicazione o rumor.</item>
    ///   <item><b>Inferred</b>: informazione dedotta da tracce o eventi indiretti.</item>
    /// </list>
    /// </summary>
    public enum BeliefSource
    {
        Seen,
        Heard,
        Inferred
    }

    // =============================================================================
    // BeliefEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Singola credenza normalizzata di un NPC. Una <c>BeliefEntry</c> non è una
    /// verità globale: è una rappresentazione soggettiva sintetica che il futuro
    /// BeliefStore conserverà per categoria e status.
    /// </para>
    ///
    /// <para><b>BeliefStore passivo</b></para>
    /// <para>
    /// Questa struttura contiene solo dati. Non interroga il mondo, non decide se
    /// una credenza sia migliore di un'altra, non filtra candidati e non applica
    /// decay. Tali responsabilità appartengono agli step successivi:
    /// BeliefUpdater, BeliefStore, decay confidence e QuerySystem.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BeliefId</b>: identificatore univoco per-NPC della credenza.</item>
    ///   <item><b>Category</b>: categoria semantica della credenza.</item>
    ///   <item><b>EstimatedPosition</b>: posizione stimata dell'entità, risorsa o area.</item>
    ///   <item><b>Confidence</b>: sicurezza soggettiva della credenza, nel range previsto 0-1.</item>
    ///   <item><b>Freshness</b>: freschezza dell'informazione, nel range previsto 0-1.</item>
    ///   <item><b>LastUpdatedTick</b>: tick dell'ultimo aggiornamento della credenza.</item>
    ///   <item><b>SourceCount</b>: numero di tracce MemoryStore che contribuiscono alla credenza.</item>
    ///   <item><b>Source</b>: origine principale della credenza.</item>
    ///   <item><b>Status</b>: stato operativo corrente della credenza.</item>
    /// </list>
    /// </summary>
    public struct BeliefEntry
    {
        /// <summary>
        /// Identificatore univoco della credenza all'interno del BeliefStore del singolo NPC.
        /// Non è globale: due NPC diversi potranno usare lo stesso numero per credenze diverse.
        /// </summary>
        public int BeliefId;

        /// <summary>
        /// Categoria semantica della credenza, usata dal futuro BeliefStore per esporre
        /// query banali per categoria e status senza incorporare logica decisionale.
        /// </summary>
        public BeliefCategory Category;

        /// <summary>
        /// Posizione stimata dell'entità, risorsa o area a cui si riferisce la credenza.
        /// È una stima soggettiva: può essere vecchia, sbagliata o contraddetta da nuove tracce.
        /// </summary>
        public Vector2Int EstimatedPosition;

        /// <summary>
        /// Quanto l'NPC è sicuro della credenza. Il documento definisce il range come 0-1;
        /// la normalizzazione effettiva verrà applicata dai sistemi che creano o aggiornano il belief.
        /// </summary>
        public float Confidence;

        /// <summary>
        /// Quanto è recente l'informazione. Nel documento decade più rapidamente della Confidence,
        /// ma il decadimento concreto appartiene a uno step successivo.
        /// </summary>
        public float Freshness;

        /// <summary>
        /// Tick dell'ultimo aggiornamento della credenza. Serve ai sistemi futuri per calcolare
        /// decay, freshness e priorità temporale senza interrogare il mondo globale.
        /// </summary>
        public int LastUpdatedTick;

        /// <summary>
        /// Numero di tracce MemoryStore che contribuiscono a questa credenza sintetica.
        /// Non contiene le tracce stesse: conserva solo il conteggio richiesto dal documento.
        /// </summary>
        public int SourceCount;

        /// <summary>
        /// Origine principale della credenza: vista, sentita o inferita.
        /// La priorità effettiva tra fonti verrà gestita dal futuro BeliefUpdater.
        /// </summary>
        public BeliefSource Source;

        /// <summary>
        /// Stato operativo della credenza: attiva, debole, conflittuale, vecchia o scartata.
        /// L'interpretazione di questo valore resta responsabilità dei futuri sistemi di query.
        /// </summary>
        public BeliefStatus Status;
    }
}
