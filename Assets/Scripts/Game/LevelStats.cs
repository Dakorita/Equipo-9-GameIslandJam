using UnityEngine;

namespace SignalNoise.Game
{
    /// <summary>
    /// ScriptableObject that defines per-character stat modifiers.
    /// Create via Assets → Create → SignalNoise → CharacterData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelStats", menuName = "SignalNoise/LevelStats")]
    public class LevelStats : ScriptableObject
    {

        [Header("Noise & Spread")]
        public float noiseSpreadMultiplier = 1f;

        [Header("Turns Needed to Win")]
        [Tooltip("Turns Needed to win")]
        public int turnsToWin  = 0;

        [Header("Clarity of the level")]
        [Tooltip("Signal Necessary to win Multiplier")]
        public int signalNeededMultiplier = 0;

        [Header("Entropy of the level")]
        [Tooltip("Entropy to Loose Multiplier")]
        public float entropyNeededMultiplier = 1f;
        
    }
}