using System;
using UnityEngine;

namespace StealthGame
{
    /// <summary>Payload broadcast on the noise channel each time the player emits a footstep / interaction sound.</summary>
    public struct NoiseEventData
    {
        /// <summary>World position where the noise originated.</summary>
        public Vector2 Position;
        /// <summary>Loudness scalar in [0, 1]. Scales the guard's effective hearing radius.</summary>
        public float   Intensity;
    }

    /// <summary>
    /// ScriptableObject event channel for noise emissions. Decouples emitters
    /// (player, future distraction items) from listeners (guards).
    /// </summary>
    [CreateAssetMenu(menuName = "StealthGame/Channels/Noise Event Channel")]
    public class NoiseEventChannelSO : ScriptableObject
    {
        /// <summary>Invoked once per <see cref="Raise"/> call.</summary>
        public event Action<NoiseEventData> OnRaised;

        /// <summary>Broadcasts <paramref name="data"/> to every subscriber.</summary>
        public void Raise(NoiseEventData data) => OnRaised?.Invoke(data);
    }
}
