using UnityEngine;
using HollowDescent.Gameplay;

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Opens/closes the shop UI when the player enters/exits the Shop room.
    /// </summary>
    public class ShopTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !other.CompareTag("Player")) return;
            Debug.Log($"[ShopTrigger] Player entered shop. ShopSystem.Instance={(ShopSystem.Instance != null ? "exists" : "NULL")}");
            if (ShopSystem.Instance != null)
                ShopSystem.Instance.OpenShop();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other != null && other.CompareTag("Player"))
                ShopSystem.Instance?.CloseShop();
        }
    }
}
