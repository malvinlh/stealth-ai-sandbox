namespace StealthGame
{
    /// <summary>
    /// Polymorphic guard state. Implementations live in <c>StateMachine/States/</c> —
    /// one class per state, transitioned via <see cref="GuardStateMachine.ChangeState"/>.
    /// </summary>
    public interface IGuardState
    {
        /// <summary>Invoked once when this state becomes active. Set entry SFX, gizmo colour, blackboard, etc.</summary>
        void Enter(GuardContext ctx);

        /// <summary>Invoked every frame while active. Read perception, drive movement, request transitions.</summary>
        /// <param name="dt">Delta time in seconds.</param>
        void Tick(GuardContext ctx, float dt);

        /// <summary>Invoked once when this state is leaving. Clean up agent paths, raise exit events, etc.</summary>
        void Exit(GuardContext ctx);
    }
}
