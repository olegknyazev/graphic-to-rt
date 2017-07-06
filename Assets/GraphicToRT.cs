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
            Utils.WithoutScaleAndRotation(_graphic.transform, () => {
                UpdateRTSize();
                UpdateRT();
            });
        }

        [Conditional("UNITY_EDITOR")]
        public void OnGUI() {
            if (_rt) GUI.DrawTexture(new Rect(0, 0, _rt.width, _rt.height), _rt);
        }

        Rect graphicRect { get { return _rectTransform.rect; } }
        Quaternion graphicRotation { get { return _rectTransform.rotation; } }
        Vector3 graphicNormal { get { return _rectTransform.forward; } }
        Vector3 graphicCenter { get { return _rectTransform.TransformPoint(graphicRect.center); } }
        Vector2 graphicSize { get { return graphicRect.size; } }

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
            var desiredWidth = (int)graphicSize.x;
            var desiredHeight = (int)graphicSize.y;
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
                Utils.WithRenderMode(_localCanvas.rootCanvas, RenderMode.WorldSpace, () => 
                Utils.WithObjectLayer(_graphic.gameObject, layer, () => {
                    _camera.orthographicSize = graphicSize.y / 2;
                    _camera.aspect = graphicSize.x / graphicSize.y;
                    _camera.transform.position = graphicCenter - graphicNormal * _camera.nearClipPlane * 2f;
                    _camera.transform.rotation = graphicRotation;
                    _camera.targetTexture = _rt;
                    _camera.cullingMask = 1 << layer;
                    _camera.Render();
                }));
            }
        }
    }
}
