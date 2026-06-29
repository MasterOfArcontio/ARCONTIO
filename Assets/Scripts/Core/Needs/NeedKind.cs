namespace Arcontio.Core
{
    // =============================================================================
    // NeedKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo stabile dei bisogni rappresentabili nello stato runtime per-NPC.
    /// Ogni valore indicizza un elemento dell'array <c>NpcNeeds.States</c>, quindi
    /// l'ordine dell'enum è parte del contratto di salvataggio e debug.
    /// </para>
    ///
    /// <para><b>Progressive integration dei bisogni</b></para>
    /// <para>
    /// Hunger, Thirst e Rest sono bisogni fisiologici diretti. Security, Stability
    /// e Sociality sono bisogni psicologici attivati in v0.04.11 con decay baseline.
    /// Health e Comfort restano nel catalogo per compatibilità e UI, ma non devono
    /// essere trattati come scalari autonomi finché il BodyWound System e la formula
    /// derivativa del comfort non sono implementati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>0-2</b>: bisogni fisiologici diretti già operativi.</item>
    ///   <item><b>3-4</b>: bisogni fisici/comfort presenti ma parziali o derivativi.</item>
    ///   <item><b>5-7</b>: bisogni psicologici baseline attivati dallo step v0.04.11.</item>
    ///   <item><b>COUNT</b>: sentinella usata per dimensionare array e overlay debug.</item>
    /// </list>
    /// </summary>
    public enum NeedKind
    {
        Hunger    = 0,
        Thirst    = 1,
        Rest      = 2,
        Health    = 3,
        Comfort   = 4,
        Security  = 5,
        Stability = 6,
        Sociality = 7,
        COUNT     = 8
    }

    /// <summary>
    /// Stato runtime di un singolo bisogno.
    ///
    /// I flag IsAlert e IsCritical sono valori derivati:
    /// vengono settati da NeedsDecaySystem ogni tick confrontando Value01
    /// con NpcThresholds.NeedAlert01 e NeedCritical01 del DNA dell'NPC.
    /// Non serializzarli — vengono ricalcolati al caricamento.
    /// </summary>
    public struct NeedState
    {
        /// <summary>0 = bisogno soddisfatto, 1 = bisogno al limite critico.</summary>
        public float Value01;

        /// <summary>True se Value01 >= NpcThresholds.NeedAlert01 del DNA.</summary>
        public bool IsAlert;

        /// <summary>True se Value01 >= NpcThresholds.NeedCritical01 del DNA.</summary>
        public bool IsCritical;
    }

    /// <summary>
    /// Componente runtime per-NPC: array di NeedState indicizzato per NeedKind.
    ///
    /// Sostituisce la vecchia struct Needs (Hunger01 / Fatigue01 / Morale01).
    ///
    /// Nota sull'aliasing dell'array:
    ///   States è un array (reference type) dentro una struct.
    ///   Il pattern esistente (TryGetValue → modifica → riassegna al dictionary) rimane
    ///   valido perché l'array è condiviso per riferimento tra copie della struct.
    ///   Usare sempre i metodi helper (GetValue, AddValue, SetValue, SetFlags)
    ///   per modificare gli elementi in modo sicuro (struct NeedState è value type,
    ///   quindi l'accesso diretto via States[i].Campo non modifica l'array).
    /// </summary>
    public struct NpcNeeds
    {
        public NeedState[] States;

        /// <summary>Crea un NpcNeeds con tutti i bisogni a 0 e flag false.</summary>
        public static NpcNeeds Default()
        {
            return new NpcNeeds { States = new NeedState[(int)NeedKind.COUNT] };
        }

        /// <summary>
        /// Factory rapida per i test/seed scenario: crea NpcNeeds con Hunger e Rest
        /// già impostati a valori specifici. Gli altri NeedKind rimangono a 0.
        /// </summary>
        public static NpcNeeds Make(float hunger, float rest)
        {
            var n = Default();
            n.SetValue(NeedKind.Hunger, hunger);
            n.SetValue(NeedKind.Rest,   rest);
            return n;
        }

        // ── Lettura ────────────────────────────────────────────────────────────

        public float GetValue(NeedKind k)
            => States != null ? States[(int)k].Value01 : 0f;

        public bool IsAlert(NeedKind k)
            => States != null && States[(int)k].IsAlert;

        public bool IsCritical(NeedKind k)
            => States != null && States[(int)k].IsCritical;

        // ── Scrittura ──────────────────────────────────────────────────────────

        /// <summary>Somma delta al valore, risultato clampato a [0, 1].</summary>
        public void AddValue(NeedKind k, float delta)
        {
            if (States == null) return;
            var s = States[(int)k];
            float v = s.Value01 + delta;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            s.Value01 = v;
            States[(int)k] = s;
        }

        /// <summary>Imposta il valore, clampato a [0, 1].</summary>
        public void SetValue(NeedKind k, float value01)
        {
            if (States == null) return;
            var s = States[(int)k];
            float v = value01;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            s.Value01 = v;
            States[(int)k] = s;
        }

        /// <summary>Aggiorna i flag IsAlert/IsCritical per un singolo bisogno.</summary>
        public void SetFlags(NeedKind k, bool isAlert, bool isCritical)
        {
            if (States == null) return;
            var s = States[(int)k];
            s.IsAlert    = isAlert;
            s.IsCritical = isCritical;
            States[(int)k] = s;
        }
    }

    // =============================================================================
    // ObjectFoodNutritionResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato normalizzato della lettura nutrizionale di una definizione oggetto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: nutrizione come dato, non come decisione del command</b></para>
    /// <para>
    /// I comandi di consumo devono applicare una mutazione autorizzata, non decidere
    /// localmente quali oggetti siano cibo e quanto nutrano. Questa struct trasporta
    /// quindi il responso data-driven gia' risolto dal catalogo oggetti, includendo
    /// il marker di fallback necessario ai percorsi legacy ancora presenti.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ObjectDefId</b>: definizione oggetto richiesta dal consumer.</item>
    ///   <item><b>IsConsumableFood</b>: indica se il catalogo/fallback consente il consumo.</item>
    ///   <item><b>IsTypedFoodItem</b>: vero per oggetti con proprieta' <c>FoodItem</c>.</item>
    ///   <item><b>IsLegacyFoodStock</b>: vero per oggetti con proprieta' <c>FoodStock</c> o fallback legacy.</item>
    ///   <item><b>NutritionValue</b>: recupero fame da applicare al bisogno Hunger.</item>
    ///   <item><b>UsedNutritionFallback</b>: vero quando e' stato usato <c>NeedsConfig.eatSatietyGain</c>.</item>
    ///   <item><b>FailureReason</b>: diagnostica sintetica per definizioni non consumabili.</item>
    /// </list>
    /// </summary>
    public readonly struct ObjectFoodNutritionResult
    {
        public readonly string ObjectDefId;
        public readonly bool IsConsumableFood;
        public readonly bool IsTypedFoodItem;
        public readonly bool IsLegacyFoodStock;
        public readonly float NutritionValue;
        public readonly bool UsedNutritionFallback;
        public readonly string FailureReason;

        public ObjectFoodNutritionResult(
            string objectDefId,
            bool isConsumableFood,
            bool isTypedFoodItem,
            bool isLegacyFoodStock,
            float nutritionValue,
            bool usedNutritionFallback,
            string failureReason)
        {
            ObjectDefId = objectDefId ?? string.Empty;
            IsConsumableFood = isConsumableFood;
            IsTypedFoodItem = isTypedFoodItem;
            IsLegacyFoodStock = isLegacyFoodStock;
            NutritionValue = nutritionValue;
            UsedNutritionFallback = usedNutritionFallback;
            FailureReason = failureReason ?? string.Empty;
        }
    }

    // =============================================================================
    // ObjectFoodNutritionResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver read-only per stabilire se una definizione oggetto rappresenta cibo
    /// consumabile e quale valore nutrizionale deve applicare al bisogno Fame.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ponte controllato tra legacy food_stock e item typed</b></para>
    /// <para>
    /// La v0.71.05 introduce prodotti biologici e prepara l'inventario typed, ma non
    /// rimuove ancora <c>NpcPrivateFood</c> o il consumo da <c>FoodStockComponent</c>.
    /// Questo resolver centralizza la regola comune: <c>FoodItem</c> indica cibo
    /// typed, <c>FoodStock</c> indica cibo legacy/generico, <c>NutritionValue</c>
    /// resta il dato primario e <c>NeedsConfig.eatSatietyGain</c> e' solo fallback
    /// dichiarato per i percorsi legacy.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>catalog lookup</b>: legge la definizione dal <c>World.ObjectDefs</c>.</item>
    ///   <item><b>classificazione</b>: distingue <c>FoodItem</c>, <c>FoodStock</c> e non-cibo.</item>
    ///   <item><b>nutrizione</b>: usa <c>NutritionValue</c> se positivo.</item>
    ///   <item><b>fallback legacy</b>: usa il fallback solo per food stock o per esplicita compatibilita' privata.</item>
    /// </list>
    /// </summary>
    public static class ObjectFoodNutritionResolver
    {
        private const string FoodItemPropertyKey = "FoodItem";
        private const string FoodStockPropertyKey = "FoodStock";
        private const string NutritionValuePropertyKey = "NutritionValue";
        private const float HardSafeLegacyFallback = 0.45f;

        // =============================================================================
        // Resolve
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la nutrizione di una definizione oggetto usando il catalogo del
        /// mondo, con un fallback legacy opzionale per i percorsi storici.
        /// </para>
        ///
        /// <para><b>Compatibilita' controllata</b></para>
        /// <para>
        /// I world di test o vecchi salvataggi possono non avere ancora il catalogo
        /// oggetti caricato. In quei casi il chiamante puo' indicare che il defId e'
        /// un cibo legacy noto; il resolver restituisce un risultato consumabile ma
        /// marca <c>UsedNutritionFallback</c>, cosi' il debito resta osservabile.
        /// </para>
        /// </summary>
        public static ObjectFoodNutritionResult Resolve(
            World world,
            string objectDefId,
            float legacyFallback,
            bool allowLegacyFallbackWhenDefinitionMissing)
        {
            string safeDefId = objectDefId ?? string.Empty;
            float safeFallback = legacyFallback > 0f ? legacyFallback : HardSafeLegacyFallback;

            // Se il catalogo non e' disponibile ma il chiamante sta attraversando un
            // percorso legacy dichiarato, manteniamo compatibilita' senza fingere che
            // il dato arrivi dal catalogo.
            if (world == null
                || string.IsNullOrWhiteSpace(safeDefId)
                || !world.TryGetObjectDef(safeDefId, out ObjectDef def)
                || def == null)
            {
                if (allowLegacyFallbackWhenDefinitionMissing)
                {
                    return new ObjectFoodNutritionResult(
                        safeDefId,
                        isConsumableFood: true,
                        isTypedFoodItem: false,
                        isLegacyFoodStock: true,
                        nutritionValue: safeFallback,
                        usedNutritionFallback: true,
                        failureReason: string.Empty);
                }

                return new ObjectFoodNutritionResult(
                    safeDefId,
                    isConsumableFood: false,
                    isTypedFoodItem: false,
                    isLegacyFoodStock: false,
                    nutritionValue: 0f,
                    usedNutritionFallback: false,
                    failureReason: "ObjectDefMissing");
            }

            bool isTypedFoodItem = HasPositiveFlag(def, FoodItemPropertyKey);
            bool isLegacyFoodStock = HasPositiveFlag(def, FoodStockPropertyKey);
            if (!isTypedFoodItem && !isLegacyFoodStock)
            {
                return new ObjectFoodNutritionResult(
                    safeDefId,
                    isConsumableFood: false,
                    isTypedFoodItem: false,
                    isLegacyFoodStock: false,
                    nutritionValue: 0f,
                    usedNutritionFallback: false,
                    failureReason: "ObjectDefIsNotFood");
            }

            if (def.TryGetPropertyValue(NutritionValuePropertyKey, out float nutritionValue)
                && nutritionValue > 0f)
            {
                return new ObjectFoodNutritionResult(
                    safeDefId,
                    isConsumableFood: true,
                    isTypedFoodItem: isTypedFoodItem,
                    isLegacyFoodStock: isLegacyFoodStock,
                    nutritionValue: nutritionValue,
                    usedNutritionFallback: false,
                    failureReason: string.Empty);
            }

            // Il fallback e' ammesso solo per il cibo legacy/generico. Un FoodItem
            // typed senza NutritionValue positivo deve emergere come dato incompleto,
            // perche' in futuro berry/acorn/etc. non devono nutrire per magia.
            if (isLegacyFoodStock)
            {
                return new ObjectFoodNutritionResult(
                    safeDefId,
                    isConsumableFood: true,
                    isTypedFoodItem: isTypedFoodItem,
                    isLegacyFoodStock: true,
                    nutritionValue: safeFallback,
                    usedNutritionFallback: true,
                    failureReason: string.Empty);
            }

            return new ObjectFoodNutritionResult(
                safeDefId,
                isConsumableFood: false,
                isTypedFoodItem: isTypedFoodItem,
                isLegacyFoodStock: false,
                nutritionValue: 0f,
                usedNutritionFallback: false,
                failureReason: "NutritionValueMissingOrInvalid");
        }

        // =============================================================================
        // HasPositiveFlag
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge una proprieta' numerica come flag data-driven positivo.
        /// </para>
        /// </summary>
        private static bool HasPositiveFlag(ObjectDef def, string key)
        {
            return def != null
                   && def.TryGetPropertyValue(key, out float value)
                   && value > 0f;
        }
    }
}
