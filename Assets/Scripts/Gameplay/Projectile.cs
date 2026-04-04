using System;
using UnityEngine;
using HollowDescent.AI;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Moves forward; damages EnemyBase on overlap or sweep, then destroys. Walls block after enemies along the ray.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private float _lifetime;
        private float _spawnTime;
        private Vector3 _prevPosition;
        private float _hitRadius = 0.35f;
        private Collider _myCollider;
        private static readonly Collider[] OverlapBuffer = new Collider[32];

        public void Init(Vector3 direction, float speed, float lifetime, float hitRadius)
        {
            _direction = direction.normalized;
            _speed = speed;
            _lifetime = lifetime;
            _hitRadius = Mathf.Clamp(hitRadius, 0.06f, 3f);
            _spawnTime = Time.time;
            _prevPosition = transform.position;
            _myCollider = GetComponent<Collider>();
        }

        private void Update()
        {
            _prevPosition = transform.position;
            var step = _direction * (_speed * Time.deltaTime);
            var nextPos = _prevPosition + step;

            if (TryDamageEnemyAtPosition(_prevPosition) || TryDamageEnemyAtPosition(nextPos))
                return;

            if (ProcessSegmentHits(_prevPosition, nextPos))
                return;

            transform.position = nextPos;

            if (Time.time - _spawnTime >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (TryDamageEnemyAtPosition(transform.position))
                return;
        }

        /// <summary>Overlap sometimes catches kinematic bullets tunneling a thin collider.</summary>
        private bool TryDamageEnemyAtPosition(Vector3 position)
        {
            var r = Mathf.Max(0.08f, _hitRadius);
            var count = Physics.OverlapSphereNonAlloc(position, r, OverlapBuffer, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var c = OverlapBuffer[i];
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

        /// <summary>
        /// SphereCast along the step; closest hit wins — enemy applies damage, anything else blocks the shot.
        /// </summary>
        private bool ProcessSegmentHits(Vector3 from, Vector3 to)
        {
            var seg = to - from;
            var dist = seg.magnitude;
            if (dist < 1e-5f)
                return false;
            var dir = seg / dist;
            var castRadius = Mathf.Max(0.06f, _hitRadius * 0.55f);

            // Pull start forward slightly so casts don't start "inside" overlapping hulls/floor in odd ways
            var skin = Mathf.Min(0.06f, dist * 0.25f);
            var castFrom = from + dir * skin;
            var castDist = Mathf.Max(0f, dist - skin);

            var hits = Physics.SphereCastAll(castFrom, castRadius, dir, castDist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
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
