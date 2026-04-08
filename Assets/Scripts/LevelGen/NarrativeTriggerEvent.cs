using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Triggered narrative beat: TextMeshPro popup + audio + NPC reaction.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NarrativeTriggerEvent : MonoBehaviour
    {
        [Header("Narrative")]
        [TextArea(2, 4)]
        [SerializeField] private string[] lines =
        {
            "You're getting the hang of this, just like I did."
        };
        [SerializeField] private float lineDurationSeconds = 2.7f;
        [SerializeField] private float typeCharDelaySeconds = 0.035f;
        [SerializeField] private float fadeOutSeconds = 1.1f;
        [SerializeField] private bool oneShot = true;

        [Header("Audio")]
        [SerializeField] private AudioClip narrativeClip;
        [SerializeField] private float volume = 1f;

        [Header("Screen Text")]
        [SerializeField] private int popupFontSize = 44;
        [SerializeField] private Color popupColor = new Color(0.95f, 0.95f, 1f, 1f);

        [Header("NPC Reaction")]
        [SerializeField] private NPCNavReact reactingNpc;

        private bool _triggered;
        private TextMeshProUGUI _popup;
        private AudioSource _audioSource;
        private Coroutine _playRoutine;
        private GameObject _canvasRoot;
        private static AudioClip _generatedFallbackClip;

        public void SetLines(string[] newLines)
        {
            lines = newLines;
        }

        public void SetReactingNpc(NPCNavReact npc)
        {
            reactingNpc = npc;
        }

        public void SetNarrativeClip(AudioClip clip)
        {
            narrativeClip = clip;
        }

        private void Awake()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = volume;
            _audioSource.maxDistance = 100f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;

            // Keep overlay canvas owned by this trigger so restarts/unloads cannot orphan it.
            _canvasRoot = new GameObject("NarrativeOverlayCanvas");
            _canvasRoot.transform.SetParent(transform, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            _canvasRoot.AddComponent<CanvasScaler>();
            _canvasRoot.AddComponent<GraphicRaycaster>();

            var popupGo = new GameObject("NarrativePopup");
            popupGo.transform.SetParent(_canvasRoot.transform, false);
            _popup = popupGo.AddComponent<TextMeshProUGUI>();
            _popup.text = "";
            _popup.alignment = TextAlignmentOptions.Center;
            _popup.fontSize = popupFontSize;
            _popup.color = popupColor;
            _popup.outlineWidth = 0.15f;
            _popup.textWrappingMode = TextWrappingModes.Normal;
            var rt = _popup.rectTransform;
            rt.anchorMin = new Vector2(0.08f, 0.72f);
            rt.anchorMax = new Vector2(0.92f, 0.92f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _popup.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_playRoutine != null)
                StopCoroutine(_playRoutine);
            _playRoutine = null;
            _popup = null;
            _canvasRoot = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (oneShot && _triggered) return;
            _triggered = true;
            if (_playRoutine != null) StopCoroutine(_playRoutine);
            _playRoutine = StartCoroutine(PlayNarrative(other.transform));
        }

        private IEnumerator PlayNarrative(Transform player)
        {
            reactingNpc?.ReactToPlayer(player);
            PlayNarrativeAudio();

            if (_popup == null || lines == null || lines.Length == 0)
            {
                _playRoutine = null;
                yield break;
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                _popup.gameObject.SetActive(true);
                _popup.alpha = 1f;
                yield return StartCoroutine(TypeLine(line));
                yield return new WaitForSeconds(lineDurationSeconds);
                yield return StartCoroutine(FadeOutPopup());
            }

            _popup.gameObject.SetActive(false);
            _playRoutine = null;
        }

        private void PlayNarrativeAudio()
        {
            if (_audioSource == null) return;
            var clip = narrativeClip != null ? narrativeClip : GetFallbackClip();
            if (clip != null)
            {
                Debug.Log($"[Narrative] Playing clip: {clip.name} (length: {clip.length:F2}s)");
                _audioSource.PlayOneShot(clip, volume);
            }
            else
            {
                Debug.LogWarning("[Narrative] No audio clip available to play.");
            }
        }

        private static AudioClip GetFallbackClip()
        {
            if (_generatedFallbackClip != null) return _generatedFallbackClip;

            const int sampleRate = 22050;
            const float seconds = 7.0f;
            var sampleCount = Mathf.CeilToInt(sampleRate * seconds);
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var sweep = Mathf.Sin(2f * Mathf.PI * 0.06f * t);
                var root = Mathf.Lerp(76f, 92f, (sweep + 1f) * 0.5f);
                var drone = Mathf.Sin(2f * Mathf.PI * root * t) * 0.045f;
                var harmony = Mathf.Sin(2f * Mathf.PI * (root * 1.333f) * t + 0.6f) * 0.022f;
                var sub = Mathf.Sin(2f * Mathf.PI * (root * 0.5f) * t + 1.7f) * 0.016f;

                // Slow ghostly shimmer, no static-style random noise.
                var shimmer = Mathf.Sin(2f * Mathf.PI * 2.7f * t + Mathf.Sin(2f * Mathf.PI * 0.2f * t)) * 0.004f;

                // Sparse bell-like swells every ~2.6s.
                var pulsePhase = Mathf.Repeat(t, 2.6f) / 2.6f;
                var bellEnv = Mathf.Exp(-8f * pulsePhase);
                var bell = Mathf.Sin(2f * Mathf.PI * 312f * t) * 0.018f * bellEnv;

                var amp = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 0.32f * t + 0.8f);
                data[i] = (drone + harmony + sub + shimmer + bell) * amp;
            }

            _generatedFallbackClip = AudioClip.Create("NarrativeFallbackWhisper", sampleCount, 1, sampleRate, false);
            _generatedFallbackClip.SetData(data, 0);
            return _generatedFallbackClip;
        }

        private IEnumerator TypeLine(string line)
        {
            _popup.text = "";
            for (var i = 1; i <= line.Length; i++)
            {
                _popup.text = line.Substring(0, i);
                yield return new WaitForSeconds(typeCharDelaySeconds);
            }
        }

        private IEnumerator FadeOutPopup()
        {
            if (_popup == null) yield break;
            if (fadeOutSeconds <= 0f)
            {
                _popup.alpha = 0f;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < fadeOutSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / fadeOutSeconds);
                _popup.alpha = 1f - t;
                yield return null;
            }
            _popup.alpha = 0f;
        }
    }
}
