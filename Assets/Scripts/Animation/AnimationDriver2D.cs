using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Top-down 2D animation driver. Reads a 2D velocity each frame and switches
    /// the attached <see cref="Animator"/> into the matching directional state
    /// (Idle / MoveUp / MoveDown / MoveLeft / MoveRight) via <c>Animator.Play</c>.
    /// No transitions or float parameters are required on the controller asset.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class AnimationDriver2D : MonoBehaviour
    {
        [SerializeField] private string idleState  = "Idle";
        [SerializeField] private string upState    = "MoveUp";
        [SerializeField] private string downState  = "MoveDown";
        [SerializeField] private string leftState  = "MoveLeft";
        [SerializeField] private string rightState = "MoveRight";

        [Tooltip("Below this speed the driver plays the Idle state.")]
        [SerializeField] private float movingThreshold = 0.05f;

        private Animator _animator;
        private int _currentHash;

        private void Awake() => _animator = GetComponent<Animator>();

        /// <summary>
        /// Selects the directional state matching <paramref name="velocity"/> and
        /// asks the animator to play it. No-ops if already in that state.
        /// </summary>
        /// <param name="velocity">World-space 2D velocity (m/s or wu/s).</param>
        public void Drive(Vector2 velocity)
        {
            string target;
            if (velocity.sqrMagnitude < movingThreshold * movingThreshold)
                target = idleState;
            else if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                target = velocity.x > 0f ? rightState : leftState;
            else
                target = velocity.y > 0f ? upState : downState;

            int targetHash = Animator.StringToHash(target);
            if (_currentHash == targetHash) return;
            _animator.Play(targetHash);
            _currentHash = targetHash;
        }
    }
}
