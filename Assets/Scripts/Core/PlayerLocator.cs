using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Lazy, cached lookup for the single Player GameObject in the scene.
    /// Centralises the "Player" tag string so call-sites don't repeat it,
    /// and avoids the per-call <c>FindWithTag</c> cost after the first hit.
    /// </summary>
    public static class PlayerLocator
    {
        private const string PlayerTag = "Player";
        private static Transform _cached;

        /// <summary>
        /// Returns the player's <see cref="Transform"/> (cached). Re-resolves if
        /// the cached reference dies (e.g. after a scene reload destroys the object).
        /// </summary>
        public static Transform Find()
        {
            if (_cached != null) return _cached;
            var go = GameObject.FindWithTag(PlayerTag);
            _cached = go != null ? go.transform : null;
            return _cached;
        }

        /// <summary>Centralised tag comparison for trigger callbacks etc.</summary>
        public static bool IsPlayer(Component other) =>
            other != null && other.CompareTag(PlayerTag);
    }
}
