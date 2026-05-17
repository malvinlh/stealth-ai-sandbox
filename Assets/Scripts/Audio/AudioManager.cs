using UnityEngine;

namespace StealthGame
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("SFX Pool")]
        [SerializeField] private int poolSize = 8;

        [Header("SFX Cues")]
        public SoundCueSO footstepWalkCue;
        public SoundCueSO footstepCrouchCue;
        public SoundCueSO footstepSprintCue;
        public SoundCueSO guardSpotCue;
        public SoundCueSO guardSuspiciousCue;
        public SoundCueSO guardLoseSightCue;
        public SoundCueSO objectiveReachedCue;
        public SoundCueSO caughtStingCue;

        private AudioSource[] _pool;
        private int _poolIdx;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildPool();
        }

        private void BuildPool()
        {
            _pool = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"SfxSource_{i}");
                go.transform.SetParent(transform);
                _pool[i] = go.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
            }
        }

        public void PlaySfx(SoundCueSO cue)
        {
            if (cue == null) return;
            var clip = cue.GetRandomClip();
            if (clip == null) return;

            var src = GetNextSource();
            src.clip   = clip;
            src.volume = cue.volume;
            src.pitch  = Random.Range(cue.pitchMin, cue.pitchMax);
            src.Play();
        }

        private AudioSource GetNextSource()
        {
            // Round-robin pool — skip still-playing sources if possible
            for (int i = 0; i < _pool.Length; i++)
            {
                int idx = (_poolIdx + i) % _pool.Length;
                if (!_pool[idx].isPlaying)
                {
                    _poolIdx = (idx + 1) % _pool.Length;
                    return _pool[idx];
                }
            }
            // All busy — steal oldest
            var stolen = _pool[_poolIdx];
            _poolIdx = (_poolIdx + 1) % _pool.Length;
            return stolen;
        }
    }
}
