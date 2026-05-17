using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Two-phase investigation state. First **travels** to <c>ctx.SuspicionPoint</c>
    /// via NavMesh (with a travel-timeout safety cap), then **looks around** in place
    /// for <c>profile.suspicionDuration</c> before returning to <see cref="IdleState"/>.
    /// Transitions to <see cref="AlertState"/> if vision picks up the player.
    /// </summary>
    public class SuspiciousState : IGuardState
    {
        // Two-phase Suspicious: first travel to the suspicion point, then look around.
        // suspicionDuration counts only during the look-around phase, so the guard
        // always reaches the noise/last-known position before giving up.
        private const float TravelTimeoutMultiplier = 2.5f;

        private bool _arrived;
        private float _travelTimer;

        public void Enter(GuardContext ctx)
        {
            ctx.StateTimer = 0f;
            _arrived       = false;
            _travelTimer   = 0f;
            ctx.FovMesh?.SetStateColor(GuardStateType.Suspicious);
            AudioManager.Instance?.PlaySfx(AudioManager.Instance.guardSuspiciousCue);
            GameManager.Instance?.SpawnExclamation(ctx.Tr, "?");
        }

        public void Tick(GuardContext ctx, float dt)
        {
            // Vision — override everything
            if (ctx.Vision.CanSeePlayer)
            {
                ctx.LastKnownPlayerPos = ctx.Vision.LastSeenPosition;
                ctx.Machine.ChangeState(new AlertState());
                return;
            }

            // New noise heard mid-investigation — re-target and restart travel phase
            if (ctx.HeardNoise)
            {
                ctx.SuspicionPoint = ctx.LastHeardNoisePosition;
                ctx.HeardNoise     = false;
                _arrived           = false;
                _travelTimer       = 0f;
                ctx.StateTimer     = 0f;
            }

            if (!ctx.SuspicionPoint.HasValue || ctx.Agent == null || !ctx.Agent.isOnNavMesh)
            {
                ctx.Machine.ChangeState(new IdleState());
                return;
            }

            if (!_arrived)
            {
                // --- Travel phase ---
                ctx.Agent.speed = ctx.Profile.suspiciousSpeed;
                ctx.Agent.SetDestination(ctx.SuspicionPoint.Value);

                _travelTimer += dt;
                if (_travelTimer >= ctx.Profile.suspicionDuration * TravelTimeoutMultiplier)
                {
                    // Path blocked or too far — give up
                    ctx.Machine.ChangeState(new IdleState());
                    return;
                }

                if (!ctx.Agent.pathPending && ctx.Agent.remainingDistance <= ctx.Profile.waypointArriveThreshold)
                {
                    _arrived = true;
                    ctx.Agent.ResetPath();
                    ctx.StateTimer = 0f;
                }
                else
                {
                    Vector2 vel = ctx.Agent.velocity;
                    if (vel.sqrMagnitude > 0.0001f) IdleState.RotateToward(ctx, vel.normalized, dt);
                }
            }
            else
            {
                // --- Look-around phase ---
                ctx.StateTimer += dt;
                if (ctx.StateTimer >= ctx.Profile.suspicionDuration)
                {
                    ctx.Machine.ChangeState(new IdleState());
                    return;
                }

                const float lookAroundSpeed = 60f;
                float rad = lookAroundSpeed * dt * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                ctx.Facing = new Vector2(
                    ctx.Facing.x * cos - ctx.Facing.y * sin,
                    ctx.Facing.x * sin + ctx.Facing.y * cos);
            }
        }

        public void Exit(GuardContext ctx)
        {
            if (ctx.Agent != null && ctx.Agent.isOnNavMesh) ctx.Agent.ResetPath();
            ctx.SuspicionPoint = null;
        }
    }
}
