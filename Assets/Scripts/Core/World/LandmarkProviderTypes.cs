using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // LandmarkProviderKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Identificatore compatto del modulo che propone landmark manuali al registry.
    /// </para>
    ///
    /// <para><b>Principio architetturale: identita' provider senza stringhe runtime</b></para>
    /// <para>
    /// Il tipo e' volutamente un enum byte: serve a separare moduli diversi che
    /// possono usare lo stesso owner id locale, senza aggiungere payload testuali o
    /// riferimenti di dominio dentro i nodi landmark.
    /// </para>
    /// </summary>
    public enum LandmarkProviderKind : byte
    {
        None = 0,
        EnvironmentBiosphere = 1,
        SupportOpenSpace = 2,
        ShopModule = 3,
    }

    // =============================================================================
    // LandmarkProviderKey
    // =============================================================================
    /// <summary>
    /// <para>
    /// Chiave leggera che collega temporaneamente un candidato landmark al modulo
    /// proprietario e al suo owner locale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: sidecar mapping modulare</b></para>
    /// <para>
    /// Il registry landmark resta compatto: la chiave viaggia solo nel flusso
    /// candidate -> resolution e permette al coordinator di restituire il risultato
    /// al provider corretto.
    /// </para>
    /// </summary>
    public readonly struct LandmarkProviderKey : IEquatable<LandmarkProviderKey>
    {
        public static readonly LandmarkProviderKey None = new LandmarkProviderKey(LandmarkProviderKind.None, 0);

        public readonly LandmarkProviderKind Kind;
        public readonly int OwnerId;

        public LandmarkProviderKey(LandmarkProviderKind kind, int ownerId)
        {
            Kind = kind;
            OwnerId = ownerId < 0 ? 0 : ownerId;
        }

        public bool IsValid => Kind != LandmarkProviderKind.None && OwnerId > 0;

        public bool Equals(LandmarkProviderKey other)
        {
            return Kind == other.Kind && OwnerId == other.OwnerId;
        }

        public override bool Equals(object obj)
        {
            return obj is LandmarkProviderKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)Kind * 397) ^ OwnerId;
        }

        public static bool operator ==(LandmarkProviderKey left, LandmarkProviderKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LandmarkProviderKey left, LandmarkProviderKey right)
        {
            return !left.Equals(right);
        }
    }

    // =============================================================================
    // IWorldLandmarkProvider
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto minimo per i moduli che vogliono proporre landmark manuali durante
    /// la rebuild globale del registry.
    /// </para>
    ///
    /// <para><b>Principio architetturale: provider isolato, rebuild unica</b></para>
    /// <para>
    /// Ogni provider produce solo candidati data-only e riceve solo le proprie
    /// resolution. Il provider non crea memory, belief, job o UI: collega un modulo
    /// al registry oggettivo senza trasformarlo in manager globale.
    /// </para>
    /// </summary>
    public interface IWorldLandmarkProvider
    {
        LandmarkProviderKind ProviderKind { get; }

        int BuildLandmarkCandidates(
            World world,
            List<LandmarkRegistry.ManualLandmarkCandidate> outCandidates);

        void ApplyLandmarkResolutions(
            IReadOnlyList<LandmarkRegistry.ManualLandmarkResolution> resolutions);
    }

    // =============================================================================
    // IWorldLandmarkCoverageProvider
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contratto opzionale per provider che riempiono vuoti dopo una prima rebuild
    /// dei landmark strutturali e dei provider primari.
    /// </para>
    ///
    /// <para><b>Principio architetturale: supporto dopo copertura reale</b></para>
    /// <para>
    /// I landmark di supporto devono sapere quali landmark esistono gia'. Per questo
    /// non vengono prodotti nel passaggio primario: ricevono un registry gia'
    /// popolato e propongono solo candidati supplementari.
    /// </para>
    /// </summary>
    public interface IWorldLandmarkCoverageProvider : IWorldLandmarkProvider
    {
        int BuildCoverageLandmarkCandidates(
            World world,
            LandmarkRegistry registry,
            List<LandmarkRegistry.ManualLandmarkCandidate> outCandidates);
    }
}
