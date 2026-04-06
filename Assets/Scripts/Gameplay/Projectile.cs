using System;
using UnityEngine;
using HollowDescent.AI;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Kinematic rigidbody moved in FixedUpdate so physics queries see the bullet; damages EnemyBase on overlap or sweep.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private float _lifetime;
        private float _spawnTime;
        private float _hitRadius = 0.35f;
        private Collider _myCollider;
        private Rigidbody _rb;
        private readonly Collider[] _overlapScratch = new Collider[32];
        private const int CastMask = ~0;

        public void Init(Vector3 direction, float speed, float lifetime, float hitRadius)
        {
            _direction = direction.normalized;
            _speed = speed;
            _lifetime = lifetime;
            _hitRadius = Mathf.Clamp(hitRadius, 0.06f, 3f);
            _spawnTime = Time.time;
            _myCollider = GetComponent<Collider>();
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            var pos = _rb != null ? _rb.position : transform.position;
            var step = _direction * (_speed * Time.fixedDeltaTime);
            var nextPos = pos + step;

            if (TryDamageEnemyAtPosition(pos) || TryDamageEnemyAtPosition(nextPos))
                return;

            if (ProcessSegmentHits(pos, nextPos))
                return;

            if (_rb != null)
                _rb.MovePosition(nextPos);
            else
                transform.position = nextPos;

            var after = _rb != null ? _rb.position : transform.position;
            if (TryDamageEnemyAtPosition(after))
                return;
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= _lifetime)
                Destroy(gameObject);
        }

        private bool TryDamageEnemyAtPosition(Vector3 position)
        {
            var r = Mathf.Max(0.28f, _hitRadius * 2.25f);
            var count = Physics.OverlapSphereNonAlloc(position, r, _overlapScratch, CastMask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var c = _overlapScratch[i];
                if (c == null || c.transform == transform) continue;
                if (IsMyCollider(c)) continue;
                if (c.CompareTag("Player")) continue;
                var enemy = ResolveEnemy(c);
                if (enemy != null)
                {
                    enemy.TakeDamage(1);
                    Destroy(gameObject);
                    return true;
                }
            }
            return false;
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

            var hits = Physics.SphereCastAll(castFrom, castRadius, dir, castDist, CastMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                if (IsMyCollider(h.collider)) continue;
                if (h.collider.CompareTag("Player")) continue;

                var enemy = ResolveEnemy(h.collider);
                if (enemy != null)
                {
                    enemy.TakeDamage(1);
                    Destroy(gameObject);
                    return true;
                }

                if (h.collider.isTrigger)
                    continue;

                Destroy(gameObject);
                return true;
            }

            return false;
        }

        private static EnemyBase ResolveEnemy(Collider c)
        {
            return c.GetComponent<EnemyBase>() ?? c.GetComponentInParent<EnemyBase>() ?? c.GetComponentInChildren<EnemyBase>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.CompareTag("Player")) return;
            if (IsMyCollider(other)) return;
            var enemy = ResolveEnemy(other);
            if (enemy != null)
            {
                enemy.TakeDamage(1);
                Destroy(gameObject);
            }
        }
    }
}
