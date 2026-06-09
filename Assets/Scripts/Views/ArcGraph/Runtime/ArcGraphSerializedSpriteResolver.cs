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
    /// <para><b>Principio architetturale: asset espliciti, non caricati globalmente</b></para>
    /// <para>
    /// Il primo probe actor/object non deve chiamare <c>Resources.Load</c>. Questa
    /// entry permette invece di assegnare manualmente gli asset al resolver scena,
    /// mantenendo il caricamento fuori dai builder passivi ArcGraph.
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
    /// Resolver sprite scene-side basato su mapping serializzati da Inspector.
    /// </para>
    ///
    /// <para><b>Principio architetturale: resolver temporaneo senza asset load</b></para>
    /// <para>
    /// Questo componente implementa <see cref="IArcGraphSpriteResolver"/> per il
    /// probe actor/object. Non legge <c>World</c>, non usa <c>Resources.Load</c>,
    /// non cerca asset nella scena e non modifica ArcGraph. Traduce soltanto
    /// richieste sprite in riferimenti assegnati esplicitamente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>entries</b>: mapping specifici sprite key -> sprite.</item>
    ///   <item><b>defaultActorSprite/defaultObjectSprite</b>: fallback dichiarati.</item>
    ///   <item><b>TryResolveSprite</b>: risoluzione richiesta dal probe.</item>
    ///   <item><b>BuildCacheIfNeeded</b>: cache locale delle entry serializzate.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphSerializedSpriteResolver : MonoBehaviour, IArcGraphSpriteResolver
    {
        [SerializeField] private List<ArcGraphSerializedSpriteResolverEntry> entries = new();
        [SerializeField] private Sprite defaultActorSprite;
        [SerializeField] private Sprite defaultObjectSprite;

        private readonly Dictionary<string, Sprite> _spritesByTypedKey = new();
        private bool _cacheDirty = true;

        // =============================================================================
        // TryResolveSprite
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a risolvere una sprite key usando mapping e fallback serializzati.
        /// </para>
        ///
        /// <para><b>Risoluzione confinata</b></para>
        /// <para>
        /// Il metodo non effettua caricamenti dinamici. Se la chiave non e' mappata,
        /// usa il fallback actor/object assegnato dall'Inspector. Se anche il
        /// fallback manca, restituisce <c>false</c> e lascia al probe la diagnostica.
        /// </para>
        /// </summary>
        public bool TryResolveSprite(
            ArcGraphSpriteResolveRequest request,
            out Sprite sprite)
        {
            BuildCacheIfNeeded();

            string typedKey = CreateTypedKey(request.Kind, request.SpriteKey);
            if (_spritesByTypedKey.TryGetValue(typedKey, out sprite) && sprite != null)
                return true;

            sprite = ResolveFallback(request.Kind);
            return sprite != null;
        }

        private void OnValidate()
        {
            _cacheDirty = true;
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
