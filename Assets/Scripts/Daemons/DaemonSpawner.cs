using System;
using System.Collections.Generic;
using UnityEngine;
using SignalNoise.Entities;
using SignalNoise.Game;
using SignalNoise.Grid;

namespace SignalNoise.Daemons
{
    /// <summary>
    /// Spawns WatcherDaemon and DriftDaemon entities on the grid at the start of a run.
    /// Subscribes to GameStateManager.OnPhaseChanged so it spawns when Playing begins.
    /// </summary>
    public class DaemonSpawner : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Spawn Counts")]
        [SerializeField] private int watcherCount = 1;
        [SerializeField] private int driftCount   = 1;

        [Header("References")]
        [SerializeField] private GridManager gridManager;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameStateManager.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameStateManager.OnPhaseChanged -= HandlePhaseChanged;
        }

        // ── Event Handlers ───────────────────────────────────────────────────────

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Playing)
                SpawnDaemons();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Places daemon entities on random empty cells in the grid.
        /// </summary>
        public void SpawnDaemons()
        {
            if (gridManager == null)
            {
                Debug.LogWarning("[DaemonSpawner] GridManager reference not set.");
                return;
            }

            List<Vector2Int> emptyCells = CollectEmptyCells();

            PlaceDaemons(EntityType.WatcherDaemon, watcherCount, emptyCells);
            PlaceDaemons(EntityType.DriftDaemon,   driftCount,   emptyCells);

            Debug.Log($"[DaemonSpawner] Spawned {watcherCount} Watcher(s) and {driftCount} Drift daemon(s).");
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private void PlaceDaemons(EntityType type, int count, List<Vector2Int> available)
        {
            for (int i = 0; i < count; i++)
            {
                if (available.Count == 0)
                {
                    Debug.LogWarning($"[DaemonSpawner] No empty cells left to spawn {type}.");
                    break;
                }

                int index = UnityEngine.Random.Range(0, available.Count);
                Vector2Int pos = available[index];
                available.RemoveAt(index);

                gridManager.SetEntityAt(pos, type);
                Debug.Log($"[DaemonSpawner] Placed {type} at {pos}");
            }
        }

        private List<Vector2Int> CollectEmptyCells()
        {
            var result = new List<Vector2Int>();

            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    var cell = gridManager.GetCell(x, y);
                    if (cell != null && cell.entityType == EntityType.Empty)
                        result.Add(new Vector2Int(x, y));
                }
            }

            return result;
        }
    }
}
