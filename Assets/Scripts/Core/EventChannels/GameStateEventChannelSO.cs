using System;
using UnityEngine;

namespace StealthGame
{
    /// <summary>High-level run-state of the play session.</summary>
    public enum GameState
    {
        /// <summary>Normal play.</summary>
        Playing,
        /// <summary>Player was caught — game over flow.</summary>
        Caught,
        /// <summary>Player reached the objective — win flow.</summary>
        Won,
        /// <summary>Reserved for explicit pause UI; not currently raised by gameplay.</summary>
        Paused
    }

    /// <summary>
    /// ScriptableObject event channel that announces transitions between <see cref="GameState"/> values.
    /// Listeners (GameManager, HUD, etc.) subscribe to react without coupling to each other.
    /// </summary>
    [CreateAssetMenu(menuName = "StealthGame/Channels/Game State Event Channel")]
    public class GameStateEventChannelSO : ScriptableObject
    {
        /// <summary>Invoked once per <see cref="Raise"/> call with the new state.</summary>
        public event Action<GameState> OnRaised;

        /// <summary>Broadcasts <paramref name="state"/> to every subscriber.</summary>
        public void Raise(GameState state) => OnRaised?.Invoke(state);
    }
}
