using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Tiny polymorphic FSM driver. Owns the active <see cref="IGuardState"/>,
    /// routes <see cref="Tick"/> to it, and handles Enter/Exit dispatch on
    /// transition. Adding a new state = new <see cref="IGuardState"/> class
    /// + one <see cref="ChangeState"/> call somewhere.
    /// </summary>
    public class GuardStateMachine
    {
        private IGuardState _current;
        private readonly GuardContext _ctx;

        /// <summary>The currently-active state instance.</summary>
        public IGuardState Current => _current;

        /// <summary>Constructs the machine, binds it to <paramref name="ctx"/>, and enters <paramref name="initialState"/>.</summary>
        public GuardStateMachine(GuardContext ctx, IGuardState initialState)
        {
            _ctx = ctx;
            _ctx.Machine = this;
            ChangeState(initialState);
        }

        /// <summary>Exits the current state, swaps to <paramref name="next"/>, and calls its <c>Enter</c>. Logs the transition.</summary>
        public void ChangeState(IGuardState next)
        {
#if UNITY_EDITOR
            // Editor-only transition log for live demo; stripped from builds so Player.log stays clean.
            string from = _current?.GetType().Name ?? "-";
            string to   = next?.GetType().Name ?? "-";
            string shortId = (_ctx?.GuardId != null && _ctx.GuardId.Length >= 4) ? _ctx.GuardId[..4] : "????";
            Debug.Log($"[Guard {shortId}] {from} → {to}");
#endif

            _current?.Exit(_ctx);
            _current = next;
            _current.Enter(_ctx);
        }

        /// <summary>Forward the per-frame tick to the active state.</summary>
        public void Tick(float dt) => _current?.Tick(_ctx, dt);
    }
}
