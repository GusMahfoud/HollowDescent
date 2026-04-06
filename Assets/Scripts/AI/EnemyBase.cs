using System;
using System.Collections;
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
        private Coroutine _stunCoroutine;
        private bool _movementLocked;
        private bool _pursuitEnabled = true;

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

        private void OnDisable()
        {
            if (_stunCoroutine != null)
            {
                StopCoroutine(_stunCoroutine);
                _stunCoroutine = null;
            }
            _movementLocked = false;
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

        /// <summary>Only this instance pauses pursuit after it lands a hit on the player.</summary>
        protected bool IsHitStunned => _movementLocked;
        protected bool CanPursue => _pursuitEnabled && !IsHitStunned;

        public void SetPursuitEnabled(bool enabled)
        {
            _pursuitEnabled = enabled;
            if (!enabled)
            {
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    var vel = rb.linearVelocity;
                    rb.linearVelocity = new Vector3(0f, vel.y, 0f);
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

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

            if (_stunCoroutine != null)
                StopCoroutine(_stunCoroutine);
            _stunCoroutine = StartCoroutine(HitStunRoutine());
        }

        private IEnumerator HitStunRoutine()
        {
            _movementLocked = true;
            yield return new WaitForSeconds(playerHitStunSeconds);
            _movementLocked = false;
            _stunCoroutine = null;
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
