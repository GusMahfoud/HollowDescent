using UnityEngine;
using HollowDescent.Bootstrap;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Collectible pickup spawned when a combat room is cleared.
    /// Bobs up and down; awards currency on player contact.
    /// </summary>
    public class RewardPickup : MonoBehaviour
    {
        [SerializeField] private int currencyAmount = 15;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.3f;

        private float _baseY;

        public void SetAmount(int amount)
        {
            currencyAmount = Mathf.Max(1, amount);
        }

        private void Start()
        {
            _baseY = transform.position.y;
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            var pos = transform.position;
            pos.y = _baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = pos;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !other.CompareTag("Player")) return;
            RunState.Instance?.AddCurrency(currencyAmount);
            Destroy(gameObject);
        }
    }
}
