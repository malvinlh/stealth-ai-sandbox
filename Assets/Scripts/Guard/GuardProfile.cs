using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Shared tuning asset for a guard (or a difficulty preset). Holds every
    /// movement, perception, and behaviour value the state machine reads via
    /// <see cref="GuardContext.Profile"/> — no per-script hard-coded numbers.
    /// </summary>
    [CreateAssetMenu(menuName = "StealthGame/Guard Profile")]
    public class GuardProfile : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Wu/s while patrolling (Idle state).")]
        public float patrolSpeed = 2f;
        [Tooltip("Wu/s while investigating (Suspicious / Searching states).")]
        public float suspiciousSpeed = 2.5f;
        [Tooltip("Wu/s while pursuing (Alert state). Should sit between walk and sprint to make sprint just-barely escape.")]
        public float alertSpeed = 4f;
        [Tooltip("Degrees-per-second the guard's facing direction rotates toward the agent velocity.")]
        public float rotationSpeed = 180f;

        [Header("Vision")]
        [Tooltip("World units the cone reaches in a straight line.")]
        public float visionRange = 6f;
        [Tooltip("Total cone angle in degrees (split half-and-half around the facing direction).")]
        [Range(10f, 360f)] public float fovAngle = 90f;
        [Tooltip("Number of rays cast per frame to build the mesh. Higher = smoother cone, more cost.")]
        [Range(10, 120)] public int coneResolution = 60;
        [Tooltip("Layers that block vision (typically Obstacle). Used by both the detection raycast and the mesh fan.")]
        public LayerMask obstacleLayerMask;

        [Header("Hearing")]
        [Tooltip("Maximum world units a sprinting player (intensity 1.0) can be heard from. Walk (0.5) hears at half.")]
        public float hearingRadius = 4f;

        [Header("Communication")]
        [Tooltip("Euclidean radius (wu) within which other guards react to this guard's alert broadcast.")]
        public float communicationRadius = 8f;

        [Header("Behaviour")]
        [Tooltip("Seconds the guard spends looking around at the suspicion point after arriving.")]
        public float suspicionDuration = 3f;
        [Tooltip("Seconds the guard wanders during SearchingState before giving up.")]
        public float searchDuration = 6f;
        [Tooltip("World-unit distance at which the guard 'catches' the player while in AlertState with LOS.")]
        public float caughtRange = 0.8f;
        [Tooltip("World-unit radius around last-known-player position used to pick wander targets in SearchingState.")]
        public float searchWanderRadius = 3f;
        [Tooltip("How close (wu) the agent must be to its destination to count as 'arrived'.")]
        public float waypointArriveThreshold = 0.15f;
    }
}
