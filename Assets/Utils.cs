﻿using System;
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

        public static void WithoutScaleAndRotation(Transform transform, Action f) {
            var prevScale = transform.localScale;
            var prevRotation = transform.localRotation;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            try {
                f();
            } finally {
                transform.localScale = prevScale;
                transform.localRotation = prevRotation;
            }
        }

        public static T GetOrCreateComponent<T>(this Component c) where T : Component {
            var existing = c.GetComponent<T>();
            return existing ? existing : c.gameObject.AddComponent<T>();
        }

        public static Rect SnappedToPixels(this Rect r) {
            r.xMin = Mathf.Floor(r.xMin);
            r.yMin = Mathf.Floor(r.yMin);
            r.xMax = Mathf.Ceil(r.xMax);
            r.yMax = Mathf.Ceil(r.yMax);
            return r;
        }
    }
}
