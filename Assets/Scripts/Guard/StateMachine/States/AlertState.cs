using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Active pursuit. Navigates via NavMesh to <c>ctx.LastKnownPlayerPos</c>, broadcasts
    /// an alert event on Enter/Exit, and raises <see cref="GameState.Caught"/> when the
    /// player is visible and within <c>profile.caughtRange</c>. On arrival with no LOS,
    /// transitions to <see cref="SearchingState"/>.
    /// </summary>
    public class AlertState : IGuardState
    {
        public void Enter(GuardContext ctx)
        {
            ctx.StateTimer = 0f;
            ctx.FovMesh?.SetStateColor(GuardStateType.Alert);
            AudioManager.Instance?.PlaySfx(AudioManager.Instance.guardSpotCue);
            GameManager.Instance?.SpawnExclamation(ctx.Tr, "!");
            GameManager.Instance?.OnGuardAlerted(+1);

            ctx.AlertChannel?.Raise(new AlertEventData
            {
                AlerterGuid        = ctx.GuardId,
                AlerterPosition    = ctx.Tr.position,
                LastKnownPlayerPos = ctx.LastKnownPlayerPos ?? (Vector2)ctx.Tr.position,
                IsStarting         = true
            });
        }

        public void Tick(GuardContext ctx, float dt)
        {
            // Update last known while player is still visible
            if (ctx.Vision.CanSeePlayer)
            {
                ctx.LastKnownPlayerPos = ctx.Vision.LastSeenPosition;

                // Caught check
                if (ctx.PlayerTransform == null) return;
                float distToPlayer = Vector2.Distance(ctx.Tr.position, ctx.PlayerTransform.position);
                if (distToPlayer <= ctx.Profile.caughtRange)
                {
                    ctx.GameStateChannel?.Raise(GameState.Caught);
                    return;
                }
            }

            if (!ctx.LastKnownPlayerPos.HasValue)
            {
                ctx.Machine.ChangeState(new IdleState());
                return;
            }

            if (ctx.Agent == null || !ctx.Agent.isOnNavMesh) return;

            ctx.Agent.speed = ctx.Profile.alertSpeed;
            ctx.Agent.SetDestination(ctx.LastKnownPlayerPos.Value);

            if (!ctx.Agent.pathPending && ctx.Agent.remainingDistance <= ctx.Profile.waypointArriveThreshold)
            {
                // Reached last known position but no visual — start searching
                ctx.Agent.ResetPath();
                if (!ctx.Vision.CanSeePlayer)
                {
                    ctx.Machine.ChangeState(new SearchingState());
                }
            }
            else
            {
                Vector2 vel = ctx.Agent.velocity;
                if (vel.sqrMagnitude > 0.0001f) IdleState.RotateToward(ctx, vel.normalized, dt);
            }
        }

        public void Exit(GuardContext ctx)
        {
            if (ctx.Agent != null && ctx.Agent.isOnNavMesh) ctx.Agent.ResetPath();
            GameManager.Instance?.OnGuardAlerted(-1);

            ctx.AlertChannel?.Raise(new AlertEventData
            {
                AlerterGuid        = ctx.GuardId,
                AlerterPosition    = ctx.Tr.position,
                LastKnownPlayerPos = ctx.LastKnownPlayerPos ?? (Vector2)ctx.Tr.position,
                IsStarting         = false
            });
        }
    }
}
