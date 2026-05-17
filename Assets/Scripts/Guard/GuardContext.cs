using UnityEngine;
using UnityEngine.AI;

namespace StealthGame
{
    /// <summary>
    /// Shared "blackboard" passed to every <see cref="IGuardState"/> Tick.
    /// Holds the guard's component references, configuration, perception flags,
    /// and the mutable state-machine bookkeeping (timers, waypoint index, facing).
    /// </summary>
    public class GuardContext
    {
        // --- Identity ---
        /// <summary>Unique per-guard GUID. Used to filter self-broadcasts on event channels.</summary>
        public string GuardId;

        // --- References (set by GuardController.Awake) ---
        /// <summary>Tunables for this guard (radii, speeds, timers).</summary>
        public GuardProfile Profile;
        /// <summary>Kinematic Rigidbody2D — set so the NavMeshAgent can drive the transform.</summary>
        public Rigidbody2D Rb;
        /// <summary>NavMeshAgent that handles all movement / pathfinding.</summary>
        public NavMeshAgent Agent;
        /// <summary>Cached <c>transform</c> reference for convenience.</summary>
        public Transform Tr;
        /// <summary>FOV-and-LOS perception component.</summary>
        public GuardVision Vision;
        /// <summary>Hearing perception component.</summary>
        public GuardHearing Hearing;
        /// <summary>Patrol path; may be null if no route assigned.</summary>
        public PatrolRoute Route;
        /// <summary>Shared inter-guard alert event channel.</summary>
        public AlertEventChannelSO AlertChannel;
        /// <summary>Shared game-state channel (Win/Caught).</summary>
        public GameStateEventChannelSO GameStateChannel;
        /// <summary>Reference to the Player transform, looked up at Awake by tag.</summary>
        public Transform PlayerTransform;
        /// <summary>Back-pointer to the state machine; states call <c>Machine.ChangeState(...)</c>.</summary>
        public GuardStateMachine Machine;
        /// <summary>Mesh-based FOV visual + state-color setter.</summary>
        public FieldOfViewMesh FovMesh;

        // --- Blackboard (mutable per-frame state) ---
        /// <summary>Last position the guard saw the player. Null if never seen / forgotten.</summary>
        public Vector2? LastKnownPlayerPos;
        /// <summary>Investigation point set from hearing or inter-guard comms. Consumed by <see cref="SuspiciousState"/>.</summary>
        public Vector2? SuspicionPoint;
        /// <summary>Index into <see cref="Route"/> currently being patrolled toward.</summary>
        public int CurrentWaypointIdx;
        /// <summary>Generic per-state timer (used by Suspicious for look-around, Searching for total search duration).</summary>
        public float StateTimer;
        /// <summary>Pause timer for the current waypoint's wait time.</summary>
        public float WaypointWaitTimer;

        // World-space unit vector pointing where the guard is "looking".
        // Decoupled from transform.rotation so the sprite/animator can show
        // directional clips without the body itself being rotated.
        public Vector2 Facing = Vector2.up;

        // Hearing / communication flags (set externally, cleared by states after consumption)
        /// <summary>Set by <see cref="GuardHearing"/> when a noise event lands inside the hearing radius.</summary>
        public bool HeardNoise;
        /// <summary>World position of the most recent heard noise.</summary>
        public Vector2 LastHeardNoisePosition;

        /// <summary>Set by <see cref="GuardController.OnAlertReceived"/> when another guard broadcasts an alert.</summary>
        public bool ReceivedCommunicationAlert;
        /// <summary>The alerter guard's last-known-player position; consumed by <see cref="IdleState"/> on the next Tick.</summary>
        public Vector2 CommunicationKnownPosition;
    }
}
