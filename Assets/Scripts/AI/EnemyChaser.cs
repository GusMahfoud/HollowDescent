using UnityEngine;

namespace HollowDescent.AI
{
    /// <summary>
    /// Moves toward player; contact damage.
    /// </summary>
    public class EnemyChaser : EnemyBase
    {
        [SerializeField] private float moveSpeed = 4f;

        private Transform _player;

        public void SetPlayer(Transform p) => _player = p;

        private void Update()
        {
            if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (_player == null) return;
            var dir = (_player.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                transform.position += dir * (moveSpeed * Time.deltaTime);
                transform.forward = dir;
            }
        }

        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.CompareTag("Player"))
                DealContactDamage(other.collider);
        }
    }
}
