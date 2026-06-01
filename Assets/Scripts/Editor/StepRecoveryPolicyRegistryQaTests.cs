using Arcontio.Core;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // StepRecoveryPolicyRegistryQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Test QA EditMode per il registry dati delle policy di recupero locale degli
    /// step Job.
    /// </para>
    ///
    /// <para><b>v0.13e - Configurazione passiva da matrice Job</b></para>
    /// <para>
    /// Questi test verificano solo caricamento, normalizzazione e consultazione dei
    /// dati. Non istanziano <c>JobExecutionSystem</c>, non avanzano job, non
    /// applicano strategie, non producono <c>ICommand</c> e non trasformano il
    /// registry in un sistema decisionale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>JSON load</b>: converte failure kind e strategie testuali in enum.</item>
    ///   <item><b>Policy lookup</b>: restituisce policy dichiarate o Empty senza side effect.</item>
    ///   <item><b>Invalid entries</b>: ignora righe non valide senza rompere il catalogo.</item>
    /// </list>
    /// </summary>
    public sealed class StepRecoveryPolicyRegistryQaTests
    {
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new StepRecoveryPolicyRegistryQaTests();

                tests.RegistryLoadsPolicyDefinitionsFromJson();
                tests.DefaultRegistryContainsLockedDoorPolicy();
                tests.RegistryReturnsEmptyForMissingPolicy();
                tests.RegistryIgnoresInvalidFailureKindsAndStrategies();

                Debug.Log("[StepRecoveryPolicyRegistryQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StepRecoveryPolicyRegistryQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        [Test]
        public void RegistryLoadsPolicyDefinitionsFromJson()
        {
            var registry = new StepRecoveryPolicyRegistry();
            registry.LoadFromJson("{\"policies\":[{\"failureKind\":\"PathBlocked\",\"strategies\":[\"Repath\",\"FindAlternateCell\"],\"maxRetryCount\":2,\"maxRecoveryTicks\":12,\"maxSearchRadius\":3,\"maxAlternativeTargets\":1,\"maxRepathAttempts\":2}]}");

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.TryGetPolicy(JobStepFailureKind.PathBlocked, out var policy), Is.True);
            Assert.That(policy.FailureKind, Is.EqualTo(JobStepFailureKind.PathBlocked));
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.Repath), Is.True);
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.FindAlternateCell), Is.True);
            Assert.That(policy.MaxRetryCount, Is.EqualTo(2));
            Assert.That(policy.MaxRecoveryTicks, Is.EqualTo(12));
        }

        [Test]
        public void DefaultRegistryContainsLockedDoorPolicy()
        {
            var registry = StepRecoveryPolicyRegistry.LoadDefault();

            Assert.That(registry.TryGetPolicy(JobStepFailureKind.DoorLocked, out var policy), Is.True);
            Assert.That(policy.FailureKind, Is.EqualTo(JobStepFailureKind.DoorLocked));
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.FailJob), Is.True);
            Assert.That(policy.MaxRetryCount, Is.EqualTo(0));
        }

        [Test]
        public void RegistryReturnsEmptyForMissingPolicy()
        {
            var registry = new StepRecoveryPolicyRegistry();

            Assert.That(registry.TryGetPolicy(JobStepFailureKind.Timeout, out var policy), Is.False);
            Assert.That(policy.HasDeclaredData, Is.False);
            Assert.That(policy.FailureKind, Is.EqualTo(JobStepFailureKind.None));
        }

        [Test]
        public void RegistryIgnoresInvalidFailureKindsAndStrategies()
        {
            var registry = new StepRecoveryPolicyRegistry();
            registry.LoadFromJson("{\"policies\":[{\"failureKind\":\"NotARealFailure\",\"strategies\":[\"Repath\"]},{\"failureKind\":\"TargetInvalid\",\"strategies\":[\"Nope\",\"FailJob\"],\"maxRetryCount\":-9}]}");

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.TryGetPolicy(JobStepFailureKind.TargetInvalid, out var policy), Is.True);
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.FailJob), Is.True);
            Assert.That(policy.Strategies.Length, Is.EqualTo(1));
            Assert.That(policy.MaxRetryCount, Is.EqualTo(0));
        }
    }
}
