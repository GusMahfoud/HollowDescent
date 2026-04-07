using UnityEngine;

namespace HollowDescent.Bootstrap
{
    /// <summary>
    /// Run-scoped state: currency, kill/clear counters, performance stats.
    /// Resets on each new run via <see cref="ResetForNewRun"/>.
    /// </summary>
    public class RunState : MonoBehaviour
    {
        public static RunState Instance { get; private set; }

        public int Currency { get; private set; }
        public int EnemiesKilled { get; private set; }
        public int RoomsCleared { get; private set; }
        public int DamageTaken { get; private set; }
        public bool RunComplete { get; private set; }

        public void AddCurrency(int amount)
        {
            Currency = Mathf.Max(0, Currency + amount);
        }

        public bool SpendCurrency(int amount)
        {
            if (amount <= 0 || Currency < amount) return false;
            Currency -= amount;
            return true;
        }

        public void RecordKill() => EnemiesKilled++;

        public void RecordRoomCleared() => RoomsCleared++;

        public void RecordDamageTaken(int amount) => DamageTaken += Mathf.Max(0, amount);

        public void MarkRunComplete() => RunComplete = true;

        public void ResetForNewRun()
        {
            Currency = 0;
            EnemiesKilled = 0;
            RoomsCleared = 0;
            DamageTaken = 0;
            RunComplete = false;

            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var mods = playerGo != null ? playerGo.GetComponent<Gameplay.PlayerStatModifiers>() : null;
            mods?.ResetAllBuffs();

            Gameplay.ShopSystem.Instance?.ResetShop();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
