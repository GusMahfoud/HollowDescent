using UnityEngine;
using HollowDescent.LevelGen;

namespace HollowDescent.Bootstrap
{
    /// <summary>
    /// Handles level transitions: LoadLevel(n) clears current floor and generates level n, repositions player.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] private int currentLevel = 1;
        private FloorGenerator _floorGenerator;
        private GameObject _levelRoot;
        private Transform _player;

        public int CurrentLevel => currentLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            var levelGo = GameObject.Find("Level");
            if (levelGo != null)
            {
                _floorGenerator = levelGo.GetComponent<FloorGenerator>();
                _levelRoot = levelGo.transform.Find("LevelRoot")?.gameObject;
            }
        }

        public void RegisterLevelRoot(GameObject root)
        {
            _levelRoot = root;
        }

        public void RegisterFloorGenerator(FloorGenerator gen)
        {
            _floorGenerator = gen;
        }

        public void LoadLevel(int levelIndex)
        {
            currentLevel = levelIndex;
            if (_levelRoot != null)
                Destroy(_levelRoot);
            _levelRoot = null;

            if (_floorGenerator == null)
            {
                var levelGo = GameObject.Find("Level");
                _floorGenerator = levelGo != null ? levelGo.GetComponent<FloorGenerator>() : null;
            }

            if (_floorGenerator != null)
            {
                _floorGenerator.GenerateLevel(levelIndex);
                var startPos = _floorGenerator.GetStartPosition();
                if (_player != null && startPos.HasValue)
                    _player.position = startPos.Value + Vector3.up;
            }

            if (_player == null)
                _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
