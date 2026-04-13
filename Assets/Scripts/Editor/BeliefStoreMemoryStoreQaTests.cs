using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // BeliefStoreMemoryStoreQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la sessione 19: verifica il contratto tra MemoryStore,
    /// BeliefUpdater e BeliefStore senza passare da UI, card grafiche o world state
    /// oggettivo.
    /// </para>
    ///
    /// <para><b>Vincolo di Onniscienza</b></para>
    /// <para>
    /// Ogni test costruisce manualmente una <c>MemoryTrace</c> soggettiva e la passa
    /// al percorso di aggregazione. Non viene creato un <c>World</c>, non vengono
    /// letti <c>Objects</c>, <c>FoodStocks</c> o database globali: il BeliefStore deve
    /// derivare solo dalle informazioni gia' presenti nella trace.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Object mapping</b>: food e bed derivano da <c>SubjectDefId</c>.</item>
    ///   <item><b>Threat/social mapping</b>: predator e NPC osservati producono categorie dedicate.</item>
    ///   <item><b>Memory gate</b>: trace droppate dal MemoryStore non aggiornano il BeliefStore.</item>
    /// </list>
    /// </summary>
    public sealed class BeliefStoreMemoryStoreQaTests
    {
        // =============================================================================
        // ObjectSpottedFoodDefIdAggregatesFoodBelief
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una memoria di oggetto con <c>SubjectDefId</c> alimentare
        /// produca una credenza <c>Food</c>.
        /// </para>
        ///
        /// <para><b>Semantica fissata nella MemoryTrace</b></para>
        /// <para>
        /// La classificazione usa il testo gia' dentro la trace. Il test non fornisce
        /// nessun accesso al mondo, quindi un eventuale lookup onnisciente non sarebbe
        /// disponibile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Trace</b>: <c>ObjectSpotted</c> con <c>food_stock</c>.</item>
        ///   <item><b>Updater</b>: aggrega la trace nel BeliefStore.</item>
        ///   <item><b>Assert</b>: categoria, posizione, confidence e freshness.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ObjectSpottedFoodDefIdAggregatesFoodBelief()
        {
            // Arrange: costruiamo solo dati soggettivi minimi, senza World.
            var store = new BeliefStore();
            var updater = new BeliefUpdater();
            var trace = MakeTrace(MemoryType.ObjectSpotted, 101, "food_stock", 12, 8, 0.80f, 0.90f, isHeard: false);

            // Act: il BeliefUpdater e' l'unico layer che traduce trace -> belief.
            bool updated = updater.UpdateFromTrace(trace, store, currentTick: 10);

            // Assert: il belief e' Food perche' la trace portava il DefId food_stock.
            Assert.That(updated, Is.True);
            Assert.That(store.Entries.Count, Is.EqualTo(1));
            AssertBelief(store.Entries[0], BeliefCategory.Food, BeliefStatus.Active, BeliefSource.Seen, new Vector2Int(12, 8), 0.90f, 0.80f, 1);
        }

        // =============================================================================
        // ObjectSpottedBedDefIdAggregatesRestBelief
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una memoria comunicata di oggetto letto produca una credenza
        /// <c>Rest</c> con fonte <c>Heard</c>.
        /// </para>
        ///
        /// <para><b>Comunicazione mediata</b></para>
        /// <para>
        /// Il BeliefStore non distingue da solo la catena comunicativa: riceve dal
        /// BeliefUpdater una fonte derivata dal flag <c>IsHeard</c> della trace.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Trace</b>: <c>ObjectSpotted</c> con <c>bed_wood_poor</c>.</item>
        ///   <item><b>Source</b>: <c>IsHeard</c> true diventa <c>BeliefSource.Heard</c>.</item>
        ///   <item><b>Assert</b>: categoria <c>Rest</c> e dati numerici copiati dalla trace.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ObjectSpottedBedDefIdAggregatesRestBelief()
        {
            // Arrange: la memoria e' sentita, non vista direttamente.
            var store = new BeliefStore();
            var updater = new BeliefUpdater();
            var trace = MakeTrace(MemoryType.ObjectSpotted, 205, "bed_wood_poor", 5, 3, 0.65f, 0.75f, isHeard: true);

            // Act: nessuna query world/object database e' necessaria.
            bool updated = updater.UpdateFromTrace(trace, store, currentTick: 20);

            // Assert: "bed" nel DefId viene mappato in Rest, fonte Heard.
            Assert.That(updated, Is.True);
            Assert.That(store.Entries.Count, Is.EqualTo(1));
            AssertBelief(store.Entries[0], BeliefCategory.Rest, BeliefStatus.Active, BeliefSource.Heard, new Vector2Int(5, 3), 0.75f, 0.65f, 1);
        }

        // =============================================================================
        // ThreatAndNpcTraceAggregateDangerAndSocialBeliefs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica le due mappature non-oggetto dell'MVP: minacce in <c>Danger</c>
        /// e NPC osservati in <c>Social</c>.
        /// </para>
        ///
        /// <para><b>Aggregazione conservativa</b></para>
        /// <para>
        /// Il test non pretende fiducia, ostilita' o ranking. Controlla solo che il
        /// BeliefStore riceva categorie soggettive coerenti con le regole minime
        /// documentate.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>PredatorSpotted</b>: produce una entry <c>Danger</c>.</item>
        ///   <item><b>NpcSpotted</b>: produce una entry <c>Social</c>.</item>
        ///   <item><b>Assert</b>: le due entry restano separate per categoria/posizione.</item>
        /// </list>
        /// </summary>
        [Test]
        public void ThreatAndNpcTraceAggregateDangerAndSocialBeliefs()
        {
            // Arrange: due memorie soggettive distinte nello stesso store cognitivo.
            var store = new BeliefStore();
            var updater = new BeliefUpdater();
            var dangerTrace = MakeTrace(MemoryType.PredatorSpotted, 44, string.Empty, 18, 11, 0.95f, 0.80f, isHeard: false);
            var socialTrace = MakeTrace(MemoryType.NpcSpotted, 12, string.Empty, 4, 6, 0.70f, 0.60f, isHeard: false);

            // Act: applichiamo le due trace come farebbe il percorso lazy dopo MemoryStore.
            bool dangerUpdated = updater.UpdateFromTrace(dangerTrace, store, currentTick: 30);
            bool socialUpdated = updater.UpdateFromTrace(socialTrace, store, currentTick: 31);

            // Assert: due belief, nessuna inferenza sociale extra.
            Assert.That(dangerUpdated, Is.True);
            Assert.That(socialUpdated, Is.True);
            Assert.That(store.Entries.Count, Is.EqualTo(2));
            AssertBelief(store.Entries[0], BeliefCategory.Danger, BeliefStatus.Active, BeliefSource.Seen, new Vector2Int(18, 11), 0.80f, 0.95f, 1);
            AssertBelief(store.Entries[1], BeliefCategory.Social, BeliefStatus.Active, BeliefSource.Seen, new Vector2Int(4, 6), 0.60f, 0.70f, 1);
        }

        // =============================================================================
        // DroppedMemoryTraceDoesNotUpdateBeliefStore
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il gate QA piu' importante della sessione 19: una trace scartata
        /// dal MemoryStore non deve creare o rinforzare belief.
        /// </para>
        ///
        /// <para><b>BeliefStore derivato dal MemoryStore</b></para>
        /// <para>
        /// Il BeliefStore non deve diventare una seconda memoria parallela che conserva
        /// dati rifiutati dal MemoryStore. La pipeline aggiorna belief solo per esiti
        /// <c>Inserted</c>, <c>Reinforced</c> o <c>Replaced</c>, non per <c>Dropped</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Cap</b>: MemoryStore limitato a una trace.</item>
        ///   <item><b>Strong trace</b>: entra e crea belief <c>Food</c>.</item>
        ///   <item><b>Weak trace</b>: viene droppata e non crea belief <c>Rest</c>.</item>
        /// </list>
        /// </summary>
        [Test]
        public void DroppedMemoryTraceDoesNotUpdateBeliefStore()
        {
            // Arrange: cap volutamente piccolo per forzare un drop deterministico.
            var memoryStore = new MemoryStore { MaxTraces = 1 };
            var beliefStore = new BeliefStore();
            var updater = new BeliefUpdater();
            var strongFoodTrace = MakeTrace(MemoryType.ObjectSpotted, 301, "food_stock", 1, 1, 0.90f, 0.90f, isHeard: false);
            var weakRestTrace = MakeTrace(MemoryType.ObjectSpotted, 302, "bed_wood_poor", 9, 9, 0.10f, 0.10f, isHeard: false);

            // Act: simuliamo esplicitamente il gate usato da MemoryEncoding/TokenAssimilation.
            AddThroughMemoryGate(memoryStore, updater, beliefStore, strongFoodTrace, currentTick: 40);
            AddOrMergeResult weakResult = AddThroughMemoryGate(memoryStore, updater, beliefStore, weakRestTrace, currentTick: 41);

            // Assert: la seconda trace non entra in memoria, quindi non entra nei belief.
            Assert.That(weakResult, Is.EqualTo(AddOrMergeResult.Dropped));
            Assert.That(memoryStore.Traces.Count, Is.EqualTo(1));
            Assert.That(beliefStore.Entries.Count, Is.EqualTo(1));
            AssertBelief(beliefStore.Entries[0], BeliefCategory.Food, BeliefStatus.Active, BeliefSource.Seen, new Vector2Int(1, 1), 0.90f, 0.90f, 1);
        }

        // =============================================================================
        // MakeTrace
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una <c>MemoryTrace</c> completa per i test QA.
        /// </para>
        ///
        /// <para><b>Factory soggettiva</b></para>
        /// <para>
        /// La factory evita setup ripetuto ma non nasconde lookup esterni: tutti i dati
        /// usati dal BeliefUpdater sono passati esplicitamente nel payload.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Type/Subject</b>: identita' narrativa della memoria.</item>
        ///   <item><b>SubjectDefId</b>: semantica oggetto quando presente.</item>
        ///   <item><b>Quality</b>: intensity/freshness e reliability/confidence.</item>
        /// </list>
        /// </summary>
        private static MemoryTrace MakeTrace(
            MemoryType type,
            int subjectId,
            string subjectDefId,
            int x,
            int y,
            float intensity,
            float reliability,
            bool isHeard)
        {
            // La trace e' volutamente autosufficiente: il test non fornisce altri
            // oggetti da cui ricostruire il tipo o la posizione del soggetto.
            return new MemoryTrace
            {
                Type = type,
                SubjectId = subjectId,
                SecondarySubjectId = 0,
                SubjectDefId = subjectDefId,
                CellX = x,
                CellY = y,
                Intensity01 = intensity,
                Reliability01 = reliability,
                DecayPerTick01 = 0.01f,
                IsHeard = isHeard,
                HeardKind = isHeard ? HeardKind.DirectHeard : HeardKind.None,
                SourceSpeakerId = isHeard ? 99 : 0
            };
        }

        // =============================================================================
        // AddThroughMemoryGate
        // =============================================================================
        /// <summary>
        /// <para>
        /// Replica il gate architetturale usato dai sistemi runtime: il BeliefUpdater
        /// viene chiamato solo se il MemoryStore non droppa la trace.
        /// </para>
        ///
        /// <para><b>QA del contratto MemoryStore -> BeliefStore</b></para>
        /// <para>
        /// La funzione sta nei test per rendere esplicita la policy che la sessione 19
        /// vuole proteggere: il BeliefStore e' derivativo rispetto alle tracce accettate
        /// dalla memoria soggettiva.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>AddOrMerge</b>: prova a inserire o fondere la trace.</item>
        ///   <item><b>Dropped guard</b>: interrompe l'aggregazione se la memoria scarta.</item>
        ///   <item><b>BeliefUpdater</b>: aggiorna lo store solo per trace accettate.</item>
        /// </list>
        /// </summary>
        private static AddOrMergeResult AddThroughMemoryGate(
            MemoryStore memoryStore,
            BeliefUpdater updater,
            BeliefStore beliefStore,
            MemoryTrace trace,
            int currentTick)
        {
            // Questo if e' la regola chiave di sessione 19: niente memoria, niente belief.
            AddOrMergeResult result = memoryStore.AddOrMerge(trace);
            if (result != AddOrMergeResult.Dropped)
                updater.UpdateFromTrace(trace, beliefStore, currentTick);

            return result;
        }

        // =============================================================================
        // AssertBelief
        // =============================================================================
        /// <summary>
        /// <para>
        /// Controlla i campi essenziali di una <c>BeliefEntry</c> prodotta dai test.
        /// </para>
        ///
        /// <para><b>Contratto dati minimo</b></para>
        /// <para>
        /// L'assert non valuta ranking o decisioni. Conferma solo che la trasformazione
        /// trace -> belief preservi categoria, fonte, posizione e qualita' soggettiva.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Semantica</b>: category, status e source.</item>
        ///   <item><b>Spazio</b>: estimated position.</item>
        ///   <item><b>Qualita'</b>: confidence, freshness e source count.</item>
        /// </list>
        /// </summary>
        private static void AssertBelief(
            BeliefEntry entry,
            BeliefCategory category,
            BeliefStatus status,
            BeliefSource source,
            Vector2Int position,
            float confidence,
            float freshness,
            int sourceCount)
        {
            // Ogni confronto resta sul dato soggettivo, non su verita' oggettive.
            Assert.That(entry.Category, Is.EqualTo(category));
            Assert.That(entry.Status, Is.EqualTo(status));
            Assert.That(entry.Source, Is.EqualTo(source));
            Assert.That(entry.EstimatedPosition, Is.EqualTo(position));
            Assert.That(entry.Confidence, Is.EqualTo(confidence).Within(0.0001f));
            Assert.That(entry.Freshness, Is.EqualTo(freshness).Within(0.0001f));
            Assert.That(entry.SourceCount, Is.EqualTo(sourceCount));
        }
    }
}
