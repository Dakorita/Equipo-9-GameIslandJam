using System;
using System.Collections.Generic;
using UnityEngine;
using SignalNoise.Entities;

namespace SignalNoise.Grid
{
    /// <summary>
    /// Manages the logical CellData grid.
    /// NOT a singleton — referenced directly by TurnManager via the scene.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────────────

        [Header("Grid Dimensions")]
        [SerializeField] private int width  = 8;
        [SerializeField] private int height = 8;

        [Header("Initial Noise Multiplier")]
        [SerializeField] private float defaultNoiseSpreadMultiplier = 1f;

        // ── Public accessor ─────────────────────────────────────────────────────

        public int Width  => width;
        public int Height => height;

        /// <summary>Multiplier applied to NoiseCluster spread; updated by CharacterData.</summary>
        public float NoiseSpreadMultiplier
        {
            get => defaultNoiseSpreadMultiplier;
            set => defaultNoiseSpreadMultiplier = value;
        }

        // ── Static Events ───────────────────────────────────────────────────────

        public static event Action OnGridUpdated;

        // ── Private state ───────────────────────────────────────────────────────

        private CellData[,] grid;

        /// <summary>Snapshot of the grid taken before each evolve step — used by EchoFragment.</summary>
        private CellData[,] previousGrid;

        // ── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            Initialize();
        }

        // ── Public API ──────────────────────────────────────────────────────────

        public void Initialize()
        {
            grid         = new CellData[width, height];
            previousGrid = new CellData[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y]         = new CellData(new Vector2Int(x, y));
                    previousGrid[x, y] = grid[x, y].Clone();
                }
            }

            OnGridUpdated?.Invoke();
        }

        public CellData GetCell(int x, int y)
        {
            if (!IsInBounds(new Vector2Int(x, y))) return null;
            return grid[x, y];
        }

        public CellData GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

        public CellData[] GetNeighbors(Vector2Int pos)
        {
            var result = new List<CellData>();
            Vector2Int[] offsets =
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (var offset in offsets)
            {
                Vector2Int neighbor = pos + offset;
                if (IsInBounds(neighbor))
                    result.Add(grid[neighbor.x, neighbor.y]);
            }

            return result.ToArray();
        }

        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        public void SetEntityAt(Vector2Int pos, EntityType type)
        {
            if (!IsInBounds(pos)) return;
            grid[pos.x, pos.y].entityType = type;
            OnGridUpdated?.Invoke();
        }

        /// <summary>
        /// Manually triggers the OnGridUpdated event.
        /// Use when cells are modified in bulk outside of SetEntityAt (e.g., RedirectFlow action).
        /// </summary>
        public void NotifyGridUpdated()
        {
            OnGridUpdated?.Invoke();
        }

        /// <summary>
        /// Average noise level across all cells — used for entropy checks.
        /// </summary>
        public float GetSystemEntropy()
        {
            float total = 0f;
            int count   = width * height;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    total += grid[x, y].noiseLevel;

            return count > 0 ? total / count : 0f;
        }

        /// <summary>
        /// Average stability of cells that contain a SignalNode.
        /// Returns 0 if there are no signal nodes.
        /// </summary>
        public float GetSignalStability()
        {
            float total = 0f;
            int   count = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y].entityType == EntityType.SignalNode)
                    {
                        total += grid[x, y].stability;
                        count++;
                    }
                }
            }

            return count > 0 ? total / count : 0f;
        }

        // ── Evolution ────────────────────────────────────────────────────────────

        /// <summary>
        /// Saves a deep copy of the current grid so EchoFragments can reference it.
        /// Call this BEFORE EvolveGrid().
        /// </summary>
        public void ApplyPreviousStates()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    previousGrid[x, y] = grid[x, y].Clone();
        }

        /// <summary>
        /// Processes all entity behaviors for one turn.
        /// Works on a snapshot copy to avoid order-dependency, then writes results back.
        /// </summary>
        public void EvolveGrid()
        {
            // 1. Save current state for EchoFragment memory
            ApplyPreviousStates();

            // 2. Build a working copy so reads and writes don't interfere
            CellData[,] workingCopy = new CellData[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    workingCopy[x, y] = grid[x, y].Clone();

            // 3. Apply each entity's evolution to the working copy's neighbors
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CellData sourceCell = grid[x, y];          // read from live grid
                    Vector2Int pos      = new Vector2Int(x, y);
                    CellData[] neighbors = GetWorkingNeighbors(workingCopy, pos);

                    switch (sourceCell.entityType)
                    {
                        case EntityType.SignalNode:
                            EntityBehavior.EvolveSignalNode(workingCopy[x, y], neighbors);
                            break;

                        case EntityType.NoiseCluster:
                            EntityBehavior.EvolveNoiseCluster(workingCopy[x, y], neighbors, defaultNoiseSpreadMultiplier);
                            break;

                        case EntityType.EchoFragment:
                            EntityBehavior.EvolveEchoFragment(workingCopy[x, y], previousGrid[x, y]);
                            break;

                        case EntityType.WatcherDaemon:
                            EntityBehavior.EvolveWatcherDaemon(workingCopy[x, y], neighbors);
                            break;

                        case EntityType.DriftDaemon:
                            EntityBehavior.EvolveDriftDaemon(workingCopy[x, y], neighbors, workingCopy, pos);
                            break;
                    }

                    // Clear stasis after one turn
                    if (workingCopy[x, y].isStatic)
                        workingCopy[x, y].isStatic = false;
                }
            }

            // 4. Write the evolved data back to the live grid
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y].entityType = workingCopy[x, y].entityType;
                    grid[x, y].stability  = workingCopy[x, y].stability;
                    grid[x, y].noiseLevel = workingCopy[x, y].noiseLevel;
                    grid[x, y].isVisible  = workingCopy[x, y].isVisible;
                    grid[x, y].isStatic   = workingCopy[x, y].isStatic;
                }
            }

            OnGridUpdated?.Invoke();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private CellData[] GetWorkingNeighbors(CellData[,] workCopy, Vector2Int pos)
        {
            var result = new List<CellData>();
            Vector2Int[] offsets =
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (var offset in offsets)
            {
                Vector2Int neighbor = pos + offset;
                if (IsInBounds(neighbor))
                    result.Add(workCopy[neighbor.x, neighbor.y]);
            }

            return result.ToArray();
        }
    }
}
