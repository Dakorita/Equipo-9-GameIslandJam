using System;
using System.Collections.Generic;
using UnityEngine;
using SignalNoise.Entities;
using SignalNoise.Player;

namespace SignalNoise.Grid
{
    /// <summary>
    /// Renders the logical grid as a flat grid of sprites.
    /// Each cell has two layers:
    ///   - Background: colored block tinted by noise/stability (always visible)
    ///   - Entity:     sprite asset assigned per entity type (shown on top when assigned)
    /// If no sprite is assigned for an entity type the cell falls back to the tinted color block.
    /// </summary>
    public class GridRenderer : MonoBehaviour
    {
        // ── Static Events ───────────────────────────────────────────────────────

        public static event Action<Vector2Int> OnCellClicked;

        // ── Inspector ───────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GridManager gridManager;

        [Header("Cell Visuals")]
        [SerializeField] private float cellSize  = 1f;
        [SerializeField] private float cellGap   = 0.05f;
        [SerializeField] private float cellDepth = 0f;

        [Header("Entity Sprites")]
        [Tooltip("Sprite for empty/background cells. Leave null for a plain colored block.")]
        [SerializeField] private Sprite spriteEmpty        = null;
        [SerializeField] private Sprite spriteSignalNode   = null;
        [SerializeField] private Sprite spriteNoiseCluster = null;
        [SerializeField] private Sprite spriteEchoFragment = null;
        [SerializeField] private Sprite spriteWatcher      = null;
        [SerializeField] private Sprite spriteDrift        = null;

        [Header("Entity Animators")]
        [Tooltip("AnimatorController per entity type. Leave null to use the static sprite instead.")]
        [SerializeField] private RuntimeAnimatorController animSignalNode   = null;
        [SerializeField] private RuntimeAnimatorController animNoiseCluster = null;
        [SerializeField] private RuntimeAnimatorController animEchoFragment = null;
        [SerializeField] private RuntimeAnimatorController animWatcher      = null;
        [SerializeField] private RuntimeAnimatorController animDrift        = null;

        [Header("Background Colors")]
        [Tooltip("Tint used for the background layer when no entity sprite is assigned, or as backdrop when one is.")]
        [SerializeField] private Color colorEmpty         = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private Color colorSignalNode    = new Color(0.2f,  0.5f,  1.0f);
        [SerializeField] private Color colorNoiseCluster  = new Color(1.0f,  0.2f,  0.2f);
        [SerializeField] private Color colorEchoFragment  = new Color(0.7f,  0.2f,  1.0f);
        [SerializeField] private Color colorWatcher       = new Color(0.2f,  1.0f,  0.9f);
        [SerializeField] private Color colorDrift         = new Color(1.0f,  0.9f,  0.1f);
        [SerializeField] private Color colorHidden        = new Color(0.05f, 0.05f, 0.05f);
        [SerializeField] private Color colorSelected      = new Color(1.0f,  1.0f,  0.0f, 0.5f);

        [Header("Hover AoE Preview")]
        [Tooltip("Color of the AoE square shown while hovering over a cell.")]
        [SerializeField] private Color colorHoverPreview  = new Color(1.0f, 1.0f, 0.0f, 0.25f);

        // ── Private state ───────────────────────────────────────────────────────

        private SpriteRenderer[,] bgRenderers;     // background / tint layer
        private SpriteRenderer[,] entityRenderers; // entity sprite layer (on top)
        private SpriteRenderer[,] hoverRenderers;  // AoE hover preview layer (topmost)
        private Animator[,]       entityAnimators; // animator per entity cell (may be null)
        private Vector2Int selectedCell = new Vector2Int(-1, -1);
        private Vector2Int hoveredCell  = new Vector2Int(-1, -1);
        private Camera mainCamera;

        // Cached 1x1 white sprite used as default block when no sprite is assigned
        private Sprite defaultBlockSprite;

        // ── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            mainCamera = Camera.main;
            GridManager.OnGridUpdated += RefreshVisuals;
        }

        private void OnDestroy()
        {
            GridManager.OnGridUpdated -= RefreshVisuals;
        }

        private void Start()
        {
            if (gridManager == null)
            {
                Debug.LogError("[GridRenderer] GridManager reference is not set.", this);
                return;
            }

            defaultBlockSprite = MakeWhiteSprite();
            BuildGrid();
            RefreshVisuals();
        }

        private void Update()
        {
            HandleMouseHover();
            HandleMouseInput();
        }

        // ── Grid Construction ────────────────────────────────────────────────────

        private void BuildGrid()
        {
            int w = gridManager.Width;
            int h = gridManager.Height;

            bgRenderers     = new SpriteRenderer[w, h];
            entityRenderers = new SpriteRenderer[w, h];
            hoverRenderers  = new SpriteRenderer[w, h];
            entityAnimators = new Animator[w, h];

            float step    = cellSize + cellGap;
            float offsetX = -(w - 1) * step * 0.5f;
            float offsetY = -(h - 1) * step * 0.5f;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Vector3 localPos = new Vector3(
                        offsetX + x * step,
                        offsetY + y * step,
                        cellDepth
                    );

                    // ── Background layer ────────────────────────────────────────
                    GameObject bgObj = new GameObject($"Cell_{x}_{y}_BG");
                    bgObj.transform.SetParent(transform, false);
                    bgObj.transform.localPosition = localPos;
                    bgObj.transform.localScale    = Vector3.one * cellSize;

                    SpriteRenderer bgSR = bgObj.AddComponent<SpriteRenderer>();
                    bgSR.sprite         = spriteEmpty != null ? spriteEmpty : defaultBlockSprite;
                    bgSR.color          = colorEmpty;
                    bgSR.sortingOrder   = 0;

                    bgRenderers[x, y] = bgSR;

                    // ── Entity layer (on top) ────────────────────────────────────
                    GameObject entityObj = new GameObject($"Cell_{x}_{y}_Entity");
                    entityObj.transform.SetParent(transform, false);
                    entityObj.transform.localPosition = new Vector3(localPos.x, localPos.y, localPos.z - 0.01f);
                    entityObj.transform.localScale    = Vector3.one * cellSize;

                    SpriteRenderer entitySR = entityObj.AddComponent<SpriteRenderer>();
                    entitySR.sprite         = null;
                    entitySR.color          = Color.white;
                    entitySR.sortingOrder   = 1;
                    entitySR.enabled        = false;

                    // Animator is added but left with no controller until RefreshVisuals assigns one
                    Animator anim = entityObj.AddComponent<Animator>();
                    anim.enabled  = false;

                    entityRenderers[x, y] = entitySR;
                    entityAnimators[x, y] = anim;

                    // ── Hover AoE preview layer (topmost) ────────────────────────
                    GameObject hoverObj = new GameObject($"Cell_{x}_{y}_Hover");
                    hoverObj.transform.SetParent(transform, false);
                    hoverObj.transform.localPosition = new Vector3(localPos.x, localPos.y, localPos.z - 0.02f);
                    hoverObj.transform.localScale    = Vector3.one * cellSize;

                    SpriteRenderer hoverSR = hoverObj.AddComponent<SpriteRenderer>();
                    hoverSR.sprite         = defaultBlockSprite;
                    hoverSR.color          = Color.clear;
                    hoverSR.sortingOrder   = 2;

                    hoverRenderers[x, y] = hoverSR;
                }
            }
        }

        // ── Visual Refresh ───────────────────────────────────────────────────────

        private void RefreshVisuals()
        {
            if (bgRenderers == null || gridManager == null) return;

            int w = gridManager.Width;
            int h = gridManager.Height;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    CellData cell = gridManager.GetCell(x, y);
                    if (cell == null) continue;

                    SpriteRenderer bgSR     = bgRenderers[x, y];
                    SpriteRenderer entitySR = entityRenderers[x, y];
                    Animator       anim     = entityAnimators[x, y];

                    // ── Background: always neutral, just the grid image ───────────
                    bgSR.sprite = spriteEmpty != null ? spriteEmpty : defaultBlockSprite;
                    bgSR.color  = cell.isVisible ? colorEmpty : colorHidden;

                    if (!cell.isVisible)
                    {
                        anim.enabled     = false;
                        entitySR.enabled = false;
                        continue;
                    }

                    // ── Entity tint: noise/stability applied to the sprite itself ─
                    Color entityTint = Color.white;
                    entityTint = Color.Lerp(entityTint, Color.red,   cell.noiseLevel * 0.4f);
                    entityTint = Color.Lerp(Color.black, entityTint, 0.4f + cell.stability * 0.6f);

                    if (selectedCell.x == x && selectedCell.y == y)
                        entityTint = Color.Lerp(entityTint, colorSelected, 0.6f);

                    // ── Entity layer: animator > static sprite > prototype block ──
                    RuntimeAnimatorController controller = EntityAnimator(cell.entityType);
                    Sprite entitySprite                  = EntitySprite(cell.entityType);

                    if (controller != null)
                    {
                        if (anim.runtimeAnimatorController != controller)
                            anim.runtimeAnimatorController = controller;
                        anim.enabled     = true;
                        entitySR.color   = entityTint;
                        entitySR.enabled = true;
                    }
                    else if (entitySprite != null)
                    {
                        anim.enabled     = false;
                        entitySR.sprite  = entitySprite;
                        entitySR.color   = entityTint;
                        entitySR.enabled = true;
                    }
                    else if (cell.entityType != EntityType.Empty)
                    {
                        // Prototype fallback: colored block tinted by game state
                        anim.enabled     = false;
                        entitySR.sprite  = defaultBlockSprite;
                        entitySR.color   = Color.Lerp(EntityColor(cell.entityType), Color.red, cell.noiseLevel * 0.4f);
                        entitySR.color   = Color.Lerp(Color.black, entitySR.color, 0.4f + cell.stability * 0.6f);
                        entitySR.enabled = true;
                    }
                    else
                    {
                        anim.enabled     = false;
                        entitySR.enabled = false;
                    }
                }
            }
        }

        // ── Input ────────────────────────────────────────────────────────────────

        private void HandleMouseHover()
        {
            if (hoverRenderers == null || mainCamera == null) return;

            float distToGrid = mainCamera.orthographic
                ? 0f
                : Mathf.Abs(mainCamera.transform.position.z - cellDepth);

            Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, distToGrid);
            Vector3 worldPos  = mainCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;

            Vector2Int? hit = WorldToCell(worldPos);

            if (!PlayerController.PlayerTurnActive || !hit.HasValue)
            {
                // Clear overlay when not the player's turn or mouse is off-grid
                if (hoveredCell.x != -1)
                {
                    hoveredCell = new Vector2Int(-1, -1);
                    UpdateHoverOverlay();
                }
                return;
            }

            if (hit.Value != hoveredCell)
            {
                hoveredCell = hit.Value;
                UpdateHoverOverlay();
            }
        }

        private void UpdateHoverOverlay()
        {
            if (hoverRenderers == null || gridManager == null) return;

            int w = gridManager.Width;
            int h = gridManager.Height;
            int radius = PlayerController.HoverRadius;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    bool inAoE = hoveredCell.x != -1
                        && Mathf.Abs(x - hoveredCell.x) <= radius
                        && Mathf.Abs(y - hoveredCell.y) <= radius;

                    hoverRenderers[x, y].color = inAoE ? colorHoverPreview : Color.clear;
                }
            }
        }

        private void HandleMouseInput()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (mainCamera == null) return;

            float distToGrid = mainCamera.orthographic
                ? 0f
                : Mathf.Abs(mainCamera.transform.position.z - cellDepth);

            Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, distToGrid);
            Vector3 worldPos  = mainCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;

            Vector2Int? hit = WorldToCell(worldPos);
            if (hit.HasValue)
            {
                selectedCell = hit.Value;
                RefreshVisuals();
                OnCellClicked?.Invoke(hit.Value);
            }
        }

        private Vector2Int? WorldToCell(Vector3 worldPos)
        {
            if (gridManager == null) return null;

            int w      = gridManager.Width;
            int h      = gridManager.Height;
            float step = cellSize + cellGap;

            float offsetX = -(w - 1) * step * 0.5f + transform.position.x;
            float offsetY = -(h - 1) * step * 0.5f + transform.position.y;

            int x = Mathf.RoundToInt((worldPos.x - offsetX) / step);
            int y = Mathf.RoundToInt((worldPos.y - offsetY) / step);

            if (x >= 0 && x < w && y >= 0 && y < h)
                return new Vector2Int(x, y);

            return null;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private Color EntityColor(EntityType type)
        {
            switch (type)
            {
                case EntityType.SignalNode:    return colorSignalNode;
                case EntityType.NoiseCluster:  return colorNoiseCluster;
                case EntityType.EchoFragment:  return colorEchoFragment;
                case EntityType.WatcherDaemon: return colorWatcher;
                case EntityType.DriftDaemon:   return colorDrift;
                default:                       return colorEmpty;
            }
        }

        private RuntimeAnimatorController EntityAnimator(EntityType type)
        {
            switch (type)
            {
                case EntityType.SignalNode:    return animSignalNode;
                case EntityType.NoiseCluster:  return animNoiseCluster;
                case EntityType.EchoFragment:  return animEchoFragment;
                case EntityType.WatcherDaemon: return animWatcher;
                case EntityType.DriftDaemon:   return animDrift;
                default:                       return null;
            }
        }

        private Sprite EntitySprite(EntityType type)
        {
            switch (type)
            {
                case EntityType.SignalNode:    return spriteSignalNode;
                case EntityType.NoiseCluster:  return spriteNoiseCluster;
                case EntityType.EchoFragment:  return spriteEchoFragment;
                case EntityType.WatcherDaemon: return spriteWatcher;
                case EntityType.DriftDaemon:   return spriteDrift;
                default:                       return null;
            }
        }

        private static Sprite MakeWhiteSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
