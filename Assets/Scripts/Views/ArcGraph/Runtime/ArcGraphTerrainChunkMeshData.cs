using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainChunkMeshData
    // =============================================================================
    /// <summary>
    /// <para>
    /// Dati mesh prodotti dal builder terrain ArcGraph per un singolo chunk.
    /// </para>
    ///
    /// <para><b>Principio architetturale: mesh data, non GameObject</b></para>
    /// <para>
    /// Questa classe contiene array pronti per una mesh Unity, ma non crea
    /// <c>GameObject</c>, non aggiunge <c>MeshRenderer</c> e non possiede materiali.
    /// In questo modo <c>v0.32</c> puo' validare la costruzione terrain senza
    /// agganciare automaticamente un secondo renderer alla scena.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Vertices</b>: vertici del chunk.</item>
    ///   <item><b>Uvs</b>: coordinate UV dei vertici.</item>
    ///   <item><b>Triangles</b>: indici triangolo.</item>
    ///   <item><b>Diagnostics</b>: esito della costruzione.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainChunkMeshData
    {
        public Vector3[] Vertices { get; }
        public Vector2[] Uvs { get; }
        public int[] Triangles { get; }
        public ArcGraphTerrainChunkMeshDiagnostics Diagnostics { get; }

        public bool IsEmpty => Diagnostics.IsEmpty;

        // =============================================================================
        // ArcGraphTerrainChunkMeshData
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un contenitore mesh data terrain.
        /// </para>
        ///
        /// <para><b>Array normalizzati</b></para>
        /// <para>
        /// Gli array null vengono convertiti in array vuoti. Il chiamante puo' quindi
        /// leggere sempre le proprieta' senza null-check e decidere se applicarle a
        /// una mesh esterna.
        /// </para>
        /// </summary>
        public ArcGraphTerrainChunkMeshData(
            Vector3[] vertices,
            Vector2[] uvs,
            int[] triangles,
            ArcGraphTerrainChunkMeshDiagnostics diagnostics)
        {
            Vertices = vertices ?? new Vector3[0];
            Uvs = uvs ?? new Vector2[0];
            Triangles = triangles ?? new int[0];
            Diagnostics = diagnostics;
        }

        // =============================================================================
        // Empty
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un risultato vuoto con ragione diagnostica.
        /// </para>
        ///
        /// <para><b>Fallimento non distruttivo</b></para>
        /// <para>
        /// Un chunk vuoto non indica un errore simulativo. Puo' significare che il
        /// layer non ha snapshot, che il chunk e' fuori bounds o che manca una
        /// dipendenza visuale.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainChunkMeshData Empty(
            ArcGraphChunkCoord chunk,
            string reason)
        {
            return new ArcGraphTerrainChunkMeshData(
                new Vector3[0],
                new Vector2[0],
                new int[0],
                new ArcGraphTerrainChunkMeshDiagnostics(
                    chunk,
                    0,
                    0,
                    0,
                    false,
                    0,
                    -1,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    reason));
        }
    }
}
