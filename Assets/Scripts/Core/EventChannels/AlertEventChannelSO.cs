using System;
using UnityEngine;

namespace StealthGame
{
    /// <summary>Payload broadcast on the alert channel when a guard enters or leaves AlertState.</summary>
    public struct AlertEventData
    {
        /// <summary>GUID of the broadcasting guard. Used by listeners to ignore self-broadcasts.</summary>
        public string  AlerterGuid;
        /// <summary>World position of the broadcasting guard at the moment of the alert.</summary>
        public Vector2 AlerterPosition;
        /// <summary>The broadcasting guard's last-known-player-position; used by listeners as their suspicion target.</summary>
        public Vector2 LastKnownPlayerPos;
        /// <summary><c>true</c> when the guard entered Alert; <c>false</c> when it left Alert.</summary>
        public bool    IsStarting;
    }

    /// <summary>
    /// ScriptableObject event channel for inter-guard alerts. Guards subscribe in
    /// <c>OnEnable</c> and raise via <see cref="Raise"/>. Decouples guards from each
    /// other — no direct references.
    /// </summary>
    [CreateAssetMenu(menuName = "StealthGame/Channels/Alert Event Channel")]
    public class AlertEventChannelSO : ScriptableObject
    {
        /// <summary>Invoked once per <see cref="Raise"/> call. Subscribers receive the payload.</summary>
        public event Action<AlertEventData> OnRaised;

        /// <summary>Broadcasts <paramref name="data"/> to every subscriber.</summary>
        public void Raise(AlertEventData data) => OnRaised?.Invoke(data);
    }
}
