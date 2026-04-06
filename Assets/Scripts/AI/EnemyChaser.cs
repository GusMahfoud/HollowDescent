using UnityEngine;

namespace HollowDescent.AI
{
    /// <summary>
    /// Moves toward player; contact damage.
    /// </summary>
    public class EnemyChaser : EnemyBase
    {
        [SerializeField] private float moveSpeed = 4f;

        private Transform _player;
        private Rigidbody _rb;

        public void SetPlayer(Transform p) => _player = p;

        public void ApplyMovementProfile(float speed)
        {
            moveSpeed = Mathf.Max(0.5f, speed);
        }

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!CanPursue)
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
            var dir = (_player.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                var v = dir * moveSpeed;
                if (_rb != null)
                {
                    var vel = _rb.linearVelocity;
                    _rb.linearVelocity = new Vector3(v.x, vel.y, v.z);
                    _rb.angularVelocity = Vector3.zero;
                }
                else
                    transform.position += v * Time.fixedDeltaTime;
                transform.forward = dir;
            }
            else
                StopHorizontal();
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
