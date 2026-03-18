using UnityEngine;

namespace SchoolOfFish.Data
{
    [CreateAssetMenu(menuName = "School Of Fish/Boids Settings", fileName = "BoidsSettings")]
    public class BoidsSettings : ScriptableObject
    {
        [Header("Population")]
        [Min(1)] public int fishCount = 500;
        public Vector3 spawnExtents = new Vector3(30f, 12f, 30f);

        [Header("Core Boids")]
        [Min(0f)] public float neighborRadius = 4.5f;
        [Min(0f)] public float separationRadius = 1.6f;
        [Min(0f)] public float baseMaxSpeed = 6f;
        [Min(0f)] public float baseMaxForce = 10f;
        [Min(0f)] public float separationWeight = 1.7f;
        [Min(0f)] public float alignmentWeight = 1f;
        [Min(0f)] public float cohesionWeight = 1.1f;
        [Min(0f)] public float wanderWeight = 0.35f;
        [Min(0f)] public float boundaryWeight = 0.8f;
        [Min(0.1f)] public float boundaryRepulsionDistance = 3f;
        [Min(0f)] public float boundaryLookAheadTime = 0.45f;
        [Min(0f)] public float boundaryVelocityBoost = 1.2f;
        [Min(0f)] public float boundaryOutsideBoost = 2f;

        [Header("Panic")]
        [Min(0f)] public float predatorFearRadius = 8f;
        [Min(0f)] public float panicDuration = 2.5f;
        [Min(0f)] public float panicSpreadRadius = 5.5f;
        [Range(0f, 1f)] public float panicSpreadThreshold = 0.35f;
        [Min(1f)] public float panicSpeedMultiplier = 1.8f;
        [Min(1f)] public float panicForceMultiplier = 1.5f;

        [Header("Night Behavior")]
        [Range(0f, 1f)] public float nightAlignmentMultiplier = 0.5f;
        [Range(0f, 1f)] public float nightSpeedMultiplier = 0.65f;

        [Header("Optimization")]
        [Min(0.1f)] public float hashCellSize = 5f;
        [Range(1, 8)] public int maxNeighborCellsPerAxis = 1;
    }
}
