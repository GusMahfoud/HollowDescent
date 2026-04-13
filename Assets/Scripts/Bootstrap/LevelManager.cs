using System.Collections;
using UnityEngine;
using HollowDescent.LevelGen;

namespace HollowDescent.Bootstrap
{
    /// <summary>
    /// Level transitions: baked level prefabs (Resources) or procedural FloorGenerator fallback.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        private const string BakedLevelResourceFormat = "Prefabs/Levels/Level_{0:00}";

        [SerializeField] private int currentLevel = 1;
        [Tooltip("Optional override per index [0]=Level 1. If empty, loads Resources path Prefabs/Levels/Level_01 etc.")]
        [SerializeField] private GameObject[] levelPrefabOverrides;

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
            RefreshPlayerCache();
            RefreshLevelRootCache();
        }

        public void RegisterLevelRoot(GameObject root)
        {
            _levelRoot = root;
        }

        public void RegisterFloorGenerator(FloorGenerator gen)
        {
            _floorGenerator = gen;
        }

        public static void ClearLoadedLevelImmediate()
        {
            var levelHolder = GameObject.Find("Level");
            if (levelHolder != null)
                WipeLevelChildrenImmediate(levelHolder.transform);
            WipeLooseLevelRootsImmediate(levelHolder != null ? levelHolder.transform : null);
        }

        /// <summary>
        /// Moves the player to PlayerStart under the given level root (or current cache / Find).
        /// </summary>
        public void PlacePlayerAtLevelStart(GameObject levelRootOrNull = null)
        {
            RefreshPlayerCache();
            var rootGo = levelRootOrNull != null ? levelRootOrNull : _levelRoot;
            if (rootGo == null)
                rootGo = GameObject.Find("LevelRoot");
            if (rootGo == null || _player == null) return;

            var startT = FindChildRecursive(rootGo.transform, "PlayerStart");
            if (startT == null) return;

            _player.SetPositionAndRotation(startT.position, startT.rotation);
        }

        /// <summary>
        /// Schedules <see cref="LoadLevel"/> after the current frame so it is safe to call from
        /// physics callbacks (OnTriggerEnter, etc.). LoadLevel uses DestroyImmediate internally.
        /// </summary>
        public void LoadLevelDeferred(int levelIndex)
        {
            StartCoroutine(LoadLevelDeferredRoutine(levelIndex));
        }

        private IEnumerator LoadLevelDeferredRoutine(int levelIndex)
        {
            yield return null;
            LoadLevel(levelIndex);
        }

        public void LoadLevel(int levelIndex)
        {
            currentLevel = levelIndex;
            RefreshPlayerCache();

            var levelHolder = GameObject.Find("Level");
            if (levelHolder == null)
                levelHolder = new GameObject("Level");

            // Destroy() is end-of-frame: instantiating the next level in the same frame leaves two
            // roots under "Level" and breaks transitions. Clear children immediately.
            WipeLevelChildrenImmediate(levelHolder.transform);
            WipeLooseLevelRootsImmediate(levelHolder.transform);
            _levelRoot = null;

            if (TrySpawnBakedLevel(levelIndex, levelHolder))
                return;

            EnsureFloorGenerator(levelHolder);

            if (_floorGenerator != null)
            {
                _floorGenerator.GenerateLevel(levelIndex);
                var startPos = _floorGenerator.GetStartPosition();
                if (_player != null && startPos.HasValue)
                    _player.position = startPos.Value + Vector3.up;
                return;
            }

            Debug.LogError(
                $"[LevelManager] Could not load level {levelIndex}: no Resources prefab at " +
                $"Prefabs/Levels/Level_{levelIndex:00} (and FloorGenerator could not be added).");

            if (_player == null)
                _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private static void WipeLevelChildrenImmediate(Transform levelTransform)
        {
            if (levelTransform == null) return;
            for (var i = levelTransform.childCount - 1; i >= 0; i--)
            {
                var child = levelTransform.GetChild(i).gameObject;
                DestroyImmediate(child);
            }
        }

        private static void WipeLooseLevelRootsImmediate(Transform levelHolder)
        {
            var looseRoots = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in looseRoots)
            {
                if (t == null) continue;
                if (!string.Equals(t.name, "LevelRoot", System.StringComparison.Ordinal)) continue;
                if (levelHolder != null && t.IsChildOf(levelHolder)) continue;
                DestroyImmediate(t.gameObject);
            }
        }

        private void EnsureFloorGenerator(GameObject levelHolder)
        {
            if (_floorGenerator == null)
                _floorGenerator = levelHolder.GetComponent<FloorGenerator>();
            if (_floorGenerator == null)
                _floorGenerator = levelHolder.AddComponent<FloorGenerator>();
            RegisterFloorGenerator(_floorGenerator);
        }

        private bool TrySpawnBakedLevel(int levelIndex, GameObject levelHolder)
        {
            var prefab = GetBakedLevelPrefab(levelIndex);
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[LevelManager] Resources prefab not found: " +
                    string.Format(BakedLevelResourceFormat, levelIndex) +
                    ". Will try FloorGenerator if possible.");
                return false;
            }

            _levelRoot = Instantiate(prefab, levelHolder.transform);
            _levelRoot.name = "LevelRoot";
            RegisterLevelRoot(_levelRoot);
            PlacePlayerAtLevelStart(_levelRoot);
            if (levelIndex == 2)
                EnsureLevel2ExitToLevel3(_levelRoot.transform);
            return true;
        }

        /// <summary>
        /// Baked Level_02 originally ended at L2 Boss with no level exit. Adds a Level 3 transition trigger
        /// (same layout as Level 1 → 2) without hand-editing the huge prefab.
        /// </summary>
        private static void EnsureLevel2ExitToLevel3(Transform levelRoot)
        {
            if (levelRoot == null) return;
            if (levelRoot.Find("LevelPatch_ToLevel3") != null) return;

            var patch = new GameObject("LevelPatch_ToLevel3");
            patch.transform.SetParent(levelRoot, false);
            // After L2 Boss (x≈54) and L2 Merchant (x≈72); exit corridor sits at next slot (x≈90).
            patch.transform.position = new Vector3(90f, 0f, 0f);

            var roomGo = new GameObject("Room_To Level 3");
            roomGo.transform.SetParent(patch.transform, false);
            roomGo.transform.localPosition = Vector3.zero;
            var box = roomGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(13f, 3f, 9f);
            roomGo.transform.localPosition = Vector3.zero;
            var rc = roomGo.AddComponent<RoomController>();
            rc.roomType = RoomType.LevelExit;
            rc.roomName = "To Level 3";

            // Single blue portal sphere — transition only when touching this (not the whole room trigger).
            var portal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            portal.name = "Level3Portal";
            portal.transform.SetParent(patch.transform, false);
            portal.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            portal.transform.localScale = new Vector3(2f, 2f, 2f);
            var pr = portal.GetComponent<Renderer>();
            if (pr != null) GrayboxTintUtil.Apply(pr, new Color(0.2f, 0.6f, 1f));
            Object.Destroy(portal.GetComponent<Collider>());
            var sph = portal.AddComponent<SphereCollider>();
            sph.isTrigger = true;
            sph.radius = 0.52f;
            var exit = portal.AddComponent<LevelExitTrigger>();
            exit.SetTargetLevel(3);
        }

        private GameObject GetBakedLevelPrefab(int levelIndex)
        {
            if (levelPrefabOverrides != null && levelIndex >= 1 && levelIndex <= levelPrefabOverrides.Length)
            {
                var o = levelPrefabOverrides[levelIndex - 1];
                if (o != null) return o;
            }

            return Resources.Load<GameObject>(string.Format(BakedLevelResourceFormat, levelIndex));
        }

        private void RefreshPlayerCache()
        {
            if (_player == null)
                _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void RefreshLevelRootCache()
        {
            if (_levelRoot != null) return;
            var levelGo = GameObject.Find("Level");
            if (levelGo != null)
            {
                _floorGenerator = levelGo.GetComponent<FloorGenerator>();
                _levelRoot = levelGo.transform.Find("LevelRoot")?.gameObject;
            }
            if (_levelRoot == null)
                _levelRoot = GameObject.Find("LevelRoot");
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null) return null;
            for (var i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == childName) return c;
                var deep = FindChildRecursive(c, childName);
                if (deep != null) return deep;
            }
            return null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
