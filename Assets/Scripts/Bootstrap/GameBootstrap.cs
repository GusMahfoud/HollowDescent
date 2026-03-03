using UnityEngine;
using HollowDescent.LevelGen;
using HollowDescent.Gameplay;
using HollowDescent.AI;
using HollowDescent.UI_Debug;

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

        private GameObject _player;
        private Camera _camera;
        private FloorGenerator _floorGen;
        private GameManager _gm;

        private void Start()
        {
            EnsureGameManager();
            EnsurePlayer();
            EnsureCamera();
            EnsureFloor();
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

        private void EnsurePlayer()
        {
            if (playerPrefabOrNull != null)
            {
                _player = Instantiate(playerPrefabOrNull);
                _player.name = "Player";
                return;
            }
            _player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _player.name = "Player";
            _player.transform.position = new Vector3(0f, 1f, 0f);
            _player.tag = "Player";
            var col = _player.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            _player.AddComponent<PlayerControllerTopDown>();
            _player.AddComponent<PlayerHealth>();
        }

        private void EnsureCamera()
        {
            if (mainCameraOrNull != null)
            {
                _camera = mainCameraOrNull;
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
            var tdFollow = camGo.AddComponent<TopDownCameraFollow>();
            if (_player != null) tdFollow.SetTarget(_player.transform);
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
    }
}
