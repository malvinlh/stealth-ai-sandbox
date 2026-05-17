using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Abstract patrol path consumed by <see cref="IdleState"/>. Lets a guard query
    /// the waypoint list without knowing the concrete <see cref="PatrolRoute"/>.
    /// </summary>
    public interface IPatrolPath
    {
        /// <summary>Total number of waypoints. Zero means "no patrol assigned".</summary>
        int WaypointCount { get; }

        /// <summary>World-space position of waypoint <paramref name="index"/>. Clamped to the valid range.</summary>
        Vector2 GetWaypoint(int index);

        /// <summary>Returns the next waypoint index given the current one (loop or ping-pong, depending on implementation).</summary>
        int GetNextIndex(int current);

        /// <summary>How long (seconds) the guard should pause at waypoint <paramref name="index"/> before advancing. 0 = pass-through.</summary>
        float GetWaitTime(int index);
    }
}
