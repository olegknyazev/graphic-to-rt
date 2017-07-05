using System;
using UnityEngine;

namespace UIToRenderTarget {
    static class Utils {
        public static void WithRenderMode(Canvas canvas, RenderMode renderMode, Action f) {
            var prevMode = canvas.renderMode;
            canvas.renderMode = renderMode;
            try {
                f();
            } finally {
                canvas.renderMode = prevMode;
            }
        }

        public static void WithObjectLayer(GameObject obj, int layer, Action f) {
            var prevLayer = obj.layer;
            obj.layer = layer;
            try {
                f();
            } finally {
                obj.layer = prevLayer;
            }
        }

        public static T GetOrCreateComponent<T>(this Component c) where T : Component {
            var existing = c.GetComponent<T>();
            return existing ? existing : c.gameObject.AddComponent<T>();
        }
    }
}
