using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HollowDescent.Gameplay
{
    /// <summary>Brief tint pulse on all enemy body materials when damaged (mirrors <see cref="PlayerHitFlash"/>).</summary>
    [DisallowMultipleComponent]
    public class EnemyHitFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = new Color(1f, 0.92f, 0.85f, 1f);
        [SerializeField] private float flashDuration = 0.14f;

        private struct Snap
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public Color Original;
            public bool HasBaseColor;
        }

        private List<Snap> _snaps;
        private Coroutine _routine;

        private void Start() => RebuildCache();

        public void RebuildCache()
        {
            _snaps = new List<Snap>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    var orig = ReadSurfaceColor(m, out var hasBase);
                    _snaps.Add(new Snap { Renderer = r, MaterialIndex = i, Original = orig, HasBaseColor = hasBase });
                }
            }
        }

        public void PlayHitFlash()
        {
            if (!isActiveAndEnabled) return;
            if (_snaps == null || _snaps.Count == 0)
                RebuildCache();
            if (_snaps == null || _snaps.Count == 0) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            var elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = 1f - Mathf.Clamp01(elapsed / flashDuration);
                foreach (var s in _snaps)
                {
                    if (s.Renderer == null) continue;
                    var mats = s.Renderer.materials;
                    if (s.MaterialIndex >= mats.Length) continue;
                    var blended = Color.Lerp(s.Original, flashColor, t * t);
                    WriteSurfaceColor(mats[s.MaterialIndex], blended, s.HasBaseColor);
                }
                yield return null;
            }

            Restore();
            _routine = null;
        }

        private void Restore()
        {
            if (_snaps == null) return;
            foreach (var s in _snaps)
            {
                if (s.Renderer == null) continue;
                var mats = s.Renderer.materials;
                if (s.MaterialIndex >= mats.Length) continue;
                WriteSurfaceColor(mats[s.MaterialIndex], s.Original, s.HasBaseColor);
            }
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
    }
}
