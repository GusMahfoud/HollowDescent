using UnityEngine;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Simple player health for enemy contact/projectile damage.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 5;
        [Tooltip("Turn off to enable damage again.")]
        [SerializeField] private bool invincible = true;
        private int _current;

        private void Awake()
        {
            _current = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (invincible) return;
            _current = Mathf.Max(0, _current - amount);
            if (_current <= 0)
                Die();
        }

        private void Die()
        {
            Destroy(gameObject);
        }
    }
}
