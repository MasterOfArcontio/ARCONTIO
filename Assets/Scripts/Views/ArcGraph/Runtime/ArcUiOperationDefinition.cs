namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiOperationTargetKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo essenziale di bersaglio operativo che una azione UI ArcGraph intende
    /// preparare.
    /// </para>
    ///
    /// <para><b>Principio architetturale: intenzione UI senza mutazione</b></para>
    /// <para>
    /// Questa enum non descrive regole del mondo e non decide se una azione sia
    /// possibile. Serve solo a classificare in modo leggibile cosa l'utente sta
    /// cercando di operare dalla UI runtime. La verifica reale restera' nel layer
    /// autorizzato che produrra' il comando futuro.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessun target operativo valido.</item>
    ///   <item><b>Cell</b>: operazione su una cella.</item>
    ///   <item><b>Wall</b>: operazione di muro o struttura lineare equivalente.</item>
    ///   <item><b>Object</b>: operazione su oggetto piazzabile.</item>
    ///   <item><b>Npc</b>: operazione su NPC o spawn NPC.</item>
    ///   <item><b>Zone</b>: operazione su zona futura.</item>
    /// </list>
    /// </summary>
    public enum ArcUiOperationTargetKind
    {
        None = 0,
        Cell = 1,
        Wall = 2,
        Object = 3,
        Npc = 4,
        Zone = 5
    }

    // =============================================================================
    // ArcUiOperationDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione minima di una operazione disponibile nella UI runtime ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo azioni separato dai comandi</b></para>
    /// <para>
    /// Una operation definition descrive cosa mostrare e quale tipo di intenzione
    /// raccogliere. Non contiene riferimenti a <c>World</c>, non costruisce comandi,
    /// non conosce controller concreti e non decide policy simulativa. Lo step
    /// <c>v0.70.03</c> introdurra' il ponte che trasformera' una scelta UI in una
    /// richiesta autorizzata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>OperationKey</b>: chiave stabile della operazione.</item>
    ///   <item><b>Label</b>: testo mostrabile dalla UI.</item>
    ///   <item><b>Category</b>: gruppo visivo principale della action bar.</item>
    ///   <item><b>TargetKind</b>: tipo essenziale di bersaglio operativo.</item>
    ///   <item><b>RequiresPreview</b>: indica se serve una preview su mappa.</item>
    ///   <item><b>RequiresConfiguration</b>: indica se serve pannello config prima del click.</item>
    ///   <item><b>DebugOnly</b>: limita l'operazione a strumenti di sviluppo.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiOperationDefinition
    {
        public readonly string OperationKey;
        public readonly string Label;
        public readonly string Category;
        public readonly ArcUiOperationTargetKind TargetKind;
        public readonly bool RequiresPreview;
        public readonly bool RequiresConfiguration;
        public readonly bool DebugOnly;

        public bool IsValid => !string.IsNullOrEmpty(OperationKey) && TargetKind != ArcUiOperationTargetKind.None;

        // =============================================================================
        // ArcUiOperationDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione operazione normalizzando solo i campi necessari.
        /// </para>
        ///
        /// <para><b>Contratto asciutto</b></para>
        /// <para>
        /// Il costruttore accetta solo i dati richiesti dalla roadmap immediata.
        /// Campi come controller, tipo comando, permessi dinamici o payload generici
        /// verranno introdotti solo se uno step successivo ne avra' bisogno reale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>operationKey</b>: viene trim-ato e portato in minuscolo.</item>
        ///   <item><b>label/category</b>: vengono trim-ati, ma non interpretati.</item>
        ///   <item><b>flag</b>: restano valori semplici e leggibili.</item>
        /// </list>
        /// </summary>
        public ArcUiOperationDefinition(
            string operationKey,
            string label,
            string category,
            ArcUiOperationTargetKind targetKind,
            bool requiresPreview,
            bool requiresConfiguration,
            bool debugOnly)
        {
            OperationKey = NormalizeKey(operationKey);
            Label = NormalizeText(label);
            Category = NormalizeText(category);
            TargetKind = targetKind;
            RequiresPreview = requiresPreview;
            RequiresConfiguration = requiresConfiguration;
            DebugOnly = debugOnly;
        }

        // =============================================================================
        // NormalizeKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza una chiave tecnica senza assegnarle significato simulativo.
        /// </para>
        ///
        /// <para><b>Stabilita' dei cataloghi</b></para>
        /// <para>
        /// Le chiavi UI devono essere confrontabili senza dipendere da maiuscole,
        /// spazi o formattazione editoriale. Questa normalizzazione resta locale al
        /// contratto UI e non modifica cataloghi Core.
        /// </para>
        /// </summary>
        public static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        // =============================================================================
        // NormalizeText
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza un testo visuale mantenendolo leggibile per la UI.
        /// </para>
        /// </summary>
        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
