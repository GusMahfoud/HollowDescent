using UnityEngine;

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Rapid lightning-flash behaviour used by the StormLight random room event.
    /// Kept in its own file so prefab bake resolves MonoScript references correctly.
    /// </summary>
    public class StormLightEffect : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;
        private float _nextFlash;

        private void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = _light.intensity;
            ScheduleNextFlash();
        }

        private void Update()
        {
            if (_light == null) return;
            if (Time.time >= _nextFlash)
            {
                _light.intensity = _baseIntensity * Random.Range(0.1f, 2.5f);
                ScheduleNextFlash();
            }
        }

        private void ScheduleNextFlash() =>
            _nextFlash = Time.time + Random.Range(0.05f, 0.6f);
    }
}
