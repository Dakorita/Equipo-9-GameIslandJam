using System;
using UnityEngine;
using SignalNoise.Characters;
using SignalNoise.Grid;

namespace SignalNoise.Game
{
    public enum TurnPhase
    {
        GridEvolve,
        PlayerPhase,
        Resolve,
        CheckConditions
    }

    /// <summary>
    /// Singleton — orchestrates the turn loop.
    /// Flow: GridEvolve → PlayerPhase → Resolve → CheckConditions → (repeat)
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        public static TurnManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── Static Events ────────────────────────────────────────────────────────

        public static event Action<int> OnTurnStart;
        public static event Action<int> OnPlayerPhaseStart;
        public static event Action<int> OnActionUsed;
        public static event Action      OnTurnResolved;

        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Turn Settings")]
        [SerializeField] private int actionsPerTurn = 3;

        [Header("References")]
        [SerializeField] private GridManager    gridManager;
        [SerializeField] private WinLoseChecker winLoseChecker;

        // ── Public State ─────────────────────────────────────────────────────────

        public int       CurrentTurn    { get; private set; }
        public int       ActionsPerTurn => actionsPerTurn;
        public int       ActionsLeft    { get; private set; }
        public TurnPhase CurrentPhase   { get; private set; }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Called by GameStateManager when a run begins.
        /// </summary>
        public void StartGame(CharacterData character)
        {
            CurrentTurn = 0;
            winLoseChecker?.ResetCounters();
            BeginNextTurn();
        }

        /// <summary>
        /// Signals that the player is ready to begin their action phase.
        /// (Can also be called automatically after GridEvolve completes.)
        /// </summary>
        public void StartPlayerAction()
        {
            if (CurrentPhase != TurnPhase.GridEvolve) return;

            CurrentPhase = TurnPhase.PlayerPhase;
            ActionsLeft  = actionsPerTurn;
            OnPlayerPhaseStart?.Invoke(ActionsLeft);
        }

        /// <summary>
        /// Called by PlayerController each time an action is applied.
        /// </summary>
        public void NotifyActionUsed()
        {
            if (CurrentPhase != TurnPhase.PlayerPhase) return;

            ActionsLeft = Mathf.Max(0, ActionsLeft - 1);
            OnActionUsed?.Invoke(ActionsLeft);

            Debug.Log($"[TurnManager] Action used. Actions left: {ActionsLeft}");
        }

        /// <summary>
        /// Called by the player (e.g. an "End Turn" button) to finalize the turn.
        /// </summary>
        public void ConfirmTurn()
        {
            if (CurrentPhase != TurnPhase.PlayerPhase) return;

            // Resolve
            CurrentPhase = TurnPhase.Resolve;
            OnTurnResolved?.Invoke();
            Debug.Log($"[TurnManager] Turn {CurrentTurn} resolved.");

            // Check conditions
            CurrentPhase = TurnPhase.CheckConditions;
            winLoseChecker?.Check();

            // Only continue if the game is still running
            if (GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentPhase == GamePhase.Playing)
            {
                BeginNextTurn();
            }
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private void BeginNextTurn()
        {
            CurrentTurn++;
            CurrentPhase = TurnPhase.GridEvolve;
            OnTurnStart?.Invoke(CurrentTurn);

            Debug.Log($"[TurnManager] Turn {CurrentTurn} — GridEvolve phase.");

            if (gridManager != null)
                gridManager.EvolveGrid();
            else
                Debug.LogWarning("[TurnManager] GridManager reference not set.");

            // Immediately move to player phase after grid evolves
            StartPlayerAction();
        }
    }
}
