using UnityEngine;
using HollowDescent.Bootstrap;

namespace HollowDescent.UI_Debug
{
    /// <summary>
    /// Minimal OnGUI: current room name, enemies remaining.
    /// </summary>
    public class MinimalHUD : MonoBehaviour
    {
        [Header("Style")]
        [SerializeField] private int fontSize = 18;
        [SerializeField] private int padding = 12;

        private GUIStyle _labelStyle;

        private void OnEnable()
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = Color.white },
                padding = new RectOffset(padding, padding, padding, padding)
            };
        }

        private void OnGUI()
        {
            if (_labelStyle == null) return;
            var gm = GameManager.Instance;
            var room = gm != null ? gm.CurrentRoomName : "—";
            var enemies = gm != null ? gm.EnemiesRemainingInRoom : 0;
            GUI.Label(new Rect(padding, padding, 400, 60), $"Room: {room}\nEnemies: {enemies}", _labelStyle);
        }
    }
}
