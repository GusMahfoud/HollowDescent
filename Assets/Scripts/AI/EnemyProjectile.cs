using UnityEngine;
using HollowDescent.Gameplay;

namespace HollowDescent.AI
{
    /// <summary>
    /// Enemy projectile: moves forward, damages player on hit.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private float _lifetime;
        private int _damage = 1;
        private float _spawnTime;

        public void Init(Vector3 direction, float speed, float lifetime, int damage)
        {
            _direction = direction.normalized;
            _speed = speed;
            _lifetime = lifetime;
            _damage = damage;
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
            if (other.CompareTag("Player"))
            {
                var health = other.GetComponent<PlayerHealth>();
                if (health != null) health.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
