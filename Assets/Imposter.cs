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
            base.OnEnable();
        }

        protected override void OnDisable() {
            ApplySource(null);
            ApplyShader(null);
            _tracker.Clear();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();
            ApplyShader();
            ApplySource();
            ApplySize();
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            if (!_source || !_source.texture)
                return;
            base.OnPopulateMesh(vh);
        }

        void OnTextureChanged(GraphicToRT sender) {
            ApplySize();
            SetMaterialDirty();
            SetVerticesDirty();
        }

        void ApplySource() { ApplySource(_source); }
        void ApplySource(GraphicToRT source) {
            if (_appliedSource != source) {
                if (_appliedSource)
                    _appliedSource.textureChanged -= OnTextureChanged;
                _appliedSource = source;
                if (_appliedSource)
                    _appliedSource.textureChanged += OnTextureChanged;
                SetMaterialDirty();
                ApplySize();
            }
        }

        void ApplyShader() { ApplyShader(shader); }
        void ApplyShader(Shader shader) {
            var shouldBeCreated = shader != null;
            if (!_material && shouldBeCreated
                    || _material && !shouldBeCreated
                    || _appliedShader != shader) {
                if (_material) {
                    DestroyImmediate(_material);
                    _material = null;
                }
                _appliedShader = shader;
                if (shader)
                    _material = new Material(shader).HideAndDontSave();
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
    }
}
