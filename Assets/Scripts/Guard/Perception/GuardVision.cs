using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Per-frame line-of-sight check against the player. Combines a distance test,
    /// FOV-angle test (against <see cref="FacingDirection"/>), and a single
    /// <c>Physics2D.Raycast</c> against <c>profile.obstacleLayerMask</c>.
    /// </summary>
    public class GuardVision : MonoBehaviour
    {
        private GuardProfile _profile;
        private Transform _playerTransform;

        /// <summary><c>true</c> if the player is currently inside the cone and unobstructed.</summary>
        public bool CanSeePlayer { get; private set; }

        /// <summary>World position of the player at the last frame <see cref="CanSeePlayer"/> was <c>true</c>.</summary>
        public Vector2 LastSeenPosition { get; private set; }

        /// <summary>World-space facing vector; set each frame by <see cref="GuardController"/>.</summary>
        public Vector2 FacingDirection { get; set; } = Vector2.up;

        /// <summary>Bind the tuning profile and the player transform (cached at Awake).</summary>
        public void Initialize(GuardProfile profile, Transform player)
        {
            _profile = profile;
            _playerTransform = player;
        }

        private void Update()
        {
            CanSeePlayer = CheckVision();
            if (CanSeePlayer)
                LastSeenPosition = _playerTransform.position;
        }

        private bool CheckVision()
        {
            if (_playerTransform == null || _profile == null) return false;

            Vector2 origin = transform.position;
            Vector2 toPlayer = (Vector2)_playerTransform.position - origin;
            float dist = toPlayer.magnitude;

            if (dist > _profile.visionRange) return false;

            float angle = Vector2.Angle(FacingDirection, toPlayer);
            if (angle > _profile.fovAngle * 0.5f) return false;

            RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer.normalized, dist, _profile.obstacleLayerMask);
            return hit.collider == null;
        }

        /// <summary>Raycast LoS test to an arbitrary world position. Used for catch checks without full cone math.</summary>
        public bool HasLineOfSightTo(Vector2 worldPos)
        {
            Vector2 origin = transform.position;
            Vector2 dir = worldPos - origin;
            float dist = dir.magnitude;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir.normalized, dist, _profile.obstacleLayerMask);
            return hit.collider == null;
        }
    }
}
