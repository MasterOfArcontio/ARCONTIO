using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcSpawnPreviewSceneView
    // =============================================================================
    /// <summary>
    /// <para>
    /// Scene view minimale che disegna la preview semitrasparente dello spawn NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: rendering preview separato dalla request</b></para>
    /// <para>
    /// La preview legge la sorgente UI e il catalogo visuale NPC ArcGraph, poi
    /// compone le stesse parti sprite usate dal renderer runtime. Non legge il
    /// <c>World</c>, non esegue spawn, non valida celle e non invia comandi.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>previewSource</b>: sorgente UI di visuale, facing e cella.</item>
    ///   <item><b>spriteResolverBehaviour</b>: resolver scene-side gia' usato da ArcGraph.</item>
    ///   <item><b>npcVisualCatalogJson</b>: catalogo parti NPC in Resources.</item>
    ///   <item><b>_partRenderers</b>: pool piccolo di renderers per testa/braccia/corpo/gambe.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphNpcSpawnPreviewSceneView : MonoBehaviour
    {
        [SerializeField] private ArcGraphUiNpcSpawnPreviewSource previewSource;
        [SerializeField] private MonoBehaviour spriteResolverBehaviour;
        [SerializeField] private TextAsset npcVisualCatalogJson;
        [SerializeField] private Color previewColor = new Color(1f, 0.05f, 0.05f, 0.48f);
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float previewZOffset = -0.018f;
        [SerializeField] private float actorScale = 1f;
        [SerializeField] private Vector3 layeredSpriteLocalOffset = new Vector3(0f, 0.25f, 0f);
        [SerializeField] private int sortingOrder = 270;
        [SerializeField] private string runtimeRootName = "ArcGraphNpcSpawnPreview";

        private readonly List<SpriteRenderer> _partRenderers = new List<SpriteRenderer>(8);
        private GameObject _root;
        private SpriteRenderer _fallbackRenderer;
        private Sprite _fallbackSprite;
        private ArcGraphNpcVisualCatalog _catalog;
        private string _catalogSourceText;

        // =============================================================================
        // SetPreviewSource
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega la scene view alla sorgente preview NPC.
        /// </para>
        /// </summary>
        public void SetPreviewSource(ArcGraphUiNpcSpawnPreviewSource source)
        {
            previewSource = source;
        }

        // =============================================================================
        // SetSpriteResolverBehaviour
        // =============================================================================
        /// <summary>
        /// <para>
        /// Collega il resolver sprite ArcGraph gia' presente in scena.
        /// </para>
        /// </summary>
        public void SetSpriteResolverBehaviour(MonoBehaviour resolverBehaviour)
        {
            spriteResolverBehaviour = resolverBehaviour;
        }

        // =============================================================================
        // SetNpcVisualCatalogJson
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il JSON catalogo usato per comporre le parti NPC.
        /// </para>
        /// </summary>
        public void SetNpcVisualCatalogJson(TextAsset catalogJson)
        {
            npcVisualCatalogJson = catalogJson;
            _catalog = null;
            _catalogSourceText = null;
        }

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna posizione e parti sprite della preview durante il movimento del
        /// puntatore.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (previewSource == null
                || !previewSource.IsNpcPreviewActive
                || !previewSource.TryGetNpcPreviewCell(out int cellX, out int cellY))
            {
                HidePreview();
                return;
            }

            EnsureRoot();
            _root.transform.localPosition = originOffset + new Vector3(
                (cellX + 0.5f) * ResolveTileWorldSize(),
                (cellY + 0.5f) * ResolveTileWorldSize(),
                previewZOffset);
            _root.transform.localScale = Vector3.one * ResolvePositive(actorScale);

            bool drewAnyPart = DrawLayeredNpcPreview(previewSource.VisualKey, previewSource.Facing);
            DrawFallbackIfNeeded(drewAnyPart);
            _root.SetActive(true);
        }

        private bool DrawLayeredNpcPreview(
            string visualKey,
            ArcUiNpcSpawnFacing facing)
        {
            ArcGraphNpcVisualCatalog catalog = GetOrParseCatalog();
            IArcGraphSpriteResolver resolver = spriteResolverBehaviour as IArcGraphSpriteResolver;
            if (catalog == null || resolver == null)
                return false;

            string direction = ArcGraphUiNpcSpawnPreviewSource.ToDirectionKey(facing);
            bool drewAnyPart = false;
            EnsurePartRendererCount(catalog.PartCount);

            for (int i = 0; i < _partRenderers.Count; i++)
                _partRenderers[i].enabled = false;

            for (int i = 0; i < catalog.Parts.Count; i++)
            {
                string partKey = catalog.Parts[i];
                if (!catalog.TryResolveFrame(
                        visualKey,
                        partKey,
                        direction,
                        catalog.DefaultAnimationKey,
                        0,
                        out ArcGraphNpcVisualFrame frame))
                {
                    continue;
                }

                var request = new ArcGraphSpriteResolveRequest(
                    ArcGraphRenderItemKind.Actor,
                    entityId: -1,
                    frame.SpriteKey,
                    frame.PartKey);
                if (!resolver.TryResolveSprite(request, out Sprite sprite) || sprite == null)
                    continue;

                SpriteRenderer renderer = _partRenderers[i];
                renderer.sprite = sprite;
                renderer.color = previewColor;
                renderer.sortingOrder = sortingOrder + frame.SortingOffset;
                renderer.transform.localPosition = layeredSpriteLocalOffset;
                renderer.transform.localScale = ResolveLayeredSpriteScale(sprite, frame, catalog);
                renderer.enabled = true;
                drewAnyPart = true;
            }

            return drewAnyPart;
        }

        private void DrawFallbackIfNeeded(bool drewAnyPart)
        {
            if (_fallbackRenderer == null)
                _fallbackRenderer = CreateRenderer("Fallback");

            if (drewAnyPart)
            {
                _fallbackRenderer.enabled = false;
                return;
            }

            _fallbackRenderer.sprite = GetOrCreateFallbackSprite();
            _fallbackRenderer.color = previewColor;
            _fallbackRenderer.sortingOrder = sortingOrder;
            _fallbackRenderer.transform.localPosition = layeredSpriteLocalOffset;
            _fallbackRenderer.transform.localScale = new Vector3(0.55f, 1.05f, 1f);
            _fallbackRenderer.enabled = true;
        }

        private void EnsureRoot()
        {
            if (_root != null)
                return;

            _root = new GameObject(string.IsNullOrWhiteSpace(runtimeRootName)
                ? "ArcGraphNpcSpawnPreview"
                : runtimeRootName);
            _root.transform.SetParent(transform, false);
            _root.SetActive(false);
        }

        private void EnsurePartRendererCount(int requiredCount)
        {
            EnsureRoot();
            while (_partRenderers.Count < requiredCount)
                _partRenderers.Add(CreateRenderer("Part_" + _partRenderers.Count));
        }

        private SpriteRenderer CreateRenderer(string suffix)
        {
            EnsureRoot();
            var go = new GameObject("ArcGraphNpcSpawnPreview_" + suffix);
            go.transform.SetParent(_root.transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            return renderer;
        }

        private ArcGraphNpcVisualCatalog GetOrParseCatalog()
        {
            string json = npcVisualCatalogJson != null ? npcVisualCatalogJson.text : null;
            if (_catalog != null && _catalogSourceText == json)
                return _catalog;

            _catalogSourceText = json;
            _catalog = ArcGraphNpcVisualCatalogJson.ParseOrDefault(json);
            return _catalog;
        }

        private Sprite GetOrCreateFallbackSprite()
        {
            if (_fallbackSprite != null)
                return _fallbackSprite;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _fallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            return _fallbackSprite;
        }

        private void HidePreview()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        private float ResolveTileWorldSize()
        {
            return tileWorldSize > 0f ? tileWorldSize : 1f;
        }

        private static float ResolvePositive(float value)
        {
            return value > 0f ? value : 1f;
        }

        private static Vector3 ResolveLayeredSpriteScale(
            Sprite sprite,
            ArcGraphNpcVisualFrame frame,
            ArcGraphNpcVisualCatalog catalog)
        {
            if (sprite == null || catalog == null)
                return Vector3.one;

            float desiredPpu = catalog.PixelsPerUnit > 0 ? catalog.PixelsPerUnit : 32f;
            float actualPpu = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : desiredPpu;
            float expectedWidth = frame.FrameWidthPixels > 0 ? frame.FrameWidthPixels : catalog.FrameWidthPixels;
            float expectedHeight = frame.FrameHeightPixels > 0 ? frame.FrameHeightPixels : catalog.FrameHeightPixels;
            float actualWidth = sprite.rect.width > 0f ? sprite.rect.width : expectedWidth;
            float actualHeight = sprite.rect.height > 0f ? sprite.rect.height : expectedHeight;

            return new Vector3(
                (expectedWidth / desiredPpu) / (actualWidth / actualPpu),
                (expectedHeight / desiredPpu) / (actualHeight / actualPpu),
                1f);
        }

        private void OnDestroy()
        {
            if (_fallbackSprite != null && _fallbackSprite.texture != null)
                Destroy(_fallbackSprite.texture);
        }
    }
}
