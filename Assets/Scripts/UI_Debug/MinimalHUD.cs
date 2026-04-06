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
        [SerializeField] private int fontSize = 20;
        [SerializeField] private int padding = 10;
        [SerializeField] private int legendBoxWidth = 300;
        [SerializeField] private int swatchSize = 16;
        [SerializeField] private int rowHeight = 24;
        [SerializeField] private string firstRoomName = "Start (Safe)";

        [Header("Lives (top-right)")]
        [SerializeField] private Color lifeFullColor = new Color(1f, 0.38f, 0.42f, 1f);
        [SerializeField] private Color lifeEmptyColor = new Color(0.42f, 0.42f, 0.45f, 0.42f);
        [SerializeField] private int lifeHeartSize = 24;
        [SerializeField] private int lifeHeartSpacing = 4;

        [Header("Death screen")]
        [SerializeField] private int deathTitleFontSize = 54;
        [SerializeField] private int deathButtonFontSize = 28;

        private GUIStyle _labelStyle;
        private GUIStyle _legendTitleStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _legendToggleStyle;
        private GUIStyle _lifeTitleStyle;
        private GUIStyle _lifeHeartStyle;
        private GUIStyle _deathTitleStyle;
        private GUIStyle _deathBodyStyle;
        private GUIStyle _fullScreenButtonStyle;

        private bool _hasLeftFirstRoom;
        private bool _legendExpandedAfterFirstRoom;

        private void OnGUI()
        {
            if (_labelStyle == null || _legendTitleStyle == null || _rowStyle == null || _legendToggleStyle == null || _lifeTitleStyle == null || _lifeHeartStyle == null ||
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
                    richText = true,
                    alignment = TextAnchor.UpperLeft,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(0, 0, 0, 0)
                };
                _legendTitleStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(14, fontSize - 2),
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
                _legendToggleStyle = new GUIStyle(GUI.skin != null ? GUI.skin.button : baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(14, fontSize - 8),
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 8, 6, 6)
                };
                _lifeTitleStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(14, fontSize - 3),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft,
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

            if (_labelStyle == null || _legendTitleStyle == null || _rowStyle == null || _legendToggleStyle == null || _lifeTitleStyle == null || _lifeHeartStyle == null ||
                _deathTitleStyle == null || _deathBodyStyle == null || _fullScreenButtonStyle == null) return;

            var gm = GameManager.Instance;
            var room = gm != null ? gm.CurrentRoomName : "\u2014";
            var enemies = gm != null ? gm.EnemiesRemainingInRoom : 0;
            UpdateLegendRoomState(room);
            var legendVisible = !_hasLeftFirstRoom || _legendExpandedAfterFirstRoom;
            var hideRoomStatus = legendVisible;
            if (!hideRoomStatus)
                DrawRoomStatusTopLeft(room, enemies);
            DrawPlayerLivesTopRight();
            DrawLegendSectionBottomLeft();
            DrawDeathScreenIfNeeded();
        }

        private void UpdateLegendRoomState(string roomName)
        {
            if (_hasLeftFirstRoom) return;
            if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(firstRoomName)) return;
            if (!string.Equals(roomName.Trim(), firstRoomName.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                _hasLeftFirstRoom = true;
                _legendExpandedAfterFirstRoom = false;
            }
        }

        private void DrawRoomStatusTopLeft(string room, int enemies)
        {
            var panelRect = new Rect(padding, padding, 300, 74);
            var lineHeight = Mathf.Max(20, fontSize + 2);
            var x = panelRect.x + 8;
            GUI.Label(new Rect(x, panelRect.y + 4, panelRect.width - 16, lineHeight), $"<b>Room</b>  {room}", _labelStyle);
            GUI.Label(new Rect(x, panelRect.y + 4 + lineHeight, panelRect.width - 16, lineHeight), $"<b>Enemies</b>  {enemies}", _labelStyle);
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
            GUILayout.Label("Run ended", _deathTitleStyle);
            GUILayout.Label("No lives remaining. Restart from Level 1 to begin a fresh run.", _deathBodyStyle);
            GUILayout.Space(12f);
            if (GUILayout.Button("Restart from Level 1", _fullScreenButtonStyle) && gm != null)
                gm.RestartFromLevelOne();
            GUILayout.EndArea();
        }

        private void DrawPlayerLivesTopRight()
        {
            var gm = GameManager.Instance;
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var health = playerGo != null ? playerGo.GetComponent<PlayerHealth>() : null;
            var max = health != null ? health.MaxHealth : 5;
            var cur = health != null ? health.CurrentHealth : 0;
            var livesMax = gm != null ? gm.TotalLives : 3;
            var livesCur = gm != null ? gm.RemainingLives : livesMax;

            var panelW = 210;
            var panelX = Screen.width - padding - panelW;

            var title = $"Health {cur}/{max}";
            GUI.Label(new Rect(panelX + 8, padding + 6, panelW - 16, 24), title, _lifeTitleStyle);
            GUI.Label(new Rect(panelX + 8, padding + 22, panelW - 16, 20), $"Lives {livesCur}/{livesMax}", _rowStyle);

            var rowY = padding + 42;
            var totalW = max * (lifeHeartSize + lifeHeartSpacing) - lifeHeartSpacing;
            var heartStartX = panelX + panelW - 10 - totalW;
            for (var i = 0; i < max; i++)
            {
                GUI.contentColor = i < cur ? lifeFullColor : lifeEmptyColor;
                var hx = heartStartX + i * (lifeHeartSize + lifeHeartSpacing);
                GUI.Label(new Rect(hx, rowY, lifeHeartSize + 2, lifeHeartSize + 4), "\u2665", _lifeHeartStyle);
            }
            GUI.contentColor = Color.white;
        }

        private void DrawLegendSectionBottomLeft()
        {
            var legendX = padding;
            var legendHeight = 11 * rowHeight + 59;
            var centeredLegendY = Mathf.Max(padding, Mathf.RoundToInt((Screen.height - legendHeight) * 0.5f));

            if (!_hasLeftFirstRoom)
            {
                DrawLegend(legendX, centeredLegendY);
                return;
            }

            var buttonX = legendX;
            var buttonY = Screen.height - Mathf.Max(2, padding - 8) - 32;
            var arrow = _legendExpandedAfterFirstRoom ? "v" : ">";
            var buttonRect = new Rect(buttonX, buttonY, legendBoxWidth, 32);
            if (GUI.Button(buttonRect, arrow + " Color legend", _legendToggleStyle))
                _legendExpandedAfterFirstRoom = !_legendExpandedAfterFirstRoom;

            if (_legendExpandedAfterFirstRoom)
            {
                DrawLegend(buttonX, centeredLegendY);
            }
        }

        private void DrawLegend(int x, int y)
        {
            if (_legendTitleStyle == null || _rowStyle == null) return;
            var boxHeight = 11 * rowHeight + 59;

            var inner = padding;
            GUI.Label(new Rect(x + inner, y + inner - 1, legendBoxWidth - inner * 2, 26), "Color legend", _legendTitleStyle);
            y += 34;

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
