using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentAreaSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only minimo di un'area ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: osservazione senza autorita' di mutazione</b></para>
    /// <para>
    /// UI, debug, save/load o adapter futuri devono poter leggere una copia
    /// dell'area senza ricevere registry mutabili. Questo snapshot contiene solo
    /// value type e stringhe normalizzate.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Definition</b>: descrizione base dell'area.</item>
    ///   <item><b>Has*</b>: indica quali payload specializzati sono presenti.</item>
    ///   <item><b>*State</b>: payload copiati per fertilita', acqua, vegetazione o seed bank.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentAreaSnapshot
    {
        public readonly EnvironmentAreaDefinition Definition;
        public readonly bool HasFertility;
        public readonly EnvironmentFertilityAreaState FertilityState;
        public readonly bool HasWater;
        public readonly EnvironmentWaterAreaState WaterState;
        public readonly bool HasVegetation;
        public readonly EnvironmentVegetationAreaState VegetationState;
        public readonly bool HasSeedBank;
        public readonly EnvironmentSeedBankAreaState SeedBankState;

        public EnvironmentAreaSnapshot(
            EnvironmentAreaDefinition definition,
            bool hasFertility,
            EnvironmentFertilityAreaState fertilityState,
            bool hasWater,
            EnvironmentWaterAreaState waterState,
            bool hasVegetation,
            EnvironmentVegetationAreaState vegetationState,
            bool hasSeedBank,
            EnvironmentSeedBankAreaState seedBankState)
        {
            Definition = definition;
            HasFertility = hasFertility;
            FertilityState = fertilityState;
            HasWater = hasWater;
            WaterState = waterState;
            HasVegetation = hasVegetation;
            VegetationState = vegetationState;
            HasSeedBank = hasSeedBank;
            SeedBankState = seedBankState;
        }
    }

    // =============================================================================
    // EnvironmentSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only aggregato della foundation ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Core snapshot, non ArcGraph snapshot</b></para>
    /// <para>
    /// Questo snapshot appartiene al Core. Non espone tipi ArcGraph, SpriteKey o
    /// oggetti Unity. Un adapter futuro potra' trasformare alcuni campi in snapshot
    /// visuali, ma la biosfera resta indipendente dal renderer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Calendar</b>: stato calendario.</item>
    ///   <item><b>Climate</b>: stato clima globale.</item>
    ///   <item><b>Areas</b>: lista read-only di snapshot area.</item>
    ///   <item><b>Plants</b>: lista read-only di piante importanti.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentSnapshot
    {
        private static readonly EnvironmentAreaSnapshot[] EmptyAreas = new EnvironmentAreaSnapshot[0];
        private static readonly EnvironmentPlantSnapshot[] EmptyPlants = new EnvironmentPlantSnapshot[0];

        public EnvironmentCalendarState Calendar { get; }
        public EnvironmentGlobalClimateState Climate { get; }
        public IReadOnlyList<EnvironmentAreaSnapshot> Areas { get; }
        public IReadOnlyList<EnvironmentPlantSnapshot> Plants { get; }

        // =============================================================================
        // EnvironmentSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno snapshot aggregato usando una lista gia' materializzata.
        /// </para>
        /// </summary>
        public EnvironmentSnapshot(
            EnvironmentCalendarState calendar,
            EnvironmentGlobalClimateState climate,
            IReadOnlyList<EnvironmentAreaSnapshot> areas,
            IReadOnlyList<EnvironmentPlantSnapshot> plants = null)
        {
            // La lista viene trattata come read-only dal contratto. I futuri builder
            // potranno passare array copiati per impedire mutazioni esterne residue.
            Calendar = calendar;
            Climate = climate;
            Areas = areas ?? EmptyAreas;
            Plants = plants ?? EmptyPlants;
        }
    }
}
