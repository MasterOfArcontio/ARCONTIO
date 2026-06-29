using System;
using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentBiologicalProductCatalogConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del catalogo prodotti biologici.
    /// </para>
    ///
    /// <para><b>Principio architetturale: productKey fuori dalla specie</b></para>
    /// <para>
    /// Le specie vegetali dichiarano quali prodotti possono offrire. Questo file,
    /// invece, dichiara cosa significa quel prodotto come contratto condiviso tra
    /// Biosfera, oggetti, inventario futuro e decisioni. Il DTO non carica file e
    /// non crea stato runtime: normalizza soltanto dati di configurazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>schemaVersion</b>: versione dati del catalogo.</item>
    ///   <item><b>products</b>: definizioni configurabili dei prodotti.</item>
    ///   <item><b>ToCatalog</b>: costruisce un catalogo read-only normalizzato.</item>
    ///   <item><b>CreateDefaultSet</b>: fallback deterministico per bootstrap e test.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentBiologicalProductCatalogConfig
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public EnvironmentBiologicalProductConfig[] products =
            EnvironmentBiologicalProductConfig.CreateDefaultSet();

        // =============================================================================
        // ToCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la configurazione in catalogo read-only.
        /// </para>
        /// </summary>
        public EnvironmentBiologicalProductCatalog ToCatalog()
        {
            EnvironmentBiologicalProductConfig[] safeProducts =
                products ?? new EnvironmentBiologicalProductConfig[0];
            var definitions =
                new List<EnvironmentBiologicalProductDefinition>(safeProducts.Length);

            for (int i = 0; i < safeProducts.Length; i++)
            {
                if (safeProducts[i] == null)
                    continue;

                // Il DTO normalizza categorie e numeri; il catalogo poi filtra
                // chiavi mancanti o duplicate per mantenere lookup deterministici.
                definitions.Add(safeProducts[i].ToDefinition());
            }

            return new EnvironmentBiologicalProductCatalog(definitions);
        }
    }

    // =============================================================================
    // EnvironmentBiologicalProductConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile di un singolo prodotto biologico.
    /// </para>
    ///
    /// <para><b>Principio architetturale: biologico, oggetto e nutrizione restano distinti</b></para>
    /// <para>
    /// <c>productKey</c> e <c>objectDefId</c> collegano il mondo biologico al catalogo
    /// oggetti. La nutrizione concreta non viene duplicata qui: resta in
    /// <c>object_defs.json</c>, perche' appartiene all'item consumabile e non alla
    /// pianta o alla Biosfera.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>productKey</b>: chiave stabile prodotta dalle specie vegetali.</item>
    ///   <item><b>objectDefId</b>: id oggetto materializzabile futuro.</item>
    ///   <item><b>categories</b>: categorie testuali leggibili da JSON.</item>
    ///   <item><b>isFood</b>: flag esplicito per query/decisioni alimentari.</item>
    ///   <item><b>isTransportable</b>: compatibilita' futura con inventario typed.</item>
    ///   <item><b>recommendedToolKey</b>: tool suggerito a livello prodotto.</item>
    ///   <item><b>defaultHarvestEffort</b>: costo indicativo futuro per stanchezza.</item>
    ///   <item><b>defaultCarryUnits</b>: unita' base per trasporto futuro.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class EnvironmentBiologicalProductConfig
    {
        public string productKey = string.Empty;
        public string objectDefId = string.Empty;
        public string[] categories = Array.Empty<string>();
        public bool isFood;
        public bool isTransportable = true;
        public string recommendedToolKey = string.Empty;
        public float defaultHarvestEffort = 0.25f;
        public int defaultCarryUnits = 1;

        // =============================================================================
        // ToDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il DTO in definizione read-only normalizzata.
        /// </para>
        /// </summary>
        public EnvironmentBiologicalProductDefinition ToDefinition()
        {
            EnvironmentBiologicalProductCategory parsedCategories =
                EnvironmentBiologicalProductCatalogParsing.ParseCategoryMask(categories);

            return new EnvironmentBiologicalProductDefinition(
                productKey,
                objectDefId,
                parsedCategories,
                isFood,
                isTransportable,
                recommendedToolKey,
                defaultHarvestEffort,
                defaultCarryUnits);
        }

        // =============================================================================
        // CreateDefaultSet
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il set minimo coerente con il catalogo piante e oggetti corrente.
        /// </para>
        /// </summary>
        public static EnvironmentBiologicalProductConfig[] CreateDefaultSet()
        {
            return new[]
            {
                new EnvironmentBiologicalProductConfig
                {
                    productKey = "wood_log",
                    objectDefId = "wood_log",
                    categories = new[] { "Material" },
                    isFood = false,
                    isTransportable = true,
                    recommendedToolKey = "axe",
                    defaultHarvestEffort = 0.65f,
                    defaultCarryUnits = 1
                },
                new EnvironmentBiologicalProductConfig
                {
                    productKey = "acorn",
                    objectDefId = "acorn",
                    categories = new[] { "Food", "Seed" },
                    isFood = true,
                    isTransportable = true,
                    recommendedToolKey = string.Empty,
                    defaultHarvestEffort = 0.20f,
                    defaultCarryUnits = 1
                },
                new EnvironmentBiologicalProductConfig
                {
                    productKey = "berry",
                    objectDefId = "berry",
                    categories = new[] { "Food" },
                    isFood = true,
                    isTransportable = true,
                    recommendedToolKey = string.Empty,
                    defaultHarvestEffort = 0.15f,
                    defaultCarryUnits = 1
                }
            };
        }
    }

    // =============================================================================
    // EnvironmentBiologicalProductCatalogParsing
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper di parsing per categorie prodotto dichiarate come stringhe JSON.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stringhe ai bordi, maschere nel Core</b></para>
    /// <para>
    /// Il JSON deve restare leggibile dall'operatore. Il Core, pero', lavora meglio
    /// con maschere stabili e confronti economici. I valori sconosciuti vengono
    /// ignorati: il validatore potra' segnalarli in futuro senza rompere il runtime.
    /// </para>
    /// </summary>
    internal static class EnvironmentBiologicalProductCatalogParsing
    {
        public static EnvironmentBiologicalProductCategory ParseCategoryMask(string[] values)
        {
            if (values == null || values.Length == 0)
                return EnvironmentBiologicalProductCategory.None;

            EnvironmentBiologicalProductCategory mask =
                EnvironmentBiologicalProductCategory.None;

            for (int i = 0; i < values.Length; i++)
            {
                mask |= ParseCategory(values[i]);
            }

            return mask;
        }

        private static EnvironmentBiologicalProductCategory ParseCategory(string value)
        {
            if (string.Equals(value, "Food", StringComparison.OrdinalIgnoreCase))
                return EnvironmentBiologicalProductCategory.Food;

            if (string.Equals(value, "Material", StringComparison.OrdinalIgnoreCase))
                return EnvironmentBiologicalProductCategory.Material;

            if (string.Equals(value, "Seed", StringComparison.OrdinalIgnoreCase))
                return EnvironmentBiologicalProductCategory.Seed;

            if (string.Equals(value, "Medicine", StringComparison.OrdinalIgnoreCase))
                return EnvironmentBiologicalProductCategory.Medicine;

            if (string.Equals(value, "Fuel", StringComparison.OrdinalIgnoreCase))
                return EnvironmentBiologicalProductCategory.Fuel;

            return EnvironmentBiologicalProductCategory.None;
        }
    }
}
