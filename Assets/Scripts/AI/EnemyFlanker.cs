using UnityEngine;

namespace HollowDescent.AI
{
    /// <summary>
    /// Approaches from lateral angle to reduce doorway camping.
    /// </summary>
    public class EnemyFlanker : EnemyBase
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lateralBias = 0.7f;

        private Transform _player;
        private Rigidbody _rb;

        public void SetPlayer(Transform p) => _player = p;

        public void ApplyMovementProfile(float speed, float lateral)
        {
            moveSpeed = Mathf.Max(0.5f, speed);
            lateralBias = Mathf.Clamp(lateral, 0.15f, 1.4f);
        }

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (IsHitStunned)
            {
                StopHorizontal();
                return;
            }
            if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (_player == null)
            {
                StopHorizontal();
                return;
            }
            var toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.01f)
            {
                StopHorizontal();
                return;
            }
            var direct = toPlayer.normalized;
            var right = Vector3.Cross(Vector3.up, direct);
            var flankDir = (direct + right * lateralBias).normalized;
            var v = flankDir * moveSpeed;
            if (_rb != null)
            {
                var vel = _rb.linearVelocity;
                _rb.linearVelocity = new Vector3(v.x, vel.y, v.z);
                _rb.angularVelocity = Vector3.zero;
            }
            else
                transform.position += v * Time.fixedDeltaTime;
            transform.forward = flankDir;
        }

        private void StopHorizontal()
        {
            if (_rb == null) return;
            var vel = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(0f, vel.y, 0f);
        }

        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.CompareTag("Player"))
                DealContactDamage(other.collider);
        }
    }
}
