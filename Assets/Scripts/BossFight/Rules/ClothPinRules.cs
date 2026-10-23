using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Rules
{
    // Which cloth vertices hang from the invisible hanger: the top `band` fraction of the garment's height
    // (shoulders/collar) is pinned with max distance 0; everything below may swing up to `free` metres.
    public static class ClothPinRules
    {
        public static bool IsPinned(float height, float minHeight, float maxHeight, float band)
        {
            float cut = maxHeight - Mathf.Clamp01(band) * (maxHeight - minHeight);
            return height >= cut - 1e-5f;
        }

        // Vertices are in the garment's upright frame (+Y up).
        public static float[] MaxDistances(IReadOnlyList<Vector3> vertices, float band, float free)
        {
            var result = new float[vertices.Count];
            if (vertices.Count == 0) return result;
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (var v in vertices)
            {
                min = Mathf.Min(min, v.y);
                max = Mathf.Max(max, v.y);
            }
            for (int i = 0; i < vertices.Count; i++)
            {
                result[i] = IsPinned(vertices[i].y, min, max, band) ? 0f : Mathf.Max(free, 0f);
            }
            return result;
        }

        public static int PinnedCount(float[] maxDistances)
        {
            int n = 0;
            foreach (var d in maxDistances) if (d <= 0f) n++;
            return n;
        }
    }
}
