using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UIToRenderTarget {
    [ExecuteInEditMode]
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(RectTransform))]
    public class Imposter : MonoBehaviour {
        public GraphicToRT source;
        public Shader shader;
        
        RawImage _image;
        RectTransform _rectTransform;

        public void OnEnable() {
            _image = GetComponent<RawImage>();
            _rectTransform = GetComponent<RectTransform>();
            Assert.IsNotNull(_image);
            Assert.IsNotNull(_rectTransform);
            if (shader) _image.material = new Material(shader);
        }

        public void Update() {
            if (source) {
                _image.texture = source.texture;
                //_rectTransform.pivot = source.rectTranform.pivot;
                //_rectTransform.sizeDelta = source.rectTranform.sizeDelta;
            }
        }
    }
}
