using System;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // DecisionScoreContribution
    // =============================================================================
    /// <summary>
    /// <para>
    /// Singolo contributo nominato allo score di un candidato decisionale.
    /// </para>
    ///
    /// <para><b>Explainability del Decision Layer</b></para>
    /// <para>
    /// Ogni termine dello scoring deve restare leggibile in debug. La struttura
    /// conserva label e valore senza esporre logica, cosi' la UI o i test possono
    /// spiegare perche' una intenzione abbia vinto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Label</b>: nome stabile del termine di scoring.</item>
    ///   <item><b>Value</b>: contributo numerico gia' pesato.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionScoreContribution
    {
        public readonly string Label;
        public readonly float Value;

        public DecisionScoreContribution(string label, float value)
        {
            Label = label ?? string.Empty;
            Value = value;
        }
    }

    // =============================================================================
    // DecisionScoringConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione compatta dei pesi globali della Fase 2 del Decision Layer.
    /// </para>
    ///
    /// <para><b>Pesi globali separati dai correttivi per intent</b></para>
    /// <para>
    /// Questa struct resta il formato operativo minimale usato dallo scoring per i
    /// pesi comuni. I correttivi per singolo intent vivono invece nel catalogo JSON
    /// dedicato <c>decision_intent_score_config.json</c>, cosi' <c>game_params.json</c>
    /// non diventa un contenitore indistinto di configurazioni cognitive.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>needUrgencyWeight</b>: peso della pressione del bisogno.</item>
    ///   <item><b>competence/preference/obligation</b>: contributi soggettivi e sociali.</item>
    ///   <item><b>memoryConfidenceWeight</b>: premio alla credenza selezionata dalla query.</item>
    ///   <item><b>floor</b>: minimi espliciti per bisogno critico e obbligo alto.</item>
    /// </list>
    /// </summary>
    public struct DecisionScoringConfig
    {
        public float needUrgencyWeight;
        public float competenceWeight;
        public float preferenceWeight;
        public float obligationWeight;
        public float memoryConfidenceWeight;
        public float cognitiveModulatorWeight;
        public float criticalNeedFloor;
        public float highObligationFloor;
        public float highObligationThreshold;

        public static DecisionScoringConfig Default()
        {
            return new DecisionScoringConfig
            {
                needUrgencyWeight = 1.00f,
                competenceWeight = 0.20f,
                preferenceWeight = 0.25f,
                obligationWeight = 0.30f,
                memoryConfidenceWeight = 0.35f,
                cognitiveModulatorWeight = 0.15f,
                criticalNeedFloor = 1.25f,
                highObligationFloor = 1.00f,
                highObligationThreshold = 0.75f
            };
        }
    }

    // =============================================================================
    // DecisionIntentScoreEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione locale di scoring per una singola intenzione decisionale.
    /// </para>
    ///
    /// <para><b>Catalogo pesi intent, non routing operativo</b></para>
    /// <para>
    /// Questa struttura non decide quale job verra' creato e non introduce scorciatoie
    /// verso il Job Layer. Permette solo di rendere data-driven piccoli correttivi
    /// numerici dello score, lasciando invariati catalogo intent, query belief e
    /// costruzione delle richieste job.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>intent</b>: nome testuale del <c>DecisionIntentKind</c>.</item>
    ///   <item><b>baseScore</b>: piccolo contributo additivo esplicito.</item>
    ///   <item><b>needMultiplier</b>: moltiplicatore della pressione del bisogno.</item>
    ///   <item><b>beliefMultiplier/memoryMultiplier</b>: correttivi per target soggettivi.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct DecisionIntentScoreEntry
    {
        public string intent;
        public bool enabled;
        public float baseScore;
        public float needMultiplier;
        public float beliefMultiplier;
        public float memoryMultiplier;
        public float riskPenalty;
        public int cooldownTicks;

        public DecisionIntentKind IntentKind
        {
            get
            {
                if (Enum.TryParse(intent ?? string.Empty, ignoreCase: true, out DecisionIntentKind kind))
                    return kind;

                return DecisionIntentKind.None;
            }
        }

        public float ResolveNeedMultiplier()
        {
            return needMultiplier > 0f ? needMultiplier : 1f;
        }

        public float ResolveBeliefMultiplier()
        {
            return beliefMultiplier > 0f ? beliefMultiplier : 1f;
        }

        public float ResolveMemoryMultiplier()
        {
            return memoryMultiplier > 0f ? memoryMultiplier : 1f;
        }
    }

    // =============================================================================
    // DecisionIntentScoreConfig
    // =============================================================================
    /// <summary>
    /// <para>
    /// Configurazione JSON dei pesi globali e dei correttivi per singolo intent.
    /// </para>
    ///
    /// <para><b>Un solo punto dati per lo scoring operativo</b></para>
    /// <para>
    /// Il catalogo vive in <c>decision_intent_score_config.json</c> e non in
    /// <c>game_params.json</c>. Questo mantiene separata la configurazione generale
    /// della simulazione dal tuning decisionale degli intent, senza cambiare il
    /// percorso Decisione -> Job.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Pesi globali</b>: conversione diretta verso <c>DecisionScoringConfig</c>.</item>
    ///   <item><b>intents</b>: elenco compatto di override per intent specifici.</item>
    ///   <item><b>Fallback</b>: default conservativi per config assente o incompleta.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct DecisionIntentScoreConfig
    {
        public float needUrgencyWeight;
        public float competenceWeight;
        public float preferenceWeight;
        public float obligationWeight;
        public float memoryConfidenceWeight;
        public float cognitiveModulatorWeight;
        public float criticalNeedFloor;
        public float highObligationFloor;
        public float highObligationThreshold;
        public DecisionIntentScoreEntry[] intents;

        public static DecisionIntentScoreConfig Default()
        {
            var global = DecisionScoringConfig.Default();
            return new DecisionIntentScoreConfig
            {
                needUrgencyWeight = global.needUrgencyWeight,
                competenceWeight = global.competenceWeight,
                preferenceWeight = global.preferenceWeight,
                obligationWeight = global.obligationWeight,
                memoryConfidenceWeight = global.memoryConfidenceWeight,
                cognitiveModulatorWeight = global.cognitiveModulatorWeight,
                criticalNeedFloor = global.criticalNeedFloor,
                highObligationFloor = global.highObligationFloor,
                highObligationThreshold = global.highObligationThreshold,
                intents = new[]
                {
                    CreateEntry(DecisionIntentKind.EatCarriedFood, 0.25f, 1.15f),
                    CreateEntry(DecisionIntentKind.EatKnownFood, 0f, 1f),
                    CreateEntry(DecisionIntentKind.SearchFood, 0f, 1f),
                    CreateEntry(DecisionIntentKind.WaitAndObserve, 0.05f, 0.25f),
                }
            };
        }

        public DecisionScoringConfig ToScoringConfig()
        {
            var fallback = DecisionScoringConfig.Default();
            return new DecisionScoringConfig
            {
                needUrgencyWeight = needUrgencyWeight > 0f ? needUrgencyWeight : fallback.needUrgencyWeight,
                competenceWeight = competenceWeight > 0f ? competenceWeight : fallback.competenceWeight,
                preferenceWeight = preferenceWeight > 0f ? preferenceWeight : fallback.preferenceWeight,
                obligationWeight = obligationWeight > 0f ? obligationWeight : fallback.obligationWeight,
                memoryConfidenceWeight = memoryConfidenceWeight > 0f ? memoryConfidenceWeight : fallback.memoryConfidenceWeight,
                cognitiveModulatorWeight = cognitiveModulatorWeight > 0f ? cognitiveModulatorWeight : fallback.cognitiveModulatorWeight,
                criticalNeedFloor = criticalNeedFloor > 0f ? criticalNeedFloor : fallback.criticalNeedFloor,
                highObligationFloor = highObligationFloor > 0f ? highObligationFloor : fallback.highObligationFloor,
                highObligationThreshold = highObligationThreshold > 0f ? highObligationThreshold : fallback.highObligationThreshold
            };
        }

        public bool TryGetEntry(DecisionIntentKind kind, out DecisionIntentScoreEntry entry)
        {
            if (intents != null)
            {
                for (int i = 0; i < intents.Length; i++)
                {
                    var candidate = intents[i];
                    if (candidate.IntentKind == kind)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }

        private static DecisionIntentScoreEntry CreateEntry(DecisionIntentKind kind, float baseScore, float needMultiplier)
        {
            return new DecisionIntentScoreEntry
            {
                intent = kind.ToString(),
                enabled = true,
                baseScore = baseScore,
                needMultiplier = needMultiplier,
                beliefMultiplier = 1f,
                memoryMultiplier = 1f,
                riskPenalty = 0f,
                cooldownTicks = 0
            };
        }
    }

    [Serializable]
    public sealed class DecisionIntentScoreConfigFile
    {
        public DecisionIntentScoreConfig decisionIntentScore;
    }

    // =============================================================================
    // DecisionIntentScoreConfigLoader
    // =============================================================================
    /// <summary>
    /// <para>
    /// Caricatore Resources del catalogo pesi degli intent decisionali.
    /// </para>
    ///
    /// <para><b>Configurazione runtime leggera</b></para>
    /// <para>
    /// Il loader viene chiamato durante il bootstrap del <c>World</c>. Se il file non
    /// esiste o non contiene dati validi, assegna i default storici e non blocca la
    /// simulazione: la configurazione deve essere utile al tuning, non diventare un
    /// nuovo punto di fragilita' runtime.
    /// </para>
    /// </summary>
    public static class DecisionIntentScoreConfigLoader
    {
        private const string ResourcePath = "Arcontio/Config/decision_intent_score_config";

        public static void LoadIntoWorld(World world)
        {
            if (world == null)
                return;

            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                world.Global.DecisionIntentScore = DecisionIntentScoreConfig.Default();
                return;
            }

            try
            {
                var file = JsonUtility.FromJson<DecisionIntentScoreConfigFile>(asset.text);
                world.Global.DecisionIntentScore = file != null
                    ? file.decisionIntentScore
                    : DecisionIntentScoreConfig.Default();
            }
            catch
            {
                world.Global.DecisionIntentScore = DecisionIntentScoreConfig.Default();
            }
        }
    }
}
