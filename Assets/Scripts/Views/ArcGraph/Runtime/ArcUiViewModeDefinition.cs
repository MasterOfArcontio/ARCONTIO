namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiVisualOverlayKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Famiglia visuale degli overlay attivabili sopra la viewport ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: overlay come osservazione, non comando</b></para>
    /// <para>
    /// Questi valori non rappresentano azioni sul mondo e non sono mutualmente
    /// esclusivi. La visuale normale resta sempre attiva; ogni overlay puo' essere
    /// acceso o spento sopra la visuale principale, senza modificare la
    /// simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna famiglia visuale.</item>
    ///   <item><b>Landmark</b>: nodi/edge landmark e grafi diagnostici collegati.</item>
    ///   <item><b>LineOfSight</b>: linee di vista o campo visivo degli NPC.</item>
    ///   <item><b>Pathfinding</b>: percorsi, celle esplorate o diagnostica path.</item>
    ///   <item><b>Perception</b>: debug percezione.</item>
    ///   <item><b>Belief</b>: overlay belief/debug cognitivo.</item>
    ///   <item><b>Memory</b>: overlay memory/debug cognitivo.</item>
    ///   <item><b>Biosphere</b>: overlay futuro per letture ambientali/biosfera.</item>
    ///   <item><b>Resources</b>: overlay futuro per risorse mappa/magazzini.</item>
    /// </list>
    /// </summary>
    public enum ArcUiVisualOverlayKind
    {
        None = 0,
        Landmark = 1,
        LineOfSight = 2,
        Pathfinding = 3,
        Perception = 4,
        Belief = 5,
        Memory = 6,
        Biosphere = 7,
        Resources = 8,
        Area = 9
    }

    // =============================================================================
    // ArcUiVisualOverlayDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione data-only di un singolo overlay visuale attivabile dalla UI.
    /// </para>
    ///
    /// <para><b>Principio architetturale: Visuale normale + overlay indipendenti</b></para>
    /// <para>
    /// La definizione descrive un toggle, non una modalita' esclusiva. Il click
    /// sulla futura icona dovra' produrre una richiesta verso il controller, che
    /// aggiornera' soltanto lo stato UI. Renderer e producer leggeranno poi lo
    /// snapshot degli overlay attivi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>OverlayKey</b>: chiave stabile del toggle.</item>
    ///   <item><b>Label</b>: testo leggibile o tooltip futuro.</item>
    ///   <item><b>IconKey</b>: chiave icona, non riferimento diretto a sprite.</item>
    ///   <item><b>Kind</b>: famiglia visuale dell'overlay.</item>
    ///   <item><b>RequiresSelectedNpc</b>: true se serve un NPC selezionato.</item>
    ///   <item><b>DebugOnly</b>: true se va esposto solo in debug/sviluppo.</item>
    ///   <item><b>EnabledByDefault</b>: stato iniziale suggerito.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiVisualOverlayDefinition
    {
        public readonly string OverlayKey;
        public readonly string Label;
        public readonly string IconKey;
        public readonly ArcUiVisualOverlayKind Kind;
        public readonly bool RequiresSelectedNpc;
        public readonly bool DebugOnly;
        public readonly bool EnabledByDefault;

        public bool IsValid => !string.IsNullOrEmpty(OverlayKey);

        // =============================================================================
        // ArcUiVisualOverlayDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione overlay minimale e normalizzata.
        /// </para>
        /// </summary>
        public ArcUiVisualOverlayDefinition(
            string overlayKey,
            string label,
            string iconKey,
            ArcUiVisualOverlayKind kind,
            bool requiresSelectedNpc,
            bool debugOnly,
            bool enabledByDefault)
        {
            OverlayKey = ArcUiOperationDefinition.NormalizeKey(overlayKey);
            Label = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
            IconKey = ArcUiOperationDefinition.NormalizeKey(iconKey);
            Kind = kind;
            RequiresSelectedNpc = requiresSelectedNpc;
            DebugOnly = debugOnly;
            EnabledByDefault = enabledByDefault;
        }
    }

    // =============================================================================
    // ArcUiVisualOverlayCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo minimo degli overlay visuali previsti dalla UI runtime ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo UI separato dal renderer</b></para>
    /// <para>
    /// Il catalogo non accende LineRenderer, non costruisce debug queue e non legge
    /// il mondo. Espone soltanto i toggle che la UI potra' mostrare nella TopBar o
    /// in un blocco visuale integrato.
    /// </para>
    /// </summary>
    public static class ArcUiVisualOverlayCatalog
    {
        public const string LandmarksKey = "landmarks";
        public const string NpcLineOfSightKey = "npc_los";
        public const string PathfindingKey = "pathfinding";
        public const string PerceptionDebugKey = "perception_debug";
        public const string BeliefOverlayKey = "belief_overlay";
        public const string MemoryOverlayKey = "memory_overlay";
        public const string BiosphereOverlayKey = "biosphere_overlay";
        public const string ResourcesOverlayKey = "resources_overlay";
        public const string AreaOverlayKey = "area_overlay";

        private static readonly ArcUiVisualOverlayDefinition[] Definitions =
        {
            new ArcUiVisualOverlayDefinition(
                LandmarksKey,
                "Landmark",
                "icon_landmark",
                ArcUiVisualOverlayKind.Landmark,
                requiresSelectedNpc: false,
                debugOnly: false,
                enabledByDefault: false),
            new ArcUiVisualOverlayDefinition(
                NpcLineOfSightKey,
                "LOS NPC",
                "icon_los",
                ArcUiVisualOverlayKind.LineOfSight,
                requiresSelectedNpc: true,
                debugOnly: false,
                enabledByDefault: false),
            new ArcUiVisualOverlayDefinition(
                PathfindingKey,
                "Pathfinding",
                "icon_pathfinding",
                ArcUiVisualOverlayKind.Pathfinding,
                requiresSelectedNpc: false,
                debugOnly: true,
                enabledByDefault: false),
            new ArcUiVisualOverlayDefinition(
                PerceptionDebugKey,
                "Percezione",
                "icon_perception",
                ArcUiVisualOverlayKind.Perception,
                requiresSelectedNpc: true,
                debugOnly: true,
                enabledByDefault: false),
            new ArcUiVisualOverlayDefinition(
                BeliefOverlayKey,
                "Belief",
                "icon_belief",
                ArcUiVisualOverlayKind.Belief,
                requiresSelectedNpc: true,
                debugOnly: true,
                enabledByDefault: false),
            new ArcUiVisualOverlayDefinition(
                MemoryOverlayKey,
                "Memory",
                "icon_memory",
                ArcUiVisualOverlayKind.Memory,
                requiresSelectedNpc: true,
                debugOnly: true,
                enabledByDefault: false),
            new ArcUiVisualOverlayDefinition(
                BiosphereOverlayKey,
                "Biosfera",
                "icon_biosphere",
                ArcUiVisualOverlayKind.Biosphere,
                requiresSelectedNpc: false,
                debugOnly: true,
                enabledByDefault: false),
            new ArcUiVisualOverlayDefinition(
                ResourcesOverlayKey,
                "Risorse",
                "icon_resources",
                ArcUiVisualOverlayKind.Resources,
                requiresSelectedNpc: false,
                debugOnly: true,
                enabledByDefault: false),
            new ArcUiVisualOverlayDefinition(
                AreaOverlayKey,
                "AREA",
                "icon_area",
                ArcUiVisualOverlayKind.Area,
                requiresSelectedNpc: false,
                debugOnly: false,
                enabledByDefault: false)
        };

        public static int Count => Definitions.Length;

        // =============================================================================
        // GetDefinitions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il catalogo statico degli overlay.
        /// </para>
        /// </summary>
        public static ArcUiVisualOverlayDefinition[] GetDefinitions()
        {
            ArcUiVisualOverlayDefinition[] copy = new ArcUiVisualOverlayDefinition[Definitions.Length];
            for (int i = 0; i < Definitions.Length; i++)
                copy[i] = Definitions[i];

            return copy;
        }

        // =============================================================================
        // TryGet
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca una definizione overlay per chiave normalizzata.
        /// </para>
        /// </summary>
        public static bool TryGet(
            string overlayKey,
            out ArcUiVisualOverlayDefinition definition)
        {
            string normalized = ArcUiOperationDefinition.NormalizeKey(overlayKey);
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].OverlayKey == normalized)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }
    }

    // =============================================================================
    // ArcUiVisualOverlayRequestKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo di richiesta UI per cambiare lo stato degli overlay visuali.
    /// </para>
    /// </summary>
    public enum ArcUiVisualOverlayRequestKind
    {
        None = 0,
        Toggle = 1,
        SetEnabled = 2,
        ClearAll = 3
    }

    // =============================================================================
    // ArcUiVisualOverlayRequest
    // =============================================================================
    /// <summary>
    /// <para>
    /// Richiesta data-only generata da un futuro click su icona overlay.
    /// </para>
    ///
    /// <para><b>Principio architetturale: icona -> richiesta -> controller</b></para>
    /// <para>
    /// La richiesta non contiene riferimenti a renderer, NPC o World. Descrive solo
    /// quale overlay l'utente vuole accendere, spegnere o invertire.
    /// </para>
    /// </summary>
    public readonly struct ArcUiVisualOverlayRequest
    {
        public readonly ArcUiVisualOverlayRequestKind Kind;
        public readonly string OverlayKey;
        public readonly bool Enabled;
        public readonly string Source;

        public bool IsValid => Kind != ArcUiVisualOverlayRequestKind.None
            && (Kind == ArcUiVisualOverlayRequestKind.ClearAll || !string.IsNullOrEmpty(OverlayKey));

        private ArcUiVisualOverlayRequest(
            ArcUiVisualOverlayRequestKind kind,
            string overlayKey,
            bool enabled,
            string source)
        {
            Kind = kind;
            OverlayKey = ArcUiOperationDefinition.NormalizeKey(overlayKey);
            Enabled = enabled;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public static ArcUiVisualOverlayRequest Toggle(string overlayKey, string source)
        {
            return new ArcUiVisualOverlayRequest(
                ArcUiVisualOverlayRequestKind.Toggle,
                overlayKey,
                false,
                source);
        }

        public static ArcUiVisualOverlayRequest SetEnabled(
            string overlayKey,
            bool enabled,
            string source)
        {
            return new ArcUiVisualOverlayRequest(
                ArcUiVisualOverlayRequestKind.SetEnabled,
                overlayKey,
                enabled,
                source);
        }

        public static ArcUiVisualOverlayRequest ClearAll(string source)
        {
            return new ArcUiVisualOverlayRequest(
                ArcUiVisualOverlayRequestKind.ClearAll,
                string.Empty,
                false,
                source);
        }
    }

    // =============================================================================
    // ArcUiVisualOverlayState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot dello stato on/off degli overlay visuali ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stato compositivo non esclusivo</b></para>
    /// <para>
    /// Lo stato conserva piu' overlay attivi contemporaneamente. Questo permette
    /// casi come Landmark + LOS + Pathfinding sopra la visuale normale.
    /// </para>
    /// </summary>
    public readonly struct ArcUiVisualOverlayState
    {
        private readonly string[] _enabledOverlayKeys;

        public int EnabledCount => _enabledOverlayKeys == null ? 0 : _enabledOverlayKeys.Length;
        public bool HasAnyEnabled => EnabledCount > 0;

        public ArcUiVisualOverlayState(string[] enabledOverlayKeys)
        {
            _enabledOverlayKeys = CopyValidKeys(enabledOverlayKeys);
        }

        public bool IsEnabled(string overlayKey)
        {
            string normalized = ArcUiOperationDefinition.NormalizeKey(overlayKey);
            if (string.IsNullOrEmpty(normalized) || _enabledOverlayKeys == null)
                return false;

            for (int i = 0; i < _enabledOverlayKeys.Length; i++)
            {
                if (_enabledOverlayKeys[i] == normalized)
                    return true;
            }

            return false;
        }

        public string[] GetEnabledOverlayKeys()
        {
            if (_enabledOverlayKeys == null || _enabledOverlayKeys.Length == 0)
                return new string[0];

            string[] copy = new string[_enabledOverlayKeys.Length];
            for (int i = 0; i < _enabledOverlayKeys.Length; i++)
                copy[i] = _enabledOverlayKeys[i];

            return copy;
        }

        public static ArcUiVisualOverlayState Empty()
        {
            return new ArcUiVisualOverlayState(new string[0]);
        }

        private static string[] CopyValidKeys(string[] source)
        {
            if (source == null || source.Length == 0)
                return new string[0];

            string[] temp = new string[source.Length];
            int count = 0;
            for (int i = 0; i < source.Length; i++)
            {
                string key = ArcUiOperationDefinition.NormalizeKey(source[i]);
                if (string.IsNullOrEmpty(key) || Contains(temp, count, key))
                    continue;

                temp[count] = key;
                count++;
            }

            string[] result = new string[count];
            for (int i = 0; i < count; i++)
                result[i] = temp[i];

            return result;
        }

        private static bool Contains(string[] values, int count, string key)
        {
            for (int i = 0; i < count; i++)
            {
                if (values[i] == key)
                    return true;
            }

            return false;
        }
    }

    // =============================================================================
    // ArcUiViewOverlayKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Compatibilita' temporanea con il vecchio nome usato nella foundation v0.70.01.
    /// </para>
    /// </summary>
    public enum ArcUiViewOverlayKind
    {
        None = 0,
        Fov = 1,
        Path = 2,
        Occupancy = 3,
        NpcMemory = 4
    }

    // =============================================================================
    // ArcUiViewModeDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Compatibilita' temporanea per vecchie chiamate basate su view mode esclusiva.
    /// </para>
    ///
    /// <para>
    /// Il modello corretto da usare negli step nuovi e' <see cref="ArcUiVisualOverlayDefinition"/>.
    /// </para>
    /// </summary>
    public readonly struct ArcUiViewModeDefinition
    {
        public readonly string ViewModeKey;
        public readonly string Label;
        public readonly ArcUiViewOverlayKind OverlayKind;
        public readonly bool RequiresSelectedNpc;
        public readonly bool DebugOnly;

        public bool IsValid => !string.IsNullOrEmpty(ViewModeKey);

        public ArcUiViewModeDefinition(
            string viewModeKey,
            string label,
            ArcUiViewOverlayKind overlayKind,
            bool requiresSelectedNpc,
            bool debugOnly)
        {
            ViewModeKey = ArcUiOperationDefinition.NormalizeKey(viewModeKey);
            Label = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
            OverlayKind = overlayKind;
            RequiresSelectedNpc = requiresSelectedNpc;
            DebugOnly = debugOnly;
        }
    }
}
