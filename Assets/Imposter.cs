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
        Material _material;

        public void OnEnable() {
            _image = GetComponent<RawImage>();
            _rectTransform = GetComponent<RectTransform>();
            Assert.IsNotNull(_image);
            Assert.IsNotNull(_rectTransform);
            if (source)
                source.fixupAlphaChanged += _ => ApplyMaterialProperties();
            if (shader) {
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                ApplyMaterialProperties();
                _image.material = _material;
            }
        }

        public void Update() {
            if (source) {
                _image.texture = source.texture;
                //_rectTransform.pivot = source.rectTranform.pivot;
                //_rectTransform.sizeDelta = source.rectTranform.sizeDelta;
            }
        }

        void ApplyMaterialProperties() {
            if (_material) {
                if (source)
                    if (source.fixupAlpha) // GraphicToRT already fixed it
                        _material.DisableKeyword(Ids.FIX_ALPHA);
                    else
                        _material.EnableKeyword(Ids.FIX_ALPHA);
            }
        }

        static class Ids {
            public static readonly string FIX_ALPHA = "GRAPHIC_TO_RT_FIX_ALPHA";
        }
    }
}
