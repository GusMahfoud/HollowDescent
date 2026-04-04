using HollowDescent.Bootstrap;
using HollowDescent.Gameplay;
using UnityEngine;

namespace HollowDescent.UI_Debug
{
    /// <summary>
    /// Top-left: room name, enemies remaining, legend. Top-right: player lives (health pips).
    /// </summary>
    public class MinimalHUD : MonoBehaviour
    {
        [Header("Style")]
        [SerializeField] private int fontSize = 32;
        [SerializeField] private int padding = 18;
        [SerializeField] private int legendBoxWidth = 346;
        [SerializeField] private int swatchSize = 24;
        [SerializeField] private int rowHeight = 34;

        [Header("Lives (top-right)")]
        [SerializeField] private Color lifeFullColor = new Color(1f, 0.38f, 0.42f, 1f);
        [SerializeField] private Color lifeEmptyColor = new Color(0.42f, 0.42f, 0.45f, 0.42f);
        [SerializeField] private int lifeHeartSize = 38;
        [SerializeField] private int lifeHeartSpacing = 6;

        [Header("Death screen")]
        [SerializeField] private int deathTitleFontSize = 54;
        [SerializeField] private int deathButtonFontSize = 28;

        private GUIStyle _labelStyle;
        private GUIStyle _legendTitleStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _lifeTitleStyle;
        private GUIStyle _lifeHeartStyle;
        private GUIStyle _deathTitleStyle;
        private GUIStyle _deathBodyStyle;
        private GUIStyle _fullScreenButtonStyle;

        private void OnGUI()
        {
            if (_labelStyle == null || _legendTitleStyle == null || _rowStyle == null || _lifeTitleStyle == null || _lifeHeartStyle == null ||
                _deathTitleStyle == null || _deathBodyStyle == null || _fullScreenButtonStyle == null)
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
                _lifeTitleStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(18, fontSize - 4),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = Color.white }
                };
                _lifeHeartStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = lifeHeartSize,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0)
                };
                _deathTitleStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = deathTitleFontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.82f, 0.82f) }
                };
                _deathBodyStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(16, deathTitleFontSize - 20),
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = new Color(0.92f, 0.92f, 0.95f) }
                };
                _fullScreenButtonStyle = new GUIStyle(GUI.skin != null ? GUI.skin.button : baseStyle)
                {
                    font = font,
                    fontSize = deathButtonFontSize,
                    alignment = TextAnchor.MiddleCenter,
                    fixedHeight = 52,
                    margin = new RectOffset(12, 12, 16, 16)
                };
            }

            if (_labelStyle == null || _legendTitleStyle == null || _rowStyle == null || _lifeTitleStyle == null || _lifeHeartStyle == null ||
                _deathTitleStyle == null || _deathBodyStyle == null || _fullScreenButtonStyle == null) return;

            var y = padding;

            var gm = GameManager.Instance;
            var room = gm != null ? gm.CurrentRoomName : "—";
            var enemies = gm != null ? gm.EnemiesRemainingInRoom : 0;
            GUI.Label(new Rect(padding, y, 568, 94), $"Room: {room}\nEnemies: {enemies}", _labelStyle);
            y += 98;

            DrawPlayerLivesTopRight();

            DrawLegend(padding, y);

            DrawDeathScreenIfNeeded();
        }

        private void DrawDeathScreenIfNeeded()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.DeathScreenOpen) return;

            GUI.depth = -128;
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.84f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            const float boxW = 560f;
            const float boxH = 320f;
            GUILayout.BeginArea(new Rect((Screen.width - boxW) * 0.5f, (Screen.height - boxH) * 0.5f, boxW, boxH));
            GUILayout.Label("You died", _deathTitleStyle);
            GUILayout.Label("Your progress on this run is lost. Restart from Level 1 to try again.", _deathBodyStyle);
            GUILayout.Space(12f);
            if (GUILayout.Button("Restart from Level 1", _fullScreenButtonStyle) && gm != null)
                gm.RestartFromLevelOne();
            GUILayout.EndArea();
        }

        private void DrawPlayerLivesTopRight()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var health = playerGo != null ? playerGo.GetComponent<PlayerHealth>() : null;
            var max = health != null ? health.MaxHealth : 5;
            var cur = health != null ? health.CurrentHealth : 0;

            var title = "Lives";
            var subtitle = $"{cur} / {max}";
            var titleW = 220;
            var xRight = Screen.width - padding;
            var titleX = xRight - titleW;
            GUI.Label(new Rect(titleX, padding, titleW, 32), title, _lifeTitleStyle);
            GUI.Label(new Rect(titleX, padding + 28, titleW, 28), subtitle, _lifeTitleStyle);

            var rowY = padding + 58;
            var totalW = max * (lifeHeartSize + lifeHeartSpacing) - lifeHeartSpacing;
            var heartStartX = xRight - totalW;
            for (var i = 0; i < max; i++)
            {
                GUI.contentColor = i < cur ? lifeFullColor : lifeEmptyColor;
                var hx = heartStartX + i * (lifeHeartSize + lifeHeartSpacing);
                GUI.Label(new Rect(hx, rowY, lifeHeartSize + 4, lifeHeartSize + 8), "\u2665", _lifeHeartStyle);
            }
            GUI.contentColor = Color.white;
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
