using System;
using System.Collections.Generic;
using Arcontio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // DecisionLayerQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per la v0.05: verifica il percorso minimo
    /// DNA/Profile/Needs/Belief -> candidati -> score -> intenzione selezionata.
    /// </para>
    ///
    /// <para><b>Decision Layer senza onniscienza</b></para>
    /// <para>
    /// I test costruiscono solo dati soggettivi e componenti per-NPC. Non viene
    /// creato un <c>World</c>, non vengono letti oggetti globali e non viene usato
    /// <c>MemoryStore</c>: il target arriva gia' filtrato nel <c>BeliefStore</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Fase 1</b>: generazione candidati da bisogni e belief.</item>
    ///   <item><b>Fase 2</b>: scoring con breakdown esplicito.</item>
    ///   <item><b>Fase 3</b>: selezione deterministica top-1 per QA.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionLayerQaTests
    {
        // =============================================================================
        // HungryNpcWithFoodBeliefSelectsEatKnownFood
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica il percorso end-to-end base: un NPC affamato con una credenza Food
        /// seleziona <c>EatKnownFood</c>.
        /// </para>
        ///
        /// <para><b>DNA -> Decision -> Intenzione</b></para>
        /// <para>
        /// Il test usa DNA e profilo runtime neutri, un bisogno fame critico e un
        /// BeliefStore contenente cibo soggettivo. La selezione top-1 elimina la
        /// varianza per controllare solo il ranking.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Belief</b>: fonte Food a distanza breve.</item>
        ///   <item><b>Scoring</b>: include NeedUrgency e MemoryConfidence.</item>
        ///   <item><b>Select</b>: topN 1 produce scelta deterministica.</item>
        /// </list>
        /// </summary>
        [Test]
        public void HungryNpcWithFoodBeliefSelectsEatKnownFood()
        {
            // Arrange: costruiamo solo input permessi al Decision Layer.
            var context = MakeContext(hunger01: 0.90f, rest01: 0.10f, addFoodBelief: true);
            var candidates = new List<DecisionCandidate>();
            var generator = new DecisionCandidateGenerator();
            var scoring = new DecisionScoringService();
            var selection = new DecisionSelectionService();

            // Act: pipeline completa Fase 1 -> Fase 2 -> Fase 3.
            generator.GeneratePhase1Candidates(context, candidates);
            scoring.ScoreCandidates(context, candidates, DecisionScoringConfig.Default());
            var result = selection.Select(context, candidates, TopOneSelection(), new Random(7));

            // Assert: il target conosciuto vince sul fallback SearchFood.
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Candidate.Kind, Is.EqualTo(DecisionIntentKind.EatKnownFood));
            Assert.That(result.Candidate.BeliefResult.IsEmpty, Is.False);
            Assert.That(result.Candidate.ScoreContributions.Length, Is.GreaterThan(0));
        }

        // =============================================================================
        // HungryNpcWithoutFoodBeliefFallsBackToSearchFood
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che un NPC affamato senza belief Food non generi un'azione diretta
        /// verso cibo inesistente e ricada su <c>SearchFood</c>.
        /// </para>
        ///
        /// <para><b>Gate QuerySystem</b></para>
        /// <para>
        /// <c>EatKnownFood</c> richiede un target belief. Senza belief usabile, il
        /// candidato viene filtrato e resta il fallback di ricerca.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>BeliefStore</b>: vuoto.</item>
        ///   <item><b>Need</b>: fame sopra soglia critica.</item>
        ///   <item><b>Assert</b>: nessuna intenzione diretta a target non conosciuto.</item>
        /// </list>
        /// </summary>
        [Test]
        public void HungryNpcWithoutFoodBeliefFallsBackToSearchFood()
        {
            // Arrange: niente credenze Food, quindi niente target concreto.
            var context = MakeContext(hunger01: 0.90f, rest01: 0.10f, addFoodBelief: false);
            var candidates = new List<DecisionCandidate>();
            var generator = new DecisionCandidateGenerator();
            var scoring = new DecisionScoringService();
            var selection = new DecisionSelectionService();

            // Act: la pipeline deve produrre un'intenzione di ricerca, non un target inventato.
            generator.GeneratePhase1Candidates(context, candidates);
            scoring.ScoreCandidates(context, candidates, DecisionScoringConfig.Default());
            var result = selection.Select(context, candidates, TopOneSelection(), new Random(3));

            // Assert: SearchFood e' il comportamento conservativo quando la conoscenza manca.
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Candidate.Kind, Is.EqualTo(DecisionIntentKind.SearchFood));
            Assert.That(result.Candidate.BeliefResult.IsEmpty, Is.True);
        }

        // =============================================================================
        // DecisionInputAuditAcceptsWhitelistedPerNpcInputs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che l'audit del Decision Layer accetti il contesto per-NPC
        /// costruito senza <c>World</c> e senza <c>MemoryStore</c>.
        /// </para>
        ///
        /// <para><b>Audit omniscience MVP</b></para>
        /// <para>
        /// Il test protegge la forma del contratto introdotto in v0.05: il Decision
        /// Layer riceve dati soggettivi espliciti e non deve richiedere accesso diretto
        /// a store oggettivi o memoria episodica grezza.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Context</b>: creato dalla factory senza World.</item>
        ///   <item><b>Audit</b>: eseguito solo sul contesto decisionale.</item>
        ///   <item><b>Assert</b>: input validi e nota whitelist presente.</item>
        /// </list>
        /// </summary>
        [Test]
        public void DecisionInputAuditAcceptsWhitelistedPerNpcInputs()
        {
            // Arrange: contesto completo, ma confinato a dati per-NPC e belief soggettivi.
            var context = MakeContext(hunger01: 0.90f, rest01: 0.10f, addFoodBelief: true);

            // Act: l'audit non riceve World e non puo' leggere MemoryStore.
            var result = DecisionInputAudit.Audit(context);

            // Assert: la whitelist degli input MVP e' soddisfatta.
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.MissingRequiredInputCount, Is.EqualTo(0));
            Assert.That(result.Notes, Does.Contain("DecisionInputWhitelist"));
        }

        // =============================================================================
        // MakeContext
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un contesto decisionale isolato per i test QA.
        /// </para>
        ///
        /// <para><b>Factory senza World</b></para>
        /// <para>
        /// La factory crea DNA, profilo, bisogni e belief store direttamente. Questo
        /// rende impossibile leggere per errore <c>World.Objects</c> o altri store
        /// oggettivi durante il test.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Needs</b>: fame/riposo con flag alert e critical coerenti.</item>
        ///   <item><b>Beliefs</b>: opzionale entry Food soggettiva.</item>
        ///   <item><b>Norms</b>: filtro attivo per escludere azioni ad alto social risk.</item>
        /// </list>
        /// </summary>
        private static DecisionEvaluationContext MakeContext(float hunger01, float rest01, bool addFoodBelief)
        {
            // I dati individuali sono creati localmente: nessun registry globale e'
            // necessario per valutare il Decision Layer.
            var dna = NpcDnaProfile.CreateDefault("qa_npc", "qa", 0);
            var profile = NpcProfile.InitFromDna(dna);
            var needs = NpcNeeds.Make(hunger01, rest01);
            needs.SetFlags(NeedKind.Hunger, hunger01 >= dna.Thresholds.NeedAlert01, hunger01 >= dna.Thresholds.NeedCritical01);
            needs.SetFlags(NeedKind.Rest, rest01 >= dna.Thresholds.NeedAlert01, rest01 >= dna.Thresholds.NeedCritical01);

            var beliefs = new BeliefStore();
            if (addFoodBelief)
            {
                beliefs.AddOrMergeByCategoryAndPosition(
                    BeliefCategory.Food,
                    new Vector2Int(4, 2),
                    confidence: 0.90f,
                    freshness: 0.90f,
                    currentTick: 10,
                    source: BeliefSource.Seen);
            }

            return new DecisionEvaluationContext(
                npcId: 1,
                tick: 20,
                needs: needs,
                dna: dna,
                profile: profile,
                npcPosition: new Vector2Int(2, 2),
                beliefs: beliefs,
                beliefQueryConfig: BeliefQueryConfig.Default(),
                scheduleFrame: new DecisionScheduleFrame(false, DomainKind.None, true),
                normContext: new DecisionNormContext(true, 0.50f, false));
        }

        // =============================================================================
        // TopOneSelection
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce una configurazione di selezione deterministica top-1 per i test.
        /// </para>
        ///
        /// <para><b>QA del ranking, non della varianza</b></para>
        /// <para>
        /// La Fase 3 supporta weighted random, ma questi test vogliono controllare il
        /// candidato migliore. Per questo restringono la roulette a un solo candidato.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>topN</b>: 1.</item>
        ///   <item><b>noise</b>: 0 per non introdurre varianza non necessaria.</item>
        ///   <item><b>minimumWeight</b>: fallback positivo minimo.</item>
        /// </list>
        /// </summary>
        private static DecisionSelectionConfig TopOneSelection()
        {
            return new DecisionSelectionConfig
            {
                topN = 1,
                noise01 = 0f,
                impulsivityNoiseBonus = 0f,
                minimumWeight = 0.001f
            };
        }
    }
}
