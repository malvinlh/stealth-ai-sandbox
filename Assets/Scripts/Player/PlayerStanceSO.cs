using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Per-stance tuning (Crouch / Walk / Sprint). Multiplies <c>PlayerController.walkSpeed</c>
    /// and controls how loud each footstep is for guard hearing.
    /// </summary>
    [CreateAssetMenu(menuName = "StealthGame/Player Stance")]
    public class PlayerStanceSO : ScriptableObject
    {
        [Tooltip("Multiplier applied to PlayerController.walkSpeed. Crouch < 1, Sprint > 1.")]
        public float speedMultiplier  = 1f;

        [Tooltip("Noise intensity per footstep. 0 = silent (crouch), 1 = max (sprint). Scales the guard's effective hearing radius.")]
        public float noiseIntensity   = 0.5f;

        [Tooltip("Seconds between footstep noise events / SFX. Lower = faster cadence.")]
        public float footstepInterval = 0.4f;

        [Tooltip("Target local scale for the sprite when this stance is active. (1,1,1) = no squash.")]
        public Vector3 spriteScale    = Vector3.one;
    }
}
