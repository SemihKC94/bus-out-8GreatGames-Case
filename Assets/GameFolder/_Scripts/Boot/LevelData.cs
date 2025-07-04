using UnityEngine;

namespace SKC.Level
{
    [CreateAssetMenu(fileName = "New Level", menuName = "SKC/Level/New Level Data")]
    public class LevelData : ScriptableObject
    {
        public TextAsset gridData;
        public TextAsset destinationData;
        public TextAsset busData;
        public float duration;
        public float CamPositionY;
    }
}
