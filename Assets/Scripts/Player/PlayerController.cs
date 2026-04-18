using System;
using UnityEngine;
using SignalNoise.Characters;
using SignalNoise.Entities;
using SignalNoise.Game;
using SignalNoise.Grid;

namespace SignalNoise.Player
{
    /// <summary>
    /// Listens for cell-click events and applies the currently selected player action.
    /// One action costs one of the player's limited action budget per turn.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // ── Static Events ────────────────────────────────────────────────────────

        public static event Action<ActionType, Vector2Int> OnActionApplied;

        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Action Settings")]
        [SerializeField] private ActionType selectedAction = ActionType.FilterZone;

        [Header("Base Radii (cells from target)")]
        [SerializeField] private int filterZoneBaseRadius  = 1;
        [SerializeField] private int amplifyBaseRadius     = 1;
        [SerializeField] private int isolateBaseRadius     = 0;   // single cell
        [SerializeField] private int redirectBaseRadius    = 1;

        [Header("References")]
        [SerializeField] private GridManager    gridManager;
        [SerializeField] private CharacterData  characterData;

        // ── Private State ────────────────────────────────────────────────────────

        private bool isPlayerTurn;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            TurnManager.OnPlayerPhaseStart += HandlePlayerPhaseStart;
            TurnManager.OnTurnResolved     += HandleTurnResolved;
            GridRenderer.OnCellClicked     += HandleCellClicked;
        }

        private void OnDisable()
        {
            TurnManager.OnPlayerPhaseStart -= HandlePlayerPhaseStart;
            TurnManager.OnTurnResolved     -= HandleTurnResolved;
            GridRenderer.OnCellClicked     -= HandleCellClicked;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Called from UI to switch the queued action type.</summary>
        public void SelectAction(ActionType action)
        {
            selectedAction = action;
            Debug.Log($"[PlayerController] Selected action: {action}");
        }

        /// <summary>Sets the active character data (called after character select).</summary>
        public void SetCharacter(CharacterData data)
        {
            characterData = data;
        }

        // ── UI Button Wrappers ────────────────────────────────────────────────────
        // Unity Buttons can't call methods with custom enum params — use these instead.

        public void SelectFilterZone()    => SelectAction(ActionType.FilterZone);
        public void SelectAmplifySignal() => SelectAction(ActionType.AmplifySignal);
        public void SelectIsolateCell()   => SelectAction(ActionType.IsolateCell);
        public void SelectRedirectFlow()  => SelectAction(ActionType.RedirectFlow);
        public void ConfirmTurn()         => TurnManager.Instance?.ConfirmTurn();

        // ── Event Handlers ───────────────────────────────────────────────────────

        private void HandlePlayerPhaseStart(int actionsLeft)
        {
            isPlayerTurn = true;
        }

        private void HandleTurnResolved()
        {
            isPlayerTurn = false;
        }

        private void HandleCellClicked(Vector2Int pos)
        {
            if (!isPlayerTurn) return;
            if (TurnManager.Instance == null || TurnManager.Instance.ActionsLeft <= 0) return;
            if (gridManager == null) return;

            CellData targetCell = gridManager.GetCell(pos);
            if (targetCell == null) return;

            switch (selectedAction)
            {
                case ActionType.FilterZone:    ApplyFilterZone(pos);    break;
                case ActionType.AmplifySignal: ApplyAmplifySignal(pos); break;
                case ActionType.IsolateCell:   ApplyIsolateCell(pos);   break;
                case ActionType.RedirectFlow:  ApplyRedirectFlow(pos);  break;
            }

            TurnManager.Instance.NotifyActionUsed();
            OnActionApplied?.Invoke(selectedAction, pos);
        }

        // ── Actions ───────────────────────────────────────────────────────────────

        /// <summary>
        /// FilterZone: reduces noise in target cell and neighbors within radius.
        /// </summary>
        private void ApplyFilterZone(Vector2Int center)
        {
            float strength = characterData != null ? characterData.filterZoneStrength : 0.3f;
            int   radius   = filterZoneBaseRadius + (characterData != null ? characterData.filterRadiusBonus : 0);

            ForEachInRadius(center, radius, cell =>
            {
                cell.noiseLevel = Mathf.Clamp01(cell.noiseLevel - strength);
                cell.stability  = Mathf.Clamp01(cell.stability  + strength * 0.5f);
            });

            Debug.Log($"[PlayerController] FilterZone at {center} r={radius} s={strength}");
        }

        /// <summary>
        /// AmplifySignal: increases stability in target area, potentially accelerating SignalNode effect.
        /// </summary>
        private void ApplyAmplifySignal(Vector2Int center)
        {
            float strength = characterData != null ? characterData.amplifyStrength : 0.25f;
            int   radius   = amplifyBaseRadius + (characterData != null ? characterData.amplifyRadiusBonus : 0);

            ForEachInRadius(center, radius, cell =>
            {
                cell.stability  = Mathf.Clamp01(cell.stability + strength);
                // If there's already signal, push it harder
                if (cell.entityType == EntityType.SignalNode)
                    cell.stability = Mathf.Clamp01(cell.stability + strength * 0.5f);
            });

            Debug.Log($"[PlayerController] AmplifySignal at {center} r={radius} s={strength}");
        }

        /// <summary>
        /// IsolateCell: puts target cells into stasis for 1 turn (no interactions).
        /// </summary>
        private void ApplyIsolateCell(Vector2Int center)
        {
            int radius = isolateBaseRadius;

            ForEachInRadius(center, radius, cell =>
            {
                cell.isStatic = true;
            });

            Debug.Log($"[PlayerController] IsolateCell at {center} r={radius}");
        }

        /// <summary>
        /// RedirectFlow: rotates the entity type in the neighborhood clockwise,
        /// simulating a shift in local influence direction.
        /// </summary>
        private void ApplyRedirectFlow(Vector2Int center)
        {
            int radius = redirectBaseRadius;

            // Collect affected cells
            var affected = new System.Collections.Generic.List<CellData>();
            ForEachInRadius(center, radius, cell => affected.Add(cell));

            if (affected.Count < 2) return;

            // Rotate entity types one step
            EntityType first = affected[0].entityType;
            for (int i = 0; i < affected.Count - 1; i++)
                affected[i].entityType = affected[i + 1].entityType;
            affected[affected.Count - 1].entityType = first;

            // Trigger a grid update so the renderer refreshes
            gridManager.NotifyGridUpdated();

            Debug.Log($"[PlayerController] RedirectFlow at {center} r={radius}");
        }

        // ── Utilities ─────────────────────────────────────────────────────────────

        private void ForEachInRadius(Vector2Int center, int radius, System.Action<CellData> action)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int pos = new Vector2Int(center.x + dx, center.y + dy);
                    if (!gridManager.IsInBounds(pos)) continue;

                    CellData cell = gridManager.GetCell(pos);
                    if (cell != null && !cell.isStatic)
                        action(cell);
                }
            }
        }
    }
}
