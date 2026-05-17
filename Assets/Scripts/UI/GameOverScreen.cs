using UnityEngine;
using UnityEngine.UI;

namespace StealthGame
{
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            retryButton?.onClick.AddListener(() => GameManager.Instance?.Retry());
            mainMenuButton?.onClick.AddListener(() => GameManager.Instance?.GoToMainMenu());
        }
    }
}
