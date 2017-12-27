using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    [RequireComponent(typeof(Graphic))]
    public class GraphicToRT : BaseMeshEffect {
        RectTransform _rectTransform;
        RenderTexture _rt;

        Mesh _mesh;
        Material _material;
        CommandBuffer _commandBuffer;
        MaterialPropertyBlock _materialProperties;

        bool _dirty;
        Rect _appliedRect;
        Color _appliedColor;
        Texture _appliedTexture;

        public Texture texture { get { return _rt; } }

        public event Action<GraphicToRT> textureChanged;

        protected override void OnEnable() {
            _mesh = new Mesh();
            _commandBuffer = new CommandBuffer();
            _materialProperties = new MaterialPropertyBlock();
            graphic.RegisterDirtyMaterialCallback(OnMaterialDirty);
            Touch(CanvasUpdateRegistry.instance); // CanvasUpdateRegistry should be subscribed first
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            base.OnEnable();
        }

        protected override void OnDisable() {
            graphic.UnregisterDirtyMaterialCallback(OnMaterialDirty);
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            base.OnDisable();
            if (_rt) DestroyImmediate(_rt);
            if (_mesh) DestroyImmediate(_mesh);
            if (_material) DestroyImmediate(_material);
            if (_commandBuffer != null) _commandBuffer.Dispose();
            if (_materialProperties != null) _materialProperties.Clear();
        }

        [Conditional("UNITY_EDITOR")]
        public void OnGUI() {
            if (_rt) GUI.DrawTexture(new Rect(0, 0, _rt.width, _rt.height), _rt);
        }

        public void LateUpdate() {
            var newCaptureRect = graphic.rectTransform.rect.SnappedToPixels();
            if (newCaptureRect != _appliedRect) {
                _appliedRect = newCaptureRect;
                _dirty = true;
            }
            if (_appliedColor != graphic.color) {
                _appliedColor = graphic.color;
                _dirty = true;
            }
            if (_appliedTexture != graphic.mainTexture) {
                _appliedTexture = graphic.mainTexture;
                _dirty = true;
            }
            var width = (int)_appliedRect.width;
            var height = (int)_appliedRect.height;
            var shouldBeCreated = graphic.enabled;
            if (!_rt && shouldBeCreated
                || _rt && !shouldBeCreated
                || _rt && _rt.width != width
                || _rt && _rt.height != height) {
                if (_rt)
                    DestroyImmediate(_rt);
                if (shouldBeCreated)
                    _rt = new RenderTexture(width, height, 0).HideAndDontSave();
                _dirty = true;
                textureChanged.InvokeSafe(this);
            }
        }

        public override void ModifyMesh(VertexHelper vh) {
            if (_mesh) {
                vh.FillMesh(_mesh);
                _dirty = true;
            }
        }

        void OnMaterialDirty() {
            _dirty = true;
        }

        void OnWillRenderCanvases() {
            if (_dirty) {
                Render();
                _dirty = false;
            }
        }

        void Render() {
            UpdateMaterial();
            if (_material) {
                UpdateCommandBuffer();
                Graphics.ExecuteCommandBuffer(_commandBuffer);
            }
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
            if (!_material && prototype) {
                _material = new Material(prototype);
                UpdateCommandBuffer();
            }
        }

        void UpdateCommandBuffer() {
            _materialProperties.Clear();
            _materialProperties.SetVector("_ClipRect", new Vector4(-1000, -1000, 1000, 1000));
            if (_appliedTexture)
                _materialProperties.SetTexture("_MainTex", _appliedTexture);
            _materialProperties.SetColor("_Color", _appliedColor);
            _materialProperties.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);

            var r = _appliedRect;
            _commandBuffer.Clear();
            _commandBuffer.SetRenderTarget(_rt);
            _commandBuffer.ClearRenderTarget(false, true, Color.clear);
            _commandBuffer.SetViewProjectionMatrices(
                Matrix4x4.identity,
                Matrix4x4.Ortho(r.xMin, r.xMax, r.yMin, r.yMax, -10, 1000));
            _commandBuffer.DrawMesh(_mesh, Matrix4x4.identity, _material, 0, 0, _materialProperties);
        }

        static void Touch<T>(T obj) { }
    }
}
