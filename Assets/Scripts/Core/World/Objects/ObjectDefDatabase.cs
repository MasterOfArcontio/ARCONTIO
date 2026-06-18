using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    /// <summary>
    /// ObjectPropertyKV:
    /// Coppia Key/Value serializzabile per JsonUtility.
    /// Deve matchare il JSON: { "Key": "...", "Value": 1.0 }
    /// </summary>
    [Serializable]
    public struct ObjectPropertyKV
    {
        public string Key;
        public float Value;
    }

    // =============================================================================
    // ObjectVisualDef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sezione visuale opzionale di una definizione oggetto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo unico, lettura separata</b></para>
    /// <para>
    /// ARCONTIO conserva le informazioni principali dell'oggetto nello stesso
    /// catalogo <c>object_defs.json</c>, cosi' l'authoring resta semplice. Questo
    /// non significa pero' che il core simulativo debba dipendere dalla grafica:
    /// i sistemi di simulazione ignorano questa sezione, mentre ArcGraph puo'
    /// leggerla per sapere quale asset mostrare e con quali regole visuali.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>SpritePath</b>: path Resources preferito da ArcGraph per il PNG dell'oggetto.</item>
    ///   <item><b>VisualKind</b>: categoria visuale, per esempio <c>wall</c>, utile ai resolver futuri.</item>
    ///   <item><b>ResolverKey</b>: chiave opzionale per scegliere una famiglia di varianti visuali.</item>
    ///   <item><b>WidthPixels/HeightPixels</b>: dimensione nominale dello sprite, utile per authoring e probe.</item>
    ///   <item><b>BaseWidthPixels/BaseHeightPixels</b>: dimensione della base logica appoggiata alla cella.</item>
    ///   <item><b>BaseMiniTileMask</b>: copertura 2x2 della base, usata dai futuri raccordi pavimento a 16 pixel.</item>
    ///   <item><b>Pivot</b>: convenzione visuale del punto di ancoraggio, per ora testuale.</item>
    ///   <item><b>OffsetX/OffsetY</b>: correzione visuale in pixel, senza effetto sulla cella logica.</item>
    ///   <item><b>FadeWhenActorBehind</b>: abilita futura trasparenza quando un NPC passa dietro.</item>
    ///   <item><b>UseShadow</b>: abilita futura ombra visuale locale dell'oggetto.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ObjectVisualDef
    {
        public string SpritePath;
        public string VisualKind;
        public string ResolverKey;
        public int WidthPixels;
        public int HeightPixels;
        public int BaseWidthPixels;
        public int BaseHeightPixels;
        public string BaseMiniTileMask;
        public string Pivot;
        public int OffsetX;
        public int OffsetY;
        public bool FadeWhenActorBehind;
        public bool UseShadow;
    }

    // =============================================================================
    // ObjectDef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione data-driven di un oggetto del mondo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: dato unico, consumer specializzati</b></para>
    /// <para>
    /// Questa struttura nasce come sorgente dati unica per un tipo oggetto. I campi
    /// logici vengono usati dal core, dai job, dalla percezione e dalle cache di
    /// movimento/visione. La sezione <c>Visual</c> viene invece consumata dalla
    /// view. In questo modo evitiamo cataloghi paralleli difficili da mantenere,
    /// ma non costringiamo la simulazione a conoscere dettagli come pixel, pivot o
    /// ombre.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Id/DisplayName</b>: identita' data-driven leggibile.</item>
    ///   <item><b>SpriteKey</b>: path legacy ancora usato da MapGrid.</item>
    ///   <item><b>FootprintWidth/FootprintHeight</b>: ingombro logico XY in celle.</item>
    ///   <item><b>Flags logici</b>: occlusione, interazione, porte, movimento e visione.</item>
    ///   <item><b>Visual</b>: dati grafici opzionali per ArcGraph.</item>
    ///   <item><b>Properties</b>: proprieta' generiche per letti, workbench, stock e altri oggetti.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ObjectDef
    {
        public string Id;
        public string DisplayName;

        public string SpriteKey;
        public int FootprintWidth;
        public int FootprintHeight;

        // Classificazione logica
        public bool IsOccluder;       // se true: entra nella occlusion map
        public bool IsInteractable;
        public bool IsDoor;           // se true: questo oggetto e' una porta
        public bool IsLockable;       // se true: supporta il lock (valido solo se IsDoor=true)
        public string KeyId;          // DefId dell'oggetto chiave richiesto (valido solo se IsLockable=true)

        // Occlusion params (validi se IsOccluder=true)
        public bool BlocksVision;
        public bool BlocksMovement;
        public float VisionCost;

        // Distruzione (validi se IsOccluder=true, opzionali)
        public int MaxHp;
        public float Hardness;

        public ObjectVisualDef Visual;

        // Proprieta' generiche (letto, workbench, food, ecc.)
        public List<ObjectPropertyKV> Properties;

        // =============================================================================
        // ResolveArcGraphSpritePath
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il path sprite esplicitamente dichiarato per ArcGraph.
        /// </para>
        ///
        /// <para><b>Distacco dal fallback MapGrid</b></para>
        /// <para>
        /// ArcGraph non usa piu' <c>SpriteKey</c> come fallback implicito, perche'
        /// quel campo appartiene al percorso legacy MapGrid. Se la sezione
        /// <c>Visual.SpritePath</c> manca, il metodo ritorna stringa vuota e il
        /// renderer puo' segnalare correttamente una definizione visuale incompleta.
        /// </para>
        /// </summary>
        public string ResolveArcGraphSpritePath()
        {
            if (Visual != null && !string.IsNullOrWhiteSpace(Visual.SpritePath))
                return Visual.SpritePath;

            return string.Empty;
        }
    }

    /// <summary>
    /// Root del JSON: { "Objects": [ ... ] }
    /// </summary>
    [Serializable]
    public sealed class ObjectDefDatabase
    {
        public List<ObjectDef> Objects;
    }
}
