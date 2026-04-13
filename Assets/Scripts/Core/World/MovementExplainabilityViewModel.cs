// =============================================================================
// MovementExplainabilityViewModel.cs
// Namespace: Arcontio.Core
// Sessione: v0.04.1.h-EL_Pathfinding_ViewModel_Runtime
// =============================================================================
//
// ViewModel runtime read-only per l'Explainability Layer del pathfinding.
// Questo file non introduce pannelli, MonoBehaviour, Canvas, prefab o codice UI.
// Traduce soltanto trace EL gia' presenti nel registry in dati leggibili da una
// futura visualizzazione runtime.
//
// Regola architetturale:
// - le classi *View sono dati passivi per la visualizzazione;
// - il builder e' l'unico punto con funzioni di trasformazione;
// - il builder legge soltanto il registry EL gia' popolato;
// - il builder non interroga BeliefStore, MemoryStore o pathfinder;
// - il builder non modifica world state e non produce eventi simulativi.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // MovementExplainabilityViewModel
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modello dati di alto livello destinato alla futura visualizzazione runtime
    /// dell'EL pathfinding. Raccoglie in un unico snapshot la trace di intent piu'
    /// recente, la trace di piano piu' recente e una timeline di eventi recenti.
    /// </para>
    ///
    /// <para><b>Separazione simulazione / visualizzazione</b></para>
    /// <para>
    /// Questo tipo non contiene logica di pathfinding e non viene letto dal movimento.
    /// Esiste per dare alla UI un dato gia' normalizzato, evitando che il pannello
    /// grafico debba conoscere ring buffer, trace interne o dettagli del registry.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasNpc</b>: indica se esistono dati EL per l'NPC richiesto.</item>
    ///   <item><b>NpcId</b>: identificatore dell'NPC visualizzato.</item>
    ///   <item><b>Tick</b>: tick piu' recente ricavato dalle trace disponibili.</item>
    ///   <item><b>CurrentIntentId</b>: ultimo intent EL osservato nello store.</item>
    ///   <item><b>CurrentPlanId</b>: ultimo piano EL osservato nello store.</item>
    ///   <item><b>HeaderTitle</b>: titolo sintetico pronto per la UI.</item>
    ///   <item><b>HeaderSubtitle</b>: sottotitolo diagnostico pronto per la UI.</item>
    ///   <item><b>Intent</b>: vista normalizzata dell'intent corrente.</item>
    ///   <item><b>Plan</b>: vista normalizzata del piano corrente.</item>
    ///   <item><b>Events</b>: timeline cronologica di eventi recenti.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MovementExplainabilityViewModel
    {
        public bool HasNpc;
        public int NpcId;
        public long Tick;
        public int CurrentIntentId;
        public int CurrentPlanId;
        public string HeaderTitle = string.Empty;
        public string HeaderSubtitle = string.Empty;
        public MovementExplainabilityIntentView Intent = new MovementExplainabilityIntentView();
        public MovementExplainabilityPlanView Plan = new MovementExplainabilityPlanView();
        public List<MovementExplainabilityEventView> Events = new List<MovementExplainabilityEventView>(32);
    }

    // =============================================================================
    // MovementExplainabilityIntentView
    // =============================================================================
    /// <summary>
    /// <para>
    /// Dato passivo e formattato che descrive l'intent di movimento corrente per un
    /// NPC. I campi sono stringhe o scalari semplici per ridurre il lavoro della UI.
    /// </para>
    ///
    /// <para><b>Snapshot dell'intenzione</b></para>
    /// <para>
    /// La vista non conserva reference live a belief, oggetti o path. Quando esiste
    /// una belief causale, viene mostrata come testo derivato da <see cref="BeliefEntryRef"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasIntent</b>: indica se una intent trace era disponibile.</item>
    ///   <item><b>IntentId</b>: id EL dell'intent.</item>
    ///   <item><b>Purpose</b>: scopo normalizzato del movimento.</item>
    ///   <item><b>Target</b>: destinazione formattata per lettura umana.</item>
    ///   <item><b>Belief</b>: base conoscitiva formattata, se presente.</item>
    ///   <item><b>Urgency01</b>: urgenza normalizzata 0-1.</item>
    ///   <item><b>VerbosityLevel</b>: verbosita' con cui la trace e' stata emessa.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MovementExplainabilityIntentView
    {
        public bool HasIntent;
        public int IntentId;
        public string Purpose = string.Empty;
        public string Target = string.Empty;
        public string Belief = string.Empty;
        public float Urgency01;
        public int VerbosityLevel;
    }

    // =============================================================================
    // MovementExplainabilityPlanView
    // =============================================================================
    /// <summary>
    /// <para>
    /// Dato passivo e formattato che descrive il piano di pathfinding corrente. Serve
    /// alla futura UI per mostrare modalita' scelta, ragione di scelta e candidati
    /// valutati senza leggere direttamente <see cref="PathPlanTrace"/>.
    /// </para>
    ///
    /// <para><b>Adattamento UI senza logica simulativa</b></para>
    /// <para>
    /// Il piano qui rappresentato e' solo un riassunto. Non contiene route eseguibili
    /// e non deve essere usato come input per movement, replan o decision layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>HasPlan</b>: indica se una plan trace era disponibile.</item>
    ///   <item><b>PlanId</b>: id EL del piano.</item>
    ///   <item><b>SelectedMode</b>: modalita' scelta dal planner.</item>
    ///   <item><b>SelectionReason</b>: ragione sintetica della scelta.</item>
    ///   <item><b>RouteSummary</b>: riassunto start/goal e macro-route.</item>
    ///   <item><b>FirstStep</b>: primo passo locale noto, se presente.</item>
    ///   <item><b>Candidates</b>: candidati formattati per visualizzazione.</item>
    ///   <item><b>VerbosityLevel</b>: verbosita' con cui la trace e' stata emessa.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MovementExplainabilityPlanView
    {
        public bool HasPlan;
        public int PlanId;
        public string SelectedMode = string.Empty;
        public string SelectionReason = string.Empty;
        public string RouteSummary = string.Empty;
        public string FirstStep = string.Empty;
        public List<string> Candidates = new List<string>(4);
        public int VerbosityLevel;
    }

    // =============================================================================
    // MovementExplainabilityEventView
    // =============================================================================
    /// <summary>
    /// <para>
    /// Riga passiva della timeline runtime EL. Ogni istanza deriva da un
    /// <see cref="PathExecutionEvent"/> e contiene soltanto testo/campi semplici
    /// adatti a pannello grafico, tooltip o overlay di debug.
    /// </para>
    ///
    /// <para><b>Timeline leggibile</b></para>
    /// <para>
    /// La vista conserva il tipo evento e un dettaglio sintetico, ma non possiede
    /// reference a oggetti del mondo. La UI puo' quindi renderizzare la timeline senza
    /// interrogare pathfinder, BeliefStore o memoria dell'NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Tick</b>: tick dell'evento.</item>
    ///   <item><b>EventType</b>: tipo evento formattato.</item>
    ///   <item><b>ActiveMode</b>: modalita' runtime attiva, se nota.</item>
    ///   <item><b>CurrentCell</b>: cella corrente formattata.</item>
    ///   <item><b>TargetCell</b>: cella target formattata.</item>
    ///   <item><b>Summary</b>: testo breve prodotto dall'emitter o fallback locale.</item>
    ///   <item><b>Detail</b>: dettaglio aggiuntivo per fallimenti e porte.</item>
    ///   <item><b>VerbosityLevel</b>: verbosita' con cui l'evento e' stato emesso.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class MovementExplainabilityEventView
    {
        public long Tick;
        public string EventType = string.Empty;
        public string ActiveMode = string.Empty;
        public string CurrentCell = string.Empty;
        public string TargetCell = string.Empty;
        public string Summary = string.Empty;
        public string Detail = string.Empty;
        public int VerbosityLevel;
    }

    // =============================================================================
    // MovementExplainabilityViewModelBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Adapter read-only che costruisce un <see cref="MovementExplainabilityViewModel"/>
    /// a partire dal registry EL contenuto in <see cref="World"/>. La trasformazione
    /// copia e formatta dati gia' osservati, senza generare nuove trace.
    /// </para>
    ///
    /// <para><b>Boundary UI / Core</b></para>
    /// <para>
    /// Questo builder rappresenta il confine intenzionale tra core diagnostico e UI.
    /// Il pannello runtime futuro dovra' dipendere da questo ViewModel, non dai ring
    /// buffer interni. In questo modo la grafica resta separata dalla simulazione e la
    /// simulazione non deve conoscere i widget che la mostrano.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildForNpc</b>: entry point pubblico per generare lo snapshot UI.</item>
    ///   <item><b>Reset</b>: pulizia dell'output riutilizzabile.</item>
    ///   <item><b>FillIntent/FillPlan/FillEvent</b>: trasformazioni specializzate.</item>
    ///   <item><b>Format*</b>: helper privati di formattazione senza side effect.</item>
    /// </list>
    /// </summary>
    public static class MovementExplainabilityViewModelBuilder
    {
        private const int DefaultMaxEvents = 32;

        // =============================================================================
        // BuildForNpc
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce uno snapshot runtime dell'EL pathfinding per un singolo NPC.
        /// Restituisce false quando il world, il registry o lo store dell'NPC non sono
        /// disponibili; in quel caso l'output viene comunque ripulito.
        /// </para>
        ///
        /// <para><b>Lettura passiva del registry</b></para>
        /// <para>
        /// Il metodo non crea store vuoti, non interroga il BeliefStore e non calcola
        /// nuovi percorsi. Usa soltanto copie cronologiche ottenute dallo store EL.
        /// </para>
        /// </summary>
        public static bool BuildForNpc(
            World world,
            int npcId,
            MovementExplainabilityViewModel output,
            int maxEvents = DefaultMaxEvents)
        {
            if (output == null)
                return false;

            Reset(output, npcId);

            // La UI puo' chiedere il ViewModel anche quando l'EL e' spento o quando
            // l'NPC non ha ancora generato trace. In entrambi i casi il fallimento e'
            // informativo, non un errore simulativo.
            if (world == null
                || world.MovementExplainability == null
                || !world.MovementExplainability.TryGetNpcStore(npcId, out var store))
            {
                output.HeaderTitle = $"NPC #{npcId}";
                output.HeaderSubtitle = "EL pathfinding non disponibile per questo NPC";
                return false;
            }

            output.HasNpc = true;
            output.NpcId = npcId;
            output.CurrentIntentId = store.CurrentIntentId;
            output.CurrentPlanId = store.CurrentPlanId;
            output.HeaderTitle = $"NPC #{npcId}";
            output.HeaderSubtitle = $"Intent {store.CurrentIntentId} | Piano {store.CurrentPlanId} | Eventi {store.ExecutionEventCount}";

            // Intent e plan sono letti come ultimi snapshot perche' il pannello laterale
            // deve mostrare lo stato corrente, mentre gli eventi restano una timeline.
            if (store.TryGetLatestIntentTrace(out var intentTrace))
                FillIntent(output.Intent, intentTrace);

            if (store.TryGetLatestPlanTrace(out var planTrace))
                FillPlan(output.Plan, planTrace);

            var events = new List<PathExecutionEvent>(Math.Max(1, Math.Min(store.ExecutionEventCount, maxEvents)));
            store.CopyExecutionEventsTo(events);

            int safeMaxEvents = Math.Max(0, maxEvents);
            int startIndex = safeMaxEvents <= 0 ? events.Count : Math.Max(0, events.Count - safeMaxEvents);

            // La copia conserva l'ordine cronologico, ma applica un limite finale per
            // impedire alla UI di renderizzare piu' righe di quante ne abbia richieste.
            for (int i = startIndex; i < events.Count; i++)
            {
                var eventView = new MovementExplainabilityEventView();
                FillEvent(eventView, events[i]);
                output.Events.Add(eventView);
            }

            output.Tick = ResolveLatestTick(intentTrace, planTrace, events);
            return true;
        }

        // =============================================================================
        // Reset
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riporta l'output a uno stato vuoto e riutilizzabile. La pulizia e' nel
        /// builder, non nei dati, per mantenere le classi View come contenitori passivi.
        /// </para>
        /// </summary>
        private static void Reset(MovementExplainabilityViewModel output, int npcId)
        {
            output.HasNpc = false;
            output.NpcId = npcId;
            output.Tick = 0;
            output.CurrentIntentId = 0;
            output.CurrentPlanId = 0;
            output.HeaderTitle = string.Empty;
            output.HeaderSubtitle = string.Empty;

            output.Intent.HasIntent = false;
            output.Intent.IntentId = 0;
            output.Intent.Purpose = string.Empty;
            output.Intent.Target = string.Empty;
            output.Intent.Belief = string.Empty;
            output.Intent.Urgency01 = 0f;
            output.Intent.VerbosityLevel = 0;

            output.Plan.HasPlan = false;
            output.Plan.PlanId = 0;
            output.Plan.SelectedMode = string.Empty;
            output.Plan.SelectionReason = string.Empty;
            output.Plan.RouteSummary = string.Empty;
            output.Plan.FirstStep = string.Empty;
            output.Plan.Candidates.Clear();
            output.Plan.VerbosityLevel = 0;

            output.Events.Clear();
        }

        // =============================================================================
        // FillIntent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia una <see cref="MovementIntentTrace"/> nella vista intent. La belief,
        /// se presente, viene ridotta a testo diagnostico e non viene mai risolta
        /// contro store o registry globali.
        /// </para>
        /// </summary>
        private static void FillIntent(MovementExplainabilityIntentView view, MovementIntentTrace trace)
        {
            if (view == null || trace == null)
                return;

            view.HasIntent = true;
            view.IntentId = trace.IntentId;
            view.Purpose = trace.MovementPurpose.ToString();
            view.Target = FormatTarget(trace.TargetType, trace.TargetCell, trace.TargetObjectId);
            view.Belief = trace.HasBeliefBasis ? FormatBelief(trace.BeliefBasis) : "Nessuna belief causale registrata";
            view.Urgency01 = Mathf.Clamp01(trace.Urgency);
            view.VerbosityLevel = trace.VerbosityLevel;
        }

        // =============================================================================
        // FillPlan
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia una <see cref="PathPlanTrace"/> nella vista piano. I candidati vengono
        /// trasformati in righe leggibili, cosi' la UI non deve conoscere enum e costi
        /// interni del planning.
        /// </para>
        /// </summary>
        private static void FillPlan(MovementExplainabilityPlanView view, PathPlanTrace trace)
        {
            if (view == null || trace == null)
                return;

            view.HasPlan = true;
            view.PlanId = trace.PlanId;
            view.SelectedMode = trace.SelectedMode.ToString();
            view.SelectionReason = trace.SelectionReason.ToString();
            view.RouteSummary = FormatRoute(trace);
            view.FirstStep = trace.HasLocalRouteFirstStep
                ? FormatCell(trace.LocalRouteFirstStep)
                : "Primo passo non disponibile";
            view.VerbosityLevel = trace.VerbosityLevel;
            view.Candidates.Clear();

            if (trace.Candidates == null || trace.Candidates.Count <= 0)
            {
                view.Candidates.Add("Nessun candidato registrato");
                return;
            }

            // Ogni candidato resta una riga autonoma per facilitare il futuro binding
            // grafico in una lista compatta del pannello laterale.
            for (int i = 0; i < trace.Candidates.Count; i++)
                view.Candidates.Add(FormatCandidate(trace.Candidates[i]));
        }

        // =============================================================================
        // FillEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia un <see cref="PathExecutionEvent"/> nella riga timeline. Il dettaglio
        /// specializzato viene generato solo per fallimenti e porte, lasciando gli
        /// eventi normali piu' compatti.
        /// </para>
        /// </summary>
        private static void FillEvent(MovementExplainabilityEventView view, PathExecutionEvent evt)
        {
            if (view == null || evt == null)
                return;

            view.Tick = evt.Tick;
            view.EventType = evt.EventType.ToString();
            view.ActiveMode = string.IsNullOrWhiteSpace(evt.ActiveMode) ? "Modo non noto" : evt.ActiveMode;
            view.CurrentCell = FormatCell(evt.CurrentCell);
            view.TargetCell = FormatCell(evt.TargetCell);
            view.Summary = string.IsNullOrWhiteSpace(evt.Summary) ? evt.EventType.ToString() : evt.Summary;
            view.Detail = FormatEventDetail(evt);
            view.VerbosityLevel = evt.VerbosityLevel;
        }

        // =============================================================================
        // ResolveLatestTick
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola il tick piu' recente disponibile fra intent, piano ed eventi. Il
        /// valore serve solo come metadato di snapshot del ViewModel.
        /// </para>
        /// </summary>
        private static long ResolveLatestTick(
            MovementIntentTrace intentTrace,
            PathPlanTrace planTrace,
            List<PathExecutionEvent> events)
        {
            long latest = 0;

            if (intentTrace != null)
                latest = Math.Max(latest, intentTrace.Tick);

            if (planTrace != null)
                latest = Math.Max(latest, planTrace.Tick);

            if (events != null && events.Count > 0 && events[events.Count - 1] != null)
                latest = Math.Max(latest, events[events.Count - 1].Tick);

            return latest;
        }

        // =============================================================================
        // FormatTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Formatta il target dell'intent combinando tipo, cella e id oggetto. La
        /// formattazione e' intenzionalmente locale: non risolve id in nomi oggetto.
        /// </para>
        /// </summary>
        private static string FormatTarget(MovementTargetType targetType, Vector2Int cell, int objectId)
        {
            string objectPart = objectId > 0 ? $" | oggetto #{objectId}" : string.Empty;
            return $"{targetType} {FormatCell(cell)}{objectPart}";
        }

        // =============================================================================
        // FormatBelief
        // =============================================================================
        /// <summary>
        /// <para>
        /// Formatta lo snapshot causale della belief senza interrogare il relativo
        /// store. Confidence e freshness sono riportate come valori normalizzati.
        /// </para>
        /// </summary>
        private static string FormatBelief(BeliefEntryRef belief)
        {
            return $"{belief.Category} belief #{belief.BeliefId} | entita' #{belief.EntityId} | conf {belief.Confidence:0.00} | fresh {belief.Freshness:0.00} | eta' {belief.AgeTicks} tick";
        }

        // =============================================================================
        // FormatRoute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce un riassunto compatto della route. I nodi landmark vengono indicati
        /// come conteggio, non come lista completa, per mantenere il pannello leggibile.
        /// </para>
        /// </summary>
        private static string FormatRoute(PathPlanTrace trace)
        {
            int macroCount = trace.MacroRouteNodes == null ? 0 : trace.MacroRouteNodes.Length;
            string macroPart = macroCount > 0
                ? $" | landmark {macroCount} | costo {trace.MacroRouteCost:0.00}"
                : " | nessuna macro-route";

            return $"{FormatCell(trace.StartCell)} -> {FormatCell(trace.GoalCell)}{macroPart}";
        }

        // =============================================================================
        // FormatCandidate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un candidato planner in una singola riga testuale. La riga mantiene
        /// visibili validita', costo e motivo di scarto senza esporre la struct originale.
        /// </para>
        /// </summary>
        private static string FormatCandidate(PlannerCandidate candidate)
        {
            string state = candidate.Valid ? "valido" : $"scartato: {candidate.InvalidReason}";
            string cost = candidate.EstimatedCost >= 0f ? $"costo {candidate.EstimatedCost:0.00}" : "costo n/d";
            string note = string.IsNullOrWhiteSpace(candidate.Note) ? string.Empty : $" | {candidate.Note}";

            return $"{candidate.Mode} | {state} | {cost}{note}";
        }

        // =============================================================================
        // FormatEventDetail
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea il dettaglio esteso per un evento runtime. I dettagli specializzati
        /// restano confinati qui, cosi' la futura UI non duplica logica di formattazione.
        /// </para>
        /// </summary>
        private static string FormatEventDetail(PathExecutionEvent evt)
        {
            if (evt.HasFailureDetail)
                return FormatFailure(evt.FailureDetail);

            if (evt.HasDoorDetail)
                return FormatDoor(evt.DoorDetail);

            return string.Empty;
        }

        // =============================================================================
        // FormatFailure
        // =============================================================================
        /// <summary>
        /// <para>
        /// Formatta il dettaglio di fallimento conservando cella bloccante, NPC
        /// bloccante e stato della failure ladder quando disponibili.
        /// </para>
        /// </summary>
        private static string FormatFailure(FailureDetail detail)
        {
            string blockingCell = detail.HasBlockingCell ? FormatCell(detail.BlockingCell) : "cella n/d";
            string blockingNpc = detail.HasBlockingNpcId ? $"NPC #{detail.BlockingNpcId}" : "NPC n/d";
            string oscillation = detail.OscillationFlag ? " | oscillazione rilevata" : string.Empty;

            return $"{detail.FailureType} | blocco {blockingCell} | {blockingNpc} | tick bloccati {detail.BlockedTicks} | back-off {detail.BackOffStage} | modo {detail.LastActiveMode}{oscillation}";
        }

        // =============================================================================
        // FormatDoor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Formatta il dettaglio porta mostrando id, cella, stati e risultato del
        /// comando. Il testo e' diagnostico e non modifica l'oggetto porta reale.
        /// </para>
        /// </summary>
        private static string FormatDoor(DoorInteractionDetail detail)
        {
            string command = detail.CommandEmitted ? "comando emesso" : "nessun comando";
            string access = detail.AccessGranted ? "accesso consentito" : "accesso negato";

            return $"porta #{detail.DoorObjectId} {FormatCell(detail.DoorCell)} | {detail.StateBefore} -> {detail.StateAfter} | {command} | {access}";
        }

        // =============================================================================
        // FormatCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Formatta una cella Unity <see cref="Vector2Int"/> in una stringa stabile
        /// per pannelli, log umani e tooltip runtime.
        /// </para>
        /// </summary>
        private static string FormatCell(Vector2Int cell)
        {
            return $"({cell.x}, {cell.y})";
        }
    }
}
