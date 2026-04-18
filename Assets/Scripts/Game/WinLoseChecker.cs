using UnityEngine;
using SignalNoise.Grid;

namespace SignalNoise.Game
{
    /// <summary>
    /// Evaluates win/lose conditions after every turn resolution.
    /// Called by TurnManager at the end of ConfirmTurn().
    /// </summary>
    public class WinLoseChecker : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Win Conditions")]
        [Tooltip("Number of consecutive turns with stability above threshold required to win.")]
        [SerializeField] private int stabilityTurnsRequired = 5;

        [Tooltip("If system entropy (avg noise) drops below this value the player wins.")]
        [SerializeField] private float noiseWinThreshold = 0.2f;

        [Header("Lose Conditions")]
        [Tooltip("If system entropy exceeds this the player loses.")]
        [SerializeField] private float entropyLoseThreshold = 0.85f;

        [Tooltip("If avg signal-node stability drops below this the signal is considered collapsed.")]
        [SerializeField] private float signalCollapsedThreshold = 0.05f;

        [Header("References")]
        [SerializeField] private GridManager gridManager;

        // ── Private State ────────────────────────────────────────────────────────

        private int consecutiveStableTurns;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Called by TurnManager after each turn is fully resolved.
        /// Notifies GameStateManager if any terminal condition is met.
        /// </summary>
        public void Check()
        {
            if (gridManager == null)
            {
                Debug.LogWarning("[WinLoseChecker] GridManager reference not set.");
                return;
            }

            if (GameStateManager.Instance == null ||
                GameStateManager.Instance.CurrentPhase != GamePhase.Playing)
                return;

            float entropy        = gridManager.GetSystemEntropy();
            float signalStability = gridManager.GetSignalStability();

            // ── Lose Conditions ──────────────────────────────────────────────────

            if (entropy >= entropyLoseThreshold)
            {
                GameStateManager.Instance.TriggerLoss(
                    $"System entropy critical ({entropy:P0} ≥ {entropyLoseThreshold:P0}).");
                return;
            }

            if (signalStability <= signalCollapsedThreshold)
            {
                GameStateManager.Instance.TriggerLoss(
                    $"Signal collapsed — stability {signalStability:P0} ≤ {signalCollapsedThreshold:P0}.");
                return;
            }

            // ── Win Conditions ───────────────────────────────────────────────────

            // Win by sustained low noise
            if (entropy <= noiseWinThreshold)
            {
                GameStateManager.Instance.TriggerWin(
                    $"Noise suppressed below threshold ({entropy:P0} ≤ {noiseWinThreshold:P0}).");
                return;
            }

            // Win by N consecutive turns above stability threshold
            if (signalStability > signalCollapsedThreshold + 0.1f)
            {
                consecutiveStableTurns++;
                Debug.Log($"[WinLoseChecker] Stable turn {consecutiveStableTurns}/{stabilityTurnsRequired}");

                if (consecutiveStableTurns >= stabilityTurnsRequired)
                {
                    GameStateManager.Instance.TriggerWin(
                        $"Signal maintained stable for {stabilityTurnsRequired} consecutive turns.");
                }
            }
            else
            {
                consecutiveStableTurns = 0;
            }
        }

        public void ResetCounters()
        {
            consecutiveStableTurns = 0;
        }

        public void InitializeValues()
        {
            stabilityTurnsRequired = stabilityTurnsRequired;//DIRTY: CalculateStabilityTurns();
            noiseWinThreshold = noiseWinThreshold; //DIRTY: CalculateNoiseWinThreshold();
            entropyLoseThreshold = entropyLoseThreshold; //DIRTY: CalculateEntropyLoseThreshold();
            signalCollapsedThreshold = signalCollapsedThreshold; //DIRTY: CalculateSignalCollapsedThreshold();
            gridManager = gridManager; //DIRTY: SetGridManager();
        }
    }
}