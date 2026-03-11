using UnityEngine;
using UnityEngine.AI;
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
    /// Bootstraps the graybox level: Player, Camera, FloorGenerator, GameManager.
    /// Attach to empty GameObject in empty scene; press Play.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Optional: leave null to auto-create")]
        [SerializeField] private GameObject playerPrefabOrNull;
        [SerializeField] private Camera mainCameraOrNull;
        [SerializeField] private FloorGenerator floorGeneratorOrNull;
        [SerializeField] private GameManager gameManagerOrNull;
        [SerializeField] private AudioClip finalRoomNarrativeClip;

        private GameObject _player;
        private Camera _camera;
        private FloorGenerator _floorGen;
        private GameManager _gm;

        private void Start()
        {
            EnsureGameManager();
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

        private void EnsureLevelManager()
        {
            if (FindFirstObjectByType<LevelManager>() != null) return;
            var go = new GameObject("LevelManager");
            go.AddComponent<LevelManager>();
        }

        private void EnsurePlayer()
        {
            if (playerPrefabOrNull != null)
            {
                _player = Instantiate(playerPrefabOrNull);
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
        }

        private void EnsureCamera()
        {
            if (mainCameraOrNull != null)
            {
                _camera = mainCameraOrNull;
                EnsureSingleAudioListener(_camera);
                var follow = _camera.GetComponent<TopDownCameraFollow>();
                if (follow == null) follow = _camera.gameObject.AddComponent<TopDownCameraFollow>();
                if (_player != null) follow.SetTarget(_player.transform);
                return;
            }
            var camGo = new GameObject("Main Camera");
            _camera = camGo.AddComponent<Camera>();
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
                Destroy(listener);
            }
        }

        private void EnsureFloor()
        {
            if (floorGeneratorOrNull != null)
            {
                _floorGen = floorGeneratorOrNull;
                if (!_floorGen.HasGenerated()) _floorGen.Generate();
                return;
            }
            var levelGo = new GameObject("Level");
            _floorGen = levelGo.AddComponent<FloorGenerator>();
            _floorGen.Generate();
            var follow = FindFirstObjectByType<TopDownCameraFollow>();
            if (follow != null && _player != null) follow.SetTarget(_player.transform);
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

            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NarrativeWitnessNPC";
            npc.transform.SetParent(roomGeometry);
            npc.transform.position = roomCenter + new Vector3(-2.2f, 1f, 0.8f);
            npc.transform.localScale = new Vector3(1.2f, 1.25f, 1.2f);
            var npcRenderer = npc.GetComponent<Renderer>();
            if (npcRenderer != null) npcRenderer.material.color = new Color(0.75f, 0.75f, 0.85f);
            Debug.Log($"[Narrative] Spawned NPC in final Level 1 room at {npc.transform.position}");

            if (NavMesh.SamplePosition(npc.transform.position, out _, 2f, NavMesh.AllAreas))
            {
                var npcAgent = npc.AddComponent<NavMeshAgent>();
                npcAgent.speed = 3.2f;
                npcAgent.angularSpeed = 720f;
                npcAgent.acceleration = 14f;
                npcAgent.stoppingDistance = 0.6f;
            }
            var npcReact = npc.AddComponent<NPCNavReact>();

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
    }
}
