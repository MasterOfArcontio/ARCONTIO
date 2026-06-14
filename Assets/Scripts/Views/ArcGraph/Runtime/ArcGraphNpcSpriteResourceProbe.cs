using System.Collections.Generic;
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
        // CatalogSlotStats
        // =============================================================================
        /// <summary>
        /// <para>
        /// Statistiche interne di una combinazione parte/direzione/animazione.
        /// </para>
        ///
        /// <para><b>Principio architetturale: diagnostica asset prima del gate visuale</b></para>
        /// <para>
        /// Il gate NPC non deve limitarsi a dire "manca una sprite". Per capire se
        /// il catalogo e' coerente dobbiamo sapere anche se ogni animazione possiede
        /// frame continui. Questa struttura resta privata al probe e non entra nel
        /// runtime produttivo.
        /// </para>
        /// </summary>
        private sealed class CatalogSlotStats
        {
            public string PartKey;
            public string DirectionKey;
            public string AnimationKey;
            public int FrameCount;
            public int MaxFrameIndex = -1;
            public readonly HashSet<int> FrameIndices = new();
        }

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
            string firstMissingPartKey = string.Empty;
            string firstMissingDirectionKey = string.Empty;
            string firstMissingAnimationKey = string.Empty;

            var parts = new HashSet<string>();
            var directions = new HashSet<string>();
            var animations = new HashSet<string>();
            var slotsByKey = new Dictionary<string, CatalogSlotStats>();

            for (int i = 0; i < catalog.Frames.Count; i++)
            {
                ArcGraphNpcVisualFrame frame = catalog.Frames[i];

                parts.Add(frame.PartKey);
                directions.Add(frame.DirectionKey);
                animations.Add(frame.AnimationKey);
                RegisterCatalogSlot(slotsByKey, frame);

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
                {
                    firstMissingSpriteKey = frame.SpriteKey;
                    firstMissingPartKey = frame.PartKey;
                    firstMissingDirectionKey = frame.DirectionKey;
                    firstMissingAnimationKey = frame.AnimationKey;
                }
            }

            AnalyzeCatalogSlots(
                slotsByKey,
                out int incompleteCatalogSlotCount,
                out string firstIncompleteCatalogSlot,
                out string catalogCoverageSummary);

            string reason = missingSprites == 0 && emptySpriteKeys == 0 && incompleteCatalogSlotCount == 0
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
                reason,
                firstMissingPartKey,
                firstMissingDirectionKey,
                firstMissingAnimationKey,
                parts.Count,
                animations.Count,
                directions.Count,
                slotsByKey.Count,
                incompleteCatalogSlotCount,
                firstIncompleteCatalogSlot,
                catalogCoverageSummary));
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
            string reason,
            string firstMissingPartKey = "",
            string firstMissingDirectionKey = "",
            string firstMissingAnimationKey = "",
            int catalogPartCount = 0,
            int catalogAnimationCount = 0,
            int catalogDirectionCount = 0,
            int catalogSlotCount = 0,
            int incompleteCatalogSlotCount = 0,
            string firstIncompleteCatalogSlot = "",
            string catalogCoverageSummary = "")
        {
            return new ArcGraphNpcSpriteResourceProbeDiagnostics(
                hasCatalogJson,
                catalogParsed,
                hasResolver,
                catalogFrameCount,
                catalogPartCount,
                catalogAnimationCount,
                catalogDirectionCount,
                catalogSlotCount,
                incompleteCatalogSlotCount,
                checkedSpriteKeyCount,
                emptySpriteKeyCount,
                resolvedSpriteCount,
                missingSpriteCount,
                firstMissingSpriteKey,
                firstMissingPartKey,
                firstMissingDirectionKey,
                firstMissingAnimationKey,
                firstIncompleteCatalogSlot,
                catalogCoverageSummary,
                reason);
        }

        private static void RegisterCatalogSlot(
            Dictionary<string, CatalogSlotStats> slotsByKey,
            ArcGraphNpcVisualFrame frame)
        {
            if (slotsByKey == null)
                return;

            string key = CreateCatalogSlotKey(frame.PartKey, frame.DirectionKey, frame.AnimationKey);
            if (!slotsByKey.TryGetValue(key, out CatalogSlotStats stats) || stats == null)
            {
                // Ogni slot rappresenta una singola sequenza animata, per esempio:
                // body/south/walk. Se poi manca walk_03, il probe lo segnala come
                // slot incompleto prima ancora di guardare il risultato visivo.
                stats = new CatalogSlotStats
                {
                    PartKey = frame.PartKey,
                    DirectionKey = frame.DirectionKey,
                    AnimationKey = frame.AnimationKey
                };
                slotsByKey[key] = stats;
            }

            stats.FrameCount++;
            stats.FrameIndices.Add(frame.FrameIndex);
            if (frame.FrameIndex > stats.MaxFrameIndex)
                stats.MaxFrameIndex = frame.FrameIndex;
        }

        private static void AnalyzeCatalogSlots(
            Dictionary<string, CatalogSlotStats> slotsByKey,
            out int incompleteCatalogSlotCount,
            out string firstIncompleteCatalogSlot,
            out string catalogCoverageSummary)
        {
            incompleteCatalogSlotCount = 0;
            firstIncompleteCatalogSlot = string.Empty;

            int idleSlotCount = 0;
            int walkSlotCount = 0;
            int minIdleFrames = int.MaxValue;
            int maxIdleFrames = 0;
            int minWalkFrames = int.MaxValue;
            int maxWalkFrames = 0;

            if (slotsByKey != null)
            {
                foreach (var pair in slotsByKey)
                {
                    CatalogSlotStats stats = pair.Value;
                    if (stats == null)
                        continue;

                    if (HasFrameIndexGap(stats))
                    {
                        incompleteCatalogSlotCount++;
                        if (string.IsNullOrEmpty(firstIncompleteCatalogSlot))
                            firstIncompleteCatalogSlot = FormatCatalogSlot(stats);
                    }

                    // Per il gate corrente ci interessano soprattutto idle e walk.
                    // La summary tiene separati i due range per capire subito se il
                    // catalogo sta usando idle a 4 frame e walk a 8 frame.
                    if (stats.AnimationKey == "idle")
                    {
                        idleSlotCount++;
                        minIdleFrames = stats.FrameCount < minIdleFrames ? stats.FrameCount : minIdleFrames;
                        maxIdleFrames = stats.FrameCount > maxIdleFrames ? stats.FrameCount : maxIdleFrames;
                    }
                    else if (stats.AnimationKey == "walk")
                    {
                        walkSlotCount++;
                        minWalkFrames = stats.FrameCount < minWalkFrames ? stats.FrameCount : minWalkFrames;
                        maxWalkFrames = stats.FrameCount > maxWalkFrames ? stats.FrameCount : maxWalkFrames;
                    }
                }
            }

            catalogCoverageSummary =
                "idleSlots=" + idleSlotCount +
                ", idleFrames=" + FormatFrameRange(minIdleFrames, maxIdleFrames) +
                ", walkSlots=" + walkSlotCount +
                ", walkFrames=" + FormatFrameRange(minWalkFrames, maxWalkFrames);
        }

        private static string CreateCatalogSlotKey(
            string partKey,
            string directionKey,
            string animationKey)
        {
            return (partKey ?? string.Empty) + "|" + (directionKey ?? string.Empty) + "|" + (animationKey ?? string.Empty);
        }

        private static string FormatCatalogSlot(CatalogSlotStats stats)
        {
            if (stats == null)
                return string.Empty;

            return stats.PartKey + "/" + stats.DirectionKey + "/" + stats.AnimationKey;
        }

        private static bool HasFrameIndexGap(CatalogSlotStats stats)
        {
            if (stats == null || stats.MaxFrameIndex < 0)
                return false;

            for (int i = 0; i <= stats.MaxFrameIndex; i++)
            {
                if (!stats.FrameIndices.Contains(i))
                    return true;
            }

            return false;
        }

        private static string FormatFrameRange(
            int minFrames,
            int maxFrames)
        {
            if (minFrames == int.MaxValue || maxFrames <= 0)
                return "none";

            return minFrames == maxFrames
                ? minFrames.ToString()
                : minFrames + "-" + maxFrames;
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
                    ", catalogParts=" + diagnostics.CatalogPartCount +
                    ", catalogAnimations=" + diagnostics.CatalogAnimationCount +
                    ", catalogDirections=" + diagnostics.CatalogDirectionCount +
                    ", catalogSlots=" + diagnostics.CatalogSlotCount +
                    ", incompleteCatalogSlots=" + diagnostics.IncompleteCatalogSlotCount +
                    ", checkedSpriteKeys=" + diagnostics.CheckedSpriteKeyCount +
                    ", emptySpriteKeys=" + diagnostics.EmptySpriteKeyCount +
                    ", resolvedSprites=" + diagnostics.ResolvedSpriteCount +
                    ", missingSprites=" + diagnostics.MissingSpriteCount +
                    ", firstMissing='" + diagnostics.FirstMissingSpriteKey + "'" +
                    ", firstMissingPart='" + diagnostics.FirstMissingPartKey + "'" +
                    ", firstMissingDirection='" + diagnostics.FirstMissingDirectionKey + "'" +
                    ", firstMissingAnimation='" + diagnostics.FirstMissingAnimationKey + "'" +
                    ", firstIncompleteSlot='" + diagnostics.FirstIncompleteCatalogSlot + "'" +
                    ", coverage='" + diagnostics.CatalogCoverageSummary + "'",
                    this);
            }

            return _lastDiagnostics;
        }
    }
}
