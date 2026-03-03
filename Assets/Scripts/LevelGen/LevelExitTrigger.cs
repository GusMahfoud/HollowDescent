using UnityEngine;
using HollowDescent.Bootstrap;

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Place in the Level Exit room doorway; when player enters, triggers level change to Level 2.
    /// </summary>
    public class LevelExitTrigger : MonoBehaviour
    {
        [SerializeField] private int targetLevel = 2;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (LevelManager.Instance != null)
                LevelManager.Instance.LoadLevel(targetLevel);
        }
    }
}
