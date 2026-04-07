using System;
using UnityEngine;
using HollowDescent.Bootstrap;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Player health, hit invulnerability, death hand-off to GameManager (no destroy — allows restart).
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 5;
        [Tooltip("Turn on for testing without taking damage.")]
        [SerializeField] private bool invincible = false;
        [Tooltip("I-frames after each successful hit (uses unscaled time; works with death pause).")]
        [SerializeField] private float hitInvulnerabilitySeconds = 1f;
        private int _current;
        private float _invulnerableUntilUnscaledTime;
        private bool _isDead;

        /// <summary>Remaining life points (each hit removes one; at zero the run ends).</summary>
        public int CurrentHealth => _current;

        public int MaxHealth => GetEffectiveMax();

        public bool IsDead => _isDead;

        /// <summary>Fired only when damage is actually applied (not blocked by i-frames).</summary>
        public event Action OnDamaged;

        private void Awake()
        {
            _current = GetEffectiveMax();
        }

        private int GetEffectiveMax()
        {
            var mods = GetComponent<PlayerStatModifiers>();
            return mods != null ? mods.GetEffectiveMaxHealth(maxHealth) : maxHealth;
        }

        /// <summary>
        /// Recalculate max HP after a buff is applied mid-run. Heals the bonus amount.
        /// </summary>
        public void RecalculateMaxHealth()
        {
            var oldMax = maxHealth;
            var newMax = GetEffectiveMax();
            var bonus = newMax - oldMax;
            if (bonus > 0)
                _current = Mathf.Min(_current + bonus, newMax);
        }

        /// <returns>True if at least one point of damage was applied.</returns>
        public bool TakeDamage(int amount)
        {
            if (invincible || _isDead) return false;
            if (Time.unscaledTime < _invulnerableUntilUnscaledTime) return false;
            _current = Mathf.Max(0, _current - amount);
            _invulnerableUntilUnscaledTime = Time.unscaledTime + hitInvulnerabilitySeconds;
            RunState.Instance?.RecordDamageTaken(amount);
            OnDamaged?.Invoke();
            if (_current <= 0)
                Die();
            return true;
        }

        /// <summary>
        /// Resets health and input after choosing Restart from the death screen.
        /// </summary>
        public void ReviveForNewRun()
        {
            _isDead = false;
            _current = GetEffectiveMax();
            _invulnerableUntilUnscaledTime = Time.unscaledTime + hitInvulnerabilitySeconds;
            var ctrl = GetComponent<PlayerControllerTopDown>();
            if (ctrl != null) ctrl.enabled = true;
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            var ctrl = GetComponent<PlayerControllerTopDown>();
            if (ctrl != null) ctrl.enabled = false;
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            HollowDescent.Bootstrap.GameManager.Instance?.NotifyPlayerDied();
        }
    }
}
