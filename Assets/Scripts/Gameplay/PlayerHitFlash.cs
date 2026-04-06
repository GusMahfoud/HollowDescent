using System.Collections;
using UnityEngine;

namespace HollowDescent.Gameplay
{
    /// <summary>
    /// Brief color flash on all body materials when the player takes damage.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHitFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private float flashDuration = 0.22f;
        [SerializeField] private float peakNormalized = 0.35f;

        private PlayerHealth _health;
        private RendererMaterialSnapshot[] _snapshots;
        private Coroutine _flashRoutine;

        private struct RendererMaterialSnapshot
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public Color Original;
            public bool HasBaseColor;
        }

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
            CacheMaterials();
        }

        private void OnEnable()
        {
            if (_health == null) _health = GetComponent<PlayerHealth>();
            if (_health != null) _health.OnDamaged += StartHitFlash;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDamaged -= StartHitFlash;
        }

        private void CacheMaterials()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            var list = new System.Collections.Generic.List<RendererMaterialSnapshot>();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mats = r.materials;
                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    var orig = ReadSurfaceColor(m, out var hasBase);
                    list.Add(new RendererMaterialSnapshot
                    {
                        Renderer = r,
                        MaterialIndex = i,
                        Original = orig,
                        HasBaseColor = hasBase
                    });
                }
            }
            _snapshots = list.ToArray();
        }

        private static Color ReadSurfaceColor(Material m, out bool usedBaseColor)
        {
            usedBaseColor = false;
            if (m.HasProperty("_BaseColor"))
            {
                usedBaseColor = true;
                return m.GetColor("_BaseColor");
            }
            if (m.HasProperty("_Color"))
                return m.GetColor("_Color");
            return Color.white;
        }

        private static void WriteSurfaceColor(Material m, Color c, bool preferBase)
        {
            if (preferBase && m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            else if (m.HasProperty("_Color"))
                m.SetColor("_Color", c);
            else if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
        }

        private void StartHitFlash()
        {
            if (!isActiveAndEnabled || _snapshots == null || _snapshots.Length == 0) return;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            var elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / flashDuration);
                var envelope = u < peakNormalized
                    ? Mathf.Clamp01(u / peakNormalized)
                    : Mathf.Clamp01((1f - u) / (1f - peakNormalized));
                foreach (var s in _snapshots)
                {
                    if (s.Renderer == null) continue;
                    var mats = s.Renderer.materials;
                    if (s.MaterialIndex >= mats.Length) continue;
                    var blended = Color.Lerp(s.Original, flashColor, envelope);
                    WriteSurfaceColor(mats[s.MaterialIndex], blended, s.HasBaseColor);
                }
                yield return null;
            }

            RestoreOriginals();
            _flashRoutine = null;
        }

        private void RestoreOriginals()
        {
            if (_snapshots == null) return;
            foreach (var s in _snapshots)
            {
                if (s.Renderer == null) continue;
                var mats = s.Renderer.materials;
                if (s.MaterialIndex >= mats.Length) continue;
                WriteSurfaceColor(mats[s.MaterialIndex], s.Original, s.HasBaseColor);
            }
        }
    }
}
