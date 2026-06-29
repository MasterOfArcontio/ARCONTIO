using System;
using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphOcclusionTargetKind
    // =============================================================================
    /// <summary>
    /// <para>
    /// Tipo minimale di target visuale che puo' essere coperto da un oggetto alto
    /// ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: occlusione visuale, non simulativa</b></para>
    /// <para>
    /// Questo enum non descrive visibilita' FOV, pathfinding, collisioni o
    /// percezione NPC. Serve soltanto alla pipeline ArcGraph per sapere quale
    /// entita' visuale deve ricevere sagoma o picking assistito quando un muro alto
    /// la copre graficamente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>None</b>: nessun target coperto.</item>
    ///   <item><b>Actor</b>: NPC/actor derivato dalla render queue.</item>
    ///   <item><b>Object</b>: oggetto fisico derivato dalla render queue.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphOcclusionTargetKind
    {
        None = 0,
        Actor = 1,
        Object = 2
    }

    // =============================================================================
    // ArcGraphOcclusionTarget
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato passivo della ricerca di un target coperto da un muro alto.
    /// </para>
    ///
    /// <para><b>Contratto value-only</b></para>
    /// <para>
    /// Il risultato contiene solo identita', cella e sort key gia' presenti nella
    /// render queue. Non contiene riferimenti Unity, non contiene riferimenti al
    /// <c>World</c> e non autorizza mutazioni. Il picking e il controller visuale
    /// possono quindi consumarlo senza rompere la separazione UI/World.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind</b>: actor o object.</item>
    ///   <item><b>EntityId</b>: id actor/object.</item>
    ///   <item><b>Cell</b>: cella base del target.</item>
    ///   <item><b>SortKey</b>: priorita' visuale gia' calcolata dalla queue.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphOcclusionTarget
    {
        public readonly ArcGraphOcclusionTargetKind Kind;
        public readonly int EntityId;
        public readonly ArcGraphCellCoord Cell;
        public readonly ArcGraphRenderSortKey SortKey;

        // =============================================================================
        // ArcGraphOcclusionTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un risultato di occlusione normalizzato.
        /// </para>
        /// </summary>
        public ArcGraphOcclusionTarget(
            ArcGraphOcclusionTargetKind kind,
            int entityId,
            ArcGraphCellCoord cell,
            ArcGraphRenderSortKey sortKey)
        {
            Kind = entityId > 0 ? kind : ArcGraphOcclusionTargetKind.None;
            EntityId = entityId > 0 ? entityId : -1;
            Cell = cell;
            SortKey = sortKey;
        }

        public bool IsValid => Kind != ArcGraphOcclusionTargetKind.None && EntityId > 0;

        public static ArcGraphOcclusionTarget None()
        {
            return new ArcGraphOcclusionTarget(
                ArcGraphOcclusionTargetKind.None,
                -1,
                new ArcGraphCellCoord(0, 0, 0),
                default);
        }
    }

    // =============================================================================
    // ArcGraphOcclusionPolicy
    // =============================================================================
    /// <summary>
    /// <para>
    /// Policy data-only per decidere se un oggetto alto ArcGraph copre un actor o
    /// un oggetto fisico nella riga visuale retrostante.
    /// </para>
    ///
    /// <para><b>Principio architetturale: una sola definizione di occlusione visuale</b></para>
    /// <para>
    /// La stessa policy viene usata dal boundary di picking e dal controller di
    /// trasparenza/sagoma. Questo evita che il mouse selezioni un target dietro un
    /// muro usando una regola diversa da quella che disegna fade e overlay. La
    /// policy legge soltanto render item gia' derivati: non legge <c>World</c>, non
    /// legge Biosfera, non cambia FOV e non modifica pathfinding.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsFadeableOccluder</b>: riconosce muri/oggetti alti visuali.</item>
    ///   <item><b>TryPickCoveredTarget</b>: cerca actor/object dietro un occluder.</item>
    ///   <item><b>IsTargetBehindOccluder</b>: verifica relazione cella-target.</item>
    ///   <item><b>ResolveOcclusionDepthCells</b>: converte altezza extra in celle.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphOcclusionPolicy
    {
        public const int DefaultMaximumDepthCells = 2;

        // =============================================================================
        // IsFadeableOccluder
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se un oggetto della render queue puo' comportarsi da occluder
        /// visuale alto.
        /// </para>
        /// </summary>
        public static bool IsFadeableOccluder(ArcGraphObjectRenderItem item)
        {
            if (!item.IsVisible || item.IsHeld)
                return false;

            if (!item.FadeWhenActorBehind)
                return false;

            int baseHeight = item.VisualBaseHeightPixels;
            int visualHeight = item.VisualHeightPixels;

            // Serve una base e un'altezza esplicita: un oggetto 32x32 o una porta
            // bassa non deve diventare semi-trasparente solo perche' viene puntato.
            return baseHeight > 0
                   && visualHeight > baseHeight;
        }

        // =============================================================================
        // IsObjectBaseHit
        // =============================================================================
        /// <summary>
        /// <para>
        /// Controlla se una cella ricade nel footprint/base dell'oggetto occluder.
        /// </para>
        /// </summary>
        public static bool IsObjectBaseHit(
            ArcGraphObjectRenderItem item,
            ArcGraphCellCoord cell)
        {
            if (item.Cell.Z != cell.Z)
                return false;

            int width = item.FootprintWidth <= 0 ? 1 : item.FootprintWidth;
            int height = item.FootprintHeight <= 0 ? 1 : item.FootprintHeight;

            return cell.X >= item.Cell.X
                   && cell.X < item.Cell.X + width
                   && cell.Y >= item.Cell.Y
                   && cell.Y < item.Cell.Y + height;
        }

        // =============================================================================
        // IsCoveredObjectTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se un oggetto dietro un muro puo' essere trattato come target
        /// coperto, quindi come motivo valido per attivare picking assistito,
        /// trasparenza del muro davanti o sagoma leggera.
        /// </para>
        ///
        /// <para><b>Principio architetturale: occluder e target restano ruoli diversi</b></para>
        /// <para>
        /// Muri e porte possono coprire altri elementi, ma non devono a loro volta
        /// far diventare trasparente un muro antecedente. Questo evita catene
        /// visuali nelle file di muri o porte: la trasparenza resta al servizio di
        /// NPC e oggetti realmente ispezionabili dietro il muro, non della continuita'
        /// grafica della muratura.
        /// </para>
        /// </summary>
        public static bool IsCoveredObjectTarget(ArcGraphObjectRenderItem item)
        {
            if (!item.IsVisible || item.IsHeld)
                return false;

            if (item.IsDoor)
                return false;

            // Un oggetto che e' gia' un occluder alto non deve diventare il target
            // di un altro occluder: altrimenti una fila verticale di muri/porte
            // produrrebbe fade a catena senza NPC o oggetti realmente nascosti.
            if (IsFadeableOccluder(item))
                return false;

            if (string.Equals(item.VisualKind, "wall", StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.VisualKind, "door", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        // =============================================================================
        // TryPickCoveredTarget
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca il target actor/object migliore coperto dalla parte alta di un
        /// occluder.
        /// </para>
        /// </summary>
        public static bool TryPickCoveredTarget(
            ArcGraphObjectRenderItem occluder,
            IReadOnlyList<ArcGraphActorRenderItem> actors,
            IReadOnlyList<ArcGraphObjectRenderItem> objects,
            int maximumDepthCells,
            out ArcGraphOcclusionTarget target)
        {
            target = ArcGraphOcclusionTarget.None();

            if (!IsFadeableOccluder(occluder))
                return false;

            int safeDepth = NormalizeDepth(maximumDepthCells);
            TryFindActorBehind(occluder, actors, safeDepth, out ArcGraphOcclusionTarget actorTarget);
            TryFindObjectBehind(occluder, objects, safeDepth, out ArcGraphOcclusionTarget objectTarget);

            if (actorTarget.IsValid)
            {
                target = actorTarget;
                return true;
            }

            if (objectTarget.IsValid)
            {
                target = objectTarget;
                return true;
            }

            return false;
        }

        // =============================================================================
        // TryFindOccluderCoveringTargetCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca il primo occluder che copre una cella target gia' nota.
        /// </para>
        /// </summary>
        public static bool TryFindOccluderCoveringTargetCell(
            IReadOnlyList<ArcGraphObjectRenderItem> objects,
            ArcGraphCellCoord targetCell,
            int maximumDepthCells,
            int ignoredObjectId,
            out ArcGraphObjectRenderItem occluder)
        {
            occluder = default;

            if (objects == null || objects.Count == 0)
                return false;

            int safeDepth = NormalizeDepth(maximumDepthCells);
            bool hasSelected = false;
            ArcGraphRenderSortKey selectedSortKey = default;

            for (int i = 0; i < objects.Count; i++)
            {
                ArcGraphObjectRenderItem candidate = objects[i];
                if (candidate.ObjectId == ignoredObjectId)
                    continue;

                if (!IsFadeableOccluder(candidate))
                    continue;

                if (!IsTargetBehindOccluder(candidate, targetCell, safeDepth))
                    continue;

                // In caso di piu' muri allineati, scegliamo quello visivamente piu'
                // avanti secondo lo stesso sort key della queue.
                if (!hasSelected || candidate.SortKey.CompareTo(selectedSortKey) >= 0)
                {
                    occluder = candidate;
                    selectedSortKey = candidate.SortKey;
                    hasSelected = true;
                }
            }

            return hasSelected;
        }

        // =============================================================================
        // IsTargetBehindOccluder
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se una cella target cade nella fascia coperta dalla parte alta
        /// del muro.
        /// </para>
        /// </summary>
        public static bool IsTargetBehindOccluder(
            ArcGraphObjectRenderItem occluder,
            ArcGraphCellCoord targetCell,
            int maximumDepthCells)
        {
            if (occluder.Cell.Z != targetCell.Z)
                return false;

            int width = occluder.FootprintWidth <= 0 ? 1 : occluder.FootprintWidth;
            if (targetCell.X < occluder.Cell.X || targetCell.X >= occluder.Cell.X + width)
                return false;

            int safeDepth = NormalizeDepth(maximumDepthCells);
            int minY = occluder.Cell.Y + 1;
            int maxY = occluder.Cell.Y + safeDepth;

            return targetCell.Y >= minY && targetCell.Y <= maxY;
        }

        // =============================================================================
        // ResolveOcclusionDepthCells
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte l'altezza extra dello sprite in un numero piccolo di celle
        /// dietro la base.
        /// </para>
        /// </summary>
        public static int ResolveOcclusionDepthCells(
            ArcGraphObjectRenderItem occluder,
            int maximumDepthCells)
        {
            if (!IsFadeableOccluder(occluder))
                return 0;

            int baseHeight = occluder.VisualBaseHeightPixels;
            int extraHeight = occluder.VisualHeightPixels - baseHeight;
            int rawDepth = (extraHeight + baseHeight - 1) / baseHeight;

            if (rawDepth < 1)
                rawDepth = 1;

            int safeMaximum = NormalizeDepth(maximumDepthCells);
            return rawDepth > safeMaximum ? safeMaximum : rawDepth;
        }

        private static bool TryFindActorBehind(
            ArcGraphObjectRenderItem occluder,
            IReadOnlyList<ArcGraphActorRenderItem> actors,
            int maximumDepthCells,
            out ArcGraphOcclusionTarget target)
        {
            target = ArcGraphOcclusionTarget.None();

            if (actors == null || actors.Count == 0)
                return false;

            bool hasSelected = false;
            ArcGraphRenderSortKey selectedSortKey = default;

            for (int i = 0; i < actors.Count; i++)
            {
                ArcGraphActorRenderItem item = actors[i];
                if (!item.IsVisible)
                    continue;

                if (!IsTargetBehindOccluder(occluder, item.DiscreteCell, maximumDepthCells))
                    continue;

                if (!hasSelected || item.SortKey.CompareTo(selectedSortKey) >= 0)
                {
                    target = new ArcGraphOcclusionTarget(
                        ArcGraphOcclusionTargetKind.Actor,
                        item.ActorId,
                        item.DiscreteCell,
                        item.SortKey);
                    selectedSortKey = item.SortKey;
                    hasSelected = true;
                }
            }

            return hasSelected;
        }

        private static bool TryFindObjectBehind(
            ArcGraphObjectRenderItem occluder,
            IReadOnlyList<ArcGraphObjectRenderItem> objects,
            int maximumDepthCells,
            out ArcGraphOcclusionTarget target)
        {
            target = ArcGraphOcclusionTarget.None();

            if (objects == null || objects.Count == 0)
                return false;

            bool hasSelected = false;
            ArcGraphRenderSortKey selectedSortKey = default;

            for (int i = 0; i < objects.Count; i++)
            {
                ArcGraphObjectRenderItem item = objects[i];
                if (item.ObjectId == occluder.ObjectId)
                    continue;

                if (!IsCoveredObjectTarget(item))
                    continue;

                if (!IsTargetBehindOccluder(occluder, item.Cell, maximumDepthCells))
                    continue;

                if (!hasSelected || item.SortKey.CompareTo(selectedSortKey) >= 0)
                {
                    target = new ArcGraphOcclusionTarget(
                        ArcGraphOcclusionTargetKind.Object,
                        item.ObjectId,
                        item.Cell,
                        item.SortKey);
                    selectedSortKey = item.SortKey;
                    hasSelected = true;
                }
            }

            return hasSelected;
        }

        private static int NormalizeDepth(int maximumDepthCells)
        {
            if (maximumDepthCells <= 0)
                return DefaultMaximumDepthCells;

            return maximumDepthCells;
        }
    }
}
