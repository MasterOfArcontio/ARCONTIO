using System;
using System.Collections.Generic;
using Arcontio.Core;
using Arcontio.Core.Environment;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEnvironmentVisualDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione visuale ArcGraph per piante fisiche, vegetazione diffusa e
    /// proiezioni ambientali semantiche.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo visuale separato dalla biosfera</b></para>
    /// <para>
    /// La biosfera produce chiavi semantiche come specie, stadio, salute, tipo
    /// vegetazione e bande discrete. Questa definizione non cambia quelle chiavi e
    /// non le interpreta come logica ecologica: si limita a dire quale sprite
    /// ArcGraph usare quando quelle chiavi arrivano al boundary visuale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>VisualStateKey</b>: chiave semantica esplicita, per esempio <c>plant_oak_tree</c>.</item>
    ///   <item><b>SpriteKey</b>: path Resources o sheet#subSprite risolvibile lato scena.</item>
    ///   <item><b>Species/Growth/Health</b>: indice compatibile con <see cref="WorldPhysicalPlantProjection"/>.</item>
    ///   <item><b>Vegetation/Coverage/Condition</b>: indice compatibile con <see cref="EnvironmentDiffuseVegetationDelta"/>.</item>
    ///   <item><b>AllowsAnimation</b>: abilita animazione sprite se il LOD corrente la consente.</item>
    ///   <item><b>SortingOffset</b>: offset opzionale per futuri renderer vegetazione.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEnvironmentVisualDefinition
    {
        public readonly string VisualStateKey;
        public readonly string SpriteKey;
        public readonly string SpeciesKey;
        public readonly string GrowthStageKey;
        public readonly string HealthStateKey;
        public readonly string VegetationKindKey;
        public readonly string CoverageBandKey;
        public readonly string ConditionBandKey;
        public readonly bool AllowsAnimation;
        public readonly int SortingOffset;

        // =============================================================================
        // ArcGraphEnvironmentVisualDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione visuale normalizzando tutte le chiavi.
        /// </para>
        /// </summary>
        public ArcGraphEnvironmentVisualDefinition(
            string visualStateKey,
            string spriteKey,
            string speciesKey,
            string growthStageKey,
            string healthStateKey,
            string vegetationKindKey,
            string coverageBandKey,
            string conditionBandKey,
            bool allowsAnimation,
            int sortingOffset)
        {
            VisualStateKey = NormalizeKey(visualStateKey);
            SpriteKey = string.IsNullOrWhiteSpace(spriteKey) ? string.Empty : spriteKey.Trim();
            SpeciesKey = NormalizeKey(speciesKey);
            GrowthStageKey = NormalizeKey(growthStageKey);
            HealthStateKey = NormalizeKey(healthStateKey);
            VegetationKindKey = NormalizeKey(vegetationKindKey);
            CoverageBandKey = NormalizeKey(coverageBandKey);
            ConditionBandKey = NormalizeKey(conditionBandKey);
            AllowsAnimation = allowsAnimation;
            SortingOffset = sortingOffset;
        }

        internal static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }
    }

    // =============================================================================
    // ArcGraphEnvironmentVisualCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo runtime che risolve stati visuali ambiente/biosfera in sprite key
    /// ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: lookup visuale, non fonte di verita'</b></para>
    /// <para>
    /// Il catalogo non decide dove nascono piante, non conserva stato biologico e
    /// non crea vegetazione. Riceve dati gia' prodotti da World/Biosfera e restituisce
    /// solo una definizione visuale. In questo modo ArcGraph puo' disegnare senza
    /// leggere direttamente <c>EnvironmentState</c> o strutture mutabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_byVisualStateKey</b>: lookup diretto da <c>EnvironmentVisualProjectionRecord.VisualKey</c>.</item>
    ///   <item><b>_byPhysicalPlantKey</b>: lookup da specie/stadio/salute delle piante fisiche.</item>
    ///   <item><b>_byDiffuseVegetationKey</b>: lookup da tipo/copertura/condizione della vegetazione diffusa.</item>
    ///   <item><b>TryResolve*</b>: entry point espliciti per i contratti biosfera attuali.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphEnvironmentVisualCatalog
    {
        private readonly Dictionary<string, ArcGraphEnvironmentVisualDefinition> _byVisualStateKey = new();
        private readonly Dictionary<string, ArcGraphEnvironmentVisualDefinition> _byPhysicalPlantKey = new();
        private readonly Dictionary<string, ArcGraphEnvironmentVisualDefinition> _byDiffuseVegetationKey = new();

        public int VisualStateCount => _byVisualStateKey.Count;
        public int PhysicalPlantRuleCount => _byPhysicalPlantKey.Count;
        public int DiffuseVegetationRuleCount => _byDiffuseVegetationKey.Count;

        // =============================================================================
        // ArcGraphEnvironmentVisualCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce gli indici del catalogo da definizioni gia' normalizzate.
        /// </para>
        /// </summary>
        public ArcGraphEnvironmentVisualCatalog(
            IReadOnlyList<ArcGraphEnvironmentVisualDefinition> definitions)
        {
            if (definitions == null)
                return;

            for (int i = 0; i < definitions.Count; i++)
                AddDefinition(definitions[i]);
        }

        // =============================================================================
        // TryResolveByVisualStateKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve una chiave visuale semantica gia' pronta, per esempio
        /// <c>plant_oak_tree</c> o <c>vegetation_grass</c>.
        /// </para>
        /// </summary>
        public bool TryResolveByVisualStateKey(
            string visualStateKey,
            out ArcGraphEnvironmentVisualDefinition definition)
        {
            return _byVisualStateKey.TryGetValue(
                ArcGraphEnvironmentVisualDefinition.NormalizeKey(visualStateKey),
                out definition);
        }

        // =============================================================================
        // TryResolveProjectionRecord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve un record visuale neutrale prodotto da
        /// <see cref="EnvironmentVisualProjectionResolver"/>.
        /// </para>
        /// </summary>
        public bool TryResolveProjectionRecord(
            EnvironmentVisualProjectionRecord record,
            out ArcGraphEnvironmentVisualDefinition definition)
        {
            return TryResolveByVisualStateKey(record.VisualKey, out definition);
        }

        // =============================================================================
        // TryResolvePhysicalPlant
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve una pianta fisica proiettata dal World usando i campi reali
        /// <see cref="WorldPhysicalPlantProjection.SpeciesKey"/>,
        /// <see cref="WorldPhysicalPlantProjection.GrowthStageKey"/> e
        /// <see cref="WorldPhysicalPlantProjection.HealthState"/>.
        /// </para>
        /// </summary>
        public bool TryResolvePhysicalPlant(
            WorldPhysicalPlantProjection projection,
            out ArcGraphEnvironmentVisualDefinition definition)
        {
            return TryResolvePhysicalPlant(
                projection.SpeciesKey,
                projection.GrowthStageKey,
                projection.HealthState.ToString(),
                out definition);
        }

        // =============================================================================
        // TryResolvePhysicalPlant
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve una pianta fisica da specie, stadio e salute discreta.
        /// </para>
        /// </summary>
        public bool TryResolvePhysicalPlant(
            string speciesKey,
            string growthStageKey,
            string healthStateKey,
            out ArcGraphEnvironmentVisualDefinition definition)
        {
            string key = CreateCompositeKey(speciesKey, growthStageKey, healthStateKey);
            return _byPhysicalPlantKey.TryGetValue(key, out definition);
        }

        // =============================================================================
        // TryResolveDiffuseVegetation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve un delta di vegetazione diffusa usando i campi reali
        /// <see cref="EnvironmentDiffuseVegetationDelta.VegetationKind"/>,
        /// <see cref="EnvironmentDiffuseVegetationDelta.CoverageBand"/> e
        /// <see cref="EnvironmentDiffuseVegetationDelta.ConditionBand"/>.
        /// </para>
        /// </summary>
        public bool TryResolveDiffuseVegetation(
            EnvironmentDiffuseVegetationDelta delta,
            out ArcGraphEnvironmentVisualDefinition definition)
        {
            return TryResolveDiffuseVegetation(
                delta.VegetationKind.ToString(),
                delta.CoverageBand.ToString(),
                delta.ConditionBand.ToString(),
                out definition);
        }

        // =============================================================================
        // TryResolveDiffuseVegetation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve vegetazione diffusa da tipo, banda copertura e banda condizione.
        /// </para>
        /// </summary>
        public bool TryResolveDiffuseVegetation(
            string vegetationKindKey,
            string coverageBandKey,
            string conditionBandKey,
            out ArcGraphEnvironmentVisualDefinition definition)
        {
            string key = CreateCompositeKey(vegetationKindKey, coverageBandKey, conditionBandKey);
            return _byDiffuseVegetationKey.TryGetValue(key, out definition);
        }

        private void AddDefinition(ArcGraphEnvironmentVisualDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(definition.VisualStateKey))
                _byVisualStateKey[definition.VisualStateKey] = definition;

            if (!string.IsNullOrWhiteSpace(definition.SpeciesKey)
                && !string.IsNullOrWhiteSpace(definition.GrowthStageKey)
                && !string.IsNullOrWhiteSpace(definition.HealthStateKey))
            {
                _byPhysicalPlantKey[CreateCompositeKey(
                    definition.SpeciesKey,
                    definition.GrowthStageKey,
                    definition.HealthStateKey)] = definition;
            }

            if (!string.IsNullOrWhiteSpace(definition.VegetationKindKey)
                && !string.IsNullOrWhiteSpace(definition.CoverageBandKey)
                && !string.IsNullOrWhiteSpace(definition.ConditionBandKey))
            {
                _byDiffuseVegetationKey[CreateCompositeKey(
                    definition.VegetationKindKey,
                    definition.CoverageBandKey,
                    definition.ConditionBandKey)] = definition;
            }
        }

        private static string CreateCompositeKey(
            string first,
            string second,
            string third)
        {
            return ArcGraphEnvironmentVisualDefinition.NormalizeKey(first)
                   + "|"
                   + ArcGraphEnvironmentVisualDefinition.NormalizeKey(second)
                   + "|"
                   + ArcGraphEnvironmentVisualDefinition.NormalizeKey(third);
        }
    }
}
