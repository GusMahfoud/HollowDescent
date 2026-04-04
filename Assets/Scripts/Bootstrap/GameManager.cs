using UnityEngine;
using HollowDescent.Gameplay;

namespace HollowDescent.Bootstrap
{
    /// <summary>
    /// Central coordinator; holds references and room state for UI/debug.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] private string currentRoomName = "Start (Safe)";
        [SerializeField] private int enemiesRemainingInRoom;

        public string CurrentRoomName => currentRoomName;
        public int EnemiesRemainingInRoom => enemiesRemainingInRoom;

        public bool DeathScreenOpen { get; private set; }

        public void SetCurrentRoom(string name)
        {
            currentRoomName = name ?? "Unknown";
        }

        public void SetEnemiesRemaining(int count)
        {
            enemiesRemainingInRoom = Mathf.Max(0, count);
        }

        public void NotifyPlayerDied()
        {
            DeathScreenOpen = true;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Full health reset, reload Level 1 baked floor, warp to PlayerStart.
        /// </summary>
        public void RestartFromLevelOne()
        {
            DeathScreenOpen = false;
            Time.timeScale = 1f;
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var health = playerGo != null ? playerGo.GetComponent<PlayerHealth>() : null;
            health?.ReviveForNewRun();
            if (LevelManager.Instance != null)
                LevelManager.Instance.LoadLevel(1);
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
