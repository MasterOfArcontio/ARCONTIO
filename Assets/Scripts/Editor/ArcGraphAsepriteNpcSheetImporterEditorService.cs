using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arcontio.View.ArcGraph;
using Unity.Collections;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Arcontio.Editor
{
    // =============================================================================
    // ArcGraphAsepriteNpcSheetImporterEditor
    // =============================================================================
    /// <summary>
    /// <para>
    /// Inspector dedicato al componente importer Aseprite NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: attivazione manuale esplicita</b></para>
    /// <para>
    /// L'importer e' una feature attivabile mettendo il componente su un GameObject,
    /// ma il lavoro asset-side deve partire solo da comando umano. Questo editor
    /// mantiene tutti i campi serializzati standard e aggiunge un pulsante operativo
    /// visibile, evitando hook automatici nel ciclo scena.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>OnInspectorGUI</b>: disegna l'Inspector default e il comando di import.</item>
    /// </list>
    /// </summary>
    [CustomEditor(typeof(ArcGraphAsepriteNpcSheetImporter))]
    public sealed class ArcGraphAsepriteNpcSheetImporterEditor : UnityEditor.Editor
    {
        // =============================================================================
        // OnInspectorGUI
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna l'Inspector del componente e avvia l'import quando l'operatore
        /// preme il pulsante dedicato.
        /// </para>
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var importer = (ArcGraphAsepriteNpcSheetImporter)target;
            using (new EditorGUI.DisabledScope(importer == null))
            {
                // Il pulsante resta in fondo all'Inspector per non nascondere i
                // campi di configurazione. L'azione aggiorna la diagnostica
                // serializzata tramite il metodo pubblico del componente.
                if (GUILayout.Button("Import Aseprite NPC Sheets"))
                {
                    importer.ImportFromContextMenu();
                    EditorUtility.SetDirty(importer);
                }
            }
        }
    }

    // =============================================================================
    // ArcGraphAsepriteNpcSheetImporterEditorService
    // =============================================================================
    /// <summary>
    /// <para>
    /// Servizio Editor che converte un file Aseprite NPC a 12 layer sorgente in
    /// quattro PNG spritesheet sliced compatibili con il resolver ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: import asset confinato all'Editor</b></para>
    /// <para>
    /// Questo servizio usa <c>UnityEditor</c>, <c>AssetDatabase</c> e il parser del
    /// package Aseprite. Per questo vive sotto <c>Assets/Scripts/Editor</c> e non
    /// partecipa al runtime simulativo. Il componente scena fornisce solo input
    /// serializzati e comando manuale; qui avvengono parsing, scrittura file e
    /// configurazione del <c>TextureImporter</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Import</b>: entry point chiamato dal componente GameObject.</item>
    ///   <item><b>BuildLayerIndex</b>: mappa layer Aseprite sorgente per nome canonico.</item>
    ///   <item><b>BuildPartSheet</b>: genera uno sheet per legs/body/arms/head, includendo east derivato da west.</item>
    ///   <item><b>ResolveCell</b>: risolve celle normali e linked cell per layer/frame.</item>
    ///   <item><b>ConfigureSpriteSheet</b>: imposta import settings e sprite rect Unity.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphAsepriteNpcSheetImporterEditorService
    {
        private const string ReasonSuccess = "AsepriteNpcSheetsImported";
        private const string ReasonImporterMissing = "ImporterMissing";
        private const string ReasonSourceMissing = "SourceMissing";
        private const string ReasonOutputFolderInvalid = "OutputFolderInvalid";
        private const string ReasonLayerMissing = "LayerMissing";
        private const string ReasonFrameMissing = "FrameMissing";
        private const string ReasonSliceWindowInvalid = "SliceWindowInvalid";
        private const string ReasonOverwriteBlocked = "OverwriteBlocked";
        private const string ReasonImportFailed = "ImportFailed";

        private static readonly string[] DirectionKeys =
        {
            "south",
            "east",
            "north",
            "west"
        };

        private static readonly string[] SourceDirectionKeys =
        {
            "south",
            "north",
            "west"
        };

        private static readonly string[] PartKeys =
        {
            "legs",
            "body",
            "arms",
            "head"
        };

        // =============================================================================
        // Import
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue l'import completo del file Aseprite configurato dal componente.
        /// </para>
        ///
        /// <para><b>Pipeline controllata</b></para>
        /// <para>
        /// Il metodo valida prima sorgente e destinazioni, poi legge il file una
        /// sola volta, crea gli sheet in memoria, scrive i PNG e solo alla fine
        /// aggiorna AssetDatabase. Se manca un layer o un frame, l'operazione si
        /// interrompe senza inventare sprite silenziosi.
        /// </para>
        /// </summary>
        public static ArcGraphAsepriteNpcSheetImportDiagnostics Import(
            ArcGraphAsepriteNpcSheetImporter importer)
        {
            if (importer == null)
                return Fail(ReasonImporterMissing);

            if (!TryResolveExistingSourcePath(importer.AsepriteFileName, out string sourcePath))
                return Fail(ReasonSourceMissing);

            if (!TryBuildOutputSpecs(importer, out PartOutputSpec[] outputSpecs, out string outputError))
                return Fail(outputError);

            try
            {
                using AsepriteFile asepriteFile = AsepriteReader.ReadFile(sourcePath);
                if (asepriteFile == null || asepriteFile.noOfFrames <= 0)
                    return Fail(ReasonImportFailed);

                Dictionary<string, int> layerIndexByName = BuildLayerIndex(asepriteFile);
                string missingLayer = FindFirstMissingLayer(layerIndexByName);
                if (!string.IsNullOrEmpty(missingLayer))
                    return Fail(ReasonLayerMissing + ":" + missingLayer, asepriteFile.noOfFrames);

                int createdPngCount = 0;
                int createdSliceCount = 0;
                string firstGeneratedAssetPath = string.Empty;

                for (int i = 0; i < outputSpecs.Length; i++)
                {
                    PartOutputSpec spec = outputSpecs[i];
                    if (File.Exists(spec.FullPngPath) && !importer.OverwriteExistingPng)
                        return Fail(ReasonOverwriteBlocked + ":" + spec.AssetPngPath, asepriteFile.noOfFrames);

                    SheetBuildResult sheet = BuildPartSheet(
                        asepriteFile,
                        layerIndexByName,
                        spec.PartKey,
                        importer.SpritePivot,
                        importer.SliceWidthPixels,
                        importer.SliceHeightPixels);

                    if (!sheet.Success)
                        return Fail(sheet.Reason, asepriteFile.noOfFrames);

                    EnsureAssetFolderExists(spec.AssetFolder);
                    File.WriteAllBytes(spec.FullPngPath, sheet.Texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(sheet.Texture);

                    ConfigureSpriteSheet(
                        spec.AssetPngPath,
                        sheet.Slices,
                        importer.PixelsPerUnit,
                        importer.SpritePivot);

                    createdPngCount++;
                    createdSliceCount += sheet.Slices.Length;

                    if (string.IsNullOrEmpty(firstGeneratedAssetPath))
                        firstGeneratedAssetPath = spec.AssetPngPath;
                }

                AssetDatabase.Refresh();

                return new ArcGraphAsepriteNpcSheetImportDiagnostics(
                    true,
                    ReasonSuccess,
                    asepriteFile.noOfFrames,
                    createdPngCount,
                    createdSliceCount,
                    firstGeneratedAssetPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ArcGraphAsepriteNpcSheetImporterEditorService] Import failed: " + ex);
                return Fail(ReasonImportFailed);
            }
        }

        private static ArcGraphAsepriteNpcSheetImportDiagnostics Fail(
            string reason,
            int frameCount = 0)
        {
            return new ArcGraphAsepriteNpcSheetImportDiagnostics(
                false,
                reason,
                frameCount,
                0,
                0,
                string.Empty);
        }

        private static bool TryResolveExistingSourcePath(
            string rawPath,
            out string fullPath)
        {
            fullPath = string.Empty;

            if (string.IsNullOrWhiteSpace(rawPath))
                return false;

            string normalized = NormalizePath(rawPath);
            if (Path.IsPathRooted(normalized))
            {
                fullPath = normalized;
                return File.Exists(fullPath);
            }

            string projectRoot = Directory.GetCurrentDirectory();
            fullPath = Path.GetFullPath(Path.Combine(projectRoot, normalized));
            return File.Exists(fullPath);
        }

        private static bool TryBuildOutputSpecs(
            ArcGraphAsepriteNpcSheetImporter importer,
            out PartOutputSpec[] specs,
            out string reason)
        {
            specs = null;
            reason = string.Empty;

            string fileName = NormalizeOutputFileName(importer.OutputSheetFileName);
            var candidates = new[]
            {
                new PartOutputSpec("legs", importer.LegsOutputFolder, fileName),
                new PartOutputSpec("body", importer.BodyOutputFolder, fileName),
                new PartOutputSpec("arms", importer.ArmsOutputFolder, fileName),
                new PartOutputSpec("head", importer.HeadOutputFolder, fileName)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (!candidates[i].IsValidAssetFolder)
                {
                    reason = ReasonOutputFolderInvalid + ":" + candidates[i].AssetFolder;
                    return false;
                }
            }

            specs = candidates;
            return true;
        }

        private static Dictionary<string, int> BuildLayerIndex(
            AsepriteFile file)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int layerIndex = 0;

            for (int frameIndex = 0; frameIndex < file.frameData.Count; frameIndex++)
            {
                FrameData frame = file.frameData[frameIndex];
                for (int chunkIndex = 0; chunkIndex < frame.chunks.Count; chunkIndex++)
                {
                    if (frame.chunks[chunkIndex] is not LayerChunk layer)
                        continue;

                    // L'indice usato dalle CellChunk e' l'ordine dei LayerChunk
                    // nel file Aseprite. Se troviamo nomi duplicati, teniamo il
                    // primo: il contratto ARCONTIO richiede layer canonici univoci.
                    if (!string.IsNullOrWhiteSpace(layer.name)
                        && !result.ContainsKey(layer.name))
                    {
                        result[layer.name] = layerIndex;
                    }

                    layerIndex++;
                }
            }

            return result;
        }

        // =============================================================================
        // FindFirstMissingLayer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca il primo layer sorgente obbligatorio mancante nel file Aseprite.
        /// </para>
        ///
        /// <para><b>Contratto sorgente 12 layer</b></para>
        /// <para>
        /// Il file non deve contenere layer <c>east_*</c>. Gli sprite verso est
        /// vengono prodotti ribaltando orizzontalmente i corrispondenti layer
        /// <c>west_*</c>. La validazione richiede quindi solo south, north e west
        /// per ognuna delle quattro parti corpo.
        /// </para>
        /// </summary>
        private static string FindFirstMissingLayer(
            Dictionary<string, int> layerIndexByName)
        {
            if (layerIndexByName == null)
                return "layer-index";

            for (int directionIndex = 0; directionIndex < SourceDirectionKeys.Length; directionIndex++)
            {
                for (int partIndex = 0; partIndex < PartKeys.Length; partIndex++)
                {
                    string layerName = SourceDirectionKeys[directionIndex] + "_" + PartKeys[partIndex];
                    if (!layerIndexByName.ContainsKey(layerName))
                        return layerName;
                }
            }

            return string.Empty;
        }

        // =============================================================================
        // BuildPartSheet
        // =============================================================================
        /// <summary>
        /// <para>
        /// Genera lo spritesheet di una singola parte corpo.
        /// </para>
        ///
        /// <para><b>Derivazione east da west</b></para>
        /// <para>
        /// Le righe di output restano south/east/north/west per compatibilita'
        /// con ArcGraph. Ogni direzione legge una finestra orizzontale dello
        /// stesso frame: south nel primo terzo, north nel secondo, west/east nel
        /// terzo. Quando la riga richiesta e' east, il metodo legge la finestra
        /// laterale west e passa al blit il flag di ribaltamento orizzontale.
        /// </para>
        /// </summary>
        private static SheetBuildResult BuildPartSheet(
            AsepriteFile file,
            Dictionary<string, int> layerIndexByName,
            string partKey,
            Vector2 pivot,
            int configuredSliceWidth,
            int configuredSliceHeight)
        {
            int frameCount = file.noOfFrames;
            int sliceWidth = ResolveSliceWidth(file, configuredSliceWidth);
            int sliceHeight = ResolveSliceHeight(file, configuredSliceHeight);

            if (!TryValidateSliceWindow(file, sliceWidth, sliceHeight, out string sliceWindowReason))
                return SheetBuildResult.Fail(sliceWindowReason);

            int sheetWidth = sliceWidth * frameCount;
            int sheetHeight = sliceHeight * DirectionKeys.Length;

            var texture = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGBA32, mipChain: false)
            {
                name = partKey + "_sheet",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[sheetWidth * sheetHeight];
            var slices = new GeneratedSlice[DirectionKeys.Length * frameCount];
            int sliceIndex = 0;

            for (int directionIndex = 0; directionIndex < DirectionKeys.Length; directionIndex++)
            {
                string direction = DirectionKeys[directionIndex];
                string sourceDirection = ResolveSourceDirection(direction);
                bool flipHorizontally = ShouldFlipHorizontally(direction);
                int sourceViewportX = ResolveSourceViewportX(direction, sliceWidth);
                string layerName = sourceDirection + "_" + partKey;
                int layerIndex = layerIndexByName[layerName];

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    if (!TryResolveCell(file, layerIndex, frameIndex, out ResolvedCell cell))
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                        return SheetBuildResult.Fail(ReasonFrameMissing + ":" + layerName + ":" + frameIndex);
                    }

                    int tileX = frameIndex * sliceWidth;
                    int tileBottomY = (DirectionKeys.Length - directionIndex - 1) * sliceHeight;
                    BlitCellIntoSheet(
                        pixels,
                        sheetWidth,
                        sheetHeight,
                        tileX,
                        tileBottomY,
                        sliceWidth,
                        sliceHeight,
                        sourceViewportX,
                        0,
                        cell,
                        flipHorizontally);

                    string sliceName = direction + "_" + frameIndex.ToString("00");
                    var rect = new Rect(
                        tileX,
                        tileBottomY,
                        sliceWidth,
                        sliceHeight);

                    slices[sliceIndex++] = new GeneratedSlice(
                        sliceName,
                        rect,
                        pivot);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return SheetBuildResult.SuccessResult(texture, slices);
        }

        // =============================================================================
        // BlitCellIntoSheet
        // =============================================================================
        /// <summary>
        /// <para>
        /// Copia una cella Aseprite dentro il tile destinazione dello spritesheet.
        /// </para>
        ///
        /// <para><b>Ribaltamento controllato</b></para>
        /// <para>
        /// Il flag <c>flipHorizontally</c> viene usato solo per generare la direzione
        /// east a partire dalla finestra laterale west. Il metodo prima converte
        /// la posizione pixel assoluta della cella nella finestra sorgente della
        /// direzione, poi ribalta localmente dentro la larghezza dello slice. La
        /// sorgente Aseprite viene letta top-to-bottom; la sola conversione verso
        /// coordinate texture Unity bottom-left avviene nella coordinata Y di
        /// destinazione.
        /// </para>
        /// </summary>
        private static void BlitCellIntoSheet(
            Color32[] sheetPixels,
            int sheetWidth,
            int sheetHeight,
            int tileX,
            int tileBottomY,
            int sliceWidth,
            int sliceHeight,
            int sourceViewportX,
            int sourceViewportY,
            ResolvedCell cell,
            bool flipHorizontally)
        {
            NativeArray<Color32> source = cell.Image;
            if (!source.IsCreated || source.Length == 0)
                return;

            float opacity = Mathf.Clamp01(cell.Opacity01);

            for (int y = 0; y < cell.Height; y++)
            {
                int canvasYFromTop = cell.PosY + y;
                int localYFromTop = canvasYFromTop - sourceViewportY;
                if (localYFromTop < 0 || localYFromTop >= sliceHeight)
                    continue;

                int destinationY = tileBottomY + (sliceHeight - 1 - localYFromTop);
                if (destinationY < 0 || destinationY >= sheetHeight)
                    continue;

                int sourceRow = y * cell.Width;
                int destinationRow = destinationY * sheetWidth;

                for (int x = 0; x < cell.Width; x++)
                {
                    int canvasX = cell.PosX + x;
                    int localX = canvasX - sourceViewportX;
                    if (localX < 0 || localX >= sliceWidth)
                        continue;

                    int destinationCanvasX = flipHorizontally
                        ? sliceWidth - 1 - localX
                        : localX;
                    int destinationX = tileX + destinationCanvasX;
                    if (destinationX < 0 || destinationX >= sheetWidth)
                        continue;

                    Color32 sourceColor = source[sourceRow + x];
                    if (sourceColor.a == 0)
                        continue;

                    if (opacity < 0.999f)
                        sourceColor.a = (byte)Mathf.RoundToInt(sourceColor.a * opacity);

                    int destinationIndex = destinationRow + destinationX;
                    sheetPixels[destinationIndex] = AlphaBlend(sheetPixels[destinationIndex], sourceColor);
                }
            }
        }

        // =============================================================================
        // ResolveSliceWidth
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la larghezza slice effettiva.
        /// </para>
        ///
        /// <para>
        /// Il valore configurato dall'Inspector ha priorita'. Se vale zero, il
        /// servizio assume il layout a tre finestre orizzontali e usa un terzo
        /// della larghezza canvas Aseprite.
        /// </para>
        /// </summary>
        private static int ResolveSliceWidth(
            AsepriteFile file,
            int configuredSliceWidth)
        {
            if (configuredSliceWidth > 0)
                return configuredSliceWidth;

            return file != null
                ? file.width / 3
                : 0;
        }

        // =============================================================================
        // ResolveSliceHeight
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve l'altezza slice effettiva.
        /// </para>
        ///
        /// <para>
        /// Il valore configurato dall'Inspector ha priorita'. Se vale zero, viene
        /// usata l'altezza completa del canvas Aseprite.
        /// </para>
        /// </summary>
        private static int ResolveSliceHeight(
            AsepriteFile file,
            int configuredSliceHeight)
        {
            if (configuredSliceHeight > 0)
                return configuredSliceHeight;

            return file != null
                ? file.height
                : 0;
        }

        // =============================================================================
        // TryValidateSliceWindow
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica che le finestre direzionali orizzontali possano essere lette
        /// dal canvas sorgente.
        /// </para>
        /// </summary>
        private static bool TryValidateSliceWindow(
            AsepriteFile file,
            int sliceWidth,
            int sliceHeight,
            out string reason)
        {
            reason = string.Empty;

            if (file == null || sliceWidth <= 0 || sliceHeight <= 0)
            {
                reason = ReasonSliceWindowInvalid + ":size";
                return false;
            }

            if (sliceWidth * 3 > file.width)
            {
                reason = ReasonSliceWindowInvalid + ":width";
                return false;
            }

            return true;
        }

        // =============================================================================
        // ResolveSourceViewportX
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la coordinata X della finestra sorgente per una direzione
        /// di output.
        /// </para>
        ///
        /// <para>
        /// Il layout sorgente e' diviso in tre colonne: south, north, laterale.
        /// La direzione west legge direttamente la colonna laterale; east legge
        /// la stessa colonna e viene poi ribaltata.
        /// </para>
        /// </summary>
        private static int ResolveSourceViewportX(
            string outputDirection,
            int sliceWidth)
        {
            if (string.Equals(outputDirection, "north", StringComparison.Ordinal))
                return sliceWidth;

            if (string.Equals(outputDirection, "west", StringComparison.Ordinal)
                || string.Equals(outputDirection, "east", StringComparison.Ordinal))
            {
                return sliceWidth * 2;
            }

            return 0;
        }

        // =============================================================================
        // ResolveSourceDirection
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la direzione layer da leggere per una direzione di output.
        /// </para>
        /// </summary>
        private static string ResolveSourceDirection(
            string outputDirection)
        {
            return string.Equals(outputDirection, "east", StringComparison.Ordinal)
                ? "west"
                : outputDirection;
        }

        // =============================================================================
        // ShouldFlipHorizontally
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se la direzione di output deve essere generata ribaltando la
        /// sorgente orizzontalmente.
        /// </para>
        /// </summary>
        private static bool ShouldFlipHorizontally(
            string outputDirection)
        {
            return string.Equals(outputDirection, "east", StringComparison.Ordinal);
        }

        private static Color32 AlphaBlend(
            Color32 destination,
            Color32 source)
        {
            float srcA = source.a / 255f;
            if (srcA <= 0f)
                return destination;

            if (srcA >= 1f)
                return source;

            float dstA = destination.a / 255f;
            float outA = srcA + (dstA * (1f - srcA));
            if (outA <= 0f)
                return new Color32(0, 0, 0, 0);

            byte r = (byte)Mathf.RoundToInt(((source.r * srcA) + (destination.r * dstA * (1f - srcA))) / outA);
            byte g = (byte)Mathf.RoundToInt(((source.g * srcA) + (destination.g * dstA * (1f - srcA))) / outA);
            byte b = (byte)Mathf.RoundToInt(((source.b * srcA) + (destination.b * dstA * (1f - srcA))) / outA);
            byte a = (byte)Mathf.RoundToInt(outA * 255f);
            return new Color32(r, g, b, a);
        }

        private static bool TryResolveCell(
            AsepriteFile file,
            int layerIndex,
            int frameIndex,
            out ResolvedCell resolved)
        {
            return TryResolveCell(file, layerIndex, frameIndex, 0, out resolved);
        }

        private static bool TryResolveCell(
            AsepriteFile file,
            int layerIndex,
            int frameIndex,
            int depth,
            out ResolvedCell resolved)
        {
            resolved = default;

            if (file == null || frameIndex < 0 || frameIndex >= file.frameData.Count || depth > file.noOfFrames)
                return false;

            CellChunk cell = FindCellChunk(file.frameData[frameIndex], layerIndex);
            if (cell == null)
                return false;

            if (cell.cellType == CellTypes.LinkedCell)
            {
                if (!TryResolveCell(file, layerIndex, cell.linkedToFrame, depth + 1, out ResolvedCell linked))
                    return false;

                resolved = new ResolvedCell(
                    cell.posX,
                    cell.posY,
                    linked.Width,
                    linked.Height,
                    ResolveOpacity01(cell.opacity),
                    linked.Image);
                return true;
            }

            if ((cell.cellType != CellTypes.RawImage && cell.cellType != CellTypes.CompressedImage)
                || !cell.image.IsCreated
                || cell.image.Length == 0)
            {
                return false;
            }

            resolved = new ResolvedCell(
                cell.posX,
                cell.posY,
                cell.width,
                cell.height,
                ResolveOpacity01(cell.opacity),
                cell.image);
            return true;
        }

        private static CellChunk FindCellChunk(
            FrameData frame,
            int layerIndex)
        {
            if (frame == null)
                return null;

            for (int i = 0; i < frame.chunks.Count; i++)
            {
                if (frame.chunks[i] is CellChunk cell && cell.layerIndex == layerIndex)
                    return cell;
            }

            return null;
        }

        private static float ResolveOpacity01(byte opacity)
        {
            return opacity / 255f;
        }

        private static void ConfigureSpriteSheet(
            string assetPath,
            GeneratedSlice[] slices,
            int pixelsPerUnit,
            Vector2 pivot)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("TextureImporter missing for " + assetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit > 0 ? pixelsPerUnit : 32;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
                throw new InvalidOperationException("Sprite data provider missing for " + assetPath);

            dataProvider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> existingIdsByName = dataProvider.GetSpriteRects()
                .Where(rect => !string.IsNullOrWhiteSpace(rect.name))
                .GroupBy(rect => rect.name)
                .ToDictionary(group => group.Key, group => group.First().spriteID, StringComparer.Ordinal);

            var spriteRects = new SpriteRect[slices.Length];
            var nameFileIdPairs = new SpriteNameFileIdPair[slices.Length];

            for (int i = 0; i < slices.Length; i++)
            {
                GeneratedSlice slice = slices[i];
                GUID spriteId = existingIdsByName.TryGetValue(slice.Name, out GUID existingId)
                    ? existingId
                    : GUID.Generate();

                spriteRects[i] = new SpriteRect
                {
                    name = slice.Name,
                    spriteID = spriteId,
                    rect = slice.Rect,
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    border = Vector4.zero
                };

                nameFileIdPairs[i] = new SpriteNameFileIdPair(slice.Name, spriteId);
            }

            dataProvider.SetSpriteRects(spriteRects);

            ISpriteNameFileIdDataProvider nameProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider?.SetNameFileIdPairs(nameFileIdPairs);

            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static void EnsureAssetFolderExists(
            string assetFolder)
        {
            string fullFolder = ToFullProjectPath(assetFolder);
            if (!Directory.Exists(fullFolder))
                Directory.CreateDirectory(fullFolder);
        }

        private static string NormalizeOutputFileName(
            string rawFileName)
        {
            string fileName = string.IsNullOrWhiteSpace(rawFileName)
                ? "idle.png"
                : rawFileName.Trim();

            fileName = fileName.Replace('\\', '/');
            if (fileName.Contains("/"))
                fileName = Path.GetFileName(fileName);

            return fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : fileName + ".png";
        }

        private static string NormalizePath(
            string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Replace('\\', '/');
        }

        private static string ToFullProjectPath(
            string assetPath)
        {
            string projectRoot = Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(projectRoot, NormalizePath(assetPath)));
        }

        private readonly struct PartOutputSpec
        {
            public readonly string PartKey;
            public readonly string AssetFolder;
            public readonly string AssetPngPath;
            public readonly string FullPngPath;
            public readonly bool IsValidAssetFolder;

            public PartOutputSpec(
                string partKey,
                string rawAssetFolder,
                string outputFileName)
            {
                PartKey = partKey;
                AssetFolder = NormalizePath(rawAssetFolder).TrimEnd('/');
                IsValidAssetFolder = AssetFolder.StartsWith("Assets/", StringComparison.Ordinal)
                                     || string.Equals(AssetFolder, "Assets", StringComparison.Ordinal);
                AssetPngPath = IsValidAssetFolder
                    ? AssetFolder + "/" + outputFileName
                    : string.Empty;
                FullPngPath = IsValidAssetFolder
                    ? ToFullProjectPath(AssetPngPath)
                    : string.Empty;
            }
        }

        private readonly struct GeneratedSlice
        {
            public readonly string Name;
            public readonly Rect Rect;
            public readonly Vector2 Pivot;

            public GeneratedSlice(
                string name,
                Rect rect,
                Vector2 pivot)
            {
                Name = name;
                Rect = rect;
                Pivot = pivot;
            }
        }

        private readonly struct ResolvedCell
        {
            public readonly int PosX;
            public readonly int PosY;
            public readonly int Width;
            public readonly int Height;
            public readonly float Opacity01;
            public readonly NativeArray<Color32> Image;

            public ResolvedCell(
                int posX,
                int posY,
                int width,
                int height,
                float opacity01,
                NativeArray<Color32> image)
            {
                PosX = posX;
                PosY = posY;
                Width = width;
                Height = height;
                Opacity01 = opacity01;
                Image = image;
            }
        }

        private readonly struct SheetBuildResult
        {
            public readonly bool Success;
            public readonly string Reason;
            public readonly Texture2D Texture;
            public readonly GeneratedSlice[] Slices;

            private SheetBuildResult(
                bool success,
                string reason,
                Texture2D texture,
                GeneratedSlice[] slices)
            {
                Success = success;
                Reason = reason ?? string.Empty;
                Texture = texture;
                Slices = slices ?? Array.Empty<GeneratedSlice>();
            }

            public static SheetBuildResult SuccessResult(
                Texture2D texture,
                GeneratedSlice[] slices)
            {
                return new SheetBuildResult(true, string.Empty, texture, slices);
            }

            public static SheetBuildResult Fail(
                string reason)
            {
                return new SheetBuildResult(false, reason, null, Array.Empty<GeneratedSlice>());
            }
        }
    }
}
