using System;
using Arcontio.Core;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphTerrainVisualCatalogJson
    // =============================================================================
    /// <summary>
    /// <para>
    /// Helper per convertire JSON visuale terrain in catalogo runtime.
    /// </para>
    ///
    /// <para><b>Principio architetturale: JSON come bordo di authoring</b></para>
    /// <para>
    /// Il JSON resta un formato comodo da modificare a mano. Il runtime non deve
    /// pero' lavorare direttamente sui DTO mutabili di <c>JsonUtility</c>. Questo
    /// helper converte i campi pubblici serializzabili in strutture runtime
    /// normalizzate, senza caricare asset, senza leggere Resources e senza toccare
    /// scena o simulazione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DefaultResourcePath</b>: path suggerita del file in Resources.</item>
    ///   <item><b>ParseOrDefault</b>: parsing con fallback minimo.</item>
    ///   <item><b>TryParse</b>: parsing esplicito senza log laterali.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphTerrainVisualCatalogJson
    {
        public const string DefaultResourcePath = "Arcontio/Config/surface_defs";

        // =============================================================================
        // ParseOrDefault
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte una stringa JSON in catalogo visuale, usando un fallback minimo
        /// se il JSON e' assente o non valido.
        /// </para>
        /// </summary>
        public static ArcGraphTerrainVisualCatalog ParseOrDefault(string json)
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
        /// Prova a convertire una stringa JSON in catalogo visuale terrain.
        /// </para>
        ///
        /// <para><b>Parsing puro</b></para>
        /// <para>
        /// Il metodo usa solo la stringa ricevuta. Non stampa log, non legge path e
        /// non conserva stato globale. La diagnostica resta responsabilita' del
        /// chiamante o di un futuro probe dedicato.
        /// </para>
        /// </summary>
        public static bool TryParse(
            string json,
            out ArcGraphTerrainVisualCatalog catalog)
        {
            catalog = null;

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var legacyDto = JsonUtility.FromJson<ArcGraphTerrainVisualCatalogDto>(json);
                if (legacyDto != null && legacyDto.HasAnyTerrainVisualData())
                {
                    catalog = legacyDto.ToRuntimeCatalog();
                    return catalog != null;
                }

                var surfaceDto = JsonUtility.FromJson<CellSurfaceDefDatabase>(json);
                if (surfaceDto == null || surfaceDto.Surfaces == null || surfaceDto.Surfaces.Count == 0)
                    return false;

                catalog = BuildCatalogFromSurfaceDefs(surfaceDto);
                return catalog != null;
            }
            catch
            {
                catalog = null;
                return false;
            }
        }

        private static ArcGraphTerrainVisualCatalog CreateFallbackCatalog()
        {
            return new ArcGraphTerrainVisualCatalog(
                new[]
                {
                    new ArcGraphTerrainVisualDefinition(
                        "unknown",
                        defaultTileId: 0,
                        new[] { new ArcGraphTerrainVisualVariant(0, 1) },
                        new ArcGraphTerrainVisualAnimation(null, 0f))
                },
                Array.Empty<ArcGraphTerrainVisualTransitionSet>());
        }

        // =============================================================================
        // BuildCatalogFromSurfaceDefs
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il catalogo visuale ArcGraph partendo dal catalogo Core delle
        /// superfici cella.
        /// </para>
        ///
        /// <para><b>Principio architetturale: Core autoritativo, View derivata</b></para>
        /// <para>
        /// ArcGraph non mantiene piu' un secondo catalogo semantico dei pavimenti:
        /// legge le definizioni gia' autorizzate in <c>surface_defs.json</c> e le
        /// converte nel formato runtime atteso dal renderer terrain. Il catalogo
        /// ArcGraph resta quindi un prodotto derivato, non una nuova fonte di verita'.
        /// </para>
        /// </summary>
        private static ArcGraphTerrainVisualCatalog BuildCatalogFromSurfaceDefs(CellSurfaceDefDatabase database)
        {
            var definitions = new System.Collections.Generic.List<ArcGraphTerrainVisualDefinition>();
            var overlays = new System.Collections.Generic.List<ArcGraphTerrainVisualDualGridOverlay>();

            for (int i = 0; i < database.Surfaces.Count; i++)
            {
                CellSurfaceDef surface = database.Surfaces[i];
                if (surface == null || surface.Visual == null)
                    continue;

                string terrainId = surface.ResolveVisualRuleKey();
                if (string.IsNullOrWhiteSpace(terrainId))
                    continue;

                definitions.Add(new ArcGraphTerrainVisualDefinition(
                    terrainId,
                    surface.Visual.DefaultTileId,
                    BuildVariants(surface.Visual.Variants, surface.Visual.DefaultTileId),
                    BuildAnimation(surface.Visual.Animation),
                    BuildDetails(surface.Visual.Details, surface.Visual.DefaultTileId),
                    surface.Visual.DetailChancePermille));

                if (surface.Visual.DualGridOverlay != null)
                {
                    overlays.Add(new ArcGraphTerrainVisualDualGridOverlay(
                        terrainId,
                        surface.Visual.DualGridOverlay.Priority,
                        BuildDualGridRules(surface.Visual.DualGridOverlay.Rules)));
                }
            }

            return new ArcGraphTerrainVisualCatalog(
                definitions.ToArray(),
                Array.Empty<ArcGraphTerrainVisualTransitionSet>(),
                overlays.ToArray());
        }

        // =============================================================================
        // BuildVariants
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte le varianti dichiarate nel catalogo superfici nelle varianti
        /// runtime usate dal resolver terrain.
        /// </para>
        /// </summary>
        private static ArcGraphTerrainVisualVariant[] BuildVariants(
            CellSurfaceVisualTileVariant[] variants,
            int defaultTileId)
        {
            if (variants == null || variants.Length == 0)
            {
                return new[]
                {
                    new ArcGraphTerrainVisualVariant(defaultTileId, 1)
                };
            }

            var result = new ArcGraphTerrainVisualVariant[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                CellSurfaceVisualTileVariant variant = variants[i];
                result[i] = variant != null
                    ? new ArcGraphTerrainVisualVariant(variant.TileId, variant.Weight)
                    : new ArcGraphTerrainVisualVariant(defaultTileId, 1);
            }

            return result;
        }

        // =============================================================================
        // BuildDetails
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte i dettagli opzionali della superficie in dettagli visuali ArcGraph.
        /// </para>
        /// </summary>
        private static ArcGraphTerrainVisualDetail[] BuildDetails(
            CellSurfaceVisualTileVariant[] details,
            int defaultTileId)
        {
            if (details == null || details.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualDetail>();

            var result = new ArcGraphTerrainVisualDetail[details.Length];
            for (int i = 0; i < details.Length; i++)
            {
                CellSurfaceVisualTileVariant detail = details[i];
                result[i] = detail != null
                    ? new ArcGraphTerrainVisualDetail(detail.TileId, detail.Weight)
                    : new ArcGraphTerrainVisualDetail(defaultTileId, 1);
            }

            return result;
        }

        // =============================================================================
        // BuildAnimation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la sezione animazione del catalogo superfici nella struttura
        /// runtime usata dal clock visuale ArcGraph.
        /// </para>
        /// </summary>
        private static ArcGraphTerrainVisualAnimation BuildAnimation(CellSurfaceVisualAnimationDef animation)
        {
            return animation != null
                ? new ArcGraphTerrainVisualAnimation(animation.FrameTileIds, animation.FrameSeconds)
                : new ArcGraphTerrainVisualAnimation(null, 0f);
        }

        // =============================================================================
        // BuildDualGridRules
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte le regole dual-grid dichiarate nel catalogo superfici nelle
        /// regole overlay usate dal renderer ArcGraph.
        /// </para>
        /// </summary>
        private static ArcGraphTerrainVisualDualGridRule[] BuildDualGridRules(
            CellSurfaceVisualDualGridRuleDef[] rules)
        {
            if (rules == null || rules.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualDualGridRule>();

            var result = new ArcGraphTerrainVisualDualGridRule[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                CellSurfaceVisualDualGridRuleDef rule = rules[i];
                result[i] = rule != null
                    ? new ArcGraphTerrainVisualDualGridRule(
                        rule.Mask,
                        rule.TileId,
                        new ArcGraphTerrainVisualAnimation(rule.FrameTileIds, rule.FrameSeconds))
                    : new ArcGraphTerrainVisualDualGridRule(
                        "0000",
                        0,
                        new ArcGraphTerrainVisualAnimation(null, 0f));
            }

            return result;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualCatalogDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO radice del catalogo visuale terrain.
    /// </para>
    ///
    /// <para><b>DTO mutabile solo per JsonUtility</b></para>
    /// <para>
    /// Unity <c>JsonUtility</c> richiede classi serializzabili con campi pubblici.
    /// Questi DTO non vengono usati come modello runtime: appena parsati vengono
    /// convertiti in <c>ArcGraphTerrainVisualCatalog</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>terrains</b>: definizioni terrain type -> varianti/animazioni.</item>
    ///   <item><b>transitions</b>: regole opzionali per bordi tra terrain type.</item>
    ///   <item><b>dualGridOverlays</b>: overlay 2x2 per terreni superiori.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainVisualCatalogDto
    {
        public ArcGraphTerrainVisualDefinitionDto[] terrains;
        public ArcGraphTerrainVisualTransitionSetDto[] transitions;
        public ArcGraphTerrainVisualDualGridOverlayDto[] dualGridOverlays;

        // =============================================================================
        // HasAnyTerrainVisualData
        // =============================================================================
        /// <summary>
        /// <para>
        /// Riconosce il vecchio formato dedicato ArcGraph senza confonderlo con il
        /// nuovo catalogo unificato delle superfici.
        /// </para>
        /// </summary>
        public bool HasAnyTerrainVisualData()
        {
            return terrains != null || transitions != null || dualGridOverlays != null;
        }

        // =============================================================================
        // ToRuntimeCatalog
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il DTO radice in catalogo runtime normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualCatalog ToRuntimeCatalog()
        {
            return new ArcGraphTerrainVisualCatalog(
                BuildDefinitions(),
                BuildTransitionSets(),
                BuildDualGridOverlays());
        }

        private ArcGraphTerrainVisualDefinition[] BuildDefinitions()
        {
            if (terrains == null || terrains.Length == 0)
            {
                return new[]
                {
                    new ArcGraphTerrainVisualDefinition(
                        "unknown",
                        defaultTileId: 0,
                        new[] { new ArcGraphTerrainVisualVariant(0, 1) },
                        new ArcGraphTerrainVisualAnimation(null, 0f))
                };
            }

            var result = new ArcGraphTerrainVisualDefinition[terrains.Length];
            for (int i = 0; i < terrains.Length; i++)
            {
                result[i] = terrains[i] != null
                    ? terrains[i].ToRuntimeDefinition()
                    : new ArcGraphTerrainVisualDefinition(
                        "unknown",
                        defaultTileId: 0,
                        new[] { new ArcGraphTerrainVisualVariant(0, 1) },
                        new ArcGraphTerrainVisualAnimation(null, 0f));
            }

            return result;
        }

        private ArcGraphTerrainVisualTransitionSet[] BuildTransitionSets()
        {
            if (transitions == null || transitions.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualTransitionSet>();

            var result = new ArcGraphTerrainVisualTransitionSet[transitions.Length];
            for (int i = 0; i < transitions.Length; i++)
            {
                result[i] = transitions[i] != null
                    ? transitions[i].ToRuntimeTransitionSet()
                    : new ArcGraphTerrainVisualTransitionSet(
                        "unknown",
                        "unknown",
                        Array.Empty<ArcGraphTerrainVisualTransitionRule>());
            }

            return result;
        }

        private ArcGraphTerrainVisualDualGridOverlay[] BuildDualGridOverlays()
        {
            if (dualGridOverlays == null || dualGridOverlays.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualDualGridOverlay>();

            var result = new ArcGraphTerrainVisualDualGridOverlay[dualGridOverlays.Length];
            for (int i = 0; i < dualGridOverlays.Length; i++)
            {
                result[i] = dualGridOverlays[i] != null
                    ? dualGridOverlays[i].ToRuntimeOverlay()
                    : new ArcGraphTerrainVisualDualGridOverlay(
                        "unknown",
                        0,
                        Array.Empty<ArcGraphTerrainVisualDualGridRule>());
            }

            return result;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualDefinitionDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO di un tipo terreno visuale.
    /// </para>
    ///
    /// <para><b>Separazione fra terreno logico e tile atlas</b></para>
    /// <para>
    /// Il campo <c>terrainId</c> identifica il tipo terreno, mentre <c>defaultTileId</c>
    /// e <c>variants</c> indicano i tile atlas candidati. Questo permette di dire
    /// "questa cella e' prato" senza fissare subito quale sprite di prato verra'
    /// disegnato.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainVisualDefinitionDto
    {
        public string terrainId;
        public int defaultTileId;
        public ArcGraphTerrainVisualVariantDto[] variants;
        public ArcGraphTerrainVisualAnimationDto animation;
        public int detailChancePermille = 0;
        public ArcGraphTerrainVisualDetailDto[] details;

        // =============================================================================
        // ToRuntimeDefinition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il terrain DTO in definizione runtime normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualDefinition ToRuntimeDefinition()
        {
            return new ArcGraphTerrainVisualDefinition(
                terrainId,
                defaultTileId,
                BuildVariants(),
                animation != null
                    ? animation.ToRuntimeAnimation()
                    : new ArcGraphTerrainVisualAnimation(null, 0f),
                BuildDetails(),
                detailChancePermille);
        }

        private ArcGraphTerrainVisualVariant[] BuildVariants()
        {
            if (variants == null || variants.Length == 0)
            {
                return new[]
                {
                    new ArcGraphTerrainVisualVariant(defaultTileId, 1)
                };
            }

            var result = new ArcGraphTerrainVisualVariant[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                result[i] = variants[i] != null
                    ? variants[i].ToRuntimeVariant()
                    : new ArcGraphTerrainVisualVariant(defaultTileId, 1);
            }

            return result;
        }

        private ArcGraphTerrainVisualDetail[] BuildDetails()
        {
            if (details == null || details.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualDetail>();

            var result = new ArcGraphTerrainVisualDetail[details.Length];
            for (int i = 0; i < details.Length; i++)
            {
                result[i] = details[i] != null
                    ? details[i].ToRuntimeDetail()
                    : new ArcGraphTerrainVisualDetail(defaultTileId, 1);
            }

            return result;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualVariantDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO di una variante tile pesata.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainVisualVariantDto
    {
        public int tileId;
        public int weight = 1;

        // =============================================================================
        // ToRuntimeVariant
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la variante JSON in variante runtime normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualVariant ToRuntimeVariant()
        {
            return new ArcGraphTerrainVisualVariant(tileId, weight);
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualDetailDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO di un dettaglio decorativo tile pesato.
    /// </para>
    ///
    /// <para>
    /// I dettagli sono overlay puramente visuali: non diventano oggetti, non
    /// entrano nella navigazione e non modificano la simulazione.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainVisualDetailDto
    {
        public int tileId;
        public int weight = 1;

        // =============================================================================
        // ToRuntimeDetail
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il dettaglio JSON in dettaglio runtime normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualDetail ToRuntimeDetail()
        {
            return new ArcGraphTerrainVisualDetail(tileId, weight);
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualAnimationDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO di una animazione visuale terrain.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainVisualAnimationDto
    {
        public int[] frameTileIds;
        public float frameSeconds = 0.25f;

        // =============================================================================
        // ToRuntimeAnimation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte l'animazione JSON in struttura runtime passiva.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualAnimation ToRuntimeAnimation()
        {
            return new ArcGraphTerrainVisualAnimation(frameTileIds, frameSeconds);
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualTransitionSetDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO di un set di transizioni tra due terrain type.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainVisualTransitionSetDto
    {
        public string fromTerrainId;
        public string toTerrainId;
        public ArcGraphTerrainVisualTransitionRuleDto[] rules;

        // =============================================================================
        // ToRuntimeTransitionSet
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il set JSON in set runtime normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualTransitionSet ToRuntimeTransitionSet()
        {
            return new ArcGraphTerrainVisualTransitionSet(
                fromTerrainId,
                toTerrainId,
                BuildRules());
        }

        private ArcGraphTerrainVisualTransitionRule[] BuildRules()
        {
            if (rules == null || rules.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualTransitionRule>();

            var result = new ArcGraphTerrainVisualTransitionRule[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                result[i] = rules[i] != null
                    ? rules[i].ToRuntimeRule()
                    : new ArcGraphTerrainVisualTransitionRule(string.Empty, 0);
            }

            return result;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualTransitionRuleDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO di una regola maschera -> tile id per transizioni terrain.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainVisualTransitionRuleDto
    {
        public string mask;
        public int tileId;
        public int[] frameTileIds;
        public float frameSeconds = 0.25f;

        // =============================================================================
        // ToRuntimeRule
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la regola JSON in regola runtime normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualTransitionRule ToRuntimeRule()
        {
            var animation = new ArcGraphTerrainVisualAnimation(frameTileIds, frameSeconds);
            return new ArcGraphTerrainVisualTransitionRule(mask, tileId, animation);
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualDualGridOverlayDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO di un overlay dual-grid data-driven.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainVisualDualGridOverlayDto
    {
        public string overlayTerrainId;
        public int priority = 0;
        public ArcGraphTerrainVisualDualGridRuleDto[] rules;

        // =============================================================================
        // ToRuntimeOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte l'overlay JSON in overlay runtime normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualDualGridOverlay ToRuntimeOverlay()
        {
            return new ArcGraphTerrainVisualDualGridOverlay(
                overlayTerrainId,
                priority,
                BuildRules());
        }

        private ArcGraphTerrainVisualDualGridRule[] BuildRules()
        {
            if (rules == null || rules.Length == 0)
                return Array.Empty<ArcGraphTerrainVisualDualGridRule>();

            var result = new ArcGraphTerrainVisualDualGridRule[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                result[i] = rules[i] != null
                    ? rules[i].ToRuntimeRule()
                    : new ArcGraphTerrainVisualDualGridRule(
                        "0000",
                        0,
                        new ArcGraphTerrainVisualAnimation(null, 0f));
            }

            return result;
        }
    }

    // =============================================================================
    // ArcGraphTerrainVisualDualGridRuleDto
    // =============================================================================
    /// <summary>
    /// <para>
    /// DTO di una regola dual-grid maschera 2x2 -> tile id.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class ArcGraphTerrainVisualDualGridRuleDto
    {
        public string mask;
        public int tileId;
        public int[] frameTileIds;
        public float frameSeconds = 0.25f;

        // =============================================================================
        // ToRuntimeRule
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte la regola dual-grid JSON in regola runtime normalizzata.
        /// </para>
        /// </summary>
        public ArcGraphTerrainVisualDualGridRule ToRuntimeRule()
        {
            var animation = new ArcGraphTerrainVisualAnimation(frameTileIds, frameSeconds);
            return new ArcGraphTerrainVisualDualGridRule(mask, tileId, animation);
        }
    }
}
