using UnityEngine;

namespace SchoolOfFish.Data
{
    [CreateAssetMenu(menuName = "School Of Fish/Fish Personality", fileName = "FishPersonality")]
    public class FishPersonality : ScriptableObject
    {
        [Range(0.5f, 2f)] public float timidness = 1f;
        [Range(0.5f, 2f)] public float curiosity = 1f;
        [Range(0.7f, 1.3f)] public float maxSpeedVariance = 1f;
    }
}
