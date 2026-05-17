using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Default state. Patrols the assigned <see cref="PatrolRoute"/> if any (otherwise idle).
    /// Listens for vision, hearing, and inter-guard alerts; transitions:
    ///   • see player → <see cref="AlertState"/>
    ///   • heard noise → <see cref="SuspiciousState"/>
    ///   • comm alert → <see cref="SuspiciousState"/>
    /// </summary>
    public class IdleState : IGuardState
    {
        public void Enter(GuardContext ctx)
        {
            ctx.StateTimer = 0f;
            ctx.FovMesh?.SetStateColor(GuardStateType.Idle);
            AudioManager.Instance?.PlaySfx(AudioManager.Instance.guardLoseSightCue);

            // Clear stale perception flags so a noise/comm event left over from a
            // previous higher-alert state doesn't immediately re-trigger Suspicious.
            ctx.HeardNoise = false;
            ctx.ReceivedCommunicationAlert = false;
            ctx.WaypointWaitTimer = 0f;
        }

        public void Tick(GuardContext ctx, float dt)
        {
            // Vision — highest priority
            if (ctx.Vision.CanSeePlayer)
            {
                ctx.LastKnownPlayerPos = ctx.Vision.LastSeenPosition;
                ctx.Machine.ChangeState(new AlertState());
                return;
            }

            // Hearing
            if (ctx.HeardNoise)
            {
                ctx.SuspicionPoint = ctx.LastHeardNoisePosition;
                ctx.HeardNoise = false;
                ctx.Machine.ChangeState(new SuspiciousState());
                return;
            }

            // Alert communication from another guard
            if (ctx.ReceivedCommunicationAlert)
            {
                ctx.SuspicionPoint = ctx.CommunicationKnownPosition;
                ctx.ReceivedCommunicationAlert = false;
                ctx.Machine.ChangeState(new SuspiciousState());
                return;
            }

            if (ctx.Route == null || ctx.Route.WaypointCount == 0) return;
            if (ctx.Agent == null || !ctx.Agent.isOnNavMesh) return;

            Vector2 target = ctx.Route.GetWaypoint(ctx.CurrentWaypointIdx);
            ctx.Agent.speed = ctx.Profile.patrolSpeed;
            ctx.Agent.SetDestination(target);

            if (!ctx.Agent.pathPending && ctx.Agent.remainingDistance <= ctx.Profile.waypointArriveThreshold)
            {
                // Wait at this waypoint if it has a configured wait time (0 = pass-through)
                ctx.WaypointWaitTimer += dt;
                if (ctx.WaypointWaitTimer >= ctx.Route.GetWaitTime(ctx.CurrentWaypointIdx))
                {
                    ctx.WaypointWaitTimer = 0f;
                    ctx.CurrentWaypointIdx = ctx.Route.GetNextIndex(ctx.CurrentWaypointIdx);
                }
                return;
            }

            // While moving along the route, reset the wait timer so a partial pause
            // doesn't carry over to the next waypoint if vision/hearing interrupts us.
            ctx.WaypointWaitTimer = 0f;

            Vector2 vel = ctx.Agent.velocity;
            if (vel.sqrMagnitude > 0.0001f) RotateToward(ctx, vel.normalized, dt);
        }

        public void Exit(GuardContext ctx)
        {
            if (ctx.Agent != null && ctx.Agent.isOnNavMesh) ctx.Agent.ResetPath();
        }

        // Smooth turn toward dir at Profile.rotationSpeed deg/s. Caller passes the
        // agent's velocity direction so the FOV cone tracks the heading without snapping.
        internal static void RotateToward(GuardContext ctx, Vector2 dir, float dt)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            float currentAngle = Mathf.Atan2(ctx.Facing.y, ctx.Facing.x) * Mathf.Rad2Deg;
            float targetAngle  = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float newAngle     = Mathf.MoveTowardsAngle(currentAngle, targetAngle, ctx.Profile.rotationSpeed * dt);
            float rad          = newAngle * Mathf.Deg2Rad;
            ctx.Facing         = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}
