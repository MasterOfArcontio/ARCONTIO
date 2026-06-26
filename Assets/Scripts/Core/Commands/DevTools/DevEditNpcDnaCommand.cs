// Assets/Scripts/Core/Commands/DevTools/DevEditNpcDnaCommand.cs
using UnityEngine;

namespace Arcontio.Core.Commands.DevTools
{
    // =============================================================================
    // NpcDnaEditableField
    // =============================================================================
    /// <summary>
    /// <para>
    /// Campo DNA modificabile dal pannello runtime ArcGraph.
    /// </para>
    ///
    /// <para><b>Contratto esplicito e limitato</b></para>
    /// <para>
    /// L'editor UI non invia payload generici o nomi di proprieta' arbitrari.
    /// Ogni valore modificabile deve passare da questo enum, cosi' il comando Core
    /// sa esattamente quali parti del profilo DNA possono essere sostituite.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Capacita'</b>: forza, resistenza, agilita' e intelligenza.</item>
    ///   <item><b>Disposizioni</b>: introversione, aggressivita', curiosita' e cooperazione.</item>
    ///   <item><b>Modulatori</b>: impulsivita' e avversione al rischio.</item>
    /// </list>
    /// </summary>
    public enum NpcDnaEditableField
    {
        Strength = 0,
        Endurance = 1,
        Agility = 2,
        BaseIntelligence = 3,
        Introversion = 4,
        Aggressiveness = 5,
        Curiosity = 6,
        Cooperativeness = 7,
        Impulsivity = 8,
        RiskAversion = 9
    }

    // =============================================================================
    // NpcDnaEditValue
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coppia campo/valore usata per applicare una modifica parziale al DNA NPC.
    /// </para>
    /// </summary>
    public readonly struct NpcDnaEditValue
    {
        public readonly NpcDnaEditableField Field;
        public readonly float Value01;

        public NpcDnaEditValue(NpcDnaEditableField field, float value01)
        {
            Field = field;
            Value01 = Mathf.Clamp01(value01);
        }
    }

    // =============================================================================
    // DevEditNpcDnaCommand
    // =============================================================================
    /// <summary>
    /// <para>
    /// Comando runtime autorizzato che sostituisce il DNA di un NPC con una nuova
    /// istanza costruita a partire dal profilo corrente e dai soli valori editati.
    /// </para>
    ///
    /// <para><b>UI -> Command Gateway -> World</b></para>
    /// <para>
    /// Il RightInspector non scrive mai in <c>World.NpcDna</c>. La UI raccoglie un
    /// draft, il bridge costruisce questo comando e il comando rivalida l'NPC al
    /// momento dell'esecuzione. Se l'NPC non esiste piu', la richiesta viene scartata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_npcId</b>: NPC destinatario della modifica.</item>
    ///   <item><b>_values</b>: valori DNA parziali normalizzati 0..1.</item>
    ///   <item><b>Execute</b>: crea un nuovo <c>NpcDnaProfile</c> e lo assegna al World.</item>
    /// </list>
    /// </summary>
    public sealed class DevEditNpcDnaCommand : ICommand
    {
        private readonly int _npcId;
        private readonly NpcDnaEditValue[] _values;

        // =============================================================================
        // DevEditNpcDnaCommand
        // =============================================================================
        /// <summary>
        /// <para>
        /// Conserva la richiesta intenzionale di modifica DNA. La validazione dello
        /// stato runtime resta dentro <see cref="Execute"/>.
        /// </para>
        /// </summary>
        public DevEditNpcDnaCommand(int npcId, NpcDnaEditValue[] values)
        {
            _npcId = npcId;
            _values = values == null ? System.Array.Empty<NpcDnaEditValue>() : values;
        }

        // =============================================================================
        // Execute
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la modifica parziale al profilo DNA corrente dell'NPC.
        /// </para>
        /// </summary>
        public void Execute(World world, MessageBus bus)
        {
            if (world == null || _values.Length == 0)
                return;

            if (!world.ExistsNpc(_npcId) || !world.NpcDna.TryGetValue(_npcId, out NpcDnaProfile dna) || dna == null)
                return;

            world.NpcDna[_npcId] = BuildEditedProfile(dna);
        }

        private NpcDnaProfile BuildEditedProfile(NpcDnaProfile dna)
        {
            float strength = dna.Capacities.Strength01;
            float endurance = dna.Capacities.Endurance01;
            float agility = dna.Capacities.Agility01;
            float intelligence = dna.Capacities.BaseIntelligence01;
            float introversion = dna.Dispositions.Introversion01;
            float aggressiveness = dna.Dispositions.Aggressiveness01;
            float curiosity = dna.Dispositions.Curiosity01;
            float cooperativeness = dna.Dispositions.Cooperativeness01;
            float impulsivity = dna.CognitiveModulators.Impulsivity01;
            float riskAversion = dna.CognitiveModulators.RiskAversion01;

            for (int i = 0; i < _values.Length; i++)
            {
                NpcDnaEditValue edit = _values[i];
                switch (edit.Field)
                {
                    case NpcDnaEditableField.Strength:
                        strength = edit.Value01;
                        break;
                    case NpcDnaEditableField.Endurance:
                        endurance = edit.Value01;
                        break;
                    case NpcDnaEditableField.Agility:
                        agility = edit.Value01;
                        break;
                    case NpcDnaEditableField.BaseIntelligence:
                        intelligence = edit.Value01;
                        break;
                    case NpcDnaEditableField.Introversion:
                        introversion = edit.Value01;
                        break;
                    case NpcDnaEditableField.Aggressiveness:
                        aggressiveness = edit.Value01;
                        break;
                    case NpcDnaEditableField.Curiosity:
                        curiosity = edit.Value01;
                        break;
                    case NpcDnaEditableField.Cooperativeness:
                        cooperativeness = edit.Value01;
                        break;
                    case NpcDnaEditableField.Impulsivity:
                        impulsivity = edit.Value01;
                        break;
                    case NpcDnaEditableField.RiskAversion:
                        riskAversion = edit.Value01;
                        break;
                }
            }

            return new NpcDnaProfile(
                dna.Identity,
                new NpcCapacities(
                    strength,
                    endurance,
                    agility,
                    intelligence,
                    CloneFloatArray(dna.Capacities.CompetenceCap)),
                new NpcPreferenceSeeds(CloneFloatArray(dna.Preferences.Seeds)),
                new NpcDispositions(
                    introversion,
                    aggressiveness,
                    curiosity,
                    cooperativeness),
                dna.SocialPosition,
                new NpcObligationFrame(CloneFloatArray(dna.ObligationFrame.Seeds), dna.ObligationFrame.CulturalOrigin),
                dna.Thresholds,
                new NpcCognitiveModulators(
                    impulsivity,
                    riskAversion,
                    dna.CognitiveModulators.Conformism01,
                    dna.CognitiveModulators.Optimism01,
                    dna.CognitiveModulators.StressResilience01,
                    dna.CognitiveModulators.Sociability01,
                    dna.CognitiveModulators.DriftResistance01,
                    dna.CognitiveModulators.TraumaSensitivity01,
                    dna.CognitiveModulators.MemoryResilience01,
                    dna.CognitiveModulators.Rumination01,
                    dna.CognitiveModulators.Gullibility01),
                dna.Traits,
                dna.Tags == null ? null : (string[])dna.Tags.Clone());
        }

        private static float[] CloneFloatArray(float[] values)
        {
            return values == null ? null : (float[])values.Clone();
        }
    }
}
