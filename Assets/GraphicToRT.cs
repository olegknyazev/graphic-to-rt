using System;
using System.Collections.Generic;
using UnityEditor;
using Conditional = System.Diagnostics.ConditionalAttribute;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    [RequireComponent(typeof(Graphic))]
    public class GraphicToRT : BaseMeshEffect {
        public Texture texture { get { return _rt; } } // TODO update texture on request?

        public RectTransform rectTranform {
            get { return _rectTransform ?? (_rectTransform = GetComponent<RectTransform>()); }
        }

        public event Action<GraphicToRT> textureChanged;

        Graphic _graphic;
        RectTransform _rectTransform;
        RenderTexture _rt;
        ImposterMetrics _metrics;

        Mesh _mesh;
        Material _material;
        CommandBuffer _commandBuffer;

        protected override void OnEnable() {
            _graphic = GetComponent<Graphic>();
            _rectTransform = GetComponent<RectTransform>();
            Assert.IsNotNull(_graphic);
            Assert.IsNotNull(_rectTransform);
            _metrics = new ImposterMetrics(_rectTransform);
            _mesh = new Mesh();
            _commandBuffer = new CommandBuffer();
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            base.OnEnable();
        }

        protected override void OnDisable() {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            base.OnDisable();
            if (_rt) DestroyImmediate(_rt);
            if (_mesh) DestroyImmediate(_mesh);
            if (_material) DestroyImmediate(_material);
            if (_commandBuffer != null) _commandBuffer.Dispose();
        }

        void UpdateMaterial() {
            RecreateMaterialIfNeeded();
            if (_material)
                _material.CopyPropertiesFromMaterial(_graphic.materialForRendering);
        }

        void RecreateMaterialIfNeeded() {
            var prototype = _graphic.materialForRendering;
            if (_material && (!prototype || _material.shader != prototype.shader)) {
                DestroyImmediate(_material);
                _material = null;
            }
            if (!_material && prototype)
                _material = new Material(prototype);
        }

        [Conditional("UNITY_EDITOR")]
        public void OnGUI() {
            if (_rt) GUI.DrawTexture(new Rect(0, 0, _rt.width, _rt.height), _rt);
        }

        public void Update() {
            UpdateRTSize();
        }

        public override void ModifyMesh(VertexHelper vh) {
            _mesh.Clear();
            vh.FillMesh(_mesh);
        }

        void OnWillRenderCanvases() {
            RenderVerticesCommandBuffer();
        }

        void RenderVerticesCommandBuffer() {
            UpdateMaterial();
            if (!_material)
                return;
            var props = new MaterialPropertyBlock();
            props.SetVector("_ClipRect", new Vector4(-1000, -1000, 1000, 1000));
            props.SetTexture("_MainTex", _graphic.mainTexture);
            props.SetColor("_Color", _graphic.color);
            props.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
            _commandBuffer.Clear();
            _commandBuffer.SetRenderTarget(_rt);
            _commandBuffer.ClearRenderTarget(false, true, Color.clear);
            _commandBuffer.SetViewProjectionMatrices(
                Matrix4x4.identity,
                Matrix4x4.Ortho(0, _metrics.rect.width, 0, _metrics.rect.height, -10, 1000));
            _commandBuffer.DrawMesh(_mesh,
                Matrix4x4.identity,
                _material, 0, 0, props);
            Graphics.ExecuteCommandBuffer(_commandBuffer);
        }

        void UpdateRTSize() {
            if (!_rt || _rt.width != _metrics.width || _rt.height != _metrics.height) {
                if (_rt)
                    DestroyImmediate(_rt);
                _rt = new RenderTexture(_metrics.width, _metrics.height, 0, RenderTextureFormat.ARGB32) {
                    hideFlags = HideFlags.HideAndDontSave
                };
                textureChanged.InvokeSafe(this);
            }
        }

        class ImposterMetrics {
            static readonly Rect INVALID_RECT = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);

            readonly RectTransform _transform;

            Rect _rect;
            int _width;
            int _height;
            Rect _appliedSourceRect = INVALID_RECT;

            public ImposterMetrics(RectTransform transform) {
                _transform = transform;
            }

            public Rect sourceRect { get { return _transform.rect; } }

            public Quaternion rotation { get { return _transform.rotation; } }
            public Vector3 normal { get { return _transform.forward; } }
            public Vector3 center { get { return _transform.TransformPoint(rect.center); } }

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
}
