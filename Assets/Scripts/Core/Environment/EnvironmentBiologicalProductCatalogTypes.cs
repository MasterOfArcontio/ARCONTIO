using System;
using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentBiologicalProductCategory
    // =============================================================================
    /// <summary>
    /// <para>
    /// Categoria semantica di un prodotto biologico ottenibile da una pianta.
    /// </para>
    ///
    /// <para><b>Principio architetturale: prodotto biologico separato dalla specie</b></para>
    /// <para>
    /// La categoria appartiene al prodotto, non alla singola pianta. Una quercia
    /// puo' produrre ghiande e legna, ma "ghianda come cibo/seme" e "legna come
    /// materiale" devono restare concetti riusabili anche se in futuro arriveranno
    /// altre sorgenti, negozi, crafting o scambi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Food</b>: prodotto alimentare o consumabile come nutrimento futuro.</item>
    ///   <item><b>Material</b>: materiale non alimentare, come legno o fibra.</item>
    ///   <item><b>Seed</b>: prodotto con valore di propagazione/agricoltura futura.</item>
    ///   <item><b>Medicine</b>: prodotto medicinale futuro.</item>
    ///   <item><b>Fuel</b>: prodotto utilizzabile come combustibile futuro.</item>
    /// </list>
    /// </summary>
    [Flags]
    public enum EnvironmentBiologicalProductCategory
    {
        None = 0,
        Food = 1 << 0,
        Material = 1 << 1,
        Seed = 1 << 2,
        Medicine = 1 << 3,
        Fuel = 1 << 4
    }

    // =============================================================================
    // EnvironmentBiologicalProductDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione read-only di un prodotto biologico, separata dalle specie che lo
    /// possono generare e dagli oggetti concreti che lo rappresentano.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Biosfera produce productKey, ObjectCatalog definisce item</b></para>
    /// <para>
    /// Questo record fa da ponte controllato: la Biosfera puo' dire "questa pianta
    /// produce berry", mentre il consumo, la nutrizione e l'oggetto trasportabile
    /// restano nel catalogo oggetti tramite <c>ObjectDefId</c>. Il record non crea
    /// inventario, non consuma piante e non scrive nel World.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ProductKey</b>: chiave stabile usata da Biosfera, query e job futuri.</item>
    ///   <item><b>ObjectDefId</b>: definizione oggetto che materializzera' il prodotto.</item>
    ///   <item><b>Categories</b>: maschera semantica per decisioni e filtri futuri.</item>
    ///   <item><b>IsFood</b>: shortcut esplicito per decisioni alimentari.</item>
    ///   <item><b>IsTransportable</b>: indica se il futuro inventario typed potra' portarlo.</item>
    ///   <item><b>RecommendedToolKey</b>: tool suggerito a livello prodotto, non obbligo job.</item>
    ///   <item><b>DefaultHarvestEffort</b>: costo indicativo, utile al futuro modello stanchezza.</item>
    ///   <item><b>DefaultCarryUnits</b>: unita' base di trasporto per inventario futuro.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentBiologicalProductDefinition
    {
        public readonly string ProductKey;
        public readonly string ObjectDefId;
        public readonly EnvironmentBiologicalProductCategory Categories;
        public readonly bool IsFood;
        public readonly bool IsTransportable;
        public readonly string RecommendedToolKey;
        public readonly float DefaultHarvestEffort;
        public readonly int DefaultCarryUnits;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProductKey)
            && !string.IsNullOrWhiteSpace(ObjectDefId);

        // =============================================================================
        // EnvironmentBiologicalProductDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione normalizzata di prodotto biologico.
        /// </para>
        /// </summary>
        public EnvironmentBiologicalProductDefinition(
            string productKey,
            string objectDefId,
            EnvironmentBiologicalProductCategory categories,
            bool isFood,
            bool isTransportable,
            string recommendedToolKey,
            float defaultHarvestEffort,
            int defaultCarryUnits)
        {
            // Le chiavi vuote restano vuote: il validatore deve poterle segnalare
            // senza nascondere il problema dietro fallback troppo creativi.
            ProductKey = productKey == null ? string.Empty : productKey.Trim();
            ObjectDefId = objectDefId == null ? string.Empty : objectDefId.Trim();
            Categories = categories;
            IsFood = isFood || (categories & EnvironmentBiologicalProductCategory.Food) != 0;
            IsTransportable = isTransportable;
            RecommendedToolKey = recommendedToolKey == null ? string.Empty : recommendedToolKey.Trim();
            DefaultHarvestEffort = EnvironmentMath.Clamp01(defaultHarvestEffort);
            DefaultCarryUnits = defaultCarryUnits <= 0 ? 1 : defaultCarryUnits;
        }

        // =============================================================================
        // HasCategory
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la definizione include una categoria semantica.
        /// </para>
        /// </summary>
        public bool HasCategory(EnvironmentBiologicalProductCategory category)
        {
            return category != EnvironmentBiologicalProductCategory.None
                   && (Categories & category) != 0;
        }
    }

    // =============================================================================
    // EnvironmentBiologicalProductCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo read-only dei prodotti biologici conosciuti dal runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: lookup stabile senza stato runtime</b></para>
    /// <para>
    /// Il catalogo non conosce piante vive, quantita' disponibili o inventari NPC.
    /// Offre solo lookup per <c>productKey</c>, cosi' query, validatori e futuri job
    /// possono condividere lo stesso contratto senza accedere a strutture mutabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Products</b>: lista read-only delle definizioni accettate.</item>
    ///   <item><b>TryGetProduct</b>: lookup case-insensitive per productKey.</item>
    ///   <item><b>ContainsProduct</b>: controllo presenza leggero.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentBiologicalProductCatalog
    {
        private static readonly EnvironmentBiologicalProductDefinition[] EmptyProducts =
            new EnvironmentBiologicalProductDefinition[0];

        private readonly Dictionary<string, EnvironmentBiologicalProductDefinition> _byKey =
            new Dictionary<string, EnvironmentBiologicalProductDefinition>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<EnvironmentBiologicalProductDefinition> Products { get; }
        public int ProductCount => Products.Count;

        // =============================================================================
        // EnvironmentBiologicalProductCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il catalogo copiando solo definizioni valide e non duplicate.
        /// </para>
        /// </summary>
        public EnvironmentBiologicalProductCatalog(
            IReadOnlyList<EnvironmentBiologicalProductDefinition> products)
        {
            if (products == null || products.Count == 0)
            {
                Products = EmptyProducts;
                return;
            }

            var accepted = new List<EnvironmentBiologicalProductDefinition>(products.Count);
            for (int i = 0; i < products.Count; i++)
            {
                EnvironmentBiologicalProductDefinition product = products[i];
                if (!product.IsValid || _byKey.ContainsKey(product.ProductKey))
                    continue;

                // La prima definizione vince, come per il catalogo piante. Il
                // validatore produce warning sui duplicati, mentre il catalogo resta
                // deterministico anche con dati authoring sporchi.
                _byKey.Add(product.ProductKey, product);
                accepted.Add(product);
            }

            Products = accepted.Count == 0 ? EmptyProducts : accepted.ToArray();
        }

        // =============================================================================
        // TryGetProduct
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un prodotto biologico per chiave stabile.
        /// </para>
        /// </summary>
        public bool TryGetProduct(
            string productKey,
            out EnvironmentBiologicalProductDefinition product)
        {
            product = default;

            if (string.IsNullOrWhiteSpace(productKey))
                return false;

            return _byKey.TryGetValue(productKey, out product);
        }

        // =============================================================================
        // ContainsProduct
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se il catalogo contiene la chiave prodotto richiesta.
        /// </para>
        /// </summary>
        public bool ContainsProduct(string productKey)
        {
            return TryGetProduct(productKey, out _);
        }
    }
}
