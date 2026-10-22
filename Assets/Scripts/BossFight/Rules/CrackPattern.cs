using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Rules
{
    public readonly struct CrackSegment
    {
        public readonly Vector2 A;
        public readonly Vector2 B;
        public readonly float Width;

        public CrackSegment(Vector2 a, Vector2 b, float width)
        {
            A = a;
            B = b;
            Width = width;
        }
    }

    // Seeded crack lines in 0..1 space, radiating from an impact point, plus a rasteriser for the UI/wall textures.
    public static class CrackPattern
    {
        public static List<CrackSegment> Generate(int seed, Vector2 impact, int branches, int segmentsPerBranch, float length)
        {
            var rng = new System.Random(seed);
            var result = new List<CrackSegment>();
            for (int b = 0; b < branches; b++)
            {
                float angle = (b + (float)rng.NextDouble() * 0.6f) / branches * Mathf.PI * 2f;
                Walk(rng, result, impact, angle, segmentsPerBranch, length / segmentsPerBranch, 0.02f, true);
            }
            return result;
        }

        private static void Walk(System.Random rng, List<CrackSegment> result, Vector2 start, float angle, int count, float step, float width, bool allowChild)
        {
            var point = start;
            for (int i = 0; i < count; i++)
            {
                angle += ((float)rng.NextDouble() - 0.5f) * 0.9f;
                float stepLength = step * (0.7f + (float)rng.NextDouble() * 0.6f);
                var next = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * stepLength;
                if (next.x < 0f || next.x > 1f || next.y < 0f || next.y > 1f) return;
                float w = width * (1f - (float)i / count * 0.7f);
                result.Add(new CrackSegment(point, next, w));
                if (allowChild && i > 0 && rng.NextDouble() < 0.3)
                {
                    float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                    Walk(rng, result, next, angle + side * (0.6f + (float)rng.NextDouble() * 0.5f), Mathf.Max(1, (count - i) / 2), step * 0.8f, w * 0.7f, false);
                }
                point = next;
            }
        }

        // Transparent background, lines in the given colour; width is a fraction of the texture size.
        public static Color32[] Rasterize(int size, IReadOnlyList<CrackSegment> segments, Color lineColor)
        {
            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            Color32 line = lineColor;
            foreach (var s in segments)
            {
                float radius = Mathf.Max(0.5f, s.Width * size * 0.5f);
                var a = s.A * (size - 1);
                var b = s.B * (size - 1);
                int steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude * 2f));
                for (int k = 0; k <= steps; k++)
                {
                    Stamp(pixels, size, Vector2.Lerp(a, b, (float)k / steps), radius, line);
                }
            }
            return pixels;
        }

        private static void Stamp(Color32[] pixels, int size, Vector2 center, float radius, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - center.x, dy = y - center.y;
                    if (dx * dx + dy * dy <= radius * radius) pixels[y * size + x] = color;
                }
            }
        }
    }
}
