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
            var desiredWidth = (int)_metrics.width;
            var desiredHeight = (int)_metrics.height;
            if (!_rt || _rt.width != desiredWidth || _rt.height != desiredHeight) {
                if (_rt)
                    DestroyImmediate(_rt);
                _rt = new RenderTexture(desiredWidth, desiredHeight, 0, RenderTextureFormat.ARGB32) {
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
            readonly RectTransform _rectTransform;

            public ImposterMetrics(RectTransform transform) {
                _rectTransform = transform;
            }

            public Rect sourceRect { get { return _rectTransform.rect; } }
            
            public Quaternion rotation { get { return _rectTransform.rotation; } }
            public Vector3 normal { get { return _rectTransform.forward; } }
            public Vector3 center { get { return _rectTransform.TransformPoint(rect.center); } }

            public Rect rect { get { return sourceRect.SnappedToPixels(); } }
            public int width { get { return (int)rect.width; } }
            public int height { get { return (int)rect.height; } }

            // TODO cache
        }
    }
}
