using System.Collections.Generic;

namespace Arcontio.Core.Environment
{
    // =============================================================================
    // EnvironmentAreaQueryResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato read-only di una query spaziale su snapshot ambientale.
    /// </para>
    ///
    /// <para><b>Principio architetturale: lettura per consumer senza stato mutabile</b></para>
    /// <para>
    /// I consumer futuri devono poter chiedere quali layer ambientali interessano
    /// una cella senza ricevere accesso ai dizionari interni di <see cref="EnvironmentState"/>.
    /// Questo risultato contiene solo una lista gia' materializzata e flag derivati.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cell</b>: cella richiesta.</item>
    ///   <item><b>Areas</b>: aree abilitate che contengono la cella.</item>
    ///   <item><b>AreaCount</b>: numero di aree trovate.</item>
    ///   <item><b>Has*</b>: presenza di payload specializzati tra le aree trovate.</item>
    /// </list>
    /// </summary>
    public sealed class EnvironmentAreaQueryResult
    {
        private static readonly EnvironmentAreaSnapshot[] EmptyAreas = new EnvironmentAreaSnapshot[0];

        public EnvironmentCellCoord Cell { get; }
        public IReadOnlyList<EnvironmentAreaSnapshot> Areas { get; }
        public int AreaCount => Areas.Count;
        public bool HasFertility { get; }
        public bool HasWater { get; }
        public bool HasVegetation { get; }
        public bool HasSeedBank { get; }

        // =============================================================================
        // EnvironmentAreaQueryResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce il risultato aggregando aree e flag derivati.
        /// </para>
        /// </summary>
        public EnvironmentAreaQueryResult(
            EnvironmentCellCoord cell,
            IReadOnlyList<EnvironmentAreaSnapshot> areas)
        {
            Cell = cell;
            Areas = areas ?? EmptyAreas;
            bool hasFertility = false;
            bool hasWater = false;
            bool hasVegetation = false;
            bool hasSeedBank = false;

            // I flag vengono precalcolati per rendere economiche le letture frequenti
            // dei consumer futuri, senza costringerli a conoscere i payload interni.
            for (int i = 0; i < Areas.Count; i++)
            {
                hasFertility = hasFertility || Areas[i].HasFertility;
                hasWater = hasWater || Areas[i].HasWater;
                hasVegetation = hasVegetation || Areas[i].HasVegetation;
                hasSeedBank = hasSeedBank || Areas[i].HasSeedBank;
            }

            HasFertility = hasFertility;
            HasWater = hasWater;
            HasVegetation = hasVegetation;
            HasSeedBank = hasSeedBank;
        }
    }

    // =============================================================================
    // EnvironmentSnapshotQuery
    // =============================================================================
    /// <summary>
    /// <para>
    /// Query helper read-only per snapshot ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: snapshot come contratto di lettura</b></para>
    /// <para>
    /// Questo helper non interroga World, MapGrid, ArcGraph o oggetti runtime. Lavora
    /// soltanto su <see cref="EnvironmentSnapshot"/> gia' prodotto dal Core e offre
    /// filtri lineari semplici, sufficienti per la foundation v0.39.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>QueryCell</b>: trova tutte le aree abilitate che contengono una cella.</item>
    ///   <item><b>QueryCellByKind</b>: trova aree di un layer specifico.</item>
    ///   <item><b>ContainsLayer</b>: controlla presenza di un layer sulla cella.</item>
    ///   <item><b>TryGetTopPriorityArea</b>: sceglie l'area a priorita' maggiore.</item>
    /// </list>
    /// </summary>
    public static class EnvironmentSnapshotQuery
    {
        private static readonly EnvironmentPlantSnapshot[] EmptyPlants =
            new EnvironmentPlantSnapshot[0];

        // =============================================================================
        // QueryCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce tutte le aree abilitate che contengono la cella indicata.
        /// </para>
        /// </summary>
        public static EnvironmentAreaQueryResult QueryCell(
            EnvironmentSnapshot snapshot,
            EnvironmentCellCoord cell)
        {
            return QueryCellByKind(snapshot, cell, null);
        }

        // =============================================================================
        // QueryCellByKind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce le aree abilitate di un tipo specifico che contengono la cella.
        /// </para>
        /// </summary>
        public static EnvironmentAreaQueryResult QueryCellByKind(
            EnvironmentSnapshot snapshot,
            EnvironmentCellCoord cell,
            EnvironmentAreaKind kind)
        {
            return QueryCellByKind(snapshot, cell, (EnvironmentAreaKind?)kind);
        }

        // =============================================================================
        // ContainsLayer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se almeno un'area del layer richiesto contiene la cella.
        /// </para>
        /// </summary>
        public static bool ContainsLayer(
            EnvironmentSnapshot snapshot,
            EnvironmentCellCoord cell,
            EnvironmentAreaKind kind)
        {
            return TryGetTopPriorityArea(snapshot, cell, kind, out _);
        }

        // =============================================================================
        // TryGetTopPriorityArea
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca l'area abilitata a priorita' maggiore per cella e tipo richiesti.
        /// </para>
        /// </summary>
        public static bool TryGetTopPriorityArea(
            EnvironmentSnapshot snapshot,
            EnvironmentCellCoord cell,
            EnvironmentAreaKind kind,
            out EnvironmentAreaSnapshot area)
        {
            area = default;
            bool found = false;
            int bestPriority = int.MinValue;

            var areas = snapshot?.Areas;
            if (areas == null)
                return false;

            for (int i = 0; i < areas.Count; i++)
            {
                var candidate = areas[i];
                var definition = candidate.Definition;

                // La query rispetta il gate dichiarativo e non espone aree disabilitate
                // a consumer futuri come AI, debug o adapter visuali.
                if (!definition.IsEnabled
                    || definition.Kind != kind
                    || !definition.Bounds.Contains(cell))
                {
                    continue;
                }

                if (!found || definition.Priority > bestPriority)
                {
                    area = candidate;
                    bestPriority = definition.Priority;
                    found = true;
                }
            }

            return found;
        }

        // =============================================================================
        // TryGetPlant
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca una pianta nello snapshot usando il suo id ambientale.
        /// </para>
        /// </summary>
        public static bool TryGetPlant(
            EnvironmentSnapshot snapshot,
            EnvironmentPlantId plantId,
            out EnvironmentPlantSnapshot plant)
        {
            plant = default;

            if (snapshot?.Plants == null || !plantId.IsValid)
                return false;

            for (int i = 0; i < snapshot.Plants.Count; i++)
            {
                // Il lookup e' lineare nella foundation. Indici dedicati potranno
                // arrivare quando il numero di piante importanti sara' reale.
                if (!snapshot.Plants[i].PlantId.Equals(plantId))
                    continue;

                plant = snapshot.Plants[i];
                return true;
            }

            return false;
        }

        // =============================================================================
        // QueryPlantsAtCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce le piante importanti presenti sulla cella indicata.
        /// </para>
        /// </summary>
        public static IReadOnlyList<EnvironmentPlantSnapshot> QueryPlantsAtCell(
            EnvironmentSnapshot snapshot,
            EnvironmentCellCoord cell)
        {
            var plants = snapshot?.Plants;
            if (plants == null || plants.Count == 0)
                return EmptyPlants;

            var matches = new List<EnvironmentPlantSnapshot>();
            for (int i = 0; i < plants.Count; i++)
            {
                // Piu' piante sulla stessa cella sono ammesse a livello dati: il
                // vincolo visuale/oggetto resta una decisione di adapter o job futuri.
                if (plants[i].Cell.Equals(cell))
                    matches.Add(plants[i]);
            }

            return matches;
        }

        // =============================================================================
        // QueryPlantsBySpecies
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce le piante importanti appartenenti alla specie indicata.
        /// </para>
        /// </summary>
        public static IReadOnlyList<EnvironmentPlantSnapshot> QueryPlantsBySpecies(
            EnvironmentSnapshot snapshot,
            string speciesKey)
        {
            var plants = snapshot?.Plants;
            if (plants == null || plants.Count == 0 || string.IsNullOrWhiteSpace(speciesKey))
                return EmptyPlants;

            var matches = new List<EnvironmentPlantSnapshot>();
            for (int i = 0; i < plants.Count; i++)
            {
                // Il confronto case-insensitive segue la semantica del Plant Catalog:
                // le chiavi sono identita' logiche, non path asset.
                if (string.Equals(
                    plants[i].SpeciesKey,
                    speciesKey,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(plants[i]);
                }
            }

            return matches;
        }

        private static EnvironmentAreaQueryResult QueryCellByKind(
            EnvironmentSnapshot snapshot,
            EnvironmentCellCoord cell,
            EnvironmentAreaKind? kind)
        {
            var matches = new List<EnvironmentAreaSnapshot>();
            var areas = snapshot?.Areas;

            if (areas == null)
                return new EnvironmentAreaQueryResult(cell, matches);

            for (int i = 0; i < areas.Count; i++)
            {
                var candidate = areas[i];
                var definition = candidate.Definition;

                // La scansione lineare e' intenzionale in v0.39: prima rendiamo
                // stabile il contratto, poi potremo introdurre indici per chunk.
                if (!definition.IsEnabled)
                    continue;

                if (kind.HasValue && definition.Kind != kind.Value)
                    continue;

                if (!definition.Bounds.Contains(cell))
                    continue;

                matches.Add(candidate);
            }

            return new EnvironmentAreaQueryResult(cell, matches);
        }
    }
}
