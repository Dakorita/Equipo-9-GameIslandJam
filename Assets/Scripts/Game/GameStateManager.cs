using System;
using UnityEngine;
using SignalNoise.Characters;

namespace SignalNoise.Game
{
    public enum GamePhase
    {
        CharacterSelect,
        Playing,
        Won,
        Lost
    }
    

    /// <summary>
    /// Singleton — manages the top-level game phase and exposes global state.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] VNManager vnManager;
        // ── Singleton ────────────────────────────────────────────────────────────

        public static GameStateManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Static Events ────────────────────────────────────────────────────────

        public static event Action<string>    OnGameWon;
        public static event Action<string>    OnGameLost;
        public static event Action<GamePhase> OnPhaseChanged;

        // ── Public State ─────────────────────────────────────────────────────────

        public GamePhase CurrentPhase    { get; private set; } = GamePhase.CharacterSelect;
        public CharacterData ActiveCharacter { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private TurnManager turnManager;
    
        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the player selects a character and begins the run.
        /// </summary>
        public void StartGame(CharacterData character)
        {
            if (character == null)
            {
                Debug.LogWarning("[GameStateManager] StartGame called with null CharacterData.");
                return;
            }

            ActiveCharacter = character;
            SetPhase(GamePhase.Playing);

            if (turnManager != null)
                turnManager.StartGame(character);
            else
                Debug.LogWarning("[GameStateManager] TurnManager reference is not set.");
        }

        public void TriggerWin(string reason)
        {
            if (CurrentPhase != GamePhase.Playing) return;
            SetPhase(GamePhase.Won);
            OnGameWon?.Invoke(reason);
            Debug.Log($"[GameStateManager] WIN — {reason}");
            if (vnManager != null)
            {
                vnManager.ResumeDialogue();
            }
        }

        public void TriggerLoss(string reason)
        {
            if (CurrentPhase != GamePhase.Playing) return;
            SetPhase(GamePhase.Lost);
            OnGameLost?.Invoke(reason);
            Debug.Log($"[GameStateManager] LOSE — {reason}");
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private void SetPhase(GamePhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
            Debug.Log($"[GameStateManager] Phase → {phase}");
        }
    }
}
