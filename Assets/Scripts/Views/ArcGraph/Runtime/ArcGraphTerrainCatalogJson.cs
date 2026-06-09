using System;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainCatalogJson
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper per convertire JSON terrain ArcGraph in catalogo runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: JSON al confine, catalogo nel runtime</b></para>
    /// <para>
    /// Il file JSON serve all'authoring. Il renderer non deve lavorare direttamente
    /// su DTO mutabili: riceve un <c>ArcGraphTerrainCatalog</c> normalizzato. Questo
    /// helper non carica file, non legge Resources, non crea materiali e non tocca
    /// scena o simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DefaultResourcePath</b>: path suggerita del catalogo in Resources.</item>
    ///   <item><b>ParseOrDefault</b>: parse con fallback minimo.</item>
    ///   <item><b>TryParse</b>: parse esplicito con esito booleano.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainCatalogJson
    {
        public const string DefaultResourcePath = "ArcGraph/Config/ArcGraphTerrainCatalog";

        // =============================================================================
        // ParseOrDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una stringa JSON in catalogo terrain, usando fallback minimo se
        /// il JSON e' assente o non valido.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainCatalog ParseOrDefault(string json)
        {
            return TryParse(json, out var catalog)
                ? catalog
                : CreateFallbackCatalog();
        }

        // =============================================================================
        // TryParse
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a convertire una stringa JSON in catalogo terrain runtime.
        /// </para>
        ///
        /// <para><b>Parsing puro</b></para>
        /// <para>
        /// Il metodo usa solo la stringa ricevuta. Non apre path, non usa
        /// <c>Resources.Load</c>, non stampa log e non conserva cache globale. Il
        /// chiamante scene-side decidera' quando leggere il file e come mostrare
        /// eventuale diagnostica.
        /// </para>
        /// </summary>
        public static bool TryParse(
            string json,
            out ArcGraphTerrainCatalog catalog)
        {
            catalog = null;

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var dto = JsonUtility.FromJson<ArcGraphTerrainCatalogDto>(json);
                if (dto == null)
                    return false;

                catalog = dto.ToRuntimeCatalog();
                return catalog != null;
            }
            catch
            {
                catalog = null;
                return false;
            }
        }

        private static ArcGraphTerrainCatalog CreateFallbackCatalog()
        {
            return new ArcGraphTerrainCatalog(
                "ArcGraph/Atlas/TerrainAtlas",
                tilePixels: 32,
                atlasWidthPixels: 32,
                atlasHeightPixels: 32,
                new[]
                {
                    new ArcGraphTerrainCatalogEntry(0, "fallback", 0, 0)
                });
        }
    }

    // =============================================================================
    // ArcGraphTerrainCatalogDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del catalogo terrain ArcGraph.
    /// </para>
    ///
    /// <para><b>DTO mutabile solo per JsonUtility</b></para>
    /// <para>
    /// Unity <c>JsonUtility</c> lavora bene con campi pubblici e classi semplici.
    /// Per questo il DTO resta intenzionalmente mutabile. La conversione verso
    /// <c>ArcGraphTerrainCatalog</c> normalizza valori mancanti o non validi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>terrainAtlasResourcePath</b>: path descrittiva dell'atlas.</item>
    ///   <item><b>tilePixels</b>: dimensione tile atlas.</item>
    ///   <item><b>atlasWidthPixels/atlasHeightPixels</b>: dimensioni fallback.</item>
    ///   <item><b>tiles</b>: elenco tile id -> UV.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainCatalogDto
    {
        public string terrainAtlasResourcePath = "ArcGraph/Atlas/TerrainAtlas";
        public int tilePixels = 32;
        public int atlasWidthPixels;
        public int atlasHeightPixels;
        public ArcGraphTerrainCatalogTileDto[] tiles;

        // =============================================================================
        // ToRuntimeCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il DTO JSON in catalogo runtime normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphTerrainCatalog ToRuntimeCatalog()
        {
            ArcGraphTerrainCatalogEntry[] entries = BuildEntries();

            return new ArcGraphTerrainCatalog(
                terrainAtlasResourcePath,
                tilePixels,
                atlasWidthPixels,
                atlasHeightPixels,
                entries);
        }

        private ArcGraphTerrainCatalogEntry[] BuildEntries()
        {
            if (tiles == null || tiles.Length == 0)
            {
                return new[]
                {
                    new ArcGraphTerrainCatalogEntry(0, "fallback", 0, 0)
                };
            }

            var entries = new ArcGraphTerrainCatalogEntry[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                ArcGraphTerrainCatalogTileDto tile = tiles[i];
                entries[i] = tile != null
                    ? tile.ToRuntimeEntry()
                    : new ArcGraphTerrainCatalogEntry(0, "fallback", 0, 0);
            }

            return entries;
        }
    }

    // =============================================================================
    // ArcGraphTerrainCatalogTileDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile di una tile terrain dentro il catalogo ArcGraph.
    /// </para>
    ///
    /// <para><b>Entry minima</b></para>
    /// <para>
    /// Per il primo catalogo servono solo id e coordinate atlas. Campi come costo
    /// movimento, fertilita' o layer ecosistema non appartengono al catalogo sprite:
    /// verranno gestiti da moduli simulativi o map data separati.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainCatalogTileDto
    {
        public int id;
        public string name;
        public int uvX;
        public int uvY;

        // =============================================================================
        // ToRuntimeEntry
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la tile JSON in entry runtime normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainCatalogEntry ToRuntimeEntry()
        {
            return new ArcGraphTerrainCatalogEntry(id, name, uvX, uvY);
        }
    }
}
