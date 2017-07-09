using UnityEngine;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    public class FixupAlphaEffect : MonoBehaviour {
        public Shader shader;

        Material _material;

        public void OnEnable() {
            if (!shader || !shader.isSupported) {
                enabled = false;
                return;
            }
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest) {
            Graphics.Blit(src, dest, _material);
        }
    }
}
