using UnityEngine;
using SignalNoise.Entities;

namespace SignalNoise.Grid
{
    public class CellData
    {
        public EntityType entityType;
        public float stability;     // 0-1
        public float noiseLevel;    // 0-1
        public bool isVisible;      // partial information mechanic
        public bool isStatic;       // isolated/stasis — prevents interaction for 1 turn
        public Vector2Int position;

        public CellData(Vector2Int pos)
        {
            position = pos;
            entityType = EntityType.Empty;
            stability = 0.5f;
            noiseLevel = 0f;
            isVisible = true;
            isStatic = false;
        }

        /// <summary>
        /// Deep copy — used by EchoFragment for state memory.
        /// </summary>
        public CellData Clone()
        {
            return new CellData(position)
            {
                entityType = this.entityType,
                stability  = this.stability,
                noiseLevel = this.noiseLevel,
                isVisible  = this.isVisible,
                isStatic   = false   // stasis does not carry over to clone
            };
        }
    }
}
