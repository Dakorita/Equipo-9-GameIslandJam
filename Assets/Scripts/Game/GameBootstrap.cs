using System.Collections.Generic;
using UnityEngine;
using SignalNoise.Characters;
using SignalNoise.Entities;
using SignalNoise.Grid;
using SignalNoise.Player;
using TMPro;

namespace SignalNoise.Game
{
    /// <summary>
    /// Populates the grid and starts the game.
    /// Assign a LevelConfig asset to control entity placement (manual or procedural).
    /// Keyboard: 1/2/3/4 = select action, Space = confirm turn.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager     gridManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private CharacterData   characterData;
        [SerializeField] private VNManager vnManager;
        [Header("Level Configuration")]
        [Tooltip("Defines which entities are placed on the grid. Leave empty to use the built-in fallback layout.")]
        [SerializeField] private LevelConfig levelConfig;
        [SerializeField] private TextMeshProUGUI texto;
        private void Start()
        {
            if (gridManager == null)
            {
                Debug.LogError("[GameBootstrap] GridManager not assigned.");
                return;
            }

            PlaceInitialEntities();

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.StartGame(characterData);
            else
                Debug.LogError("[GameBootstrap] GameStateManager not found in scene.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) playerController?.SelectFilterZone();
            if (Input.GetKeyDown(KeyCode.Alpha2)) playerController?.SelectAmplifySignal();
            if (Input.GetKeyDown(KeyCode.Alpha3)) playerController?.SelectIsolateCell();
            if (Input.GetKeyDown(KeyCode.Alpha4)) playerController?.SelectRedirectFlow();
            if (Input.GetKeyDown(KeyCode.Space) && vnManager.IsPaused())  TurnManager.Instance?.ConfirmTurn();
        }

        private void OnGUI()
        {
            if (TurnManager.Instance == null || gridManager == null) return;
            if (texto == null) return;
            texto.text = $"Turn: {TurnManager.Instance.CurrentTurn}  |  Actions left: {TurnManager.Instance.ActionsLeft}\n" +
                         $"Entropy: {gridManager.GetSystemEntropy():P0}  |  Signal: {gridManager.GetSignalStability():P0}\n" +
                         "Keys: [1] Filter  [2] Amplify  [3] Isolate  [4] Redirect  [Space] End Turn";
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 14 };

            /**GUI.Label(new Rect(10, 10, 300, 25),
                $"Turn: {TurnManager.Instance.CurrentTurn}  |  Actions left: {TurnManager.Instance.ActionsLeft}", style);

            GUI.Label(new Rect(10, 30, 300, 25),
                $"Entropy: {gridManager.GetSystemEntropy():P0}  |  Signal: {gridManager.GetSignalStability():P0}", style);

            GUI.Label(new Rect(10, 50, 350, 25),
                "Keys: [1] Filter  [2] Amplify  [3] Isolate  [4] Redirect  [Space] End Turn", style);**/
        }

        // ── Placement ────────────────────────────────────────────────────────────

        private void PlaceInitialEntities()
        {
            if (levelConfig == null)
            {
                PlaceFallbackLayout();
                return;
            }

            if (levelConfig.useProceduralGeneration)
                PlaceEntitiesProcedurally();
            else
                PlaceEntitiesManually();
        }

        /// <summary>Places entities at the exact positions defined in LevelConfig.manualPlacements.</summary>
        private void PlaceEntitiesManually()
        {
            foreach (var entry in levelConfig.manualPlacements)
            {
                if (!gridManager.IsInBounds(entry.position))
                {
                    Debug.LogWarning($"[GameBootstrap] Manual placement at {entry.position} is out of bounds — skipped.");
                    continue;
                }
                gridManager.SetEntityAt(entry.position, entry.type);
            }
        }

        /// <summary>Randomly places entities on empty cells using the counts in LevelConfig.</summary>
        private void PlaceEntitiesProcedurally()
        {
            if (levelConfig.seed != 0)
                Random.InitState(levelConfig.seed);

            List<Vector2Int> available = CollectEmptyCells();

            SpawnRandom(EntityType.SignalNode,    levelConfig.signalCount, available);
            SpawnRandom(EntityType.NoiseCluster,  levelConfig.noiseCount,  available);
            SpawnRandom(EntityType.EchoFragment,  levelConfig.echoCount,   available);
        }

        private void SpawnRandom(EntityType type, int count, List<Vector2Int> available)
        {
            for (int i = 0; i < count; i++)
            {
                if (available.Count == 0)
                {
                    Debug.LogWarning($"[GameBootstrap] No empty cells left to place {type}.");
                    return;
                }
                int index = Random.Range(0, available.Count);
                gridManager.SetEntityAt(available[index], type);
                available.RemoveAt(index);
            }
        }

        private List<Vector2Int> CollectEmptyCells()
        {
            var result = new List<Vector2Int>();
            for (int x = 0; x < gridManager.Width; x++)
                for (int y = 0; y < gridManager.Height; y++)
                {
                    var cell = gridManager.GetCell(x, y);
                    if (cell != null && cell.entityType == EntityType.Empty)
                        result.Add(new Vector2Int(x, y));
                }
            return result;
        }

        /// <summary>Hardcoded fallback layout used when no LevelConfig is assigned.</summary>
        private void PlaceFallbackLayout()
        {
            // S = SignalNode, N = NoiseCluster, E = EchoFragment
            //  . . . . . . . .
            //  . S . . . . S .
            //  . . N . . N . .
            //  . . . E . . . .
            //  . . . . . . . .
            //  . . N . . . . .
            //  . S . . . . S .
            //  . . . . . . . .
            gridManager.SetEntityAt(new Vector2Int(1, 1), EntityType.SignalNode);
            gridManager.SetEntityAt(new Vector2Int(6, 1), EntityType.SignalNode);
            gridManager.SetEntityAt(new Vector2Int(1, 6), EntityType.SignalNode);
            gridManager.SetEntityAt(new Vector2Int(6, 6), EntityType.SignalNode);

            gridManager.SetEntityAt(new Vector2Int(2, 2), EntityType.NoiseCluster);
            gridManager.SetEntityAt(new Vector2Int(5, 2), EntityType.NoiseCluster);
            gridManager.SetEntityAt(new Vector2Int(2, 5), EntityType.NoiseCluster);

            gridManager.SetEntityAt(new Vector2Int(3, 3), EntityType.EchoFragment);
        }
    }
}
