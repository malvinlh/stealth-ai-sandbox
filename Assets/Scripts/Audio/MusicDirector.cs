using UnityEngine;

namespace StealthGame
{
    public class MusicDirector : MonoBehaviour
    {
        [SerializeField] private AlertEventChannelSO alertChannel;
        [SerializeField] private AudioSource calmSource;
        [SerializeField] private AudioSource tenseSource;
        [SerializeField] private float crossfadeSpeed = 1.5f;

        private int _alertCount;
        private float _tenseTarget;

        private void Awake()
        {
            if (alertChannel == null)
            {
                var channels = Resources.FindObjectsOfTypeAll<AlertEventChannelSO>();
                if (channels != null && channels.Length > 0)
                    alertChannel = channels[0];
            }

            if (calmSource == null || tenseSource == null)
            {
                var sources = GetComponentsInChildren<AudioSource>(true);
                foreach (var source in sources)
                {
                    if (source == null) continue;
                    if (calmSource == null && source.name.Contains("Calm"))
                        calmSource = source;
                    else if (tenseSource == null && source.name.Contains("Tense"))
                        tenseSource = source;
                }

                if (calmSource == null && sources.Length > 0)
                    calmSource = sources[0];
                if (tenseSource == null && sources.Length > 1)
                    tenseSource = sources[1];
            }
        }

        private void OnEnable()
        {
            if (alertChannel != null)
                alertChannel.OnRaised += OnAlert;
        }

        private void OnDisable()
        {
            if (alertChannel != null)
                alertChannel.OnRaised -= OnAlert;
        }

        private void Start()
        {
            if (calmSource != null) calmSource.Play();
            if (tenseSource != null) { tenseSource.volume = 0f; tenseSource.Play(); }
        }

        private void OnAlert(AlertEventData data)
        {
            _alertCount += data.IsStarting ? 1 : -1;
            _alertCount = Mathf.Max(0, _alertCount);
            _tenseTarget = _alertCount > 0 ? 1f : 0f;
        }

        private void Update()
        {
            if (tenseSource == null || calmSource == null) return;
            tenseSource.volume = Mathf.MoveTowards(tenseSource.volume, _tenseTarget, crossfadeSpeed * Time.deltaTime);
            calmSource.volume  = Mathf.MoveTowards(calmSource.volume,  1f - _tenseTarget, crossfadeSpeed * Time.deltaTime);
        }
    }
}
