using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentSeedBankAreaState
    // =============================================================================
    /// <summary>
    /// <para>
    /// Payload ambientale di seed bank diffusa per area.
    /// </para>
    ///
    /// <para><b>Principio architetturale: semi naturali come pressione ecologica</b></para>
    /// <para>
    /// La seed bank descrive la disponibilita' astratta di semi naturali in un'area.
    /// Non crea item, non istanzia piante e non rappresenta scorte agricole: quei
    /// concetti avranno contratti separati quando la simulazione lo richiedera'.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AreaId</b>: area a cui appartiene la seed bank.</item>
    ///   <item><b>Entries</b>: specie o categorie disponibili in forma normalizzata.</item>
    ///   <item><b>TotalAmount01</b>: disponibilita' aggregata normalizzata.</item>
    ///   <item><b>AverageViability01</b>: vitalita' media normalizzata.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentSeedBankAreaState
    {
        private static readonly EnvironmentSeedBankEntry[] EmptyEntries =
            new EnvironmentSeedBankEntry[0];

        public EnvironmentAreaId AreaId { get; }
        public IReadOnlyList<EnvironmentSeedBankEntry> Entries { get; }
        public float TotalAmount01 { get; }
        public float AverageViability01 { get; }

        // =============================================================================
        // EnvironmentSeedBankAreaState
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il payload seed bank copiando le entry fornite.
        /// </para>
        /// </summary>
        public EnvironmentSeedBankAreaState(
            EnvironmentAreaId areaId,
            IReadOnlyList<EnvironmentSeedBankEntry> entries)
        {
            AreaId = areaId;
            Entries = CopyEntries(entries);
            TotalAmount01 = ComputeTotalAmount01(Entries);
            AverageViability01 = ComputeAverageViability01(Entries);
        }

        private static IReadOnlyList<EnvironmentSeedBankEntry> CopyEntries(
            IReadOnlyList<EnvironmentSeedBankEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return EmptyEntries;

            var copy = new EnvironmentSeedBankEntry[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                // EnvironmentSeedBankEntry normalizza gia' amount e viability. La copia
                // impedisce al chiamante di sostituire l'array originario dopo il build.
                copy[i] = entries[i];
            }

            return copy;
        }

        private static float ComputeTotalAmount01(
            IReadOnlyList<EnvironmentSeedBankEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return 0f;

            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
                total += entries[i].Amount01;

            return EnvironmentMath.Clamp01(total / entries.Count);
        }

        private static float ComputeAverageViability01(
            IReadOnlyList<EnvironmentSeedBankEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return 0f;

            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
                total += entries[i].Viability01;

            return EnvironmentMath.Clamp01(total / entries.Count);
        }
    }
}
