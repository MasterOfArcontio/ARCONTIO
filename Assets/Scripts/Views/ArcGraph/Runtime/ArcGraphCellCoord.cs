using System;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphCellCoord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coordinate discrete di una cella renderizzabile da <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: coordinate 3D discrete preparatorie</b></para>
    /// <para>
    /// Il runtime attuale lavora ancora su una mappa 2D, ma la fondazione grafica
    /// deve essere gia' compatibile con livelli di altitudine discreti. Per questo
    /// la coordinata include sempre <c>X</c>, <c>Y</c> e <c>Z</c>. Nella fase
    /// iniziale il valore operativo sara' quasi sempre <c>Z = 0</c>, ma il contratto
    /// evita di dover cambiare tutte le firme quando saranno introdotti sottosuolo,
    /// tetti, altitudini e celle cielo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>X</b>: coordinata orizzontale sulla griglia.</item>
    ///   <item><b>Y</b>: coordinata verticale sulla griglia 2D.</item>
    ///   <item><b>Z</b>: livello discreto di altitudine o profondita'.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphCellCoord : IEquatable<ArcGraphCellCoord>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        // =============================================================================
        // ArcGraphCellCoord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una coordinata cella completa.
        /// </para>
        ///
        /// <para><b>Compatibilita' con la mappa attuale</b></para>
        /// <para>
        /// Il parametro <c>z</c> ha default zero per consentire al rendering attuale
        /// di usare subito il contratto senza inventare livelli non ancora simulati.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>x/y</b>: coordinate cella esistenti.</item>
        ///   <item><b>z</b>: livello discreto futuro, default <c>0</c>.</item>
        /// </list>
        /// </summary>
        public ArcGraphCellCoord(int x, int y, int z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        // =============================================================================
        // Equals
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta due coordinate cella usando tutti e tre gli assi discreti.
        /// </para>
        ///
        /// <para><b>Dirty state deterministico</b></para>
        /// <para>
        /// Il confronto completo evita collisioni logiche tra la stessa cella
        /// <c>X/Y</c> su livelli <c>Z</c> diversi. Questo sara' essenziale quando
        /// una cella cielo, un tetto e un pavimento potranno occupare la stessa
        /// colonna spaziale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>X</b>: deve coincidere.</item>
        ///   <item><b>Y</b>: deve coincidere.</item>
        ///   <item><b>Z</b>: deve coincidere.</item>
        /// </list>
        /// </summary>
        public bool Equals(ArcGraphCellCoord other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is ArcGraphCellCoord other && Equals(other);
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
            return $"({X},{Y},{Z})";
        }
    }

    // =============================================================================
    // ArcGraphChunkCoord
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coordinate discrete di un chunk grafico dentro un livello <c>Z</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: aggiornamento localizzato</b></para>
    /// <para>
    /// Il rendering futuro non deve ridisegnare tutta la mappa quando cambia una
    /// singola cella. Questa struttura identifica il blocco grafico sporco, cosi'
    /// i layer potranno aggiornare solo le porzioni necessarie della mappa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>X</b>: indice chunk orizzontale.</item>
    ///   <item><b>Y</b>: indice chunk verticale.</item>
    ///   <item><b>Z</b>: livello discreto del chunk.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphChunkCoord : IEquatable<ArcGraphChunkCoord>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        // =============================================================================
        // ArcGraphChunkCoord
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una coordinata chunk completa.
        /// </para>
        ///
        /// <para><b>Compatibilita' con z = 0</b></para>
        /// <para>
        /// Il parametro <c>z</c> resta opzionale per permettere al renderer attuale
        /// di ragionare ancora su un solo livello senza perdere il contratto futuro.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>x/y</b>: indici chunk sul piano.</item>
        ///   <item><b>z</b>: livello discreto del chunk.</item>
        /// </list>
        /// </summary>
        public ArcGraphChunkCoord(int x, int y, int z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        // =============================================================================
        // Equals
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta due chunk usando piano e livello discreto.
        /// </para>
        ///
        /// <para><b>Dirty chunk multilivello</b></para>
        /// <para>
        /// Due chunk con stessi <c>X/Y</c> ma diverso <c>Z</c> non sono equivalenti:
        /// uno potrebbe rappresentare il suolo e l'altro un tetto o un livello
        /// sotterraneo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>X</b>: deve coincidere.</item>
        ///   <item><b>Y</b>: deve coincidere.</item>
        ///   <item><b>Z</b>: deve coincidere.</item>
        /// </list>
        /// </summary>
        public bool Equals(ArcGraphChunkCoord other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is ArcGraphChunkCoord other && Equals(other);
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
