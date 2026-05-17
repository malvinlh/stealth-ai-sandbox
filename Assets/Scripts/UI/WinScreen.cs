using UnityEngine;
using UnityEngine.UI;

namespace StealthGame
{
    public class WinScreen : MonoBehaviour
    {
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            mainMenuButton?.onClick.AddListener(() => GameManager.Instance?.GoToMainMenu());
            quitButton?.onClick.AddListener(() => GameManager.Instance?.Quit());
        }
    }
}
