using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEnvironmentVisualScope
    // =============================================================================
    /// <summary>
    /// <para>
    /// Classifica la forma visuale di un layer ambientale ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: forma grafica, non simulazione</b></para>
    /// <para>
    /// La biosfera, il meteo, l'acqua, la luce e gli incendi futuri potranno avere
    /// sistemi simulativi separati. Questa enum descrive solo come ArcGraph riceve
    /// e conserva il risultato visuale: per cella, come overlay globale o come
    /// effetto locale identificato. Non decide crescita, flusso, propagazione,
    /// temperatura, visibilita' percettiva o comportamento NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CellCache</b>: dati visuali indicizzati per cella.</item>
    ///   <item><b>GlobalOverlay</b>: dato visuale globale o per livello Z.</item>
    ///   <item><b>LocalEffect</b>: effetti locali indicizzati per id runtime/visuale.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphEnvironmentVisualScope
    {
        CellCache = 0,
        GlobalOverlay = 1,
        LocalEffect = 2
    }

    // =============================================================================
    // ArcGraphEnvironmentVisualLayerContract
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto value-only che descrive un layer ambientale visuale di ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: contratto dichiarativo prima del renderer</b></para>
    /// <para>
    /// La <c>v0.36a</c> non introduce rendering produttivo e non accende Unity.
    /// Questo contratto serve a fissare il perimetro tecnico dei futuri step:
    /// quale layer riceve snapshot esterni, quale layer puo' usare animazione
    /// sprite gestita da ArcGraph, quale layer usa dirty per cella e quale resta
    /// overlay globale. Tutti i campi sono primitivi o enum per mantenere il dato
    /// leggero e leggibile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>LayerId</b>: layer ArcGraph collegato.</item>
    ///   <item><b>Scope</b>: forma di cache visuale.</item>
    ///   <item><b>SourceSystemKey</b>: sistema esterno atteso come produttore snapshot.</item>
    ///   <item><b>RegisteredByDefault</b>: indica se entra nei layer foundation.</item>
    ///   <item><b>RequiresExternalSnapshots</b>: impedisce al layer di generare contenuto da solo.</item>
    ///   <item><b>UsesDirtyCells</b>: indica se la modifica marca celle/chunk sporchi.</item>
    ///   <item><b>AllowsArcGraphSpriteAnimation</b>: abilita futura scelta frame ArcGraph.</item>
    ///   <item><b>AllowsGlobalOverlay</b>: abilita composizione sopra la scena.</item>
    ///   <item><b>AllowsLocalTint</b>: abilita tinta/luce locale per cella.</item>
    ///   <item><b>AllowsUnityObjectCreation</b>: deve restare false in questa fase.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEnvironmentVisualLayerContract
    {
        public readonly ArcGraphLayerId LayerId;
        public readonly ArcGraphEnvironmentVisualScope Scope;
        public readonly string SourceSystemKey;
        public readonly bool RegisteredByDefault;
        public readonly bool RequiresExternalSnapshots;
        public readonly bool UsesDirtyCells;
        public readonly bool AllowsArcGraphSpriteAnimation;
        public readonly bool AllowsGlobalOverlay;
        public readonly bool AllowsLocalTint;
        public readonly bool AllowsUnityObjectCreation;

        public bool IsEnvironmentLayer =>
            LayerId == ArcGraphLayerId.Vegetation
            || LayerId == ArcGraphLayerId.Water
            || LayerId == ArcGraphLayerId.Light
            || LayerId == ArcGraphLayerId.Effect
            || LayerId == ArcGraphLayerId.Weather;

        // =============================================================================
        // ArcGraphEnvironmentVisualLayerContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il contratto dichiarativo di un layer ambientale visuale.
        /// </para>
        ///
        /// <para><b>Contratto passivo</b></para>
        /// <para>
        /// Il costruttore normalizza solo stringhe nulle. Non registra layer, non
        /// alloca renderer, non carica asset e non valida sistemi ambientali reali.
        /// Il suo scopo e' rendere esplicite le regole che i prossimi sottostep
        /// dovranno rispettare.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>layerId</b>: layer ArcGraph a cui si riferisce il contratto.</item>
        ///   <item><b>scope</b>: forma visuale del dato.</item>
        ///   <item><b>sourceSystemKey</b>: nome descrittivo del produttore esterno.</item>
        ///   <item><b>flag booleani</b>: capacita' ammesse o vietate.</item>
        /// </list>
        /// </summary>
        public ArcGraphEnvironmentVisualLayerContract(
            ArcGraphLayerId layerId,
            ArcGraphEnvironmentVisualScope scope,
            string sourceSystemKey,
            bool registeredByDefault,
            bool requiresExternalSnapshots,
            bool usesDirtyCells,
            bool allowsArcGraphSpriteAnimation,
            bool allowsGlobalOverlay,
            bool allowsLocalTint,
            bool allowsUnityObjectCreation)
        {
            LayerId = layerId;
            Scope = scope;
            SourceSystemKey = sourceSystemKey ?? string.Empty;
            RegisteredByDefault = registeredByDefault;
            RequiresExternalSnapshots = requiresExternalSnapshots;
            UsesDirtyCells = usesDirtyCells;
            AllowsArcGraphSpriteAnimation = allowsArcGraphSpriteAnimation;
            AllowsGlobalOverlay = allowsGlobalOverlay;
            AllowsLocalTint = allowsLocalTint;
            AllowsUnityObjectCreation = allowsUnityObjectCreation;
        }
    }

    // =============================================================================
    // ArcGraphEnvironmentVisualContractCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo statico dei contratti ambientali visuali preparatori per
    /// <c>v0.36</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: roadmap codificata, non runtime produttivo</b></para>
    /// <para>
    /// Il catalogo permette a test, audit e futuri builder di interrogare cosa e'
    /// consentito per Vegetation, Water, Light, Effect e Weather. Non registra i
    /// layer nello stack, non crea snapshot e non viene chiamato automaticamente dal
    /// bootstrap. I contratti sono volutamente piccoli: servono a evitare ambiguita'
    /// prima di implementare i sottostep ambientali veri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AppendDefaultContracts</b>: aggiunge i contratti a una lista riusabile.</item>
    ///   <item><b>CreateDefaultContracts</b>: helper comodo per audit e harness.</item>
    ///   <item><b>TryGetDefaultContract</b>: lookup lineare su cinque elementi.</item>
    ///   <item><b>IsEnvironmentLayer</b>: filtro layer ambientali.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphEnvironmentVisualContractCatalog
    {
        // =============================================================================
        // AppendDefaultContracts
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiunge i cinque contratti ambientali default a una lista fornita dal
        /// chiamante.
        /// </para>
        ///
        /// <para><b>Contratto CPU-leggero</b></para>
        /// <para>
        /// Il metodo permette di riusare una lista esistente negli harness o in
        /// futuri strumenti diagnostici. Non mantiene stato globale mutabile e non
        /// alloca oggetti Unity. La dimensione e' fissa e piccola: cinque record.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>target</b>: lista da popolare, se non nulla.</item>
        /// </list>
        /// </summary>
        public static void AppendDefaultContracts(IList<ArcGraphEnvironmentVisualLayerContract> target)
        {
            if (target == null)
                return;

            target.Add(CreateVegetationContract());
            target.Add(CreateWaterContract());
            target.Add(CreateLightContract());
            target.Add(CreateEffectContract());
            target.Add(CreateWeatherContract());
        }

        // =============================================================================
        // CreateDefaultContracts
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un array con i cinque contratti ambientali default.
        /// </para>
        ///
        /// <para><b>Helper da audit</b></para>
        /// <para>
        /// L'array e' pensato per QA e lettura diagnostica, non per un hot path di
        /// rendering. Il runtime grafico futuro potra' preferire
        /// <c>AppendDefaultContracts</c> se vorra' riusare buffer.
        /// </para>
        /// </summary>
        public static ArcGraphEnvironmentVisualLayerContract[] CreateDefaultContracts()
        {
            return new[]
            {
                CreateVegetationContract(),
                CreateWaterContract(),
                CreateLightContract(),
                CreateEffectContract(),
                CreateWeatherContract()
            };
        }

        // =============================================================================
        // TryGetDefaultContract
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il contratto default per un layer ambientale, se esiste.
        /// </para>
        ///
        /// <para><b>Lookup piccolo e prevedibile</b></para>
        /// <para>
        /// Il catalogo contiene solo cinque layer. Uno switch esplicito e'
        /// sufficiente, evita dizionari statici e rende chiara la mappa tra layer e
        /// contratto.
        /// </para>
        /// </summary>
        public static bool TryGetDefaultContract(
            ArcGraphLayerId layerId,
            out ArcGraphEnvironmentVisualLayerContract contract)
        {
            switch (layerId)
            {
                case ArcGraphLayerId.Vegetation:
                    contract = CreateVegetationContract();
                    return true;
                case ArcGraphLayerId.Water:
                    contract = CreateWaterContract();
                    return true;
                case ArcGraphLayerId.Light:
                    contract = CreateLightContract();
                    return true;
                case ArcGraphLayerId.Effect:
                    contract = CreateEffectContract();
                    return true;
                case ArcGraphLayerId.Weather:
                    contract = CreateWeatherContract();
                    return true;
                default:
                    contract = default;
                    return false;
            }
        }

        // =============================================================================
        // IsEnvironmentLayer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se un layer id appartiene al gruppo ambientale di <c>v0.36</c>.
        /// </para>
        /// </summary>
        public static bool IsEnvironmentLayer(ArcGraphLayerId layerId)
        {
            return TryGetDefaultContract(layerId, out _);
        }

        private static ArcGraphEnvironmentVisualLayerContract CreateVegetationContract()
        {
            return new ArcGraphEnvironmentVisualLayerContract(
                ArcGraphLayerId.Vegetation,
                ArcGraphEnvironmentVisualScope.CellCache,
                "VegetationSystem",
                registeredByDefault: false,
                requiresExternalSnapshots: true,
                usesDirtyCells: true,
                allowsArcGraphSpriteAnimation: true,
                allowsGlobalOverlay: false,
                allowsLocalTint: false,
                allowsUnityObjectCreation: false);
        }

        private static ArcGraphEnvironmentVisualLayerContract CreateWaterContract()
        {
            return new ArcGraphEnvironmentVisualLayerContract(
                ArcGraphLayerId.Water,
                ArcGraphEnvironmentVisualScope.CellCache,
                "WaterSystem",
                registeredByDefault: false,
                requiresExternalSnapshots: true,
                usesDirtyCells: true,
                allowsArcGraphSpriteAnimation: true,
                allowsGlobalOverlay: false,
                allowsLocalTint: false,
                allowsUnityObjectCreation: false);
        }

        private static ArcGraphEnvironmentVisualLayerContract CreateLightContract()
        {
            return new ArcGraphEnvironmentVisualLayerContract(
                ArcGraphLayerId.Light,
                ArcGraphEnvironmentVisualScope.CellCache,
                "LightSystem",
                registeredByDefault: false,
                requiresExternalSnapshots: true,
                usesDirtyCells: true,
                allowsArcGraphSpriteAnimation: false,
                allowsGlobalOverlay: true,
                allowsLocalTint: true,
                allowsUnityObjectCreation: false);
        }

        private static ArcGraphEnvironmentVisualLayerContract CreateEffectContract()
        {
            return new ArcGraphEnvironmentVisualLayerContract(
                ArcGraphLayerId.Effect,
                ArcGraphEnvironmentVisualScope.LocalEffect,
                "EffectSystem",
                registeredByDefault: false,
                requiresExternalSnapshots: true,
                usesDirtyCells: true,
                allowsArcGraphSpriteAnimation: true,
                allowsGlobalOverlay: false,
                allowsLocalTint: true,
                allowsUnityObjectCreation: false);
        }

        private static ArcGraphEnvironmentVisualLayerContract CreateWeatherContract()
        {
            return new ArcGraphEnvironmentVisualLayerContract(
                ArcGraphLayerId.Weather,
                ArcGraphEnvironmentVisualScope.GlobalOverlay,
                "WeatherSystem",
                registeredByDefault: false,
                requiresExternalSnapshots: true,
                usesDirtyCells: false,
                allowsArcGraphSpriteAnimation: true,
                allowsGlobalOverlay: true,
                allowsLocalTint: false,
                allowsUnityObjectCreation: false);
        }
    }
}
