using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // ObjectPropertyKV
    // =============================================================================
    /// <summary>
    /// <para>
    /// Coppia numerica data-driven usata per proprieta' oggetto estendibili.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bordo authoring flessibile</b></para>
    /// <para>
    /// Le proprieta' testuali restano accettabili nel catalogo JSON e nei bordi di
    /// authoring/debug. Le strutture runtime calde, come l'inventario NPC, devono
    /// invece evitare di duplicare stringhe quando possono riferirsi a objectId,
    /// enum o component store.
    /// </para>
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

        // Peso/ingombro inventario fisico.
        public int WeightUnits;
        public int BulkUnits;

        // Regole materiali base.
        public bool Stackable;
        public bool HasDurability;

        // Collocazioni ammesse. Sono bool serializzabili invece di stringhe runtime:
        // il JSON resta leggibile, ma i sistemi possono validare senza parsing.
        public bool CanPlaceInHand;
        public bool CanPlaceInContainer;
        public bool CanEquipHead;
        public bool CanEquipHands;
        public bool CanEquipUndergarment;
        public bool CanEquipOvergarment;
        public bool CanEquipArmor;
        public bool CanEquipFeet;
        public bool CanEquipSidearm;
        public bool CanEquipBack;

        // Classificazione contenitore. Il tipo resta stringa per il bordo
        // authoring JSON; i sistemi runtime la convertiranno in enum/contratti
        // quando arrivera' lo step dei macro-slot configurabili.
        public bool IsContainer;
        public string ContainerKind;

        // Capacita' eventuale degli oggetti contenitore, per esempio zaini.
        public int ContainerBulkCapacityUnits;
        public int ContainerWeightCapacityUnits;

        // =============================================================================
        // TryGetPropertyValue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca una proprieta' numerica data-driven usando la chiave testuale
        /// dichiarata in <c>object_defs.json</c>.
        /// </para>
        ///
        /// <para><b>Principio architetturale: catalogo oggetti come fonte del dato di consumo</b></para>
        /// <para>
        /// Alcuni valori, come <c>NutritionValue</c>, appartengono al tipo oggetto o
        /// risorsa e non al comando che li consuma. Questo metodo permette ai sistemi
        /// runtime di leggere quel dato senza duplicare string parsing o introdurre
        /// costanti hardcoded nei comandi gameplay.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>key</b>: chiave testuale del JSON, confrontata senza distinguere maiuscole/minuscole.</item>
        ///   <item><b>value</b>: valore numerico associato alla proprieta' trovata.</item>
        ///   <item><b>return</b>: false se la lista proprieta' manca, la chiave e' vuota o non esiste.</item>
        /// </list>
        /// </summary>
        public bool TryGetPropertyValue(string key, out float value)
        {
            value = 0f;

            if (string.IsNullOrWhiteSpace(key) || Properties == null)
                return false;

            for (int i = 0; i < Properties.Count; i++)
            {
                ObjectPropertyKV property = Properties[i];
                if (!string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                value = property.Value;
                return true;
            }

            return false;
        }

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

    // =============================================================================
    // ObjectDefDatabase
    // =============================================================================
    /// <summary>
    /// <para>
    /// Root serializzabile del catalogo oggetti.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo data-driven unico</b></para>
    /// <para>
    /// Il file JSON espone oggetti come dati autorevoli. I consumer specializzati
    /// leggono solo le parti che possiedono: il core legge fisica, componenti e
    /// collocazioni; ArcGraph legge Visual; i sistemi cibo leggono nutrizione.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ObjectDefDatabase
    {
        public List<ObjectDef> Objects;
    }
}
