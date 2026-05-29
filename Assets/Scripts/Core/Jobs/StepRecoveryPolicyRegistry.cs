using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // StepRecoveryPolicyRegistry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Registry dati per caricare da JSON le policy dichiarative di recupero locale
    /// degli step Job.
    /// </para>
    ///
    /// <para><b>v0.13e - Configurazione recovery locale da matrice Job</b></para>
    /// <para>
    /// Questo componente traduce in dati runtime la matrice operativa dei failure e
    /// dei recovery locali, ma non applica ancora recovery produttivo. Non legge il
    /// <c>World</c>, non ricostruisce fasi, non sostituisce target, non programma
    /// retry, non emette command e non richiede rivalutazioni cognitive. Il suo
    /// scopo e' rendere le policy consultabili da un futuro evaluator senza lasciare
    /// costanti sparse nel codice.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>LoadDefault</b>: carica <c>Resources/Arcontio/Jobs/job_recovery_policies</c>.</item>
    ///   <item><b>LoadFromJson</b>: deserializza definizioni dichiarative e normalizza enum/limiti.</item>
    ///   <item><b>TryGetPolicy</b>: restituisce la policy per failure kind senza decidere recoverability.</item>
    /// </list>
    /// </summary>
    public sealed class StepRecoveryPolicyRegistry
    {
        public const string DefaultResourcePath = "Arcontio/Jobs/job_recovery_policies";

        private readonly Dictionary<JobStepFailureKind, StepRecoveryPolicy> _policies = new();

        public int Count => _policies.Count;

        public static StepRecoveryPolicyRegistry LoadDefault()
        {
            var registry = new StepRecoveryPolicyRegistry();
            var asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
                return registry;

            registry.LoadFromJson(asset.text);
            return registry;
        }

        public void LoadFromJson(string json)
        {
            _policies.Clear();
            if (string.IsNullOrWhiteSpace(json))
                return;

            var root = JsonUtility.FromJson<StepRecoveryPolicyFile>(json);
            if (root?.policies == null)
                return;

            for (int i = 0; i < root.policies.Length; i++)
            {
                var definition = root.policies[i];
                if (definition == null)
                    continue;

                if (!TryParseEnum(definition.failureKind, out JobStepFailureKind failureKind)
                    || failureKind == JobStepFailureKind.None)
                {
                    continue;
                }

                _policies[failureKind] = new StepRecoveryPolicy(
                    failureKind,
                    ParseStrategies(definition.strategies),
                    definition.maxRetryCount,
                    definition.maxRecoveryTicks,
                    definition.maxSearchRadius,
                    definition.maxAlternativeTargets,
                    definition.maxRepathAttempts);
            }
        }

        public bool TryGetPolicy(JobStepFailureKind failureKind, out StepRecoveryPolicy policy)
        {
            if (_policies.TryGetValue(failureKind, out policy))
                return true;

            policy = StepRecoveryPolicy.Empty();
            return false;
        }

        private static StepRecoveryStrategy[] ParseStrategies(string[] values)
        {
            if (values == null || values.Length == 0)
                return Array.Empty<StepRecoveryStrategy>();

            var parsed = new List<StepRecoveryStrategy>(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                if (!TryParseEnum(values[i], out StepRecoveryStrategy strategy)
                    || strategy == StepRecoveryStrategy.None)
                {
                    continue;
                }

                parsed.Add(strategy);
            }

            return parsed.ToArray();
        }

        private static bool TryParseEnum<TEnum>(string value, out TEnum parsed)
            where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                parsed = default;
                return false;
            }

            return Enum.TryParse(value, ignoreCase: true, out parsed);
        }
    }

    [Serializable]
    public sealed class StepRecoveryPolicyFile
    {
        public StepRecoveryPolicyDefinition[] policies;
    }

    [Serializable]
    public sealed class StepRecoveryPolicyDefinition
    {
        public string failureKind;
        public string[] strategies;
        public int maxRetryCount;
        public int maxRecoveryTicks;
        public int maxSearchRadius;
        public int maxAlternativeTargets;
        public int maxRepathAttempts;
    }
}
