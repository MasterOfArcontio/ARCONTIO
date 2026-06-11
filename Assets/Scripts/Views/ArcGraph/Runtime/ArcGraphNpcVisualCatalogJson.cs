using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcVisualCatalogJson
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper per convertire JSON visuale NPC in catalogo runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo NPC separato dal renderer</b></para>
    /// <para>
    /// Il JSON descrive parti, direzioni, animazioni e sprite key. Il renderer non
    /// deve conoscere il formato del file: riceve un <c>ArcGraphNpcVisualCatalog</c>
    /// gia' normalizzato. Questo helper non carica sprite, non usa Resources, non
    /// crea GameObject e non modifica lo stato simulativo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DefaultResourcePath</b>: path suggerita del catalogo in Resources.</item>
    ///   <item><b>ParseOrDefault</b>: parse con catalogo fallback.</item>
    ///   <item><b>TryParse</b>: parse esplicito con esito booleano.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphNpcVisualCatalogJson
    {
        public const string DefaultResourcePath = "ArcGraph/Config/ArcGraphNpcVisualCatalog";

        // =============================================================================
        // ParseOrDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte JSON in catalogo visuale NPC, usando fallback minimo se serve.
        /// </para>
        /// </summary>
        public static ArcGraphNpcVisualCatalog ParseOrDefault(string json)
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
        /// Prova a convertire una stringa JSON in catalogo visuale NPC.
        /// </para>
        ///
        /// <para><b>Parsing puro</b></para>
        /// <para>
        /// Il metodo riceve testo gia' caricato dal chiamante. Non apre file, non
        /// usa <c>Resources.Load</c>, non stampa log e non mantiene cache globale.
        /// </para>
        /// </summary>
        public static bool TryParse(
            string json,
            out ArcGraphNpcVisualCatalog catalog)
        {
            catalog = null;

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var dto = JsonUtility.FromJson<ArcGraphNpcVisualCatalogDto>(json);
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

        private static ArcGraphNpcVisualCatalog CreateFallbackCatalog()
        {
            return new ArcGraphNpcVisualCatalog(
                "human_default",
                "idle",
                32,
                32,
                48,
                new[] { "body", "head", "legs", "feet" },
                new[]
                {
                    new ArcGraphNpcVisualFrame(
                        "human_default",
                        "body",
                        "south",
                        "idle",
                        0,
                        string.Empty,
                        8,
                        0)
                });
        }
    }

    // =============================================================================
    // ArcGraphNpcVisualCatalogDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile del catalogo visuale NPC ArcGraph.
    /// </para>
    ///
    /// <para><b>DTO mutabile solo al confine JSON</b></para>
    /// <para>
    /// La forma JSON resta semplice e leggibile. La conversione verso runtime
    /// normalizza chiavi, parti e frame, poi costruisce il dizionario interno del
    /// catalogo per evitare lookup costose durante il rendering.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphNpcVisualCatalogDto
    {
        public string defaultVisualKey = "human_default";
        public string defaultAnimationKey = "idle";
        public int pixelsPerUnit = 32;
        public int frameWidthPixels = 32;
        public int frameHeightPixels = 48;
        public string[] parts;
        public ArcGraphNpcVisualFrameDto[] frames;
        public ArcGraphNpcVisualFramePatternDto[] framePatterns;

        // =============================================================================
        // ToRuntimeCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il DTO JSON in catalogo runtime normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphNpcVisualCatalog ToRuntimeCatalog()
        {
            return new ArcGraphNpcVisualCatalog(
                defaultVisualKey,
                defaultAnimationKey,
                pixelsPerUnit,
                frameWidthPixels,
                frameHeightPixels,
                parts,
                BuildFrames());
        }

        private ArcGraphNpcVisualFrame[] BuildFrames()
        {
            int explicitFrameCount = frames != null ? frames.Length : 0;
            int patternFrameCount = CountPatternFrames();

            if (explicitFrameCount == 0 && patternFrameCount == 0)
            {
                return new[]
                {
                    new ArcGraphNpcVisualFrame(
                        defaultVisualKey,
                        "body",
                        "south",
                        defaultAnimationKey,
                        0,
                        string.Empty,
                        8,
                        0)
                };
            }

            var result = new List<ArcGraphNpcVisualFrame>(explicitFrameCount + patternFrameCount);

            for (int i = 0; i < explicitFrameCount; i++)
            {
                ArcGraphNpcVisualFrameDto frame = frames[i];
                result.Add(frame != null
                    ? frame.ToRuntimeFrame(defaultVisualKey, defaultAnimationKey)
                    : new ArcGraphNpcVisualFrame(defaultVisualKey, "body", "south", defaultAnimationKey, 0, string.Empty, 8, 0));
            }

            AppendPatternFrames(result);
            return result.ToArray();
        }

        private int CountPatternFrames()
        {
            if (framePatterns == null || framePatterns.Length == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < framePatterns.Length; i++)
            {
                ArcGraphNpcVisualFramePatternDto pattern = framePatterns[i];
                if (pattern == null)
                    continue;

                int partCount = pattern.partKeys != null && pattern.partKeys.Length > 0
                    ? pattern.partKeys.Length
                    : 1;
                int directionCount = pattern.directionKeys != null && pattern.directionKeys.Length > 0
                    ? pattern.directionKeys.Length
                    : 1;
                int frameCount = pattern.frameCount > 0 ? pattern.frameCount : 1;

                count += partCount * directionCount * frameCount;
            }

            return count;
        }

        private void AppendPatternFrames(List<ArcGraphNpcVisualFrame> result)
        {
            if (framePatterns == null || framePatterns.Length == 0)
                return;

            for (int i = 0; i < framePatterns.Length; i++)
            {
                ArcGraphNpcVisualFramePatternDto pattern = framePatterns[i];
                if (pattern == null)
                    continue;

                pattern.AppendRuntimeFrames(
                    result,
                    defaultVisualKey,
                    defaultAnimationKey);
            }
        }
    }

    // =============================================================================
    // ArcGraphNpcVisualFrameDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile di un frame sprite NPC.
    /// </para>
    ///
    /// <para><b>Entry visuale minima</b></para>
    /// <para>
    /// Per il primo passaggio servono parte, direzione, animazione, frame index,
    /// sprite key e sorting offset. Offset locali, pivot speciali, tint e item in
    /// mano potranno essere aggiunti dopo, senza cambiare il principio del catalogo.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphNpcVisualFrameDto
    {
        public string visualKey;
        public string partKey;
        public string directionKey;
        public string animationKey;
        public int frameIndex;
        public string spriteKey;
        public int durationTicks = 8;
        public int sortingOffset;

        // =============================================================================
        // ToRuntimeFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il DTO in frame runtime normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphNpcVisualFrame ToRuntimeFrame(
            string fallbackVisualKey,
            string fallbackAnimationKey)
        {
            return new ArcGraphNpcVisualFrame(
                string.IsNullOrWhiteSpace(visualKey) ? fallbackVisualKey : visualKey,
                partKey,
                directionKey,
                string.IsNullOrWhiteSpace(animationKey) ? fallbackAnimationKey : animationKey,
                frameIndex,
                spriteKey,
                durationTicks,
                sortingOffset);
        }
    }

    // =============================================================================
    // ArcGraphNpcVisualFramePatternDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO serializzabile per generare piu' frame NPC con una regola compatta.
    /// </para>
    ///
    /// <para><b>Catalogo leggibile e non esplosivo</b></para>
    /// <para>
    /// Una animazione modulare moltiplica parti, direzioni e frame. Scrivere ogni
    /// combinazione a mano renderebbe il JSON fragile. Questo pattern mantiene il
    /// file corto, ma produce comunque frame runtime normali, identici a quelli
    /// dichiarati esplicitamente.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphNpcVisualFramePatternDto
    {
        public string visualKey;
        public string[] partKeys;
        public string[] directionKeys;
        public string animationKey;
        public int frameCount = 1;
        public string spriteKeyPattern;
        public int durationTicks = 8;
        public int[] sortingOffsets;

        // =============================================================================
        // AppendRuntimeFrames
        // =============================================================================
        /// <summary>
        /// <para>
        /// Espande il pattern in frame runtime e li aggiunge alla lista ricevuta.
        /// </para>
        /// </summary>
        public void AppendRuntimeFrames(
            List<ArcGraphNpcVisualFrame> result,
            string fallbackVisualKey,
            string fallbackAnimationKey)
        {
            if (result == null)
                return;

            string resolvedVisualKey = string.IsNullOrWhiteSpace(visualKey)
                ? fallbackVisualKey
                : visualKey;
            string resolvedAnimationKey = string.IsNullOrWhiteSpace(animationKey)
                ? fallbackAnimationKey
                : animationKey;
            string[] resolvedParts = partKeys != null && partKeys.Length > 0
                ? partKeys
                : new[] { "body" };
            string[] resolvedDirections = directionKeys != null && directionKeys.Length > 0
                ? directionKeys
                : new[] { "south" };
            int resolvedFrameCount = frameCount > 0 ? frameCount : 1;

            for (int partIndex = 0; partIndex < resolvedParts.Length; partIndex++)
            {
                string partKey = resolvedParts[partIndex];
                int sortingOffset = ResolveSortingOffset(partIndex, partKey);

                for (int directionIndex = 0; directionIndex < resolvedDirections.Length; directionIndex++)
                {
                    string directionKey = resolvedDirections[directionIndex];

                    for (int frameIndex = 0; frameIndex < resolvedFrameCount; frameIndex++)
                    {
                        result.Add(new ArcGraphNpcVisualFrame(
                            resolvedVisualKey,
                            partKey,
                            directionKey,
                            resolvedAnimationKey,
                            frameIndex,
                            BuildSpriteKey(
                                resolvedVisualKey,
                                partKey,
                                directionKey,
                                resolvedAnimationKey,
                                frameIndex),
                            durationTicks,
                            sortingOffset));
                    }
                }
            }
        }

        private int ResolveSortingOffset(
            int partIndex,
            string partKey)
        {
            if (sortingOffsets != null
                && partIndex >= 0
                && partIndex < sortingOffsets.Length)
            {
                return sortingOffsets[partIndex];
            }

            string normalizedPart = ArcGraphNpcVisualFrame.NormalizeKey(partKey, "body");
            if (normalizedPart == "legs")
                return 1;
            if (normalizedPart == "feet")
                return 2;
            if (normalizedPart == "head")
                return 3;

            return 0;
        }

        private string BuildSpriteKey(
            string resolvedVisualKey,
            string partKey,
            string directionKey,
            string resolvedAnimationKey,
            int frameIndex)
        {
            string pattern = string.IsNullOrWhiteSpace(spriteKeyPattern)
                ? "ArcGraph/NPC/{visual}/{part}/{direction}_{animation}_{frame00}"
                : spriteKeyPattern;

            // La sostituzione e' volutamente semplice e dichiarativa: il JSON
            // decide il nome asset, il codice evita solo di duplicare 208 entry.
            return pattern
                .Replace("{visual}", resolvedVisualKey)
                .Replace("{part}", partKey)
                .Replace("{direction}", directionKey)
                .Replace("{animation}", resolvedAnimationKey)
                .Replace("{frame00}", frameIndex.ToString("00"))
                .Replace("{frame}", frameIndex.ToString());
        }
    }
}
