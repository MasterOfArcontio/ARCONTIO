using System;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentCellCoord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coordinata discreta di una cella ambientale nella griglia sistemica di
    /// ARCONTIO.
    /// </para>
    ///
    /// <para><b>Principio architetturale: spazio ambientale multilivello</b></para>
    /// <para>
    /// La biosfera non deve nascere come estensione 2D del vecchio renderer. Anche
    /// se la prima foundation usera' quasi sempre <c>Z = 0</c>, il contratto
    /// ambientale conserva subito il livello discreto per restare compatibile con
    /// altitudine, sottosuolo, tetti e celle cielo futuri.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>X</b>: coordinata orizzontale della cella.</item>
    ///   <item><b>Y</b>: coordinata verticale sul piano della mappa.</item>
    ///   <item><b>Z</b>: livello discreto, con default concettuale <c>0</c>.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentCellCoord : IEquatable<EnvironmentCellCoord>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        // =============================================================================
        // EnvironmentCellCoord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una coordinata ambientale completa.
        /// </para>
        ///
        /// <para><b>Compatibilita' progressiva</b></para>
        /// <para>
        /// Il parametro <c>z</c> resta opzionale per permettere ai primi sistemi
        /// data-only di ragionare sul piano principale senza perdere la firma futura.
        /// </para>
        /// </summary>
        public EnvironmentCellCoord(int x, int y, int z = 0)
        {
            // Le coordinate restano semplici value type: nessuna validazione di
            // bounds viene fatta qui, per non legare il contratto alla mappa attiva.
            X = x;
            Y = y;
            Z = z;
        }

        // =============================================================================
        // Equals
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta due coordinate usando tutti gli assi discreti.
        /// </para>
        /// </summary>
        public bool Equals(EnvironmentCellCoord other)
        {
            // Il confronto include Z per evitare collisioni future tra suolo,
            // sottosuolo, tetti e celle cielo nella stessa colonna X/Y.
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is EnvironmentCellCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // Hash esplicito e stabile sui tre assi discreti.
                int hash = 17;
                hash = (hash * 31) + X;
                hash = (hash * 31) + Y;
                hash = (hash * 31) + Z;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }
    }

    // =============================================================================
    // EnvironmentChunkCoord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coordinata discreta di un chunk ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dirty work localizzabile</b></para>
    /// <para>
    /// La biosfera futura non deve scandire tutta la mappa per ogni cambiamento.
    /// Questo value object prepara indici per chunk e maschere di area, senza
    /// introdurre ancora alcun renderer o sistema di invalidazione produttivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>X</b>: indice chunk orizzontale.</item>
    ///   <item><b>Y</b>: indice chunk verticale.</item>
    ///   <item><b>Z</b>: livello discreto del chunk.</item>
    /// </list>
    /// </summary>
    public readonly struct EnvironmentChunkCoord : IEquatable<EnvironmentChunkCoord>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        // =============================================================================
        // EnvironmentChunkCoord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una coordinata chunk ambientale.
        /// </para>
        /// </summary>
        public EnvironmentChunkCoord(int x, int y, int z = 0)
        {
            // Anche qui nessuna dipendenza da dimensione chunk concreta: il calcolo
            // cella -> chunk vivra' in un helper futuro configurabile.
            X = x;
            Y = y;
            Z = z;
        }

        // =============================================================================
        // Equals
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta due chunk ambientali includendo il livello discreto.
        /// </para>
        /// </summary>
        public bool Equals(EnvironmentChunkCoord other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is EnvironmentChunkCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + X;
                hash = (hash * 31) + Y;
                hash = (hash * 31) + Z;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"chunk({X},{Y},{Z})";
        }
    }
}
