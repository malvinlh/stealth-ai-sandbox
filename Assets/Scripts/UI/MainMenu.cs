using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StealthGame
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private int gameSceneIndex = 1;

        private void Awake()
        {
            int resolvedSceneIndex = ResolveSceneIndex();
            playButton?.onClick.AddListener(() => SceneManager.LoadScene(resolvedSceneIndex));
            quitButton?.onClick.AddListener(Application.Quit);
        }

        private int ResolveSceneIndex()
        {
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            if (sceneCount <= 0) return 0;
            if (gameSceneIndex >= 0 && gameSceneIndex < sceneCount) return gameSceneIndex;
            return 0;
        }
    }
}
