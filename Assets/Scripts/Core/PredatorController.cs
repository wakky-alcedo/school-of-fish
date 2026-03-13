using System.Collections.Generic;
using UnityEngine;

namespace SchoolOfFish.Core
{
    public class PredatorController : MonoBehaviour
    {
        public enum PatrolMode
        {
            Circle,
            Waypoints
        }

        [SerializeField] private PatrolMode patrolMode = PatrolMode.Circle;
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float turnSpeed = 4f;

        [Header("Circle")]
        [SerializeField] private Transform circleCenter;
        [SerializeField] private float circleRadius = 18f;
        [SerializeField] private float verticalWaveAmplitude = 2.2f;
        [SerializeField] private float verticalWaveFrequency = 0.25f;

        [Header("Waypoints")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private float waypointReachDistance = 0.8f;

        private int _waypointIndex;

        public Vector3 Position => transform.position;

        private void Update()
        {
            Vector3 target = patrolMode == PatrolMode.Circle ? ComputeCircleTarget() : ComputeWaypointTarget();
            MoveToTarget(target);
        }

        private Vector3 ComputeCircleTarget()
        {
            Vector3 center = circleCenter != null ? circleCenter.position : Vector3.zero;
            float angle = Time.time * moveSpeed * 0.15f;

            Vector3 p = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * circleRadius;
            p.y += Mathf.Sin(Time.time * verticalWaveFrequency) * verticalWaveAmplitude;
            return p;
        }

        private Vector3 ComputeWaypointTarget()
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                return transform.position;
            }

            Transform current = waypoints[_waypointIndex];
            if (current == null)
            {
                return transform.position;
            }

            Vector3 target = current.position;
            if ((target - transform.position).sqrMagnitude <= waypointReachDistance * waypointReachDistance)
            {
                _waypointIndex = (_waypointIndex + 1) % waypoints.Count;
            }

            return target;
        }

        private void MoveToTarget(Vector3 target)
        {
            Vector3 toTarget = target - transform.position;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.75f);

            if (patrolMode == PatrolMode.Circle)
            {
                Vector3 center = circleCenter != null ? circleCenter.position : Vector3.zero;
                Gizmos.DrawWireSphere(center, circleRadius);
                return;
            }

            if (waypoints == null || waypoints.Count == 0)
            {
                return;
            }

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(waypoints[i].position, 0.2f);
                Transform next = waypoints[(i + 1) % waypoints.Count];
                if (next != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, next.position);
                }
            }
        }
    }
}
