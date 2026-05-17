using System;
using UnityEngine;
using UnityEngine.AI;

namespace StealthGame
{
    /// <summary>
    /// Top-level guard MonoBehaviour. Wires up perception, the NavMeshAgent, and
    /// the <see cref="GuardStateMachine"/>. Subscribes to the shared
    /// <see cref="AlertEventChannelSO"/> so other guards' alerts can flip this
    /// guard into Suspicious. Never holds a direct reference to another guard.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(GuardVision), typeof(GuardHearing))]
    public class GuardController : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Tuning asset (radii, speeds, timers). Can be shared between guards or duplicated for difficulty variants.")]
        [SerializeField] private GuardProfile profile;
        [Tooltip("Per-guard patrol route. Null = guard idles on the spot.")]
        [SerializeField] private PatrolRoute patrolRoute;

        [Header("Channels")]
        [Tooltip("Shared inter-guard alert event channel.")]
        [SerializeField] private AlertEventChannelSO alertChannel;
        [Tooltip("Shared noise event channel emitted by PlayerNoiseEmitter / NoiseSource.")]
        [SerializeField] private NoiseEventChannelSO noiseChannel;
        [Tooltip("Shared game-state event channel (Won/Caught).")]
        [SerializeField] private GameStateEventChannelSO gameStateChannel;

        [Header("Animation")]
        [Tooltip("Optional. If empty, Awake searches the GameObject + children for a component.")]
        [SerializeField] private AnimationDriver2D animationDriver;

        private GuardContext _ctx;
        private GuardStateMachine _stateMachine;
        private FieldOfViewMesh _fovMesh;
        private AnimationDriver2D _anim;
        private GuardVision _vision;

        private void Awake()
        {
            _anim = animationDriver != null ? animationDriver : GetComponentInChildren<AnimationDriver2D>(true);

            // Build FOV mesh child
            var fovGo = new GameObject("FOVMesh");
            fovGo.transform.SetParent(transform, false);
            _fovMesh = fovGo.AddComponent<FieldOfViewMesh>();
            _fovMesh.Initialize(profile);

            // Find player (centralised lookup; tag string + cache live in PlayerLocator)
            Transform playerTr = PlayerLocator.Find();

            // Init perception
            _vision = GetComponent<GuardVision>();
            _vision.Initialize(profile, playerTr);

            var hearing = GetComponent<GuardHearing>();

            // NavMeshAgent — drives movement. Configure for 2D top-down use.
            var agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogWarning("[GuardController] No NavMeshAgent on Guard - add one to the prefab and bake a 2D NavMesh.", this);
            }
            else
            {
                agent.updateRotation = false;     // we rotate the sprite ourselves
                agent.updateUpAxis  = false;      // keep Z-up disabled for 2D
                agent.angularSpeed  = 0f;
                agent.acceleration  = 50f;        // snappy
                agent.autoBraking   = true;
            }

            // Rigidbody2D must not fight the agent's transform updates
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

            // Build context
            _ctx = new GuardContext
            {
                GuardId         = Guid.NewGuid().ToString(),
                Profile         = profile,
                Rb              = rb,
                Agent           = agent,
                Tr              = transform,
                Vision          = _vision,
                Hearing         = hearing,
                Route           = patrolRoute,
                AlertChannel    = alertChannel,
                GameStateChannel = gameStateChannel,
                PlayerTransform = playerTr,
                FovMesh         = _fovMesh,
            };

            hearing.Initialize(_ctx, noiseChannel);

            _stateMachine = new GuardStateMachine(_ctx, new IdleState());
        }

        private void OnEnable()
        {
            if (alertChannel != null)
                alertChannel.OnRaised += OnAlertReceived;
        }

        private void OnDisable()
        {
            if (alertChannel != null)
                alertChannel.OnRaised -= OnAlertReceived;
        }

        private void Update()
        {
            _stateMachine.Tick(Time.deltaTime);

            // Propagate the decoupled facing vector to perception components and the
            // FOV mesh so they track the agent's heading without rotating the transform.
            if (_fovMesh != null) _fovMesh.FacingDirection = _ctx.Facing;
            if (_vision  != null) _vision.FacingDirection  = _ctx.Facing;

            _anim?.Drive(_ctx.Agent != null ? _ctx.Agent.velocity : Vector2.zero);
        }

        private void OnAlertReceived(AlertEventData data)
        {
            // Ignore our own broadcasts and only react to alerts being raised (not cleared)
            if (data.AlerterGuid == _ctx.GuardId) return;
            if (!data.IsStarting) return;

            // Pure radius trigger per PDF spec — wall-blocking is NOT required.
            // The alerted guard's subsequent movement to LastKnownPlayerPos still routes through NavMesh
            // via NavMeshAgent.SetDestination inside SuspiciousState/AlertState.
            float dist = Vector2.Distance(transform.position, data.AlerterPosition);

#if UNITY_EDITOR
            string myId   = (_ctx?.GuardId   != null && _ctx.GuardId.Length   >= 4) ? _ctx.GuardId[..4]   : "????";
            string fromId = (data.AlerterGuid != null && data.AlerterGuid.Length >= 4) ? data.AlerterGuid[..4] : "????";
            Debug.Log($"[Guard {myId}] alert received from {fromId} at dist={dist:0.00} (radius={profile.communicationRadius})");
#endif

            if (dist > profile.communicationRadius)
            {
#if UNITY_EDITOR
                Debug.Log($"[Guard {myId}] out of range - ignored");
#endif
                return;
            }

            // Only interrupt patrol/suspicious, not alert/searching already in progress
            if (_stateMachine.Current is AlertState || _stateMachine.Current is SearchingState)
            {
#if UNITY_EDITOR
                Debug.Log($"[Guard {myId}] already busy ({_stateMachine.Current.GetType().Name}) — ignored");
#endif
                return;
            }

            _ctx.ReceivedCommunicationAlert = true;
            _ctx.CommunicationKnownPosition = data.LastKnownPlayerPos;
#if UNITY_EDITOR
            Debug.Log($"[Guard {myId}] comm flag SET, target={data.LastKnownPlayerPos}");
#endif
        }

        // --- Gizmos ---
        private void OnDrawGizmosSelected()
        {
            if (profile == null) return;

            Vector2 facing = _ctx != null ? _ctx.Facing : (Vector2)transform.up;

            // Vision arc (white in editor, actual cone handled by FieldOfViewMesh at runtime)
            Gizmos.color = new Color(1f, 1f, 0.5f, 0.4f);
            DrawArc(transform.position, facing, profile.fovAngle, profile.visionRange);

            // Hearing radius
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.3f);
            DrawCircle(transform.position, profile.hearingRadius);

            // Communication radius
            Gizmos.color = new Color(1f, 0.4f, 0.8f, 0.2f);
            DrawCircle(transform.position, profile.communicationRadius);

            // State label in scene view
#if UNITY_EDITOR
            if (Application.isPlaying && _stateMachine != null)
            {
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.15f,
                    _stateMachine.Current?.GetType().Name ?? "—");
            }
#endif
        }

        private void OnDrawGizmos()
        {
            if (profile == null) return;
            Vector2 facing = _ctx != null ? _ctx.Facing : (Vector2)transform.up;
            // Faint vision cone — always visible so the level designer can read coverage at a glance.
            Gizmos.color = new Color(1f, 1f, 0.5f, 0.15f);
            DrawArc(transform.position, facing, profile.fovAngle, profile.visionRange);
            // Faint communication radius — always visible so guard-to-guard overlap is readable in scene view.
            Gizmos.color = new Color(1f, 0.4f, 0.8f, 0.10f);
            DrawCircle(transform.position, profile.communicationRadius);
        }

        private static void DrawArc(Vector3 origin, Vector2 forward, float angleDeg, float radius)
        {
            float halfFov = angleDeg * 0.5f;
            int segments = 20;
            Vector3 prev = origin + (Vector3)RotateDir(forward, -halfFov) * radius;
            for (int i = 1; i <= segments; i++)
            {
                float t = -halfFov + i * (angleDeg / segments);
                Vector3 next = origin + (Vector3)RotateDir(forward, t) * radius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
            Gizmos.DrawLine(origin, origin + (Vector3)RotateDir(forward, -halfFov) * radius);
            Gizmos.DrawLine(origin, origin + (Vector3)RotateDir(forward,  halfFov) * radius);
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i       * 360f / segments * Mathf.Deg2Rad;
                float a1 = (i + 1) * 360f / segments * Mathf.Deg2Rad;
                Gizmos.DrawLine(
                    center + new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius),
                    center + new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius));
            }
        }

        private static Vector2 RotateDir(Vector2 dir, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            return new Vector2(dir.x * cos + dir.y * sin, -dir.x * sin + dir.y * cos);
        }
    }
}
