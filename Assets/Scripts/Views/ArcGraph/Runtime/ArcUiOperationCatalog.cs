namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcUiOperationCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Primo catalogo statico minimale delle azioni, dei gruppi e delle operazioni
    /// UI runtime ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo UI non esecutivo</b></para>
    /// <para>
    /// Questo catalogo prepara dati per la futura bottom action bar e per il futuro
    /// pannello azione. Non legge <c>World</c>, non invia comandi, non apre inspector
    /// e non sostituisce il vecchio F3. Le chiavi di definizione, come
    /// <c>bed_wood</c> o <c>npc_human</c>, sono riferimenti testuali che un adapter
    /// autorizzato potra' risolvere piu' avanti verso i cataloghi runtime reali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Actions</b>: pulsanti principali della bottom action bar.</item>
    ///   <item><b>Groups</b>: macrogruppi sinistri del pannello azione.</item>
    ///   <item><b>Operations</b>: icone operative concrete, gia' orientate alle varianti.</item>
    ///   <item><b>Find/Get</b>: filtri leggeri per alimentare la UI futura.</item>
    ///   <item><b>TryFindDuplicateOperationKey</b>: validazione minima contro chiavi duplicate.</item>
    /// </list>
    /// </summary>
    public static class ArcUiOperationCatalog
    {
        private static readonly ArcUiActionDefinition[] Actions =
        {
            new ArcUiActionDefinition("build", "Costruisci", "build", true, false),
            new ArcUiActionDefinition("insert", "Inserisci", "insert", true, false),
            new ArcUiActionDefinition("jobs", "Gestisci lavori", "jobs", true, true),
            new ArcUiActionDefinition("zones", "Zone", "zones", true, true),
            new ArcUiActionDefinition("objects", "Oggetti", "objects", true, false),
            new ArcUiActionDefinition("npc", "NPC", "npc", true, false),
            new ArcUiActionDefinition("institutions", "Istituzioni", "institutions", true, true),
            new ArcUiActionDefinition("research", "Ricerca", "research", true, true)
        };

        private static readonly ArcUiActionGroupDefinition[] Groups =
        {
            new ArcUiActionGroupDefinition("build", "structures", "Strutture", false),
            new ArcUiActionGroupDefinition("build", "furniture", "Mobili", false),
            new ArcUiActionGroupDefinition("build", "production", "Produzione", false),
            new ArcUiActionGroupDefinition("build", "storage", "Depositi", false),
            new ArcUiActionGroupDefinition("build", "defense", "Difesa", true),
            new ArcUiActionGroupDefinition("build", "decorations", "Decorazioni", true),
            new ArcUiActionGroupDefinition("insert", "objects", "Oggetti", false),
            new ArcUiActionGroupDefinition("npc", "humans", "Umani", false),
            new ArcUiActionGroupDefinition("objects", "selected_object", "Selezione", false),
            new ArcUiActionGroupDefinition("npc", "selected_npc", "Selezione", false)
        };

        private static readonly ArcUiOperationDefinition[] Operations =
        {
            new ArcUiOperationDefinition(
                "build_wall_stone",
                "Muro di pietra",
                "wall_stone",
                "build",
                "structures",
                ArcUiOperationKind.Insert,
                ArcUiOperationTargetKind.Wall,
                "wall_stone",
                true,
                true,
                true,
                false),

            new ArcUiOperationDefinition(
                "place_door_wood",
                "Porta di legno",
                "door_wood",
                "build",
                "structures",
                ArcUiOperationKind.Insert,
                ArcUiOperationTargetKind.Object,
                "door_wood",
                true,
                true,
                false,
                false),

            new ArcUiOperationDefinition(
                "place_bed_wood",
                "Letto di legno",
                "bed_wood",
                "build",
                "furniture",
                ArcUiOperationKind.Insert,
                ArcUiOperationTargetKind.Object,
                "bed_wood",
                true,
                true,
                false,
                false),

            new ArcUiOperationDefinition(
                "place_food_stock",
                "Food stock",
                "food_stock",
                "insert",
                "objects",
                ArcUiOperationKind.Insert,
                ArcUiOperationTargetKind.Object,
                "food_stock",
                true,
                true,
                false,
                false),

            new ArcUiOperationDefinition(
                "spawn_npc_human",
                "Umano",
                "npc_human",
                "npc",
                "humans",
                ArcUiOperationKind.Insert,
                ArcUiOperationTargetKind.Npc,
                "npc_human",
                true,
                true,
                false,
                false),

            new ArcUiOperationDefinition(
                "edit_selected_npc",
                "Modifica NPC",
                "edit",
                "npc",
                "selected_npc",
                ArcUiOperationKind.Edit,
                ArcUiOperationTargetKind.Npc,
                string.Empty,
                false,
                true,
                false,
                false),

            new ArcUiOperationDefinition(
                "delete_selected_npc",
                "Elimina NPC",
                "delete",
                "npc",
                "selected_npc",
                ArcUiOperationKind.Delete,
                ArcUiOperationTargetKind.Npc,
                string.Empty,
                false,
                false,
                false,
                false),

            new ArcUiOperationDefinition(
                "edit_selected_object",
                "Modifica oggetto",
                "edit",
                "objects",
                "selected_object",
                ArcUiOperationKind.Edit,
                ArcUiOperationTargetKind.Object,
                string.Empty,
                false,
                true,
                false,
                false),

            new ArcUiOperationDefinition(
                "delete_selected_object",
                "Elimina oggetto",
                "delete",
                "objects",
                "selected_object",
                ArcUiOperationKind.Delete,
                ArcUiOperationTargetKind.Object,
                string.Empty,
                false,
                false,
                false,
                false),

            new ArcUiOperationDefinition(
                "edit_selected_wall",
                "Modifica muro",
                "edit",
                "build",
                "structures",
                ArcUiOperationKind.Edit,
                ArcUiOperationTargetKind.Wall,
                string.Empty,
                false,
                true,
                false,
                false),

            new ArcUiOperationDefinition(
                "delete_selected_wall",
                "Elimina muro",
                "delete",
                "build",
                "structures",
                ArcUiOperationKind.Delete,
                ArcUiOperationTargetKind.Wall,
                string.Empty,
                false,
                false,
                false,
                false)
        };

        // =============================================================================
        // GetActions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce i pulsanti azione disponibili per la UI runtime.
        /// </para>
        /// </summary>
        public static ArcUiActionDefinition[] GetActions(bool includeDebug)
        {
            var count = 0;

            // Primo passaggio: contiamo quanti elementi sono visibili nel contesto
            // richiesto, evitando liste mutabili condivise tra chiamate.
            for (var i = 0; i < Actions.Length; i++)
            {
                if (includeDebug || !Actions[i].DebugOnly)
                {
                    count++;
                }
            }

            var result = new ArcUiActionDefinition[count];
            var index = 0;

            // Secondo passaggio: copiamo solo i valori ammessi. La copia impedisce
            // a un consumer UI di alterare accidentalmente il catalogo statico.
            for (var i = 0; i < Actions.Length; i++)
            {
                if (includeDebug || !Actions[i].DebugOnly)
                {
                    result[index] = Actions[i];
                    index++;
                }
            }

            return result;
        }

        // =============================================================================
        // GetGroups
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce i macrogruppi legati a un pulsante azione.
        /// </para>
        /// </summary>
        public static ArcUiActionGroupDefinition[] GetGroups(string actionKey, bool includeDebug)
        {
            var normalizedActionKey = ArcUiOperationDefinition.NormalizeKey(actionKey);
            var count = 0;

            for (var i = 0; i < Groups.Length; i++)
            {
                if (Groups[i].ActionKey == normalizedActionKey && (includeDebug || !Groups[i].DebugOnly))
                {
                    count++;
                }
            }

            var result = new ArcUiActionGroupDefinition[count];
            var index = 0;

            for (var i = 0; i < Groups.Length; i++)
            {
                if (Groups[i].ActionKey == normalizedActionKey && (includeDebug || !Groups[i].DebugOnly))
                {
                    result[index] = Groups[i];
                    index++;
                }
            }

            return result;
        }

        // =============================================================================
        // GetOperations
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce le operation filtrate per pulsante azione e macrogruppo.
        /// </para>
        /// </summary>
        public static ArcUiOperationDefinition[] GetOperations(
            string actionKey,
            string groupKey,
            bool includeDebug)
        {
            var normalizedActionKey = ArcUiOperationDefinition.NormalizeKey(actionKey);
            var normalizedGroupKey = ArcUiOperationDefinition.NormalizeKey(groupKey);
            var count = 0;

            for (var i = 0; i < Operations.Length; i++)
            {
                if (MatchesOperationFilter(Operations[i], normalizedActionKey, normalizedGroupKey, includeDebug))
                {
                    count++;
                }
            }

            var result = new ArcUiOperationDefinition[count];
            var index = 0;

            for (var i = 0; i < Operations.Length; i++)
            {
                if (MatchesOperationFilter(Operations[i], normalizedActionKey, normalizedGroupKey, includeDebug))
                {
                    result[index] = Operations[i];
                    index++;
                }
            }

            return result;
        }

        // =============================================================================
        // TryFindOperation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca una operation tramite chiave stabile.
        /// </para>
        /// </summary>
        public static bool TryFindOperation(
            string operationKey,
            bool includeDebug,
            out ArcUiOperationDefinition operation)
        {
            var normalizedOperationKey = ArcUiOperationDefinition.NormalizeKey(operationKey);

            for (var i = 0; i < Operations.Length; i++)
            {
                if (Operations[i].OperationKey == normalizedOperationKey && (includeDebug || !Operations[i].DebugOnly))
                {
                    operation = Operations[i];
                    return true;
                }
            }

            operation = default;
            return false;
        }

        // =============================================================================
        // TryFindDuplicateOperationKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se il catalogo contiene due operation con la stessa chiave.
        /// </para>
        /// </summary>
        public static bool TryFindDuplicateOperationKey(out string duplicateOperationKey)
        {
            // Il catalogo e' piccolo: un controllo quadratico e' piu' leggibile di
            // una struttura ausiliaria e non introduce dipendenze inutili.
            for (var i = 0; i < Operations.Length; i++)
            {
                for (var j = i + 1; j < Operations.Length; j++)
                {
                    if (Operations[i].OperationKey == Operations[j].OperationKey)
                    {
                        duplicateOperationKey = Operations[i].OperationKey;
                        return true;
                    }
                }
            }

            duplicateOperationKey = string.Empty;
            return false;
        }

        // =============================================================================
        // MatchesOperationFilter
        // =============================================================================
        /// <summary>
        /// <para>
        /// Controlla se una operation appartiene al filtro richiesto.
        /// </para>
        /// </summary>
        private static bool MatchesOperationFilter(
            ArcUiOperationDefinition operation,
            string actionKey,
            string groupKey,
            bool includeDebug)
        {
            if (!operation.IsValid || (!includeDebug && operation.DebugOnly))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(actionKey) && operation.ActionKey != actionKey)
            {
                return false;
            }

            return string.IsNullOrEmpty(groupKey) || operation.GroupKey == groupKey;
        }
    }
}
