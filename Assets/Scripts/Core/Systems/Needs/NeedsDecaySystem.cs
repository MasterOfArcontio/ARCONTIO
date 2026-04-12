using System.Collections.Generic;
using Arcontio.Core.Diagnostics;

namespace Arcontio.Core
{
    // =============================================================================
    // NeedsDecaySystem
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sistema runtime che applica il decay dei bisogni attivi e aggiorna i flag
    /// derivati <c>IsAlert</c> / <c>IsCritical</c> per ogni NPC.
    /// </para>
    ///
    /// <para><b>Separazione decisione/esecuzione</b></para>
    /// <para>
    /// Questo sistema non sceglie azioni, non crea job e non consulta memoria o
    /// world state oggettivo per dedurre intenzioni. Si limita ad aggiornare lo
    /// stato interno dei bisogni; la trasformazione del bisogno in decisione resta
    /// responsabilità delle rule e, in futuro, del Decision Layer tramite BeliefStore
    /// e QuerySystem.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Fisiologici rapidi</b>: Hunger, Thirst e Rest hanno decay diretto e più aggressivo.</item>
    ///   <item><b>Psicologici lenti</b>: Security, Stability e Sociality ricevono decay baseline più graduale.</item>
    ///   <item><b>Derivati futuri</b>: Health e Comfort non vengono aggiornati qui finché BodyWound e la formula comfort non sono specificati nel runtime.</item>
    ///   <item><b>Flag</b>: alert e critical sono ricalcolati per tutti i NeedKind usando le soglie DNA dell'NPC.</item>
    /// </list>
    /// </summary>
    public sealed class NeedsDecaySystem : ISystem
    {
        public int Period => 1;

        private readonly List<int> _npcIds = new(2048);

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue un tick di decay sui bisogni degli NPC presenti nel mondo e salva
        /// il risultato nello store <c>world.Needs</c>.
        /// </para>
        ///
        /// <para><b>Baseline psicologica senza onniscienza</b></para>
        /// <para>
        /// I nuovi decay psicologici sono volutamente interni e lenti: non leggono
        /// pericoli, alleati, ostili o stato globale. Quando BeliefStore e QuerySystem
        /// saranno attivi, sistemi dedicati potranno modulare questi valori usando
        /// solo conoscenza soggettiva dell'NPC.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Snapshot NPC</b>: copia gli id in un buffer riusabile per evitare allocazioni e mutazioni durante l'iterazione.</item>
        ///   <item><b>Decay rapido</b>: applica fame, sete e riposo tramite <c>ApplyFastPhysiologicalDecay</c>.</item>
        ///   <item><b>Decay lento</b>: applica sicurezza, stabilità e socialità tramite <c>ApplySlowPsychologicalDecay</c>.</item>
        ///   <item><b>Soglie</b>: recupera le soglie dal DNA o usa fallback conservativi.</item>
        ///   <item><b>Persistenza runtime</b>: riassegna la struct <c>NpcNeeds</c> al dizionario dopo l'aggiornamento.</item>
        /// </list>
        /// </summary>
        public void Update(World world, Tick tick, MessageBus bus, Telemetry telemetry)
        {
            if (world.NpcDna.Count == 0) return;

            var cfg = world.Global.Needs;

            _npcIds.Clear();
            _npcIds.AddRange(world.NpcDna.Keys);

            int updated = 0;

            for (int i = 0; i < _npcIds.Count; i++)
            {
                int npcId = _npcIds[i];
                if (!world.Needs.TryGetValue(npcId, out var n)) continue;
                if (n.States == null) continue;

                ApplyFastPhysiologicalDecay(ref n, cfg);
                ApplySlowPsychologicalDecay(ref n, cfg);

                // ── Flag IsAlert / IsCritical da soglie DNA ───────────────────
                // Legge NpcThresholds dal DNA; fallback a valori conservativi se il DNA manca.
                // I flag vengono aggiornati per TUTTI i NeedKind, compresi quelli a 0:
                // non scatteranno mai finché il valore rimane sotto la soglia.
                float alertThr    = 0.60f;
                float criticalThr = 0.85f;

                if (world.NpcDna.TryGetValue(npcId, out var dna))
                {
                    alertThr    = dna.Thresholds.NeedAlert01;
                    criticalThr = dna.Thresholds.NeedCritical01;
                }

                for (int k = 0; k < (int)NeedKind.COUNT; k++)
                {
                    float v = n.States[k].Value01;
                    n.SetFlags((NeedKind)k, v >= alertThr, v >= criticalThr);
                }

                world.Needs[npcId] = n;
                updated++;
            }

            telemetry.Counter("NeedsDecay.Updated", updated);
        }

        // =============================================================================
        // ApplyFastPhysiologicalDecay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il gruppo di decay rapido ai bisogni fisiologici diretti.
        /// </para>
        ///
        /// <para><b>Categoria rapida</b></para>
        /// <para>
        /// Fame, sete e riposo rappresentano pressioni corporee a scala breve:
        /// crescono abbastanza velocemente da generare cicli osservabili nella
        /// simulazione. La velocità concreta non è hardcoded qui, ma arriva da
        /// <c>NeedsConfig</c>, quindi resta modificabile da file.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Hunger</b>: usa <c>satietyDecayPerTick</c>.</item>
        ///   <item><b>Thirst</b>: usa <c>thirstDecayPerTick</c>, di norma più rapido della fame.</item>
        ///   <item><b>Rest</b>: usa <c>restDecayPerTick</c>, più lento ma comunque fisiologico.</item>
        /// </list>
        /// </summary>
        private static void ApplyFastPhysiologicalDecay(ref NpcNeeds needs, NeedsConfig cfg)
        {
            // Thirst possiede ancora recovery pendente, ma il valore cresce comunque
            // per tenere pronta la pressione fisiologica quando i water source arriveranno.
            needs.AddValue(NeedKind.Hunger, cfg.satietyDecayPerTick);
            needs.AddValue(NeedKind.Thirst, cfg.thirstDecayPerTick);
            needs.AddValue(NeedKind.Rest,   cfg.restDecayPerTick);
        }

        // =============================================================================
        // ApplySlowPsychologicalDecay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il gruppo di decay lento ai bisogni psicologici baseline.
        /// </para>
        ///
        /// <para><b>Categoria lenta senza onniscienza</b></para>
        /// <para>
        /// Sicurezza, stabilità e socialità crescono come pressioni interne lente.
        /// Questo metodo non legge danger belief, relazioni sociali, oggetti vicini
        /// o world state globale: quando quei segnali saranno disponibili, dovranno
        /// arrivare da sistemi soggettivi e non da accessi diretti.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Security</b>: usa <c>securityDecayPerTick</c> come baseline di insicurezza progressiva.</item>
        ///   <item><b>Stability</b>: usa <c>stabilityDecayPerTick</c> come erosione emotiva lenta.</item>
        ///   <item><b>Sociality</b>: usa <c>socialityDecayPerTick</c> come bisogno di contatto sociale.</item>
        /// </list>
        /// </summary>
        private static void ApplySlowPsychologicalDecay(ref NpcNeeds needs, NeedsConfig cfg)
        {
            // Non anticipiamo BeliefStore/QuerySystem: questi tre valori restano
            // baseline interne fino a quando i layer cognitivi non saranno pronti.
            needs.AddValue(NeedKind.Security,  cfg.securityDecayPerTick);
            needs.AddValue(NeedKind.Stability, cfg.stabilityDecayPerTick);
            needs.AddValue(NeedKind.Sociality, cfg.socialityDecayPerTick);
        }
    }
}
