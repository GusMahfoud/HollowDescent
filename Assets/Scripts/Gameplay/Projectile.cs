using UnityEngine;
using HollowDescent.AI;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Moves forward; damages EnemyBase on overlap or trigger, then destroys.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private float _lifetime;
        private float _spawnTime;
        private Vector3 _prevPosition;
        private const float HitRadius = 1.25f;
        private static readonly Collider[] OverlapBuffer = new Collider[32];
        private static readonly RaycastHit[] SweepBuffer = new RaycastHit[16];

        public void Init(Vector3 direction, float speed, float lifetime)
        {
            _direction = direction.normalized;
            _speed = speed;
            _lifetime = lifetime;
            _spawnTime = Time.time;
            _prevPosition = transform.position;
        }

        private void Update()
        {
            _prevPosition = transform.position;
            transform.position += _direction * (_speed * Time.deltaTime);
            if (Time.time - _spawnTime >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }
            if (CheckOverlap(transform.position))
                return;
            var dist = Vector3.Distance(_prevPosition, transform.position);
            if (dist > 0.001f)
                CheckSweep(_prevPosition, _direction, dist);
        }

        private void CheckSweep(Vector3 origin, Vector3 dir, float distance)
        {
            var count = Physics.SphereCastNonAlloc(origin, HitRadius * 0.5f, dir, SweepBuffer, distance + HitRadius, -1, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var c = SweepBuffer[i].collider;
                if (c == null) continue;
                if (c.transform == transform) continue;
                if (c.CompareTag("Player")) continue;
                var enemy = c.GetComponent<EnemyBase>() ?? c.GetComponentInParent<EnemyBase>() ?? c.GetComponentInChildren<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(1);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private bool CheckOverlap(Vector3 position)
        {
            var count = Physics.OverlapSphereNonAlloc(position, HitRadius, OverlapBuffer, -1, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var c = OverlapBuffer[i];
                if (c == null) continue;
                if (c.transform == transform) continue;
                if (c.CompareTag("Player")) continue;
                var enemy = c.GetComponent<EnemyBase>() ?? c.GetComponentInParent<EnemyBase>() ?? c.GetComponentInChildren<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(1);
                    Destroy(gameObject);
                    return true;
                }
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) return;
            var enemy = other.GetComponent<EnemyBase>() ?? other.GetComponentInParent<EnemyBase>() ?? other.GetComponentInChildren<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(1);
                Destroy(gameObject);
            }
        }
    }
}
