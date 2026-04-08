using UnityEngine;
using HollowDescent.Gameplay;
using HollowDescent.LevelGen;
using HollowDescent.AI;
using HollowDescent.UI_Debug;

namespace HollowDescent.Bootstrap
{
    /// <summary>
    /// Central coordinator; holds references and room state for UI/debug.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] private int totalLives = 3;
        [SerializeField] private int remainingLives;
        [SerializeField] private string currentRoomName = "Start (Safe)";
        [SerializeField] private int enemiesRemainingInRoom;

        public int TotalLives => Mathf.Max(1, totalLives);
        public int RemainingLives => Mathf.Clamp(remainingLives, 0, TotalLives);
        public string CurrentRoomName => currentRoomName;
        public int EnemiesRemainingInRoom => enemiesRemainingInRoom;
        public string EnemyComposition { get; private set; } = "";

        public bool DeathScreenOpen { get; private set; }

        public void SetCurrentRoom(string name)
        {
            currentRoomName = name ?? "Unknown";
        }

        public void SetEnemiesRemaining(int count)
        {
            enemiesRemainingInRoom = Mathf.Max(0, count);
        }

        public void SetEnemyComposition(string composition)
        {
            EnemyComposition = composition ?? "";
        }

        public void NotifyPlayerDied()
        {
            if (remainingLives > 1)
            {
                remainingLives--;
                RespawnCurrentLevelFromDeath();
                return;
            }

            remainingLives = 0;
            DeathScreenOpen = true;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Full reset for game over: refill lives, restore player, restart at Level 1.
        /// </summary>
        public void RestartFromLevelOne()
        {
            DeathScreenOpen = false;
            Time.timeScale = 1f;
            remainingLives = TotalLives;
            RunState.Instance?.ResetForNewRun();
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var health = playerGo != null ? playerGo.GetComponent<PlayerHealth>() : null;
            health?.ReviveForNewRun();
            if (LevelManager.Instance != null)
                LevelManager.Instance.LoadLevel(1);
            currentRoomName = "Start (Safe)";
            enemiesRemainingInRoom = 0;
        }

        /// <summary>
        /// Full reset: restore state, destroy level, show start menu.
        /// </summary>
        public void ReturnToMainMenu()
        {
            DeathScreenOpen = false;
            Time.timeScale = 1f;
            remainingLives = TotalLives;
            RunState.Instance?.ResetForNewRun();

            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var health = playerGo != null ? playerGo.GetComponent<PlayerHealth>() : null;
            health?.ReviveForNewRun();
            if (playerGo != null) playerGo.SetActive(false);

            // Destroy all loaded level geometry (both Level/* and loose LevelRoot objects).
            LevelManager.ClearLoadedLevelImmediate();
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                    DestroyImmediate(enemy.gameObject);
            }

            currentRoomName = "Start (Safe)";
            enemiesRemainingInRoom = 0;
            EnemyComposition = "";

            // Re-show the start menu — it will call BootstrapGame on click
            var bootstrap = FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null)
            {
                bootstrap.ShowMainMenu();
                return;
            }

            // Fallback if bootstrap isn't present.
            StartMenuOverlay.Show("Hollow Descent", "Start Game", RestartFromLevelOne);
        }

        private void RespawnCurrentLevelFromDeath()
        {
            DeathScreenOpen = false;
            Time.timeScale = 1f;
            var roomNameAtDeath = currentRoomName;

            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var health = playerGo != null ? playerGo.GetComponent<PlayerHealth>() : null;
            health?.ReviveForNewRun();

            var roomController = RoomController.FindByRoomName(roomNameAtDeath);
            if (roomController != null)
                roomController.HandlePlayerDeathInRoom();

            if (LevelManager.Instance != null)
                LevelManager.Instance.PlacePlayerAtLevelStart();

            currentRoomName = "Start (Safe)";
            enemiesRemainingInRoom = 0;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            remainingLives = Mathf.Clamp(remainingLives <= 0 ? totalLives : remainingLives, 1, TotalLives);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
