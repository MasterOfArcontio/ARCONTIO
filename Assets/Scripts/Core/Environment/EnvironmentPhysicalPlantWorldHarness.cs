using Arcontio.Core.Config;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentPhysicalPlantWorldHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato compatto del controllo data-only sul boundary fisico tra Biosfera
    /// e <see cref="World"/> per le piante reali in mappa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: verifica del boundary senza renderer</b></para>
    /// <para>
    /// Questo risultato non riguarda ArcGraph, MapGrid o sprite. Misura solo se un
    /// delta prodotto dalla Biosfera viene recepito dal <see cref="World"/> come
    /// presenza fisica: occupazione cella, blocco movimento, blocco visione, linea
    /// di vista e dirty percettivo degli NPC vicini.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BornProjectionOk</b>: una pianta nata viene registrata come proiezione World-side.</item>
    ///   <item><b>BornBlockingOk</b>: la pianta nata blocca movimento, visione e LOS.</item>
    ///   <item><b>BornDirtyOk</b>: la nascita sporca la percezione degli NPC vicini, non di quelli lontani.</item>
    ///   <item><b>StateChangedOk</b>: uno stage/health delta aggiorna la proiezione esistente.</item>
    ///   <item><b>RelocationOk</b>: uno spostamento libera la cella vecchia e blocca la nuova.</item>
    ///   <item><b>DeathRemovalOk</b>: la morte rimuove la proiezione fisica e libera la cella.</item>
    ///   <item><b>DeathDirtyOk</b>: la morte sporca la percezione vicino alla cella liberata.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentPhysicalPlantWorldHarnessResult
    {
        public readonly bool BornProjectionOk;
        public readonly bool BornBlockingOk;
        public readonly bool BornDirtyOk;
        public readonly bool StateChangedOk;
        public readonly bool RelocationOk;
        public readonly bool DeathRemovalOk;
        public readonly bool DeathDirtyOk;

        public bool IsSuccessful =>
            BornProjectionOk
            && BornBlockingOk
            && BornDirtyOk
            && StateChangedOk
            && RelocationOk
            && DeathRemovalOk
            && DeathDirtyOk;

        // =============================================================================
        // EnvironmentPhysicalPlantWorldHarnessResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato aggregando i singoli controlli del boundary.
        /// </para>
        /// </summary>
        public EnvironmentPhysicalPlantWorldHarnessResult(
            bool bornProjectionOk,
            bool bornBlockingOk,
            bool bornDirtyOk,
            bool stateChangedOk,
            bool relocationOk,
            bool deathRemovalOk,
            bool deathDirtyOk)
        {
            BornProjectionOk = bornProjectionOk;
            BornBlockingOk = bornBlockingOk;
            BornDirtyOk = bornDirtyOk;
            StateChangedOk = stateChangedOk;
            RelocationOk = relocationOk;
            DeathRemovalOk = deathRemovalOk;
            DeathDirtyOk = deathDirtyOk;
        }
    }

    // =============================================================================
    // EnvironmentPhysicalPlantWorldHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per verificare che le piante fisiche della Biosfera vengano
    /// proiettate correttamente nel <see cref="World"/> senza usare MapGrid,
    /// ArcGraph, scene Unity o asset visuali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Biosfera decide, World applica fisica</b></para>
    /// <para>
    /// La Biosfera produce delta semantici delle piante, mentre il World mantiene
    /// le cache fisiche consultate da pathfinding, FOV e percezione. Questo harness
    /// protegge quel contratto: una pianta non e' un oggetto di catalogo, ma quando
    /// esiste fisicamente deve comportarsi come un ostacolo rispetto a movimento e
    /// linea di vista.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultChecks</b>: esegue il caso completo nascita/cambio/spostamento/morte.</item>
    ///   <item><b>CreateHarnessWorld</b>: costruisce un World isolato con due NPC di controllo.</item>
    ///   <item><b>CreateDelta</b>: materializza delta Biosfera compatti senza creare PlantInstance.</item>
    ///   <item><b>IsOnlyNearNpcDirty</b>: verifica che il dirty resti locale e conservativo.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentPhysicalPlantWorldHarness
    {
        private const int NearNpcX = 10;
        private const int NearNpcY = 10;
        private const int FarNpcX = 40;
        private const int FarNpcY = 40;

        private static readonly EnvironmentPlantId ProbePlantId = new EnvironmentPlantId(6200);
        private static readonly EnvironmentAreaId ProbeAreaId = new EnvironmentAreaId(62);
        private static readonly EnvironmentCellCoord BornCell = new EnvironmentCellCoord(12, 10);
        private static readonly EnvironmentCellCoord RelocatedCell = new EnvironmentCellCoord(14, 10);

        // =============================================================================
        // RunDefaultChecks
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue il controllo completo sul contratto Biosfera -> World per una
        /// singola pianta fisica.
        /// </para>
        /// </summary>
        public static EnvironmentPhysicalPlantWorldHarnessResult RunDefaultChecks()
        {
            World world = CreateHarnessWorld(out int nearNpcId, out int farNpcId);

            // CreateNpc marca dirty durante il bootstrap dell'NPC. Lo puliamo qui
            // per misurare solo l'effetto dei delta Biosfera successivi.
            world.ClearAllNpcPerceptionDirty();

            bool bornApplied = world.ApplyEnvironmentPhysicalPlantDelta(CreateDelta(
                EnvironmentPhysicalPlantDeltaKind.Born,
                BornCell,
                default,
                "mature",
                EnvironmentPlantHealthState.Healthy,
                isAlive: true));

            bool bornProjectionOk =
                bornApplied
                && world.TryGetPhysicalPlant(ProbePlantId, out var bornProjection)
                && bornProjection.Cell.Equals(BornCell)
                && bornProjection.SpeciesKey == "oak_tree"
                && bornProjection.GrowthStageKey == "mature"
                && bornProjection.HealthState == EnvironmentPlantHealthState.Healthy
                && bornProjection.IsAlive;

            bool bornBlockingOk =
                world.HasPhysicalPlantAt(BornCell.X, BornCell.Y)
                && world.IsMovementBlocked(BornCell.X, BornCell.Y)
                && world.IsVisionBlocked(BornCell.X, BornCell.Y)
                && world.TryGetOccluder(BornCell.X, BornCell.Y, out bool bornBlocksVision, out bool bornBlocksMovement, out float bornVisionCost)
                && bornBlocksVision
                && bornBlocksMovement
                && bornVisionCost > 0f
                && !world.HasLineOfSight(NearNpcX, NearNpcY, 16, 10);

            bool bornDirtyOk = IsOnlyNearNpcDirty(world, nearNpcId, farNpcId);

            // Lo stage update non deve creare un secondo occupante. Aggiorna la
            // proiezione e continua a sporcare localmente il blocco percettivo.
            world.ClearAllNpcPerceptionDirty();
            bool stateApplied = world.ApplyEnvironmentPhysicalPlantDelta(CreateDelta(
                EnvironmentPhysicalPlantDeltaKind.StageChanged,
                BornCell,
                BornCell,
                "dry",
                EnvironmentPlantHealthState.Stressed,
                isAlive: true));

            bool stateChangedOk =
                stateApplied
                && world.PhysicalPlants.Count == 1
                && world.TryGetPhysicalPlant(ProbePlantId, out var changedProjection)
                && changedProjection.Cell.Equals(BornCell)
                && changedProjection.GrowthStageKey == "dry"
                && changedProjection.HealthState == EnvironmentPlantHealthState.Stressed
                && IsOnlyNearNpcDirty(world, nearNpcId, farNpcId);

            // La relocation e' il caso delicato per le cache: la cella vecchia deve
            // tornare libera e la nuova deve diventare bloccante nello stesso delta.
            world.ClearAllNpcPerceptionDirty();
            bool relocationApplied = world.ApplyEnvironmentPhysicalPlantDelta(CreateDelta(
                EnvironmentPhysicalPlantDeltaKind.Relocated,
                RelocatedCell,
                BornCell,
                "dry",
                EnvironmentPlantHealthState.Stressed,
                isAlive: true));

            bool relocationOk =
                relocationApplied
                && world.PhysicalPlants.Count == 1
                && !world.HasPhysicalPlantAt(BornCell.X, BornCell.Y)
                && !world.IsMovementBlocked(BornCell.X, BornCell.Y)
                && !world.IsVisionBlocked(BornCell.X, BornCell.Y)
                && world.HasPhysicalPlantAt(RelocatedCell.X, RelocatedCell.Y)
                && world.IsMovementBlocked(RelocatedCell.X, RelocatedCell.Y)
                && world.IsVisionBlocked(RelocatedCell.X, RelocatedCell.Y)
                && IsOnlyNearNpcDirty(world, nearNpcId, farNpcId);

            // La morte fisica deve togliere l'ostacolo. La PlantInstance biologica
            // potra' anche restare nello stato Biosfera per storia/save futuri, ma
            // il World non deve piu' considerarla blocco di mappa.
            world.ClearAllNpcPerceptionDirty();
            bool deathApplied = world.ApplyEnvironmentPhysicalPlantDelta(CreateDelta(
                EnvironmentPhysicalPlantDeltaKind.Died,
                RelocatedCell,
                RelocatedCell,
                "dead",
                EnvironmentPlantHealthState.Dead,
                isAlive: false));

            bool deathRemovalOk =
                deathApplied
                && world.PhysicalPlants.Count == 0
                && !world.HasPhysicalPlantAt(RelocatedCell.X, RelocatedCell.Y)
                && !world.IsMovementBlocked(RelocatedCell.X, RelocatedCell.Y)
                && !world.IsVisionBlocked(RelocatedCell.X, RelocatedCell.Y)
                && world.HasLineOfSight(NearNpcX, NearNpcY, 16, 10);

            bool deathDirtyOk = IsOnlyNearNpcDirty(world, nearNpcId, farNpcId);

            return new EnvironmentPhysicalPlantWorldHarnessResult(
                bornProjectionOk,
                bornBlockingOk,
                bornDirtyOk,
                stateChangedOk,
                relocationOk,
                deathRemovalOk,
                deathDirtyOk);
        }

        // =============================================================================
        // CreateHarnessWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un World minimo con due NPC: uno vicino alla pianta di test e uno
        /// abbastanza lontano da non essere sporcato dal dirty locale.
        /// </para>
        /// </summary>
        private static World CreateHarnessWorld(out int nearNpcId, out int farNpcId)
        {
            var sim = new SimulationParams
            {
                npcVisionRangeCells = 6,
                npcVisionUseCone = true,
                npcVisionFovDegrees = 90,
                perception = new ObjectPerceptionRuntimeParams
                {
                    dirtyRadiusMarginCells = 1
                }
            };

            var world = new World(new WorldConfig(sim));
            nearNpcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("BiosphereHarness_NearNpc"),
                NpcNeeds.Default(),
                new Social(),
                NearNpcX,
                NearNpcY);
            farNpcId = world.CreateNpc(
                NpcDnaProfile.CreateDefault("BiosphereHarness_FarNpc"),
                NpcNeeds.Default(),
                new Social(),
                FarNpcX,
                FarNpcY);

            return world;
        }

        // =============================================================================
        // CreateDelta
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un delta Biosfera compatto per la pianta sonda usata dall'harness.
        /// </para>
        /// </summary>
        private static EnvironmentPhysicalPlantDelta CreateDelta(
            EnvironmentPhysicalPlantDeltaKind kind,
            EnvironmentCellCoord cell,
            EnvironmentCellCoord previousCell,
            string growthStageKey,
            EnvironmentPlantHealthState healthState,
            bool isAlive)
        {
            return new EnvironmentPhysicalPlantDelta(
                kind,
                ProbePlantId,
                ProbeAreaId,
                cell,
                previousCell,
                "oak_tree",
                growthStageKey,
                healthState,
                isAlive);
        }

        // =============================================================================
        // IsOnlyNearNpcDirty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il dirty percettivo sia stato assegnato al solo NPC vicino.
        /// </para>
        /// </summary>
        private static bool IsOnlyNearNpcDirty(World world, int nearNpcId, int farNpcId)
        {
            return world.PerceptionDirtyNpcCount == 1
                   && world.IsNpcPerceptionDirty(nearNpcId)
                   && !world.IsNpcPerceptionDirty(farNpcId);
        }
    }
}
