using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainTileUvMap
    // =============================================================================
    /// <summary>
    /// <para>
    /// Mappa terrain ArcGraph per convertire <c>tileId</c> in coordinate UV.
    /// </para>
    ///
    /// <para><b>Principio architetturale: atlas visuale senza asset authority</b></para>
    /// <para>
    /// Questa classe non possiede la texture, non carica asset e non crea materiali.
    /// Riceve soltanto le dimensioni dell'atlas e la dimensione delle tile, poi
    /// calcola UV per un atlas regolare. Replica la convenzione legacy utile a
    /// MapGrid, ma resta nel namespace ArcGraph e non dipende da
    /// <c>MapGridTileAtlas</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AtlasWidthPixels/AtlasHeightPixels</b>: dimensioni atlas ricevute.</item>
    ///   <item><b>TilePixels</b>: dimensione tile in pixel.</item>
    ///   <item><b>TilesPerRow/TilesPerColumn</b>: griglia derivata.</item>
    ///   <item><b>_tileUv</b>: registrazione tileId -> cella atlas.</item>
    ///   <item><b>TryGetUvQuad</b>: conversione tileId -> quattro UV Unity.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainTileUvMap
    {
        private const float MinimumInsetSafetyPixels = 0.0001f;
        private readonly Dictionary<int, Vector2Int> _tileUv = new();

        public int AtlasWidthPixels { get; }
        public int AtlasHeightPixels { get; }
        public int TilePixels { get; }
        public float UvInsetPixels { get; }
        public int TilesPerRow { get; }
        public int TilesPerColumn { get; }
        public bool IsValid => TilesPerRow > 0 && TilesPerColumn > 0;
        public int RegisteredTileCount => _tileUv.Count;

        // =============================================================================
        // ArcGraphTerrainTileUvMap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una mappa UV terrain da dimensioni atlas primitive.
        /// </para>
        ///
        /// <para><b>Normalizzazione conservativa</b></para>
        /// <para>
        /// Valori non validi vengono normalizzati a una griglia minima sicura. Questo
        /// evita divisioni per zero durante QA o harness test. La diagnostica futura
        /// potra' comunque leggere <c>IsValid</c> e <c>RegisteredTileCount</c>.
        /// </para>
        /// </summary>
        public ArcGraphTerrainTileUvMap(
            int atlasWidthPixels,
            int atlasHeightPixels,
            int tilePixels,
            float uvInsetPixels = 0.5f)
        {
            AtlasWidthPixels = atlasWidthPixels > 0 ? atlasWidthPixels : 1;
            AtlasHeightPixels = atlasHeightPixels > 0 ? atlasHeightPixels : 1;
            TilePixels = tilePixels > 0 ? tilePixels : 1;
            UvInsetPixels = NormalizeInsetPixels(uvInsetPixels, TilePixels);

            TilesPerRow = Mathf.Max(1, AtlasWidthPixels / TilePixels);
            TilesPerColumn = Mathf.Max(1, AtlasHeightPixels / TilePixels);
        }

        // =============================================================================
        // Register
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una singola associazione <c>tileId -> cella atlas</c>.
        /// </para>
        ///
        /// <para><b>Registrazione esplicita</b></para>
        /// <para>
        /// Il metodo non valida l'esistenza reale del tile nella texture, perche'
        /// questa classe non possiede la texture. Si limita a conservare coordinate
        /// griglia ricevute dal chiamante.
        /// </para>
        /// </summary>
        public void Register(int tileId, int uvX, int uvY)
        {
            _tileUv[tileId] = new Vector2Int(uvX, uvY);
        }

        // =============================================================================
        // RegisterMany
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una sequenza di definizioni UV terrain.
        /// </para>
        ///
        /// <para><b>Batch semplice</b></para>
        /// <para>
        /// Il batch serve per importare definizioni da configurazioni o harness test
        /// senza esporre il dizionario interno. Le definizioni duplicate sostituiscono
        /// il valore precedente, come nel comportamento legacy.
        /// </para>
        /// </summary>
        public void RegisterMany(IEnumerable<ArcGraphTerrainTileUvDefinition> definitions)
        {
            if (definitions == null)
                return;

            foreach (var definition in definitions)
                Register(definition.TileId, definition.UvX, definition.UvY);
        }

        // =============================================================================
        // TryGetUvQuad
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un <c>tileId</c> nelle quattro UV Unity del quad.
        /// </para>
        ///
        /// <para><b>Convenzione atlas</b></para>
        /// <para>
        /// Come nel legacy MapGrid, <c>UvY = 0</c> indica la prima riga in alto
        /// dell'immagine. Unity usa origine UV in basso, quindi il metodo converte
        /// la riga prima di calcolare <c>vMin</c> e <c>vMax</c>.
        /// </para>
        /// </summary>
        public bool TryGetUvQuad(
            int tileId,
            out Vector2 uv0,
            out Vector2 uv1,
            out Vector2 uv2,
            out Vector2 uv3)
        {
            bool found = _tileUv.TryGetValue(tileId, out var cell);
            if (!found)
                cell = Vector2Int.zero;

            int yFromBottom = (TilesPerColumn - 1) - cell.y;

            // L'inset sposta le UV appena dentro la cella atlas. Questo evita che
            // il campionamento del materiale terrain legga esattamente il bordo fra
            // due slice adiacenti durante zoom/pan, senza cambiare geometria mesh,
            // tile id, asset PNG o import settings.
            float inset = UvInsetPixels;
            float uMin = ((cell.x * TilePixels) + inset) / AtlasWidthPixels;
            float vMin = ((yFromBottom * TilePixels) + inset) / AtlasHeightPixels;
            float uMax = (((cell.x + 1) * TilePixels) - inset) / AtlasWidthPixels;
            float vMax = (((yFromBottom + 1) * TilePixels) - inset) / AtlasHeightPixels;

            uv0 = new Vector2(uMin, vMin);
            uv1 = new Vector2(uMax, vMin);
            uv2 = new Vector2(uMax, vMax);
            uv3 = new Vector2(uMin, vMax);

            return found;
        }

        private static float NormalizeInsetPixels(
            float requestedInsetPixels,
            int tilePixels)
        {
            if (requestedInsetPixels <= 0f)
                return 0f;

            float maxInset = Mathf.Max(0f, (tilePixels * 0.5f) - MinimumInsetSafetyPixels);
            return Mathf.Min(requestedInsetPixels, maxInset);
        }
    }
}
