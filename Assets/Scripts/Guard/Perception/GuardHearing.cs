using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Listens to a <see cref="NoiseEventChannelSO"/> and writes <c>HeardNoise</c> /
    /// <c>LastHeardNoisePosition</c> on the <see cref="GuardContext"/> when the noise
    /// falls within the guard's effective hearing radius (<c>profile.hearingRadius × intensity</c>).
    /// </summary>
    public class GuardHearing : MonoBehaviour
    {
        private NoiseEventChannelSO _noiseChannel;
        private GuardContext _ctx;

        /// <summary>Called by <see cref="GuardController.Awake"/> to bind context + channel. Subscribes to the channel.</summary>
        public void Initialize(GuardContext ctx, NoiseEventChannelSO noiseChannel)
        {
            _ctx = ctx;
            _noiseChannel = noiseChannel;
            if (_noiseChannel != null)
                _noiseChannel.OnRaised += OnNoiseRaised;
        }

        private void OnDestroy()
        {
            if (_noiseChannel != null)
                _noiseChannel.OnRaised -= OnNoiseRaised;
        }

        private void OnNoiseRaised(NoiseEventData data)
        {
            if (_ctx == null) return;
            float effectiveRadius = _ctx.Profile.hearingRadius * data.Intensity;
            if (Vector2.Distance(transform.position, data.Position) <= effectiveRadius)
            {
                _ctx.HeardNoise = true;
                _ctx.LastHeardNoisePosition = data.Position;
            }
        }
    }
}
