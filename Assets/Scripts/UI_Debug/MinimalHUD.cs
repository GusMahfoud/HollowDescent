using System.Collections.Generic;
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
        private GUIStyle _currencyStyle;
        private GUIStyle _buffStyle;
        private GUIStyle _menuButtonStyle;

        private bool _hasLeftFirstRoom;
        private bool _legendExpandedAfterFirstRoom;

        // Room flavor subtitles
        private GUIStyle _flavorStyle;
        private string _currentFlavor;
        private float _flavorShowTime;
        private float _flavorDuration = 4f;
        private string _lastFlavorRoom;

        private static readonly Dictionary<string, string> RoomFlavor = new Dictionary<string, string>
        {
            { "Start (Safe)", "A moment of calm before the descent." },
            { "Combat 1", "Something stirs in the darkness." },
            { "Branch A", "The Hollow splits. Choose wisely." },
            { "Branch B", "An alternate path through forgotten halls." },
            { "Reconverge", "All paths lead here." },
            { "Shop (Safe)", "A merchant who shouldn't exist." },
            { "To Level 2", "The way down beckons." },
            { "L2 Start", "Deeper. The walls shift when you're not looking." },
            { "L2 Combat 1", "The trials grow restless." },
            { "L2 Combat 2", "Containment failed long ago." },
            { "L2 Merchant (Safe)", "Echoes trade hands. Stock meant for those who go deeper." },
            { "L2 Boss", "The gauntlet before the last door." },
            { "To Level 3", "One more descent. The heart of the Hollow." },
            { "Level 3 Start (Safe)", "The architecture ends. Whatever built this is waiting." },
            { "L3 Combat 1", "The floor remembers every footstep." },
            { "The Architect", "It was never meant to be contained." },
        };

        // Room Cleared flash
        private GUIStyle _clearedStyle;
        private string _clearedText;
        private float _clearedShowTime;
        private float _clearedDuration = 2.5f;

        // Final boss → ending lines (before victory / run-complete screen)
        private bool _showEndingDialogue;
        private string[] _endingLines;
        private int _endingLineIndex;

        private static readonly string[] DefaultFinalEndingLines =
        {
            "The Architect unravels into violet dust.",
            "What you defeated was only a caretaker—something older still watches from the seams.",
            "The Hollow loosens its grip. Light finds you again, thin but real.",
            "For now, you are free."
        };

        private void OnGUI()
        {
            if (FindFirstObjectByType<StartMenuOverlay>() != null) return;

            if (_labelStyle == null || _legendTitleStyle == null || _rowStyle == null || _legendToggleStyle == null || _lifeTitleStyle == null || _lifeHeartStyle == null ||
                _deathTitleStyle == null || _deathBodyStyle == null || _fullScreenButtonStyle == null || _currencyStyle == null || _buffStyle == null)
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
                _currencyStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(14, fontSize - 2),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft,
                    normal = { textColor = new Color(1f, 0.9f, 0.3f) }
                };
                _buffStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(12, fontSize - 4),
                    alignment = TextAnchor.UpperLeft,
                    normal = { textColor = new Color(0.7f, 0.9f, 1f) }
                };
                _flavorStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(16, fontSize - 2),
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.UpperLeft,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.7f) }
                };
                _menuButtonStyle = new GUIStyle(GUI.skin != null ? GUI.skin.button : baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(12, fontSize - 6),
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 8, 4, 4)
                };
                _clearedStyle = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.Max(22, fontSize + 4),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.9f, 0.3f) }
                };
            }

            if (_labelStyle == null || _legendTitleStyle == null || _rowStyle == null || _legendToggleStyle == null || _lifeTitleStyle == null || _lifeHeartStyle == null ||
                _deathTitleStyle == null || _deathBodyStyle == null || _fullScreenButtonStyle == null || _currencyStyle == null || _buffStyle == null) return;

            if (_showEndingDialogue)
            {
                DrawFinalEndingDialogue();
                return;
            }

            var gm = GameManager.Instance;
            var room = gm != null ? gm.CurrentRoomName : "\u2014";
            var enemies = gm != null ? gm.EnemiesRemainingInRoom : 0;
            UpdateLegendRoomState(room);
            UpdateFlavorText(room);
            var legendVisible = !_hasLeftFirstRoom || _legendExpandedAfterFirstRoom;
            var hideRoomStatus = legendVisible;
            if (!hideRoomStatus)
            {
                DrawRoomStatusTopLeft(room, enemies);
                DrawCurrencyDisplay();
                DrawFlavorSubtitle();
            }
            DrawRoomClearedFlash();
            DrawPlayerLivesTopRight();
            DrawLegendSectionBottomLeft();
            DrawMainMenuButton();
            DrawDeathScreenIfNeeded();
            DrawVictoryScreenIfNeeded();
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
            var panelRect = new Rect(padding, padding, 400, 74);
            var lineHeight = Mathf.Max(20, fontSize + 2);
            var x = panelRect.x + 8;
            GUI.Label(new Rect(x, panelRect.y + 4, panelRect.width - 16, lineHeight), $"<b>Room</b>  {room}", _labelStyle);
            var gm = GameManager.Instance;
            var comp = gm != null ? gm.EnemyComposition : "";
            var enemyText = enemies > 0 && !string.IsNullOrEmpty(comp)
                ? $"<b>Enemies</b>  {enemies} ({comp})"
                : $"<b>Enemies</b>  {enemies}";
            GUI.Label(new Rect(x, panelRect.y + 4 + lineHeight, panelRect.width - 16, lineHeight), enemyText, _labelStyle);
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
            const float boxH = 520f;
            GUILayout.BeginArea(new Rect((Screen.width - boxW) * 0.5f, (Screen.height - boxH) * 0.5f, boxW, boxH));
            GUILayout.Label("Run ended", _deathTitleStyle);
            GUILayout.Label("The Hollow claims another. Your echoes fade into silence.", _deathBodyStyle);
            GUILayout.Space(8f);
            DrawRunStats();
            GUILayout.Space(12f);
            if (GUILayout.Button("Restart from Level 1", _fullScreenButtonStyle))
            {
                if (gm != null) gm.RestartFromLevelOne();
            }
            GUILayout.EndArea();
        }

        private void DrawRunStats()
        {
            var rs = RunState.Instance;
            if (rs == null || _deathBodyStyle == null) return;
            GUILayout.Label($"Time: {rs.GetRunTimeFormatted()}", _deathBodyStyle);
            GUILayout.Label($"Enemies Defeated: {rs.EnemiesKilled}", _deathBodyStyle);
            GUILayout.Label($"Rooms Cleared: {rs.RoomsCleared}", _deathBodyStyle);
            GUILayout.Label($"Damage Taken: {rs.DamageTaken}", _deathBodyStyle);
            GUILayout.Label($"Echoes Earned: {rs.Currency}", _deathBodyStyle);
        }

        private void DrawVictoryScreenIfNeeded()
        {
            var rs = RunState.Instance;
            if (rs == null || !rs.RunComplete) return;
            var gm = GameManager.Instance;
            if (gm != null && gm.DeathScreenOpen) return;

            GUI.depth = -128;
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.84f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            const float boxW = 560f;
            const float boxH = 520f;
            GUILayout.BeginArea(new Rect((Screen.width - boxW) * 0.5f, (Screen.height - boxH) * 0.5f, boxW, boxH));
            GUILayout.Label("Run Complete!", _deathTitleStyle);
            GUILayout.Label("The final trial falls silent. The architects would be proud... or terrified.", _deathBodyStyle);
            GUILayout.Space(8f);
            DrawRunStats();
            GUILayout.Space(12f);
            if (GUILayout.Button("Play Again", _fullScreenButtonStyle))
            {
                if (gm != null) gm.RestartFromLevelOne();
                rs.ResetForNewRun();
            }
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

            DrawActiveBuffs(panelX + 8, rowY + lifeHeartSize + 8);
        }

        private void DrawCurrencyDisplay()
        {
            var rs = RunState.Instance;
            if (rs == null || _currencyStyle == null) return;
            var y = padding + 80;
            GUI.Label(new Rect(padding + 8, y, 200, 24), $"Echoes: {rs.Currency}", _currencyStyle);
        }

        private void UpdateFlavorText(string roomName)
        {
            if (string.IsNullOrEmpty(roomName) || roomName == _lastFlavorRoom) return;
            _lastFlavorRoom = roomName;
            if (RoomFlavor.TryGetValue(roomName, out var flavor))
            {
                _currentFlavor = flavor;
                _flavorShowTime = Time.unscaledTime;
            }
        }

        private void DrawFlavorSubtitle()
        {
            if (_flavorStyle == null || string.IsNullOrEmpty(_currentFlavor)) return;
            var elapsed = Time.unscaledTime - _flavorShowTime;
            if (elapsed > _flavorDuration) return;
            // Fade in for 0.5s, hold, fade out last 1s
            float alpha;
            if (elapsed < 0.5f)
                alpha = elapsed / 0.5f;
            else if (elapsed > _flavorDuration - 1f)
                alpha = (_flavorDuration - elapsed) / 1f;
            else
                alpha = 1f;
            var prev = GUI.contentColor;
            GUI.contentColor = new Color(0.8f, 0.8f, 0.7f, alpha);
            var y = padding + 106;
            GUI.Label(new Rect(padding + 8, y, 400, 24), _currentFlavor, _flavorStyle);
            GUI.contentColor = prev;
        }

        public void NotifyRoomCleared(int echoesAwarded, bool isBoss)
        {
            NotifyRoomCleared(echoesAwarded, isBoss, false);
        }

        public void NotifyRoomCleared(int echoesAwarded, bool isBoss, bool isFinalBoss)
        {
            _clearedText = isFinalBoss
                ? $"The Architect falls! +{echoesAwarded} Echoes"
                : isBoss
                    ? $"Boss Defeated! +{echoesAwarded} Echoes"
                    : $"Room Cleared! +{echoesAwarded} Echoes";
            _clearedShowTime = Time.unscaledTime;
        }

        /// <summary>
        /// Called when the Level 3 final boss is defeated. Shows story beats, then <see cref="RunState.MarkRunComplete"/>.
        /// </summary>
        public void QueueFinalEndingSequence()
        {
            RunState.Instance?.FreezeRunTimer();
            _endingLines = DefaultFinalEndingLines;
            _endingLineIndex = 0;
            _showEndingDialogue = true;
        }

        private void DrawFinalEndingDialogue()
        {
            if (_deathTitleStyle == null || _deathBodyStyle == null || _fullScreenButtonStyle == null) return;

            GUI.depth = -129;
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            const float boxW = 560f;
            GUILayout.BeginArea(new Rect((Screen.width - boxW) * 0.5f, Screen.height * 0.18f, boxW, 460f));
            GUILayout.Label("Epilogue", _deathTitleStyle);
            if (_endingLines != null && _endingLineIndex >= 0 && _endingLineIndex < _endingLines.Length)
                GUILayout.Label(_endingLines[_endingLineIndex], _deathBodyStyle);
            GUILayout.Space(20f);
            if (GUILayout.Button("Continue", _fullScreenButtonStyle))
            {
                _endingLineIndex++;
                if (_endingLines == null || _endingLineIndex >= _endingLines.Length)
                {
                    _showEndingDialogue = false;
                    RunState.Instance?.MarkRunComplete();
                }
            }
            GUILayout.EndArea();
        }

        private void DrawRoomClearedFlash()
        {
            if (_clearedStyle == null || string.IsNullOrEmpty(_clearedText)) return;
            var elapsed = Time.unscaledTime - _clearedShowTime;
            if (elapsed > _clearedDuration) return;
            float alpha;
            if (elapsed < 0.3f)
                alpha = elapsed / 0.3f;
            else if (elapsed > _clearedDuration - 0.8f)
                alpha = (_clearedDuration - elapsed) / 0.8f;
            else
                alpha = 1f;
            var prev = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.9f, 0.3f, alpha);
            var w = 400f;
            var h = 40f;
            GUI.Label(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.3f, w, h), _clearedText, _clearedStyle);
            GUI.contentColor = prev;
        }

        private void DrawActiveBuffs(int startX, int startY)
        {
            if (_buffStyle == null) return;
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var mods = playerGo != null ? playerGo.GetComponent<PlayerStatModifiers>() : null;
            if (mods == null) return;
            var buffs = mods.GetActiveBuffSummary();
            if (buffs == null || buffs.Count == 0) return;
            var y = startY;
            foreach (var name in buffs)
            {
                GUI.Label(new Rect(startX, y, 180, 20), name, _buffStyle);
                y += 18;
            }
        }

        private void DrawMainMenuButton()
        {
            if (_menuButtonStyle == null) return;
            var gm = GameManager.Instance;
            // Don't show during death/victory screens (they have their own buttons)
            if (gm != null && gm.DeathScreenOpen) return;
            var rs = RunState.Instance;
            if (rs != null && rs.RunComplete) return;

            var btnW = 80f;
            var btnH = 28f;
            var btnX = Screen.width - padding - btnW;
            var btnY = Screen.height - padding - btnH;
            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "Menu", _menuButtonStyle))
            {
                if (gm != null) gm.ReturnToMainMenu();
            }
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
