using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainCatalogEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione runtime di una singola tile terreno ArcGraph dentro un atlas.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dato visuale dichiarato, non hardcoded</b></para>
    /// <para>
    /// La conformazione della mappa continua a dire quale <c>TileId</c> possiede
    /// ogni cella. Il catalogo visuale dice invece dove quel <c>TileId</c> si trova
    /// nell'atlas. Separare questi due ruoli permette di cambiare sprite terreno
    /// senza riscrivere la mappa o la simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Id</b>: tile id usato dalle celle terrain.</item>
    ///   <item><b>Name</b>: nome leggibile per diagnostica e authoring.</item>
    ///   <item><b>UvX/UvY</b>: colonna e riga della tile nell'atlas.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainCatalogEntry
    {
        public readonly int Id;
        public readonly string Name;
        public readonly int UvX;
        public readonly int UvY;

        // =============================================================================
        // ArcGraphTerrainCatalogEntry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una entry terrain catalog normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainCatalogEntry(
            int id,
            string name,
            int uvX,
            int uvY)
        {
            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? "terrain_" + id : name;
            UvX = uvX < 0 ? 0 : uvX;
            UvY = uvY < 0 ? 0 : uvY;
        }
    }

    // =============================================================================
    // ArcGraphTerrainCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo runtime delle tile terreno ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo visuale passivo</b></para>
    /// <para>
    /// Il catalogo non carica texture, non legge file e non crea materiali. Conserva
    /// solo informazioni gia' normalizzate: path descrittiva dell'atlas, dimensione
    /// tile, dimensioni atlas opzionali e mapping tile id -> UV. Il renderer puo'
    /// usarlo per costruire una <c>ArcGraphTerrainTileUvMap</c> senza conoscere il
    /// formato JSON originale.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>AtlasResourcePath</b>: path descrittiva dell'atlas usato dal catalogo.</item>
    ///   <item><b>TilePixels</b>: dimensione in pixel di una tile atlas.</item>
    ///   <item><b>AtlasWidthPixels/AtlasHeightPixels</b>: dimensioni fallback dell'atlas.</item>
    ///   <item><b>UvInsetPixels</b>: rientro UV anti-bleeding applicato dentro ogni slice.</item>
    ///   <item><b>Entries</b>: entry normalizzate ordinate come nel JSON.</item>
    ///   <item><b>BuildUvMap</b>: crea la UV map consumata dal mesh builder terrain.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainCatalog
    {
        private readonly ArcGraphTerrainCatalogEntry[] _entries;

        public string AtlasResourcePath { get; }
        public int TilePixels { get; }
        public int AtlasWidthPixels { get; }
        public int AtlasHeightPixels { get; }
        public float UvInsetPixels { get; }
        public int EntryCount => _entries.Length;
        public IReadOnlyList<ArcGraphTerrainCatalogEntry> Entries => _entries;

        // =============================================================================
        // ArcGraphTerrainCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un catalogo terrain runtime normalizzando dimensioni e entry.
        /// </para>
        /// </summary>
        public ArcGraphTerrainCatalog(
            string atlasResourcePath,
            int tilePixels,
            int atlasWidthPixels,
            int atlasHeightPixels,
            float uvInsetPixels,
            ArcGraphTerrainCatalogEntry[] entries)
        {
            AtlasResourcePath = atlasResourcePath ?? string.Empty;
            TilePixels = tilePixels > 0 ? tilePixels : 32;
            UvInsetPixels = uvInsetPixels < 0f ? 0f : uvInsetPixels;
            _entries = entries != null ? CopyEntries(entries) : new ArcGraphTerrainCatalogEntry[0];
            AtlasWidthPixels = ResolveAtlasPixels(atlasWidthPixels, useX: true);
            AtlasHeightPixels = ResolveAtlasPixels(atlasHeightPixels, useX: false);
        }

        // =============================================================================
        // BuildUvMap
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una UV map terrain usando la texture materiale se disponibile.
        /// </para>
        ///
        /// <para><b>Fallback non distruttivo</b></para>
        /// <para>
        /// Se il materiale possiede una texture, le dimensioni reali della texture
        /// vincono sulle dimensioni dichiarate nel JSON. Se la texture manca, il
        /// catalogo usa le dimensioni dichiarate o inferite dalle entry. Questo
        /// mantiene il renderer avviabile anche durante asset authoring incompleto.
        /// </para>
        /// </summary>
        public ArcGraphTerrainTileUvMap BuildUvMap(Texture texture)
        {
            int atlasWidth = texture != null ? texture.width : AtlasWidthPixels;
            int atlasHeight = texture != null ? texture.height : AtlasHeightPixels;

            var uvMap = new ArcGraphTerrainTileUvMap(
                atlasWidth,
                atlasHeight,
                TilePixels,
                UvInsetPixels);
            for (int i = 0; i < _entries.Length; i++)
            {
                ArcGraphTerrainCatalogEntry entry = _entries[i];
                uvMap.Register(entry.Id, entry.UvX, entry.UvY);
            }

            return uvMap;
        }

        private static ArcGraphTerrainCatalogEntry[] CopyEntries(
            ArcGraphTerrainCatalogEntry[] entries)
        {
            var copy = new ArcGraphTerrainCatalogEntry[entries.Length];
            entries.CopyTo(copy, 0);
            return copy;
        }

        private int ResolveAtlasPixels(
            int declaredPixels,
            bool useX)
        {
            if (declaredPixels > 0)
                return declaredPixels;

            int maxCell = 0;
            for (int i = 0; i < _entries.Length; i++)
            {
                int coordinate = useX ? _entries[i].UvX : _entries[i].UvY;
                if (coordinate > maxCell)
                    maxCell = coordinate;
            }

            return Mathf.Max(1, (maxCell + 1) * TilePixels);
        }
    }
}
