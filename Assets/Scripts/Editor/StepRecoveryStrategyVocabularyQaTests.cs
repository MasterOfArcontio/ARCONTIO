using System;
using Arcontio.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // StepRecoveryStrategyVocabularyQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il vocabolario passivo delle strategie candidate di
    /// recupero locale degli step Job.
    /// </para>
    ///
    /// <para><b>v0.11c.04c - Lessico strategy senza policy runtime</b></para>
    /// <para>
    /// Questi test proteggono la natura data-only del nuovo enum: coprono la
    /// presenza dei nomi richiesti senza introdurre mapping produttivi verso
    /// <c>JobStepFailureKind</c>, <c>StepResultStatus</c>, recovery policy,
    /// command emission o mutazione del World.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CLI harness</b>: entry point diagnostico per batchmode quando il runner XML non parte.</item>
    ///   <item><b>Vocabulary coverage</b>: tutti i nomi candidati sono rappresentati.</item>
    ///   <item><b>No concrete strategy</b>: <c>None</c> resta distinto dalle strategie reali.</item>
    ///   <item><b>No mapping</b>: il tipo resta distinto dai failure kind step-local.</item>
    ///   <item><b>No command</b>: nessun valore enum rappresenta un <c>ICommand</c>.</item>
    /// </list>
    /// </summary>
    public sealed class StepRecoveryStrategyVocabularyQaTests
    {
        // =============================================================================
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point minimo per eseguire questi QA test da Unity batchmode quando
        /// il runner CLI standard non produce il file XML dei risultati.
        /// </para>
        ///
        /// <para><b>Harness diagnostico senza runtime behavior</b></para>
        /// <para>
        /// Il metodo invoca gli stessi test NUnit della classe e termina l'Editor con
        /// codice non-zero solo in caso di eccezione. Non viene chiamato dal runtime
        /// simulativo, non istanzia <c>JobExecutionSystem</c>, non avanza job, non
        /// emette command e non muta il World.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new StepRecoveryStrategyVocabularyQaTests();

                tests.StepRecoveryStrategyContainsLocalRecoveryVocabulary();
                tests.StepRecoveryStrategyNoneIsNotAConcreteStrategy();
                tests.StepRecoveryStrategyRemainsDistinctFromFailureKind();

                Debug.Log("[StepRecoveryStrategyVocabularyQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StepRecoveryStrategyVocabularyQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        // =============================================================================
        // StepRecoveryStrategyContainsLocalRecoveryVocabulary
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il vocabolario contenga solo le strategie candidate
        /// richieste per i futuri modelli passivi di recovery locale.
        /// </para>
        /// </summary>
        [Test]
        public void StepRecoveryStrategyContainsLocalRecoveryVocabulary()
        {
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.RetrySameAction), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.WaitAndRetry), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.FindEquivalentTarget), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.FindAlternateCell), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.Repath), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.RequestAssistance), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.RelaxLocalCriteria), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.RebuildCurrentPhase), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.FailPhase), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.FailJob), Is.True);
            Assert.That(Enum.IsDefined(typeof(StepRecoveryStrategy), StepRecoveryStrategy.EscalateToDecision), Is.True);
        }

        // =============================================================================
        // StepRecoveryStrategyNoneIsNotAConcreteStrategy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il valore neutro resti distinto dalle strategie reali senza
        /// attribuire policy, priorita' o recuperabilita' ad alcuna voce.
        /// </para>
        /// </summary>
        [Test]
        public void StepRecoveryStrategyNoneIsNotAConcreteStrategy()
        {
            Assert.That(StepRecoveryStrategy.None, Is.EqualTo((StepRecoveryStrategy)0));
            Assert.That(StepRecoveryStrategy.None, Is.Not.EqualTo(StepRecoveryStrategy.RetrySameAction));
            Assert.That(StepRecoveryStrategy.None, Is.Not.EqualTo(StepRecoveryStrategy.EscalateToDecision));
        }

        // =============================================================================
        // StepRecoveryStrategyRemainsDistinctFromFailureKind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il vocabolario delle strategie resti distinto dal
        /// vocabolario dei failure kind, senza introdurre mapping impliciti tra
        /// causa locale e risposta futura.
        /// </para>
        /// </summary>
        [Test]
        public void StepRecoveryStrategyRemainsDistinctFromFailureKind()
        {
            Assert.That(typeof(StepRecoveryStrategy), Is.Not.EqualTo(typeof(JobStepFailureKind)));
            Assert.That((object)StepRecoveryStrategy.WaitAndRetry, Is.Not.AssignableTo<ICommand>());
            Assert.That((object)StepRecoveryStrategy.EscalateToDecision, Is.Not.AssignableTo<ICommand>());
        }
    }
}
