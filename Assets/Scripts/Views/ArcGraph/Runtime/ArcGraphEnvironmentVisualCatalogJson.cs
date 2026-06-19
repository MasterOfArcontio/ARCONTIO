using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEnvironmentVisualCatalogJson
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper per convertire il JSON visuale ambiente/biosfera in catalogo runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: parsing al bordo View</b></para>
    /// <para>
    /// Il file JSON resta dentro <c>Resources/ArcGraph/Config</c>, quindi appartiene
    /// al bordo visuale. Il parser non carica sprite, non crea GameObject e non
    /// legge il World: trasforma soltanto righe testuali in lookup deterministici.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DefaultResourcePath</b>: path Resources del catalogo.</item>
    ///   <item><b>ParseOrDefault</b>: parse con catalogo vuoto se il file manca.</item>
    ///   <item><b>TryParse</b>: parse esplicito con esito booleano.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphEnvironmentVisualCatalogJson
    {
        public const string DefaultResourcePath = "ArcGraph/Config/ArcGraphEnvironmentVisualCatalog";

        // =============================================================================
        // ParseOrDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte JSON in catalogo, restituendo un catalogo vuoto se il testo non
        /// e' valido.
        /// </para>
        /// </summary>
        public static ArcGraphEnvironmentVisualCatalog ParseOrDefault(string json)
        {
            return TryParse(json, out var catalog)
                ? catalog
                : new ArcGraphEnvironmentVisualCatalog(null);
        }

        // =============================================================================
        // TryParse
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a convertire una stringa JSON in
        /// <see cref="ArcGraphEnvironmentVisualCatalog"/>.
        /// </para>
        /// </summary>
        public static bool TryParse(
            string json,
            out ArcGraphEnvironmentVisualCatalog catalog)
        {
            catalog = null;

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var dto = JsonUtility.FromJson<ArcGraphEnvironmentVisualCatalogDto>(json);
                if (dto == null)
                    return false;

                catalog = dto.ToRuntimeCatalog();
                return catalog != null;
            }
            catch
            {
                catalog = null;
                return false;
            }
        }
    }

    // =============================================================================
    // ArcGraphEnvironmentVisualCatalogDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del catalogo visuale ambiente/biosfera.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphEnvironmentVisualCatalogDto
    {
        public ArcGraphEnvironmentVisualEntryDto[] visualEntries;
        public ArcGraphEnvironmentVisualEntryDto[] physicalPlants;
        public ArcGraphEnvironmentVisualEntryDto[] diffuseVegetation;

        // =============================================================================
        // ToRuntimeCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte le tre sezioni JSON in un unico catalogo runtime indicizzato.
        /// </para>
        /// </summary>
        public ArcGraphEnvironmentVisualCatalog ToRuntimeCatalog()
        {
            var definitions = new List<ArcGraphEnvironmentVisualDefinition>();
            AppendEntries(visualEntries, definitions);
            AppendEntries(physicalPlants, definitions);
            AppendEntries(diffuseVegetation, definitions);
            return new ArcGraphEnvironmentVisualCatalog(definitions);
        }

        private static void AppendEntries(
            ArcGraphEnvironmentVisualEntryDto[] entries,
            List<ArcGraphEnvironmentVisualDefinition> target)
        {
            if (entries == null || target == null)
                return;

            for (int i = 0; i < entries.Length; i++)
            {
                ArcGraphEnvironmentVisualEntryDto entry = entries[i];
                if (entry == null)
                    continue;

                target.Add(entry.ToRuntimeDefinition());
            }
        }
    }

    // =============================================================================
    // ArcGraphEnvironmentVisualEntryDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// Singola riga JSON del catalogo visuale ambiente/biosfera.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphEnvironmentVisualEntryDto
    {
        public string visualStateKey;
        public string spriteKey;
        public string speciesKey;
        public string growthStageKey;
        public string healthStateKey;
        public string vegetationKindKey;
        public string coverageBandKey;
        public string conditionBandKey;
        public bool allowsAnimation;
        public int sortingOffset;

        // =============================================================================
        // ToRuntimeDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Trasforma una riga mutabile JSON in definizione runtime immutabile.
        /// </para>
        /// </summary>
        public ArcGraphEnvironmentVisualDefinition ToRuntimeDefinition()
        {
            return new ArcGraphEnvironmentVisualDefinition(
                visualStateKey,
                spriteKey,
                speciesKey,
                growthStageKey,
                healthStateKey,
                vegetationKindKey,
                coverageBandKey,
                conditionBandKey,
                allowsAnimation,
                sortingOffset);
        }
    }
}
