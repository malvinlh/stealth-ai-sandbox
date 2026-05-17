using UnityEngine;
using UnityEngine.AI;

namespace StealthGame
{
    /// <summary>
    /// Post-Alert sweep. Wanders within <c>profile.searchWanderRadius</c> of the
    /// last-known-player position for <c>profile.searchDuration</c> seconds,
    /// snapping each wander target to the NavMesh. Re-acquires the player via
    /// vision → <see cref="AlertState"/>; otherwise expires to <see cref="IdleState"/>.
    /// </summary>
    public class SearchingState : IGuardState
    {
        private Vector2 _wanderTarget;

        public void Enter(GuardContext ctx)
        {
            ctx.StateTimer = 0f;
            ctx.FovMesh?.SetStateColor(GuardStateType.Searching);
            PickNewWanderTarget(ctx);
        }

        public void Tick(GuardContext ctx, float dt)
        {
            if (ctx.Vision.CanSeePlayer)
            {
                ctx.LastKnownPlayerPos = ctx.Vision.LastSeenPosition;
                ctx.Machine.ChangeState(new AlertState());
                return;
            }

            ctx.StateTimer += dt;
            if (ctx.StateTimer >= ctx.Profile.searchDuration)
            {
                ctx.Machine.ChangeState(new IdleState());
                return;
            }

            if (ctx.Agent == null || !ctx.Agent.isOnNavMesh) return;

            ctx.Agent.speed = ctx.Profile.suspiciousSpeed;
            ctx.Agent.SetDestination(_wanderTarget);

            if (!ctx.Agent.pathPending && ctx.Agent.remainingDistance <= ctx.Profile.waypointArriveThreshold)
            {
                PickNewWanderTarget(ctx);
                return;
            }

            Vector2 vel = ctx.Agent.velocity;
            if (vel.sqrMagnitude > 0.0001f) IdleState.RotateToward(ctx, vel.normalized, dt);
        }

        public void Exit(GuardContext ctx)
        {
            if (ctx.Agent != null && ctx.Agent.isOnNavMesh) ctx.Agent.ResetPath();
            ctx.LastKnownPlayerPos = null;

            // Drop any stale perception that accumulated during the search so the
            // guard returns to Idle cleanly rather than blipping back to Suspicious.
            ctx.HeardNoise = false;
            ctx.ReceivedCommunicationAlert = false;
        }

        private void PickNewWanderTarget(GuardContext ctx)
        {
            if (!ctx.LastKnownPlayerPos.HasValue) return;
            Vector2 offset = Random.insideUnitCircle * ctx.Profile.searchWanderRadius;
            Vector2 candidate = ctx.LastKnownPlayerPos.Value + offset;

            // Snap to NavMesh so SetDestination always has a reachable target
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, ctx.Profile.searchWanderRadius, NavMesh.AllAreas))
                _wanderTarget = hit.position;
            else
                _wanderTarget = ctx.LastKnownPlayerPos.Value;
        }
    }
}
