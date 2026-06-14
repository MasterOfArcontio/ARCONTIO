using System;
using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWallCardinalResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver passivo che calcola la variante cardinale di un muro osservando
    /// solo gli snapshot oggetto gia' presenti nel layer ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: variante visuale senza lettura del World</b></para>
    /// <para>
    /// Il resolver non interroga <c>World</c>, non legge <c>MapGrid</c>, non carica
    /// asset e non crea oggetti scena. Riceve snapshot gia' copiati e produce una
    /// chiave sprite testuale. In questo modo la scelta della variante grafica del
    /// muro resta una conseguenza della render queue, non una nuova sorgente di
    /// verita' simulativa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildWallCellIndex</b>: indicizza le celle occupate da muri compatibili.</item>
    ///   <item><b>ResolveSpriteKey</b>: aggiunge il suffisso cardinale alla sprite key base.</item>
    ///   <item><b>ResolveMask</b>: calcola la maschera <c>N/E/S/W</c> in quattro cifre.</item>
    ///   <item><b>IsWallSnapshot</b>: riconosce gli snapshot con <c>VisualKind = wall</c>.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphWallCardinalResolver
    {
        private const string WallVisualKind = "wall";

        // =============================================================================
        // BuildWallCellIndex
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un indice leggero delle celle che contengono muri.
        /// </para>
        ///
        /// <para><b>Contratto CPU-leggero</b></para>
        /// <para>
        /// L'indice viene costruito una sola volta per build della queue oggetti.
        /// La chiave primaria e' la famiglia visuale del muro, ricavata da
        /// <c>VisualResolverKey</c> o, in assenza, da <c>DefId</c>. Questo evita che
        /// muri di famiglie diverse si colleghino automaticamente tra loro.
        /// </para>
        /// </summary>
        public static Dictionary<string, HashSet<ArcGraphCellCoord>> BuildWallCellIndex(
            IReadOnlyList<ArcGraphObjectVisualSnapshot> snapshots)
        {
            var index = new Dictionary<string, HashSet<ArcGraphCellCoord>>(StringComparer.Ordinal);
            if (snapshots == null)
                return index;

            for (int i = 0; i < snapshots.Count; i++)
            {
                ArcGraphObjectVisualSnapshot snapshot = snapshots[i];
                if (!IsWallSnapshot(snapshot))
                    continue;

                string familyKey = ResolveFamilyKey(snapshot);
                if (string.IsNullOrWhiteSpace(familyKey))
                    continue;

                if (!index.TryGetValue(familyKey, out var cells))
                {
                    cells = new HashSet<ArcGraphCellCoord>();
                    index[familyKey] = cells;
                }

                // Una cella muro e' identificata dalla coordinata discreta completa,
                // incluso Z. Questo evita collegamenti verticali tra piani diversi.
                cells.Add(snapshot.Cell);
            }

            return index;
        }

        // =============================================================================
        // ResolveSpriteKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la sprite key finale di un oggetto, aggiungendo il suffisso
        /// cardinale solo se l'oggetto e' un muro.
        /// </para>
        ///
        /// <para><b>Convenzione variante</b></para>
        /// <para>
        /// La maschera e' ordinata come <c>N/E/S/W</c>. Per esempio
        /// <c>1010</c> significa muro collegato a nord e sud, mentre
        /// <c>0101</c> significa collegato a est e ovest. La sprite key finale
        /// diventa quindi, per esempio,
        /// <c>MapGrid/Sprites/Objects/wall_stone_1010</c>.
        /// </para>
        /// </summary>
        public static string ResolveSpriteKey(
            ArcGraphObjectVisualSnapshot snapshot,
            IReadOnlyDictionary<string, HashSet<ArcGraphCellCoord>> wallCellsByFamily)
        {
            string baseSpriteKey = ResolveBaseSpriteKey(snapshot);
            if (!IsWallSnapshot(snapshot))
                return baseSpriteKey;

            string familyKey = ResolveFamilyKey(snapshot);
            if (string.IsNullOrWhiteSpace(familyKey)
                || wallCellsByFamily == null
                || !wallCellsByFamily.TryGetValue(familyKey, out var wallCells))
            {
                return baseSpriteKey;
            }

            string mask = ResolveMask(snapshot.Cell, wallCells);
            if (string.IsNullOrWhiteSpace(baseSpriteKey))
                return string.Empty;

            return baseSpriteKey + "_" + mask;
        }

        // =============================================================================
        // ResolveMask
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola la maschera cardinale <c>N/E/S/W</c> per una cella muro.
        /// </para>
        ///
        /// <para><b>Regola locale</b></para>
        /// <para>
        /// Il metodo controlla soltanto le quattro celle cardinali sullo stesso
        /// livello Z. Non considera diagonali, stanze, pavimenti o terreno. Questo
        /// mantiene la responsabilita' limitata alla scelta della variante muro.
        /// </para>
        /// </summary>
        public static string ResolveMask(
            ArcGraphCellCoord cell,
            ISet<ArcGraphCellCoord> wallCells)
        {
            if (wallCells == null)
                return "0000";

            bool north = wallCells.Contains(new ArcGraphCellCoord(cell.X, cell.Y + 1, cell.Z));
            bool east = wallCells.Contains(new ArcGraphCellCoord(cell.X + 1, cell.Y, cell.Z));
            bool south = wallCells.Contains(new ArcGraphCellCoord(cell.X, cell.Y - 1, cell.Z));
            bool west = wallCells.Contains(new ArcGraphCellCoord(cell.X - 1, cell.Y, cell.Z));

            return (north ? "1" : "0")
                   + (east ? "1" : "0")
                   + (south ? "1" : "0")
                   + (west ? "1" : "0");
        }

        // =============================================================================
        // IsWallSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Determina se uno snapshot oggetto deve partecipare al resolver muri.
        /// </para>
        ///
        /// <para><b>Filtro stretto</b></para>
        /// <para>
        /// Sono considerati muri solo gli oggetti non trasportati con
        /// <c>VisualKind = wall</c>. Il resolver non usa nomi file, sprite key o
        /// footprint per dedurre implicitamente che un oggetto sia un muro.
        /// </para>
        /// </summary>
        public static bool IsWallSnapshot(ArcGraphObjectVisualSnapshot snapshot)
        {
            return snapshot.ObjectId > 0
                   && !snapshot.IsHeld
                   && string.Equals(
                       snapshot.VisualKind,
                       WallVisualKind,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveBaseSpriteKey(ArcGraphObjectVisualSnapshot snapshot)
        {
            return string.IsNullOrWhiteSpace(snapshot.SpriteKey)
                ? string.Empty
                : snapshot.SpriteKey.Trim();
        }

        private static string ResolveFamilyKey(ArcGraphObjectVisualSnapshot snapshot)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.VisualResolverKey))
                return snapshot.VisualResolverKey.Trim();

            return string.IsNullOrWhiteSpace(snapshot.DefId)
                ? string.Empty
                : snapshot.DefId.Trim();
        }
    }
}
