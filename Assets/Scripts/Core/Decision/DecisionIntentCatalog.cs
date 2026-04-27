namespace Arcontio.Core
{
    // =============================================================================
    // DecisionIntentKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo stabile delle intenzioni astratte che il Decision Layer puo'
    /// produrre prima che esista il Job System completo.
    /// </para>
    ///
    /// <para><b>Separazione Decision Layer / execution</b></para>
    /// <para>
    /// L'intenzione descrive cosa l'NPC vuole fare, non come lo eseguira'. La
    /// traduzione in Job, Step o Command resta fuori da questo enum e verra'
    /// introdotta progressivamente nelle sessioni successive.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Survival</b>: intenzioni guidate dai bisogni primari.</item>
    ///   <item><b>Work</b>: intenzioni legate a domini produttivi e istituzionali.</item>
    ///   <item><b>Social</b>: intenzioni comunicative e relazionali.</item>
    ///   <item><b>Meta</b>: ricerca, attesa e recupero quando mancano target affidabili.</item>
    /// </list>
    /// </summary>
    public enum DecisionIntentKind
    {
        None = 0,
        EatKnownFood = 1,
        SearchFood = 2,
        DrinkKnownWater = 3,
        SearchWater = 4,
        RestKnownPlace = 5,
        SearchRestPlace = 6,
        TakeRestrictedFood = 7,
        UseRestrictedRestPlace = 8,
        SeekSafety = 9,
        MaintainStability = 10,
        SeekSocialContact = 11,
        AskForHelp = 12,
        CommunicateKnownDanger = 13,
        PatrolArea = 14,
        FarmFood = 15,
        BuildStructure = 16,
        CraftItem = 17,
        HaulToStorage = 18,
        ManageStorage = 19,
        GovernColony = 20,
        ExploreArea = 21,
        WaitAndObserve = 22
    }

    // =============================================================================
    // DecisionIntentMetadata
    // =============================================================================
    /// <summary>
    /// <para>
    /// Metadati statici associati a una singola intenzione del catalogo.
    /// </para>
    ///
    /// <para><b>Catalogo data-puro</b></para>
    /// <para>
    /// La struttura non contiene delegati, query live o riferimenti al World. Serve
    /// solo a dare al Decision Layer informazioni leggibili e deterministiche:
    /// dominio, bisogno primario, flag di emergenza e disponibilita' MVP.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: identificatore stabile dell'intenzione.</item>
    ///   <item><b>Domain</b>: dominio NpcProfile usato da competence/preference/obligation.</item>
    ///   <item><b>PrimaryNeed</b>: bisogno che puo' generare urgenza diretta.</item>
    ///   <item><b>RequiresBeliefTarget</b>: true se serve un target dal QuerySystem.</item>
    ///   <item><b>IsEmergencyIntent</b>: true se puo' essere favorita da bisogno critico.</item>
    ///   <item><b>RequiresNormCheck</b>: true se l'intenzione attraversa il filtro norme/social risk.</item>
    ///   <item><b>SocialRisk01</b>: rischio sociale statico iniziale, in attesa del Social Layer completo.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionIntentMetadata
    {
        public readonly DecisionIntentKind Kind;
        public readonly DomainKind Domain;
        public readonly NeedKind PrimaryNeed;
        public readonly BeliefCategory TargetBeliefCategory;
        public readonly bool RequiresBeliefTarget;
        public readonly bool IsEmergencyIntent;
        public readonly bool IsMvpAvailable;
        public readonly bool RequiresNormCheck;
        public readonly float SocialRisk01;
        public readonly string DebugLabel;

        public DecisionIntentMetadata(
            DecisionIntentKind kind,
            DomainKind domain,
            NeedKind primaryNeed,
            BeliefCategory targetBeliefCategory,
            bool requiresBeliefTarget,
            bool isEmergencyIntent,
            bool isMvpAvailable,
            string debugLabel)
            : this(
                kind,
                domain,
                primaryNeed,
                targetBeliefCategory,
                requiresBeliefTarget,
                isEmergencyIntent,
                isMvpAvailable,
                requiresNormCheck: false,
                socialRisk01: 0f,
                debugLabel: debugLabel)
        {
        }

        public DecisionIntentMetadata(
            DecisionIntentKind kind,
            DomainKind domain,
            NeedKind primaryNeed,
            BeliefCategory targetBeliefCategory,
            bool requiresBeliefTarget,
            bool isEmergencyIntent,
            bool isMvpAvailable,
            bool requiresNormCheck,
            float socialRisk01,
            string debugLabel)
        {
            Kind = kind;
            Domain = domain;
            PrimaryNeed = primaryNeed;
            TargetBeliefCategory = targetBeliefCategory;
            RequiresBeliefTarget = requiresBeliefTarget;
            IsEmergencyIntent = isEmergencyIntent;
            IsMvpAvailable = isMvpAvailable;
            RequiresNormCheck = requiresNormCheck;
            SocialRisk01 = Clamp01(socialRisk01);
            DebugLabel = debugLabel ?? string.Empty;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    // =============================================================================
    // DecisionIntentCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo centralizzato delle intenzioni note al Decision Layer v0.05.
    /// </para>
    ///
    /// <para><b>Integrazione progressiva</b></para>
    /// <para>
    /// La sessione 1 introduce solo la forma dati e i metadati statici. Le sessioni
    /// successive useranno questo catalogo per generare candidati, filtrare
    /// precondizioni e calcolare score senza duplicare tabelle in piu' punti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>All</b>: array ordinato delle intenzioni effettivamente enumerabili.</item>
    ///   <item><b>GetMetadata</b>: switch esplicito e deterministico per una singola intenzione.</item>
    ///   <item><b>TryGetMetadata</b>: variante guardata per input esterni o test.</item>
    /// </list>
    /// </summary>
    public static class DecisionIntentCatalog
    {
        public static readonly DecisionIntentKind[] All =
        {
            DecisionIntentKind.EatKnownFood,
            DecisionIntentKind.SearchFood,
            DecisionIntentKind.DrinkKnownWater,
            DecisionIntentKind.SearchWater,
            DecisionIntentKind.RestKnownPlace,
            DecisionIntentKind.SearchRestPlace,
            DecisionIntentKind.TakeRestrictedFood,
            DecisionIntentKind.UseRestrictedRestPlace,
            DecisionIntentKind.SeekSafety,
            DecisionIntentKind.MaintainStability,
            DecisionIntentKind.SeekSocialContact,
            DecisionIntentKind.AskForHelp,
            DecisionIntentKind.CommunicateKnownDanger,
            DecisionIntentKind.PatrolArea,
            DecisionIntentKind.FarmFood,
            DecisionIntentKind.BuildStructure,
            DecisionIntentKind.CraftItem,
            DecisionIntentKind.HaulToStorage,
            DecisionIntentKind.ManageStorage,
            DecisionIntentKind.GovernColony,
            DecisionIntentKind.ExploreArea,
            DecisionIntentKind.WaitAndObserve
        };

        public static bool TryGetMetadata(DecisionIntentKind kind, out DecisionIntentMetadata metadata)
        {
            if (kind == DecisionIntentKind.None)
            {
                metadata = default;
                return false;
            }

            metadata = GetMetadata(kind);
            return metadata.Kind != DecisionIntentKind.None;
        }

        public static DecisionIntentMetadata GetMetadata(DecisionIntentKind kind)
        {
            // Lo switch resta volutamente esplicito: per un catalogo piccolo e stabile
            // e' piu' leggibile di una tabella mutabile inizializzata a runtime.
            switch (kind)
            {
                case DecisionIntentKind.EatKnownFood:
                    return new DecisionIntentMetadata(kind, DomainKind.Agriculture, NeedKind.Hunger, BeliefCategory.Food, true, true, true, "EatKnownFood");
                case DecisionIntentKind.SearchFood:
                    return new DecisionIntentMetadata(kind, DomainKind.Exploration, NeedKind.Hunger, BeliefCategory.Food, false, true, true, "SearchFood");
                case DecisionIntentKind.DrinkKnownWater:
                    return new DecisionIntentMetadata(kind, DomainKind.Exploration, NeedKind.Thirst, BeliefCategory.Situation, true, true, false, "DrinkKnownWater");
                case DecisionIntentKind.SearchWater:
                    return new DecisionIntentMetadata(kind, DomainKind.Exploration, NeedKind.Thirst, BeliefCategory.Situation, false, true, false, "SearchWater");
                case DecisionIntentKind.RestKnownPlace:
                    return new DecisionIntentMetadata(kind, DomainKind.Social, NeedKind.Rest, BeliefCategory.Rest, true, true, true, "RestKnownPlace");
                case DecisionIntentKind.SearchRestPlace:
                    return new DecisionIntentMetadata(kind, DomainKind.Exploration, NeedKind.Rest, BeliefCategory.Rest, false, true, true, "SearchRestPlace");
                case DecisionIntentKind.TakeRestrictedFood:
                    return new DecisionIntentMetadata(kind, DomainKind.Social, NeedKind.Hunger, BeliefCategory.Food, true, true, true, true, 0.80f, "TakeRestrictedFood");
                case DecisionIntentKind.UseRestrictedRestPlace:
                    return new DecisionIntentMetadata(kind, DomainKind.Social, NeedKind.Rest, BeliefCategory.Rest, true, true, true, true, 0.55f, "UseRestrictedRestPlace");
                case DecisionIntentKind.SeekSafety:
                    return new DecisionIntentMetadata(kind, DomainKind.Security, NeedKind.Security, BeliefCategory.Danger, false, true, false, "SeekSafety");
                case DecisionIntentKind.MaintainStability:
                    return new DecisionIntentMetadata(kind, DomainKind.Social, NeedKind.Stability, BeliefCategory.Situation, false, false, false, "MaintainStability");
                case DecisionIntentKind.SeekSocialContact:
                    return new DecisionIntentMetadata(kind, DomainKind.Social, NeedKind.Sociality, BeliefCategory.Social, false, false, false, "SeekSocialContact");
                case DecisionIntentKind.AskForHelp:
                    return new DecisionIntentMetadata(kind, DomainKind.Social, NeedKind.Sociality, BeliefCategory.Social, false, true, false, "AskForHelp");
                case DecisionIntentKind.CommunicateKnownDanger:
                    return new DecisionIntentMetadata(kind, DomainKind.Social, NeedKind.Security, BeliefCategory.Danger, true, true, false, "CommunicateKnownDanger");
                case DecisionIntentKind.PatrolArea:
                    return new DecisionIntentMetadata(kind, DomainKind.Security, NeedKind.Security, BeliefCategory.Danger, false, false, false, "PatrolArea");
                case DecisionIntentKind.FarmFood:
                    return new DecisionIntentMetadata(kind, DomainKind.Agriculture, NeedKind.Hunger, BeliefCategory.Food, false, false, false, "FarmFood");
                case DecisionIntentKind.BuildStructure:
                    return new DecisionIntentMetadata(kind, DomainKind.Construction, NeedKind.Comfort, BeliefCategory.Structure, false, false, false, "BuildStructure");
                case DecisionIntentKind.CraftItem:
                    return new DecisionIntentMetadata(kind, DomainKind.Crafting, NeedKind.Comfort, BeliefCategory.Structure, false, false, false, "CraftItem");
                case DecisionIntentKind.HaulToStorage:
                    return new DecisionIntentMetadata(kind, DomainKind.Storage, NeedKind.Stability, BeliefCategory.Structure, false, false, false, "HaulToStorage");
                case DecisionIntentKind.ManageStorage:
                    return new DecisionIntentMetadata(kind, DomainKind.Storage, NeedKind.Stability, BeliefCategory.Situation, false, false, false, "ManageStorage");
                case DecisionIntentKind.GovernColony:
                    return new DecisionIntentMetadata(kind, DomainKind.Governance, NeedKind.Stability, BeliefCategory.Situation, false, false, false, "GovernColony");
                case DecisionIntentKind.ExploreArea:
                    return new DecisionIntentMetadata(kind, DomainKind.Exploration, NeedKind.Stability, BeliefCategory.Structure, false, false, false, "ExploreArea");
                case DecisionIntentKind.WaitAndObserve:
                    return new DecisionIntentMetadata(kind, DomainKind.None, NeedKind.Stability, BeliefCategory.Situation, false, false, true, "WaitAndObserve");
                default:
                    return default;
            }
        }
    }
}
