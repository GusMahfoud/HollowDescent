using System;
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
        Exit,
        LevelExit
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

        private bool _isShopRoom;
        private bool _encounterActive;
        private bool _encounterCleared;
        private readonly List<EnemyBase> _spawnedEnemies = new List<EnemyBase>();
        private GameObject _rewardMarker;
        private BoxCollider _trigger;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            if (_trigger != null) _trigger.isTrigger = true;
            ReconcileDoorBlockersFromScene();
        }

        /// <summary>
        /// Serialized door references can break after bake/instance changes; merge in any DoorBlocker under this room's geometry.
        /// </summary>
        private void ReconcileDoorBlockersFromScene()
        {
            doorBlockers.RemoveAll(d => d == null);

            var levelRoot = transform.parent;
            if (levelRoot == null) return;

            Transform geo = null;
            var expected = "RoomGeometry_" + roomName;
            for (var i = 0; i < levelRoot.childCount; i++)
            {
                var c = levelRoot.GetChild(i);
                if (c.name == expected ||
                    (c.name.StartsWith("RoomGeometry_", StringComparison.Ordinal) &&
                     roomName != null &&
                     c.name.EndsWith(roomName, StringComparison.Ordinal)))
                {
                    geo = c;
                    break;
                }
            }
            if (geo == null) return;

            for (var i = 0; i < geo.childCount; i++)
            {
                var t = geo.GetChild(i);
                if (t.name == "DoorBlocker")
                    RegisterDoor(t);
            }
        }

        private void Start()
        {
            _isShopRoom = roomName == "Shop (Safe)";
            // Rooms should be traversable by default; combat rooms lock only once encounter starts.
            SetDoorsOpen(true, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_isShopRoom && other != null && other.CompareTag("Player"))
                Gameplay.ShopSystem.Instance?.ExitShopRoom();
        }

        public void SetDoorsOpen(bool open, bool instant = false)
        {
            foreach (var d in doorBlockers)
            {
                if (d == null) continue;
                var blocker = d.GetComponent<DoorBlocker>();
                if (blocker == null) blocker = d.gameObject.AddComponent<DoorBlocker>();
                blocker.SetOpen(open, instant);
            }
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
            if (_isShopRoom)
                Gameplay.ShopSystem.Instance?.EnterShopRoom();
            if (roomType == RoomType.Combat || roomType == RoomType.Boss)
            {
                SetRoomEnemiesPursuitEnabled(true);
                if (_encounterActive && !_encounterCleared)
                    SetDoorsOpen(false);
            }
            if (roomType == RoomType.StartSafe || roomType == RoomType.Safe || roomType == RoomType.LevelExit) return;
            if (_encounterCleared) return;
            if (roomType != RoomType.Combat && roomType != RoomType.Boss) return;
            if (_encounterActive) return;

            _encounterActive = true;
            SetDoorsOpen(false);
            SpawnEnemies();
        }

        public static RoomController FindByRoomName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var rooms = FindObjectsByType<RoomController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var room in rooms)
            {
                if (room == null) continue;
                if (string.Equals(room.roomName, name, StringComparison.OrdinalIgnoreCase))
                    return room;
            }
            return null;
        }

        public void HandlePlayerDeathInRoom()
        {
            if (roomType != RoomType.Combat && roomType != RoomType.Boss) return;
            if (_encounterCleared) return;
            SetDoorsOpen(true);
            SetRoomEnemiesPursuitEnabled(false);
        }

        private void SetRoomEnemiesPursuitEnabled(bool enabled)
        {
            _spawnedEnemies.RemoveAll(e => e == null);
            foreach (var enemy in _spawnedEnemies)
                enemy.SetPursuitEnabled(enabled);
        }

        private void SpawnEnemies()
        {
            _spawnedEnemies.Clear();
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                FinishEncounterIfNoEnemiesSpawned();
                return;
            }

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
            // Set enemy composition for HUD display
            var parts = new List<string>();
            if (chaserCount > 0) parts.Add($"{chaserCount} Chaser{(chaserCount > 1 ? "s" : "")}");
            if (shooterCount > 0) parts.Add($"{shooterCount} Shooter{(shooterCount > 1 ? "s" : "")}");
            if (flankerCount > 0) parts.Add($"{flankerCount} Flanker{(flankerCount > 1 ? "s" : "")}");
            GameManager.Instance?.SetEnemyComposition(string.Join(", ", parts));
            FinishEncounterIfNoEnemiesSpawned();
        }

        /// <summary>
        /// Prevents soft-lock when spawn lists are empty, counts are zero, or Player was missing during spawn.
        /// </summary>
        private void FinishEncounterIfNoEnemiesSpawned()
        {
            if (!_encounterActive || _encounterCleared) return;
            if (_spawnedEnemies.Count > 0) return;
            OnEncounterCleared();
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
                    ApplySpawnVariety(eb, shooter, flanker);
                    return eb;
                }
            }

            GameObject primitive;
            if (flanker) primitive = CreateFlankerPrimitive(pos);
            else if (shooter) primitive = CreateShooterPrimitive(pos);
            else primitive = CreateChaserPrimitive(pos);

            var enemy = primitive.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.OnDeath += OnEnemyDied;
                ApplySpawnVariety(enemy, shooter, flanker);
            }
            return enemy;
        }

        /// <summary>Per-instance stat bands so chaser / shooter / flanker feel different run-to-run.</summary>
        private void ApplySpawnVariety(EnemyBase eb, bool shooter, bool flanker)
        {
            if (eb == null) return;
            var seed = eb.gameObject.GetInstanceID() ^ (roomName != null ? roomName.GetHashCode() : 0);
            var rng = new System.Random(seed);
            var bossBump = roomType == RoomType.Boss ? rng.Next(0, 2) : 0;
            var levelBump = (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel >= 2) ? 1 : 0;

            if (flanker && eb is EnemyFlanker ef)
            {
                eb.ApplyRuntimeCombatStats(rng.Next(2, 5) + bossBump + levelBump, rng.Next(1, 3));
                ef.ApplyMovementProfile(rng.Next(38, 63) / 10f, rng.Next(35, 105) / 100f);
                MaybeTintRuntimePrimitive(eb, rng, flanker, shooter);
            }
            else if (shooter && eb is EnemyShooter es)
            {
                eb.ApplyRuntimeCombatStats(rng.Next(2, 4) + bossBump + levelBump, rng.Next(1, 3));
                es.ApplyCombatProfile(
                    rng.Next(24, 36) / 10f,
                    rng.Next(50, 80) / 10f,
                    rng.Next(11, 22) / 10f,
                    rng.Next(85, 125) / 10f);
                MaybeTintRuntimePrimitive(eb, rng, flanker, shooter);
            }
            else if (eb is EnemyChaser ec)
            {
                eb.ApplyRuntimeCombatStats(rng.Next(1, 4) + bossBump + levelBump, rng.Next(1, 3));
                var speedBoost = levelBump > 0 ? 0.5f : 0f;
                ec.ApplyMovementProfile(rng.Next(30, 52) / 10f + speedBoost);
                MaybeTintRuntimePrimitive(eb, rng, flanker, shooter);
            }
            else
            {
                eb.ApplyRuntimeCombatStats(rng.Next(2, 4) + bossBump + levelBump, rng.Next(1, 3));
            }
        }

        private static void MaybeTintRuntimePrimitive(EnemyBase eb, System.Random rng, bool flanker, bool shooter)
        {
            if (eb == null || !eb.gameObject.name.StartsWith("Enemy_", StringComparison.Ordinal)) return;
            var r = eb.GetComponent<Renderer>();
            if (r == null) return;
            if (flanker)
                GrayboxTintUtil.Apply(r, new Color(0.5f + (float)rng.NextDouble() * 0.25f, 0.1f, 0.5f + (float)rng.NextDouble() * 0.22f));
            else if (shooter)
                GrayboxTintUtil.Apply(r, new Color(0.78f + (float)rng.NextDouble() * 0.15f, 0.32f + (float)rng.NextDouble() * 0.18f, 0.06f));
            else
                GrayboxTintUtil.Apply(r, new Color(0.85f + (float)rng.NextDouble() * 0.1f, 0.1f + (float)rng.NextDouble() * 0.18f, 0.1f + (float)rng.NextDouble() * 0.18f));
        }

        private GameObject CreateChaserPrimitive(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Enemy_Chaser";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            GrayboxTintUtil.Apply(go.GetComponent<Renderer>(), new Color(0.9f, 0.2f, 0.2f));
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
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            GrayboxTintUtil.Apply(go.GetComponent<Renderer>(), new Color(0.8f, 0.4f, 0.1f));
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
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            GrayboxTintUtil.Apply(go.GetComponent<Renderer>(), new Color(0.6f, 0.2f, 0.6f));
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
            GameManager.Instance?.SetEnemyComposition("");
            var echoBonus = roomType == RoomType.Boss ? 25 : 10;
            RunState.Instance?.RecordRoomCleared();
            RunState.Instance?.AddCurrency(echoBonus);
            if (roomType == RoomType.Boss)
                RunState.Instance?.MarkRunComplete();
            var hud = FindFirstObjectByType<HollowDescent.UI_Debug.MinimalHUD>();
            if (hud != null) hud.NotifyRoomCleared(echoBonus, roomType == RoomType.Boss);
        }

        private void SpawnRewardMarker()
        {
            if (_rewardMarker != null) return;
            var center = _trigger != null ? _trigger.bounds.center : transform.position;
            _rewardMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _rewardMarker.name = "RewardMarker";
            _rewardMarker.transform.position = center + Vector3.up * 0.6f;
            _rewardMarker.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            var col = _rewardMarker.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }
            var r = _rewardMarker.GetComponent<Renderer>();
            if (r != null) GrayboxTintUtil.Apply(r, new Color(1f, 0.9f, 0.2f));
            var pickup = _rewardMarker.AddComponent<RewardPickup>();
            pickup.SetAmount(roomType == RoomType.Boss ? 40 : 15);
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
