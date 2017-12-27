using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    public class Imposter : Graphic {
        [SerializeField] GraphicToRT _source;
        [SerializeField] Shader _shader;
        
        RectTransform _rectTransform;
        DrivenRectTransformTracker _tracker;
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

        public override Texture mainTexture {
            get { return _source ? _source.texture : base.mainTexture; }
        }

        protected override void Awake() {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
            Assert.IsNotNull(_rectTransform);
        }

        protected override void OnEnable() {
            _tracker.Clear();
            _tracker.Add(this, _rectTransform, DrivenTransformProperties.SizeDelta);
            ApplyShader();
            ApplySource();
            SetMaterialDirty();
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
            vh.Clear();
            if (!_source || !_source.texture)
                return;
            base.OnPopulateMesh(vh);
        }

        void OnTextureChanged() {
            ApplySize();
            SetMaterialDirty();
            SetVerticesDirty();
        }

        void ApplySource() {
            if (_appliedSource != _source) {
                if (_appliedSource)
                    _appliedSource.textureChanged.RemoveListener(OnTextureChanged);
                _appliedSource = _source;
                if (_appliedSource)
                    _appliedSource.textureChanged.AddListener(OnTextureChanged);
                SetMaterialDirty();
                ApplySize();
            }
        }

        void ApplyShader() {
            var shouldBeCreated = _shader != null;
            if (!_material && shouldBeCreated
                    || _material && !shouldBeCreated
                    || _appliedShader != _shader) {
                if (_material)
                    DestroyMaterial();
                _appliedShader = _shader;
                if (_shader)
                    _material = new Material(_shader).HideAndDontSave();
                ApplyMaterial();
            }
        }

        void ApplyMaterial() {
            material = _material;
        }

        void ApplySize() {
            if (_rectTransform && _source && _source.texture)
                _rectTransform.sizeDelta = new Vector2(_source.texture.width, _source.texture.height);
        }
        
        void DestroyMaterial() {
            DestroyImmediate(_material);
            _material = null;
            material = null;
        }
    }
}
