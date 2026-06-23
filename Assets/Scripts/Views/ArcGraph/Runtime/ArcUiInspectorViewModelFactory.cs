namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiInspectorViewModelFactory
    // =============================================================================
    /// <summary>
    /// <para>
    /// Factory minima per costruire ViewModel del RightInspector ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dati inspector preparati prima della UI</b></para>
    /// <para>
    /// La factory traduce contratti UI gia' autorizzati, come
    /// <see cref="ArcUiSelectionTarget"/> e <see cref="ArcUiSelectionActionRequest"/>,
    /// in un <see cref="ArcUiInspectorViewModel"/> leggibile dal pannello. Non
    /// legge il <c>World</c>, non interroga NPC runtime, non risolve oggetti reali,
    /// non invia comandi e non apre pannelli Unity. In questo modo il
    /// RightInspector resta un renderer di ViewModel e non diventa un nuovo
    /// DevTools monolitico.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildForSelection</b>: crea tab read-only da un target selezionato.</item>
    ///   <item><b>BuildForAction</b>: crea tab per richiesta Edit/Delete non distruttiva.</item>
    ///   <item><b>Build*Tabs</b>: separa il vocabolario iniziale di NPC, oggetti, muri e celle.</item>
    ///   <item><b>CommonRows</b>: righe minime condivise, basate solo sul target.</item>
    /// </list>
    /// </summary>
    public sealed class ArcUiInspectorViewModelFactory
    {
        private const string InfoTabKey = "info";
        private const string EditTabKey = "edit";
        private const string DeleteTabKey = "delete";

        private ArcUiInspectorRuntimeSnapshotProvider _runtimeSnapshotProvider;

        // =============================================================================
        // SetRuntimeSnapshotProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il provider opzionale che puo' produrre snapshot runtime
        /// read-only gia' formattati per l'inspector.
        /// </para>
        ///
        /// <para><b>Boundary separato dalla view</b></para>
        /// <para>
        /// La factory continua a ricevere contratti UI. Quando esiste un provider
        /// autorizzato, delega a lui la costruzione del ViewModel specifico invece
        /// di far leggere al pannello UGUI strutture runtime mutabili.
        /// </para>
        /// </summary>
        public void SetRuntimeSnapshotProvider(ArcUiInspectorRuntimeSnapshotProvider provider)
        {
            _runtimeSnapshotProvider = provider;
        }

        // =============================================================================
        // BuildForSelection
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un ViewModel read-only per il target selezionato.
        /// </para>
        ///
        /// <para><b>Boundary read-only</b></para>
        /// <para>
        /// Il metodo accetta solo il target gia' prodotto dal layer selezione. Non
        /// effettua hit test, non scansiona render queue e non legge strutture
        /// simulativo-mutabili.
        /// </para>
        /// </summary>
        public ArcUiInspectorViewModel BuildForSelection(ArcUiSelectionTarget target)
        {
            if (!target.IsValid)
                return ArcUiInspectorViewModel.Empty();

            if (target.Kind == ArcUiSelectionTargetKind.Npc
                && _runtimeSnapshotProvider != null
                && _runtimeSnapshotProvider.TryBuildNpcViewModel(target, out ArcUiInspectorViewModel npcViewModel))
            {
                return npcViewModel;
            }

            if ((target.Kind == ArcUiSelectionTargetKind.Object || target.Kind == ArcUiSelectionTargetKind.Wall)
                && _runtimeSnapshotProvider != null
                && _runtimeSnapshotProvider.TryBuildObjectViewModel(target, out ArcUiInspectorViewModel objectViewModel))
            {
                return objectViewModel;
            }

            ArcUiInspectorTab[] tabs = target.Kind switch
            {
                ArcUiSelectionTargetKind.Npc => BuildNpcTabs(target),
                ArcUiSelectionTargetKind.Object => BuildObjectTabs(target),
                ArcUiSelectionTargetKind.Wall => BuildWallTabs(target),
                ArcUiSelectionTargetKind.Cell => BuildCellTabs(target),
                _ => BuildGenericTabs(target)
            };

            return new ArcUiInspectorViewModel(
                ResolveTitle(target),
                target,
                tabs,
                InfoTabKey);
        }

        // =============================================================================
        // BuildForAction
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un ViewModel per una richiesta rapida Modifica/Elimina.
        /// </para>
        ///
        /// <para><b>Richiesta non distruttiva</b></para>
        /// <para>
        /// La richiesta resta solo stato UI. Il ViewModel informa il pannello che
        /// l'utente ha chiesto una modifica o eliminazione, ma non contiene
        /// istruzioni operative per applicarla.
        /// </para>
        /// </summary>
        public ArcUiInspectorViewModel BuildForAction(ArcUiSelectionActionRequest request)
        {
            if (!request.IsValid)
                return ArcUiInspectorViewModel.Empty();

            if (request.IsEdit
                && request.Target.Kind == ArcUiSelectionTargetKind.Npc
                && _runtimeSnapshotProvider != null
                && _runtimeSnapshotProvider.TryBuildNpcEditViewModel(request, out ArcUiInspectorViewModel npcEditViewModel))
            {
                return npcEditViewModel;
            }

            if (request.IsEdit
                && (request.Target.Kind == ArcUiSelectionTargetKind.Object || request.Target.Kind == ArcUiSelectionTargetKind.Wall)
                && _runtimeSnapshotProvider != null
                && _runtimeSnapshotProvider.TryBuildObjectEditViewModel(request, out ArcUiInspectorViewModel objectEditViewModel))
            {
                return objectEditViewModel;
            }

            ArcUiInspectorTab[] tabs = request.IsEdit
                ? BuildEditTabs(request)
                : BuildDeleteTabs(request);

            return new ArcUiInspectorViewModel(
                ResolveTitle(request.Target),
                request.Target,
                tabs,
                request.IsEdit ? EditTabKey : DeleteTabKey);
        }

        private static ArcUiInspectorTab[] BuildNpcTabs(ArcUiSelectionTarget target)
        {
            return new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", CommonRows("Ispezione", target)),
                PlaceholderTab("dna", "DNA", "Snapshot DNA non ancora collegato"),
                PlaceholderTab("memory", "Memoria", "Memory trace non ancora collegata"),
                PlaceholderTab("belief", "Belief", "Belief snapshot non ancora collegato"),
                PlaceholderTab("decision", "Decision", "Decision/query non ancora collegate"),
                PlaceholderTab("job", "Job", "Job/fallback non ancora collegati"),
                PlaceholderTab("path", "Path", "Pathfinding non ancora collegato"),
                PlaceholderTab("debug", "Debug", "Debug NPC non ancora collegato")
            };
        }

        private static ArcUiInspectorTab[] BuildObjectTabs(ArcUiSelectionTarget target)
        {
            return new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", CommonRows("Ispezione", target)),
                PlaceholderTab("state", "Stato", "Stato oggetto non ancora collegato"),
                PlaceholderTab("use", "Uso", "Uso oggetto non ancora collegato"),
                PlaceholderTab("storage", "Storage", "Storage non ancora collegato"),
                PlaceholderTab("debug", "Debug", "Debug oggetto non ancora collegato")
            };
        }

        private static ArcUiInspectorTab[] BuildWallTabs(ArcUiSelectionTarget target)
        {
            return new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", CommonRows("Ispezione", target)),
                PlaceholderTab("material", "Materiale", "Materiale/variante non ancora collegati"),
                PlaceholderTab("connections", "Connessioni", "Connessioni muro non ancora collegate"),
                PlaceholderTab("state", "Stato", "Stato muro non ancora collegato"),
                PlaceholderTab("debug", "Debug", "Debug muro non ancora collegato")
            };
        }

        private static ArcUiInspectorTab[] BuildCellTabs(ArcUiSelectionTarget target)
        {
            return new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", CommonRows("Ispezione", target)),
                PlaceholderTab("surface", "Superficie", "Info superficie non ancora collegata"),
                PlaceholderTab("occupation", "Occupazione", "Occupazione cella non ancora collegata"),
                PlaceholderTab("debug", "Debug", "Debug cella non ancora collegato")
            };
        }

        private static ArcUiInspectorTab[] BuildGenericTabs(ArcUiSelectionTarget target)
        {
            return new[]
            {
                new ArcUiInspectorTab(InfoTabKey, "Info", CommonRows("Ispezione", target)),
                PlaceholderTab("debug", "Debug", "Debug target non ancora collegato")
            };
        }

        private static ArcUiInspectorTab[] BuildEditTabs(ArcUiSelectionActionRequest request)
        {
            return new[]
            {
                new ArcUiInspectorTab(
                    EditTabKey,
                    "Modifica",
                    CombineRows(
                        CommonRows("Modifica richiesta", request.Target),
                        new[]
                        {
                            new ArcUiInspectorRow("Stato", "Richiesta ricevuta"),
                            new ArcUiInspectorRow("Effetto", "Nessuna modifica applicata"),
                            new ArcUiInspectorRow("Prossimo ponte", "Edit ViewModel autorizzato")
                        })),
                new ArcUiInspectorTab(InfoTabKey, "Dati", CommonRows("Dati target", request.Target))
            };
        }

        private static ArcUiInspectorTab[] BuildDeleteTabs(ArcUiSelectionActionRequest request)
        {
            return new[]
            {
                new ArcUiInspectorTab(
                    DeleteTabKey,
                    "Conferma",
                    CombineRows(
                        CommonRows("Eliminazione richiesta", request.Target),
                        new[]
                        {
                            new ArcUiInspectorRow("Stato", "Richiesta ricevuta"),
                            new ArcUiInspectorRow("Effetto", "Nessuna eliminazione applicata"),
                            new ArcUiInspectorRow("Prossimo ponte", "Conferma + Command Gateway")
                        })),
                new ArcUiInspectorTab(InfoTabKey, "Dati", CommonRows("Dati target", request.Target))
            };
        }

        private static ArcUiInspectorTab PlaceholderTab(
            string tabKey,
            string label,
            string message)
        {
            return new ArcUiInspectorTab(
                tabKey,
                label,
                new[]
                {
                    new ArcUiInspectorRow("Stato", "Non collegato"),
                    new ArcUiInspectorRow("Dettaglio", message)
                });
        }

        private static ArcUiInspectorRow[] CommonRows(
            string modeLabel,
            ArcUiSelectionTarget target)
        {
            return new[]
            {
                new ArcUiInspectorRow("Modalita'", modeLabel),
                new ArcUiInspectorRow("Tipo", ResolveTargetKindLabel(target.Kind)),
                new ArcUiInspectorRow("Id", string.IsNullOrWhiteSpace(target.Id) ? "--" : target.Id),
                new ArcUiInspectorRow("Nome", string.IsNullOrWhiteSpace(target.DisplayName) ? "--" : target.DisplayName),
                new ArcUiInspectorRow("Cella", FormatCell(target.Cell)),
                new ArcUiInspectorRow("Sorgente", string.IsNullOrWhiteSpace(target.SourceView) ? "--" : target.SourceView)
            };
        }

        private static ArcUiInspectorRow[] CombineRows(
            ArcUiInspectorRow[] first,
            ArcUiInspectorRow[] second)
        {
            if (first == null || first.Length == 0)
                return second ?? System.Array.Empty<ArcUiInspectorRow>();

            if (second == null || second.Length == 0)
                return first;

            var combined = new ArcUiInspectorRow[first.Length + second.Length];
            first.CopyTo(combined, 0);
            second.CopyTo(combined, first.Length);
            return combined;
        }

        private static string ResolveTitle(ArcUiSelectionTarget target)
        {
            if (!string.IsNullOrWhiteSpace(target.DisplayName))
                return target.DisplayName;

            if (!string.IsNullOrWhiteSpace(target.Id))
                return ResolveTargetKindLabel(target.Kind) + " " + target.Id;

            return ResolveTargetKindLabel(target.Kind);
        }

        private static string ResolveTargetKindLabel(ArcUiSelectionTargetKind kind)
        {
            return kind switch
            {
                ArcUiSelectionTargetKind.Npc => "NPC",
                ArcUiSelectionTargetKind.Object => "Oggetto",
                ArcUiSelectionTargetKind.Wall => "Muro",
                ArcUiSelectionTargetKind.Cell => "Cella",
                ArcUiSelectionTargetKind.Plant => "Pianta",
                ArcUiSelectionTargetKind.Zone => "Zona",
                ArcUiSelectionTargetKind.Debug => "Debug",
                _ => "Nessun target"
            };
        }

        private static string FormatCell(ArcGraphCellCoord cell)
        {
            return "col " + cell.X + " | riga " + cell.Y + " | z " + cell.Z;
        }
    }
}
