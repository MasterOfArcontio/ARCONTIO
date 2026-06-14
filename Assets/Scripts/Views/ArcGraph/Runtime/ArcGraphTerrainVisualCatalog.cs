using System;
using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisualVariant
    // =============================================================================
    /// <summary>
    /// <para>
    /// Variante visuale di un tipo terreno ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: varieta' visuale deterministica</b></para>
    /// <para>
    /// La variante non cambia la simulazione. Serve solo a decidere quale tile
    /// grafico usare per una cella che appartiene allo stesso tipo terreno, ad
    /// esempio prato base, prato con fiori o prato con piccoli ciuffi. Il peso
    /// permette una scelta pseudo-casuale stabile per coordinata, senza generare
    /// flickering runtime.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TileId</b>: id tile atlas da usare per questa variante.</item>
    ///   <item><b>Weight</b>: peso relativo nella scelta deterministica.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualVariant
    {
        public readonly int TileId;
        public readonly int Weight;

        // =============================================================================
        // ArcGraphTerrainVisualVariant
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una variante visuale normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualVariant(int tileId, int weight)
        {
            TileId = tileId;
            Weight = weight > 0 ? weight : 1;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualDetail
    // =============================================================================
    /// <summary>
    /// <para>
    /// Dettaglio decorativo visuale applicabile sopra un tipo terreno.
    /// </para>
    ///
    /// <para><b>Principio architetturale: decorazione senza peso simulativo</b></para>
    /// <para>
    /// Il dettaglio non e' un oggetto, non blocca il movimento, non occlude la
    /// vista e non entra nel layer degli item. Serve solo a disegnare piccoli
    /// elementi atmosferici sopra il tile base, ad esempio fiori, ciuffi, pietre
    /// decorative o foglie. La scelta resta deterministica per coordinata, cosi'
    /// la scena non cambia aspetto a ogni frame.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TileId</b>: id tile atlas del dettaglio decorativo.</item>
    ///   <item><b>Weight</b>: peso relativo nella scelta del dettaglio.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualDetail
    {
        public readonly int TileId;
        public readonly int Weight;

        // =============================================================================
        // ArcGraphTerrainVisualDetail
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un dettaglio decorativo normalizzando il peso.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualDetail(int tileId, int weight)
        {
            TileId = tileId;
            Weight = weight > 0 ? weight : 1;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualAnimation
    // =============================================================================
    /// <summary>
    /// <para>
    /// Descrizione passiva di una animazione tile terrain.
    /// </para>
    ///
    /// <para><b>Principio architetturale: tempo visuale separato dal tick simulativo</b></para>
    /// <para>
    /// La sequenza di frame descrive solo la presentazione. Non impone che il tick
    /// simulativo abbia la stessa frequenza dell'animazione. Questo consente di
    /// animare acqua, lava o dettagli ambientali a frequenza ridotta e solo sui
    /// chunk visibili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FrameTileIds</b>: tile id ordinati della sequenza animata.</item>
    ///   <item><b>FrameSeconds</b>: durata minima di un frame visuale.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualAnimation
    {
        private readonly int[] _frameTileIds;

        public IReadOnlyList<int> FrameTileIds => _frameTileIds ?? Array.Empty<int>();
        public int FrameCount => _frameTileIds != null ? _frameTileIds.Length : 0;
        public float FrameSeconds { get; }
        public bool IsValid => _frameTileIds != null && _frameTileIds.Length > 0 && FrameSeconds > 0f;

        // =============================================================================
        // ArcGraphTerrainVisualAnimation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una animazione terrain normalizzando frame e durata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualAnimation(int[] frameTileIds, float frameSeconds)
        {
            _frameTileIds = CopyFrameTileIds(frameTileIds);
            FrameSeconds = frameSeconds > 0.0001f ? frameSeconds : 0.25f;
        }

        public int GetFrameTileId(int frameIndex)
        {
            if (_frameTileIds == null || _frameTileIds.Length == 0)
                return 0;

            int safeIndex = frameIndex % _frameTileIds.Length;
            if (safeIndex < 0)
                safeIndex += _frameTileIds.Length;

            return _frameTileIds[safeIndex];
        }

        private static int[] CopyFrameTileIds(int[] frameTileIds)
        {
            if (frameTileIds == null || frameTileIds.Length == 0)
                return Array.Empty<int>();

            var copy = new int[frameTileIds.Length];
            frameTileIds.CopyTo(copy, 0);
            return copy;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualTransitionRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Regola visuale per una transizione tra due tipi terreno.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bordi dichiarati, non special case nel renderer</b></para>
    /// <para>
    /// La regola dice quale tile usare quando una cella di un tipo terreno confina
    /// con un altro tipo terreno secondo una maschera cardinale. Per il primo
    /// modello usiamo maschere semplici come <c>N</c>, <c>E</c>, <c>S</c>, <c>W</c>
    /// o combinazioni come <c>NE</c>. Sistemi piu' complessi tipo Wang tiles possono
    /// essere costruiti sopra questo contratto senza cambiare la mappa.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Mask</b>: direzioni cardinali normalizzate.</item>
    ///   <item><b>TileId</b>: tile atlas statico o fallback del primo frame.</item>
    ///   <item><b>Animation</b>: sequenza opzionale per bordi animati, ad esempio acqua-prato.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualTransitionRule
    {
        public readonly string Mask;
        public readonly int TileId;
        public readonly ArcGraphTerrainVisualAnimation Animation;
        public bool HasAnimation => Animation.IsValid;

        // =============================================================================
        // ArcGraphTerrainVisualTransitionRule
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una regola di transizione normalizzando la maschera.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualTransitionRule(string mask, int tileId)
            : this(mask, tileId, new ArcGraphTerrainVisualAnimation(null, 0f))
        {
        }

        // =============================================================================
        // ArcGraphTerrainVisualTransitionRule
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una regola di transizione normalizzando la maschera e
        /// registrando una eventuale sequenza animata.
        /// </para>
        ///
        /// <para>
        /// Se l'animazione non e' valida, la regola resta identica al modello
        /// statico precedente e usa solo <c>TileId</c>. Questo mantiene compatibili
        /// i bordi gia' dichiarati, come prato-pietra.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualTransitionRule(
            string mask,
            int tileId,
            ArcGraphTerrainVisualAnimation animation)
        {
            Mask = NormalizeMask(mask);
            TileId = tileId;
            Animation = animation;
        }

        public bool Matches(string mask)
        {
            return string.Equals(Mask, NormalizeMask(mask), StringComparison.Ordinal);
        }

        public int ResolveTileId(float visualTimeSeconds)
        {
            if (!Animation.IsValid)
                return TileId;

            float safeTime = visualTimeSeconds < 0f ? 0f : visualTimeSeconds;
            int frameIndex = (int)(safeTime / Animation.FrameSeconds);
            return Animation.GetFrameTileId(frameIndex);
        }

        private static string NormalizeMask(string mask)
        {
            if (string.IsNullOrWhiteSpace(mask))
                return string.Empty;

            bool n = false;
            bool e = false;
            bool s = false;
            bool w = false;

            for (int i = 0; i < mask.Length; i++)
            {
                char c = char.ToUpperInvariant(mask[i]);
                n |= c == 'N';
                e |= c == 'E';
                s |= c == 'S';
                w |= c == 'W';
            }

            return (n ? "N" : string.Empty)
                   + (e ? "E" : string.Empty)
                   + (s ? "S" : string.Empty)
                   + (w ? "W" : string.Empty);
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualTransitionSet
    // =============================================================================
    /// <summary>
    /// <para>
    /// Insieme di regole di bordo da un terreno sorgente a un terreno vicino.
    /// </para>
    ///
    /// <para><b>Principio architetturale: transizioni leggibili e limitate</b></para>
    /// <para>
    /// Il set tiene separate le transizioni dal tipo terreno base. Questo evita di
    /// gonfiare il renderer con if specifici come "se prato vicino a pietra". Il
    /// resolver consulta il set solo quando riceve la maschera dei vicini.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FromTerrainId</b>: terreno della cella corrente.</item>
    ///   <item><b>ToTerrainId</b>: terreno vicino che genera il bordo.</item>
    ///   <item><b>Rules</b>: regole maschera -> tile id.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainVisualTransitionSet
    {
        private readonly ArcGraphTerrainVisualTransitionRule[] _rules;

        public string FromTerrainId { get; }
        public string ToTerrainId { get; }
        public IReadOnlyList<ArcGraphTerrainVisualTransitionRule> Rules => _rules;
        public int RuleCount => _rules.Length;

        // =============================================================================
        // ArcGraphTerrainVisualTransitionSet
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un set di transizioni normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualTransitionSet(
            string fromTerrainId,
            string toTerrainId,
            ArcGraphTerrainVisualTransitionRule[] rules)
        {
            FromTerrainId = NormalizeTerrainId(fromTerrainId);
            ToTerrainId = NormalizeTerrainId(toTerrainId);
            _rules = CopyRules(rules);
        }

        public bool TryResolveTileId(string mask, out int tileId)
        {
            return TryResolveTileId(mask, 0f, out tileId, out _);
        }

        public bool TryResolveTileId(
            string mask,
            float visualTimeSeconds,
            out int tileId,
            out bool usedAnimation)
        {
            for (int i = 0; i < _rules.Length; i++)
            {
                if (_rules[i].Matches(mask))
                {
                    tileId = _rules[i].ResolveTileId(visualTimeSeconds);
                    usedAnimation = _rules[i].HasAnimation;
                    return true;
                }
            }

            tileId = 0;
            usedAnimation = false;
            return false;
        }

        internal static string NormalizeTerrainId(string terrainId)
        {
            return string.IsNullOrWhiteSpace(terrainId)
                ? "unknown"
                : terrainId.Trim().ToLowerInvariant();
        }

        private static ArcGraphTerrainVisualTransitionRule[] CopyRules(
            ArcGraphTerrainVisualTransitionRule[] rules)
        {
            if (rules == null || rules.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualTransitionRule>();

            var copy = new ArcGraphTerrainVisualTransitionRule[rules.Length];
            rules.CopyTo(copy, 0);
            return copy;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualDefinition
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione visuale completa di un tipo terreno.
    /// </para>
    ///
    /// <para><b>Principio architetturale: terrain type prima del tile finale</b></para>
    /// <para>
    /// Il tipo terreno descrive cosa rappresenta la cella a livello visuale, ad
    /// esempio <c>grass</c>, <c>stone_floor</c> o <c>water</c>. Il tile finale viene
    /// poi risolto da varianti, animazioni o transizioni. Questa separazione evita
    /// che la mappa debba conoscere ogni variante grafica.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>TerrainId</b>: identificatore leggibile del tipo terreno.</item>
    ///   <item><b>DefaultTileId</b>: tile fallback stabile.</item>
    ///   <item><b>Variants</b>: varianti visuali pesate.</item>
    ///   <item><b>Animation</b>: animazione opzionale.</item>
    ///   <item><b>Details</b>: overlay decorativi opzionali e non simulativi.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainVisualDefinition
    {
        private readonly ArcGraphTerrainVisualVariant[] _variants;
        private readonly ArcGraphTerrainVisualDetail[] _details;

        public string TerrainId { get; }
        public int DefaultTileId { get; }
        public IReadOnlyList<ArcGraphTerrainVisualVariant> Variants => _variants;
        public int VariantCount => _variants.Length;
        public ArcGraphTerrainVisualAnimation Animation { get; }
        public bool HasAnimation => Animation.IsValid;
        public IReadOnlyList<ArcGraphTerrainVisualDetail> Details => _details;
        public int DetailCount => _details.Length;
        public int DetailChancePermille { get; }
        public bool HasDetails => _details.Length > 0 && DetailChancePermille > 0;

        // =============================================================================
        // ArcGraphTerrainVisualDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione visuale terrain normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualDefinition(
            string terrainId,
            int defaultTileId,
            ArcGraphTerrainVisualVariant[] variants,
            ArcGraphTerrainVisualAnimation animation)
            : this(terrainId, defaultTileId, variants, animation, null, 0)
        {
        }

        // =============================================================================
        // ArcGraphTerrainVisualDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una definizione visuale terrain normalizzata, includendo
        /// eventuali dettagli decorativi puramente grafici.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualDefinition(
            string terrainId,
            int defaultTileId,
            ArcGraphTerrainVisualVariant[] variants,
            ArcGraphTerrainVisualAnimation animation,
            ArcGraphTerrainVisualDetail[] details,
            int detailChancePermille)
        {
            TerrainId = ArcGraphTerrainVisualTransitionSet.NormalizeTerrainId(terrainId);
            DefaultTileId = defaultTileId;
            _variants = NormalizeVariants(defaultTileId, variants);
            Animation = animation;
            _details = NormalizeDetails(details);
            DetailChancePermille = ClampPermille(detailChancePermille);
        }

        public int ResolveVariantTileId(int seed)
        {
            if (_variants.Length == 0)
                return DefaultTileId;

            int totalWeight = 0;
            for (int i = 0; i < _variants.Length; i++)
                totalWeight += _variants[i].Weight;

            if (totalWeight <= 0)
                return DefaultTileId;

            int roll = PositiveModulo(seed, totalWeight);
            int cursor = 0;

            for (int i = 0; i < _variants.Length; i++)
            {
                cursor += _variants[i].Weight;
                if (roll < cursor)
                    return _variants[i].TileId;
            }

            return _variants[_variants.Length - 1].TileId;
        }

        public bool TryResolveDetailTileId(int seed, out int tileId)
        {
            tileId = 0;

            if (!HasDetails)
                return false;

            // Il primo roll decide se la cella riceve davvero un dettaglio. La
            // soglia in permille evita float e mantiene stabile il risultato.
            int chanceRoll = PositiveModulo(seed, 1000);
            if (chanceRoll >= DetailChancePermille)
                return false;

            int totalWeight = 0;
            for (int i = 0; i < _details.Length; i++)
                totalWeight += _details[i].Weight;

            if (totalWeight <= 0)
                return false;

            int detailRoll = PositiveModulo(seed / 1000, totalWeight);
            int cursor = 0;
            for (int i = 0; i < _details.Length; i++)
            {
                cursor += _details[i].Weight;
                if (detailRoll < cursor)
                {
                    tileId = _details[i].TileId;
                    return true;
                }
            }

            tileId = _details[_details.Length - 1].TileId;
            return true;
        }

        private static ArcGraphTerrainVisualVariant[] NormalizeVariants(
            int defaultTileId,
            ArcGraphTerrainVisualVariant[] variants)
        {
            if (variants == null || variants.Length == 0)
            {
                return new[]
                {
                    new ArcGraphTerrainVisualVariant(defaultTileId, 1)
                };
            }

            var copy = new ArcGraphTerrainVisualVariant[variants.Length];
            variants.CopyTo(copy, 0);
            return copy;
        }

        private static ArcGraphTerrainVisualDetail[] NormalizeDetails(
            ArcGraphTerrainVisualDetail[] details)
        {
            if (details == null || details.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualDetail>();

            var copy = new ArcGraphTerrainVisualDetail[details.Length];
            details.CopyTo(copy, 0);
            return copy;
        }

        private static int ClampPermille(int value)
        {
            if (value < 0)
                return 0;

            return value > 1000 ? 1000 : value;
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
                return 0;

            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualDualGridRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Regola visuale dual-grid che collega una maschera a quattro quadranti a un
    /// tile atlas.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bordo derivato da 2x2 semantico</b></para>
    /// <para>
    /// La regola non guarda una singola cella e i suoi lati, ma una finestra di
    /// quattro celle logiche. La maschera usa sempre l'ordine dichiarato dal
    /// progetto: alto sinistra, alto destra, basso sinistra, basso destra. Per il
    /// prato questo significa che <c>0001</c> rappresenta tre quadranti non prato e
    /// prato solo in basso a destra.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Mask</b>: stringa normalizzata di quattro caratteri 0/1.</item>
    ///   <item><b>TileId</b>: tile atlas della transizione dual-grid.</item>
    ///   <item><b>Animation</b>: sequenza opzionale futura per transizioni animate.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphTerrainVisualDualGridRule
    {
        public readonly string Mask;
        public readonly int TileId;
        public readonly ArcGraphTerrainVisualAnimation Animation;
        public bool HasAnimation => Animation.IsValid;

        // =============================================================================
        // ArcGraphTerrainVisualDualGridRule
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una regola dual-grid normalizzando maschera e animazione.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualDualGridRule(
            string mask,
            int tileId,
            ArcGraphTerrainVisualAnimation animation)
        {
            Mask = NormalizeMask(mask);
            TileId = tileId;
            Animation = animation;
        }

        public bool Matches(string mask)
        {
            return string.Equals(Mask, NormalizeMask(mask), StringComparison.Ordinal);
        }

        public int ResolveTileId(float visualTimeSeconds)
        {
            if (!Animation.IsValid)
                return TileId;

            float safeTime = visualTimeSeconds < 0f ? 0f : visualTimeSeconds;
            int frameIndex = (int)(safeTime / Animation.FrameSeconds);
            return Animation.GetFrameTileId(frameIndex);
        }

        internal static string NormalizeMask(string mask)
        {
            if (string.IsNullOrWhiteSpace(mask))
                return "0000";

            char[] normalized = { '0', '0', '0', '0' };
            int cursor = 0;
            for (int i = 0; i < mask.Length && cursor < normalized.Length; i++)
            {
                char c = mask[i];
                if (c != '0' && c != '1')
                    continue;

                normalized[cursor] = c;
                cursor++;
            }

            return new string(normalized);
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualDualGridOverlay
    // =============================================================================
    /// <summary>
    /// <para>
    /// Overlay dual-grid per un terrain type che deve stare sopra ai terreni
    /// sottostanti.
    /// </para>
    ///
    /// <para><b>Principio architetturale: terreno superiore indipendente dal fondo</b></para>
    /// <para>
    /// Il caso guida e' il prato: il codice non deve sapere se sotto c'e' pietra,
    /// mattonella, terra o acqua. L'overlay dice solo dove il prato compare nei
    /// quattro quadranti della finestra 2x2. Il mesh builder puo' quindi disegnare
    /// prima il terreno sottostante e poi il tile prato trasparente in overlay.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>OverlayTerrainId</b>: terrain type che produce gli 1 della maschera.</item>
    ///   <item><b>Priority</b>: ordine futuro tra piu' overlay concorrenti.</item>
    ///   <item><b>Rules</b>: regole maschera 2x2 -> tile atlas.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainVisualDualGridOverlay
    {
        private readonly ArcGraphTerrainVisualDualGridRule[] _rules;

        public string OverlayTerrainId { get; }
        public int Priority { get; }
        public IReadOnlyList<ArcGraphTerrainVisualDualGridRule> Rules => _rules;
        public int RuleCount => _rules.Length;

        // =============================================================================
        // ArcGraphTerrainVisualDualGridOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un overlay dual-grid normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualDualGridOverlay(
            string overlayTerrainId,
            int priority,
            ArcGraphTerrainVisualDualGridRule[] rules)
        {
            OverlayTerrainId = ArcGraphTerrainVisualTransitionSet.NormalizeTerrainId(overlayTerrainId);
            Priority = priority;
            _rules = CopyRules(rules);
        }

        public bool IsOverlayTerrain(string terrainId)
        {
            return string.Equals(
                OverlayTerrainId,
                ArcGraphTerrainVisualTransitionSet.NormalizeTerrainId(terrainId),
                StringComparison.Ordinal);
        }

        public bool TryResolveTileId(
            string mask,
            float visualTimeSeconds,
            out int tileId,
            out bool usedAnimation)
        {
            for (int i = 0; i < _rules.Length; i++)
            {
                if (_rules[i].Matches(mask))
                {
                    tileId = _rules[i].ResolveTileId(visualTimeSeconds);
                    usedAnimation = _rules[i].HasAnimation;
                    return true;
                }
            }

            tileId = 0;
            usedAnimation = false;
            return false;
        }

        private static ArcGraphTerrainVisualDualGridRule[] CopyRules(
            ArcGraphTerrainVisualDualGridRule[] rules)
        {
            if (rules == null || rules.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualDualGridRule>();

            var copy = new ArcGraphTerrainVisualDualGridRule[rules.Length];
            rules.CopyTo(copy, 0);
            return copy;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualCatalog
    // =============================================================================
    /// <summary>
    /// <para>
    /// Catalogo visuale data-driven per il terreno ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: authoring terrain senza renderer onnisciente</b></para>
    /// <para>
    /// Il catalogo conserva definizioni di terreno, varianti, dettagli decorativi,
    /// animazioni e transizioni. Non legge la mappa, non carica texture, non crea
    /// mesh e non interroga il <c>World</c>. Il renderer potra' usarlo tramite un
    /// resolver passivo, mantenendo separati dato simulativo, dato visuale e scena
    /// Unity.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Definitions</b>: tipi terreno indicizzati per id leggibile.</item>
    ///   <item><b>TransitionSets</b>: regole opzionali per bordi tra terreni.</item>
    ///   <item><b>DualGridOverlays</b>: overlay 2x2 per terreni superiori come il prato.</item>
    ///   <item><b>TryGetDefinition</b>: lookup runtime senza scansione esterna.</item>
    ///   <item><b>TryGetTransitionSet</b>: lookup da terreno sorgente a terreno vicino.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphTerrainVisualCatalog
    {
        private readonly ArcGraphTerrainVisualDefinition[] _definitions;
        private readonly ArcGraphTerrainVisualTransitionSet[] _transitionSets;
        private readonly ArcGraphTerrainVisualDualGridOverlay[] _dualGridOverlays;
        private readonly Dictionary<string, ArcGraphTerrainVisualDefinition> _definitionsById;
        private readonly Dictionary<string, ArcGraphTerrainVisualTransitionSet> _transitionSetsByPair;

        public IReadOnlyList<ArcGraphTerrainVisualDefinition> Definitions => _definitions;
        public IReadOnlyList<ArcGraphTerrainVisualTransitionSet> TransitionSets => _transitionSets;
        public IReadOnlyList<ArcGraphTerrainVisualDualGridOverlay> DualGridOverlays => _dualGridOverlays;
        public int DefinitionCount => _definitions.Length;
        public int TransitionSetCount => _transitionSets.Length;
        public int DualGridOverlayCount => _dualGridOverlays.Length;

        // =============================================================================
        // ArcGraphTerrainVisualCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un catalogo visuale terrain normalizzando gli indici interni.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualCatalog(
            ArcGraphTerrainVisualDefinition[] definitions,
            ArcGraphTerrainVisualTransitionSet[] transitionSets)
            : this(definitions, transitionSets, null)
        {
        }

        // =============================================================================
        // ArcGraphTerrainVisualCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un catalogo visuale terrain completo di overlay dual-grid.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualCatalog(
            ArcGraphTerrainVisualDefinition[] definitions,
            ArcGraphTerrainVisualTransitionSet[] transitionSets,
            ArcGraphTerrainVisualDualGridOverlay[] dualGridOverlays)
        {
            _definitions = definitions != null
                ? CopyDefinitions(definitions)
                : Array.Empty<ArcGraphTerrainVisualDefinition>();

            _transitionSets = transitionSets != null
                ? CopyTransitionSets(transitionSets)
                : Array.Empty<ArcGraphTerrainVisualTransitionSet>();

            _dualGridOverlays = dualGridOverlays != null
                ? CopyDualGridOverlays(dualGridOverlays)
                : Array.Empty<ArcGraphTerrainVisualDualGridOverlay>();

            _definitionsById = BuildDefinitionsIndex(_definitions);
            _transitionSetsByPair = BuildTransitionSetIndex(_transitionSets);
        }

        public bool TryGetDefinition(
            string terrainId,
            out ArcGraphTerrainVisualDefinition definition)
        {
            return _definitionsById.TryGetValue(
                ArcGraphTerrainVisualTransitionSet.NormalizeTerrainId(terrainId),
                out definition);
        }

        public bool TryGetTransitionSet(
            string fromTerrainId,
            string toTerrainId,
            out ArcGraphTerrainVisualTransitionSet transitionSet)
        {
            return _transitionSetsByPair.TryGetValue(
                BuildTransitionKey(fromTerrainId, toTerrainId),
                out transitionSet);
        }

        internal static string BuildTransitionKey(string fromTerrainId, string toTerrainId)
        {
            return ArcGraphTerrainVisualTransitionSet.NormalizeTerrainId(fromTerrainId)
                   + "->"
                   + ArcGraphTerrainVisualTransitionSet.NormalizeTerrainId(toTerrainId);
        }

        private static ArcGraphTerrainVisualDefinition[] CopyDefinitions(
            ArcGraphTerrainVisualDefinition[] definitions)
        {
            var copy = new ArcGraphTerrainVisualDefinition[definitions.Length];
            definitions.CopyTo(copy, 0);
            return copy;
        }

        private static ArcGraphTerrainVisualTransitionSet[] CopyTransitionSets(
            ArcGraphTerrainVisualTransitionSet[] transitionSets)
        {
            var copy = new ArcGraphTerrainVisualTransitionSet[transitionSets.Length];
            transitionSets.CopyTo(copy, 0);
            return copy;
        }

        private static ArcGraphTerrainVisualDualGridOverlay[] CopyDualGridOverlays(
            ArcGraphTerrainVisualDualGridOverlay[] dualGridOverlays)
        {
            var copy = new ArcGraphTerrainVisualDualGridOverlay[dualGridOverlays.Length];
            dualGridOverlays.CopyTo(copy, 0);
            Array.Sort(copy, CompareDualGridOverlays);
            return copy;
        }

        private static int CompareDualGridOverlays(
            ArcGraphTerrainVisualDualGridOverlay left,
            ArcGraphTerrainVisualDualGridOverlay right)
        {
            if (left == null && right == null)
                return 0;

            if (left == null)
                return 1;

            if (right == null)
                return -1;

            return right.Priority.CompareTo(left.Priority);
        }

        private static Dictionary<string, ArcGraphTerrainVisualDefinition> BuildDefinitionsIndex(
            ArcGraphTerrainVisualDefinition[] definitions)
        {
            var index = new Dictionary<string, ArcGraphTerrainVisualDefinition>();
            for (int i = 0; i < definitions.Length; i++)
            {
                ArcGraphTerrainVisualDefinition definition = definitions[i];
                if (definition == null)
                    continue;

                index[definition.TerrainId] = definition;
            }

            return index;
        }

        private static Dictionary<string, ArcGraphTerrainVisualTransitionSet> BuildTransitionSetIndex(
            ArcGraphTerrainVisualTransitionSet[] transitionSets)
        {
            var index = new Dictionary<string, ArcGraphTerrainVisualTransitionSet>();
            for (int i = 0; i < transitionSets.Length; i++)
            {
                ArcGraphTerrainVisualTransitionSet transitionSet = transitionSets[i];
                if (transitionSet == null)
                    continue;

                index[BuildTransitionKey(transitionSet.FromTerrainId, transitionSet.ToTerrainId)] = transitionSet;
            }

            return index;
        }
    }
}
