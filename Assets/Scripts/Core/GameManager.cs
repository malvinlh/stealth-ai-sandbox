using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StealthGame
{
    /// <summary>
    /// Singleton orchestrator for the run-time game state. Listens to the
    /// <see cref="GameStateEventChannelSO"/>; on Caught/Won, plays the sting SFX,
    /// runs camera shake / hit-stop / UI fade-in, and pauses the simulation.
    /// Also exposes helpers used by guards (alert counter, exclamation spawn).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>Scene-wide singleton; assigned in <c>Awake</c>.</summary>
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameStateEventChannelSO gameStateChannel;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameObject winScreen;
        [SerializeField] private CameraShake cameraShake;

        [Header("Juice")]
        [SerializeField] private float hitStopDuration = 0.1f;
        [SerializeField] private GameObject exclamationPrefab; // "!" floating text prefab
        [SerializeField] private GameObject questionPrefab;    // "?" floating text prefab

        [Header("Exclamation FX")]
        [Tooltip("World-unit Y offset above the guard where the !/? prefab is spawned.")]
        [SerializeField] private float exclamationHeadOffset = 0.12f;
        [Tooltip("Seconds before the spawned exclamation auto-destroys.")]
        [SerializeField] private float exclamationLifetime = 1.5f;

        private int _alertCount;
        private bool _gameEnded;

        // Per-guard exclamation tracker: keyed by parent Transform so a new spawn
        // can replace the old one immediately on state change.
        private readonly Dictionary<Transform, GameObject> _activeExclamations = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (gameStateChannel != null)
                gameStateChannel.OnRaised += OnGameState;
        }

        private void OnDisable()
        {
            if (gameStateChannel != null)
                gameStateChannel.OnRaised -= OnGameState;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (winScreen != null)     winScreen.SetActive(false);
        }

        private void OnGameState(GameState state)
        {
            if (_gameEnded) return;

            if (state == GameState.Caught)
                StartCoroutine(HandleCaught());
            else if (state == GameState.Won)
                StartCoroutine(HandleWon());
        }

        private IEnumerator HandleCaught()
        {
            _gameEnded = true;
            AudioManager.Instance?.PlaySfx(AudioManager.Instance.caughtStingCue);
            cameraShake?.Shake(0.25f, 0.45f);

            // Hit-stop
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = 1f;

            // Fade in game over
            if (gameOverScreen != null)
            {
                gameOverScreen.SetActive(true);
                var cg = gameOverScreen.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0f;
                    float t = 0f;
                    while (t < 1f) { t += Time.unscaledDeltaTime * 3f; cg.alpha = t; yield return null; }
                }
            }

            // Pause the game so guards/music stop while the player reads the screen.
            // Buttons (Retry / MainMenu) still work — UI events are time-scale independent.
            Time.timeScale = 0f;
        }

        private IEnumerator HandleWon()
        {
            _gameEnded = true;
            AudioManager.Instance?.PlaySfx(AudioManager.Instance.objectiveReachedCue);
            if (winScreen != null)
            {
                winScreen.SetActive(true);
                var cg = winScreen.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0f;
                    float t = 0f;
                    while (t < 1f) { t += Time.unscaledDeltaTime * 3f; cg.alpha = t; yield return null; }
                }
            }

            Time.timeScale = 0f;
        }

        /// <summary>Adjusts the global alert counter (used by MusicDirector + camera shake).</summary>
        /// <param name="delta">+1 when a guard enters Alert, -1 when it exits.</param>
        public void OnGuardAlerted(int delta)
        {
            _alertCount = Mathf.Max(0, _alertCount + delta);
            if (delta > 0) cameraShake?.Shake(0.1f, 0.25f);
        }

        /// <summary>Spawns an "!" or "?" prefab as a child of <paramref name="parent"/> so it follows the guard. Auto-destroys after 1.5 s.</summary>
        /// <param name="parent">Transform to parent the spawn under (usually the guard's transform).</param>
        /// <param name="symbol"><c>"!"</c> for Alert, <c>"?"</c> for Suspicious.</param>
        public void SpawnExclamation(Transform parent, string symbol)
        {
            GameObject prefab = symbol == "!" ? exclamationPrefab : questionPrefab;
            if (prefab == null || parent == null) return;

            // Replace any existing icon on this parent so the visual matches the current state immediately.
            if (_activeExclamations.TryGetValue(parent, out var existing) && existing != null)
                Destroy(existing);

            var go = Instantiate(prefab, parent);
            go.transform.localPosition = Vector3.up * exclamationHeadOffset;
            _activeExclamations[parent] = go;
            Destroy(go, exclamationLifetime);
        }

        // --- UI Buttons ---

        /// <summary>Reloads the current scene. Restores <c>Time.timeScale</c> via <c>Start</c>.</summary>
        public void Retry() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        /// <summary>Exits the player in a build; no-op in the editor.</summary>
        public void Quit()  => Application.Quit();

        /// <summary>Loads the MainMenu scene (build index 0).</summary>
        public void GoToMainMenu() => SceneManager.LoadScene(0);
    }
}
