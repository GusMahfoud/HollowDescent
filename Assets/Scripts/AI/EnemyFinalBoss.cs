using System.Collections;
using UnityEngine;
using HollowDescent.Gameplay;
using HollowDescent.LevelGen;

namespace HollowDescent.AI
{
    /// <summary>
    /// Unique arena boss: high health, slow movement, periodically spawns chaser minions.
    /// Minions are registered with the owning <see cref="RoomController"/> so clearing the room
    /// only completes when the boss dies.
    /// </summary>
    public class EnemyFinalBoss : EnemyChaser
    {
        [Header("Final Boss")]
        [SerializeField] private int bossMaxHealth = 52;
        [SerializeField] private int bossContactDamage = 2;
        [SerializeField] private float minionSpawnIntervalSeconds = 8f;
        [SerializeField] private int maxConcurrentMinions = 6;
        [SerializeField] private float minionMoveSpeed = 3.4f;

        private RoomController _room;
        private Coroutine _spawnRoutine;
        private int _activeMinions;

        /// <summary>Called by <see cref="RoomController"/> after instantiation.</summary>
        public void Initialize(RoomController room)
        {
            _room = room;
            if (_spawnRoutine == null && isActiveAndEnabled)
                _spawnRoutine = StartCoroutine(SpawnMinionsRoutine());
        }

        protected override void Awake()
        {
            base.Awake();
            ApplyRuntimeCombatStats(bossMaxHealth, bossContactDamage);
            ApplyMovementProfile(2.1f);
            transform.localScale = Vector3.one * 2.1f;
            var tint = new Color(0.22f, 0.05f, 0.38f);
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r != null) GrayboxTintUtil.Apply(r, tint);
            }
        }

        private void OnDisable()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }
        }

        private IEnumerator SpawnMinionsRoutine()
        {
            yield return new WaitForSeconds(3f);
            while (enabled && gameObject != null)
            {
                yield return new WaitForSeconds(minionSpawnIntervalSeconds);
                if (!enabled || _room == null) yield break;
                if (_activeMinions >= maxConcurrentMinions) continue;
                TrySpawnOneMinion();
            }
        }

        private void TrySpawnOneMinion()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            var offset = Random.insideUnitSphere * 4f;
            offset.y = 0f;
            var pos = transform.position + offset;
            if (player != null)
            {
                var away = (pos - player.transform.position);
                away.y = 0f;
                if (away.sqrMagnitude < 4f)
                    pos = transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.right) * 5f;
            }

            var go = SimpleFigureVisuals.CreateBossMinion(pos);
            var chaser = go.GetComponent<EnemyChaser>();
            chaser.SetPlayer(player != null ? player.transform : null);
            chaser.ApplyMovementProfile(minionMoveSpeed);
            chaser.ApplyRuntimeCombatStats(2, 1);

            go.transform.SetParent(_room.transform, true);
            _room.RegisterFinalBossMinion(chaser);
            _activeMinions++;
            chaser.OnDeath += _ => { _activeMinions = Mathf.Max(0, _activeMinions - 1); };
        }
    }
}
