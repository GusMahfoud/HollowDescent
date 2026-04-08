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

        [Header("Styling")]
        [SerializeField] private string defaultSubtitle = "Descend if you dare.";
        [SerializeField] private Color titlePulseLow = new Color(0.92f, 0.93f, 0.95f);
        [SerializeField] private Color titlePulseHigh = new Color(1f, 1f, 1f);
        [SerializeField] private float titlePulseSpeed = 1.8f;
        [SerializeField] private bool forceOpaqueBackdrop = true;

        private Action _onStart;
        private float _pulseTime;

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
        }

        private void OnDisable()
        {
            if (startButtonOrNull != null)
                startButtonOrNull.onClick.RemoveListener(OnStartClicked);
            if (quitButtonOrNull != null)
                quitButtonOrNull.onClick.RemoveListener(OnQuitClicked);
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
