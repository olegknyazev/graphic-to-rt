using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(RectTransform))]
    public class Imposter : MonoBehaviour {
        [SerializeField] GraphicToRT _source;
        [SerializeField] Shader _shader;
        
        RawImage _image; // TODO get rid of RawImage
        RectTransform _rectTransform;
        Material _material;

        GraphicToRT _appliedSource;
        Shader _appliedShader;

        public GraphicToRT source {
            get { return _source; }
            set {
                if (_source != value) {
                    _source = value;
                    ApplySource();
                }
            }
        }

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
            _image = GetComponent<RawImage>();
            _rectTransform = GetComponent<RectTransform>();
            Assert.IsNotNull(_image);
            Assert.IsNotNull(_rectTransform);
            ApplyShader();
            ApplySource();
            ApplyTexture();
            ApplyMaterial();
        }

        public void OnDisable() {
            DestroyMaterial();
        }

        [Conditional("UNITY_EDITOR")]
        public void OnValidate() {
            ApplyShader();
            ApplySource();
        }

        void OnFixupApplyChanged(GraphicToRT graphicToRT) {
            Assert.IsNotNull(graphicToRT);
            Assert.AreEqual(_source, graphicToRT);
            ApplyMaterialProperties();
        }

        void OnTextureChanged(GraphicToRT graphicToRT) {
            Assert.IsNotNull(graphicToRT);
            Assert.AreEqual(_source, graphicToRT);
            ApplyTexture();
        }

        void ApplySource() {
            if (_appliedSource != _source) {
                if (_appliedSource) {
                    _appliedSource.fixupAlphaChanged -= OnFixupApplyChanged;
                    _appliedSource.textureChanged -= OnTextureChanged;
                }
                _appliedSource = _source;
                if (_appliedSource) {
                    _appliedSource.fixupAlphaChanged += OnFixupApplyChanged;
                    _appliedSource.textureChanged += OnTextureChanged;
                }
                ApplyMaterialProperties();
                ApplyTexture();
            }
        }

        void ApplyShader() {
            if (_appliedShader != _shader) {
                if (_material)
                    DestroyMaterial();
                _appliedShader = _shader;
                if (_shader)
                    _material = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
                ApplyMaterial();
                ApplyMaterialProperties();
            }
        }

        void ApplyMaterial() {
            if (_image)
                _image.material = _material;
        }

        void ApplyTexture() {
            if (_image && _source)
                _image.texture = _source.texture;
        }

        void ApplyMaterialProperties() {
            if (_material && _source)
                if (_source.fixupAlpha) // GraphicToRT already fixed channels
                    _material.DisableKeyword(Ids.FIX_ALPHA);
                else
                    _material.EnableKeyword(Ids.FIX_ALPHA);
        }
        
        void DestroyMaterial() {
            DestroyImmediate(_material);
            _material = null;
        }

        static class Ids {
            public static readonly string FIX_ALPHA = "GRAPHIC_TO_RT_FIX_ALPHA";
        }
    }
}
