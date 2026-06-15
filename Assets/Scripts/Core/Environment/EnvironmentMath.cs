namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentMath
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper matematico minimale per i contratti ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: normalizzazione locale senza dipendenze Unity</b></para>
    /// <para>
    /// La foundation ambiente deve restare Core-side e data-only. Questo helper
    /// evita di importare <c>UnityEngine.Mathf</c> solo per clamp semplici, mantenendo
    /// i tipi ambientali indipendenti dal rendering.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Clamp01</b>: normalizza valori continui nel range 0..1.</item>
    /// </list>
    /// </summary>
    internal static class EnvironmentMath
    {
        // =============================================================================
        // Clamp01
        // =============================================================================
        /// <summary>
        /// <para>
        /// Limita un valore float al range normalizzato <c>0..1</c>.
        /// </para>
        /// </summary>
        public static float Clamp01(float value)
        {
            // La forma esplicita evita dipendenze Unity e resta prevedibile in test
            // isolati di compilazione.
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }
    }
}
