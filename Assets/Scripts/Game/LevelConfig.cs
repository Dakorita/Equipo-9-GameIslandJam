using System.Collections.Generic;
using UnityEngine;
using SignalNoise.Entities;

namespace SignalNoise.Game
{
    [System.Serializable]
    public class EntityPlacement
    {
        public Vector2Int position;
        public EntityType type;
    }

    /// <summary>
    /// Defines the initial entity layout for a level.
    /// Supports manual placement (design specific layouts) or procedural generation (random from counts).
    /// Create via Assets → Create → SignalNoise → LevelConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelConfig", menuName = "SignalNoise/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Generation Mode")]
        [Tooltip("If true, entities are placed randomly using the counts below. If false, uses the Manual Placements list.")]
        public bool useProceduralGeneration = false;

        // ── Manual Placement ─────────────────────────────────────────────────────

        [Header("Manual Placement")]
        [Tooltip("Exact entity positions. Only used when useProceduralGeneration is false.")]
        public List<EntityPlacement> manualPlacements = new List<EntityPlacement>();

        // ── Procedural Generation ────────────────────────────────────────────────

        [Header("Procedural — Entity Counts")]
        [Tooltip("Only used when useProceduralGeneration is true.")]
        [Min(0)] public int signalCount = 4;
        [Min(0)] public int noiseCount  = 3;
        [Min(0)] public int echoCount   = 1;

        [Header("Procedural — Seed")]
        [Tooltip("Seed for procedural placement. 0 = random each time.")]
        public int seed = 0;
    }
}
