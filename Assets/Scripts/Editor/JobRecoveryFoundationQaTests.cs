using Arcontio.Core;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arcontio.Tests
{
    // =============================================================================
    // JobRecoveryFoundationQaTests
    // =============================================================================
    /// <summary>
    /// <para>
    /// Matrice QA EditMode per proteggere la foundation passiva del recupero locale
    /// degli step Job.
    /// </para>
    ///
    /// <para><b>v0.11c.04g - Recovery foundation senza runtime recovery</b></para>
    /// <para>
    /// Questi test verificano solo separazioni e assenze: nessun mapping automatico,
    /// nessuna command emission, nessuna mutazione del World e nessuna semantica
    /// produttiva collegata ai vocabolari recovery. Non istanziano
    /// <c>JobExecutionSystem</c> e non avanzano job.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CLI harness</b>: entry point diagnostico per batchmode quando il runner XML non parte.</item>
    ///   <item><b>Step result boundary</b>: failed/blocked/waiting restano contratti runtime separati.</item>
    ///   <item><b>Recovery DTO neutrality</b>: policy e result non autorizzano recovery.</item>
    ///   <item><b>No command/World</b>: i tipi recovery non implementano command e non espongono World.</item>
    /// </list>
    /// </summary>
    public sealed class JobRecoveryFoundationQaTests
    {
        // =============================================================================
        // RunFromCommandLine
        // =============================================================================
        /// <summary>
        /// <para>
        /// Entry point minimo per eseguire questi QA test da Unity batchmode quando
        /// il runner CLI standard non produce il file XML dei risultati.
        /// </para>
        /// </summary>
        public static void RunFromCommandLine()
        {
            try
            {
                var tests = new JobRecoveryFoundationQaTests();

                tests.FailedStepResultRemainsDistinctFromRecoveryResultKinds();
                tests.RecoveryResultNoneDoesNotMeanSuccessFailureOrExecutedRecovery();
                tests.EmptyStepRecoveryPolicyDoesNotAuthorizeRecovery();
                tests.BlockedAndWaitingRemainTechnicalWaitGateResults();
                tests.JobStepFailureKindDoesNotMapAutomaticallyToStepRecoveryStrategy();
                tests.StepRecoveryPolicyDoesNotMapAutomaticallyToJobRecoveryResult();
                tests.RecoveryFoundationTypesDoNotImplementICommand();
                tests.RecoveryFoundationTypesDoNotExposeWorldMutationSurface();

                Debug.Log("[JobRecoveryFoundationQaTests] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[JobRecoveryFoundationQaTests] FAIL\n" + ex);
                EditorApplication.Exit(1);
            }
        }

        // =============================================================================
        // FailedStepResultRemainsDistinctFromRecoveryResultKinds
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che il fallimento runtime dello step resti distinto dai result
        /// kind futuri <c>PhaseFailed</c> e <c>JobFailed</c>.
        /// </para>
        /// </summary>
        [Test]
        public void FailedStepResultRemainsDistinctFromRecoveryResultKinds()
        {
            var failed = StepResult.Failed(JobFailureReason.StepFailed, "qa-failed");

            Assert.That(failed.Status, Is.EqualTo(StepResultStatus.Failed));
            Assert.That(failed.Status, Is.Not.EqualTo((object)JobRecoveryResultKind.PhaseFailed));
            Assert.That(failed.Status, Is.Not.EqualTo((object)JobRecoveryResultKind.JobFailed));
            Assert.That(typeof(StepResultStatus), Is.Not.EqualTo(typeof(JobRecoveryResultKind)));
        }

        // =============================================================================
        // RecoveryResultNoneDoesNotMeanSuccessFailureOrExecutedRecovery
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>JobRecoveryResultKind.None</c> significhi solo assenza di
        /// risultato dichiarato, non successo, fallimento o recovery eseguita.
        /// </para>
        /// </summary>
        [Test]
        public void RecoveryResultNoneDoesNotMeanSuccessFailureOrExecutedRecovery()
        {
            var result = JobRecoveryResult.None();

            Assert.That(result.Kind, Is.EqualTo(JobRecoveryResultKind.None));
            Assert.That(result.HasDeclaredResult, Is.False);
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.Recovered));
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.PhaseFailed));
            Assert.That(result.Kind, Is.Not.EqualTo(JobRecoveryResultKind.JobFailed));
        }

        // =============================================================================
        // EmptyStepRecoveryPolicyDoesNotAuthorizeRecovery
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una policy vuota resti assenza di dati dichiarati, senza
        /// autorizzare alcuna strategia.
        /// </para>
        /// </summary>
        [Test]
        public void EmptyStepRecoveryPolicyDoesNotAuthorizeRecovery()
        {
            var policy = StepRecoveryPolicy.Empty();

            Assert.That(policy.HasDeclaredData, Is.False);
            Assert.That(policy.FailureKind, Is.EqualTo(JobStepFailureKind.None));
            Assert.That(policy.Strategies, Is.Empty);
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.RetrySameAction), Is.False);
            Assert.That(policy.ContainsStrategy(StepRecoveryStrategy.WaitAndRetry), Is.False);
        }

        // =============================================================================
        // BlockedAndWaitingRemainTechnicalWaitGateResults
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che <c>Blocked</c> e <c>Waiting</c> restino esiti tecnici di
        /// <c>StepResult</c>, non alias della strategia recovery <c>WaitAndRetry</c>.
        /// </para>
        /// </summary>
        [Test]
        public void BlockedAndWaitingRemainTechnicalWaitGateResults()
        {
            var blocked = StepResult.Blocked(2, "blocked");
            var waiting = StepResult.Waiting(3, "waiting");

            Assert.That(blocked.Status, Is.EqualTo(StepResultStatus.Blocked));
            Assert.That(waiting.Status, Is.EqualTo(StepResultStatus.Waiting));
            Assert.That(blocked.Status, Is.Not.EqualTo((object)StepRecoveryStrategy.WaitAndRetry));
            Assert.That(waiting.Status, Is.Not.EqualTo((object)StepRecoveryStrategy.WaitAndRetry));
            Assert.That(typeof(StepResultStatus), Is.Not.EqualTo(typeof(StepRecoveryStrategy)));
        }

        // =============================================================================
        // JobStepFailureKindDoesNotMapAutomaticallyToStepRecoveryStrategy
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che failure kind e strategy restino vocabolari separati, senza
        /// API di mapping automatica tra causa e risposta.
        /// </para>
        /// </summary>
        [Test]
        public void JobStepFailureKindDoesNotMapAutomaticallyToStepRecoveryStrategy()
        {
            Assert.That(typeof(JobStepFailureKind), Is.Not.EqualTo(typeof(StepRecoveryStrategy)));
            Assert.That(HasPublicMemberReturning(typeof(JobStepFailureKind), typeof(StepRecoveryStrategy)), Is.False);
            Assert.That(HasPublicMemberReturning(typeof(StepRecoveryStrategy), typeof(JobStepFailureKind)), Is.False);
        }

        // =============================================================================
        // StepRecoveryPolicyDoesNotMapAutomaticallyToJobRecoveryResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che una policy dichiarativa non produca automaticamente un
        /// recovery result.
        /// </para>
        /// </summary>
        [Test]
        public void StepRecoveryPolicyDoesNotMapAutomaticallyToJobRecoveryResult()
        {
            var policy = new StepRecoveryPolicy(
                JobStepFailureKind.PathBlocked,
                new[] { StepRecoveryStrategy.Repath },
                1,
                2,
                3,
                4,
                5);

            Assert.That(policy.HasDeclaredData, Is.True);
            Assert.That(HasPublicMemberReturning(typeof(StepRecoveryPolicy), typeof(JobRecoveryResult)), Is.False);
            Assert.That(HasPublicMemberReturning(typeof(StepRecoveryPolicy), typeof(JobRecoveryResultKind)), Is.False);
        }

        // =============================================================================
        // RecoveryFoundationTypesDoNotImplementICommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che nessun tipo recovery foundation sia un command simulativo.
        /// </para>
        /// </summary>
        [Test]
        public void RecoveryFoundationTypesDoNotImplementICommand()
        {
            foreach (var type in RecoveryFoundationTypes())
            {
                Assert.That(typeof(ICommand).IsAssignableFrom(type), Is.False, type.Name);
            }
        }

        // =============================================================================
        // RecoveryFoundationTypesDoNotExposeWorldMutationSurface
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che i tipi recovery foundation non espongano membri pubblici che
        /// accettano o restituiscono <c>World</c>.
        /// </para>
        /// </summary>
        [Test]
        public void RecoveryFoundationTypesDoNotExposeWorldMutationSurface()
        {
            foreach (var type in RecoveryFoundationTypes())
            {
                var membersExposeWorld = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Any(MemberReferencesWorld);

                Assert.That(membersExposeWorld, Is.False, type.Name);
            }
        }

        private static Type[] RecoveryFoundationTypes()
        {
            return new[]
            {
                typeof(JobStepFailureKind),
                typeof(StepRecoveryStrategy),
                typeof(StepRecoveryPolicy),
                typeof(JobRecoveryResultKind),
                typeof(JobRecoveryResult)
            };
        }

        private static bool HasPublicMemberReturning(Type owner, Type returnType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            return owner.GetMethods(flags).Any(method => method.ReturnType == returnType)
                || owner.GetProperties(flags).Any(property => property.PropertyType == returnType)
                || owner.GetFields(flags).Any(field => field.FieldType == returnType);
        }

        private static bool MemberReferencesWorld(MemberInfo member)
        {
            if (member is FieldInfo field)
                return ReferencesWorld(field.FieldType);

            if (member is PropertyInfo property)
                return ReferencesWorld(property.PropertyType);

            if (member is MethodInfo method)
            {
                if (ReferencesWorld(method.ReturnType))
                    return true;

                return method.GetParameters().Any(parameter => ReferencesWorld(parameter.ParameterType));
            }

            if (member is ConstructorInfo constructor)
                return constructor.GetParameters().Any(parameter => ReferencesWorld(parameter.ParameterType));

            return false;
        }

        private static bool ReferencesWorld(Type type)
        {
            if (type == typeof(World))
                return true;

            if (type.IsArray)
                return ReferencesWorld(type.GetElementType());

            return false;
        }
    }
}
