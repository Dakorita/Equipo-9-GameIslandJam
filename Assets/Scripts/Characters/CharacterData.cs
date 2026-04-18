using UnityEngine;

namespace SignalNoise.Characters
{
    /// <summary>
    /// ScriptableObject that defines per-character stat modifiers.
    /// Create via Assets → Create → SignalNoise → CharacterData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "SignalNoise/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string characterName = "Unknown";
        public Sprite portrait;           // can be null in prototype

        [Header("Noise & Spread")]
        [Tooltip("Multiplier applied to NoiseCluster spread. <1 = slower, >1 = faster.")]
        public float noiseSpreadMultiplier = 1f;

        [Header("Action Radius Bonuses")]
        [Tooltip("Extra cells added to FilterZone radius.")]
        public int filterRadiusBonus  = 0;

        [Tooltip("Extra cells added to AmplifySignal radius.")]
        public int amplifyRadiusBonus = 0;

        [Header("Character Feel")]
        [Tooltip("Scales all effect radii. High values = more volatile, wider reach.")]
        public float volatility = 1f;

        [Header("Action Strengths")]
        [Tooltip("How much noise is reduced per cell by FilterZone (0-1).")]
        public float filterZoneStrength = 0.3f;

        [Tooltip("How much stability is increased per cell by AmplifySignal (0-1).")]
        public float amplifyStrength = 0.25f;
    }
}
