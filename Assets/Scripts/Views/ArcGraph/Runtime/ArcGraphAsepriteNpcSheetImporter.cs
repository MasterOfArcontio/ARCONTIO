using System;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphAsepriteNpcSheetImportDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato serializzabile dell'ultimo import Aseprite NPC eseguito dal
    /// componente scena.
    /// </para>
    ///
    /// <para><b>Principio architetturale: diagnostica tool-side non invasiva</b></para>
    /// <para>
    /// L'importer produce asset grafici per ArcGraph, ma non deve diventare un
    /// sistema runtime della simulazione. Questa struttura conserva quindi solo
    /// informazioni di esito leggibili dall'Inspector: non legge il World, non
    /// influenza il tick e non crea alcuna autorita' simulativa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Success</b>: indica se l'operazione ha completato tutti i passaggi richiesti.</item>
    ///   <item><b>Reason</b>: motivo sintetico dell'esito, utile per gate manuali.</item>
    ///   <item><b>SourceFrameCount</b>: numero frame rilevati nel file Aseprite.</item>
    ///   <item><b>CreatedPngCount</b>: numero PNG scritti o riscritti.</item>
    ///   <item><b>CreatedSliceCount</b>: numero sprite rect creati sugli sheet.</item>
    ///   <item><b>FirstGeneratedAssetPath</b>: primo asset path generato, utile per controllo rapido.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct ArcGraphAsepriteNpcSheetImportDiagnostics
    {
        public bool Success;
        public string Reason;
        public int SourceFrameCount;
        public int CreatedPngCount;
        public int CreatedSliceCount;
        public string FirstGeneratedAssetPath;

        // =============================================================================
        // ArcGraphAsepriteNpcSheetImportDiagnostics
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una diagnostica completa per l'Inspector.
        /// </para>
        /// </summary>
        public ArcGraphAsepriteNpcSheetImportDiagnostics(
            bool success,
            string reason,
            int sourceFrameCount,
            int createdPngCount,
            int createdSliceCount,
            string firstGeneratedAssetPath)
        {
            Success = success;
            Reason = reason ?? string.Empty;
            SourceFrameCount = sourceFrameCount;
            CreatedPngCount = createdPngCount;
            CreatedSliceCount = createdSliceCount;
            FirstGeneratedAssetPath = firstGeneratedAssetPath ?? string.Empty;
        }
    }

    // =============================================================================
    // ArcGraphAsepriteNpcSheetImporter
    // =============================================================================
    /// <summary>
    /// <para>
    /// Componente scena attivabile manualmente per trasformare un file Aseprite NPC
    /// stratificato in quattro spritesheet PNG ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tool esplicito al bordo asset</b></para>
    /// <para>
    /// Il componente vive su un GameObject solo come punto di configurazione e
    /// comando manuale. L'import vero usa API Editor e viene compilato solo dentro
    /// Unity Editor. In build runtime questo componente non importa file, non
    /// scrive asset, non usa AssetDatabase e non parte automaticamente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>asepriteFileName</b>: path del file .aseprite o .ase da leggere.</item>
    ///   <item><b>outputSheetFileName</b>: nome PNG creato dentro ogni cartella parte, normalmente idle.png.</item>
    ///   <item><b>*OutputFolder</b>: cartelle AssetDatabase dove creare legs/body/arms/head.</item>
    ///   <item><b>sliceWidthPixels/sliceHeightPixels</b>: dimensione in pixel dello slice generato e della finestra sorgente.</item>
    ///   <item><b>pixelsPerUnit</b>: PPU assegnato al TextureImporter dei PNG generati.</item>
    ///   <item><b>overwriteExistingPng</b>: gate esplicito per evitare sovrascritture accidentali.</item>
    ///   <item><b>ImportFromContextMenu</b>: comando manuale esposto dal menu contestuale del componente.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphAsepriteNpcSheetImporter : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private string asepriteFileName;

        [Header("Output")]
        [SerializeField] private string outputSheetFileName = "idle.png";
        [SerializeField] private string legsOutputFolder = "Assets/Resources/ArcGraph/NPC/human_default/legs";
        [SerializeField] private string bodyOutputFolder = "Assets/Resources/ArcGraph/NPC/human_default/body";
        [SerializeField] private string armsOutputFolder = "Assets/Resources/ArcGraph/NPC/human_default/arms";
        [SerializeField] private string headOutputFolder = "Assets/Resources/ArcGraph/NPC/human_default/head";

        [Header("Slicing")]
        [SerializeField] private int sliceWidthPixels;
        [SerializeField] private int sliceHeightPixels;

        [Header("Import Settings")]
        [SerializeField] private int pixelsPerUnit = 32;
        [SerializeField] private Vector2 spritePivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private bool overwriteExistingPng = true;
        [SerializeField] private bool logDiagnostics = true;

        [Header("Last Result")]
        [SerializeField] private ArcGraphAsepriteNpcSheetImportDiagnostics lastDiagnostics;

        public string AsepriteFileName => asepriteFileName;
        public string OutputSheetFileName => outputSheetFileName;
        public string LegsOutputFolder => legsOutputFolder;
        public string BodyOutputFolder => bodyOutputFolder;
        public string ArmsOutputFolder => armsOutputFolder;
        public string HeadOutputFolder => headOutputFolder;
        public int SliceWidthPixels => sliceWidthPixels;
        public int SliceHeightPixels => sliceHeightPixels;
        public int PixelsPerUnit => pixelsPerUnit > 0 ? pixelsPerUnit : 32;
        public Vector2 SpritePivot => spritePivot;
        public bool OverwriteExistingPng => overwriteExistingPng;
        public bool LogDiagnostics => logDiagnostics;
        public ArcGraphAsepriteNpcSheetImportDiagnostics LastDiagnostics => lastDiagnostics;

        // =============================================================================
        // ImportFromContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia manualmente l'importer Aseprite dal menu contestuale del componente.
        /// </para>
        ///
        /// <para><b>Attivazione esplicita</b></para>
        /// <para>
        /// Il metodo non viene chiamato da <c>Start</c>, <c>Update</c> o altri hook
        /// automatici. L'operatore deve selezionare il GameObject, configurare i
        /// path e lanciare il comando. Questo evita side effect asset-side durante
        /// il normale Play Mode o durante il bootstrap scena ArcGraph.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Import Aseprite NPC Sheets")]
        public void ImportFromContextMenu()
        {
#if UNITY_EDITOR
            lastDiagnostics = ArcGraphAsepriteNpcSheetImporterEditorBridge.Import(this);
#else
            lastDiagnostics = new ArcGraphAsepriteNpcSheetImportDiagnostics(
                false,
                "EditorOnlyImporter",
                0,
                0,
                0,
                string.Empty);
#endif

            if (logDiagnostics)
            {
                Debug.Log(
                    "[ArcGraphAsepriteNpcSheetImporter] " + lastDiagnostics.Reason +
                    " success=" + lastDiagnostics.Success +
                    ", frames=" + lastDiagnostics.SourceFrameCount +
                    ", png=" + lastDiagnostics.CreatedPngCount +
                    ", slices=" + lastDiagnostics.CreatedSliceCount +
                    ", first='" + lastDiagnostics.FirstGeneratedAssetPath + "'",
                    this);
            }
        }

        private void OnValidate()
        {
            // Manteniamo i valori numerici dentro un range sensato gia' in
            // Inspector. Il servizio Editor ripete comunque le validazioni prima
            // di scrivere file, perche' i dati serializzati potrebbero arrivare da
            // merge o edit manuali.
            if (pixelsPerUnit <= 0)
                pixelsPerUnit = 32;

            if (sliceWidthPixels < 0)
                sliceWidthPixels = 0;

            if (sliceHeightPixels < 0)
                sliceHeightPixels = 0;

            spritePivot.x = Mathf.Clamp01(spritePivot.x);
            spritePivot.y = Mathf.Clamp01(spritePivot.y);
        }
    }

#if UNITY_EDITOR
    // =============================================================================
    // ArcGraphAsepriteNpcSheetImporterEditorBridge
    // =============================================================================
    /// <summary>
    /// <para>
    /// Ponte compile-time tra il componente scena e il servizio Editor reale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: nessun UnityEditor nel runtime build</b></para>
    /// <para>
    /// Questa classe esiste nello stesso file del componente solo come frontiera
    /// condizionale. Il nome del servizio Editor viene risolto esclusivamente
    /// quando <c>UNITY_EDITOR</c> e' definito, impedendo a build runtime di
    /// dipendere da namespace o assembly editor-only.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Import</b>: delega il lavoro al servizio in Assets/Scripts/Editor.</item>
    /// </list>
    /// </summary>
    internal static class ArcGraphAsepriteNpcSheetImporterEditorBridge
    {
        // =============================================================================
        // Import
        // =============================================================================
        /// <summary>
        /// <para>
        /// Delega l'import al servizio Editor.
        /// </para>
        /// </summary>
        public static ArcGraphAsepriteNpcSheetImportDiagnostics Import(
            ArcGraphAsepriteNpcSheetImporter importer)
        {
            Type serviceType = Type.GetType(
                "Arcontio.Editor.ArcGraphAsepriteNpcSheetImporterEditorService, Assembly-CSharp-Editor");

            if (serviceType == null)
            {
                return new ArcGraphAsepriteNpcSheetImportDiagnostics(
                    false,
                    "EditorServiceMissing",
                    0,
                    0,
                    0,
                    string.Empty);
            }

            var importMethod = serviceType.GetMethod(
                "Import",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (importMethod == null)
            {
                return new ArcGraphAsepriteNpcSheetImportDiagnostics(
                    false,
                    "EditorImportMethodMissing",
                    0,
                    0,
                    0,
                    string.Empty);
            }

            object result = importMethod.Invoke(null, new object[] { importer });
            if (result is ArcGraphAsepriteNpcSheetImportDiagnostics diagnostics)
                return diagnostics;

            return new ArcGraphAsepriteNpcSheetImportDiagnostics(
                false,
                "EditorImportResultInvalid",
                0,
                0,
                0,
                string.Empty);
        }
    }
#endif
}
