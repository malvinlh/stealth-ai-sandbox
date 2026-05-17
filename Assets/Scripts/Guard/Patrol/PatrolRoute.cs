using System.Collections.Generic;
using UnityEngine;

namespace StealthGame
{
    /// <summary>How <see cref="PatrolRoute.GetNextIndex"/> wraps at the ends of the route.</summary>
    public enum PatrolLoopMode
    {
        /// <summary>After the last waypoint, jump back to index 0.</summary>
        Loop,
        /// <summary>After the last waypoint, reverse direction (and again at index 0).</summary>
        PingPong
    }

    /// <summary>
    /// MonoBehaviour container for a guard's patrol path. Stores parallel
    /// position + wait-time lists and exposes the <see cref="IPatrolPath"/>
    /// interface that <see cref="IdleState"/> consumes.
    /// </summary>
    public class PatrolRoute : MonoBehaviour, IPatrolPath
    {
        [SerializeField] private List<Vector2> waypoints = new();
        [SerializeField] private List<float> waitTimes = new();
        [SerializeField] private PatrolLoopMode loopMode = PatrolLoopMode.Loop;

        private int _pingPongDir = 1;

        /// <inheritdoc/>
        public int WaypointCount => waypoints.Count;

        /// <inheritdoc/>
        public Vector2 GetWaypoint(int index)
        {
            if (waypoints.Count == 0) return transform.position;
            return waypoints[Mathf.Clamp(index, 0, waypoints.Count - 1)];
        }

        /// <inheritdoc/>
        public float GetWaitTime(int index)
        {
            if (index < 0 || index >= waitTimes.Count) return 0f;
            return Mathf.Max(0f, waitTimes[index]);
        }

        /// <inheritdoc/>
        public int GetNextIndex(int current)
        {
            if (waypoints.Count <= 1) return 0;

            if (loopMode == PatrolLoopMode.Loop)
                return (current + 1) % waypoints.Count;

            // PingPong
            int next = current + _pingPongDir;
            if (next >= waypoints.Count || next < 0)
            {
                _pingPongDir *= -1;
                next = current + _pingPongDir;
            }
            return Mathf.Clamp(next, 0, waypoints.Count - 1);
        }

        /// <summary>Editor-only accessor for in-place mutation by <see cref="PatrolRouteEditor"/>. Do not call at runtime.</summary>
        public List<Vector2> GetWaypointsRaw() => waypoints;

        /// <summary>Editor-only accessor for in-place mutation of the parallel wait-time list. Do not call at runtime.</summary>
        public List<float> GetWaitTimesRaw() => waitTimes;

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count == 0) return;
            Gizmos.color = new Color(0.4f, 0.8f, 0.4f, 0.6f);
            for (int i = 0; i < waypoints.Count; i++)
            {
                Gizmos.DrawSphere(waypoints[i], 0.12f);
                int next = loopMode == PatrolLoopMode.Loop
                    ? (i + 1) % waypoints.Count
                    : i + 1;
                if (next < waypoints.Count)
                    Gizmos.DrawLine(waypoints[i], waypoints[next]);
            }
        }
    }
}
