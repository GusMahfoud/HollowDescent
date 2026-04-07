using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using HollowDescent.Bootstrap;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Singleton shop: holds inventory, handles purchases, renders IMGUI overlay.
    /// </summary>
    public class ShopSystem : MonoBehaviour
    {
        public static ShopSystem Instance { get; private set; }

        [Header("Style")]
        [SerializeField] private int panelWidth = 500;
        [SerializeField] private int panelHeight = 460;
        [SerializeField] private int titleFontSize = 28;
        [SerializeField] private int bodyFontSize = 18;

        public bool IsOpen { get; private set; }
        public bool InShopRoom { get; private set; }

        private struct ShopItem
        {
            public string Name;
            public string Description;
            public int Cost;
            public BuffType Buff;
            public float BuffValue;
            public bool Purchased;
        }

        private List<ShopItem> _items;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _currencyStyle;
        private Texture2D _panelBg;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitInventory();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void InitInventory()
        {
            _items = new List<ShopItem>
            {
                new ShopItem { Name = "Swift Boots", Description = "Move faster", Cost = 20, Buff = BuffType.MoveSpeed, BuffValue = 5f },
                new ShopItem { Name = "Rapid Fire", Description = "Shoot faster", Cost = 25, Buff = BuffType.FireRate, BuffValue = 0.7f },
                new ShopItem { Name = "Vitality Shard", Description = "+2 max HP", Cost = 30, Buff = BuffType.MaxHealth, BuffValue = 2f },
                new ShopItem { Name = "Sharp Rounds", Description = "Hit harder", Cost = 35, Buff = BuffType.Damage, BuffValue = 1.5f },
            };
        }

        public void EnterShopRoom()
        {
            InShopRoom = true;
            IsOpen = true;
        }

        public void ExitShopRoom()
        {
            InShopRoom = false;
            IsOpen = false;
        }

        public void OpenShop() => IsOpen = true;

        public void CloseShop() => IsOpen = false;

        public void ResetShop()
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                item.Purchased = false;
                _items[i] = item;
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                if (IsOpen)
                    CloseShop();
                else if (InShopRoom)
                    OpenShop();
            }
        }

        private GUIStyle _reopenButtonStyle;

        private void OnGUI()
        {
            EnsureStyles();

            // Show reopen button when in shop room but overlay is closed
            if (!IsOpen && InShopRoom)
            {
                if (_reopenButtonStyle == null)
                {
                    _reopenButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 20,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                var btnW = 220f;
                var btnH = 44f;
                var btnX = (Screen.width - btnW) * 0.5f;
                var btnY = Screen.height - btnH - 20f;
                if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "Open Shop [Esc]", _reopenButtonStyle))
                    OpenShop();
                return;
            }

            if (!IsOpen) return;

            GUI.depth = -200;

            // Full-screen dim
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            // Solid panel background
            var x = (Screen.width - panelWidth) * 0.5f;
            var y = (Screen.height - panelHeight) * 0.5f;
            var panelRect = new Rect(x, y, panelWidth, panelHeight);
            GUI.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = prevColor;

            // Border
            GUI.color = new Color(1f, 0.85f, 0.4f, 0.5f);
            GUI.DrawTexture(new Rect(x, y, panelWidth, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + panelHeight - 2, panelWidth, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y, 2, panelHeight), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + panelWidth - 2, y, 2, panelHeight), Texture2D.whiteTexture);
            GUI.color = prevColor;

            var inset = 20;
            GUILayout.BeginArea(new Rect(x + inset, y + inset, panelWidth - inset * 2, panelHeight - inset * 2));

            GUILayout.Label("The Merchant's Cache", _titleStyle);
            GUILayout.Space(4);

            var currency = RunState.Instance != null ? RunState.Instance.Currency : 0;
            GUILayout.Label($"Echoes: {currency}", _currencyStyle);
            GUILayout.Space(8);

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                GUILayout.BeginHorizontal(GUILayout.Height(40));

                var label = item.Purchased
                    ? $"<color=#888888>{item.Name} — {item.Description} (OWNED)</color>"
                    : $"{item.Name} — {item.Description}  [{item.Cost} Echoes]";
                GUILayout.Label(label, _bodyStyle, GUILayout.Width(panelWidth - 150), GUILayout.Height(34));

                GUI.enabled = !item.Purchased && currency >= item.Cost;
                if (GUILayout.Button("Buy", _buttonStyle, GUILayout.Width(80), GUILayout.Height(34)))
                    TryBuy(i);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
                GUILayout.Space(6);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Continue", _buttonStyle, GUILayout.Height(36)))
                CloseShop();

            GUILayout.EndArea();
        }

        private void TryBuy(int index)
        {
            var item = _items[index];
            if (item.Purchased) return;
            if (RunState.Instance == null || !RunState.Instance.SpendCurrency(item.Cost)) return;

            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var mods = playerGo != null ? playerGo.GetComponent<PlayerStatModifiers>() : null;
            if (mods == null) return;

            mods.ApplyBuff(item.Buff, item.BuffValue, item.Name);
            item.Purchased = true;
            _items[index] = item;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = titleFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.4f) }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = bodyFontSize,
                richText = true,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = Mathf.Max(14, bodyFontSize - 2),
                alignment = TextAnchor.MiddleCenter
            };
            _currencyStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = Mathf.Max(14, bodyFontSize - 1),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.9f, 0.3f) }
            };
        }
    }
}
