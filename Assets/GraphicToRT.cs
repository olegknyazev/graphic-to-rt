using System;
using Conditional = System.Diagnostics.ConditionalAttribute;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    [RequireComponent(typeof(Graphic))]
    public class GraphicToRT : MonoBehaviour {
        public int layer = 31;
        public Shader fixupAlphaShader;
        public bool fixupAlpha = true;

        public Texture texture { get { return _rt; } }
        public RectTransform rectTranform { get { return _rectTransform; } }

        public event Action<GraphicToRT> fixupAlphaChanged;
        public event Action<GraphicToRT> textureChanged;

        Graphic _graphic;
        RectTransform _rectTransform;
        RenderTexture _rt;
        Camera _camera;
        GraphicRaycaster _originalRaycaster;
        Canvas _localCanvas;
        GraphicRaycaster _localRaycaster;
        ImposterMetrics _metrics;

        bool _appliedFixupAlpha = false;
        Shader _appliedFixupAlphaShader = null;

        public void OnEnable() {
            _graphic = GetComponent<Graphic>();
            _rectTransform = GetComponent<RectTransform>();
            Assert.IsNotNull(_graphic);
            Assert.IsNotNull(_rectTransform);
            _camera = CreateCamera();
            _metrics = new ImposterMetrics(_rectTransform);
            ApplyFixupAlpha();
        }

        public void OnDisable() {
            if (_camera) DestroyImmediate(_camera.gameObject);
            if (_rt) DestroyImmediate(_rt);
        }

        public void Update() {
            if (fixupAlpha != _appliedFixupAlpha || fixupAlphaShader != _appliedFixupAlphaShader) {
                ApplyFixupAlpha();
                fixupAlphaChanged.InvokeSafe(this);
            }
            UpdateLocalCanvas();
            Utils.WithoutScaleAndRotation(_graphic.transform, () => {
                UpdateRTSize();
                UpdateRT();
            });
        }

        [Conditional("UNITY_EDITOR")]
        public void OnGUI() {
            if (_rt) GUI.DrawTexture(new Rect(0, 0, _rt.width, _rt.height), _rt);
        }

        Camera CreateCamera() {
            var camera =
                new GameObject("GraphicToRT Camera") {
                    hideFlags = HideFlags.HideAndDontSave
                }.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            return camera;
        }

        void ApplyFixupAlpha() {
            var effect = _camera.GetComponent<FixupAlphaEffect>();
            if (effect && !fixupAlpha)
                DestroyImmediate(effect);
            else if (!effect && fixupAlpha)
                effect = _camera.gameObject.AddComponent<FixupAlphaEffect>();
            if (effect) {
                effect.shader = fixupAlphaShader;
                effect.enabled = true; // it might be disabled before if shader wasn't set
            }
            _appliedFixupAlpha = fixupAlpha;
            _appliedFixupAlphaShader = fixupAlphaShader;
        }

        void UpdateLocalCanvas() {
            if (!_localCanvas)
                _localCanvas = this.GetOrCreateComponent<Canvas>();
            if (!_originalRaycaster)
                _originalRaycaster = _localCanvas.rootCanvas.GetComponent<GraphicRaycaster>();
            if (_originalRaycaster) {
                if (!_localRaycaster)
                    _localRaycaster = _graphic.GetOrCreateComponent<GraphicRaycaster>();
                _localRaycaster.blockingObjects = _originalRaycaster.blockingObjects;
                _localRaycaster.ignoreReversedGraphics = _originalRaycaster.ignoreReversedGraphics;
            }
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

        void UpdateRT() {
            if (_rt && _localCanvas.rootCanvas) {
                Assert.AreEqual(_metrics.width, _rt.width);
                Assert.AreEqual(_metrics.height, _rt.height);
                Utils.WithRenderMode(_localCanvas.rootCanvas, RenderMode.WorldSpace, () =>
                Utils.WithObjectLayer(_graphic.gameObject, layer, () => {
                    _camera.orthographicSize = _metrics.height / 2f;
                    _camera.aspect = _metrics.width / (float)_metrics.height;
                    _camera.transform.position = _metrics.center - _metrics.normal * _camera.nearClipPlane * 2f;
                    _camera.transform.rotation = _metrics.rotation;
                    _camera.targetTexture = _rt;
                    _camera.cullingMask = 1 << layer;
                    _camera.Render();
                }));
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
