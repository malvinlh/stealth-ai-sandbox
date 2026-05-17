using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Emits a footstep noise event each <c>stance.footstepInterval</c> seconds while the
    /// player is moving. Plays the matching footstep SFX (walk or sprint; crouch silent)
    /// and raises a <see cref="NoiseEventChannelSO"/> event so guards can hear.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerNoiseEmitter : MonoBehaviour
    {
        [SerializeField] private NoiseEventChannelSO noiseChannel;

        private PlayerController _player;
        private float _footstepTimer;

        private void Awake() => _player = GetComponent<PlayerController>();

        private void FixedUpdate()
        {
            if (_player.MoveInput.sqrMagnitude < 0.01f) return;

            var stance = _player.CurrentStance;
            if (stance == null || stance.noiseIntensity <= 0f) return;

            _footstepTimer -= Time.fixedDeltaTime;
            if (_footstepTimer > 0f) return;

            _footstepTimer = stance.footstepInterval;

            noiseChannel?.Raise(new NoiseEventData
            {
                Position  = transform.position,
                Intensity = stance.noiseIntensity
            });

            // Footstep SFX — walk less frequent than sprint via per-stance footstepInterval (walk 0.40 s vs sprint 0.22 s)
            var am = AudioManager.Instance;
            if (am == null) return;
            if (stance.noiseIntensity >= 1f)
                am.PlaySfx(am.footstepSprintCue);
            else
                am.PlaySfx(am.footstepWalkCue);
        }
    }
}
