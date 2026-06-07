using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainChunkMeshBuilder
    // =============================================================================
    /// <summary>
    /// <para>
    /// Builder passivo che trasforma snapshot terrain ArcGraph in mesh data per
    /// chunk.
    /// </para>
    ///
    /// <para><b>Principio architetturale: chunk renderer senza scena</b></para>
    /// <para>
    /// Il builder legge solo <c>ArcGraphTerrainLayer</c>, una UV map e una policy
    /// visuale. Produce array mesh, ma non crea <c>GameObject</c>, non aggiunge
    /// componenti Unity, non carica asset e non legge <c>MapGridData</c>. E' il
    /// primo passo produttivo del terrain renderer, mantenuto pero' disaccoppiato
    /// dalla scena per evitare un doppio renderer permanente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>BuildChunk</b>: costruisce mesh data per un chunk esplicito.</item>
    ///   <item><b>ResolveVisualTileId</b>: replica la policy visuale legacy.</item>
    ///   <item><b>Hash2D</b>: variante floor deterministica.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainChunkMeshBuilder
    {
        // =============================================================================
        // BuildChunk
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce i dati mesh terrain per un singolo chunk.
        /// </para>
        ///
        /// <para><b>Input snapshot-only</b></para>
        /// <para>
        /// Il metodo interroga solo il layer terrain ArcGraph. Celle mancanti vengono
        /// saltate, cosi' i chunk ai bordi o i context parziali non causano errori
        /// distruttivi. Ogni cella presente diventa un quad.
        /// </para>
        /// </summary>
        public ArcGraphTerrainChunkMeshData BuildChunk(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainTileUvMap uvMap,
            ArcGraphChunkCoord chunk,
            int chunkSizeCells,
            float tileWorld,
            ArcGraphTerrainVisualPolicy visualPolicy)
        {
            if (terrainLayer == null)
                return ArcGraphTerrainChunkMeshData.Empty(chunk, "TerrainLayerMissing");

            if (uvMap == null)
                return ArcGraphTerrainChunkMeshData.Empty(chunk, "UvMapMissing");

            int safeChunkSize = chunkSizeCells > 0 ? chunkSizeCells : 1;
            float safeTileWorld = tileWorld > 0.0001f ? tileWorld : 1f;

            int startX = chunk.X * safeChunkSize;
            int startY = chunk.Y * safeChunkSize;
            int endX = startX + safeChunkSize;
            int endY = startY + safeChunkSize;

            var vertices = new List<Vector3>(safeChunkSize * safeChunkSize * 4);
            var uvs = new List<Vector2>(safeChunkSize * safeChunkSize * 4);
            var triangles = new List<int>(safeChunkSize * safeChunkSize * 6);

            bool usedFallbackUv = false;
            int cellCount = 0;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    var cell = new ArcGraphCellCoord(x, y, chunk.Z);
                    if (!terrainLayer.TryGetCell(cell, out var snapshot))
                        continue;

                    int tileId = ResolveVisualTileId(terrainLayer, snapshot, visualPolicy);
                    bool foundUv = uvMap.TryGetUvQuad(tileId, out var uv0, out var uv1, out var uv2, out var uv3);
                    if (!foundUv)
                        usedFallbackUv = true;

                    AddQuad(
                        vertices,
                        uvs,
                        triangles,
                        x,
                        y,
                        safeTileWorld,
                        uv0,
                        uv1,
                        uv2,
                        uv3);

                    cellCount++;
                }
            }

            string reason = cellCount > 0 ? "ChunkMeshBuilt" : "ChunkHasNoTerrainSnapshots";
            var diagnostics = new ArcGraphTerrainChunkMeshDiagnostics(
                chunk,
                cellCount,
                vertices.Count,
                triangles.Count,
                usedFallbackUv,
                reason);

            return new ArcGraphTerrainChunkMeshData(
                vertices.ToArray(),
                uvs.ToArray(),
                triangles.ToArray(),
                diagnostics);
        }

        private static void AddQuad(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            int cellX,
            int cellY,
            float tileWorld,
            Vector2 uv0,
            Vector2 uv1,
            Vector2 uv2,
            Vector2 uv3)
        {
            int v = vertices.Count;

            float wx = cellX * tileWorld;
            float wy = cellY * tileWorld;

            vertices.Add(new Vector3(wx, wy, 0f));
            vertices.Add(new Vector3(wx + tileWorld, wy, 0f));
            vertices.Add(new Vector3(wx + tileWorld, wy + tileWorld, 0f));
            vertices.Add(new Vector3(wx, wy + tileWorld, 0f));

            uvs.Add(uv0);
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);

            triangles.Add(v + 0);
            triangles.Add(v + 2);
            triangles.Add(v + 1);

            triangles.Add(v + 0);
            triangles.Add(v + 3);
            triangles.Add(v + 2);
        }

        private static int ResolveVisualTileId(
            ArcGraphTerrainLayer terrainLayer,
            ArcGraphTerrainCellSnapshot snapshot,
            ArcGraphTerrainVisualPolicy policy)
        {
            if (snapshot.IsBlocked)
            {
                var north = new ArcGraphCellCoord(
                    snapshot.Cell.X,
                    snapshot.Cell.Y + 1,
                    snapshot.Cell.Z);

                bool northIsFloor = terrainLayer.TryGetCell(north, out var northSnapshot)
                                    && !northSnapshot.IsBlocked;

                return northIsFloor ? policy.WallTopTileId : policy.WallTileId;
            }

            if (!policy.UseLegacyFloorVariants || policy.FloorVariantCount <= 1)
                return snapshot.TileId;

            int hash = Hash2D(snapshot.Cell.X, snapshot.Cell.Y);
            int variant = Mathf.Abs(hash) % policy.FloorVariantCount;
            return policy.FloorBaseTileId + variant;
        }

        private static int Hash2D(int x, int y)
        {
            unchecked
            {
                int h = 17;
                h = (h * 31) + x;
                h = (h * 31) + y;
                h ^= (h << 13);
                h ^= (h >> 17);
                h ^= (h << 5);
                return h;
            }
        }
    }
}
