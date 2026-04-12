// =============================================================================
// SetMoveIntentCommand.cs
// Namespace: Arcontio.Core
// Patch: 0.02.05.B
// =============================================================================
//
// PROBLEMA RISOLTO IN QUESTA PATCH
// ─────────────────────────────────────────────────────────────────────────────
// Prima di questa patch, SetMoveIntentCommand.Execute eseguiva logica di
// planning non banale:
//   - valutava se usare direct path o macro-route landmark
//   - costruiva il path greedy diretto (TryBuildGreedyDirectPath)
//   - avviava l'esecuzione della macro-route (BeginMacroRouteExecutionForNpc)
//   - costruiva un prefix path come fallback
//
// Questo violava il contratto di ICommand:
//   "Un comando muta il World in modo atomico e minimale.
//    Non fa planning, non decide strategie di navigazione."
//
// La logica di selezione del tipo di navigazione appartiene al MovementSystem,
// che ha accesso al tick context, può valutare lo stato frame per frame, e
// può reagire ai cambiamenti del mondo (ostacoli dinamici, NPC che bloccano).
//
// NUOVO CONTRATTO (Patch 0.02.05.B)
// ─────────────────────────────────────────────────────────────────────────────
// SetMoveIntentCommand fa SOLO queste cose:
//   1) Scrive il MoveIntent nel World con IsNew = true.
//   2) Pulisce gli stati di navigazione precedenti.
//   3) Imposta NpcAction a MoveTo per osservabilità immediata nella View.
//
// Il MovementSystem, al primo tick con IsNew = true, esegue:
//   - CanNpcUseDirectPath → sceglie direct o macro-route
//   - BeginMacroRouteExecutionForNpc o SetDebugDirectPathForNpc
//   - Imposta IsNew = false
//
// PERCHÉ IL FLAG IsNew NEL MOVEINTENT
// ─────────────────────────────────────────────────────────────────────────────
// Il flag IsNew è autocontenuto nella struct e rende il contratto esplicito:
// "questo intent non è ancora stato inizializzato dal MovementSystem".
// Evita di dover passare il tick al Command o leggere TickContext
// (che sarebbe un accoppiamento indesiderato in un Command).
// =============================================================================

using Arcontio.Core.Logging;

namespace Arcontio.Core
{
    /// <summary>
    /// <b>SetMoveIntentCommand</b> — scrive un <see cref="MoveIntent"/> nel World.
    ///
    /// <para>
    /// Questo comando è <b>intenzionalmente minimale</b>: trasferisce la decisione
    /// della Rule nel World in modo atomico, senza fare alcun planning.
    /// Rispetta il principio fondamentale di Arcontio:
    /// <i>"Le Rule producono comandi, non mutano direttamente il World."</i>
    /// </para>
    ///
    /// <para><b>Cosa fa questo comando:</b></para>
    /// <list type="number">
    ///   <item>Scrive il <see cref="MoveIntent"/> con <c>IsNew = true</c>,
    ///         segnalando al <c>MovementSystem</c> che serve inizializzazione.</item>
    ///   <item>Pulisce tutti gli stati di navigazione precedenti (debug path,
    ///         macro-route, local search, direct commit).</item>
    ///   <item>Aggiorna <c>NpcAction</c> per osservabilità immediata nella View.</item>
    /// </list>
    ///
    /// <para><b>Cosa NON fa (Patch 0.02.05.B):</b></para>
    /// <list type="bullet">
    ///   <item>NON valuta se usare direct path o macro-route.</item>
    ///   <item>NON costruisce alcun path.</item>
    ///   <item>NON chiama <c>BeginMacroRouteExecutionForNpc</c>.</item>
    /// </list>
    ///
    /// <para>
    /// Tutta la logica di inizializzazione della navigazione è stata spostata
    /// in <see cref="MovementSystem.InitializeNavigation"/>, che la esegue
    /// al primo tick di ogni nuovo intent (<c>intent.IsNew == true</c>).
    /// </para>
    ///
    /// <para><b>Patch:</b> 0.02.05.B</para>
    /// </summary>
    public sealed class SetMoveIntentCommand : ICommand
    {
        /// <summary>ID dell'NPC a cui applicare l'intent.</summary>
        private readonly int _npcId;

        /// <summary>Intent di movimento da scrivere nel World.</summary>
        private readonly MoveIntent _intent;

        /// <summary>
        /// Costruisce il comando.
        /// </summary>
        /// <param name="npcId">ID dell'NPC che deve muoversi.</param>
        /// <param name="intent">Intent di movimento (target cell, reason, ecc.).</param>
        public SetMoveIntentCommand(int npcId, MoveIntent intent)
        {
            _npcId  = npcId;
            _intent = intent;
        }

        /// <summary>Nome del comando per logging e debug.</summary>
        public string Name => nameof(SetMoveIntentCommand);

        /// <inheritdoc/>
        public void Execute(World world, MessageBus bus)
        {
            if (_intent.Active)
            {
                // ============================================================
                // INTENT ATTIVO: nuovo movimento richiesto
                // ============================================================

                bool hadOldIntent = world.NpcMoveIntents.TryGetValue(_npcId, out var oldIntent) && oldIntent.Active;
                bool hadBackOff = world.Pathfinding.MoveBackOff.TryGetValue(_npcId, out var backOff) && backOff != null && backOff.Active;
                bool hadLocalSearch = world.Pathfinding.GoalLocalSearchExecution.TryGetValue(_npcId, out var localState) && localState != null && localState.Active;
                bool hadMacroRoute = world.Pathfinding.MacroRouteExecution.TryGetValue(_npcId, out var macroState) && macroState != null && macroState.Active;

                ArcontioLogger.Trace(
                    new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "Move", npcId: _npcId),
                    new LogBlock(LogLevel.Trace, "log.move.intent_command_debug")
                        .AddField("command", nameof(SetMoveIntentCommand))
                        .AddField("targetX", _intent.TargetX)
                        .AddField("targetY", _intent.TargetY)
                        .AddField("reason", _intent.Reason.ToString())
                        .AddField("hadOldIntent", hadOldIntent)
                        .AddField("oldTargetX", hadOldIntent ? oldIntent.TargetX : 0)
                        .AddField("oldTargetY", hadOldIntent ? oldIntent.TargetY : 0)
                        .AddField("hadBackOff", hadBackOff)
                        .AddField("backOffStage", hadBackOff ? backOff.Stage : 0)
                        .AddField("hadLocalSearch", hadLocalSearch)
                        .AddField("localFailureReason", hadLocalSearch ? localState.FailureReason : string.Empty)
                        .AddField("hadMacroRoute", hadMacroRoute)
                        .AddField("macroNavMode", hadMacroRoute ? macroState.NavigationMode : string.Empty)
                        .AddField("macroFailureReason", hadMacroRoute ? macroState.FailureReason : string.Empty)
                );

                // Copia l'intent e imposta IsNew = true.
                // Il MovementSystem leggerà questo flag al primo tick e
                // inizializzerà la navigazione (direct path o macro-route).
                var intent   = _intent;
                intent.IsNew = true;

                // Pulisce gli stati di navigazione del tick precedente.
                // Fondamentale: senza questo, il MovementSystem potrebbe
                // ereditare un debug path o una macro-route della navigazione
                // precedente e fare scelte incoerenti.
                world.ClearDebugNavigationPathsForNpc(_npcId);
                world.ClearDebugMacroRouteForNpc(_npcId);
                world.ClearNpcLocalSearchState(_npcId, string.Empty);
                world.ClearNpcDirectCommitState(_npcId, string.Empty);
                world.Pathfinding.ClearMoveBackOff(_npcId);

                // Scrive l'intent nel World (source of truth del movimento NPC).
                world.SetMoveIntent(_npcId, intent);

                // Aggiorna NpcAction immediatamente per la View.
                // Usiamo il target finale (non quello effettivo della macro-route):
                // la View mostrerà la destinazione intenzionale dell'NPC, non
                // il prossimo waypoint landmark intermedio.
                world.SetNpcAction(_npcId, NpcActionState.MoveTo(
                    _intent.TargetX,
                    _intent.TargetY,
                    _intent.Reason.ToString()
                ));
            }
            else
            {
                // ============================================================
                // INTENT INATTIVO: cancellazione / reset del movimento
                // ============================================================
                // L'NPC deve fermarsi. Puliamo tutto lo stato di navigazione
                // e portiamo l'NPC in idle.

                world.SetMoveIntent(_npcId, _intent);
                world.ClearDebugNavigationPathsForNpc(_npcId);
                world.ClearDebugMacroRouteForNpc(_npcId);
                world.ClearNpcLocalSearchState(_npcId, string.Empty);
                world.ClearNpcDirectCommitState(_npcId, string.Empty);
                world.Pathfinding.ClearMoveBackOff(_npcId);
                world.SetNpcIdle(_npcId);
            }
        }
    }
}
