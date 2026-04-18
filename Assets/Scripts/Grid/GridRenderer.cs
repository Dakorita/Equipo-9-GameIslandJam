using System;
using System.Collections.Generic;
using UnityEngine;
using SignalNoise.Entities;

namespace SignalNoise.Grid
{
    /// <summary>
    /// Renders the logical grid as a flat grid of colored sprites.
    /// No external assets required — uses Unity's built-in white sprite tinted per entity type.
    /// </summary>
    public class GridRenderer : MonoBehaviour
    {
        // ── Static Events ───────────────────────────────────────────────────────

        public static event Action<Vector2Int> OnCellClicked;

        // ── Inspector ───────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GridManager gridManager;

        [Header("Cell Visuals")]
        [SerializeField] private float cellSize      = 1f;
        [SerializeField] private float cellGap       = 0.05f;
        [SerializeField] private float cellDepth     = 0f;

        [Header("Colors")]
        [SerializeField] private Color colorEmpty         = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private Color colorSignalNode    = new Color(0.2f,  0.5f,  1.0f);
        [SerializeField] private Color colorNoiseCluster  = new Color(1.0f,  0.2f,  0.2f);
        [SerializeField] private Color colorEchoFragment  = new Color(0.7f,  0.2f,  1.0f);
        [SerializeField] private Color colorWatcher       = new Color(0.2f,  1.0f,  0.9f);
        [SerializeField] private Color colorDrift         = new Color(1.0f,  0.9f,  0.1f);
        [SerializeField] private Color colorHidden        = new Color(0.05f, 0.05f, 0.05f);
        [SerializeField] private Color colorSelected      = new Color(1.0f,  1.0f,  0.0f, 0.5f);

        // ── Private state ───────────────────────────────────────────────────────

        private GameObject[,] cellObjects;
        private SpriteRenderer[,] cellRenderers;
        private Vector2Int selectedCell = new Vector2Int(-1, -1);
        private Camera mainCamera;

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

            BuildGrid();
            RefreshVisuals();
        }

        private void Update()
        {
            HandleMouseInput();
        }

        // ── Grid Construction ────────────────────────────────────────────────────

        private void BuildGrid()
        {
            int w = gridManager.Width;
            int h = gridManager.Height;

            cellObjects   = new GameObject[w, h];
            cellRenderers = new SpriteRenderer[w, h];

            float step = cellSize + cellGap;

            // Centre the grid on this transform
            float offsetX = -(w - 1) * step * 0.5f;
            float offsetY = -(h - 1) * step * 0.5f;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    GameObject cell = new GameObject($"Cell_{x}_{y}");
                    cell.transform.SetParent(transform, false);
                    cell.transform.localPosition = new Vector3(
                        offsetX + x * step,
                        offsetY + y * step,
                        cellDepth
                    );

                    SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                    sr.sprite = GetDefaultSprite();
                    sr.color  = colorEmpty;

                    // Scale to match desired cell size
                    // Unity's default sprite is 100 px/unit, so local scale = cellSize
                    cell.transform.localScale = Vector3.one * cellSize;

                    cellObjects[x, y]   = cell;
                    cellRenderers[x, y] = sr;
                }
            }
        }

        // ── Visual Refresh ───────────────────────────────────────────────────────

        private void RefreshVisuals()
        {
            if (cellRenderers == null || gridManager == null) return;

            int w = gridManager.Width;
            int h = gridManager.Height;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    CellData cell = gridManager.GetCell(x, y);
                    if (cell == null) continue;

                    SpriteRenderer sr = cellRenderers[x, y];

                    if (!cell.isVisible)
                    {
                        sr.color = colorHidden;
                        continue;
                    }

                    Color baseColor = EntityColor(cell.entityType);

                    // Tint toward red based on noiseLevel, darken with low stability
                    baseColor = Color.Lerp(baseColor, Color.red, cell.noiseLevel * 0.4f);
                    baseColor = Color.Lerp(Color.black, baseColor, 0.4f + cell.stability * 0.6f);

                    // Highlight selected cell
                    if (selectedCell.x == x && selectedCell.y == y)
                        baseColor = Color.Lerp(baseColor, colorSelected, 0.6f);

                    sr.color = baseColor;
                }
            }
        }

        // ── Input ────────────────────────────────────────────────────────────────

        private void HandleMouseInput()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (mainCamera == null) return;

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
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

            int w    = gridManager.Width;
            int h    = gridManager.Height;
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
                case EntityType.SignalNode:   return colorSignalNode;
                case EntityType.NoiseCluster: return colorNoiseCluster;
                case EntityType.EchoFragment: return colorEchoFragment;
                case EntityType.WatcherDaemon: return colorWatcher;
                case EntityType.DriftDaemon:  return colorDrift;
                default:                      return colorEmpty;
            }
        }

        /// <summary>
        /// Returns Unity's built-in white sprite so no external assets are required.
        /// </summary>
        private Sprite GetDefaultSprite()
        {
            // Creates a 1x1 white texture and wraps it as a sprite
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
