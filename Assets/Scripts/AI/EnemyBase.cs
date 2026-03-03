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

        protected int CurrentHealth;
        public event Action<EnemyBase> OnDeath;

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public virtual void TakeDamage(int amount)
        {
            CurrentHealth -= amount;
            if (CurrentHealth <= 0)
                Die();
        }

        protected virtual void Die()
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        protected void DealContactDamage(Collider other)
        {
            var player = other.GetComponent<PlayerControllerTopDown>();
            if (player != null)
            {
                var health = other.GetComponent<PlayerHealth>();
                if (health != null) health.TakeDamage(contactDamage);
            }
        }
    }
}
