using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthGame
{
    /// <summary>
    /// Any interactive object that emits noise when the player activates it (e.g., a crate, a door).
    /// Attach to an interactable world object.
    /// </summary>
    public class NoiseSource : MonoBehaviour
    {
        [SerializeField] private NoiseEventChannelSO noiseChannel;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private float noiseIntensity = 0.8f;
        [SerializeField] private float interactRadius = 1.5f;

        private Transform _playerTransform;

        private void Awake()
        {
            _playerTransform = PlayerLocator.Find();
        }

        private void OnEnable()
        {
            var action = interactAction?.action;
            if (action == null) return;

            action.performed += OnInteract;
        }

        private void OnDisable()
        {
            var action = interactAction?.action;
            if (action == null) return;

            action.performed -= OnInteract;
        }

        private void OnInteract(InputAction.CallbackContext _)
        {
            if (_playerTransform == null) return;
            if (Vector2.Distance(transform.position, _playerTransform.position) > interactRadius) return;

            noiseChannel?.Raise(new NoiseEventData
            {
                Position  = transform.position,
                Intensity = noiseIntensity
            });
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
