using UnityEngine;
using SignalNoise.Characters;
using SignalNoise.Entities;
using SignalNoise.Grid;
using SignalNoise.Player;

namespace SignalNoise.Game
{
    /// <summary>
    /// Minimal test harness — populates the grid and starts the game.
    /// Attach to any GameObject in the scene.
    /// Keyboard: 1/2/3/4 = select action, Space = confirm turn.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager    gridManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private CharacterData  characterData;

        // ── Initial layout (8x8) ─────────────────────────────────────────────────
        // S = SignalNode, N = NoiseCluster, E = EchoFragment
        //
        //  . . . . . . . .
        //  . S . . . . S .
        //  . . N . . N . .
        //  . . . E . . . .
        //  . . . . . . . .
        //  . . N . . . . .
        //  . S . . . . S .
        //  . . . . . . . .

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
            if (Input.GetKeyDown(KeyCode.Space))  TurnManager.Instance?.ConfirmTurn();
        }

        private void OnGUI()
        {
            if (TurnManager.Instance == null || gridManager == null) return;

            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 14 };

            GUI.Label(new Rect(10, 10, 300, 25),
                $"Turn: {TurnManager.Instance.CurrentTurn}  |  Actions left: {TurnManager.Instance.ActionsLeft}", style);

            GUI.Label(new Rect(10, 30, 300, 25),
                $"Entropy: {gridManager.GetSystemEntropy():P0}  |  Signal: {gridManager.GetSignalStability():P0}", style);

            GUI.Label(new Rect(10, 50, 350, 25),
                "Keys: [1] Filter  [2] Amplify  [3] Isolate  [4] Redirect  [Space] End Turn", style);
        }

        private void PlaceInitialEntities()
        {
            // SignalNodes
            gridManager.SetEntityAt(new Vector2Int(1, 1), EntityType.SignalNode);
            gridManager.SetEntityAt(new Vector2Int(6, 1), EntityType.SignalNode);
            gridManager.SetEntityAt(new Vector2Int(1, 6), EntityType.SignalNode);
            gridManager.SetEntityAt(new Vector2Int(6, 6), EntityType.SignalNode);

            // NoiseClusters
            gridManager.SetEntityAt(new Vector2Int(2, 2), EntityType.NoiseCluster);
            gridManager.SetEntityAt(new Vector2Int(5, 2), EntityType.NoiseCluster);
            gridManager.SetEntityAt(new Vector2Int(2, 5), EntityType.NoiseCluster);

            // EchoFragment
            gridManager.SetEntityAt(new Vector2Int(3, 3), EntityType.EchoFragment);
        }
    }
}
