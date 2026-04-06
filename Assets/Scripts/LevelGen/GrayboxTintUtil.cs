using UnityEngine;

namespace HollowDescent.LevelGen
{
    /// <summary>
    /// Tints renderers without calling <see cref="Renderer.material"/> (which instantiates leaks in edit mode).
    /// Assigns a dedicated <see cref="Material"/> instance via <see cref="Renderer.sharedMaterial"/>.
    /// Bake step then replaces these with asset materials under Assets/Materials/BakedGraybox.
    /// </summary>
    public static class GrayboxTintUtil
    {
        private static Shader _litShader;

        private static Shader ResolveLitShader()
        {
            if (_litShader != null) return _litShader;
            _litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_litShader == null) _litShader = Shader.Find("HDRP/Lit");
            if (_litShader == null) _litShader = Shader.Find("Standard");
            return _litShader;
        }

        public static void Apply(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            var sh = ResolveLitShader();
            if (sh == null)
            {
                Debug.LogWarning("[GrayboxTintUtil] No Lit shader found; skipping tint.");
                return;
            }
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            renderer.sharedMaterial = m;
        }
    }
}
