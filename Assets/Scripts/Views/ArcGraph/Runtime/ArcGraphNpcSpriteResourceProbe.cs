using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphNpcSpriteResourceProbe
    // =============================================================================
    /// <summary>
    /// <para>
    /// Probe scene-side per verificare che le sprite key del catalogo NPC siano
    /// risolvibili dal resolver configurato.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test asset fuori dal renderer</b></para>
    /// <para>
    /// Il renderer NPC non deve trasformarsi in strumento di audit asset. Questo
    /// componente resta separato: legge un <see cref="TextAsset"/> catalogo gia'
    /// assegnato, usa un <see cref="IArcGraphSpriteResolver"/> scene-side e conta
    /// quali sprite key vengono risolte. Non crea GameObject, non disegna, non legge
    /// il World e non modifica ArcGraph.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>npcVisualCatalogJson</b>: catalogo NPC da verificare.</item>
    ///   <item><b>spriteResolverBehaviour</b>: componente che implementa il resolver sprite.</item>
    ///   <item><b>ProbeNpcSpriteResources</b>: entry point manuale e programmabile.</item>
    ///   <item><b>LastDiagnostics</b>: ultimo risultato del probe.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphNpcSpriteResourceProbe : MonoBehaviour
    {
        [SerializeField] private TextAsset npcVisualCatalogJson;
        [SerializeField] private MonoBehaviour spriteResolverBehaviour;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private int sampleEntityId = -1;

        private ArcGraphNpcSpriteResourceProbeDiagnostics _lastDiagnostics;

        public ArcGraphNpcSpriteResourceProbeDiagnostics LastDiagnostics => _lastDiagnostics;

        // =============================================================================
        // SetNpcVisualCatalogJson
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il catalogo NPC da verificare.
        /// </para>
        /// </summary>
        public void SetNpcVisualCatalogJson(TextAsset catalogJson)
        {
            npcVisualCatalogJson = catalogJson;
        }

        // =============================================================================
        // SetSpriteResolverBehaviour
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il componente resolver sprite da usare per il probe.
        /// </para>
        /// </summary>
        public void SetSpriteResolverBehaviour(MonoBehaviour resolverBehaviour)
        {
            spriteResolverBehaviour = resolverBehaviour;
        }

        // =============================================================================
        // ProbeNpcSpriteResourcesFromContextMenu
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue il probe da menu contestuale Unity.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Probe NPC Sprite Resources")]
        public void ProbeNpcSpriteResourcesFromContextMenu()
        {
            ProbeNpcSpriteResources();
        }

        // =============================================================================
        // ProbeNpcSpriteResources
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica tutte le sprite key dichiarate dal catalogo NPC.
        /// </para>
        ///
        /// <para><b>Uso previsto</b></para>
        /// <para>
        /// Prima del gate visuale, l'operatore puo' importare i PNG sotto
        /// <c>Assets/Resources/ArcGraph/NPC/...</c>, assegnare catalogo e resolver,
        /// poi lanciare questo probe. Se il numero di sprite mancanti e' zero, i
        /// path asset sono coerenti con il catalogo.
        /// </para>
        /// </summary>
        public ArcGraphNpcSpriteResourceProbeDiagnostics ProbeNpcSpriteResources()
        {
            bool hasCatalogJson = npcVisualCatalogJson != null;
            if (!hasCatalogJson)
                return StoreAndLogDiagnostics(CreateDiagnostics(false, false, HasResolver(), 0, 0, 0, 0, 0, string.Empty, "CatalogJsonMissing"));

            if (!ArcGraphNpcVisualCatalogJson.TryParse(npcVisualCatalogJson.text, out ArcGraphNpcVisualCatalog catalog)
                || catalog == null)
            {
                return StoreAndLogDiagnostics(CreateDiagnostics(true, false, HasResolver(), 0, 0, 0, 0, 0, string.Empty, "CatalogParseFailed"));
            }

            IArcGraphSpriteResolver resolver = ResolveSpriteResolver();
            bool hasResolver = resolver != null;
            if (!hasResolver)
                return StoreAndLogDiagnostics(CreateDiagnostics(true, true, false, catalog.FrameCount, 0, 0, 0, 0, string.Empty, "ResolverMissing"));

            int checkedSpriteKeys = 0;
            int emptySpriteKeys = 0;
            int resolvedSprites = 0;
            int missingSprites = 0;
            string firstMissingSpriteKey = string.Empty;

            for (int i = 0; i < catalog.Frames.Count; i++)
            {
                ArcGraphNpcVisualFrame frame = catalog.Frames[i];

                if (string.IsNullOrWhiteSpace(frame.SpriteKey))
                {
                    emptySpriteKeys++;
                    continue;
                }

                checkedSpriteKeys++;

                var request = new ArcGraphSpriteResolveRequest(
                    ArcGraphRenderItemKind.Actor,
                    sampleEntityId,
                    frame.SpriteKey,
                    frame.PartKey,
                    usesSimplifiedRepresentation: false);

                if (resolver.TryResolveSprite(request, out Sprite sprite) && sprite != null)
                {
                    resolvedSprites++;
                    continue;
                }

                missingSprites++;
                if (string.IsNullOrEmpty(firstMissingSpriteKey))
                    firstMissingSpriteKey = frame.SpriteKey;
            }

            string reason = missingSprites == 0 && emptySpriteKeys == 0
                ? "NpcSpriteResourcesReady"
                : "NpcSpriteResourcesIncomplete";

            return StoreAndLogDiagnostics(CreateDiagnostics(
                true,
                true,
                true,
                catalog.FrameCount,
                checkedSpriteKeys,
                emptySpriteKeys,
                resolvedSprites,
                missingSprites,
                firstMissingSpriteKey,
                reason));
        }

        private IArcGraphSpriteResolver ResolveSpriteResolver()
        {
            return spriteResolverBehaviour as IArcGraphSpriteResolver;
        }

        private bool HasResolver()
        {
            return ResolveSpriteResolver() != null;
        }

        private ArcGraphNpcSpriteResourceProbeDiagnostics CreateDiagnostics(
            bool hasCatalogJson,
            bool catalogParsed,
            bool hasResolver,
            int catalogFrameCount,
            int checkedSpriteKeyCount,
            int emptySpriteKeyCount,
            int resolvedSpriteCount,
            int missingSpriteCount,
            string firstMissingSpriteKey,
            string reason)
        {
            return new ArcGraphNpcSpriteResourceProbeDiagnostics(
                hasCatalogJson,
                catalogParsed,
                hasResolver,
                catalogFrameCount,
                checkedSpriteKeyCount,
                emptySpriteKeyCount,
                resolvedSpriteCount,
                missingSpriteCount,
                firstMissingSpriteKey,
                reason);
        }

        private ArcGraphNpcSpriteResourceProbeDiagnostics StoreAndLogDiagnostics(
            ArcGraphNpcSpriteResourceProbeDiagnostics diagnostics)
        {
            _lastDiagnostics = diagnostics;

            if (logDiagnostics)
            {
                Debug.Log(
                    "[ArcGraphNpcSpriteResourceProbe] " +
                    "reason=" + diagnostics.Reason +
                    ", catalogJson=" + diagnostics.HasCatalogJson +
                    ", catalogParsed=" + diagnostics.CatalogParsed +
                    ", resolver=" + diagnostics.HasResolver +
                    ", catalogFrames=" + diagnostics.CatalogFrameCount +
                    ", checkedSpriteKeys=" + diagnostics.CheckedSpriteKeyCount +
                    ", emptySpriteKeys=" + diagnostics.EmptySpriteKeyCount +
                    ", resolvedSprites=" + diagnostics.ResolvedSpriteCount +
                    ", missingSprites=" + diagnostics.MissingSpriteCount +
                    ", firstMissing='" + diagnostics.FirstMissingSpriteKey + "'",
                    this);
            }

            return _lastDiagnostics;
        }
    }
}
