using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    public class Imposter : Graphic {
        [SerializeField] GraphicToRT _source;
        [SerializeField] Shader _shader;
        
        RectTransform _rectTransform;
        Material _material;
        DrivenRectTransformTracker _tracker;

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

        public override Texture mainTexture {
            get { return _source ? _source.texture : base.mainTexture; }
        }

        protected override void OnEnable() {
            _rectTransform = GetComponent<RectTransform>();
            Assert.IsNotNull(_rectTransform);
            _tracker.Clear();
            _tracker.Add(this, _rectTransform, DrivenTransformProperties.SizeDelta);
            ApplyShader();
            ApplySource();
            ApplyTexture();
            ApplyMaterial();
            ApplySize();
            base.OnEnable();
        }

        protected override void OnDisable() {
            DestroyMaterial();
            _tracker.Clear();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();
            ApplyShader();
            ApplySource();
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh) {
            var rect = GetPixelAdjustedRect();
            vh.Clear();
            vh.AddVert(rect.xMin, rect.yMin, 0, 0);
            vh.AddVert(rect.xMin, rect.yMax, 0, 1);
            vh.AddVert(rect.xMax, rect.yMax, 1, 1);
            vh.AddVert(rect.xMax, rect.yMin, 1, 0);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
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
            ApplySize();
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
                ApplySize();
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
            material = _material;
        }

        void ApplyTexture() {
            SetMaterialDirty();
        }

        void ApplySize() {
            if (_source)
                _rectTransform.sizeDelta = _source.rectTranform.sizeDelta;
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
            material = null;
        }

        static class Ids {
            public static readonly string FIX_ALPHA = "GRAPHIC_TO_RT_FIX_ALPHA";
        }
    }
}
