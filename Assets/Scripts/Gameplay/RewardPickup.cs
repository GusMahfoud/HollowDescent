using UnityEngine;
using HollowDescent.Bootstrap;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Collectible pickup spawned when a combat room is cleared.
    /// Trigger stays fixed; only the coin mesh bobs so the pickup doesn't flicker in/out of the player trigger.
    /// </summary>
    public class RewardPickup : MonoBehaviour
    {
        [SerializeField] private int currencyAmount = 15;
        [SerializeField] private float bobSpeed = 2.6f;
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float spinSpeedDegrees = 108f;

        private Transform _coinVisual;
        private Vector3 _coinBaseLocal;

        public void SetAmount(int amount)
        {
            currencyAmount = Mathf.Max(1, amount);
        }

        private void Awake()
        {
            _coinVisual = transform.Find("CoinVisual");
            if (_coinVisual != null)
                _coinBaseLocal = _coinVisual.localPosition;
        }

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (_coinVisual == null) return;
            _coinVisual.Rotate(0f, spinSpeedDegrees * Time.deltaTime, 0f, Space.World);
            var lp = _coinBaseLocal;
            lp.y = _coinBaseLocal.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            _coinVisual.localPosition = lp;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !other.CompareTag("Player")) return;
            RunState.Instance?.AddCurrency(currencyAmount);
            Destroy(gameObject);
        }
    }
}
