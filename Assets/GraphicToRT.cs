using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    [RequireComponent(typeof(Graphic))]
    public class GraphicToRT : MonoBehaviour {
        public int layer = 31;

        Graphic _graphic;
        RectTransform _rectTransform;
        RenderTexture _rt;
        Camera _camera;
        GraphicRaycaster _originalRaycaster;
        Canvas _localCanvas;
        GraphicRaycaster _localRaycaster;
        ImposterMetrics _metrics;

        public void OnEnable() {
            _graphic = GetComponent<Graphic>();
            _rectTransform = GetComponent<RectTransform>();
            _camera = CreateCamera();
            _metrics = new ImposterMetrics(_rectTransform);
            Assert.IsNotNull(_graphic);
        }

        public void OnDisable() {
            if (_camera) DestroyImmediate(_camera.gameObject);
            if (_rt) DestroyImmediate(_rt);
        }

        public void Update() {
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
            Rect _actualForSourceRect = INVALID_RECT;

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
                if (_actualForSourceRect != sourceRect) {
                    _rect = sourceRect.SnappedToPixels();
                    _width = (int)_rect.width;
                    _height = (int)_rect.height;
                    _actualForSourceRect = sourceRect;
                }
            }
        }
    }
}
