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

        public void SetPlayer(Transform p) => _player = p;

        private void Update()
        {
            if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (_player == null) return;
            var toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            var dist = toPlayer.magnitude;
            if (dist > 0.01f)
            {
                var dir = toPlayer.normalized;
                transform.forward = dir;
                if (dist < preferredDistance * 0.9f)
                    transform.position -= dir * (moveSpeed * Time.deltaTime);
                else if (dist > preferredDistance * 1.1f)
                    transform.position += dir * (moveSpeed * Time.deltaTime);
            }
            if (Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + fireInterval;
                Fire();
            }
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
            rb.useGravity = false;
            var p = proj.AddComponent<EnemyProjectile>();
            p.Init(dir, projectileSpeed, 2f, 1);
        }

        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.CompareTag("Player"))
                DealContactDamage(other.collider);
        }
    }
}
