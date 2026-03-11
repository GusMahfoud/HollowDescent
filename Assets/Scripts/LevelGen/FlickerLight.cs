using UnityEngine;

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Flickers a Point Light to create horror/danger atmosphere in combat rooms.
    /// </summary>
    public class FlickerLight : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;

        [SerializeField] private float flickerSpeed = 15f;
        [SerializeField] private float flickerAmount = 0.35f;

        private void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null)
                _baseIntensity = _light.intensity;
        }

        private void Update()
        {
            if (_light == null) return;
            _light.intensity = _baseIntensity
                + Mathf.Sin(Time.time * flickerSpeed) * (flickerAmount * Random.Range(0.5f, 1f));
        }
    }
}