using UnityEngine;
using HollowDescent.LevelGen;

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

        public void SetCurrentRoom(string name)
        {
            currentRoomName = name ?? "Unknown";
        }

        public void SetEnemiesRemaining(int count)
        {
            enemiesRemainingInRoom = Mathf.Max(0, count);
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
