using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcVisualFrame
    // =============================================================================
    /// <summary>
    /// <para>
    /// Singolo frame sprite di una parte visuale NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: frame dichiarato, animazione derivata</b></para>
    /// <para>
    /// Il frame non contiene una <c>Sprite</c> Unity. Contiene solo la chiave sprite
    /// che un resolver scene-side potra' trasformare in asset. Questo mantiene il
    /// catalogo indipendente da Resources, Addressables, prefab e scena.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>VisualKey</b>: variante visuale NPC, ad esempio human_default.</item>
    ///   <item><b>PartKey</b>: parte del corpo, ad esempio body/head/legs/feet.</item>
    ///   <item><b>DirectionKey</b>: direzione normalizzata: north/south/east/west.</item>
    ///   <item><b>AnimationKey</b>: animazione normalizzata: idle/walk/run futura.</item>
    ///   <item><b>FrameIndex</b>: indice frame dentro animazione e direzione.</item>
    ///   <item><b>SpriteKey</b>: chiave asset risolvibile dal lato scena.</item>
    ///   <item><b>DurationTicks</b>: durata logica del frame per animazione leggera.</item>
    ///   <item><b>SortingOffset</b>: offset sorting rispetto al root actor.</item>
    ///   <item><b>FrameWidthPixels/FrameHeightPixels</b>: dimensione logica del frame, utile per sprite LOD semplificati.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphNpcVisualFrame
    {
        public readonly string VisualKey;
        public readonly string PartKey;
        public readonly string DirectionKey;
        public readonly string AnimationKey;
        public readonly int FrameIndex;
        public readonly string SpriteKey;
        public readonly int DurationTicks;
        public readonly int SortingOffset;
        public readonly int FrameWidthPixels;
        public readonly int FrameHeightPixels;

        // =============================================================================
        // ArcGraphNpcVisualFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un frame visuale NPC normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphNpcVisualFrame(
            string visualKey,
            string partKey,
            string directionKey,
            string animationKey,
            int frameIndex,
            string spriteKey,
            int durationTicks,
            int sortingOffset,
            int frameWidthPixels = 32,
            int frameHeightPixels = 48)
        {
            VisualKey = NormalizeKey(visualKey, "human_default");
            PartKey = NormalizeKey(partKey, "body");
            DirectionKey = NormalizeDirection(directionKey);
            AnimationKey = NormalizeKey(animationKey, "idle");
            FrameIndex = frameIndex < 0 ? 0 : frameIndex;
            SpriteKey = spriteKey ?? string.Empty;
            DurationTicks = durationTicks > 0 ? durationTicks : 1;
            SortingOffset = sortingOffset;
            FrameWidthPixels = frameWidthPixels > 0 ? frameWidthPixels : 32;
            FrameHeightPixels = frameHeightPixels > 0 ? frameHeightPixels : 48;
        }

        internal static string NormalizeDirection(string value)
        {
            string key = NormalizeKey(value, "south");

            if (key == "north" || key == "south" || key == "east" || key == "west")
                return key;

            return "south";
        }

        internal static string NormalizeKey(
            string value,
            string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim().ToLowerInvariant();
        }
    }

    // =============================================================================
    // ArcGraphNpcVisualCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo runtime delle parti e animazioni sprite NPC ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: vestizione NPC data-driven</b></para>
    /// <para>
    /// Il catalogo descrive come comporre un NPC con piu' parti visuali e piu'
    /// direzioni senza caricare asset. Il renderer futuro potra' chiedere il frame
    /// corretto per corpo, testa, gambe e piedi in base a direzione, animazione e
    /// indice frame, poi passare la <c>SpriteKey</c> a un resolver scene-side.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DefaultVisualKey</b>: variante NPC da usare se manca una scelta specifica.</item>
    ///   <item><b>DefaultAnimationKey</b>: animazione fallback.</item>
    ///   <item><b>Parts</b>: parti ordinate da disegnare.</item>
    ///   <item><b>Frames</b>: frame dichiarati dal JSON.</item>
    ///   <item><b>TryResolveFrame</b>: lookup diretto tramite chiave composita.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphNpcVisualCatalog
    {
        private readonly string[] _parts;
        private readonly ArcGraphNpcVisualFrame[] _frames;
        private readonly Dictionary<string, ArcGraphNpcVisualFrame> _framesByKey = new();
        private readonly Dictionary<string, int> _frameCountByAnimationKey = new();

        public string DefaultVisualKey { get; }
        public string DefaultAnimationKey { get; }
        public int PixelsPerUnit { get; }
        public int FrameWidthPixels { get; }
        public int FrameHeightPixels { get; }
        public IReadOnlyList<string> Parts => _parts;
        public IReadOnlyList<ArcGraphNpcVisualFrame> Frames => _frames;
        public int PartCount => _parts.Length;
        public int FrameCount => _frames.Length;

        // =============================================================================
        // ArcGraphNpcVisualCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un catalogo visuale NPC normalizzando parti e frame.
        /// </para>
        /// </summary>
        public ArcGraphNpcVisualCatalog(
            string defaultVisualKey,
            string defaultAnimationKey,
            int pixelsPerUnit,
            int frameWidthPixels,
            int frameHeightPixels,
            string[] parts,
            ArcGraphNpcVisualFrame[] frames)
        {
            DefaultVisualKey = ArcGraphNpcVisualFrame.NormalizeKey(defaultVisualKey, "human_default");
            DefaultAnimationKey = ArcGraphNpcVisualFrame.NormalizeKey(defaultAnimationKey, "idle");
            PixelsPerUnit = pixelsPerUnit > 0 ? pixelsPerUnit : 32;
            FrameWidthPixels = frameWidthPixels > 0 ? frameWidthPixels : 32;
            FrameHeightPixels = frameHeightPixels > 0 ? frameHeightPixels : 48;
            _parts = NormalizeParts(parts);
            _frames = frames != null ? CopyFrames(frames) : new ArcGraphNpcVisualFrame[0];
            BuildFrameIndex();
        }

        // =============================================================================
        // TryResolveFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve un frame specifico senza scansionare l'intero catalogo.
        /// </para>
        ///
        /// <para><b>Lookup CPU-leggero</b></para>
        /// <para>
        /// Il renderer chiamera' questo metodo spesso. Per questo il catalogo crea
        /// un dizionario al momento della costruzione e risolve ogni frame con una
        /// sola chiave composita normalizzata.
        /// </para>
        /// </summary>
        public bool TryResolveFrame(
            string visualKey,
            string partKey,
            string directionKey,
            string animationKey,
            int frameIndex,
            out ArcGraphNpcVisualFrame frame)
        {
            string key = CreateFrameKey(
                ArcGraphNpcVisualFrame.NormalizeKey(visualKey, DefaultVisualKey),
                ArcGraphNpcVisualFrame.NormalizeKey(partKey, "body"),
                ArcGraphNpcVisualFrame.NormalizeDirection(directionKey),
                ArcGraphNpcVisualFrame.NormalizeKey(animationKey, DefaultAnimationKey),
                frameIndex < 0 ? 0 : frameIndex);

            return _framesByKey.TryGetValue(key, out frame);
        }

        // =============================================================================
        // ResolveFrameCount
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce quanti frame esistono per una combinazione parte/direzione/animazione.
        /// </para>
        ///
        /// <para><b>Animazione senza scansione runtime</b></para>
        /// <para>
        /// Il renderer deve scegliere il frame molte volte. Per evitare di contare
        /// i frame durante ogni render, il catalogo prepara un indice compatto alla
        /// costruzione e qui restituisce solo un intero gia' calcolato.
        /// </para>
        /// </summary>
        public int ResolveFrameCount(
            string visualKey,
            string partKey,
            string directionKey,
            string animationKey)
        {
            string key = CreateAnimationKey(
                ArcGraphNpcVisualFrame.NormalizeKey(visualKey, DefaultVisualKey),
                ArcGraphNpcVisualFrame.NormalizeKey(partKey, "body"),
                ArcGraphNpcVisualFrame.NormalizeDirection(directionKey),
                ArcGraphNpcVisualFrame.NormalizeKey(animationKey, DefaultAnimationKey));

            return _frameCountByAnimationKey.TryGetValue(key, out int frameCount) && frameCount > 0
                ? frameCount
                : 1;
        }

        private void BuildFrameIndex()
        {
            _framesByKey.Clear();
            _frameCountByAnimationKey.Clear();

            for (int i = 0; i < _frames.Length; i++)
            {
                ArcGraphNpcVisualFrame frame = _frames[i];
                string key = CreateFrameKey(
                    frame.VisualKey,
                    frame.PartKey,
                    frame.DirectionKey,
                    frame.AnimationKey,
                    frame.FrameIndex);
                _framesByKey[key] = frame;

                // Salviamo il conteggio come "indice massimo + 1": il catalogo
                // non richiede frame obbligatoriamente continui, ma per le
                // animazioni dichiarate da pattern produce proprio sequenze 0..N.
                string animationKey = CreateAnimationKey(
                    frame.VisualKey,
                    frame.PartKey,
                    frame.DirectionKey,
                    frame.AnimationKey);
                int candidateCount = frame.FrameIndex + 1;
                if (!_frameCountByAnimationKey.TryGetValue(animationKey, out int currentCount)
                    || candidateCount > currentCount)
                {
                    _frameCountByAnimationKey[animationKey] = candidateCount;
                }
            }
        }

        private static string[] NormalizeParts(string[] parts)
        {
            if (parts == null || parts.Length == 0)
                return new[] { "body", "head", "legs", "feet" };

            var result = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                result[i] = ArcGraphNpcVisualFrame.NormalizeKey(parts[i], "body");

            return result;
        }

        private static ArcGraphNpcVisualFrame[] CopyFrames(ArcGraphNpcVisualFrame[] frames)
        {
            var copy = new ArcGraphNpcVisualFrame[frames.Length];
            frames.CopyTo(copy, 0);
            return copy;
        }

        private static string CreateFrameKey(
            string visualKey,
            string partKey,
            string directionKey,
            string animationKey,
            int frameIndex)
        {
            return visualKey + "|" + partKey + "|" + directionKey + "|" + animationKey + "|" + frameIndex;
        }

        private static string CreateAnimationKey(
            string visualKey,
            string partKey,
            string directionKey,
            string animationKey)
        {
            return visualKey + "|" + partKey + "|" + directionKey + "|" + animationKey;
        }
    }
}
