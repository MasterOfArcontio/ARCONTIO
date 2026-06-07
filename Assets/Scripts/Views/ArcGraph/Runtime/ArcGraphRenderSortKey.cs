using System;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphRenderSortKey
    // =============================================================================
    /// <summary>
    /// <para>
    /// Chiave deterministica di ordinamento per item renderizzabili ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: sorting leggibile prima del renderer Unity</b></para>
    /// <para>
    /// Il futuro wrapper Unity avra' bisogno di tradurre la posizione logica in
    /// sorting layer, sorting order o draw order. Questa struttura prepara quel
    /// dato senza creare sprite e senza consultare camera o scena. L'ordinamento e'
    /// stabile: prima livello Z, poi Y, poi X, poi layer visuale, poi kind e id.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Z</b>: livello discreto di altitudine/profondita'.</item>
    ///   <item><b>Y/X</b>: coordinate piano usate per ordine deterministico.</item>
    ///   <item><b>VisualLayerOrder</b>: priorita' relativa tra oggetti e actor.</item>
    ///   <item><b>Kind</b>: categoria render item.</item>
    ///   <item><b>EntityId</b>: id finale per stabilizzare pareggi.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphRenderSortKey : IComparable<ArcGraphRenderSortKey>
    {
        public readonly int Z;
        public readonly int Y;
        public readonly int X;
        public readonly int VisualLayerOrder;
        public readonly ArcGraphRenderItemKind Kind;
        public readonly int EntityId;

        // =============================================================================
        // ArcGraphRenderSortKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una chiave sorting completa.
        /// </para>
        ///
        /// <para><b>Value-only</b></para>
        /// <para>
        /// Tutti i campi sono primitivi o enum. La chiave non trattiene riferimenti
        /// a sprite, mesh, camera o stato mondo.
        /// </para>
        /// </summary>
        public ArcGraphRenderSortKey(
            int z,
            int y,
            int x,
            int visualLayerOrder,
            ArcGraphRenderItemKind kind,
            int entityId)
        {
            Z = z;
            Y = y;
            X = x;
            VisualLayerOrder = visualLayerOrder;
            Kind = kind;
            EntityId = entityId;
        }

        // =============================================================================
        // FromCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una chiave sorting a partire da una cella discreta.
        /// </para>
        ///
        /// <para><b>Sorting da coordinate logiche</b></para>
        /// <para>
        /// Il metodo usa solo coordinate ArcGraph. Non interpreta world position,
        /// camera, pixel perfect o sorting layer Unity.
        /// </para>
        /// </summary>
        public static ArcGraphRenderSortKey FromCell(
            ArcGraphCellCoord cell,
            int visualLayerOrder,
            ArcGraphRenderItemKind kind,
            int entityId)
        {
            return new ArcGraphRenderSortKey(
                cell.Z,
                cell.Y,
                cell.X,
                visualLayerOrder,
                kind,
                entityId);
        }

        public int CompareTo(ArcGraphRenderSortKey other)
        {
            int z = Z.CompareTo(other.Z);
            if (z != 0)
                return z;

            int y = Y.CompareTo(other.Y);
            if (y != 0)
                return y;

            int x = X.CompareTo(other.X);
            if (x != 0)
                return x;

            int layer = VisualLayerOrder.CompareTo(other.VisualLayerOrder);
            if (layer != 0)
                return layer;

            int kind = Kind.CompareTo(other.Kind);
            if (kind != 0)
                return kind;

            return EntityId.CompareTo(other.EntityId);
        }
    }
}
