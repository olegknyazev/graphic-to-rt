using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
using UnityEngine;
using UnityEngine.Assertions;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    public class FixupAlphaEffect : MonoBehaviour {
        [SerializeField] Shader _shader;

        Material _material;
        Shader _appliedShader;

        public Shader shader {
            get { return _shader; }
            set {
                if (_shader != value) {
                    _shader = value;
                    ApplyShader();
                }
            }
        }

        public void OnEnable() {
            if (DisableIfNotSupported())
                return;
            ApplyShader();
        }

        public void OnDisable() {
            DestroyMaterial();
        }

        [Conditional("UNITY_EDITOR")]
        public void OnValidate() {
            ApplyShader();
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest) {
            Assert.IsNotNull(_material);
            Graphics.Blit(src, dest, _material);
        }

        void ApplyShader() {
            if (_appliedShader != _shader) {
                DestroyMaterial();
                _appliedShader = _shader;
                if (DisableIfNotSupported())
                    return;
                _material = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        bool DisableIfNotSupported() {
            if (!_shader || !_shader.isSupported) {
                enabled = false;
                return true;
            }
            return false;
        }

        void DestroyMaterial() {
            if (_material) {
                DestroyImmediate(_material);
                _material = null;
            }
        }
    }
}
