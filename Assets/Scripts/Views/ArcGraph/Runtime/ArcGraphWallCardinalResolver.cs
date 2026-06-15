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
    ///   <item><b>ResolveSpriteKey</b>: produce la chiave della sub-sprite dentro la striscia muro.</item>
    ///   <item><b>ResolveMask</b>: calcola la maschera <c>N/E/S/W</c> in quattro cifre.</item>
    ///   <item><b>ResolveSubSpriteName</b>: traduce la maschera nel nome dello sprite sliced.</item>
    ///   <item><b>IsWallSnapshot</b>: riconosce gli snapshot con <c>VisualKind = wall</c>.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphWallCardinalResolver
    {
        private const string WallVisualKind = "wall";
        private const char SpriteSheetSeparator = '#';

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
        /// Risolve la sprite key finale di un oggetto, usando la variante cardinale
        /// solo se l'oggetto e' un muro.
        /// </para>
        ///
        /// <para><b>Convenzione spritesheet</b></para>
        /// <para>
        /// La maschera e' ordinata come <c>N/E/S/W</c>. La chiave finale usa la
        /// forma <c>sheet#subSprite</c>, per esempio
        /// <c>MapGrid/Sprites/Objects/wall_stone#wall_stone_1010</c>. Questo
        /// permette a Unity di caricare una sola PNG sliced, alta 83 pixel e
        /// divisa in slot larghi 32 pixel, senza richiedere file separati.
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

            if (string.IsNullOrWhiteSpace(baseSpriteKey))
                return string.Empty;

            string mask = ResolveMask(snapshot.Cell, wallCells);
            string subSpriteName = ResolveSubSpriteName(
                baseSpriteKey,
                mask,
                snapshot);
            if (string.IsNullOrWhiteSpace(subSpriteName))
                return baseSpriteKey;

            return baseSpriteKey + SpriteSheetSeparator + subSpriteName;
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
        // ResolveSubSpriteName
        // =============================================================================
        /// <summary>
        /// <para>
        /// Traduce una maschera cardinale nel nome della sub-sprite dentro la
        /// striscia muro.
        /// </para>
        ///
        /// <para><b>Contratto grafico striscia 17 slot</b></para>
        /// <para>
        /// Il file grafico e' una striscia unica di 17 elementi <c>32x83</c>, larga
        /// quindi <c>544x83</c>. I nomi delle sub-sprite devono essere coerenti con
        /// la maschera <c>N/E/S/W</c>. Solo il muro orizzontale <c>0101</c> possiede
        /// due varianti, scelte con hash stabile della cella e dell'oggetto per
        /// evitare sia pattern completamente fisso sia casualita' non deterministica.
        /// </para>
        /// </summary>
        public static string ResolveSubSpriteName(
            string baseSpriteKey,
            string mask,
            ArcGraphObjectVisualSnapshot snapshot)
        {
            string prefix = ResolveSpriteNamePrefix(baseSpriteKey);
            if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(mask))
                return string.Empty;

            if (mask == "0101")
            {
                int variant = ResolveHorizontalVariant(snapshot);
                return prefix + "_0101_" + variant;
            }

            if (IsKnownWallMask(mask))
                return prefix + "_" + mask;

            return string.Empty;
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

        private static bool IsKnownWallMask(string mask)
        {
            switch (mask)
            {
                case "1010":
                case "1111":
                case "1101":
                case "0111":
                case "1110":
                case "1011":
                case "0110":
                case "0011":
                case "1100":
                case "1001":
                case "0001":
                case "0100":
                case "0010":
                case "1000":
                case "0000":
                    return true;
                default:
                    return false;
            }
        }

        private static int ResolveHorizontalVariant(ArcGraphObjectVisualSnapshot snapshot)
        {
            unchecked
            {
                // Hash intenzionalmente piccolo e stabile: non deve essere una
                // sorgente casuale runtime, ma solo una distribuzione ripetibile
                // delle due varianti orizzontali disponibili nello spritesheet.
                int hash = 17;
                hash = (hash * 31) + snapshot.ObjectId;
                hash = (hash * 31) + snapshot.Cell.X;
                hash = (hash * 31) + snapshot.Cell.Y;
                hash = (hash * 31) + snapshot.Cell.Z;
                return (hash & 1) == 0 ? 0 : 1;
            }
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

        private static string ResolveSpriteNamePrefix(string baseSpriteKey)
        {
            string normalized = string.IsNullOrWhiteSpace(baseSpriteKey)
                ? string.Empty
                : baseSpriteKey.Trim().Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            int separatorIndex = normalized.LastIndexOf('/');
            if (separatorIndex < 0 || separatorIndex >= normalized.Length - 1)
                return normalized;

            return normalized.Substring(separatorIndex + 1);
        }
    }
}
