using UnityEngine;

namespace StealthGame
{
    [CreateAssetMenu(menuName = "StealthGame/Sound Cue")]
    public class SoundCueSO : ScriptableObject
    {
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitchMin = 0.95f;
        [Range(0.5f, 2f)] public float pitchMax = 1.05f;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }
}
