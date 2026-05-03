using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // JobTemplateRegistry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Registry minimale data-driven dei template di job caricati da JSON.
    /// </para>
    ///
    /// <para><b>Template dati, executor C#</b></para>
    /// <para>
    /// Il JSON descrive solo id, fasi e action kind. Non contiene target runtime,
    /// policy decisionali o logica esecutiva. I target arrivano da <c>JobRequest</c>
    /// e gli effetti restano affidati agli executor C# che producono <c>ICommand</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>LoadDefault</b>: carica <c>Resources/Arcontio/Jobs/job_templates</c>.</item>
    ///   <item><b>TryGetTemplate</b>: recupera un template per id stabile.</item>
    ///   <item><b>BuildPlan</b>: materializza un <c>JobPlan</c> senza leggere il World.</item>
    /// </list>
    /// </summary>
    public sealed class JobTemplateRegistry
    {
        public const string DefaultResourcePath = "Arcontio/Jobs/job_templates";
        public const string FoodKnownCommunityStockTemplateId = "food.eat_known_community_stock.v1";

        private readonly Dictionary<string, JobTemplateDefinition> _templates = new();

        public int Count => _templates.Count;

        public static JobTemplateRegistry LoadDefault()
        {
            var registry = new JobTemplateRegistry();
            var asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[JobTemplateRegistry] Missing Resources/{DefaultResourcePath}.json. Food job slice will stay inactive.");
                return registry;
            }

            registry.LoadFromJson(asset.text);
            return registry;
        }

        public void LoadFromJson(string json)
        {
            _templates.Clear();
            if (string.IsNullOrWhiteSpace(json))
                return;

            var root = JsonUtility.FromJson<JobTemplateFile>(json);
            if (root?.templates == null)
                return;

            for (int i = 0; i < root.templates.Length; i++)
            {
                var template = root.templates[i];
                if (template == null || string.IsNullOrWhiteSpace(template.templateId))
                    continue;

                _templates[template.templateId] = template;
            }
        }

        public bool TryGetTemplate(string templateId, out JobTemplateDefinition template)
        {
            return _templates.TryGetValue(templateId ?? string.Empty, out template);
        }

        public bool TryBuildPlan(string templateId, JobRequest request, out JobPlan plan, out string reason)
        {
            plan = null;
            reason = string.Empty;

            if (!TryGetTemplate(templateId, out var template))
            {
                reason = "TemplateMissing";
                return false;
            }

            var phases = template.phases ?? Array.Empty<JobTemplatePhaseDefinition>();
            var materializedPhases = new JobPhase[phases.Length];

            for (int p = 0; p < phases.Length; p++)
            {
                var phaseDef = phases[p];
                if (phaseDef == null)
                {
                    reason = "NullPhase";
                    return false;
                }

                if (!TryParseEnum(phaseDef.kind, JobPhaseKind.Custom, out JobPhaseKind phaseKind))
                {
                    reason = "InvalidPhaseKind";
                    return false;
                }

                var actionDefs = phaseDef.actions ?? Array.Empty<JobTemplateActionDefinition>();
                var actions = new JobAction[actionDefs.Length];
                for (int a = 0; a < actionDefs.Length; a++)
                {
                    var actionDef = actionDefs[a];
                    if (actionDef == null)
                    {
                        reason = "NullAction";
                        return false;
                    }

                    if (!TryParseEnum(actionDef.kind, JobActionKind.Custom, out JobActionKind actionKind))
                    {
                        reason = "InvalidActionKind";
                        return false;
                    }

                    actions[a] = MaterializeAction(actionDef.actionId, actionKind, request);
                }

                materializedPhases[p] = new JobPhase(
                    phaseDef.phaseId,
                    phaseKind,
                    string.IsNullOrWhiteSpace(phaseDef.phaseId) ? phaseKind.ToString() : phaseDef.phaseId,
                    actions.Length,
                    phaseDef.isInterruptible,
                    actions);
            }

            plan = new JobPlan(template.templateId, materializedPhases);
            reason = "PlanBuilt";
            return true;
        }

        private static JobAction MaterializeAction(string actionId, JobActionKind kind, JobRequest request)
        {
            if (kind == JobActionKind.MoveToCell)
                return new JobAction(actionId, kind, actionId, request.HasTargetCell, request.TargetCell, request.TargetObjectId, 0, string.Empty);

            if (kind == JobActionKind.Consume)
                return new JobAction(actionId, kind, actionId, request.HasTargetCell, request.TargetCell, request.TargetObjectId, 0, "known_food");

            return new JobAction(actionId, kind, actionId, request.HasTargetCell, request.TargetCell, request.TargetObjectId, 0, string.Empty);
        }

        private static bool TryParseEnum<TEnum>(string value, TEnum fallback, out TEnum parsed)
            where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                parsed = fallback;
                return true;
            }

            return Enum.TryParse(value, ignoreCase: true, out parsed);
        }
    }

    [Serializable]
    public sealed class JobTemplateFile
    {
        public JobTemplateDefinition[] templates;
    }

    [Serializable]
    public sealed class JobTemplateDefinition
    {
        public string templateId;
        public JobTemplatePhaseDefinition[] phases;
    }

    [Serializable]
    public sealed class JobTemplatePhaseDefinition
    {
        public string phaseId;
        public string kind;
        public bool isInterruptible = true;
        public JobTemplateActionDefinition[] actions;
    }

    [Serializable]
    public sealed class JobTemplateActionDefinition
    {
        public string actionId;
        public string kind;
    }
}
