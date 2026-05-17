using System.Collections;
using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Shakes a target Transform with linearly-decaying random offsets in unscaled time.
    /// Defaults to Camera.main so the component still works when attached to a non-camera
    /// GameObject (e.g. the GameManager wrapper).
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Tooltip("Optional. If null at Awake, falls back to Camera.main.transform.")]
        [SerializeField] private Transform target;

        private Transform _target;
        private Vector3   _origin;

        private void Awake()
        {
            _target = target != null ? target : Camera.main != null ? Camera.main.transform : null;
            if (_target == null)
            {
                Debug.LogWarning("[CameraShake] No target and no Camera.main — shake will no-op.", this);
                return;
            }
            _origin = _target.localPosition;
        }

        public void Shake(float magnitude, float duration)
        {
            if (_target == null) return;
            StopAllCoroutines();
            StartCoroutine(DoShake(magnitude, duration));
        }

        private IEnumerator DoShake(float magnitude, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float strength = magnitude * (1f - t);
                _target.localPosition = _origin + (Vector3)Random.insideUnitCircle * strength;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _target.localPosition = _origin;
        }
    }
}
