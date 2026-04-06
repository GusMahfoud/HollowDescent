using UnityEngine;
using HollowDescent.Gameplay;

namespace HollowDescent.AI
{
    /// <summary>
    /// Keeps distance from player, shoots projectiles periodically.
    /// </summary>
    public class EnemyShooter : EnemyBase
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float preferredDistance = 6f;
        [SerializeField] private float fireInterval = 1.5f;
        [SerializeField] private float projectileSpeed = 10f;

        private Transform _player;
        private float _nextFireTime;
        private Rigidbody _rb;

        public void SetPlayer(Transform p) => _player = p;

        public void ApplyCombatProfile(float moveSpd, float preferredDist, float fireInt, float projSpd)
        {
            moveSpeed = Mathf.Max(0.5f, moveSpd);
            preferredDistance = Mathf.Clamp(preferredDist, 3f, 14f);
            fireInterval = Mathf.Clamp(fireInt, 0.4f, 3.5f);
            projectileSpeed = Mathf.Max(3f, projSpd);
        }

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!CanPursue) return;
            if (Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + fireInterval;
                Fire();
            }
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
            var toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            var dist = toPlayer.magnitude;
            if (dist > 0.01f)
            {
                var dir = toPlayer.normalized;
                transform.forward = dir;
                Vector3 v;
                if (dist < preferredDistance * 0.9f)
                    v = -dir * moveSpeed;
                else if (dist > preferredDistance * 1.1f)
                    v = dir * moveSpeed;
                else
                    v = Vector3.zero;

                if (_rb != null)
                {
                    var vel = _rb.linearVelocity;
                    _rb.linearVelocity = new Vector3(v.x, vel.y, v.z);
                    _rb.angularVelocity = Vector3.zero;
                }
                else if (v.sqrMagnitude > 0.01f)
                    transform.position += v * Time.fixedDeltaTime;
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

        private void Fire()
        {
            if (_player == null) return;
            var dir = (_player.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            dir.Normalize();
            var proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = "EnemyProjectile";
            proj.transform.position = transform.position + Vector3.up * 0.8f + dir;
            proj.transform.localScale = Vector3.one * 0.4f;
            proj.GetComponent<Collider>().isTrigger = true;
            var rb = proj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var p = proj.AddComponent<EnemyProjectile>();
            p.Init(dir, projectileSpeed, 2f, 1, this);
        }

        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.CompareTag("Player"))
                DealContactDamage(other.collider);
        }
    }
}
