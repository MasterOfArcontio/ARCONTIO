using System;
using System.Collections.Generic;
using Arcontio.Core;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiNpcOption
    // =============================================================================
    /// <summary>
    /// <para>
    /// Opzione NPC minimale mostrabile dalla UI ArcGraph quando un pannello deve
    /// scegliere un proprietario o un target umano senza ricevere riferimenti runtime
    /// mutabili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: World -> snapshot -> UI</b></para>
    /// <para>
    /// La UI non deve conservare <c>NpcDnaProfile</c>, componenti, dizionari del
    /// mondo o riferimenti a entita' vive. Questa struttura copia solo id e nome:
    /// al click, il command gateway rivalidera' l'id contro il World corrente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId</b>: identificatore runtime dell'NPC.</item>
    ///   <item><b>DisplayName</b>: nome gia' normalizzato per etichette compatte.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphUiNpcOption
    {
        public readonly int NpcId;
        public readonly string DisplayName;

        public bool IsValid => NpcId > 0;

        // =============================================================================
        // ArcGraphUiNpcOption
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una voce NPC copiando solo valori primitivi.
        /// </para>
        /// </summary>
        public ArcGraphUiNpcOption(int npcId, string displayName)
        {
            NpcId = npcId;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? "NPC " + npcId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : displayName.Trim();
        }
    }

    // =============================================================================
    // ArcGraphUiNpcOptionProvider
    // =============================================================================
    /// <summary>
    /// <para>
    /// Provider read-only per le liste NPC usate dai pannelli ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: provider autorizzato, UI asciutta</b></para>
    /// <para>
    /// Il pannello azione deve poter mostrare "Owner: NPC X" per il food stock e
    /// per gli oggetti che accettano proprieta'. Per non far leggere il World alla
    /// UI, questo componente usa il <see cref="ArcGraphRuntimeContextProvider"/>
    /// gia' autorizzato, costruisce una lista ordinata di snapshot e la restituisce
    /// come array di valori.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>runtimeContextProvider</b>: sorgente read-only del World corrente.</item>
    ///   <item><b>_buffer</b>: lista temporanea riusata per ridurre allocazioni locali.</item>
    ///   <item><b>GetNpcOptions</b>: produce snapshot id/nome ordinati per id.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphUiNpcOptionProvider : MonoBehaviour
    {
        [SerializeField] private ArcGraphRuntimeContextProvider runtimeContextProvider;

        private readonly List<ArcGraphUiNpcOption> _buffer = new List<ArcGraphUiNpcOption>(16);

        // =============================================================================
        // SetRuntimeContextProvider
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il provider runtime autorizzato usato per produrre snapshot NPC.
        /// </para>
        /// </summary>
        public void SetRuntimeContextProvider(ArcGraphRuntimeContextProvider provider)
        {
            runtimeContextProvider = provider;
        }

        // =============================================================================
        // GetNpcOptions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce una fotografia compatta degli NPC presenti nel World.
        /// </para>
        /// </summary>
        public ArcGraphUiNpcOption[] GetNpcOptions()
        {
            _buffer.Clear();

            World world = ResolveWorld();
            if (world == null || world.NpcDna == null)
                return Array.Empty<ArcGraphUiNpcOption>();

            foreach (var pair in world.NpcDna)
            {
                int npcId = pair.Key;
                if (npcId <= 0)
                    continue;

                string name = pair.Value != null ? pair.Value.Identity.Name : string.Empty;
                _buffer.Add(new ArcGraphUiNpcOption(npcId, name));
            }

            _buffer.Sort((left, right) => left.NpcId.CompareTo(right.NpcId));
            return _buffer.ToArray();
        }

        // =============================================================================
        // ResolveWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Recupera il World dal context provider senza esporlo al pannello UI.
        /// </para>
        /// </summary>
        private World ResolveWorld()
        {
            if (runtimeContextProvider == null)
                return null;

            ArcGraphRuntimeContext context = runtimeContextProvider.BuildTerrainRuntimeContext();
            return context?.World;
        }
    }
}
