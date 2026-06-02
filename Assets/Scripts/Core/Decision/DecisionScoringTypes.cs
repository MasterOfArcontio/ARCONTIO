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
    /// Configurazione dei pesi della Fase 2 del Decision Layer.
    /// </para>
    ///
    /// <para><b>Pesi nominati e centralizzati</b></para>
    /// <para>
    /// I pesi restano in una struct separata per evitare costanti sparse negli
    /// evaluator. In questa sessione e' attivo solo <c>needUrgencyWeight</c>; i
    /// campi successivi entrano nelle sessioni 6-8.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>needUrgencyWeight</b>: peso della pressione del bisogno.</item>
    ///   <item><b>competenceWeight/preferenceWeight</b>: riservati alla sessione 6.</item>
    ///   <item><b>obligationWeight</b>: riservato alla sessione 7.</item>
    ///   <item><b>memoryConfidenceWeight</b>: riservato alla sessione 8.</item>
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
    /// Prima di questa struttura i pesi globali vivevano solo in codice. La v0.19b li
    /// espone in JSON senza cambiare la semantica base: se il file manca o e' parziale,
    /// i valori di default restano quelli storici.
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
