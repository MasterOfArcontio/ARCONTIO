using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphSerializedSpriteResolverEntry
    // =============================================================================
    /// <summary>
    /// <para>
    /// Entry serializzabile che collega una sprite key ArcGraph a una
    /// <see cref="Sprite"/> Unity assegnata da Inspector.
    /// </para>
    ///
    /// <para><b>Principio architetturale: mapping esplicito prima del lookup asset</b></para>
    /// <para>
    /// Questa entry permette di assegnare manualmente asset specifici al resolver
    /// scena. Il mapping esplicito ha priorita' rispetto al lookup opzionale in
    /// <c>Resources</c>, cosi' eventuali override locali restano controllabili da
    /// Inspector senza modificare il catalogo dati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>kind</b>: tipo item actor/object.</item>
    ///   <item><b>spriteKey</b>: chiave prodotta da ArcGraph.</item>
    ///   <item><b>sprite</b>: asset Unity assegnato da Inspector.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct ArcGraphSerializedSpriteResolverEntry
    {
        public ArcGraphRenderItemKind kind;
        public string spriteKey;
        public Sprite sprite;
    }

    // =============================================================================
    // ArcGraphSerializedSpriteResolver
    // =============================================================================
    /// <summary>
    /// <para>
    /// Resolver sprite scene-side basato su mapping serializzati da Inspector e
    /// lookup opzionale in <c>Assets/Resources</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: asset load confinato al bordo scena</b></para>
    /// <para>
    /// Questo componente implementa <see cref="IArcGraphSpriteResolver"/> per il
    /// probe actor/object. Non legge <c>World</c>, non cerca oggetti nella scena e
    /// non modifica ArcGraph. L'unico caricamento dinamico ammesso qui e'
    /// <c>Resources.Load&lt;Sprite&gt;</c>, confinato a questo resolver scene-side:
    /// i builder passivi ArcGraph continuano a produrre solo chiavi e richieste
    /// dati, senza conoscere Unity asset database o cartelle progetto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>entries</b>: mapping specifici sprite key -> sprite.</item>
    ///   <item><b>defaultActorSprite/defaultObjectSprite</b>: fallback dichiarati.</item>
    ///   <item><b>enableResourcesLookup</b>: abilita lookup automatico da Resources.</item>
    ///   <item><b>TryResolveSprite</b>: risoluzione richiesta dal probe.</item>
    ///   <item><b>BuildCacheIfNeeded</b>: cache locale delle entry serializzate.</item>
    ///   <item><b>ResolveFromResources</b>: lookup scene-side con cache hit/miss.</item>
    ///   <item><b>TryResolveFromResourceSheetKey</b>: lookup sub-sprite da PNG sliced.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphSerializedSpriteResolver : MonoBehaviour, IArcGraphSpriteResolver
    {
        [SerializeField] private List<ArcGraphSerializedSpriteResolverEntry> entries = new();
        [SerializeField] private Sprite defaultActorSprite;
        [SerializeField] private Sprite defaultObjectSprite;
        [SerializeField] private bool enableResourcesLookup = true;
        [SerializeField] private bool cacheResourcesHits = true;
        [SerializeField] private bool cacheResourcesMisses = true;
        [SerializeField] private bool logDiagnostics;

        private readonly Dictionary<string, Sprite> _spritesByTypedKey = new();
        private readonly Dictionary<string, Sprite> _resourceSpritesByKey = new();
        private readonly Dictionary<string, Dictionary<string, Sprite>> _resourceSheetSpritesByPath = new();
        private readonly HashSet<string> _resourceMissesByKey = new();
        private bool _cacheDirty = true;
        private int _manualHitCount;
        private int _resourceHitCount;
        private int _resourceMissCount;
        private int _fallbackHitCount;

        public int ManualHitCount => _manualHitCount;
        public int ResourceHitCount => _resourceHitCount;
        public int ResourceMissCount => _resourceMissCount;
        public int FallbackHitCount => _fallbackHitCount;

        // =============================================================================
        // TryResolveSprite
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a risolvere una sprite key usando mapping manuale, lookup
        /// <c>Resources</c> opzionale e fallback serializzati.
        /// </para>
        ///
        /// <para><b>Ordine di risoluzione stabile</b></para>
        /// <para>
        /// Prima vengono rispettati gli override dell'Inspector. Solo se non esiste
        /// un mapping esplicito, il resolver prova a caricare la sprite da
        /// <c>Assets/Resources</c> usando la sprite key ArcGraph come path. Se anche
        /// quel tentativo fallisce, usa il fallback actor/object assegnato da
        /// Inspector. Se manca anche il fallback, restituisce <c>false</c> e lascia
        /// al renderer la diagnostica visuale.
        /// </para>
        /// </summary>
        public bool TryResolveSprite(
            ArcGraphSpriteResolveRequest request,
            out Sprite sprite)
        {
            BuildCacheIfNeeded();

            string typedKey = CreateTypedKey(request.Kind, request.SpriteKey);
            if (_spritesByTypedKey.TryGetValue(typedKey, out sprite) && sprite != null)
            {
                _manualHitCount++;
                return true;
            }

            if (ResolveFromResources(request.SpriteKey, out sprite))
            {
                _resourceHitCount++;
                return true;
            }

            sprite = ResolveFallback(request.Kind);
            if (sprite == null)
                return false;

            _fallbackHitCount++;
            return true;
        }

        // =============================================================================
        // ClearRuntimeCacheFromContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota manualmente le cache runtime del resolver da menu contestuale Unity.
        /// </para>
        ///
        /// <para><b>Uso previsto durante test asset</b></para>
        /// <para>
        /// Serve quando si cambiano PNG o import settings mentre la scena e' aperta:
        /// il mapping Inspector viene ricostruito, gli hit/miss <c>Resources</c>
        /// vengono dimenticati e i contatori diagnostici tornano a zero.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear Sprite Resolver Runtime Cache")]
        public void ClearRuntimeCacheFromContextMenu()
        {
            ClearRuntimeCache();
        }

        private void OnValidate()
        {
            _cacheDirty = true;
            _resourceMissesByKey.Clear();
        }

        private void ClearRuntimeCache()
        {
            _cacheDirty = true;
            _spritesByTypedKey.Clear();
            _resourceSpritesByKey.Clear();
            _resourceSheetSpritesByPath.Clear();
            _resourceMissesByKey.Clear();
            _manualHitCount = 0;
            _resourceHitCount = 0;
            _resourceMissCount = 0;
            _fallbackHitCount = 0;
        }

        private void BuildCacheIfNeeded()
        {
            if (!_cacheDirty)
                return;

            _spritesByTypedKey.Clear();

            // Il loop e' piccolo e avviene solo quando la cache e' sporca. Le entry
            // senza chiave o senza sprite vengono ignorate, cosi' l'Inspector puo'
            // contenere righe provvisorie non ancora configurate.
            for (int i = 0; i < entries.Count; i++)
            {
                ArcGraphSerializedSpriteResolverEntry entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry.spriteKey) || entry.sprite == null)
                    continue;

                string typedKey = CreateTypedKey(entry.kind, entry.spriteKey);
                _spritesByTypedKey[typedKey] = entry.sprite;
            }

            _cacheDirty = false;
        }

        private bool ResolveFromResources(
            string spriteKey,
            out Sprite sprite)
        {
            sprite = null;

            if (!enableResourcesLookup || string.IsNullOrWhiteSpace(spriteKey))
                return false;

            // Resources.Load vuole un path relativo alla cartella Resources e senza
            // estensione. Le sprite key del catalogo NPC sono gia' prodotte in
            // quella forma, per esempio:
            // ArcGraph/NPC/human_default/body/south_idle_00
            //
            // Per gli sprite sliced usiamo invece una convenzione esplicita:
            // sheet#subSprite. Esempio:
            // ArcGraph/Objects/wall_stone#wall_stone_1010
            // In quel caso si carica una volta la sheet con Resources.LoadAll e
            // poi si cerca il nome della sub-sprite dentro gli asset importati.
            if (_resourceSpritesByKey.TryGetValue(spriteKey, out sprite) && sprite != null)
                return true;

            if (_resourceMissesByKey.Contains(spriteKey))
                return false;

            if (TryResolveFromResourceSheetKey(spriteKey, out sprite))
            {
                if (cacheResourcesHits)
                    _resourceSpritesByKey[spriteKey] = sprite;

                if (logDiagnostics)
                    Debug.Log("[ArcGraphSerializedSpriteResolver] Resources sheet hit: " + spriteKey, this);

                return true;
            }

            sprite = Resources.Load<Sprite>(spriteKey);
            if (sprite != null)
            {
                if (cacheResourcesHits)
                    _resourceSpritesByKey[spriteKey] = sprite;

                if (logDiagnostics)
                    Debug.Log("[ArcGraphSerializedSpriteResolver] Resources hit: " + spriteKey, this);

                return true;
            }

            if (TryResolveFromResourceSpriteCollection(spriteKey, out sprite))
            {
                if (cacheResourcesHits)
                    _resourceSpritesByKey[spriteKey] = sprite;

                if (logDiagnostics)
                    Debug.Log("[ArcGraphSerializedSpriteResolver] Resources sprite collection hit: " + spriteKey, this);

                return true;
            }

            _resourceMissCount++;
            if (cacheResourcesMisses)
                _resourceMissesByKey.Add(spriteKey);

            if (logDiagnostics)
                Debug.Log("[ArcGraphSerializedSpriteResolver] Resources miss: " + spriteKey, this);

            return false;
        }

        // =============================================================================
        // TryResolveFromResourceSpriteCollection
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a risolvere una sprite caricando tutte le sprite presenti nello
        /// stesso asset Resources.
        /// </para>
        ///
        /// <para><b>Principio architetturale: tolleranza agli import settings Unity</b></para>
        /// <para>
        /// Alcuni PNG singoli possono essere importati da Unity come
        /// <c>Sprite Mode = Multiple</c>. In quel caso <c>Resources.Load&lt;Sprite&gt;</c>
        /// puo' non trovare la sprite anche se il file esiste e anche se
        /// <c>Resources.LoadAll&lt;Sprite&gt;</c> la restituisce correttamente. Questo
        /// fallback resta confinato al resolver asset-side e non cambia il
        /// contratto dei builder ArcGraph, che continuano a produrre solo sprite
        /// key testuali.
        /// </para>
        /// </summary>
        private static bool TryResolveFromResourceSpriteCollection(
            string spriteKey,
            out Sprite sprite)
        {
            sprite = null;

            if (string.IsNullOrWhiteSpace(spriteKey))
                return false;

            Sprite[] sprites = Resources.LoadAll<Sprite>(spriteKey);
            if (sprites == null || sprites.Length == 0)
                return false;

            string requestedName = ExtractResourceName(spriteKey);
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite candidate = sprites[i];
                if (candidate == null)
                    continue;

                if (string.Equals(candidate.name, requestedName, StringComparison.Ordinal)
                    || string.Equals(candidate.name, requestedName + "_0", StringComparison.Ordinal))
                {
                    sprite = candidate;
                    return true;
                }
            }

            // Se il file contiene una sola sub-sprite, e' il fallback piu' sicuro:
            // il path Resources identifica gia' il PNG richiesto e non una sheet
            // condivisa con nomi multipli.
            if (sprites.Length == 1 && sprites[0] != null)
            {
                sprite = sprites[0];
                return true;
            }

            return false;
        }

        // =============================================================================
        // TryResolveFromResourceSheetKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a risolvere una sprite key nel formato <c>sheet#subSprite</c>.
        /// </para>
        ///
        /// <para><b>Contratto per spritesheet sliced</b></para>
        /// <para>
        /// Unity importa una PNG con <c>Sprite Mode = Multiple</c> come gruppo di
        /// sub-sprite. <c>Resources.Load&lt;Sprite&gt;</c> non basta a recuperare in modo
        /// affidabile una singola sub-sprite da quel gruppo; per questo il resolver
        /// carica la sheet con <c>Resources.LoadAll&lt;Sprite&gt;</c> e indicizza i nomi.
        /// Il lookup resta confinato al bordo scena: i builder ArcGraph continuano a
        /// produrre solo stringhe.
        /// </para>
        /// </summary>
        private bool TryResolveFromResourceSheetKey(
            string spriteKey,
            out Sprite sprite)
        {
            sprite = null;

            if (!TryParseResourceSheetKey(spriteKey, out string sheetPath, out string subSpriteName))
                return false;

            Dictionary<string, Sprite> spritesByName = GetOrBuildResourceSheetCache(sheetPath);
            if (spritesByName == null || spritesByName.Count == 0)
                return false;

            return spritesByName.TryGetValue(subSpriteName, out sprite) && sprite != null;
        }

        private Dictionary<string, Sprite> GetOrBuildResourceSheetCache(string sheetPath)
        {
            if (string.IsNullOrWhiteSpace(sheetPath))
                return null;

            if (_resourceSheetSpritesByPath.TryGetValue(sheetPath, out var cached))
                return cached;

            var result = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            Sprite[] sprites = Resources.LoadAll<Sprite>(sheetPath);
            if (sprites != null)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite candidate = sprites[i];
                    if (candidate == null || string.IsNullOrWhiteSpace(candidate.name))
                        continue;

                    result[candidate.name] = candidate;
                }
            }

            _resourceSheetSpritesByPath[sheetPath] = result;
            return result;
        }

        private static bool TryParseResourceSheetKey(
            string spriteKey,
            out string sheetPath,
            out string subSpriteName)
        {
            sheetPath = string.Empty;
            subSpriteName = string.Empty;

            if (string.IsNullOrWhiteSpace(spriteKey))
                return false;

            int separatorIndex = spriteKey.IndexOf('#');
            if (separatorIndex <= 0 || separatorIndex >= spriteKey.Length - 1)
                return false;

            sheetPath = spriteKey.Substring(0, separatorIndex).Trim();
            subSpriteName = spriteKey.Substring(separatorIndex + 1).Trim();
            return !string.IsNullOrWhiteSpace(sheetPath)
                   && !string.IsNullOrWhiteSpace(subSpriteName);
        }

        private static string ExtractResourceName(string spriteKey)
        {
            if (string.IsNullOrWhiteSpace(spriteKey))
                return string.Empty;

            int slashIndex = spriteKey.LastIndexOf('/');
            return slashIndex >= 0 && slashIndex < spriteKey.Length - 1
                ? spriteKey.Substring(slashIndex + 1)
                : spriteKey;
        }

        private Sprite ResolveFallback(ArcGraphRenderItemKind kind)
        {
            if (kind == ArcGraphRenderItemKind.Actor)
                return defaultActorSprite;

            if (kind == ArcGraphRenderItemKind.Object)
                return defaultObjectSprite;

            return null;
        }

        private static string CreateTypedKey(
            ArcGraphRenderItemKind kind,
            string spriteKey)
        {
            return kind + "|" + (spriteKey ?? string.Empty);
        }
    }
}
