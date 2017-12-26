using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    [RequireComponent(typeof(Graphic))]
    public class GraphicToRT : BaseMeshEffect {
        public UnityEvent textureChanged = new UnityEvent();

        public Texture texture { get { return _rt; } }

        public RectTransform rectTranform {
            get { return _rectTransform ?? (_rectTransform = GetComponent<RectTransform>()); }
        }

        public ImposterMetrics imposterMetrics {
            get {
                return _metrics ?? (_metrics = new ImposterMetrics(rectTranform));
            }
        }

        RectTransform _rectTransform;
        ImposterMetrics _metrics;
        RenderTexture _rt;

        Mesh _mesh;
        Material _material;
        CommandBuffer _commandBuffer;

        protected override void OnEnable() {
            _mesh = new Mesh();
            _commandBuffer = new CommandBuffer();
            graphic.RegisterDirtyMaterialCallback(OnMaterialDirty);
            base.OnEnable();
        }

        protected override void OnDisable() {
            graphic.UnregisterDirtyMaterialCallback(OnMaterialDirty);
            base.OnDisable();
            if (_rt) DestroyImmediate(_rt);
            if (_mesh) DestroyImmediate(_mesh);
            if (_material) DestroyImmediate(_material);
            if (_commandBuffer != null) _commandBuffer.Dispose();
        }

        [Conditional("UNITY_EDITOR")]
        public void OnGUI() {
            if (_rt) GUI.DrawTexture(new Rect(0, 0, _rt.width, _rt.height), _rt);
        }

        public void Update() {
            UpdateRTSize();
        }

        public override void ModifyMesh(VertexHelper vh) {
            vh.FillMesh(_mesh);
            Render();
        }

        void OnMaterialDirty() {
            Render();
        }

        void UpdateMaterial() {
            RecreateMaterialIfNeeded();
            if (_material)
                _material.CopyPropertiesFromMaterial(graphic.materialForRendering);
        }

        void RecreateMaterialIfNeeded() {
            var prototype = graphic.materialForRendering;
            if (_material && (!prototype || _material.shader != prototype.shader)) {
                DestroyImmediate(_material);
                _material = null;
            }
            if (!_material && prototype)
                _material = new Material(prototype);
        }

        void Render() {
            UpdateMaterial();
            if (!_material)
                return;
            var props = new MaterialPropertyBlock();
            props.SetVector("_ClipRect", new Vector4(-1000, -1000, 1000, 1000));
            props.SetTexture("_MainTex", graphic.mainTexture);
            props.SetColor("_Color", graphic.color);
            props.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
            var r = imposterMetrics.rect;
            _commandBuffer.Clear();
            _commandBuffer.SetRenderTarget(_rt);
            _commandBuffer.ClearRenderTarget(false, true, Color.clear);
            _commandBuffer.SetViewProjectionMatrices(
                Matrix4x4.identity,
                Matrix4x4.Ortho(r.xMin, r.xMax, r.yMin, r.yMax, -10, 1000));
            _commandBuffer.DrawMesh(_mesh, Matrix4x4.identity, _material, 0, 0, props);
            Graphics.ExecuteCommandBuffer(_commandBuffer);
        }

        void UpdateRTSize() {
            var width = imposterMetrics.width;
            var height = imposterMetrics.height;
            var shouldBeCreated = graphic.enabled;
            if (!_rt && shouldBeCreated
                    || _rt && !shouldBeCreated
                    || _rt && _rt.width != width
                    || _rt && _rt.height != height) {
                if (_rt)
                    DestroyImmediate(_rt);
                if (shouldBeCreated)
                    _rt = new RenderTexture(width, height, 0).HideAndDontSave();
                textureChanged.Invoke();
            }
        }
    }

    public class ImposterMetrics {
        static readonly Rect INVALID_RECT = new Rect(Single.NaN, Single.NaN, Single.NaN, Single.NaN);

        readonly RectTransform _transform;

        Rect _rect;
        int _width;
        int _height;
        Rect _appliedSourceRect = INVALID_RECT;

        public ImposterMetrics(RectTransform transform) {
            _transform = transform;
        }

        public Rect sourceRect { get { return _transform.rect; } }

        public Rect rect { get { RecalculateIfNeeded(); return _rect; } }
        public int width { get { RecalculateIfNeeded(); return _width; } }
        public int height { get { RecalculateIfNeeded(); return _height; } }

        void RecalculateIfNeeded() {
            if (_appliedSourceRect != sourceRect) {
                _rect = sourceRect.SnappedToPixels();
                _width = (int)_rect.width;
                _height = (int)_rect.height;
                _appliedSourceRect = sourceRect;
            }
        }
    }
}
