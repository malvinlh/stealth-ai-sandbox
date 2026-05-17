using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthGame
{
    /// <summary>
    /// Top-down 2D player. Reads Move/Crouch/Sprint actions, picks the active
    /// <see cref="PlayerStanceSO"/> each FixedUpdate, drives the Rigidbody2D velocity,
    /// and forwards the velocity to an optional <see cref="AnimationDriver2D"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Stances")]
        [SerializeField] private PlayerStanceSO crouchStance;
        [SerializeField] private PlayerStanceSO walkStance;
        [SerializeField] private PlayerStanceSO sprintStance;

        [Header("Base Speed")]
        [SerializeField] private float walkSpeed = 3f;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference crouchAction;
        [SerializeField] private InputActionReference sprintAction;

        [Header("Animation")]
        [Tooltip("Optional. If empty, Awake searches the GameObject + children for a component.")]
        [SerializeField] private AnimationDriver2D animationDriver;

        /// <summary>The stance asset selected this frame (Crouch / Walk / Sprint).</summary>
        public PlayerStanceSO CurrentStance { get; private set; }

        /// <summary>Latest move-input vector read from the New Input System. Updated in FixedUpdate.</summary>
        public Vector2 MoveInput { get; private set; }

        private Rigidbody2D _rb;
        private AnimationDriver2D _anim;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _anim = animationDriver != null ? animationDriver : GetComponentInChildren<AnimationDriver2D>(true);
            CurrentStance = walkStance;
        }

        private void OnEnable()
        {
            moveAction?.action.Enable();
            crouchAction?.action.Enable();
            sprintAction?.action.Enable();
        }

        private void OnDisable()
        {
            moveAction?.action.Disable();
            crouchAction?.action.Disable();
            sprintAction?.action.Disable();
        }

        private void FixedUpdate()
        {
            MoveInput = moveAction?.action.ReadValue<Vector2>() ?? Vector2.zero;

            bool sprinting = sprintAction?.action.IsPressed() ?? false;
            bool crouching = crouchAction?.action.IsPressed() ?? false;

            if (sprinting && MoveInput.sqrMagnitude > 0.01f)
                CurrentStance = sprintStance;
            else if (crouching)
                CurrentStance = crouchStance;
            else
                CurrentStance = walkStance;

            float speed = walkSpeed * CurrentStance.speedMultiplier;
            _rb.linearVelocity = MoveInput * speed;

            _anim?.Drive(_rb.linearVelocity);
        }
    }
}
