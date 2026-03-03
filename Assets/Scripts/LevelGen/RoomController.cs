using System.Collections.Generic;
using UnityEngine;
using HollowDescent.AI;
using HollowDescent.Bootstrap;
using HollowDescent.Gameplay;

namespace HollowDescent.LevelGen
{
    public enum RoomType
    {
        StartSafe,
        Combat,
        Safe,
        Boss,
        Exit
    }

    /// <summary>
    /// Per-room logic: trigger lock-in, spawn enemies, clear reward, flanker spawn points.
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [Header("Config")]
        public RoomType roomType;
        public string roomName = "Room";
        public List<Transform> doorBlockers = new List<Transform>();
        public List<Transform> enemySpawnPoints = new List<Transform>();
        public List<bool> spawnPointIsFlanker = new List<bool>();

        [Header("Enemy Prefabs (optional; null = use runtime creation)")]
        [SerializeField] private GameObject chaserPrefab;
        [SerializeField] private GameObject shooterPrefab;
        [SerializeField] private GameObject flankerPrefab;

        [Header("Encounter")]
        public int chaserCount = 2;
        public int shooterCount = 0;
        public int flankerCount = 1;

        private bool _encounterActive;
        private bool _encounterCleared;
        private readonly List<EnemyBase> _spawnedEnemies = new List<EnemyBase>();
        private GameObject _rewardMarker;
        private BoxCollider _trigger;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            if (_trigger != null) _trigger.isTrigger = true;
        }

        private void Start()
        {
            if (roomType == RoomType.StartSafe || roomType == RoomType.Safe)
                SetDoorsOpen(true);
        }

        public void SetDoorsOpen(bool open)
        {
            foreach (var d in doorBlockers)
                if (d != null) d.gameObject.SetActive(!open);
        }

        public void RegisterDoor(Transform doorBlocker)
        {
            if (doorBlocker != null && !doorBlockers.Contains(doorBlocker))
                doorBlockers.Add(doorBlocker);
        }

        public void RegisterSpawnPoint(Transform t, bool isFlanker)
        {
            if (t == null) return;
            enemySpawnPoints.Add(t);
            spawnPointIsFlanker.Add(isFlanker);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (GameManager.Instance != null)
                GameManager.Instance.SetCurrentRoom(roomName);
            if (roomType == RoomType.StartSafe || roomType == RoomType.Safe) return;
            if (_encounterCleared) return;
            if (roomType != RoomType.Combat && roomType != RoomType.Boss) return;
            if (_encounterActive) return;

            _encounterActive = true;
            SetDoorsOpen(false);
            SpawnEnemies();
        }

        private void SpawnEnemies()
        {
            _spawnedEnemies.Clear();
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var spawns = new List<(Transform t, bool flank)>();
            for (var i = 0; i < enemySpawnPoints.Count; i++)
                spawns.Add((enemySpawnPoints[i], i < spawnPointIsFlanker.Count && spawnPointIsFlanker[i]));

            var chaserSpawns = new List<Transform>();
            var flankerSpawns = new List<Transform>();
            var shooterSpawns = new List<Transform>();
            foreach (var (t, flank) in spawns)
            {
                if (flank) flankerSpawns.Add(t);
                else
                {
                    chaserSpawns.Add(t);
                    shooterSpawns.Add(t);
                }
            }
            if (chaserSpawns.Count == 0) chaserSpawns.AddRange(enemySpawnPoints);
            if (flankerSpawns.Count == 0 && enemySpawnPoints.Count > 1)
                flankerSpawns.Add(enemySpawnPoints[enemySpawnPoints.Count - 1]);

            var chaserIndex = 0;
            for (var i = 0; i < chaserCount && chaserSpawns.Count > 0; i++)
            {
                var sp = chaserSpawns[chaserIndex % chaserSpawns.Count];
                var e = SpawnEnemyAt(sp.position, false, false);
                if (e != null) _spawnedEnemies.Add(e);
                chaserIndex++;
            }
            for (var i = 0; i < shooterCount && shooterSpawns.Count > 0; i++)
            {
                var sp = shooterSpawns[i % shooterSpawns.Count];
                var e = SpawnEnemyAt(sp.position, true, false);
                if (e != null) _spawnedEnemies.Add(e);
            }
            for (var i = 0; i < flankerCount && flankerSpawns.Count > 0; i++)
            {
                var sp = flankerSpawns[i % flankerSpawns.Count];
                var e = SpawnEnemyAt(sp.position, false, true);
                if (e != null) _spawnedEnemies.Add(e);
            }

            UpdateGMEnemyCount();
        }

        private EnemyBase SpawnEnemyAt(Vector3 pos, bool shooter, bool flanker)
        {
            GameObject prefab = null;
            if (flanker && flankerPrefab != null) prefab = flankerPrefab;
            else if (shooter && shooterPrefab != null) prefab = shooterPrefab;
            else if (chaserPrefab != null) prefab = chaserPrefab;

            if (prefab != null)
            {
                var go = Instantiate(prefab, pos, Quaternion.identity);
                var eb = go.GetComponent<EnemyBase>();
                if (eb != null)
                {
                    eb.OnDeath += OnEnemyDied;
                    return eb;
                }
            }

            GameObject primitive;
            if (flanker) primitive = CreateFlankerPrimitive(pos);
            else if (shooter) primitive = CreateShooterPrimitive(pos);
            else primitive = CreateChaserPrimitive(pos);

            var enemy = primitive.GetComponent<EnemyBase>();
            if (enemy != null) enemy.OnDeath += OnEnemyDied;
            return enemy;
        }

        private GameObject CreateChaserPrimitive(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Enemy_Chaser";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
            go.tag = "Enemy";
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            var rb = go.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var mat = go.GetComponent<Renderer>().material;
            if (mat != null) mat.color = new Color(0.9f, 0.2f, 0.2f);
            var chaser = go.AddComponent<EnemyChaser>();
            chaser.SetPlayer(GameObject.FindGameObjectWithTag("Player")?.transform);
            return go;
        }

        private GameObject CreateShooterPrimitive(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Enemy_Shooter";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            go.tag = "Enemy";
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            var rb = go.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            var mat = go.GetComponent<Renderer>().material;
            if (mat != null) mat.color = new Color(0.8f, 0.4f, 0.1f);
            var shooter = go.AddComponent<EnemyShooter>();
            shooter.SetPlayer(GameObject.FindGameObjectWithTag("Player")?.transform);
            return go;
        }

        private GameObject CreateFlankerPrimitive(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Enemy_Flanker";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            go.tag = "Enemy";
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            var rb = go.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            var mat = go.GetComponent<Renderer>().material;
            if (mat != null) mat.color = new Color(0.6f, 0.2f, 0.6f);
            var flanker = go.AddComponent<EnemyFlanker>();
            flanker.SetPlayer(GameObject.FindGameObjectWithTag("Player")?.transform);
            return go;
        }

        private void OnEnemyDied(EnemyBase enemy)
        {
            _spawnedEnemies.Remove(enemy);
            UpdateGMEnemyCount();
            if (_spawnedEnemies.Count <= 0)
                OnEncounterCleared();
        }

        private void UpdateGMEnemyCount()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetEnemiesRemaining(_spawnedEnemies.Count);
        }

        private void OnEncounterCleared()
        {
            _encounterCleared = true;
            SetDoorsOpen(true);
            SpawnRewardMarker();
            if (GameManager.Instance != null)
                GameManager.Instance.SetEnemiesRemaining(0);
        }

        private void SpawnRewardMarker()
        {
            if (_rewardMarker != null) return;
            var center = _trigger != null ? _trigger.bounds.center : transform.position;
            _rewardMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _rewardMarker.name = "RewardMarker";
            _rewardMarker.transform.position = center + Vector3.up * 0.6f;
            _rewardMarker.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            _rewardMarker.GetComponent<Collider>().enabled = false;
            var r = _rewardMarker.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1f, 0.9f, 0.2f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = roomType == RoomType.StartSafe || roomType == RoomType.Safe ? Color.green : Color.red;
            var b = _trigger != null ? _trigger.bounds : new Bounds(transform.position, Vector3.one * 10f);
            Gizmos.DrawWireCube(b.center, b.size);
            foreach (var sp in enemySpawnPoints)
                if (sp != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(sp.position, 0.5f);
                }
        }
    }
}
