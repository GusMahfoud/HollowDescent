using System;
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
        [SerializeField] private int panelWidth = 560;
        [SerializeField] private int panelHeight = 520;
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
        private bool _useDeepMerchantTitle;
        private string _activeCatalogKey = "";
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _currencyStyle;
        private GUIStyle _descStyle;
        private Texture2D _panelBg;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitDefaultInventory();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void InitDefaultInventory()
        {
            _items = new List<ShopItem>
            {
                new ShopItem { Name = "Swift Boots", Description = "Worn by the Hollow's couriers (+5 move speed)", Cost = 20, Buff = BuffType.MoveSpeed, BuffValue = 5f },
                new ShopItem { Name = "Rapid Fire", Description = "Etched with acceleration glyphs (0.7x fire delay)", Cost = 25, Buff = BuffType.FireRate, BuffValue = 0.7f },
                new ShopItem { Name = "Vitality Shard", Description = "A fragment of living stone (+2 max HP)", Cost = 30, Buff = BuffType.MaxHealth, BuffValue = 2f },
                new ShopItem { Name = "Sharp Rounds", Description = "Tipped with crystallized venom (1.5x damage)", Cost = 35, Buff = BuffType.Damage, BuffValue = 1.5f },
            };
        }

        /// <summary>Exclusive stock for the Level 2 merchant (before descending to Level 3).</summary>
        private void InitDeepMerchantInventory()
        {
            _items = new List<ShopItem>
            {
                new ShopItem { Name = "Nullthread Cloak", Description = "Spun from whatever the walls shed at night (+6 move speed)", Cost = 45, Buff = BuffType.MoveSpeed, BuffValue = 6f },
                new ShopItem { Name = "Core Overcharge", Description = "Bypasses the safety seal; your hands hum (0.55x fire delay)", Cost = 55, Buff = BuffType.FireRate, BuffValue = 0.55f },
                new ShopItem { Name = "Heartglass Reliquary", Description = "Three vials of someone else's pulse (+3 max HP)", Cost = 62, Buff = BuffType.MaxHealth, BuffValue = 3f },
                new ShopItem { Name = "Spire Needles", Description = "Carved from the Architect's cast-offs (1.65x damage)", Cost = 70, Buff = BuffType.Damage, BuffValue = 1.65f },
            };
        }

        public void EnterShopRoom()
        {
            InShopRoom = true;
            IsOpen = true;
            var gm = GameManager.Instance;
            var rn = gm != null ? gm.CurrentRoomName : "";
            var deep = !string.IsNullOrEmpty(rn) && rn.IndexOf("Merchant", StringComparison.OrdinalIgnoreCase) >= 0;
            _useDeepMerchantTitle = deep;
            var key = deep ? "deep" : "default";
            if (key != _activeCatalogKey)
            {
                _activeCatalogKey = key;
                if (deep)
                    InitDeepMerchantInventory();
                else
                    InitDefaultInventory();
            }
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
            _useDeepMerchantTitle = false;
            _activeCatalogKey = "";
            InitDefaultInventory();
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

            GUILayout.Label(_useDeepMerchantTitle ? "The Deep Merchant" : "The Merchant's Cache", _titleStyle);
            GUILayout.Space(4);

            var currency = RunState.Instance != null ? RunState.Instance.Currency : 0;
            GUILayout.Label($"Echoes: {currency}", _currencyStyle);
            GUILayout.Space(8);

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];

                // Item name + cost on first line, description on second
                var nameLine = item.Purchased
                    ? $"<color=#888888>{item.Name}  (OWNED)</color>"
                    : $"{item.Name}  <color=#FFE066>[{item.Cost} Echoes]</color>";
                var descLine = item.Purchased
                    ? $"<color=#666666>{item.Description}</color>"
                    : $"<color=#BBBBBB>{item.Description}</color>";

                GUILayout.BeginHorizontal(GUILayout.Height(56));

                GUILayout.BeginVertical(GUILayout.Width(panelWidth - 160));
                GUILayout.Label(nameLine, _bodyStyle, GUILayout.Height(24));
                GUILayout.Label(descLine, _descStyle, GUILayout.Height(20));
                GUILayout.EndVertical();

                GUI.enabled = !item.Purchased && currency >= item.Cost;
                if (GUILayout.Button("Buy", _buttonStyle, GUILayout.Width(80), GUILayout.Height(50)))
                    TryBuy(i);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
                GUILayout.Space(10);
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
            _descStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = Mathf.Max(12, bodyFontSize - 4),
                fontStyle = FontStyle.Italic,
                richText = true,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.73f, 0.73f, 0.73f) }
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
