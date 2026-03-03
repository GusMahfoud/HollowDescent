using UnityEngine;
using HollowDescent.Bootstrap;

namespace HollowDescent.UI_Debug
{
    /// <summary>
    /// Top-left: room name, enemies remaining, and a color legend for in-game objects.
    /// </summary>
    public class MinimalHUD : MonoBehaviour
    {
        [Header("Style")]
        [SerializeField] private int fontSize = 32;
        [SerializeField] private int padding = 18;
        [SerializeField] private int legendBoxWidth = 346;
        [SerializeField] private int swatchSize = 24;
        [SerializeField] private int rowHeight = 34;

        private GUIStyle _labelStyle;
        private GUIStyle _legendTitleStyle;
        private GUIStyle _rowStyle;

        private void OnGUI()
        {
            if (_labelStyle == null || _legendTitleStyle == null || _rowStyle == null)
            {
                GUIStyle baseStyle = null;
                if (GUI.skin != null)
                    baseStyle = GUI.skin.label ?? GUI.skin.box;
                if (baseStyle == null)
                    baseStyle = new GUIStyle();
                var font = baseStyle.font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                _labelStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = fontSize,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(padding, padding, padding, padding)
                };
                _legendTitleStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = fontSize,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                _rowStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(12, fontSize - 4),
                    normal = { textColor = new Color(1f, 1f, 1f, 1f) },
                    padding = new RectOffset(4, 2, 4, 2)
                };
            }

            if (_labelStyle == null || _legendTitleStyle == null || _rowStyle == null) return;

            var y = padding;

            var gm = GameManager.Instance;
            var room = gm != null ? gm.CurrentRoomName : "—";
            var enemies = gm != null ? gm.EnemiesRemainingInRoom : 0;
            GUI.Label(new Rect(padding, y, 568, 94), $"Room: {room}\nEnemies: {enemies}", _labelStyle);
            y += 98;

            DrawLegend(padding, y);
        }

        private void DrawLegend(int x, int y)
        {
            if (_legendTitleStyle == null || _rowStyle == null) return;
            var boxHeight = 11 * rowHeight + 59;
            var rect = new Rect(x, y, legendBoxWidth, boxHeight);
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var inner = padding;
            GUI.Label(new Rect(x + inner, y + inner, legendBoxWidth - inner * 2, 44), "Color legend", _legendTitleStyle);
            y += 51;

            DrawLegendRow(x + inner, y, new Color(0.5f, 0.55f, 0.45f), "Safe room (floor)");
            y += rowHeight;
            DrawLegendRow(x + inner, y, new Color(0.6f, 0.7f, 0.5f), "Safe room marker");
            y += rowHeight;
            DrawLegendRow(x + inner, y, new Color(0.45f, 0.4f, 0.4f), "Combat room (floor)");
            y += rowHeight;
            DrawLegendRow(x + inner, y, new Color(0.55f, 0.45f, 0.35f), "Combat marker");
            y += rowHeight;
            DrawLegendRow(x + inner, y, new Color(0.6f, 0.2f, 0.25f), "Boss arena");
            y += rowHeight;
            DrawLegendRow(x + inner, y, new Color(0.4f, 0.38f, 0.42f), "Occlusion pillar");
            y += rowHeight;
            DrawLegendRow(x + inner, y, new Color(0.5f, 0.48f, 0.52f), "Raised platform");
            y += rowHeight;
            DrawLegendRow(x + inner, y, new Color(1f, 0.9f, 0.2f), "Reward / pickup");
            y += rowHeight;
            DrawLegendRow(x + inner, y, new Color(0.9f, 0.2f, 0.2f), "Chaser enemy");
            y += rowHeight;
            DrawLegendRow(x + inner, y, new Color(0.6f, 0.2f, 0.6f), "Flanker enemy");
        }

        private void DrawLegendRow(int x, int y, Color swatchColor, string text)
        {
            if (_rowStyle == null) return;
            var swatchRect = new Rect(x, y, swatchSize, swatchSize);
            GUI.color = swatchColor;
            GUI.DrawTexture(swatchRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var textRect = new Rect(x + swatchSize + 6, y - 1, legendBoxWidth - swatchSize - 24, rowHeight + 4);
            GUI.contentColor = Color.white;
            GUI.Label(textRect, text ?? "", _rowStyle);
        }
    }
}
