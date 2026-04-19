using System;
using UnityEngine;
using HollowDescent.AI;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Kinematic rigidbody moved in FixedUpdate; sphere-casts along each segment so walls block shots (no room-wide overlap).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private float _lifetime;
        private float _spawnTime;
        private float _hitRadius = 0.35f;
        private int _damage = 1;
        private Collider _myCollider;
        private Rigidbody _rb;
        private const int CastMask = ~0;
        private const float MinVisibleTime = 0.06f;

        public void Init(Vector3 direction, float speed, float lifetime, float hitRadius, int damage = 1)
        {
            _direction = direction.normalized;
            _speed = speed;
            _lifetime = lifetime;
            _hitRadius = Mathf.Clamp(hitRadius, 0.06f, 3f);
            _damage = Mathf.Max(1, damage);
            _spawnTime = Time.time;
            _myCollider = GetComponent<Collider>();
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            var pos = _rb != null ? _rb.position : transform.position;
            var step = _direction * (_speed * Time.fixedDeltaTime);
            var nextPos = pos + step;

            // Only segment casts — no overlap spheres (those ignored walls and hit through rooms).
            if (ProcessSegmentHits(pos, nextPos))
                return;

            if (_rb != null)
                _rb.MovePosition(nextPos);
            else
                transform.position = nextPos;
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= _lifetime)
                Destroy(gameObject);
        }

        private bool IsMyCollider(Collider c)
        {
            return _myCollider != null && c == _myCollider;
        }

        private bool ProcessSegmentHits(Vector3 from, Vector3 to)
        {
            var seg = to - from;
            var dist = seg.magnitude;
            if (dist < 1e-5f)
                return false;
            var dir = seg / dist;
            var castRadius = Mathf.Max(0.1f, _hitRadius * 0.75f);

            var skin = Mathf.Min(0.1f, dist * 0.35f);
            var castFrom = from + dir * skin;
            var castDist = Mathf.Max(0f, dist - skin);

            // Grace period: skip all hits so the projectile always renders for a few frames
            if (Time.time - _spawnTime < MinVisibleTime)
                return false;

            var hits = Physics.SphereCastAll(castFrom, castRadius, dir, castDist, CastMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                if (IsMyCollider(h.collider)) continue;
                if (h.collider.CompareTag("Player")) continue;

                // Enemies first (including trigger colliders on some setups).
                var enemy = ResolveEnemy(h.collider);
                if (enemy != null)
                {
                    enemy.TakeDamage(_damage);
                    Destroy(gameObject);
                    return true;
                }

                // Non-enemy triggers (room volumes, pickups): do not block the shot.
                if (h.collider.isTrigger)
                    continue;

                // Floor planes: ignore so downward shots don't clip the ground.
                if (h.collider.name.StartsWith("Floor_", StringComparison.Ordinal))
                    continue;

                // Solid geometry (walls, doors): stop here.
                Destroy(gameObject);
                return true;
            }

            return false;
        }

        private static EnemyBase ResolveEnemy(Collider c)
        {
            return c.GetComponent<EnemyBase>() ?? c.GetComponentInParent<EnemyBase>() ?? c.GetComponentInChildren<EnemyBase>();
        }

    }
}
