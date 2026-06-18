using System;
using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // CellSurfaceVisualDef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Sezione visuale opzionale di una definizione superficie cella.
    /// </para>
    ///
    /// <para><b>Principio architetturale: catalogo unico, consumer separati</b></para>
    /// <para>
    /// Il catalogo superficie contiene anche le chiavi visuali per evitare un
    /// secondo catalogo parallelo solo per ArcGraph. Il Core conserva il dato,
    /// ma i sistemi simulativi non devono dipendere da dettagli come animazione,
    /// variante grafica o tile key. ArcGraph puo' invece leggere questa sezione
    /// come dato derivato autorizzato.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>VisualRuleKey</b>: chiave della regola visuale usata dagli adapter.</item>
    ///   <item><b>ArcGraphTileKey</b>: chiave tile concreta o famiglia tile lato ArcGraph.</item>
    ///   <item><b>AllowsWaterAnimation</b>: indica se il renderer puo' usare clock animazione acqua.</item>
    ///   <item><b>UsesDeterministicVariants</b>: indica se la vista puo' variare sprite in modo seeded.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class CellSurfaceVisualDef
    {
        public string VisualRuleKey;
        public string ArcGraphTileKey;
        public bool AllowsWaterAnimation;
        public bool UsesDeterministicVariants;
    }

    // =============================================================================
    // CellSurfaceDef
    // =============================================================================
    /// <summary>
    /// <para>
    /// Definizione data-driven di un tipo di pavimento/superficie cella.
    /// </para>
    ///
    /// <para><b>Principio architetturale: pavimento come dato Core autoritativo</b></para>
    /// <para>
    /// Una superficie come erba, terra, pavimento in pietra o acqua non deve essere
    /// dedotta da cache di movimento, occlusione o oggetti. Questa definizione
    /// descrive cosa significa quel tipo di cella sia per la simulazione sia per la
    /// vista, mantenendo una sola anagrafica leggibile come gia' accade per
    /// <c>object_defs.json</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Id/DisplayName</b>: identita' stabile del tipo superficie.</item>
    ///   <item><b>MacroSurface</b>: categoria Natural, Artificial o Water.</item>
    ///   <item><b>MovementCost</b>: costo base futuro per query/pathfinding.</item>
    ///   <item><b>Blocks*</b>: blocchi fisici base della superficie, distinti dagli oggetti sopra.</item>
    ///   <item><b>CanHost*</b>: regole passive per biosfera e vegetazione futura.</item>
    ///   <item><b>Base*</b>: valori ambientali iniziali leggibili da sistemi futuri.</item>
    ///   <item><b>Visual</b>: chiavi visuali per ArcGraph, senza dipendenza inversa View -> Core.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class CellSurfaceDef
    {
        public string Id;
        public string DisplayName;
        public string MacroSurface;
        public float MovementCost;
        public bool BlocksMovement;
        public bool BlocksVision;
        public bool CanHostNaturalVegetation;
        public bool CanHostPhysicalPlant;
        public float BaseFertility01;
        public float BaseMoisture01;
        public CellSurfaceVisualDef Visual;

        // =============================================================================
        // ResolveMacroSurface
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte il testo del JSON nella enum Core usata da <see cref="CellSurfaceLayer"/>.
        /// </para>
        /// </summary>
        public CellSurfaceMacro ResolveMacroSurface()
        {
            if (string.Equals(MacroSurface, "Water", StringComparison.OrdinalIgnoreCase))
                return CellSurfaceMacro.Water;

            if (string.Equals(MacroSurface, "Artificial", StringComparison.OrdinalIgnoreCase))
                return CellSurfaceMacro.Artificial;

            return CellSurfaceMacro.Natural;
        }

        // =============================================================================
        // ResolveVisualRuleKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la chiave visuale preferita con fallback stabile all'Id.
        /// </para>
        /// </summary>
        public string ResolveVisualRuleKey()
        {
            if (Visual != null && !string.IsNullOrWhiteSpace(Visual.VisualRuleKey))
                return Visual.VisualRuleKey;

            return string.IsNullOrWhiteSpace(Id) ? string.Empty : Id;
        }
    }

    // =============================================================================
    // CellSurfaceDefDatabase
    // =============================================================================
    /// <summary>
    /// <para>
    /// Root serializzabile del file <c>surface_defs.json</c>.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class CellSurfaceDefDatabase
    {
        public List<CellSurfaceDef> Surfaces;
    }
}
