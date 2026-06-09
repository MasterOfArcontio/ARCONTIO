using System;
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
        public string[] parts;
        public ArcGraphNpcVisualFrameDto[] frames;

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
                parts,
                BuildFrames());
        }

        private ArcGraphNpcVisualFrame[] BuildFrames()
        {
            if (frames == null || frames.Length == 0)
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

            var result = new ArcGraphNpcVisualFrame[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                ArcGraphNpcVisualFrameDto frame = frames[i];
                result[i] = frame != null
                    ? frame.ToRuntimeFrame(defaultVisualKey, defaultAnimationKey)
                    : new ArcGraphNpcVisualFrame(defaultVisualKey, "body", "south", defaultAnimationKey, 0, string.Empty, 8, 0);
            }

            return result;
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
}
