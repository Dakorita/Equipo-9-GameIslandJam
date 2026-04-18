using UnityEngine;
using SignalNoise.Grid;

namespace SignalNoise.Entities
{
    /// <summary>
    /// Pure static class that defines per-entity evolution rules.
    /// All methods operate on CellData references — GridManager calls these during EvolveGrid().
    /// </summary>
    public static class EntityBehavior
    {
        // ── SignalNode ──────────────────────────────────────────────────────────

        /// <summary>
        /// SignalNode increases the stability of every neighboring cell by 0.1, clamped to [0,1].
        /// </summary>
        public static void EvolveSignalNode(CellData cell, CellData[] neighbors)
        {
            if (cell.isStatic) return;

            foreach (CellData neighbor in neighbors)
            {
                if (neighbor == null || neighbor.isStatic) continue;
                neighbor.stability = Mathf.Clamp01(neighbor.stability + 0.1f);
            }
        }

        // ── NoiseCluster ────────────────────────────────────────────────────────

        /// <summary>
        /// NoiseCluster spreads noise to adjacent cells.
        /// If a neighbor's noiseLevel exceeds the conversion threshold it converts to a NoiseCluster:
        ///   - SignalNode → NoiseCluster (loses 0.2 stability)
        ///   - Empty cell → NoiseCluster (fully corrupted)
        /// <paramref name="spreadMultiplier"/> is provided by GridManager (affected by CharacterData).
        /// <paramref name="emptyConversionThreshold"/> is configured on GridManager.
        /// </summary>
        public static void EvolveNoiseCluster(CellData cell, CellData[] neighbors, float spreadMultiplier, float emptyConversionThreshold = 0.8f)
        {
            if (cell.isStatic) return;

            foreach (CellData neighbor in neighbors)
            {
                if (neighbor == null || neighbor.isStatic) continue;

                neighbor.noiseLevel = Mathf.Clamp01(neighbor.noiseLevel + 0.15f * spreadMultiplier);

                if (neighbor.noiseLevel > 0.8f && neighbor.entityType == EntityType.SignalNode)
                {
                    neighbor.entityType = EntityType.NoiseCluster;
                    neighbor.stability  = Mathf.Clamp01(neighbor.stability - 0.2f);
                }
                else if (neighbor.noiseLevel >= emptyConversionThreshold && neighbor.entityType == EntityType.Empty)
                {
                    neighbor.entityType = EntityType.NoiseCluster;
                }
            }
        }

        // ── EchoFragment ────────────────────────────────────────────────────────

        /// <summary>
        /// EchoFragment copies the previous cell's entity type and blends stability values,
        /// creating a pseudo-memory / pattern-echo effect.
        /// </summary>
        public static void EvolveEchoFragment(CellData cell, CellData previousCell)
        {
            if (cell.isStatic || previousCell == null) return;

            // Replicate the previous entity type
            cell.entityType = previousCell.entityType;

            // Partially blend stability toward the remembered state
            cell.stability = Mathf.Lerp(cell.stability, previousCell.stability, 0.4f);

            // Carry over some noise memory
            cell.noiseLevel = Mathf.Lerp(cell.noiseLevel, previousCell.noiseLevel, 0.3f);
        }

        // ── WatcherDaemon ───────────────────────────────────────────────────────

        /// <summary>
        /// WatcherDaemon reveals (makes visible) all neighboring cells — partial-information mechanic.
        /// </summary>
        public static void EvolveWatcherDaemon(CellData cell, CellData[] neighbors)
        {
            // Watcher is never blocked by stasis
            foreach (CellData neighbor in neighbors)
            {
                if (neighbor == null) continue;
                neighbor.isVisible = true;
            }

            cell.isVisible = true;
        }

        // ── DriftDaemon ─────────────────────────────────────────────────────────

        /// <summary>
        /// DriftDaemon randomly swaps its entity type with one of its neighbors,
        /// simulating slow positional drift across the grid.
        /// </summary>
        public static void EvolveDriftDaemon(CellData cell, CellData[] neighbors, CellData[,] grid, Vector2Int pos)
        {
            if (cell.isStatic || neighbors.Length == 0) return;

            // Pick a random non-null, non-static neighbor
            CellData target = null;
            int attempts = 0;
            while (attempts < 10)
            {
                CellData candidate = neighbors[Random.Range(0, neighbors.Length)];
                if (candidate != null && !candidate.isStatic)
                {
                    target = candidate;
                    break;
                }
                attempts++;
            }

            if (target == null) return;

            // Swap entity types only (stability/noise stay as terrain context)
            EntityType temp = cell.entityType;
            cell.entityType  = target.entityType;
            target.entityType = temp;
        }
    }
}
