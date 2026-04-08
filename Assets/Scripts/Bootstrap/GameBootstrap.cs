using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
using HollowDescent.LevelGen;
using HollowDescent.Gameplay;
using HollowDescent.AI;
using HollowDescent.UI_Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HollowDescent.Bootstrap
{
    /// <summary>
    /// Bootstraps the game: managers, Player (prefab/Resources), Camera, baked or procedural level, narrative NPC.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        private const string PlayerResourcePath = "Prefabs/Characters/Player";
        private const string WitnessResourcePath = "Prefabs/Characters/NarrativeWitnessNPC";
        private const string Level1ResourcePath = "Prefabs/Levels/Level_01";
        private const string BackgroundMusicResourcePath = "Audio/background-music";

        [Header("Optional: leave null to use Resources path or runtime fallback")]
        [SerializeField] private GameObject playerPrefabOrNull;
        [SerializeField] private GameObject witnessNpcPrefabOrNull;
        [SerializeField] private Camera mainCameraOrNull;
        [SerializeField] private FloorGenerator floorGeneratorOrNull;
        [SerializeField] private GameManager gameManagerOrNull;
        [SerializeField] private AudioClip finalRoomNarrativeClip;
        [Header("Audio")]
        [SerializeField] private AudioClip backgroundMusicClipOrNull;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.12f;
        [Header("Startup Menu")]
        [SerializeField] private bool showStartupMenu = true;
        [SerializeField] private string startupGameTitle = "Hollow Descent";
        [SerializeField] private string startupButtonText = "Start Game";

        private GameObject _player;
        private Camera _camera;
        private FloorGenerator _floorGen;
        private GameManager _gm;
        private bool _hasBootstrapped;

        private void Start()
        {
            EnsureBackgroundMusic();

            if (showStartupMenu)
            {
                StartMenuOverlay.Show(startupGameTitle, startupButtonText, BootstrapGame);
                return;
            }

            BootstrapGame();
        }

        /// <summary>
        /// Called by GameManager to return to main menu. Resets bootstrap state and shows the start overlay.
        /// </summary>
        public void ShowMainMenu()
        {
            _hasBootstrapped = false;
            StartMenuOverlay.Show(startupGameTitle, startupButtonText, RebootGame);
        }

        private void RebootGame()
        {
            _hasBootstrapped = true;

            // Re-activate player (may be inactive, so tag search won't find it)
            if (_player != null) _player.SetActive(true);
            var playerGo = _player != null ? _player : GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) playerGo.SetActive(true);

            // Rebuild level
            if (LevelManager.Instance != null)
                LevelManager.Instance.LoadLevel(1);

            EnsureNarrativeTrigger();
        }

        private void BootstrapGame()
        {
            if (_hasBootstrapped) return;
            _hasBootstrapped = true;
            EnsureGameManager();
            EnsureRunState();
            EnsureShopSystem();
            EnsureLevelManager();
            EnsurePlayer();
            EnsureCamera();
            EnsureFloor();
            EnsureNarrativeTrigger();
        }

        private void EnsureGameManager()
        {
            if (gameManagerOrNull != null)
            {
                _gm = gameManagerOrNull;
                return;
            }
            var go = new GameObject("GameManager");
            _gm = go.AddComponent<GameManager>();
            go.AddComponent<MinimalHUD>();
        }

        private void EnsureRunState()
        {
            if (FindFirstObjectByType<RunState>() != null) return;
            var go = new GameObject("RunState");
            go.AddComponent<RunState>();
        }

        private void EnsureShopSystem()
        {
            if (FindFirstObjectByType<ShopSystem>() != null) return;
            var go = new GameObject("ShopSystem");
            go.AddComponent<ShopSystem>();
        }

        private void EnsureLevelManager()
        {
            if (FindFirstObjectByType<LevelManager>() != null) return;
            var go = new GameObject("LevelManager");
            go.AddComponent<LevelManager>();
        }

        private void EnsurePlayer()
        {
            var existing = GameObject.FindGameObjectWithTag("Player");
            if (existing != null)
            {
                _player = existing;
                ConfigurePlayerPhysics(_player);
                return;
            }

            var prefab = playerPrefabOrNull != null ? playerPrefabOrNull : Resources.Load<GameObject>(PlayerResourcePath);
            if (prefab != null)
            {
                _player = Instantiate(prefab);
                _player.name = "Player";
                ConfigurePlayerPhysics(_player);
                return;
            }

            _player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _player.name = "Player";
            _player.transform.position = new Vector3(0f, 1f, 0f);
            _player.tag = "Player";
            ConfigurePlayerPhysics(_player);
            _player.AddComponent<PlayerControllerTopDown>();
            _player.AddComponent<PlayerHealth>();
        }

        private static void ConfigurePlayerPhysics(GameObject player)
        {
            if (player == null) return;
            var col = player.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            var rb = player.GetComponent<Rigidbody>();
            if (rb == null) rb = player.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (player.GetComponent<PlayerHitFlash>() == null)
                player.AddComponent<PlayerHitFlash>();
            if (player.GetComponent<PlayerStatModifiers>() == null)
                player.AddComponent<PlayerStatModifiers>();
        }

        private void EnsureCamera()
        {
            if (mainCameraOrNull != null)
            {
                _camera = mainCameraOrNull;
                EnsureUrpCameraData(_camera.gameObject);
                EnsureSingleAudioListener(_camera);
                var follow = _camera.GetComponent<TopDownCameraFollow>();
                if (follow == null) follow = _camera.gameObject.AddComponent<TopDownCameraFollow>();
                if (_player != null) follow.SetTarget(_player.transform);
                return;
            }

            var existingGo = GameObject.FindGameObjectWithTag("MainCamera");
            if (existingGo != null)
            {
                var existingCam = existingGo.GetComponent<Camera>();
                if (existingCam != null)
                {
                    _camera = existingCam;
                    EnsureUrpCameraData(_camera.gameObject);
                    EnsureSingleAudioListener(_camera);
                    var follow = _camera.GetComponent<TopDownCameraFollow>();
                    if (follow == null) follow = _camera.gameObject.AddComponent<TopDownCameraFollow>();
                    if (_player != null) follow.SetTarget(_player.transform);
                    return;
                }
            }

            var camGo = new GameObject("Main Camera");
            _camera = camGo.AddComponent<Camera>();
            EnsureUrpCameraData(camGo);
            _camera.orthographic = false;
            _camera.transform.position = new Vector3(0f, 18f, -8f);
            _camera.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            _camera.tag = "MainCamera";
            EnsureSingleAudioListener(_camera);
            var tdFollow = camGo.AddComponent<TopDownCameraFollow>();
            if (_player != null) tdFollow.SetTarget(_player.transform);
        }

        private static void EnsureUrpCameraData(GameObject cameraObject)
        {
            if (cameraObject == null) return;
            if (cameraObject.GetComponent<UniversalAdditionalCameraData>() == null)
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
        }

        private static void EnsureSingleAudioListener(Camera targetCamera)
        {
            if (targetCamera == null) return;

            var targetListener = targetCamera.GetComponent<AudioListener>();
            if (targetListener == null)
                targetListener = targetCamera.gameObject.AddComponent<AudioListener>();

            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var listener in listeners)
            {
                if (listener == null) continue;
                if (listener == targetListener)
                {
                    listener.enabled = true;
                    continue;
                }
                Object.Destroy(listener);
            }
        }

        private void EnsureFloor()
        {
            var follow = FindFirstObjectByType<TopDownCameraFollow>();

            if (floorGeneratorOrNull != null)
            {
                _floorGen = floorGeneratorOrNull;
                LevelManager.Instance?.RegisterFloorGenerator(_floorGen);
                if (!_floorGen.HasGenerated()) _floorGen.Generate();
                if (follow != null && _player != null) follow.SetTarget(_player.transform);
                LevelManager.Instance?.PlacePlayerAtLevelStart();
                return;
            }

            var existingRoot = GameObject.Find("LevelRoot");
            if (existingRoot != null)
            {
                LevelManager.Instance?.RegisterLevelRoot(existingRoot);
                LevelManager.Instance?.PlacePlayerAtLevelStart(existingRoot);
                if (follow != null && _player != null) follow.SetTarget(_player.transform);
                return;
            }

            var baked = Resources.Load<GameObject>(Level1ResourcePath);
            if (baked != null)
            {
                var levelParent = GameObject.Find("Level") ?? new GameObject("Level");
                var root = Instantiate(baked, levelParent.transform);
                root.name = "LevelRoot";
                LevelManager.Instance?.RegisterLevelRoot(root);
                LevelManager.Instance?.PlacePlayerAtLevelStart(root);
                if (follow != null && _player != null) follow.SetTarget(_player.transform);
                return;
            }

            var levelGo = new GameObject("Level");
            _floorGen = levelGo.AddComponent<FloorGenerator>();
            LevelManager.Instance?.RegisterFloorGenerator(_floorGen);
            _floorGen.Generate();
            if (follow != null && _player != null) follow.SetTarget(_player.transform);
            LevelManager.Instance?.PlacePlayerAtLevelStart();
        }

        private void EnsureNarrativeTrigger()
        {
            var levelRoot = GameObject.Find("LevelRoot");
            if (levelRoot == null) return;

            var roomTrigger = levelRoot.transform.Find("Room_To Level 2");
            var roomGeometry = levelRoot.transform.Find("RoomGeometry_To Level 2");
            if (roomTrigger == null || roomGeometry == null)
            {
                Debug.LogWarning("[Narrative] Could not find Level 1 final room objects (Room_To Level 2 / RoomGeometry_To Level 2).");
                return;
            }

            var roomCenter = roomGeometry.position;
            var triggerCollider = roomTrigger.GetComponent<BoxCollider>();
            if (triggerCollider != null)
                roomCenter = triggerCollider.bounds.center;

            var npcReact = roomGeometry.GetComponentInChildren<NPCNavReact>(true);
            GameObject npc;

            if (npcReact != null)
            {
                npc = npcReact.gameObject;
                Debug.Log($"[Narrative] Using existing witness NPC at {npc.transform.position}");
            }
            else
            {
                var witnessPrefab = witnessNpcPrefabOrNull != null
                    ? witnessNpcPrefabOrNull
                    : Resources.Load<GameObject>(WitnessResourcePath);

                if (witnessPrefab == null)
                {
                    npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    npc.name = "NarrativeWitnessNPC";
                    npc.transform.SetParent(roomGeometry);
                    npc.transform.position = roomCenter + new Vector3(-2.2f, 1f, 0.8f);
                    npc.transform.localScale = new Vector3(1.2f, 1.25f, 1.2f);
                    var npcRenderer = npc.GetComponent<Renderer>();
                    if (npcRenderer != null) GrayboxTintUtil.Apply(npcRenderer, new Color(0.75f, 0.75f, 0.85f));
                    if (NavMesh.SamplePosition(npc.transform.position, out _, 2f, NavMesh.AllAreas))
                    {
                        var npcAgent = npc.AddComponent<NavMeshAgent>();
                        npcAgent.speed = 3.2f;
                        npcAgent.angularSpeed = 720f;
                        npcAgent.acceleration = 14f;
                        npcAgent.stoppingDistance = 0.6f;
                    }
                    npcReact = npc.AddComponent<NPCNavReact>();
                }
                else
                {
                    npc = Instantiate(witnessPrefab, roomGeometry);
                    npc.name = "NarrativeWitnessNPC";
                    npc.transform.position = roomCenter + new Vector3(-2.2f, 1f, 0.8f);
                    npc.transform.rotation = Quaternion.identity;
                    npcReact = npc.GetComponent<NPCNavReact>();
                    if (npcReact == null) npcReact = npc.AddComponent<NPCNavReact>();
                    Debug.Log($"[Narrative] Spawned witness prefab in final Level 1 room at {npc.transform.position}");
                }
            }

            var narrativeEvent = roomTrigger.GetComponent<NarrativeTriggerEvent>();
            if (narrativeEvent == null)
                narrativeEvent = roomTrigger.gameObject.AddComponent<NarrativeTriggerEvent>();
            narrativeEvent.SetReactingNpc(npcReact);
            var resolvedClip = ResolveFinalRoomNarrativeClip();
            narrativeEvent.SetNarrativeClip(resolvedClip);
            if (resolvedClip == null)
                Debug.LogWarning("[Narrative] No custom clip assigned on GameBootstrap.finalRoomNarrativeClip. Using fallback eerie audio.");
            else
                Debug.Log($"[Narrative] Using custom clip: {resolvedClip.name}");
        }

        private AudioClip ResolveFinalRoomNarrativeClip()
        {
            if (finalRoomNarrativeClip != null) return finalRoomNarrativeClip;

#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets("Errie_Sound t:AudioClip");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                finalRoomNarrativeClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
#endif
            return finalRoomNarrativeClip;
        }

        private void EnsureBackgroundMusic()
        {
            var existing = GameObject.Find("BackgroundMusic");
            if (existing != null)
            {
                var existingSource = existing.GetComponent<AudioSource>();
                if (existingSource != null)
                {
                    existingSource.loop = true;
                    existingSource.spatialBlend = 0f;
                    existingSource.volume = backgroundMusicVolume;
                    if (!existingSource.isPlaying)
                        existingSource.Play();
                }
                return;
            }

            var clip = ResolveBackgroundMusicClip();
            if (clip == null)
            {
                Debug.LogWarning("[Audio] Background music clip not found. Assign GameBootstrap.backgroundMusicClipOrNull or add Resources/Audio/background-music.");
                return;
            }

            var go = new GameObject("BackgroundMusic");
            DontDestroyOnLoad(go);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
            source.volume = backgroundMusicVolume;
            source.priority = 200; // Lower priority than critical SFX so effects remain clear.
            source.ignoreListenerPause = true;
            source.Play();
        }

        private AudioClip ResolveBackgroundMusicClip()
        {
            if (backgroundMusicClipOrNull != null) return backgroundMusicClipOrNull;

            backgroundMusicClipOrNull = Resources.Load<AudioClip>(BackgroundMusicResourcePath);
            if (backgroundMusicClipOrNull != null) return backgroundMusicClipOrNull;

#if UNITY_EDITOR
            backgroundMusicClipOrNull = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/background-music.mp3");
#endif
            return backgroundMusicClipOrNull;
        }
    }
}
