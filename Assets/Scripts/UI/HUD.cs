using UnityEngine;
using UnityEngine.UI;

namespace StealthGame
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private Image noiseMeter;
        [SerializeField] private GameStateEventChannelSO gameStateChannel;

        private PlayerController _player;

        private void Awake()
        {
            if (gameStateChannel == null)
            {
                var channels = Resources.FindObjectsOfTypeAll<GameStateEventChannelSO>();
                if (channels != null && channels.Length > 0)
                    gameStateChannel = channels[0];
            }

            if (noiseMeter == null)
            {
                Debug.LogWarning("[HUD] noiseMeter Image is not assigned — noise bar will not render.", this);
            }
            else if (noiseMeter.sprite == null)
            {
                // Image.fillAmount needs a source sprite to clip. Assign a white runtime sprite
                // so the meter renders even if the UI was built without a sprite slot.
                var tex = Texture2D.whiteTexture;
                noiseMeter.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                noiseMeter.type = Image.Type.Filled;
                noiseMeter.fillMethod = Image.FillMethod.Horizontal;
                noiseMeter.fillOrigin = (int)Image.OriginHorizontal.Left;
                noiseMeter.fillAmount = 0f;
            }
        }

        private void Start()
        {
            var playerTr = PlayerLocator.Find();
            if (playerTr != null) _player = playerTr.GetComponentInChildren<PlayerController>(true);
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

        private void Update()
        {
            if (_player == null)
            {
                var playerTr = PlayerLocator.Find();
                if (playerTr != null) _player = playerTr.GetComponentInChildren<PlayerController>(true);
            }

            if (noiseMeter == null || _player == null) return;
            bool moving = _player.MoveInput.sqrMagnitude > 0.01f;
            noiseMeter.fillAmount = (moving && _player.CurrentStance != null)
                ? _player.CurrentStance.noiseIntensity
                : 0f;
        }

        private void OnGameState(GameState state)
        {
            if (state == GameState.Caught || state == GameState.Won)
                gameObject.SetActive(false);
        }
    }
}
