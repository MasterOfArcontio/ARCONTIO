using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphSceneProbeRenderer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Renderer debug temporaneo per visualizzare un <c>ArcGraphVisualProbeFrame</c>
    /// dentro una scena Unity.
    /// </para>
    ///
    /// <para><b>Principio architetturale: probe visivo, non renderer produttivo</b></para>
    /// <para>
    /// Questo componente crea oggetti runtime temporanei sotto un root dedicato.
    /// Non legge il <c>World</c>, non chiama <c>SimulationHost</c>, non carica asset,
    /// non modifica MapGrid e non deve essere considerato il renderer definitivo di
    /// ArcGraph. Serve solo a vedere a schermo il frame dati controllato prodotto
    /// dalla <c>v0.36.03v</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RenderDefaultProbe</b>: costruisce e disegna il frame finto default.</item>
    ///   <item><b>RenderFrame</b>: consuma un frame ArcGraph gia' pronto.</item>
    ///   <item><b>ClearProbe</b>: distrugge il root temporaneo.</item>
    ///   <item><b>CreateSprite</b>: crea sprite runtime colorati per i layer.</item>
    ///   <item><b>EnsureDebugSprite</b>: genera una texture 1x1 interna al probe.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphSceneProbeRenderer : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private bool renderDefaultProbeOnStart;
        [SerializeField] private bool clearBeforeRender = true;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private bool placeProbeAtSceneCameraCenter = true;
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private float terrainScale = 1f;
        [SerializeField] private float waterScale = 0.95f;
        [SerializeField] private float vegetationScale = 0.7f;
        [SerializeField] private float objectScale = 0.6f;
        [SerializeField] private float actorScale = 0.75f;
        [SerializeField] private float lightScale = 1f;

        private const string ProbeRootName = "ArcGraphSceneProbeRoot";
        private const int TerrainSortingOrder = 0;
        private const int WaterSortingOrder = 10;
        private const int VegetationSortingOrder = 20;
        private const int ObjectSortingOrder = 40;
        private const int ActorSortingOrder = 50;
        private const int LightSortingOrder = 100;

        private Transform _root;
        private Sprite _debugSprite;
        private Texture2D _debugTexture;

        private static readonly Color TerrainColor = new(0.42f, 0.42f, 0.42f, 1f);
        private static readonly Color TerrainVariantColor = new(0.50f, 0.50f, 0.50f, 1f);
        private static readonly Color WaterColor = new(0.10f, 0.35f, 1f, 0.75f);
        private static readonly Color VegetationColor = new(0.12f, 0.70f, 0.18f, 0.90f);
        private static readonly Color ObjectColor = new(1f, 0.55f, 0.12f, 1f);
        private static readonly Color ActorColor = new(0.95f, 0.15f, 0.95f, 1f);
        private static readonly Color LocalLightColor = new(1f, 0.88f, 0.20f, 0.38f);
        private static readonly Color DarkLightColor = new(0f, 0f, 0f, 0.45f);
        private static readonly Color DimLightColor = new(0.15f, 0.18f, 0.35f, 0.30f);

        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Avvia opzionalmente il probe default.
        /// </para>
        ///
        /// <para><b>Attivazione esplicita</b></para>
        /// <para>
        /// Il flag serializzato resta false di default. Il probe non deve accendersi
        /// automaticamente in una scena produttiva: l'operatore deve abilitarlo
        /// volontariamente oppure usare il context menu.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (!renderDefaultProbeOnStart)
                return;

            RenderDefaultProbe();
        }

        // =============================================================================
        // RenderDefaultProbe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce e renderizza il frame default del Visual Probe.
        /// </para>
        ///
        /// <para><b>Uso previsto</b></para>
        /// <para>
        /// Questo metodo e' il primo comando pratico di test: genera la mini scena
        /// dati 4x4 gia' validata dall'harness e la disegna con primitive sprite
        /// colorate. Non usa asset definitivi.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Render Default Probe")]
        public void RenderDefaultProbe()
        {
            EnsureDebugSprite();

            bool hasCamera = sceneCamera != null;
            bool hasDrawableResource = _debugSprite != null;

            ArcGraphVisualProbeFrame frame = ArcGraphVisualProbeHarness.CreateDefaultProbeFrame(
                hasLegacyRenderer: true,
                hasCamera,
                hasDrawableResource,
                requestSceneProbe: true);

            RenderFrame(frame);
        }

        // =============================================================================
        // RenderFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna un frame ArcGraph gia' costruito.
        /// </para>
        ///
        /// <para><b>Gate prima della scena</b></para>
        /// <para>
        /// Se il gate comparativo non consente il probe scena temporaneo, il metodo
        /// non crea oggetti. Questo evita che il renderer debug aggiri le policy
        /// impostate in <c>ArcGraphComparisonGate</c>.
        /// </para>
        /// </summary>
        public void RenderFrame(ArcGraphVisualProbeFrame frame)
        {
            if (frame == null)
            {
                LogWarning("FrameMissing");
                return;
            }

            if (!frame.ComparisonDiagnostics.CanAttachSceneProbe)
            {
                LogWarning("SceneProbeBlocked: " + frame.ComparisonDiagnostics.Reason);
                return;
            }

            if (clearBeforeRender)
                ClearProbe();

            EnsureRoot();
            EnsureDebugSprite();

            RenderTerrain(frame);
            RenderWater(frame);
            RenderVegetation(frame);
            RenderActorObjectQueue(frame);
            RenderLight(frame);

            if (logDiagnostics)
            {
                Debug.Log(
                    "[ArcGraphSceneProbe] Rendered probe frame. " +
                    "terrainCells=" + frame.Diagnostics.TerrainCellCount +
                    ", actorObjectEntries=" + frame.Diagnostics.ActorObjectEntryCount +
                    ", vegetation=" + frame.Diagnostics.VegetationItemCount +
                    ", water=" + frame.Diagnostics.WaterItemCount +
                    ", light=" + frame.Diagnostics.LightItemCount);
            }
        }

        // =============================================================================
        // ClearProbe
        // =============================================================================
        /// <summary>
        /// <para>
        /// Distrugge tutti gli oggetti temporanei creati dal probe.
        /// </para>
        ///
        /// <para><b>Cleanup confinato</b></para>
        /// <para>
        /// Il cleanup agisce solo sul root dedicato del probe. Non cerca altri
        /// renderer nella scena e non modifica MapGrid.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Clear Probe")]
        public void ClearProbe()
        {
            if (_root == null)
                _root = FindExistingRoot();

            if (_root == null)
                return;

            DestroyProbeObject(_root.gameObject);
            _root = null;
        }

        private void RenderTerrain(ArcGraphVisualProbeFrame frame)
        {
            int spriteIndex = 0;

            for (int chunkIndex = 0; chunkIndex < frame.TerrainChunks.Length; chunkIndex++)
            {
                ArcGraphTerrainChunkMeshData chunk = frame.TerrainChunks[chunkIndex];
                Vector3[] vertices = chunk.Vertices;

                for (int i = 0; i + 3 < vertices.Length; i += 4)
                {
                    Vector3 center = AverageQuad(vertices[i], vertices[i + 1], vertices[i + 2], vertices[i + 3]);
                    Color color = (spriteIndex % 2 == 0) ? TerrainColor : TerrainVariantColor;

                    CreateSprite(
                        "Terrain_" + spriteIndex,
                        center,
                        terrainScale,
                        color,
                        TerrainSortingOrder);

                    spriteIndex++;
                }
            }
        }

        private void RenderWater(ArcGraphVisualProbeFrame frame)
        {
            for (int i = 0; i < frame.WaterItems.Length; i++)
            {
                ArcGraphWaterRenderItem item = frame.WaterItems[i];
                if (!item.IsVisible)
                    continue;

                CreateSprite(
                    "Water_" + i,
                    CellCenter(item.Cell),
                    waterScale,
                    WaterColor,
                    WaterSortingOrder);
            }
        }

        private void RenderVegetation(ArcGraphVisualProbeFrame frame)
        {
            for (int i = 0; i < frame.VegetationItems.Length; i++)
            {
                ArcGraphVegetationRenderItem item = frame.VegetationItems[i];
                if (!item.IsVisible)
                    continue;

                CreateSprite(
                    "Vegetation_" + i,
                    CellCenter(item.Cell),
                    vegetationScale,
                    VegetationColor,
                    VegetationSortingOrder);
            }
        }

        private void RenderActorObjectQueue(ArcGraphVisualProbeFrame frame)
        {
            ArcGraphRenderQueue queue = frame.ActorObjectQueue;
            if (queue == null)
                return;

            for (int i = 0; i < queue.Entries.Count; i++)
            {
                ArcGraphRenderQueueEntry entry = queue.Entries[i];
                if (entry.Kind == ArcGraphRenderItemKind.Actor)
                    RenderActor(queue, entry);
                else if (entry.Kind == ArcGraphRenderItemKind.Object)
                    RenderObject(queue, entry);
            }
        }

        private void RenderActor(
            ArcGraphRenderQueue queue,
            ArcGraphRenderQueueEntry entry)
        {
            if (entry.ItemIndex < 0 || entry.ItemIndex >= queue.ActorItems.Count)
                return;

            ArcGraphActorRenderItem item = queue.ActorItems[entry.ItemIndex];
            if (!item.IsVisible)
                return;

            CreateSprite(
                "Actor_" + item.ActorId,
                new Vector3(item.VisualX + 0.5f, item.VisualY + 0.5f, 0f),
                actorScale,
                ActorColor,
                ActorSortingOrder);
        }

        private void RenderObject(
            ArcGraphRenderQueue queue,
            ArcGraphRenderQueueEntry entry)
        {
            if (entry.ItemIndex < 0 || entry.ItemIndex >= queue.ObjectItems.Count)
                return;

            ArcGraphObjectRenderItem item = queue.ObjectItems[entry.ItemIndex];
            if (!item.IsVisible)
                return;

            CreateSprite(
                "Object_" + item.ObjectId,
                CellCenter(item.Cell),
                objectScale,
                ObjectColor,
                ObjectSortingOrder);
        }

        private void RenderLight(ArcGraphVisualProbeFrame frame)
        {
            for (int i = 0; i < frame.LightItems.Length; i++)
            {
                ArcGraphLightRenderItem item = frame.LightItems[i];
                if (!item.IsVisible)
                    continue;

                CreateSprite(
                    "Light_" + i,
                    CellCenter(item.Cell),
                    lightScale,
                    ResolveLightColor(item),
                    LightSortingOrder);
            }
        }

        private SpriteRenderer CreateSprite(
            string name,
            Vector3 localCellPosition,
            float scale,
            Color color,
            int sortingOrder)
        {
            EnsureRoot();
            EnsureDebugSprite();

            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = ResolveWorldPosition(localCellPosition);
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale * tileWorldSize);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _debugSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return renderer;
        }

        private void EnsureRoot()
        {
            if (_root != null)
                return;

            _root = FindExistingRoot();
            if (_root != null)
                return;

            var go = new GameObject(ProbeRootName);
            go.transform.SetParent(transform, false);
            _root = go.transform;
        }

        private Transform FindExistingRoot()
        {
            Transform existing = transform.Find(ProbeRootName);
            return existing;
        }

        private void EnsureDebugSprite()
        {
            if (_debugSprite != null)
                return;

            _debugTexture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            _debugTexture.name = "ArcGraphSceneProbePixel";
            _debugTexture.SetPixel(0, 0, Color.white);
            _debugTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            _debugSprite = Sprite.Create(
                _debugTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
            _debugSprite.name = "ArcGraphSceneProbeSprite";
        }

        private Vector3 CellCenter(ArcGraphCellCoord cell)
        {
            return new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z);
        }

        private Vector3 ResolveWorldPosition(Vector3 localCellPosition)
        {
            Vector3 baseOffset = originOffset;

            if (placeProbeAtSceneCameraCenter && sceneCamera != null)
            {
                Vector3 cameraPosition = sceneCamera.transform.position;
                baseOffset += new Vector3(cameraPosition.x, cameraPosition.y, 0f);
                baseOffset -= new Vector3(2f * tileWorldSize, 2f * tileWorldSize, 0f);
            }

            return baseOffset + (localCellPosition * tileWorldSize);
        }

        private static Vector3 AverageQuad(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            return (a + b + c + d) * 0.25f;
        }

        private static Color ResolveLightColor(ArcGraphLightRenderItem item)
        {
            if (item.HasLocalSource)
                return LocalLightColor;

            if (item.Intensity01 <= 0.25f)
                return DarkLightColor;

            return DimLightColor;
        }

        private void LogWarning(string reason)
        {
            if (!logDiagnostics)
                return;

            Debug.LogWarning("[ArcGraphSceneProbe] " + reason);
        }

        private static void DestroyProbeObject(Object unityObject)
        {
            if (unityObject == null)
                return;

            if (Application.isPlaying)
                Destroy(unityObject);
            else
                DestroyImmediate(unityObject);
        }

        private void OnDestroy()
        {
            ClearProbe();

            if (_debugSprite != null)
            {
                DestroyProbeObject(_debugSprite);
                _debugSprite = null;
            }

            if (_debugTexture != null)
            {
                DestroyProbeObject(_debugTexture);
                _debugTexture = null;
            }
        }
    }
}
