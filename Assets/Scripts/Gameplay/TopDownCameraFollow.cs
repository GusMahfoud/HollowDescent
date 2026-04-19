using UnityEngine;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Top-down camera: fixed height and angle, smooth follow.
    /// </summary>
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform target;
        [SerializeField] private float height = 18f;
        [SerializeField] private float pitchAngle = 70f;
        [SerializeField] private float smoothTime = 0.15f;

        private Vector3 _velocity;

        public void SetTarget(Transform t) => target = t;

        private void FixedUpdate()
        {
            if (target == null) return;
            var desiredPos = target.position + Quaternion.Euler(pitchAngle, 0f, 0f) * (Vector3.back * (height / Mathf.Sin(pitchAngle * Mathf.Deg2Rad)));
            desiredPos.y = target.position.y + height;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
            transform.LookAt(target.position + Vector3.up * 2f);
        }
    }
}
