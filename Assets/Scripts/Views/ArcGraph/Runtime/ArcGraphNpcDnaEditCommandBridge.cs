using System.Collections.Generic;
using System.Globalization;
using Arcontio.Core;
using Arcontio.Core.Commands.DevTools;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcDnaDraftValue
    // =============================================================================
    /// <summary>
    /// <para>
    /// Valore temporaneo preparato dal RightInspector per una barra DNA editabile.
    /// </para>
    ///
    /// <para><b>Draft UI, non dato runtime</b></para>
    /// <para>
    /// Il campo <c>RowKey</c> e' una chiave della view, non un nome di proprieta'
    /// del Core. Il bridge la mappera' verso <c>NpcDnaEditableField</c> prima di
    /// costruire il comando autorizzato.
    /// </para>
    /// </summary>
    public readonly struct ArcGraphNpcDnaDraftValue
    {
        public readonly string RowKey;
        public readonly float Value01;

        public ArcGraphNpcDnaDraftValue(string rowKey, float value01)
        {
            RowKey = string.IsNullOrWhiteSpace(rowKey) ? string.Empty : rowKey.Trim();
            Value01 = Mathf.Clamp01(value01);
        }
    }

    // =============================================================================
    // ArcGraphNpcDnaEditCommandBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Bridge autorizzato tra RightInspector e comando di modifica DNA NPC.
    /// </para>
    ///
    /// <para><b>UI -> draft -> bridge -> command</b></para>
    /// <para>
    /// La view produce un draft locale fatto di chiavi riga e valori 0..1. Questo
    /// bridge valida il target NPC, traduce le chiavi in campi Core espliciti e
    /// accoda <see cref="DevEditNpcDnaCommand"/> al <see cref="SimulationHost"/>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>simulationHost</b>: host su cui accodare il comando.</item>
    ///   <item><b>RequestApplyDnaDraft</b>: entry point chiamato dal tasto Applica.</item>
    ///   <item><b>TryMapField</b>: mappa chiavi UI in enum Core.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphNpcDnaEditCommandBridge : MonoBehaviour
    {
        [SerializeField] private bool bridgeEnabled = true;
        [SerializeField] private SimulationHost simulationHost;

        // =============================================================================
        // SetSimulationHost
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna l'host runtime su cui accodare il comando autorizzato.
        /// </para>
        /// </summary>
        public void SetSimulationHost(SimulationHost host)
        {
            simulationHost = host;
        }

        // =============================================================================
        // RequestApplyDnaDraft
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il draft UI in comando Core e lo accoda alla simulazione.
        /// </para>
        /// </summary>
        public bool RequestApplyDnaDraft(
            ArcUiSelectionTarget target,
            ArcGraphNpcDnaDraftValue[] draftValues)
        {
            if (!bridgeEnabled
                || target.Kind != ArcUiSelectionTargetKind.Npc
                || draftValues == null
                || draftValues.Length == 0)
            {
                return false;
            }

            if (!int.TryParse(target.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out int npcId) || npcId <= 0)
                return false;

            var values = new List<NpcDnaEditValue>(draftValues.Length);
            for (int i = 0; i < draftValues.Length; i++)
            {
                if (TryMapField(draftValues[i].RowKey, out NpcDnaEditableField field))
                    values.Add(new NpcDnaEditValue(field, draftValues[i].Value01));
            }

            if (values.Count == 0)
                return false;

            SimulationHost host = simulationHost != null ? simulationHost : SimulationHost.Instance;
            if (host == null)
                return false;

            host.EnqueueExternalCommand(new DevEditNpcDnaCommand(npcId, values.ToArray()));
            return true;
        }

        private static bool TryMapField(string rowKey, out NpcDnaEditableField field)
        {
            switch (rowKey)
            {
                case "dna_strength":
                    field = NpcDnaEditableField.Strength;
                    return true;
                case "dna_endurance":
                    field = NpcDnaEditableField.Endurance;
                    return true;
                case "dna_agility":
                    field = NpcDnaEditableField.Agility;
                    return true;
                case "dna_intelligence":
                    field = NpcDnaEditableField.BaseIntelligence;
                    return true;
                case "dna_introversion":
                    field = NpcDnaEditableField.Introversion;
                    return true;
                case "dna_aggressiveness":
                    field = NpcDnaEditableField.Aggressiveness;
                    return true;
                case "dna_curiosity":
                    field = NpcDnaEditableField.Curiosity;
                    return true;
                case "dna_cooperation":
                    field = NpcDnaEditableField.Cooperativeness;
                    return true;
                case "dna_impulsivity":
                    field = NpcDnaEditableField.Impulsivity;
                    return true;
                case "dna_risk":
                    field = NpcDnaEditableField.RiskAversion;
                    return true;
                default:
                    field = default;
                    return false;
            }
        }
    }
}
