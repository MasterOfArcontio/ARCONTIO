using System;

namespace Arcontio.Core
{
    // =============================================================================
    // CellSurfaceMacro
    // =============================================================================
    /// <summary>
    /// <para>
    /// Categoria primaria della superficie fisica/ambientale di una cella del mondo.
    /// </para>
    ///
    /// <para><b>Principio architetturale: pavimento come dato Core, non come cache oggetti</b></para>
    /// <para>
    /// Questa enum separa la natura di base della cella da cache derivate come
    /// movimento, occlusione e occupazione oggetti. Una cella puo' essere acqua,
    /// naturale o artificiale indipendentemente dal fatto che sopra abbia un muro,
    /// un letto, un NPC o nessun oggetto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Natural</b>: superfici naturali come erba, terra, roccia grezza.</item>
    ///   <item><b>Artificial</b>: superfici costruite come pavimenti, piastrelle, strade.</item>
    ///   <item><b>Water</b>: superfici liquide o coperte da acqua.</item>
    /// </list>
    /// </summary>
    public enum CellSurfaceMacro
    {
        Natural = 0,
        Artificial = 10,
        Water = 20
    }

    // =============================================================================
    // CellSurfaceSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Fotografia read-only della superficie autoritativa di una singola cella.
    /// </para>
    ///
    /// <para><b>Principio architetturale: snapshot consumer-safe</b></para>
    /// <para>
    /// UI, ArcGraph e debug devono leggere una copia o un value object, non un array
    /// mutabile interno del <c>World</c>. Questo snapshot rende espliciti macro tipo,
    /// chiave semantica e regola visuale, senza esporre la memoria interna del layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>X/Y/Z</b>: coordinata discreta della cella.</item>
    ///   <item><b>MacroSurface</b>: categoria primaria della superficie.</item>
    ///   <item><b>SurfaceKey</b>: chiave semantica, per esempio grass o stone_floor.</item>
    ///   <item><b>VisualRuleKey</b>: chiave opzionale per scegliere una regola visuale.</item>
    ///   <item><b>BiomeAreaKey</b>: riferimento testuale opzionale a un'area/bioma futuro.</item>
    /// </list>
    /// </summary>
    public readonly struct CellSurfaceSnapshot
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public readonly CellSurfaceMacro MacroSurface;
        public readonly string SurfaceKey;
        public readonly string VisualRuleKey;
        public readonly string BiomeAreaKey;

        // =============================================================================
        // CellSurfaceSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una fotografia immutabile normalizzando le chiavi vuote.
        /// </para>
        /// </summary>
        public CellSurfaceSnapshot(
            int x,
            int y,
            int z,
            CellSurfaceMacro macroSurface,
            string surfaceKey,
            string visualRuleKey,
            string biomeAreaKey)
        {
            X = x;
            Y = y;
            Z = z;
            MacroSurface = macroSurface;
            SurfaceKey = CellSurfaceLayer.NormalizeSurfaceKey(surfaceKey, macroSurface);
            VisualRuleKey = string.IsNullOrWhiteSpace(visualRuleKey)
                ? SurfaceKey
                : visualRuleKey;
            BiomeAreaKey = string.IsNullOrWhiteSpace(biomeAreaKey)
                ? string.Empty
                : biomeAreaKey;
        }
    }

    // =============================================================================
    // CellSurfaceLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer autoritativo Core per il tipo di superficie presente su ogni cella.
    /// </para>
    ///
    /// <para><b>Principio architetturale: World come source of truth</b></para>
    /// <para>
    /// Il tipo pavimento non deve vivere in <c>_blocksMovement</c>, in
    /// <c>_occlusion</c> o in <c>_objIdByCell</c>, perche' quelle strutture sono
    /// cache derivate da oggetti e condizioni runtime. Questo layer appartiene al
    /// <c>World</c> e conserva la superficie di base della cella: cosa c'e' sotto
    /// agli oggetti, non cosa blocca o occupa temporaneamente la cella.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Array paralleli</b>: macro, surface key, visual rule e riferimento area futura.</item>
    ///   <item><b>Explicit flags</b>: distinguono default iniziale da celle realmente impostate.</item>
    ///   <item><b>SetSurface</b>: unico punto di scrittura controllata della superficie.</item>
    ///   <item><b>TryGetSurface</b>: lettura snapshot sicura per consumer e adapter.</item>
    /// </list>
    /// </summary>
    public sealed class CellSurfaceLayer
    {
        public const string DefaultNaturalSurfaceKey = "grass";
        public const string DefaultArtificialSurfaceKey = "stone_floor";
        public const string DefaultWaterSurfaceKey = "water";

        private readonly CellSurfaceMacro[] _macroSurfaces;
        private readonly string[] _surfaceKeys;
        private readonly string[] _visualRuleKeys;
        private readonly string[] _biomeAreaKeys;
        private readonly bool[] _explicitCells;

        public int Width { get; }
        public int Height { get; }
        public int ZLevel { get; }
        public int CellCount => Width * Height;
        public int ExplicitAssignmentCount { get; private set; }
        public bool HasExplicitAssignments => ExplicitAssignmentCount > 0;

        // =============================================================================
        // CellSurfaceLayer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un layer pieno di default naturale stabile.
        /// </para>
        ///
        /// <para><b>Default non equivale a dato importato</b></para>
        /// <para>
        /// Tutte le celle partono come erba naturale, ma non vengono marcate come
        /// assegnazioni esplicite. Questo permette ai bridge transitori di capire se
        /// il layer e' gia' stato popolato da una sorgente mappa reale oppure se sta
        /// solo offrendo un fallback neutro.
        /// </para>
        /// </summary>
        public CellSurfaceLayer(
            int width,
            int height,
            int zLevel = 0,
            CellSurfaceMacro defaultMacro = CellSurfaceMacro.Natural,
            string defaultSurfaceKey = DefaultNaturalSurfaceKey,
            string defaultVisualRuleKey = null)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            ZLevel = zLevel;

            int size = Width * Height;
            _macroSurfaces = new CellSurfaceMacro[size];
            _surfaceKeys = new string[size];
            _visualRuleKeys = new string[size];
            _biomeAreaKeys = new string[size];
            _explicitCells = new bool[size];

            FillDefault(defaultMacro, defaultSurfaceKey, defaultVisualRuleKey);
        }

        // =============================================================================
        // SetSurface
        // =============================================================================
        /// <summary>
        /// <para>
        /// Imposta la superficie autoritativa di una cella in bounds.
        /// </para>
        ///
        /// <para><b>Scrittura esplicita e tracciabile</b></para>
        /// <para>
        /// Il metodo non aggiorna pathfinding, occlusione o oggetti. Cambia solo la
        /// superficie di base. I sistemi derivati potranno in futuro reagire tramite
        /// command/gateway dedicati, senza confondere questo dato con le cache calde.
        /// </para>
        /// </summary>
        public bool SetSurface(
            int x,
            int y,
            CellSurfaceMacro macroSurface,
            string surfaceKey,
            string visualRuleKey = null,
            string biomeAreaKey = null)
        {
            if (!InBounds(x, y))
                return false;

            int index = Index(x, y);

            // La prima scrittura esplicita della cella viene contata una sola volta:
            // aggiornamenti successivi alla stessa cella restano leciti ma non
            // gonfiano la diagnostica del layer.
            if (!_explicitCells[index])
            {
                _explicitCells[index] = true;
                ExplicitAssignmentCount++;
            }

            _macroSurfaces[index] = macroSurface;
            _surfaceKeys[index] = NormalizeSurfaceKey(surfaceKey, macroSurface);
            _visualRuleKeys[index] = string.IsNullOrWhiteSpace(visualRuleKey)
                ? _surfaceKeys[index]
                : visualRuleKey;
            _biomeAreaKeys[index] = string.IsNullOrWhiteSpace(biomeAreaKey)
                ? string.Empty
                : biomeAreaKey;

            return true;
        }

        // =============================================================================
        // TryGetSurface
        // =============================================================================
        /// <summary>
        /// <para>
        /// Legge la superficie di una cella producendo uno snapshot read-only.
        /// </para>
        /// </summary>
        public bool TryGetSurface(int x, int y, out CellSurfaceSnapshot snapshot)
        {
            if (!InBounds(x, y))
            {
                snapshot = default;
                return false;
            }

            int index = Index(x, y);
            snapshot = new CellSurfaceSnapshot(
                x,
                y,
                ZLevel,
                _macroSurfaces[index],
                _surfaceKeys[index],
                _visualRuleKeys[index],
                _biomeAreaKeys[index]);
            return true;
        }

        // =============================================================================
        // InBounds
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se la coordinata appartiene alla griglia del layer.
        /// </para>
        /// </summary>
        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        // =============================================================================
        // NormalizeSurfaceKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza una chiave superficie vuota scegliendo il default della macro.
        /// </para>
        /// </summary>
        public static string NormalizeSurfaceKey(string surfaceKey, CellSurfaceMacro macroSurface)
        {
            if (!string.IsNullOrWhiteSpace(surfaceKey))
                return surfaceKey;

            if (macroSurface == CellSurfaceMacro.Water)
                return DefaultWaterSurfaceKey;

            if (macroSurface == CellSurfaceMacro.Artificial)
                return DefaultArtificialSurfaceKey;

            return DefaultNaturalSurfaceKey;
        }

        private void FillDefault(
            CellSurfaceMacro defaultMacro,
            string defaultSurfaceKey,
            string defaultVisualRuleKey)
        {
            string normalizedSurfaceKey = NormalizeSurfaceKey(defaultSurfaceKey, defaultMacro);
            string normalizedVisualRuleKey = string.IsNullOrWhiteSpace(defaultVisualRuleKey)
                ? normalizedSurfaceKey
                : defaultVisualRuleKey;

            for (int i = 0; i < _macroSurfaces.Length; i++)
            {
                // Queste sono scritture di costruzione, non assegnazioni semantiche
                // provenienti da un layout o da una simulazione.
                _macroSurfaces[i] = defaultMacro;
                _surfaceKeys[i] = normalizedSurfaceKey;
                _visualRuleKeys[i] = normalizedVisualRuleKey;
                _biomeAreaKeys[i] = string.Empty;
                _explicitCells[i] = false;
            }

            ExplicitAssignmentCount = 0;
        }

        private int Index(int x, int y)
        {
            return (y * Width) + x;
        }
    }
}
