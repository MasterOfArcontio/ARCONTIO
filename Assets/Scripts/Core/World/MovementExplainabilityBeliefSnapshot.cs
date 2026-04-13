using System;

namespace Arcontio.Core
{
    // =============================================================================
    // MovementExplainabilityBeliefSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper di conversione tra risultati del QuerySystem belief e snapshot
    /// <see cref="BeliefEntryRef"/> usabili dall'Explainability Layer pathfinding.
    /// </para>
    ///
    /// <para><b>Causalita' esplicita, non ricostruzione a posteriori</b></para>
    /// <para>
    /// Questo helper non interroga <see cref="BeliefStore"/> e non sceglie una belief:
    /// riceve una <see cref="BeliefEntry"/> o un <see cref="BeliefQueryResult"/> gia'
    /// selezionato dal Decision Layer e ne produce una copia minimale. In questo modo
    /// il movimento puo' trasportare la causa diagnostica verso l'EL senza trasformare
    /// il pathfinding in un lettore del BeliefStore.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TryFromQueryResult</b>: converte il risultato non vuoto di una query.</item>
    ///   <item><b>FromBeliefEntry</b>: converte una entry gia' scelta dal chiamante.</item>
    ///   <item><b>AgeTicks</b>: calcolato come differenza difensiva tra tick corrente e ultimo aggiornamento.</item>
    /// </list>
    /// </summary>
    public static class MovementExplainabilityBeliefSnapshot
    {
        // =============================================================================
        // TryFromQueryResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un <see cref="BeliefQueryResult"/> non vuoto in una snapshot EL.
        /// Restituisce false quando la query non ha prodotto alcuna belief utilizzabile.
        /// </para>
        ///
        /// <para><b>Adapter QuerySystem -> EL</b></para>
        /// <para>
        /// Il metodo stabilisce il ponte previsto dalla sessione G: il Decision Layer
        /// potra' usare il QuerySystem e poi passare il risultato al movimento come
        /// snapshot, evitando che l'EL debba indovinare la causa in un secondo momento.
        /// </para>
        /// </summary>
        public static bool TryFromQueryResult(
            in BeliefQueryResult queryResult,
            long currentTick,
            out BeliefEntryRef beliefRef)
        {
            if (queryResult.IsEmpty)
            {
                beliefRef = default;
                return false;
            }

            beliefRef = FromBeliefEntry(queryResult.Belief, currentTick);
            return true;
        }

        // =============================================================================
        // FromBeliefEntry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia una <see cref="BeliefEntry"/> gia' selezionata in un
        /// <see cref="BeliefEntryRef"/> diagnostico, calcolando l'eta' della belief in
        /// modo bounded e senza conservare reference live allo store.
        /// </para>
        /// </summary>
        public static BeliefEntryRef FromBeliefEntry(in BeliefEntry belief, long currentTick)
        {
            long ageTicks = Math.Max(0L, currentTick - belief.LastUpdatedTick);

            return new BeliefEntryRef
            {
                Category = belief.Category,
                BeliefId = belief.BeliefId,
                EntityId = 0,
                Confidence = belief.Confidence,
                Freshness = belief.Freshness,
                AgeTicks = ageTicks,
            };
        }
    }
}
