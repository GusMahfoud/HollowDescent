using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Simple NPC reaction: investigate near player, then retreat.
    /// Uses NavMeshAgent when possible.
    /// </summary>
    public class NPCNavReact : MonoBehaviour
    {
        [SerializeField] private float investigateDistance = 1.5f;
        [SerializeField] private float investigatePauseSeconds = 1.5f;
        [SerializeField] private float retreatDistance = 4f;

        private NavMeshAgent _agent;
        private Vector3 _homePosition;
        private Coroutine _reactionRoutine;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _homePosition = transform.position;
        }

        public void ReactToPlayer(Transform player)
        {
            if (player == null) return;
            if (_reactionRoutine != null) StopCoroutine(_reactionRoutine);
            _reactionRoutine = StartCoroutine(ReactRoutine(player));
        }

        private IEnumerator ReactRoutine(Transform player)
        {
            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
                transform.forward = toPlayer.normalized;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                var investigatePoint = player.position;
                if (toPlayer.sqrMagnitude > 0.01f)
                    investigatePoint -= toPlayer.normalized * investigateDistance;

                if (TrySampleNavPosition(investigatePoint, out var navInvestigate))
                    _agent.SetDestination(navInvestigate);

                yield return new WaitForSeconds(investigatePauseSeconds);

                var retreatPoint = _homePosition;
                if (Vector3.Distance(transform.position, _homePosition) < 0.5f && toPlayer.sqrMagnitude > 0.01f)
                    retreatPoint = transform.position - toPlayer.normalized * retreatDistance;

                if (TrySampleNavPosition(retreatPoint, out var navRetreat))
                    _agent.SetDestination(navRetreat);
            }
            else
            {
                yield return new WaitForSeconds(investigatePauseSeconds);
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    var start = transform.position;
                    var end = start - toPlayer.normalized * 1.2f;
                    var elapsed = 0f;
                    const float duration = 0.35f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        var t = Mathf.Clamp01(elapsed / duration);
                        transform.position = Vector3.Lerp(start, end, t);
                        yield return null;
                    }
                }
            }

            _reactionRoutine = null;
        }

        private static bool TrySampleNavPosition(Vector3 target, out Vector3 sampled)
        {
            if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas))
            {
                sampled = hit.position;
                return true;
            }
            sampled = target;
            return false;
        }
    }
}
