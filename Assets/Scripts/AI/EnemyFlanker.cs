using UnityEngine;

namespace HollowDescent.AI
{
    /// <summary>
    /// Approaches from lateral angle to reduce doorway camping.
    /// </summary>
    public class EnemyFlanker : EnemyBase
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lateralBias = 0.7f;

        private Transform _player;

        public void SetPlayer(Transform p) => _player = p;

        private void Update()
        {
            if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (_player == null) return;
            var toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.01f) return;
            var direct = toPlayer.normalized;
            var right = Vector3.Cross(Vector3.up, direct);
            var flankDir = (direct + right * lateralBias).normalized;
            transform.position += flankDir * (moveSpeed * Time.deltaTime);
            transform.forward = flankDir;
        }

        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.CompareTag("Player"))
                DealContactDamage(other.collider);
        }
    }
}
