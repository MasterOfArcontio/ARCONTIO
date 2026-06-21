namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiOperationKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo essenziale di intenzione rappresentata da una operazione UI ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: distinguere cosa vuole fare la UI</b></para>
    /// <para>
    /// La UI deve poter descrivere se l'utente sta inserendo, modificando,
    /// eliminando o cambiando una modalita' visuale senza conoscere il comando Core
    /// finale. Questa enum resta piccola: non modella ogni caso futuro, ma separa le
    /// famiglie di intenzioni gia' emerse dalla progettazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessuna intenzione valida.</item>
    ///   <item><b>Insert</b>: inserimento di un elemento nel mondo tramite richiesta autorizzata.</item>
    ///   <item><b>Edit</b>: modifica configurata di un target gia' selezionato.</item>
    ///   <item><b>Delete</b>: eliminazione richiesta su un target gia' selezionato.</item>
    ///   <item><b>SetState</b>: scelta tra stati discreti, ad esempio velocita' simulativa.</item>
    ///   <item><b>ToggleView</b>: accensione o spegnimento di una visualizzazione.</item>
    ///   <item><b>ToolMode</b>: cambio di modalita' strumento, ad esempio singolo o brush.</item>
    /// </list>
    /// </summary>
    public enum ArcUiOperationKind
    {
        None = 0,
        Insert = 1,
        Edit = 2,
        Delete = 3,
        SetState = 4,
        ToggleView = 5,
        ToolMode = 6
    }

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
    /// Una operation definition descrive cosa mostrare, dove mostrarlo nel pannello
    /// azione e quale tipo di intenzione raccogliere. Non contiene riferimenti a
    /// <c>World</c>, non costruisce comandi, non conosce controller concreti e non
    /// decide policy simulativa. Lo step <c>v0.70.03</c> introdurra' il ponte che
    /// trasformera' una scelta UI in una richiesta autorizzata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>OperationKey</b>: chiave stabile della operazione.</item>
    ///   <item><b>Label</b>: testo mostrabile dalla UI.</item>
    ///   <item><b>IconKey</b>: chiave icona, non riferimento diretto a sprite.</item>
    ///   <item><b>ActionKey</b>: pulsante della bottom action bar che apre il pannello.</item>
    ///   <item><b>GroupKey</b>: macrogruppo sinistro del pannello azione.</item>
    ///   <item><b>OperationKind</b>: famiglia di intenzione UI.</item>
    ///   <item><b>TargetKind</b>: tipo essenziale di bersaglio operativo.</item>
    ///   <item><b>TargetDefId</b>: id di variante catalogo quando la operation inserisce qualcosa.</item>
    ///   <item><b>RequiresPreview</b>: indica se serve una preview su mappa.</item>
    ///   <item><b>RequiresConfiguration</b>: indica se serve pannello config prima del click.</item>
    ///   <item><b>SupportsBrush</b>: indica se l'inserimento puo' usare modalita' brush.</item>
    ///   <item><b>DebugOnly</b>: limita l'operazione a strumenti di sviluppo.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcUiOperationDefinition
    {
        public readonly string OperationKey;
        public readonly string Label;
        public readonly string IconKey;
        public readonly string ActionKey;
        public readonly string GroupKey;
        public readonly ArcUiOperationKind OperationKind;
        public readonly ArcUiOperationTargetKind TargetKind;
        public readonly string TargetDefId;
        public readonly bool RequiresPreview;
        public readonly bool RequiresConfiguration;
        public readonly bool SupportsBrush;
        public readonly bool DebugOnly;

        public bool IsValid => !string.IsNullOrEmpty(OperationKey) && OperationKind != ArcUiOperationKind.None;

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
        /// Il costruttore accetta solo i dati richiesti dalla roadmap immediata:
        /// posizione nel pannello, variante scelta, famiglia operativa e flag UI.
        /// Campi come controller, tipo comando, permessi dinamici o payload generici
        /// verranno introdotti solo se uno step successivo ne avra' bisogno reale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>operationKey</b>: viene trim-ato e portato in minuscolo.</item>
        ///   <item><b>action/group/targetDef</b>: sono chiavi testuali normalizzate.</item>
        ///   <item><b>flag</b>: restano valori semplici e leggibili.</item>
        /// </list>
        /// </summary>
        public ArcUiOperationDefinition(
            string operationKey,
            string label,
            string iconKey,
            string actionKey,
            string groupKey,
            ArcUiOperationKind operationKind,
            ArcUiOperationTargetKind targetKind,
            string targetDefId,
            bool requiresPreview,
            bool requiresConfiguration,
            bool supportsBrush,
            bool debugOnly)
        {
            // Le chiavi tecniche vengono normalizzate tutte nello stesso modo per
            // rendere stabile il confronto tra catalogo, controller e UI.
            OperationKey = NormalizeKey(operationKey);
            Label = NormalizeText(label);
            IconKey = NormalizeKey(iconKey);
            ActionKey = NormalizeKey(actionKey);
            GroupKey = NormalizeKey(groupKey);
            OperationKind = operationKind;
            TargetKind = targetKind;
            TargetDefId = NormalizeKey(targetDefId);

            // Questi flag non eseguono logica: descrivono solo cosa dovra'
            // preparare la UI quando la operation verra' selezionata.
            RequiresPreview = requiresPreview;
            RequiresConfiguration = requiresConfiguration;
            SupportsBrush = supportsBrush;
            DebugOnly = debugOnly;
        }

        // =============================================================================
        // ArcUiOperationDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruttore compatibile con la foundation iniziale <c>v0.70.01</c>.
        /// </para>
        ///
        /// <para><b>Compatibilita' di step</b></para>
        /// <para>
        /// Mantiene utilizzabile la forma precedente del contratto mentre il codice
        /// viene migrato verso la gerarchia azione/gruppo/operazione. La categoria
        /// precedente viene trattata come <c>ActionKey</c>.
        /// </para>
        /// </summary>
        public ArcUiOperationDefinition(
            string operationKey,
            string label,
            string category,
            ArcUiOperationTargetKind targetKind,
            bool requiresPreview,
            bool requiresConfiguration,
            bool debugOnly)
            : this(
                operationKey,
                label,
                string.Empty,
                category,
                string.Empty,
                ArcUiOperationKind.Insert,
                targetKind,
                string.Empty,
                requiresPreview,
                requiresConfiguration,
                false,
                debugOnly)
        {
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
