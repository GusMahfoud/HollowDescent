using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace HollowDescent.UI_Debug
{
    /// <summary>
    /// Startup menu controller bound to a prefab-built UI hierarchy.
    /// </summary>
    public class StartMenuOverlay : MonoBehaviour
    {
        private const string MenuPrefabResourcePath = "Prefabs/UI/StartMenu";

        [Header("References (assign in prefab)")]
        [SerializeField] private Image backdropOrNull;
        [SerializeField] private Text titleTextOrNull;
        [SerializeField] private TMP_Text titleTmpOrNull;
        [SerializeField] private Text subtitleTextOrNull;
        [SerializeField] private TMP_Text subtitleTmpOrNull;
        [SerializeField] private Button startButtonOrNull;
        [SerializeField] private Button quitButtonOrNull;
        [SerializeField] private Text startButtonTextOrNull;
        [SerializeField] private TMP_Text startButtonTmpTextOrNull;
        [SerializeField] private GameObject controlsPanelOrNull;
        [SerializeField] private Text controlsTextOrNull;
        [SerializeField] private TMP_Text controlsTmpTextOrNull;
        [SerializeField] private Button controlsContinueButtonOrNull;
        [SerializeField] private Text controlsContinueTextOrNull;
        [SerializeField] private TMP_Text controlsContinueTmpTextOrNull;

        [Header("Styling")]
        [SerializeField] private string defaultSubtitle = "Descend if you dare.";
        [SerializeField] private Color titlePulseLow = new Color(0.92f, 0.93f, 0.95f);
        [SerializeField] private Color titlePulseHigh = new Color(1f, 1f, 1f);
        [SerializeField] private float titlePulseSpeed = 1.8f;
        [SerializeField] private bool forceOpaqueBackdrop = true;
        [Header("Controls Popup")]
        [SerializeField] private bool showControlsBeforeStart = true;
        [SerializeField, TextArea(3, 8)] private string controlsDescription =
            "Controls\n\nWASD - Move\nMouse - Aim\nLeft Click - Shoot\nEsc - Shop (in safe/shop rooms)\n\nTip: Clear rooms to open doors and collect rewards.";
        [SerializeField] private string controlsContinueLabel = "Continue";

        private Action _onStart;
        private float _pulseTime;
        private bool _waitingForControlsContinue;

        public static StartMenuOverlay Show(string title, string buttonText, Action onStart)
        {
            var existing = FindAnyOverlay();
            if (existing != null)
            {
                SetOverlayVisible(existing, true);
                existing.Configure(title, buttonText, onStart);
                return existing;
            }

            var prefab = Resources.Load<StartMenuOverlay>(MenuPrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError("[StartMenuOverlay] Missing prefab at Resources/" + MenuPrefabResourcePath + ".");
                return null;
            }

            var instance = Instantiate(prefab);
            SetOverlayVisible(instance, true);
            instance.Configure(title, buttonText, onStart);
            return instance;
        }

        public void AssignReferences(Image backdrop, Text title, Text subtitle, Button startButton, Text startButtonText)
        {
            backdropOrNull = backdrop;
            titleTextOrNull = title;
            subtitleTextOrNull = subtitle;
            startButtonOrNull = startButton;
            startButtonTextOrNull = startButtonText;
        }

        private void Awake()
        {
            AutoWireReferences();
            EnsureEventSystem();
        }

        private void OnEnable()
        {
            if (startButtonOrNull != null)
            {
                startButtonOrNull.onClick.RemoveListener(OnStartClicked);
                startButtonOrNull.onClick.AddListener(OnStartClicked);
            }
            if (quitButtonOrNull != null)
            {
                quitButtonOrNull.onClick.RemoveListener(OnQuitClicked);
                quitButtonOrNull.onClick.AddListener(OnQuitClicked);
            }
            if (controlsContinueButtonOrNull != null)
            {
                controlsContinueButtonOrNull.onClick.RemoveListener(OnControlsContinueClicked);
                controlsContinueButtonOrNull.onClick.AddListener(OnControlsContinueClicked);
            }
        }

        private void OnDisable()
        {
            if (startButtonOrNull != null)
                startButtonOrNull.onClick.RemoveListener(OnStartClicked);
            if (quitButtonOrNull != null)
                quitButtonOrNull.onClick.RemoveListener(OnQuitClicked);
            if (controlsContinueButtonOrNull != null)
                controlsContinueButtonOrNull.onClick.RemoveListener(OnControlsContinueClicked);
        }

        private void Configure(string title, string buttonText, Action onStart)
        {
            _onStart = onStart;

            var resolvedTitle = string.IsNullOrWhiteSpace(title) ? "Hollow Descent" : title;
            var resolvedButton = string.IsNullOrWhiteSpace(buttonText) ? "Start" : buttonText;

            if (titleTextOrNull != null) titleTextOrNull.text = resolvedTitle;
            if (titleTmpOrNull != null) titleTmpOrNull.text = resolvedTitle;
            if (subtitleTextOrNull != null) subtitleTextOrNull.text = defaultSubtitle;
            if (subtitleTmpOrNull != null) subtitleTmpOrNull.text = defaultSubtitle;
            if (startButtonTextOrNull != null) startButtonTextOrNull.text = resolvedButton;
            if (startButtonTmpTextOrNull != null) startButtonTmpTextOrNull.text = resolvedButton;
            if (controlsTextOrNull != null) controlsTextOrNull.text = controlsDescription;
            if (controlsTmpTextOrNull != null) controlsTmpTextOrNull.text = controlsDescription;
            if (controlsContinueTextOrNull != null) controlsContinueTextOrNull.text = controlsContinueLabel;
            if (controlsContinueTmpTextOrNull != null) controlsContinueTmpTextOrNull.text = controlsContinueLabel;
            _waitingForControlsContinue = false;
            SetControlsPopupVisible(false);
            EnsureBackdropOpacity();
        }

        private void Update()
        {
            if (titleTextOrNull == null && titleTmpOrNull == null) return;
            _pulseTime += Time.unscaledDeltaTime;
            var t = 0.5f + 0.5f * Mathf.Sin(_pulseTime * Mathf.Max(0.1f, titlePulseSpeed));
            var c = Color.Lerp(titlePulseLow, titlePulseHigh, t);
            if (titleTextOrNull != null) titleTextOrNull.color = c;
            if (titleTmpOrNull != null) titleTmpOrNull.color = c;
        }

        private void OnStartClicked()
        {
            if (showControlsBeforeStart)
            {
                EnsureControlsPopupBuilt();
                if (controlsPanelOrNull != null)
                {
                    _waitingForControlsContinue = true;
                    SetMainMenuInteractable(false);
                    SetControlsPopupVisible(true);
                    return;
                }
            }

            StartRunNow();
        }

        private void OnControlsContinueClicked()
        {
            if (!_waitingForControlsContinue) return;
            _waitingForControlsContinue = false;
            SetControlsPopupVisible(false);
            StartRunNow();
        }

        private void StartRunNow()
        {
            var cb = _onStart;
            CloseAllMenus();
            cb?.Invoke();
        }

        private void EnsureBackdropOpacity()
        {
            if (!forceOpaqueBackdrop || backdropOrNull == null) return;
            var c = backdropOrNull.color;
            c.a = 1f;
            backdropOrNull.color = c;
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void CloseAllMenus()
        {
            var menus = FindObjectsByType<StartMenuOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var menu in menus)
            {
                if (menu == null) continue;
                SetOverlayVisible(menu, false);
            }
        }

        private static StartMenuOverlay FindAnyOverlay()
        {
            var menus = FindObjectsByType<StartMenuOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return menus != null && menus.Length > 0 ? menus[0] : null;
        }

        private static void SetOverlayVisible(StartMenuOverlay overlay, bool visible)
        {
            if (overlay == null) return;
            var canvasRoot = overlay.GetComponentInParent<Canvas>();
            if (canvasRoot != null)
                canvasRoot.gameObject.SetActive(visible);
            else
                overlay.gameObject.SetActive(visible);
        }

        private void AutoWireReferences()
        {
            if (backdropOrNull == null)
                backdropOrNull = FindByNameInChildren<Image>("background");

            if (startButtonOrNull == null)
                startButtonOrNull = FindByNameInChildren<Button>("PlayButton");
            if (quitButtonOrNull == null)
                quitButtonOrNull = FindByNameInChildren<Button>("QuitButton");
            if (quitButtonOrNull == null)
                quitButtonOrNull = FindByNameInChildren<Button>("Close-button");
            if (quitButtonOrNull == null)
                quitButtonOrNull = FindByNameInChildren<Button>("CloseButton");

            if (startButtonOrNull != null)
            {
                if (startButtonTextOrNull == null)
                    startButtonTextOrNull = startButtonOrNull.GetComponentInChildren<Text>(true);
                if (startButtonTmpTextOrNull == null)
                    startButtonTmpTextOrNull = startButtonOrNull.GetComponentInChildren<TMP_Text>(true);
            }

            if (titleTextOrNull == null)
                titleTextOrNull = FindByNameInChildren<Text>("Title");
            if (titleTmpOrNull == null)
                titleTmpOrNull = FindByNameInChildren<TMP_Text>("Title");

            if (subtitleTextOrNull == null)
                subtitleTextOrNull = FindByNameInChildren<Text>("Subtitle");
            if (subtitleTmpOrNull == null)
                subtitleTmpOrNull = FindByNameInChildren<TMP_Text>("Subtitle");

            if (controlsPanelOrNull == null)
            {
                var controlsPanelImage = FindByNameInChildren<Image>("ControlsPanel");
                if (controlsPanelImage != null)
                    controlsPanelOrNull = controlsPanelImage.gameObject;
            }
            if (controlsTextOrNull == null)
                controlsTextOrNull = FindByNameInChildren<Text>("ControlsText");
            if (controlsTmpTextOrNull == null)
                controlsTmpTextOrNull = FindByNameInChildren<TMP_Text>("ControlsText");
            if (controlsContinueButtonOrNull == null)
                controlsContinueButtonOrNull = FindByNameInChildren<Button>("ControlsContinueButton");
            if (controlsContinueButtonOrNull != null)
            {
                if (controlsContinueTextOrNull == null)
                    controlsContinueTextOrNull = controlsContinueButtonOrNull.GetComponentInChildren<Text>(true);
                if (controlsContinueTmpTextOrNull == null)
                    controlsContinueTmpTextOrNull = controlsContinueButtonOrNull.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void SetMainMenuInteractable(bool interactable)
        {
            if (startButtonOrNull != null) startButtonOrNull.interactable = interactable;
            if (quitButtonOrNull != null) quitButtonOrNull.interactable = interactable;
        }

        private void SetControlsPopupVisible(bool visible)
        {
            if (controlsPanelOrNull != null)
                controlsPanelOrNull.SetActive(visible);
            SetMainMenuInteractable(!visible);
            if (startButtonOrNull != null) startButtonOrNull.gameObject.SetActive(!visible);
            if (quitButtonOrNull != null) quitButtonOrNull.gameObject.SetActive(!visible);
        }

        private void EnsureControlsPopupBuilt()
        {
            if (controlsPanelOrNull != null)
            {
                ApplyControlsPopupLayout();
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var panelGo = new GameObject("ControlsPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas.transform, false);
            controlsPanelOrNull = panelGo;

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 420f);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0.05f, 0.06f, 0.08f, 0.94f);

            var textGo = new GameObject("ControlsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(panelGo.transform, false);
            controlsTmpTextOrNull = textGo.GetComponent<TMP_Text>();
            if (controlsTmpTextOrNull == null) return;
            controlsTmpTextOrNull.text = controlsDescription;
            controlsTmpTextOrNull.alignment = TextAlignmentOptions.TopLeft;
            controlsTmpTextOrNull.fontSize = 16f;
            controlsTmpTextOrNull.textWrappingMode = TextWrappingModes.Normal;
            controlsTmpTextOrNull.lineSpacing = 4f;
            controlsTmpTextOrNull.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(36f, 96f);
            textRect.offsetMax = new Vector2(-36f, -34f);

            var buttonGo = new GameObject("ControlsContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panelGo.transform, false);
            controlsContinueButtonOrNull = buttonGo.GetComponent<Button>();
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = new Color(0.26f, 0.46f, 0.31f, 1f);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(220f, 52f);
            buttonRect.anchoredPosition = new Vector2(0f, 22f);

            var buttonLabelGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            buttonLabelGo.transform.SetParent(buttonGo.transform, false);
            controlsContinueTmpTextOrNull = buttonLabelGo.GetComponent<TMP_Text>();
            if (controlsContinueTmpTextOrNull == null) return;
            controlsContinueTmpTextOrNull.text = controlsContinueLabel;
            controlsContinueTmpTextOrNull.alignment = TextAlignmentOptions.Center;
            controlsContinueTmpTextOrNull.fontSize = 22f;
            controlsContinueTmpTextOrNull.color = Color.white;
            var buttonLabelRect = buttonLabelGo.GetComponent<RectTransform>();
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = Vector2.zero;
            buttonLabelRect.offsetMax = Vector2.zero;

            controlsContinueButtonOrNull.onClick.RemoveListener(OnControlsContinueClicked);
            controlsContinueButtonOrNull.onClick.AddListener(OnControlsContinueClicked);
            ApplyControlsPopupLayout();
            SetControlsPopupVisible(false);
        }

        private void ApplyControlsPopupLayout()
        {
            if (controlsPanelOrNull == null) return;

            var panelRect = controlsPanelOrNull.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(760f, 420f);
                panelRect.anchoredPosition = Vector2.zero;
            }

            var panelImage = controlsPanelOrNull.GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = new Color(0.05f, 0.06f, 0.08f, 0.94f);

            if (controlsTmpTextOrNull != null)
            {
                controlsTmpTextOrNull.fontSize = 16f;
                controlsTmpTextOrNull.textWrappingMode = TextWrappingModes.Normal;
                controlsTmpTextOrNull.lineSpacing = 4f;
                controlsTmpTextOrNull.alignment = TextAlignmentOptions.TopLeft;
                var tr = controlsTmpTextOrNull.rectTransform;
                tr.anchorMin = new Vector2(0f, 0f);
                tr.anchorMax = new Vector2(1f, 1f);
                tr.offsetMin = new Vector2(36f, 96f);
                tr.offsetMax = new Vector2(-36f, -34f);
            }

            if (controlsContinueButtonOrNull != null)
            {
                var br = controlsContinueButtonOrNull.GetComponent<RectTransform>();
                if (br != null)
                {
                    br.anchorMin = new Vector2(0.5f, 0f);
                    br.anchorMax = new Vector2(0.5f, 0f);
                    br.pivot = new Vector2(0.5f, 0f);
                    br.sizeDelta = new Vector2(220f, 52f);
                    br.anchoredPosition = new Vector2(0f, 22f);
                }
            }
        }

        private T FindByNameInChildren<T>(string name) where T : Component
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (var c in GetComponentsInChildren<T>(true))
            {
                if (c != null && string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase))
                    return c;
            }
            return null;
        }

        private static void EnsureEventSystem()
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            var oldStandalone = es.GetComponent<StandaloneInputModule>();
            if (oldStandalone != null)
                Destroy(oldStandalone);
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            var newInputModule = es.GetComponent("InputSystemUIInputModule");
            if (newInputModule != null)
                Destroy(newInputModule);
            if (es.GetComponent<StandaloneInputModule>() == null)
                es.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
