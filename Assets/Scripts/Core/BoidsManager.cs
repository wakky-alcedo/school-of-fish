using System;
using System.Collections.Generic;
using SchoolOfFish.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SchoolOfFish.Core
{
    public class BoidsManager : MonoBehaviour
    {
        [SerializeField] private BoidsSettings settings;
        [SerializeField] private FishAgent fishPrefab;
        [SerializeField] private Transform schoolRoot;
        [SerializeField] private PredatorController predator;
        [SerializeField] private EnvironmentProvider environmentProvider;
        [SerializeField] private FishPersonality[] personalityPool;

        private readonly List<FishAgent> _agents = new List<FishAgent>(512);
        private readonly List<AgentState> _states = new List<AgentState>(512);
        private readonly Dictionary<int, List<int>> _spatialHash = new Dictionary<int, List<int>>(512);
        private Material _runtimeFishMaterial;
        private Material _runtimeTrailMaterial;

        private struct AgentState
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float PanicTimer;
            public FishPersonality Personality;
            public float WanderSeed;
        }

        private void Start()
        {
            TryAutoAssignReferences();

            if (settings == null)
            {
                Debug.LogWarning("BoidsManager is missing required reference: settings.");
                enabled = false;
                return;
            }

            if (schoolRoot == null)
            {
                schoolRoot = transform;
            }

            if (fishPrefab == null)
            {
                Debug.LogWarning("BoidsManager: fishPrefab is not assigned. Using runtime-generated fish instances.");
            }

            SpawnInitialFish();
        }

        private void TryAutoAssignReferences()
        {
            if (schoolRoot == null)
            {
                schoolRoot = transform;
            }

            if (predator == null)
            {
                predator = FindFirstObjectByType<PredatorController>();
            }

            if (environmentProvider == null)
            {
                environmentProvider = FindFirstObjectByType<EnvironmentProvider>();
            }

#if UNITY_EDITOR
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<BoidsSettings>("Assets/Settings/BoidsSettings.asset");
            }

            if (fishPrefab == null)
            {
                fishPrefab = AssetDatabase.LoadAssetAtPath<FishAgent>("Assets/Prefabs/Fish.prefab");
            }

            if (fishPrefab == null)
            {
                fishPrefab = FindFishPrefabInProject();
            }

            if ((personalityPool == null || personalityPool.Length == 0))
            {
                FishPersonality defaultPersonality = AssetDatabase.LoadAssetAtPath<FishPersonality>(
                    "Assets/Settings/FishPersonality_Default.asset");
                if (defaultPersonality != null)
                {
                    personalityPool = new[] { defaultPersonality };
                }
            }
#endif
        }

#if UNITY_EDITOR
        private static FishAgent FindFishPrefabInProject()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                FishAgent fish = prefab.GetComponent<FishAgent>();
                if (fish != null)
                {
                    Debug.Log("BoidsManager: Auto-assigned fishPrefab from " + path);
                    return fish;
                }
            }

            return null;
        }
#endif

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f || _states.Count == 0)
            {
                return;
            }

            BuildSpatialHash();

            float dayNightBlend = environmentProvider != null ? environmentProvider.Night01 : 0f;
            float alignmentWeight = settings.alignmentWeight * Mathf.Lerp(1f, settings.nightAlignmentMultiplier, dayNightBlend);
            float speedFactor = Mathf.Lerp(1f, settings.nightSpeedMultiplier, dayNightBlend);
            float verticalScale = Mathf.Clamp01(settings.verticalSwimMultiplier);

            for (int i = 0; i < _states.Count; i++)
            {
                AgentState state = _states[i];
                int neighborCount;
                int panickedNeighborCount;

                Vector3 separation = ComputeSeparation(i, state.Position, out neighborCount, out panickedNeighborCount);
                Vector3 alignment = ComputeAlignment(i, state.Position);
                Vector3 cohesion = ComputeCohesion(i, state.Position);
                Vector3 flee = ComputeFlee(state.Position);
                Vector3 wander = ComputeWander(i, dt);
                Vector3 boundary = ComputeBoundary(state.Position, state.Velocity);

                separation = ScaleVertical(separation, verticalScale);
                alignment = ScaleVertical(alignment, verticalScale);
                cohesion = ScaleVertical(cohesion, verticalScale);
                flee = ScaleVertical(flee, verticalScale);
                wander = ScaleVertical(wander, verticalScale);
                boundary = ScaleVertical(boundary, verticalScale);

                bool isPanicked = UpdatePanicState(ref state, neighborCount, panickedNeighborCount, flee.sqrMagnitude > 0f, dt);

                float personalitySpeed = state.Personality != null ? state.Personality.maxSpeedVariance : 1f;
                float timidness = state.Personality != null ? state.Personality.timidness : 1f;

                float maxSpeed = settings.baseMaxSpeed * speedFactor * personalitySpeed;
                float maxForce = settings.baseMaxForce;
                if (isPanicked)
                {
                    maxSpeed *= settings.panicSpeedMultiplier;
                    maxForce *= settings.panicForceMultiplier;
                }

                Vector3 desired =
                    separation * settings.separationWeight +
                    alignment * alignmentWeight +
                    cohesion * settings.cohesionWeight +
                    flee * timidness +
                    wander * settings.wanderWeight +
                    boundary * settings.boundaryWeight;

                Vector3 steer = Vector3.ClampMagnitude(desired, maxForce);
                state.Velocity = Vector3.ClampMagnitude(state.Velocity + steer * dt, maxSpeed);
                state.Velocity = ScaleVertical(state.Velocity, verticalScale);
                state.Position += state.Velocity * dt;

                KeepInsideBounds(ref state.Position, ref state.Velocity);

                _states[i] = state;
                _agents[i].ApplySimulation(state.Position, state.Velocity);
                _agents[i].SetPanicVisual(Mathf.Clamp01(state.PanicTimer / settings.panicDuration));
            }
        }

        private void SpawnInitialFish()
        {
            _agents.Clear();
            _states.Clear();

            for (int i = 0; i < settings.fishCount; i++)
            {
                Vector3 localPos = new Vector3(
                    UnityEngine.Random.Range(-settings.spawnExtents.x, settings.spawnExtents.x),
                    UnityEngine.Random.Range(-settings.spawnExtents.y, settings.spawnExtents.y),
                    UnityEngine.Random.Range(-settings.spawnExtents.z, settings.spawnExtents.z));

                FishAgent agent = CreateFishInstance(schoolRoot.TransformPoint(localPos));
                agent.SetMaxPitchAngle(settings.maxPitchAngleDegrees);
                _agents.Add(agent);

                Vector3 dir = UnityEngine.Random.onUnitSphere;
                dir.y *= 0.5f;
                if (dir.sqrMagnitude < 0.001f)
                {
                    dir = Vector3.forward;
                }

                _states.Add(new AgentState
                {
                    Position = agent.transform.position,
                    Velocity = ScaleVertical(dir.normalized * settings.baseMaxSpeed * 0.6f, Mathf.Clamp01(settings.verticalSwimMultiplier)),
                    PanicTimer = 0f,
                    Personality = GetRandomPersonality(),
                    WanderSeed = UnityEngine.Random.value * 1000f
                });
            }
        }

        private static Vector3 ScaleVertical(Vector3 v, float verticalScale)
        {
            v.y *= verticalScale;
            return v;
        }

        private FishAgent CreateFishInstance(Vector3 worldPosition)
        {
            if (fishPrefab != null)
            {
                return Instantiate(fishPrefab, worldPosition, Quaternion.identity, schoolRoot);
            }

            GameObject root = new GameObject("Fish");
            root.transform.SetParent(schoolRoot, false);
            root.transform.position = worldPosition;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
            body.transform.localScale = new Vector3(0.18f, 0.35f, 0.18f);

            Collider collider = body.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.6f;
            trail.startWidth = 0.08f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.06f;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                Renderer renderer = body.GetComponent<Renderer>();

                if (_runtimeFishMaterial == null)
                {
                    _runtimeFishMaterial = new Material(shader)
                    {
                        color = new Color(0.2f, 0.65f, 0.8f, 1f)
                    };
                }

                if (_runtimeTrailMaterial == null)
                {
                    _runtimeTrailMaterial = new Material(shader)
                    {
                        color = new Color(0.15f, 0.95f, 1f, 0.85f)
                    };
                }

                renderer.sharedMaterial = _runtimeFishMaterial;
                trail.sharedMaterial = _runtimeTrailMaterial;
            }

            return root.AddComponent<FishAgent>();
        }

        private FishPersonality GetRandomPersonality()
        {
            if (personalityPool == null || personalityPool.Length == 0)
            {
                return null;
            }

            return personalityPool[UnityEngine.Random.Range(0, personalityPool.Length)];
        }

        private Vector3 ComputeSeparation(int index, Vector3 position, out int neighborCount, out int panickedNeighborCount)
        {
            neighborCount = 0;
            panickedNeighborCount = 0;
            Vector3 separation = Vector3.zero;

            foreach (int n in EnumerateNeighbors(index, position, settings.neighborRadius))
            {
                Vector3 toMe = position - _states[n].Position;
                float sqrDist = toMe.sqrMagnitude;
                if (sqrDist < 0.0001f)
                {
                    continue;
                }

                neighborCount++;
                if (_states[n].PanicTimer > 0f)
                {
                    panickedNeighborCount++;
                }

                if (sqrDist <= settings.separationRadius * settings.separationRadius)
                {
                    separation += toMe / (sqrDist + 0.01f);
                }
            }

            return separation.normalized;
        }

        private Vector3 ComputeAlignment(int index, Vector3 position)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (int n in EnumerateNeighbors(index, position, settings.neighborRadius))
            {
                sum += _states[n].Velocity;
                count++;
            }

            if (count == 0)
            {
                return Vector3.zero;
            }

            return (sum / count).normalized;
        }

        private Vector3 ComputeCohesion(int index, Vector3 position)
        {
            Vector3 center = Vector3.zero;
            int count = 0;

            foreach (int n in EnumerateNeighbors(index, position, settings.neighborRadius))
            {
                center += _states[n].Position;
                count++;
            }

            if (count == 0)
            {
                return Vector3.zero;
            }

            center /= count;
            return (center - position).normalized;
        }

        private Vector3 ComputeFlee(Vector3 position)
        {
            if (predator == null)
            {
                return Vector3.zero;
            }

            Vector3 toFish = position - predator.Position;
            float sqrDist = toFish.sqrMagnitude;
            float fearSqr = settings.predatorFearRadius * settings.predatorFearRadius;
            if (sqrDist > fearSqr)
            {
                return Vector3.zero;
            }

            float danger01 = 1f - Mathf.Clamp01(Mathf.Sqrt(sqrDist) / settings.predatorFearRadius);
            return toFish.normalized * (0.3f + 0.7f * danger01);
        }

        private Vector3 ComputeWander(int index, float dt)
        {
            AgentState s = _states[index];
            float t = Time.time * 0.35f + s.WanderSeed;
            float x = Mathf.PerlinNoise(t, 0.17f) * 2f - 1f;
            float y = Mathf.PerlinNoise(0.91f, t) * 2f - 1f;
            float z = Mathf.PerlinNoise(t, 0.63f) * 2f - 1f;
            return new Vector3(x, y * 0.5f, z).normalized;
        }

        private Vector3 ComputeBoundary(Vector3 worldPos, Vector3 worldVel)
        {
            Vector3 localPos = schoolRoot.InverseTransformPoint(worldPos);
            Vector3 localVel = schoolRoot.InverseTransformDirection(worldVel);
            Vector3 ext = settings.spawnExtents;
            float repulsionDistance = Mathf.Max(0.1f, settings.boundaryRepulsionDistance);
            float lookAhead = Mathf.Max(0f, settings.boundaryLookAheadTime);
            float velBoost = Mathf.Max(0f, settings.boundaryVelocityBoost);
            float outsideBoost = Mathf.Max(0f, settings.boundaryOutsideBoost);
            float speedRef = Mathf.Max(0.1f, settings.baseMaxSpeed);

            Vector3 steer = Vector3.zero;

            steer.x = ComputeAxisBoundaryRepulsion(localPos.x, localVel.x, ext.x, repulsionDistance, lookAhead, speedRef, velBoost, outsideBoost);
            steer.y = ComputeAxisBoundaryRepulsion(localPos.y, localVel.y, ext.y, repulsionDistance, lookAhead, speedRef, velBoost, outsideBoost) * 0.6f;
            steer.z = ComputeAxisBoundaryRepulsion(localPos.z, localVel.z, ext.z, repulsionDistance, lookAhead, speedRef, velBoost, outsideBoost);

            return steer;
        }

        private static float ComputeAxisBoundaryRepulsion(
            float localPos,
            float localVel,
            float halfExtent,
            float repulsionDistance,
            float lookAhead,
            float speedRef,
            float velocityBoost,
            float outsideBoost)
        {
            float current = ComputeAxisBoundaryRepulsionAtPoint(localPos, localVel, halfExtent, repulsionDistance, speedRef, velocityBoost, outsideBoost);
            if (lookAhead <= 0f)
            {
                return current;
            }

            float futurePos = localPos + localVel * lookAhead;
            float future = ComputeAxisBoundaryRepulsionAtPoint(futurePos, localVel, halfExtent, repulsionDistance, speedRef, velocityBoost, outsideBoost);

            return Mathf.Abs(future) > Mathf.Abs(current) ? future : current;
        }

        private static float ComputeAxisBoundaryRepulsionAtPoint(
            float localPos,
            float localVel,
            float halfExtent,
            float repulsionDistance,
            float speedRef,
            float velocityBoost,
            float outsideBoost)
        {
            float abs = Mathf.Abs(localPos);
            float sign = Mathf.Sign(localPos);

            if (abs >= halfExtent)
            {
                float over = abs - halfExtent;
                float over01 = Mathf.Clamp01(over / repulsionDistance);
                return -sign * (1f + over01 * outsideBoost);
            }

            float start = Mathf.Max(0f, halfExtent - repulsionDistance);
            if (abs <= start)
            {
                return 0f;
            }

            float t = Mathf.InverseLerp(start, halfExtent, abs);
            float proximity = Mathf.SmoothStep(0f, 1f, t);

            // 壁方向へ進んでいる場合にだけ斥力を増幅する。
            float outwardSpeed = Mathf.Max(0f, sign * localVel);
            float outward01 = Mathf.Clamp01(outwardSpeed / speedRef);
            float directionalBoost = 1f + outward01 * velocityBoost;

            return -sign * proximity * directionalBoost;
        }

        private bool UpdatePanicState(ref AgentState state, int neighborCount, int panickedNeighborCount, bool predatorNearby, float dt)
        {
            if (predatorNearby)
            {
                state.PanicTimer = settings.panicDuration;
            }
            else if (neighborCount > 0)
            {
                float ratio = (float)panickedNeighborCount / neighborCount;
                if (ratio >= settings.panicSpreadThreshold)
                {
                    state.PanicTimer = Mathf.Max(state.PanicTimer, settings.panicDuration * 0.65f);
                }
            }

            if (state.PanicTimer > 0f)
            {
                state.PanicTimer = Mathf.Max(0f, state.PanicTimer - dt);
            }

            return state.PanicTimer > 0f;
        }

        private void KeepInsideBounds(ref Vector3 position, ref Vector3 velocity)
        {
            Vector3 local = schoolRoot.InverseTransformPoint(position);
            Vector3 ext = settings.spawnExtents;
            bool changed = false;

            if (Mathf.Abs(local.x) > ext.x)
            {
                local.x = Mathf.Clamp(local.x, -ext.x, ext.x);
                velocity.x *= -0.4f;
                changed = true;
            }
            if (Mathf.Abs(local.y) > ext.y)
            {
                local.y = Mathf.Clamp(local.y, -ext.y, ext.y);
                velocity.y *= -0.4f;
                changed = true;
            }
            if (Mathf.Abs(local.z) > ext.z)
            {
                local.z = Mathf.Clamp(local.z, -ext.z, ext.z);
                velocity.z *= -0.4f;
                changed = true;
            }

            if (changed)
            {
                position = schoolRoot.TransformPoint(local);
            }
        }

        private IEnumerable<int> EnumerateNeighbors(int selfIndex, Vector3 position, float radius)
        {
            Vector3Int cell = WorldToCell(position);
            int range = Mathf.Max(1, settings.maxNeighborCellsPerAxis);
            float radiusSqr = radius * radius;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        int key = Hash(cell.x + x, cell.y + y, cell.z + z);
                        if (!_spatialHash.TryGetValue(key, out List<int> bucket))
                        {
                            continue;
                        }

                        for (int i = 0; i < bucket.Count; i++)
                        {
                            int idx = bucket[i];
                            if (idx == selfIndex)
                            {
                                continue;
                            }

                            if ((_states[idx].Position - position).sqrMagnitude <= radiusSqr)
                            {
                                yield return idx;
                            }
                        }
                    }
                }
            }
        }

        private void BuildSpatialHash()
        {
            _spatialHash.Clear();

            for (int i = 0; i < _states.Count; i++)
            {
                Vector3Int cell = WorldToCell(_states[i].Position);
                int key = Hash(cell.x, cell.y, cell.z);

                if (!_spatialHash.TryGetValue(key, out List<int> bucket))
                {
                    bucket = new List<int>(16);
                    _spatialHash.Add(key, bucket);
                }

                bucket.Add(i);
            }
        }

        private Vector3Int WorldToCell(Vector3 world)
        {
            float cell = Mathf.Max(0.1f, settings.hashCellSize);
            Vector3 local = schoolRoot.InverseTransformPoint(world);
            return new Vector3Int(
                Mathf.FloorToInt(local.x / cell),
                Mathf.FloorToInt(local.y / cell),
                Mathf.FloorToInt(local.z / cell));
        }

        private static int Hash(int x, int y, int z)
        {
            unchecked
            {
                int h = x * 73856093;
                h ^= y * 19349663;
                h ^= z * 83492791;
                return h;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (settings == null)
            {
                return;
            }

            Transform root = schoolRoot != null ? schoolRoot : transform;
            Gizmos.color = new Color(0.25f, 0.75f, 1f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(root.position, root.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, settings.spawnExtents * 2f);
        }
    }
}
