using System.Collections.Generic;
using Arcontio.Core;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentBiologicalProductCatalogValidator
    // =============================================================================
    /// <summary>
    /// <para>
    /// Validatore read-only dei contratti tra specie vegetali, prodotti biologici e
    /// catalogo oggetti.
    /// </para>
    ///
    /// <para><b>Principio architetturale: coerenza ai bordi, nessuna mutazione</b></para>
    /// <para>
    /// Questo validatore non corregge dati e non crea fallback impliciti nel World.
    /// Segnala incoerenze tra cataloghi separati: specie che dichiarano prodotti
    /// mancanti, prodotti senza oggetto, food senza nutrizione e non-food marcati
    /// come alimenti. In questo modo la separazione resta utile e verificabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ValidateProductCatalog</b>: controlla forma e duplicati del catalogo prodotti.</item>
    ///   <item><b>ValidatePlantProductLinks</b>: controlla specie -> productKey.</item>
    ///   <item><b>ValidateObjectLinks</b>: controlla productKey -> ObjectDef e proprieta' food.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentBiologicalProductCatalogValidator
    {
        // =============================================================================
        // Validate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Valida la coerenza complessiva tra catalogo piante, prodotti e oggetti.
        /// </para>
        /// </summary>
        public static EnvironmentConfigValidationResult Validate(
            EnvironmentPlantCatalogConfig plantCatalogConfig,
            EnvironmentBiologicalProductCatalogConfig productCatalogConfig,
            IReadOnlyDictionary<string, ObjectDef> objectDefs)
        {
            var issues = new List<EnvironmentConfigValidationIssue>();
            ValidateProductCatalog(productCatalogConfig, issues);

            EnvironmentBiologicalProductCatalog productCatalog =
                productCatalogConfig != null
                    ? productCatalogConfig.ToCatalog()
                    : new EnvironmentBiologicalProductCatalogConfig().ToCatalog();

            ValidatePlantProductLinks(plantCatalogConfig, productCatalog, issues);
            ValidateObjectLinks(productCatalog, objectDefs, issues);

            return new EnvironmentConfigValidationResult(issues);
        }

        private static void ValidateProductCatalog(
            EnvironmentBiologicalProductCatalogConfig productCatalogConfig,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (productCatalogConfig == null)
            {
                AddIssue(
                    issues,
                    "ENV_BIO_PRODUCT_CATALOG_MISSING",
                    EnvironmentConfigValidationSeverity.Warning,
                    "Catalogo prodotti biologici assente: verra' usato il fallback default.");
                return;
            }

            if (productCatalogConfig.schemaVersion <= 0)
            {
                AddIssue(
                    issues,
                    "ENV_BIO_PRODUCT_SCHEMA_VERSION_INVALID",
                    EnvironmentConfigValidationSeverity.Warning,
                    "schemaVersion catalogo prodotti biologici non positivo.");
            }

            EnvironmentBiologicalProductConfig[] products = productCatalogConfig.products;
            if (products == null || products.Length == 0)
            {
                AddIssue(
                    issues,
                    "ENV_BIO_PRODUCT_CATALOG_EMPTY",
                    EnvironmentConfigValidationSeverity.Error,
                    "Catalogo prodotti biologici vuoto: le specie non possono risolvere productKey.");
                return;
            }

            var keys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var duplicates = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < products.Length; i++)
            {
                EnvironmentBiologicalProductConfig product = products[i];
                if (product == null)
                {
                    AddIssue(
                        issues,
                        "ENV_BIO_PRODUCT_NULL",
                        EnvironmentConfigValidationSeverity.Warning,
                        "Prodotto biologico nullo: verra' ignorato dal catalogo.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(product.productKey))
                {
                    AddIssue(
                        issues,
                        "ENV_BIO_PRODUCT_KEY_EMPTY",
                        EnvironmentConfigValidationSeverity.Error,
                        "Prodotto biologico senza productKey.");
                    continue;
                }

                if (!keys.Add(product.productKey) && duplicates.Add(product.productKey))
                {
                    AddIssue(
                        issues,
                        "ENV_BIO_PRODUCT_KEY_DUPLICATE",
                        EnvironmentConfigValidationSeverity.Warning,
                        "Prodotto biologico duplicato: il catalogo usera' la prima definizione.");
                }

                if (string.IsNullOrWhiteSpace(product.objectDefId))
                {
                    AddIssue(
                        issues,
                        "ENV_BIO_PRODUCT_OBJECT_DEF_EMPTY",
                        EnvironmentConfigValidationSeverity.Error,
                        "Prodotto biologico senza objectDefId.");
                }
            }
        }

        private static void ValidatePlantProductLinks(
            EnvironmentPlantCatalogConfig plantCatalogConfig,
            EnvironmentBiologicalProductCatalog productCatalog,
            List<EnvironmentConfigValidationIssue> issues)
        {
            EnvironmentPlantSpeciesConfig[] species = plantCatalogConfig?.species;
            if (species == null || species.Length == 0 || productCatalog == null)
                return;

            for (int speciesIndex = 0; speciesIndex < species.Length; speciesIndex++)
            {
                EnvironmentPlantSpeciesConfig entry = species[speciesIndex];
                if (entry == null || entry.products == null)
                    continue;

                for (int productIndex = 0; productIndex < entry.products.Length; productIndex++)
                {
                    EnvironmentPlantProductConfig product = entry.products[productIndex];
                    if (product == null || string.IsNullOrWhiteSpace(product.productKey))
                        continue;

                    if (productCatalog.ContainsProduct(product.productKey))
                        continue;

                    AddIssue(
                        issues,
                        "ENV_PLANT_PRODUCT_NOT_IN_BIO_PRODUCT_CATALOG",
                        EnvironmentConfigValidationSeverity.Error,
                        $"Specie '{entry.speciesKey}' dichiara productKey '{product.productKey}' non presente nel catalogo prodotti biologici.");
                }
            }
        }

        private static void ValidateObjectLinks(
            EnvironmentBiologicalProductCatalog productCatalog,
            IReadOnlyDictionary<string, ObjectDef> objectDefs,
            List<EnvironmentConfigValidationIssue> issues)
        {
            if (productCatalog == null || productCatalog.ProductCount == 0)
                return;

            if (objectDefs == null || objectDefs.Count == 0)
            {
                AddIssue(
                    issues,
                    "ENV_BIO_PRODUCT_OBJECT_CATALOG_MISSING",
                    EnvironmentConfigValidationSeverity.Warning,
                    "Catalogo oggetti assente: impossibile validare objectDefId dei prodotti biologici.");
                return;
            }

            for (int i = 0; i < productCatalog.Products.Count; i++)
            {
                EnvironmentBiologicalProductDefinition product = productCatalog.Products[i];
                if (!product.IsValid)
                    continue;

                if (!objectDefs.TryGetValue(product.ObjectDefId, out ObjectDef objectDef) || objectDef == null)
                {
                    AddIssue(
                        issues,
                        "ENV_BIO_PRODUCT_OBJECT_DEF_MISSING",
                        EnvironmentConfigValidationSeverity.Error,
                        $"Prodotto biologico '{product.ProductKey}' punta a objectDefId mancante '{product.ObjectDefId}'.");
                    continue;
                }

                bool objectIsFood = objectDef.TryGetPropertyValue("FoodItem", out float foodFlag)
                                    && foodFlag > 0f;
                bool hasNutrition = objectDef.TryGetPropertyValue("NutritionValue", out float nutritionValue)
                                    && nutritionValue > 0f;

                if (product.IsFood && (!objectIsFood || !hasNutrition))
                {
                    AddIssue(
                        issues,
                        "ENV_BIO_PRODUCT_FOOD_OBJECT_INVALID",
                        EnvironmentConfigValidationSeverity.Error,
                        $"Prodotto food '{product.ProductKey}' richiede ObjectDef '{product.ObjectDefId}' con FoodItem e NutritionValue > 0.");
                }

                if (!product.IsFood && (objectIsFood || hasNutrition))
                {
                    AddIssue(
                        issues,
                        "ENV_BIO_PRODUCT_NON_FOOD_OBJECT_MARKED_FOOD",
                        EnvironmentConfigValidationSeverity.Warning,
                        $"Prodotto non-food '{product.ProductKey}' punta a ObjectDef '{product.ObjectDefId}' che sembra consumabile come cibo.");
                }
            }
        }

        private static void AddIssue(
            List<EnvironmentConfigValidationIssue> issues,
            string code,
            EnvironmentConfigValidationSeverity severity,
            string message)
        {
            issues.Add(new EnvironmentConfigValidationIssue(
                code,
                severity,
                EnvironmentAreaId.None,
                message));
        }
    }
}
