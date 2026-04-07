using System.Collections.Generic;
using UnityEngine;

namespace HollowDescent.Gameplay
{
    public enum BuffType { MoveSpeed, FireRate, MaxHealth, Damage }

    /// <summary>
    /// Holds in-run stat buffs applied by the shop. Other scripts read effective values from here.
    /// </summary>
    public class PlayerStatModifiers : MonoBehaviour
    {
        private float _bonusMoveSpeed;
        private float _fireRateMultiplier = 1f;
        private int _bonusMaxHealth;
        private float _damageMultiplier = 1f;

        private readonly List<string> _buffNames = new List<string>();

        public void ApplyBuff(BuffType type, float value, string displayName)
        {
            switch (type)
            {
                case BuffType.MoveSpeed:
                    _bonusMoveSpeed += value;
                    break;
                case BuffType.FireRate:
                    _fireRateMultiplier *= value;
                    break;
                case BuffType.MaxHealth:
                    _bonusMaxHealth += Mathf.RoundToInt(value);
                    var health = GetComponent<PlayerHealth>();
                    health?.RecalculateMaxHealth();
                    break;
                case BuffType.Damage:
                    _damageMultiplier *= value;
                    break;
            }
            if (!string.IsNullOrEmpty(displayName))
                _buffNames.Add(displayName);
        }

        public void ResetAllBuffs()
        {
            _bonusMoveSpeed = 0f;
            _fireRateMultiplier = 1f;
            _bonusMaxHealth = 0;
            _damageMultiplier = 1f;
            _buffNames.Clear();
        }

        public float GetEffectiveMoveSpeed(float baseSpeed) => baseSpeed + _bonusMoveSpeed;

        public float GetEffectiveFireRate(float baseRate) => baseRate * _fireRateMultiplier;

        public int GetEffectiveMaxHealth(int baseMax) => baseMax + _bonusMaxHealth;

        public float GetDamageMultiplier() => _damageMultiplier;

        public List<string> GetActiveBuffSummary() => _buffNames;
    }
}
