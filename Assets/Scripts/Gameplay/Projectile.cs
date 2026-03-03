using UnityEngine;
using HollowDescent.AI;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Moves forward; on trigger with Enemy, damages and destroys.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private float _lifetime;
        private float _spawnTime;

        public void Init(Vector3 direction, float speed, float lifetime)
        {
            _direction = direction.normalized;
            _speed = speed;
            _lifetime = lifetime;
            _spawnTime = Time.time;
        }

        private void Update()
        {
            transform.position += _direction * (_speed * Time.deltaTime);
            if (Time.time - _spawnTime >= _lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(1);
                Destroy(gameObject);
            }
        }
    }
}
