namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// Dati runtime della MapGrid (solo view-data).
    ///
    /// Contiene lo stato "a celle" minimo:
    /// - tileId terreno per cella
    /// - flag bloccato (ostacolo pieno / occluder)
    /// - flag terreno non attraversabile (acqua, lava futura, voragini future)
    ///
    /// IMPORTANTISSIMO:
    /// - Questo NON e' il World del simulatore.
    /// - E' un buffer di dati che la view usa per renderizzare.
    /// - La sorgente puo' essere:
    ///   - layout JSON (ora)
    ///   - simulatore (poi)
    ///   - procedurale (poi)
    /// </summary>
    public sealed class MapGridData
    {
        public readonly int Width;
        public readonly int Height;

        private readonly int[] _terrain;
        private readonly bool[] _blocked;
        private readonly bool[] _terrainTraversalBlocked;

        public MapGridData(int w, int h)
        {
            Width = w;
            Height = h;

            _terrain = new int[w * h];
            _blocked = new bool[w * h];
            _terrainTraversalBlocked = new bool[w * h];
        }

        /// <summary>
        /// Check bounds in modo veloce e sicuro.
        /// (uint trick evita branch su negativo)
        /// </summary>
        public bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

        /// <summary>
        /// Converte (x,y) in indice lineare.
        /// Convenzione: row-major (x + y*Width).
        /// </summary>
        public int Index(int x, int y) => x + y * Width;

        public int GetTerrain(int x, int y) => _terrain[Index(x, y)];
        public void SetTerrain(int x, int y, int tileId) => _terrain[Index(x, y)] = tileId;

        public bool IsBlocked(int x, int y) => _blocked[Index(x, y)];
        public void SetBlocked(int x, int y, bool v) => _blocked[Index(x, y)] = v;

        // =============================================================================
        // IsTerrainTraversalBlocked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce se la cella e' bloccata dal terreno di base, non da un oggetto
        /// solido o da un occluder.
        /// </para>
        ///
        /// <para><b>Principio architetturale: separazione tra blocco visuale e blocco di attraversamento</b></para>
        /// <para>
        /// Il vecchio renderer MapGrid usa <c>IsBlocked</c> per disegnare muri e
        /// occluder. L'acqua, pero', deve impedire il movimento senza essere
        /// renderizzata come muro legacy. Per questo il blocco dovuto al terreno
        /// vive in un flag separato, che gli adapter ArcGraph possono poi combinare
        /// con il blocco oggettivo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>_terrainTraversalBlocked</b>: buffer booleano parallelo alla griglia terrain.</item>
        /// </list>
        /// </summary>
        public bool IsTerrainTraversalBlocked(int x, int y) => _terrainTraversalBlocked[Index(x, y)];

        // =============================================================================
        // SetTerrainTraversalBlocked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta il blocco di attraversamento prodotto dal terreno della cella.
        /// </para>
        /// </summary>
        public void SetTerrainTraversalBlocked(int x, int y, bool v) => _terrainTraversalBlocked[Index(x, y)] = v;

        // =============================================================================
        // IsMovementBlocked
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il blocco complessivo utile ai consumer grafici e, in futuro,
        /// ai ponti runtime che devono sapere se una cella e' attraversabile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>IsBlocked</b>: ostacoli pieni, muri e occluder legacy.</item>
        ///   <item><b>IsTerrainTraversalBlocked</b>: blocco dovuto al terreno, per esempio acqua.</item>
        /// </list>
        /// </summary>
        public bool IsMovementBlocked(int x, int y)
        {
            int index = Index(x, y);
            return _blocked[index] || _terrainTraversalBlocked[index];
        }
    }
}
