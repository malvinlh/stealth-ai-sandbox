using UnityEngine;

namespace StealthGame
{
    public class ObjectiveZone : MonoBehaviour
    {
        [SerializeField] private GameStateEventChannelSO gameStateChannel;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (PlayerLocator.IsPlayer(other))
                gameStateChannel?.Raise(GameState.Won);
        }
    }
}
