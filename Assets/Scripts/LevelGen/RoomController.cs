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
        LevelExit,
        /// <summary>Unique final encounter; run completes only after this boss falls.</summary>
        FinalBoss
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
        [SerializeField] private GameObject finalBossPrefab;

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
            _isShopRoom = roomName == "Shop (Safe)" || roomName == "L2 Merchant (Safe)";
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
            if (roomType == RoomType.Combat || roomType == RoomType.Boss || roomType == RoomType.FinalBoss)
            {
                SetRoomEnemiesPursuitEnabled(true);
                if (_encounterActive && !_encounterCleared)
                    SetDoorsOpen(false);
            }
            if (roomType == RoomType.StartSafe || roomType == RoomType.Safe || roomType == RoomType.LevelExit) return;
            if (_encounterCleared) return;
            if (roomType != RoomType.Combat && roomType != RoomType.Boss && roomType != RoomType.FinalBoss) return;
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
            if (roomType != RoomType.Combat && roomType != RoomType.Boss && roomType != RoomType.FinalBoss) return;
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
            if (roomType == RoomType.FinalBoss)
            {
                SpawnFinalBossEncounter();
                return;
            }

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

        private void SpawnFinalBossEncounter()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                FinishEncounterIfNoEnemiesSpawned();
                return;
            }

            var pos = enemySpawnPoints.Count > 0 ? enemySpawnPoints[0].position : transform.position + Vector3.up * 0.1f;
            GameObject go;
            EnemyFinalBoss fb;
            if (finalBossPrefab != null)
            {
                go = Instantiate(finalBossPrefab, pos, Quaternion.identity);
                go.transform.SetParent(transform, true);
                fb = go.GetComponent<EnemyFinalBoss>();
                if (fb == null) fb = go.AddComponent<EnemyFinalBoss>();
            }
            else
            {
                go = CreateFinalBossPrimitive(pos);
                go.transform.SetParent(transform, true);
                fb = go.GetComponent<EnemyFinalBoss>();
            }

            fb.Initialize(this);
            fb.OnDeath += OnEnemyDied;
            _spawnedEnemies.Add(fb);
            UpdateGMEnemyCount();
            GameManager.Instance?.SetEnemyComposition("The Architect");
            FinishEncounterIfNoEnemiesSpawned();
        }

        private static GameObject CreateFinalBossPrimitive(Vector3 pos)
        {
            var go = SimpleFigureVisuals.CreateFinalBossEnemy(pos);
            go.AddComponent<EnemyFinalBoss>();
            return go;
        }

        /// <summary>Minions spawned by <see cref="EnemyFinalBoss"/>; killing them does not clear the room.</summary>
        public void RegisterFinalBossMinion(EnemyBase minion)
        {
            if (minion == null || roomType != RoomType.FinalBoss) return;
            minion.OnDeath += OnFinalBossMinionDied;
            _spawnedEnemies.Add(minion);
            UpdateGMEnemyCount();
        }

        private void OnFinalBossMinionDied(EnemyBase m)
        {
            if (m != null) m.OnDeath -= OnFinalBossMinionDied;
            _spawnedEnemies.Remove(m);
            UpdateGMEnemyCount();
        }

        private void FinalBossCleanupMinions()
        {
            for (var i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                var e = _spawnedEnemies[i];
                if (e == null || e is EnemyFinalBoss) continue;
                e.OnDeath -= OnFinalBossMinionDied;
                Destroy(e.gameObject);
                _spawnedEnemies.RemoveAt(i);
            }
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
                go.transform.SetParent(transform, true);
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
                primitive.transform.SetParent(transform, true);
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
            Color c;
            if (flanker)
                c = new Color(0.5f + (float)rng.NextDouble() * 0.25f, 0.1f, 0.5f + (float)rng.NextDouble() * 0.22f);
            else if (shooter)
                c = new Color(0.78f + (float)rng.NextDouble() * 0.15f, 0.32f + (float)rng.NextDouble() * 0.18f, 0.06f);
            else
                c = new Color(0.85f + (float)rng.NextDouble() * 0.1f, 0.1f + (float)rng.NextDouble() * 0.18f, 0.1f + (float)rng.NextDouble() * 0.18f);
            foreach (var r in eb.GetComponentsInChildren<Renderer>())
            {
                if (r != null) GrayboxTintUtil.Apply(r, c);
            }
        }

        private GameObject CreateChaserPrimitive(Vector3 pos) => SimpleFigureVisuals.CreateChaserEnemy(pos);

        private GameObject CreateShooterPrimitive(Vector3 pos) => SimpleFigureVisuals.CreateShooterEnemy(pos);

        private GameObject CreateFlankerPrimitive(Vector3 pos) => SimpleFigureVisuals.CreateFlankerEnemy(pos);

        private void OnEnemyDied(EnemyBase enemy)
        {
            if (enemy is EnemyFinalBoss)
            {
                FinalBossCleanupMinions();
                _spawnedEnemies.Remove(enemy);
                UpdateGMEnemyCount();
                OnEncounterCleared();
                return;
            }

            if (roomType == RoomType.FinalBoss)
            {
                _spawnedEnemies.Remove(enemy);
                UpdateGMEnemyCount();
                return;
            }

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

            var echoBonus = roomType == RoomType.FinalBoss ? 60 : (roomType == RoomType.Boss ? 25 : 10);
            RunState.Instance?.RecordRoomCleared();
            RunState.Instance?.AddCurrency(echoBonus);

            if (roomType == RoomType.FinalBoss)
            {
                var hudEnd = FindFirstObjectByType<HollowDescent.UI_Debug.MinimalHUD>();
                if (hudEnd != null) hudEnd.QueueFinalEndingSequence();
                else RunState.Instance?.MarkRunComplete();
            }

            var hud = FindFirstObjectByType<HollowDescent.UI_Debug.MinimalHUD>();
            if (hud != null)
                hud.NotifyRoomCleared(echoBonus, roomType == RoomType.Boss, roomType == RoomType.FinalBoss);
        }

        private void SpawnRewardMarker()
        {
            if (_rewardMarker != null) return;
            // Anchor to room controller pivot (floor center). Using trigger world bounds can offset the reward outside the visible room.
            _rewardMarker = new GameObject("RewardMarker");
            _rewardMarker.transform.SetParent(transform, false);
            _rewardMarker.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            _rewardMarker.transform.localRotation = Quaternion.identity;

            var sc = _rewardMarker.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.95f;
            sc.center = Vector3.zero;

            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "CoinVisual";
            var coinCol = coin.GetComponent<Collider>();
            if (coinCol != null) Destroy(coinCol);
            coin.transform.SetParent(_rewardMarker.transform, false);
            coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            coin.transform.localScale = new Vector3(1.45f, 0.06f, 1.45f);
            coin.transform.localPosition = Vector3.zero;
            var coinR = coin.GetComponent<Renderer>();
            if (coinR != null) GrayboxTintUtil.Apply(coinR, new Color(1f, 0.82f, 0.12f));

            var pickup = _rewardMarker.AddComponent<RewardPickup>();
            pickup.SetAmount(roomType == RoomType.FinalBoss ? 60 : (roomType == RoomType.Boss ? 40 : 15));
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
