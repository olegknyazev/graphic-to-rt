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

        // in world space
        Vector2 _graphicSize;
        Vector3 _graphicCenter;
        Vector3 _graphicNormal;
        Quaternion _graphicRotation;

        static readonly Vector3[] s_worldCorners = new Vector3[4];

        public void OnEnable() {
            _graphic = GetComponent<Graphic>();
            _rectTransform = GetComponent<RectTransform>();
            _camera = CreateCamera();
            Assert.IsNotNull(_graphic);
        }

        public void OnDisable() {
            if (_camera) DestroyImmediate(_camera.gameObject);
            if (_rt) DestroyImmediate(_rt);
        }

        public void Update() {
            UpdateLocalCanvas();
            UpdateGraphicMetrics();
            UpdateRTSize();
            UpdateRT();
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

        void UpdateGraphicMetrics() {
            var wc = s_worldCorners;
            _rectTransform.GetWorldCorners(wc);
            var xAxis = wc[3] - wc[0];
            var yAxis = wc[1] - wc[0];
            _graphicSize = new Vector2(xAxis.magnitude, yAxis.magnitude);
            _graphicCenter = (wc[0] + wc[1] + wc[2] + wc[3]) / 4;
            _graphicNormal = Vector3.Cross(xAxis, yAxis).normalized;
            _graphicRotation = Quaternion.FromToRotation(Vector3.forward, _graphicNormal);
        }

        void UpdateRTSize() {
            var desiredWidth = (int)_graphicSize.x;
            var desiredHeight = (int)_graphicSize.y;
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
                Utils.WithRenderMode(_localCanvas.rootCanvas, RenderMode.WorldSpace, () => Utils.WithObjectLayer(_graphic.gameObject, layer, () => {
                    _camera.orthographicSize = _graphicSize.y / 2;
                    _camera.aspect = _graphicSize.x / _graphicSize.y;
                    _camera.transform.position = _graphicCenter - _graphicNormal * _camera.nearClipPlane * 2f;
                    _camera.transform.rotation = _graphicRotation;
                    _camera.targetTexture = _rt;
                    _camera.cullingMask = 1 << layer;
                    _camera.Render();
                }));
            }
        }
    }
}
