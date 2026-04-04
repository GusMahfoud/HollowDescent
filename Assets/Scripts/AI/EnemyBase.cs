using System;
using UnityEngine;
using HollowDescent.Gameplay;

namespace HollowDescent.AI
{
    /// <summary>
    /// Base enemy: health, damage, death event.
    /// </summary>
    public class EnemyBase : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] protected int maxHealth = 2;
        [SerializeField] protected int contactDamage = 1;

        [Header("On successful player hit")]
        [SerializeField] private float playerHitKnockbackDistance = 0.55f;
        [SerializeField] private float playerHitStunSeconds = 0.5f;

        protected int CurrentHealth;
        public event Action<EnemyBase> OnDeath;
        private float _hitStunUntilUnscaledTime;

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }

        public virtual void TakeDamage(int amount)
        {
            CurrentHealth -= amount;
            if (CurrentHealth <= 0)
                Die();
        }

        /// <summary>Re-roll HP and contact damage after spawn (runtime primitives / prefabs).</summary>
        public void ApplyRuntimeCombatStats(int hp, int damage)
        {
            maxHealth = Mathf.Max(1, hp);
            contactDamage = Mathf.Max(1, damage);
            CurrentHealth = maxHealth;
        }

        protected virtual void Die()
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        protected bool IsHitStunned => Time.unscaledTime < _hitStunUntilUnscaledTime;

        /// <summary>
        /// Brief knockback away from the player, then hold still for <see cref="playerHitStunSeconds"/>.
        /// </summary>
        public void RegisterSuccessfulPlayerHit(Transform playerTransform)
        {
            if (playerTransform == null) return;
            var away = transform.position - playerTransform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
            away.Normalize();

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
                rb.MovePosition(rb.position + away * playerHitKnockbackDistance);
            else
                transform.position += away * playerHitKnockbackDistance;

            _hitStunUntilUnscaledTime = Time.unscaledTime + playerHitStunSeconds;
        }

        protected void DealContactDamage(Collider other)
        {
            if (other == null || !other.CompareTag("Player")) return;
            if (other.GetComponent<PlayerControllerTopDown>() == null) return;
            var health = other.GetComponent<PlayerHealth>();
            if (health == null || health.IsDead) return;
            if (!health.TakeDamage(contactDamage)) return;
            RegisterSuccessfulPlayerHit(other.transform);
        }
    }
}
